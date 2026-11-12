using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace kohya_ss_workspace_WPFver.Logic;

public class CroppedImage
{
    public double Seconds { get; set; }
    public string ImageName { get; set; } 
    public bool isImagesType { get; set; } //When true  it is picture, also frame.
    public string SourceFileImageName {get; set;} //Picture name
    public string FilePath { get; set; }
    public BitmapSource Original { get; set; }
    public BitmapSource Cropped { get; set; }
    public bool IsCropped { get; set; }
    public string PromptText { get; set; }

    public int NewResizeWidth { get; set; }
    public int NewResizeHeight { get; set; }
}