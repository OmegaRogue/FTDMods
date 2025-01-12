using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LuaExtension.DAG
{
	public class FunctionNode
	{
		public List<Node> Parameters = new();
		public List<Node> Outputs    = new();
		public Node       Body;

		public FunctionNode(string luaCode)
		{
			var returns      = luaCode.Split("return") ?? [];
			var returnValues = new List<string[]>();
			foreach (var s in returns.Skip(1))
			{
				var ret   = s.Split().Where(s1 => !string.IsNullOrEmpty(s1)).ToArray();
				var build = new StringBuilder();
				build.Append(ret[0]);
				var continued     = false;
				foreach (var c in ret.Skip(1))
				{
					var lastContinued = continued;
					continued     = c.EndsWith(',');
					if (!(continued || lastContinued))
						break;
					build.Append(c);
				}
				returnValues.Add(build.ToString().Split(","));
	
			}
			returnValues.Sort((strings, strings1) => strings.Length>strings1.Length?1:strings.Length<strings1.Length?-1:0);
			returnValues.Reverse();


			var mainSplit = luaCode.Split("()".ToCharArray());
			var param     = mainSplit[1].Split(',');
			var retur     = returnValues.First();

			Outputs = retur.Select(s => new Node { Content = s, Type = NodeType.Output }).ToList();
			Body = new Node { Content = mainSplit.Last().Split("end").First(), Type = NodeType.Function, Children = Outputs };
			Parameters = param.Select(s => new Node { Content = s, Type = NodeType.Input, Children = [Body]}).ToList();
		}

		public override string ToString()
		{
			var param = Parameters.Select(node => node.Content).Aggregate((a, b) => $"{a}, {b}") ?? string.Empty;
			return $"function({param}) {Body.Content} end";
		}
	}
}