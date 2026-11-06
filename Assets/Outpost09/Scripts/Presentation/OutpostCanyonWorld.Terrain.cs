using System.Collections.Generic;
using UnityEngine;

// Presentation owner: the continuous land mesh, natural trails and anchored props.
public partial class OutpostCanyonWorld
{
    Material wornDirt;
    bool IsBridgeSegment(int route, int segment) => OutpostTerrainShape.IsBridge(route, segment);

    void NaturalLandforms()
    {
        Cube("Deep riverbed", new Vector3(0, -16, 20), new Vector3(190, 2, 190), rockDark, terrainRoot);
        OrganicMaterials();
        RiverSurfaces();
        wornDirt = Surface("Worn sandy trail", new Color(.57f, .38f, .25f), 0, .08f);
        wornDirt.SetTexture("_BaseMap", sand.GetTexture("_BaseMap"));
        int width = OutpostTerrainShape.CellsX + 1, depth = OutpostTerrainShape.CellsZ + 1;
        var heights = new float[width, depth];
        terrainHeights = heights;
        for (int z = 0; z < depth; z++) for (int x = 0; x < width; x++)
            heights[x, z] = OutpostTerrainShape.Height(OutpostTerrainShape.MinX + x, OutpostTerrainShape.MinZ + z);
        // Small chunks allow 16-bit meshes, local collider cooking and culling.
        const int chunk = 24;
        for (int z = 0; z < depth - 1; z += chunk) for (int x = 0; x < width - 1; x += chunk)
            TerrainChunk(heights, x, z, Mathf.Min(chunk, width - 1 - x), Mathf.Min(chunk, depth - 1 - z));
        NaturalArch(new Vector3(39, Mathf.Max(GroundSurface(31.5f,59),GroundSurface(46.5f,59))+.2f, 59));
    }
    Vector3 TerrainVertex(float[,] heights, int x, int z) => new Vector3(OutpostTerrainShape.MinX + x, heights[x, z], OutpostTerrainShape.MinZ + z);
    Vector3 TerrainNormal(float[,] heights, int x, int z)
    {
        int maxX = heights.GetLength(0) - 1, maxZ = heights.GetLength(1) - 1;
        float dx = heights[Mathf.Min(x + 1, maxX), z] - heights[Mathf.Max(x - 1, 0), z];
        float dz = heights[x, Mathf.Min(z + 1, maxZ)] - heights[x, Mathf.Max(z - 1, 0)];
        return new Vector3(-dx, 2, -dz).normalized;
    }
    void TerrainChunk(float[,] heights, int startX, int startZ, int sizeX, int sizeZ)
    {
        var vertices = new List<Vector3>(); var normals = new List<Vector3>(); var uv = new List<Vector2>();
        var submeshes = new[] { new List<int>(), new List<int>(), new List<int>() };
        for (int z = startZ; z < startZ + sizeZ; z++) for (int x = startX; x < startX + sizeX; x++)
        {
            AddTerrainTriangle(heights, x, z, x, z + 1, x + 1, z, vertices, normals, uv, submeshes);
            AddTerrainTriangle(heights, x + 1, z, x, z + 1, x + 1, z + 1, vertices, normals, uv, submeshes);
        }
        var mesh = new Mesh { name = "Continuous eroded land " + startX + "_" + startZ };
        mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uv); mesh.subMeshCount = 3;
        for (int i = 0; i < 3; i++) mesh.SetTriangles(submeshes[i], i);
        mesh.RecalculateTangents(); mesh.RecalculateBounds(); ownedMeshes.Add(mesh);
        var go = new GameObject("Connected canyon terrain " + startX + "_" + startZ); go.transform.SetParent(terrainRoot, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterials = new[] { geological, geological, geological };
        go.AddComponent<MeshCollider>().sharedMesh = mesh;
    }
    void AddTerrainTriangle(float[,] heights, int ax, int az, int bx, int bz, int cx, int cz,
        List<Vector3> vertices, List<Vector3> normals, List<Vector2> uv, List<int>[] triangles)
    {
        Vector3 a = TerrainVertex(heights, ax, az), b = TerrainVertex(heights, bx, bz), c = TerrainVertex(heights, cx, cz);
        Vector3 face = Vector3.Cross(b - a, c - a).normalized;
        int material = face.y > .72f ? 0 : (Mathf.FloorToInt((a.y + b.y + c.y) / 5) % 4 == 0 ? 2 : 1);
        int first = vertices.Count; vertices.Add(a); vertices.Add(b); vertices.Add(c);
        normals.Add(TerrainNormal(heights, ax, az)); normals.Add(TerrainNormal(heights, bx, bz)); normals.Add(TerrainNormal(heights, cx, cz));
        // Project steep faces vertically so their sediment texture is not stretched.
        foreach (var p in new[] { a, b, c })
            uv.Add(face.y > .72f ? new Vector2(p.x, p.z) / 7 : Mathf.Abs(face.x) > Mathf.Abs(face.z) ? new Vector2(p.z, p.y) / 7 : new Vector2(p.x, p.y) / 7);
        triangles[material].Add(first); triangles[material].Add(first + 1); triangles[material].Add(first + 2);
    }
    void DustTrail(Vector3 a, Vector3 b)
    {
        Vector3 forward = b - a; forward.y = 0; forward.Normalize();
        var right = Vector3.Cross(Vector3.up, forward);
        int steps = Mathf.Max(2, Mathf.CeilToInt(Vector3.Distance(a, b) * 1.5f));
        var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
        for (int i = 0; i <= steps; i++)
        {
            Vector3 centre = Vector3.Lerp(a, b, i / (float)steps);
            float halfWidth = 1.6f + Mathf.PerlinNoise(centre.x * .27f + 15, centre.z * .3f) * .45f;
            for (int side = -1; side <= 1; side += 2)
            {
                var p = centre + right * side * halfWidth;
                p.y = OutpostTerrainShape.Height(p.x, p.z) + .055f;
                vertices.Add(p); uv.Add(new Vector2(side == -1 ? 0 : 1, i / 4f));
            }
            if (i < steps) { int n = i * 2; triangles.AddRange(new[] { n, n + 2, n + 1, n + 1, n + 2, n + 3 }); }
        }
        var mesh = new Mesh { name = "Trail following natural ground" };
        mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); ownedMeshes.Add(mesh);
        var trail = new GameObject("Worn trail embedded in terrain"); trail.transform.SetParent(pathsRoot, false);
        trail.AddComponent<MeshFilter>().sharedMesh = mesh; trail.AddComponent<MeshRenderer>().sharedMaterial = wornDirt;
        // Actual ground below provides collision; this thin overlay only adds wear.
    }
    void StationFoundations(Vector3 deck, Vector3 forward, Vector3 right)
    {
        for (int side = -1; side <= 1; side += 2) for (int front = -1; front <= 1; front += 2)
        {
            var p = deck + right * side * 2.1f + forward * front * 1.6f;
            float ground = GroundSurface(p.x, p.z);
            if (ground > deck.y - .5f) continue;
            Cube("Footing anchored into native bedrock", new Vector3(p.x, ground + .07f, p.z), new Vector3(1, .25f, 1), darkSteel, baseRoot);
            Beam("Bedrock foundation leg", new Vector3(p.x, ground, p.z), p - Vector3.up * .35f, .35f, .35f, steel, baseRoot);
        }
        // Rear service ramp lands on the existing ridge, rather than a floating pad.
        Vector3 start = deck - forward * 3.2f, end = deck - forward * 7;
        end.y = GroundSurface(end.x, end.z) + .12f;
        Beam("Ridge service ramp", start, end, 1.5f, .16f, steel, baseRoot);
    }
    void NaturalArch(Vector3 centre)
    {
        const int rings = 23, sides = 10;
        var vertices = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
        for (int i = 0; i < rings; i++)
        {
            float angle = i / (float)(rings - 1) * Mathf.PI;
            var p = centre + new Vector3(Mathf.Cos(angle) * 7.5f, Mathf.Sin(angle) * 9, 0);
            var radial = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0);
            for (int j = 0; j <= sides; j++)
            {
                float a = j / (float)sides * Mathf.PI * 2;
                float radius = 1.8f + Mathf.PerlinNoise(i * .35f, (j % sides) * .6f) * .6f;
                vertices.Add(p + radial * (Mathf.Cos(a) * radius) + Vector3.forward * (Mathf.Sin(a) * radius));
                uv.Add(new Vector2(j / (float)sides * 2, i / 3f));
                if (i < rings - 1 && j < sides) { int n = i * (sides + 1) + j; triangles.AddRange(new[] { n, n + sides + 1, n + 1, n + 1, n + sides + 1, n + sides + 2 }); }
            }
        }
        var mesh = new Mesh { name = "Eroded sandstone arch" }; mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateNormals(); mesh.RecalculateBounds(); ownedMeshes.Add(mesh);
        var go = new GameObject("Natural eastern rock arch"); go.transform.SetParent(terrainRoot, false); go.AddComponent<MeshFilter>().sharedMesh = mesh; go.AddComponent<MeshRenderer>().sharedMaterial = rock; go.AddComponent<MeshCollider>().sharedMesh = mesh;
    }
    void GroundedDetails()
    {
        for (int i = 0; i < 180; i++)
        {
            float x = Range(-58, 58), z = Range(-32, 86);
            OutpostTerrainShape.NearestRoad(x, z, out float distance, out _, out _);
            if (distance < 3.8f) continue;
            float height = OutpostTerrainShape.Height(x, z);
            if (height < OutpostTerrainShape.RiverLevel + .6f || NearStation(x, z, 6)) continue;
            float rise = Range(.3f, 1.6f), radius = Range(.5f, 1.8f);
            Rock(new Vector3(x, height - rise * .2f, z), radius, rise, radius * Range(.7f, 1.4f), 500 + i);
            if (i % 4 == 0) Scrub(new Vector3(x + radius, height, z));
        }
        // Small crystal colonies grow out of the banks instead of floating beside markers.
        for (int r = 0; r < OutpostMapLayout.Routes.Length; r++)
        {
            var spawn = OutpostMapLayout.Routes[r][0];
            for (int i = 0; i < 5; i++)
            {
                float x = spawn.x + (i % 2 == 0 ? -1 : 1) * Range(4, 7), z = spawn.z + Range(-3, 3);
                float y = OutpostTerrainShape.Height(x, z);
                if (y < OutpostTerrainShape.RiverLevel + .5f) continue;
                var shard = Shape("Bank crystal growth", PrimitiveType.Cylinder, new Vector3(x, y + .5f, z), new Vector3(.22f, Range(.4f, .9f), .22f), crystal, decorRoot, false);
                shard.transform.rotation = Quaternion.Euler(Range(-15, 15), 0, Range(-18, 18));
            }
        }
        BankWaterfall(-18, 9, 2.4f); BankWaterfall(12, 34, 2.2f); BankWaterfall(7, -5, 1.8f);
    }
    bool NearStation(float x, float z, float radius)
    {
        foreach (var p in OutpostMapLayout.StationCenters) if (Vector2.Distance(new Vector2(x, z), new Vector2(p.x, p.z)) < radius) return true;
        return false;
    }
    void Scrub(Vector3 p)
    {
        // Thin mineralised branches, not tall cover in the firing lanes.
        p.y = GroundSurface(p.x, p.z);
        for (int n = 0; n < 3; n++)
        {
            var branch = Shape("Desert scrub", PrimitiveType.Capsule, p + Vector3.up * .23f,
                new Vector3(.045f, .25f, .045f), rockDark, decorRoot, false);
            branch.transform.rotation = Quaternion.Euler(Range(-35, 35), Range(0, 180), Range(-35, 35));
        }
    }
    void BankWaterfall(float x, float z, float width)
    {
        // Locate a real channel bank; the ribbon starts on terrain and ends in water.
        for (float dx = -5; dx <= 5; dx += .5f)
        {
            float top = OutpostTerrainShape.Height(x + dx, z);
            float lower = OutpostTerrainShape.Height(x + dx, z + 1.5f);
            if (top < -7 || lower > top - 2) continue;
            Waterfall(new Vector3(x + dx, top, z), new Vector3(x + dx, OutpostTerrainShape.RiverLevel + .12f, z + 1), width);
            break;
        }
    }
}
