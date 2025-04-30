# ObjectCloner
Designed to copy objects quickly and easily. To be able to copy, it creates a constructor at runtime, where all the properties of the input object are assigned. It supports primitive types, objects, arrays and lists. It supports circular references in the object structure and any level of deep hierarchy. Instead of using reflection, which has poor performance, it uses a type generator that builds the constructor through IL code.

In tests using a loop with 1,000,000 instances, the traditional instantiation averaged 0.458 μs. Using the cloner, the average was 1.318 μs — approximately 2 to 3 times slower than traditional instantiation. It’s worth noting that this cloner includes handling for circular references and null object references.
