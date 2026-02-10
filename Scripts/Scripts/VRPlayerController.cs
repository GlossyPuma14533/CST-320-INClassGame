using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// VR Player Controller for Meta Quest 3
/// Handles locomotion, teleportation, and basic VR movement
/// Works with Unity XR Interaction Toolkit
///
/// USAGE:
/// 1. Attach to the XR Origin GameObject
/// 2. Ensure XR Interaction Toolkit is installed
/// 3. Configure movement settings in Inspector
/// 4. Assign XR controllers in the Inspector
///
/// IMPORTANT: This script requires Unity XR Interaction Toolkit
/// Install via Package Manager: com.unity.xr.interaction.toolkit
///
/// Author: GDT-120 Course Materials
/// Date: 2026
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class VRPlayerController : MonoBehaviour
{
    #region Enums

    /// <summary>
    /// Type of locomotion system
    /// </summary>
    public enum LocomotionType
    {
        Teleport,           // Teleport-based movement (comfort mode)
        Continuous,         // Smooth joystick movement
        Hybrid              // Both teleport and continuous
    }

    /// <summary>
    /// Turning mode for player rotation
    /// </summary>
    public enum TurnMode
    {
        Snap,               // Snap rotation (e.g., 30° increments)
        Smooth              // Smooth continuous rotation
    }

    #endregion

    #region Inspector Variables

    [Header("Locomotion Settings")]
    [Tooltip("Type of movement system")]
    public LocomotionType locomotionType = LocomotionType.Hybrid;

    [Tooltip("Movement speed (m/s) for continuous movement")]
    [Range(1f, 10f)]
    public float moveSpeed = 3f;

    [Tooltip("Sprint speed multiplier (when sprint button held)")]
    [Range(1f, 3f)]
    public float sprintMultiplier = 1.5f;

    [Tooltip("Enable gravity")]
    public bool useGravity = true;

    [Tooltip("Gravity force")]
    public float gravity = -9.81f;

    [Header("Turning Settings")]
    [Tooltip("Type of rotation system")]
    public TurnMode turnMode = TurnMode.Snap;

    [Tooltip("Snap turn angle (degrees)")]
    [Range(15f, 90f)]
    public float snapTurnAngle = 30f;

    [Tooltip("Smooth turn speed (degrees per second)")]
    [Range(30f, 180f)]
    public float smoothTurnSpeed = 90f;

    [Header("Teleportation Settings")]
    [Tooltip("Maximum teleport distance")]
    [Range(5f, 30f)]
    public float maxTeleportDistance = 10f;

    [Tooltip("Teleport arc height")]
    [Range(0f, 5f)]
    public float teleportArcHeight = 2f;

    [Tooltip("Layer mask for valid teleport surfaces")]
    public LayerMask teleportLayerMask;

    [Header("Comfort Settings")]
    [Tooltip("Enable vignette effect during movement (reduces motion sickness)")]
    public bool useVignette = true;

    [Tooltip("Vignette intensity during movement")]
    [Range(0f, 1f)]
    public float vignetteIntensity = 0.5f;

    [Header("XR References")]
    [Tooltip("Reference to main VR camera")]
    public Camera xrCamera;

    [Tooltip("Left controller input (if using XR Interaction Toolkit)")]
    public ActionBasedController leftController;

    [Tooltip("Right controller input (if using XR Interaction Toolkit)")]
    public ActionBasedController rightController;

    [Header("Input Actions (for manual setup)")]
    [Tooltip("Left hand primary button for movement")]
    public XRNode movementHand = XRNode.LeftHand;

    [Tooltip("Right hand primary button for rotation")]
    public XRNode rotationHand = XRNode.RightHand;

    #endregion

    #region Private Variables

    private CharacterController characterController;
    private Vector3 velocity;
    private bool isGrounded;
    private float lastSnapTurnTime;
    private float snapTurnCooldown = 0.3f;
    private Vector2 moveInput;
    private Vector2 turnInput;

    // XR Input devices
    private InputDevice leftHandDevice;
    private InputDevice rightHandDevice;

    #endregion

    #region Unity Lifecycle Methods

    void Start()
    {
        // TODO: STEP 1 - Initialize components and references
        // HINT: Get CharacterController, find VR camera if not assigned

        characterController = GetComponent<CharacterController>();

        if (xrCamera == null)
        {
            xrCamera = GetComponentInChildren<Camera>();

            if (xrCamera == null)
            {
                Debug.LogError("VRPlayerController: No XR Camera found! Please assign manually.");
            }
        }

        // TODO: STEP 2 - Initialize XR input devices
        // HINT: Get left and right hand input devices using XRNode

        InitializeInputDevices();

        Debug.Log($"VRPlayerController initialized. Locomotion: {locomotionType}, Turn Mode: {turnMode}");
    }

    void Update()
    {
        // TODO: STEP 3 - Check if player is grounded
        // HINT: Use CharacterController.isGrounded or raycast downward

        CheckGrounded();

        // TODO: STEP 4 - Get input from VR controllers
        // HINT: Read joystick values for movement and turning

        GetVRInput();

        // TODO: STEP 5 - Handle movement based on locomotion type
        // HINT: Call appropriate movement method based on locomotionType

        if (locomotionType == LocomotionType.Continuous || locomotionType == LocomotionType.Hybrid)
        {
            HandleContinuousMovement();
        }

        // TODO: STEP 6 - Handle rotation based on turn mode
        // HINT: Call appropriate rotation method based on turnMode

        HandleRotation();

        // TODO: STEP 7 - Apply gravity if enabled
        // HINT: Use Physics formula: velocity.y += gravity * Time.deltaTime

        if (useGravity)
        {
            ApplyGravity();
        }
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes XR input devices
    /// </summary>
    private void InitializeInputDevices()
    {
        // TODO: STEP 8 - Get XR input devices
        // HINT: Use InputDevices.GetDeviceAtXRNode()

        leftHandDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        rightHandDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        // Verify devices are valid
        if (!leftHandDevice.isValid || !rightHandDevice.isValid)
        {
            Debug.LogWarning("VRPlayerController: XR Input devices not immediately available. Will retry during gameplay.");
        }
    }

    #endregion

    #region Input Handling

    /// <summary>
    /// Gets input from VR controllers
    /// </summary>
    private void GetVRInput()
    {
        // TODO: STEP 9 - Read joystick/thumbstick input
        // HINT: Use InputDevice.TryGetFeatureValue() to read Primary2DAxis
        // Left controller for movement, right controller for rotation

        // Re-initialize devices if they become invalid
        if (!leftHandDevice.isValid)
            leftHandDevice = InputDevices.GetDeviceAtXRNode(movementHand);
        if (!rightHandDevice.isValid)
            rightHandDevice = InputDevices.GetDeviceAtXRNode(rotationHand);

        // Get movement input from left hand
        if (leftHandDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 leftStick))
        {
            moveInput = leftStick;
        }
        else
        {
            moveInput = Vector2.zero;
        }

        // Get rotation input from right hand
        if (rightHandDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 rightStick))
        {
            turnInput = rightStick;
        }
        else
        {
            turnInput = Vector2.zero;
        }
    }

    /// <summary>
    /// Checks if sprint button is pressed
    /// </summary>
    private bool IsSprintPressed()
    {
        // TODO: STEP 10 - Check if sprint button is held
        // HINT: Check for trigger or grip button on movement hand
        // Use InputDevice.TryGetFeatureValue() with CommonUsages.gripButton or triggerButton

        if (leftHandDevice.TryGetFeatureValue(CommonUsages.gripButton, out bool gripPressed))
        {
            return gripPressed;
        }

        return false;
    }

    #endregion

    #region Movement

    /// <summary>
    /// Checks if player is on the ground
    /// </summary>
    private void CheckGrounded()
    {
        // TODO: STEP 11 - Check if player is grounded
        // HINT: Use characterController.isGrounded
        // Optionally do an additional raycast for more accuracy

        isGrounded = characterController.isGrounded;

        // Additional raycast check for better ground detection
        if (!isGrounded)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, 0.2f))
            {
                isGrounded = true;
            }
        }

        // Reset vertical velocity when grounded
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small downward force to keep grounded
        }
    }

    /// <summary>
    /// Handles continuous smooth movement
    /// </summary>
    private void HandleContinuousMovement()
    {
        // TODO: STEP 12 - Implement continuous movement
        // HINT: Calculate movement direction based on camera forward
        // Apply moveInput to create movement vector
        // Use characterController.Move() to apply movement

        if (moveInput.magnitude < 0.1f)
            return; // No input, no movement

        // Get camera forward direction (but keep it horizontal)
        Vector3 forward = xrCamera.transform.forward;
        Vector3 right = xrCamera.transform.right;

        // Project onto horizontal plane
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        // Calculate movement direction
        Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x);

        // Apply speed and sprint multiplier
        float speed = moveSpeed;
        if (IsSprintPressed())
        {
            speed *= sprintMultiplier;
        }

        // Move the character
        Vector3 move = moveDirection * speed * Time.deltaTime;
        characterController.Move(move);

        // Apply vignette effect if enabled
        if (useVignette && moveInput.magnitude > 0.5f)
        {
            // This would connect to a post-processing effect
            // Students can implement visual comfort features here
        }
    }

    /// <summary>
    /// Applies gravity to the player
    /// </summary>
    private void ApplyGravity()
    {
        // TODO: STEP 13 - Apply gravity
        // HINT: Increment velocity.y by gravity * Time.deltaTime
        // Use characterController.Move() to apply vertical velocity

        if (!isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }

        // Apply vertical movement
        characterController.Move(velocity * Time.deltaTime);
    }

    #endregion

    #region Rotation

    /// <summary>
    /// Handles player rotation based on turn mode
    /// </summary>
    private void HandleRotation()
    {
        // TODO: STEP 14 - Handle rotation based on turnMode
        // HINT: Call SnapTurn() or SmoothTurn() based on mode

        if (turnMode == TurnMode.Snap)
        {
            HandleSnapTurn();
        }
        else if (turnMode == TurnMode.Smooth)
        {
            HandleSmoothTurn();
        }
    }

    /// <summary>
    /// Handles snap turn rotation (discrete angles)
    /// </summary>
    private void HandleSnapTurn()
    {
        // TODO: STEP 15 - Implement snap turning
        // HINT: Check if horizontal input exceeds threshold (e.g., 0.7)
        // Only turn if cooldown period has passed
        // Rotate by snapTurnAngle in the input direction

        // Check if enough time has passed since last snap turn
        if (Time.time - lastSnapTurnTime < snapTurnCooldown)
            return;

        // Check for significant horizontal input
        if (Mathf.Abs(turnInput.x) > 0.7f)
        {
            float turnAmount = snapTurnAngle * Mathf.Sign(turnInput.x);
            transform.Rotate(0, turnAmount, 0);

            lastSnapTurnTime = Time.time;

            Debug.Log($"Snap turn: {turnAmount}°");
        }
    }

    /// <summary>
    /// Handles smooth continuous rotation
    /// </summary>
    private void HandleSmoothTurn()
    {
        // TODO: STEP 16 - Implement smooth turning
        // HINT: Continuously rotate based on horizontal input
        // Use smoothTurnSpeed and Time.deltaTime for smooth rotation

        if (Mathf.Abs(turnInput.x) > 0.1f)
        {
            float turnAmount = turnInput.x * smoothTurnSpeed * Time.deltaTime;
            transform.Rotate(0, turnAmount, 0);
        }
    }

    #endregion

    #region Teleportation

    /// <summary>
    /// Handles teleportation input and execution
    /// Called by XR Teleportation system or manually
    /// </summary>
    public void TeleportPlayer(Vector3 destination)
    {
        // TODO: STEP 17 - Implement teleportation
        // HINT: Validate destination is on valid surface
        // Move character controller to destination
        // Optional: Add fade effect for comfort

        // Validate destination
        if (!IsValidTeleportDestination(destination))
        {
            Debug.Log("Invalid teleport destination");
            return;
        }

        // Teleport the player
        characterController.enabled = false; // Disable to allow direct position change
        transform.position = destination;
        characterController.enabled = true;

        Debug.Log($"Teleported to: {destination}");

        // TODO: STEP 18 (ADVANCED) - Add fade effect
        // Students can implement screen fade for comfort
        // StartCoroutine(FadeEffect());
    }

    /// <summary>
    /// Validates if a position is a valid teleport destination
    /// </summary>
    private bool IsValidTeleportDestination(Vector3 position)
    {
        // TODO: STEP 19 - Validate teleport destination
        // HINT: Check if position is within max distance
        // Check if surface is in teleportLayerMask
        // Ensure there's ground at that position

        // Check distance
        float distance = Vector3.Distance(transform.position, position);
        if (distance > maxTeleportDistance)
        {
            return false;
        }

        // Raycast down to ensure there's ground
        RaycastHit hit;
        if (Physics.Raycast(position + Vector3.up, Vector3.down, out hit, 2f, teleportLayerMask))
        {
            return true;
        }

        return false;
    }

    #endregion

    #region Public API

    /// <summary>
    /// Sets the player's position (useful for checkpoints, respawning)
    /// </summary>
    public void SetPosition(Vector3 position)
    {
        characterController.enabled = false;
        transform.position = position;
        characterController.enabled = true;
    }

    /// <summary>
    /// Sets the player's rotation
    /// </summary>
    public void SetRotation(Quaternion rotation)
    {
        transform.rotation = rotation;
    }

    /// <summary>
    /// Enables or disables player movement
    /// </summary>
    public void SetMovementEnabled(bool enabled)
    {
        this.enabled = enabled;
    }

    /// <summary>
    /// Gets the current movement velocity
    /// </summary>
    public Vector3 GetVelocity()
    {
        return characterController.velocity;
    }

    /// <summary>
    /// Checks if player is currently moving
    /// </summary>
    public bool IsMoving()
    {
        return characterController.velocity.magnitude > 0.1f;
    }

    #endregion

    #region Debug Helpers

    void OnDrawGizmos()
    {
        // TODO: STEP 20 - Draw debug visualization
        // HINT: Draw teleport range, ground check ray, movement direction

        // Draw teleport range
        Gizmos.color = new Color(0, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, maxTeleportDistance);

        // Draw ground check
        Gizmos.color = Color.red;
        Gizmos.DrawRay(transform.position, Vector3.down * 0.2f);
    }

    #endregion
}

/*
 * STUDENT EXERCISE SUGGESTIONS:
 *
 * 1. ADVANCED MOVEMENT:
 *    - Implement climbing mechanics
 *    - Add swimming or flying modes
 *    - Create grappling hook system
 *    - Implement wall running
 *
 * 2. COMFORT FEATURES:
 *    - Add screen fade during teleport
 *    - Implement FOV reduction during movement
 *    - Create rest frames (periodic slowdown)
 *    - Add comfort mode toggle
 *
 * 3. INTERACTION ENHANCEMENTS:
 *    - Add crouch/prone positions
 *    - Implement gesture-based movement
 *    - Create vehicle controls
 *    - Add physics-based pushing/pulling
 *
 * 4. ACCESSIBILITY:
 *    - Seated mode support
 *    - One-handed play option
 *    - Adjustable height calibration
 *    - Alternative control schemes
 *
 * 5. VR OPTIMIZATION:
 *    - Predictive tracking
 *    - Haptic feedback for movement
 *    - Audio footsteps synchronized with movement
 *    - Dynamic collision detection
 */
