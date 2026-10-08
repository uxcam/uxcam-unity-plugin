using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace UXCamSDK.Internal
{
    internal static class StackTraceParser
    {
        // Exception.StackTrace: "  at Game.Player.Jump (System.Int32 height) [0x00012] in /path/Player.cs:42"
        static readonly Regex s_DotNetFrame = new Regex(
            @"^\s*at\s+(?<method>[^\(]+?)\s*\((?<args>[^\)]*)\)(?:\s*\[0x[0-9a-fA-F]+\])?(?:\s+in\s+(?<file>.+?):(?:line\s+)?(?<line>\d+))?\s*$",
            RegexOptions.Compiled);

        // Unity's log stack trace: "Game.Player:Jump (int) (at Assets/Scripts/Player.cs:42)"
        static readonly Regex s_UnityFrame = new Regex(
            @"^\s*(?<method>[^\s\(]+?)\s*\((?<args>[^\)]*)\)(?:\s*\(at\s+(?<file>.+?):(?<line>\d+)\))?\s*$",
            RegexOptions.Compiled);

        internal static List<Dictionary<string, object>> Parse(string stackTrace)
        {
            var frames = new List<Dictionary<string, object>>();
            if (string.IsNullOrEmpty(stackTrace)) return frames;

            foreach (var rawLine in stackTrace.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var line = rawLine.Trim();
                if (line.Length == 0) continue;

                var match = s_DotNetFrame.Match(line);
                if (!match.Success) match = s_UnityFrame.Match(line);

                if (!match.Success)
                {
                    frames.Add(Frame(string.Empty, line, string.Empty, -1));
                    continue;
                }

                SplitMethod(match.Groups["method"].Value.Trim(), out var type, out var method);
                var file = match.Groups["file"].Success ? match.Groups["file"].Value : string.Empty;
                if (file == "<filename unknown>") file = string.Empty;
                var lineNumber = match.Groups["line"].Success && int.TryParse(match.Groups["line"].Value, out var n) ? n : -1;
                frames.Add(Frame(type, method, file, lineNumber));
            }

            return frames;
        }

        static void SplitMethod(string qualified, out string type, out string method)
        {
            int split = qualified.LastIndexOf(':');
            if (split < 0) split = qualified.LastIndexOf('.');
            if (split <= 0 || split == qualified.Length - 1)
            {
                type = string.Empty;
                method = qualified;
                return;
            }

            type = qualified.Substring(0, split);
            method = qualified.Substring(split + 1);
        }

        static Dictionary<string, object> Frame(string type, string method, string file, int line)
        {
            return new Dictionary<string, object>
            {
                ["class"] = type,
                ["method"] = method,
                ["file"] = file,
                ["line"] = line,
            };
        }
    }
}
