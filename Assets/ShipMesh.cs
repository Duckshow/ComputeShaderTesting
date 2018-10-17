using UnityEngine;

public class ShipMesh : MonoBehaviour {

	private const int PIXELS_PER_UNIT = 32;

	private const int VERTICES_PER_TILE = 5;
	private const int TRIS_PER_TILE = 4;

	private const int VERTEX_INDEX_BOTTOM_LEFT = 0;
	private const int VERTEX_INDEX_TOP_LEFT = 1;
	private const int VERTEX_INDEX_TOP_RIGHT = 2;
	private const int VERTEX_INDEX_BOTTOM_RIGHT = 3;
	private const int VERTEX_INDEX_CENTER = 4;

	private const string PROPERTY_SCALESHIPX = "_ScaleShipX";
	private const string PROPERTY_SCALESHIPY = "_ScaleShipY";
	private const string PROPERTY_SCALEPERLIN = "_ScalePerlin";
	private const string PROPERTY_SCALETIME = "_ScaleTime";
	
	[SerializeField] private Material material;
	[SerializeField] private Camera cameraPixellation;
	[SerializeField] private RenderTexture texturePixellation;
	[SerializeField] private Transform meshPixellation;
	[Space]
	[SerializeField] private Vector2Int sizeShip;
	[SerializeField] private Vector2Int sizeTile;
	[SerializeField] private float scalePerlin = 0.125f;
	[SerializeField] private float scaleTime = 2.0f;

	private Color32[] vertexColors = new Color32[VERTICES_PER_TILE];

	private MeshRenderer meshRenderer;
	private MeshFilter meshFilter;
	private Mesh mesh;

	private int propertyID_ScaleShipX;
	private int propertyID_ScaleShipY;
	private int propertyID_ScalePerlin;
	private int propertyID_ScaleTime;


