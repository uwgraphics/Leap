using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Utility functions for statistics.
/// </summary>
public static class StatUtil
{
    /// <summary>
    /// Compute Cohen's kappa as a measure of intercoder reliability
    /// </summary>
    /// <typeparam name="TItem">Item type</typeparam>
    /// <typeparam name="TCat">Category type</typeparam>
    /// <param name="items">Items</param>
    /// <param name="cats">Categories</param>
    /// <param name="itemCats1">Categorized items from coder 1</param>
    /// <param name="itemCats2">Categorized items from coder 2</param>
    /// <returns></returns>
    public static float ComputeCohenKappa<TItem, TCat>(TItem[] items, TCat[] cats, TCat[] itemCats1, TCat[] itemCats2)
    {
        if (items.Length != itemCats1.Length || items.Length != itemCats2.Length)
            throw new ArgumentException("Number of items must equal the number of categorized items from each coder", "items");
        if (itemCats1.Any(ic => !cats.Contains(ic)))
            throw new ArgumentException("One or more items from coder 1 have invalid categories", "itemCats1");
        if (itemCats2.Any(ic => !cats.Contains(ic)))
            throw new ArgumentException("One or more items from coder 1 have invalid categories", "itemCats2");

        Dictionary<TCat, int> catIndices = new Dictionary<TCat, int>();
        for (int catIndex = 0; catIndex < cats.Length; ++catIndex)
            catIndices[cats[catIndex]] = catIndex;

        // Count items in each category and number of agreements
        int n = items.Length;
        int no = 0; // number of agreements
        int[] nc1 = new int[cats.Length];
        int[] nc2 = new int[cats.Length];
        for (int i = 0; i < items.Length; ++i)
        {
            if (itemCats1[i].Equals(itemCats2[i]))
                ++no;

            ++nc1[catIndices[itemCats1[i]]];
            ++nc2[catIndices[itemCats2[i]]];
        }
        
        // Compute agreement probabilities
        float po = ((float)no) / n;
        float pe = 0f;
        for (int catIndex = 0; catIndex < cats.Length; ++catIndex)
            pe += ((float)(nc1[catIndex] * nc2[catIndex]))/(n * n);

        float k = pe < 0.99995f ? (po - pe) / (1 - pe) : 0f;
        return k;
    }
}

