using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

// Unity -> TCP localhost -> MATLAB -> ROS 2 /hvac/setpoint
// Simulink ROS 2 telemetry -> MATLAB -> TCP localhost -> Unity
// Uses only Unity/.NET standard networking. No Ubuntu/Docker required.
[RequireComponent(typeof(HVACDashboardUI))]
public class HVACTcpWindowsBridge : MonoBehaviour
{
    public string host = "127.0.0.1";
    public int port = 16555;
    public bool showConnectionOverlay = true;

    [Serializable]
    private class BridgePacket
    {
        public string type;
        public float cabinTempC;
        public bool rosData;
        public bool heaterOn;
        public bool acOn;
        public bool maintainOn;
        public bool sensorFault;
        public bool commLost;
        public bool hasCommLost;
        public float setpoint;
        public string message;
    }

    private HVACDashboardUI dashboard;
    private Thread worker;
    private volatile bool running;
    private volatile bool connected;
    private volatile bool hasRealRosData;
    private volatile float requestedSetpoint = 22.5f;
    private volatile string connectionError = "";
    private readonly ConcurrentQueue<string> incoming = new ConcurrentQueue<string>();

    private void Start()
    {
        dashboard = GetComponent<HVACDashboardUI>();
        requestedSetpoint = dashboard.targetTempC;
        running = true;
        worker = new Thread(NetworkLoop) { IsBackground = true, Name = "HVAC-MATLAB-TCP" };
        worker.Start();
    }

    private void Update()
    {
        if (dashboard == null) return;
        requestedSetpoint = Mathf.Clamp(dashboard.targetTempC, 16f, 30f);
        dashboard.rosConnected = connected && hasRealRosData;

        string packetText;
        int processed = 0;
        while (processed < 20 && incoming.TryDequeue(out packetText))
        {
            processed++;
            try
            {
                BridgePacket packet = JsonUtility.FromJson<BridgePacket>(packetText);
                if (packet == null || packet.type != "telemetry") continue;
                hasRealRosData = packet.rosData;
                if (packet.rosData)
                {
                    // The real Simulink/ROS 2 model is the source of truth.
                    dashboard.cabinTempC = packet.cabinTempC;
                    dashboard.heaterOn = packet.heaterOn;
                    dashboard.acOn = packet.acOn;
                    dashboard.maintainOn = packet.maintainOn;
                    dashboard.sensorFault = packet.sensorFault;
                    if (packet.hasCommLost)
                        dashboard.commLost = packet.commLost;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("HVAC TCP : JSON recu invalide : " + e.Message);
            }
        }
    }

    private void NetworkLoop()
    {
        byte[] receiveBuffer = new byte[2048];
        StringBuilder pending = new StringBuilder();
        while (running)
        {
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    client.NoDelay = true;
                    client.Connect(host, port);
                    using (NetworkStream stream = client.GetStream())
                    {
                        connected = true;
                        connectionError = "";
                        pending.Length = 0;
                        DateTime lastSend = DateTime.MinValue;
                        while (running && client.Connected)
                        {
                            if ((DateTime.UtcNow - lastSend).TotalSeconds >= 1.0)
                            {
                                float target = requestedSetpoint;
                                string json = "{\"type\":\"setpoint\",\"value\":" +
                                    target.ToString("0.0", CultureInfo.InvariantCulture) + "}\n";
                                byte[] bytes = Encoding.UTF8.GetBytes(json);
                                stream.Write(bytes, 0, bytes.Length);
                                lastSend = DateTime.UtcNow;
                            }

                            while (stream.DataAvailable)
                            {
                                int received = stream.Read(receiveBuffer, 0, receiveBuffer.Length);
                                if (received <= 0) throw new Exception("Connexion TCP fermee");
                                for (int i = 0; i < received; ++i)
                                {
                                    char ch = (char)receiveBuffer[i];
                                    if (ch == '\n')
                                    {
                                        if (pending.Length > 0)
                                            incoming.Enqueue(pending.ToString().TrimEnd('\r'));
                                        pending.Length = 0;
                                    }
                                    else if (pending.Length < 8192)
                                    {
                                        pending.Append(ch);
                                    }
                                    else
                                    {
                                        throw new Exception("Trame TCP trop longue");
                                    }
                                }
                            }
                            // Detect graceful disconnect even if no data is available.
                            if (client.Client.Poll(0, SelectMode.SelectRead) &&
                                client.Client.Available == 0)
                                throw new Exception("MATLAB a ferme la connexion TCP");
                            Thread.Sleep(30);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                connectionError = ex.Message;
            }
            connected = false;
            hasRealRosData = false;
            if (running) Thread.Sleep(1200);
        }
    }

    private void OnGUI()
    {
        if (!showConnectionOverlay) return;
        GUI.Box(new Rect(18f, 326f, 390f, 64f),
            "PASSERELLE WINDOWS (sans Docker)\n" +
            "TCP MATLAB : " + (connected ? "CONNECTE" : "NON CONNECTE") +
            " | ROS 2 : " + (hasRealRosData ? "DONNEES RECUES" : "EN ATTENTE"));
    }

    private void OnDestroy() { StopWorker(); }
    private void OnApplicationQuit() { StopWorker(); }

    private void StopWorker()
    {
        running = false;
        if (worker != null && worker.IsAlive)
            worker.Join(350);
        worker = null;
    }
}
