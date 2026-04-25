using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class cardScanner : MonoBehaviour
{
    [System.Serializable]
    public class CardData
    {
        public string imageName;
        public GameObject previewPrefab;
        public GameObject networkPrefab;
    }

    [SerializeField] private ARTrackedImageManager trackedImageManager;
    [SerializeField] private List<CardData> cards;
    [SerializeField] private GameObject placeButton;

    private Dictionary<string, CardData> cardDatabase;

    private Dictionary<ARTrackedImage, GameObject> previews = new();
    private Dictionary<ARTrackedImage, CardData> trackedCards = new();

    public bool sharedSpaceReady = false;

    void Awake()
    {
        cardDatabase = new Dictionary<string, CardData>();

        foreach (var card in cards)
        {
            cardDatabase[card.imageName] = card;
        }
    }

    void Start()
    {
        placeButton.SetActive(false);
    }


    void OnEnable()
    {
        trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    void OnDisable()
    {
        trackedImageManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs args)
    {
        //if (!sharedSpaceReady) return;

        foreach (var trackedImage in args.added)
        {
            SpawnPreview(trackedImage);
        }

        foreach (var trackedImage in args.updated)
        {
            UpdatePreview(trackedImage);
        }

        foreach (var trackedImage in args.removed)
        {
            RemovePreview(trackedImage);
        }
    }

    void SpawnPreview(ARTrackedImage trackedImage)
    {
        string imageName = trackedImage.referenceImage.name;

        if (!cardDatabase.TryGetValue(imageName, out CardData card))
        {
            Debug.LogWarning("Carta no registrada: " + imageName);
            return;
        }

        GameObject preview = Instantiate(card.previewPrefab, trackedImage.transform);
        preview.transform.localPosition = Vector3.zero;

        previews[trackedImage] = preview;
        trackedCards[trackedImage] = card;
    }

    void UpdatePreview(ARTrackedImage trackedImage)
    {
        if (previews.TryGetValue(trackedImage, out GameObject preview))
        {
            bool isTracking = trackedImage.trackingState == TrackingState.Tracking;

            preview.SetActive(isTracking);

            UpdateButtonVisibility();
        }
    }

    void RemovePreview(ARTrackedImage trackedImage)
    {
        if (previews.TryGetValue(trackedImage, out GameObject preview))
        {
            Destroy(preview);
            previews.Remove(trackedImage);
            trackedCards.Remove(trackedImage);

            UpdateButtonVisibility();
        }
    }

    void UpdateButtonVisibility()
    {
        foreach (var kvp in previews)
        {
            if (kvp.Key.trackingState == TrackingState.Tracking)
            {
                placeButton.SetActive(true);
                return;
            }
        }

        placeButton.SetActive(false);
    }

    public void FixFirstDetectedCard()
    {
        foreach (var kvp in previews)
        {
            ARTrackedImage trackedImage = kvp.Key;
            GameObject preview = kvp.Value;
            CardData card = trackedCards[trackedImage];

            Vector3 pos = preview.transform.position;
            Quaternion rot = preview.transform.rotation;

            SubmitSpawnRequestServerRpc(card.imageName, pos, rot);

            Destroy(preview);
            previews.Remove(trackedImage);
            trackedCards.Remove(trackedImage);

            break;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void SubmitSpawnRequestServerRpc(string imageName, Vector3 pos, Quaternion rot)
    {
        if (!cardDatabase.TryGetValue(imageName, out CardData card))
            return;

        GameObject obj = Instantiate(card.networkPrefab, pos, rot);
        obj.GetComponent<NetworkObject>().Spawn();
    }
}
