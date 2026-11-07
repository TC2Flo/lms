namespace TC2.Base.Components
{
	public static partial class Pathfinding
	{
		[Flags]
		public enum Flags: uint
		{
			None = 0,

			Air = 1 << 0,
			Land = 1 << 1,
			Climber = 1 << 2
		}

		public static bool TryFindNearbyValidPos(
		[AllowNull] ref this Pathfinding.Data pf, 
		[DisallowNull] ref Region.Data region, 
		[DisallowNull] ref Vec2f pos, 
		float max_distance = 8.00f)
		{
			if (pf.IsNull()) goto fail;

			var pf_include = Terrain.PathFlags.None;
			var pf_allow_empty = false;

			if (pf.flags.HasAny(Pathfinding.Flags.Land))
			{
				pf_include |= Terrain.PathFlags.Ground;
			}

			if (pf.flags.HasAny(Pathfinding.Flags.Climber))
			{
				pf_include |= Terrain.PathFlags.Ladder;
			}

			if (pf.flags.HasAny(Pathfinding.Flags.Air))
			{
				pf_include |= Terrain.PathFlags.Background | Terrain.PathFlags.Edge | Terrain.PathFlags.Drop;
				pf_allow_empty = true;
			}

			if (region.TryGetAnyValidPathPos(ref pos.vect, include: pf_include, allow_empty: pf_allow_empty, max_distance: max_distance))
			{
				return true;
			}

			fail:
			return false;
		}

		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region, sync_table_capacity: 64), IComponent.With<Pathfinding.State>]
		public partial struct Data: IComponent
		{
			public Pathfinding.Flags flags;
		}

		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region, sync_table_capacity: 64)]
		public partial struct State: IComponent
		{
			[Net.Segment.A, Save.Ignore] public FixedArray32<Vector2> points_high;
			[Net.Segment.A, Save.Ignore] public int points_high_count;

			[Save.NewLine]
			[Net.Segment.B, Save.Ignore] public FixedArray32<Vector2> points;
			[Net.Segment.B, Save.Ignore] public FixedArray32<uint> indices;
			[Net.Segment.B, Save.Ignore] public int points_count;

			[Save.NewLine]
			[Net.Segment.C, Save.Ignore] public int current_high_index;
			[Net.Segment.C, Save.Ignore] public int visible_high_index;
			[Net.Segment.C, Save.Ignore] public int current_index;
			[Net.Segment.C, Save.Ignore] public int visible_index;
			[Net.Segment.C, Save.Ignore] public Pathfinding.Results last_result;
			[Net.Segment.C, Save.Ignore] public Terrain.RepathResult last_repath_result;
			[Net.Segment.C, Save.Ignore] public Terrain.RepathResult last_repath_result_high;
			[Net.Segment.C, Save.Ignore] public int retry_count;

			[Save.NewLine]
			[Save.Ignore] public float debug_ms;
			[Net.Ignore, Save.Ignore] public float los_timer;
			[Net.Ignore, Save.Ignore] public float last_update_time;
			[Net.Ignore, Save.Ignore] public float last_los_time;
			
			[Net.Ignore, Save.Ignore] public float last_node_time;
			[Net.Ignore, Save.Ignore] public float last_repath_time;
			[Net.Ignore, Save.Ignore] public float last_pos_time;
			[Net.Ignore, Save.Ignore] public float next_jitter_time;

			[Net.Ignore, Save.Ignore, Asset.Ignore] private ulong unused_00;

			[Net.Ignore, Save.Ignore] public Vector2 jitter;
			[Net.Ignore, Save.Ignore] public Vector2 last_pos;
		}

#if CLIENT
		public static void Draw(ref Region.Data.Common region, ref Pathfinding.State pathfinding_state)
		{
			{
				var points = pathfinding_state.points_high;
				for (var i = 0; i < pathfinding_state.points_high_count; i++)
				{
					//var color = new Color32BGRA(0xffff0000);
					//if (i <= pathfinding_state.visible_high_index) color = 0xff00ff00;
					//if (i > pathfinding_state.current_high_index) color = color.WithAlphaMult(0.50f);
					//color = color.WithAlphaMult(0.20f);

					var color = new Color32BGRA(0xffff0000);
					//if (i <= pathfinding_state.visible_index) color = 0xff00ff00;
					//if (i > pathfinding_state.current_index)
					//{
					//	color = color.WithAlphaMult(0.75f);
					//}

					if (i <= pathfinding_state.visible_high_index) color = Color32BGRA.Yellow;
					if (i < pathfinding_state.current_high_index) color = Color32BGRA.Green;

					if (i > 0) GUI.DrawLine(region.WorldToCanvas(points[i - 1]), region.WorldToCanvas(points[i]), color.WithAlphaMult(0.25f), thickness: 6.00f);
					GUI.DrawText($"[{i + 1}]", region.WorldToCanvas(points[i]), color: color);

					GUI.DrawCircleFilled(region.WorldToCanvas(points[i]), radius: 16.00f, color: color with { a = 200 }, segments: 4);
				}
			}

			{
				var points = pathfinding_state.points;
				for (var i = 0; i < pathfinding_state.points_count; i++)
				{
					var color = new Color32BGRA(0xffff0000);
					//if (i <= pathfinding_state.visible_index) color = 0xff00ff00;
					//if (i > pathfinding_state.current_index)
					//{
					//	color = color.WithAlphaMult(0.75f);
					//}

					if (i <= pathfinding_state.visible_index) color = Color32BGRA.Yellow;
					if (i < pathfinding_state.current_index) color = Color32BGRA.Green;


					if (i > 0) GUI.DrawLine(region.WorldToCanvas(points[i - 1]), region.WorldToCanvas(points[i]), color, thickness: 2.00f);
					GUI.DrawText($"[{i + 1}]", region.WorldToCanvas(points[i]), color: color);

					GUI.DrawCircleFilled(region.WorldToCanvas(points[i]), radius: 6.00f, color: color, segments: 4);
				}
			}
		}
