using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Friflo.Engine.ECS;

// ReSharper disable UnusedType.Global
// ReSharper disable ArrangeTypeModifiers
// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable NotAccessedField.Local
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming
// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedParameter.Local
// ReSharper disable once CheckNamespace
namespace Demo;

// Most fields and properties declared in the types below are not utilized by the ECS.
// They are used to check and showcase rendering and editing in Explorer and Inspector.
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
#pragma warning disable CS0169 // The field is never used



#region components

struct Floats : IComponent
{
    public float    flt32;
    public double   flt64;
    public decimal  deci;
}

struct Enums : IComponent
{
    public  EnumByte    enumByte;
    public  WeekDay     weekDay;
    // [Flags]
    public  EnumFlags8  flags8;
    public  EnumFlags32 flags32;
    public  EnumFlags64 flags64;
    public  EnumRGBA    rgba;
    public  Layers      layers;
    // bool has [Flags] behavior
    public  bool        boolean;
}

struct Time : IComponent
{
    public DateTime value;
}

/// <summary>
/// Custom vector types <see cref="Coord2"/>, <see cref="Coord3"/>, <see cref="ColorRGB"/>, <see cref="ColorRGBA"/> are drawn with<br/>
/// custom TypeDrawer's: <see cref="Coord2Drawer"/>, <see cref="Coord3Drawer"/>, <see cref="ColorRGBDrawer"/>, <see cref="ColorRGBADrawer"/>.
/// </summary>
struct Vectors : IComponent
{
    public Coord2       coord2;
    public Coord3       coord3;
    public Vector2      vector2;
    public Vector3      vector3;
    public Vector4      vector4;

    [UiTypeDomain("color")]
    public Vector3      color3 = new Vector3(1, 0, 0);
    [UiTypeDomain("color")]
    public Vector4      color4 = new Vector4(0, 1, 0, 0);
    
    public ColorRGB     rgb;
    public ColorRGBA    rgba;
    
    public Vectors() {}
}

struct Integers : IComponent
{
    public sbyte    int8;
    public short    int16;
    public int      int32;
    public long     int64;
    
    public byte     uint8;
    public ushort   uint16;
    public uint     uint32;
    public ulong    uint64;
        
    public void InstanceMethod() {} // ensure it is ignored by reflection
}

/// <summary>The value of number fields annotated [UiDrag] can be changed with mouse dragging.</summary>
struct Drags : IComponent
{
    [UiDrag(1, 0,    100)]    public sbyte      int8;
    [UiDrag(1, 0,   1000)]    public short      int16;
    [UiDrag(1, 0, 10_000)]    public int        int32;
    
    [UiDrag(1, 0,    100)]    public byte       uint8;
    [UiDrag(1, 0,   1000)]    public ushort     uint16;
    [UiDrag(1, 0, 10_000)]    public uint       uint32;
    
    [UiDrag(1, 0, 1000)]      public float      flt32;
    
    [UiDrag(1, 0, 1000)]      public Coord2     coord2;
    [UiDrag(1, 0, 1000)]      public Coord3     coord3;
    [UiDrag(1, 0, 1000)]      public Vector2    vector2;
    [UiDrag(1, 0, 1000)]      public Vector3    vector3;
    [UiDrag(1, 0, 1000)]      public Vector4    vector4;
}

struct DragInt : IComponent
{
    [UiDrag(1, 0, 10_000)]    public int      value;
    
    public DragInt(int value) { this.value = value; }
}

struct CompA : IComponent {
    public int      a;
    public CompA(int value) { a = value; }
}

struct CompB : IComponent
{
    public int      b;
    public CompB(int value) { b = value; }
}

struct CompC : IComponent
{
    public int      c;
    public CompC(int value) { c = value; }
}

struct CompD : IComponent
{
    public int      d;
    public CompD(int value) { d = value; }
}

/// <summary>
/// Renders the abbreviation of an exception (#IOE -> InvalidOperationException) and its stacktrace as a tooltip.
/// </summary> 
struct Errors : IComponent
{
    public int      int32       => throw new InvalidOperationException("error int32");
    public EnumByte enumByte    => throw new InvalidOperationException("error enumByte");
}
    
