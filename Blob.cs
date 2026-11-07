
using Keg.Engine.Game;

namespace TC2.Base.Components
{
	public static partial class Blob
	{
		[IComponent.Data(Net.SendType.Reliable, region_only: true), IComponent.With<Blob.State>]
		public partial struct Data(): IComponent
		{
			[Flags]
			public enum Flags: ushort
			{
				None = 0,

				Unlimited = 1 << 0,
			}

			public enum Type: byte
			{
				Undefined,

				Liquid,
				Melt,
				Slime,
				Plasma,
				Gas,
				Steam,
				Slurry,
				Powder,
				Debris,
				Essence,
			}

			public ISubstance.Handle h_substance;
			public Material.Form form_type = Material.Form.Chunk;
			public Temperature temperature = Temperature.Ambient;
			public Blob.Data.Flags flags;
			public Blob.Data.Type type;
			//public Color32BGRA color;

			[Net.Segment.B, Editor.Picker.Position(true, true)] public Vector2 offset;

			[Net.Segment.B] public float initial_velocity_mult = 0.30f;
			[Net.Segment.B] public float size_mult = 1.00f;
			[Net.Segment.B] public float falling_step = 3.50f;
			[Net.Segment.B] public float surface_step = 2.00f;

			[Net.Segment.B] public float min_distance = 1.25f;
			[Net.Segment.B] public float max_distance = 0.75f;
			[Net.Segment.B] public float interval_reset = 0.80f;
			[Net.Segment.B] public float interval_effect = 0.10f;
		}

		[IComponent.Data(Net.SendType.Unreliable, region_only: true)]
		public partial struct State(): IComponent
		{
			[Flags]
			public enum Flags: ushort
			{
				None = 0,


			}

			[Net.Segment.B] public float flow_velocity;
			[Net.Segment.B] public Mass mass_flow_rate;
			[Net.Segment.B] public Energy specific_heat_capacity;
			[Net.Segment.B] public ColorBGRA tint = ColorBGRA.White;
			[Net.Segment.B, Editor.Picker.Position(false, true)] public Vector2 pos_source;

			[Net.Segment.C] public byte current_count;
			[Net.Segment.C] public byte current_index;
			[Net.Segment.C] public Blob.State.Flags flags;
			[Net.Segment.C] public Mass remaining_mass;
			[Net.Segment.C] public Mass available_mass;
			[Net.Segment.C] public float radius = 0.50f;
			[Net.Segment.C] public FixedArray8<Vector2> points;

			[Asset.Ignore] public EntRef<Transform.Data> ref_transform;
			[Asset.Ignore, Net.Ignore, Save.Ignore] public FixedArray8<float> distances;
			[Asset.Ignore, Net.Ignore, Save.Ignore] public Vector2 pos_prev;
			[Asset.Ignore, Net.Ignore, Save.Ignore] public float current_distance;
			[Asset.Ignore, Net.Ignore, Save.Ignore] public float t_flow_elapsed;
			[Asset.Ignore, Net.Ignore, Save.Ignore] public float t_next_sync;
			[Asset.Ignore, Net.Ignore, Save.Ignore] public float t_next_reset;
			[Asset.Ignore, Net.Ignore, Save.Ignore] public float t_next_effect;
		}

		[IEvent.Data]
		public ref struct ContactEvent: IEvent
		{
			[Flags]
			public enum Flags: uint
			{
				None = 0u,


			}

			public readonly Entity ent_blob;

			public readonly ref Blob.Data blob;
			public readonly ref Blob.State blob_state;
			public readonly ref Temperature temperature;
			public readonly ref Mass mass;

			public Blob.ContactEvent.Flags flags;

			public Vector2 velocity;
			public Vector2 world_position;

			public ContactEvent(Entity ent_blob, ref Blob.Data blob, ref Blob.State blob_state, ref Temperature temperature, ref Mass mass, Blob.ContactEvent.Flags flags, Vector2 velocity, Vector2 world_position)
			{
				this.ent_blob = ent_blob;

				this.blob = ref blob;
				this.blob_state = ref blob_state;
				this.temperature = ref temperature;
				this.mass = ref mass;

				this.flags = flags;

				this.velocity = velocity;
				this.world_position = world_position;
			}
		}

		[IEvent.Data]
		public struct PourEvent(): IEvent, IEvent<Crafting.ConfiguredRecipe>, IDrawable<Crafting.ConfiguredRecipe>
		{
			[Flags]
			public enum Flags: uint
			{
				None = 0u,

				//Unlimited = 1 << 0,
			}

			[Asset.Ignore, Save.Ignore] public Entity ent_parent;
			//[Asset.Ignore] public EntRef<Transform.Data> ref_transform;

			public readonly Prefab.Handle h_prefab;
			public ISubstance.Handle h_substance;
			public Temperature temperature;
			public Mass mass;
			public float rate;
			//public Color32BGRA color;
			public Blob.PourEvent.Flags flags;

			public Blob.Data.Flags flags_add;
			public Blob.Data.Flags flags_rem;
			public Vector2 offset;

			//public Vector2 velocity;
			//public Vector2 world_position;

			void IEvent<Crafting.ConfiguredRecipe>.Bind(ref Crafting.ConfiguredRecipe arg)
			{
				this.ent_parent = arg.ent_target;

				//this.ref_transform.Set(arg.ent_target);
				this.temperature = arg.context.temperature;
				//this.world_position.SetIfHasValue(arg.context.world_position);

				ref var prd_substance = ref arg.products.GetFirstOrNull((x) => x.type == Crafting.Product.Type.Substance && x.flags.HasAny(Crafting.Product.Flags.Parameter));
				if (prd_substance.IsNotNull())
				{
					this.h_substance = prd_substance.h_substance;
					this.mass += prd_substance.GetProductAmount(ref arg.context.random, arg.amount_multiplier);
				}

				//App.WriteLine($"Bind {this.ent_parent}; {this.h_substance}; {this.h_prefab}; {this.mass}");
			}

#if CLIENT
			void IDrawable<Crafting.ConfiguredRecipe>.OnDraw(ref Crafting.ConfiguredRecipe arg, GUI.Group group)
			{
				GUI.NewLine(3);

				using (var group_row = GUI.Group.New(size: new(GUI.RmX, 32), padding: new(6, 4)))
				{
					group_row.DrawBackground(GUI.tex_panel);

					//GUI.TitleCentered("Form Type:"u8, pivot: new(0, 0), offset: new(4, 0));
					GUI.TitleCentered($"> Pour {this.h_prefab.GetName()} ({this.h_substance.GetName()}) <", size: 20, pivot: new(0.50f, 0.50f), color: GUI.col_button_yellow);
				}
			}
#endif
		}

