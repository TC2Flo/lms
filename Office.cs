
using System.Diagnostics.CodeAnalysis;
using TC2.Conquest;

namespace TC2.Base.Components
{
	public static partial class Personnel
	{
		public partial struct Slot()
		{
			public enum Type: byte
			{
				Undefined = 0,

				//Worker,
				//Resident,
				//Guest,

			}

			public enum Role: byte
			{
				Undefined = 0,

				Passenger,
				Resident,
				Prisoner,
				Driver,
				Gunner,
				Guard,
				Worker,
				Patient,
				Guest,
				Officer,
				Manager,
				Technician,
				Operator,
				Clerk,
				Warden,
				Foreman,
				Quartermaster,
				Inspector,
				Chef,
				Cook,
				Farmer,
				Metalworker,
				Blacksmith,
				Laborer,
				Lumberjack,
				Accountant,
				Border_Guard,
				Engineer,
				Pilot,
				Police,
				Merchant,
				Mechanic,
				Porter,
				Miner,
				Alchemist,
				Researcher,
				Scientist,
				Teacher,
				Assistant,
				Biologist,
				Test_Subject,
				Gardener,
				Janitor,
				Professor,
				Machinist,
				Tailor,
				Doctor,
				Welder,
				Director,
				Supervisor,
				Bartender,
				Butcher,
				Brewer
			}

			[Flags]
			public enum Flags: ushort
			{
				None = 0,

				Required = 1 << 0,

				[Obsolete] No_Enter = 1 << 1,
				[Obsolete] No_Leave = 1 << 2,

				Faction = 1 << 3,
				Neutral = 1 << 4,
				Controller = 1 << 5,
				Draw_Head = 1 << 6,
				Eject_On_Destroy = 1 << 7,

				Can_Craft = 1 << 13,
				Can_Manage = 1 << 14,
				Can_Use_Inventory = 1 << 15,
			}

			[Flags]
			public enum Tags: ulong
			{
				None = 0u,

				Security = 1ul << 0,
				Management = 1ul << 1,
				Combat = 1ul << 2,
				Research = 1ul << 3,
				Administrative = 1ul << 4,
				Civil = 1ul << 5,
				Military = 1ul << 6,
				Criminal = 1ul << 7,
				Industrial = 1ul << 8,
				Logistics = 1ul << 9,
				Production = 1ul << 10,
				Dangerous = 1ul << 11,
				Manual_Labor = 1ul << 12,
				Paperwork = 1ul << 13,
				Sedentary = 1ul << 14,
				Commerce = 1ul << 15,
				Financial = 1ul << 16,
				Technical = 1ul << 17,
				Maintenance = 1ul << 18,
				Operation = 1ul << 19,
				Vehicle = 1ul << 20,
				Overseer = 1ul << 21,
				Unskilled = 1ul << 22,
				Giant = 1ul << 23, // for giant characters
				Prisoner = 1ul << 24, // for prisoners
				Medical = 1ul << 25, // for medical personnel
				Education = 1ul << 26, // Teachers, professors, instructors
				Service = 1ul << 27, // Waiters, bartenders, hospitality
				Agriculture = 1ul << 28, // Farmers, ranchers, beekeepers
				Transport = 1ul << 29, // Drivers, pilots, sailors, conductors
				Crafts = 1ul << 30, // Tailors, cobblers, artisans
				Entertainment = 1ul << 31
			}

			//[Flags]
			//public enum Roles: uint
			//{
			//	None = 0u,

			//	Passenger = 1u << 0,
			//	Resident = 1u << 1,
			//	Prisoner = 1u << 2,
			//	Driver = 1u << 3,
			//	Gunner = 1u << 4,
			//	Guard = 1u << 5,
			//	Worker = 1u << 6,
			//	Patient = 1u << 7,
			//	Guest = 1u << 8,
			//	Officer = 1u << 9,
			//	Manager = 1u << 10,
			//	Technician = 1u << 11,
			//	Operator = 1u << 12,
			//	Clerk = 1u << 13,
			//	Warden = 1u << 14,
			//	Foreman = 1u << 15,
			//	Quartermaster = 1u << 16,
			//	 = 1u << 17,
			//}



			public Personnel.Slot.Type type;
			public Personnel.Slot.Role role;
			public Personnel.Slot.Flags flags;
			[Editor.Picker.Position(true)] public Vec2i16 offset;

			[Save.NewLine]
			public Personnel.Slot.Tags tags;
			public Color32BGRA color;
			public float multiplier;
		}

		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Global | IComponent.Scope.Region)]
		public partial struct Data(): IComponent, ICharacterStorage
		{
			[Flags]
			public enum Flags: ushort
			{
				None = 0,

				Draw_Heads = 1 << 0


				//[Obsolete] Store_Characters = 1 << 0,
			}

			[Net.Segment.A, Save.Force, Editor.Slider.Clamped(0, 7)] public required byte characters_capacity;
			[Net.Segment.A] private byte unused_01;
			[Net.Segment.A] public Personnel.Data.Flags flags;
			[Net.Segment.A] public float assign_radius = 16.00f;
			[Net.Segment.A] private uint unused_02;
			[Net.Segment.A] private uint unused_03;


			//public float move_radius = 6.00f;

			[Net.Segment.B, Asset.Ignore] public FixedArray8<ICharacter.Handle> characters;

			[Save.NewLine]
			[Net.Segment.C, Save.TrimEmpty] public FixedArray8<Personnel.Slot> slots;

			//public Span<ICharacter.Handle> Characters
			//{
			//	[UnscopedRef]
			//	get => this.characters.AsSpan();
			//}

			[UnscopedRef]
			Span<ICharacter.Handle> ICharacterStorage.GetCharacterSpan() => this.GetCharacterSpan();
		}

		//[ISystem.Update.C(ISystem.Mode.Single, ISystem.Scope.Region)]
		//public static void OnUpdateStored(ISystem.Info info, ref Region.Data region, ref XorRandom random,
		//Entity ent_character, [Source.Owned] ref Character.Data character,
		//Entity ent_personnel, [Source.Stored] ref Personnel.Data personnel,
		////[Source.]
		//[Source.Stored] in Transform.Data transform)
		//{
		//	// App.WriteLine($"{character.name}");
		//}

#if SERVER
		[ISystem.Update.C(ISystem.Mode.Single, ISystem.Scope.Region, interval: 1.50f)]
		public static void OnUpdateStoredWorker(ISystem.Info info, ref Region.Data region, ref XorRandom random,
		Entity ent_character, [Source.Owned] ref Character.Data character,
		[Source.Owned] ref Worker.Data worker, [Source.Owned] ref Worker.State worker_state,
		Entity ent_personnel, [Source.Stored] ref Personnel.Data personnel,
		[Source.Stored] in Transform.Data transform, [Source.Stored] ref Workshop.Data workshop)
		{
			//if (info.Tick % 128 == 0)
			{
				if (!character.h_player)
				{
					if (worker.ref_order.ComponentID != 0 && worker_state.work_index.HasValue) //worker_state.last_order_flags.HasAny(Crafting.Order.Flags.In_Progress))
					{

					}
					else
					{
						var span_status_flags = workshop.order_status_flags.AsSpan(workshop.orders_capacity);
						span_status_flags.GetFirstOrNull(static (x) => x.HasAny(Workshop.StatusFlags.Needs_Workers), out var index);
						if (index >= 0)
						{
							Worker.SetWorkTarget(worker: ref worker, ent_worker: ent_character, ent_target: ent_personnel, h_order: IComponent.Handle<Crafting.Order>.FromIndexPair((uint)index));
							worker_state.next_check = default;
							worker_state.next_work = default;
						}
					}
				}
				//App.WriteLine($"{ent_character.GetAssetHandle<ICharacter.Handle>()}; {worker_state.last_order_flags}; {worker.ref_order.ComponentID}; {worker_state.work_index}");
			}
		}
#endif

		public static Span<ICharacter.Handle> GetCharacterSpan(ref this Personnel.Data personnel)
		{
			return personnel.characters.Slice(personnel.characters_capacity);
		}

		public struct AssignRPC: Net.IRPC<Personnel.Data>
		{
			public required ICharacter.Handle h_character;
			public required byte slot_dst_index;
			public required (Entity entity, IComponent.Handle h_component) src;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Personnel.Data data)
			{
				ref var region_common = ref rpc.GetRegionCommon();
				Assert.IsNotNull(ref region_common);

				ref var character_data = ref this.h_character.GetData();
				Assert.IsNotNull(ref character_data);

				ref var h_character_dst = ref data.GetCharacterSpan().GetRefAtIndexOrNull(this.slot_dst_index);
				Assert.IsNotNull(ref h_character_dst);
				Assert.Check(!h_character_dst);

				Assert.Check(this.src.entity.TryGetRecord(out var rec_src));

				if (this.src.h_component.IsComponent<Dormitory.Data>())
				{
					ref var dormitory = ref rec_src.GetComponent<Dormitory.Data>();
					Assert.IsNotNull(ref dormitory);

					ref var h_character_src = ref dormitory.GetCharacterSpan().GetRefOfValueOrNull(this.h_character, out var index_src, out _);
					Assert.IsNotNull(ref h_character_src);

					var ent_character = this.h_character.GetEntity(ref region_common);
					Assert.Alive(ent_character);

					(h_character_src, h_character_dst) = (h_character_dst, h_character_src);
					ent_character.ReplaceRelation(parent: rpc.entity, relation: Relation.Type.Stored, check_recursion: true);

					dormitory.Sync(rec_src, true);
					data.Sync(rpc.record, true);
				}
			}
#endif
		}

		public struct EnterRPC: Net.IRPC<Personnel.Data>
		{
			public ICharacter.Handle h_character;
			public int index;

#if SERVER
			// TODO: make a helper method for this
			public void Invoke(Net.IRPC.Context rpc, ref Personnel.Data data)
			{
				ref var region_common = ref rpc.entity.GetRegionCommon();
				ref var region = ref region_common.AsRegion();

				ref var character_data = ref this.h_character.GetData(out var character_asset);
				Assert.IsNotNull(ref character_data);

				var characters_span = data.GetCharacterSpan();
				Assert.Check(!characters_span.Contains(character_asset)); // cannot enter if already inside

				ref var transform = ref rpc.entity.GetTransform();
				Assert.IsNotNull(ref transform);

				if (region.IsNotNull())
				{
					ref var character = ref region.GetCharacter(this.h_character, out var ent_character);
					Assert.IsNotNull(ref character);

					var ent_controlled = character.ent_controlled;
					Assert.Check(ent_controlled.IsAlive());

					if (characters_span.TryGetAvailableIndex(ref this.index))
					{
						//var ts = Timestamp.Now();
						var ev = new Entrance.ExportEvent();
						ev.ent_root = ent_controlled;
						ev.Trigger(ent_controlled); //, IComponent.Handle.FromComponent<Organic.Data>());
						//ev.TriggerDeferred(ent_controlled); //, IComponent.Handle.FromComponent<Organic.Data>());
						//var ts_elapsed = ts.GetMilliseconds();
						//App.WriteLine($"ev: {ts_elapsed:0.0000} ms");

						var ent_pickup_target = character.ent_pickup_target;
						if (ent_pickup_target.IsAlive())
						{
							ref var holdable = ref ent_pickup_target.GetComponent<Holdable.Data>();
							if (holdable.IsNotNull())
							{
								if (holdable.flags.HasNone(Holdable.Flags.Storable))
								{
									ent_pickup_target.RemoveRelation(character.ent_pickup, Relation.Type.Child);
									App.WriteLine(ent_pickup_target.GetFullName(), color: App.Color.Green);
									//Arm.Drop(character.ent_pickup);
								}
							}
						}

						character.state_flags.AddFlag(Character.StateFlags.Despawning | Character.StateFlags.Keep_Alive);
						//character.state_flags.AddFlag(Character.StateFlags.Keep_Alive, data.flags.HasAny(Personnel.Data.Flags.Store_Characters));

						if (true) //data.flags.HasAny(Personnel.Data.Flags.Store_Characters))
						{
							character.ent_controlled.SetAssociatedCharacter(ICharacter.Handle.None); //.AddRelation(rpc.entity, Relation.Type.Stored, check_recursion: true);
																									 //ent_character.AddRelation(rpc.entity, Relation.Type.Stored, check_recursion: true);
						}
						else
						{
							ref var player = ref character.h_player.GetPlayer(ref region);
							if (player.IsNotNull())
							{
								player.SetControlledCharacter(ICharacter.Handle.None);
							}
						}

						character_asset.flags.RemoveFlag(Asset.Flags.Recycle);
						//character_asset.prefab = "unit.guy";
						//character_asset.Promote();

						characters_span[this.index] = this.h_character;
						data.Sync(rpc.entity);

						var ents_include = ev.ents_include;

						var ent_tmp = rpc.entity;
						region.Schedule((ref region) =>
						{
							//App.WriteLine("exported");
							ent_character.ReplaceRelation(ent_tmp, Relation.Type.Stored);

							var ents_include_tmp = ents_include;
							var character_prefab = ent_controlled.ExportPrefab(character_asset.identifier, despawn: true, ents_include: ents_include_tmp.AsSpan());
							App.WriteLine($"exported to {character_prefab}");

							region.Schedule((ref region) =>
							{
								//var character_prefab = ent_controlled.ExportPrefab(character_asset.identifier, despawn: true);
								//App.WriteLine($"exported to {character_prefab}");

								ref var character = ref region.GetCharacter(character_asset.GetHandle(), out var ent_character);
								if (Assert.IsNotNull(ref character, Assert.Level.Warn))
								{
									character.state_flags.RemoveFlag(Character.StateFlags.Despawning | Character.StateFlags.Keep_Alive);
									character.Sync(ent_character, true);
								}

								character_asset.data.prefab = character_prefab;
								character_asset.Sync();

								//App.WriteLine("exported");
								//ent_character.AddRelation(ent_tmp, Relation.Type.Stored, check_recursion: false);
							});
						});

						Sound.Play(ref region, "drop", transform.position);
					}
				}

			}
#endif
		}

		public struct LeaveRPC: Net.IRPC<Personnel.Data>
		{
			public ICharacter.Handle h_character;
			public bool control;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Personnel.Data data)
			{
				ref var character_data = ref this.h_character.GetData();
				Assert.IsNotNull(ref character_data);

				ref var region = ref rpc.connection.GetRegion();
				Assert.IsNotNull(ref region);

				ref var player = ref rpc.connection.GetPlayer(out var player_asset);
				Assert.IsNotNull(ref player);

				ref var transform = ref rpc.entity.GetComponent<Transform.Data>();
				Assert.IsNotNull(ref transform);

				var characters_span = data.GetCharacterSpan();

				var index = -1;
				Assert.Check(characters_span.TryGetIndexOf(this.h_character, ref index));

				characters_span[index] = default;
				data.Sync(rpc.entity);

				//ref var character = ref region.GetCharacter(this.h_character, out var ent_character);
				//if (character.IsNotNull())
				//{
				//	ent_character.RemoveRelation(rpc.entity, Relation.Type.Stored);
				//	App.WriteLine("removed rel");
				//}

				App.WriteLine("spawning char");

				var h_character_current = rpc.connection.GetCharacterHandle();
				var h_character_tmp = this.h_character;

				var flags_tmp = Spawn.SpawnEvent.Flags.None;
				if (this.control && (h_character_current == 0 || h_character_current == this.h_character || characters_span.Contains(h_character_current))) flags_tmp.AddFlag(Spawn.SpawnEvent.Flags.Control);

				Spawner.SpawnCharacter(region: ref region,
					h_character: this.h_character,
					position: transform.position,
					h_faction: character_data.faction,
					h_player: player_asset,
					flags: flags_tmp);
				//.ContinueWith(ent =>
				//{
				//	ref var character = ref ent.GetRegion().GetCharacter(h_character_tmp, out var ent_character);
				//	if (character.IsNotNull())
				//	{
				//		ent_character.RemoveRelation(rpc.entity, Relation.Type.Stored);
				//		App.WriteLine("removed rel");
				//	}
				//});

			}
