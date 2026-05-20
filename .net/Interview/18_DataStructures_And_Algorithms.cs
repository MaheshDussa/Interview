// =====================================================================
//  18) DATA STRUCTURES & ALGORITHMS — Interview Q&A
// =====================================================================
//  Goal: a clean, self-explanatory cheat-sheet of the data structures
//        and algorithms most frequently asked in .NET interviews.
//        Each section has:
//          - What it is (one liner)
//          - When to use it
//          - Big-O at a glance
//          - A small, runnable C# example
// =====================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Interview.DataStructuresAndAlgorithms
{
    // =====================================================================
    //  PART A : DATA STRUCTURES
    // =====================================================================

    // ---------------------------------------------------------------------
    // Q1: Array vs List<T> in .NET?
    // A : Array       -> fixed size, contiguous memory, fastest indexing.
    //     List<T>     -> dynamic array, doubles capacity on overflow.
    //     Both are O(1) index, O(n) search, O(n) insert/remove at middle.
    // ---------------------------------------------------------------------
    public static class ArrayVsList
    {
        public static void Demo()
        {
            int[] arr = { 1, 2, 3 };          // fixed length
            var list = new List<int> { 1, 2 }; // grows automatically
            list.Add(3);

            Console.WriteLine(arr[0]);    // O(1) index
            Console.WriteLine(list[1]);   // O(1) index
        }
    }

    // ---------------------------------------------------------------------
    // Q2: Singly vs Doubly Linked List?
    // A : Singly  -> each node has Next only.
    //     Doubly  -> each node has Next AND Prev (LinkedList<T> in .NET).
    //     Use linked list when frequent insert/remove at head/middle
    //     and indexing is NOT needed. Access by index is O(n).
    // ---------------------------------------------------------------------
    public class SinglyLinkedList<T>
    {
        private class Node { public T Value; public Node? Next; public Node(T v) { Value = v; } }

        private Node? _head;

        // Insert at front — O(1)
        public void AddFirst(T value)
        {
            var node = new Node(value) { Next = _head };
            _head = node;
        }

        // Linear search — O(n)
        public bool Contains(T value)
        {
            for (var cur = _head; cur != null; cur = cur.Next)
                if (EqualityComparer<T>.Default.Equals(cur.Value, value)) return true;
            return false;
        }

        // Reverse in place — O(n), O(1) extra space
        public void Reverse()
        {
            Node? prev = null, cur = _head;
            while (cur != null)
            {
                var next = cur.Next;
                cur.Next = prev;
                prev = cur;
                cur = next;
            }
            _head = prev;
        }
    }

    // ---------------------------------------------------------------------
    // Q3: Stack vs Queue?
    // A : Stack<T> -> LIFO (Last In First Out). Push / Pop / Peek — O(1).
    //                 Used for: undo, recursion, expression parsing.
    //     Queue<T> -> FIFO (First In First Out). Enqueue / Dequeue — O(1).
    //                 Used for: BFS, scheduling, producer-consumer.
    // ---------------------------------------------------------------------
    public static class StackQueueDemo
    {
        public static void Demo()
        {
            var stack = new Stack<int>();
            stack.Push(1); stack.Push(2);
            Console.WriteLine(stack.Pop()); // 2

            var queue = new Queue<int>();
            queue.Enqueue(1); queue.Enqueue(2);
            Console.WriteLine(queue.Dequeue()); // 1
        }
    }

    // ---------------------------------------------------------------------
    // Q4: HashSet<T> and Dictionary<TKey,TValue>?
    // A : Both backed by a hash table.
    //     HashSet<T>           -> unique values, O(1) avg add/contains.
    //     Dictionary<TK,TV>    -> unique keys -> values, O(1) avg lookup.
    //     Worst case O(n) on heavy collisions.
    //     Keys must have a stable GetHashCode() + Equals().
    // ---------------------------------------------------------------------
    public static class HashCollections
    {
        public static int FirstDuplicate(int[] nums)
        {
            // Classic interview question solved with HashSet — O(n) time, O(n) space.
            var seen = new HashSet<int>();
            foreach (var n in nums)
            {
                if (!seen.Add(n)) return n; // Add returns false if already present
            }
            return -1;
        }
    }

    // ---------------------------------------------------------------------
    // Q5: Binary Tree vs Binary Search Tree (BST)?
    // A : Binary tree           -> any node has up to 2 children.
    //     Binary Search Tree    -> left subtree < node < right subtree.
    //     BST search/insert/delete: O(log n) balanced, O(n) skewed.
    //     Self-balancing variants: AVL, Red-Black (used by SortedDictionary).
    // ---------------------------------------------------------------------
    public class BinarySearchTree
    {
        private class Node { public int Value; public Node? Left, Right; public Node(int v) { Value = v; } }
        private Node? _root;

        // Insert — O(log n) average
        public void Insert(int value) => _root = Insert(_root, value);
        private static Node Insert(Node? node, int value)
        {
            if (node == null) return new Node(value);
            if (value < node.Value) node.Left = Insert(node.Left, value);
            else if (value > node.Value) node.Right = Insert(node.Right, value);
            return node;
        }

        // In-order traversal yields a sorted sequence.
        public IEnumerable<int> InOrder() => InOrder(_root);
        private static IEnumerable<int> InOrder(Node? node)
        {
            if (node == null) yield break;
            foreach (var v in InOrder(node.Left)) yield return v;
            yield return node.Value;
            foreach (var v in InOrder(node.Right)) yield return v;
        }
    }

    // ---------------------------------------------------------------------
    // Q6: What is a Heap / Priority Queue?
    // A : A complete binary tree where parent <= children (min-heap) or
    //     parent >= children (max-heap). Backed by an array.
    //     Insert / ExtractTop -> O(log n). Peek -> O(1).
    //     .NET 6+ ships PriorityQueue<TElement,TPriority>.
    //     Used for: Dijkstra, top-K, scheduling.
    // ---------------------------------------------------------------------
    public static class HeapDemo
    {
        public static int[] TopKLargest(int[] nums, int k)
        {
            // Min-heap of size k; the smallest element of the heap is the
            // k-th largest overall once we have processed everything.
            var pq = new PriorityQueue<int, int>();
            foreach (var n in nums)
            {
                pq.Enqueue(n, n);
                if (pq.Count > k) pq.Dequeue();
            }
            var result = new int[pq.Count];
            for (int i = result.Length - 1; i >= 0; i--) result[i] = pq.Dequeue();
            return result;
        }
    }

    // ---------------------------------------------------------------------
    // Q7: What is a Graph and how do we represent it?
    // A : A set of vertices V and edges E. Two common representations:
    //       Adjacency list  -> Dictionary<TNode, List<TNode>>  (sparse)
    //       Adjacency matrix-> bool[,] / int[,]                (dense)
    //     Traversals: BFS (queue, shortest path in unweighted graph)
    //                 DFS (stack/recursion, cycle detect, topological sort)
    // ---------------------------------------------------------------------
    public class Graph
    {
        private readonly Dictionary<int, List<int>> _adj = new();

        public void AddEdge(int u, int v)
        {
            if (!_adj.ContainsKey(u)) _adj[u] = new List<int>();
            if (!_adj.ContainsKey(v)) _adj[v] = new List<int>();
            _adj[u].Add(v);
            _adj[v].Add(u); // undirected
        }

        // BFS — shortest path in edges from source.
        public IEnumerable<int> Bfs(int source)
        {
            var visited = new HashSet<int> { source };
            var queue = new Queue<int>();
            queue.Enqueue(source);

            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                yield return node;
                foreach (var next in _adj[node])
                    if (visited.Add(next)) queue.Enqueue(next);
            }
        }

        // DFS — recursive.
        public IEnumerable<int> Dfs(int source)
        {
            var visited = new HashSet<int>();
            return DfsCore(source, visited);
        }
        private IEnumerable<int> DfsCore(int node, HashSet<int> visited)
        {
            if (!visited.Add(node)) yield break;
            yield return node;
            foreach (var next in _adj[node])
                foreach (var v in DfsCore(next, visited)) yield return v;
        }
    }

    // ---------------------------------------------------------------------
    // Q8: What is a Trie (prefix tree)?
    // A : Tree where each edge is a character. Used for:
    //       autocomplete, spell-check, IP routing, dictionary lookup.
    //     Insert / Search by word of length L -> O(L), independent of N.
    // ---------------------------------------------------------------------
    public class Trie
    {
        private class Node { public Dictionary<char, Node> Next = new(); public bool IsEnd; }
        private readonly Node _root = new();

        public void Insert(string word)
        {
            var cur = _root;
            foreach (var ch in word)
            {
                if (!cur.Next.TryGetValue(ch, out var nxt))
                    cur.Next[ch] = nxt = new Node();
                cur = nxt;
            }
            cur.IsEnd = true;
        }

        public bool Search(string word) => Find(word)?.IsEnd == true;
        public bool StartsWith(string prefix) => Find(prefix) != null;

        private Node? Find(string s)
        {
            var cur = _root;
            foreach (var ch in s)
            {
                if (!cur.Next.TryGetValue(ch, out var nxt)) return null;
                cur = nxt;
            }
            return cur;
        }
    }


    // =====================================================================
    //  PART B : ALGORITHMS
    // =====================================================================

    // ---------------------------------------------------------------------
    // Q9: Big-O cheat sheet for common operations.
    //
    //   Structure           | Access | Search | Insert | Delete
    //   --------------------|--------|--------|--------|--------
    //   Array               | O(1)   | O(n)   | O(n)   | O(n)
    //   Sorted Array        | O(1)   | O(log n)| O(n)   | O(n)
    //   Linked List         | O(n)   | O(n)   | O(1)*  | O(1)*
    //   Stack / Queue       | O(n)   | O(n)   | O(1)   | O(1)
    //   HashSet/Dictionary  | -      | O(1)~  | O(1)~  | O(1)~
    //   BST (balanced)      | O(log n)| O(log n)| O(log n)| O(log n)
    //   Heap                | -      | O(n)   | O(log n)| O(log n)
    //   Trie (len L)        | -      | O(L)   | O(L)   | O(L)
    //
    //   (* given a reference to the node)
    //   (~ amortized, assuming a good hash function)
    // ---------------------------------------------------------------------

    // ---------------------------------------------------------------------
    // Q10: Sorting algorithms recap.
    //   See /leetcode/sorting.cs for full implementations.
    //
    //   Algorithm        | Best        | Worst       | Space    | Stable?
    //   -----------------|-------------|-------------|----------|---------
    //   Bubble Sort      | O(n)        | O(n^2)      | O(1)     | Stable
    //   Selection Sort   | O(n^2)      | O(n^2)      | O(1)     | Unstable
    //   Insertion Sort   | O(n)        | O(n^2)      | O(1)     | Stable
    //   Merge Sort       | O(n log n)  | O(n log n)  | O(n)     | Stable
    //   Quick Sort       | O(n log n)  | O(n^2)      | O(log n) | Unstable
    //   Heap Sort        | O(n log n)  | O(n log n)  | O(1)     | Unstable
    //   Counting Sort    | O(n + k)    | O(n + k)    | O(k)     | Stable
    //
    //   Rule of thumb: use Array.Sort / List<T>.Sort (introsort, O(n log n)).
    // ---------------------------------------------------------------------

    // ---------------------------------------------------------------------
    // Q11: Searching algorithms recap.
    //   See /leetcode/searching.cs for full implementations.
    //
    //   Algorithm             | Worst       | Input requirement
    //   ----------------------|-------------|-----------------------------
    //   Linear Search         | O(n)        | None
    //   Binary Search         | O(log n)    | Sorted array
    //   Jump Search           | O(sqrt n)   | Sorted array
    //   Interpolation Search  | O(log log n)| Sorted + uniform distribution
    //   Exponential Search    | O(log n)    | Sorted (or unbounded) array
    //   Ternary Search        | O(log3 n)   | Sorted array
    // ---------------------------------------------------------------------

    // ---------------------------------------------------------------------
    // Q12: Recursion vs Iteration?
    // A : Recursion uses the call stack; cleaner for divide-and-conquer
    //     (merge sort, tree traversals). Watch out for StackOverflow.
    //     Convert to iteration when depth can be large; use an explicit
    //     Stack<T>/Queue<T> if needed.
    // ---------------------------------------------------------------------
    public static class RecursionDemo
    {
        // Recursive factorial — clear but O(n) stack frames.
        public static long FactorialRec(int n) => n <= 1 ? 1 : n * FactorialRec(n - 1);

        // Iterative factorial — O(1) stack.
        public static long FactorialIter(int n)
        {
            long result = 1;
            for (int i = 2; i <= n; i++) result *= i;
            return result;
        }
    }

    // ---------------------------------------------------------------------
    // Q13: Dynamic Programming (DP) — what and when?
    // A : DP = recursion + memoization (or bottom-up tabulation) used when
    //     a problem has:
    //       1) Overlapping sub-problems
    //       2) Optimal sub-structure
    //     Examples: Fibonacci, Knapsack, LCS, Coin Change, Edit Distance.
    // ---------------------------------------------------------------------
    public static class DynamicProgramming
    {
        // Fibonacci — bottom-up DP, O(n) time, O(1) space.
        public static long Fib(int n)
        {
            if (n < 2) return n;
            long a = 0, b = 1;
            for (int i = 2; i <= n; i++)
            {
                var c = a + b;
                a = b;
                b = c;
            }
            return b;
        }

        // 0/1 Knapsack — classic DP, O(n * capacity).
        public static int Knapsack(int[] weights, int[] values, int capacity)
        {
            int n = weights.Length;
            var dp = new int[n + 1, capacity + 1];

            for (int i = 1; i <= n; i++)
            {
                for (int w = 0; w <= capacity; w++)
                {
                    dp[i, w] = dp[i - 1, w]; // skip item
                    if (weights[i - 1] <= w)
                    {
                        // include item i-1
                        dp[i, w] = Math.Max(dp[i, w],
                                            dp[i - 1, w - weights[i - 1]] + values[i - 1]);
                    }
                }
            }
            return dp[n, capacity];
        }
    }

    // ---------------------------------------------------------------------
    // Q14: Greedy algorithms — what and when?
    // A : Make the locally optimal choice at each step hoping the result
    //     is globally optimal. Works only when the problem has the
    //     "greedy choice property" (e.g., Huffman coding, activity
    //     selection, Dijkstra's shortest path, minimum coin change with
    //     canonical coin systems).
    // ---------------------------------------------------------------------
    public static class GreedyDemo
    {
        // Activity Selection: pick the maximum number of non-overlapping
        // activities. Greedy: always pick the next activity that finishes earliest.
        public static int MaxActivities((int start, int end)[] activities)
        {
            var sorted = activities.OrderBy(a => a.end).ToArray();
            int count = 0, lastEnd = int.MinValue;
            foreach (var a in sorted)
            {
                if (a.start >= lastEnd) { count++; lastEnd = a.end; }
            }
            return count;
        }
    }

    // ---------------------------------------------------------------------
    // Q15: Divide and Conquer
    // A : 1) Divide the problem into smaller sub-problems.
    //     2) Solve each recursively.
    //     3) Combine the results.
    //     Classic examples: Merge Sort, Quick Sort, Binary Search,
    //     Closest Pair of Points, Strassen's matrix multiplication.
    // ---------------------------------------------------------------------

    // ---------------------------------------------------------------------
    // Q16: Two-Pointer technique
    // A : Maintain two indices that move toward/with each other to
    //     achieve O(n) instead of O(n^2). Works on sorted arrays /
    //     strings. Examples: pair sum, palindrome check, removing
    //     duplicates in-place, container with most water.
    // ---------------------------------------------------------------------
    public static class TwoPointer
    {
        // Returns indices (i, j) such that sortedArr[i] + sortedArr[j] == target, or (-1,-1).
        public static (int, int) PairSumSorted(int[] sortedArr, int target)
        {
            int left = 0, right = sortedArr.Length - 1;
            while (left < right)
            {
                int sum = sortedArr[left] + sortedArr[right];
                if (sum == target) return (left, right);
                if (sum < target) left++;
                else right--;
            }
            return (-1, -1);
        }
    }

    // ---------------------------------------------------------------------
    // Q17: Sliding Window technique
    // A : Maintain a window [left..right] over an array/string and slide
    //     it instead of recomputing from scratch. Turns many O(n^2)
    //     problems into O(n). Examples: max sum of size-k subarray,
    //     longest substring without repeats, min window containing T.
    // ---------------------------------------------------------------------
    public static class SlidingWindow
    {
        // Max sum of any subarray of size k — O(n).
        public static int MaxSubarraySum(int[] arr, int k)
        {
            if (arr.Length < k) return 0;

            int windowSum = 0;
            for (int i = 0; i < k; i++) windowSum += arr[i];

            int best = windowSum;
            for (int i = k; i < arr.Length; i++)
            {
                windowSum += arr[i] - arr[i - k]; // slide by 1
                if (windowSum > best) best = windowSum;
            }
            return best;
        }
    }

    // ---------------------------------------------------------------------
    // Q18: Backtracking
    // A : DFS over the solution space, undoing ("backtracking") choices
    //     that lead to dead ends. Use for: permutations, combinations,
    //     N-Queens, Sudoku, word search, subset sum.
    // ---------------------------------------------------------------------
    public static class Backtracking
    {
        // All permutations of distinct integers.
        public static IList<IList<int>> Permutations(int[] nums)
        {
            var result = new List<IList<int>>();
            Backtrack(nums, new List<int>(), new bool[nums.Length], result);
            return result;
        }

        private static void Backtrack(int[] nums, List<int> current, bool[] used, List<IList<int>> result)
        {
            if (current.Count == nums.Length)
            {
                result.Add(new List<int>(current)); // snapshot
                return;
            }
            for (int i = 0; i < nums.Length; i++)
            {
                if (used[i]) continue;
                used[i] = true;
                current.Add(nums[i]);

                Backtrack(nums, current, used, result);

                // Undo the choice — this is the "backtrack" step.
                used[i] = false;
                current.RemoveAt(current.Count - 1);
            }
        }
    }

    // ---------------------------------------------------------------------
    // Q19: Bit manipulation tricks worth knowing.
    //   x & 1            -> 1 if x is odd, else 0
    //   x & (x - 1)      -> clears the lowest set bit (count set bits)
    //   x ^ x            -> 0   (XOR trick: find unique number in pairs)
    //   x << k           -> multiply by 2^k
    //   x >> k           -> divide  by 2^k (signed)
    // ---------------------------------------------------------------------
    public static class BitTricks
    {
        public static int CountSetBits(int x)
        {
            int count = 0;
            while (x != 0) { x &= (x - 1); count++; }
            return count;
        }

        // Find the single number where every other number appears twice.
        public static int SingleNumber(int[] nums)
        {
            int x = 0;
            foreach (var n in nums) x ^= n;
            return x;
        }
    }

    // ---------------------------------------------------------------------
    // Q20: How to approach an algorithm question in an interview?
    //   1. Restate the problem and clarify constraints (size, range, nulls).
    //   2. Walk through small examples by hand.
    //   3. Start with a brute-force solution; state its Big-O.
    //   4. Identify bottlenecks; pick a better data structure or pattern
    //      (hashing, two-pointer, sliding window, DP, greedy, divide & conquer).
    //   5. Code cleanly; name variables clearly; handle edge cases
    //      (empty input, single element, duplicates, overflow).
    //   6. Test with edge cases and analyze final Big-O time & space.
    // ---------------------------------------------------------------------
}
