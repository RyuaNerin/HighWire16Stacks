﻿using System.ComponentModel.Composition;
using System.IO;
using System.Reflection;
using System.Windows;
using HighWire16Stacks.Utilities;
using Newtonsoft.Json;
using System;

namespace HighWire16Stacks.Core
{
    [Export(typeof(Settings))]
    [JsonObject(MemberSerialization.OptIn)]
    internal class Settings : DependencyObject
    {
        private static readonly Settings m_instance = new Settings();
        public static Settings Instance { get { return Settings.m_instance; } }

        private readonly static JsonSerializer JSerializer = new JsonSerializer { Formatting = Formatting.Indented };
        private readonly static string SettingFilePath;

        static Settings()
        {
            Settings.SettingFilePath = Path.GetFullPath(Assembly.GetExecutingAssembly().Location) + ".cnf";
        }

        public static void Load()
        {
            if (File.Exists(SettingFilePath))
            {
                try
                {
                    using (var fr = File.OpenRead(Settings.SettingFilePath))
#if DEBUG
                    using (var sr = new StreamReader(fr, System.Text.Encoding.UTF8))
                    using (var jr = new JsonTextReader(sr))
#else
                    using (var sr = new BinaryReader(fr))
                    using (var jr = new Newtonsoft.Json.Bson.BsonReader(fr))
#endif
                    {
                        JSerializer.Populate(jr, Settings.m_instance);
                    }
                }
                catch
                {
                }
            }
        }

        public void Save()
        {
            try
            {
                using (var fw = new FileStream(Settings.SettingFilePath, FileMode.OpenOrCreate))
                {
                    fw.SetLength(0);

#if DEBUG
                    using (var sw = new StreamWriter(fw, System.Text.Encoding.UTF8))
                    using (var jw = new JsonTextWriter(sw))
#else
                    using (var sw = new BinaryWriter(fw))
                    using (var jw = new Newtonsoft.Json.Bson.BsonWriter(sw))
#endif
                    JSerializer.Serialize(jw, this);
                }
            }
            catch
            {
            }
        }

        private static readonly DependencyProperty OverlayLeftDP
            = DependencyProperty.Register("OverlayLeft", typeof(double), typeof(Settings), new FrameworkPropertyMetadata(200d));
        [JsonProperty]
        public double OverlayLeft
        {
            get { return (double)this.GetValue(OverlayLeftDP); }
            set { this.SetValue(OverlayLeftDP, value); }
        }

        private static readonly DependencyProperty OverlayTopDP
            = DependencyProperty.Register("OverlayTop", typeof(double), typeof(Settings), new FrameworkPropertyMetadata(200d));
        [JsonProperty]
        public double OverlayTop
        {
            get { return (double)this.GetValue(OverlayTopDP); }
            set { this.SetValue(OverlayTopDP, value); }
        }

        private static readonly DependencyProperty OpacityDP
            = DependencyProperty.Register("Opacity", typeof(double), typeof(Settings), new FrameworkPropertyMetadata(1d));
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

        private static readonly DependencyProperty OverlayFPSDP
            = DependencyProperty.Register("OverlayFPS", typeof(double), typeof(Settings), new FrameworkPropertyMetadata(30d, PropertyChangedCallback));
        [JsonProperty]
        public double OverlayFPS
        {
            get { return (double)this.GetValue(OverlayFPSDP); }
            set { this.SetValue(OverlayFPSDP, value); }
        }

        public int OverlayRefreshCycle
        {
            get { return (int)Math.Ceiling(1000 / OverlayFPS); }
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

        private static readonly DependencyProperty AutoHideDP
            = DependencyProperty.Register("AutoHide", typeof(bool), typeof(Settings), new FrameworkPropertyMetadata(false, PropertyChangedCallback));
        [JsonProperty]
        public bool AutoHide
        {
            get { return (bool)this.GetValue(AutoHideDP); }
            set { this.SetValue(AutoHideDP, value); }
        }

        private static readonly DependencyProperty SortByTimeDP
            = DependencyProperty.Register("SortByTime", typeof(bool), typeof(Settings), new FrameworkPropertyMetadata(false, PropertyChangedCallback));
        [JsonProperty]
        public bool SortByTime
        {
            get { return (bool)this.GetValue(SortByTimeDP); }
            set { this.SetValue(SortByTimeDP, value); }
        }

        private SortedList<int> m_checkedList = new SortedList<int>();
        [JsonProperty]
        public int[] Checekd
        {
            get
            {
                lock (this.m_checkedList)
                    return this.m_checkedList.ToArray();
            }
            set
            {
                lock (this.m_checkedList)
                    for (int i = 0; i < value.Length; ++i)
                        this.m_checkedList.AddRange(value);
            }
        }

        public void SetChecked(bool isChecked, int id)
        {
            lock (this.m_checkedList)
            {
                if (isChecked)
                    this.m_checkedList.Add(id);
                else
                    this.m_checkedList.Remove(id);
            }
        }

        public bool IsChecked(int id)
        {
            lock (this.m_checkedList)
                return this.m_checkedList.Contains(id);
        }
        
        private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == OverlayFPSDP)
                Worker.SetDelay(((Settings)d).OverlayRefreshCycle);

            else if (e.Property == ClickThroughDP)
                Worker.SetClickThrough((bool)e.NewValue);
            
            else if (e.Property == AutoHideDP)
                Worker.SetAutohide((bool)e.NewValue);

            else if (e.Property == SortByTimeDP)
                Worker.OverlayInstance.SetSortByTime((bool)e.NewValue);
        }
    }
}