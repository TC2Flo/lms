
//using System.Diagnostics.CodeAnalysis;

//namespace TC2.Base.Components
//{
//	[Obsolete]
//	public static partial class Upgradable
//	{
//		//public enum Type: byte
//		//{
//		//	Undefined = 0,
//		//}

//		[Flags]
//		public enum Flags: ushort
//		{
//			None = 0,
//		}

//		[IComponent.Data(Net.SendType.Reliable)]
//		public struct Data(): IComponent
//		{
//			public Upgradable.Flags flags;

//			[Save.NewLine]
//			public Filter.Mask<Recipe.Flags> filter_flags;
//			[Save.NewLine]
//			public Filter.Mask<Recipe.Tags> filter_tags;
//		}

////#if SERVER
////		[ISystem.Modified.Component<Upgradable.Data>(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global)]
////		public static void OnModified(ISystem.Info.Common info, ref Region.Data.Common region, Entity ent_upgradable,
////		[Source.Owned] ref Upgradable.Data upgradable, [Source.Owned, Pair.Component<Upgradable.Data>] ref Crafting.Order order, [Source.Owned] in Transform.Data transform)
////		{
////			//ref var order = ref upgradable.order;
////			if (order.TryProgress())
////			{
////				if (order.flags.HasAny(Crafting.Order.Flags.Complete))
////				{
////					ref var recipe_data = ref order.h_recipe.GetData();
////					if (recipe_data.IsNotNull())
////					{
////						Crafting.Context.NewFromSelf(ref region, ent_upgradable, out var context);
////						Crafting.Produce(ref context, recipe_data.products, spawn_flags: Resource.SpawnFlags.Merge);
////					}

////					order = default;
////					upgradable.Sync(ent_upgradable, true);
////				}
////			}

////			//ref var order = ref upgradable.order;
////			//if (order.flags.TryRemoveFlag(Crafting.Order.Flags.Complete))
////			//{
////			//	ref var recipe_data = ref order.recipe.GetData();
////			//	if (recipe_data.IsNotNull())
////			//	{
////			//		Crafting.Context.NewFromSelf(ref region, ent_upgradable, out var context);
////			//		Crafting.Produce(ref context, recipe_data.products, spawn_flags: Resource.SpawnFlags.Merge);
////			//	}

////			//	order = default;
////			//	upgradable.Sync(ent_upgradable, true);
////			//}
////		}
////#endif

//		public struct SelectRPC: Net.IRPC<Upgradable.Data>
//		{
//			public IRecipe.Handle h_recipe;

//#if SERVER
//			public void Invoke(Net.IRPC.Context rpc, ref Upgradable.Data data)
//			{
//				ref var recipe_data = ref h_recipe.GetData();
//				Assert.IsNotNull(ref recipe_data);

//				ref var order = ref rpc.entity.GetOrAddPair<Upgradable.Data, Crafting.Order>(true);
//				Assert.Check(order.flags.HasNone(Crafting.Order.Flags.In_Progress));
				
//				order = new Crafting.Order()
//				{
//					h_recipe = this.h_recipe,
//					amount_multiplier = 1.00f,
//					flags = Crafting.Order.Flags.In_Progress
//				};

//				//var work_index = 0;

//				//var requirements = recipe_data.requirements.AsSpan();
//				//for (var i = 0; i < requirements.Length; i++)
//				//{
//				//	ref var requirement = ref requirements[i];
//				//	if (requirement.type == Crafting.Requirement.Type.Work)
//				//	{
//				//		order.work[work_index++] = new Work.Amount(requirement.work, Work.Amount.Flags.Pending, requirement.group, requirement.difficulty, (byte)i, requirement.amount);
//				//	}
//				//}

//				//data.order = order;
//				//data.Sync(rpc.entity);
//			}
//#endif
//		}

//#if CLIENT
//		public partial struct UpgradableGUI: IGUICommand
//		{
//			public Entity ent_interactable;
//			public Entity ent_upgradable;

//			public Transform.Data transform;
//			public Upgradable.Data upgradable;
//			public Crafting.Order order;

