
namespace TC2.Base.Components
{
	public static partial class Clock
	{
		[IComponent.Data(Net.SendType.Reliable, region_only: true)]
		public struct Data: IComponent
		{
			public float time_offset;
			[Editor.Picker.Position(true)]
			public Vector2 offset;
		}

#if CLIENT

		public static Texture.Handle tex_blank = "blank";

		[ISystem.Render(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void ClockRender([Source.Owned] in Transform.Data transform, [Source.Owned] ref Clock.Data clock,
		[Source.Owned] in Animated.Renderer.Data renderer, [Source.Global] in World.Global world_global)
		{
			if (Camera.IsVisible(Camera.CullType.Rect1x_Padded, transform.position))
			{

				//var time_of_day = world_global.time_cached + clock.time_offset;
				//var time_of_day_seconds = time_of_day * 24.00f * 60.00f * 60.00f;

				//var minutes = MathF.Floor((time_of_day_seconds / 60.00f) % 60.00f);
				//var hours = MathF.Floor(time_of_day_seconds / 60.00f) / 60.00f;

				var hours = (float)world_global.date.Hour;
				var minutes = (float)world_global.date.Minute;

				{
					var hand_length = 0.50f;
					var rot = -MathF.Tau * Maths.Normalize01(hours % 12.00f, 12.00f) + (MathF.PI * 0.50f);
					var (sin, cos) = MathF.SinCos(rot);

					Animated.Renderer.Draw(transform, new()
					{
						sprite = new Sprite(tex_blank, 8, 1, 0, 0),
						z = renderer.z + 0.01f,
						scale = new Vector2(hand_length, 1.00f),
						offset = clock.offset + (new Vector2(cos, -sin) * hand_length * 0.50f),
						rotation = rot,
						color = new Color32BGRA(0xff2a2817)
					});
				}

				{
					var hand_length = 0.75f;
					var rot = (-MathF.Tau * Maths.Normalize01(minutes, 60.00f)) + (MathF.PI * 0.50f);
					var (sin, cos) = MathF.SinCos(rot);

					Animated.Renderer.Draw(transform, new()
					{
						sprite = new Sprite(tex_blank, 8, 1, 0, 0),
						z = renderer.z + 0.01f,
						scale = new Vector2(hand_length, 1.00f),
						offset = clock.offset + (new Vector2(cos, -sin) * hand_length * 0.50f),
						rotation = rot,
						color = new Color32BGRA(0xff2a2817)
					});
				}

				//if (sound_emitter.IsNotNull())
				//{
				//	sound_emitter.pitch = 1.00f;
				//}
			}
		}
#endif
	}
}
