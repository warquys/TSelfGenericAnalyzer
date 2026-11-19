# TSelfGeneric Analyzer

When naming an generic attribute `TSelf` (Case Insensitivity) the anazlyser will check if the inheriting class or implenting interface is implenting it or is a `TSelf` generic argument. Only C# is now supported.

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
// You need to define it yourself outisde an namsapce
[AttributeUsage(AttributeTargets.GenericParameter)]
public class TSelfAttribute : Attribute { }

interface ISelfRequested<[TSelf] T> where T : ISelfRequested<T> { }

class Implementation : ISelfRequested<Implementation> { }

class BadImplementation : ISelfRequested<Implementation> { }
//                                       ~~~~~~~~~~~~~~
//                      Warning here, should be BadImplementation class.
```

You can configure the analyzer in `.editorconfig`. Values may contain a comma- or
semicolon-separated list, so existing single-value configurations remain valid:

```conf
[*.cs]
dotnet_tselfgeneric.tself_attribute_name = TSelfAttribute, Self.TSelfAttribute
dotnet_tselfgeneric.tself_attribute_name.enable = true
dotnet_tselfgeneric.tself_param_name = TSelf; TSelfType
dotnet_tselfgeneric.tself_param_name.enable = true
```

The attribute names are metadata names (including the namespace when needed).
The analyzer keeps the default values `TSelfAttribute` and `TSelf` when no
configuration is provided. When multiple values are configured, the nested
generic-parameter diagnostic accepts any configured name or attribute.

Not inheriting the aspect of the TSelf when passing a generic argument will result in a warning.

```cs
interface ISelfRequested<TSelf> where TSelf : ISelfRequested<TSelf> { }

class SomeImplementation<T> : ISelfRequested<T> where T : ISelfRequested<T> { }
//                       ~
//  Warning here, should be name TSelf or as the TSelfAttribute like [TSelf] T.
```

Analyzer and code-fix messages are available in French (`fr`), Spanish (`es`),
German (`de`), Italian (`it`), and Brazilian Portuguese (`pt-BR`).
