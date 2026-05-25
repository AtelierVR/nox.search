using System.Collections.Generic;
using System.Linq;
using Nox.CCK.Language;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.Search;
using UnityEngine.Events;

namespace api.nox.search {
	public class Main : IMainModInitializer, ISearchAPI {
		internal readonly List<IHandler> Handlers = new();
		internal static   Main           Instance;
		internal          IModCoreAPI    CoreAPI;
		private           LanguagePack   _languagePack;

		internal static readonly UnityEvent<IHandler> OnHandlerAdded   = new();
		internal static readonly UnityEvent<IHandler> OnHandlerRemoved = new();

		private void InvokeHandlerAdded(IHandler handler) {
			OnHandlerAdded.Invoke(handler);
			CoreAPI.EventAPI.Emit("search_handler_added", handler);
		}

		private void InvokeHandlerRemoved(IHandler handler) {
			OnHandlerRemoved.Invoke(handler);
			CoreAPI.EventAPI.Emit("search_handler_removed", handler);
		}

		public IHandler Add(IHandler handler) {
			if (handler == null) {
				Logger.LogError("Cannot register a null search handler");
				return null;
			}

			if (string.IsNullOrWhiteSpace(handler.GetId())) {
				Logger.LogError("Cannot register a search handler with an empty id");
				return null;
			}

			if (Handlers.Exists(b => b.GetId() == handler.GetId())) {
				Logger.LogError($"Search handler with id {handler.GetId()} already exists");
				return null;
			}

			Handlers.Add(handler);
			InvokeHandlerAdded(handler);
			return handler;
		}

		public void Remove(string id) {
			var handler = Get(id);
			if (handler == null) {
				Logger.LogError($"Search handler with id {id} does not exist");
				return;
			}

			Handlers.Remove(handler);
			InvokeHandlerRemoved(handler);
		}

		public IHandler Get(string id)
			=> Handlers.Find(b => b.GetId() == id);

		public bool Has(string id)
			=> Handlers.Exists(b => b.GetId() == id);


		public void OnInitializeMain(IMainModCoreAPI api) {
			CoreAPI       = api;
			Instance      = this;
			_languagePack = api.AssetAPI.GetAsset<LanguagePack>("lang.asset");
			LanguageManager.AddPack(_languagePack);
			Logger.Log("Search API initialized");
		}

		public void OnDisposeMain() {
			if (_languagePack) {
				LanguageManager.RemovePack(_languagePack);
				_languagePack = null;
			}

			foreach (var handler in Handlers)
				InvokeHandlerRemoved(handler);
			Handlers.Clear();

			CoreAPI  = null;
			Instance = null;
		}
	}
}