//			public void Draw()
//			{
//				using (var window = GUI.Window.Interaction("Upgrades"u8, this.ent_interactable, color_tab: GUI.col_button_yellow))
//				{
//					this.StoreCurrentWindowTypeID(order: 500);
//					if (window.show)
//					{
//						ref var region_common = ref this.ent_interactable.GetRegionCommon();
//						var h_character = Client.GetCharacterHandle();

//						Crafting.Context.NewFromCharacter(region: ref region_common, h_character: h_character, ent_producer: this.ent_upgradable, context: out var context);

//						ref var order_recipe = ref this.order.h_recipe.GetData();
//						if (order_recipe.IsNull())
//						{
//							var h_recipe_selected = window.GetStoredData<IRecipe.Handle>(0);

//							var recipes_span = FixedArray.CreateSpan16NoInit<IRecipe.Handle>(out var buffer);
//							IRecipe.Database.GetHandlesFiltered(span: ref recipes_span, arg: in this.upgradable, predicate: static (in IRecipe.Definition asset, in Upgradable.Data upgradable) =>
//							{
//								return upgradable.filter_tags.Evaluate(asset.data.tags) && upgradable.filter_flags.Evaluate(asset.data.flags);
//							});

//							using (var scroll = GUI.Scrollbox.New("scroll.recipes"u8, size: new(Maths.Min(192, GUI.RmX * 0.50f), GUI.RmY)))
//							{
//								using (var group = GUI.Group.New(size: GUI.Rm))
//								{
//									group.DrawBackground(GUI.tex_panel);

//									for (var i = 0; i < recipes_span.Length; i++)
//									{
//										var h_recipe = recipes_span[i];
//										ref var recipe_data = ref h_recipe.GetData();
//										if (recipe_data.IsNotNull())
//										{
//											using (GUI.ID<UpgradableGUI, IRecipe.Handle>.Push(h_recipe))
//											{
//												using (var group_row = GUI.Group.New(size: new(GUI.RmX, 48)))
//												{
//													if (group_row.IsVisible())
//													{
//														if (GUI.DrawRecipeButton(context: ref context, rect: group_row.GetOuterRect(), h_recipe: h_recipe, h_recipe_selected: ref h_recipe_selected,
//														evaluation_flags: Crafting.EvaluateFlags.Display_Only,
//														flags_add: GUI.DrawRecipeFlags.None,
//														flags_rem: GUI.DrawRecipeFlags.Products))
//														{
//															window.SetStoredData<IRecipe.Handle>(0, h_recipe_selected);
//														}
//													}
//												}
//											}
//										}
//									}
//								}
//							}

//							GUI.SameLine();

//							using (var group_right = GUI.Group.New(size: new(GUI.RmX, GUI.RmY), padding: new(4)))
//							{
//								group_right.DrawBackground(GUI.tex_window);

//								ref var recipe_data = ref h_recipe_selected.GetData();
//								if (recipe_data.IsNotNull())
//								{
//									if (GUI.DrawShopRecipe(context: ref context, h_recipe: h_recipe_selected,
//									evaluation_flags: Crafting.EvaluateFlags.Display_Only,
//									flags_add: GUI.DrawRecipeFlags.None,
//									flags_rem: GUI.DrawRecipeFlags.Products | GUI.DrawRecipeFlags.Send_RPC).HasAny(GUI.DrawRecipeResults.Submit))
//									{
//										var rpc = new Upgradable.SelectRPC
//										{
//											h_recipe = h_recipe_selected
//										};
//										rpc.Send(this.ent_upgradable);
//									}
//								}
//								else
//								{

//								}
//							}
//						}
//						else
//						{
//							ref var shipment = ref context.shipment.data;
//							//ref var worker = ref context.worker.data;
//							//ref var experience = ref context.experience.data;

//							var has_resources = true;
//							//var has_worker = context.worker.IsValid();

//							using (GUI.Group.New(size: new(GUI.RmX * 0.50f, GUI.RmY), padding: new(4)))
//							{
//								GUI.Draw9Slice(GUI.tex_panel, new(4), GUI.Rm);

