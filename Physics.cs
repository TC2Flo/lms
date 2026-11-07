global using Radius = Keg.Engine.Game.Distance;
global using Width = Keg.Engine.Game.Distance;
global using Height = Keg.Engine.Game.Distance;
global using Length = Keg.Engine.Game.Distance;
global using Depth = Keg.Engine.Game.Distance;

using System.Diagnostics.CodeAnalysis;

namespace TC2.Base.Components
{
	public static partial class Phys
	{
		// useful info
		// https://en.wikipedia.org/wiki/Orders_of_magnitude_(pressure_)

		// ISO 13443 https://en.wikipedia.org/wiki/Standard_temperature_and_pressure (air at 15°C: 101.325 kPa, 1.225 kg/m^3)
		//public const double reference_atmospheric_pressure = 101325.000; // Pa, at sea level 
		//public const float reference_atmospheric_pressure_bar = 1.01325f; // Bar, at sea level 

		//public const float reference_temperature = 288.150f; // 15°C in K
		//public const float reference_air_density = 1.225f; // kg/m^3
		//public const float reference_air_mol_per_m3 = 42.2851f;


		// https://en.wikipedia.org/wiki/Avogadro_constant
		public const double avogadro_constant = 6.02214076e+23;
		public const float avogadro_constant_f32 = 6.02214076e+23f;

		// https://en.wikipedia.org/wiki/Vacuum_permeability
		public const double magnetic_constant = 1.25663706127e-6;
		public const float magnetic_constant_f32 = 1.25663706127e-6f;

		// https://en.wikipedia.org/wiki/Boltzmann_constant
		private const double boltzmann_constant = 1.380649e-23;
		public const double boltzmann_constant_kj = 1.380649e-26;
		public const float boltzmann_constant_f32 = 1.380649e-23f;
		public const float boltzmann_constant_f32_kj = 1.380649e-26f;

		//// https://en.wikipedia.org/wiki/Gas_constant
		//public const double molar_gas_constant = 8.31446261815324;
		//public const double molar_gas_constant_bar = 8.31447e-5;


		//public const float molar_gas_constant_f32 = 8.31446261815324f;
		//public const float molar_gas_constant_f32_bar = 8.31446261815324e-5f;
		//public const float molar_gas_constant_f32_kj = 8.31446261815324e-3f;



		// https://en.wikipedia.org/wiki/Gas_constant
		private const double molar_gas_constant = 8.31446261815324; // boltzmann_constant * avogadro_constant;
		//private const double molar_gas_constant_kj = 8.31446261815324e-3; // boltzmann_constant_kj * avogadro_constant;
		public const double molar_gas_constant_bar = 8.31446261815324e-5; // (boltzmann_constant * avogadro_constant) * 1.00e-5;


		private const float molar_gas_constant_f32 = 8.31446261815324f;
		//private const float molar_gas_constant_f32_kj = 8.31446261815324e-3f;
		public const float molar_gas_constant_f32_bar = 8.31446261815324e-5f;


		//public const float air_n2_ratio = 0.724f; // ratio of n2 per 1 kg of air
		//public const float air_o2_ratio = 0.232f; // ratio of o2 per 1 kg of air
		//public const float air_co2_ratio = 0.044f; // ratio of co2 per 1 kg of air

		public const float air_n2_ratio = 0.669f;
		public const float air_o2_ratio = 0.278f;
		public const float air_co2_ratio = 0.053f;

		public const float reference_gravity = 9.807f;
		public const float reference_gravity_force_mult = 3.000f;
		public const float reference_altitude = 0.00f;

		// https://www.omnicalculator.com/physics/ideal-gas-density
		public static readonly Pressure atmospheric_pressure_kordel = 1.000f; // bar, at sea level, 300 K 
		public static readonly Energy air_specific_heat = 1.005f; // kJ/(kg*K)
		public const float ambient_temperature = 300.00f; // K

		public const float water_critical_point = 647.096f;

		public const float o_atomic_mass = 0.015999f; // kg/mol
		public const float n_atomic_mass = 0.0140067f; // kg/mol
		public const float h_atomic_mass = 0.001008f; // kg/mol
		public const float c_atomic_mass = 0.012011f; // kg/mol
		public const float s_atomic_mass = 0.032065f; // kg/mol

		public const float o2_molar_mass = 0.032003f; // kg/mol
		public const float h2_molar_mass = 0.002016f; // kg/mol
		public const float n2_molar_mass = 0.028014f; // kg/mol
		public const float co_molar_mass = 0.028010f; // kg/mol
		public const float co2_molar_mass = 0.044009f; // kg/mol
		public const float so2_molar_mass = s_atomic_mass + (o_atomic_mass * 2); // kg/mol
		public const float no2_molar_mass = 0.046005f; // kg/mol
		public const float h2o_molar_mass = 0.0180153f; // kg/mol
		public const float ch4_molar_mass = 0.016043f; // kg/mol
		public const float air_molar_mass = (air_n2_ratio * n2_molar_mass) + (air_o2_ratio * o2_molar_mass) + (air_co2_ratio * co2_molar_mass); // kg/mol // https://en.wikipedia.org/wiki/Molar_mass = 0.029643228

		public static readonly Vec8f air_molar_mass_mult = new Vec8f
		(
			Phys.n2_molar_mass,
			Phys.o2_molar_mass,
			Phys.co2_molar_mass,
			Phys.h2o_molar_mass,

			Phys.h2_molar_mass,
			Phys.co_molar_mass,
			Phys.so2_molar_mass,
			Phys.no2_molar_mass
		);

		public const float o2_molar_mass_inv = 1.00f / o2_molar_mass;
		public const float h2_molar_mass_inv = 1.00f / h2_molar_mass;
		public const float n2_molar_mass_inv = 1.00f / n2_molar_mass;
		public const float co_molar_mass_inv = 1.00f / co_molar_mass;
		public const float co2_molar_mass_inv = 1.00f / co2_molar_mass;
		public const float so2_molar_mass_inv = 1.00f / so2_molar_mass;
		public const float no2_molar_mass_inv = 1.00f / no2_molar_mass;
		public const float h2o_molar_mass_inv = 1.00f / h2o_molar_mass;
		public const float ch4_molar_mass_inv = 1.00f / ch4_molar_mass;
		public const float air_molar_mass_inv = 1.00f / air_molar_mass;

		public static readonly Vec8f air_molar_mass_mult_inv = new Vec8f
		(
			Phys.n2_molar_mass_inv,
			Phys.o2_molar_mass_inv,
			Phys.co2_molar_mass_inv,
			Phys.h2o_molar_mass_inv,

			Phys.h2_molar_mass_inv,
			Phys.co_molar_mass_inv,
			Phys.so2_molar_mass_inv,
			Phys.no2_molar_mass_inv
		);

