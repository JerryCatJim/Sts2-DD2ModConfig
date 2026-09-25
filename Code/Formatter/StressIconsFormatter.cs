using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using SmartFormat.Core.Extensions;
using MegaCrit.Sts2.Core.Localization;
using DD2ModConfig.Code.Powers;

namespace DD2ModConfig.Code.Formatters;

public class StressIconsFormatter : IFormatter
{
    public string Name
    {
        get
        {
            return "stressIcons";
        }
        set
        {
            throw new NotImplementedException();
        }
    }

    public bool CanAutoDetect { get; set; }

    public bool TryEvaluateFormat(IFormattingInfo formattingInfo)
    {
        object? currentValue = formattingInfo.CurrentValue;
        int result = 0;
        if (!(currentValue is PowerVar<StressPower> stressVar))
        {
            if (!(currentValue is CalculatedVar calculatedVar))
            {
                if (!(currentValue is decimal num))
                {
                    if (!(currentValue is int num2))
                    {
                        if (!(currentValue is string text2))
                        {
                            throw new LocException($"Unknown value='{formattingInfo.CurrentValue}' type={formattingInfo.CurrentValue?.GetType()}");
                        }
                        if (!int.TryParse(formattingInfo.FormatterOptions, out result))
                        {
                            return false;
                        }
                    }
                    else
                    {
                        result = num2;
                    }
                }
                else
                {
                    result = (int)num;
                }
            }
            else
            {
                result = Convert.ToInt32(calculatedVar.Calculate(null));
            }
        }
        else
        {
            result = Convert.ToInt32(stressVar.PreviewValue);
        }
        string text3 = "[img=24x24 center]res://DD2ModConfig/Images/FormatterIcons/stress_small.png[/img]";
        string text4 = ((result > 0 && result < 4) ? string.Concat(Enumerable.Repeat(text3, result)) : ((!(formattingInfo.CurrentValue is DynamicVar dynamicVar)) ? $"{result}{text3}" : (dynamicVar.ToHighlightedString(inverse: false) + text3)));
        formattingInfo.Write(text4);
        return true;
    }
}
