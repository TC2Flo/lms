
namespace TC2.Base.Components
{
	public static partial class Squad
	{
		public static bool enable_create_squad_menu = false;

		// TODO: optimize memory layout
		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public partial struct Data(): IComponent
		{
			[Flags]
			public enum Flags: ushort
			{
				None = 0,

				[Asset.Ignore] Force = 1 << 0,

				Hidden = 1 << 1,
				Locked = 1 << 2,
				Public = 1 << 3,

				No_Unit_Remove = 1 << 8,
			}

			[Asset.Ignore] public Vec2f position_current;
			[Asset.Ignore] public Vec2f position_goal;

			[Asset.Ignore] public Vector2? position_a;
			[Asset.Ignore] public Vector2? position_b;

			[Asset.Ignore] public Entity ent_target;
			[Asset.Ignore] public Entity ent_follow;
			[Asset.Ignore] public Entity ent_storage;
			[Asset.Ignore] public Entity ent_home;

			public Color32BGRA color; // = Color32BGRA.White;

			//public float 

			[Save.NewLine]
			public NPC.ItemHints item_hints_prefer;
			public AI.Behavior.CombatFlags combat_flags;
			public AI.Behavior.MovementFlags movement_flags;
			public Squad.Data.Flags flags;


			[Save.NewLine]
			public Commandable.OrderType order_type;
			[Editor.Slider.Clamped(0, 8)] public byte max_size = 4;
			[Asset.Ignore, Save.Ignore] public byte unit_count;
			[Asset.Ignore, Save.Ignore] public byte idle_count;

			[Save.NewLine]
			public float money;
			public IBankAccount.Handle h_bank;
			private ushort unused_00;

			// TODO: probably remove this
			[Asset.Ignore, Save.Ignore] public FixedArray8<Vector2> positions;

			[Asset.Ignore, Save.Ignore, Net.Ignore] public float t_next_order;
			[Asset.Ignore, Save.Ignore, Net.Ignore] public float t_last_order;

			[Net.Ignore, Save.Ignore, Asset.Ignore] private ulong unused_01;
		}

#if SERVER
		[ISystem.AddFirst(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("initialized", true, Source.Modifier.Owned)]
		public static void OnAdd(ISystem.Info info, ref Region.Data region, ref XorRandom random, Entity ent_squad, 
		[Source.Owned] ref Squad.Data squad, [Source.Owned] ref Transform.Data transform, [Source.Owned, Optional] in Faction.Data faction)
		{
			// App.WriteLine($"on add squad; {transform.GetPositionOld()}, {transform.GetPositionNew()}");

			squad.color = faction.id.GetColorA().WithAlpha(200);

			Squad.SetOrder(region: ref region,
				random: ref random,
				oc_squad: new(ref squad, ent_squad),
				position_a: squad.position_a ?? transform.position,
				type: Commandable.OrderType.Move,
				flags: Squad.Data.Flags.Force);
		}
#endif

		[ISystem.PreUpdate.Reset(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnReset([Source.Owned] ref Squad.Data squad)
		{
			//squad.position_mid = default;
			squad.unit_count = 0;
			squad.idle_count = 0;
			//transform.position = Vector2.Lerp(transform.position, ((squad.position_a ?? transform.position) + (squad.position_b ?? transform.position)) * 0.50f, 0.40f);
			squad.position_current = Vector2.Lerp(squad.position_current, squad.position_a ?? squad.position_current, 0.40f);
		}

		[ISystem.VeryLateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnVeryLateUpdate([Source.Owned] ref Squad.Data squad)
		{
			//if (squad.flags.HasAny(Data.Flags.Force))
			//{
			//	App.WriteLine("force");
			//}
			squad.flags.RemoveFlag(Squad.Data.Flags.Force);
		}

		[Shitcode]
		[ISystem.PreUpdate.Overrides.Apply(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnUpdate_UnitsA(ISystem.Info info, ref Region.Data region,
		[Source.Owned, Original] ref AI.Behavior behavior_original, [Source.Owned, Override] ref AI.Behavior behavior_override,
		[Source.Parent<Squad.Relation>] ref Squad.Data squad, [Source.Owned, Pair.First] ref Squad.Relation squad_rel)
		{
			behavior_override.movement_flags = squad.movement_flags;
			behavior_override.combat_flags = squad.combat_flags;
			behavior_override.item_hints_prefer = squad.item_hints_prefer;
			behavior_original.position_goal = squad.position_goal;

			//if (squad.unit_count == 0)
			//{
			//	squad_transform.position = body.GetPosition();
			//}
			//else
			//{
			//	squad_transform.position += body.GetPosition();
			//	squad_transform.position *= 0.50f;
			//}

			//squad_transform.position = region.GetNearestHighPathPos(squad_transform.position, Terrain.PathFlags.Ground | Terrain.PathFlags.Ladder);
			//squad_transform.position = region.GetNearestPathPos(squad_transform.position, Terrain.PathFlags.Ground | Terrain.PathFlags.Ladder);

			squad_rel.index = (uint)squad.unit_count;

			squad.unit_count++;
			if (behavior_original.type == AI.Behavior.Type.Idle)
			{
				squad.idle_count++;
			}
		}

#if SERVER
		[ISystem.AddFirst(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("dead", true, Source.Modifier.Owned)]
		public static void OnDeath(ISystem.Info info, ref Region.Data region, Entity ent_organic,
		[Source.Owned, Original] ref Organic.Data organic,
		[Source.Parent<Squad.Relation>] in Squad.Data squad)
		{
			ent_organic.RemoveRel<Squad.Relation>(Entity.Wildcard);
		}

		[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		[HasComponent<Player.Data>(Source.Modifier.Shared, false), HasTag("dead", false, Source.Modifier.Owned)]
		public static void OnUpdate_UnitsB(ISystem.Info info, ref Region.Data region, ref XorRandom random, Entity ent_commandable, Entity ent_squad,
		[Source.Owned] ref Body.Data body, [Source.Owned] ref Commandable.Data commandable,
		[Source.Owned, Original] ref AI.Behavior behavior_original, [Source.Owned, Override] ref AI.Behavior behavior_override,
		//[Source.Owned, Original] ref NPC.Data npc_riginal, [Source.Owned, Override] ref NPC.Data npc_override,
		//[Source.Owned, Original] ref AI.Data ai_original, [Source.Owned, Override] ref AI.Data ai_override,
		[Source.Owned, Pair.First] ref Squad.Relation squad_rel,
		[Source.Parent<Squad.Relation>] ref Squad.Data squad)
		{
			var force = false;
			if ((info.WorldTime >= squad_rel.t_next_update && squad_rel.t_last_order < squad.t_last_order) || (force = (squad_rel.flags.HasAny(Squad.Relation.Flags.Dirty) | squad.flags.HasAny(Squad.Data.Flags.Force)))) // || squad_rel.t_last_order < squad.t_last_order)
			{
				squad_rel.t_next_update = info.WorldTime + random.NextFloatRange(0.40f, 1.00f);

				var pos = default(Vector2?);
				if (squad.positions[squad_rel.index] != Vector2.Zero) pos = squad.positions[squad_rel.index];

				switch (behavior_override.type)
				{
					case AI.Behavior.Type.Undefined:
					case AI.Behavior.Type.Idle:
					case AI.Behavior.Type.Move:
					{
						var dist_sq = Vector2.DistanceSquared(pos ?? squad.position_current, body.GetPosition());
						var req_distance_sq = (1 * 1);

						if (force || dist_sq >= req_distance_sq)
						{
							var ev = new Commandable.OrderEvent()
							{
								type = Commandable.OrderType.Move,
								position = pos
								//position = region.GetWanderPos(transform_squad.position + random.NextUnitVector2Range(0, 1), random.NextFloatRange(1, 3) * squad.unit_count, air: false, include: Terrain.PathFlags.Ground, require: Terrain.PathFlags.Ground, exclude: Terrain.PathFlags.Solid | Terrain.PathFlags.Ladder | Terrain.PathFlags.Wall) + random.NextUnitVector2Range(0, 1)
								//position = region.GetNearestPathPos(region.GetNearestHighPathPos(transform_squad.position + random.NextUnitVector2Range(1, 4), Terrain.PathFlags.Ground) + random.NextUnitVector2Range(0, 2), Terrain.PathFlags.Ground | Terrain.PathFlags.Ladder) + random.NextUnitVector2Range(0, 2)
							};
							ent_commandable.TriggerEventDeferred(ev, sync: false);

							//App.WriteLine("override");
						}
					}
					break;

					case AI.Behavior.Type.Capture:
					case AI.Behavior.Type.Defend:
					case AI.Behavior.Type.Attack:
					{
						if (force || behavior_override.ref_target_threat.entity == 0)
						{
							var ev = new Commandable.OrderEvent()
							{
								type = Commandable.OrderType.Move,
								position = pos
								//position = region.GetNearestPathPos(region.GetNearestHighPathPos(transform_squad.position + random.NextUnitVector2Range(1, 4), Terrain.PathFlags.Ground) + random.NextUnitVector2Range(0, 2), Terrain.PathFlags.Ground | Terrain.PathFlags.Ladder) + random.NextUnitVector2Range(0, 2)
								//position = region.GetWanderPos(transform_squad.position + random.NextUnitVector2Range(0, 1), random.NextFloatRange(1, 3) * squad.unit_count, air: false, include: Terrain.PathFlags.Ground, require: Terrain.PathFlags.Ground, exclude: Terrain.PathFlags.Solid | Terrain.PathFlags.Ladder | Terrain.PathFlags.Wall) + random.NextUnitVector2Range(0, 1)
							};
							ent_commandable.TriggerEventDeferred(ev, sync: false);
						}
					}
					break;
				}

				//ai_behavior_original.combat_flags = squad.combat_flags;
				//ai_behavior_original.movement_flags = squad.movement_flags;
				//ai_behavior_original.target_flags = squad.target_flags;

				//if (squad.combat_flags.HasAny(Data.CombatFlags.Attack_Visible))
				//{
				//	ai_original.stance = AI.Stance.Aggressive;
				//}

				//switch (squad.order_type)
				//{
				//	case Commandable.OrderType.Attack:
				//	{
				//		ai_original.stance = AI.Stance.Aggressive;
				//	}
				//	break;

				//	case Commandable.OrderType.Defend:
				//	{
				//		ai_original.stance = AI.Stance.Aggressive;
				//		//ai_behavior_original.type = AI.Behavior.Type.Defend;
				//	}
				//	break;

				//	case Commandable.OrderType.Capture:
				//	{
				//		ai_original.stance = AI.Stance.Aggressive;
				//		//ai_behavior_original.type = AI.Behavior.Type.Capture;
				//	}
				//	break;
				//}

				squad_rel.flags.RemoveFlag(Squad.Relation.Flags.Dirty);
				squad_rel.t_last_order = squad.t_last_order;
			}



			//npc_override.wander_radius = 4;
			//ai_behavior_override.position_pivot = squad_transform.position;

			//#if SERVER
			//			region.DrawDebugCircle(ai_behavior_override.position_pivot, 1.00f, squad.color);
			//#endif
		}
#endif

		//[ISystem.VeryEarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		//public static void OnUpdate(ISystem.Info info, Entity ent_squad_rel, Entity ent_squad, ref XorRandom random, ref Region.Data region,
		//[Source.Owned] ref Body.Data body, [Source.Owned, Pair.Any] ref Squad.Relation squad_rel, [Source.Parent<Squad.Relation>] ref Squad.Data squad)
		//{

		//}

		[IRelation.Data(Net.SendType.Unreliable, IRelation.Flags.Unique | IRelation.Flags.Acyclic | IRelation.Flags.DontInherit | IRelation.Flags.Traversable, region_only: true)]
		public partial struct Relation(): IRelation
		{
			[Flags]
			public enum Flags: uint
			{
				None = 0,

				Dirty = 1u << 0
			}

			public uint index;
			public Squad.Relation.Flags flags = Squad.Relation.Flags.Dirty;
			[Save.Ignore, Net.Ignore] public float t_last_order;
			[Save.Ignore, Net.Ignore] public float t_next_update;
		}

		[Shitcode]
		public partial struct DEV_SetOrderRPC: Net.IRPC<Squad.Data>
		{
			public Entity ent_target;
			public IComponent.Handle h_component_target;

			public Squad.Data.Flags flags;

			public Vector2? position_a;
			public Vector2? position_b;

			public required Commandable.OrderType order_type;
			//public Commandable.OrderFlags flags;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Data data)
			{
				ref var region = ref rpc.connection.GetRegion();
				ref var player = ref rpc.connection.GetPlayer();

				if (region.IsNotNull() && player.IsNotNull() && rpc.entity.GetFactionHandle() == player.h_faction)
				{
					SetOrder(region: ref region,
						random: ref region.GetRandom(),
						oc_squad: new(ref data, rpc.entity),
						position_a: this.position_a,
						position_b: this.position_b,
						ent_target: this.ent_target,
						flags: this.flags,
						type: this.order_type.OrDefault(Commandable.OrderType.Move));

					//App.WriteLine("move");
					Sound.PlayGUI(ref rpc.connection, "ui.misc.04", volume: 0.45f, pitch: 2.00f);
				}
			}
#endif
		}

#if SERVER
		[Shitcode]
		public static void SetOrder(ref Region.Data region, ref XorRandom random, OwnedComponent<Squad.Data> oc_squad,
		Vector2? position_a = default, Vector2? position_b = default, Entity ent_target = default, Squad.Data.Flags flags = default, 
		Commandable.OrderType type = Commandable.OrderType.Undefined)
		{
			// App.WriteLine($"SetOrder: {position_a}; {position_b}; {ent_target}; {type}", App.Color.Magenta);

			ref var squad = ref oc_squad.data;
			if (squad.IsNotNull())
			{
				var positions_span = squad.positions.AsSpan();
				positions_span.Clear();

				if (position_a.TryGetValue(out var pos_a) && region.TryGetAnyValidPathPos(ref pos_a, include: Terrain.PathFlags.Ground | Terrain.PathFlags.Ladder | Terrain.PathFlags.Edge, exclude: Terrain.PathFlags.Solid))
				{
					position_a = pos_a;

					var i = 0;
					if (position_b.TryGetValue(out var pos_b) && !Vec2f.IsInDistance(pos_a, pos_b, 2.00f) 
					&& region.TryGetAnyValidPathPos(pos: ref pos_b,
					include: Terrain.PathFlags.Ground | Terrain.PathFlags.Ladder | Terrain.PathFlags.Edge,
					exclude: Terrain.PathFlags.Solid))
					{
						position_b = pos_b;

						foreach (ref var pos in positions_span)
						{
							var pos_tmp = Maths.Lerp(pos_a, pos_b, random.NextFloat01());
							if (!region.TryGetAnyValidPathPos(pos: ref pos_tmp,
								include: Terrain.PathFlags.Ground | Terrain.PathFlags.Ladder | Terrain.PathFlags.Edge,
								exclude: Terrain.PathFlags.Solid)) pos_tmp = pos_b;

							pos = pos_tmp;

							i++;
						}
					}
					else
					{
						position_b = null;

						foreach (ref var pos in positions_span)
						{
							var pos_tmp = pos_a + random.NextUnitVector2Range(new Vector2(0.75f, 1.00f) * (i * 1.20f), new Vector2(0.00f, 0.25f));
							if (!region.TryGetAnyValidPathPos(pos: ref pos_tmp,
								include: Terrain.PathFlags.Ground | Terrain.PathFlags.Ladder | Terrain.PathFlags.Edge,
								exclude: Terrain.PathFlags.Solid)) pos_tmp = pos_a;

							pos = pos_tmp;

							i++;
						}
					}
				}
				else
				{
					position_a = null;
					position_b = null;
				}

				squad.position_a = position_a;
				squad.position_b = position_b;

				squad.position_goal = position_b ?? position_a ?? default;
				//squad.position_goal = position_a ?? position_b ?? default;

				squad.ent_target = ent_target;
				squad.t_last_order = region.GetWorldTime();

				//data.order_type = this.order_type;
				//data.flags = this.flags;

				squad.flags.SetFlagMasked(flags, Squad.Data.Flags.Force);

				squad.Sync(oc_squad.entity, true);
			}
		}
#endif

		//		public partial struct DEV_TransferMoneyRPC: Net.IRPC<Squad.Data>
		//		{
		//			public float amount;

		//			public Entity ent_src;
		//			public Entity ent_dst;

		//#if SERVER
		//			public void Invoke(Net.IRPC.Context rpc, ref Data data)
		//			{
		//				ref var region = ref connection.GetRegion();
		//				ref var player = ref connection.GetPlayer();

		//				var h_faction = player.faction_id;

		//				try
		//				{
		//					Assert.Check(region.IsNotNull(), "Region is null.", Assert.Level.Error);
		//					Assert.Check(player.IsNotNull(), "Player is null.", Assert.Level.Error);
		//					Assert.Check(this.ent_src != this.ent_dst, "Cannot transfer money to iself.", Assert.Level.Error);
		//					Assert.Check(this.ent_src == entity || this.ent_dst == entity, "Entity must be either ent_src or ent_dst.", Assert.Level.Error);
		//					Assert.Check(this.ent_src.IsAlive(), "ent_src must be alive.", Assert.Level.Error);
		//					Assert.Check(this.ent_dst.IsAlive(), "ent_dst must be alive.", Assert.Level.Error);

		//					var h_faction_src = this.ent_src.GetFaction();
		//					var h_faction_dst = this.ent_dst.GetFaction();

		//					//Assert.Check(h_faction == h_faction_src, "Player faction must belong to same faction as ent_src.", Assert.Level.Error);
		//					Assert.Check(h_faction_dst == 0 || h_faction_dst == h_faction, "ent_dst's faction must be either null or same as ent_src's faction.", Assert.Level.Error);

		//					var oc_money_src = this.ent_src.GetComponentWithOwner<Money.Data>(Keg.Engine.Game.Relation.Type.Instance, true);
		//					var oc_money_dst = this.ent_dst.GetComponentWithOwner<Money.Data>(Keg.Engine.Game.Relation.Type.Instance, true);

		//					Assert.Check(oc_money_src.IsValid(), "oc_money_src is not valid.", Assert.Level.Error);
		//					Assert.Check(oc_money_dst.IsValid(), "oc_money_dst is not valid.", Assert.Level.Error);

		//					var amount_clamped = Maths.Clamp(this.amount, 0.00f, oc_money_src.data.amount);
		//					if (Assert.Check(amount_clamped > 0.00f, "Clamped money amount must be higher than 0.00f.", Assert.Level.Warn))
		//					{
		//						oc_money_src.data.amount -= amount;
		//						oc_money_src.Sync(true);

		//						oc_money_dst.data.amount += amount;
		//						oc_money_dst.Sync(true);
		//					}
		//				}
		//				catch (Exception e)
		//				{
		//					App.WriteExceptionText("Failed to transfer money!", e);
		//				}
		//			}
		//#endif
		//		}

		public partial struct DEV_EditRPC: Net.IRPC<Squad.Data>
		{
			public Color32BGRA? color;

			public Squad.Data.Flags? flags;
			public AI.Behavior.MovementFlags? movement_flags;
			public AI.Behavior.CombatFlags? combat_flags;
			public NPC.ItemHints? item_hints;

			public Entity? ent_target;
			public Entity? ent_follow;
			public Entity? ent_storage;
			public Entity? ent_home;

#if SERVER
			// TODO: add better validation
			public void Invoke(Net.IRPC.Context rpc, ref Data data)
			{
				ref var region = ref rpc.connection.GetRegion();
				ref var player = ref rpc.connection.GetPlayer();

				if (region.IsNotNull() && player.IsNotNull() && rpc.entity.GetFactionHandle() == player.h_faction)
				{
					this.color.TryCopyTo(ref data.color);
					this.flags.TryCopyTo(ref data.flags);
					this.movement_flags.TryCopyTo(ref data.movement_flags);
					this.combat_flags.TryCopyTo(ref data.combat_flags);
					this.item_hints.TryCopyTo(ref data.item_hints_prefer);

					this.ent_target.TryCopyTo(ref data.ent_target);
					this.ent_follow.TryCopyTo(ref data.ent_follow);
					this.ent_storage.TryCopyTo(ref data.ent_storage);
					this.ent_home.TryCopyTo(ref data.ent_home);

					data.Sync(rpc.entity, true);
				}
			}
#endif
		}

#if CLIENT
		public partial struct SquadGUI: IGUICommand
		{
			public Entity ent_squad;
			public Squad.Data squad;
			//public Transform.Data transform;

			public void Draw()
			{
				ref var region = ref this.ent_squad.GetRegionCommon();
				if (region.IsCurrent())
				{
					ref var player = ref Client.GetPlayer();

					var pos = region.WorldToCanvas(this.squad.position_current);
					var rect_canvas = GUI.GetCanvasRect().Pad(16, 16, 16, 16);
					pos = rect_canvas.ClipPoint(pos);

					using (var window = GUI.Window.HUD($"squad.{this.ent_squad}", position: pos, size: new Vector2(200, 64)))
					{
						if (window.show)
						{
							GUI.TitleCentered(this.ent_squad.GetName(), pivot: new(0.50f, 1.00f), color: this.squad.color);
							//GUI.DrawWindowBackground(GUI.tex_window_menu);

							var hovered = false;
							var selected = false;

							ref var selection = ref player.ent_player.GetComponent<Selection.Data>();
							if (selection.IsNotNull())
							{
								selected = selection.ref_selected_squad.entity == this.ent_squad;
							}

							if (GUI.IsHoveringRect(AABB.Circle(pos, 12.00f), clip: false, window_only: false))
							{
								hovered = true;
								GUI.SetCursor(App.CursorType.Hand, 10);

								if (GUI.GetMouse().GetKeyDown(Mouse.Key.Left))
								{
									var rpc = new Selection.SelectSquadRPC()
									{
										ent_squad = this.ent_squad
									};
									rpc.Send(player.ent_player);
								}
							}

							if (this.squad.position_b.HasValue)
							{
								var pos_b = region.WorldToCanvas(this.squad.position_b.Value);

								var dir = (this.squad.position_b.Value - this.squad.position_current).GetNormalized(out var len);
								if (len > 2.00f)
								{
									GUI.DrawLine(pos, pos_b - (dir * 3), color: this.squad.color.WithAlphaMult(selected ? 0.50f : 0.25f), thickness: 2);
									GUI.DrawLine(pos_b, pos_b - (dir.RotateByDeg(-30) * 20), color: this.squad.color.WithAlphaMult(selected ? 0.50f : 0.25f), thickness: 2);
									GUI.DrawLine(pos_b, pos_b - (dir.RotateByDeg(30) * 20), color: this.squad.color.WithAlphaMult(selected ? 0.50f : 0.25f), thickness: 2);
								}
							}
							GUI.DrawCircleFilled(pos, selected ? 12.00f : 8.00f, color: hovered ? GUI.col_white : this.squad.color.WithAlphaMult(selected ? 1.00f : 0.45f), segments: 16);
						}
					}
				}
			}
		}

		//[ISystem.VeryEarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region)]
		//public static void OnGUI1(ISystem.Info info, ref Region.Data region,
		//[Source.Owned] in Transform.Data transform,
		//[Source.Parent<Squad.Relation>] in Squad.Data squad, [Source.Parent<Squad.Relation>] in Transform.Data squad_transform)
		//{
		//	//GUI.DrawCircleFilled(transform.position.WorldToCanvas(), 4.00f, color: squad.color.WithAlphaMult(0.75f), segments: 5);
		//	//GUI.DrawLine2(transform.position.WorldToCanvas(), squad_transform.position.WorldToCanvas(), color_a: squad.color.WithAlphaMult(0.50f), color_b: squad.color.WithAlphaMult(0.10f), thickness_a: 5.00f, thickness_b: 0.00f, layer: GUI.Layer.Background);
		//}

		[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnGUI2(Entity entity,
		[Source.Owned] in Squad.Data squad,
		[Source.Owned] in Faction.Data faction)
		{
			if (faction.id == Client.GetFactionHandle())
			{
				var gui = new SquadGUI()
				{
					ent_squad = entity,
					squad = squad
				};
				gui.Submit();
			}

			//if (faction.IsNull() || faction.id == Client.GetFactionHandle())
			//{
			//	var gui = new SquadGUI()
			//	{
			//		ent_squad = entity,
			//		squad = squad,
			//		transform = transform
			//	};
			//	gui.Submit();
			//}
		}
#endif
	}
}
