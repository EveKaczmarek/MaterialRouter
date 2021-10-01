using System;
using System.IO;

using ChaCustom;

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker.UI;

namespace MaterialRouter
{
	[BepInPlugin(GUID, Name, Version)]
	[BepInDependency(KoikatuAPI.GUID, KoikatuAPI.VersionConst)]
	[BepInDependency("com.deathweasel.bepinex.materialeditor", "3.1.5")]
	[BepInIncompatibility("KK_ClothesLoadOption")]
	[BepInIncompatibility("com.jim60105.kk.studiocoordinateloadoption")]
	[BepInIncompatibility("com.jim60105.kk.coordinateloadoption")]
	public partial class MaterialRouter : BaseUnityPlugin
	{
		public const string GUID = "madevil.kk.mr";
		public const string Name = "Material Router";
		public const string Version = "2.0.1.0";

		internal static ConfigEntry<bool> _cfgDebugMode;
		internal static ConfigEntry<bool> _cfgAutoRefresh;

		internal static MakerButton _bottonMaterialRouter;

		internal static ManualLogSource _logger;
		internal static MaterialRouter _instance;
		internal static Harmony _hooksInstance;
		internal static Harmony _hooksMakerInstance;

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

			_exportSavePath = Path.Combine(Paths.GameRootPath, "Temp");
		}

		private void Start()
		{
			{
				BaseUnityPlugin _instance = JetPack.Toolbox.GetPluginInstance("madevil.kk.MovUrAcc");
				if (_instance != null && !JetPack.Toolbox.PluginVersionCompare(_instance, "1.4.0.0"))
					_logger.LogError($"MovUrAcc 1.4+ is required to work properly, version {_instance.Info.Metadata.Version} detected");
			}

			{
				BaseUnityPlugin _instance = JetPack.Toolbox.GetPluginInstance("madevil.kk.ca");
				if (_instance != null && !JetPack.Toolbox.PluginVersionCompare(_instance, "1.8.0.0"))
					_logger.LogError($"Character Accessory 1.8+ is required to work properly, version {_instance.Info.Metadata.Version} detected");
			}

			CharacterApi.RegisterExtraBehaviour<MaterialRouterController>(GUID);

			_hooksInstance = Harmony.CreateAndPatchAll(typeof(Hooks), "MaterialRouter");

			_hooksInstance.Patch(JetPack.MaterialEditor.Type["MaterialEditorCharaController"].GetMethod("OnReload", AccessTools.all, null, new[] { typeof(GameMode), typeof(bool) }, null), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.MaterialEditorCharaController_OnReload_Prefix)));
			_hooksInstance.Patch(JetPack.MaterialEditor.Type["MaterialEditorCharaController"].GetMethod("OnCoordinateBeingLoaded", AccessTools.all, null, new[] { typeof(ChaFileCoordinate), typeof(bool) }, null), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.MaterialEditorCharaController_OnCoordinateBeingLoaded_Prefix)));

			InitEvent_KKAPI();
			InitEvent_JetPack();
		}

		internal static void ReloadChara(ChaControl _chaCtrl)
		{
			/*
			_chaCtrl.AssignCoordinate((ChaFileDefine.CoordinateType)_chaCtrl.fileStatus.coordinateType);
			_chaCtrl.ChangeCoordinateType((ChaFileDefine.CoordinateType)_chaCtrl.fileStatus.coordinateType, false);
			*/
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
