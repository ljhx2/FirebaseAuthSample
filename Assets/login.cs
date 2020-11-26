using Google;
using Firebase.Auth;

using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using Facebook.Unity;
using System.Collections;
using System;

public class login : MonoBehaviour
{
	// Auth 용 instance
    FirebaseAuth auth = null;

    // 사용자 계정
    FirebaseUser user = null;

    // 기기 연동이 되어 있는 상태인지 체크한다.
    private bool signedIn = false;
    
    private void Awake()
    {
        
    }

    private void Start()
    {
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                Debug.Log("Firebase.DependencyStatus.Available");
                // Create and hold a reference to your FirebaseApp,
                // where app is a Firebase.FirebaseApp property of your application class.
                //app = Firebase.FirebaseApp.DefaultInstance;

                // Set a flag here to indicate whether Firebase is ready to use by your app.

                // 초기화
                auth = Firebase.Auth.FirebaseAuth.DefaultInstance;

                // 유저의 로그인 정보에 어떠한 변경점이 생기면 실행되게 이벤트를 걸어준다.
                auth.StateChanged += AuthStateChanged;
                //AuthStateChanged(this, null);
            }
            else
            {
                UnityEngine.Debug.LogError(System.String.Format(
                  "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
                // Firebase Unity SDK is not safe to use here.
            }
        });


