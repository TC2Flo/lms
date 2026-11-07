namespace TC2.Base.Components
{
	public static class Lockpick
	{
		[IComponent.Data(Net.SendType.Unreliable, region_only: true)]
		public struct Data: IComponent
		{
			[Save.Ignore, Net.Ignore] public float t_next_use;
		}

#if CLIENT
		public struct LockpickGUI: IGUICommand
		{
			public Transform.Data transform;
			public Control.Data control;
			public Lockpick.Data lockpick;
			public Entity entity;

			public void Draw()
			{

			}
		}

		[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("local", true, Source.Modifier.Parent)]
		public static void OnGUI(ISystem.Info info, Entity entity,
		[Source.Parent] in Interactor.Data interactor, [Source.Owned] ref Lockpick.Data lockpick, [Source.Owned] in Transform.Data transform, [Source.Owned] in Control.Data control)
		{
			var gui = new LockpickGUI()
			{
				entity = entity,
				transform = transform,
				lockpick = lockpick,
				control = control
			};
			gui.Submit();
		}
#endif

#if SERVER
		[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region), HasRelation(Source.Modifier.Owned, Relation.Type.Stored, false)]
		public static void Update(ISystem.Info info, Entity entity, ref XorRandom random,
		[Source.Owned] ref Lockpick.Data lockpick, [Source.Owned] in Transform.Data transform, [Source.Owned] in Control.Data control, [Source.Owned] in Body.Data body)
		{
			if (control.mouse.GetKey(Mouse.Key.Left) && info.WorldTime >= lockpick.t_next_use)
			{

			}
		}
#endif
	}
}
