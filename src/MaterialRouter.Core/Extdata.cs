using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using MessagePack;
using ParadoxNotion.Serialization;

using BepInEx.Logging;

#if KKS
using ExtensibleSaveFormat;
#endif

namespace MaterialRouter
{
	public partial class MaterialRouter
	{
		internal static string ExtDataKey = GUID;
		internal static int ExtDataVer = 2;

		internal static List<string> _objClothesNames = new List<string>() { "ct_clothesTop", "ct_clothesBot", "ct_bra", "ct_shorts", "ct_gloves", "ct_panst", "ct_socks", "ct_shoes_inner", "ct_shoes_outer" };
		internal static List<string> _objClothesPartsNames = new List<string>() { "ct_top_parts_A", "ct_top_parts_B", "ct_top_parts_C" };

		[Serializable]
		[MessagePackObject]
		public class RouteRule
		{
			[Key("ObjectType")]
			public ObjectType ObjectType { get; set; } = ObjectType.Unknown;
			[Key("Coordinate")]
			public int Coordinate { get; set; } = -1;
			[Key("GameObjectName")]
			public string GameObjectName { get; set; }
			[Key("RendererName")]
			public string RendererName { get; set; }
			[Key("Action")]
			public Action Action { get; set; }
			[Key("OldName")]
			public string OldName { get; set; }
			[Key("NewName")]
			public string NewName { get; set; }
		}

		[Serializable]
		[MessagePackObject]
		public class RouteRuleV1
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

		public enum ObjectType
		{
			Unknown,
			Clothing,
			Accessory,
			Hair,
			Character
		};

		internal static List<RouteRule> MigrationV1(List<RouteRuleV1> _source)
		{
			List<RouteRule> _result = new List<RouteRule>();
			if (_source?.Count == 0) return _result;

			foreach (RouteRuleV1 _oldRule in _source)
			{
				RouteRule _newRule = new RouteRule();
				_newRule.Action = _oldRule.Action;
				_newRule.OldName = _oldRule.OldName;
				_newRule.NewName = _oldRule.NewName;

				_newRule.RendererName = _oldRule.GameObjectPath.Substring(_oldRule.GameObjectPath.LastIndexOf("/") + 1);

				List<string> _chunks = SplitPath(_oldRule.GameObjectPath);

				if (_chunks.Any(x => x.StartsWith("ca_slot")))
				{
					_newRule.ObjectType = ObjectType.Accessory;
					_newRule.GameObjectName = _chunks.LastOrDefault(x => x.StartsWith("ca_slot"));
					_result.Add(_newRule);
					DebugMsg(LogLevel.Warning, "[MigrationV1]\n" + _oldRule.GameObjectPath + "\n" + JSONSerializer.Serialize(_newRule.GetType(), _newRule, true));
					continue;
				}

				if (_objClothesNames.Contains(_chunks[1]))
				{
					_newRule.ObjectType = ObjectType.Clothing;
					_newRule.GameObjectName = _chunks[1];
					//if (_objClothesPartsNames.Contains(_chunks[2]))
					if (!_chunks.FirstOrDefault(x => _objClothesPartsNames.Contains(x)).IsNullOrEmpty())
						_newRule.GameObjectName = _chunks.FirstOrDefault(x => _objClothesPartsNames.Contains(x));
					_result.Add(_newRule);
					DebugMsg(LogLevel.Warning, "[MigrationV1]\n" + _oldRule.GameObjectPath + "\n" + JSONSerializer.Serialize(_newRule.GetType(), _newRule, true));
					continue;
				}

				if (_chunks.Any(x => x.StartsWith("ct_hair")))
				{
					_newRule.ObjectType = ObjectType.Hair;
					_newRule.GameObjectName = _chunks.FirstOrDefault(x => x.StartsWith("ct_hair"));
					_result.Add(_newRule);
					DebugMsg(LogLevel.Warning, "[MigrationV1]\n" + _oldRule.GameObjectPath + "\n" + JSONSerializer.Serialize(_newRule.GetType(), _newRule, true));
					continue;
				}

				if (_chunks.Any(x => x.StartsWith("ct_head")))
				{
					_newRule.ObjectType = ObjectType.Character;
					_newRule.GameObjectName = "ct_head";
					_result.Add(_newRule);
					DebugMsg(LogLevel.Warning, "[MigrationV1]\n" + _oldRule.GameObjectPath + "\n" + JSONSerializer.Serialize(_newRule.GetType(), _newRule, true));
					continue;
				}

				if (_chunks.Any(x => x.StartsWith("cf_j_root")))
				{
					_newRule.ObjectType = ObjectType.Character;
					_newRule.GameObjectName = "cf_j_root";
					_result.Add(_newRule);
					DebugMsg(LogLevel.Warning, "[MigrationV1]\n" + _oldRule.GameObjectPath + "\n" + JSONSerializer.Serialize(_newRule.GetType(), _newRule, true));
					continue;
				}
			}

			return _result;
		}

