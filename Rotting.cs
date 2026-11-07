
namespace TC2.Base.Components
{
	public static partial class Rotting
	{
		public static bool enable_effects = true;

		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public partial struct Data(): IComponent
		{
			public float rate = 1.00f;
			[Asset.Ignore] public float rot;
		}

#if SERVER
		[ISystem.AddFirst(ISystem.Mode.Single, ISystem.Scope.Region, defer: true)]
		[HasTag("dead", true, Source.Modifier.Owned), HasTag("initialized", true, Source.Modifier.Owned), HasComponent<Organic.Data>(Source.Modifier.Owned)]
		public static void OnAdd(Entity entity,
		[HasComponent<Rotting.Data>(Source.Modifier.Owned)] bool has_rotting)
		{
			if (!has_rotting)
			{
				//App.WriteLine($"Added rotting to entity {entity}.");
				ref var rotting = ref entity.GetOrAddComponent<Rotting.Data>(sync: true);
				ref var sound_emitter = ref entity.GetOrAddPair<Rotting.Data, Sound.Emitter>(sync: true);
				sound_emitter.file = snd_flies;
				sound_emitter.volume = 0.50f;
				sound_emitter.volume_mult = 0.00f;
				sound_emitter.priority = 0.10f;
				sound_emitter.size = 1.00f;
				sound_emitter.size_max_modifier = 0.40f;
				sound_emitter.flags = Sound.Emitter.Flags.Start_Muted;
			}
			else
			{
				App.WriteLine($"Entity {entity} is already rotting.");
			}
		}
#endif

		public static Sound.Handle snd_flies = "flies.loop.00";
		public static Texture.Handle tex_flies = "flies.00";

		[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region, interval: 0.50f)]
		public static void Update(ISystem.Info info, Entity entity, ref Region.Data region, ref XorRandom random,
		[Source.Owned] ref Rotting.Data rotting, [Source.Owned, Pair.Component<Rotting.Data>] ref Sound.Emitter sound_emitter,
		[Source.Owned] in Body.Data body, [Source.Owned] in Transform.Data transform)
		{
			var can_rot = rotting.rate > 0.00f;
			if (can_rot)
			{
				rotting.rot += (info.DeltaTime * rotting.rate) * (Constants.Organic.rotting_speed * 0.002f);
			}
			//sound_emitter.size_max_modifier = 0.75f;

#if CLIENT
			if (Rotting.enable_effects && Camera.IsVisible(Camera.CullType.Rect2x, transform.position))
			{
				if (!can_rot || (rotting.rot >= 0.15f && !body.GetParent().IsValid()))
				{
					if (can_rot) sound_emitter.volume_mult = Maths.Lerp01(0.10f, 0.30f, (rotting.rot - 0.15f) * 1.50f);
					sound_emitter.pitch_mult = Maths.Lerp(sound_emitter.pitch_mult, random.NextFloatRange(0.80f, 1.20f), 0.20f);

					Particle.Spawn(ref region, new Particle.Data()
					{
						texture = Rotting.tex_flies,
						lifetime = 0.50f,
						pos = transform.position + random.NextVector2(0.50f) + new Vector2(0, -0.75f),
						fps = 14,
						frame_count = 7,
						frame_count_total = 7,
						frame_offset = random.NextByteRange(0, 7),
						//scale = random.NextFloatRange(1.50f, 2.50f),
						scale = random.NextFloatRange(0.90f, 1.10f),
						rotation = random.NextFloat(10.00f),
						angular_velocity = random.NextFloat(0.50f),
						//growth = -0.30f,
						//color_a = new Color32BGRA(170, 65, 60, 70),
						//color_b = new Color32BGRA(0, 30, 30, 35),
					});

					if (random.NextBool(0.20f + (can_rot ? rotting.rot * 0.50f : 0.50f)))
					{
						Particle.Spawn(ref region, new Particle.Data()
						{
							texture = Sludge.texture_smoke,
							lifetime = random.NextFloatRange(2.00f, 5.00f),
							pos = transform.position - new Vector2(0.00f, 0.50f),
							vel = new Vector2(0.10f, -0.10f) * 0.50f,
							force = new Vector2(0.14f, -0.20f) * random.NextFloatRange(0.90f, 1.10f),
							fps = random.NextByteRange(4, 6),
							frame_count = 64,
							frame_count_total = 64,
							frame_offset = random.NextByteRange(0, 64),
							scale = random.NextFloatRange(0.30f, 0.40f),
							rotation = random.NextFloat(10.00f),
							angular_velocity = random.NextFloat(1.70f),
							growth = 0.20f,
							color_a = random.NextColor32Range(new Color32BGRA(100, 150, 180, 25), new Color32BGRA(75, 137, 180, 25)),
							color_b = new Color32BGRA(0, 240, 240, 20)
						});
					}
				}
				else
				{
					sound_emitter.volume_mult = 0.00f;
				}
			}
#endif
		}
	}
}
