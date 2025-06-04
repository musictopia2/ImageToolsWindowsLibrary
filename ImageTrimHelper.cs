namespace ImageToolsWindowsLibrary;
public static class ImageTrimHelper
{
    public static void CropAndTrimImage(this TwoRegionImageEntry entry,
        BasicList<Rectangle> firstRegionDeletes,
        BasicList<Rectangle> secondRegionDeletes,
        string newPath,
        EnumRegionLayoutMode layoutMode,
        Color? trimFillColor = null
        )
    {
        if (!File.Exists(entry.ImageFile))
        {
            throw new FileNotFoundException("Main image file not found", entry.ImageFile);
        }
        using var original = new Bitmap(entry.ImageFile);
        // Crop and trim first region
        using var firstCropped = original.Clone(entry.FirstRegion!.Value, original.PixelFormat);
        using var g1 = Graphics.FromImage(firstCropped);
        using var brush = new SolidBrush(trimFillColor ?? Color.White);

        foreach (var rect in firstRegionDeletes)
        {
            var adjusted = new Rectangle(
                rect.X - entry.FirstRegion.Value.X,
                rect.Y - entry.FirstRegion.Value.Y,
                rect.Width,
                rect.Height);
            g1.FillRectangle(brush, adjusted);
        }

        Bitmap? secondCropped = null;
        if (entry.SecondRegion != null)
        {
            secondCropped = original.Clone(entry.SecondRegion.Value, original.PixelFormat);
            using var g2 = Graphics.FromImage(secondCropped);
            foreach (var rect in secondRegionDeletes)
            {
                var adjusted = new Rectangle(
                    rect.X - entry.SecondRegion.Value.X,
                    rect.Y - entry.SecondRegion.Value.Y,
                    rect.Width,
                    rect.Height);
                g2.FillRectangle(brush, adjusted);
            }
        }

        // Compose final image
        Bitmap finalImage;
        if (secondCropped == null)
        {
            finalImage = new Bitmap(firstCropped.Width, firstCropped.Height);
            using var finalGraphics = Graphics.FromImage(finalImage);
            finalGraphics.DrawImage(firstCropped, Point.Empty);
        }
        else if (layoutMode == EnumRegionLayoutMode.Landscape)
        {
            finalImage = new Bitmap(firstCropped.Width + secondCropped.Width, Math.Max(firstCropped.Height, secondCropped.Height));
            using var finalGraphics = Graphics.FromImage(finalImage);
            finalGraphics.DrawImage(firstCropped, new Point(0, 0));
            finalGraphics.DrawImage(secondCropped, new Point(firstCropped.Width, 0));
        }
        else // Portrait
        {
            finalImage = new Bitmap(Math.Max(firstCropped.Width, secondCropped.Width), firstCropped.Height + secondCropped.Height);
            using var finalGraphics = Graphics.FromImage(finalImage);
            finalGraphics.DrawImage(firstCropped, new Point(0, 0));
            finalGraphics.DrawImage(secondCropped, new Point(0, firstCropped.Height));
        }

        // Save
        var dir = Path.GetDirectoryName(newPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        finalImage.Save(newPath);
        finalImage.Dispose(); // Explicit disposal since it's outside a using block
        secondCropped?.Dispose(); // Same for second region if it was used

    }


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
