using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ElementEmulator : MonoBehaviour {

	[SerializeField]
	private ComputeShader shader;
	[SerializeField]
	private Texture2D terrainMap;
	[SerializeField]
	private Material material;

	private const int THREAD_COUNT_MAX = 1024;
	private const int PIXELS_PER_TILE_EDGE = 32;
	private const int GRID_WIDTH_TILES = 3;
	private const int GRID_HEIGHT_TILES = 1;
	private const string KERNEL_MAIN = "CSMain";
	
	private const string PROPERTY_UVBUFFER = "uvBuffer";
	private int shaderPropertyID_uvBuffer;

	private const string PROPERTY_TERRAINMAP = "terrainMap";
	private int shaderPropertyID_terrainMap;

	private const string PROPERTY_OUTPUT = "output";
	private int shaderPropertyID_output;

	private int kernelID;
	private int threadCountAxis;


	void Awake(){
		kernelID = shader.FindKernel(KERNEL_MAIN);
		threadCountAxis = (int)Mathf.Sqrt(THREAD_COUNT_MAX);

		shaderPropertyID_uvBuffer = Shader.PropertyToID(PROPERTY_UVBUFFER);
		shaderPropertyID_terrainMap = Shader.PropertyToID(PROPERTY_TERRAINMAP);
		shaderPropertyID_output = Shader.PropertyToID(PROPERTY_OUTPUT);
	}
	void Start () {
		RunShader();
	}
	
	[EasyButtons.Button]
	public void RunShader(){
		transform.localScale = new Vector3(GRID_WIDTH_TILES, GRID_HEIGHT_TILES, 1);

		int gridPixelsWidth = PIXELS_PER_TILE_EDGE * GRID_WIDTH_TILES;
		int gridPixelsHeight = PIXELS_PER_TILE_EDGE * GRID_HEIGHT_TILES;
		
		RenderTexture output = new RenderTexture(gridPixelsWidth, gridPixelsHeight, 24);
		output.enableRandomWrite = true;
		output.Create();

		Vector2[] uvs = GetGridUVs();
		ComputeBuffer buffer = new ComputeBuffer(uvs.Length, sizeof(float) * 2);
		buffer.SetData(uvs);
		shader.SetBuffer(kernelID, shaderPropertyID_uvBuffer, buffer);
		shader.SetTexture(kernelID, shaderPropertyID_terrainMap, terrainMap);
		shader.SetTexture(kernelID, shaderPropertyID_output, output);

		shader.Dispatch(kernelID, gridPixelsWidth / threadCountAxis, gridPixelsHeight / threadCountAxis, 1);
		material.mainTexture = output;

		buffer.Dispose();
	}

	Vector2[] GetGridUVs(){
		Vector2[] uvs = new Vector2[3];
		uvs[0] = new Vector2(0, 0);
		uvs[1] = new Vector2(0.3333f, 0);
		uvs[2] = new Vector2(0.6666f, 0);
		return uvs;
	}
}
