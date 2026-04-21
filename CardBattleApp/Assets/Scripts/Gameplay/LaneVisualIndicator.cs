using UnityEngine;

public class LaneVisualIndicator : MonoBehaviour
{
    [SerializeField] private MeshRenderer laneRenderer;

    // Colores con estilo Minimalismo Funcional (Tech/Cyber)
    [SerializeField] private Color defaultColor = new Color(0.2f, 0.2f, 0.2f, 0.5f); // Gris oscuro semi-transparente
    [SerializeField] private Color occupiedColor = new Color(0.0f, 0.8f, 1.0f, 0.5f); // Azul cyan tech

    public bool IsOccupied { get; private set; }

    private void Start()
    {
        SetOccupiedState(false);
    }

    public void SetOccupiedState(bool occupied)
    {
        IsOccupied = occupied;

        if (laneRenderer != null)
        {
            // Cambiamos el color del material dinámicamente
            laneRenderer.material.color = occupied ? occupiedColor : defaultColor;
        }
    }
}