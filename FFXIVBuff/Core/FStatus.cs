using System;
using System.ComponentModel;
using System.Diagnostics;

namespace FFXIVBuff.Core
{
    [DebuggerDisplay("[{Id}] {Name} : {Desc}")]
    internal class FStatus : IComparable<FStatus>, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public FStatus(int id, string name, string desc, int icon, int iconStack, bool isBad, bool isNonExpries, bool isChecked)
        {
            this.Id             = id;
            this.Icon           = icon;
            this.Name           = name;
            this.Desc           = desc;
            this.IsDebuff       = isBad;
            this.IconCount      = iconStack;
            this.IsNonExpries   = isNonExpries;
            
            this.IsChecked      = isChecked;
        }
        public int      Id              { get; private set; }
        public int      Icon            { get; private set; }
        public string   Name            { get; private set; }
        public string   Desc            { get; private set; }
        public int      IconCount       { get; private set; }
        public bool     IsDebuff        { get; private set; }
        public bool     IsNonExpries    { get; private set; }


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

                Settings.Instance.SetChecked(value, this.Id);

                if (this.PropertyChanged != null)
                    this.PropertyChanged(this, new PropertyChangedEventArgs("IsChceked"));
            }
        }

        public int CompareTo(FStatus obj)
        {
            return this.Id.CompareTo(obj.Id);
        }
    }
}
