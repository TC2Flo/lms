
namespace TC2.Base.Components
{
	public static partial class Spectator
	{
		[Flags]
		public enum Flags: uint
		{
			None = 0u,

			Active = 1u << 0,
			Gridcast = 1u << 1,
			Freemove = 1u << 2,

			Manual_Zoom = 1u << 3,
			Distance_Zoom = 1u << 4,

			No_Zoom = 1u << 16,
			No_Mouse = 1u << 17,
			No_Cull = 1u << 18,
		}

		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public partial struct Data(): IComponent
		{


			public Spectator.Flags flags;

			public float zoom_speed = 0.50f;
			public float zoom_modfier = 1.00f;
			public float lerp_speed_pos = 0.70f;
			public float damp_modifier = 2.50f;
			public float pred_mult = 0.05f;
			public float follow_timeout = 2.00f;
			public float zoom_min = 1.00f;
			public float zoom_max = 2.00f;
			//public float gridcast_distance = 10.00f;

			public Mouse.Key camera_move_key = Mouse.Key.Right;

			public Maths.Easing easing;
			public Maths.EasingType easing_type;

			[Editor.Picker.Position(true, mark_modified: false, sensitivity: 2.00f)]
			[Save.Ignore] public Vector2 followed_offset;
			[Save.Ignore, Net.Ignore] public Vector2? override_pos;

			[Save.Ignore] public Entity ent_followed;
			[Save.Ignore] public Entity ent_followed_next;
			[Save.Ignore] public Entity ent_projectile;


			[Save.Ignore, Net.Ignore] public float t_follow_timeout;
			[Save.Ignore, Net.Ignore] public Vector2 current_position;
			[Save.Ignore, Net.Ignore] public Vector2 current_velocity;
			[Save.Ignore, Net.Ignore] public EntRef<Transform.Data> ref_transform_followed;
			[Save.Ignore, Net.Ignore] public EntRef<Transform.Data> ref_transform_projectile;

			//public Data()
			//{

			//}
		}

		public struct SetNextFollowedRPC: Net.IRPC<Spectator.Data>
		{
			public Entity ent_target;
			public Entity ent_next_out;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Spectator.Data data)
			{
				Assert.Check(rpc.connection.GetEntity() == rpc.entity);

				if (this.ent_target.IsAlive())
				{
					ref var region_common = ref this.ent_target.GetRegionCommon();

					// TODO: kinda hacky
					ref var gun_state = ref this.ent_target.GetComponent<Gun.State>();
					if (gun_state.IsNotNull())
					{
						var ent_next = (Entity)region_common.GetNewEntityID(ECS.EntityKind.Entity);
						gun_state.ent_projectile_next = ent_next;
						this.ent_next_out = ent_next;

						data.ent_followed_next = ent_next;
						data.Sync(rpc.entity);
					}
				}
				else
				{
					data.ent_followed_next = default;
					data.Sync(rpc.entity);
				}
			}
#endif
		}

		[Shitcode]
		public struct EditRPC: Net.IRPC<Spectator.Data>
		{
			public Entity? ent_followed;
			public Vector2? followed_offset;
			//public Vector2? override_pos;

			public Spectator.Flags? flags;

			public float? lerp_speed_pos;
			public float? damp_modifier;
			public float? zoom_speed;
			public float? zoom_modfier;
			public float? pred_mult;
			public float? follow_timeout;
			public float? zoom_min;
			public float? zoom_max;

			public Mouse.Key? camera_move_key;
			public Maths.Easing? easing;
			public Maths.EasingType? easing_type;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Spectator.Data data)
			{
				Assert.Check(rpc.connection.GetEntity() == rpc.entity);

				var sync = false;
				sync |= data.ent_followed.TrySet(this.ent_followed);
				sync |= data.followed_offset.TrySet(this.followed_offset);
				//if ((data.override_pos != this.override_pos).RelayTo(ref sync)) data.override_pos = this.override_pos;				
				sync |= data.flags.TrySet(this.flags);
				sync |= data.lerp_speed_pos.TrySet(this.lerp_speed_pos);
				sync |= data.damp_modifier.TrySet(this.damp_modifier);
				sync |= data.zoom_speed.TrySet(this.zoom_speed);
				sync |= data.zoom_modfier.TrySet(this.zoom_modfier);
				sync |= data.pred_mult.TrySet(this.pred_mult);
				sync |= data.follow_timeout.TrySet(this.follow_timeout);
				sync |= data.zoom_min.TrySet(this.zoom_min);
				sync |= data.zoom_max.TrySet(this.zoom_max);
				sync |= data.camera_move_key.TrySet(this.camera_move_key);
				sync |= data.easing.TrySet(this.easing);
				sync |= data.easing_type.TrySet(this.easing_type);
				if (sync)
				{
					data.Sync(rpc.entity);
				}
			}
#endif
		}

