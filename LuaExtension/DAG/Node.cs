using System;
using System.Collections.Generic;

namespace LuaExtension.DAG
{
	public class Node
	{
		public List<Node> Children = new();
		public NodeType   Type;
		public string     Content = string.Empty;
	}
}