
//namespace TC2.Base.Components
//{
//	[Obsolete]
//	public static partial class Capturable
//	{
//		[IComponent.Data(Net.SendType.Unreliable, IComponent.Scope.Global | IComponent.Scope.Region)]
//		public partial struct Data(): IComponent
//		{
//			public Crafting.Order order;
//			public IFaction.Handle capturing_faction_id;

//			public float max_distance = 10.00f;
//			public float amount = 1000.00f;
//			public float difficulty = 10.00f;

//			public float defender_modifier = 1.00f;
//			public float attacker_modifier = 1.00f;

//			[Net.Ignore, Save.Ignore]
//			public float next_check;
//		}

//		public static ref Work.Amount GetWork(ref this Capturable.Data capturable)
//		{
//			return ref capturable.order.work[0];
//		}

//		public struct CaptureRPC: Net.IRPC<Capturable.Data>
//		{
//			public IFaction.Handle faction;

//#if SERVER
//			public void Invoke(Net.IRPC.Context rpc, ref Capturable.Data data)
//			{
//				//if (World.IsDevMode())
//				//{
//				//	ref var player = ref rpc.connection.GetPlayer();
//				//	data.capturing_faction_id = player.h_faction;

//				//	ref var order = ref data.order;

//				//	order.amount_multiplier = 1.00f;
//				//	order.flags = Crafting.Order.Flags.In_Progress;

//				//	var work_span = order.work.AsSpan();
//				//	work_span[0] = new Work.Amount("capturing", Work.Amount.Flags.Pending, 0, (byte)data.difficulty, 0, this.faction.id == 0 ? 0.00f : data.amount);

//				//	rpc.entity.SetFaction(this.faction);
//				//	data.Sync(rpc.entity);
//				//}
//			}
//#endif
//		}

//#if SERVER
//		//[ISystem.EarlyUpdate(ISystem.Mode.Single, ISystem.Scope.Region, interval: 3.00f)]
//		//public static void OnUpdate_Dormitory(ISystem.Info info, Entity entity, ref Region.Data region,
//		//[Source.Owned] ref Dormitory.Data dormitory, [Source.Owned] ref Capturable.Data capturable, [Source.Owned] ref Faction.Data faction)
//		//{
//		//	capturable.attacker_modifier = 1.00f;
//		//	capturable.defender_modifier = 1.00f;

//		//	ref var work = ref capturable.GetWork();

//		//	ref var work_data = ref work.type.GetData();
//		//	if (work_data.IsNotNull() && work.remaining > 0.00f && capturable.capturing_faction_id != 0 && capturable.capturing_faction_id != faction.id)
//		//	{
//		//		var friend_count = 0;
//		//		var enemy_count = 0;

//		//		var character_span = dormitory.GetCharacterSpan();
//		//		foreach (var h_character in character_span)
//		//		{
//		//			ref var character_data = ref h_character.GetData();
//		//			if (character_data.IsNotNull())
//		//			{
//		//				if (character_data.faction == faction.id)
//		//				{
//		//					friend_count++;
//		//				}
//		//				else if (character_data.faction != 0)
//		//				{
//		//					enemy_count++;
//		//				}
//		//			}
//		//		}

//		//		capturable.defender_modifier *= MathF.Pow(1.20f, friend_count / (1.00f + (enemy_count * 0.75f)));
//		//		capturable.attacker_modifier *= MathF.Pow(1.15f, enemy_count);
//		//	}
//		//}

//		//[ISystem.Update.A(ISystem.Mode.Single, ISystem.Scope.Region, interval: 0.35f), HasTag("dead", false, Source.Modifier.Owned)]
//		//public static void OnUpdate_Worker(ISystem.Info info, Entity entity, ref Region.Data region, Entity ent_capturable, [Source.Owned] in Transform.Data transform,
//		//[Source.Owned] ref Worker.Data worker, [Source.Owned] ref Worker.State worker_state, [Source.Owned] ref Faction.Data faction_self,
//		//[Source.Work] ref Capturable.Data capturable, [Source.Work] in Transform.Data transform_capturable, [Source.Work] ref Faction.Data faction_capturable)
//		//{
//		//	if (info.WorldTime >= worker_state.next_work && Vector2.DistanceSquared(transform.position, transform_capturable.position) <= (capturable.max_distance * capturable.max_distance))
//		//	{
//		//		var cooldown = 0.20f;

//		//		ref var order = ref capturable.order;
//		//		if (order.flags.HasAll(Crafting.Order.Flags.InProgress))
//		//		{
//		//			var experience = entity.GetComponentWithOwner<Experience.Data>(Relation.Type.Instance);

