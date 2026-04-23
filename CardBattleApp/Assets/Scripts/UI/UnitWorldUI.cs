using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitWorldUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI nameText;
    public Image healthFill; // Debe estar configurado como Image Type: Filled

    // Referencia a la entidad matemática que rastrea
    private Unit _trackedUnit;

    /// <summary>
    /// Enlaza este visualizador con los datos lógicos del GameManager.
    /// </summary>
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
        // Actualizamos la barra de vida constantemente si la unidad sigue viva
        if (_trackedUnit != null && healthFill != null)
        {
            // Evitamos divisiones por cero por seguridad
            if (_trackedUnit.maxHp > 0)
            {
                healthFill.fillAmount = _trackedUnit.currentHp / _trackedUnit.maxHp;
            }
        }
    }
}