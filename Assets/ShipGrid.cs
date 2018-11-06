using UnityEngine;

public class ShipGrid : Singleton<ShipGrid> {
	public class Tile {
		public enum RoomType { None, Corridor, Greenhouse }
		private RoomType roomType = RoomType.None;
		private RoomType roomTypeTemporary = RoomType.None;

		private Int2 posGrid;
		private Int2 posTileAssetBlock;
		private Int2 posTileAssetBlockTemporary;


		public Tile(Int2 posGrid) {
			this.posGrid = posGrid;
		}

		public void Init() {
			Int2 zero = Int2.zero;
			SetRoomType(RoomType.None, zero, zero, shouldSetTemporary: false);
		}

		public Int2 GetPosTileAssetBlock(bool shouldGetTemporary){
			return shouldGetTemporary ? posTileAssetBlockTemporary : posTileAssetBlock;
		}

		void SetPosTileAssetBlock(Int2 posGridBlockBottomLeft, Int2 posGridBlockTopRight, Int2 posGridTile, TileAssetBlock tileAssetBlock, bool shouldSetTemporary) {
			if (posGridBlockTopRight.x == 0 && posGridBlockTopRight.y == 0){
				if (shouldSetTemporary){
					posTileAssetBlockTemporary = Int2.zero;
				}
				else{
					posTileAssetBlock = Int2.zero;
				}
			}
			else{
				Int2 newPosTileAssetBlock = posGridTile - posGridBlockBottomLeft;
				Int2 posTileAssetBlockMax = posGridBlockTopRight - posGridBlockBottomLeft;

				if (newPosTileAssetBlock.x == posTileAssetBlockMax.x) {
					newPosTileAssetBlock.x = 2;
				}
				else if (newPosTileAssetBlock.x > 0){
					newPosTileAssetBlock.x = 1;
				}
				else{
					newPosTileAssetBlock.x = 0;
				}

				if (newPosTileAssetBlock.y == posTileAssetBlockMax.y && tileAssetBlock.HasValueAtPos(newPosTileAssetBlock.x, 4)){
					newPosTileAssetBlock.y = 4;
				}
				else if (newPosTileAssetBlock.y == posTileAssetBlockMax.y - 1 && tileAssetBlock.HasValueAtPos(newPosTileAssetBlock.x, 3)){
					newPosTileAssetBlock.y = 3;
				}
				else if(newPosTileAssetBlock.y == 0 && tileAssetBlock.HasValueAtPos(newPosTileAssetBlock.x, 0)){
					newPosTileAssetBlock.y = 0;
				}
				else if (newPosTileAssetBlock.y == 1 && tileAssetBlock.HasValueAtPos(newPosTileAssetBlock.x, 1)){
					newPosTileAssetBlock.y = 1;
				}
				else{
					newPosTileAssetBlock.y = 2;
				}

				if (shouldSetTemporary){
					posTileAssetBlockTemporary = newPosTileAssetBlock;
				}
				else{
					posTileAssetBlock = newPosTileAssetBlock;
				}
			}
		}

		public RoomType GetRoomType(bool shouldGetTemporary) {
			return shouldGetTemporary ? roomTypeTemporary : roomType;
		}

		public bool HasTemporarySettings() {
			return roomTypeTemporary != RoomType.None;
		}

		public void SetRoomType(RoomType roomType, Int2 posGridRoomStart, Int2 posGridRoomEnd, bool shouldSetTemporary) {
			if (shouldSetTemporary){ 
				this.roomTypeTemporary = roomType; 
			}
			else { 
				this.roomType = roomType; 
			}

			TileAssetBlock tileAssetBlock = AssetManager.GetInstance().GetTileAssetBlockForRoomType(roomType);
			SetPosTileAssetBlock(posGridRoomStart, posGridRoomEnd, posGrid, tileAssetBlock, shouldSetTemporary);
			ShipMesh.GetInstance().UpdateTileAsset(posGrid);
		}

		public void ClearRoomTypeTemporary(){
			roomTypeTemporary = RoomType.None;
			posTileAssetBlockTemporary = new Int2(0, 0);
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


	public static Int2 ConvertWorldToGrid(Vector2 posWorld) {
		Int2 size = GetInstance().size;
		Int2 sizeHalf = GetInstance().sizeHalf;
		float x = Mathf.Clamp(posWorld.x + sizeHalf.x, 0, size.x - 1);
		float y = Mathf.Clamp(posWorld.y + sizeHalf.y, 0, size.y - 1);
		return new Int2((int)x, (int)y);
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
				grid[x, y].Init();
			}
		}
	}

	public Tile GetTile(Int2 posGrid) {
		return grid[posGrid.x, posGrid.y];
	}
}
