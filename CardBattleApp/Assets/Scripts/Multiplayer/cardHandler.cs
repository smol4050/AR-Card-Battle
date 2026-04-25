using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class cardHandler : MonoBehaviour
{
    [System.Serializable]
    public class CardData
    {
        public string imageName;
        public GameObject previewPrefab;
        public GameObject finalPrefab;
    }

    [SerializeField] private ARTrackedImageManager trackedImageManager;
    [SerializeField] private List<CardData> cards;
    [SerializeField] private GameObject placeButton;

    private Dictionary<string, CardData> cardDB = new();
    private Dictionary<ARTrackedImage, GameObject> previews = new();

    private ARTrackedImage currentTracked;

    void Awake()
    {
        foreach (var card in cards)
        {
            cardDB[card.imageName] = card;
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
        foreach (var img in args.added)
            HandleImage(img);

        foreach (var img in args.updated)
            HandleImage(img);

        foreach (var img in args.removed)
            RemoveImage(img);

        UpdateButton();
    }

    void HandleImage(ARTrackedImage img)
    {
        string name = img.referenceImage.name;

        if (!cardDB.TryGetValue(name, out CardData card))
            return;

        if (img.trackingState == TrackingState.Tracking)
        {
            currentTracked = img;

            if (!previews.ContainsKey(img))
            {
                GameObject preview = Instantiate(card.previewPrefab, img.transform);
                preview.transform.localPosition = Vector3.zero;
                previews[img] = preview;
            }
        }
        else
        {
            RemoveImage(img);
        }
    }

    void RemoveImage(ARTrackedImage img)
    {
        if (previews.TryGetValue(img, out GameObject preview))
        {
            Destroy(preview);
            previews.Remove(img);
        }
    }

    void UpdateButton()
    {
        bool hasTracking = currentTracked != null &&
                   currentTracked.trackingState != TrackingState.None;

        placeButton.SetActive(hasTracking);
    }

    
    public void PlaceObject()
    {
        if (currentTracked == null) return;

        string name = currentTracked.referenceImage.name;

        if (!cardDB.TryGetValue(name, out CardData card))
            return;

        Vector3 pos = currentTracked.transform.position;
        Quaternion rot = currentTracked.transform.rotation;

        Instantiate(card.finalPrefab, pos, rot);

        
        if (previews.TryGetValue(currentTracked, out GameObject preview))
        {
            Destroy(preview);
            previews.Remove(currentTracked);
        }

        placeButton.SetActive(false);

        
    }
}
