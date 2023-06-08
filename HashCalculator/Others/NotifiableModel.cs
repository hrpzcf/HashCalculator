using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HashCalculator
{
    public class NotifiableModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void SetPropNotify<T>(
            ref T property,
            T value,
            [CallerMemberName] string name = null)
        {
            property = value;
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
