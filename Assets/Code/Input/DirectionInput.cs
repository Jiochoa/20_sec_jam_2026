using UnityEngine;
using UnityEngine.InputSystem;
//--------------------------------------------------------------------
//Gets directional input (analogue stick, WASD, arrow keys) from a Unity Input System Vector2 action. Used by PlayerInput
//--------------------------------------------------------------------
public class DirectionInput
{
    public enum Direction
    {
        Neutral,
        Up,
        Down,
        Left,
        Right
    }
    public Vector2 m_RawInput;
    public Vector2 m_ClampedInput;
    public Direction m_Direction;

    InputAction m_Action;
    float m_DirectionThreshold;

    //Override
    bool m_IsBeingOverridden;
    Vector2 m_OverriddenInput;

    public bool GetIsBeingOverridden()
    {
        return m_IsBeingOverridden;
    }
    public void SetOverride(bool a_Override, Vector2 a_OverriddenInput = default(Vector2))
    {
        if (a_Override)
        {
            m_OverriddenInput = a_OverriddenInput;
        }
        m_IsBeingOverridden = a_Override;
    }

    public DirectionInput(float a_DirectionThreshold)
    {
        m_DirectionThreshold = a_DirectionThreshold;
    }

    public void SetAction(InputAction a_Action)
    {
        m_Action = a_Action;
    }

    public bool IsInThisDirection(Vector2 a_Direction)
    {
        float dot = Vector2.Dot(a_Direction, m_ClampedInput);
        if (dot >= m_DirectionThreshold)
        {
            return true;
        }
        return false;
    }

    public bool HasSurpassedThreshold()
    {
        return (m_ClampedInput.magnitude >= m_DirectionThreshold);
    }

	public void Update ()
    {
        if (m_IsBeingOverridden)
        {
            m_RawInput = m_OverriddenInput.normalized;
        }
        else
        {
            m_RawInput = (m_Action != null) ? m_Action.ReadValue<Vector2>() : Vector2.zero;
        }
        m_ClampedInput = (m_RawInput.magnitude > 1) ? m_RawInput.normalized : m_RawInput;

        if (Mathf.Abs(m_ClampedInput.x) > m_DirectionThreshold || Mathf.Abs(m_ClampedInput.y) > m_DirectionThreshold)
        {
            if (Mathf.Abs(m_ClampedInput.x) > Mathf.Abs(m_ClampedInput.y))
            {
                if (m_ClampedInput.x > 0)
                {
                    m_Direction = Direction.Right;
                }
                else
                {
                    m_Direction = Direction.Left;
                }
            }
            else
            {
                if (m_ClampedInput.y > 0)
                {
                    m_Direction = Direction.Up;
                }
                else
                {
                    m_Direction = Direction.Down;
                }
            }
        }
        else
        {
            m_Direction = Direction.Neutral;
        }
	}
}
