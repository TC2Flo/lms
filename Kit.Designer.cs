
namespace TC2.Base.Components
{
	public static partial class KitDesigner
	{
		[IComponent.Data(Net.SendType.Unreliable)]
		public partial struct Data: IComponent
		{
			public FixedArray4<IKit.Handle> kits;
		}

		public struct NewRPC: Net.IRPC<KitDesigner.Data>
		{
			public byte kit_index;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref KitDesigner.Data data)
			{
				ref var region = ref rpc.entity.GetRegion();
				if (!region.IsNull())
				{
					var identifier = $"tmp.{rpc.entity.id}.{this.kit_index}".ToLowerInvariant();
					var def = IKit.Database.Register(identifier, null, Asset.Scope.Region, region_id: region.GetID());

					ref var tmp_kit = ref def.data;
					tmp_kit.flags |= Kit.Flags.Custom | Kit.Flags.Editable | Kit.Flags.Consume;

					IKit.Database.Update(def, ref tmp_kit);
					IKit.Database.SyncTest(def);

					data.kits[this.kit_index] = def.GetHandle();
					data.Sync(rpc.entity, true);
				}
			}
#endif
		}

		public struct CreateRPC: Net.IRPC<KitDesigner.Data>
		{
			public byte kit_index;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref KitDesigner.Data data)
			{
				ref var region = ref rpc.entity.GetRegion();
				ref var player = ref rpc.connection.GetPlayer();
				ref var transform = ref rpc.entity.GetComponent<Transform.Data>();

				var tmp_kit_handle = data.kits[this.kit_index];

				ref var tmp_kit = ref tmp_kit_handle.GetData();
				if (!transform.IsNull() && tmp_kit.IsNotNull() && tmp_kit.flags.HasAny(Kit.Flags.Editable))
				{
					var copy_kit = tmp_kit;
					copy_kit.flags.SetFlag(Kit.Flags.Editable, false);
					copy_kit.flags.SetFlag(Kit.Flags.Custom | Kit.Flags.Selectable | Kit.Flags.Consume, true);

					var identifier = Asset.GenerateRandomIdentifier();
					var def = IKit.Database.Register(identifier, null, Asset.Scope.World, region_id: 0);

					IKit.Database.Update(def, ref copy_kit);
					IKit.Database.SyncTest(def);

					data.Sync(rpc.entity, true);
				}
			}
#endif
		}

		public struct EditRPC: Net.IRPC<KitDesigner.Data>
		{
			public byte kit_index;

			public FixedString32? name;
			public Kit.Slot? slot;
			public NPC.ItemHints? categories;
			public ISpecies.Handle? species;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref KitDesigner.Data data)
			{
				ref var region = ref rpc.connection.GetRegion();
				Assert.Check(region.IsNotNull(), "Region is null.", Assert.Level.Error);

				ref var player = ref rpc.connection.GetPlayer();
				Assert.Check(player.IsNotNull(), "Player is null.", Assert.Level.Error);

				ref var transform = ref rpc.entity.GetComponent<Transform.Data>();
				Assert.Check(transform.IsNotNull(), "Transform is null.", Assert.Level.Error);

				var h_kit = data.kits[this.kit_index];

				ref var kit_data = ref h_kit.GetData();
				Assert.Check(kit_data.IsNotNull(), "Kit data is null.", Assert.Level.Error);
				Assert.Check(kit_data.flags.HasAny(Kit.Flags.Editable), "Kit is not editable.", Assert.Level.Error);

				if (name.TryGetValue(out var v_name))
				{
					kit_data.name = v_name;
				}

				if (this.slot.TryCopyTo(ref kit_data.slot))
				{
					kit_data.shipment.items = default;

					ref var items = ref kit_data.shipment.items;

					switch (kit_data.slot)
					{
						case Kit.Slot.Primary:
						case Kit.Slot.Secondary:
						{
							items[0].type = Shipment.Item.Type.Prefab;
							items[0].max = 1;
							//items[0].item_filter = NPC.ItemHints.Weapon;

							items[1].type = Shipment.Item.Type.Resource;
							//items[1].item_filter = NPC.ItemHints.Materials;
							//items[1].material_filter = Material.Flags.Ammo;
						}
						break;

						case Kit.Slot.Tool:
						{
							items[0].type = Shipment.Item.Type.Prefab;
							items[0].max = 1;
							//items[0].item_filter = NPC.ItemHints.Tools;
						}
						break;

						case Kit.Slot.Utility:
						{
							items[0].type = Shipment.Item.Type.Prefab;
							items[0].max = 1;
							//items[0].item_filter = NPC.ItemHints.None;

							items[1].type = Shipment.Item.Type.Resource;
							//items[1].item_filter = NPC.ItemHints.None;
							//items[1].material_filter = Material.Flags.None;
						}
						break;

						case Kit.Slot.Resources:
						{
							items[0].type = Shipment.Item.Type.Resource;
							//items[0].item_filter = NPC.ItemHints.Materials;
							//items[0].material_filter = Material.Flags.None;

							items[1].type = Shipment.Item.Type.Resource;
							//items[1].item_filter = NPC.ItemHints.Materials;
							//items[1].material_filter = Material.Flags.None;

							items[2].type = Shipment.Item.Type.Resource;
							//items[2].item_filter = NPC.ItemHints.Materials;
							//items[2].material_filter = Material.Flags.None;

							items[3].type = Shipment.Item.Type.Resource;
							//items[3].item_filter = NPC.ItemHints.Materials;
							//items[3].material_filter = Material.Flags.None;
						}
						break;

						case Kit.Slot.Head:
						case Kit.Slot.Chest:
						{
							items[0].type = Shipment.Item.Type.Prefab;
							items[0].max = 1;
							//items[0].item_filter = NPC.ItemHints.Armor;
						}
						break;

						case Kit.Slot.Vehicle:
						{
							items[0].type = Shipment.Item.Type.Prefab;
							items[0].max = 1;
							//items[0].item_filter = NPC.ItemHints.Vehicle;

							items[1].type = Shipment.Item.Type.Resource;
							//items[1].material_filter = Material.Flags.Fuel;
						}
						break;

						default:
						{

						}
						break;
					}
				}

				this.categories.TryCopyTo(ref kit_data.categories);
				this.species.TryCopyTo(ref kit_data.species);

				h_kit.Sync(false);

				//data.Sync(entity, true);
			}
