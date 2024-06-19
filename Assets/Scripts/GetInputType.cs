using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

public class GetInputType : MonoBehaviour
{
    [SerializeField] private PlayerInput _playerInput;
    [SerializeField] private GameObject _mouseControllsGameObject;
    [SerializeField] private GameObject _touchControllsGameObject;

    private void OnEnable()
    {
        InputUser.onChange += OnDeviceChanged;
    }
    private void OnDisable()
    {
        InputUser.onChange -= OnDeviceChanged;
    }
    private void OnDeviceChanged(InputUser user, InputUserChange userChange, InputDevice device)
    {
        if (userChange.Equals(InputUserChange.ControlsChanged))
        {
            string currentControlScheme = user.controlScheme.ToString().Split('(')[0];
            Debug.Log(currentControlScheme);
            if (currentControlScheme == "Keyboard&Mouse")
            {
                _touchControllsGameObject.SetActive(false);
                _mouseControllsGameObject.SetActive(true);
            }
            else if (currentControlScheme == "Touch")
            {
                _touchControllsGameObject.SetActive(true);
                _mouseControllsGameObject.SetActive(false);
            }
        }
    }
}
