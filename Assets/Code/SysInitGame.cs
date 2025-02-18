using Leopotam.EcsProto;
using Leopotam.EcsProto.QoL;

class SysInitGame : IProtoInitSystem {
    [DI ()] GameAspect _gameAspect = default;

    public void Init (IProtoSystems systems) {
        _gameAspect.CGame.NewEntity ();
    }
}