		internal static List<string> SplitPath(string _source)
		{
			List<string> _result = new List<string>();
			_source = _source.Trim();
			if (_source.IsNullOrEmpty()) return _result;

			if (_source.Substring(_source.Length - 2, 1) != "/")
				_source = $"{_source}/";

			while (_source.IndexOf("/") > -1)
			{
				string _seg = _source.Substring(0, _source.IndexOf("/"));
				_result.Add(_seg);
				_source = _source.Remove(0, _seg.Length + 1);
			}

			return _result;
		}

		internal static ObjectType GetObjectType(GameObject _gameObject)
		{
			if (_gameObject == null)
				return ObjectType.Unknown;

			string _name = _gameObject?.name;

			if (_name == "ct_head")
				return ObjectType.Character;
			if (_name.StartsWith("ct_hair"))
				return ObjectType.Hair;
			if (_name.StartsWith("ca_slot"))
				return ObjectType.Accessory;
			if (_objClothesNames.Contains(_name) || _objClothesPartsNames.Contains(_name))
				return ObjectType.Clothing;

			return ObjectType.Unknown;
		}
#if KKS
		internal static void InitCardImport()
		{
			ExtendedSave.CardBeingImported += CardBeingImported;
		}

		internal static void CardBeingImported(Dictionary<string, PluginData> _importedExtData, Dictionary<int, int?> _coordinateMapping)
		{
			List<RouteRule> RouteRuleList = new List<RouteRule>();

			if (_importedExtData.TryGetValue(ExtDataKey, out PluginData _pluginData))
			{
				if (_pluginData.version == 1)
				{
					if (_pluginData.data.TryGetValue("BodyTrigger", out object _loadedBodyTrigger) && _loadedBodyTrigger != null)
					{
						List<RouteRuleV1> _tempBodyTrigger = MessagePackSerializer.Deserialize<List<RouteRuleV1>>((byte[]) _loadedBodyTrigger);
						if (_tempBodyTrigger?.Count > 0)
						{
							List<RouteRule> _tempRouteRuleList = MigrationV1(_tempBodyTrigger);
							RouteRuleList.AddRange(_tempRouteRuleList);
						}
					}
					if (_pluginData.data.TryGetValue("OutfitTriggers", out object _loadedOutfitTriggers) && _loadedOutfitTriggers != null)
					{
						Dictionary<int, List<RouteRuleV1>> _tempOutfitTriggers = MessagePackSerializer.Deserialize<Dictionary<int, List<RouteRuleV1>>>((byte[]) _loadedOutfitTriggers);
						foreach (KeyValuePair<int, List<RouteRuleV1>> _kvp in _tempOutfitTriggers)
						{
							if (_kvp.Value?.Count > 0)
							{
								List<RouteRule> _tempRouteRuleList = MigrationV1(_kvp.Value);
								_tempRouteRuleList.ForEach(x => x.Coordinate = _kvp.Key);
								RouteRuleList.AddRange(_tempRouteRuleList);
							}
						}
					}
				}
				else
				{
					if (_pluginData.data.TryGetValue("RouteRuleList", out object _loadedRouteRuleList) && _loadedRouteRuleList != null)
					{
						List<RouteRule> _tempRouteRuleList = MessagePackSerializer.Deserialize<List<RouteRule>>((byte[]) _loadedRouteRuleList);
						if (_tempRouteRuleList?.Count > 0)
							RouteRuleList.AddRange(_tempRouteRuleList);
					}
				}

				_importedExtData.Remove(ExtDataKey);

				if (RouteRuleList?.Count > 0)
				{
					for (int i = 0; i < RouteRuleList.Count; i++)
					{
						int _coordinateIndex = RouteRuleList[i].Coordinate;
						if (_coordinateIndex < 0) continue;
						RouteRuleList[i].Coordinate = (int) _coordinateMapping[_coordinateIndex];
					}

					PluginData _pluginDataNew = new PluginData() { version = ExtDataVer };
					_pluginDataNew.data.Add("RouteRuleList", MessagePackSerializer.Serialize(RouteRuleList));
					_importedExtData[ExtDataKey] = _pluginDataNew;
				}
			}
		}
#endif
	}
}
