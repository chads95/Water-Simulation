﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveTypes
{
    public static float SinXWave(
        Vector3 position,
        float speed,
        float scale,
        float waveDistance,
        float noiseStrength,
        float noiseWalk,
        float timeSinceStart)
    {
        float x = position.x;
        float y = 0f;
        float z = position.z;

        //Using only x or z will produce straight waves
        //Using only y will produce an up/down movement
        //x + y + z rolling waves
        //x * z produces a moving sea without rolling waves

        float waveType = y + x + z;

        y += Mathf.Sin((timeSinceStart * speed + waveType) / waveDistance) * scale;

        //add noise
        y += Mathf.PerlinNoise(x + noiseWalk, y + Mathf.Sin(timeSinceStart * 0.1f)) * noiseStrength;

        return y;
    }
}