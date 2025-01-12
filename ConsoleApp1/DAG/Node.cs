namespace ConsoleApp1.DAG
{
	public class Node
	{
		public List<Node> Children = new();
		public NodeType   Type;
		public string     Content = string.Empty;
	}
}