using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Linq;
using System;
using System.Reflection;

[CustomPropertyDrawer(typeof(TileAssetBlock))]
public class TileAssetBlockDrawer : PropertyDrawer {

	private static readonly Float2 PADDING = new Float2(20.0f, 10.0f);


	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
		position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
		EditorGUI.BeginProperty(position, label, property);

		Rect foldoutRect = position;
		foldoutRect.height = EditorGUIUtility.singleLineHeight;
		property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, property.isExpanded ? "Hide" : "Show");
		
		if (property.isExpanded){
			EditorGUI.indentLevel = 0;
			EditorGUIUtility.labelWidth = 0.1f;
			for (int y = 0; y < TileAssetBlock.MAX_HEIGHT; y++){
				for (int x = 0; x < TileAssetBlock.MAX_WIDTH; x++){
					DrawField(TileAssetBlock.VARIABLE_NAMES[y * TileAssetBlock.MAX_WIDTH + x], x, y, property, position);
				}
			}
		}

		EditorGUI.EndProperty();
	}

	void DrawField(string fieldName, int x, int y, SerializedProperty ownerProperty, Rect ownerPosition) {
		SerializedProperty prop = ownerProperty.FindPropertyRelative(fieldName);

		float width = GetChildPropertyWidth(ownerPosition.width);
		float height = GetChildPropertyHeight(prop);

		Vector2 pos = ownerPosition.position;
		pos.x += x * width + x * PADDING.x;
		pos.y += (y + 1) * height + (y + 1) * PADDING.y;

		Rect rect = new Rect(pos.x, pos.y, width, height);
		EditorGUI.PropertyField(rect, prop, includeChildren: true);
	}

	float GetChildPropertyWidth(float ownerWidth) { 
		return (ownerWidth / 3.0f) - PADDING.x;
	}

	float GetChildPropertyHeight(SerializedProperty childProp) { 
		return EditorGUI.GetPropertyHeight(childProp, includeChildren: true);
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
		if(property.isExpanded) {
			return PADDING.y * 2 + (EditorGUI.GetPropertyHeight(property, label, includeChildren: false) + PADDING.y) * TileAssetBlock.MAX_HEIGHT;
		}
		else{
			return EditorGUI.GetPropertyHeight(property, label, includeChildren: false);
		}
	}
}