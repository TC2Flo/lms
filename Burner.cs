
namespace TC2.Base.Components
{
	public static partial class Burner
	{
		public static readonly Texture.Handle texture_smoke = "BiggerSmoke_Light";
		public static readonly Texture.Handle texture_light = "light.circle.00";

		public const float update_interval = 0.20f;
		public const float preheating_multiplier = 2.00f;
		public const float heat_loss_multiplier = 2.00f;

		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Region), IComponent.With<Burner.State>]
		public struct Data(): IComponent
		{
			[Flags]
			public enum Flags: uint
			{
				None = 0,

				No_GUI = 1u << 0,

				Open_Air = 1u << 1,
				No_Smoke = 1u << 2,
				Show_Misc = 1u << 3,
			}

			public enum Type: byte
			{
				Undefined = 0,

				Solid,
				Liquid,
				Gas,
			}

			//[Editor.Picker.Position(relative: true)]
			//public Vector2 smoke_offset;

			//[Editor.Picker.Direction(true, true)]
			//public Vector2 smoke_direction = new(0, -1);

			//[Editor.Picker.Direction(true, true)]
			//public Vector2 exhaust_direction = new(0, -1);

			//[Editor.Picker.Direction(true, true)]
			//public Vector2 intake_direction = new(0, -1);

			//[Editor.Picker.Position(relative: true)]
			//public Vector2 exhaust_offset;

			//[Editor.Picker.Position(relative: true)]
			//public Vector2 intake_offset;

			//[Editor.Picker.Position(relative: true, mark_modified: true)] public Vector2 burst_offset;
			//public Sprite sprite_burst = SteamEngine.sprite_burst_default;

			public Burner.Data.Flags flags;
			public Burner.Data.Type type = Burner.Data.Type.Solid;

			//[Obsolete] public Height height = 2.00f.m();

			//public Area area_intake = 0.20f.m2();
			//public Area area_exhaust = 0.20f.m2();
			public Area area_fuel = 0.50f.m2();
			public Area area_aux = Area.Circle(5.00f.cm());
			//public Area area_surface = 2.00f.m2();
			public float fuel_amount_mult = 0.10f;
			//[Obsolete] public Volume volume_chamber = 1.00f.m3();
			public Pressure pressure_max = 8.00f.bar();
			public Temperature temperature_max = 1700.00f.Kelvin();

			//public float specific_heat = 800.00f;
			public float insulation = 0.50f;
			//public float combustion_efficiency = 1.00f;
			//public float heating_efficiency = 1.00f;

			//public float airtightness = 0.10f;
			//public float valve_strength = 3.00f;

			//public float filter_o2 = 1.00f;
			//public float filter_n2 = 1.00f;
			//public float filter_co2 = 1.00f;

			//public float supply_o2 = 0.00f;
			//public float supply_n2 = 0.00f;
			//public float supply_co2 = 0.00f;


			//public float insulation = 0.70f;

			//public float chamber_volume = Units.l(1000.00f);
			//public float wall_thickness = Units.mm(2.00f);

			//public IMaterial.Handle h_material_surface;

			//public float air_intake_area = 0.004f; // Surface area of the air intake hole in m^2

			//[Obsolete] public float _energy_loss;
			//[Obsolete] public float _drain_multiplier = 1.00f;
			//[Obsolete] public float _temperature_multiplier = 1.00f;
			//[Obsolete] public float _loss_multiplier = 1.00f;

			//public float volume_modifier = 0.70f;
			//public float pitch_modifier = 1.00f;
		}

		//public static float GetVolumetricFlowRate(float area, float velocity)
		//{
		//	return area * velocity;
		//}

		//public static float GetMassFlowRate(float area, float velocity, float density)
		//{
		//	return area * velocity * density;
		//}

		//public struct Soot
		//{
		//	public Mass kg_s;
		//	public Mass kg_
		//	public Mass kg_ash;
		//}

		// https://en.wikipedia.org/wiki/Combustion
		// https://en.wikipedia.org/wiki/Energy_conversion_efficiency
		// https://en.wikipedia.org/wiki/Heat_of_combustion
		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public struct State(): IComponent
		{
			[Flags]
			public enum Flags: uint
			{
				None = 0,

				Ignited = 1u << 0,
			}


			public Temperature temperature_fuel_surface = Temperature.Ambient;
			public Temperature temperature_fuel_bulk = Temperature.Ambient;
			//[Obsolete] public Temperature temperature_chamber = Temperature.Ambient;
			//[Obsolete] public Temperature temperature_exhaust = Temperature.Ambient;

	
			public float flame_size;
			public float flame_growth;


			//[Obsolete] public float modifier_intake = 1.00f;
			//[Obsolete] public float modifier_exhaust = 1.00f;
			public float modifier_solid = 0.00f;
			public float modifier_fluid = 0.00f;

	
			public float modifier_intake_target = 1.00f;
			public float modifier_output_target = 1.00f;
			public float modifier_solid_target = 0.00f;
			public float modifier_fluid_target = 0.00f;

			//public Amount moles_o2;
			//public Amount moles_h2;
			//public Amount moles_n2;

			//public Amount moles_co;
			//public Amount moles_co2;
			//public Amount moles_no2;
			//public Amount moles_h2o;

			public Mass mass_char;
			public Mass mass_soot;
			public Mass mass_ash;
			private float unused_00;
			private float unused_01;
			private float unused_02;
			//[Obsolete] public Mass mass_air;

			public Resource.Data fuel_vapor_a;
			public Resource.Data fuel_vapor_b;

			//public Color32BGRA color_smoke;

			public Power power_current;
			public Energy released_energy_current;
			//[Obsolete] public Energy heat_capacity_air;

			//public float intake_velocity;
			//public float exhaust_velocity;

			//public float intake_input; // m3/s
			//public float intake_output; // m3/s

			//public float exhaust_input; // m3/s
			//public float exhaust_output; // m3/s

			//public float exhaust_input; // m3/s
			//public float intake_input; // m3/s

			//public float exhaust_purity; // TODO: remove

			public float o2_ratio;
			public float o2_consumed;

			[Obsolete] public double available_power; // TODO: remove

			public Burner.State.Flags flags;

			//public float fuel_consumed; // TODO: remove
			//public float diffusion = 0.50f;

			//[Obsolete] public Pressure pressure_chamber;

			[Save.Ignore, Net.Ignore] public float t_next_inventory_sync;
			[Save.Ignore, Net.Ignore] public float t_next_sound;
			[Save.Ignore, Net.Ignore] public float t_next_burn;
			[Save.Ignore, Net.Ignore] public float unused_03;
		}

		public static bool IsFuelValid(in Resource.Data resource)
		{
			return resource.IsValid() && resource.material.GetData().flags.HasAll(Material.Flags.Fuel);
		}

		//[ISystem.EarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		//public static void UpdateCrafter(ISystem.Info info, Entity entity,
		//[Source.Owned] ref Crafter.State crafter_state, [Source.Owned] in Burner.Data burner, [Source.Owned] in Burner.State burner_state)
		//{
		//	//crafter_state.temperature = burner_state.temperature_exhaust;
		//}

		//#if CLIENT
		//		[ISystem.Render(ISystem.Mode.Single, ISystem.Scope.Region), HasTag<Burner.Data>("damaged", true, Source.Modifier.Owned)]
		//		public static void RenderBurst([Source.Owned] in Transform.Data transform, [Source.Owned] in Animated.Renderer.Data renderer, [Source.Owned] ref Burner.Data burner)
		//		{
		//			Animated.Renderer.Draw(transform, new()
		//			{
		//				sprite = burner.sprite_burst,
		//				z = renderer.z + 0.01f,
		//				//rotation = -(resizable.flags.HasAny(Resizable.Flags.Orthogonal) ? Maths.Snap(rot, MathF.PI * 0.50f) : rot),
		//				rotation = 0.00f,
		//				scale = Vector2.One,
		//				offset = burner.burst_offset
		//			});
		//		}
		//#endif

