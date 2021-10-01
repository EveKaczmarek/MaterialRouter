using UnityEngine;
using ChaCustom;

namespace MaterialRouter
{
	public partial class MaterialRouter
	{
		public partial class TemplateUI : MonoBehaviour
		{
			public ChaControl _chaCtrl
			{
				get
				{
					/*
					if (JetPack.CharaStudio.Running)
						return JetPack.CharaStudio.CurOCIChar?.charInfo;
					else
					*/
						return CustomBase.Instance?.chaCtrl;
				}
			}
			public int _currentCoordinateIndex => (int) _chaCtrl?.fileStatus?.coordinateType;

			public int _windowRectID;
			public Rect _windowRect, _dragWindowRect;

			public Vector2 _windowSize = Vector2.zero;
			public Vector2 _windowPos = Vector2.zero;
			public Vector2 _windowInitPos = Vector2.zero;
			public Texture2D _windowBGtex = null;
			public string _windowTitle;
			public bool _hasFocus = false;
			public bool _passThrough = false;
			public bool _onAccTab = false;
			public int _slotIndex = -1;

			public Vector2 _ScreenRes = Vector2.zero;
			public bool _cfgResScaleEnable = true;
			public float _cfgScaleFactor = 1f;
			public Vector2 _resScaleFactor = Vector2.one;
			public Matrix4x4 _resScaleMatrix;

			public bool _initStyle = true;
			public GUIStyle _windowSolid;
			public GUIStyle _buttonActive;
			public GUIStyle _label;
			public GUIStyle _labelDisabled;
			public GUIStyle _labelAlignCenter;
			public GUIStyle _labelAlignCenterCyan;
#if KK
			public readonly Color _windowBG = new Color(0.5f, 0.5f, 0.5f, 1f);
#elif KKS
			public readonly Color _windowBG = new Color(0.2f, 0.2f, 0.2f, 1f);
#endif

			protected virtual void Awake()
			{
				DontDestroyOnLoad(this);
				enabled = false;

				_windowRectID = GUIUtility.GetControlID(FocusType.Passive);

				_windowPos.x = _windowInitPos.x;
				_windowPos.y = _windowInitPos.y;

				_windowBGtex = JetPack.UI.MakePlainTex((int) _windowSize.x, (int) _windowSize.y, _windowBG);
				_windowRect = new Rect(_windowPos.x, _windowPos.y, _windowSize.x, _windowSize.y);
				ChangeRes();
			}

			protected virtual void OnGUI()
			{
				if (JetPack.CharaStudio.Running)
				{
					if (JetPack.CharaStudio.CurOCIChar == null) return;
				}
				else
				{
					if (CustomBase.Instance?.chaCtrl == null) return;
					if (CustomBase.Instance.customCtrl.hideFrontUI) return;
#if KK
					if (!Manager.Scene.Instance.AddSceneName.IsNullOrEmpty() && Manager.Scene.Instance.AddSceneName != "CustomScene") return;
#endif
				}

				if (_ScreenRes.x != Screen.width || _ScreenRes.y != Screen.height)
					ChangeRes();

				if (_initStyle)
				{
					ChangeRes();
					InitStyle();
				}

				GUI.matrix = _resScaleMatrix;
				_dragWindowRect = GUILayout.Window(_windowRectID, _windowRect, DrawDragWindow, "", _windowSolid);
				_windowRect.x = _dragWindowRect.x;
				_windowRect.y = _dragWindowRect.y;

				Event _windowEvent = Event.current;
				if (EventType.MouseDown == _windowEvent.type || EventType.MouseUp == _windowEvent.type || EventType.MouseDrag == _windowEvent.type || EventType.MouseMove == _windowEvent.type)
					_hasFocus = false;

				if ((!_passThrough || _hasFocus) && JetPack.UI.GetResizedRect(_windowRect).Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
					Input.ResetInputAxes();
			}

			protected virtual void DrawDragWindow(int _windowID)
			{
#if !KK
				GUI.backgroundColor = Color.grey;
#endif
				Event _windowEvent = Event.current;
				if (EventType.MouseDown == _windowEvent.type || EventType.MouseUp == _windowEvent.type || EventType.MouseDrag == _windowEvent.type || EventType.MouseMove == _windowEvent.type)
					_hasFocus = true;

				GUI.Box(new Rect(0, 0, _windowSize.x, _windowSize.y), _windowBGtex);
				GUI.Box(new Rect(0, 0, _windowSize.x, 30), _windowTitle, new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter });

				if (GUI.Button(new Rect(_windowSize.x - 27, 4, 23, 23), new GUIContent("X", "Close this window")))
				{
					CloseWindow();
				}
				/*
				if (GUI.Button(new Rect(_windowSize.x - 50, 4, 23, 23), new GUIContent("0", "Config window will not block mouse drag from outside (experemental)"), (_passThrough ? _buttonActive : "button")))
				{
					_passThrough = !_passThrough;
					_logger.LogMessage($"Pass through mode: {(_passThrough ? "ON" : "OFF")}");
				}
				*/
				if (GUI.Button(new Rect(4, 4, 23, 23), new GUIContent("<", "Reset window position")))
				{
					ChangeRes();
				}

				if (GUI.Button(new Rect(27, 4, 23, 23), new GUIContent("T", "Use current window position when reset")))
				{
					if (_cfgResScaleEnable)
					{
						_windowPos.x = _windowRect.x * _cfgScaleFactor;
						_windowPos.y = _windowRect.y * _cfgScaleFactor;
					}
					else
					{
						_windowPos.x = _windowRect.x / _resScaleFactor.x * _cfgScaleFactor;
						_windowPos.y = _windowRect.y / _resScaleFactor.y * _cfgScaleFactor;
					}

					_windowInitPos.x = _windowPos.x;
					_windowInitPos.y = _windowPos.y;
					// todo: something to update config
				}

				GUILayout.BeginVertical();
				{
					GUILayout.Space(10);
					DragWindowContent();
				}
				GUILayout.EndVertical();
				GUI.DragWindow();
			}

