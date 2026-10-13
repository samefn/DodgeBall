using System;
using UnityEngine;

[Serializable]
public struct PlayerInputState
{
    public Vector2 move;
    public float yaw;
    public float pitch;
    public bool sprint;
    public bool crouch;
    public bool jumpPressed;
    public bool pickupPressed;
    public bool throwPressed;
    public bool throwHeld;
    public int sequence;
    public float deltaTime;
}
