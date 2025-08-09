using System.Numerics;
using System.Threading.Tasks;
using HmiTesting.Core.DTOs;
using HmiTesting.Core.Interfaces;
using LibUA.Core;
using NUnit.Framework.Constraints;
using static HmiTesting.Core.Helpers.PathHandler;
public class CTRL_VarIn<T> : BaseCTRLInWithLabel where T : INumber<T>
{

    public CMPVarIn<T> VarIn { get; private set; }
    public CTRL_VarIn(IOpcUaSession session, LocatorNodeId control) : base(session, control, "CoT_Label_VarInCRTL", "text")
    {
        _session = session;
        _control = control;
        _contentElement = new CMPContentElement(_session, _control);
        initaializeComponents();
    }

    protected virtual void initaializeComponents()
    {
        VarIn = _contentElement.getComponentByName<CMPVarIn<T>>("CoT_CMP_VarInCRTL");
    }

    public LocalizedText TextProperty { get => GetProperty<LocalizedText>("text"); set => SetProperty("text", value); }
    public async Task WaitForTextProperty(LocalizedText text) => await WaitForProperty<LocalizedText>("text", text);

    public T ValueProperty { get => GetProperty<T>("value"); set => SetProperty("value", value); }
    public async Task WaitForValueProperty(T value) => await WaitForProperty<T>("value", value);

    public int DecimalPlacesProperty { get => GetProperty<int>("decimalPlaces"); set => SetProperty("decimalPlaces", value); }
    public async Task WaitForDecimalPlacesProperty(int decimalPlaces) => await WaitForProperty<int>("decimalPlaces", decimalPlaces);

    public bool ErrorStateProperty { get => GetProperty<bool>("errorState"); set => SetProperty("errorState", value); }
    public async Task WaitForErrorStateProperty(bool errorState) => await WaitForProperty<bool>("errorState", errorState);

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
    
        public async Task WaitForEnabled(bool enabled)
    {
        await VarIn.WaitForEnabled(enabled);
        //TODO: //bool labelEnableState = await label.WaitForEnabled(enabled); //Not Implemented On Optix site
    }  

}