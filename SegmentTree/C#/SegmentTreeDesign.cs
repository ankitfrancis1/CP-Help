using System;
using System.Collections.Generic;


namespace SegmentTreeDesign
{
    class SegmentTree<T>
    {
        public delegate T Operation(T a, T b);
        private readonly Operation Operate;
        private readonly T Identity;
        public int Length { get; set; }
        public int Capacity { get; set; }
        private T[] Tree;
        private readonly double LOAD_FACTOR = 2F;

        public SegmentTree(List<T> initalValues, Operation Operation, T identity)
        {
            this.Operate = Operation;
            this.Identity = identity;
            Length = Capacity = initalValues.Count;
            this.Tree = new T[2*Capacity - 1];
            BuildTree(0, Capacity - 1, 0, initalValues);
        }

        private int GetMid (int start, int end) { return start + (end - start) / 2; }
        private int LeftChild(int node) { return node + 1; }
        private int RightChild(int node, int start, int mid)
        {
            int leavesLeftSubtree = mid - start + 1;
            int nodesLeftSubtree = 2 * leavesLeftSubtree - 1;
            return node + nodesLeftSubtree + 1;
        }

        private void BuildTree(int rangeStart, int rangeEnd, int currentNode, List<T> initialValues) {
            if (rangeStart == rangeEnd)
            {
                Tree[currentNode] = initialValues[rangeStart];
                return;
            }
            int mid = GetMid(rangeStart, rangeEnd);
            //left Subtree
            BuildTree(rangeStart, mid, LeftChild(currentNode), initialValues);
            //right Subtree
            BuildTree(mid + 1, rangeEnd, RightChild(currentNode, rangeStart, mid),initialValues);
            //combine
            Tree[currentNode] = Operate(Tree[LeftChild(currentNode)], Tree[RightChild(currentNode, rangeStart, mid)]);
        }

        private void Resize(int newCapacity) 
        {
            List<T> values = new List<T>();
            for (int i = 0; i < Length; i++) 
            {
                values.Add(Get(i));
            }
            //append identity to match new size
            for (int i = Length; i < newCapacity; i++)
            {
                values.Add(Identity);
            }
            //rebuild the tree
            this.Tree = new T[2* newCapacity - 1];
            BuildTree(0, newCapacity - 1, 0, values);
            this.Capacity = newCapacity;
        }

        private void Set(int index, int rangeStart, int rangeEnd, int currentNode, T value)
        {
            if (rangeStart == rangeEnd && rangeEnd == index)
            {
                Tree[currentNode] = value;
                return;
            }
            int mid = GetMid(rangeStart, rangeEnd);
            if (index <= mid)
                Set(index, rangeStart, mid, LeftChild(currentNode), value);
            else
                Set(index, mid + 1, rangeEnd, RightChild(currentNode, rangeStart, mid), value);
            //as left and right child has changed. currentNode's value should change.
            Tree[currentNode] = Operate(Tree[LeftChild(currentNode)], Tree[RightChild(currentNode, rangeStart, mid)]);
        }

        private T QueryRange(int queryStart, int queryEnd, int rangeStart, int rangeEnd, int currentNode) 
        {
            /*
                qs                   qe
                ----------------------
                    -----------
                    rs        re
            */
            //The current node represents a range that is fully contained within the query
            // so we need the value of this node completely
            if (queryStart <= rangeStart && rangeEnd <= queryEnd) 
            {    
                return Tree[currentNode];   
            }
            /*
                  qs       qe                  or                    qs       qe
                  ----------                                       ----------
                               rs       re            rs       re 
                               ----------             ----------  
            */
            //the query has no overlap with the range of the currentNode
            if (queryStart > rangeEnd || rangeStart > queryEnd) 
            {
                return Identity;
            }
            int mid = GetMid(rangeStart, rangeEnd);
            T leftValue = QueryRange(queryStart, queryEnd, rangeStart, mid, LeftChild(currentNode));
            T rightValue = QueryRange(queryStart, queryEnd, mid + 1, rangeEnd, RightChild(currentNode, rangeStart, mid));
            return Operate(leftValue, rightValue);
        }

        public T QueryRange(int start, int end) 
        {
            if (start < 0 || start >= this.Length) throw new ArgumentException("start: " + start);
            if (end < 0 || end >= this.Length) throw new ArgumentException("end: " + end);
            return QueryRange(start, end, 0, this.Capacity - 1, 0);
        }
        public T Get(int index)
        {
            if (index < 0 || index >= this.Length) throw new ArgumentException("index:"+index);
            return QueryRange(index, index);
        }
        public void Set(int index, T value)
        {
            if (index < 0 || index >= this.Length) throw new ArgumentException("index:" + index);
            Set(index, 0, this.Capacity - 1, 0, value);
        }

