namespace TC2.Base.Components
{
	public static partial class LinkedMapTeleporter
	{
		[Flags]
		public enum Flags : uint
		{
			None = 0,

			// Le véhicule dans lequel le joueur est assis voyage avec lui.
			// Sans ce flag (ou si le véhicule a d'autres passagers), le joueur
			// descend du véhicule et voyage seul, le véhicule reste sur place.
			Allow_Vehicles = 1u << 0,
		}

		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Global | IComponent.Scope.Region)]
		public partial struct Data : IComponent
		{
			[Save.Force, Editor.Picker.Position(relative: true)]
			public required Vec2f offset;

			[Save.Force]
			public Vec2f direction;

			[Save.Force]
			public float radius;

			[Save.Force]
			public float power;

			[Save.Force]
			public float mass_max;

			[Save.Force]
			public float volume_max;

			[Save.Force]
			public Flags flags;

			[Save.Force]
			public ISoundMix.Handle h_soundmix_teleport;

			// Décalage appliqué à l'arrivée du VÉHICULE (en plus de la position du personnage).
			// Y négatif = vers le haut : évite que le châssis apparaisse enfoncé dans le sol ou le tunnel.
			[Save.Force]
			public Vec2f vehicle_offset;

			// Voyage ENTRE RÉGIONS uniquement : délai MINIMAL (en secondes) avant la r-assise, compté
			// APRÈS la confirmation du chargement de la région par le client (BoardReadyRPC, voir plus bas).
			// Test 19:32 : rasseoir avant l'entrée du client place la relation Seat dans la fullsync
			// d'entrée ; l'EntityCreate du corps dépasse alors la limite Flecs côté client
			// (FLECS_ID_DESC_MAX) -> le corps n'est jamais créé côté client -> perte de contrôle.
			// La r-assise doit donc partir en DELTA, après le signal du client.
			// Sans effet pour un voyage dans la même région (rassis immédiatement).
			[Save.Force]
			public float board_delay;

			// Code ÉMETTEUR de ce tunnel (son adresse à 5 chiffres). Exemple : 11122
			[Save.Force]
			public uint emitter_code;

			// Code ÉMETTEUR du tunnel de destination. Exemple : 33322
			[Save.Force]
			public uint receiver_code;

			// ID de la région cible (0 = même région)
			[Save.Force]
			public byte target_region_id;

			public Data()
			{
				this.offset = Vec2f.Zero;
				this.direction = new Vec2f(0f, -1f);
				this.radius = 1f;
				this.power = 1f;
				this.mass_max = 100f;
				this.volume_max = 2f;
				this.flags = Flags.None;
				this.vehicle_offset = new Vec2f(0f, -2f);
				this.board_delay = 2.0f;
			}
		}

