using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serializable data for room wall blocks.
/// Each block stores 4 corner points (12 floats) for a wall quad.
/// </summary>
[Serializable]
public class RoomBlockData
{
    public string roomName;
    public float wallHeight = 3f;
    public List<BlockEntry> blocks = new List<BlockEntry>();

    [Serializable]
    public class BlockEntry
    {
        // Point 0
        public float posX, posY, posZ;
        // Point 1
        public float scaleX, scaleY, scaleZ;
        // Point 2
        public float rotY, extra1, extra2;
        // Point 3
        public float extra3, extra4, extra5;
    }

    public string ToJson()
    {
        return JsonUtility.ToJson(this, true);
    }

    public static RoomBlockData FromJson(string json)
    {
        return JsonUtility.FromJson<RoomBlockData>(json);
    }
}
