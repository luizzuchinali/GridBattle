using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using GridBattle.Core;
using GridBattle.Gameplay.Simulation;
using Newtonsoft.Json;
using UnityEngine;

namespace GridBattle.Gameplay.Meta
{
    /// <summary>
    /// Local recorder of the design metrics (GDD 9): appends one JSON object per line to
    /// <c>metrics.jsonl</c> in the persistent data path. Nothing is sent anywhere (how the data
    /// is collected, stored and sent is an open question). Does nothing when
    /// <see cref="MetaSettings.RecordMetrics"/> is off.
    /// <para>
    /// Line format: <c>{"t": UTC time, "session": id, "seq": n, "type": eventType, "ctx": {...}, "data": payload}</c>.
    /// <see cref="MetricsHooks"/> records what the game raises by itself; other modules (map nodes,
    /// talents offered/chosen/declined, rerolls, bans, skips...) call <see cref="Record"/>.
    /// </para>
    /// Lines are buffered and written when the buffer is large, a battle ends, the app loses focus
    /// or quits (<see cref="Flush"/>).
    /// </summary>
    public static class MetricsRecorder
    {
        public const string DefaultFileName = "metrics.jsonl";
        public const string RotatedFileName = "metrics.old.jsonl";

        private const int FlushThreshold = 50;

        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            Formatting = Formatting.None,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        };

        private static readonly List<string> Buffer = new();
        private static readonly Dictionary<string, object> Context = new();
        private static string _fileName = DefaultFileName;
        private static string _sessionId;
        private static int _sequence;

        /// <summary>Whether metrics are being recorded (setting on).</summary>
        public static bool Enabled => MetaSettings.Current.RecordMetrics && !SimMode.IsActive;

        public static string FilePath => SaveSystem.GetPath(_fileName);

        /// <summary>Tests only: writes to another file (flushing what is buffered to the current one first). Null = default.</summary>
        public static void UseFile(string fileName)
        {
            Flush();
            _fileName = string.IsNullOrEmpty(fileName) ? DefaultFileName : fileName;
        }

        /// <summary>Random id of this play session, written on every line.</summary>
        public static string SessionId => _sessionId ??= Guid.NewGuid().ToString("N").Substring(0, 12);

        /// <summary>
        /// Adds a line. <paramref name="payload"/> must be plain data (numbers, strings, lists,
        /// dictionaries, anonymous objects); do not pass Unity objects or vectors.
        /// </summary>
        public static void Record(string eventType, object payload = null)
        {
            if (string.IsNullOrEmpty(eventType) || !Enabled) return;

            var line = new Dictionary<string, object>
            {
                ["t"] = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                ["session"] = SessionId,
                ["seq"] = _sequence++,
                ["type"] = eventType,
            };
            if (Context.Count > 0)
                line["ctx"] = new Dictionary<string, object>(Context);
            if (payload != null)
                line["data"] = payload;

            try
            {
                Buffer.Add(JsonConvert.SerializeObject(line, JsonSettings));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Metrics event '{eventType}' could not be serialized: {exception.Message}");
                return;
            }

            if (Buffer.Count >= FlushThreshold)
                Flush();
        }

        /// <summary>
        /// Sets a field written on every following line under "ctx" (e.g. the class id, the run seed,
        /// the current depth). Pass null to remove it.
        /// </summary>
        public static void SetContext(string key, object value)
        {
            if (string.IsNullOrEmpty(key)) return;

            if (value == null)
                Context.Remove(key);
            else
                Context[key] = value;
        }

        public static void ClearContext() => Context.Clear();

        /// <summary>Writes the buffered lines to the file (rotating it when it grew past the size limit).</summary>
        public static void Flush()
        {
            if (Buffer.Count == 0) return;

            var text = new StringBuilder();
            foreach (var line in Buffer)
                text.Append(line).Append('\n');
            Buffer.Clear();

            try
            {
                var path = FilePath;
                RotateIfTooBig(path);
                File.AppendAllText(path, text.ToString());
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to write '{_fileName}': {exception}");
            }
        }

        /// <summary>Flushes and forgets the session (new session id, empty context).</summary>
        internal static void Reset()
        {
            Flush();
            _fileName = DefaultFileName;
            Context.Clear();
            _sessionId = null;
            _sequence = 0;
        }

        internal static void OnFocusChanged(bool hasFocus)
        {
            if (!hasFocus)
                Flush();
        }

        private static void RotateIfTooBig(string path)
        {
            var maxKb = MetaSettings.Current.MetricsMaxFileKb;
            if (maxKb <= 0 || !File.Exists(path)) return;
            if (new FileInfo(path).Length <= maxKb * 1024L) return;

            var rotated = SaveSystem.GetPath(RotatedFileName);
            if (File.Exists(rotated))
                File.Delete(rotated);
            File.Move(path, rotated);
        }
    }
}
