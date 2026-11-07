
namespace TC2.Base.Components
{
	public static partial class Essence
	{
		public static partial class Bubble
		{
			[Flags]
			public enum Flags: byte
			{
				None = 0,

				Active = 1 << 0,
				Released = 1 << 1
			}

			public enum Type: byte
			{
				Undefined = 0,


			}

			[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Global | IComponent.Scope.Region)]
			public partial struct Data(): IComponent
			{
				public IEssence.Handle h_essence;
				public Essence.Bubble.Flags flags;
				public Essence.Bubble.Type type;


				[Save.NewLine]
				public float pull;
				public float amount;
				[Editor.Slider.Clamped(min: 0.00f, max: 16.00f, snap: 0.001f)]
				public float radius;
				public float centroid_cross;

				public sbyte collapse_count_rem = 4;
				public byte collapse_unused_00;
				public Texture.Handle h_tex_vert;
				public Texture.Handle h_tex_line;
				public Texture.Handle h_tex_glow;

				[Save.NewLine]
				[Editor.Picker.Position(relative: false)] public FixedArray4<Vec2f> positions;
				public FixedArray4<Vec2f> velocities;
				public FixedArray4<float> intensities;

				//[Save.NewLine]
				//[Editor.Picker.Position(relative: true)] public Vec2f offsets[0];
				//[Editor.Picker.Position(relative: true)] public Vec2f offsets[1];
				//[Editor.Picker.Position(relative: true)] public Vec2f offsets[2];
				//[Editor.Picker.Position(relative: true)] public Vec2f offsets[3];

				//[Save.NewLine]
				//[Editor.Picker.Direction] public Vec2f velocities[0];
				//[Editor.Picker.Direction] public Vec2f velocities[1];
				//[Editor.Picker.Direction] public Vec2f velocities[2];
				//[Editor.Picker.Direction] public Vec2f velocities[3];

				//[Save.NewLine]
				//public float intensity_a;
				//public float intensity_b;
				//public float intensity_c;
				//public float intensity_mid;
			}

			[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region)]
			public static void OnUpdate_A(ISystem.Info info, ref XorRandom random, ref Region.Data region, Entity entity,
			[Source.Owned] ref Transform.Data transform,
			[Source.Owned] ref Essence.Bubble.Data bubble)
			{
				//bubble.offsets[3] = (bubble.offsets[0] + bubble.offsets[1] + bubble.offsets[2]) * 0.333f;
				//transform.GetPositionNewRef() = transform.LocalToWorld(bubble.offsets[3]);

				//bubble.velocities[0] = ((bubble.offsets[1] - bubble.offsets[0]) + (bubble.offsets[2] - bubble.offsets[0])) * 0.50f;
				//bubble.velocities[1] = ((bubble.offsets[1] - bubble.offsets[0]) + (bubble.offsets[2] - bubble.offsets[1]));
				//bubble.velocities[2] = ((bubble.offsets[1] - bubble.offsets[2]) + (bubble.offsets[2] - bubble.offsets[0]));

				//bubble.velocities[3] = bubble.velocities[0] + bubble.velocities[1] + bubble.velocities[2];

				//bubble.offsets[0] += bubble.velocities[0] * App.fixed_update_interval_s * 4;
				//bubble.offsets[1] += bubble.velocities[1] * App.fixed_update_interval_s * 5;
				//bubble.offsets[2] += bubble.velocities[2] * App.fixed_update_interval_s * 5;
				//bubble.offsets[3] += bubble.velocities[3] * App.fixed_update_interval_s;

				if (bubble.flags.HasAny(Flags.Released))
				{
					var centroid_cross = bubble.centroid_cross.Abs();
					if (centroid_cross < 0.10f) //Maths.epsilon)
					{
						App.WriteLine("zap");

#if SERVER
						var amount = Maths.Clamp(bubble.amount / bubble.collapse_count_rem, 10, 500);
						Essence.SpawnCollapsingNode(ref region, bubble.h_essence, amount, bubble.positions[3], full_collapse: true);

						if (--bubble.collapse_count_rem <= 0)
						{
							entity.Delete();
						}
						//entity.Delete();
#endif
					}
				}
			}

