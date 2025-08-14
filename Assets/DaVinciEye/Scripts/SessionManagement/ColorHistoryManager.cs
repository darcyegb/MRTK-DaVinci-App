using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace DaVinciEye.SessionManagement
{
    /// <summary>
    /// Manages color match history with session-based storage and persistence
    /// Provides comprehensive color matching data management for artists
    /// </summary>
    public class ColorHistoryManager : MonoBehaviour
    {
        [Header("History Configuration")]
        [SerializeField] private int maxHistorySize = 500;
        [SerializeField] private bool autoSaveEnabled = true;
        [SerializeField] private float autoSaveInterval = 30f; // seconds
        [SerializeField] private bool enableSessionGrouping = true;
        
        [Header("Storage Settings")]
        [SerializeField] private string historyFileName = "color_match_history.json";
        [SerializeField] private bool useCompression = true;
        [SerializeField] private bool createBackups = true;
        
        // Properties
        public List<ColorMatchData> CurrentSessionHistory { get; private set; } = new List<ColorMatchData>();
        public List<ColorMatchData> AllTimeHistory { get; private set; } = new List<ColorMatchData>();
        public ColorMatchSession CurrentSession { get; private set; }
        public int TotalMatches => AllTimeHistory.Count;
        public int SessionMatches => CurrentSessionHistory.Count;
        
        // Events
        public event Action<ColorMatchData> OnColorMatchAdded;
        public event Action<ColorMatchSession> OnSessionStarted;
        public event Action<ColorMatchSession> OnSessionEnded;
        public event Action<List<ColorMatchData>> OnHistoryLoaded;
        public event Action OnHistoryCleared;
        public event Action<string> OnHistoryError;
        
        // Private fields
        private string historyFilePath;
        private float lastAutoSaveTime;
        private bool isInitialized = false;
        
        private void Awake()
        {
            InitializeHistoryManager();
        }
        
        private void Start()
        {
            StartNewSession();
            LoadHistoryFromDisk();
        }
        
        private void Update()
        {
            if (autoSaveEnabled && Time.time - lastAutoSaveTime > autoSaveInterval)
            {
                SaveHistoryToDisk();
                lastAutoSaveTime = Time.time;
            }
        }
        
        private void InitializeHistoryManager()
        {
            // Set up file path
            historyFilePath = Path.Combine(Application.persistentDataPath, historyFileName);
            
            // Initialize collections
            CurrentSessionHistory = new List<ColorMatchData>();
            AllTimeHistory = new List<ColorMatchData>();
            
            lastAutoSaveTime = Time.time;
            isInitialized = true;
            
            Debug.Log($"ColorHistoryManager: Initialized with file path: {historyFilePath}");
        }
        
        /// <summary>
        /// Start a new color matching session
        /// </summary>
        public void StartNewSession()
        {
            if (CurrentSession != null)
            {
                EndCurrentSession();
            }
            
            CurrentSession = new ColorMatchSession
            {
                sessionId = Guid.NewGuid().ToString(),
                startTime = DateTime.Now,
                matches = new List<ColorMatchData>()
            };
            
            CurrentSessionHistory.Clear();
            
            OnSessionStarted?.Invoke(CurrentSession);
            Debug.Log($"ColorHistoryManager: New session started - ID: {CurrentSession.sessionId}");
        }
        
        /// <summary>
        /// End the current session
        /// </summary>
        public void EndCurrentSession()
        {
            if (CurrentSession == null) return;
            
            CurrentSession.endTime = DateTime.Now;
            CurrentSession.matches = new List<ColorMatchData>(CurrentSessionHistory);
            
            OnSessionEnded?.Invoke(CurrentSession);
            
            // Save session data
            SaveHistoryToDisk();
            
            Debug.Log($"ColorHistoryManager: Session ended - ID: {CurrentSession.sessionId}, Matches: {CurrentSessionHistory.Count}");
            CurrentSession = null;
        }
        
        /// <summary>
        /// Add a color match to the current session history
        /// </summary>
        public void AddColorMatch(ColorMatchData matchData)
        {
            if (!isInitialized)
            {
                Debug.LogError("ColorHistoryManager: Not initialized");
                return;
            }
            
            if (matchData == null)
            {
                Debug.LogWarning("ColorHistoryManager: Attempted to add null match data");
                return;
            }
            
            // Set session information
            if (CurrentSession != null)
            {
                matchData.sessionId = CurrentSession.sessionId;
            }
            
            // Ensure timestamp is set
            if (matchData.timestamp == default(DateTime))
            {
                matchData.timestamp = DateTime.Now;
            }
            
            // Add to current session
            CurrentSessionHistory.Add(matchData);
            
            // Add to all-time history
            AllTimeHistory.Add(matchData);
            
            // Maintain history size limit
            if (AllTimeHistory.Count > maxHistorySize)
            {
                AllTimeHistory.RemoveAt(0);
            }
            
            OnColorMatchAdded?.Invoke(matchData);
            
            Debug.Log($"ColorHistoryManager: Color match added - Accuracy: {matchData.matchAccuracy:F2}, Total: {AllTimeHistory.Count}");
        }
        
        /// <summary>
        /// Get color matches from current session
        /// </summary>
        public List<ColorMatchData> GetCurrentSessionMatches()
        {
            return new List<ColorMatchData>(CurrentSessionHistory);
        }
        
        /// <summary>
        /// Get all color matches from history
        /// </summary>
        public List<ColorMatchData> GetAllMatches()
        {
            return new List<ColorMatchData>(AllTimeHistory);
        }
        
        /// <summary>
        /// Get color matches filtered by criteria
        /// </summary>
        public List<ColorMatchData> GetMatchesFiltered(ColorMatchFilter filter)
        {
            var matches = AllTimeHistory.AsEnumerable();
            
            // Filter by date range
            if (filter.startDate.HasValue)
            {
                matches = matches.Where(m => m.timestamp >= filter.startDate.Value);
            }
            
            if (filter.endDate.HasValue)
            {
                matches = matches.Where(m => m.timestamp <= filter.endDate.Value);
            }
            
            // Filter by accuracy range
            if (filter.minAccuracy.HasValue)
            {
                matches = matches.Where(m => m.matchAccuracy >= filter.minAccuracy.Value);
            }
            
            if (filter.maxAccuracy.HasValue)
            {
                matches = matches.Where(m => m.matchAccuracy <= filter.maxAccuracy.Value);
            }
            
            // Filter by session
            if (!string.IsNullOrEmpty(filter.sessionId))
            {
                matches = matches.Where(m => m.sessionId == filter.sessionId);
            }
            
            // Filter by color similarity
            if (filter.referenceColor.HasValue)
            {
                Color refColor = filter.referenceColor.Value;
                float tolerance = filter.colorTolerance ?? 0.1f;
                
                matches = matches.Where(m => 
                    Vector3.Distance(
                        new Vector3(m.referenceColor.r, m.referenceColor.g, m.referenceColor.b),
                        new Vector3(refColor.r, refColor.g, refColor.b)
                    ) <= tolerance
                );
            }
            
            return matches.ToList();
        }
        
        /// <summary>
        /// Get color match statistics
        /// </summary>
        public ColorMatchStatistics GetStatistics()
        {
            if (AllTimeHistory.Count == 0)
            {
                return new ColorMatchStatistics();
            }
            
            var stats = new ColorMatchStatistics
            {
                totalMatches = AllTimeHistory.Count,
                sessionMatches = CurrentSessionHistory.Count,
                averageAccuracy = AllTimeHistory.Average(m => m.matchAccuracy),
                bestMatch = AllTimeHistory.OrderByDescending(m => m.matchAccuracy).FirstOrDefault(),
                worstMatch = AllTimeHistory.OrderBy(m => m.matchAccuracy).FirstOrDefault(),
                totalSessions = GetUniqueSessions().Count
            };
            
            // Calculate accuracy distribution
            stats.excellentMatches = AllTimeHistory.Count(m => m.matchAccuracy >= 0.9f);
            stats.goodMatches = AllTimeHistory.Count(m => m.matchAccuracy >= 0.7f && m.matchAccuracy < 0.9f);
            stats.fairMatches = AllTimeHistory.Count(m => m.matchAccuracy >= 0.5f && m.matchAccuracy < 0.7f);
            stats.poorMatches = AllTimeHistory.Count(m => m.matchAccuracy < 0.5f);
            
            // Calculate recent activity
            DateTime weekAgo = DateTime.Now.AddDays(-7);
            stats.recentMatches = AllTimeHistory.Count(m => m.timestamp >= weekAgo);
            
            return stats;
        }
        
        /// <summary>
        /// Get unique sessions from history
        /// </summary>
        public List<string> GetUniqueSessions()
        {
            return AllTimeHistory
                .Where(m => !string.IsNullOrEmpty(m.sessionId))
                .Select(m => m.sessionId)
                .Distinct()
                .ToList();
        }
        
        /// <summary>
        /// Clear all color match history
        /// </summary>
        public void ClearHistory()
        {
            CurrentSessionHistory.Clear();
            AllTimeHistory.Clear();
            
            // Delete file
            if (File.Exists(historyFilePath))
            {
                File.Delete(historyFilePath);
            }
            
            OnHistoryCleared?.Invoke();
            Debug.Log("ColorHistoryManager: History cleared");
        }
        
        /// <summary>
        /// Clear current session history only
        /// </summary>
        public void ClearCurrentSession()
        {
            CurrentSessionHistory.Clear();
            
            if (CurrentSession != null)
            {
                CurrentSession.matches.Clear();
            }
            
            Debug.Log("ColorHistoryManager: Current session cleared");
        }
        
        /// <summary>
        /// Export history to JSON string
        /// </summary>
        public string ExportHistoryToJson()
        {
            try
            {
                var exportData = new ColorHistoryExportData
                {
                    exportDate = DateTime.Now,
                    totalMatches = AllTimeHistory.Count,
                    matches = AllTimeHistory.ToArray()
                };
                
                return JsonUtility.ToJson(exportData, true);
            }
            catch (Exception e)
            {
                OnHistoryError?.Invoke($"Export failed: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Import history from JSON string
        /// </summary>
        public bool ImportHistoryFromJson(string jsonData, bool mergeWithExisting = true)
        {
            try
            {
                var importData = JsonUtility.FromJson<ColorHistoryExportData>(jsonData);
                
                if (!mergeWithExisting)
                {
                    AllTimeHistory.Clear();
                }
                
                foreach (var match in importData.matches)
                {
                    if (!AllTimeHistory.Any(m => m.timestamp == match.timestamp && 
                                                Vector3.Distance(new Vector3(m.referenceColor.r, m.referenceColor.g, m.referenceColor.b),
                                                               new Vector3(match.referenceColor.r, match.referenceColor.g, match.referenceColor.b)) < 0.01f))
                    {
                        AllTimeHistory.Add(match);
                    }
                }
                
                // Maintain size limit
                if (AllTimeHistory.Count > maxHistorySize)
                {
                    AllTimeHistory = AllTimeHistory.OrderByDescending(m => m.timestamp).Take(maxHistorySize).ToList();
                }
                
                SaveHistoryToDisk();
                OnHistoryLoaded?.Invoke(AllTimeHistory);
                
                Debug.Log($"ColorHistoryManager: Imported {importData.matches.Length} matches");
                return true;
            }
            catch (Exception e)
            {
                OnHistoryError?.Invoke($"Import failed: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Save history to disk
        /// </summary>
        public void SaveHistoryToDisk()
        {
            try
            {
                var historyData = new ColorHistoryData
                {
                    version = "1.0",
                    saveDate = DateTime.Now,
                    matches = AllTimeHistory.ToArray()
                };
                
                string json = JsonUtility.ToJson(historyData, true);
                
                // Create backup if enabled
                if (createBackups && File.Exists(historyFilePath))
                {
                    string backupPath = historyFilePath + ".backup";
                    File.Copy(historyFilePath, backupPath, true);
                }
                
                File.WriteAllText(historyFilePath, json);
                
                Debug.Log($"ColorHistoryManager: History saved - {AllTimeHistory.Count} matches");
            }
            catch (Exception e)
            {
                OnHistoryError?.Invoke($"Save failed: {e.Message}");
                Debug.LogError($"ColorHistoryManager: Save failed - {e.Message}");
            }
        }
        
        /// <summary>
        /// Load history from disk
        /// </summary>
        public void LoadHistoryFromDisk()
        {
            try
            {
                if (!File.Exists(historyFilePath))
                {
                    Debug.Log("ColorHistoryManager: No existing history file found");
                    return;
                }
                
                string json = File.ReadAllText(historyFilePath);
                var historyData = JsonUtility.FromJson<ColorHistoryData>(json);
                
                AllTimeHistory = new List<ColorMatchData>(historyData.matches);
                
                OnHistoryLoaded?.Invoke(AllTimeHistory);
                
                Debug.Log($"ColorHistoryManager: History loaded - {AllTimeHistory.Count} matches");
            }
            catch (Exception e)
            {
                OnHistoryError?.Invoke($"Load failed: {e.Message}");
                Debug.LogError($"ColorHistoryManager: Load failed - {e.Message}");
                
                // Try to load backup
                LoadBackupHistory();
            }
        }
        
        private void LoadBackupHistory()
        {
            try
            {
                string backupPath = historyFilePath + ".backup";
                if (File.Exists(backupPath))
                {
                    string json = File.ReadAllText(backupPath);
                    var historyData = JsonUtility.FromJson<ColorHistoryData>(json);
                    
                    AllTimeHistory = new List<ColorMatchData>(historyData.matches);
                    
                    Debug.Log($"ColorHistoryManager: Backup history loaded - {AllTimeHistory.Count} matches");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"ColorHistoryManager: Backup load failed - {e.Message}");
                AllTimeHistory = new List<ColorMatchData>();
            }
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveHistoryToDisk();
            }
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                SaveHistoryToDisk();
            }
        }
        
        private void OnDestroy()
        {
            EndCurrentSession();
            SaveHistoryToDisk();
        }
    }
}