namespace TC2.Base.Components
{
	public static partial class RecoilCompensator
	{
		[IComponent.Data(Net.SendType.Reliable, region_only: true)]
		public partial struct Data(): IComponent
		{
			//public Vector4 color = Essence.GetColor(Essence.Type.Motion);

			public IEssence.Handle h_essence = "motion";

			public Sound.Handle sound = "recoil_compensator.00";
			public float sound_volume_mult = 1.30f;
			public float sound_pitch = 1.00f;

			[Statistics.Info("Modifier", description: "TODO: Desc", format: "{0:0.##}x", comparison: Statistics.Comparison.None, priority: Statistics.Priority.Medium)]
			public float modifier = 1.00f;

			//[Statistics.Info("Ratio", description: "TODO: Desc", format: "{0:0.##}", comparison: Statistics.Comparison.None, priority: Statistics.Priority.Medium)]
			//public float ratio = 1.00f;

			[Statistics.Info("Force", description: "TODO: Desc", format: "{0:0.00}N", comparison: Statistics.Comparison.None, priority: Statistics.Priority.Medium)]
			public float force;

			[Statistics.Info("Offset", description: "TODO: Desc", format: "{0:0.##}", comparison: Statistics.Comparison.None, priority: Statistics.Priority.Medium), Editor.Picker.Position(relative: true)]
			public Vector2 offset;

			[Save.Ignore, Net.Ignore] public float current_power;
		}

		public static readonly Texture.Handle tex_light = "light.circle.00";

		[ISystem.EarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnUpdate(ref XorRandom random, ref Region.Data region, ISystem.Info info, Entity entity,
		[Source.Owned] in Transform.Data transform, [Source.Owned] in Control.Data control, [Source.Owned] ref Body.Data body,
		[Source.Owned] ref Gun.Data gun, [Source.Owned] ref Gun.State gun_state,
		[Source.Owned] ref RecoilCompensator.Data recoil_compensator)
		{
			if (gun_state.stage == Gun.Stage.Fired)
			{
				recoil_compensator.current_power = 1.00f;

#if CLIENT
				ref var essence_data = ref recoil_compensator.h_essence.GetData();
				if (essence_data.IsNotNull())
				{
					var dir = -transform.GetDirection();
					var pos = transform.LocalToWorld(recoil_compensator.offset);

					var intensity = 1.50f + Maths.Clamp((recoil_compensator.force.Abs() / essence_data.emit_force.Abs()) * 2.00f, 0.00f, 10.00f);
					var color_a = ColorBGRA.Lerp(essence_data.color_emit, ColorBGRA.White, 0.80f).WithAlphaMult(2);
					var color_b = essence_data.color_emit.WithColorMult(0.20f).WithAlphaMult(0.00f);

					Sound.Play(recoil_compensator.sound, pos, volume: recoil_compensator.sound_volume_mult, pitch: recoil_compensator.sound_pitch, size: 1.20f, dist_multiplier: 1.10f);

					Particle.Spawn(ref region, new Particle.Data()
					{
						texture = tex_light,
						lifetime = 0.20f,
						pos = pos,
						vel = dir * 30.00f,
						drag = 0.50f,
						frame_count = 1,
						frame_count_total = 1,
						frame_offset = 0,
						scale = 0.70f,
						stretch = new Vector2(0.70f, 0.50f),
						face_dir_ratio = 1.00f,
						growth = 50.00f,
						color_a = color_a,
						color_b = color_b,
						lit = 0,
						glow = intensity *8
					});
				}
#endif
			}
			else
			{
				if (recoil_compensator.current_power > Maths.epsilon)
				{
					var force = transform.LocalToWorld(gun.muzzle_offset - gun.receiver_offset, position: false).GetNormalized() * recoil_compensator.current_power * recoil_compensator.force * App.tickrate_f32;
					var recoil_force = gun_state.last_recoil.Length();

//#if CLIENT
//					App.WriteLine($"{gun_state.last_recoil:0.00}; {force:0.00}; {recoil_force:0.00}; {force.Length():0.00}");
//#endif

					body.AddForceWorld(force, transform.LocalToWorld(recoil_compensator.offset));
					recoil_compensator.current_power = Maths.MoveTowards(recoil_compensator.current_power, 0.00f, info.DeltaTime * 150.00f * recoil_compensator.modifier);
				}
			}
		}
	}
}
