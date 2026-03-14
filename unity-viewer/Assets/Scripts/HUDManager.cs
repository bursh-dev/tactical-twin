using UnityEngine;

/// <summary>
/// HUD for round-based shooting game.
/// Shows live stats during play, summary screen after round.
/// </summary>
public class HUDManager : MonoBehaviour
{
    [Header("References")]
    public TargetManager targetManager;

    [Header("Crosshair")]
    public int crosshairSize = 10;
    public int crosshairThickness = 2;
    public Color crosshairColor = Color.white;

    private GUIStyle labelStyle;
    private GUIStyle smallLabelStyle;
    private GUIStyle titleStyle;
    private GUIStyle instructionStyle;
    private GUIStyle hitScoreStyle;
    private GUIStyle gradeStyle;
    private GUIStyle summaryStyle;
    private Texture2D crosshairTexture;
    private Texture2D bgTexture;

    // Floating hit score
    private string floatingText;
    private float floatingTimer;
    private Vector3 floatingWorldPos;
    private Color floatingColor;

    void Start()
    {
        crosshairTexture = new Texture2D(1, 1);
        crosshairTexture.SetPixel(0, 0, crosshairColor);
        crosshairTexture.Apply();

        bgTexture = new Texture2D(1, 1);
        bgTexture.SetPixel(0, 0, new Color(0, 0, 0, 0.7f));
        bgTexture.Apply();

        if (targetManager == null)
            targetManager = FindFirstObjectByType<TargetManager>();
    }

    public void ShowHitScore(int points, Vector3 worldPos)
    {
        floatingText = $"+{points}";
        if (points == 10) floatingText = "+10 BULLSEYE!";
        else if (points >= 8) floatingText = $"+{points} Great!";

        floatingWorldPos = worldPos;
        floatingTimer = 1.2f;
        floatingColor = points >= 8 ? Color.yellow : points >= 5 ? Color.white : Color.gray;
    }

    void OnGUI()
    {
        InitStyles();

        if (targetManager == null) return;

        switch (targetManager.State)
        {
            case TargetManager.RoundState.WaitingToStart:
                DrawStartScreen();
                break;
            case TargetManager.RoundState.Playing:
                DrawCrosshair();
                DrawPlayingHUD();
                DrawFloatingScore();
                break;
            case TargetManager.RoundState.RoundOver:
                DrawSummaryScreen();
                break;
        }
    }

    void InitStyles()
    {
        if (labelStyle != null) return;

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
        };
        labelStyle.normal.textColor = Color.white;

        smallLabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
        };
        smallLabelStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 36,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };
        titleStyle.normal.textColor = Color.white;

        instructionStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            alignment = TextAnchor.MiddleCenter,
        };
        instructionStyle.normal.textColor = new Color(0.9f, 0.9f, 0.6f);

        hitScoreStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };

        gradeStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 72,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };

        summaryStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            alignment = TextAnchor.MiddleLeft,
        };
        summaryStyle.normal.textColor = Color.white;
    }

    void DrawStartScreen()
    {
        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;

        GUI.DrawTexture(new Rect(cx - 250, cy - 120, 500, 240), bgTexture);

        GUI.Label(new Rect(cx - 250, cy - 100, 500, 50), "TACTICAL TWIN", titleStyle);
        GUI.Label(new Rect(cx - 250, cy - 40, 500, 40), "Shooting Range", instructionStyle);
        GUI.Label(new Rect(cx - 250, cy + 10, 500, 40), $"{targetManager.targetsPerRound} targets | {targetManager.targetTimeout}s per target", instructionStyle);
        GUI.Label(new Rect(cx - 250, cy + 60, 500, 40), "Press R to start round", instructionStyle);
        GUI.Label(new Rect(cx - 250, cy + 90, 500, 30), "WASD move | Mouse look | Q/E up/down", smallLabelStyle);
        smallLabelStyle.alignment = TextAnchor.MiddleCenter;
        GUI.Label(new Rect(cx - 250, cy + 90, 500, 30), "WASD move | Mouse look | Q/E up/down", smallLabelStyle);
        smallLabelStyle.alignment = TextAnchor.MiddleLeft;
    }

    void DrawPlayingHUD()
    {
        float y = 10f;
        float x = 10f;

        int remaining = targetManager.targetsPerRound - targetManager.TargetsSpawned;
        GUI.Label(new Rect(x, y, 300, 30), $"Score: {targetManager.TotalScore}", labelStyle);
        y += 28f;

        float elapsed = Time.time - targetManager.RoundStartTime;
        int minutes = (int)(elapsed / 60f);
        int seconds = (int)(elapsed % 60f);
        GUI.Label(new Rect(x, y, 300, 30), $"Time: {minutes:D2}:{seconds:D2}", labelStyle);
        y += 28f;

        GUI.Label(new Rect(x, y, 300, 25),
            $"Targets: {targetManager.TargetsHit} hit / {targetManager.TargetsSpawned} of {targetManager.targetsPerRound}",
            smallLabelStyle);
        y += 22f;

        if (targetManager.TargetsHit > 0)
        {
            GUI.Label(new Rect(x, y, 300, 25),
                $"Avg: {targetManager.GetAvgScore():F1}/10 | Best: {targetManager.GetBestScore()}/10",
                smallLabelStyle);
        }

        // Show remaining targets count on right side
        string remainText = remaining > 0 ? $"{remaining} left" : "Last target!";
        labelStyle.alignment = TextAnchor.MiddleRight;
        GUI.Label(new Rect(Screen.width - 160, 10, 150, 30), remainText, labelStyle);
        labelStyle.alignment = TextAnchor.MiddleLeft;
    }

    void DrawSummaryScreen()
    {
        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;
        float boxW = 500;
        float boxH = 380;

        GUI.DrawTexture(new Rect(cx - boxW / 2, cy - boxH / 2, boxW, boxH), bgTexture);

        float y = cy - boxH / 2 + 15;

        GUI.Label(new Rect(cx - boxW / 2, y, boxW, 45), "ROUND COMPLETE", titleStyle);
        y += 50;

        // Grade
        string grade = targetManager.GetGrade();
        Color gradeColor = grade == "S" ? Color.yellow :
                           grade == "A" ? Color.green :
                           grade == "B" ? new Color(0.5f, 1f, 0.5f) :
                           grade == "C" ? Color.white :
                           Color.red;
        gradeStyle.normal.textColor = gradeColor;
        GUI.Label(new Rect(cx + 100, y, 120, 90), grade, gradeStyle);

        // Stats
        float sx = cx - boxW / 2 + 30;

        GUI.Label(new Rect(sx, y, 300, 30), $"Score:  {targetManager.TotalScore} / {targetManager.targetsPerRound * 10}", summaryStyle);
        y += 30;

        float hitRate = targetManager.TargetsSpawned > 0
            ? (float)targetManager.TargetsHit / targetManager.TargetsSpawned * 100f : 0;
        GUI.Label(new Rect(sx, y, 300, 30), $"Hits:  {targetManager.TargetsHit} / {targetManager.TargetsSpawned}  ({hitRate:F0}%)", summaryStyle);
        y += 30;

        GUI.Label(new Rect(sx, y, 300, 30), $"Missed:  {targetManager.TargetsMissed} (timed out)", summaryStyle);
        y += 30;

        GUI.Label(new Rect(sx, y, 300, 30), $"Avg Score:  {targetManager.GetAvgScore():F1} / 10", summaryStyle);
        y += 30;

        GUI.Label(new Rect(sx, y, 300, 30), $"Best Shot:  {targetManager.GetBestScore()} / 10", summaryStyle);
        y += 30;

        float duration = targetManager.RoundEndTime - targetManager.RoundStartTime;
        int dMin = (int)(duration / 60f);
        int dSec = (int)(duration % 60f);
        GUI.Label(new Rect(sx, y, 300, 30), $"Round Time:  {dMin:D2}:{dSec:D2}", summaryStyle);
        y += 45;

        GUI.Label(new Rect(cx - boxW / 2, y, boxW, 35), "Press R to play again", instructionStyle);
    }

    void DrawCrosshair()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;
        int gap = 6;

        GUI.DrawTexture(new Rect(
            cx - crosshairThickness / 2f, cy - crosshairSize - gap,
            crosshairThickness, crosshairSize), crosshairTexture);
        GUI.DrawTexture(new Rect(
            cx - crosshairThickness / 2f, cy + gap,
            crosshairThickness, crosshairSize), crosshairTexture);
        GUI.DrawTexture(new Rect(
            cx - crosshairSize - gap, cy - crosshairThickness / 2f,
            crosshairSize, crosshairThickness), crosshairTexture);
        GUI.DrawTexture(new Rect(
            cx + gap, cy - crosshairThickness / 2f,
            crosshairSize, crosshairThickness), crosshairTexture);
        GUI.DrawTexture(new Rect(
            cx - 1f, cy - 1f, 3f, 3f), crosshairTexture);
    }

    void DrawFloatingScore()
    {
        if (floatingTimer <= 0f) return;

        floatingTimer -= Time.deltaTime;
        float alpha = Mathf.Clamp01(floatingTimer / 0.5f);

        Camera cam = Camera.main;
        if (cam == null) cam = FindFirstObjectByType<Camera>();
        if (cam == null) return;

        Vector3 screenPos = cam.WorldToScreenPoint(floatingWorldPos + Vector3.up * (1.2f - floatingTimer) * 0.5f);
        if (screenPos.z < 0) return;

        float guiY = Screen.height - screenPos.y;
        hitScoreStyle.normal.textColor = new Color(floatingColor.r, floatingColor.g, floatingColor.b, alpha);
        GUI.Label(new Rect(screenPos.x - 80, guiY - 20, 160, 40), floatingText, hitScoreStyle);
    }

    // Keep for compatibility but no longer used directly
    public void AddScore(int points) { }
    public void RecordShot(bool hit, int points) { }
}
