using System;
using UnityEngine;
[Serializable] public class TileAssetBlock {
	[Serializable] public struct ColumnDataInt2 { // can't use 2D array if we want custom prop drawer to work...
		public Int2[] Data;
	}

	public const int MAX_WIDTH = 3;
	public const int MAX_HEIGHT = 5;
	public const int INDEX_CENTER_Y = 2;

	public ColumnDataInt2[] Block = new ColumnDataInt2[MAX_WIDTH];
	public Int2[] Line = new Int2[MAX_WIDTH];

	private bool hasSetHasValueInBlock = false;
	private bool hasValueInBlock = false;


	public Int2 GetPosTexture(Int2 posTileAssetBlock) {
		if (posTileAssetBlock.y == INDEX_CENTER_Y && HasValueAtPos(posTileAssetBlock.x)) {
			return Line[posTileAssetBlock.x];
		}

		return Block[posTileAssetBlock.x].Data[posTileAssetBlock.y];
	}

	public bool HasValueAtPos(int x, int y) {
		Int2 texPos = Block[x].Data[y];
		return texPos.x >= 0 && texPos.y >= 0;
	}

	public bool HasValueAtPos(int x) {
		Int2 texPos = Line[x];
		return texPos.x >= 0 && texPos.y >= 0;
	}

	public bool HasValueInBlock(){
		if (hasSetHasValueInBlock) { 
			return hasValueInBlock;
		}

		hasSetHasValueInBlock = true;

		for (int x = 0; x < Block.Length; x++){
			ColumnDataInt2 block = Block[x];
			for (int y = 0; y < block.Data.Length; y++){
				Int2 data = block.Data[y];
				if (data.x >= 0 || data.y >= 0) {
					hasValueInBlock = true;
					return true;
				}
			}
		}

		hasValueInBlock = false;
		return false;
	}
}