		public static readonly Energy o2_gas_constant = molar_gas_constant_f32_bar / o2_molar_mass; // kJ/(kg*K)
		public static readonly Energy h2_gas_constant = molar_gas_constant_f32_bar / h2_molar_mass; // kJ/(kg*K)
		public static readonly Energy n2_gas_constant = molar_gas_constant_f32_bar / n2_molar_mass; // kJ/(kg*K)
		public static readonly Energy co_gas_constant = molar_gas_constant_f32_bar / co_molar_mass; // kJ/(kg*K)
		public static readonly Energy co2_gas_constant = molar_gas_constant_f32_bar / co2_molar_mass; // kJ/(kg*K)
		public static readonly Energy so2_gas_constant = molar_gas_constant_f32_bar / so2_molar_mass; // kJ/(kg*K)
		public static readonly Energy no2_gas_constant = molar_gas_constant_f32_bar / no2_molar_mass; // kJ/(kg*K)
		public static readonly Energy ch4_gas_constant = molar_gas_constant_f32_bar / ch4_molar_mass; // kJ/(kg*K)
		public static readonly Energy air_gas_constant = molar_gas_constant_f32_bar / air_molar_mass; // kJ/(kg*K)

		public static readonly Density o2_density_kordel = 1.00f / (o2_gas_constant.m_value * ambient_temperature); // kg/m³ at 300 K, 1 bar
		public static readonly Density h2_density_kordel = 1.00f / (h2_gas_constant.m_value * ambient_temperature); // kg/m³ at 300 K, 1 bar
		public static readonly Density n2_density_kordel = 1.00f / (n2_gas_constant.m_value * ambient_temperature); // kg/m³ at 300 K, 1 bar
		public static readonly Density co_density_kordel = 1.00f / (co_gas_constant.m_value * ambient_temperature); // kg/m³ at 300 K, 1 bar
		public static readonly Density co2_density_kordel = 1.00f / (co2_gas_constant.m_value * ambient_temperature); // kg/m³ at 300 K, 1 bar
		public static readonly Density so2_density_kordel = 1.00f / (so2_gas_constant.m_value * ambient_temperature); // kg/m³ at 300 K, 1 bar
		public static readonly Density no2_density_kordel = 1.00f / (no2_gas_constant.m_value * ambient_temperature); // kg/m³ at 300 K, 1 bar
		public static readonly Density ch4_density_kordel = 1.00f / (ch4_gas_constant.m_value * ambient_temperature); // kg/m³ at 300 K, 1 bar
		public static readonly Density air_density_kordel = 1.00f / (air_gas_constant.m_value * ambient_temperature); // kg/m³ at 300 K, 1 bar

		public static readonly Energy s_molar_heat_capacity = Energy.J(22.75); // kJ/K*mol
		public static readonly Energy c_molar_heat_capacity = Energy.J(8.517); // kJ/K*mol
		public static readonly Energy o2_molar_heat_capacity = Energy.J(29.378); // kJ/K*mol
		public static readonly Energy h2_molar_heat_capacity = Energy.J(28.836); // kJ/K*mol
		public static readonly Energy n2_molar_heat_capacity = Energy.J(29.124); // kJ/K*mol
		public static readonly Energy co_molar_heat_capacity = Energy.J(29.1); // kJ/K*mol
		public static readonly Energy co2_molar_heat_capacity = Energy.J(37.135); // kJ/K*mol
		public static readonly Energy so2_molar_heat_capacity = Energy.J(39.94); // kJ/K*mol
		public static readonly Energy no2_molar_heat_capacity = Energy.J(37.2); // kJ/K*mol
		public static readonly Energy h2o_molar_heat_capacity = Energy.J(75.385); // kJ/K*mol
		public static readonly Energy ch4_molar_heat_capacity = Energy.J(35.7); // kJ/K*mol
		public static readonly Energy air_molar_heat_capacity = (air_n2_ratio * n2_molar_heat_capacity) + (air_o2_ratio * o2_molar_heat_capacity) + (air_co2_ratio * co2_molar_heat_capacity); // kJ/K*mol

		public static readonly Vec8f air_molar_heat_capacity_mult = new Vec8f
		(
			Phys.n2_molar_heat_capacity,
			Phys.o2_molar_heat_capacity,
			Phys.co2_molar_heat_capacity,
			Phys.h2o_molar_heat_capacity,

			Phys.h2_molar_heat_capacity,
			Phys.co_molar_heat_capacity,
			Phys.so2_molar_heat_capacity,
			Phys.no2_molar_heat_capacity
		);

		//public static readonly Energy s_specific_heat_capacity = s_molar_heat_capacity / s_atomic_mass // kJ/K*kg
		//public static readonly Energy c_specific_heat_capacity = Energy.J(8.517); // kJ/K*kg
		//public static readonly Energy o2_specific_heat_capacity = Energy.J(29.378); // kJ/K*kg
		//public static readonly Energy h2_specific_heat_capacity = Energy.J(28.836); // kJ/K*kg
		//public static readonly Energy n2_specific_heat_capacity = Energy.J(29.124); // kJ/K*kg
		//public static readonly Energy co_specific_heat_capacity = Energy.J(29.1); // kJ/K*kg
		//public static readonly Energy co2_specific_heat_capacity = Energy.J(37.135); // kJ/K*kg
		//public static readonly Energy so2_specific_heat_capacity = Energy.J(39.94); // kJ/K*kg
		//public static readonly Energy no2_specific_heat_capacity = Energy.J(37.2); // kJ/K*kg
		//public static readonly Energy h2o_specific_heat_capacity = Energy.J(75.385); // kJ/K*kg
		//public static readonly Energy ch4_specific_heat_capacity = Energy.J(35.7); // kJ/K*kg
		//public static readonly Energy air_specific_heat_capacity = (air_n2_ratio * n2_molar_heat_capacity) + (air_o2_ratio * o2_molar_heat_capacity) + (air_co2_ratio * co2_molar_heat_capacity); // kJ/K*mol


		// Std enthalpy of formation
		public const float c_enthalpy = 0.00f; // kJ/mol
		public const float o2_enthalpy = 0.00f; // kJ/mol
		public const float h2_enthalpy = 0.00f; // kJ/mol
		public const float n2_enthalpy = 0.00f; // kJ/mol
		public const float co_enthalpy = -110.53f; // kJ/mol
		public const float co2_enthalpy = -393.51f; // kJ/mol
		public const float no2_enthalpy = 33.2f; // kJ/mol
		public const float h2o_enthalpy = -241.82f; // kJ/mol
		public const float ch4_enthalpy = -74.81f; // kJ/mol


		// Std molar entropy
		public const float c_entropy = 5.74f; // J/K*mol
		public const float o2_entropy = 205.14f; // J/K*mol
		public const float h2_entropy = 130.68f; // J/K*mol
		public const float n2_entropy = 191.61f; // J/K*mol
		public const float co_entropy = 197.67f; // J/K*mol
		public const float co2_entropy = 213.74f; // J/K*mol
		public const float no2_entropy = 240.1f; // J/K*mol
		public const float h2o_entropy = 188.83f; // J/K*mol
		public const float ch4_entropy = 186.26f; // J/K*mol


