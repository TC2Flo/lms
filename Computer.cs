
namespace TC2.Base.Components
{
	public static partial class Module
	{
		public static partial class Slots
		{
			public enum Type: byte
			{
				Undefined = 0,

				Card,
				Motor,
				Sensor,
				Relay,
				Controller,
				Heater,
				Timer
			}

			[Flags]
			public enum Flags: byte
			{
				None = 0,

				No_Menu = 1 << 0
			}

			[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Region, sync_table_capacity: 64)]
			public partial struct Data(): IComponent
			{
				[Save.Force] public required Module.Slots.Type slot_type;
				public Module.Slots.Flags flags;
				public Signal.Channels channels_push;
				public Signal.Channels channels_pull;

				[Save.NewLine]
				[Editor.Slider.Clamped(min: 24, 24 * 32, snap: 8)]
				[Save.Force] public required Vec2u16 gui_size = new(48);
				private uint unused_01;
				private uint unused_02;

				[Save.NewLine]
				[Editor.Slider.Clamped(min: -4.00f, max: 4.00f, snap: 0.125f), Editor.Picker.Position(true)]
				[Save.Force] public required Vector2 offset;
				[Editor.Slider.Clamped(min: -200.00f, max: 200.00f, snap: 0.100f)]
				public float z = -2.00f;
				[Editor.Slider.Clamped(min: -MathF.Tau, max: +MathF.Tau, snap: MathF.Tau / 32.00f)]
				public float rotation;
			}
		}

		[IRelation.Data(Net.SendType.Reliable, IRelation.Flags.Acyclic | IRelation.Flags.DontInherit | IRelation.Flags.Traversable, IComponent.Scope.Region)]
		public partial struct Link(): IRelation
		{
			[Save.Force] public required Module.Slots.Type slot_type;

			[Save.NewLine]
			[Editor.Slider.Clamped(min: -4.00f, max: 4.00f, snap: 0.125f), Editor.Picker.Position(true)]
			[Save.Force] public required Vector2 offset;
			[Editor.Slider.Clamped(min: -200.00f, max: 200.00f, snap: 0.100f)]
			public float z = -2.00f;
			[Editor.Slider.Clamped(min: -MathF.Tau, max: +MathF.Tau, snap: MathF.Tau / 32.00f)]
			public float rotation;
		}

		[Flags]
		public enum Flags: ushort
		{
			None = 0,

			Unremovable = 1 << 0,
			Share_Interactable = 1 << 1,
		}

		[IComponent.Data(Net.SendType.Reliable, sync_table_capacity: 128, region_only: true)]
		public struct Data: IComponent
		{
			[Save.Force] public required Module.Slots.Type slot_type;
			private byte unused_00;
			public Module.Flags flags;

			[Save.NewLine]
			public byte frame_default;
			public byte frame_stored;

			private byte unused_01;
			private byte unused_02;
		}

#if CLIENT
		public static GUI.EntitySlot BeginSlot(ref GUI.ItemContext context, Utf8String identifier, Utf8String name, Entity ent_slot, Entity ent_module, Vector2 size, bool enabled = true)
		{
			var slot = GUI.EntitySlot.New(context: ref context, identifier: identifier, name: name, ent_attached: ent_module, size: size, enabled: enabled);
			{
				var skip = false;
				//if (slot.hovered)
				//{
				//	if (App.debug_mode_gui && kb.GetKey(Keyboard.Key.LeftAlt) && GUI.GetMouse().GetKeyDown(Mouse.Key.Right))
				//	{
				//		GUI.SetDebugEntity(this.ent_program);
				//		skip = true;
				//	}
				//}

				if (slot.pressed && !skip)
				{
					App.WriteLine(slot.action);

					switch (slot.action)
					{
						case GUI.ItemContext.Action.Add:
						{
							var rpc = new Module.AddRPC
							{
								ent_arm = context.ent_pickup,
								ent_module = context.ent_pickup_target
							};
							rpc.Send(ent_slot);
						}
						break;

						case GUI.ItemContext.Action.Remove:
						{
							var rpc = new Module.RemoveRPC()
							{
								ent_arm = context.ent_pickup,
								ent_module = ent_module
							};
							rpc.Send(ent_slot);
						}
						break;

						case GUI.ItemContext.Action.Swap:
						{
							var rpc = new Module.AddRPC
							{

							};
							rpc.Send(ent_slot);
						}
						break;

						case GUI.ItemContext.Action.Other:
						{
							App.WriteLine("other");
							var rpc = new Interactor.SetTargetRPC()
							{
								ent_target = ent_module
							};
							rpc.Send(context.ent_pickup);
						}
						break;
					}
				}
			}
			return slot;
		}

		public struct ModulesGUI: IGUICommand
		{
			public Entity ent_slots;
			public Entity ent_module;
			public Entity ent_interactable;

			public Module.Data module;
			public Module.Slots.Data slots;


			public void Draw()
			{
				var slot_gui_size = this.slots.gui_size.ToVec2f();
				using (var window = GUI.Window.InteractionMisc("Modules"u8, this.ent_interactable, size: slot_gui_size))
				{
					this.StoreCurrentWindowTypeID(order: -100);
					if (window.show)
					{
						var context = GUI.ItemContext.Begin();

						using (var group_slot = GUI.Group.New(slot_gui_size))
						{
							using (var slot = Module.BeginSlot(ref context, "module.slot"u8, this.slots.slot_type.ToStringUtf8(), this.ent_slots, this.ent_module, size: GUI.Rm, enabled: true))
							{

							}
						}
					}
				}
			}
		}

