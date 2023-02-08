namespace FJarvis.Data.Traits;

public class BinarySearchTree
{
    public Guid Id { get; set; }
    public BinarySearchTree Left { get; set; }
    public BinarySearchTree Right { get; set; }

    public BinarySearchTree(Guid guid)
    {
        Id = guid;
    }

    // Insert a new node into the Binary Search Tree
    public void Insert(Guid guid)
    {
        if (guid.CompareTo(Id) < 0)
        {
            if (Left == null)
            {
                Left = new BinarySearchTree(guid);
            }
            else
            {
                Left.Insert(guid);
            }
        }
        else
        {
            if (Right == null)
            {
                Right = new BinarySearchTree(guid);
            }
            else
            {
                Right.Insert(guid);
            }
        }
    }
}