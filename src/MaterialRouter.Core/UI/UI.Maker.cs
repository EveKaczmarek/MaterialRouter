using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using JetPack;

namespace MaterialRouter
{
	public partial class MaterialRouter
	{
		internal static MaterialRouterUI _charaConfigWindow;

		internal class MaterialRouterUI : TemplateUI
		{
			private MaterialRouterController _pluginCtrl => _chaCtrl?.gameObject?.GetComponent<MaterialRouterController>();

			private Vector2 _listScrollPos = Vector2.zero;
			private readonly GUILayoutOption _buttonElem = GUILayout.Width(60);

			internal bool _cfgAutoRefresh = false;
			internal GameObject _curGameObject = null;
			private static HashSet<string> _ignoreList = new HashSet<string>() { "AAAPK_indicator", "BendUrAcc_indicator" };
			private RouteRule _editRule = null;
			private string _editNameHolder = "", _editMode = "";

			internal void ResetEdit()
			{
				_editRule = null;
				_editNameHolder = "";
				_editMode = "";
			}

			protected override void Awake()
			{
				_windowSize = new Vector2(400, 525);

				_windowInitPos.x = 525;
				_windowInitPos.y = 80;

				_cfgAutoRefresh = MaterialRouter._cfgAutoRefresh.Value;

				base.Awake();
			}
			/*
			protected override void InitStyle()
			{
				base.InitStyle();
			}
			*/
			internal override void CloseWindow()
			{
				_curGameObject = null;
				base.CloseWindow();
			}

			protected override void OnGUI()
			{
				if (_curGameObject == null)
				{
					enabled = false;
					return;
				}

				base.OnGUI();
			}

			protected override void DrawDragWindow(int _windowID)
			{
				_windowTitle = "Material Router - " + _curGameObject?.name;
				base.DrawDragWindow(_windowID);
			}

