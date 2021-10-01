using System.Collections.Generic;
using System.Linq;
using System.IO;

using UnityEngine;
using ParadoxNotion.Serialization;

using KKAPI.Chara;
using KKAPI.Maker;
using JetPack;

namespace MaterialRouter
{
	public partial class MaterialRouter
	{
		internal static string _exportSavePath = "";
		internal static Dictionary<string, string> _exportSaveFile = new Dictionary<string, string>() { ["Body"] = "MaterialRouterBody.json", ["Outfit"] = "MaterialRouterOutfit.json", ["Outfits"] = "MaterialRouterOutfits.json" };

		public partial class MaterialRouterController : CharaCustomFunctionController
		{
			internal void ClothingCopiedEvent(int _srcCoordinateIndex, int _dstCoordinateIndex, List<int> _copiedSlotIndexes)
			{
				foreach (int _slotIndex in _copiedSlotIndexes)
				{
					RouteRuleList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.Coordinate == _dstCoordinateIndex && x.GameObjectName == _objClothesNames[_slotIndex]);
					if (_slotIndex == 0)
						RouteRuleList.RemoveAll(x => x.ObjectType == ObjectType.Clothing && x.Coordinate == _dstCoordinateIndex && _objClothesPartsNames.Contains(x.GameObjectName));

					List<RouteRule> _copy = RouteRuleList.Where(x => x.ObjectType == ObjectType.Clothing && x.Coordinate == _srcCoordinateIndex && x.GameObjectName == _objClothesNames[_slotIndex]).ToList().JsonClone<List<RouteRule>>();
					if (_slotIndex == 0)
						_copy.AddRange(RouteRuleList.Where(x => x.ObjectType == ObjectType.Clothing && x.Coordinate == _srcCoordinateIndex && _objClothesPartsNames.Contains(x.GameObjectName)).ToList().JsonClone<List<RouteRule>>());

					_copy.ForEach(x => x.Coordinate = _dstCoordinateIndex);
					RouteRuleList.AddRange(_copy);
				}
			}

			internal void AccessoryTransferEvent(AccessoryTransferEventArgs _ev)
			{
				TransferAccSlotInfo(_currentCoordinateIndex, _ev);
				ChaControl.ChangeCoordinateTypeAndReload(false);
			}

			internal void TransferAccSlotInfo(int _coordinateIndex, AccessoryTransferEventArgs _ev)
			{
				string _srcName = $"ca_slot{_ev.SourceSlotIndex:00}";
				string _dstName = $"ca_slot{_ev.DestinationSlotIndex:00}";

				RouteRuleList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.Coordinate == _coordinateIndex && x.GameObjectName == _dstName);
				List<RouteRule> _copy = RouteRuleList.Where(x => x.ObjectType == ObjectType.Accessory && x.Coordinate == _coordinateIndex && x.GameObjectName == _srcName).ToList().JsonClone<List<RouteRule>>();
				if (_copy?.Count > 0)
				{
					_copy.ForEach(x => x.GameObjectName = _dstName);
					RouteRuleList.AddRange(_copy);
				}
			}

			internal void AccessoryCopyEvent(AccessoryCopyEventArgs _ev)
			{
				int _srcCoordinateIndex = (int) _ev.CopySource;
				int _dstCoordinateIndex = (int) _ev.CopyDestination;
				List<int> _copiedSlotIndexes = _ev.CopiedSlotIndexes.ToList();

				foreach (int _slotIndex in _copiedSlotIndexes)
				{
					string _gameObjectName = $"ca_slot{_slotIndex:00}";
					RouteRuleList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.Coordinate == _dstCoordinateIndex && x.GameObjectName == _gameObjectName);
					List<RouteRule> _copy = RouteRuleList.Where(x => x.ObjectType == ObjectType.Accessory && x.Coordinate == _srcCoordinateIndex && x.GameObjectName == _gameObjectName).ToList().JsonClone<List<RouteRule>>();
					if (_copy?.Count > 0)
					{
						_copy.ForEach(x => x.Coordinate = _dstCoordinateIndex);
						RouteRuleList.AddRange(_copy);
					}
				}

