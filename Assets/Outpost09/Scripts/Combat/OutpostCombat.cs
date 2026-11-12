using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed partial class OutpostCombat : MonoBehaviour
{
    public enum EnemyKind { Runner, Sprinter, Spitter, Bruiser }

    const int Pistol = 0;
    public const int MachineGun = 1;
    public const int LaserRifle = 2;
    const float NormalFov = 70f;
    public const string ControlsHelp = "Mouse: aim / fire   RMB: zoom\nF1-F3: teleport   1-3: weapons\nR: reload   G: grenade   Esc: pause";
    public const string PostShortcuts = "F1 LEFT   F2 CENTER   F3 RIGHT";
    const float ShotRange = 120f;
    static readonly int[] MagazineSizes = { 12, 60, 30 };
    static readonly string[] PostNames = { "LEFT", "CENTER", "RIGHT" };
    static readonly string[] WeaponNames = { "PISTOL", "MACHINE GUN", "LASER RIFLE" };

    readonly List<Alien> enemies = new List<Alien>();
    readonly HashSet<GameObject> transientObjects = new HashSet<GameObject>();
    readonly int[] ammo = (int[])MagazineSizes.Clone();
    readonly bool[] owned = { true, false, false };
    [SerializeField, HideInInspector] OutpostBattlefield battlefield;
    [SerializeField, HideInInspector] Camera cam;
    int post = 1, weapon, grenades;
    float cooldown, fireAt, reloadAt, yaw, pitch, flash;
    float recoilPitch, machineGunSpread;

    public int EnemyCount => enemies.Count;
    public string CurrentPostName => PostNames[post];
    public string CurrentWeaponName => GetWeaponName(weapon);
    public int CurrentAmmo => ammo[weapon];
    public int GrenadeCount => grenades;
    public float PostCooldownRemaining => Mathf.Max(0f, cooldown - Time.time);
    public bool PostFlashActive => Time.time < flash;
    internal OutpostBattlefield Battlefield => battlefield;
    internal Vector3 Target { get; private set; }
    internal bool Playing { get; private set; }

    public event System.Action PauseRequested;
    public event System.Action<EnemyKind> EnemyDefeated;
    public event System.Action<int> PlayerHit;
    public event System.Action<int> GateHit;

    public void Initialize(Camera viewCamera, OutpostBattlefield map)
    {
        battlefield = map;
        cam = viewCamera;
        cam.nearClipPlane = .04f;
        cam.fieldOfView = NormalFov;
        MovePost(1, true);
        BuildWeaponModel();
    }

    public void SetRunActive(bool active) => Playing = active;

    void MovePost(int index, bool force = false)
    {
        if (!force && (index == post || Time.time < cooldown)) return;
        if (!battlefield.TryGetStation(index, out var position, out var aimPoint)) return;
        post = index; cooldown = Time.time + (force ? 0 : 1); flash = Time.time + .18f;
        Target = cam.transform.position = position;
        cam.transform.LookAt(aimPoint);
        yaw = cam.transform.eulerAngles.y; pitch = Mathf.DeltaAngle(0f, cam.transform.eulerAngles.x);
        recoilPitch = 0f;
        machineGunSpread = 0f;
    }

    public void Spawn(EnemyKind kind, int ordinal, int stage)
    {
        int routeCount = battlefield.RouteCount;
        if (routeCount == 0)
        {
            Debug.LogError("OUTPOST 09: No enemy spawn routes are configured.", this);
            return;
        }
        int routeIndex = (ordinal + (int)kind + stage) % routeCount;
        if (!battlefield.TryGetRoute(routeIndex, out var route))
        {
            Debug.LogError($"OUTPOST 09: Enemy spawn route {routeIndex} is invalid.", this);
            return;
        }

        var o = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        o.name = kind.ToString();
        float bodyClearance = Alien.BodyClearance(kind);
        o.transform.position = route[0] + Vector3.up * bodyClearance;
        o.transform.localScale = Vector3.one * bodyClearance;
        var enemy = o.AddComponent<Alien>();
        enemy.Setup(this, kind, routeIndex);
        enemies.Add(enemy);
        BuildAlienVisual(o, kind);
    }

    void Update()
    {
        if (!Playing) return;
        if (displayedWeapon != weapon) RebuildWeapon();
        var k = Keyboard.current; var m = Mouse.current;
        if (k == null || m == null) return;
        if (k.escapeKey.wasPressedThisFrame) { PauseRequested?.Invoke(); return; }
        if (k.f1Key.wasPressedThisFrame) MovePost(0);
        if (k.f2Key.wasPressedThisFrame) MovePost(1);
        if (k.f3Key.wasPressedThisFrame) MovePost(2);
        yaw += m.delta.ReadValue().x * .1f; pitch = Mathf.Clamp(pitch - m.delta.ReadValue().y * .1f, -70, 70);
        recoilPitch = Mathf.MoveTowards(recoilPitch, 0f, 10f * Time.deltaTime);
        machineGunSpread = Mathf.MoveTowards(machineGunSpread, 0f, 3f * Time.deltaTime);
        cam.transform.rotation = Quaternion.Euler(pitch - recoilPitch, yaw, 0);
        bool aiming = m.rightButton.isPressed;
        cam.fieldOfView = aiming ? 45 : NormalFov;
        if (k.digit1Key.wasPressedThisFrame) weapon = Pistol;
        if (k.digit2Key.wasPressedThisFrame && owned[MachineGun]) weapon = MachineGun;
        if (k.digit3Key.wasPressedThisFrame && owned[LaserRifle]) weapon = LaserRifle;
        if (k.rKey.wasPressedThisFrame) Reload();
        if (k.gKey.wasPressedThisFrame) ThrowGrenade();
        bool pullingTrigger = weapon == MachineGun ? m.leftButton.isPressed : m.leftButton.wasPressedThisFrame;
        if (pullingTrigger && Time.time >= fireAt && Time.time >= reloadAt) Shoot(aiming);
    }

    public static string GetWeaponName(int index) => WeaponNames[index];
    public bool OwnsWeapon(int index) => owned[index];

    public void UnlockWeapon(int index)
    {
        owned[index] = true;
        weapon = index;
    }

    public void AddGrenade() => grenades++;

    public bool HasLivingEnemies()
    {
        enemies.RemoveAll(enemy => enemy == null);
        return enemies.Count > 0;
    }

    internal void ReportEnemyDefeated(Alien enemy, EnemyKind kind)
    {
        if (Playing && enemies.Remove(enemy)) EnemyDefeated?.Invoke(kind);
    }

    internal void ReportPlayerHit(int damage) => PlayerHit?.Invoke(damage);
    internal void ReportGateHit(int damage) => GateHit?.Invoke(damage);

    internal GameObject TrackTransient(GameObject item)
    {
        transientObjects.RemoveWhere(value => value == null);
        transientObjects.Add(item);
        return item;
    }

    public void ClearTransientObjects()
    {
        foreach (var item in transientObjects)
            if (item != null) DeactivateAndDestroy(item);
        transientObjects.Clear();
    }

    public void EndRun()
    {
        ClearTransientObjects();
        StopAllCoroutines();
    }

    public void ResetRun()
    {
        EndRun();
        foreach (var enemy in enemies)
            if (enemy != null) DeactivateAndDestroy(enemy.gameObject);
        enemies.Clear();
        foreach (var shot in FindObjectsByType<AcidShot>())
            DeactivateAndDestroy(shot.gameObject);

        grenades = 0;
        weapon = Pistol;
        owned[Pistol] = true;
        owned[MachineGun] = owned[LaserRifle] = false;
        MagazineSizes.CopyTo(ammo, 0);
        reloadAt = fireAt = 0f;
        MovePost(1, true);
        flash = 0f;
        cam.fieldOfView = NormalFov;
        if (displayedWeapon != weapon) RebuildWeapon();
    }

    static void DeactivateAndDestroy(GameObject item)
    {
        // Destroy is deferred, so disable the collider and scripts immediately.
        item.SetActive(false);
        Destroy(item);
    }

    void Shoot(bool aiming)
    {
        if (ammo[weapon] <= 0) { Reload(); return; }
        ammo[weapon]--;

        Vector3 origin = cam.transform.position;
        Vector3 direction = cam.transform.forward;
        switch (weapon)
        {
            case Pistol:
                fireAt = Time.time + .3f;
                recoilPitch += .45f;
                HitFirstEnemy(origin, direction, 25);
                break;
            case MachineGun:
                fireAt = Time.time + .09f;
                recoilPitch += aiming ? .45f : .8f;
                float spread = (.35f + machineGunSpread) * (aiming ? .55f : 1f);
                Vector2 offset = Random.insideUnitCircle * spread;
                direction = cam.transform.rotation * (Quaternion.Euler(offset.y, offset.x, 0) * Vector3.forward);
                machineGunSpread = Mathf.Min(2.5f, machineGunSpread + .55f);
                HitFirstEnemy(origin, direction, 12);
                break;
            case LaserRifle:
                fireAt = Time.time + .65f;
                recoilPitch += 1.4f;
                FireLaser(origin, direction);
                break;
        }
        cam.transform.rotation = Quaternion.Euler(pitch - recoilPitch, yaw, 0);
    }

    void HitFirstEnemy(Vector3 origin, Vector3 direction, int damage)
    {
        if (Physics.Raycast(origin, direction, out var hit, ShotRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            hit.collider.GetComponentInParent<Alien>()?.Damage(damage);
    }

    void FireLaser(Vector3 origin, Vector3 direction)
    {
        var hits = Physics.RaycastAll(origin, direction, ShotRange, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (first, second) => first.distance.CompareTo(second.distance));

        var struck = new HashSet<Alien>();
        int damage = 65;
        foreach (var hit in hits)
        {
            var enemy = hit.collider.GetComponentInParent<Alien>();
            if (enemy == null) break;
            if (!struck.Add(enemy)) continue;

            enemy.Damage(damage);
            if (struck.Count == 3) break;
            damage -= 20;
        }
    }

    void Reload()
    {
        if (Time.time < reloadAt || ammo[weapon] == MagazineSizes[weapon]) return;

        float duration = weapon == Pistol ? 1.15f : weapon == MachineGun ? 2.5f : 2.2f;
        reloadAt = Time.time + duration;
        StartCoroutine(FinishReload(weapon, duration));
    }

    IEnumerator FinishReload(int reloadingWeapon, float duration)
    {
        yield return new WaitForSeconds(duration);
        ammo[reloadingWeapon] = MagazineSizes[reloadingWeapon];
    }

    void ThrowGrenade()
    {
        if (grenades <= 0) return;

        grenades--;
        var grenade = TrackTransient(GameObject.CreatePrimitive(PrimitiveType.Sphere));
        grenade.name = "Grenade";
        grenade.transform.position = cam.transform.position + cam.transform.forward;
        grenade.transform.localScale = Vector3.one * .35f;
        grenade.GetComponent<Renderer>().material.color = Color.gray;
        grenade.AddComponent<Grenade>().Launch(this, cam.transform.forward * 16 + Vector3.up * 5);
    }

    internal void ExplodeGrenade(Vector3 position, float radius)
    {
        foreach (var enemy in enemies.ToArray())
        {
            if (enemy == null) continue;

            float distance = Vector3.Distance(position, enemy.transform.position);
            if (distance < radius) enemy.Damage(Mathf.RoundToInt(120 * (1 - distance / radius)));
        }

        var effect = TrackTransient(GameObject.CreatePrimitive(PrimitiveType.Sphere));
        effect.name = "Grenade blast";
        effect.transform.position = position;
        effect.transform.localScale = Vector3.one * (radius * 2);
        effect.GetComponent<Renderer>().material.color = new Color(1, .4f, .05f);
        var collider = effect.GetComponent<Collider>();
        collider.enabled = false;
        Destroy(effect, .18f);
    }
}
