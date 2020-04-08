using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public enum PlayerControlMode { FirstPerson, ThirdPerson}
    public PlayerControlMode mode;

    // References
    [Space(20)]
    public CharacterController characterController;
    [Header("First person camera")]
    public Transform fpCameraTransform;
    [Header("Third person camera")]
    public Transform cameraPole;
    public Transform tpCameraTransform;
    public Transform graphics;
    [Space(20)]

    // Player settings
    [Header("Settings")]
    public float cameraSensitivity;
    public float moveSpeed;
    public float moveInputDeadZone;

    [Header("Third person camera settings")]
    public LayerMask cameraObstacleLayers;
    float maxCameraDistance;
    bool isMoving;

    // Touch detection
    int leftFingerId, rightFingerId;
    float halfScreenWidth;

    // Camera control
    Vector2 lookInput;
    float cameraPitch;

    // Player movement
    Vector2 moveTouchStartPosition;
    Vector2 moveInput;

    // Start is called before the first frame update
    void Start()
    {
        // id = -1 means the finger is not being tracked
        leftFingerId = -1;
        rightFingerId = -1;

        // only calculate once
        halfScreenWidth = Screen.width / 2;

        // calculate the movement input dead zone
        moveInputDeadZone = Mathf.Pow(Screen.height / moveInputDeadZone, 2);

        if (mode == PlayerControlMode.ThirdPerson) {

            // Get the initial angle for the camera pole
            cameraPitch = cameraPole.localRotation.eulerAngles.x;

            // Set max camera distance to the distance the camera is from the player in the editor
            maxCameraDistance = tpCameraTransform.localPosition.z;
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Handles input
        GetTouchInput();


        if (rightFingerId != -1) {
            // Ony look around if the right finger is being tracked
            //Debug.Log("Rotating");
            LookAround();
        }

        if (leftFingerId != -1)
        {
            // Ony move if the left finger is being tracked
            //Debug.Log("Moving");
            Move();
        }
    }

    void FixedUpdate()
    {
        if (mode == PlayerControlMode.ThirdPerson) MoveCamera();
    }

    void GetTouchInput() {
        // Iterate through all the detected touches
        for (int i = 0; i < Input.touchCount; i++)
        {

            Touch t = Input.GetTouch(i);

            // Check each touch's phase
            switch (t.phase)
            {
                case TouchPhase.Began:

                    if (t.position.x < halfScreenWidth && leftFingerId == -1)
                    {
                        // Start tracking the left finger if it was not previously being tracked
                        leftFingerId = t.fingerId;

                        // Set the start position for the movement control finger
                        moveTouchStartPosition = t.position;
                    }
                    else if (t.position.x > halfScreenWidth && rightFingerId == -1)
                    {
                        // Start tracking the rightfinger if it was not previously being tracked
                        rightFingerId = t.fingerId;
                    }

                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:

                    if (t.fingerId == leftFingerId)
                    {
                        // Stop tracking the left finger
                        leftFingerId = -1;
                        //Debug.Log("Stopped tracking left finger");
                        isMoving = false;
                    }
                    else if (t.fingerId == rightFingerId)
                    {
                        // Stop tracking the right finger
                        rightFingerId = -1;
                        //Debug.Log("Stopped tracking right finger");
                    }

                    break;
                case TouchPhase.Moved:

                    // Get input for looking around
                    if (t.fingerId == rightFingerId)
                    {
                        lookInput = t.deltaPosition * cameraSensitivity * Time.deltaTime;
                    }
                    else if (t.fingerId == leftFingerId) {

                        // calculating the position delta from the start position
                        moveInput = t.position - moveTouchStartPosition;
                    }

                    break;
                case TouchPhase.Stationary:
                    // Set the look input to zero if the finger is still
                    if (t.fingerId == rightFingerId)
                    {
                        lookInput = Vector2.zero;
                    }
                    break;
            }
        }
    }

    void LookAround()
    {

        switch (mode)
        {
            case PlayerControlMode.FirstPerson:
                // vertical (pitch) rotation is applied to the first person camera
                cameraPitch = Mathf.Clamp(cameraPitch - lookInput.y, -90f, 90f);
                fpCameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0, 0);
                break;
            case PlayerControlMode.ThirdPerson:
                // vertical (pitch) rotation is applied to the third person camera pole
                cameraPitch = Mathf.Clamp(cameraPitch - lookInput.y, -90f, 90f);
                cameraPole.localRotation = Quaternion.Euler(cameraPitch, 0, 0);
                break;
        }

        if (mode == PlayerControlMode.ThirdPerson && !isMoving)
        {
            // Rotate the graphics in the opposite direction when stationary
            graphics.Rotate(graphics.up, -lookInput.x);
        }
        // horizontal (yaw) rotation
        transform.Rotate(transform.up, lookInput.x);
    }

    void MoveCamera() {

        Vector3 rayDir = tpCameraTransform.position - cameraPole.position;

        Debug.DrawRay(cameraPole.position, rayDir, Color.red);
        // Check if the camera would be colliding with any obstacle
        if (Physics.Raycast(cameraPole.position, rayDir, out RaycastHit hit, Mathf.Abs(maxCameraDistance), cameraObstacleLayers)){
            // Move the camera to the impact point
            tpCameraTransform.position = hit.point;
        } else {
            // Move the camera to the max distance on the local z axis
            tpCameraTransform.localPosition = new Vector3(0, 0, maxCameraDistance);
        }
    }

    void Move() {

        // Don't move if the touch delta is shorter than the designated dead zone
        if (moveInput.sqrMagnitude <= moveInputDeadZone)
        {
            isMoving = false;
            return;
        }

        if (!isMoving) {
            graphics.localRotation = Quaternion.Euler(0, 0, 0);
            isMoving = true;
        }
        // Multiply the normalized direction by the speed
        Vector2 movementDirection = moveInput.normalized * moveSpeed * Time.deltaTime;
        // Move relatively to the local transform's direction
        characterController.Move(transform.right * movementDirection.x + transform.forward * movementDirection.y);
    }

}
