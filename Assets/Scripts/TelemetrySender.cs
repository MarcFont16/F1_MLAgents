using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class TelemetrySender : MonoBehaviour
{
    // network config
    public string ipAddress = "127.0.0.1";
    public int port = 5005;

    private UdpClient udpClient;
    private F1Agent agent;

    void Start()
    {
        udpClient = new UdpClient();
        agent = GetComponent<F1Agent>();
    }

    void Update()
    {
        if (agent == null) return;

        // build simple json payload
        float friction = agent.trackMaterial != null ? agent.trackMaterial.dynamicFriction : 0.8f;
        
        string json = $@"{{
            ""speed"": {agent.currentActualSpeed},
            ""steering"": {agent.currentTurnInput},
            ""friction"": {friction}
        }}";

        // send udp packet to node.js
        byte[] data = Encoding.UTF8.GetBytes(json);
        udpClient.Send(data, data.Length, ipAddress, port);
    }

    void OnApplicationQuit()
    {
        // clean up port
        if (udpClient != null) udpClient.Close();
    }
}