using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Grenade : MonoBehaviour
{
    const float BlastRadius = 5f;

    OutpostCombat combat;
    Rigidbody body;
    float fuse = 2f;

    internal void Launch(OutpostCombat owner, Vector3 velocity)
    {
        combat = owner;
        body = GetComponent<Rigidbody>();
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        body.linearVelocity = velocity;
    }

    void FixedUpdate()
    {
        if (combat == null || !combat.Playing) return;

        fuse -= Time.fixedDeltaTime;
        if (fuse > 0f) return;

        GetComponent<Collider>().enabled = false;
        enabled = false;
        combat.ExplodeGrenade(body.position, BlastRadius);
        Destroy(gameObject);
    }
}
