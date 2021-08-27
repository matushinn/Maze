using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class PlayerMotion : MonoBehaviour {
	public class Animation
	{
		public float Duration { get; set; }
		public Action Complete { get; set; }
		Action<float> animate;

		public Animation(Action<float> a,float d,Action c = null)
		{
			this.animate = a;
			this.Duration = d;
			this.Complete = c;
		}

		//引数p(全体の何％終わったのか),
		public void Animate(float p)
		{
			animate(p);
			//アニメーションが終了した場合、Complete()
			if (p >= 1.0f && Complete != null)
			{
				Complete();
			}
		}
	}

	//animationクラスの管理、保存場所
	List<Animation> animations = new List<Animation>();
	//アニメーションが始まった時間
	float started_time = 0;

	//いろいろな形でPlayerMotionにアニメーションを登録することができる
	public void Add(Action<float> animate, float duration, Action complete = null)
	{
		Add(new Animation(animate,duration,complete));
	}

	public void Add(Animation[] anis)
	{
		foreach (Animation ani in anis)
		{
			Add(ani);
		}
	}

	public void Add(Animation ani)
	{
		this.animations.Add(ani);
	}

	public void Unset()
	{
		animations.ForEach(a => a.Animate(1f));
		animations.Clear();
		started_time = 0f;
	}

	public void Cancel()
	{
		animations.Clear();
		started_time = 0f;
	}

	public void Set(Action<float> animate, float duration, Action complete = null)
    {
        Unset();
        Add(new Animation(animate, duration, complete));
    }

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if (animations.Count > 0)
		{
			if (started_time == 0f)
			{
				//Unityをスタートした時間をコピーする
				started_time = Time.realtimeSinceStartup;

			}
			//どこまでアニメーションが進んだかの割合の計算
			float progress = (Time.realtimeSinceStartup - started_time) / animations[0].Duration;
			//最大でも1
			animations[0].Animate(Mathf.Min(1f, progress));
			//アニメーションが終わった場合
			if (progress >= 1.0f)
			{
				animations.RemoveAt(0);
				started_time = 0f;
			}
		}
	}
}
