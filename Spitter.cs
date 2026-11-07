
namespace TC2.Base.Components
{
	public static partial class Spitter
	{
		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Region), IComponent.With<Spitter.State>()]
		public partial struct Data(): IComponent
		{
			public Sound.Handle sound;

			public float cooldown = 0.50f;
			public float max_distance = 1.00f;

			public float jitter_multiplier;
			public float velocity_multiplier;

			[Editor.Picker.Position(true, true)]
			public Vector2 offset;

			public Prefab.Handle prefab_projectile;
		}

		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public partial struct State: IComponent
		{
			[Net.Ignore, Save.Ignore] public float next_attack;
		}

		[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("dead", false, Source.Modifier.Owned)]
		public static void Update(ISystem.Info info, Entity entity, ref Region.Data region, ref XorRandom random,
		[Source.Owned] in Spitter.Data spitter, [Source.Owned] ref Spitter.State spitter_state, [Source.Owned, Override] in Organic.Data organic, [Source.Owned] in Organic.State organic_state,
		[Source.Owned] ref Body.Data body, [Source.Owned] in Control.Data control, [Source.Owned] in Transform.Data transform)
		{
			if (organic_state.consciousness_shared > 0.30f && info.WorldTime > spitter_state.next_attack && control.mouse.GetKey(Mouse.Key.Left))
			{
				spitter_state.next_attack = info.WorldTime + spitter.cooldown;

				var pos_w_offset = transform.LocalToWorld(spitter.offset);
				//var dir = transform.GetDirection();
				var base_vel = body.GetVelocity();

				var dir = (control.mouse.position - transform.position).GetNormalized(out var len);
				len = Maths.Min(len, spitter.max_distance);

				//body.AddForce(dir * body.GetMass() * App.tickrate * spitter.velocity);

				var velocity_jitter = Maths.Clamp(spitter.jitter_multiplier * 0.20f, 0.00f, 1.00f) * 0.50f;
				var angle_jitter = Maths.Clamp(spitter.jitter_multiplier, 0.00f, 25.00f);
				var random_multiplier = 1.00f + random.NextFloat(velocity_jitter);

#if SERVER
				Sound.Play(ref region, spitter.sound, pos_w_offset);

				var args =
				(
					damage_mult: 1.00f,
					vel: dir.RotateByDeg(random.NextFloat(angle_jitter * 0.50f)) * spitter.velocity_multiplier * random_multiplier,
					ent_owner: entity,
					ent_spitter: entity,
					faction_id: body.GetFaction()
				);

				region.SpawnPrefab(spitter.prefab_projectile, pos_w_offset, rotation: args.vel.GetAngleRadians(), velocity: args.vel, angular_velocity: dir.GetParity()).ContinueWith(ent =>
				{
					ref var projectile = ref ent.GetComponent<Projectile.Data>();
					if (!projectile.IsNull())
					{
						projectile.damage_base *= args.damage_mult;
						projectile.damage_bonus *= args.damage_mult;
						projectile.velocity = args.vel;
						projectile.ent_owner = args.ent_owner;
						projectile.faction_id = args.faction_id;
						projectile.Sync(ent, true);
					}

					//ref var explosive = ref ent.GetComponent<Explosive.Data>();
					//if (!explosive.IsNull())
					//{
					//	explosive.ent_owner = args.ent_owner;
					//	explosive.Sync(ent, true);
					//}

					if (true)
					{
						var ent_child = args.ent_spitter.GetChild(Relation.Type.Rope);
						if (ent_child.IsAlive())
						{
							ent_child.RemoveRelation(entity, Relation.Type.Rope);
							ent_child.Delete();
						}
						ent.AddRelation(args.ent_spitter, Relation.Type.Rope);
					}

					//if (args.gun_flags.HasAll(Gun.Flags.Child_Projectiles))
					//{
					//	ent.AddRelation(args.ent_gun, Relation.Type.Child);
					//}
				});
#endif
			}
		}
	}
}
