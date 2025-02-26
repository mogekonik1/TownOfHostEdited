using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using static TOHE.Options;

namespace TOHE.Roles.Crewmate;

public static class Snitch
{
    private static readonly int Id = 20500;
    private static readonly List<byte> playerIdList = new();
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Snitch);

    private static OptionItem OptionEnableTargetArrow;
    private static OptionItem OptionCanGetColoredArrow;
    private static OptionItem OptionCanFindNeutralKiller;
    private static OptionItem OptionCanFindMadmate;
    private static OptionItem OptionRemainingTasks;

    private static bool EnableTargetArrow;
    private static bool CanGetColoredArrow;
    private static bool CanFindNeutralKiller;
    private static bool CanFindMadmate;
    private static int RemainingTasksToBeFound;

    private static readonly Dictionary<byte, bool> IsExposed = new();
    private static readonly Dictionary<byte, bool> IsComplete = new();

    private static readonly HashSet<byte> TargetList = new();
    private static readonly Dictionary<byte, Color> TargetColorlist = new();

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.Snitch);
        OptionEnableTargetArrow = BooleanOptionItem.Create(Id + 10, "SnitchEnableTargetArrow", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Snitch]);
        OptionCanGetColoredArrow = BooleanOptionItem.Create(Id + 11, "SnitchCanGetArrowColor", false, TabGroup.CrewmateRoles, false).SetParent(OptionEnableTargetArrow);
        OptionCanFindNeutralKiller = BooleanOptionItem.Create(Id + 12, "SnitchCanFindNeutralKiller", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Snitch]);
        OptionCanFindMadmate = BooleanOptionItem.Create(Id + 14, "SnitchCanFindMadmate", false, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Snitch]);
        OptionRemainingTasks = IntegerOptionItem.Create(Id + 13, "SnitchRemainingTaskFound", new(0, 10, 1), 1, TabGroup.CrewmateRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.Snitch]);
        OverrideTasksData.Create(Id + 20, TabGroup.CrewmateRoles, CustomRoles.Snitch);
    }
    public static void Init()
    {
        playerIdList.Clear();
        IsEnable = false;

        EnableTargetArrow = OptionEnableTargetArrow.GetBool();
        CanGetColoredArrow = OptionCanGetColoredArrow.GetBool();
        CanFindNeutralKiller = OptionCanFindNeutralKiller.GetBool();
        CanFindMadmate = OptionCanFindMadmate.GetBool();
        RemainingTasksToBeFound = OptionRemainingTasks.GetInt();

        IsExposed.Clear();
        IsComplete.Clear();

        TargetList.Clear();
        TargetColorlist.Clear();
    }

    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        IsEnable = true;

        IsExposed[playerId] = false;
        IsComplete[playerId] = false;
    }

    public static bool IsEnable;
    public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);
    private static bool GetExpose(PlayerControl pc)
    {
        if (!IsThisRole(pc.PlayerId) || !pc.IsAlive() || pc.Is(CustomRoles.Madmate)) return false;

        var snitchId = pc.PlayerId;
        return IsExposed[snitchId];
    }
    private static bool IsSnitchTarget(PlayerControl target) => IsEnable && (target.Is(CustomRoleTypes.Impostor) || (target.IsNeutralKiller() && CanFindNeutralKiller) || (target.Is(CustomRoles.Madmate) && CanFindMadmate));
    public static void CheckTask(PlayerControl snitch)
    {
        if (!snitch.IsAlive() || snitch.Is(CustomRoles.Madmate)) return;

        var snitchId = snitch.PlayerId;
        var snitchTask = snitch.GetPlayerTaskState();

        if (!IsExposed[snitchId] && snitchTask.RemainingTasksCount <= RemainingTasksToBeFound)
        {
            foreach (var target in Main.AllAlivePlayerControls)
            {
                if (!IsSnitchTarget(target)) continue;

                TargetArrow.Add(target.PlayerId, snitchId);
            }
            IsExposed[snitchId] = true;
        }

        if (IsComplete[snitchId] || !snitchTask.IsTaskFinished) return;

        foreach (var target in Main.AllAlivePlayerControls)
        {
            if (!IsSnitchTarget(target)) continue;

            var targetId = target.PlayerId;
            NameColorManager.Add(snitchId, targetId);

            if (!EnableTargetArrow) continue;

            TargetArrow.Add(snitchId, targetId);

            //ターゲットは共通なので2回登録する必要はない
            if (!TargetList.Contains(targetId))
            {
                TargetList.Add(targetId);

                if (CanGetColoredArrow)
                    TargetColorlist.Add(targetId, target.GetRoleColor());
            }
        }
        IsComplete[snitchId] = true;
    }

    /// <summary>
    /// タスクが進んだスニッチに警告マーク
    /// </summary>
    /// <param name="seer">キラーの場合有効</param>
    /// <param name="target">スニッチの場合有効</param>
    /// <returns></returns>
    public static string GetWarningMark(PlayerControl seer, PlayerControl target)
        => IsSnitchTarget(seer) && GetExpose(target) ? Utils.ColorString(RoleColor, "★") : "";

    /// <summary>
    /// キラーからスニッチに対する矢印
    /// </summary>
    /// <param name="seer">キラーの場合有効</param>
    /// <param name="target">キラーの場合有効</param>
    /// <returns></returns>
    public static string GetWarningArrow(PlayerControl seer, PlayerControl target = null)
    {
        if (GameStates.IsMeeting || !IsSnitchTarget(seer)) return "";
        if (target != null && seer.PlayerId != target.PlayerId) return "";

        var exposedSnitch = playerIdList.Where(s => !Main.PlayerStates[s].IsDead && IsExposed[s]);
        if (exposedSnitch.Count() == 0) return "";

        var warning = "★";
        if (EnableTargetArrow)
            warning += TargetArrow.GetArrows(seer, exposedSnitch.ToArray());

        return Utils.ColorString(RoleColor, warning);
    }
    /// <summary>
    /// スニッチからキラーへの矢印
    /// </summary>
    /// <param name="seer">スニッチの場合有効</param>
    /// <param name="target">スニッチの場合有効</param>
    /// <returns></returns>
    public static string GetSnitchArrow(PlayerControl seer, PlayerControl target = null)
    {
        if (!IsThisRole(seer.PlayerId) || seer.Is(CustomRoles.Madmate)) return "";
        if (!EnableTargetArrow || GameStates.IsMeeting) return "";
        if (target != null && seer.PlayerId != target.PlayerId) return "";
        var arrows = "";
        foreach (var targetId in TargetList)
        {
            var arrow = TargetArrow.GetArrows(seer, targetId);
            arrows += CanGetColoredArrow ? Utils.ColorString(TargetColorlist[targetId], arrow) : arrow;
        }
        return arrows;
    }
    public static void OnCompleteTask(PlayerControl player)
    {
        if (!IsThisRole(player.PlayerId) || player.Is(CustomRoles.Madmate)) return;
        CheckTask(player);
    }
}
