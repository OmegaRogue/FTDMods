using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using BrilliantSkies.Core.Constants;
using BrilliantSkies.Core.Help;
using BrilliantSkies.Ftd.Constructs.Modules.Main.Events;

using Ftd.Blocks.BreadBoards.GenericGetter;

using HarmonyLib;

using MoonSharp.Interpreter;

namespace LuaExtension
{
#nullable enable
	[MoonSharpUserData]
	public class BetterLuaBinding
	{
		private MainConstruct       _c;
		public  List<string>        LogMessages = new ();
		private FiredMunitionReturn FMR         = new ();
		
		
		#region VehicleEvents
		
		private Dictionary<string, IVehicleEvent> _eventNamesAndClasses = new();

		private EventProxies _proxy = new();
		
		private long _listenerId = 0;
		[MoonSharpHidden]
		public void CheckEvents()
		{
			foreach (var (name, @event) in _eventNamesAndClasses)
			{
				var v = @event.GetValue(ref _listenerId);
				if (v > 0f)
					_proxy[name].RaiseEvent(this, v);
				
			}
		}

		private class EventProxies
		{
			private readonly Dictionary<string, EventProxy<float>> _proxies = new();

			public EventProxy<float> this[string name]
			{
				get
				{
					if (_proxies.TryGetValue(name, out var proxy))
						return proxy;
					var prox = new EventProxy<float>(typeof(EventProxies).GetEvent(name), this);
					_proxies.Add(name, prox);
					return prox;
				}
				set => _proxies[name] = value;
			}

#pragma warning disable CS0067 // Event is never used
			// ReSharper disable EventNeverSubscribedTo.Local
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
			public event EventHandler<float> CramFire;

			public event EventHandler<float> MissileFired;
			public event EventHandler<float> ApsFired;
			public event EventHandler<float> PulseLaserFired;
			public event EventHandler<float> CwLaserFired;
			public event EventHandler<float> PlasmaFired;
			public event EventHandler<float> FlamerFired;
			public event EventHandler<float> Spawn;
			public event EventHandler<float> BlockLost;
			public event EventHandler<float> OrderGiven;
			public event EventHandler<float> ProjectileWarning;
			public event EventHandler<float> HealthFraction;
			public event EventHandler<float> MaterialsShotUp;
			public event EventHandler<float> MaterialSent;
			public event EventHandler<float> MaterialReceived;
			public event EventHandler<float> EnergySent;
			public event EventHandler<float> EnergyReceived;
			public event EventHandler<float> MaterialDumpFound;
			public event EventHandler<float> TargetSpotted;
			public event EventHandler<float> TargetDead;
			public event EventHandler<float> PrimaryTargetDead;
			public event EventHandler<float> MissileReloaded;
			public event EventHandler<float> Collision;
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

			// ReSharper restore EventNeverSubscribedTo.Local
#pragma warning restore CS0067 // Event is never used
		}

		#endregion
		
		public BetterLuaBinding(MainConstruct c)
		{
			_c = c;
			var properties = _c.Events.GetType().GetProperties();
			foreach (var propertyInfo in properties)
				if (propertyInfo.PropertyType == typeof(VehicleEvent))
					_eventNamesAndClasses.Add(propertyInfo.Name, (VehicleEvent)propertyInfo.GetValue(_c.Events));
				else if (propertyInfo.PropertyType == typeof (VehicleInfo))
					_eventNamesAndClasses.Add(propertyInfo.Name, (VehicleInfo) propertyInfo.GetValue(_c.Events));
		}

		public BetterLuaBinding CleanUp()
		{
			_c = (MainConstruct)null;
			return (BetterLuaBinding)null;
		}

		#region Log

		public void LogToHud(string message) => BrilliantSkies.Ui.Special.InfoStore.InfoStore.Add(message);

		public void Log(string message)
		{
			LogMessages.Insert(0, message);
			if (LogMessages.Count <= 100)
				return;
			LogMessages.RemoveAt(100);
		}

		public void ClearLogs()
		{
			LogMessages.Clear();
			
		}

		#endregion
		

		public Block[] GetBlocksOnVehicle() => GetAllBlocksOnVehicle().ToArray();

		public string GetBlockTypes()
		{
			return string.Join(",",GetAllBlocksOnVehicle().Select(block => block.GetType().Name).Distinct());
		}

		public Block GetBlock(int i = 0, string name = "") => GetAllBlocksOnVehicle().Where(block => name == "" || block.IdSet.Name== name).ToArray()[i];
		private IEnumerable<Block> GetAllBlocksOnVehicle()
		{
			foreach (var block in _c.AllBasics.AliveAndDead.Blocks.Where(block => !block.IsStructural))
				yield return block;
			foreach (var block in _c.AllBasics.AllSubconstructsBelowUs.SelectMany(sc => sc.AllBasics.AliveAndDead.Blocks.Where(block => !block.IsStructural)))
				yield return block;
		}

		public string VariablesForBlocks() => (from assembly in AppDomain.CurrentDomain.GetAssemblies()
											   from type in assembly.GetTypes()
											   where type.IsSubclassOf(typeof(Block))
											   from pair in GetterSourceFinder.GetVariablesForBlockType(type)
											   select
												   $"block: {type.FullName} attributename: {pair.variable.Attribute.Name} propertyname: {pair.variable.Property.Name} variable: {pair.variable} set: {pair.set}").Join(delimiter: "\n");
		
		public string ReadablesForBlocks() => (from assembly in AppDomain.CurrentDomain.GetAssemblies()
											   from type in assembly.GetTypes()
											   where type.IsSubclassOf(typeof(Block))
											   from pair in GetterSourceFinder.GetReadablesForBlockType(type)
											   select
												   $"block: {type.FullName} {pair.property.Name} {pair.attribute.Name}").Join(delimiter: "\n");

		// public float GetVariable(string tag)
		// {
		// 	InternalVariables TheInternalVariables = InternalVariables.Get(TheMainConstruct, true);
		// 	TheInternalVariables.UpdatePeriod(0.1f);
		// 	MoonSharp.Interpreter.Debugging.IDebugger
		// 	return TheInternalVariables.GetValueByTag(tag);
		// }
		// public string GetVariableString(string tag)
		// {
		// 	InternalVariables TheInternalVariables = InternalVariables.Get(TheMainConstruct, true);
		// 	TheInternalVariables.UpdatePeriod(0.1f);
		//
		// 	return TheInternalVariables.GetValueByTag(tag);
		// }


		// public FriendlyInfo GetFriendlyInfo(int index)
		// {
		// 	List<MainConstruct> list = StaticConstructablesManager.Constructables
		// 														  .Where(
		// 															   (Func<MainConstruct, bool>)(t => t is
		// 																		   {
		// 																			   Destroyed: false
		// 																		   } && t      != _c &&
		// 																		   t.GetTeam() == _c.GetTeam()))
		// 														  .ToList();
		// 	return index >= 0 && index < list.Count ? new FriendlyInfo(list[index]) : new FriendlyInfo();
		// }
	}
}