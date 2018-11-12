using UnityEngine;

public class BuildTool : Singleton<BuildTool> {

	private Mouse instanceMouse;

	private Int2 posGridStart;
	private Int2 posGridEnd;

	private int affectedTileCount = 0;
	private Int2[] affectedTiles;

	private ShipGrid.Tile.RoomType roomTypeCurrent = ShipGrid.Tile.RoomType.Corridor;


	public override bool IsUsingAwakeDefault() { return true; }
	public override void AwakeDefault() {
		instanceMouse = Mouse.GetInstance();
	}

	public override bool IsUsingUpdateDefault() { return true; }
	public override void UpdateDefault() { 
		if (Mouse.GetInstance().GetStateLMB() == Mouse.StateEnum.Click){
			posGridStart = instanceMouse.GetPosGrid();
		}
		else if (Mouse.GetInstance().GetStateLMB() == Mouse.StateEnum.Hold){
			roomTypeCurrent = Input.GetKey(KeyCode.Space) ? ShipGrid.Tile.RoomType.Greenhouse : ShipGrid.Tile.RoomType.Corridor;

			posGridEnd = instanceMouse.GetPosGrid();
			ClearDraggedOutTiles();
			DrawDraggedOutTiles(isTemporary: true);
		}
		else if (Mouse.GetInstance().GetStateLMB() == Mouse.StateEnum.Release){
			ClearDraggedOutTiles();
			DrawDraggedOutTiles(isTemporary: false);
		}
	}

	void ClearDraggedOutTiles() {
		for (int i = 0; i < affectedTileCount; i++){
			Int2 tilePosGrid = affectedTiles[i];
			ShipGrid.Tile tile = ShipGrid.GetInstance().GetTile(tilePosGrid);
			tile.ClearRoomTypeTemporary();
		}
	}

	void DrawDraggedOutTiles(bool isTemporary) {
		Int2 posGridRoomStart = posGridStart;
		Int2 posGridRoomEnd = posGridEnd;

		TileAssetBlock.BlockType typeBlock = TileAssetBlock.BlockType.None;
		TileAssetBlock tileAssetBlock = AssetManager.GetInstance().GetTileAssetBlockForRoomType(roomTypeCurrent);
		if (posGridEnd.y - posGridStart.y != 0 && tileAssetBlock.HasAnyValueInBlock()) {
			typeBlock = TileAssetBlock.BlockType.Block;
		}
		else if (posGridEnd.x - posGridStart.x != 0 && tileAssetBlock.HasAnyValueInLine()) { 
			typeBlock = TileAssetBlock.BlockType.Line;
			posGridRoomEnd.y = posGridRoomStart.y;
		}
		else if(tileAssetBlock.HasAnyValueInSingle()){
			typeBlock = TileAssetBlock.BlockType.Single;
			posGridRoomEnd = posGridRoomStart;
		}
		else{
			Debug.LogError(roomTypeCurrent + "'s TileAssetBlock doesn't contain any data!");
		}

		Int2 posGridRoomBottomLeft = new Int2(Mathf.Min(posGridRoomStart.x, posGridRoomEnd.x), Mathf.Min(posGridRoomStart.y, posGridRoomEnd.y));
		Int2 posGridRoomTopRight = new Int2(Mathf.Max(posGridRoomStart.x, posGridRoomEnd.x), Mathf.Max(posGridRoomStart.y, posGridRoomEnd.y));

		Int2 coveredTileCount = new Int2();
		coveredTileCount.x = (posGridRoomTopRight.x - posGridRoomBottomLeft.x) + 1;
		coveredTileCount.y = (posGridRoomTopRight.y - posGridRoomBottomLeft.y) + 1;

		if (coveredTileCount.y > 1){
			if(posGridEnd.y > posGridStart.y) posGridRoomBottomLeft.y -= (coveredTileCount.y - 1);
			else posGridRoomTopRight.y += (coveredTileCount.y - 1);
			coveredTileCount.y = coveredTileCount.y * 2 - 1;
		}

		int posGridRoomBottomClamping = Mathf.Max(0, posGridRoomBottomLeft.y) - posGridRoomBottomLeft.y;
		posGridRoomBottomLeft.y += posGridRoomBottomClamping;
		posGridRoomTopRight.y -= posGridRoomBottomClamping;
		coveredTileCount.y -= posGridRoomBottomClamping * 2;

		int posGridRoomTopClamping = posGridRoomTopRight.y - Mathf.Min(posGridRoomTopRight.y, ShipGrid.GetInstance().GetSize().y - 1);
		posGridRoomBottomLeft.y += posGridRoomTopClamping;
		posGridRoomTopRight.y -= posGridRoomTopClamping;
		coveredTileCount.y -= posGridRoomTopClamping * 2;

		affectedTileCount = coveredTileCount.x * coveredTileCount.y;
		if (affectedTiles == null || affectedTiles.Length < affectedTileCount){
			affectedTiles = new Int2[affectedTileCount];
		}

		int roomID = ShipGrid.Tile.GetUniqueRoomID(posGridRoomBottomLeft);
		Int2 maxViableSize = coveredTileCount;

		bool isDone = false;
		for (int x = 0; x < coveredTileCount.x; x++){
			if(isDone) break;

			for (int y = 0; y < coveredTileCount.y; y++){
				Int2 tilePosGrid = posGridRoomBottomLeft + new Int2(x, y);
				ShipGrid.Tile tile = ShipGrid.GetInstance().GetTile(tilePosGrid);

				bool isRoom = tile.GetIsRoom();
				if (posGridRoomBottomLeft.y + y == posGridRoomStart.y){
					if (isRoom){
						int distanceToRoomCenterX = Mathf.Abs((posGridRoomBottomLeft.x + x) - posGridRoomStart.x);
						maxViableSize.x = Mathf.Min(maxViableSize.x, distanceToRoomCenterX);
						isDone = true;
						break;
					}
				}
				else{
					if (isRoom){
						int distanceToRoomCenterY = Mathf.Abs((posGridRoomBottomLeft.y + y) - posGridRoomStart.y) - 1;
						maxViableSize.y = Mathf.Min(maxViableSize.y, distanceToRoomCenterY * 2 + 1);
					}
				}

			}
		}
		Debug.Log(maxViableSize);
		coveredTileCount = maxViableSize;

		for (int x = 0; x < coveredTileCount.x; x++){
			// if(isDone) break;

			for (int y = 0; y < coveredTileCount.y; y++){
				// Debug.Log(x + ", " + y + " - " + coveredTileCountX + "/" + coveredTileCountY);
				Int2 tilePosGrid = posGridRoomBottomLeft + new Int2(x, y);
				ShipGrid.Tile tile = ShipGrid.GetInstance().GetTile(tilePosGrid);
				// if (x == 0 && y == 0 && tile.RoomID >= 0){
				// 	isDone = true;
				// 	break;
				// 	// roomID = tile.RoomID;
				// }

				tile.CreateRoom(roomTypeCurrent, roomID, typeBlock, posGridRoomBottomLeft, posGridRoomTopRight, isTemporary);
				affectedTiles[y * coveredTileCount.x + x] = tilePosGrid;
			}
		}
	}
}
