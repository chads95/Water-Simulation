using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//Generates the mesh that's below and above the water
public class ModifyBoatMesh
{
    private Transform boatTrans;
    //Coordinates of all vertices in the original boat
    Vector3[] boatVertices;
    int[] boatTriangles;
    //The boats rigidbody
    private Rigidbody boatRB;

    private BoatPhysics boatPhysics;

    //make the transformation from local to global once
    public Vector3[] boatVerticesGlobal;
    float[] allDistancesToWater;

    //The part of the boat that's under water
    private Mesh underWaterMesh;
    public List<TriangleData> underWaterTriangleData = new List<TriangleData>();
    public List<Vector3> intersectionVerticesList; //= new List<Vector3>();

    //The part of the boat that's above water
    public List<TriangleData> aboveWaterTriangleData = new List<TriangleData>();

    private MeshCollider underWaterMeshCollider;

    //Slamming resistance forces
    public List<SlammingForceData> slammingForceData = new List<SlammingForceData>();
    public List<int> indexOfOriginalTriangle = new List<int>();
    //The total area of the entire boat
    public float boatArea;

    float timeSinceStart;

    public ModifyBoatMesh(GameObject boatObj, GameObject underWaterObj, GameObject aboveWaterObj, Rigidbody boatRB)
    {
        //Get the transform
        boatTrans = boatObj.transform;

        //Get the rigid body
        this.boatRB = boatRB;

        //Get the meshcollider
        underWaterMeshCollider = underWaterObj.GetComponent<MeshCollider>();

        //Save the mesh
        underWaterMesh = underWaterObj.GetComponent<MeshFilter>().mesh;

        //Init the arrays and lists
        boatVertices = boatObj.GetComponent<MeshFilter>().mesh.vertices;
        boatTriangles = boatObj.GetComponent<MeshFilter>().mesh.triangles;

        //The boat vertices in global position
        boatVerticesGlobal = new Vector3[boatVertices.Length];
        allDistancesToWater = new float[boatVertices.Length];

        //Setup the slamming force data
        for (int i = 0; i < (boatTriangles.Length / 3); i++)
        {
            slammingForceData.Add(new SlammingForceData());
        }

        CalculateOriginalTrianglesArea();
    }

    //Generate the underwater mesh (and the abovewater mesh)
    public void GenerateUnderwaterMesh(List<Vector3> intersectionVertices)
    {
        //Reset
        aboveWaterTriangleData.Clear();
        underWaterTriangleData.Clear();
        intersectionVertices.Clear();

        //Switch the submerged triangle area with the one in the previous time step
        for (int j = 0; j < slammingForceData.Count; j++)
        {
            slammingForceData[j].previousSubmergedArea = slammingForceData[j].submergedArea;
        }

        indexOfOriginalTriangle.Clear();

        //find the distance to water with the same time
        timeSinceStart = Time.time;


        for (int j = 0; j < boatVertices.Length; j++)
        {
            //The coordinate should be in global position
            Vector3 globalPos = boatTrans.TransformPoint(boatVertices[j]);

            //Save the global position 
            boatVerticesGlobal[j] = globalPos;

            allDistancesToWater[j] = WaterController.current.DistanceToWater(globalPos, timeSinceStart);
        }

        //Add the triangles
        AddTriangles();
        intersectionVerticesList = intersectionVertices;
    }



