using Keg.Engine.Game;
using Keg.Extensions;
using TC2.Base.Components;

namespace TC2.Siege
{
	public static partial class Siege
	{
		[IComponent.Data(Net.SendType.Unreliable)]
		public partial struct Coordinator(): IComponent
		{
			[Flags]
			public enum Flags: uint
			{
				None = 0,

				//Ready = 1 << 0
			}

			public enum Status: uint
			{
				Undefined = 0,

				[Asset.Ignore] Waiting,
				[Asset.Ignore] Dispatching,
			}

			[Asset.Ignore] public int last_wave;

			[Asset.Ignore] public int wave_size;
			[Asset.Ignore] public int wave_size_rem;

			public int pref_size_assault = 6;
			public int pref_size_defense = 4;
			public int pref_size_support = 6;
			public int pref_size_artillery = 3;

			[Asset.Ignore] public int num_spawned_total;

			//public float wave_interval = 60.00f;

			public Siege.Coordinator.Flags flags;
			public Siege.Coordinator.Status status;

			//[Save.Ignore, Net.Ignore] public Vector2 pos_frontline;
			//[Save.Ignore, Net.Ignore] public Vector2 pos_support;
			//[Save.Ignore, Net.Ignore] public Vector2 pos_obstacle;
			//[Save.Ignore, Net.Ignore] public Vector2 pos_goal;
			//[Save.Ignore, Net.Ignore] public Vector2 pos_origin;

			//[Save.Ignore, Net.Ignore] public EntRef<Transform.Data> ref_target;

			[Save.Ignore, Net.Ignore] public EntRef<Squad.Data> ref_squad_assault;
			[Save.Ignore, Net.Ignore] public EntRef<Squad.Data> ref_squad_support;
			[Save.Ignore, Net.Ignore] public EntRef<Squad.Data> ref_squad_defense;
			[Save.Ignore, Net.Ignore] public EntRef<Squad.Data> ref_squad_artillery;

			//[Save.Ignore, Net.Ignore] public float score_

			[Save.Ignore, Net.Ignore] public float next_dispatch;
			//[Save.Ignore, Net.Ignore] public float next_order;
			//[Save.Ignore, Net.Ignore] public float next_spawn;
			//[Save.Ignore, Net.Ignore] public float next_search;
			[Save.Ignore, Net.Ignore] public float next_buy;
			//[Save.Ignore, Net.Ignore] public float next_wave;
		}

		public partial struct DEV_ChangeTeamRPC: Net.ISRPC<Siege.Gamemode.Singleton>
		{
			public IFaction.Handle h_faction;

#if SERVER
			public void Invoke(ref NetConnection conn, ref Siege.Gamemode.Singleton data)
			{
				Assert.IsDevMode();
				Assert.IsValid(this.h_faction);

				var h_faction_player = conn.GetFactionHandle();

				Assert.Check(this.h_faction != h_faction_player);
				Assert.Check(this.h_faction == data.h_faction_attackers || this.h_faction == data.h_faction_defenders);

				conn.SetFaction(this.h_faction, entity_only_same_faction: true);
			}
#endif
		}

		public partial struct DEV_ExecuteUnitRPC: Net.IRPC<Dormitory.Data>
		{
			public ICharacter.Handle h_character;

#if SERVER
			public void Invoke(Net.IRPC.Context context, ref Dormitory.Data data)
			{
				Assert.IsValid(this.h_character);

				ref var region = ref context.GetRegion();
				Assert.IsNotNull(ref region);

				var h_faction_dormitory = context.entity.GetFactionHandle();
				var h_faction_player = context.connection.GetFactionHandle();
				Assert.IsEqual(h_faction_dormitory, h_faction_player);

				var characters_span = data.GetCharacterSpan();
				if (Assert.Check(characters_span.TryRemove(this.h_character)))
				{
					data.Sync(context.entity);

					ref var character_data = ref this.h_character.GetData();
					if (character_data.IsNotNull())
					{
						ref var species_data = ref character_data.species.GetData();
						if (species_data.IsNotNull())
						{
							Sound.Play(region: ref region,
								sound: (character_data.gender == Organic.Gender.Male ? species_data.sounds_death_male : species_data.sounds_death_female).GetRandom(ref region.GetRandom()),
								world_position: context.entity.GetPosition());
						}
					}

					this.h_character.Die(region.GetID());
				}
			}
#endif
		}

		public partial struct DEV_SpawnUnitRPC: Net.IRPC<Dormitory.Data>
		{
			public ICharacter.Handle h_character;
			public FixedArray4<IKit.Handle> kits;
			public Entity ent_squad;

#if SERVER
			public void Invoke(Net.IRPC.Context context, ref Dormitory.Data data)
			{
				//throw new NotImplementedException();

				//ref var region = ref context.GetRegion();
				//ref var player = ref connection.GetPlayer();

				//var characters = data.GetCharacterSpan();
				//var ts = Timestamp.Now();
				//characters.Compact();
				//var ts_elapsed = ts.GetMilliseconds();
				//App.WriteLine($"Compacted in {ts_elapsed:0.00000} ms");
				//data.Sync(entity, true);

				ref var region = ref context.GetRegion();
				Assert.IsNotNull(ref region);

				var h_faction_dormitory = context.entity.GetFactionHandle();
				var h_faction_player = context.connection.GetFactionHandle();
				Assert.IsEqual(h_faction_dormitory, h_faction_player);

				ref var character_data = ref this.h_character.GetData();
				Assert.IsNotNull(ref character_data);

				var h_faction_squad = this.ent_squad.GetFactionHandle();
				var h_faction_character = character_data.faction;

				// TODO: update the checks
				if (h_faction_dormitory == h_faction_player && h_faction_squad == h_faction_player && (h_faction_character == 0 || h_faction_character == h_faction_player))
				{
					SpawnUnit(region: ref region, random: ref region.GetRandom(), ent_squad: this.ent_squad,
						ent_dormitory: context.entity, dormitory: ref data, kits: this.kits.AsSpan(),
						h_character: this.h_character, h_faction: h_faction_player);

					//coordinator.num_spawned_total++;
				}
			}
#endif
		}

