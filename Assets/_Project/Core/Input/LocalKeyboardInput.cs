using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class LocalKeyboardInput : MonoBehaviour, IPlayerInputSource
{
    private const float MaxPitch = 85f;

    [SerializeField] private KeyboardProfile profile;

    private PlayerInputState state;

    public PlayerInputState Current => state;

    public void SetProfile(KeyboardProfile keyboardProfile)
    {
        profile = keyboardProfile;
    }

    private void Start()
    {
        state.yaw = transform.eulerAngles.y;

        if (profile != null && profile.useMouseLook)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || profile == null)
            return;

        if (InputGate.Blocked)
        {
            state = new PlayerInputState { yaw = state.yaw, pitch = state.pitch };
            return;
        }

        Vector2 move = new Vector2(Axis(keyboard, profile.left, profile.right), Axis(keyboard, profile.back, profile.forward));
        state.move = Vector2.ClampMagnitude(move, 1f);
        state.sprint = IsHeld(keyboard, profile.sprint);
        state.crouch = IsHeld(keyboard, profile.crouch);
        state.jumpPressed = WasPressed(keyboard, profile.jump);
        state.pickupPressed = WasPressed(keyboard, profile.pickup);
        state.throwPressed = WasPressed(keyboard, profile.throwBall);
        state.throwHeld = IsHeld(keyboard, profile.throwBall);
        UpdateLook(keyboard);
    }

    private void UpdateLook(Keyboard keyboard)
    {
        Vector2 keyLook = new Vector2(Axis(keyboard, profile.lookLeft, profile.lookRight), Axis(keyboard, profile.lookDown, profile.lookUp));
        Vector2 look = keyLook * (profile.keyLookSpeed * Time.deltaTime);

        if (profile.useMouseLook && Mouse.current != null)
            look += Mouse.current.delta.ReadValue() * (profile.mouseSensitivity * GameSettings.MouseSensitivity);

        state.yaw = Mathf.Repeat(state.yaw + look.x, 360f);
        state.pitch = Mathf.Clamp(state.pitch - look.y, -MaxPitch, MaxPitch);
    }

    private static float Axis(Keyboard keyboard, Key negative, Key positive)
    {
        return (IsHeld(keyboard, positive) ? 1f : 0f) - (IsHeld(keyboard, negative) ? 1f : 0f);
    }

    private static bool IsHeld(Keyboard keyboard, Key key)
    {
        return key != Key.None && keyboard[key].isPressed;
    }

    private static bool WasPressed(Keyboard keyboard, Key key)
    {
        return key != Key.None && keyboard[key].wasPressedThisFrame;
    }
}
