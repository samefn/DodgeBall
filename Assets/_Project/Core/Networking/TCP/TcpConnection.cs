using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class TcpConnection
{
    private const int MaxPendingFrames = 2048;
    private const int ReceiveBufferSize = 64 * 1024;
    private const int MaxBatchSize = 64 * 1024;

    private readonly TcpClient client;
    private readonly NetworkStream stream;
    private readonly ConcurrentQueue<byte[]> outbox = new ConcurrentQueue<byte[]>();
    private readonly SemaphoreSlim outboxSignal = new SemaphoreSlim(0);
    private int closed;
    private volatile bool closeAfterFlush;

    public int Id { get; }
    public string RemoteAddress { get; }
    public bool IsOpen => Volatile.Read(ref closed) == 0;

    public event Action<TcpConnection, string> MessageReceived;
    public event Action<TcpConnection> Closed;

    public TcpConnection(int id, TcpClient tcpClient)
    {
        Id = id;
        client = tcpClient;
        client.NoDelay = true;
        stream = client.GetStream();
        RemoteAddress = client.Client.RemoteEndPoint is IPEndPoint endPoint ? NormalizeAddress(endPoint.Address) : "?";
    }

    public void Start()
    {
        _ = ReceiveLoop();
        _ = SendLoop();
    }

    public bool Enqueue(byte[] frame)
    {
        if (!IsOpen || closeAfterFlush)
            return false;

        if (outbox.Count >= MaxPendingFrames)
        {
            Debug.LogWarning($"[TCP] Connection {Id} has {outbox.Count} pending messages; closing it.");
            Close();
            return false;
        }

        outbox.Enqueue(frame);
        outboxSignal.Release();
        return true;
    }

    public void CloseAfterFlush()
    {
        if (!IsOpen)
            return;

        closeAfterFlush = true;
        outboxSignal.Release();
    }

    public void Close()
    {
        if (Interlocked.Exchange(ref closed, 1) != 0)
            return;

        outboxSignal.Release();
        try { stream.Close(); } catch (Exception) { }
        try { client.Close(); } catch (Exception) { }
        Closed?.Invoke(this);
    }

    private async Task SendLoop()
    {
        byte[] batch = new byte[MaxBatchSize];
        try
        {
            while (IsOpen)
            {
                await outboxSignal.WaitAsync().ConfigureAwait(false);
                while (IsOpen && !outbox.IsEmpty)
                {
                    int length = FillBatch(ref batch);
                    if (length > 0)
                        await stream.WriteAsync(batch, 0, length).ConfigureAwait(false);
                }

                if (closeAfterFlush && outbox.IsEmpty)
                {
                    await stream.FlushAsync().ConfigureAwait(false);
                    await Task.Delay(100).ConfigureAwait(false);
                    break;
                }
            }
        }
        catch (Exception e) when (e is IOException || e is ObjectDisposedException || e is SocketException || e is InvalidOperationException)
        {
            if (IsOpen)
                Debug.Log($"[TCP] Send on connection {Id} failed: {e.Message}");
        }
        finally
        {
            Close();
        }
    }

    private int FillBatch(ref byte[] batch)
    {
        int length = 0;
        while (outbox.TryPeek(out byte[] frame))
        {
            if (length > 0 && length + frame.Length > batch.Length)
                break;
            if (frame.Length > batch.Length)
                batch = new byte[frame.Length];

            outbox.TryDequeue(out frame);
            Buffer.BlockCopy(frame, 0, batch, length, frame.Length);
            length += frame.Length;
        }
        return length;
    }

    private async Task ReceiveLoop()
    {
        byte[] buffer = new byte[ReceiveBufferSize];
        FrameReader reader = new FrameReader();

        try
        {
            while (IsOpen)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
                if (bytesRead == 0)
                    break;

                reader.Append(buffer, bytesRead);
                while (reader.TryRead(out string message))
                    MessageReceived?.Invoke(this, message);
            }
        }
        catch (Exception e) when (e is IOException || e is ObjectDisposedException || e is SocketException)
        {
            if (IsOpen)
                Debug.Log($"[TCP] Connection {Id} closed: {e.Message}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[TCP] Connection {Id} dropped: {e.Message}");
        }
        finally
        {
            Close();
        }
    }

    public static string NormalizeAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
            address = address.MapToIPv4();
        return address.ToString();
    }
}
