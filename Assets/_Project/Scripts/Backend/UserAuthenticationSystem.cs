using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Laboratory.Backend
{
    /// <summary>
    /// User authentication system for account management.
    /// Handles registration, login, session management, and social auth.
    /// Integrates with backend API for secure authentication.
    /// </summary>
    public class UserAuthenticationSystem : MonoBehaviour
    {
        #region Configuration

        [Header("Backend Settings")]
        [SerializeField] private string backendUrl = "https://api.projectchimera.com";
        [SerializeField] private string registerEndpoint = "/auth/register";
        [SerializeField] private string loginEndpoint = "/auth/login";
        [SerializeField] private string refreshEndpoint = "/auth/refresh";
        [SerializeField] private string logoutEndpoint = "/auth/logout";

        [Header("Session Settings")]
        [SerializeField] private bool persistSession = true;
        [SerializeField] private float sessionRefreshInterval = 3600f; // 1 hour
        [SerializeField] private bool autoRefreshToken = true;

        [Header("Security")]
        [SerializeField] private bool requireEmailVerification = false;
        [SerializeField] private int minPasswordLength = 8;
        [SerializeField] private bool requireStrongPassword = true;

        #endregion

        #region Private Fields

        private static UserAuthenticationSystem _instance;

        // Authentication state
        private bool _isAuthenticated = false;
        private UserSession _currentSession;
        private float _lastRefreshTime = 0f;

        // User data
        private UserProfile _currentUser;

        // Statistics
        private int _totalLogins = 0;
        private int _failedLogins = 0;
        private int _sessionRefreshes = 0;

        // Events
        public event Action<UserSession> OnLoginSuccess;
        public event Action<string> OnLoginFailed;
        public event Action<UserProfile> OnRegistrationSuccess;
        public event Action<string> OnRegistrationFailed;
        public event Action OnLogout;
        public event Action<UserSession> OnSessionRefreshed;

        #endregion

        #region Properties

        public static UserAuthenticationSystem Instance => _instance;
        public bool IsAuthenticated => _isAuthenticated;
        public UserProfile CurrentUser => _currentUser;
        public string UserId => _currentSession?.userId;
        public string AuthToken => _currentSession?.accessToken;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            if (!_isAuthenticated || !autoRefreshToken) return;

            // Auto-refresh token
            if (Time.time - _lastRefreshTime >= sessionRefreshInterval)
            {
                StartCoroutine(RefreshSession());
            }
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            Debug.Log("[UserAuthenticationSystem] Initializing...");

            // Try to restore previous session
            if (persistSession)
            {
                RestoreSession();
            }

            Debug.Log("[UserAuthenticationSystem] Initialized");
        }

        #endregion

        #region Registration

        /// <summary>
        /// Register a new user account.
        /// </summary>
        public void RegisterUser(string email, string username, string password, Action<UserProfile> onSuccess = null, Action<string> onError = null)
        {
            if (!ValidateEmail(email))
            {
                string error = "Invalid email format";
                OnRegistrationFailed?.Invoke(error);
                onError?.Invoke(error);
                return;
            }

            if (!ValidatePassword(password))
            {
                string error = $"Password must be at least {minPasswordLength} characters";
                OnRegistrationFailed?.Invoke(error);
                onError?.Invoke(error);
                return;
            }

            StartCoroutine(RegisterCoroutine(email, username, password, onSuccess, onError));
        }

        private IEnumerator RegisterCoroutine(string email, string username, string password, Action<UserProfile> onSuccess, Action<string> onError)
        {
            string url = backendUrl + registerEndpoint;

            var requestData = new RegistrationRequest
            {
                email = email,
                username = username,
                password = password
            };

            string json = JsonUtility.ToJson(requestData);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 10;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var response = JsonUtility.FromJson<RegistrationResponse>(request.downloadHandler.text);
                        _currentUser = response.user;

                        OnRegistrationSuccess?.Invoke(_currentUser);
                        onSuccess?.Invoke(_currentUser);

                        Debug.Log($"[UserAuthenticationSystem] Registration successful: {username}");

                        // Auto-login if token provided
                        if (!string.IsNullOrEmpty(response.accessToken))
                        {
                            CreateSession(response.userId, response.accessToken, response.refreshToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        string error = $"Failed to parse registration response: {ex.Message}";
                        OnRegistrationFailed?.Invoke(error);
                        onError?.Invoke(error);
                    }
                }
                else
                {
                    string error = $"Registration failed: {request.error}";
                    OnRegistrationFailed?.Invoke(error);
                    onError?.Invoke(error);
                    Debug.LogError($"[UserAuthenticationSystem] {error}");
                }
            }
        }

        #endregion

        #region Login

        /// <summary>
        /// Login with email and password.
        /// </summary>
        public void Login(string email, string password, Action<UserSession> onSuccess = null, Action<string> onError = null)
        {
            StartCoroutine(LoginCoroutine(email, password, onSuccess, onError));
        }

        private IEnumerator LoginCoroutine(string email, string password, Action<UserSession> onSuccess, Action<string> onError)
        {
            string url = backendUrl + loginEndpoint;

            var requestData = new LoginRequest
            {
                email = email,
                password = password
            };

            string json = JsonUtility.ToJson(requestData);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 10;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);

                        CreateSession(response.userId, response.accessToken, response.refreshToken);
                        _currentUser = response.user;

                        _totalLogins++;

                        OnLoginSuccess?.Invoke(_currentSession);
                        onSuccess?.Invoke(_currentSession);

                        Debug.Log($"[UserAuthenticationSystem] Login successful: {_currentUser.username}");

                        // Integrate with Cloud Save
                        if (CloudSaveSystem.Instance != null)
                        {
                            CloudSaveSystem.Instance.SetCredentials(response.userId, response.accessToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        _failedLogins++;
                        string error = $"Failed to parse login response: {ex.Message}";
                        OnLoginFailed?.Invoke(error);
                        onError?.Invoke(error);
                    }
                }
                else
                {
                    _failedLogins++;
                    string error = $"Login failed: {request.error}";
                    OnLoginFailed?.Invoke(error);
                    onError?.Invoke(error);
                    Debug.LogError($"[UserAuthenticationSystem] {error}");
                }
            }
        }

        /// <summary>
        /// Login with social provider (Google, Facebook, etc.).
        /// </summary>
        public void LoginWithSocial(string provider, string token, Action<UserSession> onSuccess = null, Action<string> onError = null)
        {
            // Placeholder for social auth
            Debug.Log($"[UserAuthenticationSystem] Social login: {provider}");
            // In production, implement OAuth flow for each provider
        }

        #endregion

        #region Logout

        /// <summary>
        /// Logout current user.
        /// </summary>
        public void Logout()
        {
            if (!_isAuthenticated)
            {
                Debug.LogWarning("[UserAuthenticationSystem] Not logged in");
                return;
            }

            StartCoroutine(LogoutCoroutine());
        }

        private IEnumerator LogoutCoroutine()
        {
            string url = backendUrl + logoutEndpoint;

            using (UnityWebRequest request = UnityWebRequest.Post(url, ""))
            {
                request.SetRequestHeader("Authorization", $"Bearer {_currentSession.accessToken}");
                request.timeout = 5;

                yield return request.SendWebRequest();

                // Don't wait for response - logout locally regardless
            }

            ClearSession();
            OnLogout?.Invoke();

            Debug.Log("[UserAuthenticationSystem] Logged out");
        }

        #endregion

        #region Session Management

        private void CreateSession(string userId, string accessToken, string refreshToken)
        {
            _currentSession = new UserSession
            {
                userId = userId,
                accessToken = accessToken,
                refreshToken = refreshToken,
                createdAt = DateTime.UtcNow,
                expiresAt = DateTime.UtcNow.AddSeconds(sessionRefreshInterval)
            };

            _isAuthenticated = true;
            _lastRefreshTime = Time.time;

            if (persistSession)
            {
                SaveSession();
            }
        }

        private IEnumerator RefreshSession()
        {
            if (_currentSession == null || string.IsNullOrEmpty(_currentSession.refreshToken))
            {
                Debug.LogWarning("[UserAuthenticationSystem] Cannot refresh: No session");
                yield break;
            }

            string url = backendUrl + refreshEndpoint;

            var requestData = new RefreshRequest
            {
                refreshToken = _currentSession.refreshToken
            };

            string json = JsonUtility.ToJson(requestData);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 10;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var response = JsonUtility.FromJson<RefreshResponse>(request.downloadHandler.text);

                        _currentSession.accessToken = response.accessToken;
                        _currentSession.expiresAt = DateTime.UtcNow.AddSeconds(sessionRefreshInterval);
                        _lastRefreshTime = Time.time;
                        _sessionRefreshes++;

                        if (persistSession)
                        {
                            SaveSession();
                        }

                        OnSessionRefreshed?.Invoke(_currentSession);

                        Debug.Log("[UserAuthenticationSystem] Session refreshed");

                        // Update Cloud Save credentials
                        if (CloudSaveSystem.Instance != null)
                        {
                            CloudSaveSystem.Instance.SetCredentials(_currentSession.userId, _currentSession.accessToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[UserAuthenticationSystem] Failed to refresh session: {ex.Message}");
                        ClearSession();
                    }
                }
                else
                {
                    Debug.LogError($"[UserAuthenticationSystem] Session refresh failed: {request.error}");
                    ClearSession();
                }
            }
        }

        private void SaveSession()
        {
            try
            {
                string json = JsonUtility.ToJson(_currentSession);
                PlayerPrefs.SetString("UserSession", json);
                PlayerPrefs.Save();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UserAuthenticationSystem] Failed to save session: {ex.Message}");
            }
        }

        private void RestoreSession()
        {
            try
            {
                if (PlayerPrefs.HasKey("UserSession"))
                {
                    string json = PlayerPrefs.GetString("UserSession");
                    _currentSession = JsonUtility.FromJson<UserSession>(json);

                    // Check if session expired
                    if (_currentSession.expiresAt > DateTime.UtcNow)
                    {
                        _isAuthenticated = true;
                        _lastRefreshTime = Time.time;

                        Debug.Log($"[UserAuthenticationSystem] Session restored: {_currentSession.userId}");

                        // Refresh token if close to expiry
                        if ((currentSession.expiresAt - DateTime.UtcNow).TotalSeconds < sessionRefreshInterval * 0.2f)
                        {
                            StartCoroutine(RefreshSession());
                        }

                        // Integrate with Cloud Save
                        if (CloudSaveSystem.Instance != null)
                        {
                            CloudSaveSystem.Instance.SetCredentials(_currentSession.userId, _currentSession.accessToken);
                        }
                    }
                    else
                    {
                        Debug.Log("[UserAuthenticationSystem] Stored session expired");
                        ClearSession();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UserAuthenticationSystem] Failed to restore session: {ex.Message}");
                ClearSession();
            }
        }

        private void ClearSession()
        {
            _currentSession = null;
            _isAuthenticated = false;
            _currentUser = null;

            PlayerPrefs.DeleteKey("UserSession");
            PlayerPrefs.Save();

            // Clear Cloud Save credentials
            if (CloudSaveSystem.Instance != null)
            {
                CloudSaveSystem.Instance.ClearCredentials();
            }
        }

        #endregion

        #region Validation

        private bool ValidateEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            return email.Contains("@") && email.Contains(".");
        }

        private bool ValidatePassword(string password)
        {
            if (string.IsNullOrEmpty(password))
                return false;

            if (password.Length < minPasswordLength)
                return false;

            if (requireStrongPassword)
            {
                // Check for uppercase, lowercase, digit
                bool hasUpper = false, hasLower = false, hasDigit = false;

                foreach (char c in password)
                {
                    if (char.IsUpper(c)) hasUpper = true;
                    if (char.IsLower(c)) hasLower = true;
                    if (char.IsDigit(c)) hasDigit = true;
                }

                return hasUpper && hasLower && hasDigit;
            }

            return true;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get authentication statistics.
        /// </summary>
        public AuthenticationStats GetStats()
        {
            return new AuthenticationStats
            {
                isAuthenticated = _isAuthenticated,
                userId = _currentSession?.userId,
                sessionAge = _currentSession != null ? (float)(DateTime.UtcNow - _currentSession.createdAt).TotalSeconds : 0f,
                totalLogins = _totalLogins,
                failedLogins = _failedLogins,
                sessionRefreshes = _sessionRefreshes
            };
        }

        #endregion

        #region Context Menu

        [ContextMenu("Logout")]
        private void LogoutMenu()
        {
            Logout();
        }

        [ContextMenu("Print Statistics")]
        private void PrintStatistics()
        {
            var stats = GetStats();
            Debug.Log($"=== Authentication Statistics ===\n" +
                      $"Authenticated: {stats.isAuthenticated}\n" +
                      $"User ID: {stats.userId}\n" +
                      $"Session Age: {stats.sessionAge:F0}s\n" +
                      $"Total Logins: {stats.totalLogins}\n" +
                      $"Failed Logins: {stats.failedLogins}\n" +
                      $"Session Refreshes: {stats.sessionRefreshes}");
        }

        #endregion
    }

    #region Data Structures

    [Serializable]
    public class UserSession
    {
        public string userId;
        public string accessToken;
        public string refreshToken;
        public DateTime createdAt;
        public DateTime expiresAt;
    }

    [Serializable]
    public class UserProfile
    {
        public string userId;
        public string email;
        public string username;
        public string displayName;
        public DateTime createdAt;
        public bool emailVerified;
    }

    [Serializable]
    public struct AuthenticationStats
    {
        public bool isAuthenticated;
        public string userId;
        public float sessionAge;
        public int totalLogins;
        public int failedLogins;
        public int sessionRefreshes;
    }

    // Request/Response structures
    [Serializable] class RegistrationRequest { public string email; public string username; public string password; }
    [Serializable] class RegistrationResponse { public UserProfile user; public string userId; public string accessToken; public string refreshToken; }
    [Serializable] class LoginRequest { public string email; public string password; }
    [Serializable] class LoginResponse { public UserProfile user; public string userId; public string accessToken; public string refreshToken; }
    [Serializable] class RefreshRequest { public string refreshToken; }
    [Serializable] class RefreshResponse { public string accessToken; }

    #endregion
}
