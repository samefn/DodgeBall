using System;
using System.Collections.Generic;
using System.Text;

public class FrameReader
{
    private readonly List<byte> pending = new List<byte>();

    public void Append(byte[] data, int count)
    {
        pending.AddRange(new ArraySegment<byte>(data, 0, count));
    }

    public bool TryRead(out string message)
    {
        message = null;
        if (pending.Count < MessageFraming.HeaderSize)
            return false;

        int length = MessageFraming.ReadLength(pending[0], pending[1], pending[2], pending[3]);
        if (length < 0 || length > MessageFraming.MaxMessageSize)
            throw new InvalidOperationException($"Invalid frame length: {length}");

        int frameSize = MessageFraming.HeaderSize + length;
        if (pending.Count < frameSize)
            return false;

        byte[] body = pending.GetRange(MessageFraming.HeaderSize, length).ToArray();
        pending.RemoveRange(0, frameSize);
        message = Encoding.UTF8.GetString(body);
        return true;
    }
}
