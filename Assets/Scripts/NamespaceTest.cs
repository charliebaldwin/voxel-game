namespace Tag
{
    enum Type
    {
        Null,
        Item,
        Block
    }
    namespace Block
    {
        enum ID : int
        {
            Air,
            Grass,
            Dirt,
            Stone
        }
    }
}

public class Test
{
    Tag.Type Type = Tag.Type.Item;
    int BlockType = (int)Tag.Block.ID.Air;
}




