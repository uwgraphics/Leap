using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;


public static class DataDisplayColorChange {
    public static void Assess(List<int> dataList, InferenceCharacter character,
        bool toggle, int frame, Renderer render) {
            if (toggle)
            {
                if (dataList[frame] == 1)
                {
                    render.material.color = Color.red;
                }
                else
                {
                    render.material.color = character.DefaultColor();
                }
            }
            else {
                render.material.color = character.DefaultColor();
            }
    }



    //alternate method with a list of magnitudes.  Can decide if want it filtered and if
    //you want min or max
    public static void Assess(List<double> dataList, double threshold, InferenceCharacter character,
        bool toggle, int frame, Renderer render, bool filtered, bool min) {

        if (toggle)
        {
            List<double> temp = new List<double>();
            if (filtered)
            {
                temp = Filter.SimpleLowPass(dataList);
            }

            int minMaxCheck;
            if (min)
            {
                minMaxCheck = temp[frame] < threshold ? 1 : 0;
            }
            else {
                minMaxCheck = temp[frame] > threshold ? 1 : 0;
            }
             
            if (minMaxCheck == 1)
            {
                render.material.color = Color.red;
            }
            else
            {
                render.material.color = character.DefaultColor();
            }
        }
        else
        {
            render.material.color = character.DefaultColor();
        }
    }

    public static void Assess(List<double> dataList, double threshold, InferenceCharacter character,
        bool toggle, int frame, Renderer render, double[] filter, bool min)
    {

        if (toggle)
        {

            List<double> temp = Filter.FilterList(dataList, filter);
            
            int minMaxCheck;
            if (min)
            {
                minMaxCheck = temp[frame] < threshold ? 1 : 0;
            }
            else
            {
                minMaxCheck = temp[frame] > threshold ? 1 : 0;
            }

            if (minMaxCheck == 1)
            {
                render.material.color = Color.red;
            }
            else
            {
                render.material.color = character.DefaultColor();
            }
        }
        else
        {
            render.material.color = character.DefaultColor();
        }
    }

}
