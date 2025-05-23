namespace ImageToolsWindowsLibrary;
public class ImageHighlightState
{
    public string Path { get; set; } = "";
    public Rectangle CropArea { get; set; }
    public Rectangle OriginalHighlight { get; set; }
    public Rectangle CurrentHighlight { get; set; }
    public bool Completed { get; set; }
    /// <summary>
    /// Optional description for labeling regions (e.g., "First Region", "Second Region").
    /// Useful for internal tools and region tracking.
    /// </summary>
    public string Description { get; set; } = "";
}