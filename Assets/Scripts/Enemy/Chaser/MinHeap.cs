using UnityEngine;
using System.Collections.Generic;

public class MinHeap<T>
{
    private readonly List<(T item, int priority)> _heap = new List<(T, int)>();
    public int Count => _heap.Count;

    public void Enqueue(T item, int priority)
    {
        _heap.Add((item, priority));
        HeapifyUp(_heap.Count - 1);
    }

    public T Dequeue()
    {
        T root = _heap[0].item;
        _heap[0] = _heap[_heap.Count - 1];
        _heap.RemoveAt(_heap.Count - 1);

        if (_heap.Count > 0)
        {
            HeapifyDown(0);
        }

        return root;
    }

    private void HeapifyUp(int index)
    {
        while (index > 0)
        {
            int parent = (index - 1) / 2;
            if (_heap[index].priority >= _heap[parent].priority)
            {
                break;
            }

            (_heap[index], _heap[parent]) = (_heap[parent], _heap[index]);
            index = parent;
        }
    }

    private void HeapifyDown(int index)
    {
        int lastIndex = _heap.Count - 1;

        while (true)
        {
            int left = index * 2 + 1;
            int right = index * 2 + 2;
            int smallest = index;

            if (left <= lastIndex && _heap[left].priority < _heap[smallest].priority)
            {
                smallest = left;
            }

            if (right <= lastIndex && _heap[right].priority < _heap[smallest].priority)
            {
                smallest = right;
            }

            if (smallest == index)
            {
                break;
            }

            (_heap[index], _heap[smallest]) = (_heap[smallest], _heap[index]);
            index = smallest;
        }
    }
}
