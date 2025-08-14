using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace DaVinciEye.SessionManagement
{
    /// <summary>
    /// Manages session data including image adjustments, filter settings, and app state persistence
    /// Provides session restoration functionality for app resume scenarios
    /// </summary>
    public class SessionDataManager : MonoBehaviour
    {
        [Header("Session Configuration")]
        [SerializeField] private bool autoSaveEnabled = true;
        [SerializeField] private float autoSaveInterval = 15f; // seconds
        [SerializeField] private bool enableSessionRestore = true;
        [SerializeField] private int maxSessionHistory = 10;
        
        [Header("Storage Settings")]
        [SerializeField] private string sessionFileName = "session_data.json";
        [SerializeField] private bool createBackups = true;
        [SerializeField] private bool compressData = false;
        
        // Properties
        public SessionData CurrentSession { get; private set; }
        public bool HasActiveSession => CurrentSession != null;
        public bool IsSessionDirty { get; private set; }
        public DateTime LastSaveTime { get; private set; }
        
        // Events
        public event Action<SessionData> OnSessionCreated;
        public event Action<SessionData> OnSessionLoaded;
        public event Action<SessionData> OnSessionSaved;
        public event Action OnSessionCleared;
        public event Action<string> OnSessionError;
        
        // Private fields
        private string sessionFilePath;
        private float lastAutoSaveTime;
        private bool isInitialized = false;
        
        private void Awake()
        {
            InitializeSessionManager();
        }
        
        private void Start()
        {
            if (enableSessionRestore)
            {
                LoadLastSession();
            }
            else
            {
                CreateNewSession();
            }
        }
        
        private void Update()
        {
            if (autoSaveEnabled && IsSessionDirty && Time.time - lastAutoSaveTime > autoSaveInterval)
            {
                SaveCurrentSession();
                lastAutoSaveTime = Time.time;
            }
        }
        
        private void InitializeSessionManager()
        {
            // Set up file path
            sessionFilePath = Path.Combine(Application.persistentDataPath, sessionFileName);
            
            lastAutoSaveTime = Time.time;
            isInitialized = true;
            
            Debug.Log($"SessionDataManager: Initialized with file path: {sessionFilePath}");
        }
        
        /// <summary>
        /// Create a new session with default settings
        /// </summary>
        public void CreateNewSession()
        {
            CurrentSession = new SessionData
            {
                sessionId = Guid.NewGuid().ToString(),
                createdAt = DateTime.Now,
                lastModified = DateTime.Now,
                imageAdjustments = new ImageAdjustments(),
                filterSettings = new FilterSettings(),
                canvasData = null,
                currentImagePath = "",
                appState = new AppState()
            };
            
            IsSessionDirty = true;
            OnSessionCreated?.Invoke(CurrentSession);
            
            Debug.Log($"SessionDataManager: New session created - ID: {CurrentSession.sessionId}");
        }
        
        /// <summary>
        /// Load the last saved session
        /// </summary>
        public bool LoadLastSession()
        {
            try
            {
                if (!File.Exists(sessionFilePath))
                {
                    Debug.Log("SessionDataManager: No existing session file found, creating new session");
                    CreateNewSession();
                    return false;
                }
                
                string json = File.ReadAllText(sessionFilePath);
                CurrentSession = JsonUtility.FromJson<SessionData>(json);
                
                if (CurrentSession == null)
                {
                    Debug.LogWarning("SessionDataManager: Failed to parse session data, creating new session");
                    CreateNewSession();
                    return false;
                }
                
                // Validate session data
                ValidateSessionData();
                
                IsSessionDirty = false;
                LastSaveTime = DateTime.Now;
                OnSessionLoaded?.Invoke(CurrentSession);
                
                Debug.Log($"SessionDataManager: Session loaded - ID: {CurrentSession.sessionId}");
                return true;
            }
            catch (Exception e)
            {
                OnSessionError?.Invoke($"Failed to load session: {e.Message}");
                Debug.LogError($"SessionDataManager: Load failed - {e.Message}");
                
                // Try to load backup
                if (LoadBackupSession())
                {
                    return true;
                }
                
                // Create new session as fallback
                CreateNewSession();
                return false;
            }
        }
        
        /// <summary>
        /// Save the current session to disk
        /// </summary>
        public bool SaveCurrentSession()
        {
            if (CurrentSession == null)
            {
                Debug.LogWarning("SessionDataManager: No active session to save");
                return false;
            }
            
            try
            {
                CurrentSession.lastModified = DateTime.Now;
                
                string json = JsonUtility.ToJson(CurrentSession, true);
                
                // Create backup if enabled
                if (createBackups && File.Exists(sessionFilePath))
                {
                    string backupPath = sessionFilePath + ".backup";
                    File.Copy(sessionFilePath, backupPath, true);
                }
                
                File.WriteAllText(sessionFilePath, json);
                
                IsSessionDirty = false;
                LastSaveTime = DateTime.Now;
                OnSessionSaved?.Invoke(CurrentSession);
                
                Debug.Log($"SessionDataManager: Session saved - ID: {CurrentSession.sessionId}");
                return true;
            }
            catch (Exception e)
            {
                OnSessionError?.Invoke($"Failed to save session: {e.Message}");
                Debug.LogError($"SessionDataManager: Save failed - {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Update image adjustments in the current session
        /// </summary>
        public void UpdateImageAdjustments(ImageAdjustments adjustments)
        {
            if (CurrentSession == null)
            {
                Debug.LogWarning("SessionDataManager: No active session for image adjustments update");
                return;
            }
            
            CurrentSession.imageAdjustments = adjustments;
            CurrentSession.lastModified = DateTime.Now;
            IsSessionDirty = true;
            
            Debug.Log("SessionDataManager: Image adjustments updated");
        }
        
        /// <summary>
        /// Update filter settings in the current session
        /// </summary>
        public void UpdateFilterSettings(FilterSettings filterSettings)
        {
            if (CurrentSession == null)
            {
                Debug.LogWarning("SessionDataManager: No active session for filter settings update");
                return;
            }
            
            CurrentSession.filterSettings = filterSettings;
            CurrentSession.lastModified = DateTime.Now;
            IsSessionDirty = true;
            
            Debug.Log("SessionDataManager: Filter settings updated");
        }
        
        /// <summary>
        /// Update canvas data in the current session
        /// </summary>
        public void UpdateCanvasData(CanvasData canvasData)
        {
            if (CurrentSession == null)
            {
                Debug.LogWarning("SessionDataManager: No active session for canvas data update");
                return;
            }
            
            CurrentSession.canvasData = canvasData;
            CurrentSession.lastModified = DateTime.Now;
            IsSessionDirty = true;
            
            Debug.Log("SessionDataManager: Canvas data updated");
        }
        
        /// <summary>
        /// Update current image path in the session
        /// </summary>
        public void UpdateCurrentImagePath(string imagePath)
        {
            if (CurrentSession == null)
            {
                Debug.LogWarning("SessionDataManager: No active session for image path update");
                return;
            }
            
            CurrentSession.currentImagePath = imagePath;
            CurrentSession.lastModified = DateTime.Now;
            IsSessionDirty = true;
            
            Debug.Log($"SessionDataManager: Current image path updated - {imagePath}");
        }
        
        /// <summary>
        /// Update app state in the current session
        /// </summary>
        public void UpdateAppState(AppState appState)
        {
            if (CurrentSession == null)
            {
                Debug.LogWarning("SessionDataManager: No active session for app state update");
                return;
            }
            
            CurrentSession.appState = appState;
            CurrentSession.lastModified = DateTime.Now;
            IsSessionDirty = true;
            
            Debug.Log("SessionDataManager: App state updated");
        }
        
        /// <summary>
        /// Get current image adjustments
        /// </summary>
        public ImageAdjustments GetImageAdjustments()
        {
            return CurrentSession?.imageAdjustments ?? new ImageAdjustments();
        }
        
        /// <summary>
        /// Get current filter settings
        /// </summary>
        public FilterSettings GetFilterSettings()
        {
            return CurrentSession?.filterSettings ?? new FilterSettings();
        }
        
        /// <summary>
        /// Get current canvas data
        /// </summary>
        public CanvasData GetCanvasData()
        {
            return CurrentSession?.canvasData;
        }
        
        /// <summary>
        /// Get current image path
        /// </summary>
        public string GetCurrentImagePath()
        {
            return CurrentSession?.currentImagePath ?? "";
        }
        
        /// <summary>
        /// Get current app state
        /// </summary>
        public AppState GetAppState()
        {
            return CurrentSession?.appState ?? new AppState();
        }
        
        /// <summary>
        /// Clear the current session
        /// </summary>
        public void ClearCurrentSession()
        {
            CurrentSession = null;
            IsSessionDirty = false;
            
            // Delete session file
            if (File.Exists(sessionFilePath))
            {
                File.Delete(sessionFilePath);
            }
            
            OnSessionCleared?.Invoke();
            Debug.Log("SessionDataManager: Current session cleared");
        }
        
        /// <summary>
        /// Reset session to default state
        /// </summary>
        public void ResetSessionToDefaults()
        {
            if (CurrentSession == null)
            {
                CreateNewSession();
                return;
            }
            
            CurrentSession.imageAdjustments = new ImageAdjustments();
            CurrentSession.filterSettings = new FilterSettings();
            CurrentSession.currentImagePath = "";
            CurrentSession.appState = new AppState();
            CurrentSession.lastModified = DateTime.Now;
            
            IsSessionDirty = true;
            
            Debug.Log("SessionDataManager: Session reset to defaults");
        }
        
        /// <summary>
        /// Export session data to JSON string
        /// </summary>
        public string ExportSessionToJson()
        {
            if (CurrentSession == null)
            {
                Debug.LogWarning("SessionDataManager: No active session to export");
                return null;
            }
            
            try
            {
                return JsonUtility.ToJson(CurrentSession, true);
            }
            catch (Exception e)
            {
                OnSessionError?.Invoke($"Export failed: {e.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Import session data from JSON string
        /// </summary>
        public bool ImportSessionFromJson(string jsonData)
        {
            try
            {
                var importedSession = JsonUtility.FromJson<SessionData>(jsonData);
                
                if (importedSession == null)
                {
                    OnSessionError?.Invoke("Invalid session data format");
                    return false;
                }
                
                // Validate imported data
                ValidateSessionData(importedSession);
                
                CurrentSession = importedSession;
                CurrentSession.lastModified = DateTime.Now;
                IsSessionDirty = true;
                
                OnSessionLoaded?.Invoke(CurrentSession);
                
                Debug.Log($"SessionDataManager: Session imported - ID: {CurrentSession.sessionId}");
                return true;
            }
            catch (Exception e)
            {
                OnSessionError?.Invoke($"Import failed: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Get session summary information
        /// </summary>
        public SessionSummary GetSessionSummary()
        {
            if (CurrentSession == null)
            {
                return new SessionSummary();
            }
            
            return new SessionSummary
            {
                sessionId = CurrentSession.sessionId,
                createdAt = CurrentSession.createdAt,
                lastModified = CurrentSession.lastModified,
                hasCanvasData = CurrentSession.canvasData != null,
                hasImageLoaded = !string.IsNullOrEmpty(CurrentSession.currentImagePath),
                hasImageAdjustments = CurrentSession.imageAdjustments.isModified,
                activeFilterCount = CurrentSession.filterSettings.GetActiveFilterCount(),
                currentMode = CurrentSession.appState.currentMode
            };
        }
        
        /// <summary>
        /// Force save current session (ignores auto-save interval)
        /// </summary>
        public void ForceSave()
        {
            SaveCurrentSession();
        }
        
        /// <summary>
        /// Check if session data is valid and fix any issues
        /// </summary>
        private void ValidateSessionData(SessionData session = null)
        {
            var sessionToValidate = session ?? CurrentSession;
            
            if (sessionToValidate == null) return;
            
            // Ensure required fields are not null
            if (sessionToValidate.imageAdjustments == null)
                sessionToValidate.imageAdjustments = new ImageAdjustments();
            
            if (sessionToValidate.filterSettings == null)
                sessionToValidate.filterSettings = new FilterSettings();
            
            if (sessionToValidate.appState == null)
                sessionToValidate.appState = new AppState();
            
            if (string.IsNullOrEmpty(sessionToValidate.sessionId))
                sessionToValidate.sessionId = Guid.NewGuid().ToString();
            
            if (sessionToValidate.createdAt == default(DateTime))
                sessionToValidate.createdAt = DateTime.Now;
            
            // Validate image path exists
            if (!string.IsNullOrEmpty(sessionToValidate.currentImagePath) && 
                !File.Exists(sessionToValidate.currentImagePath))
            {
                Debug.LogWarning($"SessionDataManager: Image file not found: {sessionToValidate.currentImagePath}");
                sessionToValidate.currentImagePath = "";
            }
        }
        
        /// <summary>
        /// Load backup session if main session fails
        /// </summary>
        private bool LoadBackupSession()
        {
            try
            {
                string backupPath = sessionFilePath + ".backup";
                if (!File.Exists(backupPath))
                {
                    return false;
                }
                
                string json = File.ReadAllText(backupPath);
                CurrentSession = JsonUtility.FromJson<SessionData>(json);
                
                if (CurrentSession == null)
                {
                    return false;
                }
                
                ValidateSessionData();
                
                IsSessionDirty = false;
                LastSaveTime = DateTime.Now;
                OnSessionLoaded?.Invoke(CurrentSession);
                
                Debug.Log($"SessionDataManager: Backup session loaded - ID: {CurrentSession.sessionId}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"SessionDataManager: Backup load failed - {e.Message}");
                return false;
            }
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                SaveCurrentSession();
            }
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                SaveCurrentSession();
            }
        }
        
        private void OnDestroy()
        {
            SaveCurrentSession();
        }
    }
}