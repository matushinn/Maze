using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Floor: MonoBehaviour {

	public GameObject blockPreb;
	public Blocks blocks;
	int dx = 10;
	int dz = 10;
	//Transformの初期化
	Transform floor;
	
	string playerName = "Player";
	string enemyName = "Enemy";
	string goalName = "Goal";
	string startName = "Start";

	//floorのポジションの配列
	Dictionary<string,int[]> objPositions = new Dictionary<string,int[]>();
	
	// Use this for initialization
	void Start () {
		floor = GetComponent<Transform>();

		// Object start position
        objPositions[playerName] = new int[] { 0, 0 };
        objPositions[startName] = new int[] { 0, 0 };
        objPositions[goalName] = new int[] { dx - 1, dz -1 };
        objPositions[enemyName] = new int[] { Mathf.RoundToInt(dx/2), Mathf.RoundToInt(dz/2) };
		
		//create blocks
		//block size,instance
		blockPreb.GetComponent<Transform>().localScale =
            new Vector3(floor.localScale.x / dx, 1f, floor.localScale.z / dz);
        blocks = new Blocks(blockPreb, floor,dx,dz,"map");
		//どこにそれぞれがポジションしているか確認
        blocks.Init(objPositions);

		//Goal
		GameObject goal = GameObject.Find(goalName);
		goal.name = goalName;
		goal.GetComponent<Transform>().position = blocks.GetBlockPosition(objPositions[goalName][0],objPositions[goalName][1]);
		
		//Walls
		//ブロックのサイズを保存しておく
		Vector3 scale = blockPreb.GetComponent<Transform>().localScale;
        for (int angle = 0; angle < 360; angle += 90)
        {
            float x = Mathf.Cos(Mathf.Deg2Rad * angle);
            float z = Mathf.Sin(Mathf.Deg2Rad * angle);

            blockPreb.GetComponent<Transform>().localScale = new Vector3(
                Mathf.RoundToInt(z * 10) == 0 ? 0.01f : floor.localScale.x,
                scale.y,
                Mathf.RoundToInt(x * 10) == 0 ? 0.01f : floor.localScale.z
                );

            float px = x * floor.localScale.x / 2f;
            float pz = z * floor.localScale.z / 2f;
            float py = floor.localScale.y / 2f + floor.position.y + scale.y / 2f;
            Instantiate(blockPreb, new Vector3(px, py, pz), Quaternion.identity);
        }
        blockPreb.GetComponent<Transform>().localScale = scale;


	}
	
	// Update is called once per frame
	void Update () {
		
		//左右のマウスクリックを同時にチェックする
		int i = Enumerable.Range(1, 2).FirstOrDefault(v => Input.GetMouseButtonDown(v - 1));
		//どちらかのマウスがクリックされていたら？
		if (i != 0)
		{
			/*
			Rayがブロックに当たる場合は、そのブロックを削除する。
			Rayが床に当たる場合は、その場所にブロックを作成する。
			*/
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            RaycastHit hit = new RaycastHit();
			
			/*
			ray.origin rayのスタート場所(カメラの場所)
			ray.direction マウスのポジションの方向に向かっていく。
			hit ここにどのオブジェクトがヒットしたのか？
			Mathf.Infinity rayの長さで無限大の長さでrayを発射する。
			*/
			//rayが何かしらのオブジェクトに当たった場合
			
			if (Physics.Raycast(ray.origin, ray.direction, out hit, Mathf.Infinity))
			{
				//blocksの一覧に衝突したgameObjectがあるかどうか、そのブロックをtargetに保存
				Blocks.BlockObj target = blocks.Find(hit.collider.gameObject);
				//ブロックにぶつかり、右クリックの場合は削除
				if (i == 2 && target != null)
				{
					blocks.RemoveBlock(target);
				}
				//floorのgameObjectと衝突したものが同じだった場合 
				else if (i == 1 && gameObject == hit.collider.gameObject)
				{
					//hit.pointを変換して、そこにブロックを作る。
					int[] index = blocks.GetBlockIndexXZ(hit.point);
					blocks.CreateBlock(index[0],index[1]);
				}
				
			}
		}
	}
}