		// Gibbs free energy of formation
		public const float c_gibbs = 0.00f; // kJ/mol
		public const float o2_gibbs = 0.00f; // kJ/mol
		public const float h2_gibbs = 0.00f; // kJ/mol
		public const float n2_gibbs = 0.00f; // kJ/mol
		public const float co_gibbs = -137.17f; // kJ/mol
		public const float co2_gibbs = -394.36f; // kJ/mol
												 //public const float no2_gibbs = 0.046005f; // kJ/mol
		public const float h2o_gibbs = -228.57f; // kJ/mol
		public const float ch4_gibbs = -50.72f; // kJ/mol


		public const float carbon_flame_temperature_max = 3773.15f; // Kelvin, maximum flame temperature of carbon in 100% oxygen https://en.wikipedia.org/wiki/Adiabatic_flame_temperature

		// TODO https://www.engineeringtoolbox.com/water-liquid-gas-thermal-conductivity-temperature-pressure-d_2012.html
		public static readonly Power water_thermal_conductivity = 0.000598f; // kW/(m*K)
		public static readonly Power steam_thermal_conductivity = 0.00003585f; // kW/(m*K) 

		public static readonly Energy water_specific_heat = 4.184f; // kJ/(kg*K)
		public static readonly Temperature water_boiling_point = Temperature.Celsius(100.00f);
		//public const float water_mass_per_unit = 0.100f; // kg



		public static float GetInductanceSolenoid(int turns, Area wire_cross_section, Length length)
		{
			return (magnetic_constant_f32 * turns.Pow2() * wire_cross_section.m_value) / length.m_value;
		}

		//public static void Arrhenius(Temperature temperature)
		//{

		//}


		// https://www.omnicalculator.com/physics/lmtd

		//public static double CalculateAirPressure(Temperature temperature)
		//{
		//	return (reference_atmospheric_pressure / ambient_temperature) * (double)temperature;
		//}

		//public static double MolToKg(double moles, double molar_mass)
		//{
		//	return moles * molar_mass;
		//}

		//public static double KgToMol(double mass, double molar_mass)
		//{
		//	return mass / molar_mass;
		//}

		//public static double MolToKg(ref this IMaterial.Substance substance, double moles)
		//{
		//	return moles * substance.molar_mass;
		//}

		//// https://www.omnicalculator.com/physics/air-pressure-at-altitude
		//public static Pressure CalculateAtmosphericPressure(Temperature temperature, Height altitude)
		//{
		//	return (float)(reference_atmospheric_pressure_bar * MathF.Exp((-reference_gravity * air_molar_mass * (altitude - reference_altitude)) / (molar_gas_constant_f32_bar * temperature)));
		//}

		//// https://www.omnicalculator.com/physics/air-pressure-at-altitude
		//public static float CalculateAtmosphericPressureBar(Temperature temperature, float altitude)
		//{
		//	return ToBar(reference_atmospheric_pressure * Math.Exp((-reference_gravity * air_molar_mass * (altitude - reference_altitude)) / (molar_gas_constant * temperature)));
		//}

		// https://www.omnicalculator.com/physics/air-pressure-at-altitude
		public static Pressure CalculateAtmosphericPressure(Temperature temperature, Height altitude, float gravity = Phys.reference_gravity)
		{
			//return (reference_atmospheric_pressure_bar * MathF.Exp((-reference_gravity * air_molar_mass * (altitude - reference_altitude)) / (molar_gas_constant_f32_bar * temperature)));
			return (Pressure.Ambient * MathF.Exp((-gravity * air_molar_mass * (altitude - reference_altitude)) / (molar_gas_constant_f32 * temperature)));
		}

		// https://www.omnicalculator.com/physics/air-pressure-at-altitude
		public static Pressure GetAtmosphericPressure(ref readonly this Region.Data region, float y, float gravity = Phys.reference_gravity)
		{
			var altitude = region.GetAltitude(y);
			var temperature = region.GetAmbientTemperatureAtAltitude(altitude);

			//return (reference_atmospheric_pressure_bar * MathF.Exp((-reference_gravity * air_molar_mass * (altitude - reference_altitude)) / (molar_gas_constant_f32_bar * temperature)));
			return (Pressure.Ambient * MathF.Exp((-gravity * air_molar_mass * (altitude - reference_altitude)) / (molar_gas_constant_f32 * temperature)));
		}

		public static void GetAtmosphericInfo(ref readonly this Region.Data region, float y, out float altitude, out Temperature temperature, out Pressure pressure)
		{
			altitude = region.GetAltitude(y);
			temperature = region.GetAmbientTemperatureAtAltitude(altitude);
			pressure = (Pressure.Ambient * MathF.Exp((-Phys.reference_gravity * air_molar_mass * (altitude - reference_altitude)) / (molar_gas_constant_f32 * temperature)));
		}

		public static Temperature GetAmbientTemperature(ref readonly this Region.Data region, Vector2 pos)
		{
			var altitude = region.GetAltitude(pos);
			var temperature = Region.ambient_temperature - (altitude * 0.02f);

			return temperature;
		}

		public static Temperature GetAmbientTemperatureAtAltitude(ref readonly this Region.Data region, float altitude)
		{
			var temperature = Region.ambient_temperature - (altitude * 0.02f);
			return temperature;
		}

		//public static Density GetAirDensityAtAltitude(ref readonly this Region.Data region, float altitude)
		//{
		//	var temperature = Region.ambient_temperature - (altitude * 0.02f);
		//	return temperature;
		//}

		//// https://www.omnicalculator.com/physics/thermal-energy (KE = f * k * T / 2)
		//public static double CalculateGasKineticEnergy(Temperature temperature)
		//{
		//	return (3.00 * boltzmann_constant * temperature / 2.00);
		//}

		//// https://www.omnicalculator.com/physics/thermal-energy (U = n * Na * KE)
		//public static double CalculateGasThermalEnergy(Amount moles, Temperature temperature)
		//{
		//	return moles * avogadro_constant * CalculateGasKineticEnergy(temperature);
		//}


		//public static float CalculateGasPressure(Volume volume, Temperature temperature, float moles)
		//{
		//	return (moles * molar_gas_constant * temperature) / volume;
		//}

		//public static float ToBar(double pascal) => (float)(pascal * pascal_to_bar);
		//public static double ToPascal(float bar) => bar * bar_to_pascal;

		//public static double CalculateGasPressure(Volume volume, Temperature temperature, float moles)
		//{
		//	var volume_inv = 1.00 / volume;
		//	return ((double)moles * molar_gas_constant * (double)temperature * volume_inv);
		//}

