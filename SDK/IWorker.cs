using System;
using Cysharp.Threading.Tasks;

namespace Nox.Search {
	/// <summary>
	/// Represents a worker that performs search operations.
	/// </summary>
	public interface IWorker {
		/// <summary>
		/// The unique key identifying the worker.
		/// </summary>
		public string TitleKey
			=> "value";

		/// <summary>
		/// The arguments used for the title formatting.
		/// </summary>
		public string[] TitleArguments
			=> Array.Empty<string>();

		/// <summary>
		/// The ratio of result cards.
		/// </summary>
		public float Ratio
			=> 1f;

		/// <summary>
		/// Fetches results based on the provided options.
		/// </summary>
		/// <param name="options"></param>
		/// <returns></returns>
		public UniTask<IResult> Fetch(IFetchOptions options);

	}
}