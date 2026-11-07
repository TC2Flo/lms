namespace TC2.Base.Components
{
	public static class Grapple
	{
		[IComponent.Data(Net.SendType.Unreliable, region_only: true)]
		public struct Data(): IComponent
		{
			public Vector2 offset;
			public float max_length = 20.00f;
		}

		[ISystem.VeryLateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void UpdateRope(ISystem.Info info, [Source.Parent] ref Grapple.Data grapple, [Source.Owned] ref Rope.Renderer.Data rope, [Source.Parent] in Transform.Data transform_parent, [Source.Owned] in Transform.Data transform_child)
		{
			rope.p0 = transform_child.LocalToWorld(new Vector2(-0.750f, 0.000f));
			rope.p3 = transform_parent.LocalToWorld(grapple.offset);

			var dist = Vector2.Distance(rope.p0, rope.p3);

			rope.p1 = rope.p0 + (-transform_child.GetDirection() * (1 - (dist * 0.10f))) + new Vector2(0, dist * 0.25f);
			rope.p2 = Vector2.Lerp(rope.p0, rope.p3, 0.80f) + new Vector2(0, dist * 0.10f);

			var len = Vector2.Distance(rope.p0, rope.p1) + Vector2.Distance(rope.p1, rope.p2) + Vector2.Distance(rope.p2, rope.p3);
			rope.scale = len * 1.25f;
		}

		[ISystem.RemoveLast(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void RemoveRope(ISystem.Info info, [Source.Parent] ref Grapple.Data grapple, [Source.Owned] ref Rope.Renderer.Data rope, [Source.Parent] in Transform.Data transform_parent, [Source.Owned] in Transform.Data transform_child)
		{
			rope.p0 = Vector2.Zero;
			rope.p3 = Vector2.Zero;
			rope.p1 = Vector2.Zero;
			rope.p2 = Vector2.Zero;
		}

		[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void Update(ISystem.Info info, Entity entity, [Source.Owned] ref Grapple.Data grapple, [Source.Owned] in Transform.Data transform, [Source.Owned] in Control.Data control, [Source.Owned] ref Body.Data body, [Source.Owned] ref Joint.Base joint_base, [Source.Owned, Original] ref Joint.Rope2 joint)
		{
#if SERVER
			if (control.mouse.GetKey(Mouse.Key.Right))
			{
				joint.distance = Maths.Clamp(joint.distance - 0.25f, 0.20f, grapple.max_length);
				joint.Sync(entity);
			}

			if (control.keyboard.GetKeyDown(Keyboard.Key.Reload))
			{
				joint.distance = grapple.max_length;
				joint.Sync(entity);

				var ent_child = entity.GetChild(Relation.Type.Rope);
				if (ent_child.IsAlive())
				{
					ent_child.RemoveRelation(entity, Relation.Type.Rope);
				}
			}
#endif
		}

		[ISystem.VeryLateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void UpdateSound(ISystem.Info info, Entity entity,
		[Source.Owned] in Control.Data control, [Source.Owned] in Grapple.Data grapple, [Source.Owned] ref Sound.Mixer sound_mix)
		{
			sound_mix.modifier = control.mouse.GetKey(Mouse.Key.Right) ? 1.00f : 0.00f;
		}
	}
}
