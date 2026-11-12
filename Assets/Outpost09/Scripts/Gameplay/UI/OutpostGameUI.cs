using UnityEngine;
using SupplyItem = Outpost09.SupplyItem;

public sealed class OutpostGameUI : MonoBehaviour
{
    [SerializeField, HideInInspector] Outpost09 game;
    GUIStyle label, title, button;

    internal void Initialize(Outpost09 owner) => game = owner;

    void OnGUI()
    {
        if (game == null || !game.isActiveAndEnabled) return;
        if (label == null) { label = new GUIStyle(GUI.skin.label) { fontSize = 18, normal = { textColor = Color.white } }; title = new GUIStyle(label) { fontSize = 36, fontStyle = FontStyle.Bold }; button = new GUIStyle(GUI.skin.button) { fontSize = 19 }; }
        if (game.Mode == "menu")
        {
            MainMenuUI.Draw(game, label, title, button);
            return;
        }

        var combat = game.Combat;
        GUI.Box(new Rect(10, 10, 570, 75), "");
        GUI.Label(new Rect(20, 18, 560, 30), $"STAGE {game.Stage + 1}  WAVE {game.Wave + 1}/{Outpost09.WavesPerStage}  ENEMIES {combat.EnemyCount}  CREDITS {game.Credits}", label);
        GUI.Label(new Rect(20, 48, 560, 30), $"GATE {game.GateHealth}/{Outpost09.MaxGate}    HP {game.PlayerHealth}/{Outpost09.MaxHealth}    KILLS {game.Kills}", label);
        if (game.Playing)
        {
            GUI.Label(new Rect(20, Screen.height - 85, 700, 30), $"{OutpostCombat.PostShortcuts}    CURRENT: {combat.CurrentPostName}   COOLDOWN: {combat.PostCooldownRemaining:0.0}", label);
            GUI.Label(new Rect(20, Screen.height - 48, 600, 30), $"{combat.CurrentWeaponName}  {combat.CurrentAmmo}     GRENADES {combat.GrenadeCount}", label);
            GUI.Label(new Rect(Screen.width / 2 - 9, Screen.height / 2 - 12, 30, 30), "+", title);
            if (combat.PostFlashActive) GUI.Box(new Rect(0, 0, Screen.width, Screen.height), "");
            if (game.GateHealth <= Outpost09.MaxGate / 4) GUI.Label(new Rect(Screen.width / 2 - 95, 20, 200, 30), "GATE CRITICAL", label);
            return;
        }
        float x = Screen.width / 2 - 190, y = Screen.height / 2 - 150;
        GUI.Box(new Rect(x - 20, y - 50, 420, 410), "");
        GUI.Label(new Rect(x, y - 35, 400, 55), "OUTPOST 09", title);
        string mode = game.Mode;
        if (mode == "shop")
        {
            GUI.Label(new Rect(x, y + 30, 380, 28), "SUPPLY DEPOT  |  CREDITS " + game.Credits, label);
            ShopButton(x, y + 70, OutpostCombat.GetWeaponName(OutpostCombat.MachineGun), SupplyItem.MachineGun);
            ShopButton(x, y + 115, OutpostCombat.GetWeaponName(OutpostCombat.LaserRifle), SupplyItem.LaserRifle);
            ShopButton(x, y + 160, "GRENADE", SupplyItem.Grenade);
            ShopButton(x, y + 205, "GATE +50", SupplyItem.GateRepair);
            ShopButton(x, y + 250, "PLAYER +50", SupplyItem.PlayerHeal);
            if (GUI.Button(new Rect(x, y + 300, 380, 42), game.ShopBetweenWaves ? "NEXT WAVE" : "NEXT STAGE", button))
            {
                game.ContinueFromShop();
            }
        }
        else
        {
            GUI.Label(new Rect(x, y + 50, 380, 40), mode == "win" ? "MISSION COMPLETE" : mode == "lose" ? "GAME OVER" : "PAUSED", title);
            GUI.Label(new Rect(x, y + 110, 380, 80), $"STAGE {game.Stage + 1}   KILLS {game.Kills}\nCREDITS {game.Credits}   TIME {game.Elapsed:0}s", label);
            if (GUI.Button(new Rect(x, y + 205, 380, 42), mode == "pause" ? "RESUME" : "RESTART", button)) { if (mode == "pause") game.ResumeCombat(); else game.RestartGame(); }
            if (GUI.Button(new Rect(x, y + 260, 380, 42), "MAIN MENU", button)) game.ResetGame();
        }
    }

    void ShopButton(float x, float y, string text, SupplyItem item)
    {
        string caption = $"{text}  {game.SupplyPrice(item)}";
        if (game.SupplyFull(item)) caption += "  OWNED/FULL";
        GUI.enabled = game.CanBuySupply(item);
        if (GUI.Button(new Rect(x, y, 380, 38), caption, button)) game.BuySupply(item);
        GUI.enabled = true;
    }
}
