using UnityEngine;
using System.Collections;

/// <summary>
/// In ConversationRoleGaze scenario, this script is used to move the camera to the agent,
/// making it seem like the participant is "approaching" the agent.
/// </summary>
public class ParticipantWalkUp : MonoBehaviour
{
    /// <summary>
    /// If true, the camera will begin moving toward the agent.
    /// </summary>
    public bool walkUp = false;

    public float WalkUpTime
    {
        get { return _animationComponent["ParticipantWalkUp"].time; }
    }

    private Animation _animationComponent;

    /// <summary>
    /// true if the camera has reached its final location, false otherwise.
    /// </summary>
    public bool Arrived
    {
        get { return walkUp && _animationComponent["ParticipantWalkUp"].normalizedTime >= 1f; }
    }

    public void Start()
    {
        _animationComponent = GetComponent<Animation>();
        _animationComponent.playAutomatically = false;
        _animationComponent.wrapMode = WrapMode.ClampForever;
        _animationComponent["ParticipantWalkUp"].enabled = true;
        _animationComponent["ParticipantWalkUp"].weight = 1f;
    }

    public void Update()
    {
        if (!walkUp)
        {
            _animationComponent["ParticipantWalkUp"].time = 0f;
            _animationComponent.Sample();
        }
        else
        {
            if (!_animationComponent.isPlaying)
                _animationComponent.Play();
        }
    }
}