			protected override void DragWindowContent()
			{
				GUILayout.BeginHorizontal(GUI.skin.box, GUILayout.ExpandHeight(true));
				{
					GUILayout.BeginVertical();
					{
						if (!(_curGameObject == null))
						{
							Renderer[] _renderers = _curGameObject?.GetComponentsInChildren<Renderer>(true);

							_listScrollPos = GUILayout.BeginScrollView(_listScrollPos, false, false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUI.skin.box);
							{
								if (_renderers?.Length > 0)
								{
									foreach (Renderer _renderer in _renderers)
									{
										if (_renderer == null) continue;
										if (_ignoreList.Contains(_renderer.name) || _renderer.name.StartsWith("AccGotHigh_")) continue;

										GameObject _gameObject = _renderer.GetComponentInParent<ListInfoComponent>()?.gameObject;
										ObjectType _objectType = GetObjectType(_gameObject);
										List<RouteRule> _rules = _pluginCtrl.RouteRuleList.Where(x => x.ObjectType == _objectType && x.GameObjectName == _gameObject.name && x.RendererName == _renderer.name).OrderBy(x => (int) x.Action).ToList();

										GUILayout.BeginHorizontal(GUI.skin.box, GUILayout.ExpandWidth(true));
										{
											GUILayout.Label(_renderer.name, _label);
										}
										GUILayout.EndHorizontal();

										Material[] _materials = _renderer.materials;
										if (_materials?.Length > 0)
										{
											foreach (Material _material in _materials)
											{
												RouteRule _rule = _rules.FirstOrDefault(x => x.NewName == _material.NameFormatted());
												string _realOldName = _material.NameFormatted();

												if (_rule != null && _rule.Action == Action.Rename)
												{
													_realOldName = _rule.OldName;
													GUILayout.BeginHorizontal(GUI.skin.box, GUILayout.ExpandWidth(true));
													{
														GUILayout.Label(_rule.OldName, _labelDisabled);

														GUILayout.FlexibleSpace();

														GUI.enabled = false;
														GUILayout.Button(new GUIContent("rename", "Only one rename rule can be applied to each material"), _buttonElem);
														GUI.enabled = true;

														if (GUILayout.Button(new GUIContent("clone", "Clone the material into a new copy"), _buttonElem))
														{
															_editMode = "new";
															_editRule = new RouteRule();
															_editRule.ObjectType = _rule.ObjectType;
															_editRule.Coordinate = _rule.Coordinate;
															_editRule.GameObjectName = _rule.GameObjectName;
															_editRule.RendererName = _rule.RendererName;
															_editRule.Action = Action.Clone;
															_editRule.OldName = _realOldName;
															_editRule.NewName = _editRule.OldName;
															_editNameHolder = _editRule.OldName;
														}
													}
													GUILayout.EndHorizontal();
												}

												GUILayout.BeginHorizontal(GUI.skin.box, GUILayout.ExpandWidth(true));
												{
													if (_rule != null)
													{
														if (_editRule == _rule && _editMode == "edit")
														{
															_editNameHolder = GUILayout.TextField(_editNameHolder, GUILayout.Width(200), GUILayout.ExpandWidth(false));

															GUILayout.FlexibleSpace();

															if (GUILayout.Button("save", _buttonElem))
															{
																if (_pluginCtrl.NewNameOK(_editRule, _editNameHolder))
																{
																	_renderer.materials.First(x => x.NameFormatted() == _rule.NewName).name = _editNameHolder;
																	_rule.NewName = _editNameHolder;

																	if (_cfgAutoRefresh)
																	{
																		if (_editRule.ObjectType == ObjectType.Character || _editRule.ObjectType == ObjectType.Hair)
																			ReloadChara(_chaCtrl);
																		else
																			_chaCtrl.ChangeCoordinateTypeAndReload(false);
																	}
																	else
																		_logger.LogMessage($"Please refresh your character to take full effect");

																	ResetEdit();
																}
																else
																{
																	_logger.LogMessage($"{_editNameHolder} is not available");
																}
															}

															if (GUILayout.Button("cancel", _buttonElem))
															{
																ResetEdit();
															}
														}
														else
														{
															GUILayout.Label($"[{_rule.Action}]", _labelAlignCenterCyan, _buttonElem);
															GUILayout.Label(_material.NameFormatted(), _labelDisabled);

															GUILayout.FlexibleSpace();

															if (GUILayout.Button(new GUIContent("edit", "Edit current setting"), _buttonElem))
															{
																_editMode = "edit";
																_editRule = _rule;
																_editNameHolder = _rule.NewName;
															}

															if (GUILayout.Button(new GUIContent("delete", "Remove the setting"), _buttonElem))
															{
																if (_rule.Action == Action.Clone)
																{
																	List<Material> _temp = _renderer.materials.ToList();
																	_temp.RemoveAll(x => x.NameFormatted() == _rule.NewName);
																	_renderer.materials = _temp.ToArray();
																}
																else if (_rule.Action == Action.Rename)
																{
																	Material _temp = _renderer.materials.FirstOrDefault(x => x.NameFormatted() == _rule.NewName);
																	if (_temp != null)
																		_temp.name = _rule.OldName;
																}
																_pluginCtrl.RouteRuleList.Remove(_rule);
															}
														}
													}
													else
													{
														GUILayout.Label(_material.NameFormatted(), _labelDisabled);

														GUILayout.FlexibleSpace();

														if (_rules.Any(x => x.Action == Action.Rename) || (_editMode == "new" && _editRule.Action == Action.Rename) || _material.name.Contains("MECopy"))
															GUI.enabled = false;
														if (GUILayout.Button(new GUIContent("rename", "Rename the material"), _buttonElem))
														{
															_editMode = "new";
															_editRule = new RouteRule();
															_editRule.ObjectType = _objectType;
															_editRule.Coordinate = (_objectType == ObjectType.Character || _objectType == ObjectType.Hair) ? -1 : _currentCoordinateIndex;
															_editRule.GameObjectName = _gameObject.name;
															_editRule.RendererName = _renderer.name;
															_editRule.Action = Action.Rename;
															_editRule.OldName = _material.NameFormatted();
															_editRule.NewName = _editRule.OldName;
															_editNameHolder = _editRule.OldName;
														}
														if (!_material.name.Contains("MECopy"))
															GUI.enabled = true;
														if (GUILayout.Button(new GUIContent("clone", "Clone the material into a new copy"), _buttonElem))
														{
															_editMode = "new";
															_editRule = new RouteRule();
															_editRule.ObjectType = _objectType;
															_editRule.Coordinate = (_objectType == ObjectType.Character || _objectType == ObjectType.Hair) ? -1 : _currentCoordinateIndex;
															_editRule.GameObjectName = _gameObject.name;
															_editRule.RendererName = _renderer.name;
															_editRule.Action = Action.Clone;
															_editRule.OldName = _material.NameFormatted();
															_editRule.NewName = _editRule.OldName;
															_editNameHolder = _editRule.OldName;
														}
														GUI.enabled = true;
													}
												}
												GUILayout.EndHorizontal();

												if (_editMode == "new" && _editRule.RendererName == _renderer.name && _editRule.OldName == _realOldName)
												{
													GUILayout.BeginHorizontal(GUI.skin.box, GUILayout.ExpandWidth(true));
													{
														_editNameHolder = GUILayout.TextField(_editNameHolder, GUILayout.Width(200), GUILayout.ExpandWidth(false));
														GUILayout.FlexibleSpace();
														if (GUILayout.Button("save", _buttonElem))
														{
															if (_pluginCtrl.NewNameOK(_editRule, _editNameHolder))
															{
																_editRule.NewName = _editNameHolder;
																_pluginCtrl.RouteRuleList.Add(_editRule);
																/*
																if (_editRule.Action == Action.Clone)
																{
																	if (_material.NameFormatted() != _realOldName)
																		_logger.LogMessage($"Please refresh your character to take full effect");
																}
																*/
																if (_cfgAutoRefresh)
																{
																	if (_editRule.ObjectType == ObjectType.Character || _editRule.ObjectType == ObjectType.Hair)
																		ReloadChara(_chaCtrl);
																	else
																		_chaCtrl.ChangeCoordinateTypeAndReload(false);
																}
																else
																	_logger.LogMessage($"Please refresh your character to take full effect");

																ResetEdit();
																_pluginCtrl.ApplyGameObjectRules(_gameObject);
															}
															else
															{
																_logger.LogMessage($"{_editNameHolder} is not available");
															}
														}

														if (GUILayout.Button("cancel", _buttonElem))
														{
															ResetEdit();
														}
													}
													GUILayout.EndHorizontal();
												}
											}
										}
									}
								}
							}
							GUILayout.EndScrollView();
						}
					}
					GUILayout.EndVertical();
				}
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal(GUI.skin.box);
				{
					GUILayout.Label("Refresh", _label);

					if (GUILayout.Button("chara", _buttonElem))
					{
						ReloadChara(_chaCtrl);
					}

					if (GUILayout.Button("coord", _buttonElem))
					{
						_chaCtrl.ChangeCoordinateTypeAndReload(false);
					}

					bool _boolAutoRefresh = _cfgAutoRefresh;
					if (_boolAutoRefresh != GUILayout.Toggle(_cfgAutoRefresh, new GUIContent(" Auto Refresh", "Auto refresh on setting changed to take effect")))
					{
						_cfgAutoRefresh = !_cfgAutoRefresh;
						//if (MaterialRouter._cfgAutoRefresh.Value != _cfgAutoRefresh)
						MaterialRouter._cfgAutoRefresh.Value = _cfgAutoRefresh;
					}

					GUILayout.FlexibleSpace();
				}
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal(GUI.skin.box);
				GUILayout.Label(GUI.tooltip);
				GUILayout.EndHorizontal();
			}
		}
	}
}
