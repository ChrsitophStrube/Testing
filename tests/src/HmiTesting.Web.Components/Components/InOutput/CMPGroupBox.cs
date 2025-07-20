using HmiTesting.Core.DTOs;
using HmiTesting.Core.Interfaces;

namespace HmiTesting.Web.Components;


public class GroupBox : IInputControl
{
    private IOpcUaSession _session;
    private LocatorNodeId _groupBox;
    public GroupBox(IOpcUaSession session, LocatorNodeId groupBox)
    {
        _session = session;
        _groupBox = _groupBox;
    }


    public IInputControl ResolveGroupbox<T>(string ControlName) where T : IInputControl
    {
        var element = _session.ResolveNodeLocator(_groupBox.Locator.Page, ControlName, _groupBox.NodeId);
        return (T)Activator.CreateInstance(typeof(T), element);
    }
    public Task<bool> IsEnabled()
    {
        throw new NotImplementedException();
    }

    public async Task<bool> IsVisibleAsync(int delay)
    {
        throw new NotImplementedException();
    }

    public string GetUserRole()
    {
        throw new NotImplementedException();
    }
}
