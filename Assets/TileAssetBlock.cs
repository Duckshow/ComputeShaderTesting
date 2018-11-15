using System;
using UnityEngine;
[Serializable] public class TileAssetBlock {
	[Serializable] public struct ColumnDataInt2 { // can't use 2D array if we want custom prop drawer to work...
		public Int2[] Data;
	}

	public const int MAX_WIDTH = 3;
	public const int MAX_HEIGHT = 5;

	public enum BlockType { None, Single, Line, Block }

	public bool IsRoom = false;
	public bool CanRotate = false;

	[UnityEngine.Serialization.FormerlySerializedAs("Block")] public ColumnDataInt2[] BlockBack = new ColumnDataInt2[MAX_WIDTH];
	public ColumnDataInt2[] BlockFront = new ColumnDataInt2[MAX_WIDTH];
	[UnityEngine.Serialization.FormerlySerializedAs("Line")] public Int2[] LineBack = new Int2[MAX_WIDTH];
	public Int2[] LineFront = new Int2[MAX_WIDTH];
	[UnityEngine.Serialization.FormerlySerializedAs("Single")] public Int2 SingleBack;
	public Int2 SingleFront;

	private bool hasSetHasValueInBlock = false;
	private bool hasValueInBlock = false;

	private bool hasSetHasValueInLine = false;
	private bool hasValueInLine = false;

	private bool hasSetHasValueInSingle = false;
	private bool hasValueInSingle = false;


	public Int2 GetPosTexture(Int2 posTileAssetBlock, BlockType typeBlock, Sorting sorting) {
		posTileAssetBlock.y *= -1; // because array is "upside-down" to look better in editor
		posTileAssetBlock.y += (MAX_HEIGHT - 1) / 2; // go from 0->4 to -2->2

		switch (typeBlock){
			case BlockType.None:
				return Int2.zero;
			case BlockType.Block:
				ColumnDataInt2[] block = sorting == Sorting.Front ? BlockFront : BlockBack;
				return block[posTileAssetBlock.x].Data[posTileAssetBlock.y];
			case BlockType.Line:
				Int2[] line = sorting == Sorting.Front ? LineFront : LineBack;
				return line[posTileAssetBlock.x];
			case BlockType.Single:
				Int2 single = sorting == Sorting.Front ? SingleFront : SingleBack;
				return single;
			default:
				Debug.LogError(typeBlock + " hasn't been properly implemented yet!");
				return Int2.zero;
		}
	}

	public bool HasAnyValueInBlock(){
		if (hasSetHasValueInBlock) { 
			return hasValueInBlock;
		}

		hasSetHasValueInBlock = true;
		hasValueInBlock = HasAnyValueInBlock(BlockBack);
		if(!hasValueInBlock) hasValueInBlock = HasAnyValueInBlock(BlockFront);

		return hasValueInBlock;
	}

	public bool HasAnyValueInLine(){
		if (hasSetHasValueInLine) { 
			return hasValueInLine;
		}

		hasSetHasValueInLine = true;
		hasValueInLine = HasAnyValueInLine(LineBack);
		if(!hasValueInLine) hasValueInLine = HasAnyValueInLine(LineFront);

		return hasValueInLine;
	}

	public bool HasAnyValueInSingle(){
		if (hasSetHasValueInSingle) { 
			return hasValueInSingle;
		}

		hasValueInSingle = HasAnyValueInSingle(SingleBack);
		if(!hasValueInSingle) hasValueInSingle = HasAnyValueInSingle(SingleFront);
		return hasValueInSingle;
	}

	bool HasAnyValueInBlock(ColumnDataInt2[] block) { 
		for (int x = 0; x < block.Length; x++){
			ColumnDataInt2 column = block[x];
			for (int y = 0; y < column.Data.Length; y++){
				Int2 data = column.Data[y];
				if (data.x >= 0 && data.y >= 0) {
					return true;
				}
			}
		}
		return false;
	}

	bool HasAnyValueInLine(Int2[] line) { 
		for (int x = 0; x < line.Length; x++){
			Int2 data = line[x];
			if (data.x >= 0 && data.y >= 0) {
				return true;
			}
		}
		return false;
	}

	bool HasAnyValueInSingle(Int2 single) {
		return single.x >= 0 && single.y >= 0;
	}
}
