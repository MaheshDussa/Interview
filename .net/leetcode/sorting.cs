using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace leetcode
{
    /// <summary>
    /// Collection of classic sorting algorithms with their complexity characteristics:
    ///
    ///   Algorithm        | Best        | Worst       | Space    | Stable?
    ///   -----------------|-------------|-------------|----------|---------
    ///   Bubble Sort      | O(n)        | O(n^2)      | O(1)     | Stable
    ///   Selection Sort   | O(n^2)      | O(n^2)      | O(1)     | Unstable
    ///   Insertion Sort   | O(n)        | O(n^2)      | O(1)     | Stable
    ///   Merge Sort       | O(n log n)  | O(n log n)  | O(n)     | Stable
    ///   Quick Sort       | O(n log n)  | O(n^2)      | O(log n) | Unstable
    ///   Heap Sort        | O(n log n)  | O(n log n)  | O(1)     | Unstable
    ///   Counting Sort    | O(n + k)    | O(n + k)    | O(k)     | Stable
    /// </summary>
    public class sorting
    {

        // ----------------------------------------------------------------
        // Bubble Sort
        // Idea: Repeatedly walk through the list, swapping adjacent items
        // that are out of order. After each pass the largest remaining
        // element "bubbles up" to its final position at the end.
        // Best O(n) (already sorted, early exit), Worst O(n^2), Stable.
        //
        // Example walk-through for [5, 1, 4, 2]:
        //   Pass 1: [1,4,2,5]   (5 bubbled to the end)
        //   Pass 2: [1,2,4,5]   (4 bubbled to position 2)
        //   Pass 3: [1,2,4,5]   (no swaps -> array is sorted, early-exit)
        // ----------------------------------------------------------------
        public void BubbleSort(int[] arr)
        {
            int n = arr.Length;

            // Outer loop: we need at most n-1 passes. After pass i, the
            // last i elements are guaranteed to be in their final place.
            for (int i = 0; i < n - 1; i++)
            {
                // Track whether any swap occurred in this pass. If not,
                // the array is already sorted and we can stop early.
                bool swapped = false;

                // Inner loop: compare each adjacent pair in the still
                // unsorted prefix [0 .. n-1-i]. We stop at n-1-i because
                // the last i positions are already finalized.
                for (int j = 0; j < n - 1 - i; j++)
                {
                    // If the left element is bigger than the right one,
                    // they are out of order -> swap them so the larger
                    // value moves one step closer to the end.
                    if (arr[j] > arr[j + 1])
                    {
                        // Tuple deconstruction = single-line swap.
                        (arr[j], arr[j + 1]) = (arr[j + 1], arr[j]);
                        swapped = true;
                    }
                }

                // Optional: visualize the array after each pass.
                Console.WriteLine(string.Join(", ", arr));

                // Early-exit optimization: a full pass with no swaps
                // means every adjacent pair is already in order.
                if (!swapped) break;
            }
        }

        // ----------------------------------------------------------------
        // Selection Sort
        // Idea: For each position i, scan the remaining unsorted portion
        // to find the minimum element and swap it into position i.
        // Best/Worst O(n^2), in-place O(1), Unstable.
        //
        // Example walk-through for [5, 1, 4, 2]:
        //   i=0: min in [5,1,4,2] is 1 -> swap idx 0,1 -> [1,5,4,2]
        //   i=1: min in [5,4,2]   is 2 -> swap idx 1,3 -> [1,2,4,5]
        //   i=2: min in [4,5]     is 4 -> already in place -> [1,2,4,5]
        // ----------------------------------------------------------------
        public int[] SelectionSort(int[] ints)
        {
            int n = ints.Length;

            // Outer loop: position i is where we will place the next
            // smallest element. After this iteration, ints[0..i] is sorted.
            for (int i = 0; i < n - 1; i++)
            {
                // Assume the current position holds the minimum, then
                // try to find a smaller element to the right.
                int minIndex = i;

                // Inner loop: scan the unsorted region [i+1 .. n-1] and
                // remember the index of the smallest value seen so far.
                for (int j = i + 1; j < n; j++)
                {
                    if (ints[j] < ints[minIndex])
                    {
                        minIndex = j;
                    }
                }

                // Swap the found minimum into position i. Even if
                // minIndex == i, the swap is harmless.
                int temp = ints[minIndex];
                ints[minIndex] = ints[i];
                ints[i] = temp;
            }
            return ints;
        }


        // ----------------------------------------------------------------
        // Insertion Sort
        // Idea: Build the sorted list one element at a time by taking the
        // next item and inserting it into its correct place among the
        // already-sorted prefix (shifting larger items to the right).
        // Best O(n) (nearly sorted), Worst O(n^2), in-place, Stable.
        //
        // Mental model: like sorting playing cards in your hand. You
        // pick the next card and slide it left past any larger card
        // until it lands in the correct spot.
        //
        // Example walk-through for [5, 1, 4, 2]:
        //   i=1 key=1 -> shift 5 right -> [1,5,4,2]
        //   i=2 key=4 -> shift 5 right -> [1,4,5,2]
        //   i=3 key=2 -> shift 5,4 right -> [1,2,4,5]
        // ----------------------------------------------------------------
        public int[] InsertionSort(int[] ints)
        {
            int n = ints.Length;

            // Start from the second element (index 1) and move to the end
            for (int i = 1; i < n; i++)
            {
                // Pick the current element to be inserted into the sorted part
                int key = ints[i];

                // 'j' starts at the element right before 'i'
                int j = i - 1;

                // Shift elements of the sorted portion that are larger than 'key'
                // to the right by one position to make space
                while (j >= 0 && ints[j] > key)
                {
                    ints[j + 1] = ints[j]; // Shift element to the right
                    j--;                   // Move pointer left to check the next element
                }

                // Place the 'key' into its correct sorted position
                int correctPosition = j + 1;
                ints[correctPosition] = key;
            }

            // Return the sorted array
            return ints;
        }



        // ----------------------------------------------------------------
        // Merge Sort  (classic divide-and-conquer)
        // Idea: Divide the array in half, recursively sort each half, then
        // merge the two sorted halves into one sorted array.
        // Best/Worst O(n log n), O(n) extra space, Stable.
        //
        // Recursion tree for [5,1,4,2]:
        //          [5,1,4,2]
        //          /        \
        //       [5,1]      [4,2]
        //       /  \       /  \
        //     [5] [1]    [4] [2]
        //       \  /      \  /
        //       [1,5]     [2,4]
        //          \       /
        //          [1,2,4,5]
        // ----------------------------------------------------------------
        public int[] MergeSort(int[] ints)
        {
            // Base case: 0 or 1 element is already sorted.
            if (ints.Length <= 1)
            {
                return ints;
            }

            // Divide: Calculate the middle point
            int mid = ints.Length / 2;

            // Create Left and Right arrays explicitly
            int[] left = new int[mid];
            int[] right = new int[ints.Length - mid];

            // Populate Left array
            for (int i = 0; i < mid; i++)
            {
                left[i] = ints[i];
            }

            // Populate Right array (starts from mid index)
            for (int j = 0; j < right.Length; j++)
            {
                right[j] = ints[mid + j];
            }

            // Conquer: Recursively sort both halves
            left = MergeSort(left);
            right = MergeSort(right);

            // Combine: Merge them together
            return Merge(left, right);
        }

        private int[] Merge(int[] left, int[] right)
        {
            int[] result = new int[left.Length + right.Length];
            int i = 0, j = 0, k = 0;

            // Main comparison loop
            while (i < left.Length && j < right.Length)
            {
                if (left[i] <= right[j])
                {
                    result[k] = left[i];
                    i++; // Move left pointer
                }
                else
                {
                    result[k] = right[j];
                    j++; // Move right pointer
                }
                k++; // Always move result pointer forward
            }

            // Copy remaining items from left array (if any)
            while (i < left.Length)
            {
                result[k] = left[i];
                i++;
                k++;
            }

            // Copy remaining items from right array (if any)
            while (j < right.Length)
            {
                result[k] = right[j];
                j++;
                k++;
            }

            return result;
        }


        // ----------------------------------------------------------------
        // Quick Sort  (in-place divide-and-conquer)
        // Idea: Pick a pivot, partition the array so that elements smaller
        // than the pivot go to the left and larger to the right, then
        // recursively quicksort both partitions.
        // Average O(n log n), Worst O(n^2) (bad pivots), in-place, Unstable.
        //
        // After Partition(low..high):
        //   [ values <= pivot | pivot | values > pivot ]
        //                       ^ returned index = pivot's final spot
        // Pivot is now in its final position, so the two sides can be
        // sorted independently without ever touching the pivot again.
        // ----------------------------------------------------------------
        public int[] QuickSort(int[] ints)
        {
            // Public entry point: starts sorting from index 0 to the last index
            QuickSortInPlace(ints, 0, ints.Length - 1);
            return ints;
        }

        private void QuickSortInPlace(int[] arr, int low, int high)
        {
            // Base case: If the segment has 0 or 1 element, it is already sorted
            if (low >= high)
            {
                return;
            }

            // Partition: Rearrange elements and get the final position of the pivot
            int pivotIndex = Partition(arr, low, high);

            // Recursively sort the left side (elements smaller than or equal to pivot)
            QuickSortInPlace(arr, low, pivotIndex - 1);

            // Recursively sort the right side (elements greater than pivot)
            QuickSortInPlace(arr, pivotIndex + 1, high);
        }

        private int Partition(int[] arr, int low, int high)
        {
            // Choose the last element as the pivot
            int pivot = arr[high];

            // 'i' keeps track of the boundary for elements smaller than or equal to the pivot
            int i = low - 1;

            // Scan the array from the 'low' index up to the element just before the pivot
            for (int j = low; j < high; j++)
            {
                // If current element is smaller than or equal to pivot
                if (arr[j] <= pivot)
                {
                    i++; // Expand the smaller element boundary forward

                    // Swap arr[i] and arr[j] using a classic temporary variable
                    int temp = arr[i];
                    arr[i] = arr[j];
                    arr[j] = temp;
                }
            }

            // Place the pivot element into its correct sorted position (at index i + 1)
            int finalPivotIndex = i + 1;

            int pivotTemp = arr[finalPivotIndex];
            arr[finalPivotIndex] = arr[high];
            arr[high] = pivotTemp;

            // Return the final resting position of the pivot
            return finalPivotIndex;
        }


        // ----------------------------------------------------------------
        // Heap Sort  (in-place, uses a binary max-heap embedded in arr)
        // Idea: Build a max-heap from the array, then repeatedly swap the
        // root (largest) with the last element, shrink the heap, and
        // re-heapify. Result is an ascending sorted array.
        // Best/Worst O(n log n), in-place O(1), Unstable.
        //
        // Heap stored as an array (0-based indexing):
        //   parent(i) = (i - 1) / 2
        //   left(i)   = 2*i + 1
        //   right(i)  = 2*i + 2
        // Max-heap invariant: arr[parent] >= arr[child] for every node.
        //
        // Example walk-through for [4, 10, 3, 5, 1]:
        //   Phase 1 - Build heap: [10, 5, 3, 4, 1]
        //   Phase 2 - Extract max repeatedly:
        //     Swap [10,1] -> [1,5,3,4,|10] -> heapify -> [5,4,3,1,|10]
        //     Swap [5,1]  -> [1,4,3,|5,10] -> heapify -> [4,1,3,|5,10]
        //     Swap [4,3]  -> [3,1,|4,5,10] -> heapify -> [3,1,|4,5,10]
        //     Swap [3,1]  -> [1,|3,4,5,10] -> sorted!
        // ----------------------------------------------------------------
        public int[] HeapSort(int[] ints)
        {
            int n = ints.Length;

            // ============================================================
            // PHASE 1: Build Max-Heap (bottom-up heapify)
            // ============================================================
            // We only heapify non-leaf nodes. Leaf nodes (from n/2 to n-1)
            // are already valid heaps of size 1.
            // Start from the last parent node (n/2 - 1) and work backward
            // to the root (index 0). This ensures that when we heapify
            // a parent, its children are already valid heaps.

            // Loop variable i: starts at last parent, decrements to 0.
            // Decrement happens automatically at the end of each iteration.
            for (int i = n / 2 - 1; i >= 0; i--)
            {
                // Heapify subtree rooted at index i.
                // After this call, the subtree at i satisfies max-heap property.
                Heapify(ints, n, i);
            }

            // At this point, ints is a valid max-heap.
            // The largest element is at index 0.

            // ============================================================
            // PHASE 2: Extract Maximum Elements One by One
            // ============================================================
            // Repeatedly move the root (max element) to the end of the
            // array, reduce heap size, and restore heap property.

            // Loop variable 'end': starts at last index (n-1), decrements to 1.
            // Each iteration places one more element in its final sorted position.
            // We stop at end=1 because when heap size=1, that element is already sorted.
            for (int end = n - 1; end > 0; end--)
            {
                // Step 1: Swap root (max element at index 0) with last element.
                // The max element is now in its final sorted position at index 'end'.
                (ints[0], ints[end]) = (ints[end], ints[0]);

                // Step 2: Reduce heap size to 'end' (excluding the sorted portion).
                // The element at index 0 may violate the heap property now.
                // Re-heapify from root to restore max-heap property.
                Heapify(ints, end, 0);

                // After heapify, the next largest element is now at the root.
                // Loop continues: 'end' decrements by 1 automatically.
            }

            // Array is now sorted in ascending order.
            return ints;
        }

        /// <summary>
        /// Sift-down operation: Ensures the subtree rooted at 'root' satisfies
        /// the max-heap property. Assumes left and right subtrees are already
        /// valid max-heaps.
        /// 
        /// Parameters:
        ///   arr      - the array containing the heap
        ///   heapSize - number of elements in the active heap (0 to heapSize-1)
        ///   root     - index of the subtree root to heapify
        /// </summary>
        private void Heapify(int[] arr, int heapSize, int root)
        {
            // Start by assuming the root is the largest.
            int largest = root;

            // Calculate indices of left and right children.
            int leftChild = 2 * root + 1;
            int rightChild = 2 * root + 2;

            // Check if left child exists AND is larger than current largest.
            // leftChild < heapSize ensures the child is within the active heap.
            if (leftChild < heapSize && arr[leftChild] > arr[largest])
            {
                largest = leftChild;
            }

            // Check if right child exists AND is larger than current largest.
            if (rightChild < heapSize && arr[rightChild] > arr[largest])
            {
                largest = rightChild;
            }

            // If the largest element is NOT the root, we need to swap
            // and continue heapifying down the affected subtree.
            if (largest != root)
            {
                // Swap root with the larger child.
                (arr[root], arr[largest]) = (arr[largest], arr[root]);

                // Recursively heapify the affected subtree.
                // The child that was swapped may now violate heap property
                // in its subtree, so we heapify it.
                Heapify(arr, heapSize, largest);
            }
            // If largest == root, the subtree already satisfies heap property.
        }

        // ----------------------------------------------------------------
        // Counting Sort  (non-comparison sort)
        // Idea: Count how many times each value appears, then use the
        // running totals (prefix sums) to place each element directly into
        // its final sorted position. Works on integers in a known range.
        // Best/Worst O(n + k) where k = value range, O(k) extra space, Stable.
        //
        // NOTE: Only practical when k (= max - min + 1) is not much
        // bigger than n. For huge ranges use Radix Sort or comparison sort.
        //
        // Example walk-through for [4, 2, 2, 8, 3]:
        //   Step 1 - Find range: min=2, max=8, range=7
        //   Step 2 - Count frequencies (offset by -2):
        //            Value:  2  3  4  5  6  7  8
        //            Count: [2, 1, 1, 0, 0, 0, 1]
        //   Step 3 - Prefix sum (cumulative counts):
        //            Count: [2, 3, 4, 4, 4, 4, 5]
        //            This means: 2 values ≤2, 3 values ≤3, etc.
        //   Step 4 - Place elements (RIGHT to LEFT for stability):
        //            Process 3: count[1]=3, place at output[2], count[1]=2
        //            Process 8: count[6]=5, place at output[4], count[6]=4
        //            Process 2: count[0]=2, place at output[1], count[0]=1
        //            Process 2: count[0]=1, place at output[0], count[0]=0
        //            Process 4: count[2]=4, place at output[3], count[2]=3
        //            Result: [2, 2, 3, 4, 8]
        // ----------------------------------------------------------------
        public int[] CountingSort(int[] ints)
        {
            int n = ints.Length;

            // Edge case: empty or single-element array is already sorted.
            if (n == 0)
            {
                return ints;
            }

            // ============================================================
            // STEP 1: Find the Range of Values
            // ============================================================
            // We need to know the minimum and maximum values to:
            // 1. Size our count array appropriately
            // 2. Support negative numbers by using an offset (min)

            int min = ints[0];
            int max = ints[0];

            // Loop through array to find min and max.
            // Loop variable i: increments from 1 to n-1 automatically.
            for (int i = 1; i < n; i++)
            {
                if (ints[i] < min)
                {
                    min = ints[i];
                }
                if (ints[i] > max)
                {
                    max = ints[i];
                }
            }

            // Calculate the range of values.
            // For values [2, 8], range = 8 - 2 + 1 = 7 (values: 2,3,4,5,6,7,8)
            int range = max - min + 1;

            // ============================================================
            // STEP 2: Count Frequency of Each Value
            // ============================================================
            // count[v - min] will store the frequency of value v.
            // Subtracting min lets us use 0-based indexing even with negatives.
            int[] count = new int[range];

            // Count each value's frequency.
            // Loop variable i: increments from 0 to n-1 automatically.
            for (int i = 0; i < n; i++)
            {
                int value = ints[i];
                int index = value - min;  // Map value to count array index
                count[index]++;           // Increment count for this value
            }

            // ============================================================
            // STEP 3: Build Prefix Sum (Cumulative Counts)
            // ============================================================
            // Transform count[] from frequencies to cumulative totals.
            // After this, count[i] = number of elements ≤ (i + min).
            // This tells us where each value should END in the sorted output.

            // Loop variable i: starts at 1, increments to range-1 automatically.
            // We start at 1 because count[0] is already the count of the smallest value.
            for (int i = 1; i < range; i++)
            {
                // Add previous count to current count.
                // This creates a running total.
                count[i] = count[i] + count[i - 1];
            }

            // ============================================================
            // STEP 4: Place Elements in Sorted Order (RIGHT to LEFT)
            // ============================================================
            // We iterate from RIGHT to LEFT to maintain STABILITY.
            // Equal values will appear in the same relative order as input.

            int[] output = new int[n];

            // Loop variable i: starts at n-1, decrements to 0 automatically.
            // Processing backward preserves the original order of equal elements.
            for (int i = n - 1; i >= 0; i--)
            {
                int value = ints[i];           // Current value to place
                int index = value - min;        // Map to count array index

                // count[index] tells us how many elements are ≤ value.
                // Decrement it first (--count[index]) to get the position
                // where this value should be placed.
                count[index]--;                 // Decrement first
                int position = count[index];    // Get the position
                output[position] = value;       // Place value in output

                // This can be written more concisely as:
                // output[--count[index]] = value;
            }

            // Array is now sorted.
            return output;
        }
    }
}