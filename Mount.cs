
namespace TC2.Base.Components
{
	public static partial class Mount
	{
		[Flags]
		public enum Flags: uint
		{
			None = 0u,

			Locked = 1u << 0,

			//No_Force_Without_Controller = 1 << 1,
			No_Torque_Without_Controller = 1u << 2,

			Show_Trajectory = 1u << 3,
			Slider_Vertical = 1u << 4,
		}

		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public partial struct Data: IComponent
		{
			public Mount.Flags flags;
			public float angle;

			[Net.Ignore, Save.Ignore] public float torque_modifier;
			[Net.Ignore, Save.Ignore] public float last_angle;
		}

		public struct EditRPC: Net.IRPC<Mount.Data>
		{
			public float? angle;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Mount.Data data)
			{
				var sync = false;

				if (this.angle.HasValue)
				{
					data.angle = this.angle.Value;
					sync = true;
				}

				if (sync)
				{
					data.Sync(rpc.entity);
				}
			}
#endif
		}

		[Shitcode]
		public struct FireRPC: Net.IRPC<Mount.Data>
		{
			public Mouse.Key pressed_keys;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Mount.Data data)
			{
				ref var control = ref rpc.entity.GetComponent<Control.Data>();
				if (control.IsNotNull())
				{
					ref var player = ref rpc.connection.GetPlayerData();
					if (player.IsNotNull() && Interactor.CanInteractWith(player.ent_interactor, rpc.entity))
					{
						control.mouse.SetKeyPressed(this.pressed_keys, true);
						//control.mouse.SetKeyPressed(Mouse.Key.Left, true);
					}
				}
			}
#endif
		}

