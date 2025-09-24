using System;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
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

            Utf8String* utf8Message = null;
            try
            {
                var uiModule = UIModule.Instance();
                if (uiModule == null)
                {
                    Service.Logger.Error("Could not get UIModule instance.");
                    return;
                }

                utf8Message = Utf8String.FromString(message);
                if (utf8Message == null)
                {
                    Service.Logger.Error("Could not create Utf8String from message.");
                    return;
                }

                uiModule->ProcessChatBoxEntry(utf8Message);
            }
            catch (Exception ex)
            {
                Service.Logger.Error(ex, "Exception in ChatSender.SendMessage");
            }
            finally
            {
                if (utf8Message != null)
                {
                    utf8Message->Dtor();
                    IMemorySpace.Free(utf8Message);
                }
            }
        }
    }
}

