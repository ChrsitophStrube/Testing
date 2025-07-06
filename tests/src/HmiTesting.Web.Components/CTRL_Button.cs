using System.Threading.Tasks;
using HmiTesting.Core.DTOs;
using HmiTesting.Core.Interfaces;
using static HmiTesting.Core.Helpers.PathHandler;
public class CTRL_Button
{
    private IOpcUaSession _session;
    private LocatorNodeId _control;

    private ContentElement _contentElement;

    public CMPButtonAction button;
    public Label label;
    public CTRL_Button(IOpcUaSession session, LocatorNodeId control)
    {
        _session = session;
        _control = control;
        _contentElement = new ContentElement(_session, _control);
        initaializeComponents();
    }

    private async Task initaializeComponents()
    {
        button = _contentElement.getComponentByName<CMPButtonAction>("CoT_ButtonAction");
        label = _contentElement.getComponentByName<Label>("CoT_Label");
        await button.GetText();
    }

    string GetUserRole()
    {
        return "";
    }

    public Task<bool> IsEnabled()
    {
        throw new NotImplementedException();
    }

    public async Task<bool> IsVisibleAsync(int delay)
    {
        throw new NotImplementedException();
    }
}