	[EasyButtons.Button]
	public void Generate(){
		meshRenderer = GetComponentInChildren<MeshRenderer>();
		meshFilter = GetComponentInChildren<MeshFilter>();
		mesh = new Mesh();

		int vertexCount = sizeShip.x * sizeShip.y * VERTICES_PER_TILE;

		Vector3[] vertices = new Vector3[vertexCount];
		Vector2[] uvTexture = new Vector2[vertexCount];
		Vector2[] uvPerlin = new Vector2[vertexCount];
		int[] tris = new int[sizeShip.x * sizeShip.y * TRIS_PER_TILE * 3];
		
		vertexColors = new Color32[vertexCount];
		for (int i = 0; i < vertexColors.Length; i++){
			vertexColors[i] = new Color32(0, 0, 0, 255);
		}

		Vector3 offset = new Vector3(sizeShip.x / 2, sizeShip.y / 2, 0) * -1;

		int vIndex = 0;
		int tIndex = 0;
		for (int y = 0; y < sizeShip.y; y++){
			for (int x = 0; x < sizeShip.x; x++){
				int vIndexBottomLeft = vIndex + VERTEX_INDEX_BOTTOM_LEFT;
				int vIndexTopLeft = vIndex + VERTEX_INDEX_TOP_LEFT;
				int vIndexTopRight = vIndex + VERTEX_INDEX_TOP_RIGHT;
				int vIndexBottomRight = vIndex + VERTEX_INDEX_BOTTOM_RIGHT;
				int vIndexCenter = vIndex + VERTEX_INDEX_CENTER;

				float sizeShipX = (float)sizeShip.x;
				float sizeShipY = (float)sizeShip.y;
				Vector2 uvTextureMin = Vector2.zero;
				Vector2 uvTextureMax = Vector2.one;
				Vector2 uvPerlinMin = new Vector2(x / sizeShipX, y / sizeShipY);
				Vector2 uvPerlinMax = new Vector2((x + 1) / sizeShipX, (y + 1) / sizeShipY);
				Vector2 uvPerlinCenter = new Vector2((x + 0.5f) / sizeShipX, (y + 0.5f) / sizeShipY);

				vertices[vIndexBottomLeft] = offset + new Vector3(x, y, 0.0f);
				uvTexture[vIndexBottomLeft] = uvTextureMin;
				uvPerlin[vIndexBottomLeft] = uvPerlinMin;

				vertices[vIndexTopLeft] = offset + new Vector3(x, y + sizeTile.y, 0.0f);
				uvTexture[vIndexTopLeft] = new Vector2(uvTextureMin.x, uvTextureMax.y);
				uvPerlin[vIndexTopLeft] = new Vector2(uvPerlinMin.x, uvPerlinMax.y);

				vertices[vIndexTopRight] = offset + new Vector3(x + sizeTile.x, y + sizeTile.y, 0.0f);
				uvTexture[vIndexTopRight] = uvTextureMax;
				uvPerlin[vIndexTopRight] = uvPerlinMax;

				vertices[vIndexBottomRight] = offset + new Vector3(x + sizeTile.x, y, 0.0f);
				uvTexture[vIndexBottomRight] = new Vector2(uvTextureMax.x, uvTextureMin.y);
				uvPerlin[vIndexBottomRight] = new Vector2(uvPerlinMax.x, uvPerlinMin.y);

				vertices[vIndexCenter] = offset + new Vector3(x + sizeTile.x * 0.5f, y + sizeTile.y * 0.5f, 0.0f);
				uvTexture[vIndexCenter] = uvTextureMax * 0.5f;
				uvPerlin[vIndexCenter] = uvPerlinCenter;

				if (Random.value < 0.1f){
					Color32 newColor = new Color32(255, 0, 0, 255);
					vertexColors[vIndexBottomLeft] = newColor;
					vertexColors[vIndexTopLeft] = newColor;
					vertexColors[vIndexTopRight] = newColor;
					vertexColors[vIndexBottomRight] = newColor;
					vertexColors[vIndexCenter] = newColor;

					if(x > 0){
						vertexColors[vIndexBottomRight - VERTICES_PER_TILE] = newColor;
						vertexColors[vIndexTopRight - VERTICES_PER_TILE] = newColor;
					} 
					if (x < sizeShip.x - 1){
						vertexColors[vIndexBottomLeft + VERTICES_PER_TILE] = newColor;
						vertexColors[vIndexTopLeft + VERTICES_PER_TILE] = newColor;
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
		mesh.uv = uvTexture;
		mesh.uv2 = uvPerlin;
		mesh.triangles = tris;
		mesh.colors32 = vertexColors;
		mesh.RecalculateBounds();
		meshFilter.mesh = mesh;

		Vector2Int sizeShipPixels = sizeShip * PIXELS_PER_UNIT;
		if (sizeShipPixels.x != texturePixellation.width || sizeShipPixels.y != texturePixellation.height){
			Debug.LogErrorFormat("TexturePixellation's size has to be set manually! Correct size is X: {0}, Y: {1}", sizeShipPixels.x, sizeShipPixels.y);
		}
		cameraPixellation.orthographicSize = sizeShip.y / 2;
		meshPixellation.localScale = new Vector3(sizeShip.x, sizeShip.y, 1);

		propertyID_ScaleShipX = Shader.PropertyToID(PROPERTY_SCALESHIPX);
		propertyID_ScaleShipY = Shader.PropertyToID(PROPERTY_SCALESHIPY);
		propertyID_ScalePerlin = Shader.PropertyToID(PROPERTY_SCALEPERLIN);
		propertyID_ScaleTime = Shader.PropertyToID(PROPERTY_SCALETIME);

		material.SetFloat(propertyID_ScaleShipX, sizeShip.x);
		material.SetFloat(propertyID_ScaleShipY, sizeShip.y);
		material.SetFloat(propertyID_ScalePerlin, scalePerlin);
		material.SetFloat(propertyID_ScaleTime, scaleTime);
	}
}
