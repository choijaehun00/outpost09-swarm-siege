using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class OutpostCanyonTools
{
    const string PreviewPath = "Assets/Outpost09/Scenes/CanyonPreview.unity";
    [MenuItem("Tools/OUTPOST 09/Capture Current Canyon Views")]
    public static void CaptureCurrent()
    {
        if(UnityEngine.Object.FindAnyObjectByType<OutpostCanyonWorld>()==null) {
            Debug.LogWarning("Open a canyon preview or run the game first.");return;
        }
        string dir=Path.GetFullPath("OutpostReview");Directory.CreateDirectory(dir);
        var camera=new GameObject("Temporary canyon review camera").AddComponent<Camera>();
        try {
            OutpostCanyonWorld.ConfigureCamera(camera);
            camera.transform.position=new Vector3(86,92,128);camera.transform.LookAt(new Vector3(0,5,22));camera.fieldOfView=52;
            Capture(camera,dir+"/NaturalOverview.png");
            for(int i=0;i<3;i++) {
                camera.transform.position=OutpostMapLayout.Stations[i];camera.transform.LookAt(OutpostMapLayout.AimPoints[i]);camera.fieldOfView=70;
                Capture(camera,dir+"/NaturalStation"+(char)('A'+i)+".png");
            }
            int samples=0;
            for(int r=0;r<OutpostMapLayout.Routes.Length;r++) {
                var route=OutpostMapLayout.Routes[r];
                for(int n=0;n<route.Length-1;n++) {
                    if(OutpostTerrainShape.IsBridge(r,n))continue;
                    for(int step=1;step<10;step++) {
                        var p=Vector3.Lerp(route[n],route[n+1],step/10f);
                        if(Mathf.Abs(OutpostTerrainShape.Height(p.x,p.z)-(p.y-.045f))>.25f)
                            throw new Exception("Terrain buries a road at "+p);
                        samples++;
                    }
                }
            }
            File.WriteAllText(dir+"/NaturalValidation.txt","PASS: "+samples+" road surface samples; four camera views captured from the existing scene. No scene replaced.");
            Debug.Log("OUTPOST09_NATURAL_MAP_PASS: "+samples+" road samples; views: "+dir);
        }
        finally { UnityEngine.Object.DestroyImmediate(camera.gameObject); }
    }
    [MenuItem("Tools/OUTPOST 09/Open Canyon Preview")]
    public static void OpenPreview()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        BuildPreview();
        SceneView.lastActiveSceneView?.Frame(new Bounds(new Vector3(0,4,22),new Vector3(105,50,120)),false);
    }
    static Camera BuildPreview()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("OUTPOST 09 / Canyon World").AddComponent<OutpostCanyonWorld>().Build();
        var camera = new GameObject("Canyon overview camera").AddComponent<Camera>();
        camera.tag="MainCamera"; camera.transform.position=new Vector3(86,78,128);
        camera.transform.LookAt(new Vector3(0,0,22));camera.fieldOfView=52;
        OutpostCanyonWorld.ConfigureCamera(camera);
        return camera;
    }
    [MenuItem("Tools/OUTPOST 09/Generate Editable Canyon Scene")]
    public static void GenerateScene()
    {
        if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        BuildPreview();PersistPreview();
    }
    static void PersistPreview()
    {
        // Unique folder keeps previous art revisions recoverable.
        string folder="Assets/Outpost09/Generated/Canyon_"+DateTime.Now.ToString("yyyyMMdd_HHmmss");
        Directory.CreateDirectory(folder);Directory.CreateDirectory("Assets/Outpost09/Scenes");AssetDatabase.Refresh();
        int sequence=0;
        Action<UnityEngine.Object> save = asset => {
            if(asset==null || AssetDatabase.Contains(asset))return;
            AssetDatabase.CreateAsset(asset,folder+"/Asset_"+(sequence++).ToString("D4")+".asset");
        };
        foreach(var renderer in UnityEngine.Object.FindObjectsByType<Renderer>())foreach(var material in renderer.sharedMaterials) {
            if(material==null || AssetDatabase.Contains(material))continue;
            foreach(string property in material.GetTexturePropertyNames())save(material.GetTexture(property));save(material);
        }
        foreach(var filter in UnityEngine.Object.FindObjectsByType<MeshFilter>())save(filter.sharedMesh);
        foreach(var volume in UnityEngine.Object.FindObjectsByType<Volume>()) {
            if(volume.sharedProfile==null)continue;
            foreach(var component in volume.sharedProfile.components)save(component);save(volume.sharedProfile);
        }
        save(RenderSettings.skybox);AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),PreviewPath);
        Debug.Log("OUTPOST09: Editable canyon scene saved to "+PreviewPath);
    }
    // CLI validation: compile, construct the real map and render four reproducible views.
    public static void ValidateAndCapture()
    {
        string dir=Path.GetFullPath("OutpostReview");Directory.CreateDirectory(dir);
        var camera=BuildPreview();
        try {
            foreach(var route in OutpostMapLayout.Routes) {
                if(route.Length<2)throw new Exception("Route missing waypoints");
                if(Vector3.Distance(route[route.Length-1],OutpostMapLayout.Gate)>5)throw new Exception("Route misses gate");
            }
            Capture(camera,dir+"/CanyonOverview.png");
            for(int i=0;i<3;i++) {
                camera.transform.position=OutpostMapLayout.Stations[i];camera.transform.LookAt(OutpostMapLayout.AimPoints[i]);camera.fieldOfView=70;
                Capture(camera,dir+"/Station"+(char)('A'+i)+".png");
            }
            File.WriteAllText(dir+"/validation.txt","PASS: constructed canyon geometry; five routes terminate at gate; overview + station views rendered. Combat play-through not tested.");
            Debug.Log("OUTPOST09_CANYON_VALIDATION_PASS");
        }
        catch(Exception e) {Debug.LogException(e);throw;}
    }
    static void Capture(Camera camera,string path)
    {
        var target=new RenderTexture(1600,1000,24,RenderTextureFormat.ARGB32);
        camera.targetTexture=target;camera.Render();
        var previous=RenderTexture.active;RenderTexture.active=target;
        var image=new Texture2D(1600,1000,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
        camera.targetTexture=null;RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);target.Release();UnityEngine.Object.DestroyImmediate(target);
    }
}
