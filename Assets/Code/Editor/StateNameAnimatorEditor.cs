using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
//--------------------------------------------------------------------
//Shared inspector for the little animator components that map a "what am I doing" enum onto
//Animator Controller state names. Those names are whatever the Aseprite tags in the imported
//file are called, so instead of making you type them, this reads the states out of the assigned
//Animator Controller and offers them as dropdowns, plus a button that guesses all of them at
//once by keyword (player_idle -> Idle, and so on).
//Falls back to plain text fields when no controller is assigned yet.
//
//Subclasses just declare which properties to draw; all the machinery is here.
//--------------------------------------------------------------------
public abstract class StateNameAnimatorEditor : Editor
{
    protected struct StateSlot
    {
        public string m_PropertyName;
        public string[] m_Keywords;     //tried in order, first state containing one wins
    }

    //Object fields drawn above the state names (the owner component, the Animator)
    protected abstract string[] GetReferencePropertyNames();
    //State name fields, in the order they should appear
    protected abstract StateSlot[] GetSlots();

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        string[] references = GetReferencePropertyNames();
        for (int i = 0; i < references.Length; i++)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty(references[i]));
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("State Names", EditorStyles.boldLabel);

        List<string> states = GetControllerStates();
        if (states == null)
        {
            EditorGUILayout.HelpBox(
                "Assign an Animator that has an Animator Controller to pick states from a list. " +
                "Until then, type the Aseprite tag names by hand.",
                MessageType.Info);
        }

        StateSlot[] slots = GetSlots();
        for (int i = 0; i < slots.Length; i++)
        {
            SerializedProperty property = serializedObject.FindProperty(slots[i].m_PropertyName);
            if (states == null)
            {
                EditorGUILayout.PropertyField(property);
            }
            else
            {
                DrawStatePopup(property, states);
            }
        }

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(states == null))
        {
            if (GUILayout.Button("Match States To Controller"))
            {
                MatchStates(slots, states);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    //Dropdown of the controller's states. A name that isn't in the controller is kept rather
    //than silently replaced, and flagged, since that is exactly the typo worth seeing
    void DrawStatePopup(SerializedProperty a_Property, List<string> a_States)
    {
        List<string> options = new List<string>(a_States);
        string current = a_Property.stringValue;
        int index = options.IndexOf(current);
        if (index < 0)
        {
            options.Add(string.IsNullOrEmpty(current) ? "(none)" : current + "  (not in controller)");
            index = options.Count - 1;
        }

        string label = ObjectNames.NicifyVariableName(a_Property.name);
        int newIndex = EditorGUILayout.Popup(label, index, options.ToArray());
        if (newIndex != index && newIndex < a_States.Count)
        {
            a_Property.stringValue = a_States[newIndex];
        }
    }

    void MatchStates(StateSlot[] a_Slots, List<string> a_States)
    {
        for (int i = 0; i < a_Slots.Length; i++)
        {
            string match = FindStateByKeyword(a_States, a_Slots[i].m_Keywords);
            //No match leaves the current value alone rather than blanking it
            if (match != null)
            {
                serializedObject.FindProperty(a_Slots[i].m_PropertyName).stringValue = match;
            }
        }
    }

    static string FindStateByKeyword(List<string> a_States, string[] a_Keywords)
    {
        for (int i = 0; i < a_Keywords.Length; i++)
        {
            for (int j = 0; j < a_States.Count; j++)
            {
                if (a_States[j].ToLowerInvariant().Contains(a_Keywords[i]))
                {
                    return a_States[j];
                }
            }
        }
        return null;
    }

    //Top level states of layer 0, which is what Animator.HasState(0, StringToHash(name))
    //can actually match - states inside a sub state machine need their full path instead
    List<string> GetControllerStates()
    {
        Animator animator = serializedObject.FindProperty("m_Animator").objectReferenceValue as Animator;
        if (animator == null)
        {
            return null;
        }

        RuntimeAnimatorController runtimeController = animator.runtimeAnimatorController;
        AnimatorOverrideController overrideController = runtimeController as AnimatorOverrideController;
        if (overrideController != null)
        {
            runtimeController = overrideController.runtimeAnimatorController;
        }

        AnimatorController controller = runtimeController as AnimatorController;
        if (controller == null || controller.layers.Length == 0)
        {
            return null;
        }

        List<string> names = new List<string>();
        ChildAnimatorState[] childStates = controller.layers[0].stateMachine.states;
        for (int i = 0; i < childStates.Length; i++)
        {
            names.Add(childStates[i].state.name);
        }
        if (names.Count == 0)
        {
            return null;
        }
        names.Sort();
        return names;
    }
}
