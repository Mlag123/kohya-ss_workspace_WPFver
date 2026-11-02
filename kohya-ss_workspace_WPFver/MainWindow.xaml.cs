using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using kohya_ss_workspace_WPFver.Logic;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using Point = System.Windows.Point;

namespace kohya_ss_workspace_WPFver;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : MetroWindow
{
    private string videoPath = null;
    private int Minute;
    private int second;
    private string source_filename;
    private double seconds = 0;
    private string output_path;
    private BitmapSource image;
    private ImageSaver _imageSaver;
    private bool _isDragging = false;
    private Point _startPoint;
    private FrameExtractor frame_extractor;


    private SavedFrame editorFrame;

    private List<CroppedImage> croppedImages;

    private CroppedImage currentImage;


    private int scaleX, scaleY;

    public MainWindow()
    {
        InitializeComponent();


        frame_extractor = new FrameExtractor();
        Init();
    }


    private void ResetState()
    {
        seconds = 0;
        slider.Value = 0;
        try
        {
            _imageSaver.ResetState();
        }
        catch (Exception e)
        {
            DebugText("Failed to dispose/backup");
            DebugText(e.GetBaseException().Message);
        }
    }


    public void Init()
    {
        ImageSaver.initFolders();
        croppedImages = new List<CroppedImage>();
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
            DebugText("Video is not loaded!");
        }
    }

    public void DebugText(string text, System.Windows.Media.Brush color = null)
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
            ResetState();
            videoPath = openFileDialog.FileName;
            DebugText($"Opened file: {videoPath}");
            frame_extractor.LoadVideo(videoPath);
            slider.Maximum = frame_extractor.getDuration();
            image = frame_extractor.GetFrameAsBitmapSource(seconds);
            updateUI();
        }
    }

    private void OutputFolderPicker_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Choose output folder",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            string folder = dialog.FolderName;
            output_path = folder;
            DebugText($"Output folder: {output_path}");
        }
    }

    private void Generate_OnClick(object sender, RoutedEventArgs e)
    {
        _imageSaver.Generate(output_path,croppedImages,scaleX,scaleY);
    }

    private void SaveBut_OnClick(object sender, RoutedEventArgs e)
    {
        if (IsVideoExist())
        {
            if (string.IsNullOrEmpty(filename.Text))
            {
                DebugText("Image name is empty!");
                return;
            }

            if (string.IsNullOrEmpty(resX.Text) | string.IsNullOrEmpty(resX.Text))
            {
                DebugText("Resolution is empty");
                return;
            }

            int width = int.Parse(resX.Text);
            int height = int.Parse(resY.Text);

            if (width < 128 || height < 128 | width > 4096 | height > 4096)
            {
                DebugText($"Invalid resolution {width} x {height}");
                DebugText($"Resolution is invalid! Diapason resolution 128-4096");
                return;
            }

            scaleX = width;
            scaleY = height;
    
            try
            {
                _imageSaver.SaveToJson(filename.Text, int.Parse(resX.Text), int.Parse(resY.Text), seconds,
                    prompt.Text, videoPath);
                DebugText($"Frame Saved {seconds}");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        else
        {
            DebugText($"Video not loaded!");
        }
    }

    private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
    {
        e.Handled = new Regex("[^0-9]+").IsMatch(e.Text);
    }

    private void ToggleSwitch_OnToggled(object sender, RoutedEventArgs e)
    {
        if (Switch.IsOn)
        {
            generate.Visibility = Visibility.Visible;
            MainGrid.Visibility = Visibility.Collapsed;
            SecondGrid.Visibility = Visibility.Visible;
            LoadAssets();
        }
        else
        {
            MainGrid.Visibility = Visibility.Visible;
            SecondGrid.Visibility = Visibility.Collapsed;
            generate.Visibility = Visibility.Collapsed;
        }
    }


    public void LoadAssets()
    {
        foreach (var frame in _imageSaver.LoadFromJson())
        {
            CroppedImage clean_crop = new CroppedImage()
            {
                Seconds = frame.Seconds,
                FilePath = frame.FilePath,
                PromptText = frame.Prompt,
                IsCropped = false,
                CropWidth = 0,
                CropHeight = 0,
                CropXOffset = 0,
                CropYOffset = 0,
                ImageName = frame.ImageName
            };
            croppedImages.Add(clean_crop);
        }

        foreach (var cropFrame in croppedImages)
        {
            if (!string.IsNullOrEmpty(cropFrame.FilePath))
            {
                frame_extractor.LoadVideo(cropFrame.FilePath);
                break;
            }
        }

        ListBox.ItemsSource = croppedImages;
    }

    public bool IsVideoExist() => image != null;


//_________________________________________________________________________//


// Нажали кнопку мыши
    private void CropCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDragging = true;
        _startPoint = e.GetPosition(CropCanvas);


        Canvas.SetLeft(SelectionRect, _startPoint.X);
        Canvas.SetTop(SelectionRect, _startPoint.Y);
        SelectionRect.Width = 0;
        SelectionRect.Height = 0;
        SelectionRect.Visibility = Visibility.Visible;
    }

    private void CropCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging) return;

        Point currentPoint = e.GetPosition(CropCanvas);

        double x = Math.Min(_startPoint.X, currentPoint.X);
        double y = Math.Min(_startPoint.Y, currentPoint.Y);
        double w = Math.Abs(_startPoint.X - currentPoint.X);
        double h = Math.Abs(_startPoint.Y - currentPoint.Y);
        Canvas.SetLeft(SelectionRect, x);
        Canvas.SetTop(SelectionRect, y);
        SelectionRect.Width = w;
        SelectionRect.Height = h;
    }


    private void CropCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
    }


    private void CropImage()
    {
        if (SelectionRect.Visibility == Visibility.Visible && SelectionRect.Width > 0 && SelectionRect.Height > 0)
        {
            // Координаты прямоугольника на Canvas
            double rectX = Canvas.GetLeft(SelectionRect);
            double rectY = Canvas.GetTop(SelectionRect);
            double rectW = SelectionRect.Width;
            double rectH = SelectionRect.Height;

            // Размеры картинки на экране
            double imgWidthOnScreen = ImageEditor.ActualWidth;
            double imgHeightOnScreen = ImageEditor.ActualHeight;

            // Коэффициенты масштаба
            double scaleX = ImageEditor.Source.Width / imgWidthOnScreen;
            double scaleY = ImageEditor.Source.Height / imgHeightOnScreen;

            // Пересчет в реальные пиксели картинки
            int x = (int)(rectX * scaleX);
            int y = (int)(rectY * scaleY);
            int w = (int)(rectW * scaleX);
            int h = (int)(rectH * scaleY);

            currentImage.CropXOffset = x;
            currentImage.CropYOffset = y;
            currentImage.CropWidth = w;
            currentImage.CropHeight = h;
            currentImage.IsCropped = true;
            
            // Обрезка
            var a = new CroppedBitmap((BitmapSource)ImageEditor.Source, new Int32Rect(x, y, w, h));
            UpdateEditorUI();
            /*ImageEditor.Source = cropped;
            _imageSaver.UpdateFrame(editorFrame.Seconds, editorFrame);*/
        }
    }


    public void UpdateEditorUI()
    {
        if (currentImage != null)
        {
            Console.WriteLine(currentImage.Seconds);
            
            if (currentImage.IsCropped)
            {
                ImageEditor.Source = new CroppedBitmap((BitmapSource)frame_extractor.GetFrameAsBitmapSource(currentImage.Seconds), new Int32Rect(currentImage.CropXOffset, currentImage.CropYOffset,currentImage.CropWidth,currentImage.CropHeight));
            }
            else
            {
                ImageEditor.Source = frame_extractor.GetFrameAsBitmapSource(currentImage.Seconds);
            }
            PromptEditor.Text = currentImage.PromptText;
        }
    }

/*private void LoadEdit_OnClick(object sender, RoutedEventArgs e)
{
    imageSaved_array = _imageSaver.LoadFromJson();
    ListBox.ItemsSource = imageSaved_array;
}*/

    private void MyListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (croppedImages != null)
        {
            if (ListBox.SelectedItem is CroppedImage frame)
            {
                currentImage = frame;
                //BitmapSource a = frame_extractor.GetFrameAsBitmapSource(currentImage.Seconds);
                UpdateEditorUI();
            }
        }


        /*if (ListBox.SelectedItem is SavedFrame frame)
        {
            editorFrame = frame;

            frame_extractor.LoadVideo(frame.FilePath);
            /*editorSeconds = frame.Seconds;
            PromptEditor.Text = frame.Prompt;#2##1#
            PromptEditor.Text = editorFrame.Prompt;
            UpdateEditorUI();
        }*/
    }



    private void PromptEditor_OnKeyDown(object sender, KeyEventArgs e)
    {
        String a = PromptEditor.Text;
        currentImage.PromptText = a;
    }

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        CropImage();
    }
}