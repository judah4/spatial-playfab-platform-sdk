using System.Collections;
using System.Collections.Generic;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class SimpleLogin : MonoBehaviour
{

    [SerializeField] private string _successfulLoginScreen;
    [SerializeField] private TMP_InputField _loginInputField;
    [SerializeField] private TMP_InputField _passwordInputField;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Register()
    {
        PlayFabClientAPI.RegisterPlayFabUser(new RegisterPlayFabUserRequest() {Username = _loginInputField.text, Password = _passwordInputField.text, RequireBothUsernameAndEmail = false}, result =>
        {
            OnPlayfabLogin(result.PlayFabId, result.SessionTicket, result.EntityToken.Entity.Id, result.EntityToken.Entity.Type, result.EntityToken.EntityToken);

        }, error =>
        {
            Debug.LogError(error.GenerateErrorReport());

        });
    }

    public void Login()
    {
        PlayFabClientAPI.LoginWithPlayFab(new LoginWithPlayFabRequest() { Username = _loginInputField.text, Password = _passwordInputField.text, },
            result =>
            {

                OnPlayfabLogin(result.PlayFabId, result.SessionTicket, result.EntityToken.Entity.Id, result.EntityToken.Entity.Type, result.EntityToken.EntityToken);
            }, error =>
            {
                Debug.LogError(error.GenerateErrorReport());

            });
    }

    private void OnPlayfabLogin(string playFabId, string sessionTicket, string entityKey, string entityType, string entityToken)
    {
        LoginDetails.Instance.PlayerId = playFabId;
        LoginDetails.Instance.PlayfabSessionTicket = sessionTicket;

        Debug.Log("Logged in.");


        SceneManager.LoadScene(_successfulLoginScreen);
    }
}
