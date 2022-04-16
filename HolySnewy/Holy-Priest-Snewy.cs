using System.Collections.Generic;
using System.Linq;

namespace HyperElk.Core;

public class HolyPriest : CombatRoutine
{
    private enum Priority
    {
        Dispel,
        Tank,
        Other,
    }

    private class Unit
    {
        public Unit(string id, Priority priority)
        {
            Id = id;
            Priority = priority;
        }

        public string Id { get; }
        public Priority Priority { get; }
    }

    #region Misc

    private const string HealOutOfCombat = "Heal Out of Combat";

    private static readonly string[] PartyUnits = API.partyunits;

    private static readonly string[] RaidUnits =
    {
        "raid1", "raid2", "raid3", "raid4", "raid5", "raid6", "raid7", "raid8", "raid9", "raid10",
        "raid11", "raid12", "raid13", "raid14", "raid15", "raid16", "raid17", "raid18", "raid19", "raid20",
        "raid21", "raid22", "raid23", "raid24", "raid25", "raid26", "raid27", "raid28", "raid29", "raid30",
        "raid31", "raid32", "raid33", "raid34", "raid35", "raid36", "raid37", "raid38", "raid39", "raid40",
    };

    private Unit? focusUnit;

    private static readonly int[] BuffDispelIds =
    {
    };

    private static readonly int[] DebuffDispelIds =
    {
        325885, // Anguished Cries
        325224, // Anima Injection
        321968, // Bewildering Pollen
        327882, // Blightbeak
        324859, // Bramblethorn Entanglement
        317963, // Burden of Knowledge
        322358, // Burning Strain
        243237, // Burst
        338729, // Charged Anima
        328664, // Chilled
        323347, // Clinging Darkness
        320512, // Corroded Claws
        319070, // Corrosive Gunk
        325725, // Cosmic Artifice
        365297, // Crushing Prism
        327481, // Dark Lance
        324652, // Debilitating Plague
        330700, // Decaying Blight
        364522, // Devouring Blood
        356324, // Empowered Glyph of Restraint
        328331, // Forced Confession
        // NOTE(Snewy): Manually.
        // 320788, // Frozen Binds
        320248, // Genetic Alteration
        355915, // Glyph of Restraint
        364031, // Gloom
        338353, // Goresplatter
        328180, // Gripping Infection
        346286, // Hazardous Liquids
        320596, // Heaving Retch
        332605, // Hex
        328002, // Hurl Spores
        357029, // Hyperlight Bomb
        317661, // Insidious Venom
        327648, // Internal Strife
        322818, // Lost Confidence
        349954, // Purification Protocol
        324293, // Rasping Scream
        328756, // Repulsive Visage
        // NOTE(Snewy): Manually.
        // 360687, // Runecarver's Deathtouch
        355641, // Scintillate
        332707, // Shadow Word: Pain
        334505, // Shimmerdust Sleep
        339237, // Sinlight Visions
        325701, // Siphon Life
        329110, // Slime Injection
        333708, // Soul Corruption
        322557, // Soul Split
        356031, // Stasis Beam
        326632, // Stony Veins
        353835, // Suppression
        326607, // Turn to Stone
        360241, // Unsettling Dreams
        340026, // Wailing Grief
        320529, // Wasting Blight
        341949, // Withering Blight
        321038, // Wrack Soul
    };

    #endregion

    #region Toggles

    private const string AutoFocus = "Auto Focus";
    private const string Dispel = "Dispel";
    private const string Interrupt = "Interrupt";
    private const string Mouseover = "Mouseover";
    private const string OffensiveCds = "OffensiveCds";

    #endregion

    #region Spells

