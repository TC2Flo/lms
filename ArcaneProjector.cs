namespace TC2.Base.Components
{
	public static partial class ArcaneProjector
	{
		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Region), IComponent.With<ArcaneProjector.State>]
		public partial struct Data(): IComponent
		{
			[Flags]
			public enum Flags: uint
			{
				None = 0,

			}

			public ArcaneProjector.Data.Flags flags;

			public float efficiency = 0.50f;

			//public Vector4 color = Essence.GetColor(Essence.Type.Motion);

			public float sound_volume_mult = 1.00f;
			public float sound_pitch_mult = 1.00f;

			public float charge_rate = 1.00f;
			public float charge_max = 1000.00f;

			public float focus_min = 0.02f;
			public float focus_max = 0.60f;
			public float focus_speed = 0.20f;

			public float focus_slider_distance = 2.00f;

			public float range_max = 150.00f;

			public float pulse_duration_min = 0.50f;
			public float pulse_duration_max = 3.00f;

			public float pulse_interval_min = 0.00f;
			public float pulse_interval_max = 3.00f;

			//[Statistics.Info("Modifier", description: "TODO: Desc", format: "{0:0.##}x", comparison: Statistics.Comparison.None, priority: Statistics.Priority.Medium)]
			//public float modifier = 1.00f;

			//[Statistics.Info("Force", description: "TODO: Desc", format: "{0:0.00}N", comparison: Statistics.Comparison.None, priority: Statistics.Priority.Medium)]
			//public float force = 0.00f;

			[Statistics.Info("Offset", description: "TODO: Desc", format: "{0:0.##}", comparison: Statistics.Comparison.None, priority: Statistics.Priority.Medium), Editor.Picker.Position(relative: true)]
			public Vector2 offset;

			[Editor.Picker.Position(relative: true)]
			public Vector2 offset_barrel;

			//[Save.Ignore, Net.Ignore] public float current_power = 0.00f;
		}

		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public partial struct State: IComponent
		{
			[Flags]
			public enum Flags: uint
			{
				None = 0,


			}

			public ArcaneProjector.State.Flags flags;
			public ArcaneProjector.State.Flags flags_editable;

			public float ratio;
			public float focus_target;

			public float pulse_duration;
			public float pulse_interval;

			[Asset.Ignore] public float focus_current;
			[Asset.Ignore] public float charge_current;
			[Asset.Ignore] public float pulse_current;

			[Save.Ignore, Net.Ignore] public float t_pulse_prev;
			[Save.Ignore, Net.Ignore] public float t_pulse_next;
		}

		public struct ConfigureRPC: Net.IRPC<ArcaneProjector.Data>
		{
			public float? ratio;
			public float? focus_target;

			public float? pulse_duration;
			public float? pulse_interval;

			public ArcaneProjector.State.Flags? flags;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref ArcaneProjector.Data projector_data)
			{
				ref var projector_state = ref rpc.entity.GetComponent<ArcaneProjector.State>();
				Assert.IsNotNull(ref projector_state);

				var sync_data = false;
				var sync_state = false;

				sync_state |= projector_state.ratio.TrySet(this.ratio, 0.00f, 1.00f);
				sync_state |= projector_state.focus_target.TrySet(this.focus_target, projector_data.focus_min, projector_data.focus_max);

				sync_state |= projector_state.pulse_duration.TrySet(this.pulse_duration, projector_data.pulse_duration_min, projector_data.pulse_duration_max);
				sync_state |= projector_state.pulse_interval.TrySet(this.pulse_interval, projector_data.pulse_interval_min, projector_data.pulse_interval_max);

				sync_state |= projector_state.flags.TrySetFlagMasked(this.flags, projector_state.flags_editable);

				if (sync_data) projector_data.Sync(rpc.entity, overwrite: true);
				if (sync_state) projector_state.Sync(rpc.entity, overwrite: true);
			}
#endif
		}


