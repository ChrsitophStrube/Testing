using System.Numerics;
using HmiTesting.Core.DTOs;
using LibUA.Core;

public class CTRL_VarOut<T> : BaseCTRLOutWithLabel where T : INumber<T>
{

    public CMPVarOut<T> varOut { get; private set; }
    public CTRL_VarOut(IOpcUaSession session, LocatorNodeId control) : base(session, control, "CoT_CMP_Label", "text")
    {
        _session = session;
        _control = control;
        _contentElement = new CMPContentElement(_session, _control);
        initaializeComponents();
    }

    protected virtual void initaializeComponents()
    {
        varOut = _contentElement.getComponentByName<CMPVarOut<T>>("CoT_CMP_VarOut");
    }

    public T ValueProperty { get => GetProperty<T>("value"); set => SetProperty("value", value); }
    public async Task WaitForValueProperty(T value) => await WaitForProperty<T>("value", value);
    public bool ErrorStateProperty { get => GetProperty<bool>("errorState"); set => SetProperty("errorState", value); }
    public async Task WaitForErrorStateProperty(bool errorState) => await WaitForProperty<bool>("errorState", errorState);

    public int DecimalPlacesProperty { get => GetProperty<int>("decimalPlaces"); set => SetProperty("decimalPlaces", value); }
    public async Task WaitForDecimalPlacesProperty(int decimalPlaces) => await WaitForProperty<int>("decimalPlaces", decimalPlaces);
    public LocalizedText UnitProperty { get => GetProperty<LocalizedText>("unit"); set => SetProperty("unit", value); }
    public async Task WaitForUnitProperty(LocalizedText unit) => await WaitForProperty<LocalizedText>("unit", unit);

}