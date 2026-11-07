
namespace TC2.Base.Components
{
	public static partial class Grabber
	{
		[Flags]
		public enum Flags: uint
		{
			None = 0u,

			Can_Attach = 1u << 0,
			Requires_Holder = 1u << 1,
		}

		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Region)]
		public partial struct Data(): IComponent
		{
			public Grabber.Flags flags;
			public float radius = 1.00f;

			[Editor.Picker.Position(true)] public Vector2 offset;
			[Editor.Picker.Position(true)] public Vector2 offset_attached;
		}

		//		[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region)]
		//		public static void OnUpdate_A(ISystem.Info info, ref Region.Data region, Entity entity,
		//		[Source.Parent] ref Grabber.Data grabber, [Source.Parent] ref Joint.Base joint, [Source.Parent] in Control.Data control,
		//		[Source.Owned] in Transform.Data transform, [Source.Owned] ref Body.Data body, [Source.Owned] ref Attachment.Data attachable)
		//		{
		//			//if (grabber.flags.HasAny(Flags.Can_Attach))

		//			var ts = Timestamp.Now();
		//			Span<ShapeOverlapResult> results = stackalloc ShapeOverlapResult[8];
		//			if (region.TryOverlapBodyAll(ref body, ref results, require: Physics.Layer.Attachments, exclude: Physics.Layer.Stored))
		//			{
		//				const int children_capacity = 16;
		//				Span<Entity> children_buffer = stackalloc Entity[children_capacity];

		//				foreach (ref var result in results)
		//				{
		//#if CLIENT
		//					region.DrawDebugBody(ref result.GetBody(), Color32BGRA.Green);
		//#endif

		//					var ent_vehicle = result.entity;


		//					var children = children_buffer;
		//					ent_vehicle.GetChildren(ref children, Relation.Type.Instance);

		//					foreach (var ent_attachment in children)
		//					{
		//						ref var attachment = ref ent_attachment.GetComponent<Attachment.Slot>();
		//						if (attachment.IsNotNull() && !attachment.flags.HasAny(Attachment.Slot.Flags.Hidden))
		//						{
		//							var ent_vehicle_parent = ent_vehicle.GetParent(Relation.Type.Child).GetComponentOwner<Body.Data>(Relation.Type.Instance);
		//						}
		//					}
		//				}
		//			}
		//			var ts_elapsed = ts.GetMilliseconds();
		//#if CLIENT
		//			region.DrawDebugText(transform.position, $"{ts_elapsed:0.0000} ms", Color32BGRA.White);
		//#endif
		//		}

#if CLIENT
		public partial struct GrabberHUD: IGUICommand
		{
			public Entity ent_grabber;
			public Entity ent_attachable;

			public Grabber.Data grabber;
			public Body.Data body_grabber;
			public Body.Data body_attachable;
			public Transform.Data transform_attachable;
			public Attachment.Data attachable;

			public void Draw()
			{
				ref var region = ref this.ent_grabber.GetRegion();

				using (var window = GUI.Window.HUD("grabber.overlay"u8))
				{
					if (window.show)
					{
						TODO_AttachmentStuff(region: ref region,
							ent_grabber: this.ent_grabber,
							ent_attachable: this.ent_attachable,
							attachable: ref this.attachable,
							transform_attachable: ref this.transform_attachable,
							body_grabber: ref this.body_grabber,
							body_attachable: ref this.body_attachable);
					}
				}
			}
		}

		[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region), HasComponent<Joint.Base>(Source.Modifier.Parent)]
		public static void OnGUI_HUD(Entity ent_grabber, Entity ent_attachable,
		[Source.Parent] in Grabber.Data grabber, [Source.Parent] in Body.Data body_grabber,
		[Source.Owned] in Transform.Data transform, [Source.Owned] in Body.Data body, [Source.Owned] in Attachment.Data attachable)
		{
			if (Camera.IsVisible(Camera.CullType.Rect1x_Padded, transform.position))
			{
				var gui = new GrabberHUD()
				{
					ent_grabber = ent_grabber,
					ent_attachable = ent_attachable,

					body_attachable = body,
					body_grabber = body_grabber,
					grabber = grabber,
					transform_attachable = transform,
					attachable = attachable
				};
				gui.Submit();
			}
		}
