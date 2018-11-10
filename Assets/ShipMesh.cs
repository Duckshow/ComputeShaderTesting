using UnityEngine;

public class ShipMesh : Singleton<ShipMesh> {

	private const int PIXELS_PER_UNIT = 32;
	private const int SIZE_TILE = 1;

	private const int VERTICES_PER_TILE = 5;
	private const int TRIS_PER_TILE = 4;

	private const int VERTEX_INDEX_BOTTOM_LEFT = 0;
	private const int VERTEX_INDEX_TOP_LEFT = 1;
	private const int VERTEX_INDEX_TOP_RIGHT = 2;
	private const int VERTEX_INDEX_BOTTOM_RIGHT = 3;
	private const int VERTEX_INDEX_CENTER = 4;

	private const string PROPERTY_SCALESHIPX = "_ScaleShipX";
	private const string PROPERTY_SCALESHIPY = "_ScaleShipY";
	private const string PROPERTY_SCALEELEMENTS = "_ScaleElements";
	private const string PROPERTY_SCALETIME = "_ScaleTime";
	
	[SerializeField] private Material material;
	[SerializeField] private Camera cameraPixellation;
	[SerializeField] private RenderTexture texturePixellation;
	[SerializeField] private Transform meshPixellation;
	[Space]
	[SerializeField] private Int2 size;
	[SerializeField] private float scaleElements = 0.125f;
	[SerializeField] private float scaleTime = 2.0f;


