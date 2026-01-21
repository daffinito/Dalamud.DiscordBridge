using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Hooking;

namespace Dalamud.DiscordBridge
{
    internal unsafe class FreeCompanyActionManager : IDisposable
    {
        private readonly IGameGui gameGui;
        private readonly IFramework framework;
        private readonly IObjectTable objectTable;
        private readonly IGameInteropProvider interop;

        private delegate void FireCallbackDelegate(AtkUnitBase* unitBase, int valueCount, AtkValue* values, byte updateState);
        private Hook<FireCallbackDelegate>? fireCallbackHook;

        public FreeCompanyActionManager(IDataManager dataManager, IGameGui gameGui, IFramework framework, IObjectTable objectTable, IGameInteropProvider interop)
        {
            this.gameGui = gameGui;
            this.framework = framework;
            this.objectTable = objectTable;
            this.interop = interop;

            InitializeCallbackHook();
        }

        private void InitializeCallbackHook()
        {
            try
            {
                var fireCallbackAddress = (nint)AtkUnitBase.Addresses.FireCallback.Value;
                if (fireCallbackAddress != IntPtr.Zero)
                {
                    fireCallbackHook = interop.HookFromAddress<FireCallbackDelegate>(fireCallbackAddress, FireCallbackDetour);
                    fireCallbackHook?.Enable();
                    Service.Logger.Information("FC Action callback hook initialized successfully");
                }
                else
                {
                    Service.Logger.Warning("Could not find FireCallback address for hooking");
                }
            }
            catch (Exception ex)
            {
                Service.Logger.Error(ex, "Failed to initialize callback hook");
            }
        }

        private void FireCallbackDetour(AtkUnitBase* unitBase, int valueCount, AtkValue* values, byte updateState)
        {
            try
            {
                if (unitBase != null)
                {
                    var addonName = unitBase->NameString;

                    if (addonName == "FreeCompany" ||
                        addonName.Contains("Context") ||
                        addonName.Contains("Menu") ||
                        addonName == "SelectYesno")
                    {
                        Service.Logger.Information("=== CALLBACK DETECTED ===");
                        Service.Logger.Information($"Addon: {addonName}, ValueCount: {valueCount}, UpdateState: {updateState}");

                        for (int i = 0; i < valueCount && i < 10; i++)
                        {
                            var value = values[i];
                            Service.Logger.Information($"  Value[{i}]: Type={value.Type}, Int={value.Int}, UInt={value.UInt}");
                        }

                        Service.Logger.Information("=========================");
                    }
                }
            }
            catch (Exception ex)
            {
                Service.Logger.Error(ex, "Error in callback detour");
            }

            fireCallbackHook!.Original(unitBase, valueCount, values, updateState);
        }

