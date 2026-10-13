using System;
using UnityEngine;

[RequireComponent(typeof(PlayerIdentity))]
public class BallHandler : MonoBehaviour
{
    [Tooltip("Empty object inside the right hand bone. The held ball sits here.")]
    [SerializeField] private Transform holdPoint;
    [Tooltip("Where the player aims from, usually CameraPivot.")]
    [SerializeField] private Transform aimSource;
    [SerializeField] private float pickupRadius = 2f;
    [Tooltip("Farthest point the crosshair can aim at.")]
    [SerializeField] private float aimDistance = 40f;

    [Header("Throw power")]
    [SerializeField] private float minThrowSpeed = 4f;
    [SerializeField] private float maxThrowSpeed = 30f;
    [Tooltip("Seconds holding the key to reach maximum power.")]
    [SerializeField] private float maxChargeTime = 1.5f;
    [SerializeField] private float throwCooldown = 0.4f;

    [Header("Throw clip timing (normalized 0-1)")]
    [Tooltip("Frame where the pose freezes while charging: the last frame before the arm moves forward.")]
    [SerializeField, Range(0f, 1f)] private float holdPoseTime = 0.45f;
    [Tooltip("Frame where the ball leaves the hand.")]
    [SerializeField, Range(0f, 1f)] private float releaseTime = 0.55f;
    [Tooltip("Seconds from the start of the clip to the hold pose.")]
    [SerializeField] private float windupDuration = 0.4f;
    [Tooltip("Seconds from the hold pose to the end of the clip.")]
    [SerializeField] private float followThroughDuration = 0.45f;

    private PlayerIdentity identity;
    private BallSpawner ballSpawner;
    private Collider ownCollider;
    private Camera aimCamera;
    private float pickupRequestUntil;

    private const float PickupBufferTime = 0.3f;
    private const float QuickWindupFactor = 4f;

    private const float LaunchForwardOffset = 0.8f;
    private const float DefaultAimDistance = 15f;
    private IPlayerInputSource input;
    private DodgeBall heldBall;
    private float nextThrowTime;
    private float chargeTimer;
    private bool releaseRequested;
    private bool launched;

    public Transform HoldPoint => holdPoint;
    public float PickupRadius => pickupRadius;
    public bool IsHolding => heldBall != null;
    public bool IsThrowing => Phase != ThrowPhase.None;
    public ThrowPhase Phase { get; private set; }
    public float ThrowProgress { get; private set; }
    public float ThrowCharge => IsThrowing ? Mathf.Clamp01(chargeTimer / maxChargeTime) : 0f;
    public bool CanAct { get; set; } = true;
    public float ThrowPowerMultiplier { get; set; } = 1f;

    public event Action<int, int> OnBallCaught;

    private void Awake()
    {
        identity = GetComponent<PlayerIdentity>();
        ownCollider = GetComponent<Collider>();
        aimCamera = GetComponentInChildren<Camera>(true);
    }

    private void OnValidate()
    {
        releaseTime = Mathf.Max(releaseTime, holdPoseTime);
    }

    private void Update()
    {
        if (input == null)
            input = GetComponent<IPlayerInputSource>();
        if (input == null)
            return;

        if (!CanAct)
        {
            ResetThrow();
            pickupRequestUntil = 0f;
            return;
        }

        PlayerInputState state = input.Current;
        if (state.pickupPressed)
            pickupRequestUntil = Time.time + PickupBufferTime;

        if (IsThrowing)
            UpdateThrow(state.throwHeld);
        else if (heldBall == null && Time.time <= pickupRequestUntil)
        {
            if (TryPickup())
                pickupRequestUntil = 0f;
        }
        else if (heldBall != null && (state.throwPressed || state.throwHeld) && Time.time >= nextThrowTime)
            BeginThrow(state.throwHeld);
    }

    public void ReleaseBall()
    {
        ResetThrow();
        if (heldBall == null)
            return;

        heldBall.Release();
        heldBall = null;
    }

    private void BeginThrow(bool keyHeld)
    {
        Phase = ThrowPhase.WindingUp;
        ThrowProgress = 0f;
        chargeTimer = 0f;
        releaseRequested = !keyHeld;
        launched = false;
    }

