using UnityEngine;
public static class Extensions {
	public static float GetAxis(this Vector3 v, int axisIndex){
		if(axisIndex == 0) return v.x;
		if(axisIndex == 1) return v.y;
		if(axisIndex == 2) return v.z;
		else return 0;
	}
	public static void SetAxis(this Vector3 v, int axisIndex, float value){
		if(axisIndex == 0) v.x = value;
		if(axisIndex == 1) v.y = value;
		if(axisIndex == 2) v.z = value;
	}
}
