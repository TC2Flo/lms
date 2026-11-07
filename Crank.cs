
namespace TC2.Base.Components
{
	public static partial class Crank
	{
		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public partial struct Data(): IComponent
		{
			[Save.Force, Editor.Picker.Position(true)] public required Vec2f offset;
			[Save.Force] public required float radius = 0.50f;
			[Save.Force] public required float ratio = 1.00f;

			[Save.Force] public required float max_speed = 8.00f;
			public float torque;

			public Mouse.Key keys = Mouse.Key.Right;
			[Asset.Ignore, Save.Ignore, Net.Ignore] public float t_last_sync;
		}

		public static void CalculateArmForce(float push_force, float strength, float dexterity, float lever_radius, out float torque, out float speed)
		{
			torque = (push_force * (1.00f + (strength * 0.10f))) * lever_radius;
			speed = 20 * (1.00f + (dexterity * 0.50f));
		}

		public const float update_interval = 0.20f;

		//#if SERVER
		//		[ISystem.Update(ISystem.Mode.Single, ISystem.Scope.Region)]
		//		public static void OnUpdate(ISystem.Info info, Entity entity, [Source.Owned] in Transform.Data transform,
		//		[Source.Owned] ref Worker.Data worker, [Source.Owned] ref Worker.State worker_state, 
		//		[Source.Work] ref Axle.Data axle, [Source.Work] ref Axle.State axle_state,
		//		[Source.Work] in Transform.Data transform_crank, [Source.Work] ref Crank.Data crank)
		//		{
		//			if (info.WorldTime >= worker_state.next_work && Vector2.DistanceSquared(transform.position, transform_crank.position) <= (2 * 2))
		//			{
		//				//var cooldown = 0.20f;

		//				//axle_state.SetAngularVelocity(crank.torque, crank.max_speed * 0.50f);

		//				//App.WriteLine("crank work");

		//				//worker_state.next_work = info.WorldTime + (cooldown * 5.00f);
		//			}
		//		}
		//#endif

		//[ISystem.Add(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("initialized", true, Source.Modifier.Owned)]
		//public static void OnAddVehicle(ISystem.Info info, Entity ent_crank,
		//[Source.Owned] in Crank.Data crank, [Source.Parent] in Joint.Base joint, [Source.Parent] ref Vehicle.Data vehicle)
		//{
		//	if (!vehicle.ent_crank.IsAlive()) vehicle.ent_crank = ent_crank;
		//}

		//[ISystem.Remove(ISystem.Mode.Single, ISystem.Scope.Region)]
		//public static void OnRemVehicle(ISystem.Info info, Entity ent_crank,
		//[Source.Owned] in Crank.Data crank, [Source.Parent] in Joint.Base joint, [Source.Parent] ref Vehicle.Data vehicle)
		//{
		//	if (vehicle.ent_crank == ent_crank) vehicle.ent_crank = default;
		//}

		//[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region)]
		//public static void UpdateAxle(ISystem.Info info, Entity entity,
		//[Source.Parent] ref Axle.Data axle, [Source.Parent] ref Axle.State axle_state,
		//[Source.Owned] in Transform.Data transform, [Source.Owned] ref Crank.Data crank, [Source.Owned] ref Body.Data body)
		//{
		//	var w = body.GetAngularVelocity();
		//	body.SetAngularVelocity(axle_state.angular_velocity);
		//	axle_state.ApplyTorque(crank.torque, w);
		//}

