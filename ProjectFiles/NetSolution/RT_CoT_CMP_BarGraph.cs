#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.SQLiteStore;
using FTOptix.HMIProject;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.WebUI;
using FTOptix.Store;
using FTOptix.OPCUAServer;
using FTOptix.Retentivity;
using FTOptix.CoreBase;
using FTOptix.NetLogic;
using FTOptix.Core;
using System.Xml;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using static RT_CoT_Helper;
using System.Drawing;
using System.Data;
using System.Xml.Serialization;
#endregion

public class RT_CoT_CMP_BarGraph : BaseNetLogic
{
    private ResourceUri _svgPath;
    private XmlDocument _xml = new XmlDocument();
    private IUAVariable _minimum ,_maximum ,_lowLimit, _highLimit ,_value;
    private AdvancedSVGImage _svg;

    private float _newMinimum, _newMaximum, _newLowLimit, _newHighLimit,svgWidth  ;
    private IUAVariable  _showLimits ;
    private bool _lowLimitIsValid, _highLimitIsValid;
   
    public override void Start()
    {
        try
        {
            _svgPath = new ResourceUri(GetVariable("svgPath", LogicObject).Value);
            _xml.Load(_svgPath.Uri);
            _minimum = GetVariable("minimum", LogicObject);
            _maximum = GetVariable("maximum", LogicObject);
            _lowLimit = GetVariable("lowLimit", LogicObject);
            _highLimit = GetVariable("highLimit", LogicObject);
            _value = GetVariable("value", LogicObject);
            _showLimits = GetVariable("showLimits", LogicObject);
            var svgFunc = GetPointer("svg", LogicObject).GetPointedObj<AdvancedSVGImage>;
            _svg = svgFunc?.Invoke();

            _minimum.VariableChange += UpdateSVG;
            _maximum.VariableChange += UpdateSVG;
            _lowLimit.VariableChange += UpdateSVG;
            _highLimit.VariableChange += UpdateSVG;
            _value.VariableChange += UpdateSVG;
            _showLimits.VariableChange += UpdateSVG;

            XmlNode main = SVGNode("svg", "main");
            var tempBarWidth = float.Parse(main.Attributes["width"].Value);
            svgWidth = tempBarWidth - 4;
            UpdateSVG();

   

        }
        catch (Exception ex)
        {
            Log.Error("RT_CoT_CMP_BarGraph", $"Error in Start(): {ex.Message}");
        }
    }


    public override void Stop()
    {
        try
        {
            _minimum.VariableChange -= UpdateSVG;
            _maximum.VariableChange -= UpdateSVG;
            _lowLimit.VariableChange -= UpdateSVG;
            _highLimit.VariableChange -= UpdateSVG;
            _value.VariableChange -= UpdateSVG;
            _showLimits.VariableChange -= UpdateSVG;
        }
        catch (Exception ex)
        {
            Log.Error("RT_CoT_CMP_BarGraph", $"Error in Stop(): {ex.Message}");
        }
    }

private void ValidateLimits()
{
    try
    {   

        _newMinimum = _minimum.Value;;
        _newMaximum = _maximum.Value;
        _newLowLimit = _lowLimit.Value;
        _newHighLimit = _highLimit.Value;

        if (_newMinimum>_newMaximum)
        {
            _newMinimum=_maximum.Value;
            _newMaximum=_minimum.Value;
        }

        _lowLimitIsValid = _newMinimum < _newLowLimit && _newLowLimit < _newMaximum;
        _highLimitIsValid = _newMinimum < _newHighLimit && _newHighLimit < _newMaximum;

        if (_lowLimitIsValid && _highLimitIsValid)
        {
            if(_highLimit.Value<_lowLimit.Value)
            {
                _newLowLimit=_highLimit.Value;
                _newHighLimit=_lowLimit.Value;
            }

        }

        if(!_lowLimitIsValid)
        {
            _newLowLimit=_newHighLimit;
        }

        if(!_highLimitIsValid)
        {
            _newHighLimit=_newMaximum;
        }


    }
    catch (Exception ex)
    {
        Log.Error("RT_CoT_CMP_BarGraph", $"Error in ValidateLimits: {ex.Message}");
    }
}



    private float PixelLength(float value)
    {
        try
        {
            if (_newMaximum == _newMinimum)
                return 0;

            return (value - _newMinimum) / (_newMaximum - _newMinimum) * svgWidth;
        }
        catch (Exception ex)
        {
            Log.Error("RT_CoT_CMP_BarGraph", $"Error in PixelLength: {ex.Message}");
            return 0;
        }
    }


    private void UpdateSVG(object sender = null, VariableChangeEventArgs e = null)
    {
        try
        {
            ValidateLimits();
            ResetSections();
            SetLabels();
            _svg.SetImageContent(_xml.OuterXml);

        }
        catch (Exception ex)
        {
            Log.Error("RT_CoT_CMP_BarGraph", $"Error in UpdateSVG: {ex.Message}");
        }
    }



