using System;
using System.ComponentModel;
using System.Diagnostics;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

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
                Map(e => e.IsDebuff    ).Index('F' - 'A').TypeConverter<IntToBooleanConverter>();
                Map(e => e.IsNonExpries).Index('O' - 'A');
                Map(e => e.IsStance    ).Index('P' - 'A');
                Map(e => e.IsChecked   ).Ignore(true);
            }
            public class IntToBooleanConverter : DefaultTypeConverter
            {
                public override object ConvertFromString(TypeConverterOptions options, string value)
                {
                    return int.TryParse(value, out int i) && i > 0;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        
        public int      Id              { get; set; }
        public string   Name            { get; set; }
        public string   Desc            { get; set; }
        public int      Icon            { get; set; }
        public int      IconRange       { get; set; }
        public bool     IsDebuff        { get; set; }
        public bool     IsNonExpries    { get; set; }
        public bool     IsStance        { get; set; }
        
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
