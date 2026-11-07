
namespace TC2.Base.Components
{
	public static partial class Building
	{
		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public partial struct Data: IComponent
		{
			[Flags]
			public enum Flags: ushort
			{
				None = 0,

				[Save.Ignore, Asset.Ignore]
				Cached = 1 << 0
			}

			[Save.Force, Editor.Slider.Clamped(-16, 16, snap: 0.125f, mark_modified: true), Editor.Picker.Position(relative: true, mark_modified: true)] public AABB rect_foundation;

			[Save.NewLine]
			[Save.Force] public Building.Data.Flags flags;
			[Save.Force] public TileFlags tile_flags;
			private byte unused_a;

			[Save.NewLine]
			public float stability;
			[Editor.Picker.Position(relative: true, mark_modified: true)] private Vector2 unused_b;
		}
	}
}
