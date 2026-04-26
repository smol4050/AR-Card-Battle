using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using Unity.Netcode;

/// <summary>
/// Permite al jugador colocar su tablero localmente en una superficie detectada.
/// </summary>
public class ARBoardPlacer : MonoBehaviour
{
    public ARRaycastManager raycastManager;
    public GameObject boardLocalPrefab; // El tablero visual (no necesita NetworkObject)
    public GameObject deploymentManager; // El objeto que tiene ARDeployFlowP1/P2

    private GameObject _instantiatedBoard;
    private static List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

    private void Start()
    {
        deploymentManager.SetActive(false); // Apagado hasta que haya tablero
    }

    private void Update()
    {
        if (_instantiatedBoard != null) return; // Solo colocamos uno

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                if (raycastManager.Raycast(touch.position, s_Hits, TrackableType.PlaneWithinPolygon))
                {
                    Pose hitPose = s_Hits[0].pose;
                    _instantiatedBoard = Instantiate(boardLocalPrefab, hitPose.position, hitPose.rotation);

                    // Notificar a los flujos de despliegue
                    NotifyDeploymentFlows(_instantiatedBoard.GetComponent<BattlefieldReferences>());
                }
            }
        }
    }

    private void NotifyDeploymentFlows(BattlefieldReferences refs)
    {
        // Activamos la lógica de juego ahora que hay superficie
        deploymentManager.SetActive(true);

        // Buscamos P1 y P2 y les pasamos las referencias del tablero local
        var p1 = deploymentManager.GetComponentInChildren<ARDeployFlowP1>();
        var p2 = deploymentManager.GetComponentInChildren<ARDeployFlowP2>();

        // Necesitamos que ARCardScanner1 tenga un método para setear BoardRefs manualmente
        p1.scanner.SetBoardRefsManually(refs);
        p2.scanner.SetBoardRefsManually(refs);

        Debug.Log("<color=lime>[AR] Tablero local colocado. Flujos de juego activos.</color>");
    }
}