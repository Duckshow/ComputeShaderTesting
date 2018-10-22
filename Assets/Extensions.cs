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

[System.Serializable] public struct Float2 {
	public float x;
	public float y;

	public Float2(float x, float y) {
		this.x = x;
		this.y = y;
	}

	public static Float2 operator +(Float2 value0, Float2 value1){
		return new Float2(value0.x + value1.x, value0.y + value1.y);
	}

	public static Float2 operator -(Float2 value0, Float2 value1){
		return new Float2(value0.x - value1.x, value0.y - value1.y);
	}
	
	public static Float2 operator *(Float2 value0, int m) {
		return new Float2(value0.x * m, value0.y * m);
	}
}

[System.Serializable] public struct Int2 {
	public int x;
	public int y;

	public Int2(int x, int y) {
		this.x = x;
		this.y = y;
	}
	public Int2(float x, float y) {
		this.x = (int)x;
		this.y = (int)y;
	}

	public static Int2 operator +(Int2 value0, Int2 value1){
		return new Int2(value0.x + value1.x, value0.y + value1.y);
	}

	public static Int2 operator -(Int2 value0, Int2 value1){
		return new Int2(value0.x - value1.x, value0.y - value1.y);
	}

	public static Int2 operator *(Int2 value0, int m) {
		return new Int2(value0.x * m, value0.y * m);
	}
}
