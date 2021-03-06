﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;
using Microsoft.Win32;

namespace InitSetting
{
    public class SettingManager
    {
        public ConfigSetting CurrentSettings { get; private set; } = new ConfigSetting();

        public IEnumerable<DisplayMode> DefaultSettingList = new List<DisplayMode>
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

        private readonly List<DisplayModes> _displayModes = new List<DisplayModes>();
        public DisplayModes GetDisplayModes(int nDisplay) => _displayModes[nDisplay];

        [DllImport("user32.dll")]
        private static extern bool EnumDisplaySettings(string deviceName, int modeNum, ref DEVMODE devMode);

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        public void SaveRegistry(string keyPath)
        {
            using (var registryKey = Registry.CurrentUser.CreateSubKey(keyPath))
            {
                registryKey.SetValue("Screenmanager Is Fullscreen mode_h3981298716", CurrentSettings.FullScreen ? 1 : 0);
                registryKey.SetValue("Screenmanager Resolution Height_h2627697771", CurrentSettings.Height);
                registryKey.SetValue("Screenmanager Resolution Width_h182942802", CurrentSettings.Width);
                registryKey.SetValue("UnityGraphicsQuality_h1669003810", 2);
                registryKey.SetValue("UnitySelectMonitor_h17969598", CurrentSettings.Display);
            }
        }

        public void SaveConfigFile(string configFilePath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(configFilePath) ?? throw new InvalidOperationException("Invalid config path " + configFilePath));

            using (var fileStream = new FileStream(configFilePath, FileMode.Create))
            using (var streamWriter = new StreamWriter(fileStream, Encoding.GetEncoding("UTF-16")))
            {
                var xmlSerializerNamespaces = new XmlSerializerNamespaces();
                xmlSerializerNamespaces.Add(string.Empty, string.Empty);
                new XmlSerializer(typeof(ConfigSetting)).Serialize(streamWriter, CurrentSettings, xmlSerializerNamespaces);
            }
        }

        private void getDisplayMode_EnumDisplaySettings(int numDisplay)
        {
            // todo this method needs cleanup

            var displayDevice = default(DISPLAY_DEVICE);
            displayDevice.cb = Marshal.SizeOf(displayDevice);
            var allDisplayNames = new List<string>();
            var dispNum = 0u;
            while (EnumDisplayDevices(null, dispNum, ref displayDevice, 1u))
            {
                if ((displayDevice.StateFlags & DisplayDeviceStateFlags.AttachedToDesktop) ==
                    DisplayDeviceStateFlags.AttachedToDesktop) allDisplayNames.Add(displayDevice.DeviceName);
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
                    var w = devmode.dmPelsWidth;
                    var h = devmode.dmPelsHeight;
                    if ((num4 != w || num5 != h) && devmode.dmBitsPerPel == 32)
                    {
                        var displayMode = DefaultSettingList.FirstOrDefault(dis => dis.Width == w && dis.Height == h);
                        if (displayMode.Width != 0) list2.Add(displayMode);
                        num4 = w;
                        num5 = h;
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
                _displayModes.Add(item);
            }

            if (_displayModes.Count == 0 || _displayModes.Count != numDisplay)
                MessageBox.Show("Failed to list supported resolutions");

            if (primaryIndex < 0) return;
            _displayModes.Insert(0, _displayModes[primaryIndex]);
            _displayModes.RemoveAt(primaryIndex + 1);
        }

        public bool SetFullScreen(bool fullScreenEnabled)
        {
            var currentSettingsDisplay = CurrentSettings.Display;
            if (_displayModes[currentSettingsDisplay].list.Count == 0)
            {
                CurrentSettings.FullScreen = false;
                return false;
            }
            CurrentSettings.FullScreen = fullScreenEnabled;
            if (_displayModes[currentSettingsDisplay].list.Find((DisplayMode x) => x.text.Contains(CurrentSettings.Size)).Width == 0)
            {
                CurrentSettings.Size = _displayModes[currentSettingsDisplay].list[0].text;
                CurrentSettings.Width = _displayModes[currentSettingsDisplay].list[0].Width;
                CurrentSettings.Height = _displayModes[currentSettingsDisplay].list[0].Height;
            }
            return true;
        }

        public struct DisplayMode
        {
            public int Width;

            public int Height;

            public string text;
        }

        public struct DisplayModes
        {
            public int x;

            public int y;

            public List<DisplayMode> list;
        }

        public SettingManager()
        {
            var num = Screen.AllScreens.Length;
            getDisplayMode_EnumDisplaySettings(num);
            CurrentSettings.Size = "1280 x 720 (16 : 9)";
            CurrentSettings.Width = 1280;
            CurrentSettings.Height = 720;
            CurrentSettings.Quality = 1;
            CurrentSettings.Language = 0;
            CurrentSettings.Display = 0;
            CurrentSettings.FullScreen = false;
        }

        public void LoadSettings(string path)
        {
            using (var fileStream = new FileStream(path, FileMode.Open))
            {
                var xmlSerializer = new XmlSerializer(typeof(ConfigSetting));
                CurrentSettings = (ConfigSetting)xmlSerializer.Deserialize(fileStream);
            }
        }
    }
}