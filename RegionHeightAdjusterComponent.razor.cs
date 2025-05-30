using System.ComponentModel;

namespace ImageToolsWindowsLibrary;
public partial class RegionHeightAdjusterComponent
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Parameter]
    public string ImagePath { get; set; } = string.Empty;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Parameter]
    public Rectangle RegionBounds { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Parameter]
    public int InitialExtraHeight { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Parameter]
    public EventCallback<int> OnSave { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Parameter]
    public string ContainerWidth { get; set; } = "40vw";

    private int _heightAdjustment = 0;
    private Rectangle _previousBounds;
    private string? _adjustedRegionImageData;
    private string? _referenceExpandedImageData;
    private readonly ImageCropHelper _cropHelper = new();
    private string? _lastImagePath;
    private AppKeyboardListener? _keys;
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public static System.Windows.Window? MainWindow { get; set; }
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
    private void Keys_KeyUp(EnumKey key)
    {
        switch (key)
        {
            case EnumKey.Up:
                _heightAdjustment--;
                PopulateAdjustedRegion();
                break;
            case EnumKey.PageUp:
                _heightAdjustment -= 10;
                PopulateAdjustedRegion();
                break;
            case EnumKey.Down:
                _heightAdjustment++;
                PopulateAdjustedRegion();
                break;
            case EnumKey.F1:
                OnSave.InvokeAsync(_heightAdjustment);
                break;
        }
    }
    protected override void OnParametersSet()
    {
        bool regionChanged = _previousBounds != RegionBounds;
        if (!string.IsNullOrWhiteSpace(ImagePath) &&
            File.Exists(ImagePath) &&
            ImagePath != _lastImagePath)
        {
            _lastImagePath = ImagePath;
            _cropHelper.LoadImage(ImagePath);
            PopulateInitialRegions();
            return;
        }
        if (regionChanged)
        {
            PopulateInitialRegions();
        }
    }

    private void PopulateInitialRegions()
    {
        _previousBounds = RegionBounds;
        _heightAdjustment = 0;

        // Reference expanded view: region plus extra height on bottom
        var referenceBounds = new Rectangle(
            RegionBounds.X,
            RegionBounds.Y,
            RegionBounds.Width,
            RegionBounds.Height + InitialExtraHeight
        );
        _referenceExpandedImageData = _cropHelper.CropImageBase64(referenceBounds);

        // Default adjusted region: same as input region
        _adjustedRegionImageData = _cropHelper.CropImageBase64(RegionBounds);
    }

    private void PopulateAdjustedRegion()
    {
        int newHeight = RegionBounds.Height + _heightAdjustment;

        // Enforce minimum height
        if (newHeight < 1)
        {
            return;
        }

        var adjustedBounds = new Rectangle(
            RegionBounds.X,
            RegionBounds.Y,
            RegionBounds.Width,
            newHeight
        );

        _adjustedRegionImageData = _cropHelper.CropImageBase64(adjustedBounds);
        StateHasChanged();
    }
}