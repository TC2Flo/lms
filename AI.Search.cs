
namespace TC2.Base.Components
{
	public static partial class AI
	{
		public ref struct FetchNearestThreatArgs
		{
			[Flags]
			public enum Flags: ushort
			{
				None = 0,

				Find_Work = 1 << 0,
				Ignore_Friends = 1 << 1
			}

			public readonly Entity ent_search;
			public Entity ent_threat;

			public readonly float radius;
			public readonly float radius_sq;

			public readonly Vec2f pos;
			public readonly Vec2f pos_head;

			public Vec2f pos_enemies;
			public Vec2f pos_friends;
			public Vec2f pos_threat;

			public float threat_enemies;
			public float threat_friends;
			public float threat_highest;

			public readonly ref NPC.Data npc;
			public readonly ref AI.Behavior behavior;
			public readonly ref AI.Search search;

			public readonly IFaction.Handle h_faction;
			public FetchNearestThreatArgs.Flags flags;

			public (Entity ent_order, IComponent.Handle<Crafting.Order> h_order) avail_work;

			public FetchNearestThreatArgs(Entity ent_search, float radius, Vec2f pos, Vec2f pos_head, ref NPC.Data npc, ref AI.Behavior behavior, ref AI.Search search, IFaction.Handle h_faction, FetchNearestThreatArgs.Flags flags = Flags.None)
			{
				this.ent_search = ent_search;

				this.radius = radius;
				this.radius_sq = radius * radius;

				this.pos = pos;
				this.pos_head = pos_head;

				this.npc = ref npc;
				this.behavior = ref behavior;
				this.search = ref search;

				this.h_faction = h_faction;
				this.flags = flags;
			}
		}

