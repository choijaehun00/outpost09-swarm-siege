using UnityEngine;

internal static class MainMenuUI
{
    internal static void Draw(Outpost09 game, GUIStyle label, GUIStyle title, GUIStyle button)
    {
        float x = Screen.width / 2f - 190f;
        float y = Screen.height / 2f - 150f;
        GUI.Box(new Rect(x - 20f, y - 50f, 420f, 410f), "");
        GUI.Label(new Rect(x, y - 35f, 400f, 55f), "OUTPOST 09", title);
        GUI.Label(new Rect(x, y + 35f, 400f, 30f), "SWARM SIEGE", label);
        if (GUI.Button(new Rect(x, y + 90f, 380f, 45f), "START GAME", button)) game.StartStage();
        GUI.Label(new Rect(x, y + 155f, 390f, 90f), OutpostCombat.ControlsHelp, label);
        if (GUI.Button(new Rect(x, y + 275f, 380f, 40f), "EXIT", button)) Application.Quit();
    }
}
