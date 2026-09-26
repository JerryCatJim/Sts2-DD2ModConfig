using BaseLib.Extensions;
using HarmonyLib;
using System.Reflection;

namespace DD2ModConfig.Code.Compatibility;

public class DD2VariableMethod
{
    private MethodInfo? _method;

    private readonly Dictionary<Type, MethodInfo> _genericCalls = new Dictionary<Type, MethodInfo>();

    private readonly int[] _paramIndicies;

    private readonly int _requiredParamCount;

    public int ParamCount => _paramIndicies.Length;

    public DD2VariableMethod(params (string, string, Type?[], int[])[] possibleDefinitions)
        : this(possibleDefinitions.Select(((string, string, Type?[], int[]) def) => ((Type?, string, Type?[], int[], Func<MethodInfo, bool>?))(def.Item1.TryGetType(), def.Item2, def.Item3, def.Item4, null)).ToArray())
    {
    }

    public DD2VariableMethod(params (Type?, string, Type?[], int[])[] possibleDefinitions)
        : this(possibleDefinitions.Select(((Type?, string, Type?[], int[]) def) => ((Type?, string, Type?[], int[], Func<MethodInfo, bool>?))(def.Item1, def.Item2, def.Item3, def.Item4, null)).ToArray())
    {
    }

    public DD2VariableMethod(params (Type?, string, Type?[], int[], Func<MethodInfo, bool>?)[] possibleDefinitions)
    {
        ArgumentNullException.ThrowIfNull(possibleDefinitions);

        _paramIndicies = Array.Empty<int>();
        for (int i = 0; i < possibleDefinitions.Length; i++)
        {
            (Type?, string, Type?[], int[], Func<MethodInfo, bool>?) tuple = possibleDefinitions[i];
            if (!(tuple.Item1 == null))
            {
                _requiredParamCount = tuple.Item3.Length;
                _method = tuple.Item1.GetMethodExt(tuple.Item2, tuple.Item5, tuple.Item3);
                if (_method != null)
                {
                    _paramIndicies = tuple.Item4;
                    break;
                }
            }
        }

        if (!(_method == null))
        {
            return;
        }

        throw new Exception("Failed to get VariableMethod " + possibleDefinitions.Join(((Type?, string, Type?[], int[], Func<MethodInfo, bool>?) def) => $"[{def.Item1?.Name ?? "UNKNOWN"}.{def.Item2}({def.Item3.Join((Type? paramType) => paramType?.Name ?? "ANY")})]"));
    }

    public void Invoke(object? instance, params object?[] args)
    {
        object?[] array = new object?[_requiredParamCount];
        int i;
        for (i = 0; i < _paramIndicies.Length; i++)
        {
            array[i] = args[_paramIndicies[i]];
        }

        for (; i < _requiredParamCount; i++)
        {
            array[i] = null;
        }

        _method!.Invoke(instance, array);
    }

    public T? Invoke<T>(object? instance, params object?[] args)
    {
        object?[] array = new object?[_requiredParamCount];
        int i;
        for (i = 0; i < _paramIndicies.Length; i++)
        {
            array[i] = args[_paramIndicies[i]];
        }

        for (; i < _requiredParamCount; i++)
        {
            array[i] = null;
        }

        return (T?)_method!.Invoke(instance, array);
    }

    //BaseLib的Invoke没有返回传入参数的方法，自己加一个
    public (T? Result, object?[] Args) InvokeWithArgs<T>(object? instance, params object?[] args)
    {
        var array = new object?[_requiredParamCount];
        int i;
        for (i = 0; i < _paramIndicies.Length; i++)
            array[i] = args[_paramIndicies[i]];
        for (; i < _requiredParamCount; i++)
            array[i] = null;

        var result = (T?)_method!.Invoke(instance, array);
        return (result, array);
    }

    public TReturn? InvokeGeneric<TReturn, TGeneric>(object? instance, params object?[] args)
    {
        if (!_genericCalls.TryGetValue(typeof(TGeneric), out MethodInfo? value))
        {
            value = _method!.MakeGenericMethod(typeof(TGeneric));
            _genericCalls[typeof(TGeneric)] = value;
        }

        object?[] array = new object?[_requiredParamCount];
        int i;
        for (i = 0; i < _paramIndicies.Length; i++)
        {
            array[i] = args[_paramIndicies[i]];
        }

        for (; i < _requiredParamCount; i++)
        {
            array[i] = null;
        }

        return (TReturn)value.Invoke(instance, array)!;
    }

    public void InvokeGeneric<TGeneric>(object? instance, params object?[] args)
    {
        if (!_genericCalls.TryGetValue(typeof(TGeneric), out MethodInfo? value))
        {
            value = _method!.MakeGenericMethod(typeof(TGeneric));
            _genericCalls[typeof(TGeneric)] = value;
        }

        object?[] array = new object?[_requiredParamCount];
        int i;
        for (i = 0; i < _paramIndicies.Length; i++)
        {
            array[i] = args[_paramIndicies[i]];
        }

        for (; i < _requiredParamCount; i++)
        {
            array[i] = null;
        }

        value.Invoke(instance, array);
    }
}
