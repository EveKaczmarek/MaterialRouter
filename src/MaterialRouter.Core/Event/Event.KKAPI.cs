using System.Collections.Generic;
using System.Linq;

using ChaCustom;
using UnityEngine;
using UniRx;

using HarmonyLib;

using KKAPI.Maker;
using KKAPI.Maker.UI;

namespace MaterialRouter
{
	public partial class MaterialRouter
	{
		internal void InitEvent_KKAPI()
		{
			MakerAPI.RegisterCustomSubCategories += (_sender, _args) =>
			{
				ChaControl _chaCtrl = CustomBase.Instance.chaCtrl;
				MakerCategory _category = new MakerCategory("05_ParameterTop", "tglMaterialRouter", MakerConstants.Parameter.Attribute.Position + 1, "Router");
				_args.AddSubCategory(_category);

				_args.AddControl(new MakerText("BodyTrigger", _category, this));

				_args.AddControl(new MakerButton("Export", _category, this)).OnClick.AddListener(delegate { _makerPluginCtrl.ExportBodyTrigger(); });
				_args.AddControl(new MakerButton("Import", _category, this)).OnClick.AddListener(delegate { _makerPluginCtrl.ImportBodyTrigger(); });
				_args.AddControl(new MakerButton("Reset", _category, this)).OnClick.AddListener(delegate { _makerPluginCtrl.ResetBodyTrigger(); });

				_args.AddControl(new MakerSeparator(_category, this));

				_args.AddControl(new MakerText("OutfitTriggers", _category, this));

				_args.AddControl(new MakerButton("Export", _category, this)).OnClick.AddListener(delegate { _makerPluginCtrl.ExportOutfitTrigger(); });
				_args.AddControl(new MakerButton("Import", _category, this)).OnClick.AddListener(delegate { _makerPluginCtrl.ImportOutfitTrigger(); });
				_args.AddControl(new MakerButton("Reset", _category, this)).OnClick.AddListener(delegate { _makerPluginCtrl.ResetOutfitTrigger(); });

				const string _labelMaterialRouter = "Material Router";

				//_args.AddControl(new MakerButton($"{_labelMaterialRouter} (Body)", MakerConstants.Face.All, this)).OnClick.AddListener(() => PopupMenu(_chaCtrl.objBody, true));
				_args.AddControl(new MakerButton($"{_labelMaterialRouter} (Face)", MakerConstants.Face.All, this)).OnClick.AddListener(() => PopupMenu(_chaCtrl.objHead));

				_args.AddControl(new MakerButton(_labelMaterialRouter, MakerConstants.Clothes.Top, this)).OnClick.AddListener(() => PopupMenu(_chaCtrl.objClothes[0]));
				_args.AddControl(new MakerButton(_labelMaterialRouter, MakerConstants.Clothes.Bottom, this)).OnClick.AddListener(() => PopupMenu(_chaCtrl.objClothes[1]));
				_args.AddControl(new MakerButton(_labelMaterialRouter, MakerConstants.Clothes.Bra, this)).OnClick.AddListener(() => PopupMenu(_chaCtrl.objClothes[2]));
				_args.AddControl(new MakerButton(_labelMaterialRouter, MakerConstants.Clothes.Shorts, this)).OnClick.AddListener(() => PopupMenu(_chaCtrl.objClothes[3]));
				_args.AddControl(new MakerButton(_labelMaterialRouter, MakerConstants.Clothes.Gloves, this)).OnClick.AddListener(() => PopupMenu(_chaCtrl.objClothes[4]));
				_args.AddControl(new MakerButton(_labelMaterialRouter, MakerConstants.Clothes.Panst, this)).OnClick.AddListener(() => PopupMenu(_chaCtrl.objClothes[5]));
				_args.AddControl(new MakerButton(_labelMaterialRouter, MakerConstants.Clothes.Socks, this)).OnClick.AddListener(() => PopupMenu(_chaCtrl.objClothes[6]));
#if KK
				_args.AddControl(new MakerButton(_labelMaterialRouter, MakerConstants.Clothes.InnerShoes, this)).OnClick.AddListener(() => PopupMenu(_chaCtrl.objClothes[7]));
#endif
				_args.AddControl(new MakerButton(_labelMaterialRouter, MakerConstants.Clothes.OuterShoes, this)).OnClick.AddListener(() => PopupMenu(_chaCtrl.objClothes[8]));

				_args.AddControl(new MakerButton(_labelMaterialRouter, MakerConstants.Hair.Back, this)).OnClick.AddListener(() => PopupMenu(_chaCtrl.objHair[0]));
				_args.AddControl(new MakerButton(_labelMaterialRouter, MakerConstants.Hair.Front, this)).OnClick.AddListener(() => PopupMenu(_chaCtrl.objHair[1]));
				_args.AddControl(new MakerButton(_labelMaterialRouter, MakerConstants.Hair.Side, this)).OnClick.AddListener(() => PopupMenu(_chaCtrl.objHair[2]));
				_args.AddControl(new MakerButton(_labelMaterialRouter, MakerConstants.Hair.Extension, this)).OnClick.AddListener(() => PopupMenu(_chaCtrl.objHair[3]));

				_bottonMaterialRouter = MakerAPI.AddAccessoryWindowControl(new MakerButton(_labelMaterialRouter, null, this));
				_bottonMaterialRouter.OnClick.AddListener(() => PopupMenu(JetPack.Accessory.GetObjAccessory(_chaCtrl, JetPack.CharaMaker.CurrentAccssoryIndex)));
				_bottonMaterialRouter.Visible.OnNext(false);

				//if (JetPack.Game.ConsoleActive)
				{
					_args.AddControl(new MakerSeparator(_category, this));

					_args.AddControl(new MakerText("Tools", _category, this));

					_args.AddControl(new MakerButton("Info", _category, _instance)).OnClick.AddListener(delegate
					{
						List<RouteRule> _rules = _makerPluginCtrl.RouteRuleList;
						for (int i = -1; i < _chaCtrl.chaFile.coordinate.Length; i++)
							_logger.LogInfo($"[RouteRuleList][{i}][{_rules.Where(x => x.Coordinate == i)?.Count()}]");
					});
				}
			};

			MakerAPI.MakerBaseLoaded += (_sender, _args) =>
			{
				_hooksMakerInstance = Harmony.CreateAndPatchAll(typeof(HooksMaker), "MaterialRouterMaker");
			};

			MakerAPI.MakerFinishedLoading += (_sender, _args) =>
			{
				_charaConfigWindow = _instance.gameObject.AddComponent<MaterialRouterUI>();
			};

			MakerAPI.MakerExiting += (_sender, _args) =>
			{
				_charaConfigWindow = null;
				_hooksMakerInstance.UnpatchAll(_hooksMakerInstance.Id);
				_hooksMakerInstance = null;
			};

			AccessoriesApi.AccessoryTransferred += (_sender, _args) =>
			{
				_makerPluginCtrl.AccessoryTransferEvent(_args);
			};

			AccessoriesApi.AccessoriesCopied += (_sender, _args) =>
			{
				_makerPluginCtrl.AccessoryCopyEvent(_args);
			};
		}

		internal static void PopupMenu(GameObject _gameObject)
		{
			_charaConfigWindow._curGameObject = _gameObject;
			_charaConfigWindow.enabled = _gameObject != null;
		}
	}
}
