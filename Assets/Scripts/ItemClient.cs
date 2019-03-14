using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemClient : MonoBehaviour
{
    public Text textClientId;

    public static List<ItemClient> allItems = new List<ItemClient>();


    private void Start()
    {
        allItems.Add(this);
    }


    private void OnDestroy()
    {
        allItems.Remove(this);
    }
}
