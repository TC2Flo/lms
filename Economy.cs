using TC2.Conquest;

namespace TC2.Base.Components
{
	//[Asset.Hjson(prefix: "bank.", capacity_world: 512, capacity_region: 128, capacity_local: 0, order: -30)]
	//public interface IBankAccount: IAsset2<IBankAccount, IBankAccount.Data>
	//{
	//	[Flags]
	//	public enum Flags: uint
	//	{
	//		None = 0,
	//	}

	//	[Serializable]
	//	public struct Data
	//	{
	//		[Save.Force]
	//		public string name;

	//		public Data()
	//		{

	//		}
	//	}
	//}



	//	public struct ConfigureRPC: Net.IRPC<Constabulary.Data>
	//	{

	//#if SERVER
	//			public void Invoke(Net.IRPC.Context rpc, ref Constabulary.Data data)
	//			{

	//			}
	//#endif
	//	}


	//	public static partial class Vault
	//	{
	//		[IComponent.Data(Net.SendType.Reliable)]
	//		public partial struct Data: IComponent
	//		{
	//			public double money;
	//			public double money_capacity;
	//		}

	//		public struct TransferRPC: Net.IRPC<Vault.Data>
	//		{
	//#if SERVER
	//			public void Invoke(Net.IRPC.Context rpc, ref Vault.Data data)
	//			{

	//			}
	//#endif
	//		}
	//	}

	public static partial class City
	{
		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Global, sync_table_capacity: 32)]
		public partial struct Data: IComponent
		{

		}
	}

	public static partial class Location
	{
		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Global | IComponent.Scope.Region, sync_table_capacity: 128)]
		public partial struct Data: IComponent
		{
			public ILocation.Handle h_location;
		}

		[ISystem.Event<ILocation.RefreshEvent>(ISystem.Mode.Single, ISystem.Scope.Global | ISystem.Scope.Region)]
		public static void OnRefreshAssetEvent(ISystem.Info.Common info, ref Region.Data.Common region_common, Entity ent_location, ref ILocation.RefreshEvent ev,
		[Source.Owned] ref Location.Data location,
		[Source.Owned, Optional(true)] ref Nameable.Data nameable,
		[Source.Owned, Optional(true)] ref Faction.Data faction,
		[Source.Owned, Optional(true)] ref Company.Data company)
		{
			//App.WriteLine($"[{region_common.GetID()}] Location.OnRefreshAssetEvent({ent_location}) {ev.h_asset} ({ev.h_asset.GetName()})", App.Color.Magenta);

#if SERVER
			if (nameable.IsNotNull() && nameable.flags.HasAny(Nameable.Flags.Use_Asset_Name))
			{
				nameable.name = ev.data.name_short;
				nameable.type = Nameable.Kind.Location;
				nameable.flags = Nameable.Flags.No_Export | Nameable.Flags.No_Rename;
				nameable.Sync(ent_location, true);
			}

			if (faction.IsNotNull() && ev.data.h_faction.IsValid())
			{
				faction.id = ev.data.h_faction;
				faction.Sync(ent_location, true);
			}

			if (company.IsNotNull() && ev.data.h_company.IsValid())
			{
				company.h_company = ev.data.h_company;
				company.Sync(ent_location, true);
			}

			//if (address.IsNotNull())
			//{
			//	address.Sync(ent_location, true);
			//}
#endif
		}

		[ISystem.Monitor(ISystem.Mode.Single, ISystem.Scope.Global | ISystem.Scope.Region), HasTag("asset", true, Source.Modifier.Owned)]
		public static void OnAssetLocationParent(ISystem.Info.Common info, Entity entity, Entity ent_location,
		[Source.Parent] in Location.Data location)
		{
			if (entity.TryGetAsset(out var asset) && asset.Scope <= Asset.Scope.World)
			{
				switch (info.EventType)
				{
					case ISystem.EventType.Add:
					{
						asset.RegionID = location.h_location.GetRegionID();
					}
					break;

					case ISystem.EventType.Remove:
					{
						asset.RegionID = 0;
					}
					break;
				}
			}
			//App.WriteLine($"OnAssetLocationParent({entity}, {ent_location}, {asset?.Identifier} at {location.h_location}): scope: {asset?.Scope}; region_id: {asset?.RegionID}; {info.EventType}", App.Color.Cyan);
			//App.WriteLine($"OnAssetModified({entity}): {entity.GetKind()}", App.Color.Cyan);
		}

#if SERVER
		[ISystem.AddFirst(ISystem.Mode.Single, ISystem.Scope.Global, order: -500)]
		[ISystem.Modified.Component<Location.Data>(ISystem.Mode.Single, ISystem.Scope.Global, order: -500)]
		public static void OnAdd(Entity entity, 
		[Source.Owned] ref Location.Data location, [Source.Owned] ref Transform.Data transform)
		{
			if (ILocation.TryGetAsset(entity, out var h_location))
			{
				ref var location_data = ref h_location.GetData();
				if (location_data.IsNotNull())
				{
					location.h_location = h_location;
					if (transform.position.IsZero()) // TODO: hack (so it doesn't reset its position on savefile load, e.g. in case of zeppelins)
					{
						transform.SetPosition(location_data.point);
					}

					ref var location_parent_data = ref location_data.h_location_parent.GetData(out var location_parent_asset);
					if (location_parent_data.IsNotNull())
					{
						var ent_parent = location_parent_asset.GetGlobalEntity();
						if (ent_parent.IsAlive())
						{
							entity.AddRelation(ent_parent, Relation.Type.Child, true);
						}
					}

					location.Sync(entity, true);
					transform.Sync(entity, true);
				}
			}
			else
			{

			}

			//App.WriteLine($"OnAdd: {h_location}; {entity}");
		}

		//[ISystem.AddFirst(ISystem.Mode.Single, ISystem.Scope.Region, defer: false, order: -500)]
		//public static void OnAddRegion(ISystem.Info info, ref Region.Data region, Entity entity, [Source.Owned] ref Location.Data location)
		//{
		//	//App.WriteLine("OnAddRegion");

		//	ref var map_info = ref region.GetMapInfo();
		//	if (map_info.IsNotNull())
		//	{
		//		location.h_location = map_info.h_location;
		//		location.Sync(entity, true);
		//	}

		//	//App.WriteLine($"OnAdd: {h_location}; {entity}");
		//}
#endif


		//		public struct DEV_TestRPC: Net.IRPC<Location.Data>
		//		{
		//			public uint val;

		//#if SERVER
		//			public void Invoke(Net.IRPC.Context rpc, ref Location.Data data)
		//			{
		//				App.WriteLine($"hello {this.val}; {entity}; {data.h_location}");
		//			}
		//#endif
		//		}
	}

	public static partial class AddressExt
	{
		public struct EditAddressRPC: Net.IRPC<Address.Data>
		{
			public FixedString16? name;
			public Color32BGRA? color_a;
			public Color32BGRA? color_b;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Address.Data data)
			{
				Assert.Check(data.flags.HasNone(Address.Data.Flags.No_Editing));

				ref var address_data = ref data.h_address.GetData(out var address_asset);
				Assert.IsNotNull(ref address_data);

				var h_faction_client = rpc.connection.GetFactionHandle();
				var h_faction_entity = rpc.entity.GetFactionHandle();

				Assert.Check(h_faction_entity.IsOwnedOrNeutral(h_faction_client));

				var dirty = false;
				if (this.name.TryGetValue(out var v_name) && !v_name.IsEmpty())
				{
					address_data.name = v_name;
					dirty = true;
				}

				dirty |= this.color_a.TryCopyTo(ref address_data.color_a);
				dirty |= this.color_b.TryCopyTo(ref address_data.color_b);

				if (dirty)
				{
					address_asset.Sync();
				}
			}
#endif
		}

		public struct NewAddressRPC: Net.IRPC<Address.Data>
		{
			public FixedString16 name;
			public ushort code;
			public Color32BGRA color_a;
			public Color32BGRA color_b;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Address.Data data)
			{
				Assert.Check(!data.h_address.Exists());
				Assert.Check(data.flags.HasNone(Address.Data.Flags.No_Editing | Address.Data.Flags.Auto_Generate));

				var h_faction_client = rpc.connection.GetFactionHandle();
				var h_faction_entity = rpc.entity.GetFactionHandle();

				Assert.Check(h_faction_entity.IsOwnedOrNeutral(h_faction_client));

				var code = (ushort)(rpc.entity.GetShortID() % 65000); // TODO: this sucks
				var address_asset = Address.Register(rpc.entity, code, this.name, this.color_a, this.color_b);
				Assert.IsNotNull(address_asset);

				data.code = code;
				data.h_address = address_asset;
				data.Modified(rpc.entity, true);
			}
#endif
		}

#if CLIENT
		public partial struct AddressGUI: IGUICommand
		{
			public Entity ent_address;

			public Transform.Data transform;
			public Address.Data address;
			public IFaction.Handle h_faction;

			public static AddressExt.EditAddressRPC edit_rpc;
			public static AddressExt.NewAddressRPC new_rpc;

			public void Draw()
			{
				using (var window = GUI.Window.InteractionMisc("Address"u8, this.ent_address, size: new(48, 48), min_width: 48.00f, min_height: 48.00f))
				{
					this.StoreCurrentWindowTypeID();
					if (window.show)
					{
						var h_faction_client = Client.GetFactionHandle();
						var no_editing = this.address.flags.HasAny(Address.Data.Flags.No_Editing);
						var can_manage = !no_editing && this.h_faction.IsOwnedOrNeutral(h_faction_client);
						var has_address = this.address.h_address.IsValid();

						//if (GUI.DrawButton("Edit Address"u8, size: new(GUI.AvX, GUI.RmY), enabled: can_manage))
						if (GUI.DrawIconButton("button.address"u8, sprite: new Sprite(GUI.tex_icons_widget, 16, 16, 9, 2), size: GUI.Rm, enabled: can_manage, color: has_address ? GUI.col_edit : GUI.col_new))
						{
							GUI.OpenPopup("popup.address"u8);
							if (has_address)
							{
								edit_rpc = new EditAddressRPC();
							}
							else
							{
								new_rpc = new NewAddressRPC()
								{
									name = this.ent_address.GetName(),
									color_a = Color32BGRA.Neutral,
									color_b = Color32BGRA.Neutral
								};
							}
						}
						GUI.DrawHoverTooltip(has_address ? "Edit postal address."u8 : "Register postal address."u8);

						if (can_manage)
						{
							using (var popup = GUI.Popup.New("popup.address"u8, size: new(212, 0)))
							{
								if (popup.show)
								{
									ref var address_data = ref this.address.h_address.GetData(out var address_asset);
									if (address_data.IsNotNull())
									{
										if (GUI.TextInput("edit.name"u8, "<name>"u8, address_data.name, ref edit_rpc.name, size: new(GUI.RmX, 32)))
										{

										}

										if (GUI.DrawColorPicker("Color (A)"u8, in address_data.color_a, ref edit_rpc.color_a))
										{

										}

										GUI.SameLine();

										if (GUI.DrawColorPicker("Color (B)"u8, in address_data.color_b, ref edit_rpc.color_b))
										{

										}

										GUI.NewLine(0);
										GUI.SameLine(GUI.RmX * 0.50f);
										if (GUI.DrawButton("Apply"u8, size: new(GUI.RmX, 40), color: GUI.col_button_ok))
										{
											edit_rpc.SendAsTask(this.ent_address).WaitForRender().ContinueWith((x) =>
											{
												Notification.Push("Sucessfully applied changes.", color: GUI.font_color_success, sound: "ui.copy", volume: 0.20f, pitch: 1.10f);
												edit_rpc = default;
											});
										}
									}
									else
									{
										if (GUI.TextInput("edit.name"u8, "<name>"u8, ref new_rpc.name, size: new(GUI.RmX, 32)))
										{

										}

										if (GUI.DrawColorPicker("Color (A)"u8, ref new_rpc.color_a))
										{

										}

										GUI.SameLine();

										if (GUI.DrawColorPicker("Color (B)"u8, ref new_rpc.color_b))
										{

										}

										GUI.NewLine(0);
										GUI.SameLine(GUI.RmX * 0.50f);

										var valid = new_rpc.name.GetLength() > 3;
										if (GUI.DrawButton("Submit"u8, size: new(GUI.RmX, 40), enabled: valid, color: GUI.col_button_ok))
										{
											new_rpc.SendAsTask(this.ent_address).WaitForRender().ContinueWith((x) =>
											{
												Notification.Push("Sucessfully created postal address.", color: GUI.font_color_success, sound: "ui.copy", volume: 0.20f, pitch: 1.10f);
												new_rpc = default;
											});

											//popup.Close();
										}
									}
								}
							}
						}
					}
				}
			}
		}

		[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Global | ISystem.Scope.Region)]
		public static void OnGUI(/*ISystem.Info.Common info, */Entity ent_address,
		[Source.Owned] in Interactable.Data interactable, [Source.Owned] in Transform.Data transform,
		[Source.Owned] in Address.Data address, [Source.Owned, Optional] in Faction.Data faction)
		{
			if (interactable.IsActive() && address.flags.HasNone(Address.Data.Flags.No_GUI))
			{
				var gui = new AddressGUI()
				{
					ent_address = ent_address,

					transform = transform,
					address = address,

					h_faction = faction.id
				};
				gui.Submit();
			}
		}
#endif
	}

	//[Asset.Hjson(prefix: "economy.", capacity_world: 128, capacity_region: 64, capacity_local: 8, order: 10)]
	//public interface IEconomy: IAsset2<IEconomy, IEconomy.Data>
	//{
	//	[Flags]
	//	public enum Flags: uint
	//	{
	//		None = 0,
	//	}

	//	[Flags]
	//	public enum Tags: uint
	//	{
	//		None = 0,
	//	}

	//	static void IAsset2<IEconomy, IEconomy.Data>.OnRefresh(IEconomy.Definition definition)
	//	{
	//		ref var data = ref definition.GetData();
	//	}

	//	public struct Entry
	//	{
	//		public double amount;
	//	}

	//	public struct Data()
	//	{
	//		public Dictionary<Shipment.Item2.Header, IEconomy.Entry>
	//	}
	//}


	public static partial class Economy
	{
		//[IComponent.AddTo<Settlement.Data>]
		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Global | IComponent.Scope.Region)]
		public partial struct Data: IComponent
		{
			public float test;
		}

		//public static Texture.Handle tex_icons_depot = "ui.icons.stockpile";


		//public static ref Shipment.Item GetItem(ref this IStockpile.Data stockpile, in IStockpile.SlotID id)
		//{
		//	var index = id.Index;
		//	if (id.Index < stockpile.items.Length && index.Type == Stockpile.SlotType.Item)
		//	{
		//		return ref stockpile.items[index.Index]
		//	} 
		//}


		[Shitcode]
		public struct EditLedgerRPC: Net.IRPC<Stockpile.Data>
		{
			public uint derp;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Stockpile.Data data)
			{

			}
#endif
		}

		public struct LinkRPC: Net.IRPC<Stockpile.Data>
		{
			public IStockpile.Handle h_stockpile;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Stockpile.Data data)
			{
				data.h_stockpile = this.h_stockpile;
				data.Sync(rpc.entity);
			}
#endif
		}

		public struct EditSlotRPC: Net.IRPC<Stockpile.Data>
		{
			public enum Action: byte
			{
				Undefined = 0,

				Add,
				Rem
			}

			public IAddress.Handle h_address;

			public IStockpile.SlotID slot;
			public EditSlotRPC.Action action;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Stockpile.Data data)
			{
				Assert.Check(this.action != Action.Undefined);
				Assert.Check(this.slot.IsValid());

				ref var stockpile_data = ref data.h_stockpile.GetData(out var stockpile_asset);
				Assert.IsNotNull(ref stockpile_data);

				Assert.Check(stockpile_data.IsSlotValid(this.slot));

				var slot_type = this.slot.Type;
				var addresses = stockpile_data.addresses;

				if (data.flags.HasAny(Stockpile.Data.Flags.Full_Access))
				{

				}
				else
				{
					// TODO: add a check if current entity has the same address
				}

				//Assert.Check(slot_type

				var dirty = false;

				switch (this.action)
				{
					case EditSlotRPC.Action.Add:
					{
						Assert.Check(this.h_address.IsValid());
						if (addresses.TryGetValue(this.slot.raw, out var h_address_other))
						{
							//Assert.Check()
						}
						else
						{
							addresses[this.slot.raw] = this.h_address;
							dirty = true;
						}
						//stockpile_data.
					}
					break;

					case EditSlotRPC.Action.Rem:
					{
						Assert.Check(this.h_address.IsValid());
						if (addresses.ContainsPair(this.slot.raw, this.h_address))
						{
							dirty |= addresses.Remove(this.slot.raw);
						}
					}
					break;
				}

				if (dirty)
				{
					stockpile_asset.Sync();
				}
			}
