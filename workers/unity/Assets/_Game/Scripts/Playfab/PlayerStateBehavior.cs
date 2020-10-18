using System.Collections;
using System.Collections.Generic;
using Improbable.Gdk.Subscriptions;
using PlayFab;
using PlayFab.ServerModels;
using Spatialplayfab;
using UnityEditor;
using UnityEngine;

public class PlayerStateBehavior : MonoBehaviour
{
    [Require] protected PlayerStateWriter PlayerStateWriter;
    [Require] protected PlayerStateCommandReceiver PlayerStateCommandReceiver;

    private LinkedEntityComponent _spatialOsComponent;


    void OnEnable()
    {
        _spatialOsComponent = GetComponent<LinkedEntityComponent>();


        PlayerStateCommandReceiver.OnValidateRequestReceived += OnValidateRequest;
    }

    private void OnValidateRequest(PlayerState.Validate.ReceivedRequest request)
    {

        if (string.IsNullOrEmpty(request.Payload.SessionTicket))
        {

#if UNITY_EDITOR
            PlayerStateWriter.SendUpdate(new PlayerState.Update() {Validated = true,});
            PlayerStateCommandReceiver.SendValidateResponse(request.RequestId, new ValidateResponse(true));

            return;
#endif

            Debug.LogError("No Session Ticket Sent for Player");
            PlayerStateCommandReceiver.SendValidateResponse(request.RequestId, new ValidateResponse(false));
            return;

        }

        PlayFabServerAPI.AuthenticateSessionTicket(
            new AuthenticateSessionTicketRequest() {SessionTicket = request.Payload.SessionTicket,}, resultAuth =>
            {

                if (resultAuth.UserInfo.PlayFabId != request.Payload.PlayerId)
                {
                    Debug.LogError("Player Id not matching what player sent");
                    PlayerStateCommandReceiver.SendValidateResponse(request.RequestId, new ValidateResponse(false));
                    return;
                }
                PlayFabServerAPI.GetUserData(new GetUserDataRequest(), result =>
                {
                    //used loaded user data or player data from PlayFab

                    var playerState =
                        new PlayerState.Update() { Validated = true, };

                    PlayerStateWriter.SendUpdate(playerState);

                    PlayerStateCommandReceiver.SendValidateResponse(request.RequestId, new ValidateResponse(true));

                }, error =>
                {
                    SendBadResult(request.RequestId);

                    Debug.LogError(error.GenerateErrorReport());
                });

            }, error =>
            {
                Debug.LogError(error.GenerateErrorReport());

                SendBadResult(request.RequestId);

            });
    }

    void SendBadResult(long requestId)
    {
        PlayerStateCommandReceiver.SendValidateResponse(requestId, new ValidateResponse(false));

    }
}