#if SERVER
		// EMBARQUEMENT EN ATTENTE (voyages INTER-régions avec véhicule).
		// Clé = id de l'entité du nouveau corps du joueur. Le client annonce via BoardReadyRPC
		// qu'il a fini de charger la région cible et que son corps a bien été créé CÔTÉ CLIENT ;
		// c'est ce signal — et lui seul — qui déclenche la r-assise (voir le ticker WaitClientReady).
		// Sans lui, la relation Seat serait sérialisée dans la fullsync d'entrée et ferait échouer
		// l'EntityCreate du corps côté client (limite Flecs FLECS_ID_DESC_MAX, test 19:32).
		public sealed class PendingBoard
		{
			public bool client_ready;
		}

		private static readonly System.Collections.Generic.Dictionary<ulong, PendingBoard> boards = new();
		private static readonly object boards_lock = new();

		private static void RegisterBoard(ulong id, PendingBoard board)
		{
			lock (boards_lock) { boards[id] = board; }
		}

		private static PendingBoard GetBoard(ulong id)
		{
			lock (boards_lock) { return boards.TryGetValue(id, out var board) ? board : null; }
		}

		private static void RemoveBoard(ulong id)
		{
			lock (boards_lock) { boards.Remove(id); }
		}

		// Appelé par BoardReadyRPC (sur le thread de la région cible) : marque le client comme prêt.
		private static bool MarkClientReady(ulong id)
		{
			lock (boards_lock)
			{
				if (boards.TryGetValue(id, out var board) && !board.client_ready)
				{
					board.client_ready = true;
					return true;
				}
			}
			return false;
		}

		// Index code émetteur -> entité (stable, indépendant des IDs qui changent au redémarrage).
		private static readonly System.Collections.Generic.Dictionary<uint, ulong> code_to_entity = new();
		private static readonly object code_lock = new();

		private static void RegisterEmitterCode(uint code, Entity entity)
		{
			if (code == 0 || !entity.IsAlive()) return;
			lock (code_lock) { code_to_entity[code] = entity.id; }
		}

		private static void UnregisterEmitterCode(uint code, Entity entity)
		{
			if (code == 0) return;
			lock (code_lock)
			{
				if (code_to_entity.TryGetValue(code, out var id) && id == entity.id)
					code_to_entity.Remove(code);
			}
		}

		private static Entity FindTeleporterByEmitterCode(byte region_id, uint code)
		{
			if (code == 0) return Entity.None;

			lock (code_lock)
			{
				if (code_to_entity.TryGetValue(code, out var id))
				{
					var ent = new Entity(id).WithRegionID(region_id);
					if (ent.IsAlive())
					{
						ref var tp = ref ent.GetComponent<LinkedMapTeleporter.Data>();
						if (tp.IsNotNull() && tp.emitter_code == code)
							return ent;
					}

					ent = new Entity(id).AsGlobalEntity();
					if (ent.IsAlive())
					{
						ref var tp = ref ent.GetComponent<LinkedMapTeleporter.Data>();
						if (tp.IsNotNull() && tp.emitter_code == code)
							return ent;
					}
				}
			}

			return Entity.None;
		}

		private static Entity FindTeleporterByEmitterCodeGlobal(uint code)
		{
			if (code == 0) return Entity.None;

			lock (code_lock)
			{
				if (code_to_entity.TryGetValue(code, out var id))
				{
					var ent = new Entity(id).AsGlobalEntity();
					if (ent.IsAlive())
					{
						ref var tp = ref ent.GetComponent<LinkedMapTeleporter.Data>();
						if (tp.IsNotNull() && tp.emitter_code == code)
							return ent;
					}
				}
			}

			return Entity.None;
		}

		// Enregistre / met à jour l'index des codes émetteurs quand un tunnel apparaît ou change.
		[ISystem.Add(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global)]
		[ISystem.Modified.Component<LinkedMapTeleporter.Data>(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global)]
		public static void OnTeleporterCodeChanged(Entity entity, [Source.Owned] in LinkedMapTeleporter.Data teleporter)
		{
			if (teleporter.emitter_code != 0)
				RegisterEmitterCode(teleporter.emitter_code, entity);
		}

		[ISystem.Remove(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global)]
		public static void OnTeleporterRemoved(Entity entity, [Source.Owned] in LinkedMapTeleporter.Data teleporter)
		{
			UnregisterEmitterCode(teleporter.emitter_code, entity);
		}