		[ISystem.Manual(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void FetchNearestThreat(Entity entity, ref Region.Data region, [ISystem.Parameter] ref FetchNearestThreatArgs arg,
		[Source.Owned] in Body.Data body, [Source.Owned] in Threat.Data threat, [Source.Owned, Optional(true)] ref NPC.Data npc,
		[Source.Owned] in Transform.Data transform, [HasTag("dead", true, Source.Modifier.Owned)] bool is_dead)
		{
			if (is_dead) return;
			if (arg.IsNull()) return;
			if (entity.id == arg.ent_search) return;
			if (threat.priority_override <= 0.00f) return;
			if (arg.behavior.combat_flags.HasNone(Behavior.CombatFlags.Capture) && threat.flags.HasAny(Threat.Flags.Capturable)) return;

			var dist_sq = Vector2.DistanceSquared(transform.position, arg.pos);
			if (dist_sq > arg.radius_sq) return;

			var dist_modifier = Maths.NormalizeFast(dist_sq, arg.radius_sq).Sqrt(); //.Clamp01();
			var dist_modifier_inv = 1.00f - dist_modifier;

			//App.WriteLine($"{arg.ent_search}: {dist_modifier:0.00} {(1.00f / dist_modifier):0.00} {Vectors.RcpNormalizeSq(dist_sq, arg.radius_sq):0.00}");

			var threat_priority_tmp = threat.priority_override;
			//if (threat_priority_tmp < arg.threat_highest) return;

			if (Faction.IsFriendly(threat.faction_id, arg.h_faction)) // !(threat.faction_id == 0 || threat.faction_id != arg.h_faction))
			{
				if (arg.flags.HasNone(FetchNearestThreatArgs.Flags.Ignore_Friends))
				{
					arg.threat_friends += threat_priority_tmp * dist_modifier_inv; // (1.00f - Maths.NormalizeFast(dist_sq, arg.radius_sq));
					if (arg.pos_friends)
					{
						var pos_friends_new = arg.pos_friends.LerpFMA(transform.position, 0.50f);
						if (region.TryGetNearestPathPos(ref pos_friends_new.vect, include: Terrain.PathFlags.Ground, exclude: Terrain.PathFlags.Drop | Terrain.PathFlags.Solid | Terrain.PathFlags.Wall))
						{
							arg.pos_friends = pos_friends_new;
						}
					}
					else
					{
						arg.pos_friends = transform.position;
					}
				}

				if (arg.flags.HasAny(FetchNearestThreatArgs.Flags.Find_Work))
				{
					if (npc.IsNotNull() && npc.last_work.ent_order.id != 0 && npc.self_hints.HasAny(NPC.SelfHints.Is_Working))
					{
						arg.avail_work = npc.last_work;
						arg.flags.RemoveFlag(FetchNearestThreatArgs.Flags.Find_Work);
					}
				}
				//if (arg.)

				//region.DrawDebugCircle(transform.position, 4, Color32BGRA.Green);

				return;
			}
			//else if (arg.behavior.combat_flags.HasAny(Behavior.CombatFlags.Capture) && threat.flags.HasAny(Threat.Flags.Capturable))
			//{
			//	threat_priority_tmp = 0.10f;
			//}
			else
			{
				if (threat.flags.HasNone(Threat.Flags.No_Search_Priority_Skip))
				{ 
					if (threat_priority_tmp <= arg.search.threat_threshold) return;
				}

				//if (arg.behavior.combat_flags.HasAny(Behavior.CombatFlags.Defend) && !region.IsLOS(arg.pos_head, transform.position, allow_inside: true))
				//{
				//	return;
				//}

				//arg.enemy_threat += threat_priority_tmp * (1.00f - Maths.NormalizeFast(dist_sq, arg.radius_sq * 5.00f)) * 20.00f;
				arg.threat_enemies += threat_priority_tmp * dist_modifier_inv * 20.00f;
				//arg.pos_enemies.LerpFMARef(transform.position, 0.75f);

				if (arg.pos_enemies)
				{
					var pos_enemies_new = arg.pos_enemies.LerpFMA(transform.position, 0.50f);
					if (region.TryGetNearestPathPos(ref pos_enemies_new.vect, include: Terrain.PathFlags.Ground, exclude: Terrain.PathFlags.Drop | Terrain.PathFlags.Solid | Terrain.PathFlags.Wall))
					{
						arg.pos_enemies = pos_enemies_new;
					}
				}
				else arg.pos_enemies = transform.position;

				//region.DrawDebugCircle(transform.position, 4, Color32BGRA.Red);
			}

			//App.WriteValue(dist_modifier);

			if (arg.behavior.combat_flags.HasAny(Behavior.CombatFlags.Defend) && (!Vec2f.IsInDistance(transform.position, arg.pos_friends, 8.00f) && !region.IsLOS(arg.pos_head, transform.position, allow_inside: true)))
			{
				return;
			}

			threat_priority_tmp /= Maths.Max(0.01f, dist_modifier); // (1.00f - (dist_modifier * 0.05f));
																	//threat_priority_tmp *= (1.00f - (dist_modifier * 0.05f));
			if (threat_priority_tmp < arg.threat_highest) return;

			arg.ent_threat = entity;
			arg.threat_highest = threat_priority_tmp;
			arg.pos_threat = transform.position;
		}

		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public partial struct Search(): IComponent
		{
			public enum Type: byte
			{
				None = 0,
			}

			[Flags]
			public enum Flags: byte
			{
				None = 0,
			}

			//public EntRef<Threat.Data> ref_threat;

			public Search.Type type;
			public Search.Flags flags;

			//public Vector2 pos_enemies;
			//public Vector2 pos_friends;

			public float radius = 16.00f;
			public float threat_threshold = 5.00f;
			private float unused_00;

			//public Entity ent_test;
			//public int test_num;
			//public float test_dist_sq;

			//public Entity ent_test1;
			//public Entity ent_test2;
			//public Entity ent_test3;

			[Asset.Ignore, Net.Ignore, Save.Ignore] public float next_update;
			[Asset.Ignore, Net.Ignore, Save.Ignore] public float next_retarget;
			[Asset.Ignore, Net.Ignore, Save.Ignore] private float unused_01;
			[Asset.Ignore, Net.Ignore, Save.Ignore] private float unused_02;

			public void Reset()
			{
				this.type = default;

				this.next_update = default;
				this.next_retarget = default;
			}

			//[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
			//public static void TagTest1(ISystem.Info info, Entity entity, ref Region.Data region,
			//[Source.Owned, Original] ref AI.Search search_original,
			//[Source.Owned, Original] ref Organic.Data organic)
			//{
			//	search_original.ent_test1 = ECS.GetTagEntityConst1("dead");
			//}

			//[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
			//public static void TagTest2(ISystem.Info info, Entity entity, ref Region.Data region,
			//[Source.Owned, Original] ref AI.Search search_original,
			//[Source.Owned, Original] ref Organic.Data organic)
			//{
			//	search_original.ent_test2 = ECS.GetTagEntityConst2("dead");
			//}

			//[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
			//public static void TagTest3(ISystem.Info info, Entity entity, ref Region.Data region,
			//[Source.Owned, Original] ref AI.Search search_original,
			//[Source.Owned, Original] ref Organic.Data organic)
			//{
			//	search_original.ent_test3 = ECS.GetTagEntityConst3("dead");
			//}

			//[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
			//public static void AssetTest(ISystem.Info info, Entity entity,
			//[Source.Owned] in Faction.Data faction, [Source.Owned, Pair.Tag("hair")] ref Animated.Renderer.Data renderer)
			//{
			//	ref var faction_data = ref faction.id.GetData();
			//	if (faction_data.IsNotNull())
			//	{
			//		renderer.color = faction_data.color_a;
			//	}
			//}

			//[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
			//public static void CrashTest(Entity entity, ref Region.Data region,
			//[Source.Owned] in Transform.Data transform, [Source.Owned, Original] ref AI.Search search_original)
			//{
			//	var pos = transform.position;
			//	var arg = (pos: pos, ent_search: entity, ent_threat: default(Entity), test_num: -1, dist_sq: float.MaxValue);

			//	foreach (ref var row in region.IterateQuery<AI.GetAllThreatsQuery>())
			//	{
			//		row.Run((ISystem.Info info, Entity entity, ref Body.Data body, in Threat.Data threat, in Transform.Data transform) =>
			//		{
			//			if (entity == arg.ent_search) return;
			//			var dist_sq = Vector2.DistanceSquared(transform.position, arg.pos);
			//			if (dist_sq > arg.dist_sq) return;

			//			// some stuff
			//			arg.ent_threat = entity;
			//			arg.test_num = info.Offset;
			//			arg.dist_sq = dist_sq;
			//		});
			//	}

			//	search_original.test_num = arg.test_num;
			//	search_original.ent_test = arg.ent_threat;
			//	search_original.test_dist_sq = arg.dist_sq;
			//}

#if SERVER
			//[MethodImpl(MethodImplOptions.NoInlining)] // <-- TODO: workaround for fixing a crash on published build - for some reason, IterateQuery causes a fatal error when being JIT'd into optimized tier
			[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region, interval: 0.4793f)]
			[HasComponent<Player.Data>(Source.Modifier.Shared, false), HasTag("dead", false, Source.Modifier.Owned)]
			public static void UpdateSearch(ISystem.Info info, Entity entity, ref Region.Data region, ref XorRandom random,
			[Source.Owned] in Transform.Data transform,
			[Source.Owned] ref NPC.Data npc,
			[Source.Owned] ref AI.Search search,
			[Source.Owned, Original] ref AI.Movement movement_original, [Source.Owned, Override] ref AI.Movement movement_override,
			[Source.Owned, Original] ref AI.Behavior behavior_original, [Source.Owned, Override] ref AI.Behavior behavior_override)
			{
				if (AI.debug_paused) return;

				const bool debug_sync = false;
				const bool debug_info = false;
				const bool debug_notify = false;

				var time = info.WorldTime;
				//if (ai_override.stance == AI.Stance.Aggressive || (ai_override.stance == Stance.Defensive && (behavior_override.type == Behavior.Type.Attack || behavior_override.type == Behavior.Type.Defend)))
				if (time >= search.next_update)
				{
					//if (time >= search.next_update)
					//if (behavior_override.combat_flags.HasAny(Behavior.CombatFlags.Attack_Visible) && !(behavior_override.target_self_hints.HasAny(NPC.SelfHints.Is_Aiming)))
					if (behavior_override.combat_flags.HasAny(Behavior.CombatFlags.Attack_Visible) && !(movement_override.flags.HasAny(Movement.Flags.No_Move) && behavior_override.target_self_hints.HasAny(NPC.SelfHints.Is_Aiming)))
					{
						search.next_update = time + random.NextFloatExtra(0.90f, 1.80f);

						var h_faction = npc.h_faction_cached; // faction.OrDefault().id;

						var wants_retarget = false;

						var is_current_target_alive = behavior_original.ref_target_transform.IsAlive();
						if (is_current_target_alive)
						{
							ref var threat = ref behavior_original.ref_target_threat.GetComponent();
							if (threat.IsNotNull())
							{
								//if (behavior_override.combat_flags.HasAny(Behavior.CombatFlags.Capture) && threat.flags.HasAny(Threat.Flags.Capturable))
								//{
								//	search.next_retarget = Maths.Min(search.next_retarget, time + random.NextFloatRange(1.00f, 3.00f));
								//}

								if (threat.priority_override <= 0.00f || behavior_original.target_flags.HasAny(Behavior.TargetFlags.Dead) || (h_faction && threat.faction_id == h_faction))
								{
									//is_current_target_valid = false;
									wants_retarget = true;

									movement_original.SetTarget(behavior_override.target_goal.entity);
									behavior_original.SetTarget(behavior_override.target_goal.entity);
								}
							}
							else if (behavior_original.ref_target_threat.entity != 0)
							{
								if (debug_notify) WorldNotification.Push(ref region, $"Retarget {behavior_original.ref_target_threat.entity.GetFullName()}", Color32BGRA.Red, position: transform.position);

								//WorldNotification.Push(ref region, "* Retarget *"u8, Color32BGRA.Yellow, position: transform.position, lifetime: 2.00f);

								//WorldNotification.Push(ref region, $"* Retarget {behavior_original.ref_target_threat.entity.GetName()} *", Color32BGRA.Yellow, position: transform.position, lifetime: 8.00f);


								wants_retarget = true;
								behavior_original.Reset();
								movement_original.flags.AddFlag(Movement.Flags.Force_Repath);

								//WorldNotification.Push(ref region, "Retarget"u8, Color32BGRA.Red, position: transform.position);
							}
						}
						else // if (behavior_original.ref_target_transform.entity != 0)
						{
							wants_retarget = true;
						}

						if (wants_retarget || 
						//(time >= search.next_retarget) ||
						(time >= search.next_retarget) ||
						(!behavior_override.position_target.TryGetValue(out var pos_target) ||
						(Vec2f.IsInDistance(pos_target, movement_original.position_head, 24.00f) && !region.IsInLineOfSight(pos_a: pos_target, pos_b: movement_original.position_head, group: behavior_override.ref_target_transform.entity, mask: Physics.Layer.World, exclude: Physics.Layer.Dynamic, query_flags: Physics.QueryFlag.Static))))
						{
							search.next_retarget = time + random.NextFloatRange(2.00f, 10.00f);

							//var pos = transform.position;
							//var arg = (ent_search: entity,
							//	h_faction: h_faction,
							//	position: pos,
							//	radius: search.radius,
							//	radius_sq: search.radius.Pow2(),
							//	ent_root: default(Entity),
							//	ent_threat: default(Entity),
							//	threat_highest: 0.00f,
							//	threat_position: Vector2.Zero,
							//	pos_head: movement_original.position_head,
							//	pos_friends: pos,
							//	pos_enemies: pos,
							//	behavior_type: behavior_original.type,
							//	//self_hints: npc_override.self_hints,
							//	//item_hints: npc_override.item_hints,
							//	combat_flags: behavior_original.combat_flags,
							//	enemy_threat: 0.00f,
							//	friend_threat: 0.00f);


							var ts = Timestamp.Now();
							scoped var arg = new FetchNearestThreatArgs(ent_search: entity,
								radius: search.radius,
								pos: transform.position,
								pos_head: movement_original.position_head,
								npc: ref npc,
								behavior: ref behavior_override,
								search: ref search,
								h_faction: h_faction);

							//arg.flags.AddFlag(FetchNearestThreatArgs.Flags.Find_Work, )


							if (behavior_override.combat_flags.HasAny(Behavior.CombatFlags.Assist_Work) && npc.self_hints.HasAnyExcept(NPC.SelfHints.Can_Work, NPC.SelfHints.Is_Working))
							{
								arg.flags.AddFlag(FetchNearestThreatArgs.Flags.Find_Work);
							}

							//arg.flags.AddFlag(FetchNearestThreatArgs.Flags.Ignore_Friends, behavior_override.combat_flags.HasNone(Behavior.CombatFlags.Form_Groups))

							region.RunSystem(FetchNearestThreat, ref arg);

							if (arg.pos_enemies) behavior_original.position_enemies = arg.pos_enemies;
							if (behavior_override.combat_flags.HasAny(Behavior.CombatFlags.Form_Groups) && arg.flags.HasNone(FetchNearestThreatArgs.Flags.Ignore_Friends) && arg.pos_friends && arg.threat_friends >= (arg.threat_enemies * 0.10f) && region.TryGetAnyValidPathPos(ref arg.pos_friends.vect, include: Terrain.PathFlags.Ground))
							{
								behavior_original.position_friends = arg.pos_friends;
							}

							var pos_safety_tmp = arg.pos_friends.vect;

							//var is_valid = false;
							if (arg.pos_threat && arg.ent_threat.IsAlive())
							{
								var is_same_target = behavior_override.ref_target_transform.entity == arg.ent_threat;
								if (!is_same_target && npc.self_hints.HasNone(NPC.SelfHints.Is_Fleeing | NPC.SelfHints.Is_Doomed))
								{
									ref var threat = ref arg.ent_threat.GetComponent<Threat.Data>();
									if (threat.IsNotNull()) // && (threat.flags.HasNone(Threat.Flags.Capturable) || movement_original.type == Movement.Type.None))
									{
										//is_valid = true;

										//if (behavior_override.ref_target_transform.entity != arg.ent_threat)
										//{
										//	WorldNotification.Push(ref region, "* Spotted! *", Color32BGRA.Red, transform.position, lifetime: 3.00f, velocity: (arg.threat_position - transform.position).GetNormalized() * random.NextFloatRange(2.00f, 8.00f));
										//}

										var ev = new Commandable.OrderEvent()
										{
											type = threat.flags.HasAny(Threat.Flags.Capturable) ? Commandable.OrderType.Capture : Commandable.OrderType.Attack,
											position = arg.pos_threat,
											ent_target = arg.ent_threat
										};

										entity.TriggerEvent(ref ev);
									}
								}
							}
							else
							{
								if (behavior_original.position_home) pos_safety_tmp = behavior_original.position_home;
							}



							{
								if (behavior_override.combat_flags.HasAny(Behavior.CombatFlags.Flee | Behavior.CombatFlags.Defend | Behavior.CombatFlags.Cover))
								{
									//WorldNotification.Push(ref region, $"friends: {arg.threat_friends}\nenemies: {arg.threat_enemies}", Color32BGRA.Magenta, transform.position, lifetime: 3.00f);

									//if (arg.threat_friends > arg.threat_enemies)

									if (pos_safety_tmp.IsNotZero()) // && (!behavior_original.position_safety.HasValue || Vector2.DistanceSquared(behavior_original.position_safety.Value, transform.position) > (16 * 16)))
									{
										if (behavior_original.position_safety) pos_safety_tmp.LerpFMARef(behavior_original.position_safety, 0.50f);

										if (npc.self_hints.HasAny(NPC.SelfHints.Can_Fly))
										{
											if (region.TryGetAnyValidPathPos(pos: ref pos_safety_tmp, include: Terrain.PathFlags.None, exclude: Terrain.PathFlags.Solid | Terrain.PathFlags.Ground | Terrain.PathFlags.Door | Terrain.PathFlags.Ladder | Terrain.PathFlags.Wall)) // | Terrain.PathFlags.Ladder | Terrain.PathFlags.Door))
											{
												behavior_original.position_safety = pos_safety_tmp;
											}
										}
										else
										{
											if (region.TryGetAnyValidPathPos(pos: ref pos_safety_tmp, include: Terrain.PathFlags.Ground, exclude: Terrain.PathFlags.Solid | Terrain.PathFlags.Ladder))
											{
												behavior_original.position_safety = pos_safety_tmp;
												if (debug_sync) behavior_original.Sync(entity);
											}
										}
										//= region.GetNearestPathPos(search.pos_friends, Terrain.PathFlags.Ground | Terrain.PathFlags.Ladder | Terrain.PathFlags.Door);
									}
								}

								if (behavior_override.combat_flags.HasAny(Behavior.CombatFlags.Assist_Work) && arg.avail_work.ent_order != 0)
								{
									//var ev = new Commandable.OrderEvent()
									//{
									//	type = Commandable.OrderType.Work,
									//	h_component_target = arg.avail_work.h_order,
									//	ent_target = arg.avail_work.ent_order
									//};

									//entity.TriggerEvent(ref ev);

									//App.WriteLine("work");
									//behavior_original.type = Behavior.Type.Work;
									//movement_original.pos
								}
							}

							if (debug_info)
							{
								var ts_elapsed_ms = ts.GetMilliseconds();
								WorldNotification.Push(ref region,
								$"" +
								$"friends: {arg.threat_friends}\n" +
								$"enemies: {arg.threat_enemies}\n" +
								$"{ts_elapsed_ms:0.000} ms",
								Color32BGRA.White, transform.position, velocity: random.NextUnitVector2Extra(4, 6), lifetime: 3.00f);
							}
							//if (!is_valid)
							//{
							//	//behavior_original.Reset();
							//}
						}
					}
					else
					{
						search.next_update = time + random.NextFloatExtra(0.50f, 1.00f);
					}
				}
			}
#endif
		}
	}
}