#endif
		}

		public struct MoveRPC: Net.IRPC<Personnel.Data>
		{
			public ICharacter.Handle h_character;

			public Entity ent_source;
			public IComponent.Handle h_component_source;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Personnel.Data data)
			{

			}
#endif
		}

#if CLIENT
		public partial struct PersonnelGUI: IGUICommand
		{
			public Entity ent_personnel;

			public Personnel.Data personnel;
			public Transform.Data transform;

			public static ICharacter.Handle h_character_selected;
			public static uint? selected_index;

			public void Draw()
			{
				using (var window = GUI.Window.Interaction(identifier: "Personnel"u8, entity: this.ent_personnel,
				tooltip_tab: "View and manage workers\ninside this structure."))
				{
					this.StoreCurrentWindowTypeID(order: -80);
					if (window.show)
					{
						ref var region_common = ref this.ent_personnel.GetRegionCommon();

						var h_character_current = Client.GetCharacterHandle();

						var h_faction_entity = this.ent_personnel.GetFactionHandle();
						var h_faction_client = Client.GetFactionHandle();
						var can_manage = h_faction_entity.IsOwned(h_faction_client);

						var characters = this.personnel.GetCharacterSpan();
						var characters_count_empty = characters.GetEmptyCount();

						var is_inside = h_character_current.IsValid() && characters.Contains(h_character_current);

						if (selected_index.HasValue && selected_index.Value < characters.Length)
						{
							h_character_selected = characters[(int)selected_index.Value];
						}
						else
						{
							//if (!this.personnel.flags.HasAny(Personnel.Flags.No_Hiring))
							//{
							//	if (!characters_all.Any(x => x.id == h_character_selected.id))
							//	{
							//		h_character_selected = default;
							//	}
							//}
							if (is_inside)
							{
								h_character_selected = h_character_current;
								selected_index = (uint)characters.IndexOf(h_character_current);
							}
							else
							{
								h_character_selected = default;
								selected_index = null;
							}
						}

						//if (is_inside && h_character_selected == default)
						//{
						//	h_character_selected = h_character_current;
						//	selected_index = (uint)characters.IndexOf(h_character_current);
						//}
						//App.WriteLine($"{ts.GetMilliseconds():0.0000} ms");

						//var h_character_selected = characters[selected_index];

						//using (var scroll = GUI.Scrollbox.New("characters", size: new Vector2(GUI.RmX * 0.50f, GUI.RmY), padding: new(2)))
						using (var group_left = GUI.Group.New(size: new Vector2(GUI.RmX * 0.50f, GUI.RmY), padding: new(4)))
						{
							GUI.DrawBackground(GUI.tex_frame, group_left.GetOuterRect(), new(8, 8, 8, 8));

							for (var i = 0; i < characters.Length; i++)
							{
								using (GUI.ID<Personnel.Slot>.Push(i))
								{
									ref var slot = ref this.personnel.slots[i];

									var h_character = characters[i];
									var is_selected = i == selected_index;

									using (var group_row = GUI.Group.New(size: new(GUI.RmX, 48)))
									{
										if (group_row.IsVisible())
										{
											ref var character_data = ref h_character.GetData();

											using (var group_head = GUI.Group.New(size: new(GUI.RmY)))
											{
												group_head.DrawBackground(GUI.tex_panel);
												group_head.DrawBackground(GUI.tex_frame_white, color: GUI.col_button.Lerp(slot.color.WithAlpha(255), slot.color.a / 255.00f));
												//GUI.DrawCharacterHead(h_character, group_head.GetInnerRect());

												if (character_data.IsNotNull())
												{
													GUI.DrawSpriteCentered(character_data.sprite_head, group_head.GetOuterRect(), layer: GUI.Layer.Window, scale: 3.00f);
												}
											}

											GUI.SameLine();

											using (var group_info = GUI.Group.New(size: GUI.Rm, padding: new(4)))
											{
												group_info.DrawBackground(GUI.tex_panel);

												GUI.TitleCentered(character_data.IsNotNull() ? character_data.name : "<Empty>"u8, size: 20, pivot: new(0.00f, 0.00f), offset: new(2, 2));
												GUI.TextShadedCentered(slot.role.GetEnumName(), size: 14, pivot: new(0.00f, 1.00f), offset: new(2, -2));
											}

											//var selected = h_character == h_selected_character; // h_character_selected && h_character == h_character_selected; // selected_index;
											if (GUI.Selectable3("selectable"u8, group_row.GetOuterRect(), is_selected))
											{
												selected_index.Toggle((uint)i);
											}

											GUI.FocusableAsset(h_character);

											if (group_row.IsHovered())
											{
												using (var tooltip = GUI.Tooltip.New())
												using (GUI.Wrap.Push(224))
												{
													GUI.TextShaded(slot.tags.ToFormattedString());
												}
											}
										}
									}

									//if (Dormitory.DormitoryGUI.DrawCharacterRow(h_character, is_selected))
									//{
									//	selected_index.Toggle((uint)i);
									//}
								}
							}
						}

						GUI.SameLine();

						using (var group_right = GUI.Group.New(size: new Vector2(GUI.RmX, GUI.RmY), padding: new(0)))
						{
							GUI.DrawBackground(GUI.tex_panel, group_right.GetOuterRect(), new(8, 8, 8, 8));
							ref var character_data = ref h_character_selected.GetData();

							var is_occupied = h_character_selected && characters.Contains(h_character_selected);

							using (var group_top = GUI.Group.New(size: new(GUI.RmX, GUI.RmY - 40 - 2), padding: new(0)))
							{
								using (var group = GUI.Group.New(size: new(GUI.RmX, 32), padding: new(4)))
								{
									if (character_data.IsNotNull())
									{
										GUI.Title(character_data.name, size: 24);
									}
									else
									{
										GUI.Title("<No character selected>"u8, size: 24);
									}
								}

								GUI.SeparatorThick();

								using (var scrollbox = GUI.Scrollbox.New("scrollbox.xp"u8, size: new(GUI.RmX, GUI.RmY - 170)))
								{
									GUI.DrawBackground(GUI.tex_panel, scrollbox.group_frame.GetOuterRect(), new(8, 8, 8, 8));

									if (character_data.IsNotNull())
									{
										Experience.DrawTableSmall2(ref character_data.experience);
									}
								}

								GUI.SeparatorThick();
							}

							GUI.SeparatorThick();

							using (var group_bottom = GUI.Group.New(size: GUI.Rm, padding: new(0)))
							{
								if (is_occupied)
								{
									if (GUI.DrawButton("Assign..."u8, size: new Vector2(96, 40), enabled: false, color: GUI.col_input))
									{

									}

									GUI.SameLine();

									if (GUI.DrawButton("Leave"u8, size: new Vector2(72, 40), enabled: can_manage || (is_inside && h_character_selected == h_character_current), color: GUI.col_remove))
									{
										var rpc = new Personnel.LeaveRPC()
										{
											h_character = h_character_selected,
											control = true
										};
										rpc.Send(this.ent_personnel);
									}

									//GUI.SameLine();

									//if (GUI.DrawButton("Move to..."u8, size: new Vector2(100, 40), enabled: can_manage, color: GUI.col_button_yellow))
									//{
									//	GUI.OpenPopup("personnel.move"u8);
									//}

									//GUI.SameLine();

									//var character_count_max = Constants.Characters.character_count_max;
									//if (player.h_faction == 0 && !Constants.Characters.allow_neutral_multi_characters)
									//{
									//	character_count_max = 1;
									//}

									//if (GUI.DrawButton("Spawn", size: new Vector2(GUI.RmX, 40), enabled: can_spawn && (is_faction_owned || player_character_count < character_count_max), color: GUI.col_button_ok))
									//{
									//	var rpc = new DEV_SpawnRPC()
									//	{
									//		h_character = h_character_selected,
									//		control = !ent_controlled.IsAlive()
									//	};
									//	rpc.Send(this.ent_personnel);
									//}
									//GUI.DrawHoverTooltip("Spawn as this character.");
								}
								else
								{
									var popup_width = GUI.RmX;
									if (GUI.DrawButton("Assign..."u8, size: new Vector2(96, 40), enabled: can_manage & selected_index.HasValue, color: GUI.col_input))
									{
										GUI.OpenPopup("popup.assign"u8);
									}

									var prev_rect = GUI.GetLastItemRect();
									using (var popup = GUI.Popup.New("popup.assign"u8, position: prev_rect.bl, size: new(popup_width, 192)))
									{
										if (popup.show)
										{
											ref var region = ref region_common.AsRegion();
											var results = FixedArray.CreateSpanList8<Entity>(out var results_buffer);

											scoped var args = new FetchDormitories()
											{
												h_faction = h_faction_entity,
												max_distance = this.personnel.assign_radius,
												world_position = this.transform.position,
												results = results,
											};

											if (region.IsNotNull())
											{
												region.RunSystem(FetchDormitories.System, ref args);
											}
											results = args.results;

											using (var scroll = GUI.Scrollbox.New("scroll.assign"u8, GUI.Rm))
											{
												//if (scroll.)
												foreach (var ent_dormitory in results)
												{
													if (ent_dormitory.TryGetRecord(out var rec_dormitory))
													{
														ref var dormitory = ref rec_dormitory.GetComponent<Dormitory.Data>();
														if (dormitory.IsNotNull())
														{
															var characters_span = dormitory.GetCharacterSpan();
															foreach (var dorm_h_character in characters_span)
															{
																ref var dorm_character_data = ref dorm_h_character.GetData();
																if (dorm_character_data.IsNotNull())
																{
																	using (var push_id = GUI.ID<Personnel.Data>.Push(dorm_h_character))
																	using (var disabled = GUI.Disabled.Push(dorm_character_data.faction != h_faction_entity))
																	{
																		using (var group_row = GUI.Group.New(size: new(GUI.RmX, 40)))
																		{
																			Dormitory.DrawCharacterSmall(dorm_h_character);
																		}

																		if (GUI.Selectable3(push_id.hash, GUI.GetLastItemRect(), selected: false))
																		{
																			var rpc = new AssignRPC
																			{
																				h_character = dorm_h_character,
																				slot_dst_index = (byte)selected_index.GetValueOrDefault(),
																				src = (entity: ent_dormitory, h_component: IComponent.Handle<Dormitory.Data>.New())
																			};
																			rpc.Send(ent_personnel);
																		}
																	}
																}
															}

															GUI.SeparatorThick();
														}
													}
												}
											}
										}
									}

									GUI.SameLine();

									if (GUI.DrawButton("Enter"u8, size: new Vector2(72, 40), color: GUI.col_add,
									enabled: can_manage && h_character_current && selected_index.HasValue && !is_inside))
									{
										var rpc = new Personnel.EnterRPC()
										{
											h_character = h_character_current,
											index = (int)selected_index.Value
										};
										rpc.Send(this.ent_personnel);
									}
								}
								//else
								//{
								//	if (GUI.DrawButton("Hire", size: new Vector2(100, 40), enabled: !is_spawned && !this.personnel.flags.HasAny(Personnel.Flags.No_Hiring), color: GUI.col_button_ok))
								//	{
								//		var rpc = new DEV_HireRPC()
								//		{
								//			h_character = h_character_selected
								//		};
								//		rpc.Send(this.ent_personnel);
								//	}
								//}
								//GUI.DrawBackground(GUI.tex_frame, group.GetOuterRect(), new(4));
							}
						}

						//using (var popup = GUI.Popup.New("personnel.move"u8, size: new(244, 0), pivot: new(0.00f, 0.00f)))
						//{
						//	if (popup.show)
						//	{
						//		ref var region = ref region_common.AsRegion();
						//		if (region.IsNotNull())
						//		{
						//			Span<OverlapResult> span_results = stackalloc OverlapResult[8];
						//			if (region.TryOverlapPointAll(this.transform.position, this.personnel.move_radius, ref span_results, mask: Physics.Layer.Dormitory, require: Physics.Layer.Dormitory))
						//			{
						//				span_results.SortByDistance();

						//				foreach (ref var result in span_results)
						//				{
						//					if (result.entity == this.ent_personnel) continue;

						//					ref var personnel_other = ref result.entity.GetComponent<Personnel.Data>();
						//					if (personnel_other.IsNotNull())
						//					{
						//						using (GUI.ID.Push(result.entity))
						//						{
						//							using (var group_row = GUI.Group.New(size: new Vector2(GUI.RmX, 40)))
						//							{
						//								if (GUI.DrawButton(result.entity.GetFullName(), size: GUI.Rm, color: GUI.col_button_yellow, enabled: can_manage && personnel_other.GetCharacterSpan().GetEmptyCount() > 0))
						//								{
						//									//var rpc = new DEV_MoveRPC()
						//									//{
						//									//	h_character = h_character_selected,
						//									//	ent_dst = result.entity
						//									//};
						//									//rpc.Send(this.ent_personnel);

						//									popup.Close();
						//								}
						//								if (GUI.IsItemHovered())
						//								{
						//									GUI.DrawEntity(result.entity, color: Color32BGRA.Green.WithAlphaMult(0.75f), layer: GUI.Layer.Foreground);
						//								}
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
			}
		}

		public static readonly Animated.Renderer.Data head_renderer_default = new Animated.Renderer.Data(ICharacter.sprite_head_default, offset: Vector2.Zero, scale: Vector2.One, rotation: 0.00f, z: 0.00f);

		[ISystem.PostRender(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnPostRender([Source.Owned] ref Personnel.Data personnel,
		[Source.Owned] in Transform.Data transform, [Source.Owned] ref Animated.Renderer.Data renderer)
		{
			if (personnel.flags.HasNone(Data.Flags.Draw_Heads)) return;
			if (!Camera.IsVisible(Camera.CullType.Rect1x_Padded, transform.position)) return;

			ref var characters = ref personnel.characters;
			ref var slots = ref personnel.slots;

			var renderer_tmp = head_renderer_default;
			renderer_tmp.z = renderer.z;
			renderer_tmp.transform_index = renderer.transform_index;

			var count = (uint)personnel.characters_capacity;
			for (var i = 0u; i < count; i++)
			{
				var h_character = characters[i];
				if (!h_character) continue;

				ref var character_data = ref h_character.GetData();
				if (character_data.IsNull()) continue;

				ref var slot = ref slots[i];
				if (slot.flags.HasNone(Slot.Flags.Draw_Head)) continue;

				// TODO: expose a way to submit it in a batch
				renderer_tmp.sprite = character_data.sprite_head;
				renderer_tmp.offset = slot.offset.ToVector2() * 0.125f;

				Animated.Renderer.Draw(renderer_tmp.transform_index, renderer_tmp);
			}
		}

		[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global)]
		public static void OnGUI(Entity entity,
		[Source.Owned] in Personnel.Data personnel,
		[Source.Owned] in Transform.Data transform,
		[Source.Owned] in Interactable.Data interactable)
		{
			if (interactable.IsActive())
			{
				var gui = new PersonnelGUI()
				{
					ent_personnel = entity,

					personnel = personnel,
					transform = transform,
				};
				gui.Submit();
			}
		}
#endif
	}

	public static partial class Office
	{
		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Global | IComponent.Scope.Region)]
		public partial struct Data: IComponent
		{
			[Flags]
			public enum Flags: uint
			{
				None = 0,

				Enable_Register_Company = 1u << 0,
				Enable_Invite_Company = 1u << 1,
				Enable_Evict_Company = 1u << 2,

				Enable_Offers = 1u << 16,
				Enable_Stores = 1u << 17,
				Enable_Info = 1u << 18,
				Enable_Blueprints = 1u << 19,

				//= 1u << 16,

				Force_Faction_Company = 1u << 24
			}

			public Office.Data.Flags flags;

			[Save.NewLine]
			public Filter.Mask<IMap.Industry> filter_industry;
			public Filter.Mask2x<ICompany.Permits> filter_permits;
			public Filter.Mask2x<ICompany.Flags> filter_flags;
		}

		//[ISystem.AddFirst(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global, order: 20), HasTag("initialized", true, Source.Modifier.Owned)]
		[ISystem.Modified.Component<Faction.Data>(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global, order: 200, Defer = true), HasTag("initialized", true, Source.Modifier.Owned)]
		public static void OnFactionModified(ISystem.Info.Common info, ref Region.Data.Common region, Entity entity,
		[Source.Owned] in Office.Data office, [Source.Owned] in Faction.Data faction, [Source.Owned] ref Company.Data company)
		{
			var dirty = false;

#if SERVER
			if (office.flags.HasAny(Office.Data.Flags.Force_Faction_Company))
			{
				ref var faction_data = ref faction.id.GetData(out var faction_asset);
				//App.WriteLine($"{faction_data.h_company}");

				if (faction_data.IsNotNull())
				{
					dirty |= company.h_company.TrySet(faction_data.h_company);
				}
				else
				{
					dirty |= company.h_company.TrySet(default);
				}
			}
#endif

			//App.WriteLine($"Office.OnFactionModified(): entity: {entity}; company: {company.h_company}; faction: {faction.id}", App.Color.Magenta);

			if (dirty)
			{
#if SERVER
				company.Sync(entity, true);
#endif
			}
		}


		public struct DEV_InviteCompanyRPC(): Net.IRPC<Office.Data>
		{
			public ICompany.Handle h_company;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Office.Data data)
			{
				ref var region = ref rpc.connection.GetRegionCommon();
				Assert.Check(region.IsNotNull(), Assert.Level.Error);

				ref var player = ref rpc.connection.GetPlayer();
				Assert.Check(player.IsNotNull(), Assert.Level.Error);

				ref var company_data = ref this.h_company.GetData();
				Assert.Check(company_data.IsNotNull(), Assert.Level.Error);

				ref var company = ref rpc.entity.GetComponent<Company.Data>();
				Assert.Check(company.IsNotNull(), Assert.Level.Error);
				Assert.Check(company.h_company == 0, Assert.Level.Error);

				// TODO: add ownership validation

				company.h_company = this.h_company;
				company.Sync(rpc.entity);
			}
#endif
		}

		public struct DEV_CreateCompanyRPC(): Net.IRPC<Office.Data>
		{
			public FixedString32 name;
			public FixedString16 name_short;
			public FixedString32 desc_short;
			public FixedString8 tag;

			public IOrganization.Handle h_organization;
			//public ICompany.Kind kind;
			public ICompany.Flags flags;
			public ICompany.Liability liability;
			public ICompany.Handle h_guarantor;
			public IFaction.Handle h_faction;

			public float share_capital;
			public int stocks_count;

			public ICompany.Permits permits;
			public IMap.Industry industry_primary;
			public IMap.Industry industry_secondary;
			public IMap.Industry industry_tertiary;

			//public FixedArray16<ICharacter.Handle> characters;

			//public IMap.Services services;
			//public IMap.Services services_secondary;
			//public IMap.Crime crime;

			public bool Validate()
			{
				var ok = true;
				ok &= this.tag.IsValidASCII(5);
				ok &= this.name.IsValidASCII();
				ok &= this.name_short.IsValidASCII();
				ok &= this.desc_short.IsEmptyOrValidASCII();

				ok &= this.h_organization;
				//ok &= this.kind != ICompany.Kind.Undefined;

				return ok;
			}

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Office.Data data)
			{
				//ref var region = ref connection.GetRegionCommon();
				//Assert.Check(region.IsNotNull(), Assert.Level.Error);


				Assert.Check(this.Validate());

				ref var player = ref rpc.connection.GetPlayer(out var player_asset);
				Assert.Check(player.IsNotNull(), Assert.Level.Error);

				var identifier = App.GetSanitizedIdentifier($"{this.tag}_{Asset.GenerateRandomIdentifier()}");
				Assert.Check(!identifier.IsNullOrEmpty());

				var company_data = new ICompany.Data
				{
					tag = this.tag,
					name = this.name,
					name_short = this.name_short,
					h_organization = this.h_organization,

					color_a = Color32BGRA.Neutral,
					color_b = Color32BGRA.Neutral
				};
				//company_data.name = this.name;
				//company_data.name_short = this.name_short;

				//company_data.h_organization = this.h_organization;

				//company_data.kind = this.kind;
				company_data.flags = this.flags;
				company_data.liability = this.liability;
				company_data.h_guarantor = this.h_guarantor;
				//company.h_faction = this.h_faction;

				company_data.share_capital = this.share_capital;
				company_data.shares_count = this.stocks_count;

				company_data.permits = this.permits;
				company_data.industry_primary = this.industry_primary;
				company_data.industry_secondary = this.industry_secondary;
				company_data.industry_tertiary = this.industry_tertiary;
				//company_data.services_produced = this.services;
				//company_data.crime = this.crime;

				company_data.players = new()
				{
					[player_asset] = new(ICompany.permissions_default_owner, IOffer.permissions_default_owner)
				};
				//App.WriteLine(identifier);

				var asset = ICompany.Database.RegisterOrUpdate(identifier,
					index: null,
					scope: Asset.Scope.World,
					flags: Asset.Flags.None,
					region_id: 0,
					data: ref company_data);

				asset.Sync(true);

				ref var company = ref rpc.entity.GetComponent<Company.Data>();
				if (company.IsNotNull())
				{
					company.h_company = asset;
					company.Sync(rpc.entity, true);
				}
			}
