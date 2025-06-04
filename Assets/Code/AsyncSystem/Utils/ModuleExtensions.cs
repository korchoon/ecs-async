using System;
using Leopotam.EcsProto;
using Leopotam.EcsProto.QoL;

namespace AsyncSystem.Utils {
    public static class ModuleExtensions {
        public static void AddSystem <T>(this ProtoModules modules, T system)where T : IProtoSystem {
            if (system is IHaveAspect asyncSystem) {
                modules.AddAspect (asyncSystem.GetAspect ());
            }

            modules.AddModule (new SystemModule<T> (system));
        }

        public static void AddModulesFrom (this ProtoModules modules, ProtoModules toAdd) {
            foreach (var m in toAdd.Modules ()) {
                modules.AddModule (m);
            }

            modules.AddAspect (toAdd.BuildAspect ());
        }

        // ReSharper disable once UnusedTypeParameter
        class SystemModule<T> : IProtoModule {
            readonly IProtoSystem _system;

            public SystemModule (IProtoSystem system) {
                _system = system;
            }

            public void Init (IProtoSystems systems) {
                systems.AddSystem (_system);
            }

            public IProtoAspect[] Aspects () {
                return null;
            }

            public Type[] Dependencies () {
                return null;
            }

            public IProtoModule[] Modules () {
                return null;
            }
        }
    }
}