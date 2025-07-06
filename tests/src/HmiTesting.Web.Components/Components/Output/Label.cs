using System.Threading.Tasks;
using HmiTesting.Core.DTOs;
using HmiTesting.Core.Interfaces;
using static HmiTesting.Core.Helpers.PathHandler;
public class Label : IInputControl
{
    private IOpcUaSession _session;
    private LocatorNodeId _label;
    public Label(IOpcUaSession session, LocatorNodeId label)
    {
        _session = session;
        _label = label;
    }

    public async Task<string> GetText()
    {

        var labelTextElement = _session.ResolveNodeLocator(_label.Locator.Page, BuildPath("HorizontalLayout", "Text").ToString(), _label.NodeId);
        return await labelTextElement.Locator.Locator("span").InnerTextAsync();
    }

    public string GetUserRole()
    {
        throw new NotImplementedException();
    }

    public Task<bool> IsEnabled()
    {
        throw new NotImplementedException();
    }


    public Task<bool> IsVisibleAsync(int timeoutMs)
    {
        throw new NotImplementedException();
    }
}