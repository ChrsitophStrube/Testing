using System;
using FTOptix.Core;
using UAManagedCore;
using FTOptix.HMIProject;
using System.Runtime.CompilerServices;
using System.IO;

public static class RT_CoT_Helper
{
    #region Pointer

    /// <summary>
    /// Retrieves a NodePointer object by its name from the given logic object.
    /// Throws an exception if the pointer is not found or the name is empty.
    /// </summary>
    /// <param name="logicObject">The logic object containing the pointer.</param>
    /// <param name="pointerName">The name of the pointer to retrieve.</param>
    public static NodePointer GetPointer(string pointerName, IUANode logicObject)
    {
        if (string.IsNullOrEmpty(pointerName))
        {
            throw new CoT_HelperException(
                $"Unable to get Pointer." +
                $"\n PointerName is empty"
                );
        }
        try
        {
            return logicObject.Get<NodePointer>(pointerName) ?? throw new Exception(
                $"Pointer '{pointerName}' not found in {logicObject.BrowseName}"
                );
        }
        catch
        {
            throw new CoT_HelperException(
                $"Unable to get Pointer Object: {pointerName}"
                );

        }
    }

    /// <summary>
    /// Returns the IUANode pointed to by the given NodePointer, or throws an exception if not found.
    /// </summary>
    /// <typeparam name="T">Expected type of the node.</param>
    public static T GetPointedObj<T>(this NodePointer pointer) where T : class, IUANode
    {

        T pointerValue = pointer.GetPointedObjOrDefault<T>();

        if (pointerValue == null)
        {
            throw new CoT_HelperException(
                $"Unable to get Pointed Node for Pointer: {pointer.BrowseName}"
                );
        }

        return pointerValue;
    }

    /// <summary>
    /// Retrieves the pointed node from a NodePointer or returns null if not found.
    /// </summary>
   /* public static T GetPointedObjOrDefault<T>(this NodePointer pointer) where T : class, IUANode
    {

        return InformationModel.Get<T>(pointer.Value);
    }*/
    public static T GetPointedObjOrDefault<T>(this NodePointer pointer) where T : class, IUANode
    {
        if (pointer == null || pointer.Value.Value == null)
        {
            return null;
        }

        IUANode node = InformationModel.Get(pointer.Value);

        // Check Type
        if (node is T typedNode)
        {
            return typedNode;
        }
        else
        {
            throw new CoT_HelperException(
                    $"Invalid type for pointer '{pointer.BrowseName}'.\n" +
                    $"Expected type: '{typeof(T).Name}',\n" +
                    $"Given type: '{node?.GetType().Name ?? "null"}'"
                );
        }
    }

    /// <summary>
    /// Sets the value of a NodePointer to the given NodeId. Throws exception if NodeId is empty.
    /// </summary>
    public static void SetPointedOject(this NodePointer pointer, NodeId nodeId)
    {
        if (nodeId.IsEmpty)
        {
            throw new CoT_HelperException(
                $"Unable to Set Pointed Node for Pointer: {pointer.BrowseName}" +
                $"\n nodeId is null"
            );
        }
        pointer.Value = nodeId;
    }
    #endregion

    #region Alias

    /// <summary>
    /// Retrieves an Alias object of type T by its alias name from the given logic object.
    /// Throws exception if alias is not found or type mismatch.
    /// </summary>
    public static T GetAliasObject<T>(string aliasName, IUAObject logicObject) where T : class, IUANode
    {
        T typedObject = GetAliasObjectOrDefault<T>(aliasName, logicObject);
        if (typedObject == null)
        {
            throw new CoT_HelperException(
                $"Unable to get Pointed Node for Alias: {aliasName}"
                );
        }
        return typedObject;
    }
    /// <summary>
    /// Retrieves an Alias object of type T by its alias name from the given logic object.
    /// Returns null if alias is not found or type mismatch.
    /// </summary>
    public static T GetAliasObjectOrDefault<T>(string aliasName, IUAObject logicObject) where T : class, IUANode
    {
        if (string.IsNullOrEmpty(aliasName))
        {
            throw new CoT_HelperException(
                $"Unable to get Alias." +
                $"\n AliasName is empty"
                );
        }

        try
        {
            var aliasObject = logicObject.GetAlias(aliasName);
            T typedObject = aliasObject as T;
            return typedObject;
        }
        catch
        {
            throw new CoT_HelperException(
                $"Unable to get Alias Object: {aliasName}"
                );
        }
    }

