using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.ControllerInput;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace DD2ModConfig.Code.Patches;

public static class MouseAndControllerPatch
{
    public static bool isMouseCanceled = false;
    public static bool isControllerCanceled = false;

    [HarmonyPatch(typeof(NMouseCardPlay), "_Input")]
    public static class MouseInputPatch
    {
        public static bool Prefix(InputEvent inputEvent)
        {
            if (inputEvent is InputEventMouseButton { ButtonIndex: var buttonIndex } inputEventMouseButton)
            {
                if (buttonIndex == MouseButton.Right)
                {
                    if (inputEventMouseButton.IsPressed())
                    {
                        isMouseCanceled = true;
                        return true;
                    }
                }
            }
            isMouseCanceled = false;
            return true;
        }
    }

    [HarmonyPatch(typeof(NControllerCardPlay), "_Input")]
    public static class ControllerInputPatch
    {
        public static bool Prefix(InputEvent inputEvent)
        {
            if (inputEvent is InputEventAction inputEventAction)
            {
                if (inputEventAction.IsActionPressed(MegaInput.cancel) || inputEventAction.IsActionPressed(MegaInput.topPanel))
                {
                    isControllerCanceled = true;
                    return true;
                }
            }
            isControllerCanceled = false;
            return true;
        }
    }
}
