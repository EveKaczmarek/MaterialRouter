using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using MessagePack;

using BepInEx.Logging;
using ExtensibleSaveFormat;

using KKAPI;
using KKAPI.Chara;

namespace MaterialRouter
{
	public partial class MaterialRouter
	{
		public partial class MaterialRouterController : CharaCustomFunctionController
		{
			internal List<RouteRule> BodyTrigger = new List<RouteRule>();
			internal List<RouteRule> CurOutfitTrigger = new List<RouteRule>();
			internal Dictionary<int, List<RouteRule>> OutfitTriggers = new Dictionary<int, List<RouteRule>>();

			internal int CurrentCoordinateIndex => ChaControl.fileStatus.coordinateType;

			protected override void OnCardBeingSaved(GameMode currentGameMode)
			{
				BodyTrigger = SortRouteRules(BodyTrigger);
				for (int i = 0; i < 7; i++)
					OutfitTriggers[i] = SortRouteRules(OutfitTriggers[i]);
				PluginData ExtendedData = new PluginData();
				ExtendedData.data.Add("BodyTrigger", MessagePackSerializer.Serialize(BodyTrigger));
				ExtendedData.data.Add("OutfitTriggers", MessagePackSerializer.Serialize(OutfitTriggers));
				ExtendedData.version = ExtDataVer;
				SetExtendedData(ExtendedData);
			}

			protected override void OnCoordinateBeingSaved(ChaFileCoordinate coordinate)
			{
				OutfitTriggers[CurrentCoordinateIndex] = SortRouteRules(OutfitTriggers[CurrentCoordinateIndex]);
				PluginData ExtendedData = new PluginData();
				ExtendedData.data.Add("OutfitTrigger", MessagePackSerializer.Serialize(OutfitTriggers[CurrentCoordinateIndex]));
				ExtendedData.version = ExtDataVer;
				SetCoordinateExtendedData(coordinate, ExtendedData);
			}

			internal void OnReload_Prefix()
			{
				BodyTrigger = new List<RouteRule>();
				OutfitTriggers = new Dictionary<int, List<RouteRule>>();
				PluginData ExtendedData = GetExtendedData();

				if (ExtendedData != null && ExtendedData.data.TryGetValue("BodyTrigger", out object loadedBodyTrigger) && loadedBodyTrigger != null)
					BodyTrigger = MessagePackSerializer.Deserialize<List<RouteRule>>((byte[]) loadedBodyTrigger);

				if (ExtendedData != null && ExtendedData.data.TryGetValue("OutfitTriggers", out object loadedOutfitTriggers) && loadedOutfitTriggers != null)
					OutfitTriggers = MessagePackSerializer.Deserialize<Dictionary<int, List<RouteRule>>>((byte[]) loadedOutfitTriggers);

				BodyTrigger = SortRouteRules(BodyTrigger);
				ApplyRules(BodyTrigger);

				if (OutfitTriggers?.Count == 0)
					OutfitTriggers = new Dictionary<int, List<RouteRule>>();
				for (int i = 0; i < 7; i++)
				{
					if (!OutfitTriggers.ContainsKey(i))
						OutfitTriggers[i] = new List<RouteRule>();
					OutfitTriggers[i] = SortRouteRules(OutfitTriggers?[i]);
				}

				BuildCheckList();
				ApplyRules(OutfitTriggers[CurrentCoordinateIndex]);
			}

			internal void OnCoordinateBeingLoaded_Prefix(ChaFileCoordinate coordinate)
			{
				OutfitTriggers[CurrentCoordinateIndex] = new List<RouteRule>();
				PluginData ExtendedData = GetCoordinateExtendedData(coordinate);

				if (ExtendedData != null && ExtendedData.data.TryGetValue("OutfitTrigger", out object loadedOutfitTrigger) && loadedOutfitTrigger != null)
					OutfitTriggers[CurrentCoordinateIndex] = MessagePackSerializer.Deserialize<List<RouteRule>>((byte[]) loadedOutfitTrigger);

				if (OutfitTriggers[CurrentCoordinateIndex]?.Count == 0)
					OutfitTriggers[CurrentCoordinateIndex] = new List<RouteRule>();
				OutfitTriggers[CurrentCoordinateIndex] = SortRouteRules(OutfitTriggers[CurrentCoordinateIndex]);
				BuildCheckList();
				ApplyRules(OutfitTriggers[CurrentCoordinateIndex]);
			}

