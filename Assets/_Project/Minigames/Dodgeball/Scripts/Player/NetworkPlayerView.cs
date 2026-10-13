using System.Collections.Generic;
using UnityEngine;

public class NetworkPlayerView : MonoBehaviour
{
    private struct PredictionRecord
    {
        public int Sequence;
        public Vector3 Position;
    }

    private const float PlayerTeleportDistance = 3f;
    private const float MaxExtrapolation = 0.2f;
    private const int MaxHistory = 300;

    [Tooltip("Prediction errors below this (meters) are ignored.")]
    [SerializeField] private float correctionTolerance = 0.03f;
    [Tooltip("Prediction errors above this (meters) snap to the host position.")]
    [SerializeField] private float snapDistance = 2.5f;
    [Tooltip("How fast prediction errors are corrected.")]
    [SerializeField] private float correctionSpeed = 15f;

    private readonly SnapshotBuffer buffer = new SnapshotBuffer(PlayerTeleportDistance, MaxExtrapolation);
    private readonly List<PredictionRecord> history = new List<PredictionRecord>();
    private FPSController movement;
    private Vector3 pendingCorrection;
    private bool hasState;

    public bool IsPredicted { get; private set; }

    private void Awake()
    {
        movement = GetComponent<FPSController>();
    }

    public void EnablePrediction()
    {
        IsPredicted = true;
    }

    public void ApplyState(PlayerStateData state, float serverTime)
    {
        if (IsPredicted)
        {
            Reconcile(state);
            return;
        }

        buffer.Add(serverTime, state.position, state.yaw, state.pitch);
        if (hasState)
            return;

        transform.position = state.position;
        hasState = true;
    }

    public void RecordPrediction(int sequence)
    {
        Vector3 settled = transform.position + pendingCorrection + movement.QueuedCorrection;
        history.Add(new PredictionRecord { Sequence = sequence, Position = settled });
        if (history.Count > MaxHistory)
            history.RemoveAt(0);
    }

    private void Update()
    {
        if (IsPredicted)
        {
            ApplyCorrectionStep();
            return;
        }

        if (!hasState || !buffer.TrySample(NetworkClock.RenderTime, out Vector3 position, out float yaw, out float pitch))
            return;

        transform.position = position;
        movement.SetLook(yaw, pitch);
    }

    private void Reconcile(PlayerStateData state)
    {
        if (!hasState)
        {
            Snap(state.position);
            hasState = true;
            return;
        }

        int index = history.FindIndex(record => record.Sequence == state.lastInputSequence);
        if (index < 0)
        {
            Vector3 gap = state.position - transform.position;
            if (gap.magnitude > snapDistance || (!movement.CanMove && gap.magnitude > 0.01f))
                Snap(state.position);
            return;
        }

        Vector3 error = state.position - history[index].Position;
        history.RemoveRange(0, index + 1);
        float distance = error.magnitude;

        if (distance > snapDistance || (!movement.CanMove && distance > 0.01f))
        {
            Snap(state.position);
            return;
        }

        if (distance < correctionTolerance)
            return;

        for (int i = 0; i < history.Count; i++)
        {
            PredictionRecord record = history[i];
            record.Position += error;
            history[i] = record;
        }
        pendingCorrection += error;
    }

    private void Snap(Vector3 position)
    {
        movement.Teleport(position);
        history.Clear();
        pendingCorrection = Vector3.zero;
    }

    private void ApplyCorrectionStep()
    {
        if (pendingCorrection.sqrMagnitude < 0.000001f)
            return;

        Vector3 step = pendingCorrection * (1f - Mathf.Exp(-correctionSpeed * Time.deltaTime));
        pendingCorrection -= step;
        movement.AddCorrection(step);
    }
}
