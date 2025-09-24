using System;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace Dalamud.DiscordBridge
{
    internal class ChatSender
    {
        public unsafe void SendMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                Service.Logger.Warning("Attempted to send an empty or whitespace message to chat.");
                return;
            }

            try
            {
                var uiModule = UIModule.Instance();
                if (uiModule == null)
                {
                    Service.Logger.Error("Could not get UIModule instance.");
                    return;
                }

                using var utf8 = Utf8String.FromString(message);
                if (utf8 == null)
                {
                    Service.Logger.Error("Could not create Utf8String from message.");
                    return;
                }

                uiModule->ProcessChatBoxEntry(utf8);
            }
            catch (Exception ex)
            {
                Service.Logger.Error(ex, "Exception in ChatSender.SendMessage");
            }
        }
    }
}
