using System;


namespace Glowworm.Helpers.Enumeration;


/// <summary>
/// ??????? Key ???
/// </summary>
[AttributeUsage(AttributeTargets.All)]
public class LocalizationKeyAttribute : Attribute
{
    public string Key { get; set; }

    public LocalizationKeyAttribute(string key)
    {
        Key = key;
    }
}


