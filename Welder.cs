namespace TC2.Base.Components
{
	public static class Smirgrafit
	{
		internal static readonly Texture.Handle bigger_smoke_light = "BiggerSmoke_Light";
		internal static readonly Texture.Handle metal_spark = "Metal_Spark";
		internal static readonly Texture.Handle metal_spark_01 = "metal_spark.01";
		internal static readonly Sound.Handle[] snd_smirgrafit_spark =
		{
			"smirgrafit.spark.00"
		};

		[IEvent.Data]
		public partial struct FuseEvent(): IEvent
		{
			public Vector2 position;
		}

		[Flags]
		public enum Flags: ushort
		{
			None = 0,

			Fused = 1 << 0,

		}

		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public struct Data: IComponent
		{
			public byte unused_00;
			public byte unused_01;
			public Smirgrafit.Flags flags;

			[Save.NewLine]
			public required Area surface_area;
			[Editor.Picker.Position(true)] public required Vec2f offset;

			[Save.NewLine]
			[Asset.Ignore] public float dusty_ratio;
			[Asset.Ignore] public float unfused_ratio;

			[Save.NewLine]
			[Net.Ignore, Save.Ignore] public float unused_03;
			[Net.Ignore, Save.Ignore] public float t_next_effect;
		}

		[Flags]
		public enum EmitEffectFlags: ushort
		{
			None = 0,

			No_Particles = 1 << 0,
			No_Smoke = 1 << 1,
			No_Spark = 1 << 2,
			No_Glow = 1 << 3,
			No_Sound = 1 << 4,
			No_Shake = 1 << 5,
		}

#if CLIENT
		// TODO: modernize the old shitcode
		public static void EmitEffect(ref Region.Data region, ref XorRandom random, Vec2f pos, Smirgrafit.EmitEffectFlags effect_flags = Smirgrafit.EmitEffectFlags.None)
		{
			if (random.NextBool(0.50f))
			{
				if (effect_flags.HasNone(Smirgrafit.EmitEffectFlags.No_Particles))
				{
					if (effect_flags.HasNone(Smirgrafit.EmitEffectFlags.No_Smoke) && random.NextBool(0.21f))
					{
						Particle.Spawn(ref region, new Particle.Data
						{
							texture = bigger_smoke_light,
							lifetime = random.NextFloatRange(3.00f, 8.00f),
							pos = pos + random.NextVector2(0.05f),
							vel = random.NextVector2(1.00f),
							fps = random.NextByteRange(5, 16),
							frame_count = 64,
							frame_count_total = 64,
							frame_offset = random.NextByteRange(0, 64),
							//scale = random.NextFloatRange(1.50f, 2.50f),
							scale = random.NextFloatRange(0.11f, 0.30f),
							rotation = random.NextFloat(10.00f),
							angular_velocity = random.NextFloat(1.00f),
							growth = random.NextFloatRange(0.05f, 0.15f),
							drag = random.NextFloatRange(0.05f, 0.17f),
							force = new Vector2(random.NextFloatRange(-2.00f, 7.00f), random.NextFloatRange(-6, 1)),
							color_a = random.NextColor32Range(0x80ffffff, 0xb0ffffff),
							color_b = random.NextColor32Range(0x00ffffff, 0x00808080)
						});
					}

					if (effect_flags.HasNone(Smirgrafit.EmitEffectFlags.No_Spark))
					{
						for (var i = 0; i < 1; i++)
						{
							Particle.Spawn(ref region, new Particle.Data
							{
								texture = metal_spark_01,
								lifetime = random.NextFloatRange(0.80f, 1.80f),
								pos = pos + random.NextVector2(0.10f),
								vel = random.NextVector2Range(new Vector2(-8, 8), new Vector2(-35, 12)),
								fps = 0,
								frame_count = 1,
								frame_offset = random.NextByteRange(0, 4),
								frame_count_total = 4,
								scale = random.NextFloatRange(0.30f, 0.60f),
								growth = -random.NextFloatRange(0.60f, 1.00f),
								force = new Vector2(random.NextFloatRange(-4, 4), random.NextFloatRange(-10, 40)),
								drag = random.NextFloatRange(0.01f, 0.08f),
								stretch = new Vector2(random.NextFloatRange(1.00f, 2.00f), 0.75f),
								color_a = random.NextColor32Range(0xffffffff, 0xffffc0b0),
								color_b = random.NextColor32Range(0xffffc0b0, 0xffffa090),
								lit = 1.00f,
								face_dir_ratio = 1.00f,
							});
						}
					}

					if (effect_flags.HasNone(Smirgrafit.EmitEffectFlags.No_Glow))
					{
						Particle.Spawn(ref region, new Particle.Data
						{
							texture = Light.tex_light_fire_00,
							lifetime = random.NextFloatRange(3.00f, 15.0f),
							pos = pos + random.NextVector2(0.125f),
							vel = random.NextVector2(0.04f),
							//scale = random.NextFloatRange(2.00f, 3.50f),
							scale = random.NextFloatRange(0.60f, 1.40f),
							angular_velocity = random.NextFloat(1.35f),
							//growth = random.NextFloatRange(1.50f, 3.50f),
							growth = random.NextFloatRange(0.40f, 1.10f),
							rotation = random.NextFloat(Maths.tau),
							stretch = new Vector2(random.NextFloatRange(0.60f, 1.10f), random.NextFloatRange(0.60f, 1.10f)),
							color_a = ColorBGRA.RGBA(1.00f, 0.70f, 0.40f, random.NextFloatRange(0.10f, 0.90f)),
							color_b = ColorBGRA.RGBA(0.30f, 0.00f, 0.00f, 0.00f),
							glow = 1.00f //random.NextFloatExtra(1.00f, 4.00f)
						});
					}
				}

				if (effect_flags.HasNone(Smirgrafit.EmitEffectFlags.No_Shake) && random.NextBool(0.30f))
				{
					Shake.Emit(ref region, pos, 0.10f, 0.40f, 16.00f);
				}

				if (effect_flags.HasNone(Smirgrafit.EmitEffectFlags.No_Sound) && random.NextBool(0.70f))
				{
					Sound.Play(snd_smirgrafit_spark.GetRandom(ref random), pos + random.NextVector2(0.50f),
						volume: random.NextFloatRange(0.20f, 0.30f),
						pitch: random.NextFloatRange(0.33f, 1.18f), size: 1.00f, dist_multiplier: 0.80f);
				}
			}
		}
#endif
	}

	public static class Welder
	{
		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public struct Data(): IComponent
		{
			[Editor.Picker.Position(true)] public Vector2 offset;
			public float max_length = 3.00f;
			public float thickness = 1.00f;
			public float damage = 600.00f;
			public float damage_extra = 1100.00f;
			public float fuel_usage = 0.10f;

			public IMaterial.Handle h_material_fuel;
			public Sound.Handle sound_start;
			//public Sound.Handle sound_end;

			[Net.Ignore, Save.Ignore] public float t_last_use;
			[Net.Ignore, Save.Ignore] public float t_last_sound;
			[Net.Ignore, Save.Ignore] public float t_next_use;
		}

		[ISystem.Add(ISystem.Mode.Single, ISystem.Scope.Region)]
		[ISystem.VeryLateUpdate(ISystem.Mode.Single, ISystem.Scope.Region, interval: 0.50f)]
		public static void UpdateHoldable([Source.Owned] in Welder.Data welder, [Source.Owned] ref Holdable.Data holdable)
		{
			holdable.hints.AddFlag(NPC.ItemHints.Short_Range | NPC.ItemHints.Dangerous | NPC.ItemHints.Inaccurate | NPC.ItemHints.Industrial | NPC.ItemHints.Construction | NPC.ItemHints.Destructive | NPC.ItemHints.Unstable | NPC.ItemHints.Weapon | NPC.ItemHints.Tools | NPC.ItemHints.Usable);
		}

		internal static readonly Texture.Handle bigger_smoke_light = "BiggerSmoke_Light";
		internal static readonly Texture.Handle metal_spark = "Metal_Spark";
		internal static readonly Texture.Handle metal_spark_01 = "metal_spark.01";
		internal static readonly Sound.Handle[] snd_smirgrafit_spark =
		{
			"smirgrafit.spark.00"
		};

		public ref struct IterateArgs
		{
			public readonly ref XorRandom random;
			public readonly ref int count;

			public IBlock.Handle h_block_src;
			public IBlock.Handle h_block_dst;

			public IterateArgs(ref XorRandom random, ref int count, IBlock.Handle h_block_src, IBlock.Handle h_block_dst)
			{
				this.random = ref random;
				this.count = ref count;

				this.h_block_src = h_block_src;
				this.h_block_dst = h_block_dst;
			}
		}

		//[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]

		[Shitcode]
		public static void Func(Tile tile, scoped ref IterateArgs args, int x, int y, byte mask)
		{
			if (mask.IsFalse()) return;

			var h_block = tile.GetBlockHandle();
			if (h_block == args.h_block_src)
			{
#if SERVER
				tile.BlockID = (byte)args.h_block_dst;
#elif CLIENT
				scoped ref var random = ref args.random;
				if (random.NextBool(0.50f))
				{
					var tile_wpos = tile.GetWorldPosition();
					ref var region = ref tile.GetRegion();

					if (random.NextBool(0.21f))
					{
						Particle.Spawn(ref region, new Particle.Data()
						{
							texture = bigger_smoke_light,
							lifetime = random.NextFloatRange(3.00f, 8.00f),
							pos = tile_wpos + random.NextVector2(0.05f),
							vel = random.NextVector2(1.00f),
							fps = random.NextByteRange(5, 16),
							frame_count = 64,
							frame_count_total = 64,
							frame_offset = random.NextByteRange(0, 64),
							//scale = random.NextFloatRange(1.50f, 2.50f),
							scale = random.NextFloatRange(0.11f, 0.30f),
							rotation = random.NextFloat(10.00f),
							angular_velocity = random.NextFloat(1.00f),
							growth = random.NextFloatRange(0.05f, 0.15f),
							drag = random.NextFloatRange(0.05f, 0.17f),
							force = new Vector2(random.NextFloatRange(-2.00f, 7.00f), random.NextFloatRange(-6, 1)),
							color_a = random.NextColor32Range(0x80ffffff, 0xb0ffffff),
							color_b = random.NextColor32Range(0x00ffffff, 0x00808080)
						});
					}

					for (var i = 0; i < 1; i++)
					{
						Particle.Spawn(ref region, new Particle.Data()
						{
							texture = metal_spark_01,
							lifetime = random.NextFloatRange(0.80f, 1.80f),
							pos = tile_wpos + random.NextVector2(0.10f),
							vel = random.NextVector2Range(new Vector2(-8, 8), new Vector2(-35, 12)),
							fps = 0,
							frame_count = 1,
							frame_offset = random.NextByteRange(0, 4),
							frame_count_total = 4,
							scale = random.NextFloatRange(0.30f, 0.60f),
							growth = -random.NextFloatRange(0.60f, 1.00f),
							force = new Vector2(random.NextFloatRange(-4, 4), random.NextFloatRange(-10, 40)),
							drag = random.NextFloatRange(0.01f, 0.08f),
							stretch = new Vector2(random.NextFloatRange(1.00f, 2.00f), 0.75f),
							color_a = random.NextColor32Range(0xffffffff, 0xffffc0b0),
							color_b = random.NextColor32Range(0xffffc0b0, 0xffffa090),
							lit = 1.00f,
							face_dir_ratio = 1.00f,
						});
					}

					//if (random.NextBool(0.90f))
					{
						Particle.Spawn(ref region, new Particle.Data()
						{
							texture = Light.tex_light_fire_00,
							lifetime = random.NextFloatRange(3.00f, 15.0f),
							pos = tile_wpos + random.NextVector2(0.125f),
							vel = random.NextVector2(0.04f),
							//scale = random.NextFloatRange(2.00f, 3.50f),
							scale = random.NextFloatRange(0.60f, 1.40f),
							angular_velocity = random.NextFloat(1.35f),
							//growth = random.NextFloatRange(1.50f, 3.50f),
							growth = random.NextFloatRange(0.40f, 1.10f),
							rotation = random.NextFloat(Maths.tau),
							stretch = new Vector2(random.NextFloatRange(0.60f, 1.10f), random.NextFloatRange(0.60f, 1.10f)),
							color_a = ColorBGRA.RGBA(1.00f, 0.70f, 0.40f, random.NextFloatRange(0.10f, 0.90f)),
							color_b = ColorBGRA.RGBA(0.30f, 0.00f, 0.00f, 0.00f),
							glow = 1.00f //random.NextFloatExtra(1.00f, 4.00f)
						});
					}

					if (random.NextBool(0.30f))
					{
						Shake.Emit(ref region, tile_wpos, 0.10f, 0.40f, 16.00f);
					}

					if (args.count < 2 && random.NextBool(0.70f - (args.count * 0.30f)))
					{
						Sound.Play(snd_smirgrafit_spark.GetRandom(ref random), tile_wpos + random.NextVector2(0.50f),
							volume: random.NextFloatRange(0.20f, 0.30f),
							pitch: random.NextFloatRange(0.33f, 1.18f), size: 1.00f, dist_multiplier: 0.80f);

						args.count++;
					}
				}
#endif
			}

		}


		[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region)]
		//[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
		public static void Update(ISystem.Info info, Entity entity, ref Region.Data region, ref XorRandom random,
		[Source.Owned] in Transform.Data transform, [Source.Owned] ref Body.Data body, [Source.Owned] in Control.Data control, [Source.Owned] ref Welder.Data welder,
		[Source.Owned, Pair.Component<Welder.Data>] ref Inventory1.Data inventory)
		{
			const bool debug_draw = false;

			var time = info.WorldTime;
			var lmb = control.mouse.GetKey(Mouse.Key.Left);
			var any = lmb;

			if (any)
			{
				any &= inventory.resource == welder.h_material_fuel;
			}

			if (any)
			{
				var aim_dir = transform.GetDirection();

				var pos_a = transform.LocalToWorld(welder.offset);
				var pos_a_jet = pos_a + (aim_dir * 0.50f);
				var pos_b = pos_a_jet + (aim_dir * welder.max_length);

				var ent_parent = body.GetParent();
				var h_faction = body.GetFaction();

				var hit_terrain = false;
				var hit_pos_world = Vec2f.Zero;
				var pos_end = pos_b;

				var dist_ratio = 1.00f;

				body.AddForce(aim_dir * -body.GetMass() * 25.00f);

				if (region.TryGridCast(pos: pos_a, dir: aim_dir, out var hit_pos_grid, max_dist: welder.max_length, threshold_solid: 0.10f, allow_inside: true))
				{
#if SERVER
					if (debug_draw) region.DrawDebugLine(a: pos_a, b: hit_pos_grid, color: Color32BGRA.Cyan, thickness: 4.00f);
#endif
				}

				var can_tick = time >= welder.t_next_use;
				if (can_tick)
				{
					welder.t_next_use = time + random.NextFloatExtra(0.14f, 0.07f);

#if SERVER
					inventory.Remove(welder.h_material_fuel, welder.fuel_usage);
#endif
				}

#if SERVER
				if (can_tick)
#endif
				{
					Span<LinecastResult> results = FixedArray.CreateSpan16NoInit<LinecastResult>(out var results_buffer);

					if (region.TryLinecastAll(world_position_start: pos_a_jet, world_position_end: pos_b, radius: welder.thickness * 0.50f, hits: ref results,
					layer: Physics.Layer.Fire, mask: Physics.Layer.World | Physics.Layer.Organic | Physics.Layer.Flammable | Physics.Layer.Heatable | Physics.Layer.Destructible,
					exclude: Physics.Layer.Essence | Physics.Layer.Water | Physics.Layer.Fire | Physics.Layer.Stored | Physics.Layer.Bounds))
					{
						results.SortByDistance();



						// TODO: make this not hardcoded
						var count_tmp = 0;
						scoped var args = new IterateArgs(random: ref random, count: ref count_tmp, h_block_src: "framework.smirgrafit", h_block_dst: "framework.smirgrafit.fused");

						ref var terrain = ref region.GetTerrain();
						foreach (ref var result in results)
						{


							//if (result.entity == entity || result.entity == parent || result.entity_parent == parent) continue;

							ref var block = ref result.GetBlock(out var h_block);
							if (block.IsNotNull())
							{
								var pos_hit = result.world_position;

								if (!hit_terrain)
								{
									hit_terrain = true;
									hit_pos_world = pos_hit;
								}

								var dirty_flags = Chunk.DirtyFlags.None;
#if SERVER
								dirty_flags |= Chunk.DirtyFlags.Collider | Chunk.DirtyFlags.Texture | Chunk.DirtyFlags.Stability | Chunk.DirtyFlags.Colors | Chunk.DirtyFlags.Sync;
#endif
								if (can_tick) // this should get lowered by JIT on server
								{
									terrain.IterateSquareLine(
										world_position_a: pos_hit - (aim_dir * 0.25f),
										world_position_b: pos_hit + (aim_dir * random.NextFloatRange(0.20f, 2.50f)).RotateByRad(random.NextFloat(0.30f)),
										thickness: welder.thickness * random.NextFloatRange(0.50f, 1.20f),
										argument: ref args,
										func: Func,
										dirty_flags: dirty_flags);
								}
							}
							else
							{
								if (hit_terrain) break;

								var area = result.GetShapeArea();
								if (area <= 0.00001f) continue;



								if (result.layer.HasAny(Physics.Layer.Door | Physics.Layer.Solid) && result.mask.HasAny(Physics.Layer.Solid))
								{
									hit_terrain = true;
									hit_pos_world = result.world_position;
									hit_pos_grid = result.world_position_raw - (result.normal);
								}


#if SERVER
								var area_inv = (area * 2).RcpFast01();
								var damage = random.NextFloatExtra(welder.damage, welder.damage_extra) * area_inv;
								var skip_damage = false;

								if (result.layer.HasAny(Physics.Layer.Flammable))
								{
									//skip_damage = true;
									Fire.Ignite(ent_target: result.entity,
										step: 8.00f * area_inv,
										intensity: 100.00f,
										damage: damage,
										damage_type: Damage.Type.Phlogiston,
										temperature: 2700.00f,
										flags: Fire.Flags.None);

									damage *= 0.65f;
								}

								if (result.layer.HasAny(Physics.Layer.Heatable))
								{
									ref var heat_state = ref result.entity.GetComponent<Heat.State>();
									if (heat_state.IsNotNull())
									{
										//skip_damage = true;

										heat_state.AddEnergy(damage * 5.00f);
										heat_state.Sync(result.entity, true);

										damage *= 0.50f;
									}
								}

								if (!skip_damage)
								{
									Damage.Hit(ent_attacker: entity, ent_owner: default, ent_target: result.entity,
									position: result.world_position, velocity: aim_dir * random.NextFloatExtra(2.00f, 8.00f), normal: result.normal,
									damage_integrity: damage, damage_durability: damage * 0.70f, damage_terrain: damage,
									target_material_type: result.material_type, damage_type: Damage.Type.Phlogiston,
									flags: Damage.Flags.No_Loot_Pickup | Damage.Flags.No_Loot_Notification,
									yield: 1.10f, size: 1.20f, impulse: 100.00f, faction_id: h_faction, pain: 1.10f, stun: 0.95f);
								}

								//Damage.Hit(ent_attacker: entity, ent_owner: ent_parent, ent_target: result.entity,
								//position: result.world_position, velocity: aim_dir * random.NextFloatExtra(2.00f, 3.00f), normal: result.normal,
								//damage_integrity: damage, damage_durability: damage, damage_terrain: damage,
								//target_material_type: result.material_type, damage_type: Damage.Type.Phlogiston,
								//flags: Damage.Flags.No_Loot_Pickup,
								//yield: 0.90f, size: 1.20f, impulse: 100.00f, faction_id: h_faction, pain: 1.60f, stun: 0.95f);									
#endif
							}

							if (result.layer.HasAny(Physics.Layer.World | Physics.Layer.Solid))
							{
								pos_end = result.world_position;
							}
							//var dir = (pos_a - result.world_position).GetNormalized(out var dist);
							//dir = result.normal;

							//var t = 1.00f - Maths.NormalizeClamp(dist, welder.max_length);

							//ref var result_body = ref result.GetBody();
							//if (!result_body.IsNull())
							//{
							//	var force = dir * 100.00f * power * t;
							//	force = Physics.LimitForce(ref result_body, force, new Vector2(10, 10));
							//	result_body.AddForceWorld(force, result.world_position);
							//}
						}
					}
				}

#if CLIENT
				var dist_end = Vec2f.Distance(pos_a, hit_pos_grid);
				dist_ratio = Maths.Normalize01Fast(dist_end, welder.max_length);

				if (debug_draw) region.DrawDebugText(pos_end, $"{dist_ratio:0.00}", color: Color32BGRA.White);

				for (var i = 0; i < 1; i++)
				{
					Particle.Spawn(ref region, new Particle.Data()
					{
						texture = Geyser.texture_metal_spark,
						lifetime = random.NextFloatRange(0.10f, 0.30f) * dist_ratio,
						pos = pos_a_jet,
						vel = body.GetVelocity() + (aim_dir * random.NextFloatRange(15.00f, 40.00f)) + random.NextUnitVector2Range(0.10f, 10.50f),
						fps = 16,
						frame_count = 4,
						frame_offset = random.NextByteRange(0, 4),
						frame_count_total = 4,
						scale = random.NextFloatRange(0.50f, 1.20f),
						//rotation = random.NextFloat(MathF.PI),
						//angular_velocity = random.NextFloat(MathF.PI * 10.00f),
						growth = random.NextFloatRange(0.20f, 4.00f),
						stretch = new Vector2(random.NextFloatRange(1.00f, 2.00f), random.NextFloatRange(0.10f, 0.20f)),
						force = random.NextUnitVector2Range(2, 8) + new Vector2(0.00f, random.NextFloatRange(-40, 50)),
						color_a = ColorBGRA.White,
						color_b = ColorBGRA.White,
						drag = random.NextFloatRange(0.08f, 0.19f),
						angular_velocity = random.NextFloatRange(-0.20f, 0.40f),
						face_dir_ratio = 1.00f,
						lit = 1.00f
					});
				}

				Particle.Spawn(ref region, new Particle.Data()
				{
					texture = Light.tex_light_fire_00,
					lifetime = random.NextFloatRange(0.12f, 0.20f) * dist_ratio,
					pos = pos_a_jet,
					//vel = new Vector2(0.70f, -1.10f) * (1.00f + (modifier2 * 0.10f)),
					vel = body.GetVelocity() + (aim_dir * random.NextFloatRange(15.00f, 25.00f) * dist_ratio) + random.NextUnitVector2Range(0.10f, 1.50f),
					//force = new Vector2(0, 2.50f) * random.NextFloatRange(0.90f, 1.10f),
					scale = random.NextFloatRange(1.10f, 1.30f),
					//rotation = random.NextFloatRange(-10.00f, 10.00f),
					//angular_velocity = random.NextFloat(1.00f),
					growth = random.NextFloatRange(15.00f, 35.00f),
					drag = random.NextFloatRange(0.07f, 0.10f),
					stretch = new Vector2(random.NextFloatRange(1.55f, 1.74f), random.NextFloatRange(0.30f, 0.40f)),
					color_a = ColorBGRA.RGBA(1.00f, 0.70f, 0.30f, random.NextFloatExtra(5.00f, 10.00f)),
					color_b = ColorBGRA.RGBA(0.50f, 0.10f, 0.00f, 0.00f),
					glow = random.NextFloatRange(8.00f, 12.00f),
					force = random.NextUnitVector2Range(1, 2) + new Vector2(0.00f, random.NextFloatRange(-15, 15)) + (aim_dir * 3.00f),
					face_dir_ratio = 1.00f
				});

				if (dist_ratio < 0.98f)
				{
					var dist_ratio_end = 1.00f - (dist_ratio * 0.50f);
					Particle.Spawn(ref region, new Particle.Data()
					{
						texture = Light.tex_light_fire_00,
						lifetime = random.NextFloatRange(0.12f, 0.20f) * dist_ratio_end,
						pos = hit_pos_grid,
						//vel = new Vector2(0.70f, -1.10f) * (1.00f + (modifier2 * 0.10f)),
						vel = random.NextUnitVector2Range(0.10f, 1.50f),
						//force = new Vector2(0, 2.50f) * random.NextFloatRange(0.90f, 1.10f),
						scale = random.NextFloatRange(1.10f, 1.30f),
						//rotation = random.NextFloatRange(-10.00f, 10.00f),
						//angular_velocity = random.NextFloat(1.00f),
						growth = random.NextFloatRange(15.00f, 35.00f),
						drag = random.NextFloatRange(0.07f, 0.10f),
						//stretch = new Vector2(random.NextFloatRange(1.55f, 1.74f), random.NextFloatRange(0.30f, 0.40f)),
						color_a = ColorBGRA.RGBA(1.00f, 0.70f, 0.30f, random.NextFloatExtra(5.00f, 10.00f)),
						color_b = ColorBGRA.RGBA(0.50f, 0.10f, 0.00f, 0.00f),
						glow = random.NextFloatRange(8.00f, 12.00f),
						//force = random.NextUnitVector2Range(1, 2) + new Vector2(0.00f, random.NextFloatRange(-15, 15)) + (aim_dir * 3.00f),
						face_dir_ratio = 1.00f
					});
				}
#endif
				//reaction_force = Physics.LimitForce(ref body, reaction_force, new Vector2(10, 10));
			}
		}

		public static readonly Texture.Handle texture_smoke = "BiggerSmoke_Light";

		[ISystem.VeryLateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void UpdateEffects(ISystem.Info info, Entity entity, ref Region.Data region, ref XorRandom random,
		[Source.Owned] in Transform.Data transform, [Source.Owned] in Body.Data body, [Source.Owned, Pair.Component<Welder.Data>] ref Inventory1.Data inventory,
		[Source.Owned] in Control.Data control, [Source.Owned] ref Welder.Data welder, [Source.Owned] ref Sound.Mixer sound_mix)
		{
			var lmb = control.mouse.GetKey(Mouse.Key.Left);
			var is_active = false;

			if (lmb)
			{
				is_active = inventory.resource == welder.h_material_fuel;
			}

			sound_mix.modifier = is_active ? 1.00f : 0.00f;

#if CLIENT
			if (is_active && info.WorldTime >= welder.t_last_sound + 0.15f)
			{
				//if (control.mouse.GetKeyUp(Mouse.Key.Left))
				//{
				//	welder.last_sound = info.WorldTime;
				//	Sound.Play(welder.sound_end, transform.position, volume: 1.20f);
				//}
				//else 

				if (control.mouse.GetKeyDown(Mouse.Key.Left))
				{
					welder.t_last_sound = info.WorldTime;
					Sound.Play(welder.sound_start, transform.position, volume: 0.90f);
				}

			}

			if (is_active)
			{
				welder.t_last_sound = info.WorldTime;

				//var aim_dir = transform.GetDirection();

				//var pos_a = transform.LocalToWorld(welder.offset) + (aim_dir * 0.50f);
				////var pos_b = pos_a + (aim_dir * welder.max_length * 0.1250f);

				//for (var i = 0; i < 1; i++)
				//{
				//	Particle.Spawn(ref region, new Particle.Data()
				//	{
				//		texture = Geyser.texture_metal_spark,
				//		lifetime = random.NextFloatRange(0.10f, 0.30f),
				//		pos = pos_a,
				//		vel = body.GetVelocity() + (aim_dir * random.NextFloatRange(15.00f, 40.00f)) + random.NextUnitVector2Range(0.10f, 10.50f),
				//		fps = 16,
				//		frame_count = 4,
				//		frame_offset = random.NextByteRange(0, 4),
				//		frame_count_total = 4,
				//		scale = random.NextFloatRange(0.50f, 1.20f),
				//		//rotation = random.NextFloat(MathF.PI),
				//		//angular_velocity = random.NextFloat(MathF.PI * 10.00f),
				//		growth = random.NextFloatRange(0.20f, 4.00f),
				//		stretch = new Vector2(random.NextFloatRange(1.00f, 2.00f), random.NextFloatRange(0.10f, 0.20f)),
				//		force = random.NextUnitVector2Range(2, 8) + new Vector2(0.00f, random.NextFloatRange(-40, 50)),
				//		color_a = new Color32BGRA(255, 255, 255, 255),
				//		color_b = new Color32BGRA(255, 255, 255, 255),
				//		drag = random.NextFloatRange(0.08f, 0.19f),
				//		angular_velocity = random.NextFloatRange(-0.20f, 0.40f),
				//		face_dir_ratio = 1.00f,
				//		lit = 1.00f
				//	});
				//}

				//Particle.Spawn(ref region, new Particle.Data()
				//{
				//	texture = Light.tex_light_fire_00,
				//	lifetime = random.NextFloatRange(0.12f, 0.20f),
				//	pos = pos_a,
				//	//vel = new Vector2(0.70f, -1.10f) * (1.00f + (modifier2 * 0.10f)),
				//	vel = body.GetVelocity() + (aim_dir * random.NextFloatRange(15.00f, 35.00f)) + random.NextUnitVector2Range(0.10f, 1.50f),
				//	//force = new Vector2(0, 2.50f) * random.NextFloatRange(0.90f, 1.10f),
				//	scale = random.NextFloatRange(1.10f, 1.30f),
				//	//rotation = random.NextFloatRange(-10.00f, 10.00f),
				//	//angular_velocity = random.NextFloat(1.00f),
				//	growth = random.NextFloatRange(15.00f, 35.00f),
				//	drag = random.NextFloatRange(0.07f, 0.10f),
				//	stretch = new Vector2(random.NextFloatRange(1.55f, 1.74f), random.NextFloatRange(0.30f, 0.40f)),
				//	color_a = ColorBGRA.RGBA(1.00f, 0.70f, 0.30f, random.NextFloatExtra(5.00f, 10.00f)),
				//	color_b = ColorBGRA.RGBA(0.50f, 0.10f, 0.00f, 0.00f),
				//	glow = random.NextFloatRange(8.00f, 20.00f),
				//	force = random.NextUnitVector2Range(1, 2) + new Vector2(0.00f, random.NextFloatRange(-15, 15)) + (aim_dir * 3.00f),
				//	face_dir_ratio = 1.00f
				//});

			}
#endif
		}
	}
}
