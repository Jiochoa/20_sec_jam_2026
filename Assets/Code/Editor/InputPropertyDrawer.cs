using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(InputElement))]
public class InputPropertyDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty a_Property, GUIContent a_Label)
    {
        bool foldOut = a_Property.FindPropertyRelative("m_IsFoldingOut").boolValue;
        int lineAmount = 1;
        if (foldOut)
        {
            lineAmount += 2;
            InputElement.InputType type = (InputElement.InputType)a_Property.FindPropertyRelative("m_InputType").enumValueIndex;
            if (type == InputElement.InputType.Direction)
            {
                lineAmount += 1;
            }
        }
        return (float)(lineAmount * EditorHelp.c_LineHeight + ((foldOut) ? 4 : 0));
    }

    public override void OnGUI(Rect position, SerializedProperty a_Property, GUIContent a_Label)
    {
        position.height = EditorHelp.c_LineHeight;
        EditorGUI.BeginProperty(position, a_Label, a_Property);

        SerializedProperty foldOutProperty = a_Property.FindPropertyRelative("m_IsFoldingOut");
        string nameString = a_Property.FindPropertyRelative("m_Name").stringValue;
        if (nameString == "") nameString = "<InputName missing>";

        foldOutProperty.boolValue = EditorGUI.Foldout(position, foldOutProperty.boolValue, nameString, true);

        Rect pos = position;

        if (foldOutProperty.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorHelp.PropertyDrawerLineWithVar(a_Property, "m_Name", ref pos, "Name of this input, as referenced in code. Must match the name of an Action in the Input Actions asset (Project Settings > Input System Package)");
            EditorHelp.PropertyDrawerLineWithVar(a_Property, "m_InputType", ref pos,"Is this input a button (or key), or directional (analogue stick, arrow keys, WASD)? The matching Action should be a Button or a Vector2 Value respectively");

            InputElement.InputType type = (InputElement.InputType)a_Property.FindPropertyRelative("m_InputType").enumValueIndex;
            if (type == InputElement.InputType.Direction)
            {
                EditorHelp.PropertyDrawerLineWithVar(a_Property, "m_DirectionThreshold", ref pos, "In the case of analogue input, how far should the input be pushed before a direction is recognized? For example: how far should an analogue stick be pulled down before it registers as a crouch command");
            }
            EditorGUI.indentLevel--;
        }
        
        EditorGUI.EndProperty();
    }
}
