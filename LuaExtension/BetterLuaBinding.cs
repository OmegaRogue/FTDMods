using System;
using System.Collections.Generic;
using System.Linq;

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

		public BetterLuaBinding(MainConstruct c) => _c = c;

		public BetterLuaBinding CleanUp()
		{
			_c = (MainConstruct)null;
			return (BetterLuaBinding)null;
		}

		public void LogToHud(string message) => BrilliantSkies.Ui.Special.InfoStore.InfoStore.Add(message);

		public void Log(string message)
		{
			LogMessages.Insert(0, message);
			if (LogMessages.Count <= 100)
				return;
			LogMessages.RemoveAt(100);
		}

		public void ClearLogs() => LogMessages.Clear();

		public Block[] GetBlocksOnVehicle() => GetAllBlocksOnVehicle().ToArray();

		public string GetBlockTypes()
		{
			return string.Join(",",GetAllBlocksOnVehicle().Select(block => block.GetType().Name).Distinct());
		}

		private IEnumerable<Block> GetAllBlocksOnVehicle()
		{
			foreach (var block in _c.AllBasics.AliveAndDead.Blocks.Where(block => !block.IsStructural))
				yield return block;
			foreach (var block in _c.AllBasics.AllSubconstructsBelowUs.SelectMany(sc => sc.AllBasics.AliveAndDead.Blocks.Where(block => !block.IsStructural)))
				yield return block;
		}

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