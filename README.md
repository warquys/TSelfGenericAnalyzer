# TSelfGeneric Analyzer

Naming an generic attribute `TSelf` (Case Insensitivity) the anazlyser will check if the inheriting class or implenting interface got passed the class implenting it or a `TSelf` generic argument.

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
