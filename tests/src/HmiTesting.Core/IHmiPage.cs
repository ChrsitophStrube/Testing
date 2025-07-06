using HmiTesting.Core.enums;

namespace HmiTesting.Core.Interfaces;

public interface IHmiPage
{
    public IHmiPageArea GetAreaLayoutContentA(int number);
    public IHmiPageArea GetAreaLayoutContentB(int number);
    public IHmiPageArea GetAreaLayoutContentC(int number);
    public IHmiPageArea GetAreaLayoutListA(int number);
    public IHmiPageArea GetAreaLayoutListB(int number);
    public IHmiPageArea GetAreaLayoutListC(int number);
    public IHmiPageArea GetAreaLayoutListD(int number);
}