#if CLIENT
		public partial struct BurnerGUI: IGUICommand
		{
			public Entity ent_burner;

			public Burner.Data burner;
			public Burner.State burner_state;

			public Air.Container.Data air_container;

			public Air.Vent.Data? vent_intake;
			public Air.Vent.Data? vent_exhaust;

			public Inventory1.Data? inventory_solid;
			public Inventory1.Data? inventory_fluid;

			public Transform.Data transform;

			public static float next_ignite;
			public static float next_sync;

			public void Draw()
			{
				using (var window = GUI.Window.InteractionMisc("Burner"u8, this.ent_burner, size: new(224, 0), show_misc: this.burner.flags.HasAny(Data.Flags.Show_Misc)))
				{
					this.StoreCurrentWindowTypeID(order: 100);
					if (window.show)
					{
						var color_o2 = GUI.col_input;
						var color_water = GUI.col_input.WithColorMult(0.75f);
						var color_h2o = GUI.col_input.WithColorMult(0.75f);
						var color_waste = (Color32BGRA)0xffa080ff;
						var color_carbon = (Color32BGRA)0xffa080ff;
						var color_co2 = GUI.font_color_disabled;
						var color_so2 = GUI.font_color_yellow;
						var color_toxic = GUI.font_color_yellow;
						var color_nox = GUI.font_color_orange;
						var color_fuel = (Color32BGRA)0xffa080ff;
						var color_volatiles = (Color32BGRA)0xffff4080;
						var color_hydro = (Color32BGRA)0xffff4080;
						var color_nitrogen = (Color32BGRA)0xffffffff;
						var color_n2 = (Color32BGRA)0xffffffff;
						var color_inert = GUI.font_color_disabled;

						var total_mass = this.air_container.mass_cached;

						var moles_n2 = this.air_container.air.moles_n2;
						var moles_o2 = this.air_container.air.moles_o2;
						var moles_co2 = this.air_container.air.moles_co2;
						var moles_h2o = this.air_container.air.moles_h2o;
						var moles_so2 = this.air_container.air.moles_so2;
						var moles_h2 = this.air_container.air.moles_h2;
						var moles_co = this.air_container.air.moles_co;
						var moles_volatiles = moles_h2 + moles_co;
						var moles_no2 = this.air_container.air.moles_no2;

						var moles_total = moles_n2 + moles_o2 + moles_co2 + moles_h2o + moles_so2 + moles_volatiles + moles_no2;

						ref var region = ref this.ent_burner.GetRegion();

						GUI.TrySameLine(224);

						using (var group = GUI.Group.New(size: window.docked || true ? new Vector2(120, 24 * 4) : GUI.Rm))
						{
							//group.DrawBackground(GUI.tex_frame);

							//GUI.DrawVerticalGauge(this.burner.)

							var time = App.GetFixedTime();

							var rpc = new Burner.ConfigureRPC();
							var dirty = false;

							//GUI.DrawVerticalGauge(this.burner_state.)

							using (var group_top = GUI.Group.New(size: new(GUI.RmX, 24 * 3)))
							{
								GUI.DrawTemperatureRange(this.air_container.temperature, this.air_container.temperature, this.burner.temperature_max, size: new Vector2(24, GUI.RmY),
									title: "Temperature"u8, label_b: "Burner:"u8, label_a: "Exhaust:"u8);

								GUI.SameLine();

								GUI.DrawPressureRange(this.air_container.pressure_cached, Pressure.bar(8.00f - 1.00f), Pressure.bar(10.00f), size: new Vector2(24, GUI.RmY));

								GUI.SameLine();

								//var h_inventory_solid = this.inventory_solid.GetHandle();
								//var h_inventory_fluid = this.inventory_fluid.GetHandle();

								if (this.inventory_solid.TryGetHandle(out var h_inventory_solid))
								{
									ref var fuel = ref this.inventory_solid.Value.resource.material.GetFuel();
									using (var group_column = GUI.Group.New(size: new(24, GUI.RmY - 24)))
									{
										GUI.DrawInventory(h_inventory_solid);
										if (GUI.SliderFloat("Fuel"u8, in this.burner_state.modifier_solid_target, ref rpc.fuel_modifier, 0.00f, 1.00f, size: new(GUI.RmX, 24), color_frame: 0xffca7f7f, enabled: false))
										{
											dirty = true;
										}
									}

									if (GUI.IsItemHovered())
									{
										using (GUI.Tooltip.New(size: new(244, 0)))
										{
											using (GUI.Wrap.Push(GUI.RmX))
											{
												var w = GUI.RmX;

												//GUI.Title("Fuel");
												//GUI.Separator();

												if (fuel.IsNotNull())
												{
													var pure_mass = fuel.purity;

													using (var group_row = GUI.Group.New(size: new(GUI.RmX, 16)))
													{
														var w_row = GUI.RmX;
														GUI.TextShadedCentered("Purity:"u8, pivot: new(0.00f, 0.50f), offset: new(2, 0));
														GUI.TextShadedCentered($"{fuel.purity:P2}", pivot: new(1.00f, 0.50f), offset: new(-4, 0));
													}

													using (var group_row = GUI.Group.New(size: new(GUI.RmX, 16)))
													{
														var w_row = GUI.RmX;
														GUI.TextShadedCentered("Volatility:"u8, pivot: new(0.00f, 0.50f), offset: new(2, 0));
														GUI.TextShadedCentered($"{fuel.volatility:P2}", pivot: new(1.00f, 0.50f), offset: new(-4, 0));
													}

													GUI.NewLine(4);

													using (var group_row = GUI.Group.New(size: new(GUI.RmX, 16)))
													{
														var w_row = GUI.RmX;
														GUI.TextShadedCentered("Carbon"u8, pivot: new(1.00f, 0.50f), offset: new((48) - w_row, 0), color: 0xffa080ff);
														GUI.TextShadedCentered("Sulfur"u8, pivot: new(1.00f, 0.50f), offset: new((98) - w_row, 0), color: GUI.font_color_yellow);
														GUI.TextShadedCentered("Moisture"u8, pivot: new(1.00f, 0.50f), offset: new((156) - w_row, 0), color: GUI.col_input);
														GUI.TextShadedCentered("Volatiles"u8, pivot: new(1.00f, 0.50f), offset: new((48 * 4.50f) - w_row, 0), color: 0xffff4080);
													}

													GUI.Separator();

													using (var group_row = GUI.Group.New(size: new(GUI.RmX, 16)))
													{
														var w_row = GUI.RmX;
														GUI.TextShadedCentered($"{(fuel.purity * fuel.carbon_ratio):P0}", pivot: new(1.00f, 0.50f), offset: new((48) - w_row, 0));
														GUI.TextShadedCentered($"{(fuel.purity * fuel.sulfur_ratio):P0}", pivot: new(1.00f, 0.50f), offset: new((98) - w_row, 0));
														GUI.TextShadedCentered($"{(fuel.purity * fuel.water_ratio):P0}", pivot: new(1.00f, 0.50f), offset: new((156) - w_row, 0));
														GUI.TextShadedCentered($"~{(fuel.purity * fuel.volatiles_ratio):P0}", pivot: new(1.00f, 0.50f), offset: new((48 * 4.50f) - w_row, 0));
													}
												}
												else
												{
													using (var group_row = GUI.Group.New(size: new(GUI.RmX, 16)))
													{
														GUI.TextShaded("N/A"u8);
													}
												}
											}
										}
									}
								}

								GUI.SameLine();

								if (this.inventory_fluid.TryGetHandle(out var h_inventory_fluid))
								{
									ref var fuel = ref this.inventory_fluid.Value.resource.material.GetFuel();
									using (var group_column = GUI.Group.New(size: new(24, GUI.RmY - 24)))
									{
										GUI.DrawInventory(h_inventory_fluid);
										if (GUI.SliderFloat("Aux"u8, in this.burner_state.modifier_fluid_target, ref rpc.aux_modifier, 0.00f, 1.00f, size: new(GUI.RmX, 24), color_frame: GUI.col_input))
										{
											dirty = true;
										}
									}

									if (GUI.IsItemHovered())
									{
										using (GUI.Tooltip.New(size: new(244, 0)))
										{
											using (GUI.Wrap.Push(GUI.RmX))
											{
												var w = GUI.RmX;

												//GUI.Title("Fuel");
												//GUI.Separator();

												if (fuel.IsNotNull())
												{
													var pure_mass = fuel.purity;

													using (var group_row = GUI.Group.New(size: new(GUI.RmX, 16)))
													{
														var w_row = GUI.RmX;
														GUI.TextShadedCentered("Purity:"u8, pivot: new(0.00f, 0.50f), offset: new(2, 0));
														GUI.TextShadedCentered($"{fuel.purity:P2}", pivot: new(1.00f, 0.50f), offset: new(-4, 0));
													}

													using (var group_row = GUI.Group.New(size: new(GUI.RmX, 16)))
													{
														var w_row = GUI.RmX;
														GUI.TextShadedCentered("Volatility:"u8, pivot: new(0.00f, 0.50f), offset: new(2, 0));
														GUI.TextShadedCentered($"{fuel.volatility:P2}", pivot: new(1.00f, 0.50f), offset: new(-4, 0));
													}

													GUI.NewLine(4);

													using (var group_row = GUI.Group.New(size: new(GUI.RmX, 16)))
													{
														var w_row = GUI.RmX;
														GUI.TextShadedCentered("Carbon"u8, pivot: new(1.00f, 0.50f), offset: new((48) - w_row, 0), color: 0xffa080ff);
														GUI.TextShadedCentered("Sulfur"u8, pivot: new(1.00f, 0.50f), offset: new((98) - w_row, 0), color: GUI.font_color_yellow);
														GUI.TextShadedCentered("Moisture"u8, pivot: new(1.00f, 0.50f), offset: new((156) - w_row, 0), color: GUI.col_input);
														GUI.TextShadedCentered("Volatiles"u8, pivot: new(1.00f, 0.50f), offset: new((48 * 4.50f) - w_row, 0), color: 0xffff4080);
													}

													GUI.Separator();

													using (var group_row = GUI.Group.New(size: new(GUI.RmX, 16)))
													{
														var w_row = GUI.RmX;
														GUI.TextShadedCentered($"{(fuel.purity * fuel.carbon_ratio):P0}", pivot: new(1.00f, 0.50f), offset: new((48) - w_row, 0));
														GUI.TextShadedCentered($"{(fuel.purity * fuel.sulfur_ratio):P0}", pivot: new(1.00f, 0.50f), offset: new((98) - w_row, 0));
														GUI.TextShadedCentered($"{(fuel.purity * fuel.water_ratio):P0}", pivot: new(1.00f, 0.50f), offset: new((156) - w_row, 0));
														GUI.TextShadedCentered($"~{(fuel.purity * fuel.volatiles_ratio):P0}", pivot: new(1.00f, 0.50f), offset: new((48 * 4.50f) - w_row, 0));
													}
												}
												else
												{
													using (var group_row = GUI.Group.New(size: new(GUI.RmX, 16)))
													{
														GUI.TextShaded("N/A"u8);
													}
												}
											}
										}
									}
								}

								GUI.SameLine();

								if (GUI.SliderFloat("Intake"u8, in this.burner_state.modifier_intake_target, ref rpc.intake_modifier, 0.00f, 1.00f, size: new(24, GUI.RmY), vertical: true, color_frame: GUI.col_input)) // , color_frame: 0xffd8a198
								{
									dirty = true;
								}

								GUI.SameLine();

								if (GUI.SliderFloat("Exhaust"u8, in this.burner_state.modifier_output_target, ref rpc.output_modifier, 0.00f, 1.00f, size: new(24, GUI.RmY), vertical: true, color_frame: GUI.col_output)) // , color_frame: 0xffd8a198
								{
									dirty = true;
								}
							}

							using (var group_bottom = GUI.Group.New(size: new(GUI.RmX, GUI.RmY)))
							{
								//if (GUI.SliderFloat("Fuel", in this.burner_state.fuel_modifier_target, ref rpc.fuel_modifier, 0.00f, 1.00f, size: new(GUI.RmX, 24), color_frame: GUI.col_output))
								//{
								//	dirty = true;
								//}
								//GUI.DrawHorizontalGauge(Maths.Normalize01(this.burner_state.air_amount, this.burner.air_capacity), 1.00f, size: new(GUI.RmX, 24), color: GUI.col_input);


								GUI.DrawHorizontalGaugeMulti(
									values: [moles_n2, moles_o2, moles_h2o, moles_co2, moles_volatiles, moles_so2, moles_no2],
									colors: [color_n2, color_o2, color_h2o, color_co2, color_volatiles, color_so2, color_nox],
									max: moles_total, size: new(GUI.RmX - 48, 24));

								GUI.SameLine();

								if (GUI.DrawButton("FIRE"u8, size: new(GUI.RmX, 24), enabled: time >= next_ignite, color: GUI.col_button_error))
								//if (GUI.DrawSmallButton("ignite", size: new(24, 24), enabled: time >= next_ignite, color: GUI.font_color_yellow_b, tex_overlay: GUI.tex_button_00_overlay_warn))
								{
									next_ignite = time + 2.00f;

									var rpc_ignite = new Burner.IgniteRPC();
									rpc_ignite.state = 1.00f;
									rpc_ignite.Send(this.ent_burner);

									//App.WriteLine("released");
								}
								GUI.DrawHoverTooltip("Ignite the burner."u8);

								//GUI.DrawHorizontalGaugeMulti(values: [this.burner_state.debug_air_input, this.burner_state.air_output], colors: [0xffffffff, 0xffffaa00], max: this.burner.air_capacity, size: new(GUI.RmX, 24));
							}

							//if (GUI.IsItemHovered())
							//{
							//	using (GUI.Tooltip.New(size: new(244, 0)))
							//	{
							//		using (GUI.Wrap.Push(GUI.RmX))
							//		{
							//			var w = GUI.RmX;

							//			//GUI.Title("Burner");

							//			//GUI.LabelShaded("Fuel Usage:", $"{((this.burner_state.fuel_consumed * this.burner_state.cached_mass_per_unit) * App.tickrate * 60):0.00} kg/min");
							//			//GUI.LabelShaded("Air:", $"{this.burner_state.air_amount:0.00} m³ ({Maths.KelvinToCelsius(this.burner_state.current_temperature):0} °C)");
							//			//GUI.LabelShaded("Exhaust:", $"{this.burner_state.exhaust_amount:0.00} m³ ({Maths.KelvinToCelsius(this.burner_state.exhaust_temperature):0} °C)");
							//			//GUI.LabelShaded("Exhaust Purity:", $"{this.burner_state.exhaust_purity:P2}");

							//			//GUI.Separator();

							//			//using (var group_row = GUI.Group.New(size: new(GUI.RmX, 16)))
							//			//{
							//			//	var w_row = GUI.RmX;
							//			//	GUI.TextShadedCentered("Oxygen:", pivot: new(1.00f, 0.50f), offset: new((64) - w_row, 0), color: color_o2);
							//			//	GUI.TextShadedCentered($"{moles_o2:0.00}", pivot: new(1.00f, 0.50f), offset: new(104 - w_row, 0));
							//			//}

							//			//using (var group_row = GUI.Group.New(size: new(GUI.RmX, 16)))
							//			//{
							//			//	var w_row = GUI.RmX;
							//			//	GUI.TextShadedCentered("Volatiles:", pivot: new(1.00f, 0.50f), offset: new((64) - w_row, 0), color: color_volatiles);
							//			//	GUI.TextShadedCentered($"{moles_volatiles:0.00}", pivot: new(1.00f, 0.50f), offset: new(104 - w_row, 0));
							//			//}

							//			//using (var group_row = GUI.Group.New(size: new(GUI.RmX, 16)))
							//			//{
							//			//	var w_row = GUI.RmX;
							//			//	GUI.TextShadedCentered("CO2:", pivot: new(1.00f, 0.50f), offset: new((64) - w_row, 0), color: color_co2);
							//			//	GUI.TextShadedCentered($"{moles_co2:0.00}", pivot: new(1.00f, 0.50f), offset: new(104 - w_row, 0));
							//			//}

							//			//using (var group_row = GUI.Group.New(size: new(GUI.RmX, 16)))
							//			//{
							//			//	var w_row = GUI.RmX;
							//			//	GUI.TextShadedCentered("H2O:", pivot: new(1.00f, 0.50f), offset: new((64) - w_row, 0), color: color_h2o);
							//			//	GUI.TextShadedCentered($"{moles_h2o:0.00}", pivot: new(1.00f, 0.50f), offset: new(104 - w_row, 0));
							//			//}

							//			////using (var group_row = GUI.Group.New(size: new(GUI.RmX, 16)))
							//			////{
							//			////	var w_row = GUI.RmX;
							//			////	GUI.TextShadedCentered("Sulfur:", pivot: new(1.00f, 0.50f), offset: new((64) - w_row, 0), color: color_sulfur);
							//			////	GUI.TextShadedCentered($"{burner_state.moles_s:0.00}", pivot: new(1.00f, 0.50f), offset: new(104 - w_row, 0));
							//			////}

							//			//using (var group_row = GUI.Group.New(size: new(GUI.RmX, 16)))
							//			//{
							//			//	var w_row = GUI.RmX;
							//			//	GUI.TextShadedCentered("NOX:", pivot: new(1.00f, 0.50f), offset: new((64) - w_row, 0), color: color_nox);
							//			//	GUI.TextShadedCentered($"{moles_nitro:0.00}", pivot: new(1.00f, 0.50f), offset: new(104 - w_row, 0));
							//			//}

							//			//using (var group_row = GUI.Group.New(size: new(GUI.RmX, 16)))
							//			//{
							//			//	var w_row = GUI.RmX;
							//			//	GUI.TextShadedCentered("CO:", pivot: new(1.00f, 0.50f), offset: new((64) - w_row, 0), color: color_carbon);
							//			//	GUI.TextShadedCentered($"{burner_state.moles_co:0.00}", pivot: new(1.00f, 0.50f), offset: new(104 - w_row, 0));
							//			//}

							//			//using (var group_row = GUI.Group.New(size: new(GUI.RmX, 16)))
							//			//{
							//			//	var w_row = GUI.RmX;
							//			//	GUI.TextShadedCentered("CO2:", pivot: new(1.00f, 0.50f), offset: new((64) - w_row, 0), color: color_inert);
							//			//	GUI.TextShadedCentered($"{burner_state.moles_co2:0.00}", pivot: new(1.00f, 0.50f), offset: new(104 - w_row, 0));
							//			//}

							//			//using (var group_row = GUI.Group.New(size: new(GUI.RmX, 16)))
							//			//{
							//			//	var w_row = GUI.RmX;
							//			//	GUI.TextShadedCentered("NO2:", pivot: new(1.00f, 0.50f), offset: new((64) - w_row, 0), color: color_toxic);
							//			//	GUI.TextShadedCentered($"{burner_state.moles_no2:0.00}", pivot: new(1.00f, 0.50f), offset: new(104 - w_row, 0));
							//			//}

							//			//using (var group_row = GUI.Group.New(size: new(GUI.RmX, 16)))
							//			//{
							//			//	var w_row = GUI.RmX;
							//			//	GUI.TextShadedCentered("H2O:", pivot: new(1.00f, 0.50f), offset: new((64) - w_row, 0), color: color_water);
							//			//	GUI.TextShadedCentered($"{burner_state.moles_h2o:0.00}", pivot: new(1.00f, 0.50f), offset: new(104 - w_row, 0));
							//			//}
							//		}
							//	}
							//}

							if (dirty)
							{
								rpc.Send(this.ent_burner);
							}
						}

						if (GUI.IsItemHovered())
						{
							using (GUI.Tooltip.New(size: new(244, 0)))
							{
								using (GUI.Wrap.Push(GUI.RmX))
								{
									var w = GUI.RmX;

									//GUI.Title("Temperature");
									//GUI.Separator();

									//using (var group_row = GUI.Group.New(size: new(GUI.RmX, 16)))
									//{
									//	var w_row = GUI.RmX;
									//	GUI.TextShadedCentered("Chamber", pivot: new(1.00f, 0.50f), offset: new((48) - w_row, 0));
									//	GUI.TextShadedCentered("Exhaust", pivot: new(1.00f, 0.50f), offset: new((98) - w_row, 0));
									//	GUI.TextShadedCentered("Fuel", pivot: new(1.00f, 0.50f), offset: new((156) - w_row, 0));
									//}

									GUI.Separator();

									using (var group_row = GUI.Group.New(size: new(GUI.RmX, 16)))
									{
										var w_row = GUI.RmX;
										GUI.TextShadedCentered("Nitrogen:"u8, pivot: new(1.00f, 0.50f), offset: new((64) - w_row, 0), color: color_n2);
										GUI.TextShadedCentered($"{moles_n2:0.00}", pivot: new(1.00f, 0.50f), offset: new(104 - w_row, 0));
										GUI.TextShadedCentered($"{(moles_n2 * Phys.n2_molar_mass):0.00} kg", pivot: new(1.00f, 0.50f), offset: new(160 - w_row, 0), color: GUI.font_color_desc);
										GUI.TextShadedCentered($"{Maths.Normalize01((moles_n2 * Phys.n2_molar_mass), total_mass):P2}", pivot: new(1.00f, 0.50f), offset: new(210 - w_row, 0), color: GUI.font_color_desc);
									}

									using (var group_row = GUI.Group.New(size: new(GUI.RmX, 16)))
									{
										var w_row = GUI.RmX;
										GUI.TextShadedCentered("Oxygen:"u8, pivot: new(1.00f, 0.50f), offset: new((64) - w_row, 0), color: color_o2);
										GUI.TextShadedCentered($"{moles_o2:0.00}", pivot: new(1.00f, 0.50f), offset: new(104 - w_row, 0));
										GUI.TextShadedCentered($"{(moles_o2 * Phys.o2_molar_mass):0.00} kg", pivot: new(1.00f, 0.50f), offset: new(160 - w_row, 0), color: GUI.font_color_desc);
										GUI.TextShadedCentered($"{Maths.Normalize01((moles_o2 * Phys.o2_molar_mass), total_mass):P2}", pivot: new(1.00f, 0.50f), offset: new(210 - w_row, 0), color: GUI.font_color_desc);
									}

									using (var group_row = GUI.Group.New(size: new(GUI.RmX, 16)))
									{
										var w_row = GUI.RmX;
										GUI.TextShadedCentered("Volatiles:"u8, pivot: new(1.00f, 0.50f), offset: new((64) - w_row, 0), color: color_volatiles);
										GUI.TextShadedCentered($"{moles_volatiles:0.00}", pivot: new(1.00f, 0.50f), offset: new(104 - w_row, 0));
										GUI.TextShadedCentered($"{(moles_h2 * Phys.h2_molar_mass) + (moles_co * Phys.co_molar_mass):0.00} kg", pivot: new(1.00f, 0.50f), offset: new(160 - w_row, 0), color: GUI.font_color_desc);
										GUI.TextShadedCentered($"{Maths.Normalize01((moles_h2 * Phys.h2_molar_mass) + (moles_co * Phys.co_molar_mass), total_mass):P2}", pivot: new(1.00f, 0.50f), offset: new(210 - w_row, 0), color: GUI.font_color_desc);
									}

									using (var group_row = GUI.Group.New(size: new(GUI.RmX, 16)))
									{
										var w_row = GUI.RmX;
										GUI.TextShadedCentered("CO2:"u8, pivot: new(1.00f, 0.50f), offset: new((64) - w_row, 0), color: color_co2);
										GUI.TextShadedCentered($"{moles_co2:0.00}", pivot: new(1.00f, 0.50f), offset: new(104 - w_row, 0));
										GUI.TextShadedCentered($"{(moles_co2 * Phys.co2_molar_mass):0.00} kg", pivot: new(1.00f, 0.50f), offset: new(160 - w_row, 0), color: GUI.font_color_desc);
										GUI.TextShadedCentered($"{Maths.Normalize01((moles_co2 * Phys.co2_molar_mass), total_mass):P2}", pivot: new(1.00f, 0.50f), offset: new(210 - w_row, 0), color: GUI.font_color_desc);
									}

									using (var group_row = GUI.Group.New(size: new(GUI.RmX, 16)))
									{
										var w_row = GUI.RmX;
										GUI.TextShadedCentered("H2O:"u8, pivot: new(1.00f, 0.50f), offset: new((64) - w_row, 0), color: color_h2o);
										GUI.TextShadedCentered($"{moles_h2o:0.00}", pivot: new(1.00f, 0.50f), offset: new(104 - w_row, 0));
										GUI.TextShadedCentered($"{(moles_h2o * Phys.h2o_molar_mass):0.00} kg", pivot: new(1.00f, 0.50f), offset: new(160 - w_row, 0), color: GUI.font_color_desc);
										GUI.TextShadedCentered($"{Maths.Normalize01((moles_h2o * Phys.h2o_molar_mass), total_mass):P2}", pivot: new(1.00f, 0.50f), offset: new(210 - w_row, 0), color: GUI.font_color_desc);
									}

									using (var group_row = GUI.Group.New(size: new(GUI.RmX, 16)))
									{
										var w_row = GUI.RmX;
										GUI.TextShadedCentered("SO2:"u8, pivot: new(1.00f, 0.50f), offset: new((64) - w_row, 0), color: color_so2);
										GUI.TextShadedCentered($"{moles_so2:0.00}", pivot: new(1.00f, 0.50f), offset: new(104 - w_row, 0));
										GUI.TextShadedCentered($"{(moles_so2 * Phys.so2_molar_mass):0.00} kg", pivot: new(1.00f, 0.50f), offset: new(160 - w_row, 0), color: GUI.font_color_desc);
										GUI.TextShadedCentered($"{Maths.Normalize01((moles_so2 * Phys.so2_molar_mass), total_mass):P2}", pivot: new(1.00f, 0.50f), offset: new(210 - w_row, 0), color: GUI.font_color_desc);
									}

									//using (var group_row = GUI.Group.New(size: new(GUI.RmX, 16)))
									//{
									//	var w_row = GUI.RmX;
									//	GUI.TextShadedCentered("Sulfur:", pivot: new(1.00f, 0.50f), offset: new((64) - w_row, 0), color: color_sulfur);
									//	GUI.TextShadedCentered($"{burner_state.moles_s:0.00}", pivot: new(1.00f, 0.50f), offset: new(104 - w_row, 0));
									//}

									using (var group_row = GUI.Group.New(size: new(GUI.RmX, 16)))
									{
										var w_row = GUI.RmX;
										GUI.TextShadedCentered("NOX:"u8, pivot: new(1.00f, 0.50f), offset: new((64) - w_row, 0), color: color_nox);
										GUI.TextShadedCentered($"{moles_no2:0.00}", pivot: new(1.00f, 0.50f), offset: new(104 - w_row, 0));
										GUI.TextShadedCentered($"{(moles_no2 * Phys.no2_molar_mass):0.00} kg", pivot: new(1.00f, 0.50f), offset: new(160 - w_row, 0), color: GUI.font_color_desc);
										GUI.TextShadedCentered($"{Maths.Normalize01((moles_no2 * Phys.no2_molar_mass), total_mass):P2}", pivot: new(1.00f, 0.50f), offset: new(210 - w_row, 0), color: GUI.font_color_desc);
									}

									GUI.Separator();

									var offset_0 = 108.00f;

									using (var group_row = GUI.Group.New(size: new(GUI.RmX, 16)))
									{
										var w_row = GUI.RmX;
										GUI.TextShadedCentered("Chamber:"u8, pivot: new(0.00f, 0.50f));
										GUI.TextShadedCentered($"{this.air_container.temperature.Celsius():0}°C", pivot: new(1.00f, 0.50f), offset: new(offset_0 - w_row, 0));
									}

									if (this.vent_intake.HasValue)
									{
										using (var group_row = GUI.Group.New(size: new(GUI.RmX, 16)))
										{
											var w_row = GUI.RmX;
											GUI.TextShadedCentered("Intake:"u8, pivot: new(0.00f, 0.50f));
											GUI.TextShadedCentered($"{this.vent_intake.Value.temperature_outside.Celsius():0}°C", pivot: new(1.00f, 0.50f), offset: new(offset_0 - w_row, 0));
										}
									}

									if (this.vent_exhaust.HasValue)
									{
										using (var group_row = GUI.Group.New(size: new(GUI.RmX, 16)))
										{
											var w_row = GUI.RmX;
											GUI.TextShadedCentered("Exhaust:"u8, pivot: new(0.00f, 0.50f));
											GUI.TextShadedCentered($"{this.vent_exhaust.Value.temperature_inside.Celsius():0}°C", pivot: new(1.00f, 0.50f), offset: new(offset_0 - w_row, 0));
										}
									}

									if (this.inventory_solid.HasValue)
									{
										using (var group_row = GUI.Group.New(size: new(GUI.RmX, 16)))
										{
											var w_row = GUI.RmX;
											GUI.TextShadedCentered("Fuel:"u8, pivot: new(0.00f, 0.50f));
											GUI.TextShadedCentered($"{this.burner_state.temperature_fuel_surface.Celsius():0}°C", pivot: new(1.00f, 0.50f), offset: new(offset_0 - w_row, 0));
											GUI.TextShadedCentered($"{this.burner_state.temperature_fuel_bulk.Celsius():0}°C", pivot: new(1.00f, 0.50f), offset: new(-56, 0));
										}
									}

									using (var group_row = GUI.Group.New(size: new(GUI.RmX, 16)))
									{
										var w_row = GUI.RmX;
										GUI.TextShadedCentered("Power:"u8, pivot: new(0.00f, 0.50f));
										GUI.TextShadedCentered($"{this.burner_state.power_current:0} kW", pivot: new(1.00f, 0.50f), offset: new(offset_0 - w_row, 0));
									}

									using (var group_row = GUI.Group.New(size: new(GUI.RmX, 16)))
									{
										var w_row = GUI.RmX;
										GUI.TextShadedCentered("Air Density:"u8, pivot: new(0.00f, 0.50f));
										GUI.TextShadedCentered($"{this.air_container.density_cached:0.000}", pivot: new(1.00f, 0.50f), offset: new(offset_0 - w_row, 0));
									}

									//GUI.Title("Burner");
									//GUI.Separator();

									//GUI.LabelShaded("Fuel Usage:", $"{((this.burner_state.fuel_consumed * this.burner_state.cached_mass_per_unit) * App.tickrate * 60):0.00} kg/min");
									//GUI.LabelShaded("Air:", $"{this.burner_state.air_amount:0.00} m³ ({Maths.KelvinToCelsius(this.burner_state.current_temperature):0} °C)");
									//GUI.LabelShaded("Exhaust:", $"{this.burner_state.exhaust_amount:0.00} m³ ({Maths.KelvinToCelsius(this.burner_state.exhaust_temperature):0} °C)");
									//GUI.LabelShaded("Flow:", $"{this.burner_state.exhaust_flow_volume:0.00} m³/s ({this.burner_state.exhaust_flow_velocity:0.00} m/s)");

									//GUI.TextShaded("Air");

									//GUI.LabelShaded("Air:", $"{this.burner_state.air_amount:0.00} m³ ({Maths.KelvinToCelsius(this.burner_state.current_temperature):0} °C)");
									//GUI.LabelShaded("Flue:", $"{this.burner_state.exhaust_amount:0.00} m³ ({Maths.KelvinToCelsius(this.burner_state.exhaust_temperature):0} °C)");
									//GUI.LabelShaded("Flow:", $"{this.burner_state.exhaust_flow_volume:0.00} m³/s ({this.burner_state.exhaust_flow_velocity:0.00} m/s)");


									//using (var group_row = GUI.Group.New(size: new(GUI.RmX, 16)))
									//{
									//	GUI.TextShadedCentered("Temperature:", pivot: new(0.00f, 0.50f));
									//	GUI.TextShadedCentered($"{this.burner_state.air_amount:0.00} m³", pivot: new(0.00f, 0.50f), offset: new(64, 0), color: Color32BGRA.FromHSV(2.00f * Maths.Normalize01(this.burner_state.air_amount, this.burner.volume_chamber), 1.00f, 1.00f));
									//	GUI.TextShadedCentered($"{this.burner_state.exhaust_output:0.00} m³/s", pivot: new(0.00f, 0.50f), offset: new(128, 0));
									//}
									//using (var group_row = GUI.Group.New(size: new(GUI.RmX, 16)))
									//{
									//	GUI.TextShadedCentered("Flue:", pivot: new(0.00f, 0.50f));
									//	GUI.TextShadedCentered($"{this.burner_state.exhaust_amount:0.00} m³", pivot: new(0.00f, 0.50f), offset: new(64, 0), color: Color32BGRA.FromHSV(2.00f * Maths.Normalize01(this.burner_state.exhaust_amount, this.burner.volume_chamber), 1.00f, 1.00f));
									//}
									////GUI.LabelShaded("Air:", $"{this.burner_state.debug_air_input:0.00} m³/s", color_b: this.burner_state.debug_air_input.IsPositive() ? Color32BGRA.Green : Color32BGRA.Red);
									////GUI.LabelShaded("Exhaust:", $"{this.burner_state.exhaust_output:0.00} m³/s", color_b: this.burner_state.exhaust_output.IsPositive() ? Color32BGRA.Green : Color32BGRA.Red);
									////GUI.LabelShaded("Flow:", $"{this.burner_state.exhaust_flow_volume:0.00} m³/s ({this.burner_state.exhaust_flow_velocity:0.00} m/s)");

									////var fuel_burnt = (this.burner.fuel_capacity - this.burner_state.fuel_quantity);

									//GUI.NewLine();

									//GUI.LabelShaded("Output:", $"{this.burner_state.exhaust_output:0.00} kg");
									//GUI.LabelShaded("Power:", $"{((this.burner_state.available_power * 0.001)):0.00} kW");
									////GUI.LabelShaded("Energy:", $"{(this.burner_state.fuel_burnt_energy * 0.001 * 0.001):0.00}/{(this.burner_state.cached_fuel_energy * 0.001 * 0.001):0.00} MJ");
									////GUI.LabelShaded("Fuel:", $"{fuel_burnt:0.000} ({(fuel_burnt * this.burner_state.cached_mass_per_unit * this.burner_state.cached_fuel_energy * 0.001 * 0.001):0.00} MJ)");

								}
							}
						}

					}
				}
			}
		}

		[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region, order: 30)]
		public static void OnGUI(Entity entity, [Source.Owned] in Transform.Data transform, [Source.Owned] in Interactable.Data interactable,
		[Source.Owned] in Burner.Data burner, [Source.Owned] in Burner.State burner_state, [Source.Owned] in Air.Container.Data air_container,

		[Source.Owned, Pair.Tag("intake"), Optional(true)] ref Air.Vent.Data vent_intake,
		[Source.Owned, Pair.Tag("exhaust"), Optional(true)] ref Air.Vent.Data vent_exhaust,

		[Source.Owned, Pair.Component<Burner.Data>, Optional(true)] ref Inventory1.Data inventory_solid,
		[Source.Owned, Pair.Component<Burner.State>, Optional(true)] ref Inventory1.Data inventory_fluid)
		{
			if (interactable.IsActive() && burner.flags.HasNone(Burner.Data.Flags.No_GUI))
			{
				var gui = new BurnerGUI()
				{
					ent_burner = entity,

					burner = burner,
					burner_state = burner_state,

					air_container = air_container,

					transform = transform,
				};
				if (inventory_solid.IsNotNull()) gui.inventory_solid = inventory_solid;
				if (inventory_fluid.IsNotNull()) gui.inventory_fluid = inventory_fluid;
				if (vent_intake.IsNotNull()) gui.vent_intake = vent_intake;
				if (vent_exhaust.IsNotNull()) gui.vent_exhaust = vent_exhaust;

				gui.Submit();
			}
		}
