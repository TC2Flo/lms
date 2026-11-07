
namespace TC2.Base.Components
{
	public static partial class Hopper
	{
		[IComponent.Data(Net.SendType.Reliable, region_only: true)]
		public partial struct Data: IComponent
		{
			[Editor.Picker.Position(relative: true, mark_modified: true)]
			public Vector2 offset;
			public Sound.Handle sound_load;
		}

#if SERVER
		[ISystem.VeryLateUpdate(ISystem.Mode.Single, ISystem.Scope.Region, interval: 0.12f)]
		public static void OnUpdate(ISystem.Info info, Entity entity, ref Region.Data region,
		[Source.Owned] ref Body.Data body, [Source.Owned] in Transform.Data transform,
		[Source.Owned] ref Hopper.Data hopper,
		[Source.Owned, Pair.Component<Hopper.Data>] ref Inventory8.Data inventory)
		{
			if (body.HasArbiters())
			{
				var axis = transform.GetDirection().GetPerpendicular(float.IsNegative(transform.scale.GetParity()));
				//region.DrawDebugDir(transform.position, axis * 10, Color32BGRA.Green);

				foreach (var arbiter in body.GetArbiters())
				{
					//if (arbiter.GetParent().id == 0ul)

					//if (arbiter.GetState() == Body.Arbiter.State.Begin && arbiter.GetLayer().HasAny(Physics.Layer.Resource))
					if (arbiter.GetLayer().HasAny(Physics.Layer.Resource))
					{
						var ent_resource = arbiter.GetEntity();
						var normal = arbiter.GetNormal();
						var dot = Vector2.Dot(normal, axis);

						if (dot >= 0.95f)
						{

							//var dot = Vector2.Dot(normal, axis);
							//App.WriteLine(axis);

							//App.WriteLine(arbiter.GetNormal());

							ref var resource = ref ent_resource.GetComponent<Resource.Data>();
							if (!resource.IsNull())
							{
								if (Resource.Insert(ref inventory, ref resource, resource.quantity, null))
								{
									Sound.Play(ref region, hopper.sound_load, transform.position);

									//arbiter.SetIgnored();
								}

								break;
							}
						}
						//App.WriteLine($"{arbiter.GetEntity()}");
					}
				}
			}
		}
#endif
	}
}