    //Add all the triangles that's part of the underwater and abovewater meshes
    private void AddTriangles()
    {
        //List that will store the data to sort the vertices based on distance to water
        List<VertexData> vertexData = new List<VertexData>();

        //Add fake data that will be replaced
        vertexData.Add(new VertexData());
        vertexData.Add(new VertexData());
        vertexData.Add(new VertexData());


        int i = 0;
        int triangleCounter = 0;
        while (i < boatTriangles.Length)
        {
            //Loop through the 3 vertices
            for (int x = 0; x < 3; x++)
            {
                //Save the data needed
                vertexData[x].distance = allDistancesToWater[boatTriangles[i]];

                vertexData[x].index = x;

                vertexData[x].globalVertexPos = boatVerticesGlobal[boatTriangles[i]];

                i++;
            }


            //All vertices are above the water
            if (vertexData[0].distance > 0f && vertexData[1].distance > 0f && vertexData[2].distance > 0f)
            {
                Vector3 p1 = vertexData[0].globalVertexPos;
                Vector3 p2 = vertexData[1].globalVertexPos;
                Vector3 p3 = vertexData[2].globalVertexPos;

                //Save the triangle
                aboveWaterTriangleData.Add(new TriangleData(p1, p2, p3, boatRB, timeSinceStart));

                slammingForceData[triangleCounter].submergedArea = 0f;

                continue;
            }


            //All vertices are underwater
            if (vertexData[0].distance < 0f && vertexData[1].distance < 0f && vertexData[2].distance < 0f)
            {
                Vector3 p1 = vertexData[0].globalVertexPos;
                Vector3 p2 = vertexData[1].globalVertexPos;
                Vector3 p3 = vertexData[2].globalVertexPos;

                //Save the triangle
                underWaterTriangleData.Add(new TriangleData(p1, p2, p3, boatRB, timeSinceStart));

                slammingForceData[triangleCounter].submergedArea = slammingForceData[triangleCounter].originalArea;

                indexOfOriginalTriangle.Add(triangleCounter);
            }
            else
            {
                //Sort the vertices
                vertexData.Sort((x, y) => x.distance.CompareTo(y.distance));

                vertexData.Reverse();

                if (vertexData[0].distance > 0f && vertexData[1].distance < 0f && vertexData[2].distance < 0f)
                {
                    AddTrianglesOneAboveWater(vertexData, triangleCounter);
                }
                //Two vertices are above the water, the other is below
                else if (vertexData[0].distance > 0f && vertexData[1].distance > 0f && vertexData[2].distance < 0f)
                {
                    AddTrianglesTwoAboveWater(vertexData, triangleCounter);
                }
            }

            triangleCounter += 1;
        }
    }



    //Build the new triangles where one of the old vertices is above the water
    private void AddTrianglesOneAboveWater(List<VertexData> vertexData, int triangleCounter)
    {
        //H is always at position 0
        Vector3 H = vertexData[0].globalVertexPos;

        //Left of H is M
        //Right of H is L

        //Find the index of M
        int M_index = vertexData[0].index - 1;
        if (M_index < 0)
        {
            M_index = 2;
        }

        //heights to water
        float h_H = vertexData[0].distance;
        float h_M = 0f;
        float h_L = 0f;

        Vector3 M = Vector3.zero;
        Vector3 L = Vector3.zero;

        //This means M is at position 1 in the List
        if (vertexData[1].index == M_index)
        {
            M = vertexData[1].globalVertexPos;
            L = vertexData[2].globalVertexPos;

            h_M = vertexData[1].distance;
            h_L = vertexData[2].distance;
        }
        else
        {
            M = vertexData[2].globalVertexPos;
            L = vertexData[1].globalVertexPos;

            h_M = vertexData[2].distance;
            h_L = vertexData[1].distance;
        }

        //Point I_M
        Vector3 MH = H - M;

        float t_M = -h_M / (h_H - h_M);

        Vector3 MI_M = t_M * MH;

        Vector3 I_M = MI_M + M;


        //Point I_L
        Vector3 LH = H - L;

        float t_L = -h_L / (h_H - h_L);

        Vector3 LI_L = t_L * LH;

        Vector3 I_L = LI_L + L;

   
        //2 triangles below the water  
        underWaterTriangleData.Add(new TriangleData(M, I_M, I_L, boatRB, timeSinceStart));
        underWaterTriangleData.Add(new TriangleData(M, I_L, L, boatRB, timeSinceStart));
        //1 triangle above the water
        aboveWaterTriangleData.Add(new TriangleData(I_M, H, I_L, boatRB, timeSinceStart));

        //Calculate the total submerged area
        float totalArea = BoatPhysicsMath.GetTriangleArea(M, I_M, I_L) + BoatPhysicsMath.GetTriangleArea(M, I_L, L);

        slammingForceData[triangleCounter].submergedArea = totalArea;

        indexOfOriginalTriangle.Add(triangleCounter);
        //Add 2 times because 2 submerged triangles need to connect to the same original triangle
        indexOfOriginalTriangle.Add(triangleCounter);

        intersectionVerticesList.Add(I_M);
        intersectionVerticesList.Add(I_L);
    }