#endif

		//#if SERVER
		//		[ISystem.VeryEarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region, interval: Burner.update_interval), HasTag("lit", true, Source.Modifier.Owned)]
		//		public static void UpdateFuel(ISystem.Info info, Entity entity,
		//		[Source.Owned] in Burner.Data burner, [Source.Owned] ref Burner.State burner_state,
		//		[Pair.Component<Burner.Data>] ref Inventory1.Data inventory)
		//		{
		//			if (burner_state.resource.quantity < burner.capacity * 0.95f)
		//			{
		//				ref var resource = ref inventory[0];
		//				if (IsFuelValid(in resource))
		//				{
		//					var dirty = false;

		//					if (burner_state.cached_fuel_material.id != cached_fuel_material.id)
		//					{
		//						burner_state.cached_fuel_material = cached_fuel_material;
		//						burner_state.resource.quantity = 0.00f;
		//						dirty = true;
		//					}

		//					var required_amount = burner.capacity - burner_state.resource.quantity;
		//					if (Resource.Withdraw(ref inventory, ref burner_state.resource, ref required_amount))
		//					{
		//						dirty = true;
		//					}

		//					if (dirty)
		//					{
		//						burner_state.Sync(entity, true);
		//					}
		//				}
		//			}
		//		}
		//#endif

		public struct ConfigureRPC: Net.IRPC<Burner.State>
		{
			public float? fuel_modifier;
			public float? aux_modifier;
			public float? intake_modifier;
			public float? output_modifier;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Burner.State data)
			{
				var sync = false;

				if (data.modifier_solid_target.TrySet(this.fuel_modifier, 0.00f, 1.00f))
				{
					sync = true;
				}

				if (data.modifier_fluid_target.TrySet(this.aux_modifier, 0.00f, 1.00f))
				{
					sync = true;
				}

				if (data.modifier_output_target.TrySet(this.output_modifier, 0.00f, 1.00f))
				{
					sync = true;
				}

				if (data.modifier_intake_target.TrySet(this.intake_modifier, 0.00f, 1.00f))
				{
					sync = true;
				}

				if (sync)
				{
					data.Sync(rpc.entity, true);
				}
			}