//		//			if (capturable.capturing_faction_id == 0 || (capturable.capturing_faction_id != faction_self.id && order.work[0].remaining <= 0.00f))
//		//			{
//		//				capturable.capturing_faction_id = faction_self.id;
//		//				capturable.Sync(ent_capturable, true);
//		//			}

//		//			var work_multiplier = 1.00f;
//		//			var difficulty_multiplier = 1.00f;

//		//			if ((faction_self.id != faction_capturable.id && faction_capturable.id != 0) || (capturable.capturing_faction_id != faction_self.id))
//		//			{
//		//				work_multiplier *= -1.00f;
//		//			}

//		//			if (faction_capturable.id != 0)
//		//			{
//		//				if (faction_self.id == faction_capturable.id)
//		//				{
//		//					work_multiplier *= capturable.defender_modifier;
//		//				}
//		//				else
//		//				{
//		//					work_multiplier *= capturable.attacker_modifier;
//		//					difficulty_multiplier *= capturable.defender_modifier;
//		//				}
//		//			}
//		//			else
//		//			{
//		//				//if (faction_self.id == capturable.capturing_faction_id)
//		//				//{
//		//				//	work_multiplier *= capturable.attacker_modifier;
//		//				//	difficulty_multiplier *= capturable.attacker_modifier;
//		//				//}
//		//				//else
//		//				//{
//		//				//	work_multiplier *= capturable.defender_modifier;
//		//				//	difficulty_multiplier *= capturable.defender_modifier;
//		//				//}
//		//			}

//		//			var result = Worker.PerformWork(transform.position, ref region.AsCommon(), ref order, entity, ref worker, ref worker_state, experience, out cooldown, experience_multiplier: Constants.Experience.crafting_experience_multiplier_npc, work_multiplier: work_multiplier, difficulty_multiplier: difficulty_multiplier);

//		//			switch (result)
//		//			{
//		//				case Worker.WorkResult.Progress:
//		//				{
//		//					capturable.Sync(ent_capturable, true);
//		//				}
//		//				break;

//		//				//case Worker.WorkResult.Done:
//		//				//{
//		//				//	worker.work_index = null;
//		//				//	entity.SyncComponent(ref worker);
//		//				//}
//		//				//break;
//		//			}
//		//		}
//		//		else
//		//		{
//		//			worker.ref_order.Reset(); // h_order = default;
//		//			worker.Sync(entity, true);
//		//		}

//		//		worker_state.next_work = info.WorldTime + (cooldown * 5.00f);
//		//	}
//		//}

//		//[ISystem.VeryLateUpdate(ISystem.Mode.Single, ISystem.Scope.Region, interval: 0.50f)]
//		//[MethodImpl(MethodImplOptions.AggressiveInlining)]
//		//public static void OnUpdate(ISystem.Info info, Entity entity, [Source.Owned] ref Capturable.Data capturable, [Source.Owned] ref Faction.Data faction)
//		//{
//		//	if (info.WorldTime >= capturable.next_check)
//		//	{
//		//		if (capturable.next_check > 0.00f)
//		//		{
//		//			ref var order = ref capturable.order;

//		//			var sync = false;

//		//			ref var work = ref order.work[0];

//		//			ref var work_data = ref work.type.GetData();
//		//			if (work_data.IsNotNull())
//		//			{
//		//				if (work.remaining <= 0.00f)
//		//				{
//		//					if (faction.id != 0)
//		//					{
//		//						entity.SetFaction(0);
//		//						//App.WriteLine($"reset faction 0");

//		//						capturable.capturing_faction_id = 0;
//		//						sync = true;
//		//					}
//		//				}
//		//				else if (work.remaining >= capturable.amount)
//		//				{
//		//					if (capturable.capturing_faction_id != 0 && faction.id == 0)
//		//					{
//		//						entity.SetFaction(capturable.capturing_faction_id);
//		//						//App.WriteLine($"set faction {capturable.capturing_faction_id}");

//		//						capturable.capturing_faction_id = 0;
//		//						sync = true;
//		//					}
//		//				}
//		//			}
//		//			else
//		//			{
//		//				order.amount_multiplier = 1.00f;
//		//				order.flags = Crafting.Order.Flags.In_Progress;

//		//				work = new Work.Amount("capturing", Work.Amount.Flags.Pending, 0, (byte)capturable.difficulty, 0, faction.id == 0 ? 0.00f : capturable.amount);
//		//				sync = true;
//		//			}

