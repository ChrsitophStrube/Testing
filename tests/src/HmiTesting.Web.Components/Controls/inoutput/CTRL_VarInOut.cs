using System.Numerics;
using System.Threading.Tasks;
using HmiTesting.Core.DTOs;
using HmiTesting.Core.Interfaces;
using LibUA.Core;
using NUnit.Framework.Constraints;
using static HmiTesting.Core.Helpers.PathHandler;
public class CTRL_VarInOut<T> : BaseCTRLInWithLabel where T : INumber<T>
{

    public CMPVarIn<T> VarIn { get; private set; }
    public CMPVarOut<T> VarOut { get; private set; }
    public CTRL_VarInOut(IOpcUaSession session, LocatorNodeId control) : base(session, control, "CoT_CMP_Label", "text")
    {
        _session = session;
        _control = control;
        _contentElement = new CMPContentElement(_session, _control);
        initaializeComponents();
    }

    protected virtual void initaializeComponents()
    {
        VarIn = _contentElement.getComponentByName<CMPVarIn<T>>("CoT_CMP_VarIn");
        VarOut = _contentElement.getComponentByName<CMPVarOut<T>>("CoT_CMP_VarOut");
    }

    public LocalizedText TextProperty { get => GetProperty<LocalizedText>("text"); set => SetProperty("text", value); }
    public async Task WaitForTextProperty(LocalizedText text) => await WaitForProperty<LocalizedText>("text", text);
    public T VarInValueProperty { get => GetProperty<T>("varInValue"); set => SetProperty("varInValue", value); }
    public async Task WaitForVarInValueProperty(T varInValue) => await WaitForProperty<T>("varInValue", varInValue);
    public T VarOutValueProperty { get => GetProperty<T>("varOutValue"); set => SetProperty("varOutValue", value); }
    public async Task WaitForVarOutValueProperty(T varOutValue) => await WaitForProperty<T>("varOutValue", varOutValue);
    public int DecimalPlacesProperty { get => GetProperty<int>("decimalPlaces"); set => SetProperty("decimalPlaces", value); }
    public async Task WaitForDecimalPlacesProperty(int decimalPlaces) => await WaitForProperty<int>("decimalPlaces", decimalPlaces);
    public bool ErrorStateProperty { get => GetProperty<bool>("errostate"); set => SetProperty("errostate", value); }
    public async Task WaitForErrorStateProperty(bool errorState) => await WaitForProperty<bool>("errostate", errorState);
    public LocalizedText UnitProperty { get => GetProperty<LocalizedText>("unit"); set => SetProperty("unit", value); }
    public async Task WaitForUnitProperty(LocalizedText unit) => await WaitForProperty<LocalizedText>("unit", unit);
    public T MinimumProperty { get => GetProperty<T>("minimum"); set => SetProperty("minimum", value); }
    public async Task WaitForMinimumProperty(T minimum) => await WaitForProperty<T>("minimum", minimum);
    public T MaximumProperty { get => GetProperty<T>("maximum"); set => SetProperty("maximum", value); }
    public async Task WaitForMaximumProperty(T maximum) => await WaitForProperty<T>("maximum", maximum);



    public async Task<bool> IsEnabled()
    {
        bool varInEnabledState = await VarIn.IsEnabled();
        //TODO: //bool labelEnableState = await label.IsEnabled(); //Not Implemented On Optix site
        return varInEnabledState; //&& labelEnableState;
    }

}