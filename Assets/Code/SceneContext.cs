using UnityEngine;
using UnityEngine.UI;

class SceneContext : MonoBehaviour {
    public GameObject MenuRoot;
    public Button StartBtn;

    public GameObject GameRoot;
    public UnitView UnitPrefab;
    public Transform UnitSpawn;
    public Button ShootBtn;

    public GameObject GameOverRoot;
    public Button GameOverBtn;
}