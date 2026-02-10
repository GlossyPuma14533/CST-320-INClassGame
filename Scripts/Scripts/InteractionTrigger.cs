using UnityEngine;
using UnityEngine.Events;
using System.Collections;

/// <summary>
/// Interaction Trigger Script for VR Environments
/// Creates trigger zones that activate events when player enters/exits
/// Perfect for spawning enemies, opening doors, playing audio, or triggering cutscenes
///
/// USAGE:
/// 1. Attach to a GameObject with a Collider (set as trigger)
/// 2. Configure trigger settings in the Inspector
/// 3. Assign events to execute when triggered
/// 4. Tag player as "Player" or set custom tag
///
/// Author: GDT-120 Course Materials
/// Date: 2026
/// </summary>
[RequireComponent(typeof(Collider))]
public class InteractionTrigger : MonoBehaviour
{
    #region Enums

    /// <summary>
    /// Defines how the trigger responds to interactions
    /// </summary>
    public enum TriggerMode
    {
        OnEnter,        // Triggers when player enters the zone
        OnExit,         // Triggers when player exits the zone
        OnStay,         // Triggers continuously while player is in zone
        OnEnterAndExit  // Triggers both on enter and exit
    }

    /// <summary>
    /// Defines how many times the trigger can activate
    /// </summary>
    public enum ActivationLimit
    {
        Once,           // Trigger only once, then disable
        Limited,        // Trigger a specific number of times
        Unlimited       // Trigger infinitely
    }

    #endregion

    #region Inspector Variables

    [Header("Trigger Settings")]
    [Tooltip("When should this trigger activate?")]
    public TriggerMode triggerMode = TriggerMode.OnEnter;

    [Tooltip("How many times can this trigger activate?")]
    public ActivationLimit activationLimit = ActivationLimit.Once;

    [Tooltip("Maximum number of activations (only used if limit is 'Limited')")]
    [Min(1)]
    public int maxActivations = 3;

    [Tooltip("Delay in seconds before trigger can activate again")]
    [Range(0f, 60f)]
    public float cooldownTime = 0f;

    [Tooltip("Tag of objects that can trigger this (default: 'Player')")]
    public string triggerTag = "Player";

    [Header("Visual Feedback")]
    [Tooltip("Show trigger zone in game (for debugging)")]
    public bool showTriggerZone = false;

    [Tooltip("Color of trigger zone visualization")]
    public Color triggerColor = new Color(0f, 1f, 0f, 0.3f);

    [Tooltip("Particle effect to play when triggered")]
    public ParticleSystem triggerEffect;

    [Header("Audio")]
    [Tooltip("Sound to play when trigger activates")]
    public AudioClip triggerSound;

    [Tooltip("Volume of trigger sound")]
    [Range(0f, 1f)]
    public float soundVolume = 1f;

    [Header("Events")]
    [Tooltip("Event fired when trigger is activated (on enter)")]
    public UnityEvent OnTriggerActivated;

    [Tooltip("Event fired when player exits trigger zone")]
    public UnityEvent OnTriggerExited;

    [Tooltip("Event fired continuously while player is in trigger zone")]
    public UnityEvent OnTriggerStay;

    #endregion

    #region Private Variables

    private int activationCount = 0;
    private bool isOnCooldown = false;
    private bool playerInZone = false;
    private Collider triggerCollider;
    private AudioSource audioSource;
    private MeshRenderer debugRenderer;

    #endregion

    #region Unity Lifecycle Methods

