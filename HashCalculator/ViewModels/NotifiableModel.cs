using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HashCalculator
{
    public abstract class NotifiableModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged([CallerMemberName] string name = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void SetPropNotify<T>(ref T property, T value, [CallerMemberName] string name = null)
        {
            if (property?.Equals(value) != true)
            {
                property = value;
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}
