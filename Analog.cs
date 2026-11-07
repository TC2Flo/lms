namespace TC2.Base.Components
{
	public static partial class Analog
	{
		extension(ref Analog.Graph.Data v)
		{
			//public ref FixedArray64<float> this[Signal.Channel channel]
			//{
			//	get
			//	{
			//		switch (((uint)channel) & 0b11)
			//		{
			//			case 0: return ref v.values_0;
			//			case 1: return ref v.values_1;
			//			case 2: return ref v.values_2;
			//			case 3: return ref v.values_3;
			//			default: throw new InvalidOperationException();
			//		}
			//	}
			//}

			// TODO: wait until C# supports extension indexers
			public ref FixedArray64<float> get_Item(Signal.Channel channel)
			{
				switch (((uint)channel) & 0b11)
				{
					case 0: return ref v.values_0;
					case 1: return ref v.values_1;
					case 2: return ref v.values_2;
					case 3: return ref v.values_3;
					default: throw new InvalidOperationException();
				}
			}
		}


#if CLIENT
		public partial struct AnalogGUI: IGUICommand
		{
			public Entity entity;
			//public Analog.Writer.Data writer;
			public Transform.Data transform;
			public Analog.Relay.Data relay;
			public Analog.Graph.Data graph;

			public const int resolution_max = 60 * App.tickrate;

			public static int histogram_index;
			public static readonly float[][] histogram =
			{
				new float[resolution_max],
				new float[resolution_max],
				new float[resolution_max],
				new float[resolution_max],
			};

			public void Draw()
			{
				using (var window = GUI.Window.Interaction("Analog"u8, this.entity))
				{
					this.StoreCurrentWindowTypeID(order: -600);
					//using (var dock = GUI.Dock.New("analog.radio"))

					using (var embed_modules = GUI.Embed.New("analog.modules"u8, entity: this.entity, size: GUI.Rm))
					{
						//App.WriteLine(GUI.GetCurrentLineSize());

						if (window.show)
						{
							ref var region = ref this.entity.GetRegion();

							var resolution = this.graph.values_0.Length; // App.tickrate * 5;

							//Span<float> values = stackalloc float[resolution];
							//var timestep = App.fixed_update_interval_s / (1.00f / 60.00f);

							using (var group = GUI.Group.New(size: new Vector2(GUI.RmX >= 384 ? 224 : GUI.RmX, 128)))
							{

								//var scale = 1.00f; // group.GetInnerRect().GetHeight() * -0.1250f;
								//var lerp = Vulkan.GetCurrentLerp();

								//GUI.Title($"{histogram_index_prev} {histogram_index} {histogram_index - histogram_index_prev}");

								var signal = this.relay.signal_current;
								for (var i = 0; i < Signal.Count; i++)
								{
									//ref var bind = ref this.writer.binds[i];
									//var graph = histogram[i];

									ref readonly var values = ref this.graph.get_Item((Signal.Channel)i); // = ref graph.
									//if (window.appearing)
									//{
									//	graph.Clear();
									//}


									//var lerp = (resolution - histogram_index) * timestep;

									//values[0] = graph[histogram_index];

									//var index_prev = histogram_index;
									//values[0] = graph[histogram_index];
									//var index_prev = histogram_index;

									//var value_prev = graph[Maths.Repeat(histogram_index, resolution)];
									//var time_accumulator = 0.00f; // (resolution - histogram_index) * timestep;

									//var index_prev = Maths.Repeat(resolution + histogram_index - 0, resolution);
									//var value_prev = graph[index_prev];
									//values[0] = Maths.Clamp(signal[i], -1.00f, 1.00f);

									//for (var j = 1; j < resolution; j++)
									//{
									//	var index = Maths.Repeat(resolution + histogram_index - j, resolution);
									//	//var index_next = Maths.Repeat(resolution - histogram_index + j + 1, resolution);
									//	//var value = Maths.Lerp(graph[index_prev], graph[index], 1);
									//	var value = Maths.Clamp(graph[index], -1.00f, 1.00f);

									//	values[j] = value;
									//	value_prev = value;
									//	time_accumulator += timestep;
									//}
									//values[resolution - 1] = this.signal.channels_current[i];

									//var index_first = Maths.Repeat(histogram_index - resolution, resolution);

									//var offset = 0;
									//var lerp = (float)((region.GetCurrentTick() * (double)timestep) % ((double)timestep));

									//var index_prev = histogram_index_prev;

									using (var group_row = GUI.Group.New(size: new(GUI.RmX, group.size.Y * 0.25f), padding: new(0, 0)))
									{
										using (var group_info = GUI.Group.New(size: new(64, GUI.RmY), padding: new(6, 2)))
										{
											group_info.DrawBackground(GUI.tex_slot);

											GUI.TextCentered($"{signal[i].FixZeroSign():+0.0000;-0.0000}", color: signal[i] == 0.00f ? GUI.font_color_default : signal[i].IsPositive() ? GUI.font_color_green : GUI.font_color_red, pivot: new Vector2(0.50f, 0.50f), offset: new(0, 0));
										}

										GUI.SameLine();

										using (var group_graph = GUI.Group.New(size: GUI.Rm, padding: new(6, 6)))
										{
											group_graph.DrawBackground(GUI.tex_frame_sided_01, color: GUI.col_frame);
											var color = Signal.GetColor((Signal.Channel)i);

											var stride = GUI.RmX / resolution;

											var rect_inner = group_graph.GetInnerRect();
											var pos_base = new Vector2(rect_inner.a.X, Maths.Avg(rect_inner.a.Y, rect_inner.b.Y));
											//var value_prev = graph[index_prev] * scale;

											//var first = true;

											//GUI.DrawCircleFilled(pos_base + new Vector2(stride * histogram_index, 0.00f), 4.00f, color, segments: 3, layer: GUI.Layer.Window);
											//GUI.DrawCircleFilled(pos_base + new Vector2(stride * (resolution - histogram_index), 0.00f), 4.00f, color, segments: 3, layer: GUI.Layer.Window);

											var scale = rect_inner.GetHeight() * -0.50f;
											var index_offset_base = (uint)this.graph.offsets[i];
											var index_current = 0u;

											for (var j = index_offset_base; j < values.ULength; j++, index_current++)
											{
												GUI.DrawLine(
													a: pos_base + new Vector2(stride * index_current, values[j] * scale),
													b: pos_base + new Vector2(stride * (index_current + 1), values[j + 1] * scale),
													color: color,
													layer: GUI.Layer.Window);
											}

											for (var j = 0; j < index_offset_base; j++, index_current++)
											{
												GUI.DrawLine(
													a: pos_base + new Vector2(stride * index_current, values[j] * scale),
													b: pos_base + new Vector2(stride * (index_current + 1), values[j + 1] * scale),
													color: color,
													layer: GUI.Layer.Window);
											}

											//for (var j = 0; j < resolution - 1; j++)
											//{
											//	GUI.DrawLine(
											//		a: pos_base + new Vector2(stride * j, values[j] * scale),
											//		b: pos_base + new Vector2(stride * (j + 1), values[j + 1] * scale),
											//		color: color,
											//		layer: GUI.Layer.Window);
											//}


											//for (var index = histogram_index - 1; index >= 0; index--)
											//{
											//	var value_tmp = Maths.Lerp(value_prev, graph[index] * scale, lerp % 1.00f);

											//	//if (first) GUI.DrawCircleFilled(pos_base + new Vector2(stride * offset, value_prev), 4.00f, color, segments: 3, layer: GUI.Layer.Window);
											//	//first = false;
											//	GUI.DrawLine(pos_base + new Vector2(stride * offset, value_prev), pos_base + new Vector2(stride * (offset + 1), value_tmp), color: color, layer: GUI.Layer.Window);


											//	value_prev = value_tmp;
											//	index_prev = index;
											//	offset++;
											//	lerp += timestep;
											//}

											//for (var index = resolution - 1; index > histogram_index; index--)
											//{
											//	var value_tmp = Maths.Lerp(value_prev, graph[index] * scale, lerp % 1.00f);
											//	GUI.DrawLine(pos_base + new Vector2(stride * offset, value_prev), pos_base + new Vector2(stride * (offset + 1), value_tmp), color: color, layer: GUI.Layer.Window);

											//	value_prev = value_tmp;
											//	index_prev = index;
											//	offset++;
											//	lerp += timestep;
											//}
										}

										//graph[histogram_index] = signal[i];
									}
								}

								//histogram_index = Maths.Repeat(histogram_index + 1, resolution);
							}
						}

						//App.WriteLine(GUI.GetCurrentLineSize());


						//GUI.TrySameLine(384 - 224);

						//GUI.SameLine();

						//App.WriteLine(GUI.GetCurrentLineSize());


						//App.WriteLine(GUI.GetCurrentLineSize());

						//App.WriteLine(GUI.GetRemainingLineSpace());

						//using (var group = GUI.Group.New(size: GUI.GetRemainingLineSpace()))
						//using (var embed = GUI.Embed.New("analog.radio", size: GUI.GetRemainingLineSpace()))
						//using (var group = GUI.Group.New(size: new Vector2(GUI.GetRemainingLineSpace().X, GUI.GetLastItemRect().GetHeight())))
						{
							//GUI.DrawRect(GUI.GetLastItemRect(), layer: GUI.Layer.Foreground);
							//group.DrawRect();

							//App.WriteLine(GUI.GetRemainingLineSpace());

							//App.WriteLine(GUI.GetCurrentLineSize());

							//dock.SetSpace(size: GUI.Rm);

						}

						//using (var dock_modules = GUI.Dock.New("analog.modules"))

						//using (var embed = GUI.Embed.New("analog.modules", size: GUI.GetRemainingSpace()))
						//{

						//}
					}
				}
			}
		}

		//[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region), HasTag<Analog.Relay.Data>("display", true, Source.Modifier.Owned)]
		//public static void OnAnalogGUI(Entity entity, [Source.Owned] in Transform.Data transform, [Source.Owned] in Interactable.Data interactable,
		//[Source.Owned] in Analog.Relay.Data relay)
		//{
		//	if (interactable.IsActive())
		//	{
		//		var gui = new AnalogGUI()
		//		{
		//			entity = entity,
		//			relay = relay,
		//			//writer = writer,
		//			transform = transform,
		//		};
		//		gui.Submit();
		//	}
		//}

		[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global)]
		public static void OnGraphRelayGUI(Entity entity, [Source.Owned] in Transform.Data transform, [Source.Owned] in Interactable.Data interactable,
		[Source.Owned] in Analog.Relay.Data relay, [Source.Owned, Pair.Component<Analog.Relay.Data>] in Analog.Graph.Data graph)
		{
			if (interactable.IsActive())
			{
				var gui = new AnalogGUI()
				{
					entity = entity,
					transform = transform,
					relay = relay,
					graph = graph
					//writer = writer,
				};
				gui.Submit();
			}
		}
#endif
		public static partial class Graph
		{
			[Flags]
			public enum Flags: uint
			{
				None = 0u,


			}

			// TODO: wtf is this, was i drunk or something?
			[ITrait.Data(Net.SendType.Unreliable, IComponent.Scope.Global | IComponent.Scope.Region)]
			public struct Data(): ITrait
			{
				[Net.Segment.A] public required Analog.Graph.Flags flags;
				[Net.Segment.A] private uint unused_00_a;

				[Save.NewLine]
				[Net.Segment.B, Asset.Ignore] public FixedArray4<byte> offsets;
				[Net.Segment.B] private uint unused_00_b;

				[Save.NewLine]
				[Net.Segment.C, Asset.Ignore] public FixedArray64<float> values_0;
				[Net.Segment.C, Asset.Ignore] public FixedArray64<float> values_1;
				[Net.Segment.D, Asset.Ignore] public FixedArray64<float> values_2;
				[Net.Segment.D, Asset.Ignore] public FixedArray64<float> values_3;



				//public ref FixedArray64<float> this[Signal.Channel channel]
				//{
				//	get
				//	{
				//		switch (((uint)channel) & 0b11)
				//		{
				//			case 0: return ref this.values_0; 
				//			case 1: return ref this.values_1;
				//			case 2: return ref this.values_2;
				//			case 3: return ref this.values_3;

				//			default: throw new InvalidOperationException();
				//		}
				//	}
				//}
			}

			[ISystem.PreUpdate.A(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global)]
			public static void OnUpdate_Relay(
			[Source.Owned, Pair.Component<Analog.Relay.Data>] ref Analog.Graph.Data graph,
			[Source.Owned] in Analog.Relay.Data relay)
			{
				for (var i = 0u; i < (uint)Signal.Count; i++)
				{

					// TODO: extension indexer isn't inferred for some reason
					//scoped ref var values = ref TC2.Base.Components.Analog.get_Item(ref graph, (Signal.Channel)i);
					scoped ref var values = ref graph.get_Item((Signal.Channel)i);
					//scoped ref var values = ref graph[(Signal.Channel)i];

					ref var offset = ref graph.offsets[i];
					values[offset] = relay.signal_current[i];
					offset = (byte)(((uint)offset + 1) & values.IndexerMask);



					//scoped ref FixedArray64<float> values = ref FixedArray64<float>.Null;
					//switch (offset)
					//{
					//	case 0: values = ref graph.values_0; break;
					//	case 1: values = ref graph.values_1; break;
					//	case 2: values = ref graph.values_2; break;
					//	case 3: values = ref graph.values_3; break;
					//}


				}
			}
		}

#if CLIENT
		[Shitcode]
		public partial struct RadioGUI: IGUICommand
		{
			public Entity entity;
			public Analog.Receiver.Data? receiver;
			public Analog.Transmitter.Data? transmitter;
			//public Analog.Writer.Data writer;
			public Transform.Data transform;

			public static AABB rect_graph;

			public void Draw()
			{
				using (var window = GUI.Window.Interaction("Radio"u8, this.entity, identifier_dock: "analog.modules"u8))
				{
					this.StoreCurrentWindowTypeID(order: -500);
					if (window.show)
					{
						ref var region = ref this.entity.GetRegion();
						ref var radio = ref region.GetRadioGradient();

						var span = radio.GetSpan();
						var scale = -12.00f;
						//scale *= 0.001f;
						var resolution = 128; // radio.count;

						var hovered_value = 0.00f;

						//App.WriteLine(GUI.GetRemainingLineSpace());

						//var wrekt = GUI.GetParentWorkRect();
						//var rekt = new AABB(wrekt.a, new Vector2(wrekt.b.X, -GUI.GetCurrentLineSize().Y)); //.GetRemainingRect(); // AABB.Simple()
						//GUI.DrawRect(GUI.GetPrevRemainingRect(), layer: GUI.Layer.Foreground, color: GUI.font_color_green, thickness: 2);


						//GUI.TrySameLine(224);

						//GUI.DrawRect(GUI.GetRemainingRect(), layer: GUI.Layer.Foreground, color: GUI.font_color_red);


						//GUI.DrawTextCentered($"{GUI.GetCurrentLineSize()}; {this.receiver.HasValue}; {this.transmitter.HasValue}", GUI.GetParentWorkRect().GetPosition(pivot: new(0.50f, 0.50f)), layer: GUI.Layer.Foreground);
						//using (var group = window.docked ? GUI.Group.NewRect(GUI.GetWorkRect()) : GUI.Group.New(size: GUI.Rm))
						using (var group = GUI.Group.New(size: window.docked ? (GUI.TrySameLine(128) ? GUI.GetRemainingLineSpace() : new Vector2(224, 128)) : GUI.Rm))
						{
							using (var group_row = GUI.Group.New(size: GUI.Rm, padding: new(4, 4)))
							{
								group_row.DrawBackground(GUI.tex_window);

								var stride = GUI.RmX / resolution;
								var freq = RadioGradient.freq_min;
								var freq_step = RadioGradient.freq_range / (float)resolution;

								var rect_inner = group_row.GetInnerRect();
								RadioGUI.rect_graph = rect_inner;



								//var pos_base = new Vector2(rect_inner.a.X, rect_inner.b.Y);
								var pos_base = new Vector2(rect_inner.a.X, Maths.Avg(rect_inner.a.Y, rect_inner.b.Y));
								//var value_prev = graph[index_prev] * scale;

								//var first = true;

								//GUI.DrawCircleFilled(pos_base + new Vector2(stride * histogram_index, 0.00f), 4.00f, color, segments: 3, layer: GUI.Layer.Window);
								//GUI.DrawCircleFilled(pos_base + new Vector2(stride * (resolution - histogram_index), 0.00f), 4.00f, color, segments: 3, layer: GUI.Layer.Window);

								var h = rect_inner.GetHeight() * 0.50f;

								var value_prev = radio.GetValue(freq);
								var color_prev = Color32BGRA.FromHSV(MathF.Abs(value_prev * 0.30f), 1.00f, 1.00f);

								freq += freq_step;

								var hovered_ratio = Maths.InvLerp01(rect_inner.a.X, rect_inner.b.X, GUI.GetMousePosition().X);
								var hovered_index = MathF.Floor(Maths.Lerp01(0, resolution, hovered_ratio));

								//var hovered_ratio = radio.GetRatio(freq);
								//var hovered_index = Maths.Clamp(Maths.LerpInt(0, count, hovered_ratio), 1, count - 1);

								//for (var i = 1; i < resolution; i++)
								//{
								//	var value = radio.GetValue(freq);
								//	var color = Color32BGRA.FromHSV(MathF.Abs(value * 0.30f), 1.00f, 1.00f);

								//	if (hovered_index == i)
								//	{
								//		color = Color32BGRA.White;
								//		GUI.DrawLine(new Vector2(pos_base.X + (stride * (i)), rect_inner.a.Y), new Vector2(pos_base.X + (stride * (i)), rect_inner.b.Y), color: Color32BGRA.White.WithAlphaMult(0.25f), thickness: 2.00f, layer: GUI.Layer.Window);
								//	}

								//	if (value != 0.00f)
								//	{
								//		GUI.DrawRectFilled(AABB.Simple(pos_base + new Vector2(stride * i, 0.00f), new Vector2(stride, value * scale)), color: color, layer: GUI.Layer.Window);
								//		//GUI.DrawLine2(pos_base + new Vector2(stride * (i - 1), value_prev * scale), pos_base + new Vector2(stride * (i), value * scale), c color: GUI.col_button_yellow, layer: GUI.Layer.Window);
								//		//GUI.DrawLine2(pos_base + new Vector2(stride * (i - 1), value_prev * scale), pos_base + new Vector2(stride * (i), value * scale), color_a: color_prev, color_b: color, layer: GUI.Layer.Window);
								//	}

								//	value_prev = value;
								//	color_prev = color;

								//	freq += freq_step;

								//	//GUI.DrawLine(pos_base + new Vector2(stride * j, values[j] * scale), pos_base + new Vector2(stride * (j + 1), values[j + 1] * scale), color: color, layer: GUI.Layer.Window);
								//}



								for (var i = 1; i < resolution; i++)
								{
									//var value = (uint)MathF.Floor(radio.GetRatio(freq) * (radio.count - 1)); //span[i];
									var value = radio.GetValue(freq);
									//var value = radio.GetFrequency((int)MathF.Floor(radio.GetRatio(freq) * (radio.count - 1))); //span[i];


									//var center = 0.60f;
									//var max = 1.00f;

									//if (value.IsPositive()) value = Maths.Ma(0.00f, value - center);
									//else value = Maths.Min(0.00f, value + center);


									//var color = Color32BGRA.FromHSV(Maths.Clamp(MathF.Abs(value * 0.00030f), 0.00f, 3.00f), 1.00f, 1.00f);
									var color = Color32BGRA.FromHSV(Maths.Clamp(MathF.Abs(value * 0.30f), 0.00f, 3.00f), 1.00f, 1.00f);

									//var x_a = Maths.Lerp(rect_inner.a.X, rect_inner.b.X, ((i - 1) * radio.iter_step)); //.Pow2());
									//var x_b = Maths.Lerp(rect_inner.a.X, rect_inner.b.X, (i * radio.iter_step)); //.Pow2());

									//if (hovered_index == i)
									//{
									//	color = Color32BGRA.White;
									//	GUI.DrawLine(new Vector2(pos_base.X + (stride * (i + 0.25f)), rect_inner.a.Y), new Vector2(pos_base.X + (stride * (i + 0.25f)), rect_inner.b.Y), color: Color32BGRA.White.WithAlphaMult(0.25f), thickness: 1.00f, layer: GUI.Layer.Window);
									//}

									if (hovered_index == i)
									{
										color = Color32BGRA.White;
										GUI.DrawLine(new Vector2(pos_base.X + (stride * (i)), rect_inner.a.Y), new Vector2(pos_base.X + (stride * (i)), rect_inner.b.Y), color: Color32BGRA.White.WithAlphaMult(0.25f), thickness: 2.00f, layer: GUI.Layer.Window);
										hovered_value = value;

										var ratio = radio.GetRatio(freq);
										var index = Maths.LerpInt(0, radio.count, ratio);
										index = Maths.Clamp(index, 0, radio.count - 2);

										//GUI.DrawTextCentered($"[{index}] [{i}]: {freq:0.00}={radio.GetFrequency(index):0.00}; {hovered_value:0.00}={span[index]:0.00}", new Vector2(Maths.Avg(rect_inner.a.X, rect_inner.b.X), rect_inner.b.Y - 64), new Vector2(0.50f, 1.00f), layer: GUI.Layer.Window);
										//GUI.DrawTextCentered($"Frequency: {freq:0.00}\nSignal: {hovered_value:+0.00;-0.00}", new Vector2(Maths.Avg(rect_inner.a.X, rect_inner.b.X), rect_inner.b.Y - 8), new Vector2(0.50f, 1.00f), layer: GUI.Layer.Window);
										GUI.DrawTextCentered($"Signal: {hovered_value:+0.00;-0.00}", new Vector2(rect_inner.a.X + 6, rect_inner.b.Y - 18), new Vector2(0.00f, 1.00f), layer: GUI.Layer.Window);
										GUI.DrawTextCentered($"Frequency: {freq:0.00}", new Vector2(rect_inner.a.X + 6, rect_inner.b.Y - 6), new Vector2(0.00f, 1.00f), layer: GUI.Layer.Window);


										//var index_a = Maths.Clamp(Maths.LerpInt(0, count, ), 1, count - 1)
									}

									//if (value != 0.00f)
									{
										//GUI.DrawRectFilled(AABB.Simple(pos_base + new Vector2(stride * i, 0.00f), new Vector2(stride, value * scale)), color: color, layer: GUI.Layer.Window);
										//GUI.DrawRectFilled(AABB.Simple(pos_base + new Vector2(stride * i, 0.00f), new Vector2(stride, value * scale)), color: color, layer: GUI.Layer.Window);
										//GUI.DrawRectFilled(new AABB(new Vector2(x_a, pos_base.Y), new Vector2(x_b, pos_base.Y + (value * scale))), color: color, layer: GUI.Layer.Window);
										//GUI.DrawLine(new Vector2(x_a, pos_base.Y + (value_prev * scale)), new Vector2(x_b, pos_base.Y + (value * scale)), color: GUI.col_button_yellow, layer: GUI.Layer.Window);
										//GUI.DrawLine2(new Vector2(x_a, pos_base.Y + (value_prev * scale)), new Vector2(x_b, pos_base.Y + (value * scale)), color_a: color_prev, color_b: color, layer: GUI.Layer.Window);
										//GUI.DrawLine2(pos_base + new Vector2(stride * (i - 1), value_prev * scale), pos_base + new Vector2(stride * (i), value * scale), color_a: color_prev, color_b: color, layer: GUI.Layer.Window);

										//GUI.DrawRect(AABB.Simple(pos_base + new Vector2(stride * i, 0.00f), new Vector2(stride, value * scale)), color: color, layer: GUI.Layer.Window);


										GUI.DrawLine2(pos_base + new Vector2(stride * (i - 1), Maths.Clamp(value_prev * scale, -h, h)), pos_base + new Vector2(stride * (i), Maths.Clamp(value * scale, -h, h)), color_a: color_prev, color_b: color, layer: GUI.Layer.Window);
									}

									value_prev = value;
									color_prev = color;

									freq += freq_step;

									//GUI.DrawLine(pos_base + new Vector2(stride * j, values[j] * scale), pos_base + new Vector2(stride * (j + 1), values[j + 1] * scale), color: color, layer: GUI.Layer.Window);
								}

								//GUI.DrawTextCentered($"{hovered_value:0.00}", new Vector2(Maths.Avg(rect_inner.a.X, rect_inner.b.X), rect_inner.b.Y - 8), new Vector2(0.50f, 1.00f), layer: GUI.Layer.Window);
							}
						}
					}
				}
			}
		}

		[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region, order: -200), HasAnyComponent<Analog.Receiver.Data, Analog.Transmitter.Data>(Source.Modifier.Owned)]
		public static void OnRadioGUI(Entity entity, [Source.Owned] in Transform.Data transform, [Source.Owned] in Interactable.Data interactable,
		[Source.Owned, Optional(true)] ref Analog.Receiver.Data receiver, [Source.Owned, Optional(true)] ref Analog.Transmitter.Data transmitter)
		{
			if (interactable.IsActive())
			{
				var gui = new RadioGUI()
				{
					entity = entity,
					transform = transform,
				};

				if (receiver.IsNotNull()) gui.receiver = receiver;
				if (transmitter.IsNotNull()) gui.transmitter = transmitter;

				gui.Submit();
			}
		}
