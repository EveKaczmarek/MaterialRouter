using System.Collections.Generic;
using System.Linq;
using System.IO;

using ParadoxNotion.Serialization;
using MessagePack;

using KKAPI.Chara;
using KKAPI.Maker;

namespace MaterialRouter
{
	public partial class MaterialRouter
	{
		public partial class MaterialRouterController : CharaCustomFunctionController
		{
			internal void ImportBodyTrigger()
			{
				string ExportFilePath = Path.Combine(SavePath, SaveFile["Body"]);
				if (!File.Exists(ExportFilePath))
				{
					Logger.LogMessage($"[ImportBodyTrigger] {ExportFilePath} file doesn't exist");
					return;
				}
				List<RouteRule> data = JSONSerializer.Deserialize<List<RouteRule>>(File.ReadAllText(ExportFilePath));
				if (data?.Count == 0)
				{
					Logger.LogMessage($"[ImportBodyTrigger] no rule to import");
					return;
				}

				int skipped = 0;
				foreach (RouteRule rule in data)
				{
					if (BodyTrigger.Any(x => x.GameObjectPath == rule.GameObjectPath && x.OldName == rule.OldName && x.NewName == rule.NewName))
					{
						skipped++;
						continue;
					}
					BodyTrigger.Add(rule);
				}
				BodyTrigger = SortRouteRules(BodyTrigger);

				Logger.LogMessage($"[ImportBodyTrigger] {data?.Count - skipped} rule(s) imported, {skipped} rule(s) skipped");
			}

			internal void ExportBodyTrigger()
			{
				List<RouteRule> data = BodyTrigger;
				if (data?.Count == 0)
				{
					Logger.LogMessage($"[ExportBodyTrigger] no rule to export");
					return;
				}
				if (!Directory.Exists(SavePath))
					Directory.CreateDirectory(SavePath);
				string ExportFilePath = Path.Combine(SavePath, SaveFile["Body"]);
				string json = JSONSerializer.Serialize(data.GetType(), data, true);
				File.WriteAllText(ExportFilePath, json);
				Logger.LogMessage($"[ExportBodyTrigger] {data?.Count} rule(s) exported to {ExportFilePath}");
			}

			internal void ResetBodyTrigger()
			{
				BodyTrigger = new List<RouteRule>();
				Logger.LogMessage($"[ResetBodyTrigger] done");
			}

			internal void ImportOutfitTrigger()
			{
				string ExportFilePath = Path.Combine(SavePath, SaveFile["Outfit"]);
				if (!File.Exists(ExportFilePath))
				{
					Logger.LogMessage($"[ImportOutfitTrigger] {ExportFilePath} file doesn't exist");
					return;
				}
				List<RouteRule> data = JSONSerializer.Deserialize<List<RouteRule>>(File.ReadAllText(ExportFilePath));
				if (data?.Count == 0)
				{
					Logger.LogMessage($"[ImportOutfitTrigger] no rule to import");
					return;
				}

				int skipped = 0;
				foreach (RouteRule rule in data)
				{
					if (OutfitTriggers[CurrentCoordinateIndex].Any(x => x.GameObjectPath == rule.GameObjectPath && x.OldName == rule.OldName && x.NewName == rule.NewName))
					{
						skipped++;
						continue;
					}
					OutfitTriggers[CurrentCoordinateIndex].Add(rule);
				}
				OutfitTriggers[CurrentCoordinateIndex] = SortRouteRules(OutfitTriggers[CurrentCoordinateIndex]);

				Logger.LogMessage($"[ImportOutfitTrigger] {data?.Count - skipped} rule(s) imported, {skipped} rule(s) skipped");
			}

			internal void ExportOutfitTrigger()
			{
				List<RouteRule> data = OutfitTriggers[CurrentCoordinateIndex];
				if (data?.Count == 0)
				{
					Logger.LogMessage($"[ExportOutfitTrigger] no rule to export");
					return;
				}
				if (!Directory.Exists(SavePath))
					Directory.CreateDirectory(SavePath);
				string ExportFilePath = Path.Combine(SavePath, SaveFile["Outfit"]);
				string json = JSONSerializer.Serialize(data.GetType(), data, true);
				File.WriteAllText(ExportFilePath, json);
				Logger.LogMessage($"[ExportOutfitTrigger] {data?.Count} rule(s) exported to {ExportFilePath}");
			}

