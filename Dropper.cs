namespace TC2.Base.Components
{
	public static partial class Dropper
	{
		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Region)]
		public struct Data(): IComponent
		{
			[Flags]
			public enum Flags: uint
			{
				None = 0u,

				Continuous = 1u << 0,
				Active = 1u << 1,
			}

			[Editor.Slider.Clamped(0.00f, 15.00f, 0.01f)]
			public float batch_interval = 1.00f;
			[Editor.Slider.Clamped(0.00f, 500.00f, 1.00f)]
			public float batch_amount = 1.00f;

			public Dropper.Data.Flags flags;

			public Sound.Handle sound_enable;
			public Sound.Handle sound_disable;
			public Sound.Handle sound_drop;

			[Editor.Picker.Position(relative: true)] public Vector2 offset;
			[Editor.Picker.Direction(normalize: true)] public Vector2 dir;

			//public InvRef ref_inventory;

			[Save.Ignore, Net.Ignore] public float t_next_use;
		}

		[IEvent.Data]
		public partial struct DropEvent(): IEvent
		{
			public Entity ent_dropper;
			public Vector2 world_position;
		}

		[ISystem.Event<Dropper.DropEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnDropEvent(ref XorRandom random, ref Region.Data region, ISystem.Info info, Entity entity, ref Dropper.DropEvent data,
		[Source.Owned] ref Dropper.Data dropper, [Source.Owned] ref Transform.Data transform, [Source.Owned] ref Body.Data body, 
		[Source.Owned, Pair.Component<Dropper.Data>] ref Shipment.Data shipment)
		{
			if (shipment.IsValid())
			{
				var pos = transform.LocalToWorld(dropper.offset);
				var items_copy = shipment.items;

				var vel = body.GetVelocity();

#if CLIENT
				Sound.Play(dropper.sound_drop, pos);
#endif

#if SERVER
				if (shipment.flags.HasAny(Shipment.Flags.Unpack))
				{
					foreach (ref var item in items_copy)
					{
						if (item.IsValid())
						{
							if (item.TrySpawn(ref region, pos + random.NextVector2Range(-0.25f, 0.25f), item.quantity, rotation: transform.rotation + random.NextFloatRange(-0.20f, 0.20f), velocity: vel + random.NextUnitVector2Range(0.10f, 0.50f), angular_velocity: random.NextFloatRange(-1.00f, 1.00f)))
							{

							}
						}
					}
				}
				else
				{
					Shipment.Spawn(ref region, pos + random.NextVector2Range(-0.25f, 0.25f), items: items_copy, h_container: shipment.h_container, flags: shipment.flags); //, item.quantity, rotation: transform.rotation + random.NextFloatRange(-0.20f, 0.20f), velocity: vel + random.NextUnitVector2Range(0.10f, 0.50f), angular_velocity: random.NextFloatRange(-1.00f, 1.00f)))
				}

				shipment.count--;
				if (shipment.count == 0)
				{
					shipment = default;
				}
				shipment.Sync<Shipment.Data, Dropper.Data>(entity);
#endif
			}
		}

		[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnUpdate(ISystem.Info info, Entity entity,
		[Source.Owned] in Control.Data control, [Source.Owned] in Transform.Data transform, [Source.Owned] ref Dropper.Data dropper, 
		[Source.Owned, Pair.Component<Dropper.Data>] ref Shipment.Data shipment)
		{
#if SERVER
			if (control.mouse.GetKeyDown(Mouse.Key.Left) && info.WorldTime >= dropper.t_next_use && shipment.IsValid())
			{
				var ev = new Dropper.DropEvent();
				ev.ent_dropper = entity;
				ev.world_position = transform.position;
				entity.TriggerEventDeferred(ev, sync: true);

				dropper.t_next_use = info.WorldTime + dropper.batch_interval;
			}
#endif
		}
	}
}