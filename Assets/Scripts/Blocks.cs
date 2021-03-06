using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


//ブロック全体を管理するクラス
//BlockObjは個々のブロックを管理するインナークラス
public class Blocks {
	//一つ一つのブロックの場所を保存するクラス
	public class BlockObj
	{
		public BlockObj(int x, int z, GameObject b)
		{
			this.X = x;
			this.Z = z;
			this.Block = b;
		}
		public int X {get; private set;}
		public int Z {get; private set;}
		//その位置にブロックが存在するのか？
		public GameObject Block {get; set; }
	}

	//これでブロックを作る
	GameObject prefab;
	//floorの情報の保持
	Transform floor;
	int width;
	int height;
	BlockObj[] blocks;
	//必要に応じてブロックの情報をintの配列にコピーする
	int[] map;
	//コピーの必要があるかないか?
	bool remap;
	//一つ一つのブロックのサイズの保存
	Vector3 blockSize;
	//ブロックのセーブのkeyName
	string prefsName;

	//これらを保存するためのコンストラクタ
	public Blocks(GameObject prefab,Transform floor, int dx, int dz, string prefsName)
	{
		this.prefab = prefab;
		this.floor = floor;
		this.width = dx;
		this.height = dz;
		this.prefsName = prefsName;
		this.blockSize = prefab.GetComponent<Transform>().localScale;

		//blocksの初期化
		//10*10分作る
		blocks = new BlockObj[width * height];
		map = new int[blocks.Length];
		//Selectを使うことで、indexをつけることができる。
		foreach (var item in blocks.Select((v,i) => new {v,i}))
		{
			blocks[item.i] = new BlockObj(i2x(item.i),i2z(item.i),null);

		}
	}

	public void Init(Dictionary<string,int[]> objPositions)
	{
		//int配列にして読み込む
		int[] mapv = PlayerPrefs.GetString(prefsName).Split(',').Where(s => s.Length != 0).Select(s => int.Parse(s)).ToArray();
		//ブロックの復活
        foreach (var item in blocks.Select((v, i) => new { v, i }))
        {
			//indexの計算
            int x = i2x(item.i);
            int z = i2z(item.i);

			//objPositionsのstart,goal,player,enemyの位置と被っているか否か？
            bool b0 = objPositions.Any(i => i.Value[0] == x && i.Value[1] == z) == false;
			//b0かつmapvが-1(壁作成)ならば、createBlock
            bool b1 = b0 && (item.i < mapv.Length ? mapv[item.i] == -1 : false);
            if (b1)
            {
                CreateBlock(x, z);
            }
        }
	}

	//インデックスから横と縦を計算する
	//縦と横からインデックスを計算する
	//いろんなパターンに合わせて関数を作る
	public int i2x(int i)
	{
		return i % height;
	}

	public int i2z(int i)
	{
		return i / width;
	}

	public int[] i2xz(int i)
	{
		return new int[] { i2x(i), i2z(i) };
	}
	public int xz2i(int[] xz)
	{
		return xz2i(xz[0], xz[1]);
	}
	public int xz2i(int x,int z)
	{
		return x + z * width;
	}

	public BlockObj Find(GameObject obj)
	{
		//blocksの中のBlockがobjと一致しているかどうか、成功したらそのクラスを返す
		return Array.Find<BlockObj>(blocks, x => x.Block == obj);
	}

	//3次元の位置からブロックのセルの位置を検索する関数
	public int[] GetBlockIndexXZ(Vector3 pos)
    {
		//それぞれの数字を配列に格納する。
        int[] index = new int[] {
            Mathf.FloorToInt(( ( pos.x - (floor.position.x - floor.localScale.x/2f) ) * width / floor.localScale.x )),
            Mathf.FloorToInt( ( pos.z - (floor.position.z - floor.localScale.z/2f) ) * height / floor.localScale.z ),
        };
        return index;
    }
    public int GetBlockIndex(Vector3 pos)
    {
        return xz2i(GetBlockIndexXZ(pos));
    }
	//indexのx,zを指定して、save(作られたブロックを保存するかどうか)
	public void CreateBlock(int x, int z, bool save = true)
    {
		//blocksのindexのBlockにprefabをInstantiateする
        blocks[xz2i(x, z)].Block = UnityEngine.Object.Instantiate(prefab, GetBlockPosition(x, z), Quaternion.identity);
        remap = true;
        if (save)
        {
            SavePrefs();
        }
    }
	
	//ブロックのインデックスから３次元の座標を取得する
	public Vector3 GetBlockPosition(int index)
    {
        return GetBlockPosition(i2x(index), i2z(index));
    }
    public Vector3 GetBlockPosition(int iX, int iZ)
    {
        return new Vector3(
            iX * floor.localScale.x / width + (floor.position.x - floor.localScale.x / 2f) + blockSize.x / 2f,
            floor.position.y + floor.localScale.y / 2f + blockSize.y / 2f,
            iZ * floor.localScale.z / height + (floor.position.z - floor.localScale.z / 2f) + blockSize.z / 2f
            );
    }
	public void RemoveBlock(int x, int z, bool save = true)
    {
        RemoveBlock(blocks[xz2i(x, z)], save);
    }
    public void RemoveBlock(BlockObj obj, bool save = true)
    {
        UnityEngine.Object.Destroy(obj.Block);
        obj.Block = null;
        remap = true;
        if (save)
        {
            SavePrefs();
        }
    }

	public void SavePrefs()
    {
        GetMap();
		//一つのstringとして保存　例:"1,-1,1"
        PlayerPrefs.SetString(prefsName, string.Join(",", map.Select(x => x.ToString()).ToArray()));
        PlayerPrefs.Save();
    }
    public void DeletePrefs()
    {
        PlayerPrefs.DeleteKey(prefsName);
    }

    public int[] GetMap()
    {
        if (remap)
        {
			//indexを使えるforeach
            foreach (var item in blocks.Select((v, i) => new { v, i }))
            {
				//blockがない場合1,ある場合-1でmapを更新する
                map[xz2i(item.v.X, item.v.Z)] = (item.v.Block == null ? 1 : -1);
            }
            remap = false;
        }
        return map;
    }
	//全てのブロックに関して、メソッドfを実行する
	public bool All(System.Func<int, int, bool> f)
    {
        return blocks.Select((v, i) => new { v, i }).All(item => { return f(i2x(item.i), i2z(item.i)); });
    }
	//index x,zがmapの範囲内であるかどうかの判定の関数
    public bool IsIn(int x, int z)
    {
        return x >= 0 && x < width && z >= 0 && z < height;
    }
	//x,zの壁があるかの判定
    public bool IsWall(int x, int z)
    {
        GetMap();
        return IsIn(x, z) == false || map[xz2i(x, z)] == -1;
    }
	//経路が配列に入る
	/*
	public List<int> Solve(int start, int end)
	{
		return Solver.Solve(GetMap(), width, start, end);

	}
	*/


}
