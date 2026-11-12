using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
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

    [ObservableProperty] private ObservableCollection<CroppedImage> croppedImages = new();
    [ObservableProperty] private string imageName;
    [ObservableProperty] private BitmapSource imageCropped; // пока не знаю, нужно ли оно
    [ObservableProperty] private string consoleLog = string.Empty;
    [ObservableProperty] private string videoTimelineText = "00:00    res:0:0";

    [ObservableProperty] private int progressValue;
    [ObservableProperty] private int progressMax;
    [ObservableProperty] private bool isGenerating;
    private string currentVersion = "1.0.4.1v";

    public string imageFolderPath;
    public event Action<string> FileSelected;
    public event Action SaveButtonClick;
    public event Action GenerateButtonClick;
    public event Action<string> OpenFolderDialog;
    public string OutputPath { get; set; }

    public string FolderToSource { get; set; }
    public ObservableCollection<PawItem> Paws { get; } = new();


    private readonly Random _rnd = new();
    private TimeSpan _lastRenderTime = TimeSpan.Zero;

    public MainViewModel()
    {
        GeneratePaws(300);
    }


    public void ExplodePaws(double width, double height)
    {
        var rnd = new Random();
        foreach (var paw in Paws)
        {
            paw.VelocityX += (rnd.NextDouble() - 0.5) * 1500;
            paw.VelocityY += (rnd.NextDouble() - 0.5) * 1500;


            paw.HomeX = rnd.Next(0, (int)width);
            paw.HomeY = rnd.Next(0, (int)height);
        }
    }

    private void GeneratePaws(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var x = _rnd.Next(0, 1200);
            var y = _rnd.Next(0, 800);
            Paws.Add(new PawItem
            {
                X = _rnd.Next(0, 1200),
                Y = _rnd.Next(0, 800),
                HomeX = x,
                HomeY = y,
                Rotation = _rnd.Next(0, 360),
                Scale = 0.5 + _rnd.NextDouble() * 0.5,
                Color = "#11FFFFFF",
                RotationSpeed = (_rnd.NextDouble() - 0.5) * 20
            });
        }
    }

    public void UpdatePaws(TimeSpan renderTime, Point mousePosition, double width, double height)
    {
        if (_lastRenderTime == TimeSpan.Zero)
        {
            _lastRenderTime = renderTime;
            return;
        }

        double deltaTime = (renderTime - _lastRenderTime).TotalSeconds;
        _lastRenderTime = renderTime;

        if (deltaTime <= 0 || deltaTime > 0.1) return;

        foreach (var paw in Paws)
        {
            // Вращение
            paw.Rotation += paw.RotationSpeed * deltaTime;

            // Взаимодействие с мышью
            double dxMouse = paw.X - mousePosition.X;
            double dyMouse = paw.Y - mousePosition.Y;
            double distMouse = Math.Sqrt(dxMouse * dxMouse + dyMouse * dyMouse);

            if (distMouse < 150 && distMouse > 0)
            {
                paw.Color = "#FFFF6D00"; // Оранжевый
                double force = (150 - distMouse) / 150.0;
                paw.VelocityX += (dxMouse / distMouse) * force * 800 * deltaTime;
                paw.VelocityY += (dyMouse / distMouse) * force * 800 * deltaTime;
            }
            else
            {
                paw.Color = "#11FFFFFF";
            }

            // Пружина (притяжение к дому)
            double dxHome = paw.HomeX - paw.X;
            double dyHome = paw.HomeY - paw.Y;
            double springStrength = 2.0;
            paw.VelocityX += dxHome * springStrength * deltaTime;
            paw.VelocityY += dyHome * springStrength * deltaTime;

            paw.X += paw.VelocityX * deltaTime;
            paw.Y += paw.VelocityY * deltaTime;

            paw.VelocityX *= 0.9;
            paw.VelocityY *= 0.9;


            if (paw.X < 0)
            {
                paw.X = 0;
                paw.VelocityX *= -0.8;
            }
            else if (paw.X > width)
            {
                paw.X = width;
                paw.VelocityX *= -0.8;
            }

            if (paw.Y < 0)
            {
                paw.Y = 0;
                paw.VelocityY *= -0.8;
            }
            else if (paw.Y > height)
            {
                paw.Y = height;
                paw.VelocityY *= -0.8;
            }
        }
    }

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
    private void ClickAbout()
    {
        var about = new AboutWindow();
        about.ShowDialog();
    }

    [RelayCommand]
    private void GitHubAction()
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://github.com/Mlag123/kohya-ss_workspace_WPFver",
            UseShellExecute = true
        });
    }

    [RelayCommand]
    private void FolderPicker()
    {
        OpenFolderDialog dialog = new OpenFolderDialog
        {
            Title = "Choose folder",
            Multiselect = false
        };
        if (dialog.ShowDialog() == true)
        {
            imageFolderPath = dialog.FolderName;
            OpenFolderDialog?.Invoke(imageFolderPath);
            DebugText($"Images path {imageFolderPath}");
        }
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