#endif
		}

		[Shitcode]
		public struct RequestRPC: Net.IRPC<Stockpile.Data>
		{
			public Stockpile.Request request_base;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Stockpile.Data data)
			{
				ref var stockpile_data = ref data.h_stockpile.GetData();
				Assert.IsNotNull(ref stockpile_data);

				Assert.Check(data.request_types.Has(this.request_base.type));

				var random = XorRandom.New(true);

				switch (this.request_base.type)
				{
					case Stockpile.Request.Type.Pack:
					{
						ref var request = ref this.request_base.AsRequestType<Stockpile.Request.Pack>();
						Assert.Check(request.shipment_index_dst.Index < stockpile_data.shipments.Length);

						var items_src_tmp = new FixedArray4<Shipment.Item>();
						items_src_tmp.AsSpan().PopulateIndexed(stockpile_data.items, request.item_indices_src);


						//var shipment_items = shipment.items.AsSpan();
						//shipment_items.PopulateIndexed(stockpile_data.items.AsSpan(), request.item_indices_src.AsSpan());

						Assert.Check(stockpile_data.shipments.AsSpan().TryGetAtIndex(request.shipment_index_dst.Index, out var shipment_dst));

						if (shipment_dst.IsValid())
						{
							var shipment_dst_items = shipment_dst.items.AsSpan();
							for (var i = 0; i < items_src_tmp.Length; i++)
							{
								ref var item_src = ref items_src_tmp[i];
								ref var item_dst = ref shipment_dst_items[i];
								if ((item_src.type | item_dst.type) == 0) continue;

								Assert.Check(item_src == item_dst);
								Assert.Check(item_src.quantity >= item_dst.quantity * request.count);
								Assert.Check(item_dst.Deposit(ref item_src, amount: item_dst.quantity * request.count));
							}
							stockpile_data.shipments[request.shipment_index_dst.Index].count += request.count;
						}
						else
						{
							shipment_dst = new Shipment.Data(Shipment.Flags.Keep_Items);
							shipment_dst.h_container = request.h_container;
							shipment_dst.count = request.count;

							var shipment_dst_items = shipment_dst.items.AsSpan();
							for (var i = 0; i < items_src_tmp.Length; i++)
							{
								ref var item_src = ref items_src_tmp[i];
								ref var item_dst = ref shipment_dst_items[i];
								if ((item_src.type | item_dst.type) == 0) continue;

								Assert.Check(item_src.quantity >= request.item_amounts_src[i] * request.count);
								Assert.Check(item_dst.Deposit(ref item_src, amount: request.item_amounts_src[i] * request.count));
								shipment_dst_items[i].quantity = request.item_amounts_src[i];
							}
							stockpile_data.shipments[request.shipment_index_dst.Index] = shipment_dst;
						}

						for (var i = 0; i < items_src_tmp.Length; i++)
						{
							if (!request.item_indices_src[i].IsValid()) continue;
							stockpile_data.items[request.item_indices_src[i].Index] = items_src_tmp[i];
						}

						//{
						//	for (var i = 0; i < shipment.items.Length; i++)
						//	{
						//		var slot_id = request.item_indices_src[i];
						//		var index = slot_id.Index;
						//		if (index >= stockpile_data.items.Length || !slot_id.IsValid()) continue;

						//		ref var item_src = ref stockpile_data.items[index];
						//		var amount = Math.Min(request.item_amounts_src[i], 50000u);

						//		ref var item_dst = ref shipment.items[i];

						//		item_dst.Deposit(ref item_src, amount: amount);
						//	}
						//}

						//stockpile_data.shipments[request.shipment_index_dst.Index] = shipment;
					}
					break;

					case Stockpile.Request.Type.Unpack:
					{
						ref var request = ref this.request_base.AsRequestType<Stockpile.Request.Unpack>();
						Assert.Check(request.shipment_index_src.Index < stockpile_data.shipments.Length);

						var ok = true;

						var shipment_copy = stockpile_data.shipments[request.shipment_index_src.Index];
						Assert.Check(shipment_copy.count != 0);
						Assert.Check(shipment_copy.count >= request.count);

						var items_all = stockpile_data.items.AsSpan();
						Span<Shipment.Item> items_src = shipment_copy.items.AsSpan();
						Span<Shipment.Item> items_dst = stackalloc Shipment.Item[items_src.Length];

						for (var i = 0; i < items_dst.Length; i++)
						{
							if (!request.item_indices_dst[i].IsValid()) continue;

							var index = request.item_indices_dst[i].Index;
							if (index < items_all.Length)
							{
								Assert.Check(index < items_all.Length);
								Assert.Check(items_all[index] == items_src[i] || !items_all[index].IsValid());

								items_dst[i] = items_all[index];
							}
						}

						for (var i = 0; i < items_src.Length; i++)
						{
							ref var item_src = ref items_src[i];
							if (!item_src.IsValid()) continue;

							item_src.quantity *= request.count;
							var found = false;

							var index = 0;
							for (var j = 0; j < items_dst.Length; j++)
							{
								if (request.item_indices_dst[j].Index >= items_all.Length) continue;

								if (items_dst[j] == item_src)
								{
									index = j;
									found = true;
									break;
								}
								else if (!found && items_dst[j].type == Shipment.Item.Type.Undefined)
								{
									index = j;
									found = true;
								}
							}

							if (found)
							{
								ref var item_dst = ref items_dst[index];
								//App.WriteLine($"{item_src.material} => {item_dst.material} ({index})");

								ok &= item_src.Merge(ref item_dst);
							}
							else
							{
								////App.WriteLine($"not found {item_src.material} ({index})", App.Color.Yellow);
								Assert.Throw($"{item_src.material} ({index}) not found!", Assert.Level.Error);
								//ok = false;
								//break;
							}
						}

						//App.WriteLine($"ok: {ok}");
						if (ok)
						{
							for (var i = 0; i < items_dst.Length; i++)
							{
								if (!request.item_indices_dst[i].IsValid()) continue;

								var index = request.item_indices_dst[i].Index;
								if (index < items_all.Length)
								{
									items_all[index] = items_dst[i];
								}
							}

							ref var shipment = ref stockpile_data.shipments[request.shipment_index_src.Index];
							shipment.count -= request.count;

							if (shipment.count == 0)
							{
								shipment = default;
							}
						}
					}
					break;

					case Stockpile.Request.Type.Move:
					{
						ref var request = ref this.request_base.AsRequestType<Stockpile.Request.Move>();

						var slot_type = request.slot_src.Type;

						Assert.Check(slot_type != Stockpile.SlotType.Undefined);
						Assert.Check(request.slot_src.IsValid());
						//Assert.Check(request.slot_dst.IsValid(slot_type));
						Assert.Check(request.slot_src != request.slot_dst);
						Assert.Check(request.amount > 0);

						switch (slot_type)
						{
							case Stockpile.SlotType.Item:
							{
								ref var item_src = ref stockpile_data.items.GetRefAtIndexOrNull(request.slot_src.Index);
								ref var item_dst = ref stockpile_data.items.GetRefAtIndexOrNull(request.slot_dst.Index);

								Assert.Check(item_src == item_dst || !item_dst.IsValidRef());

								App.WriteLine($"{request.slot_src}, {item_src.IsNotNull()}; {request.slot_dst}, {item_dst.IsNotNull()}");

								if (item_src.IsNotNull() && item_dst.IsNotNull() && item_src.IsValid())
								{
									var count = Maths.Clamp((int)request.amount, 1, (int)item_src.quantity);

									if (item_dst.IsValid() && item_dst == item_src)
									{
										item_dst.quantity += (ushort)count;
										item_src.quantity -= (ushort)count;
									}
									else
									{
										item_dst = item_src with { quantity = (ushort)count };
										item_src.quantity -= (ushort)count;
									}

									if (!item_src.IsValid() || item_src.quantity == 0)
									{
										item_src = default;
									}
								}
							}
							break;

							case Stockpile.SlotType.Shipment:
							{
								ref var item_src = ref stockpile_data.shipments.GetRefAtIndexOrNull(request.slot_src.Index);
								ref var item_dst = ref stockpile_data.shipments.GetRefAtIndexOrNull(request.slot_dst.Index);

								Assert.Check(item_src == item_dst || !item_dst.IsValid());

								//App.WriteLine($"{request.slot_src}, {item_src.IsNotNull()}; {request.slot_dst}, {item_dst.IsNotNull()}");

								if (item_src.IsNotNull() && item_dst.IsNotNull() && item_src.IsValid())
								{
									var count = Maths.Clamp((int)request.amount, 1, (int)item_src.count);

									if (item_dst.IsValid() && item_dst == item_src)
									{
										item_dst.count += (ushort)count;
										item_src.count -= (ushort)count;
									}
									else
									{
										item_dst = item_src with { count = (ushort)count };
										item_src.count -= (ushort)count;
									}

									if (!item_src.IsValid() || item_src.count == 0)
									{
										item_src = default;
									}
								}
							}
							break;

							//case Stockpile.SlotType.Character:
							//{
							//	ref var item_src = ref stockpile_data.characters.GetRefAtIndexOrNull(request.slot_src.Index);
							//	ref var item_dst = ref stockpile_data.characters.GetRefAtIndexOrNull(request.slot_dst.Index);

							//	Assert.Check(item_src.IsValid());
							//	Assert.Check(!item_dst.IsValid());

							//	App.WriteLine($"{request.slot_src}, {item_src.IsNotNull()}; {request.slot_dst}, {item_dst.IsNotNull()}");

							//	item_dst = item_src;
							//	item_src = default;
							//}
							//break;
						}
					}
					break;

					case Stockpile.Request.Type.Send:
					{
						ref var request = ref this.request_base.AsRequestType<Stockpile.Request.Send>();
						ref var region = ref rpc.entity.GetRegionCommon();

						Assert.Check(region.IsNotNull());
						Assert.Check(request.h_stockpile_src == data.h_stockpile);
						Assert.Check(request.shipment_index_src.IsValid(Stockpile.SlotType.Shipment));
						Assert.Check(request.shipment_index_src.Index < stockpile_data.shipments.Length);

						ref var stockpile_src = ref request.h_stockpile_src.GetData(out var s_stockpile_src);
						ref var stockpile_dst = ref request.h_stockpile_dst.GetData(out var s_stockpile_dst);

						if (Assert.IsNotNull(ref stockpile_src) && Assert.IsNotNull(ref stockpile_dst))
						{
							ref var shipment_src = ref stockpile_src.shipments.AsSpan().GetRefAtIndexOrNull(request.shipment_index_src.Index);
							ref var shipment_dst = ref stockpile_dst.shipments.AsSpan().GetRefOfValueOrEmpty(in shipment_src, out var index_dst, out var found);

							var count = Maths.Clamp((int)request.count, 1, (int)shipment_src.count);

							if (shipment_dst.IsValid() && shipment_src == shipment_dst)
							{
								shipment_dst.count += (ushort)count;
								shipment_src.count -= (ushort)count;
							}
							else
							{
								shipment_dst = shipment_src with { count = (ushort)count };
								shipment_src.count -= (ushort)count;
							}

							if (!shipment_src.IsValid() || shipment_src.count == 0)
							{
								shipment_src = default;
							}

							s_stockpile_src.Sync(false);
							s_stockpile_dst.Sync(false);
						}
					}
					break;

					// TODO: add position validation
					case Stockpile.Request.Type.Load:
					{
						ref var request = ref this.request_base.AsRequestType<Stockpile.Request.Load>();
						ref var region = ref rpc.entity.GetRegion();

						Assert.Check(region.IsNotNull());
						Assert.Check(request.slot_index.IsValid());
						Assert.Check(stockpile_data.IsSlotValid(request.slot_index));

						switch (request.slot_type)
						{
							case Stockpile.SlotType.Item:
							{
								ref var resource_target = ref request.entity.GetComponent<Resource.Data>();
								if (resource_target.IsNotNull())
								{
									var item_tmp = Shipment.Item.Resource(resource_target.material, resource_target.quantity);

									ref var shipment_dst = ref stockpile_data.items[request.slot_index.Index];
									if (!shipment_dst.IsValid())
									{
										shipment_dst = item_tmp;
										request.entity.Delete();
									}
									else if (shipment_dst == item_tmp)
									{
										shipment_dst.quantity += item_tmp.quantity; // .count = Math.Max((ushort)(shipment_dst.count + Math.Max(resource_target.count, (ushort)1)), (ushort)1);
										request.entity.Delete();
									}
									else
									{

									}

									//App.WriteLine("hi");
								}
							}
							break;

							case Stockpile.SlotType.Shipment:
							{
								App.WriteValue((request.entity, request.slot_index, request.pos, request.slot_type));

								ref var shipment_target = ref request.entity.GetComponent<Shipment.Data>();
								if (shipment_target.IsNotNull())
								{
									ref var shipment_dst = ref stockpile_data.shipments[request.slot_index.Index];
									if (!shipment_dst.IsValid())
									{
										shipment_dst = shipment_target;
										shipment_dst.count = Maths.Max(shipment_target.count, (ushort)1);
										request.entity.Delete();
									}
									else if (shipment_dst == shipment_target)
									{
										shipment_dst.count = Maths.Max((ushort)(shipment_dst.count + Maths.Max(shipment_target.count, (ushort)1)), (ushort)1);
										request.entity.Delete();
									}
									else
									{

									}

									//App.WriteLine("hi");
								}
								else
								{
									// TODO: add overlap check
									Assert.Check(request.pos != default);

									ref var shipment_src = ref stockpile_data.shipments[request.slot_index.Index];
									Assert.Check(shipment_src.IsValid());
									Assert.Check(shipment_src.count > 0);

									Shipment.Spawn(ref region, request.pos, items: shipment_src.items.AsSpan(), h_container: shipment_src.h_container, flags: shipment_src.flags);
									shipment_src.count--;
									if (shipment_src.count == 0)
									{
										shipment_src = default;
									}
								}
							}
							break;
						}

						//Assert.Check(request.slot_index == Stockpile.SlotType.Shipment);
						//Assert.Check(request.slot_src.IsValid());
						//Assert.Check(request.slot_src != request.slot_dst);
						//Assert.Check(request.amount > 0);

					}
					break;
				}

				data.h_stockpile.Sync(false);
			}
#endif
		}

		public struct DEV_RefillRPC: Net.IRPC<Stockpile.Data>
		{
			public int count;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Stockpile.Data data)
			{
				Assert.IsDevMode();
				Assert.IsAdmin(ref rpc.connection);

				ref var stockpile_data = ref data.h_stockpile.GetData(out var stockpile_asset);
				Assert.IsNotNull(ref stockpile_data);

				var dirty = false;

				for (var i = 0; i < this.count; i++)
				{

				}

				if (dirty)
				{
					stockpile_asset.Sync();
				}
			}
#endif
		}

