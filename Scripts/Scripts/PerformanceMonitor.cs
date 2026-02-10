using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro for better text rendering in VR

/// <summary>
/// Performance Monitor Script for VR Applications
/// Tracks and displays key performance metrics (FPS, Draw Calls, Triangles, etc.)
/// Essential for optimizing VR experiences on Meta Quest 3
///
/// USAGE:
/// 1. Attach this script to an empty GameObject in your scene
/// 2. Optionally assign a UI Text component for on-screen display
/// 3. Configure display settings in the Inspector
/// 4. Press the designated key (F1 by default) to toggle the display
///
/// Author: GDT-120 Course Materials
/// Date: 2026
/// </summary>
public class PerformanceMonitor : MonoBehaviour
{
    #region Inspector Variables

    [Header("Display Settings")]
    [Tooltip("UI Text element to display performance stats (optional)")]
    public TextMeshProUGUI displayText;

    [Tooltip("Show performance overlay in VR")]
    public bool showInVR = true;

    [Tooltip("Key to toggle performance display")]
    public KeyCode toggleKey = KeyCode.F1;

    [Header("Update Settings")]
    [Tooltip("How often to update performance metrics (in seconds)")]
    [Range(0.1f, 2.0f)]
    public float updateInterval = 0.5f;

    [Header("Warning Thresholds")]
    [Tooltip("FPS below this value will trigger a warning")]
    public int fpsWarningThreshold = 72; // Quest 3 target is 72 FPS

    [Tooltip("Draw calls above this value will trigger a warning")]
    public int drawCallWarningThreshold = 200;

    [Tooltip("Triangle count above this value will trigger a warning")]
    public int triangleWarningThreshold = 500000;

    #endregion

    #region Private Variables

    private float deltaTime = 0.0f;
    private float timeSinceLastUpdate = 0.0f;
    private bool displayEnabled = true;

    // Performance metrics
    private int currentFPS;
    private int drawCalls;
    private int triangles;
    private int vertices;
    private float memoryUsage;

    // Data collection for averaging
    private float fpsAccumulator = 0f;
    private int fpsFrameCount = 0;

    #endregion

    #region Unity Lifecycle Methods

    void Start()
    {
        // TODO: Initialize the performance monitor
        // STEP 1: If no UI Text is assigned, try to find one in children
        // HINT: Use GetComponentInChildren<TextMeshProUGUI>() if displayText is null

        if (displayText == null)
        {
            displayText = GetComponentInChildren<TextMeshProUGUI>();

            // If still not found, create a debug log
            if (displayText == null)
            {
                Debug.LogWarning("PerformanceMonitor: No TextMeshProUGUI assigned or found. Performance data will only be logged to console.");
            }
        }

        // STEP 2: Initialize the display state
        // HINT: Set displayEnabled based on showInVR setting
        displayEnabled = showInVR;

        // STEP 3: Position UI for VR viewing if using UI Text
        // TODO: If you want the UI to follow the camera in VR, you'll need to:
        // - Make this GameObject a child of the XR Origin Camera
        // - Position it appropriately in front of the camera (e.g., 2 units forward, 1 unit down)
        // - Use Canvas with World Space render mode for VR compatibility

        Debug.Log("PerformanceMonitor initialized. Press " + toggleKey + " to toggle display.");
    }

    void Update()
    {
        // Calculate delta time for FPS calculation
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

        // TODO: STEP 4 - Implement toggle functionality
        // HINT: Check if the toggle key was pressed this frame
        // Use Input.GetKeyDown(toggleKey) to detect key press
        // Toggle the displayEnabled boolean

        if (Input.GetKeyDown(toggleKey))
        {
            displayEnabled = !displayEnabled;
            Debug.Log("Performance Monitor: " + (displayEnabled ? "Enabled" : "Disabled"));
        }

        // TODO: STEP 5 - Update performance metrics at intervals
        // HINT: Increment timeSinceLastUpdate by Time.unscaledDeltaTime
        // When it exceeds updateInterval, call UpdatePerformanceMetrics() and reset timer

        timeSinceLastUpdate += Time.unscaledDeltaTime;

        if (timeSinceLastUpdate >= updateInterval)
        {
            UpdatePerformanceMetrics();
            timeSinceLastUpdate = 0f;
        }

        // Update display if enabled
        if (displayEnabled)
        {
            UpdateDisplay();
        }
        else if (displayText != null)
        {
            displayText.text = "";
        }
    }

    #endregion

    #region Performance Tracking Methods

