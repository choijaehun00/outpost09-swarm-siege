using System.Collections.Generic;
using UnityEngine;

public abstract class OutpostBattlefield : MonoBehaviour
{
    public abstract Vector3 GatePosition { get; }
    public abstract int RouteCount { get; }

    // Station indices follow the left, center, right controls.
    public abstract bool TryGetStation(int index, out Vector3 position, out Vector3 aimPoint);
    public abstract bool TryGetRoute(int index, out IReadOnlyList<Vector3> points);
    public abstract bool TryFindNearestRoute(Vector3 position, out int routeIndex, out int nextPoint);
}
