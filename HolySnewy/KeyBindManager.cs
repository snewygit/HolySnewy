using System.Collections.Generic;

namespace HyperElk.Core;

public sealed class KeyBindManager
{
    private static KeyBindManager? instance;

    private static readonly string[] RareKeys =
    {
        "D7", "D8", "D9", "D0", "OemMinus", "Oemplus",
        "F6", "F7", "F8", "F9", "F10", "F11", "F12"
    };

    private static readonly string[] UncommonKeys =
    {
        "F1", "F2", "F3", "F4", "F5",
        "NumPad0", "NumPad1", "NumPad2", "NumPad3", "NumPad4", "NumPad5", "NumPad6", "NumPad7", "NumPad8", "NumPad9",
        "Decimal", "Add", "Subtract", "Multiply", "Divide"
    };

    private static readonly string[] CommonKeys =
    {
        "D1", "D2", "D3", "D4", "D5", "D6", "G", "K", "L"
    };

    private static readonly string[] ModifierKeys =
    {
        "ShiftKey", "ControlKey", "Menu"
    };

    // TODO(Snewy): Generate this list dynamically in the constructor.
    private static readonly List<KeyValuePair<string, string>> ModifierCombinations = new()
    {
        new KeyValuePair<string, string>("ShiftKey", "ControlKey"),
        new KeyValuePair<string, string>("ShiftKey", "Menu"),
        new KeyValuePair<string, string>("ControlKey", "Menu")
    };

    private static readonly Stack<KeyBind> FreeBinds = new();

    private KeyBindManager()
    {
        AddKeys(CommonKeys, FreeBinds);
        AddKeys(UncommonKeys, FreeBinds);
        AddKeys(RareKeys, FreeBinds);

        void AddKeys(IEnumerable<string> keys, Stack<KeyBind> binds)
        {
            foreach (var key in keys)
            {
                binds.Push(new KeyBind(key));
                foreach (var modifier in ModifierKeys) binds.Push(new KeyBind(key, modifier));
                foreach (var modifierCombination in ModifierCombinations) binds.Push(new KeyBind(key, modifierCombination.Key, modifierCombination.Value));
            }
        }
    }

    public static KeyBindManager Instance => instance ??= new KeyBindManager();

    public record struct KeyBind(string Key, string Mod1 = "None", string Mod2 = "None");

    public static KeyBind Next()
    {
        instance ??= new KeyBindManager();
        return FreeBinds.Pop();
    }
}