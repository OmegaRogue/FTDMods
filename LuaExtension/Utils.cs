using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using MoonSharp.Interpreter;

using UnityEngine;

namespace LuaExtension
{
	public static class Utils
	{
		public static Assembly CurrentAssembly => Assembly.GetCallingAssembly();

		public static string FileNameToResource(string file)
		{
			file = file.Replace('/',  '.');
			file = file.Replace('\\', '.');
			return CurrentAssembly.FullName.Split(',').First() + "." + file;
		}

		public static Stream LoadResourceStream(string file)
			=> CurrentAssembly.GetManifestResourceStream(FileNameToResource(file)) ??
			   throw new InvalidOperationException();

		public static async Awaitable<string> LoadResource(string file)
			=> await new StreamReader(LoadResourceStream(file))
				  .ReadToEndAsync();
		
		public static Task<DynValue> DoResourceAsync(
			this Script script,
			string      file,
			Table?      globalContext    = null,
			string?     codeFriendlyName = null)
			=> script.DoStreamAsync(LoadResourceStream(file), globalContext, codeFriendlyName ?? file);
		public static DynValue DoResource(
			this Script script,
			string      file,
			Table?      globalContext    = null,
			string?     codeFriendlyName = null)
			=> script.DoStream(LoadResourceStream(file), globalContext, codeFriendlyName ?? file);
	}
}