using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Alien : MonoBehaviour
{
    Outpost09 game; int kind, hp; float speed, attackAt;
    public void Setup(Outpost09 g, int k) { game = g; kind = k; hp = new[] { 50, 30, 70, 250 }[k]; speed = new[] { 3.5f, 6, 2.5f, 1.8f }[k]; }
    void Update()
    {
        if (!game.Playing) return;
        var target = new Vector3(0, transform.position.y, -14);
        if (kind == 2 && transform.position.z < 19)
        {
            if (Time.time >= attackAt) { attackAt = Time.time + 3; var shot = GameObject.CreatePrimitive(PrimitiveType.Sphere); shot.name = "Acid projectile"; shot.transform.position = transform.position + Vector3.up * .5f; shot.transform.localScale = Vector3.one * .45f; shot.GetComponent<Renderer>().material.color = Color.green; shot.AddComponent<AcidShot>().Setup(game, game.Target); }
            return;
        }
        if (Vector3.Distance(transform.position, target) > 2) transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        else if (Time.time >= attackAt) { attackAt = Time.time + (kind == 1 ? 1 : 2); game.HitGate(new[] { 5, 3, 0, 15 }[kind]); }
    }
    public void Damage(int value) { hp -= value; if (hp <= 0) { game.Dead(this, new[] { 20, 30, 40, 100 }[kind]); Destroy(gameObject); } }
}
