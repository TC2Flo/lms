namespace TC2.Base.Components
{
	public static partial class ArcaneAccelerator
	{
		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Region)]
		public partial struct Data(): IComponent
		{
			//public Vector4 color = Essence.GetColor(Essence.Type.Motion);

			public IEssence.Handle h_essence = "motion";

			public Sound.Handle sound = "gun.accelerator.arcane.01";
			public float sound_volume_mult = 1.30f;
			public float sound_pitch = 1.00f;

			//[Statistics.Info("Modifier", description: "TODO: Desc", format: "{0:0.##}x", comparison: Statistics.Comparison.None, priority: Statistics.Priority.Medium)]
			//public float modifier = 1.00f;

			//[Statistics.Info("Force", description: "TODO: Desc", format: "{0:0.00}N", comparison: Statistics.Comparison.None, priority: Statistics.Priority.Medium)]
			//public float force = 0.00f;

			[Statistics.Info("Offset", description: "TODO: Desc", format: "{0:0.##}", comparison: Statistics.Comparison.None, priority: Statistics.Priority.Medium), Editor.Picker.Position(relative: true)]
			public Vector2 offset;

			//[Save.Ignore, Net.Ignore] public float current_power = 0.00f;
		}

		public static readonly Texture.Handle tex_light = "light.circle.00";

		// TODO: this can be made client-only
		[Shitcode]
		[ISystem.EarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnUpdate(ref XorRandom random, ref Region.Data region, ISystem.Info info, Entity entity,
		[Source.Owned] in Transform.Data transform, [Source.Owned] in Control.Data control, [Source.Owned] ref Body.Data body,
		[Source.Owned] ref Gun.Data gun, [Source.Owned] ref Gun.State gun_state,
		[Source.Owned] ref ArcaneAccelerator.Data arcane_accelerator)
		{
			if (gun_state.stage == Gun.Stage.Fired)
			{
#if CLIENT
				ref var essence_data = ref arcane_accelerator.h_essence.GetData();
				if (essence_data.IsNotNull())
				{
					var dir = transform.GetDirection();
					var pos = transform.LocalToWorld(arcane_accelerator.offset);

					var intensity = 1.00f;
					var color_a = ColorBGRA.Lerp(essence_data.color_emit, ColorBGRA.White, 0.50f);
					var color_b = essence_data.color_emit.WithColorMult(0.20f).WithAlpha(0.00f);

					Sound.Play(sound: arcane_accelerator.sound,
						world_position: pos,
						volume: arcane_accelerator.sound_volume_mult * 2.75f,
						pitch: arcane_accelerator.sound_pitch * 1.65f,
						size: 0.30f,
						dist_multiplier: 1.50f);

					Particle.Spawn(ref region, new Particle.Data()
					{
						texture = tex_light,
						lifetime = 0.20f,
						pos = pos - (dir * 2.00f),
						vel = dir * 20.00f,
						drag = 0.20f,
						frame_count = 1,
						frame_count_total = 1,
						frame_offset = 0,
						scale = 1.00f,
						stretch = new Vector2(1.50f, 0.50f),
						face_dir_ratio = 1.00f,
						growth = 50.00f,
						color_a = color_a,
						color_b = color_b,
						glow = 14.00f * intensity
					});
				}
#endif
			}
		}
	}
}