    void Start()
    {
        // TODO: STEP 1 - Initialize and validate components
        // HINT: Get the Collider component and verify it's set as trigger

        triggerCollider = GetComponent<Collider>();

        if (triggerCollider != null && !triggerCollider.isTrigger)
        {
            Debug.LogWarning($"InteractionTrigger on {gameObject.name}: Collider is not set as trigger. Setting it now.");
            triggerCollider.isTrigger = true;
        }

        // TODO: STEP 2 - Setup audio source for trigger sounds
        // HINT: Add AudioSource component if sound is assigned
        // Configure it for 3D spatial sound

        if (triggerSound != null)
        {
            audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.clip = triggerSound;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D sound
            audioSource.volume = soundVolume;
        }

        // TODO: STEP 3 - Setup debug visualization if enabled
        // HINT: Create a semi-transparent mesh to visualize the trigger zone

        if (showTriggerZone)
        {
            SetupDebugVisualization();
        }

        Debug.Log($"InteractionTrigger initialized on {gameObject.name}. Mode: {triggerMode}, Limit: {activationLimit}");
    }

    void Update()
    {
        // TODO: STEP 4 - Handle continuous trigger (OnStay mode)
        // HINT: If triggerMode is OnStay and player is in zone, invoke the event
        // Make sure to check cooldown

        if (triggerMode == TriggerMode.OnStay && playerInZone && !isOnCooldown)
        {
            if (CanActivate())
            {
                ActivateTrigger(TriggerMode.OnStay);
            }
        }
    }

    #endregion

    #region Trigger Detection

    /// <summary>
    /// Called when another collider enters the trigger zone
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        // TODO: STEP 5 - Check if the entering object should activate this trigger
        // HINT: Verify the object has the correct tag using CompareTag()
        // Check if trigger can be activated based on mode and cooldown