		public partial struct DEV_BuyUnitRPC: Net.IRPC<Dormitory.Data>
		{
			public IOrigin.Handle h_origin;

#if SERVER
			public void Invoke(Net.IRPC.Context context, ref Dormitory.Data data)
			{
				Assert.IsValid(this.h_origin);

				ref var region = ref context.GetRegion();
				Assert.IsNotNull(ref region);

				var h_faction = context.entity.GetFactionHandle();
				Assert.IsEqual(h_faction, context.connection.GetFactionHandle());

				BuyUnit(region: ref region, random: ref region.GetRandom(), ent_dormitory: context.entity, dormitory: ref data, h_origin: this.h_origin, h_faction: h_faction);
			}
#endif
		}

#if SERVER
		public static void SetSquadOrder(ref Region.Data region, OwnedComponent<Squad.Data> oc_squad, Vector2 target_pos, Entity ent_target = default)
		{
			if (region.TryGetAnyValidPathPos(ref target_pos))
			{
				Squad.SetOrder(region: ref region, random: ref region.GetRandom(),
					oc_squad: oc_squad,
					position_a: target_pos, position_b: target_pos,
					ent_target: ent_target, type: Commandable.OrderType.Attack);
			}
			//ref var squad = ref oc_squad.data;
			//if (squad.IsNotNull())
			//{
			//	//var positions_span = squad.positions.AsSpan();

			//	//var pos_b = squad.position_a ?? target_pos;

			//	////if () // && Vector2.DistanceSquared(pos_a, pos_b) > (6 * 6))
			//	//{
			//	//	//region.Repath(pos_a: target_pos,
			//	//	//	pos_b: pos_b,
			//	//	//	positions: ref positions_span,
			//	//	//	result: out var repath_result,
			//	//	//	air: false,
			//	//	//	include: Terrain.PathFlags.Ground,
			//	//	//	require: Terrain.PathFlags.Ground,
			//	//	//	exclude: Terrain.PathFlags.Solid | Terrain.PathFlags.Ladder | Terrain.PathFlags.Wall,
			//	//	//	max_depth: 8);
			//	//}
			//	////else
			//	////{
			//	////	region.RepathWander(pos_a, ref positions_span, out var repath_result, air: false, include: Terrain.PathFlags.Ground, require: Terrain.PathFlags.Ground, exclude: Terrain.PathFlags.Solid | Terrain.PathFlags.Ladder | Terrain.PathFlags.Wall, max_depth: 8);
			//	////}

			//	//squad.order_type = Commandable.OrderType.Attack;

			//	//squad.position_a = target_pos;
			//	//squad.position_b = target_pos;

			//	//squad.ent_target = ent_target;
			//	//squad.t_last_order = region.GetWorldTime();

			//	//squad.Sync(oc_squad.entity, true);
			//}

		}

		public static void SpawnUnit(ref Region.Data region, ref XorRandom random,
		Entity ent_squad, Entity ent_dormitory,
		ref Dormitory.Data dormitory, Span<IKit.Handle> kits,
		ICharacter.Handle h_character, IFaction.Handle h_faction = default)
		{
			ref var transform = ref ent_dormitory.GetComponent<Transform.Data>();
			if (transform.IsNotNull())
			{
				//if (Dormitory.TryGenerateKits(ref data, ref random, this.h_character))
				//{
				//	App.WriteLine("ok");
				//}

				Dormitory.SpawnCharacterWithKits(region: ref region, position: transform.position, ent_dormitory: ent_dormitory,
				h_character: h_character, kits: kits, h_faction: h_faction, flags: Spawn.SpawnEvent.Flags.None).ContinueWith((ent_unit) =>
				{
					//App.WriteValue((h_character, ent_squad, ent_dormitory, ent_unit, ent_unit.GetName()));

					ent_unit.AddRel<Squad.Relation>(ent_squad);

					ref var bounty_new = ref ent_unit.GetOrAddComponent<Siege.Bounty.Data>(sync: false, ignore_mask: true);
					if (bounty_new.IsNotNull())
					{
						//bounty_new.rewards

						//bounty_new = bounty;
						//App.WriteLine($"add bounty {bounty_new.rewards[0].type} {bounty_new.rewards[0].amount}");
					}

					foreach (var h_inventory in ent_unit.GetInventories())
					{
						h_inventory.Flags |= Inventory.Flags.Unlimited | Inventory.Flags.No_Drop;
					}
				});
			}
		}

