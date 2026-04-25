using UnityEngine;
using UnityEngine.Events;

// 2. El Componente que va en tus Box Colliders
public class VRButton : MonoBehaviour, IInteractable
{
    [Header("Feedback Visual")]
    public GameObject feedbackHighlight; // Puede ser un borde brillante que se activa al mirarlo

    [Header("Eventos al Interactuar")]
    public UnityEvent OnClick; // Aquí arrastrarás las funciones del MenuManager

    private void Start()
    {
        if (feedbackHighlight != null)
        {
            feedbackHighlight.SetActive(false);
        }
    }

    public void OnHoverEnter()
    {
        if (feedbackHighlight != null) feedbackHighlight.SetActive(true);
        // Aquí podrías agregar un sonido de "Hover" pequeñito
    }

    public void OnHoverExit()
    {
        if (feedbackHighlight != null) feedbackHighlight.SetActive(false);
    }

    public void Interact()
    {
        // Ejecuta lo que sea que le hayas configurado en el Inspector
        OnClick?.Invoke();
    }
}
