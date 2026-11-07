
namespace TC2.Base.Components
{
	public static partial class Essence
	{
		public static partial class Evictor
		{
			public enum Type: byte
			{
				Undefined = 0,

				Mono,
				Dual,
				Triple
			}

			[Flags]
			public enum Flags: ushort
			{
				None = 0,


			}

			[Flags]
			public enum Tags: uint
			{
				None = 0,


			}

			[Flags]
			public enum StateFlags: ushort
			{
				None = 0,

				//Charging = 1 << 0,

				//[Asset.Ignore] Discharging = 1 << 4,
				//[Asset.Ignore] Discharged = 1 << 5,

				//[Asset.Ignore] Pulsing = 1 << 6,
				//[Asset.Ignore] Pulsed = 1 << 7,
			}

			// TODO: mem layout
			[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Global | IComponent.Scope.Region)]
			public partial struct Data(): IComponent
			{
				[Net.Segment.A, Save.Force, Editor.Picker.Position(relative: true)] public required Vec2f offset;
				[Net.Segment.A, Save.Force] public required Vec2f size;
				[Net.Segment.A, Save.Force, Editor.Picker.Direction(normalize: true)] public required Vec2f direction = Vec2f.Right;

				[Save.NewLine]
				[Net.Segment.A, Save.Force] public required Essence.Evictor.Type type;
				[Net.Segment.A] private byte unused_a_00;
				[Net.Segment.A, Save.Force] public Essence.Evictor.Flags flags;
				[Net.Segment.A, Save.Force] public Essence.Evictor.Tags tags;
				[Net.Segment.A, Save.Force] public required float speed;
				[Net.Segment.A, Save.Force] public required float ratio_step;

				[Save.NewLine]
				[Net.Segment.A] public required Sprite arm_sprite;

				[Save.NewLine]
				[Net.Segment.A, Save.Force, Editor.Picker.Position(relative: true)] public required Vec2f arm_offset;
				[Net.Segment.A, Save.Force, Editor.Picker.Position(relative: true)] public required Vec2f tip_offset;
				[Net.Segment.A, Save.Force] public required float arm_length;
				[Net.Segment.A, Save.Force] public required float arm_rotation_a;
				[Net.Segment.A, Save.Force] public required float arm_rotation_b;
				[Net.Segment.A, Save.Force] public float arm_z;

				[Net.Segment.A, Save.Force] public required float arm_bounce;
				[Net.Segment.A, Save.Force] public required float arm_acc_a;
				[Net.Segment.A, Save.Force] public required float arm_acc_b;

				[Net.Segment.A, Save.Force] public required float charge_ratio_a = 0.40f;
				[Net.Segment.A, Save.Force] public required float charge_ratio_b = 0.30f;
				[Net.Segment.A, Save.Force] public required float charge_ratio_c = 0.30f;

				[Net.Segment.A, Editor.Slider.Clamped(0.00f, 1.00f, snap: 0.001f)] public required Percentage containment = Percentage.Max;
				[Net.Segment.A, Editor.Slider.Clamped(0.00f, 1.00f, snap: 0.001f)] public required Percentage stability = Percentage.Max;

				[Save.NewLine]
				[Net.Segment.B, Asset.Ignore] public IEssence.Handle h_essence;
				[Net.Segment.B] public ISoundMix.Handle h_soundmix_pulse;
				[Net.Segment.B] public ISoundMix.Handle h_soundmix_arm_retract;
				[Net.Segment.B] public ISoundMix.Handle h_soundmix_arm_launch;

				[Save.NewLine]
				[Net.Segment.C, Asset.Ignore] public Essence.Evictor.ActionEvent.Type current_event_type;
				[Net.Segment.C, Asset.Ignore] public Essence.Evictor.StateFlags state_flags;
				[Net.Segment.C] public float phase_release = 0.90f;
				[Net.Segment.C] public float phase_activate = 0.10f;

				//[Net.Segment.C, Asset.Ignore] public IEssence.Handle h_essence_charge;
				//[Net.Segment.C] public Signal.Channel channel_emit;
				//[Net.Segment.C] private byte unused_c_02;
				//[Net.Segment.C] private ushort unused_c_03;

