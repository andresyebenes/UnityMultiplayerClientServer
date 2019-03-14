using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIClientController : MonoBehaviour
{
    public InputField inputIP;
    public InputField inputPort;
    public InputField inputMessage;
    public Client client;


    public void OnClickConnect()
    {
        if (client == null || inputIP == null || inputPort == null || string.IsNullOrEmpty(inputIP.text) || string.IsNullOrEmpty(inputPort.text))
            return;

        client.StartClient();
        client.ConnectToServer(inputIP.text, int.Parse(inputPort.text));
    }


    public void OnClickSendMessage()
    {
        if (client == null || inputMessage == null)
            return;

        client.SendCommandToServer(new Client.Command(Client.Command.Type.CUSTOM_MESSAGE, inputMessage.text));
    }

    public void OnClickSendRandomPosition()
    {
        if (client == null)
            return;

        Vector3 position = new Vector3(Random.Range(0f, 255.0f), Random.Range(0f, 255.0f), Random.Range(0f, 255.0f));
        client.SendCommandToServer(new Client.Command(Client.Command.Type.POSITION, position));
    }


    public void OnClickDisconnect()
    {
        if (client == null)
            return;

        client.Disconnect();
    }
}