//								using (GUI.Group.New2(size: GUI.Rm, padding: new(4, 4, 4, 4)))
//								{
//									using (GUI.Wrap.Push(GUI.RmX))
//									{
//										if (order_recipe.IsNotNull())
//										{
//											GUI.Text(order_recipe.desc);
//										}
//									}

//									GUI.NewLine(4);
//									GUI.Title("Requirements"u8, size: 16);
//									GUI.Separator();

//									//using (GUI.Group.New(size: new(GUI.RmX, 0.00f), padding: new Vector2(0, 4)))
//									//{
//									//	if (shipment.IsNotNull())
//									//	{
//									//		for (var i = 0; i < shipment.items.Length; i++)
//									//		{
//									//			ref var item = ref shipment.items[i];
//									//			using (GUI.ID<Crafting.Order, Shipment.Data>.Push(i))
//									//			{
//									//				using (GUI.Group.New(size: new Vector2(GUI.RmX, 48)))
//									//				{
//									//					var button_size = new Vector2(24 * 4, 32);

//									//					var bg_color = new Color32BGRA(0xffffffff);
//									//					GUI.DrawFillBackground(GUI.tex_panel, new(4));
//									//					//if (button.hovered) bg_color = bg_color.WithAlphaMult(1.00f);
//									//					//else bg_color = bg_color.WithAlphaMult(0.75f);

//									//					//GUI.Draw9Slice(GUI.tex_slot_simple, new(4, 4, 4, 4), button.bb, color: bg_color);

//									//					using (GUI.Group.New2(size: GUI.Rm, padding: new(4, 4, 4, 4)))
//									//					{
//									//						switch (item.type)
//									//						{
//									//							case Shipment.Item.Type.Resource:
//									//							{
//									//								ref var material = ref item.material.GetData();
//									//								if (material.IsNotNull())
//									//								{
//									//									var ok = item.quantity >= item.max;
//									//									has_resources &= ok;

//									//									var amount_available = 0.00f;
//									//									if (!ok)
//									//									{
//									//										if (Constants.Requirements.disable_requirements)
//									//										{
//									//											amount_available = item.max;
//									//										}
//									//										else
//									//										{
//									//											amount_available = context.GetMaterialCount(item.material);
//									//										}
//									//									}

//									//									var text = string.Empty;
//									//									var text_color = GUI.font_color_default;
//									//									var button_color = GUI.font_color_default_dark;

//									//									if (ok)
//									//									{
//									//										text = $"{item.quantity.Simplify():0.##}/{item.max.Simplify():0.##}";
//									//										text_color = GUI.font_color_green;
//									//									}
//									//									else
//									//									{
//									//										text = $"{item.quantity.Simplify():0.##}/{item.max.Simplify():0.##}";
//									//										text_color = GUI.font_color_red;

//									//										if (amount_available <= 0.00f)
//									//										{
//									//											button_color = GUI.col_button_error;
//									//										}
//									//										else if ((item.quantity + amount_available) < item.max)
//									//										{
//									//											button_color = GUI.col_button_yellow;
//									//										}
//									//										else
//									//										{
//									//											button_color = GUI.col_button_ok;
//									//										}
//									//									}

//									//									GUI.DrawMaterialSmall(item.material, new Vector2(GUI.RmY));

//									//									GUI.SameLine();

//									//									using (GUI.Group.New(size: GUI.Rm - new Vector2(40, 0), padding: new(4, 4)))
//									//									{
//									//										GUI.Title(material.name, color: GUI.font_color_default);
//									//										GUI.TextShaded(text, color: text_color);

//									//										//if (amount_available > 0.00f)
//									//										//{
//									//										//	GUI.SameLine();
//									//										//	GUI.TextShadedCentered($"+{amount_available.Simplify():0.##}", pivot: new Vector2(1, 0.00f), color: GUI.font_color_yellow);
//									//										//}
//									//									}

//									//									GUI.SameLine();

//									//									using (GUI.Group.New(size: GUI.Rm))
//									//									{

//									//										if (GUI.DrawSpriteButton("deposit"u8, "ui_button_deposit", new(40, 40), enabled: !ok && amount_available > Maths.epsilon, color: button_color))
//									//										{
//									//											var rpc = new Crafting.Order.DepositRPC()
//									//											{
//									//												s
//									//												index = (byte)i
//									//											};
//									//											rpc.Send(this.ent_upgradable);
//									//										}


