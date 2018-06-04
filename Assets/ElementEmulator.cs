using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElementEmulator : MonoBehaviour {

	struct PixelContent{ // WARNING: variables must correspond to ElementEmulator.compute's PixelContent!
		public float Element1;
		public float Temperature;

		public Vector4 Wind;
		public Vector4 TerrainMapPixel;

		public static int GetStride() {
			return sizeof(float) * 10; // must correspond to variables!
		}
	}

	[SerializeField]
	private ComputeShader shader;
	[SerializeField]
	private Texture2D terrainMap;
	[SerializeField]
	private Material material;
	private float updateInterval = 0.1f;

	private const int THREAD_COUNT_MAX = 1024;

	// ============== WARNING: shared with ElementEmulator.compute! must be equal!
	private const int PIXELS_PER_TILE_EDGE = 32;
	private const int GRID_WIDTH_TILES = 3;
	private const int GRID_HEIGHT_TILES = 1;
	private const int GRID_WIDTH_PIXELS = PIXELS_PER_TILE_EDGE * GRID_WIDTH_TILES;
	private const int GRID_HEIGHT_PIXELS = PIXELS_PER_TILE_EDGE * GRID_HEIGHT_TILES;
	//===============

	private const string KERNEL_INIT = "Init";
	private int kernelID_Init;

	private const string KERNEL_UPDATE = "Update";
	private int kernelID_Update;
	
	private const string PROPERTY_UVS = "uvs";
	private int shaderPropertyID_uvs;

	private const string PROPERTY_PIXELSCONTENT = "pixelsContent";
	private int shaderPropertyID_pixelsContent;

	private const string PROPERTY_TERRAINMAP = "terrainMap";
	private int shaderPropertyID_terrainMap;

	private const string PROPERTY_OUTPUT = "output";
	private int shaderPropertyID_output;

	private int threadCountAxis;

	private int threadGroupCountX;
	private int threadGroupCountY;

	private RenderTexture output;
	private Vector2[] uvs;
	private ComputeBuffer bufferUVs;
	private ComputeBuffer bufferPixels;
	private PixelContent[] pixelsContent;

	private float nextTimeToUpdate = -1.0f;

	private int flowPixelIndex = -1;


	void Awake(){
		kernelID_Init = shader.FindKernel(KERNEL_INIT);
		kernelID_Update = shader.FindKernel(KERNEL_UPDATE);
		threadCountAxis = (int)Mathf.Sqrt(THREAD_COUNT_MAX);
		threadGroupCountX = GRID_WIDTH_PIXELS / threadCountAxis;
		threadGroupCountY = GRID_HEIGHT_PIXELS / threadCountAxis;

		shaderPropertyID_uvs = Shader.PropertyToID(PROPERTY_UVS);
		shaderPropertyID_pixelsContent = Shader.PropertyToID(PROPERTY_PIXELSCONTENT);
		shaderPropertyID_terrainMap = Shader.PropertyToID(PROPERTY_TERRAINMAP);
		shaderPropertyID_output = Shader.PropertyToID(PROPERTY_OUTPUT);
	}


	void OnDisable(){
		bufferUVs.Dispose();
		bufferPixels.Dispose();
	}
	
	void Start () {
		InitShader();
	}

	void Update() {
		if (Time.time < nextTimeToUpdate) return;

		nextTimeToUpdate = Time.time + updateInterval;
		UpdateShader();
	}

	void InitShader(){
		transform.localScale = new Vector3(GRID_WIDTH_TILES, GRID_HEIGHT_TILES, 1);

		output = new RenderTexture(GRID_WIDTH_PIXELS, GRID_HEIGHT_PIXELS, 24);
		output.enableRandomWrite = true;
		output.filterMode = FilterMode.Point;
		output.Create();

		uvs = GetGridUVs();
		bufferUVs = new ComputeBuffer(uvs.Length, sizeof(float) * 2);
		bufferUVs.SetData(uvs);
		shader.SetBuffer(kernelID_Init, shaderPropertyID_uvs, bufferUVs);

		pixelsContent = new PixelContent[GRID_WIDTH_PIXELS * GRID_HEIGHT_PIXELS];
		int x = 0, y = 0;
		for (int i = 0; i < pixelsContent.Length; i++){
			if (i > 0){
				x++;
				if (x > GRID_WIDTH_PIXELS){
					x = 0;
					y++;
				}
			}
			float perlinMod = 4.0f;
			float perlinX = ((float)x / (float)GRID_WIDTH_PIXELS) * perlinMod * 3.0f;
			float perlinY = ((float)y / (float)GRID_HEIGHT_PIXELS) * perlinMod;

			PixelContent pixel = pixelsContent[i];
			pixel.Element1 = Mathf.Pow(Mathf.PerlinNoise(perlinX, perlinY), 1);
			pixel.Element1 = Random.value;
			pixel.Temperature = 1000.0f;
			pixelsContent[i] = pixel;
		}
		bufferPixels = new ComputeBuffer(pixelsContent.Length, PixelContent.GetStride());
		
		bufferPixels.SetData(pixelsContent);
		shader.SetBuffer(kernelID_Init, shaderPropertyID_pixelsContent, bufferPixels);

		shader.SetTexture(kernelID_Init, shaderPropertyID_terrainMap, terrainMap);

		shader.Dispatch(kernelID_Init, threadGroupCountX, threadGroupCountY, 1);
		bufferPixels.GetData(pixelsContent);
	}

	[EasyButtons.Button]
	public void UpdateShader() {
		bufferUVs.SetData(uvs);
		shader.SetBuffer(kernelID_Update, shaderPropertyID_uvs, bufferUVs);

		pixelsContent[Random.Range(0, pixelsContent.Length)].Wind = new Vector4(1, 0, 0, 0);

		bufferPixels.SetData(pixelsContent);
		shader.SetBuffer(kernelID_Update, shaderPropertyID_pixelsContent, bufferPixels);

		shader.SetTexture(kernelID_Update, shaderPropertyID_terrainMap, terrainMap);
		shader.SetTexture(kernelID_Update, shaderPropertyID_output, output);

		shader.Dispatch(kernelID_Update, threadGroupCountX, threadGroupCountY, 1);
		material.mainTexture = output;

		bufferPixels.GetData(pixelsContent);
	}

	Vector2[] GetGridUVs(){
		Vector2[] uvs = new Vector2[3];
		uvs[0] = new Vector2(0, 0);
		uvs[1] = new Vector2((1.0f / 3.0f) * 1.0f, 0);
		uvs[2] = new Vector2((1.0f / 3.0f) * 2.0f, 0);
		return uvs;
	}

	public float GetGridDiagnosis() {
		float element1Total = 0;
		float highest = -10000;
		for (int i = 0; i < pixelsContent.Length; i++){
			if(pixelsContent[i].Element1 > highest) highest = pixelsContent[i].Element1;
			element1Total += pixelsContent[i].Element1;
		}

		Debug.Log(highest);
		return element1Total;
	}
}