#endif

		public static partial class Transmitter
		{
			public struct Bind()
			{
				[Flags]
				public enum Flags: uint
				{
					None = 0u,

					//Aim_Position = 1u << 0,
					//Local = 1u << 1,
				}

				public float frequency = 5000.00f;
				public float power;
				public float offset;
				public uint lockstep;
			}

			[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Global | IComponent.Scope.Region)]
			public struct Data(): IComponent
			{
				public float freq_min = 4000.00f;
				public float freq_max = 7000.00f;
				public float power_max = 5.00f;

				public Signal.Channels mask = Signal.Channels.Red | Signal.Channels.Green | Signal.Channels.Blue | Signal.Channels.Yellow;

				public FixedArray4<Analog.Transmitter.Bind> bindings = new();
				[Save.Ignore, Net.Ignore] public Signal signal;
			}

			public struct ConfigureRPC: Net.IRPC<Analog.Transmitter.Data>
			{


#if SERVER
				public void Invoke(Net.IRPC.Context rpc, ref Analog.Transmitter.Data data)
				{
					var sync = false;

					if (sync)
					{
						data.Sync(rpc.entity, true);
					}
				}
#endif
			}

			[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region, order: 150)]
			public static void OnUpdate(ISystem.Info info, ref Region.Data region, Entity entity,
			[Source.Owned] ref Analog.Transmitter.Data transmitter, [Source.Owned] ref Analog.Relay.Data relay, [Source.Owned] in Control.Data control)
			{
				var tick = region.GetCurrentTick();
				ref var radio = ref region.GetRadioGradient();

				transmitter.signal = relay.signal_current.Select(default, transmitter.mask);
				//transmitter.frequency = Maths.Clamp(transmitter.frequency + ((control.keyboard.GetKey(Keyboard.Key.MoveRight).AsFloat() - control.keyboard.GetKey(Keyboard.Key.MoveLeft).AsFloat()) * 10), RadioGradient.freq_min, RadioGradient.freq_max);

				for (var i = 0; i < Signal.Count; i++)
				{
					ref var binding = ref transmitter.bindings[i];
					if (binding.power > Maths.epsilon && binding.frequency >= transmitter.freq_min && binding.frequency < transmitter.freq_max)
					{
						var value = transmitter.signal[i];
						value *= binding.power;
						value += binding.offset;
						value = Maths.Clamp(value, -transmitter.power_max, transmitter.power_max);

						radio.SetValue(binding.frequency, value);

						//if ((transmitter.lockstep[i] == 0 || tick % transmitter.lockstep[i] == 0) && transmitter.mask.HasAny((Signal.Channels)(1 << 0)))
						//{
						//	//radio.SetValue(transmitter.frequency + (MathF.Sin(info.WorldTime * 30) * 100.00f), relay.signal_current[i] * transmitter.power);
						//	radio.SetValue(transmitter.frequency, relay.signal_current[i] * transmitter.power);
						//}
					}
				}
			}