#if CLIENT
		//public static 

		public partial struct MountGUI: IGUICommand
		{
			public Entity ent_mount;
			public Mount.Data mount;
			public Attachment.Slot attachment_slot;

			public float angle_min;
			public float angle_max;

			public void Draw()
			{
				var vertical = this.mount.flags.HasAny(Mount.Flags.Slider_Vertical);
				using (var window = GUI.Window.InteractionMisc("Mount"u8, this.ent_mount, vertical ? new Vector2(48, 192) : new Vector2(200, 40), no_mouse_close: true))
				{
					window.DisableMouseClose();

					if (window.show)
					{
						//ref var region = ref Client.GetRegion();

						if (GUI.SliderAngle("Angle"u8, ref this.mount.angle, this.angle_min, this.angle_max, snap: 0.10f, size: vertical ? new(48, GUI.RmY - 40) : new(GUI.RmX - 64, 40), enabled: this.attachment_slot.flags.HasAny(Attachment.Slot.Flags.Manual_Aim), vertical: vertical))
						{
							var rpc = new Mount.EditRPC();
							rpc.angle = this.mount.angle;
							rpc.Send(this.ent_mount);
						}

						if (!vertical) GUI.SameLine();

						if (GUI.DrawButton("Fire"u8, size: GUI.Rm, enabled: this.attachment_slot.flags.HasAny(Attachment.Slot.Flags.Manual_Aim), color: GUI.col_button_yellow, keys: GUI.ButtonKeys.Left | GUI.ButtonKeys.Right).GetKeys(out var pressed_keys))
						{
							var rpc_fire = new FireRPC();
							rpc_fire.pressed_keys = pressed_keys.ToMouseKeys();
							rpc_fire.Send(this.ent_mount);
						}

						//if (sync)
						//{
						//	rpc.Send(this.ent_mount);
						//}
					}
				}
			}
		}

		[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnGUI(/*ISystem.Info info, */Entity ent_mount, [Source.Shared] in Mount.Data mount,
		[Source.Shared] in Interactable.Data interactable, [Source.Owned] in Attachment.Slot attachment_slot, [Source.Owned, Override] in Joint.Gear gear)
		{
			if (interactable.IsActive() && (attachment_slot.flags | attachment_slot.flags_editable).HasAny(Attachment.Slot.Flags.Manual_Aim))
			{
				var gui = new MountGUI()
				{
					ent_mount = ent_mount,
					mount = mount,
					attachment_slot = attachment_slot
				};

				gui.angle_min = gear.min;
				gui.angle_max = gear.max;

				gui.Submit();
			}
		}

		[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnGUITrajectory(ref Region.Data region, /*ISystem.Info info, Entity ent_mount, */[Source.Parent] in Mount.Data mount,
		[Source.Parent] in Interactable.Data interactable, /*[Source.Parent, Override] ref Joint.Gear gear, */[Source.Owned] in Transform.Data transform,
		[Source.Owned] ref Gun.Data gun, [Source.Owned] in Gun.State gun_state/*, [Source.Owned, Pair.Component<Gun.Data>] ref Inventory1.Data inventory_magazine*/)
		{
			if (App.debug_mode_gui_any && interactable.IsActive() && mount.flags.HasAny(Mount.Flags.Show_Trajectory))
			{
				Gun.DrawTrajectory(ref region, gun_state.resource_ammo.material, in gun, in transform);
			}
		}
#endif

#if SERVER
		[ISystem.Add(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("wrecked", true, Source.Modifier.Shared)]
		public static void OnAddWrecked(ISystem.Info info, Entity entity, [Source.Shared] ref Mount.Data mount,
		[Source.Owned] in Attachment.Slot attachment, [Source.Owned] ref Joint.Base joint_base)
		{
			var ent_item = entity.GetChild(Relation.Type.Child);
			if (ent_item.IsAlive())
			{
				ent_item.RemoveRelation(entity, Relation.Type.Child);
			}

			App.WriteLine("wreck attachment");
		}
#endif

		[ISystem.EarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnUpdateSeat(ISystem.Info info, Entity entity, [Source.Shared] ref Mount.Data mount, [Source.Owned] in Seat.Data seat)
		{
			if (mount.flags.HasAny(Mount.Flags.No_Torque_Without_Controller))
			{
				mount.torque_modifier = seat.flags.HasAny(Seat.Flags.Has_Passenger) ? 1.00f : 0.00f;
			}
		}

		//		[ISystem.Update(ISystem.Mode.Single, ISystem.Scope.Region, interval: 0.50f)]
		//		public static void OnUpdateTrajectory(ISystem.Info info, Entity ent_mount, [Source.Parent] ref Mount.Data mount, [Source.Parent] in Attachment.Slot attachment, [Source.Parent] ref Control.Data control,
		//		[Source.Parent] in Interactable.Data interactable, [Source.Parent, Override] ref Joint.Gear gear, [Source.Owned] in Transform.Data transform,
		//		[Source.Owned] ref Gun.Data gun, [Source.Owned, Pair.Component<Gun.Data>] ref Inventory1.Data inventory_magazine)
		//		{
		//			ref var region = ref info.GetRegion();

		//			if (attachment.flags.HasAll(Attachment.Slot.Flags.Manual_Aim))
		//			{		
		//				ref var material = ref inventory_magazine.resource.material.GetData();
		//				if (material.IsNotNull() && material.projectile_prefab.TryGetPrefab(out var prefab_projectile))
		//				{
		//					var pos_w_offset = transform.LocalToWorld(gun.muzzle_offset);
		//					//var dir = transform.GetDirection();

		//					//var velocity_jitter = 1.00f - (Maths.Clamp(gun.jitter_multiplier * 0.20f, 0.00f, 1.00f) * 0.50f);

		//					if (prefab_projectile.Root.TryGetComponentData<Projectile.Data>(out var projectile, true))
		//					{
		//						//var random_multiplier = random.NextFloatRange(0.90f * velocity_jitter, 1.10f);


		//						//var random_multiplier = ((0.90f * velocity_jitter) + 1.10f) * 0.50f;

		//						var p = control.mouse.position - pos_w_offset;
		//						p.Y *= -1.00f;

		//						var v = gun.velocity_multiplier * material.projectile_speed_mult;
		//						var g = region.GetGravity().Y * projectile.gravity;
		//						var d = projectile.damp;

		//						var sqrt = MathF.Sqrt((v * v * v * v) - (g * (g * (p.X * p.X) + (2.00f * p.Y * (v * v)))));
		//						var rot = MathF.Atan(((v * v) + sqrt) / (g * p.X));

		//#if SERVER
		//						WorldNotification.Push(ref region, $"{rot} {p} {d} {MathF.Exp(d)}", Color32BGRA.Red, control.mouse.position, lifetime: 0.50f);
		//#endif
		//						if (!float.IsNaN(rot))
		//						{
		//							mount.angle = -rot;
		//						}
		//					}
		//				}
		//			}
		//		}

		[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnUpdateGear(ISystem.Info info, Entity entity, [Source.Shared] ref Mount.Data mount,
		[Source.Shared] in Transform.Data transform, [Source.Shared] ref Body.Data body, [Source.Owned] in Attachment.Slot attachment, [Source.Owned] ref Joint.Base joint_base, [Source.Owned, Override] ref Joint.Gear gear)
		{
			if (attachment.flags.HasAny(Attachment.Slot.Flags.Manual_Aim))
			{
				gear.rotation = transform.WorldToLocalRotation(mount.angle, rotation: false, scale: false);

				// TODO: this overwrites mouse position on cabins with a driver
				//control.mouse.position = transform.LocalToWorld(joint_base.offset_a + new Vector2(8, 0).RotateByRad(gear.rotation));

				if (Maths.Abs(mount.angle - mount.last_angle) > 0.001f)
				{
					joint_base.Activate();
					mount.last_angle = mount.angle;
				}
			}
			else
			{
				mount.angle = gear.rotation;
			}

			if (mount.flags.HasAny(Mount.Flags.No_Torque_Without_Controller))
			{
				gear.modifier *= mount.torque_modifier;
			}
		}

		[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnUpdateControl([Source.Parent] in Transform.Data transform, [Source.Parent] in Attachment.Slot attachment, [Source.Parent] in Joint.Base joint_base, [Source.Parent, Override] in Joint.Gear gear, 
		[Source.Owned] ref Control.Data control)
		{
			if (attachment.flags.HasAny(Attachment.Slot.Flags.Manual_Aim))
			{
				control.mouse.position = transform.LocalToWorld(joint_base.offset_a + new Vector2(8, 0).RotateByRad(gear.rotation));
				//control.mouse.SetPosition(transform.LocalToWorld(joint_base.offset_a + new Vector2(8, 0).RotateByRad(gear.rotation)));
			}
		}
	}
}