#if SERVER
		[ChatCommand.Region("spectator", "", creative: true)]
		public static void SpectatorCommand(ref ChatCommand.Context context)
		{
			ref var region = ref context.GetRegion();
			Assert.IsNotNull(ref region);

			ref var player = ref context.GetPlayerData();
			Assert.IsNotNull(ref player);

			var ent_player = player.ent_player;
			if (ent_player.HasComponent<Spectator.Data>())
			{
				ent_player.RemoveComponent<Spectator.Data>();
			}
			else
			{
				ent_player.AddComponent<Spectator.Data>();
			}
		}
#endif

#if CLIENT
		public struct SpectatorGUI: IGUICommand
		{
			public Transform.Data transform;
			public Spectator.Data spectator;
			public Entity ent_spectator;

			public void Draw()
			{
				ref var region_common = ref this.ent_spectator.GetRegionCommon();

				var kb = GUI.GetKeyboard();
				var mouse = GUI.GetMouse();

				var canvas_scale = region_common.GetWorldToCanvasScale();

				using (var widget = Sidebar.Widget.New("spectator", "Spectator", new Sprite(GUI.tex_icons_widget, 16, 16, 0, 0), new Vector2(244, 400 + 24), lockable: false, order: 100.00f))
				{
					if (widget.state_flags.HasAny(Sidebar.Widget.StateFlags.Show))
					{
						using (var group = GUI.Group.New(size: GUI.Av))
						{
							var rpc = new EditRPC();
							var dirty = false;

							var ent_followed = this.spectator.ref_transform_followed.entity;
							var ent_followed_tmp = ent_followed;

							var ent_followed_next = this.spectator.ent_followed_next;
							var ent_projectile = this.spectator.ref_transform_projectile.entity;

							var is_ent_followed_next_valid = ent_followed_next.IsValid();

							var is_ent_followed_alive = ent_followed.IsAlive();
							var is_ent_followed_next_alive = ent_followed_next.IsAlive();
							var is_ent_projectile_alive = ent_projectile.IsAlive();

							if (GUI.EntityPicker(identifier: "followed.picker"u8, name: "Follow"u8, size: new Vector2(GUI.RmX - 80, 40), region_id: region_common.GetID(), entity: ref ent_followed_tmp, layer_exclude: Physics.Layer.World, color: is_ent_followed_alive ? GUI.col_button_ok : GUI.col_button_yellow).RelayTo(ref dirty))
							{
								rpc.ent_followed = ent_followed_tmp;
							}

							GUI.SameLine();

							if (GUI.DrawButton("Unset"u8, size: new Vector2(GUI.RmX, 40), color: GUI.col_remove, enabled: is_ent_followed_alive).RelayTo(ref dirty))
							{
								rpc.ent_followed = Entity.None;
							}

							//if (GUI.DrawButton("Follow Projectile"u8, size: new Vector2(GUI.RmX - 80, 40), color: is_ent_followed_next_alive ? GUI.col_remove : GUI.col_add, enabled: is_ent_followed_alive))

							var ent_next_followed_target = Entity.None;
							if (GUI.EntityPicker(identifier: "followed.next.picker"u8, name: "Follow Projectile"u8, size: new Vector2(GUI.RmX - 80, 40), region_id: region_common.GetID(), entity: ref ent_next_followed_target, layer_exclude: Physics.Layer.World, color: is_ent_followed_next_valid ? GUI.col_button_ok : GUI.col_button_yellow))
							{
								var rpc_next_followed = new SetNextFollowedRPC();
								rpc_next_followed.ent_target = ent_next_followed_target;
								rpc_next_followed.Send(this.ent_spectator);
							}

							GUI.SameLine();

							if (GUI.DrawButton("Reset"u8, size: new Vector2(GUI.RmX, 40), color: GUI.col_remove, enabled: is_ent_followed_next_valid).RelayTo(ref dirty))
							{
								var rpc_next_followed = new SetNextFollowedRPC();
								rpc_next_followed.ent_target = Entity.None;
								rpc_next_followed.Send(this.ent_spectator);
							}

							GUI.SeparatorThick();

							if (GUI.SliderFloat("Follow Timeout"u8, in this.spectator.follow_timeout, ref rpc.follow_timeout, 0.000f, 10.000f, size: new Vector2(GUI.RmX, 24), show_label: true).RelayTo(ref dirty))
							{

							}

							if (GUI.SliderFloat("Position Lerp"u8, in this.spectator.lerp_speed_pos, ref rpc.lerp_speed_pos, 0.000f, 1.000f, size: new Vector2(GUI.RmX, 24), show_label: true).RelayTo(ref dirty))
							{

							}

							if (GUI.SliderFloat("Damp Modifier"u8, in this.spectator.damp_modifier, ref rpc.damp_modifier, 0.000f, 4.000f, size: new Vector2(GUI.RmX, 24), show_label: true).RelayTo(ref dirty))
							{

							}

							if (GUI.SliderFloat("Zoom Speed"u8, in this.spectator.zoom_speed, ref rpc.zoom_speed, 0.000f, 2.000f, snap: 0.001f, size: new Vector2(GUI.RmX, 24), show_label: true).RelayTo(ref dirty))
							{

							}

							if (GUI.SliderFloat("Zoom Scale"u8, in this.spectator.zoom_modfier, ref rpc.zoom_modfier, 0.001f, 4.000f, snap: 0.010f, size: new Vector2(GUI.RmX, 24), show_label: true).RelayTo(ref dirty))
							{

							}

							if (GUI.SliderFloat("Zoom (Min)"u8, in this.spectator.zoom_min, ref rpc.zoom_min, 0.01f, 4.000f, snap: 0.01f, size: new Vector2(GUI.RmX, 24), show_label: true).RelayTo(ref dirty))
							{

							}

							if (GUI.SliderFloat("Zoom (Max)"u8, in this.spectator.zoom_max, ref rpc.zoom_max, 0.01f, 4.000f, snap: 0.01f, size: new Vector2(GUI.RmX, 24), show_label: true).RelayTo(ref dirty))
							{

							}

							if (GUI.SliderFloat("Prediction Mult"u8, in this.spectator.pred_mult, ref rpc.pred_mult, 0.000f, 2.000f, size: new Vector2(GUI.RmX, 24), show_label: true).RelayTo(ref dirty))
							{

							}

							//if (GUI.SliderFloat("Camera Speed"u8, in this.spectator., ref rpc., 0.000f, 2.000f, size: new Vector2(GUI.RmX, 24), show_label: true).RelayTo(ref dirty))
							//{

							//}

							if (GUI.EnumInput("Camera Key"u8, in this.spectator.camera_move_key, ref rpc.camera_move_key, size: new Vector2(GUI.RmX, 24), show_label: true).RelayTo(ref dirty))
							{

							}

							if (GUI.EnumInput("Flags"u8, in this.spectator.flags, ref rpc.flags, size: new Vector2(GUI.RmX, 24), show_label: true).RelayTo(ref dirty))
							{

							}

							if (GUI.Checkbox("Disable Effects"u8, ref Camera.disable_effects, size: new(GUI.RmX, 24)))
							{

							}

							if (dirty)
							{
								rpc.Send(this.ent_spectator);
							}
						}
					}
				}

				using (var hud = GUI.Window.HUD("spectator.hud"u8))
				{
					if (hud.show)
					{


					}
				}
			}
		}

		[ISystem.VeryEarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("local", true, Source.Modifier.Shared)]
		public static void OnGUI(ISystem.Info.Common info, ref Region.Data.Common region_common, Entity ent_spectator, Entity ent_player,
		[Source.Shared] ref Spectator.Data spectator, [Source.Owned] ref Transform.Data transform, [Source.Owned] ref Control.Data control,
		[Source.Shared] ref Player.Data player, [Source.Singleton] ref Camera.Singleton camera)
		{
			var gui = new SpectatorGUI()
			{
				transform = transform,
				spectator = spectator,
				ent_spectator = ent_spectator
			};
			gui.Submit();
		}

		[ISystem.PreUpdate.D(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("local", true, Source.Modifier.Shared)]
		public static void OnUpdate(ISystem.Info.Common info, ref Region.Data.Common region_common, Entity ent_spectator, Entity ent_player,
		[Source.Shared] ref Spectator.Data spectator, [Source.Owned] ref Transform.Data transform, [Source.Owned] ref Control.Data control,
		[Source.Shared] ref Player.Data player, [Source.Singleton] ref Camera.Singleton camera)
		{
			var time = info.WorldTime;

			if (spectator.ent_followed_next.IsValid())
			{
				if (spectator.ent_followed_next.IsAlive())
				{
					spectator.ent_projectile = spectator.ent_followed_next;
					spectator.ref_transform_projectile.Set(spectator.ent_followed_next);
					spectator.t_follow_timeout = time + spectator.follow_timeout;
				}
			}

			spectator.ref_transform_followed.Set(spectator.ent_followed);

			var velocity = Vector2.Zero;
			var camera_pos_target = camera.position;
			var has_target = false;

			var camera_sensitivity = 1.00f;

			if (spectator.flags.HasAll(Spectator.Flags.Freemove | Spectator.Flags.Active))
			{
				//var move_dir = Vec2f.Zero;
				if (control.mouse.GetKey(spectator.camera_move_key) || (spectator.camera_move_key == Mouse.Key.None))
				{
					var delta = GUI.GetMouseScreenPos() - (GUI.CanvasSize * 0.50f);
					var dir = delta.GetNormalized(out var dir_len);
					velocity += dir * Maths.Min(dir_len * 0.020f * camera_sensitivity, 20).Pow(2.00f);
				}

				// TODO: hack
				if (spectator.camera_move_key == Mouse.Key.Middle)
				{
					Ping.Skip();
				}
			}

			if (spectator.flags.HasAll(Spectator.Flags.Manual_Zoom | Spectator.Flags.Active))
			{
				var zoom_delta = 0.00f;
				if (control.mouse.GetKey(Mouse.Key.Back)) zoom_delta -= spectator.zoom_speed;
				if (control.mouse.GetKey(Mouse.Key.Forward)) zoom_delta += spectator.zoom_speed;

				camera.zoom_target += (zoom_delta * 0.125f * 0.20f);
			}

			if (spectator.ent_projectile.IsAlive())
			{
				ref var projectile = ref spectator.ent_projectile.GetComponent<Projectile.Data>();
				if (projectile.IsNotNull())
				{
					velocity = projectile.velocity;
				}
			}

			if (spectator.ref_transform_projectile.entity.IsValid() && spectator.ref_transform_projectile.entity == spectator.ent_followed_next)
			{
				//App.WriteLine("a");
				ref var transform_projectile = ref spectator.ref_transform_projectile.GetComponent();
				if (transform_projectile.IsNotNull())
				{
					camera_pos_target = transform_projectile.GetInterpolatedPosition(); // + (spectator.current_velocity * spectator.pred_mult);
				}
				else if (time > spectator.t_follow_timeout)
				{
					spectator.ref_transform_projectile.Reset();
					spectator.ent_projectile = Entity.None;
					//spectator.override_pos = null;
					//spectator.current_velocity = default;
				}
				else
				{
					camera_pos_target = spectator.current_position;
					spectator.current_velocity = default;
					velocity = default;
				}

				has_target = true;
			}
			else
			{
				ref var transform_target = ref spectator.ref_transform_followed.GetComponent();
				if (transform_target.IsNotNull())
				{
					camera_pos_target = transform_target.GetInterpolatedPosition();
					has_target = true;
				}
				else
				{
					spectator.ref_transform_followed.Reset();
				}
			}

			spectator.current_velocity = Maths.LerpFast(spectator.current_velocity, velocity, 0.10f);
			spectator.current_position = Maths.Lerp(spectator.current_position, camera_pos_target + (spectator.current_velocity * spectator.pred_mult), spectator.lerp_speed_pos);

			Camera.no_cull = spectator.flags.HasAny(Spectator.Flags.No_Cull);

			if (spectator.flags.HasAny(Spectator.Flags.Active))
			{
				if (has_target || spectator.flags.HasAny(Spectator.Flags.Freemove)) //.override_pos.HasValue)
				{
					//var camera_pos = camera.override_target_position ?? spectator.override_pos.Value;
					camera.override_target_position = spectator.current_position; // Maths.Lerp(camera_pos, spectator.current_position.Value + (spectator.current_velocity * spectator.pred_mult), spectator.lerp_speed_pos);
																				  //camera.distance_modifier = 10.00f;
																				  //camera.distance_modifier_current = 40.00f;
																				  //camera.distance_modifier = 10.00f;
																				  //camera.zoom_speed = 10.00f;
																				  //camera.zoom_modifier_current = 1.00f;


					camera.zoom_speed *= spectator.zoom_speed;

					if (spectator.flags.HasAny(Spectator.Flags.No_Mouse | Flags.Freemove))
					{
						camera.distance_modifier = 0.00f;
						camera.distance_modifier_current = 0.00f;
					}

					if (spectator.flags.HasAny(Spectator.Flags.No_Zoom))
					{
						camera.zoom = 1.00f;
						camera.zoom_min = 1.00f;
						camera.zoom_min_current = 1.00f;
						camera.zoom_max = 1.00f;
						camera.zoom_max_current = 1.00f;
					}
					else
					{
						camera.zoom_min = spectator.zoom_min;
						camera.zoom_max = spectator.zoom_max;
					}

					//camera.zoom = Maths.EaseInOutElastic(camera.zoom_target);

					camera.damp_modifier = spectator.damp_modifier;
					camera.zoom_modifier = spectator.zoom_modfier;
				}
				else
				{
					camera.override_target_position = null;
				}
			}
		}
#endif
	}
}
