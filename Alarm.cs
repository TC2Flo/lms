using System.Diagnostics.CodeAnalysis;
using TC2.Conquest;

namespace TC2.Base.Components
{
	public static partial class Alarm
	{
		[Flags]
		public enum Flags: uint
		{
			None = 0u,

			Active = 1u << 0,
			//Timeout = 1u << 1
		}

		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Global | IComponent.Scope.Region)]
		public struct Data(): IComponent
		{
			public Alarm.Flags flags;

			public Sound.Handle h_sound_start;
			public Sound.Handle h_sound_stop;

			public float sound_volume = 0.50f;

			public float rotation_speed = 0.10f;
			public float time_rem;
		}

#if SERVER
		[ISystem.VeryEarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("initialized", true, Source.Modifier.Owned)]
		public static void OnUpdate(ISystem.Info info, ref Region.Data region,
		[Source.Owned] in Transform.Data transform,
		[Source.Owned] ref Alarm.Data alarm, Entity ent_alarm)
		{
			//if (alarm.flags.HasAny(Alarm.Flags.Active))

			if (alarm.time_rem > 0.00f)
			{
				if (alarm.flags.TrySetFlag(Alarm.Flags.Active, (alarm.time_rem -= info.DeltaTime) > 0.00f, out var flags_old))
				{
					Sound.Play(region: ref region, sound: flags_old.HasAny(Alarm.Flags.Active) ? alarm.h_sound_stop : alarm.h_sound_start,
						world_position: transform.position,
						volume: alarm.sound_volume);

					alarm.Sync(ent_alarm);
				}

				//if (alarm.flags.TrySetFlag(Alarm.Flags.Active, alarm.time_rem > 0.00f, out var flags_old))
				//{

				//}
			}
		}
#endif

		[ISystem.VeryLateUpdate(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("initialized", true, Source.Modifier.Owned)]
		public static void OnUpdateEffects(ISystem.Info info, ref Region.Data region,
		[Source.Owned] in Transform.Data transform,
		[Source.Owned] ref Alarm.Data alarm,
		[Source.Owned, Pair.Component<Alarm.Data>, Optional(true)] ref Sound.Emitter sound_emitter,
		[Source.Owned, Pair.Component<Alarm.Data>, Optional(true)] ref Light.Data light)
		{
			var is_active = alarm.flags.HasAny(Alarm.Flags.Active);

			if (sound_emitter.IsNotNull())
			{
				sound_emitter.volume_mult = is_active ? 1.00f : 0.00f;
			}

			if (light.IsNotNull())
			{
				light.intensity = is_active ? 1.00f : 0.00f;
				if (is_active)
				{
					light.rotation += info.DeltaTime * alarm.rotation_speed;
					light.rotation = light.rotation.NormalizeAngle();
				}
			}
		}

#if SERVER
		[ISystem.Event<Entrance.EnterEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnCheckpointEnterEvent(ref XorRandom random, ref Region.Data region, ISystem.Info info, Entity ent_alarm,
		[Source.Shared] ref Entrance.EnterEvent data/*, [Source.Owned] in Checkpoint.Data checkpoint*/, [Source.Owned] ref Alarm.Data alarm)
		{
			if (data.flags.HasAny(Entrance.EnterEvent.Flags.Spawn))
			{
				alarm.time_rem = 30.00f;

				//if (alarm.flags.TryAddFlag(Alarm.Flags.Active))
				//{
				//	alarm.Sync(ent_alarm);
				//}
			}
		}
#endif
	}
}
