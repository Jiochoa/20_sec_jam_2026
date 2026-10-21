using UnityEngine;
using UnityEngine.InputSystem;
//--------------------------------------------------------------------
//ButtonInput registers keyboard or controller buttoninput to be used by PlayerInput
//Reads from a Unity Input System InputAction (resolved by PlayerInput via InputElement)
//--------------------------------------------------------------------

public class ButtonInput {
    //Legacy enums kept so old serialized prefab data (InputElement) still loads; no longer used at runtime
    public enum UnityInputType
    {
        Button,
        Axis,
        ButtonAndAxis
    }
    public enum UnityAxisRecognition
    {
        PositiveOrNegative,
        PositiveOnly,
        NegativeOnly
    }
    public bool m_IsPressed;
    public bool m_WasJustPressed;
    public bool m_WasJustReleased;

    InputAction m_Action;

    bool m_IsActuallyPressed;
    bool m_IsActuallyJustPressed;
    bool m_IsActuallyJustReleased;

    //Override
    bool m_IsBeingOverridden;

    public bool GetIsBeingOverridden()
    {
        return m_IsBeingOverridden;
    }
    public void SetOverride(bool a_Override, bool a_Input = false)
    {
        if (a_Override)
        {
            HandleButtonUpdate(a_Input);
        }
        m_IsBeingOverridden = a_Override;
    }

    public void SetAction(InputAction a_Action)
    {
        m_Action = a_Action;
    }

    public void Update()
    {
        if (m_IsBeingOverridden)
        {
            return;
        }
        if (m_Action == null)
        {
            return;
        }
        //WasPressed/ReleasedThisFrame also handle axis-type controls (gamepad triggers) via the press threshold
        if (m_Action.WasPressedThisFrame())
        {
            HandleButtonUpdate(true);
        }
        if (m_Action.WasReleasedThisFrame())
        {
            HandleButtonUpdate(false);
        }
    }

    public void FixedUpdate()
    {
        m_WasJustPressed = false;
        m_WasJustReleased = false;
        if (m_IsActuallyJustPressed)
        {
            m_WasJustPressed = true;
            m_IsActuallyJustPressed = false;
            m_IsPressed = true;
        }
        else if (m_IsActuallyJustReleased)//Stagger just pressed and released if they happened within a single fixed update
        {
            m_WasJustReleased = true;
            m_IsActuallyJustReleased = false;
            m_IsPressed = false;
        }
        else
        {
            m_IsPressed = m_IsActuallyPressed;
        }
    }

    void HandleButtonUpdate(bool a_Pressed)
    {
        if (a_Pressed && !m_IsActuallyPressed)
        {
            m_IsActuallyJustPressed = true;
        }
        else if (!a_Pressed && m_IsActuallyPressed)
        {
            m_IsActuallyJustReleased = true;
        }
        m_IsActuallyPressed = a_Pressed;
    }
}
