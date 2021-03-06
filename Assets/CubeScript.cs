﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;


public class CubeScript : MonoBehaviour {
    public CubeType type;
    private Material material;
    public GameObject[] adjacencies = new GameObject[4];
    public CubeManager cubeManager;
    private int cubeColIndex;
    private int cubeRowIndex;
    public Matrix4x4 transformationMat;
    public bool destroyable = false;
    private GameObject lastCollidedWith = null;
    public GameObject[] shatterCacheRefs;


	public void SetType(CubeType cubeType)
	{
        this.type = cubeType;
        this.material = cubeManager.GetMaterial(cubeType);

        if (cubeType == CubeType.Glass) {
            //this.gameObject.GetComponent<MeshRenderer>().enabled = false;
            for (int i = 0; i < this.gameObject.transform.childCount; i++) {
                this.transform.GetChild(i).gameObject.SetActive(true);
            }
            foreach (MegaCacheOBJRef objRef in this.transform.GetComponentsInChildren<MegaCacheOBJRef>()) {
                objRef.animate = false;
            }
        } 



        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null) {
            renderer.material = this.material;
        }
        gameObject.GetComponent<Rigidbody>().mass = 1;
	}

    public void SetCubeIndex(int index) {
        this.cubeColIndex = index;
    }

    public int GetCubeIndex()
    {
        return this.cubeColIndex;
    }

	public void ShatterCube(Collider other)
	{
        other.gameObject.layer = 11;
        if (!(type == CubeType.Glass)) {
            return;
        }
        Vector3 collPos = other.gameObject.transform.position + gameObject.transform.forward.normalized *
                                   other.gameObject.GetComponent<SphereCollider>().radius;
        Vector3 localCollPos = transform.InverseTransformPoint(collPos);
        float x_pos = Mathf.Min(Mathf.Max(localCollPos.x, -0.8f), 0.8f);
        float y_pos = Mathf.Min(Mathf.Max(localCollPos.y, -0.8f), 0.8f);
        Vector2 projectedPos = new Vector2(x_pos, y_pos);
        cubeManager.WriteString(projectedPos.ToString("F4"));
        SetClusterAnimation(projectedPos);

	}

    public void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject == lastCollidedWith) {
            return;
        }
        lastCollidedWith = collision.gameObject;

        if (collision.gameObject.tag == "Projectile")
        {
            SetType(GetNextType());

        }
        else if (collision.gameObject.tag == "Box")
        {
            if (IsCubeBelow(gameObject, collision.gameObject))
            {
                SetAdjacency(collision.gameObject, 2);
                PropagateRowIndex();
            }
            else
            {
                SetAdjacency(collision.gameObject, 3);
                SetCubeRowIndex(collision.gameObject.GetComponent<CubeScript>().cubeRowIndex + 1);
                SetLeftRightAdjacencies();

                if (type != CubeType.Glass && destroyable)
                {
                    cubeManager.HandleCubeCollision(gameObject);
                }
                destroyable = false;
            }



           
        } else if (collision.gameObject.tag == "Ground"){
            cubeManager.SetCubeBottomRow(gameObject, cubeColIndex);
            SetCubeRowIndex(0);
            SetLeftRightAdjacencies();
            PropagateRowIndex();


            if (type != CubeType.Glass && destroyable)
            {
                cubeManager.HandleCubeCollision(gameObject);
            }
            destroyable = false;

        }
    }

    public void SetAboveDestroyable() {
        if (adjacencies[2] != null) {
            CubeScript cubeScript = adjacencies[2].GetComponent<CubeScript>();
            cubeScript.destroyable = true;
            cubeScript.SetAboveDestroyable();
        }

    }

    public int GetCubeRowIndex() {
        return cubeRowIndex;
    }

    public void SetCubeRowIndex(int rowIndex) {
        cubeRowIndex = rowIndex;
        if(cubeRowIndex > 9) {
            cubeManager.CubeAboveThreshold();
        }
    }

    public void PropagateRowIndex() {
        if (adjacencies[2] == null) return;
        CubeScript cs = adjacencies[2].GetComponent<CubeScript>();
        cs.SetCubeRowIndex(cubeRowIndex + 1);
        cs.SetLeftRightAdjacencies();
        cs.PropagateRowIndex();
    }
    //If first cube is below the second cube, return true
    public bool IsCubeBelow(GameObject cube, GameObject otherCube) {
        if (cube.transform.position.y < otherCube.transform.position.y) {
            return true;
        }

        return false;
    }

    private CubeType GetNextType() {
        switch (type)
        {
            case CubeType.Blue:
                return CubeType.Red;
            case CubeType.Red:
                return CubeType.Green;
            case CubeType.Green:
                return CubeType.Blue;
            default:
                return type;
        }
    }

    public void SetLeftRightAdjacencies() {
        GameObject left = cubeManager.GetCubeAtIndex(cubeColIndex - 1, cubeRowIndex);
        GameObject right = cubeManager.GetCubeAtIndex(cubeColIndex + 1, cubeRowIndex);
        SetAdjacency(left, 0);
        SetAdjacency(right, 1);

    }

    public void SetAdjacency(GameObject go, int index) {
        if (go == null) {
            return;
        }
        adjacencies[index] = go;
        int otherIndex;
        switch (index) {
            case 0:
                otherIndex = 1;
                break;
            case 1:
                otherIndex = 0;
                break;
            case 2:
                otherIndex = 3;
                break;
            case 3:
                otherIndex = 2;
                break;
            default:
                otherIndex = 0;
                break;
        }
        go.GetComponent<CubeScript>().adjacencies[otherIndex] = gameObject;
    }

    public GameObject GetCubeAbove() {
        return adjacencies[2];
    }

    public GameObject GetCubeAdjacency(int index) {
        return adjacencies[index];
    }

    public int GetClusterIndex(Vector2 pos) {
        string path = "Assets/Resources/clusters.txt";

        //Get data from clusters file
        string line;
        StreamReader reader = new StreamReader(path);
        int closestIndex = 0;
        int currIndex = 0;
        float minDistance = float.MaxValue;
        using (reader) {
            line = reader.ReadLine();
            while (line != null) {
                string[] vals = line.Split(',');
                float x = float.Parse(vals[0].Substring(1));
                float y = float.Parse(vals[1].Substring(0, vals[1].Length - 1));
                float distance = (x - pos[0]) * (x - pos[0]) + (y - pos[1]) * (y - pos[1]);

                if (distance < minDistance) {
                    minDistance = distance;
                    closestIndex = currIndex;
                }

                currIndex += 1;
                line = reader.ReadLine();
            } 
        }
        reader.Close();
        return closestIndex;
    }

    public void SetClusterAnimation(Vector2 pos) {
        //Add array of refs and obj to public params
        int clusterIndex = GetClusterIndex(pos);
        //int clusterIndex = 2;
        GameObject shatterAnimation = cubeManager.ShatterCacheObjs[clusterIndex];
        MegaCacheOBJ shatterMCO = shatterAnimation.GetComponent<MegaCacheOBJ>();

        MegaCacheOBJRef frontFaceRef = shatterCacheRefs[0].GetComponent<MegaCacheOBJRef>();
        frontFaceRef.SetSource(shatterMCO);
        frontFaceRef.animate = true;
        frontFaceRef.loopmode = MegaCacheRepeatMode.Clamp;
        frontFaceRef.fps = 40;
                                                          
        MegaCacheOBJRef backFaceRef = shatterCacheRefs[1].GetComponent<MegaCacheOBJRef>();
        backFaceRef.SetSource(shatterMCO);
        backFaceRef.animate = true;
        backFaceRef.loopmode = MegaCacheRepeatMode.Clamp;
        backFaceRef.fps = 40;

        StartCoroutine(Fade(30));


                                        
    }
    public void FadeSelf(int frames) {
        StartCoroutine(Fade(frames));
    }

    private IEnumerator Fade(int frames) {
        Renderer renderer = this.gameObject.GetComponent<Renderer>();
        int passes = 0;
        float origAlpha = renderer.material.color.a;
        while (passes < frames)
        {
            var color = renderer.material.color;
            color.a = 0.0f;
            float fadeSpeed = origAlpha / (float) frames;
            renderer.material.color = Color.Lerp(renderer.material.color, color, fadeSpeed);

            for (int i = 0; i < this.gameObject.transform.childCount; i++)
            {
                var go = this.transform.GetChild(i).gameObject;
                var childRender = go.GetComponent<Renderer>();
                if (go.tag == "Trigger" || childRender.material.color.a < fadeSpeed) {
                    continue;
                } 
                childRender.material.color = Color.Lerp(renderer.material.color, color, fadeSpeed * Time.deltaTime);
            }

            passes += 1;
            yield return new WaitForSeconds(0.01f);
        }

        //if (type == CubeType.Glass) {
        //    SetAboveDestroyable();
        //}
        SetAboveDestroyable();
        Destroy(gameObject);
        Destroy(this.gameObject);

    }



}