    //Build the new triangles where two of the old vertices are above the water
    private void AddTrianglesTwoAboveWater(List<VertexData> vertexData, int triangleCounter)
    {
        //H and M are above the water
        //H is after the vertice that's below water, which is L
        //L is last in the sorted list
        Vector3 L = vertexData[2].globalVertexPos;

        //Find the index of H
        int H_index = vertexData[2].index + 1;
        if (H_index > 2)
        {
            H_index = 0;
        }


        //heights to water
        float h_L = vertexData[2].distance;
        float h_H = 0f;
        float h_M = 0f;

        Vector3 H = Vector3.zero;
        Vector3 M = Vector3.zero;

        if (vertexData[1].index == H_index)
        {
            H = vertexData[1].globalVertexPos;
            M = vertexData[0].globalVertexPos;

            h_H = vertexData[1].distance;
            h_M = vertexData[0].distance;
        }
        else
        {
            H = vertexData[0].globalVertexPos;
            M = vertexData[1].globalVertexPos;

            h_H = vertexData[0].distance;
            h_M = vertexData[1].distance;
        }



        //Point J_M
        Vector3 LM = M - L;

        float t_M = -h_L / (h_M - h_L);

        Vector3 LJ_M = t_M * LM;

        Vector3 J_M = LJ_M + L;


        //Point J_H
        Vector3 LH = H - L;

        float t_H = -h_L / (h_H - h_L);

        Vector3 LJ_H = t_H * LH;

        Vector3 J_H = LJ_H + L;


        //1 triangle above the water
        underWaterTriangleData.Add(new TriangleData(L, J_H, J_M, boatRB, timeSinceStart));
        //2 triangles below the water
        aboveWaterTriangleData.Add(new TriangleData(J_H, H, J_M, boatRB, timeSinceStart));
        aboveWaterTriangleData.Add(new TriangleData(J_M, H, M, boatRB, timeSinceStart));

        //Calculate the submerged area
        slammingForceData[triangleCounter].submergedArea = BoatPhysicsMath.GetTriangleArea(L, J_H, J_M);

        indexOfOriginalTriangle.Add(triangleCounter);

        intersectionVerticesList.Add(J_H);
        intersectionVerticesList.Add(J_M);
    }



    private class VertexData
    {
        //The distance to water
        public float distance;
        //store a index so we can form clockwise triangles
        public int index;
        //The global Vector3 position of the vertex
        public Vector3 globalVertexPos;
    }



    //Display the underwater or abovewater mesh
    public void DisplayMesh(Mesh mesh, string name, List<TriangleData> triangesData)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        //Build the mesh
        for (int i = 0; i < triangesData.Count; i++)
        {
            //From global coordinates to local coordinates
            Vector3 p1 = boatTrans.InverseTransformPoint(triangesData[i].p1);
            Vector3 p2 = boatTrans.InverseTransformPoint(triangesData[i].p2);
            Vector3 p3 = boatTrans.InverseTransformPoint(triangesData[i].p3);

            vertices.Add(p1);
            triangles.Add(vertices.Count - 1);

            vertices.Add(p2);
            triangles.Add(vertices.Count - 1);

            vertices.Add(p3);
            triangles.Add(vertices.Count - 1);
        }

        //Remove the old mesh
        mesh.Clear();

        //Give it a name
        mesh.name = name;

        //Add the new vertices and triangles
        mesh.vertices = vertices.ToArray();

        mesh.triangles = triangles.ToArray();

        mesh.RecalculateBounds();
    }

    //Calculate the length of the mesh that's below the water
    public float CalculateUnderWaterLength()
    {
        //Approximate the length as the length of the underwater mesh
        float underWaterLength = underWaterMesh.bounds.size.z;

        //Debug.Log(underWaterMesh.bounds.size.z);

        return underWaterLength;
    }

    private void CalculateOriginalTrianglesArea()
    {
        //Loop through all the triangles (3 vertices at a time = 1 triangle)
        int i = 0;
        int triangleCounter = 0;
        while (i < boatTriangles.Length)
        {
            Vector3 p1 = boatVertices[boatTriangles[i]];

            i++;

            Vector3 p2 = boatVertices[boatTriangles[i]];

            i++;

            Vector3 p3 = boatVertices[boatTriangles[i]];

            i++;

            //Calculate the area of the triangle
            float triangleArea = BoatPhysicsMath.GetTriangleArea(p1, p2, p3);

            //Store the area in a list
            slammingForceData[triangleCounter].originalArea = triangleArea;

            //The total area
            boatArea += triangleArea;

            triangleCounter += 1;
        }
    }
}