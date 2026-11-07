
namespace TC2.Base.Components
{
	public static partial class Storable
	{
		[ISystem.AddFirst(ISystem.Mode.Single, ISystem.Scope.Region), HasRelation(Source.Modifier.Owned, Relation.Type.Stored, true)]
		public static void OnAdd_Stored(ISystem.Info info, ref Region.Data region, Entity entity, [Source.Owned] ref Body.Data body)
		{
			//App.WriteLine($"add store {entity}", App.Color.Magenta);

			body.override_shape_layer.AddFlag(Physics.Layer.Stored);
			//region.body_disable_queue.Enqueue((IntPtr)body.ptr);
			body.Disable(force: true);

#if SERVER
			var ent_parent = entity.GetParent(Relation.Type.Child);
			if (ent_parent.IsValid())
			{
				entity.RemoveRelation(ent_parent, Relation.Type.Child);
			}
#endif
		}

		[ISystem.AddFirst(ISystem.Mode.Single, ISystem.Scope.Region), HasRelation(Source.Modifier.Parent, Relation.Type.Stored, true)]
		public static void OnAdd_Stored_Parented(ISystem.Info info, ref Region.Data region, Entity ent_body, //Entity ent_joint_parent,
		[Source.Owned] ref Body.Data body/*, [Source.Parent] in Joint.Base joint_parent*/)
		{

			//var ent_parent = body.GetParent(); //.GetParent(Relation.Type.Stored);
			//App.WriteLine($"{ent_stored.GetFullName()}:{ent_stored}; {info.GetSources()[info.TermIndex]}; term_index: {info.TermIndex}; {info.GetEventSource()}; {info.GetSources().Length}; {info.EventID}; {info.Event}; {info.EventType}; {ent_body.GetFullName()}:{ent_body} | {ent_joint_parent.GetFullName()}:{ent_joint_parent} | {joint_parent.ent_attached.GetFullName()}:{joint_parent.ent_attached} | {ent_parent.GetFullName()}:{ent_parent}");
			//App.WriteLine($"{info.GetEventSource().GetFullName()}:{info.GetEventSource()} == {ent_parent.GetFullName()}:{ent_parent}; {info.EventID}; {info.Event}; {info.EventType}; {ent_body.GetFullName()}:{ent_body} | {ent_joint_parent.GetFullName()}:{ent_joint_parent} | {joint_parent.ent_attached.GetFullName()}:{joint_parent.ent_attached}");
			//info.SetInterrupted(ent_body);

			//if ()
			//if (body.ptr != null && body.GetParent())
#if SERVER
			var ent_parent = body.GetParent(); //.GetParent(Relation.Type.Stored);
			if (info.GetEventSource() == ent_parent)
			{
				ent_body.RemoveRelation(Entity.Wildcard, Relation.Type.Child);
			}
				//ent_body.RemoveRelation(Entity.Wildcard, Relation.Type.Child);
#endif
		}

	}
}
