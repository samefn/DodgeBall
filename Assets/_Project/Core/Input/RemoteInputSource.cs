using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class RemoteInputSource : MonoBehaviour, IPlayerInputSource
{
    private const int MaxQueuedInputs = 300;

    private readonly Queue<PlayerInputState> queued = new Queue<PlayerInputState>();
    private PlayerInputState current;
    private PlayerInputState latest;
    private int lastReceivedSequence;
    private bool jumpQueued;
    private bool pickupQueued;
    private bool throwQueued;

    public PlayerInputState Current => current;

    public int LastProcessedSequence { get; private set; }

    public void Receive(PlayerInputState input)
    {
        if (input.sequence != 0 && input.sequence <= lastReceivedSequence)
            return;

        lastReceivedSequence = input.sequence;
        latest = input;
        jumpQueued |= input.jumpPressed;
        pickupQueued |= input.pickupPressed;
        throwQueued |= input.throwPressed;

        queued.Enqueue(input);
        while (queued.Count > MaxQueuedInputs)
            queued.Dequeue();
    }

    public bool TryDequeue(out PlayerInputState input)
    {
        if (queued.Count == 0)
        {
            input = default;
            return false;
        }

        input = queued.Dequeue();
        LastProcessedSequence = input.sequence;
        return true;
    }

    private void Update()
    {
        current = latest;
        current.jumpPressed = jumpQueued;
        current.pickupPressed = pickupQueued;
        current.throwPressed = throwQueued;

        jumpQueued = false;
        pickupQueued = false;
        throwQueued = false;
    }
}
