using UnityEngine;
using UnityEngine.Events;

// 1. La Interfaz
public interface IInteractable
{
    void OnHoverEnter();
    void OnHoverExit();
    void Interact();
}