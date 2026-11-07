
//namespace TC2.Base
//{
//	public static partial class HitEffects
//	{
//#if CLIENT
//		public static readonly Texture.Handle large_smoke_rough = "LargeSmoke_Rough";
//		public static readonly Texture.Handle large_smoke_rougher = "LargeSmoke_Rougher";
//		public static readonly Texture.Handle large_smoke = "LargeSmoke";
//		public static readonly Texture.Handle bigger_smoke_light = "BiggerSmoke_Light";
//		public static readonly Texture.Handle splash_00 = "splash.00";
//		public static readonly Texture.Handle metal_spark = "Metal_Spark";
//		public static readonly Texture.Handle metal_spark_01 = "metal_spark.01";
//		public static readonly Texture.Handle stone_gib = "StoneGib";
//		public static readonly Texture.Handle gravel_gib = "GravelGib";
//		public static readonly Texture.Handle wood_gib = "WoodGib";
//		public static readonly Texture.Handle glass_gib = "glass_gibs";
//		public static readonly Texture.Handle foliage_gib = "FoliageGib";
//		public static readonly Texture.Handle foliage_gib_large = "FoliageGib_Large";
//		public static readonly Texture.Handle metal_gibs_sm = "metal.gibs.sm";
//		public static readonly Texture.Handle metal_gibs_md = "metal.gibs.md";
//		public static readonly Texture.Handle metal_gibs_lg = "metal.gibs.lg";
//		public static readonly Texture.Handle mushroom_gibs = "mushroom_gibs";
//		public static readonly Texture.Handle soil_gibs = "soil_gibs";
//		public static readonly Texture.Handle flesh_gib_sm = "flesh_gib_sm";
//		public static readonly Texture.Handle flesh_gib_md = "flesh_gib_md";

//		public static Sound.Handle[] snd_geiger =
//		{
//			"geiger.00",
//			"geiger.01",
//			"geiger.02"
//		};

//		public static Sound.Handle[] snd_flesh_hit_sharp =
//		{
//			"flesh_hit_sharp_00",
//			"flesh_hit_sharp_01",
//			"flesh_hit_sharp_02"
//		};

//		public static Sound.Handle[] snd_flesh_hit_sting =
//		{
//			"flesh_hit_sting_00",
//		};

//		public static Sound.Handle[] snd_flesh_hit_impact =
//		{
//			"flesh_hit_sharp_01"
//		};

//		public static Sound.Handle[] snd_flesh_hit_slash =
//		{
//			"flesh_hit_slash_00"
//		};

//		public static Sound.Handle[] snd_flesh_hit_blunt =
//		{
//			"flesh_hit_blunt_00",
//			"flesh_hit_blunt_01",
//			"flesh_hit_blunt_02"
//		};

//		public static Sound.Handle[] snd_flesh_hit_scratch =
//		{
//			"flesh.hit.scratch.00"
//		};

//		public static Sound.Handle[] snd_flesh_gib =
//		{
//			"flesh_gib_00",
//			"flesh_gib_01",
//			"flesh_gib_02"
//		};

//		public static Sound.Handle[] snd_metal_gib =
//		{
//			"metal_gib_00"
//		};

//		public static Sound.Handle[] snd_metal_gib_lg =
//		{
//			"metal.gib.lg.00",
//			"metal.gib.lg.01"
//		};

//		public static Sound.Handle[] snd_stone_hit_sharp =
//		{
//			"stone_hit_sharp_00",
//			"stone_hit_sharp_01",
//			"stone_hit_sharp_02",
//			"stone_hit_sharp_03"
//		};

//		public static Sound.Handle[] snd_stone_hit_blunt =
//		{
//			//"stone_hit_blunt_00",
//			"stone_hit_blunt_01",
//			//"stone_hit_blunt_02"
//		};

//		public static Sound.Handle[] snd_stone_hit_impact =
//		{
//			"stone.hit.impact.00",
//			"stone.hit.impact.01",
//			"stone.hit.impact.02",
//			"stone.hit.impact.03",
//			"stone.hit.impact.04",
//			"stone.hit.impact.05",
//			"stone.hit.impact.06",
//		};

//		public static Sound.Handle[] snd_wood_hit_sharp =
//		{
//			"wood_hit_sharp_00",
//			"wood_hit_sharp_01",
//			"wood_hit_sharp_02"
//		};

//		public static Sound.Handle[] snd_wood_hit_blunt =
//		{
//			"wood_hit_blunt_00",
//			"wood_hit_blunt_01",
//			"wood_hit_blunt_02"
//		};

//		public static Sound.Handle[] snd_wood_hit_impact =
//		{
//			"wood_break_00",
//		};

//		public static Sound.Handle[] snd_wood_hit_axe =
//		{
//			"wood_hit_axe_00",
//			"wood_hit_axe_01",
//			"wood_hit_axe_02"
//		};

//		public static Sound.Handle[] snd_soil_hit_sharp =
//		{
//			"soil_hit_sharp_00",
//			"soil_hit_sharp_01",
//		};