#endif
		}

		public struct DEV_CreateOfferRPC(): Net.IRPC<Office.Data>
		{
			public ICompany.Handle h_company;

#if SERVER
			// TODO: add ownership + permission checks
			public void Invoke(Net.IRPC.Context rpc, ref Office.Data data)
			{
				ref var region = ref rpc.connection.GetRegionCommon();
				Assert.Check(region.IsNotNull(), Assert.Level.Error);

				ref var player = ref rpc.connection.GetPlayer();
				Assert.Check(player.IsNotNull(), Assert.Level.Error);

				ref var company_data = ref this.h_company.GetData(out var s_company);
				if (company_data.IsNotNull())
				{
					//Assert.Check(company_data.fac)

					var offer = new IOffer.Data();
					offer.h_company = this.h_company;
					offer.flags |= IOffer.Flags.Editing | IOffer.Flags.Editable | IOffer.Flags.Custom;

					var identifier = Asset.GenerateRandomIdentifier();

					var asset = IOffer.Database.RegisterOrUpdate(identifier,
						index: null,
						scope: Asset.Scope.World,
						flags: Asset.Flags.None,
						region_id: 0,
						data: ref offer);

					asset.Sync(true);
				}
			}
#endif
		}

		public struct DEV_EditCompanyRPC: Net.IRPC<Office.Data>
		{
			public ICompany.Handle h_company;

			public FixedString64? desc_short;
			public FixedString32? motto;

			public Color32BGRA? color_a;
			public Color32BGRA? color_b;
			public Color32BGRA? color_icon;
			public Color32BGRA? color_glow;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Office.Data data)
			{
				ref var region = ref rpc.connection.GetRegionCommon();
				Assert.Check(region.IsNotNull(), Assert.Level.Error);

				ref var player = ref rpc.connection.GetPlayer();
				Assert.Check(player.IsNotNull(), Assert.Level.Error);

				ref var company_data = ref this.h_company.GetData();
				Assert.Check(company_data.IsNotNull(), Assert.Level.Error);

				var sync = false;

				if (this.desc_short.TryGetValue(out var desc_short_v))
				{
					company_data.desc_short = desc_short_v;
					sync = true;
				}

				if (this.motto.TryGetValue(out var motto_v))
				{
					company_data.motto = motto_v;
					sync = true;
				}

				sync |= this.color_a.TryCopyTo(ref company_data.color_a);
				sync |= this.color_b.TryCopyTo(ref company_data.color_b);
				sync |= this.color_icon.TryCopyTo(ref company_data.color_brand);
				sync |= this.color_glow.TryCopyTo(ref company_data.color_glow);

				company_data.color_a.a = 255;
				company_data.color_b.a = 255;
				company_data.color_brand.a = 255;

				this.h_company.Sync(false);
			}
#endif
		}

		public struct DEV_EditOfferRPC: Net.IRPC<Office.Data>
		{
			public IOffer.Handle h_offer;
			public IOffer.Flags flags;

			public int step;
			public int min;
			public int max;

			public int stock_min;
			public int stock_max;
			public int stock_default;

			public float fee;

			public FixedArray4<Shipment.Item> items_a;
			public FixedArray4<Shipment.Item> items_b;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Office.Data data)
			{
				ref var region = ref rpc.connection.GetRegionCommon();
				Assert.Check(region.IsNotNull(), Assert.Level.Error);

				ref var player = ref rpc.connection.GetPlayer();
				Assert.Check(player.IsNotNull(), Assert.Level.Error);

				ref var offer_data = ref this.h_offer.GetData();
				Assert.Check(offer_data.IsNotNull(), Assert.Level.Error);
				//Assert.Check(offer_data.flags.HasAny(IOffer.Flags.Editable), Assert.Level.Error);

				//var items_span_a_tmp = this.items_a.AsSpan();
				//var items_span_b_tmp = this.items_b.AsSpan();

				//var items_span_a = offer_data.items_a.AsSpan();
				//var items_span_b = offer_data.items_b.AsSpan();

				// TODO: add validation

				offer_data.flags = this.flags;

				offer_data.min = this.min;
				offer_data.max = this.max;
				offer_data.step = this.step;

				offer_data.stock_min = this.stock_min;
				offer_data.stock_max = this.stock_max;
				offer_data.stock_default = this.stock_default;

				offer_data.fee = this.fee;

				offer_data.items_a = this.items_a;
				offer_data.items_b = this.items_b;

				offer_data.version++;

				this.h_offer.Sync(false);

				//data.Sync(entity, true);
			}
#endif
		}

		public static bool Validate(ref this IOffer.Data offer)
		{
			var valid = true;

			ref var company_data = ref offer.h_company.GetData();
			valid &= company_data.IsNotNull();

			valid &= offer.items_a.AsSpan().Any();
			valid &= offer.items_b.AsSpan().Any();

			valid &= offer.min > 0;
			valid &= offer.max >= offer.min;
			valid &= offer.step >= offer.min && offer.step <= offer.max;

			return valid;
		}

		public struct DEV_OfferActionRPC: Net.IRPC<Office.Data>
		{
			public enum ActionType: byte
			{
				Undefined = 0,
				Publish,
				Remove,
			}

			public IOffer.Handle h_offer;
			public Office.DEV_OfferActionRPC.ActionType action;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Office.Data data)
			{
				ref var region = ref rpc.connection.GetRegionCommon();
				Assert.Check(region.IsNotNull(), Assert.Level.Error);

				ref var player = ref rpc.connection.GetPlayerData();
				Assert.Check(player.IsNotNull(), Assert.Level.Error);

				ref var offer_data = ref this.h_offer.GetData(out var offer_asset);
				Assert.Check(offer_data.IsNotNull(), Assert.Level.Error);

				switch (this.action)
				{
					case Office.DEV_OfferActionRPC.ActionType.Publish:
					{
						Assert.Check(offer_data.Validate(), Assert.Level.Error);

						var flags = IOffer.Flags.Published;

						//App.WriteLine(offer_data.flags.ToFormattedBinary(), show_stacktrace: false);
						//App.WriteLine(flags.ToFormattedBinary(), show_stacktrace: false);
						offer_data.flags.ToggleFlag(flags);
						//App.WriteLine(offer_data.flags.ToFormattedBinary(), show_stacktrace: false);
						//App.WriteLine("", show_stacktrace: false);
					}
					break;

					case Office.DEV_OfferActionRPC.ActionType.Remove:
					{
						offer_asset.Recycle();
					}
					break;
				}

				offer_data.version++;
				this.h_offer.Sync(false);
			}
#endif
		}

		public static bool CanPlayerEditOffer(IPlayer.Handle h_player, IOffer.Handle h_offer)
		{
			if (h_player == 0 || h_offer == 0) return false;

			ref var player_data = ref h_player.GetData();
			if (player_data.IsNull()) return false;

			ref var offer_data = ref h_offer.GetData();
			if (offer_data.IsNull()) return false;

			if (!offer_data.flags.HasAny(IOffer.Flags.Editable)) return false;

			ref var company_data = ref offer_data.h_company.GetData();
			if (company_data.IsNull()) return false;

			//company_data.owners.AsSpan().fin

			return true;
		}

