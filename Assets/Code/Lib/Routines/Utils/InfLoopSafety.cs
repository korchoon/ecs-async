// ----------------------------------------------------------------------------
// Async Routines framework https://github.com/korchoon/mk.routines
// Copyright (c) 2016-2023 Mikhail Korchun <korchoon@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Threading;

class InfLoopSafety {
    readonly int _max;
    int _cachedFrameCount;
    int _timesPerFrame;
    int FrameCount () => UnityEngine.Time.frameCount;

    public InfLoopSafety (int max) {
        _max = max;
    }

    [Conditional ("PREVENT_INF_LOOP")]
    public void Check () {
        _timesPerFrame += 1;
        if (_timesPerFrame > _max) {
            throw new ("Inf loop detected");
        }

        var curFrameCount = FrameCount ();
        if (curFrameCount > _cachedFrameCount) {
            _cachedFrameCount = curFrameCount;
            _timesPerFrame = 0;
        }
    }
}