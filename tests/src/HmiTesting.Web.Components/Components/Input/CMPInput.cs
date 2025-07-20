using System.Drawing;
using HmiTesting.Core.DTOs;
using HmiTesting.Core.Interfaces;
using LibUA.Core;
using Microsoft.Playwright;

public abstract class CMPInput : CMP
{
    protected CMPInput(IOpcUaSession session, LocatorNodeId component): base(session, component)             
    {}

    public bool EnableProperty { get => GetProperty<bool>("enable"); set => SetProperty("enable", value); }
    public Task WaitForEnablePropertyAsync(bool enable) => WaitForProperty<bool>("enable", enable);

    public bool VisisbleProperty { get => GetProperty<bool>("visibility"); set => SetProperty("visibility", value); }
    public Task VisisblePropertyAsync(bool visibility) => WaitForProperty<bool>("visibility", visibility);

    public string UserRoleProperty { get => GetBrowsename("userRole"); }

}