			internal void ResetOutfitTrigger()
			{
				OutfitTriggers[CurrentCoordinateIndex] = new List<RouteRule>();
				Logger.LogMessage($"[ResetOutfitTrigger] done");
			}

			internal List<RouteRule> SortRouteRules(List<RouteRule> rules)
			{
				if (rules?.Count == 0)
					return new List<RouteRule>();
				return rules.OrderBy(x => x.GameObjectPath).ThenBy(x => x.Action).ToList();
			}

			internal void ClothingCopiedEvent(int srcIdx, int dstIdx, List<int> copySlots)
			{
				if (!OutfitTriggers.ContainsKey(srcIdx))
					OutfitTriggers[srcIdx] = new List<RouteRule>();
				if (!OutfitTriggers.ContainsKey(dstIdx))
					OutfitTriggers[dstIdx] = new List<RouteRule>();

				foreach (int slot in copySlots)
				{
					string name = "/" + ChaControl.objClothes[slot].name + "/";
					OutfitTriggers[dstIdx].RemoveAll(x => x.GameObjectPath.Contains(name));
					List<RouteRule> rules = OutfitTriggers[srcIdx].Where(x => x.GameObjectPath.Contains(name)).ToList();
					if (rules?.Count > 0)
						OutfitTriggers[dstIdx].AddRange(rules);
				}
				BuildCheckList();
			}

			internal void AccessoryTransferEvent(AccessoryTransferEventArgs ev)
			{
				TransferAccSlotInfo(CurrentCoordinateIndex, ev);
				BuildCheckList();
			}

			internal void AccessoryCopyEvent(AccessoryCopyEventArgs ev)
			{
				int srcIdx = (int) ev.CopySource;
				int dstIdx = (int) ev.CopyDestination;
				List<int> copySlots = ev.CopiedSlotIndexes.ToList();

				if (!OutfitTriggers.ContainsKey(srcIdx))
					OutfitTriggers[srcIdx] = new List<RouteRule>();
				if (!OutfitTriggers.ContainsKey(dstIdx))
					OutfitTriggers[dstIdx] = new List<RouteRule>();

				foreach (int slot in copySlots)
				{
					string name = $"/ca_slot{slot:00}/";
					OutfitTriggers[dstIdx].RemoveAll(x => x.GameObjectPath.Contains(name));
					List<RouteRule> rules = OutfitTriggers[srcIdx].Where(x => x.GameObjectPath.Contains(name)).ToList();
					if (rules?.Count > 0)
						OutfitTriggers[dstIdx].AddRange(rules);
				}
				BuildCheckList();
			}

			internal void TransferAccSlotInfo(int CoordinateIndex, AccessoryTransferEventArgs ev)
			{
				if (!OutfitTriggers.ContainsKey(CoordinateIndex))
					OutfitTriggers[CoordinateIndex] = new List<RouteRule>();
				int srcIdx = ev.SourceSlotIndex;
				int dstIdx = ev.DestinationSlotIndex;
				string srcName = $"/ca_slot{srcIdx:00}/";
				string dstName = $"/ca_slot{dstIdx:00}/";

				OutfitTriggers[CoordinateIndex].RemoveAll(x => x.GameObjectPath.Contains(dstName));
				var rules = OutfitTriggers[CoordinateIndex].Where(x => x.GameObjectPath.Contains(srcName));
				if (rules?.Count() > 0)
				{
					byte[] data = MessagePackSerializer.Serialize(rules.ToList());
					OutfitTriggers[CoordinateIndex].ForEach(x => x.GameObjectPath = x.GameObjectPath.Replace(srcName, dstName));
					OutfitTriggers[CoordinateIndex].AddRange(MessagePackSerializer.Deserialize<List<RouteRule>>(data));
				}
			}

			internal void RemoveAccSlotInfo(int CoordinateIndex, int SlotIndex)
			{
				if (!OutfitTriggers.ContainsKey(CoordinateIndex) || OutfitTriggers[CoordinateIndex]?.Count == 0)
					return;
				string slotName = $"/ca_slot{SlotIndex:00}/";
				OutfitTriggers[CoordinateIndex].RemoveAll(x => x.GameObjectPath.Contains(slotName));
			}
		}
	}
}