#endif

		public struct LinkTeleportRPC : Net.IRPC<LinkedMapTeleporter.Data>
		{
			public required ICharacter.Handle h_character;

#if SERVER
			// Récupère l'entité racine + tous ses descendants, quel que soit le type de relation.
			private static void CollectHierarchy(Entity root, System.Collections.Generic.List<Entity> result)
			{
				var visited = new System.Collections.Generic.HashSet<ulong>();
				var stack = new System.Collections.Generic.Stack<Entity>();
				stack.Push(root);

				var relation_types = System.Enum.GetValues<Relation.Type>();
				while (stack.Count > 0)
				{
					var current = stack.Pop();
					if (!current.IsAlive()) continue;
					if (!visited.Add(current.id)) continue;

					result.Add(current);

					foreach (var relation_type in relation_types)
					{
						var children = FixedArray.CreateSpan32<Entity>(out var buffer);
						current.GetChildren(ref children, relation_type);
						for (int i = 0; i < children.Length; i++)
						{
							if (children[i].IsAlive()) stack.Push(children[i]);
						}
					}
				}
			}

			// Remonte de l'entité qui porte Vehicle.Data (souvent la cabine) jusqu'au châssis principal.
			// Même chaîne que VehicleGUI : parent Child (slot d'attachement) -> corps propriétaire via Instance.
			private static Entity FindVehicleRoot(Entity ent_vehicle)
			{
				var ent_root = ent_vehicle;
				for (int i = 0; i < 16; i++)
				{
					var ent_slot = ent_root.GetParent(Relation.Type.Child);
					if (!ent_slot.IsAlive()) break;

					var ent_parent_body = ent_slot.GetComponentOwner<Body.Data>(Relation.Type.Instance);
					if (!ent_parent_body.IsAlive() || ent_parent_body.id == ent_root.id) break;

					ent_root = ent_parent_body;
				}

				return ent_root;
			}

			public void Invoke(Net.IRPC.Context rpc, ref LinkedMapTeleporter.Data data)
			{
				ref var character_data = ref this.h_character.GetData(out var character_asset);
				if (character_data.IsNull()) return;
				if (character_asset is null) return;
				if (data.receiver_code == 0)
				{
					App.WriteLine("[Teleporter] receiver_code non configuré (0)", App.Color.Red);
					return;
				}

				byte current_region_id = rpc.entity.GetRegionID();
				byte target_region_id = data.target_region_id == 0 ? current_region_id : data.target_region_id;

				// 1. Recherche du tunnel cible par code émetteur (stable après redémarrage serveur)
				Entity ent_target = FindTeleporterByEmitterCode(target_region_id, data.receiver_code);
				if (!ent_target.IsAlive())
				{
					// Secours : recherche globale si la région cible n'a pas encore chargé l'entité
					ent_target = FindTeleporterByEmitterCodeGlobal(data.receiver_code);
					if (ent_target.IsAlive())
					{
						target_region_id = ent_target.GetRegionID();
					}
					else
					{
						App.WriteLine($"[Teleporter] Cible introuvable : receiver_code {data.receiver_code} (région {target_region_id})", App.Color.Red);
						return;
					}
				}

				// 2. Calcul de la position de destination exacte
				Vec2f target_position = default;
				ref var target_region_check = ref Region.GetRegion(target_region_id);
				if (target_region_check.IsNull())
				{
					App.WriteLine($"[Teleporter] Region cible {target_region_id} introuvable", App.Color.Red);
					return;
				}

				var ent_in_region = ent_target.WithRegionID(target_region_id);
				if (ent_in_region.IsAlive())
				{
					ref var t = ref ent_in_region.GetComponent<Transform.Data>();
					if (t.IsNotNull()) target_position = t.position;
				}

				if (target_position == default)
				{
					target_position = rpc.record.GetPosition() + data.offset;
				}

				var dest_pos = target_position + (data.direction * 1.5f);
				var dest_pos_vehicle = dest_pos + data.vehicle_offset;

				// 3. Récupération du personnage et de son entité contrôlée
				ref var region = ref rpc.entity.GetRegion();
				ref var character = ref region.GetCharacter(this.h_character);
				if (character.IsNull()) return;

				var ent_controlled = character.ent_controlled;
				if (!ent_controlled.IsAlive()) return;

				var faction_local = character_data.faction;
				var h_char_local = this.h_character;
				var target_region_id_local = target_region_id;
				bool is_same_region = target_region_id == current_region_id;
				var board_delay_ticks = is_same_region ? 0 : (int)(Maths.Max(0.00f, data.board_delay) * App.tickrate_f32);

				ref var player = ref rpc.GetSenderPlayer(out var player_asset);
				var h_player = player_asset?.GetHandle() ?? default;

				// 3bis. VÉHICULE : détection (même chaîne que VehicleGUI)
				var ent_vehicle = ent_controlled.GetComponentOwner<Vehicle.Data>(Relation.Type.Seat);
				bool in_vehicle = ent_vehicle.IsAlive() && ent_vehicle.id != ent_controlled.id;
				var ent_vehicle_root = in_vehicle ? FindVehicleRoot(ent_vehicle) : Entity.None;

				bool bring_vehicle = false;
				var seats_old = new System.Collections.Generic.List<Entity>();
				var name_seat_local = null as string;
				bool was_driver = false;
				int seat_index_old = -1;

				if (in_vehicle)
				{
					var ent_seat = ent_controlled.GetComponentOwner<Seat.Data>(Relation.Type.Seat);

					var hierarchy_scan = new System.Collections.Generic.List<Entity>();
					CollectHierarchy(ent_vehicle_root, hierarchy_scan);
					App.WriteLine($"[Teleporter] vehicule: {ent_vehicle} (Vehicle.Data), racine: {ent_vehicle_root}, {hierarchy_scan.Count} entites");

					int passengers = 0;
					foreach (var ent in hierarchy_scan)
					{
						if (!ent.IsAlive()) continue;

						ref var seat = ref ent.GetComponent<Seat.Data>();
						if (seat.IsNull()) continue;

						seats_old.Add(ent);
						if (seat.flags.HasAny(Seat.Flags.Has_Passenger)) passengers++;
					}

					for (int i_seat = 0; i_seat < seats_old.Count; i_seat++)
					{
						if (ent_seat.IsAlive() && seats_old[i_seat].id == ent_seat.id)
						{
							seat_index_old = i_seat;
							break;
						}
					}

					ref var vehicle_old = ref ent_vehicle.GetComponent<Vehicle.Data>();
					if (vehicle_old.IsNotNull() && vehicle_old.ent_seat_driver.IsAlive() && vehicle_old.ent_seat_driver.id == ent_controlled.id)
					{
						was_driver = true;
					}

					if (ent_seat.IsAlive())
					{
						name_seat_local = ent_seat.GetName().ToString();
						App.WriteLine($"[Teleporter] cabine {ent_vehicle} : ent_seat={vehicle_old.ent_seat}, ent_seat_driver={vehicle_old.ent_seat_driver} ; joueur au siege {ent_seat} ({name_seat_local}){(was_driver ? " [pilote]" : "")}");
					}

					if (!data.flags.HasAny(Flags.Allow_Vehicles))
					{
						App.WriteLine($"[Teleporter] vehicule {ent_vehicle_root} non autorise par ce tunnel : il reste sur place", App.Color.Yellow);
					}
					else if (passengers > 1)
					{
						App.WriteLine($"[Teleporter] {passengers} passagers dans {ent_vehicle_root} : le vehicule reste sur place, le joueur voyage seul", App.Color.Yellow);
					}
					else if (!ent_seat.IsAlive())
					{
						App.WriteLine($"[Teleporter] siege du joueur introuvable dans {ent_vehicle_root} : le vehicule reste sur place", App.Color.Yellow);
					}
					else
					{
						bring_vehicle = true;
					}

					Vehicle.RemovePassenger(ent_vehicle: ent_seat, ent_passenger: ent_controlled);
					App.WriteLine($"[Teleporter] passager descendu de {ent_seat}");
				}

				var vehicle_include_list = new System.Collections.Generic.List<Entity>();
				var include_names = new System.Collections.Generic.HashSet<string>();
				if (bring_vehicle)
				{
					var ev_vehicle = new Entrance.ExportEvent();
					ev_vehicle.ent_root = ent_vehicle_root;
					ev_vehicle.Trigger(ent_vehicle_root);

					foreach (var ent_include in ev_vehicle.ents_include.AsSpan())
					{
						if (!ent_include.IsAlive()) continue;
						if (include_names.Add(ent_include.GetName().ToString())) vehicle_include_list.Add(ent_include);
					}

					var hierarchy_export = new System.Collections.Generic.List<Entity>();
					CollectHierarchy(ent_vehicle_root, hierarchy_export);
					var hierarchy_names = new System.Collections.Generic.HashSet<string>();
					var duplicate_names = new System.Collections.Generic.HashSet<string>();
					foreach (var ent in hierarchy_export)
					{
						if (!ent.IsAlive()) continue;
						if (!hierarchy_names.Add(ent.GetName().ToString())) duplicate_names.Add(ent.GetName().ToString());
					}
					foreach (var name_dup in duplicate_names)
					{
						App.WriteLine($"[Teleporter] avertissement : nom d'entite en doublon '{name_dup}' dans la hierarchie (l'export peut echouer ou donner un vehicule instable ; un vehicule neuf est preferable)", App.Color.Yellow);
					}
				}
				var vehicle_ents_include = vehicle_include_list.ToArray();

				App.WriteLine($"[Teleporter] Transfert immatériel {current_region_id} -> {target_region_id} pour {ent_controlled} (vehicule: {(bring_vehicle ? ent_vehicle_root.ToString() : "non")})", App.Color.Green);

				// 4. EXPORT DU PERSONNAGE
				var ev = new Entrance.ExportEvent();
				ev.ent_root = ent_controlled;
				ev.Trigger(ent_controlled);
				var ents_include = ev.ents_include;

				var ent_pickup_target = character.ent_pickup_target;
				if (ent_pickup_target.IsAlive())
				{
					ref var holdable = ref ent_pickup_target.GetComponent<Holdable.Data>();
					if (holdable.IsNotNull() && holdable.flags.HasNone(Holdable.Flags.Storable))
					{
						ent_pickup_target.RemoveRelation(character.ent_pickup, Relation.Type.Child);
					}
				}

				character.state_flags.AddFlag(Character.StateFlags.Despawning | Character.StateFlags.Keep_Alive);

				var resources_copy = default(FixedArray8<Resource.Data>);
				var inventory = character.GetInventory();
				if (inventory.IsValid())
				{
					var inventory_span = inventory.GetSpan();
					if (Assert.Check(inventory_span.TryCopyTo(resources_copy), level: Assert.Level.Warn))
					{
						inventory_span.Clear();
						inventory.MarkDirty();
					}
				}

				character.ent_controlled.SetAssociatedCharacter(ICharacter.Handle.None);

				character_asset.flags.RemoveFlag(Asset.Flags.Recycle);
				character_asset.Promote();

				if (!is_same_region)
				{
					Net.SetActiveRegion(ref rpc.connection, target_region_id);
				}

				var character_parts_old = new System.Collections.Generic.List<Entity>();
				CollectHierarchy(ent_controlled, character_parts_old);
				var character_parts_count_old = character_parts_old.Count;

				// 5. Exports puis respawn
				region.Schedule((ref Region.Data region) =>
				{
					var ents_include_tmp = ents_include;
					var character_prefab = ent_controlled.ExportPrefab(character_asset.identifier, despawn: true, ents_include: ents_include_tmp.AsSpan());
					App.WriteLine($"[Teleporter] exported to {character_prefab}");

					var vehicle_ents_include_tmp = vehicle_ents_include;
					var bring_vehicle_spawn = bring_vehicle;
					var vehicle_prefab = character_prefab;
					if (bring_vehicle_spawn)
					{
						try
						{
							vehicle_prefab = ent_vehicle_root.ExportPrefab($"{character_asset.identifier}.vehicle", despawn: true, ents_include: vehicle_ents_include_tmp);
							App.WriteLine($"[Teleporter] vehicule exporte to {vehicle_prefab}");
						}
						catch (System.Exception e)
						{
							bring_vehicle_spawn = false;
							App.WriteLine($"[Teleporter] export du vehicule echoue : {e.Message} — le joueur voyage seul", App.Color.Red);
						}
					}

					void SpawnWhenOldBodyGone(ref Region.Data reg, int attempts)
					{
						bool still_alive = ent_controlled.IsAlive() || (bring_vehicle_spawn && ent_vehicle_root.IsAlive());
						if (still_alive && attempts < 30)
						{
							reg.Schedule((ref Region.Data r) => SpawnWhenOldBodyGone(ref r, attempts + 1));
							return;
						}

						if (still_alive)
						{
							App.WriteLine("[Teleporter] ancien corps/vehicule toujours present apres 30 ticks, respawn force", App.Color.Yellow);
						}

						character_asset.data.prefab = character_prefab;
						character_asset.Sync();

						ref var target_region = ref Region.GetRegion(target_region_id_local);
						if (target_region.IsNull())
						{
							App.WriteLine($"[Teleporter] target region {target_region_id_local} introuvable, aucun respawn", App.Color.Red);
							return;
						}

						App.WriteLine($"[Teleporter] respawn dans la region {target_region_id_local} a {dest_pos} (apres {attempts} ticks)");

						Spawner.SpawnCharacter(
							ref target_region,
							h_character: h_char_local,
							position: dest_pos,
							flags: Spawn.SpawnEvent.Flags.Control | Spawn.SpawnEvent.Flags.Claim,
							h_faction: faction_local,
							h_player: h_player
						).ContinueWith((ent_new) =>
						{
							if (!ent_new.IsAlive())
							{
								App.WriteLine("[Teleporter] SpawnCharacter a renvoye une entite morte", App.Color.Red);
								return ent_new;
							}

							App.WriteLine($"[Teleporter] spawned {ent_new}");

							ref var region_new = ref ent_new.GetRegion();
							ref var character_new = ref region_new.GetCharacter(h_char_local, out var ent_character_new);
							if (Assert.IsNotNull(ref character_new, Assert.Level.Warn))
							{
								character_new.state_flags.RemoveFlag(Character.StateFlags.Despawning | Character.StateFlags.Keep_Alive);
								character_new.Sync(ent_character_new, true);
							}

							var resources_tmp = resources_copy;
							if (!resources_tmp.IsAllZeros())
							{
								ref var storage = ref ent_new.GetComponent<Storage.Data>();
								if (storage.IsNotNull() && storage.inv_storage.TryGetHandle(out var inv))
								{
									var inv_span = inv.GetSpan();
									if (Assert.Check(resources_tmp.AsSpan().TryCopyTo(inv_span), level: Assert.Level.Warn))
									{
										inv.MarkDirty();
									}
								}
							}

							// 6. VÉHICULE : respawn séparé, puis r-assise
							if (bring_vehicle_spawn)
							{
								ref var region_vehicle = ref Region.GetRegion(target_region_id_local);
								if (region_vehicle.IsNull())
								{
									App.WriteLine($"[Teleporter] target region {target_region_id_local} introuvable au respawn du vehicule", App.Color.Red);
									return ent_new;
								}

								var name_seat_tmp = name_seat_local;
								var was_driver_tmp = was_driver;
								var dest_pos_vehicle_tmp = dest_pos_vehicle;
								var h_player_tmp = h_player;
								var seat_index_tmp = seat_index_old;
								var seat_count_tmp = seats_old.Count;

								if (!is_same_region) RegisterBoard(ent_new.id, new PendingBoard());

								region_vehicle.SpawnPrefab(prefab: vehicle_prefab, position: dest_pos_vehicle_tmp, faction_id: faction_local).ContinueWith((ent_vehicle_new) =>
								{
									if (!ent_vehicle_new.IsAlive())
									{
										if (!is_same_region) RemoveBoard(ent_new.id);
										App.WriteLine("[Teleporter] le vehicule n a pas pu etre respawn", App.Color.Red);
										return ent_vehicle_new;
									}

									App.WriteLine($"[Teleporter] vehicule spawned {ent_vehicle_new}");

									void ScheduleBoard(int next_attempt)
									{
										ref var region_board = ref ent_vehicle_new.GetRegion();
										region_board.Schedule((ref Region.Data r_board) => TryBoard(next_attempt));
									}

									void TryBoard(int attempts)
									{
										if (!ent_new.IsAlive() || !ent_vehicle_new.IsAlive())
										{
											App.WriteLine("[Teleporter] re-assise abandonnee : corps ou vehicule disparu", App.Color.Red);
											return;
										}

										if (attempts == 0 && board_delay_ticks > 0)
										{
											App.WriteLine($"[Teleporter] voyage inter-region : attente de {board_delay_ticks} ticks avant de rasseoir le joueur");
										}

										if (attempts < board_delay_ticks)
										{
											ScheduleBoard(attempts + 1);
											return;
										}

										if (board_delay_ticks > 0)
										{
											var character_parts_new = new System.Collections.Generic.List<Entity>();
											CollectHierarchy(ent_new, character_parts_new);
											if (character_parts_new.Count < character_parts_count_old)
											{
												if (attempts < board_delay_ticks + 300)
												{
													ScheduleBoard(attempts + 1);
													return;
												}

												App.WriteLine($"[Teleporter] personnage incomplet ({character_parts_new.Count}/{character_parts_count_old} entites) : le joueur reste debout, pas de re-assise", App.Color.Red);
												return;
											}
										}

										var hierarchy_new = new System.Collections.Generic.List<Entity>();
										CollectHierarchy(ent_vehicle_new, hierarchy_new);

										var seats_new = new System.Collections.Generic.List<Entity>();
										var ent_cabin_new = Entity.None;
										foreach (var ent in hierarchy_new)
										{
											if (!ent.IsAlive()) continue;
											if (!ent_cabin_new.IsAlive() && ent.HasComponent<Vehicle.Data>()) ent_cabin_new = ent;
											if (ent.HasComponent<Seat.Data>()) seats_new.Add(ent);
										}

										var ent_seat_new = Entity.None;
										var found_by = "";

										if (ent_cabin_new.IsAlive())
										{
											ref var vehicle_new = ref ent_cabin_new.GetComponent<Vehicle.Data>();
											if (vehicle_new.IsNotNull() && vehicle_new.ent_seat.IsAlive() && vehicle_new.ent_seat.HasComponent<Seat.Data>())
											{
												ent_seat_new = vehicle_new.ent_seat;
												found_by = "Vehicle.ent_seat";
											}
										}

										if (!ent_seat_new.IsAlive() && seat_index_tmp >= 0 && seats_new.Count == seat_count_tmp && seat_index_tmp < seats_new.Count)
										{
											ent_seat_new = seats_new[seat_index_tmp];
											found_by = $"rang {seat_index_tmp}/{seat_count_tmp}";
										}

										if (!ent_seat_new.IsAlive() && name_seat_tmp != null)
										{
											foreach (var ent in seats_new)
											{
												if (ent.GetName().ToString() == name_seat_tmp)
												{
													ent_seat_new = ent;
													found_by = $"nom '{name_seat_tmp}'";
													break;
												}
											}
										}

										if (!ent_seat_new.IsAlive())
										{
											if (attempts < board_delay_ticks + 120)
											{
												ScheduleBoard(attempts + 1);
												return;
											}

											var names = new System.Text.StringBuilder();
											var name_count = 0;
											foreach (var ent in hierarchy_new)
											{
												if (!ent.IsAlive() || name_count >= 60) continue;
												names.Append(ent.GetName().ToString()).Append(ent.HasComponent<Seat.Data>() ? "(Seat.Data)" : "").Append(' ');
												name_count++;
											}

											App.WriteLine($"[Teleporter] siege introuvable apres {attempts} ticks : {hierarchy_new.Count} entites, {seats_new.Count} avec Seat.Data (avant: {seat_count_tmp}), siege d'origine '{name_seat_tmp}' rang {seat_index_tmp}. Entites : {names}", App.Color.Red);
											return;
										}

										Vehicle.AddPassenger(ent_seat: ent_seat_new, ent_passenger: ent_new);
										App.WriteLine($"[Teleporter] joueur {ent_new} rassis au siege {ent_seat_new} de {ent_vehicle_new} (trouve par {found_by}, apres {attempts} ticks){(was_driver_tmp ? " [pilote]" : "")}");

										ref var region_seat = ref ent_seat_new.GetRegion();
										if (region_seat.IsNotNull())
										{
											region_seat.Schedule((ref Region.Data r_dbg) =>
											{
												ref var seat_dbg = ref ent_seat_new.GetComponent<Seat.Data>();
												var seat_flags = seat_dbg.IsNotNull() ? seat_dbg.flags.ToString() : "null";

												ref var region_dbg = ref ent_new.GetRegion();
												ref var character_dbg = ref region_dbg.GetCharacter(h_char_local, out var ent_character_dbg);
												var ent_controlled_dbg = character_dbg.IsNotNull() ? character_dbg.ent_controlled : Entity.None;

												ref var player_dbg = ref h_player_tmp.GetPlayer(ref region_dbg);
												var player_controlled_dbg = player_dbg.IsNotNull() ? player_dbg.ent_controlled : Entity.None;

												App.WriteLine($"[Teleporter] diag: corps={ent_new}, siege={ent_seat_new}, seat.flags={seat_flags}, parent Seat du corps={ent_new.GetParent(Relation.Type.Seat)}, character.ent_controlled={ent_controlled_dbg}, player.ent_controlled={player_controlled_dbg}");
											});
										}
									}

									if (is_same_region)
									{
										TryBoard(0);
										return ent_vehicle_new;
									}

									var ent_body_board = ent_new;
									int delay_min_ticks = board_delay_ticks;

									void WaitClientReady(int n)
									{
										var board = GetBoard(ent_body_board.id);
										if (board == null) return;

										if (board.client_ready && n >= delay_min_ticks)
										{
											RemoveBoard(ent_body_board.id);
											App.WriteLine($"[Teleporter] client pret ({n} ticks) : r-assise de {ent_body_board} dans {ent_vehicle_new}");
											TryBoard(delay_min_ticks + 1);
											return;
										}

										if (n > 3000)
										{
											RemoveBoard(ent_body_board.id);
											App.WriteLine($"[Teleporter] aucun signal client depuis 60 s : le joueur reste debout pres de {ent_vehicle_new}, montee a bord manuelle possible", App.Color.Yellow);
											return;
										}

										ref var region_wait = ref ent_vehicle_new.GetRegion();
										if (region_wait.IsNotNull()) region_wait.Schedule((ref Region.Data r_wait) => WaitClientReady(n + 1));
									}

									WaitClientReady(0);

									return ent_vehicle_new;
								});
							}

							return ent_new;
						});
					}

					region.Schedule((ref Region.Data r) => SpawnWhenOldBodyGone(ref r, 0));
				});
			}
#endif
		}

		public struct BoardReadyRPC : Net.IRPC<Transform.Data>
		{
#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Transform.Data data)
			{
				ref var region = ref rpc.entity.GetRegion();
				if (region.IsNull()) return;

				rpc.GetSenderPlayer(out var player_asset);
				var h_player = player_asset?.GetHandle() ?? default;

				ref var player = ref h_player.GetPlayer(ref region);
				if (player.IsNull()) return;
				if (player.ent_controlled.id != rpc.entity.id) return;

				if (MarkClientReady(rpc.entity.id))
				{
					App.WriteLine($"[Teleporter] client pret : region chargee, corps {rpc.entity} cree cote client, r-assise imminente");
				}
			}
#endif
		}

