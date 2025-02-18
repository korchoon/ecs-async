using Leopotam.EcsProto;
using Leopotam.EcsProto.QoL;

struct CGame { }

struct CUnit {
    public int Health;
}

class GameAspect : ProtoAspectInject {
    public ProtoPool<CUnit> CUnit = default;
    public ProtoPool<CGame> CGame = default;

    public ProtoIt UnitIt = new (It.Inc<CUnit> ());
}