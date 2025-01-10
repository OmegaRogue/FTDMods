using System;
using System.IO;

using BrilliantSkies.Blocks.Separator;
using BrilliantSkies.Core;
using BrilliantSkies.Core.Constants;
using BrilliantSkies.Core.Timing;
using BrilliantSkies.Core.UiSounds;
using BrilliantSkies.FromTheDepths.Game.UserInterfaces;
using BrilliantSkies.Localisation;
using BrilliantSkies.Localisation.Runtime.FileManagers.Files;
using BrilliantSkies.Ui.Displayer;

using MoonSharp.Interpreter;
using MoonSharp.Interpreter.CoreLib;

using UnityEngine;

#nullable enable

namespace LuaExtension
{
	public class BetterLuaBox : LuaBox, IExtraSeparatingBlockData
	{
		 public new static ILocFile         _locFile;
	private              string           _code        = "function Update(I)\n-- put your code here \nend";
	private              string           _environment = null!;
	public new           BetterLuaBinding binding      = null!;
	public static        BetterLuaBox     ActiveBox    = null!;
	public new           float            ProcessingTime;
	public new           bool             Running; 
	public               Script           Runtime = new();

	// private readonly MemoryStream _stdOut       = new();
	// private readonly MemoryStream _stdErr       = new();
	// private          StreamReader _stdOutReader = null!;
	// private          StreamReader _stdErrReader = null!;

	static BetterLuaBox() => _locFile = Loc.GetFile("Control_LUA_Box");
	public override void BlockStart()
	{
		base.BlockStart();
		binding                    = new BetterLuaBinding(MainConstruct as MainConstruct);
		Runtime.Globals["Vector3"] = (Func<float,float,float,Vector3>)Vector3New;
		Runtime.Globals["Mathf"]   = typeof(Mathf);

		Runtime.Options.DebugPrint = s => binding.LogMessages.Add(s);
		// IoModule.SetDefaultFile(Runtime, MoonSharp.Interpreter.Platforms.StandardFileType.StdOut, _stdOut);
		// IoModule.SetDefaultFile(Runtime, MoonSharp.Interpreter.Platforms.StandardFileType.StdErr, _stdErr);
		
		// _stdOutReader = new StreamReader(_stdOut);
		// _stdErrReader = new StreamReader(_stdErr);
		
		InitialiseLua();
	}
	
	private static Vector3 Vector3New(float x, float y, float z) => new(x, y, z);

	public override void PrepForDelete()
	{
		base.PrepForDelete();
		binding = binding.CleanUp();
	}

	public new async Awaitable<string> SaveScript()
	{
		var path = Get.ProfilePaths.LuaDir().Append(
			$"{(object)MainConstruct.UniqueId}_{(object)IdSet.Id}_{(object)DateTime.Now:yyyyMMdd}.lua").ToString();
		var text = File.CreateText(path);
		await text.WriteLineAsync(_code);
		text.Close();
		return path;
	}

	private async Awaitable FixedStep(ITimeStep dt)
	{
		if (!Running)
			return;
		await RunLuaUpdate();
	}

	public override string SetText(string newText)
	{
		Running = true;
		if (newText == _code)
			return newText;
		_code = !string.IsNullOrEmpty(newText) ? newText : string.Empty;
		GetConstructableOrSubConstructable().AllMultiplayerRestricted.RPCRequest_SyncroniseBlock(this, _code);
		SetLuaCode();
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
															SpecialBasicDescriptionField = _locFile.Get("SpecialDescription", "Allows you to write and execute lua script code to control a variety of vehicle systems")
														};
		interactionReturn.AddExtraLine(_locFile.Get("Tip_PressQToEdit", "Press <<Q>> to edit code"));
		return interactionReturn;
	}

	public override void SetParameters1(Vector4 v) => IdSet.Id.Us = (int) v.w;

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
		if (!change.IsPermanentlyRemovedOrConstructDestroyed || string.IsNullOrEmpty(_environment))
			return;
		var str = @$"


			Vector3 = UnityEngine.Vector3 ;
		    Quaternion = UnityEngine.Quaternion ;
            Mathf = UnityEngine.Mathf ;
            
