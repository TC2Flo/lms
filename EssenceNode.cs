
using Keg;

namespace TC2.Base.Components
{
	public static partial class EssenceNode
	{
		[Flags]
		public enum Flags: ushort
		{
			None = 0,

			Collapsing = 1 << 0,
			Full_Collapse = 1 << 1
		}

		// TODO: optimize the struct layout so the float blocks are 16-byte aligned
		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public partial struct Data(): IComponent
		{
			public IEssence.Handle h_essence;
			public EssenceNode.Flags flags;

			[Save.NewLine]
			public float amount;
			public float stability = 1.00f;
			public float volatility = 0.10f;

			[Save.Ignore, Net.Ignore] public float t_last_collapse;
			[Save.Ignore, Net.Ignore] public float t_next_collapse;
			[Save.Ignore, Net.Ignore] public float next_zap;
			[Save.Ignore, Net.Ignore] public float unused_00;
		}

		//[IEvent.Data]
		//public partial struct ChargeEvent: IEvent
		//{
		//	public Entity ent_drainer = default;
		//	public Vector2 world_position = default;
		//	public float power = 0.00f;

		//	public ChargeEvent()
		//	{

		//	}
		//}

		[IEvent.Data]
		public partial struct CollapseEvent(): IEvent
		{
			public Vector2 world_position;
			public IEssence.Handle h_essence;
			public float essence_amount_returned = 0.00f;
			public float essence_amount_precipitated = 0.00f;
			public XorRandom random;
		}

		[IEvent.Data]
		public partial struct FailureEvent(): IEvent
		{
			public Vector2 world_position;
			public float power = 0.00f;

		}

		//		[ISystem.Event<EssenceNode.ChargeEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
		//		public static void OnCharge(ISystem.Info info, Entity entity, ref EssenceNode.ChargeEvent data, [Source.Owned] ref Transform.Data transform, [Source.Owned] ref EssenceNode.Data essence_node, [Source.Owned, Pair.Component<EssenceNode.Data>] ref Light.Data light, [Source.Owned] ref Body.Data body)
		//		{
		//			if (info.WorldTime >= essence_node.next_drain && !essence_node.flags.HasAll(EssenceNode.Flags.Collapsing))
		//			{
		//				essence_node.last_drain = info.WorldTime;

		//				essence_node.charge = Maths.Clamp01(essence_node.charge + (data.power * App.fixed_update_interval_s));
		//				if (essence_node.charge >= 1.00f)
		//				{
		//#if SERVER
		//					var amount = Maths.Min(1.00f * App.fixed_update_interval_s, essence_node.amount);

		//					essence_node.amount -= amount;

		//					essence_node.flags.SetFlag(EssenceNode.Flags.Collapsing, true);
		//					essence_node.Sync(entity);

		//					var dir = (data.world_position - transform.position).GetNormalized(out var dist);
		//					body.AddForce(-dir * (body.GetMass() * 15.00f * App.tickrate));
		//#endif
		//				}
		//			}
		//		}

		public static readonly Texture.Handle texture_glow_failure_00 = "essence.glow.failure.00";
		public static readonly Texture.Handle texture_glow_failure_01 = "essence.glow.failure.01";

