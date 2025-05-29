namespace ImageToolsWindowsLibrary;
internal class ImageCropHelper
{
    private string _imagePath = "";
    private int _naturalWidth;
    private int _naturalHeight;

    public void LoadImage(string imagePath)
    {
        _imagePath = imagePath;
        using var bmp = new Bitmap(imagePath);
        _naturalWidth = bmp.Width;
        _naturalHeight = bmp.Height;
    }

    public (int width, int height) GetNaturalSize()
        => (_naturalWidth, _naturalHeight);

    public Rectangle? ScaleRectangleToNatural(Rectangle? renderedRect, int renderedWidth, int renderedHeight)
    {
        if (renderedRect is null)
        {
            return null;
        }
        if (renderedWidth == 0 || renderedHeight == 0)
        {
            return renderedRect;
        }

        double scaleX = (double)_naturalWidth / renderedWidth;
        double scaleY = (double)_naturalHeight / renderedHeight;

        return new Rectangle(
            (int)(renderedRect.Value.X * scaleX),
            (int)(renderedRect.Value.Y * scaleY),
            (int)(renderedRect.Value.Width * scaleX),
            (int)(renderedRect.Value.Height * scaleY)
        );
    }

    public string CropImageBase64(Rectangle naturalRect)
    {
        if (string.IsNullOrEmpty(_imagePath) || naturalRect == Rectangle.Empty)
        {
            return "";
        }

        using var bmp = new Bitmap(_imagePath);
        // Make sure cropping rectangle is within bounds
        var cropRect = Rectangle.Intersect(naturalRect, new Rectangle(0, 0, bmp.Width, bmp.Height));
        if (cropRect.Width == 0 || cropRect.Height == 0)
        {
            return "";
        }

        using var cropped = bmp.Clone(cropRect, bmp.PixelFormat);
        using var ms = new MemoryStream();
        cropped.Save(ms, ImageFormat.Png);
        return $"data:image/png;base64,{Convert.ToBase64String(ms.ToArray())}";
    }
}
