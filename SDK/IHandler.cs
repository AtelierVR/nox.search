using UnityEngine;

namespace Nox.Search {
	public interface IHandler {
		public string GetId();

		public string GetTitleKey();

		public string[] GetTitleArguments();

		public string GetPlaceholderKey();

		public string[] GetPlaceholderArguments();

		public Texture2D GetIcon();

		public string   GetDescriptionKey();
		
		public string[] GetDescriptionArguments();

		public IWorker[] GetWorkers();
	}
}