namespace TC2.Base.Components
{
	public static partial class ArcaneInfuser
	{
		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Region), IComponent.With<ArcaneInfuser.State>]
		public partial struct Data(): IComponent
		{
			[Editor.Picker.Position(relative: true)]
			public Vector2 offset;

			public float radius = 2.00f;
			public float power = 200.00f;
			public float heat_amount = 10.00f;

			public Sound.Handle sound_on = ArcaneInfuser.sound_on_default;
			public Sound.Handle sound_off = ArcaneInfuser.sound_off_default;
		}

		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public partial struct State: IComponent
		{
			[Flags]
			public enum Flags: uint
			{
				None = 0,

				[Save.Ignore] Powered = 1 << 0
			}

			public ArcaneInfuser.State.Flags flags;

			[Save.Ignore, Net.Ignore] public float next_update;
			[Save.Ignore, Net.Ignore] public float next_steam;
			[Save.Ignore, Net.Ignore] public float next_infuse;

			[Save.Ignore, Net.Ignore] public float last_infuse;
		}

		public static readonly Sound.Handle sound_on_default = "arcane_infuser.on.00";
		public static readonly Sound.Handle sound_off_default = "arcane_infuser.off.00";
		public static readonly Sound.Handle sound_fizzle_default = "arcane_infuser.fizzle.00";

