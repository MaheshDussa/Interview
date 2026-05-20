using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace leetcode
{
    /// <summary>
    /// Collection of classic search algorithms with their complexity characteristics:
    ///
    ///   Algorithm             | Best     | Worst       | Space    | Input requirement
    ///   ----------------------|----------|-------------|----------|----------------------
    ///   Linear Search         | O(1)     | O(n)        | O(1)     | None
    ///   Binary Search         | O(1)     | O(log n)    | O(1)     | Sorted array
    ///   Binary Search (Rec.)  | O(1)     | O(log n)    | O(log n) | Sorted array
    ///   Jump Search           | O(1)     | O(sqrt(n))  | O(1)     | Sorted array
    ///   Interpolation Search  | O(1)     | O(n) / O(log log n) avg uniform | O(1) | Sorted, uniformly distributed
    ///   Exponential Search    | O(1)     | O(log n)    | O(1)     | Sorted array
    ///   Ternary Search        | O(1)     | O(log3 n)   | O(1)     | Sorted array
    /// </summary>
    public class searching
    {
        // ----------------------------------------------------------------
        // Linear Search
        // Idea: Walk through every element until the target is found or
        // the end is reached. Works on any (sorted or unsorted) collection.
        // Time: O(n), Space: O(1).
        // ----------------------------------------------------------------
        public int LinearSearch(int[] arr, int target)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] == target) return i;
            }
            return -1; // not found
        }

        // ----------------------------------------------------------------
        // Binary Search (iterative)
        // Idea: Repeatedly look at the middle element of a sorted array;
        // discard the half that cannot contain the target.
        // Time: O(log n), Space: O(1). Requires a sorted array.
        // ----------------------------------------------------------------
        public int BinarySearch(int[] sortedArr, int target)
        {
            int low = 0;
            int high = sortedArr.Length - 1;

            while (low <= high)
            {
                // Avoid overflow vs. (low + high) / 2.
                int mid = low + (high - low) / 2;

                if (sortedArr[mid] == target) return mid;
                if (sortedArr[mid] < target) low = mid + 1;
                else high = mid - 1;
            }
            return -1;
        }

        // ----------------------------------------------------------------
        // Binary Search (recursive)
        // Same idea as above expressed via recursion.
        // Time: O(log n), Space: O(log n) due to the call stack.
        // ----------------------------------------------------------------
        public int BinarySearchRecursive(int[] sortedArr, int target)
        {
            return BinarySearchRecursive(sortedArr, target, 0, sortedArr.Length - 1);
        }

        private int BinarySearchRecursive(int[] arr, int target, int low, int high)
        {
            if (low > high) return -1;
            int mid = low + (high - low) / 2;

            if (arr[mid] == target) return mid;
            if (arr[mid] < target) return BinarySearchRecursive(arr, target, mid + 1, high);
            return BinarySearchRecursive(arr, target, low, mid - 1);
        }

        // ----------------------------------------------------------------
        // Jump Search
        // Idea: On a sorted array, jump ahead in fixed steps of size
        // sqrt(n) until we pass the target, then do a linear search inside
        // that block. Fewer comparisons than linear, simpler than binary.
        // Time: O(sqrt(n)), Space: O(1). Requires a sorted array.
        // ----------------------------------------------------------------
        public int JumpSearch(int[] sortedArr, int target)
        {
            int n = sortedArr.Length;
            if (n == 0) return -1;

            int step = (int)Math.Floor(Math.Sqrt(n));
            int prev = 0;

            // Jump forward until we find a block whose end >= target.
            while (prev < n && sortedArr[Math.Min(step, n) - 1] < target)
            {
                prev = step;
                step += (int)Math.Floor(Math.Sqrt(n));
                if (prev >= n) return -1;
            }

            // Linear search inside the identified block [prev, min(step, n)).
            for (int i = prev; i < Math.Min(step, n); i++)
            {
                if (sortedArr[i] == target) return i;
            }
            return -1;
        }

        // ----------------------------------------------------------------
        // Interpolation Search
        // Idea: Like binary search, but guesses the probable position of
        // the target based on its value, assuming values are uniformly
        // distributed: pos = low + ((target - arr[low]) * (high - low)) / (arr[high] - arr[low])
        // Time: O(log log n) avg for uniform data, O(n) worst case. Sorted array required.
        // ----------------------------------------------------------------
        public int InterpolationSearch(int[] sortedArr, int target)
        {
            int low = 0;
            int high = sortedArr.Length - 1;

            while (low <= high && target >= sortedArr[low] && target <= sortedArr[high])
            {
                if (low == high)
                {
                    return sortedArr[low] == target ? low : -1;
                }

                // Probe position proportional to where target sits between arr[low] and arr[high].
                int pos = low + (int)(((long)(target - sortedArr[low]) * (high - low)) /
                                      (sortedArr[high] - sortedArr[low]));

                if (sortedArr[pos] == target) return pos;
                if (sortedArr[pos] < target) low = pos + 1;
                else high = pos - 1;
            }
            return -1;
        }

        // ----------------------------------------------------------------
        // Exponential Search
        // Idea: On a sorted (possibly unbounded) array, double the index
        // until you bracket the target, then run binary search inside that
        // window. Great when the target is near the beginning or size is unknown.
        // Time: O(log n), Space: O(1). Requires a sorted array.
        // ----------------------------------------------------------------
        public int ExponentialSearch(int[] sortedArr, int target)
        {
            int n = sortedArr.Length;
            if (n == 0) return -1;
            if (sortedArr[0] == target) return 0;

            // Find a range [i/2, min(i, n-1)] that contains the target.
            int i = 1;
            while (i < n && sortedArr[i] <= target) i *= 2;

            return BinarySearchRecursive(sortedArr, target, i / 2, Math.Min(i, n - 1));
        }

        // ----------------------------------------------------------------
        // Ternary Search
        // Idea: Like binary search, but split the range into three parts
        // using two midpoints. Useful conceptually; in practice binary
        // search has fewer comparisons.
        // Time: O(log3 n), Space: O(1). Requires a sorted array.
        // ----------------------------------------------------------------
        public int TernarySearch(int[] sortedArr, int target)
        {
            int low = 0;
            int high = sortedArr.Length - 1;

            while (low <= high)
            {
                int third = (high - low) / 3;
                int mid1 = low + third;
                int mid2 = high - third;

                if (sortedArr[mid1] == target) return mid1;
                if (sortedArr[mid2] == target) return mid2;

                if (target < sortedArr[mid1]) high = mid1 - 1;
                else if (target > sortedArr[mid2]) low = mid2 + 1;
                else { low = mid1 + 1; high = mid2 - 1; }
            }
            return -1;
        }
    }
}