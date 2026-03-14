using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns targets from a JSON config file.
/// </summary>
public class TargetManager : MonoBehaviour
{
    [Header("Target Prefab")]
    public GameObject targetPrefab;
    public float defaultRadius = 0.3f;

    [Header("Config")]
    public TextAsset targetConfigFile;

    private List<GameObject> spawnedTargets = new List<GameObject>();

    [System.Serializable]
    public class TargetData
    {
        public float[] position;
        public float radius;
    }

    [System.Serializable]
    public class RoomData
    {
        public string name;
        public string splatFile;
        public TargetData[] targets;
    }

    [System.Serializable]
    public class TargetConfig
    {
        public RoomData[] rooms;
    }

    void Start()
    {
        if (targetConfigFile != null)
            LoadTargets(targetConfigFile.text);
    }

    public void LoadTargets(string json)
    {
        ClearTargets();

        TargetConfig config = JsonUtility.FromJson<TargetConfig>(json);
        if (config?.rooms == null) return;

        foreach (var room in config.rooms)
        {
            if (room.targets == null) continue;

            foreach (var targetData in room.targets)
            {
                SpawnTarget(targetData);
            }
        }

        Debug.Log($"Spawned {spawnedTargets.Count} targets");
    }

    void SpawnTarget(TargetData data)
    {
        Vector3 pos = new Vector3(data.position[0], data.position[1], data.position[2]);
        float radius = data.radius > 0 ? data.radius : defaultRadius;

        GameObject target;
        if (targetPrefab != null)
        {
            target = Instantiate(targetPrefab, pos, Quaternion.identity, transform);
        }
        else
        {
            // Default: create a sphere
            target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            target.transform.position = pos;
            target.transform.parent = transform;
        }

        target.transform.localScale = Vector3.one * radius * 2f;
        target.layer = gameObject.layer;

        // Add Target component if not on prefab
        if (target.GetComponent<Target>() == null)
            target.AddComponent<Target>();

        spawnedTargets.Add(target);
    }

    public void ClearTargets()
    {
        foreach (var t in spawnedTargets)
        {
            if (t != null) Destroy(t);
        }
        spawnedTargets.Clear();
    }

    public void ResetAllTargets()
    {
        foreach (var t in spawnedTargets)
        {
            var target = t?.GetComponent<Target>();
            target?.Reset();
        }
    }

    public int GetTotalCount() => spawnedTargets.Count;

    public int GetHitCount()
    {
        int count = 0;
        foreach (var t in spawnedTargets)
        {
            var target = t?.GetComponent<Target>();
            if (target != null && target.isHit) count++;
        }
        return count;
    }
}
