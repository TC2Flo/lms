using Keg.Extensions;
using TC2.Base.Components;

namespace TC2.Siege
{
	public static partial class Capturable
	{
		[Flags]
		public enum Flags: ushort
		{
			None = 0,

			Capture_On_Wrecked = 1 << 0
		}

		// TODO: sucks
		[Dumb]
		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Global | IComponent.Scope.Region)]
		public partial struct Data: IComponent
		{
			public IFaction.Handle h_faction_capturing;
			public Capturable.Flags flags;

			//public float max_distance = 10.00f;
			//public float amount = 1000.00f;
			//public float difficulty = 10.00f;

			//public float defender_modifier = 1.00f;
			//public float attacker_modifier = 1.00f;

			[Net.Ignore, Save.Ignore]
			public float next_check;
		}
	}

	public static partial class Siege
	{
		public static partial class Target
		{
			[Flags]
			public enum Flags: uint
			{
				None = 0u,

				Captured = 1 << 0,
				Ruined = 1 << 1,
				Looted = 1 << 2,
				Locked = 1 << 3,

				Repairing = 1 << 16,
				Resupplying = 1 << 17,
				Reinforcing = 1 << 18,
			}

			public const float attack_notification_cooldown = 2.00f;

			[IComponent.Data(Net.SendType.Unreliable)]
			public partial struct Data(): IComponent
			{
				public Siege.Target.Flags flags;

				[Save.NewLine]
				public float combat_intensity_decay = 0.90f;
				public float combat_intensity_damage_mult = 0.50f;
				[Asset.Ignore] public float combat_intensity_current;

				[Asset.Ignore] public float current_capture_progress_norm;

				//[Save.Ignore, Net.Ignore] public float current_capture_progress_norm;
				//[Save.Ignore, Net.Ignore] public float current_capture_progress;
				//[Save.Ignore, Net.Ignore] public float last_capture_progress;

				[Save.Ignore, Net.Ignore] public float t_last_damage;
				[Save.Ignore, Net.Ignore] public float t_next_notification;


				[Shitcode]
				[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region, interval: 0.20f)]
				public static void OnUpdateCapturable(/*ISystem.Info info, ref Region.Data region, Entity entity,*/
				[Source.Owned] ref Capturable.Data capturable, [Source.Owned] ref Faction.Data faction,
				[Source.Owned] ref Siege.Target.Data siege_target,
				[Source.Owned] in Health.Data health, [Source.Owned] in Damage.Effects.Data damage_effects,
				[Source.Global] in Siege.Gamemode g_siege, [Source.Singleton] in Siege.Gamemode.Singleton g_siege_state)
				{
					if (capturable.flags.HasNone(Capturable.Flags.Capture_On_Wrecked)) return;

					siege_target.current_capture_progress_norm = Maths.InvLerp01(0.98f, damage_effects.threshold_wreck, health.GetHealthNormalized());
				}

#if SERVER
				[ISystem.Event<Health.PostDamageEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
				public static void OnPostDamage(ISystem.Info info, ref Region.Data region, Entity entity, [Source.Owned] ref Health.PostDamageEvent ev,
				[Source.Owned] ref Health.Data health, [Source.Owned] ref Siege.Target.Data siege_target, [Source.Owned, Optional] in Faction.Data faction)
				{

					if (siege_target.flags.HasAny(Siege.Target.Flags.Captured | Siege.Target.Flags.Ruined)) return;

					//if (ev.damage.faction_id == 0 || ev.damage.faction_id != faction.id)
					if (ev.damage.faction_id.CanAttack(faction.id))
					{
						if (info.WorldTime >= siege_target.t_next_notification)
						{
							siege_target.t_next_notification = info.WorldTime + Siege.Target.attack_notification_cooldown;

							Notification.Push(region: ref region,
								message: $"{entity.GetFullName()} is under attack! ({(siege_target.current_capture_progress_norm * 100.00f):0}% captured)",
								color: Color32BGRA.Yellow,
								lifetime: 7.00f,
								sound: "effect.buzzer.02",
								volume: 0.20f,
								pitch: 0.90f,
								faction_id: faction.id);
						}
					}
				}

				[Dumb]
				[ISystem.Add(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("wrecked", true, Source.Modifier.Owned)]
				public static void OnAddWrecked(ISystem.Info info, ref Region.Data region, Entity entity,
				[Source.Owned] ref Capturable.Data capturable, [Source.Owned] ref Faction.Data faction,
				[Source.Owned] ref Siege.Target.Data siege_target,
				[Source.Global] in Siege.Gamemode g_siege, [Source.Singleton] in Siege.Gamemode.Singleton g_siege_state)
				{
					if (capturable.flags.HasNone(Capturable.Flags.Capture_On_Wrecked)) return;

					siege_target.flags.AddFlag(Siege.Target.Flags.Captured);
					siege_target.Sync(entity);

					entity.SetFaction(capturable.h_faction_capturing);
					Notification.Push(region: ref region,
					   message: $"{entity.GetFullName()} has been captured!",
					   color: Color32BGRA.Red,
					   lifetime: 10.00f,
					   sound: "effect.buzzer.05.3x",
					   volume: 0.60f,
					   pitch: 1.00f,
					   send_type: Net.SendType.Reliable);
				}

			

				//[TODO("Implement"), NotImplemented]
				//[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region, interval: 1.00f)]
				//public static void OnUpdateCapturable(ISystem.Info info, ref Region.Data region, Entity entity,
				//[Source.Owned] ref Capturable.Data capturable, [Source.Owned] ref Faction.Data faction, 
				//[Source.Owned] ref Siege.Target.Data siege_target, 
				//[Source.Global] in Siege.Gamemode g_siege, [Source.Singleton] in Siege.Gamemode.Singleton g_siege_state)
				//{
				//	//ref var work = ref capturable.order.work[0];
				//	//siege_target.current_capture_progress = work.current;
				//	//siege_target.current_capture_progress_norm = Maths.NormalizeClamp(work.current, work.required);

				//	//if (info.WorldTime >= siege_target.t_next_notification_capture)
				//	//{
				//	//	if (siege_target.t_next_notification_capture > 0.00f)
				//	//	{
				//	//		var delta = siege_target.current_capture_progress - siege_target.last_capture_progress;
				//	//		if (MathF.Abs(delta) >= 10.00f)
				//	//		{
				//	//			siege_target.last_capture_progress = siege_target.current_capture_progress;

				//	//			if (float.IsNegative(delta))
				//	//			{
				//	//				Notification.Push(ref region, $"{entity.GetFullName()} is being captured! ({(Maths.NormalizeClamp(work.current, work.required) * 100.00f):0}%)", Color32BGRA.Yellow, lifetime: 5.00f, "buzzer.02", volume: 0.20f, pitch: 0.90f);
				//	//			}
				//	//		}
				//	//	}

				//	//	siege_target.t_next_notification_capture = info.WorldTime + 2.00f;
				//	//}
				//}

				[ISystem.Remove(ISystem.Mode.Single, ISystem.Scope.Region)]
				public static void OnRemove(ISystem.Info info, ref Region.Data region, Entity entity, [Source.Owned] ref Siege.Target.Data siege_target)
				{
					//Notification.Push(ref region, $"{entity.GetFullName()} has been destroyed!", Color32BGRA.Red, lifetime: 10.00f, "effect.buzzer.02", volume: 0.20f, pitch: 0.80f, send_type: Net.SendType.Reliable);
					Notification.Push(region: ref region,
					   message: $"{entity.GetFullName()} has been destroyed!",
					   color: Color32BGRA.Red,
					   lifetime: 10.00f,
					   sound: "effect.buzzer.05.3x",
					   volume: 0.60f,
					   pitch: 1.00f,
					   send_type: Net.SendType.Reliable);
				}
#endif
			}
		}
	}
}
