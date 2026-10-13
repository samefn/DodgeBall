using System.Collections.Generic;
using UnityEngine;

public class ArenaBounds : MonoBehaviour
{
    public const string WallPrefix = "InvisibleWalls";

    private const float EdgeMargin = 1f;
    private const float OutOfBoundsDelay = 3f;
    private const float BallDropHeight = 1.2f;
    private const float PlayerMinDistance = 2.5f;
    private const int SpawnAttempts = 30;

    private readonly HashSet<Collider> walls = new HashSet<Collider>();
    private readonly Dictionary<DodgeBall, float> outOfBoundsTime = new Dictionary<DodgeBall, float>();
    private Bounds outer;
    private Bounds inner;
    private BallSpawner ballSpawner;

    public static ArenaBounds Instance { get; private set; }
    public bool IsValid { get; private set; }
    public Vector3 Center => inner.center;

    private void Awake()
    {
        Instance = this;
        FindWalls();
    }

    private void Start()
    {
        ballSpawner = FindFirstObjectByType<BallSpawner>();
        if (ballSpawner == null)
            return;

        foreach (DodgeBall ball in ballSpawner.Balls)
            IgnoreWalls(ball.GetComponent<Collider>());
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void FindWalls()
    {
        bool first = true;
        float thickness = 0f;
        foreach (Collider candidate in FindObjectsByType<Collider>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (!candidate.name.StartsWith(WallPrefix))
                continue;

            candidate.isTrigger = false;
            walls.Add(candidate);

            Bounds wallBounds = candidate.bounds;
            thickness = Mathf.Max(thickness, Mathf.Min(wallBounds.size.x, wallBounds.size.z));
            if (first)
                outer = wallBounds;
            else
                outer.Encapsulate(wallBounds);
            first = false;
        }

        IsValid = walls.Count > 0;
        if (!IsValid)
        {
            Debug.LogWarning($"[Arena] No hay objetos '{WallPrefix}': se usarán los puntos de aparición fijos.");
            return;
        }

        float shrink = 2f * (thickness + EdgeMargin);
        inner = new Bounds(outer.center, new Vector3(
            Mathf.Max(0.5f, outer.size.x - shrink),
            outer.size.y,
            Mathf.Max(0.5f, outer.size.z - shrink)));
    }

    public bool IsWall(Collider candidate) => walls.Contains(candidate);

    public void IgnoreWalls(Collider ballCollider)
    {
        if (ballCollider == null)
            return;

        foreach (Collider wall in walls)
            Physics.IgnoreCollision(ballCollider, wall, true);
    }

    public bool Contains(Vector3 position)
    {
        return position.x >= outer.min.x && position.x <= outer.max.x
            && position.z >= outer.min.z && position.z <= outer.max.z
            && position.y >= outer.min.y - 3f;
    }

    public Vector3 FindSpawnPoint(List<Vector3> avoid)
    {
        Vector3 best = RandomPoint();
        float bestDistance = -1f;
        for (int i = 0; i < SpawnAttempts; i++)
        {
            Vector3 candidate = RandomPoint();
            float closest = float.MaxValue;
            foreach (Vector3 other in avoid)
            {
                Vector2 offset = new Vector2(candidate.x - other.x, candidate.z - other.z);
                closest = Mathf.Min(closest, offset.magnitude);
            }

            if (closest > bestDistance)
            {
                best = candidate;
                bestDistance = closest;
            }
            if (closest >= PlayerMinDistance)
                break;
        }

        return GroundAt(best);
    }

    public Quaternion FacingCenter(Vector3 position)
    {
        Vector3 toCenter = Center - position;
        toCenter.y = 0f;
        return toCenter.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toCenter) : Quaternion.identity;
    }

    private Vector3 RandomPoint()
    {
        return new Vector3(
            Random.Range(inner.min.x, inner.max.x),
            outer.center.y,
            Random.Range(inner.min.z, inner.max.z));
    }

    private Vector3 GroundAt(Vector3 point)
    {
        Vector3 origin = new Vector3(point.x, outer.max.y + 5f, point.z);
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, outer.size.y + 50f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (first, second) => first.distance.CompareTo(second.distance));
        foreach (RaycastHit hit in hits)
        {
            if (IsWall(hit.collider) || hit.collider is CharacterController || hit.collider.GetComponentInParent<DodgeBall>() != null)
                continue;
            return hit.point + Vector3.up * 0.05f;
        }

        return new Vector3(point.x, outer.min.y + 0.1f, point.z);
    }

    private void Update()
    {
        if (!IsValid || !GameSession.IsAuthoritative || ballSpawner == null)
            return;

        foreach (DodgeBall ball in ballSpawner.Balls)
        {
            if (ball.State == BallState.Held || Contains(ball.transform.position))
            {
                outOfBoundsTime[ball] = 0f;
                continue;
            }

            outOfBoundsTime.TryGetValue(ball, out float time);
            time += Time.deltaTime;
            if (time >= OutOfBoundsDelay)
            {
                ball.RespawnAt(GroundAt(RandomPoint()) + Vector3.up * BallDropHeight);
                time = 0f;
            }
            outOfBoundsTime[ball] = time;
        }
    }
}