#if CLIENT
		public static void DrawOffer(uint id, ref Crafting.Context context, ref IOffer.Data offer_data, bool is_editor = false, bool is_readonly = false, Entity ent_market = default, byte? index = null, ICompany.Handle h_company_self = default, float fee = 0.00f)
		{
			var tex_slot = GUI.tex_frame_white;

			var col_button_buy = GUI.col_buy;
			var col_button_sell = GUI.col_sell;

			var col_slot_buy = GUI.col_buy;
			var col_slot_sell = GUI.col_sell;

			using (GUI.ID<IOffer.Data, Market.Data>.Push(id))
			{
				if (offer_data.IsNotNull())
				{
					ref var company_data_offer = ref offer_data.h_company.GetData();

					//GUI.TitleCentered($"{offer_data.type}", pivot: new(0.00f, 0.50f), offset: new(8, 0));
					//GUI.SameLine();

					var pressed = GUI.DrawCounterOut(identifier: "offer.counter"u8,
						size: new Vector2(72, GUI.RmY),
						value: out var amount_mult,
						step: offer_data.step,
						min: offer_data.min,
						max: offer_data.max,
						is_readonly: is_readonly,
						logarithmic: true);

					var items_a = offer_data.items_a;
					var items_b = offer_data.items_b;

					var items_a_span = items_a.AsSpan();
					var items_b_span = items_b.AsSpan();

					var stocked_amount_sell = 0;
					var stocked_amount_buy = 0;

					var has_buy_stock = false;
					var has_sell_stock = false;

					if (ent_market.IsAlive() && index.HasValue)
					{
						ref var stock = ref ent_market.GetIndexPair<Market.Stock.Data>(index.Value);
						if (stock.IsNotNull())
						{
							if (offer_data.flags.HasAny(IOffer.Flags.Buy))
							{
								stocked_amount_buy = int.MaxValue;
								has_buy_stock = true;

								for (var i = 0; has_buy_stock && i < items_a_span.Length; i++)
								{
									ref var item = ref items_a_span[i];
									if (item.type == Shipment.Item.Type.Undefined || item.quantity <= 0) continue;

									var amount = stock.amounts_a[i];

									stocked_amount_buy = Math.Min((int)(amount / item.quantity), stocked_amount_buy);
									has_buy_stock &= amount >= (item.quantity * amount_mult);
								}
							}
							else
							{
								stocked_amount_buy = 0;
							}

							if (offer_data.flags.HasAny(IOffer.Flags.Sell))
							{
								stocked_amount_sell = int.MaxValue;
								has_sell_stock = true;

								for (var i = 0; has_sell_stock && i < items_b_span.Length; i++)
								{
									ref var item = ref items_b_span[i];
									if (item.type == Shipment.Item.Type.Undefined || item.quantity <= 0) continue;

									var amount = stock.amounts_b[i];

									var amount_add = 0.00f;
									if (fee > 0.01f && item.type == Shipment.Item.Type.Resource && item.material == Money.h_material_money)
									{
										amount_add = MathF.Ceiling(item.quantity * fee);
									}

									stocked_amount_sell = Math.Min((int)(amount / item.quantity), stocked_amount_sell);
									has_sell_stock &= amount >= (item.quantity * amount_mult);
								}
							}
							else
							{
								stocked_amount_sell = 0;
							}
						}
					}

					var eval_flags = Crafting.EvaluateFlags.Prerequisite;
					if (is_editor || is_readonly)
					{
						eval_flags |= Crafting.EvaluateFlags.Display_Only;
					}

					GUI.SameLine(2);

					using (var group_items = GUI.Group.New(size: GUI.Rm))
					{
						using (var group_button = GUI.Group.New(size: new(52, 48), padding: new(0, 4)))
						{
							Span<Crafting.Product> products_span = stackalloc Crafting.Product[items_a_span.Length];
							items_a_span.ToProducts(ref products_span);

							Span<Crafting.Requirement> requirements_span = stackalloc Crafting.Requirement[items_b_span.Length];
							items_b_span.ToRequirements(ref requirements_span);

							requirements_span.TrimInvalid();
							requirements_span.ForEach((ref x) => x.flags |= Crafting.Requirement.Flags.Prerequisite);

							products_span.TrimInvalid();

							var enabled = offer_data.flags.HasAny(IOffer.Flags.Buy);

							var color = GUI.font_color_default_dark;
							var text_color = GUI.font_color_disabled;

							if (offer_data.flags.HasAny(IOffer.Flags.Buy))
							{
								color = GUI.col_button_ok;
								text_color = GUI.font_color_button_text;
							}

							//if (GUI.DrawCraftButton(ref context, requirements_span, products_span, "Buy", size: GUI.Rm, color: GUI.col_button_ok, enabled: !is_editor && !is_readonly && index.HasValue && ent_market.IsValid(), amount_multiplier: amount_mult))
							if (GUI.DrawCraftButton(context: ref context, requirements: requirements_span, products: products_span, text: "Buy"u8, size: GUI.Rm, color: color, text_color: text_color,
								enabled: enabled && ((!is_readonly && has_buy_stock)), amount_multiplier: amount_mult, eval_flags: eval_flags))
							{
								if (!is_editor && !is_readonly && index.HasValue && ent_market.IsValid())
								{
									var rpc = new Market.TradeRPC()
									{
										index = index.Value,
										amount_multiplier = amount_mult
									};
									rpc.Send(ent_market);
								}
							}

							if (enabled && !is_readonly && !is_editor && !has_buy_stock)
							{
								if (GUI.IsItemHovered())
								{
									using (GUI.Tooltip.New())
									{
										GUI.NewLine(8);
										GUI.TitleCentered("- - Out of stock - -"u8, size: 32, color: GUI.font_color_yellow, pivot: new(0.50f, 0.50f), offset: new(0, -24));
									}
								}

								var rect = GUI.GetLastItemRect();
								//GUI.DrawSpriteCentered(new Sprite("ui_icons_trading", 16, 16, 4, 0), rect, GUI.Layer.Window, scale: 2.25f, color: GUI.font_color_red.WithAlphaMult(0.50f));
								GUI.DrawSpriteCentered(GUI.tex_icons_widget.GetSprite(16, 16, 3, 7), rect, GUI.Layer.Window, scale: 2, color: Color32BGRA.DarkRed.WithAlphaMult(0.50f));
							}

							//if (GUI.IsItemHovered())
							//{
							//	GUI.ScrollInput()
							//}
						}

						GUI.SameLine();

						using (var group_button = GUI.Group.New(size: new(52, 48), padding: new(0, 4)))
						{
							Span<Crafting.Product> products_span = stackalloc Crafting.Product[items_b_span.Length];
							items_b_span.ToProducts(ref products_span);

							Span<Crafting.Requirement> requirements_span = stackalloc Crafting.Requirement[items_a_span.Length];
							items_a_span.ToRequirements(ref requirements_span);

							requirements_span.TrimInvalid();
							requirements_span.ForEach((ref x) => x.flags |= Crafting.Requirement.Flags.Prerequisite);

							products_span.TrimInvalid();

							var enabled = offer_data.flags.HasAny(IOffer.Flags.Sell);

							var color = GUI.font_color_default_dark;
							var text_color = GUI.font_color_disabled;

							if (offer_data.flags.HasAny(IOffer.Flags.Sell))
							{
								color = GUI.col_button_error;
								text_color = GUI.font_color_button_text;
							}

							//if (GUI.DrawCraftButton(ref context, requirements_span, products_span, "Sell", size: GUI.Rm, color: GUI.col_button_error, enabled: !is_editor && !is_readonly && index.HasValue && ent_market.IsValid(), amount_multiplier: amount_mult))
							if (GUI.DrawCraftButton(context: ref context, requirements: requirements_span, products: products_span, text: "Sell"u8, size: GUI.Rm, color: color, text_color: text_color,
								enabled: enabled && ((!is_readonly && has_sell_stock)), amount_multiplier: amount_mult, eval_flags: eval_flags))
							{
								if (!is_editor && !is_readonly && index.HasValue && ent_market.IsValid())
								{
									var rpc = new Market.TradeRPC()
									{
										index = index.Value,
										amount_multiplier = amount_mult,
										sell = 1
									};
									rpc.Send(ent_market);
								}
							}

							if (enabled && !is_readonly && !is_editor && !has_sell_stock)
							{
								if (GUI.IsItemHovered())
								{
									using (GUI.Tooltip.New())
									{
										GUI.NewLine(8);
										GUI.TitleCentered("- - Out of stock - -"u8, size: 32, color: GUI.font_color_yellow, pivot: new(0.50f, 0.50f), offset: new(0, -24));
									}
								}

								var rect = GUI.GetLastItemRect();
								GUI.DrawSpriteCentered(GUI.tex_icons_widget.GetSprite(16, 16, 3, 7), rect, GUI.Layer.Window, scale: 2, color: Color32BGRA.DarkRed.WithAlphaMult(0.50f));
							}

							//if (GUI.IsItemHovered())
							//{
							//	GUI.ScrollInput()
							//}
						}

						GUI.SameLine(2);

						foreach (ref var item in items_a_span)
						{
							if (item.type != Shipment.Item.Type.Undefined)
							{
								//GUI.SameLine();
								using (var group_slot = GUI.Group.New(size: new(48, 48), padding: new(4)))
								{
									group_slot.DrawBackground(tex_slot, color: item.type == Shipment.Item.Type.Undefined ? GUI.font_color_default_dark : col_slot_buy, inner: false);
									GUI.DrawItem(ref item, size: GUI.Rm, true, amount_multiplier: amount_mult);
									//GUI.DrawRect(group_slot.GetOuterRect(), Color32BGRA.Yellow, layer: GUI.Layer.Window);
								}
								GUI.SameLine();
							}
						}

						using (var group_slot = GUI.Group.New(size: new(48, 48)))
						{
							//GUI.DrawSpriteCentered(new Sprite(tex_icons_trading, 16, 16, 1, 1), group_slot.GetOuterRect(), GUI.Layer.Window, 2.00f);
							GUI.TitleCentered("for"u8, size: 16, pivot: new(0.50f, 0.50f));
						}

						GUI.SameLine();

						foreach (ref var item in items_b_span)
						{
							if (item.type != Shipment.Item.Type.Undefined)
							{
								//GUI.SameLine();
								using (var group_slot = GUI.Group.New(size: new(48, 48), padding: new(4)))
								{
									var amount_add = 0.00f;
									if (fee >= 0.01f && item.type == Shipment.Item.Type.Resource && item.material == Money.h_material_money)
									{
										//amount_add = MathF.Ceiling(item.quantity * fee);
									}

									group_slot.DrawBackground(tex_slot, item.type == Shipment.Item.Type.Undefined ? GUI.font_color_default_dark : col_slot_sell, inner: false);
									GUI.DrawItem(ref item, size: GUI.Rm, true, amount_multiplier: amount_mult, amount_add: amount_add);
								}
								GUI.SameLine();
							}
						}

						{
							if (company_data_offer.IsNotNull())
							{
								//GUI.TitleCentered($"* {company_data_offer.name}", pivot: new(1.00f, 0.00f), offset: new(-4, 4));
								//GUI.DrawTextCentered($"{company_data_offer.name}", size: 16, position: group_items.GetInnerRect().GetPosition(new(1.00f, 0.00f)) + new Vector2(-4, 4), pivot: new(1.00f, 0.00f), font: GUI.Font.Superstar, layer: GUI.Layer.Window);
								GUI.DrawTextCentered(company_data_offer.name, size: 32, position: group_items.GetInnerRect().GetPosition(new(1.00f, 0.50f)) + new Vector2(-12, 0), pivot: new(1.00f, 0.50f), color: GUI.font_color_title.LumaBlend(company_data_offer.color_brand, 0.50f).WithAlphaMult(0.15f), font: GUI.Font.Superstar, layer: GUI.Layer.Window);
							}
						}

						var rect_rem = GUI.GetRemainingRect();
						if (GUI.IsHoveringRect(rect_rem))
						{
							using (GUI.Tooltip.New(size: new(192, 0)))
							{
								using (GUI.Wrap.Push(GUI.RmX))
								{
									if (company_data_offer.IsNotNull())
									{
										if (index.HasValue)
										{
											GUI.Title("Stock"u8);
											GUI.SeparatorThick();

											GUI.LabelShaded("Buyable:"u8, stocked_amount_buy, "0", font_a: GUI.Font.Superstar, size_a: 16);
											GUI.LabelShaded("Sellable:"u8, stocked_amount_sell, "0", font_a: GUI.Font.Superstar, size_a: 16);

											GUI.NewLine();
										}

										//GUI.LabelShaded("Fee:", offer_data.fee, "P2");
										//GUI.LabelShaded("Fee:", offer_data.fee, "P2");
										//GUI.LabelShaded("Fee:", offer_data.fee, "P2");

										GUI.LabelShaded("Company:"u8, company_data_offer.name, font_a: GUI.Font.Superstar, size_a: 16);
									}
								}
							}
						}
					}
				}
			}
		}

		public partial struct RegisterCompanyGUI: IGUICommand
		{
			public Entity ent_office;
			public Office.Data office;
			public Company.Data company;

			public static ICompany.Handle h_company_selected;
			public static Office.DEV_CreateCompanyRPC rpc_new_company;

			public static int selected_tab;

			public void Draw()
			{
				if (this.office.flags.HasAny(Data.Flags.Enable_Register_Company))
				{
					using (var window = GUI.Window.Interaction("Register Company###company.gui"u8, this.ent_office))
					{
						this.StoreCurrentWindowTypeID(order: -140);
						if (window.show)
						{
							//ref var player = ref Client.GetPlayer();
							ref var region = ref this.ent_office.GetRegionCommon();
							ref var character = ref Client.GetCharacter(out var character_asset);
							//ref var map_info = ref region.GetMapInfo();

							ref var organization = ref rpc_new_company.h_organization.GetData();

							Span<Crafting.Requirement> requirements_span = stackalloc Crafting.Requirement[4];

							var org_valid = organization.IsNotNull();
							if (org_valid)
							{
								if (organization.liability.GetCount() <= 1 && organization.liability.TryGetFirst(out rpc_new_company.liability))
								{
									//rpc_new_company.liability = organization.liability.
								}

								rpc_new_company.permits |= organization.filter_permits.require;
								rpc_new_company.flags |= organization.filter_flags.require;
								rpc_new_company.industry_primary |= organization.industry.require;
								//rpc_new_company.services |= organization.services.require;

								rpc_new_company.permits &= organization.filter_permits.require | organization.filter_permits.include;
								rpc_new_company.flags &= organization.filter_flags.require | organization.filter_flags.include;
								rpc_new_company.industry_primary &= organization.industry.require | organization.industry.include;
								//rpc_new_company.services &= organization.services.require | organization.services.include;
							}

							Crafting.Context.NewFromCharacter(ref region, character_asset, ent_producer: this.ent_office, out var context);

							using (GUI.Group.New(size: GUI.GetRemainingSpace(y: -42), padding: new(0)))
							{
								using (var group_left = GUI.Group.New(size: new(GUI.RmX * 0.50f, GUI.RmY), padding: new(4)))
								{
									using (var group = GUI.Group.New(size: new(GUI.RmX, 48), padding: new(4)))
									{
										GUI.Title("Company Registration Form"u8, size: 24);
										//if (map_info.IsNotNull())
										//{
										//	//ref var prefecture_data = ref map_info.h_prefecture.GetData();
										//	//if (prefecture_data.IsNotNull())
										//	//{
										//	//	GUI.Title(prefecture_data.name, size: 16);

										//	//	ref var governorate_data = ref prefecture_data.h_governorate.GetData();
										//	//	if (governorate_data.IsNotNull())
										//	//	{
										//	//		GUI.SameLine();
										//	//		GUI.Title($", prefecture of {governorate_data.name}", size: 16);
										//	//	}
										//	//}
										//}
									}

									GUI.SeparatorThick();

									using (var group = GUI.Group.New(size: GUI.Rm, padding: new(0)))
									{
										using (var group_info = GUI.Group.New(size: new(174, 168), padding: new(4)))
										{
											using (GUI.Wrap.Push(GUI.RmX))
											{
												GUI.Text("Operation on the Imperial Market is permitted only to legal entities registered within the Imperial Revenue Service Register.\n\nIn order to be granted a permit to operate on the Imperial Market, submit this this form filled out with all relevant information."u8);
											}
										}

										GUI.SameLine();

										using (var group_info = GUI.Group.New(size: new(GUI.RmX, 0), padding: new(0, 8)))
										{
											//if (Test("a", ref rpc_new_company.h_organization, default))
											//{

											//}

											if (GUI.AssetInput2("company.type"u8, ref rpc_new_company.h_organization, new(GUI.RmX, 32), show_label: false, filter: (x) => !x.data.flags.HasAny(IOrganization.Flags.Hidden)))
											{
												rpc_new_company.flags.SetFlag(ICompany.Flags.Private | ICompany.Flags.Collective | ICompany.Flags.Public, false);
											}

											GUI.NewLine(12);

											if (GUI.TextInput("company.name"u8, "name"u8, ref rpc_new_company.name, new(GUI.RmX, 32)))
											{

											}

											//if (GUI.TextInput("company.name_short", "name, short", ref rpc_new_company.name_short, new(GUI.RmX, 32)))
											//{

											//}

											if (GUI.TextInput("company.tagu8", "tag"u8, ref rpc_new_company.tag, new(48, 32)))
											{

											}

											GUI.SameLine();

											if (GUI.TextInput("company.name_short"u8, "name, short"u8, ref rpc_new_company.name_short, new(GUI.RmX, 32)))
											{

											}

											if (GUI.TextInput("company.desc_short"u8, "description"u8, ref rpc_new_company.desc_short, new(GUI.RmX, 32)))
											{

											}
										}

										GUI.NewLine(4);

										using (var group_input = GUI.Group.New(size: GUI.Rm))
										{
											//if (GUI.Checkbox("Private", ref rpc_new_company.flags, ICompany.Flags.Private, new(100, 32), enabled: org_valid && organization.filter_flags.include.HasAny(ICompany.Flags.Private))) // && !rpc_new_company.flags.HasAny(ICompany.Flags.Public | ICompany.Flags.Collective)))
											//{
											//	rpc_new_company.flags.SetFlag(ICompany.Flags.Public | ICompany.Flags.Collective, false);
											//}

											//GUI.SameLine();

											//if (GUI.Checkbox("Public", ref rpc_new_company.flags, ICompany.Flags.Public, new(100, 32), enabled: org_valid && organization.filter_flags.include.HasAny(ICompany.Flags.Public))) // && !rpc_new_company.flags.HasAny(ICompany.Flags.Private | ICompany.Flags.Collective)))
											//{
											//	rpc_new_company.flags.SetFlag(ICompany.Flags.Private | ICompany.Flags.Collective, false);
											//}

											//GUI.SameLine();

											//if (GUI.Checkbox("Collective", ref rpc_new_company.flags, ICompany.Flags.Collective, new(100, 32), enabled: org_valid && organization.filter_flags.include.HasAny(ICompany.Flags.Collective))) // && !rpc_new_company.flags.HasAny(ICompany.Flags.Public | ICompany.Flags.Private)))
											//{
											//	rpc_new_company.flags.SetFlag(ICompany.Flags.Private | ICompany.Flags.Public, false);
											//}

											//GUI.NewLine(8);

											//if (GUI.EnumInputMasked("Liability", ref rpc_new_company.liability, new(200 + 20, 32), enabled: org_valid && organization.liability.GetCount() > 1, mask: org_valid ? organization.liability : default))
											//{

											//}

											////if (GUI.Checkbox("Limited Liability", ref rpc_new_company.flags, ICompany.Flags.Limited_Liability, new(GUI.RmX, 32), enabled: mask_flags.HasAny(ICompany.Flags.Limited_Liability)))
											////{

											////}

											//GUI.NewLine(8);

											//var specialization_mask = IMap.Industry.Administrative | IMap.Industry.Agricultural | IMap.Industry.Arcane | IMap.Industry.
											if (GUI.EnumInput("Primary"u8, ref rpc_new_company.industry_primary, new(GUI.RmX, 32), enabled: true, mask: (org_valid ? organization.industry.include : default) & ~(rpc_new_company.industry_secondary | rpc_new_company.industry_tertiary), close_on_select: true, exclusive: true, label_ratio: 0.60f))
											{

											}

											GUI.NewLine(8);

											if (GUI.EnumInput("Secondary"u8, ref rpc_new_company.industry_secondary, new(GUI.RmX, 32), enabled: org_valid && organization.industry_max > 0 && !organization.industry.include.IsEmpty(), mask: (org_valid ? organization.industry.include : default) & ~(rpc_new_company.industry_primary | rpc_new_company.industry_tertiary), max_flags: org_valid ? organization.industry_max : 0, label_ratio: 0.72f))
											{

											}

											GUI.NewLine(8);

											if (GUI.EnumInput("Tertiary"u8, ref rpc_new_company.industry_tertiary, new(GUI.RmX, 32), enabled: org_valid && organization.industry_max > 0 && !organization.industry.include.IsEmpty(), mask: (org_valid ? organization.industry.include : default) & ~(rpc_new_company.industry_primary | rpc_new_company.industry_secondary), max_flags: org_valid ? organization.industry_max : 0, label_ratio: 0.72f))
											{

											}

											//GUI.NewLine(8);

											//if (GUI.EnumInput("Industry (Secondary)", ref rpc_new_company.industry_secondary, new(GUI.RmX, 32), enabled: org_valid && organization.industry_secondary_max > 0 && !organization.industry_secondary.include.IsEmpty(), mask: org_valid ? organization.industry_secondary.include : default, max_flags: org_valid ? organization.industry_secondary_max : 0))
											//{

											//}

											//GUI.NewLine(8);

											//if (GUI.EnumInput("Services", ref rpc_new_company.services, new(GUI.RmX, 32), enabled: org_valid && organization.services_max > 0 && !organization.services.include.IsEmpty(), mask: org_valid ? organization.services.include : default, max_flags: org_valid ? organization.services_max : 0, label_ratio: 0.72f))
											//{

											//}

											//if (GUI.EnumInput("company.flags", ref rpc_new_company.flags, new(GUI.RmX, 32), show_label: false))
											//{

											//}
										}
									}
								}

								GUI.SameLine();

								using (var group_right = GUI.Group.New(size: GUI.Rm, padding: new(4)))
								{
									using (var group = GUI.Group.New(size: new(GUI.RmX, 48), padding: new(0)))
									{
										GUI.DrawTab3("Legal"u8, new Vector2(0, GUI.RmY), 0, ref selected_tab, inner: true);

										GUI.SameLine();

										//GUI.DrawTab3("Taxes", new Vector2(120, GUI.RmY), 1, ref selected_tab, inner: true);

										//GUI.SameLine();

										GUI.DrawTab3("Permits"u8, new Vector2(120, GUI.RmY), 2, ref selected_tab, inner: true);

										GUI.SameLine();
									}

									GUI.SeparatorThick();

									using (var group_tab = GUI.Group.New(size: GUI.Rm, padding: new(8)))
									{
										group_tab.DrawBackground(GUI.tex_panel);

										if (selected_tab == 0)
										{
											//using (var group = GUI.Group.New(size: new(GUI.RmX, 0), padding: new(0)))
											//{
											//	//if (GUI.EnumInputMasked("Liability", ref rpc_new_company.liability, new(200 + 20, 32), enabled: org_valid && organization.liability.GetCount() > 1, mask: org_valid ? organization.liability : default))
											//	//{

											//	//}

											//	//GUI.NewLine(12);

											//	if (GUI.Checkbox("Private", ref rpc_new_company.flags, ICompany.Flags.Private, new(100, 32), enabled: org_valid && organization.filter_flags.include.HasAny(ICompany.Flags.Private))) // && !rpc_new_company.flags.HasAny(ICompany.Flags.Public | ICompany.Flags.Collective)))
											//	{
											//		rpc_new_company.flags.SetFlag(ICompany.Flags.Public | ICompany.Flags.Collective, false);
											//	}

											//	GUI.SameLine();

											//	if (GUI.Checkbox("Public", ref rpc_new_company.flags, ICompany.Flags.Public, new(100, 32), enabled: org_valid && organization.filter_flags.include.HasAny(ICompany.Flags.Public))) // && !rpc_new_company.flags.HasAny(ICompany.Flags.Private | ICompany.Flags.Collective)))
											//	{
											//		rpc_new_company.flags.SetFlag(ICompany.Flags.Private | ICompany.Flags.Collective, false);
											//	}

											//	GUI.SameLine();

											//	if (GUI.Checkbox("Collective", ref rpc_new_company.flags, ICompany.Flags.Collective, new(100, 32), enabled: org_valid && organization.filter_flags.include.HasAny(ICompany.Flags.Collective))) // && !rpc_new_company.flags.HasAny(ICompany.Flags.Public | ICompany.Flags.Private)))
											//	{
											//		rpc_new_company.flags.SetFlag(ICompany.Flags.Private | ICompany.Flags.Public, false);
											//	}
											//}

											using (var group = GUI.Group.New(size: new(GUI.RmX, 100), padding: new(0)))
											{
												using (GUI.Group.New2(size: new(group.size.X * 0.50f, group.size.Y), padding: new(4, 4, 12, 4)))
												{
													GUI.Title("Taxation"u8, size: 22);

													GUI.NewLine(2);

													GUI.SeparatorThick();

													GUI.NewLine(4);

													if (org_valid)
													{
														GUI.LabelShaded("Income:"u8, organization.tax_income, format: "P2", font_a: GUI.Font.Superstar, size_a: 16);
														GUI.LabelShaded("Dividends:"u8, organization.tax_dividends, format: "P2", font_a: GUI.Font.Superstar, size_a: 16);
														GUI.LabelShaded("Property:"u8, organization.tax_property, format: "P2", font_a: GUI.Font.Superstar, size_a: 16);
														GUI.LabelShaded("Wages:"u8, organization.tax_wage, format: "P2", font_a: GUI.Font.Superstar, size_a: 16);
													}
												}

												GUI.SameLine();

												using (GUI.Group.New2(size: new(group.size.X * 0.50f, group.size.Y), padding: new(12, 4, 4, 4)))
												{
													GUI.Title("-TODO-"u8, size: 22);

													GUI.NewLine(2);

													GUI.SeparatorThick();

													GUI.NewLine(4);

													//foreach (var flag in rpc_new_company.permits)
													//{
													//	GUI.TextShaded(flag.GetEnumName());
													//}
													////GUI.LabelShaded("Income tax:", organization.filter_flags.include);
													////GUI.LabelShaded("Dividend tax:", organization.tax_dividends);
													////GUI.LabelShaded("Property tax:", organization.tax_property);
													////GUI.LabelShaded("Payroll tax:", organization.tax_wage);
												}
											}

											using (var group = GUI.Group.New(size: new(GUI.RmX, 0), padding: new(0)))
											{
												GUI.Title("Ownership"u8, size: 22);

												GUI.NewLine(2);

												GUI.SeparatorThick();

												GUI.NewLine(4);

												if (GUI.Checkbox("Private"u8, ref rpc_new_company.flags, ICompany.Flags.Private, new(100, 32), enabled: org_valid && organization.filter_flags.include.HasAny(ICompany.Flags.Private))) // && !rpc_new_company.flags.HasAny(ICompany.Flags.Public | ICompany.Flags.Collective)))
												{
													rpc_new_company.flags.SetFlag(ICompany.Flags.Public | ICompany.Flags.Collective, false);
												}

												GUI.SameLine();

												if (GUI.Checkbox("Public"u8, ref rpc_new_company.flags, ICompany.Flags.Public, new(100, 32), enabled: org_valid && organization.filter_flags.include.HasAny(ICompany.Flags.Public))) // && !rpc_new_company.flags.HasAny(ICompany.Flags.Private | ICompany.Flags.Collective)))
												{
													rpc_new_company.flags.SetFlag(ICompany.Flags.Private | ICompany.Flags.Collective, false);
												}

												GUI.SameLine();

												if (GUI.Checkbox("Collective"u8, ref rpc_new_company.flags, ICompany.Flags.Collective, new(100, 32), enabled: org_valid && organization.filter_flags.include.HasAny(ICompany.Flags.Collective))) // && !rpc_new_company.flags.HasAny(ICompany.Flags.Public | ICompany.Flags.Private)))
												{
													rpc_new_company.flags.SetFlag(ICompany.Flags.Private | ICompany.Flags.Public, false);
												}

												GUI.NewLine(4);

												using (GUI.Wrap.Push(GUI.RmX))
												{
													if (rpc_new_company.flags.HasAny(ICompany.Flags.Private))
													{
														GUI.Text("- Company is owned by select few members."u8);
														if (rpc_new_company.flags.HasAny(ICompany.Flags.Stocks)) GUI.Text("- Shares may be only traded internally."u8);
													}
													else if (rpc_new_company.flags.HasAny(ICompany.Flags.Public))
													{
														GUI.Text("- Company is owned by its shareholders."u8);
														if (rpc_new_company.flags.HasAny(ICompany.Flags.Stocks)) GUI.Text("- Shares may be traded on the stock market."u8);
													}
													else if (rpc_new_company.flags.HasAny(ICompany.Flags.Collective)) GUI.Text("- Company is owned by its employees."u8);
													else if (rpc_new_company.flags.HasAny(ICompany.Flags.State_Owned)) GUI.Text("- Company is owned the state."u8);
												}
											}

											GUI.NewLine(12);

											using (var group = GUI.Group.New(size: new(GUI.RmX, 0), padding: new(0)))
											{
												GUI.Title("Liability"u8, size: 22);

												GUI.NewLine(2);

												GUI.SeparatorThick();

												//GUI.NewLine(4);

												if (GUI.EnumInputMasked("Liability"u8, ref rpc_new_company.liability, new(200 + 20, 32), enabled: org_valid && organization.liability.GetCount() > 1, mask: org_valid ? organization.liability : default, show_label: false))
												{

												}

												GUI.NewLine(4);

												using (GUI.Wrap.Push(GUI.RmX))
												{

												}
											}
										}
										else if (selected_tab == 1)
										{


											//GUI.SeparatorThick();

											//using (var group = GUI.Group.New(size: new(GUI.RmX, 100), padding: new(0)))
											//{
											//	using (GUI.Group.New2(size: new(group.size.X * 0.50f, group.size.Y), padding: new(4, 4, 12, 4)))
											//	{
											//		GUI.Title("Taxation", size: 22);

											//		GUI.NewLine(2);

											//		GUI.SeparatorThick();

											//		GUI.NewLine(4);

											//		if (org_valid)
											//		{
											//			GUI.LabelShaded("Income tax:", organization.tax_income, format: "P2");
											//			GUI.LabelShaded("Dividend tax:", organization.tax_dividends, format: "P2");
											//			GUI.LabelShaded("Property tax:", organization.tax_property, format: "P2");
											//			GUI.LabelShaded("Payroll tax:", organization.tax_wage, format: "P2");
											//		}
											//	}

											//	GUI.SameLine();

											//	using (GUI.Group.New2(size: new(group.size.X * 0.50f, group.size.Y), padding: new(12, 4, 4, 4)))
											//	{
											//		GUI.Title("B", size: 22);

											//		GUI.NewLine(2);

											//		GUI.SeparatorThick();

											//		GUI.NewLine(4);

											//		//foreach (var flag in rpc_new_company.permits)
											//		//{
											//		//	GUI.TextShaded(flag.GetEnumName());
											//		//}
											//		////GUI.LabelShaded("Income tax:", organization.filter_flags.include);
											//		////GUI.LabelShaded("Dividend tax:", organization.tax_dividends);
											//		////GUI.LabelShaded("Property tax:", organization.tax_property);
											//		////GUI.LabelShaded("Payroll tax:", organization.tax_wage);
											//	}
											//}

											using (var group = GUI.Group.New(size: new(GUI.RmX, 100), padding: new(0)))
											{
												if (rpc_new_company.flags.HasAny(ICompany.Flags.Stocks))
												{
													using (GUI.Group.New2(size: new(group.size.X, group.size.Y), padding: new(4, 4, 4, 4)))
													{
														GUI.Title("Shares"u8, size: 22);

														GUI.NewLine(2);

														GUI.SeparatorThick();

														GUI.NewLine(4);

														//if (GUI.SliderFloat("Share Capital", ref rpc_new_company.share_capital, organization.min_share_capital, organization.min_share_capital * 10, snap: 5000.00f, size: new(GUI.RmX * 0.50f, 32), show_label: false))
														//{

														//}

														//GUI.SameLine();

														using (GUI.Group.New(size: new(GUI.RmX, 32)))
														{
															var amount = 1000.00f;

															GUI.DrawMoney(rpc_new_company.share_capital, GUI.GetRemainingSpace(x: -200));

															if (GUI.IsItemHovered())
															{
																var scroll = GUI.GetMouse().GetScroll();
																if (scroll != 0.00f)
																{
																	rpc_new_company.share_capital = Maths.Max(rpc_new_company.share_capital - (scroll * amount), organization.min_share_capital);

																	//edit_transfer_money_amount -= (scroll * 100);
																	//edit_transfer_money_amount.SnapCeil(100);

																	Sound.PlayGUI(GUI.sound_select, volume: 0.07f);

																	//App.WriteLine(scroll);
																}
															}

															GUI.SameLine();

															GUI.TitleCentered("Initial Share Capital"u8, color: GUI.font_color_title, pivot: new(1.00f, 0.50f), offset: new(-8, 0));


															//GUI.TitleCentered(identifier, color: GUI.font_color_title.WithAlphaMult(enabled ? 1.00f : 0.50f), pivot: new(0.00f, 0.50f), offset: new(4, 0));
														}

														using (GUI.Group.New(size: new(GUI.RmX, 32)))
														{
															var amount = 1;

															using (GUI.Group.New(size: new(32, 32)))
															{
																GUI.TitleCentered($"{rpc_new_company.stocks_count} x", pivot: new(0.00f, 0.50f), offset: new(8, 0));
															}

															GUI.SameLine();

															GUI.DrawMoney(rpc_new_company.share_capital / rpc_new_company.stocks_count, new Vector2(100, 32));

															if (GUI.IsItemHovered())
															{
																var scroll = GUI.GetMouse().GetScroll();
																if (scroll != 0.00f)
																{
																	rpc_new_company.stocks_count = (int)Maths.Max(rpc_new_company.stocks_count - (scroll * amount), 1);

																	//edit_transfer_money_amount -= (scroll * 100);
																	//edit_transfer_money_amount.SnapCeil(100);

																	Sound.PlayGUI(GUI.sound_select, volume: 0.07f);

																	//App.WriteLine(scroll);
																}
															}

															GUI.SameLine();

															GUI.TitleCentered("Number of stocks"u8, color: GUI.font_color_title, pivot: new(1.00f, 0.50f), offset: new(-8, 0));


															//GUI.TitleCentered(identifier, color: GUI.font_color_title.WithAlphaMult(enabled ? 1.00f : 0.50f), pivot: new(0.00f, 0.50f), offset: new(4, 0));
														}
													}
												}
											}

										}
										else if (selected_tab == 2)
										{
											using (var group = GUI.Group.New(size: new(GUI.RmX, 100), padding: new(0)))
											{
												using (GUI.Group.New2(size: new(group.size.X * 0.50f, group.size.Y), padding: new(4, 4, 12, 4)))
												{
													GUI.Title("Operative"u8, size: 22);

													GUI.NewLine(2);

													GUI.SeparatorThick();

													GUI.NewLine(4);

													if (org_valid)
													{
														foreach (var flag in rpc_new_company.permits)
														{
															GUI.TextShaded(flag.GetEnumName());
														}
													}
												}

												GUI.SameLine();

												using (GUI.Group.New2(size: new(group.size.X * 0.50f, group.size.Y), padding: new(12, 4, 4, 4)))
												{
													GUI.Title("Trading"u8, size: 22);

													GUI.NewLine(2);

													GUI.SeparatorThick();

													GUI.NewLine(4);

													//foreach (var flag in rpc_new_company.permits)
													//{
													//	GUI.TextShaded(flag.GetEnumName());
													//}
													////GUI.LabelShaded("Income tax:", organization.filter_flags.include);
													////GUI.LabelShaded("Dividend tax:", organization.tax_dividends);
													////GUI.LabelShaded("Property tax:", organization.tax_property);
													////GUI.LabelShaded("Payroll tax:", organization.tax_wage);
												}
											}

											using (var group = GUI.Group.New(size: new(GUI.RmX, 0), padding: new(0)))
											{

											}
										}
									}
								}
							}

							GUI.SeparatorThick();

							using (var group_bottom = GUI.Group.New(size: GUI.Rm, padding: new(0)))
							{
								using (group_bottom.Split(size: new(100, 40), align_x: GUI.AlignX.Right, align_y: GUI.AlignY.Center))
								{
									var requirements = Span<Crafting.Requirement>.Empty;
									if (GUI.DrawRequirementButton(context: ref context,
									enabled: rpc_new_company.Validate(),
									requirements: requirements,
									text: "Submit"u8,
									size: new Vector2(100, 40),
									color: GUI.col_button_yellow))
									{
										rpc_new_company.Send(this.ent_office);
									}
								}
							}
						}
					}
				}
			}
		}

		public partial struct InviteCompanyGUI: IGUICommand
		{
			public Entity ent_office;
			public Office.Data office;
			public Company.Data company;

			public static ICompany.Handle h_company_selected;

			public static int selected_tab;

			public void Draw()
			{
				if (this.office.flags.HasAny(Data.Flags.Enable_Invite_Company))
				{
					using (var window = GUI.Window.Interaction("Invite Company"u8, this.ent_office))
					{
						this.StoreCurrentWindowTypeID(order: -70);
						if (window.show)
						{
							ref var player = ref Client.GetPlayerData(out var player_asset);
							ref var character = ref Client.GetCharacter(out var character_asset);
							ref var region = ref this.ent_office.GetRegionCommon();

							var h_player = player_asset.GetHandle();

							using (var scrollable = GUI.Scrollbox.New("scroll.companies"u8, size: new(GUI.RmX - 300, GUI.RmY), padding: new(4)))
							{
								GUI.DrawBackground(GUI.tex_panel, scrollable.group_frame.GetInnerRect(), padding: new(8));

								foreach (var s_company in ICompany.Database.GetAssetsSpan())
								{
									ref var company_data = ref s_company.GetData(out var h_company);
									//if (company_data.players?.TryGetValue(h_player, out var player_info) ?? false)

									if (company_data.IsNotNull())
									{
										if (!this.office.filter_industry.Evaluate(company_data.industry_primary | company_data.industry_secondary)) continue;
										if (!this.office.filter_permits.Evaluate(company_data.permits)) continue;
										if (!this.office.filter_flags.Evaluate(company_data.flags)) continue;

										using (GUI.ID<Company.Data>.Push(h_company))
										using (var group_row = GUI.Group.New(size: new(GUI.RmX, 48)))
										{
											group_row.DrawBackground(GUI.tex_panel);

											Market.MarketGUI.DrawCompany(h_company);

											var selected = h_company == h_company_selected;
											if (GUI.Selectable3(h_company, group_row.GetOuterRect(), selected: selected))
											{
												if (selected) h_company_selected = default;
												else h_company_selected = h_company;
											}

											GUI.FocusableAsset(h_company);
										}

										//GUI.NewLine();
									}
								}
							}

							GUI.SameLine();

							using (var group_right = GUI.Group.New(GUI.Rm, new(4)))
							{
								GUI.DrawBackground(GUI.tex_panel, group_right.GetOuterRect(), padding: new(8));

								ref var selected_company_data = ref h_company_selected.GetData();

								using (GUI.Group.New(size: GUI.GetRemainingSpace(y: -42), padding: new(0)))
								{
									using (var group_top = GUI.Group.New(size: new(GUI.RmX, GUI.RmY), padding: new(4)))
									{
										using (var group_header = GUI.Group.New(size: new(GUI.RmX, 48), padding: new(4)))
										{
											GUI.Title((selected_company_data.IsNotNull() ? selected_company_data.name : "<no company selected>"u8), size: 24);
											//if (map_info.IsNotNull())
											//{
											//	//ref var prefecture_data = ref map_info.h_prefecture.GetData();
											//	//if (prefecture_data.IsNotNull())
											//	//{
											//	//	GUI.Title(prefecture_data.name, size: 16);

											//	//	ref var governorate_data = ref prefecture_data.h_governorate.GetData();
											//	//	if (governorate_data.IsNotNull())
											//	//	{
											//	//		GUI.SameLine();
											//	//		GUI.Title($", prefecture of {governorate_data.name}", size: 16);
											//	//	}
											//	//}
											//}
										}

										GUI.SeparatorThick();

										//using (var group = GUI.Group.New(size: GUI.Rm, padding: new(0)))
										//{
										//	using (var group_info = GUI.Group.New(size: new(GUI.RmX, 0), padding: new(8)))
										//	{

										//	}

										//	GUI.NewLine(4);

										//	using (var group_input = GUI.Group.New(size: GUI.Rm))
										//	{

										//	}
										//}
									}

									using (var group_b = GUI.Group.New(size: GUI.Rm, padding: new(4)))
									{
										using (var group = GUI.Group.New(size: new(GUI.RmX, 100), padding: new(0)))
										{
											Crafting.Context.NewFromCharacter(ref region, character_asset, ent_producer: this.ent_office, out var context);

											var requirements = Span<Crafting.Requirement>.Empty;
											if (GUI.DrawRequirementButton(context: ref context,
											requirements: requirements,
											text: "Invite"u8,
											size: new Vector2(100, 40),
											color: GUI.col_button_ok,
											enabled: character_asset is not null && h_company_selected.IsValid()))
											{
												var rpc = new DEV_InviteCompanyRPC()
												{
													h_company = h_company_selected
												};
												rpc.Send(this.ent_office);
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

		public partial struct CompanyGUI: IGUICommand
		{
			public Entity ent_office;
			public Office.Data office;
			public Company.Data company;

			public static ICompany.Handle h_selected_company;
			public static IOffer.Handle h_selected_offer;
			public static Office.DEV_CreateOfferRPC rpc_new_offer;
			public static Office.DEV_EditCompanyRPC rpc_edit_company;
			public static int selected_tab;

			public static IOffer.Data edit_offer_data;
			public static Texture.Handle tex_icons_trading = "ui_icons_trading";

			public void Draw()
			{
				using (var window = GUI.Window.Interaction("Office###company.gui"u8, this.ent_office))
				{
					this.StoreCurrentWindowTypeID(order: -70);
					if (window.show)
					{
						//ref var player = ref Client.GetPlayer();

						var h_player = Client.GetPlayerHandle();

						ref var character = ref Client.GetCharacter(out var character_asset);
						ref var region = ref this.ent_office.GetRegionCommon();
						//ref var map_info = ref region.GetMapInfo();
						ref var company_data = ref this.company.h_company.GetData(out var s_company);

						var subwindow_dirty = false;
						Crafting.Context.NewFromCharacter(ref region, character_asset, ent_producer: this.ent_office, out var context);

						var player_info = default(ICompany.PlayerInfo);
						if (company_data.IsNotNull())
						{
							company_data.players?.TryGetValue(h_player, out player_info);
						}

						var h_faction_entity = this.ent_office.GetFactionHandle();
						var h_faction_client = Client.GetFactionHandle();
						var can_manage = character_asset is not null && h_faction_entity.IsOwnedOrNeutral(h_faction_client);

						using (GUI.Group.New(size: GUI.Rm, padding: new(0)))
						{
							if (company_data.IsNotNull())
							{
								using (GUI.Group.New(new(GUI.RmX, 40), new(0, 0)))
								{
									using (var group_header = GUI.Group.New(new(GUI.RmX, 40), new(8, 0)))
									{
										if (player_info.perms_company.HasAny(ICompany.Permissions.View_Offers))
										{
											GUI.DrawTab3("Catalogue"u8, new Vector2(120, GUI.RmY), 0, ref selected_tab, inner: true);
											GUI.SameLine();
										}

										if (player_info.perms_company.HasAny(ICompany.Permissions.View_Stores))
										{
											GUI.DrawTab3("Stores"u8, new Vector2(120, GUI.RmY), 1, ref selected_tab, inner: true);
											GUI.SameLine();
										}

										if (player_info.perms_company.HasAny(ICompany.Permissions.View_Info))
										{
											GUI.DrawTab3("Info"u8, new Vector2(120, GUI.RmY), 2, ref selected_tab, inner: true);
											GUI.SameLine();
										}

										GUI.TitleCentered(company_data.name, size: 24, pivot: new(0.50f, 0.50f));
										GUI.FocusableAsset(this.company.h_company);
									}
								}

								GUI.SeparatorThick(margin: new(4, 4));

								using (var group_main = GUI.Group.New(size: new Vector2(GUI.RmX, GUI.RmY), padding: new(4)))
								{
									group_main.DrawBackground(GUI.tex_panel);

									using (GUI.Group.New(size: GUI.Rm))
									{
										switch (selected_tab) // && this.headquarters.flags.HasAny(Headquarters.Flags.Show_Members))
										{
											case 0:
											{
												if (player_info.perms_company.HasAny(ICompany.Permissions.View_Offers))
												{
													//using (var scrollable = GUI.Scrollbox.New("scroll.offers", size: GUI.GetRemainingSpace(x: -288), padding: new(4)))
													using (var scrollable = GUI.Scrollbox.New("scroll.offers"u8, size: GUI.GetRemainingSpace(), padding: new(4)))
													{
														GUI.DrawBackground(GUI.tex_panel, scrollable.group_frame.GetInnerRect(), padding: new(8));

														foreach (var s_offer in IOffer.Database.GetAssetsSpan())
														{
															ref var offer_data = ref s_offer.GetData();
															if (offer_data.h_company == this.company.h_company)
															{
																var h_offer = s_offer.GetHandle();
																using (GUI.ID.Push(h_offer))
																{
																	using (var group_row = GUI.Group.New(size: new(GUI.RmX, 48)))
																	{
																		if (group_row.IsVisible())
																		{
																			var selected = h_selected_offer == h_offer;

																			GUI.DrawBackground(in group_row, GUI.tex_panel);

																			if (selected)
																			{
																				DrawOffer(h_offer.id, ref context, ref edit_offer_data, is_editor: true, h_company_self: this.company.h_company);
																			}
																			else
																			{
																				DrawOffer(h_offer.id, ref context, ref offer_data, is_readonly: true, h_company_self: this.company.h_company);
																			}

																			if (GUI.Selectable3(h_offer.id, group_row.GetOuterRect(), selected: selected))
																			{
																				if (selected)
																				{
																					h_selected_offer = default;
																					edit_offer_data = new();
																				}
																				else
																				{
																					h_selected_offer = h_offer;
																					edit_offer_data = offer_data;
																				}

																				subwindow_dirty = true;
																			}

																			GUI.FocusableAsset(h_offer);
																		}

																		GUI.NewLine(4);
																	}
																}
															}
														}

														using (GUI.ID.Push(-100))
														{
															using (var group_row = GUI.Group.New(size: new(GUI.RmX, 48)))
															{
																if (group_row.IsVisible())
																{
																	GUI.DrawBackground(in group_row, GUI.tex_panel);

																	using (GUI.Group.Centered(outer_size: new(80, GUI.RmY), inner_size: new(80, 40)))
																	{
																		if (GUI.DrawButton("New"u8, size: GUI.Rm, color: GUI.col_button_ok))
																		{
																			rpc_new_offer.h_company = s_company;
																			rpc_new_offer.Send(this.ent_office);
																		}
																	}
																	GUI.DrawHoverTooltip("Create a new trade offer."u8);
																}
															}
														}
													}

													//GUI.SameLine();

													//using (var group_right = GUI.Group.New(size: GUI.Rm, padding: new(4)))
													{
														//GUI.DrawBackground(GUI.tex_frame, group_right.GetOuterRect(), padding: new(8));

														ref var offer_data_original = ref h_selected_offer.GetData();
														if (offer_data_original.IsNotNull())
														{
															if (offer_data_original.version != edit_offer_data.version)
															{
																edit_offer_data = offer_data_original;
																subwindow_dirty = true;
															}
														}

														if (subwindow_dirty)
														{
															window.SetChildWindowOpen("office.trades.sub"u8, h_selected_offer);
														}

														ref var offer_data = ref edit_offer_data;
														if (offer_data.IsNotNull() && h_selected_offer)
														{
															var item_context = GUI.ItemContext.Begin(false);

															var items_a_span = offer_data.items_a.AsSpan();
															var items_b_span = offer_data.items_b.AsSpan();

															using (var window_child = window.BeginChildWindow("office.trades.sub"u8, GUI.AlignX.Right, GUI.AlignY.Center, size: new(320, window.group.size.Y - 32), padding: new(8), flags: GUI.Window.Flags.No_Click_Focus | GUI.Window.Flags.No_Appear_Focus))
															{
																if (window_child.show)
																{
																	var dirty = false;

																	using (var group_details = GUI.Group.New(size: GUI.GetRemainingSpace(y: -48 - 2), padding: new(4)))
																	{
																		var slot_size = new Vector2(48, 48);
																		using (var group_bab = GUI.Group.New(size: new(GUI.RmX, slot_size.Y * 4)))
																		{
																			//using (var group_row = GUI.Group.New(size: new(GUI.RmX, 64)))
																			using (var group_col = GUI.Group.New(size: new(slot_size.X, GUI.RmY)))
																			{
																				for (var i = 0; i < items_a_span.Length; i++)
																				{
																					using (GUI.ID.Push(i + 10000))
																					{
																						using (var group_slot = GUI.Group.New(size: slot_size))
																						{
																							ref var item = ref items_a_span[i];
																							dirty |= DrawOfferItem(ref item_context, id: (uint)(i + 1), ref item, frame_color: GUI.col_buy.WithAlphaMult(item.type == 0 ? 0.40f : 1.00f));
																						}
																					}
																				}
																			}

																			GUI.SameLine();

																			using (var group_col = GUI.Group.New(size: new(24, GUI.RmY)))
																			{

																			}

																			GUI.SameLine();

																			using (var group_col = GUI.Group.New(size: new(slot_size.X, GUI.RmY)))
																			{
																				for (var i = 0; i < items_b_span.Length; i++)
																				{
																					using (GUI.ID.Push(i + 10000))
																					{
																						using (var group_slot = GUI.Group.New(size: slot_size))
																						{
																							ref var item = ref items_b_span[i];
																							dirty |= DrawOfferItem(ref item_context, id: (uint)i, ref item, frame_color: GUI.col_sell.WithAlphaMult(item.type == 0 ? 0.40f : 1.00f));
																						}
																					}
																				}
																			}

																			GUI.SameLine();

																			using (var group_col = GUI.Group.New(size: GUI.Rm, padding: new(4, 0)))
																			{
																				group_col.DrawBackground(GUI.tex_panel, inner: true);

																				var h_counter = 24;
																				var w_label = 40;

																				using (var group_row = GUI.Group.New(size: new(GUI.RmX, h_counter)))
																				{
																					GUI.TitleCentered("Trade Editor"u8, pivot: new(0.00f, 0.50f), offset: new(4, 0), size: 24);
																				}

																				using (var group_row = GUI.Group.New(size: new(GUI.RmX, h_counter)))
																				{
																					using (GUI.ID.Push("offer.flags"u8))
																					{
																						if (GUI.Checkbox("Buy"u8, ref edit_offer_data.flags, IOffer.Flags.Buy, size: new(56, GUI.RmY), show_tooltip: true))
																						{

																						}

																						GUI.SameLine();

																						if (GUI.Checkbox("Sell"u8, ref edit_offer_data.flags, IOffer.Flags.Sell, size: GUI.Rm, show_tooltip: true))
																						{

																						}
																					}
																				}

																				using (var group_row = GUI.Group.New(size: new(GUI.RmX, h_counter)))
																				{
																					using (GUI.ID.Push("offer.max"u8))
																					{
																						if (GUI.DrawCounter("counter.max"u8, new Vector2(GUI.RmX - w_label, h_counter), ref edit_offer_data.max, step: edit_offer_data.step, min: edit_offer_data.step, max: 99999))
																						{
																							edit_offer_data.min = Math.Min(edit_offer_data.min, edit_offer_data.max);
																							//Maths.MinMax(edit_offer_data.min, edit_offer_data.max, out edit_offer_data.min, out edit_offer_data.max);
																						}

																						GUI.SameLine();

																						GUI.TitleCentered("Max"u8, pivot: new(0.00f, 0.50f), offset: new(4, 0), size: 16);
																					}
																				}

																				using (var group_row = GUI.Group.New(size: new(GUI.RmX, h_counter)))
																				{
																					using (GUI.ID.Push("offer.min"u8))
																					{
																						if (GUI.DrawCounter("counter.min"u8, new Vector2(GUI.RmX - w_label, h_counter), ref edit_offer_data.min, step: 1, min: 1, max: 99999))
																						{
																							edit_offer_data.max = Math.Max(edit_offer_data.min, edit_offer_data.max);
																							//Maths.MinMax(edit_offer_data.min, edit_offer_data.max, out edit_offer_data.min, out edit_offer_data.max);
																						}

																						GUI.SameLine();

																						GUI.TitleCentered("Min"u8, pivot: new(0.00f, 0.50f), offset: new(4, 0), size: 16);
																					}
																				}

																				using (var group_row = GUI.Group.New(size: new(GUI.RmX, h_counter)))
																				{
																					using (GUI.ID.Push("offer.step"u8))
																					{
																						if (GUI.DrawCounter("counter.step"u8, new Vector2(GUI.RmX - w_label, h_counter), ref edit_offer_data.step, step: 1, min: 1, max: edit_offer_data.max))
																						{

																						}

																						GUI.SameLine();

																						GUI.TitleCentered("Step"u8, pivot: new(0.00f, 0.50f), offset: new(4, 0), size: 16);
																					}
																				}

																				GUI.SeparatorThick();

																				using (var group_row = GUI.Group.New(size: new(GUI.RmX, h_counter)))
																				{
																					using (GUI.ID.Push("offer.fee"u8))
																					{
																						var fee_percent = (int)MathF.Round(edit_offer_data.fee * 100.00f, 2);
																						if (GUI.DrawCounter("fee"u8, size: new(GUI.RmX, 24), value: ref fee_percent, step: 1, min: 0, max: 100, format: Maths.NumberFormat.Percent))
																						{
																							edit_offer_data.fee = (float)MathF.Round(fee_percent / 100.00f, 2);
																						}
																						GUI.DrawHoverTooltip("Fee"u8);

																						//if (GUI.DrawCounter("counter.fee"u8, new Vector2(GUI.RmX - w_label, h_counter), ref edit_offer_data.fee, step: 0.01f, min: 0.00f, max: 10.00f, format: GUI.NumberFormat.Real2))
																						//{

																						//}

																						GUI.SameLine();

																						GUI.TitleCentered("Fee"u8, pivot: new(0.00f, 0.50f), offset: new(4, 0), size: 16);
																					}
																				}
																				GUI.DrawHoverTooltip(""u8);
																			}
																		}

																		using (var group_row = GUI.Group.New(size: new(GUI.RmX, 40)))
																		{
																			if (GUI.DrawButton("Swap"u8, size: new(120, GUI.RmY), color: GUI.col_button))
																			{
																				(edit_offer_data.items_a, edit_offer_data.items_b) = (edit_offer_data.items_b, edit_offer_data.items_a);
																				dirty = true;
																			}
																			GUI.DrawHoverTooltip("Swap left and right item columns."u8);

																			//GUI.SameLine();

																			//if (GUI.EnumInput("offer.flags", ref offer_data_original.flags, GUI.Rm, show_label: false))
																			//{

																			//}
																		}

																		using (var group_b = GUI.Group.New(size: GUI.Rm, padding: new(4)))
																		{
																			group_b.DrawBackground(GUI.tex_panel, inner: false);

																			var w_counter = 112;
																			var h_counter = 24;
																			//var w_label = 102;

																			using (var group_row = GUI.Group.New(size: new(GUI.RmX, h_counter)))
																			{
																				using (GUI.ID.Push("offer.stock_max"u8))
																				{
																					if (GUI.DrawCounter("counter.stock_max"u8, new Vector2(w_counter, h_counter), ref edit_offer_data.stock_max, step: 1, min: 0, max: 2500))
																					{
																						edit_offer_data.stock_min = Math.Min(edit_offer_data.stock_min, edit_offer_data.stock_max);
																						//Maths.MinMax(edit_offer_data.min, edit_offer_data.max, out edit_offer_data.min, out edit_offer_data.max);
																					}

																					GUI.SameLine();

																					GUI.TitleCentered("Stock (Max)"u8, pivot: new(0.00f, 0.50f), offset: new(4, 0), size: 16);
																				}
																			}

																			using (var group_row = GUI.Group.New(size: new(GUI.RmX, h_counter)))
																			{
																				using (GUI.ID.Push("offer.stock_min"u8))
																				{
																					if (GUI.DrawCounter("counter.stock_min"u8, new Vector2(w_counter, h_counter), ref edit_offer_data.stock_min, step: 1, min: 0, max: 2500))
																					{
																						edit_offer_data.stock_max = Math.Max(edit_offer_data.stock_min, edit_offer_data.stock_max);
																						//Maths.MinMax(edit_offer_data.min, edit_offer_data.max, out edit_offer_data.min, out edit_offer_data.max);
																					}

																					GUI.SameLine();

																					GUI.TitleCentered("Stock (Min)"u8, pivot: new(0.00f, 0.50f), offset: new(4, 0), size: 16);
																				}
																			}

																			using (var group_row = GUI.Group.New(size: new(GUI.RmX, h_counter)))
																			{
																				using (GUI.ID.Push("offer.stock_default"u8))
																				{
																					if (GUI.DrawCounter("counter.stock_default"u8, new Vector2(w_counter, h_counter), ref edit_offer_data.stock_default, step: 1, min: edit_offer_data.stock_min, max: edit_offer_data.stock_max))
																					{

																					}

																					GUI.SameLine();

																					GUI.TitleCentered("Stock (Base)"u8, pivot: new(0.00f, 0.50f), offset: new(4, 0), size: 16);
																				}
																			}
																		}

																		if (dirty)
																		{
																			App.WriteLine(dirty);
																		}
																	}

																	GUI.SeparatorThick();

																	using (var group_bottom = GUI.Group.New(size: new(GUI.RmX, 40)))
																	{
																		var changed = !offer_data.MemCmp(ref offer_data_original);
																		var is_valid = offer_data.Validate();

																		if (GUI.DrawButton("Apply"u8, size: new Vector2(100, group_bottom.size.Y), color: GUI.col_button_ok, enabled: changed & is_valid))
																		{
																			var rpc = new DEV_EditOfferRPC()
																			{
																				h_offer = h_selected_offer,

																				min = edit_offer_data.min,
																				max = edit_offer_data.max,
																				step = edit_offer_data.step,

																				stock_min = edit_offer_data.stock_min,
																				stock_max = edit_offer_data.stock_max,
																				stock_default = edit_offer_data.stock_default,

																				flags = edit_offer_data.flags,
																				fee = edit_offer_data.fee,

																				items_a = edit_offer_data.items_a,
																				items_b = edit_offer_data.items_b,
																			};
																			rpc.Send(this.ent_office);
																		}
																		//GUI.TitleCentered(s_offer.Identifier, pivot: new(0.00f, 0.50f), offset: new(8, 0));

																		GUI.SameLine();

																		var flags_tmp = edit_offer_data.flags;
																		//if (GUI.DrawButton("Publish", size: new Vector2(100, group_bottom.size.Y), color: GUI.col_button_ok, enabled: !changed))
																		if (GUI.Checkbox("Publish"u8, ref flags_tmp, IOffer.Flags.Published, size: new Vector2(32, 32), enabled: !changed & is_valid, show_tooltip: true))
																		{
																			var rpc = new DEV_OfferActionRPC()
																			{
																				h_offer = h_selected_offer,
																				action = DEV_OfferActionRPC.ActionType.Publish
																			};
																			rpc.Send(this.ent_office);
																		}
																	}
																}
															}
														}
													}
												}
											}
											break;

											case 1:
											{
												if (player_info.perms_company.HasAny(ICompany.Permissions.View_Stores))
												{
													using (var scrollable = GUI.Scrollbox.New("scroll.stores"u8, size: GUI.Rm, padding: new(4)))
													{
														GUI.DrawBackground(GUI.tex_panel, scrollable.group_frame.GetInnerRect(), padding: new(8));

														//WorldMap.Draw(GUI.Rm);

														//var ts = Timestamp.Now();
														foreach (var store_asset in IStore.Database.GetAssetsSpan())
														{
															ref var store_data = ref store_asset.GetData();
															if (store_data.h_company == this.company.h_company)
															{
																var h_store = store_asset.GetHandle();
																using (GUI.ID.Push(h_store))
																{
																	using (var group_row = GUI.Group.New(size: new(GUI.RmX, 96)))
																	{
																		group_row.DrawBackground(GUI.tex_panel, inner: true);

																		using (var group_title = GUI.Group.New(new(GUI.RmX, 32)))
																		{
																			GUI.TitleCentered(store_data.name, size: 24, pivot: new(0.00f, 0.50f), offset: new(8, 0));
																		}

																		var is_selected = false;
																		if (GUI.Selectable3(h_store, group_row.GetInnerRect(), is_selected))
																		{

																		}

																		GUI.FocusableAsset(h_store);
																	}
																}

																GUI.NewLine();
															}
														}
														//var ts_elapsed = ts.GetMilliseconds();
														//GUI.Text($"{ts_elapsed:0.0000} ms");

														GUI.SameLine();
													}
												}
											}
											break;

											case 2:
											{
												if (player_info.perms_company.HasAny(ICompany.Permissions.View_Info))
												{
													rpc_edit_company.h_company = this.company.h_company;
													var dirty = false;

													using (var scrollable = GUI.Scrollbox.New("scroll.management"u8, size: GUI.Rm, padding: new(4)))
													{
														GUI.DrawBackground(GUI.tex_panel, scrollable.group_frame.GetInnerRect(), padding: new(8));

														ref var organization = ref company_data.h_organization.GetData();
														if (organization.IsNotNull())
														{
															//if (organization.liability.GetCount() <= 1 && organization.liability.TryGetFirst(out rpc_new_company.liability))
															//{
															//	//rpc_new_company.liability = organization.liability.
															//}

															//rpc_new_company.permits |= organization.filter_permits.require;
															//rpc_new_company.flags |= organization.filter_flags.require;
															//rpc_new_company.industry |= organization.industry.require;
															//rpc_new_company.services |= organization.services.require;

															//rpc_new_company.permits &= organization.filter_permits.require | organization.filter_permits.include;
															//rpc_new_company.flags &= organization.filter_flags.require | organization.filter_flags.include;
															//rpc_new_company.industry &= organization.industry.require | organization.industry.include;
															//rpc_new_company.services &= organization.services.require | organization.services.include;
														}

														using (GUI.Group.New(size: GUI.GetRemainingSpace(y: -42), padding: new(0)))
														{
															using (var group_left = GUI.Group.New(size: new(GUI.RmX * 0.45f, GUI.RmY), padding: new(4)))
															{
																//using (var group = GUI.Group.New(size: new(GUI.RmX, 48), padding: new(0)))
																//{
																//	//GUI.TitleCentered(company_data.name, size: 32, pivot: new(0.00f, 0.00f), offset: new(4, 0));
																//	//GUI.Title(company_data.name, size: 32);

																//	//GUI.TextInput("company.desc_short", "<description>", company_data.desc_short, ref rpc_edit_company.desc_short, size: new(GUI.RmX, 32));


																//	//if (map_info.IsNotNull())
																//	//{
																//	//	ref var prefecture_data = ref map_info.h_prefecture.GetData();
																//	//	if (prefecture_data.IsNotNull())
																//	//	{
																//	//		GUI.Title(prefecture_data.name, size: 16);

																//	//		ref var governorate_data = ref prefecture_data.h_governorate.GetData();
																//	//		if (governorate_data.IsNotNull())
																//	//		{
																//	//			GUI.SameLine();
																//	//			GUI.Title($", prefecture of {governorate_data.name}", size: 16);
																//	//		}
																//	//	}
																//	//}
																//}

																//GUI.SeparatorThick();

																using (var group_row = GUI.Group.New(size: new(GUI.RmX, 0), padding: new(0)))
																{
																	using (GUI.Group.New2(size: new(group_row.size.X, group_row.size.Y), padding: new(4, 4, 12, 4)))
																	{
																		using (GUI.Group.New(size: new(GUI.RmX, 22)))
																		{
																			GUI.Title("Company Info"u8, size: 22);
																		}

																		GUI.NewLine(2);

																		GUI.SeparatorThick();

																		GUI.NewLine(4);

																		using (GUI.Group.New(size: new(GUI.RmX, 0), padding: new(4)))
																		{
																			//GUI.LabelShaded("Tag:", company_data.tag, font: GUI.Font.Superstar, size: 16);
																			GUI.LabelShaded("Type:"u8, organization.IsNotNull() ? organization.name : "<unknown>"u8, font_a: GUI.Font.Superstar, size_a: 16);
																			//GUI.LabelShaded("", company_data.flags, size: 14);
																			GUI.LabelShaded("Traits:"u8, company_data.flags, font_a: GUI.Font.Superstar, size_a: 16);

																			GUI.TextInput("company.desc_short"u8, "<description>"u8, company_data.desc_short, ref rpc_edit_company.desc_short, size: new(GUI.RmX, 32));
																			GUI.TextInput("company.motto"u8, "<motto>"u8, company_data.motto, ref rpc_edit_company.motto, size: new(GUI.RmX, 32));
																		}
																	}
																}

																using (var group_row = GUI.Group.New(size: GUI.Rm, padding: new(0)))
																{
																	using (GUI.Group.New2(size: new(group_row.size.X * 0.50f, group_row.size.Y), padding: new(4, 4, 12, 4)))
																	{
																		using (var group_logo = GUI.Group.New(size: new(GUI.RmX, GUI.RmX * 0.50f), padding: new(6)))
																		{
																			group_logo.DrawBackground(GUI.tex_frame_white, color: GUI.col_default.LumaBlend(company_data.color_brand, 0.75f));

																			if (company_data.IsNotNull())
																			{
																				var sprite_logo = company_data.GetLogo(ICompany.LogoSize.Sign_4x2);
																				GUI.DrawSprite(sprite_logo, group_logo.GetInnerRect(), GUI.Layer.Window);
																			}
																		}

																		//using (GUI.Group.New(size: new(GUI.RmX, 22)))
																		//{
																		//	GUI.Title("TODO", size: 22);
																		//}

																		//GUI.NewLine(2);

																		//GUI.SeparatorThick();

																		//GUI.NewLine(4);

																		////GUI.TextInput("company.desc_short", "<description>", company_data.desc_short, ref rpc_edit_company.desc_short, size: new(GUI.RmX, 32));

																	}

																	GUI.SameLine();

																	using (GUI.Group.New2(size: new(group_row.size.X * 0.50f, group_row.size.Y), padding: new(4, 4, 12, 4)))
																	{
																		using (GUI.Group.New(size: new(GUI.RmX, 22)))
																		{
																			GUI.Title("Colors"u8, size: 22);
																		}

																		GUI.NewLine(2);

																		GUI.SeparatorThick();

																		GUI.NewLine(4);

																		using (GUI.Group.New(size: new(GUI.RmX, 0), padding: new(4)))
																		{
																			using (GUI.Group.New(size: new(GUI.RmX, 24)))
																			{
																				dirty |= GUI.DrawColorPicker("Color (A)"u8, in company_data.color_a, ref rpc_edit_company.color_a);
																			}

																			using (GUI.Group.New(size: new(GUI.RmX, 24)))
																			{
																				dirty |= GUI.DrawColorPicker("Color (B)"u8, in company_data.color_b, ref rpc_edit_company.color_b);
																			}

																			using (GUI.Group.New(size: new(GUI.RmX, 24)))
																			{
																				dirty |= GUI.DrawColorPicker("Color (Icon)"u8, in company_data.color_brand, ref rpc_edit_company.color_icon);
																			}

																			using (GUI.Group.New(size: new(GUI.RmX, 24)))
																			{
																				dirty |= GUI.DrawColorPicker("Color (Glow)"u8, in company_data.color_glow, ref rpc_edit_company.color_glow);
																			}
																		}
																	}

																	//GUI.SameLine();

																	//using (GUI.Group.New2(size: new(group_row.size.X * 0.50f, group_row.size.Y), padding: new(4, 4, 12, 4)))
																	//{
																	//	using (GUI.Group.New(size: new(GUI.RmX, 22)))
																	//	{
																	//		GUI.Title("TODO", size: 22);
																	//	}

																	//	GUI.NewLine(2);

																	//	GUI.SeparatorThick();

																	//	GUI.NewLine(4);

																	//	//GUI.TextInput("company.desc_short", "<description>", company_data.desc_short, ref rpc_edit_company.desc_short, size: new(GUI.RmX, 32));

																	//}
																}
															}

															GUI.SameLine();

															using (var group_right = GUI.Group.New(size: GUI.Rm, padding: new(4)))
															{
																using (var group_row = GUI.Group.New(size: new(GUI.RmX, 0), padding: new(0)))
																{
																	using (GUI.Group.New2(size: new(group_row.size.X, group_row.size.Y), padding: new(4, 4, 12, 4)))
																	{
																		using (GUI.Group.New(size: new(GUI.RmX, 22)))
																		{
																			GUI.Title("Permits"u8, size: 22);
																		}

																		GUI.NewLine(2);

																		GUI.SeparatorThick();

																		GUI.NewLine(4);

																		using (GUI.Group.New(size: new(GUI.RmX, 0), padding: new(4)))
																		{
																			//GUI.LabelShaded("Type:", organization.IsNotNull() ? organization.name : "<unknown>", font: GUI.Font.Superstar, size: 16);

																			//GUI.TextInput("company.desc_short", "<description>", company_data.desc_short, ref rpc_edit_company.desc_short, size: new(GUI.RmX, 32));
																			//GUI.TextInput("company.motto", "<motto>", company_data.motto, ref rpc_edit_company.motto, size: new(GUI.RmX, 32));
																		}
																	}
																}

																//using (var group = GUI.Group.New(size: new(GUI.RmX, 48), padding: new(4)))
																//{
																//	dirty |= GUI.DrawColorPicker("Color (A)", in company_data.color_a, ref rpc_edit_company.color_a);
																//}

																//GUI.SeparatorThick();

																//using (var group = GUI.Group.New(size: new(GUI.RmX, 0), padding: new(8)))
																//{
																//	dirty |= GUI.DrawColorPicker("Color (B)", in company_data.color_b, ref rpc_edit_company.color_b);
																//}

																////GUI.SeparatorThick();

																//using (var group = GUI.Group.New(size: new(GUI.RmX, 100), padding: new(0)))
																//{
																//	dirty |= GUI.DrawColorPicker("Color (Glow)", in company_data.color_glow, ref rpc_edit_company.color_glow);
																//}

																//using (var group = GUI.Group.New(size: new(GUI.RmX, 100), padding: new(0)))
																//{

																//}
															}
														}

														GUI.SeparatorThick();

														using (var group_bottom = GUI.Group.New(size: GUI.Rm, padding: new(0)))
														{
															using (group_bottom.Split(size: new(100, 40), align_x: GUI.AlignX.Right, align_y: GUI.AlignY.Center))
															{
																var requirements = Span<Crafting.Requirement>.Empty;
																if (GUI.DrawRequirementButton(ref context, requirements, "Submit"u8, size: new Vector2(100, 40), color: GUI.col_button_yellow))
																{
																	rpc_edit_company.Send(this.ent_office);
																	//rpc_new_company.Send(this.ent_office);
																}
															}
														}
													}
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
			}

			public static bool DrawOfferItem(ref GUI.ItemContext item_context, uint id, ref Shipment.Item item, Color32BGRA frame_color)
			{
				var dirty = false;

				//var frame_color = GUI.col_button;

				//if (item.flags.HasAny(Shipment.Item.Flags.Buy)) frame_color = GUI.col_input;
				//if (item.flags.HasAny(Shipment.Item.Flags.Sell)) frame_color = GUI.col_output;

				using (var slot = GUI.ItemSlot.New(ref item_context, id: id, item, new(GUI.RmY), show_quantity: true, item_color: Color32BGRA.White, item_scale: 2.00f, frame_color: frame_color))
				{
					if (slot.result.TryGetValue(out var slot_result))
					{
						App.WriteLine($"{slot_result.type}; {slot_result.item_slot.type} {slot_result.item_slot.GetName()} {slot_result.item_slot.quantity:0.00}");

						if (slot_result.type == GUI.ItemSlot.InteractResult.Type.Deposit)
						{
							ref var item_tmp = ref slot_result.item_slot;
							switch (item_tmp.type)
							{
								case Shipment.Item.Type.Prefab:
								{
									item.type = item_tmp.type;
									item.prefab = item_tmp.prefab;
									item.quantity = item.max > Maths.epsilon ? Maths.Clamp(item_tmp.quantity, 0.00f, item.max) : item_tmp.quantity;
								}
								break;

								case Shipment.Item.Type.Resource:
								{
									item.type = item_tmp.type;
									item.material = item_tmp.material;
									item.quantity = item.max > Maths.epsilon ? Maths.Clamp(item_tmp.quantity, 0.00f, item.max) : item_tmp.quantity;
								}
								break;

								//case Shipment.Item.Type.Money:
								//{
								//	item.type = item_tmp.type;
								//	item.material = default;
								//	item.quantity = item.max > 0.00f ? Maths.Clamp(item_tmp.quantity, 0.00f, item.max) : item_tmp.quantity;
								//}
								//break;
							}

							dirty = true;

							Sound.PlayGUI(GUI.sound_pop, volume: 0.07f, pitch: 1.00f);
						}
						else if (slot_result.type == GUI.ItemSlot.InteractResult.Type.Withdraw)
						{
							item = slot_result.item_slot;
							dirty = true;

							Sound.PlayGUI(GUI.sound_pop, volume: 0.07f, pitch: 0.80f);
						}
					}
				}

				if (GUI.IsItemHovered())
				{
					if (item.type != Shipment.Item.Type.Undefined)
					{
						var quantity = (int)item.quantity;
						if (GUI.ScrollInput(GUI.GetLastItemRect(), ref quantity, step: 1, min: 1, max: 500000))
						{

						}
						item.quantity = quantity;
					}
				}

				//GUI.SameLine();

				//using (var group_row_right = GUI.Group.New(size: GUI.Rm))
				//{
				//	//if (GUI.ScalarInput("quantity", ref item.quantity, ))

				//	var quantity = (int)item.quantity;
				//	if (GUI.DrawCounter(size: new(96, 32), ref quantity, 1, 1, 500000, enabled: item.type != Shipment.Item.Type.Undefined))
				//	{

				//	}
				//	item.quantity = quantity;

				//	//if (GUI.Checkbox("Buy", ref is_buy, size: new Vector2(64, 32), show_text: false, show_tooltip: true))
				//	if (GUI.EnumInput("offer.flags", ref item.flags, size: new Vector2(96, 32), show_label: false, mask: Shipment.Item.Flags.Buy | Shipment.Item.Flags.Sell, exclusive: true))
				//	{

				//	}

				//	//GUI.TextCentered($"{item.type} {item.quantity:0.00}/{item.max:0.00}", pivot: new(1.00f, 0.50f));
				//}

				return dirty;
			}
		}


		[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global)]
		public static void OnGUI(Entity entity,
		[Source.Owned] in Office.Data office, [Source.Owned] in Company.Data company,
		[Source.Owned] in Interactable.Data interactable)
		{
			if (interactable.IsActive())
			{
				if (company.h_company.TryGetDefinition(out var company_asset))
				{
					var gui = new CompanyGUI()
					{
						ent_office = entity,
						company = company,

						office = office
					};
					gui.Submit();

					//if (company_asset != null && (company_asset.data.players?.TryGetValue(Client.GetPlayerHandle(), out var player_info) ?? false))
					//{
					//	if (!player_info.perms_company.IsEmpty())
					//	{
					//		var gui = new CompanyGUI()
					//		{
					//			ent_office = entity,
					//			company = company,

					//			office = office
					//		};
					//		gui.Submit();
					//	}
					//}
				}

				if (office.flags.HasAny(Office.Data.Flags.Enable_Register_Company))
				{
					var gui = new RegisterCompanyGUI()
					{
						ent_office = entity,
						company = company,

						office = office
					};
					gui.Submit();
				}

				if (company.h_company == 0 && office.flags.HasAny(Office.Data.Flags.Enable_Invite_Company))
				{
					var gui = new InviteCompanyGUI()
					{
						ent_office = entity,
						company = company,

						office = office
					};
					gui.Submit();
				}
			}
		}
#endif

	}
}
