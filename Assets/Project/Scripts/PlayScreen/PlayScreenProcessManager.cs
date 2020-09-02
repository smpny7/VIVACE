﻿using System;
using System.IO;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class PlayScreenProcessManager : MonoBehaviour
{
    private AudioSource _audioSource;
    public GameObject[] Note;
    public Text ComboText, ScoreText;
    private int _notesTotal = 0;
    private int _notesCount = 0;
    private float _startTime = 0;
    private float _stoptime = 0;
    public float[] _timing;
    public int[] _lineNum;
    private static AudioSource[] _SoundEffects;
    public static bool _isPlaying = true;
    public static bool _autoPlay = true; //自動プレイ用
    public int _combo = 0; //コンボ数
    public int _perfects = 0, _greats = 0, _goods = 0, _misss = 0;
    public static int r_perfects = 0, r_greats = 0, r_goods = 0, r_misss = 0; //リザルト画面用
    public double _score = 0; //得点
    public static double r_score = 0; //リザルト画面用
    public double _basescore = 0; //基礎点:ノーツ1つあたりのスコア

    // -- Temporary Variable. -------------------------------------------------------------
    private string csvFilePass = "CSV/burningHeart";
    // ------------------------------------------------------------------------------------

    async void Start()
    {
        _timing = new float[1024];
        _lineNum = new int[1024];
        ComboText.text = _combo.ToString("D");
        ScoreText.text = ((int) Math.Round (_score, 0, MidpointRounding.AwayFromZero)).ToString ("D7");
        await Task.Delay(1000);
        LoadCSV();
        _audioSource = GameObject.Find("Music").GetComponent<AudioSource>();
        _SoundEffects = GameObject.Find("SoundEffect").GetComponents<AudioSource>();
        _startTime = Time.time;
        await Task.Delay(2550);
        _audioSource.Play();
    }

    void Update()
    {
        if (_isPlaying == true) CheckNextNotes();
    }

    void LoadCSV()
    {
        TextAsset csv = Resources.Load(csvFilePass) as TextAsset;
        StringReader reader = new StringReader(csv.text);
        while (reader.Peek() > -1)
        {
            string line = reader.ReadLine();
            string[] values = line.Split(',');
            _timing[_notesTotal] = float.Parse(values[0]);
            _lineNum[_notesTotal++] = int.Parse(values[1]);
        }

        if (_notesTotal >= 30)
        { //コンボ数が30以上のとき
            _basescore = 1000000 / ((double)_notesTotal - 15); //基礎点は1000000点を最大コンボ数-15で割った値
        }
        else
        { //コンボ数が30未満のとき
            _basescore = 1000000 / (double)_notesTotal; // 基礎点は1000000点を最大コンボ数で割った値
        }
    }

    public async void AddScore(double magni)
    { //加点のための関数,引数magniは判定ごとのスコア倍率
        double ScoreTemp = 0;
        if (_notesTotal >= 30)
        { //コンボ数が30以上のときにはスコアは以下の通り傾斜加算
            if (_combo <= 10) //コンボ数が10以下のとき
                ScoreTemp = _basescore * 0.25 * magni; //スコアに基礎点*倍率の25％を加算
            else if (_combo <= 20) //コンボ数が20以下のとき
                ScoreTemp = _basescore * 0.5 * magni; //スコアに基礎点*倍率の50％を加算
            else if (_combo <= 30) //コンボ数が30以下のとき
                ScoreTemp = _basescore * 0.75 * magni; //スコアに基礎点*倍率の75％を加算
            else //コンボ数が31以上のとき
                ScoreTemp = _basescore * magni; //スコアに基礎点*倍率を加算
        }
        else
        { //コンボ数が30未満のときは単に基礎点*倍率を加算
            ScoreTemp = _basescore * magni;
        }
        for (int i = 0; i < 15; i++) //15分割したものを33ミリ秒ごとに15回加算()
        {
            _score += ScoreTemp / 15;
            ScoreText.text = ((int)Math.Round(_score, 0, MidpointRounding.AwayFromZero)).ToString("D7"); //四捨五入して型変換を行い表示を更新
            await Task.Delay(33);
            r_score = _score;
        }
    }

    void CheckNextNotes()
    {
        while (_timing[_notesCount] < (Time.time - _startTime) && _timing[_notesCount] != 0) SpawnNotes(_lineNum[_notesCount++]);
    }

    void SpawnNotes(int num)
    {
        Debug.Log(num);
        Instantiate(Note[num], new Vector3(-0.676f + (0.338f * num), 8.4f, 4.5f), Quaternion.Euler(-30f, 0, 0));
    }

    public static void SoundEffect(int num)
    {
        _SoundEffects[num].PlayOneShot(_SoundEffects[num].clip);
        //Debug.Log ("SoundEffect Played.");
    }

    public async void Pause()
    {
        int i = 0;
        if (_isPlaying == true)
        {
            Debug.Log("止まるドン！");
            _isPlaying = false;
            _stoptime = Time.time;
            _audioSource.Pause();
        }
        else if (_isPlaying == false)
        {
            Debug.Log("さぁ，再開するドン！");
            for (i = 3; i > 0; i--)
            {
                await Task.Delay(1000);
                Debug.Log(i);
                SoundEffect(2);
            }
            await Task.Delay(1000);
            _isPlaying = true;
            _startTime = _startTime + (Time.time - _stoptime);
            _audioSource.UnPause();
        }
    }

    public static void PerfectTimingFunc(int num)
    {
        PlayScreenProcessManager a = GameObject.Find("ProcessManager").GetComponent<PlayScreenProcessManager>();
        // Debug.Log ("Line:" + num + " Perfect!"); //ログ出力
        // Debug.Log (GetMusicTime ()); //ログ出力
        //EffectManager.Instance.PlayEffect(num); //num番目のエフェクトを表示
        SoundEffect(0); //Perfectサウンド（引数0）を再生
        a._combo++; //コンボ数を1加算
        a._perfects++; //累計Perfect数を1加算
        r_perfects++; //リザルト画面用
        a.ComboText.text = a._combo.ToString("D");
        a.AddScore(1); //スコア加算(倍率はPerfectなので1)
        Debug.Log("PerfectTimingFunc"); //ログ出力
    }

    public static void GreatTimingFunc(int num)
    {
        PlayScreenProcessManager a = GameObject.Find("ProcessManager").GetComponent<PlayScreenProcessManager>();
        SoundEffect(1); //Greatサウンド再生
        //EffectManager.Instance.PlayEffect(num); //num番目のエフェクトを表示
        a._combo++; //コンボ数を1加算
        a._greats++; //累計Great数を1加算
        r_greats++; //リザルト画面用
        a.ComboText.text = a._combo.ToString("D");
        a.AddScore(0.75); //スコア加算(倍率はGreatなので0.75)
        Debug.Log("GreatTimingFunc"); //ログ出力
    }

    public static void GoodTimingFunc(int num)
    {
        PlayScreenProcessManager a = GameObject.Find("ProcessManager").GetComponent<PlayScreenProcessManager>();
        SoundEffect(2); //Goodサウンド再生
        //EffectManager.Instance.PlayEffect(num); //num番目のエフェクトを表示
        a._combo++; //コンボ数を1加算
        a._goods++; //累計Good数を1加算
        r_goods++; //リザルト画面用
        a.ComboText.text = a._combo.ToString("D"); //コンボ数を1加算
        a.AddScore(0.25); //スコア加算(倍率はGoodなので0.25)
        Debug.Log("GoodTimingFunc"); //ログ出力
    }

    public static void MissTimingFunc(int num)
    {
        PlayScreenProcessManager a = GameObject.Find("ProcessManager").GetComponent<PlayScreenProcessManager>();
        //EffectManager.Instance.PlayEffect(num); //num番目のエフェクトを表示
        a._combo = 0; //コンボ数を初期化
        a._misss++; //累計Miss数を1加算
        r_misss++; //リザルト画面用
        a.ComboText.text = a._combo.ToString("D");
        //スコアはあげないよ！ｗ
        Debug.Log("MissTimingFunc"); //ログ出力
    }
}