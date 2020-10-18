using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "PlayFabServerAuthSettings", menuName = "SpatialPlayFab/PlayFab Server Auth Settings")]
public class PlayFabServerAuthSettings : ScriptableObject
{
    public string PlayFabKey => _playFabKey;

    [SerializeField] private string _playFabKey;

}
