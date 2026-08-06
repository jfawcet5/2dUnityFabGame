using BeyProject.Data;
using UnityEditor;
using UnityEngine;

namespace BeyProject.EditorTools
{
    public static partial class ProjectBootstrapper
    {
        private struct ItemSet
        {
            public ItemDefinition wafer;
            public ItemDefinition materialSample;
            public ItemDefinition calibrationTool;
            public ItemDefinition cleanroomKeycard;
            public ItemDefinition lithographyMask;
            public ItemDefinition recipeFile;
            public ItemDefinition processModule;
            public ItemDefinition experimentalComponent;
            public ItemDefinition prototypeAccessBadge;

            public ItemDefinition internalEmail;
            public ItemDefinition engineerNotes;
            public ItemDefinition failedExperimentLog;
            public ItemDefinition manufacturingReport;
            public ItemDefinition prototypeDocumentation;
            public ItemDefinition maintenancePass;

            public ItemDefinition powerComponent;
            public ItemDefinition memoryModule;
            public ItemDefinition parallelProcessingModule;
            public ItemDefinition focusingAlgorithmModule;
            public ItemDefinition predictiveTargetingModule;
            public ItemDefinition coolingLayer;
            public ItemDefinition siliconWafer;

            public ItemDefinition overclockLayer;
            public ItemDefinition capacitorBank;
            public ItemDefinition streamlinedCache;
            public ItemDefinition cascadeProcessor;
        }

        private struct DialogueSet
        {
            public DialogueSequence receptionist;
            public DialogueSequence floorSupervisor;
            public DialogueSequence supplyCabinet;
            public DialogueSequence technicianFirstMeeting;
            public DialogueSequence technicianRepeat;
            public DialogueSequence recipeTerminal;

            public DialogueSequence offDutyEngineer;
            public DialogueSequence whiteboard;
            public DialogueSequence passingTechnicianDefault;
            public DialogueSequence passingTechnicianWithPass;
            public DialogueSequence toolRack;
            public DialogueSequence oldAutomationUnit;
            public DialogueSequence disabledTerminalLocked;
            public DialogueSequence disabledTerminalUnlocked;

            public DialogueSequence briefingTerminal;
            public DialogueSequence componentScanner;

            public DialogueSequence loreTerminalThermal;
            public DialogueSequence loreTerminalArchitecture;
            public DialogueSequence maintenanceBayTerminal;
            public DialogueSequence hiddenCache;
        }

