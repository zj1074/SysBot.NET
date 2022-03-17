using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DoDo.Open.Sdk.Models.Bots;
using DoDo.Open.Sdk.Models.Channels;
using DoDo.Open.Sdk.Models.Events;
using DoDo.Open.Sdk.Models.Islands;
using DoDo.Open.Sdk.Models.Members;
using DoDo.Open.Sdk.Models.Messages;
using DoDo.Open.Sdk.Models.Personals;
using DoDo.Open.Sdk.Models.Resources;
using DoDo.Open.Sdk.Models.Roles;
using DoDo.Open.Sdk.Models.WebSockets;
using DoDo.Open.Sdk.Services;
using PKHeX.Core;
using SysBot.Base;

namespace SysBot.Pokemon.Dodo
{
    public class PokemonProcessService<TP> : EventProcessService where TP : PKM, new()
    {
        private readonly OpenApiService _openApiService;
        private static string LogIdentity = "DodoBot";
        private static string BotDodoId = "0";
        private static string Welcome = "at我并尝试对我说：\n头目异色怕寂寞性格98级母怪力\n六尾阿罗拉的样子形态\n未知图腾Y形态";

        public PokemonProcessService(OpenApiService openApiService)
        {
            _openApiService = openApiService;
            var output = _openApiService.GetBotInfo(new GetBotInfoInput());
            if (output != null)
            {
                BotDodoId = output.DodoId;
            }
        }

        public override void Connected(string message)
        {
            Console.WriteLine($"{message}\n");
        }

        public override void Disconnected(string message)
        {
            Console.WriteLine($"{message}\n");
        }

        public override void Reconnected(string message)
        {
            Console.WriteLine($"{message}\n");
        }

        public override void Exception(string message)
        {
            Console.WriteLine($"{message}\n");
        }

        public override void PersonalMessageEvent<T>(
            EventSubjectOutput<EventSubjectDataBusiness<EventBodyPersonalMessage<T>>> input)
        {
            var eventBody = input.Data.EventBody;

            if (eventBody.MessageBody is MessageBodyText messageBodyText)
            {
                var messageBody = messageBodyText;

                _openApiService.SetPersonalMessageSend(new SetPersonalMessageSendInput<MessageBodyText>
                {
                    DoDoId = eventBody.DodoId,
                    MessageBody = new MessageBodyText
                    {
                        Content = "触发个人消息事件-文本"
                    }
                });

                _openApiService.SetPersonalMessageSend(new SetPersonalMessageSendInput<MessageBodyText>
                {
                    DoDoId = eventBody.DodoId,
                    MessageBody = messageBody
                });
            }
            else if (eventBody.MessageBody is MessageBodyPicture messageBodyPicture)
            {
                var messageBody = messageBodyPicture;

                _openApiService.SetPersonalMessageSend(new SetPersonalMessageSendInput<MessageBodyText>
                {
                    DoDoId = eventBody.DodoId,
                    MessageBody = new MessageBodyText
                    {
                        Content = "触发个人消息事件-图片"
                    }
                });

                _openApiService.SetPersonalMessageSend(new SetPersonalMessageSendInput<MessageBodyPicture>
                {
                    DoDoId = eventBody.DodoId,
                    MessageBody = messageBody
                });
            }
            else if (eventBody.MessageBody is MessageBodyVideo messageBodyVideo)
            {
                var messageBody = messageBodyVideo;

                _openApiService.SetPersonalMessageSend(new SetPersonalMessageSendInput<MessageBodyText>
                {
                    DoDoId = eventBody.DodoId,
                    MessageBody = new MessageBodyText
                    {
                        Content = "触发个人消息事件-视频"
                    }
                });

                _openApiService.SetPersonalMessageSend(new SetPersonalMessageSendInput<MessageBodyVideo>
                {
                    DoDoId = eventBody.DodoId,
                    MessageBody = messageBody
                });
            }
        }

