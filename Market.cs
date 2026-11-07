
using Keg.Engine.Game;
using System.Text;

namespace TC2.Base.Components
{
	[Asset.Hjson(prefix: "store.", capacity_world: 512, capacity_region: 0, capacity_local: 0, 
	flags_global: Asset.Flags.Auto_Entity, flags_world: Asset.Flags.Auto_Entity)]
	public interface IStore: IAsset2<IStore, IStore.Data>
	{
		[Net.MsgPack]
		public struct Data(): IName
		{
			[Net.Key(00), Save.Force] public string name;

			//[Net.Key(00)]
			[Save.NewLine]
			[Net.Key(01), Save.Force] public IStockpile.Handle h_stockpile;
			[Net.Key(02), Save.Force] public ICompany.Handle h_company;
			[Net.Key(03), Save.Force] public ILocation.Handle h_location;
			[Net.Key(04), Save.Force] public ICatalogue.Handle h_catalogue;

			[Save.NewLine]
			[Net.Key(05), Save.Force, Save.Inline] public Shipment.Item2[] items_trading;
			//[Net.Key(06), Save.Force, Save.Inline] public Shipment.Item2[] items_trading;

			//[Net.Key(02)]
			[Save.NewLine]
			[Net.Key(08), Save.Force] public Market.Listing[] listings;

			//[Save.Force, Save.NewLine]
			//public Dictionary<ICompany.Handle, IInvoice.Handle> invoices = new(8);

			readonly ReadOnlySpan<char> IName.GetName() => this.name;
			readonly ReadOnlySpan<char> IName.GetShortName() => this.name;
		}
	}

	public static partial class Market
	{
		[Net.MsgPack]
		public partial struct Listing()
		{
			[Flags]
			public enum Permissions: byte
			{
				None = 0,

				Buy = 1 << 0,
				Sell = 1 << 1,

				Restock = 1 << 6,
				Edit = 1 << 7,
			}

			[Flags]
			public enum Flags: ushort
			{
				None = 0,

				[Name("Disabled")]
				Disabled = 1 << 0,
				[Name("Auto-Restock")]
				Auto_Restock = 1 << 1,
				[Name("Charm Pricing")]
				Price_Rounding = 1 << 2,
			}

			[Net.Key(00)] public IOffer.Handle h_offer;
			[Net.Key(01)] public int stock_amount;
			[Net.Key(02)] public float markup;
			[Net.Key(03)] public float discount;
			[Net.Key(04)] public float fee;

			[Save.NewLine]
			[Net.Key(05)] public Market.Listing.Flags flags;
			[Net.Key(06)] public ushort version;

			[Save.NewLine]
			[Net.Key(07)] public Market.Listing.Permissions perms_owner = Permissions.Buy | Permissions.Sell | Permissions.Restock | Permissions.Edit;
			[Net.Key(08)] public Market.Listing.Permissions perms_faction = Permissions.Buy | Permissions.Sell | Permissions.Restock;
			[Net.Key(09)] public Market.Listing.Permissions perms_ally = Permissions.Buy | Permissions.Sell;
			[Net.Key(10)] public Market.Listing.Permissions perms_neutral = Permissions.Buy | Permissions.Sell;
			[Net.Key(11)] public Market.Listing.Permissions perms_enemy = Permissions.None;
		}

		public static partial class Stock
		{
			[IComponent.Data(Net.SendType.Reliable, 128), ITrait.Data(Net.SendType.Reliable, 512)]
			public struct Data: IComponent, ITrait
			{
				public FixedArray4<float> amounts_a;
				public FixedArray4<float> amounts_b;
			}
		}

		[IComponent.Data(Net.SendType.Reliable)]
		public partial struct Data(): IComponent
		{
			[Flags]
			public enum Permissions: byte
			{
				None = 0,

				Buy = 1 << 0,
				Sell = 1 << 1,

				Add = 1 << 4,
				Remove = 1 << 5,
				Restock = 1 << 6,
				Edit = 1 << 7,
			}

			[Flags]
			public enum Flags: ushort
			{
				None = 0,

				No_Evict = 1 << 0,
				No_Claim = 1 << 1,
				No_Editing = 1 << 2,


				Public = 1 << 8,
				NPC = 1 << 9,
				Closed = 1 << 14,
				Disabled = 1 << 15
			}

			//public IStockpile.Handle h_stockpile;
			public IStore.Handle h_store;

			public Market.Data.Flags flags;
			public float tier;
			public float prestige;

			public Market.Data.Permissions perms_owner = Permissions.Buy | Permissions.Sell | Permissions.Add | Permissions.Remove | Permissions.Restock | Permissions.Edit;
			public Market.Data.Permissions perms_faction = Permissions.Buy | Permissions.Sell | Permissions.Restock;
			public Market.Data.Permissions perms_ally = Permissions.Buy | Permissions.Sell;
			public Market.Data.Permissions perms_neutral = Permissions.Buy | Permissions.Sell;
			public Market.Data.Permissions perms_enemy = Permissions.None;

			public byte offers_count_max;

			//public FixedArray16<Market.Listing> listings;

			//[Asset.Ignore, Save.Ignore] public InvRef inv_money;
		}

		public static float CalculateBuyScore(IMaterial.Handle h_material, ref ILocation.Data location_data)
		{
			ref var material_data = ref h_material.GetData();
			ref var commodity = ref material_data.commodity.GetRefOrNull();

			var weight = 0.00f;

			weight += location_data.urbanization * location_data.industrialization * location_data.technology * commodity.buildings_consume.GetCount(location_data.buildings);
			weight += location_data.wealth * location_data.urbanization * commodity.buildings_buy.GetCount(location_data.buildings);

			weight += location_data.urbanization * (1.00f - location_data.wilderness) * location_data.industrialization * commodity.categories_consume.GetCount(location_data.categories);
			weight += location_data.wealth * location_data.industrialization * (1.00f - location_data.wilderness) * commodity.categories_buy.GetCount(location_data.categories);

			weight += location_data.industrialization * commodity.industries_consume.GetCount(location_data.industry);
			weight += location_data.wealth * location_data.industrialization * commodity.industries_buy.GetCount(location_data.industry);

			weight -= location_data.wealth * commodity.categories.GetCount(IMap.Goods.Scrap | IMap.Goods.Waste | IMap.Goods.Raw | IMap.Goods.Soil | IMap.Goods.Rocks | IMap.Goods.Ores | IMap.Goods.Textiles | IMap.Goods.Livestock);
			weight -= location_data.technology * location_data.education * commodity.categories.GetCount(IMap.Goods.Animals | IMap.Goods.Crops | IMap.Goods.Fertilizers | IMap.Goods.Leather | IMap.Goods.Waste | IMap.Goods.Scrap | IMap.Goods.Soil | IMap.Goods.Mushrooms | IMap.Goods.Ores | IMap.Goods.Raw | IMap.Goods.Plants | IMap.Goods.Prisoners | IMap.Goods.Rocks);
			weight -= location_data.urbanization * commodity.categories.GetCount(IMap.Goods.Animals | IMap.Goods.Livestock | IMap.Goods.Crops | IMap.Goods.Dangerous | IMap.Goods.Mushrooms | IMap.Goods.Natural | IMap.Goods.Waste | IMap.Goods.Scrap | IMap.Goods.Fertilizers);
			weight -= location_data.devastation * commodity.categories.GetCount(IMap.Goods.Animals | IMap.Goods.Livestock | IMap.Goods.Crops | IMap.Goods.Dangerous | IMap.Goods.Mushrooms | IMap.Goods.Natural | IMap.Goods.Plants | IMap.Goods.Herbs | IMap.Goods.Fertilizers);

			weight -= (1.00f - location_data.wealth) * commodity.categories.GetCount(IMap.Goods.Jewelry | IMap.Goods.Arcane | IMap.Goods.Luxury | IMap.Goods.Medicine | IMap.Goods.Consumer | IMap.Goods.Refined | IMap.Goods.Vehicles | IMap.Goods.Blueprints | IMap.Goods.Stocks | IMap.Goods.Machinery);

			weight += (location_data.urbanization + location_data.industrialization - location_data.wilderness) * commodity.categories.GetCount(IMap.Goods.Construction_Materials) * Maths.Mulpo(location_data.buildings.GetCount(), 0.10f);

			//weight *= 0.30f * Maths.Mulpo(commodity.categories.GetCount(IMap.Goods.Components | IMap.Goods.Technology | IMap.Goods.Machinery | IMap.Goods.Vehicles | IMap.Goods.Refined | IMap.Goods.Arcane | IMap.Goods.Artificial | IMap.Goods.Chemicals | IMap.Goods.Energy | IMap.Goods.Paper | IMap.Goods.Blueprints | IMap.Goods.Medicine | IMap.Goods.Construction_Materials) * location_data.technology, 0.91f);
			//weight *= 0.50f * Maths.Mulpo(commodity.categories.GetCount(IMap.Goods.Munitions | IMap.Goods.Explosives | IMap.Goods.Dangerous | IMap.Goods.Coal | IMap.Goods.Weapons | IMap.Goods.Industrial | IMap.Goods.Military | IMap.Goods.Chemicals | IMap.Goods.Ores | IMap.Goods.Waste | IMap.Goods.Scrap | IMap.Goods.Kobolds | IMap.Goods.Metals | IMap.Goods.Minerals | IMap.Goods.Raw | IMap.Goods.Soil) * location_data.devastation, 0.42f);
			//weight *= 0.80f * Maths.Mulpo(commodity.categories.GetCount(IMap.Goods.Waste | IMap.Goods.Weapons | IMap.Goods.Scrap | IMap.Goods.Drugs | IMap.Goods.Explosives | IMap.Goods.Dangerous | IMap.Goods.Illegal | IMap.Goods.Alcohol | IMap.Goods.Prisoners | IMap.Goods.Toys) * location_data.anarchy, 0.95f);
			//weight *= 0.40f * Maths.Mulpo(commodity.categories.GetCount(IMap.Goods.Arcane | IMap.Goods.Technology | IMap.Goods.Blueprints | IMap.Goods.Consumer | IMap.Goods.Chemicals | IMap.Goods.Medicine | IMap.Goods.Stocks | IMap.Goods.Refined | IMap.Goods.Energy | IMap.Goods.Paper | IMap.Goods.Drugs) * location_data.education, 0.47f);
			//weight *= 0.40f * Maths.Mulpo(commodity.categories.GetCount(IMap.Goods.Artificial | IMap.Goods.Meat | IMap.Goods.Food | IMap.Goods.Technology | IMap.Goods.Machinery | IMap.Goods.Vehicles | IMap.Goods.Consumer | IMap.Goods.Industrial | IMap.Goods.Toys | IMap.Goods.Textiles | IMap.Goods.Waste | IMap.Goods.Alcohol | IMap.Goods.Tobacco | IMap.Goods.Scrap | IMap.Goods.Beverages | IMap.Goods.Clothing | IMap.Goods.Construction_Materials | IMap.Goods.Cooking | IMap.Goods.Drugs | IMap.Goods.Jewelry | IMap.Goods.Paper | IMap.Goods.Medicine | IMap.Goods.Kobolds) * location_data.urbanization, 0.66f);
			//weight *= 0.30f * Maths.Mulpo(commodity.categories.GetCount(IMap.Goods.Meat | IMap.Goods.Jewelry | IMap.Goods.Stocks | IMap.Goods.Clothing | IMap.Goods.Consumer | IMap.Goods.Arcane | IMap.Goods.Illegal | IMap.Goods.Medicine | IMap.Goods.Energy | IMap.Goods.Fuel | IMap.Goods.Paper | IMap.Goods.Technology | IMap.Goods.Toys | IMap.Goods.Beverages) * location_data.wealth, 0.75f);

			return weight;
		}

		public static float CalculateProductionScore(IMaterial.Handle h_material, ref ILocation.Data location_data)
		{
			ref var material_data = ref h_material.GetData();
			ref var commodity = ref material_data.commodity.GetRefOrNull();

			var score = 0.00f;

			//weight += location_data.urbanization * location_data.industrialization * Maths.Avg(commodity.buildings_produce.GetRatio(location_data.buildings), commodity.categories_produce.GetRatio(location_data.categories));
			//weight += location_data.industrialization * (commodity.industries_produce.GetRatio(location_data.industry) * commodity.scalability);

			//weight *= commodity.availability;
			//weight *= commodity.categories.GetRatio(location_data.goods_produced).Pow2();

			//weight = Maths.SumWeighted(
			//[
			//	//(commodity.categories.GetCount(IMap.Goods.Mined | IMap.Goods.Minerals | IMap.Goods.Ores | IMap.Goods.Rocks))
			//	//(commodity.categories.GetCount(IMap.Goods.Industrial | ), location_data.industrialization)
			//]);

			var score_mined = 0.00f;
			if (commodity.categories.HasAny(IMap.Goods.Mined))
			{
				score_mined = Maths.SumWeighted(
				[
					(commodity.categories.GetMultiplier(0.45f, IMap.Goods.Minerals | IMap.Goods.Ores | IMap.Goods.Rocks | IMap.Goods.Metals | IMap.Goods.Coal),
						location_data.buildings.GetCount((ILocation.Buildings.Borehole | ILocation.Buildings.Mine) & commodity.buildings_produce)
						* location_data.categories.GetMultiplier(location_data.devastation * location_data.industrialization * 0.30f, ILocation.Categories.Excavated | ILocation.Categories.Expansive)
						* location_data.geography.GetMultiplier(0.50f, IMap.Geography.Excavated | IMap.Geography.Hills | IMap.Geography.Mountains)),

					(commodity.categories.GetMultiplier(0.30f, IMap.Goods.Rocks | IMap.Goods.Soil),
						location_data.buildings.GetCount(ILocation.Buildings.Labor_Camp | ILocation.Buildings.Quarry | ILocation.Buildings.Mine))

						//(commodity.categories.GetCount(IMap.Goods.Industrial | ), location_data.industrialization)
				]);

				//score_mined = Maths.SumWeighted(
				//[
				//	(commodity.categories.GetMultiplier(0.45f, IMap.Goods.Minerals | IMap.Goods.Ores | IMap.Goods.Rocks | IMap.Goods.Metals | IMap.Goods.Coal), // general mining 
				//		location_data.buildings.GetCount((ILocation.Buildings.Borehole | ILocation.Buildings.Mine) & commodity.buildings_produce) // mining facilities
				//		* location_data.categories.GetMultiplier(location_data.devastation * location_data.industrialization * 0.30f, ILocation.Categories.Excavated | ILocation.Categories.Expansive) // implies large-scale mining operations
				//		* location_data.geography.GetMultiplier(0.50f, IMap.Geography.Excavated | IMap.Geography.Hills | IMap.Geography.Mountains)), // geography suitable for mining
				//	(commodity.categories.GetMultiplier(0.30f, IMap.Goods.Rocks | IMap.Goods.Soil), // surface mining / quarries
				//		location_data.buildings.GetCount(ILocation.Buildings.Labor_Camp | ILocation.Buildings.Quarry | ILocation.Buildings.Mine )

				//		//(commodity.categories.GetCount(IMap.Goods.Industrial | ), location_data.industrialization)
				//]);
			}

			var score_harvested = 0.00f;
			if (commodity.categories.HasAny(IMap.Goods.Harvested))
			{

			}

			var score_recycled = 0.00f;
			if (commodity.categories.HasAny(IMap.Goods.Recycled))
			{

			}

			var score_processed = 0.00f;
			if (commodity.categories.HasAny(IMap.Goods.Processed))
			{

			}

			var score_manufactured = 0.00f;
			if (commodity.categories.HasAny(IMap.Goods.Manufactured))
			{
				//score_manufactured = Maths.SumWeighted(
				//[
				//	(commodity.categories.GetRatio(0.50f, IMap.Goods.Arcane | IMap.Goods.Technology | IMap.Goods.
				//	)
				//]);
			}

			score = Maths.SumWeighted(
			[
				(score_mined, location_data.categories.GetRatio(ILocation.Categories.Excavated | ILocation.Categories.Wasteland))
			]);

			// TODO
			//weight -= location_data.wealth * commodity.categories.GetCount(IMap.Goods.Scrap | IMap.Goods.Waste | IMap.Goods.Raw | IMap.Goods.Soil | IMap.Goods.Rocks | IMap.Goods.Ores | IMap.Goods.Textiles | IMap.Goods.Livestock);
			//weight -= location_data.technology * location_data.education * commodity.categories.GetCount(IMap.Goods.Animals | IMap.Goods.Crops | IMap.Goods.Fertilizers | IMap.Goods.Leather | IMap.Goods.Waste | IMap.Goods.Scrap | IMap.Goods.Soil | IMap.Goods.Mushrooms | IMap.Goods.Ores | IMap.Goods.Raw | IMap.Goods.Plants | IMap.Goods.Prisoners | IMap.Goods.Rocks);
			//weight -= location_data.urbanization * commodity.categories.GetCount(IMap.Goods.Animals | IMap.Goods.Livestock | IMap.Goods.Crops | IMap.Goods.Dangerous | IMap.Goods.Mushrooms | IMap.Goods.Natural | IMap.Goods.Waste | IMap.Goods.Scrap | IMap.Goods.Fertilizers);
			//weight -= location_data.devastation * commodity.categories.GetCount(IMap.Goods.Animals | IMap.Goods.Livestock | IMap.Goods.Crops | IMap.Goods.Dangerous | IMap.Goods.Mushrooms | IMap.Goods.Natural | IMap.Goods.Plants | IMap.Goods.Herbs | IMap.Goods.Fertilizers);

			//weight -= (1.00f - location_data.wealth) * commodity.categories.GetCount(IMap.Goods.Jewelry | IMap.Goods.Arcane | IMap.Goods.Luxury | IMap.Goods.Medicine | IMap.Goods.Consumer | IMap.Goods.Refined | IMap.Goods.Vehicles | IMap.Goods.Blueprints | IMap.Goods.Stocks | IMap.Goods.Machinery);

			//weight += (location_data.urbanization + location_data.industrialization - location_data.wilderness) * commodity.categories.GetCount(IMap.Goods.Construction_Materials) * Maths.Mulpo(location_data.buildings.GetCount(), 0.10f);

			return score;
		}

