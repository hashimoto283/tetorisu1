///
/// テトリスゲームのサンプルプログラム
/// 詳細に関しては
/// https://gmgyagami.xsrv.jp/blog/2020/03/08/tetris-sample/
/// をご一読下さい。
/// 
/// このスクリプトでは主に
/// テトリスゲームの処理、タイルマップへの描画などについて
/// 管理しています。
/// 

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Tetris : MonoBehaviour
{
    //カメラ
    public GameObject mainCamera;
    //ゲームオブジェクトの読み込み
    public GameObject tileObj;
    //タイルマップ
    private Tilemap tilemap;
    //描画タイルの指定
    public TileBase[] tileChip;
    //枠の大きさ
    Vector2Int fSize;
    //タイルの情報を入れる２次元配列
    TileData[,] tileData;
    //仮想配列
    TileData[,] nextTileData;
    //落下中のオブジェクト情報
    DropTiles dropTiles;
    //一度の落下にかかる時間
    public float dropInterval;
    //消去の処理時間
    public float deleteInterval;
    //落下コルーチン
    IEnumerator DropCoroutine;

    //次のブロック表示用
    int rndBlock = 0;
    int nextBlock = 0;
    public GameObject objNextBlocks;
    Tilemap nextTilemap;

    //ゲーム操作フラグ管理
    public enum GAME_MODE
    {
        PLAY,
        WAIT,
        GAMEOVER,
    }
    public GAME_MODE gameMode;

    public void IntervalChange(float speed)
    {
        //数値の反映
        dropInterval = speed;
        deleteInterval = speed / 1.5f;
        if (deleteInterval < 0.5f) { deleteInterval = 0.5f; }
    }
    public void Tetris_Setup(Vector2Int siz)
    {
        //コンポーネントを取得
        tilemap = tileObj.GetComponent<Tilemap>();
        nextTilemap = objNextBlocks.GetComponent<Tilemap>();
        //タイルを全消去
        tilemap.ClearAllTiles();
        //配列の大きさを指定
        fSize = siz;
        //カメラの調整
        //次ブロックの表示位置も調整
        Camera_Setup();
        //配列のインスタンスを生成
        tileData = new TileData[fSize.x, fSize.y];
        nextTileData = new TileData[fSize.x, fSize.y];
        //クラスのインスタンス
        dropTiles = new DropTiles();
        //ゲームの状態
        gameMode = GAME_MODE.WAIT;
        //外周部の設置
        SetWall();
        //描画
        ViewTiles();
    }
    public void GameStart()
    {
        //ゲームの状態
        gameMode = GAME_MODE.PLAY;
        //次の次の生成タイルの指定
        rndBlock = UnityEngine.Random.Range(1, 8);
        //落下オブジェクトの生成
        Generate_DropBlock();
        //描画
        ViewTiles();
        //落下開始
        DropCoroutine = Cor_DropBlocks();
        StartCoroutine(DropCoroutine);
        //描画
        ViewTiles();
    }

    //落下コルーチン
    IEnumerator Cor_DropBlocks()
    {
        var gm = this.GetComponent<GameManager>();
        yield return new WaitForSeconds(dropInterval);
        while (true)
        {
            //落下可能な場合、落下処理
            if (Check_CanMove(Vector3Int.down))
            {
                Move_DropBlocks(Vector3Int.down);
            }
            //不可能な場合、ブロックのタイプdrop→消えるかチェック→block後に再生成
            else
            {
                //ブロックのタイプdrop→block
                foreach (Vector3Int pos in dropTiles.setPos)
                {
                    tileData[pos.x, pos.y].blockType = TileType.BLOCK;
                }
                //消える列があるかチェック
                //操作不可能に設定
                gameMode = GAME_MODE.WAIT;
                //消える列の取得
                List<int> delCol = CheckDeleteTiles();
                //消える列が存在する場合、ソート命令
                if (delCol.Count > 0)
                {
                    //列消去とスコア加算
                    DeleteTiles(delCol);
                    ViewTiles();
                    gm.Score_Update(delCol.Count);
                    yield return new WaitForSeconds(deleteInterval);
                    //列ソート
                    SortTiles(delCol);
                    ViewTiles();
                    yield return new WaitForSeconds(deleteInterval);
                }
                gameMode = GAME_MODE.PLAY;
                //再生成
                Generate_DropBlock();
            }
            //描画
            ViewTiles();
            yield return new WaitForSeconds(dropInterval);
        }
    }
    //ブロック消滅判定と処理
    //列消去
    void DeleteTiles(List<int> del)
    {
        for (int y = 1; y < fSize.y - 1; y++)
        {
            bool isDel = false;
            foreach (int d in del)
            {
                if (d == y) { isDel = true; }
            }
            if (isDel)
            {
                for (int x = 1; x < fSize.x - 1; x++)
                {
                    tileData[x, y].DataReset();
                }
            }
        }
    }
    //列ソート
    void SortTiles(List<int> del)
    {
        //仮想配列にコピー
        for (int y = 1; y < fSize.y - 1; y++)
        {
            //ソートして移動する数
            int sortCount = 0;
            foreach (int d in del)
            {
                if (d < y)
                {
                    sortCount++;
                }
            }
            for (int x = 1; x < fSize.x - 1; x++)
            {
                //落下後のブロックのみコピー
                if (tileData[x, y].blockType == TileType.BLOCK)
                {
                    nextTileData[x, y - sortCount] = tileData[x, y].Clone();
                }
            }
        }
        //仮想配列から使用中の配列へ
        for (int y = 1; y < fSize.y - 1; y++)
        {
            //ソートして移動する数
            int sortCount = 0;
            foreach (int d in del)
            {
                if (d < y)
                {
                    sortCount++;
                }
            }
            for (int x = 1; x < fSize.x - 1; x++)
            {
                tileData[x, y] = nextTileData[x, y].Clone();
                //仮想配列のデータ消去
                nextTileData[x, y].DataReset();
            }
        }
    }
    //列チェック→消える列を返す
    List<int> CheckDeleteTiles()
    {
        List<int> deleteLine = new List<int>() { };
        for (int y = 1; y < fSize.y - 1; y++)
        {
            //調べるタイルのリストを作成
            List<TileType> checktiles = new List<TileType>() { };
            for (int x = 1; x < fSize.x - 1; x++)
            {
                checktiles.Add(tileData[x, y].blockType);
            }
            //リストのタイルが全部TileType.Blockかチェック
            if (IsDelete(checktiles))
            {
                deleteLine.Add(y);
            }
        }
        return deleteLine;
    }
    //指定した列が全てブロック→true
    bool IsDelete(List<TileType> checkTiles)
    {
        bool del = true;
        for (int i = 0; i < checkTiles.Count; i++)
        {
            if (checkTiles[i] == TileType.NULL)
            {
                del = false;
            }
        }
        return del;
    }

    //ブロック生成
    void Generate_DropBlock()
    {
        //中心位置の指定
        Vector3Int center = new Vector3Int(fSize.x / 2, fSize.y - 3, 0);
        //次のタイルを指定
        nextBlock = rndBlock;
        //次の次の生成タイルの指定
        rndBlock = UnityEngine.Random.Range(1, 8);
        //次の次のタイルを、next位置においてＰＬに分かるようにする
        //座標リストを取得、描画
        nextTilemap.ClearAllTiles();
        foreach (Vector3Int pos in TileCalculation.blockList(rndBlock, 0))
        {
            nextTilemap.SetTile(pos, tileChip[rndBlock]);
        }

        //タイルを生成する位置を取得
        dropTiles.SetDropData(center, nextBlock, 0);
        //ゲームオーバー判定
        bool isGameover = false;
        //設置
        foreach (Vector3Int pos in dropTiles.setPos)
        {
            //タイルが重なっているか調べる
            if (tileData[pos.x, pos.y].blockType == TileType.BLOCK)
            {
                isGameover = true;
            }
            tileData[pos.x, pos.y].blockType = TileType.DROP;
            tileData[pos.x, pos.y].tileColor = nextBlock;
        }
        //ゲームオーバー
        if (isGameover) { Process_Gameover(); }
    }
    //ゲームオーバー処理
    void Process_Gameover()
    {
        //ゲームの状態
        gameMode = GAME_MODE.GAMEOVER;
        //コルーチン停止
        StopCoroutine(DropCoroutine);
        //ゲームオーバー時の状態を描画
        ViewTiles();
        //演出
        StartCoroutine(Cor_BlockGray());
    }
    //ゲームオーバー演出
    IEnumerator Cor_BlockGray()
    {
        var gm = this.GetComponent<GameManager>();
        yield return new WaitForSeconds(0.5f);
        for (int y = 1; y < fSize.y - 1; y++)
        {
            for (int x = 1; x < fSize.x - 1; x++)
            {
                if (tileData[x, y].blockType != TileType.NULL)
                {
                    tileData[x, y].tileColor = 9;
                }
            }
            ViewTiles();
            yield return new WaitForSeconds(0.1f);
        }
        //ゲームオーバー画面の表示
        yield return new WaitForSeconds(0.5f);
        gm.panelGameover.SetActive(true);
    }

    //0123を四方に配置,0:→　1:↓　2:←　3:↑（未使用）
    public void PushMoveButton(int num)
    {
        Vector3Int moveVec = Vector3Int.zero;
        switch (num)
        {
            //右
            case 0:
                moveVec = new Vector3Int(1, 0, 0);
                break;
            //下
            case 1:
                moveVec = new Vector3Int(0, -1, 0);
                break;
            //左
            case 2:
                moveVec = new Vector3Int(-1, 0, 0);
                break;
            //上
            case 3:
                moveVec = new Vector3Int(0, 1, 0);
                break;
        }
        //ゲームの状態判定
        if (gameMode == GAME_MODE.PLAY)
        {
            //移動判定
            if (Check_CanMove(moveVec))
            {
                //移動処理
                Move_DropBlocks(moveVec);
                //描画
                ViewTiles();
            }
        }
    }
    //ブロックの移動判定
    bool Check_CanMove(Vector3Int moveVec)
    {
        //移動判定
        bool canMove = true;
        //落下中のタイル座標リスト
        foreach (Vector3Int pos in dropTiles.setPos)
        {
            Vector3Int checkPos = pos + moveVec;
            if (tileData[checkPos.x, checkPos.y].blockType == TileType.WALL
                || tileData[checkPos.x, checkPos.y].blockType == TileType.BLOCK)
            {
                canMove = false;
            }
        }
        return canMove;
    }
    //ブロックの移動
    void Move_DropBlocks(Vector3Int moveVec)
    {
        //移動位置の描画タイルの消去
        foreach (Vector3Int pos in dropTiles.setPos)
        {
            tileData[pos.x, pos.y].blockType = TileType.NULL;
        }
        //落下オブジェの情報更新
        dropTiles.MoveCenter(moveVec);
        //位置情報を描画タイルに反映
        foreach (Vector3Int pos in dropTiles.setPos)
        {
            tileData[pos.x, pos.y].blockType = TileType.DROP;
            tileData[pos.x, pos.y].tileColor = dropTiles.shapeNum;
        }
    }
    //ブロックの回転入力
    public void Push_RotationButton(int rotDirection)
    {
        //座標チェック
        int rot = (dropTiles.rotNum + rotDirection) % 4;
        //回転に制限のあるブロック用の、回転の向き補正
        /*ブロックによって回転を制限
         * 回転無し：shapeNum=2
         * 回転２：shapeNum=4,5,7
        */
        if (dropTiles.shapeNum == 2)
        {
            rot = 0;
        }
        else if (dropTiles.shapeNum == 4
            || dropTiles.shapeNum == 5
            || dropTiles.shapeNum == 7)
        {
            rot %= 2;
        }
        //中心からの位置
        List<Vector3Int> tst = TileCalculation.blockList(dropTiles.shapeNum, rot);
        //補正
        List<Vector3Int> posList = new List<Vector3Int>() { };
        foreach (Vector3Int p in tst)
        {
            posList.Add(p + dropTiles.centerPos);
        }
        bool canRot = Check_CanRotate(posList);
        if (canRot)
        {
            //オブジェを消去
            foreach (Vector3Int pos in dropTiles.setPos)
            {
                tileData[pos.x, pos.y].DataReset();
            }
            //位置情報を描画タイルに反映
            dropTiles.SetDropData(dropTiles.centerPos, dropTiles.shapeNum, rot);
            foreach (Vector3Int pos in dropTiles.setPos)
            {
                tileData[pos.x, pos.y].blockType = TileType.DROP;
                tileData[pos.x, pos.y].tileColor = dropTiles.shapeNum;
            }
            //描画
            ViewTiles();
        }

    }
    //ブロックの回転判定
    bool Check_CanRotate(List<Vector3Int> posList)
    {
        //移動判定
        bool canMove = true;
        //落下中のタイル座標リスト
        foreach (Vector3Int checkPos in posList)
        {
            //範囲外の除去。もっと見ためよくしたい
            if (checkPos.x < 0
                || checkPos.y < 0
                || checkPos.x > fSize.x - 1
                || checkPos.y > fSize.y - 1)
            {
                canMove = false;
            }
            else
            {
                if (tileData[checkPos.x, checkPos.y].blockType == TileType.WALL
                || tileData[checkPos.x, checkPos.y].blockType == TileType.BLOCK)
                {
                    canMove = false;
                }
            }
        }
        return canMove;

    }
    //リセット、壁設置
    void SetWall()
    {
        for (int y = 0; y < fSize.y; y++)
        {
            for (int x = 0; x < fSize.x; x++)
            {
                //個別にインスタンスを作成
                tileData[x, y] = new TileData();
                nextTileData[x, y] = new TileData();
                //初期化
                tileData[x, y].DataReset();
                nextTileData[x, y].DataReset();
                //外周部を壁で囲う
                if (x == 0
                    || y == 0
                    || x == fSize.x - 1
                    || y == fSize.y - 1)
                {
                    tileData[x, y].blockType = TileType.WALL;
                }
            }
        }
    }
    //全体の描画命令
    void ViewTiles()
    {
        for (int y = 0; y < fSize.y; y++)
        {
            for (int x = 0; x < fSize.x; x++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                //空っぽの場合は表示しない
                if (tileData[x, y].blockType == TileType.NULL)
                {
                    tilemap.SetTile(pos, null);
                }
                //ブロックがある場合は、色の塗り分け（タイルの設置）
                else
                {
                    //タイルの設置
                    tilemap.SetTile(pos, tileChip[tileData[x, y].tileColor]);
                }
            }
        }
    }
    //カメラ、背景設定
    void Camera_Setup()
    {
        //カメラ位置と表示範囲の修正
        //大きさ修正
        mainCamera.GetComponent<Camera>().orthographicSize = fSize.y / 2 + 1;
        //タイルの中央のセル座標を指定
        Vector3Int tileDL = Vector3Int.zero;
        Vector3Int tileUR = new Vector3Int(fSize.x - 1, fSize.y - 1, 0);
        //タイルの中央のworld座標を取得
        Vector3 tileCenter_World
            = (tilemap.GetCellCenterWorld(tileDL)
            + tilemap.GetCellCenterWorld(tileUR)) / 2;
        //タイルの中央位置に、カメラを持ってくる
        mainCamera.GetComponent<Transform>().position
            = new Vector3(tileCenter_World.x, tileCenter_World.y, -10);

        //次ブロックの位置を調整
        Vector3Int nextVec = new Vector3Int(fSize.x + 1, fSize.y - 2, 0);
        objNextBlocks.GetComponent<Transform>().position
            = tilemap.GetCellCenterWorld(nextVec);
        objNextBlocks.GetComponent<Transform>().localScale
            = new Vector3(0.5f, 0.5f, 1);
    }
}