			[Shitcode]
			[ISystem.Update.D(ISystem.Mode.Single, ISystem.Scope.Region)]
			public static void OnUpdate_D(ISystem.Info info, ref XorRandom random, ref Region.Data region, Entity entity,
			[Source.Owned] ref Transform.Data transform,
			[Source.Owned] ref Essence.Bubble.Data bubble)
			{
				if (bubble.flags.HasNone(Essence.Bubble.Flags.Active)) return;

				//p[3] = (p[0] + p[1] + p[2]) * 0.333f;
				//transform.GetPositionNewRef() = transform.LocalToWorld(p[3]);

				var dt = App.fixed_update_interval_s;
				var p = bubble.positions;
				var v = bubble.velocities;
				var i = bubble.intensities;

				var delta_ab = Vec2f.GetDelta(p[0] + (v[0] * dt), p[1] + (v[1] * dt));
				var delta_ac = Vec2f.GetDelta(p[0] + (v[0] * dt), p[2] + (v[2] * dt));
				var delta_bc = Vec2f.GetDelta(p[1] + (v[1] * dt), p[2] + (v[2] * dt));

				//var dir_ab = delta_ab.GetNormalized()

				//v[0] = ((p[1] - p[0]) + (p[2] - p[0])) * 0.50f;
				//v[1] = ((p[1] - p[0]) + (p[2] - p[1]));
				//v[2] = ((p[1] - p[2]) + (p[2] - p[0]));
				//v[3] = v[0] + v[1] + v[2];

				//p[0] += v[0] * Dt;
				//p[1] += v[1] * Dt;
				//p[2] += v[2] * Dt;
				//p[3] += v[3] * Dt;

				var vel_mult = bubble.pull;

				v[0] += delta_ab * dt * Maths.SumRatio(i[1], i[0]) * vel_mult;
				v[0] += delta_ac * dt * Maths.SumRatio(i[2], i[0]) * vel_mult;
				v[1] -= delta_ab * dt * Maths.SumRatio(i[0], i[1]) * vel_mult;
				v[1] += delta_bc * dt * Maths.SumRatio(i[2], i[1]) * vel_mult;
				v[2] -= delta_ac * dt * Maths.SumRatio(i[0], i[2]) * vel_mult;
				v[2] -= delta_bc * dt * Maths.SumRatio(i[1], i[2]) * vel_mult;

				p[0] += v[0] * dt;
				p[1] += v[1] * dt;
				p[2] += v[2] * dt;

				var centroid = p.Slice(3).GetCentroid(out bubble.centroid_cross);
				p[3] = centroid; // (p[0] + p[1] + p[2]) * 0.333f;

				bubble.positions = p;
				bubble.velocities = v;

#if CLIENT
				region.DrawDebugLine(p[0], p[3], color: Color32BGRA.Green);
				region.DrawDebugLine(p[1], p[3], color: Color32BGRA.Green);
				region.DrawDebugLine(p[2], p[3], color: Color32BGRA.Green);
#endif

				//entity.GetBody().SetPosition(centroid);
			}

			[ISystem.PostUpdate.A(ISystem.Mode.Single, ISystem.Scope.Region)]
			public static void OnPostUpdate_A(ISystem.Info info, ref XorRandom random, ref Region.Data region, Entity entity,
			[Source.Owned] in Transform.Data transform,
			[Source.Owned] ref Essence.Bubble.Data bubble)
			{

			}

