//namespace TC2.Base.Components
//{
//	public static partial class Dodo
//	{
//		[IComponent.Data(Net.SendType.Reliable, region_only: true)]
//		public partial struct Data: IComponent
//		{
//			[Save.Ignore, Net.Ignore] public float t_next_sound;
//		}

//		//// Hack, used to register "dodo" tag for now
//		//[ISystem.EarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("dodo", true, Source.Modifier.Owned)]
//		//public static void Update(ISystem.Info info, [Source.Owned] ref Dodo.Data dodo)
//		//{

//		//}

//		public static Sound.Handle[] sounds_idle =
//		{
//			"dodo.squawk.00",
//			"dodo.squawk.01",
//			"dodo.squawk.02",
//			"dodo.squawk.03"
//		};

//		public static Sound.Handle[] sounds_pain =
//		{
//			"dodo.pain.00",
//			"dodo.pain.01"
//		};

//#if SERVER
//		[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("dodo", true, Source.Modifier.Parent), HasTag("dead", false, Source.Modifier.Owned)]
//		public static void Update(ISystem.Info info, ref Region.Data region, ref XorRandom random, [Source.Owned] in Transform.Data transform, [Source.Parent] ref Dodo.Data dodo, [Source.Owned] in Head.Data head, [Source.Parent] ref Control.Data control)
//		{
//			if (info.WorldTime >= dodo.t_next_sound)
//			{
//				dodo.t_next_sound = info.WorldTime + random.NextFloatRange(2.00f, 4.00f);

//				control.keyboard.SetKeyPressed(Keyboard.Key.MoveUp, true);
//				control.keyboard.SetKeyPressed(random.NextBool(0.50f) ? Keyboard.Key.MoveLeft : Keyboard.Key.MoveRight, true);
//				Sound.Play(ref region, Dodo.sounds_idle.GetRandom(ref random), transform.position, volume: random.NextFloatRange(0.65f, 0.70f), pitch: random.NextFloatRange(0.95f, 1.15f), size: 0.80f, dist_multiplier: 1.00f);
			
			
//			}
//		}
//#endif
//	}
//}