#endif
		}

		[Shitcode]
		public struct IgniteRPC: Net.IRPC<Burner.State>
		{
			public float state;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Burner.State data)
			{
				ref var region = ref rpc.entity.GetRegion();
				if (region.IsNotNull())
				{
					ref var transform = ref rpc.entity.GetComponent<Transform.Data>();
					if (transform.IsNotNull())
					{
						// TODO: cache the sound handle
						Sound.Play(ref region, "burner.igniter.00", transform.position, volume: 1.00f, size: 0.20f, dist_multiplier: 0.60f);
					}

					//ref var material = ref data.cached_fuel_material.GetData();
					//if (material.IsNotNull() && data.resource.quantity > 0.10f && material.flags.HasAll(Material.Flags.Fuel) && material.fuel.HasValue)
					//{
					//	ref var fuel = ref material.fuel.GetRefOrNull();
					//	if (fuel.IsNotNull())
					//	{
					//		var random = XorRandom.New(true);

					//		//data.current_temperature = Maths.Max(data.current_temperature, random.NextFloatRange(data.current_temperature fuel.ignition_temperature + random.NextFloatRange(5.00f, 20.00f));

					//		data.burn_rate_target += random.NextFloatRange(0.10f, 0.75f) * fuel.flammability;

					//		//var delta = Maths.Max(0.00f, ((fuel.ignition_temperature * 1.50f) - data.current_temperature));
					//		//data.current_temperature += random.NextFloatRange((delta * 0.10f) + 10.00f, (delta * 0.50f) + 30.00f);
					//	}
					//}

					//var random = XorRandom.New(true);
					//data.temperature_chamber.m_value.MoveTowards(1000.00f, random.NextFloatRange(25.00f, 70.00f));
					//data.temperature_fuel_surface.m_value.MoveTowards(1000.00f, random.NextFloatRange(35.00f, 65.00f));
					//data.temperature_fuel_bulk.m_value.MoveTowards(1000.00f, random.NextFloatRange(1.00f, 9.00f));

					data.flame_growth += region.GetRandom().NextFloatRange(0.033f, 0.093f);
					//data.flame_temperature.m_value.AddTowards(800, random.NextFloatRange(240.00f, 350.00f));

					//data.burn_rate_target += random.NextFloatRange(0.10f, 0.75f) * data.cached_flammability;
					//data.burn_rate += random.NextFloatRange(0.02f, 0.05f);
					//data.burn_rate_target += random.NextFloatRange(0.02f, 0.05f);
					//data.burn_rate_current += random.NextFloatRange(0.02f, 0.05f);

					data.Sync(rpc.entity, true);
				}
			}
