using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Controls.Ribbon;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using kohya_ss_workspace_WPFver.Logic;
using MahApps.Metro.Controls;
using Microsoft.Win32;

namespace kohya_ss_workspace_WPFver;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : MetroWindow
{
    private int Minute;
    private int second;
    private string source_filename;
    private string _filename;
    private double seconds = 0;
    private string output_path;
    private BitmapSource image;
    private ImageSaver _imageSaver;

    private string test_path =
        "C:/Users/MLag/RiderProjects/ImageExtractorFromVideo/ImageExtractorFromVideo/bin/Debug/net10.0/test.mp4";


    private FrameExtractor frame_extractor;

    public MainWindow()
    {
        InitializeComponent();


        frame_extractor = new FrameExtractor();
        init();
    }


    private void dispose()
    {
        seconds = 0;
        slider.Value = 0;
        console.Text = "";
        try
        {
            _imageSaver.dispose();
        }
        catch (Exception e)
        {
            debugText("Failed to dispose/backup");
            debugText(e.GetBaseException().Message);
        }
    }


    public void init()
    {
        ImageSaver.initFolders();
        _imageSaver = new ImageSaver();
        
        VideoTimelineLayout.Text = "00:00";
        slider.Maximum = frame_extractor.getDuration();
    }

    private void TimeLine_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        try
        {
            Console.WriteLine(slider.Value);
            this.seconds = slider.Value;
            image = frame_extractor.GetFrameAsBitmapSource(seconds);
            updateUI();
        }
        catch (Exception)
        {
            debugText("Video is not loaded!");
        }
    }

    public void debugText(string text)
    {
        console.AppendText($" {text}{Environment.NewLine}");
        console.ScrollToEnd();
    }


    public void updateUI()
    {
        PictureOutputMedia.Source = image;

        int _minutes = (int)this.seconds / 60;
        int _seconds = (int)this.seconds % 60;
        int w = (int)image.Width;
        int h = (int)image.Height;
        VideoTimelineLayout.Text = $"{_minutes}:{_seconds}    res:{w}:{h}";
    }


    private void FilePicker_OnClick(object sender, RoutedEventArgs e)
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "Video files (*.mp4)|*.mp4|All files (*.*)|*.*";

        if (openFileDialog.ShowDialog() == true)
        {
            dispose();
            source_filename = openFileDialog.FileName;
            debugText($"Opened file: {source_filename}");
            frame_extractor.LoadVideo(source_filename);
            slider.Maximum = frame_extractor.getDuration();
            image = frame_extractor.GetFrameAsBitmapSource(seconds);
            updateUI();
        }
    }

    private void OutputFolderPicker_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Выберите папку",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            string folder = dialog.FolderName;
            output_path = folder;
            debugText($"Output folder: {output_path}");
        }
    }

    private void Generate_OnClick(object sender, RoutedEventArgs e)
    {
       debugText("WIP");
    }

    private void SaveBut_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            if (image != null)
            {
                _imageSaver.saveToJson(seconds, prompt.Text);
                debugText($"Frame Saved {seconds}");
            }
            else
            {
                debugText($"Video not loaded!");
            }

        }
        catch (TakeException)
        {
            debugText($"The frame already exists");
        }
    }
}