//		//			if (sync)
//		//			{
//		//				//App.WriteLine($"sync");
//		//				capturable.Sync(entity, true);
//		//			}
//		//		}

//		//		capturable.next_check = info.WorldTime + 1.00f;
//		//	}
//		//}
//#endif

//#if CLIENT
//		public struct CapturableGUI: IGUICommand
//		{
//			public Entity ent_capturable;

//			public Capturable.Data capturable;
//			public Faction.Data faction;

//			[Shitcode]
//			public void Draw()
//			{
//				//using (var window = GUI.Window.InteractionMisc("Capturable"u8, this.ent_capturable, new Vector2(200, 96)))
//				//{
//				//	if (window.show)
//				//	{
//				//		ref var player = ref Client.GetPlayer();
//				//		ref var region = ref ent_capturable.GetRegion();
//				//		ref var experience = ref player.GetExperience().data;
//				//		var ref_worker = player.ent_controlled.GetComponentWithOwner<Worker.Data>(Relation.Type.Instance);

//				//		ref var worker = ref ref_worker.data;
//				//		ref var worker_state = ref ref_worker.entity.GetComponent<Worker.State>();

//				//		ref var order = ref this.capturable.order;
//				//		ref var recipe = ref order.h_recipe.GetData();

//				//		var has_resources = true;
//				//		var has_worker = false;

//				//		var player_faction_id = player.faction_id;
//				//		ref var player_faction_data = ref player_faction_id.GetData();

//				//		var color_faction = Color32BGRA.White;

//				//		ref var capturable_faction_data = ref this.faction.id.GetData();
//				//		if (capturable_faction_data.IsNotNull())
//				//		{
//				//			color_faction = capturable_faction_data.color_a;
//				//		}
//				//		else if (player_faction_data.IsNotNull())
//				//		{
//				//			color_faction = player_faction_data.color_a;
//				//		}

//				//		var work_index = 0;

//				//		using (GUI.Group.New(size: GUI.GetRemainingSpace(y: -48)))
//				//		{
//				//			for (var i = 0; i < 1; i++)
//				//			{
//				//				ref var work = ref order.work[i];
//				//				using (GUI.ID.Push(i))
//				//				{
//				//					using (GUI.Group.New(size: new Vector2(GUI.RmX, 48)))
//				//					{
//				//						GUI.DrawWork(ref experience, work, size: GUI.Rm, Color32BGRA.Lerp(GUI.col_button, color_faction, 0.80f), show_type: false, show_level: false);
//				//						work_index = i;

//				//						//GUI.OffsetLine(GUI.RmX - button_size.X);

//				//						//if (work.type != Work.Type.Undefined)
//				//						//{
//				//						//	var button_color = this.faction.id == player_faction_id ? GUI.col_button_ok : GUI.col_button_error;
//				//						//	if (worker.work_index != i)
//				//						//	{
//				//						//		if (player_faction_id == this.faction.id)
//				//						//		{
//				//						//			if (GUI.DrawButton("Defend", button_size, enabled: true && has_resources, color: button_color))
//				//						//			{
//				//						//				var rpc = new Worker.SetTargetRPC
//				//						//				{
//				//						//					ent_target = this.ent_capturable,
//				//						//					order_index = (ushort)0,
//				//						//					work_index = (ushort)i,
//				//						//				};
//				//						//				rpc.Send(ref_worker.entity);
//				//						//			}
//				//						//		}
//				//						//		else
//				//						//		{
//				//						//			if (GUI.DrawButton("Capture", button_size, enabled: true && has_resources, color: button_color))
//				//						//			{
//				//						//				var rpc = new Worker.SetTargetRPC
//				//						//				{
//				//						//					ent_target = this.ent_capturable,
//				//						//					order_index = (ushort)0,
//				//						//					work_index = (ushort)i,
//				//						//				};
//				//						//				rpc.Send(ref_worker.entity);
//				//						//			}
//				//						//		}
//				//						//	}
//				//						//	else
//				//						//	{
//				//						//		if (GUI.DrawButton((player_faction_id == this.faction.id ? "Defending..." : "Capturing..."), button_size, enabled: true && has_resources, color: button_color))
//				//						//		{
//				//						//			var rpc = new Worker.SetTargetRPC
//				//						//			{
//				//						//				ent_target = default
//				//						//			};
//				//						//			rpc.Send(ref_worker.entity);
//				//						//		}
//				//						//	}
//				//						//}
//				//						//else
//				//						//{
//				//						//	if (GUI.DrawButton("", button_size, enabled: false))
//				//						//	{

//				//						//	}
//				//						//}
//				//					}
//				//				}
//				//			}

