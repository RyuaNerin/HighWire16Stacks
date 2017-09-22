using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HighWire16Stacks.Core
{
    public enum ShowingModes
    {
        ShowAll = 0,
        ShowChecked = 1,
        HideChecked = 2
    }

    [JsonObject(MemberSerialization.OptIn)]
    internal class Settings : INotifyPropertyChanged
    {
        private static readonly Settings instance = new Settings();
        public static Settings Instance => Settings.instance;

        private readonly static JsonSerializer JSerializer = new JsonSerializer { Formatting = Formatting.Indented };
        private readonly static string SettingFilePath;

        static Settings()
        {
            Settings.SettingFilePath = Path.ChangeExtension(App.ExeLocation, ".cnf");
        }

        public static void Load()
        {
            if (File.Exists(SettingFilePath))
            {
                try
                {
                    using (var fr = File.OpenRead(Settings.SettingFilePath))
                    using (var sr = new StreamReader(fr, System.Text.Encoding.UTF8))
                    using (var jr = new JsonTextReader(sr))
                        JSerializer.Populate(jr, Settings.instance);
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
                    
                    using (var sw = new StreamWriter(fw, System.Text.Encoding.UTF8))
                    using (var jw = new JsonTextWriter(sw))
                        JSerializer.Serialize(jw, this);
                }
            }
            catch
            {
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        
        private double m_overlayLeft = 200;
        [JsonProperty]
        public double OverlayLeft
        {
            get => this.m_overlayLeft;
            set
            {
                this.m_overlayLeft = value;
                this.OnPropertyChanged();
            }
        }
        
        private double m_overlayTop = 200;
        [JsonProperty]
        public double OverlayTop
        {
            get => this.m_overlayTop;
            set
            {
                this.m_overlayTop = value;
                this.OnPropertyChanged();
            }
        }
        
        private double m_opacity = 1;
        [JsonProperty]
        public double Opacity
        {
            get => this.m_opacity;
            set
            {
                this.m_opacity = value;
                this.OnPropertyChanged();
            }
        }
        
        private double m_scale = 1;
        [JsonProperty]
        public double Scale
        {
            get => this.m_scale;
            set
            {
                this.m_scale = value;
                this.OnPropertyChanged();
            }
        }
        
        private double m_overlayFps = 30;
        [JsonProperty]
        public double OverlayFPS
        {
            get => this.m_overlayFps;
            set
            {
                this.m_overlayFps = value;
                Worker.SetDelay((int)Math.Ceiling(1000 / value));

                this.OnPropertyChanged();
            }
        }

        private bool m_clickThrough = false;
        [JsonProperty]
        public bool ClickThrough
        {
            get => this.m_clickThrough;
            set
            {
                this.m_clickThrough = value;
                Worker.SetClickThrough(value);
                this.OnPropertyChanged();
            }
        }
        
        private bool m_showDecimal = false;
        [JsonProperty]
        public bool ShowDecimal
        {
            get => this.m_showDecimal;
            set
            {
                this.m_showDecimal = value;
                Worker.SetClickThrough(value);
                this.OnPropertyChanged();
            }
        }
        
        private ShowingModes m_showMode = ShowingModes.ShowAll;
        [JsonProperty]
        public ShowingModes ShowingMode
        {
            get => this.m_showMode;
            set
            {
                this.m_showMode = value;
                this.OnPropertyChanged();
            }
        }
        
        private bool m_autoHide = false;
        [JsonProperty]
        public bool AutoHide
        {
            get => this.m_autoHide;
            set
            {
                this.m_autoHide = value;
                Worker.SetAutohide(value);
                this.OnPropertyChanged();
            }
        }

        private bool m_sortByTime;
        [JsonProperty]
        public bool SortByTime
        {
            get => this.m_sortByTime;
            set
            {
                this.m_sortByTime = value;
                Worker.OverlayInstance?.SetSortByTime(value);
                this.OnPropertyChanged();
            }
        }

        private bool m_useWaifu2x;
        [JsonProperty]
        public bool UseWaifu2x
        {
            get => this.m_useWaifu2x;
            set
            {
                this.m_useWaifu2x = value;
                this.OnPropertyChanged();
            }
        }

        private readonly HashSet<int> m_checkedList = new HashSet<int>();
        [JsonProperty]
        [JsonConverter(typeof(HashSetConverter))]
        public HashSet<int> Checked => this.m_checkedList;

        public bool IsChecked(int id)
        {
            lock (this.m_checkedList)
                return this.m_checkedList.Contains(id);
        }

        public class HashSetConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(HashSet<int>);
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var hashSet = (HashSet<int>)existingValue;
                hashSet.UnionWith(JArray.Load(reader).Cast<int>());

                return hashSet;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var hashSet = (HashSet<int>)value;
                var jo = new JArray(hashSet);
                jo.WriteTo(writer);
            }
        }
    }
}