		public static float CalculateSellScore(IMaterial.Handle h_material, ref ILocation.Data location_data)
		{
			ref var material_data = ref h_material.GetData();
			ref var commodity = ref material_data.commodity.GetRefOrNull();

			var weight = 0.00f;

			weight += commodity.buildings_produce.GetCount(location_data.buildings) * Maths.Mulpo(location_data.urbanization, 0.50f) * Maths.Mulpo(location_data.industrialization, 0.20f);
			weight += commodity.buildings_sell.GetCount(location_data.buildings) * location_data.urbanization * location_data.wealth * Maths.Mulpo(location_data.industrialization, 0.60f);

			weight += commodity.categories_produce.GetCount(location_data.categories) * (1.00f - location_data.wilderness) * location_data.industrialization;
			weight += commodity.categories_sell.GetCount(location_data.categories) * (1.00f - location_data.wilderness) * location_data.wealth * location_data.industrialization;

			weight += commodity.industries_produce.GetCount(location_data.industry) * location_data.industrialization;
			weight += commodity.industries_sell.GetCount(location_data.industry) * Maths.Mulpo(location_data.wealth, 1.50f) * location_data.industrialization;

			weight /= Maths.Max(0.50f, commodity.rarity);
			weight *= commodity.demand;
			//weight *= commodity.liquidity;

			//weight *= Maths.Normalize(location_data.buildings.GetCount(ILocation.Buildings.Scrapyard | ILocation.Buildings.Landfill), commodity.categories.GetCount(IMap.Goods.Scrap | IMap.Goods.Waste), 1.00f);

			//weight *= 0.30f * Maths.Mulpo(commodity.categories.GetCount(IMap.Goods.Components | IMap.Goods.Technology | IMap.Goods.Machinery | IMap.Goods.Vehicles | IMap.Goods.Refined | IMap.Goods.Arcane | IMap.Goods.Artificial | IMap.Goods.Chemicals | IMap.Goods.Energy | IMap.Goods.Paper | IMap.Goods.Blueprints | IMap.Goods.Medicine | IMap.Goods.Construction_Materials) * location_data.technology, 0.91f);
			//weight *= 0.50f * Maths.Mulpo(commodity.categories.GetCount(IMap.Goods.Munitions | IMap.Goods.Explosives | IMap.Goods.Dangerous | IMap.Goods.Coal | IMap.Goods.Weapons | IMap.Goods.Industrial | IMap.Goods.Military | IMap.Goods.Chemicals | IMap.Goods.Ores | IMap.Goods.Waste | IMap.Goods.Scrap | IMap.Goods.Kobolds | IMap.Goods.Metals | IMap.Goods.Minerals | IMap.Goods.Raw | IMap.Goods.Soil) * location_data.devastation, 0.42f);
			//weight *= 0.80f * Maths.Mulpo(commodity.categories.GetCount(IMap.Goods.Waste | IMap.Goods.Weapons | IMap.Goods.Scrap | IMap.Goods.Drugs | IMap.Goods.Explosives | IMap.Goods.Dangerous | IMap.Goods.Illegal | IMap.Goods.Alcohol | IMap.Goods.Prisoners | IMap.Goods.Toys) * location_data.anarchy, 0.95f);
			//weight *= 0.40f * Maths.Mulpo(commodity.categories.GetCount(IMap.Goods.Arcane | IMap.Goods.Technology | IMap.Goods.Blueprints | IMap.Goods.Consumer | IMap.Goods.Chemicals | IMap.Goods.Medicine | IMap.Goods.Stocks | IMap.Goods.Refined | IMap.Goods.Energy | IMap.Goods.Paper | IMap.Goods.Drugs) * location_data.education, 0.47f);
			//weight *= 0.40f * Maths.Mulpo(commodity.categories.GetCount(IMap.Goods.Artificial | IMap.Goods.Meat | IMap.Goods.Food | IMap.Goods.Technology | IMap.Goods.Machinery | IMap.Goods.Vehicles | IMap.Goods.Consumer | IMap.Goods.Industrial | IMap.Goods.Toys | IMap.Goods.Textiles | IMap.Goods.Waste | IMap.Goods.Alcohol | IMap.Goods.Tobacco | IMap.Goods.Scrap | IMap.Goods.Beverages | IMap.Goods.Clothing | IMap.Goods.Construction_Materials | IMap.Goods.Cooking | IMap.Goods.Drugs | IMap.Goods.Jewelry | IMap.Goods.Paper | IMap.Goods.Medicine | IMap.Goods.Kobolds) * location_data.urbanization, 0.66f);
			//weight *= 0.30f * Maths.Mulpo(commodity.categories.GetCount(IMap.Goods.Meat | IMap.Goods.Jewelry | IMap.Goods.Stocks | IMap.Goods.Clothing | IMap.Goods.Consumer | IMap.Goods.Arcane | IMap.Goods.Illegal | IMap.Goods.Medicine | IMap.Goods.Energy | IMap.Goods.Fuel | IMap.Goods.Paper | IMap.Goods.Technology | IMap.Goods.Toys | IMap.Goods.Beverages) * location_data.wealth, 0.75f);

			return weight;
		}

		public struct DEV_GenerateOffersRPC: Net.IRPC<Market.Data>
		{

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Market.Data data)
			{
				Assert.IsDevMode();
				Assert.IsAdmin(ref rpc.connection);

				ref var store_data = ref data.h_store.GetData(out var store_asset);
				Assert.IsNotNull(ref store_data);

				ref var location_data = ref store_data.h_location.GetData();
				Assert.IsNotNull(ref location_data);

				//var offers_span = store_data.listings.AsSpan();

				Span<IOffer.Handle> offers_span = stackalloc IOffer.Handle[64];
				GetOffers(data.h_store, ref offers_span);

				var materials_filtered = IMaterial.Database.GetAssets().Where(x => x.data.commodity?.flags.HasAny(IMaterial.Commodity.Flags.Marketable) ?? false).ToArray();
				var materials_filtered_span = materials_filtered.AsSpan();

				Span<float> weights_span = stackalloc float[materials_filtered_span.Length];

				// BUY
				if (true)
				{
					for (var i = 0; i < materials_filtered_span.Length; i++)
					{
						var material_asset = materials_filtered_span[i];
						weights_span[i] = CalculateBuyScore(material_asset, ref location_data);
					}
					weights_span.Sort(materials_filtered_span);

					for (var i = materials_filtered_span.Length - 1; i >= 0; i--)
					{
						App.WriteLine($"BUY [{i:00}]: {materials_filtered_span[i].data.name,-32}{weights_span[i]:0.00}");
					}
				}

				// SELL
				if (true)
				{
					for (var i = 0; i < materials_filtered_span.Length; i++)
					{
						var material_asset = materials_filtered_span[i];
						weights_span[i] = CalculateSellScore(material_asset, ref location_data);
					}
					weights_span.Sort(materials_filtered_span);

					for (var i = materials_filtered_span.Length - 1; i >= 0; i--)
					{
						App.WriteLine($"SELL [{i:00}]: {materials_filtered_span[i].data.name,-32}{weights_span[i]:0.00}");
					}
				}

				static float CalculateScore()
				{
					return 0;
				}



				var offers_span_sell = offers_span.Where(x => x.GetData().flags.HasAny(IOffer.Flags.Sell));
				offers_span_sell = offers_span_sell.OrderByDescending(x => x.GetData().max);

				for (var i = 0; i < offers_span_sell.Length; i++)
				{
					var h_offer = offers_span_sell[i];

					ref var offer_data = ref h_offer.GetData();
					if (offer_data.IsNotNull())
					{
						//App.WriteLine($"[{i:00}] {h_offer.id:000} {h_offer,-32}{offer_data.flags}");
					}
				}
			}
#endif
		}

		public static void GetOffers(IStore.Handle h_store, ref Span<IOffer.Handle> span)
		{
			ref var store_data = ref h_store.GetData(out var store_asset);
			Assert.IsNotNull(ref store_data);

			var count = 0;

			foreach (var s_offer in IOffer.Database.GetAssetsSpan())
			{
				if (count < span.Length)
				{
					ref var offer_data = ref s_offer.GetData();
					if ((offer_data.h_company == store_data.h_company || offer_data.companies_shared.Contains(store_data.h_company)) && offer_data.flags.HasAny(IOffer.Flags.Published) && !offer_data.flags.HasAny(IOffer.Flags.Hidden))
					{
						var h_offer = s_offer.GetHandle();
						span[count++] = h_offer;
					}
				}
				else break;
			}

			span = span.Slice(0, count);
		}

#if SERVER
		public static void GenerateOffers()
		{

		}
#endif

		public struct DEV_EditListingRPC: Net.IRPC<Market.Data>
		{
			public Market.Listing.Flags flags;

			public Market.Listing.Permissions perms_owner;
			public Market.Listing.Permissions perms_faction;
			public Market.Listing.Permissions perms_ally;
			public Market.Listing.Permissions perms_neutral;
			public Market.Listing.Permissions perms_enemy;

			public int stock_amount;
			public float markup;
			public float discount;
			public float fee;

			public byte index;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Market.Data data)
			{
				ref var store_data = ref data.h_store.GetData(out var store_asset);
				Assert.IsNotNull(ref store_data);

				var offers_span = store_data.listings.AsSpan();
				ref var listing = ref offers_span[this.index];

				ref var offer_data = ref listing.h_offer.GetData();
				Assert.IsNotNull(ref offer_data);

				//ref var player = ref connection.GetPlayer();
				//Assert.IsNotNull(ref player);

				//ref var region = ref connection.GetRegionCommon();
				//Assert.IsNotNull(ref region);

				// TODO: add validation

				var dirty = false;
				var mask_perms = ~Market.Listing.Permissions.None;

				listing.flags = this.flags;

				listing.perms_owner.SetFlagMasked(this.perms_owner, mask_perms);
				listing.perms_faction.SetFlagMasked(this.perms_faction, mask_perms);
				listing.perms_ally.SetFlagMasked(this.perms_ally, mask_perms);
				listing.perms_neutral.SetFlagMasked(this.perms_neutral, mask_perms);
				listing.perms_enemy.SetFlagMasked(this.perms_enemy, mask_perms);

				listing.fee = this.fee;

				listing.ApplyBuySellRestrictions();

				listing.stock_amount = this.stock_amount;
				listing.version++;

				store_asset.Sync();
				data.Sync(rpc.entity, true);
			}
#endif
		}

		public struct DEV_EditMarketRPC: Net.IRPC<Market.Data>
		{
			public Market.Data.Flags? flags;

			public Market.Data.Permissions? perms_owner;
			public Market.Data.Permissions? perms_faction;
			public Market.Data.Permissions? perms_ally;
			public Market.Data.Permissions? perms_neutral;
			public Market.Data.Permissions? perms_enemy;

			public IStockpile.Handle? h_stockpile;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Market.Data data)
			{
				Assert.Check(data.flags.HasNone(Data.Flags.No_Editing));

				//ref var player = ref connection.GetPlayer();
				//Assert.IsNotNull(ref player);

				//ref var region = ref connection.GetRegion();
				//Assert.IsNotNull(ref region);

				// TODO: add validation

				var mask_perms = ~Market.Data.Permissions.None;
				var sync = false;

				//sync |= data.flags.TrySetFlagMasked(this.flags, )
				sync |= data.perms_owner.TrySetFlagMasked(this.perms_owner, mask_perms);
				sync |= data.perms_faction.TrySetFlagMasked(this.perms_faction, mask_perms);
				sync |= data.perms_ally.TrySetFlagMasked(this.perms_ally, mask_perms);
				sync |= data.perms_neutral.TrySetFlagMasked(this.perms_neutral, mask_perms);
				sync |= data.perms_enemy.TrySetFlagMasked(this.perms_enemy, mask_perms);

				ref var store_data = ref data.h_store.GetData(out var store_asset);
				if (store_data.IsNotNull())
				{
					var sync_asset = false;

					sync_asset |= this.h_stockpile.TryCopyTo(ref store_data.h_stockpile);

					if (sync_asset)
					{
						store_asset.Sync();
					}
				}

				if (sync) data.Sync(rpc.entity, true);
			}
#endif
		}

		public struct DEV_InvoiceRPC: Net.IRPC<Market.Data>
		{
			public uint derp;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Market.Data data)
			{
				Assert.IsDevMode();
				Assert.IsAdmin(ref rpc.connection);

				ref var store_data = ref data.h_store.GetData();
				Assert.IsNotNull(ref store_data);

				RecalculateInvoices(rpc.entity, data.h_store);
			}
#endif
		}

		public struct DEV_RestockRPC: Net.IRPC<Market.Data>
		{
			public byte index;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Market.Data data)
			{
				Assert.IsDevMode();
				Assert.IsAdmin(ref rpc.connection);

				ref var store_data = ref data.h_store.GetData();
				Assert.IsNotNull(ref store_data);

				var offers_span = store_data.listings.AsSpan();
				ref var listing = ref offers_span[this.index];

				ref var offer_data = ref listing.h_offer.GetData();
				Assert.IsNotNull(ref offer_data);

				//ref var player = ref connection.GetPlayer();
				//Assert.IsNotNull(ref player);

				//ref var region = ref connection.GetRegion();
				//Assert.IsNotNull(ref region);

				ref var stock = ref rpc.entity.GetOrAddIndexPair<Market.Stock.Data>(this.index, true, true);
				for (var i = 0; i < stock.amounts_a.Length; i++)
				{
					if (offer_data.flags.HasAny(IOffer.Flags.Buy)) stock.amounts_a[i] = offer_data.items_a[i].quantity * listing.stock_amount;
					if (offer_data.flags.HasAny(IOffer.Flags.Sell)) stock.amounts_b[i] = offer_data.items_b[i].quantity * listing.stock_amount;
				}

				data.Sync(rpc.entity, true);

				Notification.Push(ref rpc.connection, $"DEV: Restocked offer #{this.index:00} at {rpc.entity.GetFullName()}.", Color32BGRA.Green);
			}