			[ISystem.Add(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("initialized", true, Source.Modifier.Owned)]
			public static void OnAdd(ISystem.Info info, ref XorRandom random, ref Region.Data region, Entity entity,
			[Source.Owned] in Transform.Data transform,
			[Source.Owned] ref Essence.Bubble.Data bubble)
			{
				bubble.positions.Fill(transform.position);
			}


#if CLIENT
			[ISystem.PostRender(ISystem.Mode.Single, ISystem.Scope.Region)]
			public static void OnPostRender(ISystem.Info info, ref XorRandom random, ref Region.Data region, Entity entity,
			[Source.Owned] in Transform.Data transform,
			[Source.Owned] ref Essence.Bubble.Data bubble)
			{
				if (bubble.flags.HasNone(Essence.Bubble.Flags.Active)) return;
				if (!Camera.IsVisible(Camera.CullType.Rect3x, bubble.positions[3])) return;

				var centroid_cross = bubble.centroid_cross.Abs();
				//if (centroid_cross.IsZero() || !centroid_cross.IsFinite()) return;

				var intensity = Maths.Clamp(0.85f / (centroid_cross + 0.01f), 0.01f, 50.00f);
				var color_a = bubble.h_essence.GetEmitColor();

				static void DrawEdge(ref Region.Data region, ColorBGRA color_a, Texture.Handle h_texture, Vec2f pos, float scale, float intensity)
				{
					Particle.Spawn(ref region, new Particle.Data
					{
						texture = h_texture,
						lifetime = 0.04f,
						pos = pos,
						//vel = dir * speed_add,
						drag = 0.00f,
						frame_count = 1,
						frame_count_total = 1,
						frame_offset = 0,
						scale = scale,
						growth = 5.00f * scale,
						color_a = color_a,
						color_b = ColorBGRA.Black,
						glow = 20.00f * intensity
					});
				}

				static void DrawLine(ref Region.Data region, ColorBGRA color_a, Texture.Handle h_texture, Vec2f pos_a, Vec2f pos_b, float scale, float intensity, float ratio)
				{
					var dir = (pos_b - pos_a).vect.GetNormalized(out var dist);

					Particle.Spawn(ref region, new Particle.Data
					{
						texture = h_texture,
						lifetime = 0.50f,
						pos = Maths.Lerp(pos_a, pos_b, ratio),
						vel = Maths.Lerp(dir, -dir, ratio) * dist,
						//vel = dir * speed_add,
						drag = 0.01f,
						frame_count = 1,
						frame_count_total = 1,
						frame_offset = 0,
						scale = scale,
						stretch = new(dist * 0.50f, 0.75f),
						rotation = dir.GetAngleRadiansFast(),
						growth = 1,
						//force = dir * 50,
						color_a = color_a,
						color_b = ColorBGRA.Black,
						glow = 5.00f * intensity
					});

					//Particle.Spawn(ref region, new Particle.Data
					//{
					//	texture = h_texture,
					//	lifetime = 0.50f,
					//	pos = Maths.Lerp(pos_a, pos_b, ratio),
					//	vel = Maths.Lerp(dir, -dir, ratio) * dist * 2,
					//	//vel = dir * speed_add,
					//	drag = 0.0f,
					//	frame_count = 1,
					//	frame_count_total = 1,
					//	frame_offset = 0,
					//	scale = scale,
					//	stretch = new(4, 0.75f),
					//	rotation = dir.GetAngleRadiansFast(),
					//	growth = dist,
					//	//force = dir * 50,
					//	color_a = color_a,
					//	color_b = ColorBGRA.Black,
					//	glow = 15.00f * intensity
					//});
				}

				var pos_a = bubble.positions[0];
				var pos_b = bubble.positions[1];
				var pos_c = bubble.positions[2];

				DrawEdge(ref region, color_a: color_a, h_texture: bubble.h_tex_vert, pos: pos_a, scale: 1.00f * bubble.intensities[0], intensity: intensity * bubble.intensities[0]);
				DrawEdge(ref region, color_a: color_a, h_texture: bubble.h_tex_vert, pos: pos_b, scale: 1.00f * bubble.intensities[1], intensity: intensity * bubble.intensities[1]);
				DrawEdge(ref region, color_a: color_a, h_texture: bubble.h_tex_vert, pos: pos_c, scale: 1.00f * bubble.intensities[2], intensity: intensity * bubble.intensities[2]);

				DrawLine(ref region, color_a: color_a, h_texture: bubble.h_tex_line,
					pos_a: pos_a, pos_b: pos_b, scale: 2.00f, intensity: intensity * 0.50f,
					ratio: Maths.SumRatio(bubble.intensities[1], bubble.intensities[0]));

				DrawLine(ref region, color_a: color_a, h_texture: bubble.h_tex_line,
					pos_a: pos_a, pos_b: pos_c, scale: 2.00f, intensity: intensity * 0.50f,
					ratio: Maths.SumRatio(bubble.intensities[2], bubble.intensities[0]));

				DrawLine(ref region, color_a: color_a, h_texture: bubble.h_tex_line,
					pos_a: pos_c, pos_b: pos_b, scale: 2.00f, intensity: intensity * 0.50f,
					ratio: Maths.SumRatio(bubble.intensities[1], bubble.intensities[2]));

				//DrawLine(ref region, h_texture: bubble.h_tex_line, pos_a: pos_b, pos_b: pos_c, scale: 1.50f, intensity: intensity * 0.50f);

				DrawEdge(ref region, color_a: color_a, h_texture: bubble.h_tex_glow, pos: bubble.positions[3], scale: 1.00f, intensity: intensity * 0.50f);
			}
#endif
		}









	}
}
