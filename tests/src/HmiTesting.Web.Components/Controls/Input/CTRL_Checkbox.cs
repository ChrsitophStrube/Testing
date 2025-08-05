using HmiTesting.Core.DTOs;
using LibUA.Core;

public class CTRL_CheckBox : BaseCTRLInWithLabel
{

    public CMPCheckbox checkBoxLeft { get; private set; }
    public CMPCheckbox checkBoxRight { get; private set; }


    public CTRL_CheckBox(IOpcUaSession session, LocatorNodeId control) : base(session, control, "CoT_CMP_Label", "text")
    {
        _session = session;
        _control = control;
        _contentElement = new CMPContentElement(_session, _control);
        initaializeComponents();
    }

    protected virtual void initaializeComponents()
    {
        checkBoxLeft = _contentElement.getComponentByName<CMPCheckbox>("CoT_CMP_CheckBoxLeft");
        checkBoxRight = _contentElement.getComponentByName<CMPCheckbox>("CoT_CMP_CheckBoxRight");
    }
    public Int32 CheckBoxPositionProperty { get => GetProperty<Int32>("checkBoxPosition"); set => SetProperty("checkBoxPosition", value); }
    public async Task WaitForCheckBoxPositionProperty(Int32 checkBoxPosition) => await WaitForProperty<Int32>("checkBoxPosition", checkBoxPosition);
    public bool CheckedProperty { get => GetProperty<bool>("checked"); set => SetProperty("checked", value); }
    public async Task WaitForCheckedProperty(bool checkedValue) => await WaitForProperty<bool>("checked", checkedValue);

    public async Task<bool> IsEnabled()
    {
        bool checkboxEnableState;
        if (CheckBoxPositionProperty == 1)
        {
            checkboxEnableState = await checkBoxRight.IsEnabled();
        }

        else
        {
            checkboxEnableState = await checkBoxLeft.IsEnabled();
        }

        //TODO: //bool labelEnableState = await label.IsEnabled(); //Not Implemented On Optix site
        return checkboxEnableState; //&& labelEnableState;
    }
}