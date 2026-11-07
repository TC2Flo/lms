namespace TC2.Base.Components
{
	//public static partial class Turbine
	//{
	//	[IComponent.Data(Net.SendType.Reliable, region_only: true)]
	//	public struct Data: IComponent
	//	{
	//		public float speed;
	//		public float speed_max;

	//		public Data()
	//		{

	//		}
	//	}
	//}

	//public static partial class Driveline
	//{
	//	[IComponent.Data(Net.SendType.Reliable, region_only: true)]
	//	public struct Data: IComponent
	//	{


	//		public Data()
	//		{

	//		}
	//	}
	//}

	public static partial class Motor
	{
		public enum Type: byte
		{
			Undefined = 0,

			Inline,
			Flat,
			Rotary,
			Radial,
			Turbine,
			Vortex
		}

		[Flags]
		public enum Flags: ushort
		{
			None = 0,

			Constant_Speed = 1 << 0,
			Constant_Power = 1 << 1,

			Control_Speed = 1 << 2,
			Control_Power = 1 << 3,

			//Faction = 1 << 8,
		}

		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Global | IComponent.Scope.Region), IComponent.With<Motor.State>()]
		public struct Data(): IComponent
		{
			public float speed_max;
			public float vibration;

			public Keyboard.Key key_acc = Keyboard.Key.MoveUp;
			public Keyboard.Key key_dec = Keyboard.Key.MoveDown;

			public Motor.Flags flags;
		}

		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Global | IComponent.Scope.Region)]
		public struct State: IComponent
		{
			public float speed_target_ratio;
			public float power_target_ratio;
			public float gear_target_ratio;

			[Save.Ignore, Net.Ignore] public float load_ratio;
			[Save.Ignore, Net.Ignore] public float speed_ratio;
			[Save.Ignore, Net.Ignore] public float utilization_ratio;
			[Save.Ignore, Net.Ignore] public float force_available;
			[Save.Ignore, Net.Ignore] public float force_required;
			[Save.Ignore, Net.Ignore] public float force_applied;

			[Save.Ignore, Net.Ignore] public float speed_max;
			[Save.Ignore, Net.Ignore] public float speed_axle;
			[Save.Ignore, Net.Ignore] public float speed_current;
			[Save.Ignore, Net.Ignore] public float intensity_current;
			[Save.Ignore, Net.Ignore] public float acceleration_current;
			[Save.Ignore, Net.Ignore] public float load_current;
			[Save.Ignore, Net.Ignore] public float load_base;
			[Save.Ignore, Net.Ignore] public float t_next_sync;
		}

		[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnUpdateVibrate([Source.Owned] ref Vibrate.Data vibrate, [Source.Owned] ref Motor.State motor_state, [Source.Owned] ref Motor.Data motor)
		{
			vibrate.modifier = motor_state.intensity_current * motor.vibration;
		}

		// TODO: add sync rate limiting
		[ISystem.Update.B(ISystem.Mode.Single, ISystem.Scope.Global | ISystem.Scope.Region)]
		public static void OnUpdateControl(ISystem.Info.Common info, Entity entity,
		[Source.Owned] in Motor.Data motor, [Source.Owned] ref Motor.State motor_state,
		[Source.Owned] in Control.Data control)
		{
#if SERVER
			if (motor.flags.HasAny(Motor.Flags.Control_Speed | Motor.Flags.Control_Power))
			{
				var axis = control.keyboard.GetKeyAxis(motor.key_acc, motor.key_dec, info.DeltaTime);

				var sync = false;
				if (control.keyboard.GetKey(Keyboard.Key.LeftShift))
				{
					if (motor.flags.HasAny(Motor.Flags.Control_Power)) sync |= motor_state.power_target_ratio.TrySet(Maths.Clamp01(motor_state.power_target_ratio + axis));
				}
				else
				{
					if (motor.flags.HasAny(Motor.Flags.Control_Speed)) sync |= motor_state.speed_target_ratio.TrySet(Maths.Clamp01(motor_state.speed_target_ratio + axis));
				}

				if (sync)
				{
					motor_state.Sync(entity, true);
				}
			}
#endif
		}

		[WIP]
		[Shitcode]
		[ISystem.Update.C(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnUpdateAttached(ISystem.Info info, Entity entity,
		[Source.Owned] ref Motor.Data motor, [Source.Owned] ref Motor.State motor_state,
		[Source.Parent] ref Axle.Data axle, [Source.Parent] ref Axle.State axle_state, [Source.Parent] ref Attachment.Slot attachment)
		{
			////if (attachment.tags_filter.)
			//axle_state.ApplyTorque(motor_state.force_available, motor_state.speed_current);
			//motor_state.force_applied = axle_state.app_torque;

			//var required_torque = axle_state.GetRequiredTorque(motor_state.speed_current) + axle_state.force_load_old;

			axle_state.force_load_new += motor_state.load_base;
			axle_state.ApplyTorque(motor_state.force_available, motor_state.speed_current);
			//axle_state.ApplyTorque(motor_state.load_current, 0.00f);

			motor_state.load_ratio = motor_state.load_ratio.FixNaN();
			motor_state.load_ratio.LerpFMARef(Maths.RelativeChangeRatio(motor_state.load_current, axle_state.force_load_old) * 10.00f, 0.13f);
			motor_state.load_current = axle_state.force_load_old;


			//var speed_ratio = Maths.Normalize01Fast(motor_state.speed_current, motor_state.speed_max);
			motor_state.speed_ratio = Maths.Normalize01Fast(axle_state.angular_velocity.Abs(), motor_state.speed_current).ReplaceNaN();

			//motor_state.force_required = required_torque;
			motor_state.speed_axle = axle_state.angular_velocity.Abs();
			motor_state.utilization_ratio = Maths.Normalize01(motor_state.load_current, motor_state.force_available).Sqrt().ReplaceNaN(); // (Maths.Normalize(required_torque, motor_state.force_available) * motor_state.power_target_ratio).Sqrt();
			motor_state.force_applied = Maths.Max(0.00f, motor_state.force_available * motor_state.utilization_ratio.Clamp01()); // * motor_state.utilization_ratio;

			//var ratio = Maths.Max(motor_state.power_target_ratio, Maths.Avg(motor_state.utilization_ratio + motor_state.power_target_ratio, motor_state.power_target_ratio) * motor_state.power_target_ratio);
			var ratio = (motor_state.utilization_ratio + Maths.Avg((motor_state.acceleration_current.Clamp0X() * 3), (motor_state.load_ratio.Clamp0X() * 4))) * motor_state.power_target_ratio;
			//var ratio = Maths.Avg(motor_state.utilization_ratio, motor_state.speed_ratio) * motor_state.power_target_ratio;
			var intensity = Maths.Clamp(ratio * 1.50f, 0.00f, ratio * motor_state.power_target_ratio * 3) * motor_state.speed_ratio;

			motor_state.intensity_current.LerpFMARef(intensity, 0.10f);
			motor_state.intensity_current = motor_state.intensity_current.ReplaceNaN();
		}

		[ISystem.Update.C(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnUpdateModule(ISystem.Info info, Entity ent_axle, Entity ent_motor,
		[Source.Owned] ref Axle.Data axle, [Source.Owned] ref Axle.State axle_state,
		[Source.Parent<Module.Link>] ref Motor.Data motor, [Source.Parent<Module.Link>] ref Motor.State motor_state)
		{
			//if (attachment.tags_filter.)

			var required_torque = axle_state.GetRequiredTorque(motor_state.speed_current) + axle_state.force_load_old;

			axle_state.force_load_new += motor_state.load_base;
			axle_state.ApplyTorque(motor_state.force_available, motor_state.speed_current);
			//axle_state.ApplyTorque(motor_state.load_current, 0.00f);


			motor_state.load_ratio = Maths.Normalize01(motor_state.force_available, axle_state.force_load_old);
			motor_state.utilization_ratio = (Maths.Normalize(required_torque, motor_state.force_available) * motor_state.power_target_ratio).Sqrt();
			motor_state.force_applied = motor_state.force_available * motor_state.utilization_ratio;

			var ratio = Maths.Max(motor_state.power_target_ratio, Maths.Avg(motor_state.utilization_ratio + motor_state.power_target_ratio, motor_state.power_target_ratio) * motor_state.power_target_ratio);
			var intensity = Maths.Clamp(ratio * 1.50f, 0.00f, ratio * motor_state.power_target_ratio * 3);

			motor_state.intensity_current = intensity; // - (motor_state.reserved * 0.10f);
													   //motor_state.load_current = axle_state.force_load_old; // - (motor_state.reserved * 0.10f);
		}

		public struct ConfigureRPC: Net.IRPC<Motor.State>
		{
			public float? speed_target_ratio;
			public float? power_target_ratio;

			public Keyboard.Key? key_acc;
			public Keyboard.Key? key_dec;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Motor.State motor_state)
			{
				{
					var sync = false;
					sync |= this.speed_target_ratio.TryCopyToClamped01(ref motor_state.speed_target_ratio);
					sync |= this.power_target_ratio.TryCopyToClamped01(ref motor_state.power_target_ratio);
					if (sync) rpc.Sync(ref motor_state);
				}

				ref var motor = ref rpc.GetComponent<Motor.Data>();
				if (motor.IsNotNull())
				{
					var sync = false;
					sync |= this.key_acc.TryCopyTo(ref motor.key_acc);
					sync |= this.key_dec.TryCopyTo(ref motor.key_dec);
					if (sync) rpc.Sync(ref motor);
				}
			}
#endif
		}

#if CLIENT
		public partial struct MotorGUI: IGUICommand
		{
			public Entity ent_motor;
			public Entity ent_axle;
			public Entity ent_interactable;

			public Motor.Data motor;
			public Motor.State motor_state;

			public Axle.Data axle;
			public Axle.State axle_state;

			public bool is_module;

			public void Draw()
			{
				//using (var window = GUI.Window.Interaction("Motor"u8, this.ent_interactable))
				using (var window = GUI.Window.InteractionMisc("Motor"u8, this.ent_interactable, size: new(0, 0), min_width: 96)) //, show_misc: this.ent_motor != Interactable.GetCurrentTarget()))
				{
					//App.WriteLine(this.ent_interactable);

					this.StoreCurrentWindowTypeID();
					//using (var embed_modules = GUI.Embed.New("motor.embed"u8, size: GUI.GetRemainingSpace()))
					{
						if (window.show)
						{
							//GUI.Text($"{this.ent_axle}\n{this.ent_interactable}\n{this.ent_motor}");

							//using (GUI.Group.New(new(0, 0)))
							{
								using (GUI.Group.New(new(GUI.RmX, 96)))
								{
									//Boiler.DrawGauge(this.arcane_engine_state.speed_current, 0.00f, this.arcane_engine.speed_max);
									//GUI.DrawGauge(MathF.Abs(this.axle_state.angular_velocity), 0.00f, this.motor.speed_max);
									GUI.DrawGauge2(MathF.Abs(this.axle_state.angular_velocity), 0.00f, this.motor_state.speed_max, size: new(96, 96));

									if (GUI.TrySameLine(96))
									{

										using (GUI.Group.New(new(GUI.RmX, 96)))
										{
											//GUI.DrawInventoryDock(Inventory.Type.Essence, new(48, 48));

											//using (GUI.Group.New(size: new(GUI.RmX, 0.00f), padding: new(4, 4)))
											//{
											//	//GUI.Text($"{MathF.Abs(this.axle_state.angular_velocity):0.00}/{this.arcane_engine.speed_max:0.00} rad/s");
											//	GUI.Text($"{MathF.Abs(this.axle_state.angular_velocity):0.00} rad/s");
											//	GUI.Text($"{(this.motor_state.force_available * 0.001f):0.00} kN");
											//	//GUI.Text($"{(this.motor_state.force_available * MathF.Abs(this.motor_state.speed_current) * 0.001f):0.00} kW");
											//}

											using (var group_keys = GUI.Group.New(new(GUI.RmX, 48)))
											{
												if (GUI.EnumInput("key.acc"u8, ref this.motor.key_acc, new Vector2(GUI.RmX, group_keys.GetHeight() * 0.50f), show_label: false))
												{
													var rpc = new Motor.ConfigureRPC()
													{
														key_acc = this.motor.key_acc
													};
													rpc.Send(this.ent_motor);
												}

												//GUI.SameLine();

												if (GUI.EnumInput("key.dec"u8, ref this.motor.key_dec, new Vector2(GUI.RmX, group_keys.GetHeight() * 0.50f), show_label: false))
												{
													var rpc = new Motor.ConfigureRPC()
													{
														key_dec = this.motor.key_dec
													};
													rpc.Send(this.ent_motor);
												}
											}
										}
									}
								}

								//using (var group_control = GUI.Group.New(new(Maths.Min(96, GUI.RmX), 0)))
								using (var group_control = GUI.Group.New(new(GUI.RmX, 24)))
								{
									if (this.motor.flags.HasNone(Motor.Flags.Constant_Speed))
									{
										//if (GUI.SliderFloatLerp("Target Speed"u8, ref this.motor_state.speed_target_ratio, 0.00f, this.motor_state.speed_max, size: new Vector2(24, 64), vertical: true))
										if (GUI.SliderFloatLerp(label: "Target Speed"u8, t: ref this.motor_state.speed_target_ratio, color_frame: GUI.col_button_yellow,
										a: 0.00f, b: this.motor_state.speed_max, size: new Vector2(GUI.RmX * 0.50f, 24), vertical: false))
										{
											var rpc = new Motor.ConfigureRPC()
											{
												speed_target_ratio = this.motor_state.speed_target_ratio
											};
											rpc.Send(this.ent_motor);
										}

										GUI.SameLine();
									}

									if (this.motor.flags.HasNone(Motor.Flags.Constant_Power))
									{
										if (GUI.SliderFloat(label: "Power"u8, value: ref this.motor_state.power_target_ratio, color_frame: GUI.col_output,
										min: 0.00f, max: 1.00f, size: new Vector2(GUI.RmX, 24), vertical: false))
										{
											var rpc = new Motor.ConfigureRPC()
											{
												power_target_ratio = this.motor_state.power_target_ratio
											};
											rpc.Send(this.ent_motor);
										}

										//GUI.SameLine();
									}

									//using (var group_keys = GUI.Group.New(new(GUI.RmX, GUI.RmY)))
									//{
									//	if (GUI.EnumInput("key.acc"u8, ref this.motor.key_acc, new Vector2(GUI.RmX, group_keys.GetHeight() * 0.50f), show_label: false))
									//	{
									//		var rpc = new Motor.ConfigureRPC()
									//		{
									//			key_acc = this.motor.key_acc
									//		};
									//		rpc.Send(this.ent_motor);
									//	}

									//	//GUI.SameLine();

									//	if (GUI.EnumInput("key.dec"u8, ref this.motor.key_dec, new Vector2(GUI.RmX, group_keys.GetHeight() * 0.50f), show_label: false))
									//	{
									//		var rpc = new Motor.ConfigureRPC()
									//		{
									//			key_dec = this.motor.key_dec
									//		};
									//		rpc.Send(this.ent_motor);
									//	}
									//}
								}
							}
						}
					}
				}
			}
		}

		[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnGUI(ISystem.Info info, ref Region.Data region,
		Entity ent_interactable, Entity ent_interactable_parent, Entity ent_motor, Entity ent_axle,
		[Source.Owned] in Motor.Data motor, [Source.Owned] in Motor.State motor_state,
		[Source.Any] in Axle.Data axle, [Source.Any] in Axle.State axle_state,
		[Source.Owned] in Interactable.Data interactable, [Source.Parent, Optional] in Interactable.Data interactable_parent)
		{
			if (interactable.IsActive() || interactable_parent.IsActive())
			{
				//App.WriteLine(ent_interactable);

				var gui = new MotorGUI()
				{
					ent_motor = ent_motor,
					ent_axle = ent_axle,

					ent_interactable = ent_interactable_parent.IsValid() ? ent_interactable_parent : ent_interactable,

					motor = motor,
					motor_state = motor_state,

					axle = axle,
					axle_state = axle_state,

					is_module = false
				};
				gui.Submit();
			}
		}

		[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnGUIModule(Entity ent_interactable, Entity ent_motor, Entity ent_axle,
		[Source.Parent<Module.Link>] ref Motor.Data motor, [Source.Parent<Module.Link>] ref Motor.State motor_state,
		[Source.Owned] in Axle.Data axle, [Source.Owned] in Axle.State axle_state,
		[Source.Owned] in Interactable.Data interactable)
		{
			if (interactable.IsActive())
			{
				var gui = new MotorGUI()
				{
					ent_motor = ent_motor,
					ent_axle = ent_axle,
					ent_interactable = ent_interactable,

					motor = motor,
					motor_state = motor_state,

					axle = axle,
					axle_state = axle_state,

					is_module = true
				};
				gui.Submit();
			}
		}
#endif
	}
}