		public static void BuyUnit(ref Region.Data region, ref XorRandom random,
		Entity ent_dormitory, ref Dormitory.Data dormitory,
		IOrigin.Handle h_origin, IFaction.Handle h_faction = default)
		{
			var span_characters = dormitory.GetCharacterSpan();
			if (span_characters.TryGetEmptyIndex(out var index))
			{
				//App.WriteValue(index);
				var h_character = Spawner.CreateCharacter(region: ref region.AsCommon(),
					random: ref random, h_origin: h_origin, h_faction: h_faction, 
					scope: Asset.Scope.Region, asset_flags: Asset.Flags.No_Save | Asset.Flags.Recycle);

				if (Spawner.TryGenerateKits(random: ref random, h_character: h_character))
				{
					//App.WriteLine("ok");
				}

				span_characters[index] = h_character;

				dormitory.Sync(ent_dormitory, true);
			}
		}
#endif

#if SERVER
		[ISystem.Event<Spawn.SpawnEvent>(ISystem.Mode.Single, ISystem.Scope.Region, order: 1000)]
		public static void OnSpawn(ISystem.Info info, Entity entity, [Source.Owned] ref Spawn.SpawnEvent ev,
		[Source.Owned] ref Spawner.Data spawner, 
		[Source.Global] in Siege.Gamemode gm_siege, [Source.Singleton] in Siege.Gamemode.Singleton s_siege)
		{
			var weapon_mult = 1.00f;
			var armor_mult = 1.00f;

			armor_mult = s_siege.difficulty * 0.04f;

			//App.WriteLine($"spawn event {data.ent_target}");
			//SetKoboldLoadout(data.ent_target, weapon_mult: weapon_mult, armor_mult: armor_mult);

			App.WriteLine("TODO: OnSpawn - NoSelect");
			//ref var commandable = ref data.ent_target.GetComponent<Commandable.Data>();
			//if (!commandable.IsNull())
			//{
			//	commandable.flags |= Commandable.Data.Flags.No_Select;

			//	//App.WriteLine(commandable.flags);
			//}
		}

		//[Obsolete]
		//public static bool TryFindTarget(ref Region.Data region, Entity ent_coordinator, IFaction.Handle faction,
		//Vector2 position_src, out Entity ent_target, out Vector2 position_target)
		//{
		//	var arg = new FindTargetArgs(ent_coordinator, faction.id, position_src, default, default, float.MaxValue, default);

		//	foreach (ref var row in region.IterateQuery<Siege.GetAllTargetsQuery>())
		//	{
		//		row.Run((ISystem.Info info, Entity entity, in Siege.Target.Data target, in Transform.Data transform, in Faction.Data faction) =>
		//		{
		//			var dist_sq = Vector2.DistanceSquared(transform.position, arg.position);
		//			if ((faction.id == 0 || faction.id != arg.faction_id) && dist_sq < arg.target_dist_nearest_sq)
		//			{
		//				arg.ent_target = entity;
		//				arg.target_dist_nearest_sq = dist_sq;
		//				arg.target_position = transform.position;

		//				//if (arg.ent_root.id == 0)
		//				//{
		//				//	arg.ent_root = arg.ent_search.GetRoot(Relation.Type.Child);
		//				//}

		//				//var ent_root = entity.GetRoot(Relation.Type.Child);
		//				//if (ent_root != arg.ent_root && ent_root.GetRoot(Relation.Type.Instance) != arg.ent_root)
		//				//{
		//				//	arg.ent_target = entity;
		//				//	arg.target_dist_nearest_sq = dist_sq;
		//				//	arg.target_position = transform.position;
		//				//}
		//			}
		//		});
		//	}

		//	ent_target = arg.ent_target;
		//	position_target = arg.target_position;

		//	return arg.ent_target.IsAlive();
		//}

