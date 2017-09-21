using System;
using System.ComponentModel;
using System.Diagnostics;
using CsvHelper.Configuration;

namespace HighWire16Stacks.Core
{
    [DebuggerDisplay("[{Id}] {Name} : {Desc}")]
    internal class FStatus : IComparable<FStatus>, INotifyPropertyChanged
    {
        public class Map : CsvClassMap<FStatus>
        {
            public Map()
            {
                Map(e => e.Id          ).Index('A' - 'A');
                Map(e => e.Name        ).Index('B' - 'A');
                Map(e => e.Desc        ).Index('C' - 'A');
                Map(e => e.Icon        ).Index('D' - 'A');
                Map(e => e.IconRange   ).Index('E' - 'A');
                Map(e => e.IsDebuff    ).Index('F' - 'A');
                Map(e => e.IsNonExpries).Index('O' - 'A');
                Map(e => e.IsStance    ).Index('P' - 'A');
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        
        public int      Id              { get; private set; }
        public int      Icon            { get; private set; }
        public string   Name            { get; private set; }
        public string   Desc            { get; private set; }
        public int      IconRange       { get; private set; }
        public bool     IsDebuff        { get; private set; }
        public bool     IsNonExpries    { get; private set; }
        public bool     IsStance        { get; private set; }
        
        public bool IsChecked
        {
            get
            {
                return Settings.Instance.Checked.Contains(this.Id);
            }
            set
            {
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