    private const string AngelicFeather = "Angelic Feather";
    private const string Apotheosis = "Apotheosis";
    private const string AscendedBlast = "Ascended Blast";
    private const string AscendedNova = "Ascended Nova";
    private const string BoonOfTheAscended = "Boon of the Ascended";
    private const string CircleOfHealing = "Circle of Healing";
    private const string DesperatePrayer = "Desperate Prayer";
    private const string DispelMagic = "Dispel Magic";
    private const string DivineHymn = "Divine Hymn";
    private const string DivineStar = "Divine Star";
    private const string Fade = "Fade";
    private const string FaeGuardians = "Fae Guardians";
    private const string FlashConcentration = "Flash Concentration";
    private const string FlashHeal = "Flash Heal";
    private const string GuardianSpirit = "Guardian Spirit";
    private const string Halo = "Halo";
    private const string Heal = "Heal";
    private const string HolyFire = "Holy Fire";
    private const string HolyNova = "Holy Nova";
    private const string HolyWordChastise = "Holy Word: Chastise";
    private const string HolyWordSalvation = "Holy Word: Salvation";
    private const string HolyWordSanctify = "Holy Word: Sanctify";
    private const string HolyWordSerenity = "Holy Word: Serenity";
    private const string MassResurrection = "Mass Resurrection";
    private const string Mindgames = "Mindgames";
    private const string PhialOfSerenity = "Phial of Serenity";
    private const string PowerInfusion = "Power Infusion";
    private const string PowerWordFortitude = "Power Word: Fortitude";
    private const string PowerWordShield = "Power Word: Shield";
    private const string PrayerOfHealing = "Prayer of Healing";
    private const string PrayerOfMending = "Prayer of Mending";
    private const string PsychicScream = "Psychic Scream";
    private const string Purify = "Purify";
    private const string Renew = "Renew";
    private const string Resurrection = "Resurrection";
    private const string ShadowWordDeath = "Shadow Word: Death";
    private const string ShadowWordPain = "Shadow Word: Pain";
    private const string Smite = "Smite";
    private const string SurgeOfLight = "Surge of Light";
    private const string Trinket1 = "trinket1";
    private const string Trinket2 = "trinket2";
    private const string UnholyNova = "Unholy Nova";
    private const string WeakenedSoul = "Weakened Soul";

    #endregion

