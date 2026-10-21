using UnityEngine;

//--------------------------------------------------------------------
//Place on a trigger collider at the final level's end door.
//Tells the GameManager the game is won. Player-only: enemies and cats
//also generate trigger events (kinematic Rigidbody + trigger capsule),
//so anything without the player's character controller is ignored.
//--------------------------------------------------------------------
public class GameEndTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider a_Other)
    {
        if (a_Other.GetComponentInParent<CharacterControllerBase>() == null)
        {
            return;
        }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGameWon();
        }
    }
}
