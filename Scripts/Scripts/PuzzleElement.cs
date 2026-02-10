using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// Puzzle Element Script for VR Adventure Quest Games
/// Handles interactive puzzle mechanics like buttons, levers, pressure plates, etc.
/// Supports multi-step puzzles, sequences, and combination locks
///
/// USAGE:
/// 1. Attach to interactive puzzle objects (buttons, levers, etc.)
/// 2. Configure puzzle type and settings in Inspector
/// 3. Link multiple elements together for complex puzzles
/// 4. Use Unity Events to trigger results (open doors, spawn items, etc.)
///
/// Author: GDT-120 Course Materials
/// Date: 2026
/// </summary>
public class PuzzleElement : MonoBehaviour
{
    #region Enums

    /// <summary>
    /// Type of puzzle interaction
    /// </summary>
    public enum PuzzleType
    {
        Button,         // Press to toggle on/off
        Lever,          // Pull to switch states
        PressurePlate,  // Activated by weight/presence
        RotationLock,   // Rotate to correct angle
        SequenceNode,   // Part of a timed sequence
        Combination     // Must match specific value/state
    }

    /// <summary>
    /// Current state of the puzzle element
    /// </summary>
    public enum ElementState
    {
        Inactive,       // Not activated
        Active,         // Currently activated
        Locked,         // Cannot be interacted with
        Solved          // Puzzle completed
    }

    #endregion

    #region Inspector Variables

    [Header("Puzzle Configuration")]
    [Tooltip("Type of puzzle element")]
    public PuzzleType puzzleType = PuzzleType.Button;

    [Tooltip("Unique ID for this puzzle element (for sequences)")]
    public string elementID = "";

    [Tooltip("Is this a toggle (stays on) or momentary (springs back)?")]
    public bool isToggle = true;

    [Tooltip("Can this element be reset after activation?")]
    public bool canReset = true;

    [Header("Interaction Settings")]
    [Tooltip("Which XR controller button activates this? (requires XR Interaction Toolkit)")]
    public string interactionButton = "Trigger";

    [Tooltip("Time required to hold for activation (0 = instant)")]
    [Range(0f, 5f)]
    public float holdTime = 0f;

    [Tooltip("Cooldown time between activations")]
    [Range(0f, 10f)]
    public float cooldownTime = 0.5f;

    [Header("Visual Feedback")]
    [Tooltip("Material for inactive state")]
    public Material inactiveMaterial;

    [Tooltip("Material for active state")]
    public Material activeMaterial;

    [Tooltip("Object to animate when activated (optional)")]
    public GameObject animatedObject;

