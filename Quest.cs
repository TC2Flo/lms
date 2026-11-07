
//namespace TC2.Base.Components
//{
//	[Asset.Hjson(prefix: "quest.", capacity_world: 512, capacity_region: 256, capacity_local: 0)]
//	public interface IQuest: IAsset2<IQuest, IQuest.Data>
//	{
//		//static void IAsset2<IQuest, IQuest.Data>.OnInit(out string prefix, out string[] extensions, out int capacity_world, out int capacity_region, out int capacity_local)
//		//{
//		//	prefix = "quest.";
//		//	extensions = new string[]
//		//	{
//		//		".hjson"
//		//	};

//		//	capacity_world = 512;
//		//	capacity_region = 256;
//		//	capacity_local = 0;
//		//}

//		//static void IAsset2<IQuest, IQuest.Data>.OnUpdate(IQuest.Definition definition, ref IQuest.Data data_new)
//		//{

//		//}

//		[Serializable]
//		public struct Goal
//		{
//			public enum Type: uint
//			{
//				Undefined,

//				Craft,
//				Buy,
//				Sell,
//				Kill,
//				Gather,
//				Build,
//				Interact,
//				Destroy
//			}

//			public IQuest.Goal.Type type;
//			public Sprite icon_extra;

//			public string desc;

//			public Crafting.Requirement requirement;
//		}

//		[Serializable]
//		public struct Reward
//		{
//			public Crafting.Product product;
//		}

//		[Serializable]
//		public struct Data
//		{
//			public string name;
//			public string desc;
//			public string category;

//			[Save.NewLine]
//			public IQuest.Goal[] goals;

//			[Save.NewLine]
//			public IQuest.Reward[] rewards;
//		}
//	}

//	public static partial class QuestTracker
//	{
//		[IComponent.Data(Net.SendType.Reliable), IComponent.AddTo<Character.Data>()]
//		public partial struct Data: IComponent
//		{
//			public FixedArray4<QuestTracker.Meta> quests_meta;

//			public Data()
//			{

//			}
//		}

//		[Serializable]
//		public struct Meta: IComparable<Meta>, IEquatable<Meta>
//		{
//			public IQuest.Handle handle;
//			public FixedArray4<float> progress;

//			int IComparable<Meta>.CompareTo(Meta other)
//			{
//				return this.handle.id.CompareTo(other.handle.id);
//			}

//			bool IEquatable<Meta>.Equals(Meta other)
//			{
//				return this.handle.id.Equals(other.handle.id);
//			}
//		}

//		public struct SelectRPC: Net.IRPC<QuestTracker.Data>
//		{
//			public int index;
//			public IQuest.Handle handle;

//#if SERVER
//			public void Invoke(Net.IRPC.Context rpc, ref QuestTracker.Data data)
//			{
//				//IQuest.Database.SyncTest(this.handle);

//				if (this.index >= 0 && this.index < data.quests_meta.Length)
//				{
//					var sync = false;

//					ref var quest = ref this.handle.GetData();
//					if (!quest.IsNull())
//					{
//						for (var i = 0; i < data.quests_meta.Length; i++)
//						{
//							if (data.quests_meta[i].handle.id == this.handle.id)
//							{
//								return;
//							}
//						}
//					}
//					else
//					{
//						this.handle = default;
//					}

//					ref var meta = ref data.quests_meta[this.index];
//					meta = default;
//					meta.handle = this.handle;

//					sync = true;
//					//}
//					//else
//					//{

//					//}

//					if (sync)
//					{
//						data.Sync(rpc.entity);
//					}
//				}
//			}
//#endif
//		}

//		public struct CompleteRPC: Net.IRPC<QuestTracker.Data>
//		{
//			public int index;

//#if SERVER
//			public void Invoke(Net.IRPC.Context rpc, ref QuestTracker.Data data)
//			{
//				if (this.index >= 0 && this.index < data.quests_meta.Length)
//				{
//					var sync = false;

//					ref var meta = ref data.quests_meta[this.index];
//					ref var region = ref rpc.entity.GetRegion();

