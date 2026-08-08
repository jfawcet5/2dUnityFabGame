using System.Collections.Generic;
using BeyProject.Combat;
using BeyProject.Core;
using BeyProject.Player;
using UnityEngine;
using UnityEngine.UI;

namespace BeyProject.UI
{
    /// <summary>
    /// Always-visible combat readout: health, energy, burst pips, reload progress, the chip
    /// modules currently shaping the weapon, the room objective, and a boss bar when one is
    /// alive. Unlike Pause/Inventory/Fabrication this is never toggled inactive - it's a
    /// passive display - so it has no exposure to the "Update() on a self-deactivating
    /// GameObject" bug class at all.
    ///
    /// Bars lerp toward their target rather than snapping: an instant jump on a bar this
    /// small is easy to miss entirely, which is what made the old health display read as
    /// non-functional.
    /// </summary>
    public class CombatHUD : MonoBehaviour
    {
        private const float BarLerpSpeed = 8f;
        private const int MaxBurstPips = 16;

        [SerializeField] private Image healthFillImage;
        [SerializeField] private Text healthText;
        [SerializeField] private Image energyFillImage;
        [SerializeField] private Text energyText;
        [SerializeField] private Text burstText;
        [SerializeField] private Transform burstPipsParent;
        [SerializeField] private GameObject reloadRoot;
        [SerializeField] private Image reloadFillImage;
        [SerializeField] private Text chipEffectsText;
        [SerializeField] private Text objectiveText;
        [SerializeField] private GameObject bossRoot;
        [SerializeField] private Image bossFillImage;
        [SerializeField] private Text bossNameText;

        private readonly List<Image> burstPips = new List<Image>();

        private PlayerHealth cachedHealth;
        private PlayerCombat cachedCombat;
        private float refreshTimer;
        private float displayedHealth = 1f;
        private float displayedEnergy = 1f;
        private float displayedBoss = 1f;
        private string lastChipSummary = "";

        private void Update()
        {
            refreshTimer -= Time.unscaledDeltaTime;
            if (cachedHealth == null || cachedCombat == null || refreshTimer <= 0f)
            {
                refreshTimer = 0.5f;
                FindPlayerComponents();
            }

            UpdateHealth();
            UpdateEnergyAndBurst();
            UpdateChipEffects();
            UpdateObjective();
            UpdateBoss();
        }

        private void UpdateHealth()
        {
            if (cachedHealth == null)
            {
                return;
            }

            float target = cachedHealth.MaxHealth > 0f ? cachedHealth.CurrentHealth / cachedHealth.MaxHealth : 0f;
            displayedHealth = Mathf.Lerp(displayedHealth, target, BarLerpSpeed * Time.deltaTime);

            if (healthFillImage != null)
            {
                healthFillImage.fillAmount = displayedHealth;

                Color color = target <= 0.3f
                    ? Color.Lerp(new Color(0.85f, 0.15f, 0.15f), new Color(1f, 0.55f, 0.35f), 0.5f + 0.5f * Mathf.Sin(Time.time * 9f))
                    : new Color(0.8f, 0.2f, 0.2f);

                // Whiten briefly on a hit so damage registers even when the bar is nearly full.
                healthFillImage.color = Color.Lerp(color, Color.white, cachedHealth.RecentDamagePulse * 0.7f);
            }

            if (healthText != null)
            {
                healthText.text = $"HP  {Mathf.CeilToInt(Mathf.Max(0f, cachedHealth.CurrentHealth))} / {Mathf.RoundToInt(cachedHealth.MaxHealth)}";
            }
        }

