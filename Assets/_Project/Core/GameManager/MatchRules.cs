using System;
using UnityEngine;

[Serializable]
public class MatchRules
{
    public const float MinDuration = 30f;
    public const float MaxDuration = 600f;
    public const int MinLives = 1;
    public const int MaxLives = 10;

    public float matchDuration = 300f;
    public int lives = 3;
    public int pointsPerHit = 1;
    public int pointsPerCatch = 1;
    public int pointsPerElimination = 2;
    public float throwPowerMultiplier = 1f;

    public MatchRules Clone()
    {
        return (MatchRules)MemberwiseClone();
    }

    public void Clamp()
    {
        matchDuration = Mathf.Clamp(matchDuration, MinDuration, MaxDuration);
        lives = Mathf.Clamp(lives, MinLives, MaxLives);
        pointsPerHit = Mathf.Clamp(pointsPerHit, 0, 10);
        pointsPerCatch = Mathf.Clamp(pointsPerCatch, 0, 10);
        pointsPerElimination = Mathf.Clamp(pointsPerElimination, 0, 10);
        throwPowerMultiplier = Mathf.Clamp(throwPowerMultiplier, 0.5f, 2f);
    }
}
