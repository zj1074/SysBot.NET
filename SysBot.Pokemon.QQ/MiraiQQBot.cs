using Mirai.Net.Data.Messages.Concretes;
using Mirai.Net.Data.Messages.Receivers;
using Mirai.Net.Sessions;
using Mirai.Net.Sessions.Http.Managers;
using Newtonsoft.Json;
using PKHeX.Core;
using SysBot.Base;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace SysBot.Pokemon.QQ
{
    public class MiraiQQBot<T> where T : PKM, new()
    {
        private const string ConfigPath = "miraiQQconfig.json";
        private static MiraiQQSettings DefaultSettings = new();
        private static PokeTradeHub<T> Hub = default!;

        private readonly MiraiBot client;
        private static string GroupId;
        private static string WelcomeString = "欢迎使用SysBot.NET";
        internal static TradeQueueInfo<T> Info => Hub.Queues.Info;
        internal static readonly List<MiraiQQQueue<T>> QueuePool = new();

        public MiraiQQSettings ReadConfig()
        {
            if (!File.Exists(ConfigPath))
            {
                return DefaultSettings;
            }

            try
            {
                var lines = File.ReadAllText(ConfigPath);
                var cfg = JsonConvert.DeserializeObject<MiraiQQSettings>(lines) ?? DefaultSettings;
                return cfg;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                LogUtil.LogError("miraiQQ config error: {}", "MiraiQQBot");
                return DefaultSettings;
            }
        }

        public MiraiQQBot(PokeTradeHub<T> hub, string message)
        {
            Hub = hub;
            WelcomeString = message;
            var cfg = ReadConfig();

            client = new MiraiBot
            {
                Address = cfg.Address,
                QQ = cfg.QQ,
                VerifyKey = cfg.VerifyKey
            };
            GroupId = cfg.GroupId;
            client.MessageReceived.OfType<GroupMessageReceiver>()
                .Subscribe(receiver =>
                {
                    var senderQQ = receiver.Sender.Id;
                    var groupId = receiver.Sender.Group.Id;
                    if (groupId != GroupId || senderQQ == client.QQ)
                        return;

                    HandleFileUpload(receiver);
                    var response = HandleCommand(receiver);
                    if (response.Length == 0)
                        return;
                });
        }

        public void StartingDistribution()
        {
            Task.Run(async () =>
            {
                await client.LaunchAsync();
                await MessageManager.SendGroupMessageAsync(GroupId, WelcomeString);
                if (typeof(T) == typeof(PK8))
                {
                    await MessageManager.SendGroupMessageAsync(GroupId, "当前版本为剑盾");
                }
                else if (typeof(T) == typeof(PB8))
                {
                    await MessageManager.SendGroupMessageAsync(GroupId, "当前版本为晶灿钻石明亮珍珠");
                }
                else if(typeof(T) == typeof(PA8))
                {
                    await MessageManager.SendGroupMessageAsync(GroupId, "当前版本为阿尔宙斯");
                }
            });
        }

        // todo: revise
        private string HandleFileUpload(GroupMessageReceiver receiver)
        {
            var senderQQ = receiver.Sender.Id;
            var groupId = receiver.Sender.Group.Id;
            var qqMsg = string.Empty;

            var fileMessages = receiver.MessageChain.OfType<FileMessage>();

            if (fileMessages.Any())
            {
                LogUtil.LogText("In file module");
                var file = fileMessages.First();
                var fileName = file.Name;
                string operationType;
                if (typeof(T) == typeof(PK8) &&
                    fileName.EndsWith(".pk8", StringComparison.OrdinalIgnoreCase)) operationType = "pk8";
                else if (typeof(T) == typeof(PB8) &&
                         fileName.EndsWith(".pb8", StringComparison.OrdinalIgnoreCase))
                    operationType = "pb8";
                else if (typeof(T) == typeof(PA8) &&
                         fileName.EndsWith(".pa8", StringComparison.OrdinalIgnoreCase))
                    operationType = "pa8";
                else return qqMsg;

                PKM pkm;
                try
                {
                    var f = FileManager.GetFileAsync(groupId, file.FileId, true).Result;

                    string url = f.DownloadInfo.Url;
                    byte[] data = new System.Net.WebClient().DownloadData(url);
                    switch (operationType)
                    {
                        case "pk8" or "pb8" when data.Length != 344:
                            MessageManager.SendGroupMessageAsync("非法文件");
                            return qqMsg;
                        case "pa8" when data.Length != 376:
                            MessageManager.SendGroupMessageAsync("非法文件");
                            return qqMsg;
                    }

                    switch (operationType)
                    {
                        case "pk8":
                            pkm = new PK8(data);
                            break;
                        case "pb8":
                            pkm = new PB8(data);
                            break;
                        case "pa8":
                            pkm = new PA8(data);
                            break;
                        default: return qqMsg;
                    }

                    LogUtil.LogText($"operationType:{operationType}");
                    FileManager.DeleteFileAsync(groupId, file.FileId);
                }
                catch (Exception ex)
                {
                    LogUtil.LogText(ex.ToString());
                    return qqMsg;
                }

                MessageManager.SendGroupMessageAsync(receiver.Sender.Name + " 上传了 " + fileName + " 文件");
                var _ = MiraiQQCommandsHelper<T>.AddToWaitingList(pkm, receiver.Sender.Name,
                    ulong.Parse(senderQQ), out string msg);
                if (_)
                {
                    GetUserFromQueueAndGenerateCodeToTrade(senderQQ);
                    msg = string.Empty;
                }
                else
                {
                    MessageManager.SendGroupMessageAsync(msg);
                }
            }

            return qqMsg;
        }

        private string HandleCommand(GroupMessageReceiver receiver)
        {
            string qqMsg = string.Empty;
            try
            {
                qqMsg = receiver.MessageChain.OfType<PlainMessage>().First().Text;
            }
            catch
            {
            }

            LogUtil.LogText($"debug qqMsg:{qqMsg}");
            var split = qqMsg.Split('\n');
            string c = "";
            string args = "";
            string nickName = receiver.Sender.Name;
            string qq = receiver.Sender.Id;
            if (split.Length > 0)
            {
                c = split[0];
                args = qqMsg.Substring(qqMsg.IndexOf('\n') + 1);
            }

            switch (c)
            {
                case "$trade":
                    var _ = MiraiQQCommandsHelper<T>.AddToWaitingList(args, nickName, ulong.Parse(qq), out string msg);
                    if (_)
                    {
                        GetUserFromQueueAndGenerateCodeToTrade(qq);
                        msg = string.Empty;
                    }

                    return msg;


                default: return string.Empty;
            }
        }

        private void GetUserFromQueueAndGenerateCodeToTrade(string qq)
        {
            var user = QueuePool.FindLast(q => q.QQ == ulong.Parse(qq));

            //LogUtil.LogInfo("QueuePool.ToString" + QueuePool.ToString(), "debug");
            if (user == null)
                return;
            QueuePool.Remove(user);

            Random rnd = new();
            try
            {
                int code = rnd.Next(0, 9999_9999); //Util.ToInt32(msg);
                var _ = AddToTradeQueue(user.Pokemon, code, user.QQ, user.DisplayName, RequestSignificance.Favored,
                    PokeRoutineType.LinkTrade, out string message);
                MessageManager.SendGroupMessageAsync(GroupId, message);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                LogUtil.LogSafe(ex, nameof(MiraiQQBot<T>));
                LogUtil.LogError($"{ex.Message}", nameof(MiraiQQBot<T>));
            }
        }

        private bool AddToTradeQueue(T pk, int code, ulong qq, string displayName, RequestSignificance sig,
            PokeRoutineType type, out string msg)
        {
            // var user = e.WhisperMessage.UserId;
            var userID = qq;
            var name = displayName;

            var trainer = new PokeTradeTrainerInfo(name, userID);
            var notifier = new MiraiQQTradeNotifier<T>(pk, trainer, code, name, GroupId);
            var tt = type == PokeRoutineType.SeedCheck ? PokeTradeType.Seed : PokeTradeType.Specific;
            var detail = new PokeTradeDetail<T>(pk, trainer, notifier, tt, code, sig == RequestSignificance.Favored);
            var trade = new TradeEntry<T>(detail, userID, type, name);

            var added = Info.AddToTradeQueue(trade, userID, sig == RequestSignificance.Owner);

            if (added == QueueResultAdd.AlreadyInQueue)
            {
                msg = $"@{name}: Sorry, you are already in the queue.";
                return false;
            }

            var position = Info.CheckPosition(userID, type);
            msg = $"@{name}: Added to the {type} queue, unique ID: {detail.ID}. Current Position: {position.Position}";

            var botct = Info.Hub.Bots.Count;
            if (position.Position > botct)
            {
                var eta = Info.Hub.Config.Queues.EstimateDelay(position.Position, botct);
                msg += $". Estimated: {eta:F1} minutes.";
            }

            return true;
        }
    }
}