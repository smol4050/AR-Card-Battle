using UnityEngine;

/// <summary>
/// Component that configures Auth from the home page
/// </summary>
public class AuthHomeConfig : MonoBehaviour
{
    [SerializeField]
    private AuthEndpoints authEndpoints;
    
    [SerializeField]
    private GameObject[] authUiElements;
    
    [SerializeField]
    private string mockDeepLinkUrl;

    private void Awake()
    {
        authEndpoints?.SetAsSettings();

        foreach(var element in authUiElements)
        {
#if NIANTIC_ARDK_AUTH_LOGIN
            element.SetActive(true);
#else
            element.SetActive(false);
#endif
        }
#if NIANTIC_ARDK_AUTH_LOGIN
        // Test code for mocking a deep link when running in the editor:
#if UNITY_EDITOR
        if (!string.IsNullOrEmpty(mockDeepLinkUrl))
        {
            LoginManager.MockDeepLink(mockDeepLinkUrl);
            return;
        }
#endif

        UserSessionManager.Start();
        AuthAccessManager.StartAuthAccess(UserSessionManager.AccessToken);
#endif
    }
}
