
using Keg.Engine.Game;

namespace TC2.Base.Components
{
	public static partial class Rotograph
	{
		[IComponent.Data(Net.SendType.Unreliable, region_only: true)]
		public partial struct Data: IComponent
		{
			public EntRef<Axle.Data> ref_axle;
			public EntRef<Axle.State> ref_axle_state;

			public float load;
			public float app_torque;
			public float net_torque;
			public float angular_velocity;
			public float angular_momentum;
			public float rotational_energy;
			public float moment_of_inertia;

			[Net.Ignore, Save.Ignore] public ulong last_tick;
		}

		[ISystem.PostUpdate.E(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnUpdate(ISystem.Info info, Entity ent_rotograph,
		[Source.Owned] ref Rotograph.Data rotograph, [Source.Owned] in Transform.Data transform, [Source.Owned] in Control.Data control)
		{
			ref var axle_state = ref rotograph.ref_axle_state.GetComponent();
			if (axle_state.IsNotNull())
			{
				rotograph.load = axle_state.force_load_old;
				rotograph.app_torque = axle_state.app_torque;
				rotograph.net_torque = axle_state.net_torque;
				rotograph.angular_velocity = axle_state.angular_velocity;
				rotograph.angular_momentum = axle_state.angular_momentum;
				rotograph.rotational_energy = axle_state.rotational_energy;
				rotograph.moment_of_inertia = axle_state.moment_of_inertia;

				rotograph.last_tick = info.Tick;
			}
		}

		public struct EditRPC: Net.IRPC<Rotograph.Data>
		{
			public Entity? ent_target;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Rotograph.Data data)
			{
				var sync = false;

				if (this.ent_target.TryGetValue(out var ent_target_v) && ent_target_v != data.ref_axle_state.entity)
				{
					if (ent_target_v.IsAlive())
					{
						ref var axle_state = ref ent_target_v.GetComponent<Axle.State>();
						Assert.IsNotNull(ref axle_state);

						data.ref_axle.Set(ent_target_v);
						data.ref_axle_state.Set(ent_target_v);
						sync = true;
					}
					else
					{
						data.ref_axle.Reset();
						data.ref_axle_state.Reset();
						sync = true;
					}
				}
				
				if (sync)
				{
					data.Sync(rpc.entity);
				}
			}
#endif
		}

#if CLIENT
		public struct RotographGUI: IGUICommand
		{
			public Entity ent_rotograph;
			public Transform.Data transform;
			public Rotograph.Data rotograph;

			public bool is_held;

			public static ulong last_tick;

			//public Axle.Data axle;
			//public Axle.State axle_state;

			public static int load_graph_index;
			public static float[] load_graph = new float[App.tickrate * 10];

			public static int app_torque_graph_index;
			public static float[] app_torque_graph = new float[App.tickrate * 10];

			public static int net_torque_graph_index;
			public static float[] net_torque_graph = new float[App.tickrate * 10];

			public static int angular_velocity_graph_index;
			public static float[] angular_velocity_graph = new float[App.tickrate * 10];

			//public static int acc_graph_index;
			//public static float[] acc_graph = new float[App.tickrate * 10];

			public static int energy_graph_index;
			public static float[] energy_graph = new float[App.tickrate * 10];

			public static int momentum_graph_index;
			public static float[] momentum_graph = new float[App.tickrate * 10];

			[Region.Local]
			public static Entity edit_entity;

			public void Draw()
			{
				//using (var window = GUI.Window.Interaction("Rotograph"u8, this.ent_rotograph))
				using (var widget = Sidebar.Widget.New("rotograph", "Rotograph", new Sprite("ruler.00", 16, 8, 0, 0), new Vector2(600 + 48, 400 + 24), lockable: false, order: 1.00f, flags: Sidebar.Widget.Flags.Align_Right))
				{
					if (widget.state_flags.HasAny(Sidebar.Widget.StateFlags.Show))
					{
						ref var player = ref Client.GetPlayer();
						ref var region = ref this.ent_rotograph.GetRegion();

						var advance = last_tick != this.rotograph.last_tick;

						//load_graph = new float[App.tickrate * 10];

						using (GUI.Group.New(size: GUI.Av))
						{
							if (GUI.EntityPicker("rotograph.picker"u8, "Target"u8, size: new(192, 48), region.GetID(), ref edit_entity, layer_exclude: Physics.Layer.Ignore_Hover | Physics.Layer.World | Physics.Layer.Essence | Physics.Layer.Water))
							{
								var rpc = new EditRPC();
								rpc.ent_target = edit_entity;
								rpc.Send(this.ent_rotograph);

								edit_entity = default;
							}

							using (GUI.Group.New(size: GUI.Rm))
							{
								var h = GUI.RmY;
								var h_row = h / 6;

								using (var group_row = GUI.Group.New(size: new Vector2(GUI.RmX, h_row), padding: new(6, 6)))
								{
									group_row.DrawBackground(GUI.tex_panel);

									GUI.DrawTextCentered("Load"u8, group_row.GetInnerRect().GetPosition(new Vector2(0.00f, 0.00f), new(4, 4)), pivot: new(0.00f, 0.00f), layer: GUI.Layer.Window);
									if (advance) load_graph[load_graph_index] = this.rotograph.load;
									GUI.LineGraph("##load"u8, load_graph, ref load_graph_index, size: GUI.Rm, scale_min: 0.00f, scale_max: 8000.00f, advance: advance);
								}

								using (var group_row = GUI.Group.New(size: new Vector2(GUI.RmX, h_row), padding: new(6, 6)))
								{
									group_row.DrawBackground(GUI.tex_panel);

									GUI.DrawTextCentered("Torque (Applied)"u8, group_row.GetInnerRect().GetPosition(new Vector2(0.00f, 0.00f), new(4, 4)), pivot: new(0.00f, 0.00f), layer: GUI.Layer.Window);
									if (advance) app_torque_graph[app_torque_graph_index] = this.rotograph.app_torque;
									GUI.LineGraph("##app_torque"u8, app_torque_graph, ref app_torque_graph_index, size: GUI.Rm, scale_min: 0.00f, scale_max: 8000.00f, advance: advance);
								}

								using (var group_row = GUI.Group.New(size: new Vector2(GUI.RmX, h_row), padding: new(6, 6)))
								{
									group_row.DrawBackground(GUI.tex_panel);

									GUI.DrawTextCentered("Torque (Net)"u8, group_row.GetInnerRect().GetPosition(new Vector2(0.00f, 0.00f), new(4, 4)), pivot: new(0.00f, 0.00f), layer: GUI.Layer.Window);
									if (advance) net_torque_graph[net_torque_graph_index] = this.rotograph.net_torque;
									GUI.LineGraph("##net_torque"u8, net_torque_graph, ref net_torque_graph_index, size: GUI.Rm, scale_min: 0.00f, scale_max: 8000.00f, advance: advance);
								}


								using (var group_row = GUI.Group.New(size: new Vector2(GUI.RmX, h_row), padding: new(6, 6)))
								{
									group_row.DrawBackground(GUI.tex_panel);

									GUI.DrawTextCentered("Angular Velocity"u8, group_row.GetInnerRect().GetPosition(new Vector2(0.00f, 0.00f), new(4, 4)), pivot: new(0.00f, 0.00f), layer: GUI.Layer.Window);
									if (advance) angular_velocity_graph[angular_velocity_graph_index] = this.rotograph.angular_velocity;
									GUI.LineGraph("##angular_velocity"u8, angular_velocity_graph, ref angular_velocity_graph_index, size: GUI.Rm, scale_min: -20.00f, scale_max: 20.00f, advance: advance);
								}

								using (var group_row = GUI.Group.New(size: new Vector2(GUI.RmX, h_row), padding: new(6, 6)))
								{
									group_row.DrawBackground(GUI.tex_panel);

									GUI.DrawTextCentered("Rotational Energy"u8, group_row.GetInnerRect().GetPosition(new Vector2(0.00f, 0.00f), new(4, 4)), pivot: new(0.00f, 0.00f), layer: GUI.Layer.Window);
									if (advance) energy_graph[energy_graph_index] = this.rotograph.rotational_energy;
									GUI.LineGraph("##energy"u8, energy_graph, ref energy_graph_index, size: GUI.Rm, scale_min: 0.00f, scale_max: 10000.00f, advance: advance);
								}

								using (var group_row = GUI.Group.New(size: new Vector2(GUI.RmX, h_row), padding: new(6, 6)))
								{
									group_row.DrawBackground(GUI.tex_panel);

									GUI.DrawTextCentered("Angular Momentum"u8, group_row.GetInnerRect().GetPosition(new Vector2(0.00f, 0.00f), new(4, 4)), pivot: new(0.00f, 0.00f), layer: GUI.Layer.Window);
									if (advance) momentum_graph[momentum_graph_index] = this.rotograph.angular_momentum;
									GUI.LineGraph("##momentum"u8, momentum_graph, ref momentum_graph_index, size: GUI.Rm, scale_min: -this.rotograph.moment_of_inertia * 30.00f, scale_max: this.rotograph.moment_of_inertia * 30.00f, advance: advance);
								}
							}
						}
					}
				}
			}
		}

		[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("local", true, Source.Modifier.Parent)]
		public static void OnGUI(ISystem.Info info, Entity ent_rotograph,
		[Source.Parent] in Interactor.Data interactor,
		[Source.Owned] ref Rotograph.Data rotograph, [Source.Owned] in Transform.Data transform, [Source.Owned] in Control.Data control)
		{
			var gui = new RotographGUI()
			{
				ent_rotograph = ent_rotograph,
				transform = transform,
				rotograph = rotograph,
				is_held = true
			};
			gui.Submit();
		}

		[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("local", true, Source.Modifier.Stored)]
		public static void OnGUI_Stored(ISystem.Info info, Entity ent_rotograph,
		[Source.Owned] ref Rotograph.Data rotograph, [Source.Owned] in Transform.Data transform, [Source.Owned] in Control.Data control)
		{
			var gui = new RotographGUI()
			{
				ent_rotograph = ent_rotograph,
				transform = transform,
				rotograph = rotograph,
				is_held = false
			};
			gui.Submit();
		}

		//[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region)]
		//public static void OnGUI(Entity entity, [Source.Owned] in Transform.Data transform,
		//[Source.Owned] in Rotograph.Data rotograph,
		//[Source.Owned] in Axle.Data axle, [Source.Owned] in Axle.State axle_state,
		//[Source.Owned] in Interactable.Data interactable)
		//{
		//	if (interactable.IsActive())
		//	{
		//		var gui = new RotographGUI()
		//		{
		//			ent_rotograph = entity,

		//			transform = transform,

		//			rotograph = rotograph,

		//			axle = axle,
		//			axle_state = axle_state
		//		};
		//		gui.Submit();
		//	}
		//}
#endif
	}
}