//ブロックの種類
public enum TileType
{
    NULL,//何もない
    WALL,//壁（破壊不可能
    BLOCK,//ブロック（列を揃えれば消える
    DROP,//落下中のブロック
}
//タイルの描画と判定に使うクラス
public class TileData
{
    //種類
    public TileType blockType;
    //ブロックの色
    public int tileColor;
    //初期化
    public void DataReset()
    {
        blockType = TileType.NULL;
        tileColor = 0;
    }
    //データコピー。複製インスタンスを返す
    public TileData Clone()
    {
        return (TileData)MemberwiseClone();
    }
}
//落下中のタイル情報
public class DropTiles
{
    //落下オブジェの中心座標
    public Vector3Int centerPos;
    //落下オブジェの形状
    public int shapeNum;
    //落下オブジェの回転位置(0~3
    public int rotNum;
    //中心(0,0,0)から見た生成位置のリスト
    public List<Vector3Int> setPos = new List<Vector3Int>() { };
    //落下オブジェの情報を入力
    public void SetDropData(Vector3Int center, int type, int rot)
    {
        //リストのリセット
        setPos.Clear();
        //中心位置
        centerPos = center;
        //形状
        shapeNum = type;
        //回転位置
        rotNum = rot;
        //中心から見たブロック位置
        List<Vector3Int> pos = TileCalculation.blockList(shapeNum, rotNum);
        //中心と周囲の座標リスト→実際の配置座標
        foreach (Vector3Int p in pos)
        {
            setPos.Add(p + centerPos);
        }
    }

