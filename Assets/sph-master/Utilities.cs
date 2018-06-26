using UnityEngine;
public class Utilities {
	public static void Math_SAXPY(ref Vector3 result, float alpha, Vector3 v){
		result[0] += alpha*v[0];
		result[1] += alpha*v[1];
		result[2] += alpha*v[2];
	}
}
