using System;
using System.Collections.Generic;

using ChaCustom;
using UnityEngine;

using BepInEx.Logging;
using HarmonyLib;

using KKAPI.Chara;

namespace MaterialRouter
{
	public partial class MaterialRouter
	{
		internal static class Hooks
		{
			/*
			[HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.LoadCharaFbxDataAsync))]
			internal static void ChaControl_LoadCharaFbxDataAsync_Prefix(ChaControl __instance, ref Action<GameObject> actObj)
			{
				Action<GameObject> oldAct = actObj;
				actObj = delegate (GameObject _gameObject)
				{
					oldAct(_gameObject);
					if (_gameObject == null) return;

					MaterialRouterController _pluginCtrl = GetController(__instance);
					if (_pluginCtrl == null) return;

					_pluginCtrl.ApplyGameObjectRules(_gameObject);
				};
			}
			*/
			internal static void MaterialEditorCharaController_OnReload_Prefix(CharaCustomFunctionController __instance)
			{
				ChaControl _chaCtrl = __instance.ChaControl;
				MaterialRouterController _pluginCtrl = GetController(_chaCtrl);
				if (_pluginCtrl == null) return;
				DebugMsg(LogLevel.Info, $"MaterialEditorCharaController_OnReload_Prefix [{_pluginCtrl._currentCoordinateIndex}]");
				_pluginCtrl.OnReload_Prefix();
			}

			internal static void MaterialEditorCharaController_OnCoordinateBeingLoaded_Prefix(CharaCustomFunctionController __instance, ChaFileCoordinate __0)
			{
				ChaControl _chaCtrl = __instance.ChaControl;
				MaterialRouterController _pluginCtrl = GetController(_chaCtrl);
				if (_pluginCtrl == null) return;
				DebugMsg(LogLevel.Info, $"MaterialEditorCharaController_OnCoordinateBeingLoaded_Prefix [{_pluginCtrl._currentCoordinateIndex}]");
				_pluginCtrl.OnCoordinateBeingLoaded_Prefix(__0);
			}
		}

		internal static class HooksMaker
		{
			[HarmonyPrefix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCoordinateTypeAndReload), new[] { typeof(bool) })]
			internal static void ChaControl_ChangeCoordinateTypeAndReload_Prefix()
			{
				if (_charaConfigWindow == null) return;

				_charaConfigWindow._curGameObject = null;
				_charaConfigWindow.enabled = false;
			}

			[HarmonyPostfix, HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeAccessory), typeof(int), typeof(int), typeof(int), typeof(string), typeof(bool))]
			internal static void ChaControl_ChangeAccessory_Postfix()
			{
				//InitCurrentSlot();
				CustomBase.Instance.StartCoroutine(InitCurrentSlotCoroutine());
			}

			[HarmonyPostfix, HarmonyPatch(typeof(CvsClothesCopy), nameof(CvsClothesCopy.CopyClothes))]
			internal static void CvsClothesCopy_CopyClothes_Postfix(CvsClothesCopy __instance)
			{
				ChaControl _chaCtrl = CustomBase.Instance.chaCtrl;
				MaterialRouterController _pluginCtrl = GetController(_chaCtrl);
				if (_pluginCtrl == null) return;

				List<int> _copiedSlotIndexes = new List<int>();
				for (int i = 0; i < Enum.GetNames(typeof(ChaFileDefine.ClothesKind)).Length; i++)
				{
					if (__instance.tglKind[i].isOn)
						_copiedSlotIndexes.Add(i);
				}

				_pluginCtrl.ClothingCopiedEvent(__instance.ddCoordeType[1].value, __instance.ddCoordeType[0].value, _copiedSlotIndexes);
				if (__instance.ddCoordeType[0].value == _chaCtrl.fileStatus.coordinateType)
					_chaCtrl.ChangeCoordinateTypeAndReload(false);
			}
		}
	}
}
