
namespace TC2.Base.Components
{
	public static partial class Newspaper
	{
		[IComponent.Data(Net.SendType.Reliable)]
		public partial struct Data: IComponent
		{
			public IText.Handle handle;
		}

#if CLIENT
		public struct NewspaperGUI: IGUICommand
		{
			public Entity ent_newspaper;

			public Newspaper.Data newspaper;
			public Transform.Data transform;

			public void Draw()
			{
				using (var window = GUI.Window.Interaction("Newspaper"u8, this.ent_newspaper))
				{
					this.StoreCurrentWindowTypeID(order: -100);
					if (window.show)
					{
						ref var region = ref ent_newspaper.GetRegion();

						using (GUI.Group.New(size: GUI.Rm, padding: new(4)))
						{
							ref var data = ref this.newspaper.handle.GetData();
							if (!data.IsNull())
							{
								using (GUI.Wrap.Push())
								{
									using (GUI.Group.New(size: new Vector2(GUI.RmX, 64), padding: new Vector2(4)))
									{
										GUI.TitleCentered(data.name, font: GUI.Font.Superstar, size: 30, pivot: new Vector2(0.00f, 0.00f));
									}
								}

								//GUI.NewLine(4);
								//GUI.Separator();
								//GUI.NewLine(4);

								ref var page = ref data.pages[0];

								using (GUI.Group.New(size: new Vector2(GUI.RmX, GUI.RmY), padding: new Vector2(4)))
								{
									using (GUI.Wrap.Push(GUI.RmX))
									{
										using (GUI.Group.New(size: new Vector2(GUI.RmX, 0), padding: new Vector2(4)))
										{
											GUI.Title(page.title.ToUpper(), font: GUI.Font.Superstar, size: 19);
										}

										//GUI.NewLine(4);
										//GUI.Separator();
										//GUI.NewLine(4);
									}

									using (var scrollbox = GUI.Scrollbox.New("scrollbox.content", GUI.Rm, padding: new Vector2(4)))
									{
										using (GUI.Wrap.Push(GUI.RmX))
										{
											GUI.Text(page.content, font: GUI.Font.Monaco, size: 14);
										}
									}
								}
							}
						}
					}
				}
			}
		}

		[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global)]
		public static void OnGUI(Entity entity, [Source.Owned] in Transform.Data transform, [Source.Owned] in Newspaper.Data newspaper, [Source.Owned] in Interactable.Data interactable)
		{
			if (interactable.IsActive())
			{
				var gui = new NewspaperGUI()
				{
					ent_newspaper = entity,

					newspaper = newspaper,
					transform = transform
				};
				gui.Submit();
			}
		}
#endif
	}
}
