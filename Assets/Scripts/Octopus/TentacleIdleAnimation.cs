using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

// animate the idle for the tentacles. Uses the free look root object instead of the follow target
public class TentacleIdleAnimation : MonoBehaviour
{
    [Header("Plug in Fields")]
    [SerializeField] private TentacleVisual _visual;

    [Header("Settings")]
    [SerializeField] private AnimationCurve _animationCurve;
    [SerializeField, MinMaxSlider(0,270,ShowFields = true)] Vector2 _minMaxMagnitude;
    [SerializeField, MinMaxSlider(0,10, ShowFields = true)] Vector2 _minMaxAnimDuration;
    [SerializeField] private float _invertMult = 1f;

    private Coroutine _idleAnimCoroutine;

    // starts or stops the Idle animation
    public void SetIdleAnimationEnabled(bool animateIdle)
    {
        if(animateIdle)
        {
            StartIdleAnimation();
        }
        else
        {
            StopIdleAnimation();
        }
    }
    [Button]
    private void StartIdleAnimation()
    {
        if(_idleAnimCoroutine != null)
        {
            StopIdleAnimation();
        }
        // reset the rotation on the object before starting the animation
        transform.localRotation = Quaternion.Euler(Vector3.zero);
        _idleAnimCoroutine = StartCoroutine(AnimateTentacle());
    }
    [Button]
    private void StopIdleAnimation()
    {
        if(_idleAnimCoroutine != null)
        {
            StopCoroutine(_idleAnimCoroutine);
            _idleAnimCoroutine = null;
        }
    }
    private void SetTentacleTexture(float value)
    {
        // sets the line renderer material y scale
        _visual.SetTentacleTextureScale(value);
    }
    private IEnumerator AnimateTentacle()
    {
        // innit variables
        float elapsed = 0;
        bool calledFirstChange = false;
        bool calledSecondChange = false;
        SetTentacleTexture(-1f * _invertMult);

        //get random variables
        float duration = Random.Range(_minMaxAnimDuration.x, _minMaxAnimDuration.y);
        float magnitude = Random.Range(_minMaxMagnitude.x, _minMaxMagnitude.y);

        while (elapsed < duration)
        {
            // get the normalized value of elapsed (between 0 and 1)
            float normalizedElapsed = elapsed / duration;

            //evaluate normalized on animation curve and multiply by magnitude
            float zRotation = _animationCurve.Evaluate(normalizedElapsed) * magnitude;

            //apply the evaluated value on the local x rotation of game object
            Vector3 targetRotation = new Vector3(0, 0, zRotation);
            transform.localRotation = Quaternion.Euler(targetRotation);

            // the first time you go over the first inflexion point, flip the texutre
            if (!calledFirstChange && normalizedElapsed > 0.25f && normalizedElapsed < 0.75f)
            {
                SetTentacleTexture(1f * _invertMult);
                calledFirstChange = true;
            }
            // same thing for the second inflexion point
            if (!calledSecondChange && normalizedElapsed > 0.75f)
            {
                SetTentacleTexture(-1f * _invertMult);
                calledSecondChange = true;
            }

            elapsed += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        // on end, start the coroutine again
        StartIdleAnimation();
    }
}
