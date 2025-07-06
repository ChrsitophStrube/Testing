using System.Formats.Asn1;
using HmiTesting.Core.DTOs;
using HmiTesting.Core.enums;
using HmiTesting.Core.Interfaces;
using Microsoft.Playwright;

namespace HmiTesting.Web.Pages;

public class HmiPageArea :
 IHmiPageArea
{
    private IOpcUaSession _session;
    public LocatorNodeId _area { get; }

    public HmiPageArea(IOpcUaSession session, LocatorNodeId area)
    {
        _session = session;
        _area = area;
    }
    public T getElementByName<T>(string name) where T : IInputControl
    {
        var element = _session.ResolveNodeLocator(_area.Locator.Page, name, _area.NodeId);
        return (T)Activator.CreateInstance(typeof(T), _session, element);
    }

    public LocatorNodeId getElementByNumber(int number)
    {
        throw new NotImplementedException();
    }
}
