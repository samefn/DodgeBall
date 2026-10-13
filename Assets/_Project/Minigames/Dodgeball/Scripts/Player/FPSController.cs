using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    [SerializeField] private Transform cameraPivot;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 4.5f;
    [SerializeField] private float sprintSpeed = 8.5f;
    [SerializeField] private float crouchSpeed = 2.2f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -20f;

    [Header("Crouch")]
    [SerializeField] private float standingHeight = 1.8f;
    [SerializeField] private float crouchHeight = 1.1f;
    [SerializeField] private float crouchTransitionSpeed = 6f;
    [SerializeField] private float cameraOffsetFromTop = 0.15f;

    [Header("Look")]
    [SerializeField] private float minPitch = -85f;
    [SerializeField] private float maxPitch = 85f;

    [Header("Animation")]
    [Tooltip("CharacterController.isGrounded flickers on steps and slopes; this keeps the jump animation from firing on them.")]
    [SerializeField] private float groundedGraceTime = 0.15f;

    private CharacterController controller;
    private IPlayerInputSource input;
    private float verticalVelocity;
    private float lastGroundedTime;
    private Vector3 correction;
    private float starvedTime;

    private const float MaxStep = 0.1f;
    private const float InputStarvationDelay = 0.25f;

    public Transform CameraPivot => cameraPivot;
    public float Pitch { get; private set; }
    public bool CanMove { get; set; } = true;
    public float SpeedMultiplier { get; set; } = 1f;
    public float WalkSpeed => walkSpeed;
    public float SprintSpeed => sprintSpeed;
    public float CrouchSpeed => crouchSpeed;
    public float JumpVelocity => Mathf.Sqrt(jumpHeight * -2f * gravity);
    public bool IsCrouching { get; private set; }
    public bool IsGrounded => Time.time - lastGroundedTime <= groundedGraceTime;
    public Vector3 QueuedCorrection => correction;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (input == null)
            input = GetComponent<IPlayerInputSource>();
        if (input == null)
            return;

        if (input is RemoteInputSource remote)
        {
            SimulateQueued(remote);
            return;
        }

        Simulate(input.Current, Time.deltaTime);
    }

    private void SimulateQueued(RemoteInputSource remote)
    {
        bool simulated = false;
        while (remote.TryDequeue(out PlayerInputState queued))
        {
            Simulate(queued, queued.deltaTime);
            simulated = true;
        }

        if (simulated)
        {
            starvedTime = 0f;
            return;
        }

        starvedTime += Time.deltaTime;
        if (starvedTime < InputStarvationDelay)
            return;

        PlayerInputState idle = remote.Current;
        idle.move = Vector2.zero;
        idle.jumpPressed = false;
        Simulate(idle, Time.deltaTime);
    }

    public void Simulate(PlayerInputState state, float deltaTime)
    {
        deltaTime = Mathf.Clamp(deltaTime, 0f, MaxStep);
        SetLook(state.yaw, state.pitch);

        if (!CanMove)
        {
            if (controller.enabled && controller.isGrounded)
                lastGroundedTime = Time.time;
            return;
        }

        UpdateCrouch(state.crouch, deltaTime);
        UpdateMovement(state, deltaTime);
    }

    public void SetLook(float yaw, float pitch)
    {
        Pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        cameraPivot.localRotation = Quaternion.Euler(Pitch, 0f, 0f);
    }

    public void Teleport(Vector3 position)
    {
        bool wasEnabled = controller.enabled;
        controller.enabled = false;
        transform.position = position;
        controller.enabled = wasEnabled;
        verticalVelocity = 0f;
        lastGroundedTime = Time.time;
        correction = Vector3.zero;
    }

    public void AddCorrection(Vector3 offset)
    {
        correction += offset;
    }

    private void UpdateMovement(PlayerInputState state, float deltaTime)
    {
        Vector2 move = Vector2.ClampMagnitude(state.move, 1f);
        float speed = (state.crouch ? crouchSpeed : state.sprint ? sprintSpeed : walkSpeed) * SpeedMultiplier;
        Vector3 horizontal = (transform.right * move.x + transform.forward * move.y) * speed;

        if (controller.isGrounded)
        {
            if (verticalVelocity < 0f)
                verticalVelocity = -2f;

            if (state.jumpPressed && !state.crouch)
            {
                verticalVelocity = JumpVelocity;
                lastGroundedTime = float.NegativeInfinity;
            }
        }

        verticalVelocity += gravity * deltaTime;
        controller.Move((horizontal + Vector3.up * verticalVelocity) * deltaTime + correction);
        correction = Vector3.zero;

        if (controller.isGrounded && verticalVelocity <= 0f)
            lastGroundedTime = Time.time;
    }

    private void UpdateCrouch(bool crouching, float deltaTime)
    {
        IsCrouching = crouching;
        float targetHeight = crouching ? crouchHeight : standingHeight;
        controller.height = Mathf.MoveTowards(controller.height, targetHeight, crouchTransitionSpeed * deltaTime);
        controller.center = Vector3.up * (controller.height * 0.5f);
        cameraPivot.localPosition = Vector3.up * (controller.height - cameraOffsetFromTop);
    }
}
