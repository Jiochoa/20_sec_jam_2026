using UnityEngine;
//--------------------------------------------------------------------
//Lets the player interact with any nearby Interactable and reads its dialogue out into
//DialogueUI. Ported from the Metroidvania project's InteractingModule.
//
//Three behaviours, chosen per Interactable:
//  Locked  - the module takes over movement and holds the player still for the conversation.
//            This is the only one that becomes the active ability (IsApplicable is true then).
//  Free    - the player keeps control; the page turns on the interact button and the box
//            closes by itself if they walk out of the Interactable's radius.
//  Instant - no dialogue, just fires the Interactable's event once.
//Free and Instant are handled passively in InactiveUpdateModule, so they never fight the
//movement abilities - the standard trick for a module that should never take over movement.
//
//Input: prefers an input named m_InteractInputName on the character's PlayerInput; with none
//set up it falls back to the action of that name in the project-wide Input Actions asset
//("Interact" is already in there, bound to E), so no wiring is needed to try it out.
//
//Priority: above walking so Locked can freeze the player, below anything that must be able
//to interrupt a conversation.
//--------------------------------------------------------------------
public class InteractingModule : GroundedControllerAbilityModule
{
    [Header("Input")]
    [SerializeField] string m_InteractInputName = "Interact";

    [Header("Detection")]
    [Tooltip("How close the player has to be to start an interaction")]
    [SerializeField] float m_DetectionRadius = 2.5f;
    [Tooltip("Where to look for Interactables. Interactable colliders are usually triggers, which are queried explicitly")]
    [SerializeField] LayerMask m_InteractableMask = ~0;

    [Header("Locked - movement freeze")]
    [Tooltip("How hard the player is brought to a stop when a Locked conversation starts")]
    [SerializeField] float m_HorizontalStoppingForce = 25.0f;
    [Tooltip("Gravity applied while frozen, so a conversation started in mid-air still lands")]
    [SerializeField] float m_Gravity = -20.0f;

    [Header("Debug")]
    [SerializeField] bool m_DebugMode = false;

    readonly Collider[] m_OverlapBuffer = new Collider[8];

    //Closest interactable this tick, refreshed in InactiveUpdateModule
    Interactable m_NearbyInteractable;
    Interactable m_PrevNearbyInteractable;

    //Locked conversation (this module is the active ability while one runs)
    Interactable m_ActiveLockedInteractable;
    bool m_LockedInteractionDone;

    //Free conversation (runs passively; the player keeps walking)
    Interactable m_ActiveFreeInteractable;

    //Input resolution, same pattern as CatTossModule
    bool m_InputResolved;
    bool m_UsePlayerInput;
    UnityEngine.InputSystem.InputAction m_FallbackAction;
    //A press lasts a whole render frame, which can span two fixed ticks. Whoever acts on a
    //press stamps this, so one button press can never turn two pages
    int m_ConsumedFrame = -1;

    //Interactables read this to decide when to swap their prompt to the "press this" sprite.
    //Published from the live module so the prompt can never disagree with the range that
    //actually lets the player interact
    public static float ReachRadius { get; private set; } = 2.5f;

    public override void InitModule(CharacterControllerBase a_CharacterController)
    {
        base.InitModule(a_CharacterController);
        ReachRadius = m_DetectionRadius;
    }

    public override bool IsApplicable()
    {
        ResolveInputOnce();
        if (!m_InputResolved)
        {
            return false;
        }
        //Stay active until the conversation is finished
        if (m_ActiveLockedInteractable != null && !m_LockedInteractionDone)
        {
            return true;
        }
        //Start one. Note this only queries the press - StartModuleImpl is what consumes it
        return (InteractPressed() &&
            m_NearbyInteractable != null &&
            m_NearbyInteractable.GetInteractionType() == Interactable.InteractionType.Locked);
    }

