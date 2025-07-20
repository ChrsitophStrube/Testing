using System.Threading.Tasks;
using HmiTesting.Core.DTOs;
using HmiTesting.Core.Interfaces;
using LibUA.Core;
using static HmiTesting.Core.Helpers.PathHandler;
public class CMPLabel : CMPOutput
{
    private IOpcUaSession _session;
    public LocatorNodeId _label { get; }
    public CMPLabel(IOpcUaSession session, LocatorNodeId label) : base(session, label)
    {
        _session = session;
        _label = label;
    }

    public string IconProperty { get => Path.GetFileName(GetProperty<string>("icon")); set => SetProperty("icon", iconbasePath + value); }
    public async Task WaitForIconProperty(string icon) => await WaitForProperty<string>("icon", Path.GetFileName(icon));

    public LocalizedText TextProperty { get => GetProperty<LocalizedText>("text"); set => SetProperty("text", value); }
    public async Task WaitForTextProperty(LocalizedText text) => await WaitForProperty<LocalizedText>("text", text);

    public async Task<string> GetText()
    {
        var labelTextElement = _session.ResolveNodeLocator(_label.Locator.Page, BuildPath("HorizontalLayout", "Text").ToString(), _label.NodeId);
        return await labelTextElement.Locator.Locator("span").InnerTextAsync();
    }

    public async Task WaitForText(string test)
    {
        var labelTextElement = _session.ResolveNodeLocator(_label.Locator.Page, BuildPath("HorizontalLayout", "Text").ToString(), _label.NodeId);
        await labelTextElement.Locator.Locator("span").InnerTextAsync();
    }


}