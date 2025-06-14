namespace ImageToolsWindowsLibrary;
public class ImageHightlightResult
{
    public BasicList<Rectangle> HighlightedRegions { get; set; } = [];
    public BasicList<RepeatedRegionModel> RepeatedRegions { get; set; } = []; //this will used for future requests.
}