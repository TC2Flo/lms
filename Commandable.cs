
namespace TC2.Base.Components
{
	public static partial class Commandable
	{
		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Global | IComponent.Scope.Region)]
		public partial struct Data: IComponent
		{
			[Flags]
			public enum Flags: ushort
			{
				None = 0,

				No_Select = 1 << 0,
			}

			public Material.Type last_material_type;
			public Commandable.OrderType last_type;
			public Commandable.Data.Flags flags;
			public uint unused_00;

			public Physics.Layer last_layer;

			[Save.NewLine]
			[Asset.Ignore] public Entity ent_target;
			[Asset.Ignore] public Entity ent_follow;
			[Asset.Ignore] public Entity ent_storage;
			[Asset.Ignore] public Entity ent_home;

			[Save.Ignore, Net.Ignore] public float last_time;
		}

		public enum OrderType: byte
		{
			Undefined = 0,

			Move,
			Pickup,
			Use,
			Attack,
			Defend,
			Harvest,
			Capture,
			Store,
			Excavate,
			Work,
			Drive,
			Enter,
			Rally
		}

		[Flags]
		public enum OrderFlags: byte
		{
			None = 0,

			Wander = 1 << 0,
			Hold = 1 << 1,
		}

		[IEvent.Data]
		public partial struct OrderEvent(): IEvent
		{
			public Commandable.OrderType type;
			public Commandable.OrderFlags flags;
			public byte unit_index;
			public byte unit_count;

			public Entity ent_target;
			public IComponent.Handle h_component_target;

			public Vector2? position;
		}

		// [ISystem.AddFirst(ISystem.Mode.Single, ISystem.Scope.Region)]
		// public static void OnAddBody([Source.Owned] in Commandable.Data commandable, [Source.Owned] ref Body.Data body)
		// {
		// 	body.override_shape_layer.SetFlag(Physics.Layer.Commandable, true);
		// 	body.flags.SetFlag(Body.Flags.NonDirty, false);
		// }

		// [ISystem.RemoveLast(ISystem.Mode.Single, ISystem.Scope.Region)]
		// public static void OnRemoveBody([Source.Owned] in Commandable.Data commandable, [Source.Owned] ref Body.Data body)
		// {
		// 	body.override_shape_layer.SetFlag(Physics.Layer.Commandable, false);
		// 	body.flags.SetFlag(Body.Flags.NonDirty, false);
		// }

		// TODO: remove DEV_ prefix
		public partial struct DEV_SetOrderRPC: Net.IRPC<Commandable.Data>
		{
			public Entity ent_target;
			public IComponent.Handle h_component_target;

			public required Vector2? position_a;
			public Vector2? position_b;

			public required Commandable.OrderType order_type;
			public Commandable.OrderFlags flags;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Commandable.Data data)
			{
				ref var region = ref rpc.GetRegion();
				ref var player = ref rpc.GetSenderPlayer(out var s_player);

				if (region.IsNotNull() && player.IsNotNull() && rpc.entity.IsFactionOwned(s_player))
				{
					//App.WriteValue(this.order_type);

					var ev = new Commandable.OrderEvent()
					{
						type = this.order_type.OrDefault(OrderType.Move),
						position = this.position_a,
						ent_target = this.ent_target,
						h_component_target = this.h_component_target,
					};
					rpc.entity.TriggerEvent(ref ev);

					Sound.PlayGUI(ref rpc.connection, "ui.misc.04", volume: 0.45f, pitch: 2.00f);
				}
			}
#endif
		}

