
namespace TC2.Base.Components
{
	//[Asset.Hjson(prefix: "company.", capacity_world: 512, capacity_region: 64, capacity_local: 0)]
	//public interface ICompany: IAsset2<ICompany, ICompany.Data>
	//{
	//	static void IAsset2<ICompany, ICompany.Data>.GenerateSample(out ICompany.Data data)
	//	{
	//		data = new();
	//		data.name = "Sample";
	//		data.desc = "Sample asset.";
	//	}

	//	public enum Kind: byte
	//	{
	//		[Name("")]
	//		Undefined = 0,

	//		Proprietorship,
	//		Cooperative,
	//		Corporation,
	//		Organization,
	//		Partnership,
	//		Gang,
	//		Cartel,
	//		Combine,
	//		Conglomerate,
	//		Institute,
	//		Guild,
	//	}

	//	[Flags]
	//	public enum Flags: uint
	//	{
	//		None = 0,

	//		Franchise = 1u << 0, 
	//		Limited_Liability = 1u << 1,
	//		Public = 1u << 2,
	//		Private = 1u << 3,
	//		State_Owned = 1u << 4,
	//		Subsidiary = 1u << 5,
	//		Criminal = 1u << 6,
	//		Stocks = 1u << 7,
	//		Local = 1u << 8,
	//		Provincial = 1u << 9,
	//		Imperial = 1u << 10,
	//		Holding = 1u << 11,
	//	}

	//	[Serializable]
	//	public struct Data
	//	{
	//		[Save.Force]
	//		public string name;
	//		[Save.Force]
	//		public string name_short;
	//		[Save.Force]
	//		public string desc_short;
	//		[Save.Force, Save.MultiLine]
	//		public string desc;
	//		[Save.Force]
	//		public string slogan;

	//		[Save.Force, Save.NewLine]
	//		public ICompany.Kind kind;
	//		[Save.Force]
	//		public ICompany.Flags flags;
	//		[Save.Force]
	//		public IFaction.Handle faction;
	//		[Save.Force]
	//		public uint employee_count;
	//		[Save.Force]
	//		public uint level;
	//		[Save.Force]
	//		public float prestige;
	//		[Save.Force]
	//		public float power_military;
	//		[Save.Force]
	//		public float power_economy;
	//		[Save.Force]
	//		public float power_political;

	//		[Save.Force, Save.NewLine]
	//		public IMap.Industry industry_primary;
	//		[Save.Force]
	//		public IMap.Industry industry_secondary;
	//		[Save.Force]
	//		public IMap.Goods goods_produced;
	//		[Save.Force]
	//		public IMap.Goods goods_consumed;
	//		[Save.Force]
	//		public IMap.Services services_produced;
	//		[Save.Force]
	//		public IMap.Services services_consumed;
	//		[Save.Force]
	//		public IMap.Crime crime;

	//		[Save.Force, Save.NewLine]
	//		public Sprite sprite_logo;

	//		[Save.Force, Save.NewLine]
	//		public ICharacter.Handle[] owners;

	//		[Save.Force, Save.NewLine]
	//		public ICompany.Handle[] affiliates;
	//	}
	//}

}
