
namespace TC2.Base.Components
{
	public static partial class AI
	{
		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public partial struct Movement: IComponent, IOverridable
		{
			public enum Type: byte
			{
				None = 0,

				Move,
				Follow,
				Straight,
				Drive,
				Passenger,
			}

			[Flags]
			public enum Flags: ushort
			{
				None = 0,

				Force_Repath = 1 << 0,
				Continuous = 1 << 1,
				No_Mouse = 1 << 2,
				No_Repath = 1 << 3,
				No_Move = 1 << 4,
				Climbing = 1 << 5,
				Driving = 1 << 6,
				Sitting = 1 << 7,
			}

			[Net.Segment.A, Asset.Ignore] public Movement.Type type;
			[Net.Segment.A, Asset.Ignore] public Movement.Flags flags;

			[Save.NewLine]
			[Asset.Ignore] public Vector2? position_target;
			[Asset.Ignore] public Vector2? position_path;
			[Asset.Ignore] public Vector2? position_stuck;
			[Asset.Ignore] public Vector2? position_stuck_contact;

			[Save.NewLine]
			[Asset.Ignore] public Vector2 position_legs;
			[Asset.Ignore] public Vector2 position_head;
			[Asset.Ignore] public Vector2 position_target_local;

			[Save.NewLine]
			[Asset.Ignore] public EntRef<Transform.Data> ref_target_transform;
			[Asset.Ignore] public EntRef<Body.Data> ref_target_body;

			[Save.NewLine]
			[Asset.Ignore, Save.Ignore] public Entity ent_last_door;
			[Asset.Ignore, Save.Ignore] public Timestamp ts_debug_last_reset;

			[Save.NewLine]
			[Asset.Ignore, Net.Ignore, Save.Ignore] public float next_update;
			[Asset.Ignore, Net.Ignore, Save.Ignore] public float next_interact;
			[Asset.Ignore, Net.Ignore, Save.Ignore] public float next_los_check;
			[Asset.Ignore, Net.Ignore, Save.Ignore] public float last_valid_los;

			[Asset.Ignore, Net.Ignore, Save.Ignore] private ulong unused_00;
			[Asset.Ignore, Net.Ignore, Save.Ignore] private ulong unused_01;

			public void SetTarget(Entity entity)
			{
				if (entity.IsAlive())
				{
					this.ref_target_transform.Set(entity);
					this.ref_target_body.Set(entity);
				}
				else
				{
					this.ref_target_transform = default;
					this.ref_target_body = default;

					//this.position_target = default;
					//this.position_target_local = default;
				}
			}

			public void Reset()
			{
				//App.WriteLine("reset");

				this.ref_target_transform = default;
				this.ref_target_body = default;

				this.position_target = default;
				this.position_target_local = default;
				this.position_path = default;

				this.position_stuck = default;
				this.position_stuck_contact = default;

				this.type = default;
				this.flags = default;

				this.next_update = default;
				this.next_interact = default;
				this.next_los_check = default;

				this.ts_debug_last_reset = Timestamp.Now();

				//App.WriteLine("reset");
			}

			[ISystem.PreUpdate.Reset(ISystem.Mode.Single, ISystem.Scope.Region)]
			public static void UpdateMovement_Reset([Source.Owned] in Body.Data body, [Source.Owned, Original] ref AI.Movement movement_original)
			{
				var pos_body = body.GetPosition();
				movement_original.position_head = pos_body;
				movement_original.position_legs = pos_body;
			}

			[ISystem.PreUpdate.A(ISystem.Mode.Single, ISystem.Scope.Region)]
			public static void UpdateHints_Climber(/*ISystem.Info info, Entity entity,*/
			[Source.Owned] in Climber.Data climber, [Source.Owned] in Body.Data body,
			[Source.Any] ref NPC.Data npc,
			[Source.Any, Original] ref AI.Movement movement_original, [HasTag("dead", true, Source.Modifier.Owned)] bool is_dead)
			//[Source.Any, Original] ref AI.Movement movement_original, [HasTag("dead", true, Source.Modifier.Owned)] bool is_dead)
			{
				npc.self_hints.SetFlag(NPC.SelfHints.Can_Climb, !is_dead
					&& climber.state_flags.HasAny(Climber.StateFlags.Has_Climber_Shape)
					&& climber.climb_speed >= 0.20f
					&& climber.cling_force >= (body.GetMass() * 0.01f));
			}

			[ISystem.PreUpdate.A(ISystem.Mode.Single, ISystem.Scope.Region)]
			public static void UpdateMovement_Runner(/*ISystem.Info info, Entity entity,*/
			[Source.Owned, Override] in Runner.Data runner, [Source.Owned] in Runner.State runner_state, [Source.Owned] in Body.Data body,
			[Source.Any] ref NPC.Data npc,
			[Source.Any, Original] ref AI.Movement movement_original, [HasTag("dead", true, Source.Modifier.Owned)] bool is_dead)
			{
				movement_original.position_legs = body.GetPosition();
				npc.self_hints.SetFlag(NPC.SelfHints.Can_Walk, !is_dead
					&& runner.max_speed >= 0.50f
					&& runner.walk_force >= (body.GetMass() * 0.01f));

				npc.self_hints.SetFlag(NPC.SelfHints.Is_Grounded, runner_state.flags.HasAny(Runner.State.Flags.Grounded));
				npc.self_hints.SetFlag(NPC.SelfHints.Is_Climbing, runner_state.flags.HasAny(Runner.State.Flags.Climbing));
				npc.self_hints.SetFlag(NPC.SelfHints.Is_Swimming, runner_state.flags.HasAny(Runner.State.Flags.Swimming));
			}

			[ISystem.PreUpdate.A(ISystem.Mode.Single, ISystem.Scope.Region)]
			public static void UpdateMovement_Head([Source.Owned] in Head.Data head, [Source.Owned] in Body.Data body,
			[Source.Any, Original] ref AI.Movement movement_original, [HasTag("dead", true, Source.Modifier.Owned)] bool is_dead)
			{
				movement_original.position_head = body.GetPosition();
			}

			[ISystem.PreUpdate.B(ISystem.Mode.Single, ISystem.Scope.Region)]
			public static void UpdateMovement_Overrides(/*ISystem.Info info, Entity entity,*/
			[Source.Owned] in Body.Data body,
			[Source.Owned, Original] ref AI.Movement movement_original, [Source.Owned, Override] ref AI.Movement movement_override)
			{
				movement_override.position_head = movement_original.position_head;
				movement_override.position_legs = movement_original.position_legs;
			}

#if SERVER
			[Shitcode]
			//[ISystem.Update.E(ISystem.Mode.Single, ISystem.Scope.Region)]
			[ISystem.Update.B(ISystem.Mode.Single, ISystem.Scope.Region)]
			[HasComponent<Player.Data>(Source.Modifier.Shared, false), HasTag("dead", false, Source.Modifier.Owned)]
			public static void UpdateMovement(ISystem.Info info, Entity entity, ref Region.Data region, ref XorRandom random,
			[Source.Owned] ref Control.Data control, [Source.Owned] in Body.Data body,
			[Source.Owned] ref Pathfinding.Data pathfinding, [Source.Owned] ref Pathfinding.State pathfinding_state,
			[Source.Owned, Original] ref AI.Data ai_original, [Source.Owned, Override] in AI.Data ai_override,
			[Source.Owned, Original] ref AI.Behavior behavior_original, [Source.Owned, Override] in AI.Behavior behavior_override,
			[Source.Owned, Original] ref AI.Movement movement_original, [Source.Owned, Override] ref AI.Movement movement_override,
			[Source.Owned] ref NPC.Data npc)
			{
				if (AI.debug_paused) return;

				//var ts = Timestamp.Now();
				var time = info.WorldTime;
				//var sync = false;

				var is_moving = false;
				var results = default(Pathfinding.Results);

				var los_pos = control.mouse.position;

				//if (pathfinding.flags.HasAny(Pathfinding.Flags.Air))
				//{
				//	movement_original.position_legs = body.GetPosition();
				//}

				//App.WriteLine(pathfinding_state.retry_count);

				var req_distance = 1.50f;

				if (npc.self_hints.HasAny(NPC.SelfHints.Is_Fleeing) && behavior_override.position_safety)
				{
					req_distance = 2.00f;
					npc.idle_distance = new(4, 2);
					movement_override.position_target = behavior_override.position_safety;

					is_moving = true;
				}
				else if (ai_override.fear > Maths.Max(ai_override.determination, ai_override.courage))
				{
					npc.avoid_distance = new(16, 0);
					npc.idle_distance = new(32, 2);

					is_moving = true;
				}

				//if (npc.self_hints.HasAny(NPC.SelfHints.In_Vehicle))
				//{
				//	npc.avoid_distance.X = 0;
				//	npc.avoid_distance.Y = 0;
				//	npc.idle_distance.X = 3;
				//	npc.idle_distance.Y = 0;
				//	req_distance = 3.00f;

				//	is_moving = true;
				//}

				switch (movement_override.type)
				{
					case Movement.Type.Move:
					{
						if (movement_override.flags.HasNone(Flags.No_Move) && time >= movement_original.next_update && movement_override.position_target.HasValue)
						{
							//App.WriteLine(req_distance);

							movement_override.position_path = pathfinding.GetPath(state: ref pathfinding_state,
								region: ref region,
								random: ref random,
								current_pos: movement_override.position_legs,
								target_pos: movement_override.position_target.GetValueOrDefault(),
								head_pos: movement_override.position_head,
								required_distance: req_distance,
								hints: npc.self_hints,
								blocked_los_pos: ref los_pos,
								results: out results,
								force_repath: movement_override.flags.HasAny(Movement.Flags.Force_Repath));


							//movement_override.position_path = pathfinding.GetPath(ref pathfinding_state, ref region, ref random, movement_override.position_legs, movement_override.position_target, npc_override.idle_distance.X, out results,
							//	force_repath: movement_override.flags.HasAll(Movement.Flags.Force_Repath));

							movement_original.flags.RemoveFlag(Movement.Flags.Force_Repath);
						}
					}
					break;

					case Movement.Type.Follow:
					{
						if (movement_override.flags.HasNone(Flags.No_Move) && time >= movement_original.next_update)
						{
							ref var target_transform = ref movement_override.ref_target_transform.GetComponent();
							if (target_transform.IsNotNull())
							{
								movement_override.position_target = target_transform.LocalToWorld(movement_override.position_target_local);

								movement_override.position_path = pathfinding.GetPath(state: ref pathfinding_state,
									region: ref region,
									random: ref random,
									current_pos: movement_override.position_legs,
									target_pos: movement_override.position_target.Value,
									head_pos: movement_override.position_head,
									required_distance: npc.idle_distance.X,
									hints: npc.self_hints,
									blocked_los_pos: ref los_pos,
									results: out results,
									force_repath: movement_override.flags.HasAny(Movement.Flags.Force_Repath));

								movement_original.flags.RemoveFlag(Movement.Flags.Force_Repath);
							}
							else
							{
								movement_original.Reset();
								//sync = true;
							}
						}
					}
					break;

					case Movement.Type.Straight:
					{
						movement_override.position_path = movement_override.position_target;
						movement_original.flags.RemoveFlag(Movement.Flags.Force_Repath);
					}
					break;
				}

				//WorldNotification.Push(ref region, $"{results}", Color32BGRA.Yellow, transform.position, lifetime: 2.00f);


				behavior_original.target_flags.SetFlag(Behavior.TargetFlags.Unreachable, results.HasAny(Pathfinding.Results.Unreachable) || (!pathfinding.flags.HasAny(Pathfinding.Flags.Air) && results.HasAny(Pathfinding.Results.In_Air)));
				behavior_original.target_flags.SetFlag(Behavior.TargetFlags.In_Air, results.HasAny(Pathfinding.Results.In_Air));
				//behavior_original.target_flags.AddFlag(Behavior.TargetFlags.Unreachable, results.HasAny(Pathfinding.Results.In_Air));
				//behavior_original.target_flags.SetFlag(Behavior.TargetFlags.Prefer_Melee, !results.HasAny(Pathfinding.Results.In_Air | Pathfinding.Results.Unreachable));

				//App.WriteLine(results);

				//App.WriteLine(movement_override.type);

				switch (movement_override.type)
				{
					case Movement.Type.Move:
					case Movement.Type.Follow:
					{
						if (results.HasAny(Pathfinding.Results.Done))
						{
							if (movement_original.flags.HasAny(Movement.Flags.Continuous))
							{
								is_moving = true;
							}
							else
							{
								if (behavior_override.type == Behavior.Type.Idle || behavior_override.type == Behavior.Type.Move || (behavior_override.type == Behavior.Type.Attack && !behavior_override.ref_target_threat.IsAlive()))
								{
									movement_original.Reset();
									//sync = true;
								}
								//else if (behavior_override.type == Behavior.Type.Move)
								//{
								//	is_moving = false;
								//}
								else
								{
									is_moving = true;
								}
							}
						}
						else if ((results.HasAny(Pathfinding.Results.Unreachable | Pathfinding.Results.Stuck | Pathfinding.Results.TimedOut) && (time - pathfinding_state.last_los_time) > 1.00f))
						{
							is_moving = true;

							npc.avoid_distance = new(16, 2);
							npc.idle_distance = new(32, 2);

							//App.WriteLine(time - pathfinding_state.last_los_time);
							//if (movement_original.flags.HasAll(Movement.Flags.Continuous))
							//{
							//	movement_original.next_update = time + 0.50f;
							//}
							//else
							//{
							//	movement_original.Reset();
							//	movement_original.next_update = time + 0.50f;
							//}
						}
						else
						{
							is_moving = true;
						}

						if (pathfinding_state.retry_count > 0)
						{
							//WorldNotification.Push(ref region, "* Stuck *", Color32BGRA.Yellow, transform.position, lifetime: 2.00f);
							//if (results.HasNone(Pathfinding.Results.LOS) && random.NextBool(0.50f))
							//{
							//	//control.keyboard.SetKeyPressed(Keyboard.Key.MoveUp, true);
							//	if (movement_override.position_target.HasValue) control.keyboard.SetKeyPressed((behavior_override.position_pivot.X - movement_override.position_target.Value.X).IsPositive() ? Keyboard.Key.MoveLeft : Keyboard.Key.MoveRight, true);
							//}

							if (results.HasAny(Pathfinding.Results.Stuck | Pathfinding.Results.Unreachable | Pathfinding.Results.TimedOut))
							{
								if (npc.self_hints.HasNone(NPC.SelfHints.Is_Fleeing | NPC.SelfHints.Is_Incapacitated)) ai_original.anger += (random.NextFloatRange(50.00f, 80.00f) + (ai_original.anger * 0.02f));
								//WorldNotification.Push(ref region, "Stuck", Color32BGRA.Yellow, transform.position);

								movement_original.position_stuck = body.GetPosition();
								if (movement_override.position_path.HasValue)
								{
									var dir = (movement_override.position_path.GetValueOrDefault() - body.GetPosition()).GetNormalized(out var dist);

									if (region.TryLinecast(world_position_start: body.GetPosition(), world_position_end: movement_override.position_path.GetValueOrDefault(), radius: 0.125f, result: out var linecast_result, mask: Physics.Layer.World | Physics.Layer.Door, exclude: Physics.Layer.Dynamic | Physics.Layer.Background, query_flags: Physics.QueryFlag.Static))
									{
										movement_original.position_stuck_contact = linecast_result.world_position; // - (linecast_result.normal_raw * 0.25f);
									}
								}

								WorldNotification.Push(ref region, "Stuck"u8, Color32BGRA.Yellow, movement_override.position_path.GetValueOrDefault());
								//if (movement_override.position_stuck.HasValue) region.DrawDebugCircle(movement_override.position_stuck.Value, 0.50f, color: Color32BGRA.Red, segments: 3, filled: true);

								//if (!region.IsInLineOfSight(body.GetPosition(), movement_override.position_target.Value, out var los_pos, 0.500f, mask: Physics.Layer.World | Physics.Layer.Door | Physics.Layer.Solid, query_flags: Physics.QueryFlag.Static))
								//{
								//	movement_original.position_stuck_contact = los_pos;
								//}

								//movement_original.position_stuck_contact = body.GetAnyContact();
							}

							if (results.HasAny(Pathfinding.Results.Stuck | Pathfinding.Results.Unreachable))
							{
								movement_original.next_update = time + (random.NextFloatRange(0.10f, 0.20f) * (1.00f + pathfinding_state.retry_count));

								//ai_original.anger += (random.NextFloatRange(20.00f, 80.00f) + (ai_original.anger * 0.02f));
								//WorldNotification.Push(ref region, "Stuck", Color32BGRA.Red, transform.position);

								//WorldNotification.Push(ref region, "* Stuck *"u8, Color32BGRA.Yellow, position: body.GetPosition(), lifetime: 2.00f);

								if ((time - movement_original.last_valid_los) < 3.00f)
								{
									pathfinding_state.retry_count = Maths.Min(pathfinding_state.retry_count, 3);
								}

								if ((pathfinding_state.retry_count >= 5 || ((time - pathfinding_state.last_pos_time) > 5.00f) && results.HasAny(Pathfinding.Results.Unreachable | Pathfinding.Results.Stuck)))
								{
									//ai_original.anger += (random.NextFloatRange(20.00f, 80.00f) + (ai_original.anger * 0.02f));
									WorldNotification.Push(ref region, "Stuck"u8, Color32BGRA.Red, position: body.GetPosition());

									behavior_original.Reset();
									movement_original.Reset();
									is_moving = false;

									pathfinding_state.Reset();

									//sync = true;

									//Sound.Play(ref region, Kobold.snd_insults.GetRandom(), transform.position, pitch: 1.20f);
								}
								//else
								//{
								//	Sound.Play(ref region, Kobold.snd_pissed.GetRandom(), transform.position, pitch: 1.20f);
								//}

								//ai_original.anger += random.NextFloatRange(350.00f, 500.00f) + (ai_original.anger * 0.50f);
							}
						}

					}
					break;

					case Movement.Type.Straight:
					{
						is_moving = true;
					}
					break;
				}

				if (pathfinding_state.retry_count == 0 && results.HasAnyExcept(Pathfinding.Results.Dirty, Pathfinding.Results.Stuck | Pathfinding.Results.Unreachable)) // && npc_override.self_hints.HasNone(NPC.SelfHints.Is_Door_Stuck))
				{
					movement_original.position_stuck.Unset(); // = null;
					movement_original.position_stuck_contact.Unset(); // = null;

					if (movement_original.ent_last_door.TryReset())
					{
						npc.self_hints.RemoveFlag(NPC.SelfHints.Is_Door_Stuck);
					}
				}

				is_moving &= time >= movement_original.next_update;
				//App.WriteLine(is_moving);

				//if (ai_override.fear > Maths.Max(ai_override.determination, ai_override.courage))
				//{
				//	npc_override.avoid_distance = new(16, 0);
				//	npc_override.idle_distance = new(32, 2);

				//	is_moving = true;
				//}


				//App.WriteLine($"t {behavior_override.type}; {movement_override.type}; {movement_override.flags}; {results}");

				if (is_moving && movement_override.position_target.TryGetValue(out var target_position_current) && movement_override.position_path.TryGetValue(out var position_path))
				{
					//App.WriteLine("moving");
					var is_done = results.HasAny(Pathfinding.Results.Done);

					var overshoot = ((body.GetVelocity() * App.fixed_update_interval_s * 2.50f));
					//var transform_pos_delta = (transform.GetPositionNew() - transform.GetPositionOld());


					//region.DrawDebugCircle(position_path, 1, Color32BGRA.Gray);

					var position = movement_override.position_legs;
					var position_pred = position + overshoot;


					//region.DrawDebugDir(position, transform_pos_delta * 8, color: Color32BGRA.Magenta);
					//region.DrawDebugLine(position, position_pred, color: Color32BGRA.Cyan, 4);


					//var target_position_current = movement_override.position_target;
					//var position_path = movement_override.position_path;
					var delta = position_path - position_pred;
					var delta_abs = Vector2.Abs(delta);

					var dir = delta.GetNormalized(out var dist);

					var is_los = (time - movement_original.last_valid_los) < 1.00f;
					//if (dist <= 25.00f)
					//{
					//	is_los = region.IsInLineOfSight(position, target_position_current, mask: Physics.Layer.World, query_flags: Physics.QueryFlag.Static);
					//}

					//region.DrawDebugDir(position, dir * 16, Color32BGRA.Magenta, 4);

					//WorldNotification.Push(ref region, "src", Color32BGRA.Yellow, position, lifetime: 5.00f);
					//WorldNotification.Push(ref region, "dst", Color32BGRA.Red, movement_override.position_path, lifetime: 5.00f);

					if (time >= movement_original.next_los_check)
					{


						//App.WriteLine($"t {behavior_override.type}; {movement_override.type}; {movement_override.flags}; {results}");
						//App.WriteLine($"check {movement_override.type}; {results}");



						if (dist <= 25.00f && behavior_override.ref_target_transform.entity.IsValid())
						{

							if (region.IsInLineOfSight(pos_a: position, pos_b: target_position_current,
							layer: Physics.Layer.Solid, mask: Physics.Layer.World, exclude: Physics.Layer.Dynamic, query_flags: Physics.QueryFlag.Static, skip_inside: true))
							{
								//App.WriteLine("los");
								movement_original.last_valid_los = time;
								is_los = true; // (time - movement_original.last_valid_los) < 1.00f;
							}
						}

						//if (!pathfinding_state.last_result.HasAny(Pathfinding.Results.LOS) || dist <= 4.00f)
						//if (Vector2.DistanceSquared(position, movement_override.position_path) < (4 * 4))
						if (pathfinding_state.last_result.HasNone(Pathfinding.Results.Done))
						{
							//App.WriteLine($"door {Vector2.Distance(position, target_position)}; {pathfinding_state.last_result.HasAny(Pathfinding.Results.LOS)}; {dist}; {is_los}");

							if (time >= movement_original.next_interact)
							{
								//WorldNotification.Push(ref region, "pos", Color32BGRA.Magenta, (position + (dir * 3.00f)), lifetime: 5.00f);
								//if (region.TryOverlapBB(position, new Vector2(4), out var overlap_result, mask: Physics.Layer.Solid, require: Physics.Layer.Door, exclude: Physics.Layer.World, query_flags: Physics.QueryFlag.Static))
								//{
								//	overlap_result.entity.NotifyDeferred(new Interactable.InteractEvent()
								//	{
								//		ent_interactor = entity,
								//		control = control
								//	}, sync: true);

								//	movement_original.next_interact = time + 1.00f - Maths.Clamp(ai_original.anger * 0.001f, 0.00f, 0.80f);
								//	ai_original.anger += random.NextFloatRange(20.00f, 80.00f) + (ai_original.anger * 0.02f);

								//	//movement_original.next_interact = info.WorldTime + 0.50f;
								//}

								//var ts2 = Timestamp.Now();

								npc.self_hints.RemoveFlag(NPC.SelfHints.Is_Door_Stuck);
								if (region.TryLinecast(world_position_start: position,
									world_position_end: position_path, // position + (dir * Maths.Min(dist, 3.00f)),
									radius: 0.50f,
									result: out var linecast_result,
									mask: Physics.Layer.Door,
									require: Physics.Layer.Solid,
									exclude: Physics.Layer.World | Physics.Layer.Background | Physics.Layer.Dynamic,
									query_flags: Physics.QueryFlag.Static))
								{
									//region.DrawDebugLine(position, position_path, color: Color32BGRA.Magenta, 4);
									//App.WriteLine($"door {time - pathfinding_state.last_pos_time}");

									if (npc.self_hints.HasAny(NPC.SelfHints.Can_Open_Doors))
									{
										linecast_result.entity.TriggerEventDeferred(new Interactable.InteractEvent()
										{
											ent_interactor = entity,
											faction_id = body.GetFaction(),
											control = control
										}, sync: true);

										movement_original.ent_last_door = linecast_result.entity;
										movement_original.next_interact = time + 1.00f - Maths.Clamp(ai_original.anger * 0.001f, 0.00f, 0.80f);
										ai_original.anger += random.NextFloatExtra(20.00f, 80.00f + (ai_original.anger * 0.02f));
									}
									else
									{
										if (time - pathfinding_state.last_pos_time > 3.00f)
										{
											//App.WriteLine(linecast_result.GetFactionHandle());
											if (linecast_result.GetFactionHandle().IsAttackable(npc.h_faction_cached))
											{
												behavior_original.SetTarget(linecast_result.entity);
											}
											else
											{

											}
											npc.self_hints.AddFlag(NPC.SelfHints.Is_Door_Stuck);

											//movement_original.position_stuck = position;

											//pathfinding_state.retry_count++;
											movement_original.position_stuck_contact = linecast_result.world_position;
											movement_original.ent_last_door = linecast_result.entity;
											movement_original.next_interact = time + 3.00f - Maths.Clamp(ai_original.anger * 0.001f, 0.00f, 0.80f);

											if (npc.self_hints.HasNone(NPC.SelfHints.Is_Fleeing | NPC.SelfHints.Is_Panicking)) ai_original.anger += random.NextFloatExtra(30.00f, 60.00f) + (ai_original.anger * 0.02f);
										}
										//App.WriteLine("door");

										//ai_original.anger += random.NextFloatRange(80.00f, 180.00f) + (ai_original.anger * 0.10f);
									}
								}
								//if (region.TryOverlapBB(position + (dir * 2.00f), new Vector2(3), out var overlap_result, mask: Physics.Layer.Door, require: Physics.Layer.Solid, exclude: Physics.Layer.World, query_flags: Physics.QueryFlag.Static))
								//{
								//	overlap_result.entity.NotifyDeferred(new Interactable.InteractEvent()
								//	{
								//		ent_interactor = entity,
								//		control = control
								//	}, sync: true);

								//	movement_original.next_interact = time + 1.00f - Maths.Clamp(ai_original.anger * 0.001f, 0.00f, 0.80f);
								//	ai_original.anger += random.NextFloatRange(20.00f, 80.00f) + (ai_original.anger * 0.02f);

								//	//movement_original.next_interact = info.WorldTime + 0.50f;
								//}
								//App.WriteLine($"{ts2.GetMilliseconds():0.0000} ms");
							}
						}

						movement_original.next_los_check = time + random.NextFloatExtra(0.30f, 0.70f);
					}

					//WorldNotification.Push(ref region, "A", Color32BGRA.Red, target_position);

					//is_los &= !npc_override.navigation_state.HasAny(NPC.NavigationState.Retreating);
					//is_done = !is_los;

					var can_move = movement_override.flags.HasNone(AI.Movement.Flags.No_Move);


					//if (ai_override.stance == AI.Stance.Defensive && !pos_safety.IsInRadius(target_position_current, 16.00f))
					if (false && ai_override.stance == AI.Stance.Defensive)
					{
						if (behavior_override.position_safety.IsNotZero() && !behavior_override.position_safety.IsInDistance(target_position_current, 16.00f))
						{
							can_move &= behavior_override.position_safety.IsInDistance(position_path, 16.00f);
							can_move &= behavior_override.position_safety.IsInDistance(position_pred, 16.00f);
						}
					}

					if (can_move)
					{
						if (pathfinding.flags.HasAny(Pathfinding.Flags.Air))
						{
							if (body.TryGetFirstArbiter(out var arbiter))
							{
								var norm = arbiter.GetNormal();
								position_path = position_pred - ((norm.GetPerpendicular(true) - body.GetVelocity())); //  + (norm * 0.10f)) * 10);
							}

							//region.DrawDebugDir(position, transform_pos_delta * 8, color: Color32BGRA.Magenta);
							//region.DrawDebugLine(position_pred, position_path, color: Color32BGRA.Cyan, 4);


							AI.MoveToAir(region: ref region, control: ref control, a: position_pred, b: position_path);
						}
						else if (pathfinding.flags.HasAny(Pathfinding.Flags.Land))
						{
							//var min = new Vector4(0, 0, 0, 0);
							//var max = new Vector4(0, 0, 0, 0);

							//var min = new Vector4(npc_override.avoid_distance.X, npc_override.avoid_distance.X, npc_override.avoid_distance.Y, npc_override.avoid_distance.Y);
							//var max = new Vector4(npc_override.idle_distance.X, npc_override.idle_distance.X, 0.50f, 2);

							//var climbing = false;

							//var max_delta_y = MathF.Abs(position_pred.Y - position_path.Y); // Maths.Max(MathF.Abs(position.Y - target_position_current.Y), (MathF.Abs(position.Y - target_position.Y)));
							//if (climber.IsNotNull())
							//{
							//	//App.WriteLine(climber.OffsetA);

							//	//if ((max_delta_y > 0.50f || delta_abs.X < delta_abs.Y) && pathfinding.flags.HasAny(Pathfinding.Flags.Climber) && region.TryOverlapBB(position, new Vector2(1.50f), out var overlap_result, mask: Physics.Layer.Climbable, exclude: Physics.Layer.Tree))
							//	if ((max_delta_y > 0.20f || delta_abs.X < delta_abs.Y) && climber.cling_entity.id != 0)
							//	{
							//		if (MathF.Abs(position.Y - position_path.Y) > 0.50f)
							//		{
							//			//target_position.X = climber.pos_climbable.X;
							//		}

							//		max.X = 1.250f;
							//		max.Y = 0.250f;

							//		//is_done = true;
							//		climbing = true;
							//	}
							//}

							if (is_los && !is_done)
							{
								position_path = target_position_current;
								movement_override.position_path = position_path;

							}

							//is_done = false;
							if (is_done)
							{
								//App.WriteLine("t");



								AI.MoveToLand(
									control: ref control,
									a: position_pred,
									b: position_path,
									min: new(npc.avoid_distance.X, npc.avoid_distance.X, npc.avoid_distance.Y, npc.avoid_distance.Y),
									max: new(npc.idle_distance.X, npc.idle_distance.X, 0.50f, 2),
									climbing: false);
								//Pathfinding.MoveToLand2(ref region, ref control, position, position_path);

								//App.WriteLine("a");
								//npc_override.MoveToLand(ref control, position_pred, position_pred, min, max, climbing);
								//Pathfinding.MoveToLand2(ref region, ref control, position, position_path);
							}
							else
							{
								//App.WriteLine("t");


								if (is_los)
								{
									if (results.HasAny(Pathfinding.Results.LOS) || pathfinding_state.points_count <= 2)
									//if (pathfinding_state.points_count <= 2)
									{


										//App.WriteLine("t");
										Pathfinding.MoveToLand2(region: ref region,
											control: ref control,
											pos_current: position,
											pos_dst: position_path,
											self_hints: npc.self_hints);
									}
									else
									{


										//Pathfinding.MoveToLand2(ref region, ref control, position, position_path);
										AI.MoveToLand(control: ref control,
											a: position,
											b: position_path,
											min: new(npc.avoid_distance.X, npc.avoid_distance.X, npc.avoid_distance.Y, npc.avoid_distance.Y),
											max: new(npc.idle_distance.X, npc.idle_distance.X, 0.50f, 2),
											climbing: false);
									}
									//npc_override.MoveToLand(ref control, position_pred, position_path, new(npc_override.avoid_distance.X, npc_override.avoid_distance.X, npc_override.avoid_distance.Y, npc_override.avoid_distance.Y), new(npc_override.idle_distance.X, npc_override.idle_distance.X, 0.50f, 2), false);
									//App.WriteLine("movetoland");
								}
								else
								{
									//App.WriteLine("t");


									Pathfinding.MoveToLand2(region: ref region,
										control: ref control,
										pos_current: position_pred,
										pos_dst: position_path,
										self_hints: npc.self_hints);
								}

								//if (npc_override.navigation_state.HasAll(NPC.NavigationState.Charging))
								//{
								//	npc_override.MoveToLand(ref control, position_pred, target_position, default, default);
								//}
								//else
								//{

								//App.WriteLine(npc_override.avoid_distance);
								//App.WriteLine("b");

								//npc_override.MoveToLand(ref control, position_pred, target_position, new(npc_override.avoid_distance.X, npc_override.avoid_distance.X, 0, 4), new(npc_override.idle_distance.X, npc_override.idle_distance.X, 4, 0), climbing);
								//}
							}
						}
					}

					if (movement_override.flags.HasNone(Movement.Flags.No_Mouse) && movement_override.position_path.HasValue)
					{
						control.mouse.position = Vector2.Lerp(control.mouse.position, movement_override.position_path.Value, 0.20f);
					}
				}

				if (npc.self_hints.HasAny(NPC.SelfHints.In_Vehicle | NPC.SelfHints.Is_Vehicle) || control.keyboard.GetKey(Keyboard.Key.NoMove))
				{
					//control.keyboard.SetKeyState(Keyboard.Key.MoveDown, false);
				}

				if (time >= movement_original.next_interact)
				{
					if (npc.self_hints.HasAny(NPC.SelfHints.Can_Open_Doors) && movement_override.ent_last_door.IsAlive())
					{
						movement_override.ent_last_door.TriggerEventDeferred(new Interactable.InteractEvent()
						{
							ent_interactor = entity,
							faction_id = body.GetFaction(),
							control = control
						}, sync: true);
					}
					movement_original.ent_last_door = default;
				}

				if (AI.debug_always_sync) // || true) // || results.HasAll(Pathfinding.Results.Dirty) || sync)
				{
					pathfinding.Sync(entity, true);
					pathfinding_state.Sync(entity, true);
					movement_original.Sync(entity, true);
					//behavior_original.Sync(entity, true);
					//npc_original.Sync(entity, true);
					ai_original.Sync(entity, true);
					//entity.SyncComponent<NPC.Data>();

					//App.WriteLine("sync");
				}

				//App.WriteLine($"{ts.GetMilliseconds():0.000} ms");
			}
#endif
		}
	}
}