//					ref var quest = ref meta.handle.GetData();
//					if (!quest.IsNull())
//					{
//						var goals_span = quest.goals.AsSpan();
//						var done = true;

//						for (var i = 0; i < goals_span.Length; i++)
//						{
//							ref var goal = ref goals_span[i];
//							if (goal.requirement.type != Crafting.Requirement.Type.Undefined)
//							{
//								var progress = meta.progress[i];
//								done &= progress >= 1.00f;
//							}
//						}

//						if (done)
//						{
//							ref var player = ref rpc.connection.GetPlayerData();
//							ref var transform = ref player.ent_controlled.GetComponent<Transform.Data>();
//							if (!transform.IsNull())
//							{
//								var products = new FixedArray4<Crafting.Product>();
//								var products_count = 0;

//								foreach (ref var reward in quest.rewards.AsSpan())
//								{
//									if (reward.product.type != Crafting.Product.Type.Undefined)
//									{
//										products[products_count++] = reward.product;
//									}
//								}

//								Crafting.Context.NewFromConnection(ref rpc.connection, ent_producer: rpc.entity, out var context, search_radius: 0.00f); // ref region, ref player, entity, out var context);
//								//Crafting.Produce(ref player, transform.position, ref products);
//								context.Produce(products.AsSpan(), spawn_flags: Resource.SpawnFlags.Merge | Resource.SpawnFlags.Pickup | Resource.SpawnFlags.Allow_Encumbered);

//								meta = default;
//								sync = true;

//								Sound.PlayGUI(ref rpc.connection, "quest_complete2", volume: 0.15f, pitch: 0.90f);
//							}
//						}
//					}

//					if (sync)
//					{
//						data.Sync(rpc.entity);
//					}
//				}
//			}
//#endif
//		}

//#if SERVER
//		[ISystem.Event<Character.ShopEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
//		public static void OnShop(ISystem.Info info, Entity entity, ref Character.ShopEvent data, [Source.Owned] ref QuestTracker.Data tracker)
//		{
//			var sync = false;

//			ref var recipe = ref data.recipe.GetData();
//			if (!recipe.IsNull())
//			{
//				var quests_meta = tracker.quests_meta.AsSpan();
//				for (var i = 0; i < quests_meta.Length; i++)
//				{
//					ref var meta = ref quests_meta[i];

//					ref var quest = ref meta.handle.GetData();
//					if (!quest.IsNull())
//					{
//						ref var goals = ref quest.goals;
//						var goal_span = goals.AsSpan();

//						for (var j = 0; j < goal_span.Length; j++)
//						{
//							var progress_amount = 0.00f;

//							ref var goal = ref goal_span[j];
//							ref var requirement = ref goal.requirement;

//							if (goal.type == IQuest.Goal.Type.Buy && recipe.type == Recipe.Type.Buy)
//							{
//								var products_span = recipe.products.AsSpan();
//								foreach (ref var product in products_span)
//								{
//									switch (requirement.type)
//									{
//										case Crafting.Requirement.Type.Resource when product.type == Crafting.Product.Type.Resource && product.material.id == requirement.material.id:
//										{
//											progress_amount = Maths.Normalize01(product.amount, requirement.amount);
//										}
//										break;
//									}
//								}
//							}
//							else if (goal.type == IQuest.Goal.Type.Sell && recipe.type == Recipe.Type.Sell)
//							{
//								var recipe_requirements_span = recipe.requirements.AsSpan();
//								foreach (ref var recipe_requirement in recipe_requirements_span)
//								{
//									switch (requirement.type)
//									{
//										case Crafting.Requirement.Type.Resource when recipe_requirement.type == Crafting.Requirement.Type.Resource && recipe_requirement.material.id == requirement.material.id:
//										{
//											progress_amount = Maths.Normalize01(recipe_requirement.amount, requirement.amount);
//										}
//										break;
//									}
//								}
//							}

//							if (progress_amount != 0.00f)
//							{
//								meta.progress[j] = Maths.Clamp01(meta.progress[j] + progress_amount);
//								sync = true;

