using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Round-based shooting game with surprise events.
/// A round = N events. Each event spawns 1-3 targets simultaneously.
/// Score = accuracy × speed multiplier.
/// </summary>
public class TargetManager : MonoBehaviour
{
    [Header("Round Settings")]
    public int eventsPerRound = 5;
    public int minTargetsPerEvent = 1;
    public int maxTargetsPerEvent = 3;
    public float targetTimeout = 6f;
    public float minPauseBetweenEvents = 3f;
    public float maxPauseBetweenEvents = 7f;

    [Header("Spawn Settings")]
    public float spawnDistanceMin = 4f;
    public float spawnDistanceMax = 10f;
    public float forwardConeAngle = 60f;

    [Header("Audio")]
    public AudioClip spawnSound;

    [Header("References")]
    public Transform playerTransform;

    public enum RoundState { WaitingToStart, Playing, EventActive, RoundOver }
    public RoundState State { get; private set; } = RoundState.WaitingToStart;

    // Round stats
    public int EventsCompleted { get; private set; }
    public int TotalTargetsSpawned { get; private set; }
    public int TargetsHit { get; private set; }
    public int TargetsMissed { get; private set; }
    public int TotalScore { get; private set; }
    public float RoundStartTime { get; private set; }
    public float RoundEndTime { get; private set; }

    // Current event
    public int CurrentEventTargetCount { get; private set; }
    public int CurrentEventTargetsCleared { get; private set; }
    public float CurrentEventStartTime { get; private set; }

    private List<GameObject> activeTargets = new List<GameObject>();
    private List<int> hitScores = new List<int>();
    private List<float> hitSpeeds = new List<float>();
    private AudioSource audioSource;
    private float nextEventTime;

    private RoomCalibrator roomCalibrator;

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