		[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void UpdateAxle(ISystem.Info info, Entity entity,
		[Source.Owned] ref Axle.Data axle, [Source.Owned] ref Axle.State axle_state,
		[Source.Owned] in Transform.Data transform, [Source.Owned] ref Crank.Data crank)
		{

		}


		[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void UpdateHeld(ISystem.Info info, Entity entity,
		[Source.Owned] ref Axle.Data axle, [Source.Owned] ref Axle.State axle_state,
		[Source.Owned] in Transform.Data transform, [Source.Owned] ref Crank.Data crank,
		[Source.Parent] ref Arm.Data arm, [Source.Parent, Override] in Organic.Data organic,
		[Source.Parent] in Control.Data control)
		{
			if (control.mouse.GetKey(crank.keys))
			{
				Crank.CalculateArmForce(arm.push_force, organic.strength, organic.dexterity, axle.radius_outer, out var torque, out var speed);
				axle_state.ApplyTorque(torque, speed);

				//App.WriteLine($"{organic.strength}; {organic.dexterity} = {torque}; {speed}");
			}
		}

		//[ISystem.Update(ISystem.Mode.Single, ISystem.Scope.Region)]
		//public static void UpdateVehicle(ISystem.Info info, Entity entity,
		//[Source.Shared] ref Vehicle.Data vehicle, [Source.Owned] ref Arm.Data arm, [Source.Shared, Override] in Organic.Data organic, [Source.Shared] in Control.Data control)
		//{
		//	App.WriteLine(vehicle.ent_crank);
		//}

		[ISystem.VeryEarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void UpdateGun(ISystem.Info info, Entity entity,
		[Source.Owned] ref Gun.Data gun, [Source.Owned] ref Gun.State gun_state,
		[Source.Owned] in Axle.Data axle, [Source.Owned] ref Axle.State axle_state,
		[Source.Owned] ref Crank.Data crank,
		[Source.Owned] ref Control.Data control)
		{
			if (gun.action == Gun.Action.Crank)
			{
				gun_state.hints.AddFlag(Gun.Hints.Hold_RMB);

				if (!gun_state.hints.HasAny(Gun.Hints.Cycled))
				{
					if (axle_state.flags.HasAny(Axle.State.Flags.Revolved))
					{
#if SERVER
						gun_state.stage = Gun.Stage.Cycling;
						gun_state.Sync(entity, true);
#endif

						//gun_state.hints.SetFlag(Gun.Hints.Cycled, true);
						//gun_state.stage = Gun.Stage.Cycling;
						//control.mouse.SetKeyPressed(Mouse.Key.Left, true);
					}
					else
					{
						//axle_state.SetAngularVelocity(Maths.Lerp(500.00f, 0.00f, Maths.NormalizeClamp(info.WorldTime - gun_state.next_cycle, 0.15f)), 0.00f);
						//axle_state.SetAngularVelocity(500.00f, 0.00f);
						//axle_state.SetAngularVelocity(axle_state.rotation, 0.00f);

						axle_state.ApplyTorque(Maths.Lerp(200.00f, 0.00f, Maths.Normalize01Fast(Maths.NormalizeAngle(axle_state.rotation), MathF.PI)), 0.00f);

						//App.WriteLine(Maths.NormalizeClamp(Maths.NormalizeAngle(axle_state.rotation), MathF.PI));
						//App.WriteLine(Maths.NormalizeClamp(info.WorldTime - gun_state.next_cycle, 0.15f));
					}
				}
			}
		}

#if CLIENT
		[ISystem.PostUpdate.D(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void UpdateRenderer(ISystem.Info info, Entity entity,
		[Source.Owned] ref Axle.Data axle, [Source.Owned] ref Axle.State axle_state,
		[Source.Owned] in Transform.Data transform, [Source.Owned] ref Crank.Data crank,
		[Source.Owned, Pair.Component<Crank.Data>] ref Animated.Renderer.Data renderer)
		{
			//renderer.offset = crank.offset;
			renderer.offset = crank.offset + (Vec2f.CreateFromRad(-axle_state.rotation * crank.ratio) * crank.radius);
			//renderer.rotation = axle_state.rotation;
		}

		[ISystem.PostUpdate.D(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void UpdateSound(ISystem.Info info, Entity entity,
		[Source.Owned] ref Axle.Data axle, [Source.Owned] ref Axle.State axle_state,
		[Source.Owned] in Transform.Data transform, [Source.Owned] ref Crank.Data crank,
		[Source.Owned, Pair.Component<Crank.Data>] ref Sound.Emitter sound_emitter)
		{
			sound_emitter.volume_mult = Maths.Lerp(sound_emitter.volume_mult, Maths.Clamp(MathF.Abs(axle_state.angular_velocity) * 2.00f, 0.00f, 1.00f) * 0.50f, 0.40f);
			sound_emitter.pitch_mult = 1.00f + (MathF.Sin(axle_state.rotation) * 0.05f);
		}
#endif

		[ISystem.Update.B(ISystem.Mode.Single, ISystem.Scope.Region, flags: ISystem.Flags.Unchecked)]
		public static void OnUpdateArm(ISystem.Info info, ref Region.Data region, Entity ent_arm,
		[Source.Shared] in Transform.Data transform, [Source.Owned] in Interactor.Data interactor,
		[Source.Owned] ref Arm.Data arm)
		{
			if (arm.axle_spin_ref.IsValid())
			{
				ref var axle = ref arm.axle_spin_ref.GetComponent();
				if (axle.IsNotNull())
				{
					axle.ApplyTorque(arm.spin_torque, arm.max_spin_speed * arm.axle_spin_modifier);
				}
			}
		}

#if CLIENT
		public partial struct CrankGUI: IGUICommand
		{
			public Entity ent_axle;

			public Axle.Data axle;
			public Axle.State axle_state;
			public Crank.Data crank;

			public static float edit_modifier_current;
			public static float t_next_sync;

			[Shitcode]
			public void Draw()
			{
				using (var window = GUI.Window.InteractionMisc("Crank"u8, this.ent_axle, size: new(0, 32)))
				{
					this.StoreCurrentWindowTypeID(-10);
					if (window.show)
					{
						ref var region = ref Client.GetRegion();

						using (var group = GUI.Group.New(size: GUI.Rm))
						{
							//GUI.DrawBackground(GUI.tex_panel, group.GetOuterRect(), new(4));

							ref var player = ref Client.GetPlayer();
							var ent_arm = player.ent_interactor;
							ref var arm = ref ent_arm.GetComponent<Arm.Data>();

							//if (GUI.SliderAngle())

							if (GUI.SliderFloat("Crank Torque"u8, ref edit_modifier_current, min: -1, max: 1, size: GUI.Rm, show_label: false))
							{
								//App.WriteValue(GUI.GetActiveID());
							}

							var last_item_id = GUI.GetLastItemID();
							var last_active_item_id = GUI.GetLastActiveID();
							var active_item_id = GUI.GetActiveID();

							var time = App.GetCurrentTime();
							var is_held = false;

							if (last_item_id != 0)
							{
								//App.WriteValue("active", last_active_item_id);

								if (last_item_id == last_active_item_id)
								{
									//active_item_id == 0
									if (GUI.IsMouseDown(GUI.MouseButton.Left))
									{
										is_held = true;
										if (time >= t_next_sync)
										{
											t_next_sync = time + 0.20f;
											//App.WriteLine("hold");

											if (arm.IsNotNull())
											{
												var rpc = new Arm.SetSpinRPC()
												{
													ent_axle = this.ent_axle,
													modifier = edit_modifier_current,
												};
												rpc.Send(ent_arm);
											}

											//App.WriteValue("active", active_item_id);

										}

										//App.WriteValue("active", active_item_id);
									}
									else if (GUI.IsMouseReleased(GUI.MouseButton.Left)) //active_item_id == 0) // GUI.IsMouseReleased(GUI.MouseButton.Left))
									{
										//App.WriteValue("release", active_item_id);

										if (arm.IsNotNull())
										{
											var rpc = new Arm.SetSpinRPC()
											{
												ent_axle = default,
												modifier = 0.00f
											};
											rpc.Send(ent_arm);
										}
									}

								}
							}

							if (!is_held)
							{
								edit_modifier_current.LerpFMARef(0.00f, 0.08f);
							}

							//if (GUI.IsItemHovered())
							//{
							//	if (GUI.IsMouseDown(GUI.MouseButton.Left))
							//	{
							//		//var time = App.GetCurrentTime();
							//		//if (time >= t_next_sync)
							//		//{
							//		//	t_next_sync = time + 0.20f;
							//		//	//App.WriteLine("hold");

							//		//	if (arm.IsNotNull())
							//		//	{
							//		//		var rpc = new Arm.SetSpinRPC()
							//		//		{
							//		//			ent_axle = this.ent_axle,
							//		//			modifier = edit_modifier_current,
							//		//		};
							//		//		rpc.Send(ent_arm);
							//		//	}
							//		//}
							//	}
							//	else if (GUI.IsMouseReleased(GUI.MouseButton.Left))
							//	{
							//	}
							//}
							//else
							//{
							//	edit_modifier_current.LerpFMARef(0.00f, 0.08f);
							//}

							//if (sync)
							//{
							//	rpc.index = (byte)i;
							//	rpc.Send(this.ent_rotato);
							//}
						}
					}
				}
			}
		}

		[ISystem.LateGUI(ISystem.Mode.Single, ISystem.Scope.Region, order: -5)]
		public static void OnGUI(Entity entity, [Source.Owned] in Interactable.Data interactable,
		[Source.Owned] in Axle.Data axle, [Source.Owned] in Axle.State axle_state,
		[Source.Owned] in Crank.Data crank)
		{
			if (interactable.IsActive() && axle.flags.HasNone(Axle.Data.Flags.No_GUI))
			{
				var gui = new CrankGUI()
				{
					ent_axle = entity,
					axle = axle,
					axle_state = axle_state,
					crank = crank
				};
				gui.Submit();
			}
		}
#endif
	}
}