//								break;
//							}
//						}
//					}
//				}

//				if (sync)
//				{
//					tracker.Sync(entity);
//				}
//			}
//		}

//		[ISystem.Event<Character.GatherEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
//		public static void OnGather(ISystem.Info info, Entity entity, ref Character.GatherEvent data, [Source.Owned] ref QuestTracker.Data tracker)
//		{
//			//App.WriteLine($"gathered {data.resource.quantity}x {data.resource.material}");

//			var sync = false;

//			var quests_meta = tracker.quests_meta.AsSpan();
//			for (var i = 0; i < quests_meta.Length; i++)
//			{
//				ref var meta = ref quests_meta[i];

//				ref var quest = ref meta.handle.GetData();
//				if (!quest.IsNull())
//				{
//					ref var goals = ref quest.goals;
//					var goal_span = goals.AsSpan();

//					for (var j = 0; j < goal_span.Length; j++)
//					{
//						var progress_amount = 0.00f;

//						ref var goal = ref goal_span[j];
//						ref var requirement = ref goal.requirement;

//						if (goal.type == IQuest.Goal.Type.Gather)
//						{
//							switch (requirement.type)
//							{
//								case Crafting.Requirement.Type.Resource:
//								{
//									if (data.resource == requirement.material)
//									{
//										progress_amount = Maths.Normalize01(data.resource.quantity, requirement.amount);
//									}
//								}
//								break;
//							}

//							if (progress_amount != 0.00f)
//							{
//								meta.progress[j] = Maths.Clamp01(meta.progress[j] + progress_amount);
//								sync = true;

//								break;
//							}
//						}
//					}
//				}
//			}

//			if (sync)
//			{
//				tracker.Sync(entity);
//			}
//		}

//		[ISystem.Event<Character.BuildEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
//		public static void OnBuild(ISystem.Info info, Entity entity, ref Character.BuildEvent data, [Source.Owned] ref QuestTracker.Data tracker)
//		{
//			//App.WriteLine($"build {data.amount}x {data.block}");

//			var sync = false;

//			ref var recipe = ref data.recipe.GetData();
//			if (!recipe.IsNull())
//			{
//				var quests_meta = tracker.quests_meta.AsSpan();
//				for (var i = 0; i < quests_meta.Length; i++)
//				{
//					ref var meta = ref quests_meta[i];

//					ref var quest = ref meta.handle.GetData();
//					if (!quest.IsNull())
//					{
//						ref var goals = ref quest.goals;
//						var goal_span = goals.AsSpan();

//						for (var j = 0; j < goal_span.Length; j++)
//						{
//							var progress_amount = 0.00f;

//							ref var goal = ref goal_span[j];
//							ref var requirement = ref goal.requirement;

//							if (goal.type == IQuest.Goal.Type.Build)
//							{
//								switch (requirement.type)
//								{
//									case Crafting.Requirement.Type.Prefab:
//									{
//										if (data.prefab.id == requirement.prefab.id)
//										{
//											progress_amount = Maths.Normalize01(data.amount, requirement.amount);
//										}
//									}
//									break;

//									case Crafting.Requirement.Type.Block:
//									{
//										if (data.block.id == requirement.block.id)
//										{
//											progress_amount = Maths.Normalize01(data.amount, requirement.amount);
//										}
//									}
//									break;
//								}

//								if (progress_amount != 0.00f)
//								{
//									meta.progress[j] = Maths.Clamp01(meta.progress[j] + progress_amount);
//									sync = true;

//									break;
//								}
//							}
//						}
//					}
//				}

//				if (sync)
//				{
//					tracker.Sync(entity);
//				}
//			}
//		}

//		[ISystem.Event<Character.InteractEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
//		public static void OnInteract(ISystem.Info info, Entity entity, ref Character.InteractEvent data, [Source.Owned] ref QuestTracker.Data tracker)
//		{
//			var sync = false;

//			var quests_meta = tracker.quests_meta.AsSpan();
//			for (var i = 0; i < quests_meta.Length; i++)
//			{
//				ref var meta = ref quests_meta[i];

