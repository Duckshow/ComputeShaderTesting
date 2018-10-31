using UnityEngine;

public class AssetManager : Singleton<AssetManager> {

	// should correspond to Tile.Type (tried automating, quite difficult)
	public TileAssetBlock None;
	public TileAssetBlock Corridor;

	public TileAssetBlock GetAssetForTile(ShipGrid.Tile tile) {
		TileAssetBlock tileAssetBlock;
		switch (tile.GetTileType()){
			case ShipGrid.Tile.Type.None:
				tileAssetBlock = None;
			case ShipGrid.Tile.Type.Corridor:
				tileAssetBlock = Corridor;
			default:
				Debug.LogError(type + " hasn't been properly implemented yet!");
				return null;
		}

		
	}
}
