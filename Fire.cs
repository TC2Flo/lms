using Keg.Extensions;

namespace TC2.Base.Components
{
	public static partial class Burning
	{
		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region, sync_table_capacity: 256)]
		public partial struct Data(): IComponent
		{
			public float flicker = 0.03f;
			public float radius = 1.00f;
			public float damage_multiplier = 1.00f;
			public float intensity = 1.00f;

			[Save.NewLine]
			public Fire.Flags flags;
			public Fire.Flags flags_burning;

			[Save.NewLine]
			public Temperature temperature = 800.00f;
			[Net.Ignore, Save.Ignore] public float next_sync;
			[Net.Ignore, Save.Ignore] public float next_decay;

			public ColorBGRA color_tint = ColorBGRA.Transparent;

			[Save.NewLine]
			[Net.Ignore, Save.Ignore] public float last_intensity;
			[Net.Ignore, Save.Ignore] public float next_fire;
			[Net.Ignore, Save.Ignore] public float next_smoke;
			[Net.Ignore, Save.Ignore] public float next_hit;
		}

		[ISystem.AddFirst(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnAdd(ISystem.Info info, Entity entity, ref XorRandom random, 
		[Source.Owned] in Transform.Data transform, [Source.Owned] ref Body.Data body, [Source.Owned] ref Burning.Data burning)
		{
			body.override_shape_layer.AddFlag(Physics.Layer.Burning);

#if CLIENT
			Sound.Play(Fire.sounds_ignite.GetRandom(ref random), transform.position, 
				volume: random.NextFloatRange(0.20f, 0.40f), 
				pitch: random.NextFloatRange(0.70f, 1.00f), 
				dist_multiplier: 0.80f);
#endif
		}

		[ISystem.RemoveLast(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnRemove(ISystem.Info info, Entity entity, ref XorRandom random, 
		[Source.Owned] in Transform.Data transform, [Source.Owned] ref Body.Data body, [Source.Owned] ref Burning.Data burning)
		{
			body.override_shape_layer.RemoveFlag(Physics.Layer.Burning);

#if CLIENT
			Sound.Play(Fire.sounds_ignite.GetRandom(ref random), transform.position, 
				volume: random.NextFloatRange(0.20f, 0.40f), 
				pitch: random.NextFloatRange(0.70f, 1.00f), 
				dist_multiplier: 0.80f);
#endif
		}

		[ISystem.Event<Health.PostDamageEvent>(ISystem.Mode.Single, ISystem.Scope.Region), HasComponent<Body.Data>(Source.Modifier.Owned, true)]
		public static void OnPostDamage(ISystem.Info info, ref XorRandom random, [Source.Owned] ref Health.PostDamageEvent ev,
		[Source.Owned] ref Health.Data health, [Source.Owned] ref Burning.Data burning)
		{
			var time = info.WorldTime;
			switch (ev.damage.damage_type)
			{
				case Damage.Type.Heat:
				case Damage.Type.Electricity:
				{
					//burning.intensity += Maths.Clamp(Maths.Max(ev.damage.damage_integrity, ev.damage.damage_durability) * 0.10f, 1.00f, 4.00f) * Maths.Min(health.integrity, health.durability) * random.NextFloatExtra(0.05f, 0.10f);
					burning.next_decay += random.NextFloatExtra(0.01f, 0.41f);
				}
				break;

				case Damage.Type.Fire:
				{
					burning.intensity += Maths.Clamp(Maths.Max(ev.damage.damage_integrity, ev.damage.damage_durability) * 0.10f, 1.00f, 4.00f) * Maths.Min(health.integrity, health.durability) * random.NextFloatExtra(0.05f, 0.10f);
					burning.next_decay += random.NextFloatExtra(0.02f, 0.18f);
				}
				break;

				case Damage.Type.Phlogiston:
				{
					//burning.intensity += Maths.Clamp(Maths.Max(ev.damage.damage_integrity, ev.damage.damage_durability) * 0.10f, 1.00f, 4.00f) * Maths.Min(health.integrity, health.durability) * random.NextFloatExtra(0.85f, 1.35f);
					burning.next_decay += random.NextFloatExtra(0.15f, 0.35f);
				}
				break;
			}
		}

#if SERVER
		// TODO: split this into multiple methods/systems, especially for something called this frequently
		[ISystem.Update.E(ISystem.Mode.Single, ISystem.Scope.Region, interval: 0.177f)]
		public static void UpdateDamage(ISystem.Info info, Entity entity, ref XorRandom random, ref Region.Data region,
		[Source.Owned] ref Body.Data body, [Source.Owned] ref Burning.Data burning)
		{
			var time = info.WorldTime;
			if (time >= burning.next_hit)
			{
				var pos_body = body.GetPosition();
				if (burning.flags.HasNone(Fire.Flags.No_Contact_Ignite) || body.type == Body.Type.Dynamic) // needed to check if submerged in water, but static bodies can be skipped if they don't have contact ignite 
				{
					if (body.HasArbiters())
					{
						foreach (var arbiter in body.GetArbiters())
						{
							var layer = arbiter.GetLayer();
							if (burning.flags.HasNone(Fire.Flags.No_Contact_Ignite) && layer.HasAnyExcept(Physics.Layer.Flammable, Physics.Layer.Fire | Physics.Layer.Water | Physics.Layer.Essence | Physics.Layer.Bounds))
							{
								var material = arbiter.GetMaterial();
								var ent_target = arbiter.GetEntity();

								//App.WriteLine("hit");
								//entity.Hit(entity, ent_target, arbiter.GetPosition(), -arbiter.GetNormal(), arbiter.GetNormal(), random.NextFloatRange(100.00f, 200.00f) * burning.damage_multiplier, material, Damage.Type.Fire, knockback: 0.00f, flags: Damage.Flags.No_Sound | Damage.Flags.No_Particles);

								var damage = random.NextFloatRange(5.00f, 10.00f) * burning.damage_multiplier * burning.intensity;

								Damage.Hit(ent_attacker: entity, ent_owner: default, ent_target: ent_target,
									position: arbiter.GetBodyPosition(), velocity: -arbiter.GetNormal(), normal: arbiter.GetNormal(),
									damage_integrity: damage, damage_durability: damage, damage_terrain: damage,
									target_material_type: material, damage_type: Damage.Type.Fire,
									yield: 1.00f, size: 1.00f, impulse: 0.00f, flags: Damage.Flags.No_Loot_Pickup | Damage.Flags.No_Loot_Drop | Damage.Flags.No_Loot_Notification);

								ent_target.Ignite(intensity: Maths.Max(1.00f, random.NextFloatRange(0.30f, 0.75f) * burning.intensity), flags: burning.flags_burning);

								//ref var burning_new = ref arbiter.GetEntity().GetOrAddComponent<Burning.Data>(sync: true);
								//if (!burning_new.IsNull())
								//{
								//	burning_new.radius = 1.00f;
								//	burning_new.flicker = 0.03f;
								//	burning_new.damage_multiplier = 1.00f;
								//	burning_new.intensity = Maths.Max(1.00f, random.NextFloatRange(burning.intensity * 0.30f, burning.intensity * 0.75f));
								//	burning_new.flags = burning.flags;
								//}
							}
							else if (layer.HasAny(Physics.Layer.Water))
							{
								burning.intensity *= 0.82f;
								break;
							}
						}
					}
				}

				if (burning.flags.HasNone(Fire.Flags.No_Radius_Ignite) && burning.intensity >= 10.00f && random.NextBool(Maths.Min(0.50f, burning.intensity * 0.10f)))
				{
					Span<OverlapResult> results = FixedArray.CreateSpan16NoInit<OverlapResult>(out var buffer);
					if (region.TryOverlapPointAll(world_position: pos_body, radius: burning.radius * random.NextFloatRange(1.20f, 1.40f), hits: ref results, mask: Physics.Layer.Flammable, exclude: Physics.Layer.Burning | Physics.Layer.Fire | Physics.Layer.World | Physics.Layer.Water | Physics.Layer.Essence | Physics.Layer.Bounds))
					{
						foreach (ref var result in results)
						{
							var material = result.material_type;

							//entity.Hit(entity, result.entity, result.world_position, -result.gradient, result.gradient, random.NextFloatRange(50.00f, 100.00f) * burning.damage_multiplier, result.material_type, Damage.Type.Fire, knockback: 0.00f, yield: 0.00f, flags: Damage.Flags.No_Sound | Damage.Flags.No_Particles);

							var damage = random.NextFloatRange(5.00f, 10.00f) * burning.damage_multiplier * burning.intensity;

							Damage.Hit(ent_attacker: entity, ent_owner: default, ent_target: result.entity,
								position: result.world_position, velocity: -result.gradient, normal: result.gradient,
								damage_integrity: damage, damage_durability: damage, damage_terrain: damage,
								target_material_type: material, damage_type: Damage.Type.Fire,
								yield: 1.00f, size: 1.00f, impulse: 0.00f, flags: Damage.Flags.No_Loot_Pickup | Damage.Flags.No_Loot_Drop | Damage.Flags.No_Loot_Notification);

							result.entity.Ignite(intensity: Maths.Max(1.00f, random.NextFloatRange(burning.intensity * 0.10f, burning.intensity * 0.35f)), flags: burning.flags_burning);
						}
					}
				}

				if (burning.flags.HasNone(Fire.Flags.No_Self_Damage) && burning.intensity > 4.00f && body.GetLayer().HasAny(Physics.Layer.Flammable))
				{
					//entity.Hit(entity, entity, transform.position, random.NextUnitVector2Range(1, 1), random.NextUnitVector2Range(1, 1), random.NextFloatRange(20.00f, 50.00f) * burning.damage_multiplier * burning.intensity, body.GetMaterial(), Damage.Type.Fire, knockback: 0.00f, yield: 0.00f, flags: Damage.Flags.No_Sound | Damage.Flags.No_Particles);

					var damage = random.NextFloatRange(5.00f, 10.00f) * burning.damage_multiplier * burning.intensity;

					Damage.Hit(ent_attacker: entity, ent_owner: default, ent_target: entity,
						position: pos_body, velocity: random.NextUnitVector2(), normal: random.NextUnitVector2(),
						damage_integrity: damage, damage_durability: damage, damage_terrain: damage,
						target_material_type: body.GetMaterial(), damage_type: Damage.Type.Fire,
						yield: 1.00f, size: 1.00f, impulse: 0.00f, flags: Damage.Flags.No_Loot_Pickup | Damage.Flags.No_Loot_Notification);
				}

				burning.next_hit = time + random.NextFloatRange(0.30f, 0.80f);
			}
		}

		// TODO: split this into multiple methods/systems, especially for something called this frequently
		[ISystem.Update.F(ISystem.Mode.Single, ISystem.Scope.Region, interval: 0.2193f)]
		public static void UpdateDecay(ISystem.Info info, Entity entity, ref XorRandom random,
		[Source.Owned] ref Body.Data body, [Source.Owned] ref Burning.Data burning)
		{
			var time = info.WorldTime;
			if (burning.flags.HasNone(Fire.Flags.No_Decay) && time >= burning.next_decay)
			{
				burning.next_decay = time + random.NextFloatExtra(0.05f, 0.20f);

				Fire.GetFlammabilityMultipliers(body.GetMaterial(), out var intensity_mult, out var decay_mult);
				var decay_lerp = (1.00f + (burning.intensity * 0.20f)).RcpFast() * decay_mult;
				burning.intensity.LerpFMARef(burning.intensity * intensity_mult, decay_lerp); // = Maths.Lerp(burning.intensity, burning.intensity * 0.70f, 0.04f * decay_mult);

				//App.WriteLine(burning.intensity);
				if (burning.intensity < 0.05f)
				{
					if (burning.intensity <= 0.0001f || random.NextBool((1.00f - (burning.intensity * 2.00f)) * 0.020f))
					{
						entity.RemoveComponent<Burning.Data>();
						return;
					}
				}
			}

			if (time >= burning.next_sync)
			{
				burning.next_sync = time + 1.00f;

				if (Maths.Abs(burning.last_intensity - burning.intensity) > 0.10f)
				{
					burning.last_intensity = burning.intensity;
					burning.Sync(entity, true);
				}
			}
		}
#endif

#if CLIENT
		[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("local", true, Source.Modifier.Shared)]
		public static void OnGUI(ISystem.Info info, Entity entity, [Source.Shared] in Player.Data player, [Source.Owned] in Burning.Data burning)
		{
			var color = GUI.font_color_red;

			IStatusEffect.ScheduleDraw(new()
			{
				//icon = "ui_icon_effect.burning",
				//icon_extra = "beer",
				value = "Burning!",
				text_color = color,
				name = "Set a man on fire, and he'll be warm for the rest of his life."
			});
		}

		[ISystem.PostUpdate.B(ISystem.Mode.Single, ISystem.Scope.Region, interval: 0.05f)]
		public static void UpdateEffects(ISystem.Info info, Entity entity, ref Region.Data region, ref XorRandom random, [Source.Owned] in Transform.Data transform, [Source.Owned] ref Body.Data body, [Source.Owned] ref Burning.Data burning)
		{
			var time = info.WorldTime;
			if (time >= burning.next_fire)
			{
				burning.next_fire = time + random.NextFloatRange(0.05f, 0.10f) * Maths.Clamp(Maths.RcpFast(1.00f + (burning.intensity * 0.05f)), 0.10f, 1.50f);

				var closest = body.GetClosestPoint(body.GetPosition() + random.NextUnitVector2Range(0.50f, 8.00f), true);
				var pos = closest.world_position_raw;

				if (time >= burning.next_smoke)
				{
					burning.next_smoke = time + random.NextFloatRange(0.60f, 0.80f);

					Particle.Spawn(ref region, new Particle.Data()
					{
						texture = Fire.texture_smoke,
						lifetime = random.NextFloatRange(2.00f, 4.00f),
						pos = pos + random.NextVector2(0.50f),
						vel = (new Vector2(0, -1) * random.NextFloatRange(0.50f, 1.50f)) + random.NextVector2(1.00f),
						fps = 16,
						frame_count = 64,
						frame_count_total = 64,
						frame_offset = random.NextByteRange(0, 64),
						//scale = random.NextFloatRange(1.50f, 2.50f),
						scale = random.NextFloatRange(0.50f, 0.70f), // * burning.intensity,
						rotation = random.NextFloat(10.00f),
						angular_velocity = random.NextFloat(0.50f),
						growth = 0.30f,
						color_a = new Color32BGRA(170, 65, 60, 70),
						color_b = new Color32BGRA(0, 30, 30, 35),
					});
				}

				if (burning.intensity >= 6.50f)
				{
					for (var i = 0; i < 2; i++)
					{
						var lifetime = random.NextFloatRange(0.30f, 1.10f);
						Particle.Spawn(ref region, new Particle.Data()
						{
							texture = Fire.texture_light,
							lifetime = lifetime,
							pos = pos,
							scale = (burning.radius * 0.80f * (1.00f + (burning.intensity * 0.15f))).Clamp(0.50f, 1.58f) * random.NextFloatRange(0.40f, 1.20f),
							color_a = random.NextColorRange(ColorBGRA.RGBA(1.00f, 0.90f, 0.60f, 1.00f), ColorBGRA.RGBA(1.00f, 0.80f, 0.30f, 1.50f)).WithTint(burning.color_tint),
							color_b = ColorBGRA.RGBA(1.00f, 0.20f, 0.00f, 0.00f),
							//growth = -random.NextFloatRange(0.70f, 4.50f),
							stretch = new Vector2(random.NextFloatRange(0.95f, 2.90f), random.NextFloatRange(0.75f, 1.85f)),
							angular_velocity = random.NextFloat(1.10f),
							growth = random.NextFloatRange(-1.00f, 4.00f),
							drag = random.NextFloatRange(0.01f, 0.09f),
							glow = Maths.Clamp(burning.intensity * 0.17f, 0.25f, 2.00f) * random.NextFloatExtra(0.35f, 1.20f),
							force = new Vector2(random.NextFloat(8), -random.NextFloatExtra(2, 8)),
							vel = random.NextUnitVector2Range(0.50f, 3.00f) + new Vector2(0.00f, -random.NextFloatRange(0.50f, 5.00f)),
							face_dir_ratio = random.NextFloatRange(0.90f, 1.00f)
						});
					}

					if (burning.intensity > 15.00f)
					{
						Particle.Spawn(ref region, new Particle.Data()
						{
							texture = Welder.metal_spark_01,
							lifetime = random.NextFloatRange(0.20f, 0.80f),
							pos = pos,
							vel = random.NextUnitVector2Range(0.50f, 8.00f) + new Vector2(0, -10),
							fps = 8,
							frame_count = 4,
							frame_offset = random.NextByteRange(0, 4),
							frame_count_total = 4,
							scale = random.NextFloatRange(0.70f, 1.50f),
							growth = random.NextFloatRange(-0.30f, 0.60f),
							force = new Vector2(random.NextFloat(40), random.NextFloatRange(-25, 20)),
							drag = random.NextFloatRange(0.01f, 0.04f),
							stretch = new Vector2(random.NextFloatRange(0.70f, 1.00f), 0.25f),
							color_a = random.NextColorRange(ColorBGRA.RGBA(1.00f, 0.90f, 0.60f, 1.00f), ColorBGRA.RGBA(1.00f, 0.80f, 0.30f, 1.50f)),
							color_b = ColorBGRA.RGBA(1.00f, 0.20f, 0.00f, 0.00f),
							glow = random.NextFloatExtra(4.35f, 8.20f),
							face_dir_ratio = 1.00f,
						});
					}

					//if (fire.intensity > 0.75f)
					//{
					//	for (var i = 0; i < 1; i++)
					//	{
					//		var lifetime = random.NextFloatRange(0.20f, 0.70f);
					//		Particle.Spawn(ref region, new Particle.Data()
					//		{
					//			texture = Fire.texture_fire,
					//			lifetime = lifetime,
					//			pos = pos + random.NextVector2Range(-0.40f, 0.40f),
					//			scale = random.NextFloatRange(0.80f, 1.00f),
					//			color_a = new Color32BGRA(255, 255, 255, 255),
					//			color_b = new Color32BGRA(255, 255, 255, 255),
					//			fps = random.NextByteRange(10, 20),
					//			frame_count = 7,
					//			frame_count_total = 7,
					//			frame_offset = random.NextByteRange(0, 7),
					//			rotation = random.NextFloatRange(-0.20f, 0.20f),
					//			growth = -random.NextFloatRange(0.70f, 2.50f),
					//			lit = 0.70f,
					//			angular_velocity = random.NextFloatRange(-4.50f, 4.50f),
					//			vel = random.NextUnitVector2Range(2.00f, 5.00f),
					//			face_dir_ratio = 1,
					//			stretch = new Vector2(2, 1),
					//			drag = random.NextFloatRange(0.10f, 0.15f)
					//		});
					//	}
					//}
				}
			}
		}
#endif
	}

	public static partial class Fire
	{
		[Flags]
		public enum Flags: ushort
		{
			None = 0,

			No_Contact_Ignite = 1 << 0,
			No_Radius_Ignite = 1 << 1,
			No_Decay = 1 << 2,
			No_Self_Damage = 1 << 3,

			Always_Contact_Ignite = 1 << 8,
		}

		public static readonly Sound.Handle[] sounds_ignite =
		{
			"fire.ignite.00",
			"fire.ignite.01",
			"fire.ignite.02",
		};

		public static void GetFlammabilityMultipliers(Material.Type material_type, out float intensity_mult, out float decay_mult)
		{
			switch (material_type)
			{
				case Material.Type.Bramble:
				{
					intensity_mult = 0.55f;
					decay_mult = 0.11f;
				}
				break;

				case Material.Type.Paper:
				{
					intensity_mult = 0.69f;
					decay_mult = 0.19f;
				}
				break;

				case Material.Type.Tree:
				{
					intensity_mult = 0.30f;
					decay_mult = 0.17f;
				}
				break;

				case Material.Type.Coal:
				case Material.Type.Wax:
				{
					intensity_mult = 0.33f;
					decay_mult = 0.03f;
				}
				break;

				case Material.Type.Fur:
				case Material.Type.Fabric:
				case Material.Type.Building_Wood:
				case Material.Type.Wood:
				{
					intensity_mult = 0.36f;
					decay_mult = 0.11f;
				}
				break;

				case Material.Type.Powder:
				case Material.Type.Resin:
				case Material.Type.Wood_Powder:
				{
					intensity_mult = 0.74f;
					decay_mult = 0.04f;
				}
				break;

				case Material.Type.Flesh:
				{
					intensity_mult = 0.28f;
					decay_mult = 0.31f;
				}
				break;

				case Material.Type.Leather:
				case Material.Type.Mushroom:
				{
					intensity_mult = 0.20f;
					decay_mult = 0.31f;
				}
				break;

				case Material.Type.Wreck:
				case Material.Type.Scrap:
				{
					intensity_mult = 0.30f;
					decay_mult = 0.18f;
				}
				break;

				case Material.Type.Foliage:
				{
					intensity_mult = 0.29f;
					decay_mult = 0.19f;
				}
				break;

				case Material.Type.Insect:
				{
					intensity_mult = 0.22f;
					decay_mult = 0.38f;
				}
				break;

				default:
				{
					intensity_mult = 0.36f;
					decay_mult = 0.18f;
				}
				break;
			}
		}

		public static readonly Texture.Handle texture_fire = "fire2";
		public static readonly Texture.Handle texture_smoke = "BiggerSmoke_Light";
		public static readonly Texture.Handle texture_metal_spark = "Metal_Spark";
		public static readonly Texture.Handle texture_light = "light.fire.00";

		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region, sync_table_capacity: 64)]
		public partial struct Data(): IComponent
		{
			public float flicker;
			public float radius = 1.00f;
			public float ignite_radius_multiplier = 1.00f;
			public float damage_multiplier = 1.00f;
			public float intensity = 1.00f;
			public float fade = 0.10f;

			[Save.NewLine]
			public Fire.Flags flags;
			public Fire.Flags flags_burning;

			[Save.NewLine]
			public Damage.Type damage_type_contact = Damage.Type.Fire;
			public Damage.Type damage_type_convection = Damage.Type.Fire;
			private ushort unused_00;

			[Save.NewLine]
			public Physics.Layer layers_require;
			public Physics.Layer layers_exclude = Physics.Layer.Fire | Physics.Layer.World | Physics.Layer.Essence | Physics.Layer.Water | Physics.Layer.Zone | Physics.Layer.Bounds;

			[Save.NewLine]
			public float glow_mult = 1.00f;
			public Color32BGRA smoke_color = new(220, 65, 60, 70);
			public Temperature temperature = 1000.00f;
			private uint unused_01;
			public ColorBGRA color_tint = ColorBGRA.Transparent;
			private ulong unused_02;

			[Save.NewLine]
			[Net.Ignore, Save.Ignore] public float last_intensity;
			[Net.Ignore, Save.Ignore] public float next_fire;
			[Net.Ignore, Save.Ignore] public float next_smoke;
			[Net.Ignore, Save.Ignore] public float next_contact;

			[Net.Ignore, Save.Ignore] public float next_hit;
			[Net.Ignore, Save.Ignore] public float next_sync;
			[Net.Ignore, Save.Ignore] private float unused_03;
			[Net.Ignore, Save.Ignore] private float unused_04;
		}

		[IEvent.Data]
		public partial struct IgniteEvent(): IEvent, IEvent<Crafting.ConfiguredRecipe>
		{
			//public Vector2 position;
			public float step = 0.10f;
			public float intensity = 1.00f;
			public float damage;

			//public ColorBGRA color_mult = ColorBGRA.White;
			public Temperature? temperature;
			public ColorBGRA? color_tint;

			public Fire.Flags flags;
			//public Fire.Flags flags_burning;
			public Damage.Type damage_type = Damage.Type.Fire;
			//public Damage.Type damage_type_convection = Damage.Type.Fire;

			void IEvent<Crafting.ConfiguredRecipe>.Bind(ref Crafting.ConfiguredRecipe data)
			{

			}
		}

#if SERVER
		[ISystem.Event<Fire.IgniteEvent>(ISystem.Mode.Single, ISystem.Scope.Region), HasComponent<Fire.Data>(Source.Modifier.Owned, false)]
		public static void OnIgnite(ISystem.Info info, Entity entity, ref Fire.IgniteEvent ev,
		[Source.Owned] ref Transform.Data transform, [Source.Owned] ref Body.Data body,
		[Source.Owned, Optional(true)] ref Burning.Data burning)
		{
			burning = ref burning.AddComponentIfNull(entity, out var added, sync: true);

			var ratio = Maths.SumRatio(ev.step, burning.intensity);

			if (ev.temperature.HasValue) burning.temperature = Maths.Lerp(burning.temperature, ev.temperature.GetValueOrDefault(), ratio);
			if (ev.color_tint.HasValue) burning.color_tint = ColorBGRA.Lerp(burning.color_tint, ev.color_tint.GetValueOrDefault(), ratio);

			//burning.color_tint.SetIfHasValue(ev.color_tint);

			//App.WriteValue(ratio);
			//App.WriteValue(ev.step);

			burning.intensity.AddTowardsDamped(ev.intensity, ev.step, ratio);
			burning.next_decay = info.WorldTime + 0.50f;

			if (burning.intensity >= ev.intensity) //Maths.Normalize01(burning.intensity  0.50f)
			{
				burning.flags |= ev.flags;
			}

			if (added)
			{

			}
			else
			{
				burning.Sync(entity, true);
			}
		}
#endif

#if SERVER
		public static void Ignite(this Entity ent_target, float intensity, Fire.Flags flags, float damage = 0.00f, 
		Damage.Type damage_type = Damage.Type.Fire, Temperature? temperature = null, ColorBGRA? tint = null, float? step = null)
		{
			if (intensity >= 1.00f)
			{
				var ev = new Fire.IgniteEvent()
				{
					damage = damage,
					damage_type = damage_type,
					flags = flags,
					step = step ?? intensity,
					intensity = intensity,
					color_tint = tint,
					temperature = temperature
				};
				ev.Trigger(ent_target);


				//ref var burning_new = ref ent_target.GetOrAddComponent<Burning.Data>(sync: true);
				//if (burning_new.IsNotNull())
				//{
				//	burning_new.intensity = Maths.Max(burning_new.intensity, intensity);
				//	burning_new.flags |= flags;
				//}
			}
		}

		[ISystem.Update.E(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void Update(ISystem.Info info, Entity entity, ref XorRandom random, ref Region.Data region, 
		[Source.Owned] in Transform.Data transform, [Source.Owned] ref Body.Data body, [Source.Owned] ref Fire.Data fire)
		{
			var time = info.WorldTime;

			if (fire.flags.HasNone(Fire.Flags.No_Contact_Ignite) && body.HasArbiters())
			{
				foreach (var arbiter in body.GetArbiters())
				{
					if (arbiter.GetState() == Body.Arbiter.State.Begin || time >= fire.next_contact)
					{
						//App.WriteLine($"{time} >= {fire.next_hit}");
						var layer = arbiter.GetLayer();
						if (layer.HasNone(fire.layers_exclude) && layer.HasAll(fire.layers_require))
						{
							var material = arbiter.GetMaterial();
							var ent_target = arbiter.GetEntity();

							//App.WriteLine("hit");

							var pos_contact = arbiter.GetContactsPosition();

							var dist = Vector2.Distance(transform.position, pos_contact);
							var modifier = fire.flags.HasAny(Fire.Flags.Always_Contact_Ignite) ? 1.00f : (1.00f - Maths.Normalize01(dist, fire.radius));
							//App.WriteLine($"dist: {dist}/{fire.radius}; {modifier:0.00}");

							if (modifier >= 0.10f || random.NextBool(0.10f + ((0.40f + (modifier * 0.10f)) * (fire.intensity * 0.40f))))
							{
								//entity.Hit(entity, ent_target, arbiter.GetPosition(), -arbiter.GetNormal(), arbiter.GetNormal(), random.NextFloatRange(100.00f, 150.00f) * fire.intensity * fire.damage_multiplier * modifier, material, Damage.Type.Fire, knockback: 0.00f, yield: 0.00f, flags: Damage.Flags.No_Sound | Damage.Flags.No_Particles);

								var damage = random.NextFloatRange(100.00f, 150.00f) * fire.intensity * fire.damage_multiplier * modifier;

								Damage.Hit(ent_attacker: entity, ent_owner: entity, ent_target: ent_target,
									position: pos_contact, velocity: -arbiter.GetNormal(), normal: arbiter.GetNormal(),
									damage_integrity: damage, damage_durability: damage, damage_terrain: damage,
									target_material_type: material, damage_type: fire.damage_type_contact,
									yield: 1.00f, size: fire.radius * 0.50f, impulse: 0.00f, 
									flags: Damage.Flags.No_Loot_Pickup | Damage.Flags.No_Loot_Drop | Damage.Flags.No_Loot_Notification);

								if (layer.HasAny(Physics.Layer.Flammable))
								{
									ent_target.Ignite(intensity: Maths.Max(0.40f, random.NextFloatRange(fire.intensity * 0.80f, fire.intensity * 1.00f)) * (0.50f + (modifier * 0.50f)), flags: fire.flags_burning);
								}
							}
						}
						//else
						//{
						//	arbiter.SetIgnored();
						//}
					}
				}
			}

			if (time >= fire.next_contact)
			{
				fire.next_contact = time + random.NextFloatRange(0.05f, 0.25f);
			}

			if (time >= fire.next_hit)
			{
				fire.next_hit = time + random.NextFloatRange(0.60f, 0.80f);
				if (fire.flags.HasNone(Fire.Flags.No_Radius_Ignite) && random.NextBool(Maths.Min(0.50f, fire.intensity * 0.10f)))
				{
					Span<OverlapResult> results = FixedArray.CreateSpan16NoInit<OverlapResult>(out var buffer);
					if (region.TryOverlapPointAll(world_position: transform.position, radius: fire.radius * random.NextFloatExtra(0.80f, 0.40f) * fire.ignite_radius_multiplier, 
					hits: ref results, mask: Physics.Layer.Flammable, 
					exclude: Physics.Layer.Burning | Physics.Layer.Fire | Physics.Layer.Water | Physics.Layer.Essence | Physics.Layer.World))
					{
						foreach (ref var result in results)
						{
							var material = result.material_type;

							//App.WriteLine("hit");
							//entity.Hit(entity, result.entity, result.world_position, -result.gradient, result.gradient, random.NextFloatRange(50.00f, 100.00f) * fire.damage_multiplier, result.material_type, Damage.Type.Fire, knockback: 0.00f, yield: 0.00f, flags: Damage.Flags.No_Sound | Damage.Flags.No_Particles);

							var damage = random.NextFloatRange(50.00f, 100.00f) * fire.damage_multiplier;

							Damage.Hit(ent_attacker: entity, ent_owner: entity, ent_target: result.entity,
								position: result.world_position, velocity: -result.gradient, normal: result.gradient,
								damage_integrity: damage, damage_durability: damage, damage_terrain: damage,
								target_material_type: result.material_type, damage_type: fire.damage_type_convection,
								yield: 1.00f, size: fire.radius * 0.50f, impulse: 0.00f, 
								flags: Damage.Flags.No_Sound | Damage.Flags.No_Particles | Damage.Flags.No_Loot_Pickup | Damage.Flags.No_Loot_Drop | Damage.Flags.No_Loot_Notification);

							result.entity.Ignite(intensity: Maths.Max(1.00f, random.NextFloatRange(fire.intensity * 0.20f, fire.intensity * 0.50f)), 
								flags: fire.flags_burning, temperature: fire.temperature);
						}
					}
				}
			}

			if (fire.flags.HasNone(Fire.Flags.No_Decay))
			{
				fire.intensity = Maths.MoveTowards(fire.intensity, 0.00f, fire.fade * info.DeltaTime * random.NextFloatRange(0.25f, 1.00f));

				if (fire.intensity < 0.50f)
				{
					entity.Delete();
					return;
				}
			}

			if (time >= fire.next_sync)
			{
				fire.next_sync = time + random.NextFloatRange(0.10f, 1.00f);

				if (Maths.Abs(fire.last_intensity - fire.intensity) > 0.01f)
				{
					fire.last_intensity = fire.intensity;
					fire.Sync(entity, true);
				}
			}
		}
#endif

#if CLIENT
		[ISystem.PostUpdate.B(ISystem.Mode.Single, ISystem.Scope.Region, interval: 0.05f)]
		public static void UpdateEffects(ISystem.Info info, ref Region.Data region, ref XorRandom random, 
		[Source.Owned] in Transform.Data transform, [Source.Owned] ref Fire.Data fire, [Source.Owned, Pair.Component<Fire.Data>] ref Light.Data light)
		{
			var time = info.WorldTime;
			if (time >= fire.next_fire)
			{
				fire.next_fire = time + random.NextFloatRange(0.20f, 0.30f);

				var lifetime = random.NextFloatRange(0.75f, 1.50f);
				Particle.Spawn(ref region, new Particle.Data()
				{
					texture = Fire.texture_light,
					lifetime = lifetime,
					pos = transform.position + random.NextUnitVector2(fire.radius * 0.20f),
					scale = random.NextFloatRange(1.00f, 2.00f) * fire.radius * 1.50f * (1.00f + Maths.Min(1.50f, fire.intensity * 0.50f * fire.glow_mult)),
					color_a = ColorBGRA.Tint(ColorBGRA.RGBA(1.00f, 0.70f, 0.30f, 8.00f), fire.color_tint),
					color_b = ColorBGRA.RGBA(1.00f, 0.20f, 0.00f, 0.00f),
					//growth = -random.NextFloatRange(0.70f, 4.50f),
					stretch = new Vector2(random.NextFloatRange(0.75f, 1.20f), random.NextFloatRange(0.50f, 1.25f)),
					angular_velocity = random.NextFloat(1.00f),
					growth = random.NextFloatRange(2.00f, 5.00f),
					drag = random.NextFloatRange(0.02f, 0.06f),
					glow = random.NextFloatExtra(1.00f, fire.intensity * 1.75f * fire.glow_mult),
					vel = random.NextUnitVector2Range(1.00f, 4.00f) + new Vector2(0.00f, -random.NextFloatExtra(2.00f + Maths.Min(8.00f, fire.intensity * 0.80f * fire.glow_mult), fire.radius + fire.flicker)),

					face_dir_ratio = random.NextFloatRange(0.50f, 1.00f)
				});

				//for (var i = 0; i < 1; i++)
				//{
				//	var lifetime = random.NextFloatRange(1.00f, 1.50f) * Maths.Clamp(fire.intensity * 0.25f, 0.25f, 1.00f);
				//	Particle.Spawn(ref region, new Particle.Data()
				//	{
				//		texture = texture_fire,
				//		lifetime = lifetime,
				//		pos = transform.position + random.NextUnitVector2Range(new Vector2(0.50f, 3.00f), new Vector2(0.00f, 1.00f)) + new Vector2(0.00f, 1.00f),
				//		scale = random.NextFloatRange(0.75f, 1.20f),
				//		color_a = new Color32BGRA(255, 255, 255, 255),
				//		color_b = new Color32BGRA(255, 255, 255, 255),
				//		fps = random.NextByteRange(12, 18),
				//		frame_count = 7,
				//		frame_count_total = 7,
				//		frame_offset = random.NextByteRange(0, 7),
				//		rotation = random.NextFloatRange(-0.20f, 0.20f),
				//		growth = -random.NextFloatRange(0.20f, 0.50f),
				//		lit = 1.00f,
				//		angular_velocity = random.NextFloatRange(-2.50f, 2.50f),
				//		vel = random.NextUnitVector2Range(2.00f, 5.00f),
				//		face_dir_ratio = 1,
				//		stretch = new Vector2(2, 1),
				//		drag = random.NextFloatRange(0.10f, 0.15f)
				//	});
				//}
			}

			light.intensity = Maths.Lerp(light.intensity, (Maths.Clamp(fire.intensity * fire.glow_mult, 0.50f, 4.00f) * (1.00f - random.NextFloatRange(0.00f, fire.flicker))), 0.10f);
			light.scale = Vector2.Lerp(light.scale, new Vector2(random.NextFloatRange(0.10f, 1.00f), random.NextFloatRange(0.10f, 0.90f)) * 6.00f * Maths.Clamp(fire.radius, 4.00f, 32.00f), 0.10f);
			light.rotation = Maths.LerpAngle(light.rotation, random.NextFloatRange(-MathF.PI, MathF.PI), fire.flicker);
			light.offset = Vector2.Lerp(light.offset, new Vector2(random.NextFloatRange(-fire.radius, fire.radius), random.NextFloatRange(0.00f, -fire.radius * 0.50f)), 0.50f);
			light.jitter = random.NextUnitVector2(fire.flicker);

			if (time >= fire.next_smoke)
			{
				fire.next_smoke = time + random.NextFloatExtra(0.20f, 0.35f);

				Particle.Spawn(ref region, new Particle.Data()
				{
					texture = texture_smoke,
					lifetime = random.NextFloatRange(3.00f, 7.00f),
					pos = transform.position + random.NextVector2(2.00f) + new Vector2(0, -1),
					vel = (new Vector2(0, -1) * random.NextFloatRange(1.50f, 3.50f)) + random.NextVector2(1.00f),
					fps = 16,
					frame_count = 64,
					frame_count_total = 64,
					frame_offset = random.NextByteRange(0, 64),
					//scale = random.NextFloatRange(1.50f, 2.50f),
					scale = random.NextFloatExtra(0.10f, 0.20f) * fire.radius * 0.50f,
					rotation = random.NextFloat(10.00f),
					angular_velocity = random.NextFloat(0.50f),
					growth = random.NextFloatExtra(0.20f, 0.10f),
					color_a = fire.smoke_color,
					color_b = fire.smoke_color.WithAlpha(0),
				});
			}

			for (var i = 0; i < 1; i++)
			{
				Particle.Spawn(ref region, new Particle.Data()
				{
					texture = texture_metal_spark,
					lifetime = random.NextFloatRange(0.20f, 0.50f),
					pos = transform.position + random.NextUnitVector2(fire.radius * 0.50f),
					vel = new Vector2(random.NextFloatRange(-24, 24), random.NextFloatRange(0, -16)),
					fps = 16,
					frame_count = 4,
					frame_offset = random.NextByteRange(0, 4),
					frame_count_total = 4,
					scale = random.NextFloatRange(0.30f, 0.70f),
					rotation = random.NextFloat(MathF.PI),
					angular_velocity = random.NextFloat(MathF.PI * 10.00f),
					growth = -random.NextFloatRange(0.70f, 2.00f),
					force = new Vector2(0.00f, 30.00f),
					drag = random.NextFloatRange(0.10f, 0.15f),
					lit = 1.00f,
				});
			}
		}
#endif
	}
}
