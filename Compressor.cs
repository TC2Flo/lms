
namespace TC2.Base.Components
{
	public static partial class Air
	{
		public static partial class Compressor
		{
			[Flags]
			public enum Flags: ushort
			{
				None = 0,


			}

			[IComponent.Data(Net.SendType.Reliable, region_only: true)]
			public partial struct Data(): IComponent
			{
				public float force = 100.00f;

				public Pressure velocity_min = -4.00f;
				public Pressure velocity_max = 4.00f;

				public float modifier_current = 1.00f;
				public float modifier_target = 1.00f;

				public float velocity_mult = 1.00f;

				public Compressor.Flags flags;
			}

			public struct ConfigureRPC: Net.IRPC<Compressor.Data>
			{
				public float? modifier;

#if SERVER
				public void Invoke(Net.IRPC.Context rpc, ref Compressor.Data data)
				{
					var sync = false;

					sync |= this.modifier.TryCopyToClamped01(ref data.modifier_target);

					if (sync)
					{
						data.Sync(rpc.entity);
					}
				}
#endif
			}

#if CLIENT
			public partial struct CompressorGUI: IGUICommand
			{
				public Entity ent_compressor;
				public Compressor.Data compressor;

				public Transform.Data transform;
				public Air.Container.Data air_container;

				public Air.Vent.Data vent_intake;
				public Air.Vent.Data vent_exhaust;

				public void Draw()
				{
					using (var window = GUI.Window.Interaction("Compressor"u8, this.ent_compressor))
					{
						this.StoreCurrentWindowTypeID(-10);
						if (window.show)
						{
							using (GUI.Group.New(new(GUI.RmX, 96)))
							{
								var delta_v = (vent_exhaust.velocity_old - vent_intake.velocity_old) * -0.50f;
								//GUI.Text($"{vent_exhaust.velocity_old}; {vent_intake.velocity_old}; {delta_v}");

								GUI.DrawGauge2(delta_v, this.compressor.velocity_min, this.compressor.velocity_max, size: new(96, 96));
							}

							//if (GUI.SliderFloat("Target Speed", ref this.compressor.modifier_target, 0.00f, 1.00f, size: new Vector2(96, 32)))
							if (GUI.SliderFloatLerp("Pressure", ref this.compressor.modifier_target, this.compressor.velocity_min, this.compressor.velocity_max, size: new Vector2(96, 32), snap: 0.05f))
							{
								var rpc = new Compressor.ConfigureRPC()
								{
									modifier = this.compressor.modifier_target
								};
								rpc.Send(this.ent_compressor);
							}
						}
					}
				}
			}

			[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region)]
			public static void OnGUI(Entity entity,
			[Source.Owned] in Transform.Data transform, [Source.Owned] in Air.Container.Data air_container,
			[Source.Owned, Pair.Tag("intake")] in Air.Vent.Data vent_intake,
			[Source.Owned, Pair.Tag("exhaust")] in Air.Vent.Data vent_exhaust,
			[Source.Owned] in Compressor.Data compressor,
			[Source.Owned] in Interactable.Data interactable)
			{
				if (interactable.IsActive())
				{
					var gui = new CompressorGUI()
					{
						ent_compressor = entity,
						compressor = compressor,

						transform = transform,
						air_container = air_container,

						vent_intake = vent_intake,
						vent_exhaust = vent_exhaust
					};
					gui.Submit();
				}
			}
#endif

			[ISystem.Update.B(ISystem.Mode.Single, ISystem.Scope.Region)]
			public static void System_Update(ISystem.Info info, ref Region.Data region, Entity entity, ref XorRandom random,
			[Source.Owned] in Transform.Data transform,
			[Source.Owned] ref Air.Compressor.Data compressor)
			{
				//compressor.modifier_target = Maths.Lerp(compressor.pressure_min, compressor.pressure_max, compressor.modifier);
				compressor.modifier_current.MoveTowardsDamped(compressor.modifier_target, 0.50f * info.DeltaTime, 0.05f);
			}

			[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
			public static void System_ApplyVents(ISystem.Info info, ref Region.Data region, Entity entity, ref XorRandom random,
			[Source.Owned] in Transform.Data transform, [Source.Owned] ref Air.Container.Data air_container,
			[Source.Owned, Pair.Tag("intake")] ref Air.Vent.Data vent_intake,
			[Source.Owned, Pair.Tag("exhaust")] ref Air.Vent.Data vent_exhaust,
			[Source.Owned] ref Air.Compressor.Data compressor)
			{
#if DEBUG
				if (Air.Container.Data.dev_is_debug)
				{
					if (Air.Container.Data.time_step > 0 && (region.GetCurrentTick() % Air.Container.Data.time_step) != 0) return;
				}
#endif

				float mult, mult_inv;

				var delta_p_target = Maths.Lerp(compressor.velocity_min, compressor.velocity_max, compressor.modifier_current); // * compressor.velocity_mult;
																																//compressor.velocity_mult = delta_p_target;

				if (delta_p_target > 0.00f)
				{
					mult = 1.00f + delta_p_target.Abs();
					mult_inv = mult.Rcp();
				}
				else
				{
					mult_inv = 1.00f + delta_p_target.Abs();
					mult = mult_inv.Rcp();
				}

				//mult = compressor.modifier_current * compressor.velocity_mult;
				//if (mult > 0.00f)
				//{
				//	mult_inv = mult.Rcp();

				//	vent_exhaust.pressure_inside *= mult;
				//	vent_intake.pressure_inside *= mult_inv;

				//	vent_exhaust.density_inside *= mult;
				//	vent_intake.density_inside *= mult_inv;
				//}

				if (mult != 0)
				{
					vent_exhaust.pressure_inside *= mult;
					vent_intake.pressure_inside *= mult_inv;

					vent_exhaust.density_inside *= mult;
					vent_intake.density_inside *= mult_inv;
				}
			}

			// TODO: make different essences apply special effects (e.g. heat essence would also heat up the fluid passing through compressor)
			[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region)]
			public static void System_UpdateEssence(ISystem.Info info,
			[Source.Owned] ref Air.Container.Data air_container,
			[Source.Owned] ref Essence.Container.Data essence_container,
			[Source.Owned] ref Air.Compressor.Data compressor)
			{
				//essence_container.rate = compressor.modifier_current;
				essence_container.rate = compressor.modifier_current;

				ref var essence_data = ref essence_container.h_essence.GetData();
				if (essence_data.IsNotNull())
				{
					var essence_amount = essence_container.available * essence_container.rate_current;
					var force = essence_data.emit_force * essence_amount;

					compressor.velocity_mult = essence_container.available * 0.10f;
				}
				else
				{
					compressor.velocity_mult = 0.00f;
				}
			}
		}
	}
}