#if SERVER
		[Shitcode]
		[HasTag("dead", false, Source.Modifier.Owned)]
		[ISystem.Event<Commandable.OrderEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnOrderEvent(ISystem.Info info, Entity entity, ref XorRandom random, ref Region.Data region, ref Commandable.OrderEvent data,
		[Source.Owned] ref Commandable.Data commandable,
		[Source.Owned, Original] ref AI.Behavior behavior, // [Source.Owned, Override] ref AI.Behavior behavior_override,
		[Source.Owned, Original] ref AI.Movement movement, // [Source.Owned, Override] ref AI.Movement movement_override,
		/*[Source.Owned, Original] ref AI.Data ai, */[Source.Owned] ref Pathfinding.State pathfinding_state,
		[Source.Owned] ref Transform.Data transform, [Source.Owned, Optional] in Faction.Data faction)
		{
			//App.WriteLine($"OnOrderEvent {entity.GetFullName()} {data.position}; {data.type}");

			movement.Reset();
			behavior.Reset();
			pathfinding_state.Reset();

			commandable.last_time = info.WorldTime;
			commandable.last_type = data.type;
			commandable.last_material_type = default;
			commandable.last_layer = default;

			var is_target_valid = data.ent_target.IsAlive();
			if (is_target_valid)
			{
				movement.ref_target_transform.Set(data.ent_target);
				movement.ref_target_body.Set(data.ent_target);
				movement.position_target = data.position;

				//behavior.ref_target_harvestable.Set(data.ent_target);
				behavior.ref_target_threat.Set(data.ent_target);
				behavior.ref_target_transform.Set(data.ent_target);
				behavior.ref_target_body.Set(data.ent_target);
				behavior.position_target = data.position;

				if (data.position.HasValue)
				{
					ref var target_transform = ref movement.ref_target_transform.GetComponent();
					if (target_transform.IsNotNull())
					{
						var pos_local = target_transform.WorldToLocalNoScale(data.position.Value);

						movement.position_target_local = pos_local;
						behavior.position_target_local = pos_local;
					}

					ref var target_body = ref movement.ref_target_body.GetComponent();
					if (target_body.IsNotNull())
					{
						commandable.last_material_type = target_body.GetMaterial();
						commandable.last_layer = target_body.GetLayer();
					}
				}
			}
			else
			{
				movement.position_target = data.position;
				behavior.position_target = data.position;
			}

			switch (data.type)
			{
				case OrderType.Move:
				{
					if (is_target_valid)
					{
						movement.type = AI.Movement.Type.Follow;
						movement.flags.SetFlag(AI.Movement.Flags.Continuous, true);

						behavior.type = AI.Behavior.Type.Move;
					}
					else
					{
						movement.type = AI.Movement.Type.Move;
						movement.flags.SetFlag(AI.Movement.Flags.Continuous, false);

						behavior.type = AI.Behavior.Type.Move;
						movement.position_target = data.position.HasValue ? region.GetNearestPathPos(data.position.Value, Terrain.PathFlags.Ground | Terrain.PathFlags.Ladder | Terrain.PathFlags.Edge, Terrain.PathFlags.None, Terrain.PathFlags.Solid) : null;
					}
				}
				break;

				case OrderType.Pickup:
				{
					if (is_target_valid)
					{
						movement.type = AI.Movement.Type.Follow;
						movement.flags.SetFlag(AI.Movement.Flags.Continuous, true);

						behavior.type = AI.Behavior.Type.Pickup;
						behavior.target_goal = (data.ent_target, data.h_component_target);
					}
					else
					{
						movement.type = AI.Movement.Type.Move;
						movement.flags.SetFlag(AI.Movement.Flags.Continuous, false);

						behavior.type = AI.Behavior.Type.Pickup;
					}
				}
				break;

				case OrderType.Use:
				{
					if (is_target_valid)
					{
						movement.type = AI.Movement.Type.Follow;
						movement.flags.SetFlag(AI.Movement.Flags.Continuous, true);

						behavior.type = AI.Behavior.Type.Use;
						behavior.target_goal = (data.ent_target, data.h_component_target);

						//ai.stance = AI.Stance.Defensive;
					}
					else
					{
						movement.type = AI.Movement.Type.Move;
						movement.flags.SetFlag(AI.Movement.Flags.Continuous, false);

						//ai.stance = AI.Stance.Defensive;
					}
				}
				break;

				case OrderType.Attack:
				{
					ref var target_threat = ref behavior.ref_target_threat.GetComponent();
					if (target_threat.IsNotNull())
					{
						if (target_threat.faction_id == 0 || target_threat.faction_id != faction.id)
						{
							//if (target_threat.flags.HasAny(Threat.Flags.Capturable))
							//{
							//	var oc_worker = entity.GetComponentWithOwner<Worker.Data>(Relation.Type.Instance);
							//	ref var worker = ref oc_worker.data;

							//	var ent_target = data.ent_target;

							//	//App.WriteLine(ent_target);

							//	if (!worker.IsNull())
							//	{
							//		worker.SetWorkTarget(oc_worker.entity, ent_target, 0, 0);

							//		behavior.type = AI.Behavior.Type.Capture;
							//		movement.type = AI.Movement.Type.Move;
							//		movement.flags.SetFlag(AI.Movement.Flags.Continuous, false);
							//		//behavior.ent_target_goal = ent_target;
							//	}
							//}
							//else
							{
								behavior.type = AI.Behavior.Type.Attack;
								movement.type = AI.Movement.Type.Follow;
								movement.flags.SetFlag(AI.Movement.Flags.Continuous, true);
							}
						}
						else
						{
							behavior.type = AI.Behavior.Type.Attack;
							movement.type = AI.Movement.Type.Move;
							movement.flags.SetFlag(AI.Movement.Flags.Continuous, false);
						}
					}
					else
					{
						behavior.type = AI.Behavior.Type.Attack;
						//movement.type = AI.Movement.Type.Move;
						//movement.type = behavior.movement_flags.HasAny(AI.Behavior.MovementFlags.Hold) ? AI.Movement.Type.None : AI.Movement.Type.Move;
						movement.type = AI.Movement.Type.Follow;
						movement.flags.SetFlag(AI.Movement.Flags.Continuous, false);
					}

					//if (is_target_valid)
					//{
					//	movement.flags.SetFlag(AI.Movement.Flags.Continuous, true);

					//	//ai.stance = AI.Stance.Aggressive;
					//}
					//else
					//{
					//	movement.type = AI.Movement.Type.Move;
					//	movement.flags.SetFlag(AI.Movement.Flags.Continuous, false);

					//	//behavior.type = AI.Behavior.Type.Attack;
					//	//behavior.flags.SetFlag(AI.Behavior.Flags.Aggressive, true);

					//	//ai.stance = AI.Stance.Aggressive;
					//}
				}
				break;

				case OrderType.Defend:
				{
					ref var target_threat = ref behavior.ref_target_threat.GetComponent();
					if (target_threat.IsNotNull())
					{
						if (target_threat.faction_id == 0 || target_threat.faction_id != faction.id)
						{

						}
						else
						{
							is_target_valid = false;
						}
					}

					if (is_target_valid)
					{
						movement.type = AI.Movement.Type.Follow;
						movement.flags.SetFlag(AI.Movement.Flags.Continuous, true);

						behavior.type = AI.Behavior.Type.Attack;
						behavior.target_goal = (data.ent_target, data.h_component_target);

						//ai.stance = AI.Stance.Defensive;
						//behavior.flags.SetFlag(AI.Behavior.Flags.Defensive, true);
					}
					else
					{
						movement.type = AI.Movement.Type.Move;
						movement.flags.SetFlag(AI.Movement.Flags.Continuous, false);

						//behavior.type = AI.Behavior.Type.Attack;
						//behavior.flags.SetFlag(AI.Behavior.Flags.Defensive, true);

						//ai.stance = AI.Stance.Defensive;
					}
				}
				break;

				case OrderType.Capture:
				{
					ref var target_threat = ref behavior.ref_target_threat.GetComponent();
					if (target_threat.IsNotNull())
					{
						if (target_threat.faction_id == 0 || target_threat.faction_id != faction.id)
						{
							if (target_threat.flags.HasAny(Threat.Flags.Capturable))
							{
								var oc_worker = entity.GetComponentWithOwner<Worker.Data>(Relation.Type.Instance);
								ref var worker = ref oc_worker.data;

								var ent_target = data.ent_target;

								//App.WriteLine(ent_target);

								if (worker.IsNotNull())
								{
									worker.SetWorkTarget(oc_worker.entity, ent_target, 0, 0);

									behavior.type = AI.Behavior.Type.Capture;
									movement.type = AI.Movement.Type.Move;
									movement.flags.SetFlag(AI.Movement.Flags.Continuous, false);
									//behavior.ent_target_goal = ent_target;
								}
							}
							else
							{
								behavior.type = AI.Behavior.Type.Attack;
								movement.type = AI.Movement.Type.Follow;
								movement.flags.SetFlag(AI.Movement.Flags.Continuous, true);
							}
						}
						else
						{
							behavior.type = AI.Behavior.Type.Attack;
							movement.type = AI.Movement.Type.Move;
							movement.flags.SetFlag(AI.Movement.Flags.Continuous, false);
						}
					}
					else
					{
						behavior.type = AI.Behavior.Type.Attack;
						movement.type = AI.Movement.Type.Move;
						movement.flags.SetFlag(AI.Movement.Flags.Continuous, false);
					}

					////ref var target_threat = ref behavior.ref_target_threat.GetValueOrNullRef();
					////if (!target_threat.IsNull())
					////{
					////	if (target_threat.faction_id == 0 || target_threat.faction_id != faction.id)
					////	{

					////	}
					////	else
					////	{
					////		is_target_valid = false;
					////	}
					////}

					//if (is_target_valid)
					//{
					//	movement.type = AI.Movement.Type.Move;
					//	movement.flags.SetFlag(AI.Movement.Flags.Continuous, true);
					//	//movement.flags.SetFlag(AI.Movement.Flags.Force_Repath, true);

					//	behavior.ref_target_threat.Set(default);
					//	behavior.ref_target_transform.Set(default);
					//	behavior.ref_target_body.Set(default);

					//	behavior.type = AI.Behavior.Type.Capture;

					//	//behavior.ent_target_goal = data.ent_target;

					//	//ai.stance = AI.Stance.Aggressive;
					//}
					//else
					//{
					//	movement.type = AI.Movement.Type.Move;
					//	movement.flags.SetFlag(AI.Movement.Flags.Continuous, false);
					//	//movement.flags.SetFlag(AI.Movement.Flags.Force_Repath, true);

					//	behavior.type = AI.Behavior.Type.Attack;

					//	//behavior.type = AI.Behavior.Type.Attack;
					//	//behavior.flags.SetFlag(AI.Behavior.Flags.Aggressive, true);

					//	//ai.stance = AI.Stance.Aggressive;
					//}
				}
				break;

				case OrderType.Harvest:
				{
					//ref var target_harvestable = ref behavior.ref_target_harvestable.GetValueOrNullRef();
					//if (!target_harvestable.IsNull())
					//{

					//}
					//else
					//{

					//}

					if (is_target_valid)
					{
						movement.type = AI.Movement.Type.Follow;
						movement.flags.SetFlag(AI.Movement.Flags.Continuous, true);

						behavior.type = AI.Behavior.Type.Harvest;
						behavior.target_goal = (data.ent_target, data.h_component_target);

						//ai.stance = AI.Stance.Defensive;
					}
					else
					{
						movement.type = AI.Movement.Type.Move;
						movement.flags.SetFlag(AI.Movement.Flags.Continuous, false);

						behavior.type = AI.Behavior.Type.Harvest;

						//ai.stance = AI.Stance.Defensive;
					}
				}
				break;

				case OrderType.Store:
				{
					if (is_target_valid)
					{
						movement.type = AI.Movement.Type.Move;
						movement.flags.SetFlag(AI.Movement.Flags.Continuous, false);

						behavior.type = AI.Behavior.Type.Store;
						behavior.target_goal = (data.ent_target, data.h_component_target);
					}
				}
				break;

				case OrderType.Work:
				{
					if (is_target_valid)
					{
						//movement.type = AI.Movement.Type.Follow;
						//movement.flags.RemoveFlag(AI.Movement.Flags.Continuous);

						//behavior.type = AI.Behavior.Type.Work;
						//behavior.target_goal = (data.ent_target, data.h_component_target);

						// App.WriteLine("worker target", color: App.Color.Magenta);


						var oc_worker = entity.GetComponentWithOwner<Worker.Data>(Relation.Type.Instance);
						ref var worker = ref oc_worker.data;

						var ent_target = data.ent_target;

						//App.WriteLine(ent_target);

						if (worker.IsNotNull())
						{
							//worker.SetWorkTarget(oc_worker.entity, ent_target, 0, 0);

							movement.type = AI.Movement.Type.Follow;
							movement.flags.RemoveFlag(AI.Movement.Flags.Continuous);

							behavior.type = AI.Behavior.Type.Work;
							behavior.target_goal = (data.ent_target, data.h_component_target);

							ref var worker_state = ref oc_worker.entity.GetComponent<Worker.State>();
							ref var workshop = ref ent_target.GetComponent<Workshop.Data>();
							if (worker_state.IsNotNull() && workshop.IsNotNull())
							{
								//App.WriteLine("worker working");


								if (worker.ref_order.ComponentID != 0 && worker_state.work_index.HasValue) //worker_state.last_order_flags.HasAny(Crafting.Order.Flags.In_Progress))
								{

								}
								else
								{
									//App.WriteLine("worker working");

									var span_status_flags = workshop.order_status_flags.AsSpan(workshop.orders_capacity);
									span_status_flags.GetFirstOrNull(static (x) => x.HasAny(Workshop.StatusFlags.Needs_Workers), out var index);
									if (index >= 0)
									{
										var h_order_target = IComponent.Handle<Crafting.Order>.FromIndexPair((uint)index);

										Worker.SetWorkTarget(worker: ref worker, ent_worker: oc_worker.entity, ent_target: ent_target, h_order: h_order_target);
										worker_state.next_check = default;
										worker_state.next_work = default;

										behavior.target_goal = (ent_target, h_order_target);
									}
								}
							}


							//behavior.type = AI.Behavior.Type.Capture;
							//movement.type = AI.Movement.Type.Move;
							//movement.flags.SetFlag(AI.Movement.Flags.Continuous, false);
							//behavior.ent_target_goal = ent_target;
						}

						//ai.stance = AI.Stance.Defensive;
					}
					else
					{
						movement.type = AI.Movement.Type.Move;
						movement.flags.RemoveFlag(AI.Movement.Flags.Continuous);

						//ai.stance = AI.Stance.Defensive;
					}
				}
				break;

				case OrderType.Drive:
				{
					if (is_target_valid)
					{
						ref var vehicle = ref data.ent_target.GetComponent<Vehicle.Data>();
						if (vehicle.IsNotNull())
						{
							movement.type = AI.Movement.Type.Follow;
							movement.flags.RemoveFlag(AI.Movement.Flags.Continuous);

							behavior.type = AI.Behavior.Type.Drive;
							behavior.target_goal = (data.ent_target, data.h_component_target);
							//ai.stance = AI.Stance.Defensive;
						}
					}
					else
					{
						movement.type = AI.Movement.Type.Move;
						movement.flags.RemoveFlag(AI.Movement.Flags.Continuous);

						//ai.stance = AI.Stance.Defensive;
					}
				}
				break;
			}

			if (data.unit_count > 1)
			{
				movement.next_update = info.WorldTime + (random.NextFloatRange(0.10f, 0.30f) * data.unit_index);
			}

			movement.flags.AddFlag(AI.Movement.Flags.Force_Repath);
			//App.WriteLine("order");
			//movement_override = movement;
			//behavior_override = behavior;

			movement.Sync(entity, true);
			behavior.Sync(entity, true);
		}
#endif
	}
}
