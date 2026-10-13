using UnityEngine;

[DefaultExecutionOrder(100)]
public class PlayerAnimatorDriver : MonoBehaviour
{
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveZHash = Animator.StringToHash("MoveZ");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsCrouchingHash = Animator.StringToHash("IsCrouching");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int ThrowTimeHash = Animator.StringToHash("ThrowTime");
    private static readonly int JumpTimeHash = Animator.StringToHash("JumpTime");

    private const float TeleportDistance = 2f;
    private const float RunBlendValue = 1.6f;

    [SerializeField] private Animator animator;
    [SerializeField] private DodgeballPlayer player;
    [SerializeField] private float moveDampTime = 0.1f;
    [SerializeField] private string throwLayerName = "Throw";
    [Tooltip("How fast the throw layer fades in and out.")]
    [SerializeField] private float throwLayerBlendSpeed = 8f;

    private int throwLayer;
    private Vector3 lastPosition;

    private void Start()
    {
        throwLayer = animator.GetLayerIndex(throwLayerName);
        if (throwLayer < 0)
            Debug.LogWarning($"[Animation] The Animator has no layer named '{throwLayerName}'.");

        lastPosition = transform.position;
    }

    private void Update()
    {
        if (!animator.isActiveAndEnabled)
            return;

        UpdateLocomotion();
        UpdateThrow();
    }

    private void UpdateLocomotion()
    {
        Vector3 displacement = transform.position - lastPosition;
        lastPosition = transform.position;
        if (displacement.magnitude > TeleportDistance)
            displacement = Vector3.zero;

        Vector3 velocity = displacement / Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 localVelocity = transform.InverseTransformDirection(new Vector3(velocity.x, 0f, velocity.z));

        Vector3 blend = localVelocity.normalized * ToBlendValue(localVelocity.magnitude);
        animator.SetFloat(MoveXHash, blend.x, moveDampTime, Time.deltaTime);
        animator.SetFloat(MoveZHash, blend.z, moveDampTime, Time.deltaTime);
        animator.SetFloat(SpeedHash, localVelocity.magnitude / player.Movement.WalkSpeed, moveDampTime, Time.deltaTime);
        animator.SetBool(IsCrouchingHash, player.IsCrouching);
        animator.SetBool(IsGroundedHash, player.IsGrounded);

        if (!player.IsGrounded)
            animator.SetFloat(JumpTimeHash, ToJumpTime(velocity.y));
    }

    private float ToJumpTime(float verticalSpeed)
    {
        float jumpVelocity = player.Movement.JumpVelocity;
        return Mathf.Clamp01(0.5f - verticalSpeed / (2f * jumpVelocity));
    }

    private float ToBlendValue(float speed)
    {
        FPSController movement = player.Movement;
        if (player.IsCrouching)
            return speed / movement.CrouchSpeed;
        if (speed <= movement.WalkSpeed)
            return speed / movement.WalkSpeed;

        float sprintRange = Mathf.Max(movement.SprintSpeed - movement.WalkSpeed, 0.01f);
        return 1f + (RunBlendValue - 1f) * Mathf.Clamp01((speed - movement.WalkSpeed) / sprintRange);
    }

    private void UpdateThrow()
    {
        if (throwLayer < 0)
            return;

        float progress = player.ThrowProgress;
        if (progress > 0f)
            animator.SetFloat(ThrowTimeHash, progress);

        float targetWeight = progress > 0f ? 1f : 0f;
        float weight = Mathf.MoveTowards(animator.GetLayerWeight(throwLayer), targetWeight, throwLayerBlendSpeed * Time.deltaTime);
        animator.SetLayerWeight(throwLayer, weight);
    }
}
