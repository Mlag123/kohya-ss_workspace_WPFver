using System.Windows;
using MahApps.Metro.Controls;

namespace kohya_ss_workspace_WPFver;

public partial class AboutWindow : MetroWindow
{
    public string Version => "v.1.0.4.1";

    public AboutWindow()
    {
        InitializeComponent();
        DataContext = this;
    }
}