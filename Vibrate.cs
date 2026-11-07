
using Keg.Engine.Game;

namespace TC2.Base.Components
{
	public static partial class Vibrate
	{
		[IComponent.Data(Net.SendType.Unreliable, region_only: true)]
		public partial struct Data: IComponent
		{
			[Flags]
			public enum Flags: uint
			{
				None = 0u,

				Shared = 1u << 0
			}

			public float frequency = 50.00f;
			public float modifier = 1.00f;
			public float rotation_mult = 0.05f;
			public Vibrate.Data.Flags flags;

			public Vector2 position_mult = new(0.02f, 0.02f);

			[Net.Ignore, Save.Ignore] public Vector2 current_offset_pos;
			[Net.Ignore, Save.Ignore] public float seed;
			[Net.Ignore, Save.Ignore] public float current_offset_rot;
			[Net.Ignore, Save.Ignore] private float unused_00;
			[Net.Ignore, Save.Ignore] private float unused_01;

			public Data()
			{
				var random = XorRandom.New(true);
				this.seed = random.NextFloatRange(-MathF.PI, MathF.PI);
			}
		}

#if CLIENT
		[ISystem.VeryEarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region), HasTag("static", true, Source.Modifier.Owned)]
		public static void OnUpdateStatic(ISystem.Info info, Entity entity,
		[Source.Owned] ref Transform.Data transform, [Source.Owned] ref Vibrate.Data vibrate)
		{
			if (vibrate.modifier > 0.00f && Camera.IsVisible(Camera.CullType.Rect1x_Padded, transform.position))
			{
				var (sin, cos) = MathF.SinCos((info.WorldTime + vibrate.seed) * vibrate.frequency);
				sin = MathF.FusedMultiplyAdd(sin, 0.50f, 0.50f);
				cos = MathF.FusedMultiplyAdd(cos, 0.50f, 0.50f);

				ref var offset_pos = ref vibrate.current_offset_pos;
				offset_pos = new Vector2(cos, sin) * vibrate.position_mult * vibrate.modifier;

				ref var offset_rot = ref vibrate.current_offset_rot;
				offset_rot = (cos * vibrate.rotation_mult) * vibrate.modifier;

				ref var pos = ref transform.GetPositionOldRef();
				pos = transform.position + offset_pos;

				ref var rot = ref transform.GetRotationOldRef();
				rot = transform.rotation + offset_rot;
			}
		}

		[ISystem.VeryLateUpdate(ISystem.Mode.Single, ISystem.Scope.Region, order: 1000), HasTag("static", false, Source.Modifier.Owned)]
		public static void OnUpdateDynamic(ISystem.Info info, Entity entity,
		[Source.Owned] ref Transform.Data transform, [Source.Owned] ref Vibrate.Data vibrate)
		{
			if (vibrate.modifier > 0.00f && Camera.IsVisible(Camera.CullType.Rect2x, transform.position))
			{
				var (sin, cos) = MathF.SinCos((info.WorldTime + vibrate.seed) * vibrate.frequency);
				sin = MathF.FusedMultiplyAdd(sin, 0.50f, 0.50f);
				cos = MathF.FusedMultiplyAdd(cos, 0.50f, 0.50f);

				ref var offset_pos = ref vibrate.current_offset_pos;
				offset_pos = new Vector2(cos, sin) * vibrate.position_mult * vibrate.modifier;

				ref var offset_rot = ref vibrate.current_offset_rot;
				offset_rot = (cos * vibrate.rotation_mult) * vibrate.modifier;

				ref var pos = ref transform.GetPositionOldRef();
				pos += offset_pos;

				ref var rot = ref transform.GetRotationOldRef();
				rot += offset_rot;
			}
		}

		[ISystem.VeryLateUpdate(ISystem.Mode.Single, ISystem.Scope.Region, order: 1080), HasTag("static", false, Source.Modifier.Parent)]
		public static void OnUpdateDynamicJointsA(ISystem.Info info, Entity entity,
		[Source.Parent] ref Transform.Data transform, [Source.Owned] ref Vibrate.Data vibrate)
		{
			if (vibrate.flags.HasAny(Vibrate.Data.Flags.Shared) && vibrate.modifier > 0.00f && Camera.IsVisible(Camera.CullType.Rect1x_Padded, transform.position))
			{
				ref var pos = ref transform.GetPositionOldRef();
				pos += vibrate.current_offset_pos;

				ref var rot = ref transform.GetRotationOldRef();
				rot += vibrate.current_offset_rot;
			}
		}

		[ISystem.VeryLateUpdate(ISystem.Mode.Single, ISystem.Scope.Region, order: 1100), HasTag("static", false, Source.Modifier.Owned)]
		public static void OnUpdateDynamicJointsB(ISystem.Info info, Entity entity,
		[Source.Owned] ref Transform.Data transform, [Source.Parent] ref Vibrate.Data vibrate)
		{
			if (vibrate.modifier > 0.00f && Camera.IsVisible(Camera.CullType.Rect1x_Padded, transform.position))
			{
				ref var pos = ref transform.GetPositionOldRef();
				pos += vibrate.current_offset_pos;

				ref var rot = ref transform.GetRotationOldRef();
				rot += vibrate.current_offset_rot;
			}
		}

		[ISystem.VeryLateUpdate(ISystem.Mode.Single, ISystem.Scope.Region, order: 1200), HasTag("static", false, Source.Modifier.Owned), HasTag("local", true, Source.Modifier.Parent)]
		public static void OnUpdateCamera(ISystem.Info info, ref Region.Data region, Entity entity,
		[Source.Parent] in Arm.Data arm, [Source.Owned] in Transform.Data transform, [Source.Owned] ref Vibrate.Data vibrate)
		{
			if (vibrate.flags.HasAny(Vibrate.Data.Flags.Shared) && vibrate.modifier > 0.10f && vibrate.frequency >= 30.00f)
			{
				var shake = (vibrate.position_mult.GetSum() + vibrate.rotation_mult) * vibrate.modifier * 1.25f;
				Shake.Emit(ref region, transform.position, trauma: shake, max: shake, radius: 500);
			}
		}
#endif
	}
}