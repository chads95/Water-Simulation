using UnityEngine;
using System.Collections;

//Controlls the water
public class WaterController : MonoBehaviour
{
    public static WaterController current;

    public bool isMoving;

    //Wave height and speed
    public float scale = 0.1f;
    public float speed = 1.0f;
    //The width between the waves
    public float waveDistance = 1f;
    //Noise parameters
    public float noiseStrength = 1f;
    public float noiseWalk = 1f;

    void Start()
    {
        current = this;
    }

    void Update()
    {
        Shader.SetGlobalFloat("_WaterScale", scale);
        Shader.SetGlobalFloat("_WaterSpeed", speed);
        Shader.SetGlobalFloat("_WaterDistance", waveDistance);
        Shader.SetGlobalFloat("_WaterTime", Time.time);
        Shader.SetGlobalFloat("_WaterNoiseStrength", noiseStrength);
        Shader.SetGlobalFloat("_WaterNoiseWalk", noiseWalk);
    }

    public float GetWaveYPos(Vector3 position, float timeSinceStart)
    {
        if (isMoving)
        {
            return WaveTypes.SinXWave(position, speed, scale, waveDistance, noiseStrength, noiseWalk, timeSinceStart);
        }
        else
        {
            return 0f;
        }

        //return 0f;
    }

    public float DistanceToWater(Vector3 position, float timeSinceStart)
    {
        float waterHeight = GetWaveYPos(position, timeSinceStart);

        float distanceToWater = position.y - waterHeight;

        return distanceToWater;
    }
}