#if SERVER
		[ISystem.AddFirst(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global, order: 100)]
		public static void OnAdd(Entity entity, ISystem.Info.Common info, [Source.Owned] ref Stockpile.Data stockpile)
		{
			//App.WriteLine($"add stockpile {stockpile.h_stockpile.id}");
			if (!stockpile.h_stockpile.IsValid() && stockpile.flags.HasAnyExcept(Stockpile.Data.Flags.Auto_Create, Stockpile.Data.Flags.Initialized))
			{
				var stockpile_data = new IStockpile.Data();
				if (stockpile.capacity_items > 0) stockpile_data.items = new Shipment.Item[stockpile.capacity_items];
				if (stockpile.capacity_shipments > 0) stockpile_data.shipments = new Shipment.Data[stockpile.capacity_shipments];
				//if (stockpile.capacity_characters > 0) stockpile_data.characters = new ICharacter.Handle[stockpile.capacity_characters];

				stockpile_data.name = entity.GetName();

				var identifier = Asset.GenerateRandomIdentifier();
				var identifier_entity = entity.GetAsset()?.Identifier ?? entity.GetIdentifier(); // TODO: kinda hacky
				if (identifier_entity.IsNotNullOrWhiteSpace()) identifier = $"{identifier_entity}.{identifier}";

				var stockpile_asset = IStockpile.Database.RegisterOrUpdate(identifier, null, Asset.Scope.World, entity.GetRegionID(), ref stockpile_data, flags: Asset.Flags.Recycle);
				stockpile_asset.Sync(true);

				stockpile.flags.AddFlag(Stockpile.Data.Flags.Initialized);
				stockpile.h_stockpile = stockpile_asset;
				stockpile.Modified(entity, true);
			}
		}

		[ISystem.Modified.Component<Entrance.Linkable.Data>(ISystem.Mode.Single, ISystem.Scope.Region, Defer = true)]
		public static void OnModifiedEntranceLink(ISystem.Info info, ref Region.Data region, 
		Entity ent_stockpile,
		[Source.Owned] ref Stockpile.Data stockpile,
		[Source.Owned] ref Entrance.Linkable.Data linkable)
		{
			//App.WriteLine($"OnModifiedEntranceLink: {linkable.h_entrance}; {ent_stockpile}", App.Color.Magenta);

			if (!stockpile.h_stockpile)
			{
				var ent_entrance_global = linkable.h_entrance.GetGlobalEntity();
				if (ent_entrance_global.IsAlive())
				{
					ref var stockpile_entrance = ref ent_entrance_global.GetComponent<Stockpile.Data>();
					if (stockpile_entrance.IsNotNull())
					{
						stockpile.h_stockpile = stockpile_entrance.h_stockpile;
						stockpile.Modified(ent_stockpile, true);
					}
				}
			}
		}

		//[ISystem.Modified<Address.Data>(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global, order: 100, Defer = true), HasTag("initialized", true, Source.Modifier.Owned)]
		//public static void OnModifiedAddress(ISystem.Info.Common info, ref Region.Data.Common region_common, Entity entity,
		//[Source.Owned] ref Entrance.Linkable.Data linkable,
		//[Source.Owned] ref Address.Data address)
		//{
		//	if (address.h_address.id == 0)
		//	{
		//		var address_data = new IAddress.Data();

		//		//ref var region = ref region_common.AsRegion();
		//		//if (region.IsNotNull())
		//		//{
		//		//	address_data.h_location = region.GetLocationHandle();
		//		//}
		//		//else
		//		//{
		//		//	entity.TryGetAssetHandle(out address_data.h_location);
		//		//}

		//		//ref var location_data = ref address_data.h_location.GetData();
		//		//if (location_data.IsNotNull())
		//		//{
		//		//	address_data.h_prefecture = location_data.h_prefecture;
		//		//}

		//		//stockpile_data.name = entity.GetName();

		//		var identifier = Asset.GenerateRandomIdentifier();
		//		var address_asset = IAddress.Database.RegisterOrUpdate(identifier, null, Asset.Scope.World, 0, ref address_data, flags: Asset.Flags.Recycle);
		//		//address_asset.Sync(true);

		//		address.h_address = address_asset;
		//		address.Modified(entity, true);

		//		App.WriteLine($"new address {address.h_address.id}", App.Color.Magenta);
		//	}
		//}

		//[ISystem.Modified<Stockpile.Data>(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global, order: 110)]
		//public static void OnFactionModified(Entity entity, ISystem.Info.Common info, [Source.Owned] ref Stockpile.Data stockpile, [Source.Owned] ref Faction.Data faction)
		//{
		//	//App.WriteLine($"add faction {faction.id}");
		//	ref var stockpile_data = ref stockpile.h_stockpile.GetData(out var stockpile_asset);
		//	if (stockpile_data.IsNotNull())
		//	{
		//		stockpile_data.h_faction = faction.id;

		//		stockpile_asset.Sync(false);
		//	}
		//}

		//[ISystem.Modified<Stockpile.Data>(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global, order: 120)]
		//public static void OnLocationModified(Entity entity, ISystem.Info.Common info, [Source.Owned] ref Stockpile.Data stockpile, [Source.Owned] ref Location.Data location)
		//{
		//	//App.WriteLine($"add location {location.h_location}");
		//	ref var stockpile_data = ref stockpile.h_stockpile.GetData(out var stockpile_asset);
		//	if (stockpile_data.IsNotNull())
		//	{
		//		stockpile_data.h_location = location.h_location;
		//		//stockpile_asset.entity.AddRelation(location.h_location.GetEntity(), Relation.Type.Child);

		//		stockpile_asset.Sync(false);
		//	}
		//}

		//[ISystem.Modified<Stockpile.Data>(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global, order: 140)]
		//public static void OnCompanyModified(Entity entity, ISystem.Info.Common info, [Source.Owned] ref Stockpile.Data stockpile, [Source.Owned] ref Company.Data company)
		//{
		//	//App.WriteLine($"add company {company.h_company}");
		//	ref var stockpile_data = ref stockpile.h_stockpile.GetData(out var stockpile_asset);
		//	if (stockpile_data.IsNotNull())
		//	{
		//		stockpile_data.h_company_provider = company.h_company;
		//		stockpile_data.h_company = company.h_company;

		//		stockpile_asset.Sync(false);
		//	}
		//}

		[ISystem.RemoveLast(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global)]
		public static void OnRemove(Entity entity, [Source.Owned] ref Stockpile.Data stockpile) //, [Source.Owned, Optional(true)] ref Market.Data market)
		{
			if (stockpile.flags.HasAny(Stockpile.Data.Flags.Auto_Create))
			{
				ref var stockpile_data = ref stockpile.h_stockpile.GetData(out var stockpile_asset);
				if (stockpile_data.IsNotNull())
				{
					//if (market.IsNotNull() && market.h_stockpile == stockpile_asset)
					//{
					//	market.h_stockpile = default;
					//	market.Sync(entity, true);
					//}

					stockpile_asset.Recycle();

					stockpile.h_stockpile = default;
					stockpile.Sync(entity, true);
				}
			}
		}
#endif


		//#if SERVER
		//		[ChatCommand.Region("bank", "Creates a bank account.", admin: true)]
		//		public static void BankCommand(ref ChatCommand.Context context, string name)
		//		{
		//			var identifier = Asset.GenerateRandomIdentifier();

		//			var d_bank = new IBankAccount.Data();
		//			d_bank.name = name;

		//			var s_bank = IBankAccount.Database.RegisterOrUpdate(identifier, null, Asset.Scope.World, 0, ref d_bank);
		//			s_bank.Sync(true);
		//		}

		//		[ChatCommand.Region("stockpile", "Creates a stockpile.", admin: true)]
		//		public static void StockpileCommand(ref ChatCommand.Context context, string name)
		//		{
		//			var identifier = Asset.GenerateRandomIdentifier();

		//			var d_stockpile = new IStockpile.Data();
		//			d_stockpile.name = name;
		//			d_stockpile.items = new Shipment.Item[32];
		//			d_stockpile.characters = new ICharacter.Handle[32];

		//			var s_stockpile = IStockpile.Database.RegisterOrUpdate(identifier, null, Asset.Scope.World, 0, ref d_stockpile);
		//			s_stockpile.Sync(true);
		//		}
		//#endif

		public static bool ContainsAnyIndexed<T, TIndex>(this HashSet<TIndex> indices, Span<T> index_to_value, T value) where TIndex : unmanaged, IConvertible where T : struct, IEquatable<T>
		{
			foreach (var entry in indices)
			{
				var index = entry.ToInt32(null);
				if (index_to_value[index].Equals(value)) return true;
			}
			return false;
		}

		public static bool ContainsAnyIndexed<T, TIndex>(this List<TIndex> indices, Span<T> index_to_value, T value) where TIndex : unmanaged, IConvertible where T : struct, IEquatable<T>
		{
			for (var i = 0; i < indices.Count; i++)
			{
				var index = indices[i].ToInt32(null);
				if (index_to_value[index].Equals(value)) return true;
			}
			return false;
		}

		public static bool ContainsAnyIndexed<T, TIndex>(this Span<TIndex> indices, Span<T> index_to_value, T value) where TIndex : unmanaged, IConvertible where T : struct, IEquatable<T>
		{
			for (var i = 0; i < indices.Length; i++)
			{
				var index = indices[i].ToInt32(null);
				if (index_to_value[index].Equals(value)) return true;
			}
			return false;
		}

		//[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
		public static void PopulateIndexed<T>(this Span<T> span_dst, Span<T> span_values, Span<IStockpile.SlotID> span_indices) where T : unmanaged
		{
			for (var i = 0; i < span_dst.Length; i++)
			{
				if (!span_indices[i].IsValid()) continue;
				span_dst[i] = span_values[span_indices[i].Index];
			}
		}

		//public static Prefab.Handle[] shipment_types =
		//[
		//	"shipment.00",
		//	"shipment.01",
		//	"shipment.02",
		//	"shipment.03",
		//	"shipment.04"
		//];

		public ref struct StockpileFilterSlot
		{
			public readonly ref IStockpile.Data stockpile;
			public IStockpile.SlotID slot_id;

			public readonly ref IStockpile.SlotID? slot_id_preview;
			public readonly ref byte slot_id_preview_subindex;

			public readonly ref int amount;
			public readonly ref int amount_add;
			public readonly ref int arg_index;

			public readonly ref bool enabled;
			public readonly ref bool valid;
			public readonly ref bool selected;
			public readonly ref bool preview;

			public StockpileFilterSlot(ref IStockpile.Data stockpile, IStockpile.SlotID slot,
				ref IStockpile.SlotID? slot_id_preview, ref byte slot_id_preview_subindex,
				ref int amount, ref int amount_add, ref int arg_index,
				ref bool enabled, ref bool valid, ref bool selected, ref bool preview)
			{
				this.stockpile = ref stockpile;
				this.slot_id = slot;

				this.slot_id_preview = ref slot_id_preview;
				this.slot_id_preview_subindex = ref slot_id_preview_subindex;

				this.amount = ref amount;
				this.amount_add = ref amount_add;
				this.arg_index = ref arg_index;

				this.enabled = ref enabled;
				this.valid = ref valid;
				this.selected = ref selected;
				this.preview = ref preview;
			}
		}
		public delegate void StockpileFilterDelegate(ref Stockpile.Request request, StockpileFilterSlot args);

#if CLIENT
		public ref struct StockpileGUISlot
		{
			public readonly ref IStockpile.Data stockpile;
			public IStockpile.SlotID slot_id;

			public readonly ref IStockpile.SlotID? slot_id_preview;
			public readonly ref byte slot_id_preview_subindex;

			public readonly ref int amount;
			public readonly ref int amount_add;
			public readonly ref int arg_index;

			public readonly ref bool enabled;
			public readonly ref bool valid;
			public readonly ref bool selected;
			public readonly ref bool preview;

			public readonly ref bool hovered;
			public readonly ref bool selectable;
			public readonly ref bool pressed;

			public readonly ref Color32BGRA color_frame;
			public readonly ref Color32BGRA color_item;

			public readonly ref readonly AABB rect;

			public StockpileGUISlot(ref IStockpile.Data stockpile, IStockpile.SlotID slot,
				ref IStockpile.SlotID? slot_id_preview, ref byte slot_id_preview_subindex,
				ref int amount, ref int amount_add, ref int arg_index,
				ref bool enabled, ref bool valid, ref bool selected, ref bool preview,
				ref bool hovered, ref bool selectable, ref bool pressed,
				ref Color32BGRA color_frame, ref Color32BGRA color_item,
				ref AABB rect)
			{
				this.stockpile = ref stockpile;
				this.slot_id = slot;

				this.slot_id_preview = ref slot_id_preview;
				this.slot_id_preview_subindex = ref slot_id_preview_subindex;

				this.amount = ref amount;
				this.amount_add = ref amount_add;
				this.arg_index = ref arg_index;

				this.enabled = ref enabled;
				this.valid = ref valid;
				this.selected = ref selected;
				this.preview = ref preview;

				this.hovered = ref hovered;
				this.selectable = ref selectable;
				this.pressed = ref pressed;

				this.color_frame = ref color_frame;
				this.color_item = ref color_item;

				this.rect = ref rect;
			}
		}
		public delegate void StockpileGUIDelegate(ref Stockpile.Request request, StockpileGUISlot args);


		public ref struct StockpileGUISkippedSlot
		{
			public readonly ref IStockpile.Data stockpile;
			public IStockpile.SlotID slot_id;

			public readonly IAddress.Handle h_address_slot;
			public readonly Vector2 cell_size;

			public StockpileGUISkippedSlot(ref IStockpile.Data stockpile, IStockpile.SlotID slot,
			IAddress.Handle h_address_slot,
			Vector2 cell_size)
			{
				this.stockpile = ref stockpile;
				this.slot_id = slot;

				this.h_address_slot = h_address_slot;

				this.cell_size = cell_size;
			}
		}
		public delegate void StockpileGUISkippedDelegate(ref GUI.Grid grid, IAddress.Handle h_address, Stockpile.Data.Flags flags, StockpileGUISkippedSlot args);
