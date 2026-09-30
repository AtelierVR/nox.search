using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Nox.CCK.Language;
using Nox.CCK.Utils;
using Nox.UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Search.Runtime.Clients {
	public class SearchPage : IPage {
		internal static string GetStaticKey()
			=> "search";

		public string GetKey()
			=> GetStaticKey();

		private readonly int _mId;
		private readonly object[] _context;
		private GameObject _content;

		private static bool T<T>(object[] o, int index, out T value) {
			if (o.Length > index && o[index] is T t) {
				value = t;
				return true;
			}

			value = default;
			return false;
		}


		internal static IPage OnGotoAction(IMenu menu, object[] o)
			=> new SearchPage(menu.Id, o);

		private SearchPage(int mId, object[] context) {
			_mId     = mId;
			_context = context;
			var handler = !T(context, 0, out string h)
				? Config.Load().Get<string>("search.last_handler")
				: h;
			var query = T(context, 1, out string q) ? q : null;
			var auto  = !T(context, 2, out bool a) || a;
			HandlerId  = handler;
			Query      = query ?? string.Empty;
			LastQuery  = (query ?? string.Empty) + " ";
			AutoSubmit = auto;
		}

		public object[] GetContext()
			=> _context;

		public IMenu GetMenu()
			=> Client.UiAPI.Get<IMenu>(_mId);

		private string HandlerId {
			get => _handlerId;
			set {
				_handlerId = value;
				var config = Config.Load();
				config.Set("search.last_handler", value);
				config.Save();
			}
		}

		private string _handlerId;
		internal string Query;
		internal string LastQuery;
		internal bool   AutoSubmit;
		internal readonly UnityEvent<WorkerTask> OnWorkerTaskUpdate = new();
		internal readonly UnityEvent<WorkerTask[]> OnWorkerTaskStart = new();
		internal readonly UnityEvent<IHandler[]> OnHandlerUpdate = new();

		internal bool IsEmptyQuery
			=> string.IsNullOrEmpty(Query);

		internal bool IsNewQuery
			=> Query != LastQuery;

		internal IHandler Handler {
			get {
				var handler = HandlerId != null
					? Main.Instance.Get(HandlerId)
					: null;
				handler ??= Main.Instance.Handlers.FirstOrDefault();
				return handler;
			}
			set {
				HandlerId = value?.GetId();
				LastQuery = string.Empty + " ";
			}
		}

		internal bool IsFetching
			=> _tasks.Count > 0
				&& _tasks.Any(t => t.Status is WorkerTaskStatus.Fetching or WorkerTaskStatus.Pending);

		private readonly List<WorkerTask> _tasks = new();

		private readonly List<CacheEntry> _cache = new();

		private sealed class CacheEntry {
			internal string        HandlerId;
			internal IFetchOptions Options;
			internal WorkerTask[]  Tasks;
		}

		private FetchOptions BuildOptions(string query)
			=> new() { Query = query, MenuId = _mId };

		private CacheEntry GetCache(IHandler handler, IFetchOptions options) {
			if (handler == null || options == null)
				return null;
			var handlerId = handler.GetId();
			return _cache.Find(
				e => e.HandlerId == handlerId
					&& SearchOptionsComparer.Compare(e.Options, options) == 0
			);
		}

		private void SetCache(IHandler handler, IFetchOptions options, WorkerTask[] tasks) {
			if (handler == null || options == null)
				return;
			var handlerId = handler.GetId();
			// Keep only the last search for each handler.
			_cache.RemoveAll(e => e.HandlerId == handlerId);
			_cache.Add(
				new CacheEntry {
					HandlerId = handlerId,
					Options   = options,
					Tasks     = tasks,
				}
			);
		}

		/// <summary>
		/// Restores the last cached results for the current handler and query, if any.
		/// Returns true when cached results were restored (no worker was fetched).
		/// </summary>
		internal bool RestoreCache() {
			var handler = Handler;
			var cached  = GetCache(handler, BuildOptions(Query));
			if (cached == null)
				return false;
			Cancel();
			_tasks.Clear();
			_tasks.AddRange(cached.Tasks);
			LastQuery = Query;
			OnWorkerTaskStart.Invoke(_tasks.ToArray());
			return true;
		}

		public GameObject GetContent(RectTransform parent) {
			if (_content)
				return _content;
			_content      = Client.GetAsset<GameObject>("ui:prefabs/split.prefab").Instantiate(parent);
			_content.name = $"[{GetStaticKey()}_{_content.GetEntityId().GetHashCode()}]";
			var splitContent   = Reference.GetComponent<RectTransform>("content", _content);
			var containerAsset = Client.GetAsset<GameObject>("ui:prefabs/container.prefab");
			var iconAsset      = Client.GetAsset<GameObject>("ui:prefabs/header_icon.prefab");
			var labelAsset     = Client.GetAsset<GameObject>("ui:prefabs/header_label.prefab");
			var scrollAsset    = Client.GetAsset<GameObject>("ui:prefabs/scroll.prefab");
			var infoAsset      = Client.GetAsset<GameObject>("ui:prefabs/infobox.prefab");
			var listAsset      = Client.GetAsset<GameObject>("ui:prefabs/list.prefab");

			// generate background containers

			// generate notification
			var container = containerAsset.Instantiate(splitContent);
			var withTitle = Client.GetAsset<GameObject>("ui:prefabs/with_title.prefab")
				.Instantiate(Reference.GetComponent<RectTransform>("content", container));
			var header = Reference.GetReference("header", withTitle);
			var icon   = iconAsset.Instantiate(Reference.GetComponent<RectTransform>("before", header));
			var label  = labelAsset.Instantiate(Reference.GetComponent<RectTransform>("content", header));

			Reference.GetComponent<Image>("image", icon).sprite = Client.GetAsset<Sprite>("ui:icons/search.png");
			Reference.GetComponent<TextLanguage>("text", label).UpdateText("search.title");

			var handlers = scrollAsset.Instantiate(Reference.GetComponent<RectTransform>("content", withTitle));
			var listsHandler = Reference.GetComponent<RectTransform>(
				"content",
				listAsset.Instantiate(Reference.GetComponent<RectTransform>("content", handlers))
			);
			var box = Client.GetAsset<GameObject>("ui:prefabs/tips.prefab").Instantiate(listsHandler);
			Reference.GetComponent<TextLanguage>("title", box).UpdateText("search.info.title");
			var handleInfo = infoAsset.Instantiate(Reference.GetComponent<RectTransform>("content", withTitle));

			// generate dashboard
			container = Client.GetAsset<GameObject>("ui:prefabs/container_full.prefab").Instantiate(splitContent);
			withTitle = Client.GetAsset<GameObject>("ui:prefabs/with_search.prefab")
				.Instantiate(Reference.GetComponent<RectTransform>("content", container));
			header = Reference.GetReference("header", withTitle);
			var content   = Reference.GetComponent<RectTransform>("content", withTitle);
			var component = _content.AddComponent<SearchComponent>();
			component.submitButton         = Reference.GetComponent<Button>("submit", header);
			component.inputField           = Reference.GetComponent<TMPro.TMP_InputField>("input", header);
			component.imageButton          = Reference.GetComponent<Image>("image", component.submitButton.gameObject);
			component.workersContainer     = scrollAsset.Instantiate(content);
			component.workerListContainer  = Reference.GetComponent<RectTransform>("content", component.workersContainer);
			component.resultContainer      = infoAsset.Instantiate(content);
			component.resultText           = Reference.GetComponent<TextLanguage>("text", component.resultContainer);
			component.inputImage           = Reference.GetComponent<Image>("image", component.inputField.gameObject);
			component.inputImageContainer  = Reference.GetReference("image_container", component.inputField.gameObject);
			component.infoContainer        = box;
			component.infoText             = Reference.GetComponent<TextLanguage>("text", component.infoContainer);
			component.infoHandlerContainer = handleInfo;
			component.infoHandlerText      = Reference.GetComponent<TextLanguage>("text", component.infoHandlerContainer);
			component.handlerContainer     = handlers;
			component.handlerListContainer = Reference.GetComponent<RectTransform>(
				"content", 
				listAsset.Instantiate(listsHandler)
			);
			component.Page = this;

			Reference.GetComponent<Image>("image", component.resultContainer)
				.sprite = Client.GetAsset<Sprite>("ui:icons/help.png");

			return _content;
		}

		internal void Cancel() {
			foreach (var cancel in _tasks)
				cancel.Cancel();
			_tasks.Clear();
		}

		async internal UniTask Submit() {
			if (IsFetching)
				return;

			var query = Query;
			LastQuery = query;

			var handler = Handler;
			if (handler == null) {
				Logger.LogDebug($"No handler found with id {HandlerId}");
				OnWorkerTaskStart.Invoke(Array.Empty<WorkerTask>());
				return;
			}

			var workers = handler.GetWorkers();
			if (workers.Length == 0) {
				Logger.LogDebug($"No workers found for handler {handler.GetId()}");
				OnWorkerTaskStart.Invoke(Array.Empty<WorkerTask>());
				return;
			}

			// Same handler and same search (query + pagination) already fetched:
			// reuse the cached results instead of running the workers again.
			var options = BuildOptions(query);
			var cached  = GetCache(handler, options);
			if (cached != null) {
				Cancel();
				_tasks.Clear();
				_tasks.AddRange(cached.Tasks);
				OnWorkerTaskStart.Invoke(_tasks.ToArray());
				return;
			}

			Cancel();

			foreach (var worker in workers.Where(w => w != null))
				_tasks.Add(
					new WorkerTask {
						Worker            = worker,
						Data              = BuildOptions(query),
						CancellationToken = new CancellationTokenSource(),
						Timeout           = 10d,
					}
				);

			var batch = _tasks.ToArray();
			OnWorkerTaskStart.Invoke(batch);
			await UniTask.WhenAll(batch.Select(t => t.Execute(this)));

			// Do not cache a batch that was canceled (page left, handler switched...).
			if (batch.Length > 0 && batch.All(t => t.Status != WorkerTaskStatus.Canceled))
				SetCache(handler, options, batch);
		}
	}
}