    protected override void StartModuleImpl()
    {
        //A Free conversation cannot carry on underneath a Locked one - InactiveUpdateModule
        //stops running the moment this module becomes the active ability
        if (m_ActiveFreeInteractable != null)
        {
            m_ActiveFreeInteractable.EndInteraction();
            m_ActiveFreeInteractable = null;
        }
        m_ActiveLockedInteractable = m_NearbyInteractable;
        m_LockedInteractionDone = false;
        //Eat the press that opened the box so it does not also skip past the first page
        ConsumeInteractPress();

        m_ActiveLockedInteractable.StartInteraction();

        //Pin the facing direction by overriding Move, or the sprite flips while the player talks
        DirectionInput moveInput = GetDirInput("Move");
        if (moveInput != null)
        {
            moveInput.SetOverride(true, Vector2.zero);
        }
        ShowPage(m_ActiveLockedInteractable);
    }

    public override void FixedUpdateModule()
    {
        //The interactable was destroyed mid-conversation
        if (m_ActiveLockedInteractable == null)
        {
            m_LockedInteractionDone = true;
            return;
        }

        //Hold the player in place: bleed off horizontal speed, keep gravity so they still land
        Vector2 velocity = m_ControlledCollider.GetVelocity();
        float deceleration = m_HorizontalStoppingForce * Time.fixedDeltaTime;
        velocity.x = (Mathf.Abs(velocity.x) > deceleration) ? velocity.x - Mathf.Sign(velocity.x) * deceleration : 0.0f;
        if (!m_ControlledCollider.IsGrounded())
        {
            velocity.y += m_Gravity * Time.fixedDeltaTime;
        }
        else
        {
            velocity.y = Mathf.Max(0.0f, velocity.y);
        }
        m_ControlledCollider.UpdateWithVelocity(velocity);

        if (!ConsumeInteractPress())
        {
            return;
        }
        //Still typing: dump the rest of the page instead of turning it
        if (DialogueUI.Instance != null && DialogueUI.Instance.IsTyping)
        {
            DialogueUI.Instance.CompleteTyping();
            return;
        }
        if (!m_ActiveLockedInteractable.AdvancePage())
        {
            m_LockedInteractionDone = true;
        }
        else
        {
            ShowPage(m_ActiveLockedInteractable);
        }
    }

    public override void InactiveUpdateModule()
    {
        ResolveInputOnce();
        if (!m_InputResolved)
        {
            return;
        }
        ScanForNearbyInteractable();
        UpdateFreeInteraction();
        TryStartFreeOrInstantInteraction();
    }

    protected override void EndModuleImpl()
    {
        //The Interactable already ended itself, either through the last AdvancePage or the
        //null-safety path above
        m_ActiveLockedInteractable = null;
        m_LockedInteractionDone = false;

        DirectionInput moveInput = GetDirInput("Move");
        if (moveInput != null)
        {
            moveInput.SetOverride(false, Vector2.zero);
        }
        if (DialogueUI.Instance != null)
        {
            DialogueUI.Instance.Hide();
        }
    }

    public override bool CanEnd()
    {
        //Block anything else from yanking the player out of a conversation mid-sentence
        return m_LockedInteractionDone;
    }

    public override string GetSpriteState()
    {
        return "Interact";
    }

    // ---- Interaction handling ----

    void ScanForNearbyInteractable()
    {
        m_NearbyInteractable = null;
        Vector3 position = m_CharacterControllerBase.transform.position;
        //Interactables are usually triggers, and the project has 'Queries hit triggers' off
        int hitCount = Physics.OverlapSphereNonAlloc(position, m_DetectionRadius, m_OverlapBuffer, m_InteractableMask, QueryTriggerInteraction.Collide);

        float closestDistance = float.MaxValue;
        for (int i = 0; i < hitCount; i++)
        {
            Interactable candidate = m_OverlapBuffer[i].GetComponentInParent<Interactable>();
            if (candidate == null || !candidate.CanInteract())
            {
                continue;
            }
            float distance = Vector3.Distance(position, candidate.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                m_NearbyInteractable = candidate;
            }
        }

        if (m_DebugMode && m_NearbyInteractable != m_PrevNearbyInteractable)
        {
            if (m_NearbyInteractable != null)
            {
                Debug.Log("InteractingModule: '" + m_NearbyInteractable.name + "' (" + m_NearbyInteractable.GetInteractionType() + ") in range.", m_NearbyInteractable);
            }
            else
            {
                Debug.Log("InteractingModule: nothing in range.");
            }
            m_PrevNearbyInteractable = m_NearbyInteractable;
        }
    }

