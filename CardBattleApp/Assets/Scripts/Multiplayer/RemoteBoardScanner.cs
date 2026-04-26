using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class RemoteBoardScanner : MonoBehaviour
{
    public ARTrackedImageManager imageManager;
    public GameObject localBoardPrefab;

    private GameObject _instantiatedBoard;

    private void OnEnable() => imageManager.trackablesChanged.AddListener(OnChanged);
    private void OnDisable() => imageManager.trackablesChanged.RemoveListener(OnChanged);

    private void OnChanged(ARTrackablesChangedEventArgs<ARTrackedImage> args)
    {
        foreach (var img in args.added)
        {
            if (img.referenceImage.name == "Battlefield_Marker" && _instantiatedBoard == null)
            {
                _instantiatedBoard = Instantiate(localBoardPrefab, img.transform.position, img.transform.rotation);
                Debug.Log("<color=lime>Tablero Local Posicionado.</color>");
            }
        }
    }
}