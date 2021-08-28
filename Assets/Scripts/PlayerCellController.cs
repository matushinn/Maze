using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerCellController : MonoBehaviour {

	//俯瞰視点の時の動作
	Dictionary<string, int[]> nextPosition = new Dictionary<string, int[]>()
    {
        {"up",      new int[]{ 0, 1, 0, 1 } },//グローバル座標におけるx,zの変化量
        {"down",    new int[]{ 0,-1, 0, 1 } },
        {"left",    new int[]{-1, 0, 0, 1 } },
        {"right",   new int[]{ 1, 0, 0, 1 } },
    };
    // moving direction, rotating axis
	//Player視点の時の動作
    Dictionary<string, int[]> nextAction = new Dictionary<string, int[]>()
    {
        {"up",      new int[]{0, 1, 0, 0 } },//ローカル座標におけるx,z軸の変化量
        {"down",    new int[]{0,-1, 0, 0 } },
        {"left",    new int[]{0, 0,-90, 0 } },
        {"right",   new int[]{0, 0, 90, 0 } },
    };
	//actionを使い分けるための配列
    Dictionary<string, int[]> actions;
	//actionに0を設定するとnextPosition、それ以外はnextAction
    public int ActionType
    {
        set { actions = value == 0 ? nextPosition : nextAction; }
    }

	Floor floor;
	PlayerMotion pmotion;

	//dialogのフラグ
	ModalDialog dlg;

	public float AutoMovingSpan {get; set;}
	//前に移動してから何秒たったか？
	float autoMovedTime = 0f;
	//speedUpできるために予めに保存しておく
	float autoMovingSpeed = 1.0f;

	//どのオブジェクトに当たって(string),どの関数を呼び出すか(Action)
	Dictionary<string,Action> triggerActions = new Dictionary<string,Action>();

	public void AddTriggerAction(string opponent,Action a)
	{
		triggerActions[opponent] = a;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (triggerActions.ContainsKey(other.name))
		{
			triggerActions[other.name]();
		}
	}

	// Use this for initialization
	void Start () {

		//ActionType = 0;

		floor = GameObject.Find("Floor").GetComponent<Floor>();
		pmotion = GetComponent<PlayerMotion>();

		dlg = GameObject.Find("Canvas").GetComponent<ModalDialog>();
		
	}
	
	// Update is called once per frame
	void Update () {
		if (dlg.Active == true)
        {
            return;
        }
		if (AutoMovingSpan == 0)
		{
			foreach (var elem in actions)
			{	
				if (Input.GetKeyDown(elem.Key))
				{
					Move(elem.Value);
				}
			}
		}
		//敵の場合
		else if (Time.realtimeSinceStartup > autoMovedTime + AutoMovingSpan / autoMovingSpeed)
        {
			autoMovedTime = Time.realtimeSinceStartup;
			pmotion.Unset();

			//現在の位置を保存しておく
			int[] pos = floor.blocks.GetBlockIndexXZ(GetComponent<Transform>().position);

			//移動可能な方向からランダムに選択していく
			List<string> avail = new List<string>();
			foreach (var d in nextPosition)
			{
				//blockでなかった場合
				if (floor.blocks.IsWall(pos[0]+d.Value[0],pos[1]+d.Value[1]) == false)
				{
					avail.Add(d.Key);
				}
				
			}
			//availに値が入っていた場合
			if (avail.Count != 0)
			{
				//availの中から一つ選んで進む
				Move(nextPosition[avail[UnityEngine.Random.Range(0,avail.Count)]]);
			}
		}
		//移動した先のポジションをアップデートする
		floor.UpdateObjPosition(gameObject.name, GetComponent<Transform>().position, GetComponent<Transform>().rotation);
	
		
	}
	public void SetColor(Color32 col)
	{
		GetComponent<Transform>().Find("Body").GetComponent<Renderer>().material.color = col;
	}

	public void Move(int[] pos ,Action aniComplete = null)
	{
		pmotion.Unset();
		//x,z軸の変化量が０ではない場合(移動しなければならない場合)
		if (pos[0] != 0 || pos[1] != 0)
		{
			Vector3 d = new Vector3(pos[0],0,pos[1]);
			//絶対座標の場合、回転するかどうか
			if (pos[3] == 1)
			{
				Quaternion q = new Quaternion();
				//前の角度から行きたい先の角度の計算
				q.SetFromToRotation(Vector3.forward, new Vector3(pos[0], 0, pos[1]));
                int y = Mathf.RoundToInt((q.eulerAngles.y - GetComponent<Transform>().eulerAngles.y)) % 360;
                if (y != 0)
                {
					//角度を補正する 修正
                    Turn(NormalizedDegree(y), null);
                }
			}else
			{
				d = GetComponent<Transform>().localRotation * d;
			}

			int[] index = floor.blocks.GetBlockIndexXZ(GetComponent<Transform>().position);
            Forward(index[0] + Mathf.RoundToInt(d.x), index[1] + Mathf.RoundToInt(d.z), aniComplete);

		}
		if (pos[2] != 0)
        {
            Turn(pos[2], aniComplete);
        }
	}
	//180~-180までに角度を補正する
	float NormalizedDegree(float deg)
	{
		while (deg > 180)
		{
			deg -= 360;
		}
		while (deg < -180)
		{
			deg += 360;
		}
		return deg;
	}
	void Forward(int x,int z, Action aniComplete)
	{
		//行こうとしている先が壁ではない場合
		if (floor.blocks.IsWall(x,z) == false)
		{
			//3次元の今現在の位置
			Vector3 pos0 = GetComponent<Transform>().position;
			//アニメーション終了時の位置
			Vector3 pos1 = floor.blocks.GetBlockPosition(x,z);
			//念のための上書き
			pos1.y = pos0.y;
			//p(アニメーション終了の割合),0.5s
			pmotion.Add(p =>
            {
				//割合分ポジションを変えていく
                GetComponent<Transform>().position = (pos1 - pos0) * p + pos0;
            }, 0.5f, aniComplete);
		}
	}
	//回転する角度、終了時のメソッドを引数に持つ
	void Turn(float deg, Action aniComplete)
	{
		//最初の角度を取得
		float deg0 = GetComponent<Transform>().eulerAngles.y;
		//引数との足し合わせた角度を補正する
		float deg1 = RoundDegree(deg0 + deg);
		pmotion.Add(p =>
        {
			//割合分角度を変えていく
            GetComponent<Transform>().rotation = Quaternion.Euler(0f, (deg1 - deg0) * p + deg0, 0f);
        }, 0.5f, aniComplete);
	}
	//90度の角度に補正する関数
	float RoundDegree(float deg)
	{
		return Mathf.FloorToInt((deg + 45) / 90) * 90;
	}
	//playermotionの全てをキャンセルできる関数
	public void CancelMotions()
	{
		pmotion.Cancel();
	}
}
