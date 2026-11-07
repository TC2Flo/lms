namespace TC2.Base.Components
{
	public static class Ruler
	{
		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public struct Data(): IComponent
		{
			[Flags]
			public enum Flags: ushort
			{
				None = 0,
			}

			[Save.TrimEmpty]
			[Asset.Ignore] public FixedArray8<Vector2> positions;
			[Asset.Ignore] public byte positions_count;
			public byte positions_capacity = 8;

			public Sound.Handle h_sound_add;
			public Sound.Handle h_sound_rem;

			public Ruler.Data.Flags flags;

			[Save.Ignore, Net.Ignore] public float t_next_use;
		}

		public struct MarkRPC: Net.IRPC<Ruler.Data>
		{
			public Vector2? position;
			public byte index;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Ruler.Data data)
			{
				var sync = false;

				var positions_span = data.positions.Slice(data.positions_capacity);
				if (this.index < positions_span.Length)
				{
					positions_span[this.index] = this.position.GetValueOrDefault().SnapFloor(0.125f);
					positions_span.Compact();
					data.positions_count = (byte)positions_span.Length;

					var random = XorRandom.New(true);
					Sound.PlayGUI(ref rpc.connection, this.position.HasValue ? data.h_sound_add : data.h_sound_rem, volume: random.NextFloatRange(0.10f, 0.12f), pitch: random.NextFloatRange(0.98f, 1.02f));

					sync = true;

					//if (this.position.TryGetValue(out var position_value))
					//{
					//	sync = positions_span.TrySetAtIndex()
					//}
					//else
					//{
					//	sync = positions_span.TryRemoveAtResize(this.index);
					//}
				}


				if (sync)
				{
					data.Sync(rpc.entity);
				}
			}
#endif
		}


#if CLIENT
		public struct RulerGUI: IGUICommand
		{
			public Transform.Data transform;
			public Ruler.Data ruler;
			public Entity entity;
			public bool is_held;

			public void Draw()
			{
				using (var hud = GUI.Window.HUD("ruler.hud"u8))
				{
					if (hud.show)
					{
						ref var region = ref this.entity.GetRegionCommon();

						var kb = GUI.GetKeyboard();
						var mouse = GUI.GetMouse();
						var canvas_scale = region.GetWorldToCanvasScale();

						var positions_count = this.ruler.positions_count;

						var positions_span = this.ruler.positions.Slice(this.ruler.positions_capacity);
						var positions_span_trimmed = positions_span.WithLength(positions_count);

						var c_pos_prev = Vector2.Zero;

						var color_a = GUI.font_color_orange;
						var color_b = GUI.font_color_yellow;

						for (var i = 0; i < positions_count; i++)
						{
							var w_pos = positions_span_trimmed[i];
							var c_pos = region.WorldToCanvas(w_pos);

							if (i > 0)
							{
								if (i < positions_count - 1)
								{
									var dir_a = (positions_span_trimmed[i - 1] - positions_span_trimmed[i]).GetNormalized();
									var dir_b = (positions_span_trimmed[i + 1] - positions_span_trimmed[i]).GetNormalized();
									var dir_c = (dir_a + dir_b).GetNormalized();
									var delta_angle = Maths.DeltaAngle(dir_a.GetAngleRadians(), dir_b.GetAngleRadians());

									GUI.DrawArc(c_pos, dir_a, dir_b, color: color_a.WithAlpha(150), radius: 1.50f * canvas_scale);
									GUI.DrawTextCentered(delta_angle * Maths.rad2deg, format: "0.00°", position: region.WorldToCanvas(w_pos + dir_c * 0.75f), pivot: new Vector2(0.50f, 0.50f), size: 14, box_shadow: true, color: GUI.font_color_desc);
								}

								GUI.DrawLine(c_pos_prev, c_pos, color_a.WithAlpha(200)); //, canvas_scale * 0.0625f);
								GUI.DrawTextCentered(Vector2.Distance(positions_span_trimmed[i - 1], positions_span_trimmed[i]), format: "0.000m", position: ((c_pos + c_pos_prev) * 0.50f), pivot: new Vector2(0.50f, 0.50f), size: 14, box_shadow: true);
							}

							GUI.DrawRectFilled(region.WorldToCanvas(AABB.Centered(w_pos, new Vector2(0.25f))), color_a.WithAlpha(150));
							GUI.DrawRect(region.WorldToCanvas(AABB.Centered(w_pos, new Vector2(0.50f))), color_a);


							c_pos_prev = c_pos;
						}

						if (this.is_held)
						{
							var w_pos_new = mouse.position;
							var w_pos_new_snapped = w_pos_new.SnapFloor(0.125f);


							if (kb.GetKeyNow(Keyboard.Key.LeftShift))
							{
								if (positions_count > 0)
								{
									w_pos_new_snapped = w_pos_new_snapped.SnapAxis(positions_span_trimmed[positions_count - 1]);
								} 
							}

							var c_pos_new_snapped = region.WorldToCanvas(w_pos_new_snapped);
							var nearest_index = -1;

							if (positions_count > 0)
							{
								positions_span_trimmed.GetNearestIndex(w_pos_new_snapped, out nearest_index, out var nearest_dist_sq);
								if (nearest_dist_sq <= 2.Pow2())
								{
									var w_pos = positions_span_trimmed[nearest_index];
									GUI.DrawRect(region.WorldToCanvas(AABB.Centered(w_pos, new Vector2(0.50f, 0.50f))), Color32BGRA.Red, thickness: 2.00f);
								}
								else
								{
									nearest_index = -1;
								}
							}

							if (positions_count > 0)
							{
								if (positions_count > 1)
								{
									var dir_a = (positions_span_trimmed[positions_count - 2] - positions_span_trimmed[positions_count - 1]).GetNormalized();
									var dir_b = (w_pos_new_snapped - positions_span_trimmed[positions_count - 1]).GetNormalized();
									var dir_c = (dir_a + dir_b).GetNormalized();
									var delta_angle = Maths.DeltaAngle(dir_a.GetAngleRadians(), dir_b.GetAngleRadians());

									GUI.DrawArc(c_pos_prev, dir_a, dir_b, color: color_b.WithAlpha(150), radius: 1.50f * canvas_scale);
									GUI.DrawTextCentered(delta_angle * Maths.rad2deg, format: "0.00°", position: region.WorldToCanvas(positions_span_trimmed[positions_count - 1] + dir_c * 0.75f), pivot: new Vector2(0.50f, 0.50f), size: 14, box_shadow: true, color: GUI.font_color_desc);
								}

								GUI.DrawLine(c_pos_prev, c_pos_new_snapped, color_b.WithAlpha(200)); //, canvas_scale * 0.0625f);
								GUI.DrawTextCentered(Vector2.Distance(positions_span_trimmed[positions_count - 1], w_pos_new_snapped), format: "0.000m", position: ((c_pos_new_snapped + c_pos_prev) * 0.50f), pivot: new Vector2(0.50f, 0.50f), size: 14, box_shadow: true);
							}

							GUI.DrawRectFilled(region.WorldToCanvas(AABB.Centered(w_pos_new_snapped, new Vector2(0.250f))), color_b.WithAlpha(100));
							GUI.DrawRect(region.WorldToCanvas(AABB.Centered(w_pos_new_snapped, new Vector2(0.50f))), color_b.WithAlpha(255));
							GUI.DrawCross(c_pos_new_snapped, color_b.WithAlpha(200), radius: region.GetWorldToCanvasScale() * 4.00f);

							if (!GUI.IsHovered)
							{
								if (mouse.GetKeyDown(Mouse.Key.Left))
								{
									var rpc = new Ruler.MarkRPC()
									{
										index = (byte)positions_count,
										position = w_pos_new_snapped,
									};
									rpc.Send(this.entity);
								}
								else if (mouse.GetKeyDown(Mouse.Key.Right))
								{
									if (nearest_index >= 0)
									{
										var rpc = new Ruler.MarkRPC()
										{
											index = (byte)nearest_index,
											position = null,
										};
										rpc.Send(this.entity);
									}
								}
							}
						}
					}
				}
			}
		}

		[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("local", true, Source.Modifier.Parent)]
		public static void OnGUI(ISystem.Info info, Entity entity,
		[Source.Parent] in Interactor.Data interactor, [Source.Owned] ref Ruler.Data ruler, [Source.Owned] in Transform.Data transform, [Source.Owned] in Control.Data control)
		{
			var gui = new RulerGUI()
			{
				entity = entity,
				transform = transform,
				ruler = ruler,
				is_held = true
			};
			gui.Submit();
		}

		[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("local", true, Source.Modifier.Stored)]
		public static void OnGUI_Stored(ISystem.Info info, Entity entity,
		[Source.Owned] ref Ruler.Data ruler, [Source.Owned] in Transform.Data transform, [Source.Owned] in Control.Data control)
		{
			var gui = new RulerGUI()
			{
				entity = entity,
				transform = transform,
				ruler = ruler,
				is_held = false
			};
			gui.Submit();
		}
#endif

#if SERVER
		[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region), HasRelation(Source.Modifier.Owned, Relation.Type.Stored, false)]
		public static void Update(ISystem.Info info, Entity entity, ref XorRandom random,
		[Source.Owned] ref Ruler.Data ruler, [Source.Owned] in Transform.Data transform, [Source.Owned] in Control.Data control, [Source.Owned] in Body.Data body)
		{
			if (control.mouse.GetKey(Mouse.Key.Left) && info.WorldTime >= ruler.t_next_use)
			{

			}
		}
#endif
	}
}
