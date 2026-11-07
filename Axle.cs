
namespace TC2.Base.Components
{
	public static partial class Rotato
	{
		public struct Bind
		{
			[Flags]
			public enum Flags: uint
			{
				None = 0u,

				Toggle = 1u << 0,
				Active = 1u << 1
			}

			public Keyboard.Key key;
			public float torque;
			public float angular_velocity;
			public Rotato.Bind.Flags flags;
		}

		[IComponent.Data(Net.SendType.Unreliable, region_only: true)]
		public partial struct Data: IComponent
		{
			public FixedArray8<Rotato.Bind> binds;
		}

		public struct EditRPC: Net.IRPC<Rotato.Data>
		{
			public Keyboard.Key? key;
			public float? torque;
			public float? angular_velocity;
			public Rotato.Bind.Flags? flags;

			public byte index;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Rotato.Data data)
			{
				ref var bind = ref data.binds.GetRefAtIndexOrNull(this.index);
				if (bind.IsNotNull())
				{
					var sync = false;

					sync |= this.key.TryCopyTo(ref bind.key);
					sync |= this.torque.TryCopyTo(ref bind.torque);
					sync |= this.angular_velocity.TryCopyTo(ref bind.angular_velocity);
					sync |= this.flags.TryCopyTo(ref bind.flags);

					if (sync)
					{
						data.Sync(rpc.entity);
					}
				}
			}
#endif
		}

#if CLIENT
		public partial struct RotatoGUI: IGUICommand
		{
			public Entity ent_rotato;
			public Rotato.Data rotato;

			public void Draw()
			{
				using (var window = GUI.Window.InteractionMisc("Rotato"u8, this.ent_rotato, size: new(0, 96 * 4)))
				{
					this.StoreCurrentWindowTypeID();
					if (window.show)
					{
						//using (GUI.Wrap.Push(GUI.RmX))
						//{
						//	var blb = (ulong)System.Diagnostics.Stopwatch.GetTimestamp();
						//	//var blba = (blb.Rol(19).ByteSwap().Neg()).Ror(7).ByteSwap(); //.ByteSwap();
						//	var blba = (((blb.Rol(19)).ByteSwap()) * ~blb).Ror(3).ByteSwap(); //.Neg()).Ror(7).ByteSwap(); //.ByteSwap();
						//																 //var blba = (XorRandom.New(true).state); //.ByteSwap();

						//	GUI.TextShaded(blb);
						//	GUI.NewLine();
						//	GUI.TextShaded(blba);
						//	GUI.NewLine();


						//	GUI.TextShaded(blb.ToFormattedBinary(true));
						//	GUI.NewLine();

						//	GUI.TextShaded(blba.ToFormattedBinary(true));
						//}
						for (var i = 0; i < this.rotato.binds.Length; i++)
						{
							ref var node = ref this.rotato.binds[i];
							using (GUI.ID<Rotato.Bind>.Push(i))
							using (var group = GUI.Group.New(size: new Vector2(GUI.AvX, 0), padding: new Vector2(4, 4)))
							{
								GUI.DrawBackground(GUI.tex_panel, group.GetOuterRect(), new(4));

								var sync = false;
								var rpc = new Rotato.EditRPC();

								if (GUI.EnumInput("key"u8, in node.key, ref rpc.key, new Vector2(GUI.RmX, 24), show_label: false).RelayTo(ref sync))
								{

								}

								if (GUI.SliderFloat("torque"u8, in node.torque, ref rpc.torque, min: -50000, max: 50000, size: new Vector2(GUI.RmX, 24), show_label: false, snap: 100).RelayTo(ref sync))
								{

								}

								if (GUI.SliderFloat("angular_velocity"u8, in node.angular_velocity, ref rpc.angular_velocity, min: -100, max: 100, size: new Vector2(GUI.RmX, 24), show_label: false, snap: 1).RelayTo(ref sync))
								{

								}

								if (GUI.EnumInput("flags"u8, in node.flags, ref rpc.flags, new Vector2(GUI.RmX, 24), show_label: false).RelayTo(ref sync))
								{

								}

								if (sync)
								{
									rpc.index = (byte)i;
									rpc.Send(this.ent_rotato);
								}
							}
						}
					}
				}
			}
		}