		[ISystem.AddFirst(ISystem.Mode.Single, ISystem.Scope.Region, defer: true)]
		public static void OnAdd(ISystem.Info info, ref Region.Data region, ref XorRandom random, Entity entity,
		[Source.Owned] ref Transform.Data transform,
		[Source.Owned] ref Body.Data body, [Source.Owned, Pair.Component<Body.Data>] ref Shape.Circle shape,
		[Source.Owned] ref Blob.Data blob, [Source.Owned] ref Blob.State blob_state)
		{
			//App.WriteLine($"init blob {transform.position}");

			blob_state.pos_source = transform.position;
			blob_state.pos_prev = blob_state.pos_source;
			blob_state.points = new FixedArray8<Vector2>(true, transform.position);
		}

#if SERVER
		[ISystem.Event<Blob.PourEvent>(ISystem.Mode.Single, ISystem.Scope.Region, order: -50)]
		public static void OnPourEvent(ISystem.Info info, ref Region.Data region, Entity entity, ref XorRandom random, [Source.Owned] ref Blob.PourEvent ev,
		[Source.Owned] ref Body.Data body, [Source.Owned] ref Transform.Data transform,
		[Source.Owned] ref Blob.Data blob, [Source.Owned] ref Blob.State blob_state,
		[Source.Owned, Optional(true)] ref Heat.State heat_state)
		{
			//var record = ent.GetRecord();

			App.WriteLine($"Blob OnPourEvent {ev.h_substance}; {ev.h_prefab}; {ev.mass}");

			blob.h_substance = ev.h_substance;
			blob.temperature = ev.temperature;
			blob.offset = ev.offset;
			blob.flags.AddRemFlags(ev.flags_add, ev.flags_rem);
			blob.Sync(entity, true);

			blob_state.remaining_mass = ev.mass;
			blob_state.mass_flow_rate = ev.rate;
			blob_state.ref_transform.Set(ev.ent_parent);
			blob_state.Sync(entity, true);

			if (heat_state.IsNotNull())
			{
				heat_state.temperature_current = ev.temperature;
				heat_state.Sync(entity);
			}
		}
#endif

#if SERVER
		[ChatCommand.Region("pour", "", creative: true)]
		public static void PourCommand(ref ChatCommand.Context context, ISubstance.Handle h_substance, float mass, float rate = 10.00f, float? temperature = null)
		{
			ref var region = ref context.GetRegion();
			Assert.IsNotNull(ref region);

			ref var substance_data = ref h_substance.GetData();
			Assert.IsNotNull(ref substance_data);

			Assert.Check(context.GetTargetPosition().TryGetValueFromRef(out var pos));
			var temperature_ambient = region.GetAmbientTemperature(pos);

			var h_prefab = default(Prefab.Handle);
			if (temperature.TryGetValue(out var v_temperature))
			{
				if (substance_data.boiling_point > 0.00f && v_temperature > substance_data.boiling_point)
				{
					h_prefab = "blob.gas";
				}
				else if (substance_data.melting_point > 0.00f && v_temperature > substance_data.melting_point)
				{
					if (substance_data.tags.HasAny(Substance.Tags.Liquid))
					{
						h_prefab = "blob.liquid";
					}
					else
					{
						h_prefab = "blob.melt";
					}
				}
				else
				{
					h_prefab = "blob.sand";
				}
			}
			else
			{
				if (substance_data.tags.HasAny(Substance.Tags.Gas))
				{
					h_prefab = "blob.gas";
					v_temperature = Maths.Max(temperature_ambient, substance_data.boiling_point + 20);
				}
				else if (substance_data.tags.HasAny(Substance.Tags.Liquid))
				{
					h_prefab = "blob.liquid";
					v_temperature = Maths.Max(temperature_ambient, substance_data.melting_point + 20);
				}
				else if (substance_data.tags.HasAny(Substance.Tags.Solid))
				{
					if (substance_data.tags.HasAny(Substance.Tags.Metal) && substance_data.melting_point > 0.00f)
					{
						h_prefab = "blob.melt";
						v_temperature = Maths.Max(temperature_ambient, Maths.Lerp(substance_data.melting_point, substance_data.boiling_point.WithFallback(substance_data.melting_point + 300), 0.20f));
					}
					else
					{
						h_prefab = "blob.sand";
						v_temperature = temperature_ambient;
					}
				}
			}
			
			if (Assert.Check(h_prefab.TryGetPrefab(out var prefab)))
			{
				region.SpawnPrefab2(prefab, position: pos).ContinueWith(ent =>
				{
					var record = ent.GetRecord();
					ref var substance_data = ref h_substance.GetData();

					ref var blob = ref record.GetComponent<Blob.Data>();
					ref var blob_state = ref record.GetComponent<Blob.State>();
					if (blob.IsNotNull() && blob_state.IsNotNull())
					{
						blob.h_substance = h_substance;
						blob_state.remaining_mass = mass;
						blob_state.mass_flow_rate = rate;

						if (substance_data.IsNotNull())
						{
							//blob_state.tint = substance_data.color_molten;
							//temperature = substance_data.melting_point + 300.00f;
						}

						blob.temperature = v_temperature;

						//blob.temperature.SetIfHasValue(temperature);
					}

					ref var heat = ref record.GetComponent<Heat.Data>();
					ref var heat_state = ref record.GetComponent<Heat.State>();
					if (heat.IsNotNull() && heat_state.IsNotNull())
					{
						heat_state.temperature_current = v_temperature;
					}
				});
			}
		}
#endif

#if SERVER
		[ISystem.Event<Blob.ContactEvent>(ISystem.Mode.Single, ISystem.Scope.Region, order: 10)]
		public static void OnContact(ISystem.Info info, ref Region.Data region, Entity entity, ref Blob.ContactEvent ev, ref XorRandom random,
		[Source.Owned] ref Body.Data body, [Source.Owned, Optional(true)] ref Heat.State heat_state)
		{
			//heat_state.temperature_current.m_value.MoveTowards(ev.temperature.m_value, 50);

			// if (resource.IsNotNull() && resource.material.GetSubstanceHandle() == ev.blob.h_substance)
			// {
			// 	resource.AddHeatedMass(ref heat_state.temperature_current, ev.mass, ev.temperature);
			// 	resource.Sync(entity, true);
			// }
			// else

			var layer = body.GetLayer();
			switch (ev.blob.type)
			{
				case Data.Type.Melt:
				{
					if (layer.HasAny(Physics.Layer.Flammable | Physics.Layer.Organic | Physics.Layer.Plant))
					{
						Fire.Ignite(ent_target: entity, step: (ev.temperature * 0.001f) * ev.mass, intensity: (ev.temperature * 0.002f) * ev.mass, flags: Fire.Flags.No_Contact_Ignite, temperature: ev.temperature);
					}

					if (heat_state.IsNotNull())
					{
						heat_state.temperature_current = Temperature.Merge(heat_state.temperature_current, ev.temperature, body.GetMass(), ev.mass);
						heat_state.Sync(entity, true);
						//App.WriteLine("blob: on contact heatable");
					}
					else
					{
						var damage_type = Damage.Type.Steam;

						var temperature_celsius = ev.temperature.Celsius();
						if (temperature_celsius >= 1337.00f) damage_type = Damage.Type.Phlogiston;
						else if (temperature_celsius >= 550.00f) damage_type = Damage.Type.Fire;
						else if (temperature_celsius >= 120.00f) damage_type = Damage.Type.Heat;

						var damage = temperature_celsius * ev.mass * 0.55f * random.NextFloatRange(0.50f, 0.90f);
						Damage.Hit(ent_attacker: ev.ent_blob,
							ent_owner: ev.ent_blob,
							ent_target: entity,
							position: ev.world_position,
							velocity: new Vector2(0.00f, -0.25f),
							normal: new Vector2(0.00f, -1.00f),
							damage_integrity: damage,
							damage_durability: damage,
							damage_terrain: damage,
							target_material_type: body.GetMaterial(),
							damage_type: damage_type,
							yield: 0.90f,
							xp_modifier: 0.00f,
							impulse: 0.00f,
							flags: Damage.Flags.No_Loot_Pickup);
					}
				}
				break;
			}
		}