    //中心座標の更新
    public void MoveCenter(Vector3Int moveVec)
    {
        centerPos += moveVec;
        //付随したデータ更新
        SetDropData(centerPos, shapeNum, rotNum);
    }
}

//計算処理
public static class TileCalculation
{
    //座標の初期配置リスト
    public static List<Vector3Int> blockList(int shapeNum, int rotNum)
    {
        List<Vector3Int> pos = new List<Vector3Int>() { };

        switch (shapeNum)
        {
            //テスト用
            case 0:
                /* 
                 * □
                 */
                pos.Add(new Vector3Int(0, 0, 0));
                break;
            case 1:
                /* 
                 * ■
                 * ■□■
                 */
                pos.Add(new Vector3Int(-1, 1, 0));
                pos.Add(new Vector3Int(-1, 0, 0));
                pos.Add(new Vector3Int(0, 0, 0));
                pos.Add(new Vector3Int(1, 0, 0));
                break;
            case 2:
                /* 
                 * ■■
                 * □■
                 */
                pos.Add(new Vector3Int(1, 1, 0));
                pos.Add(new Vector3Int(1, 0, 0));
                pos.Add(new Vector3Int(0, 1, 0));
                pos.Add(new Vector3Int(0, 0, 0));
                break;
            case 3:
                /* 
                 * 　　■
                 * ■□■
                 */
                pos.Add(new Vector3Int(-1, 0, 0));
                pos.Add(new Vector3Int(0, 0, 0));
                pos.Add(new Vector3Int(1, 0, 0));
                pos.Add(new Vector3Int(1, 1, 0));
                break;
            case 4:
                /* 
                 * ■
                 * ■□
                 * 　■
                 */
                pos.Add(new Vector3Int(-1, 1, 0));
                pos.Add(new Vector3Int(-1, 0, 0));
                pos.Add(new Vector3Int(0, 0, 0));
                pos.Add(new Vector3Int(0, -1, 0));
                break;
            case 5:
                /* 
                 * 　■
                 * ■□
                 * ■
                 */
                pos.Add(new Vector3Int(0, 1, 0));
                pos.Add(new Vector3Int(0, 0, 0));
                pos.Add(new Vector3Int(-1, 0, 0));
                pos.Add(new Vector3Int(-1, -1, 0));
                break;
            case 6:
                /* 
                 * 　■
                 * ■□■
                 */
                pos.Add(new Vector3Int(-1, 0, 0));
                pos.Add(new Vector3Int(0, 0, 0));
                pos.Add(new Vector3Int(1, 0, 0));
                pos.Add(new Vector3Int(0, 1, 0));
                break;
            case 7:
                /* 
                 * ■■□■
                 */
                pos.Add(new Vector3Int(-1, 0, 0));
                pos.Add(new Vector3Int(0, 0, 0));
                pos.Add(new Vector3Int(1, 0, 0));
                pos.Add(new Vector3Int(2, 0, 0));
                break;
        }
        pos = rotPos(Vector3Int.zero, pos, rotNum);
        return pos;
    }

    //回転後の座標リストを返すだけ
    public static List<Vector3Int> rotPos(Vector3Int center, List<Vector3Int> posList, int rotCount)
    {
        List<Vector3Int> rotList = new List<Vector3Int>() { };
        rotCount = rotCount % 4;
        foreach (Vector3Int pos in posList)
        {
            Vector3Int nextP = Vector3Int.zero;
            //指定された原点から見た座標位置
            Vector3Int checkP = pos - center;
            switch (rotCount)
            {
                case 0:
                    nextP = new Vector3Int(checkP.x, checkP.y, 0);
                    break;
                case 1:
                    //９０度
                    nextP = new Vector3Int(-checkP.y, checkP.x, 0);
                    break;
                case 2:
                    //１８０度
                    nextP = new Vector3Int(-checkP.x, -checkP.y, 0);
                    break;
                case 3:
                    //２７０度
                    nextP = new Vector3Int(checkP.y, -checkP.x, 0);
                    break;
            }
            nextP += center;
            rotList.Add(nextP);
        }
        return rotList;
    }
}
