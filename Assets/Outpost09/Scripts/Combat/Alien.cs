using System.Collections.Generic;
using UnityEngine;
using EnemyKind = OutpostCombat.EnemyKind;

public class Alien : MonoBehaviour
{
    // Persist scalar state, and resolve map waypoints by ID after script reloads.
    [SerializeField] OutpostCombat combat;
    [SerializeField] EnemyKind kind;
    [SerializeField] int hp;
    [SerializeField] float speed, attackAt;
    [SerializeField] int routeIndex = -1;
    [SerializeField] int nextPoint = 1;
    [SerializeField] bool hasRoute, dead;

    static readonly int[] HealthByKind = { 50, 30, 70, 250 };
    static readonly float[] SpeedByKind = { 3.5f, 6f, 2.5f, 1.8f };

    internal static float BodyClearance(EnemyKind kind) => kind == EnemyKind.Bruiser ? 2f : kind == EnemyKind.Sprinter ? .7f : 1f;

    internal void Setup(OutpostCombat owner, EnemyKind enemyKind, int requestedRoute)
    {
        combat = owner;
        kind = enemyKind;
        hp = HealthByKind[(int)kind];
        speed = SpeedByKind[(int)kind];
        routeIndex = requestedRoute;
        nextPoint = 1;
        hasRoute = combat.Battlefield.TryGetRoute(routeIndex, out _);
        dead = false;
    }

    bool ResolveRoute(out IReadOnlyList<Vector3> route)
    {
        var map = combat.Battlefield;
        if (hasRoute && map.TryGetRoute(routeIndex, out route))
        {
            nextPoint = Mathf.Clamp(nextPoint, 1, route.Count - 1);
            return true;
        }
        // Enemies already in Play mode may predate the new route fields.
        if (map.TryFindNearestRoute(transform.position - Vector3.up * BodyClearance(kind),
            out routeIndex, out nextPoint) && map.TryGetRoute(routeIndex, out route))
        {
            hasRoute = true;
            return true;
        }
        route = null;
        Debug.LogError("OUTPOST 09: The active map has no valid enemy routes.", this);
        enabled = false;
        return false;
    }
    void Update()
    {
        if (dead) return;
        if (combat == null) combat = FindAnyObjectByType<OutpostCombat>();
        if (combat == null || !combat.Playing || !ResolveRoute(out var route)) return;
        var target = route[nextPoint] + Vector3.up * BodyClearance(kind);
        // Range must be measured in metres, not "last 3 points": the artist's
        // shared path now contains densely sampled, rounded corners.
        if (kind == EnemyKind.Spitter && Vector3.Distance(transform.position, combat.Battlefield.GatePosition) < 36f)
        {
            if (Time.time >= attackAt) ShootAcid();
            return;
        }
        if (Vector3.Distance(transform.position, target) > .08f)
        {
            var direction = target - transform.position;
            var horizontal = new Vector3(direction.x, 0, direction.z);
            if (horizontal.sqrMagnitude > .001f) transform.rotation = Quaternion.LookRotation(-horizontal);
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        }
        else if (nextPoint < route.Count - 1) nextPoint++;
        else if (Time.time >= attackAt)
        {
            bool sprinter = kind == EnemyKind.Sprinter;
            attackAt = Time.time + (sprinter ? 1f : 2f);
            combat.ReportGateHit(kind == EnemyKind.Bruiser ? 15 : sprinter ? 3 : 5);
        }
    }

    void ShootAcid()
    {
        attackAt = Time.time + 3f;
        var shot = combat.TrackTransient(GameObject.CreatePrimitive(PrimitiveType.Sphere));
        shot.name = "Acid projectile";
        shot.transform.position = transform.position + Vector3.up * .5f;
        shot.transform.localScale = Vector3.one * AcidShot.Diameter;
        shot.GetComponent<Renderer>().material.color = Color.green;
        shot.GetComponent<Collider>().enabled = false;
        shot.AddComponent<AcidShot>().Setup(combat, combat.Target);
    }

    public void Damage(int value)
    {
        if (dead || value <= 0) return;
        hp -= value;
        if (hp > 0) return;
        dead = true;
        if (combat != null) combat.ReportEnemyDefeated(this, kind);
        Destroy(gameObject);
    }
}