    private void ResetSections()
    {
        string _redActiveColor = "#ff4040", _yellowActiveColor = "#ffe100", _greenActiveColor = "#01bf91", _defaultColour = "#ffffff";


        // Calculate pixel positions for the limits and value
        float _pixelMin = PixelLength(_newMinimum);
        float _pixelMax = PixelLength(_newMaximum);
        float _pixelLow = PixelLength(_newLowLimit);
        float _pixelHigh = PixelLength(_newHighLimit);
        float _pixelValue = PixelLength(_value.Value);


        List<(float start, float end, string defaultColor, string activeColor, string sectionName)> sections = new();

        if (!_lowLimitIsValid && !_highLimitIsValid)
        {
            sections.Add((_pixelMin, _pixelMax, _greenActiveColor, _defaultColour, "sectionleft"));
            sections.Add((0, 0, _yellowActiveColor, _defaultColour, "sectionmiddle"));
            sections.Add((0, 0, _redActiveColor, _defaultColour, "sectionright"));
        }


        else if (!_lowLimitIsValid)
        {
            sections.Add((_pixelMin, _pixelHigh, _greenActiveColor, _defaultColour, "sectionleft"));
            sections.Add((0, 0, _yellowActiveColor, _defaultColour, "sectionmiddle"));
            sections.Add((_pixelHigh, _pixelMax, _redActiveColor, _defaultColour, "sectionright"));
        }

        else if (!_highLimitIsValid)
        {
            sections.Add((_pixelMin, _pixelLow, _greenActiveColor, _defaultColour, "sectionleft"));
            sections.Add((_pixelLow, _pixelMax, _yellowActiveColor, _defaultColour, "sectionmiddle"));
            sections.Add((0, 0, _redActiveColor, _defaultColour, "sectionright"));
        }
        else
        {
            sections.Add((_pixelMin, _pixelLow, _greenActiveColor, _defaultColour, "sectionleft"));
            sections.Add((_pixelLow, _pixelHigh, _yellowActiveColor, _defaultColour, "sectionmiddle"));
            sections.Add((_pixelHigh, _pixelMax, _redActiveColor, _defaultColour, "sectionright"));
        }

        foreach (var (start, end, activeColor, defaultColor, id) in sections)
        {
            string color = (_pixelValue >= start && _pixelValue < end) ? activeColor : defaultColor;
            if (start == 0 && _pixelValue <= start) color = activeColor;
            if (end == svgWidth && _pixelValue >= end) color = activeColor;
            SetSection(id, start, end, color);
        }

        SetNeedle(_pixelValue);
    }



    private void SetSection(string id, float start, float end, string color)
    {
        XmlNode rect = SVGNode("rect", id);
        float width = Math.Max(0, end - start);
        rect.Attributes["x"].Value = start.ToString();
        rect.Attributes["width"].Value = width.ToString();
        rect.Attributes["fill"].Value = color;
    }



    private void SetNeedle(float _pixelValue)
    {
        try
        {
            XmlNode needle = SVGNode("rect", "needleitem");
            XmlNode barItem = SVGNode("rect", "baritem");
            if (_pixelValue > svgWidth)
            {
                _pixelValue = svgWidth;
            }
            else if (_pixelValue < 0)
            {
                _pixelValue = 0;
            }

            needle.Attributes["x"].Value = _pixelValue.ToString();
            barItem.Attributes["width"].Value = _pixelValue.ToString();


        }
        catch (Exception ex)
        {
            Log.Error("RT_CoT_CMP_BarGraph", $"Error setting needle position: {ex.Message}");
        }
    }


    private void SetLabels()
    {
        try
        {
            SetLabel(_newMinimum, "minimumText");
            SetLabel(_newLowLimit, "lowLimitText");
            SetLabel(_newHighLimit, "highLimitText");
            SetLabel(_newMaximum, "maximumText");
        }
        catch (Exception ex)
        {
            Log.Error("RT_CoT_CMP_BarGraph", $"Error in SetLabels Margins: {ex.Message}");
        }
    }

    private void SetLabel(float value, string labelName )
    {
        try
        {
            var Label = GetPointer(labelName, LogicObject).GetPointedObj<CoT_CMP_SyntegonText>;
            float pixelPosition = PixelLength(value);
            float offset = 2f - pixelPosition / 200f * 4f;
            var labelInstance = Label?.Invoke();
            labelInstance.LeftMargin = pixelPosition - (labelInstance.Width / 2f) + offset;
            labelInstance.Text = value.ToString();
            

            if(!_lowLimitIsValid && labelName=="lowLimitText" )
            {  
               labelInstance.Visible=false; 
                return;
            }             
            else
            {
                labelInstance.Visible=true;
            }
            
            if (!_highLimitIsValid && labelName == "highLimitText")
            {
                labelInstance.Visible=false; 
                return;
             
            } 
            else
            {
                labelInstance.Visible=true;

            }

            if (labelName == "minimumText" || labelName == "maximumText")
            {
                labelInstance.Visible = true;
            }
            else 
            {

                labelInstance.Visible= _showLimits.Value;

            } 
                if(_lowLimit.Value == _highLimit.Value && labelName=="lowLimitText")
                {
                    labelInstance.Visible=false;
                }
  
        }
        catch (Exception ex)
        {
            Log.Error("RT_CoT_CMP_BarGraph", $"Error setting label '{labelName}': {ex.Message}");
        }
    }

    private XmlNode SVGNode(string tag, string id)
    {
        try
        {
            IEnumerable<XmlNode> nodes = _xml.GetElementsByTagName(tag).Cast<XmlNode>();
            return nodes.FirstOrDefault(n => n.Attributes["id"]?.Value == id);
        }
        catch (Exception ex)
        {
            Log.Error("RT_CoT_CMP_NeedleGraph", $"Error in SVGNode: {ex.Message}");
            return null;
        }
    }
}
