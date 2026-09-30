using System;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Network;
using Nox.CCK.Utils;
using UnityEngine;
using UnityEngine.UI;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Search.Runtime.Clients {
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
			UpdateImage(Data);
		}

		private void UpdateImage(IResultData data)
			=> UpdateImageAsync(data).Forget();

		private bool _imageLoading;

		private async UniTask UpdateImageAsync(IResultData data) {
			if (_imageLoading) return;
			_imageLoading = true;

			try {
				var image = await data.Image;

				// A remote url is handled by NetworkImage (size aware request + caching).
				if (image.HasUrl) {
					var networkImage = icon.GetOrAddComponent<NetworkImage>();
					networkImage.Url = image.Url;
					icon.gameObject.SetActive(true);
					return;
				}

				if (image.HasTexture) {
					icon.texture = image.Texture;
					icon.gameObject.SetActive(true);
					return;
				}

				icon.gameObject.SetActive(false);
				icon.texture = null;
			} catch {
				icon.gameObject.SetActive(false);
				icon.texture = null;
			} finally {
				_imageLoading = false;
			}
		}
	}
}