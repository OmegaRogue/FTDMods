namespace ConsoleApp1.DAG
{
	public class Node
	{
		
		public Node[]   Parents  = [];
		public Node[]   Children = [];
		public NodeType Type;
		public string   Content = string.Empty;
	}
}