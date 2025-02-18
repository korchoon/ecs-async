using Leopotam.EcsProto;
using Leopotam.EcsProto.QoL;
using Mk.Routines;
using UnityEngine;

class SysUnit : AsyncSystem<SysUnit> {
    [DI ()] GameAspect _counterAspect = default;
    [DI ()] SceneContext _scene = default;

    protected override IProtoIt GetProtoIt () => new ProtoIt (It.Inc<CUnit> ());

    protected override async Routine Run (ProtoEntity entity) {
        var scope = await Routine.GetScope ();

        var view = Object.Instantiate (_scene.UnitPrefab, _scene.UnitSpawn);
        scope.Add (() => Object.Destroy (view.gameObject));

        var parallel = await Routine.GetParallel ();
        parallel.Attach (ObserveAmount ()); // change state 

        // healthy state
        view.Image.color = Color.green;

        await Routine.When (() => _counterAspect.CUnit.Get (entity).Health < 50);
        // injured state
        view.Image.color = Color.yellow;

        await Routine.When (() => _counterAspect.CUnit.Get (entity).Health <= 0);
        // dead state
        view.Image.color = Color.red;

        var timer = 3f;
        await Routine.When (() => {
            timer -= Time.deltaTime;
            return timer < 0f;
        });

        _counterAspect.CUnit.Del (entity);
        return;

        async Routine ObserveAmount () {
            while (true) {
                var cache = _counterAspect.CUnit.Get (entity).Health;
                view.Slider.value = cache;
                view.Text.text = cache.ToString ();
                await Routine.When (() => cache != _counterAspect.CUnit.Get (entity).Health);
            }
        }
    }
}