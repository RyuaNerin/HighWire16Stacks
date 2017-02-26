using System;
using System.ComponentModel;
using System.Diagnostics;

namespace FFXIVBuff.Core
{
    [DebuggerDisplay("[{FStatus.Id}] {FStatus.Name} ({Param})")]
    internal class UStatus : IComparable, IComparable<UStatus>, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public FStatus  FStatus { get; private set; }
        public int      Icon    { get; private set; }
        public float    Remain  { get; private set; }

        public int      Id      { get; private set; }
        public bool     Visible { get { return this.FStatus != null; } }

        private int m_iconIndex = -1;

        public void Update()
        {
            this.Id      = 0;
            this.FStatus = FResource.StatusListDic[0];
            if (this.PropertyChanged != null)
                this.PropertyChanged.BeginInvoke(this, new PropertyChangedEventArgs("Visible"), null, null);
        }
        public void Update(FStatus fstatus, int iconIndex, float remain)
        {
            this.Id      = fstatus.Id;
            this.FStatus = fstatus;
            this.Icon    = fstatus.Icon + iconIndex;
            this.Remain  = remain;

            if (this.PropertyChanged != null)
            {
                this.PropertyChanged.BeginInvoke(this, new PropertyChangedEventArgs("Visible"), null, null);
                this.PropertyChanged.BeginInvoke(this, new PropertyChangedEventArgs("Icon"), null, null);
                this.PropertyChanged.BeginInvoke(this, new PropertyChangedEventArgs("Remain"), null, null);
            }
        }
        public void Update(int iconIndex, float remain)
        {
            bool iconUpdated = this.m_iconIndex == iconIndex;;

            this.Remain = remain;
            if (iconUpdated)
            {
                this.Icon = this.FStatus.Icon + iconIndex;
                this.m_iconIndex = iconIndex;
            }

            if (this.PropertyChanged != null)
            {
                this.PropertyChanged.BeginInvoke(this, new PropertyChangedEventArgs("Remain"), null, null);

                if (iconUpdated)
                    this.PropertyChanged.BeginInvoke(this, new PropertyChangedEventArgs("Icon"), null, null);
            }
        }

        public int CompareTo(object obj)
        {
            var status = obj as UStatus;
            if (status == null)
                return -1;
            else
                return this.CompareTo(status);
        }

        public int CompareTo(UStatus obj)
        {
            return this.FStatus.CompareTo(obj.FStatus);
        }
    }
}
