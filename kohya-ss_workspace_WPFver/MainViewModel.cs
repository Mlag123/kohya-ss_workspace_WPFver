using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using kohya_ss_workspace_WPFver.Logic;
using Microsoft.Win32;

namespace kohya_ss_workspace_WPFver;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private string uiPrompt;

    [ObservableProperty] private string resolutionWidth;

    [ObservableProperty] private string resolutionHeight;

    [ObservableProperty] private BitmapSource imageOriginal;

    [ObservableProperty]
    private ObservableCollection<CroppedImage> croppedImages = new();
    [ObservableProperty] private string imageName;
    [ObservableProperty] private BitmapSource imageCropped; // пока не знаю, нужно ли оно
    [ObservableProperty] private string consoleLog = string.Empty;
    [ObservableProperty] 
    private string videoTimelineText = "00:00    res:0:0";
        
    public event Action<string> FileSelected;
    public event  Action SaveButtonClick;
    public event  Action GenerateButtonClick;
    public string OutputPath { get; set; }

    public string FolderToSource { get; set; }

    
    public void UpdateTimeline(double seconds, BitmapSource image)
    {
        if (image == null) return;

        int minutes = (int)seconds / 60;
        int secs = (int)seconds % 60;
        int w = (int)image.Width;
        int h = (int)image.Height;
    
        VideoTimelineText = $"{minutes}:{secs}    res:{w}:{h}";
    }
    
    [RelayCommand]
    private void FilePicker()
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "Video files (*.mp4)|*.mp4|All files (*.*)|*.*";
        if (openFileDialog.ShowDialog() == true)
        {
            if (openFileDialog.FileName != null)
            {
                FolderToSource = openFileDialog.FileName;
                FileSelected?.Invoke(openFileDialog.FileName);
                DebugText($"Folder {openFileDialog.FileName}");
            }
            else
            {
                DebugText("Folder is incorrect");
            }
        }
    }

    public void DebugText(string text)
    {
        ConsoleLog += $" {text}{Environment.NewLine}";
        // А скролл вниз - это уже задача View. 
    }

 


    [RelayCommand]
    private void OutputPicker()
    {
        OpenFolderDialog dialog = new OpenFolderDialog
        {
            Title = "Choose output folder",
            Multiselect = false
        };
        if (dialog.ShowDialog() == true)
        {
            if (dialog.FolderName != null)
            {
                OutputPath = dialog.FolderName;
                DebugText($"Output folder {OutputPath}");
            }
            else
            {
                DebugText("Output folder is incorrect");
            }
        }
    }


    [RelayCommand]
    private void CropButton()
    {
    }

    [RelayCommand]
    private void SaveBut()
    {
        SaveButtonClick?.Invoke();

    }


    [RelayCommand]
    private void Generate()
    {
        GenerateButtonClick?.Invoke();
    }

   
}