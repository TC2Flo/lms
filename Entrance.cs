using System.Diagnostics.CodeAnalysis;
using TC2.Conquest;

namespace TC2.Base.Components
{
	public static partial class Entrance
	{
		[Query(ISystem.Scope.Region, Query.Flags.None)]
		public delegate void GetEntrancesQuery(ISystem.Info info, ref Region.Data region, Entity ent_entrance, Entity ent_transform,
			[Source.Owned] in Entrance.Data entrance, [Source.Shared] in Transform.Data transform,
			[Source.Shared, Optional] in Spawn.Data spawn, [Source.Shared, Optional] in Faction.Data faction);

		public static (Entity entity, IEntrance.Handle h_entrance) GetNearest(ref Region.Data region, Vector2 pos, out float dist_sq, IFaction.Handle h_faction = default, Entity ent_exclude = default)
		{
			var dist_sq_current = float.MaxValue;
			var ent_nearest = Entity.None;
			var ent_nearest_entrance = Entity.None;

			foreach (ref var row in region.IterateQuery<Entrance.GetEntrancesQuery>())
			{
				var index = row.Index;
				var count = row.Count;

				row.Run((ISystem.Info info, ref Region.Data region,
				Entity ent_entrance, Entity ent_transform,
				[Source.Owned] in Entrance.Data entrance,
				[Source.Shared] in Transform.Data transform,
				[Source.Shared, Optional] in Spawn.Data spawn,
				[Source.Shared, Optional] in Faction.Data faction) =>
				{
					if ((ent_entrance != ent_exclude && ent_transform != ent_exclude) && (!h_faction || h_faction == faction.id))
					{
						var dist_sq_tmp = Vector2.DistanceSquared(pos, transform.position); // - enterable.radius.Pow2();
						if (dist_sq_tmp < dist_sq_current)
						{
							dist_sq_current = dist_sq_tmp;
							ent_nearest = ent_transform;
							ent_nearest_entrance = ent_entrance;
						}
					}
				});
			}

			dist_sq = dist_sq_current;
			ent_nearest_entrance.TryGetAssetHandle(out IEntrance.Handle h_entrance);

			return (ent_nearest, h_entrance);
		}

		[Flags]
		public enum Flags: ushort
		{
			None = 0,

			Force_Spawn = 1 << 0,
			Manual_Exit = 1 << 1,
		}

		public enum Action: byte
		{
			Undefined = 0,

			Enter,
			Trespass,
			Leave
		}

		public static partial class Linkable
		{
			[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Global | IComponent.Scope.Region)]
			public struct Data(): IComponent
			{
				public Entity ent_entrance;
				[Editor.Picker.Position(true, true)]
				public Vector2 offset;

				public IEntrance.Handle h_entrance;
				[Unused] public float radius = 16.00f;
			}

#if SERVER
			[Shitcode]
			[ISystem.Modified.Component<Entrance.Linkable.Data>(ISystem.Mode.Single, ISystem.Scope.Region, order: 100, Defer = true), HasTag("initialized", true, Source.Modifier.Owned)]
			public static void OnModified(ISystem.Info info, ref Region.Data region, Entity ent_linkable,
			[Source.Owned] in Entrance.Linkable.Data linkable,
			[Source.Owned] ref Address.Data address)
			{
				//App.WriteLine($"Entrance.Linkable.OnModified: {linkable.h_entrance}; {ent_linkable}; {(address.IsNotNull() ? address.h_address : default)}", App.Color.Magenta);

				//if (linkable.h_entrance == 0)
				//{
				//	var (ent_entrance_nearest, h_entrance_nearest) = Entrance.GetNearest(ref region, transform.position, out var dist_sq);
				//	if (ent_entrance_nearest.IsAlive())
				//	{
				//		linkable.h_entrance = h_entrance_nearest;
				//		linkable.Sync(ent_linkable, true);
				//	}
				//}

				if (address.h_address.TryGetDefinition(out var address_asset))
				{
					var dirty = false;

					if (address_asset.data.h_entrance == 0)
					{
						address_asset.data.h_entrance = linkable.h_entrance;
						dirty = true;
					}

					if (address_asset.data.h_location == 0)
					{
						address_asset.data.h_location = region.GetLocationHandle();
						dirty = true;
					}

					if (address_asset.data.entity != ent_linkable)
					{
						address_asset.data.entity = ent_linkable;
						dirty = true;
					}

					if (dirty)
					{
						address_asset.Sync();
					}
				}
			}

			[Shitcode]
			[ISystem.Add(ISystem.Mode.Single, ISystem.Scope.Region, Defer = true), HasTag("initialized", true, Source.Modifier.Owned)]
			public static void OnAdd(ISystem.Info info, ref Region.Data region, Entity ent_linkable,
			[Source.Owned] ref Entrance.Linkable.Data linkable, [Source.Owned] in Transform.Data transform)
			{
				//App.WriteLine($"Entrance.Linkable.OnAdd: {linkable.h_entrance}; {ent_linkable}", App.Color.Magenta);

				if (linkable.h_entrance == 0)
				{
					var (ent_entrance_nearest, h_entrance_nearest) = Entrance.GetNearest(ref region, transform.position, out var dist_sq);
					if (ent_entrance_nearest.IsAlive())
					{
						linkable.ent_entrance = ent_entrance_nearest;
						linkable.h_entrance = h_entrance_nearest;
						linkable.Modified(ent_linkable, true);
					}
				}
			}

			[ISystem.Modified.Component<Faction.Data>(ISystem.Mode.Single, ISystem.Scope.Region)]
			public static void OnModifiedFaction(ref Region.Data.Common region_common, Entity ent_faction, Entity ent_entrance,
			[Source.Shared] ref Faction.Data faction, [Source.Owned] ref Entrance.Data entrance)
			{
				//App.WriteLine("OnModifiedFaction", color: App.Color.Magenta);

				var ent_entrance_g = ent_entrance.AsGlobalEntity();
				ent_entrance_g.SetFaction(faction.id);
			}
#endif

