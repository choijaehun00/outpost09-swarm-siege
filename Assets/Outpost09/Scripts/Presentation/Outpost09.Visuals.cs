using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Owner: Presentation team. Shared state is declared in Assets/Outpost09.cs.
public partial class Outpost09
{
    Material Mat(Color c)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        var m = new Material(shader); m.color = c;
        if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", .42f);
        if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", c.b > c.r ? .65f : .08f);
        if (c.g > .8f && c.b > .8f)
        {
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * 2.5f);
        }
        return m;
    }

    GameObject Box(string name, Vector3 pos, Vector3 scale, Color color)
    {
        var o = GameObject.CreatePrimitive(PrimitiveType.Cube); o.name = name; o.transform.position = pos; o.transform.localScale = scale;
        o.GetComponent<Renderer>().material = Mat(color); return o;
    }

    void BuildWorld()
    {
        // Hide only the legacy sample objects at runtime; the source scene is preserved.
        foreach (var root in gameObject.scene.GetRootGameObjects())
            if (root.name == "Cube" || root.name == "Directional Light" || root.name == "Global Volume") root.SetActive(false);
        if (FindAnyObjectByType<OutpostCanyonWorld>() == null)
            new GameObject("OUTPOST 09 / Canyon World").AddComponent<OutpostCanyonWorld>().Build();
        OutpostCanyonWorld.ConfigureCamera(cam);
        for (int i = 0; i < posts.Length; i++) posts[i] = OutpostMapLayout.Stations[i];
        weaponModel = new GameObject("Weapon assembly").transform;
        weaponModel.SetParent(cam.transform, false);
        weaponModel.localPosition = new Vector3(.28f, -.26f, .52f);
        RebuildWeapon();
    }

    GameObject Part(Transform parent, string name, PrimitiveType shape, Vector3 position, Vector3 scale, Color color, Vector3 rotation = default)
    {
        var p = GameObject.CreatePrimitive(shape); p.name = name;
        p.transform.SetParent(parent, false); p.transform.localPosition = position;
        p.transform.localScale = scale; p.transform.localRotation = Quaternion.Euler(rotation);
        p.GetComponent<Renderer>().material = Mat(color); Destroy(p.GetComponent<Collider>()); return p;
    }

    void RebuildWeapon()
    {
        displayedWeapon = weapon;
        foreach (Transform child in weaponModel) Destroy(child.gameObject);
        Color steel = new Color(.19f, .21f, .23f), polymer = new Color(.055f, .065f, .075f);
        float length = weapon == 0 ? .28f : .57f;
        Part(weaponModel, "Machined receiver", PrimitiveType.Cube, Vector3.zero, new Vector3(.13f, .13f, length), steel);
        Part(weaponModel, "Textured pistol grip", PrimitiveType.Cube, new Vector3(0, -.13f, -.06f), new Vector3(.10f, .23f, .12f), polymer, new Vector3(-16, 0, 0));
        Part(weaponModel, "Barrel", PrimitiveType.Cylinder, new Vector3(0, .015f, length * .65f), new Vector3(.045f, .16f, .045f), steel, new Vector3(90, 0, 0));
        Part(weaponModel, "Dark muzzle bore", PrimitiveType.Cylinder, new Vector3(0, .015f, length * .65f + .161f), new Vector3(.026f, .002f, .026f), Color.black, new Vector3(90, 0, 0));
        Part(weaponModel, "Rear sight", PrimitiveType.Cube, new Vector3(0, .09f, -.1f), new Vector3(.08f, .045f, .025f), polymer);
        Part(weaponModel, "Front sight", PrimitiveType.Cube, new Vector3(0, .09f, length * .45f), new Vector3(.02f, .035f, .025f), polymer);
        for (int i = 0; i < 7; i++) Part(weaponModel, "Slide serration", PrimitiveType.Cube, new Vector3(.067f, 0, -.10f + i * .018f), new Vector3(.008f, .085f, .008f), polymer);
        if (weapon > 0)
        {
            Part(weaponModel, "Magazine", PrimitiveType.Cube, new Vector3(0, -.14f, .1f), new Vector3(.09f, .25f, .14f), steel, new Vector3(-8, 0, 0));
            Part(weaponModel, "Shoulder stock", PrimitiveType.Cube, new Vector3(0, -.025f, -.36f), new Vector3(.12f, .19f, .24f), polymer);
            Part(weaponModel, "Handguard", PrimitiveType.Cube, new Vector3(0, -.015f, .30f), new Vector3(.15f, .15f, .22f), polymer);
            for (int i = 0; i < 5; i++) Part(weaponModel, "Cooling vent", PrimitiveType.Cube, new Vector3(.078f, .015f, .22f + i * .035f), new Vector3(.006f, .045f, .022f), steel);
            Part(weaponModel, "Optic housing", PrimitiveType.Cylinder, new Vector3(0, .15f, -.02f), new Vector3(.09f, .09f, .09f), polymer, new Vector3(90, 0, 0));
        }
        if (weapon == 2) for (int i = 0; i < 3; i++) Part(weaponModel, "Energy capacitor", PrimitiveType.Cube, new Vector3(.08f, .02f, .08f + i * .07f), new Vector3(.015f, .07f, .04f), Color.cyan);
    }

    IEnumerator ShotEffect(Vector3 origin, Vector3 direction)
    {
        var o = GameObject.CreatePrimitive(PrimitiveType.Sphere); o.name = "Muzzle flash";
        o.transform.position = origin; o.transform.localScale = Vector3.one * (weapon == 2 ? .35f : .20f);
        o.GetComponent<Renderer>().material = Mat(weapon == 2 ? Color.cyan : new Color(1, .55f, .08f));
        Destroy(o.GetComponent<Collider>());
        yield return new WaitForSeconds(.08f); Destroy(o);
    }
}