        public void Dispose()
        {
            fireCallbackHook?.Dispose();
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

        private static readonly Dictionary<uint, FCActionInfo> StaticFCActions = new()
        {
            { 0, new FCActionInfo { Id = 0, Name = "The Heat of Battle", Description = "Increases EXP earned by 5%", IconId = 60801, Cost = 240 } },
            { 1, new FCActionInfo { Id = 1, Name = "The Heat of Battle II", Description = "Increases EXP earned by 10%", IconId = 60802, Cost = 360 } },
            { 2, new FCActionInfo { Id = 2, Name = "The Heat of Battle III", Description = "Increases EXP earned by 15%", IconId = 60803, Cost = 480 } },
            { 3, new FCActionInfo { Id = 3, Name = "In Control", Description = "Increases control by 5%", IconId = 60810, Cost = 240 } },
            { 4, new FCActionInfo { Id = 4, Name = "In Control II", Description = "Increases control by 10%", IconId = 60811, Cost = 360 } },
            { 5, new FCActionInfo { Id = 5, Name = "In Control III", Description = "Increases control by 15%", IconId = 60812, Cost = 480 } },
            { 6, new FCActionInfo { Id = 6, Name = "Survival Manual", Description = "Increases gathering EXP by 5%", IconId = 60804, Cost = 240 } },
            { 7, new FCActionInfo { Id = 7, Name = "Survival Manual II", Description = "Increases gathering EXP by 10%", IconId = 60805, Cost = 360 } },
            { 8, new FCActionInfo { Id = 8, Name = "Survival Manual III", Description = "Increases gathering EXP by 15%", IconId = 60806, Cost = 480 } },
            { 9, new FCActionInfo { Id = 9, Name = "Earth and Water", Description = "Reduces gathering attempts by 1", IconId = 60807, Cost = 240 } },
            { 10, new FCActionInfo { Id = 10, Name = "Earth and Water II", Description = "Reduces gathering attempts by 2", IconId = 60808, Cost = 360 } },
            { 11, new FCActionInfo { Id = 11, Name = "What You See", Description = "Increases gathering attempts by 1", IconId = 60809, Cost = 240 } },
            { 12, new FCActionInfo { Id = 12, Name = "What You See II", Description = "Increases gathering attempts by 2", IconId = 60819, Cost = 360 } },
            { 13, new FCActionInfo { Id = 13, Name = "Helping Hand", Description = "Increases crafting progress by 5%", IconId = 60813, Cost = 240 } },
            { 14, new FCActionInfo { Id = 14, Name = "Helping Hand II", Description = "Increases crafting progress by 10%", IconId = 60814, Cost = 360 } },
            { 15, new FCActionInfo { Id = 15, Name = "Helping Hand III", Description = "Increases crafting progress by 15%", IconId = 60815, Cost = 480 } },
            { 16, new FCActionInfo { Id = 16, Name = "Back on Your Feet", Description = "Reduces durability loss by 5", IconId = 60816, Cost = 240 } },
            { 17, new FCActionInfo { Id = 17, Name = "Back on Your Feet II", Description = "Reduces durability loss by 10", IconId = 60817, Cost = 360 } },
            { 18, new FCActionInfo { Id = 18, Name = "Back on Your Feet III", Description = "Reduces durability loss by 15", IconId = 60818, Cost = 480 } },
            { 19, new FCActionInfo { Id = 19, Name = "Meat and Mead", Description = "Reduces teleportation costs by 10%", IconId = 60820, Cost = 240 } },
            { 20, new FCActionInfo { Id = 20, Name = "Meat and Mead II", Description = "Reduces teleportation costs by 20%", IconId = 60821, Cost = 360 } },
            { 21, new FCActionInfo { Id = 21, Name = "That Which Binds Us", Description = "Increases spiritbond gain by 10%", IconId = 60822, Cost = 240 } },
            { 22, new FCActionInfo { Id = 22, Name = "That Which Binds Us II", Description = "Increases spiritbond gain by 20%", IconId = 60823, Cost = 360 } },
            { 23, new FCActionInfo { Id = 23, Name = "That Which Binds Us III", Description = "Reduces gear spiritbond requirement", IconId = 60824, Cost = 480 } },
            { 24, new FCActionInfo { Id = 24, Name = "Seal Sweetener", Description = "Increases GC seals by 10%", IconId = 60825, Cost = 240 } },
            { 25, new FCActionInfo { Id = 25, Name = "Seal Sweetener II", Description = "Increases GC seals by 15%", IconId = 60826, Cost = 360 } },
            { 26, new FCActionInfo { Id = 26, Name = "Seal Sweetener III", Description = "Increases GC seals by 20%", IconId = 60827, Cost = 480 } },
            { 27, new FCActionInfo { Id = 27, Name = "Proper Care", Description = "Reduces gear degradation by 10%", IconId = 60828, Cost = 240 } },
            { 28, new FCActionInfo { Id = 28, Name = "Proper Care II", Description = "Reduces gear degradation by 20%", IconId = 60829, Cost = 360 } },
            { 29, new FCActionInfo { Id = 29, Name = "Live off the Land", Description = "Increases enmity generation for tanks", IconId = 60830, Cost = 240 } },
            { 30, new FCActionInfo { Id = 30, Name = "A Man's Best Friend", Description = "Increases chocobo EXP by 5%", IconId = 60831, Cost = 240 } },
            { 31, new FCActionInfo { Id = 31, Name = "A Man's Best Friend II", Description = "Increases chocobo EXP by 10%", IconId = 60832, Cost = 360 } }
        };

        public List<FCActionInfo> GetAllActions()
        {
            return StaticFCActions.Values.ToList();
        }

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
                            Description = action.Description,
                            IconId = action.IconId,
                            Cost = action.Cost,
                            IsActive = true,
                            TimeRemaining = timeRemaining
                        });
                    }
                }
            }

            return activeActions;
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

        public string? ActivateAction(uint actionId)
        {
            if (objectTable.LocalPlayer == null)
                return "You must be logged in to activate FC actions.";

            var agent = GetFCAgent();
            if (agent == null || agent->InfoProxyFreeCompany == null)
                return "You must be in a Free Company to activate FC actions.";

            if (!StaticFCActions.TryGetValue(actionId, out var action))
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
                    SwitchToActionsTab();

                    framework.RunOnTick(() =>
                    {
                        ClickAction(actionId);

                        framework.RunOnTick(() =>
                        {
                            ExecuteActionFromContextMenu();

                            framework.RunOnTick(() =>
                            {
                                ConfirmYes();
                            }, TimeSpan.FromMilliseconds(300));
                        }, TimeSpan.FromMilliseconds(500));
                    }, TimeSpan.FromMilliseconds(800));
                }, TimeSpan.FromMilliseconds(800));

                return null;
            }
            catch (Exception ex)
            {
                Service.Logger.Error(ex, $"Failed to open FC menu for action {actionId}");
                return $"Failed to open FC menu: {ex.Message}";
            }
        }

        public string? DeactivateAction(uint actionId)
        {
            if (objectTable.LocalPlayer == null)
                return "You must be logged in to deactivate FC actions.";

            var agent = GetFCAgent();
            if (agent == null || agent->InfoProxyFreeCompany == null)
                return "You must be in a Free Company to deactivate FC actions.";

            var activeActions = GetActiveActions();
            var action = activeActions.FirstOrDefault(a => a.Id == actionId);
            if (action == null)
                return "That FC action is not currently active.";

            try
            {
                OpenFCMenu();

                Service.Logger.Information("=== CALLBACK LOGGING ACTIVE ===");
                Service.Logger.Information($"FC menu opened. Now manually:");
                Service.Logger.Information($"1. Click the 'Company Actions' tab");
                Service.Logger.Information($"2. Right-click on action: {action.Name} (ID: {actionId})");
                Service.Logger.Information($"3. Select 'Deactivate' and confirm");
                Service.Logger.Information("All callbacks will be logged! Check /xllog after completing these steps.");
                Service.Logger.Information("===============================");

                return null;
            }
            catch (Exception ex)
            {
                Service.Logger.Error(ex, $"Failed to open FC menu for deactivation {actionId}");
                return $"Failed to open FC menu: {ex.Message}";
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
            var addonWrapper = gameGui.GetAddonByName("FreeCompany");
            if (addonWrapper.Address != IntPtr.Zero)
            {
                var unitBase = (AtkUnitBase*)addonWrapper.Address;
                if (unitBase != null && unitBase->IsVisible)
                {
                    Service.Logger.Information("FC menu already open, skipping Show()");
                    return;
                }
            }

            var fcAgent = GetFCAgent();
            if (fcAgent == null)
            {
                Service.Logger.Error("Could not find FreeCompany agent");
                return;
            }

            var agentInterface = (AgentInterface*)fcAgent;
            agentInterface->Show();
            Service.Logger.Information("Opened FC menu");
        }

        private void SwitchToActionsTab()
        {
            var addonWrapper = gameGui.GetAddonByName("FreeCompany");
            if (addonWrapper.Address == IntPtr.Zero)
            {
                Service.Logger.Warning("FC menu addon not found for tab switch");
                return;
            }

            var unitBase = (AtkUnitBase*)addonWrapper.Address;
            if (unitBase == null || !unitBase->IsVisible)
            {
                Service.Logger.Warning("FC menu not visible for tab switch");
                return;
            }

            Service.Logger.Information("=== PLEASE MANUALLY CLICK THE ACTIONS TAB ===");
            Service.Logger.Information("The callback hook will log the correct pattern we need to use.");
            Service.Logger.Information("After clicking, check the logs for the FreeCompany callback.");
        }

        private void ClickAction(uint actionId)
        {
            var addonWrapper = gameGui.GetAddonByName("FreeCompany");
            if (addonWrapper.Address == IntPtr.Zero)
            {
                Service.Logger.Warning("FC menu addon not found for action click");
                return;
            }

            var unitBase = (AtkUnitBase*)addonWrapper.Address;
            if (unitBase == null || !unitBase->IsVisible)
            {
                Service.Logger.Warning("FC menu not visible for action click");
                return;
            }

            var values = stackalloc AtkValue[2];
            values[0].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int;
            values[0].Int = 0;
            values[1].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt;
            values[1].UInt = actionId;

            unitBase->FireCallback(3, values);
            Service.Logger.Information($"Clicked action {actionId} (callback 3, [0, {actionId}])");
        }

        private void ExecuteActionFromContextMenu()
        {
            var contextWrapper = gameGui.GetAddonByName("ContextMenu");
            if (contextWrapper.Address == IntPtr.Zero)
            {
                Service.Logger.Warning("ContextMenu addon not found");
                return;
            }

            var unitBase = (AtkUnitBase*)contextWrapper.Address;
            if (unitBase == null || !unitBase->IsVisible)
            {
                Service.Logger.Warning("ContextMenu not visible");
                return;
            }

            var values = stackalloc AtkValue[5];
            values[0].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int;
            values[0].Int = 0;
            values[1].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int;
            values[1].Int = 0;
            values[2].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt;
            values[2].UInt = 0;
            values[3].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Undefined;
            values[4].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Undefined;

            unitBase->FireCallback(5, values);
            Service.Logger.Information("Executed action from context menu (callback 5, [0, 0, 0, undefined, undefined])");
        }

        private void ConfirmYes()
        {
            var yesnoWrapper = gameGui.GetAddonByName("SelectYesno");
            if (yesnoWrapper.Address == IntPtr.Zero)
            {
                Service.Logger.Warning("SelectYesno addon not found");
                return;
            }

            var unitBase = (AtkUnitBase*)yesnoWrapper.Address;
            if (unitBase == null || !unitBase->IsVisible)
            {
                Service.Logger.Warning("SelectYesno not visible");
                return;
            }

            var values = stackalloc AtkValue[1];
            values[0].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int;
            values[0].Int = 0;

            unitBase->FireCallback(1, values);
            Service.Logger.Information("Confirmed Yes (callback 1, [0])");
        }

        private void NavigateToActionsTab()
        {
            var addonWrapper = gameGui.GetAddonByName("FreeCompany");
            Service.Logger.Information($"GetAddonByName('FreeCompany') returned address: {addonWrapper.Address:X}");

            if (addonWrapper.Address == IntPtr.Zero)
            {
                Service.Logger.Warning("FC menu addon not found - trying alternate names...");

                var alternateNames = new[] { "FreeCompanyProfile", "FreeCompanyProfileCard", "FreeCompanyActions" };
                foreach (var name in alternateNames)
                {
                    var altWrapper = gameGui.GetAddonByName(name);
                    Service.Logger.Information($"Trying addon name '{name}': {altWrapper.Address:X}");
                    if (altWrapper.Address != IntPtr.Zero)
                    {
                        Service.Logger.Information($"Found addon with name: {name}");
                        addonWrapper = altWrapper;
                        break;
                    }
                }

                if (addonWrapper.Address == IntPtr.Zero)
                {
                    Service.Logger.Error("Could not find FC menu addon with any known name");
                    return;
                }
            }

            var unitBase = (AtkUnitBase*)addonWrapper.Address;
            if (unitBase == null || !unitBase->IsVisible)
            {
                Service.Logger.Warning($"FC menu not visible. UnitBase null: {unitBase == null}, IsVisible: {(unitBase != null ? unitBase->IsVisible : false)}");
                return;
            }

            Service.Logger.Information($"UnitBase Id: {unitBase->Id}, Name: {unitBase->NameString}, IsReady: {unitBase->IsReady}");
            Service.Logger.Information("FC menu opened successfully.");
            Service.Logger.Information("=== MANUAL ACTIVATION REQUIRED ===");
            Service.Logger.Information($"Please manually activate FC Action ID {0} by:");
            Service.Logger.Information("1. Clicking the 'Company Actions' tab");
            Service.Logger.Information("2. Selecting the desired action from the list");
            Service.Logger.Information("3. Clicking the 'Activate' button");
            Service.Logger.Information("=================================");
        }

        private void SelectAndActivateAction(uint actionId)
        {
            var addonWrapper = gameGui.GetAddonByName("FreeCompany");
            if (addonWrapper.Address == IntPtr.Zero)
            {
                Service.Logger.Warning("FC menu addon not found for activation");
                return;
            }

            var unitBase = (AtkUnitBase*)addonWrapper.Address;
            if (unitBase == null || !unitBase->IsVisible)
            {
                Service.Logger.Warning("FC menu not visible for activation");
                return;
            }

            Service.Logger.Information($"*** Manual action required ***");
            Service.Logger.Information($"The FC menu is now open. Please manually:");
            Service.Logger.Information($"1. Click the 'Company Actions' tab");
            Service.Logger.Information($"2. Select action ID {actionId} from the list");
            Service.Logger.Information($"3. Click the 'Activate' button");
            Service.Logger.Information("Automatic UI interaction requires reverse-engineering the exact callback structure for this addon.");
            Service.Logger.Information("This varies by game version and is not currently implemented.");

            framework.RunOnTick(() =>
            {
                var confirmAddonWrapper = gameGui.GetAddonByName("SelectYesno");
                if (confirmAddonWrapper.Address != IntPtr.Zero)
                {
                    var confirmBase = (AtkUnitBase*)confirmAddonWrapper.Address;
                    if (confirmBase != null && confirmBase->IsVisible)
                    {
                        confirmBase->FireCallbackInt(0);
                        Service.Logger.Information("Confirmed FC action (clicked Yes)");
                    }
                }
            }, TimeSpan.FromMilliseconds(100));
        }

        private void SelectAndDeactivateAction(uint actionId)
        {
            var addonWrapper = gameGui.GetAddonByName("FreeCompany");
            if (addonWrapper.Address == IntPtr.Zero)
            {
                Service.Logger.Warning("FC menu addon not found for deactivation");
                return;
            }

            var unitBase = (AtkUnitBase*)addonWrapper.Address;
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
                var confirmAddonWrapper = gameGui.GetAddonByName("SelectYesno");
                if (confirmAddonWrapper.Address != IntPtr.Zero)
                {
                    var confirmBase = (AtkUnitBase*)confirmAddonWrapper.Address;
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
