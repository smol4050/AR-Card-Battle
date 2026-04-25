using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitWorldUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI nameText;
    public Image healthFill;

    [Header("Combat References")]
    [Tooltip("Objeto vacío posicionado en la punta del arma. " +
             "Si está asignado, la unidad se trata como Ranged.")]
    public Transform shootPoint;

    private Unit _trackedUnit;

    public void Initialize(Unit unitData)
    {
        _trackedUnit = unitData;
        if (nameText != null) nameText.text = unitData.cardId.ToString();
    }

    private void Update()
    {
        if (_trackedUnit == null || healthFill == null) return;
        if (_trackedUnit.maxHp > 0)
            healthFill.fillAmount = Mathf.Clamp01(_trackedUnit.currentHp / _trackedUnit.maxHp);
    }
}