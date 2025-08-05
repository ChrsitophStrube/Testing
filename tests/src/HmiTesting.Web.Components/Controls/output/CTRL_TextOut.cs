using System.Numerics;
using HmiTesting.Core.DTOs;
using LibUA.Core;

public class CTRL_TextOut : BaseCTRLOutWithLabel
{

    public CMPTextOut textOut { get; private set; }
    public CTRL_TextOut(IOpcUaSession session, LocatorNodeId control) : base(session, control, "CoT_CMP_Label", "labelText")
    {
        _session = session;
        _control = control;
        _contentElement = new CMPContentElement(_session, _control);
        initaializeComponents();
    }

    protected virtual void initaializeComponents()
    {
        textOut = _contentElement.getComponentByName<CMPTextOut>("CoT_CMP_TextOutCRTL");
    }

    public LocalizedText TextOutProperty { get => GetProperty<LocalizedText>("textOutText"); set => SetProperty("textOutText", value); }
    public async Task WaitForTextOutProperty(LocalizedText textOutText) => await WaitForProperty<LocalizedText>("textOutText", textOutText);
    public bool ErrorStateProperty { get => GetProperty<bool>("errorState"); set => SetProperty("errorState", value); }
    public async Task WaitForErrorStateProperty(bool errorState) => await WaitForProperty<bool>("errorState", errorState);

    public string UnitProperty { get => GetProperty<string>("unit"); set => SetProperty("unit", value); }
    public async Task WaitForUnitProperty(string unit) => await WaitForProperty<string>("unit", unit);

}