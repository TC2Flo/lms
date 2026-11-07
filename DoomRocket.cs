
namespace TC2.Base.Components
{
	public static partial class DoomRocket
	{
		[IComponent.Data(Net.SendType.Reliable, IComponent.Scope.Region)]
		public partial struct Data(): IComponent
		{
			public float speed_step = 0.50f;
			public float speed;
		}

		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Region)]
		public partial struct State: IComponent
		{
			[Save.Ignore, Net.Ignore]
			public float current_motor_speed;

			[Save.Ignore, Net.Ignore]
			public float current_motor_force;

			[Save.Ignore, Net.Ignore]
			public float target_motor_speed;
		}

		[ISystem.EarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void UpdateControls(ISystem.Info info, Entity entity, [Source.Owned] ref Transform.Data transform, 
		[Source.Owned] ref DoomRocket.Data doom_rocket, [Source.Owned] ref DoomRocket.State doom_rocket_state, [Source.Owned] in Control.Data control)
		{
			var speed = 0.00f;

			if (control.keyboard.GetKey(Keyboard.Key.MoveRight)) speed -= doom_rocket.speed;
			if (control.keyboard.GetKey(Keyboard.Key.MoveLeft)) speed += doom_rocket.speed;

			//if (control.keyboard.GetKeyDown(Keyboard.Key.Q)) doom_rocket.gear = Maths.Clamp(doom_rocket.gear - 1, 0, 4);
			//if (control.keyboard.GetKeyDown(Keyboard.Key.E)) doom_rocket.gear = Maths.Clamp(doom_rocket.gear + 1, 0, 4);

			var ratio = 1.00f; // * MathF.CopySign(1.00f, transform.scale.X); // + (doom_rocket.gear * doom_rocket.gear_mod);
			if (control.keyboard.GetKey(Keyboard.Key.LeftShift)) speed *= 2.00f;

			doom_rocket_state.target_motor_speed = (speed != 0.00f ? speed : 0.00f) * ratio;
			//doom_rocket_state.target_motor_force = (speed != 0.00f ? doom_rocket.force : doom_rocket.brake) / ratio;
		}

#if CLIENT
		public struct DoomRocketGUI: IGUICommand
		{
			public Entity ent_doom_rocket;

			public Transform.Data transform;

			public void Draw()
			{
				using (var window = GUI.Window.Interaction("Rocket"u8, this.ent_doom_rocket))
				{
					this.StoreCurrentWindowTypeID(order: -100);
					if (window.show)
					{
						ref var player = ref Client.GetPlayer();
						ref var region = ref Client.GetRegion();

						using (GUI.Group.New(size: new Vector2(GUI.RmX, GUI.RmY)))
						{

						}
					}
				}
			}
		}

		[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void OnGUI(Entity entity, [Source.Owned] in Transform.Data transform, 
		[Source.Owned] in DoomRocket.Data doom_rocket, [Source.Owned] in Interactable.Data interactable)
		{
			if (interactable.IsActive())
			{
				var gui = new DoomRocketGUI()
				{
					ent_doom_rocket = entity,

					transform = transform
				};
				gui.Submit();
			}
		}
#endif
	}
}
