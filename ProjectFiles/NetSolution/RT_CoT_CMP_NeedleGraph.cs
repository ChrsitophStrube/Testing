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
using System.Runtime.InteropServices;
using static RT_CoT_Helper;
#endregion

public class RT_CoT_CMP_NeedleGraph : BaseNetLogic
{
    private ResourceUri _svgPath;
    private AdvancedSVGImage _svg;
    private XmlDocument _xml = new XmlDocument();
    private float _newMinimum, _newMaximum, _newLimitRedLow, _newLimitYellowLow, _newLimitYellowHigh, _newLimitRedHigh, svgWidth;
    private IUAVariable _minimum, _maximum, _value;
    private IUAVariable _limitRedLow, _limitYellowLow, _limitYellowHigh, _limitRedHigh;
    private bool _limitRedLowIsValid, _limitYellowLowIsValid, _limitYellowHighIsValid, _limitRedHighIsValid;

    public override void Start()
    {
        try
        {
            _svgPath = new ResourceUri(GetVariable("svgPath", LogicObject).Value);
            _xml.Load(_svgPath.Uri);

            _minimum = GetVariable("minimum", LogicObject);
            _maximum = GetVariable("maximum", LogicObject);
            _value = GetVariable("value", LogicObject);
            _limitRedLow = GetVariable("limitRedLow", LogicObject);
            _limitYellowLow = GetVariable("limitYellowLow", LogicObject);
            _limitYellowHigh = GetVariable("limitYellowHigh", LogicObject);
            _limitRedHigh = GetVariable("limitRedHigh", LogicObject);
            var svgFunc = GetPointer("svg", LogicObject).GetPointedObj<AdvancedSVGImage>;
            _svg = svgFunc?.Invoke();

            _minimum.VariableChange += UpdateSVG;
            _maximum.VariableChange += UpdateSVG;
            _limitRedLow.VariableChange += UpdateSVG;
            _limitYellowLow.VariableChange += UpdateSVG;
            _limitYellowHigh.VariableChange += UpdateSVG;
            _limitRedHigh.VariableChange += UpdateSVG;
            _value.VariableChange += UpdateSVG;

            var main = SVGNode("svg", "main");
            var tempBarWidth = float.Parse(main.Attributes["width"].Value);
            svgWidth = tempBarWidth - 4;

            UpdateSVG();
            SetLabels();
            SetNeedlePosition();

        }
        catch (Exception ex)
        {
            Log.Error("RT_CoT_CMP_NeedleGraph", $"Error in Start: {ex.Message}");
        }
    }

    public override void Stop()
    {
        try
        {
            _minimum.VariableChange -= UpdateSVG;
            _maximum.VariableChange -= UpdateSVG;
            _limitRedLow.VariableChange -= UpdateSVG;
            _limitYellowLow.VariableChange -= UpdateSVG;
            _limitYellowHigh.VariableChange -= UpdateSVG;
            _limitRedHigh.VariableChange -= UpdateSVG;
            _value.VariableChange -= UpdateSVG;
        }
        catch (Exception ex)
        {
            Log.Error("RT_CoT_CMP_NeedleGraph", $"Error in Stop: {ex.Message}");
        }
    }

    private void UpdateSVG(object sender = null, VariableChangeEventArgs e = null)
    {
        try
        {
            ValidateLimits();
            ResetSections();
            SetNeedlePosition();
            SetLabels();
            _svg.SetImageContent(_xml.OuterXml);
        }
        catch (Exception ex)
        {
            Log.Error("RT_CoT_CMP_NeedleGraph", $"Error in OnLimitsChanged: {ex.Message}");
        }
    }

