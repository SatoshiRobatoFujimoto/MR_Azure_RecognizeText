using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.WSA.Input;
using UnityEngine.XR.WSA.WebCam;
using HoloToolkit.Unity.InputModule;

public class ImageCapture : MonoBehaviour, IInputClickHandler
{

    public static ImageCapture instance;
    public int tapsCount;
    private PhotoCapture photoCaptureObject = null;
    private bool currentlyCapturing = false;

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        InputManager.Instance.PushFallbackInputHandler(gameObject);
    }

    public void OnInputClicked(InputClickedEventData eventData)
    {
        if (currentlyCapturing == false)
        {
            currentlyCapturing = true;
            tapsCount++;
            ExecuteImageCaptureAndAnalysis();
        }
    }

    void OnCapturedPhotoToDisk(PhotoCapture.PhotoCaptureResult result)
    {
        photoCaptureObject.StopPhotoModeAsync(OnStoppedPhotoMode);
    }

    void OnStoppedPhotoMode(PhotoCapture.PhotoCaptureResult result)
    {
        photoCaptureObject.Dispose();
        photoCaptureObject = null;
        StartCoroutine(RecognizeTextManager.instance.RecognizeText());
    }

    private void ExecuteImageCaptureAndAnalysis()
    {
        Resolution cameraResolution = PhotoCapture.SupportedResolutions.OrderByDescending((res) => res.width * res.height).First();
        Texture2D targetTexture = new Texture2D(cameraResolution.width, cameraResolution.height);
        PhotoCapture.CreateAsync(false, delegate (PhotoCapture captureObject)
        {
            photoCaptureObject = captureObject;
            CameraParameters camParameters = new CameraParameters();
            camParameters.hologramOpacity = 0.0f; // for MR 0.9f
            camParameters.cameraResolutionWidth = targetTexture.width;
            camParameters.cameraResolutionHeight = targetTexture.height;
            camParameters.pixelFormat = CapturePixelFormat.BGRA32;
            captureObject.StartPhotoModeAsync(camParameters, delegate (PhotoCapture.PhotoCaptureResult result)
            {
                string filename = string.Format(@"CapturedImage{0}.jpg", tapsCount);
                string filePath = Path.Combine(Application.persistentDataPath, filename);
                RecognizeTextManager.instance.imagePath = filePath;
                photoCaptureObject.TakePhotoAsync(filePath, PhotoCaptureFileOutputFormat.JPG, OnCapturedPhotoToDisk);
                currentlyCapturing = false;
            });
        });
    }
}