//		public static Sound.Handle[] snd_soil_hit_impact =
//		{
//			"soil.hit.impact.00",
//			"soil.hit.impact.01",
//			"soil.hit.impact.02",
//			"soil.hit.impact.03"
//		};

//		public static Sound.Handle[] snd_wire_hit_generic =
//		{
//			"wire.hit.generic.00",
//			"wire.hit.generic.01",
//			"wire.hit.generic.02",
//		};

//		public static Sound.Handle[] snd_scrap_hit_shovel =
//		{
//			"scrap.hit.shovel.00",
//			"scrap.hit.shovel.01"
//		};

//		public static Sound.Handle[] snd_metal_hit_sharp =
//		{
//			"metal_hit_sharp_00",
//			"metal_hit_sharp_01",
//			"metal_hit_sharp_02",
//			"metal_hit_sharp_03",
//			"metal_hit_sharp_04"
//		};

//		public static Sound.Handle[] snd_metal_hit_bullet_heavy =
//		{
//			"metal.hit.bullet.heavy.00",
//			"metal.hit.bullet.heavy.01"
//		};

//		public static Sound.Handle[] snd_metal_hit_impact =
//		{
//			"metal.impact.heavy.hollow.00",
//			"metal.impact.heavy.hollow.01",
//			"metal.impact.heavy.hollow.02",
//			"metal.impact.heavy.hollow.03"
//		};

//		public static Sound.Handle[] snd_gravel_hit_sharp =
//		{
//			"gravel_hit_sharp_00",
//			"gravel_hit_sharp_01",
//			"gravel_hit_sharp_02",
//			"gravel_hit_sharp_03"
//		};

//		public static Sound.Handle[] snd_foliage_hit_sharp =
//		{
//			"foliage_hit_sharp_00",
//			"foliage_hit_sharp_01",
//			"foliage_hit_sharp_02",
//			"foliage_hit_sharp_03"
//		};

//		public static Sound.Handle[] snd_glass_hit_sharp =
//		{
//			"glass_hit_sharp_00",
//			//"glass_hit_sharp_01"
//		};

//		public static Sound.Handle[] snd_mushroom_hit_sharp =
//		{
//			"mushroom_hit_sharp_00",
//			"mushroom_hit_sharp_01",
//			"mushroom_hit_sharp_02"
//		};

//		public static Sound.Handle[] snd_paper_hit_generic =
//		{
//			"paper.hit.generic.00",
//			"paper.hit.generic.01"
//		};

//		public static Sound.Handle[] snd_fabric_hit_bullet =
//		{
//			"fabric.hit.bullet.00",
//			"fabric.hit.bullet.01",
//			"fabric.hit.bullet.02"
//		};

//		public static Sound.Handle[] snd_pickaxe =
//		{
//			"pickaxe_hit_0",
//			"pickaxe_hit_1",
//			"pickaxe_hit_2",
//			"pickaxe_hit_3",
//			"pickaxe_hit_4"
//		};

//		public static Texture.Handle[] tex_mask_impact =
//		{
//			"mask.impact.00",
//			"mask.impact.01",
//			"mask.impact.02",
//		};

//		public static Texture.Handle[] tex_mask_explosion =
//		{
//			"mask.explosion.00",
//			"mask.explosion.01",
//			"mask.explosion.02",
//		};

//		//public static void Init()
//		//{
//		//	InitFlesh();
//		//}

//		//public static void InitFlesh()
//		//{
//		//	Health.matrix_hit_effect.SetDefaults(Material.Type.Flesh, (ref Region.Data region, ref XorRandom random, in Health.HitEffectArgs args, bool emit_particles, bool emit_sounds) =>
//		//	{
//		//		if (emit_particles)
//		//		{

//		//		}

//		//		if (emit_sounds)
//		//		{
//		//			Sound.Play(snd_flesh_hit_sharp.GetRandom(ref random), args.world_position, 1.00f, random.NextFloatRange(0.90f, 1.10f), size: 0.70f, priority: 0.30f);
//		//		}
//		//	});

//		//	Health.matrix_hit_effect.SetValues(Material.Type.Flesh, (ref Region.Data region, ref XorRandom random, in Health.HitEffectArgs args, bool emit_particles, bool emit_sounds) =>
//		//	{
//		//		if (emit_particles)
//		//		{
//		//			for (var i = 0; i < 4; i++)
//		//			{
//		//				Particle.Spawn(ref region, new Particle.Data()
//		//				{
//		//					texture = large_smoke_rough,
//		//					lifetime = random.NextFloatRange(0.30f, 0.80f),
//		//					pos = args.world_position + random.NextVector2(0.20f),
//		//					vel = ((args.direction * 2.00f * (1 + i)) + random.NextUnitVector2Range(0.00f, 2.00f)),
//		//					fps = random.NextByteRange(5, 9),
//		//					frame_count = 16,
//		//					frame_count_total = 16,
//		//					frame_offset = 5,
//		//					scale = random.NextFloatRange(0.20f, 0.30f),
//		//					rotation = random.NextFloat(10.00f),
//		//					angular_velocity = random.NextFloat(1.50f),
//		//					growth = random.NextFloatRange(0.30f, 0.50f),
//		//					drag = random.NextFloatRange(0.06f, 0.08f),
//		//					color_a = new Color32BGRA(0xff900000),
//		//					color_b = new Color32BGRA(0xa0400000),
//		//					force = new Vector2(0.00f, i * 2)
//		//				});
//		//			}
//		//		}

