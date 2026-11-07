
namespace TC2.Base.Components
{
	//public interface IWeather
	//{

	//}

	public static partial class Weather
	{
		//[ISingleton.Data(persist: false)]
		//public struct Singleton(): ISingleton
		//{
		//	public float speed = 1.00f;
		//}

		[IComponent.Data(Net.SendType.Reliable, scope: IComponent.Scope.Region | IComponent.Scope.Global)]
		public struct Data: IComponent
		{
			public float size;
			public float intensity;
			public float speed;
			public float unused_00;
		}


		//[IComponent.Data(Net.SendType.Reliable)]
		//public struct Clear: IComponent, IWeather
		//{
		//	public float test;
		//}

		//		[IComponent.Data(Net.SendType.Reliable)]
		//		public struct Smog: IComponent, IWeather
		//		{
		//			public float intensity;

		//			[ISystem.LateUpdate(ISystem.Mode.Single, ISystem.Scope.Region)]
		//			public static void Update(ISystem.Info info, [Source.Global] ref Sky.Global sky, [Source.Global] ref Fog.Global fog, [Source.Global] ref Weather.Global weather, [Source.Owned] ref Weather.Smog smog)
		//			{
		//				fog.color_fg.a = 200;
		//				fog.color_bg.a = 255;

		//				fog.min = 0.25f;
		//				fog.max = 1.00f;

		//				fog.scale_0 *= 0.70f;
		//				fog.scale_1 *= 0.90f;
		//				//fog.scale_2 *= 1.00f;
		//				//fog.scale_3 *= 1.50f;
		//				fog.speed *= 1.00f;
		//			}
		//		}

		//#if SERVER
		//		[ChatCommand.Region("weather", "Changes the weather.", creative: true)]
		//		public static void WeatherCommand(ref ChatCommand.Context context, string identifier)
		//		{
		//			App.WriteLine($"weather cmd {identifier}");

		//			ref var region = ref context.GetRegion();
		//			ref var player = ref context.GetPlayer();

		//			ref var info = ref ECS.GetInfo(identifier);
		//			if (info.id != 0 && typeof(IWeather).IsAssignableFrom(info.Type))
		//			{
		//				App.WriteLine($"weather is {info.Type}");

		//				var ent_smog = region.CreateEntity(ECS.EntityKind.Entity);
		//				ent_smog.GetOrAddComponent<Weather.Smog>(sync: true);
		//			}
		//		}
		//#endif
	}


}