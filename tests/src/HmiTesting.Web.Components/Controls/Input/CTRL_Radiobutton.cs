using System.Threading.Tasks;
using HmiTesting.Core.DTOs;
using HmiTesting.Core.Interfaces;
using LibUA.Core;
using static HmiTesting.Core.Helpers.PathHandler;
public class CTRL_Radiobutton : BaseCTRLInWithLabel
{

    public CMPRadiobutton radiobutton { get; private set; }
    public CTRL_Radiobutton(IOpcUaSession session, LocatorNodeId control) : base(session, control, "CoT_Label_RadioButtonCTRL", "text")
    {
        _session = session;
        _control = control;
        _contentElement = new CMPContentElement(_session, _control);
        initaializeComponents();
    }

    protected virtual void initaializeComponents()
    {
        radiobutton = _contentElement.getComponentByName<CMPRadiobutton>("CoT_CMP_RadioButtonCTRL");
    }

    public Int16 SelectedOptipnProperty { get => GetProperty<Int16>("selectedOption"); set => SetProperty("selectedOption", value); }
    public async Task WaitForSelectedOptipnProperty(Int16 selectedOption) => await WaitForProperty<Int16>("selectedOption", selectedOption);

    public Int16 OptionIDProperty { get => GetProperty<Int16>("optionID"); set => SetProperty("optionID", value); }
    public async Task WaitForOptionIDProperty(Int16 optionID) => await WaitForProperty<Int16>("optionID", optionID);

    public async Task<bool> IsEnabled()
    {
        bool buttonEnableStare = await radiobutton.IsEnabled();
        //TODO: //bool labelEnableState = await label.IsEnabled(); //Not Implemented On Optix site
        return buttonEnableStare; //&& labelEnableState;
    }
}