    private void ValidateLimits()
    {
        try
        {
            _newMinimum = _minimum.Value;
            _newMaximum = _maximum.Value;

            _newLimitRedLow = _limitRedLow.Value;
            _newLimitYellowLow = _limitYellowLow.Value;
            _newLimitYellowHigh = _limitYellowHigh.Value;
            _newLimitRedHigh = _limitRedHigh.Value;

            if (_newMinimum > _newMaximum)
            {
                _newMinimum = _maximum.Value;
                _newMaximum = _minimum.Value;
            }


            _limitRedLowIsValid = _newMinimum < _newLimitRedLow && _newLimitRedLow < _newMaximum;
            _limitYellowLowIsValid = _newMinimum < _newLimitYellowLow && _newLimitYellowLow < _newMaximum;
            _limitYellowHighIsValid = _newMinimum < _newLimitYellowHigh && _newLimitYellowHigh < _newMaximum;
            _limitRedHighIsValid = _newMinimum < _newLimitRedHigh && _newLimitRedHigh < _newMaximum;

            List<(float value, bool isValid)> originalLimits = new List<(float, bool)>
                {
                    (_newMinimum, true),
                    (_newLimitRedLow, _limitRedLowIsValid),
                    (_newLimitYellowLow, _limitYellowLowIsValid),
                    (_newLimitYellowHigh, _limitYellowHighIsValid),
                    (_newLimitRedHigh, _limitRedHighIsValid),
                    (_newMaximum, true)
                };

            List<float> sortedValidValues = originalLimits
                .Where(x => x.isValid)
                .Select(x => x.value)
                .OrderBy(x => x)
                .ToList();


            int validIndex = 0;
            List<float> finalOrderedLimits = new List<float>();
            foreach (var (value, isValid) in originalLimits)
            {
                if (isValid)
                    finalOrderedLimits.Add(sortedValidValues[validIndex++]);
                else
                    finalOrderedLimits.Add(0f); // placeholder for invalid
            }

            _newMinimum = finalOrderedLimits[0];
            _newLimitRedLow = finalOrderedLimits[1];
            _newLimitYellowLow = finalOrderedLimits[2];
            _newLimitYellowHigh = finalOrderedLimits[3];
            _newLimitRedHigh = finalOrderedLimits[4];
            _newMaximum = finalOrderedLimits[5];

        }
        catch (Exception ex)
        {
            Log.Error("RT_CoT_CMP_NeedleGraph", $"Error in ValidateLimits: {ex.Message}");
        }
    }

    private float PixelLength(float value)
    {
        try
        {
            return (value - _newMinimum) / (_newMaximum - _newMinimum) * svgWidth;
        }
        catch (Exception ex)
        {
            Log.Error("RT_CoT_CMP_NeedleGraph", $"Error in PixelLength: {ex.Message}");
            return 0f;
        }
    }

