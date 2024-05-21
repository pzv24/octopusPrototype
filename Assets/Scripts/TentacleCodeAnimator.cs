using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class TentacleCodeAnimator : MonoBehaviour
{
    [SerializeField] private TentacleVisual _visual;
    [SerializeField] private float _launchAnimationDuration = 0.5f;
    [SerializeField] private float _retractAnimationDuration = 0.5f;

    private void Start()
    {
        _visual = new TentacleVisual();
    }
    [Button]
    public void AnimateLaunch(Vector3 anchorWorldPosition)
    {
        StartCoroutine(LaunchTentacle(anchorWorldPosition));
    }
    [Button]
    public void AnimateRetract()
    {
        StartCoroutine(RetractTentacle());
    }

    private IEnumerator LaunchTentacle(Vector3 anchorWorldPosition)
    {
        float lerp = 0;
        while (lerp < 1)
        {
            lerp += (1/_launchAnimationDuration) * Time.deltaTime;
            Vector3 finalPosition = Vector3.Lerp(transform.position, anchorWorldPosition, lerp);
            _visual.SetFollowEndPosition(finalPosition);
            yield return new WaitForEndOfFrame();
        }
        _visual.SetIsConnecteed(true);
    }

    private IEnumerator RetractTentacle()
    {
        _visual.SetIsConnecteed(false);
        float lerp = 0;
        while (lerp < 1)
        {
            lerp += (1 / _launchAnimationDuration) * Time.deltaTime;
            Vector3 finalPosition = Vector3.Lerp(_visual.FollowTransform.position, transform.position, lerp);
            _visual.SetFollowEndPosition(finalPosition);
            yield return new WaitForEndOfFrame();
        }
    }
}
