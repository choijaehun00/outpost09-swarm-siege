using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public partial class Outpost09 : MonoBehaviour
{
    static Outpost09 game;
    Camera cam;
    readonly List<Alien> enemies = new List<Alien>();
    readonly Vector3[] posts = { new Vector3(-12, 9, -6), new Vector3(0, 11, -9), new Vector3(12, 9, -6) };
    readonly string[] names = { "LEFT", "CENTER", "RIGHT" };
    int post = 1, stage, wave, credits, kills, grenades, weapon, gate = 500, health = 100;
    readonly int[] ammo = { 12, 60, 30 };
    readonly bool[] owned = { true, false, false };
    readonly int[,] counts = { { 5, 0, 0, 0 }, { 6, 2, 0, 0 }, { 7, 4, 0, 0 }, { 6, 4, 0, 0 }, { 7, 0, 3, 0 }, { 8, 5, 4, 0 }, { 8, 5, 3, 0 }, { 8, 6, 4, 2 }, { 10, 7, 5, 3 } };
    string mode = "menu";
    bool shopBetweenWaves;
    Transform weaponModel;
    int displayedWeapon = -1;
    float cooldown, fireAt, reloadAt, yaw, pitch, elapsed, flash;
    GUIStyle label, title, button;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() { if (FindAnyObjectByType<Outpost09>() == null) new GameObject("OUTPOST 09 Game").AddComponent<Outpost09>(); }

    void Awake()
    {
        if (game != null && game != this) { Destroy(gameObject); return; }
        game = this;
        var old = Camera.main;
        if (old != null) old.gameObject.SetActive(false);
        cam = new GameObject("Outpost Camera").AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.nearClipPlane = .04f;
        cam.fieldOfView = 70;
        RenderSettings.ambientLight = new Color(.55f, .38f, .35f);
        BuildWorld();
        MovePost(1, true);
    }

    public Vector3 Target => posts[post];
    public bool Playing => mode == "playing";

}
