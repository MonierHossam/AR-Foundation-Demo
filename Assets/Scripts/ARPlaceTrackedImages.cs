using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;
using System.IO;

public class ARPlaceTrackedImages : MonoBehaviour
{
    public Texture2D tex;

    public Material m;
    public Material m1;
    public Material m2;

    // Cache AR tracked images manager from ARCoreSession
    private ARTrackedImageManager _trackedImagesManager;

    // List of prefabs - these have to have the same names as the 2D images in the reference image library
    public GameObject[] ArPrefabs;

    public GameObject newg;

    // Internal storage of created prefabs for easier updating
    private readonly Dictionary<string, GameObject> _instantiatedPrefabs = new Dictionary<string, GameObject>();

    // Reference to logging UI element in the canvas
    public UnityEngine.UI.Text Log;

    void Awake()
    {
        _trackedImagesManager = GetComponent<ARTrackedImageManager>();
    }

    void OnEnable()
    {
        _trackedImagesManager.trackedImagesChanged += OnTrackedImagesChanged;
    }

    void OnDisable()
    {
        _trackedImagesManager.trackedImagesChanged -= OnTrackedImagesChanged;
    }

    [ContextMenu("GetColors")]
    public void GetColors()
    {
        Color32[] colors = tex.GetPixels32();

        for (int i = 0; i < colors.Length; i++)
        {
            Debug.Log("colors: " + colors[i]);
        }
        //Dictionary<Color32, int> colors = new Dictionary<Color32, int>();

        //for (int i = 0; i < tex.height; i++)
        //{
        //    for (int j = 0; j < tex.width; j++)
        //    {
        //        Color32 c = tex.GetPixel(j,i);
        //        Debug.Log("COLOR: " + c);

        //        if (!colors.ContainsKey(c))
        //        {
        //            colors.Add(c, 1);
        //        }
        //        else
        //        {
        //            int v = colors[c] + 1;
        //            colors[c] = v;
        //        }
        //    }
        //}

        //int max = 0;
        //Color32 nc = Color.cyan;
        //foreach (var item in colors)
        //{
        //    if(item.Value>max)
        //    {
        //        max = item.Value;
        //        nc = item.Key;
        //    }
        //}

        //Debug.Log("MAX: " + nc.ToString());

        //foreach (var item in colors)
        //{
        //    Debug.Log("COLORS: " + item.Key + " , " + item.Value);
        //}
    }

    private void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        // Good reference: https://forum.unity.com/threads/arfoundation-2-image-tracking-with-many-ref-images-and-many-objects.680518/#post-4668326
        // https://github.com/Unity-Technologies/arfoundation-samples/issues/261#issuecomment-555618182

        // Go through all tracked images that have been added
        // (-> new markers detected)
        foreach (var trackedImage in eventArgs.added)
        {
            // Get the name of the reference image to search for the corresponding prefab
            var imageName = trackedImage.referenceImage.name;
            foreach (var curPrefab in ArPrefabs)
            {
                if (string.Compare(curPrefab.name, imageName, StringComparison.Ordinal) == 0
                    && !_instantiatedPrefabs.ContainsKey(imageName))
                {
                    // Found a corresponding prefab for the reference image, and it has not been instantiated yet
                    // -> new instance, with the ARTrackedImage as parent (so it will automatically get updated
                    // when the marker changes in real-life)

                    //var newPrefab = Instantiate(curPrefab, trackedImage.transform);

                    var newPrefab = Instantiate(newg, trackedImage.transform);
                    // Store a reference to the created prefab
                    _instantiatedPrefabs[imageName] = newPrefab;
                    Log.text = $"{Time.time} -> Instantiated prefab for tracked image (name: {imageName}).\n" +
                               $"newPrefab.transform.parent.name: {newPrefab.transform.parent.name}.\n" +
                               $"guid: {trackedImage.referenceImage.guid}";


                    m.SetColor("_Color", Color.green);
                    m1.SetColor("_Color", Color.green);

                    //getcolors


                    ShowAndroidToastMessage("Instantiated!");
                }
            }
        }

        // Disable instantiated prefabs that are no longer being actively tracked
        foreach (var trackedImage in eventArgs.updated)
        {
            _instantiatedPrefabs[trackedImage.referenceImage.name]
                .SetActive(trackedImage.trackingState == TrackingState.Tracking);
        }

        // Remove is called if the subsystem has given up looking for the trackable again.
        // (If it's invisible, its tracking state would just go to limited initially).
        // Note: ARCore doesn't seem to remove these at all; if it does, it would delete our child GameObject
        // as well.
        foreach (var trackedImage in eventArgs.removed)
        {
            // Destroy the instance in the scene.
            // Note: this code does not delete the ARTrackedImage parent, which was created
            // by AR Foundation, is managed by it and should therefore also be deleted
            // by AR Foundation.
            Destroy(_instantiatedPrefabs[trackedImage.referenceImage.name]);
            // Also remove the instance from our array
            _instantiatedPrefabs.Remove(trackedImage.referenceImage.name);

            // Alternative: do not destroy the instance, just set it inactive
            //_instantiatedPrefabs[trackedImage.referenceImage.name].SetActive(false);

            Log.text = $"REMOVED (guid: {trackedImage.referenceImage.guid}).";
        }
    }

    /// <summary>
    /// Show an Android toast message.
    /// </summary>
    /// <param name="message">Message string to show in the toast.</param>
    private static void ShowAndroidToastMessage(string message)
    {
#if UNITY_ANDROID
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            var unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            if (unityActivity == null) return;
            var toastClass = new AndroidJavaClass("android.widget.Toast");
            unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
            {
                // Last parameter = length. Toast.LENGTH_LONG = 1
                using (var toastObject = toastClass.CallStatic<AndroidJavaObject>("makeText",
                    unityActivity, message, 1))
                {
                    toastObject.Call("show");
                }
            }));
        }
#endif
    }


}
