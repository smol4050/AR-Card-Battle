using Niantic.Lightship.AR.Loader;
using Niantic.Lightship.AR.Utilities.Auth;
using UnityEngine;
using UnityEngine.UI;

public class EnterpriseAuthManager : MonoBehaviour
{
    [Header("UI Setup")] [SerializeField] [Tooltip("Button to login or logout from the enterprise account")]
    private Button _requestButton;
    
    [SerializeField]
    private Text _requestButtonText;

    [SerializeField]
    private Text _statusText;
    
    private string _userSessionAccessToken;

    private void Awake()
    {
        Refresh();
        _requestButton.onClick.AddListener(OnRequest);
        LoginManager.LoginComplete += OnLoginComplete;
    }

    private void OnDestroy()
    {
        LoginManager.LoginComplete -= OnLoginComplete;
    }

    private void OnLoginComplete()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (!string.IsNullOrEmpty(LightshipSettingsHelper.ActiveSettings.ApiKey))
        {
            Show("API Key Set", "Login", false);
        }
        else if (LightshipSettings.Instance.UseDeveloperAuthentication &&
            !AuthPublicUtils.IsEmptyOrExpiring(LightshipSettingsHelper.ActiveSettings.RefreshToken, 0))
        {
            Show("Developer Authentication Active", "Login", false);;
        }
        else if (LoginManager.IsLoginInProgress)
        {
            Show("Login in progress", "Cancel", true);
        }
        else if (LoginManager.IsLoggedIn)
        {
            Show("Logged In", "Logout", true);
        }
        else
        {
            Show("Not Logged In", "Login", true);
        }        
    }

    private void Show(string infoText, string buttonText, bool interactable)
    {
        _requestButton.interactable = interactable;
        _requestButtonText.text = buttonText;
        _statusText.text = infoText;
    }

    private void OnRequest()
    {
        if (LoginManager.IsLoggedIn)
        {
            LoginManager.LogoutRequested();
        }
        else
        {
            LoginManager.LoginRequested();
        }
        
        Refresh();
    }
}