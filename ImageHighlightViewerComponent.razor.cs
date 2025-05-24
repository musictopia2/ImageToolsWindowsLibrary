using System.Drawing.Drawing2D;
using System.Windows;
using System.ComponentModel;
namespace ImageToolsWindowsLibrary;
public partial class ImageHighlightViewerComponent(IJSRuntime js)
{
    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ImageHighlightState? Info { get; set; }
    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string Height { get; set; } = "95vh";
    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public EventCallback<Rectangle> OnHighlightChanged { get; set; } // notify parent

    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string HighlightColor { get; set; } = cc1.Blue;
    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int ScrollOverlapPixels { get; set; } = 100; // Buffer for scrolling, default to 100 pixels
    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int HighlightDeltaY { get; set; } = 10;

    private ElementReference? _element = default;
    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public float ZoomLevel { get; set; } = 1; // Default zoom level
    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool HighlightRelativeToCrop { get; set; } = false;


    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public EventCallback OnHighlightMovedOffBottom { get; set; }

    private string CroppedImageData { get; set; } = "";
    private ScrollHelperClass? _scrollHelper;
    private AppKeyboardListener? _keys;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public static Window? MainWindow { get; set; }
    private Rectangle? _previousCrop;
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!string.IsNullOrWhiteSpace(Info?.Path) && File.Exists(Info.Path))
        {
            CroppedImageData = $"data:image/png;base64,{GetCroppedImageBase64(Info.Path, Info.CropArea, ZoomLevel)}";
        }
    }
    protected override async Task OnParametersSetAsync()
    {
        if (_previousCrop is null && Info is not null)
        {
            _previousCrop = Info.CropArea;
            return;
        }
        if (Info is null)
        {
            return;
        }
        if (_previousCrop == Info.CropArea)
        {
            return;
        }
        if (_element.HasValue == false || _scrollHelper is null)
        {
            return;
        }
        _previousCrop = Info.CropArea;
        //found that smooth scrolling made me even more dizzy, so i am not doing that anymore.
        await _scrollHelper.ScrollToTop(_element.Value);
    }
    protected override void OnInitialized()
    {
        _scrollHelper = new(js);
        if (AppKeyboardListener.MainWindow is null && MainWindow is not null)
        {
            AppKeyboardListener.MainWindow = MainWindow;
        }
        _keys = new();
        _keys.KeyUp += Keys_KeyUp;
        base.OnInitialized();
    }
    private async void Keys_KeyUp(EnumKey key)
    {
        if (key == EnumKey.Down)
        {
            await ArrowDownAsync(); // Use the method to keep logic centralized
        }
        if (key == EnumKey.Up)
        {
            await ArrowUpAsync(); // Use the method to keep logic centralized
        }
        if (key == EnumKey.PageDown)
        {
            //do automatically now (but has to manually be done).
            await ScrollDownOnePageAsync();
        }
        if (key == EnumKey.PageUp)
        {
            await ScrollUpOnePageAsync();
        }
    }
    private async Task ScrollUpOnePageAsync()
    {
        if (_element.HasValue)
        {
            await _scrollHelper!.ScrollUpOnePage(_element.Value, ScrollOverlapPixels);
        }
    }
    private async Task ScrollDownOnePageAsync()
    {
        if (_element.HasValue)
        {
            await _scrollHelper!.ScrollDownOnePage(_element.Value, ScrollOverlapPixels);
        }
    }
    public async Task ArrowDownAsync()
    {
        if (Info == null || CroppedImageData is null)
        {
            return;
        }

        int adjustedPixels = HighlightDeltaY;

        // Calculate new highlight Y (in whatever coordinate space it currently is)
        int newHighlightY = Info.CurrentHighlight.Y + adjustedPixels;

        int adjustedY;
        if (HighlightRelativeToCrop)
        {
            // Highlight is already relative to crop, so use newHighlightY directly
            adjustedY = newHighlightY;
        }
        else
        {
            // Highlight is relative to full image, convert to crop-relative by subtracting CropArea.Y
            adjustedY = newHighlightY - Info.CropArea.Y;
        }

        int highlightBottom = adjustedY + Info.CurrentHighlight.Height;

        // Check if highlight bottom is beyond the crop height
        if (highlightBottom > Info.CropArea.Height)
        {
            await OnHighlightMovedOffBottom.InvokeAsync(null);
            return;
        }

        // Update highlight position (still in original coordinate space)
        Info.CurrentHighlight = new Rectangle(
            Info.CurrentHighlight.X,
            newHighlightY,
            Info.CurrentHighlight.Width,
            Info.CurrentHighlight.Height);

        await OnHighlightChanged.InvokeAsync(Info.CurrentHighlight);
        StateHasChanged();
    }
    private async Task ArrowUpAsync()
    {
        if (Info == null || CroppedImageData is null)
        {
            return;
        }

        int adjustedPixels = HighlightDeltaY;
        int newY = Math.Max(0, Info.CurrentHighlight.Y - adjustedPixels); // prevent scrolling above image

        Info.CurrentHighlight = new Rectangle(
            Info.CurrentHighlight.X,
            newY,
            Info.CurrentHighlight.Width,
            Info.CurrentHighlight.Height);

        await OnHighlightChanged.InvokeAsync(Info.CurrentHighlight);
        StateHasChanged(); // Refresh UI
    }
    private static string GetCroppedImageBase64(string imagePath, Rectangle cropArea, double zoomLevel)
    {
        using Bitmap original = new(imagePath);

        Rectangle safeCrop = Rectangle.Intersect(cropArea, new Rectangle(0, 0, original.Width, original.Height));
        using Bitmap cropped = original.Clone(safeCrop, original.PixelFormat);

        int newWidth = (int)(cropped.Width * zoomLevel);
        int newHeight = (int)(cropped.Height * zoomLevel);

        using Bitmap zoomed = new(newWidth, newHeight);
        using Graphics g = Graphics.FromImage(zoomed);
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.DrawImage(cropped, new Rectangle(0, 0, newWidth, newHeight));

        using MemoryStream ms = new();
        zoomed.Save(ms, ImageFormat.Png);
        return Convert.ToBase64String(ms.ToArray());
    }

    private string GetContainerStyle()
    {
        if (Info == null)
        {
            return "";
        }

        int zoomedWidth = (int)(Info.CropArea.Width * ZoomLevel);
        int zoomedHeight = (int)(Info.CropArea.Height * ZoomLevel);

        return $"position: relative; display: inline-block; width: {zoomedWidth}px; height: {zoomedHeight}px;";
    }
    private string GetHighlightStyle()
    {
        if (Info == null || Info.CurrentHighlight.Width == 0 || Info.CurrentHighlight.Height == 0)
        {
            return "display: none;";
        }

        var crop = Info.CropArea;
        var highlight = Info.CurrentHighlight;

        int adjustedX = HighlightRelativeToCrop ? highlight.X : highlight.X - crop.X;
        int adjustedY = HighlightRelativeToCrop ? highlight.Y : highlight.Y - crop.Y;

        // Only clamp if we're drawing inside the crop area
        if (HighlightRelativeToCrop == false)
        {
            if (adjustedX + highlight.Width <= 0 || adjustedY + highlight.Height <= 0 ||
                adjustedX >= crop.Width || adjustedY >= crop.Height)
            {
                return "display: none;";
            }

            int clampedX = Math.Max(0, adjustedX);
            int clampedY = Math.Max(0, adjustedY);
            int clampedWidth = Math.Min(highlight.Width - (clampedX - adjustedX), crop.Width - clampedX);
            int clampedHeight = Math.Min(highlight.Height - (clampedY - adjustedY), crop.Height - clampedY);

            double zoom = ZoomLevel;
            var color = GetRgbaHighlightColor();

            return $"position: absolute; " +
                   $"left: {clampedX * zoom}px; top: {clampedY * zoom}px; " +
                   $"width: {clampedWidth * zoom}px; height: {clampedHeight * zoom}px; " +
                   $"background-color: {color}; pointer-events: none;";
        }
        else
        {
            // Use highlight directly, no clamping — assume already valid for cropped image
            double zoom = ZoomLevel;
            var color = GetRgbaHighlightColor();

            return $"position: absolute; " +
                   $"left: {adjustedX * zoom}px; top: {adjustedY * zoom}px; " +
                   $"width: {highlight.Width * zoom}px; height: {highlight.Height * zoom}px; " +
                   $"background-color: {color}; pointer-events: none;";
        }
    }
    private string GetRgbaHighlightColor()
    {
        if (string.IsNullOrWhiteSpace(HighlightColor))
        {
            return "rgba(255, 255, 0, 0.2)";
        }

        try
        {
            var color = ColorTranslator.FromHtml(HighlightColor);
            return $"rgba({color.R}, {color.G}, {color.B}, 0.2)";
        }
        catch
        {
            return "rgba(255, 255, 0, 0.2)";
        }
    }
}