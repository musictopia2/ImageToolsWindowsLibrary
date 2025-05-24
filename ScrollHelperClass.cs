namespace ImageToolsWindowsLibrary;
internal class ScrollHelperClass(IJSRuntime js) : BaseLibraryJavascriptClass(js)
{
    protected override string JavascriptFileName => "scrollhelpers.js";
    public async Task ScrollImageContainer(ElementReference? element, int pixels)
    {
        if (element is null)
        {
            return;
        }
        await ModuleTask.InvokeVoidFromClassAsync("scrollImageContainer", element, pixels);
    }
}