        private static ItemSet BuildItems()
        {
            return new ItemSet
            {
                wafer = CreateItemDefinition("Item_Wafer", "wafer_01", "Wafer",
                    "A thin disc of ultra-pure silicon - the base material for everything made here.",
                    ItemCategory.Material, false, new Color(0.75f, 0.8f, 0.85f)),

                materialSample = CreateItemDefinition("Item_MaterialSample", "material_sample_01", "Material Sample",
                    "A sealed sample pulled from the materials lab for reference.",
                    ItemCategory.Material, false, new Color(0.3f, 0.7f, 0.65f)),

                calibrationTool = CreateItemDefinition("Item_CalibrationTool", "calibration_tool_01", "Calibration Tool",
                    "Used to calibrate sensitive fab equipment. Tucked away and easy to miss.",
                    ItemCategory.Tool, false, new Color(0.9f, 0.55f, 0.2f)),

                cleanroomKeycard = CreateItemDefinition("Item_CleanroomKeycard", "cleanroom_keycard_01", "Cleanroom Keycard",
                    "Grants access to the cleanroom-controlled areas of the facility.",
                    ItemCategory.KeyItem, true, new Color(0.5f, 0.85f, 0.95f)),

                lithographyMask = CreateItemDefinition("Item_LithographyMask", "lithography_mask_01", "Lithography Mask",
                    "A patterned photomask used to project circuit designs onto a wafer.",
                    ItemCategory.Document, false, new Color(0.6f, 0.4f, 0.8f)),

                recipeFile = CreateItemDefinition("Item_RecipeFile", "recipe_file_01", "Recipe File",
                    "An old process recipe pulled from a lithography terminal.",
                    ItemCategory.Document, false, new Color(0.7f, 0.8f, 0.3f)),

                processModule = CreateItemDefinition("Item_ProcessModule", "process_module_01", "Process Module",
                    "A modular process unit salvaged from the lithography floor.",
                    ItemCategory.Component, false, new Color(0.3f, 0.5f, 0.9f)),

                experimentalComponent = CreateItemDefinition("Item_ExperimentalComponent", "experimental_component_01", "Experimental Component",
                    "An unlabeled component of unknown origin. Handle carefully.",
                    ItemCategory.Component, false, new Color(0.85f, 0.3f, 0.75f)),

                prototypeAccessBadge = CreateItemDefinition("Item_PrototypeAccessBadge", "prototype_access_badge_01", "Prototype Access Badge",
                    "A badge for a restricted area that isn't accessible yet. Might matter later.",
                    ItemCategory.KeyItem, true, new Color(0.95f, 0.8f, 0.25f)),

                internalEmail = CreateItemDefinition("Item_InternalEmail", "internal_email_01", "Internal Email",
                    "A printed email thread about scheduling. Mundane, but it paints a picture of daily life here.",
                    ItemCategory.Document, false, new Color(0.75f, 0.75f, 0.8f)),

                engineerNotes = CreateItemDefinition("Item_EngineerNotes", "engineer_notes_01", "Engineer Notes",
                    "Handwritten notes, tucked away where a supervisor wouldn't find them.",
                    ItemCategory.Document, false, new Color(0.8f, 0.7f, 0.5f)),

                failedExperimentLog = CreateItemDefinition("Item_FailedExperimentLog", "failed_experiment_log_01", "Failed Experiment Log",
                    "A log of an experiment that didn't go as planned. Someone annotated it with 'do not repeat.'",
                    ItemCategory.Document, false, new Color(0.8f, 0.4f, 0.4f)),

                manufacturingReport = CreateItemDefinition("Item_ManufacturingReport", "manufacturing_report_01", "Manufacturing Report",
                    "A yield report pulled from a terminal that shouldn't have still been running.",
                    ItemCategory.Document, false, new Color(0.5f, 0.6f, 0.75f)),

                prototypeDocumentation = CreateItemDefinition("Item_PrototypeDocumentation", "prototype_documentation_01", "Prototype Documentation",
                    "Documentation for a prototype process. Most of it is redacted.",
                    ItemCategory.Document, false, new Color(0.65f, 0.5f, 0.75f)),

                maintenancePass = CreateItemDefinition("Item_MaintenancePass", "maintenance_pass_01", "Maintenance Pass",
                    "Grants access to maintenance-restricted areas of the facility.",
                    ItemCategory.KeyItem, true, new Color(0.9f, 0.6f, 0.3f)),

                // Every module below is a trade, not an upgrade. There is deliberately no
                // module that is strictly better than the baseline chip - if there were, the
                // Fabrication screen would be a checklist rather than a decision.
                powerComponent = CreateChipModuleItem("Item_PowerComponent", "power_component_01", "Power Component",
                    "A high-density power cell. Far more total energy, but the extra mass slows you down.",
                    new Color(0.95f, 0.75f, 0.2f), ChipSlotType.Battery, batteryBonus: 60,
                    moveSpeedMultiplier: 0.85f,
                    chipTradeoffDescription: "-15% move speed."),

                memoryModule = CreateChipModuleItem("Item_MemoryModule", "memory_module_01", "Memory Module",
                    "Expands burst buffering. Many more shots before reload - but a much longer reload.",
                    new Color(0.4f, 0.8f, 0.9f), ChipSlotType.Cache, cacheBonus: 6,
                    reloadSpeedMultiplier: 0.65f,
                    chipTradeoffDescription: "Reload takes ~50% longer."),

                parallelProcessingModule = CreateChipModuleItem("Item_ParallelProcessingModule", "parallel_processing_module_01",
                    "Parallel Processing Module", "Splits each shot into three projectiles. Excellent against crowds and shields; wasteful at range.",
                    new Color(0.4f, 0.55f, 0.95f), ChipSlotType.Processor, processorBehavior: ProcessorBehaviorType.ParallelProcessing,
                    damageMultiplier: 0.45f, projectileCount: 3, coolingCostMultiplier: 1.3f,
                    chipOutputDescription: "Multi-Projectile Energy Burst",
                    chipTradeoffDescription: "Each bolt does far less damage, and shots cost +30% energy."),

                focusingAlgorithmModule = CreateChipModuleItem("Item_FocusingAlgorithmModule", "focusing_algorithm_module_01",
                    "Focusing Algorithm Module", "One large, devastating slug. Built for boss vulnerability windows and armoured targets.",
                    new Color(0.9f, 0.3f, 0.3f), ChipSlotType.Processor, processorBehavior: ProcessorBehaviorType.FocusingAlgorithm,
                    damageMultiplier: 2.6f, projectileSizeMultiplier: 1.8f, fireRateMultiplier: 0.55f, coolingCostMultiplier: 1.5f,
                    chipOutputDescription: "Focused Energy Lance",
                    chipTradeoffDescription: "Fires ~45% slower and costs +50% energy per shot."),

                predictiveTargetingModule = CreateChipModuleItem("Item_PredictiveTargetingModule", "predictive_targeting_module_01",
                    "Predictive Targeting Module", "Projectiles steer toward targets. Trivialises fast movers; the tracking overhead slows the bolts.",
                    new Color(0.55f, 0.9f, 0.55f), ChipSlotType.Processor, processorBehavior: ProcessorBehaviorType.PredictiveTargeting,
                    homing: true, damageMultiplier: 0.8f, projectileSpeedMultiplier: 0.7f,
                    chipOutputDescription: "Predictive Energy Bolt",
                    chipTradeoffDescription: "-20% damage and noticeably slower projectiles."),

                coolingLayer = CreateChipModuleItem("Item_CoolingLayer", "cooling_layer_01", "Cooling Layer",
                    "Heavy thermal plating. Shots cost much less and energy returns faster, at the cost of rate of fire.",
                    new Color(0.5f, 0.8f, 0.85f), ChipSlotType.Cooling, coolingCostMultiplier: 0.6f, coolingRegenMultiplier: 1.6f,
                    fireRateMultiplier: 0.8f,
                    chipTradeoffDescription: "-20% fire rate."),

                // Synergy modules: these exist specifically to rescue another module's
                // weakness, so the interesting decision is which pair to run, not which
                // single part is best.
                overclockLayer = CreateChipModuleItem("Item_OverclockLayer", "overclock_layer_01", "Overclock Layer",
                    "Runs the chip hot. Dramatically faster fire rate and reloads - and it drinks energy to do it.",
                    new Color(1f, 0.55f, 0.3f), ChipSlotType.Cooling,
                    coolingCostMultiplier: 1.55f, coolingRegenMultiplier: 0.75f,
                    fireRateMultiplier: 1.9f, reloadSpeedMultiplier: 1.5f,
                    chipTradeoffDescription: "+55% energy per shot and -25% regen. Pair with a Power Component."),

                capacitorBank = CreateChipModuleItem("Item_CapacitorBank", "capacitor_bank_01", "Capacitor Bank",
                    "Dumps its charge fast. Big energy pool and rapid regeneration, but it can't sustain a heavy shot.",
                    new Color(0.85f, 0.9f, 0.35f), ChipSlotType.Battery, batteryBonus: 30,
                    coolingRegenMultiplier: 1.8f, damageMultiplier: 0.85f,
                    chipTradeoffDescription: "-15% damage. Pairs well with Parallel Processing."),

                streamlinedCache = CreateChipModuleItem("Item_StreamlinedCache", "streamlined_cache_01", "Streamlined Cache",
                    "A tiny, ruthlessly fast buffer. Near-instant reloads and lighter movement - if you can live with two shots.",
                    new Color(0.6f, 0.95f, 0.8f), ChipSlotType.Cache, cacheBonus: -4,
                    reloadSpeedMultiplier: 3f, moveSpeedMultiplier: 1.15f,
                    chipTradeoffDescription: "Only 2 shots per burst. Pairs well with Focusing Algorithm."),

                cascadeProcessor = CreateChipModuleItem("Item_CascadeProcessor", "cascade_processor_01", "Cascade Processor",
                    "Twin high-velocity bolts on a hair trigger. Rewards accuracy, punishes spraying.",
                    new Color(0.75f, 0.5f, 0.95f), ChipSlotType.Processor, processorBehavior: ProcessorBehaviorType.ParallelProcessing,
                    damageMultiplier: 0.75f, projectileCount: 2, projectileSpeedMultiplier: 1.45f,
                    fireRateMultiplier: 1.35f, coolingCostMultiplier: 1.25f,
                    chipOutputDescription: "Cascading Twin-Bolt Stream",
                    chipTradeoffDescription: "+25% energy per shot; drains a standard battery quickly."),

                siliconWafer = CreateItemDefinition("Item_SiliconWafer", "silicon_wafer_01", "Silicon Wafer",
                    "A raw silicon wafer from the fabrication line. A keepsake - it doesn't do anything.",
                    ItemCategory.Material, false, new Color(0.8f, 0.82f, 0.85f))
            };
        }