struct Animated : IComponent
{
    public int      value;
}

struct EntityLink : IComponent
{
    public Entity   entity;
}

/// <summary>
/// Contains custom types having an arbitrary set of fields or properties.<br/>
/// Those types are declared by <c>struct, class or interface</c>.
/// </summary>
struct Objects : IComponent
{
    public Struct1                  struct1;
    public Class1                   class1;
    public AbstractClass            abstractClass;  // throws exception on: Inspector > Create AbstractClass
    public IInterface               @interface;     // throws exception on: Inspector > Create IInterface
    public Generic1<int>            generic1;
    public Generic1<Generic1<int>>  generic2;
    public dynamic                  dynamic;        // ignored in Inspector - has no members
    public object                   obj;            // ignored in Inspector - has no members
    public (int, int)               tuple;
    
    /// <summary>Called by the ECS when using copy / paste or calling <see cref="Entity.CopyEntity"/>.</summary>
    static void CopyValue(in Objects source, ref Objects target, in CopyContext context)
    {
        target.struct1  = source.struct1;
        if (source.class1 != null) {
            target.class1 = new Class1 {
                value1  = source.class1.value1,
                value2  = source.class1.value2,
                vector3 = source.class1.vector3
            };
        }
        target.abstractClass= source.abstractClass; // copy reference. Creating all polymorphic types is stressful and prone to errors
        target.@interface   = source.@interface;    // copy reference. Creating all polymorphic types is stressful and prone to errors
        target.generic1     = source.generic1;
        target.generic2     = source.generic2;
        target.dynamic      = source.dynamic;       // simply copy this crap type
        target.obj          = source.obj;           // simply copy this crap type
        target.tuple        = source.tuple;
    }
}

/// <summary>
/// A struct with string fields and properties.<br/>
/// <c>readonly</c> fields and readonly properties cannot be changed via UI.
/// </summary>
struct Strings : IComponent
{
    public          string  str;
    public          string  strProperty { get; set; }
    public readonly string  strReadonly;                        // value cannot be changed
    public          string  strGetter   => "no { set; }";       // value cannot be changed
    public          string  strSetter {                         // setter only is ignored
        set => throw new InvalidOperationException("setter");
    }
    public          string  this[int i] {                       // indexer is ignored (is a property with name: Item)
        get => throw new InvalidOperationException("indexer");
        set => throw new InvalidOperationException("indexer");
    }
    public          char    Char;
}

/// <summary>
/// Collection type members. They are shown as tables in Inspector if they are arrays or implement:<br/>
/// <see cref="IList{T}"/>, <see cref="ISet{T}"/>, <see cref="ICollection{T}"/>, <see cref="IEnumerable"/> or <see cref="IDictionary"/>.
/// </summary>
/// <remarks>
///     <b>Note!</b> This component exists only for Demo purposes.<br/>
///     Using similar types at scale is tempting but will degrade performance significant!
/// </remarks>
struct Collections : IComponent
{
    public ChildCollections         childCollections;
    public Sprite[]                 array;              // array
    public InlineItem4              inline4;            // IList<>
    public List<int>                intList;            // IList<>
    public List<string>             stringList;         // IList<>
    public List<Item>               itemList;           // IList<>
    public List<Struct2>            structs2;           // IList<>
    public List<Class1>             classes;            // IList<>
    public HashSet<int>             hashSet;            // ISet<>
    public HashSet<Item>            itemHashSet;        // ISet<>
    public HashSet<Class1>          classHashSet;       // ISet<>
    public LinkedList<Item>         linkedList;         // ICollection<>
    public Stack<int>               stack;              // IEnumerable<>
    public Queue<int>               queue;              // IEnumerable<>
    public Dictionary<int,int>      dictionary;         // IDictionary<,>
    public Dictionary<int,Class1>   classDictionary;    // IDictionary<,>
    
    public int?                     nullable;
    
