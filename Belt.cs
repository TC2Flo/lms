
namespace TC2.Base.Components
{
	// TODO: Needs Belt.cs exposed
	public static partial class BeltExt
	{
		//[ISystem.EarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		//public static void OnUpdateSound(ISystem.Info info, Entity entity,
		//[Source.Owned, Pair.Component<Axle.Data>] ref Sound.Mixer sound_mix,
		//[Source.Owned] in Axle.Data axle, [Source.Owned] in Axle.State axle_state)
		//{
		//	var modifier = Maths.Clamp01(MathF.Abs(axle_state.angular_velocity) / 10.00f);
		//	sound_mix.modifier = modifier;
		//}

		//[ISystem.Update.C(ISystem.Mode.Single, ISystem.Scope.Region)]
		//public static void OnUpdateWindmill(ISystem.Info info, Entity entity,
		//[Source.Owned] ref Axle.Data axle, [Source.Owned] ref Axle.State axle_state, [Source.Owned] in Windmill.Data windmill)
		//{
		//	axle_state.ApplyTorque(windmill.torque, windmill.max_speed);
		//}

		[ISystem.EarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnUpdateSound(ISystem.Info info, Entity entity,
		[Source.Owned, Pair.Component<Axle.Data>] ref Sound.Emitter sound_emitter,
		[Source.Owned] in Axle.Data axle, [Source.Owned] in Axle.State axle_state)
		{
			sound_emitter.volume_mult = Maths.Clamp01(Maths.Abs(axle_state.angular_velocity) / 40.00f);
		}

		//		[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region, order: 50)]
		//		public static void OnUpdate(ISystem.Info info, ref Region.Data region, Entity entity, [Source.Owned] in Transform.Data transform, [Source.Owned] ref Belt.Data belt)
		//		{
		//			//var ts = Timestamp.Now();

		//			if (belt.a.TryGetHandle(out var handle_a) && belt.b.TryGetHandle(out var handle_b) && belt.a_state.TryGetHandle(out var handle_a_state) && belt.b_state.TryGetHandle(out var handle_b_state))
		//			{
		//				ref var a = ref handle_a.data;
		//				ref var b = ref handle_b.data;
		//				ref var a_state = ref handle_a_state.data;
		//				ref var b_state = ref handle_b_state.data;

		//				var gear_ratio = a.ratio;
		//				if (gear_ratio <= 0.00f) gear_ratio = 1.00f;

		//				var sign = (belt.flags.HasAny(Belt.Flags.Crossed) ? -1.00f : 1.00f);

		//				var radius_a = a.radius_a;
		//				var radius_b = b.radius_b;

		//				var diameter_a = radius_a * 2.00f;
		//				var diameter_b = radius_b * 2.00f;

		//				var w_a = (a_state.angular_velocity);
		//				var w_b = (b_state.angular_velocity * sign);

		//				var diameter_ratio_a2b = diameter_a / diameter_b;
		//				var diameter_ratio_b2a = diameter_b / diameter_a;

		//				var torque_a2b_req = 0.00f;
		//				var torque_b2a_req = 0.00f;

		//				var torque_a2b = 0.00f;
		//				var torque_b2a = 0.00f;

		//				var torque_belt_a = 0.00f;
		//				var torque_belt_b = 0.00f;

		//				var v_belt = 0.00f;

		//				var w_delta_a = 0.00f;
		//				var w_delta_b = 0.00f;

		//				Maths.MinMax(diameter_a, diameter_b, out var diameter_sm, out var diameter_lg);
		//				var angle = MathF.Asin((diameter_lg - diameter_sm) / (belt.distance * 2.00f));
		//				var angle_wrap = MathF.PI - (angle * 2.00f);

		//				var tension = 1500.00f;
		//				var tension_tight = tension;
		//				var tension_slack = tension / MathF.Exp(belt.friction * angle_wrap);
		//				var effective_force = (tension_tight - tension_slack);

		//				torque_belt_a = (effective_force * radius_a);
		//				torque_belt_b = (effective_force * radius_b);

		//				w_delta_a = (w_b * diameter_ratio_b2a) - w_a;
		//				w_delta_b = (w_a * diameter_ratio_a2b) - w_b;

		//				torque_a2b_req = MathF.Abs(w_delta_a * b_state.moment_of_inertia * App.tickrate);
		//				torque_a2b = Maths.Min(torque_belt_a, torque_a2b_req) * belt.friction;

		//				torque_b2a_req = MathF.Abs(w_delta_b * a_state.moment_of_inertia * App.tickrate);
		//				torque_b2a = Maths.Min(torque_belt_b, torque_b2a_req) * belt.friction;

		//				//a_state.ApplyTorque(torque_b2a, w_b * diameter_ratio_b2a);
		//				//b_state.ApplyTorque(torque_a2b, w_a * diameter_ratio_a2b);

		//				var v_belt_a = (w_a * radius_a);
		//				var v_belt_b = (w_b * radius_b);
		//				v_belt = Maths.Avg(v_belt_a, v_belt_b);

		//				var angular_velocity_a2b = Maths.ClampMagnitude(v_belt, belt.speed_max) * diameter_ratio_a2b * sign;
		//				var angular_velocity_b2a = Maths.ClampMagnitude(v_belt, belt.speed_max) * diameter_ratio_b2a * sign;
		//				var friction_force = (a_state.force_load_old / gear_ratio) + (b_state.force_load_old * gear_ratio);

		//#if CLIENT
		//				region.DrawDebugText(transform.position + new Vector2(0.00f, 2.00f),
		//					$"w_a2b: {angular_velocity_a2b:0.000}\n" +
		//					$"w_b2a: {angular_velocity_b2a:0.000}\n" +
		//					//$"friction: {friction_force:0.000}\n" +
		//					//$"ratio: {diameter_ratio:0.000} ({diameter_a}:{diameter_b})\n" +
		//					$"ratio_a2b: {diameter_ratio_a2b:0.000}\n" +
		//					$"ratio_b2a: {diameter_ratio_b2a:0.000}\n" +
		//					$"v_belt: {v_belt:0.00} m/s\n" +
		//					$"v_belt_a: {v_belt_a:0.00} m/s\n" +
		//					$"v_belt_b: {v_belt_b:0.00} m/s\n" +
		//					$"w_delta_a: {w_delta_a:0.00} m/s\n" +
		//					$"w_delta_b: {w_delta_b:0.00} m/s\n" +
		//					$"tq_belt_a: {torque_belt_a:0.000}\n" +
		//					$"tq_belt_b: {torque_belt_b:0.000}\n" +
		//					$"tq_a2b_req: {torque_a2b_req:0.000}\n" +
		//					$"tq_b2a_req: {torque_b2a_req:0.000}\n" +
		//					$"tq_a2b: {torque_a2b:0.000}\n" +
		//					$"tq_b2a: {torque_b2a:0.000}"
		//					, Color32BGRA.White);
		//#endif

		//				belt.velocity = v_belt;
		//				belt.phase += (v_belt * App.fixed_update_interval_s) * belt.scale;
		//			}
		//			else
		//			{
		//#if SERVER
		//				entity.Delete();
		//#endif
		//			}

		//			//var ms = ts.GetMilliseconds();
		//			//App.WriteLine($"time: {ms:0.000000} ms");
		//		}


		[Shitcode]
		[ISystem.PreUpdate.C(ISystem.Mode.Single, ISystem.Scope.Region, order: 50), HasTag("initialized", true, Source.Modifier.Owned)]
		public static void OnUpdateA(ISystem.Info info, ref Region.Data region, Entity entity, 
		[Source.Owned] in Transform.Data transform, [Source.Owned] ref Belt.Data belt)
		{
			//var ts = Timestamp.Now();

			if (belt.a.TryGetHandle(out var handle_a) && belt.b.TryGetHandle(out var handle_b) && belt.a_state.TryGetHandle(out var handle_a_state) && belt.b_state.TryGetHandle(out var handle_b_state))
			{
				ref var a = ref handle_a.data;
				ref var b = ref handle_b.data;
				ref var a_state = ref handle_a_state.data;
				ref var b_state = ref handle_b_state.data;

				var gear_ratio = a.ratio;
				if (gear_ratio <= 0.00f) gear_ratio = 1.00f;

				var modifier = b.modifier.WithRoundedEpsilon().WithUnsignedZero();
				var modifier_abs = modifier.Abs();
				var sign = (belt.flags.HasAny(Belt.Flags.Crossed) ? -1.00f : 1.00f);
				var sign_modifier = modifier.Sign();

				var radius_a = a.radius_a;
				var radius_b = b.radius_b;

				var diameter_a = radius_a * 2.00f;
				var diameter_b = radius_b * 2.00f;

				var w_a = (a_state.angular_velocity);
				var w_b = (b_state.angular_velocity * sign * modifier);

				var ratio_a2b = diameter_a / diameter_b;
				var ratio_b2a = diameter_b / diameter_a;

				var tq_a2b_req = 0.00f;
				var tq_b2a_req = 0.00f;

				var tq_a2b = 0.00f;
				var tq_b2a = 0.00f;

				var tq_belt_a_max = 0.00f;
				var tq_belt_b_max = 0.00f;

				var v_belt = 0.00f;

				var w_delta_a = 0.00f;
				var w_delta_b = 0.00f;

				var w_a2b = 0.00f;
				var w_b2a = 0.00f;

				var w_a2b_acc = 0.00f;
				var w_b2a_acc = 0.00f;

				var a_moment_of_inertia = a_state.moment_of_inertia;
				var b_moment_of_inertia = b_state.moment_of_inertia;
				//var c_moment_of_inertia = a_moment_of_inertia + b_moment_of_inertia;

				var a_moment_of_inertia_inv = a_state.moment_of_inertia_inv;
				var b_moment_of_inertia_inv = b_state.moment_of_inertia_inv;
				//var c_moment_of_inertia_inv = 1.00f / c_moment_of_inertia;

				Maths.MinMax(diameter_a, diameter_b, out var diameter_sm, out var diameter_lg);
				var angle = MathF.Asin((diameter_lg - diameter_sm) / (belt.distance * 2.00f));
				var angle_wrap = MathF.PI - (angle * 2.00f);

				var tension = belt.force;
				var tension_tight = tension;
				var tension_slack = tension / MathF.Exp(belt.friction * angle_wrap);
				var effective_force = (tension_tight - tension_slack).Abs();

				var slip_a = 1.00f - ((Maths.Normalize01(w_a.Abs(), belt.speed_max)));
				var slip_b = 1.00f - ((Maths.Normalize01(w_b.Abs(), belt.speed_max)));

				tq_belt_a_max = (effective_force * radius_a);
				tq_belt_b_max = (effective_force * radius_b);

				//a_state.ApplyTorque(tq_belt_a_max, b_state.angular_velocity);
				//b_state.ApplyTorque(tq_belt_b_max, a_state.angular_velocity);


				//a_state.ApplyTorque(tq_belt_a_max, Maths.Avg(w_a, w_b));
				//b_state.ApplyTorque(tq_belt_b_max, Maths.Avg(w_a, w_b));

				//a_state.ApplyTorque(tq_belt_a_max * 0.50f, w_b);
				//b_state.ApplyTorque(tq_belt_b_max * 0.50f, w_a);

				w_a2b = w_a * ratio_a2b;
				w_b2a = w_b * ratio_b2a;

				w_delta_a = w_b2a - w_a;
				w_delta_b = w_a2b - w_b;

				//tq_a2b_req = (w_delta_a) * b_moment_of_inertia;
				//tq_b2a_req = (w_delta_b );

				//var w_a2b_acc_max = tq_belt_a_max * b_moment_of_inertia_inv;
				//var w_b2a_acc_max = 0.00f;

				//tq_a2b = Maths.ClampMagnitude(tq_a2b_req, tq_belt_a_max * App.fixed_update_interval_s); // * App.fixed_update_interval_s;
				//tq_b2a = Maths.ClampMagnitude(tq_b2a_req, tq_belt_b_max * App.fixed_update_interval_s); // * App.fixed_update_interval_s;

				//w_a2b_acc = -(tq_a2b);
				//w_b2a_acc = -(tq_b2a);

				//tq_a2b = Maths.ClampMagnitude(-((w_delta_a * b_moment_of_inertia) + (a_state.force_load_old * App.fixed_update_interval_s * belt.friction)), tq_belt_a_max);
				//tq_b2a = Maths.ClampMagnitude(-((w_delta_b * a_moment_of_inertia) + (b_state.force_load_old * App.fixed_update_interval_s * belt.friction)), tq_belt_b_max);

				tq_a2b = Maths.ClampMagnitude(-((w_delta_a * b_moment_of_inertia)) * App.tickrate_f32, tq_belt_a_max);
				tq_b2a = Maths.ClampMagnitude(-((w_delta_b * a_moment_of_inertia)) * App.tickrate_f32, tq_belt_b_max);


				//tq_a2b = Maths.ClampMagnitude(-((w_delta_a * b_moment_of_inertia)), tq_belt_a_max);
				//tq_b2a = Maths.ClampMagnitude(-((w_delta_b * a_moment_of_inertia)), tq_belt_b_max);


				w_a2b_acc = tq_a2b * b_moment_of_inertia_inv;
				w_b2a_acc = tq_b2a * a_moment_of_inertia_inv;

				tq_a2b = Maths.Abs(tq_a2b);
				tq_b2a = Maths.Abs(tq_b2a);

				var v_belt_a = ((w_a) * radius_a);
				var v_belt_b = ((w_b) * radius_b);

				//var v_belt_a = (((w_a) - (w_b2a_acc * ratio_b2a))) * radius_a;
				//var v_belt_b = (((w_b) - (w_a2b_acc * ratio_a2b))) * radius_b;

				//var v_belt_a = a_state.angular_velocity; // (((w_a))) * radius_a;
				//var v_belt_b = b_state.angular_velocity; // (((w_b))) * radius_b;


				//var v_belt_a = (((w_a) - (w_b2a_acc))) / radius_a;
				//var v_belt_b = (((w_b) - (w_a2b_acc))) / radius_b;
				v_belt = Maths.Avg(v_belt_a, v_belt_b);

				//a_state.ApplyTorque(tq_belt_a_max, v_belt);
				//b_state.ApplyTorque(tq_belt_b_max, v_belt);

				//a_state.ApplyTorque(tq_belt_a_max * slip_a, Maths.Clamp(b_state.angular_velocity, -belt.speed_max, belt.speed_max));
				//b_state.ApplyTorque(tq_belt_b_max * slip_b, Maths.Clamp(a_state.angular_velocity, -belt.speed_max, belt.speed_max));


				a_state.ApplyTorque(tq_belt_a_max * slip_a * belt.efficiency * modifier_abs, b_state.angular_velocity * sign_modifier);
				b_state.ApplyTorque(tq_belt_b_max * slip_b * belt.efficiency * modifier_abs, a_state.angular_velocity * sign_modifier);


				var load_a = tq_belt_a_max * belt.friction * slip_a * 0.03f;
				var load_b = tq_belt_b_max * belt.friction * slip_b * 0.03f;

				a_state.force_load_new += load_a;
				b_state.force_load_new += load_b;

				belt.force_a2b = load_a;
				belt.force_b2a = load_b;

#if CLIENT
				const bool draw_debug = false;
				if (draw_debug)
				{
					region.DrawDebugText(transform.position + new Vector2(0.00f, 2.00f),
						//$"w_a2b: {angular_velocity_a2b:0.000}\n" +
						//$"w_b2a: {angular_velocity_b2a:0.000}\n" +
						//$"friction: {friction_force:0.000}\n" +
						//$"ratio: {diameter_ratio:0.000} ({diameter_a}:{diameter_b})\n" +
						$"tension: {effective_force:0.000} ({tension:0.000}-{tension_slack:0.000})\n" +
						$"ratio_a2b: {ratio_a2b:0.000}\n" +
						$"ratio_b2a: {ratio_b2a:0.000}\n" +
						$"v_belt: {v_belt:0.00} m/s\n" +
						$"v_belt_a: {v_belt_a:0.00} m/s\n" +
						$"v_belt_b: {v_belt_b:0.00} m/s\n" +
						$"w_belt_a1: {(v_belt / radius_a):0.00} rad/s\n" +
						$"w_belt_a2: {(v_belt / radius_a).WithSign(modifier):0.00} rad/s\n" +
						$"w_belt_b1: {(v_belt / radius_b):0.00} rad/s\n" +
						$"w_belt_b2: {(v_belt / radius_b).WithSign(modifier):0.00} rad/s\n" +
						$"net_torque_a: {a_state.app_torque:0000.00}+{a_state.net_torque:0000.00}/{a_state.sum_torque:0000.00}\n" +
						$"net_torque_b: {b_state.app_torque:0000.00}+{b_state.net_torque:0000.00}/{b_state.sum_torque:0000.00}\n" +
						$"load_a: {a_state.force_load_old:0.000}/{a_state.force_load_new:0.000}\n" +
						$"load_b: {b_state.force_load_old:0.000}/{b_state.force_load_new:0.000}\n" +
						$"w_a2b: {w_a2b:0.000}\n" +
						$"w_b2a: {w_b2a:0.000}\n" +
						$"w_delta_a: {w_delta_a:0.00} m/s\n" +
						$"w_delta_b: {w_delta_b:0.00} m/s\n" +
						$"w_a2b_acc: {w_a2b_acc:0.000}\n" +
						$"w_b2a_acc: {w_b2a_acc:0.000}\n" +
						$"tq_belt_a_max: {tq_belt_a_max:0.000}\n" +
						$"tq_belt_b_max: {tq_belt_b_max:0.000}\n" +
						$"tq_a2b_req: {tq_a2b_req:0.000}\n" +
						$"tq_b2a_req: {tq_b2a_req:0.000}\n" +
						$"tq_a2b: {tq_a2b:0.000}\n" +
						$"tq_b2a: {tq_b2a:0.000}"
						, Color32BGRA.White);
				}
#endif

				//AxleExt.time_step = 1;
				if (AxleExt.debug_timestep && region.GetCurrentTick() % AxleExt.time_step != 0) return;

				if (a_state.angular_velocity.Abs() >= 100.000 || b_state.angular_velocity.Abs() >= 100.00f)
				{
					a_state.angular_velocity = 0.00f;
					a_state.angular_momentum = 0.00f;

					b_state.angular_velocity = 0.00f;
					b_state.angular_momentum = 0.00f;

					belt.velocity = 0.00f;

					App.WriteLine("dumb", App.Color.Yellow);

				}
				else
				{
					//a_state.ApplyTorque(tq_b2a * modifier_abs, (v_belt / radius_a));
					//b_state.ApplyTorque(tq_a2b * modifier_abs, (v_belt / radius_b) * sign_modifier);
				}

				belt.velocity = v_belt;
				belt.phase += (v_belt * App.fixed_update_interval_s) * belt.scale;
			}
			else
			{
#if SERVER
				entity.Delete();
#endif
			}

			//var ms = ts.GetMilliseconds();
			//App.WriteLine($"time: {ms:0.000000} ms");
		}
	}
}
