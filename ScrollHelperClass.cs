namespace ImageToolsWindowsLibrary;
internal class ScrollHelperClass(IJSRuntime js) : BaseLibraryJavascriptClass(js)
{
    protected override string JavascriptFileName => "scrollhelpers.js";
    public async Task ScrollDownOnePage(ElementReference element, int buffer = 100)
    {
        await ModuleTask.InvokeVoidFromClassAsync("scrollDownOnePage", element, buffer);
    }

    public async Task ScrollUpOnePage(ElementReference element, int buffer = 100)
    {
        await ModuleTask.InvokeVoidFromClassAsync("scrollUpOnePage", element, buffer);
    }
}