using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// demo使用，分配大量的活动的角色
/// </summary>
public class SpawnManager : MonoBehaviour
{

    #region 字段

    public GameObject[] spawnPrefab;
    public int gridWidth;
    public int gridHeight;

    #endregion


    #region 方法


    void Start()
    {
        for (int i = 0; i < gridWidth; i++)
        {
            for (int j = 0; j < gridHeight; j++)
            {
                int index = Random.Range(0, spawnPrefab.Length);
                GameObject go = Instantiate<GameObject>(spawnPrefab[index], new Vector3(i * 2, 0, j * 2), Quaternion.identity);
                float r = Random.Range(0.5f, 1.0f);
                go.transform.localScale = r * Vector3.one;
            }
        }
    }


    #endregion

}
