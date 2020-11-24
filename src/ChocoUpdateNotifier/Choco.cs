using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ChocoUpdateNotifier
{
    /// <summary>
    /// Wrapper for the chocolatey executables
    /// </summary>
    internal static class Choco
    {
        private static readonly Regex _pckNamePattern = new Regex("^([^|]+)\\|([^|]+)\\|([^|]+)\\|(true|false)\r?$", RegexOptions.Compiled);

        internal class ChocoPackage
        {
            public string Name { get; set; }
            public bool IsPinned { get; set; }
            public string OldVersion { get; set; }
            public string NewVersion { get; set; }
            public ChocoPackage(string Name, bool IsPinned, string OldVersion, string NewVersion)
            {
                this.Name = Name;
                this.IsPinned = IsPinned;
                this.OldVersion = OldVersion;
                this.NewVersion = NewVersion;
            }
        }

        /// <summary>
        /// Updates all un-pinned choco packages
        /// </summary>
        public static void UpdateAllPackages()
        {
            using Process proc = Process.Start(new ProcessStartInfo("choco.exe", "upgrade all -y")
            {
                Verb = "runas",
                UseShellExecute = true
            });
        }

        /// <summary>
        /// Updates a list of packages
        /// </summary>
        /// <param name="packages">Package names</param>
        public static void UpdatePackages(string[] packages)
        {
            // Create upgrade script
            string tmpScriptPath = Path.Combine(Path.GetTempPath(), "chocoUpdate.bat");
            if (File.Exists(tmpScriptPath))
            {
                File.Delete(tmpScriptPath);
            }
            File.WriteAllText(tmpScriptPath, string.Join(Environment.NewLine, packages.Select(pck => $"choco.exe upgrade {pck} -y").ToArray()));

            // Run as admin
            using Process proc = Process.Start(new ProcessStartInfo(tmpScriptPath)
            {
                Verb = "runas",
                UseShellExecute = true
            });
        }

        /// <summary>
        /// Checks for outdated chocolatey packages
        /// </summary>
        /// <returns>list of outdated package names</returns>
        public static IList<ChocoPackage> OutdatedPackages(bool noPinned = true)
        {
            var outdated = RunChoco($"outdated --ignore-unfound {(noPinned ? "--ignore-pinned" : "")} -r");
            return outdated.Split('\n')
                           .Select(line => _pckNamePattern.Match(line))
                           .Where(match => !string.IsNullOrWhiteSpace(match.Groups[1].Value))
                           .Select(match => new ChocoPackage(match.Groups[1].Value,
                                                            bool.Parse(match.Groups[4].Value),
                                                            match.Groups[2].Value,
                                                            match.Groups[3].Value))
                           .ToList();
        }

        /// <summary>
        /// Runs a chocolatey command
        /// </summary>
        /// <param name="arguments">command + arguments</param>
        /// <returns>resulting string output</returns>
        private static string RunChoco(string arguments)
        {
            using Process proc = Process.Start(new ProcessStartInfo("choco.exe", arguments)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
            var read = proc.StandardOutput.ReadToEndAsync();
            proc.WaitForExit();
            return read.Result;
        }
    }
}
