using System.Collections.Generic;
using System.IO;

using ParadoxNotion.Serialization;

using KKAPI.Chara;

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
		}
	}
}
