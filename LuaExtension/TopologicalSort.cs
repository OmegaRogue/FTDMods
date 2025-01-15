using System.Collections;
using System.Collections.Generic;
using System.Linq;

using BrilliantSkies.Common.Circuits;
using BrilliantSkies.Common.Circuits.ComponentTypes.Inputs;

namespace LuaExtension
{
	public class TopologicalSort
	{
		public Component[] Sort(Board board)
		{

			board.Components.First().BInputs.;
			var t = (board.Components.First()).BInputs[0].OurOutput.Them.Index
			var t = (board.Components.First()).AOutputs[0].OurInputs.Them;
			// board.Components.GetEnumerator().Current.First().AOutputs;
			var s = new Queue<Component>(board.Components.Where(comp => comp.BInputs.Count == 0));

			var l = new List<Component>();

			while (s.Count != 0)
			{
				var n = s.Dequeue();
				l.Add(n);
				foreach (var child in n.AOutputs)
				foreach (var bInput in child.OurComponent.BInputs)
					if (bInput.OurComponent != n)
						s.Enqueue(child.OurComponent);
			}

			return l.ToArray();
		}
	}
}