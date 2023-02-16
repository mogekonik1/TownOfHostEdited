using System.Collections.Generic;
using UnityEngine;

namespace TownOfHost
{
    public static class ChivalrousExpert
    {
        private static readonly int Id = 8021075;
        public static List<byte> playerIdList = new();
        //public static bool isKilled = false;
        public static List<byte> killed = new();

        public static void SetupCustomOption() {
            Options.SetupRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.ChivalrousExpert);
        }

        public static void Init()
        {
            playerIdList = new();
        }

        public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = isKilled(id) ? 255f : 1f;
        public static string GetKillLimit(byte id) => Utils.ColorString(CanUseKillButton(id)? Color.yellow : Color.white, isKilled(id)? $"(0)" : "(1)");
        public static bool CanUseKillButton(byte playerId)
            => !Main.PlayerStates[playerId].IsDead
            && !isKilled(playerId);
        public static bool isKilled(byte playerId) => killed.Contains(playerId);

        public static void Add(byte playerId) {
            playerIdList.Add(playerId);

            if (!Main.ResetCamPlayerList.Contains(playerId)) {
                Main.ResetCamPlayerList.Add(playerId);
            }
        }
    }
}