#if CLIENT
		public partial struct InteractionGUI: IGUICommand
		{
			public Entity ent_collector;

			public ArcaneInfuser.Data collector;
			public ArcaneInfuser.State collector_state;

			public void Draw()
			{
				using (var window = GUI.Window.Interaction("Infuser"u8, this.ent_collector))
				{
					this.StoreCurrentWindowTypeID(order: -100);
					if (window.show)
					{
						ref var player = ref Client.GetPlayer();
						ref var region = ref Client.GetRegion();

						using (GUI.Group.New(size: new Vector2(48 * 1, 48 * 5) + new Vector2(24, 24)))
						{
							GUI.DrawFillBackground(GUI.tex_frame, new(8, 8, 8, 8));

							using (GUI.Group.New(size: new(GUI.RmX, 0.00f), padding: new(12, 12)))
							{
								GUI.DrawInventoryDock(Inventory.Type.Output, size: new(48 * 1, 48 * 4));
								GUI.DrawInventoryDock(Inventory.Type.Input, size: new(48 * 1, 48 * 1));
							}
						}
					}
				}
			}
		}

		[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnGUI(Entity entity, [Source.Owned] in ArcaneInfuser.Data collector, [Source.Owned] in ArcaneInfuser.State collector_state, [Source.Owned] in Interactable.Data interactable)
		{
			if (interactable.IsActive())
			{
				var gui = new InteractionGUI()
				{
					ent_collector = entity,

					collector = collector,
					collector_state = collector_state,
				};
				gui.Submit();
			}
		}
#endif

		[Shitcode]
		[ISystem.Event<EssenceNode.CollapseEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnCollapse(ISystem.Info info, Entity entity, ref Region.Data region, ref EssenceNode.CollapseEvent data, [Source.Owned] ref Transform.Data transform,
		[Source.Owned, Pair.Component<ArcaneInfuser.Data>] ref Inventory4.Data inventory_output, [Source.Owned, Pair.Component<ArcaneInfuser.State>] ref Inventory1.Data inventory_input,
		[Source.Owned] ref ArcaneInfuser.Data collector, [Source.Owned] ref ArcaneInfuser.State collector_state,
		[Source.Owned] in Heat.Data heat, [Source.Owned] ref Heat.State heat_state)
		{
			App.WriteLine($"{data.essence_amount_precipitated} of {data.h_essence}");

#if SERVER
			var amount_modifier = 0.20f + (Maths.InvLerp01(heat.temperature_medium, heat.temperature_high, heat_state.temperature_current) * 0.80f);

			ref var material_pellet = ref IMaterial.Database.GetData("pellet", out var material_pellet_id);
			if (material_pellet.IsNotNull())
			{
				var efficiency = 0.45f;

				var essence_consumed = data.essence_amount_precipitated * amount_modifier * data.random.NextFloatRange(0.50f, 1.00f);

				var resource_pellet = new Resource.Data(material_pellet_id);
				var amount_req = Maths.Floor((essence_consumed * efficiency) * Essence.essence_per_pellet_inv);
				var amount_rem = amount_req;

				App.WriteLine($"req: {amount_req}; rem: {amount_rem}; consumed: {essence_consumed}/{data.essence_amount_precipitated}");

				ref var essence_data = ref data.h_essence.GetData();
				if (essence_data.IsNotNull())
				{
					var h_pellet_essence = essence_data.h_material_pellet;
					if (h_pellet_essence.IsNotNull())
					{
						if (Resource.Withdraw(ref inventory_input, ref resource_pellet, ref amount_rem))
						{
							resource_pellet.material = h_pellet_essence;
							data.essence_amount_precipitated -= essence_consumed; // resource_pellet.quantity / Essence.essence_per_pellet;

							WorldNotification.Push(ref region, $"Collected {resource_pellet.quantity} {essence_data.name} Essence {(data.essence_amount_precipitated > 1 ? "pellets" : "pellet")}!", color: Color32BGRA.Green, transform.position, lifetime: 5.00f);
							App.WriteLine($"Collected {resource_pellet.quantity} {resource_pellet.material}!");
							Resource.Deposit(ref inventory_output, ref resource_pellet, resource_pellet.quantity);
						
						
						}
					}
				}
			}
#endif
		}

		[IEvent.Data]
		public partial struct InfuseEvent(): IEvent
		{
			public Vector2 world_position;
		}

		public static readonly Texture.Handle tex_blank = "blank";
		public static readonly Texture.Handle tex_light = "light.circle.00";
		public static readonly Texture.Handle tex_smoke = "BiggerSmoke_Light";
		public static readonly Texture.Handle tex_spark = "metal_spark.01";

		[ISystem.Event<ArcaneInfuser.InfuseEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnInfuse(ISystem.Info info, Entity entity, ref Region.Data region, ref XorRandom random, ref ArcaneInfuser.InfuseEvent data,
		[Source.Owned] ref Transform.Data transform, [Source.Owned] ref ArcaneInfuser.Data infuser, [Source.Owned] ref ArcaneInfuser.State infuser_state,
		[Source.Owned] in Heat.Data heat, [Source.Owned] ref Heat.State heat_state)
		{
			var amount_modifier = Maths.InvLerp01(heat.temperature_medium, heat.temperature_high, heat_state.temperature_current);
			//App.WriteLine(amount_modifier);

#if SERVER
			Sound.Play(ref region, ArcaneInfuser.sound_fizzle_default, transform.position, volume: 1.00f, pitch: random.NextFloatRange(0.70f, 0.75f), size: 1.00f);
			Shake.Emit(ref region, transform.position, 0.50f, 0.80f, 20.00f);
#endif

#if CLIENT
			if (Camera.IsVisible(Camera.CullType.Rect2x, transform.position))
			{
				var dir = transform.GetDirection();

				for (var i = 0; i < 40; i++)
				{
					Particle.Spawn(ref region, new Particle.Data()
					{
						texture = tex_blank,
						lifetime = random.NextFloatRange(0.10f, 1.00f),
						pos = transform.LocalToWorld(new Vector2(random.NextFloatRange(-0.25f, 0.25f), 0)) + random.NextUnitVector2Range(0.10f, 0.20f),
						vel = dir.RotateByRad(random.NextFloatRange(-2.50f, 2.50f)) * random.NextFloatRange(0, 80),
						fps = 0,
						scale = random.NextFloatRange(0.50f, 1.50f),
						growth = -random.NextFloatRange(1.60f, 4.00f),
						force = random.NextUnitVector2Range(0, 80),
						drag = random.NextFloatRange(0.00f, 0.40f),
						stretch = new Vector2(random.NextFloatRange(0.50f, 1.50f), 0.05f),
						color_a = random.NextColor32Range(0xffffffff, 0xffc89eff),
						color_b = random.NextColor32Range(0xffc89eff, 0xffffffff),
						lit = random.NextFloatRange(0.50f, 1.00f),
						face_dir_ratio = 1.00f
					});
				}

				for (var i = 0; i < 4; i++)
				{
					Particle.Spawn(ref region, new Particle.Data()
					{
						texture = tex_light,
						lifetime = random.NextFloatRange(0.01f, 0.10f),
						pos = transform.position + random.NextVector2(0.10f),
						vel = dir.RotateByRad(random.NextFloatRange(-2.00f, 2.00f)) * random.NextFloatRange(10.00f, 40.00f),
						force = dir.RotateByRad(random.NextFloatRange(-0.20f, 0.20f)) * random.NextFloatRange(0, 200),
						//fps = random.NextByteRange(3, 5),
						//stretch = new Vector2(0.5f, 0.2f),
						scale = random.NextFloatRange(2.00f, 30.00f),
						rotation = random.NextFloatRange(-3.50f, 3.50f),
						angular_velocity = random.NextFloat(0.50f),
						growth = random.NextFloatRange(10.00f, 200.00f),
						drag = random.NextFloatRange(0.07f, 0.25f),
						color_a = random.NextColor32Range(0xffffffff, 0xffc89eff),
						color_b = random.NextColor32Range(0xffc89eff, 0xffffffff),
						glow = random.NextFloatRange(2.00f, 8.00f),
					});
				}

				var smoke_count = (int)Maths.Lerp(0, 10, amount_modifier);

				for (var i = 0; i < smoke_count; i++)
				{
					Particle.Spawn(ref region, new Particle.Data()
					{
						texture = tex_smoke,
						lifetime = random.NextFloatRange(3.00f, 5.00f),
						pos = transform.position + random.NextVector2(1.00f),
						vel = random.NextUnitVector2Range(0.50f * i, 1.50f * i) + random.NextVector2(5.00f),
						fps = random.NextByteRange(5, 10),
						frame_count = 64,
						frame_count_total = 64,
						frame_offset = random.NextByteRange(0, 64),
						//scale = random.NextFloatRange(1.50f, 2.50f),
						scale = random.NextFloatRange(0.50f, 0.80f),
						rotation = random.NextFloat(10.00f),
						angular_velocity = random.NextFloat(1.00f),
						growth = random.NextFloatRange(0.15f, 0.20f),
						drag = random.NextFloatRange(0.02f, 0.04f),
						force = new Vector2(0.00f, random.NextFloatRange(0, -2)),
						color_a = random.NextColor32Range(0x80ffffff, 0xa0ffffff),
						color_b = random.NextColor32Range(0x00ffffff, 0x00808080)
					});
				}

				for (var i = 0; i < 20; i++)
				{
					Particle.Spawn(ref region, new Particle.Data()
					{
						texture = tex_spark,
						lifetime = random.NextFloatRange(1.00f, 2.00f),
						pos = transform.position + random.NextVector2(0.20f),
						vel = (dir * random.NextFloatRange(0, 40)).RotateByRad(random.NextFloatRange(-1.15f, 1.15f)) + random.NextVector2(15),
						fps = 0,
						frame_count = 1,
						frame_offset = random.NextByteRange(0, 4),
						frame_count_total = 4,
						scale = random.NextFloatRange(0.20f, 0.60f),
						growth = -random.NextFloatRange(0.60f, 1.00f),
						force = new Vector2(0.00f, random.NextFloatRange(20, 80)),
						drag = random.NextFloatRange(0.01f, 0.10f),
						stretch = new Vector2(random.NextFloatRange(1.00f, 2.00f), 0.75f),
						color_a = random.NextColor32Range(0xffffffff, 0xffc89eff),
						color_b = random.NextColor32Range(0xffffc0b0, 0xffffa090),
						lit = 1.00f,
						face_dir_ratio = 1.00f,
					});
				}
			}
#endif

			infuser_state.last_infuse = info.WorldTime;
			infuser_state.next_infuse = info.WorldTime + 5.00f;
			infuser_state.next_steam = 0.00f;

			//heat.heat_current = 30.00f;

#if SERVER
			Span<OverlapResult> results = stackalloc OverlapResult[8];
			if (region.TryOverlapPointAll(transform.position, infuser.radius, ref results, mask: Physics.Layer.Essence))
			{
				//var ev = new EssenceNode.ChargeEvent()
				//{
				//	ent_drainer = entity,
				//	power = collector.power * 0.001f,
				//	world_position = transform.position
				//};

				foreach (ref var result in results)
				{
					//result.entity.Notify(ref ev);

					ref var essence_node = ref result.entity.GetComponent<EssenceNode.Data>();
					if (!essence_node.IsNull())
					{
						if (!essence_node.flags.HasAll(EssenceNode.Flags.Collapsing))
						{
							//essence_node.stability = 0.00f;
							//essence_node.stability = Maths.Clamp01(essence_node.stability - random.NextFloatRange(0.30f, 0.50f));
							essence_node.flags.SetFlag(EssenceNode.Flags.Collapsing, true);
							essence_node.Sync(result.entity, true);

							//essence_node.stability = Maths.MoveTowards(essence_node.stability, 0.00f, arg.power * App.fixed_update_interval_s);
						}

						//if (info.WorldTime >= essence_node.next_drain && !essence_node.flags.HasAll(EssenceNode.Flags.Collapsing))
						//{
						//	essence_node.last_drain = info.WorldTime;
						//	essence_node.stability = Maths.Lerp(essence_node.stability, 0.00f, (infuser.power / essence_node.amount));

						//	//essence_node.stability = Maths.MoveTowards(essence_node.stability, 0.00f, arg.power * App.fixed_update_interval_s);
						//}
					}

					//ref var hit_body = ref result.entity.GetComponent<Body.Data>();
					//if (!hit_body.IsNull())
					//{
					//	var dir = (transform.position - hit_body.GetWorldCenterOfGravity() - hit_body.GetVelocity()).GetNormalized(out var dist);
					//	var ratio = (dist / collector.radius).Clamp01();

					//	var force = dir * (collector.power * ratio);
					//	Physics.LimitForce(ref hit_body, force, new Vector2(5, 5));

					//	hit_body.AddForce(force);
					//}
				}
			}
#endif
		}

		[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void Update(ISystem.Info info, Entity entity, ref Region.Data region, ref XorRandom random,
		[Source.Owned] in Transform.Data transform, [Source.Owned] ref ArcaneInfuser.Data infuser, [Source.Owned] ref ArcaneInfuser.State infuser_state,
		[Source.Owned, Pair.Component<ArcaneInfuser.Data>] ref Inventory4.Data inventory, [Source.Owned] in Control.Data control,
		[Source.Owned] in Heat.Data heat, [Source.Owned] ref Heat.State heat_state)
		{
			var time = info.WorldTime;

			if (infuser_state.flags.HasAny(ArcaneInfuser.State.Flags.Powered))
			{
				heat_state.AddPower(infuser.heat_amount, App.fixed_update_interval_s);

#if SERVER
				if (time >= infuser_state.next_update)
				{
					heat_state.Sync(entity, true);
				}
#endif
			}

			if ((time - infuser_state.last_infuse) < 10.00f)
			{
				//heat_state. = Maths.MoveTowards(heat.temperature_current, Maths.KelvinToCelsius(Region.ambient_temperature), 50.00f * (1.00f - Maths.Clamp01(time - infuser_state.last_infuse)));

#if CLIENT
				if (time >= infuser_state.next_steam)
				{
					var dir = transform.GetDirection();
					var modifier = Maths.Clamp(1.00f - Maths.Normalize(time - infuser_state.last_infuse, 10.00f), 0.50f, 1.00f);

					Particle.Spawn(ref region, new Particle.Data()
					{
						texture = tex_smoke,
						lifetime = random.NextFloatRange(3.00f, 5.00f),
						pos = transform.position + random.NextVector2(0.50f),
						vel = (dir * random.NextFloatRange(0.10f, 1.00f)) + random.NextVector2(2.00f),
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
						color_a = random.NextColor32Range(0x40ffffff, 0xc0ffffff).WithAlphaMult(modifier),
						color_b = random.NextColor32Range(0x00ffffff, 0x00808080)
					});

					infuser_state.next_steam = time + 0.30f;
				}
#endif
			}

#if SERVER
			var sync = false;

			if (control.mouse.GetKeyDown(Mouse.Key.Left))
			{
				if (time >= infuser_state.next_infuse)
				{
					var ev = new ArcaneInfuser.InfuseEvent();
					ev.world_position = transform.position;
					entity.TriggerEventDeferred(ev, sync: true);
				}
			}

			if (control.mouse.GetKeyDown(Mouse.Key.Right))
			{
				infuser_state.flags.SetFlag(ArcaneInfuser.State.Flags.Powered, true);
				Sound.Play(ref region, infuser.sound_on, transform.position);

				sync = true;
			}

			if (control.mouse.GetKeyUp(Mouse.Key.Right))
			{
				infuser_state.flags.SetFlag(ArcaneInfuser.State.Flags.Powered, false);
				Sound.Play(ref region, infuser.sound_off, transform.position);

				sync = true;
			}

			if (sync)
			{
				infuser_state.Sync(entity, true);
			}
#endif
		}

		[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void UpdateEffects(ISystem.Info info, Entity entity,
		[Source.Owned] in Transform.Data transform, [Source.Owned] ref ArcaneInfuser.Data collector, [Source.Owned] ref ArcaneInfuser.State collector_state,
		[Source.Owned, Pair.Component<ArcaneInfuser.Data>] ref Inventory4.Data inventory,
		[Source.Owned, Pair.Component<ArcaneInfuser.Data>] ref Light.Data light)
		{
			//var ts = Timestamp.Now();

			var light_color = default(Vector4);

			foreach (ref var resource in inventory)
			{
				var h_essence = resource.GetEssenceType();
				if (h_essence.IsValid())
				{
					ref var essence_data = ref h_essence.GetData();
					if (essence_data.IsNotNull())
					{
						ref var material_data = ref resource.GetMaterial();
						if (material_data.IsNotNull())
						{
							light_color = Vector4.Max(light_color, essence_data.color_emit * ((resource.quantity / material_data.quantity_max) * material_data.mass_per_unit * 2.00f));
						}
					}
				}
			}

			light.color = ColorBGRA.ARGB(light_color);

			//App.WriteLine($"{ts.GetMilliseconds():0.0000} ms");
		}

		[ISystem.VeryLateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void UpdateSound(ISystem.Info info, Entity entity,
		[Source.Owned] in Transform.Data transform, [Source.Owned] ref Sound.Emitter sound_emitter,
		[Source.Owned] ref ArcaneInfuser.Data collector, [Source.Owned] ref ArcaneInfuser.State collector_state)
		{
			if (collector_state.flags.HasAny(ArcaneInfuser.State.Flags.Powered))
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