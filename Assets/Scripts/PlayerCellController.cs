using System.Collections;
using System.Collections.Generic;
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

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
