using System.Drawing;
using HmiTesting.Core.DTOs;
using HmiTesting.Core.Interfaces;
using LibUA.Core;
using Microsoft.Playwright;

public abstract class CMP : IInputControl
{
    protected readonly IOpcUaSession _session;
    public LocatorNodeId _component { get; }

    protected Color fatal100 = ColorTranslator.FromHtml("#D20000");
    protected Color fatal60 = ColorTranslator.FromHtml("#e26464");
    protected Color white = ColorTranslator.FromHtml("#FFFFFF");
    protected Color black44 = ColorTranslator.FromHtml("#8f8f8f");
    protected Color black100 = ColorTranslator.FromHtml("#000000");
    protected Color dark100 = ColorTranslator.FromHtml("#29333F");

    protected readonly string iconbasePath = @"%PROJECTDIR%\Icons\";

    protected CMP(IOpcUaSession session, LocatorNodeId component)
    {
        _session = session;
        _component = component;
    }

    protected T GetProperty<T>(string propertyName)
    {
        NodeId propertyNodeId = _session.GetNodeIdFromPath(propertyName, _component.NodeId);
        return _session.GetValue<T>(propertyNodeId);
    }

    protected string GetBrowsename(string propertyName)
    {
        NodeId propertyNodeId = _session.GetNodeIdFromPath(propertyName, _component.NodeId);
        NodeId browseNameId = _session.GetValue<NodeId>(propertyNodeId);
        return _session.GetBrowsename(browseNameId);
    }

    protected async Task WaitForProperty<T>(string propertyName, T value)
    {
        NodeId propertyNodeId = _session.GetNodeIdFromPath(propertyName, _component.NodeId);
        await _session.WaitForValueAsync<T>(propertyNodeId, value);
    }

    protected void SetProperty<T>(string propertyName, T value)
    {
        NodeId propertyNodeId = _session.GetNodeIdFromPath(propertyName, _component.NodeId);
        _session.SetValue<T>(propertyNodeId, value);
    }


    public async Task WaitForVisibleAsync(bool visebilety)
    {
        WaitForSelectorState state = visebilety ? WaitForSelectorState.Visible : WaitForSelectorState.Detached;
        await _component.Locator.WaitForAsync(new()
        {
            State = state,
            Timeout = 500
        });
    }

    public async Task<bool> GetVisibleAsync()
    {
        return await _component.Locator.IsVisibleAsync(); ;
    }
}