//				ref var quest = ref meta.handle.GetData();
//				if (!quest.IsNull())
//				{
//					ref var goals = ref quest.goals;
//					var goal_span = goals.AsSpan();

//					for (var j = 0; j < goal_span.Length; j++)
//					{
//						var progress_amount = 0.00f;

//						ref var goal = ref goal_span[j];
//						ref var requirement = ref goal.requirement;

//						if (goal.type == IQuest.Goal.Type.Interact)
//						{
//							if (data.ent_target.TryGetPrefabHandle(out var prefab_handle))
//							{
//								switch (requirement.type)
//								{
//									case Crafting.Requirement.Type.Prefab:
//									{
//										if (prefab_handle.id == requirement.prefab.id)
//										{
//											progress_amount = Maths.Normalize01(1, requirement.amount);
//										}
//									}
//									break;
//								}

//								if (progress_amount != 0.00f)
//								{
//									meta.progress[j] = Maths.Clamp01(meta.progress[j] + progress_amount);
//									sync = true;

//									break;
//								}
//							}
//						}
//					}
//				}
//			}

//			if (sync)
//			{
//				tracker.Sync(entity);
//			}
//		}

//		[ISystem.Event<Character.DestroyEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
//		public static void OnDestroy(ISystem.Info info, Entity entity, ref Character.DestroyEvent data, [Source.Owned] ref QuestTracker.Data tracker)
//		{
//			var sync = false;

//			var quests_meta = tracker.quests_meta.AsSpan();
//			for (var i = 0; i < quests_meta.Length; i++)
//			{
//				ref var meta = ref quests_meta[i];

//				ref var quest = ref meta.handle.GetData();
//				if (!quest.IsNull())
//				{
//					ref var goals = ref quest.goals;
//					var goal_span = goals.AsSpan();

//					for (var j = 0; j < goal_span.Length; j++)
//					{
//						var progress_amount = 0.00f;

//						ref var goal = ref goal_span[j];
//						ref var requirement = ref goal.requirement;

//						if (goal.type == IQuest.Goal.Type.Destroy)
//						{
//							if (data.ent_target.TryGetPrefabHandle(out var prefab_handle))
//							{
//								switch (requirement.type)
//								{
//									case Crafting.Requirement.Type.Prefab:
//									{
//										if (prefab_handle.id == requirement.prefab.id)
//										{
//											progress_amount = Maths.Normalize01(1, requirement.amount);
//										}
//									}
//									break;
//								}

//								if (progress_amount != 0.00f)
//								{
//									meta.progress[j] = Maths.Clamp01(meta.progress[j] + progress_amount);
//									sync = true;

//									break;
//								}
//							}
//						}
//					}
//				}
//			}

//			if (sync)
//			{
//				tracker.Sync(entity);
//			}
//		}
//#endif

//#if CLIENT
//		public struct TrackerGUI: IGUICommand
//		{
//			public Entity ent_tracker;

//			public QuestTracker.Data tracker;

//			public void Draw()
//			{
//				using (var window = GUI.Window.Standalone("QuestTracker"u8, position: new Vector2(GUI.CanvasSize.X - 16, 16), pivot: new Vector2(1, 0), size: new Vector2(320, 0), padding: new(4)))
//				{
//					this.StoreCurrentWindowTypeID(order: -100);
//					if (window.show)
//					{
//						ref var region = ref Client.GetRegionCommon();
//						ref var player = ref Client.GetPlayer();

//						GUI.DrawWindowBackground(GUI.tex_panel, new Vector4(4));

//						var quests_meta = this.tracker.quests_meta.AsSpan();
//						var available_index = default(int?);

//						using (GUI.Group.New(size: GUI.Rm, padding: new(4)))
//						{
//							GUI.Title("Quests", size: 32);

//							GUI.SameLine(8);

