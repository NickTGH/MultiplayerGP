using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class PlayerControls : NetworkBehaviour
{

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    

    // Update is called once per frame
    void Update()
    {
        //On left click place ship if game state is correct 
    }
    
    private void PlacementKey_Clicked()
    {
        //Start placement from grid Manager
        //getmousePosition
        GridManager.Instance.GetMousePosition();
    }
}
