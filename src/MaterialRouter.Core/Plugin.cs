using System;
using System.Collections.Generic;
using System.IO;

using ChaCustom;

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

using ExtensibleSaveFormat;

using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker.UI;
using KKAPI.Utilities;

namespace MaterialRouter
{
	[BepInPlugin(GUID, Name, Version)]
	[BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
	[BepInDependency(ExtendedSave.GUID, ExtendedSave.Version)]
#if KK
	[BepInDependency("com.deathweasel.bepinex.materialeditor", "3.1.1")]
#else
	[BepInDependency("com.deathweasel.bepinex.materialeditor", "3.1.2")]
#endif
	[BepInIncompatibility("KK_ClothesLoadOption")]
	public partial class MaterialRouter : BaseUnityPlugin
	{
		public const string GUID = "madevil.kk.mr";
#if DEBUG
		public const string Name = "Material Router (Debug Build)";
#else
		public const string Name = "Material Router";
#endif
		public const string Version = "2.3.1.1";

		internal static ConfigEntry<bool> _cfgDebugMode;
		internal static ConfigEntry<bool> _cfgAutoRefresh;
		internal static ConfigEntry<string> _cfgExportPath;
		internal static ConfigEntry<bool> _cfgDragPass;

		internal static MakerButton _bottonMaterialRouter;

		internal static ManualLogSource _logger;
		internal static MaterialRouter _instance;
		internal static Harmony _hooksInstance;
		internal static Harmony _hooksMakerInstance;

		internal static string _exportSavePath = "";
		internal static Dictionary<string, string> _exportSaveFile = new Dictionary<string, string>() { ["Body"] = "MaterialRouterBody", ["Outfit"] = "MaterialRouterOutfit", ["Outfits"] = "MaterialRouterOutfits" };

		private void Awake()
		{
			_logger = base.Logger;
			_instance = this;

			_cfgDebugMode = Config.Bind("Debug", "Debug Mode", false);

			_cfgAutoRefresh = Config.Bind("Maker", "Auto Refresh", false);
			_cfgAutoRefresh.SettingChanged += (_sender, _args) =>
			{
				if (_charaConfigWindow != null)
				{
					if (_charaConfigWindow._cfgAutoRefresh != _cfgAutoRefresh.Value)
						_charaConfigWindow._cfgAutoRefresh = _cfgAutoRefresh.Value;
				}
			};

			_cfgDragPass = Config.Bind("Maker", "Drag Pass Mode", false, new ConfigDescription("Setting window will not block mouse dragging", null, new ConfigurationManagerAttributes { Order = 15, Browsable = !JetPack.CharaStudio.Running }));
			_cfgDragPass.SettingChanged += (_sender, _args) =>
			{
				if (_charaConfigWindow == null) return;
				if (_charaConfigWindow._passThrough != _cfgDragPass.Value)
				{
					_charaConfigWindow._passThrough = _cfgDragPass.Value;
				}
			};

			_cfgExportPath = Config.Bind("General", "Export Path", Paths.ConfigPath);
			_exportSavePath = _cfgExportPath.Value;
		}

		private void Start()
		{
#if KK
			if (JetPack.MoreAccessories.BuggyBootleg)
			{
#if DEBUG
				if (!JetPack.MoreAccessories.Installed)
				{
					_logger.LogError($"Backward compatibility in BuggyBootleg MoreAccessories is disabled");
					return;
				}
#else
				_logger.LogError($"Could not load {Name} {Version} because it is incompatible with MoreAccessories experimental build");
				return;
#endif
			}

			if (!JetPack.CoordinateLoadOption.Safe)
			{
				_logger.LogError($"Could not load {Name} {Version} because it is incompatible with outdated CoordinateLoadOption");
				return;
			}
#endif
			{
				string _version = "1.4";
				BaseUnityPlugin _instance = JetPack.Toolbox.GetPluginInstance("madevil.kk.MovUrAcc");
				if (_instance != null && !JetPack.Toolbox.PluginVersionCompare(_instance, _version))
				{
					_logger.LogError($"MovUrAcc {_version}+ is required to work properly, version {_instance.Info.Metadata.Version} detected");
					if (!JetPack.Game.ConsoleActive)
						_logger.LogMessage($"[{Name}] MovUrAcc {_version}+ is required to work properly, version {_instance.Info.Metadata.Version} detected");
				}
			}

			{
				string _version = "1.8";
				BaseUnityPlugin _instance = JetPack.Toolbox.GetPluginInstance("madevil.kk.ca");
				if (_instance != null && !JetPack.Toolbox.PluginVersionCompare(_instance, _version))
				{
					_logger.LogError($"Character Accessory {_version}+ is required to work properly, version {_instance.Info.Metadata.Version} detected");
					if (!JetPack.Game.ConsoleActive)
						_logger.LogMessage($"[{Name}] Character Accessory {_version}+ is required to work properly, version {_instance.Info.Metadata.Version} detected");
				}
			}
#if KKS
			InitCardImport();
#endif
			CharacterApi.RegisterExtraBehaviour<MaterialRouterController>(GUID);

			_hooksInstance = Harmony.CreateAndPatchAll(typeof(Hooks), "MaterialRouter");

			_hooksInstance.Patch(JetPack.MaterialEditor.Type["MaterialEditorCharaController"].GetMethod("OnReload", AccessTools.all, null, new[] { typeof(GameMode), typeof(bool) }, null), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.MaterialEditorCharaController_OnReload_Prefix)));
			_hooksInstance.Patch(JetPack.MaterialEditor.Type["MaterialEditorCharaController"].GetMethod("OnCoordinateBeingLoaded", AccessTools.all, null, new[] { typeof(ChaFileCoordinate), typeof(bool) }, null), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.MaterialEditorCharaController_OnCoordinateBeingLoaded_Prefix)));

			InitEvent_KKAPI();
			InitEvent_JetPack();
		}

		internal static void ReloadChara(ChaControl _chaCtrl)
		{
			string _cardPath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Paths.ExecutablePath) + "_MaterialRouter.png");
			_chaCtrl.chaFile.SaveCharaFile(_cardPath, byte.MaxValue, false);
			_chaCtrl.chaFile.LoadFileLimited(_cardPath);
			if (_chaCtrl.chaFile.GetLastErrorCode() != 0)
				throw new Exception("LoadFileLimited failed");
			_chaCtrl.ChangeCoordinateType(true);

			_chaCtrl.Reload();
			CustomBase.Instance.updateCustomUI = true;
		}

		internal static void DebugMsg(LogLevel _level, string _meg)
		{
			if (_cfgDebugMode.Value)
				_logger.Log(_level, _meg);
			else
				_logger.Log(LogLevel.Debug, _meg);
		}

		internal static ChaControl _makerChaCtrl => CustomBase.Instance?.chaCtrl;
		internal static MaterialRouterController _makerPluginCtrl => CustomBase.Instance?.chaCtrl?.gameObject.GetComponent<MaterialRouterController>();
	}
}
