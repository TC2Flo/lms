namespace TC2.Base.Components
{
	public static partial class WorkerExt
	{
		extension<T>(ref T v) where T : struct, allows ref struct
		{
			public static ref T Null
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => ref Unsafe.NullRef<T>();
			}

			public static T Default
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => default(T);
			}
		}

		public static readonly Sound.Handle snd_complete = "order_complete";

		//#if SERVER

		[ISystem.Modified.Pair<Crafting.Order>.Wildcard(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global)]
		public static void OnModifiedOrders(ISystem.Info.Common info, ref Region.Data.Common region_common, ref XorRandom random, Entity entity, Entity ent_order,
		[Source.Owned] in Transform.Data transform,
		[Source.Owned] ref Workshop.Data workshop,
		[Source.Owned, Pair.Wildcard] ref Crafting.Order order, IComponent.Handle h_order)
		{
			var is_index_pair = h_order.IsIndexPair<Crafting.Order>(out var index);

#if SERVER
			if (is_index_pair)
			{
				if (workshop.order_status_flags[index].TrySetFlag(Workshop.StatusFlags.Needs_Workers, order.cache_flags.HasAny(Crafting.Order.CacheFlags.Needs_Workers)))
				{
					workshop.Sync(entity, true);
				}
			}
#endif

			//App.WriteLine($"OnModifiedOrders: {h_order}; {ent_order}; {info.Event}; {info.EventID}; {info.EventType}; {order.h_recipe}; {is_index_pair}; {index}");
		}

		// TODO: hack, currently overrides previous extra mass - need to figure out how to do it cumulatively (maybe a Body.Data event for recalculating mass?)
		[ISystem.Modified.Pair<Crafting.Order>.Index(ISystem.Mode.Single, ISystem.Scope.Region, index: 0)]
		public static void OnModifiedOrderMass(ISystem.Info.Common info, ref Region.Data.Common region_common, ref XorRandom random, Entity entity, Entity ent_order,
		[Source.Owned] in Transform.Data transform, [Source.Owned] ref Body.Data body, [Source.Owned] in Workshop.Data workshop,
		[Source.Owned, Pair.Index(0)] ref Crafting.Order order, IComponent.Handle h_order)
		{
			//App.WriteLine($"OnModifiedOrderMass: {h_order}; {ent_order}; {info.Event}; {info.EventID}; {info.EventType}; {order.h_recipe}");
			//if (order.flags.HasAny(Crafting.Order.Flags.Add_Mass))
			if (workshop.flags.HasAny(Workshop.Flags.Add_Order_Mass))
			{
				if (body.mass_extra.TrySet(order.current_mass))
				{
					body.MarkDirty();
				}
			}
		}

		[ISystem.Modified(ISystem.Mode.Single, ISystem.Scope.Region)]
		[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region, interval: 0.50f)]
		public static void OnUpdateWorkSprite(ISystem.Info info, ref Region.Data region, Entity ent_order,
		[Source.Owned, Pair.Tag("work")] ref Animated.Renderer.Data renderer, [Source.Owned, Pair.First] ref Crafting.Order order)
		{
			ref var recipe = ref order.h_recipe.GetData();
			if (recipe.IsNotNull() && order.current_color.IsVisible())
			{
				renderer.sprite.frame.y = 1u + Maths.LerpUInt(0u, renderer.sprite.count - 1, Maths.Normalize01Fast(order.amount_multiplier, recipe.max));
			}
			else
			{
				renderer.sprite.frame.y = 0;
			}

			renderer.color_mask_r = order.current_color;
		}

