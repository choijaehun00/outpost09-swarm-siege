using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Owner: Combat team. Shared state is declared in Assets/Outpost09.cs.
public partial class Outpost09
{
    void MovePost(int index, bool force = false)
    {
        if (!force && (index == post || Time.time < cooldown)) return;
        post = index; cooldown = Time.time + (force ? 0 : 1); flash = Time.time + .18f;
        cam.transform.position = posts[post];
        cam.transform.LookAt(new Vector3(0, 1, 18));
        yaw = cam.transform.eulerAngles.y; pitch = cam.transform.eulerAngles.x;
    }

    void Spawn(int kind, int n)
    {
        float x = ((n + kind) % 3 - 1) * 11 + Random.Range(-2f, 2f);
        var o = GameObject.CreatePrimitive(PrimitiveType.Capsule); o.name = new[] { "Runner", "Sprinter", "Spitter", "Bruiser" }[kind];
        o.transform.position = new Vector3(x, 1, 45 + Random.Range(-4, 5));
        o.transform.localScale = kind == 3 ? new Vector3(2, 2, 2) : kind == 1 ? new Vector3(.7f, .7f, .7f) : Vector3.one;
        o.GetComponent<Renderer>().material = Mat(new[] { Color.green, Color.yellow, Color.magenta, new Color(.4f, .2f, .5f) }[kind]);
        var e = o.AddComponent<Alien>(); e.Setup(this, kind); enemies.Add(e);
        for (int leg = -1; leg <= 1; leg += 2)
        {
            var limb = Box("Clawed limb", Vector3.zero, new Vector3(.15f, .75f, .2f), new Color(.12f, .1f, .18f));
            limb.transform.SetParent(o.transform, false); limb.transform.localPosition = new Vector3(leg * .65f, -.55f, 0);
            limb.transform.localRotation = Quaternion.Euler(0, 0, leg * 30);
            Destroy(limb.GetComponent<Collider>());
        }
        var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere); eye.name = "Glow"; eye.transform.SetParent(o.transform); eye.transform.localPosition = new Vector3(0, .25f, -.48f); eye.transform.localScale = Vector3.one * .28f; eye.GetComponent<Renderer>().material = Mat(Color.cyan);
        Destroy(eye.GetComponent<Collider>());
        o.GetComponent<Renderer>().material = Mat(kind == 3 ? new Color(.20f, .14f, .12f) : new Color(.19f, .24f, .12f));
        var shell = new Color(.12f, .16f, .10f);
        for (int segment = 0; segment < 4; segment++)
            Part(o.transform, "Overlapping carapace", PrimitiveType.Sphere, new Vector3(0, .25f - segment * .22f, .06f), new Vector3(1.08f, .4f, .9f), shell);
        Part(o.transform, "Armored head", PrimitiveType.Sphere, new Vector3(0, .65f, -.25f), new Vector3(.75f, .55f, .75f), shell);
        for (int side = -1; side <= 1; side += 2)
        {
            Part(o.transform, "Mandible", PrimitiveType.Capsule, new Vector3(side * .23f, .35f, -.62f), new Vector3(.1f, .28f, .1f), new Color(.48f, .43f, .29f), new Vector3(65, side * 25, 0));
            for (int joint = 0; joint < 3; joint++)
            {
                var hip = new Vector3(side * .43f, -.05f, -.3f + joint * .3f);
                Part(o.transform, "Leg upper joint", PrimitiveType.Capsule, hip + new Vector3(side * .26f, -.1f, 0), new Vector3(.13f, .38f, .13f), shell, new Vector3(0, 0, side * 60));
                Part(o.transform, "Leg lower claw", PrimitiveType.Capsule, hip + new Vector3(side * .55f, -.55f, .08f), new Vector3(.085f, .48f, .085f), shell, new Vector3(15, 0, side * 15));
            }
        }
    }

    void Update()
    {
        if (mode != "playing") return;
        if (displayedWeapon != weapon) RebuildWeapon();
        elapsed += Time.deltaTime;
        var k = Keyboard.current; var m = Mouse.current;
        if (k == null || m == null) return;
        if (k.escapeKey.wasPressedThisFrame) { mode = "pause"; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; return; }
        if (k.f1Key.wasPressedThisFrame) MovePost(0);
        if (k.f2Key.wasPressedThisFrame) MovePost(1);
        if (k.f3Key.wasPressedThisFrame) MovePost(2);
        yaw += m.delta.ReadValue().x * .1f; pitch = Mathf.Clamp(pitch - m.delta.ReadValue().y * .1f, -70, 70);
        cam.transform.rotation = Quaternion.Euler(pitch, yaw, 0);
        cam.fieldOfView = m.rightButton.isPressed ? 45 : 70;
        if (k.digit1Key.wasPressedThisFrame) weapon = 0;
        if (k.digit2Key.wasPressedThisFrame && owned[1]) weapon = 1;
        if (k.digit3Key.wasPressedThisFrame && owned[2]) weapon = 2;
        if (k.rKey.wasPressedThisFrame) Reload();
        if (k.gKey.wasPressedThisFrame && grenades > 0) ThrowGrenade();
        if ((weapon == 1 ? m.leftButton.isPressed : m.leftButton.wasPressedThisFrame) && Time.time >= fireAt && Time.time >= reloadAt) Shoot();
    }

    void Shoot()
    {
        if (ammo[weapon] <= 0) { Reload(); return; }
        ammo[weapon]--; fireAt = Time.time + (weapon == 0 ? 1f / 3 : weapon == 1 ? .1f : .25f);
        var direction = cam.transform.forward + (weapon == 1 ? Random.insideUnitSphere * .025f : Vector3.zero);
        if (Physics.Raycast(cam.transform.position, direction, out var hit, 120))
        {
            var e = hit.collider.GetComponentInParent<Alien>(); if (e != null) e.Damage(weapon == 0 ? 25 : weapon == 1 ? 12 : 40);
        }
        flash = Time.time + .07f;
        StartCoroutine(ShotEffect(cam.transform.position + cam.transform.forward * .8f, direction));
    }

    void Reload() { if (Time.time >= reloadAt) { reloadAt = Time.time + (weapon == 0 ? 1.5f : weapon == 1 ? 2.5f : 2); StartCoroutine(FinishReload(weapon, reloadAt)); } }

    IEnumerator FinishReload(int w, float at) { yield return new WaitForSeconds(at - Time.time); ammo[w] = w == 0 ? 12 : w == 1 ? 60 : 30; }

    void ThrowGrenade()
    {
        grenades--;
        var o = GameObject.CreatePrimitive(PrimitiveType.Sphere); o.name = "Grenade"; o.transform.position = cam.transform.position + cam.transform.forward; o.transform.localScale = Vector3.one * .35f;
        o.GetComponent<Renderer>().material = Mat(Color.gray); var rb = o.AddComponent<Rigidbody>(); rb.AddForce(cam.transform.forward * 16 + Vector3.up * 5, ForceMode.VelocityChange);
        StartCoroutine(Explode(o));
    }

    IEnumerator Explode(GameObject o)
    {
        yield return new WaitForSeconds(2); if (o == null) yield break;
        var pos = o.transform.position; foreach (var e in enemies.ToArray()) if (e != null && Vector3.Distance(pos, e.transform.position) < 5) e.Damage(Mathf.RoundToInt(120 * (1 - Vector3.Distance(pos, e.transform.position) / 5)));
        var fx = GameObject.CreatePrimitive(PrimitiveType.Sphere); fx.transform.position = pos; fx.transform.localScale = Vector3.one * 5; fx.GetComponent<Renderer>().material = Mat(new Color(1, .4f, .05f)); Destroy(fx.GetComponent<Collider>()); Destroy(fx, .18f); Destroy(o);
    }
}
