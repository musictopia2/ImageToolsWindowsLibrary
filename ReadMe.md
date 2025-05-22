# ImageToolsWindowsLibrary

This library contains Windows-specific Blazor components for image viewing and editing.  
**Currently, this repository includes only the `ImageRegionSelectorComponent`.**  
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

```razor
<ImageRegionSelectorComponent
    ImagePath="C:\\Images\\example.png"
    ShowCroppedImage="false"
    InitialCropRectangle="@myInitialRectangle">
    <h3>Select a Region</h3>
</ImageRegionSelectorComponent>

@code {
    private Rectangle myInitialRectangle = new Rectangle(10, 10, 100, 50);
}