		[ISystem.VeryEarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnGUIInteractable(Entity ent_interactable, Entity ent_interactable_module,
		[Source.Parent<Module.Link>] ref Interactable.Data interactable_module, [Source.Parent<Module.Link>] ref Module.Data module,
		[Source.Owned] ref Interactable.Data interactable)
		{
			interactable_module.flags.AddFlag(Interactable.Flags.Show, interactable.IsActive() & module.flags.HasAny(Module.Flags.Share_Interactable));
		}

		[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnGUISlots(Entity ent_interactable, Entity ent_slots, Entity ent_module,
		[Source.Owned] ref Interactable.Data interactable,
		[Source.Parent<Module.Link>, Optional] in Module.Data module,
		[Source.Owned] ref Module.Slots.Data slots)
		{
			if (interactable.IsActive() && slots.flags.HasNone(Module.Slots.Flags.No_Menu))
			{
				var gui = new ModulesGUI
				{
					ent_slots = ent_slots,
					ent_module = ent_module,
					ent_interactable = ent_interactable,

					module = module,
					slots = slots
				};
				gui.Submit();
			}
		}
#endif

		public struct AddRPC: Net.IRPC<Module.Slots.Data>
		{
			public Entity ent_module;
			public Entity ent_arm;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Module.Slots.Data slot)
			{
				Assert.Alive(this.ent_module);

				ref var module = ref this.ent_module.GetComponent<Module.Data>();
				Assert.IsNotNull(ref module);
				Assert.IsEqual(slot.slot_type, module.slot_type);

				var valid = true;
				var ent_module_old = default(Entity);

				//for (var i = 0; i < 4; i++)
				//{
				//	var ent_module_tmp = entity.GetParent<Module.Link>(index: i);
				//	if (ent_module_tmp != 0)
				//	{
				//		ref var module_tmp_ptr = ref ent_module_tmp.GetComponent<Module.Data>();
				//		if (module_tmp_ptr != null && module_tmp_ptr->type.HasAll(module->type))
				//		{
				//			//valid = false;
				//			ent_module_old = ent_module_tmp;
				//			break;
				//		}
				//	}
				//	else
				//	{
				//		break;
				//	}
				//}

				if (valid)
				{
					var ent_target = this.ent_module;

					//var parent = this.ent_target.GetParent();
					var parent = this.ent_module.GetParent(Relation.Type.Child);
					if (parent.IsAlive())
					{
						this.ent_module.RemoveRelation(Entity.Wildcard, Relation.Type.Child);
						if (ent_module_old != 0)
						{
							rpc.entity.RemoveRel<Module.Link>(ent_module_old);
							Arm.Pickup(this.ent_arm, ent_module_old);
						}
					}
					else
					{
						//entity.AddRel<Module.Relation>(ent_target);
					}

					rpc.entity.AddRel<Module.Link>(ent_target);
				}

			}
#endif
		}

		public struct RemoveRPC: Net.IRPC<Module.Slots.Data>
		{
			public Entity ent_module;
			public Entity ent_arm;

#if SERVER
			public readonly void Invoke(Net.IRPC.Context rpc, ref Module.Slots.Data slot)
			{
				if (this.ent_module.IsAlive())
				{
					ref var region = ref rpc.entity.GetRegion();

					ref var module = ref this.ent_module.GetComponent<Module.Data>();
					if (module.IsNotNull() && !module.flags.HasAny(Module.Flags.Unremovable) && rpc.entity.HasRel<Module.Link>(this.ent_module))
					{
						var ent_module = this.ent_module;
						var ent_arm = this.ent_arm;

						rpc.entity.RemoveRel<Module.Link>(this.ent_module);
						region.Wait().ContinueWith(() =>
						{
							if (ent_module.TryGetComponent<Transform.Data>(out var h_transform_child) && ent_arm.TryGetComponent<Transform.Data>(out var h_transform_arm))
							{
								h_transform_child.item_0.SetPosition(h_transform_arm.item_0.position);
							}

							Arm.Pickup(ent_arm, ent_module);
						});

						//if (this.ent_arm.IsAlive() && !this.ent_arm.GetChild(Relation.Type.Child).IsAlive()) Arm.Pickup(this.ent_arm, this.ent_module);
					}
				}
			}
#endif
		}

		//#if SERVER
		//		public static bool TryAdd(Entity ent_module, Entity ent_slot, Module.Slots.Type slot_type)
		//		{
		//			if (ent_module.IsAlive())
		//			{
		//				var module_ptr = ent_module.GetComponentUnsafe<Module.Data>();
		//				if (module_ptr != null)
		//				{
		//					if (slot_type.HasAll(module_ptr->type))
		//					{
		//						Span<Entity> children = FixedArray.CreateSpan8<Entity>(out var buffer);
		//						ent_slot.GetChildren(ref children, Relation.Type.Child);

		//						var valid = true;
		//						foreach (var child in children)
		//						{
		//							var child_module_ptr = child.GetComponentUnsafe<Module.Data>();
		//							if (child_module_ptr != null && child_module_ptr->type.HasAll(module_ptr->type))
		//							{
		//								valid = false;
		//								break;
		//							}
		//						}

		//						if (valid)
		//						{
		//							//var parent = this.ent_target.GetParent();
		//							var parent = ent_module.GetParent(Relation.Type.Child);
		//							if (parent.IsAlive())
		//							{
		//								ent_module.RemoveRelation(parent, Relation.Type.Child);
		//							}

		//							ent_slot.AddRel<Module.Link>(ent_module);

		//							return true;
		//						}
		//					}
		//				}
		//			}

		//			return false;
		//		}
		//#endif

		//#if CLIENT
		//		public struct ModuleWidgetGUI: IGUICommand
		//		{
		//			public Entity ent_wearer;
		//			//public Entity ent_module;
		//			public Module.Slots.Data module_slot;

		//			public void Draw()
		//			{
		//				//var count = BitOperations.PopCount((uint)this.module_slot.type);
		//				//var bits = (uint)this.module_slot.type;

		//				ref var player = ref Client.GetPlayer();
		//				if (player.IsNotNull())
		//				{
		//					var is_item_alive = player.ent_pickup_target.IsAlive();

		//					var ent_module = this.ent_wearer.GetParent<Module.Link>();

		//					//for (int i = 0; i < count; i++)
		//					foreach (var type in this.module_slot.type)
		//					{
		//						var offset = BitOperations.TrailingZeroCount((uint)type);
		//						//var type = (Module.Slot.Type)(1u << offset);
		//						//bits &= ~(uint)type;

		//						var name = type.ToString();
		//						var sprite = default(Sprite);
		//						var sprite_bg = Toolbelt.WindowGUI.tex_icon_toolbelt;

		//						var is_child_alive = ent_module.IsAlive();
		//						var is_item_valid = false;

		//						var cursor = App.CursorType.Default;

		//						if (is_item_alive)
		//						{
		//							ref var module = ref player.ent_pickup_target.GetComponent<Module.Data>();
		//							if (module.IsNotNull())
		//							{
		//								is_item_valid = module.type == type;
		//							}
		//						}

		//						if (is_child_alive)
		//						{
		//							ref var renderer = ref ent_module.GetComponent<Animated.Renderer.Data>();
		//							if (renderer.IsNotNull())
		//							{
		//								sprite = renderer.sprite with { frame = new(0, 0) };
		//							}

		//							name = ent_module.GetPrefabName();

		//							if (Constants.Module.enable_unequip)
		//							{
		//								if (is_item_valid) cursor = App.CursorType.Pickup;
		//								else if (!is_item_alive) cursor = App.CursorType.Pickup;
		//							}
		//						}
		//						else
		//						{
		//							if (Constants.Module.enable_equip)
		//							{
		//								if (is_item_valid) cursor = App.CursorType.Add;
		//							}
		//						}

		//						using (var widget = Hotbar.Widget.New(name, sprite, size: new Vector2(64, 64), 5 + (4 - offset), sprite_bg: sprite_bg, has_window: false, cursor: cursor))
		//						{
		//							if (widget.added)
		//							{
		//								if (is_child_alive && !is_item_alive)
		//								{
		//									if (Constants.Module.enable_unequip)
		//									{
		//										var rpc = new Module.UnequipRPC
		//										{
		//											ent_arm = player.ent_pickup,
		//											ent_module = ent_module
		//										};
		//										rpc.Send(this.ent_wearer);
		//									}
		//								}
		//								else if (is_item_valid)
		//								{
		//									if (Constants.Module.enable_equip && (is_child_alive ? Constants.Module.enable_unequip : true))
		//									{
		//										var rpc = new Module.EquipRPC
		//										{
		//											ent_arm = player.ent_pickup,
		//											ent_target = player.ent_pickup_target
		//										};
		//										rpc.Send(this.ent_wearer);
		//									}
		//								}
		//							}
		//						}
		//					}
		//				}
		//			}
		//		}

		//		[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("local", true, Source.Modifier.Parent)]
		//		public static void OnGUI2(Entity entity, [Source.Owned] in Module.Slots.Data module_slot, [Source.Parent] in Player.Data player)
		//		{
		//			var gui = new ModuleWidgetGUI()
		//			{
		//				ent_wearer = entity,
		//				//ent_module = entity.GetChild(Relation.Type.Wearer),
		//				module_slot = module_slot
		//			};
		//			gui.Submit();
		//		}

		//		[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("local", true, Source.Modifier.Shared)]
		//		public static void OnGUI2Shared(Entity entity, [Source.Owned] in Module.Slots.Data module_slot, [Source.Shared] in Player.Data player)
		//		=> OnGUI2(entity, in module_slot, in player);
		//#endif

		//[ISystem.PreUpdate.C(ISystem.Mode.Single, ISystem.Scope.Region)]
		//public static void OnUpdateSignal([Source.Owned] in Module.Slots.Data module_slot,
		//[Source.Parent<Module.Link>] ref Analog.Relay.Data relay_module, [Source.Owned] ref Analog.Relay.Data relay)
		//{

		//}

		[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnUpdate(ISystem.Info info, Entity entity,
		[Source.Parent<Module.Link>] in Module.Data module, [Source.Owned] in Module.Slots.Data module_slot,
		[Source.Owned] in Transform.Data transform_slot, [Source.Parent<Module.Link>] ref Transform.Data transform_module,
		[Source.Owned] in Body.Data body_slot, [Source.Parent<Module.Link>] ref Body.Data body_module)
		{
			//App.WriteLine("transform");
			body_module.SetTransform(in body_slot);
			transform_module = transform_slot;
		}

		[ISystem.PostUpdate.A(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnUpdateSpriteRenderer(ISystem.Info info, Entity entity,
		[Source.Parent<Module.Link>] in Module.Data module, [Source.Owned] in Module.Slots.Data module_slot, [Source.Owned, Pair.Wildcard] ref Module.Link link,
		[Source.Owned] in Animated.Renderer.Data renderer_parent, [Source.Parent<Module.Link>] ref Animated.Renderer.Data renderer)
		{
			renderer.offset = renderer_parent.offset + module_slot.offset + link.offset;
			renderer.rotation = renderer_parent.rotation + module_slot.rotation + link.rotation;
			renderer.z = renderer_parent.z + module_slot.z + link.z;
			//renderer.scale = renderer_parent.scale;
		}

		[ISystem.Add(ISystem.Mode.Single, ISystem.Scope.Region, order: 1000)]
		public static void OnAdd(ISystem.Info info, ref Region.Data region, Entity entity, Entity ent_module,
		[Source.Parent<Module.Link>] in Module.Data module, [Source.Owned] in Module.Slots.Data module_slot,
		[Source.Parent<Module.Link>] ref Body.Data body)
		{
			body.Disable(force: true);
			//App.WriteLine($"equip {ent_module} on {entity}; prefab:  {ent_module.GetPrefabName()}", App.Color.Cyan);
		}

		[ISystem.Remove(ISystem.Mode.Single, ISystem.Scope.Region, order: 1000)]
		public static void OnRemove(ISystem.Info info, ref Region.Data region, Entity entity, Entity ent_module,
		[Source.Parent<Module.Link>] in Module.Data module, [Source.Owned] in Module.Slots.Data module_slot,
		[Source.Parent<Module.Link>] ref Body.Data body)
		{
			if (module.flags.HasAny(Module.Flags.Unremovable))
			{
#if SERVER
				ent_module.Delete();
#endif
			}
			else
			{
				body.Enable(force: true);
			}

			//App.WriteLine($"unequip {ent_module} on {entity}; prefab: {ent_module.GetPrefabName()}", App.Color.Cyan);

		}

		[ISystem.Add(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("initialized", true, Source.Modifier.Owned)]
		public static void OnAddRenderer(ISystem.Info info, Entity entity,
		[Source.Owned] in Module.Slots.Data module_slot,
		[Source.Parent<Module.Link>] ref Animated.Renderer.Data renderer, [Source.Parent<Module.Link>] ref Module.Data module)
		{
			//renderer.sprite.frame.X = 0;
			renderer.sprite.frame.y = module.frame_stored;
			renderer.sprite.fps = 0.00f;
		}

		[ISystem.Remove(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnRemoveRenderer(ISystem.Info info, Entity entity,
		[Source.Owned] in Module.Slots.Data module_slot,
		[Source.Parent<Module.Link>] ref Animated.Renderer.Data renderer, [Source.Parent<Module.Link>] ref Module.Data module)
		{
			//renderer.sprite.frame.X = 0;
			renderer.sprite.frame.y = module.frame_default;
			renderer.sprite.fps = 0.00f;
			renderer.offset = default;
			renderer.z = default;
		}
	}

	[Asset.Hjson(prefix: "program.", capacity_world: 256, capacity_region: 64, capacity_local: 64, flags_world: Asset.Flags.Recycle, flags_region: Asset.Flags.Recycle, flags_local: Asset.Flags.Recycle)]
	public interface IProgram: IAsset2<IProgram, IProgram.Data>
	{
		static void IAsset2<IProgram, IProgram.Data>.OnInitialize()
		{
			Computer.Init();
		}

		static void IAsset2<IProgram, IProgram.Data>.GenerateSample(out IProgram.Data data)
		{
			data = new();
		}

		public enum Type: byte
		{
			Undefined = 0,
		}

		[Flags]
		public enum Flags: uint
		{
			None = 0,
		}

		public struct Data
		{
			public string name;
			public string desc;

			public Computer.Instruction[] instructions;

			public Data()
			{

			}
		}
	}

	public static partial class Computer
	{
		public static Span<Computer.Instruction> GetInstructionsSpan(ref this Computer.Program.Data computer)
		{
			return computer.instructions.Slice(computer.instructions_capacity);
		}

		//public static partial class Memory
		//{
		//	[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Global | IComponent.Scope.Region)]
		//	public partial struct Data(): IComponent
		//	{
		//		[Flags]
		//		public enum Flags: uint
		//		{
		//			None = 0,
		//		}
		//	}
		//}

		public static partial class Program
		{
			[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Global | IComponent.Scope.Region)]
			public partial struct Data: IComponent
			{
				[Flags]
				public enum Flags: byte
				{
					None = 0,
				}

				public Computer.Program.Data.Flags flags;
				public byte instructions_capacity;

				public FixedArray64<Computer.Instruction> instructions;
			}

			public struct DEV_EditProgramRPC: Net.IRPC<Computer.Program.Data>
			{
				public Computer.Instruction instruction;
				public ushort index;

#if SERVER
				public void Invoke(Net.IRPC.Context rpc, ref Computer.Program.Data data)
				{
					var program_span = data.instructions.Slice(data.instructions_capacity);
					if (this.index < program_span.Length)
					{
						program_span[this.index] = this.instruction;
						data.Sync(rpc.entity);
					}
				}
#endif
			}

			public struct DEV_CreateProgramRPC: Net.IRPC<Computer.Program.Data>
			{
				public FixedString16 name;
				public FixedString32 desc;
				public byte instruction_count;

				public IProgram.Handle h_program_out;

#if SERVER
				public void Invoke(Net.IRPC.Context rpc, ref Computer.Program.Data data)
				{
					Assert.Check(this.instruction_count > 0);

					var program_data = new IProgram.Data();
					program_data.name = this.name.OrDefault("Program");
					program_data.desc = this.desc.OrDefault("<no description>");
					program_data.instructions = new Instruction[this.instruction_count];

					var asset = IProgram.Database.RegisterOrUpdate(Asset.GenerateRandomIdentifier(), index: null, scope: Asset.Scope.World, 0, ref program_data);
					asset.Sync(true);

					this.h_program_out = asset;
				}
#endif
			}

		}

		public enum Command: byte
		{
			None = 0,

			Start,
			Stop,
			Step,
			Restart,
			Reset
		}

		public struct SetMemoryRPC: Net.IRPC<Computer.State>
		{
			public int value;
			public byte index;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Computer.State data)
			{
				var dirty = data.memory[this.index] != this.value;
				if (dirty)
				{
					data.memory[this.index] = this.value;
					data.Sync(rpc.entity, true);
				}
			}
#endif
		}

		public struct ConfigureRPC: Net.IRPC<Computer.Data>
		{
			public Computer.Data.Flags? flags;
			public uint? reg_display_modes;
			public byte? stack_display_modes;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Computer.Data data)
			{
				var sync = false;
				sync |= this.flags.TryCopyTo(ref data.flags);
				sync |= this.reg_display_modes.TryCopyTo(ref data.reg_display_modes);
				sync |= this.stack_display_modes.TryCopyTo(ref data.stack_display_modes);

				if (sync)
				{
					data.Sync(rpc.entity, true);
				}
			}
#endif
		}

		//		// TODO: make it not sync the entire asset
		//		public struct DEV_EditProgramRPC: Net.IRPC<Computer.Data>
		//		{
		//			public Computer.Instruction instruction;
		//			public ushort index;

		//#if SERVER
		//			public void Invoke(Net.IRPC.Context rpc, ref Computer.Data data)
		//			{
		//				ref var program_data = ref data.h_program.GetData(out var asset);
		//				Assert.NotNull(ref program_data, Assert.Level.Error);

		//				var program_span = program_data.instructions.AsSpan();
		//				Assert.Check(this.index < program_span.Length, Assert.Level.Error);

		//				var sync = false;
		//				sync |= true; // this.flags.TryCopyTo(ref data.flags);

		//				program_span[this.index] = this.instruction;

		//				if (sync)
		//				{
		//					asset.Sync();
		//				}
		//			}
		//#endif
		//		}

		public struct CommandRPC: Net.IRPC<Computer.State>
		{
			public Computer.Command command;
			public int arg;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Computer.State data)
			{
				var dirty = false;

				switch (this.command)
				{
					case Computer.Command.Start:
					{
						dirty |= data.flags.TryAddFlag(Computer.State.Flags.Active);
						if (dirty)
						{
							data.flags.RemoveFlag(Computer.State.Flags.Stuck | Computer.State.Flags.Busy | Computer.State.Flags.Mode_Float | Computer.State.Flags.Stopped);
							data.flags.AddFlag(Computer.State.Flags.Skip);
						}
					}
					break;

					case Computer.Command.Stop:
					{
						dirty |= data.flags.TryRemoveFlag(Computer.State.Flags.Active);
						if (dirty)
						{
							data.flags.RemoveFlag(Computer.State.Flags.Stuck | Computer.State.Flags.Busy | Computer.State.Flags.Mode_Float | Computer.State.Flags.Stopped);
							//data.flags.SetFlag(Computer.State.Flags.Skip, true);
						}
					}
					break;

					case Computer.Command.Step:
					{
						if (data.instruction_count > 0)
						{
							data.ip = (byte)Maths.Repeat(this.arg, data.instruction_count);
							dirty = true;
						}
					}
					break;

					case Computer.Command.Restart:
					{
						data.sp = 0;
						data.ip = 0;
						data.stack = default;
						data.message = default;
						dirty = true;

						if (dirty)
						{
							data.flags.RemoveFlag(Computer.State.Flags.Stuck | Computer.State.Flags.Busy | Computer.State.Flags.Mode_Float | Computer.State.Flags.Stopped);
							data.flags.AddFlag(Computer.State.Flags.Skip);
						}
					}
					break;

					case Computer.Command.Reset:
					{
						data.sp = 0;
						data.ip = 0;
						data.stack = default;
						data.message = default;
						dirty = true;

						if (dirty)
						{
							data.flags.RemoveFlag(Computer.State.Flags.Stuck | Computer.State.Flags.Busy | Computer.State.Flags.Mode_Float | Computer.State.Flags.Stopped);
							data.flags.AddFlag(Computer.State.Flags.Skip);
						}
					}
					break;
				}

				if (dirty)
				{
					data.Sync(rpc.entity, true);
				}
			}
#endif
		}

#if CLIENT
		public struct ComputerGUI: IGUICommand
		{
			public Entity ent_computer;
			public Entity ent_program;

			public Computer.Data computer;
			public Computer.State computer_state;

			public Computer.Program.Data? program_nullable;

			public static int selected_row_index;
			//public static IProgram.Handle h_selected_program;

			public static int highlighted_memory;
			public static int highlighted_ip;

			public void Draw()
			{
				using (var window = GUI.Window.Interaction("Computer"u8, this.ent_computer))
				{
					this.StoreCurrentWindowTypeID(order: -100);
					if (window.show)
					{
						ref var region_common = ref this.ent_computer.GetRegionCommon();
						var kb = GUI.GetKeyboard();

						var active = this.computer_state.flags.HasAny(Computer.State.Flags.Active);
						var context = GUI.ItemContext.Begin();

						//var program = default(Computer.Program.Data);

						//var ent_stored = this.ent_computer.GetChild(Relation.Type.Stored);
						//if (ent_stored.IsAlive())
						//{
						//	ref var program_tmp = ref ent_stored.GetComponent<Computer.Program.Data>();
						//	if (program_tmp.IsNotNull())
						//	{
						//		program = program_tmp;
						//	}
						//}

						var program = this.program_nullable.GetValueOrDefault();

						using (GUI.Group.New(size: new Vector2(GUI.RmX, GUI.RmY)))
						{
							using (var group_title = GUI.Group.New(new(GUI.RmX - (64 + 80), 64)))
							{
								if (program.instructions_capacity > 0)
								{
									//GUI.TitleCentered(program_data.name, size: 24, pivot: new(0.00f, 0.50f), offset: new(8, 0));
									//if (this.computer_state.flags.HasAny(Computer.State.Flags.Active)) GUI.TitleCentered($"{Maths.Max(this.computer_state.t_next_cycle - region.GetWorldTime(), 0.00f):0.000}s", size: 16, pivot: new(1.00f, 0.50f), offset: new(-8, 0));
									GUI.TitleCentered(this.computer_state.message, size: 16, pivot: new(1.00f, 0.50f), offset: new(-8, 0));
								}
								else
								{
									GUI.TitleCentered("<insert program card>"u8, size: 24, pivot: new(0.00f, 0.50f), offset: new(8, 0));
								}
							}

							GUI.SameLine();

							using (var group_slot = GUI.Group.New(new(64, 64)))
							{
								using (var slot = Module.BeginSlot(ref context, "slot.card"u8, "Card"u8, this.ent_computer, this.ent_program, size: GUI.Rm, enabled: true))
								{

								}

								//using (var slot = GUI.EntitySlot.New(ref context, "slot.card", "Card", this.ent_program, size: new(GUI.RmY), enabled: true))
								//{
								//	var skip = false;
								//	if (slot.hovered)
								//	{
								//		if (App.debug_mode_gui && kb.GetKey(Keyboard.Key.LeftAlt) && GUI.GetMouse().GetKeyDown(Mouse.Key.Right))
								//		{
								//			GUI.SetDebugEntity(this.ent_program);
								//			skip = true;
								//		}
								//	}

								//	if (slot.pressed && !skip)
								//	{
								//		App.WriteLine(slot.action);

								//		switch (slot.action)
								//		{
								//			case GUI.ItemContext.Action.Add:
								//			{
								//				var rpc = new Module.AddRPC
								//				{
								//					ent_arm = context.ent_pickup,
								//					ent_module = context.ent_pickup_target
								//				};
								//				rpc.Send(this.ent_computer);
								//			}
								//			break;

								//			case GUI.ItemContext.Action.Remove:
								//			{
								//				var rpc = new Module.RemoveRPC()
								//				{
								//					ent_arm = context.ent_pickup,
								//					ent_module = this.ent_program
								//				};
								//				rpc.Send(this.ent_computer);
								//			}
								//			break;

								//			case GUI.ItemContext.Action.Swap:
								//			{
								//				var rpc = new Module.AddRPC
								//				{

								//				};
								//				rpc.Send(this.ent_computer);
								//			}
								//			break;
								//		}
								//	}
								//}
							}

							GUI.SameLine();

							using (var group_buttons = GUI.Group.New(new(GUI.RmX, 64)))
							{
								if (GUI.DrawButton(active ? "Turn Off"u8 : "Turn On"u8, size: new(GUI.RmX, 32), color: active ? GUI.col_button_error : GUI.col_button_ok))
								{
									var rpc = new Computer.CommandRPC()
									{
										command = active ? Computer.Command.Stop : Computer.Command.Start
									};
									rpc.Send(this.ent_computer);
								}

								if (GUI.DrawButton("Reset"u8, size: new(GUI.RmX, 32), color: GUI.col_button_yellow))
								{
									var rpc = new Computer.CommandRPC()
									{
										command = Computer.Command.Reset
									};
									rpc.Send(this.ent_computer);
								}
							}

							GUI.SeparatorThick();

							using (var group_data = GUI.Group.New(size: new(GUI.RmX - 232, GUI.RmY), padding: new(4)))
							{
								//group_data.DrawBackground(GUI.tex_frame);

								using (var group_stack = GUI.Group.New(size: new(GUI.RmX, (this.computer_state.stack.Length * 24) + 8), padding: new(4)))
								{
									group_stack.DrawBackground(GUI.tex_frame);

									var stack_span = this.computer_state.stack.AsSpan();
									for (var i = 0; i < stack_span.Length; i++)
									{
										using (GUI.ID<Computer.Data, int>.Push(i))
										{
											using (var group_row = GUI.Group.New(size: new(GUI.RmX, 24), padding: new(4, 0)))
											{
												if (group_row.IsVisible())
												{
													group_row.DrawBackground(GUI.tex_panel_white, color: ((this.computer_state.sp == i) ? GUI.col_white.WithAlphaMult(0.30f) : GUI.col_black.WithAlphaMult(0.20f)));

													ref var value = ref stack_span[i];
													using (GUI.Group.New(size: new(80, GUI.RmY)))
													{
														GUI.TitleCentered(value, pivot: new(0.00f, 0.50f), offset: new(4, 0));
													}

													GUI.SameLine(4);

													//using (GUI.Group.New(size: new(96, GUI.RmY)))
													//{
													//	//GUI.TitleCentered($"{Maths.ToInt32BitCast(value):0.000}", pivot: new(0.00f, 0.50f), offset: new(4, 0));
													//}

													//GUI.SameLine(4);

													using (GUI.Group.New(size: new(GUI.RmX, GUI.RmY)))
													{
														GUI.TitleCentered(value.ToFormattedBinary(insert_spaces: true), pivot: new(1.00f, 0.50f), offset: new(-4, 0), font: GUI.Font.Monaco, size: 12);
													}
												}
											}
										}
									}
								}

								using (var group_memory = GUI.Group.New(size: GUI.Rm, padding: new(4)))
								{
									group_memory.DrawBackground(GUI.tex_panel);

									var reg_span = this.computer_state.memory.AsSpan();

									using (var scrollbox = GUI.Scrollbox.New("memory"u8, size: new(GUI.RmX, GUI.RmY), padding: new(0, 0)))
									{
										for (var i = 0; i < reg_span.Length; i++)
										{
											using (GUI.ID<Computer.Data, int>.Push(i))
											{
												using (var group_row = GUI.Group.New(size: new(GUI.RmX, 24), padding: new(4, 0)))
												{
													if (group_row.IsVisible())
													{
														var color = GUI.col_black.WithAlphaMult(0.20f);
														if (i == highlighted_memory)
														{
															color = GUI.col_button_yellow.WithAlphaMult(0.40f);
															highlighted_memory = -1;
														}

														group_row.DrawBackground(GUI.tex_panel_white, color: color);

														var value = reg_span[i];
														var value_f32 = Maths.ToFloat32BitCast(value);

														var display_mode_mask = (1u << i);
														var is_float = (this.computer.reg_display_modes & display_mode_mask) != 0;

														var is_float_tmp = is_float;
														if (GUI.Checkbox("Integer/Float"u8, ref is_float_tmp, new(GUI.RmY), show_text: false, show_tooltip: true))
														{
															var mode = this.computer.reg_display_modes;
															if (is_float_tmp) mode |= display_mode_mask;
															else mode &= ~display_mode_mask;

															var rpc = new Computer.ConfigureRPC()
															{
																reg_display_modes = mode
															};
															rpc.Send(this.ent_computer);
														}

														GUI.SameLine();

														using (GUI.Group.New(size: new(GUI.RmX - 224, GUI.RmY)))
														{
															if (is_float)
															{
																if (GUI.ScalarInput("mem_float"u8, ref value_f32, GUI.Rm, show_frame: false, format: Maths.NumberFormat.Real3))
																{
																	var rpc = new Computer.SetMemoryRPC()
																	{
																		value = Maths.ToInt32BitCast(value_f32),
																		index = (byte)i,
																	};
																	rpc.Send(this.ent_computer);
																}
															}
															else
															{
																if (GUI.ScalarInput("mem_int"u8, ref value, GUI.Rm, show_frame: false))
																{
																	var rpc = new Computer.SetMemoryRPC()
																	{
																		value = value,
																		index = (byte)i,
																	};
																	rpc.Send(this.ent_computer);
																}
															}
															//GUI.TitleCentered($"{value}", pivot: new(1.00f, 0.50f), offset: new(-4, 0));

															//GUI.TitleCentered($"{Maths.ToFloat32BitCast(value):0.00}", pivot: new(1.00f, 0.50f), offset: new(-4, 0));
														}

														GUI.SameLine(4);

														using (GUI.Group.New(size: new(GUI.RmX, GUI.RmY)))
														{
															GUI.TitleCentered(value.ToFormattedBinary(insert_spaces: true), pivot: new(1.00f, 0.50f), offset: new(-4, 0), font: GUI.Font.Monaco, size: 12);
														}
													}
												}
											}
										}
									}
								}
							}

							GUI.SameLine();

							using (var group_data = GUI.Group.New(size: GUI.Rm, padding: new(4)))
							{
								group_data.DrawBackground(GUI.tex_panel);

								//ref var program_data = ref this.program.GetRefOrNull();

								using (var scrollbox = GUI.Scrollbox.New("instructions"u8, size: GUI.Rm, padding: new(8, 8)))
								{
									var instr_span = program.GetInstructionsSpan();
									if (!instr_span.IsEmpty)
									{
										if (this.computer_state.flags.HasAny(Computer.State.Flags.Active) && (uint)this.computer_state.ip < instr_span.Length)
										{
											ref var current_instr = ref instr_span[this.computer_state.ip];
											if (current_instr.op < OpCode.MAX_COUNT)
											{
												ref var op_info = ref ops[(int)current_instr.op];

												if (op_info.flags.HasAny(OperationInfo.Flags.Arg_IP)) highlighted_ip = current_instr.arg;
												if (op_info.flags.HasAny(OperationInfo.Flags.Arg_Reg)) highlighted_memory = current_instr.arg;
											}
										}

										for (var i = 0; i < instr_span.Length; i++)
										{
											using (GUI.ID<Computer.Data, Computer.Instruction>.Push(i))
											{
												using (var group_row = GUI.Group.New(size: new(GUI.RmX, 24), padding: new(4, 0)))
												{
													if (group_row.IsVisible())
													{
														ref var instr = ref instr_span[i];
														var instr_copy = instr;
														ref var op_info = ref ops[(int)instr.op];

														var color = GUI.col_black;
														if (this.computer_state.ip == i) color = this.computer_state.flags.HasAny(Computer.State.Flags.Stuck) ? GUI.col_button_error : GUI.col_white;

														if (i == highlighted_ip)
														{
															color = GUI.col_button_yellow;
															highlighted_ip = -1;
														}

														group_row.DrawBackground(GUI.tex_panel_white, color: color.WithAlphaMult(0.50f));

														using (var group_index = GUI.Group.New(size: new(28, GUI.RmY)))
														{
															var dirty = false;
															GUI.TitleCentered($"{i:00}", pivot: new(0.50f, 0.50f), offset: new(0, 0), color: GUI.font_color_desc);

															if (group_index.IsHovered())
															{
																var arg_index = (int)this.computer_state.ip;
																dirty |= GUI.ScrollInput(group_index.GetOuterRect(), ref arg_index, 1, -1, instr_span.Length, invert: true);
																if (dirty)
																{
																	var rpc = new Computer.CommandRPC()
																	{
																		command = Command.Step,
																		arg = arg_index,
																	};
																	rpc.Send(this.ent_computer);
																}
															}
														}

														GUI.SameLine();

														using (GUI.Group.New(size: new(GUI.RmX - 40, GUI.RmY)))
														{
															var dirty = false;
															dirty |= GUI.EnumInput("instr.op"u8, ref instr_copy.op, size: GUI.Rm, show_label: false, height: 24 * 12, close_on_select: true, show_none: true, show_tooltip: false, max_value: OpCode.MAX_COUNT);

															var op_int = (int)instr_copy.op;
															dirty |= GUI.ScrollInput(GUI.GetLastItemRect(), ref op_int, 1, 0, ((int)Computer.OpCode.MAX_COUNT) - 1, invert: true);
															instr_copy.op = (Computer.OpCode)op_int;

															if (dirty)
															{
																var rpc = new Computer.Program.DEV_EditProgramRPC()
																{
																	instruction = instr_copy,
																	index = (ushort)i
																};
																rpc.Send(this.ent_program);
															}
															//GUI.TitleCentered(instr.op.ToString(), pivot: new(0.00f, 0.50f), offset: new(4, 0));
														}

														GUI.SameLine();

														using (GUI.Group.New(size: new(GUI.RmX, GUI.RmY)))
														{
															var dirty = false;
															var arg_enabled = op_info.flags.HasAny(OperationInfo.Flags.Arg_Reg | OperationInfo.Flags.Arg_IP | OperationInfo.Flags.Arg_Misc);

															var arg_int = (int)instr_copy.arg;
															dirty |= GUI.ScalarInput("instr.arg"u8, ref arg_int, size: GUI.Rm, format: Maths.NumberFormat.Int00, show_frame: false, enabled: arg_enabled);
															if (arg_enabled) dirty |= GUI.ScrollInput(GUI.GetLastItemRect(), ref arg_int, 1, 0, 32, invert: true);
															if (op_info.flags.HasAny(OperationInfo.Flags.Arg_Reg)) arg_int = Maths.Clamp(arg_int, 0, this.computer_state.memory.Length - 1);
															if (op_info.flags.HasAny(OperationInfo.Flags.Arg_IP)) arg_int = Maths.Clamp(arg_int, 0, instr_span.Length - 1);
															instr_copy.arg = (byte)arg_int;

															if (dirty)
															{
																var rpc = new Computer.Program.DEV_EditProgramRPC()
																{
																	instruction = instr_copy,
																	index = (ushort)i
																};
																rpc.Send(this.ent_program);
															}
															//GUI.TitleCentered($"{instr.arg}", pivot: new(1.00f, 0.50f), offset: new(-4, 0), font: GUI.Font.Monaco, size: 14);
														}

														if (GUI.IsHoveringRect(group_row.GetOuterRect()))
														{
															using (var tooltip = GUI.Tooltip.New(pos: group_row.GetOuterRect().GetPosition(1.00f, 0.50f), pivot: new(0.00f, 0.50f), size: new(300, 0)))
															{
																using (GUI.Group.New(size: new(GUI.RmX, 24), padding: new(4, 0)))
																{
																	//GUI.TitleCentered($"[0x{((int)instr.op):x2}] {instr.op}", size: 24, pivot: new(0.00f, 0.50f)); // | \"{op_info.name}\"", size: 20);
																	GUI.TitleCentered(instr.op.ToStringUtf8(), size: 24, pivot: new(0.00f, 0.50f));
																	//GUI.SameLine();
																	GUI.TitleCentered(op_info.name, font: GUI.Font.Monaco, size: 14, pivot: new(1.00f, 0.50f));
																}

																GUI.SeparatorThick();

																using (GUI.Group.New(size: new(GUI.RmX, 0), padding: new(4, 4)))
																{
																	using (GUI.Wrap.Push(GUI.RmX))
																	{
																		GUI.Text(op_info.desc, color: GUI.font_color_desc);

																		GUI.NewLine(2);

																		GUI.LabelShaded("Execution time:"u8, op_info.time, "0.000's'");
																	}
																}
															}

															if (op_info.flags.HasAny(OperationInfo.Flags.Arg_IP)) highlighted_ip = instr.arg;
															if (op_info.flags.HasAny(OperationInfo.Flags.Arg_Reg)) highlighted_memory = instr.arg;
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}

		[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnGUI(Entity ent_computer, Entity ent_program,
		[Source.Owned] in Computer.Data computer, [Source.Owned] in Computer.State computer_state,
		[Source.Parent<Module.Link>, Optional(true)] ref Computer.Program.Data program,
		[Source.Owned] in Interactable.Data interactable)
		{
			if (interactable.IsActive())
			{
				var gui = new ComputerGUI()
				{
					ent_computer = ent_computer,

					computer = computer,
					computer_state = computer_state,
				};

				if (program.IsNotNull())
				{
					gui.ent_program = ent_program;
					gui.program_nullable = program;
				}

				gui.Submit();
			}
		}
#endif

		public delegate void OperationDelegate(ref Computer.State computer, byte arg);
		public struct OperationInfo
		{
			[Flags]
			public enum Flags: uint
			{
				None = 0,

				Arg_Reg = 1u << 0,
				Arg_IP = 1u << 1,
				Arg_Misc = 1u << 2,

				Constant_Pitch = 1u << 3,
				Constant_Time = 1u << 4,
			}

			public string name;
			public string desc;

			public float time = 0.50f;

			public Sound.Handle sound;
			public float volume = 0.20f;
			public float pitch = 1.00f;

			public OperationInfo.Flags flags;
			public OperationDelegate func;

			public OperationInfo(string name, string desc, float time, Sound.Handle sound, float volume, float pitch, OperationInfo.Flags flags, OperationDelegate func)
			{
				this.name = name;
				this.desc = desc;
				this.time = time;
				this.sound = sound;
				this.volume = volume;
				this.pitch = pitch;
				this.flags = flags;
				this.func = func;
			}
		}

		public enum OpCode: byte
		{
			NOP = 0,

			// stack, memory 
			[Name("LD", desc: "Load from memory.")]
			LD, // load memory
			[Name("LD_P", desc: "Load from memory and push.")]
			LD_P, // load memory + push
			[Name("ST", desc: "Store to memory.")]
			ST, // store memory
			[Name("ST_P", desc: "Store to memory and pop.")]
			ST_P, // store memory + pop

			PUSH, // push stack
			POP, // pop stack
			DUP, // duplicate stack

			// arithmetic
			MATH, // set arithmetic mode (float/integer)
			ADD, // +
			SUB, // -
			MUL, // *
			DIV, // /
			INC, // ++
			DEC, // --

			// bitwise
			SHL, // << shift left
			SHR, // >> shift right
			AND, // &
			NOT, // ~ 
			XOR, // ^
			OR, // |

			// flow control
			JMP, // set ip to value

			JMP_Z,
			JMP_NZ,

			JMP_EQ,
			JMP_NEQ,

			JMP_LSEQ,
			JMP_LS,

			JMP_GREQ,
			JMP_GR,

			//BNE,
			//BLT,
			//BLE,
			//BGE,
			//BGT,

			// special
			PRINT, // print
			BELL, // ping/bell
			CLR_S, // clear stack
			CLR_M, // clear memory

			STOP,
			RESET,

			MAX_COUNT
		}

		public static readonly OperationInfo[] ops = new OperationInfo[(int)OpCode.MAX_COUNT];
		public static void Init()
		{
			//App.WriteLine("init computo");

			ops[(int)OpCode.NOP] = new(name: "No Operation", desc: "TODO: Desc", time: 0.10f, sound: "computer.simple.01", volume: 0.50f, pitch: 1.00f, flags: OperationInfo.Flags.None,
				func: static (ref Computer.State data, byte arg) =>
				{
					data.ip++;
				});

			ops[(int)OpCode.LD] = new(name: "Load from Memory", desc: "TODO: Desc", time: 0.45f, sound: "computer.read_write.03", volume: 0.50f, pitch: 1.00f, flags: OperationInfo.Flags.Arg_Reg,
				func: static (ref Computer.State data, byte arg) =>
				{
					var x = data.memory[arg];
					data.stack[data.sp] = x;
					data.ip++;
				});

			ops[(int)OpCode.ST] = new(name: "Store to Memory", desc: "TODO: Desc", time: 0.45f, sound: "computer.read_write.00", volume: 0.50f, pitch: 1.00f, flags: OperationInfo.Flags.Arg_Reg,
				func: static (ref Computer.State data, byte arg) =>
				{
					var x = data.stack[data.sp];
					data.memory[arg] = x;
					data.ip++;
				});

			ops[(int)OpCode.LD_P] = new(name: "Load from Memory & Push", desc: "TODO: Desc", time: 0.50f, sound: "computer.read_write.03", volume: 0.50f, pitch: 1.00f, flags: OperationInfo.Flags.Arg_Reg,
				func: static (ref Computer.State data, byte arg) =>
				{
					var x = data.memory[arg];
					data.stack[++data.sp] = x;
					data.ip++;
				});

			ops[(int)OpCode.ST_P] = new(name: "Store to Memory & Pop", desc: "TODO: Desc", time: 0.50f, sound: "computer.read_write.00", volume: 0.50f, pitch: 1.00f, flags: OperationInfo.Flags.Arg_Reg,
				func: static (ref Computer.State data, byte arg) =>
				{
					ref var x = ref data.stack[data.sp--];
					data.memory[arg] = x;
					data.ip++;

					x = default;
				});

			ops[(int)OpCode.PUSH] = new(name: "Push Stack", desc: "TODO: Desc", time: 0.15f, sound: "computer.simple.02", volume: 0.50f, pitch: 1.00f, flags: OperationInfo.Flags.None,
				func: static (ref Computer.State data, byte arg) =>
				{
					data.sp++;
					data.ip++;
				});

			ops[(int)OpCode.POP] = new(name: "Pop Stack", desc: "TODO: Desc", time: 0.15f, sound: "computer.simple.03", volume: 0.50f, pitch: 1.00f, flags: OperationInfo.Flags.None,
				func: static (ref Computer.State data, byte arg) =>
				{
					ref var x = ref data.stack[data.sp--];
					data.ip++;

					x = default;
				});

			ops[(int)OpCode.DUP] = new(name: "Duplicate Stack", desc: "TODO: Desc", time: 0.29f, sound: "computer.read_write.01", volume: 0.50f, pitch: 1.00f, flags: OperationInfo.Flags.None,
				func: static (ref Computer.State data, byte arg) =>
				{
					var x = data.stack[data.sp];
					data.stack[++data.sp] = x;
					data.ip++;
				});

			ops[(int)OpCode.CLR_S] = new(name: "Clear Stack", desc: "TODO: Desc", time: 1.50f, sound: "computer.clear.00", volume: 0.50f, pitch: 1.00f, flags: OperationInfo.Flags.None,
				func: static (ref Computer.State data, byte arg) =>
				{
					data.stack = default;
					data.sp = 0;
					data.ip++;
				});

			ops[(int)OpCode.CLR_M] = new(name: "Clear Memory", desc: "TODO: Desc", time: 1.50f, sound: "computer.clear.00", volume: 0.50f, pitch: 1.00f, flags: OperationInfo.Flags.None,
				func: static (ref Computer.State data, byte arg) =>
				{
					data.memory = default;
					data.ip++;
				});


			ops[(int)OpCode.SHL] = new(name: "Shift Left", desc: "TODO: Desc", time: 0.20f, sound: "computer.shift.00", volume: 0.50f, pitch: 1.00f, flags: OperationInfo.Flags.None,
				func: static (ref Computer.State data, byte arg) =>
				{
					ref var y = ref data.stack[data.sp--];
					ref var x = ref data.stack[data.sp];

					data.stack[data.sp] = unchecked(x << y);
					data.ip++;

					y = default;
				});

			ops[(int)OpCode.SHR] = new(name: "Shift Right", desc: "TODO: Desc", time: 0.20f, sound: "computer.shift.01", volume: 0.50f, pitch: 1.00f, flags: OperationInfo.Flags.None,
				func: static (ref Computer.State data, byte arg) =>
				{
					ref var y = ref data.stack[data.sp--];
					ref var x = ref data.stack[data.sp];

					data.stack[data.sp] = unchecked(x >> y);
					data.ip++;

					y = default;
				});

			ops[(int)OpCode.AND] = new(name: "Bitwise AND", desc: "TODO: Desc", time: 0.22f, sound: "computer.simple.03", volume: 0.50f, pitch: 1.00f, flags: OperationInfo.Flags.None,
				func: static (ref Computer.State data, byte arg) =>
				{
					ref var y = ref data.stack[data.sp--];
					ref var x = ref data.stack[data.sp];

					data.stack[data.sp] = unchecked(x & y);
					data.ip++;

					y = default;
				});

			ops[(int)OpCode.NOT] = new(name: "Bitwise NOT", desc: "TODO: Desc", time: 0.28f, sound: "computer.simple.02", volume: 0.50f, pitch: 1.00f, flags: OperationInfo.Flags.None,
				func: static (ref Computer.State data, byte arg) =>
				{
					var x = data.stack[data.sp];

					data.stack[data.sp] = unchecked(~x);
					data.ip++;
				});

			ops[(int)OpCode.XOR] = new(name: "Bitwise XOR", desc: "TODO: Desc", time: 0.28f, sound: "computer.simple.03", volume: 0.50f, pitch: 1.00f, flags: OperationInfo.Flags.None,
				func: static (ref Computer.State data, byte arg) =>
				{
					ref var y = ref data.stack[data.sp--];
					ref var x = ref data.stack[data.sp];

					data.stack[data.sp] = unchecked(x ^ y);
					data.ip++;

					y = default;
				});

			ops[(int)OpCode.OR] = new(name: "Bitwise OR", desc: "TODO: Desc", time: 0.22f, sound: "computer.simple.03", volume: 0.50f, pitch: 1.00f, flags: OperationInfo.Flags.None,
				func: static (ref Computer.State data, byte arg) =>
				{
					ref var y = ref data.stack[data.sp--];
					ref var x = ref data.stack[data.sp];

					data.stack[data.sp] = unchecked(x | y);
					data.ip++;

					y = default;
				});


			ops[(int)OpCode.ADD] = new(name: "Add", desc: "TODO: Desc", time: 0.45f, sound: "computer.read_write.03", volume: 0.50f, pitch: 1.00f, flags: OperationInfo.Flags.None,
				func: static (ref Computer.State data, byte arg) =>
				{
					ref var y = ref data.stack[data.sp--];
					ref var x = ref data.stack[data.sp];

					if (data.flags.HasAny(Computer.State.Flags.Mode_Float))
					{
						var y_flt = Maths.ToFloat32BitCast(y);
						var x_flt = Maths.ToFloat32BitCast(x);
						data.stack[data.sp] = Maths.ToInt32BitCast(unchecked(x_flt + y_flt));
					}
					else
					{
						data.stack[data.sp] = unchecked(x + y);
					}

					data.ip++;

					y = default;
				});

			ops[(int)OpCode.SUB] = new(name: "Subtract", desc: "TODO: Desc", time: 0.45f, sound: "computer.read_write.03", volume: 0.50f, pitch: 1.00f, flags: OperationInfo.Flags.None,
				func: static (ref Computer.State data, byte arg) =>
				{
					ref var y = ref data.stack[data.sp--];
					ref var x = ref data.stack[data.sp];

					if (data.flags.HasAny(Computer.State.Flags.Mode_Float))
					{
						var y_flt = Maths.ToFloat32BitCast(y);
						var x_flt = Maths.ToFloat32BitCast(x);
						data.stack[data.sp] = Maths.ToInt32BitCast(unchecked(x_flt - y_flt));
					}
					else
					{
						data.stack[data.sp] = unchecked(x - y);
					}

					data.ip++;
					y = default;
				});

			ops[(int)OpCode.MUL] = new(name: "Multiply", desc: "TODO: Desc", time: 0.80f, sound: "computer.read_write.04", volume: 0.50f, pitch: 1.00f, flags: OperationInfo.Flags.None,
				func: static (ref Computer.State data, byte arg) =>
				{
					ref var y = ref data.stack[data.sp--];
					ref var x = ref data.stack[data.sp];

					if (data.flags.HasAny(Computer.State.Flags.Mode_Float))
					{
						var y_flt = Maths.ToFloat32BitCast(y);
						var x_flt = Maths.ToFloat32BitCast(x);
						data.stack[data.sp] = Maths.ToInt32BitCast(unchecked(x * y));
					}
					else
					{
						data.stack[data.sp] = unchecked(x * y);
					}

					data.ip++;
					y = default;
				});

			ops[(int)OpCode.DIV] = new(name: "Divide", desc: "TODO: Desc", time: 1.42f, sound: "computer.complex.00", volume: 0.50f, pitch: 1.00f, flags: OperationInfo.Flags.None,
				func: static (ref Computer.State data, byte arg) =>
				{
					ref var y = ref data.stack[data.sp--];
					ref var x = ref data.stack[data.sp];

					if (data.flags.HasAny(Computer.State.Flags.Mode_Float))
					{
						var y_flt = Maths.ToFloat32BitCast(y);
						var x_flt = Maths.ToFloat32BitCast(x);

						var ret = unchecked(x_flt / y_flt);
						if (!float.IsFinite(ret))
						{
							data.flags |= Computer.State.Flags.Error;
							ret = 0;
						}

						data.stack[data.sp] = Maths.ToInt32BitCast(ret);
					}
					else
					{
						if (y == 0)
						{
							data.flags |= Computer.State.Flags.Error;
							data.stack[data.sp] = 0;
						}
						else
						{
							var ret = unchecked(x / y);
							data.stack[data.sp] = ret;
						}
					}

					data.ip++;
					y = default;
				});

			ops[(int)OpCode.INC] = new(name: "Increment", desc: "TODO: Desc", time: 0.32f, sound: "computer.simple.02", volume: 0.50f, pitch: 1.00f, flags: OperationInfo.Flags.None,
				func: static (ref Computer.State data, byte arg) =>
				{
					ref var x = ref data.stack[data.sp];

					if (data.flags.HasAny(Computer.State.Flags.Mode_Float))
					{
						var x_flt = Maths.ToFloat32BitCast(x);
						data.stack[data.sp] = Maths.ToInt32BitCast(unchecked(x_flt + 1.00f));
					}
					else
					{
						data.stack[data.sp] = unchecked(x + 1);
					}

					data.ip++;
				});

			ops[(int)OpCode.DEC] = new(name: "Decrement", desc: "TODO: Desc", time: 0.32f, sound: "computer.simple.03", volume: 0.50f, pitch: 1.00f, flags: OperationInfo.Flags.None,
				func: static (ref Computer.State data, byte arg) =>
				{
					ref var x = ref data.stack[data.sp];

					if (data.flags.HasAny(Computer.State.Flags.Mode_Float))
					{
						var x_flt = Maths.ToFloat32BitCast(x);
						data.stack[data.sp] = Maths.ToInt32BitCast(unchecked(x_flt - 1.00f));
					}
					else
					{
						data.stack[data.sp] = unchecked(x - 1);
					}

					data.ip++;
				});


			ops[(int)OpCode.PRINT] = new(name: "Print", desc: "TODO: Desc", time: 0.45f, sound: "computer.print.00", volume: 0.50f, pitch: 1.00f, flags: OperationInfo.Flags.Arg_Reg | OperationInfo.Flags.Constant_Pitch | OperationInfo.Flags.Constant_Time,
				func: static (ref Computer.State data, byte arg) =>
				{
					var x = data.memory[arg];

					data.message = data.flags.HasAny(Computer.State.Flags.Mode_Float) ? Maths.ToFloat32BitCast(x).ToString("{0.0000}") : x.ToString();
					data.ip++;
				});

			ops[(int)OpCode.MATH] = new(name: "Arithmetic Mode", desc: "TODO: Desc", time: 1.20f, sound: "computer.shift.00", volume: 0.50f, pitch: 1.00f, flags: OperationInfo.Flags.Arg_Misc,
				func: static (ref Computer.State data, byte arg) =>
				{
					data.flags.SetFlag(Computer.State.Flags.Mode_Float, arg != 0);
					data.ip++;
				});

			ops[(int)OpCode.JMP] = new(name: "Jump to instruction", desc: "TODO: Desc", time: 0.20f, sound: "computer.jump.00", volume: 0.50f, pitch: 1.00f, flags: OperationInfo.Flags.Arg_IP,
				func: static (ref Computer.State data, byte arg) =>
				{
					data.ip = arg;
				});

			ops[(int)OpCode.JMP_EQ] = new(name: "Jump if ==", desc: "TODO: Desc", time: 0.40f, sound: "computer.jump.00", volume: 0.50f, pitch: 1.00f, flags: OperationInfo.Flags.Arg_IP,
				func: static (ref Computer.State data, byte arg) =>
				{
					ref var y = ref data.stack[data.sp--];
					ref var x = ref data.stack[data.sp];

					if (x == y) data.ip = arg;
					else data.ip++;

					y = default;
				});

			ops[(int)OpCode.JMP_NEQ] = new(name: "Jump if !=", desc: "TODO: Desc", time: 0.40f, sound: "computer.jump.00", volume: 0.50f, pitch: 1.00f, flags: OperationInfo.Flags.Arg_IP,
				func: static (ref Computer.State data, byte arg) =>
				{
					ref var y = ref data.stack[data.sp--];
					ref var x = ref data.stack[data.sp];

					if (x != y) data.ip = arg;
					else data.ip++;

					y = default;
				});

			ops[(int)OpCode.JMP_Z] = new(name: "Jump if == 0", desc: "TODO: Desc", time: 0.25f, sound: "computer.jump.00", volume: 0.50f, pitch: 1.00f, flags: OperationInfo.Flags.Arg_IP,
				func: static (ref Computer.State data, byte arg) =>
				{
					ref var x = ref data.stack[data.sp];

					if (x == 0) data.ip = arg;
					else data.ip++;
				});

			ops[(int)OpCode.JMP_NZ] = new(name: "Jump if != 0", desc: "TODO: Desc", time: 0.25f, sound: "computer.jump.00", volume: 0.50f, pitch: 1.00f, flags: OperationInfo.Flags.Arg_IP,
				func: static (ref Computer.State data, byte arg) =>
				{
					ref var x = ref data.stack[data.sp];

					if (x != 0) data.ip = arg;
					else data.ip++;
				});

			ops[(int)OpCode.JMP_LSEQ] = new(name: "Jump if <=", desc: "TODO: Desc", time: 0.40f, sound: "computer.jump.00", volume: 0.50f, pitch: 1.00f, flags: OperationInfo.Flags.Arg_IP,
				func: static (ref Computer.State data, byte arg) =>
				{
					ref var y = ref data.stack[data.sp--];
					ref var x = ref data.stack[data.sp];

					if (x <= y) data.ip = arg;
					else data.ip++;

					y = default;
				});

			ops[(int)OpCode.JMP_LS] = new(name: "Jump if <", desc: "TODO: Desc", time: 0.40f, sound: "computer.jump.00", volume: 0.50f, pitch: 1.00f, flags: OperationInfo.Flags.Arg_IP,
				func: static (ref Computer.State data, byte arg) =>
				{
					ref var y = ref data.stack[data.sp--];
					ref var x = ref data.stack[data.sp];

					if (x < y) data.ip = arg;
					else data.ip++;

					y = default;
				});

			ops[(int)OpCode.JMP_GREQ] = new(name: "Jump if >=", desc: "TODO: Desc", time: 0.40f, sound: "computer.jump.00", volume: 0.50f, pitch: 1.00f, flags: OperationInfo.Flags.Arg_IP,
				func: static (ref Computer.State data, byte arg) =>
				{
					ref var y = ref data.stack[data.sp--];
					ref var x = ref data.stack[data.sp];

					if (x >= y) data.ip = arg;
					else data.ip++;

					y = default;
				});

			ops[(int)OpCode.JMP_GR] = new(name: "Jump if >", desc: "TODO: Desc", time: 0.40f, sound: "computer.jump.00", volume: 0.50f, pitch: 1.00f, flags: OperationInfo.Flags.Arg_IP,
				func: static (ref Computer.State data, byte arg) =>
				{
					ref var y = ref data.stack[data.sp--];
					ref var x = ref data.stack[data.sp];

					if (x > y) data.ip = arg;
					else data.ip++;

					y = default;
				});

			ops[(int)OpCode.STOP] = new(name: "Stop", desc: "TODO: Desc", time: 0.40f, sound: "computer.jump.00", volume: 0.50f, pitch: 1.00f, flags: OperationInfo.Flags.None,
				func: static (ref Computer.State data, byte arg) =>
				{
					data.flags.RemoveFlag(Computer.State.Flags.Active);

					data.ip++;
				});

			ops[(int)OpCode.RESET] = new(name: "Reset", desc: "TODO: Desc", time: 1.50f, sound: "computer.clear.00", volume: 0.50f, pitch: 1.00f, flags: OperationInfo.Flags.None,
				func: static (ref Computer.State data, byte arg) =>
				{
					data.ip = 0;
					data.sp = 0;
					data.stack = default;
					data.flags.RemoveFlag(Computer.State.Flags.Stuck | Computer.State.Flags.Mode_Float | Computer.State.Flags.Stopped);
					data.flags.AddFlag(Computer.State.Flags.Skip);
				});

			ops[(int)OpCode.BELL] = new(name: "Ping", desc: "TODO: Desc", time: 0.38f, sound: "computer.bell.00", volume: 0.80f, pitch: 1.00f, flags: OperationInfo.Flags.Constant_Pitch | OperationInfo.Flags.Constant_Time,
				func: static (ref Computer.State data, byte arg) =>
				{
					data.ip++;
				});
		}

		[Save.Inline]
		public struct Instruction
		{
			public OpCode op;
			public byte arg;

			public Instruction(OpCode op, byte arg)
			{
				this.op = op;
				this.arg = arg;
			}
		}

		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Global | IComponent.Scope.Region), IComponent.With<Computer.State>]
		public partial struct Data(): IComponent
		{
			[Flags]
			public enum Flags: uint
			{
				None = 0,

				Failsafe = 1 << 0,
			}

			public Computer.Data.Flags flags;
			//public IProgram.Handle h_program;

			public float speed = 1.00f;

			public uint reg_display_modes;
			public byte stack_display_modes;
		}

		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Global | IComponent.Scope.Region)]
		public partial struct State: IComponent
		{
			[Flags]
			public enum Flags: byte
			{
				None = 0,

				Active = 1 << 0,
				Stopped = 1 << 1,

				Busy = 1 << 3,
				Skip = 1 << 4,
				Stuck = 1 << 5,
				Error = 1 << 6,
				Mode_Float = 1 << 7,
			}

			public FixedArray16<int> memory;
			public FixedArray8<int> stack;

			public FixedString16 message;

			public byte ip;
			public byte sp;
			public Computer.State.Flags flags;

			[Asset.Ignore] public byte instruction_count;
			//public Computer.Instruction current_instruction;

			[Net.Ignore, Save.Ignore] public float current_shake;
			[Net.Ignore, Save.Ignore] public float t_last_cycle;
			[Net.Ignore, Save.Ignore] public float t_next_cycle;
		}

		//[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region)]
		//public static void UpdateComputerProgram(ISystem.Info info, Entity entity, ref Region.Data region,
		//[Source.Stored] ref Computer.State computer_state, [Source.Owned] ref Computer.Program.Data program)
		//{
		//	computer_state.instructions_count = program.instructions_capacity;

		//	var instructions_span = program.instructions.Slice(program.instructions_capacity);
		//	computer_state.current_instruction = instructions_span.GetAtIndexOrDefault((uint)computer_state.ip);
		//}

		//[ISystem.Update.B(ISystem.Mode.Single, ISystem.Scope.Region)]
		//public static void UpdateComputerProgram(ISystem.Info info, Entity entity, ref Region.Data region,
		//[Source.Owned] ref Computer.State computer_state,
		//[Source.Parent<Module.Link>] ref Computer.Program.Data program, [Source.Parent<Module.Link>] ref Animated.Renderer.Data renderer)
		//{
		//	computer_state.instructions_count = program.instructions_capacity;

		//	var instructions_span = program.GetInstructionsSpan();
		//	computer_state.current_instruction = instructions_span.GetAtIndexOrDefault((uint)computer_state.ip);

		//	//if (instructions_span.Length > 0)
		//	//{
		//	//	renderer.offset.Y += Maths.Normalize01(computer_state.ip, instructions_span.Length);
		//	//}
		//}

		[ISystem.PostUpdate.B(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void UpdateCardEffects(ISystem.Info info, Entity entity, ref Region.Data region,
		[Source.Owned] ref Computer.State computer_state,
		[Source.Parent<Module.Link>] ref Computer.Program.Data program, [Source.Owned, Pair.Wildcard] ref Module.Link link)
		{
			if (program.instructions_capacity > 0)
			{
				//var t = (computer_state.t_next_cycle - info.WorldTime) 
				//link.offset.Y = Maths.Lerp(link.offset.Y, Maths.Normalize01(computer_state.ip, program.instructions_capacity) * -0.50f, Maths.InvLerp01(computer_state.t_next_cycle, computer_state.t_last_cycle, info.WorldTime) * 0.50f);
				link.offset.Y = Maths.Lerp(link.offset.Y, Maths.Normalize01(computer_state.ip, program.instructions_capacity) * -0.50f, 0.25f);
				//link.offset.Y = Maths.Normalize01(computer_state.ip, program.instructions_capacity) * -0.50f;
			}
		}

		[ISystem.Monitor(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnChangeCard(ISystem.Info info, Entity entity, ref Region.Data region,
		[Source.Owned] ref Computer.State computer_state,
		[Source.Parent<Module.Link>] ref Computer.Program.Data program) //, [HasRelation<Module.Link>(Source.Modifier.Owned, true)] bool has_module)
		{
			App.WriteLine($"on change card {info.EventType}");

			computer_state.instruction_count = 0;
			computer_state.ip = 0;
			computer_state.sp = 0;

			computer_state.flags.RemoveFlag(Computer.State.Flags.Active, info.EventType != ISystem.EventType.Add);

#if SERVER
			computer_state.Sync(entity);
#endif
		}

		[ISystem.Update.C(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void UpdateComputer(ISystem.Info info, Entity entity, ref XorRandom random, ref Region.Data region, [Source.Owned] in Transform.Data transform,
		[Source.Owned] ref Computer.Data computer, [Source.Owned] ref Computer.State computer_state, [Source.Parent<Module.Link>, Optional(true)] ref Computer.Program.Data program)
		{
			var time = info.WorldTime;
			if (time >= computer_state.t_next_cycle)
			{
				computer_state.t_last_cycle = time;

#if SERVER
				var sync = false;
#endif

				if (computer_state.flags.HasAny(Computer.State.Flags.Active) && computer_state.flags.HasNone(Computer.State.Flags.Stuck | Computer.State.Flags.Skip | State.Flags.Stopped))
				{
					//ref var program_data = ref computer.h_program.GetData();
					//if (program_data.IsNotNull())

					var instr = default(Computer.Instruction);

					if (program.IsNotNull())
					{
						var instructions_span = program.GetInstructionsSpan();

						instr = instructions_span.GetAtIndexOrDefault((uint)computer_state.ip);
						computer_state.instruction_count = program.instructions_capacity;
					}

					var instr_count = computer_state.instruction_count;
					if (instr_count > 0)
					{
						try
						{
							//var instructions_span = program_data.instructions.AsSpan();
							//computer_state.ip = (short)Maths.Clamp((int)computer_state.ip, 0, instructions_span.Length - 1);
							computer_state.sp = Maths.Min(computer_state.sp, (byte)(computer_state.stack.Length - 1));

							//var instr = instructions_span[computer_state.ip];

							ref var op = ref ops[(uint)instr.op];
							if (computer_state.flags.TryAddFlag(Computer.State.Flags.Busy))
							{
								var speed_inv = op.flags.HasAny(OperationInfo.Flags.Constant_Time) ? 1.00f : computer.speed.RcpFast();
								var delay = op.time * speed_inv;
#if CLIENT
								if (delay >= 0.01f)
								{
									Sound.Play(op.sound, transform.position, volume: op.volume * 1.40f, pitch: op.pitch * (op.flags.HasAny(OperationInfo.Flags.Constant_Pitch) ? (op.flags.HasAny(OperationInfo.Flags.Constant_Time) ? 1.00f : computer.speed) : 1.00f * random.NextFloatRange(0.98f, 1.02f)), priority: 0.20f, size: 10.00f, dist_multiplier: 0.58f);
								}
#endif

								computer_state.t_next_cycle = time + delay;
							}
							else
							{
								computer_state.flags.RemoveFlag(Computer.State.Flags.Busy);

								op.func(ref computer_state, instr.arg);

								if (computer_state.ip >= instr_count)
								{
									computer_state.flags.RemoveFlag(Computer.State.Flags.Active);
#if SERVER
									sync = true;
#endif
								}

								//computer_state.t_next_cycle = time + 0.10f;
							}

							computer_state.ip = Maths.Min(computer_state.ip, (byte)(instr_count - 1));
							computer_state.sp = Maths.Min(computer_state.sp, (byte)(computer_state.stack.Length - 1));

							//computer_state.ip = (short)Maths.Clamp((int)computer_state.ip, 0, instructions_span.Length - 1);
							//computer_state.sp = (sbyte)Maths.Clamp((int)computer_state.sp, 0, computer_state.stack.Length - 1);
						}
						catch
						{
							computer_state.flags.AddFlag(Computer.State.Flags.Error);
						}
					}
					else
					{
						computer_state.ip = 0;
						computer_state.t_next_cycle = time + 1.00f;
					}

					if (computer_state.flags.TryRemoveFlag(Computer.State.Flags.Error))
					{
#if SERVER
						if (computer.flags.HasAny(Computer.Data.Flags.Failsafe))
						{
							// TODO
						}
						else
						{
							Explosion.Spawn(ref region, transform.position, new Explosive.Data()
							{
								radius = 5.00f,
								power = 3.00f,
								smoke_amount = 0.70f,
								smoke_lifetime_multiplier = 1.50f,
								damage_type = Damage.Type.Explosion,
								damage_type_secondary = Damage.Type.Shrapnel,
								stun_multiplier = 1.40f,
								pitch = 1.40f,
								volume = 1.00f,
								damage_entity = 800.00f,
								damage_terrain = 1000.00f,
								force_multiplier = 0.80f,
								sparks_amount = 3.00f,
							});
							entity.Delete();
						}
#endif

						computer_state.flags.AddFlag(Computer.State.Flags.Stuck);
						computer_state.t_next_cycle = time + 1.00f;

#if SERVER
						sync = true;
#endif
					}
				}
				else
				{
					if (computer_state.flags.TryRemoveFlag(Computer.State.Flags.Skip))
					{
#if CLIENT
						Sound.Play("computer.clear.00", transform.position, volume: 0.50f * 1.40f, pitch: random.NextFloatRange(0.95f, 1.05f), priority: 0.30f, size: 0.10f, dist_multiplier: 1.70f);
						computer_state.current_shake = 0.00f;
#endif
						computer_state.t_next_cycle = time + 1.25f;
					}
					else
					{
						computer_state.t_next_cycle = time + 1.00f;
					}
				}

#if SERVER
				if (sync)
				{
					computer_state.Sync(entity, true);
				}
#endif
			}
		}

#if CLIENT
		[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void UpdateEffects(ISystem.Info info, Entity entity, ref Region.Data region, [Source.Owned] in Transform.Data transform,
		[Source.Owned] ref Computer.Data computer, [Source.Owned] ref Computer.State computer_state, [Source.Owned, Pair.Component<Computer.Data>] ref Sound.Emitter sound_emitter, [Source.Owned] ref Animated.Renderer.Data renderer)
		{
			var active = computer_state.flags.HasAny(Computer.State.Flags.Active);
			var stuck = computer_state.flags.HasAny(Computer.State.Flags.Stuck);
			sound_emitter.volume_mult = Maths.Lerp2(sound_emitter.volume_mult, (stuck && active) ? 1.30f : 0.00f, 0.02f, 0.20f);
			//sound_emitter.pitch = Maths.Lerp(sound_emitter.pitch, (stuck && active) ? 0.60f + (computer.speed * 0.40f) : 0.40f, 0.05f);
			sound_emitter.pitch_mult = Maths.Lerp(sound_emitter.pitch_mult, (stuck && active) ? 1.00f : 0.40f, 0.05f);

			ref var shake = ref computer_state.current_shake;
			shake = Maths.Lerp2(shake, active ? (stuck ? 0.60f : Maths.Clamp(computer_state.t_next_cycle - info.WorldTime, 0.00f, 1.00f)) : 0.00f, 0.02f, 0.40f);
			if (shake > Maths.epsilon)
			{
				renderer.offset = Vector2.Lerp(renderer.offset, new Vector2(Maths.Perlin(info.WorldTime, MathF.Sin(info.WorldTime * 10), shake * 0.50f, entity.GetShortID()) * 0.20f, 0.00f) * shake, shake * 0.40f);
				renderer.rotation = Maths.Lerp(renderer.rotation, MathF.Cos(info.WorldTime * 50.00f) * 0.05f * shake * shake, 0.80f);
			}

			ref var random = ref info.GetRandom();
			if (active && stuck && shake > 0.55f && random.NextBool(0.05f))
			{
				var modifier = 0.50f;

				Particle.Spawn(ref region, new Particle.Data()
				{
					texture = ArcaneInfuser.tex_smoke,
					lifetime = random.NextFloatRange(3.00f, 5.00f),
					pos = transform.position + random.NextVector2(0.50f),
					fps = random.NextByteRange(2, 5),
					frame_count = 64,
					frame_count_total = 64,
					frame_offset = random.NextByteRange(0, 64),
					scale = random.NextFloatRange(0.20f, 0.40f),
					rotation = random.NextFloat(2.00f),
					angular_velocity = random.NextFloat(1.00f),
					growth = random.NextFloatRange(0.25f, 0.40f),
					drag = random.NextFloatRange(0.02f, 0.04f),
					force = new Vector2(2.00f, random.NextFloatRange(0, -2)),
					color_a = random.NextColor32Range(0x40ffffff, 0xc0ffffff).WithAlphaMult(modifier),
					color_b = random.NextColor32Range(0x00ffffff, 0x00808080)
				});
			}
		}
#endif
	}
}
