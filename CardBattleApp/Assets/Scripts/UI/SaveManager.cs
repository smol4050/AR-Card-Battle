using UnityEngine;
using System.IO;

public class SaveManager : MonoBehaviour
{
    // Singleton para acceder desde cualquier otro script usando SaveManager.Instance
    public static SaveManager Instance { get; private set; }

    public PlayerData DatosActuales { get; private set; }
    private string rutaGuardado;

    private void Awake()
    {
        // Si ya hay un SaveManager, nos destruimos para no duplicar
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this.gameObject); // Hace que sobreviva al cambiar de escena

        // Define dónde se guarda el JSON (Funciona en PC, Android, iOS)
        rutaGuardado = Application.persistentDataPath + "/playerData.json";
        CargarDatos();
    }

    public void GuardarDatos(PlayerData nuevosDatos)
    {
        DatosActuales = nuevosDatos;
        string json = JsonUtility.ToJson(DatosActuales, true); // true formatea el JSON para que sea legible
        File.WriteAllText(rutaGuardado, json);
        Debug.Log("Datos guardados en: " + rutaGuardado);
    }

    public void CargarDatos()
    {
        if (File.Exists(rutaGuardado))
        {
            string json = File.ReadAllText(rutaGuardado);
            DatosActuales = JsonUtility.FromJson<PlayerData>(json);
            Debug.Log("Datos cargados correctamente.");
        }
        else
        {
            DatosActuales = new PlayerData(); // Si no hay archivo, crea datos vacíos
            Debug.Log("No se encontró archivo de guardado. Se crearon datos nuevos.");
        }
    }
}