        public override void ChannelMessageEvent<T>(
            EventSubjectOutput<EventSubjectDataBusiness<EventBodyChannelMessage<T>>> input)
        {
            var eventBody = input.Data.EventBody;

            if (eventBody.MessageBody is MessageBodyText messageBodyText)
            {
                var messageBody = messageBodyText;

                var content = messageBody.Content;

                Console.WriteLine($"\n【{content}】");
                LogUtil.LogInfo($"{eventBody.Personal.NickName}({eventBody.DodoId}):{content}", LogIdentity);
                if (!content.Contains($"<@!{BotDodoId}>")) return;
                //DodoBot<TP>.SendChannelMessage($"{eventBody.DodoId} {eventBody.Personal.NickName}", eventBody.ChannelId);
                var ps = ShowdownTranslator.Chinese2Showdown(content);
                if (!string.IsNullOrWhiteSpace(ps))
                {
                    LogUtil.LogInfo($"收到命令\n{ps}", LogIdentity);
                    DodoHelper<TP>.dummy(ps, eventBody.DodoId, eventBody.Personal.NickName, eventBody.ChannelId);
                }
                else if (content.Contains("取消"))
                {
                    var result = DodoBot<TP>.Info.ClearTrade(ulong.Parse(eventBody.DodoId));
                    DodoBot<TP>.SendChannelAtMessage(ulong.Parse(eventBody.DodoId), $" {GetClearTradeMessage(result)}",
                        eventBody.ChannelId);
                }
                else if (content.Contains("位置"))
                {
                    var result = DodoBot<TP>.Info.CheckPosition(ulong.Parse(eventBody.DodoId));
                    DodoBot<TP>.SendChannelAtMessage(ulong.Parse(eventBody.DodoId),
                        $" {GetQueueCheckResultMessage(result)}",
                        eventBody.ChannelId);
                }
                else
                {
                    DodoBot<TP>.SendChannelMessage($"{Welcome}", eventBody.ChannelId);
                }
            }
        }

        public string GetQueueCheckResultMessage(QueueCheckResult<TP> result)
        {
            if (!result.InQueue || result.Detail is null)
                return "你不在队列里";
            var msg = $"你在第{result.Position}位";
            var pk = result.Detail.Trade.TradeData;
            if (pk.Species != 0)
                msg += $"，交换宝可梦：{ShowdownTranslator.GameStrings.Species[result.Detail.Trade.TradeData.Species]}";
            return msg;
        }

        private static string GetClearTradeMessage(QueueResultRemove result)
        {
            return result switch
            {
                QueueResultRemove.CurrentlyProcessing => "你正在交换中",
                QueueResultRemove.CurrentlyProcessingRemoved => "正在删除",
                QueueResultRemove.Removed => "已删除",
                _ => "你不在队列里",
            };
        }

        public override void MessageReactionEvent(
            EventSubjectOutput<EventSubjectDataBusiness<EventBodyMessageReaction>> input)
        {
            var eventBody = input.Data.EventBody;

            _openApiService.SetChannelMessageSend(new SetChannelMessageSendInput<MessageBodyText>
            {
                ChannelId = eventBody.ChannelId,
                MessageBody = new MessageBodyText
                {
                    Content = "触发消息反应事件"
                }
            });

            var reply = "";

            reply += $"反应对象类型：{eventBody.ReactionTarget.Type}\n";
            reply += $"反应对象ID：{eventBody.ReactionTarget.Id}\n";
            reply += $"反应表情类型：{eventBody.ReactionEmoji.Type}\n";
            reply += $"反应表情ID：{eventBody.ReactionEmoji.Id}\n";
            reply += $"反应类型：{eventBody.ReactionType}\n";

            _openApiService.SetChannelMessageSend(new SetChannelMessageSendInput<MessageBodyText>
            {
                ChannelId = eventBody.ChannelId,
                MessageBody = new MessageBodyText
                {
                    Content = reply
                }
            });
        }
    }
}