
using System.Text;

namespace TC2.Base.Components
{
	public static partial class AI
	{
		public static bool debug_paused = false;

#if RELEASE
		public const bool debug_always_sync = false;
		public const bool debug_draw_friends = false;
		public const bool debug_draw_enemies = false;
#else
		public static bool debug_always_sync = false;
		public static bool debug_draw_friends = false;
		public static bool debug_draw_enemies = false;

#if SERVER
		[ChatCommand.Global("ai.debug", "Force syncing NPC components to clients.", admin: true)]
		public static void AIDebugCommand(ref ChatCommand.Context context, bool? value = null)
		{
			AI.debug_always_sync.Toggle();
			Server.SendChatMessage(AI.debug_always_sync ? "Enabled AI debug mode." : "Disabled AI debug mode.", channel: Chat.Channel.System);
		}

		[ChatCommand.Global("ai.draw.friends", "Draw NPC friend nodes.", admin: true)]
		public static void AIDrawFriendsCommand(ref ChatCommand.Context context, bool? value = null)
		{
			AI.debug_draw_friends.Toggle();
			Server.SendChatMessage(AI.debug_draw_friends ? "Enabled AI friend node drawing." : "Disabled AI friend node drawing.", channel: Chat.Channel.System);
		}

		[ChatCommand.Global("ai.draw.enemies", "Draw NPC enemy nodes.", admin: true)]
		public static void AIDrawEnemiesCommand(ref ChatCommand.Context context, bool? value = null)
		{
			AI.debug_draw_enemies.Toggle();
			Server.SendChatMessage(AI.debug_draw_enemies ? "Enabled AI enemy node drawing." : "Disabled AI enemy node drawing.", channel: Chat.Channel.System);
		}

#endif
#endif

#if SERVER
		[ChatCommand.Global("ai.pause", "", admin: true)]
		public static void AIPauseCommand(ref ChatCommand.Context context, bool? value = null)
		{
			AI.debug_paused.Toggle();
			Server.SendChatMessage(AI.debug_paused ? "Paused AI." : "Unpaused AI.", channel: Chat.Channel.System);
		}
#endif

		[Flags]
		public enum Flags: uint
		{
			None = 0,
		}

		public enum Stance: byte
		{
			Undefined = 0,

			Passive,
			Aggressive,
			Defensive,
			Friendly,
		}

		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public partial struct Data(): IComponent, IOverridable
		{
			public AI.Flags flags;
			public AI.Stance stance;

			public float anger;
			public float fear;
			public float stress;
			public float frustration;
			public float determination;
			public float courage = 1000.00f;

			[Asset.Ignore] public Entity ent_home;
			[Net.Ignore, Save.Ignore] private float unused_01;
			[Net.Ignore, Save.Ignore] private float unused_02;
			[Net.Ignore, Save.Ignore] private Vec4f unused_03;
		}

		public static partial class Driver
		{
			[Flags]
			public enum Flags: uint
			{
				None = 0,

				Aggressive = 1 << 0,
				Passive = 1 << 1
			}

			[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
			public partial struct Data: IComponent
			{
				public Driver.Flags flags;
			}
		}

#if SERVER
		[Shitcode]
		[ChatCommand.Region("npc", "", creative: true)]
		public static void NPCCommand(ref ChatCommand.Context context, string origin_str, byte? faction_id = null)
		{
			ref var region = ref context.GetRegion();
			var random = XorRandom.New(true);

			var h_origin = new IOrigin.Handle(origin_str);

			ref var origin_data = ref h_origin.GetData(out var origin_asset);
			if (origin_data.IsNotNull())
			{
				//var h_character = Spawner.CreateCharacter(ref region, ref random, h_origin: h_origin, h_faction: faction_id ?? default, recycle: true);

				var h_character = Spawner.CreateCharacter(ref region.AsCommon(), ref random,
				h_origin: h_origin,
				h_faction: faction_id ?? origin_data.faction,
				scope: Asset.Scope.Region, asset_flags: Asset.Flags.Recycle);

				Spawner.TryGenerateKits(ref random, h_character);
				Spawner.SpawnCharacter(ref region, h_character, context.GetTargetPosition(), h_faction: faction_id ?? origin_data.faction, flags: Spawn.SpawnEvent.Flags.None).ContinueWith((ent_controlled) =>
				{
					ref var character = ref h_character.GetData();
					if (character.IsNotNull())
					{
						Loadout.Spawn(ent_controlled, character.kits, money: character.money);
					}
				});
			}
		}

		[Shitcode]
		[ChatCommand.Region("enemy", "", creative: true)]
		public static void EnemyCommand(ref ChatCommand.Context context, string origin = null, string faction = null)
		{
			ref var region = ref context.GetRegion();
			var random = XorRandom.New(true);

			var h_origin = new IOrigin.Handle(origin).OrDefault("human.srn.brigand.00");
			var h_faction = new IFaction.Handle(faction).OrDefault("srn");

			ref var origin_data = ref h_origin.GetData(out var origin_asset);
			Assert.IsNotNull(ref origin_data);

			ref var faction_data = ref h_faction.GetData(out var faction_asset);
			Assert.IsNotNull(ref faction_data);

			var h_character = Spawner.CreateCharacter(ref region.AsCommon(), ref random,
			h_origin: h_origin,
			h_faction: h_faction,
			scope: Asset.Scope.Region, asset_flags: Asset.Flags.Recycle);

			Spawner.TryGenerateKits(ref random, h_character);
			Spawner.SpawnCharacter(ref region, h_character, context.GetTargetPosition(), h_faction: origin_data.faction.OrDefault(h_faction), flags: Spawn.SpawnEvent.Flags.None).ContinueWith((ent_controlled) =>
			{
				ref var character = ref h_character.GetData();
				if (character.IsNotNull())
				{
					if (!ent_controlled.HasComponent<Animal.Data>())
					{
						Loadout.Spawn(entity: ent_controlled, kits: character.kits, money: character.money);

						ref var ai = ref ent_controlled.GetComponent<AI.Data>();
						if (ai.IsNotNull())
						{
							//ai.stance = args.ai_stance;
							ai.anger = 1000.00f;
							ai.frustration = 500.00f;
							ai.Sync(ent_controlled);
						}

						ref var gunner = ref ent_controlled.GetComponent<AI.Gunner.Data>();
						if (gunner.IsNotNull())
						{
							gunner.flags = Gunner.Flags.Aggressive | Gunner.Flags.Passive;
							gunner.Sync(ent_controlled);
						}

						ref var behavior = ref ent_controlled.GetComponent<AI.Behavior>();
						if (behavior.IsNotNull())
						{
							behavior.type = Behavior.Type.Attack;
							behavior.combat_flags |= Behavior.CombatFlags.Attack_Visible | Behavior.CombatFlags.Attack_When_Hit | Behavior.CombatFlags.Charge | Behavior.CombatFlags.Cover | Behavior.CombatFlags.Chase | Behavior.CombatFlags.Flee;
							behavior.movement_flags |= Behavior.MovementFlags.Wander | Behavior.MovementFlags.Hold | Behavior.MovementFlags.Open_Doors;
							behavior.Sync(ent_controlled);
						}
					}
				}
			});
		}
#endif

		[Query(ISystem.Scope.Region)]
		public delegate void GetAllThreatsQuery(ISystem.Info info, Entity entity,
			[Source.Owned] ref Body.Data body, [Source.Owned] in Threat.Data threat,
			[Source.Owned] in Transform.Data transform);