#endif
		}

		public static void RecalculateStock(Entity ent_market, IStore.Handle h_store)
		{
			var ts = Timestamp.Now();

			ref var store_data = ref h_store.GetData(out var s_store);
			if (store_data.IsNotNull())
			{
				var listings_span = store_data.listings.AsSpan();
				Span<Shipment.Item> request_items_buffer = stackalloc Shipment.Item[64];
				var request_items_count = 0;

				Span<Shipment.Item> send_items_buffer = stackalloc Shipment.Item[64];
				var send_items_count = 0;

				for (var i = 0; i < listings_span.Length; i++)
				{
					ref var listing = ref listings_span[i];
					ref var offer_data = ref listing.h_offer.GetData();
					if (offer_data.IsNotNull())
					{
						ref var stock = ref ent_market.GetIndexPair<Market.Stock.Data>((uint)i);
						if (stock.IsNotNull())
						{
							for (var j = 0; j < offer_data.items_a.Length; j++)
							{
								ref var item = ref offer_data.items_a[j];
								if (item.type == Shipment.Item.Type.Undefined) continue;

								var item_tmp = item;

								if (offer_data.flags.HasAll(IOffer.Flags.Sell | IOffer.Flags.Buy))
								{
									item_tmp.quantity = (item.quantity * listing.stock_amount) - stock.amounts_a[j];
									if (item_tmp.quantity > Maths.epsilon)
									{
										request_items_buffer.TryMergeAdd(item_tmp, ref request_items_count);
									}
									else if (item_tmp.quantity < -Maths.epsilon)
									{
										send_items_buffer.TryMergeAdd(item_tmp with { quantity = -item_tmp.quantity }, ref send_items_count);
									}
								}
								else if (offer_data.flags.HasAny(IOffer.Flags.Buy))
								{
									item_tmp.quantity = (item.quantity * listing.stock_amount) - stock.amounts_a[j];
									if (item_tmp.quantity > Maths.epsilon)
									{
										request_items_buffer.TryMergeAdd(item_tmp, ref request_items_count);
									}
								}
								else if (offer_data.flags.HasAny(IOffer.Flags.Sell))
								{
									item_tmp.quantity = stock.amounts_a[j];
									if (item_tmp.quantity > Maths.epsilon)
									{
										send_items_buffer.TryMergeAdd(item_tmp, ref send_items_count);
									}
								}

								//App.WriteLine($"{listing.h_offer}: {item_tmp} {item_tmp.quantity}; {offer_data.flags}");
							}

							for (var j = 0; j < offer_data.items_b.Length; j++)
							{
								ref var item = ref offer_data.items_b[j];
								if (item.type == Shipment.Item.Type.Undefined) continue;

								var item_tmp = item;
								//item_send.quantity = (offer_data.items_b[j].quantity * listing.stock_amount) - stock.amounts_b[j];

								if (offer_data.flags.HasAll(IOffer.Flags.Sell | IOffer.Flags.Buy))
								{
									item_tmp.quantity = (item.quantity * listing.stock_amount) - stock.amounts_b[j];
									if (item_tmp.quantity > Maths.epsilon)
									{
										request_items_buffer.TryMergeAdd(item_tmp, ref request_items_count);
									}
									else if (item_tmp.quantity < -Maths.epsilon)
									{
										send_items_buffer.TryMergeAdd(item_tmp with { quantity = -item_tmp.quantity }, ref send_items_count);
									}
								}
								else if (offer_data.flags.HasAny(IOffer.Flags.Buy))
								{
									item_tmp.quantity = stock.amounts_b[j];
									if (item_tmp.quantity > Maths.epsilon)
									{
										send_items_buffer.TryMergeAdd(item_tmp, ref send_items_count);
									}
								}
								else if (offer_data.flags.HasAny(IOffer.Flags.Sell))
								{
									item_tmp.quantity = (item.quantity * listing.stock_amount) - stock.amounts_b[j];
									if (item_tmp.quantity > Maths.epsilon)
									{
										request_items_buffer.TryMergeAdd(item_tmp, ref request_items_count);
									}
								}

								//App.WriteLine($"{listing.h_offer}: {item_tmp} {item_tmp.quantity}; {offer_data.flags}");

								//if (item_send.quantity > 0)
								//{
								//	send_items_buffer.TryMergeAdd(item_send, ref send_items_count);
								//}
							}
						}
					}
				}

				var ts_elapsed = ts.GetMilliseconds();
				App.WriteLine($"{ts_elapsed:0.0000} ms");

				App.WriteLine($"request: {request_items_count}\n{request_items_buffer.Slice(0, request_items_count).ToArray().Select(x => $"{x.quantity}x {x}").ToFormattedString()}");
				App.WriteLine($"send: {send_items_count}\n{send_items_buffer.Slice(0, send_items_count).ToArray().Select(x => $"{x.quantity}x {x}").ToFormattedString()}");
			}
		}

#if SERVER
		public static void RecalculateInvoices(Entity ent_market, IStore.Handle h_store)
		{
			var ts = Timestamp.Now();

			ref var store_data = ref h_store.GetData(out var s_store);
			if (store_data.IsNotNull())
			{
				ref var stockpile_data = ref store_data.h_stockpile.GetData(out var s_stockpile);
				if (stockpile_data.IsNotNull())
				{
					lock (stockpile_data.warrants)
					{
						var listings_span = store_data.listings.AsSpan();
						Span<Shipment.Item2> request_items_buffer = stackalloc Shipment.Item2[64];
						var request_items_count = 0;

						Span<Shipment.Item2> send_items_buffer = stackalloc Shipment.Item2[64];
						var send_items_count = 0;

						for (var i = 0; i < listings_span.Length; i++)
						{
							ref var listing = ref listings_span[i];
							ref var offer_data = ref listing.h_offer.GetData();
							if (offer_data.IsNotNull())
							{
								ref var stock = ref ent_market.GetIndexPair<Market.Stock.Data>((uint)i);
								if (stock.IsNotNull())
								{
									ref var h_warrant = ref System.Runtime.InteropServices.CollectionsMarshal.GetValueRefOrAddDefault(stockpile_data.warrants, offer_data.h_company, out var exists);
									if (!exists || !h_warrant.IsValid())
									{
										var warrant_tmp = new IWarrant.Data();
										warrant_tmp.h_company = offer_data.h_company;
										//warrant_tmp.h_company_provider = stockpile_data.h_company_provider;
										warrant_tmp.h_stockpile = store_data.h_stockpile;

										//ref var company_data = ref warrant_tmp.h_company.GetData();
										//if (company_data.IsNotNull())
										//{
										//	company_data.tag;
										//}

										h_warrant = IWarrant.Database.RegisterOrUpdate(Asset.GenerateRandomIdentifier(), index: null, scope: Asset.Scope.World, region_id: 0, ref warrant_tmp, flags: Asset.Flags.Recycle);
										h_warrant.Sync(true);
									}

									ref var warrant_data = ref h_warrant.GetData(out var s_warrant);
									//if (warrant_data.items == null || warrant_data.items.Length == 0) warrant_data.items = new Shipment.Item2[8];

									var warrant_items_span = warrant_data.items.AsSpan();

									for (var j = 0; j < offer_data.items_a.Length; j++)
									{
										ref var item = ref offer_data.items_a[j];
										if (item.type == Shipment.Item.Type.Undefined) continue;

										var item_tmp = item;

										if (offer_data.flags.HasAll(IOffer.Flags.Sell | IOffer.Flags.Buy))
										{
											item_tmp.quantity = (item.quantity * listing.stock_amount) - stock.amounts_a[j];
											if (item_tmp.quantity > Maths.epsilon)
											{
												request_items_buffer.TryMergeAdd(item_tmp, ref request_items_count);
												//warrant_items_a_span.TryAddOrSet(item_tmp);
											}
											else if (item_tmp.quantity < -Maths.epsilon)
											{
												send_items_buffer.TryMergeAdd(item_tmp with { quantity = -item_tmp.quantity }, ref send_items_count);
												warrant_items_span.TryAddOrSet(item_tmp with { quantity = -item_tmp.quantity });
											}
										}
										else if (offer_data.flags.HasAny(IOffer.Flags.Buy))
										{
											item_tmp.quantity = (item.quantity * listing.stock_amount) - stock.amounts_a[j];
											if (item_tmp.quantity > Maths.epsilon)
											{
												request_items_buffer.TryMergeAdd(item_tmp, ref request_items_count);
												//warrant_items_a_span.TryAddOrSet(item_tmp with { quantity = -item_tmp.quantity });
											}
										}
										else if (offer_data.flags.HasAny(IOffer.Flags.Sell))
										{
											item_tmp.quantity = stock.amounts_a[j];
											if (item_tmp.quantity > Maths.epsilon)
											{
												send_items_buffer.TryMergeAdd(item_tmp, ref send_items_count);
												warrant_items_span.TryMergeAdd(item_tmp);
											}
										}

										//App.WriteLine($"{listing.h_offer}: {item_tmp} {item_tmp.quantity}; {offer_data.flags}");
									}

									for (var j = 0; j < offer_data.items_b.Length; j++)
									{
										ref var item = ref offer_data.items_b[j];
										if (item.type == Shipment.Item.Type.Undefined) continue;

										var item_tmp = item;
										//item_send.quantity = (offer_data.items_b[j].quantity * listing.stock_amount) - stock.amounts_b[j];

										if (offer_data.flags.HasAll(IOffer.Flags.Sell | IOffer.Flags.Buy))
										{
											item_tmp.quantity = (item.quantity * listing.stock_amount) - stock.amounts_b[j];
											if (item_tmp.quantity > Maths.epsilon)
											{
												request_items_buffer.TryMergeAdd(item_tmp, ref request_items_count);
												//warrant_items_a_span.TryMergeAdd(item_tmp);
											}
											else if (item_tmp.quantity < -Maths.epsilon)
											{
												send_items_buffer.TryMergeAdd(item_tmp with { quantity = -item_tmp.quantity }, ref send_items_count);
												warrant_items_span.TryMergeAdd(item_tmp with { quantity = -item_tmp.quantity });
											}
										}
										else if (offer_data.flags.HasAny(IOffer.Flags.Buy))
										{
											item_tmp.quantity = stock.amounts_b[j];
											if (item_tmp.quantity > Maths.epsilon)
											{
												send_items_buffer.TryMergeAdd(item_tmp, ref send_items_count);
												warrant_items_span.TryMergeAdd(item_tmp);
											}
										}
										else if (offer_data.flags.HasAny(IOffer.Flags.Sell))
										{
											item_tmp.quantity = (item.quantity * listing.stock_amount) - stock.amounts_b[j];
											if (item_tmp.quantity > Maths.epsilon)
											{
												request_items_buffer.TryMergeAdd(item_tmp, ref request_items_count);
												//warrant_items_a_span.TryMergeAdd(item_tmp);
											}
										}

										//App.WriteLine($"{listing.h_offer}: {item_tmp} {item_tmp.quantity}; {offer_data.flags}");

										//if (item_send.quantity > 0)
										//{
										//	send_items_buffer.TryMergeAdd(item_send, ref send_items_count);
										//}
									}

								}
							}
						}

						foreach (var h_warrant in stockpile_data.warrants.Values)
						{
							ref var warrant_data = ref h_warrant.GetData();
							if (warrant_data.IsNotNull())
							{
								//warrant_data.items_a.Compact();
								//warrant_data.items_b.Compact();

								h_warrant.Sync(false);
							}
						}

						store_data.h_stockpile.Sync(false);

						var ts_elapsed = ts.GetMilliseconds();
						App.WriteLine($"{ts_elapsed:0.0000} ms");

						App.WriteLine($"request: {request_items_count}\n{request_items_buffer.Slice(0, request_items_count).ToArray().Select(x => $"{x.quantity}x {x}").ToFormattedString()}");
						App.WriteLine($"send: {send_items_count}\n{send_items_buffer.Slice(0, send_items_count).ToArray().Select(x => $"{x.quantity}x {x}").ToFormattedString()}");
					}
				}
			}
		}
#endif

		//public static void ApplyDefaultPermissions(ref this Market.Listing listing)
		//{
		//	ref var offer_data = ref listing.h_offer.GetData();
		//	if (offer_data.IsNotNull())
		//	{
		//		var mask_perms = Market.Listing.Permissions.None;
		//		mask_perms.SetFlag(Market.Listing.Permissions.Buy, offer_data.flags.HasAny(IOffer.Flags.Buy));
		//		mask_perms.SetFlag(Market.Listing.Permissions.Sell, offer_data.flags.HasAny(IOffer.Flags.Sell));

		//		listing.perms_owner = Listing.Permissions.Restock | Listing.Permissions.Edit;
		//		listing.perms_faction = Listing.Permissions.None;
		//		listing.perms_ally = Listing.Permissions.None;
		//		listing.perms_neutral = Listing.Permissions.None;
		//		listing.perms_enemy = Listing.Permissions.None;

		//		listing.perms_owner.SetFlagMasked(Listing.Permissions.Buy | Listing.Permissions.Sell, mask_perms);
		//		listing.perms_faction.SetFlagMasked(Listing.Permissions.Buy | Listing.Permissions.Sell, mask_perms);
		//		listing.perms_ally.SetFlagMasked(Listing.Permissions.Buy | Listing.Permissions.Sell, mask_perms);
		//		listing.perms_neutral.SetFlagMasked(Listing.Permissions.Buy | Listing.Permissions.Sell, mask_perms);
		//		listing.perms_enemy.SetFlagMasked(Listing.Permissions.Buy | Listing.Permissions.Sell, mask_perms);
		//	}
		//}

		public static Market.Listing.Permissions ApplyBuySellRestrictions(ref this Market.Listing listing)
		{
			var mask_perms = ~Market.Listing.Permissions.None;

			ref var offer_data = ref listing.h_offer.GetData();
			if (offer_data.IsNotNull())
			{
				mask_perms.SetFlag(Market.Listing.Permissions.Buy, offer_data.flags.HasAny(IOffer.Flags.Buy));
				mask_perms.SetFlag(Market.Listing.Permissions.Sell, offer_data.flags.HasAny(IOffer.Flags.Sell));

				listing.perms_owner &= mask_perms;
				listing.perms_faction &= mask_perms;
				listing.perms_ally &= mask_perms;
				listing.perms_neutral &= mask_perms;
				listing.perms_enemy &= mask_perms;
			}

			return mask_perms;
		}


		public struct DEV_SetOfferRPC: Net.IRPC<Market.Data>
		{
			public IOffer.Handle h_offer;
			public byte index;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Market.Data data)
			{
				ref var store_data = ref data.h_store.GetData(out var store_asset);
				Assert.IsNotNull(ref store_data);

				var offers_span = store_data.listings.AsSpan();
				ref var listing = ref offers_span[this.index];

				ref var offer_data = ref this.h_offer.GetData();
				if (offer_data.IsNotNull())
				{
					//ref var player = ref connection.GetPlayer();
					//Assert.IsNotNull(ref player);

					//ref var region = ref connection.GetRegion();
					//Assert.IsNotNull(ref region);

					listing = new Listing();
					listing.h_offer = this.h_offer;
					listing.stock_amount = offer_data.stock_default;

					listing.ApplyBuySellRestrictions();

					//var mask_perms = Market.Listing.Permissions.None;
					//mask_perms.SetFlag(Market.Listing.Permissions.Buy, offer_data.flags.HasAny(IOffer.Flags.Buy));
					//mask_perms.SetFlag(Market.Listing.Permissions.Sell, offer_data.flags.HasAny(IOffer.Flags.Sell));

					//listing.perms_owner = Listing.Permissions.Restock | Listing.Permissions.Edit;
					//listing.perms_faction = Listing.Permissions.None;
					//listing.perms_ally = Listing.Permissions.None;
					//listing.perms_neutral = Listing.Permissions.None;
					//listing.perms_enemy = Listing.Permissions.None;

					//listing.perms_owner.SetFlagMasked(Listing.Permissions.Buy | Listing.Permissions.Sell, mask_perms);
					//listing.perms_faction.SetFlagMasked(Listing.Permissions.Buy | Listing.Permissions.Sell, mask_perms);
					//listing.perms_ally.SetFlagMasked(Listing.Permissions.Buy | Listing.Permissions.Sell, mask_perms);
					//listing.perms_neutral.SetFlagMasked(Listing.Permissions.Buy | Listing.Permissions.Sell, mask_perms);
					////listing.perms_enemy.SetFlagMasked(Listing.Permissions.Buy | Listing.Permissions.Sell, mask_perms);

					ref var stock = ref rpc.entity.GetOrAddIndexPair<Market.Stock.Data>(this.index, true, true);
					stock = new Stock.Data();

					store_asset.Sync();

					//for (var i = 0; i < supply.amounts_a.Length; i++)
					//{
					//	if (offer_data.flags.HasAny(IOffer.Flags.Buy)) supply.amounts_a[i] = offer_data.items_a[i].quantity * listing.stock_amount;
					//	if (offer_data.flags.HasAny(IOffer.Flags.Sell)) supply.amounts_b[i] = offer_data.items_b[i].quantity * listing.stock_amount;
					//}
				}
				else
				{
					listing = default;
					store_asset.Sync();
				}

				data.Sync(rpc.entity, true);
			}
