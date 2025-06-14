namespace ImageToolsWindowsLibrary;
public partial class HighlightRegionsSelectorComponent(IJSRuntime js, IToast toast)
{
    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string OriginalImagePath { get; set; } = "";

    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Rectangle Bounds { get; set; }

    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public BasicList<RepeatedRegionModel> RepeatRegionList { get; set; } = [];

    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RenderFragment? ChildContent { get; set; }
    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public EventCallback<ImageHightlightResult> OnCompleted { get; set; }

    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Rectangle PreviousHighlightRegion { get; set; }

    private KeySuppressorClass? _suppress;
    private readonly BasicList<Rectangle> _regions = [];
    private bool _completed;
    private Rectangle _currentRegion = new();
    private readonly ImageCropHelper _cropHelper = new();
    private AppKeyboardListener? _keys;
    private BasicList<RepeatedRegionModel> _currentRepeatList = []; //if i send, can't send it back
    private bool _alreadyRepeated;
    private bool _alreadySuggested;
    private Rectangle _lastSuggestion;
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
        if (_completed)
        {
            return;
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
                SetMode(EnumAdjustmentMode.Move);
                break;
            case EnumKey.F2:
                SetMode(EnumAdjustmentMode.Resize);
                break;
            case EnumKey.F3:
                SetMode(EnumAdjustmentMode.AdjustEdges);
                break;
            case EnumKey.F4:
                StartNewRegion(true);
                toast.ShowInfoToast("Starting New Region");
                break;
            case EnumKey.F5:
                SetRepeatedRegion();
                break;
            case EnumKey.F6:
                Complete();
                break;
            case EnumKey.F7:
                ProcessPossibleSuggestion();
                break;
        }
    }
    private void ProcessPossibleSuggestion()
    {
        if (_alreadySuggested)
        {
            return;
        }
        //if i had a current region, will be wiped out unless i chose to start new region.
        if (_currentRegion.IsEmpty == false)
        {
            StartNewRegion(); //go ahead and start new region first.
        }
        if (PreviousHighlightRegion.IsEmpty)
        {
            return; //was empty so no suggestion can be given
        }
        _alreadySuggested = true;
        _currentRegion = new(PreviousHighlightRegion.X, PreviousHighlightRegion.Y + PreviousHighlightRegion.Height, PreviousHighlightRegion.Width, PreviousHighlightRegion.Height);
        _lastSuggestion = new(PreviousHighlightRegion.X, PreviousHighlightRegion.Y + PreviousHighlightRegion.Height, PreviousHighlightRegion.Width, PreviousHighlightRegion.Height); ;
        //hopefully this simple (?)
        StateHasChanged();
    }
    private string GetHighlightColor(Rectangle item)
    {
        var isRepeated = RepeatRegionList.Any(r => IsProperRectangleEqual(r.RepeatedHighlight, item)) ||
                    _currentRepeatList.Any(r => IsProperRectangleEqual(r.RepeatedHighlight, item));
        var color = isRepeated ? "rgba(0, 0, 0, 0.15);" : "rgba(0, 0, 255, 0.15);";
        return color;
    }
    private void SetRepeatedRegion()
    {
        if (_alreadyRepeated)
        {
            return;
        }
        if (_currentRegion.IsEmpty)
        {
            return;
        }
        _alreadyRepeated = true;
        RepeatedRegionModel repeat = new()
        {
            ImagePath = _desiredImagePath,
            OriginalBounds = Bounds,
            RepeatedHighlight = _currentRegion
        };
        _currentRepeatList.Add(repeat);
        StateHasChanged();
    }
    private void StartNewRegion(bool forceRender = true)
    {
        if (_currentRegion.IsEmpty)
        {
            return; //can't because its already empty
        }
        _regions.Add(_currentRegion);
        ClearSelection(false);
        if (forceRender)
        {
            StateHasChanged();
        }
    }
    private void FinalSend(BasicList<Rectangle> bounds)
    {
        ImageHightlightResult result = new();
        result.HighlightedRegions = bounds;
        result.RepeatedRegions = _currentRepeatList;
        _completed = true;
        ClearSelection(false);
        OnCompleted.InvokeAsync(result);
    }
    private void Complete()
    {
        if (_currentRegion.IsEmpty && _regions.Count == 0)
        {
            //this means only one region.
            return; //must have at least one item being highlighted.
        }
        if (StartPoint is not null && EndPoint is null)
        {
            return; //you have to finish or clear out first.
        }
        if (_currentRegion.IsEmpty)
        {
            FinalSend(_regions);
            return;
        }
        _regions.Add(_currentRegion);
        ClearSelection(false); //not manually this time.
        FinalSend(_regions);
    }
    private string ImageData { get; set; } = "";
    private static string ResizeIcon()
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
    private Point? StartPoint { get; set; }
    private Point? EndPoint { get; set; }
    private string? _lastImagePath = null;
    string _desiredImagePath = "";
    protected override void OnParametersSet()
    {
        StartPoint = null;
        _desiredImagePath = OriginalImagePath;
        _completed = false;
        _regions.Clear();
        _currentRepeatList.Clear();
        EndPoint = null;
        _lastSuggestion = new();
        _alreadySuggested = false;
        if (!string.IsNullOrWhiteSpace(_desiredImagePath) &&
            File.Exists(_desiredImagePath) &&
            _desiredImagePath != _lastImagePath) // only reload if path changes
        {
            _lastImagePath = _desiredImagePath;
            _cropHelper.LoadImage(_desiredImagePath);
        }
        ImageData = GetRegionImageBase64(Bounds); //i think.
        PossibleAutomateRepeatedRegions();
    }
    private void PossibleAutomateRepeatedRegions()
    {
        if (RepeatRegionList.Count == 0)
        {
            return;
        }
        var match = RepeatRegionList.FirstOrDefault(item =>
            item.ImagePath == _lastImagePath &&
            IsProperRectangleEqual(Bounds, item.OriginalBounds));
        if (match is not null)
        {
            _alreadyRepeated = true;
            _regions.Add(match.RepeatedHighlight);
        }
    }
    private static bool IsProperRectangleEqual(Rectangle original, Rectangle repeat)
    {

        return original.X == repeat.X &&
               original.Y == repeat.Y &&
               original.Width == repeat.Width &&
               original.Height == repeat.Height;
    }
    private static Rectangle GetSelectionRectangle(Point p1, Point p2)
    {
        int x = Math.Min(p1.X, p2.X);
        int y = Math.Min(p1.Y, p2.Y);
        int w = Math.Abs(p1.X - p2.X);
        int h = Math.Abs(p1.Y - p2.Y);
        return new Rectangle(x, y, w, h);
    }
    private void HandleClick(Microsoft.AspNetCore.Components.Web.MouseEventArgs e)
    {
        var clickedPoint = new Point((int)e.OffsetX, (int)e.OffsetY);

        if (StartPoint.HasValue == false)
        {
            StartPoint = clickedPoint;
            // Optional: snap X to 0 immediately if SelectEdgesOnly
        }
        else if (EndPoint.HasValue == false)
        {
            EndPoint = clickedPoint;
            var rect = GetSelectionRectangle(StartPoint.Value, EndPoint.Value);
            _currentRegion = rect;
            StartPoint = null;
            EndPoint = null;
        }
        StateHasChanged();
    }
    public void ClearSelection(bool manuallyDone)
    {
        StartPoint = null;
        EndPoint = null;
        if (IsProperRectangleEqual(_currentRegion, _lastSuggestion) && manuallyDone)
        {
            //you changed your mind about the suggestion.
            _alreadySuggested = false; //can do again if you really wanted to.
        }
        _currentRegion = new();
    }
    private string GetRegionImageBase64(Rectangle bounds)
    {
        return _cropHelper.CropImageBase64(bounds);
    }
    private void SetActiveRectangle(Rectangle rect)
    {
        _currentRegion = rect;
    }
    private void SetMode(EnumAdjustmentMode newMode, bool forceRender = true)
    {
        if (_currentMode != newMode)
        {
            _currentMode = newMode;
            if (forceRender)
            {
                StateHasChanged();
            }
        }
    }
    private void LeftArrowClicked()
    {
        var rect = _currentRegion;
        if (rect.IsEmpty)
        {
            return;
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
        var rect = _currentRegion;
        if (rect.IsEmpty)
        {
            return;
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
        var rect = _currentRegion;
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
        var rect = _currentRegion;
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
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _suppress = new(js);
            await _suppress.DisableArrowKeysAsync();
            await _suppress.DisableFunctionKeysAsync();
        }
    }
    public async ValueTask DisposeAsync()
    {
        if (_suppress is not null)
        {
            await _suppress.DisposeAsync();
        }
        // Add this line to suppress finalization
        GC.SuppressFinalize(this);
    }

}