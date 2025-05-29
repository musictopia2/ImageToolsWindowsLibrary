namespace ImageToolsWindowsLibrary;
public partial class TopRegionCropSelectorComponent
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Parameter]
    public string ImagePath { get; set; } = string.Empty;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Parameter]
    public Rectangle RegionBounds { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Parameter]
    public int? SuggestedTrimHeight { get; set; }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Parameter]
    public bool ShowRetainedOnly { get; set; } = false;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Parameter]
    public EventCallback<int> OnTrimHeightChanged { get; set; }
    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RenderFragment? ChildContent { get; set; }


    private readonly ImageCropHelper _cropHelper = new();
    private string? _lastImagePath = null;

    private int? _cropHeightSelected = null;

    private string? _desiredImagedData = null;
    //private int _naturalImageWidth;
    //private int _naturalImageHeight;

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
        if (_cropHeightSelected is null)
        {
            return; // no regions selected yet
        }
        switch (key)
        {
            case EnumKey.Down:
                DownArrowClicked();
                break;
            case EnumKey.Up:
                UpArrowClicked();
                break;
        }
    }
    private void DownArrowClicked()
    {
        _cropHeightSelected++;
        OnTrimHeightChanged.InvokeAsync(_cropHeightSelected!.Value);
        StateHasChanged();
    }
    private void UpArrowClicked()
    {
        _cropHeightSelected--;
        OnTrimHeightChanged.InvokeAsync(_cropHeightSelected!.Value);
        StateHasChanged();
    }
    protected override Task OnParametersSetAsync()
    {
        _cropHeightSelected = SuggestedTrimHeight;
        if (!string.IsNullOrWhiteSpace(ImagePath) &&
            File.Exists(ImagePath) &&
            ImagePath != _lastImagePath) // only reload if path changes
        {
            _lastImagePath = ImagePath;
            _cropHelper.LoadImage(_lastImagePath);
            //var (width, height) = _cropHelper.GetNaturalSize();
            //_naturalImageWidth = width;
            //_naturalImageHeight = height;
            _desiredImagedData = _cropHelper.CropImageBase64(RegionBounds);
        }
        return base.OnParametersSetAsync();
    }
    private Rectangle GetTrimmedRegion()
    {
        return new Rectangle(
            RegionBounds.X,
            RegionBounds.Y + _cropHeightSelected!.Value,
            RegionBounds.Width,
            RegionBounds.Height - _cropHeightSelected.Value
        );
    }
    private string GetRemainingImageBase64()
    {
        if (_cropHeightSelected is null)
        {
            return "";
        }
        return _cropHelper.CropImageBase64(GetTrimmedRegion());
    }
    private void HandleClick(Microsoft.AspNetCore.Components.Web.MouseEventArgs e)
    {
        if (SuggestedTrimHeight is not null)
        {
            return; //has to ignore because you already set the suggestion.  you have to use the keyboard from here
        }
        _cropHeightSelected = (int) e.OffsetY;
        OnTrimHeightChanged.InvokeAsync(_cropHeightSelected.Value);
    }
}