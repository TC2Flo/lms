namespace TC2.Base.Components
{
	public static partial class Helichopper
	{
		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public partial struct Data: IComponent
		{
			public float derp;
		}

#if CLIENT
		public partial struct HelichopperGUI: IGUICommand
		{
			public Entity entity;
			public Helichopper.Data helichopper;
			public Transform.Data transform;

			public void Draw()
			{
				using (var window = GUI.Window.InteractionMisc("Helichopper"u8, this.entity, new Vector2(192, 96), no_mouse_close: true))
				{
					//this.StoreCurrentWindowTypeID(order: -1000);
					if (window.show)
					{
						ref var region = ref this.entity.GetRegion();

						using (GUI.Group.New(size: GUI.Rm, padding: new(2, 2)))
						{
							////GUI.Text($"{this.helichopper.altitude:0.00} m {this.helichopper.lift_modifier:P2}");

							//GUI.DrawTemperatureRange(this.burner_state.current_temperature, this.burner_state.current_temperature, 1000, size: new Vector2(24, GUI.RmY));
							//GUI.SameLine();
							//GUI.DrawVerticalGauge(this.helichopper.current_lift, this.helichopper.target_lift, this.helichopper.lift_max, size: new Vector2(24, GUI.RmY), color_a: GUI.col_button_yellow, color_b: GUI.col_button_yellow);
							//GUI.DrawHoverTooltip($"Lift:\n{this.helichopper.current_lift:0}/{this.helichopper.lift_max:0}");
							//GUI.SameLine();
							//GUI.DrawVerticalGauge(this.helichopper.altitude, this.helichopper.altitude, 300.00f, size: new Vector2(24, GUI.RmY));
							//GUI.DrawHoverTooltip($"Altitude:\n{this.helichopper.altitude:0}/{300.00f:0}m");

							////GUI.DrawTemperatureRange(this.burner_state.current_temperature, this.burner_state.current_temperature, 1000, size: new Vector2(24, GUI.RmY));
							////GUI.SameLine();
							////GUI.DrawWorkV(0.50f, size: new Vector2(24, GUI.RmY));

							////GUI.DrawGauge(this.burner_state.modifier, 0.00f, 1.00f);

							//GUI.SameLine();


							//using (GUI.Group.New(new(GUI.RmX, 48)))
							//{
							//	GUI.DrawInventoryDock(Inventory.Type.Fuel, new(48, 48));

							//}
						}
					}
				}
			}
		}

		[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnGUI([Source.Owned] in Interactable.Data interactable, 
		Entity ent_helichopper, [Source.Owned] in Helichopper.Data helichopper, [Source.Owned] in Transform.Data transform)
		{
			if (interactable.IsActive())
			{
				var gui = new HelichopperGUI()
				{
					entity = ent_helichopper,
					helichopper = helichopper,
					transform = transform,
				};
				gui.Submit();
			}
		}
#endif

		[ISystem.VeryLateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnUpdate(ISystem.Info info, ref Region.Data region, Entity entity,
		[Source.Owned] ref Transform.Data transform, [Source.Owned] in Control.Data control, [Source.Owned] ref Body.Data body,
		[Source.Owned] ref Helichopper.Data helichopper, [Source.Owned] in Health.Data health)
		{

#if SERVER
			if (control.keyboard.GetKeyDown(Keyboard.Key.Spacebar))
			{
				transform.scale.X *= -1.00f;
				transform.Modified(entity, true);
			}
#endif

		}
	}
}
