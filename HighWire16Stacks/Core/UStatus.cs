using System.ComponentModel;
using System.Diagnostics;

namespace HighWire16Stacks.Core
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
        public FStatus FStatus => this.m_fstatus;
        
        private int m_iconIndex = -1;
        private int m_icon;
        public int Icon => this.m_icon;

        private float m_remain;
        public float Remain => this.m_remain;

        private bool m_visible;
        public bool Visible => this.m_visible;

        private bool m_isChecked;
        public bool IsChecked => this.m_isChecked;

        private bool m_isCount;
        public bool IsCount => this.m_isCount;

        private bool m_own;
        public bool Own => this.m_own;

        public void Clear()
        {
            if (this.m_id == 0)
                return;

            if (this.m_fstatus != null)
                this.m_fstatus.PropertyChanged -= this.FStatus_PropertyChanged;

            this.m_id           = 0;
            this.m_visible      = false;
            this.m_fstatus      = FResource.StatusListDic[0];
            this.m_iconIndex    = -1;

            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Visible"));
        }
        public bool Update(int id, int param, float remain, bool own)
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
                    this.m_fstatus.PropertyChanged -= this.FStatus_PropertyChanged;

                this.m_visible   = true;
                this.m_id        = id;
                this.m_fstatus   = FResource.StatusListDic[id];
                this.m_isChecked = this.m_fstatus.IsChecked;

                this.m_isCount   = remain == 0 && this.FStatus.IconRange == 0 && (!this.m_fstatus.IsNonExpries || (param > 0 && !this.FStatus.IsStance));

                this.m_fstatus.PropertyChanged += this.FStatus_PropertyChanged;

                iconUpdated = true;
            }
            else if (!this.m_isCount)
            {
                iconUpdated = this.m_iconIndex != param;
            }            

            if (iconUpdated)
            {
                this.m_iconIndex = param;
                this.m_icon = this.m_fstatus.Icon;

                if (this.m_fstatus.IconRange > 0 && param <= this.m_fstatus.IconRange)
                    this.m_icon += param - 1;
            }

            bool remainChanged = false;
            if (this.m_isCount)
            {
                this.m_remain = param;
                remainChanged = true;
            }
            else if (!this.m_fstatus.IsNonExpries)
            {
                this.m_remain = remain < 0 ? remain * -1: remain;
                remainChanged = true;
            }
            else if (this.m_remain != 0)
            {
                this.m_remain = 0;
                remainChanged = true;
            }

            if (this.PropertyChanged != null)
            {
                if (visibleUpdated)
                {
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Visible"));
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsCount"));
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsChecked"));
                }

                if (iconUpdated)
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Icon"));

                if (remainChanged)
                    this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Remain"));
            }

            return result;
        }

        private void FStatus_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.m_isChecked = this.m_fstatus.IsChecked;

            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsChecked"));
        }

        public static int CompareWithTime(UStatus a, UStatus b)
        {
            if (a.m_fstatus == null && b.m_fstatus == null) return  0;
            if (a.m_fstatus == null && b.m_fstatus != null) return -1;
            if (a.m_fstatus != null && b.m_fstatus == null) return  1;
            
            if ( a.m_own && !b.m_own) return -1;
            if (!a.m_own &&  b.m_own) return  1;

            if ( a.m_fstatus.IsDebuff && !b.m_fstatus.IsDebuff) return -1;
            if (!a.m_fstatus.IsDebuff &&  b.m_fstatus.IsDebuff) return  1;

            var compare = a.m_remain.CompareTo(b.m_remain);
            if (compare != 0)
                return compare;

            return a.m_index.CompareTo(b.m_index);
        }

        public static int Compare(UStatus a, UStatus b)
        {
            if (a.m_fstatus == null && b.m_fstatus == null) return  0;
            if (a.m_fstatus == null && b.m_fstatus != null) return -1;
            if (a.m_fstatus != null && b.m_fstatus == null) return  1;
            
            if ( a.m_own && !b.m_own) return -1;
            if (!a.m_own &&  b.m_own) return  1;

            if ( a.m_fstatus.IsDebuff && !b.m_fstatus.IsDebuff) return -1;
            if (!a.m_fstatus.IsDebuff &&  b.m_fstatus.IsDebuff) return  1;

            return a.m_index.CompareTo(b.m_index);
        }
    }
}
