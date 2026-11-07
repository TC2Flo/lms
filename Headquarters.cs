namespace TC2.Base.Components
{
	public static partial class Headquarters
	{
		[Flags]
		public enum Flags: uint
		{
			None = 0u,

			Enable_Join = 1u << 0,
			Enable_Leave = 1u << 1,

			Manage_Members = 1u << 2,
			Manage_Leader = 1u << 3,
			Manage_Style = 1u << 4,
			Manage_Policies = 1u << 5,

			Allow_Create_Faction = 1u << 8,

			Show_Members = 1u << 16,
		}

		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Global | IComponent.Scope.Region)]
		public partial struct Data: IComponent
		{
			public Headquarters.Flags flags;

			//public Vec4f vec;
			//public AABB rect;

			//public Vec4f? nvec;
			//public AABB? nrect;
		}

		//[IEvent.Data]
		//public partial struct InfiltrationEvent(): IEvent
		//{
		//	public IOrigin.Handle h_origin;
		//}

		//[ISystem.AddFirst(ISystem.Mode.Single, ISystem.Scope.Region, order: 20), HasTag("initialized", true, Source.Modifier.Owned)]
		//public static void OnAddCompany(ISystem.Info info, ref Region.Data region, Entity entity,
		//[Source.Owned] ref Market.Data market,
		//[Source.Owned, Optional(true)] ref Company.Data company,
		//{
		//	if (!market.h_store.IsValid())
		//	{
		//		var store_data = new IStore.Data();
		//		store_data.listings = new Market.Listing[8];

		//		ref var map_info = ref region.GetMapInfo();
		//		ref var region_info = ref region.GetRegionInfo();
		//		App.WriteLine(region_info.map);

		//		store_data.name = $"{entity.GetName()} - {map_info.name}";
		//		store_data.h_location = map_info.h_location;

		//		if (company.IsNotNull())
		//		{
		//			store_data.h_company = company.h_company;
		//		}

		//		if (stockpile.IsNotNull())
		//		{
		//			store_data.h_stockpile = stockpile.h_stockpile;
		//		}

		//		var identifier = Asset.GenerateRandomIdentifier();
		//		var store_asset = IStore.Database.RegisterOrUpdate(identifier, null, Asset.Scope.World, region.GetID(), ref store_data, flags: Asset.Flags.Recycle);
		//		//store_asset.entity.AddRelation(store_data.h_location.GetEntity(), Relation.Type.Child);
		//		store_asset.Sync(true);

		//		market.h_store = store_asset;
		//		market.Sync(entity, true);
		//	}
		//}

//#if SERVER
//		[ISystem.Event<Headquarters.InfiltrationEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
//		public static void OnInfiltration(ISystem.Info info, Entity entity, ref Region.Data region, ref Headquarters.InfiltrationEvent ev,
//		[Source.Owned] ref Headquarters.Data headquarters, [Source.Owned] in Transform.Data transform, [Source.Owned] in Faction.Data faction)
//		{
//			ref var faction_data = ref faction.id.GetData(out var s_faction);
//			if (faction_data.IsNotNull())
//			{
//				faction_data.scout_count++;
//				s_faction.Sync();

//				Notification.Push(ref region, "Your faction has successfully acquired a new scout.", Color32BGRA.Green, lifetime: 15.00f, sound: "ui.alert.bwoing.00", volume: 0.50f, send_type: Net.SendType.Reliable, faction_id: faction.id);
//			}

//			//App.WriteLine($"{entity}: on hire spy; {ev.h_origin}");

//			//Notification.Push(ref region, $"{entity}: on hire spy; {ev.h_origin}, now you boom", Color32BGRA.Red, lifetime: 15.00f, volume: 0.50f);

