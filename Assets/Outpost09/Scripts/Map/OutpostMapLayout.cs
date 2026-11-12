using UnityEngine;

// Map contract: presentation owns coordinates; combat consumes these routes.
// World forward (+Z) points OUT from the gate towards the invasion front.
public static class OutpostMapLayout
{
    public static readonly Vector3 Gate = new Vector3(0, 3, -24);
    // Keep the rock/platform centres independent from the camera's firing position.
    public static readonly Vector3[] StationCenters = {
        new Vector3(-25, 28, 4), new Vector3(0, 23, 26), new Vector3(26, 26, 3)
    };
    public static readonly string[] StationNames = { "A / RIDGE WATCH", "B / CANYON HEART", "C / EAST BASTION" };
    public static readonly Vector3[] AimPoints = {
        new Vector3(-12, 3, 41), new Vector3(0, 3, 65), new Vector3(14, 3, 41)
    };
    public const float PlatformRadius = 3.8f;
    public const float FiringOffset = 4.65f;
    public static readonly Vector3[] Stations = CreateFiringPositions();

    public static Vector3 StationForward(int index)
    {
        var direction = AimPoints[index] - StationCenters[index];
        direction.y = 0;
        return direction.normalized;
    }

    static Vector3[] CreateFiringPositions()
    {
        var positions = new Vector3[StationCenters.Length];
        for (int i = 0; i < positions.Length; i++)
            positions[i] = StationCenters[i] + StationForward(i) * FiringOffset + Vector3.up * .35f;
        return positions;
    }
    // Sparse survey guides describe the land routes; the dense shared Routes
    // below round their corners and grade heights against the natural land survey.
    static readonly Vector3[][] Guides = {
        new [] { new Vector3(-42,0,65),new Vector3(-36,0,53),new Vector3(-30,0,39),new Vector3(-24,0,31),new Vector3(-22,0,17),new Vector3(-8,0,4),new Vector3(-6,0,-10),new Vector3(0,3,-20) },
        new [] { new Vector3(-3,0,77),new Vector3(0,0,64),new Vector3(2,0,52),new Vector3(-4,0,43),new Vector3(-20,0,33),new Vector3(-22,0,17),new Vector3(-8,0,4),new Vector3(-6,0,-10),new Vector3(0,3,-20) },
        new [] { new Vector3(42,0,62),new Vector3(34,0,52),new Vector3(34,0,38),new Vector3(30,0,26),new Vector3(35,0,12),new Vector3(39,0,3),new Vector3(35,0,-8),new Vector3(26,0,-12),new Vector3(11,0,-13),new Vector3(0,3,-20) },
        new [] { new Vector3(-47,0,21),new Vector3(-38,0,18),new Vector3(-39,0,5),new Vector3(-33,0,-7),new Vector3(-21,0,-12),new Vector3(-6,0,-10),new Vector3(0,3,-20) },
        new [] { new Vector3(48,0,2),new Vector3(41,0,-5),new Vector3(31,0,-13),new Vector3(26,0,-12),new Vector3(11,0,-13),new Vector3(0,3,-20) }
    };
    public static readonly bool[][] RouteBridges = new bool[Guides.Length][];
    public static readonly Vector3[][] Routes = BuildRoutes();
    public static int RouteCount => Routes.Length;
    static bool GuideBridge(int r,int s) => (r==0&&s==4)||(r==1&&(s==3||s==5))||(r==2&&s==7)||(r==3&&s==3)||(r==4&&s==3);
    public static bool IsBridge(int route,int segment) => route>=0 && route<RouteBridges.Length && segment>=0 && segment<RouteBridges[route].Length && RouteBridges[route][segment];

