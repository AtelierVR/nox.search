namespace Nox.Search.Runtime.Clients {
	/// <summary>
	/// Ordering / equality helper for <see cref="IFetchOptions"/>.
	/// Two requests are identical when <see cref="Compare"/> returns 0, which can be
	/// used as a cache key to avoid fetching the same search twice.
	/// </summary>
	public static class SearchOptionsComparer {
		/// <summary>
		/// Compares two search requests by query, then page, then limit.
		/// </summary>
		public static int Compare(IFetchOptions a, IFetchOptions b) {
			if (ReferenceEquals(a, b)) return 0;
			if (a is null) return -1;
			if (b is null) return 1;

			var compare = string.CompareOrdinal(a.Query, b.Query);
			if (compare != 0) return compare;

			compare = a.Page.CompareTo(b.Page);
			if (compare != 0) return compare;

			return a.Limit.CompareTo(b.Limit);
		}
	}
}
