//  MIT License – Modified for Mandatory Attribution
//  
//  Copyright(c) 2025 Sergio Paludo
//  
//  Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files, 
//  to use, copy, modify, merge, publish, distribute, and sublicense the software, including for commercial purposes, provided that:
//  
//  01. The original author’s credit is retained in all copies of the source code;
//  02. The original author’s credit is included in any code generated, derived, or distributed from this software, including templates, libraries, or code - generating scripts.
//  
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED.


namespace ObjectCloner
{
    public class Cloner
    {
        public static T Clone<T>(T instance)
        {
            var type          = DynamicConstructorAssembler.Create(typeof(T));
            var all_instances = new Dictionary<int, object>();
            var dync          = type.BuildedType.GetConstructors()[0];
            var dyn_inst      = (T)dync.Invoke(new object[] { instance, all_instances });

            return (T)dyn_inst;
        }

    }
}
