using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using UnityEngine;
using UnityEngine.UI;
using Unity.WebRTC;
using System.Collections;

public class WebRTCImageTransfer : MonoBehaviour
{
    public RawImage displayImage; // Assign in inspector

    private RTCPeerConnection peerConnection;
    private RTCDataChannel dataChannel;
    private ClientWebSocket webSocket;
    private CancellationTokenSource cts;

    private IEnumerator Start()
    {
        cts = new CancellationTokenSource();

        var config = new RTCConfiguration
        {
            iceServers = new[] {
                new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } }
            }
        };

        peerConnection = new RTCPeerConnection(ref config);

        // Create data channel
        dataChannel = peerConnection.CreateDataChannel("imageChannel");
        SetupDataChannel();

        // ICE candidate callback
        peerConnection.OnIceCandidate = candidate =>
        {
            if (candidate != null)
                SendIceCandidate(candidate);
        };

        // Connect websocket (wait for connection)
        var wsConnectTask = ConnectWebSocket("ws://54.216.122.197:8080");
        while (!wsConnectTask.IsCompleted)
            yield return null;
        if (wsConnectTask.IsFaulted)
        {
            Debug.LogError("WebSocket connection failed");
            yield break;
        }
        Debug.Log("no error");
        // Create and send offer SDP
        yield return StartCoroutine(CreateOfferAndSend());

        // Start receiving signaling messages async without blocking
        _ = ReceiveSignalingMessages();

        // Keep alive
        while (!cts.Token.IsCancellationRequested)
            yield return null;
    }

    private void SetupDataChannel()
    {
        dataChannel.OnOpen = () =>
        {
            Debug.Log("Data channel opened");

            // Send image bytes when channel opens
            string imagePath = Application.streamingAssetsPath;
            if (System.IO.File.Exists(imagePath))
            {
                byte[] imageBytes = System.IO.File.ReadAllBytes(imagePath);
                dataChannel.Send(imageBytes);
                Debug.Log($"Sent image data ({imageBytes.Length} bytes)");
            }
            else
            {
                Debug.LogError($"Image file not found at path: {imagePath}");
            }
        };

        dataChannel.OnMessage = bytes =>
        {
            Debug.Log($"Received data length: {bytes.Length}");
            var tex = new Texture2D(2, 2);
            if (tex.LoadImage(bytes))
            {
                if (displayImage != null)
                    displayImage.texture = tex;
            }
            else
            {
                Debug.LogError("Failed to load received image data");
            }
        };
    }

    private IEnumerator CreateOfferAndSend()
    {
        var offerOp = peerConnection.CreateOffer();
        yield return offerOp;

        if (offerOp.IsError)
        {
            Debug.LogError($"CreateOffer failed: {offerOp.Error.message}");
            yield break;
        }

        var desc = offerOp.Desc;
        var setLocalOp = peerConnection.SetLocalDescription(ref desc);
        yield return setLocalOp;

        if (setLocalOp.IsError)
        {
            Debug.LogError($"SetLocalDescription failed: {setLocalOp.Error.message}");
            yield break;
        }

        SendSdp(desc);
    }

    private IEnumerator SetRemoteDescription(RTCSessionDescription desc)
    {
        var op = peerConnection.SetRemoteDescription(ref desc);
        yield return op;

        if (op.IsError)
            Debug.LogError($"SetRemoteDescription failed: {op.Error.message}");
        else
            Debug.Log("SetRemoteDescription succeeded");
    }

    private async Task ConnectWebSocket(string uri)
    {
        webSocket = new ClientWebSocket();
        try
        {
            await webSocket.ConnectAsync(new Uri(uri), CancellationToken.None);
            Debug.Log("Connected to signaling server");
        }
        catch (Exception e)
        {
            Debug.LogError($"WebSocket connection failed: {e.Message}");
            throw;
        }
    }

    private async Task ReceiveSignalingMessages()
    {
        var buffer = new byte[8192];
        try
        {
            while (webSocket.State == WebSocketState.Open && !cts.Token.IsCancellationRequested)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                    break;
                }
                var msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                HandleSignalingMessage(msg);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("WebSocket receive error: " + e.Message);
        }
    }

    private void HandleSignalingMessage(string json)
    {
        Debug.Log("Received signaling message: " + json);

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
            var candidateInit = new RTCIceCandidateInit
            {
                candidate = msg.candidate,
                sdpMid = msg.sdpMid,
                sdpMLineIndex = msg.sdpMLineIndex
            };
            peerConnection.AddIceCandidate(new RTCIceCandidate(candidateInit));
        }
    }

    private async void SendWebSocketMessage(string message)
    {
        if (webSocket != null && webSocket.State == WebSocketState.Open)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cts.Token);
        }
    }

    private void SendSdp(RTCSessionDescription desc)
    {
        var msg = new SignalingMessage
        {
            type = desc.type == RTCSdpType.Offer ? "offer" : "answer",
            sdp = desc.sdp
        };
        SendWebSocketMessage(JsonUtility.ToJson(msg));
    }

    private void SendIceCandidate(RTCIceCandidate candidate)
    {
        var msg = new SignalingMessage
        {
            type = "candidate",
            candidate = candidate.Candidate,
            sdpMid = candidate.SdpMid,
            sdpMLineIndex = candidate.SdpMLineIndex ?? 0
        };
        SendWebSocketMessage(JsonUtility.ToJson(msg));
    }

    private void OnDestroy()
    {
        cts?.Cancel();

        dataChannel?.Dispose();
        peerConnection?.Close();
        peerConnection?.Dispose();

        webSocket?.Dispose();
    }

    [Serializable]
    private class SignalingMessage
    {
        public string type;
        public string sdp;
        public string candidate;
        public string sdpMid;
        public int sdpMLineIndex;
    }
}
