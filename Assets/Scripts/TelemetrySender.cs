using System.Net.Sockets;
using System.Text;
using UnityEngine;

[System.Serializable]
public class TelemetryData
{
    public float speed;
    public float steering;
    public float friction;
}

public class TelemetrySender : MonoBehaviour
{
    // network config
    public string ipAddress = "172.26.18.98";
    public int port = 5005;

    private UdpClient udpClient;
    private F1Agent agent;

    void Start()
    {
        udpClient = new UdpClient();
        agent = GetComponentInChildren<F1Agent>();
    }

    void Update()
    {
        if (agent == null) 
        {
            Debug.LogWarning("telemetry: f1agent not found");
            return;
        }
        float currentFriction = agent.trackMaterial != null ? agent.trackMaterial.dynamicFriction : 0.8f;

        // create data object
        TelemetryData dataObj = new TelemetryData
        {
            speed = agent.currentActualSpeed,
            steering = agent.currentTurnInput,
            friction = currentFriction
        };

        // serialize safely with unity json
        string json = JsonUtility.ToJson(dataObj);
        byte[] data = Encoding.UTF8.GetBytes(json);
        
        udpClient.Send(data, data.Length, ipAddress, port);
    }

    void OnApplicationQuit()
    {
        if (udpClient != null) udpClient.Close();
    }
}