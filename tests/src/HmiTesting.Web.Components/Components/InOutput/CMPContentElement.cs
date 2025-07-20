using HmiTesting.Core.DTOs;
using HmiTesting.Core.Interfaces;
using static HmiTesting.Core.Helpers.PathHandler;
using OpcPath = HmiTesting.Core.Helpers.PathHandler.OpcPath;
public class CMPContentElement : IInputControl
{
    private IOpcUaSession _session;
    private LocatorNodeId _contentElement;
    public CMPContentElement(IOpcUaSession session, LocatorNodeId contentElement)
    {
        _session = session;
        _contentElement = contentElement;
    }

    public T getComponentByName<T>(string componentName) where T : IInputControl
    {
        var component = _session.ResolveNodeLocator(_contentElement.Locator.Page, BuildPath("HorizontalLayout", componentName).ToString(), _contentElement.NodeId); ;
        return (T)Activator.CreateInstance(typeof(T), _session, component);
    }

}