		//public static float CalculateGasMoles(Volume volume, Temperature temperature, double pressure)
		//{
		//	return (float)((pressure * (double)volume) / (molar_gas_constant * (double)temperature));
		//}

		//public static float CalculateGasVolume(float moles, Temperature temperature, double pressure)
		//{
		//	return (float)((molar_gas_constant * (double)moles * (double)temperature) / pressure);
		//}

		//public static float CalculateGasTemperature(Volume volume, float moles, double pressure)
		//{
		//	return (float)((pressure * (double)volume) / ((double)moles * molar_gas_constant));
		//}

		//public static float CalculateGasPressureBar(Volume volume, Temperature temperature, float moles)
		//{
		//	return ToBar(((double)moles * molar_gas_constant * (double)temperature) / (double)volume);
		//}

		//public static float base_altitude = 100.00f;

		//public static void OrificeFlowVelocity(float velocity, Density density, Pressure pressure_inside, out Pressure pressure_outside, float discharge_coefficient = 0.65f)
		//{
		//	density = Maths.Max(density, 0.003f);

		//	//var delta_p = (pressure_outside - pressure_inside);
		//	//velocity * velocity = (((pressure_outside - pressure_inside) * 2) / density);
		//	//velocity * velocity = ((pressure_outside - pressure_inside) * 2) / density;
		//	//(velocity * velocity) * density = ((pressure_outside - pressure_inside) * 2);
		//	//((velocity * velocity) * density) * 0.50f = (pressure_outside - pressure_inside);

		//	//pressure_outside = ((((velocity * velocity) * density) * 0.50f) + pressure_inside);
		//	pressure_outside = (-((((velocity) / discharge_coefficient) * density * 0.50f).Pow2()) + (pressure_inside));
		//}

		public static void OrificeFlowVelocity(out float velocity, Density density, Pressure pressure_inside, Pressure pressure_outside, float discharge_coefficient = 0.65f)
		{
			density = Maths.Max(density, 0.00003f);

			var delta_p = (pressure_outside - pressure_inside); // * 50.00f; //.Pa_f32();
			var delta_p_2x = delta_p + delta_p;

			velocity = (discharge_coefficient * Maths.SqrtSigned(delta_p_2x / density));
		}


		// https://www.studysmarter.co.uk/explanations/engineering/engineering-fluid-mechanics/orifice-flow/
		public static void OrificeFlow(out Volume flow_rate, Area area, Density density, Pressure pressure_inside, Pressure pressure_outside, float discharge_coefficient = 0.65f)
		{
			density = Maths.Max(density, 0.00003f);

			var delta_p = (pressure_outside - pressure_inside); // * 50.00f;
			var delta_p_2x = delta_p + delta_p;

			flow_rate = (discharge_coefficient * area.m_value * Maths.SqrtSigned((delta_p_2x) / density));
		}

		// https://www.studysmarter.co.uk/explanations/engineering/engineering-fluid-mechanics/orifice-flow/
		public static void OrificeFlowDual(out Volume flow_rate, Area area, Density density_inside, Density density_outside, Pressure pressure_inside, Pressure pressure_outside, float discharge_coefficient = 0.65f)
		{
			flow_rate = 0.00f;

			var delta_p = (pressure_outside - pressure_inside); // / (2 * pressure_outside);
			//if (Maths.Abs(delta_p) >= 0.0001f)
			{
				//var delta_p_2x = (delta_p * 50) * 2;
				var delta_p_2x = (delta_p) * 2 * 50;

				var density = (density_inside + density_outside) * 0.50f;
				density = Maths.Max(density, 0.00003f);

				flow_rate = (discharge_coefficient * area.m_value * Maths.SqrtSigned(delta_p_2x / density));
			}
			//else
			//{
			//	flow_rate = 0.00f;
			//}

			////flow_rate = 0.00f;
			////if (density > Maths.epsilon)
			////{
			////var delta_p = (float)(pressure_outside.Pa() - pressure_inside.Pa()).Pa();
			//var delta_p = (float)(((pressure_outside - pressure_inside)));
			//delta_p *= 50.00f;

			////var delta_p_abs = Maths.Abs(delta_p);
			////if (delta_p_abs > Maths.epsilon)
			////{
			//var delta_p_2x = delta_p + delta_p;

			////var flow_rate_in = 0.00f;
			////var flow_rate_out = 0.00f;

			//var density_avg = Maths.Avg(density_inside, density_outside);
			//density_avg = Maths.Max(density_avg, 0.003f);

			//flow_rate = discharge_coefficient * area.m_value * (delta_p_2x / density_avg).SqrtSigned();


			////density_inside = Maths.Max(density_inside, 0.01f);
			////density_outside = Maths.Max(density_outside, 0.01f);

			////if (density_avg > 0.00f)
			////{

			////if (delta_p.m_value.IsPositive()) flow_rate = (discharge_coefficient * area.m_value * MathF.Sqrt(delta_p_abs_2x / density_outside)).FixNaN();
			////else flow_rate = -(discharge_coefficient * area.m_value * MathF.Sqrt(delta_p_abs_2x / density_inside)).FixNaN();


			////flow_rate = (discharge_coefficient * area.m_value * MathF.Sqrt(delta_p_abs_2x / density_avg)).FixNaN().WithSign(delta_p);

			////if (delta_p.IsPositive()) flow_rate = (discharge_coefficient * area.m_value * MathF.Sqrt(delta_p_abs_2x / density_avg)).FixNaN();
			////else flow_rate = -(discharge_coefficient * area.m_value * MathF.Sqrt(delta_p_abs_2x / density_avg)).FixNaN();

			////}

			////flow_rate *= 50.00f;

			////if (density_outside > Maths.epsilon && pressu) flow_rate_in = (discharge_coefficient * area.m_value * MathF.Sqrt(Pressure.bar(delta_p_abs_2x / density_outside).Pa_f32())).FixNaN();
			////if (density_inside > Maths.epsilon && ) flow_rate_out = (discharge_coefficient * area.m_value * MathF.Sqrt(Pressure.bar(delta_p_abs_2x / density_inside).Pa_f32())).FixNaN();

			////}
			////}
		}


		public static void PressureDrop(out Pressure pressure_loss, Distance distance, float pipe_diameter, Density density, float velocity_avg, float friction_factor)
		{
			pressure_loss = friction_factor * (distance / pipe_diameter) * ((density * velocity_avg.Pow2()) * 0.50f);
		}


		// https://www.omnicalculator.com/physics/charles-law
		public static void CharlesLaw(Volume volume_a, Temperature temperature_a, out Volume volume_b, Temperature temperature_b)
		{
			volume_b = (float)((volume_a / temperature_a) * temperature_b);
		}

		// https://www.omnicalculator.com/physics/charles-law
		public static void CharlesLaw(Volume volume_a, Temperature temperature_a, Volume volume_b, out Temperature temperature_b)
		{
			temperature_b = (float)((temperature_a / volume_a) * volume_b);
		}



