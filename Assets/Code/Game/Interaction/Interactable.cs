using UnityEngine;
using UnityEngine.Events;
//--------------------------------------------------------------------
//Put this on anything the player can interact with: signs, the TV, the litter box, a door.
//Needs a collider on the same object (a trigger is fine - the module queries triggers).
//
//InteractionType.Locked  - the player is held in place for the whole conversation
//InteractionType.Free    - the player keeps walking; the dialogue closes if they wander off
//InteractionType.Instant - no dialogue at all, just fires OnInteractionEnded once on press
//                          (switches, pickups, anything one-shot)
//
//InteractingModule drives all of this and feeds the text to DialogueUI. The UnityEvents are
//there for hooking up extra reactions in the Inspector (open a door, play an animation).
//--------------------------------------------------------------------
public class Interactable : MonoBehaviour
{
    public enum InteractionType
    {
        Locked,
        Free,
        Instant
    }

    [SerializeField] InteractionType m_Type = InteractionType.Locked;

    [Tooltip("One element per page of dialogue, shown in order. Leave empty for Instant interactables")]
    [TextArea(2, 5)][SerializeField] string[] m_DialoguePages = new string[0];

    [Tooltip("How far the player can wander before a Free interaction closes itself. Unused by the other types")]
    [SerializeField] float m_InteractionRadius = 2.0f;

    [Tooltip("Optional portrait shown in the dialogue box. Leave empty for objects - the portrait area disappears")]
    [SerializeField] Sprite m_Portrait;

    [Tooltip("Optional name shown above the text. Leave empty to hide the name line")]
    [SerializeField] string m_SpeakerName;

    [Tooltip("Only lets the player interact once, ever. For one-off story beats and pickups")]
    [SerializeField] bool m_SingleUse = false;

    [Header("Prompt")]
    [Tooltip("Shown while the player is close but still out of reach - the '?' sprite")]
    [SerializeField] Sprite m_NearbySprite;
    [Tooltip("Shown once the player is close enough to actually interact - the interact key sprite")]
    [SerializeField] Sprite m_InReachSprite;
    [Tooltip("How close the player gets before the '?' appears. The switch to the key sprite happens at the InteractingModule's own detection radius, so the two can never disagree")]
    [SerializeField] float m_NoticeRadius = 5.0f;
    [Tooltip("Where the prompt sits relative to this object - usually straight above it")]
    [SerializeField] Vector3 m_PromptOffset = new Vector3(0.0f, 1.2f, 0.0f);
    [SerializeField] float m_PromptScale = 1.0f;
    [Tooltip("Optional. Leave empty and a prompt child is built in code from the sprites above; assign one to place and animate it by hand instead")]
    [SerializeField] SpriteRenderer m_PromptRenderer;
    [SerializeField] int m_PromptSortingOrder = 100;

    [Space]
    [Tooltip("Fired for every page as it is shown, with that page's text")]
    public UnityEvent<string> OnPageShown;
    [Tooltip("Fired when the interaction finishes - last page read, player walked away, or an Instant press")]
    public UnityEvent OnInteractionEnded;

    //How close the player is, as far as the prompt is concerned
    enum PromptState
    {
        None,       //Too far away, or there is nothing left to interact with
        Nearby,     //Close enough to notice: the '?'
        InReach     //Close enough to press the button: the key
    }

    int m_CurrentPage;
    bool m_Used;
    PromptState m_PromptState = PromptState.None;

    //One player, so every interactable shares the lookup
    static Transform s_Player;
    static int s_PlayerSearchFrame = -1;

    public InteractionType GetInteractionType() { return m_Type; }
    public float GetInteractionRadius() { return m_InteractionRadius; }
    public Sprite GetPortrait() { return m_Portrait; }
    public string GetSpeakerName() { return m_SpeakerName; }

    //False once a single-use interactable has been spent, so the module stops offering it
    public bool CanInteract()
    {
        return !(m_SingleUse && m_Used);
    }

    public string GetCurrentPageText()
    {
        if (m_DialoguePages == null || m_DialoguePages.Length == 0)
        {
            return "";
        }
        return m_DialoguePages[Mathf.Clamp(m_CurrentPage, 0, m_DialoguePages.Length - 1)];
    }

