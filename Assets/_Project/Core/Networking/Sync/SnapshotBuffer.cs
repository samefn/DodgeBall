using System.Collections.Generic;
using UnityEngine;

public class SnapshotBuffer
{
    private struct Sample
    {
        public float Time;
        public Vector3 Position;
        public float Yaw;
        public float Pitch;
    }

    private const int Capacity = 32;

    private readonly List<Sample> samples = new List<Sample>(Capacity);
    private readonly float teleportDistance;
    private readonly float maxExtrapolation;

    public int Count => samples.Count;

    public SnapshotBuffer(float teleportDistance, float maxExtrapolation)
    {
        this.teleportDistance = teleportDistance;
        this.maxExtrapolation = maxExtrapolation;
    }

    public void Clear()
    {
        samples.Clear();
    }

    public void Add(float time, Vector3 position, float yaw = 0f, float pitch = 0f)
    {
        if (samples.Count > 0)
        {
            Sample last = samples[samples.Count - 1];
            if (time <= last.Time)
                return;

            if ((position - last.Position).sqrMagnitude > teleportDistance * teleportDistance)
                samples.Clear();
        }

        samples.Add(new Sample { Time = time, Position = position, Yaw = yaw, Pitch = pitch });
        if (samples.Count > Capacity)
            samples.RemoveAt(0);
    }

    public bool TrySample(float renderTime, out Vector3 position, out float yaw, out float pitch)
    {
        position = Vector3.zero;
        yaw = 0f;
        pitch = 0f;
        if (samples.Count == 0)
            return false;

        Sample first = samples[0];
        if (samples.Count == 1 || renderTime <= first.Time)
        {
            Read(first, out position, out yaw, out pitch);
            return true;
        }

        for (int i = samples.Count - 2; i >= 0; i--)
        {
            Sample from = samples[i];
            if (from.Time > renderTime)
                continue;

            Sample to = samples[i + 1];
            if (renderTime <= to.Time)
            {
                float blend = Mathf.InverseLerp(from.Time, to.Time, renderTime);
                position = Vector3.Lerp(from.Position, to.Position, blend);
                yaw = Mathf.LerpAngle(from.Yaw, to.Yaw, blend);
                pitch = Mathf.Lerp(from.Pitch, to.Pitch, blend);
            }
            else
            {
                Extrapolate(to, renderTime, out position, out yaw, out pitch);
            }

            if (i > 0)
                samples.RemoveRange(0, i);
            return true;
        }

        Read(first, out position, out yaw, out pitch);
        return true;
    }

    private void Extrapolate(Sample last, float renderTime, out Vector3 position, out float yaw, out float pitch)
    {
        Sample previous = samples[samples.Count - 2];
        float span = Mathf.Max(last.Time - previous.Time, 0.0001f);
        Vector3 velocity = (last.Position - previous.Position) / span;
        float ahead = Mathf.Min(renderTime - last.Time, maxExtrapolation);

        position = last.Position + velocity * ahead;
        yaw = last.Yaw;
        pitch = last.Pitch;
    }

    private static void Read(Sample sample, out Vector3 position, out float yaw, out float pitch)
    {
        position = sample.Position;
        yaw = sample.Yaw;
        pitch = sample.Pitch;
    }
}
