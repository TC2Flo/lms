
namespace TC2.Base.Components
{
	public static partial class Wheel
	{
		[Flags]
		public enum Flags: uint
		{
			None = 0,

			[Save.Ignore, Net.Ignore] Has_Wheel = 1 << 0,
			Front = 1 << 1,
			Rear = 1 << 2,
		}

		[IComponent.Data(Net.SendType.Reliable, region_only: true)]
		public partial struct Slot(): IComponent
		{
			public float force_multiplier = 10.00f;
			public float ratio = 0.25f;
			public Wheel.Flags flags;
			[Net.Ignore, Asset.Ignore, Save.Ignore] private uint unused_00;
		}

		[IComponent.Data(Net.SendType.Reliable, region_only: true)]
		public partial struct Data: IComponent
		{
			public float test;
		}

		[ISystem.Add(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnAdd(ISystem.Info info, [Source.Owned] ref Wheel.Data wheel, [Source.Parent] ref Wheel.Slot wheel_slot)
		{
			wheel_slot.flags.AddFlag(Wheel.Flags.Has_Wheel);
		}

		[ISystem.Remove(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnRemove(ISystem.Info info, [Source.Owned] ref Wheel.Data wheel, [Source.Parent] ref Wheel.Slot wheel_slot)
		{
			wheel_slot.flags.RemoveFlag(Wheel.Flags.Has_Wheel);
		}
	}
}
