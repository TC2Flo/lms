
namespace TC2.Base.Components
{
	public static partial class Puncher
	{
		[Flags]
		public enum Flags: uint
		{
			None = 0,
			Can_Punch = 1u << 0
		}

		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Region)]
		public partial struct Data(): IComponent, IOverridable
		{
			public float damage_base;
			public float damage_bonus;

			[Save.NewLine]
			public float cooldown = 0.50f;
			public float max_distance = 1.00f;
			public float aoe = 0.50f;
			public float thickness = 0.05f;
			public float force = 500.00f;
			public float knockback = 3.00f;
			public float stun_multiplier = 1.00f;

			[Save.NewLine]
			public float penetration_falloff = 0.75f;
			public int penetration = 0;

			[Save.NewLine]
			public Vec2u32 frame;
			[Editor.Picker.Position(true)] public Vector2 offset;

			[Save.NewLine]
			public Sound.Handle sound;
			public Damage.Type damage_type = Damage.Type.Punch;
			public Physics.Layer hit_mask = Physics.Layer.Destructible | Physics.Layer.World;
			public Physics.Layer hit_exclude = Physics.Layer.Ignore_Melee | Physics.Layer.Essence | Physics.Layer.Water | Physics.Layer.Fire | Physics.Layer.Gas | Physics.Layer.Stored;
			public Physics.Layer hit_require;

			[Save.NewLine]
			public AddRemFlags<Damage.Flags> damage_flags;

			[Save.NewLine]
			[Net.Ignore, Save.Ignore] public Puncher.Flags flags;
			[Net.Ignore, Save.Ignore] public float last_update; // TODO: hack for NPCs to check if they can punch
			[Net.Ignore, Save.Ignore] public float last_attack;
			[Net.Ignore, Save.Ignore] public float next_attack;
		}

		//[ISystem.VeryEarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("dead", false, Source.Modifier.Shared), HasRelation(Source.Modifier.Shared, Relation.Type.Seat, false)]
		//public static void PreUpdate(ISystem.Info info, Entity ent_body, ref Region.Data region, ref XorRandom random,
		//[Source.Owned, Override] ref Puncher.Data puncher,
		//[Source.Shared, Override] in Organic.Data organic, [Source.Shared] ref Organic.State organic_state,
		//[Source.Shared] ref Body.Data body, [Source.Shared] in Transform.Data transform,
		//[Source.Owned] in Joint.Base joint_base, [Source.Shared, Optional(true)] ref Torso.Data torso)
		//{
		//	joint_base.state.HasAny(Joint.State.Attached)
		//}

		[Shitcode]
		[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		[HasTag("dead", false, Source.Modifier.Shared), HasRelation(Source.Modifier.Shared, Relation.Type.Seat, false)]
		public static void Update(ISystem.Info info, Entity ent_body, ref Region.Data region, ref XorRandom random,
		[Source.Owned, Original] ref Puncher.Data puncher_original, [Source.Owned, Override] ref Puncher.Data puncher_override,
		[Source.Shared, Override] in Organic.Data organic, [Source.Shared] ref Organic.State organic_state,
		[Source.Shared] ref Body.Data body, [Source.Shared] in Control.Data control, [Source.Shared] in Transform.Data transform,
		[Source.Owned] in Joint.Base joint_base, [Source.Shared, Optional(true)] ref Torso.Data torso)
		{
			//App.WriteLine(info.WorldTime);

			var time = info.WorldTime;
			var can_punch = organic_state.consciousness_shared > 0.50f && (organic_state.efficiency * (1.00f - organic_state.stun_norm)) > 0.50f && organic_state.pain_shared_old <= 1200.00f;
			puncher_original.flags.SetFlag(Puncher.Flags.Can_Punch, can_punch);
			puncher_original.last_update = time;

			if (can_punch && joint_base.ent_attached == default && time >= puncher_override.next_attack
			&& control.mouse.GetKey(Mouse.Key.Right) && !control.keyboard.GetKey(Keyboard.Key.LeftShift | Keyboard.Key.LeftControl)) // joint_base.state.HasNone(Joint.State.Attached | Joint.State.Attaching))
			{
				var dir = (control.mouse.position - transform.position).GetNormalized(out var len);
				if (len <= 5.00f)
				{
					len = Maths.Min(len, puncher_override.max_distance);

					puncher_original.last_attack = time;
					puncher_original.next_attack = time + Maths.Lerp01(puncher_override.cooldown * 2.00f, puncher_override.cooldown, organic_state.efficiency);

					var pos = transform.LocalToWorld(puncher_override.offset);

#if CLIENT
					Sound.Play(puncher_override.sound, pos, volume: 0.20f, pitch: random.NextFloatRange(0.65f, 0.80f), size: 1.00f);
#endif

					//body.AddForce(dir * body.GetMass() * App.tickrate * puncher.velocity);

					var hit_any = false;
					var hit_terrain = false;

					var h_faction = body.GetFaction();
					var results = FixedArray.CreateSpan16NoInit<LinecastResult>(out var results_buffer);

					if (region.TryLinecastAll(world_position_start: pos,
					world_position_end: pos + (dir * len),
					radius: puncher_override.thickness,
					hits: ref results,
					mask: puncher_override.hit_mask,
					exclude: puncher_override.hit_exclude,
					require: puncher_override.hit_require))
					{
						results.SortByDistance();

						var modifier = 1.00f;
						var damage_flags = Damage.Flags.No_Loot_Discard;
						damage_flags.AddRemFlags(puncher_override.damage_flags);

						var damage_mult = organic_state.efficiency * (organic.strength * 2.50f).SqrtToOne();

						var penetration = puncher_override.penetration;
						for (var i = 0; i < results.Length; i++)
						{
							ref var result = ref results[i];
							var ent_result = result.entity;

							var is_terrain = !ent_result.IsValid();
							if (is_terrain)
							{
								if (hit_terrain) continue;
								hit_terrain = true;
							}

							if (ent_result == ent_body || result.entity_parent == ent_body) continue;
							if (h_faction && result.GetFactionHandle() == h_faction) continue;

							hit_any = true;

#if CLIENT
							Shake.Emit(ref region, transform.position, 0.40f, 0.40f, radius: 2.00f);
#endif

							var damage = random.NextFloatExtra(puncher_override.damage_base, puncher_override.damage_bonus) * damage_mult;

#if SERVER
							//Damage.Hit(attacker: ent_body, owner: ent_body, target: result.entity,
							//	world_position: result.world_position, direction: dir, normal: -dir,
							//	damage: damage * modifier, damage_type: puncher.damage_type,
							//	target_material_type: result.material_type, knockback: puncher.knockback, size: puncher.aoe, flags: flags, speed: 8.00f);

							Damage.Hit(ent_attacker: ent_body, ent_owner: ent_body, ent_target: result.entity,
								position: result.world_position, velocity: dir * puncher_override.knockback, normal: -dir,
								damage_integrity: damage * modifier, damage_durability: damage * modifier, damage_terrain: damage * modifier,
								target_material_type: result.material_type, damage_type: puncher_override.damage_type, flags: damage_flags,
								yield: 0.90f, size: random.NextFloatExtra(0.90f, 0.20f) * puncher_override.aoe, impulse: puncher_override.force, 
								faction_id: h_faction, stun: puncher_override.stun_multiplier);
#endif

							//							switch (result.material_type)
							//							{
							//								case Material.Type.Stone:
							//								{
							//									organic_state.pain += damage;

							//#if CLIENT
							//									Sound.Play("flesh_hit_blunt_02", result.world_position, volume: 0.75f, pitch: random.NextFloatRange(0.65f, 0.80f));
							//#endif
							//								}
							//								break;

							//								case Material.Type.Metal:
							//								{
							//									organic_state.pain += damage * 2.00f;

							//#if CLIENT
							//									Sound.Play("flesh_hit_blunt_02", result.world_position, volume: 0.75f, pitch: random.NextFloatRange(0.65f, 0.80f));
							//#endif

							//#if SERVER
							//									Damage.Hit(attacker: ent_body, owner: ent_body, target: ent_body,
							//										world_position: result.world_position, direction: dir, normal: -dir,
							//										damage: damage * modifier * 0.20f, damage_type: Damage.Type.Impact,
							//										primary_damage_multiplier: 0.01f,
							//										secondary_damage_multiplier: 1.00f,
							//										target_material_type: Material.Type.Flesh, knockback: puncher.knockback, size: puncher.aoe, flags: flags, speed: 8.00f);
							//#endif
							//								}
							//								break;

							//								case Material.Type.Glass:
							//								{
							//									organic_state.pain += damage * 5.00f;

							//#if CLIENT
							//									Sound.Play("flesh_hit_blunt_02", result.world_position, volume: 0.75f, pitch: random.NextFloatRange(0.65f, 0.80f));
							//#endif

							//#if SERVER
							//									Damage.Hit(attacker: ent_body, owner: ent_body, target: ent_body,
							//										world_position: result.world_position, direction: dir, normal: -dir,
							//										damage: damage * modifier * 0.20f, damage_type: Damage.Type.Saw,
							//										primary_damage_multiplier: 0.80f,
							//										secondary_damage_multiplier: 1.00f,
							//										target_material_type: Material.Type.Flesh, knockback: puncher.knockback, size: puncher.aoe, flags: flags, speed: 8.00f);
							//#endif
							//								}
							//								break;

							//								case Material.Type.Wood:
							//								{
							//									organic_state.pain += damage * 0.60f;

							//#if CLIENT
							//									Sound.Play("flesh_hit_blunt_02", result.world_position, volume: 0.45f, pitch: random.NextFloatRange(0.85f, 0.90f));
							//#endif

							////#if SERVER
							////									Damage.Hit(attacker: ent_body, owner: ent_body, target: ent_body,
							////										world_position: result.world_position, direction: dir, normal: -dir,
							////										damage: damage * modifier * 0.10f, damage_type: Damage.Type.Saw,
							////										primary_damage_multiplier: 0.50f,
							////										secondary_damage_multiplier: 1.00f,
							////										target_material_type: Material.Type.Flesh, knockback: puncher.knockback, size: puncher.aoe, flags: flags, speed: 8.00f);
							////#endif
							//								}
							//								break;
							//							}

							//organic_state.pain += 100.00f;

							//flags |= Damage.Flags.No_Sound;
							modifier *= puncher_override.penetration_falloff;

							//penetration--;
							if (--penetration < 0) break;
						}
					}

					//body.AddTorque(body.GetMass() * 1000.00f);


					//var angle = transform.LocalToWorldRotation(dir.GetAngleRadians());

					var angle = dir.GetAngleRadians();
					//angle = float.IsPositive(angle) ? Maths.Clamp(angle, 0.25f, 1.00f) : Maths.Clamp(angle, -0.25f, -1.00f);

					var angle_delta = angle;
					angle_delta = Maths.Clamp(transform.GetDeltaAngle(angle), -0.70f, 0.70f);

					//App.WriteLine($"{angle_delta}; {dir.GetAngleRadians()}; {transform.rotation}; {angle}");
					body.SetAngularVelocity(angle_delta * 50);
					if (torso.IsNotNull() && torso.air_time <= 0.05f)
					{
						var force = dir * App.tickrate_f32 * puncher_override.force;
						force = Physics.LimitForce2B(ref body, force, new Vector2(20, 15));

						if (hit_any)
						{
							body.AddForce(force);
						}
						else
						{
							body.AddForce(force);
						}
					}
				}
			}
		}

#if CLIENT
		[ISystem.VeryLateUpdate(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("dead", false, Source.Modifier.Owned)]
		public static void UpdateSprite(ISystem.Info info, Entity entity,
		[Source.Owned, Override] in Puncher.Data puncher, [Source.Shared] ref Animated.Renderer.Data renderer)
		{
			if ((info.WorldTime - puncher.last_attack) <= 0.20f)
			{
				renderer.sprite.frame = puncher.frame;
			}
		}
#endif
	}
}