			protected virtual void DragWindowContent() { }

			protected virtual void InitStyle()
			{
				_windowSolid = new GUIStyle(GUI.skin.window);
				_windowSolid.normal.background = _windowSolid.onNormal.background;

				_buttonActive = new GUIStyle(GUI.skin.button);
				_buttonActive.normal.textColor = Color.cyan;
				_buttonActive.hover.textColor = Color.cyan;
				_buttonActive.fontStyle = FontStyle.Bold;

				_label = new GUIStyle(GUI.skin.label);
				_label.clipping = TextClipping.Clip;
				_label.wordWrap = false;
				_label.normal.textColor = Color.white;

				_labelDisabled = new GUIStyle(_label);
				_labelDisabled.normal.textColor = Color.grey;

				_labelAlignCenter = new GUIStyle(_label);
				_labelAlignCenter.alignment = TextAnchor.MiddleCenter;

				_labelAlignCenterCyan = new GUIStyle(_labelAlignCenter);
				_labelAlignCenterCyan.normal.textColor = Color.cyan;

				_initStyle = false;
			}

			protected virtual void OnEnable()
			{
				_hasFocus = true;
			}

			protected virtual void OnDisable()
			{
				_initStyle = true;
				_hasFocus = false;
			}

			// https://answers.unity.com/questions/840756/how-to-scale-unity-gui-to-fit-different-screen-siz.html
			protected virtual void ChangeRes()
			{
				//_cfgScaleFactor = _cfgMakerWinScale.Value;
				_ScreenRes.x = Screen.width;
				_ScreenRes.y = Screen.height;
				_resScaleFactor.x = _ScreenRes.x / 1600;
				_resScaleFactor.y = _ScreenRes.y / 900;

				if (_cfgResScaleEnable)
					_resScaleMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(_resScaleFactor.x * _cfgScaleFactor, _resScaleFactor.y * _cfgScaleFactor, 1));
				else
					_resScaleMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(_cfgScaleFactor, _cfgScaleFactor, 1));
				ResetPos();
			}

			protected virtual void ResetPos()
			{
				_windowPos.x = _windowInitPos.x;
				_windowPos.y = _windowInitPos.y;

				if (_cfgResScaleEnable)
				{
					_windowRect.x = _windowPos.x / _cfgScaleFactor;
					_windowRect.y = _windowPos.y / _cfgScaleFactor;
				}
				else
				{
					_windowRect.x = _windowPos.x * _resScaleFactor.x / _cfgScaleFactor;
					_windowRect.y = _windowPos.y * _resScaleFactor.y / _cfgScaleFactor;
				}
			}

			internal virtual void CloseWindow()
			{
				enabled = false;
			}
		}
	}
}
