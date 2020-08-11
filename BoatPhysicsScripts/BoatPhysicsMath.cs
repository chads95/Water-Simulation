using UnityEngine;
using System.Collections;

//Equations that calculates boat physics forces
public static class BoatPhysicsMath
{
    //
    // Constants
    //

    //Densities [kg/m^3]

    //Fluid
    public const float RHO_WATER = 1000f;
    public const float RHO_OCEAN_WATER = 1027f;
    public const float RHO_SUNFLOWER_OIL = 920f;
    public const float RHO_MILK = 1035f;
    //Gas
    public const float RHO_AIR = 1.225f;
    public const float RHO_HELIUM = 0.164f;
    //Solid
    public const float RHO_GOLD = 19300f;

    //Drag coefficients
    public const float C_d_flat_plate_perpendicular_to_flow = 1.28f;


    //Calculate the velocity at the center of the triangle
    public static Vector3 GetTriangleVelocity(Rigidbody boatRB, Vector3 triangleCenter)
    {


        Vector3 v_B = boatRB.velocity;

        Vector3 omega_B = boatRB.angularVelocity;

        Vector3 r_BA = triangleCenter - boatRB.worldCenterOfMass;

        Vector3 v_A = v_B + Vector3.Cross(omega_B, r_BA);

        return v_A;
    }

    //Calculate the area of a triangle with three coordinates
    public static float GetTriangleArea(Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float a = Vector3.Distance(p1, p2);
        float c = Vector3.Distance(p3, p1);

        float areaSin = (a * c * Mathf.Sin(Vector3.Angle(p2 - p1, p3 - p1) * Mathf.Deg2Rad)) / 2f;

        float area = areaSin;

        return area;
    }

    //
    // Buoyancy from http://www.gamasutra.com/view/news/237528/Water_interaction_model_for_boats_in_video_games.php
    //

    //The buoyancy force so the boat can float
    public static Vector3 BuoyancyForce(float rho, TriangleData triangleData)
    {
        Vector3 buoyancyForce = rho * Physics.gravity.y * triangleData.distanceToSurface * triangleData.area * triangleData.normal;

        //The vertical component of the hydrostatic forces don't cancel out but the horizontal do
        buoyancyForce.x = 0f;
        buoyancyForce.z = 0f;

        //Check that the force is valid, such as not NaN to not break the physics model
        buoyancyForce = CheckForceIsValid(buoyancyForce, "Buoyancy");

        return buoyancyForce;
    }

    //
    // Resistance forces from http://www.gamasutra.com/view/news/263237/Water_interaction_model_for_boats_in_video_games_Part_2.php
    //

    //Force 1 - Viscous Water Resistance (Frictional Drag)
    public static Vector3 ViscousWaterResistanceForce(float rho, TriangleData triangleData, float Cf)
    {

        //tangential velocity 
        Vector3 B = triangleData.normal;
        Vector3 A = triangleData.velocity;

        Vector3 velocityTangent = Vector3.Cross(B, (Vector3.Cross(A, B) / B.magnitude)) / B.magnitude;

        //The direction of the tangential velocity (-1 to get the flow which is in the opposite direction)
        Vector3 tangentialDirection = velocityTangent.normalized * -1f;

        //Debug.DrawRay(triangleCenter, tangentialDirection * 3f, Color.black);
        //Debug.DrawRay(triangleCenter, velocityVec.normalized * 3f, Color.blue);
        //Debug.DrawRay(triangleCenter, normal * 3f, Color.white);

        Vector3 v_f_vec = triangleData.velocity.magnitude * tangentialDirection;

        //The final resistance force
        Vector3 viscousWaterResistanceForce = 0.5f * rho * v_f_vec.magnitude * v_f_vec * triangleData.area * Cf;

        viscousWaterResistanceForce = CheckForceIsValid(viscousWaterResistanceForce, "Viscous Water Resistance");

        return viscousWaterResistanceForce;
    }

    //The Coefficient of frictional resistance - belongs to Viscous Water Resistance but is same for all so calculate once
    public static float ResistanceCoefficient(float rho, float velocity, float length)
    {

        float nu = 0.000001f;

        //Reynolds number
        float Rn = (velocity * length) / nu;

        //The resistance coefficient
        float Cf = 0.075f / Mathf.Pow((Mathf.Log10(Rn) - 2f), 2f);

        return Cf;
    }

