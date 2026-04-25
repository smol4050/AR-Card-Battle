using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARTrackedImageManager))]
public class ARRuntimeReferenceImageLibraryLoader : MonoBehaviour
{
    [Serializable]
    public class RuntimeReferenceImage
    {
        public string imageName;
        public Texture2D texture;
        public float widthInMeters;
    }

    public RuntimeReferenceImage[] images;

    private ARTrackedImageManager _imageManager;

    private IEnumerator Start()
    {
        _imageManager = GetComponent<ARTrackedImageManager>();
        _imageManager.enabled = false;

        while (ARSession.state < ARSessionState.Ready)
            yield return null;

        RuntimeReferenceImageLibrary runtimeLibrary;
        try
        {
            runtimeLibrary = _imageManager.CreateRuntimeLibrary();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ARRuntimeReferenceImageLibraryLoader] No se pudo crear la libreria runtime: {ex.Message}");
            yield break;
        }

        if (runtimeLibrary is not MutableRuntimeReferenceImageLibrary mutableLibrary)
        {
            Debug.LogError("[ARRuntimeReferenceImageLibraryLoader] El proveedor AR no soporta librerias mutables.");
            yield break;
        }

        foreach (var image in images)
        {
            if (image == null || image.texture == null || string.IsNullOrWhiteSpace(image.imageName))
                continue;

            try
            {
                mutableLibrary.ScheduleAddImageWithValidationJob(
                    image.texture,
                    image.imageName,
                    image.widthInMeters > 0f ? image.widthInMeters : null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ARRuntimeReferenceImageLibraryLoader] No se pudo agregar '{image?.imageName}': {ex.Message}");
            }
        }

        _imageManager.referenceLibrary = mutableLibrary;
        _imageManager.enabled = true;
        Debug.Log($"[ARRuntimeReferenceImageLibraryLoader] Libreria runtime cargada con {images.Length} imagenes.");
    }
}