			[Shitcode]
			[ISystem.Monitor(ISystem.Mode.Single, ISystem.Scope.Region, Defer = false)]
			public static void OnAddRemEntrance(ISystem.Info info, ref Region.Data region, Entity ent_linkable, Entity ent_entrance,
			[Source.Shared] ref Entrance.Linkable.Data linkable, [Source.Owned] in Entrance.Data entrance)
			{
				//App.WriteLine($"Entrance.Linkable.OnAddRemEntrance: {linkable.h_entrance}; {ent_entrance}; {ent_linkable}", App.Color.Magenta);

				//switch (info.EventType)
				//{

				//	case ISystem.EventType.Add:
				//	{

				//	}
				//	break;
				//}

				linkable.ent_entrance = ent_entrance;
				linkable.h_entrance = ent_entrance.GetAssetHandle<IEntrance.Handle>();

				//ent_entrance.TryGetAssetHandle(out linkable.h_entrance);

#if SERVER
				linkable.Modified(ent_linkable, true);
#endif
			}

			//[ISystem.Modified<Address.Data>(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global, order: 100, Defer = true), HasTag("initialized", true, Source.Modifier.Owned)]
			//public static void OnModifiedAddress(ISystem.Info.Common info, ref Region.Data.Common region_common, Entity entity,
			//[Source.Owned] ref Entrance.Linkable.Data linkable,
			//[Source.Owned] ref Address.Data address)
			//{
			//	if (address.h_address.id == 0)
			//	{
			//		var address_data = new IAddress.Data();

			//		//ref var region = ref region_common.AsRegion();
			//		//if (region.IsNotNull())
			//		//{
			//		//	address_data.h_location = region.GetLocationHandle();
			//		//}
			//		//else
			//		//{
			//		//	entity.TryGetAssetHandle(out address_data.h_location);
			//		//}

			//		//ref var location_data = ref address_data.h_location.GetData();
			//		//if (location_data.IsNotNull())
			//		//{
			//		//	address_data.h_prefecture = location_data.h_prefecture;
			//		//}

			//		//stockpile_data.name = entity.GetName();

			//		var identifier = Asset.GenerateRandomIdentifier();
			//		var address_asset = IAddress.Database.RegisterOrUpdate(identifier, null, Asset.Scope.World, 0, ref address_data, flags: Asset.Flags.Recycle);
			//		//address_asset.Sync(true);

			//		address.h_address = address_asset;
			//		address.Modified(entity, true);

			//		App.WriteLine($"new address {address.h_address.id}", App.Color.Magenta);
			//	}
			//}

			//			[ISystem.Monitor(ISystem.Mode.Single, ISystem.Scope.Region, Defer = false)] //, HasTag("initialized", true, Source.Modifier.Owned)]
			//			public static void OnAddRemParent(ISystem.Info info, ref Region.Data region, Entity ent_linkable, Entity ent_entrance,
			//			[Source.Parent] ref Entrance.Linkable.Data linkable,
			//			[Source.Owned] in Entrance.Data entrance)
			//			{
			//				App.WriteLine($"Entrance.Linkable.OnAddRemParent: {linkable.h_entrance}; {ent_entrance}; {ent_linkable}", App.Color.Magenta);

			//				linkable.ent_entrance = ent_entrance;
			//				ent_entrance.TryGetAssetHandle(out linkable.h_entrance);

			//#if SERVER
			//				linkable.Modified(ent_linkable, true);
			//#endif
			//			}

			public struct LinkRPC: Net.IRPC<Entrance.Linkable.Data>
			{
				public IEntrance.Handle h_entrance;

#if SERVER
				public void Invoke(Net.IRPC.Context rpc, ref Entrance.Linkable.Data data)
				{
					data.h_entrance = this.h_entrance;
					data.ent_entrance = data.h_entrance.GetEntity(rpc.entity.GetRegionID());
					data.Modified(rpc.entity, true);
				}
#endif
			}

#if CLIENT
			public partial struct LinkableGUI: IGUICommand
			{
				public Entity ent_linkable;
				public Linkable.Data linkable;
				public Transform.Data transform;

				public void Draw()
				{
					using (var window = GUI.Window.InteractionMisc("Linkable"u8, this.ent_linkable, size: new(48, 48)))
					{
						this.StoreCurrentWindowTypeID(-10);
						if (window.show)
						{
							ref var region = ref this.ent_linkable.GetRegion();

							var ent_entrance_nearest = default(Entity);
							var h_entrance_nearest = default(IEntrance.Handle);
							var pressed = false;

							var is_entrance_alive = this.linkable.ent_entrance.IsAlive();
							if (GUI.DrawIconButton("button.entrance"u8, sprite: new Sprite(GUI.tex_icons_widget, 16, 16, 7, 2), size: GUI.Rm, enabled: !is_entrance_alive, color: is_entrance_alive ? GUI.col_button : GUI.col_add))
							{
								pressed = true;
							}
							GUI.DrawHoverTooltip("Link to nearest region entrance."u8);
							if (GUI.IsItemHovered())
							{
								(ent_entrance_nearest, h_entrance_nearest) = Entrance.GetNearest(ref region, this.transform.position, out var dist_sq);
							}

							if (ent_entrance_nearest.IsValid())
							{
								GUI.DrawEntityMarker(ent_entrance_nearest);
								if (pressed)
								{
									var rpc = new Entrance.Linkable.LinkRPC();
									rpc.h_entrance = h_entrance_nearest;
									rpc.Send(this.ent_linkable);
								}
							}
						}
					}
				}
			}

