using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Owner: Combat team. Shared state is declared in Assets/Outpost09.cs.
public partial class Outpost09
{
    const int Pistol = 0;
    const int MachineGun = 1;
    const int LaserRifle = 2;
    const float ShotRange = 120f;
    static readonly int[] MagazineSizes = { 12, 60, 30 };

    float recoilPitch, machineGunSpread;

    void MovePost(int index, bool force = false)
    {
        if (!force && (index == post || Time.time < cooldown)) return;
        post = index; cooldown = Time.time + (force ? 0 : 1); flash = Time.time + .18f;
        cam.transform.position = posts[post];
        cam.transform.LookAt(OutpostMapLayout.AimPoints[index]);
        yaw = cam.transform.eulerAngles.y; pitch = Mathf.DeltaAngle(0f, cam.transform.eulerAngles.x);
        recoilPitch = 0f;
        machineGunSpread = 0f;
    }

    void Spawn(int kind, int n)
    {
        int routeIndex = (n + kind + stage) % OutpostMapLayout.Routes.Length;
        var o = GameObject.CreatePrimitive(PrimitiveType.Capsule); o.name = new[] { "Runner", "Sprinter", "Spitter", "Bruiser" }[kind];
        float bodyClearance = kind == 3 ? 2f : kind == 1 ? .7f : 1f;
        o.transform.position = OutpostMapLayout.Routes[routeIndex][0] + Vector3.up * bodyClearance;
        o.transform.localScale = kind == 3 ? new Vector3(2, 2, 2) : kind == 1 ? new Vector3(.7f, .7f, .7f) : Vector3.one;
        o.GetComponent<Renderer>().material = Mat(new[] { Color.green, Color.yellow, Color.magenta, new Color(.4f, .2f, .5f) }[kind]);
        var e = o.AddComponent<Alien>(); e.Setup(this, kind, routeIndex); enemies.Add(e);
        BuildAlienVisual(o, kind);
    }

    void Update()
    {
        if (mode != "playing") return;
        if (displayedWeapon != weapon) RebuildWeapon();
        elapsed += Time.deltaTime;
        var k = Keyboard.current; var m = Mouse.current;
        if (k == null || m == null) return;
        if (k.escapeKey.wasPressedThisFrame) { PauseCombat(); return; }
        if (k.f1Key.wasPressedThisFrame) MovePost(0);
        if (k.f2Key.wasPressedThisFrame) MovePost(1);
        if (k.f3Key.wasPressedThisFrame) MovePost(2);
        yaw += m.delta.ReadValue().x * .1f; pitch = Mathf.Clamp(pitch - m.delta.ReadValue().y * .1f, -70, 70);
        recoilPitch = Mathf.MoveTowards(recoilPitch, 0f, 10f * Time.deltaTime);
        machineGunSpread = Mathf.MoveTowards(machineGunSpread, 0f, 3f * Time.deltaTime);
        cam.transform.rotation = Quaternion.Euler(pitch - recoilPitch, yaw, 0);
        bool aiming = m.rightButton.isPressed;
        cam.fieldOfView = aiming ? 45 : 70;
        if (k.digit1Key.wasPressedThisFrame) weapon = Pistol;
        if (k.digit2Key.wasPressedThisFrame && owned[MachineGun]) weapon = MachineGun;
        if (k.digit3Key.wasPressedThisFrame && owned[LaserRifle]) weapon = LaserRifle;
        if (k.rKey.wasPressedThisFrame) Reload();
        if (k.gKey.wasPressedThisFrame) ThrowGrenade();
        bool pullingTrigger = weapon == MachineGun ? m.leftButton.isPressed : m.leftButton.wasPressedThisFrame;
        if (pullingTrigger && Time.time >= fireAt && Time.time >= reloadAt) Shoot(aiming);
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
