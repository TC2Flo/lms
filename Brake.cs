
namespace TC2.Base.Components
{
	public static partial class Brake
	{
		[IComponent.Data(Net.SendType.Unreliable, region_only: true)]
		public partial struct Data(): IComponent
		{
			public float ratio;

			public float ratio_min = 1.00f;
			public float ratio_max = 8.00f;

			[Save.Ignore, Net.Ignore] public float t_next_switch;
		}

		public struct ConfigureRPC: Net.IRPC<Brake.Data>
		{
			public float ratio;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Brake.Data data)
			{
				ref var region = ref rpc.entity.GetRegion();
				if (region.GetWorldTime() >= data.t_next_switch)
				{
					data.t_next_switch = region.GetWorldTime() + 0.10f;

					var sync = false;

					this.ratio = Maths.Clamp(this.ratio, data.ratio_min, data.ratio_max);
					if (data.ratio.TrySet(this.ratio))
					{
						sync = true;
					}

					if (sync)
					{
						data.Sync(rpc.entity);
					}
				}
			}
#endif
		}

		[ISystem.EarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void Update(ISystem.Info info, Entity entity,
		[Source.Owned] ref Axle.Data axle, [Source.Owned] ref Axle.State axle_state,
		[Source.Owned] in Transform.Data transform, [Source.Owned] ref Brake.Data brake)
		{
			axle.ratio = brake.ratio;
		}

#if CLIENT
		public partial struct BrakeGUI: IGUICommand
		{
			public Entity ent_brake;
			public Brake.Data brake;
			public Axle.Data axle;
			public Axle.State axle_state;

			public void Draw()
			{
				using (var window = GUI.Window.InteractionMisc("Brake"u8, this.ent_brake, new Vector2(128, 48)))
				{
					//this.StoreCurrentWindowTypeID();
					if (window.show)
					{
						if (GUI.SliderFloat("Ratio", ref this.brake.ratio, this.brake.ratio_min, this.brake.ratio_max, size: new Vector2(GUI.RmX, GUI.RmY), snap: 1.00f))
						{
							var rpc = new Brake.ConfigureRPC()
							{
								ratio = this.brake.ratio
							};
							rpc.Send(this.ent_brake);
						}
					}
				}
			}
		}

		[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnGUI(Entity entity, [Source.Owned] in Brake.Data brake,
		[Source.Owned] in Axle.Data axle, [Source.Owned] in Axle.State axle_state, [Source.Owned] in Interactable.Data interactable)
		{
			if (interactable.IsActive())
			{
				var gui = new BrakeGUI()
				{
					ent_brake = entity,
					brake = brake,
					axle = axle,
					axle_state = axle_state
				};
				gui.Submit();
			}
		}
#endif
	}
}
