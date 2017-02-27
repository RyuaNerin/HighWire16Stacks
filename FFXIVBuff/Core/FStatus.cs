using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Media;

namespace FFXIVBuff.Core
{
    [DebuggerDisplay("[{Id}] {Name} : {Desc}")]
    internal class FStatus : IComparable, IComparable<FStatus>, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public FStatus(int id, string name, string desc, int icon, bool isBuff)
        {
            this.Id     = id;
            this.Icon   = icon;
            this.Name   = name;
            this.Desc   = desc;
            this.IsBuff = isBuff;
        }
        public int    Id     { get; private set; }
        public int    Icon   { get; private set; }
        public string Name   { get; private set; }
        public string Desc   { get; private set; }
        public bool   IsBuff { get; private set; }

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

                if (this.PropertyChanged != null)
                    this.PropertyChanged(this, new PropertyChangedEventArgs("IsChceked"));
            }
        }

        public int CompareTo(object obj)
        {
            var status = obj as FStatus;
            if (status == null)
                return -1;
            else
                return this.CompareTo(status);
        }

        public int CompareTo(FStatus obj)
        {
            return this.Id.CompareTo(obj.Id);
        }
    }
}