    private void UpdateThrow(bool keyHeld)
    {
        if (heldBall == null && !launched)
        {
            ResetThrow();
            return;
        }

        if (!keyHeld)
            releaseRequested = true;
        if (!releaseRequested)
            chargeTimer += Time.deltaTime;

        switch (Phase)
        {
            case ThrowPhase.WindingUp:
                float windupSpeed = releaseRequested ? QuickWindupFactor : 1f;
                ThrowProgress = Mathf.MoveTowards(ThrowProgress, holdPoseTime, holdPoseTime / windupDuration * windupSpeed * Time.deltaTime);
                if (ThrowProgress >= holdPoseTime)
                    Phase = releaseRequested ? ThrowPhase.Releasing : ThrowPhase.Holding;
                break;

            case ThrowPhase.Holding:
                if (releaseRequested)
                    Phase = ThrowPhase.Releasing;
                break;

            case ThrowPhase.Releasing:
                ThrowProgress = Mathf.MoveTowards(ThrowProgress, 1f, (1f - holdPoseTime) / followThroughDuration * Time.deltaTime);
                if (!launched && ThrowProgress >= releaseTime)
                    Launch();
                if (ThrowProgress >= 1f)
                {
                    ResetThrow();
                    nextThrowTime = Time.time + throwCooldown;
                }
                break;
        }
    }

    private void Launch()
    {
        float speed = Mathf.Lerp(minThrowSpeed, maxThrowSpeed, ThrowCharge) * ThrowPowerMultiplier;
        Ray aimRay = GetAimRay();
        bool hasTarget = TryGetAimTarget(aimRay, out Vector3 target, out float targetDistance);

        Vector3 origin = hasTarget && targetDistance < LaunchForwardOffset + 0.3f
            ? holdPoint.position
            : aimRay.GetPoint(LaunchForwardOffset);

        Vector3 aimPoint = hasTarget ? target : aimRay.GetPoint(DefaultAimDistance);
        heldBall.Throw(GetLaunchVelocity(origin, aimPoint, speed), ownCollider, origin);
        heldBall = null;
        launched = true;
    }

    private static Vector3 GetLaunchVelocity(Vector3 origin, Vector3 target, float speed)
    {
        Vector3 toTarget = target - origin;
        Vector3 straight = toTarget.normalized * speed;

        float gravity = -Physics.gravity.y;
        Vector3 flat = new Vector3(toTarget.x, 0f, toTarget.z);
        float distance = flat.magnitude;
        if (gravity <= 0f || distance < 0.05f)
            return straight;

        float speedSquared = speed * speed;
        float discriminant = speedSquared * speedSquared - gravity * (gravity * distance * distance + 2f * toTarget.y * speedSquared);
        if (discriminant < 0f)
            return straight;

        float angle = Mathf.Atan((speedSquared - Mathf.Sqrt(discriminant)) / (gravity * distance));
        Vector3 direction = flat / distance * Mathf.Cos(angle) + Vector3.up * Mathf.Sin(angle);
        return direction * speed;
    }

    private Ray GetAimRay()
    {
        Transform view = aimCamera != null ? aimCamera.transform : aimSource;
        return new Ray(view.position, view.forward);
    }

    private bool TryGetAimTarget(Ray aimRay, out Vector3 target, out float distance)
    {
        RaycastHit[] hits = Physics.RaycastAll(aimRay, aimDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (first, second) => first.distance.CompareTo(second.distance));

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform))
                continue;
            if (ArenaBounds.Instance != null && ArenaBounds.Instance.IsWall(hit.collider))
                continue;

            target = hit.point;
            distance = hit.distance;
            return true;
        }

        target = Vector3.zero;
        distance = 0f;
        return false;
    }

    private void ResetThrow()
    {
        Phase = ThrowPhase.None;
        ThrowProgress = 0f;
        chargeTimer = 0f;
        releaseRequested = false;
        launched = false;
    }

    private bool TryPickup()
    {
        if (ballSpawner == null)
            ballSpawner = FindFirstObjectByType<BallSpawner>();
        if (ballSpawner == null)
            return false;

        DodgeBall closest = ballSpawner.FindClosestPickable(holdPoint.position, pickupRadius, identity.PlayerId);
        if (closest == null)
            return false;

        bool isCatch = closest.State == BallState.Thrown;
        int throwerId = closest.OwnerId;

        closest.AttachTo(holdPoint, identity.PlayerId);
        heldBall = closest;

        if (isCatch)
            OnBallCaught?.Invoke(identity.PlayerId, throwerId);
        return true;
    }
}