#if CLIENT
			public partial struct TransmitterGUI: IGUICommand
			{
				public Entity entity;
				public Analog.Transmitter.Data transmitter;
				public Transform.Data transform;

				public void Draw()
				{
					using (var window = GUI.Window.Interaction("Transmitter"u8, this.entity, identifier_dock: "analog.modules"u8))
					{
						this.StoreCurrentWindowTypeID(order: -400);
						if (window.show)
						{
							ref var region = ref this.entity.GetRegion();
							ref var radio = ref region.GetRadioGradient();

							using (var group = GUI.Group.New(size: window.docked ? new Vector2(224, 96) : GUI.Rm))
							{
								group.DrawBackground(GUI.tex_frame);
							}
						}
					}
				}
			}

			[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region, order: 30)]
			public static void OnGUI(Entity entity, [Source.Owned] in Transform.Data transform, [Source.Owned] in Interactable.Data interactable,
			[Source.Owned] ref Analog.Transmitter.Data transmitter)
			{
				if (interactable.IsActive())
				{
					var gui = new TransmitterGUI()
					{
						entity = entity,
						transmitter = transmitter,
						transform = transform,
					};
					gui.Submit();
				}
			}
#endif
		}


		public static partial class Speaker
		{
			public struct Bind()
			{
				[Flags]
				public enum Flags: uint
				{
					None = 0u,
				}

				public Analog.Speaker.Bind.Flags flags;
			}

			[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
			public struct Data(): IComponent
			{
				public FixedArray4<Analog.Speaker.Bind> bindings = new();

				public Signal.Channels mask = Signal.Channels.Red | Signal.Channels.Green | Signal.Channels.Blue | Signal.Channels.Yellow;

				[Save.Ignore, Net.Ignore] public Signal signal;
			}

			public struct ConfigureRPC: Net.IRPC<Analog.Speaker.Data>
			{
				public byte index;

#if SERVER
				public void Invoke(Net.IRPC.Context rpc, ref Analog.Speaker.Data data)
				{
					var sync = false;

					ref var bind = ref data.bindings[this.index];

					if (sync)
					{
						data.Sync(rpc.entity, true);
					}
				}
#endif
			}

			//[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region, order: 100)]
			//public static void OnUpdate(ISystem.Info info, ref Region.Data region, Entity entity,
			//[Source.Owned] ref Analog.Speaker.Data speaker, [Source.Owned] ref Analog.Relay.Data relay)
			//{
			//	var tick = region.GetCurrentTick();
			//}

#if CLIENT
			public partial struct SpeakerGUI: IGUICommand
			{
				public Entity entity;
				public Analog.Speaker.Data speaker;
				public Transform.Data transform;

				public static Analog.Speaker.ConfigureRPC edit_rpc;
				public static bool sync_dirty;
				public static float t_next_sync;

				public void Draw()
				{
					using (var window = GUI.Window.Interaction("Speaker"u8, this.entity, identifier_dock: "analog.modules"u8))
					{
						this.StoreCurrentWindowTypeID(order: -320);
						if (window.show)
						{
							ref var region = ref this.entity.GetRegion();

							GUI.TrySameLine(224);

							using (var group = GUI.Group.New(size: window.docked ? new Vector2(224, 192) : GUI.Rm, padding: new(4, 4)))
							{
								group.DrawBackground(GUI.tex_frame);
							}

							if (sync_dirty && region.GetWorldTime() >= t_next_sync)
							{
								edit_rpc.Send(this.entity);

								edit_rpc = default;
								sync_dirty = false;
								t_next_sync = region.GetWorldTime() + 0.10f;
							}
						}
					}
				}
			}

			[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region, order: 10)]
			public static void OnGUI(Entity entity, [Source.Owned] in Transform.Data transform, [Source.Owned] in Interactable.Data interactable,
			[Source.Owned] ref Analog.Speaker.Data speaker)
			{
				if (interactable.IsActive())
				{
					var gui = new SpeakerGUI()
					{
						entity = entity,
						speaker = speaker,
						transform = transform,
					};
					gui.Submit();
				}
			}
#endif
		}


		public static partial class Sensor
		{
			public struct Bind()
			{
				[Flags]
				public enum Flags: uint
				{
					None = 0u,
				}

				public Analog.Sensor.Bind.Flags flags;
			}

			[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
			public struct Data(): IComponent
			{
				public FixedArray4<Analog.Sensor.Bind> bindings = new();

				public Signal.Channels mask = Signal.Channels.Red | Signal.Channels.Green | Signal.Channels.Blue | Signal.Channels.Yellow;

				[Save.Ignore, Net.Ignore] public Signal signal;
			}

			public struct ConfigureRPC: Net.IRPC<Analog.Sensor.Data>
			{
				public byte index;

#if SERVER
				public void Invoke(Net.IRPC.Context rpc, ref Analog.Sensor.Data data)
				{
					var sync = false;

					ref var bind = ref data.bindings[this.index];

					if (sync)
					{
						data.Sync(rpc.entity, true);
					}
				}
#endif
			}

			//[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region, order: 100)]
			//public static void OnUpdate(ISystem.Info info, ref Region.Data region, Entity entity,
			//[Source.Owned] ref Analog.Sensor.Data sensor, [Source.Owned] ref Analog.Relay.Data relay)
			//{
			//	var tick = region.GetCurrentTick();
			//}

#if CLIENT
			public partial struct SensorGUI: IGUICommand
			{
				public Entity entity;
				public Analog.Sensor.Data sensor;
				public Transform.Data transform;

				public static Analog.Sensor.ConfigureRPC edit_rpc;
				public static bool sync_dirty;
				public static float t_next_sync;

				public void Draw()
				{
					using (var window = GUI.Window.Interaction("Sensor"u8, this.entity, identifier_dock: "analog.modules"u8))
					{
						this.StoreCurrentWindowTypeID(order: -320);
						if (window.show)
						{
							ref var region = ref this.entity.GetRegion();

							GUI.TrySameLine(224);

							using (var group = GUI.Group.New(size: window.docked ? new Vector2(224, 192) : GUI.Rm, padding: new(4, 4)))
							{
								group.DrawBackground(GUI.tex_frame);
							}

							if (sync_dirty && region.GetWorldTime() >= t_next_sync)
							{
								edit_rpc.Send(this.entity);

								edit_rpc = default;
								sync_dirty = false;
								t_next_sync = region.GetWorldTime() + 0.10f;
							}
						}
					}
				}
			}

			[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region, order: 10)]
			public static void OnGUI(Entity entity, [Source.Owned] in Transform.Data transform, [Source.Owned] in Interactable.Data interactable,
			[Source.Owned] ref Analog.Sensor.Data sensor)
			{
				if (interactable.IsActive())
				{
					var gui = new SensorGUI()
					{
						entity = entity,
						sensor = sensor,
						transform = transform,
					};
					gui.Submit();
				}
			}
#endif
		}

		public static partial class Receiver
		{
			public struct Bind()
			{
				[Flags]
				public enum Flags: uint
				{
					None = 0u,

					//Aim_Position = 1u << 0,
					//Local = 1u << 1,
				}

				public float frequency = 5000.00f;
				public float blend = 0.50f;
				public float magnitude = 1.00f;
				public float gain = 1.00f;

				public float min = 0.00f;
				public float max = 1.00f;
				public float center = 0.00f;
				public float unused;
				//public Signal.Channel channel;
			}

			[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Global | IComponent.Scope.Region)]
			public struct Data(): IComponent
			{
				public FixedArray4<Analog.Receiver.Bind> bindings = new();

				public float freq_min = 2000.00f;
				public float freq_max = 8000.00f;

				public Signal.Channels mask = Signal.Channels.Red | Signal.Channels.Green | Signal.Channels.Blue | Signal.Channels.Yellow;

				[Save.Ignore, Net.Ignore] public Signal signal;
			}

			public struct ConfigureRPC: Net.IRPC<Analog.Receiver.Data>
			{
				public float? frequency;
				public float? blend;
				public float? magnitude;
				public float? gain;
				public float? min;
				public float? max;
				public float? center;

				public byte index;

#if SERVER
				public void Invoke(Net.IRPC.Context rpc, ref Analog.Receiver.Data data)
				{
					var sync = false;

					ref var bind = ref data.bindings[this.index];

					sync |= bind.frequency.TrySet(this.frequency, data.freq_min, data.freq_max);
					sync |= bind.blend.TrySet(this.blend, 0.00f, 1.00f);
					sync |= bind.magnitude.TrySet(this.magnitude, 0.00f, 100.00f);
					sync |= bind.gain.TrySet(this.gain, 0.00f, 100.00f);
					sync |= bind.min.TrySet(this.min, 0.00f, 1.00f);
					sync |= bind.max.TrySet(this.max, 0.00f, 1.00f);
					sync |= bind.center.TrySet(this.center, -10.00f, 10.00f);

					if (sync)
					{
						data.Sync(rpc.entity, true);
					}
				}
#endif
			}

			[ISystem.PreUpdate.B(ISystem.Mode.Single, ISystem.Scope.Region, order: 100)]
			public static void OnUpdate(ISystem.Info info, ref Region.Data region, Entity entity,
			[Source.Owned] ref Analog.Receiver.Data receiver, [Source.Owned] ref Analog.Relay.Data relay)
			{
				var tick = region.GetCurrentTick();
				ref var radio = ref region.GetRadioGradient();

				for (var i = 0; i < Signal.Count; i++)
				{
					var value = 0.00f;

					ref var binding = ref receiver.bindings[i];
					if (receiver.mask.HasAny((Signal.Channels)(1 << 0)))
					{
						value += radio.GetValue(binding.frequency);
						value -= binding.center;
						//value = Maths.Clamp(value, binding.min, binding.max);

						//var tmp = Maths.Max(0.00f, MathF.Abs(value) - binding.center);

						//if (value.IsPositive()) value = Maths.Max(0.00f, value - binding.center);
						//else value = Maths.Min(0.00f, value + binding.center);

						var tmp = Maths.Abs(value);
						if (tmp > binding.magnitude) //tmp > binding.min && tmp <= binding.max)
						{
							value = (Maths.InvLerp01(binding.min, binding.max, value * binding.gain) - 0.50f) * 2.00f;
							//value = Maths.ClampMagnitude(Maths.NormalizeCentered(value * binding.gain, binding.magnitude),).WithUnsignedZero();
						}
						else
						{
							value = 0.00f;
						}

						//value = Maths.NormalizeCenteredCutOff(value - binding.center, binding.magnitude);

						//radio.SetValue(binding.frequency, value);

						//if ((transmitter.lockstep[i] == 0 || tick % transmitter.lockstep[i] == 0) && transmitter.mask.HasAny((Signal.Channels)(1 << 0)))
						//{
						//	//radio.SetValue(transmitter.frequency + (MathF.Sin(info.WorldTime * 30) * 100.00f), relay.signal_current[i] * transmitter.power);
						//	radio.SetValue(transmitter.frequency, relay.signal_current[i] * transmitter.power);
						//}
					}

					receiver.signal[i] = Maths.Lerp(receiver.signal[i], value, binding.blend);

					//if (receiver.lockstep[i] == 0 || tick % receiver.lockstep[i] == 0)
					//{
					//	receiver.signal[i] = radio.GetValue(receiver.frequency);
					//}
				}

				relay.signal_input += receiver.signal.Select(default, receiver.mask);
				//relay.signal_input += receiver.signal;
			}