        if (!FB.IsInitialized)
        {
            FB.Init(InitCallback_Facebook, OnHideUnity);
        }
        else
        {
            FB.ActivateApp();
        }

    }

    // 계정 로그인에 어떠한 변경점이 발생시 진입.
    void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        if (auth.CurrentUser != user)
        {
            // 연동된 계정과 기기의 계정이 같다면 true를 리턴한다. 
            signedIn = user != auth.CurrentUser && auth.CurrentUser != null;

            if (!signedIn && user != null)
            {
                UnityEngine.Debug.Log("Signed out " + user.UserId);
            }

            user = auth.CurrentUser;

            if (signedIn)
            {
                UnityEngine.Debug.Log("Signed in " + user.UserId);
                UnityEngine.Debug.Log("DisplayName : " + user.DisplayName ?? "");
                UnityEngine.Debug.Log("emailAddress : " + user.Email ?? "");
                UnityEngine.Debug.Log("photoUrl : " + user.PhotoUrl ?? "");
            }
        }
    }

    private void InitCallback_Facebook()
    {
        if (FB.IsInitialized)
        {
            FB.ActivateApp();
        }
        else
        {
            Debug.LogError("Failed to Initialize the Facebook SDK");
        }
    }
    private void OnHideUnity(bool isGameShown)
    {
        if (!isGameShown)
        {
            Time.timeScale = 0;
        }
        else
        {
            Time.timeScale = 1;
        }
    }

    private void OnDestroy()
    {
        auth.StateChanged -= AuthStateChanged;
        auth = null;
    }

    //////////////
    // 익명 로그인 //
    //////////////
    public void AnonyLogin()
    {
        // 익명 로그인 진행
        auth.SignInAnonymouslyAsync().ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("SignInAnonymouslyAsync was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("SignInAnonymouslyAsync encountered an error: " + task.Exception);
                return;
            }

            // 익명 로그인 연동 결과
            Firebase.Auth.FirebaseUser newUser = task.Result;
            Debug.LogFormat("User signed in successfully: {0} ({1})",
                newUser.DisplayName, newUser.UserId);
        });
    }
    
    //////////////
    // 구글 로그인 //
    //////////////
    public void GoogleLogout()
    {
        Debug.Log("Google Logout");
        GoogleSignIn.DefaultInstance.SignOut();
    }
    public void GoogleLoginProcessing()
    {
        if (GoogleSignIn.Configuration == null)
        {
            // 설정
            GoogleSignIn.Configuration = new GoogleSignInConfiguration
            {
                RequestIdToken = true,
                RequestEmail = true,
                // Copy this value from the google-service.json file.
                // oauth_client with type == 3
                WebClientId = "941687707302-gtlgk8ujs4ksg0ijnndjd0do6k33ljve.apps.googleusercontent.com"
            };
        }
        
        Task<GoogleSignInUser> signIn = GoogleSignIn.DefaultInstance.SignIn();

        TaskCompletionSource<FirebaseUser> signInCompleted = new TaskCompletionSource<FirebaseUser>();

        signIn.ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                Debug.Log("Google Login task.IsCanceled");
            }
            else if (task.IsFaulted)
            {
                Debug.Log("Google Login task.IsFaulted");
            }
            else
            {
                Credential credential = Firebase.Auth.GoogleAuthProvider.GetCredential(((Task<GoogleSignInUser>)task).Result.IdToken, null);
                auth.SignInWithCredentialAsync(credential).ContinueWith(authTask =>
                {
                    if (authTask.IsCanceled)
                    {
                        signInCompleted.SetCanceled();
                        Debug.Log("Google Login authTask.IsCanceled");
                        return;
                    }
                    if (authTask.IsFaulted)
                    {
                        signInCompleted.SetException(authTask.Exception);
                        Debug.Log("Google Login authTask.IsFaulted");
                        return;
                    }

                    // The user's Id, unique to the Firebase project.
                    // Do NOT use this value to authenticate with your backend server, if you
                    // have one; use User.TokenAsync() instead.
                    user = authTask.Result;
                    Debug.LogFormat("Google User signed in successfully: {0} ({1})", user.DisplayName, user.UserId);
                    return;
                });
            }
        });
    }

    
    ////////
    // 페이스북 로그인 //
    /////////
    public void FacebookLogout()
    {
        Debug.Log("Facebook Logout");
        FB.LogOut();
    }
    public void FacebookLogin()
    {
        List<string> perms = new List<string>() { "public_profile", "email", "user_friends" };
        FB.LogInWithReadPermissions(perms, FacebookAuthCallback);
    }
    private void FacebookAuthCallback(ILoginResult result)
    {
        if (result.Error != null)
        {
            Debug.LogError(string.Format("Facebook Login Error : {0}", result.Error));
        }
        else
        {
            if (FB.IsLoggedIn)
            {
                Debug.Log("Facebook Login!");

                AccessToken aToken = AccessToken.CurrentAccessToken;
                Debug.Log("ExpirationTime : " + aToken.ExpirationTime.ToString("MM/dd/yyyy HH:MM"));
                if (aToken.LastRefresh.HasValue)
                {
                    Debug.Log("LastRefresh : " + aToken.LastRefresh.Value.ToString("MM/dd/yyyy HH:MM"));
                }
                
                Debug.Log(string.Format("aToken string : {0}", aToken.TokenString));
                Debug.Log(string.Format("UserID : {0}", aToken.UserId));

                foreach (string perm in aToken.Permissions)
                {
                    Debug.Log(string.Format("permission : {0}", perm));
                }

                //FB.Mobile.RefreshCurrentAccessToken(Facebook_HandleResult);
                StartCoroutine(Co_FacebookLogin(aToken.TokenString));
            }
            else
            {
                Debug.LogError("User cancelled login");
            }
        }
    }
    private void Facebook_HandleResult(IResult result)
    {
        if (result == null)
        {
            Debug.LogError("Facebook_RefreshAccessToken Null Response");
            return;
        }
        if (!string.IsNullOrEmpty(result.Error))
        {
            Debug.LogError("Facebook_RefreshAccessToken Error Response : " + result.Error);
            return;
        }
        else if (result.Cancelled)
        {
            Debug.Log("Facebook_RefreshAccessToken Cancelled Response : " + result.RawResult);
            return;
        }
        else if (string.IsNullOrEmpty(result.RawResult))
        {
            Debug.LogError("Facebook_RefreshAccessToken Empty Response");
            return;
        }
        else
        {
            Debug.Log("Facebook_RefreshAccessToken Success Response :" + result.RawResult);
            StartCoroutine(Co_FacebookLogin(result.ResultDictionary["access_token"].ToString()));

            return;
        }
    }
    IEnumerator Co_FacebookLogin(string accessToken)
    {
        Debug.Log("Try to get Token..." + accessToken);
        while (System.String.IsNullOrEmpty(accessToken))
            yield return null;


        Credential credential = Firebase.Auth.FacebookAuthProvider.GetCredential(accessToken);
        Debug.Log("credential is " + credential.IsValid());
        auth.SignInWithCredentialAsync(credential).ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                Debug.Log("Facebook Login authTask.IsCanceled");
                return;
            }
            if (task.IsFaulted)
            {
                if (task.Exception != null)
                {
                    Debug.LogError("Exception : " + task.Exception);
                }

                Debug.Log("Facebook Login authTask.IsFaulted");
                return;
            }

            user = task.Result;
            Debug.LogFormat("Facebook User signed in successfully: {0} ({1})", user.DisplayName, user.UserId);
            return;
        });
    }

    /*
    ////////////////
    // 이메일 로그인 //
    ////////////////
    public void EmailLogin()
    {
        // 적당한 UGUI 를 만들어 email, pw 를 입력받는다.
        var email = EmailCreatePanel.transform.Find("email").Find("Text").GetComponent<UnityEngine.UI.Text>().text;
        var pw = EmailCreatePanel.transform.Find("pw").Find("Text").GetComponent<UnityEngine.UI.Text>().text;

        if (email.Length < 1 || pw.Length < 1)
        {
            Debug.Log("이메일 ID 나 PW 가 비어있습니다.");
            return;
        }

        auth.CreateUserWithEmailAndPasswordAsync(email, pw).ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                UnityEngine.Debug.LogError("CreateUserWithEmailAndPasswordAsync was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                UnityEngine.Debug.LogError("CreateUserWithEmailAndPasswordAsync encountered an error: " + task.Exception);
                return;
            }

            // firebase email user create
            Firebase.Auth.FirebaseUser newUser = task.Result;
            UnityEngine.Debug.LogFormat("Firebase Email user created successfully: {0} ({1})", newUser.DisplayName, newUser.UserId);
            return;
        });
    }
    */
    // 연동 해제
    public void SignOut()
    {
        if (auth.CurrentUser != null)
            auth.SignOut();
    }

    // 연동 계정 삭제
    public void UserDelete()
    {
        if (auth.CurrentUser != null)
            auth.CurrentUser.DeleteAsync();
    }
}