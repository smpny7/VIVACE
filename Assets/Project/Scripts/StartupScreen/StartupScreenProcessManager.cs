using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using Firebase.Database;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartupScreenProcessManager : MonoBehaviour
{
    private AudioSource _audioSource;
    public AudioClip sound;
    public GameObject panel;
    public RectTransform background;
    public Text versionDisplay;
    public Text inputUserName;

    private bool _touchableFlag = true;

    // ------------------------------------------------------------------------------------
    private const string ThisVersion = "1.1.0";
    // ------------------------------------------------------------------------------------

    private class User
    {
        public string username;

        public User(string username)
        {
            this.username = username;
        }
    }

    private async void Start()
    {
        PlayerPrefs.DeleteAll(); //ユーザ情報を初期化したい場合にコメントアウトを解除
        ScreenResponsive();
        FirebaseInit();
        _audioSource = GetComponent<AudioSource>();
        versionDisplay.text = "Ver." + ThisVersion;
        // await Task.Delay(1000);
        // _touchableFlag = true; // TODO 必要？
    }

    private static void FirebaseInit()
    {
        // ReSharper disable once NotAccessedVariable
        FirebaseApp firebaseApp;
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            var dependencyStatus = task.Result;
            if (dependencyStatus == DependencyStatus.Available)
                firebaseApp = FirebaseApp.DefaultInstance;
            else
                Debug.LogError($"Could not resolve all Firebase dependencies: {dependencyStatus}");
        });
    }

    private void Update()
    {
        if (!_touchableFlag) return; // 初期化作業が終わっていない場合，処理を行わない

        if (Input.GetMouseButtonUp(0)) InitializationProcessManagerAsync(); // クリックが検出された場合 → 認証
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended)
            InitializationProcessManagerAsync(); // タッチが検出された場合 → 認証
    }

    private void ScreenResponsive()
    {
        var scale = 1f;
        if (Screen.width < 1920) scale = 1.5f;
        if (Screen.width < Screen.height) scale = (float) (Screen.height * 16) / (Screen.width * 9);
        background.sizeDelta = new Vector2(Screen.width * scale, Screen.height * scale);
    }

    private async void InitializationProcessManagerAsync()
    {
        _touchableFlag = false;
        _audioSource.PlayOneShot(sound); // タッチ音を鳴らす
        await FirebaseAuthenticationAsync();
        if (PlayerPrefs.HasKey("username")) SceneManager.LoadScene("SelectScene");
    }

    private async Task FirebaseAuthenticationAsync()
    {
        var auth = FirebaseAuth.DefaultInstance;
        var reference = FirebaseDatabase.DefaultInstance.RootReference;
        if (auth.CurrentUser.UserId == null) // 認証済みユーザかどうか
        {
            var userid = await auth.SignInAnonymouslyAsync()
                .ContinueWith(task => !task.IsCanceled && !task.IsFaulted ? task.Result.UserId : null);
            PlayerPrefs.SetString("userid", userid);
            panel.SetActive(true);
        }
        else
        {
            var snap = await reference.Child("users").Child(auth.CurrentUser.UserId).GetValueAsync();
            var user = JsonUtility.FromJson<User>(snap.Value.ToString());
            PlayerPrefs.SetString("userid", auth.CurrentUser.UserId);
            PlayerPrefs.SetString("username", user.username);
        }
    }

    private void LicenseActivation()
    {
        // TODO: ライセンス認証
    }

    public async void RegisterButtonTappedController()
    {
        if (inputUserName.text.Length < 3) return;
        PlayerPrefs.SetString("username", inputUserName.text);
        var user = new User(PlayerPrefs.GetString("username"));
        var json = JsonUtility.ToJson(user);
        var reference = FirebaseDatabase.DefaultInstance.RootReference;
        await reference.Child("users").Child(PlayerPrefs.GetString("userid")).SetValueAsync(json);
        SceneManager.LoadScene("SelectScene");
    }
}