#if CLIENT
			public partial struct ReceiverGUI: IGUICommand
			{
				public Entity entity;
				public Analog.Receiver.Data receiver;
				public Transform.Data transform;

				//public static Analog.Receiver.ConfigureRPC edit_rpc;
				//public static bool sync_dirty;
				public static float t_next_sync;
				public static FixedArray4<Bind> bindings_local;

				public void Draw()
				{
					using (var window = GUI.Window.Interaction("Receiver"u8, this.entity, identifier_dock: "analog.modules"u8))
					{
						this.StoreCurrentWindowTypeID(order: -300);
						if (window.show)
						{
							ref var region = ref this.entity.GetRegion();
							ref var radio = ref region.GetRadioGradient();

							GUI.TrySameLine(224);

							var rect_graph = Analog.RadioGUI.rect_graph;

							using (var group = GUI.Group.New(size: window.docked ? new Vector2(280, 64 * 4) : GUI.Rm, padding: new(4, 4)))
							{
								group.DrawBackground(GUI.tex_frame);

								var h = GUI.RmY;

								for (var i = 0; i < this.receiver.bindings.Length; i++)
								{
									using (GUI.ID<Analog.Receiver.Data, Analog.Receiver.Bind>.Push(i))
									{
										using (var group_row = GUI.Group.New(size: new(GUI.RmX, h * 0.25f), padding: new(2, 2)))
										{
											group_row.DrawBackground(GUI.tex_panel);

											ref var bind = ref this.receiver.bindings[i];
											ref var bind_local = ref bindings_local[i];

											bind_local.frequency.LerpFMARef(bind.frequency, 0.10f);
											bind_local.blend.LerpFMARef(bind.blend, 0.20f);
											bind_local.magnitude.LerpFMARef(bind.magnitude, 0.20f);
											bind_local.gain.LerpFMARef(bind.gain, 0.20f);

											bind_local.min.LerpFMARef(bind.min, 0.20f);
											bind_local.max.LerpFMARef(bind.max, 0.20f);
											bind_local.center.LerpFMARef(bind.center, 0.20f);

											var signal_color = Signal.GetColor((Signal.Channel)i);

											//var freq = RadioGradient.freq_min;
											//var freq_step = RadioGradient.freq_range / (float)resolution;

											var graph_x_norm = Maths.InvLerp01(RadioGradient.freq_min, RadioGradient.freq_max, bind_local.frequency);
											var graph_y_norm = Maths.InvLerp01(10.00f, -10.00f, bind_local.center);

											var graph_line_v = rect_graph.GetLineFromNormalizedX(graph_x_norm);
											var graph_line_h = rect_graph.GetLineFromNormalizedY(graph_y_norm);

											GUI.DrawLine(graph_line_v, color: signal_color.WithAlpha(70), thickness: 1.00f, layer: GUI.Layer.Window);
											GUI.DrawLine(graph_line_h, color: signal_color.WithAlpha(140), thickness: 1.00f, layer: GUI.Layer.Window);

											var rpc = new Analog.Receiver.ConfigureRPC();
											var sync = false;

											sync |= GUI.SliderFloat(label: "Center"u8,
												value: in bind.center,
												value_changed: ref rpc.center,
												min: -10.00f,
												max: 10.00f,
												snap: 0.10f,
												size: new Vector2(32, GUI.RmY),
												vertical: true);

											GUI.SameLine();

											sync |= GUI.SliderFloat(label: "Frequency"u8,
												value: in bind.frequency,
												value_changed: ref rpc.frequency,
												min: this.receiver.freq_min,
												max: this.receiver.freq_max,
												snap: 50.00f,
												size: new Vector2(GUI.RmX * 0.50f, 24),
												vertical: false);

											//GUI.SameLine();




											if (sync && region.GetWorldTime() >= t_next_sync)
											{
												rpc.index = (byte)i;
												rpc.Send(this.entity);

												t_next_sync = region.GetWorldTime() + 0.10f;
											}
										}
									}
								}
							}
						}
					}
				}
			}

			[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region, order: 10)]
			public static void OnGUI(Entity entity, [Source.Owned] in Transform.Data transform, [Source.Owned] in Interactable.Data interactable,
			[Source.Owned] ref Analog.Receiver.Data receiver)
			{
				if (interactable.IsActive())
				{
					var gui = new ReceiverGUI()
					{
						entity = entity,
						receiver = receiver,
						transform = transform,
					};
					gui.Submit();
				}
			}
