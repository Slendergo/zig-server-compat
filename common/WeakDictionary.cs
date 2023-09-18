﻿using System.Collections;
using System.Diagnostics;

namespace common;

// Adds strong typing to WeakReference.Target using generics. Also,
// the Create factory method is used in place of a constructor
// to handle the case where target is null, but we want the 
// reference to still appear to be alive.
public class WeakReference<T> : WeakReference where T : class {
    protected WeakReference(T target)
        : base(target, false) { }

    public new T Target => (T) base.Target;

    public static WeakReference<T> Create(T target) {
        if (target == null)
            return WeakNullReference<T>.Singleton;

        return new WeakReference<T>(target);
    }
}

// Provides a weak reference to a null target object, which, unlike
// other weak references, is always considered to be alive. This 
// facilitates handling null dictionary values, which are perfectly
// legal.
internal class WeakNullReference<T> : WeakReference<T> where T : class {
    public static readonly WeakNullReference<T> Singleton = new();

    private WeakNullReference() : base(null) { }

    public override bool IsAlive => true;
}

// Provides a weak reference to an object of the given type to be used in
// a WeakDictionary along with the given comparer.
internal sealed class WeakKeyReference<T> : WeakReference<T> where T : class {
    public readonly int HashCode;

    public WeakKeyReference(T key, WeakKeyComparer<T> comparer)
        : base(key) {
        // retain the object's hash code immediately so that even
        // if the target is GC'ed we will be able to find and
        // remove the dead weak reference.
        HashCode = comparer.GetHashCode(key);
    }
}

// Compares objects of the given type or WeakKeyReferences to them
// for equality based on the given comparer. Note that we can only
// implement IEqualityComparer<T> for T = object as there is no 
// other common base between T and WeakKeyReference<T>. We need a
// single comparer to handle both types because we don't want to
// allocate a new weak reference for every lookup.
internal sealed class WeakKeyComparer<T> : IEqualityComparer<object>
    where T : class {
    private IEqualityComparer<T> comparer;

    internal WeakKeyComparer(IEqualityComparer<T> comparer) {
        if (comparer == null)
            comparer = EqualityComparer<T>.Default;

        this.comparer = comparer;
    }

    public int GetHashCode(object obj) {
        if (obj is WeakKeyReference<T> weakKey) return weakKey.HashCode;
        return comparer.GetHashCode((T) obj);
    }

    // Note: There are actually 9 cases to handle here.
    //
    //  Let Wa = Alive Weak Reference
    //  Let Wd = Dead Weak Reference
    //  Let S  = Strong Reference
    //  
    //  x  | y  | Equals(x,y)
    // -------------------------------------------------
    //  Wa | Wa | comparer.Equals(x.Target, y.Target) 
    //  Wa | Wd | false
    //  Wa | S  | comparer.Equals(x.Target, y)
    //  Wd | Wa | false
    //  Wd | Wd | x == y
    //  Wd | S  | false
    //  S  | Wa | comparer.Equals(x, y.Target)
    //  S  | Wd | false
    //  S  | S  | comparer.Equals(x, y)
    // -------------------------------------------------
    public new bool Equals(object x, object y) {
        var first = GetTarget(x, out var xIsDead);
        var second = GetTarget(y, out var yIsDead);

        if (xIsDead)
            return yIsDead ? x == y : false;

        if (yIsDead)
            return false;

        return comparer.Equals(first, second);
    }

    private static T GetTarget(object obj, out bool isDead) {
        T target;
        if (obj is WeakKeyReference<T> wref) {
            target = wref.Target;
            isDead = !wref.IsAlive;
        }
        else {
            target = (T) obj;
            isDead = false;
        }

        return target;
    }
}

/// <summary>
///     Represents a dictionary mapping keys to values.
/// </summary>
/// <remarks>
///     Provides the plumbing for the portions of IDictionary
///     <TKey,
///         TValue>
///         which can reasonably be implemented without any
///         dependency on the underlying representation of the dictionary.
/// </remarks>
[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(PREFIX + "DictionaryDebugView`2" + SUFFIX)]
public abstract class BaseDictionary<TKey, TValue> : IDictionary<TKey, TValue> {
    private const string PREFIX = "System.Collections.Generic.Mscorlib_";
    private const string SUFFIX = ",mscorlib,Version=2.0.0.0,Culture=neutral,PublicKeyToken=b77a5c561934e089";

    private KeyCollection keys;
    private ValueCollection values;

    public abstract int Count { get; }
    public abstract void Clear();
    public abstract void Add(TKey key, TValue value);
    public abstract bool ContainsKey(TKey key);
    public abstract bool Remove(TKey key);
    public abstract bool TryGetValue(TKey key, out TValue value);
    public abstract IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator();

    public bool IsReadOnly => false;

    public ICollection<TKey> Keys {
        get {
            if (keys == null)
                keys = new KeyCollection(this);

            return keys;
        }
    }

    public ICollection<TValue> Values {
        get {
            if (values == null)
                values = new ValueCollection(this);

            return values;
        }
    }

    public TValue this[TKey key] {
        get {
            if (!TryGetValue(key, out var value))
                throw new KeyNotFoundException();

            return value;
        }
        set => SetValue(key, value);
    }

    public void Add(KeyValuePair<TKey, TValue> item) {
        Add(item.Key, item.Value);
    }

    public bool Contains(KeyValuePair<TKey, TValue> item) {
        if (!TryGetValue(item.Key, out var value))
            return false;

        return EqualityComparer<TValue>.Default.Equals(value, item.Value);
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
        Copy(this, array, arrayIndex);
    }