#if SERVER
		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[ISystem.Update.D(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global, flags: ISystem.Flags.SkipLocalsInit | ISystem.Flags.Unchecked)]
		public static void UpdateOrders(ISystem.Info.Common info, ref Region.Data.Common region_common, ref XorRandom random, Entity ent_order,
		[Source.Owned] in Transform.Data transform, [Source.Owned] ref Crafting.Order order, [Source.Owned] ref Shipment.Data shipment) //.Order order)
		{
			order.world_position = transform.position;
			if (order.h_recipe)
			{
				var time = info.WorldTime;
				//var dirty = order.flags.HasAny(Crafting.Order.Flags.Dirty);
				if (order.flags.HasAny(Crafting.Order.Flags.Complete))
				{
					if (order.flags.HasAny(Crafting.Order.Flags.Auto_Complete))
					{
						Crafting.CompleteOrder(IComponent.Handle.FromComponent<Crafting.Order>(), ent_order);
					}
				}
				else if (time >= order.t_next_update && (order.flags.HasAny(Crafting.Order.Flags.Dirty) || order.flags.TryAddFlag(Crafting.Order.Flags.Cached)))
				{
					ref var recipe_data = ref order.h_recipe.GetData();
					if (recipe_data.IsNull())
					{
						order = default;
						order.Sync(ent_order);
					}
					else
					{
						order.t_next_update = time + 0.50f;

						var mark_modified = false;
						var stages = recipe_data.stages;
						if (stages.IsNotNullOrEmpty())
						{
							var dirty = order.flags.HasAny(Crafting.Order.Flags.Dirty);

							if (order.current_stage == order.next_stage) // either first stage or completing a stage
							{
								AdvanceOrderStage(region_common: ref region_common, transform: in transform, shipment: ref shipment, recipe_data: ref recipe_data, order: ref order, dirty: ref dirty, mark_modified: ref mark_modified, ent_order: ent_order, entity: ent_order, time: time);
							}
							else
							{
								UpdateOrderStage(region_common: ref region_common, transform: in transform, shipment: ref shipment, recipe_data: ref recipe_data, order: ref order, dirty: ref dirty, mark_modified: ref mark_modified, random: ref random, time: time);
							}

							if (dirty)
							{
								if (mark_modified) order.Modified(ent_order, true);
								else order.Sync(ent_order);
							}
						}
					}
				}
			}
		}
#endif

		// TODO: split this into multiple methods
		//[ISystem.Update.D(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global, flags: ISystem.Flags.Instanced, interval: 0.333f)]
		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[ISystem.Update.D(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global, flags: ISystem.Flags.Instanced | ISystem.Flags.SkipLocalsInit | ISystem.Flags.Unchecked)]
		public static void UpdateOrdersPairs(ISystem.Info.Common info, ref Region.Data.Common region_common, ref XorRandom random, Entity entity, Entity ent_order,
		[Source.Owned] in Transform.Data transform, [Source.Owned, Optional(true)] ref Crafter.State crafter_state, // [Source.Owned] ref Workshop.Data workshop,
		[Source.Owned, Pair.Wildcard] ref Crafting.Order order, IComponent.Handle h_order, //,
		[Source.Owned, Optional(true)] ref Shipment.Data shipment) // [Source.Owned, Pair.First, Optional(true)] ref Heater.Data heater)
		{
			order.world_position = transform.position;
			if (order.h_recipe)
			{
				var time = info.WorldTime;
				if (crafter_state.IsNotNull())
				{
					//order.current_temperature.m_value.LerpFMARef(crafter_state.temperature, 0.10f);
					//order.current_pressure.m_value.LerpFMARef(crafter_state.pressure, 0.10f);

					// TODO: kinda shitcode, but no time for a proper implementation atm
					order.current_temperature.m_value = Maths.Lerp2(order.current_temperature.m_value, crafter_state.temperature, 0.03f, 1.00f);
					order.current_pressure.m_value = Maths.Lerp2(order.current_pressure.m_value, crafter_state.pressure, 0.03f, 1.00f);
				}

#if SERVER
				//var dirty = order.flags.HasAny(Crafting.Order.Flags.Dirty);
				if (order.flags.HasAny(Crafting.Order.Flags.Complete))
				{
					if (order.flags.HasAny(Crafting.Order.Flags.Auto_Complete))
					{
						//ref var recipe_data = ref order.h_recipe.GetData();
						//if (recipe_data.IsNotNull() && (recipe_data.flags.HasAny(Recipe.Flags.Auto_Complete))) // || control.mouse.GetKeyDown(Mouse.Key.Right)))
						//{
						Crafting.CompleteOrder(h_order, ent_order);
						//}
					}
				}
				else if (time >= order.t_next_update && (order.flags.HasAny(Crafting.Order.Flags.Dirty) || order.flags.TryAddFlag(Crafting.Order.Flags.Cached)))
				{
					ref var recipe_data = ref order.h_recipe.GetData();
					if (recipe_data.IsNull())
					{
						order = default;
						h_order.Sync(ent_order, ref order);
					}
					else
					{
						order.t_next_update = time + 0.50f;

						var mark_modified = false;
						var stages = recipe_data.stages;
						if (stages.IsNotNullOrEmpty())
						{
							var dirty = order.flags.HasAny(Crafting.Order.Flags.Dirty);

							//if (order.current_stage < stages.Length)
							//{
							if (order.current_stage == order.next_stage) // either first stage or completing a stage
							{
								AdvanceOrderStage(region_common: ref region_common, transform: in transform, shipment: ref shipment, recipe_data: ref recipe_data, order: ref order, dirty: ref dirty, mark_modified: ref mark_modified, ent_order: ent_order, entity: entity, time: time);
							}
							else
							{
								UpdateOrderStage(region_common: ref region_common, transform: in transform, shipment: ref shipment, recipe_data: ref recipe_data, order: ref order, dirty: ref dirty, mark_modified: ref mark_modified, random: ref random, time: time);
							}
							//}
							//else
							//{
							//	if (order.flags.TryAddRemFlags(Crafting.Order.Flags.Complete, Crafting.Order.Flags.In_Progress))
							//	{
							//		//Sound.Play(region: ref region_common, sound: recipe_data.h_sound_complete, world_position: transform.position);
							//		//order.t_next_update = time + recipe_data.complete_delay;

							//		Sound.Play(region: ref region_common, sound: recipe_data.h_sound_complete, world_position: transform.position, volume: 0.80f);
							//		order.t_next_update = time + recipe_data.complete_delay;

							//		dirty = true;
							//		mark_modified = true;
							//	}
							//}

							if (dirty)
							{
								//App.WriteLine($"sync {order.current_group_work_mult}");
								if (mark_modified) h_order.MarkModified(ent_order, true);
								else h_order.Sync(ent_order, ref order);
							}
						}
					}
				}
#endif
			}


			//if (sync)
			//{
			//	workshop.Sync(entity, true);
			//}
		}

