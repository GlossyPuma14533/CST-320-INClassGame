using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Puzzle Manager - Coordinates complex multi-element puzzles
/// Handles puzzle sequences, combinations, and validation
/// Works in conjunction with PuzzleElement components
///
/// USAGE:
/// 1. Attach to an empty GameObject in your scene
/// 2. Assign all related PuzzleElement components
/// 3. Configure puzzle logic in the Inspector
/// 4. Link to game events (door opening, item spawning, etc.)
///
/// Author: GDT-120 Course Materials
/// Date: 2026
/// </summary>
public class PuzzleManager : MonoBehaviour
{
    #region Enums

    /// <summary>
    /// Type of puzzle coordination
    /// </summary>
    public enum PuzzleMode
    {
        Sequential,     // Elements must be activated in specific order
        Simultaneous,   // All elements must be active at once
        Any,            // Any combination that activates all elements
        Pattern,        // Specific pattern/combination required
        Timed           // Sequential with time limits between steps
    }

    #endregion

    #region Inspector Variables

    [Header("Puzzle Configuration")]
    [Tooltip("Unique identifier for this puzzle")]
    public string puzzleID = "Puzzle_01";

    [Tooltip("Type of puzzle coordination")]
    public PuzzleMode puzzleMode = PuzzleMode.Sequential;

    [Tooltip("All elements involved in this puzzle")]
    public List<PuzzleElement> puzzleElements = new List<PuzzleElement>();

    [Tooltip("Allow puzzle to be reset after completion?")]
    public bool allowReset = false;

    [Header("Timed Sequence Settings")]
    [Tooltip("Maximum time between sequence steps (0 = unlimited)")]
    [Range(0f, 60f)]
    public float sequenceTimeout = 10f;

    [Tooltip("Reset sequence if timeout occurs?")]
    public bool resetOnTimeout = true;

    [Header("Rewards")]
    [Tooltip("Points awarded for solving puzzle")]
    public int completionPoints = 500;

    [Tooltip("GameObjects to activate when puzzle is solved (doors, platforms, etc.)")]
    public List<GameObject> objectsToActivate = new List<GameObject>();

    [Tooltip("GameObjects to deactivate when puzzle is solved")]
    public List<GameObject> objectsToDeactivate = new List<GameObject>();

    [Header("Audio")]
    [Tooltip("Sound played when puzzle is completed")]
    public AudioClip completionSound;

    [Tooltip("Sound played when step is correct")]
    public AudioClip correctStepSound;

    [Tooltip("Sound played when step is incorrect")]
    public AudioClip incorrectStepSound;

    [Range(0f, 1f)]
    public float soundVolume = 1f;

    [Header("Visual Feedback")]
    [Tooltip("Particle effect spawned when puzzle is solved")]
    public ParticleSystem completionEffect;

    [Header("Events")]
    [Tooltip("Event fired when puzzle is solved")]
    public UnityEvent OnPuzzleSolved;

    [Tooltip("Event fired when a correct step is made")]
    public UnityEvent OnCorrectStep;

    [Tooltip("Event fired when an incorrect step is made")]
    public UnityEvent OnIncorrectStep;

    [Tooltip("Event fired when puzzle is reset")]
    public UnityEvent OnPuzzleReset;

    #endregion

    #region Private Variables

    private bool isPuzzleSolved = false;
    private int currentSequenceStep = 0;
    private float lastStepTime = 0f;
    private List<string> activationHistory = new List<string>();
    private AudioSource audioSource;
    private Dictionary<PuzzleElement, bool> elementStates = new Dictionary<PuzzleElement, bool>();

    #endregion

    #region Unity Lifecycle Methods

    void Start()
    {
        // TODO: STEP 1 - Initialize the puzzle manager
        // HINT: Setup audio source, validate puzzle elements, initialize element states

        // Setup audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0.5f; // Partially 3D
        audioSource.volume = soundVolume;

        // TODO: STEP 2 - Validate puzzle elements
        // HINT: Check if elements list is not empty
        // Sort elements by sequence order for sequential puzzles
        // Initialize element state dictionary

        if (puzzleElements == null || puzzleElements.Count == 0)
        {
            Debug.LogError($"PuzzleManager '{puzzleID}': No puzzle elements assigned!");
            return;
        }

        // Remove null elements
        puzzleElements.RemoveAll(element => element == null);

        // Sort by sequence order for sequential/timed puzzles
        if (puzzleMode == PuzzleMode.Sequential || puzzleMode == PuzzleMode.Timed)
        {
            puzzleElements = puzzleElements.OrderBy(e => e.GetSequenceOrder()).ToList();
        }

        // Initialize element states
        foreach (var element in puzzleElements)
        {
            elementStates[element] = false;
        }

        // TODO: STEP 3 - Deactivate reward objects initially
        // HINT: Set all objectsToActivate to inactive
        // Set all objectsToDeactivate to active

        foreach (var obj in objectsToActivate)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }

        foreach (var obj in objectsToDeactivate)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }

        Debug.Log($"PuzzleManager initialized: {puzzleID} (Mode: {puzzleMode}, Elements: {puzzleElements.Count})");
    }

    void Update()
    {
        // TODO: STEP 4 - Handle timed sequence timeout
        // HINT: If puzzleMode is Timed and we're mid-sequence
        // Check if time since last step exceeds sequenceTimeout
        // Reset sequence if timeout occurs

        if (puzzleMode == PuzzleMode.Timed && currentSequenceStep > 0 && !isPuzzleSolved)
        {
            if (sequenceTimeout > 0 && Time.time - lastStepTime > sequenceTimeout)
            {
                Debug.Log($"Puzzle sequence timed out! ({sequenceTimeout}s exceeded)");
                PlaySound(incorrectStepSound);
                OnIncorrectStep?.Invoke();

                if (resetOnTimeout)
                {
                    ResetPuzzle();
                }
            }
        }
    }

    #endregion

    #region Puzzle Validation

    /// <summary>
    /// Validates if a puzzle element activation is correct
    /// Called by PuzzleElement when it's activated
    /// </summary>
    public bool ValidateSequenceStep(PuzzleElement element)
    {
        // TODO: STEP 5 - Validate the activation based on puzzle mode
        // HINT: Different validation logic for each mode
        // Return true if activation is valid

        if (isPuzzleSolved)
        {
            Debug.Log($"Puzzle already solved: {puzzleID}");
            return false;
        }

        bool isValid = false;

        switch (puzzleMode)
        {
            case PuzzleMode.Sequential:
            case PuzzleMode.Timed:
                isValid = ValidateSequentialStep(element);
                break;

            case PuzzleMode.Simultaneous:
                isValid = ValidateSimultaneousStep(element);
                break;

            case PuzzleMode.Any:
                isValid = true; // Any activation is valid
                break;

            case PuzzleMode.Pattern:
                isValid = ValidatePatternStep(element);
                break;
        }

        // Update state
        if (isValid)
        {
            elementStates[element] = true;
            activationHistory.Add(element.elementID);
            lastStepTime = Time.time;

            PlaySound(correctStepSound);
            OnCorrectStep?.Invoke();

            Debug.Log($"Correct step: {element.elementID} (Step {currentSequenceStep + 1}/{puzzleElements.Count})");

            // Check if puzzle is complete
            CheckPuzzleCompletion();
        }
        else
        {
            PlaySound(incorrectStepSound);
            OnIncorrectStep?.Invoke();

            Debug.Log($"Incorrect step: {element.elementID}");
        }

        return isValid;
    }

    /// <summary>
    /// Validates sequential puzzle step
    /// </summary>
    private bool ValidateSequentialStep(PuzzleElement element)
    {
        // TODO: STEP 6 - Validate sequential activation
        // HINT: Check if this element is the next one in sequence
        // Compare element's sequence order with currentSequenceStep

        if (currentSequenceStep >= puzzleElements.Count)
        {
            return false; // Already completed
        }

        PuzzleElement expectedElement = puzzleElements[currentSequenceStep];

        if (element == expectedElement)
        {
            currentSequenceStep++;
            return true;
        }

        // Wrong element - reset sequence
        if (resetOnTimeout)
        {
            Debug.Log("Wrong sequence element activated. Resetting...");
            ResetSequence();
        }

        return false;
    }

    /// <summary>
    /// Validates simultaneous puzzle (all must be active at once)
    /// </summary>
    private bool ValidateSimultaneousStep(PuzzleElement element)
    {
        // TODO: STEP 7 - Validate simultaneous activation
        // HINT: Just accept the activation - we'll check if all are active later
        // Return true to allow activation

        return true; // Always valid, check completion separately
    }

    /// <summary>
    /// Validates pattern-based puzzle
    /// </summary>
    private bool ValidatePatternStep(PuzzleElement element)
    {
        // TODO: STEP 8 - Validate pattern (students implement custom logic)
        // HINT: This is for complex patterns like "up, up, down, down, left, right"
        // Students should implement based on their specific pattern needs

        // Example: Could check activation history against a predefined pattern
        // For now, accept all activations and check pattern on completion

        return true; // Placeholder - students implement custom pattern logic
    }

    /// <summary>
    /// Checks if the puzzle is completed
    /// </summary>
    private void CheckPuzzleCompletion()
    {
        // TODO: STEP 9 - Check if puzzle is solved
        // HINT: Different completion conditions for each mode

        bool isComplete = false;

        switch (puzzleMode)
        {
            case PuzzleMode.Sequential:
            case PuzzleMode.Timed:
                // Complete when all steps done in order
                isComplete = currentSequenceStep >= puzzleElements.Count;
                break;

            case PuzzleMode.Simultaneous:
                // Complete when all elements are currently active
                isComplete = CheckAllElementsActive();
                break;

            case PuzzleMode.Any:
                // Complete when all elements have been activated (any order)
                isComplete = elementStates.Values.All(state => state);
                break;

            case PuzzleMode.Pattern:
                // Custom pattern check (students implement)
                isComplete = CheckPattern();
                break;
        }

        if (isComplete)
        {
            SolvePuzzle();
        }
    }

    /// <summary>
    /// Checks if all elements are currently in active state
    /// </summary>
    private bool CheckAllElementsActive()
    {
        // TODO: STEP 10 - Check if all elements are active simultaneously
        // HINT: Query each PuzzleElement's current state
        // Return true only if ALL are in Active state

        foreach (var element in puzzleElements)
        {
            if (element.GetState() != PuzzleElement.ElementState.Active)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Checks if activation pattern matches required pattern
    /// </summary>
    private bool CheckPattern()
    {
        // TODO: STEP 11 - Implement pattern matching (advanced)
        // HINT: Compare activationHistory against a predefined pattern
        // Students should implement based on their specific needs

        // Example pattern: All elements activated in numerical order
        if (activationHistory.Count != puzzleElements.Count)
        {
            return false;
        }

        // Simple example: just check if all elements were activated
        return elementStates.Values.All(state => state);
    }

    #endregion

    #region Puzzle Completion

    /// <summary>
    /// Marks the puzzle as solved and triggers rewards
    /// </summary>
    private void SolvePuzzle()
    {
        // TODO: STEP 12 - Handle puzzle completion
        // HINT: Set solved flag, award points, activate rewards, play effects

        isPuzzleSolved = true;

        Debug.Log($"PUZZLE SOLVED: {puzzleID}");

        // Play completion sound and effects
        PlaySound(completionSound);

        if (completionEffect != null)
        {
            ParticleSystem effect = Instantiate(completionEffect, transform.position, Quaternion.identity);
            Destroy(effect.gameObject, effect.main.duration + 1f);
        }

        // Award points
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddPoints(completionPoints);
            GameManager.Instance.PuzzleSolved();
        }

        // Activate/deactivate reward objects
        foreach (var obj in objectsToActivate)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }

        foreach (var obj in objectsToDeactivate)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }

        // Mark all elements as solved
        foreach (var element in puzzleElements)
        {
            element.SetSolved();
        }

        // Invoke event
        OnPuzzleSolved?.Invoke();
    }

    /// <summary>
    /// Public method to check if puzzle is solved
    /// </summary>
    public bool IsPuzzleSolved()
    {
        return isPuzzleSolved;
    }

    #endregion

    #region Reset & Helper Methods

    /// <summary>
    /// Resets the puzzle to initial state
    /// </summary>
    public void ResetPuzzle()
    {
        // TODO: STEP 13 - Reset the puzzle
        // HINT: Clear sequence progress, reset element states, reset visual elements

        if (!allowReset && isPuzzleSolved)
        {
            Debug.Log($"Puzzle {puzzleID} cannot be reset (allowReset is false)");
            return;
        }

        isPuzzleSolved = false;
        currentSequenceStep = 0;
        lastStepTime = 0f;
        activationHistory.Clear();

        // Reset element states
        foreach (var element in puzzleElements)
        {
            elementStates[element] = false;
            element.ResetElement();
        }

        // Reset reward objects
        foreach (var obj in objectsToActivate)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }

        foreach (var obj in objectsToDeactivate)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }

        OnPuzzleReset?.Invoke();

        Debug.Log($"Puzzle reset: {puzzleID}");
    }

    /// <summary>
    /// Resets just the sequence progress (not the entire puzzle)
    /// </summary>
    private void ResetSequence()
    {
        // TODO: STEP 14 - Reset sequence progress
        // HINT: Reset currentSequenceStep and activation history
        // Don't reset element states completely

        currentSequenceStep = 0;
        lastStepTime = 0f;
        activationHistory.Clear();

        Debug.Log($"Sequence reset: {puzzleID}");
    }

    /// <summary>
    /// Plays a sound effect
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        // TODO: STEP 15 - Play audio feedback
        // HINT: Use audioSource to play clip if both exist

        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, soundVolume);
        }
    }

    /// <summary>
    /// Gets current progress as a percentage
    /// </summary>
    public float GetProgressPercentage()
    {
        // TODO: STEP 16 - Calculate completion percentage
        // HINT: Return 0-100 based on how many elements are activated/solved

        if (puzzleElements.Count == 0) return 0f;

        int completedCount = elementStates.Values.Count(state => state);
        return ((float)completedCount / puzzleElements.Count) * 100f;
    }

    /// <summary>
    /// Provides a hint for the next step
    /// </summary>
    public string GetHint()
    {
        // TODO: STEP 17 - Provide helpful hints
        // HINT: Return different hints based on puzzle mode and current progress

        if (isPuzzleSolved)
        {
            return "Puzzle already solved!";
        }

        switch (puzzleMode)
        {
            case PuzzleMode.Sequential:
            case PuzzleMode.Timed:
                if (currentSequenceStep < puzzleElements.Count)
                {
                    return $"Try activating: {puzzleElements[currentSequenceStep].elementID}";
                }
                break;

            case PuzzleMode.Simultaneous:
                int activeCount = puzzleElements.Count(e => e.GetState() == PuzzleElement.ElementState.Active);
                return $"Need all elements active simultaneously. Currently active: {activeCount}/{puzzleElements.Count}";

            case PuzzleMode.Any:
                int completedCount = elementStates.Values.Count(state => state);
                return $"Activate all elements in any order. Progress: {completedCount}/{puzzleElements.Count}";

            case PuzzleMode.Pattern:
                return "Find the correct pattern or sequence.";
        }

        return "No hints available.";
    }

    #endregion

    #region Debug & Editor Helpers

    /// <summary>
    /// Manually solve the puzzle (for testing)
    /// </summary>
    [ContextMenu("Solve Puzzle (Debug)")]
    public void DebugSolvePuzzle()
    {
        if (!isPuzzleSolved)
        {
            SolvePuzzle();
            Debug.Log($"Puzzle force-solved: {puzzleID}");
        }
    }

    /// <summary>
    /// Display puzzle information in console
    /// </summary>
    [ContextMenu("Show Puzzle Info")]
    public void ShowPuzzleInfo()
    {
        string info = $"\n========== PUZZLE INFO ==========\n";
        info += $"Puzzle ID: {puzzleID}\n";
        info += $"Mode: {puzzleMode}\n";
        info += $"Elements: {puzzleElements.Count}\n";
        info += $"Solved: {isPuzzleSolved}\n";
        info += $"Progress: {GetProgressPercentage():F1}%\n";
        info += $"Current Step: {currentSequenceStep}/{puzzleElements.Count}\n";

        if (puzzleMode == PuzzleMode.Sequential || puzzleMode == PuzzleMode.Timed)
        {
            info += "\nSequence Order:\n";
            for (int i = 0; i < puzzleElements.Count; i++)
            {
                var element = puzzleElements[i];
                bool completed = i < currentSequenceStep;
                info += $"  {i + 1}. {element.elementID} {(completed ? "[✓]" : "[ ]")}\n";
            }
        }

        info += "================================\n";
        Debug.Log(info);
    }

    #endregion
}

/*
 * STUDENT EXERCISE SUGGESTIONS:
 *
 * 1. ADVANCED PUZZLE TYPES:
 *    - Implement musical note sequences
 *    - Add color-matching puzzles
 *    - Create physics-based puzzles
 *    - Implement riddle-based challenges
 *
 * 2. HINT SYSTEM:
 *    - Visual hints (highlighting next element)
 *    - Progressive hints (more obvious over time)
 *    - Penalty system for using hints
 *    - Audio cues for puzzle progress
 *
 * 3. DIFFICULTY SCALING:
 *    - Adjust timeout based on difficulty
 *    - Add more elements for harder versions
 *    - Implement random pattern generation
 *    - Create puzzle variations
 *
 * 4. ACCESSIBILITY:
 *    - Skip puzzle option (with penalty)
 *    - Adjustable difficulty settings
 *    - Visual and audio feedback options
 *    - Colorblind-friendly indicators
 *
 * 5. INTEGRATION:
 *    - Link to achievement system
 *    - Track solving time/attempts
 *    - Create puzzle leaderboards
 *    - Implement daily/weekly challenges
 */
