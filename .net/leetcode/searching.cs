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
    ///   Jump Search           | O(1)     | O(√n)       | O(1)     | Sorted array
    ///   Interpolation Search  | O(1)     | O(log log n)| O(1)     | Sorted, uniform
    ///   Exponential Search    | O(1)     | O(log n)    | O(1)     | Sorted array
    ///   Ternary Search        | O(1)     | O(log₃ n)   | O(1)     | Sorted array
    /// </summary>
    public class searching
    {
        // ----------------------------------------------------------------
        // Linear Search
        // Idea: Sequentially check each element from left to right until
        // we find the target or exhaust the array. Works on any data —
        // sorted or unsorted.
        // Best O(1) (target at index 0), Worst O(n), Space O(1).
        //
        // Example walk-through for arr = [5, 1, 4, 2], target = 4:
        //   i=0: arr[0]=5  ≠ 4
        //   i=1: arr[1]=1  ≠ 4
        //   i=2: arr[2]=4  = 4 -> return index 2
        // ----------------------------------------------------------------
        public int LinearSearch(int[] arr, int target)
        {
            int n = arr.Length;

            // Iterate through every element in the array. The loop
            // automatically increments i after each iteration.
            for (int i = 0; i < n; i++)
            {
                // If we find the target, immediately return its index.
                if (arr[i] == target)
                {
                    return i;
                }
            }

            // Target not found after checking all elements.
            return -1;
        }

        // ----------------------------------------------------------------
        // Binary Search (iterative)
        // Idea: Repeatedly divide the search space in half. Look at the
        // middle element; if it matches, we're done. If target is smaller,
        // search the left half; if larger, search the right half.
        // Best O(1) (target at middle), Worst O(log n), Space O(1).
        // Requires: sorted array.
        //
        // Example walk-through for arr = [1, 2, 4, 5, 8], target = 4:
        //   low=0, high=4, mid=2 -> arr[2]=4 = target -> return 2
        //
        // Example walk-through for arr = [1, 2, 4, 5, 8], target = 5:
        //   low=0, high=4, mid=2 -> arr[2]=4 < 5 -> low=3
        //   low=3, high=4, mid=3 -> arr[3]=5 = target -> return 3
        // ----------------------------------------------------------------
        public int BinarySearch(int[] sortedArr, int target)
        {
            // Initialize search boundaries: low at start, high at end.
            int low = 0;
            int high = sortedArr.Length - 1;

            // Continue while there is a valid range to search.
            while (low <= high)
            {
                // Calculate mid-point. Using low + (high - low) / 2
                // prevents integer overflow that could occur with
                // (low + high) / 2 for very large indices.
                int mid = low + (high - low) / 2;

                // Check if we found the target at mid.
                if (sortedArr[mid] == target)
                {
                    return mid;
                }

                // Target is greater than mid value -> search right half.
                // Move low pointer just past mid (mid + 1).
                if (sortedArr[mid] < target)
                {
                    low = mid + 1;
                }
                // Target is smaller than mid value -> search left half.
                // Move high pointer just before mid (mid - 1).
                else
                {
                    high = mid - 1;
                }
            }

            // Search space exhausted, target not found.
            return -1;
        }

        // ----------------------------------------------------------------
        // Binary Search (recursive)
        // Idea: Same divide-and-conquer approach as iterative binary search,
        // but implemented using recursion. Each recursive call narrows the
        // search range by half.
        // Best O(1), Worst O(log n), Space O(log n) due to call stack.
        // Requires: sorted array.
        // ----------------------------------------------------------------
        public int BinarySearchRecursive(int[] sortedArr, int target)
        {
            // Public entry point. Delegates to the recursive helper with
            // initial boundaries covering the entire array.
            return BinarySearchRecursive(sortedArr, target, 0, sortedArr.Length - 1);
        }

        /// <summary>
        /// Recursive helper that searches arr[low..high] for target.
        /// </summary>
        private int BinarySearchRecursive(int[] arr, int target, int low, int high)
        {
            // Base case: if low > high, the search range is invalid/empty.
            if (low > high)
            {
                return -1;
            }

            // Calculate the middle index (avoiding overflow).
            int mid = low + (high - low) / 2;

            // If we found the target at mid, return its index.
            if (arr[mid] == target)
            {
                return mid;
            }

            // If mid value is less than target, recurse on the right half.
            // New range becomes [mid + 1, high].
            if (arr[mid] < target)
            {
                return BinarySearchRecursive(arr, target, mid + 1, high);
            }

            // Otherwise mid value is greater than target, recurse on left half.
            // New range becomes [low, mid - 1].
            return BinarySearchRecursive(arr, target, low, mid - 1);
        }

        // ----------------------------------------------------------------
        // Jump Search
        // Idea: On a sorted array, jump ahead in fixed-size blocks of
        // √n until we overshoot the target, then do a linear search
        // backward within that block. Sits between linear O(n) and
        // binary O(log n) in both complexity and simplicity.
        // Best O(1), Worst O(√n), Space O(1).
        // Requires: sorted array.
        //
        // Example walk-through for arr = [1, 2, 4, 5, 8, 10, 12, 15], target = 10:
        //   n=8, step=√8≈2 (initially step=2, we jump by 2)
        //   Jump phase:
        //     prev=0, step=2 -> arr[1]=2 < 10, continue
        //     prev=2, step=4 -> arr[3]=5 < 10, continue
        //     prev=4, step=6 -> arr[5]=10 ≥ 10, stop jumping
        //   Linear search in [4, 6):
        //     i=4: arr[4]=8  ≠ 10
        //     i=5: arr[5]=10 = 10 -> return 5
        // ----------------------------------------------------------------
        public int JumpSearch(int[] sortedArr, int target)
        {
            int n = sortedArr.Length;

            // Edge case: empty array has nothing to search.
            if (n == 0)
            {
                return -1;
            }

            // Optimal jump size is √n. We'll increment `step` by this
            // amount in each jump.
            int stepSize = (int)Math.Floor(Math.Sqrt(n));
            int step = stepSize;
            int prev = 0;

            // Phase 1: Jump forward in blocks of size stepSize until we
            // find a block where the last element >= target. This means
            // the target (if present) must be in [prev, step).
            while (prev < n && sortedArr[Math.Min(step, n) - 1] < target)
            {
                // Move prev to current step position.
                prev = step;

                // Jump ahead by another stepSize.
                step += stepSize;

                // If prev has gone beyond the array, target is too large.
                if (prev >= n)
                {
                    return -1;
                }
            }

            // Phase 2: Linear search inside the block [prev, min(step, n)).
            // We scan each element one by one to find the target.
            for (int i = prev; i < Math.Min(step, n); i++)
            {
                if (sortedArr[i] == target)
                {
                    return i;
                }
            }

            // Target not found in the identified block.
            return -1;
        }

        // ----------------------------------------------------------------
        // Interpolation Search
        // Idea: Like binary search, but instead of always picking the
        // middle point, we estimate where the target "should" be based on
        // its value relative to arr[low] and arr[high]. Works best when
        // values are uniformly distributed (e.g., [10, 20, 30, 40, ...]).
        // Best O(1), Average O(log log n) for uniform data, Worst O(n).
        // Space O(1). Requires: sorted array with uniform distribution.
        //
        // Formula for probe position:
        //   pos = low + ((target - arr[low]) / (arr[high] - arr[low])) * (high - low)
        //
        // Example walk-through for arr = [10, 20, 30, 40, 50], target = 30:
        //   low=0, high=4
        //   pos = 0 + ((30-10)/(50-10)) * 4 = 0 + (20/40)*4 = 0 + 2 = 2
        //   arr[2]=30 = target -> return 2
        // ----------------------------------------------------------------
        public int InterpolationSearch(int[] sortedArr, int target)
        {
            int low = 0;
            int high = sortedArr.Length - 1;

            // Continue while: (1) valid range exists, (2) target is within
            // the value range [arr[low], arr[high]]. If target is outside
            // this range, it cannot exist in the array.
            while (low <= high && target >= sortedArr[low] && target <= sortedArr[high])
            {
                // Edge case: if low and high converge to the same index,
                // check if that element is the target.
                if (low == high)
                {
                    return sortedArr[low] == target ? low : -1;
                }

                // Calculate the interpolated position. We estimate where
                // the target should be proportionally based on its value.
                // Using long for intermediate calculation to avoid overflow.
                int pos = low + (int)(((long)(target - sortedArr[low]) * (high - low)) /
                                      (sortedArr[high] - sortedArr[low]));

                // Check if we found the target at the probed position.
                if (sortedArr[pos] == target)
                {
                    return pos;
                }

                // If value at pos is less than target, search the right side.
                // Move low pointer just past pos (pos + 1).
                if (sortedArr[pos] < target)
                {
                    low = pos + 1;
                }
                // Otherwise value at pos is greater, search the left side.
                // Move high pointer just before pos (pos - 1).
                else
                {
                    high = pos - 1;
                }
            }

            // Target is outside the value range or not found.
            return -1;
        }

        // ----------------------------------------------------------------
        // Exponential Search
        // Idea: Find a range that contains the target by repeatedly
        // doubling the index (1, 2, 4, 8, 16, ...) until we overshoot,
        // then run binary search on that bracketed range. Excellent for
        // unbounded/infinite arrays or when the target is near the start.
        // Best O(1) (target at index 0), Worst O(log n), Space O(1).
        // Requires: sorted array.
        //
        // Example walk-through for arr = [1, 2, 4, 5, 8, 10, 12, 15], target = 10:
        //   n=8, arr[0]=1 ≠ 10
        //   Exponential phase:
        //     i=1: arr[1]=2  ≤ 10, double i -> i=2
        //     i=2: arr[2]=4  ≤ 10, double i -> i=4
        //     i=4: arr[4]=8  ≤ 10, double i -> i=8
        //     i=8: exceeds array bounds, stop
        //   Binary search in [i/2=4, min(8,7)=7] -> finds 10 at index 5
        // ----------------------------------------------------------------
        public int ExponentialSearch(int[] sortedArr, int target)
        {
            int n = sortedArr.Length;

            // Edge case: empty array.
            if (n == 0)
            {
                return -1;
            }

            // Quick check: if the first element is the target, return 0.
            if (sortedArr[0] == target)
            {
                return 0;
            }

            // Phase 1: Find a range [i/2, i] that contains the target
            // by exponentially increasing i (doubling: 1, 2, 4, 8, ...).
            // We stop when arr[i] > target OR i exceeds the array bounds.
            int i = 1;
            while (i < n && sortedArr[i] <= target)
            {
                // Double the index. After this, i *= 2 increments i.
                i *= 2;
            }

            // Phase 2: Run binary search on the range [i/2, min(i, n-1)].
            // The target (if present) must be in this range because:
            //   - arr[i/2] was ≤ target (or i/2 = 0)
            //   - arr[i] was > target (or i went out of bounds)
            return BinarySearchRecursive(sortedArr, target, i / 2, Math.Min(i, n - 1));
        }

        // ----------------------------------------------------------------
        // Ternary Search
        // Idea: Like binary search, but divide the search space into
        // THREE parts using two mid-points (mid1 and mid2). Compare the
        // target with both mid-points to decide which third to eliminate.
        // Theoretically O(log₃ n), but in practice binary search is
        // faster due to fewer comparisons per iteration.
        // Best O(1), Worst O(log₃ n) ≈ O(log n), Space O(1).
        // Requires: sorted array.
        //
        // Example walk-through for arr = [1, 2, 4, 5, 8, 10, 12], target = 8:
        //   low=0, high=6
        //   third = (6-0)/3 = 2
        //   mid1 = 0+2=2, mid2 = 6-2=4
        //   arr[2]=4 < 8, arr[4]=8 = target -> return 4
        // ----------------------------------------------------------------
        public int TernarySearch(int[] sortedArr, int target)
        {
            int low = 0;
            int high = sortedArr.Length - 1;

            // Continue while there is a valid search range.
            while (low <= high)
            {
                // Divide the range into three equal parts. `third` is
                // approximately one-third of the current range size.
                int third = (high - low) / 3;

                // mid1 is one-third from the left.
                int mid1 = low + third;

                // mid2 is one-third from the right.
                int mid2 = high - third;

                // Check if target is at mid1.
                if (sortedArr[mid1] == target)
                {
                    return mid1;
                }

                // Check if target is at mid2.
                if (sortedArr[mid2] == target)
                {
                    return mid2;
                }

                // If target < arr[mid1], it must be in the left third.
                // Narrow the search to [low, mid1-1].
                if (target < sortedArr[mid1])
                {
                    high = mid1 - 1;
                }
                // Else if target > arr[mid2], it must be in the right third.
                // Narrow the search to [mid2+1, high].
                else if (target > sortedArr[mid2])
                {
                    low = mid2 + 1;
                }
                // Otherwise target is between mid1 and mid2 (middle third).
                // Narrow the search to [mid1+1, mid2-1].
                else
                {
                    low = mid1 + 1;
                    high = mid2 - 1;
                }
            }

            // Target not found.
            return -1;
        }
    }
}