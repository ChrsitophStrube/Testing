using System.Threading.Tasks;
using HmiTesting.Core.DTOs;
using HmiTesting.Core.Interfaces;
using LibUA.Core;
using static HmiTesting.Core.Helpers.PathHandler;
public class BaseCTRLOutWithLabel : CMPOutput
{
    protected IOpcUaSession _session;
    protected LocatorNodeId _control;

    protected CMPContentElement _contentElement;

    private string _labelTextPropertyName;


    public CMPLabel label { get; private set; }
    public BaseCTRLOutWithLabel(IOpcUaSession session, LocatorNodeId control,string labelName,string labelTextPropertyName) : base(session, control)
    {
        _session = session;
        _control = control;
        _labelTextPropertyName = labelTextPropertyName;

        _contentElement = new CMPContentElement(_session, _control);
        initaializeComponents(labelName);
    }

    protected virtual void initaializeComponents(string labelName)
    {
        label = _contentElement.getComponentByName<CMPLabel>(labelName);
    }
    public LocalizedText LabelTextProperty { get => GetProperty<LocalizedText>(_labelTextPropertyName); set => SetProperty(_labelTextPropertyName, value); }
    public async Task WaitForLabelTextProperty(LocalizedText labelText) => await WaitForProperty<LocalizedText>(_labelTextPropertyName, labelText);

    public string IconProperty { get => Path.GetFileName(GetProperty<string>("icon")); set => SetProperty("icon", iconbasePath + value); }
    public async Task WaitForIconProperty(string icon) => await WaitForProperty<string>("icon", iconbasePath + icon);

}