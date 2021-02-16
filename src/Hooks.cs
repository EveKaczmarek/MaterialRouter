using System;

using UnityEngine;
using HarmonyLib;

using BepInEx.Logging;
	
using KKAPI.Chara;

namespace MaterialRouter
{
	public partial class Plugin
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

			[HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateType), typeof(ChaFileDefine.CoordinateType), typeof(bool))]
			internal static void ChaControl_ChangeCoordinateType_Prefix(ChaControl __instance)
			{
				MaterialRouterController pluginCtrl = GetController(__instance);
				if (pluginCtrl == null) return;
				DebugMsg(LogLevel.Warning, $"ChaControl_ChangeCoordinateType_Prefix [{pluginCtrl.CurrentCoordinateIndex}]");
				pluginCtrl.ApplyOutfitTrigger();
			}

			[HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateTypeAndReload), new[] { typeof(bool) })]
			internal static void ChaControl_ChangeCoordinateTypeAndReload_Postfix(ChaControl __instance)
			{
				MaterialRouterController pluginCtrl = GetController(__instance);
				if (pluginCtrl == null) return;
				DebugMsg(LogLevel.Warning, $"ChaControl_ChangeCoordinateTypeAndReload_Postfix [{pluginCtrl.CurrentCoordinateIndex}]");
				pluginCtrl.ApplyOutfitTrigger();
			}

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
	}
}
