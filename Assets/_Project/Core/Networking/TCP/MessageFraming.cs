using System;
using System.Text;

public static class MessageFraming
{
    public const int HeaderSize = 4;
    public const int MaxMessageSize = 64 * 1024;

    public static byte[] Frame(string message)
    {
        byte[] body = Encoding.UTF8.GetBytes(message);
        if (body.Length > MaxMessageSize)
            throw new ArgumentException($"Message exceeds {MaxMessageSize} bytes");

        byte[] frame = new byte[HeaderSize + body.Length];
        frame[0] = (byte)(body.Length >> 24);
        frame[1] = (byte)(body.Length >> 16);
        frame[2] = (byte)(body.Length >> 8);
        frame[3] = (byte)body.Length;
        Buffer.BlockCopy(body, 0, frame, HeaderSize, body.Length);
        return frame;
    }

    public static int ReadLength(byte b0, byte b1, byte b2, byte b3)
    {
        return (b0 << 24) | (b1 << 16) | (b2 << 8) | b3;
    }
}
