using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LookAtMouse : MonoBehaviour
{
    [SerializeField] private float _rotationSpeed = 5;
    [SerializeField] private bool _flipSprite = true;
    [SerializeField] private TentacleMovement _movement;

    private Vector3 _lookDirection;
    [SerializeField] private float _crossProductSign = 1;

    private void Update()
    {
        _lookDirection = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        float lookAngle = Mathf.Atan2(_lookDirection.x, _lookDirection.y) * Mathf.Rad2Deg;
        Quaternion lookRotation = Quaternion.AngleAxis(lookAngle, -Vector3.forward);
        //transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, _rotationSpeed * Time.deltaTime);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, _rotationSpeed * Time.deltaTime);
        GetNormalsAndScale();
    }

    private void GetNormalsAndScale()
    {
        Vector2 normalizedNormalSum = Vector2.zero;
        for(int i = 0; i < _movement.ActiveTentacles.Count; i++)
        {
            normalizedNormalSum += _movement.ActiveTentacles[i].AnchorNormal;
        }
        normalizedNormalSum = normalizedNormalSum.normalized;
        Vector3 directionCrossProduct = Vector3.Cross(normalizedNormalSum, _lookDirection.normalized);
        //Debug.Log(Vector3.Cross(normalizedNormalSum, _lookDirection.normalized));
        _crossProductSign = Mathf.Sign(directionCrossProduct.z) * -1;
        if (_crossProductSign != transform.localScale.x)
        {
            transform.localScale = new Vector3(_crossProductSign, 1, 1);
        }
    }
}
