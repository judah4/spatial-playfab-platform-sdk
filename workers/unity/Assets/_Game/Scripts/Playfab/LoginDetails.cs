using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoginDetails : MonoBehaviour
{
    private static LoginDetails _instance;

    public static LoginDetails Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new GameObject("Login Details").AddComponent<LoginDetails>();
            }
            return _instance;
        }
    }

    public string PlayfabSessionTicket;
    public string characterIdRequested;

    public string PlayerId;
    public string PlayerIdentityToken;

    public List<DeploymentTokens> SpatialDeployments = new List<DeploymentTokens>();
    public DeploymentTokens DeploymentSelected;
}

[System.Serializable]
public class DeploymentsResponseData
{
    public DeploymentTokens[] SpatialDeployments;
    public string PlayerIdentityToken;
}

[System.Serializable]
public class DeploymentTokens
{
    public string DeploymentName;
    public string LoginToken;
    public string Tags;

    public bool ContainsVersionTag(string version)
    {
        return ContainsTag($"version_{version.Replace(".", "_")}");
    }

    public bool ContainsTag(string tag)
    {
        if (string.IsNullOrEmpty(Tags))
            return false;

        var tagsSplit = Tags.Split(',');
        for (int cnt = 0; cnt < tagsSplit.Length; cnt++)
        {
            if (tagsSplit[cnt].Equals(tag, StringComparison.CurrentCultureIgnoreCase))
                return true;
        }

        return false;
    }

    public string VersionTag()
    {
        var tagsSplit = Tags.Split(',');
        for (int cnt = 0; cnt < tagsSplit.Length; cnt++)
        {
            if (tagsSplit[cnt].StartsWith("version", StringComparison.CurrentCultureIgnoreCase))
                return tagsSplit[cnt];
        }

        return "version_unversioned";
    }
}