#if CLIENT
		public struct TeleporterGUI : IGUICommand
		{
			public Entity ent_teleporter;

			public void Draw()
			{
				using (var window = GUI.Window.Interaction("Tunnel de Téléportation"u8, this.ent_teleporter))
				{
					this.StoreCurrentWindowTypeID(order: -100);
					if (window.show)
					{
						if (GUI.DrawButton("Travel"u8, size: new(128, 40), color: GUI.col_button_ok))
						{
							ref var tp_data = ref this.ent_teleporter.GetComponent<LinkedMapTeleporter.Data>();
							ClientBoardState.target_region_id = tp_data.IsNotNull() ? tp_data.target_region_id : (byte)0;

							var rpc = new LinkTeleportRPC
							{
								h_character = Client.GetCharacterHandle()
							};
							rpc.Send(this.ent_teleporter);
						}
					}
				}
			}
		}

		[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global)]
		public static void OnGUI(Entity entity,
		[Source.Owned] in Interactable.Data interactable,
		[Source.Owned] in LinkedMapTeleporter.Data teleporter)
		{
			if (interactable.IsActive())
			{
				var gui = new TeleporterGUI() { ent_teleporter = entity };
				gui.Submit();
			}
		}

		internal static class ClientBoardState
		{
			public static byte target_region_id;
		}

		[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global), HasTag("local", true, Source.Modifier.Owned)]
		public static void OnBoardReady(Entity entity, [Source.Owned] in Player.Data player)
		{
			var target_region_id = ClientBoardState.target_region_id;
			if (target_region_id == 0) return;

			var ent_body = player.ent_controlled;
			if (!ent_body.IsAlive()) return;
			if (ent_body.GetRegionID() != target_region_id) return;

			ClientBoardState.target_region_id = 0;
			var rpc = new BoardReadyRPC();
			rpc.Send(ent_body);
		}
#endif
	}
}