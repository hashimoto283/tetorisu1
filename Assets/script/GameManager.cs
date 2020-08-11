///
/// テトリスゲームのサンプルプログラム
/// 詳細に関しては
/// https://gmgyagami.xsrv.jp/blog/2020/03/08/tetris-sample/
/// をご一読下さい。
/// 
/// このスクリプトでは主に
/// 入出力の反映や、各種数値、ＵＩ画面、セットアップ命令などについて
/// 管理しています。
/// 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    //ゲームオーバー画面
    public GameObject panelGameover;
    //レベル、スコアのテキスト
    public GameObject objTextScore;
    Text txtScore;
    //数値管理
    public int nowScore;
    public int deleteLines;
    public float gameSpeed;
    const float gameSpeed_Default = 0.4f;
    Vector2Int frameSize = new Vector2Int(12, 22);

    void Start()
    {
        //セットアップ
        panelGameover.SetActive(false);
        txtScore = objTextScore.GetComponent<Text>();
        deleteLines = 0;
        nowScore = 0;
        gameSpeed = gameSpeed_Default;
        txtScore.text = nowScore.ToString();

        //テトリスゲームの初期設定
        var tetris = this.GetComponent<Tetris>();
        tetris.Tetris_Setup(frameSize);
        tetris.IntervalChange(gameSpeed);

        //開始
        Push_GameStart();
    }

    float pushDArrowCount = 0;
    bool isPushing = false;
    private void Update()
    {
        var tetris = this.GetComponent<Tetris>();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnityEngine.Application.Quit();
        }

        if (Input.GetKeyDown("z"))
        {
            tetris.Push_RotationButton(1);
        }
        if (Input.GetKeyDown("x"))
        {
            tetris.Push_RotationButton(3);
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            tetris.PushMoveButton(1);
            isPushing = true;
        }
        if (Input.GetKeyUp(KeyCode.DownArrow))
        {
            isPushing = false;
            pushDArrowCount = 0;
        }
        if (isPushing)
        {
            pushDArrowCount++;
            if (pushDArrowCount > 50)
            {
                tetris.PushMoveButton(1);
                pushDArrowCount = 45;
            }
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            tetris.PushMoveButton(0);
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            tetris.PushMoveButton(2);
        }
    }

    public void Push_GameStart()
    {
        var tetris = this.GetComponent<Tetris>();
        tetris.GameStart();
    }

    public void Score_Update(int deleteCount)
    {
        //スコア加算＝レベル×消去列倍率
        nowScore += PlusScore(deleteCount) * 10;
        txtScore.text = nowScore.ToString();
    }
    float SpeedSetting(int lv)
    {
        float speed = gameSpeed_Default - 0.1f*lv;
        if (speed < 0.2f) { speed = 0.2f; }
        return speed;
    }
    //消した列によるスコア加算値倍率
    int PlusScore(int count)
    {
        int bai = 1;
        switch (count)
        {
            case 2:
                bai = 3;
                break;
            case 3:
                bai = 6;
                break;
            case 4:
                bai = 10;
                break;
        }
        return bai;
    }
}
