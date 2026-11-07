
namespace TC2.Base.Components
{
	[Shitcode]
	public static partial class Explosion
	{
		//public static bool enable_terrain_damage = true;
		//public static bool enable_entity_damage = true;

#if CLIENT
		public static readonly Sound.Handle[] sounds = new Sound.Handle[]
		{
			"explosion.00",
			"explosion.01",
			"explosion.02",
			"explosion.03",
			"explosion.04",
			"explosion.05"
		};

		public static readonly Sound.Handle[] snd_metal = new Sound.Handle[]
		{
			"explosion.metal.00",
			"explosion.metal.01",
			"explosion.metal.02",
		};

		public static readonly Sound.Handle[] snd_shrapnel = new Sound.Handle[]
		{
			//"explosion.shrapnel.00",
			//"explosion.shrapnel.01",
			"explosion.shrapnel.02",
		};


		public static readonly Sound.Handle[] snd_steam = new Sound.Handle[]
		{
			"explosion.steam.00",
			"explosion.steam.01",
		};

		public static readonly Sound.Handle[] snd_poof = new Sound.Handle[]
		{
			"explosion.poof.00",
		};

		public static readonly Sound.Handle[] snd_gas = new Sound.Handle[]
		{
			"explosion.gas.00",
		};

		public static readonly Sound.Handle[] snd_fast = new Sound.Handle[]
		{
			"explosion.fast.00",
		};

		public static readonly Texture.Handle texture_smoke = "BiggerSmoke_Light";
		public static readonly Texture.Handle texture_smoke2 = "BiggerSmoke";
		public static readonly Texture.Handle texture_explosion = "Explosion";
		public static readonly Texture.Handle texture_metal_spark = "Metal_Spark";
		public static readonly Texture.Handle texture_metal_spark_01 = "metal_spark.01";
#endif

		public static readonly Texture.Handle[] tex_mask_explosion = new Texture.Handle[]
		{
			"mask.explosion.00",
			//"mask.explosion.01",
			//"mask.explosion.02"
		};

		[Flags]
		public enum Flags: ushort
		{
			None = 0,

			[Asset.Ignore, Net.Ignore] Exploded = 1 << 0,
			[Asset.Ignore, Net.Ignore] Shockwaved = 1 << 1,
			No_Split = 1 << 2,
			[Asset.Ignore, Net.Ignore] Done = 1 << 3,
			[Asset.Ignore, Net.Ignore] Deleted = 1 << 4,
		}

		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region), IComponent.With<Explosion.State>]
		public partial struct Data(): IComponent
		{
			public float radius = 4.00f;
			public float power = 1.00f;

			public float fire_amount = 1.00f;
			public float smoke_amount = 1.00f;
			public float sparks_amount;

			public float smoke_lifetime_multiplier = 1.00f;
			public float smoke_velocity_multiplier = 1.00f;

			public Color32BGRA smoke_color = new Color32BGRA(220, 180, 180, 180);

			public float flash_duration_multiplier = 1.00f;
			public float flash_intensity_multiplier = 1.00f;

			public float shake_multiplier = 1.00f;
			public float force_multiplier = 1.00f;
			public float stun_multiplier = 1.00f;
			public float terrain_radius_multiplier = 1.00f;

			public float volume = 1.00f;
			public float pitch = 1.00f;
			public float delay = 0.02f;

			//[Asset.Ignore, Net.Ignore, Obsolete] public Entity ent_owner;

			//[Asset.Ignore, Net.Ignore] public float age;
			//[Asset.Ignore, Net.Ignore] public float t_elapsed_wait;
			//[Asset.Ignore, Net.Ignore, Save.Ignore] public float next_smoke;

			public Damage.Type damage_type = Damage.Type.Explosion;
			public Damage.Type damage_type_secondary = Damage.Type.Shockwave;
			public Explosion.Flags flags;

			[Asset.Ignore] public Vec2f velocity;
			[Asset.Ignore, Net.Ignore, Save.Ignore] private ulong unused_c_01;

			[Net.Ignore] public float damage_entity;
			[Net.Ignore] public float damage_terrain;

			[Asset.Ignore, Net.Ignore] public float power_rem;
			[Asset.Ignore, Net.Ignore, Save.Ignore] private uint unused_a_00;

			[Asset.Ignore, Net.Ignore] public Entity ent_ignored;

			//[Asset.Ignore, Net.Ignore, Save.Ignore] public int smokes_count;
			[Asset.Ignore, Net.Ignore, Save.Ignore] private uint unused_b_00;
			[Asset.Ignore, Net.Ignore, Save.Ignore] private uint unused_b_01;
			[Asset.Ignore, Net.Ignore, Save.Ignore] private uint unused_b_02;
			[Asset.Ignore, Net.Ignore, Save.Ignore] private uint unused_b_03;

			//[Asset.Ignore, Net.Ignore, Save.Ignore] private ulong unused_c_00;
			//[Asset.Ignore, Net.Ignore, Save.Ignore] private ulong unused_c_01;

			//[Asset.Ignore, Net.Ignore, Save.Ignore] public FixedArray16<SmokeNode> smokes;
		}

		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public partial struct State: IComponent
		{
			[Asset.Ignore, Net.Ignore] public ushort counter;
			[Asset.Ignore, Net.Ignore, Save.Ignore] public byte smokes_count;
			[Asset.Ignore, Net.Ignore, Save.Ignore] public byte unused_00;

			[Asset.Ignore, Net.Ignore] public float age;
			[Asset.Ignore, Net.Ignore] public float t_elapsed_wait;
			[Asset.Ignore, Net.Ignore, Save.Ignore] public float next_smoke;

			[Asset.Ignore, Net.Ignore, Save.Ignore] private ulong unused_01;
			[Asset.Ignore, Net.Ignore, Save.Ignore] private ulong unused_02;

			[Asset.Ignore, Net.Ignore, Save.Ignore] public FixedArray16<SmokeNode> smokes;
		}

		public struct SmokeNode
		{
			public Vector2 pos;
			public Vector2 vel;

			public float lifetime;
			public float amount;

			public float unused;
			public float next_smoke;

			public SmokeNode(Vector2 pos, Vector2 vel, float amount, float lifetime, float next_smoke) : this()
			{
				this.pos = pos;
				this.vel = vel;
				this.amount = amount;
				this.lifetime = lifetime;
				this.next_smoke = next_smoke;
			}
		}