    public override void Initialize()
    {
        Name = "Holy Priest by Snewy";
        isAutoBindReady = true;
        isHealingRotationFocus = true;

        API.WriteLog(Name);

        // Toggles
        AddToggle(AutoFocus);
        AddToggle(Dispel);
        AddToggle(Interrupt);
        AddToggle(Mouseover);
        AddToggle(OffensiveCds);

        // Spells & Menu
        // REVIEW(Snewy): Inspect why it does lead to a rotation freeze if we try to add the Angelic Feather spell when we have it not talented.
        // SetupSpell(AngelicFeather, 121536, 121557, settingBool: true, macroPlayer: true);
        SetupSpell(Apotheosis, 200183, 200183, settingCategory: "Cooldown", settingNumber: 60, settingNumberAoEGroup: 3, settingNumberAoERaid: 6);
        SetupSpell(AscendedBlast, 325283);
        SetupSpell(AscendedNova, 325020);
        AddProp($"{BoonOfTheAscended} Damage", $"{BoonOfTheAscended} Damage", true, $"Use {BoonOfTheAscended} for Damage.", "Covenant");
        SetupSpell(BoonOfTheAscended, 325013, 325013, settingCategory: "Covenant", settingNumber: 80, settingNumberAoEGroup: 3, settingNumberAoERaid: 6);
        SetupSpell(CircleOfHealing, 204883, settingCategory: "Healing", settingNumber: 85, settingNumberAoEGroup: 3, settingNumberAoERaid: 4, macroFocus: true);
        SetupSpell(DesperatePrayer, 19236, settingCategory: "Defense", settingNumber: 40);
        SetupSpell(DispelMagic, 528, settingBool: true);
        SetupSpell(DivineHymn, 64843, settingCategory: "Cooldown", settingNumber: 50, settingNumberAoEGroup: 3, settingNumberAoERaid: 6);
        AddProp($"{DivineStar} Damage", $"{DivineStar} Damage", true, $"Use {DivineStar} for Damage.", "Damage");
        SetupSpell(DivineStar, 110744, settingCategory: "Healing", settingNumber: 85, settingNumberAoEGroup: 2, settingNumberAoERaid: 3);
        SetupSpell(Fade, 586, settingCategory: "Defense", settingBool: true);
        SetupSpell(FaeGuardians, 327661, settingCategory: "Healing", settingNumber: 80, settingNumberAoEGroup: 3, settingNumberAoERaid: 6);
        AddProp(FlashConcentration, FlashConcentration, true, $"{FlashConcentration} (Legendary) stacking.", "Healing");
        SetupSpell(FlashHeal, 2061, settingCategory: "Healing", settingNumber: 65, macroFocus: true);
        SetupSpell(GuardianSpirit, 47788, settingCategory: "Cooldown", settingNumber: 20, macroFocus: true);
        SetupSpell(Halo, 120517, settingCategory: "Healing", settingNumber: 85, settingNumberAoEGroup: 3, settingNumberAoERaid: 6);
        SetupSpell(Heal, 2060, settingCategory: "Healing", settingNumber: 80, macroFocus: true);
        AddProp(HealOutOfCombat, HealOutOfCombat, true, $"{HealOutOfCombat}.");
        SetupSpell(HolyFire, 14914);
        SetupSpell(HolyNova, 132157);
        SetupSpell(HolyWordChastise, 88625, settingCategory: "Interrupt", settingBool: true, macroMouseover: true);
        SetupSpell(HolyWordSalvation, 265202, settingCategory: "Cooldown", settingNumber: 50, settingNumberAoEGroup: 4, settingNumberAoERaid: 8);
        SetupSpell(HolyWordSanctify, 34861, settingCategory: "Healing", settingNumber: 85, settingNumberAoEGroup: 3, settingNumberAoERaid: 4, macroCursor: true);
        SetupSpell(HolyWordSerenity, 2050, settingCategory: "Healing", settingNumber: 70, macroFocus: true);
        SetupSpell(MassResurrection, 212036);
        SetupSpell(Mindgames, 323673);
        AddProp($"{PhialOfSerenity} Percent", $"{PhialOfSerenity} Percent", 40, $"Use {PhialOfSerenity}.", "Defense");
        SetupSpell(PowerInfusion, 10060, settingCategory: "Cooldown", settingBool: true, macroPlayer: true);
        SetupSpell(PowerWordFortitude, 21562, 21562, settingBool: true, macroPlayer: true);
        SetupSpell(PowerWordShield, 17, 17, settingBool: true, macroPlayer: true);
        SetupSpell(PrayerOfHealing, 596, settingCategory: "Healing", settingNumber: 0, settingNumberAoEGroup: 3, settingNumberAoERaid: 4, macroFocus: true);
        SetupSpell(PrayerOfMending, 33076, 41635, settingCategory: "Healing", settingNumber: 99, macroFocus: true);
        SetupSpell(PsychicScream, 8122, settingCategory: "Interrupt", settingBool: false);
        SetupSpell(Purify, 527, settingBool: true, macroFocus: true);
        SetupSpell(Renew, 139, 139, settingCategory: "Healing", settingNumber: 0, macroFocus: true);
        SetupSpell(Resurrection, 2006);
        SetupSpell(ShadowWordDeath, 32379, macroMouseover: true);
        SetupSpell(ShadowWordPain, 589, debuffId: 589, macroMouseover: true);
        SetupSpell(Smite, 585);
        AddProp(Trinket1, Trinket1, new[] {"Friend", "Enemy"}, Trinket1, "Trinket");
        SetupSpell(Trinket1, settingCategory: "Trinket", settingNumber: 60, settingNumberAoEGroup: 3, settingNumberAoERaid: 6);
        AddProp(Trinket2, Trinket2, new[] {"Friend", "Enemy"}, Trinket2, "Trinket");
        SetupSpell(Trinket2, settingCategory: "Trinket", settingNumber: 60, settingNumberAoEGroup: 3, settingNumberAoERaid: 6);
        SetupSpell(UnholyNova, 324724);

        // Buffs
        AddBuff(FlashConcentration, 336267);
        AddBuff(SurgeOfLight, 114255);
        AddBuffDispell(BuffDispelIds);

        // Debuffs
        AddDebuff(WeakenedSoul, 6788);
        AddDebuffDispell(DebuffDispelIds);

        // Items
        AddItem(PhialOfSerenity, 177278);

        // Macros
        AddMacroIntern("focus", "/focus");
        AddMacroIntern(Trinket1);
        AddMacroIntern(Trinket2);
        AddMacroIntern($"{Trinket1} Focus", "/use [@focus] 13");
        AddMacroIntern($"{Trinket2} Focus", "/use [@focus] 14");
    }

