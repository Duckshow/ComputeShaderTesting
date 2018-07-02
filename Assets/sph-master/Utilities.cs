using UnityEngine;
public class Utilities {
	public static void Math_SAXPY(ref Vector3 result, float alpha, Vector3 v){
		result += alpha * v;
	}
}
