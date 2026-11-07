using Keg.Extensions;
using System.Diagnostics;
using TC2.Base.Components;

#if CLIENT
namespace TC2.Base
{
	public static partial class SideMenu
	{
		public ref partial struct DrawSquads(IFaction.Handle h_faction, Entity ent_player, ref Selection.Data selection)
		{
			public IFaction.Handle h_faction = h_faction;
			public Entity ent_player = ent_player;
			public ref Selection.Data selection = ref selection;

			[ISystem.Manual(ISystem.Mode.Single, ISystem.Scope.Region)]
			public static void System(ISystem.Info info, ref Region.Data region, Entity ent_squad, [ISystem.Parameter] ref DrawSquads arg,
			[Source.Owned] ref Squad.Data squad, [Source.Owned] ref Transform.Data transform,
			[Source.Owned, Optional] in Faction.Data faction)
			{
				if (arg.IsNull()) return;
				if (arg.selection.IsNull()) return;
				if (faction.id != arg.h_faction) return;

				using (var push_id = GUI.ID<SideMenu.DrawSquads, Squad.Data>.Push(ent_squad))
				//using (var group_row = GUI.Group.New(size: new(GUI.RmX, 32), padding: new(4)))
				using (var group_row = GUI.Group.New(size: new(GUI.RmX, 40)))
				{
					using (var group_icon = GUI.Group.New(size: new(GUI.RmY)))
					{
						//group_item.DrawBackground(GUI.tex_slot_simple);
						GUI.DrawEntityIcon(ent_squad, scale: 2,
							flags: (GUI.DrawEntityIconFlags.Draw_Frame, GUI.DrawEntityIconFlags.None));
					}

					GUI.SameLine();

					using (var group_info = GUI.Group.New(size: GUI.Rm))
					{
						group_info.DrawBackground(GUI.tex_slot_simple);
						var squad_name = ent_squad.GetName();

						GUI.TitleCentered(squad_name, size: 20, pivot: new(0.00f, 0.50f), offset: new(6, 0));

						var is_selected = arg.selection.ref_selected_squad.entity == ent_squad;
						if (GUI.Selectable3(push_id.hash, group_row.GetOuterRect(), selected: is_selected))
						{
							var rpc = new Selection.SelectSquadRPC()
							{
								ent_squad = ent_squad
							};
							rpc.Send(arg.ent_player);

							Sidebar.Widget.SetActive("selection.squads", true); // !is_selected);
						}
					}
				}
			}
		}

		static partial void Ext_DrawScrollbox_Top(ref SideMenuGUI gui, ref readonly GUI.Window window, ref readonly GUI.Scrollbox scrollbox)
		{
			if (!Client.HasRegion()) return;
			//if (Client.IsLoadingRegion()) return;
			if (!Client.IsRegionReady()) return;

			ref var region = ref Client.GetRegion();
			if (region.IsNotNull())
			{
				ref var siege_state = ref region.GetSingletonComponent<Siege.Siege.Gamemode.Singleton>();
				if (siege_state.IsNotNull())
				{
					var h_faction_client = Client.GetFactionHandle();
					var ent_player = Client.GetEntity();
					ref var selection = ref ent_player.GetComponent<Selection.Data>();

					if (World.IsDevMode)
					{
						using (var group_buttons = GUI.Group.New(size: new(GUI.RmX, 40)))
						{
							if (GUI.DrawButton("Defenders"u8, size: new(GUI.RmX * 0.50f, GUI.RmY),
							error: h_faction_client == siege_state.h_faction_defenders, color: siege_state.h_faction_defenders.GetColorA()))
							{
								var rpc = new Siege.Siege.DEV_ChangeTeamRPC
								{
									h_faction = siege_state.h_faction_defenders
								};
								rpc.Send(region.GetID());
							}

							GUI.SameLine();

							if (GUI.DrawButton("Attackers"u8, size: GUI.Rm,
							error: h_faction_client == siege_state.h_faction_attackers, color: siege_state.h_faction_attackers.GetColorA()))
							{
								var rpc = new Siege.Siege.DEV_ChangeTeamRPC
								{
									h_faction = siege_state.h_faction_attackers
								};
								rpc.Send(region.GetID());
							}
						}
					}

					//GUI.SeparatorThick();

					//using (var group_squads = GUI.Group.New(size: new(GUI.RmX, 40)))
					//{

					//}

					GUI.SeparatorThick();

					const bool enable_squads = true;
					if (enable_squads && selection.IsNotNull())
					{
						using (var collapsible = GUI.Collapsible2.New("col.squads"u8, size: new(GUI.RmX, 32), default_open: true))
						{
							GUI.TitleCentered("Squads"u8, size: 24, pivot: new(0.00f, 0.50f));

							if (collapsible.Inner())
							{
								using (var group_squads = GUI.Group.New(size: new(GUI.RmX, 0)))
								{
									scoped var args = new DrawSquads(h_faction: h_faction_client, ent_player: ent_player, selection: ref selection);
									region.RunSystem(DrawSquads.System, ref args);
								}
							}
						}
					}
				}
			}
		}

		static partial void Ext_DrawScrollbox_Bottom(ref SideMenuGUI gui, ref readonly GUI.Window window, ref readonly GUI.Scrollbox scrollbox)
		{

		}
	}
}
#endif