    /// <summary>Called by the ECS when using copy / paste or calling <see cref="Entity.CopyEntity"/>.</summary> 
    private static void CopyValue(in Collections source, ref Collections target, in CopyContext context)
    {
        target.childCollections.Copy(source.childCollections);
        if (source.array != null) {
            target.array = new Sprite[source.array.Length];
            source.array.CopyTo(target.array, 0);
        }
        target.inline4 = source.inline4;
        if (source.intList != null) {
            target.intList = [..source.intList];
        }
        if (source.stringList != null) {
            target.stringList = [..source.stringList];
        }
        if (source.itemList != null) {
            target.itemList = [..source.itemList];
        }
        if (source.structs2 != null) {
            target.structs2 = [..source.structs2];
        }
        if (source.hashSet != null) {
            target.hashSet = [..source.hashSet];
        }
        if (source.itemHashSet != null) {
            target.itemHashSet = [..source.itemHashSet];
        }
        if (source.classHashSet != null) {
            target.classHashSet = [..source.classHashSet];
        }
        if (source.linkedList != null) {
            target.linkedList = new LinkedList<Item>(source.linkedList);
        }
        if (source.stack != null) {
            target.stack = new Stack<int>(source.stack);
        }
        if (source.queue != null) {
            target.queue = new Queue<int>(source.queue);
        }
        if (source.dictionary != null) {
            target.dictionary = new Dictionary<int, int>(source.dictionary);
        }
        if (source.classDictionary != null) {
            target.classDictionary = new Dictionary<int, Class1>(source.classDictionary);
        }
        target.nullable = source.nullable;
    }
}

struct Demo     : IComponent { }

struct TileSet : IComponent
{
    internal    string      texture;
    internal    int         spriteWidth;
    internal    int         spriteHeight;
    
    /// <summary>Called by the ECS when using copy / paste or calling <see cref="Entity.CopyEntity"/>.</summary>
    static void CopyValue(in TileSet source, ref TileSet target, in CopyContext context) {
        target.texture      = source.texture;
        target.spriteWidth  = source.spriteWidth;
        target.spriteHeight = source.spriteHeight;
    }
}

struct Tile : IComponent
{
    public Sprite   sprite;
    public Coord2   position;
}

// --- test inline components - components with only one field
struct RGB : IComponent
{
    public ColorRGB     rgb;
}

struct Flags8 : IComponent {
    public EnumFlags8   flags8;
}

struct Flags32 : IComponent {
    EnumFlags32 flags32;
}

struct InlineSprite : IComponent
{
    public Sprite   sprite;
}

struct InlineClass : IComponent
{
    public Class1    class1;
}

struct ItemArray : IComponent
{
    public Item[]   array;
    
    /// <summary>Called by the ECS when using copy / paste or calling <see cref="Entity.CopyEntity"/>.</summary>
    private static void CopyValue(in ItemArray source, ref ItemArray target, in CopyContext context) {
        if (source.array != null) {
            target.array = new Item[source.array.Length];
            source.array.CopyTo(target.array, 0);
        }
    }
}

/// <summary>
/// throws <see cref="MissingMethodException"/> when executing Cut or Copy command. Expects method:<br/>
/// static void CopyValue(in TestCopyError source, ref TestCopyError target, in CopyContext context)
/// </summary>
struct TestCopyError : IComponent
{
    int[]       array; // reference types require method: CopyValue() for Cut and Copy
    int         int32;
}

struct TestCycle1 : IComponent
{
    TestCycle1  childComponent => default;
    int         int32;
}

struct TestCycle2 : IComponent
{
    CycleStruct cycleStruct;
    int         int32;
}

struct TestCycle3 : IComponent
{
    CycleList    list;
    int          int32;
}


/// <summary>
/// class / struct fields are hidden in Inspector if they start with _ or annotated with <br/>
/// <c>[UiHide]</c> or <c>[DebuggerBrowsable(DebuggerBrowsableState.Never)]</c>
/// </summary>
struct HideMember : IComponent
{
            int     _hide1; // hidden
    [UiHide]int     hide2;  // hidden
    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            int     hide3;  // hidden
            int     value1; // visible
            int     value2; // visible
}
#endregion



