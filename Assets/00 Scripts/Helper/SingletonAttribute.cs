using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public class SingletonAttribute : Attribute
{
    public readonly string PathInstance;
    public readonly bool IsDontDestroy;

    public SingletonAttribute(string _pathInstance, bool _isDontDestroy)
    {
        PathInstance = _pathInstance;
        IsDontDestroy = _isDontDestroy;
    }
    public SingletonAttribute(string _name)
    {
        PathInstance = _name;
        IsDontDestroy = true;
    }
}