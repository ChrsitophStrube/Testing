using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;

class ScreenCreator
{


    // private Dictionary<string, List<string>> _possiblePLCLinks;

    public ScreenCreator()
    {
        // _possiblePLCLinks = [];
        // _possiblePLCLinks.Add("CoT_CTRL_Led", ["status"]);
        // _possiblePLCLinks.Add("CoT_CTRL_Button", ["pressed", "enable"]);
        // _possiblePLCLinks.Add("CoT_CTRL_VarOut", ["varOutValue"]);
        // _possiblePLCLinks.Add("CoT_CTRL_VarIn", ["value", "minimum", "maximum"]);
        // _possiblePLCLinks.Add("CoT_CTRL_TextOut", ["textOutText"]);
        // _possiblePLCLinks.Add("CoT_CTRL_TextIn", ["textInText"]);
        // _possiblePLCLinks.Add("CoT_CTRL_Switch", ["command", "feedback", "enable"]);
        // _possiblePLCLinks.Add("CoT_CTRL_CheckBox", ["checked"]);
        // _possiblePLCLinks.Add("CoT_CRTL_RadioButton", ["selectedOption", "optionID"]);
        // _possiblePLCLinks.Add("CoT_FP_VarOutREAL", ["phi"]);
    }

    public List<CtOUINode> genereatePLCLinks(List<CtOUINode> screens)
    {
        foreach (var screen in screens.FindAll(s => s.OptixType != null))
        {

            foreach (var prop in screen.FixedProperties.Keys)
            {
                if (screen.FixedProperties[prop].StartsWith("${"))
                {
                    screen.PhiLinks.Add(prop, $"{screen.PlcPathPrefix}/{screen.FixedProperties[prop].Replace(".", "/").Replace("${", "").Replace("}", "")}");
                    screen.FixedProperties.Remove(prop);

                }
            }
            // if (_possiblePLCLinks.ContainsKey(screen.OptixType))
            // {
            //     foreach (var possibleLink in _possiblePLCLinks[screen.OptixType])
            //     {
            //         if (screen.FixedProperties.ContainsKey(possibleLink))
            //         {
            //             if (IsPLCTag(screen.FixedProperties[possibleLink]))
            //             {
            //                 screen.PhiLinks.Add(possibleLink, $"{screen.PlcPathPrefix}/{screen.FixedProperties[possibleLink].Replace(".", "/")}");
            //                 screen.FixedProperties.Remove(possibleLink);
            //             }
            //         }
            //     }
            // }
        }
        return screens;
    }
    private bool IsPLCTag(string inputValue)
    {
        if (inputValue.Contains('/'))
            return true;
        return false;
    }
}
