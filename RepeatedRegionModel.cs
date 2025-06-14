namespace ImageToolsWindowsLibrary;
public class RepeatedRegionModel
{
    public string ImagePath { get; set; } = "";
    public Rectangle OriginalBounds { get; set; }
    public Rectangle RepeatedHighlight { get; set; }
}