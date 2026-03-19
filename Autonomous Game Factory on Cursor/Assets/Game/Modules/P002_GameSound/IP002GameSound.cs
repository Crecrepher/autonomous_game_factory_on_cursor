namespace Game
{
    public interface IP002GameSound
    {
        void Init();
        void Release();
        void SetEnabled(bool enabled);
        bool IsEnabled { get; }
        int LastPlayedSoundIndex { get; }
    }
}