//			//Explosion.Spawn(ref region, transform.position, (Entity ent_explosion, ref Explosion.Data explosion) =>
//			//{
//			//	explosion.radius = 20.00f;
//			//	explosion.power = 5.00f;
//			//	explosion.damage_type = Damage.Type.Explosion;
//			//	explosion.damage_entity = 4000.00f;
//			//	explosion.damage_terrain = 1000.00f;
//			//});
//		}
//#endif

		[ISystem.Event<IFaction.RefreshEvent>(ISystem.Mode.Single, ISystem.Scope.Global | ISystem.Scope.Region)]
		public static void OnRefreshEvent(ISystem.Info.Common info, ref Region.Data.Common region_common, Entity ent_faction, ref IFaction.RefreshEvent ev,
		[Source.Owned] ref Faction.Data faction, [Source.Owned] ref Company.Data company)
		{
			faction.id = ev.h_asset;
			company.h_company = ev.data.h_company;

#if SERVER
			faction.Sync(ent_faction, true);
			company.Sync(ent_faction, true);
#endif

			App.WriteLine($"Faction: OnRefreshEvent(); region: {region_common.GetID()}; ent: {ent_faction}; {ev.h_asset}", color: App.Color.Green);
		}



		public struct FoundFactionRPC: Net.IRPC<Headquarters.Data>
		{
			public FixedString32 name;
			public FixedString8 tag;

			public IOrganization.Handle h_organization;
			public ICompany.Flags company_flags;

			public Color32BGRA color_a;
			public Color32BGRA color_b;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Headquarters.Data data)
			{
				if (Constants.Factions.enable_found_faction)
				{
					Assert.Check(data.flags.HasAny(Flags.Allow_Create_Faction));
					//Assert.Check(rpc.record.GetFactionHandle());

					ref var player_data = ref rpc.connection.GetPlayer(out var player_asset);
					Assert.IsNotNull(ref player_data);
					Assert.Check(player_data.h_faction == 0);

					var faction_asset = Faction.Create(id: null,
						tag: this.tag,
						name: this.name,
						color_a: this.color_a,
						color_b: this.color_b,
						h_organization: this.h_organization,
						h_player: player_asset,
						h_character: player_data.h_character_main,
						flags: IFaction.Flags.Custom,
						company_flags: this.company_flags);
					Assert.IsNotNull(faction_asset);


					if (!rpc.record.GetFactionHandle())
					{
						rpc.entity.SetFaction(faction_asset);
					}

					player_asset.GetHandle().SetFaction(faction_asset);
					faction_asset.GetOrSpawnEntity(rpc.GetRegionID());

					//var oc_character = player.GetControlledCharacter();
					//if (oc_character.IsValid())
					//{
					//	ref var character = ref oc_character.data;
					//	ref var character_data = ref character.character_id.GetData(out ICharacter.Definition s_character);
					//	if (character_data.IsNotNull())
					//	{
					//		character_data.faction = faction_asset;
					//		s_character.Sync();
					//	}
					//}


					var faction_name_str = faction_asset.GetName();
					if (ICompany.TryGetImperialName(this.h_organization, this.company_flags, out var org_name))
					{
						faction_name_str = $"{faction_name_str} {org_name.ToString().Capitalize()}";
					}

					Notification.Push($"{faction_name_str} has been created!", faction_asset.data.color_a, 10.00f, "ui.alert.bwoing.00", send_type: Net.SendType.Reliable);
				}
			}
#endif
		}

		public struct EditFactionRPC: Net.IRPC<Headquarters.Data>
		{
			public Color32BGRA? color_a;
			public Color32BGRA? color_b;
			public IFaction.Policies? policies;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Headquarters.Data data)
			{
				var h_faction = rpc.entity.GetFactionHandle();
				ref var faction_data = ref h_faction.GetData(out var faction_asset);
				Assert.IsNotNull(ref faction_data);

				var h_player = rpc.GetSenderPlayerHandle();
				ref var player_data = ref h_player.GetData(out var player_asset);
				Assert.IsNotNull(ref player_data);

				Assert.Check(faction_data.IsLeader(h_player));

				var dirty = false;

				if (this.color_a.TryGetValueUnsafe(out var color_a_tmp))
				{
					color_a_tmp.a = 255;
					dirty |= faction_data.color_a.TryChange(color_a_tmp);
				}

				if (this.color_b.TryGetValueUnsafe(out var color_b_tmp))
				{
					color_b_tmp.a = 255;
					dirty |= faction_data.color_b.TryChange(color_b_tmp);
				}

				if (this.policies.TryGetValueUnsafe(out var policies_tmp))
				{
					// TODO: add editable flag mask
					dirty |= faction_data.policies.TryChange(policies_tmp);
				}

				if (dirty)
				{
					faction_asset.Sync(false);
				}
				//ref var player = ref rpc.connection.GetPlayer();
				//if (!player.IsNull() && player.h_faction != 0)
				//{
				//	ref var faction = ref rpc.entity.GetComponent<Faction.Data>();
				//	if (!faction.IsNull() && faction.id == player.h_faction)
				//	{
				//		ref var faction_data = ref faction.id.GetData();
				//		if (!faction_data.IsNull())
				//		{
				//			this.color_a.a = 255;
				//			this.color_b.a = 255;

				//			faction_data.color_a = this.color_a;
				//			faction_data.color_b = this.color_b;

				//			faction.id.Sync(false);
				//		}
				//	}
				//}
			}
