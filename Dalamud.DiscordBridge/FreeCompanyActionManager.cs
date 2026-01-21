using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;

namespace Dalamud.DiscordBridge
{
    internal unsafe class FreeCompanyActionManager
    {
        private readonly IDataManager dataManager;
        private readonly IGameGui gameGui;
        private readonly IFramework framework;
        private readonly IClientState clientState;

        public FreeCompanyActionManager(IDataManager dataManager, IGameGui gameGui, IFramework framework, IClientState clientState)
        {
            this.dataManager = dataManager;
            this.gameGui = gameGui;
            this.framework = framework;
            this.clientState = clientState;
        }

        public class FCActionInfo
        {
            public uint Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public ushort IconId { get; set; }
            public uint Cost { get; set; }
            public bool IsActive { get; set; }
            public uint TimeRemaining { get; set; }
        }

        public List<FCActionInfo> GetAllActions()
        {
            var actions = new List<FCActionInfo>();
            var sheet = dataManager.GetExcelSheet<FreeCompanyAction>();

            if (sheet == null)
            {
                Service.Logger.Error("Could not load FreeCompanyAction sheet");
                return actions;
            }

            foreach (var action in sheet)
            {
                var name = action.Name.ToString();
                if (string.IsNullOrEmpty(name))
                    continue;

                actions.Add(new FCActionInfo
                {
                    Id = action.RowId,
                    Name = name,
                    Description = action.Description.ToString(),
                    IconId = action.Icon,
                    Cost = action.Cost
                });
            }

            return actions;
        }

        public List<FCActionInfo> GetActiveActions()
        {
            var activeActions = new List<FCActionInfo>();

            if (clientState.LocalPlayer == null)
                return activeActions;

            var agent = GetFCAgent();
            if (agent == null)
                return activeActions;

            var sheet = dataManager.GetExcelSheet<FreeCompanyAction>();
            if (sheet == null)
                return activeActions;

            for (int slot = 0; slot < 2; slot++)
            {
                var timeRemaining = agent->ActionTimeRemaining[slot];
                if (timeRemaining > 0)
                {
                    var statusId = FindActiveActionInSlot(slot);
                    if (statusId > 0)
                    {
                        var action = sheet.FirstOrDefault(a => a.Action.IsValid && a.Action.Value.RowId == statusId);
                        if (action.RowId > 0)
                        {
                            activeActions.Add(new FCActionInfo
                            {
                                Id = action.RowId,
                                Name = action.Name.ToString(),
                                Description = action.Description.ToString(),
                                IconId = action.Icon,
                                Cost = action.Cost,
                                IsActive = true,
                                TimeRemaining = timeRemaining
                            });
                        }
                    }
                }
            }

            return activeActions;
        }

        private uint FindActiveActionInSlot(int slot)
        {
            var player = clientState.LocalPlayer;
            if (player == null)
                return 0;

            var fcStatusIds = new HashSet<uint>
            {
                50, 51, 52, 53, 54, 55, 56, 57, 58, 59,
                60, 61, 62, 63, 64, 65, 66, 67, 68, 69,
                70, 71, 72, 73, 74, 75, 76, 77, 78, 79,
                80, 81
            };

            foreach (var status in player.StatusList)
            {
                if (status == null)
                    continue;

                if (fcStatusIds.Contains(status.StatusId))
                    return status.StatusId;
            }

            return 0;
        }

        public string? ActivateAction(uint actionId)
        {
            if (clientState.LocalPlayer == null)
                return "You must be logged in to activate FC actions.";

            var agent = GetFCAgent();
            if (agent == null || agent->InfoProxyFreeCompany == null)
                return "You must be in a Free Company to activate FC actions.";

            var sheet = dataManager.GetExcelSheet<FreeCompanyAction>();
            if (sheet == null)
                return "Could not load FC action data.";

            var action = sheet.GetRow(actionId);
            if (action.RowId == 0)
                return $"FC Action with ID {actionId} not found.";

            var activeActions = GetActiveActions();
            if (activeActions.Any(a => a.Id == actionId))
                return $"{action.Name} is already active.";

            if (activeActions.Count >= 2)
                return "Maximum of 2 FC actions can be active at once.";

            try
            {
                OpenFCMenu();

                framework.RunOnTick(() =>
                {
                    try
                    {
                        NavigateToActionsTab();

                        framework.RunOnTick(() =>
                        {
                            try
                            {
                                SelectAndActivateAction(actionId);
                            }
                            catch (Exception ex)
                            {
                                Service.Logger.Error(ex, $"Failed to select/activate FC action {actionId}");
                            }
                        }, TimeSpan.FromMilliseconds(150));
                    }
                    catch (Exception ex)
                    {
                        Service.Logger.Error(ex, "Failed to navigate to actions tab");
                    }
                }, TimeSpan.FromMilliseconds(250));

                return null;
            }
            catch (Exception ex)
            {
                Service.Logger.Error(ex, $"Failed to activate FC action {actionId}");
                return $"Failed to activate action: {ex.Message}";
            }
        }

