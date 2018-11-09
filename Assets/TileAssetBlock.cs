using System;
using UnityEngine;
[Serializable] public class TileAssetBlock {
	[Serializable] public struct ColumnDataInt2 { // can't use 2D array if we want custom prop drawer to work...
		public Int2[] Data;
	}

	public const int MAX_WIDTH = 3;
	public const int MAX_HEIGHT = 5;

	public enum BlockType { None, Single, Line, Block }

	public ColumnDataInt2[] Block = new ColumnDataInt2[MAX_WIDTH];
	public Int2[] Line = new Int2[MAX_WIDTH];
	public Int2 Single;

	private bool hasSetHasValueInBlock = false;
	private bool hasValueInBlock = false;

	private bool hasSetHasValueInLine = false;
	private bool hasValueInLine = false;

	private bool hasSetHasValueInSingle = false;
	private bool hasValueInSingle = false;


	public Int2 GetPosTexture(Int2 posTileAssetBlock, BlockType typeBlock) {
		posTileAssetBlock.y += (MAX_HEIGHT - 1) / 2; // go from 0->4 to -2->2

		switch (typeBlock){
			case BlockType.None:
				return Int2.zero;
			case BlockType.Block:
				return Block[posTileAssetBlock.x].Data[posTileAssetBlock.y];
			case BlockType.Line:
				return Line[posTileAssetBlock.x];
			case BlockType.Single:
				return Single;
			default:
				Debug.LogError(typeBlock + " hasn't been properly implemented yet!");
				return Int2.zero;
		}
	}

	public bool HasValueInBlock(int x, int y) {
		Int2 texPos = Block[x].Data[y];
		return texPos.x >= 0 && texPos.y >= 0;
	}

	public bool HasAnyValueInBlock(){
		if (hasSetHasValueInBlock) { 
			return hasValueInBlock;
		}

		hasSetHasValueInBlock = true;

		for (int x = 0; x < Block.Length; x++){
			ColumnDataInt2 block = Block[x];
			for (int y = 0; y < block.Data.Length; y++){
				Int2 data = block.Data[y];
				if (data.x >= 0 && data.y >= 0) {
					hasValueInBlock = true;
					return true;
				}
			}
		}

		hasValueInBlock = false;
		return false;
	}

	public bool HasAnyValueInLine(){
		if (hasSetHasValueInLine) { 
			return hasValueInLine;
		}

		hasSetHasValueInLine = true;

		for (int x = 0; x < Line.Length; x++){
			Int2 data = Line[x];
			if (data.x >= 0 && data.y >= 0) {
				hasValueInLine = true;
				return true;
			}
		}

		hasValueInLine = false;
		return false;
	}

	public bool HasAnyValueInSingle(){
		if (hasSetHasValueInLine) { 
			return hasValueInLine;
		}

		hasValueInLine = Single.x >= 0 && Single.y >= 0;
		return hasValueInLine;
	}
}