    static Vector3[][] BuildRoutes()
    {
        var result=new Vector3[Guides.Length][];
        for(int r=0;r<Guides.Length;r++)
        {
            var guide=Guides[r];var points=new System.Collections.Generic.List<Vector3>();var bridges=new System.Collections.Generic.List<bool>();
            for(int s=0;s<guide.Length-1;s++)
            {
                Vector3 a=guide[s],b=guide[s+1];bool bridge=GuideBridge(r,s);
                int steps=Mathf.Max(2,Mathf.CeilToInt(Vector3.Distance(a,b)/1.6f));
                float ha=Mathf.Max(1.5f,OutpostTerrainShape.NaturalHeight(a.x,a.z));
                float hb=s==guide.Length-2?Gate.y:Mathf.Max(1.5f,OutpostTerrainShape.NaturalHeight(b.x,b.z));
                for(int n=s==0?0:1;n<=steps;n++)
                {
                    float t=n/(float)steps;Vector3 p;
                    if(bridge)p=Vector3.Lerp(a,b,t);
                    else
                    {
                        Vector3 before=guide[Mathf.Max(0,s-1)],after=guide[Mathf.Min(guide.Length-1,s+2)];
                        Vector3 ma=s>0&&GuideBridge(r,s-1)?b-a:(b-before)*.4f;
                        Vector3 mb=s<guide.Length-2&&GuideBridge(r,s+1)?b-a:(after-a)*.4f;
                        p=(2*t*t*t-3*t*t+1)*a+(t*t*t-2*t*t+t)*ma+(-2*t*t*t+3*t*t)*b+(t*t*t-t*t)*mb;
                    }
                    p.y=bridge?Mathf.Lerp(ha,hb,t):Mathf.Max(1.5f,OutpostTerrainShape.NaturalHeight(p.x,p.z));
                    points.Add(p);if(points.Count>1)bridges.Add(bridge);
                }
            }
            var last=points[points.Count-1];last.y=Gate.y;points[points.Count-1]=last;
            // Small grading works back from the gate. Do not create abrupt ramps.
            for(int i=points.Count-2;i>=0;i--)
            {
                var p=points[i];var q=points[i+1];float distance=Vector2.Distance(new Vector2(p.x,p.z),new Vector2(q.x,q.z));
                p.y=Mathf.Clamp(p.y,q.y-distance*.30f,q.y+distance*.30f);points[i]=p;
            }
            // A complete bridge span has a straight, common grade.
            for(int s=0;s<bridges.Count;s++)
            {
                if(!bridges[s])continue;
                int end=s+1;while(end<bridges.Count&&bridges[end])end++;
                for(int n=s+1;n<end;n++){var p=points[n];p.y=Mathf.Lerp(points[s].y,points[end].y,(n-s)/(float)(end-s));points[n]=p;}
                s=end-1;
            }
            result[r]=points.ToArray();RouteBridges[r]=bridges.ToArray();
        }
        return result;
    }

    public static bool TryGetRoute(int index, out Vector3[] route)
    {
        route = null;
        if (index < 0 || index >= Routes.Length) return false;
        var candidate = Routes[index];
        if (candidate == null || candidate.Length < 2) return false;
        route = candidate;
        return true;
    }

    public static bool TryFindNearestRoute(Vector3 position, out int routeIndex, out int nextPoint)
    {
        routeIndex = -1;
        nextPoint = 1;
        float best = float.PositiveInfinity;
        for (int r = 0; r < Routes.Length; r++)
        {
            if (!TryGetRoute(r, out var points)) continue;
            for (int n = 1; n < points.Length; n++)
            {
                var segment = points[n] - points[n - 1];
                float t = segment.sqrMagnitude > .0001f
                    ? Mathf.Clamp01(Vector3.Dot(position - points[n - 1], segment) / segment.sqrMagnitude) : 0;
                float distance = (position - (points[n - 1] + segment * t)).sqrMagnitude;
                if (distance >= best) continue;
                best = distance;
                routeIndex = r;
                nextPoint = n;
            }
        }
        return routeIndex >= 0;
    }
}