#endif
		}

		public struct TradeRPC: Net.IRPC<Market.Data>
		{
			//public IOffer.Handle h_offer;
			public int amount_multiplier;
			public byte index;
			public byte sell;

#if SERVER
			[Shitcode] // TODO: not thread-safe
			public void Invoke(Net.IRPC.Context rpc, ref Market.Data data)
			{
				ref var store_data = ref data.h_store.GetData();
				Assert.IsNotNull(ref store_data);

				var offers_span = store_data.listings.AsSpan();
				ref var listing = ref offers_span[this.index];

				ref var offer_data = ref listing.h_offer.GetData();
				Assert.IsNotNull(ref offer_data);

				var is_buying = this.sell == 0;
				Assert.Check(offer_data.flags.HasAny(is_buying ? IOffer.Flags.Buy : IOffer.Flags.Sell));

				ref var character = ref rpc.connection.GetCharacter(out var h_character);
				//Assert.IsNotNull(ref player);

				ref var region = ref rpc.entity.GetRegionCommon();
				Assert.IsNotNull(ref region);

				//Assert.Check(listing.ent_shipment.IsAlive());
				ref var supply = ref rpc.entity.GetIndexPair<Market.Stock.Data>(this.index);
				Assert.IsNotNull(ref supply);

				Assert.Check(Crafting.Context.NewFromCharacter(region: ref region,
					h_character: h_character,
					ent_producer: rpc.entity,
					context: out var context,
					search_radius: 8.00f));

				this.amount_multiplier = Maths.Clamp(this.amount_multiplier, offer_data.min, offer_data.max);

				var items_a_span = is_buying ? offer_data.items_a.AsSpan() : offer_data.items_b.AsSpan();
				var items_b_span = is_buying ? offer_data.items_b.AsSpan() : offer_data.items_a.AsSpan();

				var supply_a_span = is_buying ? supply.amounts_a.AsSpan() : supply.amounts_b.AsSpan();
				var supply_b_span = is_buying ? supply.amounts_b.AsSpan() : supply.amounts_a.AsSpan();

				Span<Crafting.Product> products_span = stackalloc Crafting.Product[items_a_span.Length];
				Assert.Check(items_a_span.ToProducts(ref products_span));
				products_span.TrimInvalid();

				var has_mats = true;
				for (var i = 0; i < supply_a_span.Length; i++)
				{
					has_mats &= (supply_a_span[i] >= (items_a_span[i].quantity * this.amount_multiplier));
				}
				Assert.Check(has_mats);

				Span<Crafting.Requirement> requirements_span = stackalloc Crafting.Requirement[items_b_span.Length];
				Assert.Check(items_b_span.ToRequirements(ref requirements_span));
				requirements_span.TrimInvalid();

				requirements_span.ForEach((ref x) => x.flags |= Crafting.Requirement.Flags.Prerequisite);

				var eval_flags = Crafting.EvaluateFlags.Prerequisite;
				if (Assert.Check(context.Evaluate(requirements: requirements_span, amount_multiplier: this.amount_multiplier, evaluation_flags: eval_flags), Assert.Level.Warn))
				{
					context.Consume(requirements: requirements_span,
						amount_multiplier: this.amount_multiplier,
						evaluation_flags: eval_flags);

					for (var i = 0; i < supply_a_span.Length; i++)
					{
						supply_a_span[i] -= (items_a_span[i].quantity * this.amount_multiplier);
						supply_b_span[i] += (items_b_span[i].quantity * this.amount_multiplier);
					}

					context.Produce(products: products_span,
						amount_multiplier: this.amount_multiplier,
						spawn_flags: Resource.SpawnFlags.Merge | Resource.SpawnFlags.Pickup | Resource.SpawnFlags.Allow_Encumbered | Resource.SpawnFlags.No_Discard | Resource.SpawnFlags.Show_Notification);

					if (context.world_position.TryGetValue(out var pos))
					{
						Sound.Play(ref region.AsRegion(), "buy", pos, volume: 0.50f, priority: 0.20f);
						WorldNotification.Push(ref region.AsRegion(), $"* {(is_buying ? "Buys" : "Sells")} {this.amount_multiplier:0.##}x {"<TODO>"} *", 0xff00ff00, pos, lifetime: 5.00f);
					}

					supply.Sync(rpc.entity, this.index);
				}
			}
#endif
		}

		[ISystem.Modified(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void UpdateLogo(ISystem.Info info, [Source.Owned] ref Company.Data company, [Source.Owned] ref Transform.Data transform,
		[Source.Owned, Pair.Component<Company.Data>] ref Animated.Renderer.Data renderer)
		{
			ref var company_data = ref company.h_company.GetData();
			if (company_data.IsNotNull())
			{
				renderer.sprite = company_data.GetLogo(company.logo_type);
			}
			else
			{
				renderer.sprite = ICompany.GetLogo(ICompany.tex_logo_default, company.logo_type);
			}

			// To ensure signs don't have mirrored text
			renderer.scale.X.SetSign(transform.scale.X); //.SetSign(transform.scale.X.IsNegative());
		}

		[ISystem.Modified(ISystem.Mode.Single, ISystem.Scope.Region)]
		public static void UpdateLight(ISystem.Info info, [Source.Owned] ref Company.Data company,
		[Source.Owned, Pair.Component<Company.Data>] ref Light.Data light)
		{
			ref var company_data = ref company.h_company.GetData();
			if (company_data.IsNotNull())
			{
				light.color = light.color.WithRGB(company_data.color_glow);
			}
			else
			{
				light.color = light.color.WithRGB(default);
			}
		}

		public struct DEV_SetCompanyRPC: Net.IRPC<Market.Data>
		{
			public ICompany.Handle h_company;

#if SERVER
			public void Invoke(Net.IRPC.Context rpc, ref Market.Data data)
			{
				ref var company = ref rpc.entity.GetComponent<Company.Data>();
				Assert.IsNotNull(ref company);

				ref var store_data = ref data.h_store.GetData(out var store_asset);
				Assert.IsNotNull(ref store_data);

				//Assert.Check(store_data.h_company == 0);
				Assert.Check(store_data.h_company != this.h_company, Assert.Level.Warn);

				store_data.h_company = this.h_company;
				store_asset.Sync();

				company.h_company = this.h_company;
				company.Sync(rpc.entity);
			}
#endif
		}

#if SERVER
		[ISystem.AddFirst(ISystem.Mode.Single, ISystem.Scope.Region, order: 20), HasTag("initialized", true, Source.Modifier.Owned)]
		public static void OnAdd_Region(ISystem.Info info, ref Region.Data region, Entity entity,
			[Source.Owned] ref Market.Data market,
			[Source.Owned, Optional(true)] ref Company.Data company,
			[Source.Owned, Optional(true)] ref Stockpile.Data stockpile)
		{
			if (!market.h_store.IsValid())
			{
				var store_data = new IStore.Data();
				store_data.listings = new Market.Listing[8];

				ref var map_info = ref region.GetMapInfo();
				ref var region_info = ref region.GetRegionInfo();
				App.WriteLine(region_info.map);

				store_data.name = $"{entity.GetName()} - {map_info.name}";
				store_data.h_location = map_info.h_location;

				if (company.IsNotNull())
				{
					store_data.h_company = company.h_company;
				}

				if (stockpile.IsNotNull())
				{
					store_data.h_stockpile = stockpile.h_stockpile;
				}

				//var identifier = Asset.GenerateRandomIdentifier();

				var identifier = Asset.GenerateRandomIdentifier();
				var identifier_entity = entity.GetAsset()?.Identifier ?? entity.GetIdentifier(); // TODO: kinda hacky
				if (identifier_entity.IsNotNullOrWhiteSpace()) identifier = $"{identifier_entity}.{identifier}";

				var store_asset = IStore.Database.RegisterOrUpdate(identifier, null, Asset.Scope.World, region.GetID(), ref store_data, flags: Asset.Flags.Recycle);
				//store_asset.entity.AddRelation(store_data.h_location.GetEntity(), Relation.Type.Child);
				store_asset.Sync(true);

				market.h_store = store_asset;
				market.Sync(entity, true);
			}
		}

		[ISystem.AddFirst(ISystem.Mode.Single, ISystem.Scope.Global, order: 10), HasTag("initialized", true, Source.Modifier.Owned)]
		public static void OnAdd_Global(ISystem.Info.Global info, ref Region.Data.Global region, Entity entity,
			[Source.Owned] ref Market.Data market,
			[Source.Owned, Optional(true)] ref Company.Data company,
			[Source.Owned, Optional(true)] ref Stockpile.Data stockpile,
			[Source.Owned, Optional(true)] ref Location.Data location)
		{
			if (!market.h_store.IsValid())
			{
				var store_data = new IStore.Data();
				store_data.listings = new Market.Listing[8];

				if (ILocation.TryGetAsset(entity, out var h_location))
				{

				}

				ref var location_data = ref h_location.GetData(out var location_asset);

				store_data.name = location_data.IsNotNull() ? location_data.name : "<unnamed>";
				var asset_region_id = (byte)0;

				if (location.IsNotNull())
				{
					asset_region_id = location_asset.RegionID;
					store_data.h_location = location.h_location;
				}

				if (company.IsNotNull())
				{
					store_data.h_company = company.h_company;
				}

				if (stockpile.IsNotNull())
				{
					store_data.h_stockpile = stockpile.h_stockpile;
				}

				//var identifier = Asset.GenerateRandomIdentifier();

				var identifier = Asset.GenerateRandomIdentifier();
				var identifier_entity = entity.GetAsset()?.Identifier ?? entity.GetIdentifier(); // TODO: kinda hacky
				if (identifier_entity.IsNotNullOrWhiteSpace()) identifier = $"{identifier_entity}.{identifier}";

				var store_asset = IStore.Database.RegisterOrUpdate(identifier, null, Asset.Scope.World, asset_region_id, ref store_data, flags: Asset.Flags.Recycle);
				//store_asset.entity.AddRelation(store_data.h_location.GetEntity(), Relation.Type.Child);
				store_asset.Sync(true);


				market.h_store = store_asset;
				market.Sync(entity, true);
			}
		}

		//[ISystem.Modified<Market.Data>(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global, order: 110)]
		//public static void OnFactionModified(Entity entity, ISystem.Info.Common info, [Source.Owned] ref Market.Data market, [Source.Owned] ref Faction.Data faction)
		//{
		//	App.WriteLine($"add faction {faction.id}");
		//	ref var market_data = ref market.h_store.GetData(out var market_asset);
		//	if (market_data.IsNotNull())
		//	{
		//		market_data.h_faction = faction.id;

		//		market_asset.Sync(false);
		//	}
		//}

		//[ISystem.Modified<Market.Data>(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global, order: 120)]
		//public static void OnLocationModified(Entity entity, ISystem.Info.Common info, [Source.Owned] ref Market.Data market, [Source.Owned] ref Location.Data location)
		//{
		//	App.WriteLine($"add location {location.h_location}");
		//	ref var market_data = ref market.h_store.GetData(out var market_asset);
		//	if (market_data.IsNotNull())
		//	{
		//		market_data.h_location = location.h_location;
		//		//market_asset.entity.AddRelation(location.h_location.GetEntity(), Relation.Type.Child);

		//		market_asset.Sync(false);
		//	}
		//}

		//[ISystem.Modified<Market.Data>(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global, order: 140)]
		//public static void OnCompanyModified(Entity entity, ISystem.Info.Common info, [Source.Owned] ref Market.Data market, [Source.Owned] ref Company.Data company)
		//{
		//	App.WriteLine($"add company {company.h_company}");
		//	ref var market_data = ref market.h_store.GetData(out var market_asset);
		//	if (market_data.IsNotNull())
		//	{
		//		market_data.h_company_provider = company.h_company;
		//		market_data.h_company = company.h_company;

		//		market_asset.Sync(false);
		//	}
		//}

		[ISystem.RemoveLast(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global)]
		public static void OnRemove(Entity entity, [Source.Owned] ref Market.Data market)
		{
			ref var store_data = ref market.h_store.GetData(out var store_asset);
			if (store_data.IsNotNull())
			{
				store_asset.Recycle();

				market.h_store = default;
				market.Sync(entity, true);
			}
		}
#endif

		//		public struct DEV_RerollRPC: Net.IRPC<Market.Data>
		//		{
		//#if SERVER
		//			public void Invoke(Net.IRPC.Context rpc, ref Market.Data data)
		//			{
		//				//if (connection.IsAdmin())
		//				{
		//					var random = XorRandom.New(true);
		//					ref var region = ref connection.GetRegion();

		//					//var origins = IOrigin.Database.GetAssets(Asset.Scope.Global).Where(x => x.id != 0).ToArray();

		//					var map_info_copy = region.GetMapInfo();

		//					//var species_tmp = data.species;
		//					//var hairs = IHair.Database.GetAssets(Asset.Scope.Global).Where(x => x.id != 0).ToArray();


		//					//ref var character_info = ref character_infos[i];

		//					var company = new ICompany.Data();
		//					//company.kind = ICompany.Kind.Frontier_Company;

		//					var industry_a = map_info_copy.industry.GetRandomFlag(ref random);
		//					var industry_b = map_info_copy.industry.GetRandomFlag(ref random) | map_info_copy.industry.GetRandomFlag(ref random) | map_info_copy.industry.GetRandomFlag(ref random);
		//					company.industry_primary = industry_a | industry_b;

		//					var sb = new StringBuilder();

		//					var tags = Nameable.Tags.Market;
		//					var industry = company.industry_primary;
		//					App.WriteLine(industry);

		//					if (industry.HasAny(IMap.Industry.Financial | IMap.Industry.Administrative | IMap.Industry.High_Tech))
		//					{
		//						//if (random.NextBool(0.70f)) company.kind = ICompany.Kind.;
		//						if (random.NextBool(0.30f)) tags |= Nameable.Tags.Fancy;
		//					}
		//					else if (industry.HasAny(IMap.Industry.Agricultural | IMap.Industry.Fishing | IMap.Industry.Food))
		//					{
		//						if (random.NextBool(0.30f))
		//						{
		//							company.kind = ICompany.Kind.Cooperative;
		//							if (random.NextBool(0.25f))
		//							{
		//								sb.Append("JZD ");
		//							}

		//							if (random.NextBool(0.50f))
		//							{
		//								tags |= Nameable.Tags.Food;
		//							}
		//						}
		//					}

		//					if (industry.HasAny(IMap.Industry.Manufacturing))
		//					{
		//						tags |= Nameable.Tags.Factory;
		//						if (random.NextBool(0.25f)) tags |= Nameable.Tags.Industrial;
		//					}

		//					if (industry.HasAny(IMap.Industry.Chemical))
		//					{
		//						tags |= Nameable.Tags.Chemical;
		//						if (random.NextBool(0.25f)) tags |= Nameable.Tags.Tech;
		//						if (random.NextBool(0.25f)) tags |= Nameable.Tags.Industrial;
		//					}

		//					if (industry.HasAny(IMap.Industry.Scrap))
		//					{
		//						tags |= Nameable.Tags.Dangerous | Nameable.Tags.Industrial;
		//						if (random.NextBool(0.25f)) tags |= Nameable.Tags.Lame;
		//					}

		//					if (industry.HasAny(IMap.Industry.Arcane | IMap.Industry.High_Tech))
		//					{
		//						tags |= Nameable.Tags.Tech;
		//						if (random.NextBool(0.50f)) tags |= Nameable.Tags.Fancy;
		//					}

		//					if (industry.HasAny(IMap.Industry.Financial))
		//					{
		//						tags |= Nameable.Tags.Market;
		//					}

		//					//if (map_info_copy.wealth > 0.60f)
		//					//{
		//					//	//company.kind = ICompany.Kind.Joint_Stock_Company;
		//					//}

		//					sb.Append(Nameable.GenerateName(ref random, kind: Nameable.Kind.Building, tags: tags));

		//					if (random.NextBool(0.10f))
		//					{
		//						sb.Append('-');
		//						//sb.Append(map_info_copy.name);
		//					}
		//					else if (random.NextBool(0.50f))
		//					{
		//						if (tags.HasAny(Nameable.Tags.Tech)) sb.Append("-Technik");
		//						else if (tags.HasAny(Nameable.Tags.Chemical)) sb.Append("-Chemus");
		//						else if (industry_a.HasAny(IMap.Industry.Construction | IMap.Industry.Manufacturing)) sb.Append("-Konstrukto");
		//						else if (industry_a.HasAny(IMap.Industry.Power)) sb.Append("-Elektro");
		//						//else if (industry_a.HasAny(IMap.Industry.Mining)) sb.Append("-Prospekto");
		//					}

		//					if (random.NextBool(0.40f))
		//					{
		//						var suffix = company.kind.GetEnumName(Text.Type.Imperial);
		//						if (!suffix.IsWhiteSpace())
		//						{
		//							sb.Append(' ');
		//							sb.Append(suffix);
		//						}
		//					}

		//					var name = sb.ToString();
		//					company.name = name;
		//					//company.sprite_logo = new Sprite("company.icons", 16, 16, 0, 0);

		//					var species_tmp = new ISpecies.Handle("human");

		//					var owners_require = Character.Traits.None;
		//					var owners_include = Character.Traits.None;
		//					var owners_exclude = Character.Traits.None;

		//					switch (company.kind)
		//					{
		//						default:
		//						case ICompany.Kind.Proprietorship:
		//						{
		//							company.owners = new ICharacter.Handle[1];
		//							owners_include |= Character.Traits.Middle_Class | Character.Traits.Artisan | Character.Traits.Commerce | Character.Traits.Farmer | Character.Traits.Entertainer;
		//							owners_exclude |= Character.Traits.Slave | Character.Traits.Soldier | Character.Traits.Unskilled | Character.Traits.Pet | Character.Traits.Lower_Class;
		//						}
		//						break;

		//						case ICompany.Kind.Partnership:
		//						{
		//							company.owners = new ICharacter.Handle[random.NextIntRange(1, 3)];
		//							owners_include |= Character.Traits.Military | Character.Traits.Construction | Character.Traits.Hunter | Character.Traits.Farmer | Character.Traits.Artisan | Character.Traits.Miner;
		//							owners_exclude |= Character.Traits.Slave | Character.Traits.Unskilled | Character.Traits.Pet;
		//						}
		//						break;

		//						case ICompany.Kind.Cooperative:
		//						{
		//							company.owners = new ICharacter.Handle[random.NextIntRange(1, 3)];
		//							owners_include |= Character.Traits.Farmer | Character.Traits.Hunter | Character.Traits.Artisan | Character.Traits.Culinary;
		//							owners_exclude |= Character.Traits.Slave | Character.Traits.Pet;
		//						}
		//						break;

		//						//case ICompany.Kind.:
		//						//{
		//						//	company.owners = new ICharacter.Handle[random.NextIntRange(3, 5)];
		//						//	owners_require |= Character.Traits.Professional | Character.Traits.Educated;
		//						//	owners_include |= Character.Traits.Rich | Character.Traits.Bureaucrat | Character.Traits.Nobility | Character.Traits.Law | Character.Traits.Scientist | Character.Traits.Professional | Character.Traits.Upper_Class | Character.Traits.Upper_Middle_Class;
		//						//	owners_exclude |= Character.Traits.Slave | Character.Traits.Pet | Character.Traits.Unskilled | Character.Traits.Lower_Class | Character.Traits.Lower_Middle_Class | Character.Traits.Middle_Class | Character.Traits.Laborer | Character.Traits.Lowlife;
		//						//}
		//						//break;

		//						//case ICompany.Kind.State_Company:
		//						//{
		//						//	company.owners = new ICharacter.Handle[random.NextIntRange(3, 5)];
		//						//	owners_require |= Character.Traits.Professional | Character.Traits.Educated;
		//						//	owners_include |= Character.Traits.Rich | Character.Traits.Educated | Character.Traits.Professional | Character.Traits.Nobility | Character.Traits.Bureaucrat | Character.Traits.Law;
		//						//	owners_exclude |= Character.Traits.Slave | Character.Traits.Pet | Character.Traits.Unskilled | Character.Traits.Lower_Class | Character.Traits.Lower_Middle_Class | Character.Traits.Middle_Class | Character.Traits.Laborer | Character.Traits.Lowlife | Character.Traits.Poor;
		//						//}
		//						//break;
		//					}

		//					var origins = new WeightedList<IOrigin.Handle>(IOrigin.Database.GetAssets().Where(x => x.id != 0 && x.data.species == species_tmp && x.data.character_flags.Evaluate(owners_require, owners_include, owners_exclude)).Select(x => new WeightedList<IOrigin.Handle>.Item(x.data.conditions.CalculateWeight(map_info_copy), x)));
		//					for (var i = 0; i < company.owners.Length; i++)
		//					{
		//						//var h_character = Spawner.CreateCharacter(ref region, ref random, origins.GetRandom(ref random), scope: Asset.Scope.World, recycle: false);

		//						var h_character = Spawner.CreateCharacter(ref region, ref random,
		//							h_origin: origins.GetRandom(ref random),
		//							//h_faction: h_faction,
		//							scope: Asset.Scope.World);

		//						company.owners[i] = h_character;
		//					}

		//					var identifier = Asset.GenerateRandomIdentifier();
		//					App.WriteLine(identifier);

		//					var asset = ICompany.Database.RegisterOrUpdate(identifier, null, Asset.Scope.World, 0, ref company);
		//					asset.Sync(true);

		//					data.Sync(entity);
		//				}
		//			}
		//#endif
		//		}

//#if SERVER
//		[ISystem.Add(ISystem.Mode.Single, ISystem.Scope.Region)]
//		public static void OnAddInventory<T>(ISystem.Info info, Entity entity,
//		[Source.Owned] ref Market.Data market,
//		[Source.Owned, Pair.Component<Market.Data>] ref T inventory) where T : unmanaged, IInventory
//		{
//			//if (App.log_filter.HasAny(App.LogFlags.Debug)) App.WriteLine($"{info.WorldTime} {entity} add market inventory {typeof(T).FullName}; {inventory.Type}", App.Color.Green);

//			market.inv_money.Set(entity, inventory.ID);
//			market.Sync(entity, true);
//		}
//#endif

#if CLIENT
		//public static void ValidateCompany(ref ICompany.Kind kind, ref ICompany.Flags flags)
		//{
		//	switch (kind)
		//	{
		//		case ICompany.Kind.Cooperative:
		//		{
		//			flags.SetFlag(ICompany.Flags.Private | ICompany.Flags.Public, false);
		//			flags.SetFlag(ICompany.Flags.Collective, true);
		//		}
		//		break;

		//		case ICompany.Kind.Guild:
		//		{
		//			flags.SetFlag(ICompany.Flags.Private, false);
		//		}
		//		break;

		//		case ICompany.Kind.Proprietorship:
		//		{
		//			flags.SetFlag(ICompany.Flags.Public | ICompany.Flags.Collective, false);
		//			flags.SetFlag(ICompany.Flags.Private, true);
		//		}
		//		break;
		//	}
		//}

		//public static void GetCompanyMask(ICompany.Kind kind,
		//ref ICompany.Flags flags, ref IMap.Industry industry, ref IMap.Services services, ref IMap.Crime crime, ref ICompany.Liability liability,
		//out int max_primary_industry_flags, out int max_secondary_industry_flags, out int max_services_flags,
		//out ICompany.Flags flags_mask, out IMap.Industry industry_mask, out IMap.Services services_mask, out IMap.Crime crime_mask, out BitField<ICompany.Liability> liability_mask)
		//{
		//	max_primary_industry_flags = 0;
		//	max_secondary_industry_flags = 0;
		//	max_services_flags = 0;

		//	flags_mask = default;
		//	industry_mask = default;
		//	services_mask = default;
		//	crime_mask = default;
		//	liability_mask = default;

		//	switch (kind)
		//	{
		//		case ICompany.Kind.Charity:
		//		{
		//			max_services_flags = 3;

		//			liability = ICompany.Liability.None;
		//			liability_mask.Add(ICompany.Liability.None);

		//			flags_mask.SetFlag(ICompany.Flags.Public | ICompany.Flags.Collective | ICompany.Flags.Criminal, true);
		//			//industry_mask.SetFlag(IMap.Industry.Labor | IMap.Industry.Media | IMap.Industry.Training | IMap.Industry.Tourism | IMap.Industry.Tobacco | IMap.Industry.Kobolds | IMap.Industry.Hospitality | IMap.Industry.Agricultural | IMap.Industry.Criminal | IMap.Industry.Education | IMap.Industry.Entertainment | IMap.Industry.Food | IMap.Industry.Fishing | IMap.Industry.Labor, true);
		//			services_mask.SetFlag(IMap.Services.Education | IMap.Services.Entertainment | IMap.Services.Training | IMap.Services.Waste_Management | IMap.Services.Propaganda | IMap.Services.Healthcare, true);
		//		}
		//		break;

		//		case ICompany.Kind.Cooperative:
		//		{
		//			max_primary_industry_flags = 1;
		//			max_secondary_industry_flags = 2;
		//			max_services_flags = 2;

		//			liability_mask.Add(ICompany.Liability.Limited);
		//			liability_mask.Add(ICompany.Liability.Unlimited);

		//			flags_mask.SetFlag(ICompany.Flags.Collective | ICompany.Flags.Criminal | ICompany.Flags.Subsidiary | ICompany.Flags.Holding, true);
		//		}
		//		break;

		//		case ICompany.Kind.Gang:
		//		{
		//			max_services_flags = 3;

		//			liability_mask.Add(ICompany.Liability.Unlimited);

		//			flags.SetFlag(ICompany.Flags.Collective, true);
		//			flags_mask.SetFlag(ICompany.Flags.Collective | ICompany.Flags.Criminal | ICompany.Flags.Franchise | ICompany.Flags.Holding, true);
		//		}
		//		break;

		//		case ICompany.Kind.Guild:
		//		{
		//			max_primary_industry_flags = 1;
		//			max_secondary_industry_flags = 2;
		//			max_services_flags = 2;

		//			liability_mask.Add(ICompany.Liability.None);
		//			liability_mask.Add(ICompany.Liability.Limited);
		//			liability_mask.Add(ICompany.Liability.Unlimited);

		//			flags_mask.SetFlag(ICompany.Flags.Collective | ICompany.Flags.Public | ICompany.Flags.Criminal | ICompany.Flags.Franchise | ICompany.Flags.Holding, true);
		//		}
		//		break;

		//		case ICompany.Kind.Union:
		//		{
		//			max_primary_industry_flags = 2;
		//			max_secondary_industry_flags = 2;

		//			liability = ICompany.Liability.Unlimited;
		//			liability_mask.Add(ICompany.Liability.Unlimited);

		//			flags.SetFlag(ICompany.Flags.Collective, true);
		//			flags_mask.SetFlag(ICompany.Flags.Collective | ICompany.Flags.Criminal, true);
		//		}
		//		break;

		//		case ICompany.Kind.Mafia:
		//		{
		//			max_primary_industry_flags = 1;
		//			max_secondary_industry_flags = 1;
		//			max_services_flags = 3;

		//			liability = ICompany.Liability.Unlimited;
		//			liability_mask.Add(ICompany.Liability.Unlimited);

		//			flags_mask.SetFlag(ICompany.Flags.Private | ICompany.Flags.Public | ICompany.Flags.Subsidiary | ICompany.Flags.Criminal | ICompany.Flags.Holding, true);
		//		}
		//		break;

		//		case ICompany.Kind.Partnership:
		//		{
		//			max_primary_industry_flags = 1;
		//			max_secondary_industry_flags = 2;
		//			max_services_flags = 2;

		//			liability_mask.Add(ICompany.Liability.Limited);
		//			liability_mask.Add(ICompany.Liability.Unlimited);

		//			flags_mask.SetFlag(ICompany.Flags.Private | ICompany.Flags.Collective | ICompany.Flags.Criminal, true);
		//		}
		//		break;

		//		case ICompany.Kind.Proprietorship:
		//		{
		//			max_primary_industry_flags = 0;
		//			max_secondary_industry_flags = 0;
		//			max_services_flags = 2;

		//			liability = ICompany.Liability.Unlimited;
		//			liability_mask.Add(ICompany.Liability.Unlimited);

		//			flags.SetFlag(ICompany.Flags.Private, true);

		//			flags_mask.SetFlag(ICompany.Flags.Private | ICompany.Flags.Criminal, true);
		//			industry_mask.SetFlag(IMap.Industry.Agricultural | IMap.Industry.Construction | IMap.Industry.Criminal | IMap.Industry.Exploration | IMap.Industry.Food | IMap.Industry.Kobolds | IMap.Industry.Slaves | IMap.Industry.Training | IMap.Industry.Hunting | IMap.Industry.Retail | IMap.Industry.Textile | IMap.Industry.Pharmaceutical | IMap.Industry.Hospitality | IMap.Industry.Financial, true);
		//			services_mask.SetFlag(IMap.Services.Accounting | IMap.Services.Research | IMap.Services.Transport | IMap.Services.Law | IMap.Services.Pawnbroking | IMap.Services.Waste_Management | IMap.Services.Construction | IMap.Services.Advertising | IMap.Services.Entertainment | IMap.Services.Espionage | IMap.Services.Extortion | IMap.Services.Mercenary | IMap.Services.Labor | IMap.Services.Sabotage | IMap.Services.Trading | IMap.Services.Security, true);
		//		}
		//		break;

		//		case ICompany.Kind.Cartel:
		//		{
		//			max_primary_industry_flags = 2;
		//			max_secondary_industry_flags = 3;
		//			max_services_flags = 3;

		//			liability = ICompany.Liability.Unlimited;
		//			liability_mask.Add(ICompany.Liability.Unlimited);

		//			flags_mask.SetFlag(ICompany.Flags.Private | ICompany.Flags.Collective | ICompany.Flags.Franchise | ICompany.Flags.Holding | ICompany.Flags.Criminal, true);
		//		}
		//		break;

		//		case ICompany.Kind.Corporation:
		//		{
		//			max_primary_industry_flags = 3;
		//			max_secondary_industry_flags = 4;
		//			max_services_flags = 5;

		//			liability = ICompany.Liability.Limited;
		//			liability_mask.Add(ICompany.Liability.Limited);

		//			flags_mask.SetFlag(ICompany.Flags.Private | ICompany.Flags.Public | ICompany.Flags.Subsidiary | ICompany.Flags.Criminal | ICompany.Flags.Franchise | ICompany.Flags.Stocks | ICompany.Flags.Holding, true);
		//		}
		//		break;

		//		case ICompany.Kind.Conglomerate:
		//		{
		//			max_primary_industry_flags = 4;
		//			max_secondary_industry_flags = 6;
		//			max_services_flags = 8;

		//			liability_mask.Add(ICompany.Liability.Limited);

		//			flags_mask.SetFlag(ICompany.Flags.Public | ICompany.Flags.Private | ICompany.Flags.Criminal | ICompany.Flags.Holding | ICompany.Flags.Stocks | ICompany.Flags.Political | ICompany.Flags.Government | ICompany.Flags.State_Owned, true);
		//		}
		//		break;

		//		case ICompany.Kind.Institute:
		//		{
		//			//flags.SetFlag(ICompany.Flags.)

		//			max_primary_industry_flags = 1;
		//			max_secondary_industry_flags = 2;
		//			max_services_flags = 4;

		//			liability_mask.Add(ICompany.Liability.Unlimited);
		//			liability_mask.Add(ICompany.Liability.Limited);

		//			flags_mask.SetFlag(ICompany.Flags.Public | ICompany.Flags.Private | ICompany.Flags.Criminal | ICompany.Flags.Subsidiary | ICompany.Flags.State_Owned, true);
		//			industry_mask.SetFlag(IMap.Industry.Arcane | IMap.Industry.Administrative | IMap.Industry.Biology | IMap.Industry.Chemical | IMap.Industry.Education | IMap.Industry.High_Tech | IMap.Industry.Food | IMap.Industry.Kobolds | IMap.Industry.Slaves | IMap.Industry.Training | IMap.Industry.Telecommunications | IMap.Industry.Exploration | IMap.Industry.Media | IMap.Industry.Medical | IMap.Industry.Pharmaceutical | IMap.Industry.Power | IMap.Industry.Research, true);
		//			services_mask.SetFlag(IMap.Services.Education | IMap.Services.Advertising | IMap.Services.Espionage | IMap.Services.Sabotage | IMap.Services.Bureaucracy | IMap.Services.Security | IMap.Services.Law | IMap.Services.Training | IMap.Services.Research | IMap.Services.Propaganda | IMap.Services.Communication, true);
		//		}
		//		break;

		//		case ICompany.Kind.Cult:
		//		{
		//			//max_primary_industry_flags = 1;
		//			//max_secondary_industry_flags = 2;
		//			max_services_flags = 3;

		//			industry = IMap.Industry.None;

		//			liability = ICompany.Liability.Unlimited;
		//			liability_mask.Add(ICompany.Liability.Unlimited);

		//			flags_mask.SetFlag(ICompany.Flags.Private | ICompany.Flags.Collective | ICompany.Flags.Criminal | ICompany.Flags.Township, true);
		//			//industry_mask.SetFlag(IMap.Industry.Education | IMap.Industry.Financial | IMap.Industry.Automotive | IMap.Industry.Hospitality | IMap.Industry.Training | IMap.Industry.Tourism | IMap.Industry.Agricultural | IMap.Industry.Construction | IMap.Industry.Fishing | IMap.Industry.Criminal | IMap.Industry.Tobacco, true);
		//			services_mask.SetFlag(IMap.Services.Education | IMap.Services.Accounting | IMap.Services.Indoctrination | IMap.Services.Training | IMap.Services.Waste_Management | IMap.Services.Propaganda | IMap.Services.Labor | IMap.Services.Construction, true);
		//		}
		//		break;
		//	}


		//	//if (!liability_mask.Has(liability)) liability = ICompany.Liability.Undefined;
		//	flags &= flags_mask;
		//	industry &= industry_mask;
		//	services &= services_mask;
		//	crime &= crime_mask;
		//}

		public partial struct MarketGUI: IGUICommand
		{
			public Entity ent_market;

			public Market.Data market;

			public static ICompany.Handle h_company_selected;
			//public static Market.DEV_CreateCompanyRPC rpc_new_company;

			public static string edit_search_filter;
			public static ICompany.Permits edit_permits_filter;
			public static ICompany.Permits mask_permits_filter = ICompany.Permits.Retail | ICompany.Permits.Wholesale | ICompany.Permits.Manufacturing;

			public void Draw()
			{
				//using (var window = GUI.Window.Interaction("Market | <vacant>###market.gui"u8, this.ent_market))
				using (var window = GUI.Window.Interaction("Market"u8, this.ent_market))
				{
					this.StoreCurrentWindowTypeID(order: -180);
					if (window.show)
					{
						//ref var player = ref Client.GetPlayer();

						//ref var map_info = ref region.GetMapInfo();

						ref var character = ref Client.GetCharacter(out var character_asset);
						ref var region = ref this.ent_market.GetRegionCommon();

						Crafting.Context.NewFromCharacter(ref region, character_asset, ent_producer: this.ent_market, out var context);

						//if (this.market.company.id == 0)
						{
							using (var group_search = GUI.Group.New(size: new(GUI.RmX, 40)))
							{
								if (GUI.TextInput("##search"u8, "search (Ctrl+F)"u8, ref edit_search_filter, new Vector2(GUI.RmX - 40, 40), max_length: 24, show_label: false))
								{

								}
								GUI.FocusOnCtrlF();
							}

							var is_searching = !edit_search_filter.IsNullOrEmpty();

							//var companies_all = ICompany.Database.GetAssets().Where(x => x.id != 0).ToArray();
							using (var scroll = GUI.Scrollbox.New("companies"u8, size: new Vector2(GUI.RmX, GUI.RmY), padding: new(2)))
							{
								GUI.DrawBackground(GUI.tex_frame, scroll.group_frame.GetOuterRect(), new(8, 8, 8, 8));

								foreach (var company_asset in ICompany.Database.GetAssetsSpan())
								{
									if (company_asset.id == 0) continue;
									ref var company_data = ref company_asset.GetData();

									if (company_data.permits.HasNone(ICompany.Permits.Market)) continue;
									if (is_searching && !company_data.name.Contains(edit_search_filter, StringComparison.OrdinalIgnoreCase)) continue;

									//DrawCharacter(characters[i].GetHandle());

									var h_company = company_asset.GetHandle();

									//if (characters.Contains(h_character)) continue;

									using (GUI.ID.Push(h_company))
									{
										using (var group_row = GUI.Group.New(size: new(GUI.RmX, 64)))
										{
											if (group_row.IsVisible())
											{
												DrawCompany(h_company);

												var selected = h_company == h_company_selected;
												if (GUI.Selectable3("selectable"u8, group_row.GetOuterRect(), selected))
												{
													//if (selected) h_company_selected = 0;
													//else h_company_selected = h_company;

													h_company_selected = h_company;
												}

												GUI.FocusableAsset(h_company);
											}
										}
									}
								}
							}

							//GUI.SameLine();

							using (var window_child = window.BeginChildWindow("market.companies.sub"u8, GUI.AlignX.Right, GUI.AlignY.Center, size: new(248, window.group.size.Y - 32), padding: new(8), open: true, flags: GUI.Window.Flags.No_Click_Focus | GUI.Window.Flags.No_Appear_Focus))
							{
								if (window_child.show)
								{
									using (GUI.Group.New(size: GUI.Rm, padding: new(8)))
									{
										//GetCompanyMask(rpc_new_company.kind,
										//	ref rpc_new_company.flags, ref rpc_new_company.industry_primary, ref rpc_new_company.services, ref rpc_new_company.crime, ref rpc_new_company.liability,
										//	out var max_primary_industry_flags, out var max_secondary_industry_flags, out var max_services_flags,
										//	out var mask_flags, out var mask_industry, out var mask_services, out var mask_crime, out var mask_liability);

										ref var company_data = ref h_company_selected.GetData();
										if (company_data.IsNotNull())
										{
											ref var organization_data = ref company_data.h_organization.GetData();

											using (var group = GUI.Group.New(size: new(GUI.RmX, 24), padding: new(0)))
											{
												GUI.TitleCentered(company_data.name, size: 24, pivot: new(0.00f, 1.00f));
												//if (organization_data.IsNotNull()) GUI.TextShadedCentered($"({organization_data.name})", color: GUI.font_color_desc, pivot: new(1.00f, 0.50f));
											}
										}

										GUI.SeparatorThick();
										GUI.NewLine(4);


										using (var group = GUI.Group.New(size: GUI.GetRemainingSpace(y: -48), padding: new(0)))
										{
											using (var group_info = GUI.Group.New(size: new(210, 0), padding: new(4)))
											{
												using (GUI.Wrap.Push(GUI.RmX))
												{
													if (company_data.IsNotNull())
													{
														if (GUI.DrawButton("View Offers"u8, size: new(GUI.RmX, 40)))
														{
															GUI.OpenPopup("market.add_listing"u8);
														}

														//GUI.DrawRect(group_split.GetInnerRect(), Color32BGRA.Yellow, layer: GUI.Layer.Window);
														MarketGUI.DrawOfferSelectionPopup(ref context, this.ent_market, h_company_selected, 0, default, is_readonly: true);
														//GUI.LabelShaded("Level:", company_data.level);
													}
												}
											}

											GUI.SameLine();

											using (var group_info = GUI.Group.New(size: new(GUI.RmX, 0), padding: new(8)))
											{

											}
										}

										GUI.SeparatorThick();

										if (company_data.IsNotNull())
										{
											if (GUI.DrawButton("Rent out"u8, size: new Vector2(96, 40), color: GUI.col_button_yellow))
											{
												var rpc = new DEV_SetCompanyRPC()
												{
													h_company = h_company_selected
												};
												rpc.Send(this.ent_market);
											}

											//GUI.DrawHoverTooltip("Send an offer the selected company to rent and use this property.");
											//GUI.DrawHoverTooltip("Offer rental of this property to the selected company.");
											GUI.DrawHoverTooltip("Offer the selected company a rental of this property."u8);
										}
									}
								}
							}
						}
					}
				}
			}

			public static void DrawCompany(ICompany.Handle h_company)
			{
				const bool compact = true;
				ref var company_data = ref h_company.GetData();

				using (var group = GUI.Group.New(size: GUI.Rm))
				//using (GUI.Clip.Push(group.GetOuterRect()))
				{
					//using (var group_info = GUI.Group.New(size: GUI.Rm, pa))
					{
						var is_hovered = false;

						using (var group_logo = GUI.Group.New(size: new(GUI.RmY), padding: new(6)))
						{
							is_hovered = group_logo.IsHovered();
							group_logo.DrawBackground(company_data.IsNotNull() ? company_data.sprite_frame : new Sprite(GUI.tex_frame_white), color: company_data.IsNotNull() ? GUI.col_default.LumaBlend(company_data.color_brand, 0.75f) : GUI.col_default);

							if (company_data.IsNotNull())
							{
								var sprite_logo = company_data.GetLogo(ICompany.LogoSize.Billboard_3x3);
								GUI.DrawSprite(sprite_logo, group_logo.GetInnerRect(), GUI.Layer.Window);

								if (company_data.sprite_frame_overlay.texture) GUI.DrawBackground(sprite: company_data.sprite_frame_overlay, rect: group_logo.GetOuterRect(), padding: new Vector4(8), color: GUI.col_default.LumaBlend(company_data.color_brand_overlay, 0.75f));
							}
						}

						if (company_data.IsNotNull())
						{
							if (is_hovered)
							{
								using (var tooltip = GUI.Tooltip.New(size: new(256, 0)))
								{
									using (var group_title = GUI.Group.New(size: new(GUI.RmX, 24)))
									{
										GUI.TitleCentered(company_data.GetName(), size: 20, pivot: new(0.00f, 0.50f), offset: new(4, 0));
									}

									GUI.SeparatorThick();
									//GUI.NewLine(4);

									using (var group_desc = GUI.Group.New(size: new(GUI.RmX, 0), padding: new(4)))
									{
										GUI.TextShaded(company_data.motto);
										GUI.TextShaded(company_data.GetDescription());
									}
								}
							}
						}
					}

					GUI.SameLine();

					using (var group_info = GUI.Group.New(size: GUI.Rm, padding: new(4)))
					{
						GUI.DrawBackground(GUI.tex_panel, group_info.GetOuterRect(), new(8));

						//using (var group_left = GUI.Group.New(size: new(GUI.Rm.X - (4), GUI.RmY)))
						{
							if (company_data.IsNotNull())
							{
								ref var organization_data = ref company_data.h_organization.GetData();

								using (var group_title = GUI.Group.New(size: new(GUI.RmX, 20)))
								{
									if (compact)
									{
										GUI.TitleCentered(company_data.GetName(), size: 20, pivot: new(0.00f, 0.00f), offset: new(4, 0));
										//if (ICompany.TryGetImperialName(company_data.h_organization, company_data.flags, out var org_name))
										//{
										//	GUI.TitleCentered(org_name, size: 16, pivot: new(1.00f, 1.00f), offset: new(4, 0));
										//}
									}
									else
									{
										GUI.TitleCentered(company_data.GetName(), size: 20, pivot: new(0.00f, 0.00f), offset: new(4, 0));
										if (ICompany.TryGetImperialName(company_data.h_organization, company_data.flags, out var org_name))
										{
											GUI.TitleCentered(org_name, size: 16, pivot: new(1.00f, 1.00f), offset: new(4, 0));
										}
									}

									//if (organization_data.IsNotNull())
									//{
									//	//var name_gen = $"{company_data.name} {(company_data.flags).ToFormattedString(separator: "-", text_type: Text.Type.Prefix)}{organization_data.name_imperial}{(company_data.flags).ToFormattedString(separator: "-", text_type: Text.Type.Suffix)}";
									//	//var name_gen = $"{(company_data.flags).ToFormattedString(separator: "-", text_type: Text.Type.Prefix)}{organization_data.name_imperial}{(company_data.flags).ToFormattedString(separator: "-", text_type: Text.Type.Suffix)}";
									//	//GUI.TitleCentered(name_gen, size: 24, pivot: new(0.00f, 0.00f), offset: new(4, 0));



									//	GUI.TitleCentered(name_gen, size: 16, pivot: new(1.00f, 1.00f), offset: new(4, 0));
									//}

									//GUI.TitleCentered(company_data.name, size: 24, pivot: new(0.00f, 0.00f), offset: new(4, 0));
									//if (organization_data.IsNotNull()) GUI.TitleCentered($"{company_data.industry_primary.ToFormattedString(text_type: Text.Type.Normal, separator: "-")} {organization_data.name}", size: 16, pivot: new(1.00f, 0.50f), offset: new(-4, 0));
								}

								//GUI.SeparatorThick();

								//if (organization_data.IsNotNull())
								//{
								//	GUI.TextShadedCentered($"{company_data.industry_secondary.GetEnumName(Text.Type.Normal)}", size: 14, pivot: new(1.00f, 0.00f), offset: new(-2, 2));
								//}

								//ref var faction_data = ref company_data.faction.GetData();
								//if (faction_data.IsNotNull())
								//{
								//	GUI.TextShadedCentered(faction_data.name, color: faction_data.color_a, pivot: new(1.00f, 0.00f));
								//}
								//else
								//{
								//	GUI.TextShadedCentered("Independent", color: GUI.font_color_desc, pivot: new(1.00f, 0.00f));
								//}

								//GUI.TextShadedCentered($"({company_data.kind.GetEnumName()})", color: GUI.font_color_desc, pivot: new(1.00f, 0.00f));
								//GUI.TextShadedCentered($"({company_data.industry.ToFormattedString()}, {company_data.kind.GetEnumName()})", color: GUI.font_color_desc, pivot: new(0.00f, 1.00f));

								//using (var group_desc = GUI.Group.New(new(GUI.RmX - 224, GUI.RmY)))
								//{
								//	using (GUI.Wrap.Push(GUI.RmX))
								//	{
								//		//GUI.TextShadedCentered($"{company_data.industry_primary.ToFormattedString()}", color: GUI.font_color_desc, pivot: new(0.00f, 1.00f));
								//		GUI.TextShadedCentered(company_data.desc_short, color: GUI.font_color_desc, pivot: new(0.00f, 0.00f), offset: new(4, 2));
								//	}
								//}

								//GUI.SameLine();

								using (var group_desc = GUI.Group.New(GUI.Rm))
								{
									if (organization_data.IsNotNull())
									{
										if (compact)
										{
											//if (ICompany.TryGetImperialName(company_data.h_organization, company_data.flags, out var org_name))
											//{
											//	GUI.TitleCentered(org_name,
											//		//clip: true,
											//		size: 16, pivot: new(0.00f, 0.00f), color: GUI.font_color_desc, offset: new(2, -2));
											//}
											GUI.TextShadedCentered(company_data.industry_primary.GetEnumName(Text.Type.Normal),
												clip: true,
												size: 14, pivot: new(0.00f, 0.00f), color: GUI.font_color_desc, offset: new(2, -2));
										}
										else
										{
											GUI.TextShadedCentered(company_data.industry_primary.GetEnumName(Text.Type.Normal),
												clip: false,
												size: 14, pivot: new(1.00f, 0.00f), color: GUI.font_color_title, offset: new(-2, 2));
										}
											//GUI.DrawRectFilled(GUI.GetLastItemRect(), layer: GUI.Layer.Foreground);
										//GUI.TextShadedCentered($"{company_data.industry_secondary.GetEnumName(Text.Type.Normal)}", size: 14, pivot: new(1.00f, 1.00f), color: GUI.font_color_desc, offset: new(-2, 2));
									}

									//using (GUI.Wrap.Push(GUI.RmX))
									//{
									//	if (organization_data.IsNotNull())
									//	{
									//		GUI.TextShadedCentered($"{company_data.industry_primary.GetEnumName(Text.Type.Normal)}", 
									//			size: 14, pivot: new(1.00f, 0.00f), color: GUI.font_color_title, offset: new(-2, 2));
									//		//GUI.TextShadedCentered($"{company_data.industry_secondary.GetEnumName(Text.Type.Normal)}", size: 14, pivot: new(1.00f, 1.00f), color: GUI.font_color_desc, offset: new(-2, 2));
									//	}
									//}
								}
							}
							else
							{
								GUI.TitleCentered("<empty>"u8, pivot: new(0.50f));

							}
							//GUI.TextShaded($"{character.gender}");
						}
					}

					//var selected = i == selected_index;
					//if (GUI.Selectable3("selectable", group_row.GetOuterRect(), selected))
					//{
					//	if (selected) selected_index = 0;
					//	else selected_index = i;
					//}
				}
			}

			public static void DrawCompanyLogo(ICompany.Handle h_company, ICompany.LogoSize logo_size = ICompany.LogoSize.Sign_2x2)
			{
				ref var company_data = ref h_company.GetData();

				using (var group_logo = GUI.Group.New(size: new(GUI.RmY), padding: new(6)))
				{
					group_logo.DrawBackground(GUI.tex_frame_white, color: company_data.IsNotNull() ? GUI.col_default.LumaBlend(company_data.color_brand, 0.75f) : GUI.col_default);

					if (company_data.IsNotNull())
					{
						var sprite_logo = company_data.GetLogo(logo_size);
						GUI.DrawSprite(sprite_logo, group_logo.GetInnerRect(), GUI.Layer.Window);
					}
					else
					{
						var sprite_logo = ICompany.GetLogo(ICompany.tex_logo_default, logo_size);
						GUI.DrawSprite(sprite_logo, group_logo.GetInnerRect(), GUI.Layer.Window);
					}
				}


				//var sprite = company_data.GetLogo(logo_size);
				//var sprite_size = (Vector2)sprite.size;
				//var outer_size = sprite_size.ScaleToHeight(GUI.RmY);
				//var inner_size = sprite_size.ScaleToHeight(GUI.RmY - 8, out var scale);

				//using (var group_icon = GUI.Group.Centered(outer_size: outer_size, inner_size: inner_size))
				//{
				//	group_icon.DrawBackground(GUI.tex_frame, inner: false);

				//	if (company_data.IsNotNull())
				//	{
				//		GUI.DrawSpriteCentered(sprite, group_icon.GetInnerRect(), GUI.Layer.Window, scale: scale);
				//	}
				//}
			}

			public static void DrawOfferSelectionPopup(ref Crafting.Context context, Entity ent_market, ICompany.Handle h_company, int index, scoped Span<IOffer.Handle> offers_added_span, bool is_readonly = false)
			{
				ref var company_data = ref h_company.GetData();
				if (company_data.IsNotNull())
				{
					using (var popup = GUI.Modal.New("market.add_listing"u8, size: new Vector2(754, 400), padding: new(6, 12)))
					{
						if (popup.show)
						{
							GUI.DrawWindowBackground(GUI.tex_window_character);

							using (GUI.Group.New(size: new(GUI.RmX, 32)))
							{
								GUI.TitleCentered($"{company_data.name} | Available Trade Offers", size: 32, pivot: new(0.00f, 0.50f), offset: new(8, 0));
							}

							GUI.SeparatorThick();

							using (var scroll = GUI.Scrollbox.New("offers"u8, size: GUI.Rm, padding: new(4)))
							{
								foreach (var s_offer in IOffer.Database.GetAssetsSpan())
								{
									ref var offer_data = ref s_offer.GetData();
									if ((offer_data.h_company == h_company || offer_data.companies_shared.Contains(h_company)) && offer_data.flags.HasAny(IOffer.Flags.Published) && offer_data.flags.HasNone(IOffer.Flags.Hidden))
									{
										var h_offer = s_offer.GetHandle();
										if (offers_added_span.Contains(h_offer))
										{
											continue;
										}

										using (GUI.ID.Push(h_offer))
										{
											using (var group_row = GUI.Group.New(size: new(GUI.RmX, 48)))
											{
												if (group_row.IsVisible())
												{
													GUI.DrawBackground(in group_row, GUI.tex_panel);

													using (var group_row_offer = GUI.Group.New(size: GUI.GetRemainingSpace(x: -72)))
													{
														Office.DrawOffer(h_offer.id, ref context, ref offer_data, is_readonly: true, h_company_self: h_company);
													}

													GUI.SameLine();

													using (var group_row_offer = GUI.Group.New(size: GUI.Rm, padding: new(4)))
													{
														if (GUI.DrawButton("Add"u8, GUI.Rm, color: GUI.col_button_ok, enabled: !is_readonly))
														{
															var rpc = new Market.DEV_SetOfferRPC()
															{
																h_offer = h_offer,
																index = (byte)index
															};
															rpc.Send(ent_market);
														}
													}

													if (GUI.Selectable3(h_offer.id, group_row.GetOuterRect(), selected: false, is_readonly: true))
													{
														//var rpc = new Market.DEV_AddOfferRPC()
														//{
														//	h_offer = h_offer,
														//	index = (byte)index
														//};
														//rpc.Send(ent_market);

													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}


			public static void DrawCompanySmall(ICompany.Handle h_company)
			{
				ref var company_data = ref h_company.GetData();

				DrawCompanyLogo(h_company, ICompany.LogoSize.Sign_2x2);

				GUI.SameLine();

				using (var group_info = GUI.Group.New(size: GUI.Rm, padding: new(4)))
				{
					GUI.DrawBackground(GUI.tex_panel, group_info.GetOuterRect(), new(8, 8, 8, 8));

					if (company_data.IsNotNull())
					{
						GUI.TitleCentered(company_data.name, size: 20, pivot: new(0.00f, 0.00f));
						//GUI.TextShadedCentered($"({company_data.kind.GetEnumName()})", color: GUI.font_color_desc, pivot: new(1.00f, 0.00f));
						//GUI.TextShadedCentered($"({company_data.industry.ToFormattedString()}, {company_data.kind.GetEnumName()})", color: GUI.font_color_desc, pivot: new(0.00f, 1.00f));
						GUI.TextShadedCentered(company_data.industry_primary.ToFormattedString(), color: GUI.font_color_desc, pivot: new(0.00f, 1.00f));
					}
					else
					{
						GUI.Title("<empty>"u8);

					}
					//GUI.TextShaded($"{character.gender}");
				}

				//var selected = i == selected_index;
				//if (GUI.Selectable3("selectable", group_row.GetOuterRect(), selected))
				//{
				//	if (selected) selected_index = 0;
				//	else selected_index = i;
				//}
			}
		}

		public partial struct OffersGUI: IGUICommand
		{
			public Entity ent_market;
			public Market.Data market;
			public Company.Data company;
			//public Money.Data money;
			public World.Global world_global;

			public static ICompany.Handle h_selected_company;
			public static byte? selected_index;

			//public static DEV_EditListingRPC edit_listing_rpc;
			public static Market.Listing edit_listing;

			public static string edit_search_filter;

			public void Draw()
			{
				static bool IsFilterMatch(ref IOffer.Data offer_data, string filter)
				{
					if (offer_data.IsNull()) return false;

					ref var company_data = ref offer_data.h_company.GetData();
					if (company_data.IsNotNull() && (company_data.name.Contains(filter, StringComparison.OrdinalIgnoreCase) || company_data.name_short.Contains(filter, StringComparison.OrdinalIgnoreCase)))
					{
						return true;
					}

					foreach (ref var item in offer_data.items_a)
					{
						if (item.type == Shipment.Item.Type.Undefined) continue;
						else if (item.GetName().Contains(filter, StringComparison.OrdinalIgnoreCase)) return true;
					}

					foreach (ref var item in offer_data.items_b)
					{
						if (item.type == Shipment.Item.Type.Undefined) continue;
						else if (item.GetName().Contains(filter, StringComparison.OrdinalIgnoreCase)) return true;
					}

					return false;
				}

				ref var company_data = ref this.company.h_company.GetData(out var s_company);
				if (company_data.IsNotNull())
				{
					//using (var window = GUI.Window.Interaction($"Market | {company_data.name}", this.ent_market))
					//using (var window = GUI.Window.Interaction($"Market | {company_data.name_short}###market.gui", this.ent_market))
					using (var window = GUI.Window.Interaction("Market"u8, this.ent_market))
					{
						this.StoreCurrentWindowTypeID(order: -180);
						if (window.show)
						{
							ref var character = ref Client.GetCharacter(out var character_asset);
							ref var region = ref this.ent_market.GetRegionCommon();

							var h_faction_market = this.ent_market.GetFactionHandle();

							Crafting.Context.NewFromCharacter(region: ref region,
								h_character: character_asset,
								ent_producer: this.ent_market,
								context: out var context);

							var perms_character = Market.Data.Permissions.None;

							//var h_company_character = ICompany.Handle.None;
							//if (character.IsNotNull())
							//{
							//	h_company_character = character.h_company;
							//}

							ref var store_data = ref this.market.h_store.GetData(out var s_store);
							if (store_data.IsNotNull())
							{
								if (character.IsNotNull())
								{
									if (character.h_company == store_data.h_company)
									{
										perms_character |= market.perms_owner;
									}

									if (!character.faction)
									{
										perms_character |= market.perms_neutral;
									}
									else if (character.faction == h_faction_market)
									{
										perms_character |= market.perms_faction;
									}
									else
									{
										perms_character |= market.perms_enemy;
									}
								}

								//var can_edit = store_data.h_company == h_company_character;

								var listings_span = store_data.listings.AsSpan();

								//var companies_all = ICompany.Database.GetAssets().Where(x => x.id != 0).ToArray();

								//using (var group_row = GUI.Group.New(size: new(GUI.RmX * 0.50f, 44)))
								//{

								//}

								var is_searching = !edit_search_filter.IsNullOrEmpty();

								//using (GUI.Group.New(size: new Vector2(648, GUI.RmY)))
								using (GUI.Group.New(size: GUI.Rm))
								{
									using (GUI.Group.New(size: new(GUI.RmX, 64)))
									{
										MarketGUI.DrawCompany(store_data.h_company);
									}

									GUI.FocusableAsset(store_data.h_company);

									GUI.SeparatorThick();

									if (GUI.TextInput("##search"u8, "search (Ctrl+F)"u8, ref edit_search_filter, new Vector2(GUI.RmX - 40, 40), max_length: 24, show_label: false))
									{

									}
									GUI.FocusOnCtrlF();

									//using (var scroll = GUI.Scrollbox.New("trades", size: new(648, GUI.RmY - 128), padding: new(4)))
									using (var scroll = GUI.Scrollbox.New("trades"u8, size: GUI.Rm, padding: new(4)))
									{
										GUI.DrawBackground(GUI.tex_frame, scroll.group_frame.GetInnerRect(), new(8, 8, 8, 8));

										var goods_produced = company_data.goods_produced;
										var goods_consumed = company_data.goods_consumed;

										//var goods_buy = company_data.goods_buy;
										var offers_added_span_count = 0;
										Span<IOffer.Handle> offers_added_span = stackalloc IOffer.Handle[listings_span.Length];

										for (var i = 0; i < listings_span.Length; i++)
										{
											offers_added_span[offers_added_span_count++] = listings_span[i].h_offer;
										}

										offers_added_span = offers_added_span.Slice(0, offers_added_span_count);

										//GUI.Text(goods_produced.ToFormattedString());
										//GUI.Text(goods_consumed.ToFormattedString());


										if (selected_index.HasValue)
										{
											ref var listing = ref listings_span[selected_index.Value];

											if (listing.h_offer != edit_listing.h_offer)
											{
												selected_index = null;
												edit_listing = default;

												App.WriteLine("reset listing");
											}
											else if (listing.version != edit_listing.version)
											{
												edit_listing = listing;
												App.WriteLine("updated listing");
											}
										}

										var ts = Timestamp.Now();

										var index = 0;
										foreach (ref var listing in listings_span)
										{
											var h_offer = listing.h_offer;
											ref var offer_data = ref h_offer.GetData();

											if (!is_searching || IsFilterMatch(ref offer_data, edit_search_filter))
											{
												using (GUI.ID<Market.OffersGUI, IOffer.Handle>.Push(index))
												{
													using (var group_row = GUI.Group.New(size: new(GUI.RmX, 48)))
													{
														if (group_row.IsVisible())
														{
															var selected = selected_index == index;

															GUI.DrawBackground(in group_row, GUI.tex_panel);

															if (offer_data.IsNotNull())
															{
																Office.DrawOffer(id: h_offer.id, context: ref context, offer_data: ref offer_data, ent_market: this.ent_market, index: (byte)index,
																	h_company_self: this.company.h_company, fee: listing.fee);

																//GUI.SameLine();

																//using (group_row.Split(size: new(120, 40), align_x: GUI.AlignX.Right, align_y: GUI.AlignY.Center, padding: new(0, 0, 4, 0)))
																//{
																//	if (GUI.DrawButton("DEV: Restock", size: GUI.Rm))
																//	{
																//		var rpc = new Market.DEV_RestockRPC()
																//		{
																//			index = (byte)index
																//		};
																//		rpc.Send(this.ent_market);
																//	}
																//}

																if (GUI.Selectable3(h_offer.id, group_row.GetOuterRect(), selected: selected))
																{
																	if (selected)
																	{
																		selected_index = null;
																		edit_listing = default;
																	}
																	else
																	{
																		selected_index = (byte)index;
																		edit_listing = listing;
																	}
																}

																GUI.FocusableAsset(h_offer);
															}
															else
															{
																using (var group_text = GUI.Group.New(size: new(64, GUI.RmY)))
																{
																	GUI.TitleCentered("<empty listing>"u8, pivot: new(0.00f, 0.50f), offset: new(8, 0), size: 16, color: GUI.font_color_desc);
																}

																GUI.SameLine();

																using (var group_split = group_row.Split(size: new(80, 40), align_x: GUI.AlignX.Right, align_y: GUI.AlignY.Center, padding: new(0, 0, 4, 0)))
																{
																	if (GUI.DrawButton("New"u8, size: GUI.Rm, color: GUI.col_button_yellow, error: perms_character.HasNone(Data.Permissions.Add)))
																	{
																		GUI.OpenPopup("market.add_listing"u8);
																	}

																	//GUI.DrawRect(group_split.GetInnerRect(), Color32BGRA.Yellow, layer: GUI.Layer.Window);
																}

																MarketGUI.DrawOfferSelectionPopup(ref context, this.ent_market, this.company.h_company, index, offers_added_span);
															}
														}
													}
												}

												GUI.NewLine(4);
											}

											index++;

											//var ts_elapsed = ts.GetMilliseconds();

											//GUI.Text($"draw in {ts_elapsed:0.0000} ms");
										}
									}
								}

								//window.SetChildWindowOpen("market.trades.sub", open: true);

								//GUI.SameLine();
								//using (var window_child = window.BeginChildWindow("market.trades.sub"u8, GUI.AlignX.Right, GUI.AlignY.Center, size: new(320, window.group.size.Y - 16), padding: new(8), flags: GUI.Window.Flags.No_Click_Focus | GUI.Window.Flags.No_Appear_Focus, open: perms_character.HasAny(Data.Permissions.Edit | Data.Permissions.Restock)))
								if (perms_character.HasAny(Data.Permissions.Edit | Data.Permissions.Restock))
								{
									using (var window_child = window.BeginChildWindow("market.trades.sub"u8, GUI.AlignX.Right, GUI.AlignY.Center, size: new(320, window.group.size.Y - 16), padding: new(8), flags: GUI.Window.Flags.No_Click_Focus | GUI.Window.Flags.No_Appear_Focus))
									{
										if (window_child.show)
										{
											using (GUI.Group.New(size: GUI.Rm))
											{
												using (var group = GUI.Group.New(size: new(GUI.RmX, 32), padding: new(4)))
												{
													if (selected_index.HasValue)
													{
														GUI.TitleCentered("Edit Listing"u8, size: 32, pivot: new(0.00f, 0.50f), offset: new(4, 0));
													}
													else
													{
														if (company_data.IsNotNull())
														{
															GUI.TitleCentered("Edit Market"u8, size: 32, pivot: new(0.00f, 0.50f), offset: new(4, 0));
														}
														else
														{
															GUI.TitleCentered("<no company>"u8, size: 32, pivot: new(0.00f, 0.50f), offset: new(4, 0));
														}
													}
												}

												GUI.SeparatorThick();

												using (var group = GUI.Group.New(size: GUI.Rm, padding: new(4)))
												{
													var dirty = false;

													//using (var group_row = GUI.Group.New(size: new(GUI.RmX, 24)))
													//{
													//	GUI.TitleCentered((selected_index.HasValue ? $"Offer {selected_index.Value + 1:00}" : "<no offer selected>"), pivot: new(0.00f, 0.50f), offset: new(4, 0), size: 24);
													//}

													//GUI.NewLine(2);

													//GUI.SeparatorThick();

													using (var group_details = GUI.Group.New(size: GUI.GetRemainingSpace(y: -40 - 2), padding: new(4)))
													{
														group_details.DrawBackground(GUI.tex_panel, inner: false);

														if (selected_index.TryGetValue(out var selected_index_v))
														{
															ref var offer_data = ref listings_span[selected_index_v].h_offer.GetData();
															if (offer_data.IsNotNull())
															{
																var mask_perms = ~Market.Listing.Permissions.None;
																//using (var group_row = GUI.Group.New(size: new(GUI.RmX, 24)))
																//{
																//	GUI.EnumInput("Flags"u8, ref edit_listing.flags, size: GUI.Rm, label_ratio: 0.80f);
																//}
																mask_perms &= edit_listing.ApplyBuySellRestrictions();

																//GUI.NewLine(0);

																using (var group_row = GUI.Group.New(size: new(GUI.RmX, 0)))
																{
																	using (var group_header = GUI.Group.New(size: new(GUI.RmX, 24)))
																	{
																		GUI.TitleCentered("Permissions"u8, size: 24, pivot: new(0.00f, 0.50f), offset: new(4, 0));
																	}
																	GUI.SeparatorThick();


																	GUI.EnumInput("Owner"u8, ref edit_listing.perms_owner, size: new(GUI.RmX, 24), label_ratio: 0.70f, mask: mask_perms & ~Listing.Permissions.Edit, hide_masked: false);
																	GUI.EnumInput("Faction"u8, ref edit_listing.perms_faction, size: new(GUI.RmX, 24), label_ratio: 0.70f, mask: mask_perms, hide_masked: false);
																	GUI.EnumInput("Ally"u8, ref edit_listing.perms_ally, size: new(GUI.RmX, 24), label_ratio: 0.70f, mask: mask_perms, hide_masked: false);
																	GUI.EnumInput("Neutral"u8, ref edit_listing.perms_neutral, size: new(GUI.RmX, 24), label_ratio: 0.70f, mask: mask_perms, hide_masked: false);
																	GUI.EnumInput("Enemy"u8, ref edit_listing.perms_enemy, size: new(GUI.RmX, 24), label_ratio: 0.70f, mask: mask_perms, hide_masked: false);
																}

																GUI.NewLine(0);

																using (var group_row = GUI.Group.New(size: GUI.Rm))
																{
																	using (var group_header = GUI.Group.New(size: new(GUI.RmX, 24)))
																	{
																		GUI.TitleCentered("Stock"u8, size: 24, pivot: new(0.00f, 0.50f), offset: new(4, 0));

																		using (var group_counter = group_header.Split(size: new(200, 24), align_x: GUI.AlignX.Right, align_y: GUI.AlignY.Center))
																		{
																			using (GUI.ID.Push("listing.restock_amount"u8))
																			{
																				if (GUI.DrawCounter("counter.restock_amount"u8, new Vector2(112, 24), ref edit_listing.stock_amount, step: 1, min: 1, max: 50))
																				{

																				}

																				GUI.SameLine();

																				GUI.TitleCentered("Max Units"u8, pivot: new(0.00f, 0.50f), offset: new(4, 0), size: 16);
																			}
																		}

																		GUI.SeparatorThick();
																	}

																	using (var group_col = GUI.Group.New(size: new((48 * 4) + 8, GUI.RmY)))
																	{
																		//using (var group_supply = GUI.Group.New(size: new(GUI.RmX, (48 * 2) + 24 + 8), padding: new(4)))
																		using (var group_supply = GUI.Group.New(size: new(GUI.RmX, (48 * 2) + 8), padding: new(4)))
																		{
																			group_supply.DrawBackground(GUI.tex_frame);

																			//using (var group_counter = GUI.Group.New(size: new(GUI.RmX, 24)))
																			//{
																			//	using (GUI.ID.Push("listing.restock_amount"u8))
																			//	{
																			//		if (GUI.DrawCounter("counter.restock_amount"u8, new Vector2(112, 24), ref edit_listing.stock_amount, step: 1, min: 1, max: 50))
																			//		{

																			//		}

																			//		GUI.SameLine();

																			//		GUI.TitleCentered("Max Units"u8, pivot: new(0.00f, 0.50f), offset: new(4, 0), size: 16);
																			//	}
																			//}

																			ref var supply = ref this.ent_market.GetIndexPair<Market.Stock.Data>(selected_index_v);
																			if (supply.IsNotNull())
																			{
																				//if (offer_data.flags.HasAny(IOffer.Flags.Buy))
																				{
																					for (var i = 0; i < supply.amounts_a.Length; i++)
																					{

																						//if (item.type != Shipment.Item.Type.Undefined)
																						{
																							//GUI.SameLine();
																							using (var group_slot = GUI.Group.New(size: new(48, 48), padding: new(4)))
																							{
																								group_slot.DrawBackground(GUI.tex_panel, color: GUI.col_white, inner: true);

																								var item = offer_data.items_a[i];
																								if (item.type != Shipment.Item.Type.Undefined)
																								{
																									item.max = item.quantity * edit_listing.stock_amount;
																									item.quantity = supply.amounts_a[i];

																									GUI.DrawItem(ref item, size: GUI.Rm, true);
																									//GUI.DrawItem(ref item, size: GUI.Rm, true, text_color: Color32BGRA.FromHSV(Maths.NormalizeClamp(item.quantity, item.max, 1.00f) * 2.00f, 1.00f, 1.00f));
																								}
																							}
																							GUI.SameLine();
																						}
																					}
																				}

																				GUI.NewLine();

																				//if (offer_data.flags.HasAny(IOffer.Flags.Sell))
																				{
																					for (var i = 0; i < supply.amounts_b.Length; i++)
																					{
																						//if (item.type != Shipment.Item.Type.Undefined)
																						{
																							//GUI.SameLine();
																							using (var group_slot = GUI.Group.New(size: new(48, 48), padding: new(4)))
																							{
																								group_slot.DrawBackground(GUI.tex_panel, color: GUI.col_white, inner: true);

																								var item = offer_data.items_b[i];
																								if (item.type != Shipment.Item.Type.Undefined)
																								{
																									item.max = item.quantity * edit_listing.stock_amount;
																									item.quantity = supply.amounts_b[i];

																									//GUI.DrawItem(ref item, size: GUI.Rm, true);
																									GUI.DrawItem(ref item, size: GUI.Rm, true);
																									//GUI.DrawItem(ref item, size: GUI.Rm, true, text_color: Color32BGRA.FromHSV(Maths.NormalizeClamp(item.quantity, item.max, 1.00f) * 2.00f, 1.00f, 1.00f));
																								}
																							}
																							GUI.SameLine();
																						}
																					}
																				}
																			}
																		}

																		//using (var group_row = GUI.Group.New(size: new(GUI.RmX, 24)))
																		{
																			GUI.EnumInput("Flags"u8, ref edit_listing.flags, size: new(GUI.RmX, 24), label_ratio: 0.75f);
																		}
																		mask_perms &= edit_listing.ApplyBuySellRestrictions();

																		//using (var group_bottom = GUI.Group.New(size: new(GUI.GetLastItemRect().GetWidth(), GUI.RmY), padding: new(0)))
																		//{
																		//	var fee_percent = (int)MathF.Round(edit_listing.fee * 100.00f, 2);
																		//	if (GUI.DrawCounter("fee"u8, size: new(96, 24), value: ref fee_percent, step: 1, min: 0, max: 100))
																		//	{
																		//		//App.WriteLine($"{fee_percent * 0.01f}; {fee_percent / 100.00f}; {fee_percent * MathF.ReciprocalEstimate(100.00f)}");


																		//		edit_listing.fee = (float)MathF.Round(fee_percent / 100.00f, 2);
																		//	}
																		//	GUI.DrawHoverTooltip("Fee"u8);
																		//}
																	}

																	GUI.SameLine();

																	using (var group_right = GUI.Group.New(size: GUI.Rm))
																	{
																		if (GUI.DrawButton("Restock"u8, size: new Vector2(GUI.RmX, 40), enabled: selected_index.HasValue, error: perms_character.HasNone(Data.Permissions.Restock)))
																		{
																			var rpc = new Market.DEV_RestockRPC()
																			{
																				index = (byte)selected_index.Value
																			};
																			rpc.Send(this.ent_market);
																		}

																		var fee_percent = (int)MathF.Round(edit_listing.fee * 100.00f, 2);
																		if (GUI.DrawCounter("fee"u8, size: new(GUI.RmX, 24), value: ref fee_percent, step: 1, min: 0, max: 100, format: Maths.NumberFormat.Percent))
																		{
																			//App.WriteLine($"{fee_percent * 0.01f}; {fee_percent / 100.00f}; {fee_percent * MathF.ReciprocalEstimate(100.00f)}");


																			edit_listing.fee = (float)MathF.Round(fee_percent / 100.00f, 2);
																		}
																		GUI.DrawHoverTooltip("Fee"u8);
																	}
																}


															}
														}
														else
														{
															var rpc = new Market.DEV_EditMarketRPC();

															var mask_perms = ~Market.Data.Permissions.None;
															//using (var group_row = GUI.Group.New(size: new(GUI.RmX, 40)))
															//{
															//	var h_stockpile = store_data.h_stockpile;
															//	if (GUI.AssetInput2("edit.stockpile", ref h_stockpile, size: new Vector2(GUI.RmX, GUI.RmY), tab_height: 32, show_label: false, draw: static (asset, group, is_title) =>
															//	{
															//		ref var data = ref (asset != null ? ref asset.GetData() : ref Unsafe.NullRef<IStockpile.Data>());
															//		var h_company_provider = default(ICompany.Handle);
															//		var name = default(Utf8String);

															//		if (data.IsNotNull())
															//		{
															//			h_company_provider = data.h_company;
															//			name = data.name;
															//		}
															//		else
															//		{
															//			name = "<none>"u8;
															//		}

															//		Market.MarketGUI.DrawCompanyLogo(h_company_provider, ICompany.LogoSize.Sign_2x2);
															//		GUI.SameLine();
															//		GUI.TitleCentered(name, pivot: new(0.00f, 0.00f), offset: new(4, 2));

															//		if (data.IsNotNull())
															//		{
															//			ref var address_data = ref data.h_address.GetData();
															//			if (address_data.IsNotNull())
															//			{
															//				GUI.TextShadedCentered(address_data.name_generated, color: GUI.font_color_desc, pivot: new(0.00f, 1.00f), offset: new(4, -2));
															//			}
															//		}
															//	}))
															//	{
															//		rpc.h_stockpile = h_stockpile;
															//		dirty = true;
															//	}
															//	GUI.FocusableAsset(h_stockpile);
															//	//GUI.EnumInput("Flags", ref edit_listing.flags, size: GUI.Rm, label_ratio: 0.80f);
															//}

															GUI.Title("Permissions"u8, size: 24);
															GUI.SeparatorThick();

															using (var group_row = GUI.Group.New(size: new(GUI.RmX, 0)))
															{
																dirty |= GUI.EnumInput("Owner"u8, in this.market.perms_owner, ref rpc.perms_owner, size: new(GUI.RmX, 24), label_ratio: 0.70f, mask: mask_perms & ~(Market.Data.Permissions.Edit), hide_masked: false);
																dirty |= GUI.EnumInput("Faction"u8, in this.market.perms_faction, ref rpc.perms_faction, size: new(GUI.RmX, 24), label_ratio: 0.70f, mask: mask_perms, hide_masked: false);
																dirty |= GUI.EnumInput("Ally"u8, in this.market.perms_ally, ref rpc.perms_ally, size: new(GUI.RmX, 24), label_ratio: 0.70f, mask: mask_perms, hide_masked: false);
																dirty |= GUI.EnumInput("Neutral"u8, in this.market.perms_neutral, ref rpc.perms_neutral, size: new(GUI.RmX, 24), label_ratio: 0.70f, mask: mask_perms, hide_masked: false);
																dirty |= GUI.EnumInput("Enemy"u8, in this.market.perms_enemy, ref rpc.perms_enemy, size: new(GUI.RmX, 24), label_ratio: 0.70f, mask: mask_perms, hide_masked: false);
															}

															if (dirty)
															{
																App.WriteLine(rpc.perms_owner);
																rpc.Send(this.ent_market);
															}
														}
													}

													GUI.SeparatorThick();

													using (var group_bottom = GUI.Group.New(size: GUI.Rm))
													{
														var changed = false;
														if (selected_index.TryGetValue(out var selected_index_v))
														{
															//var ts = Timestamp.Now();
															changed = !edit_listing.MemCmp(ref store_data.listings[selected_index_v]);
															//var elapsed = ts.GetMilliseconds();
															//GUI.Text($"{elapsed:0.0000} ms");


															if (GUI.DrawButton("Apply"u8, size: new Vector2(80, group_bottom.size.Y), color: GUI.col_button_ok, enabled: changed))
															{
																var rpc = new DEV_EditListingRPC();

																rpc.flags = edit_listing.flags;

																rpc.perms_owner = edit_listing.perms_owner;
																rpc.perms_faction = edit_listing.perms_faction;
																rpc.perms_neutral = edit_listing.perms_neutral;
																rpc.perms_enemy = edit_listing.perms_enemy;
																rpc.perms_ally = edit_listing.perms_ally;
																rpc.fee = edit_listing.fee;

																rpc.stock_amount = edit_listing.stock_amount;
																rpc.index = selected_index.Value;

																rpc.Send(this.ent_market);
															}

															if (World.IsDevMode && GUI.ShowDebugGUI)
															{
																GUI.SameLine();

																if (GUI.DrawButton("DEV: Restock"u8, size: new Vector2(112, group_bottom.size.Y), color: GUI.col_button_debug, enabled: selected_index.HasValue && !changed))
																{
																	var rpc = new Market.DEV_RestockRPC()
																	{
																		index = (byte)selected_index.Value
																	};
																	rpc.Send(this.ent_market);
																}
															}

															GUI.SameLine();

															if (GUI.DrawButton("Remove"u8, size: new Vector2(GUI.RmX, group_bottom.size.Y), color: GUI.col_button_error, enabled: !changed))
															{
																var rpc = new DEV_SetOfferRPC();
																rpc.index = selected_index.Value;

																rpc.Send(this.ent_market);
															}
														}
														else
														{
															if (GUI.DrawButton("Evict"u8, size: new Vector2(80, group_bottom.size.Y), color: GUI.col_button_error, enabled: true))
															{
																var rpc = new Market.DEV_SetCompanyRPC();
																rpc.h_company = default;

																rpc.Send(this.ent_market);
															}

															if (World.IsDevMode && GUI.ShowDebugGUI)
															{
																GUI.SameLine();

																if (GUI.DrawButton("DEV: Recalc."u8, size: new Vector2(112, group_bottom.size.Y), color: GUI.col_button_debug, enabled: true))
																{
																	var rpc = new Market.DEV_InvoiceRPC();
																	rpc.Send(this.ent_market);
																}

																GUI.SameLine();

																if (GUI.DrawButton("DEV: Gen."u8, size: new Vector2(GUI.RmX, group_bottom.size.Y), color: GUI.col_button_debug, enabled: true))
																{
																	var rpc = new Market.DEV_GenerateOffersRPC();
																	rpc.Send(this.ent_market);
																}
															}
														}




														//GUI.TitleCentered(s_offer.Identifier, pivot: new(0.00f, 0.50f), offset: new(8, 0));
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}

		[ISystem.EarlyGUI(ISystem.Mode.Single, ISystem.Scope.Region | ISystem.Scope.Global)]
		public static void OnGUI(Entity entity, [Source.Owned] in Interactable.Data interactable,
		[Source.Owned] in Market.Data market, 
		[Source.Owned, Optional(true)] ref Company.Data company, [Source.Global] in World.Global world_global)
		{
			if (interactable.IsActive())
			{
				if (company.IsNotNull() && company.h_company)
				{
					var gui = new OffersGUI()
					{
						ent_market = entity,

						company = company,
						market = market,
						//money = money,
						world_global = world_global
					};
					gui.Submit();
				}
				else
				{
					var gui = new MarketGUI()
					{
						ent_market = entity,

						market = market,
					};
					gui.Submit();
				}
			}
		}
#endif
	}
}
