
using System.Numerics;

namespace TC2.Base.Components
{
	public static partial class AI
	{
		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public partial struct Behavior: IComponent, IOverridable
		{
			public enum Type: byte
			{
				Undefined = 0,

				Idle,
				Move,
				Pickup,
				Use,
				Attack,
				Defend,
				Harvest,
				Capture,
				Store,
				Work,
				Drive,
				Gunner
				//Flee,
			}

			[Flags]
			public enum TargetFlags: ushort
			{
				None = 0,

				No_Attack = 1 << 0,
				In_Air = 1 << 1,
				Careful = 1 << 2,
				Dangerous = 1 << 3,
				Friendly = 1 << 4,
				Weak = 1 << 5,
				Overpowered = 1 << 6,
				Dead = 1 << 7,
				Ranged = 1 << 8,
				Melee = 1 << 9,
				Unreachable = 1 << 10,
				Has_LOS = 1 << 11,
			}

			[Flags]
			public enum MovementFlags: ushort
			{
				None = 0,

				Hold = 1 << 0,
				Wander = 1 << 1,
				[Name("Open Doors")] Open_Doors = 1 << 2,
				[Name("Return Home")] Return_Home = 1 << 3,
			}

			[Flags] // TODO: rename, since it's not combat-only anymore
			public enum CombatFlags: uint
			{
				None = 0,

				[Name("Search", desc: "Search for nearby enemies and attack them.")]
				Attack_Visible = 1u << 0,
				[Name("Retaliate", desc: "Fight back when attacked.")]
				Attack_When_Hit = 1u << 1,
				[Name("Capture", desc: "Capture nearby objectives.")]
				[NotImplemented] Capture = 1u << 7,
				[Name("Cover", desc: "Seek cover when in danger.")]
				Cover = 1u << 8,
				[Name("Flee", desc: "Flee when in danger.")]
				Flee = 1u << 9,
				[Name("Defend", desc: "Defend the objective.")]
				Defend = 1u << 10,
				[Name("Evade", desc: "Evade enemy attacks by jumping around.")]
				Evade = 1u << 11,
				[Name("Chase", desc: "Chase enemies.")]
				Chase = 1u << 12,
				[Name("Charge", desc: "Charge at the target.")]
				Charge = 1u << 13,
				[Name("Incapacitate", desc: "Prefer incapacitating enemies instead of killing them.")]
				[NotImplemented] Incapacitate = 1u << 14,
				[Name("Prefer Melee", desc: "TODO: Desc")]
				[NotImplemented] Prefer_Melee = 1u << 15,
				[Name("Prefer Ranged", desc: "TODO: Desc")]
				[NotImplemented] Prefer_Ranged = 1u << 16,
				[Name("Form Groups", desc: "TODO: Desc")]
				Form_Groups = 1 << 17,
				[Name("Resupply", desc: "TODO: Desc")]
				[NotImplemented] Resupply = 1u << 18,
				[Name("Assist (Work)", desc: "TODO: Desc")]
				[NotImplemented] Assist_Work = 1 << 20,
			}

			//public EntRef<Harvestable.Data> ref_target_harvestable = default;

			// Configuration data
			[Net.Segment.A] public AI.Behavior.Type type;
			[Net.Segment.A] public AI.Behavior.MovementFlags movement_flags;
			[Net.Segment.A] public AI.Behavior.CombatFlags combat_flags;
			[Net.Segment.A] public NPC.ItemHints item_hints_prefer;

			[Save.NewLine] // Short-term target data
			[Net.Segment.B, Asset.Ignore, Save.Ignore] public AI.Behavior.TargetFlags target_flags;
			[Net.Segment.B, Asset.Ignore, Save.Ignore] public NPC.ItemHints target_item_hints;
			[Net.Segment.B, Asset.Ignore, Save.Ignore] public NPC.SelfHints target_self_hints;
			[Net.Segment.B, Asset.Ignore, Save.Ignore] private uint unused_00;
			[Net.Segment.B, Asset.Ignore, Save.Ignore] public Vector2? position_target;

			[Save.NewLine] // Misc positioning data
			[Net.Segment.C, Asset.Ignore] public Vec2f position_target_local;
			[Net.Segment.C, Asset.Ignore] public Vec2f position_safety;
			[Net.Segment.C, Asset.Ignore] public Vec2f position_home;
			[Net.Segment.C, Asset.Ignore] public Vec2f position_enemies;
			[Net.Segment.C, Asset.Ignore] public Vec2f position_friends;

			[Save.NewLine] // Long-term target data
			[Net.Segment.C, Asset.Ignore] public Vec2f position_goal;
			[Net.Segment.D, Asset.Ignore] public (Entity entity, IComponent.Handle h_component) target_goal;
			[Net.Segment.D, Asset.Ignore] public EntRef<Threat.Data> ref_target_threat;
			[Net.Segment.D, Asset.Ignore] public EntRef<Transform.Data> ref_target_transform;
			[Net.Segment.D, Asset.Ignore] public EntRef<Body.Data> ref_target_body;

			[Save.NewLine]
			[Asset.Ignore, Save.Ignore] public Vector2 position_pivot;
			[Asset.Ignore, Save.Ignore] public Timestamp ts_debug_last_reset;

			[Asset.Ignore] public float idle_timer;
			private float unused_01;
			private float unused_02;
			private float unused_03;

			[Save.NewLine]
			[Asset.Ignore, Net.Ignore, Save.Ignore] public float t_next_action;
			[Asset.Ignore, Net.Ignore, Save.Ignore] public float t_next_update;
			[Asset.Ignore, Net.Ignore, Save.Ignore] public float t_next_check;
			[Asset.Ignore, Net.Ignore, Save.Ignore] public float t_next_toolbelt;

			[Asset.Ignore, Net.Ignore, Save.Ignore] public float t_next_wander;
			[Asset.Ignore, Net.Ignore, Save.Ignore] public float t_last_reset;
			[Asset.Ignore, Net.Ignore, Save.Ignore] public float t_last_damage;
			private float unused_04;

			public void SetTarget(Entity entity)
			{
				if (entity.IsAlive())
				{
					//this.ref_target_harvestable.Set(entity);
					this.ref_target_threat.Set(entity);
					this.ref_target_transform.Set(entity);
					this.ref_target_body.Set(entity);
				}
				else
				{
					//this.ref_target_harvestable = default;
					this.ref_target_threat = default;
					this.ref_target_transform = default;
					this.ref_target_body = default;
				}

				//App.WriteLine($"set target {entity}");
			}

			public void Reset()
			{
				//this.ref_target_harvestable = default;
				this.ref_target_threat = default;
				this.ref_target_transform = default;
				this.ref_target_body = default;

				this.target_goal = default;
				//this.ent_target = default;

				this.position_target = default;
				this.position_target_local = default;

				this.type = AI.Behavior.Type.Undefined;
				this.target_flags = default;

				this.t_next_action = default;
				this.t_next_update = default;
				this.t_next_check = default;
				//this.next_toolbelt = default;
				//this.next_wander = default;
				this.idle_timer = default;

				this.ts_debug_last_reset = Timestamp.Now();


				//App.WriteLine("reset");
			}

#if SERVER
			[Shitcode]
			[ISystem.VeryLateUpdate(ISystem.Mode.Single, ISystem.Scope.Region, interval: 0.23f)]
			public static void UpdateHints(Entity entity,
			[Source.Owned, Optional(true)] ref Health.Data health, [Source.Owned] in Control.Data control,
			[Source.Owned] in Pathfinding.Data pathfinding, [Source.Owned] ref Pathfinding.State pathfinding_state,
			[Source.Owned, Override] in AI.Data ai_override,
			[Source.Owned, Original] ref AI.Behavior behavior_original, [Source.Owned, Override] in AI.Behavior behavior_override,
			[Source.Owned, Original] ref AI.Movement movement_original,
			[Source.Owned] ref NPC.Data npc,
			[HasTag("dead", true, Source.Modifier.Owned)] bool is_dead,
			[HasComponent<Player.Data>(Source.Modifier.Shared, true)] bool is_player)
			{
				var self_hints = npc.self_hints;
				var item_hints = npc.item_hints;

				var sync = false;

				var health_norm = 1.00f;
				if (health.IsNotNull())
				{
					health_norm = health.GetHealthNormalized();
				}

				//if (is_player)
				//{
				//	behavior_original.position_target = control.mouse.position;
				//}

				if (is_player)
				{
					behavior_original.position_target = control.mouse.position;
				}

				if (self_hints.TrySetFlag(NPC.SelfHints.Is_Player, is_player))
				{
					//App.WriteLine($"NPC: OnUpdateReset {entity.GetName()}, is_player: {is_player}");

					//behavior_original.Reset();
					//movement_original.Reset();
					pathfinding_state.Reset();
				}

				self_hints.RemoveFlag
				(
					NPC.SelfHints.Has_Hands | NPC.SelfHints.Can_Use_Guns | NPC.SelfHints.Can_Hold_Items | NPC.SelfHints.Can_Use_Items | NPC.SelfHints.Can_Open_Doors |
					NPC.SelfHints.Has_Toolbelt | NPC.SelfHints.Has_Weapon | NPC.SelfHints.Can_Attack_Melee | NPC.SelfHints.Can_Attack_Ranged | NPC.SelfHints.Is_Armored |
					NPC.SelfHints.Is_Injured | NPC.SelfHints.Is_Pained | NPC.SelfHints.Is_Vulnerable | NPC.SelfHints.Is_Doomed | NPC.SelfHints.Is_Idle | NPC.SelfHints.Is_Fleeing |
					NPC.SelfHints.Can_Fly | NPC.SelfHints.Is_Stuck
				);

				if (npc.ent_arm != default)
				{
					self_hints.AddFlag(NPC.SelfHints.Has_Hands | NPC.SelfHints.Can_Use_Guns | NPC.SelfHints.Can_Hold_Items | NPC.SelfHints.Can_Use_Items | NPC.SelfHints.Can_Open_Doors);
				}

				self_hints.RemoveFlag(NPC.SelfHints.Can_Open_Doors, behavior_override.movement_flags.HasNone(MovementFlags.Open_Doors));

				if (item_hints.HasAll(NPC.ItemHints.Weapon | NPC.ItemHints.Usable))
				{
					self_hints.AddFlag(NPC.SelfHints.Has_Weapon);
					self_hints.AddFlag(NPC.SelfHints.Can_Attack_Melee, item_hints.HasAny(NPC.ItemHints.Melee));
					self_hints.AddFlag(NPC.SelfHints.Can_Attack_Ranged, item_hints.HasAny(NPC.ItemHints.Ranged));
				}
				else if (item_hints.IsEmpty())
				{
					self_hints.AddFlag(NPC.SelfHints.Can_Attack_Melee, self_hints.HasAny(NPC.SelfHints.Can_Punch | NPC.SelfHints.Is_Animal));
				}

				self_hints.AddFlag(NPC.SelfHints.Has_Toolbelt, npc.ent_toolbelt != default);
				//self_hints.AddFlag(NPC.SelfHints.Has_Weapon, item_hints.HasAny(NPC.ItemHints.Weapon));
				//self_hints.AddFlag(NPC.SelfHints.Can_Attack_Melee, item_hints.HasAll(NPC.ItemHints.Weapon | NPC.ItemHints.Melee) || self_hints.HasAny(NPC.SelfHints.Can_Punch));
				//self_hints.AddFlag(NPC.SelfHints.Can_Attack_Ranged, item_hints.GetCount(NPC.ItemHints.Weapon | NPC.ItemHints.Gun | NPC.ItemHints.Long_Range) >= 2);
				self_hints.AddFlag(NPC.SelfHints.Is_Armored, item_hints.HasAll(NPC.ItemHints.Armor | NPC.ItemHints.Defensive));

				self_hints.AddFlag(NPC.SelfHints.Is_Pained | NPC.SelfHints.Is_Injured, (npc.pain * npc.pain_modifier) >= 800.00f);
				self_hints.AddFlag(NPC.SelfHints.Is_Injured, health_norm < 0.85f);
				self_hints.AddFlag(NPC.SelfHints.Is_Vulnerable, health_norm < 0.70f || npc.consciousness <= 0.70f);
				self_hints.AddFlag(NPC.SelfHints.Is_Doomed, health_norm < 0.40f || npc.consciousness <= 0.50f);
				self_hints.AddFlag(NPC.SelfHints.Is_Fleeing, Maths.NormalizeFast(ai_override.fear, ai_override.courage) * 500.00f > ai_override.determination);

				//App.WriteLine(Maths.NormalizeFast(ai_override.fear, ai_override.courage) * 500);

				self_hints.AddFlag(NPC.SelfHints.Is_Idle, behavior_override.idle_timer >= 10.00f);

				//self_hints.SetFlag(NPC.SelfHints.Can_Climb, pathfinding.flags.HasAny(Pathfinding.Flags.Climber));
				//self_hints.SetFlag(NPC.SelfHints.Can_Walk, pathfinding.flags.HasAny(Pathfinding.Flags.Land));
				self_hints.AddFlag(NPC.SelfHints.Can_Fly, pathfinding.flags.HasAny(Pathfinding.Flags.Air));
				self_hints.AddFlag(NPC.SelfHints.Is_Stuck, pathfinding_state.last_result.HasAny(Pathfinding.Results.Stuck) || pathfinding_state.retry_count > 2);

				self_hints.AddFlag(NPC.SelfHints.Is_Vulnerable, self_hints.HasAll(NPC.SelfHints.Is_Stuck | NPC.SelfHints.Is_Injured) | self_hints.HasNone(NPC.SelfHints.Has_Weapon | NPC.SelfHints.Has_Armor | NPC.SelfHints.Is_Animal));

				//if (self_hints.HasAny(NPC.SelfHints.Can_Walk))
				//{
				//	App.WriteLine("walk");
				//}

				//self_hints.SetFlag(NPC.SelfHints.Is_Stuck, behavior_override.);

				if (npc.self_hints != self_hints)
				{
					sync = true;

					//var delta = npc.self_hints & ~self_hints;
					//App.WriteLine(delta.GetCount());
				}

				npc.self_hints = self_hints;

				if (sync || AI.debug_always_sync)
				{
					behavior_original.Sync(entity, true);
					npc.Sync(entity, true);
				}
			}

			//[ISystem.Add(ISystem.Mode.Single, ISystem.Scope.Region)]
			//public static void OnPlayerAdded(ISystem.Info info, ref Region.Data region, Entity entity,
			//[Source.Owned, Original] ref AI.Behavior behavior_original, 
			//[Source.Shared] ref Player.Data player)
			//{
			//	App.WriteLine($"OnPlayerAdded({entity}, {player.ent_player}); type: {info.EventType}", App.Color.Cyan);

			//	behavior_original.Reset();
			//	behavior_original.Sync(entity, true);

			//	//switch (info.EventType)
			//	//{
			//	//	case ISystem.EventType.Add:
			//	//	{
			//	//		behavior_original.Reset();
			//	//		behavior_original.Sync(ent_behavior_original, true);
			//	//	}
			//	//	break;

			//	//	case ISystem.EventType.Remove:
			//	//	{

			//	//	}
			//	//	break;
			//	//}
			//}

			[ISystem.Update.E(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global, interval: 0.50f)]
			[HasComponent<Player.Data>(Source.Modifier.Shared, false), HasTag("dead", false, Source.Modifier.Owned)]
			public static void OnUpdateWorker(ISystem.Info.Common info, ref Region.Data.Common region, ref XorRandom random, Entity ent_worker,
			[Source.Owned, Original] ref AI.Data ai_original, [Source.Owned, Override] ref AI.Data ai_override,
			[Source.Owned, Original] ref AI.Behavior behavior_original, [Source.Owned, Override] ref AI.Behavior behavior_override,
			[Source.Owned] in Transform.Data transform, [Source.Owned] ref NPC.Data npc,
			[Source.Shared] ref Worker.Data worker, [Source.Shared] ref Worker.State worker_state)
			{
				if (behavior_override.type == Type.Work && npc.self_hints.HasAny(NPC.SelfHints.Can_Work))
				{
					if (behavior_override.target_goal.entity != 0)
					{
						//worker.ref_order.Set(behavior_override.target_goal.entity, behavior_override.target_goal.h_component);
					}
				}
			}

			[Shitcode]
			//[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region, order: -100)] //, interval: 0.16f)]
			[ISystem.EarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region, order: -100)] //, interval: 0.16f)]
			[HasComponent<Player.Data>(Source.Modifier.Shared, false), HasTag("dead", false, Source.Modifier.Owned)]
			public static void UpdateBehavior(ISystem.Info info, Entity entity, ref Region.Data region, ref XorRandom random,
			[Source.Owned] ref Pathfinding.Data pathfinding, [Source.Owned] ref Pathfinding.State pathfinding_state,
			[Source.Owned] ref Control.Data control, [Source.Owned] in Transform.Data transform, [Source.Owned] in Body.Data body,
			[Source.Owned, Original] ref AI.Data ai_original, [Source.Owned, Override] ref AI.Data ai_override,
			[Source.Owned, Original] ref AI.Behavior behavior_original, [Source.Owned, Override] ref AI.Behavior behavior_override,
			[Source.Owned, Original] ref AI.Movement movement_original, [Source.Owned, Override] ref AI.Movement movement_override,
			[Source.Owned] ref NPC.Data npc)
			{
				if (AI.debug_paused) return;

				var time = info.WorldTime;

				const bool debug_log_sync = false;

				var sync_behavior = false;
				var sync_movement = false;

				//if (behavior_override.position_safety.HasValue)
				//{
				//	region.DrawDebugCircle(behavior_override.position_safety.Value, 0.50f, Color32BGRA.Magenta, 2.00f, true);
				//}

				//if (behavior_override.combat_flags.HasAny(CombatFlags.Flee) && npc.self_hints.HasAny(NPC.SelfHints.Is_Doomed | NPC.SelfHints.Is_Fleeing))
				//{
				//	if (npc.item_hints.HasAll(NPC.ItemHints.Armor | NPC.ItemHints.Defensive) && npc.self_hints.HasAll(NPC.SelfHints.Has_Armor | NPC.SelfHints.Is_Stunned | NPC.SelfHints.Is_Grounded))
				//	{
				//		control.keyboard.SetKeyPressed(Keyboard.Key.MoveDown, true);
				//		if (npc.self_hints.HasAny(NPC.SelfHints.Is_Doomed)) control.keyboard.SetKeyPressed(Keyboard.Key.NoMove, true);
				//	}
				//	else
				//	{
				//		npc.self_hints.AddFlag(NPC.SelfHints.Is_Fleeing);
				//		behavior_original.type = Behavior.Type.Flee;
				//		behavior_override.type = Behavior.Type.Flee;
				//	}
				//}

				//if (time > behavior_original.next_update)
				//{

				//}

				//App.WriteLine($"{entity.GetName()} {behavior_override.type} {behavior_original.type}");
				//region.DrawDebugText(transform.position, $"test", Color32BGRA.Green);

				behavior_original.position_pivot = body.GetPosition();
				if (control.mouse.position == Vector2.Zero)
				{
					control.mouse.SetPosition(behavior_original.position_pivot + random.NextUnitVector2Range(1, 4)); // TODO: shithack
				}

				//if (!behavior_original.position_safety.HasValue && ai_original.ent_home.IsValid())
				//{
				//	ref var transform_home = ref ai_original.ent_home.GetTransform();
				//	if (transform_home.IsNotNull())
				//	{
				//		behavior_original.position_safety = transform_home.position;
				//	} 
				//}


				ref var target_threat = ref behavior_override.ref_target_threat.GetComponent();
				if (target_threat.IsNotNull())
				{
					behavior_original.target_self_hints = target_threat.self_hints;
					behavior_original.target_item_hints = target_threat.item_hints;
				}
				else
				{
					behavior_original.target_self_hints = default;
					behavior_original.target_item_hints = default;
				}

				//if (behavior_override.ref_target_body.TryGetHandle(out var h_target_body))
				//{
				//	behavior_original.position_target = h_target_body.data.GetPosition();
				//}

				switch (behavior_override.type)
				{
					case Behavior.Type.Attack:
					{
						//ai_override.stance = AI.Stance.Aggressive;

						//if (pathfinding_state.last_result.HasAny(Pathfinding.Results.Stuck | Pathfinding.Results.TimedOut))
						//{
						//	ai_original.anger += random.NextFloatRange(20.00f, 80.00f) + (ai_original.anger * 0.02f);
						//	WorldNotification.Push(ref region, "Stuck", Color32BGRA.Red, transform.position);
						//}

						ref var target_transform = ref behavior_override.ref_target_transform.GetComponent();
						if (target_transform.IsNotNull())
						{
							behavior_original.position_target = target_transform.position;
						}

						if (time >= behavior_original.t_next_check)
						{
							behavior_original.t_next_check = time + (0.50f * random.NextFloatRange(0.90f, 1.10f));

							if (behavior_override.ref_target_transform.entity.IsValid())
							{
								if (!behavior_override.ref_target_transform.entity.IsAlive())
								{
									behavior_original.Reset();
									sync_behavior = true;
									if (debug_log_sync) App.WriteLine($"[attack target] synced behavior ({entity.GetName()})");
								}
							}
							else
							{
								if (debug_log_sync) App.WriteLine($"[attack target] reset behavior ({entity.GetName()})");
								behavior_original.Reset();
								sync_behavior = true;
								//movement_original.Reset();
							}
						}
					}
					break;

					case Behavior.Type.Capture:
					{
						//ai_override.stance = AI.Stance.Aggressive;

						//npc_override.avoid_distance = new Vector2(gunner.attack_distance * 0.60f, 0);
						//npc_override.idle_distance = new Vector2(gunner.attack_distance * 1.00f, 4);

						if (time >= behavior_original.t_next_check)
						{
							behavior_original.t_next_check = time + random.NextFloatRange(0.90f, 1.30f);

							npc.avoid_distance = new Vector2(3.00f, 1);
							npc.idle_distance = new Vector2(6.00f, 1);

							if (!target_threat.IsNull())
							{
								if (target_threat.faction_id == body.GetFaction())
								{
									behavior_original.Reset();
									sync_behavior = true;
									if (debug_log_sync) App.WriteLine($"[capture done] synced behavior ({entity.GetName()})");
								}
							}
							else
							{
								if (behavior_override.ref_target_transform.entity.IsValid() && !behavior_override.ref_target_transform.entity.IsAlive())
								{
									behavior_original.Reset();
									sync_behavior = true;
									if (debug_log_sync) App.WriteLine($"[capture end] synced behavior ({entity.GetName()})");
								}
							}

							//ref var target_threat = ref behavior_override.ref_target_threat.GetValueOrNullRef();
							//if (!target_threat.IsNull())
							//{

							//}
							//else
							//{
							//	behavior_original.Reset();
							//}
						}
					}
					break;

					case Behavior.Type.Harvest:
					{
						//ai_override.stance = AI.Stance.Defensive;

						if (time >= behavior_original.t_next_check)
						{
							behavior_original.t_next_check = time + (0.50f * random.NextFloatRange(0.90f, 1.10f));

							if (behavior_override.ref_target_transform.entity.IsValid())
							{
								if (!behavior_override.ref_target_transform.entity.IsAlive())
								{
									behavior_original.Reset();
									sync_behavior = true;
									if (debug_log_sync) App.WriteLine($"[harvest done] synced behavior ({entity.GetName()})");
								}
							}
							else
							{
								behavior_original.Reset();
								//sync_behavior = true;
								//movement_original.Reset();
							}
						}
					}
					break;

					case Behavior.Type.Defend:
					{
						//ai_override.stance = AI.Stance.Aggressive;

						if (time >= behavior_original.t_next_check)
						{
							behavior_original.t_next_check = time + (0.50f * random.NextFloatRange(0.90f, 1.10f));

							if (behavior_override.ref_target_transform.entity.IsValid() && !behavior_override.ref_target_transform.entity.IsAlive())
							{
								behavior_original.Reset();
								sync_behavior = true;
								if (debug_log_sync) App.WriteLine($"[defend done] synced behavior ({entity.GetName()})");
							}
						}
					}
					break;

					case Behavior.Type.Pickup:
					{
						if (behavior_override.position_target.TryGetValue(out var pos))
						{
							//ref var target_transform = ref behavior_override.ref_target.GetValueOrNullRef();
							//if (!target_transform.IsNull())
							//{
							//	pos = target_transform.position;
							//}

							npc.avoid_distance = new Vector2(0.50f, 2);
							npc.idle_distance = new Vector2(0.50f, 4);

							ref var target_transform = ref behavior_override.ref_target_transform.GetComponent();
							if (!target_transform.IsNull())
							{
								pos = target_transform.LocalToWorldNoScale(behavior_override.position_target_local);

								movement_override.position_target = pos;
								movement_override.type = Movement.Type.Move;
							}

							if (time > behavior_original.t_next_check)
							{
								if (Vector2.DistanceSquared(pos, movement_override.position_legs) < (1.50f * 1.50f))
								{
									behavior_original.t_next_check = time + (0.50f * random.NextFloatRange(0.90f, 1.10f));

									var ent_pickup = npc.ent_pickup;
									if (ent_pickup.IsAlive())
									{
										if (npc.ent_pickup_target.IsValid()) //.GetChildCount(Relation.Type.Child) != 0)
										{
											Arm.Drop(ent_pickup, npc.ent_pickup_target);

											behavior_original.Reset();
											if (debug_log_sync) App.WriteLine($"synced behavior ({entity.GetName()})"); sync_behavior = true;
										}
										else
										{
											Arm.Pickup(ent_pickup, behavior_override.ref_target_transform.entity, local_position: behavior_override.position_target_local);

											if (movement_original.type == Movement.Type.Follow && movement_original.ref_target_transform.entity == behavior_override.ref_target_transform.entity)
											{
												movement_original.Reset();
												sync_movement = true;
											}

											behavior_original.Reset();
											if (debug_log_sync) App.WriteLine($"synced behavior ({entity.GetName()})"); sync_behavior = true;
										}
									}
								}
								else
								{
									behavior_original.t_next_check = time + (0.10f * random.NextFloatRange(0.90f, 1.10f));
								}
							}
						}
					}
					break;

					case Behavior.Type.Use:
					{
						if (time > behavior_original.t_next_check)
						{
							if (behavior_override.position_target.TryGetValue(out var pos))
							{
								//var pos = behavior_override.position_target;

								ref var target_transform = ref behavior_override.ref_target_transform.GetComponent();
								if (!target_transform.IsNull())
								{
									pos = target_transform.position;
								}

								if (Vector2.DistanceSquared(pos, transform.position) < 3.00f * 3.00f)
								{
									var oc_worker = entity.GetComponentWithOwner<Worker.Data>(Relation.Type.Instance);
									ref var worker = ref oc_worker.data;

									var reset = false;

									var ent_target = behavior_override.ref_target_transform.entity;
									if (ent_target.IsAlive())
									{
										ref var target_interactable = ref ent_target.GetComponent<Interactable.Data>();
										if (!target_interactable.IsNull())
										{
											// TODO: add an interact event

											control.keyboard.SetKeyPressed(Keyboard.Key.E, true);

											if (target_interactable.flags.HasAny(Interactable.Flags.Once))
											{
												reset = true;
											}
										}
									}

									if (reset)
									{
										movement_original.Reset();
										behavior_original.Reset();

										sync_behavior = true;
										sync_movement = true;
									}

									if (!worker.IsNull())
									{
										//worker.SetWorkTarget(oc_worker.entity, ent_target);
										behavior_original.t_next_check = time + (5.00f * random.NextFloatRange(0.90f, 1.10f));
									}
									else
									{
										behavior_original.t_next_check = time + (1.00f * random.NextFloatRange(0.90f, 1.10f));
									}
								}
								else
								{
									behavior_original.t_next_check = time + (0.10f * random.NextFloatRange(0.90f, 1.10f));
								}
							}
						}
					}
					break;

					case Behavior.Type.Work:
					{
						if (time > behavior_original.t_next_check)
						{
							if (behavior_override.position_target.TryGetValue(out var pos))
							{
								//var pos = behavior_override.position_target;

								//ref var target_transform = ref behavior_override.ref_target_transform.GetComponent();
								//if (target_transform.IsNotNull())
								//{
								//	pos = target_transform.position;

								//	movement_override.position_target = pos;
								//	movement_override.type = Movement.Type.Follow;
								//}

								movement_override.type = Movement.Type.Follow;
								npc.avoid_distance = new Vector2(0.50f, 1);
								npc.idle_distance = new Vector2(2.00f, 2);

								//ref var target_transform = ref behavior_override.ref_target_transform.GetComponent();
								//if (target_transform.IsNotNull())
								//{
								//	//pos = target_transform.LocalToWorldNoScale(behavior_override.position_target_local);

								//	movement_override.position_target = pos;
								//	movement_override.type = Movement.Type.Follow;
								//}

								//App.WriteLine("worker working");

								if (Vector2.DistanceSquared(pos, transform.position) < 3.00f * 3.00f)
								{
									
									var reset = false;

									var ent_target = behavior_override.ref_target_transform.entity;
									if (ent_target.IsAlive())
									{
										//var oc_worker = entity.GetComponentWithOwner<Worker.Data>(Relation.Type.Instance);
										//ref var worker = ref oc_worker.data;
										//if (worker.IsNotNull())
										//{
										//	ref var worker_state = ref oc_worker.entity.GetComponent<Worker.State>();
										//	ref var workshop = ref ent_target.GetComponent<Workshop.Data>();
										//	if (worker_state.IsNotNull() && workshop.IsNotNull())
										//	{
										//		//App.WriteLine("worker working");


										//		if (worker.ref_order.ComponentID != 0 && worker_state.work_index.HasValue) //worker_state.last_order_flags.HasAny(Crafting.Order.Flags.In_Progress))
										//		{

										//		}
										//		else
										//		{
										//			App.WriteLine("worker working");

										//			var span_status_flags = workshop.order_status_flags.AsSpan(workshop.orders_capacity);
										//			span_status_flags.GetFirstOrNull(static (x) => x.HasAny(Workshop.StatusFlags.Needs_Workers), out var index);
										//			if (index >= 0)
										//			{
										//				Worker.SetWorkTarget(worker: ref worker, ent_worker: oc_worker.entity, ent_target: ent_target, h_order: IComponent.Handle<Crafting.Order>.FromIndexPair((uint)index));
										//				worker_state.next_check = default;
										//				worker_state.next_work = default;
										//			}
										//		}
										//	}
										//}


										//ref var target_interactable = ref ent_target.GetComponent<Interactable.Data>();
										//if (!target_interactable.IsNull())
										//{
										//	// TODO: add an interact event

										//	control.keyboard.SetKeyPressed(Keyboard.Key.E, true);

										//	if (target_interactable.flags.HasAny(Interactable.Flags.Once))
										//	{
										//		reset = true;
										//	}
										//}
									}

									if (reset)
									{
										movement_original.Reset();
										behavior_original.Reset();

										sync_behavior = true;
										sync_movement = true;
									}

									//if (worker.IsNotNull())
									//{
									//	//worker.SetWorkTarget(oc_worker.entity, ent_target);
									//	behavior_original.t_next_check = time + (5.00f * random.NextFloatRange(0.90f, 1.10f));
									//}
									//else
									//{
									//	behavior_original.t_next_check = time + (1.00f * random.NextFloatRange(0.90f, 1.10f));
									//}
								}
								else
								{
									behavior_original.t_next_check = time + (0.10f * random.NextFloatRange(0.90f, 1.10f));
								}
							}
						}
					}
					break;

					case Behavior.Type.Move:
					{
						if (movement_override.type == Movement.Type.None) // || (movement_override.type == Movement.Type.Move && Vector2.DistanceSquared(behavior_override.position_target, transform.position) <= 2.00f * 2.00f))
						{
							behavior_original.Reset();
							sync_behavior = true;
							if (debug_log_sync) App.WriteLine($"[move] synced behavior ({entity.GetName()})");
							//sync_behavior = true;
							//App.WriteLine("reset");

							//App.WriteLine(pathfinding_state.last_update_time);

							//if (pathfinding_state.last_update_time)
							//{

							//}
						}
					}
					break;

					//case Behavior.Type.Flee:
					//{
					//	if (time >= behavior_original.t_next_check)
					//	{
					//		behavior_original.t_next_check = random.NextFloatExtra(time + 0.30f, 0.50f);

					//		//movement_original.type = Movement.Type.Move;
					//		//movement_original.position_target = behavior_override.position_safety;
					//		//movement_original.ref_target_body.Reset();
					//		//movement_original.ref_target_transform.Reset();
					//		//sync_movement = true;

					//		if (npc.self_hints.HasAny(NPC.SelfHints.Is_Fleeing))
					//		{
					//			npc.emotions_queue.PushEmotionEvent(Emotion.Type.Fear);

					//			//if (npc.self_hints.HasAny(NPC.SelfHints.Is_Idle) && (!behavior_override.position_safety.IsZero() ? Vector2.DistanceSquared(behavior_override.position_safety, movement_override.position_legs) <= 6.Pow2() : npc.self_hints.HasNone(NPC.SelfHints.Has_LOS)))
					//			if (behavior_override.position_safety.IsNotZero() ? Maths.IsInDistance(behavior_override.position_safety, movement_override.position_legs, 6) : npc.self_hints.HasNone(NPC.SelfHints.Has_LOS))
					//			{
					//				npc.self_hints.RemoveFlag(NPC.SelfHints.Is_Fleeing);

					//				behavior_original.Reset();

					//				sync_behavior = true;
					//				if (debug_log_sync) App.WriteLine($"[flee] synced behavior ({entity.GetName()})");
					//			}
					//		}

					//		//movement_original.Reset();

					//		//if (behavior_override.position_target.HasValue && Vector2.DistanceSquared(behavior_override.position_target.GetValueOrDefault(), transform.position) <= (24 * 24)
					//		////&& region.IsInLineOfSight(transform.position, behavior_override.position_target.Value, mask: Physics.Layer.World, query_flags: Physics.QueryFlag.Static))
					//		//&& region.IsLOS(transform.position, behavior_override.position_target.GetValueOrDefault())) //, mask: Physics.Layer.World, query_flags: Physics.QueryFlag.Static))
					//		//{
					//		//	//movement_override.flags.SetFlag(Movement.Flags.No_Mouse, true);
					//		//}


					//	}
					//}
					//break;

					case Behavior.Type.Drive:
					{
						if (behavior_override.position_target.TryGetValue(out var pos))
						{
							//ref var target_transform = ref behavior_override.ref_target.GetValueOrNullRef();
							//if (!target_transform.IsNull())
							//{
							//	pos = target_transform.position;
							//}

							var ent_target = behavior_override.target_goal.entity;

							//npc.avoid_distance = new Vector2(0.50f, 2);
							//npc.idle_distance = new Vector2(0.50f, 4);

							ref var target_transform = ref behavior_override.ref_target_transform.GetComponent();
							if (target_transform.IsNotNull())
							{
								//pos = target_transform.LocalToWorldNoScale(behavior_override.position_target_local);

								movement_override.position_target = pos;
								movement_override.type = Movement.Type.Follow;
							}

							if (time > behavior_original.t_next_check)
							{
								if (Vector2.DistanceSquared(pos, movement_override.position_legs) < (1.50f * 1.50f))
								{
									behavior_original.t_next_check = time + (0.50f * random.NextFloatRange(0.90f, 1.10f));

									var ent_seat = Entity.Empty;
									var ent_seat_driver = Entity.Empty;

									var ent_vehicle = ent_target;
									ref var vehicle = ref ent_target.GetComponent<Vehicle.Data>();
									if (vehicle.IsNotNull())
									{
										ent_seat = vehicle.ent_seat;
										ent_seat_driver = vehicle.ent_seat_driver;
									}

									if (ent_seat.IsAlive() && ent_seat.HasComponent<Seat.Data>())
									{
										App.WriteValue("sit");
										if (ent_seat_driver == entity)
										{
											Vehicle.RemovePassenger(ent_vehicle: ent_seat, ent_passenger: entity);
										}
										else
										{
											Vehicle.AddPassenger(ent_seat: ent_seat, ent_passenger: entity);
										}

										movement_original.Reset();
										behavior_original.Reset();

										sync_behavior = true;
										sync_movement = true;
									}

									//var ent_pickup = npc.ent;
									//if (ent_pickup.IsAlive())
									//{
									//	if (npc.ent_pickup_target.IsValid()) //.GetChildCount(Relation.Type.Child) != 0)
									//	{
									//		Arm.Drop(ent_pickup, npc.ent_pickup_target);

									//		behavior_original.Reset();
									//		if (debug_log_sync) App.WriteLine($"synced behavior ({entity.GetName()})"); sync_behavior = true;
									//	}
									//	else
									//	{
									//		Arm.Pickup(ent_pickup, behavior_override.ref_target_transform.entity, local_position: behavior_override.position_target_local);

									//		if (movement_original.type == Movement.Type.Follow && movement_original.ref_target_transform.entity == behavior_override.ref_target_transform.entity)
									//		{
									//			movement_original.Reset();
									//			sync_movement = true;
									//		}

									//		behavior_original.Reset();
									//		if (debug_log_sync) App.WriteLine($"synced behavior ({entity.GetName()})"); sync_behavior = true;
									//	}
									//}
								}
								else
								{
									behavior_original.t_next_check = time + (0.10f * random.NextFloatRange(0.90f, 1.10f));
								}
							}
						}
					}
					break;

					case Behavior.Type.Store:
					{
						if (behavior_override.position_target.TryGetValue(out var pos))
						{
							//var pos = behavior_override.position_target;

							npc.avoid_distance = new Vector2(0.20f, 2);
							npc.idle_distance = new Vector2(1.00f, 4);

							ref var target_transform = ref behavior_override.ref_target_transform.GetComponent();
							if (!target_transform.IsNull())
							{
								pos = target_transform.LocalToWorldNoScale(behavior_override.position_target_local);

								movement_override.position_target = pos;
								movement_override.type = Movement.Type.Move;
							}



							//ref var target_transform = ref behavior_override.ref_target.GetValueOrNullRef();
							//if (!target_transform.IsNull())
							//{
							//	pos = target_transform.position;
							//}

							//ref var target_transform = ref behavior_override.ref_target_transform.GetValueOrNullRef();
							//if (!target_transform.IsNull())
							//{
							//	pos = target_transform.LocalToWorldNoScale(behavior_override.position_target_local);

							//	movement_override.position_target = pos;
							//	movement_override.type = Movement.Type.Move;
							//}

							if (time > behavior_original.t_next_check)
							{
								if (Vector2.DistanceSquared(pos, transform.position) < (1.50f * 1.50f))
								{
									behavior_original.t_next_check = time + (0.50f * random.NextFloatRange(0.90f, 1.10f));


									ref var storage_src = ref entity.GetComponent<Storage.Data>();
									if (storage_src.IsNotNull())
									{
										if (storage_src.inv_storage.TryGetHandle(out var h_inventory_src))
										{
											ref var storage_dst = ref behavior_override.ref_target_transform.entity.GetComponent<Storage.Data>();
											if (storage_dst.IsNotNull())
											{
												if (storage_dst.inv_storage.TryGetHandle(out var h_inventory_dst))
												{
													App.WriteLine("insert");

													var resources = h_inventory_src.GetSpan();
													for (var i = 0; i < h_inventory_src.Length; i++)
													{
														ref var resource = ref resources[i];
														h_inventory_dst.Deposit(ref resource, resource.quantity);
													}

													h_inventory_src.MarkDirty();
													h_inventory_dst.MarkDirty();

													behavior_original.Reset();
													sync_behavior = true;

													if (debug_log_sync) App.WriteLine($"[store insert] synced behavior ({entity.GetName()})");
												}
											}
										}
									}


									//Span<Entity> children = stackalloc Entity[8];
									//entity.GetChildren(ref children, Relation.Type.Instance);

									//var ent_arm = default(Entity);

									//foreach (var child in children)
									//{
									//	if (child.HasComponent<Arm.Data>())
									//	{
									//		ent_arm = child;
									//		break;
									//	}
									//}

									//if (ent_arm.IsAlive())
									//{
									//	if (ent_arm.GetChildCount(Relation.Type.Child) != 0)
									//	{
									//		Arm.Drop(ent_arm);

									//		behavior_original.Reset();
									//		sync = true;
									//	}
									//	else
									//	{
									//		Arm.Pickup(ent_arm, behavior_override.ref_target_transform.entity, local_position: behavior_override.position_target_local);

									//		if (movement_original.type == Movement.Type.Follow && movement_original.ref_target_transform.entity == behavior_override.ref_target_transform.entity)
									//		{
									//			movement_original.Reset();
									//		}

									//		behavior_original.Reset();
									//		sync = true;
									//	}
									//}
								}
								else
								{
									behavior_original.t_next_check = time + (0.10f * random.NextFloatRange(0.90f, 1.10f));
								}
							}
						}
					}
					break;

					case Behavior.Type.Idle:
					{
						//if (region.GetCurrentTick() % 50 == 0)
						//{
						//	WorldNotification.Push(ref region, $"idle {behavior_override.idle_timer}\nnext wander: {behavior_override.next_wander - time:0.00}s", Color32BGRA.Green, transform.position);
						//}


						//if (behavior_override.idle_timer > 1.00f && movement_override.type == Movement.Type.None && time > behavior_override.next_wander)
						if (behavior_override.idle_timer > 5.00f && npc.self_hints.HasNone(NPC.SelfHints.Is_Working | NPC.SelfHints.Is_Vehicle | NPC.SelfHints.In_Vehicle))
						{
							//App.WriteLine($"{behavior_original.next_wander}");

							if (behavior_override.movement_flags.HasAny(MovementFlags.Wander))
							{

								//if (behavior_original.next_wander != 0.00f)
								if (time > behavior_override.t_next_wander && (pathfinding_state.last_repath_result == Terrain.RepathResult.Undefined || pathfinding_state.last_repath_result == Terrain.RepathResult.Done || pathfinding_state.last_result.HasAny(Pathfinding.Results.Done | Pathfinding.Results.Unreachable | Pathfinding.Results.Stuck | Pathfinding.Results.TimedOut)))
								{
									//movement_original.position_target = transform.position + (random.NextVector2Range(new Vector2(-4.00f, -1.00f), new Vector2(4.00f, 1.00f)) * 8);

									//var wander_dir = random.NextUnitVector2Range(npc_override.wander_radius * 0.50f, npc_override.wander_radius);
									//if (!pathfinding.flags.HasAny(Pathfinding.Flags.Air)) wander_dir.Y = -Maths.Min(MathF.Abs(wander_dir.Y * 0.50f), 8.00f);

									var path_flags = Terrain.PathFlags.None;
									var path_flags_exclude = Terrain.PathFlags.Solid | Terrain.PathFlags.Underwater | Terrain.PathFlags.Water;

									var end_path_flags = Terrain.PathFlags.None;
									var end_path_flags_require = Terrain.PathFlags.None;
									var end_path_flags_exclude = Terrain.PathFlags.Solid | Terrain.PathFlags.Underwater | Terrain.PathFlags.Water;

									if (pathfinding.flags.HasAny(Pathfinding.Flags.Air))
									{
										//path_flags |= Terrain.PathFlags.Any;
									}

									if (pathfinding.flags.HasAny(Pathfinding.Flags.Land))
									{
										path_flags |= Terrain.PathFlags.Ground | Terrain.PathFlags.Edge;
										end_path_flags |= Terrain.PathFlags.Ground;
										end_path_flags_require |= Terrain.PathFlags.Ground;
										end_path_flags_exclude |= Terrain.PathFlags.Drop;

										if (pathfinding.flags.HasNone(Pathfinding.Flags.Climber | Pathfinding.Flags.Air))
										{
											end_path_flags_exclude |= Terrain.PathFlags.Edge | Terrain.PathFlags.Ladder;
										}
									}

									if (pathfinding.flags.HasAny(Pathfinding.Flags.Climber))
									{
										path_flags |= Terrain.PathFlags.Wall | Terrain.PathFlags.Ladder;
									}


									path_flags_exclude.AddFlag(Terrain.PathFlags.Door, npc.self_hints.HasNone(NPC.SelfHints.Can_Open_Doors));

									var pos_start_base = behavior_override.position_pivot;
									//if (behavior_override.position_home.IsNotZero())
									//{
									//	pos_start_base = behavior_override.position_home;
									//}

									var pos_start = region.GetNearestPathPos(pos_start_base, include: path_flags);

									//var wander_pos = pos_start + wander_dir;			
									//region.DrawDebugCircle(wander_pos, 2.00f, color: Color32BGRA.Yellow, 1.00f);

									//WorldNotification.Push(ref region, $"{region.GetCurrentTick()}: dir", 0xffffffff, wander_pos, velocity: Vector2.Zero, lifetime: 3.00f);
									//if (region.TryGetNearestHighPathPos(ref wander_pos, include: Terrain.PathFlags.Edge | Terrain.PathFlags.Ladder | Terrain.PathFlags.Ground | Terrain.PathFlags.Wall))

									Span<Vector2> span_positions = stackalloc Vector2[16];

									var max_depth = Maths.Clamp(random.NextIntExtra(1, (int)Maths.Ceil(npc.wander_radius * 0.125f)), 1, 12);
									region.RepathWanderHigh(pos_a: pos_start,
										positions: ref span_positions,
										result: out var repath_result,
										air: pathfinding.flags.HasAny(Pathfinding.Flags.Air),
										include: path_flags,
										require: pathfinding.flags.HasAny(Pathfinding.Flags.Air) ? default : Terrain.PathFlags.Ground,
										max_depth: max_depth,
										exclude: path_flags_exclude | Terrain.PathFlags.Ladder | Terrain.PathFlags.Underwater | Terrain.PathFlags.Water);

									//App.WriteLine($"{repath_result}; {entity.GetName()}; {span_positions.Length}; {span_positions[span_positions.Length - 1]}");
									if (repath_result != Terrain.RepathResult.Unreachable)
									{
										ref var pos_end = ref span_positions[span_positions.Length - 1];
										region.TryGetNearestValidPathPos(pos: ref pos_end,
											include: end_path_flags,
											require: end_path_flags_require,
											exclude: end_path_flags_exclude,
											max_distance: 6.00f);

										//wander_pos = region.GetNearestPathPos(wander_pos, include: Terrain.PathFlags.Ground);
										//WorldNotification.Push(ref region, $"{region.GetCurrentTick()}: iter 0", 0xffffffff, pos_end, velocity: Vector2.Zero, lifetime: 3.00f);

										//region.TryGetNearestHighPathPos(ref wander_pos, include: Terrain.PathFlags.Ground);
										//WorldNotification.Push(ref region, $"{region.GetCurrentTick()}: iter 1", 0xffffffff, wander_pos, velocity: Vector2.Zero, lifetime: 3.00f);

										var found = true;


										////if (region.TryLinecast(region.GetNearestPathPos(transform.position), wander_pos, 0.00f, out var hit_result_a, mask: Physics.Layer.World, query_flags: Physics.QueryFlag.Static))
										////{
										////	wander_pos = region.GetNearestPathPos(hit_result_a.world_position + hit_result_a.normal);
										////}

										//if (!pathfinding.flags.HasAny(Pathfinding.Flags.Air))
										//{
										//	Span<Vector2> span_positions = stackalloc Vector2[16];
										//	region.RepathHigh(pos_start, wander_pos, ref span_positions, out var repath_result, include: Terrain.PathFlags.Ground | Terrain.PathFlags.Wall | Terrain.PathFlags.Ladder | Terrain.PathFlags.Edge | Terrain.PathFlags.Door);

										//	if (repath_result != Terrain.RepathResult.Unreachable)
										//	{
										//		found = true;
										//	}
										//	else
										//	{
										//		WorldNotification.Push(ref region, "Wander failed", Color32BGRA.Red, wander_pos, lifetime: 5.00f);
										//	}

										//	//if (region.TryLinecast(wander_pos, wander_pos + (new Vector2(0, 1) * 16), 0.00f, out var hit_result_b, mask: Physics.Layer.World, query_flags: Physics.QueryFlag.Static))
										//	//{
										//	//	wander_pos = region.GetNearestPathPos(hit_result_b.world_position + hit_result_b.normal);
										//	//	found = true;
										//	//}
										//}
										//else
										//{
										//	found = true;
										//}

										//WorldNotification.Push(ref region, "c", 0xffffffff, wander_pos, velocity: Vector2.Zero, lifetime: 3.00f);

										if (found)
										{
											//App.WriteLine("found");
											behavior_original.idle_timer = 0.00f;

											movement_original.position_target = pos_end;
											movement_original.type = Movement.Type.Move;
											movement_original.flags.RemoveFlag(Movement.Flags.Continuous);
											movement_original.flags.AddFlag(Movement.Flags.Force_Repath);
											control.mouse.SetPosition(pos_end);

											pathfinding_state.Reset(true);
											sync_movement = true;


											//WorldNotification.Push(ref region, "* Wanders *", Color32BGRA.Yellow, transform.position, lifetime: 1.00f);
											//pathfinding_state.Sync(entity);
										}
									}

									//WorldNotification.Push(ref region, "* Wanders *"u8, Color32BGRA.Yellow, transform.position, lifetime: 1.00f);

									behavior_original.t_next_wander = time + random.NextFloatExtra(npc.wander_interval, npc.wander_interval_extra);
								}
							}
							//else if (behavior_override.movement_flags.HasAny(MovementFlags.Hold))
							else // if (behavior_override.combat_flags.HasAny(CombatFlags.Defend | CombatFlags.Form_Groups | CombatFlags.)) //.HasAny(MovementFlags.Hold))
							{
								if (time > behavior_override.t_next_wander)
								{
									//var pos_return = Vec2f.Zero; // behavior_override.position_friends;
									var pos_return = behavior_override.position_home;

									if (behavior_override.position_goal && behavior_override.movement_flags.HasAny(MovementFlags.Hold) && !pos_return.IsInDistance(behavior_override.position_goal, 3.00f)) pos_return = behavior_override.position_goal;
									else if (behavior_override.position_friends && behavior_override.combat_flags.HasAll(CombatFlags.Attack_Visible | CombatFlags.Form_Groups) && !pos_return.IsInDistance(behavior_override.position_friends, 4.00f)) pos_return = behavior_override.position_friends;
									else if (behavior_override.position_safety && behavior_override.combat_flags.HasAny(CombatFlags.Flee) && !pos_return.IsInDistance(behavior_override.position_safety, npc.wander_radius * 0.50f)) pos_return = behavior_override.position_safety;

									//App.WriteLine("go home");

									// TODO: messy shitcode
									if (behavior_override.movement_flags.HasNone(MovementFlags.Return_Home) && pos_return == behavior_override.position_home) pos_return = Vec2f.Zero;

									if (pos_return && !Maths.IsInDistance(pos_return, transform.position, 3.00f))
									{
										//movement_original.position_target = pos_return;
										movement_original.position_target = pos_return;
										movement_original.type = Movement.Type.Move;
										movement_original.flags.RemoveFlag(Movement.Flags.Continuous);
										movement_original.flags.AddFlag(Movement.Flags.Force_Repath);

										behavior_original.idle_timer = 0.00f;
										behavior_original.t_next_wander = random.NextFloatExtra(time + npc.wander_interval, npc.wander_interval_extra);
										pathfinding_state.Reset(true);

										WorldNotification.Push(region: ref region,
											message: "* Wanders Home *"u8,
											color: Color32BGRA.Yellow,
											position: transform.position,
											lifetime: 1.00f,
											faction_id: npc.h_faction_cached);
										//pathfinding_state.Sync(entity);
									}
									else
									{
										if (behavior_original.position_friends && behavior_original.position_friends.IsInDistance(transform.position, 8.00f))
										{
											control.mouse.SetPosition(behavior_original.position_friends);
											behavior_original.t_next_wander = random.NextFloatExtra(time + npc.wander_interval, npc.wander_interval_extra);
										}
										else
										{
											control.mouse.SetPosition(region.GetNearestHighPathPos(pos: transform.position + random.NextUnitVector2Extra(1, 8), include: Terrain.PathFlags.Ground | Terrain.PathFlags.Wall | Terrain.PathFlags.Ladder | Terrain.PathFlags.Edge, exclude: Terrain.PathFlags.Solid) + random.NextUnitVector2Extra(0, 8));
											behavior_original.t_next_wander = random.NextFloatExtra(time + npc.wander_interval, npc.wander_interval_extra);
										}
									}
								}
							}

							//movement_override.type = Movement.Type.Move;
							//movement_override.position_target = movement_original.position_target;
						}

						//App.WriteLine(behavior_original.idle_timer);
						if (movement_override.type == Movement.Type.None) behavior_original.idle_timer += info.DeltaTime * random.NextFloatRange(0.80f, 3.50f);
						else behavior_original.idle_timer = 0.00f;
					}
					break;

					default:
					{

					}
					break;
				}

				if (behavior_original.type == Behavior.Type.Undefined)
				{
					behavior_original.type = Behavior.Type.Idle;
				}

				if (sync_behavior)
				{
					behavior_original.Sync(entity, true);
					//if (debug_log_sync) App.WriteLine($"synced behavior ({entity.GetName()})");
				}

				if (sync_movement)
				{
					movement_original.Sync(entity, true);
					//if (debug_log_sync) App.WriteLine($"synced movement ({entity.GetName()})");
				}
				//if (AI.debug_always_sync) // || sync)
				//{
				//	behavior_original.Sync(entity, true);
				//	//App.WriteLine("sync");
				//}
			}
#endif
		}
	}
}