//									//										//if (GUI.DrawIconButton(new Sprite("missing"), new(40, 40), enabled: !ok, color: button_color))
//									//										//{
//									//										//	var rpc = new Construction.DepositRPC()
//									//										//	{
//									//										//		index = i
//									//										//	};
//									//										//	rpc.Send(this.ent_construction);
//									//										//}

//									//										if (GUI.IsItemHovered())
//									//										{
//									//											using (GUI.Tooltip.New())
//									//											{
//									//												using (GUI.Wrap.Push(200))
//									//												{
//									//													GUI.Text("Fetch resources from nearby inventories."u8);

//									//													if (!ok)
//									//													{
//									//														GUI.NewLine(4);
//									//														GUI.Separator();
//									//														GUI.NewLine(4);

//									//														var text_color_b = GUI.font_color_red;

//									//														if (amount_available <= 0.00f)
//									//														{
//									//															text_color_b = GUI.font_color_red;
//									//														}
//									//														else if ((item.quantity + amount_available) < item.max)
//									//														{
//									//															text_color_b = GUI.font_color_yellow;
//									//														}
//									//														else
//									//														{
//									//															text_color_b = GUI.font_color_green;
//									//														}

//									//														if (Constants.Requirements.disable_requirements)
//									//														{
//									//															GUI.LabelShaded("Available:"u8, "Unlimited", color_b: text_color_b);
//									//														}
//									//														else
//									//														{
//									//															GUI.LabelShaded("Available:"u8, $"{amount_available.Simplify():0.##}/{(item.max - item.quantity).Simplify():0.##}", color_b: text_color_b);
//									//															//GUI.Label($"Available:", $"{amount_available:0.##}", color_b: text_color_b);
//									//															//GUI.Text($"{amount_available:0.##}", color: text_color_b);
//									//															//GUI.SameLine();
//									//															//GUI.TextShadedCentered($"-{(item.max - item.quantity):0.##}", pivot: new(1.00f, 0.00f), color: GUI.font_color_red);
//									//														}
//									//													}
//									//												}
//									//											}
//									//										}
//									//									}

//									//									//if (button.pressed)
//									//									//{
//									//									//	var rpc = new Shipment.DepositRPC()
//									//									//	{
//									//									//		amount = 20.00f,
//									//									//		index = (uint)i
//									//									//	};
//									//									//	rpc.Send(this.ent_construction);
//									//									//}
//									//								}
//									//							}
//									//							break;

//									//							case Shipment.Item.Type.Prefab:
//									//							{

//									//							}
//									//							break;
//									//						}
//									//					}
//									//					//GUI.Title(item.type.ToString());


//									//				}

//									//				GUI.NewLine(4);
//									//			}
//									//		}
//									//	}
//									//}


//								}
//							}

//							GUI.SameLine();

//							using (GUI.Group.New(size: GUI.Rm))
//							{
//								DrawWorkList(ref context, ref this.order, enabled: has_resources);

//								using (GUI.Group.New(size: new Vector2(GUI.RmX, 48)))
//								{
//									var button_color = GUI.col_button_ok; // new Color32BGRA(0xff9dd80f);
//																		  //if (GUI.DrawButton($"Stage: {this.construction.stage}", new(GUI.RmX, 48), enabled: this.construction.stage == Construction.Stage.Complete, color: button_color))
//																		  //{
//																		  //	var rpc = new Construction.CompleteRPC
//																		  //	{

//									//	};
//									//	rpc.Send(this.ent_construction);
//									//}
//								}

//								using (GUI.Group.New2(size: GUI.Rm, padding: new(6, 6, 8, 8)))
//								{
//									GUI.DrawFillBackground(GUI.tex_panel, new(4), margin: new(-4));

//									GUI.Title("Info"u8, size: 16);
//									GUI.Separator();

//									using (GUI.Group.New(size: GUI.Rm, padding: new(4)))
//									{
//										//if (!has_resources && this.construction.stage == Construction.Stage.Materials) GUI.Text("- Not enough resources!"u8, color: GUI.font_color_red);
//										//if (!has_worker && this.construction.stage == Construction.Stage.Work) GUI.Text("- No worker assigned!"u8, color: GUI.font_color_yellow);