        public void Add(T value) 
        {
            if (this.Length == this.Capacity) 
            {
                Resize((int)(Capacity * LOAD_FACTOR) + 1);
            }
            this.Length++;
            Set(Length - 1, value);
        }
        public Operation GetOperation() { return this.Operate; }
        public T GetIdentity() { return this.Identity; }

        public int Count() { return this.Length; }
        public int GetCapacity() { return this.Capacity; }

        //helper methods for debugging.
        public void DisplayTree()
        {
            Console.WriteLine("[");
            for (int i = 0; i < this.Length; i++)
            {
                Console.Write(Get(i)+" ");
            }
            for (int i = this.Length; i < this.Capacity; i++)
            {
                Console.Write("_ ");
            }
            Console.WriteLine(" ]");
        }

    }

    class SegmentTreeDeletable<T> 
    {
        private readonly SegmentTree<T> segmentTree;
        private readonly SegmentTree<int> deletedIndices;
        private int Length;

        public SegmentTreeDeletable(List<T> initialValues, SegmentTree<T>.Operation operation, T identity)
        {
            segmentTree = new SegmentTree<T>(initialValues, operation, identity);
            List<int> deleted = new List<int>();
            for (int i = 0; i < segmentTree.Count(); i++)
            {
                deleted.Add(0);
            }
            this.deletedIndices = new SegmentTree<int>(deleted, (int a, int b) => a + b, 0);
            this.Length = initialValues.Count;
        }

        private int GetTrueIndex(int index) 
        {
            // trueIndex
            // [ a b c d ]
            //       2 (index)
            // [ 0 1 0 1 0 0]
            // [ a z b s c d]
            //           ^ (trueIndex)
            // trueIndex - index = number of deleted indices in the range (0...trueIndex)
            int low = index;
            int high = segmentTree.Count() - 1;
            while (low <= high)
            {
                int mid = low + (high - low) / 2;
                int deletedCount = deletedIndices.QueryRange(0, mid);
                if (mid - index < deletedCount) low = mid + 1;
                else high = mid - 1;
            }
            return low;
        }

        public T QueryRange(int start, int end) 
        {
            start = GetTrueIndex(start);
            end = GetTrueIndex(end);
            return segmentTree.QueryRange(start, end);
        }

        public void Set(int index, T value) 
        {
            index = GetTrueIndex(index);
            segmentTree.Set(index, value);
        }
        public void Add(T value) 
        {
            segmentTree.Add(value);
            deletedIndices.Add(0);
            Length++;
        }
        public T Get(int index)
        {
            index = GetTrueIndex(index);
            return segmentTree.Get(index);
        }
        public void Delete(int index) {
            if (Length == 0) throw new OverflowException("Underflow: segmentTree is empty");
            index = GetTrueIndex(index);
            //mark this index as deleted.
            deletedIndices.Set(index, 1);
            segmentTree.Set(index, segmentTree.GetIdentity());
            this.Length--;
        }

        public int Count() { return Length; }
        public int GetCapacity() 
        {
            int capacity = segmentTree.GetCapacity();
            int countDeleted = deletedIndices.QueryRange(0, capacity);
            return capacity - countDeleted;
        }
        public SegmentTree<T>.Operation getOperator() { return segmentTree.GetOperation(); }
        public T GetIdentity() { return segmentTree.GetIdentity(); }
        public void DisplayTree()
        {
            segmentTree.DisplayTree();
            deletedIndices.DisplayTree();
            Console.Write("Apparent Array [ ");
            for (int i = 0; i < Length; i++)
            {
                Console.Write(Get(i) + " ");
            }
            Console.WriteLine(" ]");
        }
    }
    
    /*
    public class Program
    {
        public static void Main(String[] args)
        {
            SegmentTreeDeletable<int> segTree = new SegmentTreeDeletable<int>(new List<int>{1,2,3}, ((int a, int b) => ((a < b) ? a : b)), int.MaxValue);
            for (int i = 3; i < 10; i++)
            {
                segTree.DisplayTree();
                segTree.Add(i * 10);
                if (i % 3 == 0)
                {
                    segTree.DisplayTree();
                    segTree.Delete(0);
                    Console.WriteLine("queryRange 0 to 2:{0}",segTree.QueryRange(0,2));
                }
            }
            segTree.DisplayTree();
        }
    }*/
}
