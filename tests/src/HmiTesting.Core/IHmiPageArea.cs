using HmiTesting.Core.DTOs;

namespace HmiTesting.Core.Interfaces;

public interface IHmiPageArea
{
    public LocatorNodeId getElementByNumber(int number);
    public T getElementByName<T>(string name) where T : IInputControl;

}