#endif

		[Flags]
		public enum Results: uint
		{
			None = 0,

			Done = 1 << 0,
			Unreachable = 1 << 1,
			Waiting = 1 << 2,
			Dirty = 1 << 3,
			TimedOut = 1 << 4,
			Stuck = 1 << 5,
			LOS = 1 << 6,
			In_Air = 1 << 7,
			LOS_Blocked = 1 << 8
		}

		public static void Reset(ref this Pathfinding.State state, bool clear = true)
		{
			if (clear)
			{
				state.points = default;
				state.points_high = default;
			}

			state.points_high_count = 0;
			state.current_high_index = 0;
			state.visible_high_index = 0;

			state.points_count = 0;
			state.current_index = 0;
			state.visible_index = 0;

			state.last_result = default;
			state.last_repath_result = default;
			state.last_repath_result_high = default;

			//App.WriteLine("reset");
			state.retry_count = 0;
			state.jitter = default;
		}

		public static Vector2 GetPath(in this Pathfinding.Data pathfinding, ref Pathfinding.State state, ref Region.Data region, ref XorRandom random, Vector2 current_pos, Vector2 target_pos, Vector2 head_pos, float required_distance, NPC.SelfHints hints, ref Vector2 blocked_los_pos, out Pathfinding.Results results, bool force_repath = false)
		{
			var node_timeout_threshold = 2.00f;

			var ts = Timestamp.Now();
			var reset = force_repath;

			var current_pos_actual = current_pos;

			var repath_low = force_repath;
			var repath_high = force_repath;

			//App.WriteLine(required_distance);

			var target_pos_new = target_pos;

			//var failed = false;

			//App.WriteLine("getpath");

			var can_fly = pathfinding.flags.HasAny(Pathfinding.Flags.Air); // & hints.HasAny(NPC.SelfHints.Can_Fly);
			var can_climb = pathfinding.flags.HasAny(Pathfinding.Flags.Climber) && hints.HasAny(NPC.SelfHints.Can_Climb); // & hints.HasNone(NPC.SelfHints.Is_Injured | NPC.SelfHints.Is_Incapacitated | NPC.SelfHints.Is_Stunned | NPC.SelfHints.In_Vehicle);
			var can_open_doors = hints.HasAny(NPC.SelfHints.Can_Open_Doors | NPC.SelfHints.Is_Vehicle);
			var is_target_air = state.last_result.HasAny(Results.In_Air);
			var time = region.GetWorldTime();

			var node_flags_preferred = Terrain.PathFlags.None;
			node_flags_preferred.AddFlag(Terrain.PathFlags.Ground, pathfinding.flags.HasAny(Pathfinding.Flags.Land));
			node_flags_preferred.AddFlag(Terrain.PathFlags.Ladder, can_climb); // pathfinding.flags.HasAny(Pathfinding.Flags.Climber));

			//var node_flags_exclude = Terrain.PathFlags.Solid | Terrain.PathFlags.Underwater;

			// TODO: setting underwater flag causes very laggy repath spam atm - make it find nearest non-underwater position instead
			var node_flags_exclude = Terrain.PathFlags.Solid | Terrain.PathFlags.Underwater;
			node_flags_exclude.AddFlag(Terrain.PathFlags.Door, !can_open_doors);

			// Reset cached times, since they weren't updating for a while (e.g. when not calling GetPath())
			if ((time - state.last_update_time) >= node_timeout_threshold)
			{
				state.last_node_time = time;
				state.retry_count = 0;
				//state.jitter = default;

				state.Reset(clear: false);

				repath_low = true;
				repath_high = true;

				//App.WriteLine("reset");
			}

			state.last_update_time = time;

			results = default;

			//if (!can_fly && !region.TryGetNearestHighPathPos(ref target_pos_new, Terrain.PathFlags.Ground | Terrain.PathFlags.Ladder))
			if (!can_fly && (!region.TryGetHighPathNode(target_pos_new, out var hi_node_air) || !hi_node_air.Flags.HasAny(node_flags_preferred))) // (ref target_pos_new, Terrain.PathFlags.Ground | Terrain.PathFlags.Ladder))
			{
				is_target_air = true;
				results |= Pathfinding.Results.In_Air;

				//#if SERVER
				//				WorldNotification.Push(ref region, "air", Color32BGRA.Cyan, hi_node_air.WorldPosition);
				//#endif


				//App.WriteLine("AIR");
			}
			else
			{
				//App.WriteLine("fail");
			}


			var dist_sq = Vector2.DistanceSquared(current_pos, target_pos);
			//if (App.IsServer) region.DrawDebugLine(current_pos, target_pos, Color32BGRA.Red, thickness: 4);


			//if (dist_sq <= (required_distance * required_distance))
			//{

			//App.WriteLine(dist_sq);

			//	return target_pos;
			//}

			// If final target is visible, mark as LOS
			//var is_los_target = (dist_sq < (required_distance * required_distance) || ((dist_sq < 16 * 16)) && region.IsInLineOfSight(current_pos, target_pos, 0.00f, mask: Physics.Layer.Solid, exclude: Physics.Layer.Dynamic, query_flags: Physics.QueryFlag.Static));
			//var is_los_target = (dist_sq < (required_distance * required_distance)) || ((dist_sq < 16 * 16 || is_target_air) && region.IsInLineOfSight(current_pos, (current_pos - target_pos).GetNormalized() * 16.00f, 0.00f, mask: Physics.Layer.Solid | Physics.Layer.World, exclude: Physics.Layer.Dynamic, query_flags: Physics.QueryFlag.Static));

			//var is_los_target = dist_sq <= required_distance.Pow2();
			//if (!is_los_target)
			//{
			//	var dir = (target_pos - head_pos).GetNormalized(out var dist);
			//	if ((dist < 16) || (is_target_air && dist < 24))
			//	{
			//		is_los_target = region.IsInLineOfSight(pos_a: head_pos, pos_b: head_pos + (dir * Maths.Min(16.00f, dist)), radius: 0.000f,
			//		mask: Physics.Layer.Solid | Physics.Layer.World,
			//		exclude: Physics.Layer.Dynamic | Physics.Layer.Background | Physics.Layer.Water | Physics.Layer.Creature,
			//		query_flags: Physics.QueryFlag.Static,
			//		skip_inside: false);

			//		//if (App.IsServer) region.DrawDebugLine(head_pos, head_pos + (dir * Maths.Min(16.00f, dist)), is_los_target ? Color32BGRA.Green : Color32BGRA.Red, thickness: 4);
			//	}
			//}

			var is_los_target = false;
			//if (dist_sq <= required_distance.Pow2())
			{
				var dir = (target_pos - current_pos).GetNormalized(out var dist); // TODO: avoid sqrt
				if (dist <= 0.125f) // most likely overlapping eachother
				{
					is_los_target = true;
				}
				else if (dist < 15)
				{
					is_los_target = region.IsInLineOfSight(pos_a: current_pos, pos_b: current_pos + (dir * Maths.Min(16.00f, dist - 0.10f)), out blocked_los_pos, radius: 0.100f,
					layer: Physics.Layer.Solid,
					mask: Physics.Layer.World, // | Physics.Layer.Water,
					exclude: Physics.Layer.Dynamic | Physics.Layer.Background | Physics.Layer.Water | Physics.Layer.Creature,
					//exclude: Physics.Layer.Dynamic | Physics.Layer.Background | Physics.Layer.Creature,
					query_flags: Physics.QueryFlag.Static,
					skip_inside: false, inside: false);

					if (!is_los_target)
					{
						results |= Pathfinding.Results.LOS_Blocked;
					}

					//App.WriteLine(is_los_target);

					if (App.IsServer && AI.debug_always_sync)
					{
						region.DrawDebugLine(current_pos, current_pos + (dir * Maths.Min(16.00f, dist)), is_los_target ? Color32BGRA.Green : Color32BGRA.Red, thickness: 4);
						region.DrawDebugCircle(blocked_los_pos, 0.25f, color: is_los_target ? Color32BGRA.Green : Color32BGRA.Red, filled: true, segments: 4);
					}
				}

			}

			//var is_los_target = (dist_sq < (required_distance * required_distance)) || ((dist_sq < 16 * 16 || (is_target_air && dist_sq < (24 * 24))) && region.IsLOS(current_pos, current_pos + ((target_pos - current_pos).GetNormalized() * 16.00f), allow_inside: false));
			if (is_los_target)
			{
				state.last_los_time = time;
				results |= Pathfinding.Results.LOS;

				//#if SERVER
				//				WorldNotification.Push(ref region, "los pos", Color32BGRA.Cyan, current_pos + ((target_pos - current_pos).GetNormalized() * 16.00f));
				//#endif

				//App.writeli

				//return target_pos;
			}

			//#if SERVER
			//			region.DrawDebugCircle(current_pos, 0.25f, Color32BGRA.Yellow, thickness: 2.00f);
			//			region.DrawDebugCircle(target_pos, 0.25f, Color32BGRA.Magenta, thickness: 2.00f);
			//#endif

			//App.WriteLine($"{dist_sq} < {required_distance * required_distance}");

			// If final target is visible, mark done
			if (is_los_target)
			{
				//App.WriteLine(dist_sq);
				if (dist_sq <= required_distance.Pow2()) // || is_target_air)
				{
					results |= Pathfinding.Results.Done;
				}
			}

			if (Vector2.DistanceSquared(current_pos, state.last_pos) > required_distance.Pow2()) // || (is_target_air && is_los_target))
			{
				state.last_pos = current_pos;
				state.last_pos_time = time;
			}

			var dt_last_los = (time - state.last_los_time);
			var dt_last_repath = (time - state.last_repath_time);
			var dt_last_node = (time - state.last_node_time);
			var dt_last_pos = (time - state.last_pos_time);

			//if (can_fly)
			//{
			//	if (is_los_target)
			//	{

			//	}
			//}

			//repath_low = true;

			// Reached last node
			if (state.current_index >= state.points_count - 1 && state.last_result.HasNone(Results.Stuck | Results.Unreachable)) // || (state.points_count > 1 && Vector2.DistanceSquared(state.points[state.current_index], state.points[state.points_count - 1]) < (required_distance * required_distance))) // || state.visible_index >= state.points_count - 1)
			{
				//if (dt_last_node > 0.50f && dt_last_repath > 0.25f) //state.last_repath_result.HasAll(Terrain.RepathResult.Partial))
				if (dt_last_repath > 0.25f) //state.last_repath_result.HasAll(Terrain.RepathResult.Partial))
				{
					repath_low = true;
				}
				else
				{
					var done = dist_sq <= (required_distance * required_distance) || (state.last_result.HasAny(Pathfinding.Results.Done) && dt_last_los < 0.50f);
					results.SetFlag(Pathfinding.Results.Done, done);
				}

				//App.WriteLine($"{state.last_repath_result} ; {state.last_repath_result_high}");

				if (state.last_repath_result == Terrain.RepathResult.Partial || state.last_repath_result_high == Terrain.RepathResult.Partial || (state.last_repath_result_high == Terrain.RepathResult.Done && state.last_repath_result_high != state.last_repath_result))
				{
					repath_low = true;
					repath_high = true;
				}

				if (state.points_count > 2)
				{
					//state.retry_count = 0;
					//App.WriteLine("retry");
				}
			}

			//App.WriteLine($"{repath_low}; {repath_high}");

			if (state.current_high_index >= state.points_high_count - 1)
			{
				if (state.last_repath_result_high == Terrain.RepathResult.Partial)
				{
					repath_low = true;
					repath_high = true;
				}
			}

			// Not done and stuck on the same node for too long
			if (results.HasNone(Pathfinding.Results.Done))
			{
				if (((state.points_count > 0 || (dt_last_los > 0.50f)) && ((time - state.last_node_time) > node_timeout_threshold))) // || (state.points_count > 0 && Vector2.Distance(target_pos, state.points[state.points_count - 1]) > 4.00f))
				{
					repath_low = true;

					state.retry_count++;
					results.AddFlag(Pathfinding.Results.TimedOut);

					current_pos = region.GetNearestPathPos(head_pos, Terrain.PathFlags.Edge | node_flags_preferred); // + random.NextUnitVector2Range(0.25f, 0.50f);
					dist_sq = Vector2.DistanceSquared(current_pos, target_pos);
				}
			}
			else
			{
				//state.retry_count = 0;
			}

			//App.WriteLine($"{repath_low}; {repath_high}");

			//repath_low = true;

			repath_low |= state.points_count == 0 && dist_sq > (3 * 3) && !is_los_target;
			repath_high |= state.points_high_count == 0 || repath_low;

			var ts_tmp = default(Timestamp);
			var repath_lo_elapsed_ms = 0.000;
			var repath_hi_elapsed_ms = 0.000;
			//var wander_elapsed_ms = 0.000;

			if (repath_high)
			{
				var span_high = state.points_high.AsSpan();

				if (can_fly)
				{
					ts_tmp = Timestamp.Now();
					region.RepathHigh(pos_a: current_pos, pos_b: target_pos, positions: ref span_high, result: out state.last_repath_result_high, air: true, max_depth: 32, exclude: node_flags_exclude);
					repath_hi_elapsed_ms += ts_tmp.GetMilliseconds();
				}
				else
				{
					//var hi_pos = target_pos;
					var hi_pos = current_pos;
					if (!is_target_air && region.TryGetNearestHighPathPos(ref hi_pos, node_flags_preferred))
					{
						ts_tmp = Timestamp.Now();
						region.RepathHigh(pos_a: current_pos, pos_b: target_pos, positions: ref span_high, result: out state.last_repath_result_high, air: false, max_depth: 32, exclude: node_flags_exclude);
						repath_hi_elapsed_ms += ts_tmp.GetMilliseconds();

						ref var pos_end = ref span_high.GetLastOrNull(); // [span_positions.Length - 1];
						if (pos_end.IsNotNull())
						{
							region.TryGetNearestValidPathPos(ref pos_end, include: node_flags_preferred, exclude: node_flags_exclude | Terrain.PathFlags.Edge | Terrain.PathFlags.Drop | Terrain.PathFlags.Underwater, max_distance: 4.00f);
						}

#if SERVER
						//WorldNotification.Push(ref region, "hi_pos1", Color32BGRA.Green, pos_end.OrDefault());
#endif
					}
					else
					{
						is_target_air = true;

						if (region.TryLinecast(world_position_start: target_pos, world_position_end: target_pos + new Vector2(0.00f, 8.00f), radius: 1.00f, result: out var linecast_result, require: Physics.Layer.World, query_flags: Physics.QueryFlag.Static))
						{
							target_pos_new = linecast_result.world_position;

							ts_tmp = Timestamp.Now();
							region.RepathHigh(pos_a: current_pos, pos_b: target_pos_new, positions: ref span_high, result: out state.last_repath_result_high, air: false, max_depth: 16, exclude: node_flags_exclude);
							repath_hi_elapsed_ms += ts_tmp.GetMilliseconds();

							//#if SERVER
							//							WorldNotification.Push(ref region, "hi_pos2", Color32BGRA.Green, target_pos_new);
							//#endif
						}
						else
						{
							ts_tmp = Timestamp.Now();
							region.RepathWanderHigh(pos_a: current_pos, positions: ref span_high, result: out state.last_repath_result_high, air: false, max_depth: 3, exclude: node_flags_exclude);
							repath_hi_elapsed_ms += ts_tmp.GetMilliseconds();
							//wander_elapsed_ms += ts_tmp.GetMilliseconds();

							target_pos_new = span_high[span_high.Length - 1];
							//target_pos_new = linecast_result.world_position;

							//results.SetFlag(Results.Done | Results.LOS, true);
							//span_high = span_high.Slice(0, 2);
							//span_high[0] = current_pos;
							//span_high[1] = state.last_pos;

							//App.WriteLine("hm");
						}
					}
				}

				state.points_high_count = span_high.Length;
				state.current_high_index = 0;
				state.visible_high_index = 0;

				if (state.points_high_count > 0)
				{
					if (!is_target_air) state.points_high[state.points_high_count - 1] = target_pos;
					//App.WriteLine(target_pos);

					for (var i = 0; i < state.points_high_count - 2; i++)
					{
						var dir = (state.points_high[i] - current_pos).GetNormalizedFast();
						state.points_high[i] = Terrain.GetNearestPathPos(ref region, state.points_high[i], include: can_fly ? Terrain.PathFlags.None : Terrain.PathFlags.Ground, exclude: Terrain.PathFlags.Solid | Terrain.PathFlags.Wall); // + (dir * random.NextFloatRange(-1.00f, 0.50f) + random.NextUnitVector2Range(0.25f, 0.80f));
					}
				}
			}

			// Advance visible high nodes
			for (var i = state.current_high_index; i < state.points_high_count - 1; i++)
			{
				//if (i < state.points_high_count - 2)
				//{
				//	var dir = (state.points_high[i] - current_pos).GetNormalizedFast();
				//	state.points_high[i] = Terrain.GetNearestPathPos(ref region, state.points_high[i], include: Terrain.PathFlags.Ground, exclude: Terrain.PathFlags.Solid | Terrain.PathFlags.Edge) + (dir * random.NextFloatRange(-1.00f, 0.50f) + random.NextUnitVector2Range(0.25f, 0.80f));
				//}

				var delta = state.points_high[i] - current_pos;
				var delta_abs = Vector2.Abs(delta);
				var dist = Vector2.Distance(state.points_high[i], current_pos);

				//App.WriteLine(dist);

				//if (dist <= 16.00f)
				//{
				//	state.current_high_index = i + 1;

				//	if (!can_fly)
				//	{
				//		//target_pos = state.points_high[state.current_high_index];
				//		//ref var high_pos = ref state.points_high[state.current_high_index];

				//		//if (region.TryLinecast(high_pos, high_pos + new Vector2(0, 16), 0.00f, out var hit_result_a, mask: Physics.Layer.World, query_flags: Physics.QueryFlag.Static))
				//		//{
				//		//	high_pos = region.GetNearestPathNode(hit_result_a.world_position + (hit_result_a.normal * 0.50f), Terrain.PathFlags.Ground | Terrain.PathFlags.Edge | Terrain.PathFlags.Ladder) + (hit_result_a.normal * 0.50f);
				//		//}
				//	}
				//}

				//var is_los = (dist <= 16) && region.IsInLineOfSight(current_pos, state.points_high[i], 0.00f, layer: Physics.Layer.Solid, mask: Physics.Layer.None, exclude: Physics.Layer.Dynamic, query_flags: Physics.QueryFlag.Static);
				var is_los = (dist <= 8.00f) || ((dist <= 16) && region.IsLOS(head_pos, state.points_high[i], allow_inside: true));
				//App.WriteLine(is_los);

				//#if SERVER
				//				region.DrawDebugLine(current_pos, state.points_high[i], Color32BGRA.Magenta, 8);
				//#endif

				if (is_los)
				{
					state.visible_high_index = i + 1;
					state.current_high_index = i + 1;
					//break;
					//pathfinding.last_node_time = time;			

					//if (!can_fly)
					//{
					//	target_pos = state.points_high[state.current_high_index];
					//	ref var high_pos = ref state.points_high[state.current_high_index];

					//	if (region.TryLinecast(high_pos, high_pos + new Vector2(0, 20), 0.00f, out var hit_result_a, mask: Physics.Layer.World, query_flags: Physics.QueryFlag.Static))
					//	{
					//		high_pos = region.GetNearestPathNode(hit_result_a.world_position + hit_result_a.normal, Terrain.PathFlags.Ground | Terrain.PathFlags.Edge | Terrain.PathFlags.Ladder);
					//	}
					//}
				}
				else
				{
					state.visible_high_index = i;

					break;
				}
			}

			if (!is_target_air && state.last_repath_result_high != Terrain.RepathResult.Unreachable && dist_sq >= 16.00f) // && !(is_los_target && is_target_air))
			{
				if (can_fly)
				{
					target_pos_new = region.GetNearestPathPos(pos: state.points_high[state.current_high_index]);
				}
				else
				{
					target_pos_new = region.GetNearestPathPos(pos: state.points_high[state.current_high_index], include: Terrain.PathFlags.Edge | node_flags_preferred);



					//target_pos = state.points_high[state.current_high_index];
					//var high_pos = state.points_high[state.current_high_index];

					//if (region.TryLinecast(high_pos, high_pos + new Vector2(0, 20), 0.00f, out var hit_result_a, mask: Physics.Layer.World, query_flags: Physics.QueryFlag.Static))
					//{
					//	target_pos_new = region.GetNearestPathNode(hit_result_a.world_position + (hit_result_a.normal * 1.00f), Terrain.PathFlags.Ground | Terrain.PathFlags.Edge | Terrain.PathFlags.Ladder);
					//}
				}
			}

			//App.WriteLine($"{repath_low}; {repath_high}");

			// Repath when there are no nodes
			if (repath_low || repath_high)
			{
				var span_pos = state.points.AsSpan();
				var span_ind = state.indices.AsSpan();

				//var is_los = region.IsInLineOfSight(current_pos, target_pos, 0.00f, mask: Physics.Layer.Solid, query_flags: Physics.QueryFlag.Static);

				if (can_fly)
				{
					if (dist_sq > (32 * 32))
					{
						ts_tmp = Timestamp.Now();
						region.RepathHigh(pos_a: current_pos, pos_b: target_pos, positions: ref span_pos, result: out state.last_repath_result, air: true, max_depth: 32, exclude: node_flags_exclude);
						repath_hi_elapsed_ms += ts_tmp.GetMilliseconds();
					}
					else
					{
						//var is_los = region.IsInLineOfSight(current_pos, target_pos, 0.00f, layer: Physics.Layer.Solid, mask: Physics.Layer.None, exclude: Physics.Layer.Dynamic, query_flags: Physics.QueryFlag.Static);
						var is_los = region.IsLOS(a: current_pos, b: target_pos, allow_inside: true);
						if (is_los)
						{
							span_pos[0] = current_pos;
							//span_pos[1] = target_pos_new;
							span_pos[1] = target_pos;
							span_pos = span_pos.WithLength(2);

							state.last_repath_result = Terrain.RepathResult.Done;
							//state.last_repath_result_high = Terrain.RepathResult.Done;
						}
						else
						{
							//#if SERVER
							//						WorldNotification.Push(ref region, "hi_pos1", Color32BGRA.Green, target_pos_new);
							//#endif

							ts_tmp = Timestamp.Now();
							region.Repath(pos_a: current_pos, pos_b: target_pos_new, positions: ref span_pos, result: out state.last_repath_result, air: true, max_depth: 32, exclude: node_flags_exclude);
							repath_lo_elapsed_ms += ts_tmp.GetMilliseconds();
						}
					}

					//if (is_los)
					//{
					//	span[0] = current_pos;
					//	span[1] = target_pos;
					//	span = span.Slice(0, 2);
					//}
				}
				else
				{
					//if (is_target_air && is_los_target)
					//{
					//	//App.WriteLine("go to done");

					//	goto done;
					//}
					//else 

					if (state.last_repath_result_high == Terrain.RepathResult.Unreachable)
					{
						state.last_repath_result = Terrain.RepathResult.Unreachable;
						span_pos = span_pos.Slice(0, 0);

						//#if SERVER
						//						WorldNotification.Push(ref region, "unreachable", Color32BGRA.Red, target_pos_new);
						//#endif

						//App.WriteLine("unreachable");
					}
					else
					{
						//App.WriteLine($"{repath_low}; {repath_high}; {dist_sq < (16 * 16)}; {target_pos}; {target_pos_new}; {current_pos}");

						current_pos = Terrain.GetNearestPathPos(region: ref region, pos: current_pos, include: Terrain.PathFlags.Edge | node_flags_preferred);
						//region.Repath(current_pos, dist_sq < (16 * 16) ? target_pos : target_pos_new, ref span, out state.last_repath_result, air: false, max_depth: 32);

						var target_pos_repath = (!is_target_air && dist_sq < (16 * 16) ? target_pos : target_pos_new);
						target_pos_repath = Terrain.GetNearestPathPos(region: ref region, pos: target_pos_repath, include: Terrain.PathFlags.Edge | Terrain.PathFlags.Wall | node_flags_preferred, exclude: node_flags_exclude);



						//region.RepathIndices(current_pos, target_pos_repath, ref span_ind, out state.last_repath_result, air: false, max_depth: 32, exclude: node_flags_exclude);
						ts_tmp = Timestamp.Now();
						region.RepathIndices(pos_a: current_pos, pos_b: target_pos_repath, indices: ref span_ind, result: out state.last_repath_result, air: false, max_depth: 32, include: node_flags_preferred, exclude: node_flags_exclude);
						repath_lo_elapsed_ms += ts_tmp.GetMilliseconds();

						//#if SERVER
						//						WorldNotification.Push(ref region, $"RepathIndices {span_ind.Length}", Color32BGRA.Green, target_pos_repath);
						//#endif

						//if (span_ind.Length > 2)
						//{
						//	span_ind = span_ind.WithLength(span_ind.Length - 1);
						//}

						span_pos = span_pos.WithLength(span_ind.Length);




						var path_node_prev = default(Terrain.PathNode);

						//Span<OverlapResult> overlap_results = stackalloc OverlapResult[8];
						for (var i = 0; i < span_ind.Length; i++)
						{
							ref var pos = ref span_pos[i];
							ref var idx = ref span_ind[i];

							if (region.TryGetPathNode(idx, out var node))
							{
								pos = node.WorldPosition;
								if (i == 0) path_node_prev = node;

								if (node.Flags.HasAny(Terrain.PathFlags.Ground))
								{
									if (region.TryLinecast(world_position_start: pos, world_position_end: pos + new Vector2(0, 1), radius: 0.35f, result: out var hit_result, mask: Physics.Layer.World, query_flags: Physics.QueryFlag.Static, skip_inside: false))
									//if (region.TryLinecast(pos, pos, 0.35f, out var hit_result, mask: Physics.Layer.World, query_flags: Physics.QueryFlag.Static, skip_inside: true))
									{
										//if (hit_result_a.alpha <= 0.00f)
										//{
										//	pos = region.GetNearestPathPos(hit_result_a.world_position, include: Terrain.PathFlags.Ground | Terrain.PathFlags.Edge | Terrain.PathFlags.Ladder, exclude: Terrain.PathFlags.Solid);
										//}
										//else
										{
											pos = hit_result.world_position + (hit_result.normal_raw * 1.25f);
										}


#if SERVER
										//WorldNotification.Push(ref region, "floor", Color32BGRA.Yellow, pos);
#endif
									}
								}
								else if (node.Flags.HasAny(Terrain.PathFlags.Edge | Terrain.PathFlags.Ladder | Terrain.PathFlags.Wall))
								{
									var dir = (pos - node.BasePosition).GetNormalizedFast();
									if (region.TryLinecast(world_position_start: pos, world_position_end: pos + (dir * 2), radius: 0.50f, result: out var hit_result, mask: Physics.Layer.World, query_flags: Physics.QueryFlag.Static, skip_inside: false))
									{
										//if (hit_result.alpha <= 0.00f)
										//{
										//	//pos = region.GetNearestPathPos(hit_result.world_position, include: Terrain.PathFlags.Ground | Terrain.PathFlags.Edge | Terrain.PathFlags.Ladder, exclude: Terrain.PathFlags.Solid);
										//}
										//else
										{
											pos = node.BasePosition + (dir * 1.25f);
										}

#if SERVER
										//WorldNotification.Push(ref region, "edge", Color32BGRA.Cyan, pos);
#endif
									}
								}

								//if (node.Flags.HasAny(Terrain.PathFlags.Ground))
								//{
								//	var center_count = 0;
								//	var center = Vector2.Zero;

								//	var overlap_results_tmp = overlap_results;
								//	if (region.TryOverlapPointAll(pos, 3.00f, ref overlap_results_tmp, mask: Physics.Layer.World, query_flags: Physics.QueryFlag.Static))
								//	{
								//		for (var j = 0; j < overlap_results_tmp.Length; j++)
								//		{
								//			ref var overlap_result = ref overlap_results_tmp[j];
								//			center += overlap_result.world_position; // + (overlap_result.gradient * 1.50f);
								//			center_count++;
								//		}
								//	}

								//	if (center_count > 0)
								//	{
								//		center /= center_count;
								//		pos = center;
								//	}
								//}

								if (can_climb)
								{
									if (i > 0)
									{
										ref var pos_prev = ref state.points[i - 1];

										if (path_node_prev.Flags.HasAny(Terrain.PathFlags.Ladder) && !node.Flags.HasAny(Terrain.PathFlags.Ladder) && node.Flags.HasNone(node_flags_exclude))
										{
											var pos_tmp = pos_prev with { Y = pos.Y };
											//pos_prev.Y = pos.Y;
											if (region.TryGetPathNode(pos_tmp, out var node_tmp) && node_tmp.Flags != Terrain.PathFlags.None)
											{
												pos = pos_tmp;
											}
										}
										else if (!path_node_prev.Flags.HasAny(Terrain.PathFlags.Ladder) && node.Flags.HasAny(Terrain.PathFlags.Ladder) && node.Flags.HasNone(node_flags_exclude))
										{
											var pos_tmp = pos with { Y = pos_prev.Y };
											//pos.Y = pos_prev.Y;
											//pos_prev.X = pos.X;
											if (region.TryGetPathNode(pos_tmp, out var node_tmp) && node_tmp.Flags != Terrain.PathFlags.None)
											{
												pos_prev = pos_tmp;
											}
										}
									}
								}

								path_node_prev = node;
							}

							//span[i]; // += random.NextUnitVector2Range(0.25f, 0.50f);
						}
					}
				}

				state.points_count = span_pos.Length;
				state.current_index = 0;
				state.visible_index = 0;

				state.last_repath_time = time;
				state.last_node_time = time;
				state.jitter = default;



				if (state.last_repath_result == Terrain.RepathResult.Unreachable && !(is_target_air & is_los_target))
				{
					state.retry_count++;

					//App.WriteLine($"retry {is_target_air}; {is_los_target}");
				}
				else
				{
					//state.retry_count = 0;
				}

				if (reset)
				{
					//App.WriteLine("reset");

					results = default;
					state.retry_count = 0;
				}

				results.SetFlag(Pathfinding.Results.Unreachable, state.last_repath_result == Terrain.RepathResult.Unreachable);
				results.SetFlag(Pathfinding.Results.Dirty, true);

				//App.WriteLine(reset);s

				//App.WriteLine($"{dist_sq}; {results}");


				//App.WriteLine("repath");
			}

			// Set target position as last node when the node chain is complete
			//if ((is_los_target && !is_target_air) || (state.points_count > 0 && state.last_repath_result != Terrain.RepathResult.Partial && dist_sq < (3 * 3)))
			if (state.points_count > 0 && ((is_los_target & !is_target_air) || (state.last_repath_result != Terrain.RepathResult.Partial && dist_sq < (3 * 3))))
			{
				state.points[Maths.Max(0, state.points_count - 1)] = target_pos_new;

				//#if SERVER
				//				WorldNotification.Push(ref region, "Last", Color32BGRA.Green, target_pos_new);
				//#endif
			}

			if (results.HasNone(Pathfinding.Results.Done))
			{
				results.SetFlag(Pathfinding.Results.Stuck, state.retry_count >= 3 || (dt_last_pos > 3.00f));

				//var prev_node = default(Terrain.PathNode);

				//var is_ladder_prev = false;
				var node_flags_prev = Terrain.PathFlags.None;

				for (var i = state.current_index; i < state.points_count; i++)
				{
					if (Vector2.DistanceSquared(state.points[i], current_pos) <= 0.500f)
					{
						state.current_index = i;
						//state.visible_index = i;
					}
				}

				// Advance visible low nodes
				for (var i = state.current_index; i < state.points_count - 1; i++)
				{
					// TODO: optimize
					var delta = state.points[i] - current_pos;
					var delta_abs = Vector2.Abs(delta);
					var dist = Vector2.Distance(state.points[i], current_pos);

					var node_flags = Terrain.PathFlags.None;
					//App.WriteLine(dist);

					if (can_fly)
					{
						//var is_los = (dist < 16) && region.IsInLineOfSight(current_pos, state.points[i], 0.00f, layer: Physics.Layer.Solid, mask: Physics.Layer.None, exclude: Physics.Layer.Dynamic, query_flags: Physics.QueryFlag.Static);
						var is_los = (dist < 16) && region.IsLOS(current_pos, state.points[i], allow_inside: false);

						//var ts_a = Timestamp.Now();
						//is_los |= !region.TryLinecast(current_pos, state.points[i + 1], 0.00f, out var result, mask: Physics.Layer.World, query_flags: Physics.QueryFlag.Static);
						//App.WriteLine($"a: {ts_a.GetMilliseconds():0.0000} ms");

						//var ts_b = Timestamp.Now();
						//is_los |= !region.IsInLineOfSight(current_pos, state.points[i + 1], 0.00f);
						//App.WriteLine($"b: {ts_b.GetMilliseconds():0.0000} ms");

						if (is_los)
						{
							state.current_index = i + 1;
							state.last_node_time = time;

							//break;
						}

						if (is_los)
						{
							state.visible_index = i + 1;
							//break;
							//pathfinding.last_node_time = time;				
						}
						else
						{
							state.visible_index = i;

							break;
						}
					}
					else
					{
						//var is_los = dist < 0.50f || ((dist < 3) && region.IsInLineOfSight(current_pos, state.points[i], 0.00f, mask: Physics.Layer.Solid, exclude: Physics.Layer.Dynamic, query_flags: Physics.QueryFlag.Static));
						//var is_los = dist < 0.50f || ((dist < 3) && region.IsInLineOfSight(current_pos, state.points[i], 0.125f, mask: Physics.Layer.Solid, exclude: Physics.Layer.Dynamic, query_flags: Physics.QueryFlag.Static));

						//var is_ladder = false;

						if (region.TryGetPathNode(state.indices[i], out var node))
						{
							node_flags = node.Flags;
							//is_ladder = node.Flags.HasAny(Terrain.PathFlags.Ladder);
						}

						var can_advance = false;
						var is_visible = true;

						if (can_climb && (node_flags_prev | node_flags).HasAny(Terrain.PathFlags.Ladder))
						{
							//App.WriteLine("t");
							can_advance = delta_abs.Y < 0.25f && delta_abs.X <= required_distance;
						}
						else
						{
							//App.WriteLine("t");

							//can_advance = dist <= required_distance || ((dist <= 3) && region.IsInLineOfSight(current_pos, state.points[i], 0.25f, layer: Physics.Layer.Solid, mask: Physics.Layer.None, exclude: Physics.Layer.Dynamic, query_flags: Physics.QueryFlag.Static));
							can_advance = dist <= required_distance || ((dist <= 3) && region.IsLOS(current_pos, state.points[i], allow_inside: false));
							//App.WriteLine(can_advance);
						}

						if ((node_flags_prev & node_flags).HasAny(Terrain.PathFlags.Ground | Terrain.PathFlags.Edge) && (!can_climb || !(node_flags_prev | node_flags).HasAny(Terrain.PathFlags.Ladder)))
						{
							can_advance |= delta_abs.X <= required_distance;
						}

						//can_advance |= dist < 0.25f;

						//if ((node_flags_prev & node_flags).HasAny(Terrain.PathFlags.Ladder))
						//{
						//	can_advance &= delta_abs.Y < required_distance * 0.50f;
						//	//is_visible &= delta_abs.X < 0.25f;
						//}

						//App.WriteLine(state.points_count);

						//var ts_a = Timestamp.Now();
						//is_los |= !region.TryLinecast(current_pos, state.points[i + 1], 0.00f, out var result, mask: Physics.Layer.World, query_flags: Physics.QueryFlag.Static);
						//App.WriteLine($"a: {ts_a.GetMilliseconds():0.0000} ms");

						//var ts_b = Timestamp.Now();
						//is_los |= !region.IsInLineOfSight(current_pos, state.points[i + 1], 0.00f);
						//App.WriteLine($"b: {ts_b.GetMilliseconds():0.0000} ms");

						//if ((delta_abs.X < 0.10f && delta_abs.Y < 0.05f) || (is_los && dist < required_distance))



						//if (i > 0) can_advance |= Vector2.DistanceSquared(state.points[i - 1], state.points[i]) <= 0.01f;
						//can_advance |= Vector2.DistanceSquared(state.points[i], state.points[i + 1]) <= 0.40f;

						//App.WriteLine(can_advance);

						if (can_advance)
						{
							state.current_index = i + 1;
							state.last_node_time = time;
							state.jitter = default;
							//state.retry_count = 0;

							//break;
						}

						//is_ladder_prev = is_ladder;

						node_flags_prev = node_flags;

						if (can_advance)
						{
							state.visible_index = i + 1;
							//break;
							//pathfinding.last_node_time = time;				
						}
						else
						{
							state.visible_index = i;

							break;
						}
					}
				}

				if ((time - state.last_node_time) > 0.50f && state.retry_count > 0)
				{
					if (dt_last_los > 0.25f && time > state.next_jitter_time) // && (time - state.last_node_time) > 0.50f)
					{
						//state.jitter = random.NextUnitVector2Range(0.20f, 1.80f) * Maths.Clamp(time - state.last_node_time, 0.00f, 2.00f); // * Math.Min(state.retry_count, 5);
						//state.jitter.Y *= 0.80f;

						state.jitter = default;

						var dir = (state.points[state.current_index] - current_pos_actual).GetNormalizedFast();
						var pos_jitter_tmp = Maths.Lerp(current_pos_actual, state.points[state.current_index], random.NextFloatExtra(0.25f, 0.25f)) + (dir.GetPerpendicularAligned(new(0.00f, dir.Y.Sign())) * random.NextFloatExtra(1.00f, 1.50f));
						if (region.TryGetAnyValidPathPos(ref pos_jitter_tmp, max_distance: 3.00f))
						{
							state.jitter = pos_jitter_tmp;
							state.next_jitter_time = time + random.NextFloatRange(0.30f, 1.50f);
						}
					}
				}
				else
				{
					state.jitter = default;
				}


				if (state.jitter.IsNotNil()) target_pos_new = state.jitter;
				else target_pos_new = state.points[state.current_index];

				//target_pos_new = state.points[state.current_index] + state.jitter; // Vector2.Lerp(state.points[state.current_index], state.current_index >= state.points_count - 1 ? state.points_high[state.current_high_index] : state.points[state.visible_index], 0.20f); // + state.jitter;
			}
			else
			{
				//App.WriteLine("h");

				if (state.points_high_count > 0 && (state.current_index >= state.points_count - 2 || state.visible_index >= state.points_count - 2))
				{
					target_pos_new = state.points_high[state.visible_high_index];
				}
				else if (state.points_count > 0)
				{
					target_pos_new = state.points[state.visible_index];
				}
			}

			if (is_los_target & is_target_air)
			{
				results |= Results.Done | Results.In_Air | Results.LOS;
			}

			state.last_result = results;

			if (is_los_target && (can_fly || (current_pos_actual.Y - target_pos.Y).Abs() <= 3.00f))
			{
				target_pos_new = target_pos;
				//state.jitter = default;

				//App.WriteLine("t");

				if (state.points_high_count > 0)
				{
					state.current_high_index = state.points_high_count - 1;
					state.visible_high_index = state.points_high_count - 1;
				}
			}

			state.debug_ms = (float)ts.GetMilliseconds();

#if SERVER
			const bool debug_always_print_repath = false;
			if ((AI.debug_always_sync || debug_always_print_repath) && state.debug_ms >= 0.10f)
			{
				WorldNotification.Push(ref region, $"{state.debug_ms:0.000}ms\nL:{repath_lo_elapsed_ms:0.000}ms\nH:{repath_hi_elapsed_ms:0.000}ms\n{state.last_repath_result};{state.points_count};{state.points_high_count}", color: Color32BGRA.GreenRed(Maths.Normalize01(state.debug_ms, 0.50f).Sqrt()), position: current_pos, lifetime: 8.00f);
			}
#endif

			if (results.HasAny(Pathfinding.Results.Dirty))
			{
				//App.WriteLine($"repath in {ts.GetMilliseconds():0.0000} ms; a: {state.last_repath_result}; b: {results}; attempt: {state.retry_count}; low: {repath_low}; high: {repath_high}; los: {is_los_target}; {dist_sq}");
			}

			//if (!can_fly)
			//{
			//	results.SetFlag(Results.In_Air, is_target_air);
			//}

#if SERVER
			//WorldNotification.Push(ref region, $"{state.last_result}", 0xffff0000, target_pos_new);
#endif

			if (state.last_result.HasAny(Pathfinding.Results.Unreachable))
			{
				target_pos_new = target_pos;
			}

			//App.WriteLine(target_pos_new);

#if SERVER
			//region.DrawDebugText(current_pos, "test", Color32BGRA.White);
			//region.DrawDebugLine(current_pos, target_pos_new, color: Color32BGRA.Yellow);
			//App.WriteLine($"{current_pos}; {target_pos_new}");

#endif

			return target_pos_new;
		}

		[Shitcode]
		public static void MoveToLand2(ref Region.Data region, ref Control.Data control, Vector2 pos_current, Vector2 pos_dst, NPC.SelfHints self_hints)
		{
			const bool show_debug = false;

			var th_l = 0.250f;
			var th_r = 0.250f;
			var th_u = 0.500f;
			var th_d = 0.500f;

			//var av_l = 0.500f;
			//var av_r = 0.500f;
			//var av_u = 0.125f;
			//var av_d = 0.125f;

			var a = pos_current;
			var b = pos_dst;

			//App.WriteLine(self_hints);

			var node_flags_a = Terrain.PathFlags.None;
			var node_flags_b = Terrain.PathFlags.None;

			var node_sides_a = Terrain.PathSides.None;
			var node_sides_b = Terrain.PathSides.None;

			//var is_climbing = false;
			if (region.TryGetPathNode(a, out var node_a))
			{
				node_flags_a = node_a.Flags;
				node_sides_a = node_a.Sides;
				//is_climbing = current_node.Flags.HasAny(Terrain.PathFlags.Ladder);

				//b = 
			}

			if (region.TryGetPathNode(b, out var node_b))
			{
				node_flags_b = node_b.Flags;
				node_sides_b = node_b.Sides;
			}

			var node_flags_ab = (node_flags_a | node_flags_b);
			var node_flags_ab_both = (node_flags_a & node_flags_b);

			var delta = (b - a);
			var dir = delta.GetNormalized(out var distance);

			var delta_abs = Vector2.Abs(delta);
			ref var kb = ref control.keyboard;

			var can_climb = self_hints.HasAny(NPC.SelfHints.Can_Climb);
			var can_fly = self_hints.HasAny(NPC.SelfHints.Can_Fly);

			var is_climbing = false;

			//var l = false;
			//var r = false;
			//var u = false;
			//var d = false;

			//App.WriteLine(delta);

			if (can_climb & node_flags_ab.HasAny(Terrain.PathFlags.Ladder))
			{
				th_l = 0.200f;
				th_r = 0.200f;
				th_u = 0.05f;
				th_d = 0.25f;

				//th_u = Maths.Max(0.125f, delta_abs.Y);
				//th_d = Maths.Max(0.125f, delta_abs.Y);

				//th_l = Maths.Max(0.125f, delta_abs.Y);
				//th_r = Maths.Max(0.125f, delta_abs.Y);

				//th_l = delta_abs.Y;
				//th_r = delta_abs.Y;

				//if (delta_abs.Y <= 0.10f)
				{
					kb.SetKeyPressed(Keyboard.Key.MoveLeft, (delta.X <= -th_l) && !(delta.X >= th_r));
					kb.SetKeyPressed(Keyboard.Key.MoveRight, (delta.X >= th_r) && !(delta.X <= -th_l));
				}

				//if (delta_abs.X <= 0.125f)
				{
					kb.SetKeyPressed(Keyboard.Key.MoveUp, (delta.Y <= -th_u) && !(delta.Y >= th_d));
					kb.SetKeyPressed(Keyboard.Key.MoveDown, (delta.Y >= th_d) && !(delta.Y <= -th_u));
				}

				is_climbing = true;
			}
			//else if (node_flags_a.HasAny(Terrain.PathFlags.Wall) && self_hints.HasAny(NPC.SelfHints.Can_Walk)) // || node_flags_a == Terrain.PathFlags.None) // && delta.Y > 0.100f)
			//																							  //else if ((node_flags_a.HasAny(Terrain.PathFlags.Wall | Terrain.PathFlags.Edge) & self_hints.HasAny(NPC.SelfHints.Can_Walk))) // || node_flags_a == Terrain.PathFlags.None) // && delta.Y > 0.100f)
			//{
			//	//kb.SetKeyPressed(Keyboard.Key.MoveLeft, delta.X < 0);
			//	//kb.SetKeyPressed(Keyboard.Key.MoveRight, delta.X > 0);

			//	//kb.SetKeyPressed(Keyboard.Key.MoveLeft, (delta.X <= -th_l) && !(delta.X >= th_r));
			//	//kb.SetKeyPressed(Keyboard.Key.MoveRight, (delta.X >= th_r) && !(delta.X <= -th_l));

			//	kb.SetKeyPressed(Keyboard.Key.MoveUp, (delta.Y <= -th_u) && !(delta.Y >= th_d));
			//	kb.SetKeyPressed(Keyboard.Key.MoveDown, (delta.Y >= th_d) && !(delta.Y <= -th_u));

			//	is_climbing = true;
			//}
			else
			{
				th_u = Maths.Clamp(delta_abs.X * -delta.Y, 1.00f, 2.00f);
				th_d = 2.00f; // Maths.Clamp(delta_abs.X * delta.Y, 0.50f, 2.00f);
							  //th_d = 8;


				kb.SetKeyPressed(Keyboard.Key.MoveLeft, (delta.X <= -th_l) && !(delta.X >= th_r));
				kb.SetKeyPressed(Keyboard.Key.MoveRight, (delta.X >= th_r) && !(delta.X <= -th_l));
				kb.SetKeyPressed(Keyboard.Key.MoveUp, (delta.Y <= -th_u) && !(delta.Y >= th_d));
				kb.SetKeyPressed(Keyboard.Key.MoveDown, (delta.Y >= th_d) && !(delta.Y <= -th_u));

			}

			if (!can_fly)
			{
				// jump
				if (!kb.GetKey(Keyboard.Key.MoveDown) && ((delta.Y < -1.00f && node_flags_ab.HasAny(Terrain.PathFlags.Edge) && (!can_climb || node_flags_ab_both.HasNone(Terrain.PathFlags.Ladder))) || (can_climb & node_flags_a.HasAny(Terrain.PathFlags.Ladder) & node_flags_b.HasNone(Terrain.PathFlags.Ladder))))
				{
					kb.SetKeyPressed(Keyboard.Key.MoveUp, true);
					is_climbing = true;
					//kb.SetKeyPressed(Keyboard.Key.MoveLeft | Keyboard.Key.MoveRight | Keyboard.Key.MoveDown, false);
				}
			}

			if (!is_climbing)
			{
				if ((node_sides_a | node_sides_b).HasNone(Terrain.PathSides.Top | Terrain.PathSides.Bottom))
				{
					kb.SetKeyPressed(Keyboard.Key.MoveDown, true);

					if (show_debug)
					{
#if SERVER
						WorldNotification.Push(ref region, "crouch"u8, Color32BGRA.Cyan, pos_current);
#endif
					}
				}
			}


			//App.WriteLine(delta);


			if (show_debug)
			{
#if SERVER
				//WorldNotification.Push(ref region, $"delta: {delta}", Color32BGRA.Cyan, b);

				if (kb.GetKey(Keyboard.Key.MoveLeft)) region.DrawDebugDir(pos_current, new Vector2(-4, 0.00f), Color32BGRA.White, 1.00f);
				if (kb.GetKey(Keyboard.Key.MoveRight)) region.DrawDebugDir(pos_current, new Vector2(4, 0.00f), Color32BGRA.White, 1.00f);
				if (kb.GetKey(Keyboard.Key.MoveUp)) region.DrawDebugDir(pos_current, new Vector2(0, -4), Color32BGRA.White, 1.00f);
				if (kb.GetKey(Keyboard.Key.MoveDown)) region.DrawDebugDir(pos_current, new Vector2(0, 4), Color32BGRA.White, 1.00f);
				region.DrawDebugDir(a, dir * distance, Color32BGRA.Yellow, 8);
#endif
			}
		}
	}
}
