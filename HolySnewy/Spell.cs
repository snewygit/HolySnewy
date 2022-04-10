using System;

namespace HyperElk.Core;

public static class Spell
{
    private enum CastModifier
    {
        Cursor,
        Focus,
        Mouseover,
        None,
        Player,
        Target
    }

    public static bool Cast(string name, bool preventMovement = false, bool harm = false, int ttd = 0, int range = 40, bool item = false) =>
        CastIntern(name, CastModifier.None, preventMovement, harm, ttd, range, item);
    
    public static bool CastCursor(string name, bool preventMovement = false, bool harm = false, int ttd = 0, int range = 40, bool item = false) =>
        CastIntern(name, CastModifier.Cursor, preventMovement, harm, ttd, range, item);
    
    public static bool CastFocus(string name, bool preventMovement = false, bool harm = false, int ttd = 0, int range = 40, bool item = false) =>
        CastIntern(name, CastModifier.Focus, preventMovement, harm, ttd, range, item);
    
    public static bool CastMouseover(string name, bool preventMovement = false, bool harm = false, int ttd = 0, int range = 40, bool item = false) =>
        CastIntern(name, CastModifier.Mouseover, preventMovement, harm, ttd, range, item);
    
    public static bool CastPlayer(string name, bool preventMovement = false, bool item = false) =>
        CastIntern(name, CastModifier.Player, preventMovement, item);
    
    public static bool CastTarget(string name, bool preventMovement = false, bool harm = true, int ttd = 0, int range = 40, bool item = false) =>
        CastIntern(name, CastModifier.Target, preventMovement, harm, ttd, range, item);

    private static bool CastIntern(string name, CastModifier castModifier = CastModifier.None, bool preventMovement = false, bool harm = false, int ttd = 0, int range = 40, bool item = false)
    {
        if (API.PlayerIsDead) return false;
        if (preventMovement && API.PlayerIsMoving) return false;
        if (item == false && API.CanCast(name) == false) return false;
        if (item && API.PlayerItemCanUse(name) && API.PlayerItemRemainingCD(name) > 0) return false;

        string castSpellIdentifier;
        switch (castModifier)
        {
            case CastModifier.Cursor:
            {
                if (harm && API.PlayerCanAttackMouseover == false) return false;
                if (API.MouseoverHealthPercent <= 0) return false;
                if (API.MouseoverRange > range) return false;
                if (API.MouseoverTTD < ttd) return false;
                castSpellIdentifier = $"{name} Cursor";
            }
                break;
            case CastModifier.Focus:
            {
                if (harm && API.PlayerCanAttackFocus == false) return false;
                if (API.FocusHealthPercent <= 0) return false;
                if (API.FocusRange > range) return false;
                if (API.FocusTTD < ttd) return false;
                castSpellIdentifier = $"{name} Focus";
            }
                break;
            case CastModifier.Mouseover:
            {
                if (API.ToggleIsEnabled("Mouseover") == false) return false;
                if (harm && API.PlayerCanAttackMouseover == false) return false;
                if (API.MouseoverHealthPercent <= 0) return false;
                if (API.MouseoverRange > range) return false;
                if (API.MouseoverTTD < ttd) return false;
                castSpellIdentifier = $"{name} Mouseover";
            }
                break;
            case CastModifier.None:
            {
                castSpellIdentifier = name;
            }
                break;
            case CastModifier.Player:
            {
                castSpellIdentifier = $"{name} Player";
            }
                break;
            case CastModifier.Target:
            {
                if (harm && API.PlayerCanAttackTarget == false) return false;
                if (API.TargetHealthPercent <= 0) return false;
                if (API.TargetRange > range) return false;
                if (API.TargetTTD < ttd) return false;
                castSpellIdentifier = name;
            }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(castModifier), castModifier, null);
        }

        API.CastSpell(castSpellIdentifier);
        return true;
    }
}