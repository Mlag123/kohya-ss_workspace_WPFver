using System.Collections;
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
    private static string json_format = ".json";
    private string jsonFile = temp_folder + "/" + "saved" + json_format;


    public static void initFolders()
    {
        if (!Directory.Exists(temp_folder))
        {
            Directory.CreateDirectory(temp_folder);
        }
    }

    private List<SavedFrame> savedFrames = new List<SavedFrame>();

    public void SaveToJson(string filename, int nW, int nH, double seconds, string prompt, string source_filepath)
    {
        if (File.Exists(jsonFile) && savedFrames.Count == 0)
        {
            string existingJson = File.ReadAllText(jsonFile);
            savedFrames = JsonSerializer.Deserialize<List<SavedFrame>>(existingJson) ?? new List<SavedFrame>();
        }

        double epsilon = 0.0001;
        if (savedFrames.Any(f => Math.Abs(f.Seconds - seconds) < epsilon))
        {
            throw new TakeException("The frame already exists");
            return;
        }

        var newFrame = new SavedFrame
        {
            ImageName = filename,
            nWidth = nW,
            nHeight = nH,
            Seconds = seconds,
            Prompt = prompt,
            FilePath = source_filepath
        };
        savedFrames.Add(newFrame);

        string json = JsonSerializer.Serialize(savedFrames, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(jsonFile, json);
    }


    public void ResetState()
    {
        string bak_json = temp_folder + "/" + getRandName() + "_bak" + json_format;

        try
        {
            if (File.Exists(jsonFile))
            {
                savedFrames.Clear();
                File.Move(jsonFile, bak_json);
            }
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
    }

    public void UpdateFrame(double seconds, SavedFrame updateData)
    {
        var frames = LoadFromJson();

        double epsilon = 0.0001;
        var frame = frames.FirstOrDefault(f => Math.Abs(f.Seconds - seconds) < epsilon);

        if (frame == null)
            throw new Exception($"Frame with seconds={seconds} not found");

        // Меняем поля найденного объекта на данные из updateData
        frame.ImageName = updateData.ImageName;
        frame.nWidth = updateData.nWidth;
        frame.nHeight = updateData.nHeight;
        frame.Prompt = updateData.Prompt;
        frame.FilePath = updateData.FilePath;
        // Seconds НЕ меняем — это ключ поиска

        string json = JsonSerializer.Serialize(frames, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(jsonFile, json);
    }
    
    
    


    public void savePromptAndImage(string prompt, BitmapSource bitmap)
    {
        string rand_name = getRandName();
        string imagePath = temp_folder + "/" + rand_name + png_format;
        string textPath = temp_folder + "/" + rand_name + text_format;

        PngBitmapEncoder encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));

        using (FileStream stream = new FileStream(imagePath, FileMode.Create))
        {
            encoder.Save(stream);
        }

        File.WriteAllText(textPath, prompt);
    }

    public List<SavedFrame> LoadFromJson()
    {
        if (!File.Exists(jsonFile))
        {
            return new List<SavedFrame>();
        }

        string json = File.ReadAllText(jsonFile);

        var frames = JsonSerializer.Deserialize<List<SavedFrame>>(json);

        return frames ?? new List<SavedFrame>();
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