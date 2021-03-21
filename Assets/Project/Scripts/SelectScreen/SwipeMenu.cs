using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class SwipeMenu : MonoBehaviour
{
    public GameObject scrollbar;
    float scroll_pos = 0;
    float distance;
    float[] pos;
    private AudioSource[] _AudioSource; //プレビュー楽曲情報格納
    //private AudioSource[] _fullAudioSource; //フル楽曲情報格納
    private static float[] Musictime; //楽曲の再生時間を格納
    public Text DisplayedMusicTime; //画面に表示される楽曲の再生時間
    public RectTransform Background;
    public RectTransform ScrollView;
    public HorizontalLayoutGroup ScrollViewPadding;
    public Text yourHighScoreText;
    public Text onlineHighScoreText;
    public ToggleGroup[] toggleGroup;
    public static int selectedNumTmp;
    private string selectedLevelTmp;

    // ------------------------------------------------------------------------------------

    static readonly string getMyScoreApiUri = EnvDataStore.getMyScoreApiUri;
    static readonly string getOnlineScoreApiUri = EnvDataStore.getOnlineScoreApiUri;
    static readonly string[] musicTitles = MusicTitleDataStore.musicTitles;

    // ------------------------------------------------------------------------------------

    [Serializable]
    public class MyScoreResponse
    {
        public bool success;
        public List<ScoreList> data;
    }

    [Serializable]
    public class ScoreList
    {
        public string name;
        public int score;
    }

    void Start()
    {
        pos = new float[transform.childCount];
        Musictime = new float[transform.childCount];
        distance = 1f / (pos.Length - 1f);
        for (int i = 0; i < pos.Length; i++)
            pos[i] = distance * i;
        //ScreenResponsive();
        GetScoresCotroller(0);
        _AudioSource = GameObject.Find("Music - pre").GetComponents<AudioSource>(); //プレビュー楽曲情報取得
        //_fullAudioSource = GameObject.Find("Music - full").GetComponents<AudioSource>(); //フル楽曲情報取得
        //for (int j = 0; j < pos.Length; j++)
        //Musictime[j] = _fullAudioSource[j].clip.length;
    }

    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            scroll_pos = scrollbar.GetComponent<Scrollbar>().value;
        }
        else
        {
            for (int i = 0; i < pos.Length; i++)
            {
                if (scroll_pos < pos[i] + (distance / 2) && scroll_pos > pos[i] - (distance / 2))
                    scrollbar.GetComponent<Scrollbar>().value = Mathf.Lerp(scrollbar.GetComponent<Scrollbar>().value, pos[i], 0.1f);
            }
        }
        for (int i = 0; i < pos.Length; i++)
        {
            if (scroll_pos < pos[i] + (distance / 2) && scroll_pos > pos[i] - (distance / 2))
            {
                transform.GetChild(i).localScale = Vector2.Lerp(transform.GetChild(i).localScale, new Vector2(1f, 1f), 0.1f);
                transform.GetChild(i).Find("NoteLogo").localScale = Vector2.Lerp(transform.GetChild(i).Find("NoteLogo").localScale, new Vector2(1f, 1f), 0.1f);
                transform.GetChild(i).Find("NoteLogo").gameObject.transform.position = Vector2.Lerp(transform.GetChild(i).Find("NoteLogo").transform.position, new Vector2(transform.GetChild(i).Find("NoteLogo").transform.position.x, 35), 0.08f);
                transform.GetChild(i).Find("Title").localScale = Vector2.Lerp(transform.GetChild(i).Find("Title").localScale, new Vector2(1f, 1f), 0.08f);
                transform.GetChild(i).Find("Title").gameObject.transform.position = Vector2.Lerp(transform.GetChild(i).Find("Title").transform.position, new Vector2(transform.GetChild(i).Find("Title").transform.position.x, 35), 0.08f);
                transform.GetChild(i).Find("Artist").localScale = Vector2.Lerp(transform.GetChild(i).Find("Artist").localScale, new Vector2(1f, 1f), 0.08f);
                transform.GetChild(i).Find("Artist").gameObject.transform.position = Vector2.Lerp(transform.GetChild(i).Find("Artist").transform.position, new Vector2(transform.GetChild(i).Find("Artist").transform.position.x, 30), 0.08f);
                transform.GetChild(i).Find("Artwork").localScale = Vector2.Lerp(transform.GetChild(i).Find("Artwork").localScale, new Vector2(1f, 1f), 0.08f);
                transform.GetChild(i).Find("Artwork").gameObject.transform.position = Vector2.Lerp(transform.GetChild(i).Find("Artwork").transform.position, new Vector2(transform.GetChild(i).Find("Artwork").transform.position.x, 10), 0.1f);
                transform.GetChild(i).Find("Easy").gameObject.SetActive(true);
                transform.GetChild(i).Find("Basic").gameObject.SetActive(true);
                transform.GetChild(i).Find("Hard").gameObject.SetActive(true);
                transform.GetChild(i).Find("Demon").gameObject.SetActive(true);
                transform.GetChild(i).Find("PlayMusic").gameObject.SetActive(true);

                SelectedMusic(i); //楽曲再生の実行と停止を行う（1フレーム毎）
                GetScoresCotroller(i);

                for (int cnt = 0; cnt < pos.Length; cnt++)
                {
                    if (i != cnt)
                    {
                        transform.GetChild(cnt).localScale = Vector2.Lerp(transform.GetChild(cnt).localScale, new Vector2(0.8f, 0.7f), 0.1f);
                        transform.GetChild(cnt).Find("NoteLogo").localScale = Vector2.Lerp(transform.GetChild(cnt).Find("NoteLogo").localScale, new Vector2(1.1f, 1.257f), 0.1f);
                        transform.GetChild(cnt).Find("NoteLogo").gameObject.transform.position = Vector2.Lerp(transform.GetChild(cnt).Find("NoteLogo").transform.position, new Vector2(transform.GetChild(cnt).Find("NoteLogo").transform.position.x, 23), 0.08f);
                        transform.GetChild(cnt).Find("Title").localScale = Vector2.Lerp(transform.GetChild(cnt).Find("Title").localScale, new Vector2(1.1f, 1.257f), 0.08f);
                        transform.GetChild(cnt).Find("Title").gameObject.transform.position = Vector2.Lerp(transform.GetChild(cnt).Find("Title").transform.position, new Vector2(transform.GetChild(cnt).Find("Title").transform.position.x, 23), 0.08f);
                        transform.GetChild(cnt).Find("Artist").localScale = Vector2.Lerp(transform.GetChild(cnt).Find("Artist").localScale, new Vector2(1f, 1.14f), 0.08f);
                        transform.GetChild(cnt).Find("Artist").gameObject.transform.position = Vector2.Lerp(transform.GetChild(cnt).Find("Artist").transform.position, new Vector2(transform.GetChild(cnt).Find("Artist").transform.position.x, 19), 0.08f);
                        transform.GetChild(cnt).Find("Artwork").localScale = Vector2.Lerp(transform.GetChild(cnt).Find("Artwork").localScale, new Vector2(1.5f, 1.71f), 0.08f);
                        transform.GetChild(cnt).Find("Artwork").gameObject.transform.position = Vector2.Lerp(transform.GetChild(cnt).Find("Artwork").transform.position, new Vector2(transform.GetChild(cnt).Find("Artwork").transform.position.x, -3), 0.1f);
                        transform.GetChild(cnt).Find("Easy").gameObject.SetActive(false);
                        transform.GetChild(cnt).Find("Basic").gameObject.SetActive(false);
                        transform.GetChild(cnt).Find("Hard").gameObject.SetActive(false);
                        transform.GetChild(cnt).Find("Demon").gameObject.SetActive(false);
                        transform.GetChild(cnt).Find("PlayMusic").gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    private void ScreenResponsive()
    {
        float scale = 1f;
        if (Screen.width < 1920)
            scale = 1.5f;
        if (Screen.width < Screen.height)
            scale = (Screen.height * 16) / (Screen.width * 9);
        Background.sizeDelta = new Vector2(Screen.width * scale, Screen.height * scale);
        ScrollView.sizeDelta = new Vector2(Screen.width, 750);
        ScrollViewPadding.padding = new RectOffset(Screen.width / 2 - 325, Screen.width / 2 - 325, 100, 0);
    }

    private void SelectedMusic(int num) //num番目の曲をループで再生
    {
        //int secondtemp = 0; //選択中楽曲の再生秒
        //int minutetemp = 0; //選択中楽曲の再生分
        for (int i = 0; i < pos.Length; i++)
        {
            if (i == num) //*numは現在選択中の楽曲通し番号
            {
                if (_AudioSource[i].isPlaying == false) //選択中の楽曲がプレビュー再生されていないとき
                {
                    _AudioSource[num].Play(); //選択中の楽曲を再生
                    //minutetemp = (int)Musictime[num] / 60;
                    //secondtemp = (int)Musictime[num] - 60 * minutetemp;
                    //DisplayedMusicTime.text = Musictime[num].ToString("PLAYTIME  " + minutetemp + ":" + secondtemp);
                }
            }
            else
            {
                if (_AudioSource[i].isPlaying == true) //非選択の楽曲がプレビュー再生されているとき
                    _AudioSource[i].Stop(); //非選択の楽曲の再生を停止
            }
        }
    }

    public void GetScoresCotroller(int selectedNum)
    {
        string selectedLevel = toggleGroup[selectedNum].ActiveToggles()
            .First().GetComponentsInChildren<Text>()
            .First(t => t.name == "Label").text;
        if (selectedNumTmp != selectedNum || selectedLevel != selectedLevelTmp)
        {
            StartCoroutine(MyScoreNetworkProcess(musicTitles[selectedNum], selectedLevel));
            StartCoroutine(OnlineScoreNetworkProcess(musicTitles[selectedNum], selectedLevel));
        }
        selectedNumTmp = selectedNum;
        selectedLevelTmp = selectedLevel;
    }

    IEnumerator MyScoreNetworkProcess(string selectedMusic, string selectedLevel)
    {
        WWWForm form = new WWWForm();
        form.AddField("token", PlayerPrefs.GetString("jwt"));
        form.AddField("music", selectedMusic);
        form.AddField("level", selectedLevel);
        UnityWebRequest www = UnityWebRequest.Post(getMyScoreApiUri, form);
        yield return www.SendWebRequest();
        if (www.isNetworkError)
            yourHighScoreText.text = "--------";
        else
            ApplyMyScore(www.downloadHandler.text);
    }

    private void ApplyMyScore(string data)
    {
        MyScoreResponse jsnData = JsonUtility.FromJson<MyScoreResponse>(data);

        if (jsnData.success && jsnData.data.Count != 0)
            yourHighScoreText.text = jsnData.data[0].score.ToString();
        else
            yourHighScoreText.text = "--------";
    }

    IEnumerator OnlineScoreNetworkProcess(string selectedMusic, string selectedLevel)
    {
        WWWForm form = new WWWForm();
        form.AddField("music", selectedMusic);
        form.AddField("level", selectedLevel);
        UnityWebRequest www = UnityWebRequest.Post(getOnlineScoreApiUri, form);
        yield return www.SendWebRequest();
        if (www.isNetworkError)
            onlineHighScoreText.text = "--------";
        else
            ApplyOnlineScore(www.downloadHandler.text);
    }

    private void ApplyOnlineScore(string data)
    {
        MyScoreResponse jsnData = JsonUtility.FromJson<MyScoreResponse>(data);

        if (jsnData.success && jsnData.data.Count != 0)
            onlineHighScoreText.text = jsnData.data[0].score.ToString();
        else
            onlineHighScoreText.text = "--------";
    }
}