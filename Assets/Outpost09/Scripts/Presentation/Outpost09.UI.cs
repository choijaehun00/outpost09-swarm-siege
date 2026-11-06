using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Owner: Presentation team. Shared state is declared in Assets/Outpost09.cs.
public partial class Outpost09
{
    void OnGUI()
    {
        if (label == null) { label = new GUIStyle(GUI.skin.label) { fontSize = 18, normal = { textColor = Color.white } }; title = new GUIStyle(label) { fontSize = 36, fontStyle = FontStyle.Bold }; button = new GUIStyle(GUI.skin.button) { fontSize = 19 }; }
        GUI.Box(new Rect(10, 10, 570, 75), "");
        GUI.Label(new Rect(20, 18, 560, 30), $"STAGE {stage + 1}  WAVE {wave + 1}/3  ENEMIES {enemies.Count}  CREDITS {credits}", label);
        GUI.Label(new Rect(20, 48, 560, 30), $"GATE {gate}/500    HP {health}/100    KILLS {kills}", label);
        if (mode == "playing")
        {
            GUI.Label(new Rect(20, Screen.height - 85, 700, 30), $"F1 LEFT   F2 CENTER   F3 RIGHT    CURRENT: {names[post]}   COOLDOWN: {Mathf.Max(0, cooldown - Time.time):0.0}", label);
            GUI.Label(new Rect(20, Screen.height - 48, 600, 30), $"{new[] { "PISTOL", "MACHINE GUN", "LASER RIFLE" }[weapon]}  {ammo[weapon]}     GRENADES {grenades}", label);
            GUI.Label(new Rect(Screen.width / 2 - 9, Screen.height / 2 - 12, 30, 30), "+", title);
            if (Time.time < flash) GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");
            if (gate <= 125) GUI.Label(new Rect(Screen.width / 2 - 95, 20, 200, 30), "GATE CRITICAL", label);
            return;
        }
        float x = Screen.width / 2 - 190, y = Screen.height / 2 - 150;
        GUI.Box(new Rect(x - 20, y - 50, 420, 410), "");
        GUI.Label(new Rect(x, y - 35, 400, 55), "OUTPOST 09", title);
        if (mode == "menu")
        {
            GUI.Label(new Rect(x, y + 35, 400, 30), "SWARM SIEGE", label);
            if (GUI.Button(new Rect(x, y + 90, 380, 45), "START GAME", button)) StartStage();
            GUI.Label(new Rect(x, y + 155, 390, 90), "Mouse: aim / fire   RMB: zoom\nF1-F3: teleport   1-3: weapons\nR: reload   G: grenade   Esc: pause", label);
            if (GUI.Button(new Rect(x, y + 275, 380, 40), "EXIT", button)) Application.Quit();
        }
        else if (mode == "shop")
        {
            GUI.Label(new Rect(x, y + 30, 380, 28), "SUPPLY DEPOT  |  CREDITS " + credits, label);
            ShopButton(x, y + 70, "MACHINE GUN  300", 300, () => { owned[1] = true; weapon = 1; }, owned[1]);
            ShopButton(x, y + 115, "LASER RIFLE  600", 600, () => { owned[2] = true; weapon = 2; }, owned[2]);
            ShopButton(x, y + 160, "GRENADE  100", 100, () => grenades++, false);
            ShopButton(x, y + 205, "GATE +50  200", 200, () => gate = Mathf.Min(500, gate + 50), gate == 500);
            ShopButton(x, y + 250, "PLAYER +50  100", 100, () => health = Mathf.Min(100, health + 50), health == 100);
            if (GUI.Button(new Rect(x, y + 300, 380, 42), shopBetweenWaves ? "NEXT WAVE" : "NEXT STAGE", button))
            {
                if (shopBetweenWaves) ResumeCombat();
                else { stage++; StartStage(); }
            }
        }
        else
        {
            GUI.Label(new Rect(x, y + 50, 380, 40), mode == "win" ? "MISSION COMPLETE" : mode == "lose" ? "GAME OVER" : "PAUSED", title);
            GUI.Label(new Rect(x, y + 110, 380, 80), $"STAGE {stage + 1}   KILLS {kills}\nCREDITS {credits}   TIME {elapsed:0}s", label);
            if (GUI.Button(new Rect(x, y + 205, 380, 42), mode == "pause" ? "RESUME" : "RESTART", button)) { if (mode == "pause") { mode = "playing"; Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; } else { ResetGame(); StartStage(); } }
            if (GUI.Button(new Rect(x, y + 260, 380, 42), "MAIN MENU", button)) ResetGame();
        }
    }

    void ShopButton(float x, float y, string text, int cost, System.Action purchase, bool disabled)
    {
        GUI.enabled = credits >= cost && !disabled;
        if (GUI.Button(new Rect(x, y, 380, 38), disabled ? text + "  OWNED/FULL" : text, button)) { credits -= cost; purchase(); }
        GUI.enabled = true;
    }
}
