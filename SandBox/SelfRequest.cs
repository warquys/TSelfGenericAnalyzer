namespace SandBox;

[System.AttributeUsage(AttributeTargets.GenericParameter, Inherited = false, AllowMultiple = false)]
sealed class TSelfAttribute : Attribute { }

public class SelfRequest<[TSelf] T>
{
    
}

// TODO: Culture bug in FR it keep be EN 
public class Imp<T> : SelfRequest<T>
{

}