using System;
using System.IO;
using System.Reflection;

using BrilliantSkies.Blocks.Separator;
using BrilliantSkies.Core;
using BrilliantSkies.Core.Constants;
using BrilliantSkies.Core.Timing;
using BrilliantSkies.Core.UiSounds;
using BrilliantSkies.FromTheDepths.Game.UserInterfaces;
using BrilliantSkies.Ftd.Constructs.Modules.Main.Events;
using BrilliantSkies.Localisation;
using BrilliantSkies.Localisation.Runtime.FileManagers.Files;
using BrilliantSkies.Ui.Displayer;

using MoonSharp.Interpreter;
using MoonSharp.Interpreter.CoreLib;
using MoonSharp.Interpreter.Loaders;

using UnityEngine;

#nullable enable

namespace LuaExtension
{
	public class BetterLuaBox : BlockWithText, IExtraSeparatingBlockData
	{
		public        string           ErrorStack = "";
		private       string           _code      = @"
function Start(I)
	-- put code here to run once
end

function Update(I)
	-- put code here to run every frame
end
";
		public        BetterLuaBinding binding    = null!;
		public static BetterLuaBox     ActiveBox  = null!;
		public        float            ProcessingTime;
		public        bool             Running;
		public        Script           Runtime    = new();
		private       Closure          _luaUpdate = null!;

		// private readonly MemoryStream _stdOut       = new();
		// private readonly MemoryStream _stdErr       = new();
		// private          StreamReader _stdOutReader = null!;
		// private          StreamReader _stdErrReader = null!;

		static BetterLuaBox() => _locFile = Loc.GetFile("Control_LUA_Box");

		public override void BlockStart()
		{
			base.BlockStart();
			binding                      = new BetterLuaBinding(MainConstruct as MainConstruct);
			Runtime.Options.ScriptLoader = new EmbeddedResourcesScriptLoader();
			Runtime.Globals["Vector3"]   = (Func<float, float, float, Vector3>)Vector3New;
			Runtime.Globals["PID"]   = typeof(LuaPID);
			Runtime.Globals["Mathf"]     = typeof(Mathf);
			Runtime.Globals["Mathematics"]     = typeof(Unity.Mathematics.math);
			// ((MainConstruct as MainConstruct).Events as MainConstructEvents).Collision.;

			Runtime.Options.DebugPrint = s => binding.LogMessages.Add(s);


			// IoModule.SetDefaultFile(Runtime, MoonSharp.Interpreter.Platforms.StandardFileType.StdOut, _stdOut);
			// IoModule.SetDefaultFile(Runtime, MoonSharp.Interpreter.Platforms.StandardFileType.StdErr, _stdErr);

			// _stdOutReader = new StreamReader(_stdOut);
			// _stdErrReader = new StreamReader(_stdErr);

			InitialiseLua().GetAwaiter().GetResult();
		}

		private static Vector3 Vector3New(float x, float y, float z) => new(x, y, z);

		public override void PrepForDelete()
		{
			base.PrepForDelete();
			binding = binding.CleanUp();
		}

		public async Awaitable<string> SaveScript()
		{
			var path = Get.ProfilePaths.LuaDir().Append(
				$"{(object)MainConstruct.UniqueId}_{(object)IdSet.Id}_{(object)DateTime.Now:yyyyMMdd}.lua").ToString();
			var text = File.CreateText(path);
			await text.WriteLineAsync(_code);
			text.Close();
			return path;
		}

		
		private void FixedStep(ITimeStep dt)
		{
			if (!Running)
				return;
			
			// binding.CheckEvents();
			RunLuaUpdate().GetAwaiter().GetResult();
		}

		public override string SetText(string newText)
		{
			Running = true;
			if (newText == _code)
				return newText;
			_code = !string.IsNullOrEmpty(newText) ? newText : string.Empty;
			GetConstructableOrSubConstructable().AllMultiplayerRestricted.RPCRequest_SyncroniseBlock(this, _code);
			SetLuaCode().GetAwaiter().GetResult();
			return newText;
		}

		public override void SyncroniseUpdate(string s1) => SetText(s1);

		public override string GetText() => _code;

		public override void Secondary(Transform T)
		{
			BetterLuaEditor.Instance.ActivateGui(this, GuiActivateType.Force);
		}

		public override InteractionReturn Secondary()
		{
			var interactionReturn = new InteractionReturn
									{
										SpecialNameField = _locFile.Get("SpecialName", "Lua box"),
										SpecialBasicDescriptionField = _locFile.Get("SpecialDescription",
											"Allows you to write and execute lua script code to control a variety of vehicle systems")
									};
			interactionReturn.AddExtraLine(_locFile.Get("Tip_PressQToEdit", "Press <<Q>> to edit code"));
			return interactionReturn;
		}

		public override void SetParameters1(Vector4 v) => IdSet.Id.Us = (int)v.w;

