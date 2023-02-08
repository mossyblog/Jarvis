namespace FJarvis.Data.Traits;

public interface IBinarySearchTree<T>
{
    // Properties
    T Root { get; }
    int Count { get; }

    // Methods
    void Add(T item);
    void Remove(T item);
    bool Contains(T item);
    T Find(T item);
    IEnumerable<T> GetChildren(T item);
    IEnumerable<T> GetParents(T item);
}

// This class will be used to manage the Entity & their relationships