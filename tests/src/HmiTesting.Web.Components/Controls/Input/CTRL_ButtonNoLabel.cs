using System.Threading.Tasks;
using HmiTesting.Core.DTOs;
using HmiTesting.Core.Interfaces;
using LibUA.Core;
using static HmiTesting.Core.Helpers.PathHandler;
public class CTRL_ButtonNoLabel : CMPInput
{
    private IOpcUaSession _session;
    private LocatorNodeId _control;
    public CMPButtonAction button { get; private set; }
    public CTRL_ButtonNoLabel(IOpcUaSession session, LocatorNodeId control) : base(session, control)
    {
        _session = session;
        _control = control;
        initaializeComponents();
    }

    private void initaializeComponents()
    {
        LocatorNodeId ButtonLocatorNodeId = _session.GetNodeLocator(_control.Locator.Page, "CoT_CMP_ButtonAction", _control.NodeId);
        button = new CMPButtonAction(_session, ButtonLocatorNodeId);
    }

    public LocalizedText textProperty { get => GetProperty<LocalizedText>("text"); set => SetProperty("text", value); }
    public async Task WaitForTextProperty(LocalizedText text) => await WaitForProperty<LocalizedText>("text", text);
    public string IconProperty { get => Path.GetFileName(GetProperty<string>("icon")); set => SetProperty("icon", iconbasePath + value); }
    public async Task WaitForIconProperty(string icon) => await WaitForProperty<string>("icon", iconbasePath + icon);
        public async Task<bool> IsEnabled()
    {
        bool buttonEnableStare = await button.IsEnabled();
        //TODO: //bool labelEnableState = await label.IsEnabled(); //Not Implemented On Optix site
        return buttonEnableStare; //&& labelEnableState;
    }

    public async Task WaitForEnabled(bool enabled)
    {
        await button.WaitForEnabled(enabled);
        //TODO: //bool labelEnableState = await label.WaitForEnabled(enabled); //Not Implemented On Optix site
    }
}