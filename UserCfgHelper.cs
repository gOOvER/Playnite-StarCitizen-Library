using System;
using System.IO;
using System.Text.RegularExpressions;

namespace StarCitizenLibrary
{
    public static class UserCfgHelper
    {
        public static void SetDisplayInfo(string channelDir, int level)
        {
            if (string.IsNullOrEmpty(channelDir) || !Directory.Exists(channelDir)) return;

            try
            {
                var cfgPath = Path.Combine(channelDir, "user.cfg");
                var displayInfoLine = string.Format("r_displayinfo = {0}", level);

                if (File.Exists(cfgPath))
                {
                    var text = File.ReadAllText(cfgPath);
                    var regex = new Regex(@"^\s*r_displayinfo\s*=.*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                    string updated;
                    if (regex.IsMatch(text))
                    {
                        updated = regex.Replace(text, displayInfoLine);
                    }
                    else
                    {
                        updated = text.TrimEnd() + "\r\n" + displayInfoLine + "\r\n";
                    }
                    File.WriteAllText(cfgPath, updated);
                }
                else
                {
                    File.WriteAllText(cfgPath, displayInfoLine + "\r\n");
                }
            }
            catch { }
        }

        public static int? GetDisplayInfo(string channelDir)
        {
            if (string.IsNullOrEmpty(channelDir)) return null;
            var cfgPath = Path.Combine(channelDir, "user.cfg");
            if (!File.Exists(cfgPath)) return null;

            try
            {
                var text = File.ReadAllText(cfgPath);
                var match = Regex.Match(text, @"^\s*r_displayinfo\s*=\s*(\d+)", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                if (match.Success && int.TryParse(match.Groups[1].Value, out var val))
                {
                    return val;
                }
            }
            catch { }

            return null;
        }
    }
}
