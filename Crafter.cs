#define NEW_WORK_ORDER

namespace TC2.Base.Components
{
	public static partial class CrafterExt
	{
		//#if SERVER
		//		[ChatCommand.Region("construction", "", creative: true)]
		//		public static void ConstructionCommand(ref ChatCommand.Context context, string identifier, float amount_multiplier = 1.00f)
		//		{
		//			ref var region = ref context.GetRegion();
		//			if (!region.IsNull())
		//			{
		//				var h_recipe = new IRecipe.Handle(identifier);
		//				ref var recipe = ref h_recipe.GetData();
		//				if (!recipe.IsNull())
		//				{
		//					var construction = recipe.construction.Value;

		//					var dismantlable_tmp = new Dismantlable.Data();
		//					dismantlable_tmp.yield = 0.90f;
		//					dismantlable_tmp.required_work = 5.00f;
		//					var dismantlable_tmp_items = dismantlable_tmp.items.AsSpan();

		//					var requirements_construction = construction.requirements;
		//					for (var i = 0; i < requirements_construction.Length; i++)
		//					{
		//						ref var requirement = ref requirements_construction[i];
		//						switch (requirement.type)
		//						{
		//							case Crafting.Requirement.Type.Resource:
		//							{
		//								dismantlable_tmp_items.Add(Shipment.Item.Resource(requirement.material, requirement.amount));
		//							}
		//							break;
		//						}
		//					}

		//					var order = new Crafting.Order();
		//					order.amount_multiplier = amount_multiplier;
		//					order.flags |= Crafting.Order.Flags.InProgress;
		//					order.recipe = h_recipe;

		//					var shipment = new Shipment.Data();
		//					shipment.flags |= Shipment.Flags.Keep_Items | Shipment.Flags.No_GUI;

		//					var work_count = 0;
		//					var item_count = 0;

		//					var requirements = recipe.requirements;
		//					for (var i = 0; i < requirements.Length; i++)
		//					{
		//						ref var requirement = ref requirements[i];
		//						switch (requirement.type)
		//						{
		//							case Crafting.Requirement.Type.Work:
		//							{
		//								order.work[work_count++] = new(requirement.work, Work.Amount.Flags.Pending, (byte)requirement.difficulty, requirement.amount * Constants.Construction.work_requirement_multiplier * amount_multiplier, 0.00f);
		//							}
		//							break;

		//							case Crafting.Requirement.Type.Resource:
		//							{
		//								shipment.items[item_count++] = Shipment.Item.Resource(requirement.material, 0.00f, requirement.amount * Constants.Construction.resource_requirement_multiplier * amount_multiplier);
		//							}
		//							break;
		//						}
		//					}

		//					region.SpawnPrefab(construction.prefab, context.GetPlayerData().control.mouse.position).ContinueWith((ent) =>
		//					{
		//						ref var dismantlable = ref ent.GetOrAddComponent<Dismantlable.Data>(sync: true, ignore_mask: true);
		//						if (!dismantlable.IsNull())
		//						{
		//							dismantlable = dismantlable_tmp;
		//						}

		//						ref var construction = ref ent.GetComponent<Construction.Data>();
		//						if (!construction.IsNull())
		//						{
		//							//construction.prefab = prefab_handle;
		//							construction.order = order;
		//							construction.stage = Construction.Stage.Materials;
		//						}

