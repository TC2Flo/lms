
namespace TC2.Base.Components
{
	public static partial class Shitbox
	{
		public static readonly Texture.Handle tex_metal_gibs_lg = "metal.gibs.lg";
		public static readonly Texture.Handle tex_metal_gibs_md = "metal.gibs.md";
		public static readonly Texture.Handle tex_metal_gibs_sm = "metal.gibs.sm";

		[IComponent.Data(Net.SendType.Unreliable, region_only: true)]
		public partial struct Data(): IComponent
		{
			public float mass = 1.00f;
			public float force;
			public float fuel_time = 1.00f;
			public float smoke_amount = 1.00f;
			public Vector2 velocity;

			[Net.Ignore, Save.Ignore] public float smoke_accumulator = 0.00f;
		}


		[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void UpdateBody(ISystem.Info info, Entity entity, [Source.Owned] ref Shitbox.Data shitbox, [Source.Owned] ref Body.Data body, [Source.Owned] ref Transform.Data transform)
		{
			
		}

		[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void UpdateProjectile(ISystem.Info info, Entity entity, [Source.Owned] ref Shitbox.Data shitbox, [Source.Owned] ref Projectile.Data projectile, [Source.Owned] ref Transform.Data transform)
		{

		}

#if CLIENT
		[ISystem.VeryEarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void UpdateSmokeBody(ISystem.Info info, ref Region.Data region, ref XorRandom random, Entity entity, [Source.Owned] ref Shitbox.Data shitbox, [Source.Owned] in Body.Data body, [Source.Owned] in Transform.Data transform)
		{
			//if (shitbox.fuel_time > 0.00f)
			//{
			//	var modifier = Maths.Clamp(shitbox.fuel_time, 0.00f, 1.00f);

			//	shitbox.smoke_accumulator += shitbox.smoke_amount;

			//	while (shitbox.smoke_accumulator >= 1.00f)
			//	{
			//		Particle.Spawn(ref region, new Particle.Data()
			//		{
			//			texture = texture_smoke,
			//			pos = transform.position,
			//			lifetime = random.NextFloatRange(10.00f, 15.00f),
			//			fps = random.NextByteRange(1, 3),
			//			frame_count = 64,
			//			frame_count_total = 64,
			//			frame_offset = random.NextByteRange(0, 64),
			//			scale = random.NextFloatRange(0.10f, 0.15f) * modifier,
			//			angular_velocity = random.NextFloatRange(-0.70f, 0.70f),
			//			force = new Vector2(random.NextFloatRange(4.00f, 8.00f), -random.NextFloatRange(0.10f, 0.50f)),
			//			rotation = random.NextFloat(10.00f),
			//			growth = random.NextFloatRange(0.10f, 0.20f),
			//			color_a = new Color32BGRA(140, 220, 220, 220).WithAlphaMult(modifier * random.NextFloatRange(0.70f, 1.00f)),
			//			color_b = new Color32BGRA(000, 150, 150, 150),
			//			drag = 0.15f,
			//			vel = -body.GetVelocity() * 0.25f
			//		});

			//		shitbox.smoke_accumulator -= 1.00f;
			//	}

			//	//Particle.Spawn(ref region, particle);
			//}
		}
#endif
	}
}
