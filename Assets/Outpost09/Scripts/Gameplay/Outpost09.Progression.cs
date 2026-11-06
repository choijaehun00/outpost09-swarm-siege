using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Owner: Gameplay team. Shared state is declared in Assets/Outpost09.cs.
public partial class Outpost09
{
    void ResumeCombat() { mode = "playing"; Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }

    void OpenShop(bool betweenWaves) { shopBetweenWaves = betweenWaves; mode = "shop"; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }

    void StartStage() { wave = 0; ResumeCombat(); StartCoroutine(Waves()); }

    IEnumerator Waves()
    {
        for (wave = 0; wave < 3; wave++)
        {
            if (wave > 0) { while (mode == "shop" || mode == "pause") yield return null; yield return new WaitForSeconds(5); }
            int row = stage * 3 + wave;
            for (int kind = 0; kind < 4; kind++) for (int n = 0; n < counts[row, kind]; n++)
            {
                while (mode == "pause") yield return null;
                if (mode != "playing") yield break;
                Spawn(kind, n); yield return new WaitForSeconds(stage == 0 ? 2 : stage == 1 ? 1.5f : 1);
            }
            while ((enemies.Count > 0 || mode == "pause") && mode != "lose" && mode != "menu") yield return null;
            if (mode != "playing") yield break;
            if (wave < 2) OpenShop(true);
        }
        credits += stage == 0 ? 300 : stage == 1 ? 500 : 800;
        if (stage == 2) { mode = "win"; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        else OpenShop(false);
    }

    public void HitPlayer(int value) { health = Mathf.Max(0, health - value); if (health == 0) Lose(); }

    public void HitGate(int value) { gate = Mathf.Max(0, gate - value); if (gate == 0) Lose(); }

    void Lose() { mode = "lose"; StopAllCoroutines(); Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }

    public void Dead(Alien e, int reward) { if (enemies.Remove(e)) { credits += reward * 2; kills++; } }

    void ResetGame()
    {
        StopAllCoroutines(); foreach (var e in enemies.ToArray()) if (e != null) Destroy(e.gameObject); enemies.Clear();
        stage = 0; wave = 0; credits = 0; kills = 0; grenades = 0; gate = 500; health = 100; weapon = 0; owned[1] = owned[2] = false; shopBetweenWaves = false;
        ammo[0] = 12; ammo[1] = 60; ammo[2] = 30; elapsed = 0; reloadAt = fireAt = 0; MovePost(1, true); mode = "menu"; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
    }
}
