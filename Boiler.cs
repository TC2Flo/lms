namespace TC2.Base.Components
{
	// https://en.wikipedia.org/wiki/Vertical_boiler
	// https://en.wikipedia.org/wiki/Water-tube_boiler
	// https://en.wikipedia.org/wiki/Fire-tube_boiler
	// https://en.wikipedia.org/wiki/Category:Boilers
	public static partial class Boiler
	{
		[IComponent.Data(Net.SendType.Reliable, region_only: true), IComponent.With<Boiler.State>]
		public struct Data(): IComponent
		{
			[Flags]
			public enum Flags: uint
			{
				None = 0u,
			}

			// https://en.wikipedia.org/wiki/Boiler
			public enum Type: byte
			{
				Undefined = 0,

				Flued, // https://en.wikipedia.org/wiki/Flued_boiler
				Flash, // https://en.wikipedia.org/wiki/Flash_boiler
				Fire_Tube,
				Water_Tube
			}

			[Editor.Picker.Position(relative: true, mark_modified: true)] public Vector2 exhaust_offset;
			[Editor.Picker.Position(relative: true, mark_modified: true)] public Vector2 burst_offset;
			public Sprite sprite_burst = SteamEngine.sprite_burst_default;

			public Pressure pressure_max = 0.80.MPa();

			public Distance tank_thickness = 2.00f.mm();

			public Volume reservoir_volume = 600.00f.l();
			public Volume exchanger_volume = 80.00f.l();
			public Volume steam_tank_volume = 40.00f.l();

			public Mass mass_tank = 5.00f.kg();
			public Mass mass_heater = 1.00f.kg();

			public Area area_surface;
			public Area area_heater;

			public float insulation = 0.20f;
			public float efficiency = 0.80f;
			public float blowdown_rate = 0.02f;
			public float flow_rate = 0.02f;

			public float burst_threshold = 0.85f;
			public float burst_chance_modifier = 1.00f;

			public Boiler.Data.Type type;
			public Boiler.Data.Flags flags;

			public IMaterial.Handle h_material_surface;
			public IMaterial.Handle h_material_heater;
		}

		[IComponent.Data(Net.SendType.Unreliable, region_only: true)]
		public struct State(): IComponent
		{
			[Flags]
			public enum Flags: uint
			{
				None = 0u,
			}

			public Boiler.State.Flags flags;


			public float safety_valve_ratio = 1.00f;
			public float output_valve_ratio = 1.00f;
			public float input_valve_ratio = 1.00f;

			public Pressure steam_pressure_bar = Pressure.Ambient; //.Ambient;

			public Amount water_moles;
			public Temperature water_temperature = Temperature.Ambient;

			public Amount steam_moles;
			public Temperature steam_temperature = Temperature.Ambient;

			public Temperature temperature_surface = Temperature.Ambient;
			public Temperature temperature_heater = Temperature.Ambient;

			[Save.Ignore, Net.Ignore] public float t_next_steam;
			[Save.Ignore, Net.Ignore] public float t_next_exhaust;
			[Save.Ignore, Net.Ignore] public float t_next_tick;

			//[Save.Ignore] public float _tank_temperature;
			//[Save.Ignore] public double tank_pressure;
		}

		public struct ConfigureRPC: Net.IRPC<Boiler.State>
		{
			public float? safety_valve_ratio;
			public float? output_valve_ratio;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Boiler.State data)
			{
				ref var region = ref rpc.entity.GetRegion();

				var sync = false;

				sync |= data.safety_valve_ratio.TrySet(this.safety_valve_ratio, 0.00f, 1.00f);
				sync |= data.output_valve_ratio.TrySet(this.output_valve_ratio, 0.00f, 1.00f);

				if (sync)
				{
					data.Sync(rpc.entity, true);
				}
			}
#endif
		}


		//public const float water_critical_point = 647.096f;
		//public const float ambient_temperature = 300.00f;
		//public const float gas_constant = 8.31446261815324f;
		//public const float absolute_zero = 273.15f;
		////public const float ambient_pressure = 101.325f; // kPa
		//public const float water_molar_mass = 0.0180153f;

		//// p = (n * R * T) / V
		//public static double CalculatePressure(float n_moles, float t_kelvin, float v_cb_meters)
		//{
		//	return ((n_moles * gas_constant * t_kelvin) / v_cb_meters);
		//}

		[ISystem.VeryEarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void Update2(Entity entity,
		[Source.Owned] in Boiler.Data boiler, [Source.Owned] ref Boiler.State boiler_state,
		[Source.Owned] in Burner.Data burner, [Source.Owned] ref Burner.State burner_state)
		{
			//return;

			////boiler_state.water_temperature = Maths.Lerp(boiler_state.water_temperature, ambient_temperature, 0.01f);
			////boiler_state.steam_temperature = Maths.Lerp(boiler_state.water_temperature, boiler_state.steam_temperature, boiler_state.fill_ratio);

			////Maths.Mix(boiler_state.water_temperature, boiler_state.steam_temperature,)

			////var steam_pressure = Phys.ToPascal(boiler_state.steam_pressure_bar);
			//var steam_pressure = boiler_state.steam_pressure_bar;
			//if (boiler_state.steam_moles > 0.00f)
			//{
			//	ref var props = ref Water.TemperatureToProperties(boiler_state.steam_temperature);

			//	var steam_pressure_vent_threshold = boiler.pressure_max * boiler_state.safety_valve_ratio;
			//	//var overflow_ratio = Maths.OverflowRatio(steam_pressure, steam_pressure_vent_threshold);
			//	//if (overflow_ratio > 0.10f)
			//	if (steam_pressure > steam_pressure_vent_threshold)
			//	{
			//		//var steam_volume_a = boiler_state.steam_volume;
			//		//var steam_volume_b = props.specific_volume_gas * boiler_state.steam_moles * Water.molar_mass

			//		var steam_moles_a = boiler_state.steam_moles;
			//		//var steam_moles_b = Phys.CalculateGasMoles(boiler.reservoir_volume, boiler_state.steam_temperature, Math.Max(steam_pressure - Pressure.bar(0.01f), steam_pressure_vent_threshold) - Phys.reference_atmospheric_pressure_bar);
			//		Phys.IdealGasLaw(MathF.Max(steam_pressure - Pressure.bar(0.01f), steam_pressure_vent_threshold) - Phys.atmospheric_pressure_kordel, boiler.reservoir_volume, out var steam_moles_b, boiler_state.steam_temperature);

			//		//var steam_moles_b = Phys.CalculateGasMoles(boiler.tank_volume, boiler_state.steam_temperature, steam_pressure - Phys.reference_atmospheric_pressure);
			//		var steam_moles_vented = Maths.Min(0.30f, (steam_moles_a - steam_moles_b));

			//		boiler_state.steam_moles -= steam_moles_vented;

			//		//if (App.CurrentTick % 50 == 0)
			//		//{
			//		//	App.WriteLine($"{steam_moles_a:0.00} -> {steam_moles_b:0.00} -> {steam_moles_vented:0.00}");
			//		//}
			//	}



			//	//if (boiler_state.tank_pressure > props.pressure * 0.10f)
			//	//{
			//	//	var steam_moles = boiler_state.steam_moles * 0.01f;

			//	//	boiler_state.steam_moles -= steam_moles;
			//	//	boiler_state.water_moles += steam_moles;
			//	//}

			//	//boiler_state.steam_volume = Maths.Lerp(boiler_state.steam_volume,  props.specific_volume_gas * boiler_state.steam_moles * Water.molar_mass, 0.50f);
			//	//steam_pressure = Maths.Lerp(steam_pressure, Phys.reference_atmospheric_pressure + Phys.CalculateGasPressure(boiler.reservoir_volume, boiler_state.steam_temperature, boiler_state.steam_moles), 0.50f);

			//	Phys.IdealGasLaw(out var pressure_new, boiler.reservoir_volume, boiler_state.steam_moles, boiler_state.steam_temperature);
			//	steam_pressure = Maths.Lerp(steam_pressure, Phys.atmospheric_pressure_kordel + pressure_new, 0.50f);
			//}
			//else
			//{
			//	//boiler_state.steam_volume = 0.00f;

			//	boiler_state.steam_temperature = Phys.ambient_temperature;
			//	steam_pressure = Phys.atmospheric_pressure_kordel;
			//}

			//boiler_state.steam_pressure_bar = steam_pressure;

			////boiler_state.fill_ratio_raw = Maths.Normalize(boiler_state.steam_volume, boiler.tank_volume);
			////boiler_state.fill_ratio = Maths.Clamp01(boiler_state.fill_ratio_raw);
			////boiler_state.tank_pressure = Maths.Lerp(Phys.reference_atmospheric_pressure, boiler_state.steam_pressure, boiler_state.fill_ratio);

			////boiler_state._tank_temperature = Maths.Lerp(ambient_temperature, boiler_state.steam_temperature, boiler_state.fill_ratio);


			//var input_moles = Maths.Min(boiler_state.water_moles, (0.10f / Water.molar_mass) * App.fixed_update_interval_s);
			////var power = burner_state._available_power;

			////if (input_moles > 0.00f && power > 0.00f)
			//if (input_moles > 0.00f)
			//{
			//	var input_mass = input_moles * Water.molar_mass;
			//	//boiler_state.water_moles -= input_moles;

			//	////var temperature = burner_state.current_temperature * boiler.efficiency;

			//	////var energy_start = boiler_state.water_temperature * Phys.water_specific_heat * input_mass;
			//	////var energy_target = temperature * Phys.water_specific_heat * input_mass;
			//	////var energy_required = Math.Max(energy_target - energy_start, 0.00);
			//	////var energy_available = power * App.fixed_update_interval_s * boiler.efficiency; // * 0.50f; // steamboiler.exchange_efficiency;

			//	//var t = Maths.Clamp((float)(energy_available / energy_required), 0.00f, 1.00f - boiler.blowdown_rate);

			//	//var temperature_final = Maths.Lerp(boiler_state.water_temperature, temperature, t);


			//	//ref var props = ref Water.TemperatureToProperties(temperature_final); //lut[Maths.Clamp((int)Maths.KelvinToCelsius(temperature_final), 0, lut.Length - 1)];

			//	//var water_moles = input_moles;
			//	//var steam_moles = 0.00f;

			//	//var pressure_max = props.pressure; // Maths.Lerp(ambient_pressure, props.pressure, steamboiler_state._fill_ratio);
			//	//if (boiler_state.tank_pressure <= pressure_max)
			//	//{
			//	//	steam_moles = water_moles * t;
			//	//	water_moles -= steam_moles;
			//	//}

			//	////boiler_state._pressure_max = pressure_max;

			//	//// use latent heat for this

			//	//if (water_moles > 0.00f)
			//	//{
			//	//	//AddHeated(ref steamboiler_state.water_moles, ref steamboiler_state.water_temperature, water_moles, temperature_final);
			//	//	boiler_state.water_temperature = Maths.SumWeighted(boiler_state.water_temperature, temperature_final, boiler_state.water_moles, water_moles);
			//	//	boiler_state.water_moles += water_moles;
			//	//}

			//	//if (steam_moles > 0.00f)
			//	//{
			//	//	//AddHeated(ref steamboiler_state.steam_moles, ref steamboiler_state.steam_temperature, steam_moles, temperature_final);
			//	//	boiler_state.steam_temperature = Maths.SumWeighted(boiler_state.steam_temperature, temperature_final, boiler_state.steam_moles, steam_moles);
			//	//	boiler_state.steam_moles += steam_moles;
			//	//}
			//}

		}

		[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void Update(ISystem.Info info, Entity entity, ref Region.Data region, ref XorRandom random,
		[Source.Owned] in Transform.Data transform,
		[Source.Owned] in Boiler.Data boiler, [Source.Owned] ref Boiler.State boiler_state,
		[Source.Owned] in Burner.Data burner, [Source.Owned] ref Burner.State burner_state,

		[Source.Owned, Pair.Tag("exhaust")] in Air.Vent.Data vent_exhaust,

		[HasTag<Boiler.Data>("damaged", true, Source.Modifier.Owned)] bool damaged)
		{

			//return;
			ref var mat_surface = ref boiler.h_material_surface.GetData();
			ref var mat_heater = ref boiler.h_material_heater.GetData();

	
//			if (mat_surface.IsNotNull() && mat_heater.IsNotNull())
//			{
//				ref var substance_surface = ref mat_surface.substance.GetRefOrNull();
//				ref var substance_heater = ref mat_heater.substance.GetRefOrNull();

//				// https://www.engineeringtoolbox.com/convective-heat-transfer-d_430.html
//				// https://www.engineeringtoolbox.com/convective-heat-source-d_1007.html
//				// https://www.engineeringtoolbox.com/heat-transfer-coefficients-exchangers-d_450.html
//				// https://www.omnicalculator.com/physics/lmtd
//				// https://www.omnicalculator.com/physics/van-der-waals-equation
//				// https://www.omnicalculator.com/physics/water-heating
//				// https://www.omnicalculator.com/physics/effectiveness-ntu
//				// https://www.omnicalculator.com/physics/mixed-air-temperature
//				// https://www.omnicalculator.com/physics/mixed-air
//				// https://www.omnicalculator.com/physics/thermodynamic-processes
//				// https://www.omnicalculator.com/physics/isentropic-flow
//				// https://www.omnicalculator.com/physics/charles-law
//				// https://www.omnicalculator.com/physics/boyles-law
//				// https://www.omnicalculator.com/physics/gay-lussacs-law

//				if (substance_surface.IsNotNull() && substance_heater.IsNotNull())
//				{
//					var heat_transfer_speed_mult = 1.00f;

//					var heat_coefficient_heater = ((float)((boiler.area_heater * substance_surface.thermal_conductivity) * (Maths.Normalize(substance_surface.thermal_conductivity * substance_surface.thermal_conductivity, burner_state.available_power))));
//					var heat_coefficient_surface = (substance_surface.thermal_conductivity + 0.02897f) * 0.50f;

//					var heat_transfer_surface = heat_coefficient_surface * boiler.area_surface * (boiler_state.temperature_surface - Region.ambient_temperature) * (1.00f - boiler.insulation);
//					var heat_transfer_heater = heat_coefficient_heater * boiler.area_heater * (boiler_state.temperature_heater - vent_exhaust.blob.temperature);
//					var heat_transfer_self = (substance_surface.thermal_conductivity * (vent_exhaust.blob.temperature - boiler_state.temperature_surface));

//					var heat_transfer_water = Maths.Normalize(Water.thermal_conductivity * boiler.area_surface * (boiler_state.water_temperature - boiler_state.temperature_surface), boiler.tank_thickness);
//					var heat_transfer_steam = Maths.Normalize(Phys.steam_thermal_conductivity * boiler.area_surface * (boiler_state.steam_temperature - boiler_state.temperature_surface), boiler.tank_thickness);

//					var dt = info.DeltaTime;

//					boiler_state.temperature_surface -= Maths.Normalize((heat_transfer_surface - heat_transfer_self) * dt * heat_transfer_speed_mult, boiler.mass_tank * substance_surface.specific_heat);
//					boiler_state.temperature_heater -= Maths.Normalize((heat_transfer_heater - heat_transfer_self + heat_transfer_surface) * dt * heat_transfer_speed_mult, boiler.mass_tank * substance_surface.specific_heat);

//					var water_kg = Water.MolToKg(boiler_state.water_moles);
//					var steam_kg = Water.MolToKg(boiler_state.steam_moles);

//					boiler_state.water_temperature -= Maths.Normalize((heat_transfer_water - heat_transfer_self + heat_transfer_surface) * dt * heat_transfer_speed_mult, water_kg * Water.specific_heat);
//					boiler_state.steam_temperature -= Maths.Normalize((heat_transfer_steam - heat_transfer_self + heat_transfer_surface) * dt * heat_transfer_speed_mult, steam_kg * 2135.00f);

//					// latent heat
//					if (boiler_state.water_moles > 0.00f && boiler_state.water_temperature > Water.boiling_point)
//					{
//						var water_temperature_extra = boiler_state.water_temperature - Water.boiling_point;
//						//App.WriteLine(water_temperature_extra);

//						var energy_vapor = water_temperature_extra * (water_kg * Water.specific_heat);
//						//App.WriteLine(energy_vapor);

//						var steam_kg_produced = (float)(energy_vapor / Water.latent_heat_of_vaporization);
//						var steam_mol_produced = Maths.Min(boiler_state.water_moles, (float)Water.KgToMol(steam_kg_produced));

//						boiler_state.water_temperature = Water.boiling_point;
//						boiler_state.steam_temperature = Maths.SumWeighted(Water.boiling_point + 4.00f, boiler_state.steam_temperature, steam_mol_produced, boiler_state.steam_moles);

//						boiler_state.water_moles -= steam_mol_produced;
//						boiler_state.steam_moles += steam_mol_produced;

//						//App.WriteLine(steam_kg_produced);
//					}

//					if (damaged)
//					{
//						if (boiler_state.steam_moles > 1.00f && region.GetCurrentTick() % 17 == 0)
//						{
//							var steam_leak_amount = Maths.Min(boiler_state.steam_moles, 2.00f) * 0.90f;


//#if CLIENT
//							var steam_particle_mult = Maths.Max(0.30f, steam_leak_amount / 2.00f);

//							Particle.Spawn(ref region, new Particle.Data()
//							{
//								texture = Boiler.texture_smoke,
//								lifetime = random.NextFloatRange(3.00f, 7.00f) * steam_particle_mult,
//								pos = transform.position + random.NextVector2(1.00f),
//								vel = new Vector2(random.NextFloatRange(-1, 1), -random.NextFloatRange(0.00f, 2.00f)),
//								force = random.NextUnitVector2Range(0.00f, 2.00f) + new Vector2(random.NextFloatRange(0, 8), -random.NextFloatRange(5, 7)),
//								fps = random.NextByteRange(3, 8),
//								frame_count = 64,
//								frame_count_total = 64,
//								frame_offset = random.NextByteRange(0, 64),
//								scale = random.NextFloatRange(0.60f, 0.80f),
//								rotation = random.NextFloat(4.00f),
//								angular_velocity = random.NextFloat(1.00f),
//								growth = random.NextFloatRange(0.05f, 0.10f) * steam_particle_mult,
//								drag = random.NextFloatRange(0.05f, 0.07f),
//								color_a = random.NextColor32Range(new Color32BGRA(200, 255, 255, 255), new Color32BGRA(140, 240, 240, 240)).WithAlphaMult(0.70f + (0.30f * steam_particle_mult)),
//								color_b = new Color32BGRA(0, 240, 240, 240),
//							});

//#endif
//							boiler_state.steam_moles -= steam_leak_amount;
//						}
//					}


//					//if (boiler_state.fill_type == State.FillType.Water)
//					//{
//					//	ref var props = ref Water.TemperatureToProperties(boiler_state.water_temperature);

//					//	if (boiler_state.pressure > Units.kPa(props.pressure))
//					//	{
//					//		var steam_moles = boiler_state.steam_quantity * 0.01f;

//					//		boiler_state.steam_quantity -= steam_moles;
//					//		boiler_state.water_quantity += steam_moles;
//					//	}
//					//	else
//					//	{
//					//		var water_moles = boiler_state.water_quantity * 0.001f;

//					//		boiler_state.steam_quantity += water_moles;
//					//		boiler_state.water_quantity -= water_moles;
//					//	}

//					//	//steamboiler_state._steam_volume = (steamboiler_state._tank_pressure < props.pressure ? props.specific_volume_gas : props.specific_volume_liquid) * steamboiler_state.steam_moles * water_molar_mass;
//					//	boiler_state.steam_volume = Maths.Lerp(boiler_state.steam_volume, props.specific_volume_gas * boiler_state.steam_mass, 0.20f);
//					//	//boiler_state.steam_pressure = Maths.Lerp(boiler_state.steam_pressure, CalculatePressure(steamboiler_state.steam_moles, steamboiler_state.steam_temperature, steamboiler.volume), 0.20f);
//					//	boiler_state.steam_pressure = Maths.Lerp(boiler_state.steam_pressure, Units.kPa(props.pressure), 0.20f);


//					//	boiler_state.fill_ratio_raw = Maths.Normalize(boiler_state.steam_volume, boiler.tank_volume);
//					//	boiler_state.fill_ratio = Maths.Clamp01(boiler_state.fill_ratio_raw);
//					//	boiler_state.pressure = Maths.Lerp(Phys.reference_atmospheric_pressure, boiler_state.steam_pressure, boiler_state.fill_ratio);
//					//}
//				}
//			}
		
		}

		[IEvent.Data]
		public partial struct ExplodeEvent(): IEvent
		{
			public float power;
		}

		public static readonly Texture.Handle texture_smoke = "BiggerSmoke_Light";

		//[ISystem.AddFirst(ISystem.Mode.Single, ISystem.Scope.Region), HasTag<SteamEngine.Data>("damaged", true, Source.Modifier.Owned)]
		[ISystem.Event<Boiler.ExplodeEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnExplode(ISystem.Info info, ref Region.Data region, ref XorRandom random, Entity entity, ref Boiler.ExplodeEvent data,
		[Source.Owned] in Transform.Data transform,
		[Source.Owned] ref Boiler.Data boiler, [Source.Owned] ref Boiler.State boiler_state)
		{
			var max_radius = Maths.Clamp(data.power, 6.00f, 100.00f);

			App.WriteLine("boiler damaged");
#if CLIENT
			Sound.Play("explosion.steam.01", transform.position, volume: 1.50f, size: 1.00f, priority: 0.70f, dist_multiplier: 2.20f);
			Shake.Emit(ref region, transform.position, 1.00f, 1.00f, max_radius * 2.00f);

			var smoke_count = (int)Maths.Clamp(data.power, 10.00f, 50.00f);
			for (var i = 0; i < smoke_count; i++)
			{
				Particle.Spawn(ref region, new Particle.Data()
				{
					texture = texture_smoke,
					lifetime = random.NextFloatRange(12.00f, 30.00f),
					pos = transform.position + random.NextVector2(0.40f),
					vel = random.NextUnitVector2Range(0.00f, 15.00f) + new Vector2(random.NextFloatRange(-40, 40), random.NextFloatRange(-30, 10)),
					force = random.NextUnitVector2Range(0.00f, 2.00f) + new Vector2(random.NextFloatRange(0, 2), -random.NextFloatRange(1, 2)),
					fps = random.NextByteRange(3, 8),
					frame_count = 64,
					frame_count_total = 64,
					frame_offset = random.NextByteRange(0, 64),
					scale = random.NextFloatRange(1.50f, 2.00f),
					//rotation = random.NextFloat(4.00f),
					angular_velocity = random.NextFloat(1.00f),
					growth = random.NextFloatRange(0.05f, 0.20f),
					drag = random.NextFloatRange(0.05f, 0.10f),
					color_a = random.NextColor32Range(new Color32BGRA(200, 255, 255, 255), new Color32BGRA(140, 240, 240, 240)),
					color_b = new Color32BGRA(0, 240, 240, 240),
					stretch = new Vector2(random.NextFloatRange(0.40f, 1.00f), random.NextFloatRange(0.40f, 1.00f)),
					face_dir_ratio = random.NextFloatRange(0.00f, 1.00f),
				});
			}
#endif

			boiler_state.steam_moles *= 0.10f;

#if SERVER
			entity.AddTag<Boiler.Data>("damaged");
#endif
		}

		[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void UpdateSteamEngine(ISystem.Info info, Entity entity, ref XorRandom random,
		[Source.Owned] in Transform.Data transform,
		[Source.Owned] ref Boiler.Data boiler, [Source.Owned] ref Boiler.State boiler_state,
		[Source.Owned] ref Axle.Data axle, [Source.Owned] ref Axle.State axle_state,
		[Source.Owned] ref SteamEngine.Data steam_engine, [Source.Owned] ref SteamEngine.State steam_engine_state,
		[HasTag<SteamEngine.Data>("damaged", true, Source.Modifier.Owned)] bool damaged)
		{
			if (!damaged)
			{
				steam_engine_state.piston_displacement = Maths.Repeat(steam_engine.bore_depth * (-axle_state.rotation / MathF.Tau), 1.00f);
				var steam_pressure = boiler_state.steam_pressure_bar;

				var bore_area = Area.Circle(steam_engine.bore_diameter * 0.50f);
				var cylinder_volume = Volume.Cylinder(bore_area, steam_engine.bore_depth);

				var cylinder_force = (float)((steam_pressure - Pressure.Ambient).Pa() * bore_area);

				var axle_mass = axle.mass + steam_engine.linkage_mass;

				var displacement_ratio = Maths.Normalize(steam_engine_state.piston_displacement, steam_engine.bore_depth);

				var stroke = Maths.Max(0.10f, ((displacement_ratio + 0.01f) % 0.50f) * 2.00f);
				var moles_used = (boiler_state.steam_moles * Maths.Normalize01(cylinder_volume, boiler.reservoir_volume)) * stroke * info.DeltaTime;
				boiler_state.steam_moles -= moles_used;
				boiler_state.water_moles += moles_used * steam_engine.condensation_ratio;

				steam_engine_state.force_current = cylinder_force * stroke;

				var acc = Maths.Normalize(cylinder_force * stroke, axle_mass) * steam_engine.bore_depth * 3.00f;
				steam_engine_state.speed_current = Maths.Lerp(steam_engine_state.speed_current, acc, 0.01f);
				axle_state.ApplyTorque(steam_engine.force, steam_engine_state.speed_current);

#if SERVER
				if (false && axle_state.angular_velocity.Abs() > steam_engine.speed_max && random.NextBool(((axle_state.angular_velocity.Abs() - steam_engine.speed_max) * 0.01f) * stroke))
				{
					var damage = ((steam_engine_state.force_current - steam_engine.force).Abs() * 0.20f) * random.NextFloatRange(0.90f, 1.50f);

					Damage.Hit(ent_attacker: entity, ent_owner: default, ent_target: entity, position: transform.LocalToWorld(steam_engine.piston_offset) + random.NextUnitVector2Range(0.00f, 1.50f), velocity: random.NextUnitVector2Range(0.50f, 1.50f) * steam_engine_state.speed_current, normal: random.NextUnitVector2Range(0.50f, 1.50f),
						damage_integrity: damage,
						damage_durability: damage,
						damage_terrain: damage,
						damage_type: Damage.Type.Crush,
						flags: Damage.Flags.No_Loot_Pickup,
						target_material_type: Material.Type.Metal);
				}
#endif
			}

			if (info.WorldTime >= steam_engine_state.next_tick)
			{
				steam_engine_state.next_tick = info.WorldTime + SteamEngine.update_interval;
			}
		}

#if SERVER
		[ISystem.Event<Health.PostDamageEvent>(ISystem.Mode.Single, ISystem.Scope.Region), HasTag<Boiler.Data>("damaged", false, Source.Modifier.Owned)]
		public static void OnPostDamage(ISystem.Info info, Entity entity, ref XorRandom random, ref Health.PostDamageEvent data, 
		[Source.Owned] ref Health.Data health, [Source.Owned] in Transform.Data transform,
		[Source.Owned] ref Boiler.Data boiler, [Source.Owned] ref Boiler.State boiler_state)
		{
			var modifier = Maths.InvLerp01((boiler.pressure_max) * 0.80f, (boiler.pressure_max) * 1.50f,  boiler_state.steam_pressure_bar);
			if (modifier > 0.30f)
			{
				var health_min = Maths.Min(health.integrity, health.durability);
				var chance = ((1.00f - (health_min * 1.50f).Clamp01()) * modifier * boiler.burst_chance_modifier);

				//App.WriteLine($"boom {chance}");
				if (health_min <= boiler.burst_threshold && random.NextBool(chance))
				{
					var power = Maths.Sqrt(boiler_state.steam_pressure_bar);

					var ev = new Boiler.ExplodeEvent()
					{
						power = power
					};
					entity.TriggerEventDeferred(ev, sync: true);
				}
			}
		}
#endif

#if CLIENT
		[ISystem.Render(ISystem.Mode.Single, ISystem.Scope.Region), HasTag<Boiler.Data>("damaged", true, Source.Modifier.Owned)]
		public static void RenderBurst([Source.Owned] in Transform.Data transform, [Source.Owned] in Animated.Renderer.Data renderer, [Source.Owned] ref Boiler.Data boiler)
		{
			Animated.Renderer.Draw(transform, new()
			{
				sprite = boiler.sprite_burst,
				z = renderer.z + 0.01f,
				rotation = 0.00f,
				scale = Vector2.One,
				offset = boiler.burst_offset
			});
		}

		[ISystem.VeryLateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void UpdateSoundExhaust(ISystem.Info info, ref Region.Data region, ref XorRandom random,
		[Source.Owned] in Transform.Data transform,
		[Source.Owned] ref Boiler.Data boiler, [Source.Owned] ref Boiler.State boiler_state,
		[Source.Owned, Pair.Component<Boiler.State>] ref Sound.Emitter sound_emitter)
		{
			var modifier_a = 0.00f;
			var modifier_b = 0.00f;
			var size = 0.50f;

			//var steam_pressure = Phys.ToPascal(boiler_state.steam_pressure_bar);
			var steam_pressure = (boiler_state.steam_pressure_bar);
			if (boiler_state.steam_moles > 0.00f)
			{
				var steam_pressure_vent_threshold = boiler.pressure_max * boiler_state.safety_valve_ratio;
				if (steam_pressure > steam_pressure_vent_threshold)
				{
					var steam_moles_a = boiler_state.steam_moles;
					//var steam_moles_b = Phys.CalculateGasMoles(boiler.reservoir_volume, boiler_state.steam_temperature, steam_pressure_vent_threshold - Phys.reference_atmospheric_pressure);
					//var steam_moles_c = Phys.CalculateGasMoles(boiler.reservoir_volume, boiler_state.steam_temperature, Math.Max(steam_pressure - Units.bar(0.01f), steam_pressure_vent_threshold) - Phys.reference_atmospheric_pressure);


					Phys.IdealGasLaw(steam_pressure_vent_threshold - Phys.atmospheric_pressure_kordel, boiler.reservoir_volume, out var steam_moles_b, boiler_state.steam_temperature);
					Phys.IdealGasLaw(MathF.Max(steam_pressure - Units.bar(0.01f), steam_pressure_vent_threshold) - Phys.atmospheric_pressure_kordel, boiler.reservoir_volume, out var steam_moles_c, boiler_state.steam_temperature);

					var steam_moles_vented = Maths.Min(0.30f, (steam_moles_a - steam_moles_c));

					modifier_a = Maths.Clamp(Maths.OverflowRatio(steam_moles_a, steam_moles_b), 0.00f, 1.50f);
					modifier_b = Maths.Clamp((steam_pressure) * 0.20f, 0.00f, 1.50f).Pow2();

					size = Maths.Clamp(steam_moles_vented * 4.00f, 0.30f, 1.50f);

					//App.WriteLine(steam_moles_vented);
				}
			}

			sound_emitter.volume_mult = Maths.Lerp2(sound_emitter.volume_mult, Maths.Clamp(Maths.Avg(modifier_a, modifier_b) * 0.15f, 0.00f, 1.00f), 0.10f, 0.40f);
			sound_emitter.pitch_mult = Maths.Lerp2(sound_emitter.pitch_mult, 0.30f + ((Maths.Avg(modifier_a, modifier_b) * 0.60f)), 0.008f, 0.10f); // 0.60f + Maths.Clamp(delta * 0.10f, 0.00f, 0.50f);

			if (modifier_a > 0.00f && info.WorldTime >= boiler_state.t_next_exhaust && Camera.IsVisible(Camera.CullType.Rect2x, transform.position))
			{
				boiler_state.t_next_exhaust = info.WorldTime + 0.10f;

				Particle.Spawn(ref region, new Particle.Data()
				{
					texture = Boiler.texture_smoke,
					lifetime = random.NextFloatRange(0.50f, 1.00f) * size,
					pos = transform.LocalToWorld(boiler.exhaust_offset) + random.NextVector2(0.20f),
					vel = transform.LocalToWorld(new Vector2(random.NextFloatRange(-0.50f, 0.50f), -random.NextFloatRange(1.50f, 4.50f)), position: false) * modifier_b,
					//vel = steam_engine.smoke_direction * (-3.00f - Maths.Clamp(delta, 0.00f, 3.00f)) * random.NextFloatRange(0.90f, 1.10f),
					force = new Vector2(0.30f, -0.40f),
					fps = random.NextByteRange(10, 15),
					frame_count = 64,
					frame_count_total = 64,
					frame_offset = random.NextByteRange(0, 64),
					scale = random.NextFloatRange(0.20f, 0.40f) * size * 0.35f,
					rotation = random.NextFloat(10.00f),
					angular_velocity = random.NextFloat(3.00f),
					growth = Maths.Clamp(0.30f * size * (1.00f + (modifier_b * 2)).Pow2(), 0.30f, 1.70f),
					drag = random.NextFloatRange(0.01f, 0.02f),
					color_a = new Color32BGRA(200, 240, 240, 240),
					color_b = new Color32BGRA(0, 240, 240, 240),
					//stretch = new Vector2(2.50f, 1.00f),
					//face_dir_ratio = 1.00f
				});
			}
		}

		public static float knob_original_rot;
		public static Vector2 knob_original_pos;

		// holdable rotary knob
		public static bool DrawKnob(Utf8String identifier, ref float value, float min, float max, Vector2 size, float step = 0.01f)
		{
			var value_old = value;

			using (var group = GUI.Group.New(size: size))
			{
				var rect = group.GetInnerRect();
				group.DrawBackground(GUI.tex_frame);

				var pressed = GUI.ButtonBehavior(identifier, rect, out var hovered, out var held);

				var angle_min = -MathF.PI * 0.75f;
				var angle_max = +MathF.PI * 0.75f;

				var ratio = Maths.InvLerp01(min, max, value);
				var rot = Maths.Lerp01(angle_min, angle_max, ratio);

				if (held)
				{
					//var rot_mouse = Maths.NormalizeAngle(dir.GetAngleRadians());

					if (GUI.GetMouse().GetKeyDown(Mouse.Key.Left))
					{
						knob_original_rot = rot;
						knob_original_pos = GUI.GetMousePosition();
					}

					var dir_a = (knob_original_pos - rect.GetPosition()).GetNormalized();
					var dir_b = (GUI.GetMousePosition() - rect.GetPosition()).GetNormalized();
					var delta = Maths.DeltaAngle(dir_a.GetAngleRadians(), dir_b.GetAngleRadians());

					rot = Maths.Clamp(knob_original_rot + delta, angle_min, angle_max);

					ratio = Maths.InvLerp01(angle_min, angle_max, rot);
					value = Maths.Lerp01(min, max, ratio);

					GUI.DrawTextCentered($"{knob_original_rot:0.00}\n{rot:0.00}\n{ratio:0.00}\n{value:0.00}\ndelta: {delta:0.00}\n{Maths.NormalizeAngle2(dir_b.GetAngleRadians()):0.00}", rect.GetPosition(), layer: GUI.Layer.Foreground);

					//App.WriteLine(dir);
				}

				GUI.DrawSprite2(GUI.tex_arrow, rect, GUI.Layer.Window, rotation: rot);

				if (held)
				{
					GUI.SetCursor(App.CursorType.Drag, 100);
				}
				else if (hovered)
				{
					GUI.SetCursor(App.CursorType.Hand, 100);
				}

			}

			return value != value_old;
		}

		public struct BoilerGUI: IGUICommand
		{
			public Entity ent_boiler;

			public Boiler.Data boiler;
			public Boiler.State boiler_state;

			public Burner.Data burner;
			public Burner.State burner_state;

			public void Draw()
			{
				//var steam_pressure = Phys.ToPascal(boiler_state.steam_pressure_bar);
				var steam_pressure = (boiler_state.steam_pressure_bar);

				using (var window = GUI.Window.Interaction("Boiler"u8, this.ent_boiler))
				{
					this.StoreCurrentWindowTypeID(order: -100);
					if (window.show)
					{
						ref var region = ref Client.GetRegionCommon();
						var h_character = Client.GetCharacterHandle();

						using (var group_top = GUI.Group.New(size: new Vector2(192, 96)))
						{
							//using (var group = GUI.Group.New(size: new Vector2(GUI.RmX, 96)))
							//{
							//	using (var group_left = GUI.Group.New(size: new(0, GUI.RmY)))
							//	{
							//		using (GUI.Group.New(size: new(GUI.RmX, GUI.RmY - 24)))
							//		{
							//			using (GUI.Group.New(size: new(GUI.RmX, GUI.RmY - 24)))
							//			{
							//				// TODO: update this to use vents
							//				GUI.DrawTemperatureRange(this.boiler_state.temperature_heater, 300, Maths.CelsiusToKelvin(2000), size: new(24, GUI.RmY),
							//											title: "Heater Temperature", label_a: "Exhaust: ", label_b: "Current: ");

							//				GUI.SameLine();

							//				GUI.DrawTemperatureRange(this.boiler_state.temperature_surface, this.boiler_state.temperature_heater, Maths.CelsiusToKelvin(2000), size: new(24, GUI.RmY),
							//											title: "Surface Temperature", label_a: "Heater: ", label_b: "Current: ");

							//				GUI.SameLine();

							//				GUI.DrawTemperatureRange(this.boiler_state.water_temperature, Water.boiling_point, Maths.CelsiusToKelvin(374), new Vector2(24, GUI.RmY),
							//											title: "Water Temperature", label_a: "Boiling Point: ", label_b: "Current: ");
							//			}

							//			if (GUI.SliderFloat("Output Valve", ref this.boiler_state.output_valve_ratio, 0.00f, 1.00f, size: new(GUI.RmX, 24), color_frame: GUI.col_output))
							//			{
							//				var rpc = new Boiler.ConfigureRPC()
							//				{
							//					output_valve_ratio = this.boiler_state.output_valve_ratio
							//				};
							//				rpc.Send(this.ent_boiler);
							//			}

							//			//if (GUI.SliderFloat("Safety Valve", ref this.boiler_state.relief_valve_ratio, 0.00f, 1.00f, size: new(GUI.RmX, 24), color_frame: GUI.col_button_ok))
							//			//{
							//			//	var rpc = new Boiler.ConfigureRPC()
							//			//	{
							//			//		relief_valve_ratio = this.boiler_state.relief_valve_ratio
							//			//	};
							//			//	rpc.Send(this.ent_boiler);
							//			//}
							//		}

							//		GUI.SameLine();

							//		if (GUI.SliderFloat("Safety Valve", ref this.boiler_state.safety_valve_ratio, 0.00f, 1.00f, size: new(24, GUI.RmY - 24), vertical: true, color_frame: GUI.col_button_yellow))
							//		{
							//			var rpc = new Boiler.ConfigureRPC()
							//			{
							//				safety_valve_ratio = this.boiler_state.safety_valve_ratio
							//			};
							//			rpc.Send(this.ent_boiler);
							//		}

							//		GUI.DrawHorizontalGaugeMulti(values: [this.boiler_state.water_moles, this.boiler_state.steam_moles], colors: [GUI.col_input.WithColorMult(0.70f), GUI.col_white.WithColorMult(0.75f)], max: this.boiler_state.water_moles + this.boiler_state.steam_moles, size: new(GUI.RmX, 24));
							//	}


							//	if (GUI.IsItemHovered())
							//	{
							//		using (GUI.Tooltip.New(size: new(192, 0)))
							//		{
							//			GUI.Title("Steam Boiler");
							//			GUI.LabelShaded("Water:", $"{Water.MolToKg(this.boiler_state.water_moles):0.00} kg ({Maths.KelvinToCelsius(this.boiler_state.water_temperature):0.00}°C)");
							//			GUI.LabelShaded("Steam:", $"{Water.MolToKg(this.boiler_state.steam_moles):0.00} kg ({Maths.KelvinToCelsius(this.boiler_state.steam_temperature):0.00}°C)");
							//		}
							//	}



							//	GUI.SameLine();

							//	//GUI.DrawPressureRange(steam_pressure, this.boiler.pressure_max * this.boiler_state.safety_valve_ratio, this.boiler.pressure_max, new Vector2(24, GUI.RmY));

							//	//GUI.SameLine();

							//	GUI.DrawGauge2((float)((steam_pressure - Phys.atmospheric_pressure_kordel) / (this.boiler.pressure_max - Phys.atmospheric_pressure_kordel)), 0.00f, 1.00f, size: new(GUI.RmY));

							//	if (GUI.IsItemHovered())
							//	{
							//		using (GUI.Tooltip.New(size: new(192, 0)))
							//		{
							//			GUI.Title("Steam Boiler");

							//			GUI.LabelShaded("Output Valve:", $"{this.boiler_state.output_valve_ratio:P0}");
							//			GUI.LabelShaded("Safety Valve:", $"{this.boiler_state.safety_valve_ratio:P0}");

							//			GUI.NewLine();

							//			GUI.LabelShaded("Pressure:", $"{this.boiler_state.steam_pressure_bar:0.00}/{(this.boiler.pressure_max):0.00} bar", color_b: Color32BGRA.FromHSV((1.00f - Maths.Normalize01(this.boiler_state.steam_pressure_bar, (this.boiler.pressure_max))) * 2, 1.00f, 1.00f));

							//			GUI.NewLine();

							//			GUI.LabelShaded("Water:", $"{Water.MolToKg(this.boiler_state.water_moles):0.00} kg ({Maths.KelvinToCelsius(this.boiler_state.water_temperature):0.00}°C)");
							//			GUI.LabelShaded("Steam:", $"{Water.MolToKg(this.boiler_state.steam_moles):0.00} kg ({Maths.KelvinToCelsius(this.boiler_state.steam_temperature):0.00}°C)");
							//		}
							//	}

							//	//GUI.SameLine();

							//	//GUI.DrawVerticalGauge(this.boiler_state.water_temperature, Maths.CelsiusToKelvin(100), Maths.CelsiusToKelvin(374), size: new(24, GUI.RmY), color_a: GUI.font_color_orange, color_b: GUI.font_color_orange);



							//}

							//if (GUI.SliderFloat("Output Valve", ref this.boiler_state.output_valve_ratio, 0.00f, 1.00f, size: new(64, 24), color_frame: GUI.col_output))
							//{
							//	var rpc = new Boiler.ConfigureRPC()
							//	{
							//		output_valve_ratio = this.boiler_state.output_valve_ratio
							//	};
							//	rpc.Send(this.ent_boiler);
							//}

							//GUI.SameLine();

							//GUI.DrawHorizontalGaugeMulti(values: [this.boiler_state.water_moles, this.boiler_state.steam_moles], colors: [GUI.col_input.WithColorMult(0.70f), GUI.col_white.WithColorMult(0.75f)], max: this.boiler_state.water_moles + this.boiler_state.steam_moles, size: new(GUI.RmX, 24));

							//if (GUI.IsItemHovered())
							//{
							//	using (GUI.Tooltip.New(size: new(192, 0)))
							//	{
							//		GUI.LabelShaded("Water:", $"{Water.MolToKg(this.boiler_state.water_moles):0.00} kg ({Maths.KelvinToCelsius(this.boiler_state.water_temperature):0.00}°C)");
							//		GUI.LabelShaded("Steam:", $"{Water.MolToKg(this.boiler_state.steam_moles):0.00} kg ({Maths.KelvinToCelsius(this.boiler_state.steam_temperature):0.00}°C)");
							//	}
							//}
						}

						GUI.SameLine();

						using (var group_burner = GUI.Group.New(size: new Vector2(96, 24 * 4))) //), padding: new(4), add_padding_to_size: true))
						{
							//group_burner.DrawBackground(GUI.tex_window);

							using (var embed_modules = GUI.Embed.New("burner"u8, entity: this.ent_boiler, size: GUI.Rm))
							{

							}
						}

						//if (DrawKnob("test", ref this.boiler_state.safety_valve_ratio, 0.00f, 1.00f, new(48, 48)))
						//{
						//	var rpc = new Boiler.ConfigureRPC()
						//	{
						//		safety_valve_ratio = this.boiler_state.safety_valve_ratio
						//	};
						//	rpc.Send(this.ent_boiler);
						//}
					}
				}
			}
		}

		[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnGUI(Entity entity,
		[Source.Owned] in Boiler.Data boiler, [Source.Owned] in Boiler.State boiler_state,
		[Source.Owned] in Burner.Data burner, [Source.Owned] in Burner.State burner_state,
		[Source.Owned] in Interactable.Data interactable)
		{
			if (interactable.IsActive())
			{
				var gui = new BoilerGUI()
				{
					ent_boiler = entity,

					boiler = boiler,
					boiler_state = boiler_state,

					burner = burner,
					burner_state = burner_state,
				};
				gui.Submit();
			}
		}
#endif
	}
}
