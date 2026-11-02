using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace kohya_ss_workspace_WPFver.Logic;

public class CroppedImage
{
    public double Seconds {get; set;}
    public string FilePath {get; set;}
    public int CropWidth {get; set;}
    public int CropHeight {get; set;}
    public int CropXOffset {get; set;}
    public int CropYOffset {get; set;}
    public bool IsCropped {get; set;}
    public string PromptText {get; set;}
    
}