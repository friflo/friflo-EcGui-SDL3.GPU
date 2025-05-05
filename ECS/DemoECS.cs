using System;
using System.Collections.Generic;
using Friflo.EcGui;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

// ReSharper disable HeuristicUnreachableCode
// ReSharper disable ConditionIsAlwaysTrueOrFalse
// ReSharper disable UnusedMember.Local
// ReSharper disable InconsistentNaming
// ReSharper disable FieldCanBeMadeReadOnly.Local
// ReSharper disable once CheckNamespace
namespace Demo;


#region systems

internal class PositionSystem : QuerySystem<Position>
{
    protected override void OnUpdate() { }
}
    
internal class NameSystem : QuerySystem<EntityName>
{
    protected override void OnUpdate() { }
}

internal class SubSystem1 : QuerySystem<DragInt, Animated>
{
    protected override void OnUpdate() { }
}

internal class SubSystem2 : QuerySystem<Position, DragInt, Animated>
{
    protected override void OnUpdate() { }
}
#endregion



internal static class DemoECS
{
    private static EntityStore              store;
    private static ArchetypeQuery<Animated> animatedQuery;
    
    /// <summary>
    /// Creates a demo <see cref="EntityStore"/> and add the store, some queries and systems to the Explorer.<br/>
    /// Adding stores, queries and systems can be done in any place. Even every frame.
    /// </summary>
    internal static void CreateEntityStore()
    {
        // --- Create a store with test data for demoing
        store = new EntityStore();
        
        var container1 = new Collections {
            array       = [new(4,0,7), new(2,1,7), new(3,1,7), new(3,2,7), new(4,3,7)],
            intList     = new List<int>(),
            itemList    = [new Item(1,2,3), new Item(4,5,6) ],
            structs2    = [new Struct2 {  value2 = 1, class1 = new Class1(), struct3 = new Struct3 { value3 = 2, struct4 = new Struct4 { value4 = 3 }}}],
            classes     = [new Class1 { value1 = 1, value2 = 2}, new Class1 { value1 = 11, value2 = 12 }],
            hashSet     = [4,5,6],
            itemHashSet = [new Item(10, 0, 0), new Item(11, 1,1) ],
            linkedList  = new LinkedList<Item>(), 
            stack       = new Stack<int>(),
            queue       = new Queue<int>(),
            dictionary  = new Dictionary<int, int> { { 1, 11 }, { 2, 12 }, { 3, 13 } }
        };
        for (int n = 0; n < 10_000; n++) container1.intList.Add(n);
        container1.inline4[0] = new Item(10, 20, 30);
        container1.inline4[1] = new Item(11, 21, 31);
        container1.linkedList.AddLast(new Item(20, 20, 20));
        container1.linkedList.AddLast(new Item(21, 21, 21));
        container1.stack.Push(7);
        container1.stack.Push(8);
        container1.stack.Push(9);
        container1.queue.Enqueue(1);
        container1.queue.Enqueue(2);
        container1.queue.Enqueue(3);
        var itemArray = new ItemArray {
            array = [new Item(1,2,3), new Item(4,5,6), new Item(7,8,9) ],
        };
        var rgb     = new RGB    { rgb = new ColorRGB {R = 241, G = 0, B = 255 }};
        var flags8  = new Flags8 { flags8 = EnumFlags8.Bit7 | EnumFlags8.Bit6 | EnumFlags8.Bit4 };
        
        store.CreateEntity(new EntityName("entity 1"), GetTile(1), new DragInt(1), new CompA(1),  new CompB(11), new CompC(42), flags8, new CompD(1337), itemArray, rgb,  Tags.Get<Tag1, Tag2, Tag3, Tag4, TestDemo>());
        store.CreateEntity(new EntityName("entity 2"), GetTile(2), new Demo(), new DragInt(2),                                                               container1,   Tags.Get<                        TestDemo>());
        store.CreateEntity(new EntityName("entity 3"), GetTile(3), new Demo(), new DragInt(3), new CompA(2),                                                    Tags.Get<Tag1,                   TestDemo>());
        store.CreateEntity(new EntityName("entity 4"), GetTile(4), new Demo(), new DragInt(4), new CompA(3),  new CompB(4),                                     Tags.Get<Tag1, Tag2,             TestDemo>());
        store.CreateEntity(new EntityName("entity 5"), GetTile(5), new Demo(), new DragInt(5), new CompA(5),  new CompB(6),  new CompC(7),                      Tags.Get<Tag1, Tag2, Tag3,       TestDemo>());
        store.CreateEntity(new EntityName("entity 6"), GetTile(6), new Demo(), new DragInt(6),                                              new CompD(8),       Tags.Get<                  Tag4, TestDemo>());
        
        var tileSetEntity7 = store.CreateEntity(7);
        tileSetEntity7.AddComponent(new EntityName("tiny wonder rpg"));
        tileSetEntity7.AddComponent(new TileSet { spriteWidth = 64, spriteHeight = 64, texture = "tiny-wonder-rpg-icons" });

        var tileSetEntity8 = store.CreateEntity(8);
        tileSetEntity8.AddComponent(new EntityName("world tileset"));
        tileSetEntity8.AddComponent(new TileSet { spriteWidth = 64, spriteHeight = 64, texture = "world_tileset" });
        
        
        
        store.CreateEntity(new EntityName("Test Objects"), new Objects { abstractClass = new Class1(), @interface = new Class1()});

        // --- create multiple entities with components
        int id    = 10;
        var tags = Tags.Get<TestTag1,TestTag2,TestTag3,TestTag4,TestTag5>();
        for (; id <= 95; id++) {
            store.CreateEntity(
                new EntityName($"entity {id}"),
                new Enums(),
                new DragInt    { value = id },
                GetTile(id)
            );
        }
        for (; id <= 100; id++) {
            store.CreateEntity(
                new EntityName($"entity {id}"),
                new Enums(),
                new DragInt    { value = id },
                new EntityLink { entity = store.GetEntityById(id - 90) },
                GetTile(id)
            );
        }
        for (; id <= 1000; id++) {
            store.CreateEntity(
                new EntityName($"entity {id}"),
                new Animated(),
                new DragInt   { value = id },
                new Enums(),
                GetTile(id),
                tags
            );
        }
        for (; id <= 10_000; id++) {
            if (id <= 10_000) store.CreateEntity( new Animated(), new DragInt{ value = id }, new Position(id,-id,id), new Enums(), GetTile(id), tags);
            else              store.CreateEntity(                 new DragInt{ value = id }, new Position(id,-id,id), new Enums(), GetTile(id), tags);
        }

        
        var root = new SystemRoot(store) {
            new PositionSystem(),
            new NameSystem(),
            new SystemGroup("Sub Group") {
                new SubSystem1(),
                new SubSystem2(),
            }
        };
        
        // --- Add stores, queries and systems to Explorer.
        // This can be done once on startup or later anywhere in code on demand.
        
        // add the store to Explorer > query
        EcGui.AddExplorerStore("Store", store);
        
        // add entire system hierarchy to Explorer > query
        EcGui.AddExplorerSystems(root);
        
        // create some queries and them to Explorer > query
        var demoTags = store.Query().AllComponents(ComponentTypes.Get<Demo>()).WithDisabled();
        EcGui.AddExplorerQuery("demo-tags", demoTags);
        
        var demoComponents = store.Query().AllTags(Tags.Get<TestDemo>()).WithDisabled();
        EcGui.AddExplorerQuery("demo-components", demoComponents);
        
        // var nameQuery = store.Query<EntityLink>();
        // EcGui.AddExplorerQuery("entity links", nameQuery);
        
        animatedQuery = store.Query<Animated>();
        EcGui.AddExplorerQuery("Animated", animatedQuery);
        
        var dragCells = store.Query<DragInt>();
        EcGui.AddExplorerQuery("DragInt", dragCells);
        
        var tileSets = store.Query<TileSet>();
        EcGui.AddExplorerQuery("TileSet", tileSets);
    }

