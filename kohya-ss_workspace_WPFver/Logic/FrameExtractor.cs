using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using FFMediaToolkit.Decoding;
using FFMediaToolkit.Graphics;
using SkiaSharp;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Media;
using SkiaSharp.Views.WPF;

namespace kohya_ss_workspace_WPFver.Logic;

public class FrameExtractor
{
    private string videoPath = null;

    private MediaFile mediaFile = null;

    private SKBitmap initBitmap(MediaFile file)
    {
        var bitmap = new SKBitmap(
            file.Video.Info.FrameSize.Width,
            file.Video.Info.FrameSize.Height,
            SKColorType.Bgra8888,
            SKAlphaType.Unpremul
        );

        return bitmap;
        
    }
    
    public void LoadVideo(string path)
    {
        videoPath = path;
    
        mediaFile?.Dispose(); 
    
        mediaFile = MediaFile.Open(videoPath);
    }
    
    public BitmapSource GetFrameAsBitmapSource(double seconds)
    {
        var imageData = mediaFile.Video.GetFrame(TimeSpan.FromSeconds(seconds));
        int width = imageData.ImageSize.Width;
        int height = imageData.ImageSize.Height;

        WriteableBitmap writeableBitmap = new WriteableBitmap(
            width, height,
            96, 96,
            PixelFormats.Bgr24,
            null);

        writeableBitmap.Lock();
    
        writeableBitmap.WritePixels(
            new Int32Rect(0, 0, width, height),

            imageData.Data.ToArray(), 
            imageData.Stride,
            0);

        writeableBitmap.Unlock();
        writeableBitmap.Freeze(); 

        return writeableBitmap;
    }

    public double getDuration()
    {
        if (mediaFile == null) return 0;
    
        TimeSpan duration = mediaFile.Info.Duration;
        return duration.TotalSeconds;
    }

    public ImageData getFrameFromTimeSpan(double seconds)
    {
        return mediaFile.Video.GetFrame(TimeSpan.FromSeconds(seconds));
    }
    
}