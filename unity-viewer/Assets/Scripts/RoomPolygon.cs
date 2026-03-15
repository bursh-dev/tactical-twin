using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Polygon data for room walls. Handles serialization and point-in-polygon test.
/// </summary>
[Serializable]
public class RoomPolygon
{
    public string roomName;
    public float floorY;
    public float wallHeight = 5f;
    public List<PolygonCorner> corners = new List<PolygonCorner>();

    [Serializable]
    public class PolygonCorner
    {
        public float x;
        public float z;

        public PolygonCorner(float x, float z)
        {
            this.x = x;
            this.z = z;
        }
    }

    /// <summary>
    /// Ray-casting point-in-polygon test on the XZ plane.
    /// </summary>
    public bool IsInside(Vector3 worldPos)
    {
        if (corners.Count < 3) return true; // No polygon = no restriction

        bool inside = false;
        int j = corners.Count - 1;
        for (int i = 0; i < corners.Count; j = i++)
        {
            float iz = corners[i].z, jz = corners[j].z;
            float ix = corners[i].x, jx = corners[j].x;

            if ((iz > worldPos.z) != (jz > worldPos.z) &&
                worldPos.x < (jx - ix) * (worldPos.z - iz) / (jz - iz) + ix)
                inside = !inside;
        }
        return inside;
    }

    public string ToJson()
    {
        return JsonUtility.ToJson(this, true);
    }

    public static RoomPolygon FromJson(string json)
    {
        return JsonUtility.FromJson<RoomPolygon>(json);
    }
}
