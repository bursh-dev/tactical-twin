using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Round-based shooting game. Spawns a set number of targets one at a time.
/// Shows summary screen at the end of the round.
/// </summary>
public class TargetManager : MonoBehaviour
{
    [Header("Round Settings")]
    public int targetsPerRound = 20;
    public float spawnInterval = 4f;
    public float spawnIntervalVariance = 2f;
    public float spawnDistanceMin = 4f;
    public float spawnDistanceMax = 10f;
    public float forwardConeAngle = 60f;
    public float targetTimeout = 8f;

    [Header("Audio")]
    public AudioClip spawnSound;

    [Header("References")]
    public Transform playerTransform;

    public enum RoundState { WaitingToStart, Playing, RoundOver }
    public RoundState State { get; private set; } = RoundState.WaitingToStart;

    // Round stats
    public int TargetsSpawned { get; private set; }
    public int TargetsHit { get; private set; }
    public int TargetsMissed { get; private set; }
    public int TotalScore { get; private set; }
    public float RoundStartTime { get; private set; }
    public float RoundEndTime { get; private set; }

    private List<GameObject> spawnedTargets = new List<GameObject>();
    private List<int> hitScores = new List<int>();
    private AudioSource audioSource;
    private float nextSpawnTime;

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
    }

    void Update()
    {
        if (playerTransform == null) return;

        if (State == RoundState.WaitingToStart)
        {
            if (Input.GetKeyDown(KeyCode.R))
                StartRound();
            return;
        }

        if (State == RoundState.RoundOver)
        {
            if (Input.GetKeyDown(KeyCode.R))
                StartRound();
            return;
        }

        // Playing state
        CleanupTargets();

        int activeCount = CountActiveTargets();
        bool allSpawned = TargetsSpawned >= targetsPerRound;

        if (!allSpawned && activeCount == 0 && Time.time >= nextSpawnTime)
        {
            SpawnForwardTarget();
            float interval = spawnInterval + Random.Range(-spawnIntervalVariance, spawnIntervalVariance);
            nextSpawnTime = Time.time + Mathf.Max(interval, 1.5f);
        }

        // Check round end
        if (allSpawned && activeCount == 0)
        {
            State = RoundState.RoundOver;
            RoundEndTime = Time.time;
        }
    }

    public void StartRound()
    {
        ClearTargets();
        TargetsSpawned = 0;
        TargetsHit = 0;
        TargetsMissed = 0;
        TotalScore = 0;
        hitScores.Clear();
        RoundStartTime = Time.time;
        State = RoundState.Playing;
        nextSpawnTime = Time.time + 1.5f;
    }

    void SpawnForwardTarget()
    {
        float halfCone = forwardConeAngle * 0.5f;
        float angleOffset = Random.Range(-halfCone, halfCone);

        Vector3 forward = playerTransform.forward;
        forward.y = 0f;
        forward.Normalize();

        Quaternion rotation = Quaternion.Euler(0f, angleOffset, 0f);
        Vector3 direction = rotation * forward;

        float distance = Random.Range(spawnDistanceMin, spawnDistanceMax);
        float heightVar = Random.Range(-1f, 1f);

        Vector3 spawnPos = playerTransform.position + direction * distance + Vector3.up * heightVar;

        GameObject target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        target.transform.position = spawnPos;
        target.transform.parent = transform;

        float radius = Random.Range(0.2f, 0.35f);
        target.transform.localScale = Vector3.one * radius * 2f;
        target.layer = gameObject.layer;

        var t = target.AddComponent<Target>();
        t.spawnTime = Time.time;
        t.timeout = targetTimeout;

        spawnedTargets.Add(target);
        TargetsSpawned++;

        if (spawnSound != null)
            audioSource.PlayOneShot(spawnSound, 0.7f);
    }

    void CleanupTargets()
    {
        for (int i = spawnedTargets.Count - 1; i >= 0; i--)
        {
            if (spawnedTargets[i] == null)
            {
                spawnedTargets.RemoveAt(i);
                continue;
            }

            var target = spawnedTargets[i].GetComponent<Target>();
            if (target == null) continue;

            // Hit target — remove after delay
            if (target.isHit && target.TimeSinceHit > 1.5f)
            {
                Destroy(spawnedTargets[i]);
                spawnedTargets.RemoveAt(i);
            }
            // Timed out — count as missed
            else if (!target.isHit && target.IsTimedOut)
            {
                TargetsMissed++;
                Destroy(spawnedTargets[i]);
                spawnedTargets.RemoveAt(i);
            }
        }
    }

    public void RecordHit(int score)
    {
        TargetsHit++;
        TotalScore += score;
        hitScores.Add(score);
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

    public float GetAvgScore()
    {
        if (hitScores.Count == 0) return 0f;
        float sum = 0;
        foreach (int s in hitScores) sum += s;
        return sum / hitScores.Count;
    }

    public int GetBestScore()
    {
        int best = 0;
        foreach (int s in hitScores)
            if (s > best) best = s;
        return best;
    }

    public string GetGrade()
    {
        if (TargetsSpawned == 0) return "-";
        float hitRate = (float)TargetsHit / TargetsSpawned;
        float avgScore = GetAvgScore();
        float combined = hitRate * 0.5f + (avgScore / 10f) * 0.5f;

        if (combined >= 0.9f) return "S";
        if (combined >= 0.8f) return "A";
        if (combined >= 0.65f) return "B";
        if (combined >= 0.5f) return "C";
        if (combined >= 0.35f) return "D";
        return "F";
    }
}