#region relations
    struct IntRelation : IRelation<int>
    {
        public          int     value;              // key
        public          int     GetRelationKey()    => value;

        public override string  ToString()          => value.ToString();
    }

    enum InventoryItemType {
        Axe     = 1,
        Gun     = 2,
        Sword   = 3,
        Shield  = 4,
    }

    /// <summary> <see cref="IRelation{TKey}"/> using an enum as relation key. </summary>
    [ComponentKey("item")] // TODO remove?
    struct InventoryItem : IRelation<InventoryItemType>
    {
        public          InventoryItemType   type;               // key
        public          int                 amount;
        public          InventoryItemType   GetRelationKey()    => type;

        public override string              ToString()          => type.ToString();
    }

    public struct AttackRelation : ILinkRelation
    {
        public          Entity  target;             // key
        public          int     speed;
        public          Entity  GetRelationKey()    => target;

        public override string  ToString()          => target.Id.ToString();
    }
    #endregion



#region tags

[ComponentSymbol("T1")] struct Tag1 : ITag;
[ComponentSymbol("T2")] struct Tag2 : ITag;
[ComponentSymbol("T3")] struct Tag3 : ITag;
[ComponentSymbol("T4")] struct Tag4 : ITag;


struct TestDemo : ITag { }

[ComponentSymbol("TT1")] struct TestTag1 : ITag;
[ComponentSymbol("TT2")] struct TestTag2 : ITag;
[ComponentSymbol("TT3")] struct TestTag3 : ITag;
[ComponentSymbol("TT4")] struct TestTag4 : ITag;
[ComponentSymbol("TT5")] struct TestTag5 : ITag;
[ComponentSymbol("TT6")] struct TestTag6 : ITag;
[ComponentSymbol("TT7")] struct TestTag7 : ITag;
[ComponentSymbol("TT8")] struct TestTag8 : ITag;
[ComponentSymbol("TT9")] struct TestTag9 : ITag;

#endregion



#region custom types

/// <summary>
/// Implements <see cref="IEquatable{T}"/> and <see cref="GetHashCode"/> to support<br/>
/// usage as key in <see cref="Dictionary{TKey,TValue}"/> and <see cref="HashSet{T}"/>
/// </summary>
public struct Item : IEquatable<Item>
{
    int         key;
    Coord2      coord;
    EnumRGBA    rgba;
    EnumByte    enumByte;
    
    public Item (int key, int x, int y) {
        this.key   = key;
        coord.x     = x;
        coord.y     = y;
    }

    public          bool    Equals(Item other)  => key == other.key;
    public override int     GetHashCode()       => key;
    
    public Item() {
        key = -1;
    }
}

/// <summary>Custom type without a TypeDrawer.</summary>
struct Struct1
{
    [UiDrag(1, 0, 10_000)]  public int      value1;
    [UiDrag(1, 0, 10_000)]  public Vector3  vector3;
                            public Struct2  struct2;
}

/// <summary>Struct added as member in <see cref="Struct1"/>.<br/> Custom type without a TypeDrawer.</summary>
struct Struct2
{
    [UiDrag(1, 0, 10_000)]  public int      value2;
                            public Struct3  struct3;
                            public Class1   class1;
}

/// <summary>Struct added as member in <see cref="Struct2"/>.<br/> Custom type without a TypeDrawer.</summary>
struct Struct3
{
    [UiDrag(1, 0, 10_000)]  public int      value3;
                            public Struct4  struct4; // not shown in Inspector. nesting level > 3
}

/// <summary>Struct added as member in <see cref="Struct3"/>.<br/> Custom type without a TypeDrawer.</summary>
struct Struct4
{
    [UiDrag(1, 0, 10_000)]  public int      value4;
}

struct Generic1<T> : IComponent
{
    [UiDrag(1, 0, 10_000)]  public T         value;
}

class Class1 : AbstractClass, IInterface
{
    [UiDrag(1, 0, 100)]     public          int      value1 { get; set; }
    [UiDrag(1, 0, 100)]     public override int      value2 { get; set; }
    [UiDrag(1, 0, 100)]     public          Vector3  vector3;
}

interface IInterface
{
    [UiDrag(1, 0, 1_000)]   public          int value1 { get; set; }
}

abstract class AbstractClass
{
    [UiDrag(1, 0, 1_000)]   public virtual  int value2 { get; set; }
}

struct MiscComponent
{
    public object   obj;
    public int[]    array;
}

