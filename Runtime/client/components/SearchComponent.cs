using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using Nox.Search;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Logger = Nox.CCK.Utils.Logger;
using Transform = UnityEngine.Transform;

namespace api.nox.search.client {
	public class SearchComponent : MonoBehaviour {
		public Button               submitButton;
		public Image                imageButton;
		public TMPro.TMP_InputField inputField;
		public Image                inputImage;
		public GameObject           inputImageContainer;
		public RectTransform        workerListContainer;
		public GameObject           workersContainer;
		public GameObject           resultContainer;
		public TextLanguage         resultText;
		public GameObject           infoContainer;
		public TextLanguage         infoText;
		public GameObject           infoHandlerContainer;
		public TextLanguage         infoHandlerText;
		public GameObject           handlerContainer;
		public RectTransform        handlerListContainer;
		public SearchPage           Page;

		private void OnDestroy() {
			if (Page == null) return;
			Page.OnWorkerTaskUpdate.RemoveListener(OnWorkerTaskUpdate);
			Page.OnWorkerTaskStart.RemoveListener(OnWorkerTaskStart);
			Page.OnHandlerUpdate.RemoveListener(UpdateHandler);
			inputField.onSubmit.RemoveListener(OnSubmit);
			inputField.onValueChanged.RemoveListener(OnQueryChanged);
			submitButton.onClick.RemoveListener(OnButtonClick);
		}

		private void Start() {
			if (Page == null) return;
			Page.OnWorkerTaskUpdate.AddListener(OnWorkerTaskUpdate);
			Page.OnWorkerTaskStart.AddListener(OnWorkerTaskStart);
			Page.OnHandlerUpdate.AddListener(UpdateHandler);
			resultText.UpdateText("search.start_search");
			resultContainer.gameObject.SetActive(true);
			workersContainer.SetActive(false);
			inputField.onSubmit.AddListener(OnSubmit);
			inputField.onValueChanged.AddListener(OnQueryChanged);
			submitButton.onClick.AddListener(OnButtonClick);
			UpdateSearchButtons();
			UpdateData();
		}

		private void OnButtonClick() {
			if (Page.IsFetching) {
				OnCancel();
			} else OnSubmit();

			UpdateSearchButtons();
		}

		private void OnWorkerTaskStart(WorkerTask[] tasks) {
			Logger.LogDebug($"SearchComponent: OnWorkerTaskStart {tasks.Length} tasks");

			foreach (Transform tf in workerListContainer)
				Destroy(tf.gameObject);

			if (tasks.Length == 0) {
				resultText.UpdateText("search.no_worker");
				resultContainer.SetActive(true);
				workersContainer.gameObject.SetActive(false);
				return;
			}

			var workerAsset = Client.GetAsset<GameObject>("prefabs/worker.prefab");

			foreach (var task in tasks) {
				var content   = Instantiate(workerAsset, workerListContainer);
				var component = content.GetComponent<WorkerComponent>();
				component.Initiate(this, task);
				content.name = $"[worker_{task.Uid}]";
				content.SetActive(true);
			}


			resultContainer.gameObject.SetActive(false);
			workersContainer.gameObject.SetActive(true);
			UpdateLayout.UpdateImmediate(workersContainer);
			UpdateSearchButtons();
		}

		private void OnWorkerTaskUpdate(WorkerTask task) {
			Logger.LogDebug($"SearchComponent: OnWorkerTaskUpdate {task.Uid} {task.Status}");

			foreach (Transform tf in workerListContainer) {
				var component = tf.GetComponent<WorkerComponent>();
				if (!component || component.Task.Uid != task.Uid) continue;
				component.UpdateData(task);
			}

			UpdateLayout.UpdateImmediate(workersContainer);
			UpdateSearchButtons();
		}


		private void OnSubmit(string query) {
			OnQueryChanged(query);
			OnSubmit();
			UpdateSearchButtons();
		}

