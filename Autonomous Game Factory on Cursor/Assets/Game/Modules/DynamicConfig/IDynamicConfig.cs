namespace Game
{
    public interface IDynamicConfig
    {
        void Init();
        void Tick(float deltaTime);

        float GetFloat(int key, float defaultValue);
        int GetInt(int key, int defaultValue);
        void SetFloat(int key, float value);
        void SetInt(int key, int value);
        bool HasKey(int key);

        event System.Action<int> OnValueChanged;
    }
}