        if (other.CompareTag(triggerTag))
        {
            playerInZone = true;

            if ((triggerMode == TriggerMode.OnEnter || triggerMode == TriggerMode.OnEnterAndExit) && !isOnCooldown)
            {
                if (CanActivate())
                {
                    ActivateTrigger(TriggerMode.OnEnter);
                }
            }
        }
    }

    /// <summary>
    /// Called when another collider exits the trigger zone
    /// </summary>
    void OnTriggerExit(Collider other)
    {
        // TODO: STEP 6 - Handle trigger exit
        // HINT: Check tag, update playerInZone, and trigger exit events if appropriate

        if (other.CompareTag(triggerTag))
        {
            playerInZone = false;

            if ((triggerMode == TriggerMode.OnExit || triggerMode == TriggerMode.OnEnterAndExit) && !isOnCooldown)
            {
                if (CanActivate())
                {
                    ActivateTrigger(TriggerMode.OnExit);
                }
            }

            // Always invoke exit event regardless of activation limit
            OnTriggerExited?.Invoke();
        }
    }

    #endregion

    #region Trigger Logic

    /// <summary>
    /// Checks if the trigger can be activated based on activation limits
    /// </summary>
    /// <returns>True if trigger can activate</returns>
    private bool CanActivate()
    {
        // TODO: STEP 7 - Implement activation limit checking
        // HINT: Use switch statement on activationLimit
        // For Once: check if activationCount == 0
        // For Limited: check if activationCount < maxActivations
        // For Unlimited: always return true

        switch (activationLimit)
        {
            case ActivationLimit.Once:
                return activationCount == 0;

            case ActivationLimit.Limited:
                return activationCount < maxActivations;

            case ActivationLimit.Unlimited:
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Activates the trigger and executes associated events
    /// </summary>
    private void ActivateTrigger(TriggerMode mode)
    {
        // Increment activation counter
        activationCount++;

        // TODO: STEP 8 - Play audio feedback
        // HINT: Check if audioSource and triggerSound exist, then play

        if (audioSource != null && triggerSound != null)
        {
            audioSource.Play();
        }

        // TODO: STEP 9 - Spawn particle effect
        // HINT: Instantiate triggerEffect at this position if assigned

        if (triggerEffect != null)
        {
            ParticleSystem effect = Instantiate(triggerEffect, transform.position, Quaternion.identity);
            Destroy(effect.gameObject, effect.main.duration + 1f);
        }

        // TODO: STEP 10 - Invoke appropriate Unity Events
        // HINT: Invoke different events based on the trigger mode

        switch (mode)
        {
            case TriggerMode.OnEnter:
            case TriggerMode.OnEnterAndExit:
                OnTriggerActivated?.Invoke();
                break;

            case TriggerMode.OnExit:
                OnTriggerExited?.Invoke();
                break;

            case TriggerMode.OnStay:
                OnTriggerStay?.Invoke();
                break;
        }

        Debug.Log($"Trigger activated: {gameObject.name} (Activation {activationCount})");

        // TODO: STEP 11 - Start cooldown if needed
        // HINT: If cooldownTime > 0, start the cooldown coroutine

        if (cooldownTime > 0)
        {
            StartCoroutine(CooldownCoroutine());
        }

        // TODO: STEP 12 - Disable trigger if activation limit reached
        // HINT: For Once mode, disable immediately
        // For Limited mode, disable when activationCount >= maxActivations

        if (activationLimit == ActivationLimit.Once ||
            (activationLimit == ActivationLimit.Limited && activationCount >= maxActivations))
        {
            DisableTrigger();
        }
    }

    /// <summary>
    /// Coroutine that handles cooldown timing
    /// </summary>
    private IEnumerator CooldownCoroutine()
    {
        // TODO: STEP 13 - Implement cooldown logic
        // HINT: Set isOnCooldown to true
        // Wait for cooldownTime seconds
        // Set isOnCooldown back to false

        isOnCooldown = true;
        yield return new WaitForSeconds(cooldownTime);
        isOnCooldown = false;
    }

    /// <summary>
    /// Disables the trigger (used when activation limit is reached)
    /// </summary>
    private void DisableTrigger()
    {
        // TODO: STEP 14 - Disable the trigger
        // HINT: Disable the collider component
        // Optionally hide the debug visualization

        if (triggerCollider != null)
        {
            triggerCollider.enabled = false;
        }

        if (debugRenderer != null)
        {
            debugRenderer.enabled = false;
        }

        Debug.Log($"Trigger disabled: {gameObject.name} (Max activations reached)");
    }

    #endregion

    #region Debug Visualization

    /// <summary>
    /// Sets up a visual representation of the trigger zone for debugging
    /// </summary>
    private void SetupDebugVisualization()
    {
        // TODO: STEP 15 - Create debug visualization mesh
        // HINT: This creates a semi-transparent mesh matching the collider shape
        // Only appears in Play mode if showTriggerZone is true

        // Add a mesh filter and renderer if they don't exist
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        debugRenderer = GetComponent<MeshRenderer>();
        if (debugRenderer == null)
        {
            debugRenderer = gameObject.AddComponent<MeshRenderer>();
        }

        // Create a simple cube or sphere mesh based on collider type
        if (triggerCollider is BoxCollider)
        {
            meshFilter.mesh = CreateCubeMesh();
        }
        else if (triggerCollider is SphereCollider)
        {
            meshFilter.mesh = CreateSphereMesh();
        }

        // Create a semi-transparent material
        Material debugMaterial = new Material(Shader.Find("Standard"));
        debugMaterial.color = triggerColor;
        debugMaterial.SetFloat("_Mode", 3); // Transparent mode
        debugMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        debugMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        debugMaterial.SetInt("_ZWrite", 0);
        debugMaterial.DisableKeyword("_ALPHATEST_ON");
        debugMaterial.EnableKeyword("_ALPHABLEND_ON");
        debugMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        debugMaterial.renderQueue = 3000;

        debugRenderer.material = debugMaterial;
    }

    /// <summary>
    /// Creates a simple cube mesh for visualization
    /// </summary>
    private Mesh CreateCubeMesh()
    {
        // Simple cube mesh (students can expand this)
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Mesh mesh = cube.GetComponent<MeshFilter>().sharedMesh;
        DestroyImmediate(cube);
        return mesh;
    }

    /// <summary>
    /// Creates a simple sphere mesh for visualization
    /// </summary>
    private Mesh CreateSphereMesh()
    {
        // Simple sphere mesh (students can expand this)
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Mesh mesh = sphere.GetComponent<MeshFilter>().sharedMesh;
        DestroyImmediate(sphere);
        return mesh;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Manually activate the trigger (useful for scripted events)
    /// </summary>
    public void ManuallyActivate()
    {
        if (CanActivate() && !isOnCooldown)
        {
            ActivateTrigger(triggerMode);
        }
    }

    /// <summary>
    /// Reset the trigger to its initial state
    /// </summary>
    public void ResetTrigger()
    {
        // TODO: STEP 16 - Implement trigger reset
        // HINT: Reset activation count, enable collider, clear cooldown

        activationCount = 0;
        isOnCooldown = false;
        playerInZone = false;

        if (triggerCollider != null)
        {
            triggerCollider.enabled = true;
        }

        if (debugRenderer != null && showTriggerZone)
        {
            debugRenderer.enabled = true;
        }

        Debug.Log($"Trigger reset: {gameObject.name}");
    }

    /// <summary>
    /// Get the number of times this trigger has been activated
    /// </summary>
    public int GetActivationCount()
    {
        return activationCount;
    }

    /// <summary>
    /// Check if trigger is currently on cooldown
    /// </summary>
    public bool IsOnCooldown()
    {
        return isOnCooldown;
    }

    #endregion

    #region Editor Helpers

    /// <summary>
    /// Draw gizmos in the editor for easier placement
    /// </summary>
    void OnDrawGizmos()
    {
        // TODO: STEP 17 - Draw trigger zone in Scene view
        // HINT: Use Gizmos to visualize the trigger area
        // Color code based on trigger state or mode

        Collider col = GetComponent<Collider>();
        if (col == null) return;

        // Set gizmo color based on activation limit
        Color gizmoColor = Color.green;
        if (activationLimit == ActivationLimit.Once)
            gizmoColor = Color.yellow;
        else if (activationLimit == ActivationLimit.Limited)
            gizmoColor = Color.cyan;

        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);

        // Draw based on collider type
        if (col is BoxCollider boxCol)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(boxCol.center, boxCol.size);
        }
        else if (col is SphereCollider sphereCol)
        {
            Gizmos.DrawSphere(transform.position + sphereCol.center, sphereCol.radius);
        }
    }

    /// <summary>
    /// Draw selected gizmo with more detail
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        Gizmos.color = Color.green;

        if (col is BoxCollider boxCol)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(boxCol.center, boxCol.size);
        }
        else if (col is SphereCollider sphereCol)
        {
            Gizmos.DrawWireSphere(transform.position + sphereCol.center, sphereCol.radius);
        }
    }

    #endregion
}

/*
 * STUDENT EXERCISE SUGGESTIONS:
 *
 * 1. ADVANCED TRIGGER CONDITIONS:
 *    - Require specific items in inventory to activate
 *    - Add time-of-day restrictions
 *    - Implement multi-object triggers (need 2+ players)
 *    - Add direction-based triggers (only from certain angles)
 *
 * 2. VISUAL ENHANCEMENTS:
 *    - Add shader effects when player is in range
 *    - Create animated trigger boundaries
 *    - Implement progressive effects (stronger when closer)
 *    - Add UI prompts when near trigger
 *
 * 3. GAMEPLAY FEATURES:
 *    - Create trigger chains (one activates next)
 *    - Implement timed trigger sequences
 *    - Add random activation chance
 *    - Create conditional triggers based on game state
 *
 * 4. OPTIMIZATION:
 *    - Use trigger layers for better performance
 *    - Implement spatial partitioning for many triggers
 *    - Add distance-based activation/deactivation
 *
 * 5. VR-SPECIFIC:
 *    - Add gaze-based activation
 *    - Implement gesture recognition triggers
 *    - Create hand proximity triggers
 *    - Add voice command integration
 */