    public override void Pulse()
    {
        if (API.PlayerIsMounted || API.PlayerIsChanneling) return;

        if (API.PlayerIsMoving)
        {
            if (CastMovementSpeedIncrease()) return;
        }

        if (API.ToggleIsEnabled(AutoFocus) && (API.PlayerIsInCombat || API.ToggleIsEnabled(Dispel) || GetSettingBool(HealOutOfCombat)))
        {
            FocusUnit();
        }

        if (API.PlayerIsInCombat)
        {
            CombatPulseIntern();
        }
        else
        {
            OutOfCombatPulseIntern();
        }
    }

    public override void CombatPulse()
    {
        // NOTE(Snewy): Is being called ONLY if the player has a valid target - so it is in fact useless for heal rotations.
    }

    public override void OutOfCombatPulse()
    {
    }

    private void CombatPulseIntern()
    {
        if (API.PlayerIsMounted || API.PlayerIsChanneling) return;

        if (API.ToggleIsEnabled(Dispel))
        {
            if (CastDispels()) return;
        }

        if (CastDefensive()) return;
        if (IsCooldowns)
        {
            if (CastCooldowns()) return;
        }

        if (CastHealing()) return;
        if (API.ToggleIsEnabled(Interrupt))
        {
            if (CastInterrupt()) return;
        }

        CastDamage();
    }

    private void OutOfCombatPulseIntern()
    {
        if (API.PlayerIsMounted || API.PlayerIsChanneling) return;

        if (API.ToggleIsEnabled(Dispel))
        {
            if (CastDispels()) return;
        }

        if (GetSettingBool(HealOutOfCombat))
        {
            if (CastHealing()) return;

            if (API.TargetMaxHealth > 1 && API.PlayerCanAttackTarget == false && API.TargetHealthPercent <= 0 && API.PlayerIsMoving == false &&
                API.CanCast(API.UnitBelowHealthPercent(1) > 1 ? MassResurrection : Resurrection))
            {
                API.CastSpell(API.UnitBelowHealthPercent(1) > 1 ? MassResurrection : Resurrection);
                return;
            }
        }

        if (GetSettingBool(PowerWordFortitude) && API.PlayerHasBuff(PowerWordFortitude) == false)
        {
            Spell.CastPlayer(PowerWordFortitude);
        }
    }

    private bool CastCooldowns()
    {
        if (focusUnit is not null && CanCastHeal(GuardianSpirit) && Spell.CastFocus(GuardianSpirit)) return true;

        if (GetSettingBool(PowerInfusion) && API.PlayerIsSolo && Spell.CastPlayer(PowerInfusion)) return true;

        if (CanCastAoEHeal(HolyWordSalvation) && Spell.Cast(HolyWordSalvation, true)) return true;

        if (CanCastAoEHeal(DivineHymn) && Spell.Cast(DivineHymn, true)) return true;

        if (CanCastAoEHeal(Apotheosis) && Spell.Cast(Apotheosis)) return true;

        // TODO(Snewy): Add Symbol of Hope.

        if (GetPropertyInt(Trinket1) == 0 && API.PlayerTrinketIsUsable(1) && API.PlayerTrinketRemainingCD(1) == 0)
        {
            if (focusUnit is not null && CanCastAoEHeal(Trinket1))
            {
                API.CastSpell($"{Trinket1} Focus");
                return true;
            }
        }

        if (GetPropertyInt(Trinket2) == 0 && API.PlayerTrinketIsUsable(2) && API.PlayerTrinketRemainingCD(2) == 0)
        {
            if (focusUnit is not null && CanCastAoEHeal(Trinket2))
            {
                API.CastSpell($"{Trinket2} Focus");
                return true;
            }
        }

        if (GetPropertyInt(Trinket1) == 1 && API.PlayerTrinketIsUsable(1) && API.PlayerTrinketRemainingCD(1) == 0)
        {
            API.CastSpell(Trinket1);
            return true;
        }

        if (GetPropertyInt(Trinket2) == 1 && API.PlayerTrinketIsUsable(2) && API.PlayerTrinketRemainingCD(2) == 0)
        {
            API.CastSpell(Trinket2);
            return true;
        }

        return false;
    }

