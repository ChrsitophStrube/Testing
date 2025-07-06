

using System.Collections.Generic;
using System.Linq;

/**
This class will create cfg model elements and will interface witht the optix object creator to create them in optix
**/
class CfgCreator
{
    public static void CreateFacePlateCfgs(TreeNode<CtOUINode> cfgs, OptixObjectCreator objectCreator)
    {
        List<CtOUINode> cfgValues = cfgs.GetLeafDescendantsValues();

        foreach (var cfgNode in cfgValues)
        {

            if (!isNotFaceplate(cfgNode.OptixType))
            {
                foreach (var item in cfgNode.PhiLinks.Keys)
                {
                    cfgNode.PhiLinks[item] = $"{cfgNode.PlcPathPrefix}/{cfgNode.PhiLinks[item].Replace(".", "/")}";
                }
                objectCreator.CreateOptixUIObject(cfgNode, false);
            }
        }
    }

    private static Dictionary<string, string> optixCFGTypes = new Dictionary<string, string> {
            { "b_", "CoT_CMP_RecipeParamBool" },
            { "r_", "CoT_CMP_RecipeParamFloat" },
            { "f_", "CoT_CMP_RecipeParamFloat" },
            { "dn_", "CoT_CMP_RecipeParamInt" }   ,
            { "n_", "CoT_CMP_RecipeParamInt" },
            { "str_", "CoT_CMP_RecipeParamString" } };
    private static bool isNotFaceplate(string optixType)
    {
        return optixType == "CtO_cfgBaseType" || optixCFGTypes.Values.Contains(optixType);
    }

    private static string GetBaseOptixTypeFromPrefix(string tagName)
    {
        string tagWithoutPath = tagName.Split(".").Last();
        foreach (var key in optixCFGTypes.Keys)
        {
            if (tagWithoutPath.StartsWith(key))
            {
                return optixCFGTypes[key];
            }
        }
        return "CoT_CMP_RecipeParamString";
    }
    public static void CreateCfgs(TreeNode<CtOUINode> cfgs, OptixObjectCreator objectCreator)
    {
        List<CtOUINode> cfgValues = cfgs.GetLeafDescendantsValues();

        foreach (var cfgNode in cfgValues)
        {
            if (isNotFaceplate(cfgNode.OptixType))
            {
                cfgNode.OptixType = GetBaseOptixTypeFromPrefix(cfgNode.FixedProperties["TagName"]);
                cfgNode.PhiLinks.Add("recipeVariable", $"{cfgNode.PlcPathPrefix}/{cfgNode.FixedProperties["TagName"].Replace(".", "/")}");
                cfgNode.Name = cfgNode.FixedProperties["TagName"];
                cfgNode.FixedProperties.Remove("TagName");
                objectCreator.CreateOptixUIObject(cfgNode, false);
            }
        }
    }
}