        roomCalibrator = FindFirstObjectByType<RoomCalibrator>();
    }

    void Update()
    {
        if (playerTransform == null) return;

        // Block input during calibration
        if (roomCalibrator != null && roomCalibrator.IsCalibrating) return;

        if (State == RoundState.WaitingToStart || State == RoundState.RoundOver)
        {
            // C to recalibrate room
            if (Input.GetKeyDown(KeyCode.C) && roomCalibrator != null)
            {
                roomCalibrator.EnterCalibration();
                return;
            }
            if (Input.GetKeyDown(KeyCode.R))
                StartRound();
            return;
        }

        // Playing state — waiting between events
        if (State == RoundState.Playing)
        {
            if (Time.time >= nextEventTime)
                StartEvent();
            return;
        }

        // EventActive — targets are live
        if (State == RoundState.EventActive)
        {
            CheckEventTargets();
        }
    }

    public void StartRound()
    {
        ClearTargets();
        EventsCompleted = 0;
        TotalTargetsSpawned = 0;
        TargetsHit = 0;
        TargetsMissed = 0;
        TotalScore = 0;
        hitScores.Clear();
        hitSpeeds.Clear();
        RoundStartTime = Time.time;
        State = RoundState.Playing;
        // First event after a short delay
        nextEventTime = Time.time + 2f;
    }

    void StartEvent()
    {
        int targetCount = Random.Range(minTargetsPerEvent, maxTargetsPerEvent + 1);
        CurrentEventTargetCount = targetCount;
        CurrentEventTargetsCleared = 0;
        CurrentEventStartTime = Time.time;

        // Spawn targets spread across the forward cone
        for (int i = 0; i < targetCount; i++)
        {
            SpawnTarget(i, targetCount);
        }

        TotalTargetsSpawned += targetCount;
        State = RoundState.EventActive;

        if (spawnSound != null)
            audioSource.PlayOneShot(spawnSound, 0.7f);
    }

    void SpawnTarget(int index, int total)
    {
        // Spawn target in ±20° arc in front of the player
        Vector3 forward = playerTransform.forward;
        forward.y = 0f;
        forward.Normalize();
        float baseAngle = Mathf.Atan2(forward.x, forward.z) * Mathf.Rad2Deg;
        float angle = baseAngle + Random.Range(-20f, 20f);

        Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
        Vector3 direction = rotation * Vector3.forward;

        float distance = Random.Range(spawnDistanceMin, spawnDistanceMax);
        float heightVar = Random.Range(-0.2f, 0.2f);

        Vector3 spawnPos = playerTransform.position + direction * distance + Vector3.up * heightVar;

        GameObject target = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        target.transform.position = spawnPos;
        target.transform.parent = transform;

        // Scale target size with distance — smaller when close, bigger when far
        float t01 = Mathf.InverseLerp(spawnDistanceMin, spawnDistanceMax, distance);
        float radius = Mathf.Lerp(0.03f, 0.08f, t01);
        target.transform.localScale = Vector3.one * radius * 2f;
        target.layer = gameObject.layer;

        var t = target.AddComponent<Target>();
        t.spawnTime = Time.time;
        t.timeout = targetTimeout;

        activeTargets.Add(target);
    }

    void CheckEventTargets()
    {
        bool allDone = true;

        for (int i = activeTargets.Count - 1; i >= 0; i--)
        {
            if (activeTargets[i] == null)
            {
                activeTargets.RemoveAt(i);
                continue;
            }

            var target = activeTargets[i].GetComponent<Target>();
            if (target == null) continue;

            if (target.isHit && target.TimeSinceHit > 1.2f)
            {
                Destroy(activeTargets[i]);
                activeTargets.RemoveAt(i);
            }
            else if (!target.isHit && target.IsTimedOut)
            {
                TargetsMissed++;
                CurrentEventTargetsCleared++;
                Destroy(activeTargets[i]);
                activeTargets.RemoveAt(i);
            }
            else if (!target.isHit)
            {
                allDone = false;
            }
        }

        // All targets in this event are resolved
        if (allDone && activeTargets.Count == 0)
        {
            EventsCompleted++;

            if (EventsCompleted >= eventsPerRound)
            {
                State = RoundState.RoundOver;
                RoundEndTime = Time.time;
            }
            else
            {
                State = RoundState.Playing;
                float pause = Random.Range(minPauseBetweenEvents, maxPauseBetweenEvents);
                nextEventTime = Time.time + pause;
            }
        }
    }

    /// <summary>
    /// Called by ShootingSystem when a target is hit.
    /// Calculates combined score from accuracy and speed.
    /// </summary>
    public void RecordHit(int accuracyScore)
    {
        TargetsHit++;
        CurrentEventTargetsCleared++;

        // Speed: how fast since event started
        float reactionTime = Time.time - CurrentEventStartTime;
        float speedMultiplier = CalculateSpeedMultiplier(reactionTime);

        int combinedScore = Mathf.RoundToInt(accuracyScore * speedMultiplier);
        combinedScore = Mathf.Max(combinedScore, 1);

        TotalScore += combinedScore;
        hitScores.Add(combinedScore);
        hitSpeeds.Add(reactionTime);
    }

    /// <summary>
    /// Speed multiplier: 2.0x for instant shots, decays to 1.0x at timeout.
    /// </summary>
    float CalculateSpeedMultiplier(float reactionTime)
    {
        // Under 1s = 2.0x, at timeout = 1.0x, linear interpolation
        float t = Mathf.Clamp01(reactionTime / targetTimeout);
        return Mathf.Lerp(2f, 1f, t);
    }

    public void ClearTargets()
    {
        foreach (var t in activeTargets)
        {
            if (t != null) Destroy(t);
        }
        activeTargets.Clear();
    }

    // Stats accessors
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

    public float GetAvgReactionTime()
    {
        if (hitSpeeds.Count == 0) return 0f;
        float sum = 0;
        foreach (float s in hitSpeeds) sum += s;
        return sum / hitSpeeds.Count;
    }

    public float GetBestReactionTime()
    {
        if (hitSpeeds.Count == 0) return 0f;
        float best = float.MaxValue;
        foreach (float s in hitSpeeds)
            if (s < best) best = s;
        return best;
    }

    public int GetMaxPossibleScore()
    {
        // Max = every target hit instantly (accuracy 10 × speed 2.0)
        return TotalTargetsSpawned * 20;
    }

    public string GetGrade()
    {
        if (TotalTargetsSpawned == 0) return "-";
        float hitRate = (float)TargetsHit / TotalTargetsSpawned;
        float avgScore = GetAvgScore();
        float maxAvg = 20f; // max possible per-target score
        float combined = hitRate * 0.5f + (avgScore / maxAvg) * 0.5f;

        if (combined >= 0.9f) return "S";
        if (combined >= 0.8f) return "A";
        if (combined >= 0.65f) return "B";
        if (combined >= 0.5f) return "C";
        if (combined >= 0.35f) return "D";
        return "F";
    }

    // Legacy accessors for HUD compatibility
    public int TargetsSpawned => TotalTargetsSpawned;
    public int targetsPerRound => eventsPerRound;
}