    private void CastDamage()
    {
        if (API.PlayerIsCasting()) return;

        // Explosives
        if (API.TargetGUIDNPCID == 120651)
        {
            if (Spell.CastTarget(ShadowWordPain)) return;
        }

        if (API.MouseoverGUIDNPCID == 120651)
        {
            if (Spell.CastMouseover(ShadowWordPain, harm: true)) return;
        }

        if (API.PlayerHasBuff(BoonOfTheAscended) && Spell.CastTarget(AscendedBlast)) return;

        if (CanCastDamage(20) && Spell.CastTarget(ShadowWordDeath)) return;
        if (CanCastDamageMouseover(20) && Spell.CastMouseover(ShadowWordDeath, harm: true)) return;

        if (API.PlayerHasBuff(BoonOfTheAscended) && Spell.CastTarget(AscendedNova)) return;

        if (Spell.CastTarget(HolyWordChastise, range: 30)) return;

        if (Spell.CastTarget(HolyFire, true)) return;

        if (GetSettingBool($"{DivineStar} Damage") && API.PlayerIsTalentSelected(6, 2) && API.PlayerFacingTargetDuration > 200 &&
            Spell.CastTarget(DivineStar, range: 24)) return;

        if (API.ToggleIsEnabled(OffensiveCds))
        {
            if (PlayerCovenantSettings == "Kyrian" && GetSettingBool($"{BoonOfTheAscended} Damage") &&
                Spell.CastTarget(BoonOfTheAscended, true, ttd: 500)) return;

            if (PlayerCovenantSettings == "Venthyr" && Spell.CastTarget(Mindgames, true, ttd: 500)) return;

            if (PlayerCovenantSettings == "Necrolord" && Spell.CastTarget(UnholyNova, ttd: 500)) return;
        }

        if (API.PlayerUnitInMeleeRangeCount > AOEUnitNumber && Spell.CastTarget(HolyNova)) return;

        if (API.TargetHasDebuff(ShadowWordPain) == false && Spell.CastTarget(ShadowWordPain, ttd: 600)) return;
        if (API.MouseoverHasDebuff(ShadowWordPain) == false && Spell.CastMouseover(ShadowWordPain, harm: true, ttd: 600)) return;

        if (Spell.CastTarget(Smite, true)) return;

        Spell.CastTarget(ShadowWordPain);
    }

    private bool CastDefensive()
    {
        if (GetSettingBool(Fade) && API.PlayerIsTargetTarget && API.PlayerIsSolo == false && Spell.CastTarget(Fade)) return true;

        if (PlayerCovenantSettings == "Kyrian" && CanCastHealPlayer(PhialOfSerenity) && Spell.Cast(PhialOfSerenity, item: true)) return true;

        if (CanCastHealPlayer(DesperatePrayer) && Spell.Cast(DesperatePrayer)) return true;

        return false;
    }

    private bool CastDispels()
    {
        if (GetSettingBool(DispelMagic) && TargetHasDispellableBuff() && Spell.CastTarget(DispelMagic)) return true;

        if (focusUnit is not null)
        {
            if (GetSettingBool(Purify) && UnitHasDispellableDebuff(focusUnit.Id) && Spell.CastFocus(Purify)) return true;
        }

        return false;
    }

