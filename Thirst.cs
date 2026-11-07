namespace TC2.Base.Components
{
	public static partial class Thirst
	{
		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Region), IComponent.With<Thirst.State>()]
		public partial struct Data : IComponent, IOverridable
		{
			[Statistics.Info("Thirst Capacity", description: "Maximum water capacity", format: "{0:0.##}", comparison: Statistics.Comparison.Higher, priority: Statistics.Priority.High)]
			public float capacity = 100.00f;
			
			[Statistics.Info("Consumption Rate", description: "Rate at which thirst decreases per second", format: "{0:0.##}/s", comparison: Statistics.Comparison.Lower, priority: Statistics.Priority.Medium)]
			public float usage = 0.14f; // Slightly higher default usage for thirst compared to hunger

			public Data() { }
		}

		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public partial struct State : IComponent
		{
			[Asset.Ignore] public float stored = 100.00f;
			[Asset.Ignore] public float current_norm = 1.00f;

			[Save.Ignore, Asset.Ignore, Net.Ignore] public float t_next_dehydration_damage;

			public State() { }
		}

		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Region)]
		public partial struct Hydration : IComponent
		{
			[Statistics.Info("Hydration Value", description: "Amount of water/fluid restored", format: "{0:0.##}", comparison: Statistics.Comparison.Higher, priority: Statistics.Priority.High)]
			public float hydration_value = 50.00f;

			[Statistics.Info("Healing Power", description: "Points de vie restorés à la consommation", format: "{0:0}", comparison: Statistics.Comparison.Higher, priority: Statistics.Priority.High)]
			public float power = 10.00f;

			[Statistics.Info("Pain Relief", description: "Réduction de la douleur organique et réveil", format: "{0:0}", comparison: Statistics.Comparison.Lower, priority: Statistics.Priority.Medium)]
			public float pain_relief = 500.00f;

			public Hydration() { }
		}

