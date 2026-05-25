using Nox.Search;

namespace api.nox.search.client {
	public class FetchOptions : IFetchOptions {
		public string Query { get; set; }
		public uint Page { get; set; }
		public uint Limit { get; set; }

		public int MenuId = 0;
	}
}