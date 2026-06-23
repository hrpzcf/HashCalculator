using System.Windows;
using System.Windows.Threading;

namespace HashCalculator.Others;

internal static class Synchronization
{
    internal static Dispatcher UI => Application.Current.Dispatcher;
}
