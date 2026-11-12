using CommunityToolkit.Mvvm.ComponentModel;

namespace kohya_ss_workspace_WPFver.Logic;

public partial  class PawItem : ObservableObject
{
    [ObservableProperty] private double x;
    [ObservableProperty] private double y;
    [ObservableProperty] private string color = "#33FFFFFF"; 
    [ObservableProperty] private double scale = 1.0; 
    [ObservableProperty] private double rotation = 0;
    
    
    public double VelocityX { get; set; }
    public double VelocityY { get; set; }
    public double RotationSpeed { get; set; } // Скорость вращения\
    public double HomeX { get; set; }
    public double HomeY { get; set; }
}