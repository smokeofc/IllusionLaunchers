﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using Microsoft.Win32;
using Application = System.Windows.Application;
using MessageBox = System.Windows.Forms.MessageBox;

namespace InitSetting
{
    public partial class MainWindow : Window
    {
        private const int MONITORINFOF_PRIMARY = 1;

        private const string m_strSaveDir = "/UserData/setup.xml";
        private const string m_customDir = "/UserData/LauncherEN";

        private const string m_strDefSizeText = "1900 x 720 (16 : 9)";
        private const int m_nDefQuality = 2;
        private const int m_nDefWidth = 1600;
        private const int m_nDefHeight = 900;
        private const int m_nDefLang = 1;
        private const bool m_bDefFullScreen = false;

        private const string decideLang = "/lang";
        private const string versioningLoc = "/version";
        private const string warningLoc = "/warning.txt";
        private const string charLoc = "/Chara.png";
        private const string backgLoc = "/LauncherBG.png";
        private const string patreonLoc = "/patreon.txt";
        private const string kkmdir = "/kkman.txt";
        private const string updateLoc = "/updater.txt";

        private const int m_nQualityCount = 3;
        private bool BackgExists;
        private bool CharExists;

        private bool is64bitOS;
        private bool isBepIn;

        private bool isIPA;
        private bool isMainGame;

        private bool isStudio;

        private string kkman;
        private bool kkmanExist;

        private string lang = "en";
        private bool LangExists;

        private string[] m_astrQuality;

        private readonly List<DisplayModes> m_listCurrentDisplay = new List<DisplayModes>();

        private readonly List<DisplayMode> m_listDefaultDisplay = new List<DisplayMode>
        {
            new DisplayMode
            {
                Width = 854,
                Height = 480,
                text = "854 x 480 (16 : 9)"
            },
            new DisplayMode
            {
                Width = 1024,
                Height = 576,
                text = "1024 x 576 (16 : 9)"
            },
            new DisplayMode
            {
                Width = 1136,
                Height = 640,
                text = "1136 x 640 (16 : 9)"
            },
            new DisplayMode
            {
                Width = 1280,
                Height = 720,
                text = "1280 x 720 (16 : 9)"
            },
            new DisplayMode
            {
                Width = 1366,
                Height = 768,
                text = "1366 x 768 (16 : 9)"
            },
            new DisplayMode
            {
                Width = 1536,
                Height = 864,
                text = "1536 x 864 (16 : 9)"
            },
            new DisplayMode
            {
                Width = 1600,
                Height = 900,
                text = "1600 x 900 (16 : 9)"
            },
            new DisplayMode
            {
                Width = 1920,
                Height = 1080,
                text = "1920 x 1080 (16 : 9)"
            },
            new DisplayMode
            {
                Width = 2048,
                Height = 1152,
                text = "2048 x 1152 (16 : 9)"
            },
            new DisplayMode
            {
                Width = 2560,
                Height = 1440,
                text = "2560 x 1440 (16 : 9)"
            },
            new DisplayMode
            {
                Width = 3200,
                Height = 1800,
                text = "3200 x 1800 (16 : 9)"
            },
            new DisplayMode
            {
                Width = 3840,
                Height = 2160,
                text = "3840 x 2160 (16 : 9)"
            }
        };

        private ConfigSetting m_Setting = new ConfigSetting();

        private readonly string m_strCurrentDir = Environment.CurrentDirectory + "\\";
        private readonly string m_strGameExe = "AI-Shoujo.exe";

        private readonly string m_strGameRegistry = "Software\\illusion\\AI-Shoujo\\AI-Shoujo\\";
        private string m_strManualDir = "/manual/お読み下さい.html";
        private readonly string m_strStudioExe = "StudioNEOV2.exe";
        private string m_strStudioManualDir = "/manual_s/お読み下さい.html";
        private readonly string m_strStudioRegistry = "Software\\illusion\\AI-Syoujyo\\StudioNEOV2";
        private string m_strVRManualDir = "/manual_vr/お読み下さい.html";


        private Mutex mutex;
        private readonly bool noTL = false;

        private bool PatreonExists;
        //string updateURL;
        //string packVersion;
        //string newPackVersion;

        private string patreonURL;
        private string q_normal = "Normal";

        private string q_performance = "Performance";
        private string q_quality = "Quality";
        private string[] s_EnglishTL;
        private string s_primarydisplay = "PrimaryDisplay";
        private string s_subdisplay = "SubDisplay";
        private bool startup;
        private string updated = "placeholder";
        private bool updatelocExists;

        private bool versionAvail;
        private bool WarningExists;

        public MainWindow()
        {
            InitializeComponent();
        }

        bool _shown;
        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            if (_shown)
                return;

            _shown = true;

