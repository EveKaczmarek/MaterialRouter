using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using MessagePack;

using BepInEx.Logging;

using ExtensibleSaveFormat;

using KKAPI;
using KKAPI.Chara;
using JetPack;

namespace MaterialRouter
{
	public partial class MaterialRouter
	{
		public partial class MaterialRouterController : CharaCustomFunctionController
		{
			internal List<RouteRule> RouteRuleList = new List<RouteRule>();

			internal int _currentCoordinateIndex => ChaControl.fileStatus.coordinateType;

			protected override void OnCardBeingSaved(GameMode currentGameMode)
			{
				if (RouteRuleList?.Count == 0)
				{
					SetExtendedData(null);
					return;
				}

				PluginData _pluginData = new PluginData();
				_pluginData.data.Add("RouteRuleList", MessagePackSerializer.Serialize(RouteRuleList));
				_pluginData.version = ExtDataVer;
				SetExtendedData(_pluginData);
			}

			protected override void OnCoordinateBeingSaved(ChaFileCoordinate coordinate)
			{
				List<RouteRule> _tempRouteRuleList = RouteRuleList.Where(x => x.Coordinate == _currentCoordinateIndex).ToList();
				if (_tempRouteRuleList?.Count == 0)
				{
					SetCoordinateExtendedData(coordinate, null);
					return;
				}

				PluginData _pluginData = new PluginData();
				_pluginData.version = ExtDataVer;
				_tempRouteRuleList.ForEach(x => x.Coordinate = -1);
				_pluginData.data.Add("RouteRuleList", MessagePackSerializer.Serialize(_tempRouteRuleList));
				SetCoordinateExtendedData(coordinate, _pluginData);
			}

			internal void OnReload_Prefix()
			{
				RouteRuleList.Clear();
				PluginData _pluginData = GetExtendedData();

				if (_pluginData == null) return;

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
			}

			internal void OnCoordinateBeingLoaded_Prefix(ChaFileCoordinate _coordinate)
			{
				RouteRuleList.RemoveAll(x => x.Coordinate == _currentCoordinateIndex);
				PluginData _pluginData = GetCoordinateExtendedData(_coordinate);

				if (_pluginData == null) return;

				if (_pluginData.version == 1)
				{
					if (_pluginData.data.TryGetValue("OutfitTrigger", out object _loadedOutfitTrigger) && _loadedOutfitTrigger != null)
					{
						List<RouteRuleV1> _tempOutfitTrigger = MessagePackSerializer.Deserialize<List<RouteRuleV1>>((byte[]) _loadedOutfitTrigger);
						if (_tempOutfitTrigger?.Count > 0)
						{
							List<RouteRule> _tempRouteRuleList = MigrationV1(_tempOutfitTrigger);
							_tempRouteRuleList.ForEach(x => x.Coordinate = _currentCoordinateIndex);
							RouteRuleList.AddRange(_tempRouteRuleList);
						}
					}
				}
				else
				{
					if (_pluginData.data.TryGetValue("RouteRuleList", out object _loadedRouteRuleList) && _loadedRouteRuleList != null)
					{
						List<RouteRule> _tempRouteRuleList = MessagePackSerializer.Deserialize<List<RouteRule>>((byte[]) _loadedRouteRuleList);
						if (_tempRouteRuleList?.Count > 0)
						{
							_tempRouteRuleList.ForEach(x => x.Coordinate = _currentCoordinateIndex);
							RouteRuleList.AddRange(_tempRouteRuleList);
						}
					}
				}
			}

			internal void ApplyGameObjectRules(GameObject _gameObject)
			{
				if (_gameObject == null || _gameObject.transform.childCount == 0) return;

				ObjectType _type = GetObjectType(_gameObject);
				int _coordinateIndex = -1;
				if (_type == ObjectType.Clothing || _type == ObjectType.Accessory)
					_coordinateIndex = _currentCoordinateIndex;

				List<RouteRule> _gameObjectRules = RouteRuleList.Where(x => x.ObjectType == _type && x.Coordinate == _coordinateIndex && x.GameObjectName == _gameObject.name).ToList();
				if (_gameObjectRules?.Count == 0) return;

				//HashSet<string> _rendererNames = new HashSet<string>(_gameObjectRules.Select(x => x.RendererName));
				//if (_gameObject.name == "cf_j_root") //todo

				List<Renderer> _renderers = _gameObject.GetComponentsInChildren<Renderer>(true)?.ToList();
				if (_renderers?.Count == 0) return;

				foreach (Renderer _renderer in _renderers)
				{
					List<RouteRule> _rendererRules = _gameObjectRules.Where(x => x.RendererName == _renderer.name).OrderByDescending(x => (int) x.Action).ToList();
					if (_rendererRules?.Count == 0) continue;

					foreach (RouteRule _rendererRule in _rendererRules)
					{
						if (_rendererRule.Action == Action.Rename)
						{
							foreach (Material x in _renderer.materials)
							{
								if (x.NameFormatted() == _rendererRule.NewName)
								{
									DebugMsg(LogLevel.Error, $"[ApplyRules] Material {_rendererRule.OldName} already renamed");
									continue;
								}

								if (x.NameFormatted() == _rendererRule.OldName)
								{
									x.name = _rendererRule.NewName;
									DebugMsg(LogLevel.Info, $"[ApplyRules][Rename][{_rendererRule.GameObjectName}][{_rendererRule.RendererName}][{_rendererRule.OldName}][{_rendererRule.NewName}][{_renderer.materials.Length}]");
								}
							}
						}
						else if (_rendererRule.Action == Action.Clone)
						{
							if (!_renderer.materials.Any(x => x.NameFormatted() == _rendererRule.NewName))
							{
								Material _material = _renderer.materials.FirstOrDefault(x => x.NameFormatted() == _rendererRule.OldName);
								if (_material != null)
								{
									Material _copy = new Material(_material);
									_copy.CopyPropertiesFromMaterial(_renderer.material);
									_copy.name = _rendererRule.NewName;
									_renderer.materials = _renderer.materials.Add(_copy);
									DebugMsg(LogLevel.Info, $"[ApplyRules][Clone][{_rendererRule.GameObjectName}][{_rendererRule.RendererName}][{_rendererRule.OldName}][{_rendererRule.NewName}][{_renderer.materials.Length}]");
								}
							}
						}
					}
				}
			}

			internal RouteRule GetRule(RouteRule _rule)
			{
				if (_rule == null) return null;
				return RouteRuleList.FirstOrDefault(x => x.ObjectType == _rule.ObjectType && x.Coordinate == _rule.Coordinate && x.GameObjectName == _rule.GameObjectName && x.RendererName == _rule.RendererName && x.Action == _rule.Action && x.OldName == _rule.OldName && x.NewName == _rule.NewName);
			}

			internal bool NewNameOK(RouteRule _rule, string _newName)
			{
				if (_rule == null) return false;
				if (_newName.Trim().IsNullOrEmpty() || _newName == _rule.OldName) return false;
				return !RouteRuleList.Any(x => x.ObjectType == _rule.ObjectType && x.Coordinate == _rule.Coordinate && x.GameObjectName == _rule.GameObjectName && x.RendererName == _rule.RendererName /*&& x.Action == _rule.Action && x.OldName == _rule.OldName*/ && x.NewName == _newName);
			}
		}

		internal static MaterialRouterController GetController(ChaControl _chaCtrl) => _chaCtrl?.gameObject.GetComponent<MaterialRouterController>();
	}
}