    private bool CastHealing()
    {
        if (focusUnit is null || API.PlayerIsCasting()) return false;

        if (GetSettingBool(FlashConcentration) && API.PlayerHasBuff(FlashConcentration) && API.PlayerBuffTimeRemaining(FlashConcentration) <= 600 &&
            API.PlayerLastSpell != FlashHeal && Spell.CastFocus(FlashHeal, API.PlayerHasBuff(SurgeOfLight) == false)) return true;

        if (API.PlayerHasBuff(BoonOfTheAscended) && Spell.CastTarget(AscendedBlast)) return true;

        if (API.PlayerHasBuff(BoonOfTheAscended) && CanCastHeal(BoonOfTheAscended) && API.FocusRange < 8 &&
            Spell.Cast(AscendedNova)) return true;

        if (CanCastAoEHeal(HolyWordSanctify) && Spell.CastCursor(HolyWordSanctify)) return true;

        if (CanCastHeal(HolyWordSerenity) && Spell.CastFocus(HolyWordSerenity)) return true;

        if (CanCastHeal(PrayerOfMending) && Spell.CastFocus(PrayerOfMending)) return true;

        if (CanCastHeal(Renew) && focusUnit.Priority == Priority.Tank && API.FocusHasBuff(Renew) == false && Spell.CastFocus(Renew)) return true;

        if (PlayerCovenantSettings == "Kyrian" && CanCastAoEHeal(BoonOfTheAscended) && Spell.Cast(BoonOfTheAscended, true)) return true;

        if (CanCastAoEHeal(CircleOfHealing) && Spell.CastFocus(CircleOfHealing)) return true;

        if (CanCastAoEHeal(Halo) && Spell.Cast(Halo, true)) return true;

        if (CanCastAoEHeal(PrayerOfHealing) && Spell.CastFocus(PrayerOfHealing, true)) return true;

        if (CanCastAoEHeal(DivineStar) && API.PlayerFacingTargetDuration > 200 && API.FocusRange < 24 && Spell.Cast(DivineStar)) return true;

        if (PlayerCovenantSettings == "Night Fae" && CanCastAoEHeal(FaeGuardians) && Spell.Cast(FaeGuardians)) return true;

        if (CanCastHeal(Renew) && API.FocusHasBuff(Renew) == false && API.UnitBuffCount(Renew) < 3 && Spell.CastFocus(Renew)) return true;

        if (CanCastHeal(FlashHeal) && (GetSettingBool(FlashConcentration) == false || API.PlayerBuffStacks(FlashConcentration) < 5) &&
            Spell.CastFocus(FlashHeal, API.PlayerHasBuff(SurgeOfLight) == false)) return true;

        if (CanCastHeal(Heal) && (GetSettingBool(FlashConcentration) && API.PlayerHasBuff(FlashConcentration) == false ||
                                  API.PlayerBuffStacks(FlashConcentration) < 5) && Spell.CastFocus(FlashHeal)) return true;

        if (CanCastHeal(Heal) && Spell.CastFocus(Heal, true)) return true;

        return false;
    }

    private bool CastInterrupt()
    {
        if (GetSettingBool(HolyWordChastise) && API.PlayerIsTalentSelected(4, 2))
        {
            if (API.TargetCanInterrupted)
            {
                if (Spell.CastTarget(HolyWordChastise, harm: true, range: 30)) return true;
            }

            if (API.MouseoverCanInterrupted)
            {
                if (Spell.CastMouseover(HolyWordChastise, harm: true, range: 30)) return true;
            }
        }

        if (GetSettingBool(PsychicScream))
        {
            if (API.TargetCanInterrupted && API.TargetRange < 8 ||
                API.MouseoverCanInterrupted && API.MouseoverRange < 8)
            {
                if (Spell.Cast(PsychicScream)) return true;
            }
        }

        return false;
    }

    private void FocusUnit()
    {
        Unit? newFocusUnit;
        // NOTE(Snewy): Sepulcher Heal Adds.
        if (API.TargetGUIDNPCID is 182822 or 184493 && API.TargetHealthPercent < 100)
        {
            newFocusUnit = new Unit("focus", Priority.Other);
        }
        else
        {
            var possibleFocusUnits = GetPossibleFocusUnits();
            newFocusUnit = GetFocusUnit(possibleFocusUnits);
        }

        if (newFocusUnit is not null && (focusUnit is null || API.FocusMaxHealth == 0 || newFocusUnit.Id != focusUnit.Id))
        {
            focusUnit = newFocusUnit;
            API.CastSpell(focusUnit.Id);
        }
    }

