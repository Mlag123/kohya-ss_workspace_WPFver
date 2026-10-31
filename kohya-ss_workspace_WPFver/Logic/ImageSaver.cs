using System.IO;
using System.Text.Json;
using System.Windows.Media.Imaging;

namespace kohya_ss_workspace_WPFver.Logic;

public class ImageSaver
{
    private static string sourcePath;
    private static string outputPath;
    private static string temp_folder = "./temp";
    private static readonly Random rnd = new Random();
    private string png_format = ".png";
    private string text_format = ".txt";
    private static string json_format = ".json";
    private  string jsonFile = temp_folder + "/" + "saved" + json_format;


    public static void initFolders()
    {
        if (!Directory.Exists(temp_folder))
        {
            Directory.CreateDirectory(temp_folder);
        }
    }

    private List<SavedFrame> savedFrames = new List<SavedFrame>();

    public void saveToJson(string filename,int nW, int nH,double seconds, string prompt)
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
            Prompt = prompt
        };
        savedFrames.Add(newFrame);
    
        string json = JsonSerializer.Serialize(savedFrames, new JsonSerializerOptions { WriteIndented = true }); 
        File.WriteAllText(jsonFile, json);
    }


    public void dispose()
    {
        string bak_json = temp_folder+"/"+getRandName()+"_bak"+json_format;

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
    


    public static string getRandName()
    {
        int length = 12;
        // Только безопасные символы, которые Windows гарантированно разрешит в имени файла
        const string allowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        char[] letters = new char[length];

        for (int i = 0; i < length; i++)
        {
            letters[i] = allowedChars[rnd.Next(0, allowedChars.Length)];
        }

        return new string(letters);
    }
}