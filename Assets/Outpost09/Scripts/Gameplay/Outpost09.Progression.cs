using System.Collections;
using UnityEngine;
using EnemyKind = OutpostCombat.EnemyKind;

public partial class Outpost09
{
    [Header("Game progression")]
    [SerializeField, Min(0f)] float interWaveDelay = 5f;
    [SerializeField] int[] stageClearRewards = { 300, 500, 800 };
    Coroutine waveRoutine;
    internal const int WavesPerStage = 3;
    internal const int MaxGate = 500;
    internal const int MaxHealth = 100;
    // Columns follow EnemyKind: Runner, Sprinter, Spitter, Bruiser.
    readonly int[,] counts = { { 5, 0, 0, 0 }, { 6, 2, 0, 0 }, { 7, 4, 0, 0 }, { 6, 4, 0, 0 }, { 7, 0, 3, 0 }, { 8, 5, 4, 0 }, { 8, 5, 3, 0 }, { 8, 6, 4, 2 }, { 10, 7, 5, 3 } };
    static readonly int[] KillRewards = { 20, 30, 40, 100 };
    int stage, wave, credits, kills, gate = MaxGate, health = MaxHealth;
    float elapsed;
    string mode = "menu";
    bool shopBetweenWaves;
    int StageCount => counts.GetLength(0) / WavesPerStage;

    internal int Stage => stage;
    internal int Wave => wave;
    internal int Credits => credits;
    internal int Kills => kills;
    internal int GateHealth => gate;
    internal int PlayerHealth => health;
    internal float Elapsed => elapsed;
    internal string Mode => mode;
    internal bool ShopBetweenWaves => shopBetweenWaves;

    internal enum SupplyItem { MachineGun, LaserRifle, Grenade, GateRepair, PlayerHeal }

    void Update()
    {
        if (Playing) elapsed += Time.deltaTime;
    }

    void SetGameMode(string next)
    {
        mode = next;
        bool playing = mode == "playing";
        Combat.SetRunActive(playing);
        // One clock for wave waits, reloads, grenade physics and projectile lifetimes.
        Time.timeScale = playing ? 1f : 0f;
        Cursor.lockState = playing ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !playing;
    }

    internal void PauseCombat() { if (Playing) SetGameMode("pause"); }

    internal void ResumeCombat()
    {
        if (mode == "menu" || mode == "pause" || mode == "shop") SetGameMode("playing");
    }

    void OpenShop(bool betweenWaves)
    {
        if (!Playing) return;
        shopBetweenWaves = betweenWaves;
        SetGameMode("shop");
        Combat.ClearTransientObjects();
    }

    internal void StartStage()
    {
        // Repeated button clicks cannot launch a second spawn loop.
        if (waveRoutine != null || (mode != "menu" && mode != "shop")) return;
        if (stage < 0 || stage >= StageCount) return;
        wave = 0;
        shopBetweenWaves = false;
        ResumeCombat();
        waveRoutine = StartCoroutine(Waves());
    }

    internal void ContinueFromShop()
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
                    Combat.Spawn((EnemyKind)kind, n, stage);
                    yield return new WaitForSeconds(stage == 0 ? 2f : stage == 1 ? 1.5f : 1f);
                }
            }
            while (mode == "playing" || mode == "pause")
            {
                bool hasEnemies = Combat.HasLivingEnemies();
                if (Playing && !hasEnemies) break;
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
        Combat.EndRun();
        StopAllCoroutines();
        waveRoutine = null;
    }

    void RewardDefeat(EnemyKind kind)
    {
        if (!Playing) return;
        credits += Mathf.Max(0, KillRewards[(int)kind]) * 2;
        kills++;
    }

    internal int SupplyPrice(SupplyItem item)
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

    internal bool SupplyFull(SupplyItem item)
    {
        switch (item)
        {
            case SupplyItem.MachineGun: return Combat.OwnsWeapon(OutpostCombat.MachineGun);
            case SupplyItem.LaserRifle: return Combat.OwnsWeapon(OutpostCombat.LaserRifle);
            case SupplyItem.GateRepair: return gate >= MaxGate;
            case SupplyItem.PlayerHeal: return health >= MaxHealth;
            default: return false;
        }
    }

    internal bool CanBuySupply(SupplyItem item) => mode == "shop" && !SupplyFull(item) && credits >= SupplyPrice(item);

    internal bool BuySupply(SupplyItem item)
    {
        if (!CanBuySupply(item)) return false;
        credits -= SupplyPrice(item);
        switch (item)
        {
            case SupplyItem.MachineGun: Combat.UnlockWeapon(OutpostCombat.MachineGun); break;
            case SupplyItem.LaserRifle: Combat.UnlockWeapon(OutpostCombat.LaserRifle); break;
            case SupplyItem.Grenade: Combat.AddGrenade(); break;
            case SupplyItem.GateRepair: gate = Mathf.Min(MaxGate, gate + 50); break;
            case SupplyItem.PlayerHeal: health = Mathf.Min(MaxHealth, health + 50); break;
        }
        return true;
    }

    internal void ResetGame()
    {
        SetGameMode("menu");
        StopAllCoroutines();
        waveRoutine = null;
        stage = wave = credits = kills = 0;
        gate = MaxGate; health = MaxHealth;
        elapsed = 0f;
        shopBetweenWaves = false;
        Combat.ResetRun();
    }

    internal void RestartGame() { ResetGame(); StartStage(); }

    void OnEnable()
    {
        if (game != this || Combat == null) return;
        Combat.PauseRequested += PauseCombat;
        Combat.PlayerHit += HitPlayer;
        Combat.GateHit += HitGate;
        Combat.EnemyDefeated += RewardDefeat;
        SetGameMode(mode);
    }

    void OnDisable()
    {
        if (game != this) return;
        if (Combat != null)
        {
            Combat.PauseRequested -= PauseCombat;
            Combat.PlayerHit -= HitPlayer;
            Combat.GateHit -= HitGate;
            Combat.EnemyDefeated -= RewardDefeat;
            Combat.SetRunActive(false);
        }
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    void OnDestroy() { if (game == this) game = null; }
    void OnApplicationFocus(bool focused) { if (!focused) PauseCombat(); }
}
