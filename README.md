# ObjectCloner
Designed to copy objects quickly and easily. To be able to copy, it creates a constructor at runtime, where all the properties of the input object are assigned. It supports primitive types, objects, arrays and lists. Copies are made in cascade, as needed, if object-type properties are identified. Instead of using reflection, which has low performance, it uses a type generator that builds the constructor through IL code.


Bugs identified to fix
- [ ] Above 2 levels, with circular reference, does not copy.
- [ ] Complete implementation of ExpressionPrinter