        private static ItemDefinition CreateItemDefinition(string assetName, string id, string displayName, string description,
            ItemCategory category, bool isKeyItem, Color color)
        {
            Sprite icon = isKeyItem
                ? GenerateKeyItemSprite($"item_{id}", color)
                : GenerateItemSprite($"item_{id}", color);

            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.id = id;
            item.displayName = displayName;
            item.color = color;
            item.description = description;
            item.category = category;
            item.isKeyItem = isKeyItem;
            item.icon = icon;

            return (ItemDefinition)CreateOrReplaceAsset(item, $"{DataFolder}/{assetName}.asset");
        }

        private static ItemDefinition CreateChipModuleItem(string assetName, string id, string displayName, string description,
            Color color, ChipSlotType slot, ProcessorBehaviorType processorBehavior = ProcessorBehaviorType.Standard,
            int batteryBonus = 0, int cacheBonus = 0, float coolingCostMultiplier = 1f, float coolingRegenMultiplier = 1f,
            float damageMultiplier = 1f, int projectileCount = 1, float projectileSizeMultiplier = 1f, bool homing = false,
            float moveSpeedMultiplier = 1f, float reloadSpeedMultiplier = 1f, float fireRateMultiplier = 1f,
            float projectileSpeedMultiplier = 1f, string chipOutputDescription = "", string chipTradeoffDescription = "")
        {
            // Badge-style sprite (same look as key items) since these get "installed" -
            // but isKeyItem stays false so they sort into Collectibles, not Key Items.
            Sprite icon = GenerateKeyItemSprite($"item_{id}", color);

            var item = ScriptableObject.CreateInstance<ItemDefinition>();
            item.id = id;
            item.displayName = displayName;
            item.color = color;
            item.description = description;
            item.category = ItemCategory.ChipModule;
            item.isKeyItem = false;
            item.icon = icon;
            item.chipSlot = slot;
            item.processorBehavior = processorBehavior;
            item.batteryBonus = batteryBonus;
            item.cacheBonus = cacheBonus;
            item.coolingCostMultiplier = coolingCostMultiplier;
            item.coolingRegenMultiplier = coolingRegenMultiplier;
            item.damageMultiplier = damageMultiplier;
            item.projectileCount = projectileCount;
            item.projectileSizeMultiplier = projectileSizeMultiplier;
            item.homing = homing;
            item.moveSpeedMultiplier = moveSpeedMultiplier;
            item.reloadSpeedMultiplier = reloadSpeedMultiplier;
            item.fireRateMultiplier = fireRateMultiplier;
            item.projectileSpeedMultiplier = projectileSpeedMultiplier;
            item.chipOutputDescription = chipOutputDescription;
            item.chipTradeoffDescription = chipTradeoffDescription;

            return (ItemDefinition)CreateOrReplaceAsset(item, $"{DataFolder}/{assetName}.asset");
        }

