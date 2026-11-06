using UnityEngine;

// Deterministic land survey. Roads cut into this landscape; they are not isolated
// platforms. No combat state is stored here, so the five AI routes stay unchanged.
public static class OutpostTerrainShape
{
    public const float RiverLevel = -12f;
    public const float CellSize = 1f;
    public const int MinX = -88, MinZ = -66, CellsX = 176, CellsZ = 178;

    static float Fade(float start, float end, float value) => Mathf.SmoothStep(0, 1, Mathf.InverseLerp(start, end, value));
    public static float Noise(float x, float z)
    {
        return Mathf.PerlinNoise(x * .032f + 71, z * .032f + 19) * .56f
            + Mathf.PerlinNoise(x * .093f + 27, z * .093f + 82) * .29f
            + Mathf.PerlinNoise(x * .28f + 53, z * .28f + 37) * .15f;
    }
    public static bool IsBridge(int route, int segment) => OutpostMapLayout.IsBridge(route, segment);

    // Independent river network: a western tributary joins the main river below
    // the central mesa. This survey does not depend on the road arrays.
    public static readonly Vector3[][] Rivers = {
        SmoothRiver(new [] {new Vector3(14,-2,-30),new Vector3(19,-2,-4),new Vector3(16,-6,12),new Vector3(11,-8,30),new Vector3(16,-10,43),new Vector3(7,-12,65),new Vector3(3,-12,90),new Vector3(10,-12,112)}),
        SmoothRiver(new [] {new Vector3(-32,-1,-9),new Vector3(-16,-3,5),new Vector3(-15,-7,21),new Vector3(-10,-9,41),new Vector3(-6,-11,55),new Vector3(7,-12,65)})
    };
    static Vector3[] SmoothRiver(Vector3[] guide)
    {
        var result=new System.Collections.Generic.List<Vector3>();
        for(int s=0;s<guide.Length-1;s++)
        {
            var a=guide[Mathf.Max(0,s-1)];var b=guide[s];var c=guide[s+1];var d=guide[Mathf.Min(guide.Length-1,s+2)];
            int steps=Mathf.CeilToInt(Vector3.Distance(b,c)/3);
            for(int n=s==0?0:1;n<=steps;n++)
            {
                float t=n/(float)steps;
                result.Add(.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t));
            }
        }
        return result.ToArray();
    }
    public static void RiverSurvey(float x,float z,out float distance,out float level,out float width)
    {
        distance=float.PositiveInfinity;level=RiverLevel;width=3;
        for(int r=0;r<Rivers.Length;r++) for(int s=0;s<Rivers[r].Length-1;s++)
        {
            Vector3 a=Rivers[r][s],b=Rivers[r][s+1];float dx=b.x-a.x,dz=b.z-a.z;
            float t=Mathf.Clamp01(((x-a.x)*dx+(z-a.z)*dz)/(dx*dx+dz*dz));
            float d=Mathf.Sqrt((x-a.x-dx*t)*(x-a.x-dx*t)+(z-a.z-dz*t)*(z-a.z-dz*t));
            if(d>=distance)continue;
            distance=d;level=Mathf.Lerp(a.y,b.y,t);
            width=(r==0?2.8f:2.1f)+Mathf.PerlinNoise(x*.048f+12,z*.048f+45)*1.7f;
        }
    }
    public static void NearestRoad(float x, float z, out float distance, out float elevation, out bool bridge)
    {
        distance = float.PositiveInfinity; elevation = 3; bridge = false;
        for (int r = 0; r < OutpostMapLayout.Routes.Length; r++)
        {
            var points = OutpostMapLayout.Routes[r];
            for (int s = 0; s < points.Length - 1; s++)
            {
                Vector3 a = points[s], b = points[s + 1];
                float dx = b.x - a.x, dz = b.z - a.z, length2 = dx * dx + dz * dz;
                float t = length2 > .0001f ? Mathf.Clamp01(((x - a.x) * dx + (z - a.z) * dz) / length2) : 0;
                float d = Mathf.Sqrt(Mathf.Pow(x - a.x - dx * t, 2) + Mathf.Pow(z - a.z - dz * t, 2));
                if (d >= distance) continue;
                distance = d; elevation = Mathf.Lerp(a.y, b.y, t); bridge = IsBridge(r, s);
            }
        }
    }
    public static float NaturalHeight(float x, float z)
    {
        float noise=Noise(x,z);
        float warpX=x+(Mathf.PerlinNoise(x*.025f+18,z*.025f+32)-.5f)*9;
        float warpZ=z+(Mathf.PerlinNoise(x*.021f+61,z*.021f+11)-.5f)*11;
        float west=1-Fade(.35f,1.2f,Mathf.Sqrt(Mathf.Pow((warpX+39)/30,2)+Mathf.Pow((warpZ-22)/60,2)));
        float east=1-Fade(.30f,1.15f,Mathf.Sqrt(Mathf.Pow((warpX-40)/34,2)+Mathf.Pow((warpZ-18)/49,2)));
        float height=3+noise*4+west*(8+noise*4)+east*(4+noise*6);
        height+=Fade(43,82,Mathf.Abs(warpX))*(14+noise*19);
        height+=Fade(-26,-63,warpZ)*(16+noise*16);
        height+=Fade(88,111,warpZ)*(7+noise*9);
        // Low-amplitude ledges follow slightly tilted strata; no repeated steps.
        height+=Mathf.Sin((height+x*.025f+z*.018f)*1.5f)*.22f;
        for (int i = 0; i < OutpostMapLayout.StationCenters.Length; i++)
        {
            var centre = OutpostMapLayout.StationCenters[i];
            var f = OutpostMapLayout.StationForward(i);
            float dx = x - centre.x, dz = z - centre.z;
            float along = dx * f.x + dz * f.z;
            float across = dx * f.z - dz * f.x;
            float lateral=i==0?16:i==1?11:18;
            float rearLength=i==0?25:i==1?17:28;
            float coast=1-Fade(.42f,1.05f,Mathf.Sqrt(Mathf.Pow(across/(lateral+noise*3),2)+Mathf.Pow((along+6)/rearLength,2)));
            float nose=1-Fade(1.6f,7,along+(noise-.5f)*1.2f);
            float top=centre.y-3.25f+(noise-.5f)*.25f;
            height=Mathf.Max(height,Mathf.Lerp(2,top,coast*nose));
        }
        RiverSurvey(x,z,out float riverDistance,out float level,out float width);
        // A broad shoulder, near-vertical bank and narrow watercourse; both rivers
        // really meet at the central confluence instead of running in parallel.
        float bank=1-Fade(width*.8f,width+4+noise*2,riverDistance);
        height=Mathf.Lerp(height,level-1.2f,bank);
        float gateDistance=Vector2.Distance(new Vector2(x,z),new Vector2(0,-24));
        height=Mathf.Lerp(height,2.95f,1-Fade(14,23,gateDistance));
        return height;
    }
    public static float Height(float x,float z)
    {
        float height=NaturalHeight(x,z);
        float best=float.PositiveInfinity,total=0,elevation=0;
        // Blend neighbouring ground segments. The former nearest-segment switch
        // produced hard triangular cliffs at every bridge approach.
        for(int r=0;r<OutpostMapLayout.Routes.Length;r++)
        {
            var route=OutpostMapLayout.Routes[r];
            for(int s=0;s<route.Length-1;s++)
            {
                if(IsBridge(r,s))continue;
                var a=route[s];var b=route[s+1];float dx=b.x-a.x,dz=b.z-a.z;
                float t=Mathf.Clamp01(((x-a.x)*dx+(z-a.z)*dz)/(dx*dx+dz*dz));
                float d2=(x-a.x-dx*t)*(x-a.x-dx*t)+(z-a.z-dz*t)*(z-a.z-dz*t);
                if(d2>42.25f)continue;
                float weight=1/(.001f+d2*d2*d2);
                total+=weight;elevation+=Mathf.Lerp(a.y,b.y,t)*weight;best=Mathf.Min(best,Mathf.Sqrt(d2));
            }
        }
        if(total>0)height=Mathf.Lerp(height,elevation/total-.045f,1-Fade(1.9f,6.5f,best));
        return height;
    }
}