#endif
		}

		public static partial class Relay
		{
			[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Global | IComponent.Scope.Region)]
			public struct Data: IComponent
			{
				[Save.Ignore] public readonly Signal signal_current;
				[Net.Ignore, Save.Ignore] public Signal signal_input;

				public Data(Signal signal, Signal signal_input)
				{
					this.signal_current = signal;
					this.signal_input = signal_input;
				}
			}

			[ISystem.PreUpdate.Reset(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global)]
			public static void OnReset([Source.Owned] ref Analog.Relay.Data relay)
			{
				relay = new Analog.Relay.Data(relay.signal_input, default);
			}
		}

		//public static partial class Reader
		//{
		//	[IComponent.Data(Net.SendType.Unreliable, region_only: true)]
		//	public partial struct Data: IComponent
		//	{
		//		[Flags]
		//		public enum Flags: uint
		//		{
		//			None = 0u,
		//		}

		//		//public float signal_cached;
		//		public Signal signal_cached;
		//		//public Signal.Channel channel;

		//		public Signal.Channels mask_read;
		//		public Signal.Channels mask_write;

		//		public float min = -1.00f;
		//		public float max = 1.00f;

		//		public Analog.Reader.Data.Flags flags;

		//		public Data()
		//		{

		//		}
		//	}

		//	[ISystem.VeryEarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region, order: -200)]
		//	public static void OnUpdateParent(ISystem.Info info, ref Region.Data region, Entity entity,
		//	[Source.Parent] in Analog.Relay.Data relay, [Source.Owned] ref Analog.Reader.Data reader)
		//	{
		//		reader.signal_cached = relay.signal_current.Select(default, reader.mask_read);
		//		//reader.signal_cached = relay.channels_current[(int)reader.channel];
		//	}

		//	[ISystem.EarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region, order: -100)]
		//	public static void OnUpdate(ISystem.Info info, ref Region.Data region, Entity entity,
		//	[Source.Owned] ref Analog.Reader.Data reader, [Source.Owned] ref Analog.Relay.Data relay)
		//	{
		//		var signal_tmp = reader.signal_cached.Select(relay.signal_input, reader.mask_write);
		//		relay.signal_input += signal_tmp;
		//	}
		//}

		public static partial class Writer
		{
			[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Global | IComponent.Scope.Region)]
			public partial struct Data(): IComponent
			{
				[Flags]
				public enum Flags: ushort
				{
					None = 0,
				}

				//public float signal_cached;
				[Save.Ignore] public Signal signal_cached;
				//public Signal.Channel channel;

				public Signal.Channels mask_read;
				public Signal.Channels mask_write;
				public Analog.Writer.Data.Flags flags;

				private uint unused;

				public float min = -1.00f;
				public float max = 1.00f;
			}

			public struct ConfigureRPC: Net.IRPC<Analog.Writer.Data>
			{


#if SERVER
				public void Invoke(Net.IRPC.Context rpc, ref Analog.Writer.Data data)
				{
					var sync = false;

					if (sync)
					{
						data.Sync(rpc.entity, true);
					}
				}
#endif
			}

			//[ISystem.PreUpdate.C(ISystem.Mode.Single, ISystem.Scope.Region)]
			//public static void OnUpdateSignal([Source.Owned] in Module.Slots.Data module_slot,
			//[Source.Parent<Module.Link>] ref Analog.Relay.Data relay_module, [Source.Owned] ref Analog.Relay.Data relay)
			//{

			//}

			//[ISystem.PreUpdate.C(ISystem.Mode.Single, ISystem.Scope.Region)]
			//public static void OnUpdateModule(ISystem.Info info, ref Region.Data region, Entity entity,
			//[Source.Shared] ref Analog.Relay.Data relay_src, [Source.Owned] in Attachment.Slot attachment, [Source.Owned] ref Analog.Writer.Data writer)
			//{
			//	relay_src.signal_input += writer.signal_cached.Select(default, writer.mask_read);
			//	writer.signal_cached = relay_src.signal_current.Select(default, writer.mask_write);

			//	//writer.signal_cached = relay.channels_current[(int)writer.channel];
			//}

			[Shitcode] // TODO: should use variable select, but this one requires constant args
			[ISystem.PreUpdate.D(ISystem.Mode.Single, ISystem.Scope.Region, order: -250)]
			public static void OnUpdateAttachment(ISystem.Info info, ref Region.Data region, Entity entity,
			[Source.Shared] ref Analog.Relay.Data relay_src, [Source.Owned] in Attachment.Slot attachment, [Source.Owned] ref Analog.Writer.Data writer)
			{
				relay_src.signal_input += writer.signal_cached.Select(default, writer.mask_read);
				writer.signal_cached = relay_src.signal_current.Select(default, writer.mask_write);

				//writer.signal_cached = relay.channels_current[(int)writer.channel];
			}

			[Shitcode] // TODO: should use variable select, but this one requires constant args
			[ISystem.PreUpdate.F(ISystem.Mode.Single, ISystem.Scope.Region, order: -100)]
			public static void OnUpdateChild(ISystem.Info info, ref Region.Data region, Entity entity,
			[Source.Parent] ref Analog.Writer.Data writer, [Source.Owned] ref Analog.Relay.Data relay_dst)
			{
				relay_dst.signal_input += writer.signal_cached.Select(default, writer.mask_write);
				writer.signal_cached = relay_dst.signal_current.Select(default, writer.mask_read);

				//var signal_tmp = writer.signal_cached.Select(relay.signal_input, writer.mask_write);
				//relay.signal_input += signal_tmp;
			}

			//[ISystem.VeryEarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region, order: -200)]
			//public static void OnUpdateParent(ISystem.Info info, ref Region.Data region, Entity entity,
			//[Source.Parent] in Analog.Relay.Data relay, [Source.Owned] ref Analog.Writer.Data writer)
			//{
			//	writer.signal_cached = relay.signal_current.Select(default, writer.mask_read);
			//	//writer.signal_cached = relay.channels_current[(int)writer.channel];
			//}

			//[ISystem.EarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region, order: -100)]
			//public static void OnUpdate(ISystem.Info info, ref Region.Data region, Entity entity,
			//[Source.Owned] ref Analog.Writer.Data writer, [Source.Owned] ref Analog.Relay.Data relay)
			//{
			//	var signal_tmp = writer.signal_cached.Select(relay.signal_input, writer.mask_write);
			//	relay.signal_input += signal_tmp;
			//}
		}

		//public static partial class Writer
		//{
		//	[IComponent.Data(Net.SendType.Unreliable, region_only: true)]
		//	public partial struct Data: IComponent
		//	{
		//		[Flags]
		//		public enum Flags: uint
		//		{
		//			None = 0u,
		//		}

		//		//public float signal_cached;
		//		public Signal signal_cached;
		//		//public Signal.Channel channel;

		//		public Signal.Channels mask_read;
		//		public Signal.Channels mask_write;

		//		public float min = -1.00f;
		//		public float max = 1.00f;

		//		public Analog.Writer.Data.Flags flags;

		//		public Data()
		//		{

		//		}
		//	}

		//	[ISystem.VeryEarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region, order: -210)]
		//	public static void OnUpdateParent(ISystem.Info info, ref Region.Data region, Entity entity,
		//	[Source.Parent] in Analog.Relay.Data relay, [Source.Owned] ref Analog.Writer.Data writer)
		//	{
		//		writer.signal_cached = relay.signal_current.Select(default, writer.mask_read);
		//		//reader.signal_cached = relay.channels_current[(int)reader.channel];
		//	}

		//	[ISystem.EarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region, order: -110)]
		//	public static void OnUpdate(ISystem.Info info, ref Region.Data region, Entity entity,
		//	[Source.Owned] ref Analog.Writer.Data writer, [Source.Owned] ref Analog.Relay.Data relay)
		//	{
		//		var signal_tmp = writer.signal_cached.Select(relay.signal_input, writer.mask_write);
		//		relay.signal_input += signal_tmp;
		//	}
		//}



		public static partial class Button
		{
			//public struct Bind()
			//{
			//	public Controller.Data.Type type;
			//}

			[Flags]
			public enum Flags: ushort
			{
				None = 0,

				Use_Misc = 1 << 14,
				No_GUI = 1 << 15, // Disable GUI for this component
			}

			[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Global | IComponent.Scope.Region)]
			public partial struct Data(): IComponent
			{
				public float signal_strength = 0.50f;

				public Signal.Channels channels_pressed;
				public Signal.Channels channels;
				public Analog.Button.Flags flags;

				public Vec2u16 button_size = new(48);
			}

			public struct UseRPC: Net.IRPC<Analog.Button.Data>
			{
				public Signal.Channels channels_pressed_add;
				public Signal.Channels channels_pressed_rem;

#if SERVER
				public void Invoke(Net.IRPC.Context rpc, ref Analog.Button.Data data)
				{
					var sync = false;
					//App.WriteValue(this.channels_pressed_add);
					sync |= data.channels_pressed.TryAddRemFlags(this.channels_pressed_add, this.channels_pressed_rem);

					if (sync)
					{
						rpc.Sync(ref data, true);
					}
				}
#endif
			}

			[Shitcode] // TODO: should use variable select, but this one requires constant args
			[ISystem.PreUpdate.B(ISystem.Mode.Single, ISystem.Scope.Region, flags: ISystem.Flags.Unchecked, order: 50), HasTag("wrecked", false, Source.Modifier.Owned)]
			public static void OnUpdate(/*ref Region.Data region, Entity ent_button,*/
			//[Source.Owned] in Transform.Data transform,
			[Source.Owned] ref Analog.Relay.Data relay, [Source.Owned] ref Analog.Button.Data button)
			{
				//var xmm0 = button.signal_strength.broadcastss();
				//var xmm1 = relay.signal_input.values.AsVector128();

				if (button.channels_pressed != Signal.Channels.None)
				{
					var signal_tmp = new Signal(button.signal_strength);
					var signal_tmp_masked = signal_tmp.Select(default, button.channels_pressed);
					relay.signal_input += signal_tmp_masked;

					button.channels_pressed = Signal.Channels.None;
				}
			}