    /// <summary>
    /// Updates all performance metrics
    /// Called at the interval specified by updateInterval
    /// </summary>
    private void UpdatePerformanceMetrics()
    {
        // TODO: STEP 6 - Calculate FPS
        // HINT: FPS = 1.0f / deltaTime
        // Cast to int for cleaner display

        currentFPS = Mathf.RoundToInt(1.0f / deltaTime);

        // TODO: STEP 7 - Get rendering statistics
        // Unity provides rendering stats through UnityEngine.Rendering namespace
        // For this basic version, we'll use UnityStats (requires UnityEditor namespace in Editor)
        // In builds, these will show approximate values

        #if UNITY_EDITOR
        // In editor, we can access detailed stats
        drawCalls = UnityEditor.UnityStats.drawCalls;
        triangles = UnityEditor.UnityStats.triangles;
        vertices = UnityEditor.UnityStats.vertices;
        #else
        // In build, estimate from rendered objects (simplified)
        // Students can expand this to get more accurate data
        drawCalls = EstimateDrawCalls();
        triangles = EstimateTriangles();
        vertices = EstimateVertices();
        #endif

        // TODO: STEP 8 - Calculate memory usage
        // HINT: Use System.GC.GetTotalMemory(false) and convert to MB
        // Divide by (1024 * 1024) to convert bytes to megabytes

        memoryUsage = System.GC.GetTotalMemory(false) / (1024f * 1024f);

        // TODO: STEP 9 - Log warnings for performance issues
        // HINT: Compare currentFPS with fpsWarningThreshold
        // Use Debug.LogWarning() for values below/above thresholds

        if (currentFPS < fpsWarningThreshold)
        {
            Debug.LogWarning($"Performance Warning: FPS ({currentFPS}) is below target ({fpsWarningThreshold})");
        }

        if (drawCalls > drawCallWarningThreshold)
        {
            Debug.LogWarning($"Performance Warning: Draw calls ({drawCalls}) exceed threshold ({drawCallWarningThreshold})");
        }

        if (triangles > triangleWarningThreshold)
        {
            Debug.LogWarning($"Performance Warning: Triangle count ({triangles}) exceeds threshold ({triangleWarningThreshold})");
        }
    }

    /// <summary>
    /// Updates the on-screen display with current performance metrics
    /// </summary>
    private void UpdateDisplay()
    {
        if (displayText == null) return;

        // TODO: STEP 10 - Format the display string
        // Create a formatted string showing all performance metrics
        // Use color tags for warnings: <color=red>text</color> for values exceeding thresholds
        // Use <color=green>text</color> for good values

        string fpsColor = currentFPS < fpsWarningThreshold ? "red" : "green";
        string drawCallColor = drawCalls > drawCallWarningThreshold ? "red" : "green";
        string triangleColor = triangles > triangleWarningThreshold ? "red" : "green";

        // Build the display string
        string displayString = "=== PERFORMANCE METRICS ===\n\n";
        displayString += $"<color={fpsColor}>FPS: {currentFPS}</color> (Target: {fpsWarningThreshold}+)\n";
        displayString += $"<color={drawCallColor}>Draw Calls: {drawCalls}</color> (Target: <{drawCallWarningThreshold})\n";
        displayString += $"<color={triangleColor}>Triangles: {triangles:N0}</color> (Target: <{triangleWarningThreshold:N0})\n";
        displayString += $"Vertices: {vertices:N0}\n";
        displayString += $"Memory: {memoryUsage:F1} MB\n";
        displayString += $"\nFrame Time: {deltaTime * 1000.0f:F1} ms\n";

        // TODO: STEP 11 - Add additional useful information
        // Consider adding: current scene name, time since start, etc.
        displayString += $"\nScene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}";

        displayText.text = displayString;
    }

    #endregion

    #region Helper Methods (For Build Version)

