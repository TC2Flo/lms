
namespace TC2.Base.Components
{
	public static partial class Claw
	{
		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Region)]
		public partial struct Data: IComponent
		{
			public Sound.Handle sound_attach;
			public Sound.Handle sound_detach;

			public byte frame_attached;
			public byte frame_detached;

			private byte unused_00;
			private byte unused_01;
		}

		[ISystem.Add(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("initialized", true, Source.Modifier.Parent)]
		public static void OnAdd(ISystem.Info info, ref Region.Data region, Entity entity, [Source.Owned] in Body.Data body_child,
		[Source.Parent] in Transform.Data transform, [Source.Parent] in Claw.Data claw, [Source.Parent] in Joint.Base joint_base, [Source.Parent] ref Animated.Renderer.Data renderer)
		{
			renderer.sprite.frame.x = claw.frame_attached;
			//App.WriteLine("add");

#if SERVER
			Sound.Play(ref region, claw.sound_attach, transform.position, volume: 1.00f, priority: 0.60f);
#endif
		}

		[ISystem.Remove(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnRemove(ISystem.Info info, Entity entity, ref Region.Data region, [Source.Owned] in Body.Data body_child,
		[Source.Parent] in Transform.Data transform, [Source.Parent] in Claw.Data claw, [Source.Parent] in Joint.Base joint_base, [Source.Parent] ref Animated.Renderer.Data renderer)
		{
			renderer.sprite.frame.x = claw.frame_detached;
			//App.WriteLine("rem");

#if SERVER
			Sound.Play(ref region, claw.sound_detach, transform.position, volume: 1.00f, priority: 0.60f);
#endif
		}
	}
}