#endif

		public static int GetQuantity(ref this IStockpile.Data stockpile_data, IStockpile.SlotID slot)
		{
			var type = slot.Type;
			switch (type)
			{
				case Stockpile.SlotType.Item:
				{
					var arr = stockpile_data.items;
					return (int)arr[slot.Index].quantity;
				}
				break;

				case Stockpile.SlotType.Shipment:
				{
					var arr = stockpile_data.shipments;
					return arr[slot.Index].count;
				}
				break;

				//case Stockpile.SlotType.Character:
				//{
				//	var arr = stockpile_data.characters;
				//	return arr[slot.Index].IsValid().AsInt();
				//}
				//break;

				default:
				{
					return 0;
				}
			}
		}

		public static bool CanMove(ref this IStockpile.Data stockpile_data, IStockpile.SlotID slot_src, IStockpile.SlotID slot_dst)
		{
			var type = slot_src.Type;
			if (slot_dst.Type == type)
			{
				switch (type)
				{
					case Stockpile.SlotType.Item:
					{
						var arr = stockpile_data.items;
						return !arr[slot_dst.Index].IsValid() || arr[slot_src.Index] == arr[slot_dst.Index];
					}
					break;

					case Stockpile.SlotType.Shipment:
					{
						var arr = stockpile_data.shipments;
						return !arr[slot_dst.Index].IsValid() || arr[slot_src.Index] == arr[slot_dst.Index];
					}
					break;

					//case Stockpile.SlotType.Character:
					//{
					//	var arr = stockpile_data.characters;
					//	return !arr[slot_dst.Index].IsValid() || arr[slot_src.Index] == arr[slot_dst.Index];
					//}
					//break;

					default:
					{
						return false;
					}
				}
			}
			else
			{
				return false;
			}
		}

		public static bool Contains(ref this IStockpile.Data stockpile_data, IStockpile.SlotID slot_shipment, IStockpile.SlotID slot_item, out int index)
		{
			index = -1;
			if (slot_shipment.IsValid(Stockpile.SlotType.Shipment) && slot_item.IsValid(Stockpile.SlotType.Item))
			{
				ref var shipment = ref stockpile_data.shipments[slot_shipment.Index];
				ref var item = ref stockpile_data.items[slot_item.Index];

				index = shipment.LastIndexOf(item);
			}
			return index != -1;
		}

		// TODO: this is slow
		public static bool CanMerge(ref this IStockpile.Data stockpile_data, IStockpile.SlotID slot_shipment, FixedArray4<IStockpile.SlotID> slots_items)
		{
			if (slot_shipment.IsValid(Stockpile.SlotType.Shipment) && !slots_items.IsEmpty())
			{
				var items = stockpile_data.items;

				ref var shipment = ref stockpile_data.shipments[slot_shipment.Index];
				for (var i = 0; i < slots_items.Length; i++)
				{
					var slot_item = slots_items[i];

					if (slot_item.raw == 0) continue;
					else if (slot_item.Type == Stockpile.SlotType.Item)
					{
						ref var item = ref items[slot_item.Index];
						if (item.material && shipment.LastIndexOf(item) == -1) return false;
					}
					else
					{
						return false;
					}
				}

				return true;
			}
			else
			{
				return false;
			}
		}

		public static ushort GetMaxCount(ref this IStockpile.Data stockpile_data, IStockpile.SlotID slot_shipment, FixedArray4<IStockpile.SlotID> slots_items, FixedArray4<ushort> amounts)
		{
			if (slot_shipment.IsValid(Stockpile.SlotType.Shipment) && !slots_items.IsEmpty())
			{
				var items = stockpile_data.items;
				var count_max = ushort.MaxValue;

				ref var shipment = ref stockpile_data.shipments[slot_shipment.Index];
				for (var i = 0; i < slots_items.Length; i++)
				{
					var slot_item = slots_items[i];

					if (slot_item.raw == 0) continue;
					else if (slot_item.Type == Stockpile.SlotType.Item)
					{
						ref var item = ref items[slot_item.Index];
						if (item.IsValid())
						{
							var amount = (ushort)item.quantity;
							if (amount == 0 || amounts[i] > amount) return 0;
							else if (amounts[i] > 0)
							{
								count_max = Maths.Min(count_max, (ushort)(amount / amounts[i]));
							}
							else
							{
								count_max = Maths.Min(count_max, amount);
							}
						}
					}
					else
					{
						return 0;
					}
				}

				if (count_max != ushort.MaxValue)
				{
					return count_max;
				}
			}

			return 0;
		}

		// TODO: this is slow
		public static bool TryRemap(ref this IStockpile.Data stockpile_data, IStockpile.SlotID slot_shipment, ref FixedArray4<IStockpile.SlotID> slots_items, ref FixedArray4<ushort> amounts)
		{
			if (slot_shipment.IsValid(Stockpile.SlotType.Shipment) && !slots_items.IsEmpty())
			{
				ref var shipment = ref stockpile_data.shipments[slot_shipment.Index];
				if (shipment.IsValid())
				{
					var items = stockpile_data.items;

					var slots_items_tmp = default(FixedArray4<IStockpile.SlotID>);
					var amounts_tmp = default(FixedArray4<ushort>);

					ref var shipment_items = ref shipment.items;

					// TODO: make it use the 4-byte item headers instead
					var items_tmp = default(FixedArray4<Shipment.Item>);

					for (var i = 0; i < items_tmp.Length; i++)
					{
						var slot_item = slots_items[i];
						if (slot_item.IsValid(Stockpile.SlotType.Item))
						{
							ref var item = ref items[slot_item.Index];

							items_tmp[i] = item;
							//items_tmp[i].quantity = (float)amounts[i];
						}
					}

					for (var i = 0; i < shipment_items.Length; i++)
					{
						ref var shipment_item = ref shipment_items[i];

						var index = items_tmp.LastIndexOf(shipment_item);
						//App.WriteLine($"{i}: {index}; {shipment_item}");

						if (index != -1)
						{
							slots_items_tmp[i] = slots_items[index];
							amounts_tmp[i] = (ushort)shipment_item.quantity;

							items_tmp[index] = default;
							slots_items[index] = default;
							amounts[index] = default;
						}
					}

					slots_items = slots_items_tmp;
					amounts = amounts_tmp;

					return true;
				}
			}

			return false;
		}

		public static bool CanMerge(ref this IStockpile.Data stockpile_data, IStockpile.SlotID slot_a, IStockpile.SlotID slot_b)
		{
			var type_a = slot_a.Type;
			var type_b = slot_b.Type;

			switch (type_a)
			{
				case Stockpile.SlotType.Item:
				{
					if (type_b == Stockpile.SlotType.Shipment)
					{
						return !stockpile_data.shipments[slot_b.Index].IsValid() || stockpile_data.shipments[slot_b.Index].LastIndexOf(stockpile_data.items[slot_a.Index]) != -1;
					}
					else if (type_b == Stockpile.SlotType.Item)
					{
						var arr = stockpile_data.items;
						return arr[slot_a.Index] == arr[slot_b.Index];
					}
				}
				break;

				case Stockpile.SlotType.Shipment:
				{
					if (type_b == Stockpile.SlotType.Shipment)
					{
						var arr = stockpile_data.shipments;
						return arr[slot_a.Index] == arr[slot_b.Index];
					}
					else if (type_b == Stockpile.SlotType.Item)
					{
						return !stockpile_data.shipments[slot_b.Index].IsValid() || stockpile_data.shipments[slot_a.Index].LastIndexOf(stockpile_data.items[slot_b.Index]) != -1;
					}
				}
				break;

				//case Stockpile.SlotType.Character:
				//{
				//	if (type_b == Stockpile.SlotType.Character)
				//	{
				//		var arr = stockpile_data.characters;
				//		return arr[slot_a.Index] == arr[slot_b.Index];
				//	}
				//}
				//break;
			}

			return false;
		}

		public static bool IsSlotValid(ref this IStockpile.Data stockpile_data, IStockpile.SlotID slot_id)
		{
			var slot_type = slot_id.Type;
			if (slot_type == Stockpile.SlotType.Item) return slot_id.Index < stockpile_data.items.Length;
			else if (slot_type == Stockpile.SlotType.Shipment) return slot_id.Index < stockpile_data.shipments.Length;
			else return false;
		}

		public static ref Shipment.Item GetItem(ref this IStockpile.Data stockpile_data, IStockpile.SlotID slot_id)
		{
			if (slot_id.Type == Stockpile.SlotType.Item) return ref stockpile_data.items.GetRefAtIndexOrNull(slot_id.Index);
			else return ref Unsafe.NullRef<Shipment.Item>();
		}

		public static ref Shipment.Data GetShipment(ref this IStockpile.Data stockpile_data, IStockpile.SlotID slot_id)
		{
			if (slot_id.Type == Stockpile.SlotType.Shipment) return ref stockpile_data.shipments.GetRefAtIndexOrNull(slot_id.Index);
			else return ref Unsafe.NullRef<Shipment.Data>();
		}

		//public static ref ICharacter.Handle GetCharacter(ref this IStockpile.Data stockpile_data, IStockpile.SlotID slot_id)
		//{
		//	if (slot_id.Type == Stockpile.SlotType.Character) return ref stockpile_data.characters.GetRefAtIndexOrNull(slot_id.Index);
		//	else return ref Unsafe.NullRef<ICharacter.Handle>();
		//}


		//public static bool CanMerge(ref this IStockpile.Data stockpile_data, IStockpile.SlotID slot_src, IStockpile.SlotID slot_dst)
		//{
		//	var type = slot_src.Type;
		//	if (slot_dst.Type == type)
		//	{
		//		switch (type)
		//		{
		//			case Stockpile.SlotType.Item:
		//			{
		//				var arr = stockpile_data.items;
		//				return arr[slot_src.Index] == arr[slot_dst.Index];
		//			}
		//			break;

		//			case Stockpile.SlotType.Shipment:
		//			{
		//				var arr = stockpile_data.shipments;
		//				return arr[slot_src.Index] == arr[slot_dst.Index];
		//			}
		//			break;

		//			case Stockpile.SlotType.Character:
		//			{
		//				var arr = stockpile_data.characters;
		//				return arr[slot_src.Index] == arr[slot_dst.Index];
		//			}
		//			break;

		//			default:
		//			{
		//				return false;
		//			}
		//		}
		//	}
		//	else
		//	{
		//		return false;
		//	}
		//}

