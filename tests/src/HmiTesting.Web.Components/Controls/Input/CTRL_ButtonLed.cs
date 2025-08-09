using System.Threading.Tasks;
using HmiTesting.Core.DTOs;
using HmiTesting.Core.Interfaces;
using LibUA.Core;
using static HmiTesting.Core.Helpers.PathHandler;
public class CTRL_ButtonLed : BaseCTRLInWithLabel
{


    public CMPButtonLed buttonLed { get; private set; }

    public CTRL_ButtonLed(IOpcUaSession session, LocatorNodeId control) : base(session, control, "Label", "labelText")
    {
        _session = session;
        _control = control;
        _contentElement = new CMPContentElement(_session, _control);
        initaializeComponents();
    }

    private void initaializeComponents()
    {
        buttonLed = _contentElement.getComponentByName<CMPButtonLed>("ButtonLed");

    }
    public LocalizedText ButtonTextProperty { get => GetProperty<LocalizedText>("buttonText"); set => SetProperty("buttonText", value); }
    public async Task WaitForButtonTextProperty(LocalizedText buttonText) => await WaitForProperty<LocalizedText>("buttonText", buttonText);
    public int LedStateProperty { get => GetProperty<int>("ledState"); set => SetProperty("ledState", value); }
    public async Task WaitForLedStateProperty(int LedState) => await WaitForProperty<int>("ledState", LedState);

    public async Task<bool> IsEnabled()
    {
        bool buttonEnableStare = await buttonLed.IsEnabled();
        //TODO: //bool labelEnableState = await label.IsEnabled(); //Not Implemented On Optix site
        return buttonEnableStare; //&& labelEnableState;
    }
    public async Task WaitForEnabled(bool enabled)
    {
        await buttonLed.WaitForEnabled(enabled);
        //TODO: //bool labelEnableState = await label.WaitForEnabled(enabled); //Not Implemented On Optix site
    }
}