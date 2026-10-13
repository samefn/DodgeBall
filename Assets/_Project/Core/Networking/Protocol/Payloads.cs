using System;
using UnityEngine;

[Serializable]
public class ConnectAcceptPayload
{
    public int playerId;
}

[Serializable]
public class LobbyPayload
{
    public int[] playerIds;
    public int[] pings;
    public bool matchInProgress;
}

[Serializable]
public class PingPayload
{
    public float sentAt;
}

[Serializable]
public class KickPayload
{
    public string reason;
}

[Serializable]
public class PlayerStateData
{
    public int playerId;
    public Vector3 position;
    public float yaw;
    public float pitch;
    public int lives;
    public int score;
    public bool holdingBall;
    public bool crouching;
    public bool grounded;
    public float throwProgress;
    public float throwCharge;
    public int lastInputSequence;
}

[Serializable]
public class BallStateData
{
    public int ballId;
    public Vector3 position;
    public int state;
    public int ownerId;
}

[Serializable]
public class WorldStatePayload
{
    public float serverTime;
    public int phase;
    public float timeRemaining;
    public float countdown;
    public int winnerId;
    public PlayerStateData[] players;
    public BallStateData[] balls;
}

[Serializable]
public class InputBatchPayload
{
    public PlayerInputState[] inputs;
}
