using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Linq;
using System;
using System.Reflection;

[CustomPropertyDrawer(typeof(TileAssetBlock))]
public class TileAssetBlockDrawer : PropertyDrawer {

	private static readonly Float2 PADDING = new Float2(20.0f, 20.0f);


	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
		position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
		EditorGUI.BeginProperty(position, label, property);

		Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
		property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, property.isExpanded ? "Hide" : "Show");

		if (property.isExpanded){
			EditorGUI.indentLevel = 0;
			EditorGUIUtility.labelWidth = 0.1f;

			SerializedProperty propBlock = property.FindPropertyRelative("Block");
			SerializedProperty propLine = property.FindPropertyRelative("Line");
			SerializedProperty propSingle = property.FindPropertyRelative("Single");

			if (propBlock.arraySize != TileAssetBlock.MAX_WIDTH) {
				propBlock.arraySize = TileAssetBlock.MAX_WIDTH;
			}
			if (propLine.arraySize != TileAssetBlock.MAX_WIDTH){
				propLine.arraySize = TileAssetBlock.MAX_WIDTH;
			}

			for (int x = 0; x < TileAssetBlock.MAX_WIDTH; x++){
				SerializedProperty propBlockData = propBlock.GetArrayElementAtIndex(x).FindPropertyRelative("Data");
				if (propBlockData.arraySize != TileAssetBlock.MAX_HEIGHT){
					propBlockData.arraySize = TileAssetBlock.MAX_HEIGHT;
				}

				for (int y = 0; y < TileAssetBlock.MAX_HEIGHT; y++){
					SerializedProperty propElement = propBlockData.GetArrayElementAtIndex(y);
					DrawField(propBlockData.GetArrayElementAtIndex(y), x, y, property, position);
				}

				DrawField(propLine.GetArrayElementAtIndex(x), x, TileAssetBlock.MAX_HEIGHT + 0.5f, property, position);
			}

			DrawField(propSingle, 0, TileAssetBlock.MAX_HEIGHT + 2.0f, property, position);
		}

		EditorGUI.EndProperty();
	}

	void DrawField(SerializedProperty field, float x, float y, SerializedProperty ownerProperty, Rect ownerPosition) {
		float width = GetChildPropertyWidth(ownerPosition.width);
		float height = GetChildPropertyHeight(field);

		Vector2 pos = ownerPosition.position;
		pos.x += x * width + x * PADDING.x;
		pos.y += (y + 1) * height + (y + 1) * PADDING.y;

		Rect rect = new Rect(pos.x, pos.y, width, height);
		EditorGUI.PropertyField(rect, field);
	}

	float GetChildPropertyWidth(float ownerWidth) { 
		return (ownerWidth / 3.0f) - PADDING.x;
	}

	float GetChildPropertyHeight(SerializedProperty childProp) { 
		return EditorGUI.GetPropertyHeight(childProp, includeChildren: true);
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
		if(property.isExpanded) {
			return PADDING.y * 2 + (EditorGUI.GetPropertyHeight(property, label, includeChildren: false) + PADDING.y) * (TileAssetBlock.MAX_HEIGHT + 3);
		}
		else{
			return EditorGUI.GetPropertyHeight(property, label, includeChildren: false);
		}
	}
}