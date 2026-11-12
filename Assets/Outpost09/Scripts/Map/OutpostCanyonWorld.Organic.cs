using System.Collections.Generic;
using UnityEngine;

public partial class OutpostCanyonWorld
{
    Material geological, trailMaterial;
    float[,] terrainHeights;

    void OrganicMaterials()
    {
        var template=Resources.Load<Material>("OutpostGeologicalGround");
        geological=template!=null?new Material(template):new Material(Shader.Find("Outpost09/Geological Ground"));
        geological.name="World-projected eroded sandstone";geological.SetTexture("_BaseMap",rock.GetTexture("_BaseMap"));ownedMaterials.Add(geological);
        var trailTemplate=Resources.Load<Material>("OutpostTrail");
        trailMaterial=trailTemplate!=null?new Material(trailTemplate):new Material(Shader.Find("Outpost09/Soft Trail"));
        trailMaterial.SetTexture("_BaseMap",sand.GetTexture("_BaseMap"));ownedMaterials.Add(trailMaterial);
    }
    float GroundSurface(float x,float z)
    {
        if(terrainHeights==null)return OutpostTerrainShape.Height(x,z);
        float gx=Mathf.Clamp(x-OutpostTerrainShape.MinX,0,OutpostTerrainShape.CellsX-.001f);
        float gz=Mathf.Clamp(z-OutpostTerrainShape.MinZ,0,OutpostTerrainShape.CellsZ-.001f);
        int ix=Mathf.FloorToInt(gx),iz=Mathf.FloorToInt(gz);float u=gx-ix,v=gz-iz;
        float a=terrainHeights[ix,iz],b=terrainHeights[ix,iz+1],c=terrainHeights[ix+1,iz],d=terrainHeights[ix+1,iz+1];
        return u+v<=1?a+(c-a)*u+(b-a)*v:d+(b-d)*(1-u)+(c-d)*(1-v);
    }
    void RiverSurfaces()
    {
        foreach(var river in OutpostTerrainShape.Rivers)
        {
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();float travelled=0;
            for(int i=0;i<river.Length;i++)
            {
                var p=river[i];if(i>0)travelled+=Vector3.Distance(p,river[i-1]);
                var direction=river[Mathf.Min(i+1,river.Length-1)]-river[Mathf.Max(0,i-1)];direction.y=0;
                var right=Vector3.Cross(Vector3.up,direction).normalized;
                OutpostTerrainShape.RiverSurvey(p.x,p.z,out _,out _,out float width);
                vertices.Add(p-right*width*.96f+Vector3.up*.05f);vertices.Add(p+right*width*.96f+Vector3.up*.05f);
                uv.Add(new Vector2(0,travelled/9));uv.Add(new Vector2(1,travelled/9));
                if(i<river.Length-1){int n=i*2;triangles.AddRange(new[]{n,n+2,n+1,n+1,n+2,n+3});}
            }
            var mesh=new Mesh{name="Connected river ribbon"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();ownedMeshes.Add(mesh);
            var go=new GameObject("River with real banks and confluence");go.transform.SetParent(decorRoot,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=water;
        }
    }
    void OrganicRoads()
    {
        var spans=new HashSet<string>();
        for(int r=0;r<OutpostMapLayout.Routes.Length;r++)
        {
            var route=OutpostMapLayout.Routes[r];int start=0;
            while(start<route.Length-1)
            {
                bool bridge=OutpostMapLayout.IsBridge(r,start);int end=start+1;
                while(end<route.Length-1&&OutpostMapLayout.IsBridge(r,end)==bridge)end++;
                if(bridge)
                {
                    string key=route[start].ToString("F2")+route[end].ToString("F2");
                    if(spans.Add(key))RoadSegment(route[start],route[end],true);
                }
                else TrailStrip(route,start,end);
                start=end;
            }
            SpawnMarker(r,route[0]);
        }
    }
    void TrailStrip(Vector3[] points,int start,int end)
    {
        var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();float travelled=0;
        for(int i=start;i<=end;i++)
        {
            var p=points[i];if(i>start)travelled+=Vector3.Distance(p,points[i-1]);
            var direction=points[Mathf.Min(i+1,end)]-points[Mathf.Max(start,i-1)];direction.y=0;
            var right=Vector3.Cross(Vector3.up,direction).normalized;
            float halfWidth=1.45f+Mathf.PerlinNoise(p.x*.13f+7,p.z*.13f)*.5f;
            for(int side=-1;side<=1;side+=2)
            {
                var v=p+right*side*halfWidth;v.y=GroundSurface(v.x,v.z)+.045f;
                vertices.Add(v);uv.Add(new Vector2(side==-1?0:1,travelled/5));
            }
            if(i<end){int n=(i-start)*2;triangles.AddRange(new[]{n,n+2,n+1,n+1,n+2,n+3});}
        }
        var mesh=new Mesh{name="Continuous worn track with soft edges"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();ownedMeshes.Add(mesh);
        var go=new GameObject("Soft worn track on surveyed ground");go.transform.SetParent(pathsRoot,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=trailMaterial;
    }
    void StationDeck(int index,Vector3 deck,Vector3 forward,Vector3 right)
    {
        Vector2[][] silhouettes={
            new[]{new Vector2(-3.5f,-3),new Vector2(-4,-.5f),new Vector2(-2.8f,2.2f),new Vector2(2.4f,2.4f),new Vector2(3.8f,0),new Vector2(3,-3.2f)},
            new[]{new Vector2(-3,-2.2f),new Vector2(-3.8f,.2f),new Vector2(-2,2.4f),new Vector2(2.6f,2.4f),new Vector2(3.6f,-.4f),new Vector2(1.6f,-3.2f)},
            new[]{new Vector2(-3.5f,-2.6f),new Vector2(-3.5f,1),new Vector2(-1.8f,2.9f),new Vector2(2.6f,2.9f),new Vector2(3.4f,-2.5f)}
        };
        var outline=silhouettes[index];var vertices=new List<Vector3>();var triangles=new List<int>();
        for(int i=0;i<outline.Length;i++)
        {
            var p=deck+right*outline[i].x+forward*outline[i].y;
            var q=deck+right*outline[(i+1)%outline.Length].x+forward*outline[(i+1)%outline.Length].y;
            int n=vertices.Count;vertices.Add(deck);vertices.Add(p);vertices.Add(q);triangles.AddRange(new[]{n,n+1,n+2});
            n=vertices.Count;vertices.Add(p);vertices.Add(p-Vector3.up*.55f);vertices.Add(q);vertices.Add(q-Vector3.up*.55f);triangles.AddRange(new[]{n,n+1,n+2,n+2,n+1,n+3});
            if(outline[i].y>.1f||outline[(i+1)%outline.Length].y>.1f)continue;
            Cube("Rear railing stanchion",p+Vector3.up*.35f,new Vector3(.08f,.7f,.08f),steel,baseRoot,false);
            Beam("Rear railing on fitted deck",p+Vector3.up*.7f,q+Vector3.up*.7f,.075f,.075f,steel,baseRoot,false);
            Cube("Recessed deck marker",p-Vector3.up*.18f,Vector3.one*.11f,amber,baseRoot,false);
        }
        var mesh=new Mesh{name="Cliff-fitted outpost deck "+index};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();ownedMeshes.Add(mesh);
        var go=new GameObject("Asymmetric fitted outpost deck");go.transform.SetParent(baseRoot,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=steel;go.AddComponent<MeshCollider>().sharedMesh=mesh;
        // Landing marks stay behind the eye instead of filling the forward view.
        Beam("Station landing mark",deck-right*.6f+Vector3.up*.025f,deck+right*.6f+Vector3.up*.025f,.04f,.025f,teal,baseRoot,false);
    }
    void DistrictDetails()
    {
        // Habitat clusters and empty sightlines alternate, instead of evenly
        // scattering identical props over every square metre of the map.
        Vector2[] colonies={new Vector2(-48,58),new Vector2(43,34),new Vector2(-32,-2),new Vector2(44,-18)};
        for(int cluster=0;cluster<colonies.Length;cluster++)for(int i=0;i<22;i++)
        {
            var centre=colonies[cluster];float angle=Range(0,Mathf.PI*2),radius=Mathf.Sqrt(Range(0,1))*8;
            float x=centre.x+Mathf.Cos(angle)*radius,z=centre.y+Mathf.Sin(angle)*radius;
            OutpostTerrainShape.NearestRoad(x,z,out float roadDistance,out _,out _);
            OutpostTerrainShape.RiverSurvey(x,z,out float riverDistance,out _,out float riverWidth);
            if(roadDistance<3.7f||riverDistance<riverWidth+1||NearStation(x,z,7))continue;
            float h=GroundSurface(x,z),rise=Range(.3f,1.4f),width=Range(.5f,1.5f);
            Rock(new Vector3(x,h-rise*.18f,z),width,rise,width*Range(.7f,1.3f),900+cluster*30+i);
            if(i%3==0)Scrub(new Vector3(x+width,h,z));
            if(cluster==1&&i%4==0)
            {
                var shard=Shape("Eastern mineral seam",PrimitiveType.Cylinder,new Vector3(x,h+.5f,z),new Vector3(.20f,Range(.4f,.9f),.2f),crystal,decorRoot,false);
                shard.transform.rotation=Quaternion.Euler(Range(-20,20),0,Range(-15,15));
            }
        }
        // Geographical identity: industrial ridge at A, relay island at B and
        // mineral/dome facility at C. All equipment is behind firing positions.
        for(int i=0;i<3;i++)
        {
            var centre=OutpostMapLayout.StationCenters[i];var f=OutpostMapLayout.StationForward(i);var right=Vector3.Cross(Vector3.up,f);
            Vector3 p=centre-f*9+right*(i==0?-5:4);p.y=GroundSurface(p.x,p.z);
            if(i==0)
            {
                for(int panel=0;panel<3;panel++)
                {
                    var q=p+right*panel*2;q.y=GroundSurface(q.x,q.z);
                    Cube("Solar footing",q+Vector3.up*.35f,new Vector3(.2f,.7f,.2f),steel,baseRoot);
                    var solar=Cube("Ridge solar collector",q+Vector3.up*.85f,new Vector3(1.7f,.08f,1.2f),darkSteel,baseRoot);solar.transform.rotation=Quaternion.Euler(18,-25,0);
                }
            }
            else if(i==1)
            {
                Shape("Island relay base",PrimitiveType.Cylinder,p+Vector3.up*.5f,new Vector3(1.1f,.5f,1.1f),steel,baseRoot);
                Beam("Island relay array",p+Vector3.up,p+Vector3.up*5,.12f,.12f,steel,baseRoot,false);
                Shape("Island relay beacon",PrimitiveType.Sphere,p+Vector3.up*5.1f,Vector3.one*.18f,teal,baseRoot,false);
            }
            else
            {
                Shape("Mineral processing tank",PrimitiveType.Cylinder,p+Vector3.up*1.1f,new Vector3(1.3f,1.1f,1.3f),steel,baseRoot);
                Beam("Bedrock intake pipe",p+new Vector3(0,.4f,0),p+right*3+new Vector3(0,.4f,0),.22f,.22f,darkSteel,baseRoot);
            }
        }
    }
}
