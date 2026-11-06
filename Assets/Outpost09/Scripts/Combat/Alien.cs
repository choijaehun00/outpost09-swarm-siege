using UnityEngine;

public class Alien : MonoBehaviour
{
    // Persist scalar state, and resolve map waypoints by ID after script reloads.
    [SerializeField] Outpost09 game;
    [SerializeField] int kind, hp;
    [SerializeField] float speed, attackAt;
    [SerializeField] int routeIndex = -1;
    [SerializeField] int nextPoint = 1;
    [SerializeField] bool hasRoute, dead;

    float Clearance => kind == 3 ? 2f : kind == 1 ? .7f : 1f;

    public void Setup(Outpost09 g, int k, int requestedRoute = 0)
    {
        game = g;
        kind = Mathf.Clamp(k, 0, 3);
        hp = new[] { 50, 30, 70, 250 }[kind];
        speed = new[] { 3.5f, 6, 2.5f, 1.8f }[kind];
        routeIndex = requestedRoute;
        nextPoint = 1;
        hasRoute = OutpostMapLayout.TryGetRoute(routeIndex, out _);
        dead = false;
    }

    bool ResolveRoute(out Vector3[] route)
    {
        if (hasRoute && OutpostMapLayout.TryGetRoute(routeIndex, out route))
        {
            nextPoint = Mathf.Clamp(nextPoint, 1, route.Length - 1);
            return true;
        }
        // Enemies already in Play mode may predate the new route fields.
        if (OutpostMapLayout.TryFindNearestRoute(transform.position - Vector3.up * Clearance,
            out routeIndex, out nextPoint) && OutpostMapLayout.TryGetRoute(routeIndex, out route))
        {
            hasRoute = true;
            return true;
        }
        route = null;
        Debug.LogError("OUTPOST 09: No valid enemy routes. Check OutpostMapLayout.Routes.", this);
        enabled = false;
        return false;
    }
    void Update()
    {
        if (dead) return;
        if (game == null) game = FindAnyObjectByType<Outpost09>();
        if (game == null || !game.Playing || !ResolveRoute(out var route)) return;
        var target = route[nextPoint] + Vector3.up * Clearance;
        // Range must be measured in metres, not "last 3 points": the artist's
        // shared path now contains densely sampled, rounded corners.
        if (kind == 2 && Vector3.Distance(transform.position, OutpostMapLayout.Gate) < 36f)
        {
            if (Time.time >= attackAt) { attackAt = Time.time + 3; var shot = GameObject.CreatePrimitive(PrimitiveType.Sphere); shot.name = "Acid projectile"; shot.transform.position = transform.position + Vector3.up * .5f; shot.transform.localScale = Vector3.one * .45f; shot.GetComponent<Renderer>().material.color = Color.green; shot.AddComponent<AcidShot>().Setup(game, game.Target); }
            return;
        }
        if (Vector3.Distance(transform.position, target) > .08f)
        {
            var direction = target - transform.position;
            var horizontal = new Vector3(direction.x, 0, direction.z);
            if (horizontal.sqrMagnitude > .001f) transform.rotation = Quaternion.LookRotation(-horizontal);
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        }
        else if (nextPoint < route.Length - 1) nextPoint++;
        else if (Time.time >= attackAt) { attackAt = Time.time + (kind == 1 ? 1 : 2); game.HitGate(kind == 3 ? 15 : kind == 1 ? 3 : 5); }
    }
    public void Damage(int value)
    {
        if (dead || value <= 0) return;
        hp -= value;
        if (hp > 0) return;
        dead = true;
        if (game != null) game.Dead(this, new[] { 20, 30, 40, 100 }[Mathf.Clamp(kind, 0, 3)]);
        Destroy(gameObject);
    }
}
