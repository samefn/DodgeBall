using System;
using UnityEngine;

[RequireComponent(typeof(PlayerIdentity))]
public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxLives = 3;
    [SerializeField] private float invulnerabilityTime = 1f;

    private PlayerIdentity identity;
    private float invulnerableUntil;

    public int Lives { get; private set; }
    public bool IsEliminated => Lives <= 0;
    public int PlayerId => identity.PlayerId;
    public bool AcceptsHits { get; set; } = true;

    public event Action<PlayerHealth, int> OnHit;
    public event Action<int> OnLivesChanged;

    private void Awake()
    {
        identity = GetComponent<PlayerIdentity>();
        Lives = maxLives;
    }

    public void ResetLives(int lives)
    {
        maxLives = Mathf.Max(1, lives);
        Lives = maxLives;
        invulnerableUntil = 0f;
        OnLivesChanged?.Invoke(Lives);
    }

    public void TakeHit(int attackerId)
    {
        if (!AcceptsHits || IsEliminated || Time.time < invulnerableUntil)
            return;

        Lives--;
        invulnerableUntil = Time.time + invulnerabilityTime;
        OnLivesChanged?.Invoke(Lives);
        OnHit?.Invoke(this, attackerId);
    }

    public void SetLivesFromNetwork(int lives)
    {
        if (lives == Lives)
            return;

        Lives = lives;
        OnLivesChanged?.Invoke(Lives);
    }
}
