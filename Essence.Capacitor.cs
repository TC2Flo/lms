
namespace TC2.Base.Components
{
	public static partial class Essence
	{
		public static partial class Capacitor
		{
			public enum Type: byte
			{
				Undefined = 0,
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

			[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Global | IComponent.Scope.Region)]
			public partial struct Data(): IComponent
			{
				[Net.Segment.A, Save.Force, Editor.Picker.Position(relative: true)] public required Vec2f offset;
				[Net.Segment.A, Save.Force] public required Vec2f size;
				[Net.Segment.A, Save.Force, Editor.Picker.Direction(normalize: true)] public required Vec2f direction = Vec2f.Down;

				[Save.NewLine]
				[Net.Segment.A, Save.Force] public required Essence.Capacitor.Type type;
				[Net.Segment.A] private byte unused_a_00;
				[Net.Segment.A, Save.Force] public Essence.Capacitor.Flags flags;
				[Net.Segment.A, Save.Force] public Essence.Capacitor.Tags tags;
				[Net.Segment.A, Save.Force, Editor.Slider.Clamped(0.00f, 10000.00f, snap: 1.00f)] public required float charge_capacity;
				[Net.Segment.A, Save.Force, Editor.Slider.Clamped(0.00f, 1.00f, snap: 0.001f)] public required float charge_leakage = 0.03f;
				[Net.Segment.A, Save.Force, Editor.Slider.Clamped(0.00f, 1.00f, snap: 0.001f)] public required float charge_efficiency;
				[Net.Segment.A, Save.Force, Editor.Slider.Clamped(0.00f, 1.00f, snap: 0.001f)] public required float discharge_rate;

				[Net.Segment.A, Save.Force, Editor.Slider.Clamped(0.00f, 1.00f, snap: 0.001f)] public required Percentage containment = Percentage.Max;
				[Net.Segment.A, Save.Force, Editor.Slider.Clamped(0.00f, 1.00f, snap: 0.001f)] public required Percentage stability = Percentage.Max;

				[Save.NewLine]
				[Net.Segment.B, Save.Force, Asset.Ignore] public required IEssence.Handle h_essence;
				[Net.Segment.B, Save.Force] public required ISoundMix.Handle h_soundmix_pulse;
				[Net.Segment.B] public ISoundMix.Handle h_soundmix_discharge;
				[Net.Segment.B] public ISoundMix.Handle h_soundmix_unused_01;

				[Save.NewLine]
				[Net.Segment.C] public Essence.Capacitor.StateFlags state_flags;
				//[Net.Segment.C, Asset.Ignore] public IEssence.Handle h_essence_charge;
				//[Net.Segment.C] public Signal.Channel channel_emit;
				//[Net.Segment.C] private byte unused_c_02;
				//[Net.Segment.C] private ushort unused_c_03;

				[Save.NewLine]
				[Net.Segment.D, Asset.Ignore] public float current_charge_add;
				[Net.Segment.D, Asset.Ignore] public float current_charge_loss;
				[Net.Segment.D, Asset.Ignore] public float current_charge;
				[Net.Segment.D, Asset.Ignore] public float current_charge_ratio;
				[Net.Segment.D, Asset.Ignore] public float current_discharge;
				[Net.Segment.D, Asset.Ignore] public float current_instability;
	



				//[Save.Force, Editor.Picker.Position(relative: true)] public required Vec2f offset;
				//[Save.Force, Editor.Picker.Direction(normalize: true)] public required Vec2f direction = Vec2f.Up;

				//[Editor.Slider.Clamped(min: 0.00f, max: 16.00f, snap: 0.001f)]
				//[Save.Force] public required float radius;
				//[Save.Force] public required float capacity;
				//[Save.Force] public float efficiency = 1.00f;

				//[Save.Force] public Essence.Capacitor.Flags flags;
				//public ushort reserved_00;
				//public uint reserved_01;
			}

			[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region)]
			public static void OnUpdate_A(ISystem.Info info, ref XorRandom random, ref Region.Data region, Entity entity,
			[Source.Owned] in Transform.Data transform, [Source.Owned] ref Essence.Capacitor.Data capacitor,
			[Source.Owned] in Control.Data control)
			{

			}

			[ISystem.Update.B(ISystem.Mode.Single, ISystem.Scope.Region)]
			public static void OnUpdate_B(ISystem.Info info, ref XorRandom random, ref Region.Data region, Entity entity,
			[Source.Owned] ref Transform.Data transform,
			[Source.Owned] ref Essence.Capacitor.Data capacitor)
			{

			}

