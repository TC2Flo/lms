
namespace TC2.Base.Components
{
	public static partial class AI
	{
		// TODO: this is a mess
		public static partial class Gunner
		{
			[Flags]
			public enum Flags: uint
			{
				None = 0,

				Aggressive = 1 << 0,
				Passive = 1 << 1
			}

			public enum Mode: uint
			{
				Undefined = 0,
				Vehicle
			}

			[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Region)]
			public partial struct Data(): IComponent //, IOverridable
			{
				public Gunner.Flags flags;

				//public float aim_min_dot = 0.97f;
				//public float aim_speed_mult = 1.00f;
				public float aim_time_mult = 1.00f;
				public float aim_decay_lerp = 0.70f;

				public float attack_distance = 4.00f;
				public float attack_cooldown = 1.50f;
				public float attack_burst_time = 0.50f;
				public float reload_cooldown = 1.00f;

				public float unused_00;
			}

			[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
			public partial struct State: IComponent
			{
				[Net.Ignore, Save.Ignore] public EntRef<Gun.Data> ref_gun;
				[Net.Ignore, Save.Ignore] public EntRef<Gun.State> ref_gun_state;

				/*[Net.Ignore, Save.Ignore] */
				public Vector2 last_occluded_pos;
				/*[Net.Ignore, Save.Ignore] */
				public Vector2 last_visible_pos;
				/*[Net.Ignore, Save.Ignore] */
				public Vector2 last_target_pos;
				/*[Net.Ignore, Save.Ignore] */
				public Vector2 gun_direction;
				/*[Net.Ignore, Save.Ignore] */
				public Vector2 gun_muzzle_position;
				/*[Net.Ignore, Save.Ignore] */
				public NPC.ItemHints item_hints;
				/*[Net.Ignore, Save.Ignore] */
				public float time_burst_end;
				/*[Net.Ignore, Save.Ignore] */
				public float next_attack;
				/*[Net.Ignore, Save.Ignore] */
				public float next_reload;
				/*[Net.Ignore, Save.Ignore] */
				public float next_flip;
				/*[Net.Ignore, Save.Ignore] */
				public float next_cycle;
				/*[Net.Ignore, Save.Ignore] */
				public float los_time;
				/*[Net.Ignore, Save.Ignore] */
				public float last_los;
				/*[Net.Ignore, Save.Ignore] */
				public float aimed_time;
				/*[Net.Ignore, Save.Ignore] */
				public Gunner.Mode mode;
				/*[Net.Ignore, Save.Ignore] */
				private ulong unused_00;
			}

#if SERVER
			//[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region)]
			[ISystem.PreUpdate.Reset(ISystem.Mode.Single, ISystem.Scope.Region)]
			[HasComponent<Player.Data>(Source.Modifier.Shared, false), HasTag("dead", false, Source.Modifier.Owned)]
			public static void Reset([Source.Owned] ref Gunner.State gunner_state)
			{
				gunner_state.item_hints = default;
				gunner_state.mode = Mode.Undefined;
			}

			[HasComponent<Gun.Data>(Source.Modifier.Owned, true)]
			[ISystem.Remove(ISystem.Mode.Single, ISystem.Scope.Region)]
			public static void OnDropGun(ISystem.Info info, Entity entity,
			[Source.Owned] in Transform.Data transform, [Source.Owned] in Holdable.Data holdable,
			[Source.Parent] ref NPC.Data npc, [Source.Parent] in Arm.Data arm, [Source.Parent] ref Gunner.State gunner_state)
			{
				npc.self_hints.RemoveFlag(NPC.SelfHints.Is_Reloading | NPC.SelfHints.Is_Aiming);
				gunner_state.aimed_time = 0.00f;
				gunner_state.los_time = 0.00f;
			}

			[ISystem.VeryLateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
			[HasComponent<Player.Data>(Source.Modifier.Parent, false), HasTag("dead", false, Source.Modifier.Parent)]
			public static void UpdateGun(ISystem.Info info, Entity entity,
			[Source.Owned] in Gun.Data gun, [Source.Owned] in Gun.State gun_state,
			[Source.Owned] in Transform.Data transform, [Source.Owned] in Holdable.Data holdable,
			[Source.Parent] in Arm.Data arm, [Source.Parent] ref Gunner.State gunner_state)
			{
				//gun_state.hints = gun_state.hints;
				//gun_state.stage = gun_state.stage;
				//gun.flags = gun.flags;
				//gunner_state.gun_range = gun.heuristic_range;
				gunner_state.item_hints = holdable.hints;

				gunner_state.ref_gun.Set(entity);
				gunner_state.ref_gun_state.Set(entity);

				gunner_state.gun_direction = transform.GetDirection();
				//gunner_state.gun_muzzle_position = transform.LocalToWorld(gun.muzzle_offset);
				gunner_state.gun_muzzle_position = transform.LocalToWorld(new Vector2(holdable.offset.X, gun.muzzle_offset.Y));

				//ref var material = ref inventory_magazine.resource.material.GetData();
				//if (material.IsNotNull())
				//{
				//	var muzzle_velocity = gun.velocity_multiplier * material.projectile_speed_mult;
				//	gunner_state.muzzle_velocity = muzzle_velocity;

				//	//if (muzzle_velocity < 100.00f || material.flags.HasAny(Material.Flags.Explosive))
				//	//{
				//	//	// TODO: Shitty hack before there's proper trajectory prediction
				//	//	gunner_state.offset_y = 1; // MathF.Abs(100.00f - muzzle_velocity) * 0.10f;
				//	//}
				//}
			}

			[Shitcode]
			[ISystem.EarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
			[HasComponent<Player.Data>(Source.Modifier.Shared, false), HasTag("dead", false, Source.Modifier.Owned)]
			public static void UpdateVehicleGun(ISystem.Info info, Entity entity,
			[Source.Seat] in Vehicle.Data vehicle, [Source.Owned] ref NPC.Data npc,
			[Source.Owned] ref Gunner.Data gunner, [Source.Owned] ref Gunner.State gunner_state, [Source.Owned] ref Control.Data control)
			{
				if (AI.debug_paused) return;

				//App.WriteLine("tank");

				if (vehicle.ent_gun.IsAlive())
				{
					var time = info.WorldTime;
					npc.self_hints.AddFlag(NPC.SelfHints.In_Vehicle);

					gunner_state.ref_gun.Set(vehicle.ent_gun);
					gunner_state.ref_gun_state.Set(vehicle.ent_gun);

					ref var gun = ref gunner_state.ref_gun.GetComponent();
					ref var transform = ref vehicle.ent_gun.GetComponent<Transform.Data>();
					if (transform.IsNotNull() && gun.IsNotNull())
					{
						//gun_state.hints = gun_state.hints | Gun.Hints.Hold_RMB;
						//gun_state.stage = gun_state.stage;
						//gun.flags = gun.flags;
						//gunner_state.gun_range = gun.heuristic_range * 2.00f;
						gunner_state.mode = Mode.Vehicle;

						//App.WriteLine(gunner_state.mode);
						//gunner_state.ent_gun = entity;
						gunner_state.gun_direction = transform.GetDirection();
						//gunner_state.gun_angle_jitter = gun_state.angle_jitter;
						//gunner_state.gun_muzzle_velocity = gun_state.muzzle_velocity;
						gunner_state.gun_muzzle_position = transform.LocalToWorld(gun.muzzle_offset);

						//if (gunner_state.mode == Mode.Vehicle)
						{
							//npc.avoid_distance.X = 8;
							//npc.re.Y = 40;
						}

						//if (gunner_state.mode == Mode.Vehicle)
						{
							var delta = control.mouse.position - transform.position;
							if (time >= gunner_state.next_flip && !Maths.SignEquals(delta.X, transform.scale.X) && Maths.Abs(delta.X) > 8.00f)
							{
								gunner_state.next_flip = time + 3.00f;
								control.keyboard.SetKeyPressed(Keyboard.Key.Spacebar, true);
							}
						}


						control.keyboard.SetKeyPressed(Keyboard.Key.MoveUp, true);
						control.mouse.SetKeyPressed(Mouse.Key.Right, true);
						//control.mouse.SetKeyPressed(Mouse.Key.Left, true);
					}
				}
			}

			[Shitcode]
			[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region)]
			[HasComponent<Player.Data>(Source.Modifier.Shared, false), HasTag("dead", false, Source.Modifier.Owned)]
			public static void Update(ISystem.Info info, Entity entity, ref XorRandom random, ref Region.Data region,
			[Source.Owned] in Gunner.Data gunner, [Source.Owned] ref Gunner.State gunner_state,
			[Source.Owned, Override] ref AI.Behavior behavior_override,
			[Source.Owned, Override] ref AI.Movement movement_override,
			//[Source.Owned, Override] ref AI.Data ai_override,
			[Source.Owned] ref NPC.Data npc,
			//[Source.Owned] ref Pathfinding.Data pathfinding, [Source.Owned] ref Pathfinding.State pathfinding_state,
			[Source.Owned] ref Control.Data control, [Source.Owned] in Transform.Data transform, [Source.Owned, Optional] in Faction.Data faction)
			{
				const bool draw_debug_any = false;
				const bool draw_debug = draw_debug_any && true;
				const bool draw_debug2 = draw_debug_any && true;
				const bool draw_debug3 = draw_debug_any && true;

				//App.WriteLine(gunner_state.mode);

				if (gunner_state.mode != Mode.Vehicle && npc.ent_pickup_target != gunner_state.ref_gun.entity)
				{
					gunner_state.ref_gun.Reset();
				}

				if (AI.debug_paused) return;

				npc.self_hints.RemoveFlag(NPC.SelfHints.Is_Aiming);

				if (gunner_state.ref_gun.entity.IsAlive())
				{
					ref var gun = ref gunner_state.ref_gun.GetComponent();
					ref var gun_state = ref gunner_state.ref_gun_state.GetComponent();

					if (gun.IsNotNull() && gun_state.IsNotNull())
					{
						var time = info.WorldTime;

						var is_artillery = gun_state.hints.HasAny(Gun.Hints.Artillery);
						var is_reloading = gun_state.stage == Gun.Stage.Reloading || gun_state.hints.HasAny(Gun.Hints.Wants_Reload);
						var is_loaded = !is_reloading && gun_state.hints.HasAny(Gun.Hints.Loaded);

						if (draw_debug) region.DrawDebugCircle(gunner_state.last_visible_pos, 0.50f, Color32BGRA.White, 1.00f, filled: true);
						if (draw_debug) region.DrawDebugCircle(gunner_state.last_occluded_pos, 0.250f, Color32BGRA.Red, 1.00f, filled: true);
						if (draw_debug) region.DrawDebugCircle(gunner_state.last_target_pos, 0.250f, Color32BGRA.Yellow, 1.00f, filled: true);

						// Reload if gun is jammed, not loaded or target is out of LOS
						if ((!is_loaded && !is_reloading) || gun_state.stage == Gun.Stage.Jammed || (!is_artillery && (time - gunner_state.last_los) >= 1.00f))
						{
							if (time < gunner_state.time_burst_end)
							{
								gunner_state.time_burst_end = time;
								gunner_state.next_attack = gunner_state.time_burst_end + random.NextFloatExtra(gunner.attack_cooldown, 0.20f);
							}

							if (time > gunner_state.next_reload && time > gunner_state.time_burst_end)
							{
								if (gun_state.stage == Gun.Stage.Reloading || gun_state.stage == Gun.Stage.Jammed)
								{
									control.keyboard.SetKeyPressed(Keyboard.Key.Reload, true);
									gunner_state.next_reload = time + random.NextFloatExtra(gunner.reload_cooldown, gunner.reload_cooldown * 0.25f);
									gunner_state.aimed_time = 0.00f;
								}
								else if (Maths.Normalize(gun_state.resource_ammo.quantity, gun.max_ammo, 1.00f) <= 0.40f)
								{
									control.keyboard.SetKeyPressed(Keyboard.Key.Reload, true);
									gunner_state.next_reload = time + random.NextFloatRange(0.20f, 0.30f);
									gunner_state.aimed_time = 0.00f;
								}
							}
						}

						//control.mouse.position = default;

						// Cycle gun if it's not reloading and needs to be cycled
						if (time >= gunner_state.next_cycle && gun_state.stage != Gun.Stage.Reloading && gun.flags.HasAny(Gun.Flags.Manual_Cycle) && gun_state.hints.HasNone(Gun.Hints.Cycled))
						{
							control.mouse.SetKeyPressed(Mouse.Key.Left, true);
							gunner_state.next_cycle += time + 0.25f;
						}



						//if (gun_state.stage == Gun.Stage.Reloading || gun_state.stage == Gun.Stage.Jammed)
						//{
						//	control.keyboard.SetKeyPressed(Keyboard.Key.MoveDown, true);

						//	npc_override.avoid_distance = new Vector2(gunner.attack_distance * 3.00f, 1);
						//	npc_override.idle_distance = new Vector2(gunner.attack_distance * 4.50f, 8);
						//}

						//if ((time - behavior_override.last_damage) < 1.00f)
						//{
						//	control.keyboard.SetKeyPressed(Keyboard.Key.MoveDown, true);
						//}

						if ((behavior_override.type == AI.Behavior.Type.Attack || behavior_override.type == Behavior.Type.Defend) && behavior_override.ref_target_body.TryGetHandle(out var h_target_body))
						{
							//App.WriteLine("ye");

							ref var target_body = ref h_target_body.data;
							if (target_body.IsNotNull() && faction.id.IsAttackable(target_body.GetFaction()))
							{
								var ent_target = target_body.GetEntity();
								var target_pos = target_body.GetPosition();
								var aim_pos = target_pos; // + (target_body.GetVelocity() * App.fixed_update_interval_s_f32);
														  //target_pos -= new Vector2(0.00f, gunner_state.offset_y);




								//if (draw_debug3)
								//{
								//	//App.WriteLine(aim_pos);

								//	//region.DrawDebugLine(transform.position, aim_pos, color: Color32BGRA.Cyan, 8);
								//	WorldNotification.Push(ref region, message: $"{aim_pos}", position: transform.position, color:Color32BGRA.Magenta, lifetime: 0.50f, force: Vec2f.Zero, velocity: Vec2f.Zero);
								//}


								//App.WriteLine("shit");

								//if (gunner_state.offset_y != 0.00f)
								//{
								//	target_pos = 
								//}

								var delta = (target_pos - gunner_state.gun_muzzle_position);
								var dir = delta.GetNormalized(out var distance);

								//if (gunner_state.mode == Mode.Vehicle)
								//{
								//	if (time >= gunner_state.next_flip && !Maths.SignEquals(delta.X, transform.scale.X) && Maths.Abs(delta.X) > 16.00f)
								//	{
								//		gunner_state.next_flip = time + 3.00f;
								//		control.keyboard.SetKeyPressed(Keyboard.Key.Spacebar, true);
								//	}
								//}


								//if (time >= gunner_state.next_attack && time >= gunner_state.time_burst_end)
								{
									if (draw_debug) region.DrawDebugLine(gunner_state.gun_muzzle_position, target_pos, Color32BGRA.Green, thickness: 10.00f * Maths.InvLerp01(0.00f, 3.00f, Maths.Max(0.00f, gunner_state.time_burst_end - time)));
								}

								//if (is_artillery)
								//{
								//	if (Gun.TryCalculateTrajectory(transform.position, target_pos, gunner_state.muzzle_velocity, region.GetGravity().Y, out var angle_lo, out var angle_hi))
								//	{
								//		App.WriteLine($"{angle_lo} {angle_hi}");
								//		dir = dir.RotateByRad(-(angle_hi ?? angle_lo ?? 0.00f));
								//		aim_pos = transform.position + (dir * distance * 0.50f);
								//	}
								//	else
								//	{
								//		//App.WriteLine("rip");
								//	}
								//}
								//App.WriteLine($"{is_artillery}");


								var time_since_last_damage = (time - behavior_override.t_last_damage);
								var attack_distance_base = Maths.Max(gunner.attack_distance, gun.heuristic_range * 1.50f);
								var attack_distance = attack_distance_base;
								var max_h = 0.750f;
								var aim_decay_lerp = gunner.aim_decay_lerp;
								var min_aim_time = 0.02f;

								var can_cover = behavior_override.combat_flags.HasAny(Behavior.CombatFlags.Cover);
								var can_jumpo = behavior_override.combat_flags.HasAny(Behavior.CombatFlags.Evade);

								if (gun_state.hints.HasAny(Gun.Hints.Supressive_Fire))
								{
									max_h += 2.00f;
									aim_decay_lerp *= 0.20f;
									attack_distance *= 1.25f;
									can_cover = true;

									//delta = (target_pos - gunner_state.gun_muzzle_position);
									//dir = delta.GetNormalized(out distance);
								}

								if (gun_state.hints.HasAny(Gun.Hints.Long_Range)) // && gunner_state.item_hints.HasAnyExcept(NPC.ItemHints.Heavy | NPC.ItemHints.Clumsy | NPC.ItemHints.Long | NPC.ItemHints.Explosive | NPC.ItemHints.Dangerous, NPC.ItemHints.Inaccurate | NPC.ItemHints.Weak | NPC.ItemHints.Small))
								{
									can_cover = true;
									can_jumpo = false;
									attack_distance *= 1.65f;
								}
								else if (gun_state.hints.HasAny(Gun.Hints.Close_Range)) // || gunner_state.item_hints.HasAnyExcept(NPC.ItemHints.Inaccurate | NPC.ItemHints.Fast | NPC.ItemHints.Crappy, NPC.ItemHints.Clumsy | NPC.ItemHints.Long))
								{
									max_h += 1.00f;
									aim_decay_lerp *= 0.70f;
									attack_distance *= 0.75f;
									min_aim_time *= 0.75f;
									can_cover = false;
									//can_jumpo &= true;


									//delta = (target_pos - gunner_state.gun_muzzle_position);
									//dir = delta.GetNormalized(out distance);
								}

								//if (region.GetCurrentTick() % 50 == 0)
								//{
								//	WorldNotification.Push(ref region, $"{gun_state.hints}\n{gun.flags}\n{gun_state.stage}\n{distance}\n{npc_override.item_hints}", Color32BGRA.Yellow, transform.position);
								//}

								if (time_since_last_damage < 5.00f)
								{
									attack_distance += 16.00f;
									//attack_distance *= 1.25f;
								}

								if ((time_since_last_damage < 2.00f) && (can_cover || !is_loaded) && npc.self_hints.HasNone(NPC.SelfHints.Is_Fleeing | NPC.SelfHints.Is_Swimming | NPC.SelfHints.Is_Climbing))
								{
									control.keyboard.SetKeyPressed(Keyboard.Key.MoveDown, true);
								}

								if (draw_debug) region.DrawDebugCircle(gunner_state.gun_muzzle_position, attack_distance, Color32BGRA.Red.WithAlphaMult(0.10f), thickness: 4.00f);

								var is_los = false;
								//var is_range = distance <= Maths.Max(gunner.engage_distance, gunner_state.gun_range);
								var is_range = distance < attack_distance;
								var is_ready = false;
								var can_attack = time > gunner_state.next_attack && is_loaded; // && gun_state.hints.HasAll(Gun.Hints.Loaded); // && gun_state.stage != Gun.Stage.Reloading && gun_state.stage != Gun.Stage.Jammed;
								var is_attacking = time <= gunner_state.time_burst_end && !is_reloading; // && gun_state.hints.HasAll(Gun.Hints.Loaded);
								var is_cooldown = time > gunner_state.time_burst_end && time <= gunner_state.next_attack;

								//if (draw_debug3)
								//{
								//	region.DrawDebugText(target_pos, $"{is_ready}", Color32BGRA.Magenta.WithAlphaMult(0.50f));
								//}

								//if (draw_debug3)
								//{
								//	//App.WriteLine(aim_pos);

								//	region.DrawDebugLine(transform.position, aim_pos, color: Color32BGRA.Cyan, 8);
								//	//if (is_attacking || can_attack) WorldNotification.Push(ref region, message: $"{can_attack}", position: transform.position, color: can_attack ? Color32BGRA.Green : Color32BGRA.Magenta, lifetime: 0.50f, force: Vec2f.Zero, velocity: Vec2f.Zero);
								//}



								if (!is_artillery && (time - behavior_override.t_last_damage) < 1.00f)
								{
									is_range = true;
								}

								//region.DrawDebugLine(gunner_state.gun_muzzle_position, target_pos, is_range ? Color32BGRA.Green : Color32BGRA.Yellow, 4);
								is_los = is_range && (distance <= 2.00f || region.IsInLineOfSight(pos_a: gunner_state.gun_muzzle_position,
																		  radius: 0.25f,
																		  pos_b: target_pos,
																		  group: ent_target,
																		  layer: Physics.Layer.Solid,
																		  mask: Physics.Layer.World,
																		  exclude: Physics.Layer.Ignore_Bullet | Physics.Layer.Creature | Physics.Layer.Background | Physics.Layer.Item | Physics.Layer.Organic,
																		  query_flags: Physics.QueryFlag.Static | Physics.QueryFlag.Dynamic));


								if (draw_debug) region.DrawDebugCircle(target_pos, 1, color: Color32BGRA.Orange, filled: true);

								//var attack_distance = Maths.Max(gunner.attack_distance, gunner_state.gun_range * 1.50f);

								if (is_artillery && is_range)
								{
									// TODO: if the angle is too higher than the gun's aimable angle limit, make the NPC move farther to the side to lower the trajectory angle
									// TODO: dumb
									if (Gun.TryCalculateTrajectory(
									pos_muzzle: gunner_state.gun_muzzle_position,
									pos_target: target_pos,
									speed: gun_state.muzzle_velocity,
									gravity: region.GetGravity().Y,
									angle_shallow: out var angle_lo,
									angle_steep: out var angle_hi))
									{
										//App.WriteLine($"{angle_lo} {angle_hi}");

										var dir_new = dir;
										
										// low-precision grid-based LOS check so it doesn't use steep angle if the terrain/obstacle is thin enough that it can be just destroyed in few shots
										var is_los_coarse = region.IsLOS(a: movement_override.position_head, b: target_pos, 
											threshold_solid: 0.80f, allow_inside: true);

										//App.WriteLine(dir);
										if (is_los) // | is_los_coarse)
										{
											var ang = (angle_lo ?? angle_hi).GetValueOrDefault();
											dir_new = ang.RadToDir();
										}
										else if (angle_lo.HasValue && is_los_coarse)
										{
											var ang = angle_lo.GetValueOrDefault(); // ?? Maths.Lerp(angle_hi.GetValueOrDefault(), angle_hi ?? angle_lo.GetValueOrDefault(), 0.90f);
											dir_new = ang.RadToDir();
										}
										else
										{
											//var ang = angle_hi ?? Maths.Lerp(angle_lo.GetValueOrDefault(), angle_hi ?? angle_lo.GetValueOrDefault(), 0.75f);
											//var ang = angle_hi ?? Maths.Lerp(angle_lo.GetValueOrDefault(), angle_hi ?? angle_lo.GetValueOrDefault(), 0.90f);
											var ang = Maths.Lerp(angle_lo.GetValueOrDefault(), angle_hi ?? angle_lo.GetValueOrDefault(), 0.99f);
											dir_new = ang.RadToDir();
										}


										//var parity = delta.GetParity();

										if (float.IsNegative(delta.X))
										{
											dir_new.X *= -1;
											//dir_new.Y *= -1;								
										}
										else
										{
											//dir_new.X *= 1;
											dir_new.Y *= -1;
											//if (float.IsNegative(delta.Y)) dir_new.Y *= -parity;
										}

										//if (float.IsNegative(delta.Y))
										//{
										//	dir_new.Y *= -1;
										//}

										if (draw_debug3)
										{
											region.DrawDebugDir(gunner_state.gun_muzzle_position, dir: dir_new * 8, color: Color32BGRA.Cyan, 2);
											region.DrawDebugArc(gunner_state.gun_muzzle_position, angle_a: transform.LocalToWorldRotation(angle_lo.GetValueOrDefault()), angle_b: transform.LocalToWorldRotation(angle_hi.GetValueOrDefault()), radius: 4, color: Color32BGRA.Magenta, thickness: 4);
										}


										dir = dir_new.GetNormalized();
										//aim_pos = gunner_state.gun_muzzle_position + (dir * Maths.Clamp(distance * 0.25f, 4, 16));
										//aim_pos = transform.position + (dir * Maths.Clamp(distance * 0.25f, 4, 16));
										aim_pos = gunner_state.gun_muzzle_position + (dir * distance); // Maths.Clamp(distance * 0.25f, 4, 16));
										var aim_pos_clamped = gunner_state.gun_muzzle_position + (dir * Maths.Clamp(distance * 0.25f, 4, 16));

										is_range = true;
										is_los = /*is_los_coarse || */region.IsInLineOfSight(
											pos_a: gunner_state.gun_muzzle_position,
											pos_b: aim_pos_clamped,
											group: ent_target,
											radius: 0.10f,
											layer: Physics.Layer.Solid,
											mask: Physics.Layer.World,
											exclude: Physics.Layer.Ignore_Bullet,
											query_flags: Physics.QueryFlag.Static,
											skip_inside: false);
									}
									else
									{
										//App.WriteLine("rip");
									}
								}

								if (gun_state.hints.HasAny(Gun.Hints.Hold_RMB))
								{
									control.mouse.SetKeyPressed(Mouse.Key.Right, true);
								}

								// Aim at last position if out of LOS (e.g. when jumping)
								if (!is_los)
								{
									if (gunner_state.los_time < -0.70f) gunner_state.last_occluded_pos = gunner_state.gun_muzzle_position;
									gunner_state.los_time = Maths.Clamp(gunner_state.los_time - info.DeltaTime, -1.00f, 0.00f); // Maths.Lerp(gunner_state.los_time, 0.00f, 0.50f);
								}
								else
								{
									if (gunner_state.los_time > 0.50f) gunner_state.last_visible_pos = gunner_state.gun_muzzle_position;
									gunner_state.last_los = time;
									//gunner_state.los_time += info.DeltaTime;
									gunner_state.los_time = Maths.Clamp(gunner_state.los_time + info.DeltaTime, 0.00f, 1.00f);
									gunner_state.last_target_pos = target_pos;

								}

								if (!is_artillery && !is_los && is_attacking && gun_state.hints.HasAny(Gun.Hints.Supressive_Fire))
								{
									target_pos = gunner_state.last_target_pos;
									aim_pos = gunner_state.last_target_pos;
									delta = (target_pos - gunner_state.gun_muzzle_position);
									dir = delta.GetNormalized(out distance);
									is_los = true;
								}

								//(time - gunner_state.last_los) <= 0.50f

								//is_ready = gunner_state.los_time >= 0.05f;

								if (is_artillery)
								{
									is_ready = is_range;
								}
								else
								{
									//App.WriteLine((time - gunner_state.last_los));
									is_ready = (time - gunner_state.last_los) <= 10.00f;
								}

								if (is_ready)
								{
									//App.WriteLine("yes");

									//control.mouse.position = Vector2.Lerp(control.mouse.position, target_pos, 0.08f);
									//control.mouse.position = Vector2.Lerp(control.mouse.position, target_pos, Maths.Clamp(gunner_state.los_time * 0.30f, 0.005f, 0.20f));
									//movement_override.flags.AddFlag(AI.Movement.Flags.No_Mouse);

									//var min_dot = 0.995f; // gunner.aim_min_dot;
									//var min_aim_time = 0.04f;


									if (is_artillery)
									{
										//min_dot = 0.98f;
										min_aim_time = 0.20f;

										control.mouse.position = Vector2.Lerp(control.mouse.position, aim_pos, 0.25f); // Maths.Clamp(gunner_state.los_time * 1.00f, 0.005f, 0.50f) );
									}
									else
									{
										//control.mouse.position = Vector2.Lerp(control.mouse.position, aim_pos, Maths.Clamp(gunner_state.aimed_time * 0.30f, 0.005f, 0.20f) * gunner.aim_speed_mult);

										//if (!is_los && gun_state.hints.HasAny(Gun.Hints.Supressive_Fire))
										//{
										//	aim_pos = gunner_state.last_target_pos;
										//}

										var aim_time_mult = Maths.Clamp(Maths.InvLerp(2.00f, 0.00f, gunner_state.aimed_time), 0.20f, 1.00f);
										control.mouse.position = Vector2.Lerp(control.mouse.position, aim_pos + (new Vector2((0.50f - Maths.Perlin(time * 2, 0.00f, 1.00f, entity.GetShortID())) * distance * 0.30f, (0.20f - Maths.Perlin(0.00f, time * 3, 1.00f, entity.GetShortID()) * 0.50f) * distance * 0.10f) * aim_time_mult), 0.30f);

										//if (is_los)
										//{
										//	control.mouse.position = Vector2.Lerp(control.mouse.position, aim_pos, 0.30f);
										//}
									}

									min_aim_time *= gunner.aim_time_mult;

									var dot = Vector2.Dot(gunner_state.gun_direction, dir);
									var theta = Maths.ScalarProjection(dir, gunner_state.gun_direction); 
									var theta_v = Maths.VectorProjection(dir, gunner_state.gun_direction); 
									var h2 = (Maths.Sqrt((1.00f + theta) * (1.00f - theta)) / theta) * dot * distance;

									if (is_artillery)
									{
										//max_h = Maths.Min(max_h, 0.50f);
										max_h = Maths.Min(max_h, 1.50f);
										//max_h = Maths.Min(max_h, 4.50f);
									}
									else
									{
										max_h *= (1.00f + (Maths.InvLerp01(2.50f, 0.00f, (gunner_state.aimed_time * 1.50f) - 0.10f) * 2.50f)) * 0.25f;
									}

									var is_aiming = is_los && h2 <= max_h;

									//var is_aimed = (is_aiming && gunner_state.aimed_time >= min_aim_time && Maths.GetDirectionSimilarity(dir, gunner_state.gun_direction) >= 0.99f) || (!is_artillery && npc.item_hints.HasNone(NPC.ItemHints.Explosive | NPC.ItemHints.Dangerous) && npc.self_hints.HasAny(NPC.SelfHints.Is_Doomed | NPC.SelfHints.Is_Injured | NPC.SelfHints.Is_Panicking));
									//var is_aimed = (is_aiming && gunner_state.aimed_time >= min_aim_time) || (!is_artillery && npc.item_hints.HasNone(NPC.ItemHints.Explosive | NPC.ItemHints.Dangerous) && npc.self_hints.HasAny(NPC.SelfHints.Is_Doomed | NPC.SelfHints.Is_Injured | NPC.SelfHints.Is_Panicking));
									//var is_aimed = (is_aiming && gunner_state.aimed_time >= min_aim_time); // || (!is_artillery && npc.item_hints.HasNone(NPC.ItemHints.Explosive | NPC.ItemHints.Dangerous) && npc.self_hints.HasAny(NPC.SelfHints.Is_Doomed | NPC.SelfHints.Is_Injured | NPC.SelfHints.Is_Panicking));

									//App.WriteLine(h2);

									//if (draw_debug3) region.DrawDebugLine(gunner_state.gun_muzzle_position, target_pos, color: is_aimed ? Color32BGRA.Green : Color32BGRA.Yellow, thickness: 10.00f);

									//var max_cone_angle = gun_state.angle_jitter * Maths.deg2rad;

									if (draw_debug3) region.DrawDebugDir(gunner_state.gun_muzzle_position, dir * gun.heuristic_range, Color32BGRA.White.WithAlphaMult(0.50f), 8.00f);
									if (draw_debug3) region.DrawDebugDir(gunner_state.gun_muzzle_position, gunner_state.gun_direction * distance, Color32BGRA.Green.WithAlphaMult(0.90f), 2.00f);
									if (draw_debug3) region.DrawDebugDir(gunner_state.gun_muzzle_position, dir * distance, Color32BGRA.White.WithAlphaMult(0.50f), 2.00f);
									if (draw_debug3) region.DrawDebugDir(gunner_state.gun_muzzle_position, dir * theta * distance, Color32BGRA.Cyan.WithAlphaMult(0.50f), 2.00f);
									if (draw_debug) region.DrawDebugDir(gunner_state.gun_muzzle_position, theta_v * distance, (is_aiming ? Color32BGRA.Green : Color32BGRA.Red).WithAlphaMult(0.50f), 2.00f);

									//region.DrawDebugDir(gunner_state.gun_muzzle_position, dir * theta * distance, Color32BGRA.Cyan.WithAlphaMult(0.50f), 2.00f);

									//MathF.Tan(theta_rad) * dot * len_b)

									//region.DrawDebugDir(gunner_state.gun_muzzle_position, dir.RotateByRad(max_cone_angle) * distance, Color32BGRA.Red.WithAlphaMult(0.50f), 1.00f);
									//region.DrawDebugDir(gunner_state.gun_muzzle_position, dir.RotateByRad(-max_cone_angle) * distance, Color32BGRA.Red.WithAlphaMult(0.50f), 1.00f);

									//region.DrawDebugCircle(gunner_state.gun_muzzle_position + (dir * distance), MathF.Tan(max_cone_angle) * distance, Color32BGRA.Red.WithAlphaMult(0.50f), 1.00f);
									if (draw_debug) region.DrawDebugCircle(gunner_state.gun_muzzle_position + (dir * distance), max_h, Color32BGRA.Yellow.WithAlphaMult(0.50f), 1.00f);
									if (draw_debug) region.DrawDebugCircle(gunner_state.gun_muzzle_position + (dir * distance), h2, (is_aiming ? Color32BGRA.Green : Color32BGRA.Red).WithAlphaMult(0.50f), 1.00f);


									// Make sure the gun is aiming at the target
									//if (dot >= min_dot)

									if (is_aiming)
									{
										var dir_similarity = Maths.GetDirectionSimilarity(dir, gunner_state.gun_direction).Clamp01();
										//gunner_state.aimed_time = Maths.Clamp(gunner_state.aimed_time + App.fixed_update_interval_s * (0.50f + (dir_similarity * 0.50f)), 0.00f, min_aim_time + 1.00f);
										gunner_state.aimed_time = Maths.Clamp(gunner_state.aimed_time + (App.fixed_update_interval_s * (0.50f + (dir_similarity * 0.50f))), 0.00f, min_aim_time + 1.00f);
										npc.self_hints.AddFlag(NPC.SelfHints.Is_Aiming);
									}
									else
									{
										gunner_state.aimed_time = Maths.MoveTowards(gunner_state.aimed_time, 0.00f, aim_decay_lerp * 0.10f);
									}

									var is_aimed = (is_aiming && gunner_state.aimed_time >= min_aim_time); // || (!is_artillery && npc.item_hints.HasNone(NPC.ItemHints.Explosive | NPC.ItemHints.Dangerous) && npc.self_hints.HasAny(NPC.SelfHints.Is_Doomed | NPC.SelfHints.Is_Injured | NPC.SelfHints.Is_Panicking));

									//region.DrawDebugCircle(gunner_state.gun_muzzle_position, attack_distance, Color32BGRA.Orange.WithAlphaMult(0.50f), 4);

									//App.WriteLine($"{region.GetCurrentTick()} {dot} >= {min_dot}; {gunner_state.aimed_time}");

									//App.WriteLine(gunner_state.aimed_time);

									//App.WriteLine($"{region.GetFixedTime()} {is_artillery}");

									//if (is_los && (is_artillery || ))
									//if (is_los && gunner_state.item_hints.HasAny(NPC.ItemHints.Clumsy | NPC.ItemHints.Explosive | NPC.ItemHints.Slow) && gun_state.hints.HasAnyExcept(Gun.Hints.Supressive_Fire | Gun.Hints.Long_Range | Gun.Hints.Artillery, Gun.Hints.No_Ammo | Gun.Hints.Wants_Reload))
									if (is_los && (is_artillery && is_range)) // && gunner_state.item_hints.HasAny(NPC.ItemHints.Clumsy | NPC.ItemHints.Explosive | NPC.ItemHints.Slow) && gun_state.hints.HasAnyExcept(Gun.Hints.Supressive_Fire | Gun.Hints.Long_Range | Gun.Hints.Artillery, Gun.Hints.No_Ammo | Gun.Hints.Wants_Reload))
									{
										movement_override.flags.AddFlag(Movement.Flags.No_Move);
									}

									if (!is_cooldown && is_loaded && gun_state.stage != Gun.Stage.Reloading && gun_state.stage != Gun.Stage.Jammed)
									{
										if (gunner_state.mode == Mode.Vehicle)
										{
											npc.avoid_distance = new Vector2(attack_distance * 0.80f, 0);
											npc.idle_distance = new Vector2(attack_distance * 0.40f, 10);
										}
										else if (is_artillery)
										{
											if (gunner_state.los_time >= 0.30f)
											{
												npc.avoid_distance = new Vector2(attack_distance * 0.20f, 0);
												npc.idle_distance = new Vector2(attack_distance * 10.00f, 10);
												movement_override.flags.AddFlag(AI.Movement.Flags.No_Move);
											}
											else
											{
												npc.avoid_distance = new Vector2(attack_distance * 0.50f, 0);
												npc.idle_distance = new Vector2(attack_distance * 0.80f, 10);
												movement_override.flags.RemoveFlag(AI.Movement.Flags.No_Move);
											}
											//movement_override.position_target = target_pos_old;
											//movement_override.position_path = target_pos_old;
										}
										else if (is_attacking && !((can_cover || behavior_override.combat_flags.HasAny(Behavior.CombatFlags.Cover | Behavior.CombatFlags.Evade)) && (time_since_last_damage < 4.00f))) // && ((time - behavior_override.last_damage) >= 0.20f))) // Move closer if currently shooting and if wasn't damaged recently
										{
											//region.DrawDebugCircle(gunner_state.gun_muzzle_position, gunner_state.gun_range, Color32BGRA.Green.WithAlphaMult(0.50f));
											//region.DrawDebugCircle(gunner_state.gun_muzzle_position, attack_distance_base, Color32BGRA.Orange.WithAlphaMult(1.00f));
											//region.DrawDebugLine(gunner_state.gun_muzzle_position, target_pos, Color32BGRA.Green);

											if (gun_state.hints.HasAny(Gun.Hints.Close_Range))
											{
												npc.avoid_distance = new Vector2(gun.heuristic_range * 0.01f, 0);
												npc.idle_distance = new Vector2(gun.heuristic_range * 0.05f, 2);
											}
											else if (gun_state.hints.HasAny(Gun.Hints.Long_Range))
											{
												npc.avoid_distance = new Vector2(gun.heuristic_range * 0.50f, 2);
												npc.idle_distance = new Vector2(gun.heuristic_range * 0.95f, 15);
											}
											else if (gun_state.hints.HasAny(Gun.Hints.Supressive_Fire) && gunner_state.los_time > -0.50f)
											{
												npc.avoid_distance = new Vector2(gun.heuristic_range * 0.20f, 0);
												npc.idle_distance = new Vector2(attack_distance, 10);
												if (npc.self_hints.HasNone(NPC.SelfHints.Is_Fleeing | NPC.SelfHints.Is_Swimming | NPC.SelfHints.Is_Climbing)) control.keyboard.SetKeyPressed(Keyboard.Key.MoveDown, true);
											}
											else
											{
												npc.avoid_distance = new Vector2(gun.heuristic_range * 0.20f, 0);
												npc.idle_distance = new Vector2(is_los ? gun.heuristic_range * 0.50f : 0.00f, 2);
											}

											if (draw_debug2) region.DrawDebugCircle(gunner_state.gun_muzzle_position, npc.avoid_distance.X, Color32BGRA.Green.WithAlphaMult(0.10f), 1, filled: true);
											if (draw_debug2) region.DrawDebugCircle(gunner_state.gun_muzzle_position, npc.idle_distance.X, Color32BGRA.Green.WithAlphaMult(0.50f), 1, filled: false);

											if ((can_jumpo && gun_state.hints.HasAll(Gun.Hints.Close_Range | Gun.Hints.Cycled | Gun.Hints.Loaded)) && !control.keyboard.GetKey(Keyboard.Key.MoveDown) && random.NextBool(0.20f))
											{
												control.keyboard.SetKeyPressed(Keyboard.Key.MoveUp, true);
											}

											//if (!is_los)
											//{
											//	control.keyboard.SetKeyPressed(Keyboard.Key.MoveUp, true);
											//}
										}
										else if (can_attack)
										{
											//npc_override.avoid_distance = new Vector2(0, 0);
											//npc_override.idle_distance = new Vector2(is_los ? 0.00f : attack_distance * 1.00f, 1);
											//movement_override.position_target = gunner_state.last_visible_pos;

											//if (behavior_override.combat_flags.HasAny(Behavior.CombatFlags.Charge))
											{
												npc.avoid_distance = new Vector2(is_los ? gun.heuristic_range * 0.20f : 0.00f, 0);
												npc.idle_distance = new Vector2(is_los ? gun.heuristic_range * 0.50f : 0.00f, 2);
											}

											if (draw_debug2) region.DrawDebugCircle(gunner_state.gun_muzzle_position, npc.avoid_distance.X, Color32BGRA.Orange.WithAlphaMult(0.10f), 1, filled: true);
											if (draw_debug2) region.DrawDebugCircle(gunner_state.gun_muzzle_position, npc.idle_distance.X, Color32BGRA.Orange.WithAlphaMult(0.50f), 1, filled: false);

										}
										else // Back off a bit otherwise
										{
											if (true) //gunner_state.los_time > 0.00f)
											{
												//region.DrawDebugCircle(gunner_state.gun_muzzle_position, attack_distance, Color32BGRA.Yellow.WithAlphaMult(0.50f));
												//region.DrawDebugCircle(gunner_state.gun_muzzle_position, attack_distance_base, Color32BGRA.Orange.WithAlphaMult(1.00f));
												//region.DrawDebugLine(gunner_state.gun_muzzle_position, target_pos, Color32BGRA.Yellow);

												// Move back and forth

												if (true) //distance < 6.00f)
												{
													if (gun_state.hints.HasAny(Gun.Hints.Close_Range))
													{
														npc.avoid_distance = new Vector2(gun.heuristic_range * 0.25f, 1);
														npc.idle_distance = new Vector2(gun.heuristic_range * 0.65f, 2);
													}
													else if (gun_state.hints.HasAny(Gun.Hints.Long_Range))
													{
														npc.avoid_distance = new Vector2(is_los ? gun.heuristic_range * 0.50f : 0.00f, 2);
														npc.idle_distance = new Vector2(is_los ? attack_distance * 1.00f : 0.00f, 0);
													}
													else
													{
														npc.avoid_distance = new Vector2(attack_distance * 0.90f, 0);
														npc.idle_distance = new Vector2(is_los ? attack_distance * 1.00f : 0.00f, 0);
													}
												}
												else
												{
													//movement_override.position_target = gunner_state.last_occluded_pos;
												}

												if (draw_debug2) region.DrawDebugCircle(gunner_state.gun_muzzle_position, npc.avoid_distance.X, Color32BGRA.Cyan.WithAlphaMult(0.10f), 1, filled: true);
												if (draw_debug2) region.DrawDebugCircle(gunner_state.gun_muzzle_position, npc.idle_distance.X, Color32BGRA.Cyan.WithAlphaMult(0.50f), 1, filled: false);

												// Sometimes do a tactical jumpo
												if (!control.keyboard.GetKey(Keyboard.Key.MoveDown) && ((can_jumpo || random.NextBool(0.01f)) || gun_state.hints.HasAny(Gun.Hints.Close_Range)))
												{
													control.keyboard.SetKeyPressed(Keyboard.Key.MoveUp, true);
												}
											}
											else
											{
												//npc_override.avoid_distance = new Vector2(gunner_state.gun_range * 0.10f, 0);
												//npc_override.idle_distance = new Vector2(gunner_state.gun_range * 40.70f, 20);

												//npc_override.avoid_distance = new Vector2(attack_distance * 1.10f, 0);
												//npc_override.idle_distance = new Vector2(attack_distance * 1.30f, 16);

												//movement_override.position_target = gunner_state.last_occluded_pos;

												if (draw_debug2) region.DrawDebugCircle(gunner_state.gun_muzzle_position, npc.avoid_distance.X, Color32BGRA.Yellow.WithAlphaMult(0.10f), 1, filled: true);
												if (draw_debug2) region.DrawDebugCircle(gunner_state.gun_muzzle_position, npc.idle_distance.X, Color32BGRA.Yellow.WithAlphaMult(0.50f), 1, filled: false);

												//region.DrawDebugCircle(gunner_state.gun_muzzle_position, attack_distance, Color32BGRA.Yellow.WithAlphaMult(0.25f));
												//region.DrawDebugCircle(gunner_state.gun_muzzle_position, attack_distance_base, Color32BGRA.Orange.WithAlphaMult(1.00f));
												if (draw_debug) region.DrawDebugLine(gunner_state.gun_muzzle_position, target_pos, Color32BGRA.Yellow.WithAlphaMult(0.50f));
											}
										}
									}
									else // Retreat is gun is jammed or reloading
									{
										//movement_override.position_path = gunner_state.last_target_pos;

										if (true)
										{
											if (gunner_state.mode == Mode.Vehicle)
											{
												npc.avoid_distance = new Vector2(attack_distance * 1.70f, 0);
												npc.idle_distance = new Vector2(attack_distance * 2.00f, 10);
											}
											else if (is_artillery)
											{
												npc.avoid_distance = new Vector2(attack_distance * 0.80f, 0);
												npc.idle_distance = new Vector2(attack_distance * 1.20f, 10);

												//gunner_state.aimed_time = 0.00f;
												//movement_override.flags.SetFlag(AI.Movement.Flags.No_Move, true);
											}
											else
											{
												if (is_los || (Vector2.DistanceSquared(transform.position, gunner_state.last_visible_pos) < 12.00f)) // || gun_state.hints.HasAny(Gun.Hints.Close_Range))
												{
													npc.avoid_distance = new Vector2(attack_distance * 1.50f, 0);
													npc.idle_distance = new Vector2(attack_distance * 2.00f, 2);
												}
												else
												{
													npc.avoid_distance = new Vector2(attack_distance * 0.00f, 0);
													npc.idle_distance = new Vector2(attack_distance * 2.50f, 10);
													if (npc.self_hints.HasNone(NPC.SelfHints.Is_Fleeing | NPC.SelfHints.Is_Swimming | NPC.SelfHints.Is_Climbing)) control.keyboard.SetKeyPressed(Keyboard.Key.MoveDown, true);
												}
											}

											gunner_state.aimed_time = 0.00f;

											if (draw_debug2) region.DrawDebugCircle(gunner_state.gun_muzzle_position, npc.avoid_distance.X, Color32BGRA.White.WithAlphaMult(0.10f), filled: true);
											if (draw_debug2) region.DrawDebugCircle(gunner_state.gun_muzzle_position, npc.idle_distance.X, Color32BGRA.White.WithAlphaMult(0.80f), filled: false);
										}
									}

									// If gun is loaded, has been aiming at the target for long enough or was damaged recently
									if (is_loaded)
									{
										//if (!gun_state.hints.HasAll(Gun.Hints.Cycled))
										//{
										//	control.mouse.SetKeyPressed(Mouse.Key.Left, true);
										//}

										//App.WriteLine("b");

										if (is_los && can_attack && !is_attacking)
										{
											var burst_time = ((gun.flags.HasAny(Gun.Flags.Automatic) && gun.burst_count <= 1) ? gunner.attack_burst_time : random.NextFloatExtra(2.00f, 1.00f));
											var burst_cooldown = gunner.attack_cooldown;

											if (gun_state.hints.HasAny(Gun.Hints.Supressive_Fire))
											{
												burst_time *= random.NextFloatExtra(0.10f, 0.75f);
												burst_cooldown *= random.NextFloatExtra(0.20f, 0.45f);
											}

											gunner_state.next_attack = time + burst_time + burst_cooldown;
											gunner_state.time_burst_end = time + burst_time; // If the gun isn't automatic, default to a short burst time 

											//WorldNotification.Push(ref region, "burst", Color32BGRA.Red, transform.position);
										}
										else
										{
											if (time < gunner_state.time_burst_end)
											{
												if (is_artillery)
												{
													if (is_los)
													{
														if (is_aimed)
														{
															control.mouse.SetKeyPressed(Mouse.Key.Left, (gun.flags.HasAny(Gun.Flags.Automatic) && gun.burst_count <= 1) || (region.GetCurrentTick() % 4 == 0)); // Shithack if gun is not automatic, where holding LMB would prevent it from shooting
														}
													}
													else
													{
														if (is_aimed)
														{
															control.mouse.SetKeyPressed(Mouse.Key.Left, (gun.flags.HasAny(Gun.Flags.Automatic) && gun.burst_count <= 1) || (region.GetCurrentTick() % 4 == 0)); // Shithack if gun is not automatic, where holding LMB would prevent it from shooting
														}
													}
												}
												else
												{
													if (is_los || gun_state.hints.HasAny(Gun.Hints.Supressive_Fire))
													{
														control.mouse.SetKeyPressed(Mouse.Key.Left, (gun.flags.HasAny(Gun.Flags.Automatic) && gun.burst_count <= 1) || (region.GetCurrentTick() % 4 == 0)); // Shithack if gun is not automatic, where holding LMB would prevent it from shooting
													}
												}
											}


											//if (is_aimed && (is_artillery || is_los || gun_state.hints.HasAny(Gun.Hints.Supressive_Fire)) && time < gunner_state.time_burst_end)
											//if (is_aimed && (is_artillery || is_los || gun_state.hints.HasAny(Gun.Hints.Supressive_Fire)) && time < gunner_state.time_burst_end)
											//{
											//	control.mouse.SetKeyPressed(Mouse.Key.Left, (gun.flags.HasAny(Gun.Flags.Automatic) && gun.burst_count <= 1) || (region.GetCurrentTick() % 4 == 0)); // Shithack if gun is not automatic, where holding LMB would prevent it from shooting
											//}
											//else
											//{

											//}
										}
									}
								}
								else
								{
									//control.mouse.position = Vector2.Lerp(control.mouse.position, transform.LocalToWorld(new Vector2(1, -1)), 0.10f);
									//movement_override.flags.SetFlag(AI.Movement.Flags.No_Mouse, true);

									//App.WriteLine("no");

									gunner_state.los_time = 0.00f;
									gunner_state.aimed_time = Maths.MoveTowards(gunner_state.aimed_time, 0.00f, aim_decay_lerp * 0.10f);
								}
							}
							else
							{
								goto idle;
							}
						}
						else
						{
							goto idle;
						}

						if (AI.debug_always_sync)
						{
							gunner_state.Sync(entity);
						}

						return;

						idle:
						{
							//if (movement_override.position_target.HasValue) control.mouse.position = Vector2.Lerp(control.mouse.position, movement_override.position_target.Value + (transform.GetDirection() * 2.00f) + new Vector2(0.00f, -1.00f), 0.40f);
							//if (movement_override.position_target.HasValue) control.mouse.position = Vector2.Lerp(control.mouse.position, movement_override.position_target.Value + (transform.GetDirection() * 2.00f) + new Vector2(0.00f, -1.00f), 0.40f);

							//control.mouse.position = Vector2.Lerp(control.mouse.position, transform.position + (transform.GetDirection() * 2.00f) + new Vector2(0.00f, -1.00f), 0.40f);
						}


						//region.DrawDebugCircle(transform.position, npc_override.avoid_distance.X, Color32BGRA.Cyan.WithAlphaMult(0.50f), 2);
						//region.DrawDebugCircle(transform.position, npc_override.idle_distance.X, Color32BGRA.Magenta.WithAlphaMult(0.50f), 4);
					}
				}
			}
#endif
		}
	}
}
