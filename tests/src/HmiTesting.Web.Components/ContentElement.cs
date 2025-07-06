using HmiTesting.Core.DTOs;
using HmiTesting.Core.Interfaces;
using static HmiTesting.Core.Helpers.PathHandler;
using OpcPath = HmiTesting.Core.Helpers.PathHandler.OpcPath;
public class ContentElement : IInputControl
{
    private IOpcUaSession _session;
    private LocatorNodeId _contentElement;
    public ContentElement(IOpcUaSession session, LocatorNodeId contentElement)
    {
        _session = session;
        _contentElement = contentElement;
    }

    public T getComponentByName<T>(string componentName) where T : IInputControl
    {
        var component = _session.ResolveNodeLocator(_contentElement.Locator.Page, BuildPath("HorizontalLayout", componentName).ToString(), _contentElement.NodeId); ;
        return (T)Activator.CreateInstance(typeof(T), _session, component);
    }

    public string GetUserRole()
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