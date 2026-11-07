
namespace TC2.Base.Components
{
	public static partial class Selection
	{
		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region), IComponent.AddTo<Player.Data>()]
		public partial struct Data(): IComponent
		{
			[Asset.Ignore] public Commandable.OrderType order_type = Commandable.OrderType.Move;
			[Asset.Ignore] public EntRef<Squad.Data> ref_selected_squad;

			[Asset.Ignore, Save.Ignore] public Vector2 a;
			[Asset.Ignore, Save.Ignore] public Vector2 b;

			[Asset.Ignore, Save.Ignore] public FixedArray4<Entity> selected_units;

			//[Save.Ignore] public FixedArray4<EntRef<Commandable.Data>> units;
		}

#if SERVER
		public static void CreateSquad(ref Region.Data region, Entity ent_selection, Vector2 world_position, IFaction.Handle h_faction = default)
		{
			region.SpawnPrefab("squad", position: world_position, faction_id: h_faction).ContinueWith((ent_squad) =>
			{
				var random = XorRandom.New(true);

				ref var squad = ref ent_squad.GetComponent<Squad.Data>();
				if (squad.IsNotNull())
				{
					squad.color = Color32BGRA.FromHSV(random.NextFloatRange(0.00f, 3.00f), 0.80f, 1.00f);
					//squad.Sync(ent_squad, true);
				}

				ref var selection = ref ent_selection.GetComponent<Selection.Data>();
				if (selection.IsNotNull())
				{
					selection.ref_selected_squad.Set(ent_squad);
					selection.Sync(ent_selection, true);
				}
			});
		}
#endif

		// TODO: remove DEV_ prefix
		public partial struct DEV_AddToSquadRPC: Net.IRPC<Selection.Data>
		{
			public Entity ent_unit;
			public Entity ent_squad;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Selection.Data data)
			{
				ref var region = ref rpc.connection.GetRegion();
				ref var player = ref rpc.connection.GetPlayer();

				if (region.IsNotNull() && player.IsNotNull() && rpc.entity == rpc.connection.GetEntity() && this.ent_unit.GetFactionHandle() == player.h_faction && (!this.ent_squad.IsAlive() || this.ent_squad.GetFactionHandle() == player.h_faction))
				{
					if (this.ent_squad.IsAlive())
					{
						this.ent_unit.AddRel<Squad.Relation>(this.ent_squad);
					}
					else
					{
						this.ent_unit.RemoveRel<Squad.Relation>(Entity.Wildcard);
					}
				}
			}
#endif
		}

		//		public partial struct DEV_CreateSquadRPC: Net.IRPC<Selection.Data>
		//		{
		//#if SERVER
		//			public void Invoke(Net.IRPC.Context rpc, ref Selection.Data data)
		//			{
		//				if (!Assert.Check(Squad.enable_create_squad_menu, Assert.Level.Warn)) return;

		//				ref var region = ref rpc.connection.GetRegion();
		//				ref var player = ref rpc.connection.GetPlayer();
		//				ref var player_comp = ref rpc.connection.GetPlayerData();

		//				if (region.IsNotNull() && player.IsNotNull() && player_comp.IsNotNull() && rpc.entity == rpc.connection.GetEntity())
		//				{
		//					var pos = player_comp.control.mouse.position;
		//					if (player_comp.ent_controlled.IsAlive())
		//					{
		//						ref var transform = ref player_comp.ent_controlled.GetComponent<Transform.Data>();
		//						if (transform.IsNotNull())
		//						{
		//							pos = transform.position;
		//						}
		//					}

		//					if (region.TryGetHighPathNodeAtLand(pos, out var node))
		//					{
		//						pos = node.WorldPosition;
		//					}

		//					CreateSquad(ref region, rpc.entity, world_position: pos, h_faction: player.h_faction);
		//				}
		//			}
		//#endif
		//		}

		public partial struct SelectSquadRPC: Net.IRPC<Selection.Data>
		{
			public Entity ent_squad;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Selection.Data data)
			{
				ref var region = ref rpc.connection.GetRegion();
				ref var player = ref rpc.connection.GetPlayerData();

				if (region.IsNotNull() && player.IsNotNull() && rpc.entity == player.ent_player && (!this.ent_squad.IsAlive() || this.ent_squad.GetFactionHandle() == player.faction_id))
				{
					data.ref_selected_squad.Set(this.ent_squad);
					data.Sync(rpc.entity, true);
				}
			}
#endif
		}

		//public partial struct InteractEvent: IEvent
		//{
		//	public EntRef<work>
		//}

		//		public partial struct SetOrderTypeRPC: Net.IRPC<Selection.Data>
		//		{
		//			public Commandable.OrderType order_type;

		//#if SERVER
		//			public void Invoke(Net.IRPC.Context rpc, ref Data data)
		//			{
		//				data.order_type = this.order_type;

		//				data.Sync(entity);
		//			}
		//#endif
		//		}

		public partial struct SelectRPC: Net.IRPC<Selection.Data>
		{
			public Vector2 a;
			public Vector2 b;

			public FixedArray4<Entity> units;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Data data)
			{
				ref var region = ref rpc.entity.GetRegion();
				ref var player = ref rpc.connection.GetPlayer();

				if (region.IsNotNull() && player.IsNotNull() && rpc.entity == rpc.connection.GetEntity())
				{
					data.a = this.a;
					data.b = this.b;

					var index = 0;

					data.selected_units = default;
					foreach (var ent_unit in this.units)
					{
						if (ent_unit.IsAlive() && ent_unit.GetFactionHandle() == player.h_faction && ent_unit.HasComponent<Commandable.Data>() && !ent_unit.HasTag("dead") && !ent_unit.HasRel<Squad.Relation>(Entity.Wildcard))
						{
							data.selected_units[index++] = ent_unit;
						}
					}

					//if (index == 0)
					//{
					//	data.ref_selected_squad.Reset();
					//}

					data.Sync(rpc.entity, true);
				}
			}
#endif
		}

