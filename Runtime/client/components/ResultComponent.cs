using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using Nox.Search;
using UnityEngine;
using UnityEngine.UI;
using Logger = Nox.CCK.Utils.Logger;

namespace api.nox.search.client {
	public class ResultComponent : MonoBehaviour {
		public   RawImage        icon;
		public   TextLanguage    text;
		public   Button          button;
		internal IResultData     Data;
		public   WorkerComponent workerComponent;

		public void Initiate(WorkerComponent wc, IResultData data) {
			workerComponent = wc;
			if (Data != null) return;
			UpdateData(data);
		}

		private void OnDestroy() {
			button.onClick.RemoveListener(OnClick);
			Data = null;
		}

		private void Awake() {
			icon.gameObject.SetActive(false);
			UpdateData(Data);
			button.onClick.AddListener(OnClick);
		}

		private void OnClick()
			=> Data.OnClick(workerComponent.search.Page.GetMenu().Id);

		public void UpdateData(IResultData data) {
			Data = data;
			if (Data == null) {
				Logger.LogError("ResultComponent: Data is not initiated.");
				return;
			}

			text.UpdateText(Data.TitleKey, Data.TitleArguments ?? Array.Empty<string>());
			UpdateImage(Data).Forget();
		}

		private bool _imageLoading;

		private async UniTask UpdateImage(IResultData data) {
			if (_imageLoading) return;
			_imageLoading = true;

			try {
				var texture = await data.Image;
				if (texture) {
					icon.texture = texture;
					icon.gameObject.SetActive(true);
				} else {
					icon.gameObject.SetActive(false);
					icon.texture = null;
				}
			} catch {
				icon.gameObject.SetActive(false);
				icon.texture = null;
			}

			_imageLoading = false;
		}
	}
}