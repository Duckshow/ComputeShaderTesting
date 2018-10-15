using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;


public class PerlinTextureGenerator : EditorWindow {
	private const float SCALE_MOD = 500.0f;
	private const string PATH_DIRECTORY = "Assets/PerlinTextures/";

	private float perlinScale = 1.0f;
	private int seed = 0;
	private int width = 32;
	private int height = 32;
	private Vector2 scrollPos;



	[MenuItem("Tools/Generate Perlin Texture")]
	public static void ShowWindow(){
		GetWindow(typeof(PerlinTextureGenerator));
	}

	void OnGUI(){
		if (Application.isPlaying) return;

		seed = EditorGUILayout.IntField("Seed", seed);
		if (GUILayout.Button("Randomize Seed")){
			seed = Random.Range(0, 1000000);
		}

		GUILayout.Space(10);
		GUILayout.Label("Please only use power-of-two values!");
		width = EditorGUILayout.IntField("Width", width);
		height = EditorGUILayout.IntField("Height", height);
		
		GUILayout.Space(10);
		perlinScale = EditorGUILayout.Slider("Scale", perlinScale * SCALE_MOD, 0.0f, 100.0f) / SCALE_MOD;
		
		if (GUILayout.Button("Create")) Create();
	}

	public void Create(){
		Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
		Color32[] pixels = new Color32[width * height];
		int i = 0;
		for (int x = 0; x < width; x++){
			for (int y = 0; y < height; y++){
				EditorUtility.DisplayProgressBar("Perlin Texture Generator", "Generating...", ((float)i / (float)(width * height)));
				byte perlin = (byte)(Mathf.PerlinNoise((seed + x) * perlinScale, (seed + y) * perlinScale) * 255);
				pixels[y * width + x] = new Color32(perlin, 0, 0, 255);
				i++;
			}
		}
		EditorUtility.ClearProgressBar();
		texture.SetPixels32(pixels);

		if (!Directory.Exists(PATH_DIRECTORY)) Directory.CreateDirectory(PATH_DIRECTORY);

		string textureFileName = "";
		string texturePath = "";
		int iterations = 0;
		do{
			textureFileName = "perlin_" + iterations.ToString();
			texturePath = PATH_DIRECTORY + textureFileName + ".png";
			iterations++;
		}
		while (File.Exists(texturePath));

		byte[] png = texture.EncodeToPNG();
		string pngPath = Application.dataPath.Replace("Assets", "") + texturePath;
		File.WriteAllBytes(pngPath, png);

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();

		TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(texturePath);
		importer.alphaIsTransparency = true;
		importer.filterMode = FilterMode.Point;
		importer.isReadable = true;
		importer.mipmapEnabled = false;
		importer.wrapMode = TextureWrapMode.Clamp;
		importer.maxTextureSize = Mathf.Max(width, height);
		importer.SaveAndReimport();

		Debug.Log("Texture has been created!");

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();
	}
}