//							using (var dropdown = GUI.Dropdown.Begin("quests.select", "Available Quests...", new Vector2(GUI.RmX, 32), height: 300))
//							{
//								if (dropdown.show)
//								{
//									for (var i = 0; i < quests_meta.Length; i++)
//									{
//										ref var meta = ref quests_meta[i];
//										if (meta.handle.id == 0)
//										{
//											available_index = i;
//											break;
//										}
//									}

//									foreach (var def in IQuest.GetDefinitions())
//									{
//										if (def.id != 0)
//										{
//											using (GUI.ID.Push(def.id))
//											{
//												using (var group_row = GUI.Group.New(size: new Vector2(GUI.RmX, 40), padding: new Vector2(4)))
//												{
//													if (group_row.IsVisible())
//													{
//														ref var quest = ref def.data;

//														GUI.Title(quest.name);
//														GUI.TextShaded(quest.category, color: GUI.font_color_default.WithAlphaMult(0.60f));

//														var selected = false; // def.id == this.tracker.handle.id;
//														for (var i = 0; i < this.tracker.quests_meta.Length; i++)
//														{
//															if (this.tracker.quests_meta[i].handle.id == def.id)
//															{
//																selected = true;
//																break;
//															}
//														}

//														if (GUI.Selectable3("select", group_row.GetOuterRect(), selected: selected, enabled: !selected))
//														{
//															var rpc = new QuestTracker.SelectRPC()
//															{
//																handle = def.id,
//																index = available_index.Value
//															};
//															rpc.Send(this.ent_tracker);
//															dropdown.Close();
//														}
//														if (GUI.IsItemHovered())
//														{
//															using (var tooltip = GUI.Tooltip.New(size: new Vector2(200, 0)))
//															{
//																GUI.Title(quest.name, size: 24);

//																using (GUI.Group.New(size: new Vector2(GUI.RmX, 24), padding: new Vector2(4)))
//																{
//																	using (GUI.Wrap.Push(GUI.RmX))
//																	{
//																		GUI.TextShaded(quest.desc);
//																	}
//																}

//																GUI.Title("Rewards");
//																GUI.SeparatorThick();

//																//Shop.DrawProducts(ref region, this.ent_tracker, this.ent_tracker, default, quest.rewards.AsSpan(), 1);

//																var products = new FixedArray4<Crafting.Product>();
//																var products_count = 0;

//																foreach (ref var reward in quest.rewards.AsSpan())
//																{
//																	if (reward.product.type != Crafting.Product.Type.Undefined)
//																	{
//																		products[products_count++] = reward.product;
//																	}
//																}

//																//GUI.DrawProducts(ref region, this.ent_tracker, this.ent_tracker, default, products.AsSpan(), 1);
//															}
//														}
//													}
//												}
//											}
//										}
//									}
//								}
//							}

//							GUI.NewLine(4);
//							GUI.SeparatorThick();
//							GUI.NewLine(4);

//							for (var i = 0; i < quests_meta.Length; i++)
//							{
//								ref var meta = ref quests_meta[i];

//								ref var quest = ref meta.handle.GetData();
//								if (!quest.IsNull())
//								{
//									ref var goals = ref quest.goals;
//									var goals_span = goals.AsSpan();

//									using (GUI.ID.Push(100 + i))
//									{
//										using (var collapsible = GUI.Collapsible2.New(meta.handle.GetDefinition().Identifier, size: new Vector2(GUI.RmX, 48), default_open: true))
//										{
//											GUI.TitleCentered(quest.name, size: 24, pivot: new Vector2(0.00f, 0.25f));
//											GUI.TitleCentered(quest.category, size: 16, pivot: new Vector2(0.00f, 0.80f), color: GUI.font_color_default.WithAlphaMult(0.60f));

//											//GUI.FillRemainingSize(new Vector2(32, 32));
//											//GUI.SameLine();

//											//GUI.ResetLine(32);
//											//if (GUI.DrawConfirmButton("X", "test", new Vector2(32, 24)))
//											//{

//											//}

