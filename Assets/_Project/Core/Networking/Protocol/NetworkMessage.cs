using System;
using UnityEngine;

[Serializable]
public class NetworkMessage
{
    public MessageType type;
    public int senderId;
    public int sequence;
    public string payload;

    public string Serialize()
    {
        return JsonUtility.ToJson(this);
    }

    public T ReadPayload<T>()
    {
        return JsonUtility.FromJson<T>(payload);
    }

    public static bool TryParse(string json, out NetworkMessage message)
    {
        message = null;
        try
        {
            message = JsonUtility.FromJson<NetworkMessage>(json);
        }
        catch (ArgumentException)
        {
            return false;
        }

        return message != null && Enum.IsDefined(typeof(MessageType), message.type);
    }
}
