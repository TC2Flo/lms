namespace TC2.Base.Components
{
	public static partial class Capybara
	{
		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public partial struct Data: IComponent
		{
			public float test;

			[Save.Ignore, Net.Ignore] public float next_sound;
		}

		public static Sound.Handle[] sounds_idle =
		{
			"capybara.idle.00",
			"capybara.idle.01",
			"capybara.idle.02",
			"capybara.idle.03",
			"capybara.idle.04"
		};

#if SERVER
		[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region, interval: 0.20f), HasTag("capybara", true, Source.Modifier.Parent), HasTag("dead", false, Source.Modifier.Owned)]
		public static void UpdateCapybara(ISystem.Info info, ref XorRandom random, ref Region.Data region, [Source.Owned] in Transform.Data transform, [Source.Parent] ref Capybara.Data capybara, [Source.Owned] in Head.Data head)
		{
			if (info.WorldTime >= capybara.next_sound)
			{
				Sound.Play(ref region, sounds_idle.GetRandom(ref random), transform.position, volume: random.NextFloatRange(0.30f, 0.40f), pitch: random.NextFloatRange(0.70f, 0.85f), size: 1.10f, dist_multiplier: 1.00f);
				capybara.next_sound = info.WorldTime + random.NextFloatRange(3.00f, 10.00f);
			}
		}
#endif
	}
}