		[Query(ISystem.Scope.Region)]
		public delegate void GetAliveThreatsQuery(ISystem.Info info, Entity entity,
			[Source.Owned] ref Body.Data body, [Source.Owned] in Threat.Data threat,
			[Source.Owned] in Transform.Data transform, [HasTag("dead", true, Source.Modifier.Owned)] bool is_dead);


#if CLIENT
		[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("dead", false, Source.Modifier.Owned)]
		public static void DrawDebug(/*ISystem.Info info, */ref Region.Data region,
		[Source.Owned] ref NPC.Data npc, [Source.Owned] in Transform.Data transform, [Source.Owned] in Control.Data control, [Source.Owned] in Body.Data body,
		/*[Source.Owned] ref Pathfinding.Data pathfinding, */[Source.Owned] ref Pathfinding.State pathfinding_state,
		//[Source.Owned, Original] ref AI.Data ai_original, [Source.Owned, Override] ref AI.Data ai_override,
		[Source.Owned, Original] ref AI.Behavior behavior_original, [Source.Owned, Override] ref AI.Behavior behavior_override,
		[Source.Owned, Original] ref AI.Movement movement_original, [Source.Owned, Override] ref AI.Movement movement_override)
		{
			if (App.debug_mode_gui && GUI.debug_draw_pathfinding_lines)
			{
				if (Camera.IsVisible(Camera.CullType.Rect2x, transform.position))
				{
					var name = body.GetEntity().GetFullName();

					var c_pos_transform = region.WorldToCanvas(transform.GetInterpolatedPosition());
					Pathfinding.Draw(ref region.AsCommon(), ref pathfinding_state);

					GUI.DrawLine(c_pos_transform, region.WorldToCanvas(control.mouse.GetInterpolatedPosition()), Color32BGRA.Cyan);
					GUI.DrawTextCentered($"Mouse\n{name}", region.WorldToCanvas(control.mouse.GetInterpolatedPosition()), box_shadow: true, size: 10, color: Color32BGRA.Cyan);

					if (movement_override.position_target.TryGetValue(out var pos_mov_target))
					{
						GUI.DrawLine(c_pos_transform, region.WorldToCanvas(pos_mov_target), Color32BGRA.Orange);
						GUI.DrawTextCentered($"Mov.Target\n{name}", region.WorldToCanvas(pos_mov_target), box_shadow: true, size: 10, color: Color32BGRA.Orange);
					}

					if (behavior_override.position_target.TryGetValue(out var pos_beh_target))
					{
						GUI.DrawLine(c_pos_transform, region.WorldToCanvas(pos_beh_target), Color32BGRA.Yellow);
						GUI.DrawTextCentered($"Beh.Target\n{name}", region.WorldToCanvas(pos_beh_target), box_shadow: true, size: 10, color: Color32BGRA.Yellow);
					}

					if (behavior_override.position_friends.IsNotZero())
					{
						GUI.DrawLine(c_pos_transform, region.WorldToCanvas(behavior_override.position_friends), Color32BGRA.Green);
						GUI.DrawTextCentered($"Beh.Friends\n{name}", region.WorldToCanvas(behavior_override.position_friends), box_shadow: true, size: 10, color: Color32BGRA.Green);
					}

					if (behavior_override.position_enemies.IsNotZero())
					{
						GUI.DrawLine(c_pos_transform, region.WorldToCanvas(behavior_override.position_enemies), Color32BGRA.Red);
						GUI.DrawTextCentered($"Beh.Enemies\n{name}", region.WorldToCanvas(behavior_override.position_enemies), box_shadow: true, size: 10, color: Color32BGRA.Red);
					}

					if (behavior_override.position_safety.IsNotZero())
					{
						GUI.DrawLine(c_pos_transform, region.WorldToCanvas(behavior_override.position_safety), Color32BGRA.Magenta);
						GUI.DrawTextCentered($"Beh.Safety\n{name}", region.WorldToCanvas(behavior_override.position_safety), box_shadow: true, size: 10, color: Color32BGRA.Magenta);
					}

					if (movement_override.position_path.TryGetValue(out var pos_mov_path))
					{
						GUI.DrawLine(c_pos_transform, region.WorldToCanvas(pos_mov_path), Color32BGRA.Green);
						GUI.DrawTextCentered($"Path\n{name}", region.WorldToCanvas(pos_mov_path), box_shadow: true, size: 10, color: Color32BGRA.Green);
					}

					var show_extra = GUI.GetMouse().position.IsInDistance(transform.position, 4.00f);

					var sb = new StringBuilder();
					sb.AppendLine($"pf.last_results: {pathfinding_state.last_result}");
					sb.AppendLine($"pf.last_repath: {pathfinding_state.last_repath_result}");
					sb.AppendLine($"pf.retry_count: {pathfinding_state.retry_count}");
					sb.AppendLine($"{pathfinding_state.debug_ms:0.000}ms");
					if (show_extra)
					{
						//sb.AppendLine($"pf.retry_count: {pathfinding_state.retry_count}");
						//sb.AppendLine($"Idle: {behavior_original.idle_timer}");
						sb.AppendLine($"npc.self_hints: {npc.self_hints}");
						sb.AppendLine($"npc.item_hints: {npc.item_hints}");
						//sb.AppendLine($"AI: {ai_override.stance}");
						sb.AppendLine($"Beh.TargetFlags: {behavior_original.target_flags}");
						sb.AppendLine($"Beh.CombatFlags: {behavior_original.combat_flags}");
						sb.AppendLine($"Beh.MovementFlags: {behavior_original.movement_flags}");
					}
					sb.AppendLine($"Beh.Type: {behavior_original.type}");
					sb.AppendLine($"Mov.Type: {movement_original.type}");
					if (show_extra)
					{
						sb.AppendLine($"Mov.Flags: {movement_original.flags}");
						sb.AppendLine($"Mouse: {control.mouse.position:0.00} / {behavior_original.position_target:0.00}");
						//sb.AppendLine($"Keyboard: {(Keyboard.Key)control.keyboard.mask}");
						sb.AppendLine($"Target: {behavior_original.ref_target_transform.entity.GetFullName()} {behavior_original.ref_target_transform.entity}/{behavior_original.ref_target_threat.entity}/{behavior_original.ref_target_body.entity} (alive: {behavior_original.ref_target_transform.entity.IsAlive()}, prefab: {behavior_original.ref_target_transform.entity.GetPrefabName()})");
						//sb.AppendLine($"Home: {ai_original.ent_home.GetFullName()}");
					}

					GUI.DrawText(sb.ToString(), c_pos_transform + new Vector2(40, -32));

					//GUI.DrawCircle(GUI.WorldToCanvas(pathfinding_state.ta ($"{pathfinding_state.last_result}\n{pathfinding_state.last_repath_result}", GUI.WorldToCanvas(transform.GetInterpolatedPosition()));

					//draw.AddText(GUI.WorldToCanvas(transform.position), 0xffffffff, $"{npc.navigation_state}");

					//draw.AddLine(GUI.WorldToCanvas(transform.position), GUI.WorldToCanvas(control.mouse.position), 0xff00ffff);
					//draw.AddCircleFilled(GUI.WorldToCanvas(control.mouse.position), 4.00f, 0xff00ffff, 8);
					////draw.AddText(GUI.WorldToCanvas(control.mouse.position + new Vector2(0.50f, 0.50f)), 0xffffffff, $"{(Mouse.Key)control.mouse.mask}");

					////draw.AddLine(GUI.WorldToCanvas(transform.position), GUI.WorldToCanvas(npc.target_position), 
				}
			}
		}
#endif

#if SERVER
		[ISystem.VeryEarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		[HasComponent<Player.Data>(Source.Modifier.Shared, false), HasTag("dead", false, Source.Modifier.Owned)]
		public static void UpdateEmotionsA(ISystem.Info info, Entity entity, ref XorRandom random,
		[Source.Owned, Original] ref AI.Data ai_original, [Source.Owned, Override] ref AI.Data ai_override,
		[Source.Owned, Original] ref AI.Behavior behavior_original, [Source.Owned, Override] ref AI.Behavior behavior_override,
		[Source.Owned] ref NPC.Data npc, [Source.Owned] ref Control.Data control)
		{
			if (ai_original.anger > 800.00f)
			{
				behavior_override.combat_flags |= Behavior.CombatFlags.Attack_Visible | Behavior.CombatFlags.Attack_When_Hit;
			}

			//if (ai_original.anger > 1000.00f)
			//{
			//	control.mouse.SetKeyPressed(Mouse.Key.Left, random.NextBool(0.30f));
			//}

			ai_original.anger = Maths.Clamp(Maths.MoveTowards(ai_original.anger, 0.00f, ai_original.anger * 0.001f), 0.00f, 3000.00f);
			//App.WriteLine(ai_original.anger);
		}

		[ISystem.VeryLateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		[HasComponent<Player.Data>(Source.Modifier.Shared, false), HasTag("dead", false, Source.Modifier.Owned)]
		public static void UpdateEmotionsB(ISystem.Info info, Entity entity,
		[Source.Owned, Original] ref AI.Data ai_original,
		[Source.Owned] ref NPC.Data npc)
		{
			ai_original.fear = Maths.Lerp2(ai_original.fear, npc.pain * Maths.Clamp(npc.consciousness, 0.10f, 1.00f).RcpFast(), 0.001f, 0.10f);
			ai_original.determination = Maths.Lerp(ai_original.determination, 1000.00f * Maths.Clamp(npc.consciousness, 0.10f, 1.00f).RcpFast(), 0.01f);
		}

		[ISystem.EarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		[HasComponent<Player.Data>(Source.Modifier.Parent, false), HasTag("dead", false, Source.Modifier.Parent)]
		public static void UpdateDrill(ISystem.Info info, Entity entity, ref Region.Data region, ref XorRandom random,
		[Source.Owned] ref Drill.Data drill, [Source.Parent] in Transform.Data transform, [Source.Owned] ref Body.Data body,
		[Source.Parent] ref Control.Data control, [Source.Parent] ref Arm.Data arm,
		[Source.Parent] ref Pathfinding.Data pathfinding, [Source.Parent] ref Pathfinding.State pathfinding_state,
		[Source.Parent, Original] ref AI.Behavior behavior_original, [Source.Parent, Override] ref AI.Behavior behavior_override,
		[Source.Parent, Original] ref AI.Movement movement_original, [Source.Parent, Override] ref AI.Movement movement_override,
		[Source.Parent] ref NPC.Data npc)
		{
			if (AI.debug_paused) return;
			if (npc.consciousness <= 0.50f) return;

			var ts = Timestamp.Now();
			var time = info.WorldTime;

			switch (behavior_override.type)
			{
				case Behavior.Type.Attack:
				case Behavior.Type.Defend:
				{
					if (behavior_override.position_target.TryGetValue(out var pos))
					{
						//var pos = behavior_override.position_target;

						//App.WriteLine(pos);
						//var dir = (pos - transform.position).GetNormalized(out var distance);



						//var can_hit = true; // time > melee_state.next_hit && time > behavior_override.next_action;
						var yes = false;

						//control.mouse.position = behavior_override.position_target;
						//return;

						//if (distance > 1.00f && (pathfinding_state.retry_count > 0 || pathfinding_state.last_result.HasAny(Pathfinding.Results.Stuck | Pathfinding.Results.Unreachable)))
						if (pathfinding_state.points_count > 0 && (pathfinding_state.retry_count > 0 || pathfinding_state.last_result.HasAny(Pathfinding.Results.Stuck | Pathfinding.Results.Unreachable)))
						{
							var target_pos = pos;

							var start_pos = pathfinding_state.points[0]; // - (dir * 8);
							var end_pos = pos;
							//var normal = dir;

							Span<LinecastResult> hits = stackalloc LinecastResult[32];
							//if (region.TryLinecastAll(transform.position + (-dir), transform.position + (dir * 3), 3.50f, ref hits, mask: Physics.Layer.World, query_flags: Physics.QueryFlag.Static))
							if (region.TryLinecastAll(start_pos, end_pos, 0.00f, ref hits, mask: Physics.Layer.World, query_flags: Physics.QueryFlag.Static))
							{
								var nearest_alpha = 1000.00f;

								for (var i = 0; i < hits.Length; i++)
								{
									ref var hit = ref hits[i];
									if (hit.alpha < nearest_alpha)
									{
										nearest_alpha = hit.alpha;
										target_pos = hit.world_position;
										//normal = hit.normal;
										yes = true;

										//break;
									}

#if SERVER
									//WorldNotification.Push(ref region, $"{i}/{hits.Length}", Color32BGRA.Red, hit.world_position);
#endif

								}

								//control.mouse.position = Vector2.Lerp(control.mouse.position, target_pos, 0.25f);

								//#if SERVER
								//							WorldNotification.Push(ref region, "a", Color32BGRA.Red, start_pos);
								//							WorldNotification.Push(ref region, "b", Color32BGRA.Red, end_pos);
								//							//WorldNotification.Push(ref region, "target", Color32BGRA.Red, target_pos);
								//#endif

								//App.WriteLine(nearest_alpha);
							}

							var nearest_node = region.GetNearestPathPos(movement_override.position_legs);
							if (yes) //Vector2.Distance(transform.position, nearest_node) > 1.00f)
							{
								//target_pos = transform.position + ((dir * 4).RotateByRad(MathF.Sin(info.WorldTime * 8.00f)) * 2); // Vector2.Lerp(control.mouse.position, pos, 0.50f);
								control.mouse.SetPosition(target_pos); // transform.position + ((dir * 4).RotateByRad(MathF.Sin(info.WorldTime * 1.50f) * 1.10f) * 1);
																	   //movement_override.position_target = target_pos - dir;
																	   //movement_override.type = Movement.Type.Straight;
																	   //movement_override.position_target = Vector2.Lerp(nearest_node, target_pos, 0.10f); // - (target_pos - transform.position).GetNormalized();
								movement_override.position_target = target_pos; // + (normal * 0.250f); // movement_original.position_target - ((transform.position - target_pos).GetNormalized() * 0.50f); // Vector2.Lerp(nearest_node, target_pos, 0.80f); // - (target_pos - transform.position).GetNormalized();
								movement_override.type = Movement.Type.Straight;

								//if (random.NextBool(0.01f))
								//{
								//	pathfinding_state.retry_count = 0;
								//	pathfinding_state.last_result = default;
								//}
							}
							else
							{
								//App.WriteLine("reset");

								control.mouse.position = Vector2.Lerp(control.mouse.position, target_pos, 0.20f);
								//pathfinding_state.Reset();
							}

							//movement_override.position_target = transform.position + (dir * distance);
							//movement_override.type = Movement.Type.Straight;


							//App.WriteLine(control.mouse.mask);

							control.mouse.SetKeyPressed(Mouse.Key.Left, true);
							//App.WriteLine(control.mouse.mask);
							//App.WriteLine("yes");
							//movement_override.flags.SetFlag(AI.Movement.Flags.No_Mouse, true);
						}
						else
						{
							if (behavior_override.ref_target_body.IsAlive())
							{
								ref var target_transform = ref behavior_override.ref_target_transform.GetComponent();
								if (!target_transform.IsNull())
								{
									pos = target_transform.LocalToWorldNoScale(behavior_override.position_target_local);
								}

								var dir = (pos - transform.position).GetNormalized(out var distance);
								movement_override.position_target = pos;

								if (distance <= drill.max_distance * 12.00f) // || (distance < 12.00f && region.IsInLineOfSight(pos, transform.position)))
								{
									npc.avoid_distance = new Vector2(0.25f, 0);
									npc.idle_distance = new Vector2(drill.max_distance * 0.80f, 2);

									control.mouse.position = Vector2.Lerp(control.mouse.position, pos, 0.50f);
									movement_override.flags.SetFlag(AI.Movement.Flags.No_Mouse, true);
									control.mouse.SetKeyPressed(Mouse.Key.Left, true);
								}
							}
						}
					}
				}
				break;

				case Behavior.Type.Move:
				{
					//if (npc.stuc)
					if (npc.self_hints.HasAny(NPC.SelfHints.Is_Door_Stuck | NPC.SelfHints.Is_Stuck) && pathfinding_state.last_result.HasAny(Pathfinding.Results.LOS_Blocked | Pathfinding.Results.Stuck))
					{
						control.mouse.SetKeyPressed(Mouse.Key.Left, true);
					}
				}
				break;
			}

			//App.WriteLine($"{ts.GetMilliseconds():0.0000} ms");
		}

		[ISystem.EarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		[HasComponent<Player.Data>(Source.Modifier.Parent, false), HasTag("dead", false, Source.Modifier.Parent)]
		public static void UpdateChainsaw(ISystem.Info info, /*Entity entity, ref Region.Data region,*/
		[Source.Owned] ref Chainsaw.Data saw, [Source.Parent] in Transform.Data transform, [Source.Owned] ref Body.Data body,
		[Source.Parent] ref Control.Data control, [Source.Parent] ref Arm.Data arm,
		[Source.Parent] ref Pathfinding.Data pathfinding, [Source.Parent] ref Pathfinding.State pathfinding_state,
		/*[Source.Parent, Original] ref AI.Behavior behavior_original, */[Source.Parent, Override] ref AI.Behavior behavior_override,
		/*[Source.Parent, Original] ref AI.Movement movement_original, */[Source.Parent, Override] ref AI.Movement movement_override,
		[Source.Parent] ref NPC.Data npc)
		{
			if (AI.debug_paused) return;

			switch (behavior_override.type)
			{
				case Behavior.Type.Attack:
				case Behavior.Type.Defend:
				{
					if (behavior_override.position_target.TryGetValue(out var pos))
					{
						var dir = (pos - transform.position).GetNormalized(out var distance);
						//movement_override.position_target = pos;

						if (distance <= (2.00f + saw.max_distance) * 4.00f)
						{
							control.mouse.position = Vector2.Lerp(control.mouse.position, pos, 0.50f);
							movement_override.flags.SetFlag(AI.Movement.Flags.No_Mouse, true);
							control.mouse.SetKeyPressed(Mouse.Key.Left, true);
						}
						
					}
					//else if (npc.self_hints.HasAny(NPC.SelfHints.Is_Door_Stuck))
					//{
					//	control.mouse.SetKeyPressed(Mouse.Key.Left, true);
					//}
				}
				break;

				case Behavior.Type.Move:
				{
					//if (npc.stuc)
					//if (npc.self_hints.HasAny(NPC.SelfHints.Is_Door_Stuck | NPC.SelfHints.Is_Stuck | NPC.SelfHints.l))
					//{
					//	control.mouse.SetKeyPressed(Mouse.Key.Left, true);
					//	movement_override.flags.SetFlag(AI.Movement.Flags.No_Mouse, true);
					//	if (movement_override.position_stuck.HasValue) control.mouse.position = movement_override.position_stuck.Value; // + movement_override.position_path
					//}
				}
				break;
			}

			//App.WriteLine($"{ts.GetMilliseconds():0.0000} ms");
		}

		[ISystem.EarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		[HasComponent<Player.Data>(Source.Modifier.Parent, false), HasTag("dead", false, Source.Modifier.Parent), HasComponent<Arm.Data>(Source.Modifier.Parent, true)]
		public static void UpdateMelee(ISystem.Info info, ref XorRandom random, ref Region.Data region,
		[Source.Owned] ref Melee.Data melee, [Source.Owned] ref Melee.State melee_state,
		[Source.Owned] in Transform.Data transform,
		[Source.Parent] ref Control.Data control,
		[Source.Parent, Original] ref AI.Behavior behavior_original, [Source.Parent, Override] ref AI.Behavior behavior_override,
		[Source.Parent, Override] ref AI.Movement movement_override,
		[Source.Parent] ref NPC.Data npc)
		{
			if (AI.debug_paused) return;
			if (npc.consciousness <= 0.50f) return;

			var ts = Timestamp.Now();
			var time = info.WorldTime;
			var invert_key = melee.flags.HasAny(Melee.Flags.Use_RMB);
			var key = invert_key ? Mouse.Key.Right : Mouse.Key.Left;

			switch (behavior_override.type)
			{
				case Behavior.Type.Attack:
				case Behavior.Type.Defend:
				{
					if (time >= behavior_original.t_next_update && behavior_override.position_target.TryGetValue(out var pos))
					{
						//var pos = behavior_override.position_target;
						var pos_melee = transform.LocalToWorld(melee.hit_offset);


						ref var target_body = ref behavior_override.ref_target_body.GetComponent();
						if (target_body.IsNotNull())
						{
							pos = target_body.GetClosestPoint(pos_melee, allow_inside: false).world_position;
						}
						else
						{
							ref var target_transform = ref behavior_override.ref_target_transform.GetComponent();
							if (target_transform.IsNotNull())
							{
								pos = target_transform.LocalToWorld(behavior_override.position_target_local);
							}
						}

						var dir = (pos - pos_melee).GetNormalized(out var distance);
						movement_override.position_target = pos;
						//movement_override.type = Movement.Type.Move;

						var can_hit = npc.item_hints.HasAny(NPC.ItemHints.Fast) || (time >= melee_state.t_next_hit && time >= behavior_override.t_next_action);
						if (behavior_override.combat_flags.HasAny(AI.Behavior.CombatFlags.Incapacitate))
						{
							can_hit &= !behavior_original.target_self_hints.HasAny(NPC.SelfHints.Is_Doomed | NPC.SelfHints.Is_Stunned | NPC.SelfHints.Is_Incapacitated) || behavior_original.target_self_hints.HasAny(NPC.SelfHints.Has_Weapon | NPC.SelfHints.Can_Attack_Melee | NPC.SelfHints.Is_Dangerous | NPC.SelfHints.Can_Punch);
						}

						//if (can_hit && distance <= melee.max_distance * 2.00f)
						//{
						//	control.mouse.position = Vector2.Lerp(control.mouse.position, pos, 0.70f);
						//	movement_override.flags.SetFlag(AI.Movement.Flags.No_Mouse, true);
						//}

						//if (can_hit && distance <= melee.max_distance * 0.90f)
						//{
						//	control.mouse.SetKeyPressed(key, true);
						//	behavior_original.next_action = info.WorldTime + random.NextFloatRange(melee.cooldown, melee.cooldown + 0.10f);
						//}

						//region.DrawDebugLine(pos, pos_melee, color: Color32BGRA.Yellow, 3);


						npc.avoid_distance = new Vector2(melee.max_distance * 1.40f, 0);
						npc.idle_distance = new Vector2(melee.max_distance, 4);
						control.mouse.position = Vector2.Lerp(control.mouse.position, pos, 0.70f);

						var is_near = distance <= 3.00f;

						if (!invert_key) control.mouse.SetKeyPressed(Mouse.Key.Right, is_near);

						//control.mouse.SetKeyPressed(key, false);
						if (can_hit)
						{
							//App.WriteLine(behavior_override.target_self_hints);

							//if (distance <= melee.max_distance * 2.00f)
							//{
							//	movement_override.flags.SetFlag(AI.Movement.Flags.No_Mouse, true);
							//}

							if (distance <= melee.max_distance * 0.90f)
							{
								control.mouse.SetKeyPressed(key, true);
								behavior_original.t_next_action = time + random.NextFloatRange(melee.cooldown, melee.cooldown * 1.20f);
							}
						}
						else if (distance <= melee.max_distance * 1.50f)
						{
							//control.mouse.SetKeyPressed(key, false);

							npc.avoid_distance = new Vector2(melee.max_distance * 4.00f * Maths.Clamp((behavior_original.t_next_action - time) * 2.00f, 0.75f, 5.00f), 1);
							npc.idle_distance = new Vector2(melee.max_distance * 4.50f, 2);
						}

					}
				}
				break;

				case Behavior.Type.Harvest:
				{
					if (time >= behavior_original.t_next_update && behavior_override.position_target.TryGetValue(out var pos))
					{
						//var pos = behavior_override.position_target;
						var valid = false;

						//ref var target_body = ref behavior_override.ref_target_body.GetValueOrNullRef();
						//if (!target_body.IsNull())
						//{
						//	pos = target_body.GetClosestPoint(pos_melee).world_position;
						//}

						ref var target_transform = ref behavior_override.ref_target_transform.GetComponent();
						if (!target_transform.IsNull())
						{
							pos = target_transform.LocalToWorldNoScale(behavior_override.position_target_local);
							valid = true;
						}

						if (valid)
						{
							var pos_melee = transform.LocalToWorld(melee.hit_offset);
							var dir = (pos - pos_melee).GetNormalized(out var distance);
							movement_override.position_target = pos;
							movement_override.type = Movement.Type.Move;

							npc.avoid_distance = new Vector2(melee.max_distance * 0.30f, 0);
							npc.idle_distance = new Vector2(melee.max_distance * 0.80f, 3);

							if (distance <= melee.max_distance * 2.00f)
							{
								control.mouse.position = Vector2.Lerp(control.mouse.position, pos, 0.50f);

								//var is_los = region.IsInLineOfSight(pos_melee, pos, mask: Physics.Layer.World, query_flags: Physics.QueryFlag.Static);

								if (distance <= melee.max_distance * 0.90f)
								{
									if (time > melee_state.t_next_hit)
									{
										control.mouse.SetKeyPressed(key, true);
									}
								}
							}
						}
					}
				}
				break;

				case Behavior.Type.Move:
				case Behavior.Type.Idle:
				{
					//control.mouse.position = Vector2.Lerp(control.mouse.position, transform.position + (transform.GetDirection() * 1.50f), 0.50f);


					//App.WriteLine("melee idle");
				}
				break;
			}

			//App.WriteLine($"{ts.GetMilliseconds():0.0000} ms");
		}

		[ISystem.EarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		[HasComponent<Player.Data>(Source.Modifier.Shared, false), HasTag("dead", false, Source.Modifier.Shared)]
		public static void UpdatePuncher(ISystem.Info info, Entity entity, ref XorRandom random, ref Region.Data region,
		[Source.Owned, Original] ref Puncher.Data puncher, [Source.Owned] in Joint.Base joint_base, [Source.Owned] ref Arm.Data arm,
		[Source.Shared] ref Control.Data control, [Source.Shared] in Transform.Data transform, [Source.Shared] ref Body.Data body,
		[Source.Shared, Original] ref AI.Behavior behavior_original, [Source.Shared, Override] ref AI.Behavior behavior_override,
		[Source.Shared, Original] ref AI.Movement movement_original, [Source.Shared, Override] ref AI.Movement movement_override,
		[Source.Shared] ref NPC.Data npc)
		{
			if (AI.debug_paused) return;
			//App.WriteLine("puncher");

			var can_punch = puncher.flags.HasAny(Puncher.Flags.Can_Punch) && joint_base.state.HasNone(Joint.State.Attached | Joint.State.Attaching) && npc.consciousness >= 0.50f && ((info.WorldTime - puncher.last_update) <= 0.30f);
			npc.self_hints.SetFlag(NPC.SelfHints.Can_Punch, can_punch);

			if (can_punch)
			{
				var ts = Timestamp.Now();
				var time = info.WorldTime;

				//App.WriteLine(behavior_override.type);

				switch (behavior_override.type)
				{
					case Behavior.Type.Attack:
					case Behavior.Type.Defend:
					{
						if (time >= behavior_original.t_next_update && behavior_override.position_target.TryGetValue(out var pos))
						{
							//App.WriteLine(behavior_override.type);

							//var pos = behavior_override.position_target;

							ref var target_body = ref behavior_override.ref_target_body.GetComponent();
							if (target_body.IsNotNull())
							{
								pos = target_body.GetClosestPoint(transform.position, allow_inside: true).world_position;
							}
							else
							{
								ref var target_transform = ref behavior_override.ref_target_transform.GetComponent();
								if (target_transform.IsNotNull())
								{
									pos = target_transform.LocalToWorldNoScale(behavior_override.position_target_local);
								}
							}

							var dir = (pos - transform.position).GetNormalized(out var distance);
							movement_override.position_target = pos;
							//movement_override.type = Movement.Type.Move;

							var can_hit = time > puncher.next_attack && time > behavior_override.t_next_action;

							if (can_hit && distance <= puncher.max_distance * 2.00f)
							{
								control.mouse.position = Vector2.Lerp(control.mouse.position, pos, 0.50f);
								movement_override.flags.SetFlag(AI.Movement.Flags.No_Mouse, true);
							}

							if (can_hit && distance <= puncher.max_distance * 3.00f)
							{
								control.mouse.SetKeyPressed(Mouse.Key.Right, true);
								behavior_original.t_next_action = info.WorldTime + random.NextFloatRange(puncher.cooldown, puncher.cooldown + 0.10f);
							}

							if (distance <= puncher.max_distance * 1.50f)
							{
								npc.avoid_distance = new Vector2(puncher.max_distance * 0.10f, 0);
								npc.idle_distance = new Vector2(puncher.max_distance * 0.80f, 2);
							}

							if (can_hit)
							{
								if (distance <= puncher.max_distance * 2.00f)
								{
									control.mouse.position = Vector2.Lerp(control.mouse.position, pos, 0.50f);
									movement_override.flags.SetFlag(AI.Movement.Flags.No_Mouse, true);
								}

								if (distance <= puncher.max_distance * 1.50f)
								{
									control.mouse.SetKeyPressed(Mouse.Key.Right, true);
									behavior_original.t_next_action = info.WorldTime + random.NextFloatRange(puncher.cooldown, puncher.cooldown + 0.10f);
								}
							}
							else
							{
								npc.avoid_distance = new Vector2(puncher.max_distance * 0.50f, 0);
								npc.idle_distance = new Vector2(puncher.max_distance, 2);
							}
						}
					}
					break;
				}

				//App.WriteLine($"{ts.GetMilliseconds():0.0000} ms");
			}
		}

#if SERVER
		//// TODO: shithack workaround, since events aren't propagated through IsA relationship
		//[ISystem.PostUpdate.F(ISystem.Mode.Single, ISystem.Scope.Region, interval: 0.50f)]
		//[HasComponent<Player.Data>(Source.Modifier.Shared, true), HasTag("dead", false, Source.Modifier.Shared)]
		//public static void OnUpdateReset(ISystem.Info info, Entity entity,
		//[Source.Owned, Original] ref AI.Behavior behavior_original,
		//[Source.Owned, Original] ref AI.Movement movement_original,
		//[Source.Owned] ref Pathfinding.State pathfinding_state)
		//{
		//	if (behavior_original.type != Behavior.Type.Idle || behavior_original.ent_target != default || movement_original.position_target.HasValue)
		//	{
		//		App.WriteLine($"NPC: OnUpdateReset {entity.GetName()}");

		//		behavior_original.Reset();
		//		movement_original.Reset();
		//		pathfinding_state.Reset();
		//	}
		//}

		//[ISystem.Event<Character.SetPlayerEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
		//public static void OnSetPlayer(ISystem.Info info, Entity entity, ref Character.SetPlayerEvent data,
		//[Source.Owned, Original] ref AI.Behavior behavior_original,
		//[Source.Owned, Original] ref AI.Movement movement_original,
		//[Source.Owned] ref Pathfinding.State pathfinding_state)
		//{
		//	App.WriteLine($"NPC: OnSetPlayer {entity.GetName()}; {data.h_player_old}; {data.h_player_new}");

		//	behavior_original.Reset();
		//	movement_original.Reset();
		//	pathfinding_state.Reset();
		//}

		//[ISystem.Event<Character.SetPlayerEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
		//public static void OnSetPlayer2(ISystem.Info info, Entity entity, ref Character.SetPlayerEvent data,
		//[Source.Owned] ref Character.Data character)
		//{
		//	App.WriteLine($"NPC: OnSetPlayer2 {entity.GetName()}; {data.h_player_old}; {data.h_player_new}");
		//}

		//[ISystem.Event<Character.SetPlayerEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
		//public static void OnSetPlayer3(ISystem.Info info, Entity entity, ref Character.SetPlayerEvent data,
		//[Source.Shared] ref Character.Data character, [Source.Owned] ref Pathfinding.State pathfinding_state)
		//{
		//	App.WriteLine($"NPC: OnSetPlayer3 {entity.GetName()}; {data.h_player_old}; {data.h_player_new}");
		//}

		//[ISystem.Event<Character.DeathEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
		//public static void OnDeath(ISystem.Info info, Entity entity, ref Character.DeathEvent data,
		//[Source.Owned, Original] ref AI.Behavior behavior_original,
		//[Source.Owned, Original] ref AI.Movement movement_original,
		//[Source.Owned] ref Pathfinding.State pathfinding_state)
		//{
		//	App.WriteLine($"NPC: OnDeath {entity.GetName()}");

		//	behavior_original.Reset();
		//	movement_original.Reset();
		//	pathfinding_state.Reset();
		//}

		//[ISystem.Add(ISystem.Mode.Single, ISystem.Scope.Region)] //, HasComponent<Player.Data>(Source.Modifier.Shared, true)]
		//public static void OnAddPlayer1(ISystem.Info info, Entity entity, Entity ent_player,
		//[Source.Shared] ref Player.Data player,
		//[Source.Owned, Original] ref AI.Behavior behavior_original, 
		//[Source.Owned, Original] ref AI.Movement movement_original,
		//[Source.Owned] ref Pathfinding.State pathfinding_state)
		//{
		//	App.WriteLine($"NPC: OnAddPlayer1 {entity.GetName()}; {ent_player.GetName()}");

		//	behavior_original.Reset();
		//	movement_original.Reset();
		//	pathfinding_state.Reset();
		//}

		//[ISystem.Add(ISystem.Mode.Single, ISystem.Scope.Region), HasComponent<Player.Data>(Source.Modifier.Shared, false)]
		//public static void OnAddPlayer2(ISystem.Info info, Entity entity,
		//[Source.Owned, Original] ref AI.Behavior behavior_original, 
		//[Source.Owned, Original] ref AI.Movement movement_original,
		//[Source.Owned] ref Pathfinding.State pathfinding_state)
		//{
		//	App.WriteLine($"NPC: OnAddPlayer2 {entity.GetName()}");

		//	behavior_original.Reset();
		//	movement_original.Reset();
		//	pathfinding_state.Reset();
		//}
#endif


		//[ISystem.VeryEarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region)] //, interval: 0.16f)]
		//[HasTag("dead", false, Source.Modifier.Shared), HasComponent<Arm.Data>(Source.Modifier.Owned, true)]
		//public static void UpdateJoint(ISystem.Info info,
		//[Source.Shared] ref NPC.Data npc, [Source.Owned] ref Joint.Base joint_base)
		//{
		//	npc.item_hints = joint_base.item_hints;
		//}

#if SERVER
		//[ISystem.Event<Character.HitEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
		//public static void OnHitEvent(ISystem.Info info, Entity entity, ref Character.HitEvent data,
		//[Source.Owned, Original] ref AI.Behavior behavior_original, [Source.Owned, Override] ref AI.Behavior behavior_override,
		//[Source.Owned, Original] ref NPC.Data npc_original, [Source.Owned, Override] ref NPC.Data npc_override)
		//{
		//	App.WriteLine($"OnHitEvent({entity})", App.Color.Magenta);
		//}

		public static float CalculateItemHeuristic(NPC.ItemHints item_hints, NPC.SelfHints self_hints, AI.Behavior.CombatFlags combat_flags, 
		AI.Behavior.TargetFlags target_flags, NPC.ItemHints target_item_hints, NPC.SelfHints target_self_hints, float distance,
		NPC.ItemHints item_hints_prefer = default)
		{
			var score = 1.00f;
			var has_target = !target_flags.IsEmpty();

			if (item_hints.HasNone(NPC.ItemHints.Usable | NPC.ItemHints.Consumable)) return -150.00f; // Skip unusable items
			if (self_hints.HasAny(NPC.SelfHints.Is_Climbing | NPC.SelfHints.Is_Swimming | NPC.SelfHints.Is_Stunned | NPC.SelfHints.Is_Fleeing | NPC.SelfHints.Is_Stuck) & item_hints.HasAny(NPC.ItemHints.Clumsy | NPC.ItemHints.Long)) return -50.00f;
			if (self_hints.HasAnyExcept(NPC.SelfHints.Is_Climbing | NPC.SelfHints.Is_Stuck, NPC.SelfHints.Has_LOS) & item_hints.HasNone(NPC.ItemHints.Small)) score -= 5.00f;
			if (self_hints.HasNone(NPC.SelfHints.Is_Grounded | NPC.SelfHints.Has_LOS) & item_hints.HasAny(NPC.ItemHints.Clumsy)) score -= 5.00f;

			if (item_hints.HasAny(NPC.ItemHints.Melee))
			{
				var amount = 0.00f;

				if (target_self_hints.HasAnyExcept(NPC.SelfHints.Is_Immune_Bullet, NPC.SelfHints.Is_Immune_Melee))
				{
					score += 100.00f;
				}

				if (item_hints.HasNone(NPC.ItemHints.Gun | NPC.ItemHints.Long_Range | NPC.ItemHints.Armor | NPC.ItemHints.Defensive) && target_flags.HasAny(Behavior.TargetFlags.Unreachable | Behavior.TargetFlags.In_Air))
				{
					return -100.00f;
				}
				//else if (has_target)
				//{
				//	amount += Maths.InvLerp01(10.00f, 1.00f, distance) * 8.00f;
				//}

				var count_negative = 0.00f;
				count_negative += target_item_hints.GetCount(NPC.ItemHints.Gun | NPC.ItemHints.Advanced | NPC.ItemHints.Heavy | NPC.ItemHints.Dangerous | NPC.ItemHints.Destructive | NPC.ItemHints.Melee);
				count_negative += target_item_hints.GetCount(NPC.ItemHints.Explosive | NPC.ItemHints.Long_Range | NPC.ItemHints.Powerful, 3.00f);
				count_negative += target_self_hints.GetCount(NPC.SelfHints.Can_Fly | NPC.SelfHints.Can_Attack_Ranged | NPC.SelfHints.Can_Walk | NPC.SelfHints.Can_Punch);
				count_negative += target_self_hints.GetCount(NPC.SelfHints.In_Vehicle | NPC.SelfHints.Is_Crowded | NPC.SelfHints.Is_Dangerous, 2.00f);
				count_negative += self_hints.GetCount(NPC.SelfHints.Is_Doomed | NPC.SelfHints.Is_Idle | NPC.SelfHints.Is_Fleeing | NPC.SelfHints.Is_Vulnerable | NPC.SelfHints.Is_Injured);

				var count_positive = 0.00f;
				count_positive += target_item_hints.GetCount(NPC.ItemHints.Short_Range | NPC.ItemHints.Crappy | NPC.ItemHints.Wrecked | NPC.ItemHints.Healing | NPC.ItemHints.Food | NPC.ItemHints.Dumb | NPC.ItemHints.Civilian | NPC.ItemHints.Consumable | NPC.ItemHints.Slow | NPC.ItemHints.Weak | NPC.ItemHints.Inaccurate | NPC.ItemHints.Primitive | NPC.ItemHints.Armor | NPC.ItemHints.Defensive | NPC.ItemHints.Junk | NPC.ItemHints.Heavy);
				count_positive += target_self_hints.GetCount(NPC.SelfHints.Is_Doomed | NPC.SelfHints.Is_Injured | NPC.SelfHints.Is_Vulnerable | NPC.SelfHints.Is_Stuck | NPC.SelfHints.Is_Incapacitated | NPC.SelfHints.Is_Stunned);
				count_positive += self_hints.GetCount(NPC.SelfHints.Can_Attack_Melee | NPC.SelfHints.Has_Armor | NPC.SelfHints.Is_Dangerous | NPC.SelfHints.Is_Armored | NPC.SelfHints.Can_Climb);

				amount -= count_negative * 0.25f;
				amount += count_positive * 0.50f;

				if (has_target)
				{
					amount *= Maths.InvLerp01(48.00f, 4.00f, distance) * 1.50f;
				}

				if (amount > 0.00f)
				{
					amount += amount * combat_flags.GetCount(Behavior.CombatFlags.Charge | Behavior.CombatFlags.Chase, 0.20f);


					//if (distance > 6.00f)
					//{
					//	amount *= 0.50f;
					//}

				}

				score += amount;

				if (combat_flags.HasAny(AI.Behavior.CombatFlags.Prefer_Melee)) score += 100.00f;


				//score *= Maths.Mulpo(count_negative, )
			}

			if (item_hints.HasAny(NPC.ItemHints.Gun))
			{
				var mult = 1.00f;

				if (has_target)
				{
					if (target_self_hints.HasNone(NPC.SelfHints.Is_Immune_Bullet))
					{
						return -200.00f;
					}

					mult += target_flags.GetCount(Behavior.TargetFlags.In_Air | Behavior.TargetFlags.Unreachable | Behavior.TargetFlags.Dangerous | Behavior.TargetFlags.Ranged | Behavior.TargetFlags.Melee | Behavior.TargetFlags.Overpowered, 0.20f);

					if (item_hints.HasAny(NPC.ItemHints.Long_Range))
					{
						mult *= 1.00f + (Maths.InvLerp(20.00f, 40.00f, distance) * 2.50f);
					}

					if (item_hints.HasAny(NPC.ItemHints.Short_Range))
					{
						mult *= 1.00f + (Maths.InvLerp01(25.00f, 5.00f, distance) * 2.00f);
					}
				}
				else if (score > 0.00f)
				{
					score += 5.00f;
					mult *= 1.20f; // Prefer guns by default
				}
				//if (target_flags.HasAny(Behavior.TargetFlags.In_Air | Behavior.TargetFlags.Unreachable | Behavior.TargetFlags.))
				//score += Maths.InvLerp01(15.00f, 5.00f, distance) * 4.00f;
				score *= mult;

				if (combat_flags.HasAny(AI.Behavior.CombatFlags.Prefer_Ranged)) score += 100.00f;

				//App.WriteLine(Maths.InvLerp01(9.00f, 3.00f, distance) * 4.00f);
				//App.WriteLine(mult);
			}

			if (!has_target && !self_hints.HasAny(NPC.SelfHints.Is_Idle) && item_hints.HasAny(NPC.ItemHints.Small))
			{
				score *= 1.75f; // Prefer small items if no target
			}

			if (item_hints.HasAny(NPC.ItemHints.Weak)) // Avoid weak items
			{
				score -= 0.20f;
				score *= 0.40f;
			}
			else if (item_hints.HasAny(NPC.ItemHints.Powerful)) // Prefer powerful items
			{
				score += 5.00f;
				score *= 1.50f;
			}

			if (!has_target && item_hints.HasAny(NPC.ItemHints.Heavy)) // Avoid cumbersome items
			{
				score *= 0.75f;
				score -= 3.00f;
			}

			//if (self_hints.HasAny(NPC.SelfHints.Is_Stuck) && item_hints.HasNone(NPC.ItemHints.Small))
			//{
			//	score -= 100.00f;
			//}

			// Prefer defensive items if hurt
			score += item_hints.GetCount(NPC.ItemHints.Defensive | NPC.ItemHints.Armor, 2.50f) * self_hints.GetCount(NPC.SelfHints.Is_Doomed | NPC.SelfHints.Is_Fleeing | NPC.SelfHints.Is_Incapacitated | NPC.SelfHints.Is_Stunned | NPC.SelfHints.Is_Injured, 10.00f);

			//if (self_hints.HasAny(NPC.SelfHints.Is_Doomed | NPC.SelfHints.Is_Fleeing | NPC.SelfHints.Is_Vulnerable | NPC.SelfHints.Is_Injured) && item_hints.HasAny(NPC.ItemHints.Defensive | NPC.ItemHints.Armor))
			//{
			//	score += 50.00f * item_hints.GetCount(NPC.ItemHints.Defensive | NPC.ItemHints.Armor);
			//}

			return score;
		}

		public static float CalculateItemHeuristic_B(NPC.ItemHints item_hints, NPC.SelfHints self_hints, AI.Behavior.CombatFlags combat_flags, AI.Behavior.TargetFlags target_flags, NPC.ItemHints target_item_hints, NPC.SelfHints target_self_hints, float distance)
		{
			var score = 1.00f;
			var has_target = !target_flags.IsEmpty();

			if (item_hints.HasNone(NPC.ItemHints.Usable | NPC.ItemHints.Consumable)) return -150.00f; // Skip unusable items
			if (self_hints.HasAnyExcept(NPC.SelfHints.Is_Climbing | NPC.SelfHints.Is_Swimming | NPC.SelfHints.Is_Stunned | NPC.SelfHints.Is_Fleeing | NPC.SelfHints.Is_Stuck, NPC.SelfHints.Has_LOS | NPC.SelfHints.Is_Aiming | NPC.SelfHints.Is_Reloading) & item_hints.HasAny(NPC.ItemHints.Clumsy | NPC.ItemHints.Long)) return -50.00f;
			if (self_hints.HasAnyExcept(NPC.SelfHints.Is_Climbing | NPC.SelfHints.Is_Stuck, NPC.SelfHints.Has_LOS) & item_hints.HasNone(NPC.ItemHints.Small)) score -= 5.00f;
			if (self_hints.HasNone(NPC.SelfHints.Is_Grounded | NPC.SelfHints.Has_LOS) & item_hints.HasAny(NPC.ItemHints.Clumsy)) score -= 5.00f;

			if (item_hints.HasAny(NPC.ItemHints.Melee))
			{
				var amount = 0.00f;

				if (target_self_hints.HasAnyExcept(NPC.SelfHints.Is_Immune_Bullet, NPC.SelfHints.Is_Immune_Melee))
				{
					score += 100.00f;
				}

				if (item_hints.HasNone(NPC.ItemHints.Gun | NPC.ItemHints.Long_Range | NPC.ItemHints.Armor | NPC.ItemHints.Defensive | NPC.ItemHints.Ranged) && target_flags.HasAny(Behavior.TargetFlags.Unreachable | Behavior.TargetFlags.In_Air))
				{
					score -= 100.00f;
				}
				//else if (has_target)
				//{
				//	amount += Maths.InvLerp01(10.00f, 1.00f, distasnce) * 8.00f;
				//}

				var count_negative = 0.00f;
				count_negative += target_item_hints.GetCount(NPC.ItemHints.Gun | NPC.ItemHints.Advanced | NPC.ItemHints.Heavy | NPC.ItemHints.Dangerous | NPC.ItemHints.Destructive | NPC.ItemHints.Melee);
				count_negative += target_item_hints.GetCount(NPC.ItemHints.Explosive | NPC.ItemHints.Long_Range | NPC.ItemHints.Powerful, 3.00f);
				count_negative += target_self_hints.GetCount(NPC.SelfHints.Can_Fly | NPC.SelfHints.Can_Attack_Ranged | NPC.SelfHints.Can_Walk | NPC.SelfHints.Can_Punch);
				count_negative += target_self_hints.GetCount(NPC.SelfHints.In_Vehicle | NPC.SelfHints.Is_Crowded | NPC.SelfHints.Is_Dangerous, 2.00f);
				count_negative += self_hints.GetCount(NPC.SelfHints.Is_Doomed | NPC.SelfHints.Is_Idle | NPC.SelfHints.Is_Fleeing | NPC.SelfHints.Is_Vulnerable | NPC.SelfHints.Is_Injured);

				var count_positive = 0.00f;
				count_positive += target_item_hints.GetCount(NPC.ItemHints.Short_Range | NPC.ItemHints.Crappy | NPC.ItemHints.Wrecked | NPC.ItemHints.Healing | NPC.ItemHints.Food | NPC.ItemHints.Dumb | NPC.ItemHints.Civilian | NPC.ItemHints.Consumable | NPC.ItemHints.Slow | NPC.ItemHints.Weak | NPC.ItemHints.Inaccurate | NPC.ItemHints.Primitive | NPC.ItemHints.Armor | NPC.ItemHints.Defensive | NPC.ItemHints.Junk | NPC.ItemHints.Heavy);
				count_positive += target_self_hints.GetCount(NPC.SelfHints.Is_Doomed | NPC.SelfHints.Is_Injured | NPC.SelfHints.Is_Vulnerable | NPC.SelfHints.Is_Stuck | NPC.SelfHints.Is_Incapacitated | NPC.SelfHints.Is_Stunned);
				count_positive += self_hints.GetCount(NPC.SelfHints.Can_Attack_Melee | NPC.SelfHints.Has_Armor | NPC.SelfHints.Is_Dangerous | NPC.SelfHints.Is_Armored | NPC.SelfHints.Can_Climb);

				amount -= count_negative * 0.25f;
				amount += count_positive * 0.50f;

				if (has_target && item_hints.HasNone(NPC.ItemHints.Ranged | NPC.ItemHints.Gun))
				{
					amount *= 1.00f + (Maths.InvLerp01(48.00f, 2.00f, distance) * 1.50f);
				}

				if (amount > 0.00f)
				{
					amount += amount * combat_flags.GetCount(Behavior.CombatFlags.Charge | Behavior.CombatFlags.Chase, 0.20f);


					//if (distance > 6.00f)
					//{
					//	amount *= 0.50f;
					//}

				}

				score += amount;

				if (combat_flags.HasAny(AI.Behavior.CombatFlags.Prefer_Melee)) score += 100.00f;


				//score *= Maths.Mulpo(count_negative, )
			}

			if (item_hints.HasAny(NPC.ItemHints.Gun))
			{
				var mult = 1.00f;

				if (has_target)
				{
					if (target_self_hints.HasNone(NPC.SelfHints.Is_Immune_Bullet))
					{
						return -200.00f;
					}

					mult += target_flags.GetCount(Behavior.TargetFlags.In_Air | Behavior.TargetFlags.Unreachable | Behavior.TargetFlags.Dangerous | Behavior.TargetFlags.Ranged | Behavior.TargetFlags.Melee | Behavior.TargetFlags.Overpowered, 0.20f);

					if (item_hints.HasAny(NPC.ItemHints.Long_Range))
					{
						mult *= 1.00f + (Maths.InvLerp01(20.00f, 40.00f, distance) * 2.50f);
					}

					if (item_hints.HasAny(NPC.ItemHints.Short_Range))
					{
						mult *= 1.00f + (Maths.InvLerp01(25.00f, 5.00f, distance) * 2.00f);
					}
				}
				else if (score > 0.00f)
				{
					score += 5.00f;
					mult *= 1.20f; // Prefer guns by default
				}
				//if (target_flags.HasAny(Behavior.TargetFlags.In_Air | Behavior.TargetFlags.Unreachable | Behavior.TargetFlags.))
				//score += Maths.InvLerp01(15.00f, 5.00f, distance) * 4.00f;
				score *= mult;

				if (combat_flags.HasAny(AI.Behavior.CombatFlags.Prefer_Ranged)) score += 100.00f;

				//App.WriteLine(Maths.InvLerp01(9.00f, 3.00f, distance) * 4.00f);
				//App.WriteLine(mult);
			}

			if (!has_target && !self_hints.HasAny(NPC.SelfHints.Is_Idle) && item_hints.HasAny(NPC.ItemHints.Small))
			{
				score *= 1.75f; // Prefer small items if no target
			}

			if (item_hints.HasAny(NPC.ItemHints.Weak)) // Avoid weak items
			{
				score -= 0.20f;
				score *= 0.40f;
			}
			else if (item_hints.HasAny(NPC.ItemHints.Powerful)) // Prefer powerful items
			{
				score += 5.00f;
				score *= 1.50f;
			}

			if (!has_target && item_hints.HasAny(NPC.ItemHints.Heavy)) // Avoid cumbersome items
			{
				score *= 0.75f;
				score -= 3.00f;
			}

			//if (self_hints.HasAny(NPC.SelfHints.Is_Stuck) && item_hints.HasNone(NPC.ItemHints.Small))
			//{
			//	score -= 100.00f;
			//}

			score += (target_self_hints.GetCount(NPC.SelfHints.Is_Armored | NPC.SelfHints.In_Vehicle | NPC.SelfHints.Is_Vehicle | NPC.SelfHints.Is_Structure | NPC.SelfHints.Is_Giant, 1.50f) * item_hints.GetCount(NPC.ItemHints.Powerful | NPC.ItemHints.Explosive | NPC.ItemHints.Dangerous | NPC.ItemHints.Destructive, 0.75f));

			// Prefer defensive items if hurt
			score += combat_flags.GetCount(Behavior.CombatFlags.Flee | Behavior.CombatFlags.Cover | Behavior.CombatFlags.Evade | Behavior.CombatFlags.Defend) * item_hints.GetCount(NPC.ItemHints.Defensive | NPC.ItemHints.Armor, 0.50f) * self_hints.GetCount(NPC.SelfHints.Is_Doomed | NPC.SelfHints.Is_Fleeing | NPC.SelfHints.Is_Incapacitated | NPC.SelfHints.Is_Stunned | NPC.SelfHints.Is_Injured, 0.60f);

			//if (self_hints.HasAny(NPC.SelfHints.Is_Doomed | NPC.SelfHints.Is_Fleeing | NPC.SelfHints.Is_Vulnerable | NPC.SelfHints.Is_Injured) && item_hints.HasAny(NPC.ItemHints.Defensive | NPC.ItemHints.Armor))
			//{
			//	score += 50.00f * item_hints.GetCount(NPC.ItemHints.Defensive | NPC.ItemHints.Armor);
			//}

			return score;
		}

		[ISystem.VeryLateUpdate(ISystem.Mode.Single, ISystem.Scope.Region, interval: 0.16f)]
		[HasTag("dead", false, Source.Modifier.Shared), HasComponent<Player.Data>(Source.Modifier.Shared, false)]
		public static void UpdateToolbelt(ISystem.Info info, ref Region.Data region, ref XorRandom random, Entity ent_toolbelt,
		[Source.Owned] ref Toolbelt.Data toolbelt,
		[Source.Shared] in Transform.Data transform,
		[Source.Shared, Original] ref AI.Behavior behavior_original, [Source.Shared, Override] ref AI.Behavior behavior_override,
		[Source.Shared] ref NPC.Data npc)
		{
			if (AI.debug_paused) return;
			if (!npc.ent_pickup) return;

			var time = info.WorldTime;
			const bool debug = false;

			if (time >= behavior_original.t_next_toolbelt)
			{
				behavior_original.t_next_toolbelt = time + random.NextFloatExtra(0.60f, 1.60f);

				//{
				//	ref var self_hints = ref npc_original.self_hints;
				//	self_hints.SetFlag(NPC.SelfHints.Has_Toolbelt, true);
				//}

				//App.WriteLine(npc.item_hints);


				//var is_held_alive = npc.ent_pickup_target.IsAlive();
				//if (is_held_alive)
				//{&& npc.item_hints.HasAny(NPC.ItemHints.Storable) &&
				//	toolbelt.filter_hints.Evaluate(npc.item_hints)
				//}


				var position_target = behavior_override.position_target ?? behavior_override.position_pivot;

				var distance = Vector2.Distance(transform.position, position_target);
				var target_flags = behavior_override.target_flags;
				var combat_flags = behavior_override.combat_flags;
				var target_self_hints = behavior_override.target_self_hints;
				var target_item_hints = behavior_override.target_item_hints;
				var self_hints_override = npc.self_hints;

				var current_score = 0.00f;

				//App.WriteLine(npc.item_hints);

				if (!npc.item_hints.IsEmpty())
				{
					current_score = CalculateItemHeuristic(item_hints: npc.item_hints,
						self_hints: self_hints_override,
						combat_flags: combat_flags,
						target_flags: target_flags,
						target_item_hints: target_item_hints,
						target_self_hints: target_self_hints,
						distance: distance,
						item_hints_prefer: behavior_override.item_hints_prefer);
				}

				//if (!is_player)
				{
					//npc_original.self_hints.SetFlag(NPC.SelfHints.Can_Attack_Ranged | NPC.SelfHints.Can_Attack_Melee | NPC.SelfHints.Has_Weapon, false);

					var item_index_best = -1;
					var item_score_best = 0.00f;

					var sb = default(StringBuilder);
					if (debug) sb = new StringBuilder();

					var ts = Timestamp.Now();
					if (self_hints_override.HasNone(NPC.SelfHints.In_Vehicle))
					{
						for (var i = 0u; i < toolbelt.capacity; i++)
						{
							var ent_item = toolbelt.items[i].entity;
							if (ent_item.id == 0) continue;

							var item_hints = toolbelt.items[i].item_hints;

							var score = CalculateItemHeuristic(item_hints: item_hints, 
								self_hints: self_hints_override, 
								combat_flags: combat_flags,
								target_flags: target_flags, 
								target_item_hints: target_item_hints, 
								target_self_hints: target_self_hints, 
								distance: distance,
								item_hints_prefer: behavior_override.item_hints_prefer);

							toolbelt.items_scores[i] = score;

							if (debug) sb.AppendLine($"{ent_item.GetName()}: {score:0.00}");

							if (score > item_score_best)
							{
								item_index_best = (int)i;
								item_score_best = score;
							}

							//if (item_hints.HasAny(NPC.ItemHints.Usable))
							//{
							//	npc_original.self_hints.AddFlag(NPC.SelfHints.Has_Weapon, item_hints.HasAny(NPC.ItemHints.Weapon));
							//	npc_original.self_hints.AddFlag(NPC.SelfHints.Can_Attack_Melee, item_hints.HasAny(NPC.ItemHints.Melee));
							//	npc_original.self_hints.AddFlag(NPC.SelfHints.Can_Attack_Ranged, item_hints.HasAny(NPC.ItemHints.Short_Range | NPC.ItemHints.Long_Range));

							//	if (!found)
							//	{
							//		if (item_hints.HasAll(NPC.ItemHints.Weapon | NPC.ItemHints.Short_Range | NPC.ItemHints.Long_Range) && (behavior_override.target_flags.HasAny(Behavior.TargetFlags.In_Air | Behavior.TargetFlags.Ranged | Behavior.TargetFlags.Unreachable | Behavior.TargetFlags.Dangerous)))
							//		{
							//			Toolbelt.Withdraw(ent_toolbelt, npc_override.ent_arm, i);
							//			found = true;
							//		}
							//		//else if (item_hints.HasAll(NPC.ItemHints.Weapon | NPC.ItemHints.Melee) && (!behavior_override.target_flags.HasAny(Behavior.TargetFlags.In_Air | Behavior.TargetFlags.Unreachable) && (distance_sq < (8 * 8))))
							//		else if (item_hints.HasAll(NPC.ItemHints.Weapon | NPC.ItemHints.Melee) && (!behavior_override.target_flags.HasAny(Behavior.TargetFlags.In_Air | Behavior.TargetFlags.Unreachable)))
							//		{
							//			Toolbelt.Withdraw(ent_toolbelt, npc_override.ent_arm, i);
							//			found = true;
							//		}
							//	}
							//}
						}
					}
					else
					{
						item_score_best = 100;
					}
					var ts_elapsed = ts.GetMilliseconds();

					//if (item_index_best < 0)
					//{
					//	if (!npc_override.item_hints.IsEmpty())
					//	{
					//		Toolbelt.Deposit(ent_toolbelt, npc_override.ent_arm.GetChild(Relation.Type.Child));
					//		behavior_original.next_toolbelt = time + (0.90f * random.NextFloatRange(1.10f, 1.50f));
					//	}
					//}
					//else

					//if (item_score_best > Maths.epsilon && item_score_best > current_score)
					if (item_score_best > current_score)
					{
						if (npc.ent_pickup_target.IsAlive())
						{
							if (npc.item_hints.HasAny(NPC.ItemHints.Storable) && toolbelt.filter_hints.Evaluate(npc.item_hints)) // deposit to toolbelt if item is storable and matches the toolbelt filter
							{
								Toolbelt.Deposit(ent_toolbelt: ent_toolbelt, ent_item: npc.ent_pickup_target);
								behavior_original.t_next_toolbelt = time + random.NextFloatExtra(0.30f, 0.50f);
							}
							else if (npc.item_hints.HasAny(NPC.ItemHints.Wrecked | NPC.ItemHints.Dumb | NPC.ItemHints.Primitive | NPC.ItemHints.Crappy | NPC.ItemHints.Junk)) // only drop stuff that's likely to be garbage, since you may not want to drop expensive items
							{
								Arm.Drop(ent_parent: npc.ent_pickup, ent_child: npc.ent_pickup_target); //.Deposit(ent_toolbelt, npc.ent_arm.GetChild(Relation.Type.Child));
								behavior_original.t_next_toolbelt = time + random.NextFloatExtra(0.30f, 0.50f);
							}
							else
							{
								behavior_original.t_next_toolbelt = time + random.NextFloatExtra(2.00f, 4.00f);
							}
						}
						else if (item_index_best >= 0)
						{
							Toolbelt.Withdraw(ent_toolbelt: ent_toolbelt, ent_arm: npc.ent_arm, index: (uint)item_index_best);
						}
					}
					else
					{
						behavior_original.t_next_toolbelt = time + (4.00f * random.NextFloatRange(0.70f, 1.40f));
					}

					//behavior_original.target_flags.AddFlag(Behavior.TargetFlags.Dangerous | Behavior.TargetFlags.Careful | Behavior.TargetFlags.Overpowered, !found);

					if (debug)
					{

						//WorldNotification.Push(ref region, $"{ts_elapsed:0.0000} ms\n{item_score_best}/{current_score}\n{target_flags}", Color32BGRA.Yellow, transform.position);
						WorldNotification.Push(ref region, $"{ts_elapsed:0.0000} ms\n{item_score_best:0.00}/{current_score:0.00}\n{target_flags}\n{sb}", Color32BGRA.Yellow, transform.position + new Vector2(0, 2), velocity: Vector2.Zero, lifetime: 2.00f);
					}
				}

			}

			//if (AI.debug_always_sync) // || results.HasAll(Pathfinding.Results.Dirty) || sync)
			//{
			//	npc_original.Sync(entity, true);
			//}
		}
#endif

		[ISystem.Event<Health.PostDamageEvent>(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("dead", false, Source.Modifier.Owned)]
		public static void OnPostDamage(ISystem.Info info, Entity entity, ref XorRandom random, ref Health.PostDamageEvent data,
		[Source.Owned] ref Health.Data health,
		[Source.Owned, Original] ref AI.Data ai_original,
		[Source.Owned, Original] ref AI.Behavior behavior_original, [Source.Owned, Override] ref AI.Behavior behavior_override,
		[Source.Owned, Original] ref AI.Movement movement_original, [Source.Owned, Optional] in Faction.Data faction)
		{
			if (AI.debug_paused) return;

			if (data.damage.faction_id == 0 || data.damage.faction_id != faction.id)
			{
				if (data.damage.ent_owner != entity && data.damage.ent_attacker != entity && data.damage.ent_owner.IsAlive())
				{
					var threat_priority = 0.00f;
					var threat_priority_current = 0.00f;

					behavior_original.t_last_damage = info.WorldTime;

					ref var threat = ref data.damage.ent_owner.GetComponent<Threat.Data>();
					if (threat.IsNotNull())
					{
						threat_priority = threat.priority_override;

						if (behavior_override.ref_target_threat.IsAlive() && behavior_override.ref_target_threat.TryGetHandle(out var h_threat))
						{
							threat_priority_current = h_threat.data.priority_override;
						}

						behavior_original.idle_timer = 0.00f;
						behavior_original.t_next_update = 0.00f;
						behavior_original.t_next_check = 0.00f;
						behavior_original.t_next_action = 0.00f;

						if (threat.priority_override > threat_priority_current)
						{
							behavior_original.type = Behavior.Type.Attack;

							//movement_original.SetTarget(data.damage.ent_owner);
							behavior_original.SetTarget(data.damage.ent_owner);
						}
					}

					/*if (npc.self_hints.HasNone(NPC.SelfHints.Is_Fleeing | NPC.SelfHints.Is_Incapacitated | NPC.SelfHints.Is_Panicking)) */
					ai_original.anger += random.NextFloatRange(data.damage.pain, data.damage.pain * 1.50f) + random.NextFloatRange(ai_original.anger * 0.10f, ai_original.anger * 0.30f);
					//App.WriteLine(ai_original.anger);
				}
			}
		}

		[ISystem.Event<Health.PostDamageEvent>(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("dead", false, Source.Modifier.Parent)]
		public static void OnPostDamageChild(ISystem.Info info, Entity entity, ref XorRandom random, ref Health.PostDamageEvent data,
		[Source.Owned] ref Health.Data health,
		[Source.Parent, Original] ref AI.Data ai_original,
		[Source.Parent, Original] ref AI.Behavior behavior_original, [Source.Parent, Override] ref AI.Behavior behavior_override,
		[Source.Parent, Original] ref AI.Movement movement_original, [Source.Parent, Optional] in Faction.Data faction)
			=> OnPostDamage(info: info,
				entity: entity,
				random: ref random,
				data: ref data,
				health: ref health,
				ai_original: ref ai_original,
				behavior_original: ref behavior_original,
				behavior_override: ref behavior_override,
				movement_original: ref movement_original,
				faction: in faction);
#endif

		public static void MoveToAir(ref Region.Data region, ref Control.Data control, Vector2 a, Vector2 b)
		{
			//var dir = (b - a).GetNormalized(out var distance);
			var delta = (b - a);
			//var dir = delta.GetNormalized(out var distance);

			//var follow_distance_max = 0.75f;
			var follow_distance_max = 0.45f;
			var follow_distance_min = 0.10f;

			var delta_abs = Vector2.Abs(delta);

			//control.keyboard.SetKeyPressed(Keyboard.Key.MoveRight, (delta_abs.X > follow_distance_max & delta.X > 0) || (delta_abs.X < follow_distance_min & delta.X < 0));
			//control.keyboard.SetKeyPressed(Keyboard.Key.MoveLeft, (delta_abs.X > follow_distance_max & delta.X < 0) || (delta_abs.X < follow_distance_min & delta.X > 0));
			//control.keyboard.SetKeyPressed(Keyboard.Key.MoveDown, (delta_abs.Y > follow_distance_max & delta.Y > 0) || (delta_abs.Y < follow_distance_min & delta.Y < 0));
			//control.keyboard.SetKeyPressed(Keyboard.Key.MoveUp, (delta_abs.Y > follow_distance_max & delta.Y < 0) || (delta_abs.Y < follow_distance_min & delta.Y > 0));

			control.keyboard.SetKeyPressed(Keyboard.Key.MoveRight, (delta.X > follow_distance_max));
			control.keyboard.SetKeyPressed(Keyboard.Key.MoveLeft, (delta.X < follow_distance_max));
			control.keyboard.SetKeyPressed(Keyboard.Key.MoveDown, (delta.Y > follow_distance_max));
			control.keyboard.SetKeyPressed(Keyboard.Key.MoveUp, (delta.Y < follow_distance_max));


			//control.keyboard.SetKeyPressed(Keyboard.Key.MoveUp, (distance > follow_distance_max));
			//control.keyboard.SetKeyPressed(Keyboard.Key.MoveDown, (distance > follow_distance_max));

			//control.keyboard.SetKeyPressed(Keyboard.Key.MoveUp, (distance > follow_distance_max && ((a.Y - b.Y) > 1.00f)));
			//control.keyboard.SetKeyPressed(Keyboard.Key.MoveDown, (distance > follow_distance_max && ((a.Y - b.Y) < -1.00f)));

			//#if SERVER
			//			//WorldNotification.Push(ref region, $"delta: {delta}", Color32BGRA.Cyan, b);

			//			if (control.keyboard.GetKey(Keyboard.Key.MoveLeft)) region.DrawDebugDir(a, new Vector2(-10, 0.00f), Color32BGRA.White, 1.00f);
			//			if (control.keyboard.GetKey(Keyboard.Key.MoveRight)) region.DrawDebugDir(a, new Vector2(10, 0.00f), Color32BGRA.White, 1.00f);
			//			if (control.keyboard.GetKey(Keyboard.Key.MoveUp)) region.DrawDebugDir(a, new Vector2(0, -10), Color32BGRA.White, 1.00f);
			//			if (control.keyboard.GetKey(Keyboard.Key.MoveDown)) region.DrawDebugDir(a, new Vector2(0, 10), Color32BGRA.White, 1.00f);
			//			region.DrawDebugDir(a, delta, Color32BGRA.Yellow, 8);
			//#endif
		}

		[Shitcode]
		public static void MoveToLand(ref Control.Data control, Vector2 a, Vector2 b, Vector4 min, Vector4 max, bool climbing)
		{
			var delta = (b - a);
			var dir = delta.GetNormalized(out var distance);

			var min_l = min.X;
			var min_r = min.Y;
			var min_u = min.Z;
			var min_d = min.W;

			var max_l = max.X;
			var max_r = max.Y;
			var max_u = max.Z;
			var max_d = max.W;

			var delta_abs = Vector2.Abs(delta);

			control.keyboard.SetKeyPressed(Keyboard.Key.MoveRight, (delta.X < -Maths.epsilon && delta_abs.X < min_l) || (delta.X > Maths.epsilon && delta_abs.X > max_r));
			control.keyboard.SetKeyPressed(Keyboard.Key.MoveLeft, (delta.X > Maths.epsilon && delta_abs.X < min_r) || (delta.X < -Maths.epsilon && delta_abs.X > max_l));

			//control.keyboard.SetKeyPressed(Keyboard.Key.MoveDown, (delta.Y < 0.00f && delta_abs.Y < min_d) || (delta.Y > 0.00f && delta_abs.Y > max_u));
			//control.keyboard.SetKeyPressed(Keyboard.Key.MoveUp, (delta.Y > 0.00f && delta_abs.Y < min_u) || (delta.Y < 0.00f && delta_abs.Y > max_d));

			//if ((delta_abs.X < 1.00f && delta.Y > 2.00f)) // || delta.Y > 3.50f)
			if (climbing ? (delta_abs.X < 1.00f || delta.Y > 2.50f) : (delta_abs.X < 1.00f && delta.Y > 2.00f))
			{
				if (climbing) control.keyboard.SetKeyPressed(Keyboard.Key.MoveDown, (delta.Y < -Maths.epsilon && delta_abs.Y < min_d) || (delta.Y > Maths.epsilon && delta_abs.Y > max_u));
				control.keyboard.SetKeyPressed(Keyboard.Key.MoveUp, (delta.Y > Maths.epsilon && delta_abs.Y < min_u) || (delta.Y < -Maths.epsilon && delta_abs.Y > max_d));
			}

			//#if SERVER
			//			//WorldNotification.Push(ref region, $"delta: {delta}", Color32BGRA.Cyan, b);

			//			if (kb.GetKey(Keyboard.Key.MoveLeft)) region.DrawDebugDir(pos_current, new Vector2(-10, 0.00f), Color32BGRA.White, 1.00f);
			//			if (kb.GetKey(Keyboard.Key.MoveRight)) region.DrawDebugDir(pos_current, new Vector2(10, 0.00f), Color32BGRA.White, 1.00f);
			//			if (kb.GetKey(Keyboard.Key.MoveUp)) region.DrawDebugDir(pos_current, new Vector2(0, -10), Color32BGRA.White, 1.00f);
			//			if (kb.GetKey(Keyboard.Key.MoveDown)) region.DrawDebugDir(pos_current, new Vector2(0, 10), Color32BGRA.White, 1.00f);
			//			region.DrawDebugDir(a, dir * distance, Color32BGRA.Yellow, 8);
			//#endif
		}

		//		[ISystem.EarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region), Exclude<Player.Data>(Source.Modifier.Shared), HasTag("dead", false, Source.Modifier.Owned)]
		//		public static void Update1(Entity entity, ISystem.Info info, [Source.Owned, Original] ref NPC.Data npc, [Source.Owned] ref Pathfinding.Data pathfinding, [Source.Owned] in Transform.Data transform)
		//		{
		//			if (npc.ref_target.TryGetHandle(out var handle))
		//			{
		//				ref var target_value = ref handle.Value;
		//				npc.target_position = target_value.position + (target_value.velocity * App.fixed_update_interval_s * 10);
		//				npc.target_velocity = target_value.velocity;

		//#if SERVER
		//				if (target_value.value <= float.Epsilon)
		//				{
		//					npc.ref_target.Set(default);
		//					entity.SyncComponent(ref npc);
		//				}
		//#endif
		//			}
		//		}
	}
}
