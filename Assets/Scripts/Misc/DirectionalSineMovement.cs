using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DirectionalSineMovement : MonoBehaviour
{
    [SerializeField] private Transform _lookAt;
    [SerializeField] private Transform _lateralMovementChild;
    private Vector2 _lookDirection;

    [SerializeField] private float _waveFrequency = 1;
    [SerializeField] private float _waveMagnitude = 1;

    private void Update()
    {
        _lookDirection = (_lookAt.position - transform.position).normalized;
        float angle = Mathf.Atan2(_lookDirection.y, _lookDirection.x) * Mathf.Rad2Deg - 90;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        _lateralMovementChild.localPosition = new Vector3(Mathf.Sin(_waveFrequency * Time.time)*_waveMagnitude, 0,0);
    }
}
