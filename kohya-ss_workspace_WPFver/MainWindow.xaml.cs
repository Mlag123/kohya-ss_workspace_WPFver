using System.Collections.ObjectModel;
using System.Drawing;
using System.Runtime.InteropServices;
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
    private double seconds = 0;
    private string output_path;
    private ImageSaver _imageSaver;
    private bool _isDragging = false;
    private Point _startPoint;
    private FrameExtractor frame_extractor;

    //  private ObservableCollection<CroppedImage> croppedImages;
    private CroppedImage currentImage;
    private bool isCropped = false;
    private int scaleX, scaleY;
    private CropManager cropManager;

    private MainViewModel _viewModel;
    private const string DEFAULT_RESOLUTION = "512";
    private CroppedImage selectedCroppedImage;
    private bool _isUpdatingFromList = false;

    [DllImport("kernel32.dll")]
    static extern bool AllocConsole();
    public MainWindow()
    {
        //AllocConsole();
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        _viewModel.FileSelected += OnFileSelected;
        _viewModel.SaveButtonClick += SaveButtonImage;
        _viewModel.GenerateButtonClick += Generate;

        Init();
    }

    public void OnFileSelected(string path)
    {
        ResetState();
        _viewModel.CroppedImages.Clear();


        videoPath = path;
        frame_extractor.LoadVideo(videoPath);
        slider.Maximum = frame_extractor.getDuration();


        var bitmap = frame_extractor.GetFrameAsBitmapSource(seconds);

        if (bitmap != null && bitmap.CanFreeze) bitmap.Freeze();

        SendImageToUI(bitmap);

        currentImage.Original = bitmap;
        currentImage.Seconds = seconds;
        SetMetaData(currentImage);
    }

    public void Init()
    {
       // ImageSaver.initFolders();
        currentImage = new CroppedImage() { IsCropped = false };
        cropManager = new CropManager();
        frame_extractor = new FrameExtractor();
        _imageSaver = new ImageSaver();
    }


    public void Generate()
    {
        _imageSaver.generate(GetCroppedImages(),_viewModel.OutputPath,GetNewResolution());
    }

    private ObservableCollection<CroppedImage> GetCroppedImages()
    {
        return _viewModel.CroppedImages;
    }

    private MainViewModel getMainViewModel()
    {
        if (DataContext is MainViewModel vs)
        {
            return vs;
        }

        DebugText("Critical Error!");
        throw new InvalidOperationException("DataContext is not MainViewModel!");
    }

    private string getOutputPath()
    {
        return _viewModel.OutputPath;
    }

    private void SaveButtonImage()
    {
        if (currentImage != null)
        {
            if (!isNameExist())
            {
                DebugText("Image name is empty!");
                return;
            }

            var resolution = GetNewResolution();

            CroppedImage origImage = new CroppedImage()
            {
                IsCropped = false,
                ImageName = filename.Text, 
                FilePath = videoPath,
                Original = currentImage.Original,
                Cropped = null,
                PromptText = GetPromtFromUI(),
                Seconds = currentImage.Seconds,
                NewResizeWidth = resolution.Width,
                NewResizeHeight = resolution.Height,
            };
            AddCroppedImageToListBox(origImage);
        }
    }

    private void ResetState()
    {
        seconds = 0;
        slider.Value = 0;
        currentImage = new CroppedImage();
    }

    private void SetMetaData(CroppedImage croppedImage)
    {
        MainViewModel vs = getMainViewModel();
        {
            if (croppedImage.IsCropped)
            {
                vs.UpdateTimeline(croppedImage.Seconds, croppedImage.Cropped);
            }
            else
            {
                vs.UpdateTimeline(croppedImage.Seconds, croppedImage.Original);
            }
        }
    }

    private void TimeLine_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingFromList) return;
        try
        {
            this.seconds = slider.Value;
            currentImage.Seconds = seconds;
            currentImage.Original = frame_extractor.GetFrameAsBitmapSource(seconds);

            SendImageToUI(currentImage);
            SetMetaData(currentImage);
        }
        catch (Exception)
        {
            DebugText("Video is not loaded!");
        }
    }





    private void Console_TextChanged(object sender, TextChangedEventArgs e)
    {
        ((TextBox)sender).ScrollToEnd();
    }


    private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
    {
        e.Handled = new Regex("[^0-9]+").IsMatch(e.Text);
    }


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
        if (isImageExist())
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
    }

    private void ResetRectangle()
    {
        SelectionRect.Width = 0;
        SelectionRect.Height = 0;
        SelectionRect.Visibility = Visibility.Hidden;
    }

    //отпустил мышь.
    private void CropCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        CropImage();
        ResetRectangle();
    }



    private bool isImageExist()
    {
        return DataContext is MainViewModel vs && vs.ImageOriginal != null;
    }


    private void CropImage()
    {
        if (!isNameExist())
        {
            DebugText("Image name is empty!");
            return;
        }

        if (isImageExist() && SelectionRect.Visibility == Visibility.Visible && SelectionRect.Width > 0 &&
            SelectionRect.Height > 0)
        {
            double rectX = Canvas.GetLeft(SelectionRect);
            double rectY = Canvas.GetTop(SelectionRect);
            double rectW = SelectionRect.Width;
            double rectH = SelectionRect.Height;

            double canvasWidth = CropCanvas.ActualWidth;
            double canvasHeight = CropCanvas.ActualHeight;
            double imageWidth = PictureOutputMedia.Source.Width;
            double imageHeight = PictureOutputMedia.Source.Height;

            double scale = Math.Min(canvasWidth / imageWidth, canvasHeight / imageHeight);

            double renderedWidth = imageWidth * scale;
            double renderedHeight = imageHeight * scale;

            double offsetX = (canvasWidth - renderedWidth) / 2.0;
            double offsetY = (canvasHeight - renderedHeight) / 2.0;

            double imageX = (rectX - offsetX) / scale;
            double imageY = (rectY - offsetY) / scale;
            double imageW = rectW / scale;
            double imageH = rectH / scale;

            int x = (int)Math.Max(0, imageX);
            int y = (int)Math.Max(0, imageY);
            int w = (int)Math.Min(imageWidth - x, imageW);
            int h = (int)Math.Min(imageHeight - y, imageH);

            if (w >= 128 && h >= 128)
            {
                var resolution = GetNewResolution();
                CroppedImage cropImg = new CroppedImage()
                {
                    IsCropped = true,
                    ImageName = filename.Text,
                    FilePath = videoPath,
                    Original = currentImage.Original,
                    Cropped = cropManager.CropImage(x, y, w, h, (BitmapSource)PictureOutputMedia.Source),
                    PromptText = GetPromtFromUI(),
                    Seconds = currentImage.Seconds,
                    NewResizeWidth = resolution.Width,
                    NewResizeHeight = resolution.Height,
                };
                SendImageToUI(cropImg);
                // SetMetaData(cropImg);
                AddCroppedImageToListBox(cropImg);
                SendPromptToUI("");
            }
            else
            {
                DebugText("Is cropped size very small!");
            }
        }
    }

    private void AddCroppedImageToListBox(CroppedImage cropImg)
    {
        _viewModel.CroppedImages.Add(cropImg);
    }


    private Resolution GetNewResolution()
    {
        if (!string.IsNullOrEmpty(resX.Text) && !string.IsNullOrEmpty(resY.Text))
        {
            int w = int.Parse(resX.Text), h = int.Parse(resY.Text);
            if (w >= 128 && w <= 4096 && h >= 128 && h <= 4096)
            {
                return new Resolution()
                {
                    Width = w,
                    Height = h
                };
            }
        }
        else
        {
            DebugText($"Resolution {resX}x{resY} is out of range (128-4096)!");
        }

        DebugText($"Resolution {resX.Text} x {resY.Text} is incorrect! Set default resolution");
        SetResolutionInUI();

        return new Resolution() { Width = 512, Height = 512 };
    }


    private bool isNameExist()
    {
        return !string.IsNullOrEmpty(filename.Text) && filename.Text.Length < 255;
    }

    private void SetResolutionInUI(Resolution resolution = null)
    {

        if (resolution == null)
        {
            _viewModel.ResolutionWidth = DEFAULT_RESOLUTION;
            _viewModel.ResolutionHeight = DEFAULT_RESOLUTION;
        }
        else
        {
            _viewModel.ResolutionWidth = resolution.Width.ToString();
            _viewModel.ResolutionHeight = resolution.Height.ToString();
        }
    }


    private string GetPromtFromUI()
    {
        if (!string.IsNullOrEmpty(prompt.Text))
        {
            return prompt.Text;
        }
        else
        {
            return "";
        }
    }

    private void SetPromptToUI(string prompt)
    {

        if (string.Equals(prompt, ""))
        {
            _viewModel.UiPrompt = "";
        }

        if (!string.IsNullOrEmpty(prompt))
        {
            _viewModel.UiPrompt = prompt;
        }
    }

    private void DebugText(string text)
    {
       
            _viewModel.DebugText(text);
        
    }


    private void SendImageToUI(CroppedImage croppedImage)
    {
        if (croppedImage == null) return;


        if (croppedImage.IsCropped)
        {
            SendImageToUI(croppedImage.Cropped);
        }
        else
        {
            SendImageToUI(croppedImage.Original);
        }
    }

    private void SendImageToUI(BitmapSource image)
    {
        _viewModel.ImageOriginal = image;
    }

    private void SendPromptToUI(CroppedImage croppedImage)
    {
        SendPromptToUI(croppedImage.PromptText);
    }

    private void SendPromptToUI(string prompt)
    {
        _viewModel.UiPrompt = prompt;
    }

    private void MyListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ListBox.SelectedItem is CroppedImage selectedImage)
        {
            selectedCroppedImage = selectedImage;
            DebugText($"Selected image {selectedImage.Seconds}");
            SendImageToUI(selectedImage);
            SendPromptToUI(selectedImage);
            SetMetaData(selectedImage);
            _isUpdatingFromList = true;
            slider.Value = selectedImage.Seconds;
            _isUpdatingFromList = false;
        }
        else
        {
            selectedCroppedImage = null;
        }
    }


    private void PromptEditor_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (selectedCroppedImage != null && e.Key == Key.Enter)
        {
            selectedCroppedImage.PromptText = prompt.Text;
            DebugText("Prompt saved!");
            e.Handled = true; // Блокируем только Enter
        }
    }


    private void Slider_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        SetPromptToUI("");
    }
}