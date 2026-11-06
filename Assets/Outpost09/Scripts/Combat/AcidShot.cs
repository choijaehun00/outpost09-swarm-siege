using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AcidShot : MonoBehaviour
{
    Outpost09 game; Vector3 target; float born;
    public void Setup(Outpost09 g, Vector3 t) { game = g; target = t; born = Time.time; }
    void Update()
    {
        if (!game.Playing) return;
        var next = Vector3.MoveTowards(transform.position, target, 12 * Time.deltaTime);
        if (Physics.Linecast(transform.position, next, out var hit) && hit.collider.GetComponent<Alien>() == null) { Destroy(gameObject); return; }
        transform.position = next;
        if (Vector3.Distance(transform.position, target) < .25f) { if (Vector3.Distance(game.Target, target) < .5f) game.HitPlayer(10); Destroy(gameObject); }
        if (Time.time - born > 8) Destroy(gameObject);
    }
}
