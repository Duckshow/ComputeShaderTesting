using UnityEngine;

public class BuildTool : Singleton<BuildTool> {

	private Mouse instanceMouse;

	private Int2 posGridStart;
	private Int2 posGridEnd;
	private Int2 posGridEndOld;

	private int affectedTileCount = 0;
	private Int2[] affectedTiles;

	private Int2 lastPosGridRoomBottomLeft = Int2.zero;
	private Int2 lastPosGridRoomTopRight = Int2.zero;
	private Int2 lastCoveredTileCount = Int2.zero;

	private ShipGrid.Tile.RoomType roomTypeCurrent = ShipGrid.Tile.RoomType.Corridor;

	private Direction rotation = Direction.None;


	public override bool IsUsingAwakeDefault() { return true; }
	public override void AwakeDefault() {
		instanceMouse = Mouse.GetInstance();
	}

	public override bool IsUsingUpdateDefault() { return true; }
	public override void UpdateDefault() {
		rotation = GetCurrentRotation();

		if (Mouse.GetInstance().GetStateLMB() == Mouse.StateEnum.Click){
			posGridStart = instanceMouse.GetPosGrid();
		}
		else if (Mouse.GetInstance().GetStateLMB() == Mouse.StateEnum.Hold){
			roomTypeCurrent = Input.GetKey(KeyCode.Space) ? ShipGrid.Tile.RoomType.Greenhouse : ShipGrid.Tile.RoomType.Corridor;

			posGridEndOld = posGridEnd;
			posGridEnd = instanceMouse.GetPosGrid();
			if (posGridEnd != posGridEndOld){
				ClearDraggedOutTiles();
				DrawDraggedOutTiles(isTemporary: true);
			}
		}
		else if (Mouse.GetInstance().GetStateLMB() == Mouse.StateEnum.Release){
			ClearDraggedOutTiles();
			DrawDraggedOutTiles(isTemporary: false);
		}
	}

	Direction GetCurrentRotation() { 
		int rotationIndex = (int)rotation;
		int directionLength = System.Enum.GetValues(typeof(Direction)).Length;
		if (Input.GetKeyDown(KeyCode.E)){ // TODO: key-input should be handled by its own class
			rotationIndex++;
			if (rotationIndex >= directionLength) {
				rotationIndex = 1;
			}
		}
		if (Input.GetKeyDown(KeyCode.Q)){ // TODO: key-input should be handled by its own class
			rotationIndex--;
			if (rotationIndex == 0){
				rotationIndex = directionLength - 1;
			}
		}
		return (Direction)rotationIndex;
	}

	void ClearDraggedOutTiles() {
		for (int i = 0; i < affectedTileCount; i++){
			Int2 tilePosGrid = affectedTiles[i];
			ShipGrid.Tile tile = ShipGrid.GetInstance().GetTile(tilePosGrid);
			tile.ClearRoomTypeTemporary();
		}
	}

