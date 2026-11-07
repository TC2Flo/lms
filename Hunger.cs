namespace TC2.Base.Components
{
	public static partial class Hunger
	{
		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Region), IComponent.With<Hunger.State>()]
		public partial struct Data : IComponent, IOverridable
		{
			[Statistics.Info("Hunger Capacity", description: "Maximum stomach capacity", format: "{0:0.##}", comparison: Statistics.Comparison.Higher, priority: Statistics.Priority.High)]
			public float capacity = 100.00f;
			
			[Statistics.Info("Consumption Rate", description: "Rate at which hunger decreases per second", format: "{0:0.##}/s", comparison: Statistics.Comparison.Lower, priority: Statistics.Priority.Medium)]
			public float usage = 0.11f;

			public Data() { }
		}

		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public partial struct State : IComponent
		{
			[Asset.Ignore] public float stored = 100.00f;
			[Asset.Ignore] public float current_norm = 1.00f;

			[Save.Ignore, Asset.Ignore, Net.Ignore] public float t_next_starvation_damage;

			public State() { }
		}

		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Region)]
		public partial struct Nutrition : IComponent
		{
			[Statistics.Info("Nutritional Value", description: "Number of calories", format: "{0:0.##}", comparison: Statistics.Comparison.Higher, priority: Statistics.Priority.High)]
			public float nutrition_value = 50.00f;

			[Statistics.Info("Healing Power", description: "Points de vie restorés à la consommation", format: "{0:0}", comparison: Statistics.Comparison.Higher, priority: Statistics.Priority.High)]
			public float power = 25.00f;

			[Statistics.Info("Pain Relief", description: "Réduction de la douleur organique et réveil", format: "{0:0}", comparison: Statistics.Comparison.Lower, priority: Statistics.Priority.Medium)]
			public float pain_relief = 1000.00f;

			public Nutrition() { }
		}

