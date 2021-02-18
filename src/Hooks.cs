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

		internal class Hooks
		{
			[HarmonyBefore(new string[] { "com.deathweasel.bepinex.materialeditor" })]
			[HarmonyPrefix, HarmonyPatch(typeof(ChaControl), "LoadCharaFbxDataAsync")]
			internal static void ChaControl_LoadCharaFbxDataAsync_Prefix(ChaControl __instance, ref Action<GameObject> actObj)
			{
				MaterialRouterController pluginCtrl = GetController(__instance);
				if (pluginCtrl == null) return;
				Action<GameObject> oldAct = actObj;
				actObj = delegate (GameObject o)
				{
					oldAct(o);
					if (o == null) return;
					DebugMsg(LogLevel.Warning, $"[ChaControl_LoadCharaFbxDataAsync_Prefix][ApplyBodyTrigger]");
					pluginCtrl.ApplyBodyTrigger();
				};
			}

			[HarmonyBefore(new string[] { "com.deathweasel.bepinex.materialeditor" })]
			[HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateType), typeof(ChaFileDefine.CoordinateType), typeof(bool))]
			internal static void ChaControl_ChangeCoordinateType_Prefix(ChaControl __instance)
			{
				MaterialRouterController pluginCtrl = GetController(__instance);
				if (pluginCtrl == null) return;
				DebugMsg(LogLevel.Warning, $"ChaControl_ChangeCoordinateType_Prefix [{pluginCtrl.CurrentCoordinateIndex}]");
				pluginCtrl.ApplyOutfitTrigger();
			}

			[HarmonyBefore(new string[] { "com.deathweasel.bepinex.materialeditor" })]
			[HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateTypeAndReload), new[] { typeof(bool) })]
			internal static void ChaControl_ChangeCoordinateTypeAndReload_Postfix(ChaControl __instance)
			{
				MaterialRouterController pluginCtrl = GetController(__instance);
				if (pluginCtrl == null) return;
				DebugMsg(LogLevel.Warning, $"ChaControl_ChangeCoordinateTypeAndReload_Postfix [{pluginCtrl.CurrentCoordinateIndex}]");
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
