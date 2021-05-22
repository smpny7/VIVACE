using System;
using UnityEngine;

public class NotesScript : MonoBehaviour
{
    // --- Attach from Unity --------------------------------------------------------------
    public int lineNum;
    // ------------------------------------------------------------------------------------

    // --- Instance -----------------------------------------------------------------------
    public PlayScreenProcessManager playScreenProcessManager;
    // ------------------------------------------------------------------------------------

    private int _lineLevel; // どの判定にいるのか
    private bool _isStop; // 一時停止中かどうか
    private KeyCode _lineKey; // このノーツを叩くキーコード
    private Rigidbody _rigidBody;

    private const bool IsDebugMode = false; // キーボードでのプレイ

    // ====================================================================================

    private void Start()
    {
        playScreenProcessManager = GameObject.Find("ProcessManager").GetComponent<PlayScreenProcessManager>();
        _rigidBody = GetComponent<Rigidbody>();
        _lineKey = GameUtil.GetKeyCodeByLineNum(lineNum); // キーコードの取得
        transform.localScale -= new Vector3(0.285f, 0, 0.055f); // 初期ポジションに移動
        _rigidBody.AddForce(0f, -0.625f * playScreenProcessManager.notesSpeedIndex,
            -0.625f * playScreenProcessManager.notesSpeedIndex * (float) Math.Sqrt(3), ForceMode.VelocityChange);
    }

    private void Update()
    {
        switch (PlayScreenProcessManager.IsPlaying)
        {
            case true:
            {
                if (_isStop)
                {
                    _rigidBody.AddForce(0f, -0.625f * playScreenProcessManager.notesSpeedIndex,
                        -0.625f * playScreenProcessManager.notesSpeedIndex * (float) Math.Sqrt(3),
                        ForceMode.VelocityChange);
                    _isStop = false;
                }

                if (_lineLevel == 6)
                {
                    RemoveFromNotesList(lineNum);
                    playScreenProcessManager.MissTimingFunc();
                    Destroy(gameObject);
                }

                if (IsDebugMode && (_lineLevel >= 1 || _lineLevel <= 5) && Input.GetKeyDown(_lineKey))
                    ONLaneTapped(lineNum);
                break;
            }
            case false when !_isStop:
                _rigidBody.velocity = Vector3.zero;
                _isStop = true;
                break;
        }
    }

    /// <summary>
    /// ノーツがJudgeオブジェクトを通過する際に実行されます．
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("GoodJudge") || other.gameObject.CompareTag("GreatJudge") ||
            other.gameObject.CompareTag("PerfectJudge")) _lineLevel++;
    }

    /// <summary>
    /// ノーツがJudgeオブジェクトを通過した後に実行されます．
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("GoodJudge") || other.gameObject.CompareTag("GreatJudge") ||
            other.gameObject.CompareTag("PerfectJudge")) _lineLevel++;
    }

    /// <summary>
    /// 有効範囲内のノーツがタップされた時の動作です．
    /// </summary>
    public void ONLaneTapped(int lineArgNum) // 0 ~ 4
    {
        if (_lineLevel < 1 || _lineLevel > 5) return;
        switch (_lineLevel) // 1：Good，2：Great，3：Perfect，4：Great，5：Good
        {
            case 1:
                RemoveFromNotesList(lineArgNum);
                playScreenProcessManager.GoodTimingFunc();
                Destroy(gameObject);
                break;
            case 2:
                RemoveFromNotesList(lineArgNum);
                playScreenProcessManager.GreatTimingFunc();
                Destroy(gameObject);
                break;
            case 3:
                RemoveFromNotesList(lineArgNum);
                playScreenProcessManager.PerfectTimingFunc();
                Destroy(gameObject);
                break;
            case 4:
                RemoveFromNotesList(lineArgNum);
                playScreenProcessManager.GreatTimingFunc();
                Destroy(gameObject);
                break;
            case 5:
                RemoveFromNotesList(lineArgNum);
                playScreenProcessManager.GoodTimingFunc();
                Destroy(gameObject);
                break;
        }
    }

    /// <summary>
    /// NotesScript のリストからノーツ情報を削除します．
    /// </summary>
    private void RemoveFromNotesList(int num)
    {
        switch (num)
        {
            case 0:
                playScreenProcessManager.lane0.RemoveAt(0);
                break;
            case 1:
                playScreenProcessManager.lane1.RemoveAt(0);
                break;
            case 2:
                playScreenProcessManager.lane2.RemoveAt(0);
                break;
            case 3:
                playScreenProcessManager.lane3.RemoveAt(0);
                break;
            case 4:
                playScreenProcessManager.lane4.RemoveAt(0);
                break;
        }
    }
}