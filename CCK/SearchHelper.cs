using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Nox.CCK.Utils;

namespace Nox.CCK.Search {
	/// <summary>
	/// Centralized helper for iterating server configurations from "servers" in Config.
	/// Each search handler can use ServersBy(feature) to filter and create its own IWorker instances.
	/// </summary>
	public static class SearchHelper {
		/// <summary>
		/// Enumerates servers that have "search": true and the given feature in their "features" array.
		/// </summary>
		public static IEnumerable<ServerInfo> ServersBy(string feature) {
			var servers = Config.Load().Get("servers");
			if (servers == null) yield break;
			var current = Config.Load().Get("server")?.ToString();
			var dict = servers.ToObject<Dictionary<string, JObject>>();
			foreach (var kv in dict) {
				var addr     = kv.Key;
				var value    = kv.Value;
				var isCurrent = current != null && addr == current;
				var search   = isCurrent || (value["search"]?.ToObject<bool>() ?? false);
				var features = value["features"]?.Values<string>().ToArray() ?? Array.Empty<string>();
				if (!(search && Array.Exists(features, f => f == feature))) continue;
				yield return new ServerInfo {
					Address = addr,
					Title   = value["title"]?.ToString()
				};
			}
		}
	}

	/// <summary>
	/// Lightweight info about a server entry in config.
	/// </summary>
	public struct ServerInfo {
		public string Address;
		public string Title;
	}
}
