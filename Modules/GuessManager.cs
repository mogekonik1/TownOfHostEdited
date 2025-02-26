﻿using Hazel;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.Translator;

namespace TOHE;

public static class GuessManager
{

    public static string GetFormatString()
    {
        string text = GetString("PlayerIdList");
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            string id = pc.PlayerId.ToString();
            string name = pc.GetRealName();
            text += $"\n{id} → {name}";
        }
        return text;
    }

    public static bool CheckCommond(ref string msg, string command, bool exact = true)
    {
        var comList = command.Split('|');
        for (int i = 0; i < comList.Count(); i++)
        {
            if (exact)
            {
                if (msg == "/" + comList[i]) return true;
            }
            else
            {
                if (msg.StartsWith("/" + comList[i]))
                {
                    msg = msg.Replace("/" + comList[i], string.Empty);
                    return true;
                }
            }
        }
        return false;
    }

    public static byte GetColorFromMsg(string msg)
    {
        if (ComfirmIncludeMsg(msg, "红|紅|red")) return 0;
        if (ComfirmIncludeMsg(msg, "蓝|藍|深蓝|blue")) return 1;
        if (ComfirmIncludeMsg(msg, "绿|綠|深绿|green")) return 2;
        if (ComfirmIncludeMsg(msg, "粉红|粉紅|pink")) return 3;
        if (ComfirmIncludeMsg(msg, "橘|橘|orange")) return 4;
        if (ComfirmIncludeMsg(msg, "黄|黃|yellow")) return 5;
        if (ComfirmIncludeMsg(msg, "黑|黑|black")) return 6;
        if (ComfirmIncludeMsg(msg, "白|白|white")) return 7;
        if (ComfirmIncludeMsg(msg, "紫|紫|perple")) return 8;
        if (ComfirmIncludeMsg(msg, "棕|棕|brown")) return 9;
        if (ComfirmIncludeMsg(msg, "青|青|cyan")) return 10;
        if (ComfirmIncludeMsg(msg, "黄绿|黃綠|浅绿|lime")) return 11;
        if (ComfirmIncludeMsg(msg, "红褐|紅褐|深红|maroon")) return 12;
        if (ComfirmIncludeMsg(msg, "玫红|玫紅|浅粉|rose")) return 13;
        if (ComfirmIncludeMsg(msg, "焦黄|焦黃|淡黄|banana")) return 14;
        if (ComfirmIncludeMsg(msg, "灰|灰|gray")) return 15;
        if (ComfirmIncludeMsg(msg, "茶|茶|tan")) return 16;
        if (ComfirmIncludeMsg(msg, "珊瑚|珊瑚|coral")) return 17;
        else return byte.MaxValue;
    }

    private static bool ComfirmIncludeMsg(string msg, string key)
    {
        var keys = key.Split('|');
        for (int i = 0; i < keys.Count(); i++)
        {
            if (msg.Contains(keys[i])) return true;
        }
        return false;
    }

    public static bool GuesserMsg(PlayerControl pc, string msg)
    {
        var originMsg = msg;

        if (!AmongUsClient.Instance.AmHost) return false;
        if (!GameStates.IsInGame || pc == null) return false;
        if (!pc.Is(CustomRoles.NiceGuesser) && !pc.Is(CustomRoles.EvilGuesser)) return false;

        int operate = 0; // 1:ID 2:猜测
        msg = msg.ToLower().TrimStart().TrimEnd();
        if (CheckCommond(ref msg, "id|guesslist|gl编号|玩家编号|玩家id|id列表|玩家列表|列表|所有id|全部id")) operate = 1;
        else if (CheckCommond(ref msg, "shoot|guess|bet|st|gs|bt|猜|赌", false)) operate = 2;
        else return false;

        if (!pc.IsAlive())
        {
            Utils.SendMessage(GetString("GuessDead"), pc.PlayerId);
            return true;
        }

        if (operate == 1)
        {
            Utils.SendMessage(GetFormatString(), pc.PlayerId);
            return true;
        }
        else if (operate == 2)
        {

            if (
            (pc.Is(CustomRoles.NiceGuesser) && Options.GGTryHideMsg.GetBool()) ||
            (pc.Is(CustomRoles.EvilGuesser) && Options.EGTryHideMsg.GetBool())
            ) TryHideMsg();
            else if (pc.AmOwner) Utils.SendMessage(originMsg, 255, pc.GetRealName());

            if (!MsgToPlayerAndRole(msg, out byte targetId, out CustomRoles role, out string error))
            {
                Utils.SendMessage(error, pc.PlayerId);
                return true;
            }
            var target = Utils.GetPlayerById(targetId);
            if (target != null)
            {
                bool guesserSuicide = false;
                if (!Main.GuesserGuessed.ContainsKey(pc.PlayerId)) Main.GuesserGuessed.Add(pc.PlayerId, 0);
                if (pc.Is(CustomRoles.NiceGuesser) && Main.GuesserGuessed[pc.PlayerId] >= Options.GGCanGuessTime.GetInt())
                {
                    Utils.SendMessage(GetString("GGGuessMax"), pc.PlayerId);
                    return true;
                }
                if (pc.Is(CustomRoles.EvilGuesser) && Main.GuesserGuessed[pc.PlayerId] >= Options.EGCanGuessTime.GetInt())
                {
                    Utils.SendMessage(GetString("EGGuessMax"), pc.PlayerId);
                    return true;
                }
                if (role == CustomRoles.SuperStar && target.Is(CustomRoles.SuperStar))
                {
                    Utils.SendMessage(GetString("GuessSuperStar"), pc.PlayerId);
                    return true;
                }
                if (role == CustomRoles.GM || target.Is(CustomRoles.GM))
                {
                    Utils.SendMessage(GetString("GuessGM"), pc.PlayerId);
                    return true;
                }
                if (target.Is(CustomRoles.Snitch) && target.AllTasksCompleted())
                {
                    Utils.SendMessage(GetString("EGGuessSnitchTaskDone"), pc.PlayerId);
                    return true;
                }
                if (role.IsAdditionRole())
                {
                    if (
                        (pc.Is(CustomRoles.NiceGuesser) && !Options.GGCanGuessAdt.GetBool()) ||
                        (pc.Is(CustomRoles.EvilGuesser) && !Options.EGCanGuessAdt.GetBool())
                        )
                    {
                        Utils.SendMessage(GetString("GuessAdtRole"), pc.PlayerId);
                        return true;
                    }
                }
                if (pc.PlayerId == target.PlayerId)
                {
                    Utils.SendMessage(GetString("LaughToWhoGuessSelf"), pc.PlayerId, Utils.ColorString(Color.cyan, GetString("MessageFromDevTitle")));
                    guesserSuicide = true;
                }
                else if (pc.Is(CustomRoles.NiceGuesser) && role.IsCrewmate() && !Options.GGCanGuessCrew.GetBool() && !pc.Is(CustomRoles.Madmate)) guesserSuicide = true;
                else if (pc.Is(CustomRoles.EvilGuesser) && role.IsImpostor() && !Options.EGCanGuessImp.GetBool()) guesserSuicide = true;
                else if (!target.Is(role)) guesserSuicide = true;

                var dp = guesserSuicide ? pc : target;
                target = dp;

                string Name = dp.GetRealName();

                Main.GuesserGuessed[pc.PlayerId]++;

                new LateTask(() =>
                {
                    Main.PlayerStates[dp.PlayerId].deathReason = PlayerState.DeathReason.Gambled;
                    dp.SetRealKiller(pc);
                    RpcGuesserMurderPlayer(dp);

                    //死者检查
                    FixedUpdatePatch.LoversSuicide(target.PlayerId);
                    if (target.Is(CustomRoles.Terrorist))
                    {
                        Logger.Info(target?.Data?.PlayerName + "はTerroristだった", "MurderPlayer");
                        Utils.CheckTerroristWin(target.Data);
                    }
                    if (Executioner.Target.ContainsValue(target.PlayerId))
                        Executioner.ChangeRoleByTarget(target);

                    Utils.NotifyRoles(isForMeeting: true, NoCache: true);

                    new LateTask(() => { Utils.SendMessage(string.Format(GetString("GuessKill"), Name), 255, Utils.ColorString(Utils.GetRoleColor(CustomRoles.NiceGuesser), GetString("GuessKillTitle"))); }, 0.6f, "Guess Msg");

                }, 0.2f, "Guesser Kill");
            }
        }
        return true;
    }

    public static TMPro.TextMeshPro nameText(this PlayerControl p) => p.cosmetics.nameText;
    public static TMPro.TextMeshPro NameText(this PoolablePlayer p) => p.cosmetics.nameText;
    public static void RpcGuesserMurderPlayer(this PlayerControl pc, float delay = 0f) //ゲッサー用の殺し方
    {
        // DEATH STUFF //
        var amOwner = pc.AmOwner;
        pc.Data.IsDead = true;
        pc.RpcExileV2();
        Main.PlayerStates[pc.PlayerId].SetDead();
        var meetingHud = MeetingHud.Instance;
        var hudManager = DestroyableSingleton<HudManager>.Instance;
        SoundManager.Instance.PlaySound(pc.KillSfx, false, 0.8f);
        hudManager.KillOverlay.ShowKillAnimation(pc.Data, pc.Data);
        if (amOwner)
        {
            hudManager.ShadowQuad.gameObject.SetActive(false);
            pc.nameText().GetComponent<MeshRenderer>().material.SetInt("_Mask", 0);
            pc.RpcSetScanner(false);
            ImportantTextTask importantTextTask = new GameObject("_Player").AddComponent<ImportantTextTask>();
            importantTextTask.transform.SetParent(AmongUsClient.Instance.transform, false);
            meetingHud.SetForegroundForDead();
        }
        PlayerVoteArea voteArea = MeetingHud.Instance.playerStates.First(
            x => x.TargetPlayerId == pc.PlayerId
        );
        if (voteArea == null) return;
        if (voteArea.DidVote) voteArea.UnsetVote();
        voteArea.AmDead = true;
        voteArea.Overlay.gameObject.SetActive(true);
        voteArea.Overlay.color = Color.white;
        voteArea.XMark.gameObject.SetActive(true);
        voteArea.XMark.transform.localScale = Vector3.one;
        foreach (var playerVoteArea in meetingHud.playerStates)
        {
            if (playerVoteArea.VotedFor != pc.PlayerId) continue;
            playerVoteArea.UnsetVote();
            var voteAreaPlayer = Utils.GetPlayerById(playerVoteArea.TargetPlayerId);
            if (!voteAreaPlayer.AmOwner) continue;
            meetingHud.ClearVote();
        }
        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.Guess, SendOption.Reliable, -1);
        writer.Write(pc.PlayerId);
        AmongUsClient.Instance.FinishRpcImmediately(writer);
    }
    public static void RpcClientGuess(PlayerControl pc)
    {
        var amOwner = pc.AmOwner;
        var meetingHud = MeetingHud.Instance;
        var hudManager = DestroyableSingleton<HudManager>.Instance;
        SoundManager.Instance.PlaySound(pc.KillSfx, false, 0.8f);
        hudManager.KillOverlay.ShowKillAnimation(pc.Data, pc.Data);
        if (amOwner)
        {
            hudManager.ShadowQuad.gameObject.SetActive(false);
            pc.nameText().GetComponent<MeshRenderer>().material.SetInt("_Mask", 0);
            pc.RpcSetScanner(false);
            ImportantTextTask importantTextTask = new GameObject("_Player").AddComponent<ImportantTextTask>();
            importantTextTask.transform.SetParent(AmongUsClient.Instance.transform, false);
            meetingHud.SetForegroundForDead();
        }
        PlayerVoteArea voteArea = MeetingHud.Instance.playerStates.First(
            x => x.TargetPlayerId == pc.PlayerId
        );
        //pc.Die(DeathReason.Kill);
        if (voteArea == null) return;
        if (voteArea.DidVote) voteArea.UnsetVote();
        voteArea.AmDead = true;
        voteArea.Overlay.gameObject.SetActive(true);
        voteArea.Overlay.color = Color.white;
        voteArea.XMark.gameObject.SetActive(true);
        voteArea.XMark.transform.localScale = Vector3.one;
        foreach (var playerVoteArea in meetingHud.playerStates)
        {
            if (playerVoteArea.VotedFor != pc.PlayerId) continue;
            playerVoteArea.UnsetVote();
            var voteAreaPlayer = Utils.GetPlayerById(playerVoteArea.TargetPlayerId);
            if (!voteAreaPlayer.AmOwner) continue;
            meetingHud.ClearVote();
        }
    }
    private static bool MsgToPlayerAndRole(string msg, out byte id, out CustomRoles role, out string error)
    {
        if (msg.StartsWith("/")) msg = msg.Replace("/", string.Empty);

        Regex r = new("\\d+");
        MatchCollection mc = r.Matches(msg);
        string result = string.Empty;
        for (int i = 0; i < mc.Count; i++)
        {
            result += mc[i];//匹配结果是完整的数字，此处可以不做拼接的
        }

        if (int.TryParse(result, out int num))
        {
            id = Convert.ToByte(num);
        }
        else
        {
            //并不是玩家编号，判断是否颜色
            //byte color = GetColorFromMsg(msg);
            //好吧我不知道怎么取某位玩家的颜色，等会了的时候再来把这里补上
            id = byte.MaxValue;
            error = GetString("GuessHelp");
            role = new();
            return false;
        }

        //判断选择的玩家是否合理
        PlayerControl target = Utils.GetPlayerById(id);
        if (target == null || target.Data.IsDead)
        {
            error = GetString("GuessNull");
            role = new();
            return false;
        }

        if (!ChatCommands.GetRoleByName(msg, out role))
        {
            error = GetString("GuessHelp");
            return false;
        }

        error = string.Empty;
        return true;
    }

    public static void TryHideMsg()
    {
        ChatUpdatePatch.DoBlockChat = true;
        Array values = Enum.GetValues(typeof(CustomRoles));
        var rd = IRandom.Instance;
        string msg;
        string[] command = new string[] { "bet", "bt", "guess", "gs", "shoot", "st", "赌", "猜", "审判", "tl", "判", "审" };
        for (int i = 0; i < 20; i++)
        {
            msg = "/";
            if (rd.Next(1, 100) < 30)
            {
                msg += "id";
            }
            else
            {
                msg += command[rd.Next(0, command.Length - 1)];
                msg += rd.Next(1, 100) < 50 ? string.Empty : " ";
                msg += rd.Next(0, 15).ToString();
                msg += rd.Next(1, 100) < 50 ? string.Empty : " ";
                CustomRoles role = (CustomRoles)values.GetValue(rd.Next(values.Length));
                msg += rd.Next(1, 100) < 50 ? string.Empty : " ";
                msg += Utils.GetRoleName(role);
            }
            var player = Main.AllAlivePlayerControls.ToArray()[rd.Next(0, Main.AllAlivePlayerControls.Count())];
            DestroyableSingleton<HudManager>.Instance.Chat.AddChat(player, msg);
            var writer = CustomRpcSender.Create("MessagesToSend", SendOption.None);
            writer.StartMessage(-1);
            writer.StartRpc(player.NetId, (byte)RpcCalls.SendChat)
                .Write(msg)
                .EndRpc();
            writer.EndMessage();
            writer.SendMessage();
        }
        ChatUpdatePatch.DoBlockChat = false;
    }
}
