using System;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.UI;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Search.Runtime.Clients {
	public class HandlerComponent : MonoBehaviour {
		public Image         icon;
		public RectTransform iconContainer;
		public TextLanguage  text;
		public Button        button;

		public IHandler Data;


		public void Initiate(IHandler data) {
			if (Data != null) return;
			UpdateData(data);
		}

		private void OnDestroy() {
			Data = null;
		}

		internal void UpdateData(IHandler data) {
			Data = data;
			if (Data == null) {
				Logger.LogError("HandlerComponent: Data is not initiated.");
				return;
			}

			var texture = data.GetIcon();
			if (!texture) {
				icon.sprite = null;
				iconContainer.gameObject.SetActive(false);
			} else {
				icon.sprite = SpriteConvertor.Convert(texture);
				iconContainer.gameObject.SetActive(true);
			}

			text.UpdateText(data.GetTitleKey(), data.GetTitleArguments() ?? Array.Empty<string>());
		}
	}
}
