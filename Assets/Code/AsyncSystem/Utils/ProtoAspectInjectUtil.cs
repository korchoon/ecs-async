using System;
using System.Collections.Generic;
using System.Reflection;
using JetBrains.Annotations;
using Leopotam.EcsProto;

namespace AsyncSystem {
    static class ProtoAspectInjectUtil {
        static readonly Type _aspectType = typeof (IProtoAspect);
        static readonly Type _poolType = typeof (IProtoPool);
        static readonly Type _itType = typeof (IProtoIt);

        public class Result {
            public List<IProtoAspect> AspectAspects;
            public List<IProtoPool> AspectPools;
            public List<Type> AspectPoolTypes;
            public List<IProtoIt> AspectIts;
            public ProtoWorld World;
            public ProtoIt It;
        }

        public static void GetPool<T> (ProtoWorld world, ref ProtoPool<T> pool) where T : struct {
            if (!world.HasPool (typeof (T))) {
                pool = new ();
                world.AddPool (pool);
            }
            else {
                pool = (ProtoPool<T>)world.Pool (typeof (T));
            }
        }

        [MustUseReturnValue]
        public static TSelf GetAspect<TSelf> (IProtoSystems systems) where TSelf : class, IProtoAspect {
            foreach (var namedWorld in systems.NamedWorlds ()) {
                if (namedWorld.Value.HasAspect (typeof (TSelf))) {
                    var protoAspect = (TSelf)namedWorld.Value.Aspect (typeof(TSelf));
                    Asr.IsNotNull (protoAspect);
                    return protoAspect;
                }
            }

            if (systems.World ().HasAspect (typeof (TSelf))) {
                var protoAspect = (TSelf)systems.World ().Aspect (typeof(TSelf));
                Asr.IsNotNull (protoAspect);
                return protoAspect;
            }


            Asr.Fail ($"Failed to find {typeof (TSelf).FullName}");
            return null;
        }

        public static Result Init (IProtoAspect self, ProtoWorld world) {
            world.AddAspect (self);
            var result = new Result {
                AspectAspects = new (4),
                AspectPools = new (8),
                AspectPoolTypes = new (8),
                AspectIts = new (8),
                World = world,
                It = null,
            };
            foreach (var f in self.GetType ().GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
                if (f.IsStatic) {
                    continue;
                }

                // аспекты.
                if (_aspectType.IsAssignableFrom (f.FieldType)) {
                    if (world.HasAspect (f.FieldType)) {
                        f.SetValue (self, world.Aspect (f.FieldType));
                        continue;
                    }

                    var aspect = (IProtoAspect)f.GetValue (self);
                    if (aspect == null) {
#if DEBUG
                        if (f.FieldType.GetConstructor (Type.EmptyTypes) == null) {
                            throw new Exception ($"аспект \"{DebugHelpers.CleanTypeName (f.FieldType)}\" должен иметь конструктор по умолчанию, либо экземпляр должен быть создан заранее");
                        }
#endif
                        aspect = (IProtoAspect)Activator.CreateInstance (f.FieldType);
                        f.SetValue (self, aspect);
                    }

                    result.AspectAspects.Add (aspect);
                    aspect.Init (world);
                    continue;
                }

                // пулы.
                if (_poolType.IsAssignableFrom (f.FieldType)) {
                    var pool = (IProtoPool)f.GetValue (self);
                    if (pool == null) {
#if DEBUG
                        if (f.FieldType.GetConstructor (Type.EmptyTypes) == null) {
                            throw new Exception ($"пул \"{DebugHelpers.CleanTypeName (f.FieldType)}\" должен иметь конструктор по умолчанию, либо экземпляр должен быть создан заранее");
                        }
#endif
                        pool = (IProtoPool)Activator.CreateInstance (f.FieldType);
                    }

                    var itemType = pool.ItemType ();
                    if (world.HasPool (itemType)) {
                        pool = world.Pool (itemType);
                    }
                    else {
                        world.AddPool (pool);
                    }

                    result.AspectPools.Add (pool);
                    result.AspectPoolTypes.Add (pool.ItemType ());
                    f.SetValue (self, pool);
                    continue;
                }

                // итераторы.
                if (_itType.IsAssignableFrom (f.FieldType)) {
                    var it = (IProtoIt)f.GetValue (self);
#if DEBUG
                    if (it == null) {
                        throw new Exception ($"итератор \"{DebugHelpers.CleanTypeName (f.FieldType)}\" должен быть создан заранее");
                    }
#endif
                    result.AspectIts.Add (it);
                }
            }

            return result;
        }

        public static void PostInit (Result arg) {
            foreach (var aspect in arg.AspectAspects) {
                aspect.PostInit ();
            }

            foreach (var it in arg.AspectIts) {
                it.Init (arg.World);
            }

            if (arg.AspectPoolTypes.Count > 0) {
                arg.It = new ProtoIt (arg.AspectPoolTypes.ToArray ());
                arg.It.Init (arg.World);
            }
        }
    }
}