#if SERVER
		// Thirst only on the TORSO (a single source of truth)
		[ISystem.Add(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnAddOrganic(Entity entity, 
			[Source.Owned] ref Organic.Data organic,
			[Source.Owned] in Torso.Data torso)
		{
			ref var thirst_state = ref entity.GetOrAddComponent<Thirst.State>(sync: true);
			ref var thirst_data  = ref entity.GetOrAddComponent<Thirst.Data>(sync: true);
			
			if (thirst_state.stored <= 0.00f && thirst_state.current_norm <= 0.00f)
			{
				thirst_state.stored = thirst_data.capacity;
				thirst_state.current_norm = 1.00f;
				entity.MarkModified<Thirst.State>(sync: true);
			}
		}

		[ISystem.Event<Consumable.ConsumeEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnConsumeHydration(ISystem.Info info, Entity entity, ref Consumable.ConsumeEvent data,
			[Source.Owned] in Thirst.Hydration hydration_src, [Source.Owned] in Consumable.Data consumable)
		{
			var ent_target = data.ent_organic;
			if (!ent_target.IsAlive()) return;

			var mod_mult = data.amount_modifier;
			if (mod_mult <= 0.00f) return;

			// 1. Restore Thirst (only if the target has the Thirst component = torso)
			ref var thirst_state = ref ent_target.GetComponent<Thirst.State>();
			ref var thirst_data  = ref ent_target.GetComponent<Thirst.Data>();

			float restored = 0.00f;
			float capacity = 100.00f;

			if (thirst_state.IsNotNull() && thirst_data.IsNotNull())
			{
				restored = hydration_src.hydration_value * mod_mult;
				capacity = thirst_data.capacity;

				thirst_state.stored = Maths.Clamp(thirst_state.stored + restored, 0.00f, capacity);
				thirst_state.current_norm = Maths.Normalize01Fast(thirst_state.stored, capacity);

				ent_target.MarkModified<Thirst.State>(sync: true);
			}

			// 2. Heal
			ref var health = ref ent_target.GetComponent<Health.Data>();
			if (health.IsNotNull() && health.max > 0.00f)
			{
				var modified_power = hydration_src.power * mod_mult;
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
				var relief = hydration_src.pain_relief * mod_mult;

				organic_state.pain            = Maths.Max(0.00f, organic_state.pain - relief);
				organic_state.pain_shared_old = Maths.Max(0.00f, organic_state.pain_shared_old - relief);

				organic_state.stun      = Maths.Max(0.00f, organic_state.stun - relief * 0.01f);
				organic_state.stun_norm = Maths.Normalize01Fast(organic_state.stun, 1.00f);

				organic_state.unconscious_time = Maths.Max(0.00f, organic_state.unconscious_time - relief * 0.02f);

				var thirst_ratio = Maths.Normalize01Fast(restored, capacity);
				var consciousness_boost = Maths.Lerp(0.25f, 0.85f, thirst_ratio) * mod_mult;

				organic_state.consciousness_shared     = Maths.Clamp(organic_state.consciousness_shared + consciousness_boost, 0.00f, 1.00f);
				organic_state.consciousness_shared_new = organic_state.consciousness_shared;

				organic_state.Modified(ent_target, sync: true);
			}
		}
#endif

		[ISystem.EarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("dead", false, Source.Modifier.Owned)]
		public static void OnUpdateThirst(ISystem.Info info, Entity entity, ref Region.Data region,
			[Source.Owned] in Thirst.Data thirst, [Source.Owned] ref Thirst.State thirst_state,
			[Source.Owned, Override] ref Organic.Data organic_override, [Source.Owned] ref Organic.State organic_state)
		{
			// Consumption
			if (thirst_state.stored > 0.00f)
			{
				thirst_state.stored.MoveTowards(0.00f, thirst.usage * App.fixed_update_interval_s);
			}

			thirst_state.current_norm = Maths.Normalize01Fast(thirst_state.stored, thirst.capacity);
			float factor = thirst_state.current_norm;

			// ========== 0 % → LETHAL (DEHYDRATION) ==========
			if (factor <= 0.00f)
			{
				// Extreme pain (felt throughout the entire body)
				organic_state.pain            += 500.00f * App.fixed_update_interval_s;
				organic_state.pain_shared_old += 500.00f * App.fixed_update_interval_s;

				// Causes loss of consciousness very quickly due to dehydration
				organic_state.unconscious_time += 10.00f * App.fixed_update_interval_s;

				// Override Consciousness
				organic_state.consciousness_shared     = Maths.Min(organic_state.consciousness_shared, 0.03f);
				organic_state.consciousness_shared_new = organic_state.consciousness_shared;

#if SERVER
				// Additional physical damage (torso) from extreme dehydration
				ref var health = ref entity.GetComponent<Health.Data>();
				if (health.IsNotNull())
				{
					health.durability = Maths.Clamp(health.durability - 0.45f, 0.00f, 1.00f);
					health.integrity  = Maths.Clamp(health.integrity  - 0.35f, 0.00f, 1.00f);
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

					organic_override.regeneration *= Maths.Lerp(0.25f, 1.00f, t);
					organic_override.strength     *= Maths.Lerp(0.70f, 1.00f, t);
					organic_override.dexterity    *= Maths.Lerp(0.75f, 1.00f, t);
					organic_override.coordination *= Maths.Lerp(0.80f, 1.00f, t);
					organic_override.motorics     *= Maths.Lerp(0.75f, 1.00f, t);

					organic_state.pain += Maths.Lerp(6.00f, 0.00f, t) * App.fixed_update_interval_s;
				}
				else if (factor >= 0.99f)
				{
					organic_override.regeneration *= 1.15f;
					organic_override.strength     *= 1.03f;
					organic_override.dexterity    *= 1.02f;
				}
			}
		}

#if CLIENT
		[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("local", true, Source.Modifier.Shared)]
		public static void OnGUI(ISystem.Info info, Entity entity, 
			[Source.Shared] in Player.Data player, 
			[Source.Owned] in Thirst.State thirst_state)
		{
			if (player.ent_controlled != entity) return;

			var norm = thirst_state.current_norm;
			var stored = thirst_state.stored;

			Color32BGRA color;
			string status;

			if (norm <= 0.10f)
			{
				color = new Color32BGRA(255, 40, 140, 255);  // deep blue/red alert tone
				status = "DEHYDRATED";
			}
			else if (norm < 0.30f)
			{
				color = new Color32BGRA(255, 90, 180, 255);  // light blue / cyan warning
				status = "THIRSTY";
			}
			else
			{
				color = new Color32BGRA(255, 40, 160, 255);  // vibrant blue
				status = "OK";
			}

			IStatusEffect.ScheduleDraw(new()
			{
				icon = "ui_icon_effect.water", // Assuming a water icon path exists
				value = string.Format("{0:0.0}/100\n{1}", stored, status),
				text_color = color,
				name = string.Format("Hydration\n{0}", status)
			});
		}
#endif
	}
}