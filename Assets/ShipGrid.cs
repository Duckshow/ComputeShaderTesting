using UnityEngine;

public class ShipGrid : Singleton<ShipGrid> {
	public class Tile {
		public enum Type { None, Corridor }
		private Type type = Type.None;

		public void SetType(Type type) {
			this.type = type;
		}
	}

	public TileAssetBlock test;
	private Tile[,] grid;

	private Int2 size;
	private Int2 sizeHalf;
	public Int2 GetSize() {
		return size;
	}


	public static Vector2Int ConvertWorldToGrid(Vector2 posWorld) {
		Int2 size = GetInstance().size;
		Int2 sizeHalf = GetInstance().sizeHalf;
		float x = Mathf.Clamp(posWorld.x + sizeHalf.x, 0, size.x);
		float y = Mathf.Clamp(posWorld.y + sizeHalf.y, 0, size.y);
		return new Vector2Int((int)x, (int)y);
	}

	public override bool IsUsingAwakeDefault() { return true; }
	public override void AwakeDefault() {
		base.AwakeDefault();

		size = ShipMesh.GetInstance().GetWorldSize();
		sizeHalf = new Int2(size.x * 0.5f, size.y * 0.5f);
		grid = new Tile[size.x, size.y];
	}

	public void SetTileType(Int2 posGrid, Tile.Type type) {
		grid[posGrid.x, posGrid.y].SetType(type);
	}
}
