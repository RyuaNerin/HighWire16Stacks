using System;
using System.ComponentModel;
using System.Diagnostics;

namespace HighWire16Stacks.Core
{
    [DebuggerDisplay("[{Id}] {Name} : {Desc}")]
    internal class FStatus : IComparable<FStatus>, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public FStatus(int id, string name, string desc, int icon, int iconStack, bool isBad, bool isNonExpries, bool isFists, bool isChecked)
        {
            this.Id             = id;
            this.Icon           = icon;
            this.Name           = name;
            this.Desc           = desc;
            this.IsDebuff       = isBad;
            this.IconCount      = iconStack;
            this.IsNonExpries   = isNonExpries;
            this.IsFists        = isFists;
            
            this.IsChecked      = isChecked;
        }

        /// <summary>A</summary>
        public int      Id              { get; private set; }
        /// <summary>B</summary>
        public int      Icon            { get; private set; }
        /// <summary>C</summary>
        public string   Name            { get; private set; }
        /// <summary>D</summary>
        public string   Desc            { get; private set; }
        /// <summary>E</summary>
        public int      IconCount       { get; private set; }
        /// <summary>F==2</summary>
        public bool     IsDebuff        { get; private set; }
        /// <summary>O</summary>
        public bool     IsNonExpries    { get; private set; }
        /// <summary>P</summary>
        public bool     IsFists         { get; private set; }
        
        private bool m_isChecked;
        public bool IsChecked
        {
            get
            {
                return this.m_isChecked;
            }
            set
            {
                this.m_isChecked = value;

                if (value)
                    Settings.Instance.Checked.Add(this.Id);
                else
                    Settings.Instance.Checked.Remove(this.Id);

                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsChecked"));
            }
        }

        public int CompareTo(FStatus obj)
        {
            return this.Id.CompareTo(obj.Id);
        }
    }
}
