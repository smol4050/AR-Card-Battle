using System;
using System.Collections;
using System.Collections.Generic;
using Niantic.Lightship.SharedAR.Colocalization;
using Unity.Netcode;
using UnityEngine;

public class ARNetworkLog : MonoBehaviour
{
    private SharedSpaceManager sharedSpaceManager;
    private string colocalizationType;
    void Start()
    {
        sharedSpaceManager = FindObjectOfType<SharedSpaceManager>();

        if(sharedSpaceManager != null)
        {
            colocalizationType = sharedSpaceManager.GetColocalizationType().ToString();
            Debug.Log("Colocalization Type: " + colocalizationType);
            sharedSpaceManager.sharedSpaceManagerStateChanged += SharedSpaceManagerStateChanged;
        }
        else
        {
            Debug.LogError("SharedSpaceManager missing in current scene");
            return;
        }



        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedCallback;

        Debug.Log($"Starting SharedAR with {colocalizationType}");
    }

    private void OnClientDisconnectedCallback(ulong clientId)
    {
        if (NetworkManager.Singleton)
        {
            if (NetworkManager.Singleton.IsHost && clientId != NetworkManager.ServerClientId)
            {
                Debug.Log($"Client Disconnected: {clientId}");
                return;
            }
            Debug.Log($"Host Disconnected");
        }
    }

    private void OnClientConnectedCallback(ulong clientId)
    {
        Debug.Log($"Client connected: {clientId}");
    }

    private void OnServerStarted()
    {
        Debug.Log("Netcode server ready");
    }

    private void SharedSpaceManagerStateChanged(SharedSpaceManager.SharedSpaceManagerStateChangeEventArgs args)
    {
        if (args.Tracking)
        {
            
            Debug.Log($"{colocalizationType} TRACKING");
        }
        else
        {
            Debug.Log($"{colocalizationType} NOT TRACKING");
        }
    }
}