namespace TC2.Siege
{
	public static partial class Siege
	{
#if CLIENT
		public partial struct SiegeDefenderGUI: IGUICommand
		{
			public Siege.Gamemode g_siege;
			public Siege.Gamemode.Singleton g_siege_state;
			public Siege.Bounty.Singleton g_bounty;

			public static readonly Texture.Handle tex_icons_minimap = "ui_icons_minimap";
			public static Entity? ent_selected_spawn_new;

			public Entity ent_respawn;
			public IFaction.Handle faction_id;
			public Respawn.Data respawn;

			public static ICharacter.Handle h_selected_character;
			[TODO("Workaround")] public static Entity todo_ent_selected_spawn;
			
			[Shitcode] // TODO: update this
			public void Draw()
			{
				ref var player = ref Client.GetPlayer();
				ref var region = ref Client.GetRegion();

				var window_pos = (GUI.CanvasSize * new Vector2(0.50f, 0.00f)) + new Vector2(0, 64 + 4);
				using (var window = GUI.Window.Standalone("Siege"u8, position: window_pos, size: new Vector2(400, 52), pivot: new Vector2(0.50f, 0.00f), padding: new(4)))
				{
					this.StoreCurrentWindowTypeID();
					if (window.show)
					{
						//GUI.DrawWindowBackground();

						ref var world = ref Client.GetWorld();
						ref var game_info = ref Client.GetGameInfo();

						var time_left = MathF.Max(this.g_siege_state.t_next_wave - this.g_siege_state.t_match_elapsed, 0.00f);

						using (GUI.Group.New(size: new Vector2(GUI.RmX - 140, GUI.RmY)))
						{
							if (this.g_siege_state.status == Gamemode.Status.Running && this.g_siege_state.flags.HasAny(Siege.Gamemode.Flags.Active))
							{
								GUI.Title($"Incoming in {(time_left):0} s", size: 22, color: time_left > 10.00f ? GUI.font_color_title : GUI.font_color_yellow);

								if (this.g_bounty.t_next_payout != 0.00f)
								{
									var rewards = this.g_bounty.rewards.AsSpan();
									if (rewards.HasAny())
									{
										var multiplier = Maths.Lerp(1.00f, 1.00f / (float)g_siege_state.player_count, g_siege.reward_share_ratio) * g_siege.reward_mult;

										foreach (ref var reward in rewards)
										{
											if (reward.type == Crafting.Product.Type.Resource && reward.material == Money.h_material_money)
											{
												var amount = Money.ToBataPrice(reward.amount * multiplier);
												GUI.Title($"Payout of {amount:0.00} {Money.symbol} in {(MathF.Max(this.g_bounty.t_next_payout - this.g_siege_state.t_match_elapsed, 0.00f)):0}s", font: GUI.Font.Monaco, size: 14, color: GUI.font_color_green);

												break;
											}
										}
										//GUI.Title($"+{this.g_bounty.} {(MathF.Max(this.g_bounty.t_next_update - this.g_siege_state.t_match_elapsed, 0.00f)):0} s", size: 22);
									}
								}
								//GUI.Title($"Difficulty: {this.g_siege_state.difficulty:0.0}", size: 22);
								//Shop.DrawProducts(ref region, default, default, default, this.g_bounty.rewards.AsSpan(), 1);
							}
							else
							{
								GUI.TitleCentered(this.g_siege_state.flags.HasAny(Siege.Gamemode.Flags.Paused) ? "Paused"u8 : this.g_siege_state.status.ToStringUtf8(), size: 32, pivot: new(0.00f, 0.50f), color: GUI.font_color_yellow);
							}
						}

						GUI.SameLine();

						using (GUI.Group.New(size: new Vector2(GUI.RmX, GUI.RmY)))
						{
							GUI.TitleCentered($"Wave: {this.g_siege_state.wave_current}", size: 32, pivot: new(1.00f, 0.00f));
							GUI.TitleCentered($"Hazard: {this.g_siege_state.difficulty:0.0}", size: 20, pivot: new(1.00f, 1.00f));
						}
					}
				}

				if (!player.flags.HasAll(Player.Flags.Alive) && !(player.flags.HasAll(Player.Flags.Editor) && !Editor.show_respawn_menu))
				{
					var max_height = GUI.CanvasSize.Y - Spawn.RespawnGUI.window_offset.Y - 12;

					Spawn.RespawnGUI.window_size.Y = Maths.Clamp(Spawn.RespawnGUI.window_size.Y, 0, max_height);

					ref var ent_selected_spawn = ref todo_ent_selected_spawn;
					ent_selected_spawn = this.respawn.ent_selected_spawn;

					using (var window = GUI.Window.Standalone("Respawn"u8, position: new Vector2(GUI.CanvasSize.X * 0.50f, 0) + Spawn.RespawnGUI.window_offset, pivot: Spawn.RespawnGUI.window_pivot, size: Spawn.RespawnGUI.window_size))
					{
						Spawn.RespawnGUI.window_size = new Vector2(800, 600);

						this.StoreCurrentWindowTypeID();
						if (window.show)
						{
							GUI.DrawWindowBackground(GUI.tex_window_menu, padding: new Vector4(8, 8, 8, 8));

							using (GUI.Group.New(size: new Vector2(GUI.RmX, GUI.RmY), padding: new(8, 8)))
							{
								if (!ent_selected_spawn.IsAlive())
								{
									foreach (ref var row in region.IterateQuery<Region.GetSpawnsQuery>())
									{
										row.Run((ISystem.Info info, Entity entity, in Spawn.Data spawn, in Nameable.Data nameable, in Transform.Data transform, in Faction.Data faction) =>
										{
											ref var player = ref Client.GetPlayer();
											if (faction.id == 0 || (faction.id == player.faction_id))
											{
												if (todo_ent_selected_spawn.id == 0 || info.GetRandom().NextBool(0.30f)) ent_selected_spawn_new = entity;
											}
										});
									}
								}

								{
									ref var info = ref region.GetMapInfo();

									using (GUI.Wrap.Push(GUI.RmX))
									{
										using (var group = GUI.Group.New(size: new(GUI.RmX, 0), padding: new(4)))
										{
											//ref var minimap = ref Minimap.MinimapHUD.minimaps[region.GetID()];
											//if (minimap != null)
											//{
											//	var map_frame_size = minimap.GetFrameSize(2);
											//	map_frame_size = map_frame_size.ScaleToSize(new Vector2(GUI.RmX, 80));

											//	Minimap.DrawMap(ref region, minimap, map_frame_size, map_scale: 1.00f);
											//}

											var minimap = Minimap.MinimapHUD.minimaps[region.GetID()];
											if (minimap != null)
											{
												var map_frame_size = minimap.GetFrameSize(2);
												map_frame_size = map_frame_size.ScaleToSize(new Vector2(GUI.RmX, 80));

												using (var map = GUI.Map.New(ref region, minimap, size: map_frame_size, map_scale: 1.00f, draw_markers: false))
												{
													//ISystem.Info info, Entity entity, [Source.Owned] in Minimap.Marker.Data marker, [Source.Owned] in Transform.Data transform, [Source.Owned, Optional] in Faction.Data faction, [Source.Owned, Optional] in Nameable.Data nameable

													foreach (ref var row in region.IterateQuery<Minimap.GetMarkersQuery>())
													{
														var selected = row.Entity == ent_selected_spawn;

														var transform_copy = default(Transform.Data);
														//var nameable_copy = default(Nameable.Data);
														var color = selected ? Color32BGRA.White : new Color32BGRA(0xff9a7f7f);
														var faction_id_tmp = this.faction_id;

														var ok = false;

														row.Run((ISystem.Info info, Entity entity, [Source.Owned] in Minimap.Marker.Data marker, [Source.Owned] in Transform.Data transform, [Source.Owned, Optional] in Faction.Data faction, [Source.Owned, Optional] in Nameable.Data nameable) =>
														{
															transform_copy = transform;
															//nameable_copy = nameable;

															if (faction.id.TryGetData(out var ref_faction) && faction.id == faction_id_tmp && marker.flags.HasAll(Minimap.Marker.Flags.Faction | Minimap.Marker.Flags.Spawner))
															{
																color = ref_faction.value.color_a;
																//sprite.frame.X = 1;
																ok = true;
															}
														});

														if (ok)
														{
															using (var node = map.DrawNode(new Sprite(tex_icons_minimap, 16, 16, 3, 0), transform_copy.GetInterpolatedPosition() + new Vector2(0, -3), color: selected ? Color32BGRA.White : color, color_hovered: selected ? Color32BGRA.White : Color32BGRA.Lerp(color, Color32BGRA.White, 0.50f)))
															{
																//GUI.DrawTextCentered(nameable_copy.name, node.rect.GetPosition() + new Vector2(16, 0), piv layer: GUI.Layer.Window, font: GUI.Font.Superstar, size: 16);

																if (node.is_hovered && !selected)
																{
																	GUI.SetCursor(App.CursorType.Hand, 100);

																	if (GUI.GetMouse().GetKeyDown(Mouse.Key.Left))
																	{
																		//App.WriteLine("press");
																		ent_selected_spawn_new = row.Entity;
																	}

																	using (GUI.Tooltip.New())
																	{
																		//GUI.Title(nameable_copy.name, font: GUI.Font.Superstar, size: 16);
																		GUI.Title(row.Entity.GetFullName(), font: GUI.Font.Superstar, size: 16);
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

								GUI.SeparatorThick();

								if (ent_selected_spawn.IsAlive())
								{
									var context = GUI.ItemContext.Begin(is_readonly: true);

									ref var dormitory = ref ent_selected_spawn.GetComponent<Dormitory.Data>();
									ref var armory = ref ent_selected_spawn.GetComponent<Armory.Data>();
									ref var shipment = ref ent_selected_spawn.GetComponent<Shipment.Data>();
									var h_inventory = default(Inventory.Handle);

									var h_selected_character_tmp = h_selected_character;

									if (dormitory.IsNotNull())
									{
										if (h_selected_character_tmp.id != 0 && !dormitory.GetCharacterSpan().Contains(h_selected_character_tmp))
										{
											h_selected_character_tmp = default;
										}
									}

									if (armory.IsNotNull())
									{
										armory.inv_storage.TryGetHandle(out h_inventory);
									}

									//Crafting.Context.New(ref region, ent_selected_spawn, ent_selected_spawn, out var crafting_context, inventory: h_inventory, shipment: new(ref shipment, ent_selected_spawn), search_radius: 0.00f);

									var selected_items = Spawn.RespawnGUI.character_id_to_selected_items.GetOrAdd(h_selected_character_tmp);

									//var context = GUI.ItemContext.Begin();

									using (GUI.Group.New(size: GUI.Rm with { X = 400 }, padding: new(0, 0)))
									{
										using (var scrollable = GUI.Scrollbox.New("characters"u8, size: GUI.GetRemainingSpace(y: -96 - 8 - 8), padding: new(4, 4), force_scrollbar: true))
										{
											if (dormitory.IsNotNull())
											{
												var characters = dormitory.GetCharacterSpan();
												for (var i = 0; i < characters.Length; i++)
												{
													//DrawCharacter(characters[i].GetHandle());

													using (GUI.ID<Siege.SiegeDefenderGUI, Character.Data>.Push(i))
													using (var group_row = GUI.Group.New(size: new(GUI.RmX, 40)))
													{
														if (group_row.IsVisible())
														{
															var h_character = characters[i];

															if (h_character && !h_selected_character_tmp)
															{
																h_selected_character_tmp = h_character;
																h_selected_character = h_character;
															}

															Dormitory.DrawCharacterSmall(h_character);

															var selected = h_selected_character_tmp && h_character == h_selected_character_tmp; // selected_index;
															if (GUI.Selectable3("selectable"u8, group_row.GetOuterRect(), selected))
															{
																h_selected_character = h_character;
															}
														}
													}
												}
											}
										}

										using (var group_storage = GUI.Group.New(size: GUI.Rm, padding: new(8, 8)))
										{
											GUI.DrawBackground(GUI.tex_frame, group_storage.GetOuterRect(), new(8, 8, 8, 8));

											if (h_inventory.IsValid())
											{
												using (GUI.Group.New(size: h_inventory.GetPreferedFrameSize()))
												{
													GUI.DrawInventory(h_inventory, is_readonly: true);
												}
											}

											GUI.SameLine();

											if (shipment.IsNotNull())
											{
												using (GUI.Group.New(size: GUI.Rm))
												{
													GUI.DrawShipment(ref context, ent_selected_spawn, ref shipment, slot_size: new(96, 48));
												}
											}
										}

									}

									GUI.SameLine();

									using (var group_character = GUI.Group.New(size: GUI.Rm))
									{
										ref var character_data = ref h_selected_character_tmp.GetData();

										using (var group_title = GUI.Group.New(size: new(GUI.RmX, 24), padding: new(8, 8)))
										{
											if (character_data.IsNotNull())
											{
												GUI.TitleCentered(character_data.name, size: 24, pivot: new(0.00f, 0.00f));
											}
											else
											{
												GUI.TitleCentered("<no character selected>"u8, size: 24, pivot: new(0.00f, 0.00f));
											}
										}

										GUI.SeparatorThick();

										using (var group_title = GUI.Group.New(size: GUI.GetRemainingSpace(y: -350), padding: new(8, 8)))
										{

										}

										GUI.SeparatorThick();

										using (var group_kits = GUI.Group.New(size: GUI.GetRemainingSpace(y: -48), padding: new(8, 8)))
										{
											GUI.DrawBackground(GUI.tex_panel, group_kits.GetOuterRect(), new(8, 8, 8, 8));
											var sw = new Stopwatch();

											using (var scrollable = GUI.Scrollbox.New("kits"u8, size: GUI.Rm, padding: new(4, 4), force_scrollbar: true))
											{
												if (dormitory.IsNotNull() && dormitory.flags.HasAny(Dormitory.Flags.No_Kit_Selection))
												{
													if (character_data.IsNotNull())
													{
														foreach (var h_kit in character_data.kits)
														{
															using (var group_row = GUI.Group.New(size: new(GUI.RmX, 40)))
															{
																Dormitory.DrawKit(h_kit, group_row.GetOuterRect(), true, true, force_readonly: true, ignore_requirements: dormitory.flags.HasAny(Dormitory.Flags.No_Kit_Requirements));
															}
														}
													}
												}
												else
												{
													if (character_data.IsNotNull() && armory.IsNotNull() && shipment.IsNotNull() && h_inventory.IsValid())
													{
														var shipment_armory_span = shipment.items.AsSpan();

														var kits_unavailable_count = 0;
														Span<IKit.Handle> kits_unavailable = stackalloc IKit.Handle[32];

														foreach (var asset in IKit.Database.GetAssets())
														{
															if (asset.id == 0) continue;
															ref var kit_data = ref asset.GetData(out var h_kit);

															if (kit_data.character_flags.Evaluate(character_data.traits) < 0.50f) continue;

															var valid = false;

															if (h_kit.Evaluate(ref character_data))
															{
																Span<Crafting.Requirement> requirements = stackalloc Crafting.Requirement[8];

																foreach (ref var item in kit_data.shipment.items)
																{
																	if (!item.IsValid()) continue;
																	if (item.flags.HasAny(Shipment.Item.Flags.No_Consume)) continue;

																	requirements.Merge(item.ToRequirement());
																}

																// TODO
																if (true) //crafting_context.Evaluate(requirements, Crafting.EvaluateFlags.None))
																{
																	valid = true;
																}
															}

															//GUI.Text($"{valid}");

															if (valid)
															{
																DrawKit(ref h_kit, ref kit_data, ref h_inventory, ref shipment_armory_span, true, selected_items);
															}
															else
															{
																kits_unavailable.Add(h_kit, ref kits_unavailable_count);
															}
														}

														for (var i = 0; i < kits_unavailable_count; i++)
														{
															var h_kit_unavailable = kits_unavailable[i];
															ref var kit_unavailable_data = ref h_kit_unavailable.GetData();

															if (kit_unavailable_data.IsNotNull())
															{
																DrawKit(ref h_kit_unavailable, ref kit_unavailable_data, ref h_inventory, ref shipment_armory_span, false, selected_items);
															}
														}
													}
												}
											}
										}

										if (ent_selected_spawn.IsAlive())
										{
											ref var faction = ref ent_selected_spawn.GetComponent<Faction.Data>();
											if (faction.IsNull() || faction.id == player.faction_id)
											{
												if (GUI.DrawButton("DEV: Generate"u8, size: new Vector2(128, 48), enabled: dormitory.IsNotNull()))
												{
													var rpc = new Dormitory.DEV_RerollRPC()
													{
														add = 1
													};
													rpc.Send(ent_selected_spawn);
												}

												GUI.SameLine();

												if (GUI.DrawButton("Respawn"u8, size: new Vector2(GUI.RmX, 48), color: GUI.col_button_ok, enabled: h_selected_character_tmp.id != 0 && dormitory.IsNotNull()))
												{
													var rpc = new Spawn.SpawnRPC
													{
														h_character = h_selected_character_tmp,
														flags = Spawn.SpawnEvent.Flags.Control, // control = true
													};

													foreach (var h_kit in selected_items)
													{
														rpc.kits.TryAdd(in h_kit);
													}

													rpc.Send(ent_selected_spawn);

													h_selected_character = default;
												}
											}
											else
											{
												ent_selected_spawn = default;
											}
										}
									}
								}

								if (ent_selected_spawn_new.HasValue && ent_selected_spawn_new != ent_selected_spawn)
								{
									var rpc = new RespawnExt.SetSpawnRPC()
									{
										ent_spawn = ent_selected_spawn_new.Value
									};
									rpc.Send(player.ent_player);
									ent_selected_spawn_new = default;
								}
							}
						}
					}
				}
			}

			private static void DrawKit(ref IKit.Handle h_kit, ref IKit.Data kit_data, ref Inventory.Handle h_inventory, ref Span<Shipment.Item> shipment_armory_span, bool valid, HashSet<IKit.Handle> selected_items)
			{
				using (GUI.ID.Push(h_kit.id))
				{
					using (var group_row = GUI.Group.New(size: new(GUI.RmX, 40), padding: new(0, 0)))
					{
						if (group_row.IsVisible())
						{
							GUI.DrawBackground(GUI.tex_panel, group_row.GetOuterRect(), new(8, 8, 8, 8));

							using (var group_title = GUI.Group.New(size: new(128, GUI.RmY), padding: new(4, 0)))
							{
								GUI.TitleCentered(kit_data.name, pivot: new(0.00f, 0.50f), color: GUI.font_color_title.WithAlphaMult(valid ? 1.00f : 0.50f));
							}

							GUI.SameLine();

							//GUI.DrawItems(shipment.items.AsSpan(), is_readonly: true);
							//GUI.DrawItems(shipment.items.AsSpan(), is_readonly: true);

							var sameline = false;
							foreach (ref var item in kit_data.shipment.items)
							{
								if (!item.IsValid()) continue;

								if (sameline) GUI.SameLine();
								sameline = true;

								var has_item = item.flags.HasAny(Shipment.Item.Flags.No_Consume) || shipment_armory_span.Contains(item);
								if (!has_item && item.type == Shipment.Item.Type.Resource)
								{
									has_item = h_inventory.GetQuantity(item.material) >= item.quantity;
								}

								if (has_item)
								{

								}

								GUI.DrawItem(ref item, size: new(40), is_readonly: true, text_color: has_item ? GUI.font_color_default : GUI.col_button_error.WithAlphaMult(0.50f), icon_color: has_item ? Color32BGRA.White : GUI.col_button_error.WithAlphaMult(0.50f));
							}

							var selected = selected_items.Contains(h_kit);
							if (GUI.Selectable3("selectable", group_row.GetOuterRect(), selected, is_readonly: !valid))
							{
								if (selected) selected_items.Remove(h_kit);
								else selected_items.Add(h_kit);
							}
						}
					}
				}
			}
		}

		[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("local", true, Source.Modifier.Owned)]
		public static void OnGUIDefender(Entity entity,
		[Source.Owned] in Player.Data player, [Source.Owned] in Respawn.Data respawn,
		[Source.Global] in Siege.Gamemode g_siege, [Source.Singleton] in Siege.Gamemode.Singleton g_siege_state, [Source.Singleton] in Siege.Bounty.Singleton g_bounty)
		{
			if (player.faction_id == g_siege_state.h_faction_defenders)
			{
				Spawn.RespawnGUI.enabled = true;

				var gui = new SiegeDefenderGUI()
				{
					ent_respawn = entity,
					faction_id = player.faction_id,
					respawn = respawn,

					g_siege = g_siege,
					g_siege_state = g_siege_state,
					g_bounty = g_bounty
				};
				gui.Submit();
			}
		}
#endif

#if CLIENT
		public partial struct SiegeAttackerGUI: IGUICommand
		{
			public Siege.Gamemode g_siege;
			public Siege.Gamemode.Singleton g_siege_state;

			public static EntRef<Dormitory.Data> ref_selected_dormitory;
			public static uint? dormitory_selected_index;

			public void Draw()
			{
				var window_pos = (GUI.CanvasSize * new Vector2(0.50f, 0.00f)) + new Vector2(0, 80);

				//using (var window = GUI.Window.Standalone("Siege2", position: window_pos, size: new Vector2(650, 540), pivot: new Vector2(0.50f, 0.00f), padding: new(6, 6), force_position: false))
				using (var widget = Sidebar.Widget.New(identifier: "siege.attackers", name: "Siege",
				icon: new Sprite(GUI.tex_icons_widget, 16, 16, 3, 0),
				size: new Vector2(650, 400), lockable: false, order: 5.00f, flags: Sidebar.Widget.Flags.Align_Right))
				{
					//this.StoreCurrentWindowTypeID();
					//if (window.show)
					if (widget.state_flags.HasAny(Sidebar.Widget.StateFlags.Show))
					{
						GUI.DrawWindowBackground(GUI.tex_window_menu);

						using (GUI.Group.New(size: GUI.Av, padding: new(4)))
						{
							ref var region = ref Client.GetRegion();
							ref var world = ref Client.GetWorld();
							ref var game_info = ref Client.GetGameInfo();
							ref var player = ref Client.GetPlayer();

							var ent_selected_squad = default(Entity);

							ref var selection = ref player.ent_player.GetComponent<Selection.Data>();
							if (selection.IsNotNull())
							{
								ent_selected_squad = selection.ref_selected_squad;
							}

							//Interactable.SetCurrentTarget(ent_selected_squad);


							var h_faction = g_siege_state.h_faction_attackers;
							//var total_count = region.GetTotalTagCount("kobold", "dead");

							//GUI.Title($"{this.g_siege_state.faction_defenders.id}");
							//GUI.Title($"{total_count}/{this.g_siege.max_npc_count} kobolds");



							GUI.SeparatorThick();

							//using (var group_bottom = GUI.Group.New(size: GUI.Rm))
							{
								ref var selected_dormitory = ref ref_selected_dormitory.GetComponent();
								if (selected_dormitory.IsNull())
								{
									//Span<Entity> span_dormitories_tmp = stackalloc Entity[1];
									//region.GetEntsWithComponent<>

									App.WriteLine("Finding default spawner...");

									foreach (ref var row in region.IterateQuery<Region.GetSpawnsQuery>())
									{
										row.Run((ISystem.Info info, Entity entity, [Source.Owned] in Spawn.Data spawn, [Source.Owned, Optional] in Nameable.Data nameable, [Source.Owned] in Transform.Data transform, [Source.Owned, Optional] in Faction.Data faction) =>
										{
											if (faction.id == h_faction)
											{
												ref_selected_dormitory.Set(entity);
											}
										});

										if (ref_selected_dormitory.IsValid()) break;
									}
								}

								ref var selected_dormitory_squad = ref Squad.Data.Null;
								if (selected_dormitory.IsNotNull()) selected_dormitory_squad = ref ref_selected_dormitory.entity.GetComponent<Squad.Data>();

								var ent_dormitory = ref_selected_dormitory.entity;

								using (var group_left = GUI.Group.New(size: new(320, GUI.RmY)))
								{
									using (var group_info = GUI.Group.New(size: new(GUI.RmX, 64), padding: new(4)))
									{
										group_info.DrawBackground(GUI.tex_window);

										using (var group_icon = GUI.Group.New(size: new(GUI.RmY)))
										{
											//group_item.DrawBackground(GUI.tex_slot_simple);
											GUI.DrawEntityIcon(ent_dormitory, scale: 2,
												flags: (GUI.DrawEntityIconFlags.Draw_Frame, GUI.DrawEntityIconFlags.None));

										}

										if (GUI.IsItemHovered())
										{
											GUI.DrawEntityMarker(ent_dormitory);
										}

										// TODO: shithack
										if (GUI.Selectable3(GUI.Hash.New(ent_dormitory), rect: GUI.GetLastItemRect(), selected: false))
										{
											GUI.SetDebugEntity(ent_dormitory);

											ref var selected_dormitory_transform = ref ent_dormitory.GetTransform();
											if (selected_dormitory_transform.IsNotNull())
											{
												App.WriteValue(selected_dormitory_transform.position);
												ref var camera = ref region.GetSingletonComponent<Camera.Singleton>();
												if (camera.IsNotNull())
												{
													camera.override_target_position = selected_dormitory_transform.position;
													camera.position = selected_dormitory_transform.position.SubY(6);
												}
											}
										}

										GUI.SameLine();

										//GUI.DrawEntityIcon(ent_selected_squad);

										//using (var group_tmp = GUI.Group.New2(size: new(64, GUI.RmY)))
										//{
										//	GUI.TitleCentered("Funds:"u8, pivot: new(0.00f, 0.50f), size: 20);
										//}
										//GUI.SameLine(4);
										//GUI.OffsetLine(64);

										if (selected_dormitory_squad.IsNotNull())
										{
											GUI.DrawMoney(selected_dormitory_squad.money, size: new(GUI.RmX, 32));
										}
									}

									GUI.SeparatorThick();

									using (var scrollbox = GUI.Scrollbox.New("scroll.characters"u8, size: GUI.Rm))
									{
										if (selected_dormitory.IsNotNull())
										{
											var characters_span = selected_dormitory.GetCharacterSpan();

											var index = 0u;
											foreach (ref var h_character in characters_span)
											{
												//if (h_character.id != 0)
												//{
												using (GUI.ID<SiegeAttackerGUI, ICharacter.Handle>.Push(index))
												{
													using (var group_row = GUI.Group.New(size: new(GUI.RmX, 40)))
													{
														using (var group_row_left = GUI.Group.New(size: GUI.GetRemainingSpace(x: -76)))
														{
															group_row_left.DrawBackground(GUI.tex_panel);

															Dormitory.DrawCharacterSmall(h_character);

															//GUI.TitleCentered(entity.GetFullName(), size: 16, pivot: new(0.00f, 0.00f), offset: new(6, 2));

															//var selected = ref_selected_dormitory == entity;
															var selected = index == dormitory_selected_index;
															if (GUI.Selectable3("select"u8, group_row_left.GetOuterRect(), selected))
															{
																dormitory_selected_index = selected ? null : index;
																//ref_selected_dormitory = selected ? default : entity;
															}
														}

														GUI.SameLine();

														var is_valid = h_character.IsValid();
														var is_enemy = false;

														ref var character_data = ref h_character.GetData();
														if (character_data.IsNotNull())
														{
															is_enemy = character_data.faction != h_faction;
															is_valid &= character_data.faction == 0 || character_data.faction == h_faction;
														}

														var col_button = GUI.col_button_ok;
														if (is_valid)
														{
															col_button = GUI.col_button_ok;
														}
														else if (is_enemy)
														{
															col_button = GUI.col_button_error;
														}
														else
														{
															col_button = GUI.col_button;
														}

														if (is_enemy)
														{
															if (GUI.DrawButton("Execute"u8, size: GUI.Rm, enabled: true, color: GUI.col_button_error))
															{
																var rpc = new Siege.DEV_ExecuteUnitRPC()
																{
																	h_character = h_character
																};
																rpc.Send(ref_selected_dormitory);
															}

															GUI.DrawHoverTooltip("Execute this captured unit."u8);
														}
														else
														{
															if (GUI.DrawButton("Deploy"u8, size: GUI.Rm, enabled: is_valid, color: col_button))
															{
																var rpc = new Siege.DEV_SpawnUnitRPC()
																{
																	h_character = h_character,
																	ent_squad = ent_selected_squad
																};
																rpc.Send(ref_selected_dormitory);
															}

															GUI.DrawHoverTooltip("Deploy this unit at the selected spawn point."u8);
														}
													}

													index++;
												}
												//}
											}
										}
									}
								}

								GUI.SameLine();

								using (var group_right = GUI.Group.New(size: GUI.Rm, padding: new(4)))
								{
									group_right.DrawBackground(GUI.tex_window);

									using (var scrollbox = GUI.Scrollbox.New("scroll.spawns"u8, size: new(GUI.RmX, 120)))
									{
										foreach (ref var row in region.IterateQuery<Region.GetSpawnsQuery>())
										{
											row.Run((ISystem.Info info, Entity entity, [Source.Owned] in Spawn.Data spawn, [Source.Owned, Optional] in Nameable.Data nameable,
											[Source.Owned] in Transform.Data transform, [Source.Owned, Optional] in Faction.Data faction) =>
											{
												if (faction.id == h_faction)
												{
													using (GUI.ID.Push(entity))
													{
														using (var group_row = GUI.Group.New(size: new(GUI.RmX, 32)))
														{
															group_row.DrawBackground(GUI.tex_panel);

															using (var group_icon = GUI.Group.New(size: new(GUI.RmY)))
															{
																//group_item.DrawBackground(GUI.tex_slot_simple);
																GUI.DrawEntityIcon(entity, scale: 2,
																	flags: (GUI.DrawEntityIconFlags.Draw_Frame, GUI.DrawEntityIconFlags.None));

															}

															GUI.SameLine();

															GUI.TitleCentered(entity.GetFullName(), size: 20, pivot: new(0.00f, 0.50f), offset: new(6, 0));

															var selected = ref_selected_dormitory == entity;
															if (GUI.Selectable3("select"u8, group_row.GetOuterRect(), selected))
															{
																//ref_selected_dormitory = selected ? default : entity;
																if (ref_selected_dormitory.entity != entity)
																{
																	ref_selected_dormitory.Set(entity); // selected ? default : entity;
																}
															}
														}
													}
												}
											});
										}
									}

									GUI.SeparatorThick();

									if (selected_dormitory.IsNotNull())
									{
										//var h_species_kobold = new ISpecies.Handle("kobold");

										var h_species_dormitory = selected_dormitory.species;
										var characters_span = selected_dormitory.GetCharacterSpan();

										var h_selected_character = characters_span.GetAtIndexOrDefault(dormitory_selected_index ?? uint.MaxValue);
										ref var character_data = ref h_selected_character.GetData();

										var origins = FixedArray.CreateSpanList16<IOrigin.Handle>(out var buffer);
										IOrigin.Database.GetHandlesFiltered(ref origins, arg: (h_species: h_species_dormitory, h_faction: h_faction),
										predicate: static (in IOrigin.Definition d_origin, (ISpecies.Handle h_species, IFaction.Handle h_faction) arg) =>
										{
											//return d_origin.data.species == arg.h_species && (d_origin.data.faction == 0 || d_origin.data.faction == arg.h_faction);
											//return d_origin.data.faction == arg.h_faction;
											return d_origin.data.faction == arg.h_faction && (!arg.h_species || d_origin.data.species == arg.h_species);
										});

										//var ent_dormitory = ref_selected_dormitory.entity;

										//using (var group_title = GUI.Group.New(size: new(GUI.RmX, 32), padding: new(4)))
										//{
										//	//GUI.TitleCentered(ent_dormitory.GetFullName(), size: 24, pivot: new(0.00f, 0.00f), offset: new(6, 2));
										//}

										//Interactable.SetCurrentTarget(ent_dormitory);

										if (character_data.IsNotNull())
										{
											using (var group_kits = GUI.Group.New(size: GUI.Rm.SubY(48 + 4)))
											{
												GUI.DrawBackground(GUI.tex_panel, group_kits.GetOuterRect(), new(8, 8, 8, 8));

												using (var scrollbox = GUI.Scrollbox.New("kits"u8, size: GUI.Rm, padding: new(4, 4), force_scrollbar: true))
												{
													foreach (var h_kit in character_data.kits)
													{
														if (!h_kit) continue;

														using (var group_row = GUI.Group.New(size: new(GUI.RmX, 40)))
														{
															Dormitory.DrawKit(h_kit, group_row.GetOuterRect(), true, true, force_readonly: true, ignore_requirements: true);
														}
													}
												}
											}

											//if (GUI.DrawButton("Spawn", size: new(GUI.RmX, 40)))
											//{
											//	var rpc = new Siege.DEV_SpawnUnitRPC()
											//	{
											//		h_character = h_selected_character,
											//		ent_squad = ent_selected_squad
											//	};
											//	rpc.Send(ent_dormitory);
											//}
										}
										else
										{
											using (var group_origins = GUI.Group.New(size: GUI.Rm.SubY(48 + 4)))
											{
												GUI.DrawBackground(GUI.tex_panel, group_origins.GetOuterRect(), new(8));

												using (var scrollbox = GUI.Scrollbox.New("scroll.origins"u8, size: GUI.Rm, force_scrollbar: true))
												{
													foreach (var h_origin in origins)
													{
														ref var origin_data = ref h_origin.GetData();
														if (origin_data.IsNotNull())
														{
															using (GUI.ID.Push(h_origin))
															using (var group_row = GUI.Group.New(size: new(GUI.RmX, 40)))
															{
																group_row.DrawBackground(GUI.tex_panel);

																using (var group_icon = GUI.Group.New(size: new(GUI.RmY)))
																using (var clip = GUI.Clip.Push(group_icon.GetInnerRect()))
																{
																	GUI.DrawSpriteCentered(sprite: origin_data.icon,
																		rect: group_icon.GetInnerRect(),
																		layer: GUI.Layer.Window,
																		scale: 2);

																	group_icon.DrawBackground(GUI.tex_frame);
																}

																GUI.SameLine();

																//Dormitory.DormitoryGUI.DrawCharacterSmall(h_character);

																GUI.TitleCentered(origin_data.name, size: 16, pivot: new(0.00f, 0.00f), offset: new(6, 2));

																//var selected = ref_selected_dormitory == entity;
																//var selected = false;
																//if (GUI.Selectable3("select", group_row.GetOuterRect(), selected))
																//{
																//	//dormitory_selected_index = selected ? null : index;
																//	//ref_selected_dormitory = selected ? default : entity;
																//}

																//GUI.SameLine();

																using (var group_button = group_row.Split(size: new(64, GUI.RmY), align_x: GUI.AlignX.Right, align_y: GUI.AlignY.Center))
																{
																	if (GUI.DrawButton(origin_data.character_flags.HasAny(Character.Traits.Pest | Character.Traits.Slave | Character.Traits.Pet) ? "Buy"u8 : "Hire"u8, size: GUI.Rm, color: GUI.col_button_yellow))
																	{
																		var rpc = new Siege.DEV_BuyUnitRPC()
																		{
																			h_origin = h_origin
																		};
																		rpc.Send(ent_dormitory);
																	}

																	//if (GUI.DrawButton("Hire", size: GUI.Rm, color: GUI.col_button_yellow))
																	//{
																	//	var rpc = new Siege.DEV_BuyUnitRPC()
																	//	{
																	//		h_origin = h_origin
																	//	};
																	//	rpc.Send(ent_dormitory);
																	//}

																	if (GUI.IsHoveringRect(group_button.GetOuterRect()))
																	{
																		using (var tooltip = GUI.Tooltip.New())
																		{
																			GUI.TextShaded("Hire this unit."u8);
																		}
																	}
																}
															}
															GUI.FocusableAsset(h_origin);

															if (!GUI.IsAnyTooltipVisible() && GUI.IsItemHovered())
															{
																using (var tooltip = GUI.Tooltip.New())
																{
																	using (GUI.Wrap.Push(200))
																	{
																		GUI.TextShaded(origin_data.desc);
																	}
																}
															}
														}
													}
												}
											}
										}

										GUI.SeparatorThick();

										using (var group_bottom = GUI.Group.New(size: GUI.Rm))
										{

										}
									}
								}
							}
						}
					}
				}
			}
		}

		[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("local", true, Source.Modifier.Owned)]
		public static void OnGUIAttacker(Entity entity, [Source.Owned] in Player.Data player,
		[Source.Global] in Siege.Gamemode g_siege, [Source.Singleton] in Siege.Gamemode.Singleton g_siege_state)
		{
			if (player.faction_id == g_siege_state.h_faction_attackers)
			{
				Spawn.RespawnGUI.enabled = false;
				//Editor.show_respawn_menu = false; // TODO: workaround for testing

				var gui = new SiegeAttackerGUI()
				{
					g_siege = g_siege,
					g_siege_state = g_siege_state
				};
				gui.Submit();
			}
		}
#endif
	}
}
