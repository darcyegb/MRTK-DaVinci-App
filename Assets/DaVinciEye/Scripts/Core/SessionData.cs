using UnityEngine;

namespace DaVinciEye.Core
{
    /// <summary>
    /// Manages session-specific data and state persistence
    /// </summary>
    [System.Serializable]
    public class SessionData
    {
        [Header("Image State")]
        public string currentImagePath = "";
        public float currentOpacity = 1.0f;
        public bool isImageVisible = false;
        
        [Header("Canvas State")]
        public bool isCanvasDefined = false;
        public string canvasAnchorId = "";
        
        [Header("Session Info")]
        public System.DateTime sessionStartTime;
        public int sessionId;
        
        /// <summary>
        /// Resets all session data to default values
        /// </summary>
        public void Reset()
        {
            currentImagePath = "";
            currentOpacity = 1.0f;
            isImageVisible = false;
            isCanvasDefined = false;
            canvasAnchorId = "";
            sessionStartTime = System.DateTime.Now;
            sessionId = Random.Range(1000, 9999);
        }
        
        /// <summary>
        /// Saves session data to PlayerPrefs
        /// </summary>
        public void Save()
        {
            string json = JsonUtility.ToJson(this);
            PlayerPrefs.SetString("DaVinciEye_SessionData", json);
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Loads session data from PlayerPrefs
        /// </summary>
        public static SessionData Load()
        {
            string json = PlayerPrefs.GetString("DaVinciEye_SessionData", "");
            
            if (string.IsNullOrEmpty(json))
            {
                var newSession = new SessionData();
                newSession.Reset();
                return newSession;
            }
            
            try
            {
                return JsonUtility.FromJson<SessionData>(json);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"SessionData: Failed to load session data: {ex.Message}");
                var newSession = new SessionData();
                newSession.Reset();
                return newSession;
            }
        }
    }
}