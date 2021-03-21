using System;
using System.IO;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using DG.Tweening;

public class PlayScreenProcessManager : MonoBehaviour
{
    // --- Attach from Unity --------------------------------------------------------------
    public GameObject[] Note; // Attach 5 notes objects.
    public GameObject PausePanel = null;
    public RectTransform Background; // Background image for responsive.
    public Text ComboText, ScoreText, JudgeText, AddText;
    public Tweener JTextFade, JTextReduce, ATextFade, ATextReduce; // For Tweener animation.
    // ------------------------------------------------------------------------------------

    // --- External variables -------------------------------------------------------------
    public static bool _autoPlay = false;
    public static bool _isPlaying;
    public static int _score = 0, _perfect = 0, _great = 0, _good = 0, _miss = 0;
    public static Vector3 _deltaPosition; // Vector falling every frame.
    // ------------------------------------------------------------------------------------

    // --- Environment variables ----------------------------------------------------------
    static readonly string registScoreApiUri = EnvDataStore.registScoreApiUri;
    static readonly string[] musicTitles = MusicTitleDataStore.musicTitles;
    // ------------------------------------------------------------------------------------

    // Audio
    private AudioSource playAudioSource;
    private AudioClip playMusic;
    private AudioSource[] judgeAudioSource;

    // Color
    private Color Perfect_c, Great_c, Good_c, Miss_c, Score_c;

    // CSV data
    private int[] lineNum = new int[2048];
    private float[] timing = new float[2048];

    private bool alreadyPlayedFlag = false;
    private bool pausebuttonFlag = false;
    private bool lowGraphicsModeFlag;
    private bool notesTouchSoundFlag;
    private double score = 0, baseScore = 0, logSqSum = 0;
    private double[] logSq; // Point increase border
    private float startTime = 0, stopTime = 0;
    public float notesSpeedIndex = 5.0f; // 3.0f ~ 8.0f
    private int combo = 0, perfect = 0, great = 0, good = 0, miss = 0, notesTotal = 0, notesCount = 0, sepPoint = 50;
    public int startTimingIndex; // Every 10ms / "+" -> slow / "-" ->fast
    private int JTextUsed = 0; // Number of times JudgeText has been changed.

    // ====================================================================================

    [Serializable]
    public class RegistScoreResponse
    {
        public bool success;
        public string msg;
    }

    // ====================================================================================

    async void Start()
    {
        // GUI Settings
        //ScreenResponsive();
        TextInitialization();
        ColorInitialization();
        AudioInitialization();
        AdjustJudgeRange();

        startTimingIndex = (PlayerPrefs.GetInt("TimingAdjustment", 5) - 5) * 10;
        notesSpeedIndex = (float)(5.0f + (PlayerPrefs.GetInt("NotesFallSpeed", 5) - 5) * 0.5f);
        _deltaPosition = (Vector3.down + Vector3.back * (float)Math.Sqrt(3)) * 0.6f * notesSpeedIndex;

        // Score Calculation
        LoadCSV();
        BaseScoreDecision();

        _isPlaying = true;

        // Play
        startTime = Time.time;
        await Task.Delay((int)((7800 + 10 * startTimingIndex) / notesSpeedIndex));
        playAudioSource.Play();
        // await Task.Delay(10000);

        alreadyPlayedFlag = true;
        lowGraphicsModeFlag = (PlayerPrefs.GetInt("lowGraphicsMode", 1) == 1 ? true : false);
        notesTouchSoundFlag = (PlayerPrefs.GetInt("NotesTouchSound", 1) == 1 ? true : false);
    }

