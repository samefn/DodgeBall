using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(menuName = "Dodgeball/Input/Keyboard Profile", fileName = "KeyboardProfile")]
public class KeyboardProfile : ScriptableObject
{
    [Header("Movement")]
    public Key forward = Key.W;
    public Key back = Key.S;
    public Key left = Key.A;
    public Key right = Key.D;

    [Header("Actions")]
    public Key jump = Key.Space;
    public Key sprint = Key.LeftShift;
    public Key crouch = Key.LeftCtrl;
    public Key pickup = Key.E;
    public Key throwBall = Key.F;

    [Header("Look")]
    public bool useMouseLook = true;
    public float mouseSensitivity = 0.1f;
    public Key lookUp = Key.None;
    public Key lookDown = Key.None;
    public Key lookLeft = Key.None;
    public Key lookRight = Key.None;
    [Tooltip("Degrees per second when looking with keys.")]
    public float keyLookSpeed = 120f;
}