#if CLIENT
		public partial struct ArcaneProjectorGUI: IGUICommand
		{
			public Entity ent_arcane_projector;

			public ArcaneProjector.Data arcane_projector;
			public ArcaneProjector.State arcane_projector_state;
			public Essence.Container.Data essence_container;

			public Transform.Data transform;

			public void Draw()
			{
				using (var window = GUI.Window.Interaction("Arcane Projector"u8, this.ent_arcane_projector))
				{
					this.StoreCurrentWindowTypeID();
					if (window.show)
					{
						var rpc_edit = new ConfigureRPC();
						var dirty = false;

						using (var group_top = GUI.Group.New(size: new(GUI.RmX, 32)))
						{
						}

						using (var group = GUI.Group.New(size: new(GUI.RmX, GUI.RmY)))
						{
							using (var group_left = GUI.Group.New(size: new(32, GUI.RmY)))
							{
								if (GUI.SliderFloat("Focus"u8, this.arcane_projector_state.focus_target, ref rpc_edit.focus_target, this.arcane_projector.focus_min, this.arcane_projector.focus_max, size: GUI.Rm, vertical: true).RelayTo(ref dirty))
								{

								}
							}

							GUI.SameLine();

							using (var group_right = GUI.Group.New(size: new(GUI.RmX, GUI.RmY)))
							{

							}
						}

						if (dirty)
						{
							rpc_edit.Send(this.ent_arcane_projector);
						}
					}
				}
			}
		}

		[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnGUI(Entity ent_arcane_projector, [Source.Owned] in Interactable.Data interactable,
		[Source.Owned] in Transform.Data transform,
		[Source.Owned] in Essence.Container.Data essence_container,
		[Source.Owned] in ArcaneProjector.Data arcane_projector, [Source.Owned] in ArcaneProjector.State arcane_projector_state)
		{
			if (interactable.IsActive())
			{
				var gui = new ArcaneProjectorGUI()
				{
					ent_arcane_projector = ent_arcane_projector,

					arcane_projector = arcane_projector,
					arcane_projector_state = arcane_projector_state,
					essence_container = essence_container,

					transform = transform,
				};
				gui.Submit();
			}
		}
#endif

		[ISystem.Modified(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnModified(ISystem.Info info,
		[Source.Owned] ref ArcaneProjector.Data arcane_projector, [Source.Owned] ref ArcaneProjector.State arcane_projector_state)
		{
			arcane_projector_state.focus_target.ClampRef(arcane_projector.focus_min, arcane_projector.focus_max);
			arcane_projector_state.ratio.Clamp01Ref();

			arcane_projector_state.pulse_duration.ClampRef(arcane_projector.pulse_duration_min, arcane_projector.pulse_duration_max);
			arcane_projector_state.pulse_interval.ClampRef(arcane_projector.pulse_interval_min, arcane_projector.pulse_interval_max);
		}

		[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnUpdateA(ISystem.Info info, ref Region.Data region, ref XorRandom random, Entity entity,
		[Source.Owned] in Transform.Data transform, [Source.Owned] in Control.Data control, [Source.Owned] ref Body.Data body,
		[Source.Owned] ref Essence.Container.Data container,
		[Source.Owned] ref ArcaneProjector.Data arcane_projector, [Source.Owned] ref ArcaneProjector.State arcane_projector_state,
		[Source.Owned, Pair.Tag("barrel"), Optional(true)] ref Animated.Renderer.Data renderer_barrel,
		[Source.Owned, Pair.Tag("barrel"), Optional(true)] ref Sound.Emitter sound_emitter_barrel)
		{
			arcane_projector_state.focus_current.MoveTowardsDamped(arcane_projector_state.focus_target, arcane_projector.focus_speed * info.WorldDeltaTime, 0.14f, out var focus_delta);
			var focus_delta_abs = focus_delta.Abs();
			var focus_delta_abs_norm = focus_delta_abs.Normalize(arcane_projector.focus_speed * info.WorldDeltaTime);

			if (renderer_barrel.IsNotNull())
			{
				var offset = arcane_projector.offset_barrel;
				offset.X += ((1.00f - arcane_projector_state.focus_current) * arcane_projector.focus_slider_distance);
				renderer_barrel.offset = offset;
			}

			if (sound_emitter_barrel.IsNotNull())
			{

				sound_emitter_barrel.pitch_mult.LerpFMARef(0.60f + (focus_delta_abs_norm * 0.40f), 0.01f);
				sound_emitter_barrel.volume_mult.MoveTowards(focus_delta_abs_norm, 0.01f);
			}
		}

		[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnUpdateEffects(ISystem.Info info, ref Region.Data region, ref XorRandom random, Entity entity,
		[Source.Owned] in Transform.Data transform, [Source.Owned] in Control.Data control, [Source.Owned] ref Body.Data body,
		[Source.Owned] ref Essence.Container.Data container,
		[Source.Owned] ref ArcaneProjector.Data arcane_projector, [Source.Owned] ref ArcaneProjector.State arcane_projector_state,
		[Source.Owned, Pair.Tag("barrel"), Optional(true)] ref Animated.Renderer.Data renderer_barrel,
		[Source.Owned, Pair.Tag("barrel"), Optional(true)] ref Sound.Emitter sound_emitter_barrel)
		{

		}

		[ISystem.Update.B(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnUpdateB(ISystem.Info info, ref Region.Data region, ref XorRandom random, Entity entity,
		[Source.Owned] in Transform.Data transform, [Source.Owned] in Control.Data control, [Source.Owned] ref Body.Data body,
		[Source.Owned] ref Essence.Container.Data container,
		[Source.Owned] ref ArcaneProjector.Data arcane_projector, [Source.Owned] ref ArcaneProjector.State arcane_projector_state)
		{

		}

		[ISystem.Update.C(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnUpdateC(ISystem.Info info, ref Region.Data region, ref XorRandom random, Entity entity,
		[Source.Owned] in Transform.Data transform, [Source.Owned] in Control.Data control, [Source.Owned] ref Body.Data body,
		[Source.Owned] ref Essence.Container.Data container,
		[Source.Owned] ref ArcaneProjector.Data arcane_projector, [Source.Owned] ref ArcaneProjector.State arcane_projector_state)
		{
			var draw_debug = false;

			var dir = transform.GetDirection();
			var pos = transform.LocalToWorld(arcane_projector.offset);

			var focus = arcane_projector_state.focus_current;
			var focus_half = focus * 0.50f;
			var focus_quarter = focus_half * 0.50f;

			var focus_mod = MathF.Cos(focus).Clamp01(); //.Pow2();

			var dist_max_max = 200.00f;
			var dist_max = 120.00f * focus_mod;
			var dist = dist_max;

			var thickness_max = MathF.Tan(focus_half) * dist_max;

			var arc = new Arc(-focus_half, focus_half, dir.GetAngleRadians());

			//var los = region.IsInLineOfSight(pos, pos + (dir * dist_max), out var pos_end, radius: 0.00f, layer: Physics.Layer.Essence, group: entity);
			var los = region.IsLOS(pos, pos + (dir * dist_max), out var los_dist);
			los_dist = dist_max;

			var pos_end = pos + (dir * los_dist);

			//var thickness_max = MathF.Tan(focus_half) * dist_max;
			var thickness_los = MathF.Tan(focus_half) * los_dist;
			var thickness = thickness_los;

			//var linecast_radius = thickness * 0.50f; // 0.00f; // MathF.Tan(focus) * los_dist * 0.50f;
			var linecast_radius = thickness_max * 0.50f;
			var linecast_len = (los_dist);

			var ts = Timestamp.Now();
			var results_span = FixedArray.CreateSpan128NoInit<LinecastResult>(out var results_buffer);
			//scoped var results_span_sliced = Span<LinecastResult>.Empty;

			var results_span_a = results_span;
			Inner(ref region, entity, pos - (dir.GetPerpendicular(false) * linecast_radius * 0.0f), dir.RotateByRad(arc.a * 0.250f), linecast_radius * 0.750f, linecast_len, ref results_span_a);

			var results_span_b = results_span.Slice(results_span_a.Length);
			Inner(ref region, entity, pos - (dir.GetPerpendicular(true) * linecast_radius * 0.0f), dir.RotateByRad(arc.b * 0.250f), linecast_radius * 0.750f, linecast_len, ref results_span_b);

			var results_span_total = results_span.WithLength(results_span_a.Length + results_span_b.Length);

			static void Inner(ref Region.Data region, Entity entity, Vector2 pos, Vector2 dir, float radius, float dist, ref Span<LinecastResult> results_span)
			{
				var pos_start = pos + (dir * radius);
				var pos_end = pos + (dir * (Maths.Max(radius, dist) + 0.01f));

				if (region.TryLinecastAll(pos_start, pos_end, radius, ref results_span, group: entity))
				{
					//results_span.SortByDistance();
				}
			}

			results_span_total.SortByDistance();

			var ts_elapsed = ts.GetMilliseconds();

			var damage = 5000.00f * focus_mod;
			if (control.mouse.GetKeyDown(Mouse.Key.Left))
			{
				foreach (ref var result in results_span_total)
				{
#if SERVER
					if (result.layer.HasAny(Physics.Layer.Destructible | Physics.Layer.World))
					{
						//var alpha_inv = (1.50f - result.alpha).Clamp01();

						var hit_dir = (result.world_position_raw - pos).GetNormalized(out var hit_distance);
						var mult = Maths.Normalize01Fast(hit_distance, dist_max);
						var mult_inv = 1.00f - mult;

						var mult_pow2 = mult.Pow2();
						var mult_pow2_inv = 1.00f - mult_pow2;

						var damage_modified = damage * random.NextFloatRange(0.50f, 1.00f) * mult_pow2_inv;
						//App.WriteLine($"dist: {hit_distance}; mult: {mult}; mult_pow2: {mult_pow2}; mult_inv: {mult_inv}; mult_pow2_inv: {mult_pow2_inv}");
						Damage.Hit(ent_attacker: entity, ent_owner: entity, ent_target: result.entity,
							position: result.world_position + random.NextUnitVector2Range(0.10f, 0.80f),
							velocity: (dir * random.NextFloatRange(5, 60 * mult_pow2_inv)).RotateByRad(random.NextFloatRange(-0.20f, 0.20f)),
							normal: dir.GetPerpendicular().RotateByRad(random.NextFloatRange(-0.20f, 0.20f)),
							damage_integrity: damage_modified, damage_durability: damage_modified, damage_terrain: damage_modified * random.NextFloatRange(0.10f, 0.40f),
							target_material_type: result.material_type, damage_type: Damage.Type.Motion_Impulse,
							impulse: 1800 * (0.70f + (mult_pow2_inv * 0.30f)),
							size: Maths.Max(4.00f, linecast_radius) * MathF.FusedMultiplyAdd(mult, 0.50f, 0.50f) * random.NextFloatRange(0.70f, 1.50f) * 2.50f);
					}
#endif
				}

				App.WriteLine("bap");
			}

			if (draw_debug)
			{
#if CLIENT
				region.DrawDebugCircle(pos_end, 0.50f, los ? Color32BGRA.Green : Color32BGRA.Red, filled: true);
				region.DrawDebugText(pos,
					$"sin: {focus_mod}\n" +
					$"results: {results_span_total.Length}/{results_span.Length}\n" +
					$"dist: {dist}\n" +
					$"thickness: {thickness}/{thickness_los}/{thickness_max}\n" +
					$"linecast_radius: {linecast_radius}\n" +
					$"linecast_len: {linecast_len}\n" +
					$"los_dist: {los_dist}\n" +
					$"elapsed: {ts_elapsed:0.0000} ms\n" +
					"", color: Color32BGRA.White);
#endif

#if CLIENT

				var arc_tmp = arc;
				arc_tmp.a *= 0.250f;
				arc_tmp.b *= 0.250f;

				//region.DrawDebugArc(pos - (dir * linecast_radius * 2), arc_tmp, dist, Color32BGRA.Yellow, thickness: linecast_radius * region.GetWorldToCanvasScale());
				region.DrawDebugArc(pos, arc_tmp, dist, Color32BGRA.Yellow.WithAlpha(50), thickness: linecast_radius * 1.50f * region.GetWorldToCanvasScale());
				//region.DrawDebugArc(pos, arc, dist, Color32BGRA.Yellow);
				region.DrawDebugDir(pos, dir * dist, Color32BGRA.Yellow, 1.00f);
				//region.DrawDebugDir(pos, dir * dist, Color32BGRA.Yellow.WithAlpha(50), thickness * 2 * region.GetWorldToCanvasScale());

				//region.DrawDebugArc(pos, arc, los_dist, Color32BGRA.Red.WithAlpha(100), thickness: 4.00f);
				//region.DrawDebugDir(pos, dir * los_dist, Color32BGRA.Red.WithAlpha(50), linecast_radius * 2 * region.GetWorldToCanvasScale());
#endif
			}
		}
	}
}