    private static bool addEntities = true;
    private static bool update = true;
    private static float frame;
    
    /// <summary>Run some simulation stuff.</summary>
    internal static void Update()
    {
        if (!update || animatedQuery == null) {
            return;
        }
        frame += 0.004f;
        foreach (var (animateComponents, entities) in animatedQuery.Chunks)
        {
            var animatedSpan = animateComponents.Span;
            for (int n = 0; n < animatedSpan.Length; n++) {
                animatedSpan[n].value = 1000 + (int)(1000f * MathF.Sin(frame + entities[n]));
            }
        }
    }
    
    private static Tile GetTile(int id) {
        --id;
        int x = id % 100;
        int y = id / 100;
        int col = (id % 24) % 6;
        int row = (id % 24) / 6;
        return new Tile { position = new Coord2 { x = x, y = y }, sprite = new Sprite { col = col, row = row, setId = 7 }};
    }
    
    private static void AddRemoveEntities()
    {
        if (addEntities) {
            var list = animatedQuery.ToEntityList();
            int count = 0;
            foreach (var entity in list) {
                entity.DeleteEntity();
                if (count++ == 10) break;
            }
        } else {
            for (int i = 0; i < 10; i++) {
                store.CreateEntity(new Animated { value = i});
            }
        }
        addEntities ^= true;
    }


#pragma warning disable CS0162 // Unreachable code detected

    /// <summary>
    /// Customize Explorer and Inspector. Should be called only once on startup.<br/>
    /// </summary>
    internal static void CustomizeEcGui()
    {
        // --- register custom TypeDrawer's on startup
        TypeDrawers.Register();

        // --- add component member columns
        if (false) {
            EcGui.Explorer.AddComponentMemberColumn<Enums>      (nameof(Enums.enumByte));
            EcGui.Explorer.AddComponentMemberColumn<EntityName> ("value.Length");
            EcGui.Explorer.AddComponentMemberColumn<DragInt>    (nameof(DragInt.value));
            EcGui.Explorer.AddComponentMemberColumn<Tile>       (nameof(Tile.sprite));
        
            EcGui.Explorer.AddComponentMemberColumn<Collections>(nameof(Collections.array));
            EcGui.Explorer.AddComponentMemberColumn<Collections>(nameof(Collections.itemList));
        
            EcGui.Explorer.AddComponentMemberColumn<Position>   (nameof(Position.value));
            EcGui.Explorer.AddComponentMemberColumn<Animated>   (nameof(Animated.value));
            EcGui.Explorer.AddComponentMemberColumn<Strings>    (nameof(Strings.str));
            EcGui.Explorer.AddComponentMemberColumn<Vectors>    (nameof(Vectors.vector3));
        }
        // --- add tag columns
        if (false) {
            EcGui.Explorer.AddTagColumn<Tag1>();
            EcGui.Explorer.AddTagColumn<Tag2>();
            EcGui.Explorer.AddTagColumn<Tag3>();
            EcGui.Explorer.AddTagColumn<Tag4>();
        }
        EcGui.Explorer.OnSelectedEntityChange += _ => { };
    }
}