    /// <summary>
    /// Estimates draw calls by counting active renderers
    /// This is a simplified estimation for build versions
    /// </summary>
    private int EstimateDrawCalls()
    {
        // TODO: STEP 12 - Implement draw call estimation
        // HINT: Find all MeshRenderer components in the scene
        // Each enabled renderer typically = 1 draw call (simplified)
        // Use FindObjectsOfType<MeshRenderer>() but be aware this is expensive
        // Consider caching this data and only updating periodically

        MeshRenderer[] renderers = FindObjectsOfType<MeshRenderer>();
        int count = 0;
        foreach (var renderer in renderers)
        {
            if (renderer.enabled && renderer.gameObject.activeInHierarchy)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Estimates total triangles in the scene
    /// This is a simplified estimation for build versions
    /// </summary>
    private int EstimateTriangles()
    {
        // TODO: STEP 13 - Implement triangle count estimation
        // HINT: Get all MeshFilter components
        // Sum up the triangle count from each mesh
        // mesh.triangles.Length / 3 = triangle count

        MeshFilter[] meshFilters = FindObjectsOfType<MeshFilter>();
        int totalTriangles = 0;

        foreach (var meshFilter in meshFilters)
        {
            if (meshFilter.sharedMesh != null)
            {
                totalTriangles += meshFilter.sharedMesh.triangles.Length / 3;
            }
        }

        return totalTriangles;
    }

    /// <summary>
    /// Estimates total vertices in the scene
    /// This is a simplified estimation for build versions
    /// </summary>
    private int EstimateVertices()
    {
        // TODO: STEP 14 - Implement vertex count estimation
        // HINT: Similar to EstimateTriangles, but use mesh.vertexCount

        MeshFilter[] meshFilters = FindObjectsOfType<MeshFilter>();
        int totalVertices = 0;

        foreach (var meshFilter in meshFilters)
        {
            if (meshFilter.sharedMesh != null)
            {
                totalVertices += meshFilter.sharedMesh.vertexCount;
            }
        }

        return totalVertices;
    }

    #endregion

    #region Public API Methods

    /// <summary>
    /// Public method to get current FPS
    /// Can be called by other scripts for game logic
    /// </summary>
    public int GetCurrentFPS()
    {
        return currentFPS;
    }

    /// <summary>
    /// Public method to check if performance is acceptable
    /// Returns true if all metrics are within acceptable ranges
    /// </summary>
    public bool IsPerformanceAcceptable()
    {
        // TODO: STEP 15 - Implement performance check
        // HINT: Return true only if ALL metrics are within thresholds
        // FPS should be >= threshold, draw calls and triangles should be <= threshold

        return currentFPS >= fpsWarningThreshold &&
               drawCalls <= drawCallWarningThreshold &&
               triangles <= triangleWarningThreshold;
    }

    /// <summary>
    /// Logs current performance metrics to console
    /// Useful for creating performance reports
    /// </summary>
    public void LogPerformanceReport()
    {
        // TODO: STEP 16 - Create a detailed performance report
        // HINT: Use Debug.Log to output a formatted report
        // Include timestamp, all metrics, and warnings

        string report = "\n========== PERFORMANCE REPORT ==========\n";
        report += $"Time: {System.DateTime.Now}\n";
        report += $"Scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}\n";
        report += $"FPS: {currentFPS} (Target: {fpsWarningThreshold}+)\n";
        report += $"Draw Calls: {drawCalls} (Target: <{drawCallWarningThreshold})\n";
        report += $"Triangles: {triangles:N0} (Target: <{triangleWarningThreshold:N0})\n";
        report += $"Vertices: {vertices:N0}\n";
        report += $"Memory Usage: {memoryUsage:F2} MB\n";
        report += $"Frame Time: {deltaTime * 1000.0f:F2} ms\n";
        report += $"Performance Status: {(IsPerformanceAcceptable() ? "ACCEPTABLE" : "NEEDS OPTIMIZATION")}\n";
        report += "=====================================\n";

        Debug.Log(report);
    }

    #endregion
}

/*
 * STUDENT EXERCISE SUGGESTIONS:
 *
 * 1. ADVANCED TRACKING:
 *    - Add tracking for specific object types (trees, buildings, characters)
 *    - Implement a graph/chart to show FPS over time
 *    - Track battery usage on Quest 3
 *
 * 2. OPTIMIZATION SUGGESTIONS:
 *    - Create an auto-optimizer that adjusts quality settings based on FPS
 *    - Implement dynamic LOD distance adjustment
 *    - Add warnings for specific problematic objects
 *
 * 3. DATA EXPORT:
 *    - Save performance data to a CSV file for analysis
 *    - Create before/after comparison tools
 *    - Generate performance reports automatically
 *
 * 4. VR-SPECIFIC FEATURES:
 *    - Track motion-to-photon latency
 *    - Monitor reprojection rate
 *    - Display performance metrics in world space UI
 *
 * 5. PROFILING INTEGRATION:
 *    - Add buttons to start/stop Unity Profiler
 *    - Create markers for specific code sections
 *    - Implement custom profiling scopes
 */
