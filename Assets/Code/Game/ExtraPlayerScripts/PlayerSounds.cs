using System.Collections.Generic;
using UnityEngine;
//--------------------------------------------------------------------
//Plays the player's movement sounds. Built like OnJumpParticleSpawner (the asset's own
//"react to what the character did" component): it listens to the controller's OnJump event
//and watches the capsule's collision info for landings, so no asset code has to change.
//
//Add this to the character object and assign both references (or leave them empty and let
//it find them). Which clips these actually make is up to the SoundManager.
//--------------------------------------------------------------------
public class PlayerSounds : MonoBehaviour
{
    [SerializeField] GroundedCharacterController m_CharacterController = null;
    [SerializeField] ControlledCapsuleCollider m_Collider = null;
    [Tooltip("How hard the character has to hit the ground before it counts as a landing worth hearing")]
    [SerializeField] float m_LandSpeedThreshold = 3.0f;
    [Tooltip("How upward a surface normal has to be to count as ground rather than a wall")]
    [Range(0.0f, 1.0f)][SerializeField] float m_LandNormalThreshold = 0.5f;

    [Header("Footsteps")]
    [Tooltip("How far the player travels between footsteps. Distance rather than time, so sprinting steps faster on its own without a second setting")]
    [SerializeField] float m_StepDistance = 1.4f;
    [Tooltip("Below this speed the player counts as standing still and stops stepping")]
    [SerializeField] float m_FootstepSpeedThreshold = 0.5f;

    float m_DistanceSinceStep;

    //Fills in the obvious references when the component is first added in the editor
    void Reset()
    {
        m_CharacterController = GetComponentInParent<GroundedCharacterController>();
        m_Collider = GetComponentInParent<ControlledCapsuleCollider>();
    }

    void Awake()
    {
        if (m_CharacterController == null)
        {
            m_CharacterController = GetComponentInParent<GroundedCharacterController>();
        }
        if (m_Collider == null)
        {
            m_Collider = GetComponentInParent<ControlledCapsuleCollider>();
        }
    }

    void OnEnable()
    {
        if (m_CharacterController != null)
        {
            m_CharacterController.OnJump += PlayJump;
        }
    }

    void OnDisable()
    {
        if (m_CharacterController != null)
        {
            m_CharacterController.OnJump -= PlayJump;
        }
    }

    void PlayJump()
    {
        SoundManager.Play(SoundId.Jump);
    }

    void FixedUpdate()
    {
        if (m_Collider == null)
        {
            return;
        }
        UpdateFootsteps();
        //Same collision list the particle spawner uses, filtered to real landings: a hard
        //enough impact against a surface that is more floor than wall
        List<CapsuleCollisionOccurrance> collisions = m_Collider.GetCollisionInfo();
        for (int i = 0; i < collisions.Count; i++)
        {
            if (collisions[i].m_VelocityLossPure.magnitude >= m_LandSpeedThreshold &&
                collisions[i].m_Normal.y >= m_LandNormalThreshold)
            {
                //The SoundManager's own repeat guard stops the several contacts of one
                //landing from stacking up
                SoundManager.Play(SoundId.Land);
                return;
            }
        }
    }

    //Steps are spaced by distance travelled rather than by a timer, so walking, sprinting and
    //being slowed all step at the rate they look like they should with one number to tune
    void UpdateFootsteps()
    {
        if (!m_Collider.IsGrounded())
        {
            //Airborne: start the count fresh so landing doesn't immediately stack a step
            //on top of the landing sound
            m_DistanceSinceStep = 0.0f;
            return;
        }

        float speed = Mathf.Abs(m_Collider.GetVelocity().x);
        if (speed < m_FootstepSpeedThreshold)
        {
            //Standing still. Bank a full step so walking off again sounds on the first stride
            m_DistanceSinceStep = m_StepDistance;
            return;
        }

        m_DistanceSinceStep += speed * Time.fixedDeltaTime;
        if (m_DistanceSinceStep >= m_StepDistance)
        {
            m_DistanceSinceStep = 0.0f;
            SoundManager.Play(SoundId.Footstep);
        }
    }
}
