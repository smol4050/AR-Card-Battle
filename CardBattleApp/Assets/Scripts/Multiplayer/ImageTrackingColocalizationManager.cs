using Niantic.Lightship.SharedAR.Colocalization;
using System.Xml.Serialization;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

[RequireComponent(typeof(SharedSpaceFactory))]
public class ImageTrackingColocalizationManager : MonoBehaviour,IColocalizationRoom
{
    [SerializeField] private Texture2D targetImage;
    [SerializeField] private float targetWidthPhysicalSize;

    [SerializeField] private GameObject connectionUI;
    [SerializeField] private TMP_InputField pinInputField;
    [SerializeField] private TextMeshProUGUI pinText;

    //[SerializeField] private ARTrackedImageManager trackedImageManager;
    [SerializeField] private cardScanner cardScanner;

    private SharedSpaceFactory sharedSpaceFactory;
    private bool startAsHost;

    private void OnEnable()
    {
        sharedSpaceFactory = GetComponent<SharedSpaceFactory>();
        sharedSpaceFactory.OnSharedSpaceTracking += SharedSpaceStartTracking;
    }

    private void OnDisable()
    {
        sharedSpaceFactory.OnSharedSpaceTracking -= SharedSpaceStartTracking;
    }

    void Start()
    {
        connectionUI.SetActive(true);
        //trackedImageManager.enabled = false;
        var managers = FindObjectsOfType<ARTrackedImageManager>();
        Debug.Log("Managers encontrados: " + managers.Length);
    }

    public void StartNetworkAsHostOrClient(bool isHost)
    {
        startAsHost = isHost;
        CreateRoomOptions();
    }

    public void CreateRoomOptions()
    {
        string roomName = pinInputField.text;
        var imageTrackingOptions = ISharedSpaceTrackingOptions.CreateImageTrackingOptions(targetImage, targetWidthPhysicalSize);
        sharedSpaceFactory.CreateRoom(imageTrackingOptions, roomName);

        connectionUI.SetActive(false);
        pinText.gameObject.SetActive(true);
        pinText.text = $"PIN: {roomName}";
        Debug.Log(roomName);

        
    }

    public void SharedSpaceStartTracking()
    {
        //trackedImageManager.enabled = true;
        cardScanner.sharedSpaceReady = true;

        if (startAsHost)
        {
            StartHost();
        }
        else
        {
            StartClient();
        }
    }

    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
    }

    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
    }

    
    

    
}
