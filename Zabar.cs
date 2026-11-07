namespace TC2.Base.Components
{
	public static partial class Zabar
	{
		[IComponent.Data(Net.SendType.Reliable, region_only: true)]
		public partial struct Data: IComponent
		{
			public float test;
		}

		// Hack, used to register "zabar" tag for now
		[ISystem.EarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("zabar", true, Source.Modifier.Owned)]
		public static void UpdateZabar(ISystem.Info info, [Source.Owned] ref Zabar.Data zabar)
		{

		}
	}
}
