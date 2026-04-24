using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitWorldUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI nameText;
    public Image healthFill;

    [Header("Combat References")]
    [Tooltip("Arrastra aquí un objeto vacío posicionado en la punta del arma.")]
    public Transform shootPoint; // NUEVO: Punto exacto de salida de la bala

    private Unit _trackedUnit;

    public void Initialize(Unit unitData)
    {
        _trackedUnit = unitData;

        if (nameText != null)
        {
            nameText.text = unitData.cardId.ToString();
        }
    }

    private void Update()
    {
        if (_trackedUnit != null && healthFill != null)
        {
            if (_trackedUnit.maxHp > 0)
            {
                healthFill.fillAmount = _trackedUnit.currentHp / _trackedUnit.maxHp;
            }
        }
    }
}