				[Save.NewLine]
				[Net.Segment.D, Asset.Ignore] public float current_phase;
				[Net.Segment.D, Asset.Ignore] public float current_ratio_target;
				[Net.Segment.D, Asset.Ignore] public float current_intensity;
				[Net.Segment.D, Asset.Ignore] public float current_speed;
				[Net.Segment.D, Asset.Ignore] public float current_arm_angular_velocity;
				[Net.Segment.D, Asset.Ignore] public float unused_d_03;

				[Save.NewLine]
				[Net.Segment.D, Asset.Ignore] public EntRef<Essence.Bubble.Data> ref_bubble;

				[Net.Segment.D, Asset.Ignore] public Vec2f wpos_tip;
				[Net.Segment.D, Asset.Ignore] public Vec2f wpos_arm_a;
				[Net.Segment.D, Asset.Ignore] public Vec2f wpos_arm_b;

				[Net.Segment.D, Asset.Ignore] public Vec2f vel_tip;
				[Net.Segment.D, Asset.Ignore] public Vec2f vel_arm_a;
				[Net.Segment.D, Asset.Ignore] public Vec2f vel_arm_b;



				//[Save.Force, Editor.Picker.Position(relative: true)] public required Vec2f offset;
				//[Save.Force, Editor.Picker.Direction(normalize: true)] public required Vec2f direction = Vec2f.Up;

				//[Editor.Slider.Clamped(min: 0.00f, max: 16.00f, snap: 0.001f)]
				//[Save.Force] public required float radius;
				//[Save.Force] public required float capacity;
				//[Save.Force] public float efficiency = 1.00f;

				//[Save.Force] public Essence.Evictor.Flags flags;
				//public ushort reserved_00;
				//public uint reserved_01;
			}

			[IEvent.Data]
			public partial struct ActionEvent(): IEvent
			{
				public enum Type: ushort
				{
					Undefined = 0,

					Reset,
					Charge,
					Launch
				}

				public IEssence.Handle h_essence;
				public ActionEvent.Type type;
				public ushort unused_00;

				public uint unused_01;

				public Vec2f world_position;

				public float essence_amount_returned = 0.00f;
				public float essence_amount_precipitated = 0.00f;
				public XorRandom random;
			}

			public struct ConfigureRPC: Net.IRPC<Essence.Evictor.Data>
			{
				public float? ratio_target;
				public float? phase_release;
				public float? phase_detach;
				public float? phase_activate;

#if SERVER
				public void Invoke(Net.IRPC.Context rpc, ref Essence.Evictor.Data data)
				{
					//if (data.flags.HasAny(Essence.Container.Flags.Faction))
					//{
					//	var h_faction_client = rpc.connection.GetFactionHandle();
					//	var h_faction = rpc.record.GetFactionHandle();

					//	Assert.Check(h_faction.IsAccessible(h_faction_client));
					//}

					var sync = false;
					sync |= this.ratio_target.TryCopyToClamped01(ref data.current_ratio_target);
					sync |= this.phase_release.TryCopyToClamped01(ref data.phase_release);
					sync |= this.phase_activate.TryCopyToClamped01(ref data.phase_activate);
					//sync |= this.phase_detach.TryCopyToClamped01(ref data.phase_detach);
					//sync |= this.power_target_ratio.TryCopyToClamped01(ref data.power_target_ratio);

					if (sync)
					{
						data.Sync(rpc.entity, true);
					}
				}
#endif
			}

			public struct ActionRPC: Net.IRPC<Essence.Evictor.Data>
			{
				public ActionEvent.Type type;

#if SERVER
				public void Invoke(Net.IRPC.Context rpc, ref Essence.Evictor.Data data)
				{
					//if (data.flags.HasAny(Essence.Container.Flags.Faction))
					//{
					//	var h_faction_client = rpc.connection.GetFactionHandle();
					//	var h_faction = rpc.record.GetFactionHandle();

					//	Assert.Check(h_faction.IsAccessible(h_faction_client));
					//}

					var sync = true;
					var ev = new Essence.Evictor.ActionEvent();
					ev.type = this.type;

					data.current_event_type = this.type;

					switch (this.type)
					{
						case ActionEvent.Type.Reset:
						{
							data.current_ratio_target = 0.00f;
							Sound.Play(ref rpc.GetRegion(), data.h_soundmix_arm_retract, pos: rpc.entity.GetPosition());
						}
						break;

						case ActionEvent.Type.Charge:
						{
							//data.current_ratio = 0.00f;
						}
						break;

						case ActionEvent.Type.Launch:
						{
							data.current_ratio_target = 1.00f;
							Sound.Play(ref rpc.GetRegion(), data.h_soundmix_arm_launch, pos: rpc.entity.GetPosition());
						}
						break;
					}

					if (sync)
					{
						data.Sync(rpc.entity, true);
					}

					ev.TriggerDeferred(rpc.entity, sync: true);
				}
#endif
			}

