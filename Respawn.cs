
namespace TC2.Base.Components
{
	public static partial class RespawnExt
	{
		public struct SetSpawnRPC: Net.IRPC<Respawn.Data>
		{
			public Entity ent_spawn;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Respawn.Data respawn)
			{
				//var span_keys = respawn.spawns_keys.AsSpan();

				//var index = span_keys.IndexOfOrEmpty(this.ent_spawn);
				//if (index >= 0)
				//{
				//	span_keys[index] = this.ent_spawn;
				//}

				//App.WriteLine($"setting spawn from {respawn.ent_selected_spawn} to {this.ent_spawn}");
				if (respawn.ent_selected_spawn.TrySet(this.ent_spawn))
				{
					respawn.Sync(rpc.entity);
				}
			}
#endif
		}
	}
}
