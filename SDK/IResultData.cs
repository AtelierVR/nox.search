using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Nox.Search {
	/// <summary>
	/// Represents a single result item in a search operation.
	/// </summary>
	public interface IResultData {
		/// <summary>
		/// The unique identifier of the result item.
		/// Usually the HashCode of the item.
		/// </summary>
		public int Id { get; }
		
		/// <summary>
		/// The image associated with the result item.
		/// This could be a thumbnail or icon representing the item.
		/// </summary>
		public UniTask<Texture2D> Image { get; }

		/// <summary>
		/// The localization key for the title of the result item.
		/// </summary>
		public string TitleKey
			=> "value";

		/// <summary>
		/// The arguments for the title localization key.
		/// Used for formatting the title string.
		/// </summary>
		public string[] TitleArguments
			=> System.Array.Empty<string>();

		/// <summary>
		/// Action to perform when the result item is clicked.
		/// </summary>
		/// <param name="menuId"></param>
		public void OnClick(int menuId);
	}
}