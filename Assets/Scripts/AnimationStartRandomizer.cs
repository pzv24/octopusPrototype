using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
public class AnimationStartRandomizer : MonoBehaviour
{
    private Animator _animator;
    [SerializeField] private TentacleVisual _visual;
    [SerializeField] private AnimationCurve _animationCurve;
    [SerializeField, MinMaxSlider(0,270,ShowFields = true)] Vector2 _minMaxMagnitude;
    [SerializeField, MinMaxSlider(0,10, ShowFields = true)] Vector2 _minMaxAnimDuration;
    [SerializeField] private float _invertMult = 1;

    private void Start()
    {
        StartCoroutine(AnimateTentacle());
    }
    public void SetTentacleTexture(float value)
    {
        _visual.SetTentacleTextureScale(value);
    }
    private IEnumerator AnimateTentacle()
    {
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
            float xRotation = _animationCurve.Evaluate(normalizedElapsed) * magnitude;

            //apply the evaluated value on the local x rotation of game object
            Vector3 targetRotation = new Vector3(xRotation, 0, 0);
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
        StartCoroutine(AnimateTentacle());
    }
}
