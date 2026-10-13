using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class DodgeBall : MonoBehaviour
{
    public const int NoOwner = -1;

    private const float TeleportDistance = 8f;
    private const float MaxExtrapolation = 0.15f;

    [SerializeField] private float minHitSpeed = 4f;

    private Rigidbody body;
    private Collider ballCollider;
    private Collider ignoredCollider;
    private RigidbodyInterpolation defaultInterpolation;
    private Transform followTarget;
    private Vector3 spawnPosition;
    private readonly SnapshotBuffer networkBuffer = new SnapshotBuffer(TeleportDistance, MaxExtrapolation);
    private Transform predictedHand;
    private float predictedHoldUntil;

    public int BallId { get; private set; }
    public BallState State { get; private set; } = BallState.Free;
    public int OwnerId { get; private set; } = NoOwner;
    public bool IsAuthoritative { get; private set; } = true;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        ballCollider = GetComponent<Collider>();
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        defaultInterpolation = body.interpolation;
    }

    public void Initialize(int ballId)
    {
        BallId = ballId;
        spawnPosition = transform.position;
    }

    public void SetAuthoritative(bool authoritative)
    {
        IsAuthoritative = authoritative;
        if (authoritative)
            return;

        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.None;
        ballCollider.enabled = false;
    }

    public bool CanBePickedBy(int playerId)
    {
        return State == BallState.Free || (State == BallState.Thrown && OwnerId != playerId);
    }

    public void AttachTo(Transform holdPoint, int holderId)
    {
        RestoreIgnoredCollision();
        State = BallState.Held;
        OwnerId = holderId;
        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.None;
        ballCollider.enabled = false;
        followTarget = holdPoint;
        SnapToFollowTarget();
    }

    public void Throw(Vector3 velocity, Collider thrower, Vector3? startPosition = null)
    {
        Detach();
        if (startPosition.HasValue)
        {
            body.position = startPosition.Value;
            transform.position = startPosition.Value;
        }
        ignoredCollider = thrower;
        if (thrower != null)
            Physics.IgnoreCollision(ballCollider, thrower, true);

        body.linearVelocity = velocity;
        UIAudio.PlayThrowAt(transform.position);
        State = BallState.Thrown;
    }

    public void Release()
    {
        Detach();
        body.linearVelocity = Vector3.zero;
        MakeFree();
    }

    public void ResetToSpawn()
    {
        RespawnAt(spawnPosition);
    }

    public void RespawnAt(Vector3 position)
    {
        Detach();
        body.position = position;
        transform.position = position;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        MakeFree();
    }

    public void ApplyNetworkState(Vector3 position, BallState state, int ownerId, Transform holderHand, float serverTime)
    {
        if (state == BallState.Thrown && State != BallState.Thrown)
            UIAudio.PlayThrowAt(position);

        networkBuffer.Add(serverTime, position);
        State = state;
        OwnerId = ownerId;

        if (state != BallState.Held && Time.time < predictedHoldUntil)
        {
            followTarget = predictedHand;
            return;
        }

        predictedHoldUntil = 0f;
        followTarget = holderHand;
    }

    public void PredictHold(Transform hand, float duration)
    {
        if (IsAuthoritative || hand == null)
            return;

        predictedHand = hand;
        predictedHoldUntil = Time.time + duration;
        followTarget = hand;
    }

    private void Update()
    {
        if (IsAuthoritative || followTarget != null)
            return;

        if (networkBuffer.TrySample(NetworkClock.RenderTime, out Vector3 position, out _, out _))
            transform.position = position;
    }

    private void LateUpdate()
    {
        if (followTarget != null)
            SnapToFollowTarget();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsAuthoritative || State != BallState.Thrown)
            return;

        PlayerHealth target = collision.collider.GetComponentInParent<PlayerHealth>();
        if (target != null && target.PlayerId != OwnerId && collision.relativeVelocity.magnitude >= minHitSpeed)
            target.TakeHit(OwnerId);

        MakeFree();
    }

    private void SnapToFollowTarget()
    {
        transform.position = followTarget.position;
        if (IsAuthoritative)
            body.position = followTarget.position;
    }

    private void Detach()
    {
        if (followTarget != null)
            SnapToFollowTarget();

        followTarget = null;
        body.position = transform.position;
        body.interpolation = defaultInterpolation;
        body.isKinematic = false;
        ballCollider.enabled = true;
        if (ArenaBounds.Instance != null)
            ArenaBounds.Instance.IgnoreWalls(ballCollider);
    }

    private void MakeFree()
    {
        RestoreIgnoredCollision();
        State = BallState.Free;
        OwnerId = NoOwner;
    }

    private void RestoreIgnoredCollision()
    {
        if (ignoredCollider != null)
            Physics.IgnoreCollision(ballCollider, ignoredCollider, false);
        ignoredCollider = null;
    }
}