	[System.Serializable] public class MeshHandler {
		private Sorting sorting;

		[SerializeField] private MeshFilter meshFilter;
		private Mesh mesh;
		private Vector2[] uvs;

		public void Init(Sorting sorting) {
			this.sorting = sorting;
			mesh = meshFilter.mesh;
			uvs = mesh.uv;
		}

		public void Update() {
			mesh.uv = uvs;
			meshFilter.mesh = mesh;
		}

		public void Generate(Int2 size) {
			mesh = new Mesh();

			int vertexCount = size.x * size.y * VERTICES_PER_TILE;

			Vector3[] vertices = new Vector3[vertexCount];
			uvs = new Vector2[vertexCount];
			Vector2[] uvPerlin = new Vector2[vertexCount];
			int[] tris = new int[size.x * size.y * TRIS_PER_TILE * 3];
			
			Color32[] vertexColors = new Color32[vertexCount];
			for (int i = 0; i < vertexColors.Length; i++){
				vertexColors[i] = new Color32(0, 0, 0, 255);
			}

			Vector3 offset = new Vector3(size.x / 2, size.y / 2, 0) * -1;

			int vIndex = 0;
			int tIndex = 0;
			for (int y = 0; y < size.y; y++){
				for (int x = 0; x < size.x; x++){
					int vIndexBottomLeft = vIndex + VERTEX_INDEX_BOTTOM_LEFT;
					int vIndexTopLeft = vIndex + VERTEX_INDEX_TOP_LEFT;
					int vIndexTopRight = vIndex + VERTEX_INDEX_TOP_RIGHT;
					int vIndexBottomRight = vIndex + VERTEX_INDEX_BOTTOM_RIGHT;
					int vIndexCenter = vIndex + VERTEX_INDEX_CENTER;

					float sizeShipX = (float)size.x;
					float sizeShipY = (float)size.y;
					Vector2 uvTextureMin = Vector2.zero;
					Vector2 uvTextureMax = Vector2.one;
					Vector2 uvPerlinMin = new Vector2(x / sizeShipX, y / sizeShipY);
					Vector2 uvPerlinMax = new Vector2((x + SIZE_TILE) / sizeShipX, (y + SIZE_TILE) / sizeShipY);
					Vector2 uvPerlinCenter = new Vector2((x + SIZE_TILE * 0.5f) / sizeShipX, (y + SIZE_TILE * 0.5f) / sizeShipY);

					vertices[vIndexBottomLeft] = offset + new Vector3(x, y, 0.0f);
					uvs[vIndexBottomLeft] = uvTextureMin;
					uvPerlin[vIndexBottomLeft] = uvPerlinMin;

					vertices[vIndexTopLeft] = offset + new Vector3(x, y + SIZE_TILE, 0.0f);
					uvs[vIndexTopLeft] = new Vector2(uvTextureMin.x, uvTextureMax.y);
					uvPerlin[vIndexTopLeft] = new Vector2(uvPerlinMin.x, uvPerlinMax.y);

					vertices[vIndexTopRight] = offset + new Vector3(x + SIZE_TILE, y + SIZE_TILE, 0.0f);
					uvs[vIndexTopRight] = uvTextureMax;
					uvPerlin[vIndexTopRight] = uvPerlinMax;

					vertices[vIndexBottomRight] = offset + new Vector3(x + SIZE_TILE, y, 0.0f);
					uvs[vIndexBottomRight] = new Vector2(uvTextureMax.x, uvTextureMin.y);
					uvPerlin[vIndexBottomRight] = new Vector2(uvPerlinMax.x, uvPerlinMin.y);

					vertices[vIndexCenter] = offset + new Vector3(x + SIZE_TILE * 0.5f, y + SIZE_TILE * 0.5f, 0.0f);
					uvs[vIndexCenter] = uvTextureMax * 0.5f;
					uvPerlin[vIndexCenter] = uvPerlinCenter;

					float random = Random.value;
					if (random < 0.1f){
						byte r = (byte)(Random.value < 0.5 ? 255 : 0);
						byte g = (byte)(Random.value < 0.5 ? 255 : 0);
						byte b = (byte)(Random.value < 0.5 ? 255 : 0);
						Color32 newColor = new Color32(r, g, b, 255);

						vertexColors[vIndexBottomLeft] = newColor;
						vertexColors[vIndexTopLeft] = newColor;
						vertexColors[vIndexTopRight] = newColor;
						vertexColors[vIndexBottomRight] = newColor;
						vertexColors[vIndexCenter] = newColor;

						if(x > 0){
							vertexColors[vIndexBottomRight - VERTICES_PER_TILE] = newColor;
							vertexColors[vIndexTopRight - VERTICES_PER_TILE] = newColor;
							vertexColors[vIndexCenter - VERTICES_PER_TILE] = newColor;
						} 
						if (x < size.x - 1){
							vertexColors[vIndexBottomLeft + VERTICES_PER_TILE] = newColor;
							vertexColors[vIndexTopLeft + VERTICES_PER_TILE] = newColor;
							vertexColors[vIndexCenter + VERTICES_PER_TILE] = newColor;
						}
					}

					vIndex += VERTICES_PER_TILE;
					
					tris[tIndex + 0] = vIndexBottomLeft;
					tris[tIndex + 1] = vIndexTopLeft;
					tris[tIndex + 2] = vIndexCenter;

					tris[tIndex + 3] = vIndexCenter;
					tris[tIndex + 4] = vIndexTopLeft;
					tris[tIndex + 5] = vIndexTopRight;

					tris[tIndex + 6] = vIndexTopRight;
					tris[tIndex + 7] = vIndexBottomRight;
					tris[tIndex + 8] = vIndexCenter;

					tris[tIndex + 9] = vIndexCenter;
					tris[tIndex + 10] = vIndexBottomRight;
					tris[tIndex + 11] = vIndexBottomLeft;

					tIndex += TRIS_PER_TILE * 3;
				}
			}

			mesh.vertices = vertices;
			mesh.uv = uvs;
			mesh.uv2 = uvPerlin;
			mesh.triangles = tris;
			mesh.colors32 = vertexColors;
			mesh.RecalculateBounds();
			meshFilter.mesh = mesh;

		}

		public void UpdateTileAsset(Int2 posGrid) {
			ShipGrid.Tile tile = ShipGrid.GetInstance().GetTile(posGrid);
			Int2 posSpriteSheet = AssetManager.GetInstance().GetAssetForTile(tile, sorting);
			Vector2 posTexture = new Vector2(posSpriteSheet.x * PIXELS_PER_UNIT, posSpriteSheet.y * PIXELS_PER_UNIT);
			Vector2 spriteSheetTilesSize = AssetManager.GetInstance().GetSpriteSheetTilesSize();


			Vector2 assetUVMin = new Vector2();
			assetUVMin.x = posTexture.x / spriteSheetTilesSize.x;
			assetUVMin.y = posTexture.y / spriteSheetTilesSize.y;

			Vector2 assetUVCenter = new Vector2();
			assetUVCenter.x = (posTexture.x + SIZE_TILE * PIXELS_PER_UNIT * 0.5f) / spriteSheetTilesSize.x;
			assetUVCenter.y = (posTexture.y + SIZE_TILE * PIXELS_PER_UNIT * 0.5f) / spriteSheetTilesSize.y;

			Vector2 assetUVMax = new Vector2();
			assetUVMax.x = (posTexture.x + SIZE_TILE * PIXELS_PER_UNIT) / spriteSheetTilesSize.x;
			assetUVMax.y = (posTexture.y + SIZE_TILE * PIXELS_PER_UNIT) / spriteSheetTilesSize.y;

			int vertexIndex = posGrid.y * (ShipMesh.GetInstance().GetSizeGrid().x * VERTICES_PER_TILE) + posGrid.x * VERTICES_PER_TILE;
			uvs[vertexIndex + VERTEX_INDEX_BOTTOM_LEFT] = assetUVMin;
			uvs[vertexIndex + VERTEX_INDEX_TOP_LEFT] = new Vector2(assetUVMin.x, assetUVMax.y);
			uvs[vertexIndex + VERTEX_INDEX_TOP_RIGHT] = assetUVMax;
			uvs[vertexIndex + VERTEX_INDEX_BOTTOM_RIGHT] = new Vector2(assetUVMax.x, assetUVMin.y);
			uvs[vertexIndex + VERTEX_INDEX_CENTER] = assetUVCenter;
		}
	}

