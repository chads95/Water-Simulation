using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ConvexHull
{
    public static List<Vector3>SortVerticesConvexHull(List<Vector3> unSortedList)
    {
        List<Vector3> sortedList = new List<Vector3>();

        //Find the vertices with the smallest x coord

        //inti with just the first in the list
        float smallestValue = unSortedList[0].x;
        int smallestIndex = 0;

        for(int i = 1; i < unSortedList.Count; i++)
        {
            if(unSortedList[i].x < smallestValue)
            {
                smallestValue = unSortedList[i].x;

                smallestIndex = i;
            }
            //If they are the same, choose the one with the smallest z value
            else if(unSortedList[i].x == smallestValue)
            {
                if(unSortedList[i].z < unSortedList[smallestIndex].z)
                {
                    smallestIndex = i;
                }
            }
        }

        //Remove the smallest value from the list and add it as the first coord on the hull
        sortedList.Add(unSortedList[smallestIndex]);

        unSortedList.RemoveAt(smallestIndex);

        //Sort the unsorted vertices based on angle
        Vector3 firstPoint = sortedList[0];
        //Everything MUST be in 2D space
        firstPoint.y = 0f;

        //Will sort from smallest to higest angle
        unSortedList = unSortedList.OrderBy(n => GetAngle(new Vector3(n.x, 0f, n.z) - firstPoint)).ToList();

        //Revers because its faster to remove from the end
        unSortedList.Reverse();

        //The vertices with the smallest angle is alos on the  convex hull
        sortedList.Add(unSortedList[unSortedList.Count - 1]);

        unSortedList.RemoveAt(unSortedList.Count - 1);

        int safety = 0;

        while(unSortedList.Count > 0 && safety < 1000)
        {
            safety += 1;

            //Is this clockwise or counter clockwise triangle
            Vector3 a = sortedList[sortedList.Count - 2];
            Vector3 b = sortedList[sortedList.Count - 1];

            Vector3 c = unSortedList[unSortedList.Count - 1];

            unSortedList.RemoveAt(unSortedList.Count - 1);

            sortedList.Add(c);

            while (IsClockWise(a,b,c) && safety < 1000)
            {
                sortedList.RemoveAt(sortedList.Count - 2);

                a = sortedList[sortedList.Count - 3];
                b = sortedList[sortedList.Count - 2];
                c = sortedList[sortedList.Count - 1];

                safety += 1;
            }
        }

        return sortedList;
    }

    private static bool IsClockWise(Vector3 a, Vector3 b, Vector3 c)
    {
        float singedArea = (b.x - a.x) * (c.z - a.z) - (b.z - a.z) * (c.x - a.x);

        if(singedArea > 0f)
        {
            return false;
        }
        else
        {
            return true;
        }
    }


    private static float GetAngle(Vector3 vec)
    {
        float angle = Mathf.Atan2(vec.z, vec.x);

        return angle;
    }
}
