using System.Collections;
using UnityEngine;
using UnityEngine.UI;
//--------------------------------------------------------------------
//The dialogue box InteractingModule writes into. One page of text at a time, revealed
//with a typewriter effect, with an optional portrait and speaker name.
//
//Nothing has to be placed in the scene: like ScreenFader, DialogueUI.Get() builds its own
//Canvas and a plain dark box if there is none. Drop this component on a hand-built UI and
//assign the references below to use that instead - any reference left empty is skipped, so
//a box with no portrait or no speaker bar works fine.
//
//Text is legacy UnityEngine.UI.Text, not TextMeshPro, to match the rest of the project's UI.
//--------------------------------------------------------------------
public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance { get; private set; }

    //Self-bootstrapping, so an Interactable works in a scene with no dialogue UI set up
    public static DialogueUI Get()
    {
        if (Instance == null)
        {
            Instance = FindFirstObjectByType<DialogueUI>();
            if (Instance == null)
            {
                GameObject go = new GameObject("DialogueUI");
                Instance = go.AddComponent<DialogueUI>();
            }
        }
        return Instance;
    }

    [Header("References (leave empty to have a default box built in code)")]
    [Tooltip("The whole box. Switched off when no dialogue is showing")]
    [SerializeField] GameObject m_Root;
    [Tooltip("Hidden entirely for interactables with no portrait, so signs and doors get a clean box")]
    [SerializeField] GameObject m_PortraitContainer;
    [SerializeField] Image m_PortraitImage;
    [Tooltip("Hidden when the interactable has no speaker name")]
    [SerializeField] Text m_SpeakerNameText;
    [SerializeField] Text m_BodyText;
    [Tooltip("Shown only once the page has finished typing: the 'press again' hint")]
    [SerializeField] GameObject m_ContinueIndicator;

    [Header("Typewriter")]
    [Tooltip("Characters revealed per second. 0 shows the whole page instantly")]
    [SerializeField] float m_TypewriterSpeed = 40.0f;

    [Header("Built-in box")]
    [Tooltip("Font for the box built in code. Empty falls back to Unity's built-in font - assign the game's pixel font here")]
    [SerializeField] Font m_Font;
    [Tooltip("Below ScreenFader's 1000, so a level transition fades over the dialogue rather than under it")]
    [SerializeField] int m_SortingOrder = 900;

    public bool IsTyping { get; private set; }
    public bool IsOpen { get; private set; }

    Coroutine m_TypewriterCoroutine;
    string m_CurrentFullText = "";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        EnsureBox();
        if (m_Root != null)
        {
            m_Root.SetActive(false);
        }
        if (m_ContinueIndicator != null)
        {
            m_ContinueIndicator.SetActive(false);
        }
    }

    //Show one page. Pass a null portrait and an empty speaker name for signs and objects
    public void ShowPage(string a_Text, Sprite a_Portrait, string a_SpeakerName)
    {
        EnsureBox();
        IsOpen = true;
        if (m_Root != null)
        {
            m_Root.SetActive(true);
        }
        if (m_ContinueIndicator != null)
        {
            m_ContinueIndicator.SetActive(false);
        }

        bool hasPortrait = (a_Portrait != null);
        if (m_PortraitContainer != null)
        {
            m_PortraitContainer.SetActive(hasPortrait);
        }
        if (m_PortraitImage != null && hasPortrait)
        {
            m_PortraitImage.sprite = a_Portrait;
        }

        bool hasName = !string.IsNullOrEmpty(a_SpeakerName);
        if (m_SpeakerNameText != null)
        {
            m_SpeakerNameText.gameObject.SetActive(hasName);
            if (hasName)
            {
                m_SpeakerNameText.text = a_SpeakerName;
            }
        }

        if (m_TypewriterCoroutine != null)
        {
            StopCoroutine(m_TypewriterCoroutine);
        }
        m_TypewriterCoroutine = StartCoroutine(TypewriterRoutine(a_Text));
    }

    //Mid-type, the interact button dumps the rest of the page instead of advancing it
    public void CompleteTyping()
    {
        if (!IsTyping)
        {
            return;
        }
        if (m_TypewriterCoroutine != null)
        {
            StopCoroutine(m_TypewriterCoroutine);
            m_TypewriterCoroutine = null;
        }
        SetVisibleCharacters(m_CurrentFullText.Length);
        IsTyping = false;
        if (m_ContinueIndicator != null)
        {
            m_ContinueIndicator.SetActive(true);
        }
    }

    public void Hide()
    {
        if (m_TypewriterCoroutine != null)
        {
            StopCoroutine(m_TypewriterCoroutine);
            m_TypewriterCoroutine = null;
        }
        IsTyping = false;
        IsOpen = false;
        if (m_Root != null)
        {
            m_Root.SetActive(false);
        }
    }

    IEnumerator TypewriterRoutine(string a_Text)
    {
        m_CurrentFullText = (a_Text != null) ? a_Text : "";
        IsTyping = true;
        if (m_TypewriterSpeed > 0.0f)
        {
            //Scaled time on purpose: pausing mid-sentence should stop the text too
            float revealed = 0.0f;
            while (revealed < m_CurrentFullText.Length)
            {
                SetVisibleCharacters(Mathf.FloorToInt(revealed));
                revealed += m_TypewriterSpeed * Time.deltaTime;
                yield return null;
            }
        }
        SetVisibleCharacters(m_CurrentFullText.Length);
        IsTyping = false;
        m_TypewriterCoroutine = null;
        if (m_ContinueIndicator != null)
        {
            m_ContinueIndicator.SetActive(true);
        }
    }

    //Legacy Text has no maxVisibleCharacters, so the not-yet-typed part is written in a
    //transparent colour instead of cut off - that way the line breaks stay put while typing
    //(a page containing its own rich text tags could be split mid-tag; keep tags out of pages)
    void SetVisibleCharacters(int a_Count)
    {
        if (m_BodyText == null)
        {
            return;
        }
        int count = Mathf.Clamp(a_Count, 0, m_CurrentFullText.Length);
        if (count >= m_CurrentFullText.Length)
        {
            m_BodyText.text = m_CurrentFullText;
            return;
        }
        m_BodyText.text = m_CurrentFullText.Substring(0, count) +
            "<color=#00000000>" + m_CurrentFullText.Substring(count) + "</color>";
    }

    // ---- Default box, built in code when nothing is assigned ----

    void EnsureBox()
    {
        if (m_Root != null)
        {
            return;
        }
        Font font = (m_Font != null) ? m_Font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject canvasObject = new GameObject("DialogueCanvas");
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = m_SortingOrder;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920.0f, 1080.0f);
        scaler.matchWidthOrHeight = 0.5f;

        //The box itself: bottom of the screen, with a margin
        m_Root = new GameObject("DialogueBox");
        m_Root.transform.SetParent(canvasObject.transform, false);
        Image background = m_Root.AddComponent<Image>();
        background.color = new Color(0.05f, 0.04f, 0.08f, 0.9f);
        background.raycastTarget = false; //Display only; never eat clicks meant for the menus
        RectTransform rootRect = background.rectTransform;
        rootRect.anchorMin = new Vector2(0.0f, 0.0f);
        rootRect.anchorMax = new Vector2(1.0f, 0.0f);
        rootRect.pivot = new Vector2(0.5f, 0.0f);
        rootRect.offsetMin = new Vector2(80.0f, 60.0f);
        rootRect.offsetMax = new Vector2(-80.0f, 60.0f + 260.0f);

        m_PortraitContainer = CreateChild(m_Root.transform, "Portrait", new Vector2(0.0f, 0.0f), new Vector2(0.0f, 1.0f),
            new Vector2(24.0f, 24.0f), new Vector2(24.0f + 200.0f, -24.0f));
        m_PortraitImage = m_PortraitContainer.AddComponent<Image>();
        m_PortraitImage.preserveAspect = true;
        m_PortraitImage.raycastTarget = false;
        m_PortraitContainer.SetActive(false);

        //Text sits to the right of where the portrait would be, so the layout does not jump
        GameObject nameObject = CreateChild(m_Root.transform, "SpeakerName", new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f),
            new Vector2(248.0f, -60.0f), new Vector2(-24.0f, -16.0f));
        m_SpeakerNameText = AddText(nameObject, font, 36, FontStyle.Bold, TextAnchor.UpperLeft);
        m_SpeakerNameText.color = new Color(1.0f, 0.85f, 0.45f, 1.0f);

        GameObject bodyObject = CreateChild(m_Root.transform, "BodyText", new Vector2(0.0f, 0.0f), new Vector2(1.0f, 1.0f),
            new Vector2(248.0f, 44.0f), new Vector2(-24.0f, -64.0f));
        m_BodyText = AddText(bodyObject, font, 30, FontStyle.Normal, TextAnchor.UpperLeft);

        GameObject indicatorObject = CreateChild(m_Root.transform, "ContinueIndicator", new Vector2(1.0f, 0.0f), new Vector2(1.0f, 0.0f),
            new Vector2(-80.0f, 12.0f), new Vector2(-24.0f, 48.0f));
        Text indicator = AddText(indicatorObject, font, 28, FontStyle.Normal, TextAnchor.LowerRight);
        indicator.text = "▼";
        m_ContinueIndicator = indicatorObject;
        m_ContinueIndicator.SetActive(false);
    }

    static GameObject CreateChild(Transform a_Parent, string a_Name, Vector2 a_AnchorMin, Vector2 a_AnchorMax, Vector2 a_OffsetMin, Vector2 a_OffsetMax)
    {
        GameObject go = new GameObject(a_Name);
        go.transform.SetParent(a_Parent, false);
        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = a_AnchorMin;
        rect.anchorMax = a_AnchorMax;
        rect.offsetMin = a_OffsetMin;
        rect.offsetMax = a_OffsetMax;
        return go;
    }

    static Text AddText(GameObject a_Object, Font a_Font, int a_Size, FontStyle a_Style, TextAnchor a_Alignment)
    {
        Text text = a_Object.AddComponent<Text>();
        text.font = a_Font;
        text.fontSize = a_Size;
        text.fontStyle = a_Style;
        text.alignment = a_Alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }
}
