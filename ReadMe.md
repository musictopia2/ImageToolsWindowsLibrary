# ImageToolsWindowsLibrary

This library contains Windows-specific Blazor components for image viewing and editing.  
**Currently, this repository includes the `ImageRegionSelectorComponent` and the `ImageHighlightViewer` component.**  
More components will be added over time to expand image processing and annotation capabilities.

---

## ImageRegionSelectorComponent

A Blazor component for selecting and cropping rectangular regions of images on Windows.  
Designed primarily for use in **Blazor apps hosted within WPF** (also usable in Windows Forms or similar).

---

### Features

- Select a region of an image by clicking to define opposite corners
- Adjust selection via arrow controls with three modes (Move, Resize, Adjust Edges)
- View full image or cropped image preview
- Obtain cropped image data as base64 or scaled rectangle
- Windows-only due to dependency on `System.Drawing.Bitmap`

---

### Usage
<ImageRegionSelectorComponent
    ImagePath="C:\\Images\\example.png"
    ShowCroppedImage="false"
    InitialCropRectangle="@myInitialRectangle">
    <h3>Select a Region</h3>
</ImageRegionSelectorComponent>

@code {
    private Rectangle myInitialRectangle = new Rectangle(10, 10, 100, 50);
}
---

## ImageHighlightViewer Component

A Blazor component for highlighting a specific region of an image, intended for scenarios where a person must manually enter information from an image (such as data entry or transcription tasks). The highlight shows the current area of focus, making it easier for users to keep track of their position.

**Note:** This component requires WPF to use, as it relies on Windows-specific APIs.

### Features

- Highlights a configurable region of an image to guide manual data entry
- Designed for workflows where users move through an image (e.g., row by row)
- Flexible controls allow moving the highlight down, or by multiple rows/areas as needed
- Useful for applications where the user must occasionally skip or move the highlight by more than one step

### Usage
<ImageHighlightViewer
    ImagePath="C:\\Images\\form.png"
    HighlightRectangle="@currentHighlight"
    OnMoveHighlight="OnMoveHighlight">
    <h3>Enter Data for Highlighted Area</h3>
</ImageHighlightViewer>

@code {
    private Rectangle currentHighlight = new Rectangle(0, 0, 200, 40);

    private void OnMoveHighlight(string direction, int steps = 1)
    {
        // Example: Move highlight down by 'steps' rows
        if (direction == "down")
        {
            currentHighlight.Y += currentHighlight.Height * steps;
        }
        // Add logic for other directions as needed
    }
}
This setup allows the highlight to be moved flexibly, including moving down by more than one row if required by the workflow.