//				//			if (World.IsDevMode())
//				//			{
//				//				if (GUI.DrawButton("DEV: Reset", new Vector2(120, 48), color: GUI.col_button_debug))
//				//				{
//				//					var rpc = new Capturable.CaptureRPC()
//				//					{
//				//						faction = 0
//				//					};
//				//					rpc.Send(this.ent_capturable);
//				//				}

//				//				GUI.SameLine();

//				//				if (GUI.DrawButton("DEV: Ally", new Vector2(120, 48), color: GUI.col_button_debug))
//				//				{
//				//					var rpc = new Capturable.CaptureRPC()
//				//					{
//				//						faction = "defenders",
//				//					};
//				//					rpc.Send(this.ent_capturable);
//				//				}

//				//				GUI.SameLine();

//				//				if (GUI.DrawButton("DEV: Enemy", new Vector2(120, 48), color: GUI.col_button_debug))
//				//				{
//				//					var rpc = new Capturable.CaptureRPC()
//				//					{
//				//						faction = "attackers"
//				//					};
//				//					rpc.Send(this.ent_capturable);
//				//				}
//				//			}

//				//			//GUI.SameLine();

//				//			//GUI.DrawWorkH(0.50f, size: new Vector2(GUI.RmX, 48));
//				//		}

//				//		using (var group_bottom = GUI.Group.New(size: GUI.Rm))
//				//		{
//				//			using (group_bottom.Split(size: group_bottom.size with { X = 80 }, GUI.AlignX.Left, GUI.AlignY.Center))
//				//			{
//				//				//GUI.TitleCentered(""u8, pivot: new(0.50f, 0.00f));
//				//			}

//				//			GUI.SameLine();

//				//			//using (GUI.Group.New(size: GUI.Rm)

//				//			ref var work = ref order.work[work_index];

//				//			ref var work_data = ref work.type.GetData();
//				//			if (work_data.IsNotNull())
//				//			{
//				//				var button_color = this.faction.id == player_faction_id ? GUI.col_button_ok : GUI.col_button_error;
//				//				if (worker_state.work_index != work_index)
//				//				{
//				//					if (player_faction_id == this.faction.id)
//				//					{
//				//						if (GUI.DrawButton("Defend"u8, size: GUI.Rm, enabled: true && has_resources, color: button_color))
//				//						{
//				//							var rpc = new Worker.SetTargetRPC
//				//							{
//				//								ent_target = this.ent_capturable,
//				//								h_order = IComponent.Handle.FromComponentPair<Capturable.Data, Crafting.Order>(),
//				//								work_index = (byte)work_index,
//				//							};
//				//							rpc.Send(ref_worker.entity);
//				//						}
//				//					}
//				//					else
//				//					{
//				//						if (GUI.DrawButton("Capture"u8, size: GUI.Rm, enabled: true && has_resources, color: button_color))
//				//						{
//				//							var rpc = new Worker.SetTargetRPC
//				//							{
//				//								ent_target = this.ent_capturable,
//				//								h_order = IComponent.Handle.FromComponentPair<Capturable.Data, Crafting.Order>(),
//				//								work_index = (byte)work_index,
//				//							};
//				//							rpc.Send(ref_worker.entity);
//				//						}
//				//					}
//				//				}
//				//				else
//				//				{
//				//					if (GUI.DrawButton((player_faction_id == this.faction.id ? "Defending..." : "Capturing..."), size: GUI.Rm, enabled: true && has_resources, color: GUI.col_button))
//				//					{
//				//						var rpc = new Worker.SetTargetRPC
//				//						{
//				//							ent_target = default
//				//						};
//				//						rpc.Send(ref_worker.entity);
//				//					}
//				//				}
//				//			}
//				//			else
//				//			{
//				//				if (GUI.DrawButton(""u8, size: GUI.Rm, enabled: false))
//				//				{

//				//				}
//				//			}
//				//		}
//				//	}
//				//}
//			}
//		}

//		//[ISystem.GUI(ISystem.Mode.Single, ISystem.Scope.Region)]
//		//public static void OnGUI(Entity entity, [Source.Owned] in Capturable.Data capturable, [Source.Owned] in Interactable.Data interactable, [Source.Owned] in Faction.Data faction)
//		//{
//		//	if (interactable.IsActive())
//		//	{
//		//		var gui = new CapturableGUI()
//		//		{
//		//			ent_capturable = entity,

//		//			capturable = capturable,
//		//			faction = faction
//		//		};
//		//		gui.Submit();
//		//	}
//		//}
//#endif
//	}
//}
