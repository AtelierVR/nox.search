namespace Nox.Search {
	/// <summary>
	/// Interface representing the worker result of a search operation.
	/// </summary>
	public interface IResult {
		/// <summary>
		/// Indicates whether the result represents an error.
		/// </summary>
		public bool IsError { get; }

		/// <summary>
		/// Provides the error message if the result is an error; otherwise, it is null.
		/// </summary>
		public string Error { get; }

		/// <summary>
		/// Indicates whether there is a next set of results available.
		/// </summary>
		/// <returns></returns>
		public bool HasNext();

		/// <summary>
		/// Results data returned from the search operation.
		/// </summary>
		public IResultData[] Data { get; }
	}
}