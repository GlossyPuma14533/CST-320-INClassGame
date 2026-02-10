using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Collectible Item Script for VR Adventure Quest Game
/// Handles collectible items (coins, crystals, power-ups, etc.)
/// Includes visual feedback, audio, scoring, and VR-specific interactions
///
/// USAGE:
/// 1. Attach this script to a GameObject representing a collectible
/// 2. Add a Collider component with "Is Trigger" enabled
/// 3. Configure item properties in the Inspector
/// 4. Tag the player GameObject as "Player" or set customPlayerTag
///
/// Author: GDT-120 Course Materials
/// Date: 2026
/// </summary>
[RequireComponent(typeof(Collider))]
public class CollectibleItem : MonoBehaviour
{
    #region Enums

    /// <summary>
    /// Type of collectible item - determines behavior and value
    /// </summary>
    public enum CollectibleType
    {
        Coin,           // Basic currency/points
        Crystal,        // Rare valuable item
        HealthPotion,   // Restores player health
        PowerUp,        // Temporary ability boost
        QuestItem       // Required for quest completion
    }

    #endregion

    #region Inspector Variables

    [Header("Collectible Properties")]
    [Tooltip("Type of collectible - determines behavior")]
    public CollectibleType itemType = CollectibleType.Coin;

    [Tooltip("Points awarded when collected")]
    [Range(1, 1000)]
    public int pointValue = 10;

    [Tooltip("Unique identifier for quest items")]
    public string itemID = "";

    [Header("Visual Settings")]
    [Tooltip("Should the item rotate continuously?")]
    public bool rotateItem = true;

    [Tooltip("Rotation speed (degrees per second)")]
    public float rotationSpeed = 90f;

    [Tooltip("Should the item bob up and down?")]
    public bool bobItem = true;

    [Tooltip("Bob height in units")]
    public float bobHeight = 0.2f;

    [Tooltip("Bob speed")]
    public float bobSpeed = 2f;

    [Tooltip("Particle effect to play on collection (optional)")]
    public ParticleSystem collectionEffect;

    [Header("Audio Settings")]
    [Tooltip("Sound to play when collected")]
    public AudioClip collectionSound;

    [Tooltip("Volume of collection sound (0-1)")]
    [Range(0f, 1f)]
    public float soundVolume = 1f;

    [Header("Interaction Settings")]
    [Tooltip("Tag of player GameObject (default: 'Player')")]
    public string playerTag = "Player";

    [Tooltip("Can item be collected by VR controller raycast?")]
    public bool allowRaycastCollection = false;

    [Tooltip("Distance from which item can be collected by raycast")]
    public float raycastCollectionDistance = 5f;

    [Header("Events")]
    [Tooltip("Event triggered when item is collected")]
    public UnityEvent OnCollected;

    #endregion

    #region Private Variables

    private Vector3 startPosition;
    private float bobOffset;
    private Collider itemCollider;
    private MeshRenderer meshRenderer;
    private bool isCollected = false;
    private AudioSource audioSource;

    #endregion

    #region Unity Lifecycle Methods

    void Start()
    {
        // TODO: STEP 1 - Initialize component references
        // HINT: Get the Collider and MeshRenderer components
        // Store the starting position for bob animation

        itemCollider = GetComponent<Collider>();
        meshRenderer = GetComponent<MeshRenderer>();
        startPosition = transform.position;

        // TODO: STEP 2 - Verify collider is set as trigger
        // HINT: Check if itemCollider.isTrigger is true
        // If not, log a warning and set it to true

        if (itemCollider != null && !itemCollider.isTrigger)
        {
            Debug.LogWarning($"CollectibleItem on {gameObject.name}: Collider is not set as trigger. Setting it now.");
            itemCollider.isTrigger = true;
        }

        // TODO: STEP 3 - Setup audio source if collection sound is assigned
        // HINT: Add an AudioSource component if one doesn't exist
        // Configure it to not play on awake and to play 3D sound

        if (collectionSound != null)
        {
            audioSource = gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.clip = collectionSound;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D sound
            audioSource.volume = soundVolume;
        }

        // TODO: STEP 4 - Generate random bob offset for variety
        // HINT: Use Random.Range(0f, Mathf.PI * 2f) to randomize starting phase
        // This makes multiple collectibles bob out of sync

        bobOffset = Random.Range(0f, Mathf.PI * 2f);

        // TODO: STEP 5 - Validate item ID for quest items
        // HINT: If itemType is QuestItem and itemID is empty, log a warning

        if (itemType == CollectibleType.QuestItem && string.IsNullOrEmpty(itemID))
        {
            Debug.LogWarning($"CollectibleItem on {gameObject.name}: Quest item has no ID assigned!");
        }
    }

