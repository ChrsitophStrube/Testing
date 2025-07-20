using System.Drawing;
using HmiTesting.Core.DTOs;
using HmiTesting.Core.Interfaces;
using LibUA.Core;
using Microsoft.Playwright;

public abstract class CMPOutput : CMP
{
    protected CMPOutput(IOpcUaSession session, LocatorNodeId component): base(session, component)             
    {}

    public bool VisisbleProperty { get => GetProperty<bool>("visibility"); set => SetProperty("visibility", value); }
    public Task VisisblePropertyAsync(bool visibility) => WaitForProperty<bool>("visibility", visibility);

}