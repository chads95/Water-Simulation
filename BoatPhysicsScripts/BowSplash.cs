using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BowSplash : MonoBehaviour
{
    public Transform sphere;
    public Transform splashTop;
    public Transform splashBotom;
    public ParticleSystem splashParticalSystem;

    private Vector3 lastPos;

    void Update()
    {
        //Debug bt drawing a line between the top and bottom
        Debug.DrawLine(splashBotom.position, splashTop.position, Color.blue);

        //Whats the position of the sphere
        //Positive is above the water, neative below water
        float bottomDistToWater = WaterController.current.DistanceToWater(splashBotom.position, Time.time);

        float topDistToWater = WaterController.current.DistanceToWater(splashTop.position, Time.time);

        //Only add foam if one is above the water and outher is below
        if(topDistToWater > 0f && bottomDistToWater < 0f)
        {
            Vector3 H = splashTop.position;
            Vector3 M = splashBotom.position;

            float h_M = bottomDistToWater;
            float h_H = topDistToWater;

            Vector3 MH = H - M;

            float t_M = -h_M / (h_H - h_M);

            Vector3 MI_M = t_M * MH;

            //This is the position where the water is intersecting with the line
            Vector3 I_M = MI_M + M;

            //Move the sphere to this position
            sphere.position = I_M;

            //Add foam if the boat is moving down into the water
            if(I_M.y < lastPos.y)
            {
                //Align the ps along the line
                splashParticalSystem.transform.LookAt(splashTop.position);

                if(!splashParticalSystem.isPlaying)
                {
                    splashParticalSystem.Play();
                }
            }
            else
            {
                splashParticalSystem.Stop();
            }

            lastPos = I_M;
        }
        else
        {
            splashParticalSystem.Stop();
            //Debug.Log("not adding foam");
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(sphere.position, 2f);
    }
}
