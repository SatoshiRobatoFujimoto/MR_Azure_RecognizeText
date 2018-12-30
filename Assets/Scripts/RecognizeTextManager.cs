using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class RecognizeTextManager : MonoBehaviour
{

    [Serializable]
    public class Words
    {
        public int[] boundingBox;
        public string text;
    }

    [Serializable]
    public class Lines
    {
        public int[] boundingBox;
        public string text;
        public Words[] words;
    }

    [Serializable]
    public class RecognitionResultData
    {
        public Lines[] lines;
    }

    [Serializable]
    public class RecognizedTextObject
    {
        public string status;
        public RecognitionResultData recognitionResult;
    }

    private string authorizationKey = "<insert-your-api-key>";
    private const string ocpApimSubscriptionKeyHeader = "Ocp-Apim-Subscription-Key";
    private string visionAnalysisEndpoint = "https://westus.api.cognitive.microsoft.com/vision/v2.0/recognizeText";
    private string requestParameters = "mode=Printed"; //"mode=Handwritten"
    private string operationLocation;

    private string imageFilePath;
    internal byte[] imageBytes;
    internal string imagePath;

    public TextMesh DebugText;

    public static RecognizeTextManager instance;

    private void Awake()
    {
        instance = this;
    }

    public IEnumerator RecognizeText()
    {
        WWWForm webForm = new WWWForm();
        string uri = visionAnalysisEndpoint + "?" + requestParameters;
        using (UnityWebRequest unityWebRequest = UnityWebRequest.Post(uri, webForm))
        {
            imageBytes = GetImageAsByteArray(imagePath);
            unityWebRequest.SetRequestHeader("Content-Type", "application/octet-stream");
            unityWebRequest.SetRequestHeader(ocpApimSubscriptionKeyHeader, authorizationKey);
            unityWebRequest.downloadHandler = new DownloadHandlerBuffer();
            unityWebRequest.uploadHandler = new UploadHandlerRaw(imageBytes);
            unityWebRequest.uploadHandler.contentType = "application/octet-stream";

            yield return unityWebRequest.SendWebRequest();

            long responseCode = unityWebRequest.responseCode;
            //Debug.Log(responseCode);
            if(responseCode == 202)
            {
                try
                {
                    var response = unityWebRequest.GetResponseHeaders();
                    operationLocation = response["Operation-Location"];
                    //Debug.Log(response["Operation-Location"]);
                }
                catch (Exception exception)
                {
                    Debug.Log("Json exception.Message: " + exception.Message);
                }

                Boolean poll = true;
                while (poll)
                {
                    using (UnityWebRequest operationLocationRequest = UnityWebRequest.Get(operationLocation))
                    {
                        operationLocationRequest.SetRequestHeader(ocpApimSubscriptionKeyHeader, authorizationKey);
                        yield return operationLocationRequest.SendWebRequest();
                        responseCode = unityWebRequest.responseCode;
                        //Debug.Log("operationLocation : "  + responseCode.ToString());
                        string jsonResponse = null;
                        jsonResponse = operationLocationRequest.downloadHandler.text;
                        //Debug.Log(jsonResponse);
                        RecognizedTextObject recognizedTextObject = new RecognizedTextObject();
                        recognizedTextObject = JsonUtility.FromJson<RecognizedTextObject>(jsonResponse);
                        //Debug.Log(recognizedTextObject.status);
                        if (recognizedTextObject.status == "Succeeded")
                        {
                            string result = null;
                            foreach (Lines line in recognizedTextObject.recognitionResult.lines)
                            {
                                result = result + line.text + "\n";
                            }
                            DebugText.text = result;
                            //Debug.Log(recognizedTextObject.recognitionResult.lines[0].text);
                            poll = false;
                        }
                        if (recognizedTextObject.status == "Failed")
                        {
                            poll = false;
                        }
                    }
                }
            }
            yield return null;
        }
    }

    private static byte[] GetImageAsByteArray(string imageFilePath)
    {
        FileStream fileStream = new FileStream(imageFilePath, FileMode.Open, FileAccess.Read);
        BinaryReader binaryReader = new BinaryReader(fileStream);
        return binaryReader.ReadBytes((int)fileStream.Length);
    }
}

