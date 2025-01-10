using System;

using BrilliantSkies.Modding;

using HarmonyLib;

using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;

using UnityEngine; // It contains 'GamePlugin' and 'GamePlugin_PostLoad'


namespace LuaExtension
{
	/// <summary>
	/// This class is implementing 'GamePlugin' or 'GamePlugin_PostLoad', it is the entry point
	/// </summary>
	public class FtDInterface : GamePlugin_PostLoad
	{
		/// <summary>Used in FtD's log to indicate the name of the loaded plugin</summary>
		public string name => "LuaExtension";

		/// <summary>Not used in FtD</summary>
		public Version version => new Version(1, 0);

		/// <summary>
		/// Called by FtD when the plugin is loaded
		/// </summary>
		public void OnLoad()
		{
			UserData.RegistrationPolicy = InteropRegistrationPolicy.Automatic;
			UserData.RegisterAssembly();
			UserData.RegisterType<Vector2>();
			UserData.RegisterType<Vector3>();
			UserData.RegisterType<Vector4>();
			UserData.RegisterType<Matrix4x4>();
			UserData.RegisterType<Quaternion>();
			UserData.RegisterType<Color>();
			UserData.RegisterType<Color32>();
			UserData.RegisterType<Mathf>();
		}

		/// <summary>
		/// Called when all the 'OnLoad()' methods of all the plugins have been called
		///
		/// Specific from 'GamePlugin_PostLoad'
		/// </summary>
		/// <returns>'true' if no problem, 'false' if any problem</returns>
		public bool AfterAllPluginsLoaded() => true;

		/// <summary>
		/// Not called by FtD
		/// </summary>
		public void OnSave() { }
	}
}