            try
            {
                InitializeFunctions();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Crash on start", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeFunctions()
        {
            //if (!DoubleStartCheck())
            //{
            //    System.Windows.Application.Current.MainWindow.Close();
            //    return;
            //}

            // Check for duplicate launches
            var process = Process.GetCurrentProcess();
            var dupl = Process.GetProcessesByName(process.ProcessName);
            if (true)
                foreach (var p in dupl)
                    if (p.Id != process.Id)
                        p.Kill();

            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            startup = true;

            //temp hide unimplemented stuffs
            CustomRes.Visibility = Visibility.Hidden;

            Directory.CreateDirectory(m_strCurrentDir + m_customDir);

            // Framework test
            isIPA = File.Exists($"{m_strCurrentDir}\\IPA.exe");
            isBepIn = Directory.Exists($"{m_strCurrentDir}\\BepInEx");

            if (isIPA && isBepIn)
                MessageBox.Show(
                    "Both BepInEx and IPA is detected in the game folder!\n\nApplying both frameworks may lead to problems when running the game!",
                    "Warning!");

            // Check if console is active

            if (File.Exists(m_strCurrentDir + "/Bepinex/config/BepInEx.cfg"))
            {
                var ud = Path.Combine(m_strCurrentDir, @"BepInEx\config\BepInEx.cfg");

                try
                {
                    var contents = File.ReadAllLines(ud).ToList();

                    var devmodeEN = contents.FindIndex(s => s.ToLower().Contains("[Logging.Console]".ToLower()));
                    if (devmodeEN >= 0)
                    {
                        var i = contents.FindIndex(devmodeEN, s => s.StartsWith("Enabled = true"));
                        var n = contents.FindIndex(devmodeEN, s => s.StartsWith("[Logging.Disk]"));
                        if (i < n) toggleConsole.IsChecked = true;
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("Something went wrong: " + e);
                }
            }
            else
            {
                toggleConsole.IsEnabled = false;
            }

            // Updater stuffs

            kkmanExist = File.Exists(m_strCurrentDir + m_customDir + kkmdir);
            updatelocExists = File.Exists(m_strCurrentDir + m_customDir + updateLoc);
            if (kkmanExist)
            {
                var kkmanFileStream = new FileStream(m_strCurrentDir + m_customDir + kkmdir, FileMode.Open,
                    FileAccess.Read);
                using (var streamReader = new StreamReader(kkmanFileStream, Encoding.UTF8))
                {
                    string line;
                    while ((line = streamReader.ReadLine()) != null) kkman = line;
                }

                kkmanFileStream.Close();
                if (updatelocExists)
                {
                    var updFileStream = new FileStream(m_strCurrentDir + m_customDir + updateLoc, FileMode.Open,
                        FileAccess.Read);
                    using (var streamReader = new StreamReader(updFileStream, Encoding.UTF8))
                    {
                        string line;
                        while ((line = streamReader.ReadLine()) != null) updated = line;
                    }

                    updFileStream.Close();
                }
                else
                {
                    updated = "";
                }
            }
            else
            {
                gridUpdate.Visibility = Visibility.Hidden;
            }

            if (!File.Exists(m_strCurrentDir + m_customDir + kkmdir))
            {
            }

            // Mod settings

            if (File.Exists($"{m_strCurrentDir}\\BepInEx\\Plugins\\DHH_AI4.dll")) toggleDHH.IsChecked = true;

            if (!File.Exists($"{m_strCurrentDir}\\BepInEx\\Plugins\\DHH_AI4.dl_") &&
                !File.Exists($"{m_strCurrentDir}\\BepInEx\\Plugins\\DHH_AI4.dll"))
                toggleDHH.IsEnabled = false;

            if (File.Exists($"{m_strCurrentDir}\\BepInEx\\Plugins\\AIGraphics\\AI_Graphics.dll"))
                toggleAIGraphics.IsChecked = true;

            if (!File.Exists($"{m_strCurrentDir}\\BepInEx\\Plugins\\AIGraphics\\AI_Graphics.dl_") &&
                !File.Exists($"{m_strCurrentDir}\\BepInEx\\Plugins\\AIGraphics\\AI_Graphics.dll"))
                toggleAIGraphics.IsEnabled = false;

            if (File.Exists($"{m_strCurrentDir}\\BepInEx\\Plugins\\AIGraphics\\AI_Graphics.dll") &&
                File.Exists($"{m_strCurrentDir}\\BepInEx\\Plugins\\DHH_AI4.dll"))
            {
                toggleDHH.IsChecked = false;
                toggleAIGraphics.IsChecked = false;
            }


            startup = false;

            LangExists = File.Exists(m_strCurrentDir + m_customDir + decideLang);
            if (LangExists)
            {
                var verFileStream = new FileStream(m_strCurrentDir + m_customDir + decideLang, FileMode.Open,
                    FileAccess.Read);
                using (var streamReader = new StreamReader(verFileStream, Encoding.UTF8))
                {
                    string line;
                    while ((line = streamReader.ReadLine()) != null) lang = line;
                }

                verFileStream.Close();
            }

            labelTranslated.Visibility = Visibility.Hidden;
            labelTranslatedBorder.Visibility = Visibility.Hidden;

            // MessageBox.Show($"Chinese is {chnActive}", "Debug");

            // Template for new translations
            //if (lang == "en-US")
            //{
            //    MainWindow.Title = "PH Launcher";
            //    warnBox.Header = "Notice!";
            //    warningText.Text = "This game is intended for adult audiences, no person under the age of 18 (or equivalent according to local law) are supposed to play or be in possession of this game.\n\nThis game contains content of a sexual nature, and some of the actions depicted within may be illegal to replicate in real life. Aka, it's all fun and games in the game, let's keep it that way shall we? (~.~)v";
            //    GameFBox.Header = "Game folders";
            //    buttonInst.Content = "Install";
            //    buttonFemaleCard.Content = "Character Cards";
            //    buttonScenes.Content = "Scenes";
            //    buttonScreenshot.Content = "ScreenShots";
            //    AISHousingDirectory.Content = "Hus";
            //    GameSBox.Header = "Game Startup";
            //    labelStart.Content = "Start PH";
            //    labelM.Content = "PH Manual";
            //    labelStartS.Content = "Start Studio";
            //    labelMS.Content = "Studio Manual";
            //    labelStartVR.Content = "Start PH VR";
            //    labelMV.Content = "VR Manual";
            //    SettingsBox.Header = "Settings";
            //    toggleFullscreen.Content = "Run Game in Fullscreen";
            //    modeDev.Content = "Developer Mode";
            //    SystemInfo.Content = "System Info";
            //    buttonClose.Content = "Exit";
            //    labelDist.Content = "Unknown Install Method";
            //    labelTranslated.Content = "Launcher translated by: <Insert Name>";
            //    translationString = "Do you want to restore Japanese language in-game?";
            //    q_performance = "Performance";
            //    q_normal = "Normal";
            //    q_quality = "Quality";
            //    s_primarydisplay = "PrimaryDisplay";
            //    s_subdisplay = "SubDisplay";
            //}

            // Translations
            if (lang == "ja")
            {
                labelTranslated.Visibility = Visibility.Visible;
                labelTranslatedBorder.Visibility = Visibility.Visible;

                m_strManualDir = "/manual/Japanese/README.html";
                m_strStudioManualDir = "/manual_s/お読み下さい.html";
                m_strVRManualDir = "/manual_v/Japanese/README.html";

                warningText.Text =
                    "このゲームは成人向けので、18歳未満（または地域の法律によりと同等の年齢）がこのゲームをプレイまたは所有しているができない。\n\nこのゲームには性的内容の内容が含まれます。内に描かれている行動は、実生活で複製することは違法です。つまり、これは面白いゲームです、そうしましょう？(~.~)v";
                buttonInst.Content = "インストール";
                buttonFemaleCard.Content = "キャラカード (女性)";
                buttonMaleCard.Content = "キャラカード (男性)";
                buttonScenes.Content = "シーン";
                buttonScreenshot.Content = "SS";
                buttonUserData.Content = "UserData";
                labelStart.Content = "ゲーム開始";
                labelStartS.Content = "スタジオ開始";
                labelM.Content = "ゲーム";
                labelMS.Content = "スタジオ";
                toggleFullscreen.Content = "全画面表示";
                toggleConsole.Content = "コンソールを有効にする";
                labelDist.Content = "不明バージョン";
                labelTranslated.Content = "初期設定翻訳者: Earthship";
                q_performance = "パフォーマンス";
                q_normal = "ノーマル";
                q_quality = "クオリティ";
                s_primarydisplay = "メインディスプレイ";
                s_subdisplay = "サブディスプレイ";
                labelDiscord.Content = "Discordを訪問";
                labelPatreon.Content = "Patreonを訪問";
                labelUpdate.Content = "ゲームを更新する";

                // AIS Exclusive
                buttonHousing.Content = "家";
                toggleDHH.Content = "DHHを有効にする";
                toggleAIGraphics.Content = "AIGraphicsを有効にする";
            }
            else if (lang == "zh-CN") // By @Madevil#1103 & @𝐄𝐀𝐑𝐓𝐇𝐒𝐇𝐈𝐏 💖#4313 
            {
                labelTranslated.Visibility = Visibility.Visible;
                labelTranslatedBorder.Visibility = Visibility.Visible;

                m_strManualDir = "/manual/Traditional Chinese/README.html";
                m_strStudioManualDir = "/manual_s/お読み下さい.html";
                m_strVRManualDir = "/manual_v/Traditional Chinese/README.html";

                warningText.Text =
                    "此游戏适用于成人用户，任何未满18岁的人（或根据当地法律规定的同等人）都不得遊玩或拥有此游戏。\n\n这个游戏包含性相关的内容，某些行为在现实生活中可能是非法的。所以，游戏中的所有乐趣请保留在游戏中，让我们保持这种方式吧? (~.~)v";
                buttonInst.Content = "游戏主目录";
                buttonFemaleCard.Content = "人物卡 (女)";
                buttonMaleCard.Content = "人物卡 (男)";
                buttonScenes.Content = "工作室场景";
                buttonScreenshot.Content = "截图";
                buttonUserData.Content = "UserData";
                labelStart.Content = "开始游戏";
                labelStartS.Content = "开始工作室";
                labelM.Content = "游戏手册";
                labelMS.Content = "工作室手册";
                toggleFullscreen.Content = "全屏执行";
                toggleConsole.Content = "激活控制台";
                labelDist.Content = "未知版本";
                labelTranslated.Content = "翻译： Madevil & Earthship";
                q_performance = "性能";
                q_normal = "标准";
                q_quality = "高画质";
                s_primarydisplay = "主显示器";
                s_subdisplay = "次显示器";
                labelDiscord.Content = "前往Discord";
                labelPatreon.Content = "前往Patreon";
                labelUpdate.Content = "更新游戏";

                // AIS Exclusive
                buttonHousing.Content = "房子";
                toggleDHH.Content = "激活DHH";
                toggleAIGraphics.Content = "激活AIGraphics";
            }
            else if (lang == "zh-TW") // By @𝐄𝐀𝐑𝐓𝐇𝐒𝐇𝐈𝐏 💖#4313 
            {
                labelTranslated.Visibility = Visibility.Visible;
                labelTranslatedBorder.Visibility = Visibility.Visible;

                m_strManualDir = "/manual/Simplified Chinese/README.html";
                m_strStudioManualDir = "/manual_s/お読み下さい.html";
                m_strVRManualDir = "/manual_v/Simplified Chinese/README.html";

                warningText.Text =
                    "此遊戲適用於成人用戶，任何未滿18歲的人（或根據當地法律規定的同等人）都不得遊玩或擁有此遊戲。\n\n這個遊戲包含性相關的內容，某些行為在現實生活中可能是非法的。所以，遊戲中的所有樂趣請保留在遊戲中，讓我們保持這種方式吧? (~.~)v";
                buttonInst.Content = "遊戲主目錄";
                buttonFemaleCard.Content = "人物卡 (女)";
                buttonMaleCard.Content = "人物卡 (男)";
                buttonScenes.Content = "工作室場景";
                buttonScreenshot.Content = "截圖";
                buttonUserData.Content = "UserData";
                labelStart.Content = "開始遊戲";
                labelStartS.Content = "開始工作室";
                labelM.Content = "遊戲手冊";
                labelMS.Content = "工作室手冊";
                toggleFullscreen.Content = "全螢幕執行";
                toggleConsole.Content = "啟動控制台";
                labelDist.Content = "未知版本";
                labelTranslated.Content = "翻譯： Earthship";
                q_performance = "性能";
                q_normal = "標準";
                q_quality = "高畫質";
                s_primarydisplay = "主顯示器";
                s_subdisplay = "次顯示器";
                labelDiscord.Content = "前往Discord";
                labelPatreon.Content = "前往Patreon";
                labelUpdate.Content = "更新遊戲";

                // AIS Exclusive
                buttonHousing.Content = "房子";
                toggleDHH.Content = "啟動DHH";
                toggleAIGraphics.Content = "啟動AIGraphics";
            }

            m_astrQuality = new[]
            {
                q_performance,
                q_normal,
                q_quality
            };

            // Do checks

            is64bitOS = Is64BitOS();
            isStudio = File.Exists(m_strCurrentDir + m_strStudioExe);
            isMainGame = File.Exists(m_strCurrentDir + m_strGameExe);

            if (m_strCurrentDir.Length >= 75)
                MessageBox.Show(
                    "The game is installed deep in the file system!\n\nThis can cause a variety of errors, so it's recommended that you move it to a shorter path, something like:\n\nC:\\Illusion\\AI.Shoujo",
                    "Critical warning!");


            // Customization options

            CharExists = File.Exists(m_strCurrentDir + m_customDir + charLoc);
            BackgExists = File.Exists(m_strCurrentDir + m_customDir + backgLoc);
            WarningExists = File.Exists(m_strCurrentDir + m_customDir + warningLoc);
            PatreonExists = File.Exists(m_strCurrentDir + m_customDir + patreonLoc);

            // Launcher Customization: Grabbing versioning of install method

            versionAvail = File.Exists(m_strCurrentDir + "version");
            if (versionAvail)
            {
                var verFileStream = new FileStream(m_strCurrentDir + "version", FileMode.Open, FileAccess.Read);
                using (var streamReader = new StreamReader(verFileStream, Encoding.UTF8))
                {
                    string line;
                    while ((line = streamReader.ReadLine()) != null) labelDist.Content = line;
                }

                verFileStream.Close();
            }

            // Launcher Customization: Defining Warning, background and character

            if (WarningExists)
            {
                var verFileStream = new FileStream(m_strCurrentDir + m_customDir + warningLoc, FileMode.Open,
                    FileAccess.Read);
                try
                {
                    using (var sr = new StreamReader(m_strCurrentDir + m_customDir + warningLoc))
                    {
                        var line = sr.ReadToEnd();
                        warningText.Text = line;
                    }
                }
                catch (IOException e)
                {
                    warningText.Text = e.Message;
                }
            }

            if (CharExists)
            {
                var urich = new Uri(m_strCurrentDir + m_customDir + charLoc, UriKind.RelativeOrAbsolute);
                PackChara.Source = BitmapFrame.Create(urich);
            }

            if (BackgExists)
            {
                var uribg = new Uri(m_strCurrentDir + m_customDir + backgLoc, UriKind.RelativeOrAbsolute);
                appBG.ImageSource = BitmapFrame.Create(uribg);
            }

            if (PatreonExists)
            {
                var verFileStream = new FileStream(m_strCurrentDir + m_customDir + patreonLoc, FileMode.Open,
                    FileAccess.Read);
                using (var streamReader = new StreamReader(verFileStream, Encoding.UTF8))
                {
                    string line;
                    while ((line = streamReader.ReadLine()) != null) patreonURL = line;
                }

                verFileStream.Close();
            }
            else
            {
                linkPatreon.Visibility = Visibility.Collapsed;
                patreonBorder.Visibility = Visibility.Collapsed;
                patreonIMG.Visibility = Visibility.Collapsed;
            }

            var num = Screen.AllScreens.Length;
            getDisplayMode_EnumDisplaySettings(num);
            m_Setting.Size = "1600 x 900 (16 : 9)";
            m_Setting.Width = 1600;
            m_Setting.Height = 900;
            m_Setting.Quality = 2;
            m_Setting.Language = 1;
            m_Setting.Display = 0;
            m_Setting.FullScreen = false;
            if (lang == "ja")
                m_Setting.Language = 0;
            if (lang == "en")
                m_Setting.Language = 1;
            if (lang == "zh-CN")
                m_Setting.Language = 2;
            if (num == 2)
            {
                dropDisplay.Items.Add(s_primarydisplay);
                dropDisplay.Items.Add($"{s_subdisplay} : 1");
            }
            else
            {
                for (var i = 0; i < num; i++)
                {
                    var newItem = i == 0 ? s_primarydisplay : $"{s_subdisplay} : " + i;
                    dropDisplay.Items.Add(newItem);
                }
            }

            foreach (var newItem2 in m_astrQuality) dropQual.Items.Add(newItem2);

            SetEnableAndVisible();

            var path = m_strCurrentDir + m_strSaveDir;
            CheckConfigFile:
            if (File.Exists(path))
            {
                try
                {
                    using (var fileStream = new FileStream(path, FileMode.Open))
                    {
                        var xmlSerializer = new XmlSerializer(typeof(ConfigSetting));
                        m_Setting = (ConfigSetting)xmlSerializer.Deserialize(fileStream);
                    }

                    m_Setting.Display = Math.Min(m_Setting.Display, num - 1);
                    setDisplayComboBox(m_Setting.FullScreen);
                    var flag = false;
                    for (var k = 0; k < dropRes.Items.Count; k++)
                        if (dropRes.Items[k].ToString() == m_Setting.Size)
                            flag = true;

                    dropRes.Text = flag ? m_Setting.Size : "1280 x 720 (16 : 9)";
                    toggleFullscreen.IsChecked = m_Setting.FullScreen;
                    dropQual.Text = m_astrQuality[m_Setting.Quality];
                    var text = m_Setting.Display == 0
                        ? s_primarydisplay
                        : $"{s_subdisplay} : " + m_Setting.Display;
                    if (num == 2)
                        text = new[]
                        {
                            s_primarydisplay,
                            $"{s_subdisplay} : 1"
                        }[m_Setting.Display];

                    if (dropDisplay.Items.Contains(text))
                    {
                        dropDisplay.Text = text;
                    }
                    else
                    {
                        dropDisplay.Text = s_primarydisplay;
                        m_Setting.Display = 0;
                        m_Setting.Language = 1;
                        m_Setting.Size = "1600 x 900 (16 : 9)";
                        m_Setting.Quality = 2;
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("/UserData/setup.xml file was corrupted, settings will be reset.");
                    File.Delete(path);
                    goto CheckConfigFile;
                }
            }
            else
            {
                setDisplayComboBox(false);
                dropRes.Text = m_Setting.Size;
                dropQual.Text = m_astrQuality[m_Setting.Quality];
                dropDisplay.Text = s_primarydisplay;
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process([In] IntPtr hProcess, out bool lpSystemInfo);

        [DllImport("user32.dll")]
        private static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice,
            uint dwFlags);

        private void SetEnableAndVisible()
        {
        }

        private void SaveRegistry()
        {
            using (var registryKey = Registry.CurrentUser.CreateSubKey(m_strGameRegistry))
            {
                registryKey.SetValue("Screenmanager Is Fullscreen mode_h3981298716", m_Setting.FullScreen ? 1 : 0);
                registryKey.SetValue("Screenmanager Resolution Height_h2627697771", m_Setting.Height);
                registryKey.SetValue("Screenmanager Resolution Width_h182942802", m_Setting.Width);
                registryKey.SetValue("UnityGraphicsQuality_h1669003810", 2);
                registryKey.SetValue("UnitySelectMonitor_h17969598", m_Setting.Display);
            }

            if (isStudio)
                using (var registryKey2 = Registry.CurrentUser.CreateSubKey(m_strStudioRegistry))
                {
                    registryKey2.SetValue("Screenmanager Is Fullscreen mode_h3981298716",
                        m_Setting.FullScreen ? 1 : 0);
                    registryKey2.SetValue("Screenmanager Resolution Height_h2627697771", m_Setting.Height);
                    registryKey2.SetValue("Screenmanager Resolution Width_h182942802", m_Setting.Width);
                    registryKey2.SetValue("UnityGraphicsQuality_h1669003810", 2);
                    registryKey2.SetValue("UnitySelectMonitor_h17969598", m_Setting.Display);
                }
        }

        private void PlayFunc(string strExe)
        {
            saveConfigFile(m_strCurrentDir + m_strSaveDir);
            SaveRegistry();
            var text = m_strCurrentDir + strExe;
            var ipa = "\u0022" + m_strCurrentDir + "IPA.exe" + "\u0022";
            var ipaArgs = "\u0022" + text + "\u0022" + " --launch";
            if (File.Exists(text) && isIPA)
            {
                Process.Start(new ProcessStartInfo(ipa) { WorkingDirectory = m_strCurrentDir, Arguments = ipaArgs });
                Application.Current.MainWindow.Close();
                return;
            }

            if (File.Exists(text))
            {
                Process.Start(new ProcessStartInfo(text) { WorkingDirectory = m_strCurrentDir });
                Application.Current.MainWindow.Close();
                return;
            }

            MessageBox.Show("Executable can't be located", "Warning!");
        }

        private void buttonStart_Click(object sender, RoutedEventArgs e)
        {
            PlayFunc(m_strGameExe);
        }

        private void buttonStartS_Click(object sender, RoutedEventArgs e)
        {
            PlayFunc(m_strStudioExe);
        }

        private void buttonClose_Click(object sender, RoutedEventArgs e)
        {
            saveConfigFile(m_strCurrentDir + m_strSaveDir);
            ReleaseMutex();
            Application.Current.MainWindow.Close();
        }

        private void Resolution_Change(object sender, SelectionChangedEventArgs e)
        {
            if (-1 == dropRes.SelectedIndex) return;
            var comboBoxCustomItem = (ComboBoxCustomItem)dropRes.SelectedItem;
            m_Setting.Size = comboBoxCustomItem.text;
            m_Setting.Width = comboBoxCustomItem.width;
            m_Setting.Height = comboBoxCustomItem.height;
        }

        private void Quality_Change(object sender, SelectionChangedEventArgs e)
        {
            var a = dropQual.SelectedItem.ToString();
            if (a == q_performance)
            {
                m_Setting.Quality = 0;
                return;
            }

            if (a == q_normal)
            {
                m_Setting.Quality = 1;
                return;
            }

            if (!(a == q_quality)) return;
            m_Setting.Quality = 2;
        }

        private void windowUnChecked(object sender, RoutedEventArgs e)
        {
            setDisplayComboBox(false);
            dropRes.Text = m_Setting.Size;
            m_Setting.FullScreen = false;
        }

        private void windowChecked(object sender, RoutedEventArgs e)
        {
            setDisplayComboBox(true);
            m_Setting.FullScreen = true;
            setFullScreenDevice();
        }

        private void buttonManual_Click(object sender, RoutedEventArgs e)
        {
            var manualEN = $"{m_strCurrentDir}\\manual\\manual_en.html";
            var manualLANG = $"{m_strCurrentDir}\\manual\\manual_{lang}.html";
            var manualJA = m_strCurrentDir + m_strManualDir;

            if (File.Exists(manualEN) || File.Exists(manualLANG) || File.Exists(manualJA))
            {
                if (File.Exists(manualLANG))
                    Process.Start(manualLANG);
                else if (File.Exists(manualEN))
                    Process.Start(manualEN);
                else
                    Process.Start(manualJA);
                return;
            }

            MessageBox.Show("Manual could not be found.", "Warning!");
        }

        private void buttonManualS_Click(object sender, RoutedEventArgs e)
        {
            var manualEN = $"{m_strCurrentDir}\\manual_s\\manual_en.html";
            var manualLANG = $"{m_strCurrentDir}\\manual_s\\manual_{lang}.html";
            var manualJA = m_strCurrentDir + m_strStudioManualDir;

            if (File.Exists(manualEN) || File.Exists(manualLANG) || File.Exists(manualJA))
            {
                if (File.Exists(manualLANG))
                    Process.Start(manualLANG);
                else if (File.Exists(manualEN))
                    Process.Start(manualEN);
                else
                    Process.Start(manualJA);
                return;
            }

            MessageBox.Show("Manual could not be found.", "Warning!");
        }

        private void buttonManualV_Click(object sender, RoutedEventArgs e)
        {
            var manualEN = $"{m_strCurrentDir}\\manual_vr\\manual_en.html";
            var manualLANG = $"{m_strCurrentDir}\\manual_vr\\manual_{lang}.html";
            var manualJA = m_strCurrentDir + m_strVRManualDir;

            if (File.Exists(manualEN) || File.Exists(manualLANG) || File.Exists(manualJA))
            {
                if (File.Exists(manualLANG))
                    Process.Start(manualLANG);
                else if (File.Exists(manualEN))
                    Process.Start(manualEN);
                else
                    Process.Start(manualJA);
                return;
            }

            MessageBox.Show("Manual could not be found.", "Warning!");
        }

        private void Display_Change(object sender, SelectionChangedEventArgs e)
        {
            if (-1 == dropDisplay.SelectedIndex) return;
            m_Setting.Display = dropDisplay.SelectedIndex;
            if (m_Setting.FullScreen)
            {
                setDisplayComboBox(true);
                setFullScreenDevice();
            }
        }

        private void buttonInst_Click(object sender, RoutedEventArgs e)
        {
            char[] trimChars =
            {
                '/'
            };
            char[] trimChars2 =
            {
                '\\'
            };
            var text = m_strCurrentDir.TrimEnd(trimChars);
            text = text.TrimEnd(trimChars2);
            if (Directory.Exists(text))
            {
                Process.Start("explorer.exe", text);
                return;
            }

            MessageBox.Show("Folder could not be found, please launch the game at least once.", "Warning!");
        }

        private void buttonScenes_Click(object sender, RoutedEventArgs e)
        {
            char[] trimChars =
            {
                '/'
            };
            char[] trimChars2 =
            {
                '\\'
            };
            var text = m_strCurrentDir.TrimEnd(trimChars);
            text = text.TrimEnd(trimChars2) + "\\UserData\\Studio\\scene";
            if (Directory.Exists(text))
            {
                Process.Start("explorer.exe", text);
                return;
            }

            MessageBox.Show("Folder could not be found, please launch the game at least once.", "Warning!");
        }

        private void buttonUserData_Click(object sender, RoutedEventArgs e)
        {
            char[] trimChars =
            {
                '/'
            };
            char[] trimChars2 =
            {
                '\\'
            };
            var text = m_strCurrentDir.TrimEnd(trimChars);
            text = text.TrimEnd(trimChars2) + "\\UserData";
            if (Directory.Exists(text))
            {
                Process.Start("explorer.exe", text);
                return;
            }

            MessageBox.Show("Folder could not be found, please launch the game at least once.", "Warning!");
        }

        private void buttonHousing_Click(object sender, RoutedEventArgs e)
        {
            char[] trimChars =
            {
                '/'
            };
            char[] trimChars2 =
            {
                '\\'
            };
            var text = m_strCurrentDir.TrimEnd(trimChars);
            text = text.TrimEnd(trimChars2) + "\\UserData\\housing";
            if (Directory.Exists(text))
            {
                Process.Start("explorer.exe", text);
                return;
            }

            MessageBox.Show("Folder could not be found, please launch the game at least once.", "Warning!");
        }

        private void buttonScreenshot_Click(object sender, RoutedEventArgs e)
        {
            char[] trimChars =
            {
                '/'
            };
            char[] trimChars2 =
            {
                '\\'
            };
            var text = m_strCurrentDir.TrimEnd(trimChars);
            text = text.TrimEnd(trimChars2) + "\\UserData\\cap";
            if (Directory.Exists(text))
            {
                Process.Start("explorer.exe", text);
                return;
            }

            MessageBox.Show("Folder could not be found, please launch the game at least once.", "Warning!");
        }

        private void buttonFemaleCard_Click(object sender, RoutedEventArgs e)
        {
            char[] trimChars =
            {
                '/'
            };
            char[] trimChars2 =
            {
                '\\'
            };
            var text = m_strCurrentDir.TrimEnd(trimChars);
            text = text.TrimEnd(trimChars2) + "\\UserData\\chara\\female";
            if (Directory.Exists(text))
            {
                Process.Start("explorer.exe", text);
                return;
            }

            MessageBox.Show("Folder could not be found, please launch the game at least once.", "Warning!");
        }

        private void buttonMaleCard_Click(object sender, RoutedEventArgs e)
        {
            char[] trimChars =
            {
                '/'
            };
            char[] trimChars2 =
            {
                '\\'
            };
            var text = m_strCurrentDir.TrimEnd(trimChars);
            text = text.TrimEnd(trimChars2) + "\\UserData\\chara\\male";
            if (Directory.Exists(text))
            {
                Process.Start("explorer.exe", text);
                return;
            }

            MessageBox.Show("Folder could not be found, please launch the game at least once.", "Warning!");
        }

        private void SystemInfo_Open(object sender, RoutedEventArgs e)
        {
            var text = Environment.ExpandEnvironmentVariables("%windir%") + "/System32/dxdiag.exe";
            if (File.Exists(text))
            {
                Process.Start(text);
                return;
            }

            MessageBox.Show("Folder could not be found, please launch the game at least once.", "Warning!");
        }

        private bool DoubleStartCheck()
        {
            bool flag;
            mutex = new Mutex(true, "AIS", out flag);
            var v = !flag;
            if (v)
            {
                if (mutex != null) mutex.Close();
                mutex = null;
                return false;
            }

            return true;
        }

        private bool ReleaseMutex()
        {
            if (mutex == null) return false;
            mutex.ReleaseMutex();
            mutex.Close();
            mutex = null;
            return true;
        }

        private void setDisplayComboBox(bool _bFullScreen)
        {
            dropRes.Items.Clear();
            var nDisplay = m_Setting.Display;
            foreach (var displayMode in _bFullScreen ? m_listCurrentDisplay[nDisplay].list : m_listDefaultDisplay)
            {
                var newItem = new ComboBoxCustomItem
                {
                    text = displayMode.text,
                    width = displayMode.Width,
                    height = displayMode.Height
                };
                dropRes.Items.Add(newItem);
            }
        }

        private void saveConfigFile(string _strFilePath)
        {
            if (!Directory.Exists(Path.GetDirectoryName(_strFilePath))) return;
            FileStream fileStream = null;
            try
            {
                fileStream = new FileStream(_strFilePath, FileMode.Create);
                if (fileStream != null)
                    using (var streamWriter = new StreamWriter(fileStream, Encoding.GetEncoding("UTF-16")))
                    {
                        var xmlSerializerNamespaces = new XmlSerializerNamespaces();
                        xmlSerializerNamespaces.Add(string.Empty, string.Empty);
                        new XmlSerializer(typeof(ConfigSetting)).Serialize(streamWriter, m_Setting,
                            xmlSerializerNamespaces);
                        fileStream = null;
                    }
            }
            finally
            {
                if (fileStream != null) fileStream.Dispose();
            }
        }

        private void getDisplayMode_CIM_VideoControllerResolution()
        {
            var instances = new ManagementClass("CIM_VideoControllerResolution").GetInstances();
            var list = new List<DisplayMode>();
            var num = 0u;
            var num2 = 0u;
            foreach (var managementBaseObject in instances)
            {
                var managementObject = (ManagementObject)managementBaseObject;
                var nXX = (uint)managementObject["HorizontalResolution"];
                var nYY = (uint)managementObject["VerticalResolution"];
                if ((num != nXX || num2 != nYY) && (ulong)managementObject["NumberOfColors"] == 4294967296UL)
                {
                    var displayMode = m_listDefaultDisplay.Find(i =>
                        (long)i.Width == (long)(ulong)nXX && (long)i.Height == (long)(ulong)nYY);
                    if (displayMode.Width != 0) list.Add(displayMode);
                    num = nXX;
                    num2 = nYY;
                }
            }

            var item = default(DisplayModes);
            item.list = list;
            m_listCurrentDisplay.Add(item);
            if (instances.Count == 0)
            {
                MessageBox.Show("Failed to list screens");
                return;
            }

            if (m_listCurrentDisplay.Count == 0) MessageBox.Show("Failed to list supported resolutions");
        }

        private void getDisplayMode_EnumDisplaySettings(int numDisplay)
        {
            var display_DEVICE = default(DISPLAY_DEVICE);
            display_DEVICE.cb = Marshal.SizeOf(display_DEVICE);
            var allDisplayNames = new List<string>();
            var dispNum = 0u;
            while (EnumDisplayDevices(null, dispNum, ref display_DEVICE, 1u))
            {
                if ((display_DEVICE.StateFlags & DisplayDeviceStateFlags.AttachedToDesktop) ==
                    DisplayDeviceStateFlags.AttachedToDesktop) allDisplayNames.Add(display_DEVICE.DeviceName);
                dispNum += 1u;
            }

            var primaryIndex = -1;
            for (var currentDisp = 0; currentDisp < allDisplayNames.Count; currentDisp++)
            {
                var displayName = allDisplayNames[currentDisp];
                var num4 = 0;
                var num5 = 0;
                var devmode = default(DEVMODE);
                var list2 = new List<DisplayMode>();
                var num6 = 0;
                while (EnumDisplaySettings(displayName, num6, ref devmode))
                {
                    var nXX = devmode.dmPelsWidth;
                    var nYY = devmode.dmPelsHeight;
                    if ((num4 != nXX || num5 != nYY) && devmode.dmBitsPerPel == 32)
                    {
                        var displayMode = m_listDefaultDisplay.Find(dis => dis.Width == nXX && dis.Height == nYY);
                        if (displayMode.Width != 0) list2.Add(displayMode);
                        num4 = nXX;
                        num5 = nYY;
                    }

                    num6++;
                }

                var item = default(DisplayModes);
                foreach (var monitorInfoEx in Screen.AllScreens)
                    if (monitorInfoEx.DeviceName == displayName)
                    {
                        item.x = monitorInfoEx.WorkingArea.Left;
                        item.y = monitorInfoEx.WorkingArea.Top;
                        if (monitorInfoEx.Primary) primaryIndex = currentDisp;
                    }

                item.list = list2;
                m_listCurrentDisplay.Add(item);
            }

            if (m_listCurrentDisplay.Count == 0 || m_listCurrentDisplay.Count != numDisplay)
                MessageBox.Show("Failed to list supported resolutions");

            if (primaryIndex < 0) return;
            m_listCurrentDisplay.Insert(0, m_listCurrentDisplay[primaryIndex]);
            m_listCurrentDisplay.RemoveAt(primaryIndex + 1);
        }

        private static int DisplaySort(DisplayModes a, DisplayModes b)
        {
            if (a.x < b.x) return -1;
            if (a.x > b.x) return 1;
            if (a.y < b.y) return -1;
            if (a.y > b.y) return 1;
            return 0;
        }

        private void setFullScreenDevice()
        {
            var nDisplay = m_Setting.Display;
            if (m_listCurrentDisplay[nDisplay].list.Count == 0)
            {
                m_Setting.FullScreen = false;
                toggleFullscreen.IsChecked = false;
                MessageBox.Show("This monitor doesn't support fullscreen.");
                return;
            }

            if (m_listCurrentDisplay[nDisplay].list.Find(x => x.text.Contains(m_Setting.Size)).Width == 0)
            {
                m_Setting.Size = m_listCurrentDisplay[nDisplay].list[0].text;
                m_Setting.Width = m_listCurrentDisplay[nDisplay].list[0].Width;
                m_Setting.Height = m_listCurrentDisplay[nDisplay].list[0].Height;
            }

            dropRes.Text = m_Setting.Size;
        }

        public bool IsWow64()
        {
            bool flag;
            return GetProcAddress(GetModuleHandle("Kernel32.dll"), "IsWow64Process") != IntPtr.Zero &&
                   IsWow64Process(Process.GetCurrentProcess().Handle, out flag) && flag;
        }

        public bool Is64BitOS()
        {
            if (IntPtr.Size == 4) return IsWow64();
            return IntPtr.Size == 8;
        }

        private void MenuCloseButton(object sender, EventArgs e)
        {
            saveConfigFile(m_strCurrentDir + m_strSaveDir);
            ReleaseMutex();
        }

        private void discord_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Process.Start("https://discord.gg/F3bDEFE");
        }

        private void patreon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Process.Start(patreonURL);
        }

        private void update_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            saveConfigFile(m_strCurrentDir + m_strSaveDir);
            SaveRegistry();

            var marcofix = m_strCurrentDir.TrimEnd('\\', '/', ' ');
            kkman = kkman.TrimEnd('\\', '/', ' ');
            string finaldir;

            if (!File.Exists($@"{kkman}\StandaloneUpdater.exe"))
                finaldir = $@"{m_strCurrentDir}{kkman}";
            else
                finaldir = kkman;

            var text = $@"{finaldir}\StandaloneUpdater.exe";

            var argdir = $"\u0022{marcofix}\u0022";
            var argloc = updated;
            var args = $"{argdir} {argloc}";

            if (!updatelocExists)
                args = $"{argdir}";

            if (File.Exists(text))
                Process.Start(new ProcessStartInfo(text) { WorkingDirectory = $@"{finaldir}", Arguments = args });
        }

        private void langEnglish(object sender, MouseButtonEventArgs e)
        {
            ChangeTL("en");
        }

        private void langJapanese(object sender, MouseButtonEventArgs e)
        {
            ChangeTL("ja");
        }

        private void langChinese(object sender, MouseButtonEventArgs e)
        {
            ChangeTL("zh-CN");
        }

        private void langTaiwanese(object sender, MouseButtonEventArgs e)
        {
            ChangeTL("zh-TW");
        }

        private void PartyFilter(string language)
        {
            if (!noTL)
                ChangeTL(language);
            else
                SetupLang(language);
        }

        private void ChangeTL(string language)
        {
            if (language == "ja") m_Setting.Language = 0;
            if (language == "en") m_Setting.Language = 1;
            if (language == "zh-CN") m_Setting.Language = 2;
            if (language == "zh-TW") m_Setting.Language = 3;
            saveConfigFile(m_strCurrentDir + m_strSaveDir);
            SaveRegistry();
            WriteLangIni(language);
            SetupLang(language);
        }

        private void WriteLangIni(string language)
        {
            if (File.Exists(m_strCurrentDir + "BepInEx/Config/AutoTranslatorConfig.ini"))
                switch (language)
                {
                    case "ja":
                        if (System.Windows.MessageBox.Show("ゲームにこの言語の選択を反映させたいですか？", "質問", MessageBoxButton.YesNo,
                            MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            disableXUA();
                            helvete(language);
                        }

                        return;
                    case "zh-CN":
                        if (System.Windows.MessageBox.Show("您是否希望遊戲中的語言反映這項語言選擇？", "問題", MessageBoxButton.YesNo,
                            MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            disableXUA();
                            helvete(language);
                        }

                        return;
                    case "zh-TW":
                        if (System.Windows.MessageBox.Show("您是否希望游戏中的语言反映这项语言选择？", "问题", MessageBoxButton.YesNo,
                            MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            disableXUA();
                            helvete(language);
                        }

                        return;
                    default:
                        if (System.Windows.MessageBox.Show(
                            "Do you want the ingame language to reflect this language choice?", "Question",
                            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            enableXUA();
                            helvete(language);
                        }

                        return;
                }
            // Borrowed from Marco
        }

        private void helvete(string language)
        {
            if (File.Exists("BepInEx/Config/AutoTranslatorConfig.ini"))
            {
                var ud = Path.Combine(m_strCurrentDir, @"BepInEx/Config/AutoTranslatorConfig.ini");

                try
                {
                    var contents = File.ReadAllLines(ud).ToList();

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

                    File.WriteAllLines(ud, contents.ToArray());
                }
                catch (Exception e)
                {
                    MessageBox.Show("Something went wrong: " + e);
                }
            }
        }

        private void enableXUA()
        {
            if (File.Exists("BepInEx/Config/AutoTranslatorConfig.ini"))
            {
                var ud = Path.Combine(m_strCurrentDir, @"BepInEx/Config/AutoTranslatorConfig.ini");

                try
                {
                    var contents = File.ReadAllLines(ud).ToList();

                    var setToLanguage = contents.FindIndex(s => s.ToLower().Contains("[Service]".ToLower()));
                    if (setToLanguage >= 0)
                    {
                        var i = contents.FindIndex(setToLanguage, s => s.StartsWith("Endpoint"));
                        if (i > setToLanguage)
                            contents[i] = "Endpoint=GoogleTranslate";
                        else
                            contents.Insert(setToLanguage + 1, "Endpoint=");
                    }
                    else
                    {
                        contents.Add("");
                        contents.Add("[Service]");
                        contents.Add("Endpoint=GoogleTranslate");
                    }

                    File.WriteAllLines(ud, contents.ToArray());
                }
                catch (Exception e)
                {
                    MessageBox.Show("Something went wrong: " + e);
                }
            }
        }

        private void disableXUA()
        {
            if (File.Exists("BepInEx/Config/AutoTranslatorConfig.ini"))
            {
                var ud = Path.Combine(m_strCurrentDir, @"BepInEx/Config/AutoTranslatorConfig.ini");

                try
                {
                    var contents = File.ReadAllLines(ud).ToList();

                    var setToLanguage = contents.FindIndex(s => s.ToLower().Contains("[Service]".ToLower()));
                    if (setToLanguage >= 0)
                    {
                        var i = contents.FindIndex(setToLanguage, s => s.StartsWith("Endpoint"));
                        if (i > setToLanguage)
                            contents[i] = "Endpoint=";
                        else
                            contents.Insert(setToLanguage + 1, "Endpoint=");
                    }
                    else
                    {
                        contents.Add("");
                        contents.Add("[Service]");
                        contents.Add("Endpoint=");
                    }

                    File.WriteAllLines(ud, contents.ToArray());
                }
                catch (Exception e)
                {
                    MessageBox.Show("Something went wrong: " + e);
                }
            }
        }

        private void deactivateTL(int i)
        {
            s_EnglishTL = new[]
            {
                "BepInEx/XUnity.AutoTranslator.Plugin.BepIn",
                "BepInEx/XUnity.AutoTranslator.Plugin.Core",
                "BepInEx/XUnity.AutoTranslator.Plugin.ExtProtocol",
                "BepInEx/XUnity.RuntimeHooker.Core",
                "BepInEx/XUnity.RuntimeHooker",
                "BepInEx/KK_Subtitles",
                "BepInEx/ExIni"
            };

            if (i == 0)
            {
                foreach (var file in s_EnglishTL)
                    if (File.Exists(m_strCurrentDir + file + ".dll"))
                        File.Move(m_strCurrentDir + file + ".dll", m_strCurrentDir + file + "._ll");
            }
            else
            {
                foreach (var file in s_EnglishTL)
                {
                    if (File.Exists(m_strCurrentDir + file + "._ll"))
                        File.Move(m_strCurrentDir + file + "._ll", m_strCurrentDir + file + ".dll");
                    helvete("en");
                }
            }
        }

        private void SetupLang(string langstring)
        {
            if (File.Exists(m_strCurrentDir + m_customDir + decideLang))
                File.Delete(m_strCurrentDir + m_customDir + decideLang);
            using (var writetext = new StreamWriter(m_strCurrentDir + m_customDir + decideLang))
            {
                writetext.WriteLine(langstring);
            }

            System.Windows.Forms.Application.Restart();
        }

        private void modeDev_Checked(object sender, RoutedEventArgs e)
        {
            using (var writetext = new StreamWriter(m_strCurrentDir + m_customDir + "/devMode"))
            {
                writetext.WriteLine("devmode: True");
            }

            if (!startup) devMode(true);
        }

        private void modeDev_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!startup) devMode(false);
        }

        private void devMode(bool setDev)
        {
            var ud = Path.Combine(m_strCurrentDir, @"BepInEx\config\BepInEx.cfg");

            try
            {
                var contents = File.ReadAllLines(ud).ToList();

                var setToLanguage = contents.FindIndex(s => s.ToLower().Contains("[Logging.Console]".ToLower()));
                if (setToLanguage >= 0 && setDev)
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

        private void HoneyPotInspector_Run(object sender, RoutedEventArgs e)
        {
            if (File.Exists($"{m_strCurrentDir}\\HoneyPot\\HoneyPotInspector.exe"))
                Process.Start($"{m_strCurrentDir}\\HoneyPot\\HoneyPotInspector.exe");
            else
                MessageBox.Show("HoneyPot doesn't seem to be applied to this installation.");
        }

        private void dhh_Checked(object sender, RoutedEventArgs e)
        {
            if (File.Exists($"{m_strCurrentDir}\\BepInEx\\Plugins\\DHH_AI4.dl_"))
            {
                if (File.Exists($"{m_strCurrentDir}\\BepInEx\\Plugins\\DHH_AI4.dll"))
                    File.Delete($"{m_strCurrentDir}\\BepInEx\\Plugins\\DHH_AI4.dll");
                File.Move($"{m_strCurrentDir}\\BepInEx\\Plugins\\DHH_AI4.dl_",
                    $"{m_strCurrentDir}\\BepInEx\\Plugins\\DHH_AI4.dll");
            }
        }

        private void dhh_Unchecked(object sender, RoutedEventArgs e)
        {
            if (File.Exists($"{m_strCurrentDir}\\BepInEx\\Plugins\\DHH_AI4.dll"))
            {
                if (File.Exists($"{m_strCurrentDir}\\BepInEx\\Plugins\\DHH_AI4.dl_"))
                    File.Delete($"{m_strCurrentDir}\\BepInEx\\Plugins\\DHH_AI4.dl_");
                File.Move($"{m_strCurrentDir}\\BepInEx\\Plugins\\DHH_AI4.dll",
                    $"{m_strCurrentDir}\\BepInEx\\Plugins\\DHH_AI4.dl_");
            }
        }

        private void aigraphics_Checked(object sender, RoutedEventArgs e)
        {
            if (File.Exists($"{m_strCurrentDir}\\BepInEx\\Plugins\\AIGraphics\\AI_Graphics.dl_"))
            {
                if (File.Exists($"{m_strCurrentDir}\\BepInEx\\Plugins\\AIGraphics\\AI_Graphics.dll"))
                    File.Delete($"{m_strCurrentDir}\\BepInEx\\Plugins\\AIGraphics\\AI_Graphics.dll");
                File.Move($"{m_strCurrentDir}\\BepInEx\\Plugins\\AIGraphics\\AI_Graphics.dl_",
                    $"{m_strCurrentDir}\\BepInEx\\Plugins\\AIGraphics\\AI_Graphics.dll");
            }

            toggleDHH.IsChecked = false;
            if (!startup)
                MessageBox.Show("Press F5 ingame for menu.", "Information");
        }

        private void aigraphics_Unchecked(object sender, RoutedEventArgs e)
        {
            if (File.Exists($"{m_strCurrentDir}\\BepInEx\\Plugins\\AIGraphics\\AI_Graphics.dll"))
            {
                if (File.Exists($"{m_strCurrentDir}\\BepInEx\\Plugins\\AIGraphics\\AI_Graphics.dl_"))
                    File.Delete($"{m_strCurrentDir}\\BepInEx\\Plugins\\AIGraphics\\AI_Graphics.dl_");
                File.Move($"{m_strCurrentDir}\\BepInEx\\Plugins\\AIGraphics\\AI_Graphics.dll",
                    $"{m_strCurrentDir}\\BepInEx\\Plugins\\AIGraphics\\AI_Graphics.dl_");
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
                DragMove();
        }

        private struct DisplayMode
        {
            public int Width;

            public int Height;

            public string text;
        }

        private struct DisplayModes
        {
            public int x;

            public int y;

            public List<DisplayMode> list;
        }
    }
}