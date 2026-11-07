namespace TC2.Base.Components
{
	public static partial class Communications
	{
		public enum Type: byte
		{
			Undefined = 0,

		}

		[Flags]
		public enum Tags: byte
		{
			None = 0,

			Military = 1 << 0,
			Trading = 1 << 1,
			Civilian = 1 << 2,
			Emergency = 1 << 3,
			Pirate = 1 << 4,
			Government = 1 << 5,
			Logistics = 1 << 6
		}

		[Flags]
		public enum Flags: ushort
		{
			None = 0,

			Public = 1 << 0,
			Encrypted = 1 << 1,

			Wireless = 1 << 2,

			Receiver = 1 << 3,
			Transmitter = 1 << 4,
		}

		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Global | IComponent.Scope.Region)]
		public struct Data(): IComponent
		{
			public Communications.Type type;
			public Communications.Tags tags;
			public Communications.Flags flags;

			public float strength = 100.00f;
			public float rate = 500.00f;
		}

		public struct ConfigureRPC: Net.IRPC<Communications.Data>
		{

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Communications.Data data)
			{
	
			}
#endif
		}

		[ISystem.Update.B(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global, flags: ISystem.Flags.Unchecked)]
		public static void OnUpdate(ISystem.Info.Common info, ref Region.Data.Common region,
		[Source.Owned] in Transform.Data transform, 
		[Source.Owned] ref Communications.Data communications, [Source.Owned, Optional] in Faction.Data faction)
		{

		}

#if CLIENT
		public struct CommunicationsGUI: IGUICommand
		{
			public Entity ent_communications;

			public Communications.Data communications;
			public Transform.Data transform;

			public IFaction.Handle h_faction;

			public void Draw()
			{
				using (var window = GUI.Window.Interaction("Communications"u8, this.ent_communications))
				{
					this.StoreCurrentWindowTypeID(order: -100);
					if (window.show)
					{
						ref var player = ref Client.GetPlayer();
						ref var region = ref ent_communications.GetRegion();

						//using (GUI.Group.New(size: new Vector2(GUI.RmX, GUI.RmY)))
						//{
						//	using (var group = GUI.Group.New(size: new Vector2(GUI.RmX, GUI.RmY), padding: new Vector2(8, 8)))
						//	{
						//		GUI.DrawBackground(GUI.tex_frame, group.GetOuterRect(), new(8));
						//	}
						//}

						//using (var window_side = window.BeginChildWindow("side.left", GUI.AlignX.Left, GUI.AlignY.Top, size: new(256, 264), padding: new(8), open: true))
						//{
						//	if (window_side.show)
						//	{
						//		using (GUI.Group.New(GUI.Rm))
						//		{
						//			using (var dock = GUI.Dock.New("Misc"))
						//			{
						//				GUI.DrawInventoryDock(Inventory.Type.Essence, new(48, 48));

						//				dock.SetSpace(size: GUI.Rm);
						//			}

						//			//GUI.Text("hi");
						//			//GUI.DrawInventoryDock(this.vehicle.inventory_type, GUI.Rm);
						//		}
						//	}
						//}
					}
				}
			}
		}

		[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global)]
		public static void OnGUI(ISystem.Info.Common info, ref Region.Data.Common region, Entity entity,
		[Source.Owned] in Transform.Data transform, [Source.Owned] in Interactable.Data interactable,
		[Source.Owned] in Communications.Data communications, [Source.Owned, Optional] in Faction.Data faction)
		{
			if (interactable.IsActive())
			{
				var gui = new CommunicationsGUI()
				{
					ent_communications = entity,
					communications = communications,
					transform = transform,
					h_faction = faction.id
				};
				gui.Submit();
			}
		}
#endif
	}
}