#if CLIENT
		public static readonly Sprite tex_icons_depot = GUI.tex_icons_widget.GetSprite(16, 16, 0, 5);

		[Shitcode]
		public struct StockpileGUI: IGUICommand
		{
			public Entity ent_stockpile;
			public Transform.Data transform;
			public Stockpile.Data stockpile;
			public Address.Data address;

			//public static FixedArray4<Shipment.Item> edit_items;

			public static Stockpile.Request? edit_request;
			//public static Shipment.Data? edit_shipment;

			public static void DrawGrid(scoped ref GUI.Grid grid, IStockpile.Handle h_stockpile, StockpileFilterDelegate dg_filter = null, StockpileGUIDelegate dg_gui = null, StockpileGUISkippedDelegate dg_gui_skipped = null, IAddress.Handle h_address = default, Stockpile.Data.Flags flags = Stockpile.Data.Flags.None)
			{
				var show_debug = false;

				const float alpha_selected = 1.00f;
				const float alpha_default = 0.75f;
				const float alpha_preview = 0.75f;
				const float alpha_disabled = 0.50f;
				const float selected_luma_blend = 0.85f;

				ref var stockpile_data = ref h_stockpile.GetData(out var stockpile_asset);
				var context = GUI.ItemContext.Begin(true);

				ref var request_base = ref edit_request.GetRefOrNull();

				var item_size = new Vector2(48, 48);
				var ship_size = new Vector2(96, 96);
				var char_size = new Vector2(48, 48);

				var addresses = stockpile_data.addresses;
				var stockpile_items = stockpile_data.items.AsSpan();
				var stockpile_ships = stockpile_data.shipments.AsSpan();
				//var stockpile_chars = stockpile_data.characters.AsSpan();

				var has_slot_filter = h_address.IsValid();
				var hide_filtered_slots = flags.HasAny(Stockpile.Data.Flags.Hide_Unowned);
				var ignore_ownership = flags.HasAny(Stockpile.Data.Flags.Full_Access);

				//using (var grid = GUI.Grid.New(GUI.Rm, item_size))
				{
					#region Items
					for (var i = 0; i < stockpile_items.Length; i++)
					{
						var slot_id = new IStockpile.SlotID(i, Stockpile.SlotType.Item);
						var slot_has_address = addresses.TryGetValue(slot_id.raw, out var h_address_slot);
						var is_owned = has_slot_filter && h_address_slot == h_address;

						if (!is_owned && hide_filtered_slots)
						{
							if (dg_gui_skipped != null)
							{
								var args_skip = new StockpileGUISkippedSlot(stockpile: ref stockpile_data, slot: slot_id,
								h_address_slot: h_address_slot,
								cell_size: item_size);
								dg_gui_skipped.Invoke(ref grid, h_address, flags, args_skip);
							}

							continue;
						}

						//if (has_slot_filter && !addresses.ContainsPair(slot_id.raw, h_address)) continue;

						var item = stockpile_items[i];
						using (var cell = grid.Push(item_size, slot_id.raw))
						{
							var rect = cell.group.GetOuterRect();
							var pressed = GUI.ButtonBehavior(slot_id.raw, rect, out var hovered, out var held, allow_overlap: true);

							var arg_index = -1;

							var valid = item.quantity >= Resource.epsilon;
							var preview = false;
							var selectable = false;
							var amount = (int)item.quantity;
							var amount_add = 0;
							var color_frame = GUI.col_frame;
							var color_item = GUI.col_white;
							var enabled = true;
							var selected = false;

							var slot_id_preview = default(IStockpile.SlotID?);
							var slot_id_preview_subindex = default(byte);

							if (dg_filter != null)
							{
								var args_filter = new StockpileFilterSlot(stockpile: ref stockpile_data, slot: slot_id,
									slot_id_preview: ref slot_id_preview, slot_id_preview_subindex: ref slot_id_preview_subindex,
									amount: ref amount, amount_add: ref amount_add, arg_index: ref arg_index,
									enabled: ref enabled, valid: ref valid, selected: ref selected, preview: ref preview);
								dg_filter.Invoke(ref request_base, args_filter);
							}

							if (has_slot_filter)
							{
								enabled &= ignore_ownership | is_owned;
							}

							if (dg_gui != null)
							{
								var args_gui = new StockpileGUISlot(stockpile: ref stockpile_data, slot: slot_id,
									slot_id_preview: ref slot_id_preview, slot_id_preview_subindex: ref slot_id_preview_subindex,
									amount: ref amount, amount_add: ref amount_add, arg_index: ref arg_index,
									enabled: ref enabled, valid: ref valid, selected: ref selected, preview: ref preview,
									hovered: ref hovered, selectable: ref selectable, pressed: ref pressed,
									color_frame: ref color_frame, color_item: ref color_item,
									rect: ref rect);
								dg_gui.Invoke(ref request_base, args_gui);
							}

							if (preview && slot_id_preview.TryGetValue(out var v_slot_id_preview))
							{
								var slot_id_preview_type = v_slot_id_preview.Type;
								switch (slot_id_preview_type)
								{
									case Stockpile.SlotType.Shipment:
									{
										item = stockpile_ships[v_slot_id_preview.Index].items[slot_id_preview_subindex];
										item.quantity = 0;
									}
									break;

									case Stockpile.SlotType.Item:
									{
										item = stockpile_items[v_slot_id_preview.Index];
										item.quantity = 0;
									}
									break;
								}
							}

							var alpha = alpha_disabled;
							if (enabled)
							{
								alpha = alpha_default;
								if (selected || (hovered && selectable))
								{
									alpha = alpha_selected;
									color_frame = GUI.col_frame.LumaBlend(color_frame, selected_luma_blend);

									if (pressed)
									{
										Sound.PlayGUI(GUI.sound_select, volume: 0.07f);
									}

									if (hovered)
									{
										GUI.SetCursor(App.CursorType.Hand, 5);
									}
								}
							}
							color_frame = color_frame.WithAlphaMult(alpha);
							//if (preview && !selected) alpha = alpha_preview;

							if (h_address_slot.TryGetDefinition(out var address_slot_asset))
							{
								GUI.DrawTextCentered(slot_id.Index, position: rect.GetPosition() + new Vector2(2, 0), pivot: new(0.50f, 0.50f), font: GUI.Font.Editia, size: 16, color: address_slot_asset.data.color_a.WithAlpha(75), layer: GUI.Layer.Window, format: "00");
								GUI.DrawTextCentered(address_slot_asset.data.code, position: rect.GetPosition() + new Vector2(0, 12), pivot: new(0.50f, 0.50f), font: GUI.Font.Editia, size: 8, color: address_slot_asset.data.color_a.WithAlpha(75), layer: GUI.Layer.Window, format: "00 000");
							}
							else
							{
								GUI.DrawTextCentered(slot_id.Index, position: rect.GetPosition() + new Vector2(2, 0), pivot: new(0.50f, 0.50f), font: GUI.Font.Editia, size: 16, color: GUI.font_color_desc.WithAlpha(50), layer: GUI.Layer.Window, format: "00");
							}

							using (var slot = GUI.ItemSlot.New(ref context, id: cell.id, item, item_size, enabled: enabled, shipment_flags: Shipment.Flags.Keep_Items, frame_color: color_frame, item_color: color_item, amount_add: amount_add, disabled_alpha: alpha_disabled, is_readonly: true, alpha: preview ? alpha_preview : 1.00f))
							{
								//GUI.DrawTextCentered(slot_id.Index, position: rect.GetPosition() + new Vector2(2, 0), pivot: new(0.50f, 0.50f), font: GUI.Font.Editia, size: 16, color: GUI.font_color_desc.WithAlpha(100), layer: GUI.Layer.Window, format: "00");
							}

							if (show_debug)
							{
								GUI.DrawHoverTooltip($"{slot_id.Index}; {slot_id.Type}; {slot_id.raw}; {slot_id.IsValid()}; " +
									$"{cell.id}; valid: {valid}; selectable: {selectable}; selected: {selected}; " +
									$"arg: {arg_index}; enabled: {enabled}; " +
									$"hovered: {hovered}; held: {held}; pressed: {pressed}");
							}
							else
							{
								if (hovered)
								{
									if (flags.HasNone(Stockpile.Data.Flags.Hide_Unowned) && flags.HasAll(Stockpile.Data.Flags.Edit_Addresses | Stockpile.Data.Flags.Show_Addresses) && address_slot_asset != null)
									{
										GUI.DrawHoverTooltip($"Owner: {address_slot_asset.data.name_generated}\n{address_slot_asset.data.name}");

										GUI.DrawEntity(address_slot_asset.data.entity, color: GUI.col_button_yellow.WithAlpha(175), layer: GUI.Layer.Background);
										GUI.DrawEntityMarker(address_slot_asset.data.entity, color: GUI.col_button_yellow, cross_size: 1.00f, layer: GUI.Layer.Background);
									}
								}
							}
						}
					}
					#endregion

					Span<int> index_ordering = [2, 0, 3, 1];

					#region Shipments
					for (var i = 0; i < stockpile_ships.Length; i++)
					{
						var slot_id = new IStockpile.SlotID(i, Stockpile.SlotType.Shipment);
						var slot_has_address = addresses.TryGetValue(slot_id.raw, out var h_address_slot);
						var is_owned = has_slot_filter && h_address_slot == h_address;

						if (!is_owned && hide_filtered_slots)
						{
							if (dg_gui_skipped != null)
							{
								var args_skip = new StockpileGUISkippedSlot(stockpile: ref stockpile_data, slot: slot_id,
								h_address_slot: h_address_slot,
								cell_size: ship_size);
								dg_gui_skipped.Invoke(ref grid, h_address, flags, args_skip);
							}

							continue;
						}

						//if (has_slot_filter && !addresses.ContainsPair(slot_id.raw, h_address)) continue;
						//if (has_slot_filter && addresses.TryGetValue(slot_id.raw, out var address_tmp) && address_tmp != h_address_filter) continue;
						//if (has_slot_filter && !slots_visible.Contains(slot_id)) continue;

						var ship = stockpile_ships[i];
						using (var cell = grid.Push(ship_size, slot_id.raw))
						{
							var rect = cell.group.GetOuterRect();
							var pressed = GUI.ButtonBehavior(slot_id.raw, rect, out var hovered, out var held, allow_overlap: true);

							var arg_index = -1;

							var valid = ship.IsValid();
							var preview = false;
							var selectable = false;
							var amount = (int)ship.count;
							var amount_add = 0;
							var color_frame = GUI.col_frame;
							var color_item = GUI.col_white;
							var enabled = true;
							var selected = false;

							var slot_id_preview = default(IStockpile.SlotID?);
							var slot_id_preview_subindex = default(byte);

							if (dg_filter != null)
							{
								var args_filter = new StockpileFilterSlot(stockpile: ref stockpile_data, slot: slot_id,
								slot_id_preview: ref slot_id_preview, slot_id_preview_subindex: ref slot_id_preview_subindex,
								amount: ref amount, amount_add: ref amount_add, arg_index: ref arg_index,
								enabled: ref enabled, valid: ref valid, selected: ref selected, preview: ref preview);
								dg_filter.Invoke(ref request_base, args_filter);
							}

							if (has_slot_filter)
							{
								enabled &= ignore_ownership | is_owned;
							}

							if (dg_gui != null)
							{
								var args_gui = new StockpileGUISlot(stockpile: ref stockpile_data, slot: slot_id,
								slot_id_preview: ref slot_id_preview, slot_id_preview_subindex: ref slot_id_preview_subindex,
								amount: ref amount, amount_add: ref amount_add, arg_index: ref arg_index,
								enabled: ref enabled, valid: ref valid, selected: ref selected, preview: ref preview,
								hovered: ref hovered, selectable: ref selectable, pressed: ref pressed,
								color_frame: ref color_frame, color_item: ref color_item,
								rect: ref rect);
								dg_gui.Invoke(ref request_base, args_gui);
							}

							if (preview && slot_id_preview.TryGetValue(out var v_slot_id_preview))
							{
								var slot_id_preview_type = v_slot_id_preview.Type;
								switch (slot_id_preview_type)
								{
									case Stockpile.SlotType.Shipment:
									{
										ship = stockpile_ships[v_slot_id_preview.Index];
										ship.count = 0;
									}
									break;
								}
							}

							var alpha = alpha_disabled;
							if (enabled)
							{
								alpha = alpha_default;
								if (selected || (hovered && selectable))
								{
									alpha = alpha_selected;
									color_frame = GUI.col_frame.LumaBlend(color_frame, selected_luma_blend);

									if (pressed)
									{
										Sound.PlayGUI(GUI.sound_select, volume: 0.07f);
									}

									if (hovered)
									{
										GUI.SetCursor(App.CursorType.Hand, 5);
									}
								}
							}
							color_frame = color_frame.WithAlphaMult(alpha);
							if (preview && !selected) alpha = alpha_preview;

							if (h_address_slot.TryGetDefinition(out var address_slot_asset))
							{
								GUI.DrawTextCentered(slot_id.Index, position: rect.GetPosition() + new Vector2(2, 0), pivot: new(0.50f, 0.50f), font: GUI.Font.Editia, size: 24, color: address_slot_asset.data.color_a.WithAlpha(75), layer: GUI.Layer.Window, format: "00");
								GUI.DrawTextCentered(address_slot_asset.data.code, position: rect.GetPosition() + new Vector2(0, 18), pivot: new(0.50f, 0.50f), font: GUI.Font.Editia, size: 12, color: address_slot_asset.data.color_a.WithAlpha(75), layer: GUI.Layer.Window, format: "00 000");
								//GUI.DrawTextCentered(address_slot_asset.data.name_generated, position: rect.GetPosition() + new Vector2(0, 18), pivot: new(0.50f, 0.50f), font: GUI.Font.Editia, size: 12, color: address_slot_asset.data.color_a.WithAlpha(75), layer: GUI.Layer.Window);
							}
							else
							{
								GUI.DrawTextCentered(slot_id.Index, position: rect.GetPosition() + new Vector2(2, 0), pivot: new(0.50f, 0.50f), font: GUI.Font.Editia, size: 24, color: GUI.font_color_desc.WithAlpha(50), layer: GUI.Layer.Window, format: "00");
							}

							using (var group_shipment = GUI.Group.New(size: ship_size))
							{
								if (valid || preview)
								{
									var shipment_items = ship.items.AsSpan();

									var rect_top = rect;
									var rect_bottom = rect;
									var rect_center = rect;
									//rect_bottom.a.Y += rect.GetSize().Y * 0.50f;
									//rect_bottom.a.X += 8;
									//rect_bottom.b.Y -= 12;

									rect_bottom.m_value.y += rect.GetSize().Y * 0.50f;
									rect_bottom.m_value.x += 8;
									rect_bottom.m_value.w -= 12;

									//using (var group_prefab = cell.group.Split(size: new(GUI.RmX, 48), align_x: GUI.AlignX.Left, align_y: GUI.AlignY.Bottom, padding: new(8, 4, 8, 8)))
									{
										if (ship.h_container.GetDefinition()?.data.h_prefab.TryGetPrefab(out var prefab) ?? false)
										{
											//GUI.DrawSpriteCentered(prefab.GetIcon(), rect: rect.Pad(8, 4, 8, 8), layer: GUI.Layer.Window, scale: 2.00f, pivot: new(0.00f, 1.00f), color: GUI.col_white.WithAlphaMult(0.80f).WithAlphaMult(alpha));
											GUI.DrawSpriteCentered(prefab.GetIcon(), rect: rect.Pad(8, 4, 8, 6), layer: GUI.Layer.Window, scale: 2.00f, pivot: new(0.50f, 1.00f), color: GUI.col_white.WithAlphaMult(0.80f * alpha));
										}
									}

									for (var j = 0; j < shipment_items.Length; j++)
									{
										var idx = index_ordering[j];
										using (var group_item = cell.group.Split(new(item_size.X, 16), GUI.AlignX.Left, GUI.AlignY.Top, new(2 + (idx & 1) * 38, -2 + (idx >= 2 ? 1 : 0) * 24, 0, 0)))
										{
											ref var item = ref shipment_items[idx];
											//GUI.Text($"{item.quantity}");
											GUI.DrawItem(ref item, item_size, padding: new(8, 0, 0, 4), is_readonly: !enabled, icon_color: GUI.col_white.WithAlphaMult(alpha), text_color: GUI.font_color_default.WithAlphaMult(alpha)); // + ((j & 1) * 8)); );
										}
									}


									//GUI.TitleCentered($"{ship.count + amount_add}", size: 18, pivot: new(0.50f, 1.00f), offset: new(0, -8), color: (amount_add == 0 ? GUI.font_color_title : (amount_add > 0 ? GUI.col_add : GUI.col_remove)).WithAlphaMult(alpha));

									if (ship.count == 0)
									{
										GUI.TitleCentered(amount_add, size: 18, pivot: new(0.50f, 1.00f), offset: new(0, -8), color: (amount_add == 0 ? GUI.font_color_title : (amount_add > 0 ? GUI.col_add : GUI.col_remove)).WithAlphaMult(alpha));
									}
									else
									{
										GUI.TitleCentered(ship.count, size: 18, pivot: new(0.50f, 1.00f), offset: new(0, -8), color: GUI.font_color_title.WithAlphaMult(alpha));
										if (amount_add != 0)
										{
											var rect_text = GUI.GetLastItemRect();
											//rect_text.b.X = rect_text.a.X;
											//rect_text.a.X = rect.a.X;

											//rect_text.a.X = rect_text.b.X;
											//rect_text.b.X = rect.b.X;

											rect_text.m_value.x = rect_text.m_value.z;
											rect_text.m_value.z = rect.m_value.z;


											GUI.TitleCentered(amount_add > 0 ? $"+{amount_add}" : $"-{Math.Abs(amount_add)}", rect_text, size: 18, pivot: new(0.00f, 0.50f), offset: new(0, 0), color: (amount_add > 0 ? GUI.col_add : GUI.col_remove).WithAlphaMult(alpha));
										}
									}
								}

								cell.group.DrawBackground(GUI.tex_frame_white, color: color_frame.WithAlphaMult(alpha));
							}

							if (show_debug)
							{
								GUI.DrawHoverTooltip($"{slot_id.Index}; {slot_id.Type}; {slot_id.raw}; {slot_id.IsValid()}; " +
								$"{cell.id}; valid: {valid}; selectable: {selectable}; selected: {selected}; " +
								$"arg: {arg_index}; enabled: {enabled}; " +
								$"hovered: {hovered}; held: {held}; pressed: {pressed}");
							}
							else
							{
								if (hovered)
								{
									if (flags.HasNone(Stockpile.Data.Flags.Hide_Unowned) && flags.HasAll(Stockpile.Data.Flags.Edit_Addresses | Stockpile.Data.Flags.Show_Addresses) && address_slot_asset != null)
									{
										GUI.DrawHoverTooltip($"Owner: {address_slot_asset.data.name_generated}\n{address_slot_asset.data.name}");

										GUI.DrawEntity(address_slot_asset.data.entity, color: GUI.col_button_yellow.WithAlpha(175), layer: GUI.Layer.Background);
										GUI.DrawEntityMarker(address_slot_asset.data.entity, color: GUI.col_button_yellow, cross_size: 1.00f, layer: GUI.Layer.Background);
									}
								}
							}
						}
					}
					#endregion

					//#region Characters
					//for (var i = 0; i < stockpile_chars.Length; i++)
					//{
					//	var slot_id = new IStockpile.SlotID(i, Stockpile.SlotType.Character);
					//	if (has_slot_filter && !slots_visible.Contains(slot_id)) continue;

					//	var h_character = stockpile_chars[i];
					//	using (var cell = grid.Push(item_size, slot_id.raw))
					//	{
					//		var rect = cell.group.GetOuterRect();
					//		var pressed = GUI.ButtonBehavior(slot_id.raw, rect, out var hovered, out var held, allow_overlap: true);

					//		var arg_index = -1;

					//		var valid = h_character.IsValid();
					//		var preview = false;
					//		var selectable = false;
					//		var amount = valid ? 1 : 0;
					//		var amount_add = 0;
					//		var color_frame = GUI.col_frame;
					//		var color_item = GUI.col_white;
					//		var enabled = true;
					//		var selected = false;

					//		var slot_id_preview = default(IStockpile.SlotID?);
					//		var slot_id_preview_subindex = default(byte);

					//		var args_filter = new StockpileFilterSlot(stockpile: ref stockpile_data, slot: slot_id,
					//			slot_id_preview: ref slot_id_preview, slot_id_preview_subindex: ref slot_id_preview_subindex,
					//			amount: ref amount, amount_add: ref amount_add, arg_index: ref arg_index,
					//			enabled: ref enabled, valid: ref valid, selected: ref selected, preview: ref preview);
					//		dg_filter?.Invoke(ref request_base, args_filter);

					//		var args_gui = new StockpileGUISlot(stockpile: ref stockpile_data, slot: slot_id,
					//			slot_id_preview: ref slot_id_preview, slot_id_preview_subindex: ref slot_id_preview_subindex,
					//			amount: ref amount, amount_add: ref amount_add, arg_index: ref arg_index,
					//			enabled: ref enabled, valid: ref valid, selected: ref selected, preview: ref preview,
					//			hovered: ref hovered, selectable: ref selectable, pressed: ref pressed,
					//			color_frame: ref color_frame, color_item: ref color_item,
					//			rect: ref rect);
					//		dg_gui?.Invoke(ref request_base, args_gui);

					//		if (preview && slot_id_preview.TryGetValue(out var v_slot_id_preview))
					//		{
					//			var slot_id_preview_type = v_slot_id_preview.Type;
					//			switch (slot_id_preview_type)
					//			{
					//				case Stockpile.SlotType.Character:
					//				{
					//					h_character = stockpile_chars[v_slot_id_preview.Index];
					//				}
					//				break;
					//			}
					//		}

					//		var alpha = alpha_disabled;
					//		if (enabled)
					//		{
					//			alpha = alpha_default;
					//			if (selected || (hovered && selectable))
					//			{
					//				alpha = alpha_selected;
					//				color_frame = GUI.col_frame.LumaBlend(color_frame, selected_luma_blend);

					//				if (pressed)
					//				{
					//					Sound.PlayGUI(GUI.sound_select, volume: 0.07f);
					//				}

					//				if (hovered)
					//				{
					//					GUI.SetCursor(App.CursorType.Hand, 5);
					//				}
					//			}
					//		}
					//		color_frame = color_frame.WithAlphaMult(alpha);
					//		if (preview && !selected) alpha = alpha_preview;

					//		GUI.DrawCharacterHead(h_character, char_size, color: color_frame);

					//		//using (var slot = GUI.ItemSlot.New(ref context, id: cell.id, item, item_size, enabled: enabled, shipment_flags: Shipment.Flags.Keep_Items, frame_color: color_frame, item_color: color_item, amount_add: amount_add, disabled_alpha: alpha, is_readonly: true))
					//		//{

					//		//}

					//		if (show_debug)
					//		{
					//			GUI.DrawHoverTooltip($"{slot_id.Index}; {slot_id.Type}; {slot_id.raw}; {slot_id.IsValid()}; " +
					//			$"{cell.id}; valid: {valid}; selectable: {selectable}; selected: {selected}; " +
					//			$"arg: {arg_index}; enabled: {enabled}; " +
					//			$"hovered: {hovered}; held: {held}; pressed: {pressed}");
					//		}
					//	}
					//}
					//#endregion
				}
			}

			public void Draw()
			{
				ref var region = ref this.ent_stockpile.GetRegion();

				static void Reset()
				{
					//edit_items = default;
					edit_request = null;
					//edit_shipment = null;
				}

				using (var window = GUI.Window.Interaction(identifier: "Stockpile"u8, entity: this.ent_stockpile, tooltip_tab: "Bulk item storage and access\nto cross-region shipping."))
				{
					this.StoreCurrentWindowTypeID();
					if (window.show)
					{
						GUI.DrawWindowBackground();

						var show_debug = false;

						var h_stockpile = this.stockpile.h_stockpile;

						ref readonly var mouse = ref GUI.GetMouse();
						ref var stockpile_data = ref h_stockpile.GetData();

						var context = GUI.ItemContext.Begin(true);
						Crafting.Context.NewFromCurrentCharacter(this.ent_stockpile, out var context_character, search_radius: 8.00f);

						using (var group_top = GUI.Group.New(size: new(GUI.RmX, 56)))
						{
							if (stockpile_data.IsNotNull())
							{
								ref var company_provider_data = ref stockpile_data.h_company.GetData();
								//if (company_provider_data.IsNotNull())
								//{
								//	Market.MarketGUI.DrawCompanyLogo(stockpile_data.h_company, ICompany.LogoSize.Sign_2x2);
								//}

								Market.MarketGUI.DrawCompanyLogo(stockpile_data.h_company, ICompany.LogoSize.Sign_2x2);
								GUI.FocusableAsset(stockpile_data.h_company);

								GUI.SameLine();

								using (var group_text = GUI.Group.New(new(GUI.RmX - 200, GUI.RmY)))
								{
									GUI.TitleCentered($"{this.ent_stockpile.GetName()} | {stockpile_data.name}", size: 24, pivot: new(0.00f, 0.50f), offset: new(8, -12));
									GUI.FocusableAsset(h_stockpile);

									ref var address_data = ref this.address.h_address.GetData();
									if (address_data.IsNotNull())
									{
										GUI.TitleCentered(address_data.name_generated, size: 16, pivot: new(0.00f, 0.50f), offset: new(8, 4), color: GUI.font_color_desc);
										GUI.FocusableAsset(this.address.h_address);
										//GUI.TitleCentered(address_data.name_generated, size: 16, pivot: new(0.00f, 0.50f), offset: new(8, -12));
										//GUI.TitleCentered(this.ent_stockpile.GetName(), size: 16, pivot: new(1.00f, 0.00f), offset: new(-8, 18));
									}

									if (company_provider_data.IsNotNull())
									{
										//GUI.TitleCentered($"Provided by {company_provider_data.name}", size: 16, pivot: new(0.00f, 0.50f), offset: new(8, 4), color: GUI.font_color_desc);
									}
									else
									{
										//GUI.TitleCentered("<no company>", size: 16, pivot: new(0.00f, 0.50f), offset: new(8, 4), color: GUI.font_color_desc);
									}

									//GUI.TitleCentered(stockpile_data.name, size: 24, pivot: new(0.00f, 0.50f), offset: new(8, -12));
									//ref var address_data = ref this.address.h_address.GetData();
									//if (address_data.IsNotNull())
									//{
									//	GUI.TitleCentered(address_data.name_generated, size: 16, pivot: new(1.00f, 0.00f), offset: new(-8, 4));
									//	GUI.TitleCentered(this.ent_stockpile.GetName(), size: 16, pivot: new(1.00f, 0.00f), offset: new(-8, 18));
									//}

									//if (company_provider_data.IsNotNull())
									//{
									//	GUI.TitleCentered($"Provided by {company_provider_data.name}", size: 16, pivot: new(0.00f, 0.50f), offset: new(8, 4), color: GUI.font_color_desc);
									//}
									//else
									//{
									//	GUI.TitleCentered("<no company>", size: 16, pivot: new(0.00f, 0.50f), offset: new(8, 4), color: GUI.font_color_desc);
									//}

									//ref var location_data = ref stockpile_data.h_location.GetData();
									//if (location_data.IsNotNull())
									//{
									//	GUI.TitleCentered(location_data.name, size: 16, pivot: new(0.00f, 1.00f), offset: new(8, -2), color: GUI.font_color_desc);
									//}
									//else if (stockpile_data.flags.HasAny(IStockpile.Flags.Mobile))
									//{
									//	ref var location_docked_data = ref stockpile_data.h_location_docked.GetData();
									//	if (location_docked_data.IsNotNull())
									//	{
									//		GUI.TitleCentered($"Parked at {location_docked_data.name_short}", size: 16, pivot: new(0.00f, 1.00f), offset: new(8, -2), color: GUI.font_color_desc);
									//	}
									//	else
									//	{
									//		GUI.TitleCentered("Travelling...", size: 16, pivot: new(0.00f, 1.00f), offset: new(8, -2), color: GUI.font_color_desc);
									//	}
									//}
								}
								//GUI.FocusableAsset(h_stockpile);

								GUI.SameLine();

								using (var group_warrants = GUI.Group.New(GUI.Rm))
								{
									//if (GUI.AssetInput2("stockpile.warrants", ref h_selected_warrant, new Vector2(GUI.RmX, 32), show_label: false,
									//	filter: (x) => x.data.h_stockpile == h_stockpile,
									//	draw: (asset, group, is_title) =>
									//	{
									//		if (asset != null)
									//		{
									//			GUI.TitleCentered(asset.data.h_company.ToString(), pivot: new(0.00f, 0.50f), offset: new(8, 0));
									//		}
									//		else
									//		{
									//			GUI.TitleCentered("<null>", pivot: new(0.00f, 0.50f), offset: new(8, 0));
									//		}
									//	}))
									//{

									//}
								}

								//GUI.TitleCentered($"{stockpile_data.}")

								//using (var group_company = group_top.Split(new(), GUI.AlignX.Right, GUI.AlignY.Top))
								//{
								//	if (stockpile_data.h_company.IsValid())
								//	{
								//		Market.StockpileGUI.DrawCompanyLogo(stockpile_data.h_company, ICompany.LogoSize.Sign_4x2);
								//	}
								//}
							}
							else
							{
								//if (region.IsNotNull())
								//{
								//	//if (GUI.DrawConfirmButton("link.stockpile"u8, "Link Stockpile"u8, "Are you sure?"u8, size: new(192, GUI.RmY), font_size: 24, color: GUI.col_button_yellow))

								//	if (GUI.DrawConfirmButton("link.stockpile"u8, "Link"u8, "Are you sure?"u8, size: new(224, GUI.RmY), font_size: 24, color: GUI.col_button_yellow))
								//	{
								//		ref var linkable = ref this.ent_stockpile.GetComponent<Entrance.Linkable.Data>();
								//		if (linkable.IsNotNull())
								//		{
								//			var ent_entrance_global = linkable.h_entrance.GetGlobalEntity();
								//			if (ent_entrance_global.IsAlive())
								//			{
								//				ref var stockpile_entrance = ref ent_entrance_global.GetComponent<Stockpile.Data>();
								//				if (stockpile_entrance.IsNotNull())
								//				{
								//					var rpc = new LinkRPC();
								//					rpc.h_stockpile = stockpile_entrance.h_stockpile;
								//					rpc.Send(this.ent_stockpile);
								//				}
								//			}
								//		}
								//	}
								//}
								//GUI.TitleCentered("<no stockpile linked>", size: 24, pivot: new(0.00f, 0.50f), offset: new(16, 0), color: GUI.font_color_title.WithAlphaMult(0.50f));
							}
						}

						//GUI.NewLine();

						GUI.SeparatorThick();

						if (stockpile_data.IsNotNull())
						{
							var item_size = new Vector2(48, 48);
							//var ship_size = new Vector2(96, 96);
							//var char_size = new Vector2(48, 48);

							var stockpile_items = stockpile_data.items.AsSpan();
							var stockpile_ships = stockpile_data.shipments.AsSpan();
							//var stockpile_chars = stockpile_data.characters.AsSpan();

							var h_address_current = stockpile_data.h_address;
							var h_address_filter = IAddress.Handle.None;

							var columns = Math.Max(1, (int)this.stockpile.grid_column_count);
							var group_size = new Vector2(columns * item_size.X, (stockpile_items.Length / columns) * item_size.Y);

							//var enable_selection = true;
							//var item_enable = true;
							//var ship_enable = true;
							//var char_enable = true;

							//var alpha_selected = 1.00f;
							//var alpha_default = 0.75f;
							//var alpha_preview = 0.75f;
							//var alpha_disabled = 0.25f;

							var h_faction_entity = this.ent_stockpile.GetFactionHandle();
							var h_faction_client = context_character.h_faction; // Client.GetFactionHandle();

							var can_submit = false;
							var can_manage = context_character.h_character && h_faction_entity.IsOwnedOrNeutral(h_faction_client);

							//var selected_luma_blend = 0.85f;
							var auto_submit = false;

							//var args_amount = Span<ushort>.Empty;
							//var args_items = Span<IStockpile.SlotID>.Empty;
							//var args_ships = Span<IStockpile.SlotID>.Empty;
							//var args_chars = Span<IStockpile.SlotID>.Empty;

							//var src_filled_count = 0;
							//var dst_filled_count = 0;

							var dg_filter = default(Economy.StockpileFilterDelegate);
							var dg_gui = default(Economy.StockpileGUIDelegate);
							var dg_gui_skipped = default(Economy.StockpileGUISkippedDelegate);

							if (this.stockpile.flags.HasNone(Stockpile.Data.Flags.Full_Access))
							{
								h_address_filter = this.address.h_address;
							}

							//scoped var slots_visible = Span<IStockpile.SlotID>.Empty;
							//if (this.stockpile.flags.HasNone(Stockpile.Data.Flags.Full_Access))
							//{
							//	slots_visible = this.stockpile.slots.Slice(this.stockpile.slots_count);
							//}

							#region Validation
							ref var edit_request_base = ref edit_request.GetRefOrNull();
							var current_type = edit_request?.type ?? Stockpile.Request.Type.Undefined;
							switch (current_type)
							{
								case Stockpile.Request.Type.Pack:
								{
									ref var request = ref edit_request_base.AsRequestType<Stockpile.Request.Pack>();

									dg_filter = static (ref request_base, slot) =>
									{
										ref var request = ref request_base.AsRequestType<Stockpile.Request.Pack>();
										if (request.IsNotNull())
										{
											var type = slot.slot_id.Type;
											if (type == Stockpile.SlotType.Shipment)
											{
												slot.selected = slot.slot_id == request.shipment_index_dst;
												slot.enabled = slot.selected || !slot.valid || (request.item_indices_src.IsEmpty() ? true : slot.stockpile.CanMerge(slot.slot_id, request.item_indices_src));
											}
											else if (type == Stockpile.SlotType.Item)
											{
												if (slot.valid)
												{
													if (request.shipment_index_dst.raw == 0)
													{
														slot.enabled = request.item_indices_src.TryGetIndexOfOrEmpty(slot.slot_id, ref slot.arg_index, out slot.selected);
													}
													else
													{
														ref var shipment = ref slot.stockpile.shipments[request.shipment_index_dst.Index];
														if (shipment.IsValid())
														{
															slot.enabled = slot.stockpile.Contains(request.shipment_index_dst, slot.slot_id, out slot.arg_index) && slot.amount >= shipment.items[slot.arg_index].quantity;
															slot.selected = slot.enabled && request.item_indices_src.TryGetIndexOf(slot.slot_id, ref slot.arg_index);
														}
														else
														{
															slot.enabled = request.item_indices_src.TryGetIndexOfOrEmpty(slot.slot_id, ref slot.arg_index, out slot.selected);
														}
													}
												}
												else
												{
													slot.enabled = false;
												}
											}
											else
											{
												slot.enabled = false;
											}
										}
									};

									dg_gui = static (ref request_base, slot) =>
									{
										ref var request = ref request_base.AsRequestType<Stockpile.Request.Pack>();
										if (request.IsNotNull())
										{
											slot.selectable = slot.enabled;
											if (slot.enabled)
											{
												var type = slot.slot_id.Type;

												ref var shipment = ref slot.stockpile.GetShipment(request.shipment_index_dst);
												var is_shipment_valid = shipment.IsValidRef();

												if (slot.selected || slot.hovered)
												{
													slot.color_frame = type == Stockpile.SlotType.Shipment ? GUI.col_dst : GUI.col_src;

													if (type == Stockpile.SlotType.Shipment)
													{
														slot.amount_add = request.count;
													}
													else
													{
														if (slot.arg_index != -1)
														{
															slot.amount_add = -(request.count * Math.Min(slot.amount, request.item_amounts_src[slot.arg_index]));
														}
													}
												}

												if (request.shipment_index_dst.raw != 0)
												{
													if (slot.selected)
													{
														//slot.color_frame = type == Stockpile.SlotType.Shipment ? GUI.col_src : GUI.col_dst;
													}

													if (slot.selected || slot.hovered)
													{
														//slot.color_frame = type == Stockpile.SlotType.Shipment ? GUI.col_dst : GUI.col_src;

														//var amount_tmp = (int)request.count;
														//if (GUI.ScrollInput(slot.rect, ref amount_tmp, 1, -50000, 50000))
														//{
														//	var amount_max = slot.stockpile.GetQuantity(request.shipment_index_src);
														//	request.count = (ushort)Maths.Clamp(Maths.Wrap(amount_tmp, 1, amount_max + 1), 1, amount_max);
														//}

														if (slot.hovered)
														{
															if (type == Stockpile.SlotType.Shipment || is_shipment_valid)
															{
																var amount_tmp = (int)request.count;
																if (GUI.ScrollInput(slot.rect, ref amount_tmp, 1, -50000, 50000))
																{
																	var amount_max = slot.stockpile.GetMaxCount(request.shipment_index_dst, request.item_indices_src, request.item_amounts_src); // 100; // slot.stockpile.GetQuantity(request.shipment_index_dst);
																	request.count = (ushort)Maths.Clamp(Maths.Wrap(amount_tmp, 1, amount_max + 1), 1, amount_max);
																}
															}
															else if (type == Stockpile.SlotType.Item && slot.selected)
															{
																var amount_tmp = (int)request.item_amounts_src[slot.arg_index];
																if (GUI.ScrollInput(slot.rect, ref amount_tmp, 1, -50000, 50000))
																{
																	var amount_max = slot.amount;
																	request.item_amounts_src[slot.arg_index] = (ushort)Maths.Clamp(Maths.Wrap(amount_tmp, 1, amount_max + 1), 1, amount_max);
																}
															}
														}
													}

													if (slot.pressed)
													{
														if (type == Stockpile.SlotType.Shipment)
														{
															if (slot.selected)
															{
																request.shipment_index_dst = default;
															}
															else
															{
																request.shipment_index_dst = slot.slot_id;
																slot.stockpile.TryRemap(request.shipment_index_dst, ref request.item_indices_src, ref request.item_amounts_src);
																request.count = Maths.Clamp(request.count, (ushort)1, slot.stockpile.GetMaxCount(request.shipment_index_dst, request.item_indices_src, request.item_amounts_src));
															}
														}
														else
														{
															if (slot.selected)
															{
																request.item_indices_src[slot.arg_index] = default;
																if (!is_shipment_valid) request.item_amounts_src[slot.arg_index] = default;
															}
															else
															{
																request.item_indices_src[slot.arg_index] = slot.slot_id;
																if (!is_shipment_valid) request.item_amounts_src[slot.arg_index] = (ushort)slot.amount;
																slot.stockpile.TryRemap(request.shipment_index_dst, ref request.item_indices_src, ref request.item_amounts_src);
																request.count = Maths.Clamp(request.count, (ushort)1, slot.stockpile.GetMaxCount(request.shipment_index_dst, request.item_indices_src, request.item_amounts_src));
															}

															//App.WriteLine($"{request.item_indices_src[slot.arg_index]}; {slot.arg_index}; {slot.slot_id}");
															//request.item_indices_src[slot.arg_index] = slot.selected ? default : slot.slot_id;
															//request.item_amounts_src[slot.arg_index] = is_shipment_valid ? (ushort)shipment.items[slot.arg_index].quantity : (ushort)slot.amount;
														}
													}
												}
												else
												{
													if (slot.pressed)
													{
														if (type == Stockpile.SlotType.Shipment)
														{
															if (slot.selected)
															{
																request.shipment_index_dst = default;
															}
															else
															{
																request.shipment_index_dst = slot.slot_id;
																slot.stockpile.TryRemap(request.shipment_index_dst, ref request.item_indices_src, ref request.item_amounts_src);
																request.count = Maths.Clamp(request.count, (ushort)1, slot.stockpile.GetMaxCount(request.shipment_index_dst, request.item_indices_src, request.item_amounts_src));
															}

															//request.shipment_index_dst = slot.selected ? default : slot.slot_id;
															//request.count = 1;
														}
														else
														{
															if (slot.selected)
															{
																request.item_indices_src[slot.arg_index] = default;
																request.item_amounts_src[slot.arg_index] = 0;
															}
															else
															{
																request.item_indices_src[slot.arg_index] = slot.slot_id;
																request.item_amounts_src[slot.arg_index] = 1;
															}

															//if (slot.arg_index != -1)
															//{

															//	//request.item_indices_src[slot.arg_index] = slot.selected ? default : slot.slot_id;
															//	//request.item_amounts_src[slot.arg_index] = (ushort)slot.amount;
															//}
														}
													}
												}
											}
										}
									};

									// TODO: shitty validation, do it properly
									can_submit = request.count > 0
									&& request.shipment_index_dst.IsValid(Stockpile.SlotType.Shipment)
									&& !request.item_indices_src.IsEmpty();
									if (can_submit)
									{
										ref var shipment = ref stockpile_ships[request.shipment_index_dst.Index];
										if (shipment.IsValid())
										{
											can_submit &= request.item_indices_src.GetFilledCount() == stockpile_ships[request.shipment_index_dst.Index].items.GetFilledCount()
											&& request.count <= stockpile_data.GetMaxCount(request.shipment_index_dst, request.item_indices_src, request.item_amounts_src)
											//&& request.h_container == shipment.h_container
											&& stockpile_data.CanMerge(request.shipment_index_dst, request.item_indices_src);
										}
										else
										{
											can_submit &= request.h_container.IsValid();
										}
									}
								}
								break;

								case Stockpile.Request.Type.Unpack:
								{
									ref var request = ref edit_request_base.AsRequestType<Stockpile.Request.Unpack>();

									dg_filter = static (ref request_base, slot) =>
									{
										ref var request = ref request_base.AsRequestType<Stockpile.Request.Unpack>();
										if (request.IsNotNull())
										{
											var type = slot.slot_id.Type;
											if (request.shipment_index_src.raw == 0)
											{
												slot.enabled = slot.valid && slot.slot_id.Type == Stockpile.SlotType.Shipment;
											}
											else
											{
												ref var shipment = ref slot.stockpile.shipments[request.shipment_index_src.Index];

												if (type == Stockpile.SlotType.Shipment)
												{
													slot.selected = slot.slot_id == request.shipment_index_src;
													slot.enabled = slot.selected;
												}
												else if (type == Stockpile.SlotType.Item)
												{
													if (slot.valid)
													{
														slot.enabled = slot.stockpile.Contains(request.shipment_index_src, slot.slot_id, out slot.arg_index);
														slot.selected = slot.enabled && request.item_indices_dst.Contains(slot.slot_id);
													}
													else
													{
														slot.enabled = request.item_indices_dst.GetRefOfValueOrEmpty(slot.slot_id, out slot.arg_index, out slot.selected).IsNotNull()
															&& slot.arg_index != -1 && shipment.items[slot.arg_index].IsValid();

														if (slot.enabled)
														{
															slot.slot_id_preview = request.shipment_index_src;
															slot.slot_id_preview_subindex = (byte)slot.arg_index;
														}
													}
												}
												else
												{
													slot.enabled = false;
												}
											}
										}
									};

									dg_gui = static (ref request_base, slot) =>
									{
										ref var request = ref request_base.AsRequestType<Stockpile.Request.Unpack>();
										if (request.IsNotNull())
										{
											slot.selectable = slot.enabled;
											if (slot.enabled)
											{
												var type = slot.slot_id.Type;
												if (request.shipment_index_src.raw != 0)
												{
													ref var shipment = ref slot.stockpile.shipments[request.shipment_index_src.Index];

													if (slot.selected || slot.hovered)
													{
														slot.color_frame = type == Stockpile.SlotType.Shipment ? GUI.col_src : GUI.col_dst;

														var amount_tmp = (int)request.count;
														if (GUI.ScrollInput(slot.rect, ref amount_tmp, 1, -50000, 50000))
														{
															var amount_max = slot.stockpile.GetQuantity(request.shipment_index_src);
															request.count = (ushort)Maths.Clamp(Maths.Wrap(amount_tmp, 1, amount_max + 1), 1, amount_max);
														}

														if (type == Stockpile.SlotType.Shipment)
														{
															slot.amount_add = -request.count;
														}
														else
														{
															if (slot.arg_index != -1)
															{
																slot.amount_add = request.count * ((int)shipment.items[slot.arg_index].quantity);
																slot.preview = !slot.valid;
															}
														}
													}
												}

												if (slot.pressed)
												{
													if (type == Stockpile.SlotType.Shipment)
													{
														request.shipment_index_src = slot.selected ? default : slot.slot_id;
														request.item_indices_dst = default;
														request.count = 1;
													}
													else
													{
														if (slot.arg_index != -1)
														{
															request.item_indices_dst[slot.arg_index] = slot.selected ? default : slot.slot_id;
														}
													}
												}
											}
										}
									};

									// TODO: shitty validation, do it properly
									can_submit = request.count > 0
									&& request.shipment_index_src.IsValid(Stockpile.SlotType.Shipment) && request.count <= stockpile_data.GetQuantity(request.shipment_index_src)
									&& request.item_indices_dst.GetFilledCount() == stockpile_ships[request.shipment_index_src.Index].items.GetFilledCount();
								}
								break;

								case Stockpile.Request.Type.Move:
								{
									ref var request = ref edit_request_base.AsRequestType<Stockpile.Request.Move>();

									dg_filter = static (ref request_base, slot) =>
									{
										ref var request = ref request_base.AsRequestType<Stockpile.Request.Move>();
										if (request.IsNotNull())
										{
											if (request.slot_src.raw == 0)
											{
												slot.enabled = slot.valid;
												slot.arg_index = 0;
											}
											else
											{
												slot.enabled = slot.stockpile.CanMove(request.slot_src, slot.slot_id);
												if (slot.enabled)
												{
													slot.enabled &= request.slot_src_dst.GetRefOfValueOrEmpty(slot.slot_id, out slot.arg_index, out slot.selected).IsNotNull();
													if (!slot.valid)
													{
														slot.slot_id_preview = request.slot_src;
													}
												}
											}
										}
									};

									dg_gui = static (ref request_base, slot) =>
									{
										ref var request = ref request_base.AsRequestType<Stockpile.Request.Move>();
										if (request.IsNotNull())
										{
											slot.selectable = slot.enabled;
											if (slot.enabled)
											{
												if (request.slot_src.raw != 0)
												{
													if (slot.selected || slot.hovered)
													{
														//slot.color_frame = slot.arg_index == 0 ? GUI.col_src : GUI.col_dst; // GUI.col_input;
														slot.color_frame = slot.arg_index == 0 ? GUI.col_input : GUI.col_output;

														var amount_tmp = (int)request.amount;
														if (GUI.ScrollInput(slot.rect, ref amount_tmp, 1, -50000, 50000))
														{
															var amount_max = slot.stockpile.GetQuantity(request.slot_src);
															request.amount = (ushort)Maths.Clamp(Maths.Wrap(amount_tmp, 1, amount_max + 1), 1, amount_max);
														}

														slot.amount_add = slot.arg_index == 0 ? -request.amount : request.amount;
														slot.preview = !slot.valid;
													}
												}

												if (slot.pressed)
												{
													request.slot_src_dst[slot.arg_index] = slot.selected ? default : slot.slot_id;
													request.slot_src_dst.Compact();

													if (slot.arg_index == 0)
													{
														request.amount = (ushort)slot.stockpile.GetQuantity(request.slot_src);
													}
												}
											}
										}
									};

									can_submit = true;
									can_submit &= request.slot_src.IsValid();
									can_submit &= request.slot_dst.IsValid();
									can_submit &= request.amount > 0;
									auto_submit = true;
								}
								break;

								case Stockpile.Request.Type.Send:
								{
									ref var request = ref edit_request_base.AsRequestType<Stockpile.Request.Send>();

									//item_enable = false;
									//ship_enable = true;
									//char_enable = false;

									//request.h_stockpile_src = this.stockpile.h_stockpile;

									//can_submit = true;

									//args_ships = new Span<IStockpile.SlotID>(ref request.shipment_index_src);
									//if (request.shipment_index_src.Type == Stockpile.SlotType.Shipment)
									//{
									//	ref var shipment_src = ref stockpile_ships.GetRefAtIndexOrNull(request.shipment_index_src.Index);
									//	if (shipment_src.IsNotNull() && shipment_src.IsValid())
									//	{

									//	}
									//	else
									//	{
									//		request.shipment_index_src = default;
									//		can_submit = false;
									//	}
									//}
									//else can_submit = false;
								}
								break;

								case Stockpile.Request.Type.Load:
								{
									ref var request = ref edit_request_base.AsRequestType<Stockpile.Request.Load>();

									dg_filter = static (ref request_base, slot) =>
									{
										ref var request = ref request_base.AsRequestType<Stockpile.Request.Load>();
										if (request.IsNotNull())
										{
											//slot.enabled = slot.slot_id.IsValid(Stockpile.SlotType.Shipment)
											//&& (request.entity.IsValid() ? !slot.valid : slot.valid);

											if (request.entity.IsValid())
											{
												slot.enabled = slot.slot_id.IsValid(request.slot_type);
											}
											else
											{
												slot.enabled = slot.slot_id.IsValid(Stockpile.SlotType.Shipment)
												&& (request.entity.IsValid() ? !slot.valid : slot.valid);
											}

											//switch (slot.slot_id.Type)
											//{
											//	case Stockpile.SlotType.Item:
											//	{
											//		slot.enabled = slot.slot_id.IsValid(Stockpile.SlotType.Item) && request.entity.IsValid()
											//	}
											//	break;

											//	case Stockpile.SlotType.Shipment:
											//	{

											//	}
											//	break;

											//	default:
											//	{

											//	}
											//	break;
											//}

											slot.selected = slot.enabled && request.slot_index == slot.slot_id;

											//if (request.slot_index.raw == 0)
											//{
											//	slot.enabled = slot.valid;
											//	slot.arg_index = 0;
											//}
											//else
											//{
											//	//slot.enabled = slot.stockpile.CanMove(request.slot_src, slot.slot_id);
											//	//if (slot.enabled)
											//	//{
											//	//	slot.enabled &= request.slot_src_dst.GetRefOfValueOrEmpty(slot.slot_id, out slot.arg_index, out slot.selected).IsNotNull();
											//	//	if (!slot.valid)
											//	//	{
											//	//		slot.slot_id_preview = request.slot_src;
											//	//	}
											//	//}
											//}
										}
									};

									dg_gui = static (ref request_base, slot) =>
									{
										ref var request = ref request_base.AsRequestType<Stockpile.Request.Load>();
										if (request.IsNotNull())
										{
											slot.selectable = slot.enabled;
											if (slot.enabled)
											{
												if (request.slot_index.raw != 0)
												{
													if (slot.selected || slot.hovered)
													{
														////slot.color_frame = slot.arg_index == 0 ? GUI.col_src : GUI.col_dst; // GUI.col_input;
														//slot.color_frame = slot.arg_index == 0 ? GUI.col_input : GUI.col_output;

														//var amount_tmp = (int)request.amount;
														//if (GUI.ScrollInput(slot.rect, ref amount_tmp, 1, -50000, 50000))
														//{
														//	var amount_max = slot.stockpile.GetQuantity(request.slot_src);
														//	request.amount = (ushort)Maths.Clamp(Maths.Wrap(amount_tmp, 1, amount_max + 1), 1, amount_max);
														//}

														//slot.amount_add = slot.arg_index == 0 ? -request.amount : request.amount;
														//slot.preview = !slot.valid;
													}
												}

												if (slot.pressed)
												{
													request.slot_index = slot.selected ? default : slot.slot_id;
													if (!request.entity.IsValid()) request.slot_type = slot.selected ? default : slot.slot_id.Type;
												}
											}
										}
									};

									//item_enable = false;
									//ship_enable = true;
									//char_enable = false;

									////request.h_stockpile_src = stockpile.h_stockpile;

									//args_ships = new Span<IStockpile.SlotID>(ref request.slot_index);

									switch (request.slot_type)
									{
										case Stockpile.SlotType.Item:
										{
											can_submit = request.slot_index.IsValid(Stockpile.SlotType.Item)
											&& request.entity.IsAlive()
											&& stockpile_items.GetRefAtIndexOrNull(request.slot_index.Index).IsNotNull();
										}
										break;

										case Stockpile.SlotType.Shipment:
										{
											can_submit = request.slot_index.IsValid(Stockpile.SlotType.Shipment)
											&& request.entity.IsAlive() ? true : request.pos != default
											&& stockpile_ships.GetRefAtIndexOrNull(request.slot_index.Index).IsValid();
										}
										break;
									}

									auto_submit = true;
								}
								break;

								default:
								{
									//enable_selection = false;

									//alpha_selected = 1.00f;
									//alpha_default = 1.00f;
								}
								break;
							}
							#endregion

							if (this.stockpile.flags.HasAny(Stockpile.Data.Flags.Hide_Unowned))
							{
								var addresses = stockpile_data.addresses;

								var num_slots_owned_item = 0;
								for (byte i = 0; i < stockpile_items.Length; i++)
								{
									var slot_id = new IStockpile.SlotID(i, Stockpile.SlotType.Item);
									if (addresses.ContainsPair(slot_id.raw, h_address_filter)) num_slots_owned_item++;
								}

								var num_slots_owned_ship = 0;
								for (byte i = 0; i < stockpile_ships.Length; i++)
								{
									var slot_id = new IStockpile.SlotID(i, Stockpile.SlotType.Shipment);
									if (addresses.ContainsPair(slot_id.raw, h_address_filter)) num_slots_owned_ship++;
								}

								var num_slots_owned_item_rem = this.stockpile.capacity_items - num_slots_owned_item;
								var num_slots_owned_ship_rem = this.stockpile.capacity_shipments - num_slots_owned_ship;
								var ent_stockpile_tmp = this.ent_stockpile;

								dg_gui_skipped = (ref grid, h_address, flags, slot) =>
								{
									if (slot.h_address_slot) return;

									var type = slot.slot_id.Type;
									if (type == Stockpile.SlotType.Item)
									{
										if (slot.stockpile.items[slot.slot_id.Index].IsValid())
										{
											return;
										}
										else if (num_slots_owned_item_rem > 0)
										{
											num_slots_owned_item_rem--;
										}
										else
										{
											return;
										}
									}
									else if (type == Stockpile.SlotType.Shipment)
									{
										if (slot.stockpile.shipments[slot.slot_id.Index].IsValid())
										{
											return;
										}
										else if (num_slots_owned_ship_rem > 0)
										{
											num_slots_owned_ship_rem--;
										}
										else
										{
											return;
										}
									}

									using (var cell = grid.Push(slot.cell_size, slot.slot_id.raw))
									{
										var rect = cell.group.GetOuterRect();
										using (var button = GUI.CustomButton.New(id: "buy.slot"u8,
											size: GUI.Rm,
											padding: new(4),
											enabled: can_manage && h_address && !edit_request.HasValue,
											sound: GUI.sound_button,
											sound_volume: 0.30f))
										{
											GUI.Draw9Slice(button.hovered ? GUI.tex_slot_simple_filled : GUI.tex_slot_simple_filled, new Vector4(4), button.bb, color: button.hovered ? GUI.col_button_ok : GUI.col_white);
											//GUI.DrawSpriteCentered(new Sprite("ui_icon_lock", 16, 16, 0, 0), button.bb, GUI.Layer.Window, scale: 2.00f, color: GUI.col_white.WithAlpha(70));

											GUI.DrawTextCentered(slot.slot_id.Index, position: rect.GetPosition() + new Vector2(2, 0), pivot: new(0.50f, 0.50f), font: GUI.Font.Editia, size: type == Stockpile.SlotType.Item ? 16 : 24, color: GUI.font_color_desc.WithAlpha(100), layer: GUI.Layer.Window, format: "00");

											if (button.pressed)
											{
												var rpc = new EditSlotRPC();
												rpc.action = EditSlotRPC.Action.Add;
												rpc.slot = slot.slot_id;
												rpc.h_address = h_address;
												rpc.Send(ent_stockpile_tmp);

												//Sound.PlayGUI("buy", volume: 0.50f);
												//Sound.PlayGUI("buy", volume: 0.50f);
											}
										}

										if (GUI.IsItemHovered())
										{
											using (var tooltip = GUI.Tooltip.New())
											{
												GUI.TextShaded("Claim this stockpile slot."u8);
												if (can_manage && !h_address)
												{
													GUI.TextShaded("<< You need to register a postal\naddress for this stockpile first!"u8, color: GUI.font_color_error);
												}
											}
										}
									}

								};
							}

							#region Grid					
							using (var group_left = GUI.Group.New(size: new Vector2(group_size.X + 22 + 8, GUI.RmY)))
							{
								using (var scrollbox = GUI.Scrollbox.New("scroll.items"u8, size: GUI.Rm, padding: new(4)))
								{
									scrollbox.group_frame.DrawBackground(GUI.tex_window, inner: true);

									using (var group_main = GUI.Group.New(size: GUI.Rm))
									{
										using (GUI.ID.Push(this.stockpile.h_stockpile))
										{
											var grid = GUI.Grid.New(GUI.Rm, item_size);
											{
												StockpileGUI.DrawGrid(ref grid, h_stockpile, dg_filter: dg_filter, dg_gui: dg_gui, dg_gui_skipped: dg_gui_skipped, h_address: h_address_filter, flags: this.stockpile.flags);
											}
											grid.Dispose();
										}
									}
								}
							}
							#endregion

							GUI.SameLine();

							using (var group_right = GUI.Group.New(size: GUI.Av))
							{
								group_right.DrawBackground(GUI.tex_panel);

								using (GUI.Group.New(size: new(GUI.RmX, 48)))
								{
									if (this.stockpile.request_types.Has(Stockpile.Request.Type.Move))
									{
										if (GUI.DrawIconButton("move"u8, tex_icons_depot.WithFrameX((uint)Stockpile.Request.Type.Move), size: new(48, 48), scale: 2, color: edit_request?.type == Stockpile.Request.Type.Move ? GUI.col_button_highlight : GUI.col_button))
										{
											if (edit_request?.type == Stockpile.Request.Type.Move)
											{
												Reset();
											}
											else
											{
												Reset();

												var request = new Stockpile.Request.Move()
												{

												};
												edit_request = request.AsRequestBase();
											}
										}
										GUI.DrawHoverTooltip("Move..."u8);
										GUI.SameLine();
									}

									if (this.stockpile.request_types.Has(Stockpile.Request.Type.Pack))
									{
										if (GUI.DrawIconButton("pack"u8, tex_icons_depot.WithFrameX((uint)Stockpile.Request.Type.Pack), size: new(48, 48), scale: 2, color: edit_request?.type == Stockpile.Request.Type.Pack ? GUI.col_button_highlight : GUI.col_button))
										{
											if (edit_request?.type == Stockpile.Request.Type.Pack)
											{
												Reset();
											}
											else
											{
												Reset();

												var request = new Stockpile.Request.Pack()
												{
													count = 1,
												};
												edit_request = request.AsRequestBase();

												//edit_shipment = new Shipment.Data();
											}
										}
										GUI.DrawHoverTooltip("Pack items into..."u8);
										GUI.SameLine();
									}

									if (this.stockpile.request_types.Has(Stockpile.Request.Type.Unpack))
									{
										if (GUI.DrawIconButton("unpack"u8, tex_icons_depot.WithFrameX((uint)Stockpile.Request.Type.Unpack), size: new(48, 48), scale: 2, color: edit_request?.type == Stockpile.Request.Type.Unpack ? GUI.col_button_highlight : GUI.col_button))
										{
											if (edit_request?.type == Stockpile.Request.Type.Unpack)
											{
												Reset();
											}
											else
											{
												Reset();

												var request = new Stockpile.Request.Unpack()
												{
													count = 1,
												};
												edit_request = request.AsRequestBase();
											}
										}
										GUI.DrawHoverTooltip("Unpack shipment into..."u8);
										GUI.SameLine();
									}

									if (this.stockpile.request_types.Has(Stockpile.Request.Type.Send))
									{
										if (GUI.DrawIconButton("send"u8, tex_icons_depot.WithFrameX((uint)Stockpile.Request.Type.Send), size: new(48, 48), scale: 2, color: edit_request?.type == Stockpile.Request.Type.Send ? GUI.col_button_highlight : GUI.col_button))
										{
											if (edit_request?.type == Stockpile.Request.Type.Send)
											{
												Reset();
											}
											else
											{
												Reset();

												var request = new Stockpile.Request.Send()
												{
													count = 1,
												};
												edit_request = request.AsRequestBase();
											}
										}
										GUI.DrawHoverTooltip("Send to..."u8);
										GUI.SameLine();
									}

									if (this.stockpile.request_types.Has(Stockpile.Request.Type.Load))
									{
										if (region.IsNotNull())
										{
											if (GUI.DrawIconButton("load"u8, tex_icons_depot.WithFrameX((uint)Stockpile.Request.Type.Load), size: new(48, 48), scale: 2, color: edit_request?.type == Stockpile.Request.Type.Load ? GUI.col_button_highlight : GUI.col_button))
											{
												if (edit_request?.type == Stockpile.Request.Type.Load)
												{
													Reset();
												}
												else
												{
													Reset();

													var request = new Stockpile.Request.Load()
													{

													};
													edit_request = request.AsRequestBase();
												}
											}
											GUI.DrawHoverTooltip("Load/Unload..."u8);
										}
									}

									if (GUI.ShowDebugGUI)
									{

										if (GUI.DrawIconButton("dev.refill"u8, tex_icons_depot.WithFrameX(0), size: new(48, 48), scale: 2, color: GUI.col_button_debug))
										{
											var rpc = new DEV_RefillRPC()
											{

											};
											rpc.Send(this.ent_stockpile);
										}
										GUI.DrawHoverTooltip("DEV: Refill"u8);
									}

								}

								GUI.SeparatorThick();

								can_submit &= can_manage;

								using (GUI.Group.New(size: GUI.Rm))
								{
									if (edit_request_base.IsNotNull() && this.stockpile.request_types.Has(edit_request_base.type))
									{
										switch (edit_request_base.type)
										{
											case Stockpile.Request.Type.Pack:
											{
												ref var request = ref edit_request_base.AsRequestType<Stockpile.Request.Pack>();

												//ref var edit_shipment_tmp = ref edit_shipment.GetRefOrNull();
												//if (edit_shipment_tmp.IsNotNull())
												{
													if (GUI.AssetInput2("edit.stockpile.containers"u8, ref request.h_container, size: new(256, 64), show_label: false, close_on_select: false, tab_height: 64,
													filter: (x) => x.data.h_prefab.id != 0,
													draw: (asset, group, is_title) =>
													{
														if (asset != null && asset.data.h_prefab.TryGetPrefab(out var prefab))
														{
															//using (var group_prefab = GUI.Group.New(size: new(GUI.RmX, GUI.RmY)))
															//{
															GUI.DrawPrefabSmall(asset.data.h_prefab, size: new(0, GUI.RmY), 1, false, pivot: new(0.00f, 0.50f), padding: new(8, 4, 4, 4));
															//}

															GUI.TitleCentered(asset.data.name, rect: group.GetInnerRect(), pivot: new(1.00f, 0.00f), offset: new(-8, 4), size: 16);
														}
														else
														{
															GUI.TitleCentered("<container>"u8, pivot: new(1.00f, 0.00f), offset: new(-8, 4), size: 16);
														}
													}))
													{

													}
												}

												if (show_debug)
												{
													GUI.DrawStyledEditorForType(ref request, row_size: new(GUI.RmX, 32));
												}
											}
											break;

											case Stockpile.Request.Type.Unpack:
											{
												ref var request = ref edit_request_base.AsRequestType<Stockpile.Request.Unpack>();
												if (show_debug)
												{
													GUI.DrawStyledEditorForType(ref request, row_size: new(GUI.RmX, 32));
												}
											}
											break;

											case Stockpile.Request.Type.Move:
											{
												ref var request = ref edit_request_base.AsRequestType<Stockpile.Request.Move>();
												if (show_debug)
												{
													GUI.DrawStyledEditorForType(ref request, row_size: new(GUI.RmX, 32));
												}
											}
											break;

											case Stockpile.Request.Type.Send:
											{
												ref var request = ref edit_request_base.AsRequestType<Stockpile.Request.Send>();
												ref var handle = ref request.h_stockpile_dst;

												var h_stockpile_current = this.stockpile.h_stockpile;

												//if (GUI.AssetInput2("stockpile.send.dst", ref request.h_stockpile_dst, new Vector2(256 - 64, 32), show_label: false,
												if (GUI.AssetInput2("stockpile.send.dst"u8, ref request.h_stockpile_dst, new Vector2(GUI.RmX, 32), show_label: false,
												filter: (x) =>
												{
													return x != h_stockpile_current && (x.data.h_address == h_address_current);
												},
												draw: (asset, group, is_title) =>
												{
													if (asset != null)
													{
														GUI.TitleCentered(asset.data.name, pivot: new(0.00f, 0.50f), offset: new(8, 0));
													}
													else
													{
														GUI.TitleCentered("<null>"u8, pivot: new(0.00f, 0.50f), offset: new(8, 0));
													}
												}))
												{

												}

												ref var stockpile_dst_data = ref handle.GetData();

												using (var group = GUI.Group.New(size: GUI.Rm - new Vector2(0, 44)))
												{
													using (var group_l = GUI.Group.New(size: new(64, GUI.RmY)))
													{
														group_l.DrawBackground(GUI.tex_frame);
													}

													GUI.SameLine();


													using (var group_r = GUI.Group.New(size: GUI.Rm))
													{
														group_r.DrawBackground(GUI.tex_panel);

														if (GUI.DrawButton("View"u8, new(64, 40), enabled: stockpile_dst_data.IsNotNull()))
														{
															//WorldMap.FocusLocation(stockpile_dst_data.h_address.GetLocationHandle());
														}
														GUI.DrawHoverTooltip("View this location on the world map."u8);

														GUI.SeparatorThick();
													}
												}

												if (show_debug)
												{
													GUI.DrawStyledEditorForType(ref request, row_size: new(GUI.RmX, 32));
												}
											}
											break;

											case Stockpile.Request.Type.Load:
											{
												ref var request = ref edit_request_base.AsRequestType<Stockpile.Request.Load>();
												ref var body = ref this.ent_stockpile.GetComponent<Body.Data>();
												if (body.IsNotNull())
												{
													var bb = body.GetAABB();
													var bb_w = AABB.Centered(this.transform.GetInterpolatedPosition(), bb.GetSize());
													var bb_c = region.WorldToCanvas(bb_w);

													//if (selecting)
													{
														GUI.DrawRect(bb_c, Color32BGRA.Orange.WithAlphaMult(0.65f), 2.00f);

														//if (request.slot_index.IsValid())
														//{

														//}

														Span<OverlapBBResult> results = stackalloc OverlapBBResult[16];
														if (region.TryOverlapBBAll(bb_w, ref results, mask: Physics.Layer.Shipment | Physics.Layer.Resource))
														{
															foreach (ref var result in results)
															{
																var bb_result = result.GetAABB();

																var is_hovered = !GUI.IsHovered && bb_result.ContainsPoint(mouse.position);
																var is_selected = request.entity == result.entity;

																if (is_hovered && mouse.GetKeyDown(Mouse.Key.Left))
																{
																	if (is_selected)
																	{
																		request.entity = default;
																		request.slot_type = Stockpile.SlotType.Undefined;
																	}
																	else
																	{
																		request.entity = result.entity;
																		request.slot_type = result.layer.HasAny(Physics.Layer.Shipment) ? Stockpile.SlotType.Shipment : Stockpile.SlotType.Item;
																	}
																}

																//if (is_selected)
																//{
																//	edit_shipment = shipment;
																//}
																//else
																{
																	GUI.DrawEntity(result.entity, color: is_selected ? Color32BGRA.Green.WithAlphaMult(is_hovered ? 1.00f : 0.70f) : Color32BGRA.Yellow.WithAlphaMult(is_hovered ? 0.90f : 0.25f));
																	GUI.DrawRect(region.WorldToCanvas(bb_result), color: is_selected ? Color32BGRA.Green.WithAlphaMult(is_hovered ? 0.70f : 0.50f) : Color32BGRA.Yellow.WithAlphaMult(is_hovered ? 0.70f : 0.45f), 1.00f);

																	//GUI.DrawEntity(result.entity, color: Color32BGRA.Yellow.WithAlphaMult(is_hovered ? 0.90f : 0.25f));
																	//GUI.DrawRect(region.WorldToCanvas(bb_result.Grow(0.25f)), color: Color32BGRA.Yellow.WithAlphaMult(is_hovered ? 0.70f : 0.45f), 1.00f);
																}

																//ref var shipment = ref result.entity.GetComponent<Shipment.Data>();
																//if (shipment.IsNotNull())
																//{
																//	var is_hovered = bb_result.ContainsPoint(mouse.position);
																//	var is_selected = request.entity == result.entity;

																//	if (is_hovered && mouse.GetKeyDown(Mouse.Key.Left))
																//	{
																//		request.entity = is_selected ? default : result.entity;
																//	}

																//	//if (is_selected)
																//	//{
																//	//	edit_shipment = shipment;
																//	//}
																//	//else
																//	{
																//		GUI.DrawEntity(result.entity, color: is_selected ? Color32BGRA.Green.WithAlphaMult(is_hovered ? 1.00f : 0.70f) : Color32BGRA.Yellow.WithAlphaMult(is_hovered ? 0.90f : 0.25f));
																//		GUI.DrawRect(region.WorldToCanvas(bb_result), color: is_selected ? Color32BGRA.Green.WithAlphaMult(is_hovered ? 0.70f : 0.50f) : Color32BGRA.Yellow.WithAlphaMult(is_hovered ? 0.70f : 0.45f), 1.00f);

																//		//GUI.DrawEntity(result.entity, color: Color32BGRA.Yellow.WithAlphaMult(is_hovered ? 0.90f : 0.25f));
																//		//GUI.DrawRect(region.WorldToCanvas(bb_result.Grow(0.25f)), color: Color32BGRA.Yellow.WithAlphaMult(is_hovered ? 0.70f : 0.45f), 1.00f);
																//	}
																//}
																//else
																//{
																//	ref var resource = ref result.entity.GetComponent<Resource.Data>();
																//	if (resource.IsNotNull())
																//	{
																//		var is_hovered = bb_result.ContainsPoint(mouse.position);
																//		var is_selected = request.entity == result.entity;

																//		if (is_hovered && mouse.GetKeyDown(Mouse.Key.Left))
																//		{
																//			request.entity = is_selected ? default : result.entity;
																//		}

																//		//if (is_selected)
																//		//{
																//		//	edit_shipment = shipment;
																//		//}
																//		//else
																//		{
																//			GUI.DrawEntity(result.entity, color: is_selected ? Color32BGRA.Green.WithAlphaMult(is_hovered ? 1.00f : 0.70f) : Color32BGRA.Yellow.WithAlphaMult(is_hovered ? 0.90f : 0.25f));
																//			GUI.DrawRect(region.WorldToCanvas(bb_result), color: is_selected ? Color32BGRA.Green.WithAlphaMult(is_hovered ? 0.70f : 0.50f) : Color32BGRA.Yellow.WithAlphaMult(is_hovered ? 0.70f : 0.45f), 1.00f);

																//			//GUI.DrawEntity(result.entity, color: Color32BGRA.Yellow.WithAlphaMult(is_hovered ? 0.90f : 0.25f));
																//			//GUI.DrawRect(region.WorldToCanvas(bb_result.Grow(0.25f)), color: Color32BGRA.Yellow.WithAlphaMult(is_hovered ? 0.70f : 0.45f), 1.00f);
																//		}
																//	}
																//}
															}
														}

														if (request.slot_index.IsValid(Stockpile.SlotType.Shipment))
														{
															//App.WriteLine("hi");

															ref var shipment = ref stockpile_ships.GetRefAtIndexOrNull(request.slot_index.Index);
															if (shipment.IsNotNull() && shipment.h_container.TryGetDefinition(out var container_asset) && container_asset.data.h_prefab.TryGetPrefab(out var container_prefab) && container_prefab != null)
															{
																var is_placeable = true;
																var is_obstructed = false;
																var is_placed = request.pos != Vector2.Zero;

																var pos = is_placed ? request.pos : mouse.GetInterpolatedPosition();
																var bb_prefab = AABB.Centered(pos, container_asset.data.size);
																pos = bb_prefab.GetPosition();

																var transform_dst = new Transform.Data(pos, 0.00f, Vector2.One);

																is_placeable &= bb_w.ContainsPoint(bb_prefab.a) && bb_w.ContainsPoint(bb_prefab.b);
																if (is_placeable)
																{
																	if (region.TryOverlapBB(bb_prefab.GetPosition(), bb_prefab.GetSize(), out var result, mask: Physics.Layer.Solid))
																	{
																		is_obstructed = true;
																	}
																}
																is_placeable &= !is_obstructed;

																var color_preview = Color32BGRA.Red;
																if (is_obstructed)
																{
																	color_preview = Color32BGRA.Red;
																}
																else if (is_placeable)
																{
																	color_preview = Color32BGRA.Green;
																}

																//App.WriteLine("ok");

																//if (pos != default)
																{
																	GUI.DrawPrefabAsEntity(container_prefab, in transform_dst, color: color_preview.WithAlphaMult(is_placed ? 0.70f : 0.50f));
																	//GUI.DrawEntity(request.entity, in transform_dst, color: color_preview.WithAlphaMult(is_placeable ? 0.90f : 0.25f));
																	GUI.DrawRect(region.WorldToCanvas(bb_prefab), color: color_preview.WithAlphaMult(is_placed ? 0.60f : 0.40f), 1.00f);
																}

																if (!GUI.IsHovered)
																{
																	if (is_placeable && mouse.GetKeyDown(Mouse.Key.Left))
																	{
																		request.pos = pos;
																	}
																	else if (mouse.GetKeyDown(Mouse.Key.Right))
																	{
																		request.pos = default;
																		request.slot_index = default;
																	}
																}
															}
														}

														//{
														//	ref var shipment = ref request.entity.GetComponent<Shipment.Data>();
														//	if (shipment.IsNotNull())
														//	{
														//		GUI.DrawEntity(request.entity, color: Color32BGRA.Yellow.WithAlphaMult(is_hovered ? 0.90f : 0.25f));
														//		GUI.DrawRect(region.WorldToCanvas(bb_result.Grow(0.25f)), color: Color32BGRA.Yellow.WithAlphaMult(is_hovered ? 0.70f : 0.45f), 1.00f);
														//	}
														//	else
														//	{

														//	}
														//}

														GUI.DrawCross(region.WorldToCanvas(request.pos), color: GUI.col_white, radius: 12.00f);

														if (!GUI.IsHovered)
														{
															if (mouse.GetKeyDown(Mouse.Key.Right))
															{
																request.pos = default;
																request.slot_index = default;
																request.slot_type = default;
																request.entity = default;
															}
														}
													}

													if (show_debug)
													{
														GUI.DrawStyledEditorForType(ref request, row_size: new(GUI.RmX, 32));
													}


												}
											}
											break;
										}

										GUI.SeparatorThick();

										if (GUI.DrawButton("Submit"u8, new(100, 40), enabled: can_submit, color: GUI.col_button_ok) || (can_submit && auto_submit))
										{
											var rpc = new Economy.RequestRPC();
											rpc.request_base = edit_request_base;
											rpc.Send(this.ent_stockpile);

											switch (edit_request_base.type)
											{
												case Stockpile.Request.Type.Pack:
												{
													Reset();

													ref var request = ref edit_request.GetRefOrDefault().AsRequestType<Stockpile.Request.Pack>();
													request = new();
													request.count = 1;
												}
												break;

												case Stockpile.Request.Type.Unpack:
												{
													Reset();

													ref var request = ref edit_request.GetRefOrDefault().AsRequestType<Stockpile.Request.Unpack>();
													request = new();
													request.count = 1;
												}
												break;

												case Stockpile.Request.Type.Move:
												{
													Reset();

													ref var request = ref edit_request.GetRefOrDefault().AsRequestType<Stockpile.Request.Move>();
													request = new();
												}
												break;

												case Stockpile.Request.Type.Send:
												{
													Reset();

													ref var request = ref edit_request.GetRefOrDefault().AsRequestType<Stockpile.Request.Send>();
													request = new();
												}
												break;

												case Stockpile.Request.Type.Load:
												{
													ref var request = ref edit_request.GetRefOrDefault().AsRequestType<Stockpile.Request.Load>();
													if (request.entity.IsValid() || stockpile_ships.GetAtIndexOrDefault(request.slot_index.Index).count <= 1)
													{
														request = new();
													}
													else
													{
														request.pos = default;
													}
												}
												break;
											}
										}
									}
								}
							}
						}
						else
						{
							GUI.TitleCentered("<no stockpile linked>"u8, size: 32, pivot: new(0.50f, 0.50f), offset: new(0, -24), color: GUI.font_color_title.WithAlphaMult(0.50f));
						}

						//GUI.DrawText($"{ts.GetMilliseconds():0.0000} ms", window.group.a);
					}
				}
			}
		}

		[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global)]
		public static void OnGUI(Entity entity,
		[Source.Owned] in Stockpile.Data stockpile,
		[Source.Owned] in Interactable.Data interactable,
		[Source.Owned] in Transform.Data transform,
		[Source.Owned, Optional] in Address.Data address)
		{
			if (stockpile.flags.HasNone(Stockpile.Data.Flags.Hidden) && interactable.IsActive())
			{
				var gui = new StockpileGUI()
				{
					ent_stockpile = entity,
					transform = transform,
					stockpile = stockpile,
					address = address
				};
				gui.Submit();
			}
		}
#endif
	}
}
