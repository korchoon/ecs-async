using Leopotam.EcsProto;
using Leopotam.EcsProto.QoL;

static class ModuleExtensions {
    public static void AddSystem (this ProtoModules modules, IProtoSystem system) {
        if (system is IAsyncSystem asyncSystem) {
            modules.AddAspect (asyncSystem.GetAspect ());
        }

        modules.AddModule (new SystemModule (system));
    }

    class SystemModule : IProtoModule {
        public readonly IProtoSystem System;

        public SystemModule (IProtoSystem system) {
            System = system;
        }

        public void Init (IProtoSystems systems) {
            systems.AddSystem (System);
        }

        public IProtoAspect[] Aspects () {
            return null;
        }

        public IProtoModule[] Modules () {
            return null;
        }
    }
}