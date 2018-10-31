using System;
using UnityEngine;
[Serializable] public class TileAssetBlock {

	public const int MAX_WIDTH = 3;
	public const int MAX_HEIGHT = 5;

	public static readonly string[] VARIABLE_NAMES = new string[]{
		"X0Y0",
		"X1Y0",
		"X2Y0",
		"X0Y1",
		"X1Y1",
		"X2Y1",
		"X0Y2",
		"X1Y2",
		"X2Y2",
		"X0Y3",
		"X1Y3",
		"X2Y3",
		"X0Y4",
		"X1Y4",
		"X2Y4"
	};

	public Int2 X0Y0;
	public Int2 X1Y0;
	public Int2 X2Y0;
	public Int2 X0Y1;
	public Int2 X1Y1;
	public Int2 X2Y1;
	public Int2 X0Y2;
	public Int2 X1Y2;
	public Int2 X2Y2;
	public Int2 X0Y3;
	public Int2 X1Y3;
	public Int2 X2Y3;
	public Int2 X0Y4;
	public Int2 X1Y4;
	public Int2 X2Y4;

	public Int2 GetPosTexture(Int2 posTileAssetBlock) { 
		
	}
}