        private void UpdateEnergyAndBurst()
        {
            if (cachedCombat == null)
            {
                return;
            }

            float target = cachedCombat.MaxEnergy > 0f ? cachedCombat.CurrentEnergy / cachedCombat.MaxEnergy : 0f;
            displayedEnergy = Mathf.Lerp(displayedEnergy, target, BarLerpSpeed * Time.deltaTime);

            if (energyFillImage != null)
            {
                energyFillImage.fillAmount = displayedEnergy;
                // Goes amber when there isn't enough left for another shot - the moment the
                // player needs to know about, rather than a generic "low" threshold.
                bool starved = cachedCombat.CurrentEnergy < cachedCombat.CurrentStats.shotEnergyCost;
                energyFillImage.color = starved ? new Color(0.95f, 0.65f, 0.25f) : new Color(0.3f, 0.7f, 0.95f);
            }

            if (energyText != null)
            {
                energyText.text = $"EN  {Mathf.FloorToInt(Mathf.Max(0f, cachedCombat.CurrentEnergy))} / {Mathf.RoundToInt(cachedCombat.MaxEnergy)}";
            }

            UpdateBurstPips();

            if (burstText != null)
            {
                burstText.text = cachedCombat.IsReloading
                    ? "RELOADING"
                    : $"{cachedCombat.CurrentBurst} / {cachedCombat.BurstCapacity}";
            }

            if (reloadRoot != null)
            {
                reloadRoot.SetActive(cachedCombat.IsReloading);
            }

            if (reloadFillImage != null && cachedCombat.IsReloading)
            {
                reloadFillImage.fillAmount = cachedCombat.ReloadProgress;
            }
        }

        /// <summary>
        /// Discrete pips make "two shots left" legible at a glance in a way a continuous bar
        /// never is. Pooled and re-shown rather than rebuilt, since burst capacity changes
        /// whenever a Cache module is swapped.
        /// </summary>
        private void UpdateBurstPips()
        {
            if (burstPipsParent == null)
            {
                return;
            }

            int capacity = Mathf.Clamp(cachedCombat.BurstCapacity, 0, MaxBurstPips);

            while (burstPips.Count < capacity)
            {
                var pipGO = new GameObject($"Pip{burstPips.Count}", typeof(Image), typeof(LayoutElement));
                pipGO.transform.SetParent(burstPipsParent, false);
                LayoutElement layout = pipGO.GetComponent<LayoutElement>();
                layout.preferredWidth = 9f;
                layout.preferredHeight = 9f;
                burstPips.Add(pipGO.GetComponent<Image>());
            }

            for (int i = 0; i < burstPips.Count; i++)
            {
                bool used = i < capacity;
                burstPips[i].gameObject.SetActive(used);

                if (used)
                {
                    burstPips[i].color = i < cachedCombat.CurrentBurst
                        ? new Color(0.45f, 0.9f, 1f)
                        : new Color(1f, 1f, 1f, 0.16f);
                }
            }
        }

        private void UpdateChipEffects()
        {
            if (chipEffectsText == null)
            {
                return;
            }

            if (ChipManager.Instance == null)
            {
                chipEffectsText.text = "";
                return;
            }

            List<string> labels = ChipManager.Instance.GetActiveEffectLabels();
            string summary = labels.Count > 0
                ? string.Join("  •  ", labels)
                : "Standard Chip";

            // Only touch the Text when it actually changed - this runs every frame and
            // assigning Text.text rebuilds the mesh whether or not the string differs.
            if (summary != lastChipSummary)
            {
                lastChipSummary = summary;
                chipEffectsText.text = summary;
            }
        }

        private void UpdateObjective()
        {
            if (objectiveText == null)
            {
                return;
            }

            string objective = CombatEncounterController.CurrentObjectiveText;
            if (objectiveText.text != objective)
            {
                objectiveText.text = objective;
            }
        }

        private void UpdateBoss()
        {
            BossEnemy boss = BossEnemy.Active;

            if (bossRoot != null)
            {
                bossRoot.SetActive(boss != null);
            }

            if (boss == null)
            {
                displayedBoss = 1f;
                return;
            }

            displayedBoss = Mathf.Lerp(displayedBoss, boss.HealthFraction, BarLerpSpeed * Time.deltaTime);
            Debug.Log($"Displayed boss: {displayedBoss}");

            if (bossFillImage != null)
            {
                bossFillImage.fillAmount = displayedBoss;
                bossFillImage.color = boss.IsVulnerable
                    ? new Color(1f, 0.95f, 0.5f)
                    : new Color(0.9f, 0.3f, 0.2f);
            }

            if (bossNameText != null)
            {
                string state = boss.IsVulnerable ? "  —  OVERHEATED" : "";
                bossNameText.text = $"{boss.DisplayName}   (Phase {boss.Phase}){state}";
            }
        }

        private void FindPlayerComponents()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
            {
                cachedHealth = null;
                cachedCombat = null;
                return;
            }

            cachedHealth = player.GetComponent<PlayerHealth>();
            cachedCombat = player.GetComponent<PlayerCombat>();
        }
    }
}
