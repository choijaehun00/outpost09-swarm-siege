using UnityEngine;

public class AcidShot : MonoBehaviour
{
    internal const float Diameter = .45f;
    const float Speed = 12f;
    const float ArcHeight = 4f;

    OutpostCombat combat;
    Vector3 origin, target;
    float duration, elapsed;

    internal void Setup(OutpostCombat owner, Vector3 aimPoint)
    {
        combat = owner;
        origin = transform.position;
        target = aimPoint;
        duration = Vector3.Distance(origin, target) / Speed;
    }

    void Update()
    {
        if (combat == null || !combat.Playing) return;

        elapsed += Time.deltaTime;
        float progress = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
        Vector3 position = transform.position;
        Vector3 next = Vector3.Lerp(origin, target, progress);
        next.y += 4f * ArcHeight * progress * (1f - progress);
        Vector3 travel = next - position;
        if (travel.sqrMagnitude > 0f)
        {
            var hits = Physics.SphereCastAll(position, Diameter * .5f, travel.normalized,
                travel.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits)
            {
                if (hit.collider.GetComponentInParent<Alien>() != null) continue;
                Destroy(gameObject);
                return;
            }
        }

        transform.position = next;
        if (progress < 1f) return;

        if (combat.Target == target) combat.ReportPlayerHit(10);
        Destroy(gameObject);
    }
}
