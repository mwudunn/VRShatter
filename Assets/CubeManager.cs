﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor;

public enum CubeType { Red, Blue, Green, Glass };
public class CubeManager : MonoBehaviour
{
    public Material blue;
    public Material red;
    public Material green;
    public Material glass;
    public Material transparent;
    public GameObject cubePrefab;
    public GameObject glassCubePrefab;
    public TextAsset textFile;
    public Game game;
    public GameObject[] ShatterCacheObjs;


    bool shouldSpawn = true;
    float gameSpeed = 9.0f;
    float zDist = 5.0f;
    int cubeSpread = 13;
    int cubeProbabilitySeed = 0;
    public GameObject[] cubeRow;
    float startTime;

    private void Start()
    {
        startTime = Time.time;
        cubeRow = new GameObject[cubeSpread];
    }

    public Material GetMaterial(CubeType type)
    {
        switch (type)
        {
            case CubeType.Blue:
                return blue;
            case CubeType.Red:
                return red;
            case CubeType.Green:
                return green;
            default:
                return transparent;
        }
    }

    public void HandleCubeCollision(GameObject cube)
    {
        List<GameObject> matchingCubes = FindMatchingCubes(cube);
        if (matchingCubes.Count >= 3)
        {
            game.AdjustScore(matchingCubes.Count);
            //Debug.Log(game.GetScore());
            while (matchingCubes.Count > 0) {
                GameObject go = matchingCubes[0];
                matchingCubes.RemoveAt(0);
                //Destroy(go);
                go.GetComponent<CubeScript>().FadeSelf(15);
            }
        }
    }

    public int GetCubeSpread() {
        return cubeSpread;
    }

    public GameObject GetCubeAtIndex(int col, int row)
    {
        if (col >= cubeSpread) col = col % cubeSpread;
        if (col < 0) col = cubeSpread + col;

        GameObject cube = cubeRow[col];
        while (cube != null && row > 0) {
            CubeScript cs = cube.GetComponent<CubeScript>();
            if (cs != null) {
                cube = cs.GetCubeAbove();
                row -= 1;
            } else {
                return null;
            }

        }
        return cube;
    }

    public int FindCubeIndex(GameObject target)
    {
        GameObject cube = cubeRow[target.GetComponent<CubeScript>().GetCubeIndex()];
        int counter = 0;
        while (cube != null && cube != target)
        {
            CubeScript cubeScript = gameObject.GetComponent<CubeScript>();
            if (cubeScript == null) {
                break;
            }
            cube = cubeScript.GetCubeAbove();
            counter++;
        }
        return counter;
    }

    public List<GameObject> FindMatchingCubes(GameObject cube)
    {
        CubeScript cubeScript = cube.GetComponent<CubeScript>();
        HashSet<GameObject> matchingCubes = new HashSet<GameObject>();
        List<GameObject> candidateCubes = new List<GameObject>();
        matchingCubes.Add(cube);

        AddRangeNonNull(candidateCubes, matchingCubes, cubeScript);

        while (candidateCubes.Count > 0)
        {
            GameObject nextCubeGO = candidateCubes[0];
            candidateCubes.RemoveAt(0);
            CubeScript nextCube = nextCubeGO.GetComponent<CubeScript>();

            if (nextCube.type == cubeScript.type)
            {
                matchingCubes.Add(nextCubeGO);
                AddRangeNonNull(candidateCubes, matchingCubes, nextCube);
            }
        }
        var arr = new GameObject[matchingCubes.Count];
        matchingCubes.CopyTo(arr);
        return new List<GameObject>(arr);
    }

    public void AddRangeNonNull(List<GameObject> candidates, HashSet<GameObject> matches, CubeScript cubeScript) {
        for (int i = 0; i < 4; i++)
        {
            GameObject go = cubeScript.GetCubeAdjacency(i);
            if (go != null && !matches.Contains(go))
            {
                candidates.Add(go);
            }
        }
    }

    public CubeType ChooseType() {
        cubeProbabilitySeed = (cubeProbabilitySeed + 1) % 10;
        switch(cubeProbabilitySeed) 
        {
            case 0:
                return SelectCube(0.1f, 0.1f, 0.1f);
            case 1:
                return SelectCube(0.1f, 0.1f, 0.1f);
            case 2:
                return SelectCube(0.2f, 0.2f, 0.2f);
            case 3:
                return SelectCube(0.2f, 0.2f, 0.2f);
            case 4:
                return SelectCube(0.2f, 0.2f, 0.2f);
            case 5:
                return SelectCube(0.25f, 0.25f, 0.25f);
            case 6:
                return SelectCube(0.25f, 0.25f, 0.25f);
            case 7:
                return SelectCube(0.3f, 0.3f, 0.3f);
            case 8:
                return SelectCube(0.4f, 0.3f, 0.3f);
            case 9:
                return SelectCube(0.3f, 0.4f, 0.3f);
            default:
                return SelectCube(0.0f, 0.0f, 0.0f);
        }  
    }

    private CubeType SelectCube(float pGreen, float pRed, float pBlue)
    {
        float fRand = Random.Range(0.0f, 1.0f);
        if (fRand >= 1.0f - pGreen)
        {
            return CubeType.Green;
        }
        else if (fRand >= 1.0f - pGreen - pRed)
        {
            return CubeType.Red;
        }
        else if (fRand >= 1.0f - pGreen - pRed - pBlue)
        {
            return CubeType.Blue;
        }
        else
        {
            return CubeType.Glass;
        }
    }

    public void Spawn()
    {
        CubeType nextType = ChooseType();
        GameObject cube;
        
        if (nextType == CubeType.Glass) {
            cube = (GameObject)Instantiate(
                glassCubePrefab);
        } else {
            cube = (GameObject)Instantiate(
                cubePrefab);
        }
        cube.GetComponent<CubeScript>().cubeManager = this;
        int index = Random.Range(0, cubeSpread);
        cube.GetComponent<CubeScript>().SetType(nextType);
        placeCube(cube, index);

    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time - startTime > 5.0f) {
            gameSpeed += 1.0f;
            startTime = Time.time;
        }
        if (shouldSpawn)
        {
            Spawn();
            shouldSpawn = false;
            StartCoroutine(wait());
        }

    }

    IEnumerator wait()
    {
        yield return new WaitForSeconds(1 / (gameSpeed / 10 ));
        shouldSpawn = true;
    }

    private void placeCube(GameObject cube, int locationIndex)
    {
        var position = cube.transform.position;
        var rotation = cube.transform.rotation;

        cube.transform.Rotate(0.0f, locationIndex * (360.0f / (cubeSpread * 2 + 1)), 0.0f);
        cube.transform.position = position + cube.transform.forward * zDist;

        //SetCubeBottomRow(cube, locationIndex);

        cube.GetComponent<CubeScript>().SetCubeIndex(locationIndex);
    }

    public void SetCubeBottomRow(GameObject cube, int locationIndex) {
        cubeRow[locationIndex] = cube;
    }


    public GameObject GetCubeButtomRow(int locationIndex) {
        return cubeRow[locationIndex];
    }

    public void WriteString(string str)
    {
        string path = "Assets/Resources/data.txt";

        //Write some text to the data.txt file
        StreamWriter writer = new StreamWriter(path, true);
        writer.WriteLine(str);
        writer.Close();

        //Re-import the file to update the reference in the editor
        //AssetDatabase.ImportAsset(path);
        //TextAsset asset = (TextAsset)Resources.Load("data");

        //Print the text from the file
        //Debug.Log(str);
    }

    public void CubeAboveThreshold() {
        game.GameOver();
    }



}
