
namespace TC2.Base.Components
{
	public static partial class Geyser
	{
#if CLIENT
		public static readonly Texture.Handle texture_fire = "fire2";
		public static readonly Texture.Handle texture_smoke = "BiggerSmoke_Light";
		public static readonly Texture.Handle texture_metal_spark = "Metal_Spark";
		public static readonly Texture.Handle texture_light = "light.circle.00";
#endif

		[IComponent.Data(Net.SendType.Unreliable, region_only: true)]
		public partial struct Data: IComponent
		{
			public float flicker;
			public float radius;

			[Net.Ignore, Save.Ignore] public float next_fire;
			[Net.Ignore, Save.Ignore] public float next_smoke;
		}

		[ISystem.Modified.Component<Geyser.Data>(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnModified(ISystem.Info info, Entity entity, [Source.Owned] ref Geyser.Data geyser, [Source.Owned] ref Body.Data body, [Source.Owned, Pair.Component<Body.Data>] ref Shape.Circle shape)
		{
			shape.radius = geyser.radius * 4.00f;
			//fire.radius = shape.radius;
			entity.MarkModified<Body.Data, Shape.Circle>();
		}

		[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void Update(ISystem.Info info, ref Region.Data region, ref XorRandom random, [Source.Owned] in Transform.Data transform, [Source.Owned] ref Geyser.Data geyser,
		[Source.Owned] ref Sound.Emitter sound_emitter, [Source.Owned, Pair.Component<Geyser.Data>] ref Light.Data light)
		{
#if CLIENT
			light.intensity = 1.00f - random.NextFloatRange(0.00f, geyser.flicker);
			light.scale = new Vector2(geyser.radius * 32.00f) * light.intensity * new Vector2(0.70f, 1.50f);
			light.rotation = Maths.LerpAngle(light.rotation, random.NextFloatRange(-0.40f, 0.40f), 0.10f);

			var pos = transform.position + new Vector2(0.00f, geyser.radius * 2.00f);
			var dir = transform.GetDirection();

			var is_visible = Camera.IsVisible(Camera.CullType.Rect2x, pos);

			if (info.WorldTime >= geyser.next_smoke)
			{
				geyser.next_smoke = info.WorldTime + 0.20f;

				if (is_visible)
				{
					Particle.Spawn(ref region, new Particle.Data()
					{
						texture = texture_smoke,
						lifetime = random.NextFloatRange(3.00f, 5.00f),
						pos = transform.LocalToWorld(random.NextVector2(2.00f) + new Vector2(0, -1)),
						vel = ((new Vector2(0, -2) * random.NextFloatRange(1.50f, 5.00f)) + random.NextVector2(1.00f)).RotateByDir(dir),
						fps = 10,
						force = new Vector2(random.NextFloatRange(0.00f, 2.00f), 0.00f),
						frame_count = 64,
						frame_count_total = 64,
						frame_offset = random.NextByteRange(0, 64),
						//scale = random.NextFloatRange(1.50f, 2.50f),
						scale = random.NextFloatRange(0.60f, 1.00f),
						rotation = random.NextFloat(10.00f),
						angular_velocity = random.NextFloat(0.50f),
						growth = random.NextFloatRange(0.30f, 0.50f),
						color_a = new Color32BGRA(120, 255, 255, 255),
						color_b = new Color32BGRA(0, 255, 255, 255),
					});
				}
			}

			if (info.WorldTime >= geyser.next_fire)
			{
				geyser.next_fire = info.WorldTime + 0.10f;

				if (is_visible)
				{
					Shake.Emit(ref region, pos, random.NextFloatRange(0.10f, 0.40f), 0.50f, random.NextFloatRange(10.00f, 25.00f));

					for (var i = 0; i < 2; i++)
					{
						var lifetime = random.NextFloatRange(0.80f, 1.00f);
						Particle.Spawn(ref region, new Particle.Data()
						{
							pos = transform.LocalToWorld(random.NextVector2(1.50f, 0.00f)),
							scale = random.NextFloatRange(0.95f, 1.30f),
							vel = random.NextVector2Range(new Vector2(-0.50f, 0.50f), new Vector2(-5.00f, -10.00f) * geyser.radius).RotateByDir(dir),
							color_a = new Color32BGRA(255, 255, 255, 255),
							color_b = new Color32BGRA(200, 255, 255, 255),
							fps = random.NextByteRange(8, 10),
							frame_count = 7,
							frame_count_total = 7,
							frame_offset = random.NextByteRange(0, 7),
							texture = texture_fire,
							rotation = random.NextFloatRange(-0.90f, 0.90f),
							lifetime = lifetime,
							growth = -0.60f,
							force = random.NextVector2Range(new Vector2(-0.20f, 0.20f), new Vector2(0.00f, -1.00f)),
							angular_velocity = random.NextFloatRange(-1.00f, 1.00f),
							drag = 0.02f,
							lit = 1.00f
						});

						Particle.Spawn(ref region, new Particle.Data()
						{
							texture = texture_light,
							lifetime = random.NextFloatRange(0.30f, 1.00f),
							pos = transform.LocalToWorld(new Vector2(random.NextFloatRange(-0.50f, 0.50f), -random.NextFloatRange(1.00f, 3.00f))),
							//vel = new Vector2(0.70f, -1.10f) * (1.00f + (modifier2 * 0.10f)),
							vel = new Vector2(random.NextFloatRange(-geyser.radius, geyser.radius), -random.NextFloatRange(5.00f, 20.00f) * geyser.radius).RotateByDir(dir),
							//force = new Vector2(0, 2.50f) * random.NextFloatRange(0.90f, 1.10f),
							scale = random.NextFloatRange(1.00f, 4.00f) * geyser.radius,
							//rotation = random.NextFloatRange(-10.00f, 10.00f),
							angular_velocity = random.NextFloat(1.00f),
							growth = random.NextFloatRange(20.00f, 50.00f),
							drag = random.NextFloatRange(0.02f, 0.04f),
							stretch = new Vector2(random.NextFloatRange(1.25f, 2.00f), random.NextFloatRange(0.50f, 0.70f)),
							color_a = ColorBGRA.RGBA(1.00f, 0.70f, 0.30f, 8.00f),
							color_b = ColorBGRA.RGBA(1.00f, 0.20f, 0.00f, 0.00f),
							glow = random.NextFloatRange(1.00f, 1.50f) * (1.00f + (geyser.radius * 0.25f)),
							face_dir_ratio = 1.00f
						});
					}
				}
			}

			if (is_visible)
			{
				for (var i = 0; i < 2; i++)
				{
					Particle.Spawn(ref region, new Particle.Data()
					{
						texture = texture_metal_spark,
						lifetime = random.NextFloatRange(0.20f, 0.70f),
						pos = transform.LocalToWorld(random.NextVector2(2.50f)),
						vel = new Vector2(random.NextFloatRange(-20, 20), random.NextFloatRange(-2, -40)).RotateByDir(dir),
						fps = 16,
						frame_count = 4,
						frame_offset = (byte)random.NextFloatRange(0.00f, 4.00f),
						frame_count_total = 4,
						scale = random.NextFloatRange(0.50f, 0.90f),
						//rotation = random.NextFloat(MathF.PI),
						//angular_velocity = random.NextFloat(MathF.PI * 10.00f),
						growth = -random.NextFloatRange(0.70f, 2.00f),
						stretch = new Vector2(random.NextFloatRange(1.50f, 4.00f), random.NextFloatRange(0.50f, 1.00f)),
						force = new Vector2(0.00f, random.NextFloatRange(10, 30)),
						drag = random.NextFloatRange(0.01f, 0.10f),
						face_dir_ratio = 1.00f,
						lit = 1.00f
					});
				}
			}
#endif
		}
	}
}
