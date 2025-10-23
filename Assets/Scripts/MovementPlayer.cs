using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class MovementPlayer : NetworkBehaviour
{

    public float Speed = 5f;
    public NetworkTransport transport;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (IsServer)
        {

        }
        if (!IsOwner || !IsSpawned) return;

        if (Keyboard.current.spaceKey.wasReleasedThisFrame)
        {
            Vector2 mousePos = Input.mousePosition;
            transform.position = mousePos;
            Debug.Log(mousePos.x + " " + mousePos.y);

            //Call_ClientRpc();
            //Call2_ServerRpc();
        }

        var multiplier = Speed * Time.deltaTime;

        if (Keyboard.current.aKey.isPressed)
        {
            transform.position += new Vector3(-multiplier, 0, 0);
        }
        if (Keyboard.current.dKey.isPressed)
        {
            transform.position += new Vector3(multiplier, 0, 0);
        }
        if (Keyboard.current.wKey.isPressed)
        {
            transform.position += new Vector3(0, multiplier, 0);
        }
        if (Keyboard.current.sKey.isPressed)
        {
            transform.position += new Vector3(0, -multiplier, 0);
        }
    }

    [ClientRpc]
    private void Call_ClientRpc()
    {
        Debug.Log("RPC Client");
    }

    [ServerRpc]
    private void Call2_ServerRpc()
    {
        Debug.Log("RPC Server");
    }

}
