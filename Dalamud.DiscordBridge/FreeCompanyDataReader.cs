using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using Lumina.Excel.Sheets;

namespace Dalamud.DiscordBridge
{
    internal unsafe class FreeCompanyDataReader
    {
        public FreeCompanyDataReader()
        {
        }

        public class FCActionInfo
        {
            public uint Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public uint TimeRemaining { get; set; }
        }

        public class FCMemberInfo
        {
            public string Name { get; set; } = string.Empty;
            public ushort Location { get; set; }
        }

        public List<FCActionInfo> GetActiveActions()
        {
            var activeActions = new List<FCActionInfo>();

            var player = Service.ObjectTable.LocalPlayer;
            if (player == null)
                return activeActions;

            var statusSheet = Service.Data.GetExcelSheet<Status>();
            if (statusSheet == null)
                return activeActions;

            var agent = GetFCAgent();
            var slotIndex = 0;

            foreach (var playerStatus in player.StatusList)
            {
                if (playerStatus == null)
                    continue;

                var statusRow = statusSheet.GetRow(playerStatus.StatusId);
                if (statusRow.IsFcBuff)
                {
                    uint timeRemaining = 0;
                    if (agent != null && slotIndex < 3)
                    {
                        var t = agent->ActionTimeRemaining[slotIndex];
                        if (t > 0 && t <= 86400)
                            timeRemaining = t;
                    }

                    activeActions.Add(new FCActionInfo
                    {
                        Id = playerStatus.StatusId,
                        Name = statusRow.Name.ToString(),
                        TimeRemaining = timeRemaining,
                    });
                    slotIndex++;
                }
            }

            return activeActions;
        }

        public List<FCMemberInfo> GetOnlineMembers()
        {
            var members = new List<FCMemberInfo>();

            var agent = GetFCAgent();
            if (agent == null || agent->InfoProxyFreeCompanyMember == null)
                return members;

            var memberProxy = agent->InfoProxyFreeCompanyMember;
            var entryCount = memberProxy->InfoProxyCommonList.InfoProxyPageInterface.InfoProxyInterface.EntryCount;

            Service.Logger.Information($"FC member proxy has {entryCount} entries loaded");

            for (uint i = 0; i < entryCount; i++)
            {
                var entry = memberProxy->InfoProxyCommonList.GetEntry(i);
                if (entry == null)
                    continue;

                if ((entry->State & InfoProxyCommonList.CharacterData.OnlineStatus.Online) == 0)
                    continue;

                members.Add(new FCMemberInfo
                {
                    Name = entry->NameString,
                    Location = entry->Location,
                });
            }

            return members;
        }

        private AgentFreeCompany* GetFCAgent()
        {
            var agentModule = AgentModule.Instance();
            if (agentModule == null)
                return null;

            return (AgentFreeCompany*)agentModule->GetAgentByInternalId(AgentId.FreeCompany);
        }
    }
}
