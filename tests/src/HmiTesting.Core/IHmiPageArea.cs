using HmiTesting.Core.DTOs;

namespace HmiTesting.Core.Interfaces;

public interface IHmiPageArea
{
    List<LocatorNodeId>? getAllElements();
    public T getElementByName<T>(string name) where T : IInputControl;

}