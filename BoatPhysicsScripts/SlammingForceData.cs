using UnityEngine;
using System.Collections;

public class SlammingForceData
{

    public float originalArea;
    //How much area of a triangle in the whole boat is submerged
    public float submergedArea;
    public float previousSubmergedArea;
    public Vector3 triangleCenter;
    //Velocity
    public Vector3 velocity;
    public Vector3 previousVelocity;
}