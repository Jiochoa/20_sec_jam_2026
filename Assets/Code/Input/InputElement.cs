using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
[System.Serializable]
public class InputElement
{
    public enum InputType
    {
        Button,
        Direction
    }
    public string m_Name;
    public InputType m_InputType;
    //Legacy Input Manager fields, kept so old serialized prefab data still loads; no longer used at runtime
    public ButtonInput.UnityInputType m_UnityInputType;
    public ButtonInput.UnityAxisRecognition m_UnityAxisRecognition;
    public string m_ButtonName;
    public string m_HorizontalAxisName;
    public string m_VerticalAxisName;
    public float m_DirectionThreshold;
    [HideInInspector]
    public bool m_IsFoldingOut;

    ButtonInput m_ButtonInput;
    DirectionInput m_DirectionInput;

    //Called by PlayerInput on Awake. Finds the InputAction matching m_Name in the actions asset
    public void ResolveAction(InputActionAsset a_Asset)
    {
        InputAction action = a_Asset.FindAction(m_Name);
        if (action == null)
        {
            Debug.LogError("Input '" + m_Name + "' has no matching action in InputActionAsset '" + a_Asset.name + "'. Add an action with that name to the asset.");
            return;
        }
        switch (m_InputType)
        {
            case InputType.Button:
                GetButtonInput().SetAction(action);
                break;
            case InputType.Direction:
                GetDirectionInput().SetAction(action);
                break;
        }
    }

    public ButtonInput GetButtonInput()
    {
        if (m_ButtonInput == null)
        {
            m_ButtonInput = new ButtonInput();
        }
        return m_ButtonInput;
    }

    public DirectionInput GetDirectionInput()
    {
        if (m_DirectionInput == null)
        {
            m_DirectionInput = new DirectionInput(m_DirectionThreshold);
        }
        return m_DirectionInput;
    }

    public void Update()
    {
        switch (m_InputType)
        {
            case InputType.Button:
                GetButtonInput().Update();
            break;
            case InputType.Direction:
                GetDirectionInput().Update();
            break;
        }
    }

    public void FixedUpdate()
    {
        switch (m_InputType)
        {
            case InputType.Button:
                GetButtonInput().FixedUpdate();
            break;
        }
    }
}
