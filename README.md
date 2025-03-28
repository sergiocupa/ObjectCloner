# ObjectCloner
Intended to copy objects quickly and simply. It creates a constructor at runtime that scans the object properties and assigns the corresponding properties of the target object. It supports primitive types, object, array and list. For object type properties, a new constructor is also created for this type. It does not use reflection, which has low performance, it uses a type creator that builds the constructor through IL codes.


Bugs identified to fix
- [#] Above 2 levels, with circular reference, does not copy.
- [ ] Complete implementation of ExpressionPrinter