#if SERVER
		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void UpdateOrderStage(ref Region.Data.Common region_common, in Transform.Data transform, ref Shipment.Data shipment,
		ref IRecipe.Data recipe_data, ref Crafting.Order order, ref bool dirty, ref bool mark_modified, ref XorRandom random, float time)
		{
			var stages_span = recipe_data.stages.AsSpan();
			ref var stage_option = ref stages_span[order.current_stage].options[order.current_stage_option];
			var reqs_span = stage_option.requirements.AsSpan();

			if (order.requirements_bits_group_mask == 0)
			{
				order.requirements_bits_group_mask = reqs_span.GetRequirementsMask(order.current_group);
			}

			var reqs_bits_pending = ~order.requirements_bits_stage_done;
			reqs_bits_pending |= order.requirements_bits_stage_always;
			reqs_bits_pending &= order.requirements_bits_stage_mask;
			reqs_bits_pending &= order.requirements_bits_group_mask;

			//App.WriteLine(reqs_bits_pending);

			var current_group_work_mult = 1.00f;

			if (reqs_bits_pending != 0)
			{
				var reqs_bits_done_cached = order.requirements_bits_stage_done;
				if (!order.work_handles.IsAllZeros())
				{
					for (var i = 0u; i < order.work.ULength; i++)
					{
						ref var work = ref order.work[i];
						if (reqs_bits_pending.TestBit(work.req_index)) // && req_index < reqs_span.Length)
						{
							ref var work_data = ref order.work_handles[i].GetData(); // req.work.GetData();
							if (work_data.IsNotNull()) // & req.type == Crafting.Requirement.Type.Work)
							{
								if (work.flags.HasAny(Work.Amount.Flags.Passive | Work.Amount.Flags.Ambient) && (reqs_bits_done_cached & work.requirements_bits_mask) == work.requirements_bits_mask && time >= order.next_work_times[i])
								{
									var work_cooldown = work_data.cooldown;

									var amount = work_data.amount_base;
									amount *= work_data.multiplier * order.current_group_work_mult;
									if (work_data.amount_jitter != 0.00f) amount *= random.NextFloat01FMA(work_data.amount_jitter, 1.00f);
									if (work_data.flags.HasNone(IWork.Flags.Time_Independent)) amount *= work_cooldown;
									amount = Maths.Clamp(amount, 0.00f, work.remaining);

									work.remaining -= amount; // Maths.Clamp(work.current + amount, 0.00f, work.required);

									//App.WriteLine("e");
									//order.flags.RemoveFlag(Crafting.Order.Flags.Cached);

									var h_sound = work_data.sounds_passive.GetRandom(ref random);
									if (h_sound)
									{
										Sound.Play(region: ref region_common,
											sound: h_sound,
											world_position: transform.position,
											volume: work_data.sound_volume * (work_data.flags.HasAny(IWork.Flags.No_Randomization) ? 1.00f : random.NextFloatExtra(0.95f, 0.10f)),
											pitch: work_data.sound_pitch * (work_data.flags.HasAny(IWork.Flags.No_Randomization) ? 1.00f : random.NextFloatExtra(0.88f, 0.22f)),
											size: work_data.sound_size,
											dist_multiplier: work_data.sound_dist);
									}

									order.next_work_times[i] = time + work_cooldown;
									dirty = true;
								}

								order.requirements_bits_stage_done.SetBit(work.req_index, work.remaining <= 0.00f);
								if (work.flags.TrySetFlag(Work.Amount.Flags.Pending, !order.requirements_bits_stage_done.TestBit(work.req_index)))
								{
									dirty = true;
								}
								reqs_bits_pending.RemBit(work.req_index);
							}
							//App.WriteLine(order.requirements_bits_stage_done.ToFormattedBinary2());
						}
					}
				}

				if (reqs_bits_pending != 0)
				{
					if (shipment.IsNotNull())
					{
						for (var i = 0u; i < shipment.items.ULength; i++)
						{
							ref var item = ref shipment.items[i];
							if (item.type == Shipment.Item.Type.Undefined) continue;

							var req_index = order.shipment_reqs_mappings[i];
							if (reqs_bits_pending.TestBit(req_index))
							{
								order.requirements_bits_stage_done.SetBit(req_index, item.quantity >= item.max);
								reqs_bits_pending.RemBit(req_index);
							}
						}
					}
				}

				if (reqs_bits_pending != 0)
				{
					do
					{
						var req_index = reqs_bits_pending.TzCount();
						ref var req = ref reqs_span[req_index];

						//App.WriteLine($"[{req_index}] {req.type}: {requirements_bits_pending.ToFormattedBinary2(' ')}");

						var ok = false;
						//if (req.group != current_group & req.flags.HasAny(Crafting.Requirement.Flags.Group_Evaluate)) continue;
						//App.WriteLine($"[{req_index}] {req.type}: {requirements_bits_pending.ToFormattedBinary2(' ')}");

						switch (req.type)
						{
							case Crafting.Requirement.Type.Temperature:
							{
								var t = req.GetRequirementBias(order.current_temperature);
								if (req.flags.HasAny(Crafting.Requirement.Flags.Optional)) t += 1.00f;
								ok = (t > 0.05f);

								if (req.group == order.current_group)
								{
									current_group_work_mult *= t;
								}
								//App.WriteLine(t);
							}
							break;

							case Crafting.Requirement.Type.Pressure:
							{
								var t = req.GetRequirementBias(order.current_pressure);
								if (req.flags.HasAny(Crafting.Requirement.Flags.Optional)) t += 1.00f;
								ok = (t > 0.05f);

								if (req.group == order.current_group)
								{
									current_group_work_mult *= t;
								}
								//App.WriteLine(t);
							}
							break;
						}

						order.requirements_bits_stage_done.SetBit(req_index, ok);
						//if (ok) order.requirements_bits_stage_done.AddBit(req_index); // |= (1u << req_index);
						//else order.requirements_bits_stage_done.RemBit(req_index); // &= ~(1u << req_index);
					}
					while (reqs_bits_pending.ResetLowestSetBit() != 0);
				}

				dirty |= order.requirements_bits_stage_done != reqs_bits_done_cached;
				order.flags.RemoveFlag(Crafting.Order.Flags.Cached | Crafting.Order.Flags.Dirty);
			}
			else
			{
				order.flags.RemoveFlag(Crafting.Order.Flags.Cached | Crafting.Order.Flags.Dirty, dirty);
			}

			if (order.requirements_bits_stage_done == order.requirements_bits_stage_mask)
			{
				Sound.Play(region: ref region_common, sound: stage_option.h_sound_complete, world_position: transform.position, volume: 0.80f);
				order.t_next_update = time + stage_option.complete_delay;
				order.current_stage++;

				//App.WriteLine("stage done");

				dirty = true;
				mark_modified = true;

				order.flags.RemoveFlag(Crafting.Order.Flags.Cached);
			}
			else if ((order.requirements_bits_stage_done & order.requirements_bits_group_mask) == order.requirements_bits_group_mask)
			{
				order.requirements_bits_group_mask = 0;
				order.t_next_update = time + stage_option.advance_delay;
				order.current_group++;

				//App.WriteLine("group done");

				dirty = true;
				mark_modified = true;

				order.flags.RemoveFlag(Crafting.Order.Flags.Cached);
			}

			order.current_group_work_mult = current_group_work_mult;
			order.flags.RemoveFlag(Crafting.Order.Flags.Cached, order.requirements_bits_stage_always != 0);
		}

		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void AdvanceOrderStage([UnscopedRef] ref Region.Data.Common region_common,
		in Transform.Data transform, ref Shipment.Data shipment, ref IRecipe.Data recipe_data, [UnscopedRef] ref Crafting.Order order,
		ref bool dirty, ref bool mark_modified, Entity ent_order, Entity entity, float time)
		{
			var stages_span = recipe_data.stages.AsSpan();

			if (order.next_stage == 0)
			{
				Sound.Play(region: ref region_common,
					sound: recipe_data.h_sound_begin,
					world_position: transform.position,
					volume: 0.80f);
			}
			else
			{
				//Context.NewFromOrder(ref regionCommon, ent_order, ref componentFromHandle, out var context, ent_requester, ent_pickup, 4f);

				//ref var stage_prev = ref stages_span.GetRefAtIndexOrNull()[order.current_stage - 1];
				ref var stage_prev_option = ref stages_span[order.current_stage - 1].options[order.current_stage_option];
				if (stage_prev_option.on_advance.IsValid())
				{
					Crafting.Context.NewFromOrder(region: ref region_common, ent_order: ent_order, order: ref order, context: out var context, search_radius: 4.00f);

					Crafting.ConfiguredStage.Create(recipe: ref recipe_data, // IRecipe.Data.Null, // recipe_data,
					context: ref context, // ref Crafting.Context.Null, // context,
					stage_index: (byte)(order.current_stage - 1),
					selected_stage_options: order.selected_stage_options,
					reqs_dst: FixedArray.CreateSpan16<Crafting.Requirement>(out var reqs_stage_buffer),
					prds_dst: FixedArray.CreateSpan8<Crafting.Product>(out var prds_stage_buffer),
					amount_multiplier: order.amount_multiplier,
					configured_stage: out scoped var configured_stage,
					flags: Crafting.ConfiguredStage.Flags.None,
					residues: ref SpanList<Shipment.Item2>.Null);

					var ev_copy = stage_prev_option.on_advance.GetCopy();
					ev_copy.Bind(ref configured_stage);
					ev_copy.TriggerEvent(ent_order);
				}
			}

			if (order.current_stage < (uint)stages_span.Length)
			{
				order.next_stage++;
				order.current_stage_option = order.GetStageOptionIndex();
				order.current_group = 0;
				order.cache_flags = Crafting.Order.CacheFlags.None;
				//order.requirements_bits_stage = 0;
				//order.requirements_bits = 0;
				ref var stage = ref stages_span[order.current_stage];
				ref var stage_option = ref stage.options[order.current_stage_option];

				var reqs_span = stage_option.requirements.AsSpan();

				order.requirements_bits_group_mask = 0;
				//order.requirements_bits_stage_mask = 0;
				//order.requirements_bits_stage_always = 0;
				//order.requirements_bits_stage_done = 0;

				//var reqs_bits_stage_mask = 0u; // order.requirements_bits_stage_mask;
				//var reqs_bits_stage_done = 0u; // order.requirements_bits_stage_done;
				//var reqs_bits_stage_always = 0u; // order.requirements_bits_stage_always;

				//var item_counter = 0u;
				//var work_counter = 0u;

				var is_shipment_dirty = false;

				if (order.flags.TryRemoveFlag(Crafting.Order.Flags.Has_Shipment) & shipment.IsNotNull())
				{
					is_shipment_dirty = true;
					shipment.Clear();
				}

				order.shipment_reqs_mappings.Clear();
				order.work_handles.Clear();
				order.work.Clear();
				order.work_units_rem.Clear();
				order.work_step_sizes.Clear();

				//App.WriteLine("update stage");


				var reqs_bits_stage_mask = 0u; // order.requirements_bits_stage_mask;
				var reqs_bits_stage_done = 0u; // order.requirements_bits_stage_done;
				var reqs_bits_stage_always = 0u; // order.requirements_bits_stage_always;
				var reqs_bits_stage_optional = 0u; // order.requirements_bits_stage_always;
				var reqs_bits_stage_simultaneous = 0u; // order.requirements_bits_stage_always;
				var item_counter = 0u;
				var work_counter = 0u;

				var unit_count = (int)order.amount_multiplier;
				var no_delay = false;

				for (var req_index = 0; req_index < reqs_span.Length; req_index++)
				{
					ref var req = ref reqs_span[req_index];
					//if (req.group != order.current_group) continue;

					reqs_bits_stage_mask.AddBit(req_index);
					reqs_bits_stage_done.AddBit(req_index, req.flags.HasAny(Crafting.Requirement.Flags.Prerequisite));
					reqs_bits_stage_always.AddBit(req_index, req.flags.HasAny(Crafting.Requirement.Flags.Always_Evaluate));
					reqs_bits_stage_optional.AddBit(req_index, req.flags.HasAny(Crafting.Requirement.Flags.Optional));
					//reqs_bits_stage_simultaneous.AddBit(req_index, req.flags.HasAny(Crafting.Requirement.Flags.Simultaneous));

					if (!reqs_bits_stage_done.TestBit(req_index))
					{
						switch (req.type)
						{
							case Crafting.Requirement.Type.Resource:
							case Crafting.Requirement.Type.Prefab:
							{
								var item = (Shipment.Item)req.ToItem2();
								item.quantity = 0.00f;
								item.max = req.GetRequirementAmount(order.amount_multiplier).amount_snapped;

								if (Assert.IsNotNull(ref shipment, Assert.Level.Warn))
								{
									shipment.items[item_counter] = item;
									order.shipment_reqs_mappings[item_counter] = (byte)req_index;

									//App.WriteLine($"add shipment item req [{item_counter}] {item.quantity}/{item.max} {item.type}");
									item_counter++;

									//var item = shipment.items[item_index++];
									//item
								}
							}
							break;

							case Crafting.Requirement.Type.Work:
							{
								//if (Constants.Requirements.enable_instant_work)
								//{
								//	reqs_bits_stage_done |= 1u << req_index;
								//	continue;
								//}

								ref var work_data = ref req.work.GetData();
								if (work_data.IsNotNull())
								{
									var work_flags = Work.Amount.Flags.Pending;
									//work_flags.AddFlag(Work.Amount.Flags.Crafter_Only, req.flags.HasAny(Crafting.Requirement.Flags.Crafter_Only));
									//work_flags.AddFlag(Work.Amount.Flags.Ambient, work_data.flags.HasAny(IWork.Flags.Ambient));
									//work_flags.AddFlag(Work.Amount.Flags.Passive, work_data.flags.HasAny(IWork.Flags.Passive));
									work_flags |= (Work.Amount.Flags)(work_data.flags.WithMasked(IWork.Flags.Manual | IWork.Flags.Machine | IWork.Flags.Passive | IWork.Flags.Ambient | IWork.Flags.Crafter_Only));

									//work_flags.

									var work = new Work.Amount(remaining: req.GetRequirementAmount(order.amount_multiplier).amount_snapped, req_index: (byte)req_index, flags: work_flags);

									if (req.flags.HasAny(Crafting.Requirement.Flags.Conditional))
									{
										//work.requirements_bits_mask |= (ushort)((~(uint.MaxValue << req_index)) & ~reqs_bits_stage_optional);
										work.requirements_bits_mask |= (ushort)~(uint.MaxValue << req_index);
										reqs_bits_stage_simultaneous.AddBit(req_index, req.flags.HasAny(Crafting.Requirement.Flags.Simultaneous));
									}

									if (req.flags.HasAny(Crafting.Requirement.Flags.Stepped))
									{
										order.work_units_rem[work_counter] = (byte)(uint)unit_count;
										order.work_step_sizes[work_counter] = work.remaining / unit_count;
									}

									work.requirements_bits_mask &= (ushort)~(reqs_bits_stage_optional | reqs_bits_stage_simultaneous);

									if (Constants.Requirements.enable_instant_work)
									{
										work.SetComplete();
										reqs_bits_stage_done.AddBit(req_index);
										no_delay = true;
										//if (req.flags.HasAny(Crafting.Requirement.Flags.Stepped))
										//{
										//	order.work_units_rem[work_counter] = 0;
										//}
									}

									order.cache_flags.TryAddFlag(Crafting.Order.CacheFlags.Needs_Workers, work_flags.HasNone(Work.Amount.Flags.Ambient | Work.Amount.Flags.Passive | Work.Amount.Flags.Crafter_Only));

									order.work_handles[work_counter] = req.work;
									order.work[work_counter] = work;

									work_counter++;
								}
							}
							break;
						}
					}
				}

				if (stage_option.color != 0) order.current_color = stage_option.color;

				order.requirements_bits_stage_mask = reqs_bits_stage_mask;
				order.requirements_bits_stage_done = reqs_bits_stage_done;
				order.requirements_bits_stage_always = reqs_bits_stage_always;

				is_shipment_dirty |= item_counter != 0;
				if (is_shipment_dirty & shipment.IsNotNull())
				{
					order.flags.AddFlag(Crafting.Order.Flags.Has_Shipment);
					shipment.items.ClearAfter((int)item_counter);

					shipment.flags.RemoveFlag(Shipment.Flags.Cached);
					shipment.Modified(entity, true);
				}

				//order.flags |= Crafting.Order.Flags.InProgress;

				order.flags.AddRemFlags(Crafting.Order.Flags.In_Progress, Crafting.Order.Flags.Cached);
				order.t_next_update = time + stage_option.start_delay;

				if (no_delay)
				{
					order.t_next_update = default;
				}

				dirty = true;
				mark_modified = true;

				Sound.Play(region: ref region_common,
					sound: stage_option.h_sound_begin,
					world_position: transform.position,
					volume: 0.80f);
			}
			else
			{
				if (order.flags.TryAddRemFlags(Crafting.Order.Flags.Complete, Crafting.Order.Flags.In_Progress))
				{
					//Sound.Play(region: ref region_common, sound: recipe_data.h_sound_complete, world_position: transform.position);
					//order.t_next_update = time + recipe_data.complete_delay;

					order.t_next_update = time + recipe_data.complete_delay;
					order.cache_flags = Crafting.Order.CacheFlags.None;

					dirty = true;
					mark_modified = true;

					Sound.Play(region: ref region_common,
						sound: order.h_sound_complete.ElseIfZero(recipe_data.h_sound_complete),
						world_position: transform.position,
						volume: 0.80f);
				}
			}
		}
