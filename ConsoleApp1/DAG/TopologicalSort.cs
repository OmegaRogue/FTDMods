namespace ConsoleApp1.DAG
{
	public class TopologicalSort
	{
		public Node[] Sort(Node[] start)
		{
			var s = new Queue<Node>();
			var l = new List<Node>();

			while (s.Count != 0)
			{
				var n = s.Dequeue();
				l.Add(n);
				foreach (var child in n.Children)
					if (child.Parents.Any(node => node == n))
						s.Enqueue(child);
				
			}

			return l.ToArray();
		}
	}
}