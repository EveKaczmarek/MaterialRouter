using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

using ChaCustom;
using UnityEngine;
using UniRx;
using ParadoxNotion.Serialization;

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
	[BepInDependency("marco.kkapi")]
	[BepInDependency("com.deathweasel.bepinex.materialeditor", "2.5")]
	public partial class MaterialRouter : BaseUnityPlugin
	{
		public const string GUID = "madevil.kk.mr";
		public const string PluginName = "Material Router";
		public const string Version = "1.1.2.0";

		internal static ConfigEntry<bool> CfgDebugMode { get; set; }
		internal static ConfigEntry<bool> CfgSkipCloned { get; set; }

		internal static int ExtDataVer = 1;
		internal static string SavePath = "";
		internal static Dictionary<string, string> SaveFile = new Dictionary<string, string>() { ["Body"] = "MaterialRouterBody.json", ["Outfit"] = "MaterialRouterOutfit.json", ["Outfits"] = "MaterialRouterOutfits.json" };
		internal static MakerToggle tglSkipCloned;
		internal static MakerButton btmGetTemplate;
		internal static MakerButton btmImportSetting;
		internal static MakerButton btmRemoveSetting;

		internal static List<string> objClothesNames = new List<string>() { "ct_clothesTop", "ct_clothesBot", "ct_bra", "ct_shorts", "ct_gloves", "ct_panst", "ct_socks", "ct_shoes_inner", "ct_shoes_outer" };
		internal static List<string> objClothesTopPartsNames = new List<string>() { "ct_top_parts_A", "ct_top_parts_B", "ct_top_parts_C" };

		internal static new ManualLogSource Logger;
		internal static MaterialRouter Instance;
		internal static Harmony HooksInstance;
		internal static Harmony HooksMakerInstance;

		private void Awake()
		{
			Logger = base.Logger;
			Instance = this;

			CfgDebugMode = Config.Bind("Debug", "Debug Mode", false);
			CfgSkipCloned = Config.Bind("Maker", "Skip Cloned", true);
			CfgSkipCloned.SettingChanged += (sender, args) =>
			{
				if (MakerAPI.InsideMaker)
					tglSkipCloned.Value = CfgSkipCloned.Value;
			};

			SavePath = Path.Combine(Paths.GameRootPath, "Temp");
		}

		private void Start()
		{
			CharacterApi.RegisterExtraBehaviour<MaterialRouterController>(GUID);

			HooksInstance = Harmony.CreateAndPatchAll(typeof(Hooks));

			BepInEx.Bootstrap.Chainloader.PluginInfos.TryGetValue("com.deathweasel.bepinex.materialeditor", out PluginInfo PluginInfo);
			Type MaterialEditorCharaController = PluginInfo.Instance.GetType().Assembly.GetType("KK_Plugins.MaterialEditor.MaterialEditorCharaController");
			HooksInstance.Patch(MaterialEditorCharaController.GetMethod("OnReload", AccessTools.all, null, new[] { typeof(GameMode), typeof(bool) }, null), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.MaterialEditorCharaController_OnReload_Prefix)));
			HooksInstance.Patch(MaterialEditorCharaController.GetMethod("OnCoordinateBeingLoaded", AccessTools.all, null, new[] { typeof(ChaFileCoordinate), typeof(bool) }, null), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.MaterialEditorCharaController_OnCoordinateBeingLoaded_Prefix)));
			HooksInstance.Patch(MaterialEditorCharaController.GetMethod("CorrectTongue", AccessTools.all, null, new Type[0], null), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.MaterialEditorCharaController_CorrectTongue_Prefix)));
			Type MaterialEditorMaterialAPI = PluginInfo.Instance.GetType().Assembly.GetType("MaterialEditorAPI.MaterialAPI");
			HooksInstance.Patch(MaterialEditorMaterialAPI.GetMethod("SetTexture", AccessTools.all, null, new[] { typeof(GameObject), typeof(string), typeof(string), typeof(Texture2D) }, null), prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.MaterialAPI_SetTexture_Prefix)));

			MakerAPI.MakerBaseLoaded += (object sender, RegisterCustomControlsEvent ev) =>
			{
				HooksMakerInstance = Harmony.CreateAndPatchAll(typeof(HooksMaker));
			};

			MakerAPI.MakerFinishedLoading += (object sender, EventArgs ev) =>
			{
				btmGetTemplate.Visible.OnNext(false);
				btmImportSetting.Visible.OnNext(false);
				btmRemoveSetting.Visible.OnNext(false);
			};

			AccessoriesApi.SelectedMakerAccSlotChanged += (object sender, AccessorySlotEventArgs ev) =>
			{
				InitCurrentSlot();
			};

			MakerAPI.MakerExiting += (object sender, EventArgs ev) =>
			{
				HooksMakerInstance.UnpatchAll(HooksMakerInstance.Id);
				HooksMakerInstance = null;
			};

			AccessoriesApi.AccessoryTransferred += (object sender, AccessoryTransferEventArgs ev) =>
			{
				MaterialRouterController pluginCtrl = GetController(MakerAPI.GetCharacterControl());
				pluginCtrl.AccessoryTransferEvent(ev);
			};

			AccessoriesApi.AccessoriesCopied += (object sender, AccessoryCopyEventArgs ev) =>
			{
				MaterialRouterController pluginCtrl = GetController(MakerAPI.GetCharacterControl());
				pluginCtrl.AccessoryCopyEvent(ev);
			};

			MakerAPI.RegisterCustomSubCategories += (object sender, RegisterSubCategoriesEvent ev) =>
			{
				ChaControl chaCtrl = MakerAPI.GetCharacterControl();
				MaterialRouterController pluginCtrl = GetController(chaCtrl);

				MakerCategory category = new MakerCategory("05_ParameterTop", "tglMaterialRouter", MakerConstants.Parameter.Attribute.Position + 1, "Router");
				ev.AddSubCategory(category);

				ev.AddControl(new MakerText("BodyTrigger", category, this));

				ev.AddControl(new MakerButton("Export", category, this)).OnClick.AddListener(delegate { pluginCtrl.ExportBodyTrigger(); });
				ev.AddControl(new MakerButton("Import", category, this)).OnClick.AddListener(delegate { pluginCtrl.ImportBodyTrigger(); });
				ev.AddControl(new MakerButton("Reset", category, this)).OnClick.AddListener(delegate { pluginCtrl.ResetBodyTrigger(); });

				ev.AddControl(new MakerSeparator(category, this));

				ev.AddControl(new MakerText("OutfitTriggers", category, this));

				ev.AddControl(new MakerButton("Export", category, this)).OnClick.AddListener(delegate { pluginCtrl.ExportOutfitTrigger(); });
				ev.AddControl(new MakerButton("Import", category, this)).OnClick.AddListener(delegate { pluginCtrl.ImportOutfitTrigger(); });
				ev.AddControl(new MakerButton("Reset", category, this)).OnClick.AddListener(delegate { pluginCtrl.ResetOutfitTrigger(); });

				ev.AddControl(new MakerSeparator(category, this));

				ev.AddControl(new MakerText("Config", category, this));

				tglSkipCloned = ev.AddControl(new MakerToggle(category, "Get Template Skip Cloned", CfgSkipCloned.Value, this));
				tglSkipCloned.ValueChanged.Subscribe(value => CfgSkipCloned.Value = value);

				ev.AddControl(new MakerSeparator(category, this));

				ev.AddControl(new MakerText("Tools", category, this));

				ev.AddControl(new MakerButton("Reload", category, Instance)).OnClick.AddListener(delegate
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

				ev.AddControl(new MakerButton("Head Get Template", MakerConstants.Face.All, this)).OnClick.AddListener(() => PrintRendererInfo(chaCtrl, chaCtrl.objHead, true));
				ev.AddControl(new MakerButton("Body Get Template", MakerConstants.Face.All, this)).OnClick.AddListener(() => PrintRendererInfo(chaCtrl, chaCtrl.objBody, true));

				const string labelConsolePrint = "Console Output";
				const string labelGenerateSetting = "Generate Setting";
				const string labelRemoveSetting = "Remove Setting";

				ev.AddControl(new MakerButton(labelConsolePrint, MakerConstants.Clothes.Top, this)).OnClick.AddListener(() => PrintRendererInfo(chaCtrl, chaCtrl.objClothes[0]));
				ev.AddControl(new MakerButton(labelConsolePrint, MakerConstants.Clothes.Bottom, this)).OnClick.AddListener(() => PrintRendererInfo(chaCtrl, chaCtrl.objClothes[1]));
				ev.AddControl(new MakerButton(labelConsolePrint, MakerConstants.Clothes.Bra, this)).OnClick.AddListener(() => PrintRendererInfo(chaCtrl, chaCtrl.objClothes[2]));
				ev.AddControl(new MakerButton(labelConsolePrint, MakerConstants.Clothes.Shorts, this)).OnClick.AddListener(() => PrintRendererInfo(chaCtrl, chaCtrl.objClothes[3]));
				ev.AddControl(new MakerButton(labelConsolePrint, MakerConstants.Clothes.Gloves, this)).OnClick.AddListener(() => PrintRendererInfo(chaCtrl, chaCtrl.objClothes[4]));
				ev.AddControl(new MakerButton(labelConsolePrint, MakerConstants.Clothes.Panst, this)).OnClick.AddListener(() => PrintRendererInfo(chaCtrl, chaCtrl.objClothes[5]));
				ev.AddControl(new MakerButton(labelConsolePrint, MakerConstants.Clothes.Socks, this)).OnClick.AddListener(() => PrintRendererInfo(chaCtrl, chaCtrl.objClothes[6]));
				ev.AddControl(new MakerButton(labelConsolePrint, MakerConstants.Clothes.InnerShoes, this)).OnClick.AddListener(() => PrintRendererInfo(chaCtrl, chaCtrl.objClothes[7]));
				ev.AddControl(new MakerButton(labelConsolePrint, MakerConstants.Clothes.OuterShoes, this)).OnClick.AddListener(() => PrintRendererInfo(chaCtrl, chaCtrl.objClothes[8]));

				btmGetTemplate = MakerAPI.AddAccessoryWindowControl(new MakerButton(labelConsolePrint, null, this));
				btmGetTemplate.OnClick.AddListener(() => PrintRendererInfo(chaCtrl, chaCtrl.GetAccessoryObject(AccessoriesApi.SelectedMakerAccSlot)));
				btmImportSetting = MakerAPI.AddAccessoryWindowControl(new MakerButton(labelGenerateSetting, null, this));
				btmImportSetting.OnClick.AddListener(() => pluginCtrl.ImportFromRendererInfo(AccessoriesApi.SelectedMakerAccSlot));
				btmRemoveSetting = MakerAPI.AddAccessoryWindowControl(new MakerButton(labelRemoveSetting, null, this));
				btmRemoveSetting.OnClick.AddListener(() => pluginCtrl.RemoveAccSlotInfo(AccessoriesApi.SelectedMakerAccSlot));

				ev.AddControl(new MakerButton(labelConsolePrint, MakerConstants.Hair.Back, this)).OnClick.AddListener(() => PrintRendererInfo(chaCtrl, chaCtrl.objHair[0], true));
				ev.AddControl(new MakerButton(labelConsolePrint, MakerConstants.Hair.Front, this)).OnClick.AddListener(() => PrintRendererInfo(chaCtrl, chaCtrl.objHair[1], true));
				ev.AddControl(new MakerButton(labelConsolePrint, MakerConstants.Hair.Side, this)).OnClick.AddListener(() => PrintRendererInfo(chaCtrl, chaCtrl.objHair[2], true));
				ev.AddControl(new MakerButton(labelConsolePrint, MakerConstants.Hair.Extension, this)).OnClick.AddListener(() => PrintRendererInfo(chaCtrl, chaCtrl.objHair[3], true));
			};
		}

		internal static void PrintRendererInfo(ChaControl chaCtrl, GameObject go, bool chara = false)
		{
			if (go == null)
				return;

			MaterialRouterController pluginCtrl = GetController(chaCtrl);
			Renderer[] rends = go.GetComponentsInChildren<Renderer>(true);
			List<RouteRule> rules = new List<RouteRule>();
			int skipped = 0;
			foreach (Renderer rend in rends)
			{
				foreach (Material mat in rend.materials)
				{
					string ObjPath = GetGameObjectPath(rend.transform).Replace(chaCtrl.gameObject.name + "/", "");
					string MatName = mat.NameFormatted();

					RouteRule rule = new RouteRule
					{
						GameObjectPath = ObjPath,
						Action = Action.Clone,
						OldName = MatName,
						NewName = MatName + "_cloned"
					};

					RouteRule exist = null;
					if (chara)
						exist = pluginCtrl.BodyTrigger.Where(x => x.GameObjectPath == ObjPath && x.NewName == MatName).FirstOrDefault();
					else
						exist = pluginCtrl.CurOutfitTrigger.Where(x => x.GameObjectPath == ObjPath && x.NewName == MatName).FirstOrDefault();

					if (exist != null)
					{
						if (CfgSkipCloned.Value)
						{
							skipped++;
							continue;
						}
						else
							rule.Action = exist.Action;
					}
					rules.Add(rule);
				}
			}
			Logger.LogWarning($"cloned/renamed skipped: {skipped}\n" + JSONSerializer.Serialize(rules.GetType(), rules, true));
		}

		internal static void InitCurrentSlot()
		{
			if (!MakerAPI.InsideAndLoaded) return;

			if (MakerAPI.GetCharacterControl()?.GetAccessoryObject(AccessoriesApi.SelectedMakerAccSlot) == null)
			{
				btmGetTemplate.Visible.OnNext(false);
				btmImportSetting.Visible.OnNext(false);
				btmRemoveSetting.Visible.OnNext(false);
			}
			else
			{
				btmGetTemplate.Visible.OnNext(true);
				btmImportSetting.Visible.OnNext(true);
				btmRemoveSetting.Visible.OnNext(true);
			}
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
