using DD2ModConfig.Code.ResoluteOrMeltdown;
using MegaCrit.Sts2.Core.Models;
using System.Reflection;

namespace DD2ModConfig.Code.Core;

public class RMModelDb
{
    // 泛型方法定义只查一次
    private static readonly MethodInfo? GenericMethod = typeof(RMModelDb)
        .GetMethods(BindingFlags.Public | BindingFlags.Static)
        .FirstOrDefault(m =>
            m.Name == nameof(ResoluteOrMeltdown) &&
            m.IsGenericMethodDefinition &&
            m.GetParameters().Length == 0);
    public static T ResoluteOrMeltdown<T>() where T : ResoluteOrMeltdownModel
    {
        return ModelDb.GetById<T>(ModelDb.GetId<T>());
    }
    public static ResoluteOrMeltdownModel ResoluteOrMeltdown(Type modelType)
    {
        if (modelType == null)
            throw new ArgumentNullException(nameof(modelType));

        if (GenericMethod == null)
            throw new InvalidOperationException(
                "Generic overload of ResoluteOrMeltdown<T>() not found.");

        if (!typeof(ResoluteOrMeltdownModel).IsAssignableFrom(modelType))
            throw new ArgumentException(
                $"{modelType.FullName} is not a ResoluteOrMeltdownModel.", nameof(modelType));

        if (modelType.IsAbstract || modelType.IsInterface)
            throw new ArgumentException(
                $"{modelType.FullName} is abstract or interface.", nameof(modelType));

        if (modelType.ContainsGenericParameters)
            throw new ArgumentException(
                $"{modelType.FullName} contains unbound generic parameters.", nameof(modelType));

        MethodInfo closed;
        try
        {
            closed = GenericMethod.MakeGenericMethod(modelType);
        }
        catch (ArgumentException e)
        {
            throw new ArgumentException(
                $"Failed to close generic method for {modelType.FullName}. " +
                $"Check that it satisfies 'where T : ResoluteOrMeltdownModel'.", e);
        }

        object? result;
        try
        {
            //第一个参数为调用者this，但静态方法调用者为null，第二个参数是函数参数，函数没有参数所以为空
            result = closed.Invoke(null, Array.Empty<object>());
        }
        catch (TargetInvocationException e)
        {
            // 拆掉反射包装，把真实异常抛出去
            throw e.InnerException ?? e;
        }

        if (result is not ResoluteOrMeltdownModel model)
            throw new InvalidOperationException(
                $"RMModelDb.ResoluteOrMeltdown<{modelType.Name}>() returned null or wrong type.");

        return model;
    }
}