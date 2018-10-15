using UnityEngine;

public class PerlinTest : MonoBehaviour {
	[SerializeField] private Color32[] vertexColors;

	private MeshFilter meshFilter;


	void OnValidate() {
		if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
		int vertexCount = meshFilter.sharedMesh.vertexCount;
		if (vertexColors.Length != vertexCount) vertexColors = new Color32[vertexCount];
		
		meshFilter.sharedMesh.colors32 = vertexColors;
	}
}
