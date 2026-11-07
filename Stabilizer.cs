
namespace TC2.Base.Components
{
	public static partial class Stabilizer
	{
		[IComponent.Data(Net.SendType.Reliable, region_only: true)]
		public partial struct Data: IComponent
		{
			[Editor.Picker.Position(true)] public Vector2 offset;
		}
	}
}