		[ISystem.Manual(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void FetchAssaultTarget(Entity entity, ref Region.Data region, [ISystem.Parameter] ref FindTargetArgs arg,
		[Source.Owned] in Siege.Target.Data target, [Source.Owned] in Transform.Data transform,
		[Source.Owned, Optional] in Faction.Data faction)
		{
			if (faction.id == arg.faction_id) return;
			if (target.current_capture_progress_norm >= 0.50f) return;

			var dist_sq = Vec2f.DistanceSq(transform.position, arg.position);
			if (dist_sq >= arg.target_dist_nearest_sq) return;

			arg.ent_target = entity;
			arg.target_dist_nearest_sq = dist_sq;
			arg.target_position = transform.position;
		}

		[ISystem.Manual(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void FetchSupportTarget(Entity entity, ref Region.Data region, [ISystem.Parameter] ref FindTargetArgs arg,
		[Source.Owned] in Siege.Target.Data target, [Source.Owned] in Transform.Data transform, [Source.Owned] in Health.Data health,
		[Source.Owned, Optional] in Faction.Data faction)
		{
			if (faction.id == arg.faction_id) return;

			var dist_sq = Vec2f.DistanceSq(transform.position, arg.position);
			if (dist_sq >= arg.target_dist_nearest_sq) return;

			arg.ent_target = entity;
			arg.target_dist_nearest_sq = dist_sq;
			arg.target_position = transform.position;
		}

		[ISystem.Manual(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void FetchDefenseTarget(Entity entity, ref Region.Data region, [ISystem.Parameter] ref FindTargetArgs arg,
		[Source.Owned] in Siege.Target.Data target, [Source.Owned] in Transform.Data transform, [Source.Owned] in Health.Data health,
		[Source.Owned, Optional] in Faction.Data faction)
		{
			//if (faction.id == arg.faction_id) return;
			if (target.current_capture_progress_norm < 0.40f) return;

			var dist_sq = Vec2f.DistanceSq(transform.position, arg.position);
			if (dist_sq < arg.target_dist_nearest_sq) return;

			arg.ent_target = entity;
			arg.target_dist_nearest_sq = dist_sq;
			arg.target_position = transform.position;
		}

		public static bool TryFindAssaultTarget(ref Region.Data region, Entity ent_coordinator, IFaction.Handle faction,
		Vector2 position_src, out Entity ent_target, out Vector2 position_target)
		{
			var arg = new FindTargetArgs(ent_coordinator, faction.id, position_src, default, default, float.MaxValue, default);

			region.RunSystem(FetchAssaultTarget, ref arg);
			//foreach (ref var row in region.IterateQuery<Siege.GetAllTargetsQuery>())
			//{
			//	row.Run((ISystem.Info info, Entity entity, in Siege.Target.Data target, in Transform.Data transform, in Faction.Data faction) =>
			//	{
			//		var dist_sq = Vector2.DistanceSquared(transform.position, arg.position);
			//		if ((faction.id != arg.faction_id) && dist_sq < arg.target_dist_nearest_sq)
			//		{
			//			arg.ent_target = entity;
			//			arg.target_dist_nearest_sq = dist_sq;
			//			arg.target_position = transform.position;
			//		}
			//	});
			//}

			ent_target = arg.ent_target;
			position_target = arg.target_position;

			return arg.ent_target.IsAlive();
		}

		public static bool TryFindSupportTarget(ref Region.Data region, Entity ent_coordinator, IFaction.Handle faction,
		Vector2 position_src, out Entity ent_target, out Vector2 position_target)
		{
			var arg = new FindTargetArgs(ent_coordinator, faction.id, position_src, default, default, float.MaxValue, default);

			region.RunSystem(FetchSupportTarget, ref arg);
			//foreach (ref var row in region.IterateQuery<Siege.GetAllTargetsQuery>())
			//{
			//	row.Run((ISystem.Info info, Entity entity, in Siege.Target.Data target, in Transform.Data transform, in Faction.Data faction) =>
			//	{
			//		var dist_sq = Vector2.DistanceSquared(transform.position, arg.position);
			//		if ((faction.id != arg.faction_id) && dist_sq < arg.target_dist_nearest_sq)
			//		{
			//			arg.ent_target = entity;
			//			arg.target_dist_nearest_sq = dist_sq;
			//			arg.target_position = transform.position;
			//		}
			//	});
			//}

			ent_target = arg.ent_target;
			position_target = arg.target_position;

			return arg.ent_target.IsAlive();

			//var arg = new FindTargetArgs(ent_coordinator, faction.id, position_src, default, default, float.MaxValue, default);

			//foreach (ref var row in region.IterateQuery<Siege.GetAllTargetsQuery>())
			//{
			//	row.Run((ISystem.Info info, Entity entity, in Siege.Target.Data target, in Transform.Data transform, in Faction.Data faction) =>
			//	{
			//		var dist_sq = Vector2.DistanceSquared(transform.position, arg.position);
			//		if (dist_sq < arg.target_dist_nearest_sq && (faction.id == arg.faction_id ? target.current_capture_progress_norm <= 0.25f : target.current_capture_progress_norm <= 0.50f))
			//		{
			//			arg.ent_target = entity;
			//			arg.target_dist_nearest_sq = dist_sq;
			//			arg.target_position = transform.position;
			//		}
			//	});
			//}

			//ent_target = arg.ent_target;
			//position_target = arg.target_position;

			//return arg.ent_target.IsAlive();
		}

		public static bool TryFindDefenseTarget(ref Region.Data region, Entity ent_coordinator, IFaction.Handle faction,
		Vector2 position_src, out Entity ent_target, out Vector2 position_target)
		{
			var arg = new FindTargetArgs(ent_coordinator, faction.id, position_src, default, default, 0.00f, default);

			region.RunSystem(FetchDefenseTarget, ref arg);
			//foreach (ref var row in region.IterateQuery<Siege.GetAllTargetsQuery>())
			//{
			//	row.Run((ISystem.Info info, Entity entity, in Siege.Target.Data target, in Transform.Data transform, in Faction.Data faction) =>
			//	{
			//		var dist_sq = Vector2.DistanceSquared(transform.position, arg.position);
			//		if ((faction.id != arg.faction_id) && dist_sq < arg.target_dist_nearest_sq)
			//		{
			//			arg.ent_target = entity;
			//			arg.target_dist_nearest_sq = dist_sq;
			//			arg.target_position = transform.position;
			//		}
			//	});
			//}

			ent_target = arg.ent_target;
			position_target = arg.target_position;

			return arg.ent_target.IsAlive();

			//var arg = new FindTargetArgs(ent_coordinator, faction.id, position_src, default, default, float.MaxValue, default);

			//foreach (ref var row in region.IterateQuery<Siege.GetAllTargetsQuery>())
			//{
			//	row.Run((ISystem.Info info, Entity entity, in Siege.Target.Data target, in Transform.Data transform, in Faction.Data faction) =>
			//	{
			//		var dist_sq = Vector2.DistanceSquared(transform.position, arg.position);
			//		if (dist_sq < arg.target_dist_nearest_sq && ((faction.id == 0 && target.current_capture_progress_norm < 1.00f) || (faction.id == arg.faction_id && target.current_capture_progress_norm <= 0.90f)))
			//		{
			//			arg.ent_target = entity;
			//			arg.target_dist_nearest_sq = dist_sq;
			//			arg.target_position = transform.position;
			//		}
			//	});
			//}

			//ent_target = arg.ent_target;
			//position_target = arg.target_position;

			//return arg.ent_target.IsAlive();
		}

		private struct FindNearestSpawnArgs(IFaction.Handle faction_id, Vector2 position_target)
		{
			public IFaction.Handle faction_id = faction_id;

			public Vector2 position_target = position_target;
			public float target_dist_nearest_sq = float.MaxValue;

			public Entity ent_spawn = default;
			public Vector2 position_spawn = default;
		}

		public static bool TryFindNearestSpawn(ref Region.Data region, IFaction.Handle faction,
		Vector2 position_target, out Entity ent_spawn, out Vector2 position_spawn)
		{
			var arg = new FindNearestSpawnArgs(faction, position_target);

			foreach (ref var row in region.IterateQuery<Region.GetSpawnsQuery>())
			{
				row.Run((ISystem.Info info, Entity entity, [Source.Owned] in Spawn.Data spawn, [Source.Owned, Optional] in Nameable.Data nameable, [Source.Owned] in Transform.Data transform, [Source.Owned, Optional] in Faction.Data faction) =>
				{
					var dist_sq = Vector2.DistanceSquared(transform.position, arg.position_target);
					if (faction.id == arg.faction_id && dist_sq < arg.target_dist_nearest_sq)
					{
						arg.ent_spawn = entity;
						arg.target_dist_nearest_sq = dist_sq;
						arg.position_spawn = transform.position;
					}
				});
			}

			ent_spawn = arg.ent_spawn;
			position_spawn = arg.position_spawn;

			return arg.ent_spawn.IsAlive();
		}

		// Helper: set up a squad entity after it has been spawned to reduce duplication in OnUpdate
		private static void SetupSpawnedSquad(Entity ent_squad, Color32BGRA color,
		AI.Behavior.MovementFlags movement_flags, AI.Behavior.CombatFlags combat_flags,
		Utf8String name_utf8, Action<Entity> dg_on_set)
		{
			ref var squad = ref ent_squad.GetComponent<Squad.Data>();
			if (squad.IsNotNull())
			{
				squad.color = color;
				squad.movement_flags = movement_flags;
				squad.combat_flags = combat_flags;
				squad.Sync(ent_squad, true);
			}

			ref var nameable = ref ent_squad.GetComponent<Nameable.Data>();
			if (nameable.IsNotNull())
			{
				nameable.name = name_utf8;
				nameable.Sync(ent_squad, true);
			}

			if (dg_on_set is not null)
			{
				dg_on_set(ent_squad);
			}
		}

		private static void HandleAssaultSquad(ref Region.Data region, ref XorRandom random,
		Entity ent_coordinator, ref Siege.Coordinator coordinator,
		ref Dormitory.Data dormitory, in Transform.Data transform,
		IFaction.Handle h_faction, float time)
		{
			var characters_span = dormitory.GetCharacterSpan();
			ref var squad = ref coordinator.ref_squad_assault.GetComponent();
			if (squad.IsNotNull())
			{
				if (time >= squad.t_next_order)
				{
					if (squad.unit_count >= 2)
					{
						if (true)
						{
							if (Siege.TryFindAssaultTarget(region: ref region,
							ent_coordinator: ent_coordinator,
							faction: h_faction,
							position_src: transform.position, // squad.position_a ?? squad.position_current, //  ?? transform.position,
							ent_target: out var ent_target,
							position_target: out var pos_target))
							{
								Siege.SetSquadOrder(region: ref region,
									oc_squad: new(ref squad, coordinator.ref_squad_assault.entity),
									target_pos: pos_target,
									ent_target: ent_target);

								squad.t_next_order = time + random.NextFloatRange(3.00f, 8.00f);
							}
							else
							{
								squad.t_next_order = time + random.NextFloatRange(5.00f, 10.00f);
							}
						}
						else
						{
							squad.t_next_order = time + random.NextFloatRange(10.00f, 25.00f);
						}
					}
					else
					{
						//Siege.SetSquadOrder(region: ref region,
						//	oc_squad: new(ref squad, coordinator.ref_squad_assault.entity),
						//	target_pos: transform.position,
						//	ent_target: default);

						squad.t_next_order = time + random.NextFloatRange(5.00f, 9.00f);
					}
				}

				var squad_size = squad.unit_count;
				var num = Maths.Clamp(Maths.Min(coordinator.wave_size_rem, random.NextIntExtra(1, 1)), 0, Maths.Max(0, coordinator.pref_size_assault - squad_size));
				for (var i = 0; i < num; i++)
				{
					if (characters_span.TryGetValidIndex(out var character_index))
					{
						Siege.SpawnUnit(region: ref region, random: ref random,
							ent_squad: coordinator.ref_squad_assault.entity,
							ent_dormitory: ent_coordinator,
							dormitory: ref dormitory,
							kits: default,
							h_character: characters_span[character_index],
							h_faction: h_faction);

						coordinator.num_spawned_total++;
						coordinator.wave_size_rem--;
					}
				}
			}
		}

		private static void HandleSupportSquad(ref Region.Data region, ref XorRandom random,
		Entity ent_coordinator, ref Siege.Coordinator coordinator,
		ref Dormitory.Data dormitory, in Transform.Data transform,
		IFaction.Handle h_faction, float time)
		{
			var characters_span = dormitory.GetCharacterSpan();
			ref var squad = ref coordinator.ref_squad_support.GetComponent();
			if (squad.IsNotNull())
			{
				if (time >= squad.t_next_order)
				{
					if (squad.idle_count > squad.unit_count * 0.20f)
					{
						if (Siege.TryFindSupportTarget(region: ref region,
						ent_coordinator: ent_coordinator,
						faction: h_faction,
						position_src: squad.position_a ?? squad.position_current, // transform.position,
						ent_target: out var ent_target,
						position_target: out var pos_target))
						{
							Siege.SetSquadOrder(region: ref region,
								oc_squad: new(ref squad, coordinator.ref_squad_support.entity),
								target_pos: pos_target,
								ent_target: ent_target);
						}
						else
						{
							ref var squad_assault = ref coordinator.ref_squad_assault.GetComponent();
							if (squad_assault.IsNotNull())
							{
								Span<Vector2> span_positions = stackalloc Vector2[4];
								region.RepathHigh(pos_a: squad.position_a ?? squad.position_current, // transform.position,
									pos_b: squad_assault.position_a ?? squad_assault.position_current, //  ?? transform.position,
									positions: ref span_positions,
									result: out var repath_result,
									include: Terrain.PathFlags.Ground | Terrain.PathFlags.Edge | Terrain.PathFlags.Ladder,
									max_depth: 4);

								if (!span_positions.IsEmpty && repath_result != Terrain.RepathResult.Unreachable)
								{
									Siege.SetSquadOrder(region: ref region,
										oc_squad: new(ref squad, coordinator.ref_squad_support.entity),
										target_pos: span_positions.GetLast(),
										ent_target: squad_assault.ent_target);
								}
							}
						}
						squad.t_next_order = time + random.NextFloatRange(3.00f, 5.00f);
					}
					else
					{
						squad.t_next_order = time + random.NextFloatRange(1.00f, 2.00f);
					}
				}

				var squad_size = squad.unit_count;
				var num = Maths.Clamp(Maths.Min(coordinator.wave_size_rem, random.NextIntExtra(1, 1)), 0, Maths.Max(0, coordinator.pref_size_support - squad_size));
				for (var i = 0; i < num; i++)
				{
					if (characters_span.TryGetValidIndex(out var character_index))
					{
						Siege.SpawnUnit(region: ref region,
							random: ref random,
							ent_squad: coordinator.ref_squad_support.entity,
							ent_dormitory: ent_coordinator,
							dormitory: ref dormitory,
							kits: default,
							h_character: characters_span[character_index],
							h_faction: h_faction);

						coordinator.num_spawned_total++;
						coordinator.wave_size_rem--;
					}
				}
			}
		}

		private static void HandleDefenseSquad(ref Region.Data region, ref XorRandom random,
		Entity ent_coordinator, ref Siege.Coordinator coordinator,
		ref Dormitory.Data dormitory, in Transform.Data transform,
		IFaction.Handle h_faction, float time)
		{
			var characters_span = dormitory.GetCharacterSpan();
			ref var squad = ref coordinator.ref_squad_defense.GetComponent();
			if (squad.IsNotNull())
			{
				if ((time - squad.t_last_order) >= 15.00f)
				{
					if (Siege.TryFindDefenseTarget(region: ref region,
					ent_coordinator: ent_coordinator,
					faction: h_faction,
					position_src: transform.position, //squad.position_a ?? squad.position_current, //  ?? transform.position,
					ent_target: out var ent_target,
					position_target: out var pos_target))
					{
						Siege.SetSquadOrder(region: ref region,
							oc_squad: new(ref squad, coordinator.ref_squad_defense.entity),
							target_pos: pos_target,
							ent_target: ent_target);
					}
				}
				var squad_size = squad.unit_count;
				var num = Maths.Clamp(Maths.Min(coordinator.wave_size_rem, random.NextIntExtra(1, 1)), 0, Maths.Max(0, coordinator.pref_size_defense - squad_size));
				for (var i = 0; i < num; i++)
				{
					if (characters_span.TryGetValidIndex(out var character_index))
					{
						Siege.SpawnUnit(region: ref region,
							random: ref random,
							ent_squad: coordinator.ref_squad_defense.entity,
							ent_dormitory: ent_coordinator,
							dormitory: ref dormitory,
							kits: default,
							h_character: characters_span[character_index],
							h_faction: h_faction);

						coordinator.num_spawned_total++;
						coordinator.wave_size_rem--;
					}
				}
			}
		}

		private static void HandleArtillerySquad(ref Region.Data region, ref XorRandom random,
		Entity ent_coordinator, ref Siege.Coordinator coordinator,
		ref Dormitory.Data dormitory, in Transform.Data transform,
		IFaction.Handle h_faction, float time)
		{
			var characters_span = dormitory.GetCharacterSpan();
			ref var squad = ref coordinator.ref_squad_artillery.GetComponent();
			if (squad.IsNotNull())
			{
				//if (time >= squad.t_next_order)
				//{
				//	if (squad.idle_count > squad.unit_count * 0.20f)
				//	{

				//		squad.t_next_order = time + random.NextFloatRange(3.00f, 5.00f);
				//	}
				//	else
				//	{
				//		squad.t_next_order = time + random.NextFloatRange(1.00f, 2.00f);
				//	}
				//}
				//var squad_size = squad.unit_count;
				//var num = Maths.Clamp(random.NextIntRange(1, coordinator.wave_size_rem), 0, Maths.Max(0, coordinator.pref_size_artillery - squad_size));
				//for (var i = 0; i < num; i++)
				//{
				//	if (characters_span.TryGetValidIndex(out var character_index))
				//	{
				//		Siege.SpawnUnit(region: ref region,
				//			random: ref random,
				//			ent_squad: coordinator.ref_squad_artillery.entity,
				//			ent_dormitory: ent_coordinator,
				//			dormitory: ref dormitory,
				//			kits: default,
				//			h_character: characters_span[character_index],
				//			h_faction: h_faction);

				//		coordinator.wave_size_rem--;
				//	}
				//}
			}
		}

		[ISystem.AddFirst(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("initialized", true, Source.Modifier.Owned)]
		public static void OnAdd(ref Region.Data region, Entity entity,
		[Source.Owned] ref Transform.Data transform, [Source.Owned] ref Siege.Coordinator coordinator,
		[Source.Owned] ref Stockpile.Data stockpile, [Source.Owned, Optional] in Faction.Data faction)
		{
			// not really needed anymore, since it can be setup by the Stockpile.Data component itself
			if (!stockpile.h_stockpile.IsValid())
			{
				var identifier = Asset.GenerateRandomIdentifier();
				//App.WriteLine(identifier);

				var stockpile_data = new IStockpile.Data();
				stockpile_data.name = "Stuffpile";
				stockpile_data.items = new Shipment.Item[32];

				var asset = IStockpile.Database.RegisterOrUpdate(identifier: identifier, index: null, scope: Asset.Scope.Region,
					region_id: region.GetID(), data: ref stockpile_data);

				asset.Sync(register: true);

				stockpile.h_stockpile = asset;
				stockpile.Sync(entity, true);
			}

			if (!coordinator.ref_squad_assault.IsValid())
			{
				region.SpawnPrefab("squad", position: transform.position, faction_id: faction.id).ContinueWith((ent_squad) =>
				{
					ent_squad.AddRelation(entity, Relation.Type.Instance, true);

					SetupSpawnedSquad(ent_squad: ent_squad,
						color: 0xffff2e1a,
						movement_flags:
						//AI.Behavior.MovementFlags.Wander |
						AI.Behavior.MovementFlags.Hold |
						AI.Behavior.MovementFlags.Open_Doors,
						combat_flags:
						AI.Behavior.CombatFlags.Attack_Visible |
						AI.Behavior.CombatFlags.Attack_When_Hit |
						AI.Behavior.CombatFlags.Chase |
						AI.Behavior.CombatFlags.Charge |
						AI.Behavior.CombatFlags.Capture |
						//AI.Behavior.CombatFlags.Prefer_Melee |
						AI.Behavior.CombatFlags.Cover |
						AI.Behavior.CombatFlags.Evade,
						//AI.Behavior.CombatFlags.Flee,
						//AI.Behavior.CombatFlags.Form_Groups |
						//AI.Behavior.CombatFlags.Capture,
						name_utf8: "Assault"u8,
						dg_on_set: (ent) =>
						{
							ref var coordinator = ref entity.GetComponent<Siege.Coordinator>();
							if (coordinator.IsNotNull())
							{
								coordinator.ref_squad_assault.Set(ent);
							}
						});
				});
			}

			if (!coordinator.ref_squad_support.IsValid())
			{
				region.SpawnPrefab("squad", position: transform.position, faction_id: faction.id).ContinueWith((ent_squad) =>
				{
					ent_squad.AddRelation(entity, Relation.Type.Instance, true);

					SetupSpawnedSquad(ent_squad: ent_squad,
						color: 0xffffcb00,
						movement_flags:
						//AI.Behavior.MovementFlags.Hold |
						AI.Behavior.MovementFlags.Open_Doors,
						combat_flags:
						AI.Behavior.CombatFlags.Attack_Visible |
						AI.Behavior.CombatFlags.Attack_When_Hit |
						AI.Behavior.CombatFlags.Cover |
						AI.Behavior.CombatFlags.Defend |
						AI.Behavior.CombatFlags.Chase |
						AI.Behavior.CombatFlags.Flee |
						AI.Behavior.CombatFlags.Form_Groups |
						AI.Behavior.CombatFlags.Prefer_Ranged,
						name_utf8: "Support"u8,
						dg_on_set: (ent) =>
						{
							ref var coordinator = ref entity.GetComponent<Siege.Coordinator>();
							if (coordinator.IsNotNull())
							{
								coordinator.ref_squad_support.Set(ent);
							}
						});
				});
			}

			if (!coordinator.ref_squad_defense.IsValid())
			{
				region.SpawnPrefab("squad", position: transform.position, faction_id: faction.id).ContinueWith((ent_squad) =>
				{
					ent_squad.AddRelation(entity, Relation.Type.Instance, true);

					SetupSpawnedSquad(ent_squad: ent_squad,
						color: 0xff1cc29a,
						movement_flags:
						//AI.Behavior.MovementFlags.Hold |
						AI.Behavior.MovementFlags.Wander |
						AI.Behavior.MovementFlags.Open_Doors,
						combat_flags:
						AI.Behavior.CombatFlags.Attack_Visible |
						AI.Behavior.CombatFlags.Attack_When_Hit |
						AI.Behavior.CombatFlags.Cover |
						AI.Behavior.CombatFlags.Defend |
						//AI.Behavior.CombatFlags.Charge |
						AI.Behavior.CombatFlags.Chase |
						AI.Behavior.CombatFlags.Flee,
						//AI.Behavior.CombatFlags.Prefer_Ranged, // |
															   //AI.Behavior.CombatFlags.Evade,
															   //AI.Behavior.CombatFlags.Capture |
															   //AI.Behavior.CombatFlags.Prefer_Melee |
															   //AI.Behavior.CombatFlags.Form_Groups,
						name_utf8: "Defense"u8,
						dg_on_set: (ent) =>
						{
							ref var coordinator = ref entity.GetComponent<Siege.Coordinator>();
							if (coordinator.IsNotNull())
							{
								coordinator.ref_squad_defense.Set(ent);
							}
						});
				});
			}

			if (!coordinator.ref_squad_artillery.IsValid())
			{
				region.SpawnPrefab("squad", position: transform.position, faction_id: faction.id).ContinueWith((ent_squad) =>
				{
					ent_squad.AddRelation(entity, Relation.Type.Instance, true);

					SetupSpawnedSquad(ent_squad: ent_squad,
						color: 0xffffcb00,
						movement_flags:
						AI.Behavior.MovementFlags.Hold |
						AI.Behavior.MovementFlags.Open_Doors,
						combat_flags:
						AI.Behavior.CombatFlags.Attack_When_Hit |
						AI.Behavior.CombatFlags.Cover |
						AI.Behavior.CombatFlags.Flee |
						AI.Behavior.CombatFlags.Prefer_Ranged,
						name_utf8: "Artillery"u8,
						dg_on_set: (ent) =>
						{
							ref var coordinator = ref entity.GetComponent<Siege.Coordinator>();
							if (coordinator.IsNotNull())
							{
								coordinator.ref_squad_artillery.Set(ent);
							}
						});
				});
			}
		}

		[Shitcode]
		[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region, interval: 0.10f)]
		public static void OnUpdate(ref Region.Data region, ref XorRandom random, Entity entity,
		[Source.Owned] ref Transform.Data transform, [Source.Owned] ref Siege.Coordinator coordinator,
		[Source.Owned] ref Dormitory.Data dormitory,
		[Source.Global] in Siege.Gamemode gm_siege, [Source.Singleton] in Siege.Gamemode.Singleton s_siege,
		[Source.Owned, Optional] in Faction.Data faction)
		{
			if (Constants.World.enable_npc_spawning && Constants.World.enable_ai && s_siege.status == Gamemode.Status.Running && s_siege.flags.HasAny(Siege.Gamemode.Flags.Active))
			{
				var match_time = s_siege.t_match_elapsed;
				var time = region.GetWorldTime();

				//App.WriteValue(entity);

				if (s_siege.wave_current != coordinator.last_wave)
				{
					coordinator.last_wave = s_siege.wave_current;

					//coordinator.next_wave = time + coordinator.wave_interval + Maths.Clamp(difficulty * 10.00f, 0.00f, 120.00f);
					coordinator.wave_size = (int)Maths.Clamp(gm_siege.wave_size_base + (Maths.Floor(Maths.Pow(s_siege.difficulty, 0.80f)) * gm_siege.wave_size_mult), 0, gm_siege.wave_size_max);
					coordinator.wave_size_rem = coordinator.wave_size;

					coordinator.status = Coordinator.Status.Dispatching;

					//Notification.Push(ref region, $"Group of {coordinator.wave_size} kobolds approaching from the {((transform.position.X / region.GetTerrain().GetWidth()) < 0.50f ? "west" : "east")}!", Color32BGRA.Yellow, lifetime: 10.00f, "ui.alert.02", volume: 0.60f, pitch: 0.75f);
					//Notification.Push(ref region, $"Group of {coordinator.wave_size} kobolds approaching from the {((transform.position.X / region.GetTerrain().GetWidth()) < 0.50f ? "west" : "east")}!", Color32BGRA.Red, lifetime: 10.00f);
					Notification.Push(ref region, $"Group of {coordinator.wave_size} kobolds approaching from the {((transform.position.X / region.GetTerrain().GetWidth()) < 0.50f ? "west" : "east")}!", Color32BGRA.Red, lifetime: 10.00f, send_type: Net.SendType.Reliable);

					//InitDispatcher(region: ref region, entity: entity, 
					//	transform: in transform, coordinator: in coordinator, 
					//	stockpile: ref stockpile, h_faction: faction.id);
				}

				if (s_siege.flags.HasNone(Siege.Gamemode.Flags.No_Dispatcher))
				{
					if (match_time >= coordinator.next_buy)
					{
						var characters_span = dormitory.GetCharacterSpan();
						var characters_empty_count = characters_span.GetEmptyCount();

						if (characters_empty_count > 0)
						{
							var h_species_kobold = new ISpecies.Handle("kobold");

							//Span<IOrigin.Handle> origins = stackalloc IOrigin.Handle[16];

							var origins = FixedArray.CreateSpanList16<IOrigin.Handle>(out var buffer);
							IOrigin.Database.GetHandlesFiltered(ref origins, h_species_kobold, (in x, h_species) =>
							{
								return x.data.species == h_species;
							});

							for (var i = 0; i < Maths.Min(3, characters_empty_count); i++)
							{
								Siege.BuyUnit(region: ref region, random: ref random, ent_dormitory: entity, dormitory: ref dormitory,
								h_origin: origins.GetRandom(ref random), h_faction: faction.id);
							}
						}

						coordinator.next_buy = match_time + random.NextFloatRange(5.00f, 15.00f);
					}
				}

				switch (coordinator.status)
				{
					case Coordinator.Status.Undefined:
					{
						//coordinator.next_wave = time + 60.00f;
						coordinator.status = Coordinator.Status.Waiting;
					}
					break;

					case Coordinator.Status.Waiting:
					{

					}
					break;

					case Siege.Coordinator.Status.Dispatching:
					{
						if (true) //(g_siege_state.t_next_wave - match_time) >= 30.00f)
						{
							if (s_siege.flags.HasNone(Siege.Gamemode.Flags.No_Dispatcher))
							{
								if (match_time >= coordinator.next_dispatch)
								{
									coordinator.next_dispatch = match_time + random.NextFloatRange(1.00f, 3.00f);

									//WorldNotification.Push(region: ref region,
									//	message: $"Wave: {coordinator.wave_size - coordinator.wave_size_rem}/{coordinator.wave_size}",
									//	color: Color32BGRA.White,
									//	position: transform.position,
									//	velocity: Vec2f.Zero,
									//	force: Vec2f.Zero,
									//	write_log: false,
									//	lifetime: 2);

									// Dispatch logic split into handlers for readability
									HandleAssaultSquad(region: ref region, random: ref random, ent_coordinator: entity, coordinator: ref coordinator,
										dormitory: ref dormitory, transform: in transform, h_faction: faction.id, time: time);

									HandleSupportSquad(region: ref region, random: ref random, ent_coordinator: entity, coordinator: ref coordinator,
										dormitory: ref dormitory, transform: in transform, h_faction: faction.id, time: time);

									HandleDefenseSquad(region: ref region, random: ref random, ent_coordinator: entity, coordinator: ref coordinator,
										dormitory: ref dormitory, transform: in transform, h_faction: faction.id, time: time);

									HandleArtillerySquad(region: ref region, random: ref random, ent_coordinator: entity, coordinator: ref coordinator,
										dormitory: ref dormitory, transform: in transform, h_faction: faction.id, time: time);
								}
							}
						}
					}
					break;
				}
			}
		}
#endif
	}
}
