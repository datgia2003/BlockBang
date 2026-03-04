using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillUI : MonoBehaviour
{
    [SerializeField] private int skillIndex;
    [SerializeField] private Button skillButton;
    [SerializeField] private TextMeshProUGUI cooldownText;
    [SerializeField] private Image cooldownOverlay;

    private Skill skill;

    void Start()
    {
        if (SkillManager.Instance != null)
        {
            var skills = SkillManager.Instance.GetSkills();
            if (skillIndex >= 0 && skillIndex < skills.Count)
            {
                skill = skills[skillIndex];
            }
        }

        if (skillButton != null)
        {
            skillButton.onClick.AddListener(ActivateSkill);
        }
    }

    void Update()
    {
        if (skill != null)
        {
            if (skill.IsReady())
            {
                skillButton.interactable = true;
                if(cooldownText != null) cooldownText.text = "";
                if(cooldownOverlay != null) cooldownOverlay.fillAmount = 0;
            }
            else
            {
                skillButton.interactable = false;
                if(cooldownText != null) cooldownText.text = skill.CurrentCooldown.ToString();
                if(cooldownOverlay != null) cooldownOverlay.fillAmount = (float)skill.CurrentCooldown / skill.Cooldown;
            }
        }
    }

    private void ActivateSkill()
    {
        if (SkillManager.Instance != null)
        {
            SkillManager.Instance.ActivateSkill(skillIndex);
        }
    }
}
