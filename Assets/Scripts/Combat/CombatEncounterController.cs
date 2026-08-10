using System.Collections;
using System.Collections.Generic;
using BeyProject.Core;
using BeyProject.Overworld;
using BeyProject.UI;
using UnityEngine;

namespace BeyProject.Combat
{
    public enum CombatObjectiveType
    {
        None,
        DefeatAllEnemies,
        DestroyShieldGeneratorsThenEnemies,
        ActivateAllSwitches,
        DestroyAllTurrets
    }

    /// <summary>
    /// Generic, reusable room gate - not hardcoded to any specific room, and now not
    /// hardcoded to "kill everything" either. All four objective types are branched on the
    /// enum in one component (same convention as EnemyBase/WorldAction) and every one of
    /// them reuses the existing combat systems: a switch objective is still the same
    /// IInteractable, a generator objective is still the same shooting.
    ///
    /// Everything is counted at Start(), by which point already-completed props have
    /// self-destroyed in their own Awake() from their save flags - Unity runs all Awake()s
    /// before any Start() - so a re-entered room naturally starts with a correct count and
    /// can complete immediately.
    /// </summary>
    public class CombatEncounterController : MonoBehaviour
    {
        [SerializeField] private Door targetDoor;
        [SerializeField] private string clearedFlag = "combat_room_cleared";
        [SerializeField] private List<CombatObjectiveType> objectives = new List<CombatObjectiveType> { CombatObjectiveType.DefeatAllEnemies };
        [SerializeField] private string objectiveAnnouncement = "";

        [SerializeField]  private int remainingEnemies;
        private int remainingGenerators;
        private int remainingTurrets;
        private int remainingSwitches;
        private bool cleared;

        /// <summary>The live objective line for the HUD, or empty when there's nothing to show.</summary>
        public static string CurrentObjectiveText { get; private set; } = "";

        private void OnEnable()
        {
            CurrentObjectiveText = "";
        }

        private void Start()
        {
            foreach (EnemyBase enemy in FindObjectsOfType<EnemyBase>())
            {
                if (enemy.GetIsDefeated()) {
                    continue;
                }
                remainingEnemies++;
                enemy.Defeated += HandleEnemyDefeated;
            }

            foreach (ShieldGenerator generator in FindObjectsOfType<ShieldGenerator>())
            {
                if (generator.GetIsDestroyed())
                {
                    continue;
                }
                
                remainingGenerators++;
                generator.Destroyed += HandleGeneratorDestroyed;
            }

            foreach (Turret turret in FindObjectsOfType<Turret>())
            {
                if (turret.GetIsDestroyed())
                {
                    continue;
                }

                remainingTurrets++;
                turret.Destroyed += HandleTurretDestroyed;
            }

            foreach (CombatSwitch combatSwitch in FindObjectsOfType<CombatSwitch>())
            {
                if (combatSwitch.IsActivated)
                {
                    continue;
                }

                remainingSwitches++;
                combatSwitch.Activated += HandleSwitchActivated;
            }

            EvaluateObjective();
            RefreshObjectiveText();

            if (!string.IsNullOrEmpty(objectiveAnnouncement) && !cleared)
            {
                StartCoroutine(AnnounceObjectiveAfterRoomTitle());
            }
        }

        /// <summary>
        /// RoomIntro shows the room name from its own Start(), and RoomTitleUI.Show cancels
        /// whatever banner is already running - so announcing the objective immediately would
        /// simply erase the room name. Wait out that banner first.
        /// </summary>
        private IEnumerator AnnounceObjectiveAfterRoomTitle()
        {
            yield return new WaitForSecondsRealtime(2.6f);

            if (!cleared)
            {
                RoomTitleUI.Instance?.Show(objectiveAnnouncement);
            }
        }

        private void OnDestroy()
        {
            CurrentObjectiveText = "";
        }

        private void HandleEnemyDefeated()
        {
            remainingEnemies--;
            RefreshObjectiveText();
            EvaluateObjective();
        }

        private void HandleGeneratorDestroyed()
        {
            remainingGenerators--;
            RefreshObjectiveText();
            EvaluateObjective();
        }

        private void HandleTurretDestroyed()
        {
            remainingTurrets--;
            RefreshObjectiveText();
            EvaluateObjective();
        }

        private void HandleSwitchActivated()
        {
            remainingSwitches--;
            RefreshObjectiveText();
            EvaluateObjective();
        }

        private bool IsObjectiveComplete(CombatObjectiveType objective)
        {
            switch (objective)
            {
                case CombatObjectiveType.DestroyShieldGeneratorsThenEnemies:
                    return remainingGenerators <= 0 && remainingEnemies <= 0;
                case CombatObjectiveType.ActivateAllSwitches:
                    return remainingSwitches <= 0;
                case CombatObjectiveType.DestroyAllTurrets:
                    return remainingTurrets <= 0;
                default:
                    return remainingEnemies <= 0;
            }
        }

        private void RefreshObjectiveText()
        {
            if (cleared)
            {
                CurrentObjectiveText = "";
                return;
            }

            CombatObjectiveType current = CombatObjectiveType.None;

            foreach (CombatObjectiveType objective in objectives)
            {
                if (!IsObjectiveComplete(objective))
                {
                    current = objective;
                }
            }

            switch (current)
            {
                case CombatObjectiveType.DestroyShieldGeneratorsThenEnemies:
                    CurrentObjectiveText = remainingGenerators > 0
                        ? $"Shield Generators: {remainingGenerators}  (enemies invulnerable)"
                        : $"Hostiles remaining: {remainingEnemies}";
                    break;
                case CombatObjectiveType.ActivateAllSwitches:
                    CurrentObjectiveText = $"Switches remaining: {remainingSwitches}";
                    break;
                case CombatObjectiveType.DestroyAllTurrets:
                    CurrentObjectiveText = $"Turrets remaining: {remainingTurrets}";
                    break;
                default:
                    CurrentObjectiveText = $"Hostiles remaining: {remainingEnemies}";
                    break;
            }
        }

        private void EvaluateObjective()
        {
            int completeCount = 0;

            foreach (CombatObjectiveType objective in objectives)
            {
                if (IsObjectiveComplete(objective))
                {
                    completeCount++;
                }
            }

            if (completeCount < objectives.Count)
            {
                return;
            }

            cleared = true;
            CurrentObjectiveText = "";

            SaveSystem.Instance?.SetFlag(clearedFlag);
            targetDoor?.UnlockRemotely();
            RoomTitleUI.Instance?.Show("Route Clear");
        }
    }
}
