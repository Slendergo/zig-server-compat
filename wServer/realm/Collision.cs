namespace wServer.realm;

public class CollisionNode<T> where T : ICollidable<T> {
    public int Data;

    public CollisionNode<T> Next;

    /*  Bit field:
     *  0  - 7  X coordinate of chunk
     *  8  - 15 Y coordinate of chunk
     *  16 - 23 Collision Map Type
     *
     */
    public ICollidable<T> Parent;

    public CollisionNode<T> Previous;

    public void InsertAfter(CollisionNode<T> node) {
        if (Next != null) {
            node.Next = Next;
            Next.Previous = node;
        }
        else {
            node.Next = null;
        }

        node.Previous = this;
        Next = node;
    }

    public CollisionNode<T> Remove() {
        CollisionNode<T> ret = null;
        if (Previous != null) {
            ret = Previous;
            Previous.Next = Next;
        }

        if (Next != null) {
            ret = Next;
            Next.Previous = Previous;
        }

        Previous = null;
        Next = null;
        return ret;
    }
}

public interface ICollidable<T> where T : ICollidable<T> {
    CollisionMap<T> Parent { get; set; }
    CollisionNode<T> CollisionNode { get; set; }
    float X { get; }
    float Y { get; }
}

//TODO: thread-safe?
public class CollisionMap<T> where T : ICollidable<T> {
    public const int CHUNK_SIZE = 16;

    private const int ACTIVE_RADIUS = 3;
    private CollisionNode<T>[,] chunks;
    private byte type;
    private int w, h, cW, cH;

    public CollisionMap(byte type, int w, int h) {
        this.type = type;
        chunks = new CollisionNode<T>[
            cW = (w + CHUNK_SIZE - 1) / CHUNK_SIZE,
            cH = (h + CHUNK_SIZE - 1) / CHUNK_SIZE];
        this.w = w;
        this.h = h;
    }

    private int GetData(int chunkX, int chunkY) {
        return chunkX | (chunkY << 8) | (type << 16);
    }

    public void Insert(T obj) {
        if (obj.CollisionNode != null)
            throw new InvalidOperationException("Object already added into collision map.");

        var x = (int) (obj.X / CHUNK_SIZE);
        var y = (int) (obj.Y / CHUNK_SIZE);
        obj.CollisionNode = new CollisionNode<T> {
            Data = GetData(x, y),
            Parent = obj
        };
        obj.Parent = this;

        if (chunks[x, y] == null)
            chunks[x, y] = obj.CollisionNode;
        else
            chunks[x, y].InsertAfter(obj.CollisionNode);
    }

    public void Move(T obj, float newX, float newY) {
        if (obj.CollisionNode == null) {
            Insert(obj);
            return;
        }

        if (obj.Parent != this)
            throw new InvalidOperationException("Cannot move object accoss different map.");

        var x = (int) (newX / CHUNK_SIZE);
        var y = (int) (newY / CHUNK_SIZE);
        var newDat = GetData(x, y);
        if (obj.CollisionNode.Data != newDat) {
            var oldX = (int) (obj.X / CHUNK_SIZE);
            var oldY = (int) (obj.Y / CHUNK_SIZE);
            if (chunks[oldX, oldY] == obj.CollisionNode)
                chunks[oldX, oldY] = obj.CollisionNode.Remove();
            else
                obj.CollisionNode.Remove();

            if (chunks[x, y] == null)
                chunks[x, y] = obj.CollisionNode;
            else
                chunks[x, y].InsertAfter(obj.CollisionNode);
            obj.CollisionNode.Data = newDat;
        }
    }

    public void Remove(T obj) {
        if (obj.CollisionNode == null)
            return;
        if (obj.Parent != this)
            throw new InvalidOperationException("Cannot remove object accoss different map.");

        var x = (int) (obj.X / CHUNK_SIZE);
        var y = (int) (obj.Y / CHUNK_SIZE);
        if (chunks[x, y] == obj.CollisionNode)
            chunks[x, y] = obj.CollisionNode.Remove();
        else
            obj.CollisionNode.Remove();

        obj.CollisionNode = null;
        obj.Parent = null;
    }

    public IEnumerable<T> HitTest(Position pos, double radius) {
        return HitTest(pos.X, pos.Y, radius);
    }

    public IEnumerable<T> HitTest(double _x, double _y, double radius) {
        var xl = Math.Max(0, (int) (_x - radius) / CHUNK_SIZE);
        var xh = Math.Min(cW - 1, (int) (_x + radius) / CHUNK_SIZE);
        var yl = Math.Max(0, (int) (_y - radius) / CHUNK_SIZE);
        var yh = Math.Min(cH - 1, (int) (_y + radius) / CHUNK_SIZE);
        for (var y = yl; y <= yh; y++)
        for (var x = xl; x <= xh; x++) {
            var node = chunks[x, y];
            while (node != null) {
                yield return (T) node.Parent;
                node = node.Next;
            }
        }
    }

    public IEnumerable<T> HitTest(double _x, double _y) {
        if (_x < 0 || _x >= w || _y <= 0 || _y >= h) yield break;
        var x = (int) _x / CHUNK_SIZE;
        var y = (int) _y / CHUNK_SIZE;
        var node = chunks[x, y];
        while (node != null) {
            yield return (T) node.Parent;
            node = node.Next;
        }
    }

    public IEnumerable<T> GetActiveChunks(CollisionMap<T> from) {
        if (from.w != w || from.h != h)
            throw new ArgumentException("from");

        var ret = new HashSet<T>();
        for (var y = 0; y < cH; y++)
        for (var x = 0; x < cW; x++)
            if (from.chunks[x, y] != null)
                for (var i = -ACTIVE_RADIUS; i <= ACTIVE_RADIUS; i++)
                for (var j = -ACTIVE_RADIUS; j <= ACTIVE_RADIUS; j++) {
                    if (x + j < 0 || x + j >= cW || y + i < 0 || y + i >= cH)
                        continue;
                    var node = chunks[x + j, y + i];
                    while (node != null) {
                        ret.Add((T) node.Parent);
                        node = node.Next;
                    }
                }

        return ret;
    }
}