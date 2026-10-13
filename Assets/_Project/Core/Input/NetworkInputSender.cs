using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(50)]
public class NetworkInputSender : MonoBehaviour
{
    [SerializeField] private float sendRate = 60f;

    private readonly List<PlayerInputState> pending = new List<PlayerInputState>();
    private IPlayerInputSource source;
    private NetworkPlayerView predictedView;
    private float sendTimer;
    private int sequence;

    private void Awake()
    {
        source = GetComponent<IPlayerInputSource>();
    }

    private void Update()
    {
        if (source == null)
            source = GetComponent<IPlayerInputSource>();
        if (predictedView == null)
            predictedView = GetComponent<NetworkPlayerView>();
        if (source == null || NetworkService.Instance == null || !NetworkService.Instance.IsClient)
            return;

        PlayerInputState input = source.Current;
        input.sequence = ++sequence;
        input.deltaTime = Time.deltaTime;
        pending.Add(input);
        if (predictedView != null)
            predictedView.RecordPrediction(input.sequence);

        sendTimer += Time.unscaledDeltaTime;
        if (sendTimer < 1f / sendRate)
            return;

        sendTimer = 0f;
        NetworkService.Instance.Send(MessageType.PlayerInputBatch, new InputBatchPayload { inputs = pending.ToArray() });
        pending.Clear();
    }
}