#endif
		}

		//[ISystem.EarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region, order: -100)]
		//public static void UpdateVent2(ISystem.Info info, ref Region.Data region, Entity entity, ref XorRandom random,
		//[Source.Owned] in Transform.Data transform, [Source.Owned] ref Air.Container.Data air_container,
		//[Source.Owned, Pair.Tag("intake")] ref Vent.Data vent_intake,
		//[Source.Owned, Pair.Tag("exhaust")] ref Vent.Data vent_exhaust)
		//{
		//	var air = air_container.air;

		//	var moles_total = air.GetTotalMoles();
		//}

		// https://en.wikipedia.org/wiki/Exhaust_gas
		// https://en.wikipedia.org/wiki/Combustion#Solid_fuels
		// https://en.wikipedia.org/wiki/Heat_release_parameter
		// https://en.wikipedia.org/wiki/Category:Combustion
		[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region, order: -100)]
		public static void Update(ISystem.Info info, ref Region.Data region, Entity entity, ref XorRandom random,
		[Source.Owned] in Transform.Data transform, [Source.Owned] in Body.Data body,
		[Source.Owned] in Burner.Data burner, [Source.Owned] ref Burner.State burner_state, [Source.Owned] ref Air.Container.Data air_container,

		[Source.Owned, Pair.Tag("intake"), Optional(true)] ref Air.Vent.Data vent_intake,
		[Source.Owned, Pair.Tag("exhaust"), Optional(true)] ref Air.Vent.Data vent_exhaust,

		[Source.Owned, Pair.Component<Burner.Data>, Optional(true)] ref Inventory1.Data inventory_solid,
		[Source.Owned, Pair.Component<Burner.State>, Optional(true)] ref Inventory1.Data inventory_fluid)
		{
			//return;

			if (Air.Container.Data.dev_is_debug)
			{
				if (Air.Container.Data.time_step > 0 && (region.GetCurrentTick() % Air.Container.Data.time_step) != 0) return;
			}

			var dt = App.fixed_update_interval_s;
			const bool show_debug = false;
			const bool debug_closed = false;

			var ts = Timestamp.Now();

			//var temperature_ambient = Temperature.Ambient;
			//var pressure_ambient = Phys.atmospheric_pressure_kordel;

			var pyrolysis_start = 110.00f.Celsius();
			var pyrolysis_peak = 250.00f.Celsius();
			//var pyrolysis_peak2 = 308.00f.Celsius();
			//var pyrolysis_end = 500.00f.Celsius();
			//var oxidation_start = 110.00f.Celsius();
			//var oxidation_peak = 900.00f.Celsius();
			//var combustion_start = 264.00f.Celsius();

			//var pressure = (Pressure)Maths.Max(0.01f, burner_state.pressure_chamber);

			var pressure = air_container.pressure_cached;

			//var air_velocity_ambient = 0.80f * Maths.Avg(burner_state.modifier_intake, burner_state.modifier_exhaust);
			//if (pressure < pressure_ambient) air_velocity_ambient = Maths.Max(air_velocity_ambient, Maths.Min(20.00f, (1.00f / Maths.Normalize01(pressure, pressure_ambient).Pow2())));

			//var area_intake = 1.00f;
			//var area_exhaust = 1.00f;

			var vel_intake = 0.00f;
			var vel_exhaust = 0.00f; // air_velocity_ambient;

			//var volume_displaced = 0.00f;

			if (vent_intake.IsNotNull())
			{
				//area_intake = vent_intake.cross_section * burner_state.modifier_intake;
				vel_intake = vent_intake.velocity_old.Abs();
				//volume_displaced -= Maths.Min(0.00f, vent_intake.flow_rate);
			}

			if (vent_exhaust.IsNotNull())
			{
				//area_exhaust = vent_exhaust.cross_section * burner_state.modifier_exhaust;
				vel_exhaust = vent_exhaust.velocity_old.Abs(); //.GetFlowVelocity();
															   //volume_displaced -= Maths.Min(0.00f, vent_exhaust.flow_rate);
			}

			//volume_displaced *= dt;

			var air_velocity_avg = Maths.Avg(vel_intake, vel_exhaust);
			var heat_transfer_speed = 3.00f;

			var heat_capacity_air = Maths.Max(1.00f, air_container.air.GetHeatCapacity());
			//App.WriteLine(heat_capacity_air);

			var air_mass = air_container.mass_cached;

			var temperature_fuel_inside = burner_state.temperature_fuel_bulk;
			var temperature_fuel_surface = burner_state.temperature_fuel_surface;
			var temperature_chamber = air_container.temperature; // burner_state.temperature_chamber;
																 //var temperature_exhaust = burner_state.temperature_exhaust;

			var evaporation_modifier = Maths.InvLerp01(pyrolysis_start, pyrolysis_peak, temperature_fuel_surface);
			//var solid_oxidation_modifier = Maths.InvLerp01(oxidation_start, oxidation_peak, temperature_fuel_surface);
			//var vapor_oxidation_modifier = Maths.InvLerp01(oxidation_start, oxidation_peak, temperature_chamber);

			//var reaction_energy_2c_o2_to_2co = -(Energy)(((Phys.co_enthalpy * 2)) - ((Phys.c_enthalpy * 2) + (Phys.o2_enthalpy * 1)));
			//var mult_2c_o2_to_2co = Maths.InvMidBias(380.00f.Celsius(), 900.00f.Celsius(), 3500.00f.Celsius(), temperature_chamber);

			var reaction_energy_2co_o2_to_2co2 = -(Energy)(((Phys.co2_enthalpy * 2)) - ((Phys.co_enthalpy * 2) + (Phys.o2_enthalpy * 1)));
			var mult_2co_o2_to_2co2 = Maths.InvMidBias(110.00f.Celsius(), 500.00f.Celsius(), 2220.00f.Celsius(), temperature_chamber);

			var reaction_energy_2h2_o2_to_2h2o = -(Energy)(((Phys.h2o_enthalpy * 2)) - ((Phys.h2_enthalpy * 2) + (Phys.o2_enthalpy * 1)));
			var mult_h2o = Maths.InvLerp01(110.00f.Celsius(), 500.00f.Celsius(), temperature_chamber);

			var reaction_energy_no2 = -(Energy)(((Phys.no2_enthalpy * 2)) - ((Phys.n2_enthalpy * 1) + (Phys.o2_enthalpy * 2)));
			var mult_no2 = Maths.InvLerp01(850.00f.Celsius(), 3000.00f.Celsius(), temperature_chamber).Pow2();

			var mult_char = Maths.InvMidBias(150.00f.Celsius(), 600.00f.Celsius(), 4000.00f.Celsius(), temperature_chamber);

			var vapor_a_loss_mult = 1.00f;
			var vapor_b_loss_mult = 1.00f;

			scoped ref var material_solid = ref Unsafe.NullRef<IMaterial.Data>();
			scoped ref var material_fluid = ref Unsafe.NullRef<IMaterial.Data>();
			scoped ref var fuel_solid = ref Unsafe.NullRef<IMaterial.Fuel>();

			if (inventory_solid.IsNotNull()) material_solid = ref inventory_solid.resource.GetMaterial();
			if (inventory_fluid.IsNotNull()) material_fluid = ref inventory_fluid.resource.GetMaterial();

			if (material_solid.IsNotNull())
			{
				fuel_solid = ref material_solid.fuel.GetRefOrNull();
				if (fuel_solid.IsNotNull() && fuel_solid.volatiles_ratio > Maths.epsilon && burner_state.fuel_vapor_a == 0)
				{
					var volatiles_span = fuel_solid.volatiles.AsSpan();
					if (volatiles_span.Length > 0)
					{
						var h_volatiles = volatiles_span[0];
						burner_state.fuel_vapor_a = h_volatiles;
					}
				}
			}

			if (material_fluid.IsNotNull())
			{
				burner_state.fuel_vapor_b.header = inventory_fluid.resource.header;
			}

			scoped ref var material_a = ref burner_state.fuel_vapor_a.material.GetData();
			scoped ref var material_b = ref burner_state.fuel_vapor_b.material.GetData();

			scoped ref var fuel_a = ref burner_state.fuel_vapor_a.material.GetFuel();
			scoped ref var fuel_b = ref burner_state.fuel_vapor_b.material.GetFuel();

			var fuel_type_a = IMaterial.Fuel.Type.Undefined;
			var fuel_type_b = IMaterial.Fuel.Type.Undefined;

			if (fuel_a.IsNotNull())
			{
				fuel_type_a = fuel_a.type;
				vapor_a_loss_mult = fuel_a.decay_rate * air_velocity_avg;
			}

			if (fuel_b.IsNotNull())
			{
				fuel_type_b = fuel_b.type;
				vapor_b_loss_mult = fuel_b.decay_rate * air_velocity_avg;
			}

			var mass_fuel_surface = 0.00f;
			var mass_fuel_inside = 0.00f;

			var air = air_container.air;

			ref var moles_o2 = ref air.moles_o2;
			ref var moles_h2 = ref air.moles_h2;
			ref var moles_n2 = ref air.moles_n2;
			ref var moles_co = ref air.moles_co;
			ref var moles_co2 = ref air.moles_co2;
			ref var moles_so2 = ref air.moles_so2;
			ref var moles_no2 = ref air.moles_no2;
			ref var moles_h2o = ref air.moles_h2o;

			var mass_char = burner_state.mass_char;
			var mass_ash = burner_state.mass_ash;
			var mass_soot = burner_state.mass_soot;

			var moles_total = air_container.moles_total_cached;

			//var moles_total =
			//moles_n2 +
			//moles_o2 +
			//moles_h2 +
			//moles_co +
			//moles_co2 +
			//moles_no2 +
			//moles_h2o;

			static void Combustion(ref readonly IMaterial.Fuel fuel, ref Mass mass_fuel, ref Amount moles_o2, ref Amount moles_co2, ref Amount moles_h2o, ref Amount moles_n2, ref Amount moles_h2, out Energy energy, out float o2_avail_ratio, float combustion_rate, ref Amount moles_o2_consumed)
			{
				var x = fuel.carbon_ratio;
				var y = fuel.hydrogen_ratio;
				var z = x + (y * 0.25f);

				var c_kg = x * Phys.c_atomic_mass;
				var h_kg = y * Phys.h_atomic_mass;

				var ch_kg = c_kg + h_kg;

				var ch_ratio = (x + y);
				var fuel_ratio = ch_ratio * fuel.purity;

				var mass_fuel_pure = mass_fuel.m_value * fuel_ratio;
				var mass_ratio = Maths.Normalize(mass_fuel_pure, ch_kg);

				//App.WriteLine($"{ch_kg}; {mass_fuel_pure}; {mass_ratio}");

				c_kg *= mass_ratio;
				h_kg *= mass_ratio;
				ch_kg *= mass_ratio;


				var o2_kg = Amount.kg((Amount)Maths.Min(moles_o2, combustion_rate), Phys.o2_molar_mass);
				var o2_kg_req = z * mass_ratio * Phys.o2_molar_mass;

				var o2_ratio = Maths.Normalize01(o2_kg, o2_kg_req);

				var o2_consumed = (o2_kg_req * Phys.co2_molar_mass_inv) * o2_ratio;
				var co2_added = x * mass_ratio * o2_ratio;
				var h2o_added = (y * 0.50f) * mass_ratio * o2_ratio;

				moles_o2 += fuel.oxygen_ratio * mass_ratio * 0.50f * o2_ratio; // 
				moles_n2 += fuel.nitrogen_ratio * mass_ratio * 0.50f * o2_ratio; // 
				var fuel_kg_consumed = mass_fuel.m_value * o2_ratio;

				energy = fuel.energy_combustion * mass_fuel.m_value * o2_ratio;

				//#if CLIENT
				//				if (App.CurrentTick % 150 == 0)
				//				{
				//					App.WriteLine($"\nc_kg: {c_kg:0.000000}\nh_kg: {h_kg:0.000000}\nch_kg: {ch_kg:0.000000}\n" +
				//						$" co2: {co2_added:0.00000} + h2o: {h2o_added:0.00000} = {(c_kg + h_kg + o2_kg_req):0.00000} = {(co2_kg_added + h2o_kg_added):0.00000}" +
				//						$" o2_kg_req: {o2_kg:0.00000}/{o2_kg_req:0.00000}; mass_ratio: {mass_ratio:0.0000}; o2_ratio: {o2_ratio:0.0000};" +
				//						$" sum: {c_kg + h_kg:0.00000} == {mass_fuel:0.00000}");
				//				}
				//#endif


				moles_co2 += co2_added;
				moles_h2o += h2o_added;
				moles_o2 -= o2_consumed;
				mass_fuel -= fuel_kg_consumed;

				moles_o2_consumed += o2_consumed;
				o2_avail_ratio = o2_ratio;
			}

			//static void CombustionChar(ref Mass mass_char, ref Amount moles_o2, ref Amount moles_co2, out Energy energy, out float o2_avail_ratio, float combustion_rate, ref Amount moles_o2_consumed)
			//{
			//	var o2_kg = Amount.kg((Amount)Maths.Min(moles_o2, combustion_rate), Phys.o2_molar_mass);
			//	var o2_kg_req = z * mass_ratio * Phys.o2_molar_mass;

			//	var o2_ratio = Maths.Normalize01(o2_kg, o2_kg_req);

			//	var o2_consumed = (o2_kg_req * Phys.co2_molar_mass_inv) * o2_ratio;
			//	var co2_added = x * mass_ratio * o2_ratio;
			//	var h2o_added = (y * 0.50f) * mass_ratio * o2_ratio;

			//	moles_o2 += fuel.oxygen_ratio * mass_ratio * 0.50f * o2_ratio; // 
			//	moles_n2 += fuel.nitrogen_ratio * mass_ratio * 0.50f * o2_ratio; // 
			//	var fuel_kg_consumed = mass_fuel.m_value * o2_ratio;

			//	energy = fuel.energy_combustion * mass_fuel.m_value * o2_ratio;

			//	//#if CLIENT
			//	//				if (App.CurrentTick % 150 == 0)
			//	//				{
			//	//					App.WriteLine($"\nc_kg: {c_kg:0.000000}\nh_kg: {h_kg:0.000000}\nch_kg: {ch_kg:0.000000}\n" +
			//	//						$" co2: {co2_added:0.00000} + h2o: {h2o_added:0.00000} = {(c_kg + h_kg + o2_kg_req):0.00000} = {(co2_kg_added + h2o_kg_added):0.00000}" +
			//	//						$" o2_kg_req: {o2_kg:0.00000}/{o2_kg_req:0.00000}; mass_ratio: {mass_ratio:0.0000}; o2_ratio: {o2_ratio:0.0000};" +
			//	//						$" sum: {c_kg + h_kg:0.00000} == {mass_fuel:0.00000}");
			//	//				}
			//	//#endif


			//	moles_co2 += co2_added;
			//	moles_h2o += h2o_added;
			//	moles_o2 -= o2_consumed;
			//	mass_fuel -= fuel_kg_consumed;

			//	moles_o2_consumed += o2_consumed;
			//	o2_avail_ratio = o2_ratio;
			//}

			if (fuel_solid.IsNotNull())
			{
				//fuel_mass_evaporated_raw = Maths.Min(mass_fuel_total, evaporation_rate) * dt;
				//fuel_mass_evaporated = fuel_mass_evaporated_raw;
				//fuel_specific_heat = fuel.specific_heat;
			}
			else
			{
				temperature_fuel_inside = Phys.ambient_temperature; // temperature_ambient;
				temperature_fuel_surface = Phys.ambient_temperature;
			}

			var reaction_energy_released = 0.00f;
			var combustion_energy_released = 0.00f;
			var combustion_energy_absorbed = 0.00f;

			var fuel_kg_consumed = 0.00f;
			var o2_consumed = (Amount)0.00f;

			var flammability = 0.00f;

			var reaction_rate = Maths.Lerp(0.10f, 1.00f, Maths.InvLerp01(500, 2500, temperature_chamber).Pow2());

			#region Reactions
			if (mult_no2 > 0.01f)
			{
				var moles_potential = moles_o2 * Maths.Min(moles_o2, moles_n2);
				var moles_produced = moles_potential * mult_no2 * dt * reaction_rate;

				moles_no2 += moles_produced;
				moles_o2 -= moles_produced;
				moles_n2 -= moles_produced;

				reaction_energy_released += reaction_energy_no2 * moles_produced;
			}

			//mult_2co_o2_to_2co2 *= 1.00f - carbon_oxygen_ratio;
			if (mult_2co_o2_to_2co2 > 0.01f)
			{
				var moles_potential = moles_o2 * Maths.Min(moles_o2, moles_co) * 0.50f;
				var moles_produced = moles_potential * mult_2co_o2_to_2co2 * dt * reaction_rate;

				moles_co2 += moles_produced;
				moles_co -= moles_produced * 2.00f;
				moles_o2 -= moles_produced;

				reaction_energy_released += reaction_energy_2co_o2_to_2co2 * moles_produced;
			}

			if (mult_h2o > 0.01f)
			{
				var moles_potential = moles_o2 * Maths.Min(moles_o2, moles_h2) * 0.50f;
				var moles_produced = moles_potential * mult_h2o * dt * reaction_rate;

				moles_h2o += moles_produced;
				moles_o2 -= moles_produced;
				moles_h2 -= moles_produced * 2.00f;

				reaction_energy_released += reaction_energy_2h2_o2_to_2h2o * moles_produced;
			}
			#endregion

			if (fuel_solid.IsNotNull())
			{
				var mass_fuel_total = Maths.Min(inventory_solid.resource.quantity, inventory_solid.stack_size_multiplier * material_solid.quantity_max * burner.fuel_amount_mult) * material_solid.mass_per_unit;
				//App.WriteLine(mass_fuel_total);

				if (mass_fuel_total > 0.11f)
				{
					var area_fuel = burner.area_fuel * fuel_solid.surface_area_mult;

					mass_fuel_surface = Maths.Min(4.00f * material_solid.mass_per_unit * area_fuel * fuel_solid.purity * (1.00f + fuel_solid.porosity), mass_fuel_total * 0.50f);
					mass_fuel_inside = mass_fuel_total - mass_fuel_surface;

					evaporation_modifier *= fuel_solid.volatility;

					var fuel_specific_heat = fuel_solid.specific_heat;

					var solid_fire_ratio = Maths.InvLerp01(fuel_solid.flash_point, fuel_solid.flash_point + Maths.Avg(fuel_solid.autoignition_temperature, fuel_solid.flame_temperature_o2), Maths.Lerp(Maths.Avg(temperature_fuel_inside, temperature_fuel_surface), temperature_fuel_surface, burner_state.o2_ratio));
					var evaporation_rate = area_fuel * evaporation_modifier * Maths.Mulpo(solid_fire_ratio, 0.50f);

					var htc_air = Phys.GetAirConvectionHTC(Maths.Max(1.00f, air_velocity_avg));
					var combustion_rate = (area_fuel * solid_fire_ratio * burner_state.o2_ratio * (burner_state.flame_size + fuel_solid.porosity)) + (burner_state.flame_size * fuel_solid.flammability * fuel_solid.volatility * 0.10f);
					var mass_fuel_surface_tmp = (Mass)mass_fuel_surface;

					flammability += mass_fuel_surface * fuel_solid.flammability * solid_fire_ratio;

					var o2_avail_ratio_tmp = 0.00f;
					var energy_released = (Energy)0.00f;
					var mass_fuel_consumed = 0.00f;

					if (inventory_solid.resource.quantity > 0.80f)
					{
						Phys.TransferHeatConvection(ref temperature_chamber, ref temperature_fuel_surface, air_mass, mass_fuel_surface, Phys.air_specific_heat, fuel_specific_heat, htc_air, area_fuel, dt * heat_transfer_speed);
						Phys.TransferHeatDiffusion(ref temperature_fuel_surface, ref temperature_fuel_inside, mass_fuel_surface, mass_fuel_inside, fuel_specific_heat, 0.10f, fuel_solid.thermal_conductivity, dt * heat_transfer_speed * 4.00f);

						//if (temperature_fuel_surface >= fuel_solid.autoignition_temperature)
						//{
						//	combustion_rate = Maths.Max(combustion_rate, fuel_solid.volatility * Maths.Max(0.00f, (temperature_fuel_surface - fuel_solid.autoignition_temperature) * 0.01f));
						//}

						Combustion(in fuel_solid, ref mass_fuel_surface_tmp, ref moles_o2, ref moles_co2, ref moles_h2o, ref moles_n2, ref moles_h2, out energy_released, out o2_avail_ratio_tmp, combustion_rate: combustion_rate, ref o2_consumed);

						//var mass_char_consumed = Maths.Max(mass_char - mass_char_tmp, 0.00f);
						mass_fuel_consumed = Maths.Max(mass_fuel_surface - mass_fuel_surface_tmp, 0.00f);

						fuel_kg_consumed += mass_fuel_consumed;
						//mass_char -= mass_char_consumed;
						//mass_soot += mass_fuel_consumed * (1.00f - o2_avail_ratio_tmp);


						var energy_absorbed = Maths.MultDiff(ref energy_released.m_value, 0.91f);

						combustion_energy_released += energy_released;
						combustion_energy_absorbed += energy_absorbed;
						temperature_chamber.m_value.AddTowards(fuel_solid.flame_temperature_o2, Maths.Normalize(energy_released, heat_capacity_air));
					}

					var fuel_mass_evaporated_raw = (Mass)(Maths.Min(mass_fuel_total, evaporation_rate) * dt);
					//var fuel_mass_evaporated_raw = (Mass)Maths.Clamp(evaporation_rate * dt, mass_fuel_consumed, mass_fuel_surface_tmp);
					if (fuel_mass_evaporated_raw > Maths.epsilon)
					{
						var fuel_mass_evaporated = fuel_mass_evaporated_raw;

						var ash_mass_produced = Maths.MultDiff(ref fuel_mass_evaporated.m_value, fuel_solid.purity);
						if (fuel_a.IsNotNull())
						{
							var volatiles_amount_add = fuel_mass_evaporated / material_a.mass_per_unit;
							burner_state.fuel_vapor_a.quantity.AddTowards(air_container.volume * solid_fire_ratio, volatiles_amount_add);
						}

						mass_ash += ash_mass_produced;
						mass_soot += fuel_mass_evaporated * (1.00f - o2_avail_ratio_tmp);

						//mass_soot += fuel_mass_evaporated;

						inventory_solid.resource.Remove(fuel_mass_evaporated_raw / material_solid.mass_per_unit);
#if SERVER
						if (info.WorldTime >= burner_state.t_next_inventory_sync)
						{
							inventory_solid.MarkDirty();
						}
#endif

						temperature_fuel_surface = Maths.SumWeighted(temperature_fuel_surface, temperature_fuel_inside, mass_fuel_surface - fuel_mass_evaporated_raw, fuel_mass_evaporated_raw);
					}
				}
			}

			#region Vapor Combustion
			if (fuel_type_a == IMaterial.Fuel.Type.Liquid || fuel_type_a == IMaterial.Fuel.Type.Gas)
			{

				var fuel_a_mass = (Mass)(burner_state.fuel_vapor_a.quantity * material_a.mass_per_unit);
				var fuel_a_mass_tmp = fuel_a_mass;

				flammability += fuel_a.flammability * fuel_a_mass * (1.00f + fuel_a.volatility);

				var combustion_rate = burner_state.flame_size * fuel_a.flammability * burner_state.fuel_vapor_a.quantity;
				if (temperature_chamber >= fuel_a.autoignition_temperature)
				{
					combustion_rate = Maths.Max(combustion_rate, fuel_a.flammability * Maths.Max(0.00f, (temperature_chamber - fuel_a.autoignition_temperature) * 0.001f * burner_state.o2_ratio));
				}

				Combustion(in fuel_a, ref fuel_a_mass_tmp, ref moles_o2, ref moles_co2, ref moles_h2o, ref moles_n2, ref moles_h2, out var energy_released, out var o2_avail_ratio_tmp, combustion_rate: combustion_rate * dt, ref o2_consumed);
				var fuel_kg_consumed_tmp = Maths.Max(fuel_a_mass - fuel_a_mass_tmp, 0.00f);

				fuel_kg_consumed += fuel_kg_consumed_tmp;
				burner_state.fuel_vapor_a.quantity -= Maths.Min(fuel_kg_consumed_tmp / material_a.mass_per_unit, burner_state.fuel_vapor_a.quantity);

				combustion_energy_released += energy_released;
				mass_ash += fuel_kg_consumed_tmp * (1.00f - fuel_a.purity);
				temperature_chamber.m_value.AddTowards(fuel_a.flame_temperature_o2, Maths.Normalize(energy_released, heat_capacity_air));

#if CLIENT
				if (show_debug)
				{
					region.DrawDebugText(transform.position + new Vector2(-4.00f, -4.00f),
					$"o2_avail_ratio_tmp: {o2_avail_ratio_tmp:0.00000000}\n" +
					$"fuel_a_mass: {fuel_a_mass:0.0000} kg\n" +
					$"fuel_a_mass_tmp: {fuel_a_mass_tmp:0.0000}\n" +
					$"fuel_consumed: {fuel_kg_consumed_tmp:0.0000}\n" +
					//$"o2_consumed: {o2_consumed_tmp:0.0000}\n" +
					$"quantity: {burner_state.fuel_vapor_a.quantity:0.0000}\n" +
					$"flame_size: {burner_state.flame_size:0.000000}\n" +
					$"energy: {energy_released:0.000000} kJ\n" +
					"", Color32BGRA.Yellow);
				}
#endif
			}
			else
			{
				burner_state.fuel_vapor_a = default;
			}

			if (fuel_type_b == IMaterial.Fuel.Type.Liquid || fuel_type_b == IMaterial.Fuel.Type.Gas)
			{
				//burner_state.fuel_vapor_b.quantity.AddTowards(burner.volume_chamber * burner_state.modifier_fluid, burner.area_aux * 20.00f * fuel_b.viscosity.RcpFast() * burner_state.modifier_fluid * dt, out var delta);
				burner_state.fuel_vapor_b.quantity.AddTowards(air_container.volume * burner_state.modifier_fluid, burner.area_aux * 1.00f * fuel_b.viscosity.RcpFast() * burner_state.modifier_fluid * dt, out var delta);
				delta = Maths.Min(inventory_fluid.resource.quantity, delta).Abs();

				inventory_fluid.resource.Remove(delta);
#if SERVER
				if (info.WorldTime >= burner_state.t_next_inventory_sync)
				{
					inventory_fluid.MarkDirty();
				}
#endif

				var fuel_b_mass = (Mass)(burner_state.fuel_vapor_b.quantity * material_b.mass_per_unit);
				var fuel_b_mass_tmp = fuel_b_mass;

				flammability += fuel_b.flammability * fuel_b_mass * (1.00f + fuel_b.volatility);

				var combustion_rate = (burner_state.flame_size * fuel_b.flammability) * (1.00f + (fuel_b_mass * 1.97f)).Pow2();
				if (temperature_chamber >= fuel_b.autoignition_temperature)
				{
					combustion_rate = Maths.Max(combustion_rate, fuel_b.flammability * Maths.Max(0.00f, (temperature_chamber - fuel_b.autoignition_temperature) * 0.001f * burner_state.o2_ratio));
				}

				Combustion(in fuel_b, ref fuel_b_mass_tmp, ref moles_o2, ref moles_co2, ref moles_h2o, ref moles_n2, ref moles_h2, out var energy_released, out var o2_avail_ratio_tmp, combustion_rate: combustion_rate * dt, ref o2_consumed);

				var fuel_kg_consumed_tmp = Maths.Max(fuel_b_mass - fuel_b_mass_tmp, 0.00f);

				fuel_kg_consumed += fuel_kg_consumed_tmp;
				burner_state.fuel_vapor_b.quantity -= Maths.Min(fuel_kg_consumed_tmp / material_b.mass_per_unit, burner_state.fuel_vapor_b.quantity);

				combustion_energy_released += energy_released;
				mass_ash += fuel_kg_consumed_tmp * (1.00f - fuel_b.purity);
				temperature_chamber.m_value.AddTowards(fuel_b.flame_temperature_o2, Maths.Normalize(energy_released, heat_capacity_air));

#if CLIENT
				if (show_debug)
				{
					region.DrawDebugText(transform.position + new Vector2(4.00f, -4.00f),
					$"o2_avail_ratio_tmp: {o2_avail_ratio_tmp:0.00000000}\n" +
					$"fuel_b_mass: {fuel_b_mass:0.0000} kg\n" +
					$"fuel_b_mass_tmp: {fuel_b_mass_tmp:0.0000}\n" +
					$"fuel_consumed: {fuel_kg_consumed_tmp:0.0000}\n" +
					//$"o2_consumed: {o2_consumed_tmp:0.0000}\n" +
					$"quantity: {burner_state.fuel_vapor_b.quantity:0.0000}\n" +
					$"flame_size: {burner_state.flame_size:0.000000}\n" +
					$"energy: {energy_released:0.000000} kJ\n" +
					"", Color32BGRA.Yellow);
				}
#endif

				if (!inventory_fluid.resource.IsValid())
				{
					burner_state.fuel_vapor_b = default;
					inventory_fluid.resource = default;
				}
			}
			else
			{
				burner_state.fuel_vapor_b = default;
			}
			#endregion

			#region Flame
			var o2_consumed_delta = o2_consumed - burner_state.o2_consumed;
			//burner_state.flame_growth = Maths.Clamp(Maths.Lerp(burner_state.flame_growth, o2_consumed * flammability * 100, 0.25f).Sqrt(), 0.00f, 10.00f);
			burner_state.flame_growth.MoveTowardsDamped((burner_state.o2_ratio * flammability * 100).Sqrt(), o2_consumed.m_value, 0.10f); //, 0.00f, 10.00f);

			var flame_size = burner_state.flame_size;
			flame_size *= Maths.Lerp01(0.891f, 0.998f, burner_state.o2_ratio);
			flame_size += burner_state.flame_growth * burner_state.o2_ratio;
			burner_state.flame_growth *= 0.98f;

			burner_state.o2_consumed = o2_consumed;
			burner_state.o2_ratio = Maths.Normalize01(moles_o2, moles_total);
			#endregion

			//Phys.IdealGasLaw(Phys.atmospheric_pressure_kordel, out var volume_gas, moles_total, temperature_chamber);

			//var volume_gas_excess = Maths.Max(0.00f, volume_gas - air_container.volume);

			//Phys.VentilationAirFlow(area_exhaust, burner.height, temperature_exhaust, temperature_ambient, out var air_flow_output);

			//Phys.VolumetricFlowRate(area_intake, air_velocity_ambient, out var air_flow_intake);
			//Phys.VolumetricFlowRate(area_exhaust, air_velocity_ambient, out var air_flow_exhaust);

			//var volume_gas_out_exhaust_potential = air_flow_output * dt;
			//var volume_gas_out_exhaust_actual = Maths.Clamp(volume_gas_excess, 0.00f, volume_gas_out_exhaust_potential);

			//var volume_gas_backdraft = Maths.Max(0.00f, volume_gas_excess - volume_gas_out_exhaust_actual);

			//var volume_gas_out_intake_potential = air_flow_intake * dt; // TODO: calculate backdraft air flow separately, since it has to overcome the intake's pumped/ambient air velocity
			//var volume_gas_out_intake_actual = Maths.Clamp(volume_gas_backdraft, 0.00f, volume_gas_out_intake_potential);

			//var volume_gas_in_intake_potential = air_flow_intake * dt;
			//var volume_gas_in_intake_actual = Maths.Max(0.00f, volume_gas_in_intake_potential - volume_gas_out_intake_actual);

			//var volume_gas_in_exhaust_potential = air_flow_exhaust * dt;
			//var volume_gas_in_exhaust_actual = Maths.Max(0.00f, volume_gas_in_exhaust_potential - volume_gas_out_exhaust_actual);

			//var volume_gas_out = volume_gas_out_intake_actual + volume_gas_out_exhaust_actual;
			//var volume_gas_in = volume_gas_in_intake_actual + volume_gas_in_exhaust_actual;

			//var volume_gas_new = volume_gas - volume_gas_out - volume_gas_in;
			//pressure = Maths.Normalize(volume_gas - volume_gas_out, burner.volume_chamber) * Phys.atmospheric_pressure_kordel;

			//var loss_ratio = Maths.Normalize01(Maths.Max(burner.volume_chamber - volume_gas_in, volume_gas_new), volume_gas);

			var particulates = new Air.Particulates();

			if (vent_intake.IsNotNull())
			{
				var volume_displaced = Maths.Min(0.00f, vent_intake.flow_rate_old).Abs();
				if (volume_displaced > 0.00f)
				{
					var loss_ratio_raw = Maths.Normalize01(volume_displaced, air_container.volume);
					var loss_ratio = 1.00f - (loss_ratio_raw * 0.40f);

					particulates.soot += mass_soot.m_value.MultDiff(loss_ratio);
					particulates.soot += burner_state.fuel_vapor_a.quantity.MultDiff((1.00f - (loss_ratio_raw * vapor_a_loss_mult * 0.32f)).Clamp01());
					particulates.soot += burner_state.fuel_vapor_b.quantity.MultDiff((1.00f - (loss_ratio_raw * vapor_b_loss_mult * 0.32f)).Clamp01());
					particulates.ash += mass_ash.m_value.MultDiff(loss_ratio);
				}
			}

			if (vent_exhaust.IsNotNull())
			{
				var volume_displaced = Maths.Min(0.00f, vent_exhaust.flow_rate_old).Abs();
				if (volume_displaced > 0.00f)
				{
					var loss_ratio_raw = Maths.Normalize01(volume_displaced, air_container.volume);
					var loss_ratio = 1.00f - (loss_ratio_raw * 0.40f);

					particulates.soot += mass_soot.m_value.MultDiff(loss_ratio);
					particulates.soot += burner_state.fuel_vapor_a.quantity.MultDiff((1.00f - (loss_ratio_raw * vapor_a_loss_mult * 0.32f)).Clamp01());
					particulates.soot += burner_state.fuel_vapor_b.quantity.MultDiff((1.00f - (loss_ratio_raw * vapor_b_loss_mult * 0.32f)).Clamp01());
					particulates.ash += mass_ash.m_value.MultDiff(loss_ratio);
				}
			}

			air_container.particulates += particulates;

			temperature_chamber.m_value += Maths.Normalize(reaction_energy_released, heat_capacity_air);
			var ambient_temperature = region.GetAmbientTemperature(transform.position);

			temperature_chamber.m_value.MoveTowards(ambient_temperature, (ambient_temperature - temperature_chamber).m_value.Abs() * (1.00f - burner.insulation) * dt * 0.30f);

			burner_state.temperature_fuel_surface = Maths.Clamp(temperature_fuel_surface, 200, 5000);
			burner_state.temperature_fuel_bulk = Maths.Clamp(temperature_fuel_inside, 200, 5000);

			air_container.air = Vec8f.Max(Vec8f.From(air), Vec8f.Zero).To<Air.Composition>();
			air_container.temperature = Maths.Clamp(temperature_chamber, 200, 5000);

			burner_state.mass_char = mass_char;
			burner_state.mass_ash = mass_ash;
			burner_state.mass_soot = mass_soot;

			//burner_state.modifier_intake.MoveTowards(burner_state.modifier_intake_target, Maths.Max(MathF.Abs(burner_state.modifier_intake_target - burner_state.modifier_intake), 0.15f) * 8.00f * dt);
			//burner_state.modifier_exhaust.MoveTowards(burner_state.modifier_output_target, Maths.Max(MathF.Abs(burner_state.modifier_output_target - burner_state.modifier_exhaust), 0.15f) * 8.00f * dt);
			burner_state.modifier_solid.MoveTowards(burner_state.modifier_solid_target, 4.00f * dt);
			burner_state.modifier_fluid.MoveTowards(burner_state.modifier_fluid_target, 4.00f * dt);

			//burner_state.pressure_chamber = pressure;
			//burner_state.exhaust_purity = Maths.SumRatio(mass_pure_exhaust + mass_steam_exhaust, mass_soot_exhaust + mass_ash_exhaust).Clamp01();
			burner_state.released_energy_current = Maths.Max(reaction_energy_released + combustion_energy_released, 0.00f);
			burner_state.power_current.m_value = Maths.Lerp(burner_state.power_current.m_value, burner_state.released_energy_current.m_value * App.tickrate_f32, 0.02f);

			burner_state.flame_size = flame_size;

			//var smoke_color = Color32BGRA.Lerp(new Color32BGRA(255, 50, 40, 30), new Color32BGRA(220, 255, 255, 255), burner_state.exhaust_purity.Pow2());
			//if (temperature_exhaust < 400.00f) smoke_color.a = 0;

			//burner_state.color_smoke = smoke_color;

			if (vent_intake.IsNotNull())
			{
				vent_intake.modifier.MoveTowardsDamped(burner_state.modifier_intake_target, 4.00f * dt, 0.50f);
				//vent_intake.flow_rate.MoveTowardsDamped((volume_gas_in_intake_actual - volume_gas_out_intake_actual) * App.tickrate, 1.00f, 0.10f);
			}

			if (vent_exhaust.IsNotNull())
			{
				vent_exhaust.modifier.MoveTowardsDamped(burner_state.modifier_output_target, 4.00f * dt, 0.50f);

				//vent_exhaust.flow_rate.MoveTowardsDamped((volume_gas_out_exhaust_actual - volume_gas_in_exhaust_actual) * App.tickrate, 1.00f, 0.10f);
			}

#if SERVER
			if (info.WorldTime >= burner_state.t_next_inventory_sync)
			{
				burner_state.t_next_inventory_sync = info.WorldTime + random.NextFloatRange(2.00f, 4.00f);
			}

			if (region.GetCurrentTick() % 64 == 0)
			{
				//burner_state.Sync(entity);
			}

			if (false && pressure > burner.pressure_max && temperature_chamber > burner.temperature_max)
			{
				Explosion.Spawn(ref region, transform.position, (ent_explosion, ref explosion) =>
				{
					explosion.radius = 13.00f;
					explosion.power = 2.00f;
					explosion.damage_type = Damage.Type.Explosion;
					explosion.damage_type_secondary = Damage.Type.Shockwave;
					explosion.fire_amount = 1.50f;
					explosion.sparks_amount = 1.40f;
					explosion.smoke_amount = 1.80f;
					explosion.flash_duration_multiplier = 1.40f;
					explosion.force_multiplier *= 0.20f;
					explosion.damage_entity = 2500.00f;
					explosion.damage_terrain = 19000.00f;
				});

				burner_state = new();

				burner_state.Sync(entity);

				//entity.Delete();
			}
#endif

#if CLIENT
			if (show_debug)
			{
				var ts_ms = ts.GetMilliseconds();

				region.DrawDebugText(transform.position + new Vector2(-3.00f, 2.00f),
					$"elapsed: {ts_ms:0.0000} ms\n\n" +

					$"o2_ratio: {burner_state.o2_ratio:0.00}\n" +
					$"o2_consumed: {burner_state.o2_consumed:0.00}\n" +
					$"o2_consumed_delta: {o2_consumed_delta:0.00}\n" +
					$"moles_total: {moles_total:0.00}\n" +
					$"moles_air: {(moles_o2 + moles_n2 + moles_co2):0.00}\n\n" +

					$"moles_o2: {air_container.air.moles_o2:0.00}\n" +
					$"moles_h2: {air_container.air.moles_h2:0.00}\n" +
					$"moles_n2: {air_container.air.moles_n2:0.00}\n" +
					$"moles_co2: {air_container.air.moles_co2:0.00}\n" +

					$"moles_co: {air_container.air.moles_co:0.00}\n" +
					$"moles_so2: {air_container.air.moles_so2:0.00}\n" +
					$"moles_no2: {air_container.air.moles_no2:0.00}\n" +
					$"moles_h2o: {air_container.air.moles_h2o:0.00}\n\n" +

					//$"test: {moles_o2 * Maths.Normalize01(moles_c * 0.50f, moles_o2):0.00}\n\n" +

					$"heat_capacity: {heat_capacity_air:0.0000}\n" +
					//$"energy: {oxidation_energy_produced:0.0000}\n" +
					//$"energy: {oxidation_energy_produced:0.0000}\n\n" +

					//$"kelvin_flame: {burner_state.flame_temperature:0.00}\n" +
					$"flame_size: {burner_state.flame_size:0.0000}\n" +
					$"flame_growth: {burner_state.flame_growth:0.0000}\n\n" +

					$"mass_o2: {(air_container.air.moles_o2 * Phys.o2_molar_mass):0.00} kg\n" +
					$"mass_n2: {(air_container.air.moles_n2 * Phys.n2_molar_mass):0.00} kg\n" +
					$"mass_co2: {(air_container.air.moles_co2 * Phys.co2_molar_mass):0.00} kg\n" +
					$"mass_air: {((air_container.air.moles_o2 * Phys.o2_molar_mass) + (air_container.air.moles_n2 * Phys.n2_molar_mass) + (air_container.air.moles_co2 * Phys.co2_molar_mass)):0.00} kg\n\n" +

					$"mass_char: {burner_state.mass_char:0.00}\n" +
					$"mass_soot: {mass_soot:0.00}\n" +
					$"mass_ash: {mass_ash:0.00}\n" +
					//$"mass_gas: {mass_gas:0.00} ({air_kg_chamber:0.00})\n" +
					//$"mass_fuel: {mass_fuel_total:0.00}\n" +
					//$"mass_gas_a: {burner_state.mass_air:0.0000}\n" +
					//$"mass_gas_b: {mass_gas_b:0.0000}\n" +
					//$"mass_gas_delta: {mass_gas_b - mass_gas_a:0.0000}\n" +
					//$"Delta: {pressure_delta:0.00000000} bar",
					"", Color32BGRA.Yellow);

				region.DrawDebugText(transform.position + new Vector2(3.00f, 2.00f),
					//$"air_displaced_ratio: {air_intake_ratio:0.000000}\n" +
					//$"loss_ratio: {loss_ratio:0.000000}\n" +
					//$"overflow_ratio: {overflow_ratio:0.000000}\n" +
					//$"vent_ratio: {vent_ratio:0.000000}\n" +
					//$"moles_oxygen_intake: {moles_oxygen_intake:0.000000}\n" +
					//$"moles_oxygen_max: {moles_oxygen_max:0.00}\n\n" +
					//$"carbon_oxygen_ratio: {carbon_oxygen_ratio:0.0000}\n" +

					//$"mass_pure_exh: {mass_pure_exhaust:0.00}\n" +
					//$"mass_soot_exh: {mass_soot_exhaust:0.00}\n" +
					//$"mass_air_in: {air_kg_intake:0.00}\n" +
					//$"solid_oxidation_modifier: {solid_oxidation_modifier:0.00000}\n" +
					//$"vapor_oxidation_modifier: {vapor_oxidation_modifier:0.00000}\n" +
					//$"air_molar_mass: {Phys.air_molar_mass:0.000000000}\n" +
					//$"air_density_kordel: {Phys.air_density_kordel:0.000000000}\n" +

					//$"pressure_atmospheric: {Phys.CalculateAtmosphericPressure(temperature_ambient, 0.00f):0.00000}\n" +
					$"pressure: {pressure:0.0000}\n\n" +

					$"vapor_a: {burner_state.fuel_vapor_a.quantity:0.00} {burner_state.fuel_vapor_a.material}\n" +
					$"vapor_b: {burner_state.fuel_vapor_b.quantity:0.00} {burner_state.fuel_vapor_b.material}\n\n" +

					//$"mult_2co_o2_to_2co2: {mult_2co_o2_to_2co2:0.00}\n" +
					//$"mult_2c_o2_to_2co: {mult_2c_o2_to_2co:0.00}\n" +
					//$"mult_char: {mult_char:0.00}\n" +
					//$"mult_c_o2_to_co2: {mult_c_o2_to_co2:0.00}\n" +
					//$"mult_h2o: {mult_h2o:0.00}\n" +
					//$"mult_methane_o2: {mult_methane_o2:0.00}\n" +
					//$"mult_no2: {mult_no2:0.00}\n\n" +


					//$"moles_oxygen added: {moles_oxygen_added:0.00}\n" +
					//$"moles_carbon_released: {moles_c_released:0.00}\n\n" +

					//$"mass_volatiles: {burner_state.mass_volatiles:0.00}\n" +

					//$"temperature_exhaust: {burner_state.temperature_exhaust.Celsius():0.00} °C\n" +
					//$"temperature_chamber: {burner_state.temperature_chamber.Celsius():0.00} °C\n" +
					$"temperature_fuel_surface: {burner_state.temperature_fuel_surface.Celsius():0.00} °C\n" +
					$"temperature_fuel: {burner_state.temperature_fuel_bulk.Celsius():0.00} °C\n\n" +
					//$"mass_fuel_total: {mass_fuel_total:0.0000}\n" +
					//$"mass_fuel_surface: {mass_fuel_surface:0.0000}\n" +
					//$"mass_fuel_inside: {mass_fuel_inside:0.0000}\n" +
					//$"mass_total: {(mass_total):0.0000}\n\n" +
					//$"air_kg_chamber: {air_kg_chamber:0.0000}\n" +
					//$"fuel_area: {area_fuel:0.0000}\n" +
					$"evaporation_modifier: {evaporation_modifier:0.00}\n" +
					//$"evaporation_rate: {evaporation_rate:0.00}\n" +
					$"released_energy_current: {burner_state.released_energy_current:0.0000}\n" +
					$"combustion_energy_released: {combustion_energy_released:0.0000}\n" +
					$"combustion_energy_absorbed: {combustion_energy_absorbed:0.0000}\n" +
					$"reaction_energy_released: {reaction_energy_released:0.0000}\n\n" +
					//$"oxidation_energy_produced_solid: {oxidation_energy_produced_solid:0.00}\n" +
					//$"temperature_chamber_added: {temperature_chamber_added:0.00}\n" +
					//$"htc: {htc:0.000000}\n" +
					//$"heat_flux: {heat_flux:0.00000}\n" +
					//$"heat_flux_inside: {heat_flux_inside:0.00000}" +
					//$"air_velocity: {air_velocity:0.000}\n\n" +
					$"air_velocity_avg: {air_velocity_avg:0.000}\n" +
					//$"air_velocity_ambient: {air_velocity_ambient:0.000}\n" +
					//$"air_velocity_exhaust_test: {air_velocity_exhaust_test:0.000}\n" +
					//$"air_velocity_exhaust: {air_velocity_exhaust:0.000}\n\n" +

					//$"air_volume_input: {air_volume_input:0.000}\n" +
					//$"volume_gas: {volume_gas:0.000}\n" +
					//$"volume_gas_new: {volume_gas_new:0.000}\n" +
					//$"volume_gas_excess: {volume_gas_excess:0.000}\n" +
					//$"volume_gas_in: {volume_gas_in:0.000}\n" +
					//$"volume_gas_out: {volume_gas_out:0.000}\n" +
					//$"volume_gas_backdraft: {volume_gas_backdraft:0.000}\n" +

					//$"volume_gas_out_exhaust_potential: {volume_gas_out_exhaust_potential:0.000}\n" +
					//$"volume_gas_out_exhaust_actual: {volume_gas_out_exhaust_actual:0.000}\n" +

					//$"volume_gas_in_intake_potential: {volume_gas_in_intake_potential:0.000}\n" +
					//$"volume_gas_in_intake_actual: {volume_gas_in_intake_actual:0.000}\n" +
					//$"volume_gas_vent_max: {volume_gas_out_exhaust_max:0.000}\n" +
					//$"volume_gas_vent_max: {air_volume_input:0.000}\n" +
					//$"air_flow: {air_flow:0.000}\n" +
					//$"air_flow_ambient: {air_flow_ambient:0.000}\n" +
					//$"air_flow_vent: {air_flow_vent:0.000}" +
					"",
					//$"Delta: {pressure_delta:0.00000000} bar",
					Color32BGRA.Yellow);
			}
#endif
		}

		[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region), HasComponent<Burner.Data>(Source.Modifier.Owned, true)]
		public static void UpdateSprites(
		[Source.Owned, Pair.Tag("intake"), Optional(true)] ref Animated.Renderer.Data renderer_intake,
		[Source.Owned, Pair.Tag("exhaust"), Optional(true)] ref Animated.Renderer.Data renderer_exhaust,

		[Source.Owned, Pair.Tag("intake"), Optional(true)] ref Air.Vent.Data vent_intake,
		[Source.Owned, Pair.Tag("exhaust"), Optional(true)] ref Air.Vent.Data vent_exhaust,

		[Source.Owned, Pair.Tag("aux"), Optional(true)] ref Animated.Renderer.Data renderer_aux)
		{
			if (renderer_intake.IsNotNull() && vent_intake.IsNotNull())
			{
				renderer_intake.offset = vent_intake.offset + new Vector2(0.00f, Maths.Lerp01(0.00f, -0.500f, vent_intake.modifier));
			}

			if (renderer_exhaust.IsNotNull() && vent_exhaust.IsNotNull())
			{
				renderer_exhaust.rotation = Maths.Lerp01(0.00f, 1.150f, vent_exhaust.modifier);
			}
		}

