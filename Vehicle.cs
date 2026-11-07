
using System;

namespace TC2.Base.Components
{
	// TODO: Needs Vehicle.cs exposed
	public static partial class VehicleExt
	{
#if SERVER
		[ISystem.Event<EssenceNode.FailureEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnFailure(ISystem.Info info, Entity entity, ref Region.Data region, Entity ent_attachment_slot, ref XorRandom random, ref EssenceNode.FailureEvent data, [Source.Owned] ref Body.Data body, [Source.Owned] ref Attachment.Data attachment, [Source.Parent] ref Attachment.Slot attachment_slot)
		{
			Sound.Play(ref region, "metal_gib_00", data.world_position, volume: 1.50f, pitch: random.NextFloatRange(0.70f, 1.20f));
			entity.RemoveRelation(ent_attachment_slot, Relation.Type.Child);

			body.AddForceLocal(random.NextUnitVector2Range(5.00f, 20.00f) * body.GetMass() * App.tickrate, random.NextUnitVector2Range(0.50f, 1.00f));
		}
#endif

#if CLIENT
		[ISystem.Event<Interactor.HoverEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnHover(ref Region.Data region, ISystem.Info info, Entity ent_transform, Entity ent_attachment, [Source.Shared] ref Interactor.HoverEvent ev,
		[Source.Shared] in Transform.Data transform, [Source.Owned] in Attachment.Slot attachment, [Source.Owned] in Joint.Base joint_base)
		{
			var mouse = Control.GetMouse();
			var kb = Control.GetKeyboard();

			if (!kb.GetKeyNow(Keyboard.Key.LeftControl)) return;

			ref var context = ref ev.context;

			//GUI.Title($"{info.Offset}; {info.FrameOffset}; {info.Count}; {info.TableCount}; {info.EventID}; {info.Event}");

			var wpos_slot = transform.LocalToWorldInterpolated(joint_base.offset_a);
			var wpos_mouse = mouse.GetInterpolatedPosition();

			var cpos_slot = region.WorldToCanvas(wpos_slot);
			var c_radius = attachment.radius * region.GetWorldToCanvasScale() * 0.750f;
			var ent_attached = joint_base.ent_attached;

			var color = GUI.font_color_orange;
			var color_text = GUI.font_color_default;

			var can_attach = false;
			var can_detach = false;

			var is_hovered = false;

			if (ev.ent_selected == 0 && Maths.IsInRadius(wpos_slot, wpos_mouse, attachment.radius * 0.750f))
			{
				is_hovered = true;
				ev.ent_selected = ent_attachment;
			}

			//ref var player = ref Client.GetPlayer();
			//if (player.IsNotNull())
			{
				var ent_held = context.ent_pickup_target;
				if (ent_held.IsAlive())
				{
					ref var attachable = ref context.GetAttachable();
					if (attachable.IsNotNull())
					{
						var offset = joint_base.offset_a - attachable.offset;
						var transform_tmp = transform;
						transform_tmp.SetPosition(transform.LocalToWorldInterpolated(offset));

						can_attach = attachment.tags_filter.Evaluate(attachable.tags);
						if (can_attach)
						{
							if (joint_base.ent_attached.IsAlive())
							{
								color = GUI.font_color_yellow_b;
								color_text = GUI.font_color_yellow_b;
							}
							else
							{
								color = GUI.font_color_success;
								color_text = GUI.font_color_success;
							}
						}
						else
						{
							color = GUI.font_color_error;
							color_text = GUI.font_color_error;
						}

						if (is_hovered)
						{
							GUI.DrawEntity(ent_held, in transform_tmp, color: color.WithAlpha(150));

							if (can_attach)
							{
								GUI.SetCursor(App.CursorType.Add, 2500);

								if (mouse.GetKeyDown(Mouse.Key.Left))
								{
									var rpc = new Attachment.AttachRPC
									{

									};
									rpc.Send(ent_attachment);
								}
							}
						}
					}
				}
				else if (ent_attached.IsAlive())
				{
					ref var holdable = ref ent_attached.GetComponent<Holdable.Data>();
					ref var attachable = ref ent_attached.GetComponent<Attachment.Data>();
					if (holdable.IsNotNull() && attachable.IsNotNull())
					{
						can_detach = true;
						if (can_detach)
						{
							color = GUI.font_color_orange;
							color_text = GUI.font_color_orange;
						}
						else
						{
							color = GUI.font_color_error;
							color_text = GUI.font_color_error;
						}

						if (is_hovered)
						{
							GUI.DrawEntity(ent_attached, color: color.WithAlpha(150));

							if (can_detach)
							{
								GUI.SetCursor(App.CursorType.Remove, 2500);

								if (mouse.GetKeyDown(Mouse.Key.Left))
								{
									var rpc = new Attachment.DetachRPC
									{
										offset = attachable.offset,
										pickup = 1,
									};
									rpc.Send(ent_attachment);
								}
							}
						}
					}
				}

				//GUI.Title($"{info.Offset}; {info.FrameOffset}; {info.Count}; {info.TableCount}; {info.EventID}; {info.Event}")
				//GUI.TextShaded(attachment.name, color: Color32BGRA.Magenta);
			}

			if (!is_hovered)
			{
				color = color.WithColorMult(0.75f);
				color_text = color.WithAlphaMult(0.50f);
			}

			GUI.DrawCircleFilled(cpos_slot, radius: c_radius * (is_hovered ? 0.250f : 0.200f), color: color, segments: 4);
			if (is_hovered) GUI.DrawCross(cpos_slot, radius: c_radius, color: color.WithAlpha(200), thickness: 2.00f);
			if (is_hovered)
			{
				GUI.DrawTextCentered(attachment.name, cpos_slot + new Vector2(0.00f, 16.00f), pivot: new Vector2(0.50f, 0.00f), font: GUI.Font.Superstar, size: 20, box_shadow: true, color: color_text);
				GUI.DrawTextCentered(ent_attached.GetName(), cpos_slot + new Vector2(0.00f, 32.00f), pivot: new Vector2(0.50f, 0.00f), font: GUI.Font.Monaco, size: 16, box_shadow: true, color: color_text);
			}
		}
#endif

#if CLIENT
		public struct VehicleGUI: IGUICommand
		{
			public Entity ent_vehicle;

			public Vehicle.Data vehicle;
			public Transform.Data transform;

			[Shitcode]
			public void Draw()
			{
				if (this.vehicle.flags.HasAny(Vehicle.Flags.Has_Attachments))
				{
					using (var window = GUI.Window.InteractionMisc("Hardpoints"u8, this.ent_vehicle, size: new(48, 48), show_misc: this.vehicle.flags.HasAny(Vehicle.Flags.Use_Misc_Window)))
					{
						this.StoreCurrentWindowTypeID(order: -150);

						//GUI.DrawEntity(this.ent_vehicle, in this.transform, color: Color32BGRA.Yellow.WithAlphaMult(0.50f), layer: GUI.Layer.Background);

						//using (GUI.Group.New(size: GUI.Rm))
						//{
						if (window.show)
						{
							//using (GUI.Group.New(size: GUI.Rm))
							{
								ref var player = ref Client.GetPlayer();
								ref var region = ref Client.GetRegion();

								var kb = Control.GetKeyboard();

								var children = FixedArray.CreateSpan16NoInit<Entity>(out var children_buffer);
								this.ent_vehicle.GetChildren(ref children, Relation.Type.Instance);
								var ent_vehicle_parent = this.ent_vehicle.GetParent(Relation.Type.Child).GetComponentOwner<Body.Data>(Relation.Type.Instance);

								var context = GUI.ItemContext.Begin();
								{
									var ent_held_item = context.ent_pickup_target;
									var is_held_item_alive = ent_held_item.IsAlive();

									var ent_player_vehicle = player.ent_controlled.GetComponentOwner<Vehicle.Data>(Relation.Type.Seat);
									//App.WriteLine(ent_player_vehicle);

									//if (ent_held_item.TryGetPrefab(out var held_prefab))
									//{

									//}

									var attachable_tags = default(Attachment.Tags);
									ref var attachable = ref context.GetAttachable();

									if (attachable.IsNotNull())
									{
										attachable_tags = attachable.tags;
									}

									//ref var wrench = ref ent_held_item.GetComponent<WrenchOld.Data>();
									//if (!wrench.IsNull())
									//{

									//}
									//Span<float> children_keys = stackalloc float[children.Length];
									var children_keys = FixedArray.CreateSpan16NoInit<float>(out var children_keys_buffer).WithLength(children.Length);

									var i = 0;
									foreach (var ent_attachment in children)
									{
										ref var attachment = ref ent_attachment.GetComponent<Attachment.Slot>();
										if (attachment.IsNotNull() && attachment.flags.HasNone(Attachment.Slot.Flags.Hidden))
										{
											children_keys[i] = (attachment.rank) + (i * 0.0001f);
										}
										i++;
									}

									children_keys.Sort(children);

									//GUI.DrawEntity(this.ent_vehicle, in this.transform, color: Color32BGRA.Yellow.WithAlphaMult(0.50f), layer: GUI.Layer.Background);

									var first = true;

									//var group_current = default(GUI.Group);

									using (var grid = GUI.Grid.New(size: GUI.Rm, new Vector2(8)))
									{
										foreach (var ent_attachment in children)
										{
											ref var attachment = ref ent_attachment.GetComponent<Attachment.Slot>();
											if (attachment.IsNotNull() && attachment.flags.HasNone(Attachment.Slot.Flags.Hidden))
											{
												using (GUI.ID.Push(ent_attachment))
												{
													var ent_attached = ent_attachment.GetChild(Relation.Type.Child);
													var is_attached_alive = ent_attached.IsAlive();
													var is_locked = attachment.flags.HasAny(Attachment.Slot.Flags.Locked);

													var error = default(string);
													var is_held_item_valid = is_held_item_alive;
													var is_occupied = false;

													var flags = attachment.flags;
													var tags_filter = attachment.tags_filter;

													if (is_held_item_alive)
													{
														if (attachable.IsNull())
														{
															error = "Item not attachable.";
															is_held_item_valid = false;
														}
														else
														{
															if (!tags_filter.Evaluate(attachable_tags))
															{
																error = "Item not compatible with hardpoint.";
																is_held_item_valid = false;
															}

															//if (!tags_filter.Evaluate(attachable_tags, out var missing, out var excluded))
															//{
															//	error = $"Item is not compatible with hardpoint.\n(Requires {missing.ToFormattedString(separator: "+")} {tags_filter.include.ToFormattedString(separator: "/")}, {excluded})";
															//	is_held_item_valid = false;
															//}
														}
													}

													if (ent_player_vehicle.IsValid() && ent_attached == ent_player_vehicle)
													{
														error = "Occupied by a passenger.";
														is_locked = true;
														is_occupied = true;
													}

													var cycle_interval = attachment.cycle_interval;
													var cycle_offset = attachment.cycle_offset;

													var dirty = false;

													var edit_rpc = new Attachment.EditRPC
													{
														flags = flags
													};

													//if (!first)
													//{
													//	if (GUI.TrySameLine(96))
													//	{
													//		//group_current = GUI.Group.New(size: new(GUI.RmX, 48));
													//	}

													//	//GUI.TrySameLine(96);

													//	//GUI.SameLine();

													//	//if (GUI.RmX < 96)
													//	//{
													//	//	GUI.NewLine();
													//	//}
													//}
													//else
													//{
													//	first = false;
													//}

													var group_size = attachment.gui_size;



													if (group_size.X <= 0.00f) group_size.X = GUI.RmX;

													//using (var group_slot = GUI.Group.New(size: group_size))
													using (var cell = grid.Push(group_size))
													{
														var slot_item = default(Shipment.Item);
														slot_item.type = Shipment.Item.Type.Prefab;
														slot_item.max = 1;

														var is_attaching = false;

														ref var joint_base = ref ent_attachment.GetComponent<Joint.Base>();
														if (joint_base.IsNotNull())
														{
															is_attaching = joint_base.state.HasAny(Joint.State.Attaching);
														}

														void DrawTooltip(in GUI.EntitySlot slot, in GUI.Tooltip tooltip)
														{
															using (GUI.Wrap.Push(GUI.RmX))
															{
																var tags = attachable_tags;
																if (!is_held_item_alive && is_attached_alive)
																{
																	ref var attachment = ref ent_attached.GetComponent<Attachment.Data>();
																	if (attachment.IsNotNull())
																	{
																		tags = attachment.tags;
																	}
																}

																var tags_combined = tags | tags_filter.require | tags_filter.include | tags_filter.exclude;

																//var ok = tags_filter.Evaluate(tooltip_attachable_tags, out var missing, out var excluded);

																using (var group = GUI.Group.New(new(GUI.RmX, 0)))
																{
																	foreach (var tag in tags_combined)
																	{
																		var color = GUI.font_color_default;

																		if (tags_filter.exclude.HasAny(tag))
																		{
																			color = GUI.font_color_red_b;
																		}
																		else if (tags_filter.require.HasAny(tag))
																		{
																			color = GUI.font_color_green_b;
																		}
																		else if (tags_filter.include.HasAny(tag))
																		{
																			color = GUI.font_color_yellow_b;
																		}

																		if (!tags.HasAny(tag)) color.a = 100;

																		GUI.Text(tag.ToStringUtf8(), color: color);

																		if (GUI.TrySameLine(32))
																		{
																			GUI.Text(", "u8, color: color);
																			GUI.SameLine();
																		}

																		//if (first)
																		//{
																		//	first = false;
																		//}
																		//else
																		//{
																		//	GUI.SameLine();
																		//}
																	}
																}

																//using (var group_columns = GUI.Group.New(new(GUI.RmX, 0)))
																//{
																//	var col_w = group_columns.size.X / 3.00f;

																//	using (var group_col = GUI.Group.New(new(col_w, 0), padding: new(4, 0)))
																//	{
																//		GUI.TextShadedCentered("All", pivot: new Vector2(0.50f, 0.00f));
																//		GUI.NewLine();
																//		GUI.Separator();

																//		//var tags_comb = tags_filter.require | Attachment.Tags.
																//		foreach (var tag in tags_filter.require)
																//		{
																//			GUI.Text(tag, color: exc)
																//		}
																//	}

																//	GUI.SameLine();

																//	using (var group_col = GUI.Group.New(new(col_w, 0), padding: new(4, 0)))
																//	{
																//		GUI.TextShadedCentered("Any", pivot: new Vector2(0.50f, 0.00f));
																//		GUI.NewLine();
																//		GUI.Separator();
																//	}

																//	GUI.SameLine();

																//	using (var group_col = GUI.Group.New(new(col_w, 0), padding: new(4, 0)))
																//	{
																//		GUI.TextShadedCentered("Not", pivot: new Vector2(0.50f, 0.00f));
																//		GUI.NewLine();
																//		GUI.Separator();
																//	}
																//}

																//$"Item is not compatible with hardpoint.\n(Requires {missing.ToFormattedString(separator: "+")} {tags_filter.include.ToFormattedString(separator: "/")}, {excluded})";
																//is_held_item_valid = false;

																//GUI.TextShaded(tags_filter.include.ToFormattedString());
															}

															GUI.NewLine(4);
														}

														using (var slot = GUI.EntitySlot.New(context: ref context, identifier: $"attachment_{ent_attachment}", name: attachment.name, ent_attached: ent_attached, size: GUI.Rm, enabled: !is_locked, error: error, color_frame: is_attaching ? GUI.col_button_yellow : null, on_hover: DrawTooltip))
														{
															var skip = false;
															if (slot.hovered)
															{
																if (is_attached_alive && !is_held_item_valid)
																{
																	GUI.SetCursor(App.CursorType.Hand, 1100);
																}

																if (App.debug_mode_gui && kb.GetKey(Keyboard.Key.LeftAlt) && GUI.GetMouse().GetKeyDown(Mouse.Key.Right))
																{
																	GUI.SetDebugEntity(ent_attachment);
																	skip = true;
																}

																if (!skip)
																{
																	if (joint_base.IsNotNull())
																	{
																		if (is_held_item_alive)
																		{
																			var offset = joint_base.offset_a;
																			if (attachable.IsNotNull())
																			{
																				offset -= attachable.offset;
																			}

																			var transform_tmp = this.transform;
																			transform_tmp.SetPosition(this.transform.LocalToWorldInterpolated(offset));

																			GUI.DrawEntity(ent_held_item, in transform_tmp, color: (error == null ? Color32BGRA.Green : Color32BGRA.Red).WithAlphaMult(0.80f), layer: GUI.Layer.Background);

																		}

																		var pos = region.WorldToCanvas(this.transform.LocalToWorldInterpolated(joint_base.offset_a));
																		var color_gizmo = Color32BGRA.Yellow;

																		//GUI.DrawCircleFilled(pos, 0.250f * region.GetWorldToCanvasScale(), color: Color32BGRA.Yellow.WithAlphaMult(0.50f), segments: 4);
																		GUI.DrawRect(AABB.Centered(pos, new Vector2(0.375f) * region.GetWorldToCanvasScale()), color: color_gizmo.WithAlphaMult(0.75f), thickness: 1.00f);
																		GUI.DrawCross(pos, radius: 1.375f * region.GetWorldToCanvasScale(), color: color_gizmo.WithAlphaMult(0.250f), thickness: 1.00f);
																		//GUI.DrawCircle(pos, 0.250f * region.GetWorldToCanvasScale(), color: Color32BGRA.Yellow.WithAlphaMult(0.50f), segments: 4, thickness: 1.00f);

																		ref var joint_gear = ref ent_attachment.GetComponent<Joint.Gear>();
																		if (joint_gear.IsNotNull())
																		{
																			if (joint_gear.min != joint_gear.max)
																			{
																				var arc = new Arc(joint_gear.min, joint_gear.max, joint_base.rotation_origin + joint_base.rotation_offset);
																				//arc.flags.SetFlag(Arc.Flags.Mirror_X, joint_base.flags.HasAny(Joint.Flags.Mirror_X));
																				//arc.flags.SetFlag(Arc.Flags.Mirror_Y, joint_base.flags.HasAny(Joint.Flags.Mirror_Y));

																				var arc_angles_tmp_local = arc.GetAngles();
																				var arc_angles_tmp_world = arc_angles_tmp_local;
																				arc_angles_tmp_world.X = transform.LocalToWorldRotation(arc_angles_tmp_world.X, scale: false, normalize: false);
																				arc_angles_tmp_world.Y = transform.LocalToWorldRotation(arc_angles_tmp_world.Y, scale: false, normalize: false);
																				var angle_world_aim = joint_gear.GetWorldRotation() + (joint_base.flags.HasAny(Joint.Flags.Mirror_X) ? MathF.PI : 0.00f);

																				var arc_radius = 4 * region.GetWorldToCanvasScale();

																				GUI.DrawLine(pos, pos + Maths.RadToDir(angle_world_aim, arc_radius * 4.00f), color: GUI.font_color_orange.WithAlpha(200), thickness: 1.00f);

																				GUI.DrawArc(pos, arc_angles_tmp_world.X, arc_angles_tmp_world.Y, radius: arc_radius, color: color_gizmo, thickness: 2.00f);
																				GUI.DrawLine(pos, pos + Maths.RadToDir(arc_angles_tmp_world.X, arc_radius * 1.25f), color: color_gizmo.WithAlpha(200), thickness: 1.00f);
																				GUI.DrawLine(pos, pos + Maths.RadToDir(arc_angles_tmp_world.Y, arc_radius * 1.25f), color: color_gizmo.WithAlpha(200), thickness: 1.00f);
																			}
																		}

																		GUI.DrawTextCentered(attachment.name, pos + new Vector2(0, -20), color: GUI.font_color_yellow, font: GUI.Font.Superstar, size: 16, pivot: new(0.50f, 0.50f));
																	}

																	{
																		ref var transform_tmp = ref ent_attached.GetComponent<Transform.Data>();
																		if (transform_tmp.IsNotNull())
																		{
																			//GUI.DrawEntity(ent_attached, in transform_tmp, color: Color32BGRA.Yellow.WithAlphaMult(0.40f), layer: GUI.Layer.Background);

																			var can_interact = Interactor.CanInteractWith(player.ent_interactor, ent_attached);
																			GUI.HighlightEntity(ent_attached, in transform_tmp, color: (can_interact ? Color32BGRA.Yellow : Color32BGRA.Red).WithAlphaMult(0.40f), layer: GUI.Layer.Background);
																		}
																	}

																	var is_interactable = false;

																	if (is_attached_alive)
																	{
																		//GUI.SetCursor(App.CursorType.Pickup, 1200);

																		if (!is_occupied && !is_locked && kb.GetKeyDown(Keyboard.Key.C))
																		{
																			var pickup_offset = default(Vector2?);

																			ref var attached_body = ref ent_attached.GetComponent<Body.Data>();
																			if (attached_body.IsNotNull())
																			{
																				ref var transform = ref player.ent_controlled.GetComponent<Transform.Data>();
																				if (transform.IsNotNull())
																				{
																					var overlap_result = attached_body.GetClosestPoint(transform.position, allow_inside: true, no_inside_if_solid: true);
																					pickup_offset = attached_body.WorldToLocal(overlap_result.world_position);
																				}
																			}

																			var rpc = new Attachment.DetachRPC
																			{
																				pickup = 1,
																				offset = pickup_offset
																			};
																			rpc.Send(ent_attachment);
																		}

																		if (ent_attached.HasComponent<Interactable.Data>())
																		{
																			GUI.SetCursor(App.CursorType.Interact, 5000);
																			is_interactable = true;

																			if (kb.GetKeyDown(Keyboard.Key.E) || (!slot.enabled && GUI.GetMouse().GetKeyDown(Mouse.Key.Left)))
																			{
																				var rpc = new Interactor.SetTargetRPC()
																				{
																					ent_target = player.ent_interactor_target == ent_attached ? default : ent_attached
																				};
																				rpc.Send(player.ent_interactor);
																			}
																		}
																	}

																	//GUI.TitleCentered($"LMB: {slot.action}\nRMB: Edit", rect: group_slot.GetInnerRect().Pad(8, 8, 8, 8), font: GUI.Font.Monaco, size: 14, color: GUI.font_color_title, pivot: new(0.00f, 0.00f), wrap: true);
																	if (attachment.flags_editable != Attachment.Slot.Flags.None)
																	{
																		GUI.TextShadedCenteredRect(
																			"LMB: Interact\n".If(is_interactable) +
																			"RMB: Edit\n" +
																			"C: Pickup",
																			rect: cell.group.GetInnerRect().Pad(10, 8, 8, 8), font: GUI.Font.Monaco, size: 14, color: GUI.font_color_default, pivot: new(0.00f, 0.00f), wrap: true, box_shadow: true);

																		if (GUI.GetMouse().GetKeyDown(Mouse.Key.Right))
																		{
																			GUI.OpenPopup("modal.attachment"u8);
																			Sound.PlayGUI(GUI.sound_window_open, volume: 0.30f, pitch: 1.20f);

																			//var rpc = new Interactor.SetTargetRPC()
																			//{
																			//	ent_target = player.ent_interactor_target == ent_vehicle_parent ? default : ent_vehicle_parent
																			//};
																			//rpc.Send(player.ent_interactor);
																		}
																	}
																}
															}

															if (slot.pressed && !skip)
															{
																switch (slot.action)
																{
																	case GUI.ItemContext.Action.Add:
																	{
																		var rpc = new Attachment.AttachRPC
																		{

																		};
																		rpc.Send(ent_attachment);
																	}
																	break;

																	case GUI.ItemContext.Action.Remove:
																	{
																		//if (App.debug_mode_gui && GUI.GetKeyboard().GetKey(Keyboard.Key.LeftAlt))
																		//{
																		//	GUI.SetDebugEntity(ent_attachment);
																		//}
																		//else
																		{
																			var rpc = new Interactor.SetTargetRPC()
																			{
																				ent_target = player.ent_interactor_target == ent_attached ? default : ent_attached
																			};
																			rpc.Send(player.ent_interactor);
																		}

																		//var rpc = new Attachment.DetachRPC
																		//{
																		//	pickup = 1
																		//};
																		//rpc.Send(ent_attachment);
																	}
																	break;

																	case GUI.ItemContext.Action.Swap:
																	{
																		var rpc = new Interactor.SetTargetRPC()
																		{
																			ent_target = ent_attached
																		};
																		rpc.Send(player.ent_interactor);

																		//var rpc = new Attachment.AttachRPC
																		//{

																		//};
																		//rpc.Send(ent_attachment);
																	}
																	break;
																}

																//if (ent_held_item.IsAlive())
																//{
																//	if (is_held_item_valid)
																//	{
																//		var rpc = new Attachment.AttachRPC
																//		{

																//		};
																//		rpc.Send(ent_attachment);
																//	}
																//	else
																//	{
																//		var rpc = new Attachment.DetachRPC
																//		{

																//		};
																//		rpc.Send(ent_attachment);
																//	}
																//	//}
																//}
																//else
																//{
																//	if (ent_attached.IsAlive())
																//	{
																//		var rpc = new Attachment.DetachRPC
																//		{

																//		};
																//		rpc.Send(ent_attachment);
																//	}
																//}
															}
														}

														//GUI.TextCentered("Test", new Vector2(0.00f, 1.00f));

														if (attachment.flags_editable != Attachment.Slot.Flags.None)
														{
															//if (GUI.DrawButton("Edit...", size: new Vector2(GUI.RmX, 40)))
															//{
															//	GUI.OpenPopup($"##Modal.{ent_attachment}");
															//}

															//GUI.DrawHoverTooltip($"Edit this attachment slot's properties.");

															//using (var modal = GUI.Popup.New("modal.attachment"u8, size: new(200, 300), padding: new(4, 4), pivot: new(0.50f, 0.00f), position: window.group.GetOuterRect().GetPosition(0.50f, 1.00f) + new Vector2(0, 4)))
															using (var modal = GUI.Popup.New("modal.attachment"u8, size: new(Maths.Max(cell.group.GetWidth(), 192), 192 + 32), padding: new(4, 4), pivot: new(0.00f, 0.00f), position: cell.group.GetOuterRect().GetPosition(0.00f, 1.00f)))
															{
																if (modal.show)
																{
																	GUI.DrawWindowBackground();

																	using (var group_top = GUI.Group.New(new(GUI.RmX, 40)))
																	{
																		if (attachment.flags_editable.HasAny(Attachment.Slot.Flags.Locked))
																		{
																			if (GUI.Checkbox("Lock"u8, ref flags, Attachment.Slot.Flags.Locked, new Vector2(GUI.RmY), show_text: false, icon: GUI.tex_icons_widget.GetSprite(16, 16, 0, 3)))
																			{
																				dirty = true;
																				edit_rpc.flags = flags;
																			}
																			GUI.DrawHoverTooltip("Disables attaching and detaching objects on this slot."u8);

																			GUI.SameLine();
																		}

																		if (attachment.flags_editable.HasAny(Attachment.Slot.Flags.Disabled))
																		{
																			if (GUI.Checkbox("Disable"u8, ref flags, Attachment.Slot.Flags.Disabled, new Vector2(GUI.RmY), show_text: false, icon: GUI.tex_icons_widget.GetSprite(16, 16, 0, 9), icon_color: GUI.col_button_error))
																			{
																				dirty = true;
																				edit_rpc.flags = flags;
																			}
																			GUI.DrawHoverTooltip("Disables all controls."u8);
																		
																			GUI.SameLine();
																		}

																		if (GUI.DrawButton("Detach"u8, new Vector2(GUI.RmX, GUI.RmY), color: GUI.col_button_error, enabled: is_attached_alive && attachment.flags.HasNone(Attachment.Slot.Flags.Locked)))
																		{
																			var rpc = new Attachment.DetachRPC
																			{
																				pickup = 0
																			};
																			rpc.Send(ent_attachment);
																		}
																	}

																	GUI.SeparatorThick();

																	using (var scrollbox = GUI.Scrollbox.New("scrollbox"u8, size: GUI.Rm))
																	{

																		//GUI.Text("hello");

																		//if (attachment.flags_editable.HasAny(Attachment.Slot.Flags.Disabled))
																		//{
																		//	if (GUI.Checkbox("Disable"u8, ref flags, Attachment.Slot.Flags.Disabled, new Vector2(GUI.RmX, 24)))
																		//	{
																		//		dirty = true;
																		//		edit_rpc.flags = flags;
																		//	}
																		//	GUI.DrawHoverTooltip("Disables all controls."u8);
																		//}

																		//if (attachment.flags_editable.HasAny(Attachment.Slot.Flags.Locked))
																		//{
																		//	if (GUI.Checkbox("Lock"u8, ref flags, Attachment.Slot.Flags.Locked, new Vector2(GUI.RmX, 24)))
																		//	{
																		//		dirty = true;
																		//		edit_rpc.flags = flags;
																		//	}
																		//	GUI.DrawHoverTooltip("Disables attaching and detaching objects on this slot."u8);
																		//}

																		if (attachment.flags_editable.HasAny(Attachment.Slot.Flags.Hold))
																		{
																			if (GUI.Checkbox("Hold to Aim"u8, ref flags, Attachment.Slot.Flags.Hold, new Vector2(GUI.RmX, 24)))
																			{
																				dirty = true;
																				edit_rpc.flags = flags;
																			}
																			GUI.DrawHoverTooltip("Require holding right mouse button to control."u8);
																		}

																		if (attachment.flags_editable.HasAny(Attachment.Slot.Flags.Invert))
																		{
																			if (GUI.Checkbox("Invert LMB/RMB"u8, ref flags, Attachment.Slot.Flags.Invert, new Vector2(GUI.RmX, 24)))
																			{
																				dirty = true;
																				edit_rpc.flags = flags;
																			}
																			GUI.DrawHoverTooltip("Invert left and right mouse buttons."u8);
																		}

																		if (attachment.flags_editable.HasAny(Attachment.Slot.Flags.Manual_Aim))
																		{
																			if (GUI.Checkbox("Manual Aiming"u8, ref flags, Attachment.Slot.Flags.Manual_Aim, new Vector2(GUI.RmX, 24)))
																			{
																				dirty = true;
																				edit_rpc.flags = flags;
																			}
																			GUI.DrawHoverTooltip("Disables mouse-based aiming. Useful for artillery."u8);
																		}

																		if (attachment.flags_editable.HasAny(Attachment.Slot.Flags.Loose))
																		{
																			if (GUI.Checkbox("Free Rotation"u8, ref flags, Attachment.Slot.Flags.Loose, new Vector2(GUI.RmX, 24), enabled: !attachment.flags.HasAny(Attachment.Slot.Flags.Brake)))
																			{
																				dirty = true;
																				edit_rpc.flags = flags;
																			}
																			GUI.DrawHoverTooltip("Allows the attached object to rotate freely."u8);
																		}

																		if (attachment.flags_editable.HasAny(Attachment.Slot.Flags.Brake))
																		{
																			if (GUI.Checkbox("Lock Rotation"u8, ref flags, Attachment.Slot.Flags.Brake, new Vector2(GUI.RmX, 24), enabled: !attachment.flags.HasAny(Attachment.Slot.Flags.Loose)))
																			{
																				dirty = true;
																				edit_rpc.flags = flags;
																			}
																			GUI.DrawHoverTooltip("Prevents rotation of the attached object."u8);
																		}

																		if (attachment.flags_editable.HasAny(Attachment.Slot.Flags.Use_Center_Of_Gravity))
																		{
																			if (GUI.Checkbox("Centered"u8, ref flags, Attachment.Slot.Flags.Use_Center_Of_Gravity, new Vector2(GUI.RmX, 24)))
																			{
																				dirty = true;
																				edit_rpc.flags = flags;
																			}
																			GUI.DrawHoverTooltip("Centers the attached object."u8);
																		}

																		if (attachment.flags_editable.HasAny(Attachment.Slot.Flags.Keyboard_Read))
																		{
																			if (GUI.Checkbox("Keyboard (Read)"u8, ref flags, Attachment.Slot.Flags.Keyboard_Read, new Vector2(GUI.RmX, 24), enabled: !attachment.flags.HasAny(Attachment.Slot.Flags.Keyboard_Write)))
																			{
																				dirty = true;
																				edit_rpc.flags = flags;
																			}
																			GUI.DrawHoverTooltip("Read keyboard from the attached object."u8);
																		}

																		if (attachment.flags_editable.HasAny(Attachment.Slot.Flags.Keyboard_Write))
																		{
																			if (GUI.Checkbox("Keyboard (Write)"u8, ref flags, Attachment.Slot.Flags.Keyboard_Write, new Vector2(GUI.RmX, 24), enabled: !attachment.flags.HasAny(Attachment.Slot.Flags.Keyboard_Read)))
																			{
																				dirty = true;
																				edit_rpc.flags = flags;
																			}
																			GUI.DrawHoverTooltip("Write keyboard to the attached object."u8);
																		}

																		if (attachment.flags_editable.HasAny(Attachment.Slot.Flags.Mouse_Read))
																		{
																			if (GUI.Checkbox("Mouse (Read)"u8, ref flags, Attachment.Slot.Flags.Mouse_Read, new Vector2(GUI.RmX, 24), enabled: !attachment.flags.HasAny(Attachment.Slot.Flags.Mouse_Write)))
																			{
																				dirty = true;
																				edit_rpc.flags = flags;
																			}
																			GUI.DrawHoverTooltip("Read mouse from the attached object."u8);
																		}

																		if (attachment.flags_editable.HasAny(Attachment.Slot.Flags.Mouse_Write))
																		{
																			if (GUI.Checkbox("Mouse (Write)"u8, ref flags, Attachment.Slot.Flags.Mouse_Write, new Vector2(GUI.RmX, 24), enabled: !attachment.flags.HasAny(Attachment.Slot.Flags.Mouse_Read)))
																			{
																				dirty = true;
																				edit_rpc.flags = flags;
																			}
																			GUI.DrawHoverTooltip("Write mouse to the attached object."u8);
																		}

																		if (attachment.flags_editable.HasAny(Attachment.Slot.Flags.Cycle) && GUI.SliderFloat("Cycle Interval"u8, ref cycle_interval, 0.00f, 1.00f))
																		{
																			dirty = true;
																			edit_rpc.cycle_interval = cycle_interval;
																		}

																		if (attachment.flags_editable.HasAny(Attachment.Slot.Flags.Cycle) && GUI.SliderFloat("Cycle Offset"u8, ref cycle_offset, 0.00f, 1.00f))
																		{
																			dirty = true;
																			edit_rpc.cycle_offset = cycle_offset;
																		}

																		if (attachment.flags_editable.HasAny(Attachment.Slot.Flags.Rotation))
																		{
																			if (GUI.SliderAngle("Rotation"u8, ref attachment.rotation, -(MathF.PI - 0.00001f), +(MathF.PI - 0.00001f), snap: 1.00f, size: new(GUI.RmX, 40)))
																			{
																				dirty = true;
																				edit_rpc.rotation = attachment.rotation;
																			}
																		}

																		if (attachment.flags_editable.HasAny(Attachment.Slot.Flags.Offset))
																		{
																			var offset_tmp = attachment.offset;

																			if (GUI.Picker("picker.attachment.offset"u8, "Offset"u8, new Vector2(48, 48), ref offset_tmp, attachment.offset_bounds.a, attachment.offset_bounds.b, sensitivity: 0.01f, scale: this.transform.scale))
																			{
																				dirty = true;
																				edit_rpc.offset_x = offset_tmp.X;
																				edit_rpc.offset_y = offset_tmp.Y;
																			}

																			GUI.SameLine();

																			using (GUI.Group.New(size: GUI.GetRemainingLineSpace()))
																			{
																				GUI.TitleCentered("Offset"u8, pivot: new(0.00f, 0.00f), offset: new(4, 6));
																				GUI.TitleCentered($"[{attachment.offset.X:0.000}, {attachment.offset.Y:0.000}]", pivot: new(0.00f, 1.00f), offset: new(4, -6));
																			}

																			//ref var joint_base = ref ent_attachment.GetComponent<Joint.Base>();
																			if (joint_base.IsNotNull())
																			{
																				var pos_current = this.transform.LocalToWorldInterpolated(joint_base.offset_a + attachment.offset);
																				var pos_u = this.transform.LocalToWorldInterpolated(joint_base.offset_a + new Vector2(0.00f, attachment.offset_bounds.a.Y));
																				var pos_d = this.transform.LocalToWorldInterpolated(joint_base.offset_a + new Vector2(0.00f, attachment.offset_bounds.b.Y));

																				//var transform_gizmo = this.transform;
																				//if (is_attached_alive)
																				//{
																				//	ref var transform_attached = ref ent_attached.GetComponent<Transform.Data>();
																				//	if (transform_attached.IsNotNull())
																				//	{
																				//		transform_gizmo = transform_attached;

																				//		pos_current = transform_gizmo.LocalToWorldInterpolated(joint_base.offset_b);
																				//		//pos_u = transform_gizmo.LocalToWorldInterpolated(joint_base.offset_b + new Vector2(0.00f, attachment.offset_bounds.a.Y));
																				//		//pos_d = transform_gizmo.LocalToWorldInterpolated(joint_base.offset_b + new Vector2(0.00f, attachment.offset_bounds.b.Y));


																				//		//transform_gizmo.SetRotation(transform_attached.GetRotationOld(), transform_attached.GetRotationNew());
																				//	}
																				//}


																				GUI.DrawLine(region.WorldToCanvas(pos_u), region.WorldToCanvas(pos_d), Color32BGRA.Orange.WithAlphaMult(0.75f), 2.00f);

																				if (MathF.Abs(attachment.offset_bounds.a.X - attachment.offset_bounds.b.X) > Maths.epsilon)
																				{
																					var pos_l = this.transform.LocalToWorldInterpolated(joint_base.offset_a + new Vector2(attachment.offset_bounds.a.X, attachment.offset.Y));
																					var pos_r = this.transform.LocalToWorldInterpolated(joint_base.offset_a + new Vector2(attachment.offset_bounds.b.X, attachment.offset.Y));
																					GUI.DrawLine(region.WorldToCanvas(pos_l), region.WorldToCanvas(pos_r), Color32BGRA.Orange.WithAlphaMult(0.75f), 2.00f);
																				}

																				GUI.DrawCircleFilled(region.WorldToCanvas(pos_current), 6.00f, Color32BGRA.Yellow, 4);


																				//var offset_base = Vector2.Zero;
																				//var pos_current = transform.LocalToWorldInterpolated(joint_base.offset_a + attachment.offset);

																				//var transform_gizmo = this.transform;
																				//if (is_attached_alive)
																				//{
																				//	ref var transform_attached = ref ent_attached.GetComponent<Transform.Data>();
																				//	if (transform_attached.IsNotNull())
																				//	{
																				//		offset_base = joint_base.offset_b + attachment.offset;
																				//		transform_gizmo = transform_attached;
																				//		pos_current = transform_gizmo.LocalToWorldInterpolated(joint_base.offset_b);

																				//		//transform_gizmo.SetRotation(transform_attached.GetRotationOld(), transform_attached.GetRotationNew());
																				//	}
																				//}


																				////transform_gizmo.SetRotation(transform.GetRotationOld(), transform.GetRotationNew());


																				//var pos_u = transform_gizmo.LocalToWorldInterpolated(offset_base + new Vector2(0.00f, attachment.offset_bounds.a.Y));
																				//var pos_d = transform_gizmo.LocalToWorldInterpolated(offset_base + new Vector2(0.00f, attachment.offset_bounds.b.Y));
																				//GUI.DrawLine(pos_u.WorldToCanvas(), pos_d.WorldToCanvas(), Color32BGRA.Orange.WithAlphaMult(0.75f), 2.00f);

																				////GUI.DrawTextCentered($"{offset_base}", pos_current.WorldToCanvas());
																				////GUI.DrawTextCentered($"{offset_base}", pos_current.WorldToCanvas());

																				//if (MathF.Abs(attachment.offset_bounds.a.X - attachment.offset_bounds.b.X) > Maths.epsilon)
																				//{
																				//	var pos_l = transform_gizmo.LocalToWorldInterpolated(offset_base + new Vector2(attachment.offset_bounds.a.X, attachment.offset.Y));
																				//	var pos_r = transform_gizmo.LocalToWorldInterpolated(offset_base + new Vector2(attachment.offset_bounds.b.X, attachment.offset.Y));
																				//	GUI.DrawLine(pos_l.WorldToCanvas(), pos_r.WorldToCanvas(), Color32BGRA.Orange.WithAlphaMult(0.75f), 2.00f);
																				//}

																				//GUI.DrawCircle(transform.LocalToWorldInterpolated(joint_base.offset_a).WorldToCanvas(), 12.00f, Color32BGRA.Orange, 1.00f, 4);
																				//GUI.DrawCircleFilled(pos_current.WorldToCanvas(), 6.00f, Color32BGRA.Yellow, 4);




																				//GUI.DrawCircle(GUI.WorldToCanvas(this.transform.LocalToWorldInterpolated(joint_base.offset_a)), 0.50f * GUI.GetWorldToCanvasScale(), 0xffffffff);
																			}

																			//GUI.DrawLine(attachment.)
																		}

																		//GUI.FillRemainingSize(new Vector2(GUI.RmX, 40));

																		//if (GUI.DrawButton("Detach"u8, new Vector2(GUI.RmX, 40), color: GUI.col_button_error, enabled: is_attached_alive))
																		//{
																		//	var rpc = new Attachment.DetachRPC
																		//	{
																		//		pickup = 0
																		//	};
																		//	rpc.Send(ent_attachment);
																		//}

																		//else
																		//{
																		//	if (GUI.DrawButton("Attach", new Vector2(GUI.RmX, 40), color: GUI.col_button_ok))
																		//	{
																		//		var rpc = new Attachment.AttachRPC
																		//		{

																		//		};
																		//		rpc.Send(ent_attachment);
																		//	}
																		//}
																	}
																}
															}
														}

														//if (attachment.flags_editable.HasAny(Attachment.Slot.Flags.Disabled))
														//{
														//	if (GUI.Checkbox("Disable", ref flags, Attachment.Slot.Flags.Disabled, new Vector2(96, 24)))
														//	{
														//		dirty = true;
														//		edit_rpc.flags = flags;
														//	}
														//}
													}

													if (dirty)
													{
														edit_rpc.Send(ent_attachment);
													}
												}
											}
										}
									}
								}
							}
						}

						//if (this.vehicle.flags.HasAny(Vehicle.Flags.Include_Misc_Window))
						//{
						//	GUI.SameLine();

						//	using (GUI.Group.New(GUI.Rm))
						//	{
						//		using (var dock = GUI.Dock.New("Misc"))
						//		{
						//			dock.SetSpace(size: GUI.Rm);
						//		}

						//		//GUI.Text("hi");
						//		//GUI.DrawInventoryDock(this.vehicle.inventory_type, GUI.Rm);
						//	}
						//}
						//}
					}
				}
			}
		}

		[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region, order: -10)]
		public static void OnGUI(/*ISystem.Info info, */Entity entity, [Source.Owned] in Interactable.Data interactable,
		[Source.Owned] in Transform.Data transform, 
		[Source.Owned] in Vehicle.Data vehicle)
		{
			if (interactable.IsActive() && vehicle.flags.HasAny(Vehicle.Flags.Has_Attachments))
			{
				var gui = new VehicleGUI()
				{
					ent_vehicle = entity,

					vehicle = vehicle,
					transform = transform
				};
				gui.Submit();
			}
		}
#endif
	}
}