#endif
		}

		public struct JoinFactionRPC: Net.IRPC<Headquarters.Data>
		{
#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Headquarters.Data data)
			{
				if (Constants.Factions.enable_join_faction && data.flags.HasAll(Headquarters.Flags.Enable_Join))
				{
					var h_faction = rpc.entity.GetFactionHandle();
					ref var faction_data = ref h_faction.GetData(out var faction_asset);
					Assert.IsNotNull(ref faction_data);

					var h_player = rpc.GetSenderPlayerHandle();
					ref var player_data = ref h_player.GetData(out var player_asset);
					Assert.IsNotNull(ref player_data);

					Assert.Check(faction_data.policies.HasAny(IFaction.Policies.Allow_Recruitment));
					Assert.Check(!player_data.h_faction);

					ref var player = ref rpc.connection.GetPlayerData();
					if (player.IsNotNull())
					{
						player.SetFaction(h_faction);
						player.ent_controlled.SetFaction(h_faction);

						ref var character_data = ref player.GetControlledCharacterData(out var h_character);
						if (character_data.IsNotNull())
						{
							character_data.faction = h_faction;
							h_character.Sync(false);
						}
					}

					//ref var player = ref rpc.connection.GetPlayerData();
					//if (!player.IsNull())
					//{
					//	ref var faction = ref rpc.entity.GetComponent<Faction.Data>();
					//	if (!faction.IsNull() && faction.id != 0)
					//	{
					//		//player.ent_player.AddRelation(faction.ent_faction, Relation.Type.Instance);

					//		player.SetFaction(faction.id);
					//		player.ent_controlled.SetFaction(faction.id);

					//		ref var character_data = ref player.GetControlledCharacterData(out var h_character);
					//		if (character_data.IsNotNull())
					//		{
					//			character_data.faction = faction.id;
					//			h_character.Sync(false);
					//		}

					//		//var oc_character = player.GetControlledCharacter();
					//		//if (oc_character.data.IsNotNull())
					//		//{
					//		//	throw new NotImplementedException();
					//		//	//ref var character_data = ref oc_character.data.character_id.GetData();
					//		//	//if (character_data.IsNotNull())
					//		//	//{
					//		//	//	character_data.faction = faction.id;
					//		//	//	oc_character.data.character_id.Sync(false);
					//		//	//}
					//		//}


					//		//player.ent_controlled.SetFaction(faction.id);

					//		//Notification.Push(ref entity.GetRegion(), $"DEBUG: Joined faction \"{faction.name}\".", faction.color_a, 5.00f, "vo_join");
					//	}
					//}
				}
			}
#endif
		}

		public struct ClaimLeaderRPC: Net.IRPC<Headquarters.Data>
		{
			public byte remove;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Headquarters.Data data)
			{
				if (Constants.Factions.enable_leadership)
				{
					ref var player = ref rpc.connection.GetPlayer(out var player_asset);
					if (player.IsNotNull())
					{
						ref var faction = ref rpc.entity.GetComponent<Faction.Data>();
						if (faction.IsNotNull() && faction.id != 0 && player.h_faction == faction.id)
						{
							ref var faction_data = ref faction.id.GetData();
							if (faction_data.IsNotNull())
							{
								var sync = false;

								if (faction_data.IsLeader(player_asset))
								{
									if (this.remove == 1)
									{
										faction_data.players.Remove(player_asset); //.h_player_leader = default;
										Notification.Push($"{player.name} has renounced leadership of faction \"{faction_data.name}\"!", faction_data.color_a, 5.00f, send_type: Net.SendType.Reliable);

										sync = true;
									}
									else
									{
										if (App.log_filter.HasAny(App.LogFlags.Warn)) App.WriteLine($"Player (player.id: {player.steam_id}) attempted to claim leadership of faction (faction.id: {faction.id}) they already own!", App.Color.Yellow);
									}
								}
								else
								{
									if (!faction_data.HasLeader())
									{
										var info = faction_data.players.GetValueOrDefault(player_asset);
										info.perms_faction.AddFlag(IFaction.Permissions.Owner);
										faction_data.players[player_asset] = info;

										Notification.Push($"{player.name} has claimed leadership of faction \"{faction_data.name}\"!", faction_data.color_a, 5.00f, send_type: Net.SendType.Reliable);

										sync = true;
									}
									else
									{
										if (App.log_filter.HasAny(App.LogFlags.Warn)) App.WriteLine($"Player (player.id: {player.steam_id}) attempted to claim leadership of faction (faction.id: {faction.id}) that already has a leader!", App.Color.Yellow);
									}
								}

								if (sync)
								{
									faction.id.Sync(false);
									//faction.Sync(faction.ent_faction);
								}
							}
						}
					}
				}
			}
