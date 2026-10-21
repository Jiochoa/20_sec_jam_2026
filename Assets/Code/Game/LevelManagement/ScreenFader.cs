using System.Collections;
using UnityEngine;
using UnityEngine.UI;
//--------------------------------------------------------------------
//Singleton overlay that fades the screen to black and back. It builds its own Canvas
//and Image in code, so no scene setup is required - just call ScreenFader.Get().
//Used by LevelFadeTransition to hide the teleport between in-scene levels.
//Uses unscaled time so it still works if the game is paused (Time.timeScale = 0).
//--------------------------------------------------------------------
public class ScreenFader : MonoBehaviour
{
    static ScreenFader g_Instance;

    public static ScreenFader Get()
    {
        if (g_Instance == null)
        {
            g_Instance = FindFirstObjectByType<ScreenFader>();
            if (g_Instance == null)
            {
                GameObject go = new GameObject("ScreenFader");
                g_Instance = go.AddComponent<ScreenFader>();
            }
        }
        return g_Instance;
    }

    Image m_Overlay;
    bool m_IsTransitioning;

    public bool IsTransitioning()
    {
        return m_IsTransitioning;
    }

    void Awake()
    {
        if (g_Instance != null && g_Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        g_Instance = this;
        EnsureOverlay();
    }

    void EnsureOverlay()
    {
        if (m_Overlay != null)
        {
            return;
        }
        GameObject canvasObject = new GameObject("FadeCanvas");
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; //On top of any gameplay UI

        GameObject imageObject = new GameObject("FadeImage");
        imageObject.transform.SetParent(canvasObject.transform, false);
        m_Overlay = imageObject.AddComponent<Image>();
        m_Overlay.color = new Color(0.0f, 0.0f, 0.0f, 0.0f);
        m_Overlay.raycastTarget = false; //Never eat input; we only display
        RectTransform rect = m_Overlay.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    //Fade to black, invoke a_OnBlack (do the teleport here, while the screen is hidden), then fade back in
    public void FadeThroughBlack(float a_FadeOutTime, float a_HoldTime, float a_FadeInTime, System.Action a_OnBlack)
    {
        if (m_IsTransitioning)
        {
            return;
        }
        EnsureOverlay();
        StartCoroutine(FadeRoutine(a_FadeOutTime, a_HoldTime, a_FadeInTime, a_OnBlack));
    }

    IEnumerator FadeRoutine(float a_FadeOutTime, float a_HoldTime, float a_FadeInTime, System.Action a_OnBlack)
    {
        m_IsTransitioning = true;
        yield return Fade(0.0f, 1.0f, a_FadeOutTime);
        if (a_OnBlack != null)
        {
            a_OnBlack();
        }
        if (a_HoldTime > 0.0f)
        {
            yield return new WaitForSecondsRealtime(a_HoldTime);
        }
        yield return Fade(1.0f, 0.0f, a_FadeInTime);
        m_IsTransitioning = false;
    }

    IEnumerator Fade(float a_From, float a_To, float a_Duration)
    {
        if (a_Duration <= 0.0f)
        {
            SetAlpha(a_To);
            yield break;
        }
        float t = 0.0f;
        while (t < a_Duration)
        {
            t += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(a_From, a_To, t / a_Duration));
            yield return null;
        }
        SetAlpha(a_To);
    }

    void SetAlpha(float a_Alpha)
    {
        Color c = m_Overlay.color;
        c.a = a_Alpha;
        m_Overlay.color = c;
    }
}
