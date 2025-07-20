using System.Threading.Tasks;
using HmiTesting.Core.DTOs;
using HmiTesting.Core.Interfaces;
using LibUA.Core;
using static HmiTesting.Core.Helpers.PathHandler;
public class CTRL_TextIn : BaseCTRLInWithLabel
{

    public CMPTextIn textIn { get; private set; }
    public CTRL_TextIn(IOpcUaSession session, LocatorNodeId control) : base(session, control, "CoT_CMP_Label", "labelText")
    {
        _session = session;
        _control = control;
        _contentElement = new CMPContentElement(_session, _control);
        initaializeComponents();
    }

    protected virtual void initaializeComponents()
    {
        textIn = _contentElement.getComponentByName<CMPTextIn>("CoT_CMP_TextIn");
    }

    public LocalizedText TextInTextProperty { get => GetProperty<LocalizedText>("textInText"); set => SetProperty("textInText", value); }
    public async Task WaitForTextInTextProperty(LocalizedText textInText) => await WaitForProperty<LocalizedText>("textInText", textInText);

    public bool ErrorStateProperty { get => GetProperty<bool>("errorState"); set => SetProperty("errorState", value); }
    public async Task WaitForErrorStateProperty(bool errorState) => await WaitForProperty<bool>("errorState", errorState);

    public string UnitProperty { get => GetProperty<string>("unit"); set => SetProperty("unit", value); }
    public async Task WaitForUnitProperty(string unit) => await WaitForProperty<string>("unit", unit);


    public async Task<bool> IsEnabled()
    {
        bool buttonEnableStare = await textIn.IsEnabled();
        //TODO: //bool labelEnableState = await label.IsEnabled(); //Not Implemented On Optix site
        return buttonEnableStare; //&& labelEnableState;
    }

}