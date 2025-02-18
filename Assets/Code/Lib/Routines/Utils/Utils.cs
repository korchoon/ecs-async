// ----------------------------------------------------------------------------
// Async Routines framework https://github.com/korchoon/mk.routines
// Copyright (c) 2016-2023 Mikhail Korchun <korchoon@gmail.com>
// ----------------------------------------------------------------------------

namespace Mk.Routines {
    static class FLAGS {
        public const string DEBUG = "DEBUG";
        public const string UNITY_EDITOR = "UNITY_EDITOR";
        public const string UNITY_INCLUDE_TESTS = "UNITY_INCLUDE_TESTS";
    }
    
    static class Utils {
        public static bool TrySetNull<T>(ref T arg, out T buf) where T : class {
            if (arg == null) {
                buf = null;
                return false;
            }

            buf = arg;
            arg = null;
            return true;
        }
    }
}