		// https://www.omnicalculator.com/physics/boyles-law
		public static void BoyleLaw(Pressure pressure_a, Volume volume_a, out Pressure pressure_b, Volume volume_b)
		{
			pressure_b = (float)((pressure_a * volume_a) / volume_b);
		}

		// https://www.omnicalculator.com/physics/boyles-law
		public static void BoyleLaw(Pressure pressure_a, Volume volume_a, Pressure pressure_b, out Volume volume_b)
		{
			volume_b = (float)((pressure_a * volume_a) / pressure_b);
		}



		// https://www.omnicalculator.com/physics/gay-lussacs-law
		public static void GayLussacLaw(Pressure pressure_a, Temperature temperature_a, out Pressure pressure_b, Temperature temperature_b)
		{
			pressure_b = (float)((pressure_a / temperature_a) * temperature_b);
		}

		// https://www.omnicalculator.com/physics/gay-lussacs-law
		public static void GayLussacLaw(Pressure pressure_a, Temperature temperature_a, Pressure pressure_b, out Temperature temperature_b)
		{
			temperature_b = (float)((temperature_a * pressure_b) / pressure_a);
		}



		// https://www.omnicalculator.com/physics/ideal-gas-law
		public static void IdealGasLaw(out Pressure pressure, Volume volume, Amount moles, Temperature temperature)
		{
			pressure = (float)((moles * molar_gas_constant_f32_bar * temperature) / volume);
		}

		// https://www.omnicalculator.com/physics/ideal-gas-law
		public static void IdealGasLaw(Pressure pressure, out Volume volume, Amount moles, Temperature temperature)
		{
			volume = (float)((moles * molar_gas_constant_f32_bar * temperature) / pressure);
		}

		// https://www.omnicalculator.com/physics/ideal-gas-law
		public static void IdealGasLaw(Pressure pressure, Volume volume, out Amount moles, Temperature temperature)
		{
			moles = (float)((pressure * volume) / (molar_gas_constant_f32_bar * temperature));
		}

		// https://www.omnicalculator.com/physics/ideal-gas-law
		public static void IdealGasLaw(Pressure pressure, Volume volume, Amount moles, out Temperature temperature)
		{
			temperature = (float)((pressure * volume) / (moles * molar_gas_constant_f32_bar));
		}



		// https://www.omnicalculator.com/physics/ideal-gas-law
		public static void IdealGasLawFast(out Pressure pressure, Volume volume, Amount moles, Temperature temperature)
		{
			pressure = (float)((moles * molar_gas_constant_f32_bar * temperature) * volume.m_value.RcpFast());
		}

		// https://www.omnicalculator.com/physics/ideal-gas-law
		public static void IdealGasLawFast(Pressure pressure, out Volume volume, Amount moles, Temperature temperature)
		{
			volume = (float)((moles * molar_gas_constant_f32_bar * temperature) * pressure.m_value.RcpFast());
		}

		// https://www.omnicalculator.com/physics/ideal-gas-law
		public static void IdealGasLawFast(Pressure pressure, Volume volume, out Amount moles, Temperature temperature)
		{
			moles = (float)((pressure * volume) * (molar_gas_constant_f32_bar * temperature).m_value.RcpFast());
		}

		// https://www.omnicalculator.com/physics/ideal-gas-law
		public static void IdealGasLawFast(Pressure pressure, Volume volume, Amount moles, out Temperature temperature)
		{
			temperature = (float)((pressure * volume) * (moles * molar_gas_constant_f32_bar).m_value.RcpFast());
		}



		// https://www.omnicalculator.com/physics/ideal-gas-density
		public static float GetSpecificGasConstant(float molar_mass)
		{
			return molar_gas_constant_f32 / molar_mass;
		}


		// https://www.omnicalculator.com/physics/ideal-gas-density
		public static float GetSpecificGasConstantBar(float molar_mass)
		{
			return molar_gas_constant_f32_bar / molar_mass;
		}

		// https://www.omnicalculator.com/physics/ideal-gas-density
		public static float GetAirDensity(Pressure pressure, Temperature temperature)
		{
			return ((pressure) / (GetSpecificGasConstantBar(air_molar_mass) * temperature));
		}



		// https://www.omnicalculator.com/physics/ideal-gas-density
		public static void IdealGasDensity(Mass molar_mass, out Pressure pressure, Temperature temperature, Density density)
		{
			//density = pressure / (GetSpecificGasConstantBar(air_molar_mass) * temperature);
			//pressure * density = (GetSpecificGasConstantBar(air_molar_mass) * temperature);
			pressure = GetSpecificGasConstantBar(molar_mass) * temperature.m_value * density.m_value;
		}

		// https://www.omnicalculator.com/physics/ideal-gas-density
		public static void IdealGasDensity(Mass molar_mass, Pressure pressure, out Temperature temperature, Density density)
		{
			//temperature = (density * pressure) / GetSpecificGasConstantBar(molar_mass);
			temperature = pressure.m_value / (GetSpecificGasConstantBar(molar_mass) * density.m_value);
		}

		// https://www.omnicalculator.com/physics/ideal-gas-density
		public static void IdealGasDensity(Mass molar_mass, Pressure pressure, Temperature temperature, out Density density)
		{
			density = pressure / (GetSpecificGasConstantBar(molar_mass) * temperature);
		}




		// https://www.omnicalculator.com/physics/buoyancy
		/// <summary>
		/// 
		/// </summary>
		/// <param name="density">Density of the fluid the object is immersed in, kg/m³</param>
		/// <param name="volume">Volume of the displaced fluid, m³</param>
		/// <param name="buoyant_force">Buoyant force, N</param>
		public static void Buoyancy(Density density, Volume volume, float gravity, out float buoyant_force)
		{
			buoyant_force = density * volume * gravity;
		}




		//// https://www.omnicalculator.com/physics/bernoulli-equation
		//public static void Bernoulli(Density density, Pressure pressure_a, float velocity_a, Area area_a, out Pressure pressure_b, float velocity_b, Area area_b)
		//{
		//	pressure_a + (0.50f * density * velocity_a.Pow2()) = pressure_b + (0.50f * density * velocity_b.Pow2());
		//}









		//// https://en.wikipedia.org/wiki/Water%E2%80%93gas_shift_reaction
		//public static float WGS_Coeff(Temperature temperature)
		//{
		//	temperature.m_value = Maths.Clamp(temperature.m_value, 600, 2000);
		//	return (float)Math.Pow(10.00, (-2.4198 + (0.0003855 * temperature)) + (2180.6 / temperature));
		//}

		//public static void Hydrocarbon(Temperature temperature, ref Amount moles_c, ref Amount moles_h, ref Amount moles_o2, out Amount moles_co2, out Amount moles_co, out Amount moles_h2o, out Amount moles_h2, out Energy energy)
		//{
		//	var n = moles_c.m_value;

