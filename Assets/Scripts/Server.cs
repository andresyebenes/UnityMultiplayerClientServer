using System.Net;
using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;
using UdpCNetworkDriver = Unity.Networking.Transport.BasicNetworkDriver<Unity.Networking.Transport.IPv4UDPSocket>;
using System.Text;

public class Server : MonoBehaviour
{
    public delegate void ClientConnected(int clientId);
    public static event ClientConnected OnClientConnected;
    public delegate void ClientDisconnected(int clientId);
    public static event ClientDisconnected OnClientDisconnected;

    private UdpCNetworkDriver _serverDriver;
    private NativeList<NetworkConnection> _clientConnections;
    private bool _driverStarted = false;
    private List<Command> _commandQueue = new List<Command>();

    public struct Command
    {
        public Type type;
        public object content;
        public int clientId;

        public enum Type
        {
            KEEP_ALIVE = 1,
            CUSTOM_MESSAGE,
            POSITION,
            ROTATION
        }

        public Command(Type cmdType, object contentValue, int clientIdValue)
        {
            type = cmdType;
            content = contentValue;
            clientId = clientIdValue;
        }
    }

    private void Start()
    {
        //StartServer(9000, 16);
    }


    private void OnDestroy()
    {
        if (!_serverDriver.Equals(default(UdpCNetworkDriver)))
            _serverDriver.Dispose();

        if (!_clientConnections.Equals(default(NativeList<NetworkConnection>)))
        _clientConnections.Dispose();
    }


    private void Update()
    {
        if (!_driverStarted)
            return;

        _serverDriver.ScheduleUpdate().Complete();

        // CleanUpConnections
        for (int i = 0; i < _clientConnections.Length; i++)
        {
            if (!_clientConnections[i].IsCreated)
            {
                _clientConnections.RemoveAtSwapBack(i);
                --i;
            }
        }

        // AcceptNewConnections
        NetworkConnection c;
        while ((c = _serverDriver.Accept()) != default(NetworkConnection))
        {
            _clientConnections.Add(c);
            VisualLog.instance.log("Client connected. ID: " + c.InternalId + ", Hash: " + c.GetHashCode());

            if (OnClientConnected != null)
                OnClientConnected(c.InternalId);
        }

        DataStreamReader stream;
        for (int i = 0; i < _clientConnections.Length; i++)
        {
            if (!_clientConnections[i].IsCreated)
                Assert.IsTrue(true);

            NetworkEvent.Type cmd;

            while ((cmd = _serverDriver.PopEventForConnection(_clientConnections[i], out stream)) != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Data)
                {
                    ProcessCommandReceived(_clientConnections[i], stream);
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    VisualLog.instance.log("Client " + _clientConnections[i].InternalId.ToString() + ", Hash: " + _clientConnections[i].GetHashCode() + " disconnected from server");
                    _clientConnections[i] = default(NetworkConnection);

                    if (OnClientDisconnected != null)
                        OnClientDisconnected(_clientConnections[i].InternalId);
                }
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


    public void StartServer(int port, int maxConnections)
    {
        VisualLog.instance.log("Starting server, port: " + port);
        _serverDriver = new UdpCNetworkDriver(new INetworkParameter[0]);

        if (_serverDriver.Bind(new IPEndPoint(IPAddress.Any, port)) != 0)
            VisualLog.instance.log("Failed to bind to port " + port);
        else
        {
            _serverDriver.Listen();
            VisualLog.instance.log("Server started successfully");
        }

        _clientConnections = new NativeList<NetworkConnection>(maxConnections, Allocator.Persistent);
        _driverStarted = true;
    }


    public void SendCommandToClients(Command command)
    {
        if (!_driverStarted)
        {
            VisualLog.instance.log("Can't send any commands, server not started");
            return;
        }

        if (command.clientId == -1)
        {
            for (int i = 0; i < _clientConnections.Length; i++)
            {
                Command broadcastCommand = new Command(command.type, command.content, _clientConnections[i].InternalId);
                _commandQueue.Add(broadcastCommand);
            }
        }
        else
        {
            _commandQueue.Add(command);
        }
    }


    private void SendCommand(Command command)
    {
        using (var writer = new DataStreamWriter(256, Allocator.Temp))
        {
            writer.Write((uint)command.type);
            switch (command.type)
            {
                case Command.Type.KEEP_ALIVE:
                    //VisualLog.instance.log("Sending " + command.type.ToString() + " to Client ID " + command.clientId.ToString());
                    break;
                case Command.Type.CUSTOM_MESSAGE:
                    VisualLog.instance.log("Sending " + command.type.ToString() + " to Client ID " + command.clientId.ToString() + ". Message: " + (string)command.content);
                    writer.Write(Encoding.UTF8.GetBytes((string)command.content));
                    break;
                case Command.Type.POSITION:
                    Vector3 position = (Vector3)command.content;
                    VisualLog.instance.log("Sending " + command.type.ToString() + " to Client ID " + command.clientId.ToString() + ". Position: " + position);
                    writer.Write(position.x);
                    writer.Write(position.y);
                    writer.Write(position.z);
                    break;
            }

            for (int i = 0; i < _clientConnections.Length; i++)
            {
                if (_clientConnections[i].InternalId == command.clientId)
                    _serverDriver.Send(_clientConnections[i], writer);
            }
        }
    }


    private void ProcessCommandReceived(NetworkConnection connection, DataStreamReader stream)
    {
        var readerCtx = default(DataStreamReader.Context);
        Client.Command.Type commandType = (Client.Command.Type)stream.ReadUInt(ref readerCtx);

        switch(commandType)
        {
            case Client.Command.Type.KEEP_ALIVE:
                //VisualLog.instance.log("Received " + commandType.ToString() + " from Client " + connection.InternalId.ToString() + ", Hash: " + connection.GetHashCode());
                SendCommand(new Command(Command.Type.KEEP_ALIVE, 0, connection.InternalId));
                break;
            case Client.Command.Type.CUSTOM_MESSAGE:
                string message = Encoding.UTF8.GetString(stream.ReadBytesAsArray(ref readerCtx, stream.Length - 4));
                VisualLog.instance.log("Received " + commandType.ToString() + " from Client " + connection.InternalId.ToString() + ", Hash: " + connection.GetHashCode() + ". Message: " + message);
                break;
            case Client.Command.Type.POSITION:
                float x = stream.ReadFloat(ref readerCtx);
                float y = stream.ReadFloat(ref readerCtx);
                float z = stream.ReadFloat(ref readerCtx);
                Vector3 position = new Vector3(x, y, z);
                VisualLog.instance.log("Received " + commandType.ToString() + " from Client " + connection.InternalId.ToString() + ", Hash: " + connection.GetHashCode() + ". Position: " + position);
                break;
            default:
                break;
        }
    }
}