		public override void StateChanged(IBlockStateChange change)
		{
			base.StateChanged(change);
			if (change.InitiatedOrInitiatedInUnrepairedState_OnlyCalledOnce)
			{
				GetConstructableOrSubConstructable().iBlocksWithText.BlocksWithText.Add(this);
				MainConstruct.SchedulerRestricted.RegisterForBeforeFixedUpdate(FixedStep);
			}
			else if (change.IsPermanentlyRemovedOrConstructDestroyed)
			{
				GetConstructableOrSubConstructable().iBlocksWithText.BlocksWithText.Remove(this);
				MainConstruct.SchedulerRestricted.UnregisterForBeforeFixedUpdate(FixedStep);
			}

			if (!change.IsPermanentlyRemovedOrConstructDestroyed)
				return;
// 			var str = @"
//
//
// 			Vector3 = UnityEngine.Vector3 ;
// 		    Quaternion = UnityEngine.Quaternion ;
//             Mathf = UnityEngine.Mathf ;
//             
// 			sandbox_env = nil";

			// Runtime.DoString(str);
			// LuaSvr.mainState.doString(str);
		}

		public async Awaitable InitialiseLua()
		{
			try
			{
				ActiveBox = this;
				await Runtime.DoResourceAsync("Lua/environment.lua");

				// await Runtime.DoStringAsync(str);
				_luaUpdate = Runtime.Globals["RunUpdate"] as Closure;


				// binding.LogMessages.Add(_stdOutReader.ReadToEnd());
				// ErrorStack = _stdErrReader.ReadToEnd();

				// LuaSvr.mainState.doString(str);
			}
			catch (ScriptRuntimeException ex)
			{
				// ErrorStack = _stdErrReader.ReadToEnd();
				Debug.LogError(ex.DecoratedMessage);
				ErrorStack = ex.DecoratedMessage + ex.HelpLink + ex.Message + ex.StackTrace;
			}
		}

		public async Awaitable RunLuaUpdate()
		{
			if (Net.IsClient)
				binding.Log(
					"Automatic Message:- Lua does not run on a multiplayer client- it is only executed on the host.");
			else
				try
				{
					var realtimeSinceStartup = Time.realtimeSinceStartup;

					ErrorStack = "";
					ActiveBox  = this;
					await _luaUpdate.CallAsync(binding);

					// Runtime.Call(Runtime.Globals[$"EnterSandboxRunUpdate{_environment}"], binding);
					// LuaSvr.mainState.errorDelegate = UpdateErrorStack;
					// LuaSvr.mainState.getFunction("EnterSandboxRunUpdate" + _environment).call(binding);

					ProcessingTime = Time.realtimeSinceStartup - realtimeSinceStartup;

					// binding.LogMessages.Add(_stdOutReader.ReadToEnd());
					// ErrorStack = _stdErrReader.ReadToEnd();
				}
				catch (ScriptRuntimeException ex)
				{
					ProcessingTime = 0.0f;
					ErrorStack     = ex.HelpLink + ex.Message + ex.StackTrace;
					Running        = false;
					ErrorStack     = ex.DecoratedMessage + ex.HelpLink + ex.Message + ex.StackTrace;

					// ErrorStack     = _stdErrReader.ReadToEnd();
					Debug.LogError(ex.DecoratedMessage);
					GUISoundManager.GetSingleton().PlayFailure();
				}
		}

		public async Awaitable SetLuaCode()
		{
			try
			{
				await SaveScript();
				ErrorStack = "";

				// LuaSvr.mainState.errorDelegate = UpdateErrorStack;
				// Runtime.Call(Runtime.Globals[$"run_sandboxstring{_environment}"], _code);
				await Runtime.DoStringAsync(_code);

				// LuaSvr.mainState.getFunction("run_sandboxstring" + _environment).call(_code);
				Running = true;

				// binding.LogMessages.Add(_stdOutReader.ReadToEnd());
				// ErrorStack = _stdErrReader.ReadToEnd();
			}
			catch (ScriptRuntimeException ex)
			{
				// ErrorStack = _stdErrReader.ReadToEnd();
				ErrorStack = ex.DecoratedMessage + ex.HelpLink + ex.Message + ex.StackTrace;
				Debug.LogError(ex.HelpLink + ex.Message + ex.StackTrace);
				Running = false;
				GUISoundManager.GetSingleton().PlayFailure();
				Debug.Log("SetLuaCode error log: " + ex.DecoratedMessage + ex.StackTrace);
			}
		}

		object IExtraSeparatingBlockData.GetExtraData() => _code;

		void IExtraSeparatingBlockData.SetExtraData(
			object                             data,
			Separator.CoordinateTransformation coordinateTransformation
		)
		{
			if (data is not string str)
				return;
			SetText(str);
		}
	}
}