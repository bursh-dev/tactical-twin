using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns targets ahead of the player as they navigate.
/// One target at a time, always in front — surprise style.
/// </summary>
public class TargetManager : MonoBehaviour
{
    [Header("Spawning")]
    public float spawnInterval = 5f;
    public float spawnIntervalVariance = 3f;
    public float spawnDistanceMin = 4f;
    public float spawnDistanceMax = 10f;
    public float forwardConeAngle = 60f;
    public int maxActiveTargets = 1;
    public float defaultRadius = 0.3f;

    [Header("Audio")]
    public AudioClip spawnSound;

    [Header("References")]
    public Transform playerTransform;

    private List<GameObject> spawnedTargets = new List<GameObject>();
    private AudioSource audioSource;
    private float nextSpawnTime;
    private int totalSpawned;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (playerTransform == null)
        {
            var player = GameObject.Find("Player");
            if (player != null) playerTransform = player.transform;
        }

        nextSpawnTime = Time.time + 3f;
    }

    void Update()
    {
        if (playerTransform == null) return;

        CleanupHitTargets();

        int activeCount = CountActiveTargets();
        if (activeCount < maxActiveTargets && Time.time >= nextSpawnTime)
        {
            SpawnForwardTarget();
            nextSpawnTime = Time.time + spawnInterval + Random.Range(-spawnIntervalVariance, spawnIntervalVariance);
            nextSpawnTime = Mathf.Max(nextSpawnTime, Time.time + 2f);
        }
    }

    void SpawnForwardTarget()
    {
        // Spawn within a cone in front of the player
        float halfCone = forwardConeAngle * 0.5f;
        float angleOffset = Random.Range(-halfCone, halfCone);

        // Get player's forward direction (yaw only)
        Vector3 forward = playerTransform.forward;
        forward.y = 0f;
        forward.Normalize();

        // Rotate forward by random angle within cone
        Quaternion rotation = Quaternion.Euler(0f, angleOffset, 0f);
        Vector3 direction = rotation * forward;

        float distance = Random.Range(spawnDistanceMin, spawnDistanceMax);
        float heightVar = Random.Range(-1f, 1f);

        Vector3 spawnPos = playerTransform.position + direction * distance + Vector3.up * heightVar;

        // Create sphere target
        GameObject target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        target.transform.position = spawnPos;
        target.transform.parent = transform;

        float radius = Random.Range(0.2f, 0.35f);
        target.transform.localScale = Vector3.one * radius * 2f;
        target.layer = gameObject.layer;

        if (target.GetComponent<Target>() == null)
            target.AddComponent<Target>();

        spawnedTargets.Add(target);
        totalSpawned++;

        if (spawnSound != null)
            audioSource.PlayOneShot(spawnSound, 0.7f);
    }

    void CleanupHitTargets()
    {
        for (int i = spawnedTargets.Count - 1; i >= 0; i--)
        {
            if (spawnedTargets[i] == null)
            {
                spawnedTargets.RemoveAt(i);
                continue;
            }

            var target = spawnedTargets[i].GetComponent<Target>();
            if (target != null && target.isHit && target.TimeSinceHit > 1.5f)
            {
                Destroy(spawnedTargets[i]);
                spawnedTargets.RemoveAt(i);
            }
        }
    }

    int CountActiveTargets()
    {
        int count = 0;
        foreach (var t in spawnedTargets)
        {
            if (t == null) continue;
            var target = t.GetComponent<Target>();
            if (target != null && !target.isHit) count++;
        }
        return count;
    }

    public void ClearTargets()
    {
        foreach (var t in spawnedTargets)
        {
            if (t != null) Destroy(t);
        }
        spawnedTargets.Clear();
    }

    public int GetTotalCount() => totalSpawned;

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
