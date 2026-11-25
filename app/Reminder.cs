using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace SipStretch
{
    public class Preferences
    {
        private static readonly object SaveLock = new object();
        public int WaterMinutes = 45;
        public int MoveMinutes = 60;
        public bool WaterEnabled = true;
        public bool MoveEnabled = true;

        internal static int LimitMinutes(int value)
        {
            return Math.Max(1, Math.Min(480, value));
        }

        public static Preferences Load(string path)
        {
            var preferences = new Preferences();
            string[] lines;
            try
            {
                lines = File.ReadAllLines(path, Encoding.UTF8);
            }
            catch (IOException) { return preferences; }
            catch (UnauthorizedAccessException) { return preferences; }

            foreach (string line in lines)
            {
                int split = line.IndexOf('=');
                if (split < 1) continue;
                string key = line.Substring(0, split).Trim().ToLowerInvariant();
                string value = line.Substring(split + 1).Trim();
                int minutes;
                bool enabled;
                switch (key)
                {
                    case "waterminutes":
                        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out minutes))
                            preferences.WaterMinutes = LimitMinutes(minutes);
                        break;
                    case "moveminutes":
                        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out minutes))
                            preferences.MoveMinutes = LimitMinutes(minutes);
                        break;
                    case "waterenabled":
                        if (bool.TryParse(value, out enabled)) preferences.WaterEnabled = enabled;
                        break;
                    case "moveenabled":
                        if (bool.TryParse(value, out enabled)) preferences.MoveEnabled = enabled;
                        break;
                }
            }
            return preferences;
        }

        // The temporary file and destination share a directory so replacement is one rename.
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool MoveFileEx(string existing, string destination, uint flags);

        public void Save(string path)
        {
            lock (SaveLock) SaveToPath(path);
        }

        private void SaveToPath(string path)
        {
            string destination = Path.GetFullPath(path);
            string temporary = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    writer.WriteLine("WaterMinutes=" + LimitMinutes(WaterMinutes).ToString(CultureInfo.InvariantCulture));
                    writer.WriteLine("MoveMinutes=" + LimitMinutes(MoveMinutes).ToString(CultureInfo.InvariantCulture));
                    writer.WriteLine("WaterEnabled=" + WaterEnabled);
                    writer.WriteLine("MoveEnabled=" + MoveEnabled);
                    writer.Flush();
                    stream.Flush(true);
                }
                const uint replaceExisting = 1, writeThrough = 8;
                if (!MoveFileEx(temporary, destination, replaceExisting | writeThrough))
                    throw new IOException("Не удалось сохранить настройки.", new Win32Exception(Marshal.GetLastWin32Error()));
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }
    }

    public class ReminderClock
    {
        private int waterMinutes, moveMinutes;
        private bool waterEnabled, moveEnabled;
        private DateTime? nextWater, nextMove;

        public ReminderClock(Preferences prefs, DateTime now)
        {
            Reset(prefs, now);
        }

        public void Reset(Preferences prefs, DateTime now)
        {
            waterMinutes = Preferences.LimitMinutes(prefs.WaterMinutes);
            moveMinutes = Preferences.LimitMinutes(prefs.MoveMinutes);
            waterEnabled = prefs.WaterEnabled;
            moveEnabled = prefs.MoveEnabled;
            Resume(now);
        }

        public void Resume(DateTime now)
        {
            nextWater = waterEnabled ? now.AddMinutes(waterMinutes) : (DateTime?)null;
            nextMove = moveEnabled ? now.AddMinutes(moveMinutes) : (DateTime?)null;
        }

        public DateTime? Next(bool water)
        {
            return water ? nextWater : nextMove;
        }

        public bool? Due(DateTime now)
        {
            if (nextWater <= now && (!nextMove.HasValue || nextWater <= nextMove)) return true;
            if (nextMove <= now) return false;
            return null;
        }

        private void Schedule(bool water, DateTime due)
        {
            if (water && waterEnabled) nextWater = due;
            if (!water && moveEnabled) nextMove = due;
        }

        public void Finish(bool water, DateTime now)
        {
            Schedule(water, now.AddMinutes(water ? waterMinutes : moveMinutes));
        }

        public void Snooze(bool water, DateTime now)
        {
            Schedule(water, now.AddMinutes(5));
        }
    }
}
