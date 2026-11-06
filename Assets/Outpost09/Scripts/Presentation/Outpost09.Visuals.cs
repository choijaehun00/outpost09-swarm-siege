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
        cam.backgroundColor = new Color(.34f, .16f, .17f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(.39f, .20f, .20f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 30;
        RenderSettings.fogEndDistance = 115;
        Box("Red desert", new Vector3(0, -1, 14), new Vector3(100, 1, 110), new Color(.47f, .19f, .13f));
        Box("MAIN GATE", new Vector3(0, 2, -15), new Vector3(10, 5, 1), new Color(.14f, .27f, .32f));
        Box("Gate illuminated lintel", new Vector3(0, 4.9f, -14.3f), new Vector3(11, .3f, .4f), Color.cyan);
        for (int side = -1; side <= 1; side += 2)
        {
            Box("Gate pylon", new Vector3(side * 6, 2.5f, -14), new Vector3(1.5f, 6, 2), new Color(.12f, .22f, .27f));
            Box("Pylon light", new Vector3(side * 6, 4.6f, -12.9f), new Vector3(.6f, .7f, .15f), Color.cyan);
        }
        for (int i = 0; i < 3; i++)
        {
            Box("Defense station " + names[i], posts[i] + Vector3.down * 1.8f, new Vector3(5, .5f, 5), new Color(.15f, .2f, .25f));
            Box("Station shield", posts[i] + new Vector3(0, -.9f, 2.1f), new Vector3(4.8f, 1.1f, .3f), new Color(.2f, .33f, .37f));
            for (int side = -1; side <= 1; side += 2)
            {
                float supportHeight = posts[i].y - 1.5f;
                Box("Tower support", new Vector3(posts[i].x + side * 2, supportHeight / 2 - .5f, posts[i].z), new Vector3(.65f, supportHeight, .65f), new Color(.10f, .17f, .20f));
                Box("Guard rail", posts[i] + new Vector3(side * 2.2f, -.9f, 0), new Vector3(.18f, 1.1f, 4.3f), new Color(.17f, .27f, .31f));
            }
            Box("Tower cyan trim", posts[i] + new Vector3(0, -1.35f, 2.3f), new Vector3(4.8f, .12f, .15f), Color.cyan);
        }
        for (int i = 0; i < 14; i++)
        {
            float x = (i % 7 - 3) * 7f, z = 7 + i / 7 * 18;
            Box("Rock cover", new Vector3(x, 0, z), new Vector3(2 + i % 3, 2, 2), new Color(.3f, .18f, .16f));
        }
        Box("Left barrier", new Vector3(-16, 1, 4), new Vector3(2, 3, 14), new Color(.18f, .25f, .28f));
        Box("Right barrier", new Vector3(16, 1, 4), new Vector3(2, 3, 14), new Color(.18f, .25f, .28f));
        for (int i = 0; i < 12; i++)
        {
            float x = (i % 2 == 0 ? -1 : 1) * (23 + i % 3 * 3);
            float z = 3 + i * 3.5f;
            var spire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spire.name = "Alien hive spire"; spire.transform.position = new Vector3(x, 2, z);
            spire.transform.localScale = new Vector3(1.1f, 2 + i % 3, 1.1f);
            spire.GetComponent<Renderer>().material = Mat(new Color(.22f, .12f, .28f));
            Box("Hive glow", new Vector3(x, 2.2f, z - 1), new Vector3(.5f, 1.5f, .12f), new Color(.75f, .15f, .65f));
        }
        QualitySettings.shadowDistance = 100;
        QualitySettings.shadows = ShadowQuality.All;
        var light = new GameObject("Sun").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.2f; light.transform.rotation = Quaternion.Euler(40, -30, 0);
        light.shadows = LightShadows.Soft;
        light.shadowStrength = .8f;
        light.color = new Color(1f, .79f, .65f);
        for (int i = 0; i < posts.Length; i++)
        {
            var lamp = new GameObject("Station floodlight").AddComponent<Light>();
            lamp.type = LightType.Spot; lamp.color = new Color(.45f, .8f, 1f);
            lamp.intensity = 8; lamp.range = 38; lamp.spotAngle = 85;
            lamp.transform.position = posts[i] + Vector3.up * 2;
            lamp.transform.rotation = Quaternion.Euler(35, 0, 0);
        }
        var gun = Box("Visible weapon", new Vector3(.3f, -.28f, .8f), new Vector3(.17f, .18f, .8f), new Color(.12f, .16f, .2f));
        gun.transform.SetParent(cam.transform, false);
        gun.transform.localPosition = new Vector3(.3f, -.28f, .8f);
        Destroy(gun.GetComponent<Collider>());
        Destroy(gun);
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
