namespace Nox.Search {
	/// <summary>
	/// Interface representing options for fetching search results.
	/// </summary>
	public interface IFetchOptions {
		/// <summary>
		/// The search query string.
		/// This can include keywords, phrases, or specific terms to filter the search results.
		/// </summary>
		public string Query { get; }

		/// <summary>
		/// Page number for paginated results.
		/// Zero-based index indicating which page of results to fetch.
		/// </summary>
		public uint Page { get; }

		/// <summary>
		/// Number of results to return per page.
		/// Defines the maximum number of items to include in the result set.
		/// </summary>
		public uint Limit { get; }
	}
}