#if CLIENT
			public partial struct ButtonGUI: IGUICommand
			{
				public Entity ent_button;
				public Analog.Button.Data button;
				public Transform.Data transform;

				public void Draw()
				{
					//using (var window = GUI.Window.Interaction("Button"u8, this.ent_button, identifier_dock: "analog.modules"u8))

					var button_size = this.button.button_size.ToVec2f();

					using (var window = GUI.Window.InteractionMisc("Button"u8, this.ent_button, size: default, show_misc: this.button.flags.HasAny(Flags.Use_Misc)))
					{
						this.StoreCurrentWindowTypeID(order: -300);
						if (window.show)
						{
							ref var region = ref this.ent_button.GetRegion();
							//ref var radio = ref region.GetRadioGradient();

							//GUI.TrySameLine(224);

							//using (var group = GUI.Group.New(size: window.docked ? default : GUI.Rm)) //, padding: new(4)))
							{
								//group.DrawBackground(GUI.tex_frame);

								//var button_size = GUI.Rm.ToVec2f();
								//button_size *= 0.50f;


								var sameline = false;
								for (var i = 0u; i < (uint)Signal.Count; i++)
								{
									var channel = (Signal.Channel)i;
									if (this.button.channels.HasChannel(channel))
									{
										if (sameline) GUI.TrySameLine(button_size.x);

										using (GUI.ID<Analog.Button.Data, Signal.Channels>.Push((uint)channel))
										{
											if (GUI.DrawIconButton("analog.btn"u8, sprite: default, size: button_size, color: channel.GetColor()))
											{
												//App.WriteValue(channel);
												//App.WriteValue(channel.ToChannels());
												var rpc = new Analog.Button.UseRPC()
												{
													channels_pressed_add = channel.ToChannels()
												};
												rpc.Send(this.ent_button);
											}
										}
										sameline = true;
									}
								}

								//if (this.button.channels.HasAny(Signal.Channels.Red))
								//{
								//	if (GUI.DrawIconButton("analog.btn.0"u8, sprite: default, size: button_size, color: Signal.GetColor(Signal.Channel.Red)))
								//	{

								//	}
								//}

								//if (this.button.channels.HasAny(Signal.Channels.Green))
								//{
								//	GUI.TrySameLine(button_size.x);

								//	if (GUI.DrawIconButton("analog.btn.1"u8, sprite: default, size: button_size, color: Signal.GetColor(Signal.Channel.Green)))
								//	{
								//		Signal.Channel.Yellow.ToStringUtf8
								//	}
								//}
							}
						}
					}
				}
			}

			[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region)]
			public static void OnGUI(Entity ent_button, [Source.Owned] in Transform.Data transform, [Source.Owned] in Interactable.Data interactable,
			[Source.Owned] ref Analog.Button.Data button)
			{
				if (interactable.IsActive())
				{
					var gui = new ButtonGUI()
					{
						ent_button = ent_button,
						button = button,
						transform = transform,
					};
					gui.Submit();
				}
			}
