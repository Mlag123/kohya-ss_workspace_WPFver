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
    private double seconds = 0;
    private string output_path;
    private ImageSaver _imageSaver;
    private bool _isDragging = false;
    private Point _startPoint;
    private FrameExtractor frame_extractor;

    private List<CroppedImage> croppedImages;
    private CroppedImage currentImage;
    private bool isCropped = false;
    private int scaleX, scaleY;
    private CropManager cropManager;

    private MainViewModel _viewModel;
    private const string DEFAULT_RESOLUTION = "512";

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;
        _viewModel.FileSelected += OnFileSelected;

        Init();
    }

    public void OnFileSelected(string path)
    {
        ResetState(); // Сбрасываем старые данные

        videoPath = path;
        frame_extractor.LoadVideo(videoPath);
        slider.Maximum = frame_extractor.getDuration();


        var bitmap = frame_extractor.GetFrameAsBitmapSource(seconds);

        // Замораживаем, если нужно (чтобы UI не упал)
        if (bitmap != null && bitmap.CanFreeze) bitmap.Freeze();

        // Обновляем ViewModel
        SendImageToUI(bitmap);

        // Если currentImage нужен для дальнейшей логики — обновляем его тоже
        currentImage.Original = bitmap;
        currentImage.Seconds = seconds;
        SetMetaData(currentImage);
    }

    public void Init()
    {
        ImageSaver.initFolders();
        currentImage = new CroppedImage() { IsCropped = false };
        cropManager = new CropManager();
        frame_extractor = new FrameExtractor();
        _imageSaver = new ImageSaver();
        croppedImages = new List<CroppedImage>();
        //  VideoTimelineLayout.Text = "00:00";
        //   slider.Maximum = frame_extractor.getDuration();
    }

    private void ResetState()
    {
        seconds = 0;
        slider.Value = 0;
        currentImage = new CroppedImage();

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

    private void SetMetaData(CroppedImage croppedImage)
    {
        if (DataContext is MainViewModel vs)
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


    /// <summary>
    /// DEPRECATED FUNC
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void FilePicker_OnClick(object sender, RoutedEventArgs e)
    {
        /*OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "Video files (*.mp4)|*.mp4|All files (*.*)|*.*";

        if (openFileDialog.ShowDialog() == true)
        {
            ResetState();
            videoPath = openFileDialog.FileName;
            DebugText($"Opened file: {videoPath}");
            frame_extractor.LoadVideo(videoPath);
            slider.Maximum = frame_extractor.getDuration();
            currentImage = new CroppedImage()
            {
                Original = frame_extractor.GetFrameAsBitmapSource(seconds),
                Seconds = seconds
            };
            updateUI();
        }*/
    }


    /// <summary>
    /// DEPRECATED FUNC
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
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


    /// <summary>
    /// DEPRECATED FUNC!
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Generate_OnClick(object sender, RoutedEventArgs e)
    {
        //   _imageSaver.Generate(output_path,croppedImages,scaleX,scaleY);
    }

    private void Console_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Прокручиваем вниз, когда текст меняется
        ((TextBox)sender).ScrollToEnd();
    }


    /// <summary>
    /// DEPRECARED FUNC
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SaveBut_OnClick(object sender, RoutedEventArgs e)
    {
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
            // Координаты прямоугольника на Canvas
            double rectX = Canvas.GetLeft(SelectionRect);
            double rectY = Canvas.GetTop(SelectionRect);
            double rectW = SelectionRect.Width;
            double rectH = SelectionRect.Height;

            // Размеры Canvas и картинки
            double canvasWidth = CropCanvas.ActualWidth;
            double canvasHeight = CropCanvas.ActualHeight;
            double imageWidth = PictureOutputMedia.Source.Width;
            double imageHeight = PictureOutputMedia.Source.Height;

            // Считаем масштаб (как в Stretch="Uniform")
            double scale = Math.Min(canvasWidth / imageWidth, canvasHeight / imageHeight);

            // Реальные размеры картинки на экране
            double renderedWidth = imageWidth * scale;
            double renderedHeight = imageHeight * scale;

            // Вычисляем ёбаные отступы (letterbox/pillarbox)
            double offsetX = (canvasWidth - renderedWidth) / 2.0;
            double offsetY = (canvasHeight - renderedHeight) / 2.0;

            // Пересчитываем координаты относительно самой картинки
            double imageX = (rectX - offsetX) / scale;
            double imageY = (rectY - offsetY) / scale;
            double imageW = rectW / scale;
            double imageH = rectH / scale;

            // Ограничиваем, чтобы не вылезти за пределы картинки
            int x = (int)Math.Max(0, imageX);
            int y = (int)Math.Max(0, imageY);
            int w = (int)Math.Min(imageWidth - x, imageW);
            int h = (int)Math.Min(imageHeight - y, imageH);

            var resolution = GetNewResolution();
            CroppedImage cropImg = new CroppedImage()
            {
                IsCropped = true,
                ImageName = currentImage.ImageName,
                FilePath = videoPath,
                Original = currentImage.Original,
                Cropped = cropManager.CropImage(x, y, w, h, (BitmapSource)PictureOutputMedia.Source),
                PromptText = GetPromtFromUI(),
                Seconds = currentImage.Seconds,
                NewResizeWidth = resolution.Width,
                NewResizeHeight = resolution.Height,
            };
            SendImageToUI(cropImg);

            croppedImages.Add(cropImg);
            ListBox.ItemsSource = null;
            ListBox.ItemsSource = croppedImages;
            SendPromptToUI("");
        }
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
        if (DataContext is MainViewModel vm)
        {
            if (resolution == null)
            {
                vm.ResolutionWidth = DEFAULT_RESOLUTION;
                vm.ResolutionHeight = DEFAULT_RESOLUTION;
            }
            else
            {
                vm.ResolutionWidth = resolution.Width.ToString();
                vm.ResolutionHeight = resolution.Height.ToString();
            }
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

    private void DebugText(string text)
    {
        if (DataContext is MainViewModel vs)
        {
            vs.DebugText(text);
        }
    }


    private void SendImageToUI(CroppedImage croppedImage)
    {
        if (croppedImage == null) return;


        if (croppedImage.IsCropped && croppedImage.Cropped != null)
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
        if (DataContext is MainViewModel vm)
        {
            vm.ImageOriginal = image;
        }
    }

    private void SendPromptToUI(CroppedImage croppedImage)
    {
        SendPromptToUI(croppedImage.PromptText);
    }

    private void SendPromptToUI(string prompt)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.UiPrompt = prompt;
        }
    }

    private void MyListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ListBox.SelectedItem is CroppedImage selectedImage)
        {
            DebugText($"Selected image {selectedImage.Seconds}");
            SendImageToUI(selectedImage);
            SendPromptToUI(selectedImage);
            SetMetaData(selectedImage);
        }
    }


    private void PromptEditor_OnKeyDown(object sender, KeyEventArgs e)
    {
        //    String a = PromptEditor.Text;
        //  currentImage.PromptText = a;
    }

    ///deprecated
    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        //CropImage();
    }
}