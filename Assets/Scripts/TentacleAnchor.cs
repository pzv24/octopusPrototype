using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class TentacleAnchor : MonoBehaviour
{
    [SerializeField] private Rigidbody2D _playerRB;
    [SerializeField] private bool _isConnected = true;

    [SerializeField, ReadOnly] private Vector2 _fromPlayerVector = Vector2.zero;

    public Vector2 FromPlayerVector { get { return _fromPlayerVector; } }

    private void FixedUpdate()
    {
        if( _isConnected)
        {
            _fromPlayerVector = PlayerVector();
        }
    }

    private Vector2 PlayerVector()
    {
        Vector2 playerV2 = new Vector2(_playerRB.gameObject.transform.position.x, _playerRB.gameObject.transform.position.y);
        Vector2 currentV2 = new Vector2(transform.position.x, transform.position.y);
        return currentV2 - playerV2;
    }

    public void DeactivateTentacle()
    {
        _isConnected = false;
        gameObject.SetActive(false);
    }

    public void ActivateTentacle(Vector2 anchorPosition)
    {
        _isConnected = true;
        gameObject.SetActive(true);
        transform.position = new Vector3(anchorPosition.x, anchorPosition.y, transform.position.z);
    }
}
