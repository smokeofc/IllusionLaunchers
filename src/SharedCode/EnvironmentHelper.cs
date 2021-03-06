﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media.Imaging;
using Application = System.Windows.Forms.Application;
using MessageBox = System.Windows.Forms.MessageBox;

namespace InitSetting
{
    public static class EnvironmentHelper
    {
        private static readonly string _mStrSaveDir = "/UserData/setup.xml";
        private static readonly string _mCustomDir = "/UserData/LauncherEN";
        private static readonly string _decideLang = "/lang";
        private static readonly string _versioningLoc = "/version";
        private static readonly string _warningLoc = "/warning.txt";
        private static readonly string _charLoc = "/Chara.png";
        private static readonly string _backgLoc = "/LauncherBG.png";
        private static readonly string _patreonLoc = "/patreon.txt";
        private static readonly string _kkmdir = "/kkman.txt";
        private static readonly string _updateLoc = "/updater.txt";
        private static bool _langExists;
        private static bool _updatelocExists;
        private static string _kkman;
        private static string _updateSources = "placeholder";

        public static string Language { get; private set; } = "en";

        public static bool KKmanExist { get; private set; }
        public static bool IsIpa { get; private set; }
        public static bool IsBepIn { get; private set; }
        public static string PatreonUrl { get; private set; }
        public static string VersionString { get; private set; }
        public static string WarningString { get; private set; }
        public static BitmapFrame CustomBgImage { get; private set; }
        public static BitmapFrame CustomCharacterImage { get; private set; }

        public static string BepinPluginsDir { get; private set; }
        public static string GameRootDirectory { get; private set; }

