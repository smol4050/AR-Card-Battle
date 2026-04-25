using UnityEngine;
using UnityEngine.InputSystem;

public class VRPlayerController : MonoBehaviour
{
    [Header("Configuración de Interacción")]
    public float interactDistance = 15f;
    public Transform reticle;

    [Header("Ajustes del Giroscopio")]
    [Range(1f, 20f)]
    public float suavizado = 10f; // Mayor número = movimiento más rápido y nervioso. Menor = más suave.

    private GameObject cameraContainer;
    private IInteractable lastTarget;

    void Start()
    {
        // 1. Configurar Contenedor de Cámara
        cameraContainer = new GameObject("Camera Container");
        cameraContainer.transform.position = transform.position;
        transform.SetParent(cameraContainer.transform);

        // 2. Activar el Sensor
        if (AttitudeSensor.current != null)
        {
            InputSystem.EnableDevice(AttitudeSensor.current);
        }

        // 3. Compensación inicial para Landscape
        // Giramos el contenedor para que el "frente" del sensor coincida con el horizonte
        cameraContainer.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    void Update()
    {
        if (MenuManager.Instance != null && MenuManager.Instance.HayPanelesAbiertos())
        {
            if (reticle != null) reticle.gameObject.SetActive(false);
            return;
        }

        if (reticle != null) reticle.gameObject.SetActive(true);

        RotarCamaraGiroscopio();
        ManejarRaycast();
    }

    private void RotarCamaraGiroscopio()
    {
        if (AttitudeSensor.current != null)
        {
            // Leemos la actitud (orientación) actual
            Quaternion q = AttitudeSensor.current.attitude.ReadValue();

            // Re-mapeo de ejes para que funcione en Landscape (Horizontal)
            // Invertimos Z y W para corregir el "espejo" de Unity
            Quaternion nuevaRotacion = new Quaternion(q.x, q.y, -q.z, -q.w);

            // APLICAMOS SLERP: Esto es lo que quita el temblor. 
            // Interpola suavemente entre la rotación actual y la nueva.
            transform.localRotation = Quaternion.Slerp(
                transform.localRotation,
                nuevaRotacion,
                Time.deltaTime * suavizado
            );
        }
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
                if (lastTarget != interactable)
                {
                    lastTarget?.OnHoverExit();
                    interactable.OnHoverEnter();
                    lastTarget = interactable;
                    if (reticle != null) reticle.localScale = Vector3.one * 1.5f;
                }

                if (Pointer.current != null && Pointer.current.press.wasPressedThisFrame)
                {
                    interactable.Interact();
                }
            }
            else { LimpiarTarget(); }
        }
        else { LimpiarTarget(); }
    }

    private void LimpiarTarget()
    {
        if (lastTarget != null)
        {
            lastTarget.OnHoverExit();
            lastTarget = null;
            if (reticle != null) reticle.localScale = Vector3.one;
        }
    }
}