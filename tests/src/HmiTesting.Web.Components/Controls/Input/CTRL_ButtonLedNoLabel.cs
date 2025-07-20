using System.Threading.Tasks;
using HmiTesting.Core.DTOs;
using HmiTesting.Core.Interfaces;
using LibUA.Core;
using static HmiTesting.Core.Helpers.PathHandler;
public class CTRL_ButtonLedNoLabel : CMPInput
{
    private IOpcUaSession _session;
    private LocatorNodeId _control;
    public CMPButtonLed button { get; private set; }
    public CTRL_ButtonLedNoLabel(IOpcUaSession session, LocatorNodeId control) : base(session, control)
    {
        _session = session;
        _control = control;
        initaializeComponents();
    }

    private void initaializeComponents()
    {
        LocatorNodeId ButtonLocatorNodeId = _session.ResolveNodeLocator(_control.Locator.Page, "CoT_CMP_ButtonLed", _control.NodeId);
        button = new CMPButtonLed(_session, ButtonLocatorNodeId);
    }

    public LocalizedText textProperty { get => GetProperty<LocalizedText>("text"); set => SetProperty("text", value); }
    public async Task WaitForTextProperty(LocalizedText text) => await WaitForProperty<LocalizedText>("text", text);
    public string IconProperty { get => Path.GetFileName(GetProperty<string>("icon")); set => SetProperty("icon", iconbasePath + value); }
    public async Task WaitForIconProperty(string icon) => await WaitForProperty<string>("icon", iconbasePath + icon);

    public int LedStateProperty { get => GetProperty<Int16>("ledState"); set => SetProperty("ledState", value); }
    public async Task WaitForLedStateProperty(Int16 LedState) => await WaitForProperty<Int16>("ledState", LedState);
}