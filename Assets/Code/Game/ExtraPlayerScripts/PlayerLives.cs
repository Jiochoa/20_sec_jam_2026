using UnityEngine;
using UnityEngine.Events;
using System.Collections;
//--------------------------------------------------------------------
//Put this on the player character (next to the CharacterControllerBase).
//Tracks the player's lives, grants brief invulnerability (with a blink) after a hit,
//and when all lives are gone respawns the player at the current checkpoint with
//fresh lives. Hook UI up to the events.
//--------------------------------------------------------------------
public class PlayerLives : MonoBehaviour
{
    [SerializeField] int m_MaxLives = 3;
    [SerializeField] float m_InvulnerabilityTime = 1.5f;
    [SerializeField] float m_BlinkInterval = 0.12f;

    [Header("Knockback")]
    [Tooltip("Horizontal speed the player is pushed away from the damage source with")]
    [SerializeField] float m_KnockbackSpeed = 7.0f;
    [Tooltip("Upward speed added to the push, so the player briefly leaves the ground")]
    [SerializeField] float m_KnockbackUpwardSpeed = 4.0f;

    public UnityEvent<int> OnLivesChanged;
    public UnityEvent OnAllLivesLost;

    public int m_CurrentLives;
    float m_InvulnerableUntil;
    Renderer[] m_Renderers;
    CharacterControllerBase m_CharacterController;

    void Awake()
    {
        m_CurrentLives = m_MaxLives;
        m_Renderers = GetComponentsInChildren<Renderer>();
        m_CharacterController = GetComponent<CharacterControllerBase>();
    }

    public int GetCurrentLives()
    {
        return m_CurrentLives;
    }

    public int GetMaxLives()
    {
        return m_MaxLives;
    }

    public bool IsInvulnerable()
    {
        return (Time.time < m_InvulnerableUntil);
    }

    public void TakeDamage(int a_Amount)
    {
        TakeDamage(a_Amount, transform.position);
    }

    //Pass where the damage came from to push the player away from it
    public void TakeDamage(int a_Amount, Vector3 a_DamageSourcePosition)
    {
        if (IsInvulnerable() || m_CurrentLives <= 0)
        {
            return;
        }
        ApplyKnockback(a_DamageSourcePosition);
        m_CurrentLives = Mathf.Max(0, m_CurrentLives - a_Amount);
        m_InvulnerableUntil = Time.time + m_InvulnerabilityTime;
        if (OnLivesChanged != null)
        {
            OnLivesChanged.Invoke(m_CurrentLives);
        }
        if (m_CurrentLives <= 0)
        {
            SoundManager.Play(SoundId.PlayerDeath);
            HandleAllLivesLost();
        }
        else
        {
            SoundManager.Play(SoundId.PlayerHurt);
            StopAllCoroutines();
            StartCoroutine(BlinkRoutine());
        }
    }

    void ApplyKnockback(Vector3 a_SourcePosition)
    {
        if (m_CharacterController == null)
        {
            return;
        }
        ControlledCollider controlledCollider = m_CharacterController.GetCollider();
        if (controlledCollider == null)
        {
            return;
        }
        //Push horizontally away from the source; when the source is exactly on top of us, default to pushing right
        float direction = (transform.position.x >= a_SourcePosition.x) ? 1.0f : -1.0f;
        controlledCollider.SetVelocity(new Vector2(direction * m_KnockbackSpeed, m_KnockbackUpwardSpeed));
    }

    void HandleAllLivesLost()
    {
        if (OnAllLivesLost != null)
        {
            OnAllLivesLost.Invoke();
        }
        //Jam default: respawn at the current checkpoint with fresh lives
        m_CurrentLives = m_MaxLives;
        if (OnLivesChanged != null)
        {
            OnLivesChanged.Invoke(m_CurrentLives);
        }
        InSceneLevelSwitcher switcher = InSceneLevelSwitcher.Get();
        if (switcher != null)
        {
            switcher.Respawn();
        }
    }

    IEnumerator BlinkRoutine()
    {
        bool visible = true;
        while (IsInvulnerable())
        {
            visible = !visible;
            SetRenderersEnabled(visible);
            yield return new WaitForSeconds(m_BlinkInterval);
        }
        SetRenderersEnabled(true);
    }

    void SetRenderersEnabled(bool a_Enabled)
    {
        for (int i = 0; i < m_Renderers.Length; i++)
        {
            if (m_Renderers[i] != null)
            {
                m_Renderers[i].enabled = a_Enabled;
            }
        }
    }
}
