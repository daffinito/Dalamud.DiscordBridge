using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace Dalamud.DiscordBridge
{
    internal unsafe class FreeCompanyDataReader
    {
        private readonly IObjectTable objectTable;

        public FreeCompanyDataReader(IObjectTable objectTable)
        {
            this.objectTable = objectTable;
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
            public byte ClassJob { get; set; }
            public ushort Level { get; set; }
        }

        private static readonly Dictionary<uint, FCActionInfo> StaticFCActions = new()
        {
            { 0, new FCActionInfo { Id = 0, Name = "The Heat of Battle" } },
            { 1, new FCActionInfo { Id = 1, Name = "The Heat of Battle II" } },
            { 2, new FCActionInfo { Id = 2, Name = "The Heat of Battle III" } },
            { 3, new FCActionInfo { Id = 3, Name = "In Control" } },
            { 4, new FCActionInfo { Id = 4, Name = "In Control II" } },
            { 5, new FCActionInfo { Id = 5, Name = "In Control III" } },
            { 6, new FCActionInfo { Id = 6, Name = "Survival Manual" } },
            { 7, new FCActionInfo { Id = 7, Name = "Survival Manual II" } },
            { 8, new FCActionInfo { Id = 8, Name = "Survival Manual III" } },
            { 9, new FCActionInfo { Id = 9, Name = "Earth and Water" } },
            { 10, new FCActionInfo { Id = 10, Name = "Earth and Water II" } },
            { 11, new FCActionInfo { Id = 11, Name = "What You See" } },
            { 12, new FCActionInfo { Id = 12, Name = "What You See II" } },
            { 13, new FCActionInfo { Id = 13, Name = "Helping Hand" } },
            { 14, new FCActionInfo { Id = 14, Name = "Helping Hand II" } },
            { 15, new FCActionInfo { Id = 15, Name = "Helping Hand III" } },
            { 16, new FCActionInfo { Id = 16, Name = "Back on Your Feet" } },
            { 17, new FCActionInfo { Id = 17, Name = "Back on Your Feet II" } },
            { 18, new FCActionInfo { Id = 18, Name = "Back on Your Feet III" } },
            { 19, new FCActionInfo { Id = 19, Name = "Meat and Mead" } },
            { 20, new FCActionInfo { Id = 20, Name = "Meat and Mead II" } },
            { 21, new FCActionInfo { Id = 21, Name = "That Which Binds Us" } },
            { 22, new FCActionInfo { Id = 22, Name = "That Which Binds Us II" } },
            { 23, new FCActionInfo { Id = 23, Name = "That Which Binds Us III" } },
            { 24, new FCActionInfo { Id = 24, Name = "Seal Sweetener" } },
            { 25, new FCActionInfo { Id = 25, Name = "Seal Sweetener II" } },
            { 26, new FCActionInfo { Id = 26, Name = "Seal Sweetener III" } },
            { 27, new FCActionInfo { Id = 27, Name = "Proper Care" } },
            { 28, new FCActionInfo { Id = 28, Name = "Proper Care II" } },
            { 29, new FCActionInfo { Id = 29, Name = "Live off the Land" } },
            { 30, new FCActionInfo { Id = 30, Name = "A Man's Best Friend" } },
            { 31, new FCActionInfo { Id = 31, Name = "A Man's Best Friend II" } }
        };

        public List<FCActionInfo> GetActiveActions()
        {
            var activeActions = new List<FCActionInfo>();

            if (objectTable.LocalPlayer == null)
                return activeActions;

            var agent = GetFCAgent();
            if (agent == null)
                return activeActions;

            for (int slot = 0; slot < 2; slot++)
            {
                var timeRemaining = agent->ActionTimeRemaining[slot];
                if (timeRemaining > 0)
                {
                    var actionId = FindActiveActionIdInSlot(slot);
                    if (actionId > 0 && StaticFCActions.TryGetValue(actionId, out var action))
                    {
                        activeActions.Add(new FCActionInfo
                        {
                            Id = action.Id,
                            Name = action.Name,
                            TimeRemaining = timeRemaining
                        });
                    }
                }
            }

            return activeActions;
        }

        public List<FCMemberInfo> GetOnlineMembers()
        {
            var members = new List<FCMemberInfo>();

            var agent = GetFCAgent();
            if (agent == null || agent->InfoProxyFreeCompany == null)
                return members;

            var infoProxy = agent->InfoProxyFreeCompany;
            var onlineCount = infoProxy->OnlineMembers;
            var totalCount = infoProxy->TotalMembers;

            Service.Logger.Information($"FC has {totalCount} total members, {onlineCount} online");

            return members;
        }

        private uint FindActiveActionIdInSlot(int slot)
        {
            var player = objectTable.LocalPlayer;
            if (player == null)
                return 0;

            var statusToActionMap = new Dictionary<uint, uint>
            {
                { 414, 0 }, { 50, 1 }, { 51, 2 },
                { 415, 3 }, { 52, 4 }, { 53, 5 },
                { 416, 6 }, { 54, 7 }, { 55, 8 },
                { 56, 9 }, { 57, 10 }, { 58, 11 }, { 59, 12 },
                { 60, 13 }, { 61, 14 }, { 62, 15 },
                { 63, 16 }, { 64, 17 }, { 65, 18 },
                { 66, 19 }, { 67, 20 }, { 68, 21 }, { 69, 22 }, { 70, 23 },
                { 71, 24 }, { 72, 25 }, { 73, 26 },
                { 74, 27 }, { 75, 28 }, { 76, 29 },
                { 77, 30 }, { 78, 31 }
            };

            foreach (var status in player.StatusList)
            {
                if (status == null)
                    continue;

                if (statusToActionMap.TryGetValue(status.StatusId, out var actionId))
                    return actionId;
            }

            return 0;
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