		//[ISystem.Event<Blob.ContactEvent>(ISystem.Mode.Single, ISystem.Scope.Region, order: 10)]
		//public static void OnContactHeatableResource(ISystem.Info info, ref Region.Data region, Entity entity, ref Blob.ContactEvent ev,
		//[Source.Owned] ref Body.Data body, [Source.Owned] ref Heat.State heat_state, [Source.Owned] ref Resource.Data resource)
		//{
		//	//heat_state.temperature_current.m_value.MoveTowards(ev.temperature.m_value, 50);

		//	//heat_state.temperature_current = Temperature.Merge(heat_state.temperature_current, ev.temperature, body.GetMass(), ev.mass);


		//	resource.AddHeatedMass(ref heat_state.temperature_current, ev.mass, ev.temperature);
		//	heat_state.Sync(entity, true);

		//	App.WriteLine("blob: on contact heatable resource");


		//	//var temperature_new = Temperature.Merge(heat_state.temperature_current, ev.temperature, resource.quantity * mass_per_unit, amount_rem * mass_per_unit);
		//	//App.WriteLine($"OnMergeResource(): add temperature: {ev.temperature} to {heat_state.temperature_current} => {temperature_new}; amount_rem: {amount_rem}");

		//	//heat_state.temperature_current = temperature_new;
		//	//heat_state.Sync(ent_resource, true);

		//}
#endif

