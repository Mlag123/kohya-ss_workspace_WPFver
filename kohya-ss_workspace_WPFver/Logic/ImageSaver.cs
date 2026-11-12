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


    /// <summary>
    /// Deprecated
    /// </summary>
    public static void initFolders()
    {
        if (!Directory.Exists(temp_folder))
        {
            Directory.CreateDirectory(temp_folder);
        }
    }


    public void generate(ObservableCollection<CroppedImage> croppedImages, string outputPath, Resolution resolution)
    {
        
        
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
    
        // 1. Рисуем ВНУТРИ using
        using (DrawingContext drawingContext = drawingVisual.RenderOpen())
        {
            drawingContext.DrawRectangle(Brushes.Black, null, new Rect(0, 0, resolution.Width, resolution.Height));
            double x = (resolution.Width - newWidth) / 2;
            double y = (resolution.Height - newHeight) / 2;
            drawingContext.DrawImage(scaledImage, new Rect(x, y, newWidth, newHeight));
        } // <--- КОНТЕКСТ ЗАКРЫВАЕТСЯ ЗДЕСЬ. Всё зафиксировано.

        // 2. Рендерим ПОСЛЕ закрытия контекста
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


    /*public void Generate(string outputFolder, List<CroppedImage> croppedImages, int resX, int resY)
{
    FrameExtractor frameExtractor = new FrameExtractor();

    // Загружаем видео (один раз)
    foreach (var _frame in croppedImages)
    {
        if (!string.IsNullOrEmpty(_frame.FilePath))
        {
            frameExtractor.LoadVideo(_frame.FilePath);
            break;
        }
    }

    for (int i = 0; i < croppedImages.Count; i++)
    {
        var croppedImage = croppedImages[i];

        // Выдергиваем оригинал
        var originalFrame = frameExtractor.GetFrameAsBitmapSource(croppedImage.Seconds);

        // Если видео не отдало кадр — пропускаем
        if (originalFrame == null)
        {
            Console.WriteLine($"Failed to extract frame at {croppedImage.Seconds} sec");
            continue;
        }

        // Обрезаем (если нужно)
        BitmapSource finalImage;
        if (croppedImage.IsCropped)
        {
            finalImage = new CroppedBitmap(originalFrame, new Int32Rect(
                (int)croppedImage.CropXOffset,
                (int)croppedImage.CropYOffset,
                (int)croppedImage.CropWidth,
                (int)croppedImage.CropHeight));
        }
        else
        {
            finalImage = originalFrame;
        }


        // Считаем коэффициент (выбираем наименьший, чтобы влезло в рамки)
        double scale = Math.Min((double)resX / finalImage.PixelWidth, (double)resY / finalImage.PixelHeight);

        int newWidth = (int)(finalImage.PixelWidth * scale);
        int newHeight = (int)(finalImage.PixelHeight * scale);

        // Масштабируем картинку
        TransformedBitmap scaledImage = new TransformedBitmap(finalImage, new ScaleTransform(scale, scale));
        scaledImage.Freeze();

        // Рисуем на холсте нужного размера (центрируем)
        var drawingVisual = new DrawingVisual();
        using (DrawingContext drawingContext = drawingVisual.RenderOpen())
        {
            // Заливаем фон черным (или можно Brushes.Transparent)
            drawingContext.DrawRectangle(Brushes.Black, null, new Rect(0, 0, resX, resY));

            // Центрируем картинку
            double x = (resX - newWidth) / 2.0;
            double y = (resY - newHeight) / 2.0;
            drawingContext.DrawImage(scaledImage, new Rect(x, y, newWidth, newHeight));
        }

        // Рендерим холст в BitmapSource
        var renderTarget = new RenderTargetBitmap(resX, resY, 96, 96, PixelFormats.Pbgra32);
        renderTarget.Render(drawingVisual);
        renderTarget.Freeze();

        // --- СОХРАНЕНИЕ ---

        PngBitmapEncoder encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(renderTarget));

        string baseName = string.IsNullOrEmpty(croppedImage.ImageName) ? "frame" : croppedImage.ImageName;
        string imagePath = outputFolder + "/" + baseName + "_" + i + ".png";
        string textPath = outputFolder + "/" + baseName + "_" + i + ".txt";

        using (FileStream stream = new FileStream(imagePath, FileMode.Create))
        {
            encoder.Save(stream);
        }

        File.WriteAllText(textPath, croppedImage.PromptText);
    }*/


    /*
     *
     *
     *public void Generate(string outputFolder)
    {
        List<SavedFrame> frames = LoadFromJson();
        FrameExtractor frameExtractor = new  FrameExtractor();
        frameExtractor.LoadVideo(frames[0].FilePath);

        for (int i = 0; i < frames.Count; i++)
        {
            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(frameExtractor.GetFrameAsBitmapSource(frames[i].Seconds)));
            Console.WriteLine(frames[i].ImageName);
            using (FileStream fileStream = new FileStream(outputFolder + "/" + frames[i].ImageName+"_"+i+".png", FileMode.Create))
            {
                encoder.Save(fileStream);
            }

            File.WriteAllText(outputFolder + "/" + frames[i].ImageName + "_" + i + ".txt", frames[i].Prompt);

        }


    }

     *
     */


    /// <summary>
    /// Deprecated
    /// </summary>
    /// <returns></returns>
    public static string getRandName()
    {
        int length = 12;
        const string allowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        char[] letters = new char[length];
        for (int i = 0; i < length; i++)
        {
            letters[i] = allowedChars[rnd.Next(0, allowedChars.Length)];
        }

        return new string(letters);
    }
}