#endif
		}




		public static partial class Controller
		{
			public struct Bind()
			{
				public Controller.Data.Type type;

				public float center = 0.00f;
				public float magnitude = 1.00f;

				public float gain = 1.00f;
				public float threshold = 0.10f;
			}

			[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Global | IComponent.Scope.Region)]
			public partial struct Data(): IComponent
			{
				[Flags]
				public enum Flags: uint
				{
					None = 0u,
				}

				public enum Type: uint
				{
					None = 0,

					Mouse_Horizontal,
					Mouse_Vertical,

					Mouse_Trigger,

					Move_Horizontal,
					Move_Vertical,
				}

				public FixedArray4<Analog.Controller.Bind> binds = new();
				public Analog.Controller.Data.Flags flags;
			}

			public struct ConfigureRPC: Net.IRPC<Analog.Controller.Data>
			{


#if SERVER
				public void Invoke(Net.IRPC.Context rpc, ref Analog.Controller.Data data)
				{
					var sync = false;

					if (sync)
					{
						data.Sync(rpc.entity, true);
					}
				}
#endif
			}

			[ISystem.VeryEarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region, flags: ISystem.Flags.Unchecked, order: 50), HasTag("wrecked", false, Source.Modifier.Owned)]
			public static void OnUpdateControl(ref Region.Data region,
			[Source.Owned] in Transform.Data transform, [Source.Owned] ref Control.Data control,
			[Source.Owned] in Analog.Relay.Data relay, [Source.Owned] ref Analog.Controller.Data controller)
			{
				ref var mouse = ref control.mouse;
				ref var kb = ref control.keyboard;

				//float? mouse_x = null;
				//float? mouse_y = null;

				for (var i = 0; i < controller.binds.Length; i++)
				{
					ref var bind = ref controller.binds[i];
					var value = relay.signal_current[i];
					//var delta = value - bind.offset;

					//value -= bind.offset;

					value -= bind.center;
					value = Maths.NormalizeCentered(value, bind.magnitude);

					if (Maths.Abs(value) - bind.threshold > Maths.epsilon)
					{
						value *= bind.gain;

						switch (bind.type)
						{
							case Data.Type.Mouse_Horizontal:
							{
								mouse.position.X = value + transform.position.X;
							}
							break;

							case Data.Type.Mouse_Vertical:
							{
								mouse.position.Y = transform.position.Y - value;
							}
							break;

							case Data.Type.Mouse_Trigger:
							{
								mouse.SetKeyPressed(Mouse.Key.Left, value < -Maths.epsilon);
								mouse.SetKeyPressed(Mouse.Key.Right, value > Maths.epsilon);
							}
							break;

							case Data.Type.Move_Horizontal:
							{
								kb.SetKeyPressed(Keyboard.Key.MoveLeft, value < -Maths.epsilon);
								kb.SetKeyPressed(Keyboard.Key.MoveRight, value > Maths.epsilon);
							}
							break;

							case Data.Type.Move_Vertical:
							{
								kb.SetKeyPressed(Keyboard.Key.MoveDown, value < -Maths.epsilon);
								kb.SetKeyPressed(Keyboard.Key.MoveUp, value > Maths.epsilon);
							}
							break;
						}
					}
				}


#if CLIENT
				//region.DrawDebugLine(transform.position, mouse.position, Color32BGRA.Green, 2);

				if (false)
				{
					ref var terrain = ref region.GetTerrain();
					//var hit = terrain.TryGridCast(transform.position, (mouse.position - transform.position).GetNormalized(), out var hit_pos);


					var dir = (mouse.position - transform.position).GetNormalizedFast();

					var ts0 = Timestamp.Now();
					ref var sg_cell_nearest = ref terrain.GetNearestCellNonSolid2(transform.position, threshold: 0.50f);
					var sg_cell_nearest_pos = sg_cell_nearest.IsNull() ? transform.position : (transform.position + sg_cell_nearest.GetWorldPositionMid()) * 0.50f;

					var hit0 = terrain.TryGridCast(sg_cell_nearest_pos, dir, out var hit_pos0, max_dist: 200, allow_inside: false);
					var ts0_elapsed = ts0.GetMilliseconds();

					ref var sg_cell_hit = ref terrain.GetCell(hit_pos0);

					var ts1 = Timestamp.Now();
					var hit1 = !region.IsInLineOfSight(transform.position, mouse.position, hit_pos: out var hit_pos1, radius: 0.50f, mask: Physics.Layer.World, query_flags: Physics.QueryFlag.Static);
					var ts1_elapsed = ts1.GetMilliseconds();

					//var derp = Unsafe.BitCast<uint, float>(0b0_00000001_00000000000000000000001u);
					var derp = float.Epsilon * 200;
					var derp2 = 1.1754945e-38f;
					//var derp2 = 1.175494e-38f;
					//var derp2 = float.Epsilon * 10;
					//var derp2 = derp * 0.89f; // 1.1754945e-38f;

					//region.DrawDebugText(transform.position, $"grid: {ts0_elapsed:0.0000} ms\nline: {ts1_elapsed:0.0000} ms\n" +
					//	$"{(derp):r}\n" +
					//	$"{(derp2):r}\n" +
					//	$"{1.00f / derp}\n" +
					//	$"{derp.ToFormattedBinary2(separator: ' ')}\n" +
					//	$"{derp2.ToFormattedBinary2(separator: ' ')}\n" +
					//	$"subn: {float.IsSubnormal(derp)}\n" +
					//	$"norm: {float.IsNormal(derp)}\n" +
					//	$"real: {float.IsRealNumber(derp)}\n" +
					//	$"nan: {float.IsNaN(derp)}\n" +
					//	$"fin: {float.IsFinite(derp)}\n" +
					//	$"inf: {float.IsInfinity(derp)}\n" +
					//	"", Color32BGRA.White);
					//region.DrawDebugText(transform.position, $"grid: {ts0_elapsed:0.0000} ms\nline: {ts1_elapsed:0.0000} ms\n{(1.00f / (0.00f + )))}", Color32BGRA.White);
					region.DrawDebugText(hit_pos0, sg_cell_hit.flags.ToString(), Color32BGRA.White);

					region.DrawDebugLine(transform.position, hit_pos0, hit0 ? Color32BGRA.Green : Color32BGRA.Yellow);
					region.DrawDebugLine(transform.position, hit_pos1, hit1 ? Color32BGRA.DarkGreen : Color32BGRA.Orange);

					region.DrawDebugCircle(hit_pos0, 0.25f, hit0 ? Color32BGRA.Green : Color32BGRA.Yellow, 0.50f, filled: true);
					region.DrawDebugCircle(hit_pos1, 0.25f, hit1 ? Color32BGRA.DarkGreen : Color32BGRA.Orange, 0.750f, filled: true);

					region.DrawDebugCircle(mouse.position, 0.25f, Color32BGRA.Cyan, 1.00f, filled: true);
				}
#endif


				//if (mouse_x.HasValue || mouse_y.HasValue)
				//{
				//	var mouse_local = new Vector2(mouse_x ?? 0.00f, mouse_y ?? 0.00f);
				//	mouse.position.X = transform.LocalToWorld()
				//}
			}

