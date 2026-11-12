using System.Windows;
using System.Windows.Media.Imaging;

namespace kohya_ss_workspace_WPFver.Logic;

public class CropManager
{
    public BitmapSource CropImage(int x, int y, int w, int h, BitmapSource source)
    {
        return new CroppedBitmap(source, new Int32Rect(x, y, w, h));
    }
}