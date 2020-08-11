using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateExtraBoatMeshes
{

    private Transform boatTrans;

    public GenerateExtraBoatMeshes(GameObject boatObj)
    {
        //Get the transfrom
        boatTrans = boatObj.transform; 
    }

    public void DisplayMesh(Mesh mesh, string name, List<TriangleData> triangleData)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        //Build the mesh
        for(int i=0; i < triangleData.Count; i++)
        {
            //from global coord to local
            Vector3 p1 = boatTrans.InverseTransformPoint(triangleData[i].p1);
            Vector3 p2 = boatTrans.InverseTransformPoint(triangleData[i].p2);
            Vector3 p3 = boatTrans.InverseTransformPoint(triangleData[i].p3);

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

        //Add the new vetices and triangles
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();

        //Important to re calculate bounds as they are needed to cal underwater mesh
        mesh.RecalculateBounds();
    }

    //Display the mesh thats the mirror
    public void DisplayMirrorMesh(Mesh mesh, string name, List<TriangleData> triangleData)
    {
        //Move the vertices based on distance to water
        float timeSinceStart = Time.time;

        for(int i=0; i < triangleData.Count; i++)
        {
            TriangleData thisTriangle = triangleData[i];

            //The vertices in TriangleData are global
            thisTriangle.p1.y -= WaterController.current.DistanceToWater(thisTriangle.p1, timeSinceStart) * 2f;
            thisTriangle.p2.y -= WaterController.current.DistanceToWater(thisTriangle.p2, timeSinceStart) * 2f;
            thisTriangle.p3.y -= WaterController.current.DistanceToWater(thisTriangle.p3, timeSinceStart) * 2f;

            //Flip the triangle because it will be inside out in the mirror
            Vector3 tmp = thisTriangle.p2;

            thisTriangle.p2 = thisTriangle.p3;

            thisTriangle.p3 = tmp;

            triangleData[i] = thisTriangle;
        }

        DisplayMesh(mesh, name, triangleData);
    }

    //Generate the foam skirt
    //intersectionVertices are in global pos
    public void GenerateFoamSkirt(Mesh mesh, string name, List<Vector3> intersectionVertices)
    {
        //clean the vertices
        List<Vector3> cleanedVertices = CleanVertices(intersectionVertices);

        //Display in which order the vertices have been added to the list

        //sort the vertices
        List<Vector3> sortedVertices = ConvexHull.SortVerticesConvexHull(cleanedVertices);

        //DisplayVertexOrder(sortedVertices)

        //Add more vertices by splitting sections that are too far away to get s mooth foam
        List<Vector3> finalVertices = AddVertices(sortedVertices);

        //DisplayVerticesOrder

        DisplayVerticesOrderHeight(finalVertices, Color.green);

        //create FOam mesh
        CreateFoamMesh(finalVertices, mesh, name);
    }

    private List<Vector3> CleanVertices(List<Vector3> intersectionVertices)
    {
        List<Vector3> cleanedVertices = new List<Vector3>();

        for (int i =0; i< intersectionVertices.Count; i++)
        {
            bool hasFoundNearbyVertice = false;

            for(int j = 0; j < cleanedVertices.Count; j++)
            {
                if(Vector3.SqrMagnitude(cleanedVertices[j] - intersectionVertices[i]) < 0.1f)
                {
                    hasFoundNearbyVertice = true;

                    break;
                }
            }

            if(!hasFoundNearbyVertice)
            {
                cleanedVertices.Add(intersectionVertices[i]);
            }
        }

        return cleanedVertices;
    }

    //Add more vertices bt splitting sections that are to far away
    private List<Vector3> AddVertices(List<Vector3> sortedVertices)
    {
        List<Vector3> finalVertices = new List<Vector3>();

        float distBetweenNewVertices = 4f;

        for(int i =0; i < sortedVertices.Count; i ++)
        {
            int lastVertPos = i - 1;

            if (lastVertPos < 0)
            {
                lastVertPos = sortedVertices.Count - 1;
            }

            Vector3 lastVert = sortedVertices[lastVertPos];
            Vector3 thisVert = sortedVertices[i];

            float distance = Vector3.Magnitude(thisVert - lastVert);

            Vector3 dir = Vector3.Normalize((thisVert - lastVert));

            int newVertices = Mathf.FloorToInt(distance / distBetweenNewVertices);

            //Add the new vertices
            finalVertices.Add(lastVert);

            for(int j = 1; j < newVertices; j++)
            {
                Vector3 newVert = lastVert + j * dir * distBetweenNewVertices;

                finalVertices.Add(newVert);
            }
        }

        //Add the last vertex
        finalVertices.Add(sortedVertices[sortedVertices.Count - 1]);

        //Make sure all the vertice are above the water
        float timeSinceStart = Time.time;

        for(int i=0; i < finalVertices.Count; i++)
        {
            Vector3 thisVertice = finalVertices[i];

            thisVertice.y = WaterController.current.GetWaveYPos(thisVertice, timeSinceStart);

            thisVertice.y += 0.1f;
            finalVertices[i] = thisVertice;
        }
        return finalVertices;
    }

    //Create the foam mesh
    private void CreateFoamMesh(List<Vector3> finalVertices, Mesh mesh, string name)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        //how far from the boat is the foam extrudued
        float foamSize = 2f;
        float timeSinceStart = Time.time;


        Vector3 TL = finalVertices[finalVertices.Count - 1];
        Vector3 TR = finalVertices[0];

        Vector3 vecBetween = Vector3.Normalize(TR - TL);

        Vector3 normal = new Vector3(vecBetween.z, 0f, -vecBetween.x);

        //Left side
        Vector3 vecBetweenLeft = Vector3.Normalize(TL - finalVertices[finalVertices.Count - 2]);

        Vector3 normalLeft = new Vector3(vecBetweenLeft.z, 0f, -vecBetween.x);

        Vector3 avrageNormalLeft = Vector3.Normalize((normalLeft + normal) * 0.5f);

        //Move the vertex along the avrage normal
        Vector3 BL = TL + avrageNormalLeft * foamSize;

        //Move the outer part of the foam with the wave
        BL.y = WaterController.current.GetWaveYPos(BL, timeSinceStart);

        //From global coorinates to local 
        Vector3 TL_local = boatTrans.InverseTransformPoint(TL);
        Vector3 BL_local = boatTrans.InverseTransformPoint(BL);

        vertices.Add(TL_local);
        vertices.Add(BL_local);

        uvs.Add(new Vector2(0f, 0f));
        uvs.Add(new Vector2(0f, 1f));

        //Main Loop
        for(int i = 0; i < finalVertices.Count; i++)
        {
            //Right side
            int rightPos = i + 1;
            if(rightPos > finalVertices.Count - 1)
            {
                rightPos = 0;
            }

            Vector3 vecBetweenRight = Vector3.Normalize(finalVertices[rightPos] - TR);

            Vector3 normalRight = new Vector3(vecBetweenRight.z, 0f, -vecBetweenRight.x);

            Vector3 avrageNormalRight = Vector3.Normalize((normalRight + normal) * 0.5f);

            //Move the vertex along the avrage normal
            Vector3 BR = TR + avrageNormalRight * foamSize;

            //Move the outer part of the foam with the wave
            BR.y = WaterController.current.GetWaveYPos(BR, timeSinceStart);

            //Move the outer part of the foam with the wave
            BR.y = WaterController.current.GetWaveYPos(BR, timeSinceStart);

            //From global coordinates to local coordinates
            Vector3 TR_local = boatTrans.InverseTransformPoint(TR);
            Vector3 BR_local = boatTrans.InverseTransformPoint(BR);

            vertices.Add(TR_local);
            vertices.Add(BR_local);

            uvs.Add(new Vector2(1f, 0f));
            uvs.Add(new Vector2(1f, 1f));

            //TL-BR-BL
            triangles.Add(vertices.Count - 4);
            triangles.Add(vertices.Count - 1);
            triangles.Add(vertices.Count - 3);
            //TL-TR-BR
            triangles.Add(vertices.Count - 4);
            triangles.Add(vertices.Count - 2);
            triangles.Add(vertices.Count - 1);

            //Update for the next iteration

            //Update the normal and the cornors for the next iteration
            normalLeft = normal;

            normal = normalRight;

            avrageNormalLeft = avrageNormalRight;

            TL = TR;
            TR = finalVertices[rightPos];
        }

        //Remove the old mesh
        mesh.Clear();

        //Give the mesh  name
        mesh.name = name;

        //Add the new vertices and triangles
        mesh.vertices = vertices.ToArray();

        mesh.triangles = triangles.ToArray();

        mesh.uv = uvs.ToArray();
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }

    //Display in which order the vertices have been added to list by
    //connecting them with a line

    private void DisplayVerticesOrder(List<Vector3> verticesList, Color color)
    {
        //A line connecting all vertices
        float height = 0.5f;
        for(int i=0; i < verticesList.Count; i++)
        {
            Vector3 start = verticesList[i] + Vector3.up * height;

            //Connect the end with the start
            int endPos = i + 1;

            if(i == verticesList.Count - 1)
            {
                endPos = 0;
            }

            Vector3 end = verticesList[endPos] + Vector3.up * height;

            Debug.DrawLine(start, end, color);
        }
    }

    //Display the which order the vertices have been added to a list by
    //drawing a line form their cooridinates, and the height if based on there position in the list

    private void DisplayVerticesOrderHeight(List<Vector3> verticesList, Color color)
    {
        float length = 0.1f;
        for (int i = 0; i < verticesList.Count; i++)
        {
            Debug.DrawRay(verticesList[i], Vector3.up * length, color);

            //So we can see the sorting order
            length += 0.2f;
        }
    }
}
