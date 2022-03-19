using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PKHeX.Core;
using SysBot.Base;

namespace SysBot.Pokemon.Dodo
{
    public class DodoHelper<T> where T : PKM, new()
    {
        public static void StartTrade(string ps, string dodoId, string nickName, string channelId)
        {
            var _ = CheckAndGetPkm(ps, dodoId, out var msg, out var pkm);
            if (!_)
            {
                DodoBot<T>.SendChannelMessage(msg, channelId);
                return;
            }

            var code = DodoBot<T>.Info.GetRandomTradeCode();
            var text = $"派送:{ShowdownTranslator.GameStrings.Species[pkm.Species]}\n密码:{code:0000 0000}";

            var __ = AddToTradeQueue(pkm, code, ulong.Parse(dodoId), nickName, channelId,
                PokeRoutineType.LinkTrade, out string message);
            DodoBot<T>.SendChannelMessage(message, channelId);
        }


        public static bool CheckAndGetPkm(string setstring, string username, out string msg, out T outPkm)
        {
            outPkm = new T();
            if (!DodoBot<T>.Info.GetCanQueue())
            {
                msg = "Sorry, I am not currently accepting queue requests!";
                return false;
            }

            var set = ShowdownUtil.ConvertToShowdown(setstring);
            if (set == null)
            {
                msg = $"Skipping trade, @{username}: Empty nickname provided for the species.";
                return false;
            }

            var template = AutoLegalityWrapper.GetTemplate(set);
            if (template.Species < 1)
            {
                msg =
                    $"Skipping trade, @{username}: Please read what you are supposed to type as the command argument.";
                return false;
            }

            if (set.InvalidLines.Count != 0)
            {
                msg =
                    $"Skipping trade, @{username}: Unable to parse Showdown Set:\n{string.Join("\n", set.InvalidLines)}";
                return false;
            }

            try
            {
                var sav = AutoLegalityWrapper.GetTrainerInfo<T>();
                var pkm = sav.GetLegal(template, out var result);

                if (!pkm.CanBeTraded())
                {
                    msg = $"Skipping trade, @{username}: Provided Pokemon content is blocked from trading!";
                    return false;
                }

                if (pkm is T pk)
                {
                    var valid = new LegalityAnalysis(pkm).Valid;
                    if (valid)
                    {
                        outPkm = pk;

                        msg =
                            $"@{username} - added to the waiting list. Your request from the waiting list will be removed if you are too slow!";
                        return true;
                    }
                }

                var reason = result == "Timeout"
                    ? "宝可梦创造超时"
                    : "我没办法创造非法宝可梦";
                msg = $"<!@{username}>: {reason}";
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                LogUtil.LogSafe(ex, nameof(DodoBot<T>));
                msg = $"Skipping trade, @{username}: An unexpected problem occurred.";
            }

            return false;
        }

        private static bool AddToTradeQueue(T pk, int code, ulong userId, string name, string channelId,
            PokeRoutineType type, out string msg)
        {
            var trainer = new PokeTradeTrainerInfo(name, userId);
            var notifier = new DodoTradeNotifier<T>(pk, trainer, code, name, channelId);
            var tt = type == PokeRoutineType.SeedCheck ? PokeTradeType.Seed : PokeTradeType.Specific;
            var detail =
                new PokeTradeDetail<T>(pk, trainer, notifier, tt, code, true);
            var trade = new TradeEntry<T>(detail, userId, type, name);

            var added = DodoBot<T>.Info.AddToTradeQueue(trade, userId, false);

            if (added == QueueResultAdd.AlreadyInQueue)
            {
                msg = $"<@!{userId}> 你已经在队列中，请不要重复发送";
                return false;
            }

            var position = DodoBot<T>.Info.CheckPosition(userId, type);
            //msg = $"@{name}: Added to the {type} queue, unique ID: {detail.ID}. Current Position: {position.Position}";
            msg = $" 你在第{position.Position}位";

            var botct = DodoBot<T>.Info.Hub.Bots.Count;
            if (position.Position > botct)
            {
                var eta = DodoBot<T>.Info.Hub.Config.Queues.EstimateDelay(position.Position, botct);
                //msg += $". Estimated: {eta:F1} minutes.";
                msg += $", 需等待约{eta:F1}分钟";
            }

            return true;
        }
    }
}