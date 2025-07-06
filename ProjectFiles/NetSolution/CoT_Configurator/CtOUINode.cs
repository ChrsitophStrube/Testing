using System;
using System.Collections.Generic;

class CtOUINode
{
    private Dictionary<string, string> _fixedProperties;
    private Dictionary<string, string> _phiLinks;
    private Dictionary<string, string> _screenLinks;
    private string _pathToNode;
    private string _name;
    private string _optixType;
    private string _plcPathPrefix;


    private bool isNewOpti_Type = false;

    internal Dictionary<string, string> FixedProperties { get => _fixedProperties; set => _fixedProperties = value; }
    public Dictionary<string, string> PhiLinks { get => _phiLinks; set => _phiLinks = value; }
    public string Name { get => _name; set => _name = value; }
    public string OptixType { get => _optixType; set => _optixType = value; }
    public string PathToNode { get => _pathToNode; set => _pathToNode = value; }
    public bool IsNewOptixType { get => isNewOpti_Type; set => isNewOpti_Type = value; }
    public string PlcPathPrefix { get => _plcPathPrefix; set => _plcPathPrefix = value; }
    public Dictionary<string, string> ScreenLinks { get => _screenLinks; set => _screenLinks = value; }

    public CtOUINode()
    {
        _fixedProperties = new();
        _phiLinks = new();
        _screenLinks = new();
    }
    public CtOUINode(string name)
    {
        _fixedProperties = new();
        _phiLinks = new();
        _screenLinks = new();

        this._name = name;
    }
}