        private static ItemDatabase BuildItemDatabase(params ItemDefinition[] items)
        {
            var database = ScriptableObject.CreateInstance<ItemDatabase>();
            database.allItems = items;
            return (ItemDatabase)CreateOrReplaceAsset(database, $"{DataFolder}/ItemDatabase.asset");
        }

        private static BeyIdentity CreateBeyIdentity(string assetName, string id, string displayName, Color color)
        {
            var identity = ScriptableObject.CreateInstance<BeyIdentity>();
            identity.id = id;
            identity.displayName = displayName;
            identity.color = color;
            return (BeyIdentity)CreateOrReplaceAsset(identity, $"{DataFolder}/{assetName}.asset");
        }

        private static DialogueSet BuildDialogue()
        {
            return new DialogueSet
            {
                receptionist = CreateDialogue("Dialogue_Receptionist", "Receptionist",
                    "Welcome to the fab. Please sign in at the terminal before wandering off.",
                    "Cleanroom protocols apply the moment you're past the lobby doors."),

                floorSupervisor = CreateDialogue("Dialogue_FloorSupervisor", "Floor Supervisor",
                    "You'll need clearance to reach Lithography from here.",
                    "Check the supply cabinet - I think there's a spare keycard rattling around in there."),

                supplyCabinet = CreateDialogue("Dialogue_SupplyCabinet", "Supply Cabinet",
                    "You find a Cleanroom Keycard inside."),

                technicianFirstMeeting = CreateDialogue("Dialogue_TechnicianFirstMeeting", "Lithography Technician",
                    "You made it through the cleanroom door - not everyone bothers to look.",
                    "Here, take this mask. You'll want it more than I do."),

                technicianRepeat = CreateDialogue("Dialogue_TechnicianRepeat", "Lithography Technician",
                    "Careful around the exposure equipment. It doesn't forgive mistakes."),

                recipeTerminal = CreateDialogue("Dialogue_RecipeTerminal", "Terminal",
                    "You pull up an old process recipe from the archive."),

                offDutyEngineer = CreateDialogue("Dialogue_OffDutyEngineer", "Off-Duty Engineer",
                    "Break's the only part of the shift I don't have to think about tolerances.",
                    "Don't mind me, just recharging."),

                whiteboard = CreateDialogue("Dialogue_Whiteboard", "Whiteboard",
                    "Someone's sketched a rough process flow. Doesn't look official.",
                    "There's a bad pun about yield rates written in the corner."),

                passingTechnicianDefault = CreateDialogue("Dialogue_PassingTechnicianDefault", "Passing Technician",
                    "That maintenance hatch is sealed tight. You'd need a maintenance pass to get it open.",
                    "There's a tool rack against the wall - sometimes maintenance leaves spares."),

                passingTechnicianWithPass = CreateDialogue("Dialogue_PassingTechnicianWithPass", "Passing Technician",
                    "Good, you've got a maintenance pass. That hatch should open for you now."),

                toolRack = CreateDialogue("Dialogue_ToolRack", "Tool Rack",
                    "Tucked behind some spare parts, you find a Maintenance Pass."),

                oldAutomationUnit = CreateDialogue("Dialogue_OldAutomationUnit", "Old Automation Unit",
                    "...UNIT OPERATIONAL. AWAITING TASK ASSIGNMENT. ...STILL AWAITING."),

                disabledTerminalLocked = CreateDialogue("Dialogue_DisabledTerminalLocked", "Disabled Terminal",
                    "The terminal needs a calibration tool connected before it'll boot. Nothing happens."),

                disabledTerminalUnlocked = CreateDialogue("Dialogue_DisabledTerminalUnlocked", "Disabled Terminal",
                    "You jury-rig the calibration tool into the terminal. It flickers on and prints a report."),

                briefingTerminal = CreateDialogue("Dialogue_BriefingTerminal", "Briefing Terminal",
                    "COMBAT PROTOTYPE WING - EXPERIMENTAL ACCESS",
                    "Move: WASD. Aim: Mouse. Fire: Left Click.",
                    "Explore ahead to find chip components, then install them at the Fabrication Station.",
                    "Your chip's Processor, Cache, Battery, and Cooling modules all shape how your weapon behaves."),

                componentScanner = CreateDialogue("Dialogue_ComponentScanner", "Component Scanner",
                    "The scanner hums, cataloguing chip components as you pass. Purely diagnostic - it doesn't do anything to them."),

                loreTerminalThermal = CreateDialogue("Dialogue_LoreTerminalThermal", "Thermal Log Terminal",
                    "INCIDENT LOG 114-C: Containment core entered thermal runaway during an unattended overnight run.",
                    "Post-mortem: the core vents heat in bursts. Between bursts it is briefly defenceless.",
                    "Recommendation to future teams: do not out-damage it. Wait for the vent."),

                loreTerminalArchitecture = CreateDialogue("Dialogue_LoreTerminalArchitecture", "Design Archive",
                    "Nobody here builds the 'best' chip. There isn't one.",
                    "Every module we fabricate takes something away to give something back.",
                    "The engineers who do well are the ones who decide what they're willing to lose."),

                maintenanceBayTerminal = CreateDialogue("Dialogue_MaintenanceBayTerminal", "Bay Control",
                    "MAINTENANCE BAY - AUTOMATED DEFENCE ACTIVE.",
                    "Shield generators are protecting the units in this bay. Rounds will not penetrate.",
                    "Destroy the generators first, then clear the bay. A prototype module is stored at the back."),

                hiddenCache = CreateDialogue("Dialogue_HiddenCache", "Loose Panel",
                    "The panel comes away easily - someone's been in here before.",
                    "Tucked behind it is a chip module that never made it onto the inventory.")
            };
        }

        private static DialogueSequence CreateDialogue(string assetName, string speakerName, params string[] lines)
        {
            var sequence = ScriptableObject.CreateInstance<DialogueSequence>();
            sequence.speakerName = speakerName;
            sequence.lines = lines;
            return (DialogueSequence)CreateOrReplaceAsset(sequence, $"{DialogueFolder}/{assetName}.asset");
        }
    }
}