        public string? DeactivateAction(uint actionId)
        {
            if (clientState.LocalPlayer == null)
                return "You must be logged in to deactivate FC actions.";

            var agent = GetFCAgent();
            if (agent == null || agent->InfoProxyFreeCompany == null)
                return "You must be in a Free Company to deactivate FC actions.";

            var activeActions = GetActiveActions();
            if (!activeActions.Any(a => a.Id == actionId))
                return "That FC action is not currently active.";

            try
            {
                OpenFCMenu();

                framework.RunOnTick(() =>
                {
                    try
                    {
                        NavigateToActionsTab();

                        framework.RunOnTick(() =>
                        {
                            try
                            {
                                SelectAndDeactivateAction(actionId);
                            }
                            catch (Exception ex)
                            {
                                Service.Logger.Error(ex, $"Failed to select/deactivate FC action {actionId}");
                            }
                        }, TimeSpan.FromMilliseconds(150));
                    }
                    catch (Exception ex)
                    {
                        Service.Logger.Error(ex, "Failed to navigate to actions tab");
                    }
                }, TimeSpan.FromMilliseconds(250));

                return null;
            }
            catch (Exception ex)
            {
                Service.Logger.Error(ex, $"Failed to deactivate FC action {actionId}");
                return $"Failed to deactivate action: {ex.Message}";
            }
        }

        private AgentFreeCompany* GetFCAgent()
        {
            var agentModule = AgentModule.Instance();
            if (agentModule == null)
                return null;

            return (AgentFreeCompany*)agentModule->GetAgentByInternalId(AgentId.FreeCompany);
        }

        private void OpenFCMenu()
        {
            var agent = (AgentInterface*)gameGui.FindAgentInterface("FreeCompany");
            if (agent == null)
            {
                Service.Logger.Error("Could not find FreeCompany agent");
                return;
            }

            agent->Show();
            Service.Logger.Information("Opened FC menu");
        }

        private void NavigateToActionsTab()
        {
            var addon = gameGui.GetAddonByName("FreeCompany");
            if (addon == IntPtr.Zero)
            {
                Service.Logger.Warning("FC menu addon not found");
                return;
            }

            var unitBase = (AtkUnitBase*)addon;
            if (unitBase == null || !unitBase->IsVisible)
            {
                Service.Logger.Warning("FC menu not visible");
                return;
            }

            var values = stackalloc AtkValue[2];
            values[0].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int;
            values[0].Int = 0;
            values[1].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int;
            values[1].Int = 2;

            unitBase->FireCallback(2, values);
            Service.Logger.Information("Navigated to FC Actions tab");
        }

        private void SelectAndActivateAction(uint actionId)
        {
            var addon = gameGui.GetAddonByName("FreeCompany");
            if (addon == IntPtr.Zero)
            {
                Service.Logger.Warning("FC menu addon not found for activation");
                return;
            }

            var unitBase = (AtkUnitBase*)addon;
            if (unitBase == null || !unitBase->IsVisible)
            {
                Service.Logger.Warning("FC menu not visible for activation");
                return;
            }

            var values = stackalloc AtkValue[2];
            values[0].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int;
            values[0].Int = 3;
            values[1].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int;
            values[1].Int = (int)actionId;

            unitBase->FireCallback(2, values);
            Service.Logger.Information($"Activated FC action {actionId}");

            framework.RunOnTick(() =>
            {
                var confirmAddon = gameGui.GetAddonByName("SelectYesno");
                if (confirmAddon != IntPtr.Zero)
                {
                    var confirmBase = (AtkUnitBase*)confirmAddon;
                    if (confirmBase != null && confirmBase->IsVisible)
                    {
                        var confirmValues = stackalloc AtkValue[1];
                        confirmValues[0].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int;
                        confirmValues[0].Int = 0;
                        confirmBase->FireCallback(1, confirmValues);
                        Service.Logger.Information("Confirmed FC action activation");
                    }
                }
            }, TimeSpan.FromMilliseconds(100));
        }

        private void SelectAndDeactivateAction(uint actionId)
        {
            var addon = gameGui.GetAddonByName("FreeCompany");
            if (addon == IntPtr.Zero)
            {
                Service.Logger.Warning("FC menu addon not found for deactivation");
                return;
            }

            var unitBase = (AtkUnitBase*)addon;
            if (unitBase == null || !unitBase->IsVisible)
            {
                Service.Logger.Warning("FC menu not visible for deactivation");
                return;
            }

            var values = stackalloc AtkValue[2];
            values[0].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int;
            values[0].Int = 4;
            values[1].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int;
            values[1].Int = (int)actionId;

            unitBase->FireCallback(2, values);
            Service.Logger.Information($"Deactivated FC action {actionId}");

            framework.RunOnTick(() =>
            {
                var confirmAddon = gameGui.GetAddonByName("SelectYesno");
                if (confirmAddon != IntPtr.Zero)
                {
                    var confirmBase = (AtkUnitBase*)confirmAddon;
                    if (confirmBase != null && confirmBase->IsVisible)
                    {
                        var confirmValues = stackalloc AtkValue[1];
                        confirmValues[0].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int;
                        confirmValues[0].Int = 0;
                        confirmBase->FireCallback(1, confirmValues);
                        Service.Logger.Information("Confirmed FC action deactivation");
                    }
                }
            }, TimeSpan.FromMilliseconds(100));
        }
    }
}