			[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region)]
			public static void OnGUI(Entity ent_linkable,
			[Source.Owned] in Transform.Data transform,
			[Source.Owned] in Entrance.Linkable.Data linkable,
			[Source.Owned] in Interactable.Data interactable)
			{
				if (interactable.IsActive())
				{
					var gui = new LinkableGUI()
					{
						ent_linkable = ent_linkable,
						linkable = linkable,
						transform = transform,
					};
					gui.Submit();
				}
			}
#endif
		}

		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Global | IComponent.Scope.Region, flags: IComponent.Flags.Remove_On_Split)]
		public struct Data: IComponent, ICharacterStorage
		{
			//public IEntrance.Handle h_entrance;
			public Entrance.Flags flags;
			public ushort unused_00;
			//public float radius = 32.00f;

			public FixedArray16<ICharacter.Handle> characters;

			[UnscopedRef]
			Span<ICharacter.Handle> ICharacterStorage.GetCharacterSpan() => this.GetCharacterSpan();
		}

		public static Span<ICharacter.Handle> GetCharacterSpan(ref this Entrance.Data entrance)
		{
			return entrance.characters.AsSpan();
		}

		[ISystem.Monitor(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global)]
		public static void OnAddRemCharacter(ISystem.Info.Common info, ref Region.Data.Common region_common,
		Entity entity, Entity ent_entrance, Entity ent_character,
		[Source.Stored] ref Entrance.Data entrance, [Source.Owned] ref Character.Data character)
		{
			var h_character = ent_character.GetAssetHandle<ICharacter.Handle>();
			//App.WriteLine($"OnAddRemCharacter {entity}; {ent_entrance}; {ent_character}", color: App.Color.Magenta);

			//			if (h_character.IsValid())
			//			{
			//#if SERVER
			//				switch (info.EventType)
			//				{
			//					case ISystem.EventType.Add:
			//					{
			//						entrance.characters.AsSpan().AddUnique(h_character);
			//					}
			//					break;

			//					case ISystem.EventType.Remove:
			//					{
			//						entrance.characters.AsSpan().TryRemove(h_character);
			//					}
			//					break;
			//				}

			//				entrance.Sync(ent_entrance);
			//#endif
			//			}
		}

		//		[ISystem.Modified.Component<Entrance.Data>(ISystem.Mode.Single, ISystem.Scope.Region, Defer = false), HasTag("initialized", true, Source.Modifier.Owned)]
		//		public static void OnModifiedEntrance(ISystem.Info info, ref Region.Data region, Entity ent_linkable, Entity ent_entrance,
		//		[Source.Parent] ref Entrance.Linkable.Data linkable,
		//		[Source.Owned] in Entrance.Data entrance)
		//		{
		//			App.WriteLine($"Entrance.OnModifiedEntrance: {linkable.h_entrance}; {ent_entrance}; {ent_linkable}", App.Color.Magenta);

		//			linkable.ent_entrance = ent_entrance;
		//			ent_entrance.TryGetAssetHandle(out linkable.h_entrance);

		//#if SERVER
		//			linkable.Modified(ent_linkable, true);
		//#endif
		//		}


#if SERVER
		[Shitcode]
		[ISystem.Modified.Component<Address.Data>(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global, order: 100, Defer = true), HasTag("initialized", true, Source.Modifier.Owned)]
		public static void OnModifiedAddress(ISystem.Info.Common info, ref Region.Data.Common region,
		Entity ent_entrance, Entity ent_address, Entity ent_location,
		[Source.Owned] ref Entrance.Data entrance, [Source.Owned] ref Address.Data address, [Source.Any] ref Location.Data location)
		{
			//App.WriteLine($"Entrance.OnModifiedAddress: {address.h_address}; {ent_location}; {ent_address}", App.Color.Magenta);

			if (address.h_address.TryGetDefinition(out var address_asset))
			{
				var dirty = false;

				if (address_asset.data.h_entrance == 0)
				{
					dirty |= ent_entrance.TryGetAssetHandle(out address_asset.data.h_entrance);
				}

				if (address_asset.data.h_location == 0)
				{
					dirty |= ent_location.TryGetAssetHandle(out address_asset.data.h_location);
				}

				if (address_asset.data.entity != ent_address)
				{
					address_asset.data.entity = ent_address;
					dirty = true;
				}

				if (dirty)
				{
					address_asset.Sync();
				}
			}
		}
#endif

		[IEvent.Data]
		public partial struct EnterEvent(): IEvent
		{
			[Flags]
			public enum Flags: ushort
			{
				None = 0,

				Spawn = 1 << 0
			}

			public Entity ent_entrance_global;

			public ICharacter.Handle h_character;
			public IPlayer.Handle h_player;
			public IFaction.Handle h_faction;
			public EnterEvent.Flags flags;
		}

		[IEvent.Data]
		public partial struct ExportEvent(): IEvent
		{
			[Flags]
			public enum Flags: ushort
			{
				None = 0,
			}

			public Entity ent_root;
			public FixedArray4<Entity> ents_include;

			public ICharacter.Handle h_character;
			public IPlayer.Handle h_player;
			public IFaction.Handle h_faction;
			public ExportEvent.Flags flags;
		}

//		[ISystem.Event<Entrance.ExportEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
//		public static void OnExportEvent_Test1(ref Region.Data region, ISystem.Info info, Entity ent_entity,
//		[Source.Cascade] ref Entrance.ExportEvent ev, [Source.Owned] ref Entity.Data entity)
//		{
//#if SERVER
//			//ref var claw = ref ent_entity.GetComponent<Claw.Data>();
//			//if (claw.IsNotNull())
//			//{
//			//	var ent_joint = ent_entity.GetChild(Relation.Type.Instance);
//			//	if (ent_joint.IsAlive())
//			//	{
//			//		App.WriteValue(ent_joint.GetFullName());

//			//		var ent_attached = ent_joint.GetChild(Relation.Type.Child);
//			//		if (ent_attached.IsAlive())
//			//		{
//			//			App.WriteValue(ent_attached.GetFullName());
//			//			ent_attached.RemoveRelation(ent_joint, Relation.Type.Wildcard);
//			//		}
//			//	}
//			//}
//#endif
//			App.WriteValue(nameof(OnExportEvent_Test1), (ent_entity.GetName(), ent_entity.id, ev.ent_root, info.DeltaTime), color: App.Color.Magenta);
//		}

//		[ISystem.Event<Entrance.ExportEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
//		public static void OnExportEvent_Claw(Entity ent_grabber,
//		[Source.Cascade] ref Entrance.ExportEvent ev, [Source.Owned] ref Grabber.Data grabber, [Source.Owned] ref Joint.Base joint_base)
//		{
//#if SERVER
//			//ref var claw = ref ent_entity.GetComponent<Claw.Data>();
//			//if (claw.IsNotNull())
//			//{
//			//	var ent_joint = ent_entity.GetChild(Relation.Type.Instance);
//			//	if (ent_joint.IsAlive())
//			//	{
//			//		App.WriteValue(ent_joint.GetFullName());

//			//		var ent_attached = ent_joint.GetChild(Relation.Type.Child);
//			//		if (ent_attached.IsAlive())
//			//		{
//			//			App.WriteValue(ent_attached.GetFullName());
//			//			ent_attached.RemoveRelation(ent_joint, Relation.Type.Wildcard);
//			//		}
//			//	}
//			//}
//#endif
//			App.WriteValue(nameof(OnExportEvent_Claw), (ent_grabber.GetName(), ent_grabber.id, ev.ent_root, joint_base.ent_attached), color: App.Color.Magenta);
//		}

