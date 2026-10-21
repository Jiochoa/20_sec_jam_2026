using UnityEngine;
//--------------------------------------------------------------------
//A trigger placed at the end of a level (e.g. on the end door). When the player walks
//through it, the screen fades to black, the InSceneLevelSwitcher moves the player to
//m_LevelIndex, then the screen fades back in.
//Reuses the asset's in-scene level system - this only adds the fade and a player filter.
//The trigger's Collider must have Is Trigger enabled; the player's Rigidbody makes the
//trigger event fire (no Rigidbody needed on this object).
//--------------------------------------------------------------------
public class LevelFadeTransition : MonoBehaviour
{
    [Tooltip("Index (in the InSceneLevelSwitcher's Levels list) to send the player to")]
    [SerializeField] int m_LevelIndex = 1;
    [SerializeField] float m_FadeOutTime = 0.4f;
    [SerializeField] float m_HoldTime = 0.1f;
    [SerializeField] float m_FadeInTime = 0.4f;

    void OnTriggerEnter(Collider a_Collider)
    {
        //Only the player triggers a level change (not cats or thrown cats)
        if (a_Collider.GetComponent<ControlledCapsuleCollider>() == null)
        {
            return;
        }
        ScreenFader fader = ScreenFader.Get();
        if (fader.IsTransitioning())
        {
            return;
        }
        InSceneLevelSwitcher switcher = InSceneLevelSwitcher.Get();
        if (switcher == null)
        {
            Debug.LogWarning("LevelFadeTransition: no InSceneLevelSwitcher in the scene, cannot switch levels.");
            return;
        }
        int index = m_LevelIndex;
        fader.FadeThroughBlack(m_FadeOutTime, m_HoldTime, m_FadeInTime, () =>
        {
            switcher.SetIndex(index);
            switcher.Respawn(); //Teleports the player to that level's start point and snaps the camera
        });
    }
}
