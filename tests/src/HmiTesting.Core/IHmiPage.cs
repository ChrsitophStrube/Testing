using HmiTesting.Core.enums;
using Microsoft.Playwright;

namespace HmiTesting.Core.Interfaces;

public interface IHmiPage
{
    IPage Page { get; }
    public IHmiPageArea GetAreaLayoutContentA(int number);
    public IHmiPageArea GetAreaLayoutContentB(int number);
    public IHmiPageArea GetAreaLayoutContentC(int number);
    public IHmiPageArea GetAreaLayoutListA(int number);
    public IHmiPageArea GetAreaLayoutListB(int number);
    public IHmiPageArea GetAreaLayoutListC(int number);
    public IHmiPageArea GetAreaLayoutListD(int number);
}