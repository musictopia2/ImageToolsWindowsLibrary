namespace ImageToolsWindowsLibrary;
public partial class TwoRegionSelectorComponent(IJSRuntime JSRuntime)
{
    private enum EnumRegionStep
    {
        SelectingFirst,
        SelectingSecond,
        Done
    }

    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string ImagePath { get; set; } = "";
    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool ShowCroppedImages { get; set; }
    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public EnumRegionLayoutMode LayoutMode { get; set; }

    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RenderFragment? ChildContent { get; set; }
    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public EventCallback OnReviewAffirmed { get; set; }

    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int DesiredWidth { get; set; } //helpful so when i click the second point, can figure out what is needed.

    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int DesiredLeft { get; set; } //this is the left side of the image.  so if you want to adjust the left side, then you can use this.
    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int DesiredTop { get; set; } //this is if i know what the top and left is but need to populate the height part alone.

    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Rectangle? ReviewFirstRegion { get; set; }

    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Rectangle? ReviewSecondRegion { get; set; }

    private Rectangle? _firstRectangle = null;
    private Rectangle? _secondRectangle = null;
    private int _naturalImageWidth;
    private int _naturalImageHeight;
    private readonly ImageCropHelper _cropHelper = new();


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
        if (_firstRectangle is null && _secondRectangle is null)
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
            case EnumKey.Left:
                LeftArrowClicked();
                break;
            case EnumKey.Right:
                RightArrowClicked();
                break;
            case EnumKey.F1:
                SetMode(EnumAdjustmentMode.Move, true);
                break;
            case EnumKey.F2:
                SetMode(EnumAdjustmentMode.Resize, true);
                break;
            case EnumKey.F4:
                SetMode(EnumAdjustmentMode.AdjustEdges, true);
                break;
        }
    }

    private string ImageData { get; set; } = "";
    private EnumRegionStep _currentStep = EnumRegionStep.SelectingFirst;

    private string ResizeIcon()
    {
        return """
            <svg xmlns="http://www.w3.org/2000/svg" 
                 fill="none" 
                 viewBox="0 0 24 24" 
                 stroke="blue" 
                 stroke-width="2" 
                 width="24" 
                 height="24">
              <path stroke-linecap="round" stroke-linejoin="round" d="M4 12h16M12 4v16M6 6l12 12M6 18L18 6" />
            </svg>
            """;
    }
    private void SetMode(EnumAdjustmentMode newMode, bool forceRender = true)
    {
        if (DesiredLeft > 0 && DesiredWidth > 0)
        {
            _currentMode = EnumAdjustmentMode.Resize; //must be resize.
            if (forceRender)
            {
                StateHasChanged();
            }
        }
        if (_currentStep == EnumRegionStep.Done)
        {
            if (newMode == EnumAdjustmentMode.Move)
            {
                OnReviewAffirmed.InvokeAsync();
            }
            return; // no adjustments allowed after completion
        }
        if (_currentMode != newMode)
        {
            _currentMode = newMode;
            if (forceRender)
            {
                StateHasChanged();
            }
        }
    }

    public void ApplySuggestions(Rectangle? firstRegion, Rectangle? secondRegion)
    {
        if (firstRegion is null && secondRegion is null)
        {
            throw new CustomBasicException("At least one region must be provided for suggestions.");
        }
        if (firstRegion is not null)
        {
            _firstRectangle = firstRegion.Value;
        }
        if (secondRegion is not null)
        {
            _secondRectangle = secondRegion.Value;
            _currentStep = EnumRegionStep.SelectingSecond; //if you decide to do both, then you adjust the second one.
        }
    }

    private Point? StartPoint { get; set; }
    private Point? EndPoint { get; set; }

    private string? _lastImagePath = null;
    protected override async Task OnParametersSetAsync()
    {
        if (DesiredLeft > 0 && DesiredWidth > 0)
        {
            _currentMode = EnumAdjustmentMode.Resize; //can only resize.
        }
        if (!string.IsNullOrWhiteSpace(ImagePath) &&
            File.Exists(ImagePath) &&
            ImagePath != _lastImagePath) // only reload if path changes
        {
            _lastImagePath = ImagePath;
            _cropHelper.LoadImage(ImagePath);
            var (width, height) = _cropHelper.GetNaturalSize();
            _naturalImageWidth = width;
            _naturalImageHeight = height;
            var bytes = await File.ReadAllBytesAsync(ImagePath);
            ImageData = $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
            _firstRectangle = ReviewFirstRegion;
            _secondRectangle = ReviewSecondRegion;
            if (_firstRectangle.HasValue && _secondRectangle.HasValue)
            {
                _currentStep = EnumRegionStep.Done;
            }
            else if (_firstRectangle.HasValue)
            {
                _currentStep = EnumRegionStep.SelectingSecond;
            }
            else
            {
                _currentStep = EnumRegionStep.SelectingFirst;
            }
        }
    }

    private async Task OnImageLoaded()
    {
        await GetRenderedImageSizeAsync();
        StateHasChanged(); // trigger rerender if layout depends on dimensions
    }

    private ElementReference _imageRef;

    private int _renderedImageWidth;
    private int _renderedImageHeight;
    private async Task GetRenderedImageSizeAsync()
    {
        int y = await JSRuntime.GetContainerHeight(_imageRef);
        int x = await JSRuntime.GetContainerWidth(_imageRef);
        _renderedImageWidth = x;
        _renderedImageHeight = y;
    }



    private Rectangle? GetScaledCropRectangle(Rectangle? rect)
    {
        if (rect is null || _renderedImageWidth == 0 || _renderedImageHeight == 0)
        {
            return null;
        }

        double scaleX = (double)_naturalImageWidth / _renderedImageWidth;
        double scaleY = (double)_naturalImageHeight / _renderedImageHeight;
        return new Rectangle(
            (int)(rect.Value.X * scaleX),
            (int)(rect.Value.Y * scaleY),
            (int)(rect.Value.Width * scaleX),
            (int)(rect.Value.Height * scaleY)
        );
    }

    public TwoRegionImageEntry GetTwoRegionEntry()
    {
        TwoRegionImageEntry output = new()
        {
            ImageFile = ImagePath,
            FirstRegion = _firstRectangle,
            SecondRegion = _secondRectangle
        };
        _firstRectangle = null;
        _secondRectangle = null;
        _currentStep = EnumRegionStep.SelectingFirst;
        return output;
    }
    private Rectangle GetSelectionRectangle(Point p1, Point p2)
    {
        int x = Math.Min(p1.X, p2.X);
        int y = Math.Min(p1.Y, p2.Y);
        int w = Math.Abs(p1.X - p2.X);
        int h = Math.Abs(p1.Y - p2.Y);
        if (DesiredWidth > 0)
        {
            w = DesiredWidth; //i think if you have a desired width, use that.
        }
        return new Rectangle(x, y, w, h);
    }
    private void HandleClick(Microsoft.AspNetCore.Components.Web.MouseEventArgs e)
    {
        var clickedPoint = new Point((int)e.OffsetX, (int)e.OffsetY);

        if (StartPoint.HasValue == false)
        {
            StartPoint = clickedPoint;
            if (DesiredLeft > 0)
            {
                StartPoint = new(DesiredLeft, StartPoint.Value.Y);
            }
        }
        else if (EndPoint.HasValue == false)
        {
            EndPoint = clickedPoint;
            var rect = GetSelectionRectangle(StartPoint.Value, EndPoint.Value);
            if (_currentStep == EnumRegionStep.SelectingFirst)
            {
                _firstRectangle = rect;
            }
            else if (_currentStep == EnumRegionStep.SelectingSecond)
            {
                _secondRectangle = rect;
            }

            StartPoint = null;
            EndPoint = null;

            StateHasChanged();
        }

        StateHasChanged();
    }
    public void ClearSelection()
    {
        if (DesiredLeft > 0 && DesiredTop > 0)
        {
            StartPoint = new(DesiredLeft, DesiredTop);
        }
        else
        {
            StartPoint = null;
        }
        EndPoint = null;
        if (ReviewFirstRegion is null)
        {
            _firstRectangle = null;
            _currentStep = EnumRegionStep.SelectingFirst;
        }
        else
        {
            _currentStep = EnumRegionStep.SelectingSecond; //you can't do the first one anymore.
        }
        _secondRectangle = null;
    }
    private string GetFirstRegionImageBase64()
    {
        if (_firstRectangle is null)
        {
            return "";
        }
        var scaledRect = GetScaledCropRectangle(_firstRectangle);
        return _cropHelper.CropImageBase64(scaledRect!.Value);
    }
    private string GetSecondRegionImageBase64()
    {
        if (_secondRectangle is null)
        {
            return "";
        }
        var scaledRect = GetScaledCropRectangle(_secondRectangle);
        return _cropHelper.CropImageBase64(scaledRect!.Value);
    }

    private void SetActiveRectangle(Rectangle rect)
    {
        if (_currentStep == EnumRegionStep.SelectingSecond && _secondRectangle is not null)
        {
            _secondRectangle = rect;
        }
        else if (_firstRectangle is not null)
        {
            _firstRectangle = rect;
        }
    }
    private Rectangle GetActiveRectangle()
    {
        return _currentStep switch
        {
            EnumRegionStep.SelectingSecond when _secondRectangle is not null => _secondRectangle.Value,
            _ when _firstRectangle is not null => _firstRectangle.Value,
            _ => new()
        };
    }
    private void LeftArrowClicked()
    {
        var rect = GetActiveRectangle();
        if (rect.IsEmpty)
        {
            return;
        }
        if (DesiredLeft > 0)
        {
            return; //has to ignore because you already know the left.
        }
        switch (_currentMode)
        {
            case EnumAdjustmentMode.Move:
                rect.X -= 1;
                break;

            case EnumAdjustmentMode.Resize:
                rect.Width = Math.Max(1, rect.Width - 1);
                break;

            case EnumAdjustmentMode.AdjustEdges:
                rect.X += 1;
                rect.Width = Math.Max(1, rect.Width - 1);
                break;
        }

        SetActiveRectangle(rect);
        StateHasChanged();
    }
    private void RightArrowClicked()
    {
        var rect = GetActiveRectangle();
        if (rect.IsEmpty)
        {
            return;
        }
        if (DesiredLeft > 0)
        {
            return; //has to ignore because you already know the left.
        }
        switch (_currentMode)
        {
            case EnumAdjustmentMode.Move:
                rect.X += 1;
                break;

            case EnumAdjustmentMode.Resize:
                rect.Width += 1;
                break;

            case EnumAdjustmentMode.AdjustEdges:
                rect.X -= 1;
                rect.Width += 1;
                break;
        }

        SetActiveRectangle(rect);
        StateHasChanged();
    }
    private void UpArrowClicked()
    {
        var rect = GetActiveRectangle();
        if (rect.IsEmpty)
        {
            return;
        }
        switch (_currentMode)
        {
            case EnumAdjustmentMode.Move:
                rect.Y -= 1;
                break;

            case EnumAdjustmentMode.Resize:
                rect.Height = Math.Max(1, rect.Height - 1);
                break;

            case EnumAdjustmentMode.AdjustEdges:
                rect.Y += 1;
                rect.Height = Math.Max(1, rect.Height - 1);
                break;
        }

        SetActiveRectangle(rect);
        StateHasChanged();
    }

    private void DownArrowClicked()
    {
        var rect = GetActiveRectangle();
        if (rect.IsEmpty)
        {
            return;
        }
        switch (_currentMode)
        {
            case EnumAdjustmentMode.Move:
                rect.Y += 1;
                break;

            case EnumAdjustmentMode.Resize:
                rect.Height += 1;
                break;

            case EnumAdjustmentMode.AdjustEdges:
                rect.Y -= 1;
                rect.Height += 1;
                break;
        }

        SetActiveRectangle(rect);
        StateHasChanged();
    }
    private enum EnumAdjustmentMode
    {
        Move,
        Resize,
        AdjustEdges
    }
    private EnumAdjustmentMode _currentMode = EnumAdjustmentMode.Move;

    //best to have the parent (child) decide to call when the second one is needed.
    public void BeginSecondRegion()
    {
        if (_firstRectangle is not null)
        {
            _currentStep = EnumRegionStep.SelectingSecond;
            StateHasChanged();
        }
    }
}