using System.Collections;
using System.Collections.Generic;
using Improbable.Gdk.Subscriptions;
using Spatialplayfab;
using UnityEngine;

public class PlayerStateVisualizer : MonoBehaviour
{
    [Require] protected ClientAuthorityCheckWriter ClientAuthorityCheck;
    [Require] protected PlayerStateReader PlayerStateReader;
    [Require] protected PlayerStateCommandSender PlayerStateCommandSender;

    private LinkedEntityComponent _spatialOsComponent;


    void OnEnable()
    {
        _spatialOsComponent = GetComponent<LinkedEntityComponent>();

        if (PlayerStateReader.Data.Validated == false)
        {
            PlayerStateCommandSender.SendValidateCommand(_spatialOsComponent.EntityId, new ValidatePlayer(LoginDetails.Instance.PlayerId, LoginDetails.Instance.PlayfabSessionTicket));
        }
    }
}