		//						ref var shipment_new = ref ent.GetOrAddComponent<Shipment.Data>(sync: true, ignore_mask: true);
		//						if (!shipment_new.IsNull())
		//						{
		//							shipment_new = shipment;
		//						}
		//					});
		//				}
		//			}
		//		}
		//#endif

#if SERVER
		[ISystem.Event<Crafting.ProduceEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnConstructionProduce(ISystem.Info info, Entity ent_construction, ref Region.Data region, ref XorRandom random, ref Crafting.ProduceEvent ev,
		[Source.Owned] in Transform.Data transform,
		[Source.Owned] ref Construction.Data construction)
		{
			//App.WriteValue(ent_construction);

			ref var recipe_data = ref ev.h_recipe.GetData();
			if (recipe_data.IsNotNull())
			{
				Sound.Play(region: ref region, sound: recipe_data.h_sound_finalize, world_position: transform.position);
			}

			region.Schedule((ref Region.Data region) => ent_construction.Delete());
		}
#endif

#if SERVER
		[ISystem.Event<Crafter.WorkEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnWorkEvent(ISystem.Info.Common info, ref Region.Data.Common region_common, Entity ent_crafter, ref XorRandom random, [Source.Owned] ref Crafter.WorkEvent ev,
		[Source.Owned] ref Crafter.Data crafter, [Source.Owned] ref Crafter.State crafter_state,
		[Source.Owned] in Transform.Data transform)
		{
			//var ts = Timestamp.Now();
			//App.WriteLine("work event");

			//if (crafter.h_crafter && crafter_state.flags.HasAny(Crafter.State.Flags.Ready) && info.WorldTime >= crafter_state.t_next_update)
			if (crafter.h_crafter && info.WorldTime >= crafter_state.t_next_update)
			{
				var cooldown = 3.00f;
				var sync = false;

				ref var result = ref ev.work_result; // Worker.WorkResult.Idle;
													 //ev.work_result = result;
				result = Worker.WorkResult.Idle;

				ref var order = ref crafter.ref_order_selected.GetComponent();
				if (order.IsNotNull())
				{
					//ref var random = ref info.GetRandom();
					if (ent_crafter == order.ent_crafter_selected)
					{
						//WorldNotification.Push(ref region_common.AsRegion(), "tick", Color32BGRA.Yellow, transform.position);

						var work_mult = 1.00f;
						ref var mode = ref Unsafe.NullRef<ICrafter.ModeInfo>();

						if (true) //crafter_state.current_mode.HasValue) // && !crafter_state.current_work_index.HasValue)
						{
							ref var crafter_data = ref crafter.h_crafter.GetData();
							if (crafter_data.IsNotNull())
							{
								mode = ref crafter_data.modes.GetRefAtIndexOrNull(crafter_state.current_mode);
								if (mode.IsNotNull())
								{
									//if (!crafter_state.current_work_index.HasValue)
									{
										byte? work_index_tmp = null;

										for (var i = 0u; i < order.work_handles.ULength; i++)
										{
											var h_work = order.work_handles[i];
											if (!h_work) continue;
											if (order.work[i].flags.HasNone(Work.Amount.Flags.Pending)) continue;

											if (mode.ContainsWork(h_work))
											{
												work_mult = mode.work_multiplier;
												work_index_tmp = (byte)i;

												//ev.h_work = h_work;
												break;
											}
										}
										sync |= crafter_state.current_work_index.TrySetValue(work_index_tmp);
									}
								}
							}

							//App.WriteLine(ent_crafter.GetFullName());

							var pos = transform.LocalToWorld(crafter.offset);

							if (mode.IsNotNull())
							{
								result = Worker.PerformWorkCrafter(region_common: ref region_common, random: ref random, ev: ref ev,
									crafter_data: ref crafter_data, mode: ref mode,
									ent_crafter: ent_crafter, crafter: ref crafter, crafter_state: ref crafter_state, order: ref order,
									world_position: pos, cooldown: out cooldown/*, work_multiplier: work_mult*/);

								cooldown *= mode.cooldown_multiplier;
							}

							const bool debug_print_result = false;
							if (debug_print_result)
							{
								WorldNotification.Push(ref region_common.AsRegion(), result.ToStringUtf8(), Color32BGRA.Yellow, pos);
							}

							//App.WriteValue(result);

							switch (result)
							{
								case Worker.WorkResult.Progress:
								{
									order.flags.AddFlag(Crafting.Order.Flags.Dirty);
									order.cache_flags.RemoveFlag(Crafting.Order.CacheFlags.Work_Progress);

									if (mode.IsNotNull())
									{
										order.Sync(crafter.ref_order_selected.entity, crafter.ref_order_selected.ComponentID);
										crafter_state.flags.TryAddRemFlags(mode.crafter_state_on_work);
									}

									sync = true;
								}
								break;

								case Worker.WorkResult.Idle:
								case Worker.WorkResult.Waiting:
								{

								}
								break;

								case Worker.WorkResult.Undefined:
								case Worker.WorkResult.Failed:
								case Worker.WorkResult.Skipped:
								case Worker.WorkResult.Invalid:
								case Worker.WorkResult.Error:
								case Worker.WorkResult.Outdated:
								{

								}
								break;

								case Worker.WorkResult.Done:
								{
									order.flags.AddFlag(Crafting.Order.Flags.Dirty);
									order.cache_flags.RemoveFlag(Crafting.Order.CacheFlags.Work_Done | Crafting.Order.CacheFlags.Work_Progress);

									crafter_state.current_work_index = null;
									sync = true;
								}
								break;
							}

							crafter_state.last_work_result = result;
						}
						else
						{
							crafter.ref_order_selected.Reset();
							sync = true;
						}
					}
				}
				else
				{
					if (crafter_state.current_work_index.HasValue)
					{
						crafter_state.current_work_index = null;
						sync = true;
						//crafter_state.Sync(ent_crafter, true);
					}
				}

				crafter_state.t_next_update = info.WorldTime + cooldown;

				if (sync)
				{
					crafter.Sync(ent_crafter, true);
					crafter_state.Sync(ent_crafter, true);
				}
			}

			//var ts_elapsed = ts.GetMilliseconds();
			//App.WriteLine($"{ts_elapsed:0.00000} ms");
		}
#endif


#if CLIENT
		//public static void DrawRecipe(ref Crafting.Context context, ref Crafter.Data crafter, ref Crafter.State crafter_state, bool draw_title = true)
		//{
		//	ref var recipe = ref crafter.GetCurrentRecipe();
		//	if (!recipe.IsNull())
		//	{
		//		//Crafting.Context.New(ref region, )

		//		CrafterExt.DrawRecipe(context: ref context, recipe: ref crafter.GetCurrentRecipe(), current_index: crafter_state.current_index, progress: crafter_state.progress.AsSpan(), amount_multiplier: crafter.amount_multiplier, draw_title: draw_title);

		//		//CrafterExt.DrawRecipe(ref recipe, current_index: crafter_state.current_index, progress: crafter_state.progress.AsSpan(), amount_multiplier: crafter.amount_multiplier);
		//	}
		//}

		public static void DrawRecipe(ref Crafting.Context context, ref IRecipe.Data recipe, int current_index, scoped Span<Crafting.Progress> progress, float amount_multiplier = 1.00f, bool draw_title = true)
		{
			CrafterExt.DrawRecipeRequirements(context: ref context, recipe: ref recipe, current_index: current_index, progress: progress, amount_multiplier: amount_multiplier, draw_title: draw_title);

			GUI.NewLine();

			CrafterExt.DrawRecipeProducts(context: ref context, recipe: ref recipe, amount_multiplier: amount_multiplier, draw_title: draw_title);
		}

		public static void DrawRecipeRequirements(ref Crafting.Context context, ref IRecipe.Data recipe, int current_index, scoped Span<Crafting.Progress> progress, float amount_multiplier = 1.00f, bool draw_title = true)
		{
			//var draw_title = false;

			if (draw_title)
			{
				GUI.Title("Requires"u8, size: 24);
				GUI.SeparatorThick();
			}

			if (recipe.IsNotNull())
			{
				var requirements_span = recipe.requirements.AsSpan();
				for (var i = 0; i < requirements_span.Length; i++)
				{
					ref var requirement = ref requirements_span[i];
					var current_progress = 0.00f;
					if (current_index < progress.Length) current_progress = progress[i].value;

					if (requirement.type != Crafting.Requirement.Type.Undefined)
					{
						using (GUI.ID.Push(100 + i))
						{
							using (var group_req = GUI.Group.New2(size: new(GUI.RmX, 40)))
							{
								var color_bg = GUI.col_black; //.WithAlphaMult(0.30f);
								if (i == current_index) color_bg = Color32BGRA.Lerp(color_bg, new Color32BGRA(0xff808080), 0.50f + ((MathF.Sin(App.GetFixedTime() * 2.00f) + 1.00f) * 0.50f * 0.50f)); ///.WithAlphaMult(0.30f);
								//color_bg = color_bg.WithAlphaMult(0.30f);

								//GUI.DrawBackground(GUI.tex_panel, group_req.GetOuterRect(), new(4));

								switch (requirement.type)
								{
									case Crafting.Requirement.Type.Resource:
									{
										//using (var group_inner = GUI.Group.New2(size: new(GUI.RmX - 40, GUI.RmY), padding: new Vector4(8, 2, 4, 2)))
										using (var group_inner = GUI.Group.New2(size: GUI.Rm, padding: new Vector4(8, 2, 4, 2)))
										{
											//GUI.DrawBackground(GUI.tex_panel_white, group_inner.GetOuterRect(), new(4), color: color_bg);
											GUI.DrawResourceRequirement(requirement.material, current_progress * requirement.amount * amount_multiplier, requirement.amount * amount_multiplier, bg_color: color_bg);
										}
										//GUI.SameLine();
										//if (DrawDepositButton(ref context, requirement.material, current_progress * requirement.amount * amount_multiplier, requirement.amount * amount_multiplier))
										//{
										//	var rpc = new Construction.DepositRPC()
										//	{
										//		index = i
										//	};
										//	rpc.Send(context.ent_this);
										//}
										//if (GUI.DrawButton("Deposit", size: GUI.Rm))
										//{

										//}
									}
									break;

									case Crafting.Requirement.Type.Work:
									{
										//using (var group_inner = GUI.Group.New2(size: new(GUI.RmX - 80, GUI.RmY), padding: new Vector4(8, 2, 4, 2)))
										using (var group_inner = GUI.Group.New2(size: GUI.Rm, padding: new Vector4(8, 2, 4, 2)))
										{
											//GUI.DrawBackground(GUI.tex_panel_white, group_inner.GetOuterRect(), new(4), color: color_bg);
											GUI.DrawWorkRequirement(requirement.work, current_progress * requirement.amount * amount_multiplier, requirement.amount * amount_multiplier, requirement.difficulty, bg_color: color_bg);
										}
										//GUI.SameLine();
										//if (GUI.DrawButton("Work", size: GUI.Rm))
										//{

										//}
										//{
										//	var ok = false;
										//	var button_color = !ok ? GUI.col_button_yellow : GUI.font_color_default_dark;

										//	if (GUI.DrawSpriteButton("work", "ui_button_work", new(40, 40), enabled: !ok, color: button_color))
										//	{

										//	}
										//}
									}
									break;

									case Crafting.Requirement.Type.Temperature:
									{
										using (var group_inner = GUI.Group.New2(size: GUI.Rm, padding: new Vector4(8, 2, 4, 2)))
										{
											//GUI.DrawBackground(GUI.tex_panel_white, group_inner.GetOuterRect(), new(4), color: color_bg);
											GUI.DrawTemperatureRequirement(current_progress * requirement.amount, requirement.amount, bg_color: color_bg);
										}
									}
									break;
								}
							}
						}
					}
				}
			}
		}

		public static void DrawRecipeProducts(ref Crafting.Context context, ref IRecipe.Data recipe, float amount_multiplier = 1.00f, bool draw_title = true)
		{
			if (draw_title)
			{
				GUI.Title("Produces"u8, size: 24);
				GUI.SeparatorThick();
			}

			if (recipe.IsNotNull())
			{
				var products_span = recipe.products.AsSpan();
				for (var i = 0; i < products_span.Length; i++)
				{
					ref var product = ref products_span[i];
					//ref var progress_tmp = ref progress[i];

					if (product.type != Crafting.Product.Type.Undefined)
					{
						using (var group_prd = GUI.Group.New2(size: new(GUI.RmX, 40), padding: new Vector4(8, 2, 4, 2)))
						{
							var color_bg = GUI.col_black.WithAlphaMult(0.30f);

							GUI.DrawBackground(GUI.tex_panel_white, group_prd.GetOuterRect(), new(4), color: color_bg);
							GUI.DrawProduct(ref product, amount_multiplier: amount_multiplier);
						}
					}
				}
			}
		}

#endif

#if CLIENT
		public static bool DrawDepositButton(ref Crafting.Context context, IMaterial.Handle h_material, float quantity, float quantity_max)
		{
			var pressed = false;

			ref var material = ref h_material.GetData();
			if (material.IsNotNull())
			{
				var ok = quantity >= quantity_max;
				var button_color = !ok ? GUI.col_button_yellow : GUI.font_color_default_dark;


				//if (inventory_ptr != null)
				//{
				//	var dirty = Resource.WithdrawInternal(Inventory.GetResourcesPtr(inventory_ptr), inventory_ptr->size, ref resource, ref amount);
				//	if (dirty) inventory_ptr->flags |= Inventory.Flags.Dirty;
				//}

				var amount_available = 0.00f;
				if (!ok)
				{
					if (Constants.Requirements.disable_requirements)
					{
						amount_available = quantity_max;
					}
					else if (context.world_position.HasValue)
					{
						amount_available = context.GetMaterialCount(h_material);

						//amount_available += context.inventory.GetQuantity(h_material);
						//amount_available += Resource.GetCountInRadius(ref context.region, h_material, context.world_position.Value, context.search_radius, required_flags: Inventory.Flags.Public);
					}
				}

				var text = string.Empty;
				var text_color = GUI.font_color_default;

				if (ok)
				{
					//text = $"{(item.quantity).Simplify():0.##}/{quantity_max.Simplify():0.##}";
					//text = $"{(item.quantity).Simplify():0.##}";
					text = $"{(quantity):0.##}/{quantity_max:0.##}";
					text_color = GUI.font_color_green;
				}
				else
				{
					text = $"{(quantity):0.##}/{quantity_max:0.##}";
					//text = $"{(item.quantity).Simplify():0.##}+{amount_available.Simplify():0.##}/{quantity_max.Simplify():0.##}";
					//if (amount_available > 0.00f) text += $" ({amount_available.Simplify():0.##} available)";
					//if (amount_available <= 0.00f)
					//{
					//	text_color = GUI.font_color_red;
					//}
					//else if ((item.quantity + amount_available) < quantity_max)
					//{
					//	text_color = GUI.font_color_yellow;
					//}
					//else
					//{
					//	text_color = GUI.font_color_green;
					//}
					text_color = GUI.font_color_red;
				}

				//GUI.DrawMaterialSmall(h_material, new Vector2(GUI.RmY));

				//GUI.SameLine();

				//using (GUI.Group.New(size: GUI.Rm - new Vector2(40, 0), padding: new(4, 4)))
				//{
				//	GUI.Title(material.name, color: GUI.font_color_default);
				//	GUI.TextShaded(text, color: text_color);

				//	//if (amount_available > 0.00f)
				//	//{
				//	//	GUI.SameLine();
				//	//	GUI.TextShadedCentered($"+{amount_available.Simplify():0.##}", pivot: new Vector2(1, 0.00f), color: GUI.font_color_yellow);
				//	//}
				//}

				//GUI.SameLine();

				using (GUI.Group.New(size: GUI.Rm))
				{
					if (GUI.DrawSpriteButton("deposit"u8, "ui_button_deposit", new(40, 40), enabled: !ok && amount_available > Maths.epsilon, color: button_color))
					{
						pressed = true;
					}


					//if (GUI.DrawIconButton(new Sprite("missing"), new(40, 40), enabled: !ok, color: button_color))
					//{
					//	var rpc = new Construction.DepositRPC()
					//	{
					//		index = i
					//	};
					//	rpc.Send(this.ent_construction);
					//}

					if (GUI.IsItemHovered())
					{
						using (GUI.Tooltip.New())
						{
							using (GUI.Wrap.Push(200))
							{
								GUI.Text("Fetch resources from nearby inventories."u8);

								if (!ok)
								{
									GUI.NewLine(4);
									GUI.Separator();
									GUI.NewLine(4);

									var text_color_b = GUI.font_color_red;

									if (amount_available <= 0.00f)
									{
										text_color_b = GUI.font_color_red;
									}
									else if ((quantity + amount_available) < quantity_max)
									{
										text_color_b = GUI.font_color_yellow;
									}
									else
									{
										text_color_b = GUI.font_color_green;
									}

									if (Constants.Requirements.disable_requirements)
									{
										GUI.LabelShaded("Available:"u8, "Unlimited"u8, color_b: text_color_b);
									}
									else
									{
										GUI.LabelShaded("Available:"u8, $"{amount_available:0.##}", color_b: text_color_b);
									}
								}
							}
						}
					}
				}

				//if (button.pressed)
				//{
				//	var rpc = new Shipment.DepositRPC()
				//	{
				//		amount = 20.00f,
				//		index = (uint)i
				//	};
				//	rpc.Send(this.ent_construction);
				//}
			}

			return pressed;
		}

		public partial struct ConstructionGUI: IGUICommand
		{
			public Entity ent_construction;

			public IComponent.Handle<Crafting.Order> h_order;
			public Crafting.Order order;

			public Construction.Data construction;

			public Shipment.Data shipment;
			public Transform.Data transform;
			public Faction.Data faction;

			public void Draw()
			{
				using (var window = GUI.Window.Interaction(identifier: "Construction Site"u8, entity: this.ent_construction))
				{
					this.StoreCurrentWindowTypeID(-100);
					if (window.show)
					{
						//using (GUI.Group.New(GUI.Rm))
						{
							//ref var region = ref this.ent_construction.GetRegion();
							//ref var player = ref Client.GetPlayer();
							//ref var character_data = ref Client.GetCharacterHandle().GetData(out var character_asset);
							//ref var experience = ref player.GetExperience().data;
							//var ref_worker = player.ent_controlled.GetComponentWithOwner<Worker.Data>(Relation.Type.Instance);
							//ref var worker = ref ref_worker.data;

							//Crafting.Context.New(ref region, this.ent_construction, player.ent_controlled, out var context, world_position: this.transform.position, inventory: player.GetInventory(), experience: player.GetExperience(), h_faction: character_data.IsNull() ? this.faction.id : character_data.faction);
							Crafting.Context.NewFromCurrentCharacter(ent_producer: this.ent_construction, out var context, search_radius: this.order.search_radius);

							//var order = this.ent_construction.GetTrait<Construction.Data, Crafting.Order>().OrDefault();

#if NEW_WORK_ORDER
							if (this.order.h_recipe.IsValid())
							{
								using (var scrollbox = GUI.Scrollbox.New("scroll.order"u8, size: GUI.Rm, padding: new(0, 0), force_scrollbar: true))
								{
									Crafting.DrawOrder(order: in this.order,
									entity: this.ent_construction,
									handle: this.h_order,
									context: ref context,
									draw_flags_rem: Crafting.DrawOrderFlags.Products | Crafting.DrawOrderFlags.Collapsible |
									Crafting.DrawOrderFlags.Icon | Crafting.DrawOrderFlags.Header | Crafting.DrawOrderFlags.Cancel | Crafting.DrawOrderFlags.Buttons |
									Crafting.DrawOrderFlags.Temperature | Crafting.DrawOrderFlags.Mass | Crafting.DrawOrderFlags.Pressure | Crafting.DrawOrderFlags.Info | Crafting.DrawOrderFlags.Description);
								}
							}
							else
							{
								var h_selected_recipe = this.construction.h_recipe;

								ref var baseline_recipe_data = ref h_selected_recipe.GetData(out var baseline_recipe_asset);
								ref var selected_recipe_data = ref baseline_recipe_data;

								ref var selected_blueprint_data = ref this.construction.h_blueprint.GetData();
								if (selected_blueprint_data.IsNotNull())
								{
									selected_recipe_data = ref selected_blueprint_data.h_recipe_generated.GetData();
									h_selected_recipe = selected_blueprint_data.h_recipe_generated;
								}

								if (selected_recipe_data.IsNotNull())
								{
									var draw_args = new GUI.DrawRecipeArgs()
									{
										selected_stage_options = ref this.construction.selected_stage_options
									};

									var draw_results = GUI.DrawShopRecipe(context: ref context,
										h_recipe: h_selected_recipe,
										evaluation_flags: Crafting.EvaluateFlags.Prerequisite | Crafting.EvaluateFlags.Display_Only,
										flags_add: GUI.DrawRecipeFlags.None,
										flags_rem: GUI.DrawRecipeFlags.Counter | GUI.DrawRecipeFlags.Send_RPC,
										enabled: true,
										args: draw_args);
									//Crafting.DrawOrder(order: in this.order, entity: this.ent_construction, handle: this.h_order, context: ref context);

									using (var window_child = window.BeginChildWindow(identifier: "construction.sub"u8,
									anchor_x: GUI.AlignX.Right,
									anchor_y: GUI.AlignY.Top,
									open: true,
									size: new Vector2(96 + 12 + 4, 480),
									padding: new(4, 4)))
									{
										if (window_child.show)
										{
											using (var group = GUI.Group.New(size: GUI.Rm, padding: new(2)))
											{
												var bp_spanlist = FixedArray.CreateSpan32NoInit<IBlueprint.Handle>(out var bp_buffer).AsSpanList();
												IBlueprint.Database.GetHandlesFiltered(ref bp_spanlist, baseline_recipe_asset.GetHandle(),
													static (in x, arg) => x.data.h_recipe == arg && x.data.flags.HasNone(IBlueprint.Flags.Editable | IBlueprint.Flags.Hidden));

												//using (var group_top = GUI.Group.New(size: new(GUI.RmX, 32)))
												//{
												//	GUI.TitleCentered("Blueprints"u8, size: 24, pivot: new(0.00f, 0.50f), offset: new(4, 0));
												//}

												//GUI.SeparatorThick();

												var h_selected_recipe_tmp = h_selected_recipe;

												using (var group_mid = GUI.Group.New(size: GUI.Rm))
												{
													foreach (var h_blueprint in bp_spanlist)
													{
														ref var blueprint_data = ref h_blueprint.GetData();
														if (blueprint_data.IsNotNull())
														{
															ref var recipe_data = ref blueprint_data.h_recipe_generated.GetData();
															if (recipe_data.IsNotNull())
															{
																using (var group_bp = GUI.Group.New(size: new(GUI.RmX)))
																{
																	//group_bp.DrawBackground(GUI.tex_slot);

																	var is_selected = h_blueprint == this.construction.h_blueprint;

																	if (GUI.DrawRecipeButton(context: ref context,
																	rect: group_bp.GetOuterRect(),
																	h_recipe: blueprint_data.h_recipe_generated,
																	h_recipe_selected: ref h_selected_recipe_tmp,
																	tooltip_anchor: GUI.Anchor.Right,
																	tooltip_pivot: new(0.00f, 0.50f)))
																	{
																		var rpc = new Construction.ConfigureRPC()
																		{
																			h_blueprint = is_selected ? IBlueprint.Handle.None : h_blueprint
																		};
																		rpc.Send(this.ent_construction);
																	}
																}
															}
														}
													}
												}
											}
										}
									}

									if (draw_results.HasAny(GUI.DrawRecipeResults.Stage_Options | GUI.DrawRecipeResults.Amount_Multiplier | GUI.DrawRecipeResults.Container))
									{
										var rpc = new Construction.ConfigureRPC()
										{
											selected_stage_options = draw_args.selected_stage_options
										};
										rpc.Send(this.ent_construction);
									}
									else if (draw_results.HasAny(GUI.DrawRecipeResults.Submit))
									{
										App.WriteLine("submit");

										var rpc = new Construction.SubmitRPC()
										{

										};
										rpc.Send(this.ent_construction);
									}
								}
							}
#else
						ref var recipe = ref order.recipe.GetData();
						var has_resources = true;
						//var has_worker = false;

						using (GUI.Group.New(size: new(GUI.RmX * 0.50f, GUI.RmY), padding: new(4)))
						{
							GUI.Draw9Slice(GUI.tex_panel, new(4), GUI.Rm);

							using (GUI.Group.New2(size: GUI.Rm, padding: new(4, 4, 4, 4)))
							{
								using (GUI.Wrap.Push(GUI.RmX))
								{
									if (recipe.IsNotNull())
									{
										GUI.Text(recipe.GetDescription().OrDefault(recipe.GetDescriptionFallback()));
									}
								}

								GUI.NewLine(4);
								GUI.Title("Requirements"u8, size: 16);
								GUI.Separator();

								using (GUI.Group.New(size: new(GUI.RmX, 0.00f), padding: new Vector2(0, 4)))
								{
									for (var i = 0; i < this.shipment.items.Length; i++)
									{
										ref var item = ref this.shipment.items[i];
										using (GUI.ID.Push(i))
										{
											using (GUI.Group.New(size: new Vector2(GUI.RmX, 48)))
											{
												var button_size = new Vector2(24 * 4, 32);

												var bg_color = new Color32BGRA(0xffffffff);
												GUI.DrawFillBackground(GUI.tex_panel, new(4));
												//if (button.hovered) bg_color = bg_color.WithAlphaMult(1.00f);
												//else bg_color = bg_color.WithAlphaMult(0.75f);

												//GUI.Draw9Slice(GUI.tex_slot_simple, new(4, 4, 4, 4), button.bb, color: bg_color);

												using (GUI.Group.New2(size: GUI.Rm, padding: new(4, 4, 4, 4)))
												{
													switch (item.type)
													{
														case Shipment.Item.Type.Resource:
														{
															ref var material = ref item.material.GetData();
															if (material.IsNotNull())
															{
																var ok = item.quantity >= item.max;
																has_resources &= ok;

																var amount_available = 0.00f;
																if (!ok)
																{
																	if (Constants.Requirements.disable_requirements)
																	{
																		amount_available = item.max;
																	}
																	else
																	{
																		amount_available = context.GetMaterialCount(item.material);
																	}
																}

																var text = string.Empty;
																var text_color = GUI.font_color_default;
																var button_color = GUI.font_color_default_dark;

																if (ok)
																{
																	text = $"{item.quantity.Simplify():0.##}/{item.max.Simplify():0.##}";
																	text_color = GUI.font_color_green;
																}
																else
																{
																	text = $"{item.quantity.Simplify():0.##}/{item.max.Simplify():0.##}";
																	text_color = GUI.font_color_red;

																	if (amount_available <= 0.00f)
																	{
																		button_color = GUI.col_button_error;
																	}
																	else if ((item.quantity + amount_available) < item.max)
																	{
																		button_color = GUI.col_button_yellow;
																	}
																	else
																	{
																		button_color = GUI.col_button_ok;
																	}
																}

																GUI.DrawMaterialSmall(item.material, new Vector2(GUI.RmY));

																GUI.SameLine();

																using (GUI.Group.New(size: GUI.Rm - new Vector2(40, 0), padding: new(4, 4)))
																{
																	GUI.Title(material.name, color: GUI.font_color_default);
																	GUI.TextShaded(text, color: text_color);

																	//if (amount_available > 0.00f)
																	//{
																	//	GUI.SameLine();
																	//	GUI.TextShadedCentered($"+{amount_available.Simplify():0.##}", pivot: new Vector2(1, 0.00f), color: GUI.font_color_yellow);
																	//}
																}

																GUI.SameLine();

																using (GUI.Group.New(size: GUI.Rm))
																{

																	if (GUI.DrawSpriteButton("deposit"u8, "ui_button_deposit", new(40, 40), enabled: !ok && amount_available > Maths.epsilon, color: button_color))
																	{
																		var rpc = new Construction.DepositRPC()
																		{
																			index = i
																		};
																		rpc.Send(this.ent_construction);
																	}


																	//if (GUI.DrawIconButton(new Sprite("missing"), new(40, 40), enabled: !ok, color: button_color))
																	//{
																	//	var rpc = new Construction.DepositRPC()
																	//	{
																	//		index = i
																	//	};
																	//	rpc.Send(this.ent_construction);
																	//}

																	if (GUI.IsItemHovered())
																	{
																		using (GUI.Tooltip.New())
																		{
																			using (GUI.Wrap.Push(200))
																			{
																				GUI.Text("Fetch resources from nearby inventories."u8);

																				if (!ok)
																				{
																					GUI.NewLine(4);
																					GUI.Separator();
																					GUI.NewLine(4);

																					var text_color_b = GUI.font_color_red;

																					if (amount_available <= 0.00f)
																					{
																						text_color_b = GUI.font_color_red;
																					}
																					else if ((item.quantity + amount_available) < item.max)
																					{
																						text_color_b = GUI.font_color_yellow;
																					}
																					else
																					{
																						text_color_b = GUI.font_color_green;
																					}

																					if (Constants.Requirements.disable_requirements)
																					{
																						GUI.LabelShaded("Available:"u8, "Unlimited"u8, color_b: text_color_b);
																					}
																					else
																					{
																						GUI.LabelShaded("Available:"u8, $"{amount_available.Simplify():0.##}/{(item.max - item.quantity).Simplify():0.##}", color_b: text_color_b);
																						//GUI.Label($"Available:", $"{amount_available:0.##}", color_b: text_color_b);
																						//GUI.Text($"{amount_available:0.##}", color: text_color_b);
																						//GUI.SameLine();
																						//GUI.TextShadedCentered($"-{(item.max - item.quantity):0.##}", pivot: new(1.00f, 0.00f), color: GUI.font_color_red);
																					}
																				}
																			}
																		}
																	}
																}

																//if (button.pressed)
																//{
																//	var rpc = new Shipment.DepositRPC()
																//	{
																//		amount = 20.00f,
																//		index = (uint)i
																//	};
																//	rpc.Send(this.ent_construction);
																//}
															}
														}
														break;

														case Shipment.Item.Type.Prefab:
														{

														}
														break;
													}
												}
												//GUI.Title(item.type.ToString());


											}

											GUI.NewLine(4);
										}
									}
								}


							}
						}

						GUI.SameLine();

						using (GUI.Group.New(size: GUI.Rm))
						{
							for (var i = 0; i < order.work.Length; i++)
							{
								ref var work = ref order.work[i];
								using (GUI.ID<Construction.Data, Work.Amount>.Push(i))
								{
									using (GUI.Group.New(size: new Vector2(GUI.RmX, 32)))
									{
										var button_size = new Vector2(24 * 4, 32);

										ref var work_data = ref work.type.GetData();

										var work_color = Color32BGRA.Button;
										if (work_data.IsNotNull())
										{
											//work_color = ImGuiNative.igImAlphaBlendColors(work_color, work_data.color);
											work_color = Color32BGRA.LerpRGB(work_color, work_data.color, work_data.color.a / 255.00f);
										}

										GUI.DrawWork(ref context.experience.data, work, new(GUI.RmX - button_size.X, GUI.RmY), work_color);
										//GUI.OffsetLine(GUI.RmW - button_size.X);
										GUI.SameLine();

										if (work_data.IsNotNull())
										{
											var button_color = has_resources ? GUI.col_button_yellow : GUI.font_color_default_dark;

											if (work.remaining <= 0.00f)
											{
												if (GUI.DrawButton(""u8, button_size, enabled: false))
												{

												}
											}
											else if (GUI.GetDebugKey())
											{
												if (GUI.DrawButton("DEV: Force"u8, button_size, enabled: work.remaining > 0.00f, color: GUI.col_button_debug))
												{
													var rpc = new Worker.DEV_ForceCompleteRPC
													{
														ent_target = this.ent_construction,
														h_order = IComponent.Handle.FromComponentPair<Construction.Data, Crafting.Order>(),
														work_index = (byte)i,
													};
													rpc.Send(context.worker.entity);
												}
											}
											else if (work.flags.HasAny(Work.Amount.Flags.Crafter_Only))
											{
												if (GUI.DrawButton(""u8, button_size, enabled: false))
												{

												}
											}
											else if (context.worker.data.work_index != i)
											{
												if (GUI.DrawButton("Work"u8, button_size, enabled: work.remaining > 0.00f && has_resources, color: button_color))
												{
													var rpc = new Worker.SetTargetRPC
													{
														ent_target = this.ent_construction,
														h_order = IComponent.Handle.FromComponentPair<Construction.Data, Crafting.Order>(),
														work_index = (byte)i,
													};
													rpc.Send(context.worker.entity);
												}
											}
											else
											{
												if (GUI.DrawButton("Stop"u8, button_size, enabled: work.remaining > 0.00f && has_resources, color: button_color))
												{
													var rpc = new Worker.SetTargetRPC
													{
														ent_target = default
													};
													rpc.Send(context.worker.entity);
												}
											}
										}
										else
										{
											if (GUI.DrawButton(""u8, button_size, enabled: false))
											{

											}
										}
									}
								}
							}

							using (GUI.Group.New(size: new Vector2(GUI.RmX, 48)))
							{
								var button_color = GUI.col_button_ok; // new Color32BGRA(0xff9dd80f);
								if (GUI.DrawButton($"Stage: {this.construction.stage}", new(GUI.RmX, 48), enabled: this.construction.stage == Construction.Stage.Complete, color: button_color))
								{
									var rpc = new Construction.CompleteRPC
									{

									};
									rpc.Send(this.ent_construction);
								}
							}

							using (GUI.Group.New2(size: GUI.Rm, padding: new(6, 6, 8, 8)))
							{
								GUI.DrawFillBackground(GUI.tex_panel, new(4), margin: new(-4));

								GUI.Title("Info"u8, size: 16);
								GUI.Separator();

								using (GUI.Group.New(size: GUI.Rm, padding: new(4)))
								{
									if (!has_resources && this.construction.stage == Construction.Stage.Materials) GUI.Text("- Not enough resources!"u8, color: GUI.font_color_red);
									//if (!has_worker && this.construction.stage == Construction.Stage.Work) GUI.Text("- No worker assigned!"u8, color: GUI.font_color_yellow);

									//if (GUI.DrawButton("Complete", new Vector2(80, 48)))
									//{
									//	var rpc = new Construction.DebugRPC
									//	{

									//	};
									//	rpc.Send(this.ent_construction);
									//}
								}
							}
						}
#endif
						}
					}
				}
			}
		}

		[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnGUI(Entity entity, IComponent.Handle<Crafting.Order> h_order,
		[Source.Owned] in Construction.Data construction, [Source.Owned, Pair.Component<Construction.Data>] in Crafting.Order order,
		[Source.Owned] in Interactable.Data interactable,
		[Source.Owned] in Transform.Data transform, [Source.SharedAny] in Shipment.Data shipment, [Source.Owned, Optional] in Faction.Data faction)
		{
			if (interactable.IsActive())
			{
				var gui = new ConstructionGUI()
				{
					ent_construction = entity,

					h_order = h_order,
					order = order,

					construction = construction,
					transform = transform,
					shipment = shipment,
					faction = faction
				};
				gui.Submit();
			}
		}

		public struct WorkshopHUD: IGUICommand
		{
			public Vector2 world_position;
			public Entity ent_workshop;
			public Workshop.Data workshop;

			public static float next_work_time;

			public void Draw()
			{
				ref var region = ref this.ent_workshop.GetRegion();
				using (var widget = Sidebar.Widget.New("workshop", "Workshop", new Sprite(GUI.tex_icons_widget, 16, 16, 7, 0), size: new Vector2(400, 300), has_window: true, lockable: false))
				{
					if (widget.state_flags.HasAny(Sidebar.Widget.StateFlags.Show))
					{
						ref var player = ref Client.GetPlayer();
						ref var experience = ref player.GetExperience().data;
						var ref_worker = player.ent_controlled.GetComponentWithOwner<Worker.Data>(Relation.Type.Instance);
						ref var worker = ref ref_worker.data;
					}
				}
			}
		}


		[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("local", true, Source.Modifier.Shared)]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void OnHUD(ISystem.Info info, Entity entity, [Source.Shared] in Player.Data player, [Source.Owned] ref Workshop.Data workshop, [Source.Owned] in Transform.Data transform)
		{
			return;

			var gui = new WorkshopHUD()
			{
				ent_workshop = entity,
				workshop = workshop,
				world_position = transform.GetInterpolatedPosition()
			};
			gui.Submit();
		}
#endif

		//[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region, interval: 0.50f)]
		//public static void OnUpdateWorkSprite(ISystem.Info info, ref Region.Data region, Entity ent_order,
		//[Source.Owned, Pair.Tag("work")] ref Animated.Renderer.Data renderer, [Source.Owned, Pair.First] ref Crafting.Order order)
		//{
		//	ref var recipe = ref order.h_recipe.GetData();
		//	if (recipe.IsNotNull())
		//	{
		//		renderer.sprite.frame.y = 1u + Maths.LerpUInt(0u, renderer.sprite.count, Maths.Normalize01Fast(order.amount_multiplier, recipe.max));
		//		renderer.color_mask_r = order.current_color;
		//	}
		//	else
		//	{
		//		renderer.sprite.frame.y = 0;
		//	}
		//}

		[Shitcode]
		[ISystem.PostUpdate.A(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnUpdateAxle(/*ISystem.Info info, ref Region.Data region,*/
		/*[Source.Owned] ref Crafter.Data crafter,*/ [Source.Owned] ref Crafter.State crafter_state, /*Entity ent_crafter,*/
		/*[Source.Owned] in Axle.Data axle,*/ [Source.Owned] in Axle.State axle_state/*, Entity ent_axle*/)
		{
			crafter_state.angular_velocity = axle_state.angular_velocity;
		}

		[Shitcode]
		[ISystem.PostUpdate.A(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnUpdateAmbient(/*ISystem.Info info, ref Region.Data region,*/
		/*[Source.Owned] ref Crafter.Data crafter,*/ [Source.Owned] ref Crafter.State crafter_state, /*Entity ent_crafter,*/
		[Source.Owned] in Transform.Data transform)
		{
			crafter_state.temperature = Temperature.Ambient;
			crafter_state.temperature_ambient = Temperature.Ambient;
			crafter_state.pressure = Pressure.Ambient;
		}

		[Shitcode]
		[ISystem.PostUpdate.B(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnUpdateFillable(/*ISystem.Info info, ref Region.Data region,*/
		/*[Source.Owned] ref Crafter.Data crafter,*/ [Source.Owned] ref Crafter.State crafter_state, /*Entity ent_crafter,*/
		[Source.Owned] in Fillable.Data fillable)
		{
			crafter_state.capacity = fillable.volume_capacity;
			crafter_state.volume = fillable.volume_current;

			crafter_state.h_substance_fill = fillable.h_substance_fill;
			crafter_state.mass = fillable.mass_current;
		}

		[Shitcode]
		[ISystem.PostUpdate.B(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnUpdateHeat(/*ISystem.Info info, ref Region.Data region,*/
		/*[Source.Owned] ref Crafter.Data crafter,*/ [Source.Owned] ref Crafter.State crafter_state, /*Entity ent_crafter,*/
		/*[Source.Owned] in Heat.Data heat, */[Source.Owned] in Heat.State heat_state, /*Entity ent_heat,*/
		[Source.Owned] in Transform.Data transform)
		{
			crafter_state.temperature = heat_state.temperature_current; // Maths.Max(crafter_state.temperature, heat_state.temperature_current);
			crafter_state.temperature_ambient = heat_state.temperature_ambient; // Maths.Max(crafter_state.temperature_ambient, heat_state.temperature_ambient);
		}

		[Shitcode]
		[ISystem.PostUpdate.C(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnUpdateHeatAir(/*ISystem.Info info, ref Region.Data region,*/
		/*[Source.Owned] ref Crafter.Data crafter,*/ [Source.Owned] ref Crafter.State crafter_state, /*Entity ent_crafter,*/
		[Source.Owned] in Air.Container.Data air_container)
		{
			crafter_state.pressure = Maths.Max(crafter_state.pressure, air_container.pressure_cached);
			crafter_state.capacity = Maths.Max(crafter_state.capacity, air_container.volume);
			crafter_state.temperature = Maths.Max(crafter_state.temperature, air_container.temperature);
			//crafter_state.volume = air_container.volume;
		}

		[Shitcode]
		[ISystem.PostUpdate.D(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnUpdateHeatAttachments(
		[Source.Parent] in Attachment.Slot attachment_slot, [Source.Parent] ref Crafter.State crafter_state,
		[Source.Owned] in Heat.State heat_state)
		{
			crafter_state.temperature = Maths.Max(crafter_state.temperature, heat_state.temperature_current);
		}


#if CLIENT
		public struct CrafterGUI: IGUICommand
		{
			public Entity ent_crafter;

			public Transform.Data transform;

			public Crafter.Data crafter;
			public Crafter.State crafter_state;

			public void Draw()
			{
				if (false)
				{
					using (var window = GUI.Window.Interaction("Craft"u8, entity: this.ent_crafter, tooltip_tab: "Crafting"))
					{
						this.StoreCurrentWindowTypeID(order: -90);
						if (window.show)
						{
							using (GUI.Group.New(size: GUI.Rm))
							{

								ref var shop = ref this.ent_crafter.GetComponent<Shop.Data>();
								if (shop.IsNotNull())
								{
									var h_selected_recipe = Shop.ShopGUI.edit_h_selected_recipe;
									var h_selected_blueprint = Shop.ShopGUI.edit_h_selected_blueprint;

									ref var recipe_data = ref h_selected_recipe.GetData(out var recipe_asset);


									if (recipe_data.IsNotNull())
									{
										var evaluation_flags = Crafting.EvaluateFlags.Prerequisite;
										var h_order_selected_id = window.GetStoredData<ulong>(42);

										Crafting.Context.NewFromCurrentCharacter(this.ent_crafter, out var context);

										using (var group_top = GUI.Group.New(size: new(GUI.RmX, 48)))
										{
											group_top.DrawBackground(GUI.tex_frame);
										}

										using (var scroll_h = GUI.Scrollbox.New("scroll.recipe"u8, size: GUI.Rm))
										{
											var stages_span = recipe_data.stages.AsSpan();
											for (var i = 0u; i < stages_span.ULength; i++)
											{
												ref var stage = ref stages_span[(int)i];

												if (i != 0) GUI.SameLine();

												using (var group_stage = GUI.Group.New(size: new(192, GUI.RmY), padding: new(4)))
												{
													group_stage.DrawBackground(GUI.tex_window); //, color: GUI.col_frame);
												}
											}
										}

										//GUI.DrawShopRecipe(context: ref context,
										//h_recipe: h_selected_recipe,
										//h_order: h_order_selected_id,
										//evaluation_flags: evaluation_flags);

									}
								}
							}
						}
					}
				}

				using (var window = GUI.Window.InteractionMisc("Crafter"u8, this.ent_crafter, size: new(48, 96 * 1), min_width: 48))
				{
					this.StoreCurrentWindowTypeID(order: -100);
					if (window.show)
					{
						using (GUI.Group.New(size: GUI.Rm))
						{
							//GUI.SameLine(24);
							if (this.crafter.flags.HasNone(Crafter.Data.Flags.Hide_Pressure))
							{
								if (this.crafter.pressure_max > 0)
								{
									GUI.DrawPressureRange(this.crafter_state.pressure, Phys.atmospheric_pressure_kordel, this.crafter.pressure_max, size: new(24, GUI.RmY));
								}
								else
								{
									using (var group_left = GUI.Group.New(size: new(GUI.RmX * 0.50f, GUI.RmY)))
									{
										group_left.DrawBackground(GUI.tex_slot_filled);
									}
								}
								GUI.SameLine();
							}

							if (this.crafter.flags.HasNone(Crafter.Data.Flags.Hide_Temperature))
							{
								if (this.crafter.temperature_max > 0)
								{
									GUI.DrawTemperatureRange(this.crafter_state.temperature, this.crafter.temperature_max, this.crafter.temperature_max + 300, size: new(24, GUI.RmY), label_b: "Current"u8, label_a: "Max"u8);
								}
								else
								{
									using (var group_left = GUI.Group.New(size: new(GUI.RmX, GUI.RmY)))
									{
										group_left.DrawBackground(GUI.tex_slot_filled);
									}
								}
							}
						}
					}
				}
			}
		}

		[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region, order: -100)]
		public static void OnGUI(Entity entity,
		[Source.Owned] in Interactable.Data interactable, [Source.Owned] in Transform.Data transform,
		[Source.Owned] in Crafter.Data crafter, [Source.Owned] in Crafter.State crafter_state)
		{
			if (interactable.IsActive() && crafter.flags.HasNone(Crafter.Data.Flags.No_GUI) && !crafter.flags.HasAll(Crafter.Data.Flags.Hide_Temperature | Crafter.Data.Flags.Hide_Pressure))
			{
				var gui = new CrafterGUI()
				{
					ent_crafter = entity,

					transform = transform,

					crafter = crafter,
					crafter_state = crafter_state
				};
				gui.Submit();
			}
		}
#endif

		//#if SERVER
		//		[ISystem.Update.D(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global)]
		//		public static void UpdateProgress(ISystem.Info.Common info, ref Region.Data.Common region_common, Entity ent_crafter, ref XorRandom random,
		//		[Source.Owned] ref Crafter.Data crafter, [Source.Owned] ref Crafter.State crafter_state,
		//		[Source.Owned] in Transform.Data transform)
		//		{
		//			return;

		//			if (crafter.h_crafter && crafter_state.flags.HasAny(Crafter.State.Flags.Ready) && info.WorldTime >= crafter_state.t_next_update)
		//			{
		//				var cooldown = 3.00f;
		//				var sync = false;

		//				ref var order = ref crafter.ref_order_selected.GetComponent();
		//				if (order.IsNotNull())
		//				{
		//					//ref var random = ref info.GetRandom();
		//					if (ent_crafter == order.ent_crafter_selected)
		//					{
		//						//WorldNotification.Push(ref region_common.AsRegion(), "tick", Color32BGRA.Yellow, transform.position);

		//						var work_mult = 1.00f;
		//						scoped ref var mode = ref Unsafe.NullRef<ICrafter.ModeInfo>();

		//						if (crafter_state.current_mode.HasValue) // && !crafter_state.current_work_index.HasValue)
		//						{
		//							ref var crafter_data = ref crafter.h_crafter.GetData();
		//							if (crafter_data.IsNotNull())
		//							{
		//								mode = ref crafter_data.modes.GetRefAtIndexOrNull(crafter_state.current_mode.GetValueOrDefault());
		//								if (mode.IsNotNull())
		//								{
		//									//if (!crafter_state.current_work_index.HasValue)
		//									{
		//										byte? work_index_tmp = null;

		//										for (var i = 0u; i < order.work_handles.ULength; i++)
		//										{
		//											var h_work = order.work_handles[i];
		//											if (!h_work) continue;
		//											if (order.work[i].flags.HasNone(Work.Amount.Flags.Pending)) continue;

		//											if (mode.ContainsWork(h_work))
		//											{
		//												work_mult = mode.work_multiplier;
		//												work_index_tmp = (byte)i;
		//												break;
		//											}							
		//										}
		//										sync |= crafter_state.current_work_index.TrySetValue(work_index_tmp);
		//									}
		//								}
		//							}
		//						}

		//						//App.WriteLine(ent_crafter.GetFullName());
		//						var result = Worker.WorkResult.Idle;


		//						if (mode.IsNotNull())
		//						{
		//							var pos = transform.LocalToWorld(crafter.offset);
		//							result = Worker.PerformWorkMachine(region_common: ref region_common,
		//									ent_crafter: ent_crafter, crafter: ref crafter, crafter_state: ref crafter_state,
		//									order: ref order, world_position: pos, out cooldown, work_multiplier: work_mult);

		//							cooldown *= mode.cooldown_multiplier;
		//						}

		//						const bool debug_print_result = false;
		//						if (debug_print_result)
		//						{
		//							WorldNotification.Push(ref region_common.AsRegion(), result.ToString(), Color32BGRA.Yellow, transform.position);
		//						}

		//						//App.WriteValue(result);

		//						switch (result)
		//						{
		//							case Worker.WorkResult.Progress:
		//							{
		//								order.flags.AddFlag(Crafting.Order.Flags.Dirty);
		//								order.cache_flags.RemoveFlag(Crafting.Order.CacheFlags.Work_Progress);

		//								if (mode.IsNotNull())
		//								{
		//									order.Sync(crafter.ref_order_selected.entity, crafter.ref_order_selected.ComponentID);
		//									crafter_state.flags.TryAddRemFlags(mode.crafter_state_on_work);
		//								}

		//								sync = true;
		//							}
		//							break;

		//							case Worker.WorkResult.Idle:
		//							case Worker.WorkResult.Waiting:
		//							{

		//							}
		//							break;

		//							case Worker.WorkResult.Failed:
		//							case Worker.WorkResult.Skipped:
		//							case Worker.WorkResult.Invalid:
		//							case Worker.WorkResult.Error:
		//							case Worker.WorkResult.Outdated:
		//							{

		//							}
		//							break;

		//							case Worker.WorkResult.Done:
		//							{
		//								order.flags.AddFlag(Crafting.Order.Flags.Dirty);
		//								order.cache_flags.RemoveFlag(Crafting.Order.CacheFlags.Work_Done | Crafting.Order.CacheFlags.Work_Progress);

		//								crafter_state.current_work_index = null;
		//								sync = true;
		//							}
		//							break;
		//						}

		//						crafter_state.last_work_result = result;
		//					}
		//					else
		//					{
		//						crafter.ref_order_selected.Reset();
		//						sync = true;
		//					}
		//				}
		//				else
		//				{
		//					if (crafter_state.current_work_index.HasValue)
		//					{
		//						crafter_state.current_work_index = null;
		//						sync = true;
		//						//crafter_state.Sync(ent_crafter, true);
		//					}
		//				}

		//				if (sync)
		//				{
		//					crafter.Sync(ent_crafter, true);
		//					crafter_state.Sync(ent_crafter, true);
		//				}

		//				crafter_state.t_next_update = info.WorldTime + cooldown;
		//			}
		//		}
		//#endif
	}
}
