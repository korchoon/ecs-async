using AsyncSystem.Utils;
using Leopotam.EcsProto;
using Leopotam.EcsProto.QoL;
using UnityEngine;

class Startup : MonoBehaviour {
    [SerializeField] SceneContext _sceneContext;
    ProtoSystems _systems;

    void InitModules (ProtoModules modules) {
        modules.AddModule (new AutoInjectModule (injectToServices: true));
        modules.AddAspect (new GameAspect ());
        modules.AddSystem (new SysInitGame ());
        modules.AddSystem (new SysGameFlow ());
        modules.AddSystem (new SysUnit ());
    }

    void InitServices (IProtoSystems systems) {
        systems.AddService (_sceneContext);
    }

    void Awake () {
        var modules = new ProtoModules ();
        InitModules (modules);
        var world = new ProtoWorld (modules.BuildAspect ());
        _systems = new ProtoSystems (world);
        _systems.AddModule (modules.BuildModule ());
        InitServices (_systems);
        _systems.Init ();
    }

    void Update () {
        _systems.Run ();
    }
}