    void TryStartFreeOrInstantInteraction()
    {
        if (m_ActiveFreeInteractable != null || m_NearbyInteractable == null)
        {
            return;
        }
        Interactable.InteractionType type = m_NearbyInteractable.GetInteractionType();
        //Locked is not ours to start here - IsApplicable does that, and it has already seen this press
        if (type == Interactable.InteractionType.Locked)
        {
            return;
        }
        if (!ConsumeInteractPress())
        {
            return;
        }

        if (type == Interactable.InteractionType.Instant)
        {
            m_NearbyInteractable.StartInteraction(); //Fires and ends in one go, no dialogue
            return;
        }
        m_ActiveFreeInteractable = m_NearbyInteractable;
        m_ActiveFreeInteractable.StartInteraction();
        ShowPage(m_ActiveFreeInteractable);
    }

    void UpdateFreeInteraction()
    {
        if (m_ActiveFreeInteractable == null)
        {
            return;
        }
        //Walked off: close the box and let the interactable know
        float distance = Vector3.Distance(m_CharacterControllerBase.transform.position, m_ActiveFreeInteractable.transform.position);
        if (distance > m_ActiveFreeInteractable.GetInteractionRadius())
        {
            m_ActiveFreeInteractable.EndInteraction();
            EndFreeInteraction();
            return;
        }

        if (!ConsumeInteractPress())
        {
            return;
        }
        if (DialogueUI.Instance != null && DialogueUI.Instance.IsTyping)
        {
            DialogueUI.Instance.CompleteTyping();
            return;
        }
        if (!m_ActiveFreeInteractable.AdvancePage())
        {
            EndFreeInteraction();
        }
        else
        {
            ShowPage(m_ActiveFreeInteractable);
        }
    }

    void EndFreeInteraction()
    {
        m_ActiveFreeInteractable = null;
        if (DialogueUI.Instance != null)
        {
            DialogueUI.Instance.Hide();
        }
    }

    void ShowPage(Interactable a_Interactable)
    {
        DialogueUI.Get().ShowPage(a_Interactable.GetCurrentPageText(), a_Interactable.GetPortrait(), a_Interactable.GetSpeakerName());
    }

    // ---- Input ----

    //PlayerInput registers itself on the character in Awake, so this waits rather than resolving in InitModule
    void ResolveInputOnce()
    {
        if (m_InputResolved)
        {
            return;
        }
        PlayerInput playerInput = m_CharacterControllerBase.GetPlayerInput();
        if (playerInput == null)
        {
            return;
        }
        m_InputResolved = true;
        m_UsePlayerInput = playerInput.DoesInputExist(m_InteractInputName);
        if (m_UsePlayerInput)
        {
            return;
        }
        UnityEngine.InputSystem.InputActionAsset asset = UnityEngine.InputSystem.InputSystem.actions;
        m_FallbackAction = (asset != null) ? asset.FindAction(m_InteractInputName) : null;
        if (m_FallbackAction != null)
        {
            m_FallbackAction.Enable();
        }
        else
        {
            Debug.LogWarning("InteractingModule: no input named '" + m_InteractInputName + "' on the PlayerInput and no matching action in the Input Actions asset. Interacting is disabled.");
        }
    }

    //Query only - safe to call from IsApplicable, which runs more than once per tick
    bool InteractPressed()
    {
        if (m_ConsumedFrame == Time.frameCount)
        {
            return false;
        }
        if (m_UsePlayerInput)
        {
            ButtonInput input = GetButtonInput(m_InteractInputName);
            return (input != null && input.m_WasJustPressed);
        }
        return (m_FallbackAction != null && m_FallbackAction.WasPressedThisFrame());
    }

    //Takes the press, so nothing else can act on it this frame
    bool ConsumeInteractPress()
    {
        if (!InteractPressed())
        {
            return false;
        }
        m_ConsumedFrame = Time.frameCount;
        return true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.9f, 1.0f, 1.0f);
        Vector3 center = (m_CharacterControllerBase != null) ? m_CharacterControllerBase.transform.position : transform.position;
        Gizmos.DrawWireSphere(center, m_DetectionRadius);
    }
}