		//[ISystem.Update.B(ISystem.Mode.Single, ISystem.Scope.Region)]
		//public static void OnUpdateParent(ISystem.Info info, ref Region.Data region, ref XorRandom random,
		//Entity ent_body, Entity ent_transform_parent,
		//[Source.Owned] ref Body.Data body, [Source.Parent] in Transform.Data transform_parent,
		//[Source.Owned] ref Blob.Data blob, [Source.Owned] ref Blob.State blob_state)
		//{
		//	blob_state.pos_source = transform_parent.LocalToWorld(blob.offset);
		//	blob_state.ent_parent_cached = ent_transform_parent;
		//}

#if SERVER
		[Shitcode]
		[ISystem.Update.B(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnUpdateFlow(ISystem.Info info, ref Region.Data region, ref XorRandom random, Entity entity,
		[Source.Owned] ref Transform.Data transform,
		[Source.Owned] ref Body.Data body, [Source.Owned, Pair.Component<Body.Data>] ref Shape.Circle shape,
		[Source.Owned] ref Blob.Data blob, [Source.Owned] ref Blob.State blob_state,
		[Source.Owned, Optional(true)] ref Heat.State heat_state)
		{
			var sync = false;
			if (blob_state.remaining_mass <= blob_state.available_mass || blob_state.available_mass < 0.00f)
			{
				// var points_span = blob_state.points.AsSpan(blob_state.current_count);
				// var point_last = points_span.GetLastOrDefault(body.GetPosition());

				blob_state.pos_source = Maths.MoveTowards(blob_state.pos_source, body.GetPosition(), 0.10f);
				blob_state.points[0] = blob_state.pos_source;

				sync = true;
				// blob_state.Sync(entity, )

				if (blob_state.available_mass <= 0.50f)
				{
					entity.Delete();
					return;
				}
			}
			else
			{
				ref var transform_parent = ref blob_state.ref_transform.GetComponent();
				if (transform_parent.IsNotNull())
				{
					blob_state.pos_source = transform_parent.LocalToWorld(blob.offset);
				}
			}

			var time = info.WorldTime;
			var dt = info.DeltaTime;

			var current_index = blob_state.current_index;

			blob_state.t_flow_elapsed += dt;

			//var pos_contact = body.GetAnyContact();

			var has_arbiters = body.HasArbiters();
			var pos_current = Maths.Avg((body.GetAnyContact() ?? body.GetPosition()), body.GetPosition()); // transform.position;

			
			var reset = false;

			var pos_point = current_index == 0 ? blob_state.pos_source : blob_state.points[current_index - 1];

			var temperature = heat_state.IsNotNull() ? heat_state.temperature_current : region.GetAmbientTemperature(pos_current);
			//blob_state.available_mass += (Mass)Maths.Min(Maths.Max(blob_state.remaining_mass.m_value * 0.10f, 0.10f), blob_state.mass_flow_rate.m_value * random.NextFloatExtra(0.75f, 0.25f));
			//blob_state.available_mass += (Mass)Maths.Min(blob_state.remaining_mass * 0.65f, blob_state.mass_flow_rate.m_value * random.NextFloatExtra(0.75f, 0.25f) * dt);
			ref var mass = ref blob_state.available_mass;
			if (blob.flags.HasAny(Data.Flags.Unlimited))
			{
				var remaining_mass = blob_state.remaining_mass;
				mass += remaining_mass.m_value.Take(blob_state.mass_flow_rate.m_value * dt);
			}
			else
			{
				mass += blob_state.remaining_mass.m_value.Take(blob_state.mass_flow_rate.m_value * dt);
			}
			// App.WriteLine(mass);

			//var mass = blob_state.available_mass;
			//var mass_cached = mass;

			//var mass = (Mass)Maths.Min(Maths.Max(blob_state.remaining_mass.m_value * 0.10f, 0.10f), blob_state.mass_flow_rate.m_value * random.NextFloatExtra(0.75f, 0.25f));
			//var mass_cached = mass;

			var delta = (pos_point - pos_current);
			var dir = delta.GetNormalized(out var dist);

			var ev = new ContactEvent(ent_blob: entity,
				blob: ref blob,
				blob_state: ref blob_state,
				temperature: ref temperature,
				mass: ref mass,
				flags: ContactEvent.Flags.None,
				velocity: dir * dist,
				world_position: pos_current);

			if (dist >= blob.falling_step || time >= blob_state.t_next_sync || (has_arbiters && (dist >= blob.surface_step || (body.TryGetFirstArbiter(out var arbiter_first) && arbiter_first.GetState() == Body.Arbiter.State.Begin))))
			{
				blob_state.t_next_sync = time + 0.20f;

				if (has_arbiters)
				{
					foreach (var arbiter in body.GetArbiters())
					{
						var state = arbiter.GetState();
						if (state == Body.Arbiter.State.Begin)
						{
							var ent_arbiter = arbiter.GetEntity();
							if (ent_arbiter != blob_state.ref_transform.entity && ent_arbiter.IsAlive())
							{
								ev.world_position = arbiter.GetContactsPosition();

								ev.Trigger(ent_arbiter);
								if (mass <= 0.00f) break;
							}
						}
					}

					//App.WriteLine(ev.mass);

					if (mass <= 0.00f || body.GetVelocity().LengthSquared() <= 2.50f)
					{
						reset = true;
					}
				}

				if (dist >= blob.min_distance)
				{
					var dist_cached = blob_state.distances[current_index];

					blob_state.points[current_index] = pos_current;
					blob_state.distances[current_index] = dist;

					blob_state.current_index++;
					if (Maths.Abs(dist - dist_cached) >= blob.max_distance)
					{
						blob_state.current_count = (byte)(blob_state.current_index);
						reset = true;
					}
					else
					{
						blob_state.current_count = (byte)Maths.Max(blob_state.current_count, blob_state.current_index);
					}

					sync = true;
				}
				else
				{

				}
			}

			// region.DrawDebugCircle(blob_state.pos_source, 0.250f, Color32BGRA.Magenta, filled: true);

			if (blob_state.pos_prev != default)
			{
				blob_state.current_distance += Vector2.Distance(blob_state.pos_prev, pos_current);
			}
			blob_state.pos_prev = pos_current;

			//App.WriteLine(blob_state.current_distance);

			if (reset || time >= blob_state.t_next_reset || blob_state.current_index >= blob_state.points.Length)
			{
				sync = true;

				if (heat_state.IsNotNull())
				{
					//heat_state.temperature_current = substance_data.melting_point + 300;
					heat_state.temperature_current = blob.temperature;
					heat_state.Sync(entity, true);
				}

				ref var substance_data = ref blob.h_substance.GetData();
				if (substance_data.IsNotNull())
				{
					blob_state.specific_heat_capacity = substance_data.specific_heat_capacity;

					if (substance_data.tags.HasAny(Substance.Tags.Solid))
					{
						if (substance_data.boiling_point > 0.00f && blob.temperature > substance_data.boiling_point)
						{
							blob_state.tint = substance_data.color_flame;
						}
						else if (substance_data.melting_point > 0.00f && blob.temperature > substance_data.melting_point)
						{
							blob_state.tint = substance_data.color_molten;
						}
						else
						{
							blob_state.tint = substance_data.color_dust;
						}
					}
					else if (substance_data.tags.HasAny(Substance.Tags.Liquid))
					{
						blob_state.tint = substance_data.color_liquid;
					}
					else if (substance_data.tags.HasAny(Substance.Tags.Gas))
					{
						blob_state.tint = substance_data.color_smoke;
					}
					else
					{
						blob_state.tint = substance_data.color_gib;
					}

					//blob_state.tint = substance_data.color_molten;

					//if (heat_state.IsNotNull() && blob.type == Blob.Data.Type.Lava)
					//{
					//	heat_state.temperature_current = substance_data.melting_point + 300;
					//	heat_state.Sync(entity, true);
					//}
					//blob_state.mass_flow_rate = 


					if (has_arbiters)
					{
						foreach (var arbiter in body.GetArbiters())
						{
							// var state = arbiter.GetState();
							// App.WriteLine($"{arbiter.GetEntity()} {state}");
							// if (state == Body.Arbiter.State.Begin)
							{
								var ent_arbiter = arbiter.GetEntity();
								if (ent_arbiter != blob_state.ref_transform.entity && ent_arbiter.IsAlive())
								{
									ev.world_position = arbiter.GetContactsPosition();

									//App.WriteLine(ev.mass);
									ev.Trigger(ent_arbiter);
									if (mass <= 0.00f) goto done;
								}
							}
						}
					}
					
					if ((mass >= 5.00f || mass >= blob_state.remaining_mass) && (has_arbiters || (blob_state.current_distance >= 15.00f)))
					{
						ref var material_data = ref substance_data.GetMaterial(blob.form_type, out var material_asset);
						if (material_data.IsNotNull())
						{
							var h_material = material_asset.GetHandle();
							
							var mass_spawn = Maths.Max(0.50f, mass * random.NextFloatExtra(0.10f, 0.50f));
							if (has_arbiters || time >= blob_state.t_next_reset)
							{
								Resource.Spawn(region: ref region,
									material: h_material,
									world_position: body.GetPosition() + random.NextUnitVector2(0.20f),
									amount: mass_spawn * h_material.GetMassPerUnitInv(),
									max_distance: random.NextFloatExtra(0.50f, 2.00f),
									velocity: random.NextVector2Range(new Vector2(-3, 3), new Vector2(-4, -2)),
									flags: Resource.SpawnFlags.Merge,
									temperature: temperature);
								//blob_state.available_mass -= mass;
								mass -= mass_spawn;
							}
						}
						else
						{
							mass *= 0.75f;
						}
					}
				}

				done:

				//blob_state.available_mass = 100.00f;
				blob_state.t_next_sync = time;
				blob_state.t_next_reset = time + blob.interval_reset;
				blob_state.current_count = blob_state.current_index;
				blob_state.current_index = 0;
				blob_state.current_distance = 0.00f;
				blob_state.pos_prev = blob_state.pos_source;
				blob_state.t_flow_elapsed = 0.00f;
				//blob_state.radius = (Maths.Sqrt(blob_state.mass_flow_rate) * blob.h_substance.GetDensityInv() * 50.00f).ClampMin(0.50f);
				blob_state.radius = (Maths.Sqrt(Maths.Min(Maths.Max((blob_state.remaining_mass.m_value + blob_state.available_mass) * 0.15f, 0.10f), blob_state.mass_flow_rate)) * 0.10f).Clamp(0.15f, 1.00f) * blob.size_mult;

				// var surface_v = shape.GetSurfaceVelocity();
				// surface_v.X.ToggleSign();

				// shape.SetSurfaceVelocity(surface_v);

				shape.radius = Maths.Max(0.50f, blob_state.radius);
				shape.mass = Maths.Max(1.00f, blob_state.available_mass);
				shape.Modified<Body.Data, Shape.Circle>(entity, true);

				body.SetVelocity(region.GetGravity() * blob.initial_velocity_mult * random.NextFloatRange(0.90f, 1.00f));
				body.SetAngularVelocity(0.00f);
				body.SetRotation(0.00f);
				body.SetPosition(blob_state.pos_source);
			}

			//if (blob.flags.HasNone(Blob.Data.Flags.Unlimited))
			//{
			//	blob_state.remaining_mass += mass - mass_cached; // Maths.Max(mass_cached - mass, 0.00f);
			//}

			if (sync)
			{
				blob_state.Sync(entity, true);
			}
		}
#endif

		[ISystem.Update.C(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnUpdateFire(ISystem.Info info, ref Region.Data region, ref XorRandom random, Entity entity,
		[Source.Owned] ref Transform.Data transform,
		[Source.Owned] ref Blob.Data blob, [Source.Owned] ref Blob.State blob_state,
		[Source.Owned] ref Fire.Data fire)
		{
			fire.radius = blob_state.radius;
			fire.ignite_radius_multiplier = 1.20f;

			ref var substance_data = ref blob.h_substance.GetData();
			if (substance_data.IsNotNull())
			{
				fire.color_tint = substance_data.color_flame;
			}

#if SERVER

#endif
		}

		//[ISystem.Update.B(ISystem.Mode.Single, ISystem.Scope.Region)]
		//public static void OnUpdateB(ISystem.Info info, ref Region.Data region, ref XorRandom random, Entity entity,
		//[Source.Owned] ref Transform.Data transform, [Source.Owned, Pair.Component<Body.Data>] ref Shape.Circle shape,
		//[Source.Owned] ref Blob.Data blob, [Source.Owned] ref Blob.State blob_state)
		//{

		//}

		//[ISystem.PostUpdate.C(ISystem.Mode.Single, ISystem.Scope.Region)]
		//public static void OnPostUpdateC(ISystem.Info info, ref Region.Data region, ref XorRandom random, Entity entity,
		//[Source.Owned] ref Transform.Data transform, [Source.Owned, Pair.Component<Body.Data>] ref Shape.Circle shape,
		//[Source.Owned] ref Heat.Data heat, [Source.Owned] ref Heat.State heat_state,
		//[Source.Owned] ref Blob.Data blob, [Source.Owned] ref Blob.State blob_state)
		//{

		//}

		public static readonly Texture.Handle tex_splash_00 = "splash.00";
		public static readonly Texture.Handle tex_splash_00_smooth = "splash.00.smooth";
		public static readonly Texture.Handle tex_splash_00_glow = "splash.00.glow";
		public static readonly Texture.Handle tex_splash_01_glow = "splash.01.glow";
		public static readonly Texture.Handle tex_splash_02_glow = "splash.02.glow";

		public static readonly Texture.Handle tex_smoke = "BiggerSmoke_Light";

		[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnUpdateEffects(ISystem.Info info, ref Region.Data region, ref XorRandom random, Entity entity,
		[Source.Owned] in Transform.Data transform,
		[Source.Owned] ref Blob.Data blob, [Source.Owned] ref Blob.State blob_state,
		[Source.Owned, Optional(true)] ref Heat.State heat_state,
		[Source.Owned, Pair.Component<Blob.Data>, Optional(true)] ref Sound.Emitter sound_emitter,
		[Source.Owned, Pair.Component<Blob.Data>, Optional(true)] ref Light.Data light)
		{
#if CLIENT
			var time = info.WorldTime;
			var points = blob_state.points;

			var modifier = blob_state.radius; // (Maths.Sqrt(Maths.Min(Maths.Max((blob_state.remaining_mass.m_value + blob_state.available_mass) * 0.15f, 0.10f), blob_state.mass_flow_rate)) * 0.10f).ClampMin(0.15f);

			if (sound_emitter.IsNotNull())
			{
				var points_span = points.AsSpan(blob_state.current_count);
				var point_last = points_span.GetLastOrDefault(blob_state.pos_source);

				sound_emitter.volume_mult.LerpFMARef(Maths.Lerp01(0.30f, 1.45f, modifier), 0.05f);
				sound_emitter.pitch_mult.LerpFMARef(Maths.Lerp01(1.40f, 0.75f, modifier), 0.05f);
				sound_emitter.offset = Maths.Avg(point_last, blob_state.pos_source);
			}

			if (time >= blob_state.t_next_effect)
			{
				blob_state.t_next_effect = time + blob.interval_effect;
				var mass = (Mass)Maths.Min((blob_state.remaining_mass.m_value + blob_state.available_mass), blob_state.mass_flow_rate.m_value.Sqrt() * 0.10f) * random.NextFloatExtra(0.75f, 0.25f);
				if (mass >= 0.10f)
				{
					var strength = mass.m_value;

					
					//var points = blob_state.points;


					//region.DrawDebugLine(pos_prev, blob_state.pos_source, color: blob.color, thickness: blob.thickness);

					var pos_prev = blob_state.pos_source; // transform.position;
					var dir = new Vector2(0, 1);

					var temperature_current = Temperature.Ambient;

					if (heat_state.IsNotNull())
					{
						temperature_current = heat_state.temperature_current;
						//color_glow = Temperature.GetGlowColor(1200 + heat_state.temperature_current).WithColorMult(2);
						//color_glow_b = Temperature.GetGlowColor(700).WithColorMult(2);



					}

					var color_glow = Temperature.GetGlowColor(temperature_current);
					var color_glow_b = Temperature.GetGlowColor(700);

					var color_glow_clamped = color_glow.WithAlpha(1.00f);
					var color_spark = color_glow;
					var intensity = 1.00f;

					//var strength = (Maths.Min(blob_state.mass_flow_rate, blob_state.available_mass.m_value * App.fixed_update_interval_s)).ReplaceNaN(0.10f);

					var smoke_speed_mult = 1.05f;
					var smoke_size_mult = 1.75f;

					var dist_total = 0.00f;

					for (var pos_index = 0; pos_index < blob_state.current_count; pos_index++)
					{
						var pos = points[pos_index];

						// region.DrawDebugCircle(pos, 0.250f, Color32BGRA.Yellow, filled: true);
						// region.DrawDebugLine(pos_prev, pos, color: Color32BGRA.Magenta.WithAlpha(150), thickness: 10.00f);

						var step_count = (pos_index < blob_state.current_count - 1) ? 3 : 1;
						for (var step_index = 0; step_index < step_count; step_index++)
						{
							dir = (pos - pos_prev).GetNormalized(out var dist);
							// if (dist >= 7.00f || dist < 0.75f) break;

							// region.DrawDebugDir(pos_prev, dir * dist * 2, Color32BGRA.Red, 4);
							// region.DrawDebugCircle(pos_prev, 0.375f, Color32BGRA.Red, filled: true);
							dist = Maths.Clamp(dist, 0.00f, 7.00f);
							dist_total += dist;
							
							// if (pos_index == blob_state.current_count - 1) dist *= 0.10f;

							switch (blob.type)
							{
								case Blob.Data.Type.Liquid:
								{
									Particle.Spawn(ref region, new Particle.Data()
									{
										texture = tex_splash_00_smooth,
										lifetime = random.NextFloatRange(0.30f, 0.40f),
										pos = pos_prev + random.NextVector2(0.20f),
										vel = (dir * 5 * dist * random.NextFloatRange(0.75f, 0.90f)) + random.NextVector2(1.10f),
										//fps = random.NextByteRange(4, 12),
										//frame_count = 16,
										//frame_count_total = 16,
										//frame_offset = random.NextByteRange(2, 6),
										scale = random.NextFloatRange(0.95f, 1.20f) * strength.Mulpo(0.50f) * modifier,
										// rotation = dir.GetAngleRadiansFast() + MathF.PI * 0.50f,
										angular_velocity = random.NextFloat(4.00f) * modifier,
										face_dir_ratio = 1.00f,
										growth = random.NextFloatRange(3.50f, 6.80f),
										//pos = pos_prev + random.NextVector2(1.20f),
										//vel = (dir * dist * 8) + random.NextVector2(2.80f),
										fps = random.NextByteRange(4, 12),
										frame_count = 16,
										frame_count_total = 16,
										frame_offset = random.NextByteRange(2, 6),
										//scale = random.NextFloatRange(0.80f, 1.00f),
										//rotation = random.NextFloat(8.00f),
										//angular_velocity = random.NextFloat(0.50f),
										//face_dir_ratio = 1.00f,
										//growth = random.NextFloatRange(0.50f, 1.80f),
										drag = random.NextFloatRange(0.05f, 0.09f),
										stretch = new Vector2(1.00f, 0.50f),
										//color_a = random.NextColor32Range(0xffffffff, 0xff5e9ac3),
										color_a = random.NextColor32Range(0xffffffff, blob_state.tint),
										color_b = new Color32BGRA(0x00ffffff),
										force = new Vector2(0.00f, random.NextFloatExtra(25.00f, 30.00f))
									});
								}
								break;

								case Blob.Data.Type.Melt:
								{
									if (temperature_current >= 900.00f && random.NextBool(0.25f * modifier))
									{
										var intensity_spark = Maths.InvLerp01(750, 1800, temperature_current);

										var pos_spark = pos_prev + random.NextUnitVector2(1);
										var dir_up = new Vector2(0, 1);

										for (var i = 0; i < 2; i++)
										{
											var lit = random.NextBool(0.60f);
											Particle.Spawn(ref region, new Particle.Data()
											{
												texture = Welder.metal_spark_01,
												lifetime = random.NextFloatRange(0.20f, 0.40f),
												pos = pos_spark + random.NextVector2(0.50f),
												vel = (dir_up.RotateByRad(random.NextFloat(1.00f)) * random.NextFloatExtra(0, 4) * intensity_spark) + new Vector2(random.NextFloat(25.00f * intensity), random.NextFloatExtra(10.00f, -5.00f)),
												fps = 8,
												frame_count = 4,
												frame_offset = random.NextByteRange(0, 4),
												frame_count_total = 4,
												scale = random.NextFloatRange(0.70f, 1.10f) * intensity_spark * (lit ? 0.75f : 1.20f) * strength,
												growth = -random.NextFloatRange(0.30f, 0.60f),
												force = new Vector2(random.NextFloatRange(-10, 10), random.NextFloatRange(35, 150)),
												drag = random.NextFloatRange(0.01f, 0.12f),
												stretch = new Vector2(random.NextFloatRange(0.70f, 1.10f), 0.65f),
												color_a = lit ? color_glow_clamped : color_glow,
												color_b = (lit ? color_glow_clamped : color_glow).WithAlpha(0),
												lit = lit ? 1.00f : 0.00f,
												glow = lit ? 0.00f : 4.00f + (intensity_spark * 5.00f),
												face_dir_ratio = 1.00f,
											});
										}
									}

									if (random.NextBool(0.75f))
									{
										Particle.Spawn(ref region, new Particle.Data()
										{
											texture = tex_smoke,
											lifetime = random.NextFloatRange(0.30f, 0.50f),
											pos = pos_prev + random.NextVector2(0.50f) * modifier,
											vel = (dir * Maths.Max(5, dist * 5)), // + random.NextVector2(1.50f), // * modifier.Mulpo(0.15f),
											fps = random.NextByteRange(0, 32),
											frame_count = 64,
											frame_count_total = 64,
											frame_offset = random.NextByteRange(10, 32),
											scale = random.NextFloatExtra(0.70f, 0.50f) * Maths.Max(0.50f, modifier),
											//rotation = random.NextFloat(8.00f),
											// angular_velocity = random.NextFloat(2.00f) * modifier,
											face_dir_ratio = 1.00f,
											growth = random.NextFloatRange(0.10f, 0.30f),
											drag = random.NextFloatRange(0.06f, 0.08f),
											stretch = new Vector2(1.00f, random.NextFloatExtra(0.30f, 0.40f) * modifier),
											color_a = random.NextColorRange(blob_state.tint, color_glow_clamped, 0.20f),
											color_b = blob_state.tint.WithAlpha(0),
											lit = 0.95f,
											force = new Vector2(0, random.NextFloatExtra(10.00f, 30.00f)) // * modifier.Mulpo(0.45f)
										});
									}

									if (true)
									{
										Particle.Spawn(ref region, new Particle.Data()
										{
											texture = Light.tex_light_fire_00,
											lifetime = random.NextFloatRange(0.30f, 0.40f),
											pos = pos_prev + random.NextVector2(0.20f),
											vel = (dir * 5 * dist * random.NextFloatRange(0.75f, 0.90f)) + random.NextVector2(1.10f),
											//fps = random.NextByteRange(4, 12),
											//frame_count = 16,
											//frame_count_total = 16,
											//frame_offset = random.NextByteRange(2, 6),
											scale = random.NextFloatRange(4.95f, 8.90f) * strength.Mulpo(0.50f) * modifier,
											// rotation = dir.GetAngleRadiansFast() + MathF.PI * 0.50f,
											angular_velocity = random.NextFloat(4.00f) * modifier,
											face_dir_ratio = 1.00f,
											growth = random.NextFloatRange(10.50f, 35.80f),
											drag = random.NextFloatRange(0.08f, 0.12f),
											stretch = new Vector2(random.NextFloatExtra(0.35f, 0.30f), random.NextFloatExtra(0.12f, 0.20f) * modifier),
											color_a = random.NextColorRange(color_glow, color_glow_b, 0.25f),
											color_b = color_glow_b,
											force = (new Vector2(0.00f, random.NextFloat01(7.00f)) + (dir * dist * 4 * random.NextFloatRange(0.80f, 1.00f))), // + random.NextUnitVector2(5)) * random.NextFloatExtra(0.90f, 0.10f),
											glow = random.NextFloatExtra(5, 5)
										});
									}
								}
								break;

								case Blob.Data.Type.Powder:
								{
									//if (temperature_current >= 900.00f && random.NextBool(0.25f * modifier))
									//{
									//	var intensity_spark = Maths.InvLerp01(750, 1800, temperature_current);

									//	var pos_spark = pos_prev + random.NextUnitVector2(1);
									//	var dir_up = new Vector2(0, 1);

									//	for (var i = 0; i < 2; i++)
									//	{
									//		var lit = random.NextBool(0.60f);
									//		Particle.Spawn(ref region, new Particle.Data()
									//		{
									//			texture = Welder.metal_spark_01,
									//			lifetime = random.NextFloatRange(0.20f, 0.40f),
									//			pos = pos_spark + random.NextVector2(0.50f),
									//			vel = (dir_up.RotateByRad(random.NextFloat(1.00f)) * random.NextFloatExtra(0, 4) * intensity_spark) + new Vector2(random.NextFloat(25.00f * intensity), random.NextFloatExtra(10.00f, -5.00f)),
									//			fps = 8,
									//			frame_count = 4,
									//			frame_offset = random.NextByteRange(0, 4),
									//			frame_count_total = 4,
									//			scale = random.NextFloatRange(0.70f, 1.10f) * intensity_spark * (lit ? 0.75f : 1.20f) * strength,
									//			growth = -random.NextFloatRange(0.30f, 0.60f),
									//			force = new Vector2(random.NextFloatRange(-10, 10), random.NextFloatRange(35, 150)),
									//			drag = random.NextFloatRange(0.01f, 0.12f),
									//			stretch = new Vector2(random.NextFloatRange(0.70f, 1.10f), 0.65f),
									//			color_a = lit ? color_glow_clamped : color_glow,
									//			color_b = (lit ? color_glow_clamped : color_glow).WithAlpha(0),
									//			lit = lit ? 1.00f : 0.00f,
									//			glow = lit ? 0.00f : 4.00f + (intensity_spark * 5.00f),
									//			face_dir_ratio = 1.00f,
									//		});
									//	}
									//}

									// Particle.Spawn(ref region, new Particle.Data()
									// {
									// 	lifetime = random.NextFloatRange(1.20f, 3.20f),
									// 	pos = pos_prev + random.NextVector2(0.50f),
									// 	vel = (dir * random.NextFloatRange(-4.00f, 8.00f)) + random.NextVector2(6.00f),
									// 	texture = "largesmoke_rough",
									// 	fps = random.NextByteRange(3, 6),
									// 	frame_count = 16,
									// 	frame_count_total = 16,
									// 	frame_offset = random.NextByteRange(1, 2),
									// 	scale = random.NextFloatRange(0.75f, 1.25f),

									// 	//scale = random.NextFloatRange(1.50f, 2.50f),
									// 	force = new Vector2(random.NextFloatRange(-1, 1), random.NextFloatRange(-2, 15)),
									// 	rotation = random.NextFloat(1.00f),
									// 	angular_velocity = random.NextFloat(3.00f),
									// 	face_dir_ratio = random.NextFloatRange(0.20f, 0.50f),
									// 	growth = -random.NextFloatRange(-0.20f, 0.20f),
									// 	drag = random.NextFloatRange(0.08f, 0.11f),
									// 	stretch = new Vector2(1.00f, random.NextFloatExtra(0.35f, 0.45f)),
									// 	color_a = blob_state.tint.WithColorMult(random.NextFloatExtra(0.75f, 0.65f)),
									// 	color_b = blob_state.tint.WithAlpha(0),
									// });

									Particle.Spawn(ref region, new Particle.Data()
									{
										texture = Damage.gravel_gibs_sm_white,
										lifetime = random.NextFloatRange(0.30f, 0.50f),
										pos = pos_prev + random.NextVector2(modifier * 0.70f),
										vel = (dir * random.NextFloatRange(-2.80f, 2.50f)) + random.NextVector2(7.00f), // + new Vector2(0.00f, random.NextFloatExtra(1, 5)),
										fps = 0,
										frame_count = 1,
										frame_offset = random.NextByteRange(0, 16),
										frame_count_total = 16,
										scale = random.NextFloatRange(1.20f, 1.40f),
										rotation = random.NextFloat(MathF.PI),
										angular_velocity = random.NextFloat(MathF.PI * 5.00f),
										growth = -random.NextFloatRange(0.50f, 0.80f),
										color_a = blob_state.tint.WithColorMult(random.NextFloatExtra(0.95f, 0.65f)),
										color_b = blob_state.tint,
										force = new Vector2(0.00f, random.NextFloatRange(25.00f, 30.00f))
									});

									// if (random.NextBool(0.80f))
									{
										Particle.Spawn(ref region, new Particle.Data()
										{
											texture = Damage.bigger_smoke_light,
											lifetime = random.NextFloatExtra(0.70f, 1.10f),
											pos = pos_prev + random.NextVector2(0.30f) * modifier,
											vel = dir * Maths.Max(5, dist * 3) * random.NextFloatExtra(1.00f, 1.50f), // + random.NextVector2(1.50f), // * modifier.Mulpo(0.15f),
											fps = random.NextByteRange(8, 32),
											frame_count = 64,
											frame_count_total = 64,
											frame_offset = random.NextByteRange(10, 32),
											scale = random.NextFloatExtra(0.55f, 0.85f) * Maths.Max(0.50f, modifier),
											// rotation = random.NextFloat(8.00f),
											angular_velocity = random.NextFloat(2.50f) * modifier,
											face_dir_ratio = 0.98f,
											growth = random.NextFloatRange(-0.30f, 0.60f),
											drag = random.NextFloatRange(0.05f, 0.11f),
											stretch = new Vector2(1.00f, random.NextFloatExtra(0.15f, 0.55f)),
											color_a = blob_state.tint.WithColorMult(random.NextFloatExtra(0.75f, 0.65f)).WithAlphaMult(random.NextFloatExtra(0.60f, 0.40f)),
											color_b = blob_state.tint.WithAlpha(0),
											force = new Vector2(random.NextFloat(dist_total) * 2.50f, random.NextFloatExtra(0.00f, 4.00f)) // * modifier.Mulpo(0.45f)
										});
									}
								}
								break;
							}

							if (blob.type == Blob.Data.Type.Melt)
							{
								if (random.NextBool(0.10f))
								{
									Particle.Spawn(ref region, new Particle.Data()
									{
										texture = Heat.texture_smoke,
										lifetime = random.NextFloatExtra(0.50f, 2.50f),
										pos = pos + random.NextUnitVector2(strength) + (dir * random.NextFloatExtra(0, 2)), // + (dir * random.NextFloatRange(0.00f, 1.00f))),
										vel = new Vector2(random.NextFloat(intensity), random.NextFloatRange(-2.55f, 2.00f) * intensity * 3 * strength) + (dir * random.NextFloatExtra(0, 5)), // (-dir * random.NextFloatRange(3.00f, 6.00f) * intensity) + new Vector2(0, -2),
										force = new Vector2(0.10f, random.NextFloatRange(-4.50f, 2.50f)) * smoke_speed_mult,
										fps = random.NextByteRange(2, 8),
										frame_count = 64,
										frame_count_total = 64,
										frame_offset = random.NextByteRange(0, 64),
										scale = random.NextFloatRange(0.20f, 0.50f) * smoke_size_mult * (0.50f + (strength * 0.50f)),
										rotation = random.NextFloat(float.Pi),
										drag = random.NextFloatRange(0.00f, 0.05f),
										angular_velocity = random.NextFloatExtra(0.00f, 1.00f) * smoke_speed_mult,
										growth = random.NextFloatExtra(0.10f, 0.40f),
										color_a = new Color32BGRA(0xffffffff).WithAlphaMult(random.NextFloatExtra(0.50f, 0.20f)),
										color_b = new Color32BGRA(0x00ffffff),
									});
								}
							}

							pos_prev = Maths.MoveTowards(pos_prev, pos, random.NextFloatExtra(0.50f, 1.50f));
						}

						// pos_prev = pos;
					}

					if (blob.type == Data.Type.Powder)
					{
						Particle.Spawn(ref region, new Particle.Data()
						{
							lifetime = random.NextFloatRange(2.20f, 3.20f),
							pos = pos_prev + (dir * 1.50f),
							vel = random.NextVector2(6.00f),
							texture = Damage.large_smoke_rough,
							fps = random.NextByteRange(8, 13),
							frame_count = 16,
							frame_count_total = 16,
							frame_offset = random.NextByteRange(1, 2),
							scale = random.NextFloatRange(1.15f, 1.55f) * modifier * 1.50f,

							//scale = random.NextFloatRange(1.50f, 2.50f),
							force = new Vector2(random.NextFloatRange(-10, 10), 0.00f),
							rotation = random.NextFloat(1.00f),
							angular_velocity = random.NextFloat(3.00f),
							// face_dir_ratio = random.NextFloatRange(0.20f, 0.50f),
							growth = -random.NextFloatRange(-0.10f, 0.10f),
							drag = random.NextFloatRange(0.11f, 0.15f),
							stretch = new Vector2(1.00f, random.NextFloatExtra(0.35f, 0.45f)),
							color_a = blob_state.tint.WithColorMult(random.NextFloatExtra(0.75f, 0.65f)),
							color_b = blob_state.tint,
						});
					}
				}


				//for (var j = 0; j < 6; j++)
				//{
				//	Particle.Spawn(ref region, new Particle.Data()
				//	{
				//		texture = "splash.00",
				//		lifetime = random.NextFloatRange(0.50f, 1.00f),
				//		pos = transform.position + (random.NextVector2(1.20f) * new Vector2(1.00f, 0.10f)),
				//		vel = random.NextVector2(4.80f),
				//		fps = random.NextByteRange(4, 12),
				//		frame_count = 16,
				//		frame_count_total = 16,
				//		frame_offset = random.NextByteRange(2, 6),
				//		scale = random.NextFloatRange(0.90f, 1.50f),
				//		//rotation = random.NextFloat(8.00f),
				//		angular_velocity = random.NextFloat(0.50f),
				//		face_dir_ratio = 0.90f,
				//		growth = random.NextFloatRange(0.50f, 0.80f),
				//		drag = random.NextFloatRange(0.05f, 0.10f),
				//		stretch = new Vector2(1.00f, 0.50f),
				//		color_a = random.NextColor32Range(0xffffffff, 0xff5e9ac3),
				//		color_b = new Color32BGRA(0x00ffffff),
				//		force = new Vector2(0.00f, 10.00f)
				//	});
				//}


				//region.DrawDebugLine(pos_prev, blob_state.pos_source, color: blob.color, thickness: blob.thickness);


				//blob_state.pos_source

				//var ts = Timestamp.Now();
				//blob.t_next_smoke = info.WorldTime + (blob.interval * random.NextFloatRange(0.60f, 1.00f));

				//var pos = transform.GetInterpolatedPosition();
				//if (Camera.IsVisible(Camera.CullType.Rect2x, transform.position))
				//{

				//}
			}
#endif
		}
	}
}