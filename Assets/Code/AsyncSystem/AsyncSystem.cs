using System;
using System.Linq;
using Leopotam.EcsProto;
using Leopotam.EcsProto.QoL;
using Mk.Routines;

/// <summary>
/// Система, которая позволяет 
/// </summary>
/// <typeparam name="TSelf"></typeparam>
public abstract class AsyncSystem<TSelf> : IAsyncSystem, IProtoRunSystem, IProtoInitSystem, IProtoDestroySystem
    where TSelf : AsyncSystem<TSelf> {
    _Aspect _self;
    Inc _incMode;

    ProtoItExc _routineStart;
    Slice<(Routine routine, ProtoPackedEntityWithWorld entity)> _buf = new ();

    public IProtoAspect GetAspect () => new _Aspect ();

    protected abstract Routine Run (ProtoEntity entity);

    protected abstract IProtoIt GetProtoIt ();


    public void Init (IProtoSystems systems) {
        _self = ProtoAspectInjectUtil.GetAspect<_Aspect> (systems);
        var world = _self.World ();
        var sourceIt = GetProtoIt ();
        if (sourceIt is ProtoIt inc) {
            _incMode = new () {
                Source = inc,
                End = new (new[] { typeof (_Routine) })
            };
            _incMode.Source.Init (world);
            _incMode.End.Init (world);
            var inc1 = inc.Includes ().Select (t => t.ItemType ()).ToArray ();
            _routineStart = new (inc1, It.Exc<_Routine> ());
        }
        else {
            throw new NotImplementedException ();
        }

        _routineStart.Init (world);

        Run ();
    }

    public void Run () {
        foreach (var e in _routineStart) {
            _self._routine.Add (e).Routine = Run (e);
        }

        var either = _incMode;
        foreach (var e in either.End) {
            if (either.Source.Has (e)) {
                _buf.Add ((_self._routine.Get (e).Routine, _self._world.PackEntityWithWorld (e)));
            }
            else {
                _self._routine.DelIfExists (e);
            }
        }

        for (var i = 0; i < _buf.Len (); i++) {
            ref var valueTuple = ref _buf.Get (i);
            if (!valueTuple.entity.TryUnpack (out _, out _)) {
                continue;
            }

            valueTuple.routine.Tick ();
        }

        _buf.Clear ();
    }

    public void Destroy () {
        foreach (var e in _incMode.End) {
            _self._routine.DelIfExists (e);
        }
    }

    class _Aspect : IProtoAspect {
        public ProtoWorld _world;
        public ProtoPool<_Routine> _routine;

        ProtoAspectInjectUtil.Result _res;

        public void Init (ProtoWorld world) {
            _res = ProtoAspectInjectUtil.Init (this, world);
            Asr.IsTrue (_world == null);
            _world = world;
            ProtoAspectInjectUtil.GetPool (_world, ref _routine);
        }

        public void PostInit () {
            ProtoAspectInjectUtil.PostInit (_res);
        }

        public ProtoWorld World () {
            return _world;
        }
    }

    class Inc {
        public ProtoIt Source;
        public ProtoIt End;
    }

    struct _Routine : IProtoAutoReset<_Routine> {
        public Routine Routine;

        public void AutoReset (ref _Routine c) {
            c.Routine?.Dispose ();
            c = default;
        }
    }
}