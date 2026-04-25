using UnityEngine;

public class ControlGiroscopio360 : MonoBehaviour
{
    private GameObject cameraContainer;
    private Quaternion rotation;

    void Start()
    {
        // Creamos un contenedor para la cámara para corregir la orientación inicial
        cameraContainer = new GameObject("Camera Container");
        cameraContainer.transform.position = transform.position;
        transform.SetParent(cameraContainer.transform);

        // Activamos el giroscopio del celular
        Input.gyro.enabled = true;
    }

    void Update()
    {
        //if (UIManager.Instance != null && UIManager.Instance.HayAlgunaVentanaAbierta())
        //{
        //    return; // No rotar si hay menús abiertos
        //}

        // Aplicamos la rotación del giroscopio a la cámara
        // Unity usa un sistema de coordenadas diferente al sensor, por eso se invierte
        transform.localRotation = GyroToUnity(Input.gyro.attitude);
    }

    private static Quaternion GyroToUnity(Quaternion q)
    {
        // Esta conversión es estándar para que el movimiento del cel coincida con la vista
        return new Quaternion(q.x, q.y, -q.z, -q.w);
    }
}