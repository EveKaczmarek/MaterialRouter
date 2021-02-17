using System.Collections.Generic;
using System.Linq;
using System.IO;

using ParadoxNotion.Serialization;
using MessagePack;

using KKAPI.Chara;
using KKAPI.Maker;

namespace MaterialRouter
{
	public partial class Plugin
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
				BodyTrigger = data;
				Logger.LogMessage($"[ImportBodyTrigger] {data?.Count} rule(s) imported");
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

			internal void ResetBodyTrigger() => BodyTrigger = new List<RouteRule>();

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
				OutfitTriggers[CurrentCoordinateIndex] = data;
				Logger.LogMessage($"[ImportOutfitTrigger] {data?.Count} rule(s) imported");
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
				string ExportFilePath = Path.Combine(SavePath, SaveFile["body"]);
				string json = JSONSerializer.Serialize(data.GetType(), data, true);
				File.WriteAllText(ExportFilePath, json);
				Logger.LogMessage($"[ExportOutfitTrigger] {data?.Count} rule(s) exported to {ExportFilePath}");
			}

			internal void ResetOutfitTrigger() => OutfitTriggers[CurrentCoordinateIndex] = new List<RouteRule>();

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
			}

			internal void AccessoryTransferEvent(AccessoryTransferEventArgs ev)
			{
				int CoordinateIndex = CurrentCoordinateIndex;

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
			}
		}
	}
}