		[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnUpdateControl(ISystem.Info info, Entity entity, ref Region.Data region, ref XorRandom random,
		[Source.Owned] ref EssenceNode.Data essence_node, [Source.Owned, Pair.Component<EssenceNode.Data>] ref Light.Data light,
		[Source.Owned] in Transform.Data transform, [Source.Owned] in Control.Data control, [Source.Owned] ref Body.Data body)
		{
			if (control.mouse.GetKey(Mouse.Key.Right))
			{
				essence_node.stability = Maths.MoveTowards(essence_node.stability, Maths.Pow2(1.00f - essence_node.volatility), essence_node.volatility * info.DeltaTime * 0.50f);
			}

			if (control.mouse.GetKey(Mouse.Key.Left) && info.WorldTime >= essence_node.t_next_collapse && essence_node.stability <= 0.98f)
			{
				ref var essence_data = ref essence_node.h_essence.GetData();
				if (essence_data.IsNotNull())
				{
					var amount = essence_node.amount * (1.00f - essence_node.stability).Clamp01();

					if (control.mouse.GetKeyDown(Mouse.Key.Left))
					{
#if SERVER
						essence_node.flags.SetFlag(EssenceNode.Flags.Collapsing, true);
						//App.WriteLine($"{essence_node.stability}; {essence_node.amount}; {essence_node.volatility}");
						//essence_node.Sync(entity, true);
#endif
					}

					//					switch (essence_data.type_tmp)
					//					{
					//						case Essence.Type.Motion:
					//						{
					//							if (control.mouse.GetKeyDown(Mouse.Key.Left))
					//							{
					//#if SERVER
					//								essence_node.flags.SetFlag(EssenceNode.Flags.Collapsing, true);
					//								App.WriteLine($"{essence_node.stability}; {essence_node.amount}; {essence_node.volatility}");
					//								//essence_node.Sync(entity, true);
					//#endif
					//							}
					//						}
					//						break;

					//						case Essence.Type.Heat:
					//						{
					//							Span<OverlapResult> results = stackalloc OverlapResult[4];
					//							if (region.TryOverlapPointAll(transform.position, 1.00f, ref results, mask: Physics.Layer.Flammable, exclude: Physics.Layer.Essence))
					//							{
					//								foreach (ref var result in results)
					//								{
					//									var ent_result = result.entity;
					//									App.WriteLine(amount);
					//									//ent_result.Ignite(amount, Fire.Flags.None);
					//								}
					//							}
					//						}
					//						break;
					//					}

				}

				//essence_node.next_zap = info.WorldTime + 1.00f;
			}
		}

		[ISystem.Event<EssenceNode.CollapseEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnCollapseEvent(ref Region.Data region, ISystem.Info info, Entity entity, ref EssenceNode.CollapseEvent data, 
		[Source.Owned] ref Transform.Data transform, [Source.Owned] ref EssenceNode.Data essence_node, 
		[Source.Owned, Pair.Component<EssenceNode.Data>] ref Light.Data light, [Source.Owned] ref Body.Data body)
		{
			ref var random = ref data.random;

			//var essence_amount_precipitated = data.essence_amount * random.NextFloatExtra(0.50f, Maths.Pow(essence_node.volatility, 3.00f) * 0.50f).Clamp01(); // * 0.20f;



			ref var essence_data = ref data.h_essence.GetData();
			if (essence_data.IsNotNull())
			{
				Collapse(ref region, ref random, data.h_essence, entity, transform.position, data.essence_amount_precipitated);

#if SERVER
				essence_node.amount = Maths.Max(essence_node.amount + data.essence_amount_returned, 0.00f);

				//if (essence_node.amount <= 1.00f)
				//{
				//	//entity.Delete();
				//}
				//else
				//{
				//	essence_node.Sync(entity);
				//}

				essence_node.Sync(entity, true);
#endif

				if (essence_node.flags.HasNone(EssenceNode.Flags.Full_Collapse))
				{
					body.AddForce(random.NextUnitVector2Range(0.50f, 1.00f) * (body.GetMass() * Maths.Lerp01(25.00f, 75.00f, essence_node.volatility * 4.00f) * App.tickrate_f32));
				}

				//data.essence_amount *= 0.10f;
			}
		}

		[Shitcode] // TODO: this sucks greatly
		public static void Collapse(ref Region.Data region, ref XorRandom random, IEssence.Handle h_essence, Entity entity, Vector2 world_position, float amount)
		{
			ref var essence_data = ref h_essence.GetData();
			if (essence_data.IsNotNull())
			{
				var power2 = Maths.Ceil(amount * 20.00f); // * 0.20f;
				if (power2 >= 10.00f)
				{
					//App.WriteLine($"precipitated: {essence_amount_precipitated}; power: {power2}");

					switch (essence_data.type_tmp)
					{
						case Essence.Type.Motion:
						{
							//var max_radius = MathF.Sqrt(power2) * 0.50f;
							//var power = MathF.Sqrt(max_radius);
							//var power_sqrt = MathF.Sqrt(power);

							var power = Maths.Cbrt(amount * 9.00f * Maths.tau2_inv);
							var power_sqrt = Maths.Sqrt(power);
							var max_radius = power * 1.20f;
							var shockwave_radius = Maths.PowFast(max_radius, 1.50f) * 1.20f;


							//App.WriteLine($"amount: {amount:0.00}; precipitated: power2: {power2}; max_radius: {max_radius}; power: {power}; power_sqrt: {power_sqrt}; shockwave_radius: {shockwave_radius}");

							var results = FixedArray.CreateSpan64NoInit<OverlapResult>(out var results_buffer); // stackalloc OverlapResult[64];
							if (region.TryOverlapPointAll(world_position: world_position,
							radius: shockwave_radius,
							hits: ref results,
							mask: Physics.Layer.Dynamic,
							exclude: Physics.Layer.Essence | Physics.Layer.Static | Physics.Layer.World))
							{
								//App.WriteLine(results.Length);
								foreach (ref var result in results)
								{
									ref var hit_body = ref result.GetBody(); // TODO: limit and apply force directly to OverlapResult
									if (hit_body.IsNotNull())
									{
										var dir = (result.world_position - world_position).GetNormalized(out var dist);
										var ratio = 1.00f - Maths.Normalize01(dist, shockwave_radius * 1.80f);
										//ratio *= ratio;

										//var force = dir * 4000.00f * ratio;
										var force = dir * power2 * ratio * 2500.00f * App.tickrate_f32;
										force = Physics.LimitForce2B(ref hit_body, force, new Vector2(30, 20));
										force *= random.NextFloatRange(0.50f, 1.00f);

										hit_body.AddForceWorld(force, Maths.Avg(hit_body.GetPosition(), result.world_position));
									}
								}
							}

#if SERVER
							if (power2 >= 50.00f)
							{
								var args = (pitch_mult: random.NextFloatRange(0.90f, 1.00f), color: essence_data.color_emit);
								region.SpawnPrefab("explosion", world_position).ContinueWith(ent =>
								{
									ref var explosion = ref ent.GetComponent<Explosion.Data>();
									if (!explosion.IsNull())
									{
										explosion.power = power * 0.70f;
										explosion.radius = max_radius * 1.90f;
										explosion.damage_entity = Maths.Pow(power2 * 2.00f, 1.10f) * 1.50f;
										explosion.damage_terrain = Maths.Pow(power2 * 8.00f, 0.93f) * 3.00f;
										explosion.damage_type = Damage.Type.Motion_Impulse;
										explosion.damage_type_secondary = Damage.Type.Shockwave;
										//explosion.ent_owner = entity;
										explosion.fire_amount = 0.00f;
										explosion.smoke_amount = 0.00f;
										explosion.smoke_lifetime_multiplier = 0.80f;
										explosion.smoke_velocity_multiplier = 1.00f;
										explosion.flash_duration_multiplier = power * 0.06f;
										explosion.flash_intensity_multiplier = power_sqrt * 0.10f;
										explosion.sparks_amount = 0.00f;
										explosion.volume = power * 0.25f;
										explosion.pitch = Maths.Lerp01(4.00f, 0.80f, power * 0.10f) * args.pitch_mult;
										explosion.shake_multiplier = power_sqrt * 0.10f;
										explosion.force_multiplier = Maths.Clamp(power_sqrt, 0.50f, 1.50f);
										//explosion.ent_ignored = explosion_tmp.ent_ignored;

										//App.WriteLine($"dmg: {explosion.damage_entity}; {explosion.damage_terrain}; {explosion.radius}; {explosion.power}; {power2}");

										explosion.Sync(ent, true);
									}

									ref var light = ref ent.GetTrait<Explosion.Data, Light.Data>();
									if (!light.IsNull())
									{
										light.color = ColorBGRA.Lerp(args.color, ColorBGRA.White, 0.20f) * 4.00f;
										light.intensity = power;
									}
								});
							}
#endif

#if CLIENT
							Shake.Emit(ref region, world_position, power_sqrt * 0.30f, power_sqrt * 0.30f, radius: max_radius * 3.00f);
							Sound.Play("essence.drain.zap.00", world_position, volume: power_sqrt * 0.80f, pitch: Maths.Max(Maths.Lerp01(1.30f, 0.10f, power * 0.05f) * random.NextFloatRange(0.90f, 1.00f), 0.30f), size: max_radius * 0.20f, dist_multiplier: power_sqrt * 0.50f, priority: 0.65f);

							if (power2 >= 50.00f)
							{
								Sound.Play("essence.drain.zap.00", world_position, volume: power * 1.50f, pitch: Maths.Max(Maths.Lerp01(1.00f, 0.05f, power * 0.08f) * random.NextFloatRange(0.90f, 1.00f), 0.30f), size: max_radius * power, dist_multiplier: power_sqrt * max_radius * 0.40f, priority: 0.75f);
							}

							if (power2 >= 2750.00f)
							{
								Sound.Play("essence.blast.00", world_position, volume: power * 1.00f, pitch: Maths.Max(0.30f, Maths.Lerp01(7.00f, 1.00f, power * 0.07f)) * random.NextFloatRange(0.90f, 1.00f), size: max_radius * power * 3.00f, dist_multiplier: power * 2.00f, priority: 0.90f);
							}

							var smoke_count = (int)(max_radius * 1.00f * power_sqrt);
							for (var i = 0; i < smoke_count; i++)
							{
								Particle.Spawn(ref region, new Particle.Data()
								{
									texture = Essence.texture_smoke,
									lifetime = random.NextFloatRange(0.15f, power * 0.25f),
									pos = world_position + random.NextVector2(0.40f),
									vel = random.NextUnitVector2Range(max_radius * 0.12f, max_radius * power_sqrt * 0.70f) * 2.00f,
									force = random.NextUnitVector2Range(20.00f, 40.00f),
									fps = random.NextByteRange(15, 20),
									frame_count = 64,
									frame_count_total = 64,
									frame_offset = random.NextByteRange(0, 64),
									scale = random.NextFloatRange(0.40f, 0.70f) * power_sqrt * 0.50f,
									//rotation = random.NextFloat(10.00f),
									angular_velocity = random.NextFloat(5.00f) / power,
									growth = random.NextFloatRange(1.00f, 5.00f) * power_sqrt * 0.50f,
									drag = random.NextFloatRange(0.02f, 0.03f),
									color_a = ColorBGRA.ARGB(0.80f, 0.90f, 0.90f, 0.90f).WithAlphaMult(random.NextFloatRange(0.60f, 1.00f) * Maths.Clamp01(power_sqrt * 0.50f)),
									color_b = new Color32BGRA(0, 240, 240, 240),
									stretch = new Vector2(random.NextFloatRange(0.80f, 1.00f), random.NextFloatRange(1.00f, 1.30f) * power_sqrt * 0.50f),
									face_dir_ratio = 0.80f,
								});
							}
#endif
						}
						break;

						case Essence.Type.Life:
						{
							//var max_radius = MathF.Sqrt(power2);
							//var power = MathF.Sqrt(max_radius);
							//var power_sqrt = MathF.Sqrt(power);
							var max_radius = Maths.Cbrt(amount * 9.00f * Maths.tau2_inv);
							var power = max_radius;
							var power_sqrt = Maths.Sqrt(power);

							Span<OverlapResult> results = stackalloc OverlapResult[32];
							if (region.TryOverlapPointAll(world_position, max_radius, ref results, mask: Physics.Layer.Organic | Physics.Layer.Creature, exclude: Physics.Layer.Essence))
							{
								foreach (ref var result in results)
								{
#if SERVER
									ref var tumor = ref result.entity.GetOrAddComponent<Tumor.Data>(sync: true);
									if (!tumor.IsNull())
									{

									}
#endif
								}
							}

#if SERVER
							ref var terrain = ref region.GetTerrain();

							static void SetTileFunc(ref Tile tile, int x, int y, byte mask, ref (int, int) arg)
							{
								if (tile.BlockID != 0 && tile.Flags.HasNone(TileFlags.Solid))
								{
									//ref var block = ref tile.Block;
									//if (block.IsNotNull() && block.block_mithril.id != 0)
									//{
									//	//tile.Reset();

									//	//tile.Health = 255;
									//	tile.BlockID = block.block_mithril.id;
									//}
								}
							}

							var arg = (a: 10, b: 10);
							//terrain.IterateMask(world_position, new Vector2(max_radius * App.pixels_per_unit * 0.25f), Explosion.tex_mask_explosion.GetRandom(ref random), ref arg, SetTileFunc, dirty_flags: Chunk.DirtyFlags.Sync | Chunk.DirtyFlags.Collider | Chunk.DirtyFlags.Neighbours, iteration_flags: Terrain.IterationFlags.None);
#endif

#if CLIENT
							Shake.Emit(ref region, world_position, power * 0.20f, power_sqrt * 0.40f, radius: max_radius * 2.00f);
							Sound.Play("essence.collapse.life.00", world_position, volume: power_sqrt * 0.80f, pitch: Maths.Lerp01(1.20f, 0.10f, power * 0.05f) * random.NextFloatRange(0.90f, 1.00f), size: max_radius * 0.20f, dist_multiplier: power_sqrt * 0.50f);

							if (power2 >= 3000.00f)
							{
								Sound.Play("essence.blast.00", world_position, volume: power * 0.10f, pitch: Maths.Max(0.30f, Maths.Lerp01(7.00f, 1.00f, power * 0.07f)) * random.NextFloatRange(0.90f, 1.00f), size: max_radius * 1.00f, dist_multiplier: power * 2.00f);
							}

							var smoke_count = (int)(max_radius * 0.50f);
							for (var i = 0; i < smoke_count; i++)
							{
								Particle.Spawn(ref region, new Particle.Data()
								{
									texture = Essence.texture_smoke,
									lifetime = random.NextFloatRange(0.30f, power_sqrt * 0.45f),
									pos = world_position + random.NextVector2(0.40f),
									vel = random.NextUnitVector2Range(max_radius * 0.50f, max_radius) * 0.10f,
									force = random.NextUnitVector2Range(10.00f, 40.00f),
									fps = random.NextByteRange(15, 30),
									frame_count = 64,
									frame_count_total = 64,
									frame_offset = random.NextByteRange(0, 64),
									scale = random.NextFloatRange(0.70f, 1.50f) * power,
									rotation = random.NextFloat(2.00f),
									angular_velocity = random.NextFloat(4.00f),
									growth = -random.NextFloatRange(0.50f, 2.00f),
									drag = random.NextFloatRange(0.06f, 0.10f),
									color_a = essence_data.color_emit.WithAlphaMult(0.40f),
									color_b = essence_data.color_emit.WithAlphaMult(0.10f),
									stretch = new Vector2(random.NextFloatRange(0.20f, 1.00f), random.NextFloatRange(0.70f, 1.80f)),
									lit = random.NextFloatRange(0.10f, 0.80f),
									face_dir_ratio = random.NextFloatRange(0.10f, 0.90f),
								});

								Particle.Spawn(ref region, new Particle.Data()
								{
									texture = Essence.texture_glow,
									lifetime = random.NextFloatRange(0.10f, power_sqrt * 0.15f),
									pos = world_position + random.NextVector2(0.40f),
									force = random.NextUnitVector2Range(20.00f, 40.00f),
									fps = random.NextByteRange(10, 15),
									frame_count = 2,
									frame_count_total = 2,
									frame_offset = random.NextByteRange(0, 64),
									scale = random.NextFloatRange(0.40f, 2.00f),
									rotation = random.NextFloat(10.00f),
									angular_velocity = random.NextFloat(10.00f),
									growth = random.NextFloatRange(60.00f, 180.00f),
									drag = random.NextFloatRange(0.06f, 0.10f),
									color_a = essence_data.color_emit * 1.00f,
									color_b = new Color32BGRA(0, 240, 240, 240),
									stretch = new Vector2(random.NextFloatRange(0.20f, 1.00f), random.NextFloatRange(0.70f, 0.80f)),
									//face_dir_ratio = 0.50f,
									glow = random.NextFloatRange(5.00f, 100.00f)
								});
							}
#endif
						}
						break;

						case Essence.Type.Heat:
						{
							//var max_radius = MathF.Sqrt(power2 * 0.25f);
							//var max_radius = Maths.Cbrt(amount * 9.00f * Maths.tau2_inv);
							//var power = max_radius;
							//var power_sqrt = Maths.Sqrt(power);
							////var max_radius2 = Maths.Pow((3.00f * essence_amount_precipitated) / (MathF.PI * 4.00f), 1.00f / 3.00f);
							//var max_radius2 = Maths.Pow((3.00f * essence_amount_precipitated) / (MathF.PI * 4.00f), 1.00f / 3.00f);

							//App.WriteLine($"precipitated: {essence_amount_precipitated}; power2: {power2}; max_radius: {max_radius}; power: {power}; power_sqrt: {power_sqrt}; max_radius2: {max_radius2}");
							//App.WriteLine($"precipitated: {data.essence_amount_precipitated}; power2: {power2}; max_radius: {max_radius}; power: {power}; power_sqrt: {power_sqrt}");

							var power = Maths.Cbrt(amount * 9.00f * Maths.tau2_inv);
							var power_sqrt = Maths.Sqrt(power);
							var power2_sqrt = Maths.Sqrt(power2);
							var max_radius = power_sqrt * 2.00f; // * 1.20f;
							var max_radius_inv = max_radius.Rcp();
							var shockwave_radius = power * 1.50f; // Maths.PowFast(power, 0.75f) * 1.20f;
							var shockwave_radius_inv = shockwave_radius.Rcp(); // Maths.PowFast(power, 0.75f) * 1.20f;

							//App.WriteValue("essence boom", (amount, power, power_sqrt, power2, power2_sqrt, max_radius, shockwave_radius));
#if SERVER

							//var results_capacity = (int)Maths.Clamp(max_radius * power_sqrt, 16, 256);
							//Span<OverlapResult> results = stackalloc OverlapResult[results_capacity];

							//region.DrawDebugCircle(world_position, radius: shockwave_radius, color: Color32BGRA.Yellow, filled: true);
							//region.DrawDebugCircle(world_position, radius: max_radius, color: Color32BGRA.Red, filled: true);

							var results = FixedArray.CreateSpan64NoInit<OverlapResult>(out var results_buffer);
							if (region.TryOverlapPointAll(world_position: world_position, radius: shockwave_radius, hits: ref results, 
							mask: Physics.Layer.Flammable | Physics.Layer.Heatable | Physics.Layer.Destructible, exclude: Physics.Layer.Essence))
							{
								foreach (ref var result in results)
								{
									var ent_result = result.entity;
									var result_layer = result.layer;
									var result_dist = result.distance;
									//var result_modifier = (1.00f - (result_dist * shockwave_radius_inv).Clamp01().Pow2())
									var result_modifier = (1.00f - (result_dist * shockwave_radius_inv).Clamp01()).Pow2();

									var result_damage = result_modifier * power2;
									if (result_modifier >= 0.30f && result_layer.HasAny(Physics.Layer.Flammable | Physics.Layer.Burning | Physics.Layer.Heatable))
									{
										//ent_result.Ignite(intensity: result_modifier * power * 100, damage: result_damage, flags: Fire.Flags.None, damage_type: Damage.Type.Phlogiston, temperature: 6000);
										ent_result.Ignite(intensity: power * 2.00f, 
											damage: result_damage, flags: Fire.Flags.None,
											damage_type: Damage.Type.Phlogiston, temperature: 6000);
									}

									if (result_layer.HasAny(Physics.Layer.Heatable))
									{
										ref var heat_state = ref ent_result.GetComponent<Heat.State>();
										if (heat_state.IsNotNull())
										{
											//skip_damage = true;

											heat_state.AddEnergy(result_damage * 15.00f);
											heat_state.Sync(ent_result, true);

											//damage *= 0.50f;
										}
									}

									Damage.Hit(ent_attacker: entity, ent_owner: default, ent_target: ent_result,
									position: result.world_position, velocity: result.gradient * random.NextFloatExtra(10.00f, 2.50f), normal: -result.normal_raw,
									damage_integrity: result_damage * 1.10f, damage_durability: result_damage, damage_terrain: result_damage * 0.90f,
									target_material_type: result.material_type, damage_type: Damage.Type.Heat,
									flags: Damage.Flags.No_Loot_Pickup | Damage.Flags.No_Loot_Notification,
									yield: 0.80f, size: Maths.Min(power_sqrt * 0.50f, 2.00f), impulse: 0.00f, pain: 10.00f, stun: 0.50f);

									//App.WriteValue((power_sqrt * 0.50f, result_damage, result.material_type));
								}
							}

							// TODO: dumb
							if (power2 >= 10.00f)
							{
								var args = (pitch_mult: random.NextFloatRange(0.90f, 1.00f), color: essence_data.color_emit);
								region.SpawnPrefab("explosion", world_position).ContinueWith(ent =>
								{
									ref var explosion = ref ent.GetComponent<Explosion.Data>();
									if (explosion.IsNotNull())
									{
										explosion.power = Maths.Pow(power, 0.50f) * 1.25f; // * 1.80f;
										explosion.radius = max_radius;
										explosion.damage_entity = Maths.Pow(power2 * 1.40f, 0.78f) * 1.50f;
										explosion.damage_terrain = Maths.Pow(power2 * 1.50f, 0.90f) * 1.70f;
										explosion.damage_type = Damage.Type.Explosion;
										explosion.damage_type_secondary = Damage.Type.Heat;
										explosion.fire_amount = 0.20f;
										explosion.smoke_amount = 1.50f;
										explosion.smoke_lifetime_multiplier = 0.50f;
										explosion.smoke_velocity_multiplier = 2.80f;
										explosion.flash_duration_multiplier = power_sqrt * 2.50f;
										explosion.flash_intensity_multiplier = power_sqrt * 0.080f;
										explosion.smoke_color = new Color32BGRA(240, 255, 255, 255);
										explosion.sparks_amount = 2.00f;
										explosion.volume = Maths.Clamp(power * 0.50f, 0.90f, 10.00f);
										explosion.pitch = Maths.Lerp01(1.40f, 0.90f, power * 0.10f) * args.pitch_mult;
										explosion.force_multiplier = power_sqrt * 0.45f;
										explosion.shake_multiplier = power_sqrt * 0.10f;

										//App.WriteValue((explosion.power, explosion.radius, explosion.damage_terrain, explosion.damage_entity, explosion.force_multiplier, explosion.shake_multiplier));

										//App.WriteLine($"power: {explosion.power}; damage ent: {explosion.damage_entity}; damage ter: {explosion.damage_terrain}");

										explosion.Sync(ent, true);
									}

									ref var light = ref ent.GetTrait<Explosion.Data, Light.Data>();
									if (light.IsNotNull())
									{
										light.color = ColorBGRA.Lerp(args.color, ColorBGRA.White, 0.20f) * 4.00f;
										light.intensity = power;
									}
								});
							}
#endif

#if CLIENT
							//Shake.Emit(ref region, world_position, power * 0.50f, power * 0.50f, radius: max_radius * 4.00f);
							//Sound.Play("essence.collapse.heat.00", world_position, volume: power, pitch: Maths.Lerp01(2.50f, 0.50f, power * 0.05f) * random.NextFloatRange(0.90f, 1.00f), size: max_radius * 0.50f, dist_multiplier: power);

							//if (power2 >= 100.00f)
							//{
							//	Sound.Play("essence.drain.zap.00", world_position, volume: power * 1.50f, pitch: Maths.Lerp01(1.00f, 0.45f, power * 0.08f) * random.NextFloatRange(0.90f, 1.00f) * 1.75f, size: max_radius * 1.00f, dist_multiplier: power * 2.00f);
							//}

							//if (power2 >= 2500.00f)
							//{
							//	Sound.Play("essence.blast.00", world_position, volume: power * 0.10f, pitch: Maths.Max(0.30f, Maths.Lerp01(7.00f, 1.00f, power * 0.07f)) * random.NextFloatRange(0.90f, 1.00f), size: max_radius * 1.00f, dist_multiplier: power * 2.00f);
							//}

							//var smoke_count = (int)(max_radius * 1.50f);
							//for (var i = 0; i < smoke_count; i++)
							//{
							//	Particle.Spawn(ref region, new Particle.Data()
							//	{
							//		texture = Essence.texture_smoke,
							//		lifetime = 0.70f + random.NextFloatRange(0.20f, power_sqrt * 0.70f),
							//		pos = world_position + random.NextVector2(0.40f),
							//		vel = random.NextUnitVector2Range(max_radius * 0.50f, max_radius * power_sqrt * 3.40f),
							//		force = random.NextUnitVector2Range(20.00f, 40.00f),
							//		fps = random.NextByteRange(9, 16),
							//		frame_count = 64,
							//		frame_count_total = 64,
							//		frame_offset = random.NextByteRange(0, 64),
							//		scale = random.NextFloatRange(0.30f, 0.40f),
							//		rotation = random.NextFloat(10.00f),
							//		angular_velocity = random.NextFloat(0.20f),
							//		growth = random.NextFloatRange(8.00f, 10.00f),
							//		drag = random.NextFloatRange(0.06f, 0.10f),
							//		color_a = ColorBGRA.ARGB(1.00f, 0.90f, 0.90f, 0.90f).WithAlphaMult(random.NextFloatRange(0.50f, 1.00f) * Maths.Clamp01(power_sqrt * 0.50f)),
							//		color_b = new Color32BGRA(0, 240, 240, 240),
							//		stretch = new Vector2(random.NextFloatRange(0.80f, 1.00f), random.NextFloatRange(0.70f, 0.80f)),
							//		face_dir_ratio = 0.50f,
							//	});
							//}
#endif

							//#if SERVER
							//						region.SpawnPrefab("explosion", world_position).ContinueWith(x =>
							//						{
							//							ref var explosion = ref x.GetComponent<Explosion.Data>();
							//							if (!explosion.IsNull())
							//							{
							//								explosion.power = MathF.Log2(power2 * 0.50f); // 4;
							//								explosion.radius = MathF.Sqrt(power2);
							//								explosion.damage_entity = MathF.Sqrt(power2) * 400.00f;
							//								explosion.damage_terrain = MathF.Sqrt(power2) * 1500.00f;
							//								explosion.smoke_amount = 0;
							//								explosion.smoke_lifetime_multiplier = 0;
							//								explosion.smoke_velocity_multiplier = 0;
							//								explosion.sparks_amount = MathF.Log2(power2 * 0.50f) * 0.50f;
							//								explosion.volume = 0.50f;
							//								explosion.pitch = 2;

							//								explosion.Sync(x);
							//							}
							//						});
							//#endif
						}
						break;

						case Essence.Type.Radiance:
						{
							//var max_radius = MathF.Sqrt(power2);
							//var power = MathF.Sqrt(max_radius);
							//var power_sqrt = MathF.Sqrt(power);

							var max_radius = Maths.Cbrt(amount * 9.00f * Maths.tau2_inv);
							var power = max_radius;
							var power_sqrt = Maths.Sqrt(power);

							//App.WriteLine($"precipitated; power2: {power2}; max_radius: {max_radius}; power: {power}; power_sqrt: {power_sqrt}");

#if SERVER
							if (power2 >= 200.00f)
							{
								var args = (pitch_mult: random.NextFloatRange(0.90f, 1.00f), color: essence_data.color_emit);
								region.SpawnPrefab("explosion", world_position).ContinueWith(ent =>
								{
									//ref var explosion = ref x.GetComponent<Explosion.Data>();
									//if (!explosion.IsNull())
									//{
									//	explosion.power = power;
									//	explosion.radius = max_radius * 1.00f;
									//	explosion.damage_entity = Maths.Pow(power2, 2.00f) * 0.10f;
									//	explosion.damage_terrain = Maths.Pow(power2, 1.50f) * 0.10f;
									//	explosion.damage_type = Damage.Type.Fire;
									//	explosion.ent_owner = entity;
									//	explosion.fire_amount = 0.50f;
									//	explosion.smoke_amount = 0.50f;
									//	explosion.smoke_lifetime_multiplier = 0.50f;
									//	explosion.smoke_velocity_multiplier = 1.00f;
									//	explosion.flash_duration_multiplier = 0.20f;
									//	explosion.flash_intensity_multiplier = power_sqrt * 0.25f;
									//	explosion.sparks_amount = 0.00f;
									//	explosion.shake_multiplier = 0.01f;
									//	explosion.volume = power * 0.15f;
									//	explosion.pitch = Maths.Lerp01(8.00f, 0.80f, power * 0.10f) * args.pitch_mult;
									//	//explosion.ent_ignored = explosion_tmp.ent_ignored;

									//	explosion.Sync(x);
									//}

									ref var explosion = ref ent.GetComponent<Explosion.Data>();
									if (explosion.IsNotNull())
									{
										explosion.power = power * 1.70f;
										explosion.radius = max_radius * 0.50f;
										explosion.damage_entity = Maths.Pow(power2, 0.80f) * 2.50f;
										explosion.damage_terrain = Maths.Pow(power2, 0.90f) * 2.20f;
										explosion.damage_type = Damage.Type.Radiation;
										explosion.damage_type_secondary = Damage.Type.Fire;
										//explosion.ent_owner = entity;
										explosion.fire_amount = 0.00f;
										explosion.smoke_amount = 0.50f;
										explosion.smoke_lifetime_multiplier = 0.30f;
										explosion.smoke_velocity_multiplier = 2.00f;
										explosion.flash_duration_multiplier = power * 0.06f;
										explosion.flash_intensity_multiplier = power_sqrt * 0.15f;
										explosion.smoke_color = new Color32BGRA(240, 255, 255, 255);
										explosion.sparks_amount = 1.00f;
										explosion.volume = power * 0.15f;
										explosion.pitch = Maths.Lerp01(8.00f, 1.50f, power * 0.01f) * args.pitch_mult;
										explosion.force_multiplier *= 0.30f;
										//explosion.ent_ignored = explosion_tmp.ent_ignored;

										//App.WriteLine($"damage ent: {explosion.damage_entity}; damage ter: {explosion.damage_terrain}");

										explosion.Sync(ent, true);
									}

									ref var light = ref ent.GetTrait<Explosion.Data, Light.Data>();
									if (light.IsNotNull())
									{
										light.color = args.color * 1.00f;
										light.intensity = power;
									}
								});
							}
#endif

#if CLIENT
							Particle.Spawn(ref region, new Particle.Data()
							{
								texture = Light.tex_light_circle_04,
								lifetime = power_sqrt * 0.20f,
								pos = world_position,
								scale = power_sqrt * 1.00f,
								growth = random.NextFloatRange(3.00f, 4.00f) * 200,
								color_a = essence_data.color_emit.WithAlphaMult(power * 2),
								color_b = ColorBGRA.None,
								glow = 10.00f
							});

							//var particle_count = (int)(max_radius);
							//for (var i = 0; i < particle_count; i++)
							//{
							//	Particle.Spawn(ref region, new Particle.Data()
							//	{
							//		texture = "light.circle.04",
							//		lifetime = random.NextFloatRange(0.10f, power_sqrt * 0.20f),
							//		pos = world_position + random.NextVector2(0.40f),
							//		vel = random.NextUnitVector2Range(max_radius * 0.10f, max_radius * power_sqrt * 0.50f),
							//		force = random.NextUnitVector2Range(5.00f, 10.00f),
							//		scale = random.NextFloatRange(1.00f, 2.00f) * (power_sqrt * 1.00f),
							//		growth = random.NextFloatRange(1.00f, 4.00f) * 500,
							//		drag = random.NextFloatRange(0.06f, 0.10f),
							//		color_a = Essence.GetColor(Essence.Type.Radiance).WithAlphaMult(power),
							//		color_b = Vector4.Zero,
							//		glow = random.NextFloatRange(5.00f, 20.00f)
							//	});
							//}

							Shake.Emit(ref region, world_position, power * 0.05f, power * 0.10f, radius: max_radius * 1.00f);
							Sound.Play("essence.collapse.radiance.00", world_position, volume: power_sqrt * 0.90f, pitch: Maths.Lerp01(4.00f, 1.00f, power * 0.10f) * random.NextFloatRange(0.90f, 1.00f), size: max_radius * 1.50f, dist_multiplier: power * 0.70f, priority: 0.80f);
#endif
						}
						break;

						case Essence.Type.Electricity:
						{
							//var max_radius = MathF.Sqrt(power2);
							//var power = MathF.Sqrt(max_radius);
							//var power_sqrt = MathF.Sqrt(power);

							var max_radius = Maths.Cbrt(amount * 9.00f * Maths.tau2_inv);
							var power = max_radius;
							var power_sqrt = Maths.Sqrt(power);

							//App.WriteLine($"precipitated: {power2}; {max_radius}; {power}; {power_sqrt}");

#if SERVER
							if (power2 >= 50.00f)
							{
								var args = (pitch_mult: random.NextFloatRange(0.80f, 1.20f), color: essence_data.color_emit);
								region.SpawnPrefab("explosion", world_position).ContinueWith(ent =>
								{
									ref var explosion = ref ent.GetComponent<Explosion.Data>();
									if (!explosion.IsNull())
									{
										explosion.power = power * 1.30f;
										explosion.radius = max_radius * 0.50f;
										explosion.damage_entity = power2 * 4.50f;
										explosion.damage_terrain = Maths.Pow(power2, 1.10f) * 0.50f;
										explosion.damage_type = Damage.Type.Electricity;
										explosion.damage_type_secondary = Damage.Type.Shockwave;
										//explosion.ent_owner = entity;
										explosion.fire_amount = 0.00f;
										explosion.smoke_amount = 1.00f;
										explosion.smoke_color = Color32BGRA.White;
										explosion.smoke_lifetime_multiplier = 0.70f;
										explosion.smoke_velocity_multiplier = 2.00f;
										explosion.flash_duration_multiplier = power * 0.12f;
										explosion.flash_intensity_multiplier = power_sqrt * 0.05f;
										explosion.sparks_amount = 0.00f;
										explosion.volume = 1.50f;
										explosion.pitch = 2.00f * args.pitch_mult;
										//explosion.ent_ignored = explosion_tmp.ent_ignored;

										//App.WriteLine($"{power2}; {explosion.damage_entity}; {explosion.damage_terrain}");

										explosion.Sync(ent, true);
									}

									ref var light = ref ent.GetTrait<Explosion.Data, Light.Data>();
									if (!light.IsNull())
									{
										light.color = args.color * 4.00f;
										light.intensity = power;
										light.size_modifier *= power_sqrt;
									}
								});
							}
#endif

#if CLIENT
							Shake.Emit(ref region, world_position, power * 0.50f, power * 0.50f, radius: max_radius * 4.00f);
							Sound.Play("essence.collapse.electricity.01", world_position, volume: power_sqrt * 0.80f, pitch: Maths.Lerp01(1.30f, 0.80f, power * 0.03f) * random.NextFloatRange(0.80f, 1.20f), size: max_radius * 0.20f, dist_multiplier: power_sqrt * 0.50f);

							if (power2 > 250.00f)
							{
								Sound.Play("essence.drain.zap.00", world_position, volume: power * 1.50f, pitch: Maths.Lerp01(1.00f, 0.35f, power * 0.08f) * random.NextFloatRange(0.80f, 1.20f), size: max_radius * 1.00f, dist_multiplier: power * 2.00f);
							}

							//App.WriteLine(power);
							if (power >= 12.00f)
							{
								Sound.Play("essence.blast.00", world_position, volume: power * 1.00f, pitch: Maths.Max(0.30f, Maths.Lerp01(3.00f, 0.70f, power * 0.04f)) * random.NextFloatRange(0.70f, 1.40f), size: max_radius * 1.00f, dist_multiplier: power * 2.00f);
							}

							var smoke_count = (int)max_radius;
							for (var i = 0; i < smoke_count; i++)
							{
								Particle.Spawn(ref region, new Particle.Data()
								{
									texture = Essence.texture_smoke,
									lifetime = random.NextFloatRange(0.20f, power_sqrt * 0.25f),
									pos = world_position + random.NextVector2(0.40f),
									vel = random.NextUnitVector2Range(max_radius * 0.50f, max_radius * power_sqrt),
									force = random.NextUnitVector2Range(20.00f, 40.00f),
									fps = random.NextByteRange(3, 5),
									frame_count = 64,
									frame_count_total = 64,
									frame_offset = random.NextByteRange(0, 64),
									scale = random.NextFloatRange(0.40f, 0.50f),
									rotation = random.NextFloat(10.00f),
									angular_velocity = random.NextFloat(2.00f),
									growth = random.NextFloatRange(10.00f, 20.00f),
									drag = random.NextFloatRange(0.06f, 0.10f),
									color_a = ColorBGRA.ARGB(1.00f, 0.90f, 0.90f, 0.90f).WithAlphaMult(random.NextFloatRange(0.50f, 1.00f) * Maths.Clamp01(power_sqrt * 0.50f)),
									color_b = new Color32BGRA(0, 240, 240, 240),
									stretch = new Vector2(random.NextFloatRange(0.80f, 1.00f), random.NextFloatRange(0.70f, 0.80f)),
									face_dir_ratio = 0.50f,
								});
							}
#endif
						}
						break;

						case Essence.Type.Failure:
						{
							//var max_radius = MathF.Sqrt(power2) * 1.00f;
							//var power = MathF.Sqrt(max_radius);
							//var power_sqrt = MathF.Sqrt(power);

							var max_radius = Maths.Cbrt(amount * 9.00f * Maths.tau2_inv);
							var power = max_radius;
							var power_sqrt = Maths.Sqrt(power);

							//App.WriteLine($"precipitated: {power2}; {max_radius}; {power}; {power_sqrt}");

							Span<OverlapResult> results = stackalloc OverlapResult[32];
							if (region.TryOverlapPointAll(world_position, max_radius, ref results, mask: Physics.Layer.Item | Physics.Layer.Attachable | Physics.Layer.Holdable | Physics.Layer.Vehicle, exclude: Physics.Layer.Essence))
							{
								foreach (ref var result in results)
								{
									var alpha = Maths.Normalize01(result.distance, max_radius);
									var ent_result = result.entity;

									ref var hit_body = ref result.GetBody();
									if (!hit_body.IsNull())
									{
										var dir = (result.world_position - world_position).GetNormalized(out var dist);
										var ratio = 1.00f - (dist / max_radius);
										ratio *= ratio;

										//var force = dir * 4000.00f * ratio;
										var force = dir * power2 * ratio * -3000.00f * App.tickrate_f32;
										force = Physics.LimitForce2B(ref hit_body, force, new Vector2(10));

										hit_body.AddForceWorld(force, result.world_position);
									}

#if SERVER
									var chance = 1.00f - alpha;
									chance *= Maths.Clamp01(power * 0.70f);

									//App.WriteLine($"chance: {chance}; {result.distance}");

									if (random.NextBool(chance))
									{
										var ev = new EssenceNode.FailureEvent()
										{
											world_position = world_position,
											power = power
										};

										ent_result.TriggerEvent(ref ev);
									}
#endif
								}
							}

#if CLIENT
							Shake.Emit(ref region, world_position, power * 1.00f, power * 1.00f, radius: max_radius * 6.00f);
							Sound.Play("essence.collapse.failure.01", world_position, volume: power_sqrt * 0.80f, pitch: Maths.Lerp01(3.00f, 0.10f, power * 0.05f) * random.NextFloatRange(0.90f, 1.00f), size: max_radius * 0.20f, dist_multiplier: power_sqrt * 0.50f);

							if (power2 >= 2250.00f)
							{
								Sound.Play("essence.blast.00", world_position, volume: power * 1.00f, pitch: Maths.Max(0.30f, Maths.Lerp01(10.00f, 1.00f, power * 0.08f)) * random.NextFloatRange(0.90f, 1.00f), size: max_radius * 1.00f, dist_multiplier: power * 2.00f);
							}

							var smoke_count = (int)(max_radius * 4);
							for (var i = 0; i < smoke_count; i++)
							{
								Particle.Spawn(ref region, new Particle.Data()
								{
									texture = Essence.texture_smoke,
									lifetime = random.NextFloatRange(0.80f, power_sqrt * 0.25f),
									pos = world_position + random.NextVector2(0.40f),
									vel = random.NextUnitVector2Range(max_radius * 0.50f, max_radius * power_sqrt),
									force = random.NextUnitVector2Range(20.00f, 200.00f),
									fps = random.NextByteRange(3, 5),
									frame_count = 64,
									frame_count_total = 64,
									frame_offset = random.NextByteRange(0, 64),
									scale = random.NextFloatRange(0.40f, 0.50f),
									rotation = random.NextFloat(10.00f),
									angular_velocity = random.NextFloat(2.00f),
									growth = random.NextFloatRange(2.00f, 10.00f),
									drag = random.NextFloatRange(0.06f, 0.10f),
									color_a = essence_data.color_emit.WithAlphaMult(0.40f),
									color_b = essence_data.color_emit.WithAlphaMult(0.10f),
									stretch = new Vector2(random.NextFloatRange(0.20f, 1.00f), random.NextFloatRange(0.70f, 0.80f)),
									face_dir_ratio = 1.00f,
								});

								Particle.Spawn(ref region, new Particle.Data()
								{
									texture = EssenceNode.texture_glow_failure_01,
									lifetime = random.NextFloatRange(0.30f, power * 0.80f),
									pos = world_position + random.NextVector2(0.40f),
									vel = random.NextUnitVector2Range(max_radius * 0.10f, max_radius * power_sqrt * 0.50f),
									force = random.NextUnitVector2Range(5.00f, 10.00f),
									fps = random.NextByteRange(2, 20),
									frame_count = 4,
									frame_count_total = 4,
									frame_offset = 0,
									scale = random.NextFloatRange(10.00f, 20.00f) * (power * 0.25f),
									rotation = random.NextFloat(10.00f),
									angular_velocity = random.NextFloat(10.00f),
									growth = -random.NextFloatRange(15.00f, 20.00f),
									drag = random.NextFloatRange(0.06f, 0.10f),
									color_a = essence_data.color_emit.WithAlphaMult(2.00f),
									color_b = essence_data.color_emit.WithAlphaMult(0.10f),
									stretch = new Vector2(random.NextFloatRange(0.10f, 4.00f), random.NextFloatRange(0.10f, 4.00f)),
									face_dir_ratio = 0.70f,
									glow = random.NextFloatRange(5.00f, 20.00f)
								});
							}
#endif
						}
						break;

						case Essence.Type.Anger:
						{
							//var max_radius = MathF.Sqrt(power2) * 0.50f;
							//var power = MathF.Sqrt(max_radius);
							//var power_sqrt = MathF.Sqrt(power);

							var power = Maths.Cbrt(amount * 9.00f * Maths.tau2_inv);
							var power_sqrt = Maths.Sqrt(power);
							var max_radius = power * 1.25f;
							var shockwave_radius = Maths.Pow(max_radius, 1.25f) * 0.45f;


							//App.WriteLine($"amount: {amount:0.00}; precipitated: power2: {power2}; max_radius: {max_radius}; power: {power}; power_sqrt: {power_sqrt}; shockwave_radius: {shockwave_radius}");

							Span<OverlapResult> results = FixedArray.CreateSpan64NoInit<OverlapResult>(out var results_buffer);
							if (region.TryOverlapPointAll(world_position: world_position,
							radius: shockwave_radius,
							hits: ref results,
							mask: Physics.Layer.Dynamic,
							exclude: Physics.Layer.Essence | Physics.Layer.Static | Physics.Layer.World))
							{
								foreach (ref var result in results)
								{
									ref var hit_body = ref result.GetBody(); // TODO: limit and apply force directly to OverlapResult
									if (hit_body.IsNotNull())
									{
										var dir = (result.world_position - world_position).GetNormalized(out var dist);
										var ratio = 1.00f - Maths.Normalize01(dist, shockwave_radius * 1.80f);
										//ratio *= ratio;

										//var force = dir * 4000.00f * ratio;
										var force = dir * power2 * ratio * 3200.00f * App.tickrate_f32;
										force = Physics.LimitForce2B(ref hit_body, force, new Vector2(30, 20));
										force *= random.NextFloatRange(0.50f, 1.00f);

										hit_body.AddForceWorld(force, Maths.Avg(hit_body.GetPosition(), result.world_position));
									}
								}

								//App.WriteLine(results.Length);
								//foreach (ref var result in results)
								//{

								//	var dir = result.gradient;
								//	var dist = result.distance;
								//	//var dir = (result.world_position - world_position).GetNormalized(out var dist);
								//	var ratio = 1.00f - Maths.Normalize01(dist, shockwave_radius * 1.80f);
								//	//ratio *= ratio;

								//	//var force = dir * 4000.00f * ratio;
								//	var force = dir * power2 * ratio * -2500.00f * App.tickrate_f32;

								//	result.AddForceWorld(force, result.world_position);

								//	//force = Physics.LimitForce2B(ref hit_body, force, new Vector2(30, 20));
								//	//force *= random.NextFloatRange(0.50f, 1.00f);

								//	//hit_body.AddForceWorld(force, Vector2.Lerp(hit_body.GetPosition(), result.world_position, 0.50f));
								//}
							}

#if SERVER
							if (power2 >= 10.00f)
							{
								var args = (pitch_mult: random.NextFloatRange(0.90f, 1.00f), color: essence_data.color_emit);
								region.SpawnPrefab("explosion", world_position).ContinueWith(ent =>
								{
									ref var explosion = ref ent.GetComponent<Explosion.Data>();
									if (explosion.IsNotNull())
									{
										explosion.power = power * 2.00f;
										explosion.radius = Maths.Clamp(2.00f + (max_radius.Sqrt() * 1.25f), 4.00f, 20.00f); //.PowFast(0.75f);
										explosion.damage_entity = Maths.Pow(power2 * 2.00f, 1.10f) * 2.50f;
										explosion.damage_terrain = Maths.Pow(power2 * 12.00f, 0.93f) * 5.00f;
										explosion.damage_type = Damage.Type.Fracture;
										explosion.damage_type_secondary = Damage.Type.Explosion;
										//explosion.ent_owner = entity;
										explosion.fire_amount = 0.10f;
										explosion.smoke_amount = 0.40f;
										explosion.smoke_lifetime_multiplier = 0.80f;
										explosion.smoke_velocity_multiplier = 5.00f;
										explosion.flash_duration_multiplier = power * 0.10f;
										explosion.flash_intensity_multiplier = power_sqrt * 0.10f;
										explosion.sparks_amount = 0.40f;
										explosion.volume = power * 0.25f;
										explosion.pitch = Maths.Lerp01(4.00f, 0.80f, power * 0.10f) * args.pitch_mult;
										explosion.shake_multiplier = power_sqrt * 1.50f;
										explosion.force_multiplier = 1.40f;
										explosion.stun_multiplier = 0.75f;
										explosion.terrain_radius_multiplier = 1.10f;
										//explosion.ent_ignored = explosion_tmp.ent_ignored;

										//App.WriteLine($"dmg: {explosion.damage_entity}; {explosion.damage_terrain}; {explosion.radius}; {explosion.power}; {power2}");

										explosion.Sync(ent, true);
									}

									ref var light = ref ent.GetTrait<Explosion.Data, Light.Data>();
									if (light.IsNotNull())
									{
										light.color = ColorBGRA.Lerp(args.color, ColorBGRA.White, 0.20f) * 4.00f;
										light.intensity = power;
									}
								});
							}
#endif

#if CLIENT
							Shake.Emit(ref region, world_position, power_sqrt * 0.30f, power_sqrt * 0.30f, radius: max_radius * 3.00f);
							Sound.Play(essence_data.sound_collapse, world_position, volume: power_sqrt * 0.80f, pitch: Maths.Max(Maths.Lerp01(1.30f, 0.10f, power * 0.05f) * random.NextFloatExtra(0.90f, 0.20f), 0.30f), size: max_radius * 0.20f, dist_multiplier: power_sqrt * 0.50f, priority: 0.65f);

							if (power2 >= 250.00f)
							{
								Sound.Play(essence_data.sound_blast, world_position, volume: power_sqrt * 1.40f, pitch: Maths.Max(Maths.Lerp01(1.00f, 0.10f, power * 0.08f) * random.NextFloatExtra(0.80f, 0.30f), 0.30f), size: max_radius * power, dist_multiplier: power_sqrt * 0.35f, priority: 0.75f);
							}

							//if (power2 >= 750.00f)
							//{
							//	Sound.Play("essence.blast.00", world_position, volume: power * 1.00f, pitch: Maths.Max(0.30f, Maths.Lerp01(3.00f, 1.00f, power * 0.07f)) * random.NextFloatRange(0.90f, 1.00f), size: max_radius * power * 3.00f, dist_multiplier: power * 2.00f, priority: 0.90f);
							//}

							var smoke_count = (int)(max_radius * 1.00f * power_sqrt);
							for (var i = 0; i < smoke_count; i++)
							{
								Particle.Spawn(ref region, new Particle.Data()
								{
									texture = Essence.texture_smoke,
									lifetime = random.NextFloatRange(0.15f, power * 0.25f),
									pos = world_position + random.NextVector2(0.40f),
									vel = random.NextUnitVector2Range(max_radius * 0.12f, max_radius * power_sqrt * 0.70f) * 2.00f,
									force = random.NextUnitVector2Range(20.00f, 40.00f),
									fps = random.NextByteRange(15, 20),
									frame_count = 64,
									frame_count_total = 64,
									frame_offset = random.NextByteRange(0, 64),
									scale = random.NextFloatRange(0.40f, 0.70f) * power_sqrt * 0.50f,
									//rotation = random.NextFloat(10.00f),
									angular_velocity = random.NextFloat(5.00f) / power,
									growth = random.NextFloatRange(1.00f, 5.00f) * power_sqrt * 0.50f,
									drag = random.NextFloatRange(0.02f, 0.03f),
									color_a = ColorBGRA.ARGB(0.80f, 0.90f, 0.90f, 0.90f).WithAlphaMult(random.NextFloatRange(0.60f, 1.00f) * Maths.Clamp01(power_sqrt * 0.50f)),
									color_b = new Color32BGRA(0, 240, 240, 240),
									stretch = new Vector2(random.NextFloatRange(0.80f, 1.00f), random.NextFloatRange(1.00f, 1.30f) * power_sqrt * 0.50f),
									face_dir_ratio = 0.80f,
								});
							}
#endif
						}
						break;
					}
				}
			}
		}

		public static void GetPotentialEssence(ref EssenceNode.Data essence_node, out float charge, out float amount, out float radius)
		{

			//App.WriteLine($"charge: {charge}; stability: {essence_node.stability}");

			charge = Maths.Clamp01(1.00f - Maths.Pow3(essence_node.stability));
			amount = Maths.Clamp(essence_node.amount * Maths.Clamp01(charge * (0.10f + Maths.Pow2(essence_node.volatility))), 0.00f, 5000.00f); // because we don't want to crash the server

			//radius = MathF.Sqrt(amount);
			radius = Maths.Cbrt((9.00f * amount) * Maths.tau2_inv); //, 1.00f / 3.00f);
		}

		[Shitcode]
		[ISystem.EarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void Update(ISystem.Info info, Entity entity, ref Region.Data region, ref XorRandom random, [Source.Owned] ref Transform.Data transform,
		[Source.Owned] ref EssenceNode.Data essence_node, [Source.Owned, Pair.Component<EssenceNode.Data>] ref Light.Data light)
		{
			if (essence_node.flags.HasAny(EssenceNode.Flags.Collapsing))
			{
				//var charge = Maths.Clamp01(1.00f - Maths.Pow(essence_node.stability, 3.00f));
				//App.WriteLine($"charge: {charge}; stability: {essence_node.stability}");

				var essence_amount_total = essence_node.amount;
				if (essence_amount_total > 0.00f)
				{
					//var essence_amount_released = Maths.Clamp(essence_amount_total * Maths.Clamp01(charge * (0.10f + Maths.Pow(essence_node.volatility, 2.00f))), 0.00f, 500.00f); // because we don't want to crash the server

					//var radius = MathF.Sqrt(essence_amount_released);

					EssenceNode.GetPotentialEssence(ref essence_node, out var charge, out var essence_amount_released, out var radius);
					//App.WriteLine($"amount: {essence_amount_released}; charge: {charge}; radius: {radius}");

#if CLIENT
					light.intensity = radius;
					light.scale = new Vector2(4, 4) * 0.50f * charge * radius;
					light.color = ColorBGRA.White; // new Vector4(1, 1, 1, 1);
					light.offset = random.NextUnitVector2Range(0.10f, 0.20f);

					//Shake.Emit(ref region, transform.position, 0.50f, 0.50f, radius: 80.00f);
					//Sound.Play("essence.drain.zap.00", transform.position, volume: 1.00f, pitch: random.NextFloatRange(0.90f, 1.00f), size: 0.70f, dist_multiplier: 2.00f);
#endif

#if SERVER
					essence_node.amount -= essence_amount_released;

					var ev = new EssenceNode.CollapseEvent()
					{
						world_position = transform.position,
						h_essence = essence_node.h_essence,
						essence_amount_precipitated = essence_amount_released,
						random = XorRandom.New(true) // random
					};

					var max_radius = 2.00f;
					Span<OverlapResult> results = stackalloc OverlapResult[16];
					if (region.TryOverlapPointAll(transform.position, max_radius, ref results, mask: Physics.Layer.Entity, exclude: Physics.Layer.Essence | Physics.Layer.World))
					{
						foreach (ref var result in results)
						{
							result.entity.TriggerEvent(ref ev);
						}
					}

					//var essence_released = ev.essence_amount;

					//entity.Notify(ref ev);

					//var essence_amount_rem = Maths.Clamp(ev.essence_amount, 0.00f, essence_amount_released);
					//var essence_amount_regained = essence_amount_rem * (1.00f - Maths.Pow(essence_node.volatility, 3.00f));
					//var essence_amount_collapsed = essence_amount_rem - essence_amount_regained;

					//var essence_amount_lost = Maths.Clamp(essence_amount_released - ev.essence_amount, 0.00f, essence_amount_released);
					//var essence_amount_regained = Maths.Clamp(essence_amount_released - ev.essence_amount, 0.00f, essence_amount_released);
					//App.WriteLine($"total: {essence_amount_total}; released: {essence_amount_released}; lost: {essence_amount_lost}; charge: {charge}");

					//essence_node.volatility = Maths.Clamp01(essence_node.volatility + (Maths.Clamp01(1.00f - (essence_node.stability - (essence_node.volatility * 0.50f))) * 1.00f * Maths.Clamp01(0.20f + essence_node.volatility) * random.NextFloatRange(0.50f, 1.00f)));

					var essence_amount_precipitated = essence_amount_released - ev.essence_amount_precipitated;
					if (essence_node.flags.HasAny(EssenceNode.Flags.Full_Collapse))
					{
						essence_amount_precipitated += ev.essence_amount_precipitated;
					}
					else
					{
						essence_amount_precipitated += ev.essence_amount_precipitated * random.NextFloatExtra(0.20f, Maths.Pow3(essence_node.volatility) * 0.80f).Clamp01(); // * 0.20f;
					}
					essence_amount_precipitated = Maths.Clamp(essence_amount_precipitated, 0.00f, essence_amount_released);

					var essence_amount_returned = essence_amount_released - essence_amount_precipitated;

					var ratio = Maths.Normalize01(essence_amount_precipitated, essence_amount_released);
					//App.WriteLine($"ratio: {ratio}");

					if (essence_node.flags.HasAny(EssenceNode.Flags.Full_Collapse))
					{
						essence_node.volatility = 1.00f;
						essence_node.stability = 0.00f;
					}
					else
					{
						essence_node.volatility = Maths.Clamp01(essence_node.volatility + Maths.Clamp((1.00f - essence_node.stability) * random.NextFloatRange(1.00f, 4.00f) * (1.00f + (essence_node.volatility)), 0.05f, essence_node.volatility * random.NextFloatRange(1.00f, 4.00f)));
						essence_node.stability = Maths.Clamp(essence_node.stability + 0.20f, 0.00f, 1.00f - (random.NextFloatRange(essence_node.volatility * essence_node.volatility * 0.05f, essence_node.volatility * essence_node.volatility * 0.20f)));
					}

					var ev_post = new EssenceNode.CollapseEvent()
					{
						world_position = transform.position,
						h_essence = essence_node.h_essence,
						essence_amount_precipitated = essence_amount_precipitated, //essence_amount_released - essence_amount_lost,
						essence_amount_returned = essence_amount_returned,
						random = XorRandom.New(true)
					};

					entity.TriggerEventDeferred(ev_post, sync: true);



					//var essence_amount_lost = essence_amount_released - ev.essence_amount;
					//App.WriteLine($"total: {essence_amount_total}; released: {essence_amount_released}; lost: {essence_amount_lost}; {ev.essence_amount}");


					//essence_node.amount = Maths.Max(essence_node.amount - essence_amount_lost, 0.00f);

					//if (essence_node.amount <= 0.00f)
					//{
					//	entity.Delete();
					//}
#endif
					if (essence_node.flags.HasAny(EssenceNode.Flags.Full_Collapse))
					{
						essence_node.t_next_collapse = info.WorldTime + 3.00f;
					}
					else
					{
						essence_node.t_next_collapse = info.WorldTime + 0.50f + (5.00f * (1.00f - essence_node.volatility) * random.NextFloatRange(0.75f, 1.00f));
					}
					essence_node.flags.RemoveFlag(EssenceNode.Flags.Collapsing);
				}
				else
				{
#if SERVER
					entity.Delete();
#endif
				}
			}
			else
			{
#if SERVER
				if (info.WorldTime >= essence_node.t_next_collapse)
				{
					if (essence_node.stability <= 0.00f || (essence_node.stability <= 0.900f && random.NextBool((1.00f - essence_node.stability) * 0.006f)))
					{
						//var amount = Maths.Min(1.00f * App.fixed_update_interval_s, essence_node.amount);

						//essence_node.amount -= amount;

						//essence_node.volatility = Maths.Clamp01(essence_node.volatility + ((1.00f - essence_node.stability) * 0.50f * essence_node.volatility * random.NextFloatRange(0.50f, 1.00f)));
						essence_node.flags.AddFlag(EssenceNode.Flags.Collapsing);
						essence_node.Sync(entity, true);
					}

					//if (essence_node.amount <= 10.00f)
					//{
					//	entity.Delete();
					//}
				}
				else if (essence_node.amount <= 10.00f)
				{
					entity.Delete();
				}
#endif

				essence_node.stability = Maths.Clamp(Maths.Lerp(essence_node.stability, Maths.Pow2(1.00f - (essence_node.volatility * essence_node.volatility * 0.50f * random.NextFloatRange(0.10f, 0.50f))), (1.00f - essence_node.volatility) * App.fixed_update_interval_s * 0.50f * essence_node.stability), 0.00f, 1.00f); // Maths.Max(essence_node.stability, 1.00f - ((essence_node.volatility) * random.NextFloatRange(0.10f, 0.90f))));
				essence_node.volatility = Maths.Clamp01(Maths.MoveTowards(essence_node.volatility, 0.03f, 0.000004f + (Maths.Clamp01(essence_node.volatility * essence_node.volatility) * 0.0005f)));
			}
		}

#if CLIENT
		[Shitcode]
		[ISystem.VeryEarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void UpdateEffects(ref Region.Data region, ref XorRandom random, ISystem.Info info, Entity entity, [Source.Owned] ref Transform.Data transform,
		[Source.Owned] ref EssenceNode.Data essence_node, [Source.Owned, Pair.Component<EssenceNode.Data>] ref Light.Data light,
		[Source.Owned, Pair.Component<EssenceNode.Data>] ref Sound.Emitter sound_emitter, [Source.Owned] ref Body.Data body)
		{
			ref var essence_data = ref essence_node.h_essence.GetData();
			if (essence_data.IsNotNull())
			{
				EssenceNode.GetPotentialEssence(ref essence_node, out _, out var essence_amount_released, out var radius);
				//App.WriteLine(essence_amount_released);

				//var intensity = (essence_node.amount * 0.0005f) * charge;
				var intensity = (essence_amount_released * 0.0005f); // * (1.00f - (0.20f + (essence_node.stability * 0.80f)));
																	 //var intensity2 = (essence_node.amount * 0.0005f) * charge;

				if (intensity > 0.01f && essence_node.stability < 1.00f) // && info.WorldTime >= essence_node.next_drain)
				{
					var radius_sqrt = Maths.SqrtToOne(radius);
					var dir = -body.GetVelocity().GetNormalized(out var len);
					//light.offset = dir * 4;

					//var charge = 1.00f - Maths.Pow(Maths.Lerp(essence_node.stability, 1.00f - Maths.Clamp01((essence_node.volatility * essence_node.volatility * 0.80f) - 0.20f), 0.20f), 2.00f);
					var freq = 4.00f;

					//App.WriteLine($"amount: {essence_amount_released}; intensity: {intensity}; radius: {radius}; radius_sqrt: {radius_sqrt}");

					var noise_x = Maths.Perlin(info.WorldTime * freq, 0.00f, 8.00f);
					var noise_y = Maths.Perlin(0.00f, info.WorldTime * 0.50f, 1.00f);

					//var noise_rot = Maths.Perlin(0.00f, info.WorldTime * 10, 1.00f);
					var noise_intensity = Maths.Perlin(0.00f, info.WorldTime * 1, 1.00f);

					//body.SetRotation(0.00f);
					//light.rotation = random.NextFloatRange(-3, 3); //-dir.GetAngleRadians() - (MathF.PI * 0.50f);
					light.rotation = random.NextFloatRange(-0.50f, 0.50f) - dir.GetAngleRadiansFast() - (MathF.PI * 0.50f);

					var color = essence_data.color_emit;
					//color.bgra.ClampMaxRef(intensity * 0.20f);
					color.bgra *= (intensity * 0.20f);

					//light.color = new Vector4(surge.charge * 0.20f, 1, 1, 1);

					light.color = color;
					light.intensity = ((0.50f + MathF.Abs(noise_intensity) + (radius_sqrt * 0.1f)) * 4.00f) * (intensity * 3.00f).Clamp01();
					light.scale = new Vector2(4 + (2 * noise_x), 4 + (1 * noise_y) * len * 0.25f) * 0.50f * intensity * (1.00f + (radius_sqrt * 0.4f));
					light.offset = random.NextUnitVector2Range(0.25f, 0.40f);

					sound_emitter.volume_mult = Maths.Clamp((intensity * intensity * 8.00f), 0.00f, 4.00f);
					sound_emitter.pitch_mult = 0.30f + (intensity * 0.750f);
					sound_emitter.size_max_modifier = 0.70f + (intensity * 1.50f);

					if (intensity > 0.25f)
					{
						//App.WriteLine(intensity);


						//App.WriteLine(intensity);
						//var radius = (essence_node.amount * 0.005f) * intensity;
						//radius = Maths.Min(radius * radius, 200);

						//App.WriteLine(radius);
						Shake.Emit(ref region, transform.position, intensity * 0.07f, Maths.Clamp(intensity * 0.40f, 0.05f, 0.70f), Maths.Min(radius * radius, 200));
					}

					if (random.NextBool(intensity * 4.00f))
					{
						Particle.Spawn(ref region, new Particle.Data()
						{
							texture = ArcaneAttractor.texture_spark,
							lifetime = random.NextFloatRange(0.01f, 0.10f) + Maths.Min(intensity * 0.40f * random.NextFloatRange(0.50f, 1.00f), 1.00f),
							pos = transform.position + random.NextVector2(0.10f),
							vel = dir.RotateByRad(random.NextFloatRange(-0.20f, 0.20f) * (1.00f + (intensity * 20.00f))) * random.NextFloatRange(1.00f, 4.00f),
							force = dir.RotateByRad(random.NextFloatRange(-0.20f, 0.20f)) * random.NextFloatRange(0, 200),
							//fps = random.NextByteRange(3, 5),
							//stretch = new Vector2(0.1f,0.4f),
							frame_count = 1,
							frame_count_total = 1,
							frame_offset = 0,
							scale = random.NextFloatRange(0.40f, 0.50f) + (radius_sqrt * 0.02f * random.NextFloatRange(0.75f, 1.00f)),
							rotation = random.NextFloatRange(-0.10f, 0.10f),
							angular_velocity = random.NextFloat(0.50f + Maths.Min(intensity * 4.00f, 3.00f)),
							growth = random.NextFloatRange(4.00f, 5.00f) * (1.00f + (radius_sqrt * 0.50f)),
							drag = random.NextFloatRange(0.01f, 0.25f),
							color_a = ColorBGRA.Lerp(color, color + ColorBGRA.White, random.NextFloatRange(0.00f, 0.20f)) * random.NextFloatRange(0.50f, 2.50f),
							color_b = ColorBGRA.Lerp(color.WithAlpha(0.00f), ColorBGRA.Transparent, random.NextFloatRange(0.00f, 0.40f)) * random.NextFloatRange(0.50f, 2.50f),
							glow = random.NextFloatRange(1.00f, 4.00f) * (1.00f + (intensity * 4.00f)),
							stretch = new Vector2(random.NextFloatRange(1.50f, 3.00f), random.NextFloatRange(0.50f, 4.00f)),
							face_dir_ratio = random.NextFloatRange(0.80f, 0.95f),
						});
					}
				}
				else if (!essence_node.flags.HasAll(EssenceNode.Flags.Collapsing))
				{
					light.rotation += random.NextFloatRange(-0.10f, 0.10f); //-dir.GetAngleRadians() - (MathF.PI * 0.50f);

					var color = essence_data.color_emit * 0.50f;
					//color.a = 1.00f;

					light.intensity = Maths.Lerp(light.intensity, 0.00f, 0.10f);
					light.scale.X = Maths.Lerp(light.scale.X, 0.00f, 0.30f);
					light.scale.Y = Maths.Lerp(light.scale.Y, 0.00f, 0.10f);
					light.offset = random.NextUnitVector2Range(0.25f, 1.00f);
					light.color = Color32BGRA.Lerp(light.color, color, 0.10f);
					//light.scale = Vector2.Lerp(light.scale, Vector2.Zero, 0.20f);

					sound_emitter.volume_mult = 0.00f;
					sound_emitter.pitch_mult = 0.00f;
				}
			}
		}
#endif
	}
}