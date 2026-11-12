using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public partial class OutpostCanyonWorld : OutpostBattlefield
{
    public const string Version = "Canyon / 03 - confluence frontier";
    readonly List<Material> ownedMaterials = new List<Material>();
    readonly List<Mesh> ownedMeshes = new List<Mesh>();
    readonly List<Texture2D> ownedTextures = new List<Texture2D>();
    Material rock, rockDark, sand, steel, darkSteel, amber, teal, red, crystal, water, foam;
    System.Random random;
    Transform terrainRoot, baseRoot, pathsRoot, decorRoot;
    bool built;

    public static OutpostBattlefield Prepare(Camera camera)
    {
        RenderSettings.ambientLight = new Color(.55f, .38f, .35f);
        var world = FindAnyObjectByType<OutpostCanyonWorld>();
        if (world == null)
        {
            world = new GameObject("OUTPOST 09 / Canyon World").AddComponent<OutpostCanyonWorld>();
            world.Build();
        }
        ConfigureCamera(camera);
        return world;
    }

    public override Vector3 GatePosition => OutpostMapLayout.Gate;
    public override int RouteCount => OutpostMapLayout.RouteCount;

    public override bool TryGetStation(int index, out Vector3 position, out Vector3 aimPoint)
    {
        position = aimPoint = default;
        if (index < 0 || index >= OutpostMapLayout.Stations.Length) return false;
        position = OutpostMapLayout.Stations[index];
        aimPoint = OutpostMapLayout.AimPoints[index];
        return true;
    }

    public override bool TryGetRoute(int index, out IReadOnlyList<Vector3> points)
    {
        bool found = OutpostMapLayout.TryGetRoute(index, out var route);
        points = route;
        return found;
    }

    public override bool TryFindNearestRoute(Vector3 position, out int routeIndex, out int nextPoint) =>
        OutpostMapLayout.TryFindNearestRoute(position, out routeIndex, out nextPoint);

    float Range(float a, float b) => a + (float)random.NextDouble() * (b - a);
    Material Surface(string name, Color color, float metallic = 0, float smoothness = .2f, float glow = 0)
    {
        var m = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, color = color };
        m.SetFloat("_Metallic", metallic); m.SetFloat("_Smoothness", smoothness);
        if (glow > 0) { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", color * glow); }
        ownedMaterials.Add(m); return m;
    }
    Transform Group(string name) { var t = new GameObject(name).transform; t.SetParent(transform, false); return t; }
    GameObject Shape(string name, PrimitiveType shape, Vector3 p, Vector3 s, Material m, Transform parent, bool solid = true)
    {
        var o = GameObject.CreatePrimitive(shape); o.name = name; o.transform.SetParent(parent, false);
        o.transform.position = p; o.transform.localScale = s; o.GetComponent<Renderer>().sharedMaterial = m;
        if (!solid) { var c = o.GetComponent<Collider>(); if (Application.isPlaying) Destroy(c); else DestroyImmediate(c); }
        return o;
    }
    GameObject Cube(string name, Vector3 p, Vector3 s, Material m, Transform parent, bool solid = true) => Shape(name, PrimitiveType.Cube, p, s, m, parent, solid);
    void Beam(string name, Vector3 a, Vector3 b, float width, float depth, Material material, Transform parent, bool solid = true)
    {
        var o = Cube(name, (a + b) / 2, new Vector3(width, depth, Vector3.Distance(a, b)), material, parent, solid);
        o.transform.rotation = Quaternion.LookRotation(b - a);
    }
    public void Build()
    {
        if (built) return; built = true; random = new System.Random(9009);
        terrainRoot = Group("01 / Sandstone escarpments"); pathsRoot = Group("02 / Five invasion routes");
        baseRoot = Group("03 / Outposts and final gate"); decorRoot = Group("04 / Water, crystals and props");
        Materials(); Environment(); Landforms(); Roads(); Fortress();
        for (int i = 0; i < 3; i++) Station(i);
        Details();
    }
    void Materials()
    {
        rock = Surface("Layered red sandstone", new Color(.56f,.26f,.17f), 0, .12f);
        rockDark = Surface("Iron-rich strata", new Color(.32f,.145f,.11f), 0, .16f);
        sand = Surface("Wind-scoured ochre", new Color(.53f,.34f,.23f), 0, .12f);
        steel = Surface("Weathered blue gunmetal", new Color(.20f,.25f,.27f), .72f, .38f);
        darkSteel = Surface("Recessed carbon panels", new Color(.07f,.10f,.12f), .45f, .27f);
        amber = Surface("Amber safety lighting", new Color(1,.32f,.045f), .2f, .4f, 4);
        teal = Surface("Cyan station identifiers", new Color(.13f,.8f,1), .25f, .5f, 3);
        red = Surface("Hostile spawn beacon", new Color(1,.035f,.07f), .1f, .4f, 3);
        crystal = Surface("Alien amethyst", new Color(.42f,.055f,.23f), .32f, .7f, .6f);
        // A Resources material retains the custom shader in standalone builds.
        var flowTemplate = Resources.Load<Material>("OutpostCanyonFlow");
        var flowShader = flowTemplate != null ? flowTemplate.shader : Shader.Find("Outpost09/Canyon Flow");
        water = new Material(flowShader) { name = "Flowing canyon water" }; water.SetColor("_BaseColor",new Color(.08f,.27f,.31f,.84f)); water.SetFloat("_Speed", .12f); ownedMaterials.Add(water);
        foam = new Material(flowShader) { name = "Waterfall ribbons" }; foam.SetColor("_BaseColor", new Color(.49f,.72f,.76f,.64f)); foam.SetFloat("_Speed", .7f); ownedMaterials.Add(foam);
        rock.SetTexture("_BaseMap", Texture(false)); rockDark.SetTexture("_BaseMap", rock.GetTexture("_BaseMap"));
        sand.SetTexture("_BaseMap", Texture(true));
        var normal = NormalTexture();
        foreach (var m in new [] { rock, rockDark, sand }) { m.EnableKeyword("_NORMALMAP"); m.SetTexture("_BumpMap", normal); m.SetFloat("_BumpScale", .55f); }
    }
    Texture2D Texture(bool ground)
    {
        const int n = 256; var tex = new Texture2D(n,n,TextureFormat.RGBA32,true) { name = ground ? "Sand grain" : "Sedimentary grain", wrapMode = TextureWrapMode.Repeat, anisoLevel = 8 };
        var pixels = new Color[n*n];
        for (int y=0;y<n;y++) for(int x=0;x<n;x++) {
            float noise = Mathf.PerlinNoise(x/23f,y/27f)*.25f+Mathf.PerlinNoise(x/5f,y/5f)*.15f;
            float strata = ground ? 0 : Mathf.Sin(y*.33f + Mathf.PerlinNoise(x/65f,y/80f)*3)*.07f;
            float v = .63f+noise+strata+Range(-.055f,.055f); pixels[y*n+x]=new Color(v,v*.97f,v*.92f);
        }
        tex.SetPixels(pixels); tex.Apply(); ownedTextures.Add(tex); return tex;
    }
    Texture2D NormalTexture()
    {
        const int n=128; var tex = new Texture2D(n,n,TextureFormat.RGBA32,true,true) { name="Sandstone micro normal",wrapMode=TextureWrapMode.Repeat,anisoLevel=8 };
        var pixels=new Color[n*n];
        for(int y=0;y<n;y++) for(int x=0;x<n;x++) {
            float h=Mathf.PerlinNoise(x*.15f,y*.15f);
            Vector3 normal=new Vector3((h-Mathf.PerlinNoise((x+1)*.15f,y*.15f))*2,(h-Mathf.PerlinNoise(x*.15f,(y+1)*.15f))*2,1).normalized;
            pixels[y*n+x]=new Color(normal.x*.5f+.5f,normal.y*.5f+.5f,normal.z*.5f+.5f,normal.x*.5f+.5f);
        }
        tex.SetPixels(pixels);tex.Apply();ownedTextures.Add(tex);return tex;
    }
    void Environment()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.43f,.48f,.62f); RenderSettings.ambientEquatorColor=new Color(.35f,.28f,.28f); RenderSettings.ambientGroundColor=new Color(.13f,.10f,.12f);
        RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.006f;RenderSettings.fogColor=new Color(.43f,.34f,.39f);
        var skyShader=Shader.Find("Skybox/Procedural");
        if(skyShader!=null) {
            var sky=new Material(skyShader);sky.SetColor("_SkyTint",new Color(.55f,.48f,.61f));sky.SetColor("_GroundColor",new Color(.26f,.18f,.19f));sky.SetFloat("_AtmosphereThickness",.8f);sky.SetFloat("_Exposure",1.1f);sky.SetFloat("_SunSize",.035f);RenderSettings.skybox=sky;ownedMaterials.Add(sky);
        }
        var sun=new GameObject("Warm low sun").AddComponent<Light>();sun.transform.SetParent(transform);sun.type=LightType.Directional;sun.intensity=2.1f;sun.color=new Color(1,.76f,.57f);sun.shadows=LightShadows.Soft;sun.shadowStrength=.85f;sun.transform.rotation=Quaternion.Euler(28,-37,0);RenderSettings.sun=sun;
        var fill=new GameObject("Cool sky fill").AddComponent<Light>();fill.transform.SetParent(transform);fill.type=LightType.Directional;fill.intensity=.35f;fill.color=new Color(.45f,.62f,1);fill.transform.rotation=Quaternion.Euler(55,145,0);
        var volume=new GameObject("Canyon colour grade").AddComponent<Volume>();volume.transform.SetParent(transform);volume.isGlobal=true;volume.priority=20;
        var profile=ScriptableObject.CreateInstance<VolumeProfile>();volume.sharedProfile=profile;
        var bloom=profile.Add<Bloom>(true);bloom.intensity.Override(.35f);bloom.threshold.Override(1.15f);bloom.scatter.Override(.65f);
        var tone=profile.Add<Tonemapping>(true);tone.mode.Override(TonemappingMode.ACES);
        var color=profile.Add<ColorAdjustments>(true);color.postExposure.Override(.35f);color.contrast.Override(7);color.saturation.Override(-8);
        var vignette=profile.Add<Vignette>(true);vignette.intensity.Override(.15f);vignette.smoothness.Override(.5f);
        QualitySettings.shadowDistance=140;
        // Clone the active URP asset so project/team settings are not changed.
        var current=QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
        if(current==null) current=GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
        if(Application.isPlaying && current!=null) {
            originalPipeline=QualitySettings.renderPipeline; runtimePipeline=Instantiate(current);runtimePipeline.name="Canyon runtime quality";
            runtimePipeline.msaaSampleCount=4;runtimePipeline.shadowDistance=140;QualitySettings.renderPipeline=runtimePipeline;
        }
    }
    RenderPipelineAsset originalPipeline;
    UniversalRenderPipelineAsset runtimePipeline;
    public static void ConfigureCamera(Camera camera)
    {
        camera.clearFlags=CameraClearFlags.Skybox;camera.farClipPlane=550;camera.allowHDR=true;camera.allowMSAA=true;
        var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;
        data.antialiasing=AntialiasingMode.SubpixelMorphologicalAntiAliasing;
    }
    void Landforms()
    {
        NaturalLandforms();
    }
    void Rock(Vector3 basePoint,float rx,float height,float rz,int seed)
    {
        const int sides=13,rings=7;
        var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();
        for(int y=0;y<rings;y++) for(int x=0;x<=sides;x++) {
            int k=x%sides;float a=k/(float)sides*Mathf.PI*2;
            float taper=1-y/(float)(rings-1)*.17f;
            // Repeated angle noise at the seam, horizontal ledges along the height.
            float jag=1+Mathf.PerlinNoise(k*.77f+seed,y*.63f)*.23f;
            float ledge=y%2==0?1:.92f;
            vertices.Add(basePoint+new Vector3(Mathf.Cos(a)*rx*jag*taper*ledge,height*y/(rings-1),Mathf.Sin(a)*rz*jag*taper*ledge));
            uv.Add(new Vector2(x/(float)sides*rx*.8f,y/(float)(rings-1)*height*.24f));
            if(y<rings-1 && x<sides){int j=y*(sides+1)+x;triangles.AddRange(new[]{j,j+sides+1,j+1,j+1,j+sides+1,j+sides+2});}
        }
        int center=vertices.Count;vertices.Add(basePoint+Vector3.up*height);uv.Add(Vector2.one*.5f);
        for(int x=0;x<sides;x++)triangles.AddRange(new[]{center,(rings-1)*(sides+1)+x+1,(rings-1)*(sides+1)+x});
        var mesh=new Mesh { name="Stratified sandstone mesh" };mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();ownedMeshes.Add(mesh);
        var go=new GameObject("Sedimentary cliff");go.transform.SetParent(terrainRoot,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=seed%4==0?rockDark:rock;
        // Large terrain is a continuous collision mesh; these are grounded details.
        go.AddComponent<MeshCollider>().sharedMesh=mesh;
    }
    void Roads()
    {
        OrganicRoads();
    }
    void RoadSegment(Vector3 a,Vector3 b,bool bridge)
    {
        var d=b-a;var right=Vector3.Cross(Vector3.up,d).normalized;float length=d.magnitude;
        if(bridge) Beam("Bridge deck",a-Vector3.up*.22f,b-Vector3.up*.22f,4.6f,.44f,steel,pathsRoot);
        else DustTrail(a,b);
        if(bridge) {
            for(int side=-1;side<=1;side+=2) {
                Beam("Bridge lower girder",a+right*side*2-Vector3.up*1.6f,b+right*side*2-Vector3.up*1.6f,.35f,.5f,darkSteel,pathsRoot);
                int bays=Mathf.CeilToInt(length/3);
                for(int i=0;i<bays;i++) {
                    Vector3 p=Vector3.Lerp(a,b,i/(float)bays)+right*side*2;
                    Vector3 q=Vector3.Lerp(a,b,(i+1)/(float)bays)+right*side*2;
                    Beam("Truss diagonal",p-Vector3.up*1.5f,q,.14f,.14f,steel,pathsRoot,false);
                }
            }
        }
        int lamps=Mathf.CeilToInt(length/5);
        for(int i=0;i<=lamps;i++) for(int side=-1;side<=1;side+=2) {
            Vector3 p=Vector3.Lerp(a,b,i/(float)lamps)+right*side*2.05f;
            if(bridge) {
                Cube("Bridge rail post",p+Vector3.up*.5f,new Vector3(.10f,1,.1f),steel,pathsRoot,false);
                if(i<lamps)Beam("Bridge handrail",p+Vector3.up*.85f,Vector3.Lerp(a,b,(i+1)/(float)lamps)+right*side*2.05f+Vector3.up*.85f,.10f,.10f,steel,pathsRoot,false);
            }
            if(bridge) Cube("Bridge approach light",p+Vector3.up*.08f,new Vector3(.12f,.08f,.28f),amber,pathsRoot,false);
        }
    }
    void SpawnMarker(int index,Vector3 p)
    {
        Ring(p+Vector3.up*.06f,2.1f,.06f,red,decorRoot);
        for(int side=-1;side<=1;side+=2) {
            Cube("Spawn warning pole",p+new Vector3(side*2.7f,1.1f,0),new Vector3(.14f,2.2f,.14f),darkSteel,decorRoot);
            Shape("Red beacon",PrimitiveType.Sphere,p+new Vector3(side*2.7f,2.3f,0),Vector3.one*.2f,red,decorRoot,false);
        }
        Sign("SPAWN "+(char)('A'+index),p+new Vector3(0,2.8f,1),.25f,red,decorRoot);
    }
    void Station(int index)
    {
        Vector3 deck=OutpostMapLayout.StationCenters[index]-Vector3.up*1.65f;
        Vector3 forward=OutpostMapLayout.StationForward(index), right=Vector3.Cross(Vector3.up,forward);
        StationFoundations(deck,forward,right);
        StationDeck(index,deck,forward,right);
        Beam("Open firing balcony",deck+forward*2.1f-Vector3.up*.15f,deck+forward*5.15f-Vector3.up*.15f,2,.3f,steel,baseRoot);
        for(int side=-1;side<=1;side+=2)
            Beam("Cantilever underside brace",deck+forward*1.5f-Vector3.up*1.7f+right*side*.7f,deck+forward*4.8f-Vector3.up*.35f+right*side*.7f,.12f,.18f,darkSteel,baseRoot,false);
        Vector3 rear=deck-forward*2.3f-right*.8f;
        Cube("Outpost utility cabin",rear+Vector3.up*1.1f,new Vector3(1.8f,2.2f,1.8f),steel,baseRoot);
        Cube("Cabin roof",rear+Vector3.up*2.3f,new Vector3(2,.2f,2),darkSteel,baseRoot);
        Cube("Cabin illuminated window",rear+new Vector3(0,1.4f,.91f),new Vector3(1.2f,.6f,.05f),amber,baseRoot,false);
        for(int i=0;i<2;i++)Crate(deck-forward*1.7f+right*(1.5f+i*.7f)+Vector3.up*.3f,.55f);
        Vector3 mast=deck-forward*2.1f-right*2;
        Shape("Radio mast",PrimitiveType.Cylinder,mast+Vector3.up*3,new Vector3(.10f,3,.10f),steel,baseRoot);
        Beam("Antenna crossbar",mast+new Vector3(-1,4,0),mast+new Vector3(1,4,0),.07f,.07f,steel,baseRoot,false);
        Shape("Radio warning light",PrimitiveType.Sphere,mast+Vector3.up*6.1f,Vector3.one*.18f,red,baseRoot,false);
        Sign("OUTPOST "+(char)('A'+index),deck+forward*5.3f-Vector3.up*1.3f,.23f,amber,baseRoot);
        // Satellite dish behind A, spherical sensor dome on C.
        if(index==0) {
            var dish=Shape("Comms dish",PrimitiveType.Sphere,rear+Vector3.up*3.3f,new Vector3(2.2f,.25f,2.2f),steel,baseRoot,false);dish.transform.rotation=Quaternion.Euler(42,0,25);
            Beam("Dish feed",rear+Vector3.up*3.3f,rear+new Vector3(0,4.2f,.5f),.08f,.08f,darkSteel,baseRoot,false);
        }
        if(index==2)Shape("Sensor dome",PrimitiveType.Sphere,rear+Vector3.up*2.6f,new Vector3(2.1f,1.3f,2.1f),Surface("Dome ceramic",new Color(.6f,.66f,.69f),.2f,.45f),baseRoot);
    }
    void Fortress()
    {
        var p=OutpostMapLayout.Gate;
        Cube("Gate forecourt",p+new Vector3(0,-.4f,0),new Vector3(29,.8f,17),steel,baseRoot);
        for(int side=-1;side<=1;side+=2) {
            Cube("Fortress wall",p+new Vector3(side*16,5,-2),new Vector3(17,10,3.5f),steel,baseRoot);
            for(int i=0;i<4;i++) {
                Cube("Wall buttress",p+new Vector3(side*(9+i*4.5f),4,0),new Vector3(.65f,8.5f,1.4f),darkSteel,baseRoot);
                Cube("Wall amber strip",p+new Vector3(side*(9+i*4.5f),8.3f,.75f),new Vector3(.2f,.5f,.12f),amber,baseRoot,false);
            }
            Cube("Gate armored pillar",p+new Vector3(side*6.8f,5,0),new Vector3(2,11,4),darkSteel,baseRoot);
            Cube("Pillar light",p+new Vector3(side*6.8f,5,2.05f),new Vector3(.16f,7,.06f),amber,baseRoot,false);
            Cube("Gate door",p+new Vector3(side*2.8f,4.1f,.3f),new Vector3(5.45f,8.2f,.7f),darkSteel,baseRoot);
            for(int i=0;i<3;i++)Beam("Door reinforcement",p+new Vector3(side*.5f,1+i*2.3f,.75f),p+new Vector3(side*5,2.2f+i*2.1f,.75f),.16f,.22f,steel,baseRoot,false);
        }
        Cube("Gate lintel",p+new Vector3(0,9.5f,0),new Vector3(15,1.5f,4),steel,baseRoot);
        Cube("Gate central seam",p+new Vector3(0,4.2f,.72f),new Vector3(.10f,8.1f,.06f),amber,baseRoot,false);
        Sign("09  /  FINAL GATE",p+new Vector3(0,10,.0f)+Vector3.forward*2.1f,.5f,amber,baseRoot);
        for(int i=0;i<4;i++)Crate(p+new Vector3(-10+i*1.3f,.6f,4),1.1f);
    }
    void Crate(Vector3 p,float size)
    {
        Cube("Supply case",p,Vector3.one*size,darkSteel,baseRoot);
        for(int side=-1;side<=1;side+=2)Cube("Case corner band",p+new Vector3(side*size*.4f,0,0),new Vector3(size*.08f,size*1.02f,size*1.02f),steel,baseRoot,false);
        Cube("Case latch",p+new Vector3(0,0,size*.51f),new Vector3(size*.2f,size*.13f,.03f),amber,baseRoot,false);
    }
    void Ring(Vector3 p,float radius,float width,Material material,Transform parent)
    {
        for(int i=0;i<48;i++) {
            float a=i/48f*Mathf.PI*2,b=(i+1)/48f*Mathf.PI*2;
            Beam("Light ring",p+new Vector3(Mathf.Cos(a),0,Mathf.Sin(a))*radius,p+new Vector3(Mathf.Cos(b),0,Mathf.Sin(b))*radius,width,width,material,parent,false);
        }
    }
    void Sign(string text,Vector3 position,float size,Material accent,Transform parent)
    {
        Cube("Sign backing",position,new Vector3(text.Length*size*.57f+1,size*1.6f,.14f),darkSteel,parent,false);
        var label=new GameObject(text).AddComponent<TextMesh>();label.transform.SetParent(parent,false);label.transform.position=position+Vector3.forward*.09f;
        label.transform.rotation=Quaternion.Euler(0,180,0);label.anchor=TextAnchor.MiddleCenter;label.alignment=TextAlignment.Center;label.fontSize=64;label.characterSize=size/6;label.color=new Color(.95f,.87f,.73f);label.text=text;
        Cube("Sign underline",position+new Vector3(0,-size*.72f,.09f),new Vector3(text.Length*size*.57f+.8f,.045f,.04f),accent,parent,false);
    }
    void Details()
    {
        DistrictDetails();
    }
    void Waterfall(Vector3 top,Vector3 bottom,float width)
    {
        for(int i=0;i<4;i++) {
            var strip=Shape("Flowing waterfall",PrimitiveType.Quad,(top+bottom)/2+Vector3.right*(i-1.5f)*width*.22f,new Vector3(width*.4f,Vector3.Distance(top,bottom),1),foam,decorRoot,false);
            strip.transform.rotation=Quaternion.Euler(0,180,0);
        }
        Shape("Waterfall foam",PrimitiveType.Cylinder,bottom+Vector3.up*.1f,new Vector3(width*1.4f,.04f,width),foam,decorRoot,false);
    }
    void OnDestroy()
    {
        if(runtimePipeline!=null) {QualitySettings.renderPipeline=originalPipeline;Destroy(runtimePipeline);}
        if(!Application.isPlaying)return;
        foreach(var m in ownedMaterials)if(m!=null)Destroy(m);
        foreach(var m in ownedMeshes)if(m!=null)Destroy(m);
        foreach(var t in ownedTextures)if(t!=null)Destroy(t);
    }
}
