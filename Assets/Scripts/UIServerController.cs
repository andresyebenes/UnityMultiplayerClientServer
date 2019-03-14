using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIServerController : MonoBehaviour
{
    public InputField inputPort;
    public InputField inputMaxConnections;
    public InputField inputMessage;
    public Server server;
    public GameObject clientListContent;
    public GameObject clientListPrefab;


    private void OnEnable()
    {
        Server.OnClientConnected += Server_OnClientConnected;
        Server.OnClientDisconnected += Server_OnClientDisconnected;
    }


    private void OnDisable()
    {
        Server.OnClientConnected -= Server_OnClientConnected;
        Server.OnClientDisconnected -= Server_OnClientDisconnected;
    }


    public void OnClickStart()
    {
        if (server == null || inputPort == null || string.IsNullOrEmpty(inputMaxConnections.text) || inputPort == null || string.IsNullOrEmpty(inputMaxConnections.text))
            return;

        server.StartServer(int.Parse(inputPort.text), int.Parse(inputMaxConnections.text));
    }


    public void OnClickSendMessage()
    {
        if (server == null || inputMessage == null)
            return;

        Server.Command command = new Server.Command(Server.Command.Type.CUSTOM_MESSAGE, inputMessage.text, -1);
        server.SendCommandToClients(command);
    }

    public void OnClickSendRandomPosition()
    {
        if (server == null)
            return;

        Vector3 position = new Vector3(Random.Range(0f, 255.0f), Random.Range(0f, 255.0f), Random.Range(0f, 255.0f));
        Server.Command command = new Server.Command(Server.Command.Type.POSITION, position, -1);
        server.SendCommandToClients(command);
    }


    private void Server_OnClientConnected(int clientId)
    {
        Debug.Log("Server_OnClientConnected: " + clientId);
        GameObject itemClientObject = Instantiate<GameObject>(clientListPrefab, clientListContent.transform);
        ItemClient itemClient = itemClientObject.GetComponent<ItemClient>();
        
        if (itemClient != null)
            itemClient.textClientId.text = clientId.ToString();
    }


    private void Server_OnClientDisconnected(int clientId)
    {
        Debug.Log("Server_OnClientDisconnected: " + clientId);
        ItemClient itemClientToRemove = null;

        foreach (ItemClient itemClient in ItemClient.allItems)
        {
            if (itemClient.textClientId.text == clientId.ToString())
                itemClientToRemove = itemClient;
        }

        if (itemClientToRemove != null)
            Destroy(itemClientToRemove.gameObject);
    }
}