	void DrawDraggedOutTiles(bool isTemporary) {
		if (ShipGrid.GetInstance().GetTile(posGridStart).GetIsRoom()){
			return;
		}

		Int2 posGridRoomStart = posGridStart;
		Int2 posGridRoomEnd = posGridEnd;

		TileAssetBlock.BlockType typeBlock = TileAssetBlock.BlockType.None;
		TileAssetBlock tileAssetBlock = AssetManager.GetInstance().GetTileAssetBlockForRoomType(roomTypeCurrent);

		// Block
		if (posGridEnd.y - posGridStart.y != 0 && tileAssetBlock.HasAnyValueInBlock()) {
			rotation = Direction.None;
			typeBlock = TileAssetBlock.BlockType.Block;
		}

		// Line
		else if (posGridEnd.x - posGridStart.x != 0 && tileAssetBlock.HasAnyValueInLine()) {
			rotation = Direction.None;
			typeBlock = TileAssetBlock.BlockType.Line;
			posGridRoomEnd.y = posGridRoomStart.y;
		}

		// Single
		else if(tileAssetBlock.HasAnyValueInSingle()){
			typeBlock = TileAssetBlock.BlockType.Single;
			posGridRoomEnd = posGridRoomStart;
		}

		// Fail
		else{
			Debug.LogError(roomTypeCurrent + "'s TileAssetBlock doesn't contain any data!");
		}

		Int2 newPosGridRoomBottomLeft = new Int2(Mathf.Min(posGridRoomStart.x, posGridRoomEnd.x), Mathf.Min(posGridRoomStart.y, posGridRoomEnd.y));
		Int2 newPosGridRoomTopRight = new Int2(Mathf.Max(posGridRoomStart.x, posGridRoomEnd.x), Mathf.Max(posGridRoomStart.y, posGridRoomEnd.y));

		Int2 newCoveredTileCount = new Int2();
		newCoveredTileCount.x = (newPosGridRoomTopRight.x - newPosGridRoomBottomLeft.x) + 1;
		newCoveredTileCount.y = (newPosGridRoomTopRight.y - newPosGridRoomBottomLeft.y) + 1;

		if (newCoveredTileCount.y > 1){
			if(posGridEnd.y > posGridStart.y) newPosGridRoomBottomLeft.y -= (newCoveredTileCount.y - 1);
			else newPosGridRoomTopRight.y += (newCoveredTileCount.y - 1);
			newCoveredTileCount.y = newCoveredTileCount.y * 2 - 1;
		}

		Int2 gridSize = ShipGrid.GetInstance().GetSize();

		bool isColliding = false;
		for (int x = 0; x < newCoveredTileCount.x; x++){
			if(isColliding) break;

			for (int y = 0; y < newCoveredTileCount.y; y++){
				Int2 tilePosGrid = newPosGridRoomBottomLeft + new Int2(x, y);
				tilePosGrid.x = Mathf.Clamp(tilePosGrid.x, 0, gridSize.x);
				tilePosGrid.y = Mathf.Clamp(tilePosGrid.y, 0, gridSize.y);

				ShipGrid.Tile tile = ShipGrid.GetInstance().GetTile(tilePosGrid);
				isColliding = tile.GetIsRoom();

				Vector2 size = new Vector2(gridSize.x, gridSize.y);
				Vector2 worldPos = (Vector2)ShipGrid.GetInstance().transform.position - (size / 2) + new Vector2(tilePosGrid.x, tilePosGrid.y) + new Vector2(0.5f, 0.5f);
				float duration = 100.0f;
				float length = 0.25f;
				Debug.DrawLine(worldPos + length * Vector2.up, worldPos + length * Vector2.right, Color.magenta, duration);
				Debug.DrawLine(worldPos + length * Vector2.right, worldPos + length * Vector2.down, Color.magenta, duration);
				Debug.DrawLine(worldPos + length * Vector2.down, worldPos + length * Vector2.left, Color.magenta, duration);
				Debug.DrawLine(worldPos + length * Vector2.left, worldPos + length * Vector2.up, Color.magenta, duration);
			}
		}

		if (isColliding){
			newCoveredTileCount = lastCoveredTileCount;
			newPosGridRoomBottomLeft = lastPosGridRoomBottomLeft;
			newPosGridRoomTopRight = lastPosGridRoomTopRight;
		}
		lastPosGridRoomBottomLeft = newPosGridRoomBottomLeft;
		lastPosGridRoomTopRight = newPosGridRoomTopRight;
		lastCoveredTileCount = newCoveredTileCount;

		affectedTileCount = newCoveredTileCount.x * newCoveredTileCount.y;
		if (affectedTiles == null || affectedTiles.Length < affectedTileCount){
			affectedTiles = new Int2[affectedTileCount];
		}

		int roomID = ShipGrid.Tile.GetUniqueRoomID(newPosGridRoomBottomLeft);
		for (int x = 0; x < newCoveredTileCount.x; x++){
			for (int y = 0; y < newCoveredTileCount.y; y++){
				Int2 tilePosGrid = newPosGridRoomBottomLeft + new Int2(x, y);
				tilePosGrid.x = Mathf.Clamp(tilePosGrid.x, 0, gridSize.x);
				tilePosGrid.y = Mathf.Clamp(tilePosGrid.y, 0, gridSize.y);

				ShipGrid.Tile tile = ShipGrid.GetInstance().GetTile(tilePosGrid);
				tile.SetRotation(rotation, isTemporary);
				tile.CreateRoom(roomTypeCurrent, roomID, typeBlock, newPosGridRoomBottomLeft, newPosGridRoomTopRight, isTemporary);
				affectedTiles[y * newCoveredTileCount.x + x] = tilePosGrid;
			}
		}
	}
}
