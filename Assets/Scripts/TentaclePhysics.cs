using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class TentaclePhysics : MonoBehaviour
{

    [SerializeField] private List<TentacleAnchor> _tentacles = new List<TentacleAnchor>();
    [SerializeField, ReadOnly] private Vector2 _finalVector = Vector2.zero;
    private Rigidbody2D _rigidBody;
    [SerializeField] private float _implulseMagnitude = 1;
    //public void FindIndividualVectors()
    //{
    //    _tentacleVectors.Clear();
    //    for (int i = 0; i < _tentacles.Count; i++)
    //    {
    //        Vector2 anchorLocation = new Vector2(_tentacles[i].transform.position.x, _tentacles[i].transform.position.y);
    //        _tentacleVectors.Add(anchorLocation - new Vector2(transform.position.x, transform.position.y));
    //    }
    //}

    private void Start()
    {
        _rigidBody = GetComponent<Rigidbody2D>();
    }
    private void FindFinal()
    {
        _finalVector = Vector2.zero;
        for (int i = 0; i < _tentacles.Count; i++)
        {
            if(_tentacles[i].IsConnected)
            {
                _finalVector += _tentacles[i].FromPlayerVector;
            }
        }
    }

    private void Impulse()
    {
        _rigidBody.AddForce(_finalVector * _implulseMagnitude);
    }
    private void Update()
    {
        FindFinal();
        Impulse();
    }

    private void OnDrawGizmos()
    {
        foreach (TentacleAnchor tentacle in _tentacles)
        {
            Gizmos.DrawLine(transform.position, tentacle.FromPlayerVector + Get2DPosition());
        }
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, _finalVector + Get2DPosition());
    }

    private Vector2 Get2DPosition()
    {
        return new Vector2(transform.position.x, transform.position.y);
    }
}