    private void ResetSections()
    {
        try
        {
            string _redActiveColor = "#ff4040", _redInActiveColor = "#999999", _yellowActiveColor = "#ffe100", _yellowInActiveColor = "#dbdbdb", _greenActiveColor = "#01bf91", _greenInActiveColor = "#ffffff";

            float _pixelMin = PixelLength(_newMinimum);
            float _pixelMax = PixelLength(_newMaximum);
            float _pixelValue = PixelLength(_value.Value);
            float _pixelRedLow = _limitRedLowIsValid ? PixelLength(_newLimitRedLow) : _pixelMin;
            float _pixelYellowLow = _limitYellowLowIsValid ? PixelLength(_newLimitYellowLow) : _pixelRedLow;
            float _pixelYellowHigh = _limitYellowHighIsValid ? PixelLength(_newLimitYellowHigh) : _pixelYellowLow;
            float _pixelRedHigh = _limitRedHighIsValid ? PixelLength(_newLimitRedHigh) : _pixelYellowHigh;

            List<(float start, float end, string defaultColor, string activeColor, string sectionName)> sections = new();

            if (!_limitRedLowIsValid && !_limitYellowLowIsValid && !_limitYellowHighIsValid && !_limitRedHighIsValid)
            {
                sections.Add((0, 0, _redActiveColor, _redInActiveColor, "sectionOne"));
                sections.Add((0, 0, _yellowActiveColor, _yellowInActiveColor, "sectionTwo"));
                sections.Add((_pixelMin, _pixelMax, _greenActiveColor, _greenInActiveColor, "sectionThree"));
                sections.Add((0, 0, _yellowActiveColor, _yellowInActiveColor, "sectionFour"));
                sections.Add((0, 0, _redActiveColor, _redInActiveColor, "sectionFive"));
            }
            else if (!_limitRedLowIsValid && !_limitYellowLowIsValid && !_limitRedHighIsValid)
            {

                sections.Add((0, 0, _redActiveColor, _redInActiveColor, "sectionOne"));
                sections.Add((0, 0, _yellowActiveColor, _yellowInActiveColor, "sectionTwo"));
                sections.Add((_pixelMin, _pixelYellowHigh, _greenActiveColor, _greenInActiveColor, "sectionThree"));
                sections.Add((_pixelYellowHigh, _pixelMax, _yellowActiveColor, _yellowInActiveColor, "sectionFour"));
                sections.Add((0, 0, _redActiveColor, _redInActiveColor, "sectionFive"));

            }
            else if (!_limitRedLowIsValid && !_limitYellowLowIsValid && !_limitYellowHighIsValid)
            {

                sections.Add((0, 0, _redActiveColor, _redInActiveColor, "sectionOne"));
                sections.Add((0, 0, _yellowActiveColor, _yellowInActiveColor, "sectionTwo"));
                sections.Add((_pixelMin, _pixelRedHigh, _greenActiveColor, _greenInActiveColor, "sectionThree"));
                sections.Add((0, 0, _yellowActiveColor, _yellowInActiveColor, "sectionFour"));
                sections.Add((_pixelRedHigh, _pixelMax, _redActiveColor, _redInActiveColor, "sectionFive"));


            }
            else if (!_limitRedLowIsValid && !_limitRedHighIsValid && !_limitYellowHighIsValid)
            {

                sections.Add((0, 0, _redActiveColor, _redInActiveColor, "sectionOne"));
                sections.Add((_pixelMin, _pixelYellowLow, _yellowActiveColor, _yellowInActiveColor, "sectionTwo"));
                sections.Add((_pixelYellowLow, _pixelMax, _greenActiveColor, _greenInActiveColor, "sectionThree"));
                sections.Add((0, 0, _yellowActiveColor, _yellowInActiveColor, "sectionFour"));
                sections.Add((0, 0, _redActiveColor, _redInActiveColor, "sectionFive"));

            }
            else if (!_limitYellowLowIsValid && !_limitRedHighIsValid && !_limitYellowHighIsValid)
            {

                sections.Add((_pixelMin, _pixelRedLow, _redActiveColor, _redInActiveColor, "sectionOne"));
                sections.Add((0, 0, _yellowActiveColor, _yellowInActiveColor, "sectionTwo"));
                sections.Add((_pixelRedLow, _pixelMax, _greenActiveColor, _greenInActiveColor, "sectionThree"));
                sections.Add((0, 0, _yellowActiveColor, _yellowInActiveColor, "sectionFour"));
                sections.Add((0, 0, _redActiveColor, _redInActiveColor, "sectionFive"));

            }
            else if (!_limitRedLowIsValid && !_limitYellowLowIsValid)
            {

                sections.Add((0, 0, _redActiveColor, _redInActiveColor, "sectionOne"));
                sections.Add((0, 0, _yellowActiveColor, _yellowInActiveColor, "sectionTwo"));
                sections.Add((_pixelMin, _pixelYellowHigh, _greenActiveColor, _greenInActiveColor, "sectionThree"));
                sections.Add((_pixelYellowHigh, _pixelRedHigh, _yellowActiveColor, _yellowInActiveColor, "sectionFour"));
                sections.Add((_pixelRedHigh, _pixelMax, _redActiveColor, _redInActiveColor, "sectionFive"));

            }
            else if (!_limitRedHighIsValid && !_limitYellowHighIsValid)
            {

                sections.Add((_pixelMin, _pixelRedLow, _redActiveColor, _redInActiveColor, "sectionOne"));
                sections.Add((_pixelRedLow, _pixelYellowLow, _yellowActiveColor, _yellowInActiveColor, "sectionTwo"));
                sections.Add((_pixelYellowLow, _pixelMax, _greenActiveColor, _greenInActiveColor, "sectionThree"));
                sections.Add((0, 0, _yellowActiveColor, _yellowInActiveColor, "sectionFour"));
                sections.Add((0, 0, _redActiveColor, _redInActiveColor, "sectionFive"));


            }
            else if (!_limitRedHighIsValid)
            {
                sections.Add((_pixelMin, _pixelRedLow, _redActiveColor, _redInActiveColor, "sectionOne"));
                sections.Add((_pixelRedLow, _pixelYellowLow, _yellowActiveColor, _yellowInActiveColor, "sectionTwo"));
                sections.Add((_pixelYellowLow, _pixelYellowHigh, _greenActiveColor, _greenInActiveColor, "sectionThree"));
                sections.Add((0, 0, _yellowActiveColor, _yellowInActiveColor, "sectionFour"));
                sections.Add((_pixelYellowHigh, _pixelMax, _yellowActiveColor, _yellowInActiveColor, "sectionFive"));

            }
            else if (!_limitYellowHighIsValid)
            {
                sections.Add((_pixelMin, _pixelRedLow, _redActiveColor, _redInActiveColor, "sectionOne"));
                sections.Add((_pixelRedLow, _pixelYellowLow, _yellowActiveColor, _yellowInActiveColor, "sectionTwo"));
                sections.Add((_pixelYellowLow, _pixelRedHigh, _greenActiveColor, _greenInActiveColor, "sectionThree"));
                sections.Add((0, 0, _yellowActiveColor, _yellowInActiveColor, "sectionFour"));
                sections.Add((_pixelRedHigh, _pixelMax, _redActiveColor, _redInActiveColor, "sectionFive"));
            }
            else if (!_limitRedLowIsValid)
            {
                sections.Add((0, 0, _redActiveColor, _redInActiveColor, "sectionOne"));
                sections.Add((_pixelMin, _pixelYellowLow, _yellowActiveColor, _yellowInActiveColor, "sectionTwo"));
                sections.Add((_pixelYellowLow, _pixelYellowHigh, _greenActiveColor, _greenInActiveColor, "sectionThree"));
                sections.Add((_pixelYellowHigh, _pixelRedHigh, _yellowActiveColor, _yellowInActiveColor, "sectionFour"));
                sections.Add((_pixelRedHigh, _pixelMax, _redActiveColor, _redInActiveColor, "sectionFive"));

            }
            else if (!_limitYellowLowIsValid)
            {
                sections.Add((_pixelMin, _pixelRedLow, _redActiveColor, _redInActiveColor, "sectionOne"));
                sections.Add((0, 0, _yellowActiveColor, _yellowInActiveColor, "sectionTwo"));
                sections.Add((_pixelRedLow, _pixelYellowHigh, _greenActiveColor, _greenInActiveColor, "sectionThree"));
                sections.Add((_pixelYellowHigh, _pixelRedHigh, _yellowActiveColor, _yellowInActiveColor, "sectionFour"));
                sections.Add((_pixelRedHigh, _pixelMax, _redActiveColor, _redInActiveColor, "sectionFive"));

            }
            else
            {
                sections.Add((_pixelMin, _pixelRedLow, _redActiveColor, _redInActiveColor, "sectionOne"));
                sections.Add((_pixelRedLow, _pixelYellowLow, _yellowActiveColor, _yellowInActiveColor, "sectionTwo"));
                sections.Add((_pixelYellowLow, _pixelYellowHigh, _greenActiveColor, _greenInActiveColor, "sectionThree"));
                sections.Add((_pixelYellowHigh, _pixelRedHigh, _yellowActiveColor, _yellowInActiveColor, "sectionFour"));
                sections.Add((_pixelRedHigh, _pixelMax, _redActiveColor, _redInActiveColor, "sectionFive"));
            }

            foreach (var (start, end, activeColor, defaultColor, sectionName) in sections)
            {
                string color = _pixelValue >= start && _pixelValue < end ? activeColor : defaultColor;
                if (start == _pixelMin && _pixelValue <= start)
                {
                    color = activeColor;
                }
                if (end == _pixelMax && _pixelValue >= end)
                {
                    color = activeColor;
                }
                SetSection(sectionName, start, end, color);
            }
        }
        catch (Exception ex)
        {
            Log.Error("RT_CoT_CMP_NeedleGraph", $"Error in ResetSections: {ex.Message}");
        }
    }