//		[ISystem.Event<Entrance.ExportEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
//		public static void OnExportEvent_Claw2(Entity ent_claw,
//		[Source.Cascade] ref Entrance.ExportEvent ev, [Source.Shared] ref Claw.Data claw, [Source.Owned] ref Joint.Base joint_base)
//		{
//#if SERVER
//			//ref var claw = ref ent_entity.GetComponent<Claw.Data>();
//			//if (claw.IsNotNull())
//			//{
//			//	var ent_joint = ent_entity.GetChild(Relation.Type.Instance);
//			//	if (ent_joint.IsAlive())
//			//	{
//			//		App.WriteValue(ent_joint.GetFullName());

//			//		var ent_attached = ent_joint.GetChild(Relation.Type.Child);
//			//		if (ent_attached.IsAlive())
//			//		{
//			//			App.WriteValue(ent_attached.GetFullName());
//			//			ent_attached.RemoveRelation(ent_joint, Relation.Type.Wildcard);
//			//		}
//			//	}
//			//}
//#endif
//			App.WriteValue(nameof(OnExportEvent_Claw2), (ent_claw.GetName(), ent_claw.id, ev.ent_root, joint_base.ent_attached), color: App.Color.Magenta);
//		}

		// TODO: this affects all Claw components in the hierarchy - so if you're holding a tractor that has a claw with tongs, the tractor's claw also drops whatever it's holding
		[ISystem.Event<Entrance.ExportEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnExportEvent_Claw3(Entity ent_claw, Entity ent_joint_base,
		[Source.SharedCascade] ref Entrance.ExportEvent ev, [Source.Shared] ref Claw.Data claw, [Source.Owned] ref Joint.Base joint_base)
		{
#if SERVER
			if (joint_base.ent_attached.IsAlive())
			{
				joint_base.ent_attached.RemoveRelation(ent_joint_base, Relation.Type.Child);
			}

			//ref var claw = ref ent_entity.GetComponent<Claw.Data>();
			//if (claw.IsNotNull())
			//{
			//	var ent_joint = ent_entity.GetChild(Relation.Type.Instance);
			//	if (ent_joint.IsAlive())
			//	{
			//		App.WriteValue(ent_joint.GetFullName());

			//		var ent_attached = ent_joint.GetChild(Relation.Type.Child);
			//		if (ent_attached.IsAlive())
			//		{
			//			App.WriteValue(ent_attached.GetFullName());
			//			ent_attached.RemoveRelation(ent_joint, Relation.Type.Wildcard);
			//		}
			//	}
			//}
#endif
			//App.WriteValue(nameof(OnExportEvent_Claw3), (ent_claw.GetName(), ent_claw.id, ev.ent_root, joint_base.ent_attached.GetName()), color: App.Color.Magenta);
		}

		[ISystem.Event<Entrance.ExportEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnExportEvent_Equipment1(ISystem.Info info, Entity ent_equip, Entity ent_equipment, IComponent.Handle h_equip,
		[Source.Owned] ref Entrance.ExportEvent ev, [Source.Owned] ref Body.Data body, [Source.Owned, Pair.Wildcard] ref Equip.Data equip, [Source.Parent<Equip.Data>] ref Equipment.Data equipment)
		{
			var ent_root = body.GetRoot(); // ent_equip;
			var ent_parent = body.GetParent();
			var ok = ent_equip == ev.ent_root && ent_equipment.IsAlive();
			if (ok)
			{
				ok &= ev.ents_include.AsSpan().TryAddUnique(ent_equipment);
			}

			//App.WriteValue(nameof(OnExportEvent_Equipment1), (h_equip.ToString(), ent_root, ent_equip, ent_parent, ent_equipment.GetName()), color: ok ? App.Color.Green : App.Color.Red, show_stacktrace: false);
		}

		[ISystem.Event<Entrance.ExportEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnExportEvent_Equipment2(ISystem.Info info, Entity ent_equip, Entity ent_equipment, IComponent.Handle h_equip,
		[Source.Parent] ref Entrance.ExportEvent ev, [Source.Owned] ref Body.Data body, [Source.Owned, Pair.Wildcard] ref Equip.Data equip, [Source.Parent<Equip.Data>] ref Equipment.Data equipment)
		{
			var ent_root = body.GetRoot(); // info.GetEventSource();
			var ent_parent = body.GetParent();
			var ok = ent_parent == ev.ent_root && ent_equipment.IsAlive();
			if (ok)
			{
				ok &= ev.ents_include.AsSpan().TryAddUnique(ent_equipment);
			}

			//App.WriteValue(nameof(OnExportEvent_Equipment2), (h_equip.ToString(), ent_root, ent_equip, ent_parent, ent_equipment.GetName()), color: ok ? App.Color.Green : App.Color.Red, show_stacktrace: false);
		}

		//[ISystem.Event<Entrance.ExportEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
		//public static void OnExportEvent_Equipment3(ISystem.Info info, Entity ent_equip, Entity ent_equipment, IComponent.Handle h_equip,
		//[Source.All] ref Entrance.ExportEvent ev, [Source.Owned] ref Body.Data body, [Source.Owned, Pair.Wildcard] ref Equip.Data equip, [Source.Parent<Equip.Data>] ref Equipment.Data equipment)
		//{
		//	var ent_root = body.GetRoot(); // info.GetEventSource();
		//	var ent_parent = body.GetParent();
		//	//App.WriteValue(nameof(OnExportEvent_Equipment3), (h_equip.ToString(), ent_root, ent_equip, ent_parent), color: ent_root == ev.ent_root ? App.Color.Green : App.Color.Red, show_stacktrace: false);
		//}

		//[ISystem.Event<Entrance.ExportEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
		//public static void OnExportEvent_Equipment3(ISystem.Info info, Entity ent_equip, Entity ent_equipment,
		//[Source.Test] ref Entrance.ExportEvent ev, [Source.Any, Pair.Wildcard] ref Equip.Data equip)
		//{
		//	App.WriteValue(nameof(OnExportEvent_Equipment3), (info.Count, info.InstanceCount, info.FrameOffset, ent_equip.GetName()), color: App.Color.Magenta, show_stacktrace: false);
		//}

		//[ISystem.Event<Entrance.ExportEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
		//public static void OnExportEvent_Equipment4(ISystem.Info info, Entity ent_equip, Entity ent_equipment,
		//[Source.Any] ref Entrance.ExportEvent ev, [Source.Any, Pair.Wildcard] ref Equip.Data equip, [Source.Parent<Equip.Data>] ref Equipment.Data equipment)
		//{
		//	App.WriteValue(nameof(OnExportEvent_Equipment4), (info.Count, info.InstanceCount, info.FrameOffset, ent_equip.GetName()), color: App.Color.Magenta, show_stacktrace: false);
		//}

		//[ISystem.Event<Entrance.ExportEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
		//public static void OnExportEvent_Test2(ref Region.Data region, ISystem.Info info, Entity ent_entity,
		//[Source.Test] ref Entrance.ExportEvent ev, [Source.Owned] ref Entity.Data entity)
		//{
		//	App.WriteValue(nameof(OnExportEvent_Test2), (ent_entity.GetName(), ent_entity.id, ev.ent_root, info.InstanceCount), color: App.Color.Magenta);
		//}

		//[ISystem.Event<Entrance.ExportEvent>(ISystem.Mode.Single, ISystem.Scope.Region)]
		//public static void OnExportEvent_Test3(ref Region.Data region, ISystem.Info info, Entity ent_entity,
		//[Source.Any] ref Entrance.ExportEvent ev, [Source.Owned] ref Entity.Data entity)
		//{
		//	App.WriteValue(nameof(OnExportEvent_Test3), (ent_entity.GetName(), ent_entity.id, ev.ent_root, info.DeltaTime), color: App.Color.Magenta);
		//}

		public struct SpawnRPC: Net.IRPC<Entrance.Data>
		{
			public ICharacter.Handle h_character;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Entrance.Data data)
			{
				App.WriteLine("spawn rpc");

				ref var region = ref rpc.entity.GetRegion();
				Assert.IsNotNull(ref region);

				ref var player_data = ref rpc.connection.GetPlayer(out var player_asset);
				Assert.IsNotNull(ref player_data);

				ref var character_data = ref this.h_character.GetData(out var character_asset);
				Assert.IsNotNull(ref character_data);
				if (character_data.players.IsNotNullOrEmpty()) Assert.Check(character_data.players.Contains(player_asset));
				else Assert.Check(character_data.faction == player_data.h_faction);

				//Assert.Check((character_data.players.IsNullOrEmpty()) || (character_data.players?.Contains(player_asset) ?? false));

				ref var entrance_data = ref rpc.entity.GetAssetData(out IEntrance.Definition entrance_asset);
				Assert.IsNotNull(ref entrance_data);

				var ent_entrance_g = entrance_asset.GetGlobalEntity();
				Assert.Check(ent_entrance_g.IsAlive());

				var ent_location_g = ent_entrance_g.GetParent(Relation.Type.Child);
				Assert.Check(ent_location_g.IsAlive());

				var region_id_location = entrance_asset.RegionID;

				var ent_character_g = this.h_character.GetGlobalEntity();
				var ent_character_r = ent_character_g.WithRegionID(region_id_location);

				ref var transform = ref rpc.entity.GetComponent<Transform.Data>();
				Assert.IsNotNull(ref transform);

				ref var data_g = ref ent_entrance_g.GetComponent<Entrance.Data>();
				Assert.IsNotNull(ref data_g);

				var characters_span = data_g.GetCharacterSpan();
				if (Assert.Check(characters_span.TryRemove(this.h_character)))
				{
					//var control = true;
					var h_character_current = rpc.connection.GetCharacterHandle();

					//ent_character_g.RemoveRelation(ent_entrance_g, Relation.Type.Child);
					//ent_character_g.AddRelation(ent_location_g, Relation.Type.Stored, false);
					ent_character_g.ReplaceRelation(ent_location_g, Relation.Type.Stored);

					// TODO: thread safety
					var resources_copy = default(FixedArray8<Resource.Data>); //.Default;
					{
						ref var storage = ref ent_character_g.GetComponent<Storage.Data>();
						if (storage.IsNotNull())
						{
							if (Assert.Check(storage.inv_storage.TryGetHandle(out var inventory), level: Assert.Level.Warn))
							{
								var inventory_span = inventory.GetSpan();
								if (Assert.Check(inventory_span.TryCopyTo(resources_copy), level: Assert.Level.Warn))
								{
									inventory_span.Clear();
									inventory.MarkDirty();
								}
								else
								{

								}
							}
						}
					}

					data_g.Sync(ent_entrance_g);

					Spawner.SpawnCharacter(region: ref region,
					h_character: this.h_character,
					position: transform.position,
					h_faction: character_data.faction,
					h_player: player_asset,
					flags: Spawn.SpawnEvent.Flags.Control /*&& (h_character_current == 0 || h_character_current == this.h_character)*/).ContinueWith((ent_controlled) =>
					{
						App.WriteLine("spawned");

						if (Assert.Check(character_asset is not null, level: Assert.Level.Warn))
						{
							Loadout.Spawn(ent_controlled, character_asset.data.kits, money: character_asset.data.money);
							character_asset.data.kits = null;
							character_asset.data.money = 0.00f;
						}

						var resources_tmp = resources_copy;
						if (!resources_tmp.IsAllZeros())
						{
							ref var storage = ref ent_controlled.GetComponent<Storage.Data>();
							if (storage.IsNotNull())
							{
								if (Assert.Check(storage.inv_storage.TryGetHandle(out var inventory), level: Assert.Level.Warn))
								{
									var inventory_span = inventory.GetSpan();

									if (Assert.Check(resources_tmp.AsSpan().TryCopyTo(inventory_span), level: Assert.Level.Warn))
									{
										inventory.MarkDirty();
									}
									else
									{

									}

									//var inventory_span = inventory.GetSpan();
									//foreach (ref var resource in resources_tmp)
									//{
									//	if (resource)
									//	{
									//		if (Assert.Check(inventory.Deposit(ref resource), level: Assert.Level.Warn))
									//		{

									//		}
									//	}
									//}

									//inventory.MarkDirty();
								}
							}
						}
					});
				}
			}
#endif
		}

		public struct EnterRPC: Net.IRPC<Entrance.Data>
		{
			public ICharacter.Handle h_character;
			public EnterEvent.Flags flags;

			public Entity ent_spawn;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Entrance.Data data)
			{
				App.WriteLine("enter rpc");

				ref var region = ref rpc.entity.GetRegion();
				Assert.IsNotNull(ref region);

				ref var player_data = ref rpc.connection.GetPlayer(out var player_asset);
				Assert.IsNotNull(ref player_data);

				ref var character_data = ref this.h_character.GetData(out var character_asset);
				Assert.IsNotNull(ref character_data);
				if (character_data.players.IsNotNullOrEmpty()) Assert.Check(character_data.players.Contains(player_asset));
				else Assert.Check(character_data.faction == player_data.h_faction);

				ref var entrance_data = ref rpc.entity.GetAssetData(out IEntrance.Definition entrance_asset);
				Assert.IsNotNull(ref entrance_data);

				var ent_entrance_g = entrance_asset.GetGlobalEntity();
				Assert.Check(ent_entrance_g.IsAlive());

				var ent_location_g = ent_entrance_g.GetParent(Relation.Type.Child);
				Assert.Check(ent_location_g.IsAlive());

				var region_id_location = entrance_asset.RegionID;

				var ent_character_g = this.h_character.GetGlobalEntity();
				var ent_character_r = ent_character_g.WithRegionID(region_id_location);

				ref var data_g = ref ent_entrance_g.GetComponent<Entrance.Data>();
				Assert.IsNotNull(ref data_g);

				ref var character = ref region.GetCharacter(h_character);
				Assert.IsNotNull(ref character);

				var ent_controlled = character.ent_controlled;
				Assert.Check(ent_controlled.IsAlive());

				var characters_span = data_g.GetCharacterSpan();
				if (Assert.Check(characters_span.TryAddUnique(this.h_character)))
				{
					//Entrance.Enter<Entrance.Data>(rpc.entity, this.h_character);
					//ent_character.AddRelation(ent_tmp, Relation.Type.Stored, check_recursion: false);

					//var ts = Timestamp.Now();
					var ev = new Entrance.ExportEvent();
					ev.ent_root = ent_controlled;
					ev.Trigger(ent_controlled);

					//App.WriteLine(ev.ents_include);

					//var ts_elapsed = ts.GetMilliseconds();
					//App.WriteLine($"ev: {ts_elapsed:0.0000} ms");

					var ent_pickup_target = character.ent_pickup_target;
					if (ent_pickup_target.IsAlive())
					{
						App.WriteLine(ent_pickup_target);

						ref var holdable = ref ent_pickup_target.GetComponent<Holdable.Data>();
						if (holdable.IsNotNull())
						{
							if (holdable.flags.HasNone(Holdable.Flags.Storable))
							{
								ent_pickup_target.RemoveRelation(character.ent_pickup, Relation.Type.Child);
								App.WriteLine(ent_pickup_target.GetFullName(), color: App.Color.Green);
								//Arm.Drop(character.ent_pickup);
							}
						}
					}

					character.state_flags.AddFlag(Character.StateFlags.Despawning | Character.StateFlags.Keep_Alive);
					//character.state_flags.AddFlag(Character.StateFlags.Keep_Alive);
					//character.state_flags.AddFlag(Character.StateFlags.Keep_Alive, data.flags.HasAny(Personnel.Data.Flags.Store_Characters));

					var resources_copy = default(FixedArray8<Resource.Data>); //.Default;

					var inventory = character.GetInventory();
					if (inventory.IsValid())
					{
						var inventory_span = inventory.GetSpan();
						if (Assert.Check(inventory_span.TryCopyTo(resources_copy), level: Assert.Level.Warn))
						{
							inventory_span.Clear();
							inventory.MarkDirty();
							// TODO
						}
						else
						{

						}
					}

					if (true) //data.flags.HasAny(Personnel.Data.Flags.Store_Characters))
					{
						character.ent_controlled.SetAssociatedCharacter(ICharacter.Handle.None); //.AddRelation(rpc.entity, Relation.Type.Stored, check_recursion: true);
																								 //ent_character.AddRelation(rpc.entity, Relation.Type.Stored, check_recursion: true);
					}
					else
					{
						ref var player = ref character.h_player.GetPlayer(ref region);
						if (player.IsNotNull())
						{
							player.SetControlledCharacter(ICharacter.Handle.None);
						}
					}

					character_asset.flags.RemoveFlag(Asset.Flags.Recycle);
					character_asset.Promote();

					//var ent_tmp = rec_enterable.entity;

					var ents_include = ev.ents_include;

					// TODO: dumb
					region.Schedule((ref region) =>
					{
						//App.WriteLine("exported");
						ent_character_r.ReplaceRelation(ent_entrance_g.WithRegionID(region.GetID()), Relation.Type.Stored);

						var ents_include_tmp = ents_include;
						var character_prefab = ent_controlled.ExportPrefab(character_asset.identifier, despawn: true, ents_include: ents_include_tmp.AsSpan());
						App.WriteLine($"exported to {character_prefab}");

						region.Schedule((ref region) =>
						{
							//var character_prefab = ent_controlled.ExportPrefab(character_asset.identifier, despawn: true);
							//App.WriteLine($"exported to {character_prefab}");

							ref var character = ref region.GetCharacter(character_asset.GetHandle(), out var ent_character);
							if (Assert.IsNotNull(ref character, Assert.Level.Warn))
							{
								character.state_flags.RemoveFlag(Character.StateFlags.Despawning | Character.StateFlags.Keep_Alive);
								character.Sync(ent_character, true);
							}

							character_asset.data.prefab = character_prefab;
							character_asset.Sync();

							//region.Schedule((ref region) =>
							//{
							//App.WriteLine("exported");
							//ent_character.AddRelation(ent_entrance_g.WithRegionID(region.GetID()), Relation.Type.Stored, check_recursion: false);
							//});
						});
					});

					character_asset.GetOrSpawnEntity(0).ContinueWith((ent_character) =>
					{
						ent_character.ReplaceRelation(ent_entrance_g, Relation.Type.Stored);

						ref var storage = ref ent_character.GetComponent<Storage.Data>();
						if (storage.IsNotNull())
						{
							if (storage.inv_storage.TryGetHandle(out var inventory))
							{
								var resources_tmp = resources_copy;
								var inventory_span = inventory.GetSpan();

								if (Assert.Check(resources_tmp.AsSpan().TryCopyTo(inventory_span), level: Assert.Level.Warn))
								{
									inventory.MarkDirty();
								}
								else
								{

								}
							}
						}

						return ent_character;
					});

					data_g.Sync(ent_entrance_g);
				}

				//if (rpc.entity.region_id != 0)
				//{
				//	Entrance.Enter<Entrance.Data>(rpc.entity, this.h_character);
				//}
				//else
				//{
				//	var ent_character_global = this.h_character.GetGlobalEntity();
				//	Assert.Check(ent_character_global.IsAlive());

				//	// TODO: add a check if unit is parented to entrance's global entity
				//	var ent_entrance_global = rpc.entity.AsGlobalEntity();
				//	Assert.Check(ent_entrance_global.IsAlive());

				//	var ent_location_global = ent_entrance_global.GetParent(Relation.Type.Child);
				//	Assert.Check(ent_location_global.IsAlive());

				//	ent_character_global.AddRelation(ent_location_global, Relation.Type.Child, false);

				//	this.flags.AddFlag(EnterEvent.Flags.Spawn, data.flags.HasAny(Entrance.Flags.Force_Spawn));

				//	var ev = new Entrance.EnterEvent()
				//	{
				//		ent_entrance_global = ent_entrance_global,
				//		h_character = this.h_character,
				//		h_faction = rpc.connection.GetFactionHandle(),
				//		h_player = player_asset,
				//		flags = this.flags
				//	};
				//	rpc.entity.TriggerEventDeferred(ev, true);
				//}
			}
#endif
		}

		public struct MoveRPC: Net.IRPC<Entrance.Data>
		{
			public enum Action: byte
			{
				Undefined = 0,

				Move_To_Region,
				Move_To_World,
			}

			public required ICharacter.Handle h_character;
			public required MoveRPC.Action action;
			//public Entity ent_character_global;

			public (Entity ent_character, bool ok) results;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Entrance.Data data)
			{
				ref var region_common = ref rpc.entity.GetRegionCommon();
				Assert.Check(region_common.IsGlobal());

				ref var character_data = ref this.h_character.GetData(out var character_asset);
				Assert.IsNotNull(ref character_data);

				ref var entrance_data = ref rpc.entity.GetAssetData(out IEntrance.Definition entrance_asset);
				Assert.IsNotNull(ref entrance_data);

				var ent_entrance_g = entrance_asset.GetGlobalEntity();
				Assert.Check(ent_entrance_g.IsAlive());

				var ent_location_g = ent_entrance_g.GetParent(Relation.Type.Child);
				Assert.Check(ent_location_g.IsAlive());

				var region_id_location = entrance_asset.RegionID;

				var ent_character_g = character_asset.GetGlobalEntity();
				var ent_character_r = ent_character_g.WithRegionID(region_id_location);

				var characters_span = data.GetCharacterSpan();

				var sync = false;

				App.WriteLine($"{character_asset.RegionID}; {this.action}");

				switch (this.action)
				{
					case MoveRPC.Action.Move_To_Region:
					{
						if (Assert.Check(characters_span.TryAddUnique(this.h_character)))
						{
							character_asset.RegionID = region_id_location;
							//character_asset.Promote();
							character_asset.Sync();

							//ent_character_g.RemoveRelation(ent_entrance_g, Relation.Type.Child);
							//ent_character_g.ReplaceRelation(ent_location_g, Relation.Type.Stored);

							//var h_player = rpc.GetSenderPlayerHandle();
							character_asset.GetOrSpawnEntity(region_id_location).ContinueWith((ent_character) =>
							{
								ent_character.AddRelation(ent_entrance_g.WithRegionID(ent_character.region_id), Relation.Type.Stored);

								//var ent_player = h_player.GetRegionEntity(ent_character.region_id);
								//ent_character.SetPlayer(ent_player: ent_player);

								return ent_character;
							});

							this.results.ent_character = ent_character_r;
							this.results.ok = true;
							sync = true;
						}
					}
					break;

					case MoveRPC.Action.Move_To_World:
					{
						if (Assert.Check(characters_span.TryRemoveSwapback(this.h_character)))
						{
							ent_character_r.Delete();

							character_asset.RegionID = 0;
							character_asset.Promote();
							character_asset.Sync();

							character_asset.GetOrSpawnEntity(0).ContinueWith((ent_character) =>
							{
								//ent_character.RemoveRelation(Entity.Wildcard, Relation.Type.Stored);

								ent_character.ReplaceRelation(ent_entrance_g, Relation.Type.Stored);
								//ent_character.AddRelation(ent_entrance_g, Relation.Type.Child);

								return ent_character;
							});

							this.results.ent_character = ent_character_g;
							this.results.ok = true;
							sync = true;
						}
					}
					break;
				}

				if (sync)
				{
					rpc.Sync(ref data);
				}

				//ref var region = ref rpc.entity.GetRegion();
				//ref var region_global = ref Region.GetGlobalRegion();

				//ref var entrance = ref rpc.entity.GetComponentWithOwner<Entrance.Data>(Relation.Type.Instance, out var ent_entrance);
				//Assert.IsNotNull(ref entrance);

				//ref var entrance_data = ref ent_entrance.GetAssetData(out IEntrance.Definition entrance_asset);
				//Assert.IsNotNull(ref entrance_data);

				//var ent_entrance_global = entrance_asset.GetGlobalEntity();
				//Assert.Check(ent_entrance_global.IsAlive());

				//var ent_location_global = ent_entrance_global.GetParent(Relation.Type.Child);
				//Assert.Check(ent_location_global.IsAlive());

				//ref var character_data = ref this.h_character.GetData(out var character_asset);

				//ref var character = ref region.GetCharacter(this.h_character);
				//Assert.IsNotNull(ref character);

				//var ent_controlled = character.ent_controlled;
				////Assert.Check(ent_controlled.IsAlive());

				//var ent_character = this.h_character.GetEntity(ref region); // character.ent_character;

				//var ent_pickup_target = character.ent_pickup_target;
				//if (ent_pickup_target.IsAlive())
				//{
				//	ref var holdable = ref ent_pickup_target.GetComponent<Holdable.Data>();
				//	if (holdable.IsNotNull())
				//	{
				//		if (holdable.flags.HasNone(Holdable.Flags.Storable))
				//		{
				//			Arm.Drop(character.ent_pickup);
				//		}
				//	}
				//}

				//character.state_flags.AddFlag(Character.StateFlags.Despawning);
				//ref var player = ref character.h_player.GetPlayer(ref region);
				//player.SetControlledCharacter(ICharacter.Handle.None);
				////ent_controlled.SetAssociatedCharacter(default);

				//var h_character_tmp = this.h_character;
				//var entrance_flags = data.flags;

				//character_asset.flags.RemoveFlag(Asset.Flags.Recycle);
				//character_asset.prefab = "unit.guy";
				//character_asset.Promote();

				//App.WriteLine(character_asset);

				//this.ent_character_global = character_asset.GetGlobalEntity();

				//var ent_tmp = rpc.entity;
				//region.Schedule((ref Region.Data region) =>
				//{
				//	App.WriteLine(character_asset);


				//	//character_asset.flags.AddFlag(Asset.Flags.Entity);

				//	region.Schedule((ref Region.Data region) =>
				//	{
				//		if (ent_controlled.IsAlive())
				//		{
				//			var character_prefab = ent_controlled.ExportPrefab(character_asset.identifier, despawn: true);

				//			App.WriteLine(character_asset);


				//			character_asset.data.prefab = character_prefab;
				//		}
				//		else
				//		{
				//			ent_character.Delete();
				//		}

				//		ref var dormitory = ref ent_tmp.GetComponent<Dormitory.Data>();
				//		if (entrance_flags.HasAny(Entrance.Flags.Manual_Exit) && dormitory.IsNotNull())
				//		{
				//			var characters_span = dormitory.GetCharacterSpan();

				//			var index = -1;
				//			if (characters_span.TryGetIndexOfOrEmpty(h_character_tmp, ref index, out var contains))
				//			{
				//				characters_span[index] = h_character_tmp;
				//				dormitory.Sync(ent_tmp);

				//				character_asset.SpawnEntity(0).ContinueWith((ent) =>
				//				{
				//					ent.AddRelation(ent_location_global, Relation.Type.Child, false);
				//				});
				//			}
				//		}
				//		else
				//		{
				//			Region.GetGlobalRegion().Schedule((ref Region.Data.Global region) =>
				//			{
				//				App.WriteLine(character_asset);


				//				character_asset.SpawnEntity(0).ContinueWith((ent) =>
				//				{
				//					//ent.SetEnabled(true);
				//					ent.AddRelation(ent_entrance_global, Relation.Type.Child, false);

				//					App.WriteLine(character_asset);


				//					//if (inventory_src_array != null)
				//					//{
				//					//	ref var storage = ref ent.GetComponent<Storage.Data>();
				//					//	if (storage.IsNotNull() && storage.inv_storage.TryGetHandle(out var h_inventory))
				//					//	{
				//					//		var inventory_dst_span = h_inventory.GetSpan();
				//					//		inventory_dst_span.CopyFrom(inventory_src_array);
				//					//		h_inventory.MarkDirty();
				//					//	}
				//					//}
				//				});
				//			});
				//		}
				//	});
				//});

			}
#endif
		}

		//#if CLIENT
		//		public struct EntranceGUI: IGUICommand
		//		{
		//			public Entity entity;
		//			public Entity ent_entrance;

		//			public Entrance.Data entrance;
		//			public Transform.Data transform;

		//			public IFaction.Handle h_faction;

		//			public void Draw()
		//			{
		//				using (var window = GUI.Window.InteractionMisc("Entrance"u8, this.entity, size: new(0, 40), min_width: 128, min_height: 40))
		//				{
		//					this.StoreCurrentWindowTypeID(order: -100);
		//					if (window.show)
		//					{
		//						ref var player = ref Client.GetPlayer();
		//						ref var region = ref this.entity.GetRegion();

		//						if (GUI.DrawButton("Leave Region"u8, size: GUI.Rm, color: GUI.col_button_yellow))
		//						{
		//							var ent_entrance_global = this.ent_entrance.AsGlobalEntity();
		//							var rpc = new Entrance.MoveRPC()
		//							{
		//								h_character = Client.GetCharacterHandle(),
		//							};
		//							rpc.SendAsTask(this.ent_entrance).ContinueWith((x) =>
		//							{
		//								GUI.RegionMenu.ToggleWidget(true);

		//								WorldMap.hs_selected_entities.Clear();
		//								WorldMap.hs_selected_entities.Add(x.ent_character_global);
		//								//WorldMap.FocusEntity(x.ent_character_global);
		//								WorldMap.FocusEntity(ent_entrance_global, interact: true);

		//								Checkpoint.CommonCheckpointGUI.edit_ent_selected = x.ent_character_global;
		//							});

		//							//{
		//							//	var rpc = new RespawnExt.SetSpawnRPC()
		//							//	{
		//							//		ent_spawn = this.entity,
		//							//	};
		//							//	rpc.Send(Client.GetEntity());
		//							//}

		//							//GUI.RegionMenu.ToggleWidget(true);
		//							//WorldMap.FocusEntity(this.ent_entrance.AsGlobalEntity(), interact: false);
		//						}
		//						//GUI.DrawHoverTooltip($"{ent_entrance}; {Client.GetCharacterHandle()}");
		//					}
		//				}
		//			}
		//		}

		//		[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region, order: 5000)]
		//		public static void OnGUI(ISystem.Info info, ref Region.Data region, Entity entity, Entity ent_entrance,
		//		[Source.Owned] in Transform.Data transform, [Source.Owned] in Interactable.Data interactable,
		//		[Source.SharedAny] in Entrance.Data entrance, [Source.Owned, Optional] in Faction.Data faction)
		//		{
		//			return;

		//			if (interactable.IsActive())
		//			{
		//				//var ent_entrance = entity.GetComponentOwner<Entrance.Data>(Relation.Type.Instance);

		//				var gui = new EntranceGUI()
		//				{
		//					entity = entity,
		//					ent_entrance = ent_entrance,
		//					entrance = entrance,
		//					transform = transform,
		//					h_faction = faction.id
		//				};
		//				gui.Submit();
		//			}
		//		}
		//#endif

		//[ISystem.Modified<Entrance.Data>(ISystem.Mode.Single, ISystem.Scope.Global)]
		//public static void OnModified(Entity entity, [Source.Owned] ref Entrance.Data entrance, [Source.Owned] ref WorldMap.Marker.Data marker)
		//{
		//	if (entity.TryGetAsset(out IEntrance.Definition entrance_asset))
		//	{
		//		marker.relative_offset = entrance_asset.data.relative_offset;
		//		marker.icon = entrance_asset.data.icon;
		//	}
		//}
	}
}
