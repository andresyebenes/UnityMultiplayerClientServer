using System.Net;
using Unity.Collections;
using UnityEngine;
using Unity.Networking.Transport;
using System.Collections.Generic;
using System.Text;

using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;

public class Client : MonoBehaviour
{
    public float keepAliveInterval = 5f;

    private UdpCNetworkDriver _clientDriver;
    private NetworkConnection _serverConnection;
    private bool _driverStarted = false;
    private bool _connectionSent = false;
    private bool _connectedToServer = false;
    private List<Command> _commandQueue = new List<Command>();

    public struct Command
    {
        public Type type;
        public object content;

        public enum Type
        {
            KEEP_ALIVE = 1,
            CUSTOM_MESSAGE,
            POSITION,
            ORIENTATION
        }

        public Command(Type cmdType, object contentValue)
        {
            type = cmdType;
            content = contentValue;
        }
    }

    private void OnDestroy()
    {
        if (_connectedToServer)
            Disconnect();

        if (!_clientDriver.Equals(default(UdpCNetworkDriver)))
            _clientDriver.Dispose();
    }


    private void Update()
    {
        if (!_driverStarted || !_connectionSent)
            return;

        _clientDriver.ScheduleUpdate().Complete();

        if (!_serverConnection.IsCreated)
            return;

        DataStreamReader stream;
        NetworkEvent.Type cmd;

        while ((cmd = _serverConnection.PopEvent(_clientDriver, out stream)) !=
               NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                VisualLog.instance.log("Connected to the server");
                _connectedToServer = true;
                InvokeRepeating("KeepAlive", keepAliveInterval, keepAliveInterval);
            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                ProcessCommandReceived(stream);
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                Disconnect();
                /*VisualLog.instance.log("Client got disconnected from server");
                _serverConnection = default(NetworkConnection);
                _connectedToServer = false;*/
            }
        }

        if (_commandQueue.Count > 0)
        {
            for (int i = 0; i < _commandQueue.Count; i++)
            {
                SendCommand(_commandQueue[i]);
            }

            _commandQueue.Clear();
            _commandQueue = new List<Command>();
        }
    }


    public void StartClient()
    {
        if (_driverStarted)
        {
            VisualLog.instance.log("Client driver already started");
            return;
        }

        VisualLog.instance.log("Starting client");
        _clientDriver = new UdpCNetworkDriver(new INetworkParameter[0]);
        _serverConnection = default(NetworkConnection);
        _driverStarted = true;
    }


    public void ConnectToServer(string ip, int port)
    {
        VisualLog.instance.log("Connecting to server IP: " + ip + ", port: " + port);
        try
        {
            var endpoint = new IPEndPoint(IPAddress.Parse(ip), port);
            _serverConnection = _clientDriver.Connect(endpoint);
            _connectionSent = true;
        } catch
        {
            VisualLog.instance.log("Invalid connection");
        }
    }


    public void Disconnect()
    {
        if (!_connectedToServer)
            return;

        _serverConnection.Disconnect(_clientDriver);
        _serverConnection = default(NetworkConnection);
        _connectedToServer = false;
        CancelInvoke("KeepAlive");
        VisualLog.instance.log("Disconnected from server");
    }


    public void SendCommandToServer(Command command)
    {
        if (!_driverStarted || !_connectedToServer)
        {
            VisualLog.instance.log("Can't send any commands, not connected to server");
            return;
        }

        _commandQueue.Add(command);
    }


    private void KeepAlive()
    {
        Command command = new Command(Command.Type.KEEP_ALIVE, 0);
        SendCommand(command);
    }


    private void SendCommand(Command command)
    {
        using (var writer = new DataStreamWriter(256, Allocator.Temp))
        {
            writer.Write((uint)command.type);

            switch (command.type)
            {
                case Command.Type.KEEP_ALIVE:
                    //VisualLog.instance.log("Sending " + command.type.ToString());
                    break;
                case Command.Type.CUSTOM_MESSAGE:
                    VisualLog.instance.log("Sending " + command.type.ToString() + ". Message: " + (string)command.content);
                    writer.Write(Encoding.UTF8.GetBytes((string)command.content));
                    break;
                case Command.Type.POSITION:
                    Vector3 position = (Vector3)command.content;
                    VisualLog.instance.log("Sending " + command.type.ToString() + ". Position: " + position);
                    writer.Write(position.x);
                    writer.Write(position.y);
                    writer.Write(position.z);
                    break;
            }

            _serverConnection.Send(_clientDriver, writer);
        }
    }


    private void ProcessCommandReceived(DataStreamReader stream)
    {
        var readerCtx = default(DataStreamReader.Context);
        Server.Command.Type commandType = (Server.Command.Type)stream.ReadUInt(ref readerCtx);

        switch (commandType)
        {
            case Server.Command.Type.KEEP_ALIVE:
                //VisualLog.instance.log("Received " + commandType.ToString() + " from Server");
                break;
            case Server.Command.Type.CUSTOM_MESSAGE:
                string message = Encoding.UTF8.GetString(stream.ReadBytesAsArray(ref readerCtx, stream.Length - 4));
                VisualLog.instance.log("Received " + commandType.ToString() + " from Server. Message: " + message);
                break;
            case Server.Command.Type.POSITION:
                float x = stream.ReadFloat(ref readerCtx);
                float y = stream.ReadFloat(ref readerCtx);
                float z = stream.ReadFloat(ref readerCtx);
                Vector3 position = new Vector3(x, y, z);
                VisualLog.instance.log("Received " + commandType.ToString() + " from Server. Position: " + position);
                break;
            default:
                break;
        }
    }
}