    public bool Remove(KeyValuePair<TKey, TValue> item) {
        if (!Contains(item))
            return false;

        return Remove(item.Key);
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    protected abstract void SetValue(TKey key, TValue value);

    private static void Copy<T>(ICollection<T> source, T[] array, int arrayIndex) {
        if (array == null)
            throw new ArgumentNullException("array");

        if (arrayIndex < 0 || arrayIndex > array.Length)
            throw new ArgumentOutOfRangeException("arrayIndex");

        if (array.Length - arrayIndex < source.Count)
            throw new ArgumentException("Destination array is not large enough. Check array.Length and arrayIndex.");

        foreach (var item in source)
            array[arrayIndex++] = item;
    }

    private abstract class Collection<T> : ICollection<T> {
        protected readonly IDictionary<TKey, TValue> dictionary;

        protected Collection(IDictionary<TKey, TValue> dictionary) {
            this.dictionary = dictionary;
        }

        public int Count => dictionary.Count;

        public bool IsReadOnly => true;

        public void CopyTo(T[] array, int arrayIndex) {
            Copy(this, array, arrayIndex);
        }

        public virtual bool Contains(T item) {
            foreach (var element in this)
                if (EqualityComparer<T>.Default.Equals(element, item))
                    return true;
            return false;
        }

        public IEnumerator<T> GetEnumerator() {
            foreach (var pair in dictionary)
                yield return GetItem(pair);
        }

        public bool Remove(T item) {
            throw new NotSupportedException("Collection is read-only.");
        }

        public void Add(T item) {
            throw new NotSupportedException("Collection is read-only.");
        }

        public void Clear() {
            throw new NotSupportedException("Collection is read-only.");
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        protected abstract T GetItem(KeyValuePair<TKey, TValue> pair);
    }

    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(PREFIX + "DictionaryKeyCollectionDebugView`2" + SUFFIX)]
    private class KeyCollection : Collection<TKey> {
        public KeyCollection(IDictionary<TKey, TValue> dictionary)
            : base(dictionary) { }

        protected override TKey GetItem(KeyValuePair<TKey, TValue> pair) {
            return pair.Key;
        }

        public override bool Contains(TKey item) {
            return dictionary.ContainsKey(item);
        }
    }

    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy(PREFIX + "DictionaryValueCollectionDebugView`2" + SUFFIX)]
    private class ValueCollection : Collection<TValue> {
        public ValueCollection(IDictionary<TKey, TValue> dictionary)
            : base(dictionary) { }

        protected override TValue GetItem(KeyValuePair<TKey, TValue> pair) {
            return pair.Value;
        }
    }
}

public sealed class WeakDictionary<TKey, TValue> : BaseDictionary<TKey, TValue>
    where TKey : class {
    private WeakKeyComparer<TKey> comparer;

    private Dictionary<object, TValue> dictionary;

    public WeakDictionary()
        : this(0, null) { }

    public WeakDictionary(int capacity)
        : this(capacity, null) { }

    public WeakDictionary(IEqualityComparer<TKey> comparer)
        : this(0, comparer) { }

    public WeakDictionary(int capacity, IEqualityComparer<TKey> comparer) {
        this.comparer = new WeakKeyComparer<TKey>(comparer);
        dictionary = new Dictionary<object, TValue>(capacity, this.comparer);
    }

    // WARNING: The count returned here may include entries for which
    // either the key or value objects have already been garbage
    // collected. Call RemoveCollectedEntries to weed out collected
    // entries and update the count accordingly.
    public override int Count => dictionary.Count;

    public override void Add(TKey key, TValue value) {
        if (key == null) throw new ArgumentNullException("key");
        WeakReference<TKey> weakKey = new WeakKeyReference<TKey>(key, comparer);
        dictionary.Add(weakKey, value);
    }

    public override bool ContainsKey(TKey key) {
        return dictionary.ContainsKey(key);
    }

    public override bool Remove(TKey key) {
        return dictionary.Remove(key);
    }

    public override bool TryGetValue(TKey key, out TValue value) {
        if (dictionary.TryGetValue(key, out var weakValue)) {
            value = weakValue;
            return true;
        }

        value = default;
        return false;
    }

    protected override void SetValue(TKey key, TValue value) {
        WeakReference<TKey> weakKey = new WeakKeyReference<TKey>(key, comparer);
        dictionary[weakKey] = value;
    }

    public override void Clear() {
        dictionary.Clear();
    }

    public override IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
        foreach (var kvp in dictionary) {
            var weakKey = (WeakReference<TKey>) kvp.Key;
            var value = kvp.Value;
            var key = weakKey.Target;
            if (weakKey.IsAlive)
                yield return new KeyValuePair<TKey, TValue>(key, value);
        }
    }

    // Removes the left-over weak references for entries in the dictionary
    // whose key or value has already been reclaimed by the garbage
    // collector. This will reduce the dictionary's Count by the number
    // of dead key-value pairs that were eliminated.
    public void RemoveCollectedEntries() {
        List<object> toRemove = null;
        foreach (var pair in dictionary) {
            var weakKey = (WeakReference<TKey>) pair.Key;

            if (!weakKey.IsAlive) {
                if (toRemove == null)
                    toRemove = new List<object>();
                toRemove.Add(weakKey);
            }
        }

        if (toRemove != null)
            foreach (var key in toRemove)
                dictionary.Remove(key);
    }
}