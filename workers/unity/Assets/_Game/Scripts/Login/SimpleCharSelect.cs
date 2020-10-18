using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleCharSelect : MonoBehaviour
{

    [SerializeField] private string _gameScene;
    [SerializeField]
    string _platformApiUrl;

    [SerializeField]
    TMP_Text _statusText;

    // Start is called before the first frame update
    void Start()
    {
        _statusText.text = "Checking deployments...";
        StartCoroutine(SendGetDeployments());
    }


    public void Connect()
    {
        SceneManager.LoadScene(_gameScene);
    }

    public void ConnectLocal()
    {
        LoginDetails.Instance.DeploymentSelected = new DeploymentTokens()
        {
            DeploymentName = "Local",
        };
        Connect();
    }

    IEnumerator SendGetDeployments()
    {

        if (string.IsNullOrEmpty(LoginDetails.Instance.PlayerId))
            yield break;

        UnityEngine.Networking.UnityWebRequest webRequest = new UnityEngine.Networking.UnityWebRequest($"{_platformApiUrl}/deployments", "GET");
        webRequest.redirectLimit = 0;

        webRequest.downloadHandler = (UnityEngine.Networking.DownloadHandler) new UnityEngine.Networking.DownloadHandlerBuffer();
        webRequest.SetRequestHeader("Authorization", "Bearer " + LoginDetails.Instance.PlayfabSessionTicket);
        webRequest.useHttpContinue = false;

        yield return webRequest.SendWebRequest();

        if (webRequest.isNetworkError)
        {
            _statusText.text = "Error.";
            Debug.LogError("Deployments " + webRequest.error);
        }
        else
        {
            Debug.Log("Deployments " + webRequest.downloadHandler.text);
            //Debug.Log(webRequest.responseCode + " " + credential);
            if (webRequest.responseCode == 200)
            {
                var deployments = JsonUtility.FromJson<DeploymentsResponseData>(webRequest.downloadHandler.text);
                //credential = credential.Substring(1, credential.Length - 2);

                LoginDetails.Instance.SpatialDeployments.Clear();
                LoginDetails.Instance.SpatialDeployments.AddRange(deployments.SpatialDeployments);

                if(LoginDetails.Instance.SpatialDeployments.Count > 0)
                {
                    LoginDetails.Instance.DeploymentSelected = LoginDetails.Instance.SpatialDeployments[0];
                }

                LoginDetails.Instance.PlayerIdentityToken = deployments.PlayerIdentityToken;
                //refresh view
                _statusText.text = "Ready!";
            }
            else
            {
                _statusText.text = "Error.";
                Debug.LogError($"Issue pulling deployments {webRequest.url} {webRequest.responseCode} {webRequest.error} {(webRequest.downloadHandler != null ? webRequest.downloadHandler.text : null)}");

            }
        }

    }
}
