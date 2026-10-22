using UnityEngine;

public interface IInitializable
{
    bool Initialized { get; }
    public void Initialize();
}