//										//if (GUI.DrawButton("Complete", new Vector2(80, 48)))
//										//{
//										//	var rpc = new Construction.DebugRPC
//										//	{

//										//	};
//										//	rpc.Send(this.ent_construction);
//										//}
//									}
//								}
//							}
//						}
//					}
//				}
//			}

//			public static void DrawWorkList(ref Crafting.Context context, ref Crafting.Order order, bool enabled = true)
//			{
//				//var ent_pickup_target = context.ent_pickup.IsValid() ? context.ent_pickup.GetChild(Relation.Type.Child) : default;
//				//ref var worker = ref context.worker.data;

//				//for (var i = 0; i < order.work.Length; i++)
//				//{
//				//	ref var work = ref order.work[i];
//				//	using (GUI.ID<Crafting.Order>.Push(i))
//				//	{
//				//		using (GUI.Group.New(size: new Vector2(GUI.RmX, 32)))
//				//		{
//				//			var button_size = new Vector2(24 * 4, 32);

//				//			GUI.DrawWork(ref context.experience.data, work, new(GUI.RmX - button_size.X, GUI.RmY), 0xffff8000);
//				//			GUI.OffsetLine(GUI.RmX - button_size.X);

//				//			ref var work_data = ref work.type.GetData();
//				//			if (work_data.IsNotNull())
//				//			{
//				//				var button_color = enabled ? GUI.col_button_yellow : GUI.font_color_default_dark;

//				//				if (work.remaining <= 0.00f) // >= work.required)
//				//				{
//				//					if (GUI.DrawButton(Utf8String.Empty, button_size, enabled: false))
//				//					{

//				//					}
//				//				}
//				//				else if (worker.IsNotNull())
//				//				{
//				//					if (worker.work_index != i)
//				//					{
//				//						if (GUI.DrawButton("Work"u8, button_size, enabled: enabled && work.remaining > 0 && ent_pickup_target != context.ent_producer, color: button_color))
//				//						{
//				//							var rpc = new Worker.SetTargetRPC
//				//							{
//				//								ent_target = context.ent_producer,
//				//								h_order = IComponent.Handle.FromComponentPair<Upgradable.Data, Crafting.Order>(),
//				//								work_index = (byte)i,
//				//							};
//				//							rpc.Send(context.worker.entity);
//				//						}
//				//					}
//				//					else
//				//					{
//				//						if (GUI.DrawButton("Stop"u8, button_size, enabled: enabled && work.remaining > 0 && ent_pickup_target != context.ent_producer, color: button_color))
//				//						{
//				//							var rpc = new Worker.SetTargetRPC
//				//							{
//				//								ent_target = default
//				//							};
//				//							rpc.Send(context.worker.entity);
//				//						}
//				//					}
//				//				}
//				//				else
//				//				{
//				//					if (GUI.DrawButton("Work"u8, button_size, enabled: false, color: button_color))
//				//					{

//				//					}
//				//				}
//				//			}
//				//			else
//				//			{
//				//				if (GUI.DrawButton(Utf8String.Empty, button_size, enabled: false))
//				//				{

//				//				}
//				//			}
//				//		}
//				//	}
//				//}
			
//			}
//		}

//		[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global)]
//		public static void OnGUI(Entity ent_interactable, Entity ent_upgradable,
//		[Source.Owned] in Interactable.Data interactable, [Source.Owned] in Transform.Data transform,
//		[Source.Owned] in Upgradable.Data upgradable, [Source.Owned, Pair.Component<Upgradable.Data>] in Crafting.Order order)
//		{
//			if (interactable.IsActive())
//			{
//				var gui = new UpgradableGUI()
//				{
//					ent_interactable = ent_interactable,
//					ent_upgradable = ent_upgradable,

//					transform = transform,
//					upgradable = upgradable,
//					order = order

//					//axle = axle,
//					//axle_state = axle_state,
//				};
//				gui.Submit();
//			}
//		}
//#endif
//	}
//}
