namespace Game
{
    public struct P002MatchResult
    {
        public int BlockType { get; set; }
        public int MatchCount { get; set; }
        public int[] MatchedX { get; set; }
        public int[] MatchedY { get; set; }
        public int SpawnX { get; set; }
        public int SpawnY { get; set; }
        public int ActivatedSpecialType { get; set; }
        public int ActivatedSpecialX { get; set; }
        public int ActivatedSpecialY { get; set; }
    }
}
