using UnityEngine;
using UnityEngine.UI;

namespace Nox.Search.Runtime.Clients {
	[RequireComponent(typeof(GridLayoutGroup))]
	public class GridFitter : MonoBehaviour {
		public float ratio = 1.0f;
		public float minWidth = 150f;

		private void Awake()
			=> UpdateLayout();
		public void OnValidate()
			=> UpdateLayout();

		public void UpdateLayout() {
			var gridLayoutGroup = GetComponent<GridLayoutGroup>();
			var rectTransform   = GetComponent<RectTransform>();

			var width = rectTransform.rect.width;

			var cellWidth = width > minWidth ? width : minWidth;
			var nbCells   = 1;
			while (cellWidth > minWidth) {
				// get the width of the cell based on the number of columns, spacing and padding
				cellWidth = (width - gridLayoutGroup.padding.left - gridLayoutGroup.padding.right -
					(gridLayoutGroup.spacing.x * (nbCells - 1))) / nbCells;
				if (cellWidth < minWidth)
					break;
				nbCells++;
			}

			// if the cell width is too small, we need to reduce the number of columns
			if (cellWidth < minWidth && nbCells > 1) {
				// reduce the number of columns
				nbCells--;
				cellWidth = (width - gridLayoutGroup.padding.left - gridLayoutGroup.padding.right -
					(gridLayoutGroup.spacing.x * (nbCells - 1))) / nbCells;
			}

			var cellSize = new Vector2(cellWidth, cellWidth / ratio);
			gridLayoutGroup.cellSize = cellSize;
		}
	}
}