			[ISystem.Update.C(ISystem.Mode.Single, ISystem.Scope.Region)]
			public static void OnUpdate_C(ISystem.Info info, ref XorRandom random, ref Region.Data region, Entity entity,
			[Source.Owned] ref Transform.Data transform,
			[Source.Owned] ref Essence.Capacitor.Data capacitor,
			[Source.Owned, Pair.Component<Essence.Capacitor.Data>] ref Essence.Charge.Data charge)
			{

			}

			[ISystem.PostUpdate.D(ISystem.Mode.Single, ISystem.Scope.Region)]
			public static void OnPostUpdate_Charge([Source.Owned] ref Essence.Container.Data essence_container,
			[Source.Owned] ref Essence.Capacitor.Data essence_capacitor)
			{
				essence_capacitor.h_essence = essence_container.h_essence;
				essence_capacitor.current_charge_ratio = Maths.NormalizeFast(essence_capacitor.current_charge, essence_capacitor.charge_capacity);

				essence_capacitor.current_instability = Maths.Max((essence_capacitor.current_charge_ratio) -
					(essence_container.stability * essence_container.h_essence.GetStabilityMult()), 0.00f).Pow2();

				var current_charge = essence_capacitor.current_charge;
				//current_charge -= current_charge * essence_capacitor.charge_leakage;

				var rate = essence_container.rate_current * essence_container.h_essence.GetDischargeMult();

				var amount_loss = current_charge * essence_capacitor.charge_leakage;
				var amount_add = (rate * essence_container.available) * essence_capacitor.charge_efficiency * App.fixed_update_interval_s_f32;

				//current_charge *= (1.00f - essence_capacitor.charge_leakage);

				current_charge -= amount_loss;
				current_charge += amount_add;

				essence_capacitor.current_charge_add = amount_add;
				essence_capacitor.current_charge_loss = amount_loss;

				essence_capacitor.current_charge = current_charge;
			}


#if CLIENT
			[ISystem.Render(ISystem.Mode.Single, ISystem.Scope.Region)]
			public static void OnRender_A(ISystem.Info info, ref XorRandom random, ref Region.Data region, Entity entity,
			[Source.Owned] in Transform.Data transform,
			[Source.Owned] ref Essence.Capacitor.Data capacitor,
			[Source.Owned, Pair.Component<Essence.Capacitor.Data>, Optional(true)] ref Essence.Charge.Data charge,
			[Source.Owned, Pair.Component<Essence.Capacitor.Data>, Optional(true)] ref Light.Data light)
			{
				if (light.IsNotNull())
				{
					light.color = capacitor.h_essence.GetEmitColor();
					light.intensity = (capacitor.current_charge_ratio * 2.00f).Sqrt();
				}
			}
#endif

#if CLIENT
			public struct CapacitorGUI: IGUICommand
			{
				public Entity ent_capacitor;
				public Transform.Data transform;
				public Essence.Capacitor.Data capacitor;

				public void Draw()
				{
					using (var window = GUI.Window.Interaction("Capacitor"u8, this.ent_capacitor))
					{
						this.StoreCurrentWindowTypeID(order: -100);
						if (window.show)
						{
							using (GUI.Group.New(size: GUI.Rm))
							{
								using (GUI.Group.New(size: new(GUI.RmX - 128, 0), padding: new(0)))
								{
									GUI.DrawHorizontalGauge(this.capacitor.current_charge, this.capacitor.charge_capacity, size: new(GUI.RmX, 24));

									GUI.LabelShaded("Total:"u8, this.capacitor.current_charge, format: "0.00' Ef'", width: GUI.RmX * 0.75f);
									GUI.LabelShaded("Net:"u8, this.capacitor.current_charge_add - this.capacitor.current_charge_loss, format: "0.00' Ef'", width: GUI.RmX * 0.75f);
									GUI.LabelShaded("In:"u8, this.capacitor.current_charge_add, format: "0.00' Ef'", width: GUI.RmX * 0.75f);
									GUI.LabelShaded("Loss:"u8, this.capacitor.current_charge_loss, format: "0.00' Ef'", width: GUI.RmX * 0.75f);

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
			[Source.Owned] in Essence.Capacitor.Data capacitor)
			{
				if (interactable.IsActive())
				{
					var gui = new CapacitorGUI()
					{
						ent_capacitor = entity,
						transform = transform,
						capacitor = capacitor,
					};
					gui.Submit();
				}
			}
#endif
		}
	}
}