/// <summary>Custom 2D vector type</summary>
public struct Coord2
{
    public int x;
    public int y;
}

/// <summary>Custom 3D vector type</summary>
struct Coord3
{
    public int x;
    public int y;
    public int z;
}

/// <summary>Custom vector type to encode an RGB color</summary>
[StructLayout(LayoutKind.Explicit)]
struct ColorRGB
{
    [FieldOffset(0)] public long value;
    
    [FieldOffset(0)] public byte R;
    [FieldOffset(1)] public byte G;
    [FieldOffset(2)] public byte B;
}

/// <summary>Custom vector type to encode an RGBA color</summary>
[StructLayout(LayoutKind.Explicit)]
struct ColorRGBA
{
    [FieldOffset(0)] public long value;
    
    [FieldOffset(0)] public byte R;
    [FieldOffset(1)] public byte G;
    [FieldOffset(2)] public byte B;
    [FieldOffset(3)] public byte A;
}

/// <summary>The sprite fields and its image are rendered with custom drawer <see cref="SpriteDrawer"/>.</summary>
struct Sprite 
{
    /// <summary>Column of sprite in <see cref="TileSet"/> component</summary>
    internal int col;
    /// <summary>Row of sprite in <see cref="TileSet"/> component</summary>
    internal int row;
    /// <summary>
    /// The entity having a <see cref="TileSet"/> component. ECS acts as Asset Manager.<br/>
    /// This approach enables to show and edit the TileSet in EcGui.<br/>
    /// <b>Note:</b> For simplicity and performance this field should not be a reference type.
    /// </summary>
    /// <remarks>
    /// Possible alternatives<br/>
    /// - Using only one TileSet -> a singleton.<br/>
    /// - Store and reference a TileSet in a separate Asset Manager outside the ECS.
    /// </remarks>
    internal int setId;
    
    public Sprite(int col, int row, int setId) {
        this.col    = col;
        this.row    = row;
        this.setId  = setId;
    }
}

struct ChildCollections
{
    public Item[]               array;      // array
    public List<int>            listInt;    // IList<>
    public LinkedList<Struct2>  linkedList; // ICollection<>
    
    internal void Copy(in ChildCollections source)
    {
        if (source.array != null) {
            array = new Item[source.array.Length];
            source.array.CopyTo(array, 0);
        }
        if (source.listInt != null) {
            listInt = new List<int>(source.listInt);
        }
        if (source.linkedList != null) {
            linkedList = new LinkedList<Struct2>(source.linkedList);
        }
    }
}

struct CycleStruct
{
    public CycleStruct  child => default; // TODO check why missing
}

class CycleList
{
    List<CycleList> children;
}


#pragma warning disable CS9181 // Inline array indexer will not be used for element access expression.

/// <summary>
/// Inline arrays provide high memory locality and have a fixed length.<br/>
/// To view and edit them in the Inspector it implements <see cref="IList{T}"/>.
/// </summary>
[InlineArray(4)]
public struct InlineItem4 : IList<Item>
{
    internal Item   values;
    
    public ReadOnlySpan<Item> AsSpan() => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<InlineItem4, Item>(ref this), 4);
    
    IEnumerator<Item>   IEnumerable<Item>.GetEnumerator()   => new ItemArray4Enumerator(this);
    IEnumerator         IEnumerable.GetEnumerator()         => new ItemArray4Enumerator(this);
    public ItemArray4Enumerator     GetEnumerator()         => new ItemArray4Enumerator(this); // allocation free enumerator
    
    public void Clear   ()                      => throw new NotSupportedException("array length is fixed to 4");
    public void Add     (Item item)             => throw new NotSupportedException("array length is fixed to 4");
    public bool Remove  (Item item)             => throw new NotSupportedException("array length is fixed to 4");
    public void Insert  (int index, Item item)  => throw new NotSupportedException("array length is fixed to 4");
    public void RemoveAt(int index)             => throw new NotSupportedException("array length is fixed to 4");

    public bool Contains(Item item)             => AsSpan().Contains(item);
    public int  IndexOf (Item item)             => AsSpan().IndexOf(item);
    public int  Count                           => 4;
    public bool IsReadOnly                      => false;
    public void CopyTo(Item[] array, int arrayIndex) => AsSpan().CopyTo(new Span<Item>(array, arrayIndex, 4));

    public Item this[int index] {
        get => this[index];
        set => this[index] = value;
    }
}

