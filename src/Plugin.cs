using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using ChaCustom;
using UnityEngine;
using UniRx;

using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Maker.UI;

namespace MaterialRouter
{
	[BepInPlugin(GUID, PluginName, Version)]
	[BepInDependency("marco.kkapi", "1.1.5")]
	[BepInDependency("com.deathweasel.bepinex.materialeditor", "2.5")]
	public partial class Plugin : BaseUnityPlugin
	{
		public const string GUID = "madevil.kk.mr";
		public const string PluginName = "Material Router";
		public const string Version = "1.0.1.0";

		internal static ConfigEntry<bool> CfgDebugMode { get; set; }

		internal static int ExtDataVer = 1;
		internal static string SavePath = "";
		internal static Dictionary<string, string> SaveFile = new Dictionary<string, string>() { ["Body"] = "MaterialRouterBody.json", ["Outfit"] = "MaterialRouterOutfit.json", ["Outfits"] = "MaterialRouterOutfits.json" };

		internal static new ManualLogSource Logger;
		internal static Plugin Instance;
		internal static Harmony HooksInstance;

		private void Awake()
		{
			Logger = base.Logger;
			Instance = this;

			CfgDebugMode = Config.Bind("Debug", "Debug Mode", false);

			SavePath = Path.Combine(Paths.GameRootPath, "Temp");
		}

		private void Start()
		{
			CharacterApi.RegisterExtraBehaviour<MaterialRouterController>(GUID);

			HooksInstance = Harmony.CreateAndPatchAll(typeof(Hooks));

			BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue("com.deathweasel.bepinex.materialeditor", out PluginInfo PluginInfo);
			Type MaterialEditorCharaController = (PluginInfo.Instance.GetType()).Assembly.GetType("KK_Plugins.MaterialEditor.MaterialEditorCharaController");
			HooksInstance.Patch(MaterialEditorCharaController.GetMethod("OnReload", AccessTools.all, null, new[] { typeof(GameMode), typeof(bool) }, null), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.MaterialEditorCharaController_OnReload_Prefix)));
			HooksInstance.Patch(MaterialEditorCharaController.GetMethod("OnCoordinateBeingLoaded", AccessTools.all, null, new[] { typeof(ChaFileCoordinate), typeof(bool) }, null), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.MaterialEditorCharaController_OnCoordinateBeingLoaded_Prefix)));
			HooksInstance.Patch(MaterialEditorCharaController.GetMethod("CorrectTongue", AccessTools.all, null, new Type[0], null), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.MaterialEditorCharaController_CorrectTongue_Prefix)));

			MakerAPI.RegisterCustomSubCategories += (object sender, RegisterSubCategoriesEvent ev) =>
			{
				ChaControl chaCtrl = MakerAPI.GetCharacterControl();
				MaterialRouterController pluginCtrl = chaCtrl?.gameObject?.GetComponent<MaterialRouterController>();

				MakerCategory category = new MakerCategory("05_ParameterTop", "tglMaterialRouter", MakerConstants.Parameter.Attribute.Position + 1, "Router");
				ev.AddSubCategory(category);

				ev.AddControl(new MakerText("BodyTrigger", category, this));

				MakerButton btnExportBody = new MakerButton($"Export", category, Instance);
				ev.AddControl(btnExportBody);
				btnExportBody.OnClick.AddListener(delegate { pluginCtrl.ExportBodyTrigger(); });

				MakerButton btnImportBody = new MakerButton("Import", category, Instance);
				ev.AddControl(btnImportBody);
				btnImportBody.OnClick.AddListener(delegate { pluginCtrl.ImportBodyTrigger(); });

				MakerButton btnResetBody = new MakerButton("Reset", category, Instance);
				ev.AddControl(btnResetBody);
				btnResetBody.OnClick.AddListener(delegate { pluginCtrl.ResetBodyTrigger(); });

				ev.AddControl(new MakerSeparator(category, this));

				ev.AddControl(new MakerText("OutfitTriggers", category, this));

				MakerButton btnExportOutfit = new MakerButton($"Export", category, Instance);
				ev.AddControl(btnExportOutfit);
				btnExportOutfit.OnClick.AddListener(delegate { pluginCtrl.ExportOutfitTrigger(); });

				MakerButton btnImportOutfit = new MakerButton("Import", category, Instance);
				ev.AddControl(btnImportOutfit);
				btnImportOutfit.OnClick.AddListener(delegate { pluginCtrl.ImportOutfitTrigger(); });

				MakerButton btnResetOutfit = new MakerButton("Reset", category, Instance);
				ev.AddControl(btnResetOutfit);
				btnResetOutfit.OnClick.AddListener(delegate { pluginCtrl.ResetOutfitTrigger(); });

				ev.AddControl(new MakerSeparator(category, this));

				ev.AddControl(new MakerText("Tools", category, this));

				MakerButton btnReload = new MakerButton("Reload", category, Instance);
				ev.AddControl(btnReload);
				btnReload.OnClick.AddListener(delegate
				{
					string CardPath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Paths.ExecutablePath) + "_MaterialRouter.png");
					chaCtrl.chaFile.SaveCharaFile(CardPath, byte.MaxValue, false);

					chaCtrl.chaFile.LoadFileLimited(CardPath);
					if (chaCtrl.chaFile.GetLastErrorCode() != 0)
						throw new Exception("LoadFileLimited failed");
					chaCtrl.ChangeCoordinateType(true);
					chaCtrl.Reload();
					CustomBase.Instance.updateCustomUI = true;
				});
			};
		}

		internal static void DebugMsg(LogLevel LogLevel, string LogMsg)
		{
			if (CfgDebugMode.Value)
				Logger.Log(LogLevel, LogMsg);
		}
	}

	public static partial class Extensions
	{
		public static string NameFormatted(this Material go) => go == null ? "" : go.name.Replace("(Instance)", "").Replace(" Instance", "").Trim();
		public static string NameFormatted(this string name) => name.Replace("(Instance)", "").Replace(" Instance", "").Trim();

		public static T[] AddToArray<T>(this T[] arr, T item)
		{
			var list = arr.ToList();
			list.Add(item);
			return list.ToArray();
		}
	}
}
