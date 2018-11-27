using System;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class GameObjectTypeAttribute : Attribute
{
    public string Type { get; private set; }

    public GameObjectTypeAttribute(string type)
    {
        Type = type;
    }
}