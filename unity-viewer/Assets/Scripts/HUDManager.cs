using UnityEngine;

/// <summary>
/// On-screen HUD: crosshair, score, precision stats, floating hit scores.
/// </summary>
public class HUDManager : MonoBehaviour
{
    [Header("References")]
    public TargetManager targetManager;

    [Header("Crosshair")]
    public int crosshairSize = 10;
    public int crosshairThickness = 2;
    public Color crosshairColor = Color.white;

    [Header("HUD")]
    public bool showTimer = true;

    private int score;
    private float startTime;
    private GUIStyle labelStyle;
    private GUIStyle scoreLabelStyle;
    private GUIStyle instructionStyle;
    private GUIStyle hitScoreStyle;
    private Texture2D crosshairTexture;

    // Precision tracking
    private int totalShots;
    private int totalHits;
    private int totalPointsScored;
    private int totalPointsPossible;

    // Floating hit score
    private string floatingText;
    private float floatingTimer;
    private Vector3 floatingWorldPos;
    private Color floatingColor;

    void Start()
    {
        startTime = Time.time;
        score = 0;

        crosshairTexture = new Texture2D(1, 1);
        crosshairTexture.SetPixel(0, 0, crosshairColor);
        crosshairTexture.Apply();
    }

    public void AddScore(int points)
    {
        score += points;
    }

    public void RecordShot(bool hit, int points)
    {
        totalShots++;
        if (hit)
        {
            totalHits++;
            totalPointsScored += points;
            totalPointsPossible += 10;
        }
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
        DrawCrosshair();
        DrawScorePanel();
        DrawFloatingScore();

        if (Cursor.lockState != CursorLockMode.Locked)
            DrawInstructions();
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

        scoreLabelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
        };
        scoreLabelStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);

        instructionStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };
        instructionStyle.normal.textColor = Color.white;

        hitScoreStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };
    }

    void DrawCrosshair()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;
        int gap = 6;

        // Top
        GUI.DrawTexture(new Rect(
            cx - crosshairThickness / 2f, cy - crosshairSize - gap,
            crosshairThickness, crosshairSize), crosshairTexture);
        // Bottom
        GUI.DrawTexture(new Rect(
            cx - crosshairThickness / 2f, cy + gap,
            crosshairThickness, crosshairSize), crosshairTexture);
        // Left
        GUI.DrawTexture(new Rect(
            cx - crosshairSize - gap, cy - crosshairThickness / 2f,
            crosshairSize, crosshairThickness), crosshairTexture);
        // Right
        GUI.DrawTexture(new Rect(
            cx + gap, cy - crosshairThickness / 2f,
            crosshairSize, crosshairThickness), crosshairTexture);

        // Center dot
        GUI.DrawTexture(new Rect(
            cx - 1f, cy - 1f, 3f, 3f), crosshairTexture);
    }

    void DrawScorePanel()
    {
        float y = 10f;
        float x = 10f;

        GUI.Label(new Rect(x, y, 300, 30), $"Score: {score}", labelStyle);
        y += 30f;

        if (showTimer)
        {
            float elapsed = Time.time - startTime;
            int minutes = (int)(elapsed / 60f);
            int seconds = (int)(elapsed % 60f);
            GUI.Label(new Rect(x, y, 300, 30), $"Time: {minutes:D2}:{seconds:D2}", labelStyle);
            y += 30f;
        }

        // Precision stats
        if (totalShots > 0)
        {
            float hitRate = (float)totalHits / totalShots * 100f;
            GUI.Label(new Rect(x, y, 300, 25), $"Accuracy: {hitRate:F0}% ({totalHits}/{totalShots})", scoreLabelStyle);
            y += 22f;

            if (totalPointsPossible > 0)
            {
                float precision = (float)totalPointsScored / totalPointsPossible * 100f;
                float avgPoints = (float)totalPointsScored / totalHits;
                GUI.Label(new Rect(x, y, 300, 25), $"Precision: {precision:F0}% (avg {avgPoints:F1}/10)", scoreLabelStyle);
            }
        }
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

        // Unity GUI has Y inverted from screen coords
        float guiY = Screen.height - screenPos.y;

        hitScoreStyle.normal.textColor = new Color(floatingColor.r, floatingColor.g, floatingColor.b, alpha);
        GUI.Label(new Rect(screenPos.x - 80, guiY - 20, 160, 40), floatingText, hitScoreStyle);
    }

    void DrawInstructions()
    {
        string text = "Click to start\nWASD to move | Mouse to look\nShift to sprint | Click to shoot\nQ/E up/down | Esc to release cursor";
        GUI.Label(new Rect(0, Screen.height / 2f - 60f, Screen.width, 120), text, instructionStyle);
    }
}
