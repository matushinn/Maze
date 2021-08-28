using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ModalDialog : MonoBehaviour {

	//modalpaneのimageの保存
	GameObject modalPanel;
	//modalDialogが表示しているのか否か
	public bool Active { get; private set; }
	//modalDialogのオブジェクトの保存
	List<GameObject> gameObjects = new List<GameObject>();
	//ModalDialogを消した時に起こす処理
	Action<string> done;

	// Use this for initialization
	void Start () {
		this.Active = false;
		modalPanel = GetComponent<Transform>().Find("ModalPanel").gameObject;
		//Hierarchyの配置の場所(SiblingIndex)
		int sindex = modalPanel.GetComponent<Transform>().GetSiblingIndex();
		foreach (Transform c in GetComponent<Transform>())
		{
			//modalDialogより大きいインデックスはgameObjectとして管理する
			if (sindex <= c.GetSiblingIndex())
			{
				gameObjects.Add(c.gameObject);
			}
		}
		//gameObjectを全て削除
		Cancel();
		gameObjects.ToList().ForEach(o => {
            Button b = o.GetComponent<Button>();
			//ボタンの場合
            if (b != null)
            {
				//ボタンが押された時は,onClickedが呼ばれる
                b.onClick.AddListener(() => onClicked(b.name));
            }
        });
		
	}
	public void Cancel()
	{
		this.Active = false;
		//全てのsetActiveをfalseに設定
		gameObjects.ToList().ForEach(o => o.SetActive(false));
	}

	void onClicked(string name)
	{
		if (this.done != null)
		{
			this.done(name);
		}
		Cancel();
	}
	//daialog表示する関数(daialogが消える時に実装する(done),画面に表示する文字(text))
	public void DoModal(Action<string> done,string text = "")
	{
		//表示
		this.Active = true;
		//終了する時の処理の保存
		this.done = done;
		gameObjects.Where(o => o.name == "Text").First().GetComponent<Text>().text = text;
		gameObjects.ForEach(o => o.SetActive(true));
		//徐々に表示する
		StartCoroutine(Fade(0.1f));
	}
	IEnumerator Fade(float df)
    {
		//キャンパスの色を取得
        var c = modalPanel.GetComponent<CanvasRenderer>().GetColor();
		//最初の透明度
        c.a = df > 0 ? 0f : 1f;
        modalPanel.GetComponent<CanvasRenderer>().SetColor(c);
        for (var a = c.a; a >= 0f && a <= 1f; a += df)
        {
            c.a = a;
            modalPanel.GetComponent<CanvasRenderer>().SetColor(c);
            yield return new WaitForSeconds(0.1f);
        }
    }

	// Update is called once per frame
	void Update () {
		
	}
}