#endif
		}

		[Shitcode] // TODO: cleanup, use non-nested asserts
		public struct KickRPC: Net.IRPC<Headquarters.Data>
		{
			public IPlayer.Handle h_player;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Headquarters.Data data)
			{
				if (Constants.Factions.enable_leave_faction && Constants.Factions.enable_kick)
				{
					ref var player_caller = ref rpc.connection.GetPlayer(out var player_caller_asset);
					Assert.IsNotNull(ref player_caller);

					ref var player_kicked = ref h_player.GetData(out var player_kicked_asset);
					Assert.IsNotNull(ref player_kicked);

					{
						ref var faction = ref rpc.entity.GetComponent<Faction.Data>();
						if (faction.IsNotNull() && faction.id != 0 && player_caller.h_faction == faction.id)
						{
							ref var region = ref rpc.entity.GetRegionCommon();

							ref var faction_data = ref faction.id.GetData(out var faction_asset);
							if (faction_data.IsNotNull())
							{
								if (!faction_data.IsLeader(this.h_player)) // this.h_player != faction_data.h_player_leader)
								{
									if (faction_data.IsLeader(player_caller_asset))
									{
										//ref var player_target = ref region.GetPlayer(this.player_id);
										if (player_kicked.h_faction == faction.id)
										{
											player_kicked_asset.GetHandle().SetFaction(0);
											Notification.Push($"{player_caller.name} has kicked {player_kicked_asset.GetName()} out of faction \"{faction_data.name}\"!", faction_data.color_a, 5.00f, send_type: Net.SendType.Reliable);

											//var oc_character = player_target.GetControlledCharacter();
											//if (oc_character.data.IsNotNull())
											//{
											//	ref var character_data = ref oc_character.data.character_id.GetData();
											//	if (character_data.IsNotNull())
											//	{
											//		character_data.faction = default;
											//		oc_character.data.character_id.Sync(false);
											//	}
											//}
										}
									}
									else
									{
										if (App.log_filter.HasAny(App.LogFlags.Warn)) App.WriteLine($"Player (player.id: {player_caller.steam_id}) attempted to kick player {this.h_player} of faction (faction.id: {faction.id}) they don't own!", App.Color.Yellow);
									}
								}
							}
						}
					}
				}
			}
#endif
		}

		public struct LeaveFactionRPC: Net.IRPC<Headquarters.Data>
		{
#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Headquarters.Data data)
			{
				if (Constants.Factions.enable_leave_faction && data.flags.HasAll(Headquarters.Flags.Enable_Leave))
				{
					ref var player = ref rpc.connection.GetPlayer(out var player_asset);
					//ref var player_comp = ref connection.GetPlayerData();
					if (player.IsNotNull() && player.h_faction != 0)
					{
						ref var faction = ref rpc.entity.GetComponent<Faction.Data>();
						if (faction.IsNotNull() && faction.id != 0 && player.h_faction == faction.id)
						{
							ref var faction_data = ref faction.id.GetData();
							if (faction_data.IsNotNull())
							{
								var is_leader = faction_data.IsLeader(player_asset);
								if (is_leader) Assert.Check(faction_data.policies.HasAll(IFaction.Policies.Allow_Recruitment));

								//player.ent_player.RemoveInstanceOf(faction.entity);
								//player.ent_player.RemoveRelation(faction.ent_faction, Relation.Type.Instance);

								if (faction_data.players.Remove(player_asset))
								{
									faction.id.Sync(false);
								}
							}

							player_asset.GetHandle().SetFaction(0);

							//var oc_character = player_comp.GetControlledCharacter();
							//if (oc_character.data.IsNotNull())
							//{
							//	ref var character_data = ref oc_character.data.character_id.GetData();
							//	if (character_data.IsNotNull())
							//	{
							//		character_data.faction = default;
							//		oc_character.data.character_id.Sync(false);
							//	}
							//}

							//Notification.Push(ref entity.GetRegion(), $"DEBUG: Left faction \"{faction.name}\".", faction.color_a, 5.00f, "vo_no");
						}
					}
				}
			}
#endif
		}