#if SERVER
		//[ISystem.VeryLateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		//public static void OnUpdate(ISystem.Info info, Entity entity, ref XorRandom random, ref Region.Data region, [Source.Owned] ref Control.Data control, [Source.Owned] ref Selection.Data selection)
		//{
		//	if (control.mouse.GetKeyDown(Mouse.Key.Right))
		//	{
		//		var pos = control.mouse.position;
		//		var normal = new Vector2(0, 1);

		//		var radius = 0.125f;
		//		var ent_target = default(Entity);
		//		var mask = default(Physics.Layer);
		//		var exclude = default(Physics.Layer);
		//		var query_flags = Physics.QueryFlag.Static | Physics.QueryFlag.Dynamic;

		//		switch (selection.order_type)
		//		{
		//			case Commandable.OrderType.Move:
		//			{
		//				mask = Physics.Layer.Entity;
		//				exclude = Physics.Layer.Building | Physics.Layer.Static;
		//				//query_flags = Physics.QueryFlag.Dynamic;
		//			}
		//			break;

		//			case Commandable.OrderType.Pickup:
		//			{
		//				mask = Physics.Layer.Holdable;
		//				exclude = Physics.Layer.Building | Physics.Layer.Static;
		//			}
		//			break;

		//			case Commandable.OrderType.Use:
		//			{
		//				mask = Physics.Layer.Interactable;
		//				radius = 0.500f;
		//			}
		//			break;

		//			case Commandable.OrderType.Attack:
		//			{
		//				mask = Physics.Layer.Destructible;
		//			}
		//			break;

		//			case Commandable.OrderType.Defend:
		//			{
		//				mask = Physics.Layer.Destructible;
		//			}
		//			break;

		//			case Commandable.OrderType.Harvest:
		//			{
		//				mask = Physics.Layer.Destructible;
		//			}
		//			break;

		//			case Commandable.OrderType.Capture:
		//			{
		//				mask = Physics.Layer.Destructible;
		//			}
		//			break;

		//			case Commandable.OrderType.Store:
		//			{
		//				mask = Physics.Layer.Storage;
		//			}
		//			break;
		//		}

		//		if (region.TryOverlapPoint(pos, radius, out var overlap_result, mask: mask, exclude: exclude, query_flags: query_flags))
		//		{
		//			ent_target = overlap_result.entity;
		//		}

		//		//var material_type = Material.Type.None;

		//		//if (!ent_target.IsAlive())
		//		//{
		//		//	if (region.TryLinecast(pos, pos + new Vector2(0, 2), 0.125f, out var linecast_result, mask: Physics.Layer.World | Physics.Layer.Solid, query_flags: Physics.QueryFlag.Static))
		//		//	{
		//		//		//pos = linecast_result.world_position + (linecast_result.normal * 1.00f);
		//		//		normal = linecast_result.normal;
		//		//		//material_type = linecast_result.material_type;
		//		//	}
		//		//}

		//		var commandable_count = 0;
		//		foreach (ref var ref_commandable in selection.selected_units.AsSpan())
		//		{
		//			if (ref_commandable.TryGetComponent(out var h_commandable))
		//			{
		//				commandable_count++;
		//			}
		//		}

		//		var ent_target_root = ent_target.GetRoot(Relation.Type.Child);

		//		var index = 0;
		//		foreach (ref var ref_commandable in selection.selected_units.AsSpan())
		//		{
		//			if (ref_commandable.TryGetHandle(out var h_commandable))
		//			{
		//				//var dir = ((pos + new Vector2(0, 10)) - pos).GetNormalized(out var dist);

		//				switch (selection.order_type)
		//				{
		//					case Commandable.OrderType.Move:
		//					{
		//						var ev = new Commandable.OrderEvent()
		//						{
		//							type = Commandable.OrderType.Move,
		//							position = pos - (normal.GetPerpendicular(random.NextBool(0.50f)) * random.NextFloat(0.50f * index)) + (normal.GetPerpendicular(random.NextBool(0.50f)) * index * 2),
		//							unit_count = commandable_count,
		//							unit_index = index
		//						};

		//						if (ent_target_root != ref_commandable.entity.GetRoot(Relation.Type.Child))
		//						{
		//							ev.ent_target = ent_target;
		//						}

		//						ref_commandable.entity.Notify(ref ev);
		//					}
		//					break;

		//					case Commandable.OrderType.Pickup:
		//					{
		//						var ev = new Commandable.OrderEvent()
		//						{
		//							type = Commandable.OrderType.Pickup,
		//							position = pos - (normal.GetPerpendicular(random.NextBool(0.50f)) * random.NextFloat(0.50f * index)) + (normal.GetPerpendicular(random.NextBool(0.50f)) * index),
		//							unit_count = commandable_count,
		//							unit_index = index
		//						};

		//						if (ent_target_root != ref_commandable.entity.GetRoot(Relation.Type.Child))
		//						{
		//							ev.ent_target = ent_target;
		//						}

		//						ref_commandable.entity.Notify(ref ev);
		//					}
		//					break;

		//					case Commandable.OrderType.Use:
		//					{
		//						var ev = new Commandable.OrderEvent()
		//						{
		//							type = Commandable.OrderType.Use,
		//							position = pos - (normal.GetPerpendicular(random.NextBool(0.50f)) * random.NextFloat(0.50f * index)) + (normal.GetPerpendicular(random.NextBool(0.50f)) * index),
		//							unit_count = commandable_count,
		//							unit_index = index
		//						};

		//						if (ent_target_root != ref_commandable.entity.GetRoot(Relation.Type.Child))
		//						{
		//							ev.ent_target = ent_target;
		//						}

		//						ref_commandable.entity.Notify(ref ev);
		//					}
		//					break;

		//					case Commandable.OrderType.Attack:
		//					{
		//						var ev = new Commandable.OrderEvent()
		//						{
		//							type = Commandable.OrderType.Attack,
		//							position = pos - (normal.GetPerpendicular(random.NextBool(0.50f)) * random.NextFloat(0.50f * index) * 2) + (normal.GetPerpendicular(random.NextBool(0.50f)) * index * 2),
		//							unit_count = commandable_count,
		//							unit_index = index
		//						};

		//						if (ent_target_root != ref_commandable.entity.GetRoot(Relation.Type.Child))
		//						{
		//							ev.ent_target = ent_target;
		//							ev.position = pos;
		//						}

		//						ref_commandable.entity.Notify(ref ev);
		//					}
		//					break;

		//					case Commandable.OrderType.Defend:
		//					{
		//						var ev = new Commandable.OrderEvent()
		//						{
		//							type = Commandable.OrderType.Defend,
		//							position = pos - (normal.GetPerpendicular(random.NextBool(0.50f)) * random.NextFloat(0.50f * index) * 2) + (normal.GetPerpendicular(random.NextBool(0.50f)) * index * 2),
		//							unit_count = commandable_count,
		//							unit_index = index
		//						};

		//						if (ent_target_root != ref_commandable.entity.GetRoot(Relation.Type.Child))
		//						{
		//							ev.ent_target = ent_target;
		//						}

		//						ref_commandable.entity.Notify(ref ev);
		//					}
		//					break;

		//					case Commandable.OrderType.Harvest:
		//					{
		//						var ev = new Commandable.OrderEvent()
		//						{
		//							type = Commandable.OrderType.Harvest,
		//							position = pos - (normal.GetPerpendicular(random.NextBool(0.50f)) * random.NextFloat(0.50f * index) * 2) + (normal.GetPerpendicular(random.NextBool(0.50f)) * index * 2),
		//							unit_count = commandable_count,
		//							unit_index = index
		//						};

		//						if (ent_target_root != ref_commandable.entity.GetRoot(Relation.Type.Child))
		//						{
		//							ev.ent_target = ent_target;
		//						}

		//						ref_commandable.entity.Notify(ref ev);
		//					}
		//					break;

		//					case Commandable.OrderType.Capture:
		//					{
		//						var ev = new Commandable.OrderEvent()
		//						{
		//							type = Commandable.OrderType.Capture,
		//							position = pos - (normal.GetPerpendicular(random.NextBool(0.50f)) * random.NextFloat(0.50f * index) * 2) + (normal.GetPerpendicular(random.NextBool(0.50f)) * index * 2),
		//							unit_count = commandable_count,
		//							unit_index = index
		//						};

		//						if (ent_target_root != ref_commandable.entity.GetRoot(Relation.Type.Child))
		//						{
		//							ev.ent_target = ent_target;
		//							ev.position = pos;
		//						}

		//						ref_commandable.entity.Notify(ref ev);
		//					}
		//					break;

		//					case Commandable.OrderType.Store:
		//					{
		//						var ev = new Commandable.OrderEvent()
		//						{
		//							type = Commandable.OrderType.Store,
		//							position = pos - (normal.GetPerpendicular(random.NextBool(0.50f)) * random.NextFloat(0.50f * index)) + (normal.GetPerpendicular(random.NextBool(0.50f)) * index),
		//							unit_count = commandable_count,
		//							unit_index = index
		//						};

		//						if (ent_target_root != ref_commandable.entity.GetRoot(Relation.Type.Child))
		//						{
		//							ev.ent_target = ent_target;
		//						}

		//						ref_commandable.entity.Notify(ref ev);
		//					}
		//					break;
		//				}

		//				index++;
		//			}
		//		}
		//	}
		//}
#endif