    void Update()
    {
        if (_isPlaying) CheckNextNotes();
        if (alreadyPlayedFlag && !playAudioSource.isPlaying)
        {
            alreadyPlayedFlag = false;
            ResultSceneTransition();
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
    }

    private void TextInitialization()
    {
        AddText.text = "";
        JudgeText.text = "";
        ComboText.text = combo.ToString("D");
        ScoreText.text = ((int)Math.Round(score, 0, MidpointRounding.AwayFromZero)).ToString("D7");
    }

    private void ColorInitialization()
    {
        ColorUtility.TryParseHtmlString("#FF7DF2", out Perfect_c);
        ColorUtility.TryParseHtmlString("#FF9C7D", out Great_c);
        ColorUtility.TryParseHtmlString("#34E045", out Good_c);
        ColorUtility.TryParseHtmlString("#8D8D8D", out Miss_c);
        ColorUtility.TryParseHtmlString("#6C95FF", out Score_c);
    }

    private void AudioInitialization()
    {
        judgeAudioSource = GameObject.Find("SoundEffect").GetComponents<AudioSource>();
        Debug.Log("music/" + musicTitles[SwipeMenu.selectedNumTmp]);
        playMusic = Resources.Load<AudioClip>("music/" + musicTitles[SwipeMenu.selectedNumTmp]);
        playAudioSource = gameObject.AddComponent<AudioSource>();
        playAudioSource.clip = playMusic;
    }

    private void AdjustJudgeRange()
    {
        Transform PerfectRange = GameObject.Find("PerfectJudgeLine").GetComponent<Transform>();
        PerfectRange.transform.localScale = new Vector3(1.8f, 0.1f, notesSpeedIndex * 1.2f * 0.12f);
        Transform GreatRange = GameObject.Find("GreatJudgeLine").GetComponent<Transform>();
        GreatRange.transform.localScale = new Vector3(1.8f, 0.1f, notesSpeedIndex * 1.2f * 0.18f);
        Transform GoodRange = GameObject.Find("GoodJudgeLine").GetComponent<Transform>();
        GoodRange.transform.localScale = new Vector3(1.8f, 0.1f, notesSpeedIndex * 1.2f * 0.2f);
    }

    private void LoadCSV()
    {
        Debug.Log("CSV/" + musicTitles[SwipeMenu.selectedNumTmp] + "_" + SelectScreenProcessManager.selectedLevel);
        TextAsset csv = Resources.Load("CSV/" + musicTitles[SwipeMenu.selectedNumTmp] + "_" + SelectScreenProcessManager.selectedLevel) as TextAsset;
        StringReader reader = new StringReader(csv.text);
        while (reader.Peek() > -1)
        {
            string line = reader.ReadLine();
            string[] values = line.Split(',');
            timing[notesTotal] = float.Parse(values[0]);
            lineNum[notesTotal++] = int.Parse(values[1]);
        }
    }

    // 区分求積法によるスコア計算の準備
    private void BaseScoreDecision()
    {
        int denominator;
        logSq = new double[sepPoint];

        if (notesTotal >= sepPoint)
            denominator = sepPoint;
        else
            denominator = notesTotal;

        for (int i = 0; i < denominator; i++)
        {
            logSq[i] = Math.Log10(1 + (9 * ((double)i + 1) / (double)denominator));
            logSqSum += logSq[i];
        }
        baseScore = 1000000 / (logSqSum + (double)notesTotal - (double)denominator);
    }

    private void CheckNextNotes() // != 0だと反転孤独線の譜面が正常に読み込めません． >=0はどうですか？
    {
        while (timing[notesCount] < (Time.time - startTime) && timing[notesCount] != 0) SpawnNotes(lineNum[notesCount++]);
    }

    private void SpawnNotes(int lineNum)
    {
        Instantiate(Note[lineNum], new Vector3(-0.73f + (0.365f * lineNum), 5.4f, -0.57f), Quaternion.Euler(-30f, 0, 0));
    }

    public async void Pause() // ポーズ機能
    {
        if (_isPlaying && !pausebuttonFlag)
        {
            _isPlaying = false;
            stopTime = Time.time;
            playAudioSource.pitch = 0.0f;
            pausebuttonFlag = true;
            PausePanel.SetActive(true);
        }
        else if (!_isPlaying && pausebuttonFlag)
        {
            PausePanel.SetActive(false);
            pausebuttonFlag = false;
            for (int i = 3; i > 0; i--)
            {
                await Task.Delay(1000);
                SoundEffect(2);
            }
            await Task.Delay(1000);
            _isPlaying = true;
            startTime = startTime + (Time.time - stopTime);
            await Task.Delay(10);
            playAudioSource.pitch = 1.0f;
        }
    }

    public void SelectSceneTransition()
    {
        SceneManager.LoadScene("SelectScene");
    }

    public void PerfectTimingFunc()
    {
        if (notesTouchSoundFlag)
            SoundEffect(0);
        AddScore(0);
    }

    public void GreatTimingFunc()
    {
        if (notesTouchSoundFlag)
            SoundEffect(1);
        AddScore(1);
    }

    public void GoodTimingFunc()
    {
        if (notesTouchSoundFlag)
            SoundEffect(2);
        AddScore(2);
    }

    public void MissTimingFunc()
    {
        AddScore(3);
    }

    public void SoundEffect(int num)
    {
        //0：Perfect，1：Great，2：Good，3：Miss
        judgeAudioSource[num].PlayOneShot(judgeAudioSource[num].clip);
    }

    public async void AddScore(int num) // 引数は判定，0：Perfect，1：Great，2：Good，3：Miss
    {
        double magni = 0, scoreTemp = 0, JTextTemp = ++JTextUsed;
        Vector3 vl = new Vector3(1.5f, 1.5f, 1.5f);
        Vector3 vo = Vector3.one;

        // アニメーションの初期化
        if (JTextFade != null)
            JTextFade.Kill();
        if (JTextReduce != null)
            JTextReduce.Kill();
        if (ATextFade != null)
            ATextFade.Kill();
        if (ATextReduce != null)
            ATextReduce.Kill();

        switch (num)
        { // 判定による分岐，
            case 0:
                magni = 1;
                combo++;
                perfect++;
                JudgeText.transform.localScale = vl;
                JTextReduce = JudgeText.transform.DOScale(vo, 0.2f);
                JudgeText.color = Perfect_c;
                JudgeText.text = "Perfect!";
                break;
            case 1:
                magni = 0.75;
                combo++;
                great++;
                JudgeText.transform.localScale = vl;
                JTextReduce = JudgeText.transform.DOScale(vo, 0.2f);
                JudgeText.color = Great_c;
                JudgeText.text = "Great!";
                break;
            case 2:
                magni = 0.25;
                combo = 0;
                good++;
                JudgeText.transform.localScale = vo;
                JudgeText.color = Good_c;
                JudgeText.text = "Good!";
                break;
            case 3:
                magni = 0;
                combo = 0;
                miss++;
                JudgeText.transform.localScale = vo;
                JudgeText.color = Miss_c;
                JudgeText.text = "Miss!";
                break;
        }

        // コンボ数を更新
        ComboText.text = combo.ToString("D");

        // 区分求積法による加算スコアの計算
        if (combo <= sepPoint && combo > 0)
            scoreTemp = baseScore * logSq[combo - 1] * magni;
        else if (combo <= sepPoint)
            scoreTemp = baseScore * logSq[0] * magni;
        else
            scoreTemp = baseScore * magni;

        // 加算スコア表示の文字色を変更
        AddText.color = Score_c;
        // 加算スコア表示の値を更新
        AddText.text = "+" + ((int)Math.Round(scoreTemp, 0, MidpointRounding.AwayFromZero)).ToString("D");
        // アニメーション
        AddText.transform.localScale = vl;
        ATextReduce = AddText.transform.DOScale(vo, 0.2f);

        // 軽量化設定によってはスコアのパラパラ表示を省略
        if (!lowGraphicsModeFlag)
        {
            for (int i = 0; i < 15; i++)
            {
                score += scoreTemp / 15;
                ScoreText.text = ((int)Math.Round(score, 0, MidpointRounding.AwayFromZero)).ToString("D7");
                await Task.Delay(33);
            }
        }
        else
        {
            score += scoreTemp;
            ScoreText.text = ((int)Math.Round(score, 0, MidpointRounding.AwayFromZero)).ToString("D7");
        }

        //  ↓なんで!=じゃないんだっけ
        if (JTextUsed == JTextTemp)
            return;

        // 250ms経過しても次のAddscoreが発生していなければフェードアウト処理
        await Task.Delay(250);
        if (JTextUsed == JTextTemp)
        {
            JTextFade = DOTween.ToAlpha(() => JudgeText.color, cchanger => JudgeText.color = cchanger, 0.0f, 0.2f);
            ATextFade = DOTween.ToAlpha(() => AddText.color, cchanger => AddText.color = cchanger, 0.0f, 0.2f);
        }
        return;
    }

    public async void ResultSceneTransition()
    {
        _score = (int)Math.Round(score, 0, MidpointRounding.AwayFromZero);
        _perfect = perfect;
        _great = great;
        _good = good;
        _miss = miss;
        StartCoroutine(RegistScoreNetworkProcess(musicTitles[SwipeMenu.selectedNumTmp], SelectScreenProcessManager.selectedLevel, _score));
        await Task.Delay(1000);
        SceneManager.LoadScene("ResultScene");
    }

    IEnumerator RegistScoreNetworkProcess(string selectedMusic, string selectedLevel, int score)
    {
        WWWForm form = new WWWForm();
        form.AddField("token", PlayerPrefs.GetString("jwt"));
        form.AddField("music", selectedMusic);
        form.AddField("level", selectedLevel);
        form.AddField("score", _score);
        UnityWebRequest www = UnityWebRequest.Post(registScoreApiUri, form);
        yield return www.SendWebRequest();
        if (www.isNetworkError)
        {
            Debug.LogError("ネットワークに接続できません．(" + www.error + ")");
        }
        else
        {
            ResponseCheck(www.downloadHandler.text);
        }
    }

    private void ResponseCheck(string data)
    {
        RegistScoreResponse jsnData = JsonUtility.FromJson<RegistScoreResponse>(data);

        if (jsnData.success)
            Debug.Log("スコアを登録しました");
        else
            Debug.Log("スコアの登録に失敗しました");
    }
}