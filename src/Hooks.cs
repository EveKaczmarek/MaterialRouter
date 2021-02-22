using System;
using System.Collections.Generic;

using ChaCustom;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using BepInEx.Logging;
using HarmonyLib;

using KKAPI.Chara;
using KKAPI.Maker;

namespace MaterialRouter
{
	public partial class MaterialRouter
	{
		internal static string GetGameObjectPath(Transform transform)
		{
			string path = transform.name;
			while (transform.parent != null)
			{
				transform = transform.parent;
				path = transform.name + "/" + path;
			}
			return path;
		}

		internal static Dictionary<ChaControl, List<string>> NewNameList = new Dictionary<ChaControl, List<string>>();

		internal class Hooks
		{
			[HarmonyBefore(new string[] { "com.deathweasel.bepinex.materialeditor" })]
			[HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateType), typeof(ChaFileDefine.CoordinateType), typeof(bool))]
			internal static void ChaControl_ChangeCoordinateType_Prefix(ChaControl __instance)
			{
				MaterialRouterController pluginCtrl = GetController(__instance);
				if (pluginCtrl == null) return;
				DebugMsg(LogLevel.Warning, $"ChaControl_ChangeCoordinateType_Prefix [{pluginCtrl.CurrentCoordinateIndex}]");
				pluginCtrl.BuildCheckList();
				pluginCtrl.ApplyOutfitTrigger();
			}

			[HarmonyBefore(new string[] { "com.deathweasel.bepinex.materialeditor" })]
			[HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateTypeAndReload), new[] { typeof(bool) })]
			internal static void ChaControl_ChangeCoordinateTypeAndReload_Postfix(ChaControl __instance)
			{
				MaterialRouterController pluginCtrl = GetController(__instance);
				if (pluginCtrl == null) return;
				DebugMsg(LogLevel.Warning, $"ChaControl_ChangeCoordinateTypeAndReload_Postfix [{pluginCtrl.CurrentCoordinateIndex}]");
				pluginCtrl.BuildCheckList();
				pluginCtrl.ApplyOutfitTrigger();
			}

			[HarmonyBefore(new string[] { "com.deathweasel.bepinex.materialeditor" })]
			[HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateTypeAndReload), new[] { typeof(ChaFileDefine.CoordinateType), typeof(bool) })]
			internal static void ChaControl_ChangeCoordinateTypeAndReload_Postfix2(ChaControl __instance)
			{
				MaterialRouterController pluginCtrl = GetController(__instance);
				if (pluginCtrl == null) return;
				DebugMsg(LogLevel.Warning, $"ChaControl_ChangeCoordinateTypeAndReload_Postfix2 [{pluginCtrl.CurrentCoordinateIndex}]");
			}

			internal static void MaterialEditorCharaController_OnReload_Prefix(CharaCustomFunctionController __instance)
			{
				ChaControl chaCtrl = __instance.ChaControl;
				MaterialRouterController pluginCtrl = GetController(chaCtrl);
				if (pluginCtrl == null) return;
				DebugMsg(LogLevel.Warning, $"MaterialEditorCharaController_OnReload_Prefix [{pluginCtrl.CurrentCoordinateIndex}]");
				pluginCtrl.OnReload_Prefix();
			}

			internal static void MaterialEditorCharaController_OnCoordinateBeingLoaded_Prefix(CharaCustomFunctionController __instance, ChaFileCoordinate __0)
			{
				ChaControl chaCtrl = __instance.ChaControl;
				MaterialRouterController pluginCtrl = GetController(chaCtrl);
				if (pluginCtrl == null) return;
				DebugMsg(LogLevel.Warning, $"MaterialEditorCharaController_OnCoordinateBeingLoaded_Prefix [{pluginCtrl.CurrentCoordinateIndex}]");
				pluginCtrl.OnCoordinateBeingLoaded_Prefix(__0);
			}

			internal static void MaterialEditorCharaController_CorrectTongue_Prefix(CharaCustomFunctionController __instance)
			{
				ChaControl chaCtrl = __instance.ChaControl;
				MaterialRouterController pluginCtrl = GetController(chaCtrl);
				if (pluginCtrl == null) return;
				DebugMsg(LogLevel.Warning, $"MaterialEditorCharaController_CorrectTongue_Prefix [{pluginCtrl.CurrentCoordinateIndex}]");
				pluginCtrl.CorrectTongue_Prefix();
			}

			internal static bool MaterialAPI_SetTexture_Prefix(GameObject __0, string __1)
			{
				if (__0 == null)
					return true; // let ME handle it
				ChaControl chaCtrl = __0.GetComponentInParent<ChaControl>();
				if (chaCtrl == null || !NewNameList.ContainsKey(chaCtrl)) return true;
				if (NewNameList[chaCtrl].Count == 0 || NewNameList[chaCtrl].IndexOf(__1) < 0) return true;
				Renderer[] renderers = __0.GetComponentsInChildren<Renderer>(true);
				foreach (Renderer renderer in renderers)
				{
					for (int i = 0; i < renderer.materials.Length; i++)
					{
						if (renderer.materials[i].NameFormatted() == __1)
						{
							DebugMsg(LogLevel.Warning, $"[MaterialAPI_SetTexture_Prefix][{__0.name}][{__1}] cloned/renamed material found");
							return true;
						}
					}
				}
				return false;
			}
		}

		internal class HooksMaker
		{
			[HarmonyBefore(new string[] { "com.deathweasel.bepinex.materialeditor" })]
			[HarmonyPostfix, HarmonyPatch(typeof(CvsClothesCopy), "CopyClothes")]
			internal static void CvsClothesCopy_CopyClothes_Postfix(TMP_Dropdown[] ___ddCoordeType, Toggle[] ___tglKind)
			{
				List<int> copySlots = new List<int>();
				for (int i = 0; i < Enum.GetNames(typeof(ChaFileDefine.ClothesKind)).Length; i++)
				{
					if (___tglKind[i].isOn)
						copySlots.Add(i);
				}

				MaterialRouterController pluginCtrl = GetController(MakerAPI.GetCharacterControl());
				if (pluginCtrl != null)
					pluginCtrl.ClothingCopiedEvent(___ddCoordeType[1].value, ___ddCoordeType[0].value, copySlots);
			}
		}
	}
}
