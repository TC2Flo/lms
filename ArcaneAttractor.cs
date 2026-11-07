namespace TC2.Base.Components
{
	public static partial class ArcaneAttractor
	{
		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Region), IComponent.With<ArcaneAttractor.State>]
		public partial struct Data(): IComponent
		{
			[Editor.Picker.Position(relative: true)]
			public Vector2 offset;

			public Sound.Handle sound_on = ArcaneAttractor.sound_on_default;
			public Sound.Handle sound_off = ArcaneAttractor.sound_off_default;

			public float radius;
			public float power;
		}

		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public partial struct State: IComponent
		{
			[Flags]
			public enum Flags: uint
			{
				None = 0,

				Powered = 1 << 0
			}

			public ArcaneAttractor.State.Flags flags;

			[Save.Ignore, Net.Ignore] public float next_update;
			[Save.Ignore, Net.Ignore] public float next_steam;
		}

		public static readonly Sound.Handle sound_on_default = "arcane_infuser.on.00";
		public static readonly Sound.Handle sound_off_default = "arcane_infuser.off.00";
		public static readonly Sound.Handle sound_fizzle_default = "arcane_infuser.fizzle.00";

		[ISystem.VeryLateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void UpdateEffects(ISystem.Info info, Entity entity, ref XorRandom random, [Source.Owned, Pair.Component<Joint.Base>] ref Light.Data light, [Source.Owned, Override] in Joint.Gear gear, [Source.Shared] ref Essence.Container.Data essence_container)
		{
			var max = 0.50f;
			var modifier = Maths.Clamp(Maths.Abs(gear.GetCurrentSpeed()) - 0.10f, 0.00f, max) / max;

			var noise_x = random.NextFloatRange(-2, 2); // Maths.Perlin(info.WorldTime * freq, 0.00f, 8.00f);
			var noise_y = random.NextFloatRange(-2, 2); //Maths.Perlin(0.00f, info.WorldTime * 1.50f, 1.00f);

			//var noise_rot = Maths.Perlin(0.00f, info.WorldTime * 10, 1.00f);
			var noise_intensity = Maths.Perlin(0.00f, info.WorldTime * 1, 1.00f);

			//body.SetRotation(0.00f);
			//light.rotation = random.NextFloatRange(-3, 3); //-dir.GetAngleRadians() - (MathF.PI * 0.50f);
			light.rotation = random.NextFloatRange(-0.80f, 0.50f);

			ref var essence_data = ref essence_container.h_essence.GetData();
			if (essence_data.IsNotNull())
			{
				var color = essence_data.color_emit;

				light.color = color;
			}

			light.intensity = ((0.50f + Maths.Abs(noise_intensity)) * 4.00f) * modifier;
			light.scale = Vector2.Lerp(light.scale, new Vector2(2 + (4 * noise_x), 2 + (4 * noise_y)) * 0.50f * modifier, 0.50f);
		}

		public static readonly Texture.Handle texture_smoke = "BiggerSmoke_Light";
		public static readonly Texture.Handle texture_spark = "essence.spark";
		public static readonly Texture.Handle texture_glow = "essence.glow";
		public static readonly Texture.Handle texture_glow_transparent = "essence.glow.transparent";
		public static readonly Texture.Handle tex_smoke = "BiggerSmoke_Light";
		public static readonly Texture.Handle tex_spark = "metal_spark.01";
		public static readonly Texture.Handle tex_light = "light.circle.00";

		[Shitcode]
		[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void Update(ISystem.Info info, Entity entity, ref Region.Data region, ref XorRandom random,
		[Source.Owned] in Transform.Data transform, [Source.Owned] ref ArcaneAttractor.Data attractor, [Source.Owned] ref ArcaneAttractor.State attractor_state, [Source.Owned] in Control.Data control)
		{
			var ts = Timestamp.Now();

			if (attractor_state.flags.HasAny(ArcaneAttractor.State.Flags.Powered))
			{
				var wpos = transform.LocalToWorld(attractor.offset);
				var arg = (position: wpos, radius: attractor.radius, power: attractor.power);

				foreach (ref var row in region.IterateQuery<Essence.GetAllNodesQuery>())
				{
					row.Run((ISystem.Info info, Entity entity, ref EssenceNode.Data essence_node, in Transform.Data transform, ref Body.Data body) =>
					{
						if (info.WorldTime >= essence_node.t_next_collapse && !essence_node.flags.HasAll(EssenceNode.Flags.Collapsing))
						{
							ref var region = ref info.GetRegion();
							ref var random = ref info.GetRandom();

							//var dir = ((arg.position - (body.GetVelocity() * 0.10f)) - transform.position).GetNormalized(out var distance);
							var dir = ((arg.position - (body.GetVelocity() * 0.10f)) - transform.position).GetNormalized(out var distance);
							//var dir = ((arg.position) - transform.position).GetNormalized(out var distance);
							if (distance <= arg.radius)
							{
								var dir2 = (arg.position - transform.position).GetNormalized();

								var distance_clamped = Maths.Clamp(distance, 1.00f, arg.radius);

								var modifier = 1.00f / Maths.Sqrt(distance_clamped);


								//var modifier2 = (1.00f - (distance_clamped / arg.radius));
								//var modifier3 = (1.00f - (Maths.Clamp(distance, 1.00f, arg.radius * 0.10f) / (arg.radius * 0.10f)));
								//var modifier3_inv = 1.00f - modifier3;

								var force = dir * (arg.power * modifier) * 10.00f; // * body.GetMass();
								//if (distance < 3.00f) force *= 0.00f;

								force = Physics.LimitForce2B(ref body, force, new Vector2(10));

								if (distance < 4.00f)
								{
									if (info.WorldTime >= essence_node.t_next_collapse && !essence_node.flags.HasAll(EssenceNode.Flags.Collapsing))
									{
										essence_node.t_last_collapse = info.WorldTime;
										essence_node.stability -= Maths.Normalize(arg.power, essence_node.amount) * App.fixed_update_interval_s * Maths.Max(0.10f, essence_node.volatility); // Maths.Lerp(essence_node.stability, 0.00f, (arg.power / essence_node.amount) * App.fixed_update_interval_s * 0.08f);

										//essence_node.stability = Maths.MoveTowards(essence_node.stability, 0.00f, arg.power * App.fixed_update_interval_s);
									}
								}

#if CLIENT
								ref var essence_data = ref essence_node.h_essence.GetData();
								if (essence_data.IsNotNull())
								{
									var color = essence_data.color_emit;

									var vel = distance * 1.20f;
									var dist = distance * random.NextFloatRange(0.10f, 1.00f);

									EssenceNode.GetPotentialEssence(ref essence_node, out var charge, out var amount, out var radius);

									if (dist > 2.00f)
									{
										var modifier4 = Maths.Clamp01(dist / (arg.radius * 0.10f));
										var modifier4_inv = 1.00f - modifier4;

										Particle.Spawn(ref region, new Particle.Data()
										{
											texture = texture_glow,
											lifetime = random.NextFloatRange(0.50f, 1.00f) * Maths.Min((dist / vel) * 0.50f, 1.00f),
											pos = arg.position - (dir2 * dist) + random.NextUnitVector2Range(0, 0.50f),
											vel = dir2 * vel,
											//force = random.NextUnitVector2Range(0, 40),
											//fps = random.NextByteRange(3, 5),
											//stretch = new Vector2(0.1f,0.4f),
											frame_count = 1,
											frame_count_total = 1,
											frame_offset = 0,
											scale = random.NextFloatRange(0.25f, 0.70f) * 10,
											rotation = dir2.GetAngleRadiansFast(),
											//angular_velocity = random.NextFloatRange(-0.50f, 0.50f),
											growth = -random.NextFloatRange(10.00f, 20.00f),
											//drag = random.NextFloatRange(0.0f, 0.05f),
											color_a = ColorBGRA.Lerp(color, 0xffffffff, random.NextFloatRange(0.00f, 0.50f)).WithAlphaMult(random.NextFloatRange(0.20f, 1.00f) * modifier4_inv),
											color_b = color.WithAlphaMult(0.00f),
											glow = random.NextFloatRange(0.10f, 0.50f) * 20.00f,
											//lit = 1.00f,
											stretch = new Vector2(random.NextFloatRange(0.50f, 2.00f), random.NextFloatRange(0.10f, 0.30f)),
											face_dir_ratio = 0.00f,
										});
									}
									else
									{
										var modifier4 = Maths.Clamp01(dist / (arg.radius * 0.10f));
										var modifier4_inv = 1.00f - modifier4;

										Particle.Spawn(ref region, new Particle.Data()
										{
											texture = texture_glow,
											lifetime = random.NextFloatRange(0.50f, 1.00f) * Maths.Min((dist / vel) * 0.50f, 1.00f),
											pos = arg.position - (dir2 * dist) + random.NextUnitVector2Range(0, 0.50f),
											vel = dir2 * vel,
											//force = random.NextUnitVector2Range(0, 40),
											//fps = random.NextByteRange(3, 5),
											//stretch = new Vector2(0.1f,0.4f),
											frame_count = 1,
											frame_count_total = 1,
											frame_offset = 0,
											scale = random.NextFloatRange(0.25f, 0.70f) * 20,
											rotation = dir2.GetAngleRadiansFast(),
											//angular_velocity = random.NextFloatRange(-0.50f, 0.50f),
											growth = -random.NextFloatRange(10.00f, 20.00f),
											//drag = random.NextFloatRange(0.0f, 0.05f),
											color_a = ColorBGRA.Lerp(color, 0xffffffff, random.NextFloatRange(0.00f, 0.50f)).WithAlphaMult(random.NextFloatRange(0.20f, 1.00f) * modifier4_inv),
											color_b = color.WithAlphaMult(0.00f),
											glow = random.NextFloatRange(0.10f, 0.50f) * MathF.Sqrt(essence_node.amount * 0.50f) * 0.10f,
											//lit = 1.00f,
											stretch = new Vector2(random.NextFloatRange(0.50f, 0.50f), random.NextFloatRange(0.10f, 0.30f)),
											face_dir_ratio = 0.00f,
										});

										//Particle.Spawn(ref region, new Particle.Data()
										//{
										//	texture = texture_glow_transparent,
										//	lifetime = random.NextFloatRange(0.10f, 0.20f),
										//	pos = arg.position - (dir2 * dist) + random.NextUnitVector2Range(0, 0.50f),
										//	vel = -dir2 * 0.25f,
										//	//force = random.NextUnitVector2Range(0, 40),
										//	//fps = random.NextByteRange(3, 5),
										//	//stretch = new Vector2(0.1f,0.4f),
										//	frame_count = 1,
										//	frame_count_total = 1,
										//	frame_offset = 0,
										//	scale = random.NextFloatRange(0.05f, 0.20f),
										//	rotation = dir2.GetAngleRadians(),
										//	angular_velocity = random.NextFloatRange(-0.50f, 0.50f),
										//	growth = random.NextFloatRange(0.50f, 3.80f),
										//	//drag = random.NextFloatRange(0.0f, 0.05f),
										//	color_a = ColorBGRA.Lerp(color, 0xffffffff, random.NextFloatRange(0.00f, 0.30f)).WithAlphaMult(random.NextFloatRange(0.40f, 0.80f)),
										//	color_b = color.WithAlphaMult(0.00f),
										//	glow = random.NextFloatRange(1.00f, 4.00f),
										//	//lit = 1.00f,
										//	stretch = new Vector2(random.NextFloatRange(0.20f, 0.70f), random.NextFloatRange(0.20f, 0.70f)),
										//	face_dir_ratio = 0.00f,
										//});
									}
								}
#endif

								if (distance > 0.50f)
								{
									body.AddForce(force);
								}
							}
						}
					});
				}

				//				region.Query<Essence.GetAllNodesQuery>(Func).Execute(ref arg);
				//				static void Func(ISystem.Info info, Entity entity, ref EssenceNode.Data essence_node, in Transform.Data transform, ref Body.Data body)
				//				{
				//					if (info.WorldTime >= essence_node.next_drain && !essence_node.flags.HasAll(EssenceNode.Flags.Collapsing))
				//					{
				//						ref var arg = ref info.GetParameter<(Vector2 position, float radius, float power)>();
				//						if (!arg.IsNull())
				//						{
				//							ref var region = ref info.GetRegion();
				//							var random = XorRandom.New();

				//							//var dir = ((arg.position - (body.GetVelocity() * 0.10f)) - transform.position).GetNormalized(out var distance);
				//							var dir = ((arg.position - (body.GetVelocity() * 0.50f)) - transform.position).GetNormalized(out var distance);
				//							//var dir = ((arg.position) - transform.position).GetNormalized(out var distance);
				//							if (distance <= arg.radius)
				//							{
				//								var dir2 = (arg.position - transform.position).GetNormalized();

				//								var distance_clamped = Maths.Clamp(distance, 1.00f, arg.radius);
				//								var modifier = 1.00f / distance_clamped;
				//								var modifier2 = (1.00f - (distance_clamped / arg.radius));
				//								var modifier3 = (1.00f - (Maths.Clamp(distance, 1.00f, arg.radius * 0.10f) / (arg.radius * 0.10f)));
				//								var modifier3_inv = 1.00f - modifier3;

				//								var force = dir * (arg.power * modifier);

				//								force = Physics.LimitForce(ref body, force, new Vector2(10, 10));

				//								if (distance < 2.00f)
				//								{
				//									if (info.WorldTime >= essence_node.next_drain && !essence_node.flags.HasAll(EssenceNode.Flags.Collapsing))
				//									{
				//										essence_node.last_drain = info.WorldTime;
				//										essence_node.stability = Maths.Lerp(essence_node.stability, 0.00f, (arg.power / essence_node.amount) * App.fixed_update_interval_s * 0.05f);

				//										//essence_node.stability = Maths.MoveTowards(essence_node.stability, 0.00f, arg.power * App.fixed_update_interval_s);
				//									}
				//								}

				//#if CLIENT
				//								var color = Essence.GetColor(essence_node.type);

				//								var vel = distance * 1.20f;
				//								var dist = distance * random.NextFloatRange(0.10f, 1.00f);

				//								if (dist > 1.00f)
				//								{
				//									var modifier4 = Maths.Clamp01(dist / (arg.radius * 0.10f));
				//									var modifier4_inv = 1.00f - modifier4;

				//									Particle.Spawn(ref region, new Particle.Data()
				//									{
				//										texture = texture_glow_transparent,
				//										lifetime = random.NextFloatRange(0.10f, 1.00f) * Maths.Min((dist / vel) * 0.50f, 1.00f),
				//										pos = arg.position - (dir2 * dist) + random.NextUnitVector2Range(0, 0.50f),
				//										vel = dir2 * vel,
				//										//force = random.NextUnitVector2Range(0, 40),
				//										//fps = random.NextByteRange(3, 5),
				//										//stretch = new Vector2(0.1f,0.4f),
				//										frame_count = 1,
				//										frame_count_total = 1,
				//										frame_offset = 0,
				//										scale = random.NextFloatRange(0.05f, 0.20f),
				//										rotation = dir2.GetAngleRadians(),
				//										//angular_velocity = random.NextFloatRange(-0.50f, 0.50f),
				//										growth = random.NextFloatRange(0.50f, 3.80f),
				//										//drag = random.NextFloatRange(0.0f, 0.05f),
				//										color_a = ColorBGRA.Lerp(color, 0xffffffff, random.NextFloatRange(0.00f, 1.00f)).WithAlphaMult(random.NextFloatRange(0.40f, 2.00f) * modifier4_inv),
				//										color_b = color.WithAlphaMult(0.00f),
				//										//glow = random.NextFloatRange(1.00f, 4.00f),
				//										lit = 1.00f,
				//										stretch = new Vector2(random.NextFloatRange(0.50f, 0.70f), random.NextFloatRange(0.10f, 0.20f)),
				//										face_dir_ratio = 0.00f,
				//									});
				//								}
				//								else
				//								{
				//									Particle.Spawn(ref region, new Particle.Data()
				//									{
				//										texture = texture_glow_transparent,
				//										lifetime = random.NextFloatRange(0.10f, 0.20f),
				//										pos = arg.position - (dir2 * dist) + random.NextUnitVector2Range(0, 0.50f),
				//										vel = -dir2 * 0.25f,
				//										//force = random.NextUnitVector2Range(0, 40),
				//										//fps = random.NextByteRange(3, 5),
				//										//stretch = new Vector2(0.1f,0.4f),
				//										frame_count = 1,
				//										frame_count_total = 1,
				//										frame_offset = 0,
				//										scale = random.NextFloatRange(0.05f, 0.20f),
				//										rotation = dir2.GetAngleRadians(),
				//										angular_velocity = random.NextFloatRange(-0.50f, 0.50f),
				//										growth = random.NextFloatRange(0.50f, 3.80f),
				//										//drag = random.NextFloatRange(0.0f, 0.05f),
				//										color_a = ColorBGRA.Lerp(color, 0xffffffff, random.NextFloatRange(0.00f, 1.00f)).WithAlphaMult(random.NextFloatRange(0.40f, 0.80f)),
				//										color_b = color.WithAlphaMult(0.00f),
				//										//glow = random.NextFloatRange(1.00f, 4.00f),
				//										lit = 1.00f,
				//										stretch = new Vector2(random.NextFloatRange(0.20f, 0.70f), random.NextFloatRange(0.20f, 0.70f)),
				//										face_dir_ratio = 0.00f,
				//									});
				//								}
				//#endif

				//								if (distance > 0.50f)
				//								{
				//									body.AddForce(force);
				//								}
				//							}
				//						}
				//					}

				//				}

#if CLIENT
				if (Camera.IsVisible(Camera.CullType.Rect2x, transform.position))
				{
					if (info.WorldTime >= attractor_state.next_steam)
					{
						Particle.Spawn(ref region, new Particle.Data()
						{
							texture = tex_smoke,
							lifetime = random.NextFloatRange(3.00f, 5.00f),
							pos = transform.position + random.NextVector2(0.50f),
							vel = random.NextUnitVector2Range(0.10f, 1.00f) + random.NextVector2(2.00f),
							fps = random.NextByteRange(2, 5),
							frame_count = 64,
							frame_count_total = 64,
							frame_offset = random.NextByteRange(0, 64),
							//scale = random.NextFloatRange(1.50f, 2.50f),
							scale = random.NextFloatRange(0.20f, 0.40f),
							rotation = random.NextFloat(2.00f),
							angular_velocity = random.NextFloat(1.00f),
							growth = random.NextFloatRange(0.25f, 0.40f),
							drag = random.NextFloatRange(0.02f, 0.04f),
							force = new Vector2(2.00f, random.NextFloatRange(0, -2)),
							color_a = random.NextColor32Range(0x20ffffff, 0x80ffffff),
							color_b = random.NextColor32Range(0x00ffffff, 0x00808080)
						});

						attractor_state.next_steam = info.WorldTime + 0.30f;
					}

					var dir = transform.GetDirection();
					for (var i = 0; i < 1; i++)
					{
						Particle.Spawn(ref region, new Particle.Data()
						{
							texture = tex_spark,
							lifetime = random.NextFloatRange(0.10f, 0.20f),
							pos = transform.LocalToWorld(new Vector2(random.NextFloatRange(-1.00f, 1.50f), 0.00f)) + random.NextVector2(0.20f),
							vel = (dir * random.NextFloatRange(0, 40)).RotateByRad(random.NextFloatRange(-0.85f, 0.85f)) + random.NextVector2(15),
							fps = 0,
							frame_count = 1,
							frame_offset = random.NextByteRange(0, 4),
							frame_count_total = 4,
							scale = random.NextFloatRange(0.10f, 0.30f),
							growth = -random.NextFloatRange(0.60f, 1.00f),
							force = new Vector2(0.00f, random.NextFloatRange(20, 80)),
							drag = random.NextFloatRange(0.05f, 0.20f),
							stretch = new Vector2(random.NextFloatRange(1.00f, 2.00f), 0.75f),
							color_a = random.NextColor32Range(0xffffffff, 0xffc89eff),
							color_b = random.NextColor32Range(0xffffc0b0, 0xffffa090),
							lit = random.NextFloatRange(0.50f, 1.00f),
							face_dir_ratio = 1.00f,
						});
					}

					for (var i = 0; i < 1; i++)
					{
						Particle.Spawn(ref region, new Particle.Data()
						{
							texture = tex_light,
							lifetime = random.NextFloatRange(0.01f, 0.05f),
							pos = transform.LocalToWorld(new Vector2(random.NextFloatRange(-1.00f, 1.00f), 0.00f)) + random.NextVector2(0.20f),
							vel = dir.RotateByRad(random.NextFloatRange(-2.00f, 2.00f)) * random.NextFloatRange(10.00f, 40.00f),
							force = dir.RotateByRad(random.NextFloatRange(-0.20f, 0.20f)) * random.NextFloatRange(0, 200),
							//fps = random.NextByteRange(3, 5),
							//stretch = new Vector2(0.5f, 0.2f),
							scale = random.NextFloatRange(2.00f, 20.00f),
							rotation = random.NextFloatRange(-3.50f, 3.50f),
							angular_velocity = random.NextFloat(0.50f),
							growth = random.NextFloatRange(10.00f, 200.00f),
							drag = random.NextFloatRange(0.07f, 0.25f),
							color_a = random.NextColor32Range(0xffffa090, 0xffc89eff),
							color_b = random.NextColor32Range(0xffc89eff, 0xffffffff),
							glow = random.NextFloatRange(0.50f, 1.00f),
						});
					}
				}

				//for (var i = 0; i < 2; i++)
				//{
				//	var particle = Particle.New("blank", transform.position, random.NextFloatRange(0.05f, 0.10f));
				//	particle.pos = transform.LocalToWorld(new Vector2(random.NextFloatRange(-1.00f, 1.75f), 0)) + random.NextUnitVector2Range(0.20f, 0.50f);
				//	particle.vel = random.NextUnitVector2Range(0, 20);
				//	particle.fps = 0;
				//	particle.scale = random.NextFloatRange(0.40f, 1.00f);
				//	//particle.rotation = random.NextFloat(MathF.PI);
				//	//particle.angular_velocity = random.NextFloat(MathF.PI * 10.00f);
				//	particle.growth = -random.NextFloatRange(1.60f, 2.00f);
				//	particle.force = random.NextUnitVector2Range(20, 40);
				//	//particle.drag = random.NextFloatRange(0.01f, 0.10f);
				//	particle.stretch = new Vector2(random.NextFloatRange(0.20f, 0.40f), 0.05f);
				//	particle.color_a = random.NextColor32Range(0xffffffff, 0xffc89eff);
				//	particle.color_b = random.NextColor32Range(0xffc89eff, 0xffffffff);
				//	particle.lit = random.NextFloatRange(0.50f, 1.00f);
				//	particle.face_dir_ratio = 1.00f;
				//	Particle.Spawn(ref region, particle);
				//}
#endif
			}

			//Span<OverlapResult> results = stackalloc OverlapResult[8];
			//if (region.TryOverlapPointAll(wpos, attractor.radius, ref results, mask: Physics.Layer.Essence))
			//{
			//	//var ev = new EssenceNode.ChargeEvent()
			//	//{
			//	//	ent_drainer = entity,
			//	//	power = attractor.power * 0.001f * 0.25f,
			//	//	world_position = wpos
			//	//};

			//	foreach (ref var result in results)
			//	{
			//		result.entity.Notify(ref ev);
			//	}
			//}

#if SERVER
			var sync = false;

			if (control.mouse.GetKeyDown(Mouse.Key.Right))
			{
				var is_powered = attractor_state.flags.HasAll(ArcaneAttractor.State.Flags.Powered);
				attractor_state.flags.SetFlag(ArcaneAttractor.State.Flags.Powered, !is_powered);

				if (is_powered)
				{
					Sound.Play(ref region, attractor.sound_off, transform.position);
				}
				else
				{
					Sound.Play(ref region, attractor.sound_on, transform.position);
				}

				sync = true;
			}

			//if (control.mouse.GetKeyUp(Mouse.Key.Left))
			//{
			//	attractor_state.flags.SetFlag(ArcaneAttractor.State.Flags.Powered, false);
			//	Sound.Play(ref region, "arcane_infuser.off.00", transform.position);

			//	sync = true;
			//}

			if (sync)
			{
				attractor_state.Sync(entity);
			}
#endif
		}

		[ISystem.VeryLateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void UpdateSound(ISystem.Info info, Entity entity,
		[Source.Owned] in Transform.Data transform, [Source.Owned] ref Sound.Emitter sound_emitter,
		[Source.Owned] ref ArcaneAttractor.Data attractor, [Source.Owned] ref ArcaneAttractor.State attractor_state)
		{
			if (attractor_state.flags.HasAny(ArcaneAttractor.State.Flags.Powered))
			{
				sound_emitter.volume_mult = Maths.Lerp(sound_emitter.volume_mult, 1.00f, 0.10f);
			}
			else
			{
				sound_emitter.volume_mult = Maths.Lerp(sound_emitter.volume_mult, 0.00f, 0.10f);
			}
		}
	}
}