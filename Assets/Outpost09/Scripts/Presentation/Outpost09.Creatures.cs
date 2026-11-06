using UnityEngine;

// Presentation owner: enemy appearance can now be edited without touching Spawn/AI.
public partial class Outpost09
{
    void BuildAlienVisual(GameObject o, int kind)
    {
        for (int leg = -1; leg <= 1; leg += 2)
        {
            var limb = Box("Clawed limb", Vector3.zero, new Vector3(.15f, .75f, .2f), new Color(.12f, .1f, .18f));
            limb.transform.SetParent(o.transform, false); limb.transform.localPosition = new Vector3(leg * .65f, -.55f, 0);
            limb.transform.localRotation = Quaternion.Euler(0, 0, leg * 30);
            Destroy(limb.GetComponent<Collider>());
        }
        var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere); eye.name = "Glow"; eye.transform.SetParent(o.transform); eye.transform.localPosition = new Vector3(0, .25f, -.48f); eye.transform.localScale = Vector3.one * .28f; eye.GetComponent<Renderer>().material = Mat(Color.cyan);
        Destroy(eye.GetComponent<Collider>());
        o.GetComponent<Renderer>().material = Mat(kind == 3 ? new Color(.20f, .14f, .12f) : new Color(.19f, .24f, .12f));
        var shell = new Color(.12f, .16f, .10f);
        for (int segment = 0; segment < 4; segment++)
            Part(o.transform, "Overlapping carapace", PrimitiveType.Sphere, new Vector3(0, .25f - segment * .22f, .06f), new Vector3(1.08f, .4f, .9f), shell);
        Part(o.transform, "Armored head", PrimitiveType.Sphere, new Vector3(0, .65f, -.25f), new Vector3(.75f, .55f, .75f), shell);
        for (int side = -1; side <= 1; side += 2)
        {
            Part(o.transform, "Mandible", PrimitiveType.Capsule, new Vector3(side * .23f, .35f, -.62f), new Vector3(.1f, .28f, .1f), new Color(.48f, .43f, .29f), new Vector3(65, side * 25, 0));
            for (int joint = 0; joint < 3; joint++)
            {
                var hip = new Vector3(side * .43f, -.05f, -.3f + joint * .3f);
                Part(o.transform, "Leg upper joint", PrimitiveType.Capsule, hip + new Vector3(side * .26f, -.1f, 0), new Vector3(.13f, .38f, .13f), shell, new Vector3(0, 0, side * 60));
                Part(o.transform, "Leg lower claw", PrimitiveType.Capsule, hip + new Vector3(side * .55f, -.55f, .08f), new Vector3(.085f, .48f, .085f), shell, new Vector3(15, 0, side * 15));
            }
        }
    }
}
