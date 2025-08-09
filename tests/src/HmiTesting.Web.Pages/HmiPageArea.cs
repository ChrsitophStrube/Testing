using System.Formats.Asn1;
using HmiTesting.Core.DTOs;
using HmiTesting.Core.enums;
using HmiTesting.Core.Interfaces;
using LibUA.Core;
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
        var element = _session.GetNodeLocator(_area.Locator.Page, name, _area.NodeId);
        return (T)Activator.CreateInstance(typeof(T), _session, element);
    }

    public List<LocatorNodeId>? getAllElements()
    {
        List<LocatorNodeId> elements = new();

        List<NodeId> children = _session.GetChildren(_area.NodeId);
        foreach (NodeId child in children)
        {
            // No Object means Not needet
            if(_session.GetNodeClass(child)!= NodeClass.Object)
            { continue; }
            string browseName = _session.GetBrowsename(child);
            

            var element = _session.GetNodeLocator(_area.Locator.Page, browseName, _area.NodeId);
            elements.Add(element);
        }
        if (elements.Count == 0)
        {
            return null;
        }
        return elements;
    }
}
