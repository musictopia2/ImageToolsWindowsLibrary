namespace ImageToolsWindowsLibrary;
public partial class MultiRegionSelectorComponent(IJSRuntime js) : IAsyncDisposable
{
    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public string ImagePath { get; set; } = "";
    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public EnumRegionLayoutMode LayoutMode { get; set; }
    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public RenderFragment? ChildContent { get; set; }
    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public EventCallback<BasicList<Rectangle>> OnCompleted { get; set; }
    [Parameter]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool SelectEdgesOnly { get; set; }
    private KeySuppressorClass? _suppress;
    private bool _showCropped = false;
    private readonly BasicList<Rectangle> _regions = [];
    private bool _completed;
    private Rectangle _currentRegion = new();
    private readonly ImageCropHelper _cropHelper = new();
    private AppKeyboardListener? _keys;
    //private bool _choseLast;
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
        if (_showCropped)
        {
            if (key == EnumKey.F5)
            {
                _showCropped = false;
                StateHasChanged();
            }
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
                StartNewRegion();
                break;
            case EnumKey.F5:
                StartNewRegion(false);
                _showCropped = true;
                StateHasChanged();
                break;
            case EnumKey.F6:
                Complete();
                break;
            case EnumKey.F7:
                RemoveLastRegion();
                break;
            case EnumKey.F8:
                ChooseLastRegion();
                break;
            case EnumKey.F9:
                AssistChooseNextRegion();
                break;
        }
    }
    private bool CanChooseLastRegion()
    {
        if (SelectEdgesOnly == false)
        {
            return false;
        }
        if (_currentRegion.IsEmpty == false)
        {
            return true; //because this can be used.
        }
        if (_regions.Count == 0)
        {
            return false;
        }
        if (StartPoint is not null && EndPoint is null)
        {
            return false;
        }
        if (_currentRegion.IsEmpty == true)
        {
            return false;
        }
        return true;
    }
    private void ChooseLastRegion()
    {
        if (CanChooseLastRegion() == false)
        {
            return;
        }
        if (_currentRegion.IsEmpty == false)
        {
            StartNewRegion(false); //clear out first.
        }
        var (width, height) = _cropHelper.GetNaturalSize();
        var recent = _regions.Last();
        StartPoint = new(recent.X, recent.Bottom);
        EndPoint = new(width, height);
        var rect = GetSelectionRectangle(StartPoint.Value, EndPoint.Value);

        _currentRegion = rect;


        StartPoint = null;
        EndPoint = null;
        StateHasChanged(); //still double check i think.
    }
    private void AssistChooseNextRegion(bool forceRender = true)
    {
        if (SelectEdgesOnly == false)
        {
            return;
        }
        if (StartPoint is not null || EndPoint is not null)
        {
            return;
        }
        if (_currentRegion.IsEmpty == false)
        {
            //this implies that i want to commit.
            StartNewRegion(false); //i think
        }
        if (_regions.Count == 0)
        {
            StartPoint = new(0, 0);
            if (forceRender)
            {
                StateHasChanged();
            }
            return;
        }
        var lasts = _regions.Last();
        StartPoint = new(lasts.X, lasts.Bottom);
        if (forceRender)
        {
            StateHasChanged();
        }
    }
    private void RemoveLastRegion()
    {
        if (_regions.Count > 0 && !_completed)
        {
            _regions.RemoveLastItem();
            ClearSelection(); // Clear the current editing state
            StateHasChanged();
        }
    }
    private void StartNewRegion(bool forceRender = true)
    {
        if (_currentRegion.IsEmpty)
        {
            return; //can't because its already empty
        }
        _regions.Add(_currentRegion);
        ClearSelection();
        if (forceRender)
        {
            StateHasChanged();
        }
    }
    private void Complete()
    {
        if (_currentRegion.IsEmpty && _regions.Count == 0)
        {
            //this means only one region.

            var (width, height) = _cropHelper.GetNaturalSize();
            Rectangle bounds = new(0, 0, width, height);

            OnCompleted.InvokeAsync([bounds]); //if you have none, then its just the original (a copy)
            return; //no regions.  must have at least one region
        }
        if (StartPoint is not null && EndPoint is null)
        {
            return; //you have to finish or clear out first.
        }
        if (_currentRegion.IsEmpty)
        {
            OnCompleted.InvokeAsync(_regions);
            return;
        }
        _regions.Add(_currentRegion);
        OnCompleted.InvokeAsync(_regions);
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
    protected override async Task OnParametersSetAsync()
    {
        StartPoint = null;
        _desiredImagePath = ImagePath;
        _completed = false;
        _regions.Clear();
        EndPoint = null;
        //_choseLast = false;
        if (SelectEdgesOnly)
        {
            _currentMode = EnumAdjustmentMode.Resize;
        }
        if (!string.IsNullOrWhiteSpace(_desiredImagePath) &&
            File.Exists(_desiredImagePath) &&
            _desiredImagePath != _lastImagePath) // only reload if path changes
        {
            _lastImagePath = _desiredImagePath;
            _cropHelper.LoadImage(_desiredImagePath);
            var bytes = await File.ReadAllBytesAsync(_desiredImagePath);
            ImageData = $"data:image/png;base64,{Convert.ToBase64String(bytes)}";
        }
        if (SelectEdgesOnly)
        {
            AssistChooseNextRegion(); //do to start with.
        }
    }
    private ElementReference _imageRef;
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
            if (SelectEdgesOnly)
            {
                StartPoint = new Point(0, clickedPoint.Y);
            }
        }
        else if (EndPoint.HasValue == false)
        {
            EndPoint = clickedPoint;

            if (SelectEdgesOnly)
            {
                var (width, _) = _cropHelper.GetNaturalSize();
                EndPoint = new Point(width, clickedPoint.Y); // force X = 0 again

            }


            var rect = GetSelectionRectangle(StartPoint.Value, EndPoint.Value);

            _currentRegion = rect;


            StartPoint = null;
            EndPoint = null;

            StateHasChanged();
        }

        StateHasChanged();
    }
    public void ClearSelection()
    {
        StartPoint = null;
        EndPoint = null;
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
        if (SelectEdgesOnly)
        {
            newMode = EnumAdjustmentMode.Resize;
            if (forceRender)
            {
                StateHasChanged();
            }
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
    private void LeftArrowClicked()
    {
        if (SelectEdgesOnly)
        {
            return;
        }
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
        if (SelectEdgesOnly)
        {
            return;
        }
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
        if (SelectEdgesOnly)
        {
            var (_, height) = _cropHelper.GetNaturalSize();
            if (height == rect.Bottom)
            {
                return; //not anymore because you can't go down anymore
            }

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