#endif

		//		//[ISystem.Modified.Pair<Crafting.Order>.Wildcard(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global)]
		//		[ISystem.Modified(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global)]
		//		public static void ModifiedOrders(ISystem.Info.Common info, ref Region.Data.Common region, Entity entity,
		//		[Source.Owned] in Transform.Data transform, // [Source.Owned] ref Workshop.Data workshop,
		//		[Source.Owned, Pair.Wildcard] ref Crafting.Order order, IComponent.Handle h_order)
		//		{
		//#if SERVER
		//			if (order.flags.HasNone(Crafting.Order.Flags.Cached))
		//			{
		//				var sync = false;

		//				var done = true;
		//				foreach (ref var work in order.work)
		//				{
		//					done &= work.type == default || work.current >= work.required;
		//				}

		//				if (done)
		//				{
		//					order.flags.AddRemFlags(Crafting.Order.Flags.Complete, Crafting.Order.Flags.InProgress);
		//					Sound.Play(ref region, snd_complete, transform.position, volume: 0.50f, priority: 0.60f);

		//					sync = true;
		//				}

		//				if (sync)
		//				{
		//					h_order.Sync(entity);
		//				}
		//			}
		//#elif CLIENT

		//#endif

		//			App.WriteLine($"{info.Count}; {entity}; {info.FrameOffset} update {h_order.GetInfo().identifier_hjson},{h_order.GetTag()}");

		//			//if (sync)
		//			//{
		//			//	workshop.Sync(entity, true);
		//			//}
		//		}
		//#endif

