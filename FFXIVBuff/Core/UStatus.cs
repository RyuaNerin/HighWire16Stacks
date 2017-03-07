using System.ComponentModel;
using System.Diagnostics;

namespace FFXIVBuff.Core
{
    [DebuggerDisplay("[{FStatus.Id}] {FStatus.Name}")]
    internal class UStatus : INotifyPropertyChanged
    {
        private readonly int m_index;
        public UStatus(int index)
        {
            this.m_index = index;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        
        private int m_id;
        private FStatus m_fstatus;
        public FStatus FStatus { get { return this.m_fstatus; } }
        
        private int m_iconIndex = -1;
        private int m_icon;
        public int Icon { get { return this.m_icon; } }

        private float m_remain;
        public float Remain { get { return this.m_remain; } }

        private bool m_visible;
        public bool Visible   { get { return this.m_visible; } }

        private bool m_isChecked;
        public bool IsChecked { get { return this.m_isChecked; } }

        private bool m_isCount;
        public bool IsCount { get { return this.m_isCount; } }

        public void Clear()
        {
            if (this.m_id == 0)
                return;

            if (this.m_fstatus != null)
                this.m_fstatus.PropertyChanged -= FStatus_PropertyChanged;

            this.m_id           = 0;
            this.m_visible      = false;
            this.m_fstatus      = FResource.StatusListDic[0];
            this.m_iconIndex    = -1;

            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs("Visible"));
        }
        public bool Update(int id, int param, float remain)
        {
            bool result = false;

            bool visibleUpdated = false;
            bool iconUpdated = false;

            visibleUpdated = this.m_id != id;
            if (visibleUpdated)
            {
                result = true;
                if (!FResource.StatusListDic.ContainsKey(id))
                {
                    this.Clear();
                    return true;
                }

                if (this.m_fstatus != null)
                    this.m_fstatus.PropertyChanged -= FStatus_PropertyChanged;

                this.m_visible   = true;
                this.m_id        = id;
                this.m_fstatus   = FResource.StatusListDic[id];
                this.m_isChecked = this.m_fstatus.IsChecked;

                this.m_isCount   = remain == 0 && !this.m_fstatus.IsNonExpries && param > 0;

                this.m_fstatus.PropertyChanged += FStatus_PropertyChanged;

                iconUpdated = true;
                result = true;
            }
            else if (!this.m_isCount)
            {
                iconUpdated = this.m_iconIndex != param;
            }            

            if (iconUpdated)
            {
                this.m_iconIndex = param;
                this.m_icon = this.m_fstatus.Icon;

                if (this.m_fstatus.IconCount > 0 && param <= this.m_fstatus.IconCount)
                    this.m_icon += param - 1;
            }

            if (this.m_isCount)
                this.m_remain = param;
            else
                this.m_remain = this.m_fstatus.IsNonExpries ? 0 : remain;

            if (this.PropertyChanged != null)
            {
                if (visibleUpdated)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("Visible"));
                    this.PropertyChanged(this, new PropertyChangedEventArgs("IsCount"));
                    this.PropertyChanged(this, new PropertyChangedEventArgs("IsChecked"));
                }

                if (iconUpdated)
                    this.PropertyChanged(this, new PropertyChangedEventArgs("Icon"));

                this.PropertyChanged(this, new PropertyChangedEventArgs("Remain"));
            }

            return result;
        }

        private void FStatus_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.m_isChecked = this.m_fstatus.IsChecked;

            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs("IsChecked"));
        }

        public static int CompareWithTime(UStatus a, UStatus b)
        {
            if (a.m_fstatus == null && b.m_fstatus == null) return  0;
            if (a.m_fstatus == null && b.m_fstatus != null) return  1;
            if (a.m_fstatus != null && b.m_fstatus == null) return -1;

            if (a.m_fstatus != null && b.m_fstatus != null)
            {
                if ( a.m_fstatus.IsDebuff && !b.m_fstatus.IsDebuff) return  1;
                if (!a.m_fstatus.IsDebuff &&  b.m_fstatus.IsDebuff) return -1;
            }

            var compare = a.m_remain.CompareTo(b.m_remain);
            if (compare != 0)
                return compare;
            
            return a.m_index.CompareTo(b.m_index);
        }

        public static int Compare(UStatus a, UStatus b)
        {
            if (a.m_fstatus == null && b.m_fstatus == null) return 0;
            if (a.m_fstatus == null && b.m_fstatus != null) return 1;
            if (a.m_fstatus != null && b.m_fstatus == null) return -1;

            if (a.m_fstatus != null && b.m_fstatus != null)
            {
                if (a.m_fstatus.IsDebuff && !b.m_fstatus.IsDebuff) return 1;
                if (!a.m_fstatus.IsDebuff &&  b.m_fstatus.IsDebuff) return -1;
            }

            return a.m_index.CompareTo(b.m_index);
        }
    }
}
