
namespace TC2.Base.Components
{
	public static partial class Prosthesis
	{
		[Flags]
		public enum Flags: uint
		{
			None = 0u,

			Can_Attach = 1u << 0,
		}

		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Region)]
		public partial struct Data: IComponent
		{
			public Prosthesis.Flags flags;
			public float unused_00;

			[Save.NewLine]
			[Editor.Picker.Position(true)] public Vec2f offset;
		}

		[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnUpdate_A(ISystem.Info info, ref Region.Data region, Entity entity,
		[Source.Owned] ref Prosthesis.Data prosthesis, [Source.Parent] ref Joint.Base joint, [Source.Parent] in Control.Data control,
		[Source.Owned] in Transform.Data transform, [Source.Owned] ref Body.Data body)
		{

		}

		[WIP]
		public struct SetTargetRPC: Net.IRPC<Prosthesis.Data>
		{
			public Entity ent_target;

#if SERVER
			// TODO: add validation
			public void Invoke(Net.IRPC.Context rpc, ref Prosthesis.Data data)
			{
				Assert.Check(rpc.entity != this.ent_target);
				Assert.Check(this.ent_target.IsSameRegion(rpc.entity));
				//Assert.Alive(this.ent_target);

				if (this.ent_target.TryGetRecord(out var rec_target))
				{
					ref var joint_base = ref rec_target.GetComponent<Joint.Base>();
					Assert.IsNotNull(ref joint_base);
					Assert.Check(joint_base.flags.HasAll(Joint.Flags.Organic));
					Assert.Check(joint_base.state.HasNone(Joint.State.Attaching | Joint.State.Attached));

					rpc.entity.ReplaceRelation(this.ent_target, Relation.Type.Child, check_recursion: true);


				}
			}
#endif
		}

#if CLIENT
		public partial struct ProsthesisGUI: IGUICommand
		{
			public Entity ent_prosthesis;
			public Prosthesis.Data prosthesis;
			public Transform.Data transform;

			public void Draw()
			{
				using (var window = GUI.Window.InteractionMisc(title: "Prosthesis"u8, entity: this.ent_prosthesis, size: new Vector2(64, 0), no_mouse_close: true))
				{
					window.DisableMouseClose();

					if (window.show)
					{
						var ent_controlled = Client.GetControlledEntity();

						var children_span = FixedArray.CreateSpan8NoInit<Entity>(out var children_buffer);
						ent_controlled.GetChildren(ref children_span, Relation.Type.Instance);

						foreach (var ent_child in children_span)
						{
							ref var joint_base = ref ent_child.GetComponent<Joint.Base>();
							if (joint_base.IsNotNull() && joint_base.flags.HasAll(Joint.Flags.Organic))
							{
								using (GUI.ID<Prosthesis.Data, Joint.Base>.Push(ent_child))
								{
									if (GUI.DrawButton("Attach"u8, size: new(64, 40), 
									enabled: joint_base.state.HasNone(Joint.State.Attaching | Joint.State.Attached), 
									color: GUI.col_button_yellow, keys: GUI.ButtonKeys.Left).GetKeys(out var pressed_keys))
									{
										var rpc = new SetTargetRPC();
										rpc.ent_target = ent_child;
										rpc.Send(this.ent_prosthesis);
									}
								}
							}
						}

						//if (sync)
						//{
						//	rpc.Send(this.ent_prosthesis);
						//}
					}
				}
			}
		}

		[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnGUI(Entity ent_prosthesis, [Source.Owned] in Prosthesis.Data prosthesis,
		[Source.Owned] in Interactable.Data interactable, [Source.Owned] in Transform.Data transform)
		{
			if (interactable.IsActive())
			{
				var gui = new ProsthesisGUI()
				{
					ent_prosthesis = ent_prosthesis,
					prosthesis = prosthesis,
					transform = transform
				};
				gui.Submit();
			}
		}
#endif

		[ISystem.PreUpdate.D(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnPreUpdate_D([Source.Owned] ref Prosthesis.Data prosthesis, [Source.Parent] ref Joint.Base joint_parent)
		{
			if (prosthesis.offset) joint_parent.offset_b_override = prosthesis.offset;
		}
	}
}
