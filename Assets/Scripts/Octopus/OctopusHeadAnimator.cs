using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

// controls the animation of the octopus heads and manages the apparent forward and upwards direction of the player (used externally by other scripts)
public class OctopusHeadAnimator : MonoBehaviour
{
    [SerializeField] private TentacleMovement _movement;
    [SerializeField] private float _rotationSpeed = 5;

    [SerializeField, ReadOnly] private Vector3 _apparentUp = Vector3.zero;

    private Vector3 _lookDirection;
    private Animator _animator;

    public Vector3 PlayerApparentUp { get { return _apparentUp; } }
    public Vector3 PlayerApparentForward { get { return _lookDirection; } }

    private void Start()
    {
        _animator = GetComponent<Animator>();
    }
    private void Update()
    {
        // get the look direction based on mouse position relative to player
        _lookDirection = Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position;
        // convert that into angles for a rotation
        float lookAngle = Mathf.Atan2(_lookDirection.x, _lookDirection.y) * Mathf.Rad2Deg;
        Quaternion lookRotation = Quaternion.AngleAxis(lookAngle, -Vector3.forward);

        // rotate towards the target look rotation based on rotation speed
        transform.rotation = Quaternion.RotateTowards(transform.rotation, lookRotation, _rotationSpeed * Time.deltaTime);

        //adjust the apparent player "down" direction based on the anchor normals of connected tentacles
        GetNormalsAndScale();
    }
    private void GetNormalsAndScale()
    {
        Vector2 normalizedNormalSum = Vector2.zero;
        // sum the normals of the connected anchors
        for(int i = 0; i < _movement.ActiveTentacles.Count; i++)
        {
            normalizedNormalSum += _movement.ActiveTentacles[i].AnchorNormal;
        }
        // normalize the sum
        normalizedNormalSum = normalizedNormalSum.normalized;

        //get the normalized sum of normals with the look direction
        Vector3 directionCrossProduct = Vector3.Cross(normalizedNormalSum, _lookDirection.normalized);

        // we only need the information of the sign of the cross product
        float newCrossProduct = Mathf.Sign(directionCrossProduct.z);

        // get the new up direction, multiply by the sign extracted from the cross product avobe
        _apparentUp = Vector3.Cross(_lookDirection, Vector3.forward) * newCrossProduct;

        // animate the head accordingly to the direction
        _animator.SetInteger("Direction", (int)newCrossProduct);
    }
}
