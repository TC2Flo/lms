namespace TC2.Base.Components
{
	public static partial class Spawner
	{
		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Region, flags: IComponent.Flags.Remove_On_Split)]
		public partial struct Data(): IComponent
		{
			[Flags]
			public enum Flags: byte
			{
				None = 0,
			}

			public Spawner.Data.Flags flags;

			public byte group_size = 1;
			public byte group_size_extra;
			public byte max_count = 5;

			public AI.Stance ai_stance;
			public AI.Behavior.Type ai_type;

			//public FixedString32 filter_tag;

			public float interval = 30.00f;
			public float interval_extra = 10.00f;
			public float interval_count_modifier = 1.00f;

			[Asset.Ignore, Save.Ignore, Net.Ignore] public float t_next_spawn;
		}

		//[IEvent.Data]
		//public partial struct SpawnEvent: IEvent
		//{
		//	[Flags]
		//	public enum Flags: ushort
		//	{
		//		None = 0,

		//		Control = 1 << 0,
		//	}

		//	public Entity ent_target;
		//	public Vector2 position;

		//	public ICharacter.Handle h_character;
		//	public IFaction.Handle h_faction;
		//	public IPlayer.Handle h_player;
		//}

		//		public struct SpawnRPC: Net.IRPC<Spawner.Data>
		//		{
		//			public ICharacter.Handle h_character;
		//			public FixedArray4<IKit.Handle> kits;
		//			public bool control;

		//#if SERVER
		//			public void Invoke(Net.IRPC.Context rpc, ref Dormitory.Data data)
		//			{
		//				ref var character_data = ref this.h_character.GetData();
		//				if (character_data.IsNotNull())
		//				{
		//					ref var region = ref connection.GetRegion();
		//					ref var player = ref connection.GetPlayer();

		//					if (region.IsNotNull() && player.IsNotNull())
		//					{
		//						ref var transform = ref entity.GetComponent<Transform.Data>();
		//						if (transform.IsNotNull())
		//						{
		//							Dormitory.SpawnCharacterWithKits(ref region, position: transform.position, ent_dormitory: entity, h_character: this.h_character, kits: this.kits.AsSpan(), h_faction: character_data.faction.id == 0 ? player.h_faction : character_data.faction, h_player: connection.GetPlayerHandle(), control: this.control);
		//						}
		//					}
		//				}
		//			}
		//#endif
		//		}

		public static bool CanSpawnAsCharacter(ref this ICharacter.Data character_data, IFaction.Handle? h_faction = default, IFaction.Handle h_faction_spawn = default, IPlayer.Handle h_player = default, Spawn.Flags spawn_flags = Spawn.Flags.None)
		{
			if (character_data.IsNotNull() && character_data.flags.HasNone(Character.Flags.Dead))
			{
				if (character_data.players.ContainsOrEmpty(h_player, out var is_empty) && !is_empty && h_player) return true;
				else if (is_empty)
				{
					var h_faction_tmp = h_faction ?? character_data.faction;
					if (h_faction_tmp == character_data.faction) return true; // Character is the same team
					else if (h_faction_tmp == h_faction_spawn && !character_data.faction) return true; // Character is neutral, but is in your team's spawn 
					else if (!h_faction_tmp && spawn_flags.HasAny(Spawn.Flags.Neutral_Only)) return true; // Allow spawning on neutral-only spawns, even if not your team (e.g. kobolds, badgers)
					else if (character_data.flags.HasAny(Character.Flags.Public)) return true;
					else if (!character_data.faction && spawn_flags.HasAny(Spawn.Flags.Public) && Constants.Respawn.unclaimed_faction_spawnpoints) return true;
				}
			}
			return false;
		}

		public static bool CanSpawnAsCharacter(this ICharacter.Handle h_character, IFaction.Handle? h_faction = default, IFaction.Handle h_faction_spawn = default, IPlayer.Handle h_player = default, Spawn.Flags spawn_flags = Spawn.Flags.None)
		{
			ref var character_data = ref h_character.GetData();
			return character_data.CanSpawnAsCharacter(h_faction: h_faction, h_faction_spawn: h_faction_spawn, h_player: h_player, spawn_flags: spawn_flags);
		}

		//public static bool IsVisibleToFaction(in this Spawn.Data spawn, IFaction.Handle h_faction, IFaction.Handle h_faction_spawn)
		//{
		//	return spawn.flags.HasAny(Spawn.Flags.Neutral_Only) ? !h_faction : (spawn.flags.HasAny(Spawn.Flags.Faction) ? h_faction_spawn.IsOwned(h_faction) : h_faction_spawn.IsOwnedOrNeutral(h_faction)) || spawn.flags.HasAny(Spawn.Flags.Public);
		//}

		public static bool IsVisibleToFaction(this Spawn.Data spawn, IFaction.Handle h_faction, IFaction.Handle h_faction_spawn)
		{
			if (spawn.flags.HasAny(Spawn.Flags.Public)) return true;
			else if (spawn.flags.HasAny(Spawn.Flags.Neutral_Only)) return !h_faction || Constants.Respawn.unclaimed_faction_spawnpoints;
			else if (spawn.flags.HasAny(Spawn.Flags.Faction)) return h_faction_spawn.IsOwned(h_faction);
			else return h_faction_spawn.IsOwnedOrNeutral(h_faction);
		}

		public static bool IsSelectableByFaction(this Spawn.Data spawn, IFaction.Handle h_faction, IFaction.Handle h_faction_spawn, bool has_characters, bool is_visible)
		{
			//return is_visible && (has_characters || h_faction_spawn.id == h_faction || (h_faction_spawn.id == 0 && spawn.flags.HasAny(Spawn.Flags.Public)));
			return is_visible && (spawn.flags.HasAny(Spawn.Flags.Public) || has_characters || h_faction_spawn == h_faction);
		}

		public static (int index, ICharacter.Handle h_character) GetSpawnableCharacter(this Span<ICharacter.Handle> characters, IFaction.Handle? h_faction, IFaction.Handle h_faction_spawn, IPlayer.Handle h_player, Spawn.Flags spawn_flags = Spawn.Flags.None)
		{
			for (var i = 0; i < characters.Length; i++)
			{
				var h_character = characters[i];
				if (h_character && h_character.CanSpawnAsCharacter(h_faction: h_faction, h_faction_spawn: h_faction_spawn, h_player: h_player, spawn_flags: spawn_flags)) return (i, h_character);
			}
			return (-1, ICharacter.Handle.None);
		}

		public static Span<(int index, ICharacter.Handle h_character)> GetSpawnableCharacters(this Span<ICharacter.Handle> characters, Span<(int index, ICharacter.Handle h_character)> span, IFaction.Handle? h_faction, IFaction.Handle h_faction_spawn, IPlayer.Handle h_player, Spawn.Flags spawn_flags = Spawn.Flags.None)
		{
			var num = 0;
			for (var i = 0; (num < span.Length) && (i < characters.Length); i++)
			{
				var h_character = characters[i];
				if (h_character && h_character.CanSpawnAsCharacter(h_faction: h_faction, h_faction_spawn: h_faction_spawn, h_player: h_player, spawn_flags: spawn_flags))
				{
					span.Add((i, h_character), ref num);
				}
			}
			return span.WithLength(num);
		}

		public static (int index, ICharacter.Handle h_character) GetPlayableCharacter(ref this Dormitory.Data dormitory, IFaction.Handle? h_faction, IFaction.Handle h_faction_spawn, IPlayer.Handle h_player, Spawn.Flags spawn_flags = Spawn.Flags.None)
		{
			var characters = dormitory.GetPlayableCharacterSpan();
			return Spawner.GetSpawnableCharacter(characters: characters, h_faction: h_faction, h_faction_spawn: h_faction_spawn, h_player: h_player, spawn_flags: spawn_flags);
		}

		public static Span<(int index, ICharacter.Handle h_character)> GetPlayableCharacters(ref this Dormitory.Data dormitory, Span<(int index, ICharacter.Handle h_character)> span, IFaction.Handle? h_faction, IFaction.Handle h_faction_spawn, IPlayer.Handle h_player, Spawn.Flags spawn_flags = Spawn.Flags.None)
		{
			var characters = dormitory.GetPlayableCharacterSpan();
			return Spawner.GetSpawnableCharacters(characters: characters, span: span, h_faction: h_faction, h_faction_spawn: h_faction_spawn, h_player: h_player, spawn_flags: spawn_flags);
		}

		public static (int index, ICharacter.Handle h_character) GetSpawnableCharacter(ref this Dormitory.Data dormitory, IFaction.Handle? h_faction, IFaction.Handle h_faction_spawn, IPlayer.Handle h_player, Spawn.Flags spawn_flags = Spawn.Flags.None)
		{
			var characters = dormitory.GetCharacterSpan();
			return Spawner.GetSpawnableCharacter(characters: characters, h_faction: h_faction, h_faction_spawn: h_faction_spawn, h_player: h_player, spawn_flags: spawn_flags);
		}

		public static Span<(int index, ICharacter.Handle h_character)> GetSpawnableCharacters(ref this Dormitory.Data dormitory, Span<(int index, ICharacter.Handle h_character)> span, IFaction.Handle? h_faction, IFaction.Handle h_faction_spawn, IPlayer.Handle h_player, Spawn.Flags spawn_flags = Spawn.Flags.None)
		{
			var characters = dormitory.GetCharacterSpan();
			return Spawner.GetSpawnableCharacters(characters: characters, span: span, h_faction: h_faction, h_faction_spawn: h_faction_spawn, h_player: h_player, spawn_flags: spawn_flags);
		}

		public static bool HasSpawnableCharacters(this Span<ICharacter.Handle> characters, IFaction.Handle? h_faction, IFaction.Handle h_faction_spawn, IPlayer.Handle h_player, Spawn.Flags spawn_flags = Spawn.Flags.None)
		{
			return Spawner.GetSpawnableCharacter(characters: characters, h_faction: h_faction, h_faction_spawn: h_faction_spawn, h_player: h_player, spawn_flags: spawn_flags).index >= 0;
		}

		public static bool HasSpawnableCharacters(ref this Dormitory.Data dormitory, IFaction.Handle? h_faction, IFaction.Handle h_faction_spawn, IPlayer.Handle h_player, Spawn.Flags spawn_flags = Spawn.Flags.None)
		{
			return Spawner.GetSpawnableCharacter(dormitory: ref dormitory, h_faction: h_faction, h_faction_spawn: h_faction_spawn, h_player: h_player, spawn_flags: spawn_flags).index >= 0;
		}

		public static bool HasPlayableCharacters(ref this Dormitory.Data dormitory, IFaction.Handle? h_faction, IFaction.Handle h_faction_spawn, IPlayer.Handle h_player, Spawn.Flags spawn_flags = Spawn.Flags.None)
		{
			return Spawner.GetPlayableCharacter(dormitory: ref dormitory, h_faction: h_faction, h_faction_spawn: h_faction_spawn, h_player: h_player, spawn_flags: spawn_flags).index >= 0;
		}

		//public static bool IsSelectableByFaction(ref this Spawn.Data spawn, IFaction.Handle h_faction_spawn, IFaction.Handle h_faction)
		//{
		//	if (h_faction_spawn.id == h_faction.id)
		//	{
		//		if (h_faction.id == 0)
		//		{
		//			return spawn.flags.HasAny(Spawn.Flags.Public | Spawn.Flags.Neutral_Only);
		//		}
		//		else
		//		{
		//			return true;
		//		}
		//	}
		//	else
		//	{

		//	}
		//}

		public static readonly char[] initials =
		[
			'A',
			'B',
			'D',
			'E',
			'F',
			'G',
			'H',
			'I',
			'J',
			'M',
			'N',
			'K',
			'P',
			'R',
			'S',
			'T',
			'V',
			'W'
		];

		public static string GenerateName(ref XorRandom random, ISpecies.Flags species_flags, Character.Traits character_flags, Organic.Gender gender)
		{
			var name = string.Empty;
			if (species_flags.HasAll(ISpecies.Flags.Humanoid | ISpecies.Flags.Sapient) && character_flags.HasNone(Character.Traits.Illiterate | Character.Traits.Pest | Character.Traits.Lowlife | Character.Traits.Feral | Character.Traits.Pet | Character.Traits.Animal | Character.Traits.Lower_Class | Character.Traits.Poor | Character.Traits.Dumb | Character.Traits.Slave))
			{
				name = $"{initials.GetRandom(ref random)}.{Nameable.GenerateRandomHumanName(ref random, gender: gender)}";
			}
			else if (species_flags.HasAll(ISpecies.Flags.Insect))
			{
				name = $"Hajzl-{Nameable.GenerateName(ref random, Nameable.Kind.Consumable, Nameable.Tags.Dangerous | Nameable.Tags.Chemical | Nameable.Tags.Industrial)}";
			}
			else
			{
				name = Nameable.GenerateRandomKoboldName(ref random, gender: gender);
			}
			return name;
		}

