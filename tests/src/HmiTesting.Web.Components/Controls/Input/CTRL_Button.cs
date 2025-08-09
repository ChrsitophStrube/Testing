using System.Threading.Tasks;
using HmiTesting.Core.DTOs;
using HmiTesting.Core.Interfaces;
using LibUA.Core;
using static HmiTesting.Core.Helpers.PathHandler;
public class CTRL_Button : BaseCTRLInWithLabel
{

    public CMPButtonAction button { get; private set; }
    public CTRL_Button(IOpcUaSession session, LocatorNodeId control) : base(session, control, "CoT_Label", "labelText")
    {
        _session = session;
        _control = control;
        _contentElement = new CMPContentElement(_session, _control);
        initaializeComponents();
    }

    protected virtual void initaializeComponents()
    {
        button = _contentElement.getComponentByName<CMPButtonAction>("CoT_ButtonAction");
    }
    public LocalizedText ButtonTextProperty { get => GetProperty<LocalizedText>("buttonText"); set => SetProperty("buttonText", value); }
    public async Task WaitForButtonTextProperty(LocalizedText buttonText) => await WaitForProperty<LocalizedText>("buttonText", buttonText);

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