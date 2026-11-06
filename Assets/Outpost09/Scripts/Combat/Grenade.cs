using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Grenade : MonoBehaviour
{
    const float BlastRadius = 5f;

    Outpost09 game;
    Rigidbody body;
    float fuse = 2f;

    internal void Launch(Outpost09 owner, Vector3 velocity)
    {
        game = owner;
        body = GetComponent<Rigidbody>();
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        body.linearVelocity = velocity;
    }

    void FixedUpdate()
    {
        if (game == null || !game.Playing) return;

        fuse -= Time.fixedDeltaTime;
        if (fuse > 0f) return;

        GetComponent<Collider>().enabled = false;
        enabled = false;
        game.ExplodeGrenade(body.position, BlastRadius);
        Destroy(gameObject);
    }
}
