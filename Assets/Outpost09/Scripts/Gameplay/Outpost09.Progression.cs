using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Owner: Gameplay team. Keep shared entry points stable for Combat and Presentation.
public partial class Outpost09
{
    [Header("Game progression")]
    [SerializeField, Min(0f)] float interWaveDelay = 5f;
    [SerializeField] int[] stageClearRewards = { 300, 500, 800 };
    Coroutine waveRoutine;
    readonly HashSet<GameObject> transientObjects = new HashSet<GameObject>();
    const int WavesPerStage = 3;
    const int MaxGate = 500;
    const int MaxHealth = 100;
    int StageCount => counts.GetLength(0) / WavesPerStage;

    enum SupplyItem { MachineGun, LaserRifle, Grenade, GateRepair, PlayerHeal }

    void SetGameMode(string next)
    {
        mode = next;
        bool playing = mode == "playing";
        // One clock for wave waits, reloads, grenade physics and projectile lifetimes.
        Time.timeScale = playing ? 1f : 0f;
        Cursor.lockState = playing ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !playing;
    }

    void PauseCombat() { if (Playing) SetGameMode("pause"); }

    void ResumeCombat()
    {
        if (mode == "menu" || mode == "pause" || mode == "shop") SetGameMode("playing");
    }

    void OpenShop(bool betweenWaves)
    {
        if (!Playing) return;
        shopBetweenWaves = betweenWaves;
        SetGameMode("shop");
        ClearTransientObjects();
    }

    void StartStage()
    {
        // Repeated button clicks cannot launch a second spawn loop.
        if (waveRoutine != null || (mode != "menu" && mode != "shop")) return;
        if (stage < 0 || stage >= StageCount) return;
        wave = 0;
        shopBetweenWaves = false;
        ResumeCombat();
        waveRoutine = StartCoroutine(Waves());
    }

    void ContinueFromShop()
    {
        if (mode != "shop") return;
        if (shopBetweenWaves)
        {
            shopBetweenWaves = false;
            ResumeCombat();
        }
        else if (stage + 1 < StageCount)
        {
            stage++;
            StartStage();
        }
    }

    IEnumerator Waves()
    {
        // Keep the displayed wave at 1..3, including the stage-clear shop.
        for (int index = 0; index < WavesPerStage; index++)
        {
            if (index > 0)
            {
                while (mode == "shop" || mode == "pause") yield return null;
                if (!Playing) yield break;
                yield return new WaitForSeconds(interWaveDelay);
            }
            wave = index;
            int row = stage * WavesPerStage + wave;
            for (int kind = 0; kind < counts.GetLength(1); kind++)
            {
                for (int n = 0; n < counts[row, kind]; n++)
                {
                    while (mode == "pause") yield return null;
                    if (!Playing) yield break;
                    Spawn(kind, n);
                    yield return new WaitForSeconds(stage == 0 ? 2f : stage == 1 ? 1.5f : 1f);
                }
            }
            while (mode == "playing" || mode == "pause")
            {
                enemies.RemoveAll(enemy => enemy == null);
                if (Playing && enemies.Count == 0) break;
                yield return null;
            }
            if (!Playing) yield break;
            if (wave < WavesPerStage - 1) OpenShop(true);
        }

        waveRoutine = null;
        if (!Playing) yield break;
        if (stage < stageClearRewards.Length) credits += Mathf.Max(0, stageClearRewards[stage]);
        if (stage == StageCount - 1) EndRun("win");
        else OpenShop(false);
    }

    public void HitPlayer(int value)
    {
        if (!Playing || value <= 0) return;
        health = Mathf.Max(0, health - value);
        if (health == 0) Lose();
    }

    public void HitGate(int value)
    {
        if (!Playing || value <= 0) return;
        gate = Mathf.Max(0, gate - value);
        if (gate == 0) Lose();
    }

    void Lose() { if (Playing) EndRun("lose"); }

    void EndRun(string result)
    {
        SetGameMode(result);
        ClearTransientObjects();
        StopAllCoroutines();
        waveRoutine = null;
    }

    public void Dead(Alien enemy, int reward)
    {
        if (Playing && enemy != null && enemies.Remove(enemy))
        {
            credits += Mathf.Max(0, reward) * 2;
            kills++;
        }
    }

    int SupplyPrice(SupplyItem item)
    {
        switch (item)
        {
            case SupplyItem.MachineGun: return 300;
            case SupplyItem.LaserRifle: return 600;
            case SupplyItem.Grenade: return 100;
            case SupplyItem.GateRepair: return 200;
            case SupplyItem.PlayerHeal: return 100;
            default: return int.MaxValue;
        }
    }

    bool SupplyFull(SupplyItem item)
    {
        switch (item)
        {
            case SupplyItem.MachineGun: return owned[1];
            case SupplyItem.LaserRifle: return owned[2];
            case SupplyItem.GateRepair: return gate >= MaxGate;
            case SupplyItem.PlayerHeal: return health >= MaxHealth;
            default: return false;
        }
    }

    bool CanBuySupply(SupplyItem item) => mode == "shop" && !SupplyFull(item) && credits >= SupplyPrice(item);

    bool BuySupply(SupplyItem item)
    {
        if (!CanBuySupply(item)) return false;
        credits -= SupplyPrice(item);
        switch (item)
        {
            case SupplyItem.MachineGun: owned[1] = true; weapon = 1; break;
            case SupplyItem.LaserRifle: owned[2] = true; weapon = 2; break;
            case SupplyItem.Grenade: grenades++; break;
            case SupplyItem.GateRepair: gate = Mathf.Min(MaxGate, gate + 50); break;
            case SupplyItem.PlayerHeal: health = Mathf.Min(MaxHealth, health + 50); break;
        }
        return true;
    }

    internal GameObject TrackTransient(GameObject value)
    {
        transientObjects.RemoveWhere(item => item == null);
        transientObjects.Add(value);
        return value;
    }

    void ClearTransientObjects()
    {
        foreach (var item in transientObjects) if (item != null) { item.SetActive(false); Destroy(item); }
        transientObjects.Clear();
    }

    void ResetGame()
    {
        SetGameMode("menu");
        StopAllCoroutines();
        waveRoutine = null;
        // Deactivate immediately: delayed Destroy must not leave a same-frame collider behind.
        foreach (var enemy in enemies) if (enemy != null) { enemy.gameObject.SetActive(false); Destroy(enemy.gameObject); }
        enemies.Clear();
        ClearTransientObjects();
        foreach (var shot in FindObjectsByType<AcidShot>()) { shot.gameObject.SetActive(false); Destroy(shot.gameObject); }
        stage = wave = credits = kills = grenades = weapon = 0;
        gate = MaxGate; health = MaxHealth;
        owned[0] = true; owned[1] = owned[2] = false;
        ammo[0] = 12; ammo[1] = 60; ammo[2] = 30;
        elapsed = reloadAt = fireAt = cooldown = flash = 0;
        shopBetweenWaves = false;
        MovePost(1, true);
        flash = 0;
        cam.fieldOfView = 70;
        if (weaponModel != null) RebuildWeapon();
    }

    void RestartGame() { ResetGame(); StartStage(); }

    void OnEnable() { if (game == this) SetGameMode(mode); }
    void OnDisable()
    {
        if (game != this) return;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    void OnDestroy() { if (game == this) game = null; }
    void OnApplicationFocus(bool focused) { if (!focused) PauseCombat(); }
}
