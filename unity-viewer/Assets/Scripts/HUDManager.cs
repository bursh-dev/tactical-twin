using UnityEngine;

/// <summary>
/// On-screen HUD: crosshair, score, targets remaining, timer.
/// Uses OnGUI for simplicity in the POC.
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
    private GUIStyle instructionStyle;
    private Texture2D crosshairTexture;

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

    void OnGUI()
    {
        InitStyles();
        DrawCrosshair();
        DrawScorePanel();

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

        instructionStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
        };
        instructionStyle.normal.textColor = Color.white;
    }

    void DrawCrosshair()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;

        // Horizontal line
        GUI.DrawTexture(new Rect(
            cx - crosshairSize, cy - crosshairThickness / 2f,
            crosshairSize * 2, crosshairThickness), crosshairTexture);

        // Vertical line
        GUI.DrawTexture(new Rect(
            cx - crosshairThickness / 2f, cy - crosshairSize,
            crosshairThickness, crosshairSize * 2), crosshairTexture);
    }

    void DrawScorePanel()
    {
        float y = 10f;
        float x = 10f;

        GUI.Label(new Rect(x, y, 300, 30), $"Score: {score}", labelStyle);
        y += 30f;

        if (targetManager != null)
        {
            int hit = targetManager.GetHitCount();
            int total = targetManager.GetTotalCount();
            GUI.Label(new Rect(x, y, 300, 30), $"Targets: {hit} / {total}", labelStyle);
            y += 30f;
        }

        if (showTimer)
        {
            float elapsed = Time.time - startTime;
            int minutes = (int)(elapsed / 60f);
            int seconds = (int)(elapsed % 60f);
            GUI.Label(new Rect(x, y, 300, 30), $"Time: {minutes:D2}:{seconds:D2}", labelStyle);
        }
    }

    void DrawInstructions()
    {
        string text = "Click to start\nWASD to move | Mouse to look\nShift to sprint | Click to shoot\nEsc to release cursor";
        GUI.Label(new Rect(0, Screen.height / 2f - 60f, Screen.width, 120), text, instructionStyle);
    }
}