				if (_dstCoordinateIndex == _currentCoordinateIndex)
					ChaControl.ChangeCoordinateTypeAndReload(false);
			}

			internal void RemoveAccSlotInfo(int _slotIndex) => RemoveAccSlotInfo(_currentCoordinateIndex, _slotIndex);
			internal void RemoveAccSlotInfo(int _coordinateIndex, int _slotIndex)
			{
				string _gameObjectName = $"ca_slot{_slotIndex:00}";
				RouteRuleList.RemoveAll(x => x.ObjectType == ObjectType.Accessory && x.Coordinate == _coordinateIndex && x.GameObjectName == _gameObjectName);
			}

			internal void RemoveSlotInfo(GameObject _gameObject)
			{
				ObjectType _objectType = GetObjectType(_gameObject);
				int _coordinateIndex = (_objectType == ObjectType.Character || _objectType == ObjectType.Hair) ? -1 : _currentCoordinateIndex;
				RouteRuleList.RemoveAll(x => x.ObjectType == _objectType && x.Coordinate == _coordinateIndex && x.GameObjectName == _gameObject.name);
			}

			internal void ImportFromRendererInfo(int _slotIndex)
			{
				GameObject _gameObject = JetPack.Accessory.GetObjAccessory(ChaControl, _slotIndex);
				List<RouteRule> _rules = GenerateRules(_gameObject);
				RouteRuleList.AddRange(_rules);
			}

			internal List<RouteRule> GenerateRules(GameObject _gameObject, bool _skipExist = true)
			{
				List<RouteRule> _rules = new List<RouteRule>();
				if (_gameObject == null) return _rules;

				ObjectType _objectType = GetObjectType(_gameObject);
				Renderer[] _renderers = _gameObject.GetComponentsInChildren<Renderer>(true);
				int _skipped = 0;
				int _coordinateIndex = (_objectType == ObjectType.Character || _objectType == ObjectType.Hair) ? -1 : _makerPluginCtrl._currentCoordinateIndex;
				foreach (Renderer _renderer in _renderers)
				{
					foreach (Material _material in _renderer.materials)
					{
						string _materialName = _material.NameFormatted();
						RouteRule _rule = new RouteRule
						{
							ObjectType = _objectType,
							Coordinate = _coordinateIndex,
							GameObjectName = _gameObject.name,
							RendererName = _renderer.name,
							Action = Action.Clone,
							OldName = _materialName,
							NewName = _materialName + "_cloned"
						};

						RouteRule _exist = null;
						if (_coordinateIndex == -1)
							_exist = _makerPluginCtrl.RouteRuleList.FirstOrDefault(x => x.ObjectType == ObjectType.Character && x.GameObjectName == _gameObject.name && x.RendererName == _renderer.name && x.NewName == _materialName);
						else
							_exist = _makerPluginCtrl.RouteRuleList.FirstOrDefault(x => (x.ObjectType == ObjectType.Accessory || x.ObjectType == ObjectType.Clothing) && x.Coordinate == _makerPluginCtrl._currentCoordinateIndex && x.GameObjectName == _gameObject.name && x.RendererName == _renderer.name && x.NewName == _materialName);

						if (_exist != null)
						{
							if (_skipExist)
							{
								_skipped++;
								continue;
							}
							else
								_rule.Action = _exist.Action;
						}
						_rules.Add(_rule);
					}
				}

				return _rules;
			}

			internal void ImportBodyTrigger()
			{
				string _exportFilePath = Path.Combine(_exportSavePath, _exportSaveFile["Body"]);
				if (!File.Exists(_exportFilePath))
				{
					_logger.LogMessage($"[ImportBodyTrigger] {_exportFilePath} file doesn't exist");
					return;
				}
				List<RouteRule> _rules = JSONSerializer.Deserialize<List<RouteRule>>(File.ReadAllText(_exportFilePath));
				if (_rules?.Count == 0)
				{
					_logger.LogMessage($"[ImportBodyTrigger] no rule to import");
					return;
				}

				int _skipped = 0;
				foreach (RouteRule _rule in _rules)
				{
					if (RouteRuleList.Any(x => x.ObjectType == ObjectType.Character && x.GameObjectName == _rule.GameObjectName && x.RendererName == _rule.RendererName && x.OldName == _rule.OldName && x.NewName == _rule.NewName))
					{
						_skipped++;
						continue;
					}
					RouteRuleList.Add(_rule);
				}
				_logger.LogMessage($"[ImportBodyTrigger] {_rules?.Count - _skipped} rule(s) imported, {_skipped} rule(s) skipped");
			}

			internal void ExportBodyTrigger()
			{
				List<RouteRule> _rules = RouteRuleList.Where(x => x.ObjectType == ObjectType.Character).ToList();
				if (_rules?.Count == 0)
				{
					_logger.LogMessage($"[ExportBodyTrigger] no rule to export");
					return;
				}
				_rules = SortRouteRules(_rules);
				if (!Directory.Exists(_exportSavePath))
					Directory.CreateDirectory(_exportSavePath);
				string _exportFilePath = Path.Combine(_exportSavePath, _exportSaveFile["Body"]);
				string _json = JSONSerializer.Serialize(_rules.GetType(), _rules, true);
				File.WriteAllText(_exportFilePath, _json);
				_logger.LogMessage($"[ExportBodyTrigger] {_rules?.Count} rule(s) exported to {_exportFilePath}");
			}

			internal void ResetBodyTrigger()
			{
				RouteRuleList.RemoveAll(x => x.ObjectType == ObjectType.Character);
				_logger.LogMessage($"[ResetBodyTrigger] done");
			}

			internal void ImportOutfitTrigger()
			{
				string _exportFilePath = Path.Combine(_exportSavePath, _exportSaveFile["Outfit"]);
				if (!File.Exists(_exportFilePath))
				{
					_logger.LogMessage($"[ImportOutfitTrigger] {_exportFilePath} file doesn't exist");
					return;
				}
				List<RouteRule> _rules = JSONSerializer.Deserialize<List<RouteRule>>(File.ReadAllText(_exportFilePath));
				if (_rules?.Count == 0)
				{
					_logger.LogMessage($"[ImportOutfitTrigger] no rule to import");
					return;
				}

				int _skipped = 0;
				foreach (RouteRule _rule in _rules)
				{
					if (RouteRuleList.Any(x => (x.ObjectType == ObjectType.Accessory || x.ObjectType == ObjectType.Clothing) && x.Coordinate == _currentCoordinateIndex && x.GameObjectName == _rule.GameObjectName && x.RendererName == _rule.RendererName && x.OldName == _rule.OldName && x.NewName == _rule.NewName))
					{
						_skipped++;
						continue;
					}
					RouteRuleList.Add(_rule);
				}
				_logger.LogMessage($"[ImportOutfitTrigger] {_rules?.Count - _skipped} rule(s) imported, {_skipped} rule(s) skipped");
			}

			internal void ExportOutfitTrigger()
			{
				List<RouteRule> _rules = RouteRuleList.Where(x => (x.ObjectType == ObjectType.Accessory || x.ObjectType == ObjectType.Clothing) && x.Coordinate == _currentCoordinateIndex).ToList();
				if (_rules?.Count == 0)
				{
					_logger.LogMessage($"[ExportOutfitTrigger] no rule to export");
					return;
				}
				_rules = SortRouteRules(_rules);
				if (!Directory.Exists(_exportSavePath))
					Directory.CreateDirectory(_exportSavePath);
				string _exportFilePath = Path.Combine(_exportSavePath, _exportSaveFile["Outfit"]);
				string _json = JSONSerializer.Serialize(_rules.GetType(), _rules, true);
				File.WriteAllText(_exportFilePath, _json);
				_logger.LogMessage($"[ExportOutfitTrigger] {_rules?.Count} rule(s) exported to {_exportFilePath}");
			}

			internal void ResetOutfitTrigger()
			{
				RouteRuleList.RemoveAll(x => (x.ObjectType == ObjectType.Accessory || x.ObjectType == ObjectType.Clothing) && x.Coordinate == _currentCoordinateIndex);
				_logger.LogMessage($"[ResetOutfitTrigger] done");
			}

			internal List<RouteRule> SortRouteRules(List<RouteRule> _rules)
			{
				if (_rules?.Count == 0)
					return new List<RouteRule>();
				return _rules.OrderBy(x => x.ObjectType).ThenBy(x => x.Coordinate).ThenBy(x => x.GameObjectName).ThenBy(x => x.RendererName).ThenBy(x => x.Action).ThenBy(x => x.OldName).ThenBy(x => x.NewName).ToList();
			}
		}
	}
}
