using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Google;
using Firebase;
using Firebase.Auth;
using Firebase.Unity.Editor;
using UnityEngine.UI;
using System.Threading.Tasks;

public class AnonyOfChange : MonoBehaviour
{
	// Auth 용 instance
	FirebaseAuth auth = null;

	// 사용자 계정
	FirebaseUser user = null;

    // 이메일 전환창
    public GameObject emailPanel;

    private void Start()
    {
        auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
    }

    // 익명 로그인 -> 구글 로그인
    public void onAnonyToGoogle()
    {
        if (auth.CurrentUser != null)
        {
            Debug.Log(auth.CurrentUser.UserId);

            if (GoogleSignIn.Configuration == null)
            {
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

                    //string currentUserId = auth.CurrentUser.UserId;
                    //string cureentEmail = auth.CurrentUser.Email;
                    //string currentDisplayName = auth.CurrentUser.DisplayName;
                    //System.Uri currentPhotoUrl = auth.CurrentUser.PhotoUrl;

                    auth.CurrentUser.LinkWithCredentialAsync(credential).ContinueWith(authTask =>
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

                        user = authTask.Result;
                        Debug.LogFormat("Google User signed in successfully: {0} ({1})", user.DisplayName, user.UserId);
                    });
                }
            });
        }
        else
        {
            Debug.Log("Not logged in");
        }
    }

    // 익명 로그인 -> 이메일 로그인
    public void onAnonyToEmail()
    {
        if (emailPanel == null)
        {
            Debug.Log("이메일 전환창이 없습니다.");
            return;
        }

        // 아래에 위치한 onEmailChangeSwich() 를 실행시키기 위한 패널
        emailPanel.SetActive(true);
    }

    // 이메일 로그인 정보를 받아온 후 사용한다.
    public InputField id;
    public InputField pw;
    public void onEmailChangeSwich()
    {
        if (id.text.Length < 1 || pw.text.Length < 1)
        {
            Debug.Log("이메일 ID 나 PW 가 비어있습니다.");
            return;
        }

        Credential credential = Firebase.Auth.EmailAuthProvider.GetCredential(id.text, pw.text);

        auth.CurrentUser.LinkWithCredentialAsync(credential).ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                Debug.Log("Email Login task.IsCanceled");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.Log("Email Login task.Faulted");
                return;
            }

            user = task.Result;
            Debug.LogFormat("Firebase Email user created successfully: {0} ({1})", user.DisplayName, user.UserId);
        });
    }
}
