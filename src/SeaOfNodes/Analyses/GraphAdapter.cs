using Reko.Core.Graphs;
using SeaOfNodes.Nodes;
using System.Collections;

namespace SeaOfNodes.Analyses;

/// <summary>
/// Wrapper around the graph of <see cref="Node">Nodes</see> to 
/// facilitate graph traversals.
/// </summary>
public class GraphAdapter : DirectedGraph<Node>
{
    public GraphAdapter()
    {
    }

    public ICollection<Node> Nodes => throw new NotImplementedException();

    public void AddEdge(Node nodeFrom, Node nodeTo)
    {
        throw new NotImplementedException();
    }

    public bool ContainsEdge(Node nodeFrom, Node nodeTo)
    {
        throw new NotImplementedException();
    }

    public ICollection<Node> Predecessors(Node node)
    {
        throw new NotImplementedException();
    }

    public void RemoveEdge(Node nodeFrom, Node nodeTo)
    {
        throw new NotImplementedException();
    }

    public ICollection<Node> Successors(Node node)
    {
        return new NullFilteringCollection<Node>(node.InNodes);
    }

    private class NullFilteringCollection<T> : ICollection<T>
        where T : class
    {
        private IReadOnlyCollection<T?> items;

        public NullFilteringCollection(IReadOnlyCollection<T?> items)
        {
            this.items = items;
        }

        public int Count => throw new NotImplementedException();

        public bool IsReadOnly => throw new NotImplementedException();

        public void Add(T item)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(T item)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var item in items)
            {
                if (item is not null)
                    yield return item;
            }
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
    }
}