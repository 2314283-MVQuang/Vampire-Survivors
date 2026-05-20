using System.Collections.Generic;
using UnityEngine;

public class ExperienceLevelController : MonoBehaviour
{
    public static ExperienceLevelController instance;

    private void Awake()
    {
        instance = this;
    }

    public int currentExperience;
    public ExpPickup pickup;
    public List<int> expLevels = new List<int>();
    public int currentLevel = 1, levelCount = 100;

    private void Start()
    {
        // Safety: make sure expLevels has at least one base value
        if (expLevels.Count == 0)
        {
            expLevels.Add(30); // base exp requirement for level 1 -> 2
        }

        while (expLevels.Count < levelCount)
        {
            expLevels.Add(Mathf.CeilToInt(expLevels[expLevels.Count - 1] * 3.0f));
        }

        if (UIController.instance != null)
        {
            UIController.instance.UpdateExperience(currentExperience, expLevels[currentLevel], currentLevel);
        }
    }

    public void GetExp(int amountToGet)
    {
        currentExperience += amountToGet;

        while (currentLevel < expLevels.Count && currentExperience >= expLevels[currentLevel])
        {
            LevelUp();
            break; // keep one level-up at a time for the UI
        }

        if (UIController.instance != null)
            UIController.instance.UpdateExperience(currentExperience, expLevels[currentLevel], currentLevel);

        if (SFXManager.instance != null)
            SFXManager.instance.PlaySFXPitched(2);
    }

    public void SpawnExp(Vector3 position, int expValue)
    {
        Instantiate(pickup, position, Quaternion.identity).expValue = expValue;
    }

    private void LevelUp()
    {
        currentExperience -= expLevels[currentLevel - 1];
        currentLevel++;

        if (currentLevel >= expLevels.Count)
            currentLevel = expLevels.Count - 1;

        if (UIController.instance != null && UIController.instance.levelUpPanel != null)
            UIController.instance.levelUpPanel.SetActive(true);

        Time.timeScale = 0f;

        BuildClassBasedLevelUpChoices();

        if (PlayerStatController.instance != null)
            PlayerStatController.instance.UpdateDisplay();
    }

    /// <summary>
    /// Builds the list of upgrade choices for the level-up panel using the class progression system.
    /// Rules:
    ///   1. If no class yet → offer class selections.
    ///   2. If active class has promotion & all weapons maxed → offer Promotion.
    ///   3. Always offer weapon upgrades / unlocks for the active class when possible.
    ///   4. If CanUnlockNewClass && còn slot → cũng cho chọn class mới (KHÔNG bắt buộc vũ khí phải max nữa).
    /// </summary>
    private void BuildClassBasedLevelUpChoices()
    {
        if (UIController.instance == null || ClassManager.instance == null)
            return;

        var buttons = UIController.instance.levelUpButtons;
        Debug.Log($"✅ Buttons count: {buttons.Length}");
        var choices = new List<LevelUpChoice>();
        ClassManager cm = ClassManager.instance;

        UIController.instance.SetLevelUpPanelTitle("Choose Your Path");

        // --------------------------------------------------
        // Case 1: Player has no class yet → show class selection
        // --------------------------------------------------
        if (cm.HasNoClass)
        {
            UIController.instance.SetLevelUpPanelTitle("Choose Your Class");

            var unlockable = cm.GetUnlockableClasses();
            int count = Mathf.Min(unlockable.Count, buttons.Length);

            for (int i = 0; i < count; i++)
            {
                choices.Add(new LevelUpChoice
                {
                    type = ChoiceType.SelectClass,
                    classData = unlockable[i]
                });
            }
        }
        else
        {
            ClassData activeClass = cm.ActiveClass;
            if (activeClass == null)
                return;

            bool allWeaponsMaxed = cm.AreAllWeaponsMaxedForClass(activeClass);

            // ✅ Nếu tất cả weapons max → spawn ascend stones thay vì offer promotion
            if (allWeaponsMaxed)
            {
                if (AscendStoneManager.instance != null)
                {
                    AscendStoneManager.instance.SpawnAscendStones();
                    Debug.Log("✅ All weapons maxed! Ascend stones spawned.");
                }
            }

            // --------------------------------------------------
            // Case 3: Offer weapon upgrades for the active class (nếu còn weapon để nâng)
            // --------------------------------------------------
            List<Weapon> available = cm.GetAvailableWeaponsForClass(activeClass);
            Debug.Log($"✅ Class Weapons count: {activeClass.classWeapons?.Count ?? 0}");
            if (activeClass.classWeapons != null)
            {
                for (int w = 0; w < activeClass.classWeapons.Count; w++)
                {
                    var weaponObj = activeClass.classWeapons[w];
                    if (weaponObj == null)
                        Debug.Log($"  ❌ Weapon {w}: NULL");
                    else
                    {
                        var weaponScript = weaponObj.GetComponent<Weapon>();
                        Debug.Log($"  - Weapon {w}: {weaponObj.name} → Script: {weaponScript?.name ?? "NULL"}");
                    }
                }
            }
            Debug.Log($"✅ Available weapons: {available.Count}");

            // Số slot dành cho weapon (dùng hết buttons, class offer sẽ fill nếu còn slot)
            int maxWeaponSlots = buttons.Length;  // ✅ Dùng hết buttons
            int weaponSlots = Mathf.Min(available.Count, maxWeaponSlots);

            UIController.instance.SetLevelUpPanelTitle("Choose Your Upgrade");

            for (int i = 0; i < weaponSlots && available.Count > 0; i++)
            {
                int idx = Random.Range(0, available.Count);
                choices.Add(new LevelUpChoice
                {
                    type = ChoiceType.Weapon,
                    weapon = available[idx]
                });
                available.RemoveAt(idx);
            }

            // --------------------------------------------------
            // Case 4: Offer unlocking a new class 
            //  → KHÔNG cần allWeaponsMaxed nữa
            // --------------------------------------------------
            if (cm.CanUnlockNewClass && choices.Count < buttons.Length)
            {
                var unlockable = cm.GetUnlockableClasses();
                // nếu muốn vẫn ưu tiên khi allWeaponsMaxed, có thể điều chỉnh roll
                foreach (var candidate in unlockable)
                {
                    float roll = Random.value;

                    // ví dụ: nếu vũ khí đã max thì tăng tỉ lệ x2
                    float baseChance = candidate.secondClassUnlockChance;
                    if (allWeaponsMaxed)
                        baseChance *= 2f;

                    if (roll <= baseChance)
                    {
                        choices.Add(new LevelUpChoice
                        {
                            type = ChoiceType.SelectClass,
                            classData = candidate
                        });
                        Debug.Log($"✅ Offered new class: {candidate.className} (roll={roll}, chance={baseChance})");
                        break;
                    }
                }
            }
        }

        // --------------------------------------------------
        // Fallback: nếu không có choice nào thì vẫn phải có cái để chọn
        // --------------------------------------------------
        if (choices.Count == 0)
        {
            
            if (cm.HasNoClass)
            {
                var unlockable = cm.GetUnlockableClasses();
                int count = Mathf.Min(unlockable.Count, buttons.Length);

                for (int i = 0; i < count; i++)
                {
                    choices.Add(new LevelUpChoice
                    {
                        type = ChoiceType.SelectClass,
                        classData = unlockable[i]
                    });
                }
            }
            else
            {
                ClassData activeClass = cm.ActiveClass;
                if (activeClass != null)
                {
                    List<Weapon> fallbackWeapons = cm.GetAvailableWeaponsForClass(activeClass);
                    int count = Mathf.Min(fallbackWeapons.Count, buttons.Length);

                    for (int i = 0; i < count; i++)
                    {
                        choices.Add(new LevelUpChoice
                        {
                            type = ChoiceType.Weapon,
                            weapon = fallbackWeapons[i]
                        });
                    }
                }
            }
        }

        // --------------------------------------------------
        // Apply choices to buttons
        // --------------------------------------------------
        for (int i = 0; i < buttons.Length; i++)
        {
            if (i < choices.Count)
            {
                buttons[i].gameObject.SetActive(true);
                buttons[i].SetChoice(choices[i]);
            }
            else
            {
                buttons[i].gameObject.SetActive(false);
            }
        }

        if (UIController.instance != null)
            UIController.instance.UpdateActiveClassDisplay();
    }

