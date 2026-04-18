using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static RoomData;

public class DungeonUI : MonoBehaviour
{
    public DungeonController controller;

    public TMP_InputField sizeInput;
    public TMP_InputField seedInput;
    public Slider difficultySlider;
    public Toggle branchToggle;
    public TMP_InputField cellSpacingInput;

    public TMP_InputField combatInput;
    public TMP_InputField treasureInput;
    public TMP_InputField trapInput;

    public TMP_Text debugText;

    public void OnGenerateClicked()
    {
        controller.difficulty = difficultySlider.value;
        controller.enableBranches = branchToggle.isOn;

        if (int.TryParse(sizeInput.text, out int size))
            controller.size = size;

        if (int.TryParse(seedInput.text, out int seed))
            controller.seed = seed;

        if (float.TryParse(cellSpacingInput.text, out float cellSpacing))
            controller.cellSpacing = cellSpacing;

        if (int.TryParse(combatInput.text, out int maxCombat))
            controller.maxCombat = maxCombat;

        if (int.TryParse(treasureInput.text, out int maxTreasure))
            controller.maxTreasure = maxTreasure;

        if (int.TryParse(trapInput.text, out int maxTrap))
            controller.maxTrap = maxTrap;

        controller.RunGeneration();

        debugText.text =
            $"Size: {controller.size}\n" +
            $"Seed: {controller.seed}\n" +
            $"Difficulty: {controller.difficulty:F2}\n" +
            $"Combat num: {controller.maxCombat}\n" +
            $"Treasure num: {controller.maxTreasure}\n" +
            $"Trap num: {controller.maxTrap}\n";
    }

    public void OnRandomSeed()
    {
        int s = Random.Range(0, 99999);
        seedInput.text = s.ToString();

        OnGenerateClicked();
    }

    
}