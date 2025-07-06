


using System.Collections.Generic;
using System.Linq;
using FTOptix.RAEtherNetIP;
/**
This class will handle alarm creation and interface with the Optix Obejct Creator

**/
class AlarmCreator
{


    public static int AlarmIdtoint(string AlarmId)
    {
        if (int.TryParse(AlarmId.Replace(".", string.Empty), out int result))
            return result;
        return -1;
    }
    public static void CreateFacePlateAlarms(TreeNode<CtOUINode> alarms, OptixObjectCreator optixCreator)
    {
        List<CtOUINode> values = alarms.GetLeafDescendantsValues();

        foreach (var alarm in values)
        {
            if (alarm.OptixType != "CoT_PlcAlarm")
            {

                optixCreator.CreateOptixUIObject(alarm,false);
            }

        }
    }

    public static void CreateAlarms(TreeNode<CtOUINode> alarms, OptixObjectCreator optixCreator)
    {
        List<CtOUINode> values = alarms.GetLeafDescendantsValues();

        foreach (var alarm in values)
        {
            if (alarm.OptixType == "CoT_PlcAlarm")
            {
                CtOUINode causeRemedies = new();
                alarm.FixedProperties["id"] = AlarmIdtoint(alarm.FixedProperties["id"]).ToString();
                causeRemedies.Name = "CauseRemedy1";
                causeRemedies.OptixType = "CoT_CauseRemedy";
                causeRemedies.PathToNode = $"{alarm.PathToNode}/{alarm.Name}/CausesRemedys";
                List<string> causeremedyKeys = alarm.FixedProperties.Keys.Where(key => key.Contains("causesRemedies")).ToList();
                foreach (var remedies in causeremedyKeys)
                {
                    causeRemedies.FixedProperties.Add(remedies.Split("_")[1], alarm.FixedProperties[remedies]);
                }
                foreach (var remedies in causeremedyKeys)
                {
                    alarm.FixedProperties.Remove(remedies);
                }
                optixCreator.CreateOptixUIObject(alarm,false);
                optixCreator.CreateOptixUIObject(causeRemedies,false);
            }

        }
    }
}