    #endregion

    #region Variables

    /// <summary>
    /// Retrieves an IUAVariable by name from the given logic object.
    /// Throws an exception if the variable is not found.
    /// </summary>
    public static IUAVariable GetVariable(string variableName, IUANode logicObject)
    {
        if (string.IsNullOrEmpty(variableName))
        {
            throw new CoT_HelperException(
                $"Unable to get Variable." +
                $"\n VariableName is empty" +
                $"\n NetLogic Script: {logicObject.BrowseName}"
                );
        }


            var variable = logicObject.GetVariable(variableName);

            return variable ?? throw new CoT_HelperException(
                $"Unable to get Variable Object: {variableName}" +
                $"\nVariable is not a property of the NetLogic Script: {logicObject.BrowseName}"
                );
    }

    /// <summary>
    /// Retrieves the value of an IUAVariable.
    /// </summary>
    public static T GetVariableValue<T>(this IUAVariable variable)
    {

        T variableValue = GetVariableValueOrDefault<T>(variable);
        return variableValue ??
        throw new CoT_HelperException(
            $"Variable '{variable.BrowseName}' is null," +
            $"\nbut should from Typ {typeof(T).Name}"
    );
    }

    /// <summary>
    /// Retrieves the value of an IUAVariable, enforcing exact type match.
    /// Throws exception on mismatch or null value.
    /// </summary>
    public static T GetVariableValueOrDefault<T>(this IUAVariable variable)
    {
        if (variable == null)
        {
            throw new CoT_HelperException(
                $"variable '{variable.BrowseName}' is null"
            );
        }
        UAValue uaValue = variable.Value;
        object rawValue = uaValue?.Value;

        if (rawValue == null)
        {
            return default(T);
        }

        if (rawValue is T typedValue)
        {
            return typedValue;
        }
        else
        {
            throw new CoT_HelperException(
            $"Unable to get Variable '{variable.BrowseName}'\n" +
            $"Value expected type: '{typeof(T).Name}'\n" +
            $"Given type: '{rawValue.GetType().Name}'"
            );
        }
    }
    /// <summary>
    /// Sets the value of an IUAVariable, enforcing exact type match.
    /// Throws exception on type mismatch.
    /// </summary>
    public static void SetVariableValue<T>(this IUAVariable variable, T newValue)
    {
        if (variable == null)
        {
            throw new CoT_HelperException(
                $"variable '{variable.BrowseName}' is null"
                );
        }

        if (newValue == null)
        {
            throw new CoT_HelperException(
                 $"Cannot set null to variable '{variable.BrowseName}'"
                 );
        }

        UAValue currentUaValue = variable.Value;
        object currentRawValue = currentUaValue?.Value;

        if (currentRawValue != null && currentRawValue.GetType() != typeof(T))
        {
            throw new CoT_HelperException(
                $"Cannot assign value to variable '{variable.BrowseName}'\n" +
                $"Expected type: '{currentRawValue.GetType().Name}'\n" +
                $"Attempted type: '{typeof(T).Name}'"
            );
        }
        variable.Value = new UAValue(newValue);
    }
    #endregion

    #region Children
    //TODO Implement: children objetct get and set methods
    #endregion

    #region methods helper
    //TODO Implement: methods to support optix Export Methods
    #endregion

    /// <summary>
    /// Custom Exception class for RT_CoT_Helper errors.
    /// </summary>
    public class CoT_HelperException : Exception
    {
        public CoT_HelperException(string message)
            : base(message)
        {
        }
    }
}
