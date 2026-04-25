using UnityEngine;
using UnityEngine.InputSystem;

public class VRPlayerController : MonoBehaviour
{
    [Header("Configuración de Interacción")]
    public float interactDistance = 15f;
    public Transform reticle; // Un puntito blanco en el centro de la cámara (Canvas o Sprite)

    private GameObject cameraContainer;
    private IInteractable lastTarget;

    void Start()
    {
        // 1. Configurar Giroscopio
        cameraContainer = new GameObject("Camera Container");
        cameraContainer.transform.position = transform.position;
        transform.SetParent(cameraContainer.transform);
        Input.gyro.enabled = true;
    }

    void Update()
    {
        // Si hay un panel abierto (Ajustes/Créditos), congelamos la cámara
        if (MenuManager.Instance != null && MenuManager.Instance.HayPanelesAbiertos())
        {
            // Opcional: Ocultar el reticle si hay un menú abierto
            if (reticle != null) reticle.gameObject.SetActive(false);
            return;
        }

        if (reticle != null) reticle.gameObject.SetActive(true);

        RotarCamaraGiroscopio();
        ManejarRaycast();
    }

    private void RotarCamaraGiroscopio()
    {
        transform.localRotation = new Quaternion(
            Input.gyro.attitude.x,
            Input.gyro.attitude.y,
            -Input.gyro.attitude.z,
            -Input.gyro.attitude.w);
    }

    private void ManejarRaycast()
    {
        Ray ray = new Ray(transform.position, transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                // Entra al objeto nuevo
                if (lastTarget != interactable)
                {
                    lastTarget?.OnHoverExit();
                    interactable.OnHoverEnter();
                    lastTarget = interactable;

                    // Feedback visual del reticle (ej: crece un poquito)
                    if (reticle != null) reticle.localScale = Vector3.one * 1.5f;
                }

                // Si tocamos la pantalla... (Nuevo Input System)
                if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
                {
                    interactable.Interact();
                }
            }
            else
            {
                LimpiarTarget();
            }
        }
        else
        {
            LimpiarTarget();
        }
    }

    private void LimpiarTarget()
    {
        if (lastTarget != null)
        {
            lastTarget.OnHoverExit();
            lastTarget = null;
            if (reticle != null) reticle.localScale = Vector3.one; // Reticle vuelve a tamaño normal
        }
    }
}