		//	var x_c = moles_c.m_value;
		//	//var x_h = (x_c * 2) + 2;
		//	var x_h = moles_h.m_value;
		//	var x_o = moles_o2.m_value * 2;

		//	var k_eq = WGS_Coeff(temperature);

		//	var x_h2o = 0.50f * x_h; // n + 1;
		//	//var x_h2o = (2 * n) + 1;


		//	//var a = x_c;



		//	// k_eq * (b * c) = a * d
		//	// k_eq * b * c = a * d
		//	// k_eq * b * c = a * d
		//	// b * (k_eq * c) = a * d
		//	//var b = (a * d) / (k_eq * c);

		//	//var a = n - b;
		//	//var b = ((n - b) * d) / (k_eq * c);
		//	//var b * (k_eq * c) = ((n - b) * d);
		//	//var b * (k_eq * c) = (n - b) * d;

		//	//var b * k_eq * c = (n - b) * d;


		//	var a = n;
		//	varb = 
		//	var c = x_h2o;
		//	//var b = (a * d) / (k_eq * c);
		//	var d = (x_h - (2 * c)) * 0.50f;


		//	//var b = (d * n) / ((c * k_eq) + d);
		//	////var a = n - b;
		//	//var a = (x_o - b - c) * 0.50f;

		//	//k_eq * (b * c) = a * d
		//	//var a = (k_eq * (b * c)) / d;

		//	//var b = x_c 

		//	//var a = x_c - b;
		//	//var b = x_o - (2 * a) - c;
		//	//var c = x_h - (2 * d);
		//	//var d = (k_eq * (b * c)) / a;

		//	moles_co2 = a;
		//	moles_co = b;
		//	moles_h2o = c;
		//	moles_h2 = d;

		//	energy = k_eq;


		//	//var a = 

		//	//var o2_a = x_c + (x_h * 0.25f)

		//}

		//// https://www.engineeringtoolbox.com/convective-heat-source-d_1007.html - Air Velocity
		///// <summary>
		///// Simple approximation of air flow velocity above a heat source.<br/>
		///// <i>* Height is measured from the ground level, not heat source!</i>
		///// </summary>
		///// <param name="power">Heat source power in watts. (W)</param>
		///// <param name="height">Distance above the ground in meters. (m)</param>
		///// <param name="velocity">Air flow velocity. (m/s)</param>
		///// <param name="modifier">Coefficient for tweaking the result, usually between 1.00f and 2.00f.</param>
		//public static void ConvectiveAirFlowVelocity(double power, float height, out float velocity, float modifier = 1.00f)
		//{
		//	velocity = modifier * MathF.Cbrt((float)(power * 0.001) / Maths.Max(0.25f, height));
		//}

		//// https://www.engineeringtoolbox.com/convective-heat-source-d_1007.html - Air Flow Volume
		///// <summary>
		///// Simple approximation of air flow volume above a heat source.<br/>
		///// <i>* Height is measured from the ground level, not heat source!</i>
		///// </summary>
		///// <param name="power">Heat source power in watts. (W)</param>
		///// <param name="height">Distance above the ground in meters. (m)</param>
		///// <param name="volume">Air flow volume. (m³/s per m²)</param>
		///// <param name="modifier">Coefficient for tweaking the result, usually between 0.05f and 0.15f.</param>
		//public static void ConvectiveAirFlowVolume(double power, float height, out Volume volume, float modifier = 0.06f)
		//{
		//	volume = modifier * MathF.Cbrt((float)(power * 0.001)) * MathF.Pow(Maths.Max(1.00f, height), 5.00f / 3.00f);
		//}




		//// https://www.engineeringtoolbox.com/convective-air-flow-d_1006.html - Air Velocity
		///// <summary>
		///// Approximation of air flow velocity along a wall that acts as a heat source.
		///// </summary>
		///// <param name="height">Height of the wall in meters. (m)</param>
		///// <param name="temperature">Surface temperature. (K)</param>
		///// <param name="temperature_ambient">Ambient temperature. (K)</param>
		///// <param name="velocity">Air flow velocity. (m/s)</param>
		//public static void ConvectiveAirFlowVelocityWall(float height, Temperature temperature, Temperature temperature_ambient, out float velocity)
		//{
		//	//velocity = 0.65f * (reference_gravity * height * (temperature_ambient - temperature) / temperature_ambient).Sqrt();
		//	velocity = 0.65f * (reference_gravity * height * (MathF.Abs(temperature_ambient - temperature) / temperature_ambient)).Sqrt();
		//}

		//// https://www.engineeringtoolbox.com/convective-air-flow-d_1006.html - Air Volume
		///// <summary>
		///// Approximation of air flow volume along a wall that acts as a heat source.
		///// </summary>
		///// <param name="height">Height of the wall in meters. (m)</param>
		///// <param name="temperature">Surface temperature. (K)</param>
		///// <param name="temperature_ambient">Ambient temperature. (K)</param>
		///// <param name="volume">Air flow volume. (m³/s per m²)</param>
		//public static void ConvectiveAirFlowVolumeWall(float height, Temperature temperature, Temperature temperature_ambient, out Volume volume)
		//{
		//	//volume = 0.019f * MathF.Pow(reference_gravity * MathF.Abs(temperature_ambient - temperature) / temperature_ambient, 0.40f) * MathF.Pow(height, 1.20f);
		//	volume = 0.019f * MathF.Pow(reference_gravity * (MathF.Abs(temperature_ambient - temperature) / temperature_ambient), 0.40f) * MathF.Pow(height, 1.20f);
		//	//volume = 0.019f * MathF.Pow(9.81f * (80 - 20 / (273.00f + 20.00f)), 0.40f) * MathF.Pow(1.00f, 1.20f);
		//}


		//public static double CalculateGasPressure(Volume volume, Temperature temperature, float moles)
		//{
		//	return (moles * molar_gas_constant * temperature.K) / volume.m3;
		//}

		//public static float CalculateGasTemperature(double pressure, Volume volume, float moles)
		//{
		//	return 
		//}

		//public static void GetBiot(ref this IMaterial.Substance substance, float area, Volume volume)
		//{
		//	var biot = 
		//}


		//// https://www.omnicalculator.com/physics/heat-transfer-coefficient
		//public static float CalculateHeatCoefficient(ref readonly this IMaterial.Substance substance,  float thickness)
		//{
		//	return thickness != 0.00f ? (substance.thermal_conductivity / thickness) : 0.00f;
		//}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="thermal_conductivity">kW/(mK)</param>
		/// <param name="thickness">m</param>
		/// <returns> kW/(m²K)</returns>
		public static float GetConvectionHTC(float thermal_conductivity, float thickness)
		{
			return Power.kW(Maths.Normalize(thermal_conductivity, thickness));
		}


