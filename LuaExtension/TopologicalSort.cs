using System.Collections;
using System.Collections.Generic;
using System.Linq;

using BrilliantSkies.Common.Circuits;

namespace LuaExtension
{
	public class TopologicalSort
	{
		public Component[] Sort(Board board)
		{
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