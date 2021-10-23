using System.Collections;
using System.Collections.Generic;

using ChaCustom;

namespace MaterialRouter
{
	public partial class MaterialRouter
	{
		internal static List<string> _mappingToggleHair = new List<string>() { "tglBack", "tglFront", "tglSide", "tglExtension" };
		internal static List<string> _mappingToggleClothes = new List<string>() { "tglTop", "tglBot", "tglBra", "tglShorts", "tglGloves", "tglPanst", "tglSocks", "tglInnerShoes", "tglOuterShoes" };

		internal void InitEvent_JetPack()
		{
			JetPack.Chara.OnChangeCoordinateType += (_sender, _args) =>
			{
				if (_args.State == "Coroutine")
					InitCurrentSlot();
			};

			JetPack.CharaMaker.OnCvsNavMenuClick += (_sender, _args) =>
			{
				ChaControl _chaCtrl = CustomBase.Instance.chaCtrl;

				if (_args.TopIndex == 4)
					StartCoroutine(InitCurrentSlotCoroutine());
				if (_charaConfigWindow == null)
					return;

				if (_args.TopIndex == 4)
				{
					if (JetPack.CharaMaker.CurrentAccssoryIndex < 0)
					{
						_charaConfigWindow.SetWindowClose();
						return;
					}

					_charaConfigWindow._curGameObject = JetPack.Accessory.GetObjAccessory(_chaCtrl, JetPack.CharaMaker.CurrentAccssoryIndex);
				}
				else if (_args.TopIndex == 3)
				{
					int i = _mappingToggleClothes.IndexOf(_args.SideToggle.name);
					if (i < 0)
					{
						_charaConfigWindow.SetWindowClose();
						return;
					}
					_charaConfigWindow._curGameObject = _chaCtrl.objClothes[i];
				}
				else if (_args.TopIndex == 2)
				{
					int i = _mappingToggleHair.IndexOf(_args.SideToggle.name);
					if (i < 0)
					{
						_charaConfigWindow.SetWindowClose();
						return;
					}
					_charaConfigWindow._curGameObject = _chaCtrl.objHair[i];
				}
				else
				{
					_charaConfigWindow._curGameObject = null;
					_charaConfigWindow.enabled = false;
					return;
				}
			};
		}

		internal static void InitCurrentSlot()
		{
			if (!JetPack.CharaMaker.Loaded) return;

			if (JetPack.Accessory.GetObjAccessory(CustomBase.Instance.chaCtrl, JetPack.CharaMaker.CurrentAccssoryIndex) == null)
				_bottonMaterialRouter.Visible.OnNext(false);
			else
				_bottonMaterialRouter.Visible.OnNext(true);
		}

		internal static IEnumerator InitCurrentSlotCoroutine()
		{
			yield return JetPack.Toolbox.WaitForEndOfFrame;
			yield return JetPack.Toolbox.WaitForEndOfFrame;
			InitCurrentSlot();
		}
	}
}
