
namespace TC2.Base.Components
{
	public static partial class Animal
	{
		[Flags]
		public enum Flags: ushort
		{
			None = 0,

			Aggressive = 1 << 0,
			Passive = 1 << 1,

			Enable_Stuck_Tantrum = 1 << 4,
			Attack_Terrain = 1 << 5,

			Use_RMB_For_Movement = 1 << 6,
		}

		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Region), IComponent.With<Animal.State>]
		public partial struct Data(): IComponent, IOverridable
		{
			public Animal.Flags flags;

			public float attack_cooldown;

			public float attack_distance_melee;
			public float attack_cooldown_melee;

			public float attack_distance_ranged;
			public float attack_cooldown_ranged;

			public float charge_distance = 4.00f;
			public float retreat_distance = 6.00f;
		}

		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public partial struct State: IComponent
		{
			[Net.Ignore, Save.Ignore] public float next_attack;

			[Net.Ignore, Save.Ignore] public float next_attack_melee;
			[Net.Ignore, Save.Ignore] public float next_attack_ranged;

			[Net.Ignore, Save.Ignore] public float next_stuck_check;
		}

#if SERVER
		[Shitcode]
		[ISystem.EarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		[HasComponent<Player.Data>(Source.Modifier.Shared, false), HasTag("dead", false, Source.Modifier.Owned)]
		public static void UpdateAI(ISystem.Info info, ref XorRandom random, ref Region.Data region,
		[Source.Owned, Original] ref Animal.Data animal, [Source.Owned] ref Animal.State animal_state,
		[Source.Owned] in Transform.Data transform, [Source.Owned] ref Body.Data body,
		[Source.Owned] ref Control.Data control,
		/*[Source.Owned, Original] ref AI.Behavior behavior_original, */[Source.Owned, Override] ref AI.Behavior behavior_override,
		/*[Source.Owned, Original] ref AI.Movement movement_original, */[Source.Owned, Override] ref AI.Movement movement_override,
		[Source.Owned, Original] ref AI.Data ai_original,
		[Source.Owned] ref NPC.Data npc)
		{
			var ts = Timestamp.Now();

			var time = info.WorldTime;

			//App.WriteLine("badger");

			//return;

			npc.self_hints.AddFlag(NPC.SelfHints.Is_Animal | NPC.SelfHints.Is_Creature);
			var can_attack = npc.consciousness >= 0.65f;

			if (animal.flags.HasAny(Animal.Flags.Use_RMB_For_Movement))
			{
				if (npc.self_hints.HasAny(NPC.SelfHints.Is_Doomed | NPC.SelfHints.Is_Swimming | NPC.SelfHints.Is_Stuck))
				{
					if (time >= animal_state.next_stuck_check)
					{
						animal_state.next_stuck_check = random.NextFloatExtra(time + 0.10f, 1.00f);
						//control.mouse.position = movement_override.position_target ?? behavior_override.position_goal;
						control.mouse.SetKeyPressed(Mouse.Key.Right | Mouse.Key.Left, true);

						//Pathfinding.MoveToLand2(ref region, ref control, transform.position, behavior_override.position_target ?? control.mouse.position, npc.self_hints);
					}
				}
			}

			//if (npc.self_hints.HasAll(NPC.SelfHints.Is_Stuck | NPC.SelfHints.Can_Walk))
			//{
			//	control.keyboard.SetKeyPressed(Keyboard.Key.X, true);
			//}

			if (can_attack && npc.self_hints.HasAny(NPC.SelfHints.Is_Door_Stuck) && movement_override.position_stuck_contact.TryGetValue(out var pos_target)) //(movement_override.ent_last_door == behavior_override.ref_target_transform.entity) && behavior_override.position_target.TryGetValue(out var pos_target))
			{
				var dir = (pos_target - transform.position).GetNormalized(out var distance);

				if (AI.debug_always_sync)
				{
					region.DrawDebugCircle(pos_target, 0.50f, color: Color32BGRA.Magenta, filled: true, segments: 4);
				}

				control.mouse.position = Vector2.Lerp(control.mouse.position, pos_target, 0.10f);
				//movement_override.position_path = pos_target;

				if (distance <= animal.attack_distance_melee * 0.90f)
				{
					movement_override.type = AI.Movement.Type.None;

					//movement_override.type = AI.Movement.Type.Straight; //.Straight;

					//movement_override.type = AI.Movement.Type.Straight;




					control.mouse.SetKeyPressed(Mouse.Key.Left, true);
					animal_state.next_attack = time + (animal.attack_cooldown * random.NextFloatRange(0.20f, 1.20f));
					animal_state.next_attack_melee = time + (animal.attack_cooldown_melee * random.NextFloatRange(0.20f, 1.40f));
				}
			}
			else if (can_attack && animal.flags.HasAny(Animal.Flags.Enable_Stuck_Tantrum) && ((ai_original.anger > 400.00 && npc.self_hints.HasAny(NPC.SelfHints.Is_Stuck) && movement_override.position_stuck.TryGetValue(out var pos_stuck) && movement_override.position_stuck_contact.TryGetValue(out var pos_contact))))
			{

				var dir = (pos_contact - transform.position).GetNormalized(out var distance);
				var pos = pos_contact; // + (dir * 0.25f); // + random.NextUnitVector2Range(0.00f, 0.125f);

				//movement_override.position_target = pos;
				//movement_override.type = AI.Movement.Type.Move;

				//region.DrawDebugCircle(pos_target, 0.50f, color: Color32BGRA.Red);

				//var dir = (movement_override.position_target.Value - body.GetPosition()).GetNormalized(out var dist);



				//var pos_a = movement_override.position_path.Value;
				//var pos_b = movement_override.position_stuck.Value;

				// movement_override.position_stuck;
				//if (region.TryLinecast(body.GetPosition(), movement_override.position_target.Value, 0.750f, out var linecast_result, mask: Physics.Layer.World | Physics.Layer.Door, query_flags: Physics.QueryFlag.Static))
				//{
				//	movement_original.position_stuck_contact = linecast_result.world_position_raw; // - (linecast_result.normal * 0.25f);
				//}


				control.mouse.position = Vector2.Lerp(control.mouse.position, pos, 0.02f);
				//region.DrawDebugCircle(pos, 0.50f, color: Color32BGRA.Yellow);
				if (AI.debug_always_sync)
				{
					region.DrawDebugCircle(pos_contact, 0.50f, color: Color32BGRA.Magenta);
					region.DrawDebugCircle(pos_stuck, 0.50f, color: Color32BGRA.Red);
				}
				//region.DrawDebugLine(transform.position, control.mouse.position, Color32BGRA.Red, 4);

				//movement_override.position_target = pos;
				//movement_override.type = AI.Movement.Type.Move;


				//if (time >= animal_state.next_attack_melee)
				//if (can_attack)
				{
					//movement_override.type = AI.Movement.Type.Straight;
					if (distance <= animal.attack_distance_melee * 0.90f)
					{
						//movement_override.type = AI.Movement.Type.Straight; //.Straight;

						//movement_override.type = AI.Movement.Type.Straight;




						control.mouse.SetKeyPressed(Mouse.Key.Left, true);
						animal_state.next_attack = time + (animal.attack_cooldown * random.NextFloatRange(0.20f, 1.20f));
						animal_state.next_attack_melee = time + (animal.attack_cooldown_melee * random.NextFloatRange(0.20f, 1.40f));

					}
				}
				//else
				//{
				//	if (distance <= animal.retreat_distance)
				//	{
				//		movement_override.position_target = pos - (new Vector2(dir.X, 0.10f) * animal.retreat_distance);
				//		movement_override.type = AI.Movement.Type.Straight;
				//		movement_override.flags.SetFlag(AI.Movement.Flags.No_Mouse, true);
				//	}
				//}

				//region.DrawDebugCircle(n_pos.Value, 0.50f, color: Color32BGRA.Red);
				//App.WriteLine("a");
			}
			//else if (animal.flags.HasAny(Flags.Use_RMB_For_Movement) && (npc.self_hints.HasAny(NPC.SelfHints.Is_Stuck) || behavior_override.target_flags.HasAny(AI.Behavior.TargetFlags.Unreachable)))
			//{
			//	if (true)
			//	{
			//		control.mouse.position = behavior_override.position_target.GetValueOrDefault();
			//		control.mouse.SetKeyPressed(Mouse.Key.Right, true);
			//		//animal_state.next_attack = time + animal.attack_cooldown;
			//		animal_state.next_attack_ranged = time + (animal.attack_cooldown_ranged * random.NextFloatRange(0.90f, 1.20f));
			//	}
			//}
			else
			{
				switch (behavior_override.type)
				{
					case AI.Behavior.Type.Attack:
					{
						//if (time >= behavior_override.next_update)
						{
							npc.self_hints.AddFlag(NPC.SelfHints.Is_Alerted);
							var n_pos = behavior_override.position_target;

							//ref var target_body = ref behavior_override.ref_target_body.GetValueOrNullRef();
							//if (!target_body.IsNull())
							//{
							//	pos = target_body.GetClosestPoint(transform.position).world_position;
							//}

							//if (npc_override.self_hints.HasAny(NPC.SelfHints.Is_Stuck) && movement_override.position_stuck_contact.TryGetValue(ref n_pos))



							{
								ref var target_transform = ref behavior_override.ref_target_transform.GetComponent();
								if (target_transform.IsNotNull())
								{
									n_pos = target_transform.LocalToWorldNoScale(behavior_override.position_target_local);
								}


								//if (ai_original.anger > 400)
								//if (npc_override.self_hints.HasAny(NPC.SelfHints.Is_Stuck) && ai_original.anger >= 300.00f)
								//{
								//	n_pos = movement_override.position_stuck;


								//	//if (time >= animal_state.next_stuck_check)
								//	//{

								//	//}
								//}

								if (n_pos.TryGetValue(out var pos))
								{
									//var dir = (pos - transform.position).GetNormalized(out var distance);




									movement_override.position_target = pos;
									movement_override.type = AI.Movement.Type.Move;

									var dir = (pos - transform.position).GetNormalized(out var distance);
									if (can_attack && time >= animal_state.next_attack)
									{
										if (distance <= animal.charge_distance)
										{
											control.mouse.position = Vector2.Lerp(control.mouse.position, pos, 0.70f);

											if (distance <= animal.attack_distance_melee * 0.90f)
											{
												if (time >= animal_state.next_attack_melee && (region.IsInLineOfSight(pos_a: transform.position, pos_b: pos, group: behavior_override.ref_target_transform.entity, layer: Physics.Layer.Solid, mask: Physics.Layer.World, exclude: Physics.Layer.Creature | Physics.Layer.Dynamic, query_flags: Physics.QueryFlag.Static)))
												{
													control.mouse.SetKeyPressed(Mouse.Key.Left, true);
													animal_state.next_attack = time + (animal.attack_cooldown * random.NextFloatRange(0.60f, 1.30f));
													animal_state.next_attack_melee = time + (animal.attack_cooldown_melee * random.NextFloatRange(0.60f, 1.15f));
												}
											}
											else if (distance <= animal.attack_distance_ranged * 0.90f)
											{
												if (time >= animal_state.next_attack_ranged && (region.IsInLineOfSight(pos_a: transform.position, pos_b: pos, group: behavior_override.ref_target_transform.entity, layer: Physics.Layer.Solid, mask: Physics.Layer.World, exclude: Physics.Layer.Creature | Physics.Layer.Dynamic, query_flags: Physics.QueryFlag.Static)))
												{
													control.mouse.SetKeyPressed(Mouse.Key.Right, true);
													//animal_state.next_attack = time + animal.attack_cooldown;
													animal_state.next_attack_ranged = time + (animal.attack_cooldown_ranged * random.NextFloatRange(0.90f, 1.20f));
												}
											}
										}
									}
									else
									{
										control.mouse.position = Vector2.Lerp(control.mouse.position, pos, 0.50f);

										if (!can_attack || distance <= animal.retreat_distance)
										{
											if (npc.self_hints.HasAny(NPC.SelfHints.Can_Fly)) movement_override.position_target = pos - (new Vector2(dir.X, ((dir.Y + 0.34f) * 1.08f)) * animal.retreat_distance);
											else movement_override.position_target = pos - (new Vector2(dir.X, 0.10f) * animal.retreat_distance);

											movement_override.type = AI.Movement.Type.Straight;
											movement_override.flags.SetFlag(AI.Movement.Flags.No_Mouse, true);
										}
									}
								}
							}
						}
					}
					break;
				}
			}
		}

		//[ISystem.EarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region), Exclude<Player.Data>(Source.Modifier.Shared), HasTag("dead", false, Source.Modifier.Owned)]
		//public static void Update(ISystem.Info info,
		//[Source.Owned] in Animal.Data animal, [Source.Owned] ref Animal.State animal_state, [Source.Owned] ref NPC.Data npc, 
		//[Source.Owned] ref Control.Data control, [Source.Owned] in Transform.Data transform, [Source.Owned] in Organic.Data organic)
		//{
		//	control.mouse.SetKeyPressed(Mouse.Key.Right, false);

		//	if (npc.navigation_state.HasAll(NPC.NavigationState.Chasing))
		//	{
		//		ref var region = ref info.GetRegion();

		//		if (npc.target_position.TryGetValue(out var target_position))
		//		{
		//			var dir = (target_position - transform.position).GetNormalized(out var distance);

		//			var is_los = false;
		//			if (distance <= animal.charge_distance)
		//			{
		//				is_los = region.IsInLineOfSight(transform.position, target_position, 0.125f, mask: Physics.Layer.World, query_flags: Physics.QueryFlag.Static);
		//			}

		//			npc.navigation_state.SetFlag(NPC.NavigationState.Charging, is_los && distance <= animal.charge_distance && !npc.navigation_state.HasAll(NPC.NavigationState.Retreating));
		//			npc.navigation_state.SetFlag(NPC.NavigationState.Retreating, is_los && distance <= animal.retreat_distance && info.WorldTime < animal_state.next_attack);

		//			if (is_los)
		//			{
		//				control.mouse.SetKeyPressed(Mouse.Key.Right, true);
		//			}

		//			if (is_los && info.WorldTime > animal_state.next_attack && distance <= animal.attack_distance)
		//			{
		//				animal_state.next_attack = info.WorldTime + animal.attack_cooldown;
		//				control.mouse.SetKeyPressed(Mouse.Key.Left, true);
		//			}
		//			else
		//			{
		//				control.mouse.SetKeyPressed(Mouse.Key.Left, false);
		//			}
		//		}
		//	}
		//}
#endif
	}
}