#endif
		}

		public struct EditItemRPC: Net.IRPC<KitDesigner.Data>
		{
			public byte kit_index;
			public byte item_index;

			public Shipment.Item item;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref KitDesigner.Data data)
			{
				ref var region = ref rpc.connection.GetRegion();
				Assert.Check(region.IsNotNull(), Assert.Level.Error);

				ref var player = ref rpc.connection.GetPlayer();
				Assert.Check(player.IsNotNull(), Assert.Level.Error);

				ref var transform = ref rpc.entity.GetComponent<Transform.Data>();
				Assert.Check(transform.IsNotNull(), Assert.Level.Error);

				var h_kit = data.kits[this.kit_index];

				ref var kit_data = ref h_kit.GetData();
				Assert.Check(kit_data.IsNotNull(), Assert.Level.Error);
				Assert.Check(kit_data.flags.HasAny(Kit.Flags.Editable), Assert.Level.Error);

				ref var item_tmp = ref kit_data.shipment.items[this.item_index];
				//if (this.item.type == Shipment.Item.Type.Undefined)
				//{
				//	item_tmp.material = default;
				//	item_tmp.quantity = default;
				//}
				//else if (item_tmp.Deposit(ref this.item))
				//{
				//	App.WriteLine("deposit ok");
				//}
				//else
				//{
				//	App.WriteLine("deposit fail", App.Color.Yellow);
				//}

				switch (item_tmp.type)
				{
					case Shipment.Item.Type.Prefab:
					{
						item_tmp.prefab = item.prefab;
						item_tmp.quantity = item_tmp.max > Maths.epsilon ? Maths.Clamp(item.quantity, 0.00f, item_tmp.max) : item.quantity;
					}
					break;

					case Shipment.Item.Type.Resource:
					{
						item_tmp.material = item.material;
						item_tmp.quantity = item_tmp.max > Maths.epsilon ? Maths.Clamp(item.quantity, 0.00f, item_tmp.max) : item.quantity;
					}
					break;

					//case Shipment.Item.Type.Money:
					//{
					//	item_tmp.quantity = item_tmp.max > Maths.epsilon ? Maths.Clamp(item.quantity, 0.00f, item_tmp.max) : item.quantity;
					//}
					//break;

					default:
					{
						item_tmp.material = default;
						item_tmp.quantity = default;
						//Assert.Throw($"Item type {this.item.type} is not supported!", Assert.Level.Warn);
					}
					break;
				}

				//switch (item_tmp.type)
				//{
				//	case Shipment.Item.Type.Prefab:
				//	{
				//		item_tmp.type = Shipment.Item.Type.Prefab;
				//		item_tmp.prefab = item.prefab;
				//		item_tmp.quantity = item.quantity;
				//		item_tmp.max = item.max;
				//	}
				//	break;

				//	case Shipment.Item.Type.Resource:
				//	{
				//		item_tmp.type = Shipment.Item.Type.Resource;
				//		item_tmp.material = item.material;
				//		item_tmp.quantity = item.quantity;
				//		item_tmp.max = item.max;
				//	}
				//	break;

				//	case Shipment.Item.Type.Money:
				//	{
				//		item_tmp.type = Shipment.Item.Type.Money;
				//		item_tmp.quantity = item.quantity;
				//		item_tmp.max = item.max;
				//	}
				//	break;

				//	default:
				//	{
				//		item_tmp = default;
				//		Assert.Throw($"Item type {this.item.type} is not supported!", Assert.Level.Warn);
				//	}
				//	break;
				//}

				h_kit.Sync(false);

				//data.Sync(entity, true);
			}