		// https://www.engineeringtoolbox.com/convective-heat-transfer-d_430.html - Convective Heat Transfer Coefficient for Air
		/// <summary>
		/// Convection heat transfer coefficient approximation in kW/(m²K) based on air velocity.<br/>
		/// Clamped to velocity between 2 m/s and 20 m/s
		/// </summary>
		/// <param name="relative_air_velocity">m/s</param>
		/// <returns>kW/(m²K)</returns>
		public static Power GetAirConvectionHTC(float relative_air_velocity)
		{
			relative_air_velocity = Maths.Clamp(relative_air_velocity, 2.00f, 20.00f);
			return Power.W(12.153f - 1.163f * relative_air_velocity + 11.63f * Maths.Sqrt(relative_air_velocity));
		}

		// https://www.omnicalculator.com/physics/flow-rate
		public static void VolumetricFlowRate(Area area, float velocity, out Volume volumetric_flow_rate)
		{
			volumetric_flow_rate = (float)(velocity * area);
		}

		// https://www.omnicalculator.com/physics/flow-rate
		public static void VolumetricFlowRate(Area area, out float velocity, Volume volumetric_flow_rate)
		{
			velocity = (float)(volumetric_flow_rate / area).FixNaN();
		}

		// https://www.omnicalculator.com/physics/flow-rate
		public static void VolumetricFlowRate(out Area area, float velocity, Volume volumetric_flow_rate)
		{
			area = ((float)(volumetric_flow_rate / velocity)).FixNaN();
		}



		// https://www.omnicalculator.com/physics/flow-rate
		public static void MassFlowRate(Density density, Volume volumetric_flow_rate, out float mass_flow_rate)
		{
			mass_flow_rate = volumetric_flow_rate * density;
		}

		// https://www.omnicalculator.com/physics/flow-rate
		public static void MassFlowRate(Density density, out Volume volumetric_flow_rate, float mass_flow_rate)
		{
			volumetric_flow_rate = (float)(mass_flow_rate / density);
		}

		// https://www.omnicalculator.com/physics/flow-rate
		public static void MassFlowRate(out Density density, Volume volumetric_flow_rate, float mass_flow_rate)
		{
			density = (float)(mass_flow_rate / volumetric_flow_rate);
		}


		// https://en.wikipedia.org/wiki/Passive_ventilation
		// https://en.wikipedia.org/wiki/Stack_effect#Induced_flow
		public static void VentilationAirFlow(float area, float height, Temperature temperature_inside, Temperature temperature_outside, out Volume volumetric_flow_rate, float discharge_coefficient = 0.62f)
		{
			volumetric_flow_rate = (discharge_coefficient * area * Maths.SqrtSigned(2.00f * reference_gravity * height * ((temperature_inside - temperature_outside) / temperature_inside))).FixNaN();
		}

		// https://en.wikipedia.org/wiki/Passive_ventilation
		// https://en.wikipedia.org/wiki/Stack_effect#Induced_flow
		public static void VentilationAirPressureDelta(Pressure atmospheric_pressure, Height height, Temperature temperature_inside, Temperature temperature_outside, out Pressure pressure_delta)
		{
			pressure_delta = ((float)(0.0342f * atmospheric_pressure * height * ((1.00f / temperature_outside) - (1.00f / temperature_inside)))).FixNaN();
		}



		//// https://www.engineeringtoolbox.com/convective-heat-source-d_1007.html
		//// https://www.engineeringtoolbox.com/convective-heat-transfer-d_430.html
		///// <summary>
		///// 
		///// </summary>
		///// <param name="area">m²</param>
		///// <param name="heat_coefficient">W/(m²K)</param>
		///// <param name="temperature_fluid">K</param>
		///// <param name="temperature_surface">K</param>
		///// <returns>Heat transfer rate in W</returns>
		//public static float CalculateHeatTransferConvection(float area, float heat_coefficient, Temperature temperature_fluid, Temperature temperature_surface)
		//{
		//	return heat_coefficient * area * (temperature_surface - temperature_fluid);
		//}

		//public static void TransferHeatConvectionAmbient(Temperature temperature_outside, ref Temperature temperature_inside, float mass_surface, float dt)
		//{

		//}

		// https://www.omnicalculator.com/physics/thermodynamic-processes




		//public static void Orifice(float )
		//{

		//}




		//public static void Bernoulli()
		//{

		//}

		//public static void TransferHeatAmbient(ref Temperature temperature, Temperature temperature_ambient, Mass mass, Energy specific_heat, Area area, Power htc, float dt)
		//{
		//	//var delta = temperature_ambient - temperature;
		//	var delta = temperature - temperature_ambient;
		//	var energy = (Energy)((delta.m_value / (1.00f / (htc.m_value * area.m_value))));

		//	//temperature += (energy.m_value / (mass.m_value * specific_heat.m_value)).FixNaN();

		//	//temperature += Maths.Normalize(energy, mass.m_value * specific_heat.m_value) * dt;

		//	//App.WriteLine(Maths.Normalize(energy, mass * specific_heat));
		//	//temperature.m_value.MoveTowards(temperature_ambient, Maths.Clamp(Maths.Normalize(energy, mass * specific_heat), -delta, delta), dt);
		//}

		public static void TransferHeatAmbient(ref Temperature temperature, Temperature temperature_ambient, Mass mass, Energy specific_heat, Area area, Power htc, float dt)
		{
			var delta = temperature_ambient - temperature;
			var energy = (Energy)((htc.m_value * area.m_value) * delta.m_value);

			temperature += (energy.m_value / (mass.m_value * specific_heat.m_value)).FixNaN() * dt;
		}

		public static void TransferHeatAmbientSimple(ref Temperature temperature, Temperature temperature_ambient, Energy heat_capacity, Power rate, float dt)
		{
			var delta = temperature_ambient - temperature;
			var energy = (Energy)(rate.m_value * delta.m_value);
			temperature += (energy.m_value / heat_capacity.m_value).FixNaN() * dt;
		}

		public static void TransferHeatAmbientSimpleFast(ref Temperature temperature, Temperature temperature_ambient, Energy heat_capacity_inv, Power rate, float dt)
		{
			var delta = temperature_ambient - temperature;
			var energy = rate.m_value * delta.m_value * dt;
			temperature.m_value.FMARef(energy, heat_capacity_inv.m_value);
		}


		//public static Power GetConvectionHeatTransfer(Temperature temperature_fluid, Temperature temperature_surface, Area area, Power htc)
		//{
		//	return (float)(htc * area * (temperature_surface - temperature_fluid));
		//}

