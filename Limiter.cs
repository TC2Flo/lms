
namespace TC2.Base.Components
{
	public static partial class Limiter
	{
		[IComponent.Data(Net.SendType.Unreliable, region_only: true)]
		public partial struct Data: IComponent
		{
			public float torque;
			public float angular_velocity;

			[Save.Ignore, Net.Ignore] public float t_next_switch;
		}

		public struct ConfigureRPC: Net.IRPC<Limiter.Data>
		{
			public float? torque;
			public float? angular_velocity;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Limiter.Data data)
			{
				ref var region = ref rpc.entity.GetRegion();
				if (region.GetWorldTime() >= data.t_next_switch)
				{
					data.t_next_switch = region.GetWorldTime() + 0.10f;

					var sync = false;

					sync |= this.torque.TryGetValue(out var v_torque) && data.torque.TrySet(v_torque);
					sync |= this.angular_velocity.TryGetValue(out var v_angular_velocity) && data.angular_velocity.TrySet(v_angular_velocity);

					//this.ratio = Maths.Clamp(this.ratio, data.ratio_min, data.ratio_max);
					//if (data.ratio.TrySet(this.ratio))
					//{
					//	sync = true;
					//}

					if (sync)
					{
						data.Sync(rpc.entity);
					}
				}
			}
#endif
		}

		[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void Update(ISystem.Info info, Entity entity,
		[Source.Owned] ref Axle.Data axle, [Source.Owned] ref Axle.State axle_state,
		[Source.Owned] in Transform.Data transform, [Source.Owned] ref Limiter.Data limiter)
		{
			//axle_state.new_tmp_torque = Maths.Min(axle_state.new_tmp_torque, limiter.torque);
			//axle_state.new_tmp_torque_angular_velocity = Maths.Min(axle_state.new_tmp_torque_angular_velocity, limiter.angular_velocity);
		}

		[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void UpdateTestA(ISystem.Info info, Entity entity,
		[Source.Owned] ref Axle.Data axle, [Source.Owned] ref Axle.State axle_state,
		[Source.Owned] in Transform.Data transform, [Source.Owned, Pair.Tag("derp")] ref Limiter.Data limiter)
		{
			App.WriteLine("doot");
		}

#if CLIENT
		public partial struct LimiterGUI: IGUICommand
		{
			public Entity ent_limiter;
			public Limiter.Data limiter;
			public Axle.Data axle;
			public Axle.State axle_state;

			public void Draw()
			{
				using (var window = GUI.Window.InteractionMisc("Limiter"u8, this.ent_limiter, new Vector2(128, 96)))
				{
					//this.StoreCurrentWindowTypeID();
					if (window.show)
					{
						var rpc = new Limiter.ConfigureRPC();
						var dirty = false;

						if (GUI.SliderFloat("Torque", ref this.limiter.torque, 0.00f, 10000.00f, size: new Vector2(GUI.RmX, 32), snap: 1.00f))
						{
							dirty = true;
							rpc.torque = this.limiter.torque;
						}

						if (GUI.SliderFloat("Angular Velocity", ref this.limiter.angular_velocity, -30.00f, 30.00f, size: new Vector2(GUI.RmX, 32), snap: 1.00f))
						{
							dirty = true;
							rpc.angular_velocity = this.limiter.angular_velocity;
						}

						if (dirty)
						{
							rpc.Send(this.ent_limiter);
						}
					}
				}
			}
		}

		[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnGUI(Entity entity, [Source.Owned] in Limiter.Data limiter,
		[Source.Owned] in Axle.Data axle, [Source.Owned] in Axle.State axle_state, [Source.Owned] in Interactable.Data interactable)
		{
			if (interactable.IsActive())
			{
				var gui = new LimiterGUI()
				{
					ent_limiter = entity,
					limiter = limiter,
					axle = axle,
					axle_state = axle_state
				};
				gui.Submit();
			}
		}
#endif
	}
}
