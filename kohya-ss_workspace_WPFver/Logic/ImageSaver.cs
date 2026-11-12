using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace kohya_ss_workspace_WPFver.Logic;

public class ImageSaver
{
    private static string temp_folder = "./temp";
    private static readonly Random rnd = new Random();
    private string png_format = ".png";
    private string text_format = ".txt";


   
    public static void initFolders()
    {
        if (!Directory.Exists(temp_folder))
        {
            Directory.CreateDirectory(temp_folder);
        }
    }


    public void generate(ObservableCollection<CroppedImage> croppedImages, string outputPath, Resolution resolution)
    {

        if (string.IsNullOrEmpty(outputPath))
        {
            return;
        }
        
        if (!Directory.Exists(outputPath))
        {
            Directory.CreateDirectory(outputPath);
        }
        
        Console.WriteLine("Generating Cropped Images");
        for (int i = 0; i < croppedImages.Count; i++)
        {
            string pngoutPath = outputPath + "/" + croppedImages[i].ImageName + "_" + i + png_format;
            string textPath = outputPath + "/" + croppedImages[i].ImageName + "_" + i + text_format;

            if (croppedImages[i].IsCropped)
            {
                var finalImage = ResizeImage(croppedImages[i].Cropped, resolution);
                savePicture(finalImage, pngoutPath);
                SavePromptToFile(croppedImages[i].PromptText, textPath);
            }
            else
            {
                var finalImage = ResizeImage(croppedImages[i].Original, resolution);
                savePicture(finalImage, pngoutPath);
                SavePromptToFile(croppedImages[i].PromptText, textPath);

            }
        }
    }

    private void savePicture(BitmapSource source, string path)
    {
        PngBitmapEncoder encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(source));
        using (FileStream stream = new FileStream(path, FileMode.Create))
        {
            encoder.Save(stream);
        }
    }

    private BitmapSource ResizeImage(BitmapSource source, Resolution resolution)
    {
        double scale = Math.Min((double)resolution.Width / source.PixelWidth,
            (double)resolution.Height / source.PixelHeight);

        int newWidth = (int)(source.PixelWidth * scale);
        int newHeight = (int)(source.PixelHeight * scale);

        TransformedBitmap scaledImage = new TransformedBitmap(source, new ScaleTransform(scale, scale));
        scaledImage.Freeze();

        var drawingVisual = new DrawingVisual();
    
        using (DrawingContext drawingContext = drawingVisual.RenderOpen())
        {
            drawingContext.DrawRectangle(Brushes.Black, null, new Rect(0, 0, resolution.Width, resolution.Height));
            double x = (resolution.Width - newWidth) / 2;
            double y = (resolution.Height - newHeight) / 2;
            drawingContext.DrawImage(scaledImage, new Rect(x, y, newWidth, newHeight));
        } 

        var renderTarget = new RenderTargetBitmap(resolution.Width, resolution.Height, 96, 96, PixelFormats.Pbgra32);
        renderTarget.Render(drawingVisual);
        renderTarget.Freeze();

        return renderTarget;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="prompt"></param>
    /// <param name="bitmap"></param>
    public void SavePromptToFile(string prompt,string textPath)
    {
     
        File.WriteAllText(textPath, prompt);
    }


}