#endif
		}

#if CLIENT
		public struct KitGUI: IGUICommand
		{
			public Entity ent_kit_designer;
			public Transform.Data transform;

			public IKit.Handle h_kit;
			public KitDesigner.Data kit_designer;

			internal static byte current_kit_index;

			public void Draw()
			{
				ref var player = ref Client.GetPlayer();
				ref var region = ref Client.GetPlayer();

				using (var window = GUI.Window.Interaction("Kit Designer"u8, this.ent_kit_designer))
				{
					this.StoreCurrentWindowTypeID();
					if (window.show)
					{
						GUI.DrawWindowBackground();

						ref readonly var mouse = ref GUI.GetMouse();
						ref var kit = ref this.h_kit.GetData();
						var context = GUI.ItemContext.Begin();

						using (var group_top = GUI.Group.New(size: new(GUI.RmX, 40)))
						{
							for (var i = 0; i < this.kit_designer.kits.Length; i++)
							{
								ref var h_kit = ref this.kit_designer.kits[i];
								ref var data_blueprint = ref h_kit.GetData(out var def_kit);

								if (GUI.DrawButton($"{i}", size: new(40, 40), color: data_blueprint.IsNotNull() ? GUI.col_button_ok : GUI.col_button.WithAlphaMult(i == current_kit_index ? 1.00f : 0.50f)))
								{
									current_kit_index = (byte)i;

									if (!h_kit.IsValid())
									{
										var rpc = new KitDesigner.NewRPC()
										{
											kit_index = (byte)i
										};
										rpc.Send(this.ent_kit_designer);
									}
								}
								GUI.SameLine();
							}

							if (kit.IsNotNull())
							{
								GUI.TitleCentered(kit.name, size: 24, pivot: new(0.00f, 0.50f), offset: new(12, 0));
							}
							else
							{
								GUI.TitleCentered("<no kit selected>"u8, size: 24, pivot: new(0.00f, 0.50f), offset: new(4, 0), color: GUI.font_color_title.WithAlphaMult(0.50f));
							}
						}

						//GUI.NewLine();

						GUI.SeparatorThick();

						if (kit.IsNotNull())
						{
							using (var group_main = GUI.Group.New(size: GUI.GetRemainingSpace(y: -80 - 2), padding: new(0)))
							{
								group_main.DrawBackground(GUI.tex_panel);

								var kit_tmp = kit;
								var dirty = false;

								var edit_rpc = new KitDesigner.EditRPC
								{
									kit_index = current_kit_index
								};

								if (GUI.TextInput("kit.name", "<name>", ref kit.name, size: new Vector2(GUI.RmX - 100, 40), max_length: 30, return_only_submit: true))
								{
									if (!string.IsNullOrEmpty(kit.name) && !kit.name.Equals(kit_tmp.name, StringComparison.OrdinalIgnoreCase))
									{
										dirty |= true;
										edit_rpc.name = kit.name;
									}
									//App.WriteLine(kit_tmp.name);
								}
								GUI.DrawHoverTooltip("Display name");

								GUI.SameLine();

								if (GUI.AssetInput("kit.species", ref kit.species, size: new Vector2(GUI.RmX, 40), show_label: false))
								{
									dirty |= true;
									edit_rpc.species = kit.species;
								}
								GUI.DrawHoverTooltip("Species");

								if (GUI.EnumInput("kit.categories", ref kit.categories, size: new Vector2(GUI.RmX - 140, 40), show_label: false))
								{
									dirty |= true;
									edit_rpc.categories = kit.categories;
								}
								GUI.DrawHoverTooltip("Categories");

								GUI.SameLine();

								if (GUI.EnumInput("kit.slot", ref kit.slot, size: new Vector2(GUI.RmX, 40), show_label: false))
								{
									dirty |= true;
									edit_rpc.slot = kit.slot;
								}
								GUI.DrawHoverTooltip("Loadout slot");

								if (dirty)
								{
									edit_rpc.Send(this.ent_kit_designer);
									App.WriteLine(dirty);
								}
							}

							GUI.SeparatorThick();

							using (var group_bottom = GUI.Group.New(size: GUI.Rm, padding: new(0)))
							{
								group_bottom.DrawBackground(GUI.tex_panel);

								ref var shipment = ref kit.shipment;
								var shipment_tmp = shipment;

								var dirty = false;

								var edit_rpc = new KitDesigner.EditItemRPC
								{
									kit_index = current_kit_index
								};

								var item_count_max = shipment_tmp.items.Length;
								var items = shipment_tmp.items.AsSpan();

								switch (kit.slot)
								{
									case Kit.Slot.Primary:
									case Kit.Slot.Secondary:
									{
										item_count_max = 2;
									}
									break;

									case Kit.Slot.Tool:
									{
										item_count_max = 1;
									}
									break;

									case Kit.Slot.Utility:
									{
										item_count_max = 2;
									}
									break;

									case Kit.Slot.Resources:
									{
										item_count_max = 4;
									}
									break;

									case Kit.Slot.Head:
									case Kit.Slot.Chest:
									case Kit.Slot.Vehicle:
									{
										item_count_max = 1;
									}
									break;

									default:
									{
										item_count_max = 0;
									}
									break;
								}

								if (item_count_max > 0)
								{
									var slot_w = GUI.RmX / 4; // item_count_max;

									for (var i = 0; i < item_count_max; i++)
									{
										ref var item = ref shipment_tmp.items[i];
										using (var slot = GUI.ItemSlot.New(ref context, id: (uint)(i + 100), item, new(slot_w, 80), show_quantity: item.max != 1, item_color: Color32BGRA.White, item_scale: 3.00f))
										{
											if (slot.result.TryGetValue(out var slot_result))
											{
												App.WriteLine($"{slot_result.type}; {slot_result.item_slot.type} {slot_result.item_slot.quantity:0.00}");

												edit_rpc.item_index = (byte)i;
												edit_rpc.item = slot_result.item_slot;

												dirty = true;
											}
										}

										GUI.SameLine();
									}
								}

								if (dirty)
								{
									edit_rpc.Send(this.ent_kit_designer);
									App.WriteLine(dirty);
								}
							}

						}
						else
						{
							GUI.TitleCentered("No kit selected"u8, size: 24);
						}
					}
				}
			}
		}

		[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnGUI(Entity entity, [Source.Owned] in KitDesigner.Data kit_designer, [Source.Owned] in Interactable.Data interactable, [Source.Owned] in Transform.Data transform)
		{
			if (interactable.IsActive())
			{
				var gui = new KitGUI()
				{
					ent_kit_designer = entity,
					transform = transform,
					kit_designer = kit_designer,
				};

				var h_kit = kit_designer.kits[KitGUI.current_kit_index];
				gui.h_kit = h_kit;

				gui.Submit();
			}
		}
#endif
	}
}
