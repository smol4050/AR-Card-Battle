using UnityEngine;
using UnityEngine.InputSystem;
//using UnityEngine.InputSystem.Sensors; // Necesario para el giroscopio nuevo

public class VRPlayerController : MonoBehaviour
{
    [Header("Configuración de Interacción")]
    public float interactDistance = 15f;
    public Transform reticle;

    private GameObject cameraContainer;
    private IInteractable lastTarget;

    void Start()
    {
        // 1. Configurar Contenedor de Cámara
        cameraContainer = new GameObject("Camera Container");
        cameraContainer.transform.position = transform.position;
        transform.SetParent(cameraContainer.transform);

        // 2. Activar el Giroscopio en el New Input System
        if (AttitudeSensor.current != null)
        {
            InputSystem.EnableDevice(AttitudeSensor.current);
            Debug.Log("Giroscopio (Attitude Sensor) activado correctamente.");
        }
        else
        {
            Debug.LogWarning("¡No se detectó un sensor de actitud (giroscopio) en este dispositivo!");
        }
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
        // Leemos los datos del giroscopio con el nuevo sistema
        if (AttitudeSensor.current != null)
        {
            Quaternion q = AttitudeSensor.current.attitude.ReadValue();

            // La misma conversión para que coincida con el mundo 3D de Unity
            transform.localRotation = new Quaternion(q.x, q.y, -q.z, -q.w);
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

                // Detectar toque en la pantalla
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
            if (reticle != null) reticle.localScale = Vector3.one;
        }
    }
}