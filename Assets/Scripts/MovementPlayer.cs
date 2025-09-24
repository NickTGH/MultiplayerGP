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
            if (Keyboard.current.spaceKey.isPressed)
            {
                transform.position = Input.mousePosition;
            }
        }
        if (!IsOwner || !IsSpawned) return;

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
}