#if SERVER
		[ChatCommand.Region("boom", "", admin: true)]
		public static void BoomCommand(ref ChatCommand.Context context, float radius = 8.00f, float power = 4.00f)
		{
			ref var region = ref context.GetRegion();
			if (!region.IsNull())
			{
				Explosion.Spawn(ref region, context.GetTargetPosition(), (Entity ent_explosion, ref Explosion.Data explosion) =>
				{
					explosion.radius = radius;
					explosion.power = power;
					explosion.damage_type = Damage.Type.Explosion;
					explosion.damage_entity = 4000.00f;
					explosion.damage_terrain = 1000.00f;
				});
			}
		}

		public delegate void SetterDelegate(Entity ent_explosion, ref Explosion.Data explosion);
		public static UniTask<Entity> Spawn(ref Region.Data region, Vector2 world_position, SetterDelegate setter)
		{
			return region.SpawnPrefab("explosion", position: world_position).ContinueWith((ent) =>
			{
				ref var explosion = ref ent.GetComponent<Explosion.Data>();
				if (explosion.IsNotNull())
				{
					setter.Invoke(ent, ref explosion);
					explosion.Sync(ent, true);
				}

				return ent;
			});
		}

		//public static UniTask<Entity> Spawn(ref Region.Data region, Vector2 world_position, float power, float radius, float damage_entity, float damage_terrain, Damage.Type damage_type = Damage.Type.Explosion, float? radius = null, float? radius = null)
		//{
		//	return region.SpawnPrefab("explosion", position: world_position).ContinueWith((ent) =>
		//	{
		//		ref var explosion = ref ent.GetComponent<Explosion.Data>();
		//		if (explosion.IsNotNull())
		//		{
		//			explosion.power = power;
		//			explosion.radius = radius;

		//			explosion.damage_entity = damage_entity;
		//			explosion.damage_terrain = damage_terrain;
		//			explosion.damage_type = damage_type;
		//			default = explosive.ent_owner;
		//			explosion.fire_amount = explosive.fire_amount;
		//			explosion.smoke_amount = explosive.smoke_amount;
		//			explosion.smoke_lifetime_multiplier = explosive.smoke_lifetime_multiplier;
		//			explosion.smoke_velocity_multiplier = explosive.smoke_velocity_multiplier;
		//			explosion.flash_duration_multiplier = explosive.flash_duration_multiplier;
		//			explosion.flash_intensity_multiplier = explosive.flash_intensity_multiplier;
		//			explosion.sparks_amount = explosive.sparks_amount;
		//			explosion.volume = explosive.volume;
		//			explosion.pitch = explosive.pitch;
		//			explosion.shake_multiplier = explosive.shake_multiplier;
		//			explosion.force_multiplier = explosive.force_multiplier;

		//			if (explosive.flags.HasAny(Explosive.Flags.No_Self_Damage)) explosion.ent_ignored = ent;

		//			explosion.Sync(ent, true);
		//		}

		//		return ent;
		//	});
		//}

		public static UniTask<Entity> Spawn(ref Region.Data region, Vector2 world_position, Explosive.Data explosive)
		{
			if (explosive.modifier >= explosive.modifier_min)
			{
				return region.SpawnPrefab("explosion", position: world_position).ContinueWith((entity) =>
				{
					ref var explosion = ref entity.GetComponent<Explosion.Data>();
					if (explosion.IsNotNull())
					{
						//explosion.power = Maths.Lerp01(explosive.power * 0.50f, explosive.power, explosive.modifier);
						//explosion.radius = Maths.Lerp01(explosive.radius * 0.25f, explosive.radius, explosive.modifier);
						//explosion.damage_entity = explosive.damage_entity * explosive.modifier;
						//explosion.damage_terrain = explosive.damage_terrain * explosive.modifier;
						//explosion.damage_type = explosive.damage_type;
						//default = explosive.ent_owner;
						//explosion.fire_amount = explosive.fire_amount;
						//explosion.smoke_amount = explosive.smoke_amount;
						//explosion.smoke_lifetime_multiplier = explosive.smoke_lifetime_multiplier;
						//explosion.smoke_velocity_multiplier = explosive.smoke_velocity_multiplier;
						//explosion.flash_duration_multiplier = explosive.flash_duration_multiplier;
						//explosion.flash_intensity_multiplier = explosive.flash_intensity_multiplier;
						//explosion.sparks_amount = explosive.sparks_amount;
						//explosion.volume = explosive.volume;
						//explosion.pitch = explosive.pitch;
						//explosion.shake_multiplier = explosive.shake_multiplier;
						//explosion.force_multiplier = explosive.force_multiplier;
						//explosion.stun_multiplier = explosive.stun_multiplier;

						explosion.radius = Maths.Lerp01(explosive.radius * 0.25f, explosive.radius, explosive.modifier);
						explosion.power = Maths.Lerp01(explosive.power * 0.50f, explosive.power, explosive.modifier);

						explosion.damage_entity = explosive.damage_entity * explosive.modifier;
						explosion.damage_terrain = explosive.damage_terrain * explosive.modifier;

						explosion.damage_type = explosive.damage_type;
						explosion.damage_type_secondary = explosive.damage_type_secondary;

						explosion.shake_multiplier = explosive.shake_multiplier;
						explosion.force_multiplier = explosive.force_multiplier;
						explosion.stun_multiplier = explosive.stun_multiplier;
						explosion.terrain_radius_multiplier = explosive.terrain_radius_multiplier;

						explosion.smoke_amount = explosive.smoke_amount;
						explosion.smoke_lifetime_multiplier = explosive.smoke_lifetime_multiplier;
						explosion.smoke_velocity_multiplier = explosive.smoke_velocity_multiplier;
						explosion.smoke_color = explosive.smoke_color;

						explosion.flash_duration_multiplier = explosive.flash_duration_multiplier;
						explosion.flash_intensity_multiplier = explosive.flash_intensity_multiplier;

						explosion.fire_amount = explosive.fire_amount;
						explosion.sparks_amount = explosive.sparks_amount;

						explosion.volume = explosive.volume;
						explosion.pitch = explosive.pitch;

						//explosion.ent_owner = explosive.ent_owner;
						if (explosive.flags.HasAny(Explosive.Flags.No_Self_Damage)) explosion.ent_ignored = entity;

						explosion.Sync(entity, true);
					}

					return entity;
				});
			}
			else
			{
				return default;
			}
		}
