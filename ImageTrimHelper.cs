namespace ImageToolsWindowsLibrary;
public static class ImageTrimHelper
{
    /// <summary>
    /// Loads an image from mainImagePath, crops to desiredRegion,
    /// applies trims (fills) from deletedList relative to desiredRegion,
    /// and saves the cleaned image to newPath.
    /// </summary>
    public static void CropAndTrimImage(
        this Rectangle desiredRegion,
        string mainImagePath,
        BasicList<Rectangle> deletedList,
        string newPath,
        Color? trimFillColor = null)
    {
        if (!File.Exists(mainImagePath))
        {
            throw new FileNotFoundException("Main image file not found", mainImagePath);
        }

        using var original = new Bitmap(mainImagePath);

        // Crop the image to the desired region
        using var cropped = original.Clone(desiredRegion, original.PixelFormat);

        // Prepare graphics to apply trims
        using var g = Graphics.FromImage(cropped);
        using var brush = new SolidBrush(trimFillColor ?? Color.White);

        foreach (var trimRect in deletedList)
        {
            // Adjust trim rectangle relative to cropped image coordinates
            var adjustedRect = new Rectangle(
                trimRect.X - desiredRegion.X,
                trimRect.Y - desiredRegion.Y,
                trimRect.Width,
                trimRect.Height);

            g.FillRectangle(brush, adjustedRect);
        }

        // Make sure directory exists
        var dir = Path.GetDirectoryName(newPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        // Save the resulting image
        cropped.Save(newPath);
    }
}
