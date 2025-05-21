using System;
using System.Text;
using System.Collections;
using UnityEngine;
using Unity.WebRTC;
using NativeWebSocket;

public class WebRTCAudio : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioSource audioSource;

    [Header("Signaling")]
    public string signalingServerUrl = "ws://54.216.122.197:8080";

    private WebSocket webSocket;
    private RTCPeerConnection peerConnection;
    private AudioStreamTrack audioTrack;

    [Serializable]
    private class SignalingMessage
    {
        public string type;
        public string sdp;
        public string candidate;
        public string sdpMid;
        public int sdpMLineIndex;
    }

    private IEnumerator Start()
    {
        // Connect to signaling server
        webSocket = new WebSocket(signalingServerUrl);

        webSocket.OnOpen += () => Debug.Log("WebSocket Connected.");
        webSocket.OnError += err => Debug.LogError($"WebSocket Error: {err}");
        webSocket.OnClose += e => Debug.Log("WebSocket Closed.");
        webSocket.OnMessage += OnWebSocketMessage;

        var connectTask = webSocket.Connect();
        while (!connectTask.IsCompleted)
            yield return null;

        if (connectTask.IsFaulted)
        {
            Debug.LogError("WebSocket connection failed.");
            yield break;
        }

        // Configure WebRTC peer connection
        var config = new RTCConfiguration
        {
            iceServers = new[]
            {
            new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } }
        }
        };
        peerConnection = new RTCPeerConnection(ref config);

        peerConnection.OnIceCandidate = candidate =>
        {
            if (candidate != null)
            {
                var message = new SignalingMessage
                {
                    type = "candidate",
                    candidate = candidate.Candidate,
                    sdpMid = candidate.SdpMid,
                    sdpMLineIndex = candidate.SdpMLineIndex.GetValueOrDefault()
                };
                SendMessage(JsonUtility.ToJson(message));
            }
        };

        if (audioSource != null)
        {
            audioTrack = new AudioStreamTrack(audioSource);
            peerConnection.AddTrack(audioTrack);
        }
        else
        {
            Debug.LogError("AudioSource is not assigned.");
            yield break;
        }

        var offerOp = peerConnection.CreateOffer();
        yield return offerOp;

        if (offerOp.IsError)
        {
            Debug.LogError($"Offer creation failed: {offerOp.Error.message}");
            yield break;
        }

        var offerDesc = offerOp.Desc;
        var setLocalOp = peerConnection.SetLocalDescription(ref offerDesc);
        yield return setLocalOp;

        if (setLocalOp.IsError)
        {
            Debug.LogError($"SetLocalDescription failed: {setLocalOp.Error.message}");
            yield break;
        }

        var offerMessage = new SignalingMessage
        {
            type = "offer",
            sdp = offerDesc.sdp
        };
        SendMessage(JsonUtility.ToJson(offerMessage));
    }

    private void OnWebSocketMessage(byte[] bytes)
    {
        string json = Encoding.UTF8.GetString(bytes);
        Debug.Log($"Signaling message received: {json}");

        var msg = JsonUtility.FromJson<SignalingMessage>(json);

        if (msg.type == "answer")
        {
            var desc = new RTCSessionDescription
            {
                type = RTCSdpType.Answer,
                sdp = msg.sdp
            };
            StartCoroutine(SetRemoteDescription(desc));
        }
        else if (msg.type == "candidate")
        {
            var candidate = new RTCIceCandidate(new RTCIceCandidateInit
            {
                candidate = msg.candidate,
                sdpMid = msg.sdpMid,
                sdpMLineIndex = msg.sdpMLineIndex
            });
            peerConnection.AddIceCandidate(candidate);
        }
    }

    private IEnumerator SetRemoteDescription(RTCSessionDescription desc)
    {
        var op = peerConnection.SetRemoteDescription(ref desc);
        yield return op;

        if (op.IsError)
            Debug.LogError($"SetRemoteDescription failed: {op.Error.message}");
        else
            Debug.Log("Remote description set successfully.");
    }

    private async void SendMessage(string message)
    {
        if (webSocket != null && webSocket.State == WebSocketState.Open)
        {
            await webSocket.SendText(message);
        }
    }

    private async void OnDestroy()
    {
        audioTrack?.Dispose();
        peerConnection?.Close();
        peerConnection?.Dispose();

        if (webSocket != null)
        {
            await webSocket.Close();
        }
    }

    private void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        webSocket?.DispatchMessageQueue();
#endif
    }
}
