using System.Threading.Tasks;
using HmiTesting.Core.DTOs;
using HmiTesting.Core.Interfaces;
using LibUA.Core;
using static HmiTesting.Core.Helpers.PathHandler;
public class CTRL_Switch : BaseCTRLInWithLabel
{

    public CMPSwitch @switch { get; private set; }
    public CTRL_Switch(IOpcUaSession session, LocatorNodeId control) : base(session, control, "CoT_Label_SwitchCTRL", "text")
    {
        _session = session;
        _control = control;
        _contentElement = new CMPContentElement(_session, _control);
        initaializeComponents();
    }

    protected virtual void initaializeComponents()
    {
        @switch = _contentElement.getComponentByName<CMPSwitch>("CoT_CMP_SwitchCTRL");
    }
    public bool CommandProperty { get => GetProperty<bool>("command"); set => SetProperty("command", value); }
    public async Task WaitForCommandProperty(bool command) => await WaitForProperty<bool>("command", command);

    public async Task<bool> IsEnabled()
    {
        bool buttonEnableStare = await @switch.IsEnabled();
        //TODO: //bool labelEnableState = await label.IsEnabled(); //Not Implemented On Optix site
        return buttonEnableStare; //&& labelEnableState;
    }

        public async Task WaitForEnabled(bool enabled)
    {
        await @switch.WaitForEnabled(enabled);
        //TODO: //bool labelEnableState = await label.WaitForEnabled(enabled); //Not Implemented On Optix site
    }    
}