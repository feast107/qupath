using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;

namespace SharPath.Controls;

public partial class ImageViewer : UserControl
{
    private Image? _image;
    private ScrollViewer? _scroll;
    private Point _last;
    private bool _panning;
    private ScaleTransform _scale = new ScaleTransform(1,1);
    private TranslateTransform _translate = new TranslateTransform();
    private TransformGroup _tg = new TransformGroup();

    public static readonly DirectProperty<ImageViewer, string?> SourcePathProperty =
        AvaloniaProperty.RegisterDirect<ImageViewer, string?>(
            nameof(SourcePath), o => o.SourcePath, (o, v) => o.SourcePath = v);

    private string? _sourcePath;
    public string? SourcePath
    {
        get => _sourcePath;
        set
        {
            _sourcePath = value;
            if (!string.IsNullOrEmpty(_sourcePath))
            {
                try
                {
                    var bmp = new Bitmap(_sourcePath);
                    _image!.Source = bmp;
                    ResetView();
                }
                catch { }
            }
        }
    }

    public ImageViewer()
    {
        InitializeComponent();
        _tg.Children.Add(_scale);
        _tg.Children.Add(_translate);
        this.AttachedToVisualTree += ImageViewer_AttachedToVisualTree;
    }

    private void ImageViewer_AttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        _image = this.FindControl<Image>("PART_Image");
        _scroll = this.FindControl<ScrollViewer>("PART_ScrollViewer");
        if (_image != null)
        {
            _image.RenderTransform = _tg;
            _image.PointerWheelChanged += Image_PointerWheelChanged;
            _image.PointerPressed += Image_PointerPressed;
            _image.PointerMoved += Image_PointerMoved;
            _image.PointerReleased += Image_PointerReleased;
        }
    }

    private void ResetView()
    {
        _scale.ScaleX = 1;
        _scale.ScaleY = 1;
        _translate.X = 0;
        _translate.Y = 0;
    }

    private void Image_PointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var delta = e.Delta.Y > 0 ? 1.2 : 1.0 / 1.2;
        var old = _scale.ScaleX;
        var newScale = old * delta;
        if (newScale < 0.1) newScale = 0.1;
        if (newScale > 20) newScale = 20;

        // Zoom centered at pointer
        var p = e.GetPosition(_image);
        var ox = p.X;
        var oy = p.Y;

        _translate.X = (_translate.X - ox) * (newScale / old) + ox;
        _translate.Y = (_translate.Y - oy) * (newScale / old) + oy;

        _scale.ScaleX = newScale;
        _scale.ScaleY = newScale;
    }

    private void Image_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _panning = true;
            _last = e.GetPosition(this);
        }
    }

    private void Image_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (_panning)
        {
            var pt = e.GetPosition(this);
            var dx = pt.X - _last.X;
            var dy = pt.Y - _last.Y;
            _last = pt;
            _translate.X += dx;
            _translate.Y += dy;
        }
    }

    private void Image_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _panning = false;
    }
}
