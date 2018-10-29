using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(TileAssetBlock))]
public class TileAssetBlockDrawer : PropertyDrawer {

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

		position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

		EditorGUI.BeginProperty(position, label, property);
		EditorGUI.indentLevel = 0;
		EditorGUIUtility.labelWidth = 0.1f;

		Float2 padding = new Float2(20.0f, 10.0f);
		float fieldWidth = (position.width / 3.0f) - padding.x;
		float fieldHeight = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("X0Y0"));

		Handles.BeginGUI();
		Vector2 linePos = new Vector2();
		for (int y = 0; y < TileAssetBlock.MAX_HEIGHT; y++){
			float fieldHeightTotal = fieldHeight * y;
			float paddingTotalY = padding.y * Mathf.Max(y - 1 + 0.5f, 0.0f);
			linePos.x = 0.0f;
			linePos.y = position.y + fieldHeightTotal + paddingTotalY;
			Handles.DrawLine(linePos, new Vector2(position.x + position.width, linePos.y));

			for (int x = 0; x < TileAssetBlock.MAX_WIDTH; x++){
				float fieldWidthTotal = fieldWidth * x;
				float paddingTotalX = padding.x * Mathf.Max(x - 1 + 0.5f, 0.0f);
				linePos.x = position.x + fieldWidthTotal + paddingTotalX;
				Handles.DrawLine(linePos, new Vector2(linePos.x, position.y + position.height));
			}
		}
		Handles.EndGUI();

		DrawField("X0Y0", 0, 0, property, position, fieldWidth, padding);
		DrawField("X1Y0", 1, 0, property, position, fieldWidth, padding);
		DrawField("X2Y0", 2, 0, property, position, fieldWidth, padding);

		DrawField("X0Y1", 0, 1, property, position, fieldWidth, padding);
		DrawField("X1Y1", 1, 1, property, position, fieldWidth, padding);
		DrawField("X2Y1", 2, 1, property, position, fieldWidth, padding);

		DrawField("X0Y2", 0, 2, property, position, fieldWidth, padding);
		DrawField("X1Y2", 1, 2, property, position, fieldWidth, padding);
		DrawField("X2Y2", 2, 2, property, position, fieldWidth, padding);
		
		DrawField("X0Y3", 0, 3, property, position, fieldWidth, padding);
		DrawField("X1Y3", 1, 3, property, position, fieldWidth, padding);
		DrawField("X2Y3", 2, 3, property, position, fieldWidth, padding);
		
		DrawField("X0Y4", 0, 4, property, position, fieldWidth, padding);
		DrawField("X1Y4", 1, 4, property, position, fieldWidth, padding);
		DrawField("X2Y4", 2, 4, property, position, fieldWidth, padding);

		EditorGUI.EndProperty();
	}

	void DrawField(string fieldName, int x, int y, SerializedProperty ownerProperty, Rect ownerPosition, float width, Float2 padding) {
		SerializedProperty prop = ownerProperty.FindPropertyRelative(fieldName);
		float height = EditorGUI.GetPropertyHeight(prop, includeChildren: true);

		Float2 pos = new Float2(ownerPosition.x, ownerPosition.y);
		pos.x += x * width + x * padding.x;
		pos.y += y * height + y * padding.y;;

		Rect rect = new Rect(pos.x, pos.y, width, height);
		EditorGUI.PropertyField(rect, prop, includeChildren: true);
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
		float totalHeight = EditorGUI.GetPropertyHeight(property, label, true) + EditorGUIUtility.standardVerticalSpacing;

		while (property.NextVisible(true)){
			totalHeight += EditorGUI.GetPropertyHeight(property, label, true) + EditorGUIUtility.standardVerticalSpacing;
		}

		return totalHeight;
	}
}