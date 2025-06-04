using System;
using Leopotam.EcsProto;
using Leopotam.EcsProto.QoL;
using Mk.Scopes;

namespace AsyncSystem {
    /// <summary>
    /// Базовый класс для системы в духе стейтов FSM в ProtoECS.
    /// Позволяет писать логику в виде OnEnter-OnUpdate-OnExit в рамках сущности и фильтра, которому она удовлетворяет.
    /// </summary>
    /// <typeparam name="TSelf">Тип наследуемой системы.</typeparam>
    public abstract class StateSystem<TSelf> : IHaveAspect, IProtoRunSystem, IProtoInitSystem, IProtoDestroySystem where TSelf : StateSystem<TSelf> {
        _Aspect _self;
        Inc _incMode;
        ProtoItExc _scopeStart;
        Slice<ProtoPackedEntityWithWorld> _buf = new ();

        public IProtoAspect GetAspect () => new _Aspect ();

        /// <summary>
        /// Событие когда сущность оказалась в фильтре. Фильтр пределяется в GetProtoIt(), может быть Inc, Exc
        /// </summary>
        /// <param name="entity">Сущность</param>
        /// <param name="onExit">ивент который отработает как только сущность выйдет из фильтра (или уничтожится)</param>
        protected abstract void OnEnter (ProtoEntity entity, ISafeScope onExit);

        /// <summary>
        /// Каждый тик, пока сущность находится в фильтре, включая первый кадр
        /// </summary>
        /// <param name="entity"></param>
        protected virtual void OnUpdate (ProtoEntity entity) { }

        /// <summary>
        /// Фильтр, при попадании в который сущности начнут обрабатываться этой системой
        /// </summary>
        /// <returns></returns>
        protected abstract IProtoIt GetProtoIt ();


        public void Init (IProtoSystems systems) {
            _self = systems.World ().Aspect<_Aspect> ();
            var world = _self.World ();
            var sourceIt = GetProtoIt ();
            if (sourceIt is ProtoIt inc) {
                _incMode = new () {
                    Source = inc,
                    End = new (new[] { typeof (_State) })
                };
                _incMode.Source.Init (world);
                _incMode.End.Init (world);
                var protoPools = inc.Includes ();
                var inc1 = new Type[protoPools.Length];
                for (var i = protoPools.Length - 1; i >= 0; i--) {
                    inc1[i] = protoPools[i].ItemType ();
                }

                _scopeStart = new (inc1, It.Exc<_State> ());
            }
            else {
                throw new NotImplementedException ();
            }

            _scopeStart.Init (world);

            Run ();
        }

        public void Run () {
            foreach (var e in _scopeStart) {
                ref var c = ref _self._scope.Add (e);
                c.Scope = new ();
                OnEnter (e, c.Scope);
            }

            var either = _incMode;
            foreach (var e in either.End) {
                if (either.Source.Has (e)) {
                    _buf.Add (_self._world.PackEntityWithWorld (e));
                }
                else {
                    _self._scope.DelIfExists (e);
                }
            }

            for (var i = 0; i < _buf.Len (); i++) {
                ref var ePacked = ref _buf.Get (i);
                if (ePacked.TryUnpack (out var world, out var entity)) {
                    OnUpdate (entity);
                }
            }

            _buf.Clear ();
        }

        public void Destroy () {
            foreach (var e in _incMode.End) {
                _self._scope.DelIfExists (e);
            }
        }

        class _Aspect : IProtoAspect {
            public ProtoWorld _world;
            public ProtoPool<_State> _scope;

            ProtoAspectInjectUtil.Result _res;

            public void Init (ProtoWorld world) {
                _res = ProtoAspectInjectUtil.Init (this, world);
                Asr.IsTrue (_world == null);
                _world = world;
                ProtoAspectInjectUtil.GetPool (_world, ref _scope);
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

        struct _State : IProtoHandlers<_State> {
            public Scope Scope;

            public void SetHandlers (IProtoPool<_State> pool) {
                pool.SetResetHandler (AutoReset);
            }

            static void AutoReset (ref _State c) {
                c.Scope?.Dispose ();
                c = default;
            }
        }
    }
}