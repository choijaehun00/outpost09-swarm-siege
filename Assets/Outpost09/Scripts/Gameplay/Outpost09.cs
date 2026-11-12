using UnityEngine;

public partial class Outpost09 : MonoBehaviour
{
    static Outpost09 game;

    [SerializeField, HideInInspector] OutpostCombat combat;
    internal OutpostCombat Combat => combat;
    public bool Playing => mode == "playing";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() { if (FindAnyObjectByType<Outpost09>() == null) new GameObject("OUTPOST 09 Game").AddComponent<Outpost09>(); }

    void Awake()
    {
        if (game != null && game != this) { Destroy(gameObject); return; }
        game = this;
        var old = Camera.main;
        if (old != null) old.gameObject.SetActive(false);
        // Keep the sample scene intact while hiding its placeholder objects in Play.
        foreach (var root in gameObject.scene.GetRootGameObjects())
            if (root.name == "Cube" || root.name == "Directional Light" || root.name == "Global Volume")
                root.SetActive(false);

        var cam = new GameObject("Outpost Camera").AddComponent<Camera>();
        cam.tag = "MainCamera";
        var map = OutpostCanyonWorld.Prepare(cam);
        combat = gameObject.AddComponent<OutpostCombat>();
        combat.Initialize(cam, map);
        gameObject.AddComponent<OutpostGameUI>().Initialize(this);
    }
}
