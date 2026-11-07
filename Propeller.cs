namespace TC2.Base.Components
{
	public static partial class Propeller
	{
		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Region), IComponent.With<Propeller.State>]
		public partial struct Data(): IComponent
		{
			[Flags]
			public enum Flags: uint
			{
				None = 0u,

				Side = 1u << 0
			}

			public float speed_step = 1.00f;
			public float speed_max = 20.00f;
			public float animation_modifier = 1.00f;

			public float max_tilt = 0.200f;

			public float force_modifier = 100.00f;

			public Propeller.Data.Flags flags;
		}

		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public partial struct State: IComponent
		{
			[Flags]
			public enum Flags: uint
			{
				None = 0u,
			}

			public float target_speed;
			public float current_speed;

			public Propeller.State.Flags flags;
		}

		[ISystem.EarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void UpdateNoRotate(ISystem.Info info, ref Region.Data region,
		[Source.Owned] in Propeller.Data propeller, [Source.Owned] ref Propeller.State propeller_state,
		[Source.Owned, Override] ref NoRotate.Data no_rotate)
		{
			if (propeller.flags.HasAny(Data.Flags.Side))
			{
				var t = Maths.Normalize01(propeller_state.current_speed, propeller.speed_max);

				no_rotate.rotation = Maths.Lerp01(-propeller.max_tilt, propeller.max_tilt, t);

				//var lerp = Maths.Normalize01(propeller.current_speed, propeller.speed_max).Sqrt();
				//no_rotate.bias = Maths.Lerp(0.70f, 0.30f, lerp);
				//no_rotate.speed *= lerp;
				//no_rotate.multiplier *= Maths.Lerp01(-0.50f, 1.00f, lerp.Pow4()).Clamp01();
				//no_rotate.mass_multiplier *= Maths.Lerp01(0.10f, 1.00f, lerp.Pow4());
				//no_rotate.mass_multiplier *= 1.00f + Maths.Max(0.00f, (balloon_state.lift_modifier - 1.00f) * 0.50f);
			}
		}

#if CLIENT
		[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnUpdateEffects(ISystem.Info info, ref Region.Data region, Entity entity, ref XorRandom random,
		[Source.Owned] ref Transform.Data transform,
		[Source.Owned] ref Propeller.Data propeller, [Source.Owned] ref Propeller.State propeller_state,
		[Source.Owned, Optional(true)] ref Vibrate.Data vibrate,
		[Source.Owned, Optional(true)] ref Animated.Renderer.Data renderer,
		[Source.Owned, Pair.Component<Propeller.Data>, Optional(true)] ref Sound.Emitter sound_emitter,
		[Source.Parent, Optional(true)] ref Attachment.Slot attachment_slot)
		{
			var dt = info.DeltaTime;
			var t = Maths.Normalize01(propeller_state.current_speed, propeller.speed_max);
			var t_sqrt = t.Sqrt();

			if (vibrate.IsNotNull())
			{
				vibrate.modifier = t_sqrt;
			}

			if (sound_emitter.IsNotNull())
			{
				sound_emitter.volume_mult = t_sqrt;
				sound_emitter.pitch_mult = 0.50f + (t * 0.50f);
			}

			if (propeller.flags.HasAny(Data.Flags.Side))
			{
				if (renderer.IsNotNull())
				{
					renderer.rotation -= propeller_state.current_speed * propeller.animation_modifier * info.WorldDeltaTime;
				}
			}
			else
			{
				if (renderer.IsNotNull())
				{
					renderer.sprite.fps = MathF.Ceiling(t_sqrt * propeller.animation_modifier);

					//renderer.sprite.fps = 0.00f;
					//if ((region.GetCurrentTick() % ((ulong)Maths.Lerp01(50, 1, t_sqrt * propeller.animation_modifiers))) == 0)
					//{
					//	renderer.sprite.frame.X++; // = MathF.Ceiling(propeller_state.current_speed * propeller.animation_modifier);
					//}
				}

				if (t > 0.10f)
				{
					var particle_size = 0.80f;
					var alpha = 0.50f;
					var dir = transform.GetInterpolatedDirection() * MathF.Sin(info.WorldTime * 40.00f);
					dir += new Vector2(0.00f, random.NextFloatRange(0.10f, 0.80f));

					var len = 1.00f;

					Particle.Spawn(ref region, new Particle.Data()
					{
						texture = Levitator.texture_smoke,
						lifetime = random.NextFloatRange(0.50f, 1.00f) * particle_size,
						pos = transform.LocalToWorldInterpolated(new Vector2(MathF.Sin(info.WorldTime * 40.00f) * 4, 0.20f)),
						vel = dir.RotateByRad(random.NextFloatRange(-0.10f, 0.10f)) * random.NextFloatRange(0.10f, 1.00f) * Maths.Max(len, 10.00f) * 3.00f,
						force = new Vector2(0.00f, -0.10f) + dir.RotateByRad(random.NextFloatRange(-1.00f, 1.00f)) * random.NextFloatRange(0.90f, 1.00f) * Maths.Max(len, 1.00f),
						fps = random.NextByteRange(3, 5),
						frame_count = 64,
						frame_count_total = 64,
						frame_offset = random.NextByteRange(0, 64),
						scale = random.NextFloatRange(0.70f, 0.90f) * particle_size,
						rotation = random.NextFloat(10.00f),
						angular_velocity = random.NextFloat(2.00f),
						growth = random.NextFloatRange(1.00f, 1.50f) * particle_size,
						drag = random.NextFloatRange(0.06f, 0.10f),
						stretch = new(0.80f, 1.50f),
						color_a = new Color32BGRA(random.NextByteRange(80, 120), 240, 240, 240).WithAlphaMult(alpha),
						color_b = new Color32BGRA(0, 240, 240, 240)
					});
				}
			}
		}
#endif

		[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnUpdate(ISystem.Info info, ref Region.Data region, Entity entity, ref XorRandom random,
		[Source.Owned] ref Transform.Data transform, [Source.Owned] in Control.Data control, [Source.Owned] ref Body.Data body,
		[Source.Owned] ref Propeller.Data propeller, [Source.Owned] ref Propeller.State propeller_state,
		[Source.Parent, Optional(true)] ref Attachment.Slot attachment_slot)
		{
			var dt = info.DeltaTime;

			var speed_step = propeller.speed_step;
			var sign = transform.scale.GetParity();

			var rot = Maths.Abs(Maths.NormalizeAngle(body.GetRotation()));

			//var mass = body.GetMass();

			////propeller.current_lift = Maths.Lerp(propeller.current_lift, temperature * 8.00f, 0.50f);

			//if (control.keyboard.GetKey(Keyboard.Key.MoveRight)) speed += propeller.speed * info.DeltaTime;
			//if (control.keyboard.GetKey(Keyboard.Key.MoveLeft)) speed -= propeller.speed * info.DeltaTime;

			if (attachment_slot.IsNotNull())
			{
				var speed = 0.00f;

				if (propeller.flags.HasAny(Data.Flags.Side))
				{
					if (control.keyboard.GetKey(Keyboard.Key.MoveRight)) speed -= speed_step * dt;
					if (control.keyboard.GetKey(Keyboard.Key.MoveLeft)) speed += speed_step * dt;

					if (speed == 0.00f)
					{
						speed = Maths.GetDelta(propeller_state.target_speed, propeller.speed_max * (0.50f - (sign * 0.01f * body.GetVelocity().Length().Abs())), speed_step * dt * 0.80f);
					}
					else
					{

					}

					//var force = new Vector2(0.00f, -propeller.current_speed * propeller.force_modifier);
					//body.AddForce(force);
				}
				else
				{
					if (control.keyboard.GetKey(Keyboard.Key.MoveUp)) speed += speed_step * dt;
					if (control.keyboard.GetKey(Keyboard.Key.MoveDown)) speed -= speed_step * dt;
				}

				propeller_state.target_speed = Maths.Clamp(propeller_state.target_speed + speed, 0.00f, propeller.speed_max);
				propeller_state.current_speed.MoveTowards(propeller_state.target_speed, 10.00f);

				if (propeller.flags.HasAny(Data.Flags.Side))
				{

				}
				else
				{
					var tilt_norm = Maths.Normalize01(rot, propeller.max_tilt).Inv();
					if (tilt_norm > Maths.epsilon)
					{
						var t = Maths.Normalize01(propeller_state.current_speed, propeller.speed_max);

						var force = new Vector2(0.00f, -t.Pow2() * propeller.force_modifier) * tilt_norm;
						body.AddForceLocal(force, Vector2.Zero);
					}
				}
			}
			else
			{
				propeller_state.target_speed = 0.00f;
				propeller_state.current_speed.MoveTowards(propeller_state.target_speed, 10.00f);
			}

#if SERVER
			if (attachment_slot.IsNotNull())
			{
				// TODO: sync it properly
				if (region.GetCurrentTick() % 64 == 0)
				{
					propeller_state.Sync(entity);
				}
			}
#endif
		}
	}
}
