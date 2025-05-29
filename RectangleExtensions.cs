namespace ImageToolsWindowsLibrary;
public static class RectangleExtensions
{
    /// <summary>
    /// Gets the remaining area of the suggestion rectangle after subtracting the manual one vertically.
    /// Optionally applies a vertical offset between them.
    /// </summary>
    public static Rectangle GetRemainingBelow(this Rectangle suggestion, Rectangle manual, int verticalOffset = 0)
    {
        // Ensure overlap in horizontal space, otherwise invalid for column-style layout
        if (!suggestion.IntersectsWith(manual))
        {
            throw new CustomBasicException("No overlap");
        }

        int topY = manual.Bottom + verticalOffset;
        int maxY = suggestion.Bottom;

        if (topY >= maxY)
        {
            throw new CustomBasicException("Invalid manual rectangle");
        }

        int height = maxY - topY;
        return new Rectangle(suggestion.X, topY, suggestion.Width, height);
    }
    public static Rectangle TrimFromTop(this Rectangle region, int trimHeight)
    {
        if (trimHeight < 0 || trimHeight >= region.Height)
        {
            throw new ArgumentOutOfRangeException(nameof(trimHeight));
        }

        return new Rectangle(
            region.X,
            region.Y + trimHeight,
            region.Width,
            region.Height - trimHeight
        );
    }
    /// <summary>
    /// Returns a new rectangle widened by <paramref name="widthRequested"/> pixels to the right.
    /// If <paramref name="widthRequested"/> is 0, returns the original rectangle.
    /// Throws if <paramref name="widthRequested"/> is less than 0.
    /// </summary>
    /// <param name="region">Original rectangle</param>
    /// <param name="widthRequested">Number of pixels to widen to the right</param>
    /// <returns>New widened rectangle</returns>
    public static Rectangle WidenToRight(this Rectangle region, int widthRequested)
    {
        if (widthRequested < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(widthRequested), "Width requested must be zero or positive.");
        }

        if (widthRequested == 0)
        {
            return region;
        }

        return new Rectangle(
            region.X,
            region.Y,
            region.Width + widthRequested,
            region.Height
        );
    }
}