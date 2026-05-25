using System;
using Nox.CCK.Mods.Cores;
using Nox.CCK.Mods.Events;
using Nox.CCK.Mods.Initializers;
using Nox.CCK.Utils;
using Nox.Search.Runtime.Clients;
using Nox.UI;

namespace Nox.Search.Runtime {
	public class Client : IClientModInitializer {
		static internal IUiAPI UiAPI
			=> Main.Instance.CoreAPI.ModAPI
				.GetMod("ui")
				?.GetInstance<IUiAPI>();

		public static T GetAsset<T>(ResourceIdentifier path) where T : UnityEngine.Object
			=> Main.Instance.CoreAPI.AssetAPI.GetAsset<T>(path);

		private EventSubscription[]  _events;
		private IClientModCoreAPI    _api;

		public void OnInitializeClient(IClientModCoreAPI api) {
			_api    = api;
			Logger.Log("OnInitializeClient");
			_events = new[] {
				api.EventAPI.Subscribe("menu_goto", OnGoto)
			};
		}

		private static void OnGoto(EventData context) {
			if (!context.TryGet(0, out int mid)) return;
			if (!context.TryGet(1, out string key)) return;
			var menu = UiAPI?.Get<IMenu>(mid);
			if (menu == null) return;
			IPage page = null;
			if (SearchPage.GetStaticKey() == key)
				page = SearchPage.OnGotoAction(menu, context.Data[2..]);
			if (page == null) return;
			Main.Instance.CoreAPI.EventAPI.Emit("menu_display", menu.Id, page);
		}

		public void OnDisposeClient() {
			foreach (var subscription in _events)
				_api.EventAPI.Unsubscribe(subscription);
			_events = Array.Empty<EventSubscription>();
			_api    = null;
		}
	}
}