			internal void BuildCheckList()
			{
				NewNameList[ChaControl] = new List<string>();
				if (!OutfitTriggers.ContainsKey(CurrentCoordinateIndex) || OutfitTriggers[CurrentCoordinateIndex]?.Count == 0)
					return;
				NewNameList[ChaControl].AddRange(OutfitTriggers[CurrentCoordinateIndex].Select(x => x.NewName).ToList());
				DebugMsg(LogLevel.Info, $"[BuildCheckList] rule count {NewNameList[ChaControl].Count}");
			}

			internal void CorrectTongue_Prefix()
			{
				ApplyBodyTrigger();
				ApplyOutfitTrigger();
			}

			internal void ApplyBodyTrigger()
			{
				if (BodyTrigger?.Count == 0)
					BodyTrigger = new List<RouteRule>();
				ApplyRules(BodyTrigger);
			}

			internal void ApplyOutfitTrigger() => ApplyOutfitTrigger(CurrentCoordinateIndex);
			internal void ApplyOutfitTrigger(int CoordinateIndex)
			{
				if (!OutfitTriggers.ContainsKey(CoordinateIndex) || OutfitTriggers[CoordinateIndex]?.Count == 0)
					OutfitTriggers[CoordinateIndex] = new List<RouteRule>();
				CurOutfitTrigger = OutfitTriggers[CoordinateIndex];
				ApplyRules(CurOutfitTrigger);
			}

			internal void ApplyRules(List<RouteRule> rules)
			{
				DebugMsg(LogLevel.Info, $"[ApplyRules] rule count {rules?.Count}");

				if (rules?.Count == 0)
					return;

				foreach (RouteRule rule in rules)
				{
					Transform target = ChaControl.transform.Find(rule.GameObjectPath);
					if (target == null)
					{
						DebugMsg(LogLevel.Error, $"[ApplyRules] {rule.GameObjectPath} not found");
						continue;
					}

					Renderer rend = target.GetComponent<Renderer>();
					if (rend == null)
					{
						DebugMsg(LogLevel.Error, $"[ApplyRules] Renderer not found");
						continue;
					}

					Material mat = rend.material;
					if (mat == null)
					{
						DebugMsg(LogLevel.Error, $"[ApplyRules] Material not found");
						continue;
					}

					if (rule.Action == Action.Rename)
					{
						if (rend.material.NameFormatted() == rule.NewName)
						{
							DebugMsg(LogLevel.Error, $"[ApplyRules] Material {rule.OldName} already renamed");
							continue;
						}
						mat.name = rule.NewName;
						DebugMsg(LogLevel.Info, $"[ApplyRules][Rename][{rule.GameObjectPath}][{rule.OldName}][{rule.NewName}][{rend.materials.Length}]");
					}
					else if (rule.Action == Action.Clone)
					{
						Material copy = new Material(mat);
						if (copy.NameFormatted() != rule.OldName)
						{
							DebugMsg(LogLevel.Error, $"[ApplyRules] Material name mismatch");
							continue;
						}
						bool skip = false;
						foreach (Material x in rend.materials)
						{
							if (x.NameFormatted() == rule.NewName)
							{
								skip = true;
								DebugMsg(LogLevel.Error, $"[ApplyRules] Material {rule.OldName} already cloned");
							}
						}
						if (skip)
							continue;
						copy.CopyPropertiesFromMaterial(rend.material);
						copy.name = rule.NewName;
						rend.materials = rend.materials.AddToArray(copy);
						DebugMsg(LogLevel.Info, $"[ApplyRules][Clone][{rule.GameObjectPath}][{rule.OldName}][{rule.NewName}][{rend.materials.Length}]");
					}
				}
			}
		}

		internal static MaterialRouterController GetController(ChaControl chaCtrl) => chaCtrl?.gameObject.GetComponent<MaterialRouterController>();

		[Serializable]
		[MessagePackObject]
		public class RouteRule
		{
			[Key("GameObjectPath")]
			public string GameObjectPath { get; set; }
			[Key("Action")]
			public Action Action { get; set; }
			[Key("OldName")]
			public string OldName { get; set; }
			[Key("NewName")]
			public string NewName { get; set; }
		}

		public enum Action
		{
			Dummy,
			Rename,
			Clone,
		}
	}
}