    private void SetSection(string id, float start, float end, string fill)
    {
        XmlNode rect = SVGNode("rect", id);
        float width = Math.Max(0, end - start);
        rect.Attributes["x"].Value = start.ToString();
        rect.Attributes["width"].Value = width.ToString();
        rect.Attributes["fill"].Value = fill;
    }

    private void SetNeedlePosition()
    {
        try
        {
            float pixel = PixelLength(_value.Value);
            pixel = Math.Clamp(pixel, 0, svgWidth);

            XmlNode needle5 = GetNeedleRect("needleGroup24");
            XmlNode needle10 = GetNeedleRect("needleGroup4");

            needle5.Attributes["x"].Value = pixel.ToString();
            needle10.Attributes["x"].Value = pixel.ToString();


        }
        catch (Exception ex)
        {
            Log.Error("RT_CoT_CMP_NeedleGraph", $"Error in SetNeedlePosition: {ex.Message}");
        }
    }

    private XmlNode GetNeedleRect(string groupId)
    {
        try
        {
            XmlNode group = SVGNode("g", groupId);
            return group?.ChildNodes
                         .OfType<XmlNode>()
                         .FirstOrDefault(n => n.Name == "rect");
        }
        catch (Exception ex)
        {
            Log.Error("RT_CoT_CMP_NeedleGraph", $"Error in GetNeedleRect: {ex.Message}");
            return null;
        }
    }

