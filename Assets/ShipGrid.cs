using UnityEngine;

public class ShipGrid : Singleton<ShipGrid> {
	public class Tile {
		public enum Type { None, Corridor }
		private Type type = Type.None;

		private Int2 posGrid;
		private Int2 posTileAssetBlock;


		public Tile(Int2 posGrid) {
			this.posGrid = posGrid;
			SetTileType(Type.None);
		}

		public void SetPosTileAssetBlock(Int2 posTileAssetBlock) {
			this.posTileAssetBlock = posTileAssetBlock;
		}

		public Type GetTileType() {
			return type;
		}

		public void SetTileType(Type type) {
			this.type = type;
			ShipMesh.GetInstance().UpdateTileAsset(posGrid);
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

		size = ShipMesh.GetInstance().GetSizeWorld();
		sizeHalf = new Int2(size.x * 0.5f, size.y * 0.5f);

		grid = new Tile[size.x, size.y];
		for (int y = 0; y < size.y; y++){
			for (int x = 0; x < size.x; x++){
				grid[x, y] = new Tile(new Int2(x, y));
			}
		}
	}

	public Tile.Type GetTileType(Int2 posGrid){
		return grid[posGrid.x, posGrid.y].GetTileType();
	}

	public void SetTileType(Int2 posGrid, Tile.Type type) {
		grid[posGrid.x, posGrid.y].SetTileType(type);
	}
}
