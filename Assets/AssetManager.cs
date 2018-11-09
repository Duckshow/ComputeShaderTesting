using UnityEngine;

public class AssetManager : Singleton<AssetManager> {

	public Texture2D SpriteSheetTiles;

	// should correspond to Tile.Type (tried automating, quite difficult)
	public TileAssetBlock None;
	public TileAssetBlock Corridor;
	public TileAssetBlock Greenhouse;

	public TileAssetBlock GetTileAssetBlockForRoomType(ShipGrid.Tile.RoomType roomType) {
		switch (roomType){
			case ShipGrid.Tile.RoomType.None:
				return None;
			case ShipGrid.Tile.RoomType.Corridor:
				return Corridor;
			case ShipGrid.Tile.RoomType.Greenhouse:
				return Greenhouse;
			default:
				Debug.LogError(roomType + " hasn't been properly implemented yet!");
				return null;
		}
	}

	public Vector2 GetSpriteSheetTilesSize() {
		return new Vector2(SpriteSheetTiles.width, SpriteSheetTiles.height);
	}

	public Int2 GetAssetForTile(ShipGrid.Tile tile) {
		bool hasTemporarySettings = tile.HasTemporarySettings();
		ShipGrid.Tile.RoomType roomType = tile.GetRoomType(shouldGetTemporary: hasTemporarySettings);
		Int2 posTileAssetBlock = tile.GetPosTileAssetBlock(shouldGetTemporary: hasTemporarySettings);
		TileAssetBlock tileAssetBlock = GetTileAssetBlockForRoomType(roomType);

		return tileAssetBlock.GetPosTexture(posTileAssetBlock, tile.GetBlockType(shouldGetTemporary: hasTemporarySettings));
	}
}