    //Force 2 - Pressure Drag Force
    public static Vector3 PressureDragForce(TriangleData triangleData)
    {


        float velocity = triangleData.velocity.magnitude;

        //A reference speed used when modifying the parameters
        float velocityReference = velocity;

        velocity = velocity / velocityReference;

        Vector3 pressureDragForce = Vector3.zero;

        if (triangleData.cosTheta > 0f)
        {

            //To change the variables real-time - add the finished values later
            float C_PD1 = DebugPhysics.current.C_PD1;
            float C_PD2 = DebugPhysics.current.C_PD2;
            float f_P = DebugPhysics.current.f_P;

            pressureDragForce = -(C_PD1 * velocity + C_PD2 * (velocity * velocity)) * triangleData.area * Mathf.Pow(triangleData.cosTheta, f_P) * triangleData.normal;
        }
        else
        {

            //To change the variables real-time - add the finished values later
            float C_SD1 = DebugPhysics.current.C_SD1;
            float C_SD2 = DebugPhysics.current.C_SD2;
            float f_S = DebugPhysics.current.f_S;

            pressureDragForce = (C_SD1 * velocity + C_SD2 * (velocity * velocity)) * triangleData.area * Mathf.Pow(Mathf.Abs(triangleData.cosTheta), f_S) * triangleData.normal;
        }

        pressureDragForce = CheckForceIsValid(pressureDragForce, "Pressure drag");

        return pressureDragForce;
    }

    //Force 3 - Slamming Force (Water Entry Force)
    public static Vector3 SlammingForce(SlammingForceData slammingData, TriangleData triangleData, float boatArea, float boatMass)
    {

        if (triangleData.cosTheta < 0f || slammingData.originalArea <= 0f)
        {
            return Vector3.zero;
        }


        //Volume of water swept per second
        Vector3 dV = slammingData.submergedArea * slammingData.velocity;
        Vector3 dV_previous = slammingData.previousSubmergedArea * slammingData.previousVelocity;

        Vector3 accVec = (dV - dV_previous) / (slammingData.originalArea * Time.fixedDeltaTime);

        //The magnitude of the acceleration
        float acc = accVec.magnitude;

        //Debug.Log(slammingForceData.originalArea);


        Vector3 F_stop = boatMass * triangleData.velocity * ((2f * triangleData.area) / boatArea);


        float p = 2f;

        float acc_max = acc;

        float slammingCheat = DebugPhysics.current.slammingCheat;

        Vector3 slammingForce = Mathf.Pow(Mathf.Clamp01(acc / acc_max), p) * triangleData.cosTheta * F_stop * slammingCheat;

        //Vector3 slammingForce = Vector3.zero;

        //Debug.Log(slammingForce);

        //The force acts in the opposite direction
        slammingForce *= -1f;

        slammingForce = CheckForceIsValid(slammingForce, "Slamming");

        return slammingForce;

    }

    //Force 2 - Residual resistance - similar to "Pressure Drag Forces" above
    public static float ResidualResistanceForce()
    {


        float residualResistanceForce = 0f;

        return residualResistanceForce;
    }

    //Force 3 - Air resistance on the part of the ship above the water
    public static Vector3 AirResistanceForce(float rho, TriangleData triangleData, float C_air)
    {

        //Only add air resistance if normal is pointing in the same direction as the velocity
        if (triangleData.cosTheta < 0f)
        {
            return Vector3.zero;
        }

        //Find air resistance force
        Vector3 airResistanceForce = 0.5f * rho * triangleData.velocity.magnitude * triangleData.velocity * triangleData.area * C_air;

        //Acting in the opposite side of the velocity
        airResistanceForce *= -1f;

        airResistanceForce = CheckForceIsValid(airResistanceForce, "Air resistance");

        return airResistanceForce;
    }

    //Check that a force is not NaN
    private static Vector3 CheckForceIsValid(Vector3 force, string forceName)
    {
        if (!float.IsNaN(force.x + force.y + force.z))
        {
            return force;
        }
        else
        {
            Debug.Log(forceName += " force is NaN");

            return Vector3.zero;
        }
    }
}