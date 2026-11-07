namespace TC2.Base.Components
{
	public static class RivetGun
	{
		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public struct Data: IComponent
		{
			public Sound.Handle sound;
			[Net.Ignore, Save.Ignore] public float last_use;
		}

		[ISystem.Add(ISystem.Mode.Single, ISystem.Scope.Region)]
		[ISystem.VeryLateUpdate(ISystem.Mode.Single, ISystem.Scope.Region, interval: 0.50f)]
		public static void UpdateHoldable([Source.Owned] in RivetGun.Data rivet_gun, [Source.Owned] ref Holdable.Data holdable)
		{
			holdable.hints.SetFlag(NPC.ItemHints.Short_Range | NPC.ItemHints.Weapon | NPC.ItemHints.Tools | NPC.ItemHints.Usable, true);
		}

		[ISystem.Event<Melee.HitEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnHit(ISystem.Info info, Entity entity, ref Region.Data region, ref XorRandom random,
		ref Melee.HitEvent data, [Source.Owned] ref RivetGun.Data rivet_gun, [Source.Owned] in Transform.Data transform)
		{
#if CLIENT
			var h_essence = new IEssence.Handle("motion");
			ref var essence_data = ref h_essence.GetData();
			if (essence_data.IsNotNull())
			{
				var dir = transform.GetDirection();
				var pos = transform.position;

				var intensity = 1.00f;
				var color_a = ColorBGRA.Lerp(essence_data.color_emit, ColorBGRA.White, 0.50f);
				var color_b = essence_data.color_emit.WithColorMult(0.20f).WithAlphaMult(0.00f);

				Sound.Play(ref region, rivet_gun.sound, data.world_position, volume: 1.00f, pitch: 1.30f, size: 0.35f, dist_multiplier: 0.65f);
				Shake.Emit(ref region, data.world_position, 0.40f, 0.40f, 10.00f);

				Particle.Spawn(ref region, new Particle.Data()
				{
					texture = Light.tex_light_circle_00,
					lifetime = 0.20f,
					pos = pos - dir,
					vel = dir * 20.00f,
					drag = 0.20f,
					frame_count = 1,
					frame_count_total = 1,
					frame_offset = 0,
					scale = 1.00f,
					stretch = new Vector2(1.00f, 0.50f),
					face_dir_ratio = 1.00f,
					growth = 50.00f,
					color_a = color_a,
					color_b = color_b,
					glow = 20.00f * intensity
				});

				//Particle.Spawn(ref region, new Particle.Data()
				//{
				//	texture = Light.tex_light_circle_04,
				//	lifetime = random.NextFloatRange(1.00f, 1.25f),
				//	pos = data.world_position + (dir * 0.50f),
				//	scale = random.NextFloatRange(1.00f, 1.50f),
				//	growth = random.NextFloatRange(1.50f, 2.00f),
				//	stretch = new Vector2(0.90f, 0.60f),
				//	rotation = dir.GetAngleRadiansFast(),
				//	face_dir_ratio = 1.00f,
				//	color_a = new Vector4(1.00f, 0.70f, 0.40f, 30.00f),
				//	color_b = new Vector4(0.20f, 0.00f, 0.00f, 0.00f),
				//	glow = 1.00f
				//});
			}
#endif

#if SERVER

#endif
		}
	}
}
