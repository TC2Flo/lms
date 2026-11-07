
namespace TC2.Base.Components
{
	public static partial class Admin
	{
		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public partial struct Data(): IComponent
		{
			public float test = 10.00f;
		}

		//#if SERVER
		//		[ISystem.Add<Admin.Data>(ISystem.Mode.Single, ISystem.Scope.Region, order: 10000)]
		//		public static void OnAdd(ISystem.Info info, Entity entity,
		//		[Source.Owned] ref Admin.Data admin, [Source.Owned] in Transform.Data transform)
		//		{
		//			ref var region = ref info.GetRegion();

		//			Sound.Play(ref region, "grandpa.summon.00", transform.position, volume: 1.00f, size: 4.00f, priority: 1.00f);
		//			Shake.Emit(ref region, transform.position, 1.00f, 1.00f, 64.00f);
		//		}
		//#endif

#if CLIENT
		[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnUpdate(ISystem.Info info, Entity entity,
		[Source.Owned] ref Telescope.Data telescope, [Source.Owned] in Transform.Data transform, [Source.Owned] in Control.Data control, [Source.Owned] in Admin.Data admin,
		[Source.Shared] in Player.Data player, [Source.Singleton] ref Camera.Singleton camera)
		{
			Telescope.OnUpdate(info, entity, ref telescope, in transform, in control, default, in player, ref camera);
		}

		[ISystem.Remove(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnRemove(ISystem.Info info, Entity entity,
		[Source.Owned] ref Telescope.Data telescope, [Source.Owned] in Transform.Data transform, [Source.Owned] in Control.Data control, [Source.Owned] in Admin.Data admin,
		[Source.Shared] in Player.Data player)
		{
			Telescope.OnRemove(info, entity, ref telescope, in transform, in control, default, in player);
		}
#endif
	}
}
