using Keg.Extensions;
using TC2.Base.Components;

namespace TC2.Siege
{
	[Asset.Hjson(prefix: "siege.", capacity_world: 64, capacity_region: 0, capacity_local: 4)]
	public partial interface ISiege: IAsset2<ISiege, ISiege.Data>
	{
		//static void IAsset2<IScenario, IScenario.Data>.OnUpdate(IScenario.Definition definition, ref IScenario.Data data_new)
		//{

		//}

		//static void IAsset2<IScenario, IScenario.Data>.OnInit(out string prefix, out string[] extensions, out int capacity_world, out int capacity_region, out int capacity_local)
		//{
		//	prefix = "scenario.";
		//	extensions = new string[]
		//	{
		//		".hjson"
		//	};

		//	capacity_world = 64;
		//	capacity_region = 0;
		//	capacity_local = 0;
		//}

		[Net.MsgPack]
		public partial struct Data()
		{
			[Flags]
			public enum Flags: uint
			{
				None = 0u,


			}

			[Net.Key(02), Save.Force] public ISiege.Data.Flags flags;
			[Net.Key(03), Save.Force] public float difficulty_mult = 1.00f;

			[Save.NewLine]
			[Net.Key(07), Save.Force] public float defender_squad_money_base;
			[Net.Key(08), Save.Force] public float attacker_squad_money_base;

			[Save.NewLine]
			[Net.Key(16)] public ISiege.Wave[] waves;
		}

		//[Net.MsgPack]
		//public partial struct ()
		//{
		//	[Net.Key(00)] public ISiege.Wave[] waves;
		//}

		[Net.MsgPack]
		public partial struct Wave()
		{
			public enum Type: byte
			{
				Undefined = 0,

				Single,
				Recurrent
			}

			[Flags]
			public enum Flags: ushort
			{
				None = 0,


			}

			[Net.Key(00)] public string name;

			[Net.Key(02), Save.Force] public Wave.Type type;
			[Net.Key(03), Save.Force] public Wave.Flags flags;

			[Save.NewLine]
			[Net.Key(06), Save.Force] public uint period;
			[Net.Key(07), Save.Force] public int priority;
			[Net.Key(08), Save.Force] public float weight;
			[Net.Key(09), Save.Force] public float duration;

			[Save.NewLine]
			[Net.Key(12), Save.Force] public Limit.MinMaxWeighted difficulty_range;

			//[Save.NewLine]
			//[Net.Key(10)] public 

			[Save.NewLine]
			[Net.Key(16)] public Sound.Handle music;
			[Net.Key(17)] public float music_volume = 0.40f;
			[Net.Key(18)] public float music_pitch = 1.00f;
			[Net.Key(19)] public float music_delay = 5.00f;
		}
	}
}