#if CLIENT
		//[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		//public static void UpdateEffects_Intake(ISystem.Info info, Entity entity, ref Region.Data region, ref XorRandom random,
		//[Source.Owned] in Transform.Data transform, [Source.Owned] in Burner.Data burner, [Source.Owned] in Burner.State burner_state,
		//[Source.Owned, Pair.Tag("intake")] ref Vent.Data vent)
		////[Source.Owned, Pair.Tag("intake"), Optional(true)] ref Sound.Emitter sound_emitter)
		//{
		//	return;

		//	if (info.WorldTime >= vent.t_next_smoke && !burner.flags.HasAny(Data.Flags.No_Smoke) && !vent.flags.HasAny(Vent.Data.Flags.Has_Pipe))
		//	{
		//		if (vent.flow_rate <= -0.02f)
		//		{
		//			var modifier = MathF.Sqrt(vent.flow_rate.m_value.Abs()) * 0.50f;
		//			var vel = Maths.Min(vent.velocity.Abs(), 20.00f);

		//			vent.t_next_smoke = info.WorldTime + Maths.Lerp01(0.40f, 0.10f, Maths.Normalize01(vel * 0.90f, 8.00f)); //  (0.40f * Maths.Mulpo(purity, 0.40f));

		//			if (modifier > 0.02f && burner_state.color_smoke.IsVisible() && Camera.IsVisible(Camera.CullType.Rect2x, transform.position))
		//			{
		//				var dir = Maths.RadToDir(vent.rotation);

		//				var pos = transform.LocalToWorld(vent.offset);
		//				Particle.Spawn(ref region, new Particle.Data()
		//				{
		//					texture = texture_smoke,
		//					lifetime = Maths.Clamp(random.NextFloatRange(2.00f, 6.00f) * modifier, 1.00f, 4.00f),
		//					pos = pos + random.NextVector2(0.10f) + (dir * vel * 0.17f),
		//					//vel = new Vector2(0.70f, -1.10f) * (1.00f + (modifier2 * 0.10f)),
		//					vel = transform.LocalToWorld(dir * vel * random.NextFloatRange(0.90f, 1.10f), position: false),
		//					force = new Vector2(2.50f, -2.50f) * random.NextFloatRange(0.90f, 1.10f),
		//					fps = random.NextByteRange(6, 10),
		//					frame_count = 64,
		//					frame_count_total = 64,
		//					frame_offset = random.NextByteRange(0, 64),
		//					scale = (modifier * 0.22f) + random.NextFloatRange(0.19f, 0.28f),
		//					rotation = random.NextFloatRange(-10.00f, 10.00f),
		//					angular_velocity = random.NextFloat(0.60f),
		//					growth = 0.25f * (1.00f + (modifier.Clamp01() * 0.15f)) * random.NextFloatRange(0.90f, 1.10f),
		//					drag = random.NextFloatRange(0.015f, 0.04f),
		//					color_a = burner_state.color_smoke,
		//					color_b = burner_state.color_smoke.WithAlphaMult(0.00f)
		//				});
		//			}
		//		}
		//		else
		//		{
		//			vent.t_next_smoke = info.WorldTime + random.NextFloatRange(0.50f, 1.00f);
		//		}
		//	}
		//}

		//[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		//public static void UpdateEffects_Exhaust(ISystem.Info info, Entity entity, ref Region.Data region, ref XorRandom random,
		//[Source.Owned] in Transform.Data transform, [Source.Owned] in Burner.Data burner, [Source.Owned] in Burner.State burner_state,
		//[Source.Owned, Pair.Tag("exhaust")] ref Vent.Data vent)
		////[Source.Owned, Pair.Tag("exhaust"), Optional(true)] ref Sound.Emitter sound_emitter)
		//{
		//	return;

		//	if (info.WorldTime >= vent.t_next_smoke && !burner.flags.HasAny(Data.Flags.No_Smoke) && !vent.flags.HasAny(Vent.Data.Flags.Has_Pipe))
		//	{
		//		if (vent.flow_rate > 0.02f)
		//		{
		//			var modifier = MathF.Sqrt(vent.flow_rate) * 0.50f;
		//			var vel = Maths.Min(vent.velocity, 20.00f);

		//			vent.t_next_smoke = info.WorldTime + Maths.Lerp01(0.40f, 0.10f, Maths.Normalize01(vel * 0.90f, 8.00f)); //  (0.40f * Maths.Mulpo(purity, 0.40f));

		//			if (modifier > 0.02f && burner_state.color_smoke.IsVisible() && Camera.IsVisible(Camera.CullType.Rect2x, transform.position))
		//			{
		//				var dir = Maths.RadToDir(vent.rotation);

		//				var pos = transform.LocalToWorld(vent.offset);
		//				Particle.Spawn(ref region, new Particle.Data()
		//				{
		//					texture = texture_smoke,
		//					lifetime = Maths.Clamp(random.NextFloatRange(2.00f, 6.00f) * modifier, 1.00f, 4.00f),
		//					pos = pos + random.NextVector2(0.10f) + (dir * vel * 0.17f),
		//					//vel = new Vector2(0.70f, -1.10f) * (1.00f + (modifier2 * 0.10f)),
		//					vel = transform.LocalToWorld(dir * vel * random.NextFloatRange(0.90f, 1.10f), position: false),
		//					force = new Vector2(2.50f, -2.50f) * random.NextFloatRange(0.90f, 1.10f),
		//					fps = random.NextByteRange(6, 10),
		//					frame_count = 64,
		//					frame_count_total = 64,
		//					frame_offset = random.NextByteRange(0, 64),
		//					scale = (modifier * 0.22f) + random.NextFloatRange(0.19f, 0.28f),
		//					rotation = random.NextFloatRange(-10.00f, 10.00f),
		//					angular_velocity = random.NextFloat(0.60f),
		//					growth = 0.25f * (1.00f + (modifier.Clamp01() * 0.15f)) * random.NextFloatRange(0.90f, 1.10f),
		//					drag = random.NextFloatRange(0.015f, 0.04f),
		//					color_a = burner_state.color_smoke,
		//					color_b = burner_state.color_smoke.WithAlphaMult(0.00f)
		//				});
		//			}
		//		}
		//		else
		//		{
		//			vent.t_next_smoke = info.WorldTime + random.NextFloatRange(0.50f, 1.00f);
		//		}
		//	}
		//}

		[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void UpdateEffects(ref Region.Data region, ref XorRandom random,
		[Source.Owned] in Burner.Data burner, [Source.Owned] ref Burner.State burner_state,

		[Source.Owned, Pair.Tag("intake"), Optional(true)] ref Air.Vent.Data vent_intake,
		//[Source.Owned, Pair.Tag("exhaust"), Optional(true)] ref Vent.Data vent_exhaust,

		//[Source.Owned, Pair.Tag("intake"), Optional(true)] ref Sound.Emitter sound_emitter_intake,
		//[Source.Owned, Pair.Tag("exhaust"), Optional(true)] ref Sound.Emitter sound_emitter_exhaust,

		[Source.Owned, Pair.Tag("aux"), Optional(true)] ref Sound.Emitter sound_emitter_aux,
		[Source.Owned, Pair.Component<Burner.Data>, Optional(true)] ref Sound.Emitter sound_emitter,
		[Source.Owned, Pair.Component<Burner.Data>, Optional(true)] ref Light.Data light,

		[Source.Owned] in Air.Container.Data air_container,
		[Source.Owned] in Transform.Data transform)
		{
			//var purity = Maths.Max(burner_state.cached_purity, burner_state.exhaust_purity);

			//var moles_total = burner_state.moles_h2o

			//var color_smoke = Color32BGRA.White;
			//color_smoke = color_smoke.Lerp(new Color32BGRA(255, 50, 40, 30), )

			//var purity = burner_state.exhaust_purity;

			//var modifier1 = Maths.Clamp(burner_state.fuel_consumed * 1.70f, 0.00f, 1.50f);
			var modifier1 = (Maths.InvLerp01(370, 1200, air_container.temperature) * 1.20f).Clamp01();
			var modifier2 = (Maths.InvLerp01(400, 2000, air_container.temperature) * 1.20f).Clamp01();
			//App.WriteLine(modifier1);

			//var modifier3_intake = (Maths.Avg(1.00f - purity, modifier1 * burner_state.intake_output) * 4).Clamp01();
			//var modifier3_exhaust = (Maths.Avg(1.00f - purity, modifier1 * burner_state.exhaust_output) * 4).Clamp01();


			//modifier1 = Maths.NormalizeClamp(burner_state.fuel_quantity, burner.capacity) * burner_state.modifier; // Maths.Clamp01((burner_state.fuel_consumed * material.mass_per_unit * 50.00f) / Burner.update_interval); //  Maths.NormalizeClamp(burner_state.resource.quantity, burner.capacity);
			//modifier2 = Maths.Clamp(consumed_amount * material.mass_per_unit * 40 * (1.00f - fuel.purity), 0.30f, 2.00f);

			//ref var material = ref burner_state.cached_fuel_material.GetData();
			//if (material.IsNotNull() && burner_state.fuel_quantity >= 0.01f && material.flags.HasAll(Material.Flags.Fuel) && material.fuel.HasValue)
			//{
			//	ref var fuel = ref material.fuel.GetRefOrNull();

			//	var quantity = Maths.Min(burner_state.fuel_quantity, burner.capacity);
			//	var drain = 1.00f * burner._drain_multiplier * burner_state.modifier;

			//	var consumed_amount = ((float)(drain / fuel.energy) * burner.capacity) / Burner.update_interval;

			//	modifier1 = Maths.NormalizeClamp(burner_state.fuel_quantity, burner.capacity) * burner_state.modifier; // Maths.Clamp01((burner_state.fuel_consumed * material.mass_per_unit * 50.00f) / Burner.update_interval); //  Maths.NormalizeClamp(burner_state.resource.quantity, burner.capacity);
			//	modifier2 = Maths.Clamp(consumed_amount * material.mass_per_unit * 40 * (1.00f - fuel.purity), 0.30f, 2.00f);

			//	//purity = fuel.purity;
			//}

			if (sound_emitter.IsNotNull())
			{
				sound_emitter.volume_mult = Maths.Lerp2(sound_emitter.volume_mult, modifier1, 0.01f, 0.25f).Clamp0X();
				sound_emitter.pitch_mult = Maths.Lerp(sound_emitter.pitch_mult, 0.30f + (modifier1), 0.01f).Clamp0X();
			}

			if (sound_emitter_aux.IsNotNull())
			{
				sound_emitter_aux.volume_mult = Maths.Lerp2(sound_emitter_aux.volume_mult, burner_state.fuel_vapor_b.material != 0 ? burner_state.modifier_fluid * 2.00f : 0.00f, 0.10f, 0.25f).Clamp01();
				sound_emitter_aux.pitch_mult = Maths.Lerp2(sound_emitter_aux.pitch_mult, 0.70f + (burner_state.modifier_fluid * 0.40f), 0.05f, 0.03f).Clamp0X();
			}

			if (light.IsNotNull())
			{
				var intake_modifier = 1.00f;
				if (vent_intake.IsNotNull())
				{
					intake_modifier = vent_intake.flags.HasAny(Air.Vent.Data.Flags.Has_Pipe) ? 0.05f : vent_intake.modifier;
				}

				//light.intensity = Maths.Lerp(light.intensity, modifier1 + Maths.Max((burner_state.current_temperature - 700.00f) / 1000.00f, 0.00f), 0.10f);
				light.intensity = Maths.Lerp(light.intensity, Maths.Sqrt(modifier2 * 2.50f) * random.NextFloatRange(0.80f, 1.50f), 0.10f) * MathF.Pow((intake_modifier * 2.00f).Clamp01(), 0.10f);
			}

			if (burner_state.released_energy_current > 15.00f && !burner.flags.HasAny(Data.Flags.Open_Air)) Shake.Emit(ref region, transform.position, burner_state.released_energy_current * 0.01f, 0.23f, burner_state.released_energy_current * 0.12f);

			//if (!burner.flags.HasAny(Data.Flags.No_Smoke) && info.WorldTime >= burner_state.t_next_smoke)
			//{
			//	var modifier2_intake = MathF.Sqrt(burner_state.intake_output * 4) * 1.80f;
			//	var modifier2_exhaust = MathF.Sqrt(burner_state.exhaust_output * 4) * 1.80f;


			//	var vel = burner_state.exhaust_velocity * 0.60f; // * burner_state.modifier_output;
			//	vel.Clamp(0.00f, 20.00f);

			//	burner_state.t_next_smoke = info.WorldTime + Maths.Lerp01(0.40f, 0.10f, Maths.Normalize01(vel * 0.90f, 8.00f)); //  (0.40f * Maths.Mulpo(purity, 0.40f));

			//	if (Camera.IsVisible(Camera.CullType.Rect2x, transform.position))
			//	{
			//		if (modifier2_exhaust > 0.02f && burner_state.color_smoke.IsVisible())
			//		{
			//			var pos = transform.LocalToWorld(burner.smoke_offset);
			//			Particle.Spawn(ref region, new Particle.Data()
			//			{
			//				texture = texture_smoke,
			//				lifetime = Maths.Clamp(random.NextFloatRange(2.00f, 6.00f) * modifier2_exhaust, 1.00f, 4.00f),
			//				pos = pos + random.NextVector2(0.10f) + (burner.smoke_direction * vel * 0.17f),
			//				//vel = new Vector2(0.70f, -1.10f) * (1.00f + (modifier2 * 0.10f)),
			//				vel = transform.LocalToWorld(burner.smoke_direction * vel * random.NextFloatRange(0.90f, 1.10f), position: false),
			//				force = new Vector2(2.50f, -2.50f) * random.NextFloatRange(0.90f, 1.10f),
			//				fps = random.NextByteRange(6, 10),
			//				frame_count = 64,
			//				frame_count_total = 64,
			//				frame_offset = random.NextByteRange(0, 64),
			//				scale = (modifier2_exhaust * 0.22f) + random.NextFloatRange(0.19f, 0.28f),
			//				rotation = random.NextFloatRange(-10.00f, 10.00f),
			//				angular_velocity = random.NextFloat(0.60f),
			//				growth = 0.25f * (1.00f + (modifier2_exhaust.Clamp01() * 0.15f)) * random.NextFloatRange(0.90f, 1.10f),
			//				drag = random.NextFloatRange(0.015f, 0.04f),
			//				color_a = burner_state.color_smoke,
			//				color_b = burner_state.color_smoke.WithAlphaMult(0.00f)
			//			});
			//		}

			//		if (modifier2_intake > 0.02f && burner_state.color_smoke.IsVisible())
			//		{
			//			var pos = transform.LocalToWorld(burner.intake_offset);
			//			Particle.Spawn(ref region, new Particle.Data()
			//			{
			//				texture = texture_smoke,
			//				lifetime = Maths.Clamp(random.NextFloatRange(2.00f, 6.00f) * modifier2_intake, 1.00f, 4.00f),
			//				pos = pos + random.NextVector2(0.10f) + (burner.intake_direction * vel * 0.17f),
			//				//vel = new Vector2(0.70f, -1.10f) * (1.00f + (modifier2 * 0.10f)),
			//				vel = transform.LocalToWorld(burner.intake_direction * vel * random.NextFloatRange(0.90f, 1.10f), position: false),
			//				force = new Vector2(2.50f, -2.50f) * random.NextFloatRange(0.90f, 1.10f),
			//				fps = random.NextByteRange(6, 10),
			//				frame_count = 64,
			//				frame_count_total = 64,
			//				frame_offset = random.NextByteRange(0, 64),
			//				scale = (modifier2_intake * 0.22f) + random.NextFloatRange(0.19f, 0.28f),
			//				rotation = random.NextFloatRange(-10.00f, 10.00f),
			//				angular_velocity = random.NextFloat(0.60f),
			//				growth = 0.25f * (1.00f + (modifier2_intake.Clamp01() * 0.15f)) * random.NextFloatRange(0.90f, 1.10f),
			//				drag = random.NextFloatRange(0.015f, 0.04f),
			//				color_a = burner_state.color_smoke,
			//				color_b = burner_state.color_smoke.WithAlphaMult(0.00f)
			//			});
			//		}
			//	}
			//}

		}
#endif
	}

}