#endif

		//[MethodImpl(MethodImplOptions.NoInlining)]
		// TODO: split this into multiple methods
		[ISystem.VeryLateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnUpdate(ISystem.Info info, ref Region.Data region, Entity entity, ref XorRandom random,
		[Source.Owned] ref Transform.Data transform, 
		[Source.Owned] ref Explosion.Data explosion, [Source.Owned] ref Explosion.State explosion_state,
		[Source.Owned, Pair.Component<Explosion.Data>] ref Light.Data light)
		{
			const bool debug_show_radius = false;
			const bool debug_print_results = false;

			const bool use_quadratic_falloff = false;

			static float GetDistanceMult(float distance, float radius_inv)
			{
				if (use_quadratic_falloff)
				{
					return (1.00f - (distance * radius_inv).Clamp01()).Pow2();
				}
				else
				{
					return 1.00f - (distance * radius_inv).Clamp01().Pow2();
				}
			}

			if (explosion_state.t_elapsed_wait < explosion.delay)
			{
				explosion_state.t_elapsed_wait += info.DeltaTime;
				return;
			}

			if (explosion.flags.HasAny(Explosion.Flags.Done) || explosion.power <= 0.01f || explosion.radius <= 0.01f)
			{
#if SERVER
				if (explosion.flags.TryAddFlag(Explosion.Flags.Deleted))
				{
					entity.Delete();
				}
#endif

				return;
			}

#if CLIENT
			var cull_small = !Camera.IsVisible(Camera.CullType.Rect1x, transform.position);
			var cull_mid = !Camera.IsVisible(Camera.CullType.Rect2x, transform.position);
			var cull_large = !Camera.IsVisible(Camera.CullType.Rect4x, transform.position);
#endif

			ref var terrain = ref region.GetTerrain();
			explosion_state.counter++;

			var radius = Maths.Max(1.00f, explosion.radius);
			var magnitude = Maths.Max(1.00f, MathF.Log2(radius));

			var scale_0 = magnitude * 0.35f;
			//var scale_1 = Maths.Max(1.00f, (0.50f + magnitude).Pow2() * (explosion.power * 0.50f));
			var scale_1 = Maths.Max(1.00f, (magnitude).Pow2() * (explosion.power * 0.50f));
			//var scale_1 = Maths.Max(1.00f, (0.25f + magnitude).Pow(1.50f) * (explosion.power * 0.50f));
			//var scale_1 = Maths.Max(1.00f, (0.50f + magnitude).Pow2() * (explosion.power * 0.50f));
			//var scale_1 = Maths.Max(1.00f, (0.50f + magnitude).Pow2() * (explosion.power * 1.00f));
			var scale_2 = Maths.Sqrt(scale_1);

			var duration = scale_2;
			var radius_terrain = radius * explosion.terrain_radius_multiplier;

			//var is_above_map = transform.position.Y <= 0.00f;

			//explosion.velocity = new Vec2f(40, -20);
			if (explosion.velocity) transform.AddOffset(explosion.velocity * App.fixed_update_interval_s);

			var sg_cell_nearest_pos = transform.position;
			if (transform.position.Y <= 0.00f)
			{

			}
			else
			{
				if (region.TryGetAnyValidPathPos(ref sg_cell_nearest_pos, include: Terrain.PathFlags.Background | Terrain.PathFlags.Ground | Terrain.PathFlags.Wall, exclude: Terrain.PathFlags.Solid, max_distance: 6.00f, allow_empty: true))
				{
					//region.DrawDebugLine(transform.position, sg_cell_nearest_pos, color: Color32BGRA.Red, thickness: 4);
				}

				//ref var sg_cell_nearest = ref terrain.GetNearestCellNonSolid2(transform.position, threshold: 0.50f);
				//if (sg_cell_nearest.IsNotNull())
				//{
				//	//var sg_cell_nearest_pos = sg_cell_nearest.IsNull() ? transform.position : (transform.position + sg_cell_nearest.GetWorldPositionMid()) * 0.50f;
				//	sg_cell_nearest_pos = sg_cell_nearest.GetWorldPositionMid();
				//}
			}

			if (debug_show_radius)
			{
				region.DrawDebugLine(transform.position, sg_cell_nearest_pos, color: Color32BGRA.Red, thickness: 4);
				region.DrawDebugCircle(sg_cell_nearest_pos, radius, color: Color32BGRA.Red, thickness: 3.00f);
				region.DrawDebugCircle(sg_cell_nearest_pos, radius_terrain, color: Color32BGRA.Yellow, thickness: 3.00f);
			}

#if SERVER
			if (explosion_state.counter == 1)
			{
				//App.WriteLine($"{explosion.radius}; {explosion.power}; {radius}; {magnitude}; {scale_0}; {scale_1}; {scale_2}; {explosion.damage_entity}; {explosion.damage_terrain}; {explosion.terrain_radius_multiplier}", color: App.Color.Cyan);
				//App.WriteValue((explosion.radius, explosion.power, radius, magnitude, scale_0, scale_1, scale_2, explosion.damage_entity, explosion.damage_terrain, explosion.terrain_radius_multiplier), color: App.Color.Cyan);
			}
#endif

			if (explosion_state.counter == 8 && explosion.force_multiplier > 0.00f)
			{
				var shockwave_radius = Maths.Sqrt(radius * explosion.power) * 5.00f; // * 0.0f;
																					 //App.WriteValue(shockwave_radius);
				var base_force = scale_2 * 2.00f;

				if (shockwave_radius >= 2.00f && base_force > 1.00f)
				{
					var shockwave_radius_inv = 1.00f / shockwave_radius;

					//App.WriteLine($"{shockwave_radius}; {shockwave_radius_inv}; {transform.position}");

					var tmp_buffer_capacity = Maths.Clamp(((int)(shockwave_radius * 3.00f)).NPOT(), 32, 512);
					Span<OverlapResult> tmp_buffer = stackalloc OverlapResult[tmp_buffer_capacity];

					//App.WriteValue(tmp_buffer_capacity);

					if (debug_show_radius)
					{
						region.DrawDebugCircle(sg_cell_nearest_pos, shockwave_radius, color: Color32BGRA.Cyan, thickness: 3.00f);
					}


					// Shockwave
					//if (false)
					{
						//region.DrawDebugCircle(transform.position, shockwave_radius, color: Color32BGRA.Red);
						var random_det = XorRandom.New(entity.GetLower(), (uint)explosion_state.counter);

						// entity shockwave/secondary damage
						var results = tmp_buffer;
						if (region.TryOverlapPointAll(world_position: sg_cell_nearest_pos, //transform.position,
						radius: shockwave_radius,
						hits: ref results,
						mask: Physics.Layer.Destructible | Physics.Layer.Resource,
						require: Physics.Layer.Dynamic,
						exclude: Physics.Layer.Static | Physics.Layer.Water | Physics.Layer.Fire | Physics.Layer.Essence | Physics.Layer.World | Physics.Layer.Bounds | Physics.Layer.Stored,
						group: explosion.ent_ignored))
						{
							results.SortByDistance();

							var damage_flags = Damage.Flags.No_Loot_Pickup | Damage.Flags.No_Loot_Notification;
							//damage_flags.SetFlag(Damage.Flags.No_Sound, results.Length > 8);

							//App.WriteLine($"{results.Length}/{tmp_buffer_capacity}; {MathF.Pow(radius, 1.35f)}; {shockwave_radius}");
							foreach (ref var result in results)
							{
								//if (result.entity != explosion.ent_ignored)
								{
									var is_los = result.distance <= 0.375f || terrain.IsLOS(a: result.world_position,
										b: sg_cell_nearest_pos,
										threshold_solid: 0.60f,
										allow_inside: true);

									//var dir = hit.gradient.GetNormalized();
									//dir.Y = MathF.CopySign(dir.Y, 1);
									var delta = result.world_position - sg_cell_nearest_pos;
									//if (result.layer.HasAny(Physics.Layer.Dynamic))
									//{
									delta.Y -= 3.00f;
									//}

									var dir = delta.GetNormalized();

									//var dist_mult = MathF.Sqrt(1.00f - Maths.Clamp01(result.distance * shockwave_radius_inv));
									//var dist_mult = 1.00f - Maths.Clamp01(result.distance * shockwave_radius_inv).Pow2();
									//var dist_mult = (1.00f - (result.distance * shockwave_radius_inv).Clamp01()).Pow2();
									var dist_mult = GetDistanceMult(result.distance, shockwave_radius_inv);
									//var force = dir * (Maths.Min(base_force, result.GetMass() * 50.00f * 50) * magnitude * 35.00f * (1.00f - (Maths.Max(result.distance * 0.50f, 0) * shockwave_radius_inv)));
									//var force = (Maths.Clamp(base_force, result.GetMass() * App.tickrate, result.GetMass() * 50.00f * App.tickrate) * magnitude * 35.00f * dist_mult);



									var area = result.GetShapeArea();
									var area_modifier = Maths.SumRatio(base_force * explosion.force_multiplier, area).Pow2();

									var dist_mult_base = dist_mult;


									//var base_force_clamped = Maths.Clamp(base_force, result.GetMass() * 0.01f, result.GetMass() * 50.00f);

									var result_mass = result.GetMass();
									var base_force_adjusted = Maths.Clamp(base_force, result_mass * area_modifier, result_mass * area_modifier * 50.00f);
									if (!is_los)
									{
										dist_mult *= dist_mult * 0.20f;
										//dist_mult *= dist_mult;
										base_force_adjusted *= 0.40f;
									}

									var force_mult = magnitude * 35.00f * dist_mult_base;
									//App.WriteValue((result.entity.GetName(), shockwave_radius, scale_0, scale_1, scale_2, magnitude, base_force, base_force_adjusted, area, area_modifier));

									//App.WriteLine($"grad: {hit.gradient}; force: {force}; {hit.entity}; mass: {hit.GetMass()}; pos: {hit.world_position}; dir: {dir}; dist: {hit.distance}; radius: {shockwave_radius}; radius_inv: {shockwave_radius_inv}; transform: {transform.position}");
									base_force_adjusted *= random_det.NextFloatExtra(0.80f, 0.20f);

									//if (result.layer.HasAny(Physics.Layer.Dynamic))
									//{
									//	result.AddForce(force); //d, hit.world_position);
									//}

									//App.WriteLine(force);

									//App.WriteLine($"{result.entity.GetName()}: {is_los}; {scale_2}; {force_mult}; {dist_mult}");

									if (result.layer.HasAny(Physics.Layer.Destructible) && scale_2 > 2.00f && explosion.damage_entity > 10.00f)
									{
#if SERVER
										//Damage.Hit(entity, explosion.ent_owner, hit.entity, hit.world_position, dir, -dir, MathF.Pow(1.00f - (Maths.Max(hit.distance, 0) * shockwave_radius_inv), 1.50f) * force.Length() * 0.10f, hit.material_type, Damage.Type.Shockwave, knockback: 1.00f, speed: scale_1);

										//var damage = (1.00f - (result.distance * shockwave_radius_inv).Clamp01().Pow(1.50f)) * base_force * 1.50f * force_mult;
										var damage = (1.00f - (result.distance * shockwave_radius_inv).Clamp01().PowFast(1.50f)) * Maths.Min(explosion.damage_entity, (1.00f + area).Sqrt()) * force_mult;
										//var damage = dist_mult_base * base_force * 0.50f * force_mult;
										var stun = explosion.power * 2.00f * explosion.stun_multiplier * (0.50f + dist_mult_base);

										//App.WriteLine($"{result.entity.GetName()}: {is_los}; {scale_2}; {force_mult}; {dist_mult}; stun: {stun}; impulse: {base_force_adjusted * force_mult}; mass: {result.GetMass()}; dmg: {damage}");

										Damage.Hit(ent_attacker: entity, ent_owner: default, ent_target: result.entity,
											position: result.world_position, velocity: dir * scale_1 * dist_mult, normal: -dir,
											damage_integrity: damage, damage_durability: damage, damage_terrain: damage,
											target_material_type: result.material_type, damage_type: explosion.damage_type_secondary,
											yield: 0.70f, size: 3.00f, impulse: base_force_adjusted * force_mult,
											flags: damage_flags, stun: stun);
#endif
									}
									else
									{
										result.AddForceWorld(force: dir * base_force_adjusted * force_mult, world_position: result.world_position); //d, hit.world_position);
									}
								}
							}
						}

#if SERVER
						// Terrain shockwave (for dust)
						results = tmp_buffer;
						if (region.TryOverlapPointAll(
						world_position: sg_cell_nearest_pos, // transform.position,
						radius: shockwave_radius * 0.40f,
						hits: ref results,
						mask: Physics.Layer.World,
						require: Physics.Layer.World,
						exclude: Physics.Layer.Dynamic | Physics.Layer.Water | Physics.Layer.Fire | Physics.Layer.Gas | Physics.Layer.Essence,
						query_flags: Physics.QueryFlag.Static))
						{
							results.SortByDistance();

							//var damage_flags = Damage.Flags.No_Loot_Pickup;
							//damage_flags.SetFlag(Damage.Flags.No_Sound, results.Length > 8);

							//var i = 0;
							//App.WriteLine($"{results.Length}/{tmp_buffer_capacity}; {MathF.Pow(radius, 1.35f)}; {shockwave_radius}");

							var effect_chance = 1.00f;
							foreach (ref var result in results)
							{
								//var dir = hit.gradient.GetNormalized();
								//dir.Y = MathF.CopySign(dir.Y, 1);
								var delta = result.world_position - sg_cell_nearest_pos;
								var dir = delta.GetNormalized();

								var shape_area = result.GetShapeArea();
								//var dist_mult = MathF.Sqrt(1.00f - Maths.Clamp01(result.distance * shockwave_radius_inv));
								//var dist_mult = 1.00f - Maths.Clamp01(result.distance * shockwave_radius_inv).Pow2();
								//var dist_mult = (1.00f - (result.distance * shockwave_radius_inv).Clamp01()).Pow2();
								var dist_mult = GetDistanceMult(result.distance, shockwave_radius_inv);

								//var force = dir * (Maths.Min(base_force, result.GetMass() * 50.00f * 50) * magnitude * 35.00f * (1.00f - (Maths.Max(result.distance * 0.50f, 0) * shockwave_radius_inv)));
								var force = (base_force * magnitude * 35.00f * dist_mult);
								//App.WriteLine($"grad: {hit.gradient}; force: {force}; {hit.entity}; mass: {hit.GetMass()}; pos: {hit.world_position}; dir: {dir}; dist: {hit.distance}; radius: {shockwave_radius}; radius_inv: {shockwave_radius_inv}; transform: {transform.position}");
								force *= random_det.NextFloatExtra(0.80f, 0.20f);

								var damage = Maths.PowFast(1.00f - (Maths.Max(result.distance, 0) * shockwave_radius_inv), 1.50f) * force * 0.10f;

								//var damage_flags_tmp = damage_flags;

								var damage_flags = Damage.Flags.No_Loot_Pickup | Damage.Flags.No_Particles | Damage.Flags.No_Sound | Damage.Flags.No_Loot_Notification;
								if (damage_flags.TryRemoveFlag(Damage.Flags.No_Particles | Damage.Flags.No_Sound, random.NextBool(0.50f * shape_area * dist_mult * effect_chance)))
								{
									effect_chance *= 0.80f;
								}

								Damage.Hit(ent_attacker: entity, ent_owner: default, ent_target: result.entity,
									position: result.world_position, velocity: dir * scale_1 * dist_mult, normal: -dir, // Vector2.Reflect(dir, result.normal_raw)
									damage_integrity: damage, damage_durability: damage, damage_terrain: damage,
									target_material_type: result.material_type, damage_type: explosion.damage_type_secondary,
									yield: 0.70f, size: 3.00f, impulse: force, flags: damage_flags);
							}
						}
#endif
					}
				}
			}

#if SERVER
			// Post-explosion for chain reactions
			if (explosion_state.counter == 5 && explosion.damage_entity > 10.00f)
			{
				var radius2 = radius * 0.25f;
				var radius2_inv = 1.00f / radius2;

				var base_force = scale_2 * 5.00f * explosion.force_multiplier;

				var tmp_buffer_capacity = (int)Maths.Clamp(((int)Maths.Pow(radius2, 1.35f)).NPOT(), 32, 512);
				Span<OverlapResult> tmp_buffer = stackalloc OverlapResult[tmp_buffer_capacity];

				//Span<OverlapResult> hits = stackalloc OverlapResult[tmp_buffer_capacity];

				var results = tmp_buffer;
				if (region.TryOverlapPointAll(world_position: sg_cell_nearest_pos, //transform.position,
				radius: radius2,
				hits: ref results,
				mask: Physics.Layer.Destructible | Physics.Layer.Resource,
				exclude: Physics.Layer.Essence | Physics.Layer.Fire | Physics.Layer.Water | Physics.Layer.World,
				group: explosion.ent_ignored))
				{
					foreach (ref var result in results)
					{
						//if (result.entity != explosion.ent_ignored && terrain.IsLOS(sg_cell_nearest_pos, result.world_position, allow_inside: true))
						if (result.distance <= 0.50f || terrain.IsLOS(sg_cell_nearest_pos, result.world_position, allow_inside: true))
						{
							//var dir = (hit.world_position - transform.position).GetNormalized();
							//Damage.Hit(entity, explosion.ent_owner, hit.entity, hit.world_position, -hit.gradient, hit.gradient, (1.00f - (Maths.Max(hit.distance, 0) * radius2_inv)) * explosion.damage_entity * 0.50f, hit.material_type, explosion.damage_type, knockback: 0.00f, speed: scale_1);

							var delta = result.world_position - sg_cell_nearest_pos; //transform.position;
							delta.Y -= 3.00f;
							var dir = delta.GetNormalized();

							//var dist_mult = 1.00f - Maths.Clamp01(result.distance * radius2_inv).Pow2();
							//var dist_mult = (1.00f - (result.distance * radius2_inv).Clamp01()).Pow2();
							var dist_mult = GetDistanceMult(result.distance, radius2_inv);

							var damage = explosion.damage_entity * 0.50f * dist_mult;
							var force = (base_force * magnitude * 35.00f * dist_mult);

							Damage.Hit(ent_attacker: entity, ent_owner: default, ent_target: result.entity,
								position: result.world_position, velocity: dir * scale_1 * dist_mult, normal: -result.gradient,
								damage_integrity: damage, damage_durability: damage, damage_terrain: damage,
								target_material_type: result.material_type, damage_type: explosion.damage_type,
								yield: 0.80f, size: 1.00f, impulse: force, stun: explosion.stun_multiplier, flags: Damage.Flags.No_Loot_Pickup | Damage.Flags.No_Loot_Notification);
						}
					}
				}
			}
#endif

#if SERVER
			// Split into extra explosions if big enough
			if (explosion.flags.HasNone(Explosion.Flags.No_Split) && explosion_state.counter % 4 == 0) // && random.NextBool(0.50f))
			{
				var scale_1_b = Maths.Pow2(magnitude) * explosion.power_rem;
				if (scale_1_b > 20.00f)
				{
					//App.WriteLine($"{explosion.power_rem}/{explosion.power}; {scale_1_b}");

					for (var i = 0; i < 2; i++)
					{
						var vel = random.NextUnitVector2Range(radius * 0.50f, radius * 0.75f);
						var pos_hit = sg_cell_nearest_pos + vel;

						if (region.TryLinecast(world_position_start: sg_cell_nearest_pos, world_position_end: pos_hit, radius: 1.00f, result: out var result, layer: Physics.Layer.Solid, mask: Physics.Layer.World, exclude: Physics.Layer.Dynamic, query_flags: Physics.QueryFlag.Static))
						{
							pos_hit = result.world_position;
						}

						// TODO: this is dumb
						var explosion_tmp = new Explosion.Data();
						explosion_tmp.power = explosion.power_rem;
						explosion_tmp.radius = radius * 0.75f;
						explosion_tmp.damage_entity = explosion.damage_entity * 0.75f;
						explosion_tmp.damage_terrain = explosion.damage_terrain * 0.75f;
						explosion_tmp.damage_type = explosion.damage_type;
						explosion_tmp.damage_type_secondary = explosion.damage_type_secondary;
						//explosion_tmp.ent_owner = explosion.ent_owner;
						explosion_tmp.fire_amount = explosion.fire_amount * 0.50f;
						explosion_tmp.smoke_amount = explosion.smoke_amount;
						explosion_tmp.smoke_lifetime_multiplier = explosion.smoke_lifetime_multiplier;
						explosion_tmp.smoke_velocity_multiplier = explosion.smoke_velocity_multiplier;
						explosion_tmp.flash_duration_multiplier = explosion.flash_duration_multiplier;
						explosion_tmp.flash_intensity_multiplier = explosion.flash_intensity_multiplier;
						explosion_tmp.sparks_amount = explosion.sparks_amount * 0.25f;
						explosion_tmp.volume = explosion.volume * 0.75f;
						explosion_tmp.pitch = explosion.pitch * 0.85f;
						explosion_tmp.shake_multiplier = explosion.shake_multiplier;
						explosion_tmp.terrain_radius_multiplier = explosion.terrain_radius_multiplier;

						region.SpawnPrefab("explosion", Vector2.Lerp(sg_cell_nearest_pos, pos_hit, random.NextFloatRange(0.80f, 1.00f))).ContinueWith(ent =>
						{
							ref var explosion = ref ent.GetComponent<Explosion.Data>();
							if (explosion.IsNotNull())
							{
								explosion.power = explosion_tmp.power;
								explosion.radius = explosion_tmp.radius;
								explosion.damage_entity = explosion_tmp.damage_entity;
								explosion.damage_terrain = explosion_tmp.damage_terrain;
								explosion.damage_type = explosion_tmp.damage_type;
								explosion.damage_type_secondary = explosion_tmp.damage_type_secondary;
								//explosion.ent_owner = explosion_tmp.ent_owner;
								explosion.fire_amount = explosion_tmp.fire_amount;
								explosion.smoke_amount = explosion_tmp.smoke_amount;
								explosion.smoke_lifetime_multiplier = explosion_tmp.smoke_lifetime_multiplier;
								explosion.smoke_velocity_multiplier = explosion_tmp.smoke_velocity_multiplier;
								explosion.flash_duration_multiplier = explosion_tmp.flash_duration_multiplier;
								explosion.flash_intensity_multiplier = explosion_tmp.flash_intensity_multiplier;
								explosion.sparks_amount = explosion_tmp.sparks_amount;
								explosion.volume = explosion_tmp.volume;
								explosion.pitch = explosion_tmp.pitch;
								explosion.ent_ignored = explosion_tmp.ent_ignored;
								explosion.shake_multiplier = explosion_tmp.shake_multiplier;
								explosion.terrain_radius_multiplier = explosion_tmp.terrain_radius_multiplier;

								explosion.Sync(ent, true);
							}
						});
					}

					explosion.power_rem *= 0.60f;
				}
			}
#endif

			if (explosion.flags.HasAny(Explosion.Flags.Exploded))
			{
#if CLIENT
				var age_mult = Maths.PowFast(1.00f - Maths.Normalize01(explosion_state.age, Maths.Clamp(duration * 0.35f, 0.50f, 4.00f)), 0.50f);
				var age_mult_b = Maths.PowFast(1.00f - Maths.Normalize01(explosion_state.age, Maths.Clamp(duration * 0.35f, 0.50f, 2.00f * explosion.flash_duration_multiplier)), 0.50f);

				light.intensity = magnitude * age_mult_b * age_mult_b * 2.00f * explosion.flash_intensity_multiplier;
				light.scale = Vec2f.One * radius * 16.00f * (0.50f + (0.50f + age_mult));

				//if (info.WorldTime >= explosion.next_smoke)
				if (explosion.smoke_amount > 0.01f && explosion.smoke_lifetime_multiplier > 0.01f)
				{
					var smoke_color_a0 = explosion.smoke_color with { a = 240 };
					var smoke_color_a1 = explosion.smoke_color.WithColorMult(0.50f).WithAlphaMult(0.85f);

					var smoke_color_b0 = smoke_color_a0 with { a = 0 };
					var smoke_color_b1 = smoke_color_b0 with { a = 0 };

					var smokes_count = (uint)explosion_state.smokes_count;
					for (var i = 0u; i < smokes_count; i++)
					{
						ref var smoke = ref explosion_state.smokes[i];

						//explosion.next_smoke = info.WorldTime + 0.50f;
						//var smoke_pos = explosion.smokes[random.NextIntRange(0, explosion.smokes_count - 1)];

						//Particle.Spawn(ref region, new Particle.Data()
						//{
						//	texture = texture_smoke,
						//	lifetime = (0) + 0.20f + random.NextFloatRange(10.00f, 15.00f),
						//	pos = smoke.pos, // transform.position + random.NextVector2Range(new Vector2(-0.50f, 0), new Vector2(0.50f, 0)),
						//	vel = new Vector2(0.00f, -0.00f),
						//	fps = random.NextByteRange(6, 10),
						//	frame_count = 64,
						//	frame_offset = random.NextByteRange(0, 64),
						//	frame_count_total = 64,
						//	scale = random.NextFloatRange(0.50f, 0.95f),
						//	rotation = random.NextFloat(MathF.PI),
						//	angular_velocity = random.NextFloat(MathF.PI * 0.10f),
						//	growth = random.NextFloatRange(0.20f, 0.40f),
						//	color_a = random.NextColor32Range(new Color32BGRA(170, 180, 180, 180), new Color32BGRA(100, 150, 150, 150)),
						//	color_b = random.NextColor32Range(new Color32BGRA(0, 100, 100, 100), new Color32BGRA(0, 80, 80, 80)),
						//	force = new Vector2(random.NextFloatRange(6.50f, 8.00f), -4.00f),
						//	drag = 0.04f
						//});


						if (smoke.lifetime > 0.10f)
						{
							if (info.WorldTime >= smoke.next_smoke)
							{
								var alpha = Maths.Clamp01(-smoke.vel.Y * 0.04f);
								if (alpha > 0.10f)
								{
									Particle.Spawn(ref region, new Particle.Data()
									{
										texture = texture_smoke,
										lifetime = random.NextFloatRange(smoke.lifetime * 1.25f, smoke.lifetime * 2.50f) * explosion.smoke_lifetime_multiplier,
										pos = smoke.pos + random.NextVector2(1), // transform.position + random.NextVector2Range(new Vector2(-0.50f, 0), new Vector2(0.50f, 0)),
										vel = smoke.vel * 0.80f * (0.25f + (1 - alpha)),
										fps = random.NextByteRange(6, 12),
										frame_count = 64,
										frame_offset = random.NextByteRange(0, 64),
										frame_count_total = 64,
										scale = random.NextFloatRange(0.90f, 1.15f) * Maths.Clamp01(0.40f + (smoke.lifetime * 0.20f)) * (0.10f + alpha),
										rotation = random.NextFloat(MathF.PI * 2),
										angular_velocity = random.NextFloat(MathF.PI * 0.40f),
										growth = random.NextFloatRange(0.10f, 0.30f),
										//color_a = random.NextColor32Range(new Color32BGRA(255, 220, 220, 220), new Color32BGRA(240, 120, 120, 120)).WithAlphaMult(Maths.Clamp01(smoke.lifetime * 0.20f * alpha) * 0.80f),
										color_a = random.NextColor32Range(smoke_color_a0, smoke_color_a1).WithAlphaMult(Maths.Clamp01(smoke.lifetime * 0.20f * alpha) * 0.80f),
										//color_b = random.NextColor32Range(new Color32BGRA(0, 200, 200, 200), new Color32BGRA(0, 150, 150, 150)),
										color_b = random.NextColor32Range(smoke_color_b0, smoke_color_b1),
										force = new Vector2(random.NextFloatRange(1.20f, 2.00f), random.NextFloatRange(-1.00f, 1.00f * (1 - alpha))),
										drag = random.NextFloatRange(0.02f, 0.03f)
									});
								}

								smoke.next_smoke = info.WorldTime + random.NextFloatRange(0.05f, 0.06f);
							}

							smoke.lifetime -= App.fixed_update_interval_s;
						}

						smoke.vel *= 0.955f;
						smoke.vel += Region.gravity * App.fixed_update_interval_s * 0.50f;
						smoke.pos += smoke.vel * App.fixed_update_interval_s;
						smoke.pos += random.NextUnitVector2Range(0.25f, 0.75f);
						smoke.vel += random.NextUnitVector2Range(0.90f, 1.10f);
					}

					//if (info.WorldTime >= explosion.next_smoke)
					//{
					//	explosion.next_smoke = info.WorldTime + 0.10f;
					//}
				}

				if (explosion.shake_multiplier > 0.02f)
				{
					Shake.Emit(ref region, sg_cell_nearest_pos, explosion.shake_multiplier * (age_mult * age_mult), 0.20f + (0.60f * age_mult * age_mult), radius: (2.00f + (radius * radius)) * (explosion.power * 0.50f));
				}
#endif

				explosion_state.age += info.DeltaTime;

#if SERVER
				if (explosion_state.age > duration)
				{
					explosion.flags.AddFlag(Explosion.Flags.Done);
				}
				//if (explosion.age >= 1.00f) entity.Delete();
#endif
			}
			else
			{
				var random_det = XorRandom.New(entity.GetLower() * (uint)explosion.radius.ToInt32BitCast(), (uint)explosion_state.counter);

				//App.WriteLine(scale_2);

				explosion.flags.AddFlag(Explosion.Flags.Exploded);
				explosion.power_rem = explosion.power * 0.50f;

#if CLIENT
				if (explosion.volume > 0.05f && explosion.pitch > 0.02f)
				{
					var h_sound = default(Sound.Handle);
					switch (explosion.damage_type)
					{
						case Damage.Type.Bullet_AC:
						case Damage.Type.Bullet_KN:
						case Damage.Type.Bullet_HW:
						case Damage.Type.Sledgehammer:
						{
							h_sound = snd_metal.GetRandom(ref random_det);
						}
						break;

						case Damage.Type.Bullet_HC:
						case Damage.Type.Bullet_MG:
						case Damage.Type.Bullet_Musket:
						case Damage.Type.Shrapnel:
						{
							h_sound = snd_shrapnel.GetRandom(ref random_det);
						}
						break;

						case Damage.Type.Corrosion:
						case Damage.Type.Steam:
						{
							h_sound = snd_steam.GetRandom(ref random_det);
						}
						break;

						case Damage.Type.Bullet_SG:
						case Damage.Type.Bullet_LC:
						case Damage.Type.Motion_Impulse:
						case Damage.Type.Shockwave:
						{
							h_sound = snd_fast.GetRandom(ref random_det);
						}
						break;

						case Damage.Type.Fire:
						case Damage.Type.Deflagration:
						case Damage.Type.Phlogiston:
						case Damage.Type.Heat:
						{
							h_sound = snd_gas.GetRandom(ref random_det);
						}
						break;

						case Damage.Type.Blunt:
						case Damage.Type.Impact:
						case Damage.Type.Crush:
						case Damage.Type.Ram:
						{
							h_sound = snd_poof.GetRandom(ref random_det);
						}
						break;

						case Damage.Type.Explosion:
						default:
						{
							h_sound = sounds.GetRandom(ref random_det);
						}
						break;
					}

					Sound.Play(sound: h_sound,
						world_position: transform.position,
						volume: (1.50f * scale_0) * explosion.volume,
						pitch: Maths.Max((1.50f / (scale_0 * (explosion.power * 0.40f))) * explosion.pitch, 0.25f),
						size: radius * 2.00f,
						priority: 0.30f,
						dist_multiplier: scale_1);
				}

				if (explosion.volume > 0.05f && explosion.pitch > 0.02f && explosion.damage_type_secondary != Damage.Type.None && explosion.damage_type_secondary != explosion.damage_type)
				{
					var h_sound = default(Sound.Handle);
					switch (explosion.damage_type_secondary)
					{
						case Damage.Type.Bullet_AC:
						case Damage.Type.Bullet_KN:
						case Damage.Type.Bullet_HW:
						case Damage.Type.Sledgehammer:
						{
							h_sound = snd_metal.GetRandom(ref random_det);
						}
						break;

						case Damage.Type.Bullet_HC:
						case Damage.Type.Bullet_MG:
						case Damage.Type.Bullet_Musket:
						case Damage.Type.Shrapnel:
						{
							h_sound = snd_shrapnel.GetRandom(ref random_det);
						}
						break;

						case Damage.Type.Corrosion:
						case Damage.Type.Steam:
						{
							h_sound = snd_steam.GetRandom(ref random_det);
						}
						break;

						case Damage.Type.Bullet_SG:
						case Damage.Type.Bullet_LC:
						case Damage.Type.Motion_Impulse:
						case Damage.Type.Shockwave:
						{
							h_sound = snd_fast.GetRandom(ref random_det);
						}
						break;

						case Damage.Type.Fire:
						case Damage.Type.Deflagration:
						case Damage.Type.Phlogiston:
						{
							h_sound = snd_gas.GetRandom(ref random_det);
						}
						break;

						case Damage.Type.Heat:
						case Damage.Type.Blunt:
						case Damage.Type.Impact:
						case Damage.Type.Crush:
						case Damage.Type.Ram:
						{
							h_sound = snd_poof.GetRandom(ref random_det);
						}
						break;

						case Damage.Type.Explosion:
						default:
						{
							h_sound = sounds.GetRandom(ref random_det);
						}
						break;
					}

					Sound.Play(sound: h_sound,
						world_position: transform.position,
						volume: (0.70f * scale_0) * explosion.volume,
						pitch: Maths.Max((1.50f / (scale_0 * (explosion.power * 0.40f))) * explosion.pitch * 1.30f, 0.25f),
						size: radius,
						priority: 0.38f,
						dist_multiplier: scale_1 * 0.75f);
				}

				if (explosion.shake_multiplier > 0.02f)
				{
					Shake.Emit(region: ref region,
						world_position: transform.position,
						trauma: Maths.Clamp01(scale_1 * 0.75f) * explosion.shake_multiplier,
						max: 1.00f,
						radius: (2.00f + (radius * radius)) * explosion.power);
				}
#endif

#if SERVER
				if (explosion.damage_terrain > 10.00f || explosion.damage_entity > 10.00f)
				{
					var tmp_buffer_capacity = (int)Maths.Clamp(((int)Maths.PowFast(radius_terrain, 1.50f)).NPOT(), 64, 512);
					Span<OverlapResult> tmp_buffer = stackalloc OverlapResult[tmp_buffer_capacity];

					//Experience.Add(explosion.ent_owner, Experience.Type.Explosives, (int)random.NextFloatRange(0.00f, (MathF.Pow(explosion.power, magnitude))));

					if (explosion.damage_terrain > 10.00f && radius_terrain >= 0.50f)
					{
						var radius_inv = 1.00f / radius_terrain;

						var ts = Timestamp.Now();

						var results = tmp_buffer;
						if (region.TryOverlapPointAll(world_position: sg_cell_nearest_pos,
						radius: radius_terrain,
						hits: ref results,
						mask: Physics.Layer.World,
						require: Physics.Layer.World,
						exclude: Physics.Layer.Dynamic | Physics.Layer.Water | Physics.Layer.Fire | Physics.Layer.Gas | Physics.Layer.Bounds,
						query_flags: Physics.QueryFlag.Static))
						{
							results.Sort();
							var ts_end = Timestamp.Now();

							if (debug_print_results) App.WriteLine($"{results.Length}/{tmp_buffer_capacity}; {radius_terrain}; {ts.GetMilliseconds(ts_end):0.0000} ms", color: App.Color.Yellow);
							var pos_prev = sg_cell_nearest_pos;
							var effect_chance = 1.00f;

							for (var i = 0; i < results.Length; i++)
							{
								//App.WriteLine($"{result.material_type}; {result.distance}; {result.GetShapeArea()}; {result.gradient} == {result.gradient.GetNormalized()}; {result.normal_raw}");
								ref var result = ref results[i];

								var shape_area = result.GetShapeArea();
								var distance_ratio = Maths.Clamp01(result.distance * radius_inv);
								var distance_modifier = (1.00f - distance_ratio).Pow2();

								var size_new = Maths.Clamp(scale_2 * shape_area * distance_modifier, shape_area, radius);
								if (size_new <= 0.25f) continue;

								//var cross = Maths.GetDirectionSimilarity(result.gradient, result.normal_raw); // Vec2f.Cross(result.normal_raw, result.gradient);
								//var damage = (1.00f - distance_ratio) * explosion.damage_terrain * 0.50f;
								var damage = distance_modifier * explosion.damage_terrain * 0.50f;


								//WorldNotification.Push(ref region, $"{result.distance:0}m; dmg: {damage:0}; scale2: {scale_2:0}", Color32BGRA.Green, result.world_position, lifetime: 8.00f);

								//var damage_flags = Damage.Flags.No_Loot_Pickup | Damage.Flags.No_Particles | Damage.Flags.No_Sound | Damage.Flags.No_Loot_Notification;
								//damage_flags.AddFlag(Damage.Flags.No_Particles | Damage.Flags.No_Sound, shape_area <= 0.10f);
								//var pass = result.GetShapeArea() >= 0.75f && (result.distance <= radius_terrain || terrain.IsLOS(sg_cell_nearest_pos, result.world_position + (result.normal_raw * 0.25f), allow_inside: true));

								var pos_new = result.world_position + (result.normal_raw * 0.50f);
								var pass = i == 0 || result.distance <= scale_2;
								var is_los = false;

								if (!pass)
								{
									pass = is_los = terrain.IsLOS(pos_prev, pos_new, allow_inside: true);
								}

								if (debug_print_results)
								{
									var size = scale_2 * 0.25f * radius_terrain;
									App.WriteLine($"{result.material_type}; dmg: {damage:0.00}; mag: {magnitude:0.00}; ratio: {distance_ratio:0.00}; mod: {distance_modifier:0.00} " +
										$"radius: {radius:0.00}; radius_terrain: {radius_terrain:0.00}; dist: {result.distance:0.00}; " +
										$"scale1: {scale_1:0.00}; scale2: {scale_2:0.00}; size: {size:0.00}; size_new: {size_new:0.00f}; area: {shape_area:0.00}; " +
										//$"gr: {result.gradient} == {result.gradient.GetNormalized()}; {result.normal_raw}" +
										"", color: pass ? App.Color.Green : App.Color.Red, show_stacktrace: false);
								}

								//App.WriteValue(result.distance);
								if (pass)
								{
									if (is_los) pos_prev = pos_new;
									//var dir = (hit.world_position - transform.position).GetNormalized();
									//Damage.Hit(entity, explosion.ent_owner, hit.entity, hit.world_position, -hit.gradient, hit.gradient, (1.00f - (Maths.Max(hit.distance, 0) * radius_inv)) * explosion.damage_terrain * 0.30f, hit.material_type, explosion.damage_type, size: scale_2 * 0.50f, speed: scale_1);

									var damage_flags = Damage.Flags.No_Loot_Pickup | Damage.Flags.No_Particles | Damage.Flags.No_Sound | Damage.Flags.No_Loot_Notification;
									if (damage_flags.TryRemoveFlag(Damage.Flags.No_Particles | Damage.Flags.No_Sound, random.NextBool(shape_area * distance_modifier * effect_chance * 0.00f)))
									{
										effect_chance *= 0.80f;
									}

									Damage.Hit(ent_attacker: entity, ent_owner: default, ent_target: result.entity,
										position: result.world_position, velocity: result.gradient * scale_1 * 2.25f, normal: -result.gradient, // Vector2.Reflect(result.normal_raw, result.gradient), // result.normal_raw, // -result.gradient,
										damage_integrity: damage, damage_durability: damage, damage_terrain: damage,
										target_material_type: result.material_type, damage_type: explosion.damage_type,
										yield: 0.80f, size: size_new, impulse: 0.00f, flags: damage_flags);
								}
								else
								{
									pos_prev = sg_cell_nearest_pos;
								}
							}
						}
					}

					if (explosion.damage_entity > 10.00f)
					{
						var radius_inv = 1.00f / radius;

						var results = tmp_buffer;
						if (region.TryOverlapPointAll(world_position: sg_cell_nearest_pos,
						radius: radius,
						hits: ref results,
						mask: Physics.Layer.Destructible,
						exclude: Physics.Layer.World | Physics.Layer.Essence | Physics.Layer.Water | Physics.Layer.Fire,
						group: explosion.ent_ignored))
						{
							//results.SortByDistance();

							foreach (ref var result in results)
							{
								//if (result.entity != explosion.ent_ignored && terrain.IsLOS(sg_cell_nearest_pos, result.world_position, allow_inside: true))
								if (result.distance <= 0.50f || terrain.IsLOS(sg_cell_nearest_pos, result.world_position, allow_inside: true))
								{
									//var dir = (hit.world_position - sg_cell_nearest_pos).GetNormalized();
									//Damage.Hit(entity, default, hit.entity, hit.world_position, -hit.gradient, hit.gradient, (1.00f - (Maths.Max(hit.distance, 0) * radius_inv)) * explosion.damage_entity, hit.material_type, explosion.damage_type, size: Maths.Min(scale_1, 30), speed: scale_1);

									//var dist_mult = 1.00f - Maths.Clamp01(result.distance * radius_inv).Pow2();
									//var dist_mult = (1.00f - (result.distance * radius_inv).Clamp01()).Pow2();
									var dist_mult = GetDistanceMult(result.distance, radius_inv);
									var damage = explosion.damage_entity * dist_mult;

									Damage.Hit(ent_attacker: entity, ent_owner: default, ent_target: result.entity,
										position: result.world_position, velocity: -result.gradient * scale_1, normal: result.gradient,
										damage_integrity: damage, damage_durability: damage, damage_terrain: damage,
										target_material_type: result.material_type, damage_type: explosion.damage_type,
										yield: 0.80f, size: Maths.Min(scale_1, 30) * dist_mult, impulse: explosion.force_multiplier * dist_mult, stun: explosion.stun_multiplier * dist_mult, flags: Damage.Flags.No_Loot_Pickup);
								}
							}
						}
					}
				}
#endif

				//{
				//	var shockwave_radius = radius * 2.50f;
				//	var shockwave_radius_inv = 1.00f / shockwave_radius;

				//	var hits = tmp_buffer;
				//	if (region.TryOverlapPointAll(sg_cell_nearest_pos, shockwave_radius, ref hits, mask: Physics.Layer.Dynamic, exclude: Physics.Layer.Essence))
				//	{
				//		foreach (ref var hit in hits)
				//		{
				//			hit.AddForce(hit.gradient.GetNormalized() * -(hit.GetMass() * 40.00f * hit.distance * 50.00f * shockwave_radius_inv));

				//			//var dir = (hit.world_position - sg_cell_nearest_pos).GetNormalized();
				//			//App.WriteLine($"{hit.gradient} vs {dir}");
				//		}
				//	}
				//}

#if CLIENT
				// Sparks
				if (!cull_mid && explosion.sparks_amount >= 0.01f)
				{
					var sparks_lifetime_mult = scale_0 * (0.20f + (explosion.sparks_amount * 0.20f));
					var sparks_force_mult = scale_0 * (explosion.force_multiplier * 0.75f);

					var sparks_count = (int)(explosion.sparks_amount * 25.00f);
					for (var i = 0; i < sparks_count; i++)
					{
						Particle.Spawn(ref region, new Particle.Data()
						{
							texture = texture_metal_spark,
							lifetime = 0.25f + (random.NextFloatRange(0.30f, 0.60f + (scale_1 * 0.25f)) * scale_1 * 0.15f * sparks_lifetime_mult),
							pos = transform.position + random.NextVector2(0.70f),
							vel = ((random.NextUnitVector2Range(0, 55 * scale_0) + new Vector2(0, -random.NextFloatRange(20.00f, 40.00f + (scale_1 * 1.50f)) * sparks_force_mult))),
							fps = 1,
							frame_count = 1,
							frame_offset = random.NextByteRange(0, 4),
							frame_count_total = 4,
							scale = random.NextFloatRange(0.70f, 0.90f),
							growth = -random.NextFloatRange(0.20f, 0.30f),
							force = new Vector2(0.00f, random.NextFloatRange(10.00f, 30.00f)),
							stretch = new Vector2(random.NextFloatRange(5.00f, 10.00f) * (0.50f + scale_1) * 0.04f, random.NextFloatRange(0.250f, 0.55f)),
							drag = random.NextFloatRange(0.001f, 0.03f),
							lit = random.NextFloatRange(0.80f, 1.40f),
							color_a = random.NextColorRange(ColorBGRA.White, 0xffffc0b0).WithAlphaMult(random.NextFloatExtra(0.90f, 0.40f)),
							color_b = random.NextColorRange(ColorBGRA.White with { b = 0.75f }, ColorBGRA.White with { b = 0.50f }),
							face_dir_ratio = 1.00f
						});
						//particle.color_a = random.NextColor32Range(0xffffffff, 0xffffc0b0);
						//particle.color_b = random.NextColor32Range(0xffffc0b0, 0xffffa090);
						//Particle.Spawn(ref region, particle);
					}
				}
#endif

				//#if SERVER
				//				terrain.HitMask(transform.position, new Vector2(64), tex_mask_explosion.GetRandom(ref random), new Vector2(1, 0), explosion.damage_terrain, 0.80f);
				//#endif

#if CLIENT
				if ((explosion.smoke_amount > 0.05f && explosion.smoke_lifetime_multiplier > 0.01f) || explosion.fire_amount > 0.05f)
#elif SERVER
				if (explosion.damage_terrain >= 10.00f)
#endif
				{
					//#if SERVER
					//					{
					//						terrain.HitMask(ent_attacker: entity,
					//							ent_owner: default,
					//							world_position: transform.position,
					//							world_position_spawn: sg_cell_nearest_pos,
					//							//size: new Vector2(random.NextFloatRange(0.90f, 1.10f), random.NextFloatRange(0.80f, 0.90f)) * radius2 * explosion.terrain_radius_multiplier * 4,
					//							//size: new Vector2(random.NextFloatRange(0.70f, 0.90f), random.NextFloatRange(0.85f, 1.05f)) * radius2 * explosion.terrain_radius_multiplier * 2.00f,
					//							size: new Vec2f(64),
					//							texture: tex_mask_explosion.GetRandom(ref random),
					//							direction: Vec2f.Down,
					//							spawn_flags: Resource.SpawnFlags.Merge,
					//							no_loot_pickup: true,
					//							damage: explosion.damage_terrain * 0.10f,
					//							yield: 0.80f);
					//					}
					//#endif


					var count_f32 = scale_1 * 0.75f;
					var count = (int)count_f32;
					if (count > 0)
					{
						var count_f32_inv = count_f32.Rcp();

#if SERVER
						//{
						//	terrain.HitMask(ent_attacker: entity,
						//		ent_owner: default,
						//		world_position: transform.position,
						//		world_position_spawn: sg_cell_nearest_pos,
						//		//size: new Vector2(random.NextFloatRange(0.90f, 1.10f), random.NextFloatRange(0.80f, 0.90f)) * radius2 * explosion.terrain_radius_multiplier * 4,
						//		//size: new Vector2(random.NextFloatRange(0.70f, 0.90f), random.NextFloatRange(0.85f, 1.05f)) * radius2 * explosion.terrain_radius_multiplier * 2.00f,
						//		size: new Vec2f(64)
						//		texture: tex_mask_explosion.GetRandom(ref random),
						//		direction: Vec2f.Down,
						//		spawn_flags: Resource.SpawnFlags.Merge,
						//		no_loot_pickup: true,
						//		damage: explosion.damage_terrain * 0.10f,
						//		yield: 0.80f);
						//}
#endif

#if CLIENT
						var smokes_count_max = (uint)Maths.Clamp((int)(count_f32 * explosion.smoke_amount), 0, explosion_state.smokes.Length);
						var smoke_accumulator = 0.00f;
						var fire_accumulator = 0.00f;

						var smoke_color_a0 = explosion.smoke_color;
						var smoke_color_a1 = explosion.smoke_color.WithColorDiv(Maths.Factor.x2).WithAlphaMult(0.75f);

						var smoke_color_b0 = smoke_color_a0.WithAlpha(0);
						var smoke_color_b1 = smoke_color_b0.WithAlpha(0);
#endif

						var i_f32 = 0.00f;
						for (var i = 0; i < count; i++, i_f32++)
						{
							var pos = transform.position + (random_det.NextUnitVector2Extra(0.25f, 0.30f) * i_f32);
							var dir = (pos - transform.position).GetNormalized(out var len);

							//App.WriteValue((count, i_f32, len, dir, pos));

							//var is_blocked = terrain.TryGridCast(pos: transform.position,
							var is_blocked = terrain.TryGridCast(pos: transform.position,
							dir: dir,
							hit_pos: out var hit_pos,
							dist: out var len_blocked,
							max_dist: len, // + 1.00f,
							threshold_solid: 0.80f,
							allow_inside: false);
							if (is_blocked)
							{
								//var len_blocked = Vector2.Distance(pos, hit_pos);
								//dir = Vector2.Reflect(dir, new Vector2(-MathF.Sign(dir.X), -MathF.Sign(dir.Y)));
								//len -= len_blocked;
							}

							var dir2 = new Vector2(dir.X, -Maths.Abs(dir.Y * 0.70f) + (dir.Y * 0.30f));
							var dir3 = new Vector2(dir.X + random_det.NextFloat(16.00f), -Maths.Abs(dir.Y * (i_f32 * count_f32_inv)) - random_det.NextFloat01()).GetNormalized();

							//App.WriteValue((count, i_f32, len, dir, pos, hit_pos, len_blocked));

#if SERVER
							if ((uint)i % 2 == 0)
							{
								var radius2 = (8.00f + ((explosion.power + (explosion.power * (i_f32 + 1.00f) * 0.750f))));
								var damage = (explosion.damage_terrain / (1.00f + len + (i_f32 * 0.20f))) * 1.00f;
								//App.WriteValue(radius2);
								//var hit_pos_b = hit_pos + (dir * 4.50f);
								//var hit_pos_b = hit_pos + (dir * 0.50f);
								var hit_pos_b = hit_pos + (dir * 0.25f);
								var size = random.NextVector2Range(0.90f, 1.10f) * radius2 * explosion.terrain_radius_multiplier * 1.00f;

								//region.DrawDebugLine(pos, hit_pos_b, color: Color32BGRA.Yellow, thickness: 16);
								//region.DrawDebugCircle(hit_pos_b, radius, color: Color32BGRA.Yellow, filled: false);

								//App.WriteValue((count, len_blocked, radius2, damage, size, transform.position - pos));


								//terrain.Hit(pos, dir, size, explosion.damage_terrain / (1.00f + len), 0.80f);
								//terrain.HitCircle(pos, dir, radius2, explosion.damage_terrain / (1.00f + len + (i * 0.50f)), 0.80f);
								terrain.HitMask(ent_attacker: entity,
									ent_owner: default,
									world_position: hit_pos_b,
									world_position_spawn: hit_pos,
									//size: new Vector2(random.NextFloatRange(0.90f, 1.10f), random.NextFloatRange(0.80f, 0.90f)) * radius2 * explosion.terrain_radius_multiplier * 4,
									//size: new Vector2(random.NextFloatRange(0.70f, 0.90f), random.NextFloatRange(0.85f, 1.05f)) * radius2 * explosion.terrain_radius_multiplier * 2.00f,
									size: size,
									texture: tex_mask_explosion.GetRandom(ref random),
									direction: dir,
									spawn_flags: Resource.SpawnFlags.Merge,
									no_loot_pickup: true,
									damage: damage,
									yield: 0.80f);
							}
#endif

#if CLIENT
							smoke_accumulator += explosion.smoke_amount;

							// Flying smokes
							if (i > 0 && explosion_state.smokes_count < smokes_count_max)
							{
								explosion_state.smokes[explosion_state.smokes_count++] = new SmokeNode(transform.position,
									dir3 * (10 + (len * random.NextFloatRange(1.10f, 1.30f) * scale_1)),
									explosion.smoke_amount * random.NextFloatRange(0.80f, 1.20f),
									explosion.smoke_lifetime_multiplier * random.NextFloatRange(6.00f, 7.00f), 0.00f);
							}

							if (!cull_mid)
							{
								// Fireball
								fire_accumulator += explosion.fire_amount;
								while (fire_accumulator >= 1.00f)
								{
									var lifetime = 0.50f + random.NextFloatRange(0.20f, Maths.Clamp(0.20f + (i_f32 * 0.030f), 0.10f, 2.00f));
									Particle.Spawn(ref region, new Particle.Data()
									{
										texture = texture_explosion,
										lifetime = lifetime,
										pos = transform.position + random_det.NextUnitVector2Range(i_f32 * 0.05f, i_f32 * 0.20f),
										scale = 0.50f + Maths.Clamp((0.80f + (i_f32 * 0.04f)) * scale_0, 0.20f, 3.00f) * random.NextFloatRange(0.75f, 1.25f),
										color_a = ColorBGRA.White,
										color_b = ColorBGRA.White,
										fps = (byte)(9 / lifetime),
										frame_count = 9,
										frame_count_total = 9,
										growth = random.NextFloatRange(0.50f, 1.10f * scale_2),
										angular_velocity = random.NextFloatRange(-0.50f, 0.50f),
										rotation = random.NextFloat(MathF.PI),
										vel = (dir * (random.NextFloatRange(0.00f, scale_1 * 1.00f) + (i_f32 * 2.50f))) + new Vector2(0.00f, -i_f32 * 0.80f),
										drag = 0.20f,
										lit = 1.00f,
									});

									fire_accumulator -= 1.00f;
								}
							}

							if (!cull_large)
							{
								// Shockwave
								Particle.Spawn(ref region, new Particle.Data()
								{
									texture = texture_smoke,
									lifetime = random.NextFloatRange(0.20f, 1.10f),
									pos = transform.position + random.NextVector2(0.40f),
									vel = random.NextUnitVector2Range(explosion.power, explosion.power * 4.00f),
									force = random.NextUnitVector2Range(20.00f, 40.00f),
									fps = random.NextByteRange(3, 5),
									frame_count = 64,
									frame_count_total = 64,
									frame_offset = random.NextByteRange(0, 64),
									scale = random.NextFloatRange(0.80f, 0.90f),
									//rotation = random.NextFloat(10.00f),
									//angular_velocity = random.NextFloat(2.00f),
									growth = random.NextFloatRange(10.00f, 20.00f),
									drag = random.NextFloatRange(0.06f, 0.10f),
									color_a = new Color32BGRA((byte)Maths.Clamp(random.NextFloatRange(explosion.power * 0.50f, explosion.power) * 20, 0, 220), 240, 240, 240),
									color_b = new Color32BGRA(0, 240, 240, 240),
									stretch = new Vector2(random.NextFloatRange(0.80f, 1.00f), random.NextFloatRange(0.70f, 0.80f)),
									face_dir_ratio = 0.50f,
								});
							}

							// Central smoke
							if (explosion.smoke_amount >= 1.20f && smoke_accumulator >= 0.50f && (uint)i % 2 == 0)
							{
								//App.WriteLine(1.00f - Maths.Clamp01(len / (radius * 0.50f)));

								Particle.Spawn(ref region, new Particle.Data()
								{
									texture = texture_smoke,
									lifetime = Maths.Clamp((((count_f32 - i_f32) * 0.15f) + 0.50f + random.NextFloatRange(3.00f, 20.00f)) * explosion.smoke_lifetime_multiplier, 0.10f, 20.00f),
									pos = hit_pos + (new Vector2(dir.X, dir.Y * 0.50f) * (1.00f + (len * 0.70f))),
									vel = dir * random.NextFloatRange(0.00f, scale_1 * 2.00f) * explosion.smoke_velocity_multiplier,
									fps = random.NextByteRange(3, 8),
									frame_count = 64,
									frame_offset = random.NextByteRange(0, 64),
									frame_count_total = 64,
									scale = Maths.Clamp(random.NextFloatRange(1.10f, 1.40f) * scale_0 * (0.20f + (1.00f - Maths.Clamp01(len / (radius * 0.50f)))), 0.20f, 3.00f),
									rotation = random.NextFloat(MathF.PI),
									angular_velocity = random.NextFloat(MathF.PI * 0.30f),
									growth = random.NextFloatRange(0.10f, 0.40f),
									//color_a = random.NextColor32Range(explosion.smoke_color, new Color32BGRA(140, 150, 150, 150)),
									//color_b = random.NextColor32Range(new Color32BGRA(0, 100, 100, 100), new Color32BGRA(0, 80, 80, 80)),
									color_a = random.NextColor32Range(smoke_color_a0, smoke_color_a1),
									color_b = random.NextColor32Range(smoke_color_b0, smoke_color_b1),
									force = new Vector2(0.00f, -random.NextFloatRange(0.00f, 10.00f)),
									drag = random.NextFloatRange(0.20f, 0.25f)
								});

								smoke_accumulator -= 0.50f;
							}

							//if (!cull_large)
							{
								// Outward smokes
								while (smoke_accumulator >= 1.00f)
								{
									Particle.Spawn(ref region, new Particle.Data()
									{
										texture = texture_smoke,
										lifetime = Maths.Clamp((((count_f32 - i_f32) * 0.05f) + 0.20f + random.NextFloatRange(1.00f, 10.00f)) * explosion.smoke_lifetime_multiplier, 0.10f, 20.00f),
										pos = hit_pos,
										vel = ((dir2 * (random.NextFloatRange(0, scale_1 * 2.00f) + (i_f32 * 0.30f))) * explosion.smoke_velocity_multiplier) + new Vector2(random.NextFloatRange(-2, 2) * (count_f32 - i_f32), -6.00f * i_f32),
										fps = random.NextByteRange(3, 8),
										frame_count = 64,
										frame_offset = random.NextByteRange(0, 64),
										frame_count_total = 64,
										scale = Maths.Clamp(random.NextFloatRange(0.90f, 1.70f) * scale_0 / (1.00f + ((count_f32 - i_f32) * 0.03f)), 0.20f, 3.00f),
										rotation = random.NextFloat(MathF.PI),
										angular_velocity = random.NextFloat(MathF.PI * 0.15f),
										growth = random.NextFloatRange(-0.10f, 0.40f),
										//color_a = random.NextColor32Range(explosion.smoke_color, new Color32BGRA(150, 100, 100, 100)),
										//color_b = random.NextColor32Range(new Color32BGRA(0, 100, 100, 100), new Color32BGRA(0, 50, 50, 50)),
										color_a = random.NextColor32Range(smoke_color_a0, smoke_color_a1),
										color_b = random.NextColor32Range(smoke_color_b0, smoke_color_b1),
										force = new Vector2(random.NextFloatRange(0.00f, 5.50f), -random.NextFloatRange(0.00f, 15.00f)),
										drag = random.NextFloatRange(0.12f, 0.20f),
										//stretch = new Vector2(1.10f, 0.90f),
										//face_dir_ratio = 0.50f
									});

									smoke_accumulator -= 1.00f;
								}
							}
#endif
						}
					}
				}
			}
		}
	}
}
