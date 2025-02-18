using Leopotam.EcsProto;
using Leopotam.EcsProto.QoL;
using Mk.Routines;
using Mk.Scopes;

class SysGameFlow : AsyncSystem<SysGameFlow> {
    [DI ()] GameAspect _gameAspect = default;
    [DI ()] SceneContext _sceneContext = default;

    protected override IProtoIt GetProtoIt () => new ProtoIt (It.Inc<CGame> ());

    protected override async Routine Run (ProtoEntity entity) {
        _sceneContext.MenuRoot.SetActive (true);
        while (true) {
            using var roundScope = new Scope ();

            await _sceneContext.StartBtn.WaitForClick ();
            // game started
            _sceneContext.MenuRoot.SetActive (roundScope, false);
            _sceneContext.GameRoot.SetActive (roundScope, true);

            _gameAspect.CUnit.NewEntity () = new () {
                Health = 100
            };

            using (var unitAliveScope = roundScope.NestedScope ()) {
                _sceneContext.ShootBtn.gameObject.SetActive (unitAliveScope, true);
                _sceneContext.ShootBtn.onClick.AddListener (unitAliveScope, Shoot);

                await Routine.When (() => _gameAspect.UnitIt.Len () == 0);
                // game over
            }

            _sceneContext.GameOverRoot.SetActive (roundScope, true);
            await _sceneContext.GameOverBtn.WaitForClick ();
        }
    }

    void Shoot () {
        foreach (var unitE in _gameAspect.UnitIt) {
            _gameAspect.CUnit.Get (unitE).Health -= 10;
        }
    }
}