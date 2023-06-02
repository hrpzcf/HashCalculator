using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HashCalculator
{
    public class BaseNotifiable : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void SetPropNotify<T>(ref T prop, T value, [CallerMemberName] string name = null)
        {
            prop = value;
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void OnPropertyChanged([CallerMemberName] string name = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
