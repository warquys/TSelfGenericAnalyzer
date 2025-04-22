# TSelfGeneric Analyzer

Naming an generic attribute `TSelf` (Case Insensitivity) the anazlyser will check if the inheriting class or implenting interface got passed the class implenting it or a `TSelf` generic argument. Only C# is supported.

```cs
interface ISelfRequested<TSelf> where TSelf : ISelfRequested<TSelf> { }
//                                 it's a good idea to restrict the TSelf in a cricular way.

class Implementation : ISelfRequested<Implementation> { }

class BadImplementation : ISelfRequested<Implementation> { }
//                                       ~~~~~~~~~~~~~~
//                      Warning here, should be BadImplementation class.

// No error because the generic parameter is fill with an other generic paramter with the same objectif
// This give the task of filling the paramter to an upper level of inheritance.
abstract class NotImplementation<TSelf> : ISelfRequested<TSelf> where TSelf : NotImplementation<TSelf> { }
// abstract is not needed, i see some case where you can strange code that can use it. BUT... meh
```

If you do not like [duck typing](https://en.wikipedia.org/wiki/Duck_typing) you can use an attriubte on the generic param.

```cs
// You need to define it your self
[AttributeUsage(AttributeTargets.GenericParameter)]
public class TSelfAttribute : Attribute { }

interface ISelfRequested<[TSelf] T> where T : ISelfRequested<T> { }

class Implementation : ISelfRequested<Implementation> { }

class BadImplementation : ISelfRequested<Implementation> { }
//                                       ~~~~~~~~~~~~~~
//                      Warning here, should be BadImplementation class.
```

This feature is not enable by default.
You can configure the analyser following this .editorconfig (default one is showed):

```conf
[*.cs]
dotnet_tselfgeneric.tself_attribute_name = TSelfAttribute
dotnet_tselfgeneric.tself_attribute_name.enable = false
dotnet_tselfgeneric.tself_param_name = TSelf
dotnet_tselfgeneric.tself_param_name.enable = true
```

know issue:

```cs
interface ISelfRequested<TSelf> where TSelf : ISelfRequested<TSelf> { }

class SomeImplementation<T> : ISelfRequested<T> where T : ISelfRequested<T> { }

class Implementation : ISelfRequested<Implementation> { }

class BadImplementation : SomeImplementation<Implementation> { }
// No Warning :(
```

This is normal i do not finsh to write the analyzer.
In the future "SomeImplementation" will require to prolongate the TSelf.
TBD...
