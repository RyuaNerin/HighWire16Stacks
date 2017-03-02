﻿using System;
using System.ComponentModel;
using System.Diagnostics;

namespace FFXIVBuff.Core
{
    [DebuggerDisplay("[{FStatus.Id}] {FStatus.Name}")]
    internal class UStatus : INotifyPropertyChanged
    {
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

        public void Clear()
        {
            if (this.m_fstatus != null)
                this.m_fstatus.PropertyChanged -= FStatus_PropertyChanged;

            this.m_id           = 0;
            this.m_visible      = false;
            this.m_fstatus      = FResource.StatusListDic[0];
            this.m_iconIndex    = -1;

            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs("Visible"));
        }
        public void Update(int id, int param, float remain)
        {
            bool visibleUpdated = false;
            bool iconUpdated = false;

            visibleUpdated = this.m_id != id;
            if (visibleUpdated)
            {
                if (this.m_fstatus != null)
                    this.m_fstatus.PropertyChanged -= FStatus_PropertyChanged;

                this.m_visible   = true;
                this.m_id        = id;
                this.m_fstatus   = FResource.StatusListDic[id];
                this.m_isChecked = this.m_fstatus.IsChecked;

                this.m_fstatus.PropertyChanged += FStatus_PropertyChanged;

                iconUpdated = true;
            }
            else
            {
                iconUpdated = this.m_iconIndex != param;
            }

            if (iconUpdated)
            {
                this.m_iconIndex = param;
                this.m_icon = this.m_fstatus.Icon;

                if (this.m_fstatus.IconCount != 0 && param <= this.m_fstatus.IconCount)
                    this.m_icon += param - 1;
            }

            this.m_remain = this.m_fstatus.IsNonExpries ? 0 : remain;

            if (this.PropertyChanged != null)
            {
                if (visibleUpdated)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("Visible"));
                    this.PropertyChanged(this, new PropertyChangedEventArgs("IsChecked"));
                }

                if (iconUpdated)
                    this.PropertyChanged(this, new PropertyChangedEventArgs("Icon"));

                this.PropertyChanged(this, new PropertyChangedEventArgs("Remain"));
            }
        }

        private void FStatus_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.m_isChecked = this.m_fstatus.IsChecked;

            if (this.PropertyChanged != null)
                this.PropertyChanged(this, new PropertyChangedEventArgs("IsChecked"));
        }
    }
}