    private void SetLabels()
    {
        try
        {
            SetLabel(_newMinimum, "minimumText", true);
            SetLabel(_newLimitRedLow, "limitRedLowText", _limitRedLowIsValid);
            SetLabel(_newLimitYellowLow, "limitYellowLowText", _limitYellowLowIsValid);
            SetLabel(_newLimitYellowHigh, "limitYellowHighText", _limitYellowHighIsValid);
            SetLabel(_newLimitRedHigh, "limitRedHighText", _limitRedHighIsValid);
            SetLabel(_newMaximum, "maximumText", true);
        }
        catch (Exception ex)
        {
            Log.Error("RT_CoT_CMP_NeedleGraph", $"Error in SetLabels: {ex.Message}");
        }
    }

    private void SetLabel(float value, string labelName, bool limitValid)
    {
        try
        {
            var Label = GetPointer(labelName, LogicObject).GetPointedObj<CoT_CMP_SyntegonText>;
            float pixelPosition = PixelLength(value);
            float offset = 2f - pixelPosition / 200f * 4f;
            var labelInstance = Label?.Invoke();
            labelInstance.LeftMargin = pixelPosition - (labelInstance.Width / 2f) + offset;
            labelInstance.Text = value.ToString();
            labelInstance.Visible = limitValid;
            if (_newLimitRedLow == _newLimitYellowLow && labelName == "limitRedLowText")
            {
                labelInstance.Visible = false;
            }
            else if (_newLimitRedHigh == _newLimitYellowHigh && labelName == "limitRedHighText")
            {
                labelInstance.Visible = false;
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
            return _xml.GetElementsByTagName(tag)
                      .Cast<XmlNode>()
                      .FirstOrDefault(n => n.Attributes["id"]?.Value == id);
        }
        catch (Exception ex)
        {
            Log.Error("RT_CoT_CMP_NeedleGraph", $"Error in SVGNode: {ex.Message}");
            return null;
        }
    }

}
