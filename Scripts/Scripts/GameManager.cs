using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Game Manager - Central controller for VR Adventure Quest Game
/// Manages game state, scoring, level progression, and global systems
/// Implements Singleton pattern for easy access from any script
///
/// USAGE:
/// 1. Attach to a GameObject named "GameManager" in your first scene
/// 2. Configure game settings in the Inspector
/// 3. Access from other scripts using: GameManager.Instance
/// 4. This persists across scenes (DontDestroyOnLoad)
///
/// Author: GDT-120 Course Materials
/// Date: 2026
/// </summary>
public class GameManager : MonoBehaviour
{
    #region Singleton Pattern

    private static GameManager instance;

    /// <summary>
    /// Singleton instance - access from anywhere with GameManager.Instance
    /// </summary>
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameManager>();

                if (instance == null)
                {
                    GameObject go = new GameObject("GameManager");
                    instance = go.AddComponent<GameManager>();
                }
            }
            return instance;
        }
    }

    #endregion

    #region Enums

    /// <summary>
    /// Current state of the game
    /// </summary>
    public enum GameState
    {
        MainMenu,       // In main menu
        Playing,        // Active gameplay
        Paused,         // Game paused
        GameOver,       // Player lost/died
        Victory,        // Level/game completed
        Loading         // Loading new scene
    }

    #endregion

    #region Inspector Variables

    [Header("Game Settings")]
    [Tooltip("Starting scene name (usually main menu)")]
    public string mainMenuScene = "MainMenu";

    [Tooltip("First gameplay scene")]
    public string firstLevelScene = "Level01";

    [Tooltip("Enable debug logging")]
    public bool debugMode = true;

    [Header("Player Settings")]
    [Tooltip("Starting player health")]
    [Range(1, 200)]
    public int startingHealth = 100;

    [Tooltip("Starting player lives")]
    [Range(1, 10)]
    public int startingLives = 3;

    [Header("Scoring")]
    [Tooltip("Enable score tracking")]
    public bool enableScoring = true;

    [Tooltip("Score multiplier (for difficulty settings)")]
    [Range(0.5f, 5f)]
    public float scoreMultiplier = 1f;

    [Header("Events")]
    [Tooltip("Event fired when game state changes")]
    public UnityEvent<GameState> OnGameStateChanged;

    [Tooltip("Event fired when score changes")]
    public UnityEvent<int> OnScoreChanged;

    [Tooltip("Event fired when player takes damage")]
    public UnityEvent<int> OnHealthChanged;

    [Tooltip("Event fired when game is won")]
    public UnityEvent OnGameWon;

    [Tooltip("Event fired when game is lost")]
    public UnityEvent OnGameLost;

    #endregion

    #region Public Properties

    // Game state
    public GameState CurrentState { get; private set; } = GameState.MainMenu;

    // Player stats
    public int CurrentHealth { get; private set; }
    public int CurrentLives { get; private set; }
    public int CurrentScore { get; private set; }

    // Gameplay data
    public float PlayTime { get; private set; }
    public int CollectiblesFound { get; private set; }
    public int TotalCollectibles { get; set; }
    public int PuzzlesSolved { get; private set; }
    public int TotalPuzzles { get; set; }

    #endregion

    #region Private Variables

    private bool isPaused = false;
    private Dictionary<string, bool> questItems = new Dictionary<string, bool>();
    private Dictionary<string, int> inventoryItems = new Dictionary<string, int>();

    #endregion

    #region Unity Lifecycle Methods

    void Awake()
    {
        // TODO: STEP 1 - Implement Singleton pattern
        // HINT: Check if instance already exists
        // If it does and it's not this, destroy this GameObject
        // Otherwise, set this as the instance and use DontDestroyOnLoad

        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // TODO: STEP 2 - Initialize game data
        InitializeGame();
    }

    void Start()
    {
        // TODO: STEP 3 - Setup initial game state
        // HINT: Load saved data if it exists
        // Set initial game state

        if (debugMode)
        {
            Debug.Log("GameManager initialized successfully.");
        }
    }

    void Update()
    {
        // TODO: STEP 4 - Track play time
        // HINT: Only increment PlayTime when CurrentState is Playing and not paused

        if (CurrentState == GameState.Playing && !isPaused)
        {
            PlayTime += Time.deltaTime;
        }

        // TODO: STEP 5 - Handle pause input (ESC key or Menu button on VR controller)
        // HINT: Check for Input.GetKeyDown(KeyCode.Escape)
        // Toggle pause state

        if (Input.GetKeyDown(KeyCode.Escape) && CurrentState == GameState.Playing)
        {
            TogglePause();
        }
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes game data to default values
    /// </summary>
    private void InitializeGame()
    {
        // TODO: STEP 6 - Initialize player stats
        // HINT: Set health, lives, and score to starting values

        CurrentHealth = startingHealth;
        CurrentLives = startingLives;
        CurrentScore = 0;
        PlayTime = 0f;
        CollectiblesFound = 0;
        PuzzlesSolved = 0;

        // Clear collections
        questItems.Clear();
        inventoryItems.Clear();

        if (debugMode)
        {
            Debug.Log($"Game initialized: Health={CurrentHealth}, Lives={CurrentLives}, Score={CurrentScore}");
        }
    }

    /// <summary>
    /// Resets game to initial state (for new game)
    /// </summary>
    public void NewGame()
    {
        // TODO: STEP 7 - Start a new game
        // HINT: Reset all stats, change state to Playing, load first level

        InitializeGame();
        ChangeGameState(GameState.Playing);

        if (!string.IsNullOrEmpty(firstLevelScene))
        {
            LoadScene(firstLevelScene);
        }

        if (debugMode)
        {
            Debug.Log("New game started!");
        }
    }

    #endregion

    #region Game State Management

    /// <summary>
    /// Changes the current game state
    /// </summary>
    public void ChangeGameState(GameState newState)
    {
        // TODO: STEP 8 - Implement state change logic
        // HINT: Store previous state, update CurrentState, invoke event
        // Handle state-specific logic (pause time, etc.)

        GameState previousState = CurrentState;
        CurrentState = newState;

        if (debugMode)
        {
            Debug.Log($"Game state changed: {previousState} -> {newState}");
        }

        // State-specific logic
        switch (newState)
        {
            case GameState.Playing:
                Time.timeScale = 1f;
                isPaused = false;
                break;

            case GameState.Paused:
                Time.timeScale = 0f;
                isPaused = true;
                break;

            case GameState.GameOver:
                Time.timeScale = 0f;
                OnGameLost?.Invoke();
                break;

            case GameState.Victory:
                Time.timeScale = 0f;
                OnGameWon?.Invoke();
                break;

            case GameState.Loading:
                // Could show loading screen here
                break;
        }

        OnGameStateChanged?.Invoke(newState);
    }

    /// <summary>
    /// Toggles pause state
    /// </summary>
    public void TogglePause()
    {
        // TODO: STEP 9 - Toggle between Playing and Paused states
        // HINT: If currently Playing, change to Paused and vice versa

        if (CurrentState == GameState.Playing)
        {
            ChangeGameState(GameState.Paused);
        }
        else if (CurrentState == GameState.Paused)
        {
            ChangeGameState(GameState.Playing);
        }
    }

    /// <summary>
    /// Triggers game over
    /// </summary>
    public void GameOver()
    {
        // TODO: STEP 10 - Handle game over
        // HINT: Change state, log final stats, could show game over screen

        ChangeGameState(GameState.GameOver);

        if (debugMode)
        {
            Debug.Log($"GAME OVER - Final Score: {CurrentScore}, Play Time: {PlayTime:F1}s");
        }
    }

    /// <summary>
    /// Triggers victory condition
    /// </summary>
    public void Victory()
    {
        // TODO: STEP 11 - Handle victory
        // HINT: Change state, log completion stats, could show victory screen

        ChangeGameState(GameState.Victory);

        if (debugMode)
        {
            Debug.Log($"VICTORY! Score: {CurrentScore}, Time: {PlayTime:F1}s, Collectibles: {CollectiblesFound}/{TotalCollectibles}");
        }
    }

    #endregion

    #region Player Stats Management

    /// <summary>
    /// Adds points to the player's score
    /// </summary>
    public void AddPoints(int points)
    {
        // TODO: STEP 12 - Add points to score
        // HINT: Apply score multiplier, update CurrentScore, invoke event

        if (!enableScoring) return;

        int adjustedPoints = Mathf.RoundToInt(points * scoreMultiplier);
        CurrentScore += adjustedPoints;

        OnScoreChanged?.Invoke(CurrentScore);

        if (debugMode)
        {
            Debug.Log($"Points added: +{adjustedPoints} (Total: {CurrentScore})");
        }
    }

    /// <summary>
    /// Removes points from the player's score (for penalties)
    /// </summary>
    public void RemovePoints(int points)
    {
        // TODO: STEP 13 - Remove points from score
        // HINT: Similar to AddPoints, but subtract and don't go below 0

        if (!enableScoring) return;

        int adjustedPoints = Mathf.RoundToInt(points * scoreMultiplier);
        CurrentScore = Mathf.Max(0, CurrentScore - adjustedPoints);

        OnScoreChanged?.Invoke(CurrentScore);

        if (debugMode)
        {
            Debug.Log($"Points removed: -{adjustedPoints} (Total: {CurrentScore})");
        }
    }

    /// <summary>
    /// Deals damage to the player
    /// </summary>
    public void TakeDamage(int damage)
    {
        // TODO: STEP 14 - Handle player damage
        // HINT: Reduce health, invoke event, check for death

        CurrentHealth -= damage;
        CurrentHealth = Mathf.Max(0, CurrentHealth);

        OnHealthChanged?.Invoke(CurrentHealth);

        if (debugMode)
        {
            Debug.Log($"Player took {damage} damage. Health: {CurrentHealth}/{startingHealth}");
        }

        // Check for death
        if (CurrentHealth <= 0)
        {
            PlayerDied();
        }
    }

    /// <summary>
    /// Heals the player
    /// </summary>
    public void Heal(int amount)
    {
        // TODO: STEP 15 - Heal the player
        // HINT: Increase health up to maximum (startingHealth), invoke event

        CurrentHealth += amount;
        CurrentHealth = Mathf.Min(startingHealth, CurrentHealth);

        OnHealthChanged?.Invoke(CurrentHealth);

        if (debugMode)
        {
            Debug.Log($"Player healed {amount}. Health: {CurrentHealth}/{startingHealth}");
        }
    }

    /// <summary>
    /// Handles player death
    /// </summary>
    private void PlayerDied()
    {
        // TODO: STEP 16 - Handle player death
        // HINT: Decrease lives, check if lives remain
        // If lives > 0, respawn; otherwise, game over

        CurrentLives--;

        if (debugMode)
        {
            Debug.Log($"Player died! Lives remaining: {CurrentLives}");
        }

        if (CurrentLives > 0)
        {
            // Respawn player
            RespawnPlayer();
        }
        else
        {
            // Game over
            GameOver();
        }
    }

    /// <summary>
    /// Respawns the player at the last checkpoint
    /// </summary>
    private void RespawnPlayer()
    {
        // TODO: STEP 17 - Respawn the player
        // HINT: Reset health, find and move player to respawn point
        // This is a simplified version - students should expand

        CurrentHealth = startingHealth;
        OnHealthChanged?.Invoke(CurrentHealth);

        // Find respawn point (students should implement checkpoint system)
        GameObject respawnPoint = GameObject.FindGameObjectWithTag("Respawn");
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null && respawnPoint != null)
        {
            player.transform.position = respawnPoint.transform.position;
            player.transform.rotation = respawnPoint.transform.rotation;
        }

        if (debugMode)
        {
            Debug.Log("Player respawned.");
        }
    }

    #endregion

    #region Collectibles & Progress Tracking

    /// <summary>
    /// Records a collectible being found
    /// </summary>
    public void CollectibleFound()
    {
        // TODO: STEP 18 - Track collectible collection
        // HINT: Increment counter, check if all collectibles found

        CollectiblesFound++;

        if (debugMode)
        {
            Debug.Log($"Collectible found: {CollectiblesFound}/{TotalCollectibles}");
        }

        // Check for completion
        if (TotalCollectibles > 0 && CollectiblesFound >= TotalCollectibles)
        {
            if (debugMode)
            {
                Debug.Log("All collectibles found!");
            }
            // Could trigger bonus or achievement here
        }
    }

    /// <summary>
    /// Records a puzzle being solved
    /// </summary>
    public void PuzzleSolved()
    {
        // TODO: STEP 19 - Track puzzle completion
        // HINT: Increment counter, award bonus points, check if all puzzles solved

        PuzzlesSolved++;
        AddPoints(100); // Bonus points for solving puzzle

        if (debugMode)
        {
            Debug.Log($"Puzzle solved: {PuzzlesSolved}/{TotalPuzzles}");
        }

        // Check for completion
        if (TotalPuzzles > 0 && PuzzlesSolved >= TotalPuzzles)
        {
            if (debugMode)
            {
                Debug.Log("All puzzles solved!");
            }
            // Could trigger victory or unlock new area
        }
    }

    /// <summary>
    /// Gets completion percentage
    /// </summary>
    public float GetCompletionPercentage()
    {
        // TODO: STEP 20 - Calculate completion percentage
        // HINT: Consider collectibles, puzzles, and other objectives
        // Return value between 0-100

        float collectiblePercent = TotalCollectibles > 0 ? (float)CollectiblesFound / TotalCollectibles : 0f;
        float puzzlePercent = TotalPuzzles > 0 ? (float)PuzzlesSolved / TotalPuzzles : 0f;

        // Average of both (could weight differently)
        float totalPercent = ((collectiblePercent + puzzlePercent) / 2f) * 100f;

        return totalPercent;
    }

    #endregion

    #region Inventory Management

    /// <summary>
    /// Adds a quest item to inventory
    /// </summary>
    public void AddQuestItem(string itemID)
    {
        // TODO: STEP 21 - Add quest item to inventory
        // HINT: Add to questItems dictionary if not already present

        if (!questItems.ContainsKey(itemID))
        {
            questItems[itemID] = true;

            if (debugMode)
            {
                Debug.Log($"Quest item acquired: {itemID}");
            }
        }
    }

    /// <summary>
    /// Checks if player has a specific quest item
    /// </summary>
    public bool HasQuestItem(string itemID)
    {
        return questItems.ContainsKey(itemID) && questItems[itemID];
    }

    /// <summary>
    /// Adds a countable item to inventory
    /// </summary>
    public void AddInventoryItem(string itemID, int quantity = 1)
    {
        // TODO: STEP 22 - Add countable items to inventory
        // HINT: Use inventoryItems dictionary to track quantities

        if (inventoryItems.ContainsKey(itemID))
        {
            inventoryItems[itemID] += quantity;
        }
        else
        {
            inventoryItems[itemID] = quantity;
        }

        if (debugMode)
        {
            Debug.Log($"Inventory item added: {itemID} x{quantity} (Total: {inventoryItems[itemID]})");
        }
    }

    /// <summary>
    /// Gets quantity of an item in inventory
    /// </summary>
    public int GetInventoryItemCount(string itemID)
    {
        return inventoryItems.ContainsKey(itemID) ? inventoryItems[itemID] : 0;
    }

    #endregion

    #region Scene Management

    /// <summary>
    /// Loads a new scene by name
    /// </summary>
    public void LoadScene(string sceneName)
    {
        // TODO: STEP 23 - Load a new scene
        // HINT: Change state to Loading, use SceneManager.LoadScene()

        ChangeGameState(GameState.Loading);

        if (debugMode)
        {
            Debug.Log($"Loading scene: {sceneName}");
        }

        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Reloads the current scene
    /// </summary>
    public void ReloadCurrentScene()
    {
        // TODO: STEP 24 - Reload current scene
        // HINT: Get active scene name and reload it

        string currentScene = SceneManager.GetActiveScene().name;
        LoadScene(currentScene);
    }

    /// <summary>
    /// Returns to main menu
    /// </summary>
    public void ReturnToMainMenu()
    {
        // TODO: STEP 25 - Return to main menu
        // HINT: Reset game state, load main menu scene

        ChangeGameState(GameState.MainMenu);
        LoadScene(mainMenuScene);
    }

    /// <summary>
    /// Quits the application
    /// </summary>
    public void QuitGame()
    {
        if (debugMode)
        {
            Debug.Log("Quitting game...");
        }

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    #endregion

    #region Save/Load System (Simplified)

    /// <summary>
    /// Saves current game state to PlayerPrefs (simplified version)
    /// </summary>
    public void SaveGame()
    {
        // TODO: STEP 26 - Implement save system
        // HINT: Use PlayerPrefs to save essential data
        // For a full game, use JSON or binary serialization

        PlayerPrefs.SetInt("CurrentScore", CurrentScore);
        PlayerPrefs.SetInt("CurrentHealth", CurrentHealth);
        PlayerPrefs.SetInt("CurrentLives", CurrentLives);
        PlayerPrefs.SetInt("CollectiblesFound", CollectiblesFound);
        PlayerPrefs.SetInt("PuzzlesSolved", PuzzlesSolved);
        PlayerPrefs.SetFloat("PlayTime", PlayTime);
        PlayerPrefs.SetString("CurrentScene", SceneManager.GetActiveScene().name);

        PlayerPrefs.Save();

        if (debugMode)
        {
            Debug.Log("Game saved successfully.");
        }
    }

    /// <summary>
    /// Loads saved game state from PlayerPrefs
    /// </summary>
    public void LoadGame()
    {
        // TODO: STEP 27 - Implement load system
        // HINT: Load data from PlayerPrefs, apply to current game state

        if (PlayerPrefs.HasKey("CurrentScore"))
        {
            CurrentScore = PlayerPrefs.GetInt("CurrentScore");
            CurrentHealth = PlayerPrefs.GetInt("CurrentHealth");
            CurrentLives = PlayerPrefs.GetInt("CurrentLives");
            CollectiblesFound = PlayerPrefs.GetInt("CollectiblesFound");
            PuzzlesSolved = PlayerPrefs.GetInt("PuzzlesSolved");
            PlayTime = PlayerPrefs.GetFloat("PlayTime");

            string savedScene = PlayerPrefs.GetString("CurrentScene");

            if (debugMode)
            {
                Debug.Log($"Game loaded. Score: {CurrentScore}, Health: {CurrentHealth}");
            }

            // Load the saved scene
            if (!string.IsNullOrEmpty(savedScene))
            {
                LoadScene(savedScene);
            }
        }
        else
        {
            if (debugMode)
            {
                Debug.Log("No saved game found.");
            }
        }
    }

    #endregion
}

/*
 * STUDENT EXERCISE SUGGESTIONS:
 *
 * 1. ADVANCED SAVE SYSTEM:
 *    - Implement JSON serialization for complex data
 *    - Add multiple save slots
 *    - Include auto-save functionality
 *    - Compress save data
 *
 * 2. PROGRESSION SYSTEM:
 *    - Add experience points and leveling
 *    - Implement skill trees or upgrades
 *    - Track achievements and trophies
 *    - Create leaderboards
 *
 * 3. ANALYTICS:
 *    - Track player behavior and metrics
 *    - Log common death locations
 *    - Record puzzle completion times
 *    - Generate play session reports
 *
 * 4. DIFFICULTY SYSTEM:
 *    - Implement multiple difficulty levels
 *    - Add dynamic difficulty adjustment
 *    - Create hardcore/permadeath modes
 *    - Add accessibility options
 *
 * 5. MULTIPLAYER SUPPORT:
 *    - Synchronize game state across network
 *    - Handle disconnections gracefully
 *    - Implement co-op progression
 *    - Add competitive modes
 */
