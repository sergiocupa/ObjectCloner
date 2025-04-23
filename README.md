# ObjectCloner
Designed to copy objects quickly and easily. To be able to copy, it creates a constructor at runtime, where all the properties of the input object are assigned. It supports primitive types, objects, arrays and lists. It supports circular references in the object structure and any level of deep hierarchy. Instead of using reflection, which has poor performance, it uses a type generator that builds the constructor through IL code.

Features to be implemented:
- [X] Implement a circular reference breaker in the instance.
- [ ] Optimizations
- [ ] Performance testing and measuring the generated instances
