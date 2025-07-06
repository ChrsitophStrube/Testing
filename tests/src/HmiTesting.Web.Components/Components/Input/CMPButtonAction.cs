using System.Threading.Tasks;
using HmiTesting.Core.DTOs;
using HmiTesting.Core.Interfaces;
using LibUA.Core;
using Microsoft.Playwright;
using static HmiTesting.Core.Helpers.PathHandler;
public class CMPButtonAction : CMPButtonBlank
{
    public CMPButtonAction(IOpcUaSession session, LocatorNodeId button) : base(session, button)
    {
    }

    public async Task<string> GetText()
    {
        var labelTextElement = _session.ResolveNodeLocator(_button.Locator.Page, BuildPath("HorizontalLayout1", "buttonText").ToString(), _button.NodeId);
        return await labelTextElement.Locator.Locator("span").InnerTextAsync();
    }
}