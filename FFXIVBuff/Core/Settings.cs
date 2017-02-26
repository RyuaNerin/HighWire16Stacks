﻿using System.ComponentModel.Composition;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using Newtonsoft.Json;

namespace FFXIVBuff.Core
{
    [Export(typeof(Settings))]
    [JsonObject(MemberSerialization.OptIn)]
    internal class Settings : DependencyObject
    {
        private static readonly Settings m_instance;
        public static Settings Instance { get { return Settings.m_instance; } }

        private readonly static JsonSerializer JSerializer = new JsonSerializer { Formatting = Formatting.Indented };
        private readonly static string SettingFilePath;

        static Settings()
        {
            Settings.SettingFilePath = Path.GetFullPath(Assembly.GetExecutingAssembly().Location) + ".cnf";

            if (!File.Exists(SettingFilePath))
            {
                Settings.m_instance = new Settings();
            }
            else
            {
                using (var sreader = new StreamReader(Settings.SettingFilePath, Encoding.UTF8))
                using (var jreader = new JsonTextReader(sreader))
                {
                    Settings.m_instance = JSerializer.Deserialize<Settings>(jreader);
                }
            }
        }

        public void Load()
        { }

        public void Save()
        {
            using (var swriter = new StreamWriter(Settings.SettingFilePath, false, Encoding.UTF8))
                JSerializer.Serialize(swriter, this);
        }

        private static readonly DependencyProperty LeftDP
            = DependencyProperty.Register("Left", typeof(double), typeof(Settings), new FrameworkPropertyMetadata(200d));
        [JsonProperty]
        public double Left
        {
            get { return (double)this.GetValue(LeftDP); }
            set { this.SetValue(LeftDP, value); }
        }

        private static readonly DependencyProperty TopDP
            = DependencyProperty.Register("Top", typeof(double), typeof(Settings), new FrameworkPropertyMetadata(200d));
        [JsonProperty]
        public double Top
        {
            get { return (double)this.GetValue(TopDP); }
            set { this.SetValue(TopDP, value); }
        }

        private static readonly DependencyProperty OpacityDP
            = DependencyProperty.Register("Opacity", typeof(double), typeof(Settings), new FrameworkPropertyMetadata(0d));
        [JsonProperty]
        public double Opacity
        {
            get { return (double)this.GetValue(OpacityDP); }
            set { this.SetValue(OpacityDP, value); }
        }

        private static readonly DependencyProperty ScaleDP
            = DependencyProperty.Register("Scale", typeof(double), typeof(Settings), new FrameworkPropertyMetadata(1d));
        [JsonProperty]
        public double Scale
        {
            get { return (double)this.GetValue(ScaleDP); }
            set { this.SetValue(ScaleDP, value); }
        }

        private static readonly DependencyProperty RefreshTimeDP
            = DependencyProperty.Register("RefreshTime", typeof(double), typeof(Settings), new FrameworkPropertyMetadata(100d, PropertyChangedCallback));
        [JsonProperty]
        public double RefreshTime
        {
            get { return (double)this.GetValue(RefreshTimeDP); }
            set { this.SetValue(RefreshTimeDP, value); }
        }

        private static readonly DependencyProperty ClickThroughDP
            = DependencyProperty.Register("ClickThrough", typeof(bool), typeof(Settings), new FrameworkPropertyMetadata(false, PropertyChangedCallback));
        [JsonProperty]
        public bool ClickThrough
        {
            get { return (bool)this.GetValue(ClickThroughDP); }
            set { this.SetValue(ClickThroughDP, value); }
        }

        private static readonly DependencyProperty ShowDecimalDP
            = DependencyProperty.Register("ShowDecimal", typeof(bool), typeof(Settings), new FrameworkPropertyMetadata(false));
        [JsonProperty]
        public bool ShowDecimal
        {
            get { return (bool)this.GetValue(ShowDecimalDP); }
            set { this.SetValue(ShowDecimalDP, value); }
        }

        private static readonly DependencyProperty ShowingModeDP
            = DependencyProperty.Register("ShowingMode", typeof(bool), typeof(Settings), new FrameworkPropertyMetadata(false));
        [JsonProperty]
        public bool ShowingMode
        {
            get { return (bool)this.GetValue(ShowingModeDP); }
            set { this.SetValue(ShowingModeDP, value); }
        }
        
        private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == RefreshTimeDP)
                Worker.Delay = (int)(double)e.NewValue;

            else if (e.Property == ClickThroughDP)
                Worker.SetClickThrough((bool)e.NewValue);
        }
    }
}