    [Tooltip("Animation curve for smooth transitions")]
    public AnimationCurve activationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("Time for animation to complete")]
    [Range(0.1f, 5f)]
    public float animationDuration = 1f;

    [Header("Audio Feedback")]
    [Tooltip("Sound played when activated")]
    public AudioClip activationSound;

    [Tooltip("Sound played when deactivated")]
    public AudioClip deactivationSound;

    [Tooltip("Sound played when puzzle is solved")]
    public AudioClip solvedSound;

    [Tooltip("Sound played when interaction fails")]
    public AudioClip failSound;

    [Range(0f, 1f)]
    public float soundVolume = 1f;

    [Header("Puzzle Logic - Rotation Lock")]
    [Tooltip("Target rotation angle for RotationLock puzzles")]
    [Range(0f, 360f)]
    public float targetAngle = 90f;

    [Tooltip("Allowed error margin for rotation")]
    [Range(0f, 45f)]
    public float angleTolerance = 5f;

    [Header("Puzzle Logic - Sequence")]
    [Tooltip("Order in sequence (1, 2, 3, etc.)")]
    public int sequenceOrder = 1;

    [Tooltip("Reference to PuzzleManager for sequence validation")]
    public PuzzleManager puzzleManager;

    [Header("Events")]
    [Tooltip("Event triggered when element is activated")]
    public UnityEvent OnActivated;

    [Tooltip("Event triggered when element is deactivated")]
    public UnityEvent OnDeactivated;

    [Tooltip("Event triggered when puzzle is solved")]
    public UnityEvent OnSolved;

    [Tooltip("Event triggered when interaction fails")]
    public UnityEvent OnFailed;

    #endregion

    #region Private Variables

    private ElementState currentState = ElementState.Inactive;
    private bool isInteracting = false;
    private float interactionTime = 0f;
    private float cooldownTimer = 0f;
    private AudioSource audioSource;
    private MeshRenderer meshRenderer;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isAnimating = false;

    #endregion

    #region Unity Lifecycle Methods

    void Start()
    {
        // TODO: STEP 1 - Initialize components and references
        // HINT: Get MeshRenderer for visual feedback
        // Setup AudioSource for sound effects
        // Store original position/rotation for animations

        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null && animatedObject != null)
        {
            meshRenderer = animatedObject.GetComponent<MeshRenderer>();
        }

        // Setup audio source
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f; // 3D sound
        audioSource.volume = soundVolume;

        // Store original transforms
        if (animatedObject != null)
        {
            originalPosition = animatedObject.transform.localPosition;
            originalRotation = animatedObject.transform.localRotation;
        }
        else
        {
            originalPosition = transform.localPosition;
            originalRotation = transform.localRotation;
        }

        // TODO: STEP 2 - Validate configuration
        // HINT: Check if puzzle manager is assigned for sequence puzzles
        // Generate unique ID if empty

        if (puzzleType == PuzzleType.SequenceNode && puzzleManager == null)
        {
            Debug.LogWarning($"PuzzleElement '{gameObject.name}': Sequence puzzle requires a PuzzleManager!");
        }

        if (string.IsNullOrEmpty(elementID))
        {
            elementID = gameObject.name + "_" + GetInstanceID();
        }

        // Set initial visual state
        UpdateVisualState();

        Debug.Log($"PuzzleElement initialized: {elementID} (Type: {puzzleType})");
    }

    void Update()
    {
        // TODO: STEP 3 - Update cooldown timer
        // HINT: Decrease cooldown timer each frame

        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
        }

        // TODO: STEP 4 - Handle hold-to-activate mechanic
        // HINT: If player is holding interaction, increment interactionTime
        // When it reaches holdTime, activate the element

        if (isInteracting && holdTime > 0)
        {
            interactionTime += Time.deltaTime;

            if (interactionTime >= holdTime)
            {
                AttemptActivation();
                isInteracting = false;
            }
        }

        // TODO: STEP 5 - Update rotation lock puzzle
        // HINT: For RotationLock type, check if current rotation matches target

        if (puzzleType == PuzzleType.RotationLock && currentState != ElementState.Solved)
        {
            CheckRotationAngle();
        }
    }

    #endregion

    #region Interaction Methods

    /// <summary>
    /// Called when player interacts with this puzzle element
    /// Can be triggered by VR controller, trigger zones, or other scripts
    /// </summary>
    public void OnInteract()
    {
        // TODO: STEP 6 - Handle interaction input
        // HINT: Check if element can be interacted with (not locked, not on cooldown)
        // For instant activation (holdTime == 0), activate immediately
        // For hold activation, start the hold timer

        if (currentState == ElementState.Locked || currentState == ElementState.Solved)
        {
            Debug.Log($"Cannot interact with {elementID}: Element is {currentState}");
            PlaySound(failSound);
            OnFailed?.Invoke();
            return;
        }

        if (cooldownTimer > 0)
        {
            Debug.Log($"Cannot interact with {elementID}: On cooldown ({cooldownTimer:F1}s remaining)");
            return;
        }

        if (holdTime == 0)
        {
            // Instant activation
            AttemptActivation();
        }
        else
        {
            // Start hold timer
            isInteracting = true;
            interactionTime = 0f;
        }
    }

    /// <summary>
    /// Called when player releases interaction (for hold mechanics)
    /// </summary>
    public void OnInteractRelease()
    {
        // TODO: STEP 7 - Handle interaction release
        // HINT: Reset interaction state if player releases before hold completes

        if (isInteracting && interactionTime < holdTime)
        {
            Debug.Log($"Interaction released early on {elementID} ({interactionTime:F1}s / {holdTime:F1}s)");
            isInteracting = false;
            interactionTime = 0f;
        }
    }

    /// <summary>
    /// Attempts to activate the puzzle element
    /// </summary>
    private void AttemptActivation()
    {
        // TODO: STEP 8 - Implement puzzle-specific activation logic
        // HINT: Different puzzle types have different validation requirements
        // Use switch statement to handle each type

        bool canActivate = true;

        switch (puzzleType)
        {
            case PuzzleType.Button:
            case PuzzleType.Lever:
            case PuzzleType.PressurePlate:
                // Simple activation - always allowed
                canActivate = true;
                break;

            case PuzzleType.SequenceNode:
                // Check if this is the correct next node in sequence
                canActivate = ValidateSequence();
                break;

            case PuzzleType.RotationLock:
                // Check if rotation is within tolerance
                canActivate = CheckRotationAngle();
                break;

            case PuzzleType.Combination:
                // Custom validation (students implement based on their needs)
                canActivate = ValidateCombination();
                break;
        }

        if (canActivate)
        {
            Activate();
        }
        else
        {
            PlaySound(failSound);
            OnFailed?.Invoke();
            Debug.Log($"Activation failed for {elementID}");
        }
    }

    /// <summary>
    /// Activates the puzzle element
    /// </summary>
    private void Activate()
    {
        // TODO: STEP 9 - Activate the element
        // HINT: Change state, play sound, trigger animation, invoke events

        // Toggle or set to active
        if (isToggle)
        {
            currentState = (currentState == ElementState.Active) ? ElementState.Inactive : ElementState.Active;
        }
        else
        {
            currentState = ElementState.Active;
        }

        // Play audio
        PlaySound(currentState == ElementState.Active ? activationSound : deactivationSound);

        // Update visuals
        UpdateVisualState();

        // Start animation
        if (animatedObject != null && !isAnimating)
        {
            StartCoroutine(AnimateElement());
        }

        // Invoke events
        if (currentState == ElementState.Active)
        {
            OnActivated?.Invoke();
            Debug.Log($"Puzzle element activated: {elementID}");

            // Check if puzzle is solved
            CheckPuzzleSolved();
        }
        else
        {
            OnDeactivated?.Invoke();
            Debug.Log($"Puzzle element deactivated: {elementID}");
        }

        // Start cooldown
        cooldownTimer = cooldownTime;

        // Reset if momentary (non-toggle)
        if (!isToggle)
        {
            Invoke(nameof(Deactivate), animationDuration);
        }
    }

    /// <summary>
    /// Deactivates the element (for momentary buttons)
    /// </summary>
    private void Deactivate()
    {
        // TODO: STEP 10 - Deactivate momentary elements
        // HINT: Return to inactive state, update visuals

        if (!isToggle && currentState == ElementState.Active)
        {
            currentState = ElementState.Inactive;
            UpdateVisualState();
            OnDeactivated?.Invoke();
        }
    }

    #endregion

    #region Puzzle Validation Methods

    /// <summary>
    /// Validates if this element is the correct next step in a sequence
    /// </summary>
    private bool ValidateSequence()
    {
        // TODO: STEP 11 - Implement sequence validation
        // HINT: Check with PuzzleManager if this is the correct next element
        // Return true if this element should activate now

        if (puzzleManager == null)
        {
            Debug.LogError($"PuzzleElement '{elementID}': No PuzzleManager assigned!");
            return false;
        }

        return puzzleManager.ValidateSequenceStep(this);
    }

    /// <summary>
    /// Checks if rotation angle is correct for RotationLock puzzles
    /// </summary>
    private bool CheckRotationAngle()
    {
        // TODO: STEP 12 - Implement rotation checking
        // HINT: Get current Y rotation angle
        // Check if it's within tolerance of targetAngle
        // Use Mathf.DeltaAngle for proper angle comparison

        GameObject rotatingObject = animatedObject != null ? animatedObject : gameObject;
        float currentAngle = rotatingObject.transform.localEulerAngles.y;
        float angleDifference = Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle));

        if (angleDifference <= angleTolerance)
        {
            if (currentState != ElementState.Solved)
            {
                Debug.Log($"Rotation lock solved! Angle: {currentAngle:F1}° (Target: {targetAngle}°)");
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Validates combination lock puzzles (custom implementation)
    /// </summary>
    private bool ValidateCombination()
    {
        // TODO: STEP 13 - Implement custom combination validation
        // HINT: This is a placeholder for students to implement their own logic
        // Could check multiple element states, inventory items, etc.

        // Example: Check if other puzzle elements are in correct states
        // Students should implement based on their specific puzzle design

        Debug.Log($"Combination validation for {elementID} - Implement custom logic!");
        return true; // Placeholder
    }

    /// <summary>
    /// Checks if the entire puzzle is solved
    /// </summary>
    private void CheckPuzzleSolved()
    {
        // TODO: STEP 14 - Determine if puzzle is complete
        // HINT: Different puzzle types have different completion conditions
        // For sequences, check with PuzzleManager
        // For single elements, mark as solved immediately

        bool isSolved = false;

        switch (puzzleType)
        {
            case PuzzleType.SequenceNode:
                // Sequence puzzle completion checked by manager
                if (puzzleManager != null)
                {
                    isSolved = puzzleManager.IsPuzzleSolved();
                }
                break;

            case PuzzleType.RotationLock:
            case PuzzleType.Combination:
                // These types are solved when correctly activated
                isSolved = true;
                break;

            case PuzzleType.Button:
            case PuzzleType.Lever:
            case PuzzleType.PressurePlate:
                // Simple types don't have a "solved" state
                isSolved = false;
                break;
        }

        if (isSolved)
        {
            SetSolved();
        }
    }

    /// <summary>
    /// Marks this element (and potentially the entire puzzle) as solved
    /// </summary>
    public void SetSolved()
    {
        // TODO: STEP 15 - Handle puzzle completion
        // HINT: Set state to Solved, play sound, invoke event, lock element

        currentState = ElementState.Solved;
        PlaySound(solvedSound);
        OnSolved?.Invoke();
        UpdateVisualState();

        Debug.Log($"Puzzle solved: {elementID}");
    }

    #endregion

    #region Visual & Audio Feedback

    /// <summary>
    /// Updates visual appearance based on current state
    /// </summary>
    private void UpdateVisualState()
    {
        // TODO: STEP 16 - Update visual feedback
        // HINT: Change material based on state
        // Could also change color, emission, etc.

        if (meshRenderer == null) return;

        switch (currentState)
        {
            case ElementState.Inactive:
                if (inactiveMaterial != null)
                    meshRenderer.material = inactiveMaterial;
                break;

            case ElementState.Active:
            case ElementState.Solved:
                if (activeMaterial != null)
                    meshRenderer.material = activeMaterial;
                break;

            case ElementState.Locked:
                // Could use a different material for locked state
                break;
        }
    }

    /// <summary>
    /// Plays a sound effect
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        // TODO: STEP 17 - Play audio feedback
        // HINT: Check if clip and audioSource exist, then play

        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, soundVolume);
        }
    }

    /// <summary>
    /// Animates the puzzle element (coroutine)
    /// </summary>
    private System.Collections.IEnumerator AnimateElement()
    {
        // TODO: STEP 18 - Implement element animation
        // HINT: Create different animations based on puzzleType
        // Use activationCurve for smooth motion
        // Button: move down/up, Lever: rotate, etc.

        isAnimating = true;
        GameObject targetObject = animatedObject != null ? animatedObject : gameObject;
        float elapsed = 0f;

        Vector3 targetPosition = originalPosition;
        Quaternion targetRotation = originalRotation;

        // Define target based on puzzle type
        switch (puzzleType)
        {
            case PuzzleType.Button:
            case PuzzleType.PressurePlate:
                // Move down when active
                targetPosition = currentState == ElementState.Active ?
                    originalPosition + Vector3.down * 0.1f : originalPosition;
                break;

            case PuzzleType.Lever:
                // Rotate when active
                targetRotation = currentState == ElementState.Active ?
                    originalRotation * Quaternion.Euler(45, 0, 0) : originalRotation;
                break;

            case PuzzleType.RotationLock:
                // Already handled by player rotation
                break;
        }

        // Animate to target
        Vector3 startPosition = targetObject.transform.localPosition;
        Quaternion startRotation = targetObject.transform.localRotation;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = activationCurve.Evaluate(elapsed / animationDuration);

            targetObject.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
            targetObject.transform.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);

            yield return null;
        }

        // Ensure final position
        targetObject.transform.localPosition = targetPosition;
        targetObject.transform.localRotation = targetRotation;

        isAnimating = false;
    }

    #endregion

    #region Public API Methods

    /// <summary>
    /// Resets the puzzle element to its initial state
    /// </summary>
    public void ResetElement()
    {
        // TODO: STEP 19 - Implement reset functionality
        // HINT: Return to inactive state, reset position/rotation, clear cooldowns

        if (!canReset) return;

        currentState = ElementState.Inactive;
        isInteracting = false;
        interactionTime = 0f;
        cooldownTimer = 0f;

        UpdateVisualState();

        GameObject targetObject = animatedObject != null ? animatedObject : gameObject;
        targetObject.transform.localPosition = originalPosition;
        targetObject.transform.localRotation = originalRotation;

        Debug.Log($"Puzzle element reset: {elementID}");
    }

    /// <summary>
    /// Locks this element (prevents interaction)
    /// </summary>
    public void LockElement()
    {
        currentState = ElementState.Locked;
        UpdateVisualState();
    }

    /// <summary>
    /// Unlocks this element (allows interaction)
    /// </summary>
    public void UnlockElement()
    {
        if (currentState == ElementState.Locked)
        {
            currentState = ElementState.Inactive;
            UpdateVisualState();
        }
    }

    /// <summary>
    /// Gets the current state of this element
    /// </summary>
    public ElementState GetState()
    {
        return currentState;
    }

    /// <summary>
    /// Gets the sequence order (for sequence puzzles)
    /// </summary>
    public int GetSequenceOrder()
    {
        return sequenceOrder;
    }

    #endregion
}

/*
 * STUDENT EXERCISE SUGGESTIONS:
 *
 * 1. ADVANCED PUZZLE MECHANICS:
 *    - Implement time-based puzzles (must complete within time limit)
 *    - Add multi-player cooperation requirements
 *    - Create pattern matching puzzles
 *    - Implement Simon Says style memory games
 *
 * 2. VR-SPECIFIC INTERACTIONS:
 *    - Use hand gestures for activation
 *    - Implement two-handed puzzles
 *    - Add haptic feedback patterns
 *    - Create physical manipulation puzzles
 *
 * 3. VISUAL ENHANCEMENTS:
 *    - Add glow effects for interactive elements
 *    - Implement particle trails for sequences
 *    - Create holographic hints
 *    - Add dynamic lighting changes
 *
 * 4. ACCESSIBILITY:
 *    - Add audio cues for visually impaired
 *    - Implement multiple difficulty levels
 *    - Create hint systems
 *    - Add colorblind-friendly indicators
 *
 * 5. INTEGRATION:
 *    - Connect to quest systems
 *    - Track puzzle completion statistics
 *    - Implement progressive difficulty
 *    - Create puzzle generation systems
 */