//											if (collapsible.Inner(padding: new Vector4(12, 0, 0, 0)))
//											{
//												using (GUI.Group.New(size: new Vector2(GUI.RmX, 0), padding: new Vector2(4)))
//												{
//													using (GUI.Wrap.Push(GUI.RmX))
//													{
//														GUI.TextShaded(quest.desc);
//													}
//												}

//												GUI.NewLine(4);

//												var done = true;

//												for (var j = 0; j < goals_span.Length; j++)
//												{
//													ref var goal = ref goals_span[j];
//													ref var requirement = ref goal.requirement;

//													if (requirement.type != Crafting.Requirement.Type.Undefined)
//													{
//														var progress = meta.progress[j];

//														if (j != 0)
//														{
//															GUI.NewLine(4);
//														}

//														using (GUI.ID.Push(1000 + j))
//														{
//															using (var group_row = GUI.Group.New2(size: new Vector2(GUI.RmX, 40), padding: new Vector4(4, 4, 12, 4)))
//															{
//																GUI.DrawBackground(GUI.tex_panel, group_row.GetOuterRect(), new Vector4(4));

//																GUI.TitleCentered(goal.type.ToStringUtf8(), size: 16, pivot: new Vector2(1.00f, 0.50f), color: GUI.font_color_title.WithAlphaMult(0.65f));

//																switch (requirement.type)
//																{
//																	case Crafting.Requirement.Type.Resource:
//																	{
//																		GUI.DrawResourceRequirement(requirement.material, requirement.amount * progress, requirement.amount, icon_overlay: goal.icon_extra);
//																	}
//																	break;

//																	case Crafting.Requirement.Type.Prefab:
//																	{
//																		GUI.DrawPrefabRequirement(requirement.prefab, (int)(requirement.amount * progress), (int)requirement.amount);
//																	}
//																	break;

//																	case Crafting.Requirement.Type.Block:
//																	{
//																		GUI.DrawBlockRequirement(requirement.block, requirement.amount * progress, requirement.amount);
//																	}
//																	break;
//																}
//															}
//														}

//														done &= progress >= 1.00f;
//													}
//												}

//												GUI.NewLine(4);

//												if (GUI.DrawButton("Complete", new Vector2(100, 40), enabled: done, color: GUI.col_button_ok))
//												{
//													var rpc = new QuestTracker.CompleteRPC()
//													{
//														index = i
//													};
//													rpc.Send(this.ent_tracker);
//												}
//												if (GUI.IsItemHovered())
//												{
//													using (var tooltip = GUI.Tooltip.New(size: new Vector2(200, 0)))
//													{
//														GUI.Title("Rewards");
//														GUI.SeparatorThick();

//														var products = new FixedArray4<Crafting.Product>();
//														var products_count = 0;

//														foreach (ref var reward in quest.rewards.AsSpan())
//														{
//															if (reward.product.type != Crafting.Product.Type.Undefined)
//															{
//																products[products_count++] = reward.product;
//															}
//														}

//														//GUI.DrawProducts(ref region, this.ent_tracker, this.ent_tracker, default, products.AsSpan(), 1);
//													}
//												}

//												GUI.SameLine();

//												if (GUI.DrawConfirmButton("confirm.abandon_quest", "Abandon", "Are you sure?", new Vector2(100, 40), color: GUI.col_button_error))
//												{
//													var rpc = new QuestTracker.SelectRPC()
//													{
//														index = i,
//														handle = default
//													};
//													rpc.Send(this.ent_tracker);
//												}
//												if (GUI.IsItemHovered())
//												{

//												}

//												GUI.NewLine(4);
//											}
//										}
//									}
//								}
//							}
//						}
//					}
//				}
//			}
//		}

//		[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("local", true, Source.Modifier.Shared)]
//		public static void OnGUI(ISystem.Info info, Entity entity, [Source.Shared] ref Player.Data player, [Source.Owned] in QuestTracker.Data tracker)
//		{
//			if (Constants.Questing.enable_quests)
//			{
//				var gui = new TrackerGUI()
//				{
//					ent_tracker = entity,

//					tracker = tracker
//				};
//				gui.Submit();
//			}
//		}
//#endif
//	}
//}
