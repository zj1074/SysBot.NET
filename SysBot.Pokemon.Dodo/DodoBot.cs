using System.Threading.Tasks;
using DoDo.Open.Sdk.Models;
using DoDo.Open.Sdk.Models.Channels;
using DoDo.Open.Sdk.Models.Messages;
using DoDo.Open.Sdk.Models.Personals;
using DoDo.Open.Sdk.Services;
using PKHeX.Core;

namespace SysBot.Pokemon.Dodo
{
    public class DodoBot<T> where T : PKM, new()
    {
        private static PokeTradeHub<T> Hub = default!;
        internal static TradeQueueInfo<T> Info => Hub.Queues.Info;

        public static OpenApiService OpenApiService;

        public DodoBot(DodoSettings settings, PokeTradeHub<T> hub)
        {
            Hub = hub;

            //开放接口服务
            OpenApiService = new OpenApiService(new OpenApiOptions
            {
                BaseApi = settings.BaseApi,
                ClientId = settings.ClientId,
                Token = settings.Token
            });
            //事件处理服务，可自定义，只要继承EventProcessService抽象类即可
            var eventProcessService = new PokemonProcessService<T>(OpenApiService, settings.ChannelId);
            //开放事件服务
            var openEventService = new OpenEventService(OpenApiService, eventProcessService, new OpenEventOptions
            {
                IsReconnect = true,
                IsAsync = true
            });
            //接收事件消息
            Task.Run(async () => { await openEventService.ReceiveAsync(); });
        }

        public static void SendChannelMessage(string message, string channelId)
        {
            if (string.IsNullOrEmpty(message)) return;
            OpenApiService.SetChannelMessageSend(new SetChannelMessageSendInput<MessageBodyText>
            {
                ChannelId = channelId,
                MessageBody = new MessageBodyText
                {
                    Content = message
                }
            });
        }

        public static void SendChannelAtMessage(ulong atDodoId, string message, string channelId)
        {
            if (string.IsNullOrEmpty(message)) return;
            OpenApiService.SetChannelMessageSend(new SetChannelMessageSendInput<MessageBodyText>
            {
                ChannelId = channelId,
                MessageBody = new MessageBodyText
                {
                    Content = $"<@!{atDodoId}> {message}"
                }
            });
        }

        public static void SendPersonalMessage(string dodoId, string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            OpenApiService.SetPersonalMessageSend(new SetPersonalMessageSendInput<MessageBodyText>
            {
                DoDoId = dodoId,
                MessageBody = new MessageBodyText
                {
                    Content = message
                }
            });
        }
    }
}