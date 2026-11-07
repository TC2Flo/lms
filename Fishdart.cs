namespace TC2.Base.Components
{
	[Shitcode]
	public static class Fishdart
	{
		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Region)]
		public struct Data(): IComponent
		{
			public float fps = 30.00f;
			[Save.Ignore, Net.Ignore] public float next_flip_time = 0.00f;
			[Save.Ignore, Net.Ignore] public bool is_facing_right = true;
		}

		[Shitcode]
		[ISystem.PostUpdate.D(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("dead", false, Source.Modifier.Owned), HasTag("fishdart", true, Source.Modifier.Owned)]
		public static void UpdateAlive(ISystem.Info info, 
		[Source.Owned] ref Fishdart.Data fishdart, [Source.Owned, Override] ref NoRotate.Data no_rotate,
		[Source.Owned] ref Transform.Data transform, [Source.Owned] in Control.Data control,
		[Source.Owned] in Body.Data body,
		[Source.Owned, Override] ref AI.Movement movement)
		{
			var time = info.WorldTime;
			Vector2 vel = body.GetVelocity();
			Vector2 target_pos = movement.position_path ?? movement.position_target ?? control.mouse.position;
			Vector2 dir = target_pos - transform.position;

			if (vel.LengthSquared() > 0.10f)
			{
				dir = vel;
			}

			// Atténue l'axe vertical pour garder un comportement bien horizontal
			dir.Y *= 0.35f; 

			if (dir.LengthSquared() < 0.001f)
			{
				dir = new Vector2(fishdart.is_facing_right ? 1.00f : -1.00f, 0.00f);
			}

			bool desired_face_right = dir.X >= 0.00f;

			// Applique le cooldown de 3 secondes avant d'autoriser un nouveau retournement de sprite
			if (desired_face_right != fishdart.is_facing_right)
			{
				if (time >= fishdart.next_flip_time)
				{
					fishdart.is_facing_right = desired_face_right;
					fishdart.next_flip_time = time + 3.00f; // Cooldown de 3 secondes
				}
			}

			// Applique l'échelle horizontale en fonction de l'état mémorisé
			float scale_abs = Maths.Abs(transform.scale.X);
			if (scale_abs == 0.00f) scale_abs = 1.00f;
			transform.scale.X = fishdart.is_facing_right ? scale_abs : -scale_abs;

			// Calcule l'angle de manière restreinte pour éviter la position verticale
			const float max_angle_rad = 0.60f; // ~34 degrés max
			float target_angle = Maths.Clamp(dir.Y / Maths.Max(0.01f, Maths.Abs(dir.X)), -MathF.Tan(max_angle_rad), MathF.Tan(max_angle_rad));
			float rot = MathF.Atan(target_angle);

			no_rotate.rotation = -rot;
		}

		[Shitcode]
		[ISystem.PostUpdate.D(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("dead", true, Source.Modifier.Owned), HasTag("fishdart", true, Source.Modifier.Owned)]
		public static void UpdateDead(ISystem.Info info, 
		[Source.Owned] ref Fishdart.Data fishdart, [Source.Owned, Override] ref NoRotate.Data no_rotate, 
		[Source.Owned] ref Animated.Renderer.Data renderer)
		{
			no_rotate.rotation = float.Pi;
			renderer.sprite.fps = 0.00f;
		}

#if CLIENT
		[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("dead", false, Source.Modifier.Owned), HasTag("fishdart", true, Source.Modifier.Owned)]
		public static void OnUpdateAnimation(ISystem.Info info, ref XorRandom random,
		[Source.Owned] ref Fishdart.Data fishdart, [Source.Owned] ref Flyer.Data flyer, [Source.Owned] ref Animated.Renderer.Data renderer)
		{
			renderer.sprite.fps = Maths.Round(fishdart.fps * flyer.lift_modifier);
			renderer.offset = Maths.LerpFMA(renderer.offset, random.NextUnitVector2(0.25f), 0.10f);
			renderer.rotation = Maths.Lerp(renderer.rotation, random.NextFloat(0.40f), 0.10f);
		}
#endif
	}
}