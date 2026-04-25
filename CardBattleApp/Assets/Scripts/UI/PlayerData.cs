using UnityEngine;

[System.Serializable] // Necesario para que JsonUtility lo pueda convertir a texto
public class PlayerData
{
    public string nombre;
    public string apodo;
    public string personajeFavorito;
    public int edad;
    public int idFotoPerfil; // Aquí guardaremos el número random (1, 2 o 3)
}