#if CLIENT
			public partial struct ControllerGUI: IGUICommand
			{
				public Entity entity;
				public Analog.Controller.Data controller;
				public Transform.Data transform;

				public void Draw()
				{
					using (var window = GUI.Window.Interaction("Controller"u8, this.entity, identifier_dock: "analog.modules"u8))
					{
						this.StoreCurrentWindowTypeID(order: -300);
						if (window.show)
						{
							ref var region = ref this.entity.GetRegion();
							ref var radio = ref region.GetRadioGradient();

							GUI.TrySameLine(224);

							using (var group = GUI.Group.New(size: window.docked ? new Vector2(224, 96) : GUI.Rm))
							{
								group.DrawBackground(GUI.tex_frame);
							}
						}
					}
				}
			}

			[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region)]
			public static void OnGUI(Entity entity, [Source.Owned] in Transform.Data transform, [Source.Owned] in Interactable.Data interactable,
			[Source.Owned] ref Analog.Controller.Data controller)
			{
				if (interactable.IsActive())
				{
					var gui = new ControllerGUI()
					{
						entity = entity,
						controller = controller,
						transform = transform,
					};
					gui.Submit();
				}
			}
#endif
		}


		public static partial class Transducer
		{
			public struct Bind()
			{
				[Flags]
				public enum Flags: uint
				{
					None = 0u,
				}

				public enum Type: byte
				{
					None = 0,

					Aim_X,
					Aim_Y,
				}

				public Keyboard.Key keys_positive;
				public Keyboard.Key keys_negative;
				public Mouse.Key mouse_positive;
				public Mouse.Key mouse_negative;

				public Analog.Transducer.Bind.Flags flags;
				public Analog.Transducer.Bind.Type type;

				//public float freq = 0.00f;
				public float magnitude = 1.00f;
				public float gain = 1.00f;
				public float ramp = 0.50f;
				public float decay = 0.50f;
				public float blend = 0.50f;
				public float center = 0.00f;
			}

			[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Global | IComponent.Scope.Region)]
			public partial struct Data(): IComponent
			{
				[Flags]
				public enum Flags: uint
				{
					None = 0u,
				}

				public FixedArray4<Analog.Transducer.Bind> binds = new();
				public Analog.Transducer.Data.Flags flags;

				[Save.Ignore] public Signal signal_cached;
				[Save.Ignore, Net.Ignore] public Vector4 signals_t;
			}

			public struct ConfigureRPC: Net.IRPC<Analog.Transducer.Data>
			{


#if SERVER
				public void Invoke(Net.IRPC.Context rpc, ref Analog.Transducer.Data data)
				{
					var sync = false;

					if (sync)
					{
						data.Sync(rpc.entity, true);
					}
				}
#endif
			}

			[ISystem.PreUpdate.B(ISystem.Mode.Single, ISystem.Scope.Region, flags: ISystem.Flags.Unchecked, order: 1000), HasTag("wrecked", false, Source.Modifier.Owned)]
			public static void OnUpdateControl([Source.Owned] in Control.Data control, [Source.Owned] ref Transform.Data transform, [Source.Owned] ref Analog.Relay.Data relay, [Source.Owned] ref Analog.Transducer.Data transducer)
			{
				for (var i = 0; i < transducer.binds.Length; i++)
				{
					ref var bind = ref transducer.binds[i];

					var value = 0.00f;

					switch (bind.type)
					{
						case Analog.Transducer.Bind.Type.Aim_X:
						{
							value += Maths.NormalizeCentered(control.mouse.position.X - transform.position.X, bind.magnitude);
						}
						break;

						case Analog.Transducer.Bind.Type.Aim_Y:
						{
							value += Maths.NormalizeCentered(transform.position.Y - control.mouse.position.Y, bind.magnitude);
						}
						break;

						default:
						{
							value += control.keyboard.GetKey(bind.keys_positive).AsFloat() - control.keyboard.GetKey(bind.keys_negative).AsFloat();
							value += control.mouse.GetKey(bind.mouse_positive).AsFloat() - control.mouse.GetKey(bind.mouse_negative).AsFloat();
						}
						break;
					}

					value.ClampRef(-1.00f, 1.00f);
					relay.signal_input[i] += Maths.Lerp(relay.signal_current[i], (bind.center) + (transducer.signal_cached[i] * bind.gain), bind.blend);
					transducer.signal_cached[i] = Maths.MoveTowards(transducer.signal_cached[i], value, value == 0.00f ? bind.decay : bind.ramp);
				}
			}

#if CLIENT
			public partial struct TransducerGUI: IGUICommand
			{
				public Entity entity;
				public Analog.Transducer.Data transducer;
				public Transform.Data transform;

				public void Draw()
				{
					using (var window = GUI.Window.Interaction("Transducer"u8, this.entity, identifier_dock: "analog.modules"u8))
					{
						this.StoreCurrentWindowTypeID(order: -300);
						if (window.show)
						{
							ref var region = ref this.entity.GetRegion();
							ref var radio = ref region.GetRadioGradient();

							GUI.TrySameLine(224);

							using (var group = GUI.Group.New(size: window.docked ? new Vector2(224, 96) : GUI.Rm))
							{
								group.DrawBackground(GUI.tex_frame);
							}
						}
					}
				}
			}

			[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region, order: 20)]
			public static void OnGUI(Entity entity, [Source.Owned] in Transform.Data transform, [Source.Owned] in Interactable.Data interactable,
			[Source.Owned] ref Analog.Transducer.Data transducer)
			{
				if (interactable.IsActive())
				{
					var gui = new TransducerGUI()
					{
						entity = entity,
						transducer = transducer,
						transform = transform,
					};
					gui.Submit();
				}
			}
#endif
		}

		//[ISystem.Update(ISystem.Mode.Single, ISystem.Scope.Region)]
		//public static void OnUpdate(ISystem.Info info, ref Region.Data region, Entity entity,
		//[Source.Owned] ref Transform.Data transform, [Source.Owned] in Control.Data control,
		//[Source.Owned] ref Analog.Relay.Data relay, [Source.Owned] ref Analog.Writer.Data writer)
		//{
		//	var time = info.WorldTime;
		//	for (var i = 0; i < writer.binds.Length; i++)
		//	{
		//		ref var bind = ref writer.binds[i];

		//		var value_raw = control.keyboard.GetKeyNow(bind.keys_positive).AsFloat() - control.keyboard.GetKeyNow(bind.keys_negative).AsFloat();
		//		var value = value_raw;

		//		relay.signal_input[i] += Maths.Lerp(relay.signal_current[i], (bind.offset) + (writer.signals_cached[i] * bind.gain), bind.blend);
		//		writer.signals_cached[i] = Maths.MoveTowards(writer.signals_cached[i], value, value_raw == 0.00f ? bind.decay : bind.ramp);
		//	}
		//}

		//[ISystem.VeryEarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region, order: 100)]
		//public static void OnUpdateAttachments(ISystem.Info info, ref Region.Data region, Entity entity,
		//[Source.Shared] in Analog.Relay.Data signal, [Source.Owned] ref Attachment.Slot slot)
		//{
		//	slot.signal = signal.channels_current[(int)slot.channel];
		//}

		[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnUpdateSound([Source.Owned] in Analog.Relay.Data relay, [Source.Owned, Pair.Component<Analog.Relay.Data>] ref Sound.Emitter sound_emitter)
		{
			sound_emitter.pitch_mult = Maths.Abs(relay.signal_current[0]);
		}
	}
}