#if CLIENT
		public partial struct CreateFactionGUI: IGUICommand
		{
			public Vector2 world_position;
			public Headquarters.Data headquarters;
			public Entity ent_headquarters;

			public static FixedString8 edit_tag;
			public static FixedString32 edit_name;

			public static IOrganization.Handle edit_h_organization;
			public static ICompany.Flags edit_company_flags;

			public static Color32BGRA edit_color_a = Color32BGRA.White;
			public static Color32BGRA edit_color_b = Color32BGRA.White;

			public void Draw()
			{
				using (var window = GUI.Window.Interaction("New Faction"u8, this.ent_headquarters))
				{
					this.StoreCurrentWindowTypeID(order: -1000);
					if (window.show)
					{
						//GUI.DrawWindowBackground();


						if (Client.GetFactionHandle() == 0)
						{
							using (var group = GUI.Group.Centered(outer_size: GUI.Rm, inner_size: new(350, 200)))
							{
								GUI.DrawBackground(GUI.tex_panel, group.GetInnerRect(), new(4));
								var errors = Faction.Validate(tag: ref edit_tag, name: ref edit_name, color_a: ref edit_color_a, color_b: ref edit_color_b, h_organization: ref edit_h_organization, company_flags: ref edit_company_flags);

								using (GUI.Group.New(size: new(GUI.RmX, 32)))
								{
									var faction_name_preview = default(FixedString64);
									var faction_name_preview_len = 0;

									faction_name_preview.Write(ref faction_name_preview_len, edit_name);
									if (faction_name_preview_len > 0)
									{
										faction_name_preview.Write(ref faction_name_preview_len, " "u8);

										if (ICompany.TryGetImperialName(edit_h_organization, edit_company_flags, out var org_name))
										{
											faction_name_preview.Write(ref faction_name_preview_len, org_name);
										}

										//faction_name_preview.Write(ref faction_name_preview_len, 10, format: "b4");

										GUI.TitleCentered(faction_name_preview, size: 20, pivot: new(0.00f, 0.50f), offset: new(6, 0));
									}
									else
									{
										GUI.TitleCentered("<New Faction>"u8, size: 20, pivot: new(0.00f, 0.50f), offset: new(6, 0));
									}
								}

								GUI.SeparatorThick();

								using (GUI.Group.New(size: GUI.Rm, padding: new(4, 4)))
								{
									//using (GUI.Group.New(size: new(GUI.RmX, 24)))
									//{
									//	GUI.TitleCentered("New Faction", size: 24);
									//}

									using (GUI.Group.New(size: new(GUI.RmX, 32)))
									{
										GUI.TextInput("tag"u8, "<tag>"u8, ref edit_tag, new Vector2(56, GUI.RmY), max_length: 6);
										GUI.SameLine();
										GUI.TextInput("name"u8, "<faction name>"u8, ref edit_name, new Vector2(GUI.RmX, GUI.RmY), max_length: 10);
									}

									GUI.SeparatorThick();

									using (GUI.Group.New(new Vector2(GUI.RmX, 32)))
									{
										if (GUI.AssetInput2("organization"u8, ref edit_h_organization, size: GUI.Rm, filter: x => x.data.flags.HasAny(IOrganization.Flags.Faction)))
										{
											edit_company_flags = default;

											ref var organization_data = ref edit_h_organization.GetData();
											if (organization_data.IsNotNull())
											{
												//var random = XorRandom.New(true);
												//edit_company_flags.AddRandomFlag(ref random, organization_data.company_flags_faction);
												//edit_company_flags.AddRandomFlag(ref random, organization_data.company_flags_faction);
											}
										}
									}

									using (GUI.Group.New(new Vector2(GUI.RmX, 32)))
									{
										var company_flags_mask = ICompany.Flags.None;
										//var company_flags_req = ICompany.Flags.None;

										ref var organization_data = ref edit_h_organization.GetData();
										if (organization_data.IsNotNull())
										{
											company_flags_mask |= organization_data.company_flags_faction;
											//company_flags_mask |= organization_data.filter_flags.include;
											//company_flags_req |= organization_data.filter_flags.require;
										}


										//edit_company_flags |= company_flags_req;

										//edit_company_flags.MaskFlag(Faction.allowed_company_flags);
										//company_flags_mask.MaskFlag(Faction.allowed_company_flags);
										//company_flags_req.MaskFlag(Faction.allowed_company_flags);


										if (GUI.EnumInput("company.flags"u8, ref edit_company_flags, size: GUI.Rm, show_label: false, mask: company_flags_mask, max_flags: 2, columns: 3))
										{
											//App.WriteLine(edit_company_flags);
										}
									}

									using (GUI.Group.New(new Vector2(128, 48)))
									{
										GUI.DrawColorPicker("Color (A)"u8, ref edit_color_a, size: new(GUI.RmX, 24));
										//GUI.DrawHoverTooltip("Color (A)"u8);

										GUI.DrawColorPicker("Color (B)"u8, ref edit_color_b, size: new(GUI.RmX, 24));
										//GUI.DrawHoverTooltip("Color (B)"u8);
									}

									GUI.SameLine();

									//using (GUI.Group.Centered(outer_size: GUI.Rm, inner_size: new(140, 48)))
									{
										//var errors = Faction.Validate(tag: ref edit_tag, name: ref edit_name, color_a: ref edit_color_a, color_b: ref edit_color_b, h_organization: ref edit_h_organization, company_flags: ref edit_company_flags);
										var is_valid = errors == Faction.CreateErrors.None;

										if (GUI.DrawButton("Create Faction"u8, size: new Vector2(140, 48), color: GUI.col_button_ok, error: !is_valid))
										{
											if (is_valid)
											{
												var rpc = new Headquarters.FoundFactionRPC()
												{
													tag = edit_tag,
													name = edit_name,
													company_flags = edit_company_flags,
													h_organization = edit_h_organization,
													color_a = edit_color_a,
													color_b = edit_color_b
												};
												rpc.Send(this.ent_headquarters);
											}
										}
										if (GUI.IsItemHovered())
										{
											using (GUI.Tooltip.New(size: new Vector2(0, 0)))
											{
												GUI.TextShaded("Create a new faction."u8);

												using (GUI.Wrap.Push(128))
												{
													if (errors != Faction.CreateErrors.None)
													{
														GUI.TextShaded(errors.ToFormattedString(separator: "\n"), color: GUI.font_color_error);
													}
												}
											}
										}

										//GUI.TextInput("##Tag", "tag", ref edit_tag, new Vector2(64, 32), max_length: 4);
										//GUI.SameLine();
										//GUI.TextInput("##Name", "name", ref edit_name, new Vector2(GUI.RmX, 32));
									}
								}
							}
						}
					}
				}
			}
		}

		public partial struct FactionGUI: IGUICommand
		{
			public Vector2 world_position;
			public Headquarters.Data headquarters;
			public IFaction.Handle h_faction;
			public Entity ent_headquarters;

			public static Color32BGRA edit_color_a;
			public static Color32BGRA edit_color_b;

			public static int selected_tab;

			public void Draw()
			{
				ref var region = ref this.ent_headquarters.GetRegion();

				ref var faction_data = ref this.h_faction.GetData();
				if (faction_data.IsNotNull())
				{
					using (var window = GUI.Window.Interaction("Faction"u8, this.ent_headquarters, tooltip_tab: "Faction info & management."))
					{
						this.StoreCurrentWindowTypeID(-100);
						if (window.show)
						{
							var mouse = GUI.GetMouse();
							ref var player_data = ref Client.GetPlayerData(out var player_asset);
							ref var company_data = ref faction_data.h_company.GetData();

							var is_leader = faction_data.IsLeader(player_asset);

							using (GUI.Group.New(new(GUI.AvX, 40)))
							{
								using (GUI.Group.New(new(GUI.RmX - 144, GUI.RmY), new(8, 0)))
								{
									var offset_x = GUI.TitleCentered(faction_data.name, size: 32, pivot: new(0.00f, 0.50f)).X;
									GUI.FocusableAsset(this.h_faction);

									if (company_data.IsNotNull() && ICompany.TryGetImperialName(company_data.h_organization, company_data.flags, out var org_name))
									{
										GUI.TitleCentered(org_name, size: 32, pivot: new(0.00f, 0.50f), offset: new(offset_x + 8, 0));
										GUI.FocusableAsset(company_data.h_organization);
									}
								}

								GUI.SameLine();

								if (player_data.h_faction == this.h_faction)
								{
									if (Constants.Factions.enable_leave_faction)
									{
										if (GUI.DrawConfirmButton("confirm.leave"u8, "Leave Faction"u8, "Do you want to leave this faction?"u8, new Vector2(GUI.RmX, GUI.RmY), color: GUI.col_button_error, enabled: this.headquarters.flags.HasAny(Headquarters.Flags.Enable_Leave) && (is_leader ? faction_data.policies.HasAll(IFaction.Policies.Allow_Recruitment) : true)))
										{
											var rpc = new Headquarters.LeaveFactionRPC()
											{

											};
											rpc.Send(this.ent_headquarters);
										}
										GUI.DrawHoverTooltip("Leave this faction."u8);
									}
								}
								else
								{
									if (Constants.Factions.enable_join_faction)
									{
										if (GUI.DrawButton("Join Faction"u8, new Vector2(GUI.RmX, GUI.RmY), color: GUI.col_button_ok, enabled: this.headquarters.flags.HasAny(Headquarters.Flags.Enable_Join) && faction_data.policies.HasAll(IFaction.Policies.Allow_Recruitment)))
										{
											var rpc = new Headquarters.JoinFactionRPC()
											{

											};
											rpc.Send(this.ent_headquarters);
										}
										GUI.DrawHoverTooltip("Join this faction."u8);
									}
								}
							}

							GUI.SeparatorThick(margin: new(4, 4));

							if (this.headquarters.flags.HasAny(Headquarters.Flags.Show_Members))
							{
								using (var group_left = GUI.Group.New(size: new Vector2(GUI.RmX * 0.60f, GUI.RmY), padding: new(2, 0)))
								{
									group_left.DrawBackground(GUI.tex_panel);

									using (GUI.Group.New(size: new Vector2(GUI.RmX, 40)))
									{
										GUI.DrawTab3("Members"u8, new Vector2(100, 40), 0, ref selected_tab, inner: true);
										GUI.SameLine();
										GUI.DrawTab3("Personnel"u8, new Vector2(100, 40), 1, ref selected_tab, inner: true);
									}

									GUI.SeparatorThick();

									using (GUI.Group.New(size: GUI.Rm))
									{
										if (selected_tab == 0 && this.headquarters.flags.HasAny(Headquarters.Flags.Show_Members))
										{
											using (var scrollable = GUI.Scrollbox.New("scroll.members"u8, size: GUI.Rm, padding: new(4)))
											{
												GUI.DrawBackground(GUI.tex_panel, scrollable.group_frame.GetInnerRect(), padding: new(8));

												using (var table = GUI.Table.New("Members"u8, 4))
												{
													if (table.show)
													{
														table.SetupColumnFlex(1);
														table.SetupColumnFixed(48);
														table.SetupColumnFixed(56);
														table.SetupColumnFixed(56);
														//table.SetupColumnFlex(1);

														using (var row = table.NextRow(16, header: true))
														{
															using (row.Column(0)) GUI.Title("Name"u8);
															using (row.Column(1)) GUI.Title("Money"u8);
															using (row.Column(2)) GUI.Title("Rank"u8);
															using (row.Column(3)) GUI.Title("Status"u8);
															//using (row.Column(2)) GUI.Title("Test");
														}

														//region.Query<Region.GetPlayersQuery>(Func).Execute(ref this);

														//var h_faction_tmp = this.h_faction;
														//var ent_headquarters_tmp = this.ent_headquarters;
														//var headquarters_flags_tmp = this.headquarters.flags;

														foreach (var player_member_asset in IPlayer.Database.GetAssetsSpan())
														{
															ref var member_player_data = ref player_member_asset.GetData();
															if (member_player_data.h_faction == this.h_faction)
															{
																using (var row = GUI.Table.Row.New(new Vector2(GUI.RmX, 20)))
																{
																	var h_player_member = player_member_asset.GetHandle();
																	using (GUI.ID.Push(h_player_member))
																	{
																		using (row.Column(0))
																		{
																			GUI.TextShaded(h_player_member.GetName());
																		}

																		//var player_tmp = player; // TODO: hack
																		//ref var money = ref player_tmp.GetMoney().data;
																		//if (!money.IsNull())
																		//{
																		//	using (row.Column(1))
																		//	{
																		//		GUI.TextShaded($"{money.amount.FormatAmount():0.##}");
																		//	}
																		//}

																		using (row.Column(2))
																		{
																			if (faction_data.IsLeader(h_player_member))
																			{
																				GUI.TextShaded("Leader"u8);
																			}
																		}

																		//using (row.Column(2))
																		//{

																		//}

																		using (row.Column(3))
																		{
																			//GUI.TextShaded(member_player_data.flags.HasAll(Player.Flags.Online) ? "Online" : "Offline");
																		}

																		GUI.SameLine();
																		var pressed = GUI.Selectable2(false, play_sound: true, size: new Vector2(0, 0), is_readonly: false);

																		if (GUI.IsItemHovered())
																		{
																			if (pressed || mouse.GetKeyDown(Mouse.Key.Right))
																			{
																				GUI.OpenPopup("context menu"u8);
																			}
																		}

																		using (var popup = GUI.Popup.New("context menu"u8))
																		{
																			if (popup.show)
																			{
																				if (Constants.Factions.enable_leave_faction && Constants.Factions.enable_kick)
																				{
																					if (GUI.DrawButton("Kick"u8, new(80, 40), color: GUI.col_button_error, enabled: this.headquarters.flags.HasAny(Headquarters.Flags.Manage_Members) && is_leader && !faction_data.IsLeader(h_player_member)))
																					{
																						var rpc = new Headquarters.KickRPC()
																						{
																							h_player = h_player_member
																						};
																						rpc.Send(this.ent_headquarters);

																						popup.Close();
																					}
																					GUI.DrawHoverTooltip($"Kick \"{h_player_member.GetName()}\" from this faction.");
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
										}
										else if (selected_tab == 1 && this.headquarters.flags.HasAny(Headquarters.Flags.Show_Members))
										{
											using (var scrollable = GUI.Scrollbox.New("scroll.characters"u8, size: GUI.Rm, padding: new(4)))
											{
												GUI.DrawBackground(GUI.tex_panel, scrollable.group_frame.GetInnerRect(), padding: new(8));

												foreach (var character_asset in ICharacter.Database.GetAssetsSpan())
												{
													ref var character_data = ref character_asset.GetData();
													if (character_data.faction == this.h_faction)
													{
														var h_character = character_asset.GetHandle();
														using (GUI.ID.Push(h_character))
														using (var group_row = GUI.Group.New(size: new(GUI.RmX, 40)))
														{
															if (group_row.IsVisible())
															{
																Dormitory.DrawCharacterSmall(h_character);
															}
														}
													}
												}
											}
										}
									}
								}

								GUI.SameLine();
							}

							if (this.headquarters.flags.HasAny(Headquarters.Flags.Manage_Leader | Flags.Manage_Style))
							{
								using (GUI.Group.New(size: new Vector2(GUI.RmX, GUI.RmY), padding: new(8)))
								{
									if (Constants.Factions.enable_leadership && this.headquarters.flags.HasAny(Headquarters.Flags.Manage_Leader))
									{
										GUI.Title("Leadership"u8, size: 20);
										GUI.SeparatorThick();

										using (var group = GUI.Group.New(size: new(GUI.RmX, 0)))
										{
											//GUI.DrawBackground(GUI.tex_panel, group.GetOuterRect(), padding: new(8));

											using (var group_inner = GUI.Group.New(size: new(GUI.RmX, 56), padding: new(4)))
											{
												GUI.DrawBackground(GUI.tex_panel, group_inner.GetOuterRect(), new(8));

												using (GUI.Group.Centered(outer_size: GUI.Rm, inner_size: new(180, 44)))
												{
													if (!is_leader)
													{
														if (GUI.DrawButton("Claim Leadership"u8, size: GUI.Rm, enabled: player_data.h_faction == this.h_faction && faction_data.flags.HasNone(IFaction.Flags.NPC) && !faction_data.HasLeader(), color: GUI.col_button_ok))
														{
															var rpc = new Headquarters.ClaimLeaderRPC()
															{
																remove = 0
															};
															rpc.Send(this.ent_headquarters);
														}
													}
													else
													{
														if (GUI.DrawButton("Renounce Leadership"u8, size: GUI.Rm, enabled: player_data.h_faction == this.h_faction && is_leader && faction_data.policies.HasAll(IFaction.Policies.Allow_Recruitment), color: GUI.col_button_error))
														{
															var rpc = new Headquarters.ClaimLeaderRPC()
															{
																remove = 1
															};
															rpc.Send(this.ent_headquarters);
														}
													}
												}
											}
										}
									}

									if (this.headquarters.flags.HasAny(Headquarters.Flags.Manage_Style))
									{
										GUI.NewLine(8);
										GUI.Title("Style"u8, size: 20);
										GUI.SeparatorThick();

										using (var group = GUI.Group.New(size: new(GUI.RmX, 0)))
										{
											//GUI.DrawBackground(GUI.tex_panel, group.GetOuterRect(), padding: new(8));

											using (var group_inner = GUI.Group.New(size: new(GUI.RmX, 56), padding: new(4)))
											{
												GUI.DrawBackground(GUI.tex_panel, group_inner.GetOuterRect(), new(8));

												var dirty = false;

												// TODO: shithack, window.appearing doesn't work, since it's a nested window - so it gets only called when a window is opened with this tab already selected
												if (window.appearing || edit_color_a.a == 0 || edit_color_b.a == 0)
												{
													edit_color_a = faction_data.color_a;
													edit_color_b = faction_data.color_b;
												}

												using (GUI.Group.New(size: new(72.00f, 24 * 2)))
												{
													dirty |= GUI.DrawColorPicker("Color (A)"u8, ref edit_color_a);
													//GUI.DrawHoverTooltip("Color (A)"u8);

													dirty |= GUI.DrawColorPicker("Color (B)"u8, ref edit_color_b);
													//GUI.DrawHoverTooltip("Color (B)"u8);
												}

												GUI.SameLine();

												using (GUI.Group.Centered(outer_size: GUI.Rm, inner_size: new(80, 40)))
												{
													if (GUI.DrawButton("Apply"u8, size: GUI.Rm, enabled: player_data.h_faction == this.h_faction && is_leader, color: GUI.col_button_yellow))
													{
														var rpc = new Headquarters.EditFactionRPC()
														{
															color_a = edit_color_a,
															color_b = edit_color_b
														};
														rpc.Send(this.ent_headquarters);
													}
												}
											}
										}
									}

									if (this.headquarters.flags.HasAny(Headquarters.Flags.Manage_Policies))
									{
										GUI.NewLine(8);
										GUI.Title("Policies"u8, size: 20);
										GUI.SeparatorThick();

										var policies_tmp = faction_data.policies;

										using (var group = GUI.Group.New(size: new(GUI.RmX, 0)))
										{
											//GUI.DrawBackground(GUI.tex_panel, group.GetOuterRect(), padding: new(8));

											using (var group_inner = GUI.Group.New(size: new(GUI.RmX, 56), padding: new(4)))
											{
												GUI.DrawBackground(GUI.tex_panel, group_inner.GetOuterRect(), new(8));


												if (GUI.Checkbox("allow recruitment"u8, ref policies_tmp, IFaction.Policies.Allow_Recruitment, size: new(GUI.RmX, 24), enabled: is_leader))
												{
													var rpc = new Headquarters.EditFactionRPC()
													{
														policies = policies_tmp
													};
													rpc.Send(this.ent_headquarters);
												}
												GUI.DrawHoverTooltip("Allow new players to join this faction."u8);

											}
										}

										//if (true)
										//{
										//	var rpc = new Headquarters.EditFactionRPC()
										//	{
										//		color_a = edit_color_a,
										//		color_b = edit_color_b
										//	};
										//	rpc.Send(this.ent_headquarters);
										//}
									}
								}
							}
						}
					}
				}
			}
		}

		[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global)]
		public static void OnGUI(Entity entity,
		[Source.Owned] in Transform.Data transform, [Source.Owned] ref Headquarters.Data headquarters,
		[Source.Owned] in Faction.Data faction, [Source.Owned] in Interactable.Data interactable)
		{
			if (interactable.IsActive())
			{
				var h_faction_player = Client.GetFactionHandle();
				if (faction.id)
				{
					var gui = new FactionGUI()
					{
						world_position = transform.GetInterpolatedPosition(),
						headquarters = headquarters,
						h_faction = faction.id,
						ent_headquarters = entity
					};
					gui.Submit();
				}
				else if (!h_faction_player)
				{
					if (headquarters.flags.HasAny(Flags.Allow_Create_Faction))
					{
						var gui = new CreateFactionGUI()
						{
							world_position = transform.GetInterpolatedPosition(),
							headquarters = headquarters,
							ent_headquarters = entity
						};
						gui.Submit();
					}
				}
			}
		}
#endif
	}
}