    void Update()
    {
        // Skip animation if already collected
        if (isCollected) return;

        // TODO: STEP 6 - Implement rotation animation
        // HINT: Use transform.Rotate() if rotateItem is true
        // Rotate around the Y axis using rotationSpeed and Time.deltaTime

        if (rotateItem)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }

        // TODO: STEP 7 - Implement bob animation
        // HINT: Use Mathf.Sin() to create smooth up/down motion
        // Calculate new Y position based on startPosition, bobHeight, and time
        // Use bobOffset to vary the phase

        if (bobItem)
        {
            float newY = startPosition.y + Mathf.Sin(Time.time * bobSpeed + bobOffset) * bobHeight;
            transform.position = new Vector3(startPosition.x, newY, startPosition.z);
        }

        // TODO: STEP 8 (ADVANCED) - Implement raycast collection for VR
        // HINT: This would check if VR controller is pointing at item and trigger is pressed
        // For now, this is left as an advanced exercise
        // You would need to raycast from XR controllers and check distance

        if (allowRaycastCollection)
        {
            // Advanced: Implement VR controller raycast detection here
            // This requires XR Interaction Toolkit integration
        }
    }

    #endregion

    #region Collision Detection

    /// <summary>
    /// Called when another collider enters this trigger collider
    /// Handles collection logic
    /// </summary>
    void OnTriggerEnter(Collider other)
    {
        // TODO: STEP 9 - Check if the colliding object is the player
        // HINT: Use other.CompareTag(playerTag)
        // Also check if item hasn't been collected yet (!isCollected)

        if (other.CompareTag(playerTag) && !isCollected)
        {
            CollectItem(other.gameObject);
        }
    }

    #endregion

    #region Collection Logic

    /// <summary>
    /// Handles the collection of this item
    /// </summary>
    /// <param name="collector">The GameObject that collected this item (usually the player)</param>
    private void CollectItem(GameObject collector)
    {
        // Mark as collected to prevent double-collection
        isCollected = true;

        // TODO: STEP 10 - Play collection sound
        // HINT: Use audioSource.Play() if audioSource is not null

        if (audioSource != null && collectionSound != null)
        {
            audioSource.Play();
        }

        // TODO: STEP 11 - Spawn collection particle effect
        // HINT: Use Instantiate() to create the particle effect at this position
        // If collectionEffect is assigned, instantiate it and destroy after duration

        if (collectionEffect != null)
        {
            ParticleSystem effect = Instantiate(collectionEffect, transform.position, Quaternion.identity);
            Destroy(effect.gameObject, effect.main.duration);
        }

        // TODO: STEP 12 - Handle different collectible types
        // HINT: Use a switch statement on itemType
        // Call appropriate methods based on type

        switch (itemType)
        {
            case CollectibleType.Coin:
            case CollectibleType.Crystal:
                AddPoints(collector);
                break;

            case CollectibleType.HealthPotion:
                RestoreHealth(collector);
                AddPoints(collector); // Also give points
                break;

            case CollectibleType.PowerUp:
                ActivatePowerUp(collector);
                AddPoints(collector);
                break;

            case CollectibleType.QuestItem:
                AddToInventory(collector);
                break;
        }

        // TODO: STEP 13 - Invoke the OnCollected event
        // HINT: Use OnCollected?.Invoke() to safely call the event
        // This allows other scripts to respond to collection

        OnCollected?.Invoke();

        // TODO: STEP 14 - Destroy or hide the collectible
        // HINT: Either destroy immediately or hide mesh and destroy after audio finishes
        // If there's audio, wait for it to finish playing

        if (audioSource != null && collectionSound != null)
        {
            // Hide the mesh but keep object alive for audio
            if (meshRenderer != null)
            {
                meshRenderer.enabled = false;
            }

            // Disable collider to prevent re-collection
            if (itemCollider != null)
            {
                itemCollider.enabled = false;
            }

            // Destroy after audio finishes
            Destroy(gameObject, collectionSound.length);
        }
        else
        {
            // No audio, destroy immediately
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Adds points to the player's score
    /// </summary>
    private void AddPoints(GameObject collector)
    {
        // TODO: STEP 15 - Award points to the player
        // HINT: Find the GameManager and call its AddPoints method
        // This assumes you have a GameManager with an AddPoints method

        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.AddPoints(pointValue);
            Debug.Log($"Collected {itemType}: +{pointValue} points");
        }
        else
        {
            Debug.LogWarning("GameManager not found! Cannot add points.");
            // Fallback: Just log the points
            Debug.Log($"Collected {itemType}: Would add {pointValue} points");
        }
    }

    /// <summary>
    /// Restores health to the player
    /// </summary>
    private void RestoreHealth(GameObject collector)
    {
        // TODO: STEP 16 - Restore player health
        // HINT: Get the PlayerHealth component from collector
        // Call its Heal or RestoreHealth method
        // This is a placeholder - implement based on your player health system

        // Example implementation (customize based on your health system):
        /*
        PlayerHealth playerHealth = collector.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.Heal(pointValue);
            Debug.Log($"Health restored: +{pointValue} HP");
        }
        */

        Debug.Log($"Health Potion collected: Would restore {pointValue} health");
    }

    /// <summary>
    /// Activates a power-up effect on the player
    /// </summary>
    private void ActivatePowerUp(GameObject collector)
    {
        // TODO: STEP 17 - Activate power-up effect
        // HINT: This could increase speed, strength, invincibility, etc.
        // Implement based on your game's power-up system
        // Consider using a PowerUpManager or PlayerStats component

        // Example implementation (customize based on your system):
        /*
        PlayerStats playerStats = collector.GetComponent<PlayerStats>();
        if (playerStats != null)
        {
            playerStats.ActivatePowerUp(itemID, 10f); // 10 second duration
            Debug.Log($"Power-up activated: {itemID}");
        }
        */

        Debug.Log($"Power-up collected: {itemID}");
    }

    /// <summary>
    /// Adds quest item to player's inventory
    /// </summary>
    private void AddToInventory(GameObject collector)
    {
        // TODO: STEP 18 - Add item to player inventory
        // HINT: Find the InventoryManager and call AddItem
        // Use the itemID to identify this quest item

        // Example implementation (customize based on your inventory system):
        /*
        InventoryManager inventory = FindObjectOfType<InventoryManager>();
        if (inventory != null)
        {
            inventory.AddItem(itemID);
            Debug.Log($"Quest item added to inventory: {itemID}");
        }
        */

        Debug.Log($"Quest item collected: {itemID}");
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Manually trigger collection (useful for VR controller interactions)
    /// </summary>
    public void Collect(GameObject collector)
    {
        if (!isCollected)
        {
            CollectItem(collector);
        }
    }

    /// <summary>
    /// Reset the collectible to its initial state
    /// Useful for respawning items
    /// </summary>
    public void ResetCollectible()
    {
        // TODO: STEP 19 - Implement reset functionality
        // HINT: Reset position, enable mesh and collider, set isCollected to false

        isCollected = false;
        transform.position = startPosition;

        if (meshRenderer != null)
        {
            meshRenderer.enabled = true;
        }

        if (itemCollider != null)
        {
            itemCollider.enabled = true;
        }
    }

    #endregion

    #region Editor Helpers

    // Draw gizmos in the editor for easier visualization
    void OnDrawGizmos()
    {
        // TODO: STEP 20 - Draw editor gizmos for collectibles
        // HINT: Use Gizmos.DrawWireSphere to show collection radius
        // Use different colors for different collectible types

        Color gizmoColor = Color.yellow;

        switch (itemType)
        {
            case CollectibleType.Coin:
                gizmoColor = Color.yellow;
                break;
            case CollectibleType.Crystal:
                gizmoColor = Color.cyan;
                break;
            case CollectibleType.HealthPotion:
                gizmoColor = Color.green;
                break;
            case CollectibleType.PowerUp:
                gizmoColor = Color.magenta;
                break;
            case CollectibleType.QuestItem:
                gizmoColor = Color.blue;
                break;
        }

        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        // Draw raycast collection range if enabled
        if (allowRaycastCollection)
        {
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);
            Gizmos.DrawWireSphere(transform.position, raycastCollectionDistance);
        }
    }

    #endregion
}

/*
 * STUDENT EXERCISE SUGGESTIONS:
 *
 * 1. ADVANCED VR INTERACTION:
 *    - Implement grab-to-collect using XR Grab Interactable
 *    - Add haptic feedback when collecting items
 *    - Create proximity-based highlighting
 *
 * 2. VISUAL ENHANCEMENTS:
 *    - Add glow effect to rare items
 *    - Implement trail renderer for movement
 *    - Create custom shaders for collectibles
 *
 * 3. GAMEPLAY FEATURES:
 *    - Add magnetic collection (auto-collect nearby items)
 *    - Implement combo system for rapid collections
 *    - Create different particle effects per item type
 *
 * 4. MULTIPLAYER SUPPORT:
 *    - Synchronize collection across network
 *    - Add collection confirmation for lag
 *    - Implement fair distribution in co-op
 *
 * 5. ANALYTICS:
 *    - Track collection rates
 *    - Log item placement effectiveness
 *    - Create heatmaps of collection locations
 */