#if SERVER
		[Shitcode]
		public static bool TryAddKit(ref Loadout.Filter? filter, Kit.Slot slot, ref WeightedList<IKit.Handle> kits_weighted, ref Span<IKit.Handle> kits, ref XorRandom random, ref ICharacter.Data character_data, ref bool added)
		{
			var ok = true;

			if (filter.TryGetValue(out var v_filter) && random.NextBool(v_filter.chance))
			{
				var ts = Timestamp.Now();

				kits_weighted.Clear();

				foreach (var h_kit_tmp in kits)
				{
					ref var kit_data = ref h_kit_tmp.GetData();
					if (kit_data.IsNotNull() && kit_data.slot == slot) // && (weight *= kit_data.character_flags.EvaluateOverlap(args.flags)) > 0.00f)
					{
						if (character_data.kits.Contains(h_kit_tmp)) continue;

						var weight_category = 0.00f;
						if (!v_filter.categories.TryEvaluateOverlap(kit_data.categories, ref weight_category)) continue;

						var weight_character = 0.00f;
						if (!kit_data.character_flags.TryEvaluateOverlap(character_data.traits, ref weight_character)) continue;

						var weight_total = weight_category * weight_character;
						if (weight_total <= 0.00f) continue;

						kits_weighted.Add(new WeightedList<IKit.Handle>.Item(weight_total, h_kit_tmp), rebuild: false);
					}
				}

				if (kits_weighted.Count > 0)
				{
					kits_weighted.Rebuild();

					//var ts_elapsed = ts.GetMilliseconds();
					//App.WriteLine($"filtered {kits_weighted.Count}/{kits.Length} kits in {ts_elapsed:0.0000} ms");

					//var sb = new StringBuilder();
					//foreach (var item in kits_weighted)
					//{
					//	sb.AppendLine($"[{item.weight:000.00}] {item.value}");
					//	//sb.AppendLine($"{item}");
					//}
					//App.WriteLine($"\n{sb}");

					var h_kit = kits_weighted.GetRandom(ref random);

					//ok = !character_data.kits.Contains(in h_kit) && character_data.kits.TryAdd(in h_kit);
					ok &= character_data.kits.TryAdd(h_kit);
					added |= ok;
				}
			}

			return ok;
		}

		[Shitcode]
		public static bool TryGenerateKits(ref XorRandom random, ICharacter.Handle h_character, Filter.Mask2x<NPC.ItemHints> kit_categories_filter = default)
		{
			var added = false;

			ref var character_data = ref h_character.GetData();
			if (character_data.IsNotNull())
			{
				var args = (character_data.species, character_data.faction, character_data.traits, kit_categories_filter);

				Span<IKit.Handle> kits = stackalloc IKit.Handle[32];
				IKit.Database.GetHandlesFiltered(ref kits, in args, static (in IKit.Definition def, in (ISpecies.Handle species, IFaction.Handle faction, Character.Traits flags, Filter.Mask2x<NPC.ItemHints> kit_categories_filter) args) =>
				{
					return (def.data.species.id == 0 || def.data.species == args.species) && (def.data.faction.id == 0 || def.data.faction == args.faction) && args.kit_categories_filter.Evaluate(def.data.categories) && def.data.character_flags.EvaluateOverlap(args.flags) > 0.00f;
				});

				ref var origin_data = ref character_data.origin.GetData();
				if (origin_data.IsNotNull())
				{
					var kits_weighted = new WeightedList<IKit.Handle>();

					if (!TryAddKit(ref origin_data.kit_filter_primary, Kit.Slot.Primary, ref kits_weighted, ref kits, ref random, ref character_data, ref added)) goto done;
					if (!TryAddKit(ref origin_data.kit_filter_secondary, Kit.Slot.Secondary, ref kits_weighted, ref kits, ref random, ref character_data, ref added)) goto done;
					if (!TryAddKit(ref origin_data.kit_filter_tool, Kit.Slot.Tool, ref kits_weighted, ref kits, ref random, ref character_data, ref added)) goto done;
					if (!TryAddKit(ref origin_data.kit_filter_utility, Kit.Slot.Utility, ref kits_weighted, ref kits, ref random, ref character_data, ref added)) goto done;
					if (!TryAddKit(ref origin_data.kit_filter_head, Kit.Slot.Head, ref kits_weighted, ref kits, ref random, ref character_data, ref added)) goto done;
					if (!TryAddKit(ref origin_data.kit_filter_chest, Kit.Slot.Chest, ref kits_weighted, ref kits, ref random, ref character_data, ref added)) goto done;
					if (!TryAddKit(ref origin_data.kit_filter_vehicle, Kit.Slot.Vehicle, ref kits_weighted, ref kits, ref random, ref character_data, ref added)) goto done;
					if (!TryAddKit(ref origin_data.kit_filter_resources, Kit.Slot.Resources, ref kits_weighted, ref kits, ref random, ref character_data, ref added)) goto done;
					if (!TryAddKit(ref origin_data.kit_filter_harness, Kit.Slot.Harness, ref kits_weighted, ref kits, ref random, ref character_data, ref added)) goto done;
				}
			}

			done:
			return added;
		}

		[Shitcode]
		public static bool TryAddKit_New(ref Loadout.Filter? filter, Kit.Slot slot, ref WeightedList<IKit.Handle> kits_weighted, ref Span<IKit.Handle> kits_selected, scoped Span<IKit.Handle> kits_available, Character.Traits traits, ref XorRandom random, ref bool added)
		{
			var ok = true;

			if (filter.TryGetValue(out var v_filter) && random.NextBool(v_filter.chance))
			{
				var ts = Timestamp.Now();

				kits_weighted.Clear();

				foreach (var h_kit_tmp in kits_available)
				{
					ref var kit_data = ref h_kit_tmp.GetData();
					if (kit_data.IsNotNull() && kit_data.slot == slot) // && (weight *= kit_data.character_flags.EvaluateOverlap(args.flags)) > 0.00f)
					{
						if (kits_selected.Contains(h_kit_tmp)) continue;

						var weight_category = 0.00f;
						if (!v_filter.categories.TryEvaluateOverlap(kit_data.categories, ref weight_category)) continue;

						var weight_character = 0.00f;
						if (!kit_data.character_flags.TryEvaluateOverlap(traits, ref weight_character)) continue;

						var weight_total = weight_category * weight_character;
						if (weight_total <= 0.00f) continue;

						kits_weighted.Add(new WeightedList<IKit.Handle>.Item(weight_total, h_kit_tmp), rebuild: false);
					}
				}

				if (kits_weighted.Count > 0)
				{
					kits_weighted.Rebuild();

					//var ts_elapsed = ts.GetMilliseconds();
					//App.WriteLine($"filtered {kits_weighted.Count}/{kits.Length} kits in {ts_elapsed:0.0000} ms");

					//var sb = new StringBuilder();
					//foreach (var item in kits_weighted)
					//{
					//	sb.AppendLine($"[{item.weight:000.00}] {item.value}");
					//	//sb.AppendLine($"{item}");
					//}
					//App.WriteLine($"\n{sb}");

					var h_kit = kits_weighted.GetRandom(ref random);

					//ok = !character_data.kits.Contains(in h_kit) && character_data.kits.TryAdd(in h_kit);
					ok &= kits_selected.TryAdd(h_kit);
					added |= ok;
				}
			}

			return ok;
		}

		[Shitcode]
		public static bool TryGenerateKits_New(ref Span<IKit.Handle> kits_selected, ref XorRandom random, IOrigin.Handle h_origin, ISpecies.Handle h_species, IFaction.Handle h_faction, Character.Traits traits)
		{
			var added = false;

			var args = (h_species, h_faction, traits);

			Span<IKit.Handle> kits_available = FixedArray.CreateSpan32NoInit<IKit.Handle>(out var kits_available_buffer);
			IKit.Database.GetHandlesFiltered(ref kits_available, in args, static (in IKit.Definition def, in (ISpecies.Handle species, IFaction.Handle faction, Character.Traits flags) args) =>
			{
				return (def.data.species.id == 0 || def.data.species == args.species) && (def.data.faction.id == 0 || def.data.faction == args.faction) && def.data.character_flags.EvaluateOverlap(args.flags) > 0.00f;
			});

			ref var origin_data = ref h_origin.GetData();
			if (origin_data.IsNotNull())
			{
				var kits_weighted = new WeightedList<IKit.Handle>();

				if (!TryAddKit_New(ref origin_data.kit_filter_primary, Kit.Slot.Primary, ref kits_weighted, ref kits_selected, kits_available, traits, ref random, ref added)) goto done;
				if (!TryAddKit_New(ref origin_data.kit_filter_secondary, Kit.Slot.Secondary, ref kits_weighted, ref kits_selected, kits_available, traits, ref random, ref added)) goto done;
				if (!TryAddKit_New(ref origin_data.kit_filter_tool, Kit.Slot.Tool, ref kits_weighted, ref kits_selected, kits_available, traits, ref random, ref added)) goto done;
				if (!TryAddKit_New(ref origin_data.kit_filter_utility, Kit.Slot.Utility, ref kits_weighted, ref kits_selected, kits_available, traits, ref random, ref added)) goto done;
				if (!TryAddKit_New(ref origin_data.kit_filter_head, Kit.Slot.Head, ref kits_weighted, ref kits_selected, kits_available, traits, ref random, ref added)) goto done;
				if (!TryAddKit_New(ref origin_data.kit_filter_chest, Kit.Slot.Chest, ref kits_weighted, ref kits_selected, kits_available, traits, ref random, ref added)) goto done;
				if (!TryAddKit_New(ref origin_data.kit_filter_vehicle, Kit.Slot.Vehicle, ref kits_weighted, ref kits_selected, kits_available, traits, ref random, ref added)) goto done;
				if (!TryAddKit_New(ref origin_data.kit_filter_resources, Kit.Slot.Resources, ref kits_weighted, ref kits_selected, kits_available, traits, ref random, ref added)) goto done;
				if (!TryAddKit_New(ref origin_data.kit_filter_harness, Kit.Slot.Harness, ref kits_weighted, ref kits_selected, kits_available, traits, ref random, ref added)) goto done;
			}

			done:
			return added;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static UniTask<Entity> SpawnCharacter(ref Region.Data region, ICharacter.Handle h_character, Vector2 position, Spawn.SpawnEvent.Flags flags, IFaction.Handle h_faction = default, IPlayer.Handle h_player = default)
		{
			ref var character_data = ref h_character.GetData(out var definition);
			if (character_data.IsNotNull())
			{
				definition.Flags.RemoveFlag(Asset.Flags.No_Save);

				//App.WriteValue(h_player);
				if (flags.HasAny(Spawn.SpawnEvent.Flags.Claim))
				{
					if (h_player && !definition.data.players.Contains(h_player))
					{
						definition.data.players = definition.data.players.Add(h_player);
						definition.data.t_next_abandon = World.GetGlobalTime() + Constants.Characters.abandon_cooldown;
					}
				}

				if (h_faction)
				{
					character_data.faction = h_faction;
				}

				//var has_controlled_entity = false;
				var ent_controlled = Entity.None;

				UniTask<Entity> task_ent_character;
				UniTask<Entity> task_ent_controlled;

				var ent_character = h_character.GetEntity(ref region); // Character.GetEntity(ref region, h_character);
				if (ent_character.IsAlive())
				{
					ref var character = ref ent_character.GetComponent<Character.Data>();
					if (character.IsNotNull())
					{
						if (character.ent_occupied.IsAlive())
						{
							//ent_character.RemoveRelation(Entity.Wildcard, Relation.Type.Stored);
						}
						else
						{
							ent_controlled = character.ent_controlled;
							//has_controlled_entity = ent_controlled.TrySet(character.ent_controlled, character.ent_controlled.IsAlive());
						}

					}

					task_ent_character = UniTask.FromResult(ent_character);
				}
				else
				{
					task_ent_character = h_character.Spawn(region.GetID());
					//Assert.Throw("TODO: Replace Character.CreateOrUpdate()", Assert.Level.Warn);
					//ent_character = h_character.CreateEntity Character.CreateOrUpdate(ref region, h_character, h_player: h_player);
				}

				task_ent_controlled = ent_controlled.IsAlive() ? UniTask.FromResult(ent_controlled) : region.SpawnPrefab(prefab: character_data.prefab, position: position, faction_id: h_faction);
				//UniTask.WhenAll(task_ent_character, task_ent_controlled).

				return task_ent_controlled.ContinueWith(async (ent_controlled) =>
				{
					//App.WriteValue(ent_controlled);
					var ent_character = await task_ent_character;
					//App.WriteValue(ent_character);

					ent_character.RemoveRelation(Entity.Wildcard, Relation.Type.Stored);
					ent_controlled.SetAssociatedCharacter(h_character);

					//await 

					//App.WriteValue(control);
					if (flags.HasAny(Spawn.SpawnEvent.Flags.Control))
					{
						ref var player = ref h_player.GetPlayer(ent_controlled.region_id);
						if (player.IsNotNull() && player.h_character != h_character)
						{
							player.SetControlledCharacter(ent_character);
						}
					}

					//App.WriteValue(ent_controlled);
					return ent_controlled;
				});
			}

			return default;
		}

		//public static Color32BGRA hair_color_gray = new(0xffe8d7c9);
		//public static Gradient<float> gr_age = new(25.00f, 27.00f, 28.00f, 29.00f, 30.00f, 31.00f, 32.00f, 33.00f, 34.00f, 36.00f, 38.00f, 40.00f, 42.00f, 45.00f, 65.00f);

		/// <summary>
		/// Creates a new character asset.
		/// </summary>
		/// <param name="region">Region to create the character for.</param>
		/// <param name="random"></param>
		/// <param name="h_origin"></param>
		/// <param name="h_character">If set, overwrites the character referred to by h_character.</param>
		/// <returns></returns>
		public static ICharacter.Handle CreateCharacter(ref Region.Data.Common region, ref XorRandom random, IOrigin.Handle h_origin,
		ICharacter.Handle h_character = default, IFaction.Handle h_faction = default, Asset.Scope scope = Asset.Scope.World, Asset.Flags? asset_flags = null,
		IPlayer.Handle h_player = default, ILocation.Handle h_location = default)
		{
			var character = new ICharacter.Data();
			character.origin = h_origin;
			character.faction = h_faction;

			if (h_player)
			{
				character.players = [h_player];
				character.t_next_abandon = World.GetGlobalTime() + Constants.Characters.abandon_cooldown;
			}
			if (!h_location) h_location = region.GetLocationHandle();

			ref var origin = ref character.origin.GetData();
			if (origin.IsNotNull() && Spawner.TryApplyOrigin(ref random, h_location, h_origin, ref character))
			{
				Prefab.Handle h_prefab_unit = default;
				ref var species = ref character.species.GetData();
				if (species.IsNotNull())
				{
					h_prefab_unit = species.prefab_unit;
				}

				if (!h_prefab_unit)
				{
					h_prefab_unit = "unit.guy";
				}

				var asset = h_character.GetDefinition();
				if (asset == null)
				{
					var identifier = $"{origin.species.GetIdentifier().OrDefault("misc")}.{Asset.GenerateRandomIdentifier()}";
					//App.WriteLine(identifier);

					asset = ICharacter.Database.RegisterOrUpdate(identifier,
						index: null,
						scope: scope,
						flags: asset_flags,
						region_id: region.GetID(),
						h_prefab: h_prefab_unit, //"unit.guy",
						h_prefab_region: "region.character",
						data: ref character);
				}
				else
				{
					asset.data = character;
				}
				asset.Sync(true);

				return asset;
			}

			return default;
		}

		[Flags]
		public enum ApplyOriginFlags: uint
		{
			None = 0u,

			Skip_Hair = 1u << 0,
			Skip_Age = 1u << 1,
			Skip_Traits = 1u << 2,
			Skip_Money = 1u << 3,
			Skip_Gender = 1u << 4,
			Skip_Experience = 1u << 5,
			Skip_Name = 1u << 6,
			Skip_Requirements = 1u << 7,
		}

		[Shitcode] // TODO: hardcoded ugly
		public static bool TryApplyOrigin(ref XorRandom random, ILocation.Handle h_location, IOrigin.Handle h_origin, ref ICharacter.Data character_data, Spawner.ApplyOriginFlags flags = ApplyOriginFlags.None)
		{
			ref var origin_data = ref h_origin.GetData(out var origin_asset);
			ref var location_data = ref h_location.GetData(out var location_asset);

			if (origin_data.IsNotNull() && location_data.IsNotNull())
			{
				character_data.origin = h_origin;
				//ref var map_info = ref region.GetMapInfo();

				//var location_asset = map_info.h_location.GetDefinition();

				ref var species = ref origin_data.species.GetData();
				character_data.species = origin_data.species;
				var h_species = character_data.species;

				var gender = character_data.gender;
				var traits = character_data.traits;
				var money = character_data.money;
				var experience = character_data.experience;
				var age = character_data.age;
				var name = character_data.name;

				// TODO: shitcode (GC, perf)
				//var origins = new WeightedList<IOrigin.Handle>(IOrigin.Database.GetAssets().Where(x => x.id != 0 && x.data.species == h_species).Select(x => new WeightedList<IOrigin.Handle>.Item(x.data.conditions.CalculateWeight(in location_data), x)));

				// TODO: shitcode (GC, perf)

				if (flags.HasNone(ApplyOriginFlags.Skip_Gender)) gender = random.NextBool(origin_data.gender_ratio) ? Organic.Gender.Female : Organic.Gender.Male;
				if (flags.HasNone(ApplyOriginFlags.Skip_Age)) age = origin_data.age.GetValue(Maths.EaseInOut(random.NextFloat01(), Maths.Easing.Quad));

				if (flags.HasNone(ApplyOriginFlags.Skip_Traits))
				{
					traits |= origin_data.character_flags;

					traits |= (origin_data.character_flags_optional & ~traits).GetRandomFlag(ref random);
					traits |= (origin_data.character_flags_optional & ~traits).GetRandomFlag(ref random);
					traits |= (origin_data.character_flags_optional & ~traits).GetRandomFlag(ref random);
				}

				if (flags.HasNone(ApplyOriginFlags.Skip_Money))
				{
					money += origin_data.money;
				}

				if (flags.HasNone(ApplyOriginFlags.Skip_Experience))
				{
					experience.values.AsVec32u8Ref() += origin_data.experience.values;
				}

				if (flags.HasNone(ApplyOriginFlags.Skip_Requirements)) character_data.requirements_hire = origin_data.requirements.Duplicate();
				ref var req_money = ref character_data.requirements_hire.AsSpan().GetMoneyRequirement();

				if (flags.HasNone(ApplyOriginFlags.Skip_Name) && name.IsNullOrEmpty()) name = GenerateName(ref random, species.flags, traits, gender);

				if (age > species.lifecycle.age_mature)
				{
					if (traits.HasAll(Character.Traits.Educated | Character.Traits.Professional) && !traits.HasAny(Character.Traits.Artisan | Character.Traits.Unskilled) && random.NextBool(0.10f + (((location_data.statistics.wealth * 0.60f) + (location_data.statistics.education * 0.80f) + (location_data.statistics.urbanization * 0.50f)) * 0.20f)))
					{
						if (traits.HasAny(Character.Traits.Medical))
						{
							if (random.NextBool(0.30f * location_data.statistics.urbanization * location_data.statistics.education))
							{
								if (flags.HasNone(ApplyOriginFlags.Skip_Name)) name = "MUDr. " + name;

								experience[Experience.Type.Medicine] += random.NextByteRange(4, 8);
								experience[Experience.Type.Intellect] += random.NextByteRange(3, 7);
								experience[Experience.Type.Dexterity] += random.NextByteRange(1, 3);
								experience[Experience.Type.Alchemistry] += random.NextByteRange(2, 5);
								experience[Experience.Type.Blades] += random.NextByteRange(2, 6);
								experience[Experience.Type.Law] += random.NextByteRange(1, 4);

								if (req_money.IsNotNull())
								{
									req_money.amount *= 1.45f;
								}
							}
							else if (random.NextBool(0.40f * location_data.statistics.wilderness * location_data.statistics.savagery * location_data.statistics.education))
							{
								if (flags.HasNone(ApplyOriginFlags.Skip_Name)) name = "MVDr. " + name;

								experience[Experience.Type.Medicine] += random.NextByteRange(2, 4);
								experience[Experience.Type.Intellect] += random.NextByteRange(2, 4);
								experience[Experience.Type.Strength] += random.NextByteRange(3, 6);
								experience[Experience.Type.Endurance] += random.NextByteRange(2, 4);
								experience[Experience.Type.Animals] += random.NextByteRange(3, 7);
								experience[Experience.Type.Farming] += random.NextByteRange(3, 5);

								if (req_money.IsNotNull())
								{
									req_money.amount *= 1.27f;
								}
							}
							else if (random.NextBool(location_data.statistics.wealth * 0.60f * location_data.statistics.urbanization * location_data.statistics.education))
							{
								if (flags.HasNone(ApplyOriginFlags.Skip_Name)) name = "Mgr. " + name;

								experience[Experience.Type.Intellect] += random.NextByteRange(1, 2);
								experience[Experience.Type.Medicine] += random.NextByteRange(1, 3);
								experience[Experience.Type.Charisma] += random.NextByteRange(2, 4);
								experience[Experience.Type.Law] += random.NextByteRange(1, 2);
								experience[Experience.Type.Commerce] += random.NextByteRange(1, 2);

								if (req_money.IsNotNull())
								{
									req_money.amount *= 1.08f;
								}
							}
						}
						else if (traits.HasAny(Character.Traits.Law))
						{
							if (random.NextBool(location_data.statistics.urbanization * location_data.statistics.wealth * location_data.statistics.education))
							{
								if (flags.HasNone(ApplyOriginFlags.Skip_Name)) name = "JUDr. " + name;

								experience[Experience.Type.Law] += random.NextByteRange(4, 9);
								experience[Experience.Type.Intellect] += random.NextByteRange(2, 4);
								experience[Experience.Type.Charisma] += random.NextByteRange(3, 6);
								experience[Experience.Type.Commerce] += random.NextByteRange(1, 4);
								experience[Experience.Type.Leadership] += random.NextByteRange(2, 5);

								if (req_money.IsNotNull())
								{
									req_money.amount *= 1.30f;
								}
							}
						}
						else if (traits.HasAny(Character.Traits.Engineer | Character.Traits.Construction))
						{
							if (random.NextBool(location_data.statistics.industrialization * location_data.statistics.education))
							{
								if (flags.HasNone(ApplyOriginFlags.Skip_Name)) name = "Ing. " + name;

								experience[Experience.Type.Intellect] += random.NextByteRange(2, 6);
								experience[Experience.Type.Engineering] += random.NextByteRange(3, 6);
								experience[Experience.Type.Commerce] += random.NextByteRange(1, 2);
								experience[Experience.Type.Leadership] += random.NextByteRange(2, 6);
								experience[Experience.Type.Metallurgy] += random.NextByteRange(2, 6);
								experience[Experience.Type.Construction] += random.NextByteRange(2, 6);
								experience[Experience.Type.Explosives] += random.NextByteRange(0, 6);
								experience[Experience.Type.Masonry] += random.NextByteRange(0, 4);
								experience[Experience.Type.Arcanology] += random.NextByteRange(0, 5);

								if (req_money.IsNotNull())
								{
									req_money.amount *= 1.30f;
								}
							}
							else if (random.NextBool(location_data.statistics.industrialization * location_data.statistics.urbanization * location_data.statistics.education))
							{
								if (flags.HasNone(ApplyOriginFlags.Skip_Name)) name = "RTDr. " + name;

								experience[Experience.Type.Intellect] += random.NextByteRange(2, 6);
								experience[Experience.Type.Engineering] += random.NextByteRange(4, 9);
								experience[Experience.Type.Metallurgy] += random.NextByteRange(5, 9);
								experience[Experience.Type.Explosives] += random.NextByteRange(3, 7);
								experience[Experience.Type.Geology] += random.NextByteRange(2, 6);
								experience[Experience.Type.Alchemistry] += random.NextByteRange(2, 6);
								experience[Experience.Type.Arcanology] += random.NextByteRange(3, 10);

								if (req_money.IsNotNull())
								{
									req_money.amount *= 1.55f;
								}
							}
						}
						else
						{
							if (random.NextBool(location_data.statistics.wealth * 0.60f))
							{
								if (flags.HasNone(ApplyOriginFlags.Skip_Name)) name = "Mgr. " + name;

								experience[Experience.Type.Intellect] += random.NextByteRange(1, 2);
								experience[Experience.Type.Commerce] += random.NextByteRange(1, 3);
								experience[Experience.Type.Charisma] += random.NextByteRange(3, 5);
								experience[Experience.Type.Law] += random.NextByteRange(1, 3);
								experience[Experience.Type.Leadership] += random.NextByteRange(1, 3);

								if (req_money.IsNotNull())
								{
									req_money.amount *= 1.08f;
								}
							}
						}

						if (traits.HasAny(Character.Traits.Scientist))
						{
							if (random.NextBool(0.50f))
							{
								if (flags.HasNone(ApplyOriginFlags.Skip_Name)) name += ", CSc.";

								experience[Experience.Type.Intellect] += random.NextByteRange(1, 3);
								experience[Experience.Type.Alchemistry] += random.NextByteRange(0, 4);
								experience[Experience.Type.Geology] += random.NextByteRange(0, 4);
								experience[Experience.Type.Engineering] += random.NextByteRange(0, 4);
								experience[Experience.Type.Metallurgy] += random.NextByteRange(0, 4);
								experience[Experience.Type.Explosives] += random.NextByteRange(0, 4);
								experience[Experience.Type.Medicine] += random.NextByteRange(0, 4);

								if (req_money.IsNotNull())
								{
									req_money.amount *= 1.12f;
								}
							}
						}
					}
					else if (traits.HasAny(Character.Traits.Educated))
					{
						if (random.NextBool(0.50f))
						{
							if (flags.HasNone(ApplyOriginFlags.Skip_Name)) name = "Bc. " + name;

							experience[Experience.Type.Intellect] += random.NextByteRange(1, 2);
							experience[Experience.Type.Commerce] += random.NextByteRange(1, 3);
							experience[Experience.Type.Charisma] += random.NextByteRange(1, 3);
							experience[Experience.Type.Law] += random.NextByteRange(1, 3);
							experience[Experience.Type.Leadership] += random.NextByteRange(1, 3);

							if (req_money.IsNotNull())
							{
								req_money.amount *= 1.05f;
							}
						}
					}
					else if (traits.HasAll(Character.Traits.Military | Character.Traits.Soldier))
					{
						//if (random.NextBool(0.50f))
						//{
						//	name = "Asp. " + name;

						//	character.experience[Experience.Type.Intellect] += random.NextByteRange(1, 2);
						//	character.experience[Experience.Type.Commerce] += random.NextByteRange(1, 3);
						//	character.experience[Experience.Type.Charisma] += random.NextByteRange(1, 3);
						//	character.experience[Experience.Type.Law] += random.NextByteRange(1, 3);
						//	character.experience[Experience.Type.Leadership] += random.NextByteRange(1, 3);
						//}
						//else if (random.NextBool(0.50f))
						//{
						//	name = "Gefr. " + name;

						//	character.experience[Experience.Type.Intellect] += random.NextByteRange(1, 2);
						//	character.experience[Experience.Type.Commerce] += random.NextByteRange(1, 3);
						//	character.experience[Experience.Type.Charisma] += random.NextByteRange(1, 3);
						//	character.experience[Experience.Type.Law] += random.NextByteRange(1, 3);
						//	character.experience[Experience.Type.Leadership] += random.NextByteRange(1, 3);
						//}
					}
				}

				if (random.NextBool(traits.GetRatio(Character.Traits.Strong | Character.Traits.Brawler | Character.Traits.Outdoor | Character.Traits.Nomad) * 1.20f))
				{
					experience[Experience.Type.Strength] += random.NextByteRange(2, 5);
					experience[Experience.Type.Endurance] += random.NextByteRange(1, 3);
					experience[Experience.Type.Leadership] += random.NextByteRange(1, 2);

					money *= random.NextFloatRange(0.90f, 1.01f);

					if (req_money.IsNotNull())
					{
						req_money.amount *= random.NextFloatRange(0.70f, 1.10f);
					}
				}

				if (random.NextBool(traits.GetRatio(Character.Traits.Laborer | Character.Traits.Lower_Class | Character.Traits.Miner | Character.Traits.Construction) * 0.90f))
				{
					experience[Experience.Type.Strength] += random.NextByteRange(1, 2);
					experience[Experience.Type.Endurance] += random.NextByteRange(1, 3);
					experience[Experience.Type.Construction] += random.NextByteRange(0, 2);
					experience[Experience.Type.Clubs] += random.NextByteRange(0, 2);
					experience[Experience.Type.Mining] += random.NextByteRange(0, 2);
					money *= random.NextFloatRange(0.60f, 1.10f);

					if (req_money.IsNotNull())
					{
						req_money.amount *= random.NextFloatRange(0.70f, 1.10f);
					}
				}

				if (random.NextBool(traits.GetRatio(Character.Traits.Insane | Character.Traits.Terrorist | Character.Traits.Fanatic | Character.Traits.Evil | Character.Traits.Engineer | Character.Traits.Criminal) * 0.75f))
				{
					experience[Experience.Type.Explosives] += random.NextByteRange(0, 8);
					experience[Experience.Type.Arcanology] += random.NextByteRange(0, 5);
					experience[Experience.Type.Animals] += random.NextByteRange(0, 5);
					experience[Experience.Type.Medicine] += random.NextByteRange(0, 5);
					money *= random.NextFloatRange(0.10f, 1.40f);

					if (req_money.IsNotNull())
					{
						req_money.amount *= random.NextFloatRange(0.10f, 2.10f);
					}
				}

				if (random.NextBool(traits.GetRatio(Character.Traits.Politician | Character.Traits.Social | Character.Traits.Elite_Class | Character.Traits.Nobility) * 0.75f))
				{
					experience[Experience.Type.Leadership] += random.NextByteRange(0, 6);
					experience[Experience.Type.Charisma] += random.NextByteRange(0, 6);
					experience[Experience.Type.Intellect] += random.NextByteRange(0, 3);
					experience[Experience.Type.Law] += random.NextByteRange(0, 5);
					money *= random.NextFloatRange(1.10f, 4.90f);

					if (req_money.IsNotNull())
					{
						req_money.amount *= random.NextFloatRange(1.30f, 3.10f);
					}
				}

				if (flags.HasNone(ApplyOriginFlags.Skip_Hair))
				{
					var age_gray = random.NextFloatRange(Maths.Lerp(species.lifecycle.age_mature, species.lifecycle.age_elder, 0.50f), species.lifecycle.age_elder);
					var gray_lerp = 1.00f - Maths.InvLerp(age_gray, age_gray + random.NextFloatRange(5.00f, 15.00f), age).Clamp01();
					character_data.hair_color = species.hair_colors.GetRandom(ref random).WithAlphaMult(gray_lerp);
				}

				switch (gender)
				{
					case Organic.Gender.Male:
					{
						if (character_data.prefab.id == 0) character_data.prefab = species.prefab_male;
						if (character_data.sprite_head.texture.id == 0) character_data.sprite_head = species.sprite_head_male;

						//App.WriteLine($"{(hairs.Where(x => x.data.gender == gender && x.data.species == h_species && x.data.flags.HasAll(IHair.Flags.Hair) && (x.data.character_flags == Character.Traits.None || x.data.character_flags.HasAny(character_flags_tmp)) && !x.data.character_flags_exclude.HasAny(character_flags_tmp))).ToArray().GetRandom(ref random)}");

						if (flags.HasNone(ApplyOriginFlags.Skip_Hair))
						{
							// TODO: shitcode (GC, perf)
							var hairs = IHair.Database.GetAssets(Asset.Scope.Global);
							character_data.hair = hairs.Where(x => x.data.gender == gender && x.data.species == h_species && x.data.flags.HasAny(IHair.Flags.Hair) && (x.data.character_flags.IsEmpty() || x.data.character_flags.HasAny(traits)) && x.data.character_flags_exclude.HasNone(traits)).ToArray().GetRandom(ref random);
							if (random.NextBool(Maths.InvLerp(Maths.Lerp(species.lifecycle.age_adolescent, species.lifecycle.age_mature, 0.50f), Maths.Lerp(species.lifecycle.age_mature, species.lifecycle.age_elder, 0.20f), age).Clamp01()))
							{
								character_data.beard = hairs.Where(x => x.data.gender == gender && x.data.species == h_species && x.data.flags.HasAny(IHair.Flags.Beard) && (x.data.character_flags.IsEmpty() || x.data.character_flags.HasAny(traits)) && x.data.character_flags_exclude.HasNone(traits)).ToArray().GetRandom(ref random);
							}
						}
					}
					break;

					case Organic.Gender.Female:
					{
						if (character_data.prefab.id == 0) character_data.prefab = species.prefab_female;
						if (character_data.sprite_head.texture.id == 0) character_data.sprite_head = species.sprite_head_female;

						if (flags.HasNone(ApplyOriginFlags.Skip_Hair))
						{
							// TODO: shitcode (GC, perf)
							var hairs = IHair.Database.GetAssets(Asset.Scope.Global);
							character_data.hair = hairs.Where(x => x.data.gender == gender && x.data.species == h_species && x.data.flags.HasAny(IHair.Flags.Hair) && (x.data.character_flags.IsEmpty() || x.data.character_flags.HasAny(traits)) && x.data.character_flags_exclude.HasNone(traits)).ToArray().GetRandom(ref random);
						}
					}
					break;
				}

				if (req_money.IsNotNull())
				{
					req_money.amount = Money.ToBataPrice(req_money.amount);
				}

				money = Maths.Floor(Money.ToBataPrice(money));

				character_data.name = name;
				character_data.kits = new IKit.Handle[8];
				character_data.gender = gender;
				character_data.traits = traits;
				character_data.money = money;
				character_data.experience = experience;
				character_data.age = age;

				return true;
			}

			return false;
		}

#endif


		[IRelation.Data(Net.SendType.Reliable, IRelation.Flags.Acyclic | IRelation.Flags.DontInherit | IRelation.Flags.Traversable, IComponent.Scope.Region)]
		public partial struct Link(): IRelation
		{
			public float test;
		}

		//[ISystem.Event<ColorBGRA.SetColorEvent>(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global)]
		//public static void OnSetColor(ISystem.Info.Common info, ref Region.Data.Common region_common, Entity entity, ref ColorBGRA.SetColorEvent ev,
		//[Source.Owned] in Transform.Data transform, [Source.Owned] ref Animated.Renderer.Data renderer)
		//{

		//}

		[IEvent.Data]
		public struct BindEvent(): IEvent
		{
			public Entity? ent_home;
			public Vec2f pos_home;

			public AI.Stance? ai_stance;
			public AI.Behavior.Type? ai_type;

			public AddRemFlags<AI.Behavior.CombatFlags> combat_flags;
			public AddRemFlags<AI.Behavior.MovementFlags> movement_flags;
		}

		public const bool debug_log_events = false;

		//[ISystem.EarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region, interval: 1.37f), HasTag("dead", false, Source.Modifier.Owned)]
		//public static void OnUpdateBind(ISystem.Info info, Entity entity, [Source.Owned, Pair.First] ref Spawner.Link link, [Source.Parent<Spawner.Link>] in Transform.Data transform_home,
		//[Source.Owned] ref NPC.Data npc, [Source.Owned, Original] ref AI.Behavior behavior, [Source.Owned] in Transform.Data transform)
		//{
		//	behavior.position_home = transform_home.position;
		//}

#if SERVER
		//[ISystem.Event<Spawn.SpawnEvent>(ISystem.Mode.Single, ISystem.Scope.Region, order: 100)]
		//public static void OnSpawnEvent(ISystem.Info info, ref Region.Data region, [Source.Owned] ref Spawn.SpawnEvent ev,
		//[Source.Owned] in Spawner.Data spawner, Entity ent_spawner,
		//[Source.Owned] in Spawn.Data spawn)
		//{
		//	var ev_bind = new Spawner.BindEvent
		//	{
		//		ent_home = ent_spawner,
		//		pos_home = ev.position.GetValueOrDefault(),

		//		ai_stance = spawner.ai_stance,
		//		ai_type = spawner.ai_type,
		//	};

		//	if (Spawner.debug_log_events) App.WriteLine($"Spawner.OnSpawnEvent(); ent_spawner: {ent_spawner}; task: {ev.task_ent_controlled.Status}; h_faction: {ev.h_faction}; h_character: {ev.h_character}; h_player: {ev.h_player}");
		//	ev.task_ent_controlled.AddEvent(ev_bind);
		//}



		[ISystem.Event<Spawner.BindEvent>(ISystem.Mode.Single, ISystem.Scope.Region, order: 10)]
		public static void OnBindEvent(Entity entity, ref Spawner.BindEvent ev,
		[Source.Owned, Original] ref AI.Data ai_original, [Source.Owned, Original] ref AI.Behavior ai_behavior_original)
		{
			if (Spawner.debug_log_events) App.WriteLine($"OnBindEvent(); entity: {entity}; ent_home: {ev.ent_home}", color: App.Color.Green);

			{
				var sync = false;
				sync |= ai_behavior_original.type.TrySet(ev.ai_type);
				sync |= ai_behavior_original.combat_flags.TryAddRemFlags(ev.combat_flags);
				sync |= ai_behavior_original.movement_flags.TryAddRemFlags(ev.movement_flags);
				if (sync) ai_behavior_original.Sync(entity);
			}

			{
				var sync = false;
				sync |= ai_original.stance.TrySet(ev.ai_stance);

				if (ev.ent_home.TryGetValue(out var ent_home))
				{
					sync |= ai_original.ent_home.TrySet(ent_home);
					if (ent_home.IsAlive())
					{
						if (ev.pos_home != default) sync |= ai_behavior_original.position_home.TrySet(ev.pos_home);
						entity.AddRel<Spawner.Link>(ent_home);
					}
				}

				if (sync) ai_original.Sync(entity);
			}
		}

		//[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region, interval: 1.00f), HasTag("initialized", true, Source.Modifier.Owned)]
		//public static void OnUpdateTest(ISystem.Info info, Entity entity, ref Region.Data region, ref XorRandom random,
		//[Source.Owned] in Transform.Data transform, [Source.Owned] ref Spawn.Data spawn, Entity ent_spawn, IComponent.Handle h_spawn, [Source.Owned, Optional] in Faction.Data faction)
		//{
		//	//App.WriteLine($"[{info.FrameOffset}] {ent_spawn}: {spawn.flags}; {h_spawn}");
		//}

		[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region, interval: 0.379f), HasTag("initialized", true, Source.Modifier.Owned)]
		public static void OnUpdate(ISystem.Info info, Entity ent_spawner, ref Region.Data region, ref XorRandom random,
		[Source.Owned] in Transform.Data transform, [Source.Owned] ref Spawner.Data spawner, [Source.Owned] ref Spawn.Data spawn,
		[Source.Owned] ref Dormitory.Data dormitory, [Source.Owned, Optional] in Faction.Data faction)
		{
			if (AI.debug_paused) return;
			if (!Constants.World.enable_npc_spawning) return;

			var time = info.WorldTime;
			if (spawner.t_next_spawn.IsNil())
			{
				spawner.t_next_spawn = time + (random.NextFloatExtra(spawner.interval, spawner.interval_extra) * 0.30f);
			}
			else if (time >= spawner.t_next_spawn)
			{
				var span_characters_spawner = dormitory.GetSpawnerCharacterSpan();
				var num_characters_valid = span_characters_spawner.Length - span_characters_spawner.GetNilCount();
				if (num_characters_valid > 0)
				{
					//var total_count = region.GetTotalTagCount(spawner.filter_tag.ToString(), "dead");
					//var total_count = region.GetTotalTagCount(spawner.filter_tag.ToString()); // TODO: currently has "dead" exclude disabled due to a Flecs bug with tags and term iterators
					//																		  //App.WriteLine($"{total_count}");

					//var total_count = region.GetLinkedCount(entity);

					var total_alive_count = region.GetRelatedCount<Spawner.Link>(ent_spawner, "dead", false);
					if (Spawner.debug_log_events) App.WriteLine($"[{faction.id.GetIdentifier()}] {ent_spawner.GetFullName()}: {total_alive_count}/{spawner.max_count}");

					if ((uint)total_alive_count < spawner.max_count)
					{
						var num_characters_rem = Maths.Min(((uint)spawner.max_count - (uint)total_alive_count), 4u);
						var num_characters_max = Maths.Min(num_characters_rem, (uint)num_characters_valid);
						var num_characters = Maths.Min(random.NextUIntExtra(spawner.group_size, spawner.group_size_extra), num_characters_max);
						if (num_characters != 0u)
						{
							var h_faction = faction.id;
							var pos_base = transform.LocalToWorld(spawn.spawn_offset);

							var span_characters = FixedArray.CreateSpan4<(int index, ICharacter.Handle h_character)>(out var buffer_characters, num_characters);
							span_characters = span_characters_spawner.GetSpawnableCharacters(span: span_characters,
								h_faction: h_faction,
								h_faction_spawn: h_faction,
								h_player: default,
								spawn_flags: Spawn.Flags.None);

							for (var i = 0; i < span_characters.Length; i++)
							{
								var (index, h_character) = span_characters[i];

								scoped var ev = new Spawn.SpawnEvent()
								{
									position = pos_base + random.NextUnitVector2(1.00f),
									h_faction = h_faction,
									h_character = h_character
								};
								ent_spawner.TriggerEvent(data: ref ev, h_component: IComponent.Handle.FromComponent<Spawn.Data>());

								// no characters available
								if (ev.h_character == default)
								{
									break;
								}

								var ev_bind = new Spawner.BindEvent
								{
									ent_home = ent_spawner,
									pos_home = ev.position ?? pos_base, //.GetValueOrDefault(),

									ai_stance = spawner.ai_stance,
									ai_type = spawner.ai_type,
								};

								if (Spawner.debug_log_events) App.WriteLine($"Spawner.OnSpawnEvent(); ent_spawner: {ent_spawner}; task: {ev.task_ent_controlled.Status}; h_faction: {ev.h_faction}; h_character: {ev.h_character}; h_player: {ev.h_player}");
								ev.task_ent_controlled.AddEvent(ev_bind);

								total_alive_count++;

								//var ent_spawner = entity;
								//region.SpawnPrefab(spawner.prefab, transform.position + random.NextVector2(1.00f), faction_id: faction_id).ContinueWith((ent) =>
								//{
								//	var ev = new SpawnEvent()
								//	{
								//		ent_target = ent
								//	};

								//	ent_spawner.Notify(ref ev);
								//});
							}
						}
					}

					var delay = total_alive_count * spawner.interval_count_modifier;
					spawner.t_next_spawn = time + Maths.Max(random.NextFloatExtra(spawner.interval + delay, spawner.interval_extra), 0.00f);
				}
				else
				{
					spawner.t_next_spawn = time + random.NextFloatExtra(spawner.interval, spawner.interval_extra);
				}
			}
		}
#endif
	}
}
