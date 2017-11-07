/*
 * Created by jiadong chen
 * http://www.chenjd.me
 * 
 * 用来烘焙动作贴图。烘焙对象使用animation组件，并且在导入时设置Rig为Legacy
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.IO;

/// <summary>
/// 保存需要烘焙的动画的相关数据
/// </summary>
public struct AnimData
{
    #region 字段

    public int vertexCount;
    public int mapWidth;
    private Animation animation;

    public List<AnimationState> animClips;
    public string name;

    private SkinnedMeshRenderer skin;

    private Animator animator;
    public List<AnimationClip> animClipsInfo;

    #endregion

    public AnimData(Animation anim, Animator anim1, SkinnedMeshRenderer smr, string goName)
    {
        vertexCount = smr.sharedMesh.vertexCount;
        mapWidth = Mathf.NextPowerOfTwo(vertexCount);
        if (anim != null)
        {
            animation = anim;
            animClips = new List<AnimationState>(anim.Cast<AnimationState>());
        }
        else
        {
            animation = null;
            animClips = null;
        }
        if (anim1 != null)
        {
            animator = anim1;
            animClipsInfo = anim1.runtimeAnimatorController.animationClips.ToList();
        }
        else
        {
            animator = null;
            animClipsInfo = null;
        }

        skin = smr;
        name = goName;
    }

    #region 方法

    public void AnimationPlay(string animName)
    {
        this.animation.Play(animName);
    }

    public void AnimatorClipPlay(string animName)
    {
        this.animator.Play(animName);
    }

    // public void SampleAnimAndBakeMesh(ref Mesh m)
    // {
    //     this.SampleAnim();
    //     this.BakeMesh(ref m);
    // }

    public void SampleAnim()
    {
        if (this.animation != null)
        {
            this.animation.Sample();
        }

    }

    public void SampleAnim(AnimationClip clip, GameObject go, float time)
    {
        if (this.animator != null)
        {
            clip.SampleAnimation(go, time);
        }
    }

    public void BakeMesh(ref Mesh m)
    {
        if (this.skin == null)
        {
            Debug.LogError("skin is null!!");
            return;
        }

        this.skin.BakeMesh(m);
    }


    #endregion

}

/// <summary>
/// 烘焙后的数据
/// </summary>
public struct BakedData
{
    #region 字段

    public string name;
    public float animLen;
    public byte[] rawAnimMap;
    public int animMapWidth;
    public int animMapHeight;

    #endregion

    public BakedData(string name, float animLen, Texture2D animMap)
    {
        this.name = name;
        this.animLen = animLen;
        this.animMapHeight = animMap.height;
        this.animMapWidth = animMap.width;
        this.rawAnimMap = animMap.GetRawTextureData();
    }
}

/// <summary>
/// 烘焙器
/// </summary>
public class AnimMapBaker
{

    #region 字段

    private AnimData? animData = null;
    private List<Vector3> vertices = new List<Vector3>();
    private Mesh bakedMesh;

    private List<BakedData> bakedDataList = new List<BakedData>();

    private GameObject target;

    #endregion

    #region 方法

    public void SetAnimData(GameObject go)
    {
        if (go == null)
        {
            Debug.LogError("go is null!!");
            return;
        }

        target = go;
        Animation animation = go.GetComponent<Animation>();
        Animator animator = go.GetComponent<Animator>();
        SkinnedMeshRenderer smr = go.GetComponentInChildren<SkinnedMeshRenderer>();

        if ((animation == null && animator == null) || smr == null)
        {
            Debug.LogError("anim or smr is null!!");
            return;
        }
        this.bakedMesh = new Mesh();
        this.animData = new AnimData(animation, animator, smr, go.name);
    }

    public List<BakedData> Bake()
    {
        if (this.animData == null)
        {
            Debug.LogError("bake data is null!!");
            return this.bakedDataList;
        }

        //每一个动作都生成一个动作图
        if (this.animData.Value.animClips != null)
        {
            for (int i = 0; i < this.animData.Value.animClips.Count; i++)
            {
                if (!this.animData.Value.animClips[i].clip.legacy)
                {
                    this.animData.Value.animClips[i].clip.legacy = true;
                }

                BakePerAnimClip(this.animData.Value.animClips[i]);
            }
        }
        if (this.animData.Value.animClipsInfo != null)
        {
            for (int i = 0; i < this.animData.Value.animClipsInfo.Count; i++)
            {
                // if (!this.animData.Value.animClipsInfo[i].legacy)
                // {
                //     this.animData.Value.animClipsInfo[i].legacy = true;
                // }

                BakePerAnimClip(this.animData.Value.animClipsInfo[i]);
            }
        }

        return this.bakedDataList;
    }

    private void BakePerAnimClip(AnimationState curAnim)
    {
        int curClipFrame = 0;
        float sampleTime = 0;
        float perFrameTime = 0;

        curClipFrame = Mathf.ClosestPowerOfTwo((int)(curAnim.clip.frameRate * curAnim.length));
        perFrameTime = curAnim.length / curClipFrame; ;

        Texture2D animMap = new Texture2D(this.animData.Value.mapWidth, curClipFrame, TextureFormat.RGBAHalf, false);
        animMap.name = string.Format("{0}_{1}.animMap", this.animData.Value.name, curAnim.name);
        this.animData.Value.AnimationPlay(curAnim.name);

        for (int i = 0; i < curClipFrame; i++)
        {
            curAnim.time = sampleTime;

            this.animData.Value.SampleAnim();
            this.animData.Value.BakeMesh(ref this.bakedMesh);

            for (int j = 0; j < this.bakedMesh.vertexCount; j++)
            {
                Vector3 vertex = this.bakedMesh.vertices[j];
                animMap.SetPixel(j, i, new Color(vertex.x, vertex.y, vertex.z));
            }

            sampleTime += perFrameTime;
        }
        animMap.Apply();

        this.bakedDataList.Add(new BakedData(animMap.name, curAnim.clip.length, animMap));
    }

    private void BakePerAnimClip(AnimationClip curAnim)
    {
        int curClipFrame = 0;
        float sampleTime = 0;
        float perFrameTime = 0;

        curClipFrame = Mathf.ClosestPowerOfTwo((int)(curAnim.frameRate * curAnim.length));
        perFrameTime = curAnim.length / curClipFrame; ;

        Texture2D animMap = new Texture2D(this.animData.Value.mapWidth, curClipFrame, TextureFormat.RGBAHalf, false);
        animMap.name = string.Format("{0}_{1}.animMap", this.animData.Value.name, curAnim.name);
        this.animData.Value.AnimatorClipPlay(curAnim.name);

        for (int i = 0; i < curClipFrame; i++)
        {
            // curAnim.time = sampleTime;

            this.animData.Value.SampleAnim(curAnim, target, sampleTime);
            this.animData.Value.BakeMesh(ref this.bakedMesh);
            // this.animData.Value.SampleAnimAndBakeMesh(ref this.bakedMesh);

            for (int j = 0; j < this.bakedMesh.vertexCount; j++)
            {
                Vector3 vertex = this.bakedMesh.vertices[j];
                animMap.SetPixel(j, i, new Color(vertex.x, vertex.y, vertex.z));
            }

            sampleTime += perFrameTime;
        }
        animMap.Apply();

        this.bakedDataList.Add(new BakedData(animMap.name, curAnim.length, animMap));
    }

    #endregion


    #region 属性


    #endregion

}
