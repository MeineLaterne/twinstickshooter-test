﻿using UnityEngine;

class MouseInputReader : MonoBehaviour, IInputReader {
    public PlayerInputData ReadInput(uint time) {
        var cursorPosition = Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Camera.main.transform.position.y));
        var rawDirection = cursorPosition - transform.position;
        var rotationAxes = new Vector2(rawDirection.x, rawDirection.z);
        var movementAxes = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        var inputs = new bool[] {
            Input.GetMouseButton(0),
            Input.GetMouseButton(1)
        };

        rotationAxes.Normalize();

        return new PlayerInputData(inputs, movementAxes, rotationAxes, time);
    }
}
