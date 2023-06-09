using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.SceneManagement;

public class TrackGenerator : MonoBehaviour
{//generates a track
    [SerializeField]
    bool testing;

    public float _trackLength;

    [SerializeField]
    GameObject _gameManager;

    [Header("Track Parts")]
    [SerializeField]
    Transform _startingCP;//starting line, placed in mannually
    [SerializeField]
    GameObject _finishLine;//the last line, Spawned in automatically
    [SerializeField]
    GameObject[] TrackPartsToSpawn;//list of all track varieties

    public List<GameObject> ActiveTrackPartsList = new List<GameObject>();

    NPC_TargetPoints npc_TargetPoints;

    [SerializeField]
    GameObject[] NPCPacks;
    float _NPCBatchCount;

    // Start is called before the first frame update

   
    private void Awake()
    {
        if (npc_TargetPoints == null)
        {
            npc_TargetPoints = FindObjectOfType<NPC_TargetPoints>();
        }

        if (!testing)
        {
            //player sets track length in Start scene
            _trackLength = WorldStats._finalTrackLength;
        }
        

        //amount of NPCs that will be in race, set at start scene//set by factor of 5
        _NPCBatchCount = WorldStats._finalNPCCount / 5;

        for(int x= 0; x < _NPCBatchCount; x++)
        {
            //Debug.Log("Spawning ghosts");
            NPCPacks[x].SetActive(true);
        }

        //Needs to be called in awake, since it assigns target points//else NPCs do not read target points
        GenerateTrack();
       

    }

    private void Update()
    {
        //for developer testing
        /*if (restart)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }*/
    }



    void GenerateTrack()
    {
        //clears list
        ActiveTrackPartsList.Clear();

        //counters for curve tracks/so that track does not loop back and interesect
        int rightCurveCounter = 0;
        int leftCurveCounter = 0;

        //spawn tracks
        for (int x = 0; x < _trackLength + 1; x++)
        {
           
            // pick a random track part from array
            int delta = Random.Range(0, TrackPartsToSpawn.Length);



            //check how many curves have been spawned back to back//avoids circling back and Intersecting 
            if (delta > 0 && delta < 3 && x > 0)
            {
                // 1 is right curve index// 2 is left curve index
                switch (delta)
                {
                    case 1:
                        if(rightCurveCounter < 1)
                        {//allow right curve spawning//reset left curve counter
                            Debug.Log("Allowing right");
                            rightCurveCounter++;
                            leftCurveCounter= 0;
                            break;
                        }
                        else
                        {//if Previous  was a right curve//spawn left curve instead

                            delta = 2;
                            leftCurveCounter++;
                            rightCurveCounter = 0;
                            break;
                        }
                        
                        
                    case 2:
                        if (leftCurveCounter < 1)
                        {//allow left curve spawning//reset right curve counter
                            Debug.Log("Allowing left");
                            leftCurveCounter++;
                            rightCurveCounter = 0;
                            break;
                        }
                        else
                        {//if Previous was a left curve//spawn right curve instead
                            delta = 1;
                            rightCurveCounter++;
                            leftCurveCounter = 0;
                            break;
                        }
                        
                }


            }

            //will always start with a striaght
            if ( x == 0)
            {
                delta= 0;

                //object spawned is assigned to a variable in order to assign it to list
                var trackPart = Instantiate(TrackPartsToSpawn[delta], _startingCP.position, _startingCP.rotation);
                ActiveTrackPartsList.Add(trackPart);

                AssignTargetPoints(trackPart.transform);

            }else if(x >= _trackLength)//check if end of designated length, if so spawn in the finsih line
            {
                Transform conectionPoint = ActiveTrackPartsList[x - 1].gameObject.transform.GetChild(0);

                Debug.Log("Finished track");
                SpawnFinishLine(conectionPoint);
                //Done with spawning, break from for loop
                break;
                
            }
            else
            {
                //get the connection point of the preicous track//will always be the first child
                Transform conectionPoint = ActiveTrackPartsList[x - 1].gameObject.transform.GetChild(0);

                //if nothing is in front and track to spawn is a striaght, spawn that track
                if(!CheckCollisionWithTrack(conectionPoint, ActiveTrackPartsList[x - 1].transform.forward))
                {
                    
                    var trackPart = Instantiate(TrackPartsToSpawn[delta], conectionPoint.position, conectionPoint.rotation);
                    ActiveTrackPartsList.Add(trackPart);

                    AssignTargetPoints(trackPart.transform);

                }
                else
                {
                    Debug.Log("Straight direction Blocked");
                    if (!CheckCollisionWithTrack(conectionPoint, ActiveTrackPartsList[x - 1].transform.right))
                    {//right side open spawn in right curve
                        
                        Debug.Log("Right Free");
                        var trackPart = Instantiate(TrackPartsToSpawn[1], conectionPoint.position, conectionPoint.rotation);
                        ActiveTrackPartsList.Add(trackPart);

                        AssignTargetPoints(trackPart.transform);
                       
                        //update curve counter
                        Debug.Log("Spawning Right curve");
                        rightCurveCounter++;
                        leftCurveCounter = 0;


                    }
                    else if(!CheckCollisionWithTrack(conectionPoint, -ActiveTrackPartsList[x - 1].transform.right))
                    {//left side open spawn in left curve
                        
                        Debug.Log("Left Free");
                        var trackPart = Instantiate(TrackPartsToSpawn[2], conectionPoint.position, conectionPoint.rotation);
                        ActiveTrackPartsList.Add(trackPart);


                        AssignTargetPoints(trackPart.transform); 
                        
                        //update curve counter
                        Debug.Log("Spawning Left curve");
                        leftCurveCounter++;
                        rightCurveCounter = 0;
                    }
                    else
                    {
                        Debug.Log("No Room, Stopping");
                        SpawnFinishLine(conectionPoint);

                        break;
                    }
                }



            }

            Debug.Log(delta);
        }
        

    }


    void AssignTargetPoints(Transform trackPart)
    {//takes in the transform of a track part, and looks at children, if child is TargetPoints, assigns it to list

        for (int j = 0; j < trackPart.childCount; j++)
        {
            //Debug.Log(j+ " / " + trackPart.childCount);
            Transform AOA = trackPart.transform.GetChild(j);
            if (AOA.CompareTag("TP"))
            {
                npc_TargetPoints.ArrayOfArrays.Add(AOA.gameObject);
            }

        }
    }


    void SpawnFinishLine(Transform conectionPoint)
    {//spawn in finihs line//set gamemanger, which holds collider to finihsline position
        var trackPart = Instantiate(_finishLine, conectionPoint.position, conectionPoint.rotation);
        ActiveTrackPartsList.Add(trackPart);

        AssignTargetPoints(trackPart.transform);

        //Transform AOA = trackpart.transform.GetChild(1);
        //npc_TargetPoints.ArrayOfArrays.Add(AOA.gameObject);

        _gameManager.transform.position= conectionPoint.position;
        _gameManager.transform.rotation = conectionPoint.rotation;
    }

    bool CheckCollisionWithTrack(Transform cp, Vector3 direction)
    {
        //Debug.Log("checking if blocked");
        Ray ray = new Ray(cp.position, direction);
        RaycastHit hit;

        return Physics.SphereCast(ray, 1f, out hit, 200, 0);
    }


    
}
