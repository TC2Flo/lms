
using Keg.Engine.Game;

namespace TC2.Base.Components
{
	public static partial class Gas
	{
		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Region)]
		public partial struct Data(): IComponent
		{
			public float radius = 4.00f;
			public float size = 1.00f;
			public float lifetime = 10.00f;
			public float speed = 1.00f;
			public float interval = 0.50f;

			public Color32BGRA color_a = new Color32BGRA(100, 255, 255, 255);
			public Color32BGRA color_b = new Color32BGRA(0, 255, 255, 255);

			[Asset.Ignore] public float modifier = 1.00f;
			[Asset.Ignore, Net.Ignore, Save.Ignore] public float next_smoke;
		}

		public static readonly Texture.Handle texture_smoke = "BiggerSmoke_Light";

		[ISystem.Modified.Component<Resource.Data>(ISystem.Mode.Single, ISystem.Scope.Region, order: 400)]
		public static void OnResourceModified(Entity entity, [Source.Owned] in Resource.Data resource, [Source.Owned] ref Gas.Data gas)
		{
			ref var material = ref resource.material.GetData();
			if (material.IsNotNull())
			{
				//var modifier = MathF.Log2(1.00f + (resource.quantity / Maths.Max(material.quantity_max, 1.00f)));
				gas.modifier = Maths.Normalize01Fast(resource.quantity, resource.GetMaxQuantity());
			}
		}

#if CLIENT
		[ISystem.VeryLateUpdate(ISystem.Mode.Single, ISystem.Scope.Region, interval: 0.117f)]
		public static void UpdateEffects(ISystem.Info info, Entity entity, ref Region.Data region, ref XorRandom random,
		[Source.Owned] in Transform.Data transform, [Source.Owned] ref Gas.Data gas)
		{
			if (info.WorldTime >= gas.next_smoke && gas.modifier > 0.01f)
			{
				//var ts = Timestamp.Now();
				gas.next_smoke = info.WorldTime + (gas.interval * random.NextFloatRange(0.60f, 1.00f));

				var pos = transform.GetInterpolatedPosition();
				if (Camera.IsVisible(Camera.CullType.Rect2x, transform.position))
				{
					var radius = gas.radius * gas.modifier;
					var area = (MathF.PI * radius * radius);

					var count = (int)MathF.Round(Maths.Clamp(MathF.Log2(area) * 0.50f, 1.00f, 10.00f));
					var step = 1.00f / count;

					//App.WriteLine(count);
					for (var i = 0; i < count; i++)
					{
						var ratio = i * step;

						//var p = region.GetNearestPathPos(transform.LocalToWorld(gas.offset + (random.NextUnitVector2Range(radius * ((i + 0) * step), radius * ((i + 1.50f) * step)) * new Vector2(2.00f, 0.25f)))) + random.NextUnitVector2Range(radius * step * 0.50f, radius * step);
						var p = region.GetNearestPathPos(pos + random.NextUnitVector2Range(0.00f, radius * ((i + 1.50f) * step)) * new Vector2(2.00f, 0.25f)) + random.NextUnitVector2Range(radius * step * 0.50f, radius * step);
						p = Vector2.Lerp(p, region.GetNearestHighPathPos(p, include: Terrain.PathFlags.Ground | Terrain.PathFlags.Wall, exclude: Terrain.PathFlags.Solid) + new Vector2(0.00f, -3 * gas.size), random.NextFloatRange(0.00f, 0.20f));

						//var p = region.GetNearestPathPos(region.GetNearestHighPathPos(transform.LocalToWorld(gas.offset + (random.NextUnitVector2Range(radius * ((i + 0) * step), radius * ((i + 1.50f) * step)) * new Vector2(2.00f, 0.25f)))) + random.NextUnitVector2Range(radius * step * 0.50f, radius * step), exclude: Terrain.PathFlags.Solid | Terrain.PathFlags.Ground);
						//var p = region.GetNearestPathPos(region.GetNearestHighPathPos(transform.LocalToWorld(gas.offset + random.NextVector2Range(new Vector2((i + 2.00f) * -radius * step, (i + 2.00f) * radius * step), new Vector2(-0.50f, 0.00f)))) + random.NextUnitVector2Range(radius * step * 0.50f, radius * step), exclude: Terrain.PathFlags.Solid | Terrain.PathFlags.Ground);

						//region.DrawDebugCircle(p, 0.125f, Color32BGRA.Red, 4.00f);

						Particle.Spawn(ref region, new Particle.Data()
						{
							texture = texture_smoke,
							lifetime = gas.lifetime * random.NextFloatRange(0.50f, 1.00f),
							//pos = region.GetNearestHighPathPos(transform.LocalToWorld(gas.offset + random.NextUnitVector2Range(radius * ((i) * step), radius * ((i + 1) * step)))),
							//pos = region.GetNearestHighPathPos(transform.LocalToWorld(gas.offset + random.NextUnitVector2Range(radius * ((i) * step), radius * ((i + 1) * step)))),
							pos = p,
							vel = new(random.NextFloatRange(-0.80f, 0.80f), -0.05f),
							force = new(random.NextFloatRange(-0.80f, 0.80f), random.NextFloatRange(-0.05f, 0.05f)),
							fps = (byte)(random.NextFloatRange(10, 16) * random.NextFloatExtra(1.00f, gas.speed * 0.10f)),
							frame_count = 64,
							frame_count_total = 64,
							frame_offset = random.NextByteRange(0, 64),
							scale = random.NextFloatRange(0.60f, 1.00f) * gas.size,
							rotation = random.NextFloat(10.00f),
							angular_velocity = random.NextFloat(2.00f * gas.speed),
							growth = random.NextFloatRange(0.15f, 0.30f) * gas.speed,
							color_a = gas.color_a.WithAlphaMult(random.NextFloatExtra(0.30f, gas.modifier * 0.70f)),
							color_b = gas.color_b,
							drag = random.NextFloatRange(0.001f, 0.01f)
						});
					}
				}

				//App.WriteLine($"{ts.GetMilliseconds():0.0000} ms");
			}
		}
#endif
	}
}