        public static bool? DeveloperModeEnabled
        {
            get
            {
                if (File.Exists(GameRootDirectory + "/Bepinex/config/BepInEx.cfg"))
                {
                    var ud = Path.Combine(GameRootDirectory, @"BepInEx\config\BepInEx.cfg");

                    try
                    {
                        var contents = File.ReadAllLines(ud).ToList();

                        var devmodeEn = contents.FindIndex(s => s.ToLower().Contains("[Logging.Console]".ToLower()));
                        if (devmodeEn >= 0)
                        {
                            var i = contents.FindIndex(devmodeEn, s => s.StartsWith("Enabled = true"));
                            var n = contents.FindIndex(devmodeEn, s => s.StartsWith("[Logging.Disk]"));
                            if (i < n) return true;
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("Something went wrong: " + e);
                    }
                }
                else
                {
                    return null;
                }

                return false;
            }
            set
            {
                if (value == null) return;

                var ud = Path.Combine(GameRootDirectory, @"BepInEx\config\BepInEx.cfg");

                try
                {
                    var contents = File.ReadAllLines(ud).ToList();

                    var setToLanguage = contents.FindIndex(s => s.ToLower().Contains("[Logging.Console]".ToLower()));
                    if (setToLanguage >= 0 && value.Value)
                    {
                        var i = contents.FindIndex(setToLanguage, s => s.StartsWith("Enabled"));
                        if (i > setToLanguage)
                            contents[i] = "Enabled = true";
                    }
                    else
                    {
                        var i = contents.FindIndex(setToLanguage, s => s.StartsWith("Enabled"));
                        if (i > setToLanguage)
                            contents[i] = "Enabled = false";
                    }

                    File.WriteAllLines(ud, contents.ToArray());
                }
                catch (Exception e)
                {
                    MessageBox.Show("Something went wrong: " + e);
                }
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process([In] IntPtr hProcess, out bool lpSystemInfo);

        public static bool IsWow64()
        {
            return GetProcAddress(GetModuleHandle("Kernel32.dll"), "IsWow64Process") != IntPtr.Zero &&
                   IsWow64Process(Process.GetCurrentProcess().Handle, out var flag) && flag;
        }

        public static bool Is64BitOs()
        {
            if (IntPtr.Size == 4) return IsWow64();
            return IntPtr.Size == 8;
        }

        public static void SetLanguage(string langstring, bool alsoXua)
        {
            if (alsoXua)
                WriteLangIni(langstring);

            if (File.Exists(GameRootDirectory + _mCustomDir + _decideLang))
                File.Delete(GameRootDirectory + _mCustomDir + _decideLang);
            using (var writetext = new StreamWriter(GameRootDirectory + _mCustomDir + _decideLang))
            {
                writetext.WriteLine(langstring);
            }

            Application.Restart();
        }

        private static void WriteLangIni(string language)
        {
            var configPath = Path.Combine(GameRootDirectory, @"BepInEx/Config/AutoTranslatorConfig.ini");

            if (!File.Exists(configPath)) return;

            if (System.Windows.MessageBox.Show("Do you want the ingame language to reflect this language choice?",
                "Question", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            try
            {
                var contents = File.ReadAllLines(configPath).ToList();

                // Fix VMD for darkness
                var setToLanguage = contents.FindIndex(s => s.ToLower().Contains("[General]".ToLower()));
                if (setToLanguage >= 0)
                {
                    var i = contents.FindIndex(setToLanguage, s => s.StartsWith("Language"));
                    if (i > setToLanguage)
                        contents[i] = $"Language={language}";
                    else
                        contents.Insert(setToLanguage + 1, $"Language={language}");
                }
                else
                {
                    contents.Add("");
                    contents.Add("[General]");
                    contents.Add($"Language={language}");
                }

                File.WriteAllLines(configPath, contents.ToArray());
            }
            catch (Exception e)
            {
                MessageBox.Show("Something went wrong: " + e);
            }
        }

        public static void CheckDuplicateStartup()
        {
            var process = Process.GetCurrentProcess();
            var dupl = Process.GetProcessesByName(process.ProcessName);
            if (true)
                foreach (var p in dupl)
                    if (p.Id != process.Id)
                        p.Kill();
        }

        public static void Initialize()
        {
            var currentDirectory = Path.GetDirectoryName(typeof(MainWindow).Assembly.Location) ??
                                   Environment.CurrentDirectory;
            GameRootDirectory = currentDirectory + "\\";

            Directory.CreateDirectory(GameRootDirectory + _mCustomDir);

            BepinPluginsDir = $"{GameRootDirectory}\\BepInEx\\Plugins\\";

            // Framework test
            IsIpa = File.Exists($"{GameRootDirectory}\\IPA.exe");
            IsBepIn = Directory.Exists($"{GameRootDirectory}\\BepInEx");

            if (IsIpa && IsBepIn)
                MessageBox.Show(
                    "Both BepInEx and IPA is detected in the game folder!\n\nApplying both frameworks may lead to problems when running the game!",
                    "Warning!");


            // Updater stuffs

            KKmanExist = File.Exists(GameRootDirectory + _mCustomDir + _kkmdir);
            _updatelocExists = File.Exists(GameRootDirectory + _mCustomDir + _updateLoc);
            if (KKmanExist)
            {
                var kkmanFileStream = new FileStream(GameRootDirectory + _mCustomDir + _kkmdir, FileMode.Open,
                    FileAccess.Read);
                using (var streamReader = new StreamReader(kkmanFileStream, Encoding.UTF8))
                {
                    string line;
                    while ((line = streamReader.ReadLine()) != null) _kkman = line;
                }

                kkmanFileStream.Close();
                if (_updatelocExists)
                {
                    var updFileStream = new FileStream(GameRootDirectory + _mCustomDir + _updateLoc, FileMode.Open,
                        FileAccess.Read);
                    using (var streamReader = new StreamReader(updFileStream, Encoding.UTF8))
                    {
                        string line;
                        while ((line = streamReader.ReadLine()) != null) _updateSources = line;
                    }

                    updFileStream.Close();
                }
                else
                {
                    _updateSources = "";
                }
            }

            _langExists = File.Exists(GameRootDirectory + _mCustomDir + _decideLang);
            if (_langExists)
            {
                var verFileStream = new FileStream(GameRootDirectory + _mCustomDir + _decideLang, FileMode.Open,
                    FileAccess.Read);
                using (var streamReader = new StreamReader(verFileStream, Encoding.UTF8))
                {
                    string line;
                    while ((line = streamReader.ReadLine()) != null) Language = line;
                }

                verFileStream.Close();
            }

            if (GameRootDirectory.Length >= 75)
                MessageBox.Show(
                    "The game is installed deep in the file system!\n\nThis can cause a variety of errors, so it's recommended that you move it to a shorter path, something like:\n\nC:\\Illusion\\AI.Shoujo",
                    "Critical warning!");

            // Customization options

            var versionPath = Path.GetFullPath(GameRootDirectory + _versioningLoc);
            if (File.Exists(versionPath))
            {
                var verFileStream = new FileStream(versionPath, FileMode.Open, FileAccess.Read);
                using (var streamReader = new StreamReader(verFileStream, Encoding.UTF8))
                {
                    string line;
                    while ((line = streamReader.ReadLine()) != null) VersionString = line;
                }

                verFileStream.Close();
            }

            var warningExists = File.Exists(GameRootDirectory + _mCustomDir + _warningLoc);
            if (warningExists)
                try
                {
                    using (var sr = new StreamReader(GameRootDirectory + _mCustomDir + _warningLoc))
                    {
                        var line = sr.ReadToEnd();
                        WarningString = line;
                    }
                }
                catch (IOException e)
                {
                    WarningString = e.Message;
                }

            var charExists = File.Exists(GameRootDirectory + _mCustomDir + _charLoc);
            if (charExists)
            {
                var urich = new Uri(GameRootDirectory + _mCustomDir + _charLoc, UriKind.RelativeOrAbsolute);
                CustomCharacterImage = BitmapFrame.Create(urich);
            }

            var backgExists = File.Exists(GameRootDirectory + _mCustomDir + _backgLoc);
            if (backgExists)
            {
                var uribg = new Uri(GameRootDirectory + _mCustomDir + _backgLoc, UriKind.RelativeOrAbsolute);
                CustomBgImage = BitmapFrame.Create(uribg);
            }

            var patreonExists = File.Exists(GameRootDirectory + _mCustomDir + _patreonLoc);
            if (patreonExists)
            {
                var verFileStream = new FileStream(GameRootDirectory + _mCustomDir + _patreonLoc, FileMode.Open,
                    FileAccess.Read);
                using (var streamReader = new StreamReader(verFileStream, Encoding.UTF8))
                {
                    string line;
                    while ((line = streamReader.ReadLine()) != null) PatreonUrl = line;
                }

                verFileStream.Close();
            }
        }

        public static string GetConfigFilePath()
        {
            return GameRootDirectory + _mStrSaveDir;
        }

        public static void DisablePlugin(string enabledPath)
        {
            var disabledPath =
                enabledPath.Substring(0, enabledPath.LastIndexOf(".dll", StringComparison.OrdinalIgnoreCase)) + ".dl_";
            if (File.Exists(enabledPath))
            {
                if (File.Exists(disabledPath)) File.Delete(disabledPath);
                File.Move(enabledPath, disabledPath);
            }
        }

        public static void EnablePlugin(string enabledPath)
        {
            var disabledPath =
                enabledPath.Substring(0, enabledPath.LastIndexOf(".dll", StringComparison.OrdinalIgnoreCase)) + ".dl_";
            if (File.Exists(disabledPath))
            {
                if (File.Exists(enabledPath)) File.Delete(enabledPath);
                File.Move(disabledPath, enabledPath);
            }
        }

        public static void ShowManual(string manualRoot)
        {
            var manualEn = manualRoot + "manual_en.html";
            var manualLang = manualRoot + $"manual_{Language}.html";
            var manualJa = manualRoot + "お読み下さい.html";

            if (File.Exists(manualLang))
            {
                Process.Start(manualLang);
            }
            else if (File.Exists(manualEn))
            {
                Process.Start(manualEn);
            }
            else if (File.Exists(manualJa))
            {
                Process.Start(manualJa);
            }
            else
            {
                var x = Directory.GetFiles(manualRoot, "*.html", SearchOption.TopDirectoryOnly).FirstOrDefault();
                if (x != null)
                    Process.Start(x);
                else
                    MessageBox.Show("Manual could not be found.", "Warning!");
            }
        }

        public static void OpenDirectory(string subDirectory)
        {
            var fullPath = Path.Combine(GameRootDirectory, subDirectory ?? "");
            fullPath = Path.GetFullPath(fullPath);
            if (Directory.Exists(fullPath))
            {
                Process.Start("explorer.exe", fullPath);
                return;
            }

            MessageBox.Show("Folder could not be found, please launch the game at least once.", "Warning!");
        }

        public static void StartUpdate()
        {
            var gameRoot = GameRootDirectory.TrimEnd('\\', '/', ' ');
            var kkmanPath = _kkman.TrimEnd('\\', '/', ' ');

            var finaldir = !File.Exists($@"{kkmanPath}\StandaloneUpdater.exe")
                ? $@"{GameRootDirectory}{kkmanPath}"
                : kkmanPath;

            var text = $@"{finaldir}\StandaloneUpdater.exe";

            var argdir = $"\u0022{gameRoot}\u0022";
            var argloc = _updateSources;
            var args = $"{argdir} {argloc}";

            if (!_updatelocExists)
                args = $"{argdir}";

            if (File.Exists(text))
                Process.Start(new ProcessStartInfo(text) { WorkingDirectory = $@"{finaldir}", Arguments = args });
        }

        public static void StartGame(string gameExeRelativePath)
        {
            var exePath = Path.GetFullPath(GameRootDirectory + gameExeRelativePath);
            if (File.Exists(exePath) && IsIpa)
            {
                var ipa = "\u0022" + GameRootDirectory + "IPA.exe" + "\u0022";
                var ipaArgs = "\u0022" + exePath + "\u0022" + " --launch";
                Process.Start(new ProcessStartInfo(ipa) { WorkingDirectory = GameRootDirectory, Arguments = ipaArgs });
            }
            else if (File.Exists(exePath))
            {
                Process.Start(new ProcessStartInfo(exePath) { WorkingDirectory = GameRootDirectory });
            }
            else
            {
                MessageBox.Show("Executable can't be located", "Warning!");
                return;
            }

            System.Windows.Application.Current.MainWindow?.Close();
        }

        //todo readd?
        //private void SystemInfo_Open(object sender, RoutedEventArgs e)
        //{
        //    var text = Environment.ExpandEnvironmentVariables("%windir%") + "/System32/dxdiag.exe";
        //    if (File.Exists(text))
        //    {
        //        Process.Start(text);
        //        return;
        //    }
        //    MessageBox.Show("Folder could not be found, please launch the game at least once.", "Warning!");
        //}
    }
}