public struct ItemArray4Enumerator : IEnumerator<Item>
{
    private             int         index;
    private readonly    InlineItem4 array;
    
    internal ItemArray4Enumerator(in InlineItem4 array) {
        this.array = array;
    }
    
    public bool MoveNext() {
        if (index < array.Count) {
            index++;
            return true;
        }
        return false;
    }

    public  void    Reset()             => index = 0;
    public  Item    Current             => array[index - 1];
            object  IEnumerator.Current => Current;
            
    public void Dispose() { }
}
#endregion



#region enums

enum EnumByte : byte
{
    Value1 	    = 1,
    Value10     = 10,
    Value100    = 100
}

enum WeekDay
{
    Sunday      = 0,
    Monday      = 1,
    Tuesday     = 2,
    Wednesday   = 3,
    Thursday    = 4,
    Friday      = 5,
    Saturday    = 6,
}

/// <summary>
/// Assign single character names to flags with [UiFlag()].<br/>
/// They are shown instead of their bit index in EcGui.
/// </summary>
[Flags]
enum EnumRGBA : byte
{
    [UiFlag("R")] Red   = 1 << 3,
    [UiFlag("G")] Green = 1 << 2,
    [UiFlag("B")] Blue  = 1 << 1,
    [UiFlag("A")] Alpha = 1 << 0,
}

[Flags]
enum EnumFlags8 : byte
{
    Bit0    = 1 << 0,
    Bit1    = 1 << 1,
    Bit2    = 1 << 2,
    Bit3    = 1 << 3,
    Bit4    = 1 << 4,
    Bit5    = 1 << 5,
    Bit6    = 1 << 6,
    Bit7    = 1 << 7,
}

[Flags]
enum EnumFlags64 : ulong
{
    Bit0    =        1 << 0,
    Bit61   = (ulong)1 << 61,
    Bit62   = (ulong)1 << 62,
    Bit63   = (ulong)1 << 63,
}

/// <summary>By default, groupSize: 8 (8 bits per byte) and bits are ordered descending.</summary>
[Flags]
enum EnumFlags32 : uint
{
    // declaration order has no effect on UI
    Bit24   = 1 << 24,
    Bit25   = 1 << 25,
    Bit26   = 1 << 26,
    Bit27   = 1 << 27,
    Bit28   = 1 << 28,
    Bit29   = 1 << 29,
    Bit30   = 1 << 30,
    Bit31   = (uint)1 << 31,
    // next group
    Bit23   = 1 << 23,
    Bit22   = 1 << 22,
    Bit21   = 1 << 21,
    Bit20   = 1 << 20,
    Bit19   = 1 << 19,
    Bit18   = 1 << 18,
    Bit17   = 1 << 17,
    Bit16   = 1 << 16,
    // next group
    Bit0            = 1 << 0,
    Bit1            = 1 << 1,
    Bit1_Redundant  = 1 << 1,   // redundant bit declaration is ignored. Same behavior as BCL
    
    Mask    = 255,              // values with multiple bits are ignored
}

/// <summary>Set groupSize: 10 and order bits ascending. Space between groups: 12</summary>
[Flags][UiFlags(10, true, 12)]
enum Layers : uint
{
    // declaration order has no effect on UI
    Layer0    = 1 << 0,
    Layer1    = 1 << 1,
    Layer2    = 1 << 2,
    Layer3    = 1 << 3,
    Layer4    = 1 << 4,
    Layer5    = 1 << 5,
    Layer6    = 1 << 6,
    Layer7    = 1 << 7,
    Layer8    = 1 << 8,
    Layer9    = 1 << 9,
    // next group
    Layer10   = 1 << 10,
    Layer11   = 1 << 11,
    Layer12   = 1 << 12,
    Layer13   = 1 << 13,
    Layer14   = 1 << 14,
    Layer15   = 1 << 15,
    Layer16   = 1 << 16,
    Layer17   = 1 << 17,
    Layer18   = 1 << 18,
    Layer19   = 1 << 19
}
#endregion