		private void OnQueryChanged(string query) {
			Page.Query = query.Trim();
			UpdateSearchButtons();
		}

		private void UpdateSearchButtons() {
			var texture = Client.GetAsset<Texture2D>(
				Page.IsFetching
					? "ui:icons/cancel.png"
					: Page.IsNewQuery
						? "ui:icons/search.png"
						: "ui:icons/refresh.png"
			);
			if (!texture) return;
			imageButton.sprite = Sprite.Create(
				texture,
				new Rect(0, 0, texture.width, texture.height),
				new Vector2(0.5f, 0.5f)
			);
		}

		private void OnSubmit()
			=> Page?.Submit().Forget();

		private void OnCancel()
			=> Page?.Cancel();

		private void UpdateData() {
			inputField.text = Page.Query.Trim();
			UpdateSearchButtons();
			var handler        = Page.Handler;
			var placeholderKey = handler.GetPlaceholderKey();
			inputField.placeholder.gameObject.GetComponent<TextLanguage>()
				?.UpdateText(
					string.IsNullOrEmpty(placeholderKey)
						? "search.query.placeholder"
						: placeholderKey,
					handler.GetPlaceholderArguments() ?? Array.Empty<string>()
				);
			var icon = handler.GetIcon();
			if (icon) {
				inputImage.sprite = Sprite.Create(
					icon,
					new Rect(0, 0, icon.width, icon.height),
					new Vector2(0.5f, 0.5f)
				);
				inputImageContainer.SetActive(true);
			} else inputImageContainer.SetActive(false);

			var desc = handler.GetDescriptionKey();
			if (!string.IsNullOrEmpty(desc)) {
				infoText.UpdateText(desc, handler.GetDescriptionArguments() ?? Array.Empty<string>());
				infoContainer.SetActive(true);
			} else infoContainer.SetActive(false);

			UpdateHandler(Main.Instance.Handlers.ToArray());
		}

		private void UpdateHandler(IHandler[] handlers) {
			if (handlers.Length == 0) {
				infoHandlerText.UpdateText("search.no_handler");
				infoHandlerContainer.SetActive(true);
				handlerContainer.gameObject.SetActive(false);
				return;
			}

			infoHandlerContainer.SetActive(false);
			handlerContainer.gameObject.SetActive(true);

			// Clear not used handlers
			var ids = new List<string>();

			foreach (Transform tf in handlerListContainer) {
				var component = tf.GetComponent<HandlerComponent>();
				if (!component) {
					Destroy(tf.gameObject);
					continue;
				}

				var handler = Array.Find(handlers, h => h.GetId() == component.Data.GetId());
				if (handler == null) {
					Destroy(tf.gameObject);
					continue;
				}

				component.UpdateData(handler);
				ids.Add(handler.GetId());
			}

			// Add new handlers
			var btn = Client.GetAsset<GameObject>("ui:prefabs/btn_icon.prefab");
			foreach (var handler in handlers) {
				if (string.IsNullOrEmpty(handler.GetId())) continue;
				if (ids.Contains(handler.GetId())) continue;
				ids.Add(handler.GetId());
				var content   = Instantiate(btn, handlerListContainer);
				var component = content.AddComponent<HandlerComponent>();
				component.icon          = Reference.GetComponent<Image>("image", content);
				component.iconContainer = Reference.GetComponent<RectTransform>("image_container", content);
				component.button        = content.GetComponent<Button>();
				component.text          = Reference.GetComponent<TextLanguage>("text", content);
				component.Initiate(handler);
				component.button.onClick.AddListener(() => OnChangeHandler(handler));
				content.name = $"{handler.GetId()}_{content.name}";
				content.SetActive(true);
			}

			UpdateLayout.UpdateImmediate(handlerContainer);
		}

		private void OnChangeHandler(IHandler handler) {
			if (Page.Handler == handler) return;
			Page.Handler = handler;
			UpdateData();
		}
	}
}