﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using MessageBox = System.Windows.Forms.MessageBox;
using System.Windows.Input;

namespace InitSetting
{
    public partial class MainWindow : Window
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool IsWow64Process([In] IntPtr hProcess, out bool lpSystemInfo);

        [DllImport("user32.dll")]
        static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

        [DllImport("user32.dll")]
        static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        [DllImport("User32.dll")]
        static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr rect, MainWindow.EnumDisplayMonitorsCallback callback, IntPtr dwData);

        [DllImport("User32.dll")]
        static extern bool GetMonitorInfo(IntPtr hMonitor, ref MainWindow.MonitorInfoEx info);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool DeviceIoControl(IntPtr hDevice, uint dwIoControlCode,
        IntPtr InBuffer, int nInBufferSize,
        IntPtr OutBuffer, int nOutBufferSize,
        out int pBytesReturned, IntPtr lpOverlapped);

        public MainWindow()
        {
            InitializeComponent();
            //if (!DoubleStartCheck())
            //{
            //    System.Windows.Application.Current.MainWindow.Close();
            //    return;
            //}

            // Check for duplicate launches

            Process process = Process.GetCurrentProcess();
            var dupl = (Process.GetProcessesByName(process.ProcessName));
            if (true)
            {
                foreach (var p in dupl)
                {
                    if (p.Id != process.Id)
                        p.Kill();
                }
            }

            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;

            startup = true;

            //temp hide unimplemented stuffs
            CustomRes.Visibility = Visibility.Hidden;
            gridUpdate.Visibility = Visibility.Hidden;

            Directory.CreateDirectory(m_strCurrentDir + m_customDir);

            if (!File.Exists(m_strCurrentDir + m_customDir + kkmdir))
            {

            }

            // Framework test
            isIPA = File.Exists($"{m_strCurrentDir}\\IPA.exe");
            isBepIn = Directory.Exists($"{m_strCurrentDir}\\BepInEx");

            if (isIPA && isBepIn)
                MessageBox.Show("Both BepInEx and IPA is detected in the game folder!\n\nApplying both frameworks may lead to problems when running the game!", "Warning!");

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
                        if (i < n)
                        {
                            toggleConsole.IsChecked = true;
                        }

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

            //if (File.Exists(m_strCurrentDir + m_customDir + "/enableUpdate") && File.Exists(m_strCurrentDir + m_customDir + "/updateURL.txt"))
            //{
            //    //Getting download URL
            //    var dlFileStream = new FileStream(m_strCurrentDir + m_customDir + "/updateURL.txt", FileMode.Open, FileAccess.Read);
            //    using (var streamReader = new StreamReader(dlFileStream, Encoding.UTF8))
            //    {
            //        string line;
            //        while ((line = streamReader.ReadLine()) != null)
            //        {
            //            updateURL = line;
            //        }
            //    }
            //    dlFileStream.Close();

            //    //Grabbing existing version
            //    var verFileStream = new FileStream(m_strCurrentDir + m_customDir + "/enableUpdate", FileMode.Open, FileAccess.Read);
            //    using (var streamReader = new StreamReader(verFileStream, Encoding.UTF8))
            //    {
            //        string line;
            //        while ((line = streamReader.ReadLine()) != null)
            //        {
            //            packVersion = line;
            //        }
            //    }
            //    verFileStream.Close();

            //    //Grabbing new version string
            //    try
            //    {
            //        newPackVersion = (new WebClient()).DownloadString(updateURL).ToString();
            //    }
            //    catch { }

            //    //Enables update button if new version is found
            //    if (packVersion != newPackVersion && newPackVersion != null)
            //    {
            //        updateBtn.Visibility = Visibility.Visible;
            //    }
            //}

            // Mod settings

            if (File.Exists($"{m_strCurrentDir}{m_customDir}/toggle32.txt"))
            {
                toggle32.IsChecked = true;
                x86 = true;
            }
            if (!File.Exists($"{m_strCurrentDir}\\PlayHome32bit.exe"))
            {
                toggle32.Visibility = Visibility.Hidden;
                x86 = false;
            }


            startup = false;

            LangExists = File.Exists(m_strCurrentDir + m_customDir + decideLang);
            if (LangExists)
            {
                var verFileStream = new FileStream(m_strCurrentDir + m_customDir + decideLang, FileMode.Open, FileAccess.Read);
                using (var streamReader = new StreamReader(verFileStream, Encoding.UTF8))
                {
                    string line;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        lang = line;
                    }
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

                warningText.Text = "このゲームは成人向けので、18歳未満（または地域の法律によりと同等の年齢）がこのゲームをプレイまたは所有しているができない。\n\nこのゲームには性的内容の内容が含まれます。内に描かれている行動は、実生活で複製することは違法です。つまり、これは面白いゲームです、そうしましょう？(~.~)v";
                buttonInst.Content = "インストール";
                buttonFemaleCard.Content = "キャラカード (女性)";
                buttonMaleCard.Content = "キャラカード (男性)";
                buttonScreenshot.Content = "SS"; buttonUserData.Content = "UserData";
                labelStart.Content = "ゲーム開始";
                labelM.Content = "ゲーム";
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

                // HoneySelect Exclusive
                labelStartSC.Content = "スタジオNEO開始";
                labelStartSN.Content = "スタジオ開始";
                labelStartVO.Content = "VR(Ocolus)開始";
                labelStartVV.Content = "VR(Vive)開始";
                labelStartBA.Content = "BattleArena開始";
                labelMSC.Content = "マニュアル";
                labelMSN.Content = "マニュアル";
                labelMVO.Content = "マニュアル";
                labelMVV.Content = "マニュアル";
                labelMBA.Content = "マニュアル";
                buttonScenesN.Content = "シーン (NEO)";
                buttonScenesC.Content = "シーン";

            }
            else if (lang == "zh-CN") // By @Madevil#1103 & @𝐄𝐀𝐑𝐓𝐇𝐒𝐇𝐈𝐏 💖#4313 
            {
                labelTranslated.Visibility = Visibility.Visible;
                labelTranslatedBorder.Visibility = Visibility.Visible;

                
            }
            else if (lang == "ko") // By @Keris-#1903 
            {
                labelTranslated.Visibility = Visibility.Visible;
                labelTranslatedBorder.Visibility = Visibility.Visible;

                
            }
            else if (lang == "es") // By @Heroine Nisa#3207
            {
                labelTranslated.Visibility = Visibility.Visible;
                labelTranslatedBorder.Visibility = Visibility.Visible;

                
            }
            else if (lang == "pt") // By @Neptune#1989 
            {
                labelTranslated.Visibility = Visibility.Visible;
                labelTranslatedBorder.Visibility = Visibility.Visible;

                
            }
            else if (lang == "fr") // By VaizravaNa#2315
            {
                labelTranslated.Visibility = Visibility.Visible;
                labelTranslatedBorder.Visibility = Visibility.Visible;

                
            }
            else if (lang == "de") // By @DONTFORGETME#6198 
            {
                labelTranslated.Visibility = Visibility.Visible;
                labelTranslatedBorder.Visibility = Visibility.Visible;

                
            }
            else if (lang == "no") // By @SmokeOfC|女神様の兄様#1984
            {
                labelTranslated.Visibility = Visibility.Visible;
                labelTranslatedBorder.Visibility = Visibility.Visible;

                
            }

            m_astrQuality = new string[]
            {
                q_performance,
                q_normal,
                q_quality
            };

            // Do checks

            is64bitOS = Is64BitOS();
            isStudio = File.Exists(m_strCurrentDir + m_strStudioExe);
            isMainGame = File.Exists(m_strCurrentDir + m_strGameExe);

            if (!is64bitOS)
            {
                toggle32.IsChecked = true;
                toggle32.IsEnabled = false;
            }

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
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        labelDist.Content = line;
                    }
                }
                verFileStream.Close();
            }

            // Launcher Customization: Defining Warning, background and character

            if (WarningExists)
            {
                var verFileStream = new FileStream(m_strCurrentDir + m_customDir + warningLoc, FileMode.Open, FileAccess.Read);
                try
                {
                    using (StreamReader sr = new StreamReader(m_strCurrentDir + m_customDir + warningLoc))
                    {
                        String line = sr.ReadToEnd();
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
                Uri urich = new Uri(m_strCurrentDir + m_customDir + charLoc, UriKind.RelativeOrAbsolute);
                PackChara.Source = BitmapFrame.Create(urich);
            }
            if (BackgExists)
            {
                Uri uribg = new Uri(m_strCurrentDir + m_customDir + backgLoc, UriKind.RelativeOrAbsolute);
                appBG.ImageSource = BitmapFrame.Create(uribg);
            }
            if (PatreonExists)
            {
                var verFileStream = new FileStream(m_strCurrentDir + m_customDir + patreonLoc, FileMode.Open, FileAccess.Read);
                using (var streamReader = new StreamReader(verFileStream, Encoding.UTF8))
                {
                    string line;
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        patreonURL = line;
                    }
                }
                verFileStream.Close();
            }
            else
            {
                linkPatreon.Visibility = Visibility.Collapsed;
                patreonBorder.Visibility = Visibility.Collapsed;
                patreonIMG.Visibility = Visibility.Collapsed;
            }

            int num = Screen.AllScreens.Length;
            getDisplayMode_EnumDisplaySettings(num);
            m_Setting.Size = "1280 x 720 (16 : 9)";
            m_Setting.Width = 1280;
            m_Setting.Height = 720;
            m_Setting.Quality = 1;
            m_Setting.Language = 0;
            m_Setting.Display = 0;
            m_Setting.FullScreen = false;
            if (num == 2)
            {
                dropDisplay.Items.Add(s_primarydisplay);
                dropDisplay.Items.Add($"{s_subdisplay} : 1");
            }
            else
            {
                for (int i = 0; i < num; i++)
                {
                    string newItem = (i == 0) ? s_primarydisplay : ($"{s_subdisplay} : " + i);
                    dropDisplay.Items.Add(newItem);
                }
            }
            foreach (string newItem2 in m_astrQuality)
            {
                dropQual.Items.Add(newItem2);
            }

            SetEnableAndVisible();

            string path = m_strCurrentDir + m_strSaveDir;
        CheckConfigFile:
            if (File.Exists(path))
            {
                try
                {
                    using (FileStream fileStream = new FileStream(path, FileMode.Open))
                    {
                        XmlSerializer xmlSerializer = new XmlSerializer(typeof(ConfigSetting));
                        m_Setting = (ConfigSetting)xmlSerializer.Deserialize(fileStream);
                    }

                    m_Setting.Display = Math.Min(m_Setting.Display, num - 1);
                    setDisplayComboBox(m_Setting.FullScreen);
                    var flag = false;
                    for (var k = 0; k < dropRes.Items.Count; k++)
                    {
                        if (dropRes.Items[k].ToString() == m_Setting.Size)
                            flag = true;
                    }
                    dropRes.Text = flag ? m_Setting.Size : "1280 x 720 (16 : 9)";
                    toggleFullscreen.IsChecked = m_Setting.FullScreen;
                    dropQual.Text = m_astrQuality[m_Setting.Quality];
                    string text = m_Setting.Display == 0 ? s_primarydisplay : $"{s_subdisplay} : " + m_Setting.Display;
                    if (num == 2)
                    {
                        text = new[]
                        {
                        s_primarydisplay,
                        $"{s_subdisplay} : 1"
                        }[m_Setting.Display];
                    }
                    if (dropDisplay.Items.Contains(text))
                        dropDisplay.Text = text;
                    else
                    {
                        dropDisplay.Text = s_primarydisplay;
                        m_Setting.Display = 0;
                    }
                }
                catch (Exception)
                {
                    System.Windows.Forms.MessageBox.Show("/UserData/setup.xml file was corrupted, settings will be reset.");
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

        void SetEnableAndVisible()
        {

        }

        void SaveRegistry()
        {
            using (RegistryKey registryKey = Registry.CurrentUser.CreateSubKey(m_strGameRegistry))
            {
                registryKey.SetValue("Screenmanager Is Fullscreen mode_h3981298716", m_Setting.FullScreen ? 1 : 0);
                registryKey.SetValue("Screenmanager Resolution Height_h2627697771", m_Setting.Height);
                registryKey.SetValue("Screenmanager Resolution Width_h182942802", m_Setting.Width);
                registryKey.SetValue("UnityGraphicsQuality_h1669003810", 2);
                registryKey.SetValue("UnitySelectMonitor_h17969598", m_Setting.Display);
            }
            if (isStudio)
            {
                using (RegistryKey registryKey2 = Registry.CurrentUser.CreateSubKey(m_strStudioRegistry))
                {
                    registryKey2.SetValue("Screenmanager Is Fullscreen mode_h3981298716", m_Setting.FullScreen ? 1 : 0);
                    registryKey2.SetValue("Screenmanager Resolution Height_h2627697771", m_Setting.Height);
                    registryKey2.SetValue("Screenmanager Resolution Width_h182942802", m_Setting.Width);
                    registryKey2.SetValue("UnityGraphicsQuality_h1669003810", 2);
                    registryKey2.SetValue("UnitySelectMonitor_h17969598", m_Setting.Display);
                }
            }
        }

        void PlayFunc(string strExe)
        {
            saveConfigFile(m_strCurrentDir + m_strSaveDir);
            SaveRegistry();
            string text = m_strCurrentDir + strExe;
            string ipa = "\u0022" + m_strCurrentDir + "IPA.exe" + "\u0022";
            string ipaArgs = "\u0022" + text + "\u0022" + " --launch";
            if (File.Exists(text) && isIPA)
            {
                Process.Start(new ProcessStartInfo(ipa) { WorkingDirectory = m_strCurrentDir, Arguments = ipaArgs });
                System.Windows.Application.Current.MainWindow.Close();
                return;
            }
            else if (File.Exists(text))
            {
                Process.Start(new ProcessStartInfo(text) { WorkingDirectory = m_strCurrentDir });
                System.Windows.Application.Current.MainWindow.Close();
                return;
            }
            MessageBox.Show("Executable can't be located", "Warning!");
        }

        void buttonStart_Click(object sender, RoutedEventArgs e)
        {
            if (x86 == true)
                PlayFunc(m_strGameExe32);
            else
                PlayFunc(m_strGameExe);
        }

        void buttonStartS_Click(object sender, RoutedEventArgs e)
        {
            if (x86 == true)
                PlayFunc(m_strStudioExe32);
            else
                PlayFunc(m_strStudioExe);
        }

        void buttonStartV_Click(object sender, RoutedEventArgs e)
        {
            PlayFunc(m_strVRExe);
        }

        void buttonClose_Click(object sender, RoutedEventArgs e)
        {
            saveConfigFile(m_strCurrentDir + m_strSaveDir);
            ReleaseMutex();
            System.Windows.Application.Current.MainWindow.Close();
        }

        void Resolution_Change(object sender, SelectionChangedEventArgs e)
        {
            if (-1 == dropRes.SelectedIndex)
            {
                return;
            }
            ComboBoxCustomItem comboBoxCustomItem = (ComboBoxCustomItem)dropRes.SelectedItem;
            m_Setting.Size = comboBoxCustomItem.text;
            m_Setting.Width = comboBoxCustomItem.width;
            m_Setting.Height = comboBoxCustomItem.height;
        }

        void Quality_Change(object sender, SelectionChangedEventArgs e)
        {
            string a = dropQual.SelectedItem.ToString();
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
            if (!(a == q_quality))
            {
                return;
            }
            m_Setting.Quality = 2;
        }

        void windowUnChecked(object sender, RoutedEventArgs e)
        {
            setDisplayComboBox(false);
            dropRes.Text = m_Setting.Size;
            m_Setting.FullScreen = false;
        }

        void windowChecked(object sender, RoutedEventArgs e)
        {
            setDisplayComboBox(true);
            m_Setting.FullScreen = true;
            setFullScreenDevice();
        }

        void buttonManual_Click(object sender, RoutedEventArgs e)
        {
            string manualEN = $"{m_strCurrentDir}\\manual\\manual_en.html";
            string manualLANG = $"{m_strCurrentDir}\\manual\\manual_{lang}.html";
            string manualJA = m_strCurrentDir + m_strManualDir;

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

        void buttonManualS_Click(object sender, RoutedEventArgs e)
        {
            string manualEN = $"{m_strCurrentDir}\\manual_s\\manual_en.html";
            string manualLANG = $"{m_strCurrentDir}\\manual_s\\manual_{lang}.html";
            string manualJA = m_strCurrentDir + m_strStudioManualDir;

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

        void buttonManualV_Click(object sender, RoutedEventArgs e)
        {
            string manualEN = $"{m_strCurrentDir}\\manual_vr\\manual_en.html";
            string manualLANG = $"{m_strCurrentDir}\\manual_vr\\manual_{lang}.html";
            string manualJA = m_strCurrentDir + m_strVRManualDir;

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

        void Display_Change(object sender, SelectionChangedEventArgs e)
        {
            if (-1 == dropDisplay.SelectedIndex)
            {
                return;
            }
            m_Setting.Display = dropDisplay.SelectedIndex;
            if (m_Setting.FullScreen)
            {
                setDisplayComboBox(true);
                setFullScreenDevice();
            }
        }

        void buttonInst_Click(object sender, RoutedEventArgs e)
        {
            char[] trimChars = new char[]
            {
                '/'
            };
            char[] trimChars2 = new char[]
            {
                '\\'
            };
            string text = m_strCurrentDir.TrimEnd(trimChars);
            text = text.TrimEnd(trimChars2);
            if (Directory.Exists(text))
            {
                Process.Start("explorer.exe", text);
                return;
            }
            MessageBox.Show("Folder could not be found, please launch the game at least once.", "Warning!");
        }

        void buttonScenes_Click(object sender, RoutedEventArgs e)
        {
            char[] trimChars = new char[]
            {
                '/'
            };
            char[] trimChars2 = new char[]
            {
                '\\'
            };
            string text = m_strCurrentDir.TrimEnd(trimChars);
            text = text.TrimEnd(trimChars2) + "\\UserData\\Studio\\scene";
            if (Directory.Exists(text))
            {
                Process.Start("explorer.exe", text);
                return;
            }
            MessageBox.Show("Folder could not be found, please launch the game at least once.", "Warning!");
        }

        void buttonUserData_Click(object sender, RoutedEventArgs e)
        {
            char[] trimChars = new char[]
            {
                '/'
            };
            char[] trimChars2 = new char[]
            {
                '\\'
            };
            string text = m_strCurrentDir.TrimEnd(trimChars);
            text = text.TrimEnd(trimChars2) + "\\UserData\\Studio\\scene";
            if (Directory.Exists(text))
            {
                Process.Start("explorer.exe", text);
                return;
            }
            MessageBox.Show("Folder could not be found, please launch the game at least once.", "Warning!");
        }

        void buttonScreenshot_Click(object sender, RoutedEventArgs e)
        {
            char[] trimChars = new char[]
            {
                '/'
            };
            char[] trimChars2 = new char[]
            {
                '\\'
            };
            string text = m_strCurrentDir.TrimEnd(trimChars);
            text = text.TrimEnd(trimChars2) + "\\UserData\\cap";
            if (Directory.Exists(text))
            {
                Process.Start("explorer.exe", text);
                return;
            }
            MessageBox.Show("Folder could not be found, please launch the game at least once.", "Warning!");
        }

        void buttonFemaleCard_Click(object sender, RoutedEventArgs e)
        {
            char[] trimChars = new char[]
            {
                '/'
            };
            char[] trimChars2 = new char[]
            {
                '\\'
            };
            string text = m_strCurrentDir.TrimEnd(trimChars);
            text = text.TrimEnd(trimChars2) + "\\UserData\\chara\\female";
            if (Directory.Exists(text))
            {
                Process.Start("explorer.exe", text);
                return;
            }
            MessageBox.Show("Folder could not be found, please launch the game at least once.", "Warning!");
        }

        void buttonMaleCard_Click(object sender, RoutedEventArgs e)
        {
            char[] trimChars = new char[]
            {
                '/'
            };
            char[] trimChars2 = new char[]
            {
                '\\'
            };
            string text = m_strCurrentDir.TrimEnd(trimChars);
            text = text.TrimEnd(trimChars2) + "\\UserData\\chara\\male";
            if (Directory.Exists(text))
            {
                Process.Start("explorer.exe", text);
                return;
            }
            MessageBox.Show("Folder could not be found, please launch the game at least once.", "Warning!");
        }

        void SystemInfo_Open(object sender, RoutedEventArgs e)
        {
            string text = Environment.ExpandEnvironmentVariables("%windir%") + "/System32/dxdiag.exe";
            if (File.Exists(text))
            {
                Process.Start(text);
                return;
            }
            MessageBox.Show("Folder could not be found, please launch the game at least once.", "Warning!");
        }

        bool DoubleStartCheck()
        {
            bool flag;
            mutex = new Mutex(true, "AIS", out flag);
            bool v = !flag;
            if (v)
            {
                if (mutex != null)
                {
                    mutex.Close();
                }
                mutex = null;
                return false;
            }
            return true;
        }

        bool ReleaseMutex()
        {
            if (mutex == null)
            {
                return false;
            }
            mutex.ReleaseMutex();
            mutex.Close();
            mutex = null;
            return true;
        }

        void setDisplayComboBox(bool _bFullScreen)
        {
            dropRes.Items.Clear();
            int nDisplay = m_Setting.Display;
            foreach (MainWindow.DisplayMode displayMode in (_bFullScreen ? m_listCurrentDisplay[nDisplay].list : m_listDefaultDisplay))
            {
                ComboBoxCustomItem newItem = new ComboBoxCustomItem
                {
                    text = displayMode.text,
                    width = displayMode.Width,
                    height = displayMode.Height
                };
                dropRes.Items.Add(newItem);
            }
        }

        void saveConfigFile(string _strFilePath)
        {
            if (!Directory.Exists(Path.GetDirectoryName(_strFilePath)))
            {
                return;
            }
            FileStream fileStream = null;
            try
            {
                fileStream = new FileStream(_strFilePath, FileMode.Create);
                if (fileStream != null)
                {
                    using (StreamWriter streamWriter = new StreamWriter(fileStream, Encoding.GetEncoding("UTF-16")))
                    {
                        XmlSerializerNamespaces xmlSerializerNamespaces = new XmlSerializerNamespaces();
                        xmlSerializerNamespaces.Add(string.Empty, string.Empty);
                        new XmlSerializer(typeof(ConfigSetting)).Serialize(streamWriter, m_Setting, xmlSerializerNamespaces);
                        fileStream = null;
                    }
                }
            }
            finally
            {
                if (fileStream != null)
                {
                    fileStream.Dispose();
                }
            }
        }

        void getDisplayMode_CIM_VideoControllerResolution()
        {
            ManagementObjectCollection instances = new ManagementClass("CIM_VideoControllerResolution").GetInstances();
            List<MainWindow.DisplayMode> list = new List<MainWindow.DisplayMode>();
            uint num = 0u;
            uint num2 = 0u;
            foreach (ManagementBaseObject managementBaseObject in instances)
            {
                ManagementObject managementObject = (ManagementObject)managementBaseObject;
                uint nXX = (uint)managementObject["HorizontalResolution"];
                uint nYY = (uint)managementObject["VerticalResolution"];
                if ((num != nXX || num2 != nYY) && (ulong)managementObject["NumberOfColors"] == 4294967296UL)
                {
                    MainWindow.DisplayMode displayMode = m_listDefaultDisplay.Find((MainWindow.DisplayMode i) => (long)i.Width == (long)((ulong)nXX) && (long)i.Height == (long)((ulong)nYY));
                    if (displayMode.Width != 0)
                    {
                        list.Add(displayMode);
                    }
                    num = nXX;
                    num2 = nYY;
                }
            }
            MainWindow.DisplayModes item = default(MainWindow.DisplayModes);
            item.list = list;
            m_listCurrentDisplay.Add(item);
            if (instances.Count == 0)
            {
                System.Windows.Forms.MessageBox.Show("Failed to list screens");
                return;
            }
            if (m_listCurrentDisplay.Count == 0)
            {
                System.Windows.Forms.MessageBox.Show("Failed to list supported resolutions");
            }
        }

        void getDisplayMode_EnumDisplaySettings(int numDisplay)
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

        static int DisplaySort(MainWindow.DisplayModes a, MainWindow.DisplayModes b)
        {
            if (a.x < b.x)
            {
                return -1;
            }
            if (a.x > b.x)
            {
                return 1;
            }
            if (a.y < b.y)
            {
                return -1;
            }
            if (a.y > b.y)
            {
                return 1;
            }
            return 0;
        }

        static MainWindow.MonitorInfoEx[] GetMonitors()
        {
            List<MainWindow.MonitorInfoEx> list = new List<MainWindow.MonitorInfoEx>();
            MainWindow.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, delegate (IntPtr hMonitor, IntPtr hdcMonitor, IntPtr lprcMonitor, IntPtr dwData)
            {
                MainWindow.MonitorInfoEx item = new MainWindow.MonitorInfoEx
                {
                    cbSize = Marshal.SizeOf(typeof(MainWindow.MonitorInfoEx))
                };
                MainWindow.GetMonitorInfo(hMonitor, ref item);
                list.Add(item);
            }, IntPtr.Zero);
            return list.ToArray();
        }

        void setFullScreenDevice()
        {
            int nDisplay = m_Setting.Display;
            if (m_listCurrentDisplay[nDisplay].list.Count == 0)
            {
                m_Setting.FullScreen = false;
                toggleFullscreen.IsChecked = new bool?(false);
                System.Windows.Forms.MessageBox.Show("This monitor doesn't support fullscreen.");
                return;
            }
            if (m_listCurrentDisplay[nDisplay].list.Find((MainWindow.DisplayMode x) => x.text.Contains(m_Setting.Size)).Width == 0)
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
            return MainWindow.GetProcAddress(MainWindow.GetModuleHandle("Kernel32.dll"), "IsWow64Process") != IntPtr.Zero && MainWindow.IsWow64Process(Process.GetCurrentProcess().Handle, out flag) && flag;
        }

        public bool Is64BitOS()
        {
            if (IntPtr.Size == 4)
            {
                return IsWow64();
            }
            return IntPtr.Size == 8;
        }

        void MenuCloseButton(object sender, EventArgs e)
        {
            saveConfigFile(m_strCurrentDir + m_strSaveDir);
            ReleaseMutex();
        }

        const int MONITORINFOF_PRIMARY = 1;

        string[] m_astrQuality;
        string[] s_EnglishTL;

        string m_strGameRegistry = "Software\\illusion\\PlayHome\\";
        string m_strStudioRegistry = "Software\\illusion\\PlayHomeStudio\\";
        string m_strGameExe = "PlayHome64bit.exe";
        string m_strStudioExe = "PlayHomeStudio64bit.exe";
        string m_strGameExe32 = "PlayHome32bit.exe";
        string m_strStudioExe32 = "PlayHomeStudio32bit.exe";
        string m_strVRExe = "VR GEDOU.exe";
        string m_strManualDir = "/manual/お読み下さい.html";
        string m_strStudioManualDir = "/manual_s/お読み下さい.html";
        string m_strVRManualDir = "/manual_vr/お読み下さい.html";

        const string m_strSaveDir = "/UserData/setup.xml";
        const string m_customDir = "/UserData/LauncherEN";

        const string m_strDefSizeText = "1280 x 720 (16 : 9)";
        const int m_nDefQuality = 1;
        const int m_nDefWidth = 1280;
        const int m_nDefHeight = 720;
        const bool m_bDefFullScreen = false;

        string m_strCurrentDir = Environment.CurrentDirectory + "\\";

        ConfigSetting m_Setting = new ConfigSetting();

        bool is64bitOS;

        bool isStudio;
        bool isMainGame;

        string lang = "en";
        bool noTL = false;
        bool startup;

        bool versionAvail;
        bool WarningExists;
        bool CharExists;
        bool BackgExists;
        bool PatreonExists;
        bool LangExists;
        bool x86;

        bool isIPA;
        bool isBepIn;

        string kkman;
        string updated = "placeholder";

        string q_performance = "Performance";
        string q_normal = "Normal";
        string q_quality = "Quality";
        string s_primarydisplay = "PrimaryDisplay";
        string s_subdisplay = "SubDisplay";

        const string decideLang = "/lang";
        const string versioningLoc = "/version";
        const string warningLoc = "/warning.txt";
        const string charLoc = "/Chara.png";
        const string backgLoc = "/LauncherBG.png";
        const string patreonLoc = "/patreon.txt";
        const string kkmdir = "/kkman.txt";
        const string updateLoc = "/updater.txt";
        //string updateURL;
        //string packVersion;
        //string newPackVersion;

        string patreonURL;

        List<MainWindow.DisplayMode> m_listDefaultDisplay = new List<MainWindow.DisplayMode>
        {
            new MainWindow.DisplayMode
            {
                Width = 854,
                Height = 480,
                text = "854 x 480 (16 : 9)"
            },
            new MainWindow.DisplayMode
            {
                Width = 1024,
                Height = 576,
                text = "1024 x 576 (16 : 9)"
            },
            new MainWindow.DisplayMode
            {
                Width = 1136,
                Height = 640,
                text = "1136 x 640 (16 : 9)"
            },
            new MainWindow.DisplayMode
            {
                Width = 1280,
                Height = 720,
                text = "1280 x 720 (16 : 9)"
            },
            new MainWindow.DisplayMode
            {
                Width = 1366,
                Height = 768,
                text = "1366 x 768 (16 : 9)"
            },
            new MainWindow.DisplayMode
            {
                Width = 1536,
                Height = 864,
                text = "1536 x 864 (16 : 9)"
            },
            new MainWindow.DisplayMode
            {
                Width = 1600,
                Height = 900,
                text = "1600 x 900 (16 : 9)"
            },
            new MainWindow.DisplayMode
            {
                Width = 1920,
                Height = 1080,
                text = "1920 x 1080 (16 : 9)"
            },
            new MainWindow.DisplayMode
            {
                Width = 2048,
                Height = 1152,
                text = "2048 x 1152 (16 : 9)"
            },
            new MainWindow.DisplayMode
            {
                Width = 2560,
                Height = 1440,
                text = "2560 x 1440 (16 : 9)"
            },
            new MainWindow.DisplayMode
            {
                Width = 3200,
                Height = 1800,
                text = "3200 x 1800 (16 : 9)"
            },
            new MainWindow.DisplayMode
            {
                Width = 3840,
                Height = 2160,
                text = "3840 x 2160 (16 : 9)"
            }
        };

        List<MainWindow.DisplayModes> m_listCurrentDisplay = new List<MainWindow.DisplayModes>();

        const int m_nQualityCount = 3;





        Mutex mutex;

        delegate void EnumDisplayMonitorsCallback(IntPtr hMonir, IntPtr hdcMonitor, IntPtr lprcMonitor, IntPtr dwData);

        internal struct MonitorInfoEx
        {
            public int cbSize;

            public MainWindow.Rect rcMonitor;

            public MainWindow.Rect rcWork;

            public int dwFlags;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDevice;
        }

        public struct Rect
        {
            public int Left;

            public int Top;

            public int Right;

            public int Bottom;
        }

        struct DisplayMode
        {
            public int Width;

            public int Height;

            public string text;
        }

        struct DisplayModes
        {
            public int x;

            public int y;

            public List<MainWindow.DisplayMode> list;
        }

        void discord_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Process.Start("https://discord.gg/F3bDEFE");
        }
        void patreon_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Process.Start(patreonURL);
        }

        void update_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            saveConfigFile(m_strCurrentDir + m_strSaveDir);
            SaveRegistry();

            string marcofix = m_strCurrentDir.TrimEnd('\\', '/', ' ');
            kkman = kkman.TrimEnd('\\', '/', ' ');
            string finaldir;

            if (!File.Exists($@"{kkman}\StandaloneUpdater.exe"))
            {
                finaldir = $@"{m_strCurrentDir}{kkman}";
            }
            else
            {
                finaldir = kkman;
            }

            string text = $@"{finaldir}\StandaloneUpdater.exe";

            string argdir = $"\u0022{marcofix}\u0022";
            string argloc = updated;
            string args = $"{argdir} {argloc}";

            if (File.Exists(text))
            {
                Process.Start(new ProcessStartInfo(text) { WorkingDirectory = $@"{finaldir}", Arguments = args });
                return;
            }
        }

        void langEnglish(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            PartyFilter("en");
        }
        void langJapanese(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            PartyFilter("ja");
        }
        void langChinese(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            PartyFilter("zh-CN");
        }
        void langKorean(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            PartyFilter("ko");
        }
        void langSpanish(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            PartyFilter("es");
        }
        void langBrazil(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            PartyFilter("pt");
        }
        void langFrench(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            PartyFilter("fr");
        }
        void langGerman(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            PartyFilter("de");
        }
        void langNorwegian(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            PartyFilter("no");
        }

        void PartyFilter(string language)
        {
            if (!noTL)
                ChangeTL(language);
            else
                SetupLang(language);
        }

        void ChangeTL(string language)
        {
            deactivateTL(1);
            WriteLangIni(language);
            SetupLang(language);
        }

        void WriteLangIni(string language)
        {
            if (File.Exists(m_strCurrentDir + "BepInEx/Config/AutoTranslatorConfig.ini"))
            {
                if (System.Windows.MessageBox.Show("Do you want the ingame language to reflect this language choice?", "Question", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    helvete(language);
                }
                // Borrowed from Marco
            }
            //MessageBox.Show($"{curAutoTLOut}", "Debug");
        }

        void helvete(string language)
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
                        {
                            contents.Insert(setToLanguage + 1, $"Language={language}");
                        }
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

        void deactivateTL(int i)
        {
            s_EnglishTL = new string[]
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
                {
                    if (File.Exists(m_strCurrentDir + file + ".dll"))
                    {
                        File.Move(m_strCurrentDir + file + ".dll", m_strCurrentDir + file + "._ll");
                    }
                }
            }
            else
            {
                foreach (var file in s_EnglishTL)
                {
                    if (File.Exists(m_strCurrentDir + file + "._ll"))
                    {
                        File.Move(m_strCurrentDir + file + "._ll", m_strCurrentDir + file + ".dll");
                    }
                    helvete("en");
                }
            }
        }

        void SetupLang(string langstring)
        {
            if (File.Exists(m_strCurrentDir + m_customDir + decideLang))
            {
                File.Delete(m_strCurrentDir + m_customDir + decideLang);
            }
            using (StreamWriter writetext = new StreamWriter(m_strCurrentDir + m_customDir + decideLang))
            {
                writetext.WriteLine(langstring);
            }
            System.Windows.Forms.Application.Restart();
        }

        private void modeDev_Checked(object sender, RoutedEventArgs e)
        {
            using (StreamWriter writetext = new StreamWriter(m_strCurrentDir + m_customDir + "/devMode"))
            {
                writetext.WriteLine("devmode: True");
            }
            if (!startup)
            {
                devMode(true);
            }
        }

        private void modeDev_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!startup)
            {
                devMode(false);
            }
        }

        void devMode(bool setDev)
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
                        contents[i] = $"Enabled = true";
                }
                else
                {
                    var i = contents.FindIndex(setToLanguage, s => s.StartsWith("Enabled"));
                    if (i > setToLanguage)
                        contents[i] = $"Enabled = false";
                }

                File.WriteAllLines(ud, contents.ToArray());
            }
            catch (Exception e)
            {
                MessageBox.Show("Something went wrong: " + e);
            }
        }

        private void checkBox_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void HoneyPotInspector_Run(object sender, RoutedEventArgs e)
        {
            if (File.Exists($"{m_strCurrentDir}\\HoneyPot\\HoneyPotInspector.exe"))
            {
                Process.Start($"{m_strCurrentDir}\\HoneyPot\\HoneyPotInspector.exe");
            }
            else
            {
                MessageBox.Show("HoneyPot doesn't seem to be applied to this installation.");
            }
        }

        private void dhh_Checked(object sender, RoutedEventArgs e)
        {
            if (File.Exists($"{m_strCurrentDir}\\Plugins\\ProjectHighHeel.dl_"))
            {
                if (File.Exists($"{m_strCurrentDir}\\Plugins\\ProjectHighHeel.dll"))
                {
                    File.Delete($"{m_strCurrentDir}\\Plugins\\ProjectHighHeel.dll");
                }
                File.Move($"{m_strCurrentDir}\\Plugins\\ProjectHighHeel.dl_", $"{m_strCurrentDir}\\Plugins\\ProjectHighHeel.dll");
            }
        }

        private void dhh_Unchecked(object sender, RoutedEventArgs e)
        {
            if (File.Exists($"{m_strCurrentDir}\\Plugins\\ProjectHighHeel.dll"))
            {
                if (File.Exists($"{m_strCurrentDir}\\Plugins\\ProjectHighHeel.dl_"))
                {
                    File.Delete($"{m_strCurrentDir}\\Plugins\\ProjectHighHeel.dl_");
                }
                File.Move($"{m_strCurrentDir}\\Plugins\\ProjectHighHeel.dll", $"{m_strCurrentDir}\\Plugins\\ProjectHighHeel.dl_");
            }
        }

        private void hp_Checked(object sender, RoutedEventArgs e)
        {
            if (File.Exists($"{m_strCurrentDir}\\Plugins\\HoneyPot.dl_"))
                MessageBox.Show("When HoneyPot is enabled, the game will use a bit longer to load in some scenes due to checking for HoneySelect assets, making it appear to be freezing for a few seconds. This is completely normal.\n\nJust disable this option again if you would rather not have that freeze.", "Information");
            if (File.Exists($"{m_strCurrentDir}\\Plugins\\HoneyPot.dl_"))
            {
                if (File.Exists($"{m_strCurrentDir}\\Plugins\\HoneyPot.dll"))
                {
                    File.Delete($"{m_strCurrentDir}\\Plugins\\HoneyPot.dll");
                }
                File.Move($"{m_strCurrentDir}\\Plugins\\HoneyPot.dl_", $"{m_strCurrentDir}\\Plugins\\HoneyPot.dll");
            }

        }

        private void hp_Unchecked(object sender, RoutedEventArgs e)
        {
            if (File.Exists($"{m_strCurrentDir}\\Plugins\\HoneyPot.dll"))
            {
                if (File.Exists($"{m_strCurrentDir}\\Plugins\\HoneyPot.dl_"))
                {
                    File.Delete($"{m_strCurrentDir}\\Plugins\\HoneyPot.dl_");
                }
                File.Move($"{m_strCurrentDir}\\Plugins\\HoneyPot.dll", $"{m_strCurrentDir}\\Plugins\\HoneyPot.dl_");
            }
        }

        private void toggle32_Checked(object sender, RoutedEventArgs e)
        {
            x86 = true;
            if (!File.Exists($"{m_customDir}{m_customDir}/toggle32.txt"))
            {
                using (StreamWriter writetext = new StreamWriter($"{m_strCurrentDir}{m_customDir}/toggle32.txt"))
                {
                    writetext.WriteLine("x86");
                }
            }
        }

        private void toggle32_Unchecked(object sender, RoutedEventArgs e)
        {
            x86 = false;
            if (File.Exists($"{m_strCurrentDir}{m_customDir}/toggle32.txt"))
            {
                File.Delete($"{m_strCurrentDir}{m_customDir}/toggle32.txt");
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }
    }
}