    /// <summary>
    /// Hiển thị menu promotion khi nhặt được ascend stone
    /// </summary>
    public void ShowAscendStonePromotionMenu()
    {
        if (ClassManager.instance == null)
            return;

        if (UIController.instance == null || UIController.instance.promotionPanel == null)
            return;

        // ✅ Check xem tất cả weapons đã max hết chưa
        ClassData activeClass = ClassManager.instance.ActiveClass;
        if (activeClass == null)
            return;

        bool allWeaponsMaxed = ClassManager.instance.AreAllWeaponsMaxedForClass(activeClass);
        if (!allWeaponsMaxed)
        {
            Debug.LogWarning("⚠️ Cannot promote: Not all weapons are maxed!");
            return;
        }

        // ✅ Build promotion choices cho tất cả classes của player
        var buttons = UIController.instance.promotionButtons;
        var choices = new List<LevelUpChoice>();

        UIController.instance.SetPromotionPanelTitle("Choose Your Promotion");

        // ✅ Offer promotion cho tất cả classes có promotionClass
        foreach (var classData in ClassManager.instance.playerClasses)
        {
            if (classData == null) continue;
            if (classData.promotionClass == null) continue;

            choices.Add(new LevelUpChoice
            {
                type = ChoiceType.Promotion,
                classData = classData
            });

            Debug.Log($"✅ Offered promotion: {classData.className} → {classData.promotionClass.className}");

            if (choices.Count >= buttons.Length)
                break;
        }

        // ✅ Nếu không có promotion nào → đóng menu
        if (choices.Count == 0)
        {
            Debug.LogWarning("⚠️ No promotions available!");
            UIController.instance.promotionPanel.SetActive(false);
            Time.timeScale = 1f;
            return;
        }

        // ✅ Apply choices to buttons
        for (int i = 0; i < buttons.Length; i++)
        {
            if (i < choices.Count)
            {
                buttons[i].gameObject.SetActive(true);
                buttons[i].SetChoice(choices[i]);
            }
            else
            {
                buttons[i].gameObject.SetActive(false);
            }
        }

        // ✅ Show panel
        UIController.instance.promotionPanel.SetActive(true);
        Time.timeScale = 0f;

        if (UIController.instance != null)
            UIController.instance.UpdateActiveClassDisplay();
    }
}

// ---------------------------------------------------------------------------

public enum ChoiceType
{
    Weapon,
    SelectClass,
    Promotion
}

[System.Serializable]
public class LevelUpChoice
{
    public ChoiceType type;
    public Weapon weapon;
    public ClassData classData;
}