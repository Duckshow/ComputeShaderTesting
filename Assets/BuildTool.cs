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
			ClearOldPreview();
			UpdatePreview();
		}
		else if (Mouse.GetInstance().GetStateLMB() == Mouse.StateEnum.Release){
			ClearOldPreview();
		}
	}

	void ClearOldPreview() {
		for (int i = 0; i < affectedTileCount; i++){
			Int2 tilePosGrid = affectedTiles[i];
			ShipGrid.Tile tile = ShipGrid.GetInstance().GetTile(tilePosGrid);
			tile.ClearRoomTypeTemporary();
		}
	}

	void UpdatePreview() {
		Int2 posGridRoomBottomLeft = new Int2(Mathf.Min(posGridStart.x, posGridEnd.x), Mathf.Min(posGridStart.y, posGridEnd.y));
		Int2 posGridRoomTopRight = new Int2(Mathf.Max(posGridStart.x, posGridEnd.x), Mathf.Max(posGridStart.y, posGridEnd.y));
		
		int coveredTileCountX = (posGridRoomTopRight.x - posGridRoomBottomLeft.x) + 1;
		int coveredTileCountY = (posGridRoomTopRight.y - posGridRoomBottomLeft.y) + 1;

		if (!AssetManager.GetInstance().GetTileAssetBlockForRoomType(roomTypeCurrent).HasValueInBlock()) {
			coveredTileCountY = 1;
		}

		if (coveredTileCountY > 1){
			if(posGridEnd.y > posGridStart.y) posGridRoomBottomLeft.y -= (coveredTileCountY - 1);
			else posGridRoomTopRight.y += (coveredTileCountY - 1);
			coveredTileCountY = coveredTileCountY * 2 - 1;
		}

		int posGridRoomBottomClamping = Mathf.Max(0, posGridRoomBottomLeft.y) - posGridRoomBottomLeft.y;
		posGridRoomBottomLeft.y += posGridRoomBottomClamping;
		posGridRoomTopRight.y -= posGridRoomBottomClamping;
		coveredTileCountY -= posGridRoomBottomClamping * 2;

		int posGridRoomTopClamping = posGridRoomTopRight.y - Mathf.Min(posGridRoomTopRight.y, ShipGrid.GetInstance().GetSize().y - 1);
		posGridRoomBottomLeft.y += posGridRoomTopClamping;
		posGridRoomTopRight.y -= posGridRoomTopClamping;
		coveredTileCountY -= posGridRoomTopClamping * 2;

		affectedTileCount = coveredTileCountX * coveredTileCountY;
		if (affectedTiles == null || affectedTiles.Length < affectedTileCount){
			affectedTiles = new Int2[affectedTileCount];
		}

		for (int x = 0; x < coveredTileCountX; x++){
			for (int y = 0; y < coveredTileCountY; y++){
				// Debug.Log(x + ", " + y + " - " + coveredTileCountX + "/" + coveredTileCountY);
				Int2 tilePosGrid = posGridRoomBottomLeft + new Int2(x, y);
				ShipGrid.Tile tile = ShipGrid.GetInstance().GetTile(tilePosGrid);
				tile.SetRoomType(roomTypeCurrent, posGridRoomBottomLeft, posGridRoomTopRight, shouldSetTemporary: true);
				affectedTiles[y * coveredTileCountX + x] = tilePosGrid;
			}
		}
	}
}