    public void StartInteraction()
    {
        m_Used = true;
        if (m_Type == InteractionType.Instant)
        {
            EndInteraction();
            return;
        }
        m_CurrentPage = 0;
        ShowCurrentPage();
    }

    //Steps to the next page. True while there is more to read, false once the last page is done
    public bool AdvancePage()
    {
        m_CurrentPage++;
        if (m_DialoguePages == null || m_CurrentPage >= m_DialoguePages.Length)
        {
            EndInteraction();
            return false;
        }
        ShowCurrentPage();
        return true;
    }

    public void EndInteraction()
    {
        if (OnInteractionEnded != null)
        {
            OnInteractionEnded.Invoke();
        }
    }

    // ---- Prompt ----

    void Awake()
    {
        EnsurePromptRenderer();
        if (m_PromptRenderer != null)
        {
            m_PromptRenderer.enabled = false;
        }
    }

    //Built in code unless one was assigned, so setting an object up is just "drop in two sprites"
    void EnsurePromptRenderer()
    {
        if (m_PromptRenderer != null || (m_NearbySprite == null && m_InReachSprite == null))
        {
            return;
        }
        GameObject promptObject = new GameObject("Prompt");
        promptObject.transform.SetParent(transform, false);
        promptObject.transform.localPosition = m_PromptOffset;
        promptObject.transform.localScale = Vector3.one * m_PromptScale;
        m_PromptRenderer = promptObject.AddComponent<SpriteRenderer>();
        m_PromptRenderer.sortingOrder = m_PromptSortingOrder;
    }

    void Update()
    {
        if (m_PromptRenderer == null)
        {
            return;
        }
        PromptState state = GetPromptState();
        if (state == m_PromptState)
        {
            return; //Only touch the renderer when the state actually changes
        }
        m_PromptState = state;
        Sprite sprite = (state == PromptState.InReach) ? m_InReachSprite : m_NearbySprite;
        //A state whose sprite has not been made yet simply shows nothing
        m_PromptRenderer.enabled = (state != PromptState.None && sprite != null);
        if (sprite != null)
        {
            m_PromptRenderer.sprite = sprite;
        }
    }

    PromptState GetPromptState()
    {
        //Nothing left to say, or the player is already reading something
        if (!CanInteract() || (DialogueUI.Instance != null && DialogueUI.Instance.IsOpen))
        {
            return PromptState.None;
        }
        Transform player = GetPlayer();
        if (player == null)
        {
            return PromptState.None;
        }
        float distance = Vector3.Distance(transform.position, player.position);
        //The module owns the reach distance; the notice radius only matters outside it
        float reach = InteractingModule.ReachRadius;
        if (distance <= reach)
        {
            return PromptState.InReach;
        }
        return (distance <= Mathf.Max(m_NoticeRadius, reach)) ? PromptState.Nearby : PromptState.None;
    }

    //The player is the one with the capsule controller - the same test LevelFadeTransition uses.
    //Searched at most once per frame in total, so a scene with no player (the menu) stays cheap
    static Transform GetPlayer()
    {
        if (s_Player == null && s_PlayerSearchFrame != Time.frameCount)
        {
            s_PlayerSearchFrame = Time.frameCount;
            ControlledCapsuleCollider player = FindFirstObjectByType<ControlledCapsuleCollider>();
            s_Player = (player != null) ? player.transform : null;
        }
        return s_Player;
    }

    void ShowCurrentPage()
    {
        if (m_DialoguePages == null || m_DialoguePages.Length == 0)
        {
            return;
        }
        if (OnPageShown != null)
        {
            OnPageShown.Invoke(GetCurrentPageText());
        }
    }

    void OnDrawGizmosSelected()
    {
        //Where the '?' appears
        Gizmos.color = new Color(1.0f, 0.85f, 0.45f, 1.0f);
        Gizmos.DrawWireSphere(transform.position, m_NoticeRadius);
        //Where the key sprite takes over. Editor-time this is the module prefab's default,
        //since no module exists to publish the real one until play
        Gizmos.color = new Color(0.4f, 0.9f, 1.0f, 1.0f);
        Gizmos.DrawWireSphere(transform.position, InteractingModule.ReachRadius);
        //Only meaningful for Free interactions, but harmless to show for all of them
        Gizmos.color = new Color(0.4f, 0.9f, 1.0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, m_InteractionRadius);
    }
}