#if CLIENT
		//static partial void Ext_DrawLeftGroup(ref Selection.SelectionGUI gui, in GUI.Group group, ref Region.Data region, ref Player.Data player, OwnedComponent<Squad.Data> oc_squad);
		//static partial void Ext_DrawRightGroup(ref Selection.SelectionGUI gui, in GUI.Group group, ref Region.Data region, ref Player.Data player, OwnedComponent<Squad.Data> oc_squad);

		[Shitcode]
		public partial struct SelectionGUI: IGUICommand
		{
			public Entity ent_selection;
			public Selection.Data selection;

			public static Vector2? a;
			public static Vector2? b;

			public static readonly Commandable.OrderType[] order_types = Enum.GetValues<Commandable.OrderType>();
			//public static Texture.Handle tex_icons_npc = "ui_icons_npc";

			public static float edit_transfer_money_amount;

			public static Entity edit_ent_selected;
			public static Commandable.OrderType edit_order_type;

			[Shitcode]
			public void Draw()
			{
				ref var region = ref this.ent_selection.GetRegion();
				if (region.IsNull()) return;

				ref var player = ref Client.GetPlayer();
				if (player.IsNull()) return;

				ref var player_data = ref Client.GetPlayerData();
				if (player_data.IsNull()) return;

				ref var faction_data = ref player_data.h_faction.GetData();
				if (faction_data.IsNull()) return;

				var is_enabled = faction_data.IsNotNull() && !faction_data.flags.HasAny(IFaction.Flags.No_Player_Squads);

				using (var widget = Sidebar.Widget.New(identifier: "selection.squads", name: "Squads",
				icon: new Sprite(GUI.tex_icons_widget, 16, 16, 1, 0), size: new Vector2(428 + 48, 350 + 24),
				enabled: is_enabled, lockable: false, order: -1.00f, flags: Sidebar.Widget.Flags.Align_Right))
				{
					ref readonly var kb = ref Control.GetKeyboard();
					if (kb.GetKeyDown(Keyboard.Key.G))
					{
						if (widget.state_flags.HasAny(Sidebar.Widget.StateFlags.Show))
						{
							if (widget.IsActive()) Sound.PlayGUI(GUI.sound_window_close, volume: 0.30f);
							widget.SetActive(false);
						}
						else
						{
							Sound.PlayGUI(GUI.sound_window_open, volume: 0.30f);
							widget.SetActive(true);
						}
					}

					if (widget.state_flags.HasAny(Sidebar.Widget.StateFlags.Show))
					{
						var ent_selected_squad = this.selection.ref_selected_squad.entity;
						if (ent_selected_squad.GetFactionHandle() != player_data.h_faction)
						{
							ent_selected_squad = default;
							this.selection.ref_selected_squad = default;
						}

						ref var selected_squad = ref this.selection.ref_selected_squad.GetComponent();

						//var draw = ImGui.GetBackgroundDrawList();
						ref readonly var mouse = ref GUI.GetMouse();

						var selecting_lmb = false;
						var selecting_rmb = false;

						var is_holding_item = player.ent_pickup_target.IsValid();

						if (!is_holding_item)
						{
							if (!GUI.IsHovered && mouse.GetKeyDown(Mouse.Key.Left))
							{
								a = mouse.position;
								selecting_lmb = true;
							}

							if ((!GUI.IsHovered && mouse.GetKey(Mouse.Key.Left)) || mouse.GetKeyUp(Mouse.Key.Left))
							{
								b = mouse.position;
								selecting_lmb = true;
							}
						}

						if (!selecting_lmb)
						{
							if (!is_holding_item || kb.GetKey(Keyboard.Key.LeftControl))
							{
								if (!GUI.IsHovered && mouse.GetKeyDown(Mouse.Key.Right))
								{
									a = mouse.position;
									selecting_rmb = true;
								}

								if ((!GUI.IsHovered && mouse.GetKey(Mouse.Key.Right)) || mouse.GetKeyUp(Mouse.Key.Right))
								{
									b = mouse.position;
									selecting_rmb = true;
								}
							}
						}

						//dragging &= kb.GetKey(Keyboard.Key.LeftShift);
						//dragging &= !Drag.is_dragging; // && Vector2.DistanceSquared(a, mouse.position) > 0.125f;

						if (a.TryGetValue(out var tmp_a))
						{
							//var tmp_a = a;
							var tmp_b = b ?? tmp_a;

							var cpos_a = region.WorldToCanvas(tmp_a);
							var cpos_b = region.WorldToCanvas(tmp_b);

							var color = GUI.font_color_yellow;

							if (selecting_lmb)
							{
								var rect = new AABB(tmp_a, tmp_b);

								var commandables = default(FixedArray4<Entity>);
								var commandables_count = 0;

								var results = FixedArray.CreateSpan32NoInit<OverlapBBResult>(out var buffer); // new OverlapBBResult[32].AsSpan();
								if (region.TryOverlapBBAll(rect: rect, hits: ref results,
								mask: Physics.Layer.Creature, exclude: Physics.Layer.Dead | Physics.Layer.Attached | Physics.Layer.Stored))
								{
									foreach (ref var result in results)
									{
										var ent_unit = result.entity;
										//if (result.GetFactionHandle() == player.faction_id && !ent_unit.HasRel<Squad.Relation>(Entity.Wildcard) && result.entity.TryGetRecord(out var rec_unit))
										if (result.GetFactionHandle() == player.faction_id && ent_unit.TryGetRecord(out var rec_unit) && rec_unit.HasComponent<Commandable.Data>()) // && !ent_unit.HasRel<Squad.Relation>(Entity.Wildcard))
										{
											var rect_commandable = result.GetAABB();
											var rect_commandable_size = rect_commandable.GetSize();

											var pos = rect_commandable.GetPosition();

											//GUI.DrawRect(new AABB(GUI.WorldToCanvas(pos + rect_commandable.a), GUI.WorldToCanvas(pos + rect_commandable.b)), 0xff00ff00);

											var color_unit = new Color32BGRA(0xff00ff00);

											GUI.DrawCircle(region.WorldToCanvas(pos), region.GetWorldToCanvasScale() * (Maths.Max(rect_commandable_size.X, rect_commandable_size.Y)), color_unit.WithAlphaMult(0.50f), 1.00f);

											commandables[commandables_count++] = ent_unit;
											if (commandables_count >= commandables.Length)
											{
												break;
											}
										}
									}
								}

								//draw.AddRectFilled(cpos_a, cpos_b, color.WithAlphaMult(0.10f), 0.00f);
								//draw.AddRect(cpos_a, cpos_b, color, 0.00f);

								GUI.DrawRect(new AABB(cpos_a, cpos_b), color);

								if (mouse.GetKeyUp(Mouse.Key.Left))
								{
									var rpc = new SelectRPC()
									{
										a = tmp_a,
										b = tmp_b,
										units = commandables
									};
									rpc.Send(this.ent_selection);

									a = null;
									b = null;
								}
							}

							if (selecting_rmb)
							{
								if (selected_squad.IsNotNull())
								{
									GUI.DrawLine(cpos_a, cpos_b, color: selected_squad.color, 4.00f, layer: GUI.Layer.Background);

									if (mouse.GetKeyUp(Mouse.Key.Right))
									{
										var rpc = new Squad.DEV_SetOrderRPC()
										{
											position_a = a,
											position_b = b,
											order_type = Commandable.OrderType.Move
										};

										if (kb.GetKey(Keyboard.Key.LeftShift))
										{
											rpc.flags |= Squad.Data.Flags.Force;
										}

										rpc.Send(ent_selected_squad);

										a = null;
										b = null;
									}
								}
								else
								{
									if (mouse.GetKeyUp(Mouse.Key.Right))
									{
										var rpc = new Commandable.DEV_SetOrderRPC()
										{
											order_type = Commandable.OrderType.Move,
											position_a = a,
											position_b = b
										};

										foreach (var ent_unit in this.selection.selected_units)
										{
											if (ent_unit.IsAlive())
											{
												rpc.Send(ent_unit);
											}
										}

										a = null;
										b = null;
									}
								}
							}
						}

						using (GUI.Group.New(size: GUI.Av))
						{
							//using (GUI.Group.New(size: new(GUI.RmX, 48)))
							//{
							//	foreach (var order_type in order_types)
							//	{
							//		if (order_type == Commandable.OrderType.Undefined) continue;

							//		GUI.SameLine();
							//		if (GUI.RmX < 48)
							//		{
							//			//App.WriteLine(GUI.RmX);
							//			GUI.NewLine();
							//		}

							//		using (GUI.Group.New(size: new(48, 48)))
							//		{
							//			//if (GUI.DrawButton(order_type.ToString(), new(48, 48), enabled: this.selection.order_type != order_type))
							//			if (GUI.DrawSpriteButton(order_type.ToString(), new Sprite(tex_icons_npc, 16, 16, (uint)order_type - 1, 0), new Vector2(48, 48), enabled: this.selection.order_type != order_type))
							//			{
							//				var rpc = new Selection.SetOrderTypeRPC()
							//				{
							//					order_type = order_type
							//				};
							//				rpc.Send(this.ent_selection);
							//			}

							//			if (GUI.IsItemHovered())
							//			{
							//				using (GUI.Tooltip.New()) GUI.Text(order_type.ToString());
							//			}
							//		}
							//	}
							//}

							using (var group_top = GUI.Group.New(size: new(GUI.RmX, 40)))
							{
								//if (Squad.enable_create_squad_menu)
								//{
								//	if (GUI.DrawButton("Create Squad"u8, new Vector2(128, group_top.size.Y)))
								//	{
								//		var rpc = new Selection.DEV_CreateSquadRPC()
								//		{

								//		};
								//		rpc.Send(this.ent_selection);
								//	}

								//	GUI.SameLine();
								//}

								//if (player.faction_id != 0)
								{
									using (var dropdown = GUI.Dropdown.Begin("squads"u8, (ent_selected_squad.IsAlive() ? ent_selected_squad.GetFullName() : "<no squad selected>"u8), size: GUI.Rm))
									{
										var is_dropdown_hovered = GUI.IsItemHovered();
										if (is_dropdown_hovered || dropdown.show || !is_holding_item)
										{
											Span<Entity> span_squads = stackalloc Entity[16];

											var ts = Timestamp.Now();
											region.GetEntsWithComponent<Squad.Data>(ref span_squads, h_faction: player.faction_id);
											var ts_elapsed = ts.GetMilliseconds();

											//GUI.Title($"Squads {span_squads.Length}; {ts_elapsed:0.0000} ms");

											if (span_squads.Length > 0)
											{
												if (dropdown.show)
												{
													foreach (var ent_squad in span_squads)
													{
														//ref var faction = ref ent_squad.GetComponent<Faction.Data>();
														//if (faction.IsNotNull() && faction.id == player.faction_id)
														//{
														using (GUI.ID.Push(ent_squad))
														using (var group_row = GUI.Group.New(new Vector2(GUI.RmX, 40)))
														{
															using (var group_icon = GUI.Group.New(size: new(GUI.RmY)))
															{
																//group_icon.DrawBackground(GUI.tex_slot_simple);
																GUI.DrawEntityIcon(ent_squad, scale: 2,
																	flags: (GUI.DrawEntityIconFlags.Draw_Frame, GUI.DrawEntityIconFlags.None));
															}

															GUI.SameLine();

															using (var group_info = GUI.Group.New(size: GUI.Rm, padding: new(6)))
															{
																group_info.DrawBackground(GUI.tex_window_popup_embed, color: GUI.col_frame);

																GUI.TitleCentered(ent_squad.GetFullName(), pivot: new(0.00f, 0.00f), offset: new(2, 0));

																var selected = ent_selected_squad == ent_squad;
																if (GUI.Selectable3("select"u8, group_row.GetOuterRect(), selected: selected))
																{
																	var rpc = new Selection.SelectSquadRPC()
																	{
																		ent_squad = ent_squad
																	};
																	rpc.Send(this.ent_selection);

																	dropdown.Close();
																}
															}
														}

														//}
													}
												}
												else
												{
													//ref readonly var mouse = ref GUI.GetMouse();
													if (is_dropdown_hovered)
													{
														var scroll = (int)mouse.GetScroll();
														if (scroll != 0)
														{
															var index = span_squads.IndexOf(ent_selected_squad);
															if (index == -1) index = 0;

															index = Maths.Wrap(index + scroll, 0, span_squads.Length);
															//App.WriteLine(index);
															Sound.PlayGUI(GUI.sound_select, volume: 0.07f);

															var rpc = new Selection.SelectSquadRPC()
															{
																ent_squad = span_squads[index]
															};
															rpc.Send(this.ent_selection);
														}
													}
													else if (!is_holding_item && (!GUI.IsHovered))
													{
														var scroll = 0;
														if (mouse.GetKeyDown(Mouse.Key.Forward)) scroll = 1;
														else if (mouse.GetKeyDown(Mouse.Key.Back)) scroll = -1;

														if (scroll != 0)
														{
															var index = span_squads.IndexOf(ent_selected_squad);
															if (index == -1) index = 0;

															index = Maths.Wrap(index + scroll, 0, span_squads.Length);
															//App.WriteLine(index);
															Sound.PlayGUI(GUI.sound_select, volume: 0.07f);

															var rpc = new Selection.SelectSquadRPC()
															{
																ent_squad = span_squads[index]
															};
															rpc.Send(this.ent_selection);
														}
													}

													//App.WriteLine(scroll);


												}
											}
										}
									}
								}
							}

							GUI.SeparatorThick();

							Span<Entity> span_units = stackalloc Entity[16];
							Span<Entity> span_units_squad = stackalloc Entity[16];

							var units_count = 0;

							if (ent_selected_squad.IsAlive())
							{
								//var ts = Timestamp.Now();
								region.GetEntsWithRelation<Squad.Relation>(ref span_units_squad, ent_selected_squad, "dead", false);
								//var ts_elapsed = ts.GetMilliseconds();

								//GUI.Title($"Sel {ent_selected_squad}; {span_units_squad.Length}; {ts_elapsed:0.0000} ms");

								foreach (var ent_unit in span_units_squad)
								{
									if (span_units.TryAdd(ent_unit)) units_count++;
								}
							}

							foreach (var ent_unit in this.selection.selected_units)
							{
								if (ent_unit.IsAlive() && !span_units_squad.Contains(ent_unit))
								{
									span_units.TryAdd(ent_unit);
								}
							}

							span_units.Sort();

							using (var group_top = GUI.Group.New2(size: new(GUI.RmX, 32 + 32 + 24)))
							{
								if (selected_squad.IsNotNull())
								{
									var selected_squad_copy = selected_squad;

									var edit_rpc = new Squad.DEV_EditRPC();
									var rpc_dirty = false;

									//var w_dropdown = GUI.RmX - 100;

									using (var group_left = GUI.Group.New2(size: new(248, 32 + 32 + 24), padding: new(4, 4, 2, 2)))
									{
										group_left.DrawBackground(GUI.tex_panel);


										using (var group_orders = GUI.Group.New2(size: new(GUI.RmX, 48)))
										{
											DrawOrderType(Commandable.OrderType.Work, frame: new(8, 6));
											GUI.SameLine();
											//DrawOrderType(Commandable.OrderType.Pickup, frame: new(1, 6));
											//GUI.SameLine();
											DrawOrderType(Commandable.OrderType.Attack, frame: new(3, 6));
											GUI.SameLine();
											DrawOrderType(Commandable.OrderType.Drive, frame: new(2, 6));

											static void DrawOrderType(Commandable.OrderType order_type, Vec2u16 frame)
											{
												using (GUI.ID<Commandable.OrderType>.Push((uint)order_type))
												{
													if (GUI.DrawIconButton("order_type"u8, GUI.spr_icons_widget.WithFrame(frame.x, frame.y), new(GUI.RmY), enabled: edit_order_type != order_type, color_icon: GUI.font_color_button_text))
													{
														edit_order_type = order_type;
													}
													GUI.DrawHoverTooltip(order_type.ToStringUtf8());
												}
											}
										}

										GUI.SeparatorThick();

										using (var group_money = GUI.Group.New2(size: GUI.Rm))
										{
											group_money.DrawBackground(GUI.tex_panel);

											//using (var group_tmp = GUI.Group.New2(size: new(64, GUI.RmY)))
											//{
											//	GUI.TitleCentered("Funds:"u8, pivot: new(0.00f, 0.50f), size: 20);
											//}
											//GUI.SameLine(4);
											//GUI.OffsetLine(64);
											GUI.DrawMoney(selected_squad.money, size: GUI.Rm);
										}

										//var ent_target_tmp = Entity.Empty;
										//var ent_unit_tmp = span_units_squad.GetFirstOrDefault();

										//if (GUI.EntityPicker("picker.work"u8, "Work"u8, size: new(96, 40), region_id: region.GetID(),
										//entity: ref ent_target_tmp, enabled: ent_unit_tmp.IsAlive()))
										//{

										//	var rpc = new Commandable.DEV_SetOrderRPC()
										//	{
										//		order_type = Commandable.OrderType.Work,
										//		ent_target = ent_target_tmp,
										//		position_a = region.CanvasToWorld(GUI.GetMousePos())
										//	};

										//	App.WriteValue((ent_target_tmp, ent_unit_tmp));
										//	rpc.Send(ent_unit_tmp);

										//	//edit_rpc.ent_target = selected_squad_copy.ent_target;
										//	//rpc_dirty = true;
										//}


										//if (GUI.EntityPicker("picker.target"u8, "Target"u8, size: new(96, 40), region_id: region.GetID(), 
										//entity: ref selected_squad_copy.ent_target))
										//{
										//	edit_rpc.ent_target = selected_squad_copy.ent_target;
										//	rpc_dirty = true;
										//}

										//GUI.SameLine();

										//if (GUI.EntityPicker("picker.follow"u8, "Follow"u8, size: new(96, 40), region_id: region.GetID(), 
										//entity: ref selected_squad_copy.ent_follow))
										//{
										//	edit_rpc.ent_follow = selected_squad_copy.ent_follow;
										//	rpc_dirty = true;
										//}



										//if (GUI.EntityPicker("picker.storage"u8, "Storage"u8, size: new(96, 40), region_id: region.GetID(), 
										//entity: ref selected_squad_copy.ent_storage, layer_require: Physics.Layer.Storage, layer_exclude: Physics.Layer.Ignore_Hover | Physics.Layer.World | Physics.Layer.Stored))
										//{
										//	edit_rpc.ent_storage = selected_squad_copy.ent_storage;
										//	rpc_dirty = true;
										//}

										//GUI.SameLine();

										//if (GUI.EntityPicker("picker.home"u8, "Home"u8, size: new(96, 40), region_id: region.GetID(), 
										//entity: ref selected_squad_copy.ent_home))
										//{
										//	edit_rpc.ent_home = selected_squad_copy.ent_home;
										//	rpc_dirty = true;
										//}
									}

									GUI.SameLine();

									using (var group_right = GUI.Group.New(size: GUI.Rm))
									{
										if (GUI.EnumInput("squad.movement_flags"u8, ref selected_squad_copy.movement_flags, size: new(GUI.RmX, 32),
										show_label: false, show_none: true, columns: 2))
										{
											edit_rpc.movement_flags = selected_squad_copy.movement_flags;
											rpc_dirty = true;
										}
										GUI.DrawHoverTooltip("Movement"u8);

										if (GUI.EnumInput("squad.combat_flags"u8, ref selected_squad_copy.combat_flags, size: new(GUI.RmX, 32),
										show_label: false, show_none: true, columns: 2))
										{
											edit_rpc.combat_flags = selected_squad_copy.combat_flags;
											rpc_dirty = true;
										}
										GUI.DrawHoverTooltip("Combat"u8);

										if (GUI.EnumInput("squad.item_hints"u8, ref selected_squad_copy.item_hints_prefer, size: new(GUI.RmX, 32),
										show_label: false, show_none: true, columns: 2))
										{
											edit_rpc.item_hints = selected_squad_copy.item_hints_prefer;
											rpc_dirty = true;
										}
										GUI.DrawHoverTooltip("Items"u8);

										if (GUI.DrawColorPicker("squad.color"u8, ref selected_squad_copy.color))
										{
											edit_rpc.color = selected_squad_copy.color;
											rpc_dirty = true;
										}
										GUI.DrawHoverTooltip("Squad Color"u8);

										//Ext_DrawRightGroup(ref this, in group_right, ref region, ref player, this.selection.ref_selected_squad);
									}

									//if (GUI.EnumInput("squad.flags", ref selected_squad_copy.flags, size: new(w_dropdown, 32), show_label: false, show_none: true))
									//{
									//	edit_rpc.flags = selected_squad_copy.flags;
									//	rpc_dirty = true;
									//}





									//GUI.SeparatorThick();


									//if (!GUI.IsHovered && !player.ent_pickup_target.IsValid() && GUI.IsMouseDoubleClicked())
									//{
									//	var rpc = new Selection.SelectSquadRPC()
									//	{
									//		ent_squad = default
									//	};
									//	rpc.Send(this.ent_selection);
									//}

									if (selected_squad.position_a.HasValue)
									{
										var c_pos_a = region.WorldToCanvas(selected_squad.position_a.GetValueOrDefault());
										foreach (var pos in selected_squad_copy.positions.AsSpan(selected_squad_copy.unit_count))
										{
											//GUI.DrawLine(c_pos_a, region.WorldToCanvas(pos), color: selected_squad.color.WithAlpha(150), thickness: 2.00f);
											GUI.DrawPoint(region.WorldToCanvas(pos), color: selected_squad.color.WithAlpha(150), radius: 4.00f);
										}
									}

									if (rpc_dirty)
									{
										edit_rpc.Send(ent_selected_squad);
									}
								}
							}

							using (var group_bottom = GUI.Group.New(GUI.Rm, padding: new(4)))
							{
								//GUI.SeparatorThick();

								group_bottom.DrawBackground(GUI.tex_window_menu);

								using (var scrollbox = GUI.Scrollbox.New("scrollbox.selection"u8, size: GUI.Rm, padding: new(0)))
								{
									//scrollbox.group_frame.DrawBackground(GUI.tex_window);

									//foreach (ref var ref_commandable in this.selection.units.AsSpan())
									foreach (var ent_unit in span_units)
									{
										if (ent_unit.IsAlive())
										{
											ref var commandable = ref ent_unit.GetComponent<Commandable.Data>(); //.GetValueOrNullRef();
											if (commandable.IsNotNull() && commandable.flags.HasNone(Commandable.Data.Flags.No_Select))
											{
												ref var body = ref ent_unit.GetComponent<Body.Data>();
												ref var transform = ref ent_unit.GetComponent<Transform.Data>();
												if (body.IsNotNull() && transform.IsNotNull())
												{
													//var rect_commandable = body.GetAABB();
													//var rect_commandable_size = rect_commandable.GetSize();

													var pos = transform.GetInterpolatedPosition(); // body.GetPosition();

													//GUI.DrawRect(new AABB(GUI.WorldToCanvas(pos + rect_commandable.a), GUI.WorldToCanvas(pos + rect_commandable.b)), 0xff00ff00);

													var color_unit = new Color32BGRA(0xff00ff00);

													var ent_unit_squad = ent_unit.GetParent<Squad.Relation>();
													if (ent_unit_squad.IsValid())
													{
														ref var unit_squad = ref ent_unit_squad.GetComponent<Squad.Data>();
														if (unit_squad.IsNotNull())
														{
															color_unit = unit_squad.color;
														}
													}

													//var last_node_pos = default(Vector2?);
													//var last_node_color = default(Color32BGRA?);

													////if (false)
													//{
													//	ref var pathfinding_state = ref ent_unit.GetComponent<Pathfinding.State>();
													//	if (pathfinding_state.IsNotNull())
													//	{
													//		if (true) //pathfinding_state.last_result.HasNone(Pathfinding.Results.Done))
													//		{
													//			//if (region.GetRandom().NextBool(0.10f)) //region.GetCurrentTick() % 16 == 0)
													//			//{
													//			//	IComponent.Handle.FromComponent<Pathfinding.State>().RequestSync(ent_unit);
													//			//}

													//			if (pathfinding_state.points_count > 0)
													//			{
													//				var point_prev = pos;

													//				var points = pathfinding_state.points;
													//				for (var i = 1; i < pathfinding_state.points_count - 2; i += 1)
													//				{
													//					if (i < pathfinding_state.current_index - 1)
													//					{
													//						points[i + 1] = pos;
													//						continue;
													//					}

													//					var node_color = new Color32BGRA(0xffff0000);
													//					if (i <= pathfinding_state.visible_index) node_color = 0xff00ff00;
													//					if (i > pathfinding_state.current_index) node_color = node_color.WithAlphaMult(0.25f);


													//					//var a = points[i - 1];
													//					//var d = points[i];
													//					//var b = a;
													//					//var c = d;

													//					//if (i == pathfinding_state.current_index)
													//					//{
													//					//	var a = pos;
													//					//	var b = points[i];
													//					//	var c = points[i + 1];

													//					//	GUI.DrawCurveCubic(GUI.WorldToCanvas(a), GUI.WorldToCanvas(a), GUI.WorldToCanvas(b), GUI.WorldToCanvas(c), node_color, segments: 16);
													//					//}
													//					//else
													//					{
													//						var a = points[i];
													//						var b = points[i + 1];

													//						GUI.DrawLine(region.WorldToCanvas(a), region.WorldToCanvas(b), node_color, thickness: 2.00f);
													//					}

													//					//var a = points[i];
													//					//var c = points[i + 1];

													//					//var dir = (c - a).GetNormalized(out var dist);
													//					//var b = a + (dir * dist * 0.50f)- (new Vector2(-dir.Y, dir.X) * 0.50f * dist);

													//					//GUI.DrawLine(GUI.WorldToCanvas(a), GUI.WorldToCanvas(c), node_color);
													//					////GUI.DrawCurve(GUI.WorldToCanvas(points[i - 1]), GUI.WorldToCanvas(new Vector2(Maths.Lerp(points[i].X, points[i - 1].X, 0.30f), Maths.Lerp(points[i - 1].Y, points[i].Y, 0.30f))), GUI.WorldToCanvas(points[i]), node_color);
													//					//GUI.DrawCurveCubic(GUI.WorldToCanvas(a), GUI.WorldToCanvas(b), GUI.WorldToCanvas(c), GUI.WorldToCanvas(d), node_color, segments: 16);
													//					//GUI.DrawCurveQuadratic(GUI.WorldToCanvas(a), GUI.WorldToCanvas(b), GUI.WorldToCanvas(c), node_color, segments: 16);
													//					//GUI.DrawCurveQuadratic(GUI.WorldToCanvas(a), GUI.WorldToCanvas(b), GUI.WorldToCanvas(c), node_color, segments: 16);
													//					//GUI.DrawCurve(GUI.WorldToCanvas(pos), GUI.WorldToCanvas(points[i - 1]), GUI.WorldToCanvas(points[i]), node_color);

													//					//point_prev = c;

													//					last_node_pos = points[i + 1];
													//					last_node_color = node_color;
													//				}
													//			}

													//			if (pathfinding_state.points_count > 0 && pathfinding_state.points_high_count > 0)
													//			{
													//				var point_prev = pathfinding_state.points[pathfinding_state.points_count - 2];

													//				var points = pathfinding_state.points_high;
													//				for (var i = pathfinding_state.current_high_index; i < pathfinding_state.points_high_count - 1; i += 1)
													//				{
													//					if (i == pathfinding_state.current_high_index)
													//					{
													//						points[i] = point_prev;
													//						//continue;
													//					}

													//					var node_color = new Color32BGRA(0xffff0000).WithAlphaMult(0.25f);


													//					var a = points[i];
													//					var b = points[i + 1];

													//					GUI.DrawLine(region.WorldToCanvas(a), region.WorldToCanvas(b), node_color, thickness: 2.00f);

													//					last_node_pos = points[i];
													//					last_node_color = node_color;
													//				}
													//			}
													//		}

													//		if (false)
													//		{
													//			if (last_node_pos.HasValue && last_node_color.HasValue)
													//			{
													//				GUI.DrawLine(region.WorldToCanvas(last_node_pos.Value), region.WorldToCanvas(pathfinding_state.points_high[pathfinding_state.points_high_count - 1]), last_node_color.Value, thickness: 2.00f);
													//			}

													//			if (pathfinding_state.points_high_count > 0)
													//			{
													//				GUI.DrawCircleFilled(region.WorldToCanvas(pathfinding_state.points_high[pathfinding_state.points_high_count - 1]), region.GetWorldToCanvasScale() * 0.25f, last_node_color ?? GUI.font_color_green, segments: 4);
													//			}
													//		}

													//		GUI.DrawCircleFilled(region.WorldToCanvas(pos - new Vector2(0.00f, rect_commandable_size.Y * 1.350f)), region.GetWorldToCanvasScale() * 0.25f, color_unit.WithAlphaMult(1.00f), segments: 4);
													//		//GUI.DrawCircleFilled(GUI.WorldToCanvas(pos), GUI.GetWorldToCanvasScale() * (Maths.Max(rect_commandable_size.X, rect_commandable_size.Y) + 0.100f), color_unit.WithAlphaMult(0.75f), segments: 4);
													//	}

													//}

													ref var npc = ref ent_unit.GetComponent<NPC.Data>();
													if (npc.IsNotNull())
													{
														ref var control = ref ent_unit.GetComponent<Control.Data>();
														if (control.IsNotNull())
														{
															GUI.DrawCircle(a: region.WorldToCanvas(pos), radius: region.GetWorldToCanvasScale(), color: color_unit.WithAlpha(200), segments: 24);
															if (npc.h_character_cached != Client.GetCharacterHandle())
															{
																GUI.DrawLine(a: region.WorldToCanvas(pos), b: region.WorldToCanvas(control.mouse.position), color: Color32BGRA.White.WithAlpha(100));
															}
														}

														using (GUI.ID.Push(ent_unit))
														using (var group = GUI.Group.New(new(GUI.RmX, 48)))
														{
															if (group.IsVisible())
															{
																if (group.IsHovered())
																{
																	GUI.DrawEntityMarker(ent_unit);
																}

																var h_character = default(ICharacter.Handle);
																var h_origin = default(IOrigin.Handle);

																ref var character = ref ent_unit.GetComponentWithOwner<Character.Data>(Relation.Type.Instance, out var ent_character, true);
																if (character.IsNotNull())
																{
																	h_character = ent_character.GetAssetHandle<ICharacter.Handle>(); // character.character_id;
																}

																ref var character_data = ref h_character.GetData();
																if (character_data.IsNotNull())
																{
																	h_origin = character_data.origin;
																}

																ref var unit_player = ref ent_unit.GetComponent<Player.Data>();
																//ref var transform = ref ent_unit.GetComponent<Transform.Data>();

																ref var origin_data = ref h_origin.GetData();

																GUI.DrawBackground(GUI.tex_panel, group.GetOuterRect(), new(4));

																//using (var group_head = GUI.Group.New(size: new(48, 48)))
																//{
																//	Dormitory.DrawCharacterHead(h_character, group_head.size);
																//	//GUI.DrawBackground(GUI.tex_frame, group_frame.GetOuterRect(), new(4));
																//}

																var is_in_squad = span_units_squad.Contains(ent_unit);
																var is_selected = edit_ent_selected == ent_unit || this.selection.selected_units.Contains(ent_unit);

																if (selected_squad.IsNotNull())
																{
																	if (is_in_squad)
																	{
																		if (GUI.DrawButton("Rem"u8, new Vector2(48, group.size.Y), color: GUI.col_button_error,
																		error: selected_squad.flags.HasAny(Squad.Data.Flags.No_Unit_Remove)))
																		{
																			var rpc = new Selection.DEV_AddToSquadRPC()
																			{
																				ent_unit = ent_unit,
																				ent_squad = default,
																			};
																			rpc.Send(this.ent_selection);
																		}
																	}
																	else
																	{
																		if (GUI.DrawButton("Add"u8, new Vector2(48, group.size.Y), color: GUI.col_button_ok,
																		error: !(selected_squad.unit_count < selected_squad.max_size)))
																		{
																			var rpc = new Selection.DEV_AddToSquadRPC()
																			{
																				ent_unit = ent_unit,
																				ent_squad = ent_selected_squad,
																			};
																			rpc.Send(this.ent_selection);
																		}
																	}

																	//if (GUI.DrawButton("Add", new Vector2(48, 48), color: GUI.col_button_ok, enabled: !span_units_squad.Contains(ent_unit)))
																	//{
																	//	var rpc = new Selection.DEV_AddToSquadRPC()
																	//	{
																	//		ent_unit = ent_unit,
																	//		ent_squad = ent_selected_squad,
																	//	};
																	//	rpc.Send(ent_selection);
																	//}


																	GUI.SameLine();
																}

																Dormitory.DrawCharacterHead(h_character, new Vector2(group.size.Y));

																GUI.SameLine();

																//var inv_frame_size = Inventory.GetFrameSize(2, 4);

																ref var behavior = ref ent_unit.GetComponent<AI.Behavior>();

																using (var group_left = GUI.Group.New(size: new Vector2(GUI.RmX - 56 - 80 - 32, group.size.Y), padding: new(4, 4)))
																{
																	using (GUI.Wrap.Push())
																	{
																		var action_text = string.Empty;

																		var current_pos = Vector2.Zero;

																		if (transform.IsNotNull())
																		{
																			current_pos = transform.position;
																		}

																		//ref var behavior = ref ent_unit.GetComponent<AI.Behavior>();

																		if (unit_player.IsNotNull())
																		{
																			action_text = $"Controlled by {unit_player.h_player.GetName()}";
																		}
																		else
																		{
																			//ref var behavior = ref ent_unit.GetComponent<AI.Behavior>();
																			if (behavior.IsNotNull())
																			{
																				if (behavior.type != AI.Behavior.Type.Idle)
																				{
																					ref var pathfinding_state = ref ent_unit.GetComponent<Pathfinding.State>();
																					if (pathfinding_state.IsNotNull())
																					{
																						var last_node_pos = default(Vector2?);
																						var last_node_color = default(Color32BGRA?);

																						if (true) //pathfinding_state.last_result.HasNone(Pathfinding.Results.Done))
																						{
																							//if (region.GetRandom().NextBool(0.10f)) //region.GetCurrentTick() % 16 == 0)
																							//{
																							//	IComponent.Handle.FromComponent<Pathfinding.State>().RequestSync(ent_unit);
																							//}

																							//if (behavior.type == AI.Behavior.Type.Move) //self_hints.HasNone(NPC.SelfHints.Is_Idle))
																							{
																								if (region.GetRandom().NextBool(0.33f)) //region.GetCurrentTick() % 16 == 0)
																								{
																									IComponent.Handle.FromComponent<Pathfinding.State>().RequestSync(ent_unit);
																								}
																							}



																							if (pathfinding_state.points_count > 0)
																							{
																								var point_prev = pos;

																								var points = pathfinding_state.points;
																								for (var i = 1; i < pathfinding_state.points_count - 2; i += 1)
																								{
																									if (i < pathfinding_state.current_index - 1)
																									{
																										points[i + 1] = pos;
																										continue;
																									}

																									var node_color = new Color32BGRA(0xffff0000);
																									if (i <= pathfinding_state.visible_index) node_color = 0xff00ff00;
																									if (i > pathfinding_state.current_index) node_color = node_color.WithAlphaMult(0.25f);


																									//var a = points[i - 1];
																									//var d = points[i];
																									//var b = a;
																									//var c = d;

																									//if (i == pathfinding_state.current_index)
																									//{
																									//	var a = pos;
																									//	var b = points[i];
																									//	var c = points[i + 1];

																									//	GUI.DrawCurveCubic(GUI.WorldToCanvas(a), GUI.WorldToCanvas(a), GUI.WorldToCanvas(b), GUI.WorldToCanvas(c), node_color, segments: 16);
																									//}
																									//else
																									{
																										var a = points[i];
																										var b = points[i + 1];

																										GUI.DrawLine(region.WorldToCanvas(a), region.WorldToCanvas(b), node_color.WithAlpha(80), thickness: 2.00f);
																									}

																									//var a = points[i];
																									//var c = points[i + 1];

																									//var dir = (c - a).GetNormalized(out var dist);
																									//var b = a + (dir * dist * 0.50f)- (new Vector2(-dir.Y, dir.X) * 0.50f * dist);

																									//GUI.DrawLine(GUI.WorldToCanvas(a), GUI.WorldToCanvas(c), node_color);
																									////GUI.DrawCurve(GUI.WorldToCanvas(points[i - 1]), GUI.WorldToCanvas(new Vector2(Maths.Lerp(points[i].X, points[i - 1].X, 0.30f), Maths.Lerp(points[i - 1].Y, points[i].Y, 0.30f))), GUI.WorldToCanvas(points[i]), node_color);
																									//GUI.DrawCurveCubic(GUI.WorldToCanvas(a), GUI.WorldToCanvas(b), GUI.WorldToCanvas(c), GUI.WorldToCanvas(d), node_color, segments: 16);
																									//GUI.DrawCurveQuadratic(GUI.WorldToCanvas(a), GUI.WorldToCanvas(b), GUI.WorldToCanvas(c), node_color, segments: 16);
																									//GUI.DrawCurveQuadratic(GUI.WorldToCanvas(a), GUI.WorldToCanvas(b), GUI.WorldToCanvas(c), node_color, segments: 16);
																									//GUI.DrawCurve(GUI.WorldToCanvas(pos), GUI.WorldToCanvas(points[i - 1]), GUI.WorldToCanvas(points[i]), node_color);

																									//point_prev = c;

																									last_node_pos = points[i + 1];
																									last_node_color = node_color;
																								}
																							}

																							if (pathfinding_state.points_count > 0 && pathfinding_state.points_high_count > 0)
																							{
																								var point_prev = pathfinding_state.points[pathfinding_state.points_count - 2];

																								var points = pathfinding_state.points_high;
																								for (var i = pathfinding_state.current_high_index; i < pathfinding_state.points_high_count - 1; i += 1)
																								{
																									if (i == pathfinding_state.current_high_index)
																									{
																										points[i] = point_prev;
																										//continue;
																									}

																									var node_color = new Color32BGRA(0xffff0000).WithAlphaMult(0.25f);


																									var a = points[i];
																									var b = points[i + 1];

																									GUI.DrawLine(region.WorldToCanvas(a), region.WorldToCanvas(b), node_color, thickness: 2.00f);

																									last_node_pos = points[i];
																									last_node_color = node_color;
																								}
																							}
																						}

																						if (true)
																						{
																							if (last_node_pos.HasValue && last_node_color.HasValue)
																							{
																								GUI.DrawLine(region.WorldToCanvas(last_node_pos.Value), region.WorldToCanvas(pathfinding_state.points_high[pathfinding_state.points_high_count - 1]), last_node_color.Value, thickness: 2.00f);
																							}

																							if (pathfinding_state.points_high_count > 0)
																							{
																								GUI.DrawCircleFilled(region.WorldToCanvas(pathfinding_state.points_high[pathfinding_state.points_high_count - 1]), region.GetWorldToCanvasScale() * 0.25f, last_node_color ?? GUI.font_color_green, segments: 4);
																							}
																						}

																						//GUI.DrawCircleFilled(region.WorldToCanvas(pos - new Vector2(0.00f, rect_commandable_size.Y * 1.350f)), region.GetWorldToCanvasScale() * 0.25f, color_unit.WithAlphaMult(1.00f), segments: 4);
																						//GUI.DrawCircleFilled(GUI.WorldToCanvas(pos), GUI.GetWorldToCanvasScale() * (Maths.Max(rect_commandable_size.X, rect_commandable_size.Y) + 0.100f), color_unit.WithAlphaMult(0.75f), segments: 4);
																					}
																				}

																				if (group.IsHovered())
																				{
																					if (behavior.position_home)
																					{
																						var c_pos_tmp = region.WorldToCanvas(behavior.position_home);
																						GUI.DrawCircleFilled(c_pos_tmp, radius: 16.00f, color: Color32BGRA.Magenta.WithAlpha(100), layer: GUI.Layer.Foreground, segments: 4);
																						GUI.DrawTextCentered("Home"u8, c_pos_tmp, color: Color32BGRA.Magenta, layer: GUI.Layer.Foreground, box_shadow: true);
																					}

																					if (behavior.position_safety)
																					{
																						var c_pos_tmp = region.WorldToCanvas(behavior.position_safety);
																						GUI.DrawCircleFilled(c_pos_tmp, radius: 24.00f, color: Color32BGRA.Yellow.WithAlpha(100), layer: GUI.Layer.Foreground, segments: 4);
																						GUI.DrawTextCentered("Safety"u8, c_pos_tmp, color: Color32BGRA.Yellow, layer: GUI.Layer.Foreground, box_shadow: true);
																					}

																					if (behavior.position_enemies)
																					{
																						var c_pos_tmp = region.WorldToCanvas(behavior.position_enemies);
																						GUI.DrawCircleFilled(c_pos_tmp, radius: 16.00f, color: Color32BGRA.Red.WithAlpha(100), layer: GUI.Layer.Foreground, segments: 4);
																						GUI.DrawTextCentered("Enemies"u8, c_pos_tmp, color: Color32BGRA.Red, layer: GUI.Layer.Foreground, box_shadow: true);
																					}

																					if (behavior.position_friends)
																					{
																						var c_pos_tmp = region.WorldToCanvas(behavior.position_friends);
																						GUI.DrawCircleFilled(c_pos_tmp, radius: 16.00f, color: Color32BGRA.Green.WithAlpha(100), layer: GUI.Layer.Foreground, segments: 4);
																						GUI.DrawTextCentered("Friends"u8, c_pos_tmp, color: Color32BGRA.Green, layer: GUI.Layer.Foreground, box_shadow: true);
																					}

																					if (behavior.position_goal)
																					{
																						var c_pos_tmp = region.WorldToCanvas(behavior.position_goal);
																						GUI.DrawCircle(c_pos_tmp, radius: 40.00f, color: Color32BGRA.Cyan.WithAlpha(150), layer: GUI.Layer.Foreground, segments: 4, thickness: 2.00f);
																						GUI.DrawTextCentered("Goal"u8, c_pos_tmp, color: Color32BGRA.Cyan, layer: GUI.Layer.Foreground, box_shadow: true);
																					}
																				}

																				switch (behavior.type)
																				{
																					case AI.Behavior.Type.Idle:
																					{
																						action_text = "Idle";
																					}
																					break;

																					case AI.Behavior.Type.Attack:
																					{
																						//action_text = $"Attacking \"{behavior.ref_target_transform.entity.GetFullName()}\"";
																						action_text = $"Attacking {behavior.ref_target_transform.entity.GetFullName()}";
																					}
																					break;

																					case AI.Behavior.Type.Harvest:
																					{
																						action_text = $"Harvesting \"{behavior.target_goal.entity.GetName()}\"";
																					}
																					break;

																					case AI.Behavior.Type.Capture:
																					{
																						action_text = $"Capturing \"{behavior.target_goal.entity.GetName()}\"";
																					}
																					break;

																					case AI.Behavior.Type.Use:
																					{
																						action_text = $"Operating \"{behavior.target_goal.entity.GetName()}\"";
																					}
																					break;

																					case AI.Behavior.Type.Store:
																					{
																						action_text = $"Storing into \"{behavior.target_goal.entity.GetName()}\"";
																					}
																					break;

																					case AI.Behavior.Type.Pickup:
																					{
																						action_text = $"Picking up \"{behavior.target_goal.entity.GetName()}\"";
																					}
																					break;

																					case AI.Behavior.Type.Drive:
																					{
																						action_text = $"Driving \"{behavior.target_goal.entity.GetName()}\"";
																					}
																					break;

																					case AI.Behavior.Type.Move:
																					{
																						if (behavior.position_target.HasValue)
																						{
																							action_text = $"Moving ({Vector2.Distance(behavior.position_target.Value, current_pos):0} meters)";
																						}
																						else
																						{
																							action_text = "Moving somewhere";
																						}
																					}
																					break;

																					case AI.Behavior.Type.Work:
																					{
																						action_text = "Working";
																					}
																					break;

																					//case AI.Behavior.Type.Flee:
																					//{
																					//	if (behavior.position_target.HasValue)
																					//	{
																					//		action_text = $"Fleeing ({Vector2.Distance(behavior.position_target.Value, current_pos):0} meters)";
																					//	}
																					//	else
																					//	{
																					//		action_text = "Fleeing somewhere";
																					//	}
																					//}
																					//break;

																					default:
																					{
																						action_text = behavior.type.ToString(); //.ToStringUtf8();
																					}
																					break;
																				}
																			}
																		}

																		//npc.self_hints;

																		var col_name = GUI.font_color_title;
																		col_name = Color32BGRA.FromHSV(Maths.SnapCeil(Maths.Lerp(0.00f, 2.00f, Maths.InvLerp01(npc.min_consciousness * 0.250f, 1.00f, npc.consciousness * npc.consciousness)), 0.50f), 1.00f, 1.00f);

																		var unit_name = character_data.IsNotNull() ? character_data.name : ent_unit.GetName();

																		if (npc.self_hints.HasAny(NPC.SelfHints.Is_Doomed) || npc.pain >= 3000.00f || npc.consciousness <= 0.35f)
																		{
																			col_name = Color32BGRA.Lerp(col_name, GUI.font_color_red, 0.75f);
																			//unit_name += " !!!";
																			//GUI.TitleCentered("!!! Doomed !!!", size: 16, pivot: new(1.00f, 1.00f), color: GUI.font_color_red);
																			GUI.TitleCentered("!!!"u8, size: 24, pivot: new(1.00f, 1.00f), color: GUI.font_color_red);
																		}
																		else if (npc.pain >= 1500.00f || npc.consciousness <= 0.50f)
																		{
																			col_name = Color32BGRA.Lerp(col_name, GUI.font_color_orange, 0.75f);
																			//unit_name += " !!";
																			//GUI.TitleCentered("!! Injured !!", size: 16, pivot: new(1.00f, 1.00f), color: GUI.font_color_orange);
																			GUI.TitleCentered("!!"u8, size: 24, pivot: new(1.00f, 1.00f), color: GUI.font_color_orange);
																		}
																		else if (npc.self_hints.HasAny(NPC.SelfHints.Is_Fleeing) || npc.pain >= 450.00f)
																		{
																			col_name = Color32BGRA.Lerp(col_name, GUI.font_color_yellow, 0.75f);
																			//unit_name += " !";
																			//GUI.TitleCentered("! Danger !", size: 16, pivot: new(1.00f, 1.00f), color: GUI.font_color_yellow);
																			GUI.TitleCentered("!"u8, size: 24, pivot: new(1.00f, 1.00f), color: GUI.font_color_yellow);
																		}

																		GUI.TitleCentered(unit_name, size: 18, pivot: new(0.00f, 0.00f), color: col_name);
																		//if (origin_data.IsNotNull())
																		//{
																		//	GUI.TextShadedCentered(origin_data.name, pivot: new(1.00f, 0.00f), color: GUI.font_color_desc);
																		//}
																		//GUI.NewLine(16);

																		//GUI.DrawEntity(behavior.ref_target_transform.entity, Color32BGRA.Red.WithAlphaMult(0.50f));

																		if (!action_text.IsNullOrEmpty())
																		{
																			//GUI.TextShaded(action_text);
																			GUI.TextShadedCentered(action_text, pivot: new(0.00f, 0.00f), offset: new(0.00f, 18.00f));
																		}
																	}
																}

																GUI.SameLine();

																using (var group_right = GUI.Group.New(size: GUI.GetRemainingSpace(x: -80 - 32), padding: new(6)))
																{
																	group_right.DrawBackground(GUI.tex_slot_simple);

																	var ent_held_item = npc.ent_arm.GetChild(Relation.Type.Child);
																	if (ent_held_item.IsAlive())
																	{
																		using (GUI.Clip.Push(group_right.GetInnerRect()))
																		{
																			GUI.DrawEntityIcon(ent_held_item);
																		}
																	}

																	//ref var storage = ref ent_unit.GetComponent<Storage.Data>();
																	//if (storage.IsNotNull())
																	//{
																	//	if (storage.inv_storage.TryGetHandle(out var h_inventory))
																	//	{
																	//		GUI.DrawInventory(h_inventory, is_readonly: true);

																	//		//Span<Resource.Data> resources = stackalloc Resource.Data[h_inventory.Length];
																	//		//h_inventory.CountResources(ref resources);

																	//		//foreach (ref var resource in resources)
																	//		//{
																	//		//	GUI.DrawResourceSmall(ref resource, new(32, 32), true);
																	//		//	GUI.SameLine(8);
																	//		//}

																	//		////GUI.DrawInventory(h_inventory, true);
																	//	}
																	//}
																}

																GUI.SameLine();

																if (GUI.DrawButton("Control"u8, new Vector2(80, group.size.Y), color: GUI.col_button_ok,
																enabled: unit_player.IsNull() && character.IsNotNull() && character_data.IsNotNull() && character_data.faction == player_data.h_faction && (!character.h_player || character.h_player == player.h_player)))
																{
																	//Client.SetCharacter(character.character_id, true, force: !is_selected);
																	Client.SetCharacter(h_character, true, force: !is_selected);
																	//var rpc = new Character.SwitchRPC()
																	//{
																	//	h_character = character.character_id
																	//};
																	//rpc.Send();
																}

																if (behavior.IsNotNull())
																{
																	GUI.SameLine();

																	var ent_target_tmp = behavior.target_goal.entity;
																	if (GUI.EntityPicker("picker.job"u8, ">>"u8, size: new(GUI.RmX, group.size.Y), region_id: region.GetID(),
																	entity: ref ent_target_tmp))
																	{
																		ref var transform_target_tmp = ref ent_target_tmp.GetTransform();
																		if (transform_target_tmp.IsNotNull())
																		{
																			var rpc = new Commandable.DEV_SetOrderRPC()
																			{
																				order_type = edit_order_type,
																				ent_target = ent_target_tmp,
																				position_a = transform_target_tmp.position
																			};

																			//App.WriteValue((ent_target_tmp, ent_unit_tmp));
																			rpc.Send(ent_unit);
																		}

																		//edit_rpc.ent_target = selected_squad_copy.ent_target;
																		//rpc_dirty = true;
																	}
																}

																//GUI.SameLine();


																if (GUI.Selectable3("select"u8, group.GetOuterRect(), selected: is_selected))
																{
																	//var rpc = new SelectRPC()
																	//{
																	//	a = this.selection.a,
																	//	b = this.selection.b,
																	//};
																	//rpc.units[0] = ent_unit;

																	//rpc.Send(this.ent_selection);

																	IComponent.Handle.FromComponent<AI.Data>().RequestSync(ent_unit);
																	IComponent.Handle.FromComponent<AI.Behavior>().RequestSync(ent_unit);
																	IComponent.Handle.FromComponent<AI.Movement>().RequestSync(ent_unit);
																	IComponent.Handle.FromComponent<Commandable.Data>().RequestSync(ent_unit);
																	IComponent.Handle.FromComponent<NPC.Data>().RequestSync(ent_unit);

																	edit_ent_selected.Toggle(ent_unit);
																	GUI.selected_entity.Toggle(ent_unit);
																}

																if (!GUI.IsAnyTooltipVisible() && GUI.IsHoveringRect(group.GetOuterRect()))
																{
																	using (var tooltip = GUI.Tooltip.New(size: new(308, 0)))
																	{
																		ref var storage = ref ent_unit.GetComponent<Storage.Data>();
																		if (storage.IsNotNull())
																		{
																			if (storage.inv_storage.TryGetHandle(out var h_inventory))
																			{
																				using (GUI.Group.New(size: h_inventory.GetFrameSize(2, 0)))
																				{
																					GUI.DrawInventory(h_inventory, is_readonly: true);
																				}
																				//Span<Resource.Data> resources = stackalloc Resource.Data[h_inventory.Length];
																				//h_inventory.CountResources(ref resources);

																				//foreach (ref var resource in resources)
																				//{
																				//	GUI.DrawResourceSmall(ref resource, new(32, 32), true);
																				//	GUI.SameLine(8);
																				//}

																				////GUI.DrawInventory(h_inventory, true);
																				///
																				GUI.NewLine();
																			}
																		}

																		if (npc.ent_toolbelt.IsAlive())
																		{
																			Span<Entity> children_toolbelt = stackalloc Entity[8];
																			npc.ent_toolbelt.GetChildren(ref children_toolbelt, Relation.Type.Stored);

																			foreach (var ent_child in children_toolbelt)
																			{
																				GUI.TrySameLine(48);

																				using (var group_item = GUI.Group.New(size: new Vector2(48, 48)))
																				{
																					group_item.DrawBackground(GUI.tex_slot_simple);

																					GUI.DrawEntityIcon(ent_child);
																				}
																			}
																		}

																		GUI.SeparatorThick();

																		using (var group_tooltip = GUI.Group.New(size: new(GUI.RmX, 0), padding: new(4)))
																		{
																			using (GUI.Wrap.Push(GUI.RmX))
																			{
																				if (npc.IsNotNull())
																				{
																					GUI.LabelShaded("Consciousness:"u8, npc.consciousness, "P2");
																					GUI.LabelShaded("Pain:"u8, npc.pain, "0");
																					GUI.LabelShaded("Item:"u8, npc.item_hints.ToFormattedString(), size_b: 12, color_b: GUI.font_color_desc);
																					GUI.LabelShaded("Self:"u8, npc.self_hints.ToFormattedString(), size_b: 12, color_b: GUI.font_color_desc);
																				}

																				//ref var behavior = ref ent_unit.GetComponent<AI.Behavior>();
																				if (behavior.IsNotNull())
																				{
																					GUI.LabelShaded("Targeting:"u8, behavior.target_flags.ToFormattedString(), size_b: 12, color_b: GUI.font_color_desc);
																					GUI.LabelShaded("Combat:"u8, behavior.combat_flags.ToFormattedString(), size_b: 12, color_b: GUI.font_color_desc);
																					GUI.LabelShaded("Movement:"u8, behavior.movement_flags.ToFormattedString(), size_b: 12, color_b: GUI.font_color_desc);
																					GUI.LabelShaded("Target Hints:"u8, behavior.target_self_hints.ToFormattedString(), size_b: 12, color_b: GUI.font_color_desc);
																					GUI.LabelShaded("Target Items:"u8, behavior.target_item_hints.ToFormattedString(), size_b: 12, color_b: GUI.font_color_desc);
																				}

																				ref var ai = ref ent_unit.GetComponent<AI.Data>();
																				if (ai.IsNotNull())
																				{
																					//GUI.LabelShaded("Stance:", ai.stance);

																					GUI.NewLine(2);
																					GUI.Separator(thickness: 1.00f);
																					GUI.NewLine(4);

																					GUI.LabelShaded("Fear:"u8, ai.fear, "0");
																					GUI.LabelShaded("Stress:"u8, ai.stress, "0");
																					GUI.LabelShaded("Courage:"u8, ai.courage, "0");
																					GUI.LabelShaded("Determination:"u8, ai.determination, "0");
																					GUI.LabelShaded("Frustration:"u8, ai.frustration, "0");
																					GUI.LabelShaded("Anger:"u8, ai.anger, "0");
																				}
																			}
																		}
																	}
																}
															}
															else
															{
																//GUI.DrawRect(group.GetOuterRect(), Color32BGRA.Red, layer: GUI.Layer.Foreground);
																//App.WriteLine("culled");

															}


														}


														//if (npc.target_position.TryGetValue(out var target_position))
														//{
														//	GUI.DrawLine(GUI.WorldToCanvas(pos), GUI.WorldToCanvas(target_position), 0xffffffff, 1.00f);
														//}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}

				//if (active && false)
				//{
				//	var ref_commandable = this.selection.units[0];

				//	ref var commandable = ref ref_commandable.GetValueOrNullRef();
				//	if (!commandable.IsNull())
				//	{
				//		ref var body = ref ref_commandable.entity.GetComponent<Body.Data>();
				//		if (!body.IsNull())
				//		{
				//			var rect_commandable = body.GetAABB();
				//			var rect_commandable_size = rect_commandable.GetSize();

				//			var pos = body.GetPosition();
				//			var pos_c = GUI.WorldToCanvas(pos + new Vector2(0, -3)) + new Vector2(0, -8);

				//			var button_size = new Vector2(48, 48);

				//			using (var window = GUI.Window.Standalone("Derp", position: pos_c, size: new(button_size.X * 3, button_size.Y * 2), pivot: new(0.50f, 0.50f)))
				//			{
				//				if (window.show)
				//				{
				//					GUI.DrawWindowBackground(GUI.tex_panel, new Vector4(4, 4, 4, 4));

				//					foreach (var order_type in order_types)
				//					{
				//						if (order_type == Commandable.OrderType.Undefined) continue;

				//						GUI.SameLine();
				//						if (GUI.RmX < button_size.X)
				//						{
				//							//App.WriteLine(GUI.RmX);
				//							GUI.NewLine();
				//						}

				//						using (GUI.Group.New(size: button_size))
				//						{
				//							//if (GUI.DrawButton(order_type.ToString(), new(48, 48), enabled: this.selection.order_type != order_type))
				//							if (GUI.DrawSpriteButton(order_type.ToString(), new Sprite(tex_icons_npc, 16, 16, (uint)order_type - 1, 0), new Vector2(48, 48), enabled: this.selection.order_type != order_type))
				//							{
				//								var rpc = new Selection.SetOrderTypeRPC()
				//								{
				//									order_type = order_type
				//								};
				//								rpc.Send(this.ent_selection);
				//							}

				//							if (GUI.IsItemHovered())
				//							{
				//								using (GUI.Tooltip.New()) GUI.Text(order_type.ToString());
				//							}
				//						}
				//					}
				//				}
				//			}
				//		}
				//	}
				//}
			}
		}

		public static bool show_always = true;

		[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("local", true, Source.Modifier.Owned)]
		public static void OnGUI(Entity entity, [Source.Owned] in Player.Data player, [Source.Owned] in Selection.Data selection)
		{
			//const bool show_always = true;
			if (Selection.show_always || ((player.flags.HasAny(Player.Flags.Editor) && !Editor.show_respawn_menu) || player.ent_controlled.IsAlive()))
			{
				var gui = new SelectionGUI()
				{
					ent_selection = entity,
					selection = selection,
				};
				gui.Submit();
			}
		}
#endif
	}
}