		[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnGUI(Entity entity, [Source.Owned] in Interactable.Data interactable,
		[Source.Owned] in Rotato.Data rotato)
		{
			if (interactable.IsActive())
			{
				var gui = new RotatoGUI()
				{
					ent_rotato = entity,
					rotato = rotato,
				};
				gui.Submit();
			}
		}
#endif

		[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region, flags: ISystem.Flags.Unchecked)]
		public static void OnUpdate(ISystem.Info info, ref Region.Data region, [Source.Owned] ref Transform.Data transform,
		[Source.Owned] ref Axle.Data axle, [Source.Owned] ref Axle.State axle_state, [Source.Owned] ref Control.Data control, [Source.Owned] ref Rotato.Data rotato)
		{
			if (AxleExt.debug_timestep && region.GetCurrentTick() % AxleExt.time_step != 0) return;

#if CLIENT
			//region.DrawDebugText(transform.position + new Vector2(-1.00f, 3 + (16 * -region.GetCanvasToWorldScale())), $"{axle_state.angular_momentum:0.000}; momentum: {axle_state.old_tmp_angular_momentum:0.000}; {axle_state.new_tmp_angular_momentum:0.000}; delta: {axle_state.old_tmp_angular_momentum_delta:0.000}; {axle_state.new_tmp_angular_momentum_delta:0.000}", Color32BGRA.White);
			//App.WriteLine($"{axle_state.angular_momentum:0.000}; momentum: {axle_state.old_tmp_angular_momentum:0.000}; {axle_state.new_tmp_angular_momentum:0.000}; delta: {axle_state.old_tmp_angular_momentum_delta:0.000}; {axle_state.new_tmp_angular_momentum_delta:0.000}");
#endif

			//if (info.GetRegion().GetCurrentTick() % AxleExt.time_step != 0) return;


			for (var i = 0; i < rotato.binds.Length; i++)
			{
				ref var bind = ref rotato.binds[i];

				if (bind.flags.HasAny(Bind.Flags.Toggle))
				{
					if (control.keyboard.GetKeyDown(bind.key)) bind.flags.ToggleFlag(Bind.Flags.Active);
				}
				else
				{
					bind.flags.SetFlag(Bind.Flags.Active, control.keyboard.GetKey(bind.key));
				}

				if (bind.flags.HasAny(Bind.Flags.Active))
				{
					axle_state.ApplyTorque(bind.torque, bind.angular_velocity);
				}

				//#if CLIENT
				//				region.DrawDebugText(transform.position + new Vector2(-1.00f, 3 + (16 * i * region.GetCanvasToWorldScale())), $"[{i:00}] {bind.torque:0.000}; {bind.angular_velocity:+0.000;-0.000}; ({bind.flags})", Color32BGRA.White);
				//#endif
			}
		}
	}

	// TODO: Needs Axle.cs exposed
	public static partial class AxleExt
	{
		public const bool debug_timestep = false;
		public static ulong time_step = 10;


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ApplyTorque(ref this Axle.State axle_state, float torque, float target_angular_velocity)
		{
			//torque = MathF.Abs(torque);
			if (torque <= Maths.epsilon) return;
			if (!Assert.Check(axle_state.torques_count < axle_state.torques.Length, Assert.Level.Warn))
			{
				return;
			}

			axle_state.torques[axle_state.torques_count++] = new(torque, target_angular_velocity);
		}

		[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnUpdateGate(/*ISystem.Info info, [Source.Shared] ref Axle.Data axle,*/ [Source.Shared] ref Axle.State axle_state,
		[Source.Shared] ref Gate.Data gate, [Source.Shared] ref Body.Data body, [Source.Owned, Original] ref Joint.Distance joint)
		{
			var dist = joint.distance - axle_state.rotation_delta;
			joint.distance = Maths.Clamp(dist, 0.00f, gate.length);

			if (Maths.Abs(gate.last_distance - joint.distance) > 0.001f)
			{
				body.Activate();

				gate.last_distance = joint.GetDelta().Length();
			}

			//axle_state.ApplyTorque(joint.GetImpulseRaw().Abs(), Region.gravity.Y.WithSign(joint.GetImpulseRaw()));

			//if (dist <= 0.00f) axle_state.ApplyTorque(100000, 0.00f);
			//else if (dist >= gate.length) axle_state.ApplyTorque(100000, 0.00f);

		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float GetRequiredTorque(ref this Axle.State axle_state, float angular_velocity)
		{
			var delta_angular_velocity = angular_velocity - axle_state.angular_velocity;
			var torque_req = Maths.Abs(delta_angular_velocity * App.tickrate_f32 * axle_state.moment_of_inertia);

			return torque_req;
		}

		[Shitcode]
		[ISystem.PreUpdate.C(ISystem.Mode.Single, ISystem.Scope.Region, flags: ISystem.Flags.Unchecked)]
		public static void OnEarlyUpdate(ISystem.Info info, ref Region.Data region,
		[Source.Owned] ref Axle.Data axle, [Source.Owned] ref Axle.State axle_state)
		{
			if (AxleExt.debug_timestep && region.GetCurrentTick() % AxleExt.time_step != 0) return;

			axle_state.moment_of_inertia = Maths.Max(0.10f, Axle.CalculateMomentOfInertia(axle.mass, axle.radius_inner, axle.radius_outer));
			axle_state.moment_of_inertia_inv = 1.00f / axle_state.moment_of_inertia;

			var sum_torque = 0.00f;
			var app_torque = 0.00f;
			var net_torque = 0.00f;

			var angular_velocity_tmp = axle_state.angular_velocity;

			if (axle_state.torques_count > 0)
			{
				var torques_span = axle_state.torques.Slice(axle_state.torques_count);
				torques_span.SortDescending();

				for (var i = torques_span.Length - 1; i >= 0; i--)
				{
					var torque = torques_span[i];

					var delta_angular_velocity = (torque.angular_velocity - angular_velocity_tmp);
					var torque_req = (delta_angular_velocity * App.tickrate_f32 * axle_state.moment_of_inertia).Abs();
					var torque_req_norm = Maths.Normalize01(torque.force, torque_req);

					var torque_app = torque_req * torque_req_norm;
					var vel_app = delta_angular_velocity * torque_req_norm;

					sum_torque += torque.force;
					app_torque += torque_app;
					net_torque += torque.force - torque_app;

					angular_velocity_tmp.MoveTowards(torque.angular_velocity, vel_app.Abs());
				}

				if (torques_span.Length > 0) torques_span.Clear();
				axle_state.torques_count = 0;
			}


			axle_state.sum_torque = sum_torque;
			axle_state.app_torque = app_torque;
			axle_state.net_torque = net_torque;

			axle_state.ApplyTorque(axle_state.force_load_new, 0.00f);

			if (!axle.flags.HasAny(Axle.Data.Flags.Unlimited_Power))
			{
				axle_state.angular_velocity = angular_velocity_tmp.WithRoundedEpsilon();
			}
			axle_state.angular_momentum = axle_state.angular_velocity * axle_state.moment_of_inertia;

			axle_state.rotational_energy = Axle.CalculateRotationalEnergy(axle_state.moment_of_inertia, axle_state.angular_velocity);

			//var rot_old = axle_state.rotation;
			//var rot = (axle_state.rotation - (axle_state.angular_velocity * App.fixed_update_interval_s));

			//axle_state.rotation_delta = Maths.DeltaAngle3(rot_old, rot);
			//axle_state.rotation_old = rot_old;

			axle_state.force_friction = Maths.Abs(9.807f * axle.mass * axle.friction);

			axle_state.force_load_old = axle_state.force_load_new;
			axle_state.force_load_new = axle_state.force_friction;

			axle_state.force_belt_in_old = axle_state.force_belt_in_new;
			axle_state.force_belt_in_new = 0.00f;

			axle_state.force_belt_out_old = axle_state.force_belt_out_new;
			axle_state.force_belt_out_new = 0.00f;





			//			if (Maths.Abs(rot) > Maths.tau)
			//			{
			//				axle_state.rotation = rot % Maths.tau;

			//#if SERVER
			//				axle_state.flags.AddFlag(Axle.State.Flags.Revolved | Axle.State.Flags.Sync_Pending);
			//#else
			//				axle_state.flags.AddFlag(Axle.State.Flags.Revolved);
			//#endif


			//			}
			//			else
			//			{
			//				axle_state.rotation = rot;
			//				axle_state.flags.RemoveFlag(Axle.State.Flags.Revolved);
			//			}

			var rot_step = axle_state.angular_velocity * App.fixed_update_interval_s;
			var rot = axle_state.rotation;
			var rot_new = rot - rot_step;

			axle_state.rotation_delta = rot_new - axle_state.rotation_old;
			axle_state.rotation_old = rot;
			axle_state.rotation = rot_new;

			if (axle.flags.HasNone(Axle.Data.Flags.No_Wrap))
			{
				//axle_state.rotation_delta = Maths.DeltaAngle3(rot_old, rot);

				//rot_delta = Maths.DeltaAngle3(rot_old, rot);

				if (Maths.Abs(rot_new) > Maths.tau)
				{
					axle_state.rotation %= Maths.tau;

#if SERVER
					axle_state.flags.AddFlag(Axle.State.Flags.Revolved | Axle.State.Flags.Sync_Pending);
#else
					axle_state.flags.AddFlag(Axle.State.Flags.Revolved);
#endif
				}
				else
				{
					axle_state.flags.RemoveFlag(Axle.State.Flags.Revolved);
				}
			}
		}

#if CLIENT
		//[ISystem.EarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region, flags: ISystem.Flags.Unchecked)]
		//public static void OnUpdateSprite(ISystem.Info info,
		//[Source.Owned] in Transform.Data transform, [Source.Owned] ref Axle.Data axle, [Source.Owned] ref Axle.State axle_state, [Source.Owned, Pair.Component<Axle.Data>] ref Animated.Renderer.Data renderer)
		//{
		//	//if (info.GetRegion().GetCurrentTick() % AxleExt.time_step != 0) return;



		//	//renderer.rotation = MathF.CopySign(axle.rotation, transform.scale.X);
		//	//renderer.rotation = axle.rotation;
		//	renderer.rotation = axle_state.rotation * transform.scale.X;
		//	renderer.z = axle.z;
		//	renderer.offset = axle.offset; // + random.NextUnitVector2Range(0.05f, 0.10f);
		//}

		[ISystem.PreRender(ISystem.Mode.Single, ISystem.Scope.Region, flags: ISystem.Flags.Unchecked)]
		public static void OnUpdateSprite(ISystem.Info info, ref Region.Data region,
		[Source.Owned] in Transform.Data transform, [Source.Owned] ref Axle.Data axle, [Source.Owned] ref Axle.State axle_state, [Source.Owned, Pair.Component<Axle.Data>] ref Animated.Renderer.Data renderer)
		{
			if (AxleExt.debug_timestep && region.GetCurrentTick() % AxleExt.time_step != 0) return;



			//renderer.rotation = MathF.CopySign(axle.rotation, transform.scale.X);
			//renderer.rotation = axle.rotation;
			renderer.rotation = Maths.LerpAngle(axle_state.rotation_old, axle_state.rotation, Vulkan.GetCurrentLerp()) * transform.scale.X;
			renderer.z = axle.z;
			renderer.offset = axle.offset; // + random.NextUnitVector2Range(0.05f, 0.10f);
		}
#endif

#if SERVER
		// TODO: really shitty sync workaround for now
		[ISystem.Update.Sync(ISystem.Mode.Single, ISystem.Scope.Region, interval: 0.30f, flags: ISystem.Flags.Unchecked)]
		public static void OnUpdateSync(Entity entity,
		[Source.Owned] ref Axle.State axle_state, [Source.Owned] ref Axle.Sync axle_sync)
		{
			return;

			//if (axle_state.flags.HasAny(Axle.State.Flags.Sync_Pending))
			{
				axle_sync.rotation = axle_state.rotation; // - (axle_sync.angular_velocity * App.fixed_update_interval_s);
				axle_sync.angular_velocity = axle_state.angular_velocity;
				axle_sync.angular_momentum = axle_state.angular_momentum;
				axle_sync.elapsed = 0.00f;
				axle_sync.Sync(entity);

				axle_state.flags.RemoveFlag(Axle.State.Flags.Sync_Pending);
			}
		}

#elif CLIENT
		//[ISystem.Update.Sync(ISystem.Mode.Single, ISystem.Scope.Region, flags: ISystem.Flags.Unchecked)]
		[ISystem.PreUpdate.B(ISystem.Mode.Single, ISystem.Scope.Region, flags: ISystem.Flags.Unchecked)]
		public static void OnUpdateSync(Entity entity, ISystem.Info info,
		[Source.Owned] ref Axle.State axle_state, [Source.Owned] ref Axle.Sync axle_sync)
		{
			return;
			//if (axle_sync.elapsed > 0.30f) return;
			//return;

			//axle_state.rotation = axle_sync.rotation - (axle_sync.angular_velocity * App.fixed_update_interval_s);
			//axle_state.angular_velocity = axle_sync.angular_velocity;
			//axle_state.angular_momentum = axle_sync.angular_momentum;

			var error_ratio = Maths.Clamp((MathF.Abs(Maths.DeltaAngle3(axle_state.rotation, axle_sync.rotation - (axle_sync.elapsed * axle_sync.angular_velocity)))) * 0.50f, 0.00f, 1.00f);
			if (axle_sync.elapsed == 0.00f || error_ratio > 0.01f)
			{
				var lerp = Maths.Clamp((1.00f - axle_sync.elapsed) * 0.10f, error_ratio, 1.00f);
				axle_state.rotation = Maths.LerpAngle(axle_state.rotation, axle_sync.rotation - (axle_sync.elapsed * axle_sync.angular_velocity), lerp);
				axle_state.angular_velocity = Maths.Lerp(axle_state.angular_velocity, axle_sync.angular_velocity, lerp);
				axle_state.angular_momentum = Maths.Lerp(axle_state.angular_momentum, axle_sync.angular_momentum, lerp);
			}
			axle_sync.elapsed += App.fixed_update_interval_s_f32;
		}
#endif


		//[ISystem.Update.B(ISystem.Mode.Single, ISystem.Scope.Region, flags: ISystem.Flags.Unchecked)]
		//public static void OnUpdateArm(ISystem.Info info, ref Region.Data region, Entity ent_arm,
		//[Source.Shared] in Transform.Data transform, [Source.Owned] in Interactor.Data interactor, [Source.Owned] ref Arm.Data arm)
		//{
		//	if (arm.axle_spin_ref.IsValid())
		//	{
		//		ref var axle = ref arm.axle_spin_ref.GetComponent();
		//		if (axle.IsNotNull())
		//		{
		//			axle.ApplyTorque((arm.spin_torque * arm.axle_spin_modifier).Abs(), arm.max_spin_speed * arm.axle_spin_modifier);
		//		}
		//	}
		//}

	}
}