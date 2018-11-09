using UnityEngine;

public class ShipGrid : Singleton<ShipGrid> {
	public class Tile {
		public enum RoomType { None, Corridor, Greenhouse }
		private RoomType typeRoom = RoomType.None;
		private RoomType typeRoomTemporary = RoomType.None;

		private TileAssetBlock.BlockType typeBlock = TileAssetBlock.BlockType.None;
		private TileAssetBlock.BlockType typeBlockTemporary = TileAssetBlock.BlockType.None;

		private Int2 posGrid;
		private Int2 posTileAssetBlock;
		private Int2 posTileAssetBlockTemporary;


		public Tile(Int2 posGrid) {
			this.posGrid = posGrid;
		}

		public void Init() {
			Int2 zero = Int2.zero;
			SetRoomType(RoomType.None, TileAssetBlock.BlockType.None, zero, zero, shouldSetTemporary: false);
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
				Int2 posBlock = posGridTile - posGridBlockBottomLeft;
				Int2 posBlockMax = posGridBlockTopRight - posGridBlockBottomLeft;
				Int2 newPosTileAssetBlock = Int2.zero;
				TileAssetBlock.BlockType blockType = GetBlockType(shouldSetTemporary);
				switch (blockType){
					case TileAssetBlock.BlockType.None:
						break;
					case TileAssetBlock.BlockType.Block:
						// x
						if (posBlock.x == posBlockMax.x){
							newPosTileAssetBlock.x = 2;
						}
						else if (posBlock.x > 0){
							newPosTileAssetBlock.x = 1;
						}
						else{
							newPosTileAssetBlock.x = 0;
						}

						// y
						if (posBlock.y == posBlockMax.y && tileAssetBlock.HasValueInBlock(newPosTileAssetBlock.x, 4)){
							newPosTileAssetBlock.y = 2;
						}
						else if (posBlock.y == posBlockMax.y - 1 && tileAssetBlock.HasValueInBlock(newPosTileAssetBlock.x, 3)){
							newPosTileAssetBlock.y = 1;
						}
						else if(posBlock.y == 0 && tileAssetBlock.HasValueInBlock(newPosTileAssetBlock.x, 0)){
							newPosTileAssetBlock.y = -2;
						}
						else if (posBlock.y == 1 && tileAssetBlock.HasValueInBlock(newPosTileAssetBlock.x, 1)){
							newPosTileAssetBlock.y = -1; aeahrea // things are upside down! figure it out!
						}
						else{
							newPosTileAssetBlock.y = 0;
						}
						break;
					case TileAssetBlock.BlockType.Line:
						// x
						if (posBlock.x == posBlockMax.x){
							newPosTileAssetBlock.x = 2;
						}
						else if (posBlock.x > 0){
							newPosTileAssetBlock.x = 1;
						}
						else{
							newPosTileAssetBlock.x = 0;
						}

						// y
						newPosTileAssetBlock.y = 0;
						break;
					case TileAssetBlock.BlockType.Single:
						newPosTileAssetBlock.x = 0;
						newPosTileAssetBlock.y = 0;
						break;
					default:
						Debug.LogError(blockType + " hasn't been properly implemented yet!");
						break;
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
			return shouldGetTemporary ? typeRoomTemporary : typeRoom;
		}

		public TileAssetBlock.BlockType GetBlockType(bool shouldGetTemporary) {
			return shouldGetTemporary ? typeBlockTemporary : typeBlock;
		}

		public bool HasTemporarySettings() {
			return typeRoomTemporary != RoomType.None;
		}

		public void SetRoomType(RoomType typeRoom, TileAssetBlock.BlockType typeBlock, Int2 posGridBlockBottomLeft, Int2 posGridBlockTopRight, bool shouldSetTemporary) {
			if (shouldSetTemporary){ 
				this.typeRoomTemporary = typeRoom;
				this.typeBlockTemporary = typeBlock;
			}
			else { 
				this.typeRoom = typeRoom; 
				this.typeBlock = typeBlock;
			}


			TileAssetBlock tileAssetBlock = AssetManager.GetInstance().GetTileAssetBlockForRoomType(typeRoom);
			SetPosTileAssetBlock(posGridBlockBottomLeft, posGridBlockTopRight, posGrid, tileAssetBlock, shouldSetTemporary);
			ShipMesh.GetInstance().UpdateTileAsset(posGrid);
		}

		public void ClearRoomTypeTemporary(){
			typeRoomTemporary = RoomType.None;
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
