namespace Game
{
    public struct P002BlockData
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int BlockType { get; set; }
        public int SpecialType { get; set; }
        public bool IsMatched { get; set; }
        public bool IsMoving { get; set; }

        public bool IsValid
        {
            get { return BlockType != 0; }
        }

        public bool IsSpecial
        {
            get { return SpecialType != 0; }
        }
    }
}