			[ISystem.Event<Essence.Evictor.ActionEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
			public static void OnActionEvent(ISystem.Info info, Entity entity, ref XorRandom random, ref Region.Data region, [Source.Owned] ref Essence.Evictor.ActionEvent ev,
			[Source.Owned] in Transform.Data transform,
			[Source.Owned] ref Essence.Container.Data container, [Source.Owned] ref Essence.Evictor.Data evictor)
			{
				App.WriteValue(ev.type);

				switch (ev.type)
				{
					case ActionEvent.Type.Reset:
					{
						ref var bubble = ref evictor.ref_bubble.GetComponent();
						if (bubble.IsNotNull())
						{
							bubble.flags.RemoveFlag(Bubble.Flags.Active);

							bubble.velocities.Clear();
#if SERVER
							bubble.Sync(evictor.ref_bubble.entity);
#endif
						}
					}
					break;

					case ActionEvent.Type.Charge:
					{
#if SERVER
						if (evictor.h_essence && !evictor.ref_bubble.IsAlive())
						{
							var h_prefab_bubble = new Prefab.Handle("essence.bubble.00");
							region.SpawnPrefab(h_prefab_bubble, position: transform.LocalToWorld(evictor.tip_offset), ent_parent: entity);
						}
#endif
					}
					break;

					case ActionEvent.Type.Launch:
					{
						ref var bubble = ref evictor.ref_bubble.GetComponent();
						if (bubble.IsNotNull())
						{
							bubble.flags.AddFlag(Bubble.Flags.Active);
#if SERVER
							bubble.Sync(evictor.ref_bubble.entity);
#endif
						}
					}
					break;
				}
			}

			[ISystem.Add(ISystem.Mode.Single, ISystem.Scope.Region)]
			public static void OnAddBubble(ISystem.Info info, Entity entity, ref XorRandom random, ref Region.Data region,
			[Source.Owned] ref Transform.Data transform_child, [Source.Parent] ref Transform.Data transform_parent,
			[Source.Owned] ref Essence.Bubble.Data bubble, [Source.Parent] ref Essence.Evictor.Data evictor)
			{
				App.WriteLine("add bubble");
				evictor.ref_bubble.Set(entity);
			}

			[ISystem.Update.C(ISystem.Mode.Single, ISystem.Scope.Region)]
			public static void OnUpdateBubble_C(ISystem.Info info, Entity entity, ref XorRandom random, ref Region.Data region,
			[Source.Owned] ref Transform.Data transform_child, [Source.Parent] ref Transform.Data transform_parent,
			[Source.Owned] ref Essence.Bubble.Data bubble, [Source.Parent] ref Essence.Evictor.Data evictor)
			{
				var t = evictor.current_phase;
				var angle_arm = Maths.LerpAngle(evictor.arm_rotation_a, evictor.arm_rotation_b, t);

				var arm_offset_tip = evictor.arm_offset + (Maths.RadToDir(-angle_arm) * evictor.arm_length * 0.75f);

				bubble.h_essence = evictor.h_essence;
				bubble.amount = evictor.current_intensity;

				if (evictor.current_event_type == ActionEvent.Type.Launch)
				{
					if (evictor.current_phase < evictor.phase_release)
					{
						//var ratio_inv = 1.00f; // (1.00f - evictor.current_ratio);
						var phase_inv = (1.00f - evictor.current_phase).Pow2();

						//if (evictor.current_event_type != ActionEvent.Type.Launch)
						{
							bubble.positions[0].LerpFMARef(evictor.wpos_tip, phase_inv * 0.50f);
							bubble.positions[1].LerpFMARef(evictor.wpos_arm_a, phase_inv * 0.50f);
							bubble.positions[2].LerpFMARef(evictor.wpos_arm_b, phase_inv * 0.50f);
						}

						var dir = transform_parent.LocalToWorldDirection(evictor.direction) * 80; // * evictor.current_ratio;
																								  //var distance_v = (evictor.wpos_arm_a.y - evictor.wpos_tip.y).Abs();
						var distance_v = 1.00f - Vec2f.Dot(evictor.wpos_arm_a, evictor.wpos_tip).Abs();
						var distance_mult = Maths.InvLerp01(0.80f, 0.10f, distance_v);

#if SERVER
					//App.WriteValue((distance_v, distance_mult, evictor.current_ratio));
#endif

						var vel_mult = 15.00f * distance_mult;

						//App.WriteValue(dir);

						bubble.velocities[0] += vel_mult * (evictor.vel_tip); // * ratio_inv;
						bubble.velocities[1] += vel_mult * (evictor.vel_arm_a); // * ratio_inv;
						bubble.velocities[2] += vel_mult * (evictor.vel_arm_b); // * ratio_inv;

						var intensity_mult = 0.002f;
						bubble.intensities[0] = evictor.current_intensity * evictor.charge_ratio_a * intensity_mult;
						bubble.intensities[1] = evictor.current_intensity * evictor.charge_ratio_b * intensity_mult;
						bubble.intensities[2] = evictor.current_intensity * evictor.charge_ratio_c * intensity_mult;

						bubble.flags.AddFlag(Bubble.Flags.Active);
					}
					else
					{
						evictor.ref_bubble.Reset();
						bubble.flags.AddFlag(Bubble.Flags.Released);

#if SERVER
						entity.RemoveRelation(Entity.Wildcard, Relation.Type.Child);

#endif
					}
				}

				//if (evictor.current_ratio < 0.95f)
				//{
				//	var ratio_inv = 1.00f - evictor.current_ratio;
				//	bubble.offset_a.LerpFMARef(transform_child.WorldToLocal(transform_parent.LocalToWorld(evictor.tip_offset)), ratio_inv);
				//	bubble.offset_b.LerpFMARef(transform_child.WorldToLocal(transform_parent.LocalToWorld(arm_offset_tip)), ratio_inv);
				//	bubble.offset_c.LerpFMARef(transform_child.WorldToLocal(transform_parent.LocalToWorld(arm_offset_tip.MulY(-1))), ratio_inv);
				//}

				//App.WriteLine("update bubble");
			}

			[ISystem.PreUpdate.A(ISystem.Mode.Single, ISystem.Scope.Region)]
			public static void OnPreUpdate_A(ISystem.Info info, ref XorRandom random, ref Region.Data region, Entity entity,
			[Source.Owned] ref Transform.Data transform,
			[Source.Owned] ref Essence.Evictor.Data evictor)
			{
				var ang_delta = Maths.DeltaAngle(evictor.arm_rotation_a, evictor.arm_rotation_b);
				var ang_delta_inv = ang_delta.Rcp();
				//evictor.current_ratio = Maths.MoveTowards(evictor.current_ratio, evictor.current_ratio_target, evictor.ratio_step);

				switch (evictor.current_event_type)
				{
					case ActionEvent.Type.Reset:
					{
						if (evictor.current_phase > 0.00f) evictor.current_arm_angular_velocity -= evictor.arm_acc_a * App.fixed_update_interval_s;
						else evictor.current_arm_angular_velocity *= -evictor.arm_bounce;
					}
					break;

					case ActionEvent.Type.Charge:
					{

					}
					break;

					case ActionEvent.Type.Launch:
					{
						if (evictor.current_phase < 1.00f) evictor.current_arm_angular_velocity += evictor.arm_acc_b * App.fixed_update_interval_s;
						else evictor.current_arm_angular_velocity *= -evictor.arm_bounce;
					}
					break;
				}

				evictor.unused_d_03 = Maths.Clamp(evictor.unused_d_03 + (evictor.current_arm_angular_velocity * App.fixed_update_interval_s), evictor.arm_rotation_a, evictor.arm_rotation_b);
				evictor.current_phase = Maths.InvLerp01(evictor.arm_rotation_a, evictor.arm_rotation_b, evictor.unused_d_03);
			}

			[ISystem.PreUpdate.B(ISystem.Mode.Single, ISystem.Scope.Region)]
			public static void OnPreUpdate_B(ISystem.Info info, ref XorRandom random, ref Region.Data region, Entity entity,
			[Source.Owned] in Transform.Data transform, [Source.Owned] ref Essence.Evictor.Data evictor)
			{
				var t = evictor.current_phase;
				var angle_arm = Maths.LerpAngle(evictor.arm_rotation_a, evictor.arm_rotation_b, t);
				var arm_offset_tip = evictor.arm_offset + (Maths.RadToDir(-angle_arm) * evictor.arm_length * 0.75f);

				var wpos_tip_new = transform.LocalToWorld(evictor.tip_offset);
				var wpos_arm_a_new = transform.LocalToWorld(arm_offset_tip);
				var wpos_arm_b_new = transform.LocalToWorld(arm_offset_tip.MulY(-1));

				evictor.vel_tip = Vec2f.GetDelta(evictor.wpos_tip, wpos_tip_new);
				evictor.vel_arm_a = Vec2f.GetDelta(evictor.wpos_arm_a, wpos_arm_a_new);
				evictor.vel_arm_b = Vec2f.GetDelta(evictor.wpos_arm_b, wpos_arm_b_new);

				evictor.wpos_tip = wpos_tip_new;
				evictor.wpos_arm_a = wpos_arm_a_new;
				evictor.wpos_arm_b = wpos_arm_b_new;
			}

			[ISystem.Update.C(ISystem.Mode.Single, ISystem.Scope.Region)]
			public static void OnUpdate_C(ISystem.Info info, ref XorRandom random, ref Region.Data region, Entity entity,
			[Source.Owned] ref Transform.Data transform,
			[Source.Owned] ref Essence.Evictor.Data evictor)
			{

			}

			[ISystem.PostUpdate.D(ISystem.Mode.Single, ISystem.Scope.Region)]
			public static void OnPostUpdate_D([Source.Owned] ref Essence.Container.Data essence_container,
			[Source.Owned] ref Essence.Evictor.Data essence_evictor)
			{
				essence_evictor.h_essence = essence_container.h_essence;
				essence_evictor.current_intensity = essence_container.GetEmittedEssenceAmount();
			}


#if CLIENT
			[ISystem.Render(ISystem.Mode.Single, ISystem.Scope.Region)]
			public static void OnRender_A(ISystem.Info info, ref XorRandom random, ref Region.Data region, Entity entity,
			[Source.Owned] in Transform.Data transform,
			[Source.Owned] ref Essence.Evictor.Data evictor,
			[Source.Owned, Pair.Component<Essence.Evictor.Data>, Optional(true)] ref Essence.Charge.Data charge,
			[Source.Owned, Pair.Component<Essence.Evictor.Data>, Optional(true)] ref Light.Data light)
			{
				if (light.IsNotNull())
				{
					//light.color = evictor.h_essence.GetEmitColor();
					//light.intensity = (evictor.current_charge_ratio * 2.00f).Sqrt();
				}
			}

			[ISystem.PostRender(ISystem.Mode.Single, ISystem.Scope.Region)]
			public static void OnPostRender(ISystem.Info info, ref XorRandom random, ref Region.Data region, Entity entity,
			[Source.Owned] in Transform.Data transform, [Source.Owned] in Animated.Renderer.Data renderer,
			[Source.Owned] ref Essence.Evictor.Data evictor)
			{
				var t = evictor.current_phase;

				var renderer_tmp = renderer;
				renderer_tmp.sprite = evictor.arm_sprite;

				{
					var arm_offset = evictor.arm_offset;

					renderer_tmp.rotation = Maths.LerpAngle(evictor.arm_rotation_a, evictor.arm_rotation_b, t);
					renderer_tmp.offset = arm_offset + new Vec2f(evictor.arm_length * 0.50f, 0.00f).RotateByRad(-renderer_tmp.rotation);
					renderer_tmp.z += evictor.arm_z;

					Animated.Renderer.Draw(renderer.transform_index, renderer_tmp);
				}

				{
					var arm_offset = evictor.arm_offset;
					arm_offset.Y.FlipSign();

					renderer_tmp.rotation = Maths.LerpAngle(evictor.arm_rotation_a, evictor.arm_rotation_b, t).MirrorAngleY();
					renderer_tmp.offset = arm_offset + new Vec2f(evictor.arm_length * 0.50f, 0.00f).RotateByRad(-renderer_tmp.rotation);
					renderer_tmp.z += evictor.arm_z;
					renderer_tmp.scale.Y.FlipSign();

					Animated.Renderer.Draw(renderer.transform_index, renderer_tmp);
				}
			}
#endif

#if CLIENT
			public struct EvictorGUI: IGUICommand
			{
				public Entity ent_evictor;
				public Transform.Data transform;
				public Essence.Evictor.Data evictor;

				public void Draw()
				{
					using (var window = GUI.Window.Interaction("Evictor"u8, this.ent_evictor))
					{
						this.StoreCurrentWindowTypeID(order: -100);
						if (window.show)
						{
							using (GUI.Group.New(size: GUI.Rm))
							{
								if (GUI.SliderFloat(label: "Target"u8,
								value: ref this.evictor.current_ratio_target,
								min: 0.00f,
								max: 1.00f,
								snap: 0.001f,
								//enabled: is_accessible,
								size: new Vector2(GUI.RmX, 24)))
								{
									var rpc = new Essence.Evictor.ConfigureRPC
									{
										ratio_target = this.evictor.current_ratio_target
									};
									rpc.Send(this.ent_evictor);
								}

								if (GUI.SliderFloat(label: "Activation Phase"u8,
								value: ref this.evictor.phase_activate,
								min: 0.00f,
								max: 1.00f,
								snap: 0.001f,
								//enabled: is_accessible,
								size: new Vector2(GUI.RmX * 0.50f, 24)))
								{
									var rpc = new Essence.Evictor.ConfigureRPC
									{
										phase_activate = this.evictor.phase_activate
									};
									rpc.Send(this.ent_evictor);
								}

								if (GUI.SliderFloat(label: "Release Phase"u8,
								value: ref this.evictor.phase_release,
								min: 0.00f,
								max: 1.00f,
								snap: 0.001f,
								//enabled: is_accessible,
								size: new Vector2(GUI.RmX * 0.50f, 24)))
								{
									var rpc = new Essence.Evictor.ConfigureRPC
									{
										phase_release = this.evictor.phase_release
									};
									rpc.Send(this.ent_evictor);
								}


								using (GUI.Group.New(size: new(GUI.RmX - 128, 0), padding: new(0)))
								{
									if (GUI.DrawButton("Reset"u8, size: new(80, 32)))
									{
										var rpc = new Essence.Evictor.ActionRPC
										{
											type = ActionEvent.Type.Reset
										};
										rpc.Send(this.ent_evictor);
									}

									if (GUI.DrawButton("Charge"u8, size: new(80, 32)))
									{
										var rpc = new Essence.Evictor.ActionRPC
										{
											type = ActionEvent.Type.Charge
										};
										rpc.Send(this.ent_evictor);
									}

									if (GUI.DrawButton("Launch"u8, size: new(80, 32)))
									{
										var rpc = new Essence.Evictor.ActionRPC
										{
											type = ActionEvent.Type.Launch
										};
										rpc.Send(this.ent_evictor);
									}

									//GUI.DrawHorizontalGauge(this.evictor.current_charge, this.evictor.charge_capacity, size: new(GUI.RmX, 24));

									//GUI.LabelShaded("Total:"u8, this.evictor.current_charge, format: "0.00' Ef'", width: GUI.RmX * 0.75f);
									//GUI.LabelShaded("Net:"u8, this.evictor.current_charge_add - this.evictor.current_charge_loss, format: "0.00' Ef'", width: GUI.RmX * 0.75f);
									//GUI.LabelShaded("In:"u8, this.evictor.current_charge_add, format: "0.00' Ef'", width: GUI.RmX * 0.75f);
									//GUI.LabelShaded("Loss:"u8, this.evictor.current_charge_loss, format: "0.00' Ef'", width: GUI.RmX * 0.75f);

								}

								//GUI.TextShaded("Derpo"u8);
							}
						}
					}
				}
			}

			[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region)]
			public static void OnGUI(Entity entity,
			[Source.Owned] in Interactable.Data interactable, [Source.Owned] in Transform.Data transform,
			[Source.Owned] in Essence.Evictor.Data evictor)
			{
				if (interactable.IsActive())
				{
					var gui = new EvictorGUI()
					{
						ent_evictor = entity,
						transform = transform,
						evictor = evictor,
					};
					gui.Submit();
				}
			}
#endif
		}
	}
}