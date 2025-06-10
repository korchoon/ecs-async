using System;
using Leopotam.EcsProto;
using Leopotam.EcsProto.QoL;
using Mk.Routines;

namespace AsyncSystem {
    /// <summary>
    /// Базовый класс для асинхронных систем в ProtoECS.
    /// Позволяет писать логику в синтаксисе `async/await`, в рамках сущности попадающей в указанный итератор
    /// </summary>
    /// <typeparam name="TSelf">Тип наследуемой системы.</typeparam>
    public abstract class AsyncSystem<TSelf> : IHaveAspect, IProtoRunSystem, IProtoInitSystem, IProtoDestroySystem
        where TSelf : AsyncSystem<TSelf> {
        _Aspect _self;
        Inc _incMode;

        ProtoItExc _routineStart;
        Slice<(Routine routine, ProtoPackedEntityWithWorld entity)> _buf = new ();

        public IProtoAspect GetAspect () => new _Aspect ();

        protected abstract Routine Run (ProtoEntity entity);

        protected abstract IProtoIt GetProtoIt ();


        public void Init (IProtoSystems systems) {
            _self = systems.World ().Aspect<_Aspect> ();
            var world = _self.World ();
            var sourceIt = GetProtoIt ();
            if (sourceIt is ProtoIt inc) {
                _incMode = new () {
                    Source = inc,
                    End = new (new[] { typeof (_Routine) })
                };
                _incMode.Source.Init (world);
                _incMode.End.Init (world);
                var protoPools = inc.Includes ();
                var inc1 = new Type[protoPools.Length];
                for (var i = protoPools.Length - 1; i >= 0; i--) {
                    inc1[i] = protoPools[i].ItemType ();
                }

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
                _self._pool.Add (e).Routine = Run (e);
            }

            var either = _incMode;
            foreach (var e in either.End) {
                if (either.Source.Has (e)) {
                    _buf.Add ((_self._pool.Get (e).Routine, _self._world.PackEntityWithWorld (e)));
                }
                else {
                    _self._pool.DelIfExists (e);
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
                _self._pool.DelIfExists (e);
            }
        }

        class _Aspect : IProtoAspect {
            public ProtoWorld _world;
            public ProtoPool<_Routine> _pool;

            public void Init (ProtoWorld world) {
                Asr.IsTrue (_world == null);
                _world = world;
                _world.AddAspect (this);
                _pool = new ();
                _world.AddPool (_pool);
            }

            public void PostInit () { }

            public ProtoWorld World () {
                return _world;
            }
        }

        class Inc {
            public ProtoIt Source;
            public ProtoIt End;
        }

        struct _Routine : IProtoHandlers<_Routine> {
            public Routine Routine;

            public void SetHandlers (IProtoPool<_Routine> pool) {
                pool.SetResetHandler (AutoReset);
            }

            static void AutoReset (ref _Routine c) {
                c.Routine?.Dispose ();
                c = default;
            }
        }
    }
}