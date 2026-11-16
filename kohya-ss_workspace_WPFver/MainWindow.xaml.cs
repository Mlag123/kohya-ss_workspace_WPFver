using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
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
    private string _videoPath;
    private double _seconds = 0;
    private ImageSaver _imageSaver;
    private bool _isDragging = false;
    private Point _startPoint;
    private FrameExtractor _frameExtractor;
    private string _imagesFolderPath;
    private CroppedImage _currentImage;
    private CropManager _cropManager;
    private MainViewModel _viewModel;
    private const string DefaultResolution = "512";
    private CroppedImage _selectedCroppedImage;
    private bool _isUpdatingFromList = false;
    private double _sliderZero = 0;


    [DllImport("kernel32.dll")]
    static extern bool AllocConsole();

    public MainWindow()
    {
        RenderOptions.ProcessRenderMode = RenderMode.Default;
#if DEBUG
        AllocConsole();
#endif
        InitializeComponent();
        PreInit();
        Init();
    }


    private void PreInit()
    {
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        SizeChanged += (s, e) => { };
        _viewModel.FileSelected += OnFileSelected;
        _viewModel.SaveButtonClick += SaveButtonImage;
        _viewModel.GenerateButtonClick += Generate;
        _viewModel.OpenFolderDialog += LoadImagesFromFolder;
        CompositionTarget.Rendering += OnRendering;
    }


    public void Init()
    {
        // ImageSaver.initFolders();
        _currentImage = new CroppedImage() { IsCropped = false };
        _cropManager = new CropManager();
        _frameExtractor = new FrameExtractor();
        _imageSaver = new ImageSaver(_viewModel);
    }

    private void ParallaxContainer_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.ExplodePaws(e.NewSize.Width, e.NewSize.Height);
        }
    }


    private void OnRendering(object? sender, EventArgs e)
    {
        if (e is not RenderingEventArgs args) return;

        var mousePos = Mouse.GetPosition(ParallaxContainer);

        if (DataContext is MainViewModel vm)
        {
            vm.UpdatePaws(args.RenderingTime, mousePos, ParallaxContainer.ActualWidth, ParallaxContainer.ActualHeight);
        }
    }


    public void LoadImagesFromFolder(string path)
    {
        ResetState();
        _imagesFolderPath = path;

        if (!Directory.Exists(_imagesFolderPath)) return;


        var imagesFile = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly)
            .Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase));


        var resolution = GetNewResolution();
        foreach (var img in imagesFile)
        {
            var bitmap = new BitmapImage(new Uri(img));
            string fileName = Path.GetFileName(img);
            var init_img = new CroppedImage()
            {
                isImagesType = true,
                IsCropped = false,
                SourceFileImageName = fileName,
                FilePath = _videoPath,
                Original = bitmap,
                Cropped = null,
                PromptText = GetPromtFromUi(),
                Seconds = -1, //When -1,  then it is image. if Second >=0, then it is video frame.
                NewResizeWidth = resolution.Width,
                NewResizeHeight = resolution.Height,
            };

            SendImageToUI(init_img);

            AddCroppedImageToListBox(init_img);
        }
    }

    public void OnFileSelected(string path)
    {
        ResetState();
        _viewModel.CroppedImages.Clear();


        _videoPath = path;
        _frameExtractor.LoadVideo(_videoPath);
        slider.Maximum = _frameExtractor.getDuration();


        var bitmap = _frameExtractor.GetFrameAsBitmapSource(_seconds);

        if (bitmap != null && bitmap.CanFreeze) bitmap.Freeze();

        SendImageToUI(bitmap);

        _currentImage.Original = bitmap;
        _currentImage.Seconds = _seconds;
        SetMetaData(_currentImage);
    }


    public void Generate()
    {
        var images = GetCroppedImages();
        if (images.Count == 0) return;

        _viewModel.ProgressMax = images.Count;
        _viewModel.ProgressValue = 0;
        _viewModel.IsGenerating = true;

        _imageSaver.Generate(
            images,
            _viewModel.OutputPath,
            GetNewResolution(),
            (done, totalCount) =>
            {
                _viewModel.ProgressValue = done;

                System.Windows.Application.Current.Dispatcher.Invoke(
                    System.Windows.Threading.DispatcherPriority.Background,
                    new Action(delegate { })
                );
            },
            GetNewFileName()
        );

        _viewModel.IsGenerating = false;
        DebugText("Generate end");
    }

    private string GetNewFileName()
    {
        if (string.IsNullOrEmpty(Filename.Text))
        {
            DebugText("Filename is empty!");
            return "";
        }

        return Filename.Text;
    }

    private ObservableCollection<CroppedImage> GetCroppedImages()
    {
        return _viewModel.CroppedImages;
    }


    private string getOutputPath()
    {
        return _viewModel.OutputPath;
    }

    private void SaveButtonImage()
    {
        if (_currentImage != null)
        {
            if (!IsNameExist())
            {
                DebugText("Image name is empty!");
                return;
            }

            var resolution = GetNewResolution();

            CroppedImage origImage = new CroppedImage()
            {
                IsCropped = false,
                FilePath = _videoPath,
                Original = _currentImage.Original,
                Cropped = null,
                PromptText = GetPromtFromUi(),
                Seconds = _currentImage.Seconds,
                NewResizeWidth = resolution.Width,
                NewResizeHeight = resolution.Height,
            };
            AddCroppedImageToListBox(origImage);
        }
    }

    private void ResetState()
    {
        _seconds = 0;
        slider.Value = 0;
        _currentImage = new CroppedImage();
        ResetCroppedImages();
    }

    private void SetMetaData(CroppedImage croppedImage)
    {
        if (croppedImage.IsCropped)
        {
            _viewModel.UpdateTimeline(croppedImage.Seconds, croppedImage.Cropped);
        }
        else
        {
            _viewModel.UpdateTimeline(croppedImage.Seconds, croppedImage.Original);
        }
    }

    private void TimeLine_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingFromList) return;
        try
        {
            this._seconds = slider.Value;
            _currentImage.Seconds = _seconds;
            _currentImage.Original = _frameExtractor.GetFrameAsBitmapSource(_seconds);

            SendImageToUI(_currentImage);
            SetMetaData(_currentImage);
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
        if (IsImageExist())
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


    private bool IsImageExist()
    {
        return DataContext is MainViewModel vs && vs.ImageOriginal != null;
    }


    private void CropImage()
    {
        if (!IsNameExist())
        {
            DebugText("Image name is empty!");
            return;
        }

        if (IsImageExist() && SelectionRect.Visibility == Visibility.Visible && SelectionRect.Width > 0 &&
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

                if (_currentImage.isImagesType)
                {
                    _currentImage.IsCropped = true;
                    _currentImage.Cropped = _cropManager.CropImage(x, y, w, h, (BitmapSource)PictureOutputMedia.Source);
                    SendImageToUI(_currentImage);
                    SendPromptToUI("");
                }
                else
                {
                    CroppedImage cropImg = new CroppedImage()
                    {
                        IsCropped = true,
                        FilePath = _videoPath,
                        Original = _currentImage.Original,
                        Cropped = _cropManager.CropImage(x, y, w, h, (BitmapSource)PictureOutputMedia.Source),
                        PromptText = GetPromtFromUi(),
                        Seconds = _currentImage.Seconds,
                        NewResizeWidth = resolution.Width,
                        NewResizeHeight = resolution.Height,
                    };
                    SendImageToUI(cropImg);
                    // SetMetaData(cropImg);
                    AddCroppedImageToListBox(cropImg);
                    SendPromptToUI("");
                }
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

    private void ResetCroppedImages()
    {
        _viewModel.CroppedImages.Clear();
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
        SetResolutionInUi();

        return new Resolution() { Width = 512, Height = 512 };
    }


    private bool IsNameExist()
    {
        return !string.IsNullOrEmpty(Filename.Text) && Filename.Text.Length < 255;
    }

    private void SetResolutionInUi(Resolution resolution = null)
    {
        if (resolution == null)
        {
            _viewModel.ResolutionWidth = DefaultResolution;
            _viewModel.ResolutionHeight = DefaultResolution;
            return;
        }

        _viewModel.ResolutionWidth = resolution.Width.ToString();
        _viewModel.ResolutionHeight = resolution.Height.ToString();
    }


    private string GetPromtFromUi()
    {
        if (!string.IsNullOrEmpty(prompt.Text))
        {
            return prompt.Text;
        }

        return "";
    }


    private void SetPromptToUi(string prompt)
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
        if (ListBox.SelectedItem is CroppedImage _selectedImage)
        {
            if (_selectedImage.isImagesType)
            {
                //When true, this picture. Antoher then frame.
                _currentImage = _selectedImage;
                _selectedCroppedImage = _selectedImage;
                DebugText($"Selected image {_selectedImage.SourceFileImageName}");
                SendImageToUI(_selectedImage);
                SendPromptToUI(_selectedImage);
                SetMetaData(_selectedImage);
                _isUpdatingFromList = true;
                slider.Value = _sliderZero;
                _isUpdatingFromList = false;
            }
            else
            {
                _selectedCroppedImage = _selectedImage;
                DebugText($"Selected image {_selectedImage.Seconds}");
                SendImageToUI(_selectedImage);
                SendPromptToUI(_selectedImage);
                SetMetaData(_selectedImage);
                _isUpdatingFromList = true;
                slider.Value = _selectedImage.Seconds;
                _isUpdatingFromList = false;
            }
        }
        else
        {
            _selectedCroppedImage = null;
        }
    }


    private void PromptEditor_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (_selectedCroppedImage != null && e.Key == Key.Enter)
        {
            _selectedCroppedImage.PromptText = prompt.Text;
            DebugText("Prompt saved!");
            e.Handled = true;
        }
    }


    private void Slider_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        SetPromptToUi("");
    }

    private void MenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        if (ListBox.SelectedItem is CroppedImage selectedImage)
        {
            DebugText($"Removed: {selectedImage.SourceFileImageName}");
            _viewModel.CroppedImages.Remove(selectedImage);
        }
    }
}