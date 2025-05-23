using System.Windows;
namespace ImageToolsWindowsLibrary;
public partial class ImageHighlightViewerComponent
{
    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public ImageHighlightState? Info { get; set; }
    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public EventCallback<Rectangle> OnHighlightChanged { get; set; } // notify parent

    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string HighlightColor { get; set; } = cc1.Blue;

    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int PixelsToGoDown { get; set; } = 10;
    private string CroppedImageData { get; set; } = "";

    private AppKeyboardListener? _keys;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public static Window? MainWindow { get; set; }
    protected override void OnParametersSet()
    {
        base.OnParametersSet();
        if (!string.IsNullOrWhiteSpace(Info?.Path) && File.Exists(Info.Path))
        {
            CroppedImageData = $"data:image/png;base64,{GetCroppedImageBase64(Info.Path, Info.CropArea)}";
        }
    }
    protected override void OnInitialized()
    {
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
    }

    public async Task ArrowDownAsync()
    {
        if (Info == null)
        {
            return;
        }

        Info.CurrentHighlight = new Rectangle(
            Info.CurrentHighlight.X,
            Info.CurrentHighlight.Y + PixelsToGoDown,
            Info.CurrentHighlight.Width,
            Info.CurrentHighlight.Height);

        await OnHighlightChanged.InvokeAsync(Info.CurrentHighlight);
        StateHasChanged(); // Refresh UI
    }

    private static string GetCroppedImageBase64(string imagePath, Rectangle cropArea)
    {
        using Bitmap original = new(imagePath);

        Rectangle safeCrop = Rectangle.Intersect(cropArea, new Rectangle(0, 0, original.Width, original.Height));
        using Bitmap cropped = original.Clone(safeCrop, original.PixelFormat);

        using MemoryStream ms = new();
        cropped.Save(ms, ImageFormat.Png);
        return Convert.ToBase64String(ms.ToArray());
    }

    private string GetContainerStyle()
    {
        return $"position: relative; display: inline-block; width: {Info?.CropArea.Width}px; height: {Info?.CropArea.Height}px;";
    }

    private string GetHighlightStyle()
    {
        if (Info == null || Info.CurrentHighlight.Width == 0 || Info.CurrentHighlight.Height == 0)
            return "display: none;";

        var crop = Info.CropArea;
        var highlight = Info.CurrentHighlight;

        int adjustedX = highlight.X - crop.X;
        int adjustedY = highlight.Y - crop.Y;

        if (adjustedX + highlight.Width <= 0 || adjustedY + highlight.Height <= 0 ||
            adjustedX >= crop.Width || adjustedY >= crop.Height)
        {
            return "display: none;";
        }

        int clampedX = Math.Max(0, adjustedX);
        int clampedY = Math.Max(0, adjustedY);
        int clampedWidth = Math.Min(highlight.Width - (clampedX - adjustedX), crop.Width - clampedX);
        int clampedHeight = Math.Min(highlight.Height - (clampedY - adjustedY), crop.Height - clampedY);

        var color = GetRgbaHighlightColor();
        return $"position: absolute; " +
               $"left: {clampedX}px; top: {clampedY}px; " +
               $"width: {clampedWidth}px; height: {clampedHeight}px; " +
               $"background-color: {color}; pointer-events: none;";
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