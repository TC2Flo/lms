namespace TC2.Base.Components
{
	public static class VacuumCleaner
	{
		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public struct Data(): IComponent
		{
			public Vector2 offset;
			public float max_length = 7.00f;

			[Net.Ignore, Save.Ignore] public float last_use;
			[Net.Ignore, Save.Ignore] public float last_sound;
		}

		[ISystem.Add(ISystem.Mode.Single, ISystem.Scope.Region)]
		[ISystem.VeryLateUpdate(ISystem.Mode.Single, ISystem.Scope.Region, interval: 0.50f)]
		public static void UpdateHoldable([Source.Owned] in VacuumCleaner.Data vacuum, [Source.Owned] ref Holdable.Data holdable)
		{
			holdable.hints.SetFlag(NPC.ItemHints.Short_Range | NPC.ItemHints.Defensive | NPC.ItemHints.Weapon | NPC.ItemHints.Tools | NPC.ItemHints.Usable, true);
		}

		[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void Update(ISystem.Info info, Entity entity, ref Region.Data region, ref XorRandom random,
		[Source.Owned] in Transform.Data transform, [Source.Owned] ref Body.Data body, [Source.Owned] in Control.Data control, [Source.Owned] ref VacuumCleaner.Data vacuum)
		{
			var lmb = control.mouse.GetKey(Mouse.Key.Left);
			var rmb = control.mouse.GetKey(Mouse.Key.Right);

			if (lmb || rmb)
			{
				var aim_dir = transform.GetDirection();

				var pos_a = transform.LocalToWorld(vacuum.offset) + (aim_dir * 1.00f);
				var pos_b = pos_a + (aim_dir * vacuum.max_length);

				var power = 50.00f;
				if (rmb) power *= -1.50f;

				var parent = body.GetParent();

				Span<LinecastResult> results = stackalloc LinecastResult[16];

				if (region.TryLinecastAll(pos_a, pos_b, 3.50f, ref results, mask: Physics.Layer.Entity, exclude: Physics.Layer.World | Physics.Layer.Building | Physics.Layer.Static))
				{
					foreach (ref var result in results)
					{
						if (result.entity == entity || result.entity == parent || result.entity_parent == parent) continue;

						var dir = (pos_a - result.world_position).GetNormalized(out var dist);
						dir = result.normal;

						var t = 1.00f - Maths.Normalize01Fast(dist, vacuum.max_length);

						ref var result_body = ref result.GetBody();
						if (!result_body.IsNull())
						{
							var force = dir * 100.00f * power * t;
							force = Physics.LimitForce2B(ref result_body, force, new Vector2(10));
							result_body.AddForceWorld(force, result.world_position);
						}
					}

					//var reaction_force = -aim_dir * 50.00f * power;
					//reaction_force = Physics.LimitForce(ref body, reaction_force, new(5, 5));
					//body.AddForce(reaction_force);
				}

				//reaction_force = Physics.LimitForce(ref body, reaction_force, new Vector2(10, 10));
			}
		}

		public static readonly Texture.Handle texture_smoke = "BiggerSmoke_Light";

		[ISystem.VeryLateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void UpdateEffects(ISystem.Info info, Entity entity, ref Region.Data region, ref XorRandom random,
		[Source.Owned] in Transform.Data transform, [Source.Owned] in Control.Data control, [Source.Owned] ref VacuumCleaner.Data vacuum, [Source.Owned] ref Sound.Mixer sound_mix)
		{
			var lmb = control.mouse.GetKey(Mouse.Key.Left);
			var rmb = control.mouse.GetKey(Mouse.Key.Right);
			var any = lmb || rmb;

			sound_mix.modifier = (lmb || rmb) ? 1.00f : 0.00f;

#if CLIENT
			if (info.WorldTime >= vacuum.last_sound + 0.20f)
			{
				if (control.mouse.GetKeyUp(Mouse.Key.Left | Mouse.Key.Right))
				{
					vacuum.last_sound = info.WorldTime;
					Sound.Play("vacuum_cleaner.end", transform.position);
				}
				else if (control.mouse.GetKeyDown(Mouse.Key.Left | Mouse.Key.Right))
				{
					vacuum.last_sound = info.WorldTime;
					Sound.Play("vacuum_cleaner.start", transform.position, volume: 0.80f);
				}

			}

			if (lmb || rmb)
			{
				var aim_dir = transform.GetDirection();

				var pos_a = transform.LocalToWorld(vacuum.offset) + (aim_dir * 1.00f);
				var pos_b = pos_a + (aim_dir * vacuum.max_length * 0.80f);

				if (rmb)
				{
					Particle.Spawn(ref region, new Particle.Data()
					{
						texture = texture_smoke,
						lifetime = random.NextFloatRange(0.20f, 0.50f),
						pos = pos_a,
						vel = aim_dir.RotateByRad(random.NextFloatRange(-0.10f, 0.10f)) * vacuum.max_length * 8,
						force = default,
						fps = random.NextByteRange(8, 10),
						frame_count = 64,
						frame_count_total = 64,
						frame_offset = random.NextByteRange(0, 64),
						scale = random.NextFloatRange(0.10f, 0.20f),
						rotation = random.NextFloat(10.00f),
						angular_velocity = random.NextFloat(7.00f),
						growth = random.NextFloatRange(3.00f, 4.00f),
						drag = 0.15f,
						color_a = new Color32BGRA(random.NextByteRange(50, 100), 240, 240, 240),
						color_b = new Color32BGRA(0, 240, 240, 240)
					});
				}
				else
				{
					Particle.Spawn(ref region, new Particle.Data()
					{
						texture = texture_smoke,
						lifetime = random.NextFloatRange(0.20f, 0.40f),
						pos = pos_b,
						vel = -aim_dir.RotateByRad(random.NextFloatRange(-0.10f, 0.10f)) * vacuum.max_length * 10,
						force = default,
						fps = random.NextByteRange(8, 10),
						frame_count = 64,
						frame_count_total = 64,
						frame_offset = random.NextByteRange(0, 64),
						scale = random.NextFloatRange(0.80f, 1.00f),
						rotation = random.NextFloat(10.00f),
						angular_velocity = random.NextFloat(8.00f),
						growth = -random.NextFloatRange(3.00f, 4.00f),
						drag = 0.23f,
						color_a = new Color32BGRA(random.NextByteRange(0, 50), 240, 240, 240),
						color_b = new Color32BGRA(100, 240, 240, 240)
					});
				}
			}
#endif
		}
	}
}