#if SERVER
		// Hunger only on the TORSO (a single source of truth)
		[ISystem.Add(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnAddOrganic(Entity entity, 
			[Source.Owned] ref Organic.Data organic,
			[Source.Owned] in Torso.Data torso)
		{
			ref var hunger_state = ref entity.GetOrAddComponent<Hunger.State>(sync: true);
			ref var hunger_data  = ref entity.GetOrAddComponent<Hunger.Data>(sync: true);
			
			if (hunger_state.stored <= 0.00f && hunger_state.current_norm <= 0.00f)
			{
				hunger_state.stored = hunger_data.capacity;
				hunger_state.current_norm = 1.00f;
				entity.MarkModified<Hunger.State>(sync: true);
			}
		}

		[ISystem.Event<Consumable.ConsumeEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnConsumeNutrition(ISystem.Info info, Entity entity, ref Consumable.ConsumeEvent data,
			[Source.Owned] in Hunger.Nutrition nutrition_src, [Source.Owned] in Consumable.Data consumable)
		{
			var ent_target = data.ent_organic;
			if (!ent_target.IsAlive()) return;

			var mod_mult = data.amount_modifier;
			if (mod_mult <= 0.00f) return;

			// 1. Restore Hunger (only if the target has the Hunger component = torso)
			ref var hunger_state = ref ent_target.GetComponent<Hunger.State>();
			ref var hunger_data  = ref ent_target.GetComponent<Hunger.Data>();

			float restored = 0.00f;
			float capacity = 100.00f;

			if (hunger_state.IsNotNull() && hunger_data.IsNotNull())
			{
				restored = nutrition_src.nutrition_value * mod_mult;
				capacity = hunger_data.capacity;

				hunger_state.stored = Maths.Clamp(hunger_state.stored + restored, 0.00f, capacity);
				hunger_state.current_norm = Maths.Normalize01Fast(hunger_state.stored, capacity);

				ent_target.MarkModified<Hunger.State>(sync: true);
			}

			// 2. Heal
			ref var health = ref ent_target.GetComponent<Health.Data>();
			if (health.IsNotNull() && health.max > 0.00f)
			{
				var modified_power = nutrition_src.power * mod_mult;
				if (modified_power > 0.00f)
				{
					health.durability = Maths.Clamp(health.durability + (modified_power / health.max), 0.00f, 1.00f);
					health.integrity  = Maths.Clamp(health.integrity  + (modified_power / health.max) * 0.5f, 0.00f, 1.00f);
					ent_target.MarkModified<Health.Data>(sync: true);
				}
			}

			// 3. Pain / stun / consciousness
			ref var organic_state = ref ent_target.GetComponent<Organic.State>();
			if (organic_state.IsNotNull())
			{
				var relief = nutrition_src.pain_relief * mod_mult;

				organic_state.pain            = Maths.Max(0.00f, organic_state.pain - relief);
				organic_state.pain_shared_old = Maths.Max(0.00f, organic_state.pain_shared_old - relief);

				organic_state.stun      = Maths.Max(0.00f, organic_state.stun - relief * 0.01f);
				organic_state.stun_norm = Maths.Normalize01Fast(organic_state.stun, 1.00f);

				organic_state.unconscious_time = Maths.Max(0.00f, organic_state.unconscious_time - relief * 0.02f);

				var hunger_ratio = Maths.Normalize01Fast(restored, capacity);
				var consciousness_boost = Maths.Lerp(0.25f, 0.85f, hunger_ratio) * mod_mult;

				organic_state.consciousness_shared     = Maths.Clamp(organic_state.consciousness_shared + consciousness_boost, 0.00f, 1.00f);
				organic_state.consciousness_shared_new = organic_state.consciousness_shared;

				organic_state.Modified(ent_target, sync: true);
			}
		}
#endif

		[ISystem.EarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("dead", false, Source.Modifier.Owned)]
		public static void OnUpdateHunger(ISystem.Info info, Entity entity, ref Region.Data region,
			[Source.Owned] in Hunger.Data hunger, [Source.Owned] ref Hunger.State hunger_state,
			[Source.Owned, Override] ref Organic.Data organic_override, [Source.Owned] ref Organic.State organic_state)
		{
			// Consumption
			if (hunger_state.stored > 0.00f)
			{
				hunger_state.stored.MoveTowards(0.00f, hunger.usage * App.fixed_update_interval_s);
			}

			hunger_state.current_norm = Maths.Normalize01Fast(hunger_state.stored, hunger.capacity);
			float factor = hunger_state.current_norm;

// ========== 0 % → LETHAL  ==========
if (factor <= 0.00f)
{
	// Extreme pain (felt throughout the entire body)
	organic_state.pain            += 400.00f * App.fixed_update_interval_s;
	organic_state.pain_shared_old += 400.00f * App.fixed_update_interval_s;

	// Causes loss of consciousness very quickly
	organic_state.unconscious_time += 8.00f * App.fixed_update_interval_s;

	// Override Consciousness
	organic_state.consciousness_shared     = Maths.Min(organic_state.consciousness_shared, 0.05f);
	organic_state.consciousness_shared_new = organic_state.consciousness_shared;

#if SERVER
	// Additional physical damage (torso)
	ref var health = ref entity.GetComponent<Health.Data>();
	if (health.IsNotNull())
	{
		health.durability = Maths.Clamp(health.durability - 0.35f, 0.00f, 1.00f);
		health.integrity  = Maths.Clamp(health.integrity  - 0.30f, 0.00f, 1.00f);
		entity.MarkModified<Health.Data>(sync: true);
	}

	organic_state.Modified(entity, sync: true);
#endif
}
			// ========== ≥ 1 % → NO damage ==========
			else
			{
				if (factor < 0.30f)
				{
					float t = Maths.Normalize01(factor, 0.01f, 0.30f);

					organic_override.regeneration *= Maths.Lerp(0.35f, 1.00f, t);
					organic_override.strength     *= Maths.Lerp(0.78f, 1.00f, t);
					organic_override.dexterity    *= Maths.Lerp(0.82f, 1.00f, t);
					organic_override.coordination *= Maths.Lerp(0.85f, 1.00f, t);
					organic_override.motorics     *= Maths.Lerp(0.80f, 1.00f, t);

					organic_state.pain += Maths.Lerp(4.00f, 0.00f, t) * App.fixed_update_interval_s;
				}
				else if (factor >= 0.99f)
				{
					organic_override.regeneration *= 1.12f;
					organic_override.strength     *= 1.05f;
					organic_override.dexterity    *= 1.03f;
					organic_override.coordination *= 0.03f;
					organic_override.motorics     *= 0.03f;
				}
			}
		}

#if CLIENT
		[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("local", true, Source.Modifier.Shared)]
		public static void OnGUI(ISystem.Info info, Entity entity, 
			[Source.Shared] in Player.Data player, 
			[Source.Owned] in Hunger.State hunger_state)
		{
			if (player.ent_controlled != entity) return;

			var norm = hunger_state.current_norm;
			var stored = hunger_state.stored;

			Color32BGRA color;
			string status;

			if (norm <= 0.10f)
			{
				color = new Color32BGRA(255, 255, 40, 40);   // rouge
				status = "STARVING";
			}
			else if (norm < 0.30f)
			{
				color = new Color32BGRA(255, 255, 140, 40);  // orange
				status = "LOW";
			}
			else
			{
				color = new Color32BGRA(255, 40, 220, 40);   // vert
				status = "OK";
			}

			IStatusEffect.ScheduleDraw(new()
			{
				icon = "ui_icon_effect.food",
				value = string.Format("{0:0.0}/100\n{1}", stored, status),
				text_color = color,
				name = string.Format("Calories\n{0}", status)
			});
		}
#endif
	}
}