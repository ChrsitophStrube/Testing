using System;
using System.Collections.Generic;

class CtOObjectNode
{
    private Dictionary<string, string> fixedProperties;
    private Dictionary<string, string> phiLinks;
    private string pathToNode;
    private string name;
    private string optixType;

    internal Dictionary<string, string> FixedProperties { get => fixedProperties; set => fixedProperties = value; }
    public Dictionary<string, string> PhiLinks { get => phiLinks; set => phiLinks = value; }
    public string Name { get => name; set => name = value; }
    public string OptixType { get => optixType; set => optixType = value; }

    public CtOObjectNode()
    {
        fixedProperties = new();
        phiLinks = new();

    }
    public CtOObjectNode(string name)
    {
        fixedProperties = new();
        phiLinks = new();
        this.name = name;
    }
}