    private static void AddMacroCastCursor(string name, int id) =>
        AddMacroIntern($"{name} Cursor", @$"/cast [@cursor] #{id}#
/stopspelltarget");

    private static void AddMacroCastFocus(string name, int id) =>
        AddMacroIntern($"{name} Focus", $"/cast [@focus] #{id}#");

    private static void AddMacroCastMouseover(string name, int id) =>
        AddMacroIntern($"{name} Mouseover", $"/cast [@mouseover] #{id}#");

    private static void AddMacroCastPlayer(string name, int id) =>
        AddMacroIntern($"{name} Player", $"/cast [@player] #{id}#");

    private static void AddMacroIntern(string name, string text = "")
    {
        var (key, mod1, mod2) = KeyBindManager.Next();
        AddMacro(name, key, mod1, mod2, text);
    }

    private static bool CanCastAoEHeal(string name) => API.UnitBelowHealthPercentParty(GetSettingNumber(name)) >= GetSettingNumberAoEGroup(name) ||
                                                       API.UnitBelowHealthPercentRaid(GetSettingNumber(name)) >= GetSettingNumberAoERaid(name);

    private static bool CanCastDamage(int health) => API.TargetHealthPercent <= health;

    private static bool CanCastDamageMouseover(int health) => API.MouseoverHealthPercent <= health;

    private static bool CanCastHeal(string name) => API.FocusHealthPercent <= GetSettingNumber(name);

    private static bool CanCastHealPlayer(string name) => API.PlayerHealthPercent <= GetSettingNumber(name);

    private static bool CastMovementSpeedIncrease()
    {
        if (GetSettingBool(PowerWordShield) &&
            API.PlayerIsTalentSelected(2, 2) &&
            API.PlayerHasDebuff(WeakenedSoul, remainingGDC: false) == false &&
            Spell.CastPlayer(PowerWordShield)) return true;

        // REVIEW(Snewy): Inspect why it does not detect the Buff correctly and recasts it too fast.
        // if (API.PlayerIsTalentSelected(2, 3) &&
        //     GetSettingBool(AngelicFeather) &&
        //     API.PlayerHasBuff(AngelicFeather, remainingGDC: false) == false &&
        //     Spell.CastPlayer(AngelicFeather)) return true;

        return false;
    }

    private static HashSet<Unit> GetPossibleFocusUnits()
    {
        HashSet<Unit> possibleFocusUnits = new();
        if (API.PlayerIsSolo)
        {
            possibleFocusUnits.Add(new Unit("player", Priority.Other));
        }
        else if (API.PlayerIsInGroup && API.PlayerIsInRaid == false)
        {
            AddDispellableUnitsToSet(possibleFocusUnits, PartyUnits);
            var lowestUnit = API.UnitLowestParty(out var lowestTank);
            if (lowestTank is not null && lowestTank != "none" && API.UnitRange(lowestTank) < 40) possibleFocusUnits.Add(new Unit(lowestTank, Priority.Tank));
            if (lowestUnit is not null && lowestUnit != "none" && API.UnitRange(lowestUnit) < 40) possibleFocusUnits.Add(new Unit(lowestUnit, Priority.Other));
        }
        else if (API.PlayerIsInRaid)
        {
            AddDispellableUnitsToSet(possibleFocusUnits, RaidUnits);
            var lowestUnit = API.UnitLowestRaid(out var lowestTank);
            if (lowestTank is not null && lowestTank != "none" && API.UnitRange(lowestTank) < 40) possibleFocusUnits.Add(new Unit(lowestTank, Priority.Tank));
            if (lowestUnit is not null && lowestUnit != "none" && API.UnitRange(lowestUnit) < 40) possibleFocusUnits.Add(new Unit(lowestUnit, Priority.Other));
        }

        void AddDispellableUnitsToSet(ISet<Unit> units, IEnumerable<string> unitsToCheck)
        {
            foreach (var unit in unitsToCheck)
            {
                if (API.ToggleIsEnabled(Dispel) &&
                    unit != "none" &&
                    API.UnitRange(unit) < 40 &&
                    UnitHasDispellableDebuff(unit) &&
                    API.CanCast(Purify))
                {
                    units.Add(new Unit(unit, Priority.Dispel));
                }
            }
        }

        return possibleFocusUnits;
    }

    private static Unit? GetFocusUnit(ISet<Unit> units)
    {
        // #1 Dispels
        var dispellableUnits = units.Where(u => u.Priority == Priority.Dispel).ToArray();
        if (dispellableUnits.Length > 0)
        {
            // NOTE(Snewy): Prioritize non tank unit dispels.
            return dispellableUnits.FirstOrDefault(u => API.UnitRoleSpec(u.Id) != API.TankRole) ?? dispellableUnits[0];
        }

        // #2 Prayer of Mending
        if (API.CanCast(PrayerOfMending))
        {
            var prayerOfMendingUnitTank = units.FirstOrDefault(u =>
                u.Priority == Priority.Tank && API.UnitHasBuff(PrayerOfMending, u.Id) == false &&
                API.UnitHealthPercent(u.Id) <= GetSettingNumber(PrayerOfMending));
            if (prayerOfMendingUnitTank is not null) return prayerOfMendingUnitTank;
            var prayerOfMendingUnitOther = units.FirstOrDefault(u =>
                u.Priority == Priority.Other && API.UnitHasBuff(PrayerOfMending, u.Id) == false &&
                API.UnitHealthPercent(u.Id) <= GetSettingNumber(PrayerOfMending));
            if (prayerOfMendingUnitOther is not null) return prayerOfMendingUnitOther;
        }

        // #3 Tank > Other
        var tankUnit = units.FirstOrDefault(u => u.Priority == Priority.Tank);
        var otherUnit = units.FirstOrDefault(u => u.Priority == Priority.Other);
        if (tankUnit is not null && otherUnit is not null)
        {
            return API.UnitHealthPercent(tankUnit.Id) <= API.UnitHealthPercent(otherUnit.Id) + 5 ? tankUnit : otherUnit;
        }

        return tankUnit ?? otherUnit;
    }

    private static bool GetSettingBool(string name) => GetPropertyBool(name);

    private static int GetSettingNumber(string name) => GetPropertyInt($"{name} Percent");

    private static int GetSettingNumberAoEGroup(string name) => GetPropertyInt($"{name} AoE Group");

    private static int GetSettingNumberAoERaid(string name) => GetPropertyInt($"{name} AoE Raid");

    private static void SetupSpell(string name, int? id = null, int? buffId = null, int? debuffId = null, string settingCategory = "Misc", bool? settingBool = null,
        int? settingNumber = null, int? settingNumberAoEGroup = null, int? settingNumberAoERaid = null, bool? macroCursor = null, bool? macroFocus = null,
        bool? macroMouseover = null, bool? macroPlayer = null)
    {
        var (key, mod1, mod2) = KeyBindManager.Next();
        if (id is not null) AddSpell(name, id.Value, key, mod1, mod2);
        if (buffId is not null) AddBuff(name, buffId.Value);
        if (debuffId is not null) AddDebuff(name, debuffId.Value);
        if (settingBool is not null) AddProp(name, name, settingBool.Value, $"Use {name}.", settingCategory);
        if (settingNumber is not null) AddProp($"{name} Percent", $"{name} Percent", settingNumber.Value, $"Use {name}.", settingCategory);
        if (settingNumberAoEGroup is not null) AddProp($"{name} AoE Group", $"{name} AoE Group", settingNumberAoEGroup.Value, $"Use {name}.", settingCategory);
        if (settingNumberAoERaid is not null) AddProp($"{name} AoE Raid", $"{name} AoE Raid", settingNumberAoERaid.Value, $"Use {name}.", settingCategory);
        if (macroCursor is not null && id is not null) AddMacroCastCursor(name, id.Value);
        if (macroFocus is not null && id is not null) AddMacroCastFocus(name, id.Value);
        if (macroMouseover is not null && id is not null) AddMacroCastMouseover(name, id.Value);
        if (macroPlayer is not null && id is not null) AddMacroCastPlayer(name, id.Value);
    }

    private static bool TargetHasDispellableBuff() => API.TargetHasBuffDispel(BuffDispelIds);
    private static bool UnitHasDispellableDebuff(string unit) => API.UnitHasDebuffDispel(DebuffDispelIds, unit, false, true);
}