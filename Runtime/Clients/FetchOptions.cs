namespace Nox.Search.Runtime.Clients {
	public class FetchOptions : IFetchOptions {
		public string Query { get; set; }
		public uint Page { get; set; }
		public uint Limit { get; set; }

		public int MenuId = 0;

		public int CompareTo(IFetchOptions other)
			=> SearchOptionsComparer.Compare(this, other);
	}
}