using System;
using System.Collections.Generic;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using Nox.Search;
using UnityEngine;
using Logger = Nox.CCK.Utils.Logger;
using Transform = UnityEngine.Transform;

namespace api.nox.search.client {
	public class WorkerComponent : MonoBehaviour {
		public TextLanguage title;
		public TextLanguage message;
		internal WorkerTask Task;
		public SearchComponent search;
		public RectTransform resultContainer;

		public GridFitter fitter;

		public void Initiate(SearchComponent s, WorkerTask task) {
			search = s;
			UpdateData(task);
		}

		private void OnDestroy()
			=> Task = null;

		private void Awake()
			=> UpdateData(Task);


		public void UpdateData(WorkerTask task) {
			Task = task;
			if (Task == null) {
				Logger.LogError("WorkerComponent: Task is not initiated.");
				return;
			}

			Logger.LogDebug($"WorkerComponent: UpdateData {Task.Worker.TitleKey} {Task.Status}");

			title.UpdateText(Task.Worker.TitleKey, Task.Worker.TitleArguments ?? Array.Empty<string>());
			message.UpdateText(Task.MessageKey, Task.MessageArgs);
			if (Task.Status == WorkerTaskStatus.Completed) {
				var ids = new List<int>();
				var ds = Task.Result?.Data ?? Array.Empty<IResultData>();
				fitter.ratio = Task.Worker.Ratio;
				foreach (Transform tf in resultContainer) {
					var result = tf.GetComponent<ResultComponent>();
					if (!result) {
						Destroy(tf.gameObject);
						continue;
					}

					var data = Array.Find(ds, d => d.Id == result.Data.Id);
					if (data == null) {
						Destroy(tf.gameObject);
						continue;
					}

					ids.Add(result.Data.Id);
					result.UpdateData(data);
				}

				// add missing results
				var asset = Client.GetAsset<GameObject>("prefabs/result.prefab");
				foreach (var data in ds) {
					if (ids.Contains(data.Id)) continue;
					var content = Instantiate(asset, resultContainer);
					var component = content.GetComponent<ResultComponent>();
					component.Initiate(this, data);
					content.name = $"[result_{data.Id}]";
					content.SetActive(true);
					ids.Add(data.Id);
				}

				resultContainer.gameObject.SetActive(true);
				message.gameObject.SetActive(false);
			}
			else {
				resultContainer.gameObject.SetActive(false);
				message.gameObject.SetActive(true);
			}

			UpdateLayout.UpdateImmediate(transform as RectTransform);
		}
	}
}