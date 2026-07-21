using System;
using System.Windows;
using HashCalculator.ViewModels.Windows;

namespace HashCalculator.Views.Windows;

public partial class ExceptionWindow
{

    internal ExceptionWindowModel Model { get; }

    public ExceptionWindow()
    {
        this.Model = new ExceptionWindowModel(this);
        this.DataContext = this.Model;
        this.InitializeComponent();
        this.Closed += this.ExceptionWindowClosed;
    }

    private void ExceptionWindowClosed(object sender, EventArgs e)
    {
        Application.Current.Shutdown();
    }
}
