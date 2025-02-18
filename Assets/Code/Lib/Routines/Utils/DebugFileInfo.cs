// ----------------------------------------------------------------------------
// Async Routines framework https://github.com/korchoon/mk.routines
// Copyright (c) 2016-2023 Mikhail Korchun <korchoon@gmail.com>
// ----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Mk.Routines {
    class DebugFileInfo {
        Dictionary<StackFrame, string> FrameToLine;
        Dictionary<string, string[]> PathToLines;

        public DebugFileInfo () {
            FrameToLine = new ();
            PathToLines = new ();
        }

        // [Conditional("MK_TRACE")]
        public void SetDebugName (ref string target, int skipFrames) {
            var withoutFileInfo = new StackTrace (1 + skipFrames, fNeedFileInfo: false).GetFrame (0);
            if (TryGetLine (withoutFileInfo, out var line)) {
                target = line;
                return;
            }

            var fullFrame = new StackTrace (1 + skipFrames, fNeedFileInfo: true).GetFrame (0);
            GetLine (withoutFileInfo, fullFrame, out line);
            target = line;
        }

        public string GetDebugName (int skipFrames) {
            var withoutFileInfo = new StackTrace (1 + skipFrames, fNeedFileInfo: false).GetFrame (0);
            if (TryGetLine (withoutFileInfo, out var line)) {
                return line;
            }

            var fullFrame = new StackTrace (1 + skipFrames, fNeedFileInfo: true).GetFrame (0);
            GetLine (withoutFileInfo, fullFrame, out line);
            return line;
        }

        bool TryGetLine (StackFrame withoutFileInfo, out string line) {
            return FrameToLine.TryGetValue (withoutFileInfo, out line);
        }

        void GetLine (StackFrame withoutFileInfo, StackFrame withFileInfo, out string line) {
            var path = withFileInfo.GetFileName ();
            if (!PathToLines.TryGetValue (path, out var lines)) {
                if (!File.Exists (path)) {
                    line = default;
                    return;
                }

                lines = File.ReadAllLines (path);
                PathToLines.Add (path, lines);
            }

            var fileLineNumber = withFileInfo.GetFileLineNumber ();
            var s = lines[fileLineNumber - 1];
            // line = $"{s.Trim()} | {Path.GetFileName(path)}:{fileLineNumber}";
            var col = withFileInfo.GetFileColumnNumber ();
            line = $"{s.Substring (col - 1).Trim ()} [{Path.GetFileNameWithoutExtension (path)}:{fileLineNumber}]";
            FrameToLine.Add (withoutFileInfo, line);
        }
    }
}