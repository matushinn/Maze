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

	//playerPrefabの保存場所
	public GameObject playerPrefab;
	//スクリプト内で使うための変数
	GameObject player;

	//俯瞰視点用のカメラを保存する変数
	Camera birdEye;
	//Player視点のカメラを保存する変数
	Camera playersEye;
	//開始時点でのどちらの視点のカメラなのかの保持
	public bool start_bird_view;

	//timertext保存の変数
	GameObject timerText;
	float timer = 0;
	
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

		//player,enemy
		new string[] {playerName,enemyName}.Select((v,i) => new {v,i}).All(
			item =>
			{
				GameObject obj = Instantiate(playerPrefab);
				obj.name = item.v;
				Transform transform = obj.GetComponent<Transform>();
				Vector3 p = blocks.GetBlockPosition(objPositions[item.v][0], objPositions[item.v][1]);
				//y軸だけは自前で計算、スライドあり
                p.y = floor.localScale.y / 2f + floor.position.y + transform.localScale.y * transform.Find("Body").localScale.y / 2f;
                transform.position = p;
                PlayerCellController ctrl = obj.GetComponent<PlayerCellController>();

				//player専用の処理
				if (item.v == playerName)
				{
					player = obj;
					//自動的に移動する距離(0は移動しない)
                    ctrl.AutoMovingSpan = 0f;
					//Goalに当たった時の処理
					ctrl.AddTriggerAction(goalName, () => {
						//全てのモーションをキャンセル
                        ctrl.CancelMotions();
                        //dlg.DoModal(name => { }, timer.ToString("0.0"));
                        timer = 0.0f;

						//初期値にpositionを戻す
                        transform.position = blocks.GetBlockPosition(objPositions[startName][0], objPositions[startName][1]);
						//向いている向きも初期状態に戻す
                        transform.localRotation = Quaternion.identity;

                        //audio_source_effects.PlayOneShot(audio_goal);
                    });

				}
				else if (item.v == enemyName)
				{
					ctrl.AutoMovingSpan = 5f;
                    ctrl.SetColor(new Color32(165, 35, 86, 255));

					
				}
				return true;
			}
		);

		//視点カメラの登録
		birdEye = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();
		birdEye.enabled = false;
		playersEye = player.GetComponent<Transform>().Find("Camera").GetComponent<Camera>();
		playersEye.enabled = true;
		//視点が変わった時に、それに適した動作方法を設定する
		SetPlayerActionType();
		//フラグがtrueならカメラの視点を変える
		if (start_bird_view == true)
		{
			ChangeCamera();
		}
		
		//timertextの更新
		timerText = GameObject.Find("TimerText");

	}

	void SetPlayerActionType()
	{
		//birdEye true 0 flase 1
		player.GetComponent<PlayerCellController>().ActionType = birdEye.enabled ? 0:1;
	}
	//カメラの視点を変えて,actiontypeも設定する関数
	public void ChangeCamera()
	{
		birdEye.enabled = !birdEye.enabled;
		playersEye.enabled = !playersEye.enabled;
		SetPlayerActionType();
	}
	//player,enemyの現在の位置を取得できる管理する関数、中で変数の中身を上書きしていく。
	public void UpdateObjPosition(string name, Vector3 pos, Quaternion rot)
    {
        int[] index = blocks.GetBlockIndexXZ(pos);
		
        objPositions[name] = index;
    }
	
	// Update is called once per frame
	void Update () 
	{
		//俯瞰視点の場合
		if (birdEye.enabled == true)
		{
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
		timer += Time.deltaTime;
		timerText.GetComponent<Text>().text = timer.ToString("0.0");
	}
}