//		//		if (emit_sounds)
//		//		{
//		//			Sound.Play(snd_flesh_hit_sharp.GetRandom(ref random), args.world_position, 1.00f, random.NextFloatRange(0.90f, 1.10f), size: 0.70f, priority: 0.30f);
//		//		}
//		//	}, Damage.Type.Bullet_LC, Damage.Type.Bullet_SG, Damage.Type.Bullet_HC, Damage.Type.Bullet_MG, Damage.Type.Bullet_AC, Damage.Type.Bullet_AC, Damage.Type.Bullet_HW, Damage.Type.Bullet_Musket);

//		//	Health.matrix_hit_effect.SetValues(Material.Type.Flesh, (ref Region.Data region, ref XorRandom random, in Health.HitEffectArgs args, bool emit_particles, bool emit_sounds) =>
//		//	{
//		//		if (emit_particles)
//		//		{
//		//			if (args.integrity <= 0.95f)
//		//			{
//		//				for (var i = 0; i < 2; i++)
//		//				{
//		//					Particle.Spawn(ref region, new Particle.Data()
//		//					{
//		//						texture = large_smoke_rough,
//		//						lifetime = random.NextFloatRange(0.50f, 1.10f),
//		//						pos = args.world_position + random.NextVector2(0.60f),
//		//						vel = random.NextUnitVector2Range(2.00f, 6.00f),
//		//						fps = random.NextByteRange(5, 9),
//		//						frame_count = 16,
//		//						frame_count_total = 16,
//		//						frame_offset = 5,
//		//						scale = random.NextFloatRange(0.20f, 0.50f),
//		//						rotation = random.NextFloat(10.00f),
//		//						angular_velocity = random.NextFloat(1.50f),
//		//						growth = random.NextFloatRange(0.30f, 0.50f),
//		//						drag = random.NextFloatRange(0.04f, 0.06f),
//		//						color_a = new Color32BGRA(0xff900000),
//		//						color_b = new Color32BGRA(0xa0400000),
//		//						force = new Vector2(0.00f, 8.50f)
//		//					});
//		//				}
//		//			}
//		//		}

//		//		if (emit_sounds)
//		//		{
//		//			Sound.Play(snd_flesh_hit_sharp.GetRandom(ref random), args.world_position, 1.00f, random.NextFloatRange(0.90f, 1.10f), size: 0.70f, priority: 0.30f);
//		//		}
//		//	}, Damage.Type.Blunt, Damage.Type.Punch, Damage.Type.Club, Damage.Type.Impact, Damage.Type.Ram);

//		//	Health.matrix_hit_effect.SetValues(Material.Type.Flesh, (ref Region.Data region, ref XorRandom random, in Health.HitEffectArgs args, bool emit_particles, bool emit_sounds) =>
//		//	{
//		//		if (emit_particles)
//		//		{
//		//			for (var i = 0; i < 3; i++)
//		//			{
//		//				Particle.Spawn(ref region, new Particle.Data()
//		//				{
//		//					texture = large_smoke_rough,
//		//					lifetime = random.NextFloatRange(0.50f, 1.10f),
//		//					pos = args.world_position + random.NextVector2(0.60f),
//		//					vel = random.NextUnitVector2Range(2.00f, 6.00f),
//		//					fps = random.NextByteRange(5, 9),
//		//					frame_count = 16,
//		//					frame_count_total = 16,
//		//					frame_offset = 5,
//		//					scale = random.NextFloatRange(0.20f, 0.50f),
//		//					rotation = random.NextFloat(10.00f),
//		//					angular_velocity = random.NextFloat(1.50f),
//		//					growth = random.NextFloatRange(0.30f, 0.50f),
//		//					drag = random.NextFloatRange(0.04f, 0.06f),
//		//					color_a = new Color32BGRA(0xff900000),
//		//					color_b = new Color32BGRA(0xa0400000),
//		//					force = new Vector2(0.00f, 8.50f)
//		//				});
//		//			}
//		//		}

//		//		if (emit_sounds)
//		//		{
//		//			Sound.Play(snd_flesh_hit_sharp.GetRandom(ref random), args.world_position, 1.00f, random.NextFloatRange(0.90f, 1.10f), size: 0.70f, priority: 0.30f);
//		//		}
//		//	}, Damage.Type.Pickaxe, Damage.Type.Bite, Damage.Type.Drill, Damage.Type.Slash, Damage.Type.Stab, Damage.Type.Arrow, Damage.Type.Axe, Damage.Type.Sting, Damage.Type.Saw, Damage.Type.Scratch, Damage.Type.Shovel);
//		//}

//#endif
//	}
//}
