using System.Collections.Generic;
using UnityEngine;

public class BallSpawner : MonoBehaviour
{
    [SerializeField] private DodgeBall ballPrefab;
    [SerializeField] private Transform[] spawnPoints;

    private readonly List<DodgeBall> balls = new List<DodgeBall>();

    public IReadOnlyList<DodgeBall> Balls => balls;

    private void Awake()
    {
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            DodgeBall ball = Instantiate(ballPrefab, spawnPoints[i].position, Quaternion.identity);
            ball.Initialize(i);
            balls.Add(ball);
        }
    }

    public void SetAuthoritative(bool authoritative)
    {
        foreach (DodgeBall ball in balls)
            ball.SetAuthoritative(authoritative);
    }

    public void ResetAll()
    {
        foreach (DodgeBall ball in balls)
            ball.ResetToSpawn();
    }

    public DodgeBall GetBall(int ballId)
    {
        return ballId >= 0 && ballId < balls.Count ? balls[ballId] : null;
    }

    public DodgeBall FindClosestPickable(Vector3 position, float radius, int playerId)
    {
        DodgeBall closest = null;
        float closestDistance = radius;
        foreach (DodgeBall ball in balls)
        {
            float distance = Vector3.Distance(ball.transform.position, position);
            if (distance > closestDistance || !ball.CanBePickedBy(playerId))
                continue;

            closest = ball;
            closestDistance = distance;
        }

        return closest;
    }
}
