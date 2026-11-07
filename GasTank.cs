
namespace TC2.Base.Components
{
	public static partial class GasTank
	{
		[IComponent.Data(Net.SendType.Reliable, region_only: true)]
		public struct Data(): IComponent
		{
			public float tank_volume = 1.00f;
		}
	}
}
