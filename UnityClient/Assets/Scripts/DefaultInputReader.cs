﻿using UnityEngine;

public class DefaultInputReader : MonoBehaviour, IInputReader
{
    [SerializeField] private float stickDeadzone = 0.2f;

    public PlayerInputData ReadInput(uint time) {
        
        var movementAxes = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        var rotationAxes = new Vector2(Input.GetAxis("RightStickX"), Input.GetAxis("RightStickY"));
        var inputs = new bool[2];
        
        inputs[0] = Input.GetMouseButton(0);
        inputs[1] = Mathf.Abs(rotationAxes.x) > stickDeadzone || Mathf.Abs(rotationAxes.y) > stickDeadzone;

        return new PlayerInputData(inputs, movementAxes, rotationAxes, time);
    }

}