			sandbox_env{_environment} = nill";
		// Runtime.DoString(str);
		// LuaSvr.mainState.doString(str);
	}

	public void InitialiseLua()
	{
		try
		{
			ActiveBox = this;
			_environment = LuaMaster.Instance.EnvironmentCounter++.ToString();
			var str = $@"
			-- Print contents of `tbl`, with indentation.
			-- `indent` sets the initial level of indentation.
			function tprint (tbl, indent)
			  if not indent then indent = 0 end
			  for k, v in pairs(tbl) do
			    formatting = string.rep(""  "", indent) .. k .. "": ""
			    if type(v) == ""table"" then
			      print(formatting)
			      tprint(v, indent+1)
			    elseif type(v) == 'boolean' then
			      print(formatting .. tostring(v))      
			    else
			      print(formatting .. v)
			    end
			  end
			end

			sandbox_env{_environment} = 
			{{
				ipairs = ipairs,
				next = next,
				pairs = pairs,
				pcall = pcall,
				tonumber = tonumber,
				tostring = tostring,
				type = type,
				unpack = unpack,
                getmetatable = getmetatable,
                setmetatable = setmetatable,
                _VERSION = _VERSION,

				coroutine = {{ create = coroutine.create, resume = coroutine.resume, 
					running = coroutine.running, status = coroutine.status, 
					wrap = coroutine.wrap, yield = coroutine.yield }},

				string = {{ byte = string.byte, char = string.char, find = string.find, 
					format = string.format, gmatch = string.gmatch, gsub = string.gsub, 
					len = string.len, lower = string.lower, match = string.match, 
					rep = string.rep, reverse = string.reverse, sub = string.sub, 
					upper = string.upper }},

				table = {{ insert = table.insert, maxn = table.maxn, remove = table.remove, 
					sort = table.sort, getn = table.getn }},

				math = {{ abs = math.abs, acos = math.acos, asin = math.asin, 
					atan = math.atan, atan2 = math.atan2, ceil = math.ceil, cos = math.cos, 
					cosh = math.cosh, deg = math.deg, exp = math.exp, floor = math.floor, 
					fmod = math.fmod, frexp = math.frexp, huge = math.huge, 
					ldexp = math.ldexp, log = math.log, log10 = math.log10, max = math.max, 
					min = math.min, modf = math.modf, pi = math.pi, pow = math.pow, 
					rad = math.rad, random = math.random, sin = math.sin, sinh = math.sinh, 
					sqrt = math.sqrt, tan = math.tan, tanh = math.tanh,
	                randomseed = math.randomseed}},
			 
				import = {{function() end}},

			    --Quaternion = UnityEngine.Quaternion,
		        --Vector3 = UnityEngine.Vector3,
		        

		        --Mathf = UnityEngine.Mathf,
            
			}}



			function run_sandboxstring{_environment}(untrusted_code)
				if untrusted_code:byte(1) == 27 then 
					error('binary bytecode prohibited') ;
				end

				local untrusted_function, message = loadstring(untrusted_code)

				if not untrusted_function then 
					error(message) ;
				end

				setfenv(untrusted_function, sandbox_env{_environment})
				return pcall(untrusted_function)
			end



			function RunUpdate(I)
				if (Update ~= nil) then
				    Update(I) ;
                end
			end

			function EnterSandboxRunUpdate{_environment}(I)
				setfenv(RunUpdate,sandbox_env{_environment}) ;
				RunUpdate(I) ;
			end
			";
			Runtime.DoString(str);
			// binding.LogMessages.Add(_stdOutReader.ReadToEnd());
			// ErrorStack = _stdErrReader.ReadToEnd();
			
			// LuaSvr.mainState.doString(str);
		}
		catch (ScriptRuntimeException ex)
		{
			// ErrorStack = _stdErrReader.ReadToEnd();
			Debug.LogError(ex.DecoratedMessage);
			ErrorStack =  ex.DecoratedMessage + ex.HelpLink + ex.Message + ex.StackTrace;
		}
	}

	public async Awaitable RunLuaUpdate()
	{
		if (Net.IsClient)
		{
			binding.Log("Automatic Message:- Lua does not run on a multiplayer client- it is only executed on the host.");
		}
		else
		{
			try
			{
				var realtimeSinceStartup = Time.realtimeSinceStartup;
				if (!string.IsNullOrEmpty(_environment))
				{
					ErrorStack = "";
					ActiveBox       = this;
					await Runtime.CallAsync(Runtime.Globals["RunUpdate"], binding);
					
					// Runtime.Call(Runtime.Globals[$"EnterSandboxRunUpdate{_environment}"], binding);
					// LuaSvr.mainState.errorDelegate = UpdateErrorStack;
					 // LuaSvr.mainState.getFunction("EnterSandboxRunUpdate" + _environment).call(binding);
				}
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
	}

	public async Awaitable SetLuaCode()
	{
		try
		{
			if (string.IsNullOrEmpty(_environment))
				return;
			SaveScript();
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
			ErrorStack =  ex.DecoratedMessage + ex.HelpLink + ex.Message + ex.StackTrace;
			Debug.LogError(ex.HelpLink + ex.Message + ex.StackTrace);
			Running = false;
			GUISoundManager.GetSingleton().PlayFailure();
			Debug.Log("SetLuaCode error log: " + ex.DecoratedMessage + ex.StackTrace);
		}
	}
	object IExtraSeparatingBlockData.GetExtraData() => _code;

	void IExtraSeparatingBlockData.SetExtraData(
		object                             data,
		Separator.CoordinateTransformation coordinateTransformation)
	{
		if (!(data is string str))
			return;
		SetText(str);
	}
	}
}