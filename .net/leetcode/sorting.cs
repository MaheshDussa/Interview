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

            // Treat ints[0] as a trivially sorted prefix of size 1.
            // Each iteration grows the sorted prefix by one element.
            for (int i = 1; i < n; i++)
            {
                // The element we are trying to insert into the sorted prefix.
                int key = ints[i];

                // Start comparing with the rightmost element of the prefix.
                int j = i - 1;

                // Shift every element greater than `key` one slot to the right,
                // making room for `key`. Stops when we either fall off the
                // left edge OR find an element that is <= key.
                while (j >= 0 && ints[j] > key)
                {
                    ints[j + 1] = ints[j];
                    j--;
                }

                // j+1 is the correct landing spot for `key`.
                ints[j + 1] = key;
            }
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
            // Base case: an array of length 0 or 1 is already sorted.
            if (ints.Length <= 1)
            {
                return ints;
            }

            // Divide: split the array at the midpoint.
            int mid = ints.Length / 2;

            // Conquer: recursively sort the left and right halves.
            // (Take/Skip allocate new arrays which is fine for clarity.)
            int[] left = MergeSort(ints.Take(mid).ToArray());
            int[] right = MergeSort(ints.Skip(mid).ToArray());

            // Combine: merge two sorted halves into one sorted array.
            return Merge(left, right);
        }

        /// <summary>
        /// Merge two already-sorted arrays into one sorted array — O(n).
        /// </summary>
        private int[] Merge(int[] left, int[] right)
        {
            // Output buffer big enough to hold every element.
            int[] result = new int[left.Length + right.Length];

            // i -> next index to read in left
            // j -> next index to read in right
            // k -> next index to write in result
            int i = 0, j = 0, k = 0;

            // Walk both inputs in parallel. At each step copy whichever
            // front element is smaller. Using "<=" preserves the
            // original relative order of equal items, which is what
            // makes Merge Sort STABLE.
            while (i < left.Length && j < right.Length)
            {
                if (left[i] <= right[j])
                    result[k++] = left[i++];
                else
                    result[k++] = right[j++];
            }

            // At this point exactly one of the two inputs has elements
            // remaining. Copy them over as-is — they are already sorted
            // and larger than everything in `result`.
            while (i < left.Length) result[k++] = left[i++];
            while (j < right.Length) result[k++] = right[j++];

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
            // Public entry point. We sort in place but also return the
            // same array so it's easy to chain calls.
            QuickSortInPlace(ints, 0, ints.Length - 1);
            return ints;
        }

        /// <summary>
        /// Sort arr[low..high] in place using recursive Quick Sort.
        /// </summary>
        private void QuickSortInPlace(int[] arr, int low, int high)
        {
            // Base case: a 0- or 1-element segment is already sorted.
            if (low >= high) return;

            // Partition the segment around a pivot. After this call
            // arr[pivotIndex] is in its final sorted position.
            int pivotIndex = Partition(arr, low, high);

            // Recurse on the left side  (everything <= pivot).
            QuickSortInPlace(arr, low, pivotIndex - 1);
            // Recurse on the right side (everything >  pivot).
            QuickSortInPlace(arr, pivotIndex + 1, high);
        }

        /// <summary>
        /// Lomuto partition scheme. Uses arr[high] as the pivot, then
        /// rearranges arr[low..high] so that all values <= pivot come
        /// before all values > pivot. Returns the pivot's final index.
        /// </summary>
        private int Partition(int[] arr, int low, int high)
        {
            // Choose the rightmost element as the pivot.
            int pivot = arr[high];

            // `i` tracks the END of the "<= pivot" region. It starts at
            // low-1 meaning that region is initially empty.
            int i = low - 1;

            // Scan every element in [low .. high-1] (skip the pivot itself).
            for (int j = low; j < high; j++)
            {
                // If arr[j] belongs on the "<= pivot" side, grow that
                // region by one and swap arr[j] into it.
                if (arr[j] <= pivot)
                {
                    i++;
                    (arr[i], arr[j]) = (arr[j], arr[i]);
                }
            }

            // Finally, swap the pivot into the slot just after the
            // "<= pivot" region. That slot is its final sorted position.
            (arr[i + 1], arr[high]) = (arr[high], arr[i + 1]);
            return i + 1;
        }

        // ----------------------------------------------------------------
        // Heap Sort  (in-place, uses a binary max-heap embedded in arr)
        // Idea: Build a max-heap from the array, then repeatedly swap the
        // root (largest) with the last element, shrink the heap, and
        // re-heapify. Result is an ascending sorted array.
        // Best/Worst O(n log n), in-place O(1), Unstable.
        //
        // Heap stored as an array (0-based):
        //   parent(i) = (i - 1) / 2
        //   left(i)   = 2*i + 1
        //   right(i)  = 2*i + 2
        // Max-heap invariant: arr[parent] >= arr[child] for every node.
        // ----------------------------------------------------------------
        public int[] HeapSort(int[] ints)
        {
            int n = ints.Length;

            // Phase 1: Build a max-heap in place. We only need to
            // heapify the non-leaf nodes (indices 0 .. n/2 - 1) because
            // every leaf is trivially a valid heap of size 1.
            // Iterating bottom-up ensures children are already valid
            // heaps by the time we heapify their parent.
            for (int i = n / 2 - 1; i >= 0; i--)
                Heapify(ints, n, i);

            // Phase 2: Repeatedly extract the maximum.
            // The largest element always sits at index 0 (the root).
            // Swap it with the last element of the current heap, then
            // "shrink" the heap by one and restore the heap property
            // on the smaller heap.
            for (int end = n - 1; end > 0; end--)
            {
                // Move current max to its final sorted position at `end`.
                (ints[0], ints[end]) = (ints[end], ints[0]);

                // Re-heapify the remaining heap of size `end`.
                Heapify(ints, end, 0);
            }

            return ints;
        }

        /// <summary>
        /// Sift-down: assumes the subtrees rooted at left(root) and
        /// right(root) are already valid max-heaps, then pushes
        /// arr[root] downward until the subtree rooted at `root` is
        /// also a valid max-heap. Considers only indices [0..heapSize-1].
        /// </summary>
        private void Heapify(int[] arr, int heapSize, int root)
        {
            // Index of the largest value among (root, left child, right child).
            int largest = root;
            int left = 2 * root + 1;
            int right = 2 * root + 2;

            // Check the left child (if it exists inside the active heap).
            if (left < heapSize && arr[left] > arr[largest]) largest = left;
            // Check the right child.
            if (right < heapSize && arr[right] > arr[largest]) largest = right;

            // If the root was NOT the largest, swap it with the larger
            // child and recurse down into that child to keep restoring
            // the heap property level by level.
            if (largest != root)
            {
                (arr[root], arr[largest]) = (arr[largest], arr[root]);
                Heapify(arr, heapSize, largest);
            }
        }

        // ----------------------------------------------------------------
        // Counting Sort  (non-comparison sort)
        // Idea: Count how many times each value appears, then use the
        // running totals (prefix sums) to place each element directly into
        // its final sorted position. Works on integers in a known range.
        // Best/Worst O(n + k) where k = value range, O(k) extra space, Stable.
        //
        // NOTE: only practical when k (= max - min + 1) is not much
        // bigger than n. For huge ranges use Radix Sort or a comparison sort.
        //
        // Example walk-through for [4, 2, 2, 8, 3]:
        //   min=2, max=8, range=7  (offset each value by -2)
        //   counts (by value 2..8): [2, 1, 1, 0, 0, 0, 1]
        //   prefix:                  [2, 3, 4, 4, 4, 4, 5]   <- exclusive ends
        //   placement (right-to-left for stability): [2,2,3,4,8]
        // ----------------------------------------------------------------
        public int[] CountingSort(int[] ints)
        {
            // Edge case: empty input is already sorted.
            if (ints.Length == 0) return ints;

            // Find the value range so we can size the count array and
            // also support negative numbers via the `min` offset.
            int min = ints.Min();
            int max = ints.Max();
            int range = max - min + 1;

            // count[v - min] will hold the number of occurrences of value v.
            int[] count = new int[range];
            // output holds the final sorted result.
            int[] output = new int[ints.Length];

            // 1. Tally: count how many times each value appears.
            //    Shifting by -min lets us use a 0-based index even when
            //    the input contains negative numbers.
            foreach (int value in ints)
                count[value - min]++;

            // 2. Prefix sum: turn counts into "exclusive end positions".
            //    After this loop count[i] equals the number of input
            //    values that are <= (i + min), which is exactly the
            //    index AFTER the block of (i + min)s in the sorted output.
            for (int i = 1; i < range; i++)
                count[i] += count[i - 1];

            // 3. Place: iterate the input from RIGHT to LEFT so that
            //    equal values keep their original relative order
            //    (this is what makes Counting Sort STABLE).
            //    We decrement count[value - min] first, then write
            //    into that slot — so count[] always points to the
            //    next free slot for that value.
            for (int i = ints.Length - 1; i >= 0; i--)
            {
                int value = ints[i];
                output[--count[value - min]] = value;
            }

            return output;
        }
    }
}