#endif

		// TODO: temporary garbage shitcode, rework this
		[Shitcode, Shitcode]
		public static void TODO_AttachmentStuff(ref Region.Data region, Entity ent_grabber, Entity ent_attachable, 
		ref Attachment.Data attachable, ref Transform.Data transform_attachable, 
		ref Body.Data body_grabber, ref Body.Data body_attachable)
		{
			Span<ShapeOverlapResult> results = FixedArray.CreateSpan8NoInit<ShapeOverlapResult>(out var results_buffer);
			if (region.TryOverlapBodyAll(body: ref body_attachable, results: ref results, require: Physics.Layer.Attachments, exclude: Physics.Layer.Stored))
			{
				var attachable_tags = attachable.tags;
				var children = FixedArray.CreateSpan16NoInit<Entity>(out var children_buffer).AsSpanList();

				var ent_grabber_parent = body_grabber.GetParent();

				foreach (ref var result in results)
				{
					var ent_vehicle = result.entity;
					if (ent_vehicle == ent_grabber) continue;
					if (ent_vehicle == ent_attachable) continue;
					if (ent_vehicle == ent_grabber_parent) continue;

					ref var transform_vehicle = ref ent_vehicle.GetComponent<Transform.Data>();

					ent_vehicle.GetChildren(list: ref children, relation: Relation.Type.Instance);

					foreach (var ent_attachment in children)
					{
						ref var attachment = ref ent_attachment.GetComponent<Attachment.Slot>();
						if (attachment.IsNotNull() && attachment.flags.HasNone(Attachment.Slot.Flags.Hidden))
						{
							var flags = attachment.flags;

							var tags_filter = attachment.tags_filter;
							if (tags_filter.Evaluate(attachable_tags))
							{
								ref var joint_base = ref ent_attachment.GetComponent<Joint.Base>();
								if (joint_base.IsNotNull())
								{
									var offset = joint_base.offset_a - attachable.offset;

									var transform_tmp = transform_vehicle;
#if CLIENT
									transform_tmp.SetPosition(transform_vehicle.LocalToWorldInterpolated(offset));
#elif SERVER
									transform_tmp.SetPosition(transform_vehicle.LocalToWorld(offset));
#endif

									var dist_sq = Vector2.DistanceSquared(transform_tmp.position, transform_attachable.position);
									if (dist_sq < 2.00f.Pow2())
									{
										var is_occupied = joint_base.ent_attached != default;
										var is_in_distance = dist_sq < 1.00f.Pow2();
										var is_valid = !is_occupied && is_in_distance;

#if CLIENT
										//var error = default(string);

										//if (!tags_filter.Evaluate(attachable_tags))
										//{
										//	error = "Item not compatible with hardpoint.";
										//	//is_held_item_valid = false;
										//}

										var color = Color32BGRA.Yellow;
										if (is_occupied) color = Color32BGRA.Red;
										else if (is_in_distance) color = Color32BGRA.Green;

										//GUI.DrawEntity(ent_attachable, in transform_tmp, color: (error == null ? Color32BGRA.Green : Color32BGRA.Red).WithAlpha(150), layer: GUI.Layer.Background);
										GUI.DrawEntity(ent_attachable, in transform_tmp, color: color.WithAlpha(150), layer: GUI.Layer.Background);
#elif SERVER
										if (is_valid)
										{
											ent_attachable.AddRelation(ent_attachment, Relation.Type.Child, true);
										}
#endif

										return;
									}
								}
							}
						}
					}

					children.Clear();
				}
			}
		}

		[ISystem.PreUpdate.D(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnPreUpdate_D([Source.Owned] ref Grabber.Data grabber, [Source.Owned] ref Joint.Base joint)
		{
			if (grabber.offset_attached.IsNotZero()) joint.offset_b_override = grabber.offset_attached;
		}

#if SERVER
		//[ISystem.AddFirst(ISystem.Mode.Single, ISystem.Scope.Region), HasRelation(Source.Modifier.Shared, Relation.Type.Stored, true)]
		//public static void OnStore(Entity ent_grabber, [Source.Owned] ref Grabber.Data grabber, [Source.Owned] in Joint.Base joint)
		//{
		//	//var ent_child = ent_grabber.GetChild(Relation.Type.Child);
		//	//if (ent_child.IsAlive())
		//	//{
		//	//	App.WriteLine("tongs");

		//	//	ent_child.RemoveRelation(ent_grabber, Relation.Type.Child);
		//	//}
		//}

		//[ISystem.Monitor(ISystem.Mode.Single, ISystem.Scope.Region), HasRelation(Source.Modifier.SharedAny, Relation.Type.Child, true)]
		//public static void OnPickupChanged(ISystem.Info info, Entity ent_grabber, [Source.Owned] ref Grabber.Data grabber, [Source.Owned] in Joint.Base joint)
		//{
		//	App.WriteLine($"{info.EventType}; drop tongs");

		//	if (grabber.flags.HasAny(Grabber.Flags.Requires_Holder))
		//	{
		//		//var ent_child = ent_grabber.GetChild(Relation.Type.Child);

		//		var ent_child = joint.ent_attached;
		//		if (ent_child.IsAlive())
		//		{
		//			ent_child.RemoveRelation(ent_grabber, Relation.Type.Child);
		//		}
		//	}
		//	//var ent_child = ent_grabber.GetChild(Relation.Type.Child);
		//	//if (ent_child.IsAlive())
		//	//{
		//	//	App.WriteLine("tongs");

		//	//	ent_child.RemoveRelation(ent_grabber, Relation.Type.Child);
		//	//}
		//}

		//[ISystem.Monitor(ISystem.Mode.Single, ISystem.Scope.Region)]
		//public static void OnPickupChanged2(ISystem.Info info, Entity ent_grabber, [Source.Owned] ref Grabber.Data grabber, [Source.Parent] in Joint.Base joint_parent, [Source.Owned] in Joint.Base joint)
		//{
		//	App.WriteLine($"{info.EventType}; drop tongs2");
		//}


		//[ISystem.Monitor(ISystem.Mode.Single, ISystem.Scope.Region), HasComponent<Heat.Data>(Source.Modifier.Owned, true)]
		//public static void OnGrab(ref Region.Data region, ISystem.Info info, Entity ent_grabber, Entity ent_body_grabber,
		//[Source.Parent] )
		//{
		//	switch (info.EventType)
		//	{
		//		case ISystem.EventType.Add:
		//		{
		//			body.override_shape_layer.AddFlag(Physics.Layer.Heatable);
		//			body.MarkDirty();
		//		}
		//		break;

		//		case ISystem.EventType.Remove:
		//		{
		//			body.override_shape_layer.RemoveFlag(Physics.Layer.Heatable);
		//			body.MarkDirty();
		//		}
		//		break;
		//	}
		//}

		[Shitcode]
		[ISystem.Update.C(ISystem.Mode.Single, ISystem.Scope.Region), HasComponent<Joint.Base>(Source.Modifier.Owned)]
		public static void OnUpdate_C(ref Region.Data region, Entity ent_grabber,
		[Source.Owned] ref Grabber.Data grabber,
		[Source.Shared] in Transform.Data transform, [Source.Shared] in Control.Data control, [Source.Shared] ref Body.Data body_grabber)
		{
			if (control.mouse.GetKeyDown(Mouse.Key.Left))
			{
				var radius = grabber.radius;
				var pos = transform.LocalToWorld(grabber.offset);

				var ent_child = ent_grabber.GetChild(Relation.Type.Child);
				if (ent_child.IsAlive())
				{
					ent_child.RemoveRelation(ent_grabber, Relation.Type.Child);

					Inner(region: ref region, ent_grabber: ent_grabber, ent_attachable: ent_child, body_grabber: ref body_grabber);

					[Shitcode]
					static void Inner(ref Region.Data region, Entity ent_grabber, Entity ent_attachable, ref Body.Data body_grabber)
					{
						ref var attachable = ref ent_attachable.GetComponent<Attachment.Data>();
						if (attachable.IsNull()) return;

						ref var transform_attachable = ref ent_attachable.GetComponent<Transform.Data>();
						if (transform_attachable.IsNull()) return;

						ref var body_attachable = ref ent_attachable.GetComponent<Body.Data>();
						if (body_attachable.IsNull()) return;

						TODO_AttachmentStuff(region: ref region, ent_grabber: ent_grabber, ent_attachable: ent_attachable, 
							attachable: ref attachable, transform_attachable: ref transform_attachable, body_grabber: ref body_grabber, body_attachable: ref body_attachable);
					}
				}
				else
				{
					var results = FixedArray.CreateSpan16NoInit<OverlapResult>(out var results_buffer); // stackalloc OverlapResult[16];
					if (region.TryOverlapPointAll(world_position: pos, radius: radius, hits: ref results, mask: Physics.Layer.Holdable | Physics.Layer.Item | Physics.Layer.Attachable | Physics.Layer.Crate, require: Physics.Layer.Dynamic, exclude: Physics.Layer.Static | Physics.Layer.World | Physics.Layer.Essence | Physics.Layer.Gas | Physics.Layer.Fire | Physics.Layer.Water | Physics.Layer.Liquid | Physics.Layer.Stored | Physics.Layer.Held | Physics.Layer.Attached | Physics.Layer.Powder))
					{
						var index = -1;
						var nearest_distance = radius;

						for (var i = 0; i < results.Length; i++)
						{
							ref var result = ref results[i];

							if (result.distance < nearest_distance)
							{
								if (!result.entity.IsAlive()) continue;
								if (result.entity_parent != default) continue;
								if (result.entity == body_grabber.GetEntity() || result.entity == body_grabber.GetParent()) continue;
								//if (!hit.entity.HasComponent<Holdable.Data>()) continue;
								if (result.entity.GetParent(Relation.Type.Seat).IsAlive()) continue;
								if (result.entity.GetParent(Relation.Type.Child).IsAlive()) continue;

								nearest_distance = result.distance;
								index = i;
							}
						}

						if (index >= 0)
						{
							ref var nearest = ref results[index];

							if (nearest.GetRoot() != ent_grabber.GetRoot())
							{
								if (App.log_filter.HasAny(App.LogFlags.Debug)) App.WriteLine($"{nearest.entity}; {nearest.entity_parent}; {nearest.entity.GetParent(Relation.Type.Seat)}; {nearest.entity.GetParent(Relation.Type.Child)}");

								var offset_attached = nearest.WorldToLocal(nearest.world_position);

								ref var holdable = ref nearest.entity.GetComponent<Holdable.Data>();
								if (holdable.IsNotNull())
								{
									if (offset_attached.IsInDistance(holdable.offset, holdable.radius)) // < (holdable.radius * holdable.radius))
									{
										offset_attached = default;
										//joint.offset_b_override = holdable.offset;
									}
									//else
									//{
									//	joint.offset_b_override = pickup_pos_l;
									//}

									//entity.SyncComponent<Joint.Base>(ref joint);
								}

								grabber.offset_attached = offset_attached;
								grabber.Sync(ent_grabber);
								nearest.entity.AddRelation(ent_grabber, Relation.Type.Child, check_recursion: true);
							}
						}
					}
				}
			}
		}
#endif
	}
}