#if CLIENT
		//public partial struct WorkerHUD: IGUICommand
		//{
		//	public Entity ent_worker;
		//	public Entity ent_transform_work;

		//	public Crafting.Order order;
		//	public Worker.Data worker;
		//	public Worker.State worker_state;

		//	public Transform.Data transform;
		//	public Transform.Data transform_work;

		//	public void Draw()
		//	{
		//		var pos_a = this.transform.GetInterpolatedPosition();
		//		var pos_b = this.transform_work.GetInterpolatedPosition();

		//		var dir = (pos_b - pos_a).GetNormalized(out var len);

		//		if (len <= Worker.max_distance * 2)
		//		{
		//			var cpos_a = pos_a.WorldToCanvas();
		//			var cpos_b = pos_b.WorldToCanvas();

		//			var scale = GUI.GetWorldToCanvasScale();
		//			var show = false;

		//			var color = GUI.font_color_yellow;
		//			var text_color = GUI.font_color_yellow;
		//			var text = new Utf8String("No work\navailable"u8);

		//			if (this.worker_state.work_index.TryGetValue(out var work_index))
		//			{
		//				if (len <= Worker.max_distance)
		//				{
		//					ref var recipe = ref this.order.h_recipe.GetData();
		//					if (recipe.IsNotNull())
		//					{
		//						ref var work = ref this.order.work[work_index];
		//						ref var req = ref recipe.requirements[work.req_index];

		//						var (amount_req, amount_raw) = req.GetRequirementAmount(order.amount_multiplier);
		//						var amount = amount_req - work.remaining;

		//						text = $"Working on:\n{this.order.amount_multiplier:0}x {recipe.GetShortName()}\n{amount:0}/{amount_req:0} {work.type.GetShortName()}";
		//						text_color = GUI.font_color_yellow;
		//						color = GUI.font_color_yellow;
		//						show = true;
		//					}
		//				}
		//				else
		//				{
		//					text = "Out of\nrange";
		//					text_color = GUI.font_color_red_b;
		//					color = GUI.font_color_red_b;
		//					show = true;
		//				}
		//			}

		//			if (show)
		//			{
		//				GUI.DrawLine2(cpos_b - (dir * 1 * scale), cpos_a + (dir * 1 * scale), color.WithAlphaMult(0.40f), color.WithAlphaMult(0.00f), thickness_a: 4.00f, thickness_b: 4.00f);
		//				GUI.DrawTextCentered(text, cpos_b, font: GUI.Font.Superstar, size: 16, color: text_color);
		//			}
		//		}
		//	}
		//}

		//[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("local", true, Source.Modifier.Shared)]
		//public static void OnWorkerHUD(ISystem.Info info, Entity entity, 
		//[Source.Owned] in Worker.Data worker, [Source.Owned] in Transform.Data transform)
		//{
		//	//if (worker.h_order != 0)
		//	//{
		//	//	//ref var order = ref worker.h_order
		//	//	//var gui = new WorkerHUD()
		//	//	//{
		//	//	//	ent_worker = entity,
		//	//	//	worker = worker,
		//	//	//	order = workshop.orders[ordex_index],
		//	//	//	transform = transform,
		//	//	//	transform_work = transform_work
		//	//	//};
		//	//	//gui.Submit();
			
		//	//}
		//}

		//[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("local", true, Source.Modifier.Shared)]
		//public static void OnWorkerHUD_Workshop(ISystem.Info info, Entity entity, Entity ent_transform_work, [Source.Owned] in Transform.Data transform,
		//[Source.Owned] ref Worker.Data worker, [Source.Work] in Transform.Data transform_work, [Source.Work] in Workshop.Data workshop)
		//{
		//	//if (worker.order_index.TryGetValue(out var ordex_index) && worker.work_index.TryGetValue(out var work_index))
		//	//{
		//	//	var gui = new WorkerHUD()
		//	//	{
		//	//		ent_worker = entity,
		//	//		ent_transform_work = ent_transform_work,
		//	//		worker = worker,
		//	//		order = workshop.orders[ordex_index],
		//	//		transform = transform,
		//	//		transform_work = transform_work
		//	//	};
		//	//	gui.Submit();
		//	//}
		//}

		//[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("local", true, Source.Modifier.Shared)]
		//public static void OnWorkerHUD_Construction(ISystem.Info info, Entity entity, Entity ent_transform_work, [Source.Owned] in Transform.Data transform,
		//[Source.Owned] ref Worker.Data worker, [Source.Work] in Transform.Data transform_work, [Source.Work] in Construction.Data construction)
		//{
		//	//if (worker.order_index.TryGetValue(out var ordex_index) && worker.work_index.TryGetValue(out var work_index))
		//	//{
		//	//	var gui = new WorkerHUD()
		//	//	{
		//	//		ent_worker = entity,
		//	//		ent_transform_work = ent_transform_work,
		//	//		worker = worker,
		//	//		order = construction.order,
		//	//		transform = transform,
		//	//		transform_work = transform_work
		//	//	};
		//	//	gui.Submit();
		//	//}
		//}

		//public partial struct WorkshopHUD: IGUICommand
		//{
		//	public Entity ent_workshop;
		//	public Transform.Data transform;
		//	public Workshop.Data workshop;

		//	public void Draw()
		//	{
		//		var pos = this.transform.GetInterpolatedPosition() - new Vector2(0, 1);
		//		var cpos = pos.WorldToCanvas();

		//		var complete_count = 0;
		//		foreach (ref var order in this.workshop.orders.AsSpan())
		//		{
		//			if (order.flags.HasAny(Crafting.Order.Flags.Complete))
		//			{
		//				complete_count++;
		//			}
		//		}

		//		if (complete_count > 0)
		//		{
		//			var text = $" {complete_count} order{(complete_count == 1 ? "" : "s")} ready\nto be collected";
		//			var text_color = GUI.font_color_green;

		//			GUI.DrawTextCentered(text, cpos, font: GUI.Font.Superstar, size: 16, color: text_color);
		//		}
		//	}
		//}

		//[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region)]
		//public static void OnWorkshopHUD(ISystem.Info info, Entity entity,
		//[Source.Owned] in Transform.Data transform, [Source.Owned] in Workshop.Data workshop)
		//{
		//	var gui = new WorkshopHUD()
		//	{
		//		ent_workshop = entity,
		//		transform = transform,
		//		workshop = workshop
		//	};
		//	gui.Submit();
		//}
#endif
	}
}