	[SerializeField] private MeshHandler meshBack;
	[SerializeField] private MeshHandler meshFront;

	private bool isDirty = false;

	private int propertyID_ScaleShipX;
	private int propertyID_ScaleShipY;
	private int propertyID_ScaleElements;
	private int propertyID_ScaleTime;

	public Int2 GetSizeGrid() {
		return size;
	}
	
	public Int2 GetSizeWorld() {
		return size * SIZE_TILE;
	}

	public override bool IsUsingAwakeEarly() { return true; }
	public override void AwakeEarly(){
		meshBack.Init(Sorting.Back);
		meshFront.Init(Sorting.Front);
	}

	public override bool IsUsingUpdateLate() { return true; }
	public override void UpdateLate() { 
		if (isDirty){
			isDirty = false;

			meshBack.Update();
			meshFront.Update();
		}
	}

	[EasyButtons.Button]
	public void Generate(){
		meshBack.Generate(size);
		meshFront.Generate(size);

		Int2 sizeShipPixels = size * PIXELS_PER_UNIT;
		if (sizeShipPixels.x != texturePixellation.width || sizeShipPixels.y != texturePixellation.height){
			Debug.LogErrorFormat("TexturePixellation's size has to be set manually! Correct size is X: {0}, Y: {1}", sizeShipPixels.x, sizeShipPixels.y);
		}
		cameraPixellation.orthographicSize = size.y / 2;
		meshPixellation.localScale = new Vector3(size.x, size.y, 1);

		propertyID_ScaleShipX = Shader.PropertyToID(PROPERTY_SCALESHIPX);
		propertyID_ScaleShipY = Shader.PropertyToID(PROPERTY_SCALESHIPY);
		propertyID_ScaleElements = Shader.PropertyToID(PROPERTY_SCALEELEMENTS);
		propertyID_ScaleTime = Shader.PropertyToID(PROPERTY_SCALETIME);

		material.SetFloat(propertyID_ScaleShipX, size.x);
		material.SetFloat(propertyID_ScaleShipY, size.y);
		material.SetFloat(propertyID_ScaleElements, scaleElements);
		material.SetFloat(propertyID_ScaleTime, scaleTime);
	}

	public void UpdateTileAsset(Int2 posGrid) {
		meshBack.UpdateTileAsset(posGrid);
		meshFront.UpdateTileAsset(posGrid);
		isDirty = true;
	}
}