		public static void TransferHeatConvection(ref Temperature temperature_a, ref Temperature temperature_b, Mass mass_a, Mass mass_b, Energy specific_heat_a, Energy specific_heat_b, Power htc, Area area, float dt)
		{
			var delta = temperature_b - temperature_a;
			var energy = (Energy)(((htc.m_value * area.m_value) * delta.m_value).FixNaN());

			temperature_a += (energy.m_value / (mass_a.m_value * specific_heat_a.m_value)).FixNaN() * dt;
			temperature_b -= (energy.m_value / (mass_b.m_value * specific_heat_b.m_value)).FixNaN() * dt;

			//var delta = MathF.Abs(temperature_b - temperature_a);
			//var energy = MathF.Abs(heat_flux * dt).WithSign(temperature_a < temperature_b);
			//temperature_a -= (float)Maths.Clamp(Maths.Normalize(energy, mass_a * specific_heat_a), -delta, delta);
			//temperature_b += (float)Maths.Clamp(Maths.Normalize(energy, mass_b * specific_heat_b), -delta, delta);
		}

		public static void TransferHeatDiffusion(ref Temperature temperature_a, ref Temperature temperature_b, Mass mass_a, Mass mass_b, Energy specific_heat, Distance distance, float thermal_conductivity, float dt)
		{
			var delta = temperature_b - temperature_a;
			var energy = (Energy)(thermal_conductivity * (delta.m_value / distance).m_value.FixNaN());

			temperature_a += (energy.m_value / (mass_a.m_value * specific_heat.m_value)).FixNaN() * dt;
			temperature_b -= (energy.m_value / (mass_b.m_value * specific_heat.m_value)).FixNaN() * dt;

			//var delta = MathF.Abs(temperature_b - temperature_a);
			//var energy = MathF.Abs(heat_flux * dt).WithSign(temperature_a < temperature_b);
			//temperature_a -= (float)Maths.Clamp(Maths.Normalize(energy, mass_a * specific_heat_a), -delta, delta);
			//temperature_b += (float)Maths.Clamp(Maths.Normalize(energy, mass_b * specific_heat_b), -delta, delta);
		}

		//public static void TransferHeatConduction(ref Temperature temperature_a, ref Temperature temperature_b, Mass mass_a, Mass mass_b, Energy specific_heat_a, Energy specific_heat_b, float , Area area, float dt)
		//{
		//	var delta = temperature_b - temperature_a;
		//	var energy = (Energy)((htc.m_value * area.m_value) * delta.m_value);

		//	temperature_a += (energy.m_value / (mass_a.m_value * specific_heat_a.m_value)).FixNaN() * dt;
		//	temperature_b -= (energy.m_value / (mass_b.m_value * specific_heat_b.m_value)).FixNaN() * dt;

		//	//var delta = MathF.Abs(temperature_b - temperature_a);
		//	//var energy = MathF.Abs(heat_flux * dt).WithSign(temperature_a < temperature_b);
		//	//temperature_a -= (float)Maths.Clamp(Maths.Normalize(energy, mass_a * specific_heat_a), -delta, delta);
		//	//temperature_b += (float)Maths.Clamp(Maths.Normalize(energy, mass_b * specific_heat_b), -delta, delta);
		//}

		//public static void EqualizeTemperature(ref Temperature temperature_a, ref Temperature temperature_b, Mass mass_a, Mass mass_b, Energy specific_heat_a, Energy specific_heat_b, float htc, Area area, float dt)
		//{
		//	var delta = MathF.Abs(temperature_b - temperature_a);
		//	var energy = MathF.Abs(heat_flux * dt).WithSign(temperature_a < temperature_b);
		//	temperature_a -= (float)Maths.Clamp(Maths.Normalize(energy, mass_a * specific_heat_a), -delta, delta);
		//	temperature_b += (float)Maths.Clamp(Maths.Normalize(energy, mass_b * specific_heat_b), -delta, delta);
		//}

		//public static void EqualizeTemperature(ref Temperature temperature_a, ref Temperature temperature_b, Mass mass_a, Mass mass_b, Energy specific_heat_a, Energy specific_heat_b, Power heat_flux, float dt)
		//{
		//	var delta = MathF.Abs(temperature_b - temperature_a);
		//	var energy = MathF.Abs(heat_flux * dt).WithSign(temperature_a < temperature_b);
		//	temperature_a -= (float)Maths.Clamp(Maths.Normalize(energy, mass_a * specific_heat_a), -delta, delta);
		//	temperature_b += (float)Maths.Clamp(Maths.Normalize(energy, mass_b * specific_heat_b), -delta, delta);
		//}



		//// https://www.engineeringtoolbox.com/convective-heat-source-d_1007.html
		//// https://www.engineeringtoolbox.com/convective-heat-transfer-d_430.html
		//public static float TransferHeatConvection(ref IMaterial.Substance substance, float area, float heat_coefficient, Temperature temperature_fluid, Temperature temperature_surface)
		//{
		//	return heat_coefficient * area * (temperature_surface - temperature_fluid);

		//	//var heat_capacity = mass * substance.specific_heat_capacity;
		//	//var heat_coefficient = 7.70f;
		//	//var heat_conductivity = 385.0;

		//	//var heat_transfer_speed_mult = 60.00f;

		//	//var heat_transfer_inner = heat_coefficient * area * (distillery_state.temperature_current - burner_state.current_temperature);
		//	//distillery_state.temperature_current -= (heat_transfer_inner * info.DeltaTime * heat_transfer_speed_mult) / heat_capacity;
		//}

		//public static void TransferHeat(ref Temperature temperature, float mass, float specific_heat, float dt)
		//{
		//	const float heat_transfer_speed_mult = 1.00f;

		//	var heat_coefficient_heater = ((float)((boiler.area_heater * substance_surface.thermal_conductivity) * (Maths.Normalize(substance_surface.thermal_conductivity * substance_surface.thermal_conductivity, burner_state.available_power))));

		//	var heat_transfer_outer = heat_coefficient_outer * boiler.area_surface * (temperature - Region.ambient_temperature);
		//	var heat_transfer_self = (substance_surface.thermal_conductivity * (burner_state.current_temperature - temperature));

		//	temperature -= Maths.Normalize((heat_transfer_outer - heat_transfer_self) * dt * heat_transfer_speed_mult, mass * specific_heat);
		//}

		//public static void CalculateGasPressure(ref double pressure, Temperature temperature, Volume volume, float moles)
		//{
		//	pressure = (double)((double)moles * molar_gas_constant * (double)temperature) / (double)volume;
		//}

		//public static void CalculateGasTemperature(double pressure, ref Temperature temperature, Volume volume, float moles)
		//{
		//	temperature = (float)((pressure * (double)volume) / ((double)moles * molar_gas_constant));
		//}

		//public static void CalculateGasVolume(double pressure, Temperature temperature, ref Volume volume, float moles)
		//{
		//	volume = (float)(((double)moles * molar_gas_constant * (double)temperature) / pressure);
		//}

		//public static void CalculateGasMoles(double pressure, Temperature temperature, Volume volume, ref float moles)
		//{
		//	moles = (float)((pressure * (double)volume) / (molar_gas_constant * (double)temperature));
		//}

		//public static float CalculateGasVolume(double pressure, Temperature temperature, float moles)
		//{
		//	return (moles * (float)molar_gas_constant * temperature) / volume;
		//}
	}
}
